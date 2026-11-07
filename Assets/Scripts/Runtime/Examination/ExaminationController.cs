using System;
using System.Collections.Generic;
using System.Text;
using ProjectED.Cases;
using ProjectED.Days;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ProjectED.Examination
{
    public sealed class ExaminationController : MonoBehaviour
    {
        [Header("DAY 자동 생성")]
        [SerializeField]
        private DayDefinition dayDefinition;

        [SerializeField, Tooltip("같은 DAY SO와 시드를 사용하면 같은 시민 구성이 생성됩니다.")]
        private int daySeed = 1001;

        [Header("수동 사건 큐 (DAY 미지정 시 사용)")]
        [SerializeField]
        private List<CitizenCaseData> caseQueue = new();

        [SerializeField]
        private bool loadFirstCaseOnStart = true;

        [Header("한글 폰트")]
        [SerializeField]
        private TMP_FontAsset fontOverride;

        [SerializeField]
        private bool useWindowsSystemFontFallback = true;

        [SerializeField]
        private string windowsSystemFontFamily = "Malgun Gothic";

        [Header("기존 화면 UI")]
        [SerializeField]
        private Image citizenImage;

        [SerializeField]
        private GameObject idCardPanel;

        [SerializeField]
        private GameObject paperPanel;

        [SerializeField]
        private Button displayButton;

        [SerializeField]
        private Button stampButton;

        [Header("데이터 표시")]
        [SerializeField]
        private CaseDialogueController dialogueController;

        [SerializeField]
        private TMP_Text idCardText;

        [SerializeField]
        private TMP_Text paperText;

        [SerializeField]
        private TMP_Text statusText;

        [Header("처분 선택")]
        [SerializeField]
        private Button approvalButton;

        [SerializeField]
        private Button rejectionButton;

        [SerializeField]
        private Button detentionButton;

        [SerializeField]
        private Button treatmentButton;

        private readonly List<CitizenCaseInstance> runtimeQueue = new();
        private CitizenCaseData currentCase;
        private CitizenCaseInstance currentCitizen;
        private DayPlan currentDayPlan;
        private int currentCaseIndex = -1;
        private CaseDisposition selectedDisposition = CaseDisposition.ExtractionApproval;
        private ExaminationState state = ExaminationState.Waiting;
        private TMP_FontAsset generatedRuntimeFont;

        public event Action<CitizenCaseData, CaseDisposition, bool> CaseJudged;
        public event Action<CitizenCaseData> DisplayRequested;
        public event Action<CitizenCaseInstance, CaseDisposition, bool> CitizenJudged;
        public event Action<CitizenCaseInstance> CitizenDisplayRequested;
        public event Action<DayPlan> DayLoaded;
        public event Action<DayPlan> DayCompleted;

        public CitizenCaseData CurrentCase => currentCase;
        public CitizenCaseInstance CurrentCitizen => currentCitizen;
        public DayPlan CurrentDayPlan => currentDayPlan;
        public DayDefinition DayDefinition => dayDefinition;
        public int DaySeed => daySeed;
        public int CurrentCitizenPosition => currentCaseIndex + 1;
        public int TotalCitizenCount => runtimeQueue.Count;
        public CaseDisposition SelectedDisposition => selectedDisposition;
        public bool HasActiveCase => state == ExaminationState.Inspecting;

        private void Awake()
        {
            ResolveSceneReferences();
            ApplyRuntimeFont();
        }

        private void OnEnable()
        {
            AddButtonListeners();
            if (dialogueController != null)
            {
                dialogueController.DialogueCompleted += HandleDialogueCompleted;
            }
        }

        private void Start()
        {
            // DAY 로드는 CampaignManager가 담당한다. 기존 샘플 DAY와 이중 로딩되지 않도록 막는다.
            if (CampaignManager.Instance != null &&
                CampaignManager.Instance.examinationController == this)
            {
                return;
            }

            if (!loadFirstCaseOnStart)
            {
                return;
            }

            if (dayDefinition != null)
            {
                LoadDay(dayDefinition, daySeed);
            }
            else
            {
                SetCaseQueue(caseQueue, true);
            }
        }

        private void OnDisable()
        {
            RemoveButtonListeners();
            if (dialogueController != null)
            {
                dialogueController.DialogueCompleted -= HandleDialogueCompleted;
            }
        }

        public bool LoadDay(DayDefinition definition, int seed, bool loadImmediately = true)
        {
            dayDefinition = definition;
            daySeed = seed;
            currentDayPlan = null;
            runtimeQueue.Clear();

            var buildResult = new DayCasePlanner().Build(definition, seed);
            if (!buildResult.Success)
            {
                Debug.LogError("DAY 상세 오류:\n" + BuildPlanFailureMessage(buildResult));
                ResetCurrentCitizen();
                ShowNoCaseState(BuildPlanFailureMessage(buildResult));
                return false;
            }

            currentDayPlan = buildResult.Plan;
            runtimeQueue.AddRange(currentDayPlan.Citizens);
            ResetCurrentCitizen();
            DayLoaded?.Invoke(currentDayPlan);

            if (loadImmediately)
            {
                LoadCase(0);
            }

            return true;
        }

        public bool ReloadDay(bool loadImmediately = true)
        {
            return LoadDay(dayDefinition, daySeed, loadImmediately);
        }

        public void SetCaseQueue(IEnumerable<CitizenCaseData> cases, bool loadImmediately = true)
        {
            var sourceCases = new List<CitizenCaseData>();
            if (cases != null)
            {
                foreach (var caseData in cases)
                {
                    if (caseData != null)
                    {
                        sourceCases.Add(caseData);
                    }
                }
            }

            caseQueue.Clear();
            caseQueue.AddRange(sourceCases);
            dayDefinition = null;
            currentDayPlan = null;
            runtimeQueue.Clear();

            for (var index = 0; index < caseQueue.Count; index++)
            {
                runtimeQueue.Add(CitizenCaseInstance.CreateStory(caseQueue[index], "MANUAL", index + 1));
            }

            ResetCurrentCitizen();
            if (loadImmediately)
            {
                LoadCase(0);
            }
        }

        public void LoadCase(int index)
        {
            if (index < 0 || index >= runtimeQueue.Count || runtimeQueue[index] == null)
            {
                ShowNoCaseState();
                return;
            }

            currentCaseIndex = index;
            currentCitizen = runtimeQueue[index];
            currentCase = currentCitizen.SourceCase;
            selectedDisposition = CaseDisposition.ExtractionApproval;
            state = ExaminationState.Conversing;

            if (citizenImage != null)
            {
                citizenImage.sprite = currentCitizen.Portrait;
                citizenImage.enabled = currentCitizen.Portrait != null;
                citizenImage.preserveAspect = true;
            }

            SetChoiceButtonsInteractable(false);
            SetDisplayButtonInteractable();
            if (stampButton != null)
            {
                stampButton.interactable = false;
            }

            SetButtonLabel(stampButton, "도장 찍기");
            RenderDocuments();
            RenderDispositionSelection();

            if (dialogueController != null && dialogueController.StartDialogue(currentCitizen))
            {
                SetProgressStatus("말풍선을 클릭해 대화를 진행하세요.");
            }
            else
            {
                BeginInspection();
            }
        }

        public void OpenDisplay()
        {
            if ((state != ExaminationState.Conversing && state != ExaminationState.Inspecting) || currentCitizen == null)
            {
                return;
            }

            if (!CanUseSearchTerminal())
            {
                SetProgressStatus("오늘은 검색 단말기를 사용할 수 없습니다.");
                return;
            }

            CitizenDisplayRequested?.Invoke(currentCitizen);
            if (currentCase != null)
            {
                DisplayRequested?.Invoke(currentCase);
            }

            SetProgressStatus("검색 단말기를 호출했습니다. 단말기 화면은 다음 단계에서 연결됩니다.");
        }

        public void SelectExtractionApproval() => SelectDisposition(CaseDisposition.ExtractionApproval);
        public void SelectDocumentRejection() => SelectDisposition(CaseDisposition.DocumentRejection);
        public void SelectDetention() => SelectDisposition(CaseDisposition.Detention);
        public void SelectTreatmentReferral() => SelectDisposition(CaseDisposition.TreatmentReferral);

        public void StampSelectedDisposition()
        {
            if (state == ExaminationState.Result)
            {
                // 게임 오버 시 다음 시민/다음 DAY로 진행하지 않는다.
                var ending = GameManager.Instance != null
                    ? GameManager.Instance.GetComponent<EndingManager>() : null;
                if ((GameManager.Instance != null && GameManager.Instance.IsGameOver()) ||
                    (ending != null && ending.HasResolvedEnding))
                {
                    Debug.Log("게임 오버: 심사 진행 중단");
                    if (stampButton != null) stampButton.interactable = false;
                    return;
                }

                // 상인 이벤트의 선택을 마치기 전에는 다음 시민으로 넘어가지 않는다.
                if (CampaignManager.Instance != null)
                {
                    var dayEvents = CampaignManager.Instance.GetComponent<DayEventManager>();
                    if (dayEvents != null && dayEvents.merchantOfferPending)
                    {
                        Debug.Log("상인 제안을 먼저 수락하거나 거절해야 합니다.");
                        return;
                    }
                }

                LoadNextCaseOrFinish();
                return;
            }

            if (state != ExaminationState.Inspecting || currentCitizen == null)
            {
                return;
            }

            // Resolve today's active regulations before recording the verdict.
            var ruleVerdict = DayRuleEngine.Evaluate(currentCitizen, currentDayPlan);
            var expectedDisposition = ruleVerdict.ExpectedDisposition;
            var isCorrect = selectedDisposition == expectedDisposition;

            // These authored story choices are all allowed on the stated DAY,
            // even though a CitizenCaseData asset has only one stored verdict.
            CampaignManager campaign = CampaignManager.Instance;
            int verdictDay = currentDayPlan != null && currentDayPlan.Definition != null
                ? currentDayPlan.Definition.DayNumber : 0;
            if (!isCorrect && campaign != null && currentCitizen.IsStoryCase)
            {
                if (verdictDay == 12 && campaign.IsPriestCase(currentCitizen) &&
                    (selectedDisposition == CaseDisposition.ExtractionApproval ||
                     selectedDisposition == CaseDisposition.Detention))
                    isCorrect = true;
                else if (verdictDay == 14 && campaign.IsQuackDoctorCase(currentCitizen) &&
                         selectedDisposition == CaseDisposition.DocumentRejection)
                    isCorrect = true;
            }

            // 판정 결과는 도장을 찍을 때 단 한 번 기록한다.
            if (GameManager.Instance != null)
            {
                GameManager.Instance.ApplyExaminationResult(isCorrect);
            }

            // DAY 16-17: forward confirmed valid consent to the late-game
            // rule controller (which handles its separate trust penalty).
            if (ruleVerdict.ConsentValid && currentDayPlan != null &&
                (currentDayPlan.Definition.DayNumber == 16 ||
                 currentDayPlan.Definition.DayNumber == 17) &&
                !currentCitizen.IsStoryCase && GameManager.Instance != null)
            {
                var lateRules = GameManager.Instance.GetComponent<LateDayEventController>();
                if (lateRules != null)
                {
                    lateRules.RecordConsentRuleDecision(
                        currentDayPlan.Definition.DayNumber, true, false, selectedDisposition);
                }
            }

            state = ExaminationState.Result;
            SetChoiceButtonsInteractable(false);
            SetDisplayButtonInteractable();

            var reason = !string.IsNullOrWhiteSpace(ruleVerdict.Reason)
                ? ruleVerdict.Reason
                : string.IsNullOrWhiteSpace(currentCitizen.JudgmentReason)
                    ? "판정 사유가 설정되지 않았습니다."
                    : currentCitizen.JudgmentReason;
            SetProgressStatus(isCorrect
                ? $"정확한 판정입니다.\n{reason}"
                : $"판정 오류입니다. 정답: {GetDispositionLabel(expectedDisposition)}\n{reason}");

            SetButtonLabel(stampButton, currentCaseIndex + 1 < runtimeQueue.Count ? "다음 시민" : "심사 종료");
            CitizenJudged?.Invoke(currentCitizen, selectedDisposition, isCorrect);
            if (currentCase != null)
            {
                CaseJudged?.Invoke(currentCase, selectedDisposition, isCorrect);
            }

            // Story events resolve some endings *after* CitizenJudged, so check
            // again here before allowing the player to press Next Citizen.
            var endingAfterJudgment = GameManager.Instance != null
                ? GameManager.Instance.GetComponent<EndingManager>() : null;
            if ((endingAfterJudgment != null && endingAfterJudgment.HasResolvedEnding) ||
                (GameManager.Instance != null && GameManager.Instance.IsGameOver()))
            {
                SetButtonLabel(stampButton, "게임 종료");
                if (stampButton != null) stampButton.interactable = false;
                Debug.Log("심사 이후 게임 오버가 확정되어 진행을 중단합니다.");
            }
        }

        private void SelectDisposition(CaseDisposition disposition)
        {
            if (state != ExaminationState.Inspecting)
            {
                return;
            }

            // Detention becomes available on DAY 7, not before.
            if (disposition == CaseDisposition.Detention && currentDayPlan != null &&
                currentDayPlan.Definition != null && currentDayPlan.Definition.DayNumber < 7)
                return;

            selectedDisposition = disposition;
            RenderDispositionSelection();
        }

        private void HandleDialogueCompleted()
        {
            if (state == ExaminationState.Conversing)
            {
                BeginInspection();
            }
        }

        private void BeginInspection()
        {
            state = ExaminationState.Inspecting;
            SetChoiceButtonsInteractable(true);
            if (detentionButton != null && currentDayPlan != null &&
                currentDayPlan.Definition != null && currentDayPlan.Definition.DayNumber < 7)
                detentionButton.interactable = false;
            SetDisplayButtonInteractable();
            if (stampButton != null) stampButton.interactable = true;
            SetProgressStatus("서류를 확인하고 처분을 선택하세요.");
        }

        private void RenderDispositionSelection()
        {
            SetButtonLabel(approvalButton, "추출 승인", selectedDisposition == CaseDisposition.ExtractionApproval);
            SetButtonLabel(rejectionButton, "서류 반려", selectedDisposition == CaseDisposition.DocumentRejection);
            SetButtonLabel(detentionButton, "피검자 구금", selectedDisposition == CaseDisposition.Detention);
            SetButtonLabel(treatmentButton, "치료 전환", selectedDisposition == CaseDisposition.TreatmentReferral);
        }

        private void RenderDocuments()
        {
            if (currentCitizen == null)
            {
                return;
            }

            var features = GetActiveFeatures();
            var showIdCard = currentDayPlan == null || (features & DayFeatureFlags.CitizenIdCard) != 0;
            var showPaper = currentDayPlan == null || HasAnyPaperFeature(features);
            idCardPanel?.SetActive(showIdCard);
            paperPanel?.SetActive(showPaper);

            SetButtonLabel(displayButton, "검색 단말기");
            if (idCardText != null && showIdCard)
            {
                idCardText.text = CitizenDocumentFormatter.FormatIdCard(currentCitizen);
            }

            if (paperText != null && showPaper)
            {
                paperText.text = currentDayPlan == null && currentCase != null
                    ? CitizenDocumentFormatter.FormatLegacyApplication(currentCase)
                    : CitizenDocumentFormatter.FormatPaper(currentCitizen, features);
            }
        }

        private void LoadNextCaseOrFinish()
        {
            var nextIndex = currentCaseIndex + 1;
            if (nextIndex < runtimeQueue.Count)
            {
                LoadCase(nextIndex);
                return;
            }

            state = ExaminationState.Complete;
            SetStatus($"배정된 시민 {runtimeQueue.Count}명의 심사가 모두 끝났습니다.");
            SetButtonLabel(stampButton, "심사 완료");
            SetChoiceButtonsInteractable(false);
            if (displayButton != null) displayButton.interactable = false;
            if (stampButton != null) stampButton.interactable = false;
            if (currentDayPlan != null)
            DayCompleted?.Invoke(currentDayPlan);

            // 엔딩이 확정되었다면 다음 날로 이동하지 않음
            var endingManager = GameManager.Instance != null
                ? GameManager.Instance.GetComponent<EndingManager>()
                : null;

            if (endingManager != null && endingManager.HasResolvedEnding)
            {
                Debug.Log("엔딩이 확정되어 다음 DAY 진행을 중단합니다.");
                return;
            }

            // 백색서고 최종 제안의 수락 또는 거절을 기다림
            var progress = CampaignManager.Instance != null
                ? CampaignManager.Instance.GetComponent<StoryProgressController>()
                : null;

            if (progress != null && progress.WaitingForWhiteFinalOffer)
            {
                Debug.Log("DAY 13 종료: 백색서고 최종 제안 선택 대기");
                return;
            }


            // 수동 사건 큐는 자동 DAY 진행 대상이 아니다.
            if (currentDayPlan == null || GameManager.Instance == null)
            {
                return;
            }

            if (GameManager.Instance.IsGameOver())
            {
                Debug.Log("게임 오버: DAY 진행 중단");
                return;
            }

            // 최종일 완료 후에는 18일을 다시 로드하지 않는다.
            if (GameManager.Instance.currentDay >= 18)
            {
                Debug.Log("DAY 18 완료: 최종 엔딩 판정 대기");
                return;
            }

            GameManager.Instance.GoToNextDay();

            if (CampaignManager.Instance != null)
            {
                CampaignManager.Instance.LoadCurrentDay();
            }
        }

        private void ShowNoCaseState(string message = null)
        {
            ResetCurrentCitizen();
            state = ExaminationState.Waiting;
            dialogueController?.StopDialogue();

            if (citizenImage != null) citizenImage.enabled = false;
            idCardPanel?.SetActive(false);
            paperPanel?.SetActive(false);
            SetChoiceButtonsInteractable(false);
            if (displayButton != null) displayButton.interactable = false;
            if (stampButton != null) stampButton.interactable = false;
            SetStatus(string.IsNullOrWhiteSpace(message) ? "심사할 시민 데이터가 없습니다." : message);
        }

        private void ResetCurrentCitizen()
        {
            currentCase = null;
            currentCitizen = null;
            currentCaseIndex = -1;
            state = ExaminationState.Waiting;
        }

        private bool CanUseSearchTerminal()
        {
            return currentDayPlan == null || currentDayPlan.Definition.HasFeature(DayFeatureFlags.SearchTerminal);
        }

        private DayFeatureFlags GetActiveFeatures()
        {
            return currentDayPlan?.Definition.UnlockedFeatures ??
                   (DayFeatureFlags.CitizenIdCard |
                    DayFeatureFlags.RemovalApplication |
                    DayFeatureFlags.SearchTerminal);
        }

        private void SetDisplayButtonInteractable()
        {
            if (displayButton != null)
            {
                displayButton.interactable =
                    (state == ExaminationState.Conversing || state == ExaminationState.Inspecting) &&
                    CanUseSearchTerminal();
            }
        }

        private void SetProgressStatus(string message)
        {
            if (runtimeQueue.Count > 0 && currentCaseIndex >= 0)
            {
                SetStatus($"처리 현황 {currentCaseIndex + 1} / {runtimeQueue.Count}\n{message}");
                return;
            }

            SetStatus(message);
        }

        private static string BuildPlanFailureMessage(DayPlanBuildResult result)
        {
            var builder = new StringBuilder("DAY 생성에 실패했습니다.");
            if (result == null)
            {
                return builder.ToString();
            }

            foreach (var issue in result.Issues)
            {
                if (issue.Severity == CaseValidationSeverity.Error)
                {
                    builder.Append($"\n- {issue.Message}");
                }
            }

            return builder.ToString();
        }

        private static bool HasAnyPaperFeature(DayFeatureFlags features)
        {
            const DayFeatureFlags paperFeatures =
                DayFeatureFlags.RemovalApplication |
                DayFeatureFlags.DoctorPrescription |
                DayFeatureFlags.MemoryStructureReport |
                DayFeatureFlags.RelatedPersonConsent;
            return (features & paperFeatures) != 0;
        }

        private void ResolveSceneReferences()
        {
            citizenImage ??= FindChildComponent<Image>("Person");
            idCardPanel ??= FindChild("IDcard");
            paperPanel ??= FindChild("Paper");
            displayButton ??= FindChildComponent<Button>("Btn_Display");
            stampButton ??= FindChildComponent<Button>("Btn_Stamp");
            if (dialogueController == null) dialogueController = GetComponent<CaseDialogueController>();
            idCardText ??= FindChildComponent<TMP_Text>("IDcard/IDCardText");
            paperText ??= FindChildComponent<TMP_Text>("Paper/PaperText");
            statusText ??= FindChildComponent<TMP_Text>("StatusText");
            approvalButton ??= FindChildComponent<Button>("DispositionPanel/Btn_Approve");
            rejectionButton ??= FindChildComponent<Button>("DispositionPanel/Btn_Reject");
            detentionButton ??= FindChildComponent<Button>("DispositionPanel/Btn_Detain");
            treatmentButton ??= FindChildComponent<Button>("DispositionPanel/Btn_Treatment");
        }

        private void ApplyRuntimeFont()
        {
            var fontToUse = fontOverride;
            var isWindows = Application.platform == RuntimePlatform.WindowsEditor ||
                            Application.platform == RuntimePlatform.WindowsPlayer;
            if (fontToUse == null && useWindowsSystemFontFallback && isWindows)
            {
                try
                {
                    generatedRuntimeFont = TMP_FontAsset.CreateFontAsset(windowsSystemFontFamily, "Regular", 90);
                    if (generatedRuntimeFont != null)
                    {
                        generatedRuntimeFont.name = $"Runtime {windowsSystemFontFamily}";
                        generatedRuntimeFont.hideFlags = HideFlags.DontSave;
                        generatedRuntimeFont.atlasPopulationMode = AtlasPopulationMode.Dynamic;
                        generatedRuntimeFont.isMultiAtlasTexturesEnabled = true;
                        fontToUse = generatedRuntimeFont;
                    }
                }
                catch (Exception exception)
                {
                    Debug.LogWarning($"시스템 한글 폰트를 만들지 못했습니다: {exception.Message}", this);
                }
            }

            if (fontToUse == null)
            {
                return;
            }

            foreach (var text in GetComponentsInChildren<TMP_Text>(true))
            {
                text.font = fontToUse;
            }
        }

        private GameObject FindChild(string path)
        {
            var child = transform.Find(path);
            return child != null ? child.gameObject : null;
        }

        private T FindChildComponent<T>(string path) where T : Component
        {
            var child = transform.Find(path);
            return child != null ? child.GetComponent<T>() : null;
        }

        private void AddButtonListeners()
        {
            displayButton?.onClick.AddListener(OpenDisplay);
            stampButton?.onClick.AddListener(StampSelectedDisposition);
            approvalButton?.onClick.AddListener(SelectExtractionApproval);
            rejectionButton?.onClick.AddListener(SelectDocumentRejection);
            detentionButton?.onClick.AddListener(SelectDetention);
            treatmentButton?.onClick.AddListener(SelectTreatmentReferral);
        }

        private void RemoveButtonListeners()
        {
            displayButton?.onClick.RemoveListener(OpenDisplay);
            stampButton?.onClick.RemoveListener(StampSelectedDisposition);
            approvalButton?.onClick.RemoveListener(SelectExtractionApproval);
            rejectionButton?.onClick.RemoveListener(SelectDocumentRejection);
            detentionButton?.onClick.RemoveListener(SelectDetention);
            treatmentButton?.onClick.RemoveListener(SelectTreatmentReferral);
        }

        private void SetChoiceButtonsInteractable(bool interactable)
        {
            if (approvalButton != null) approvalButton.interactable = interactable;
            if (rejectionButton != null) rejectionButton.interactable = interactable;
            if (detentionButton != null) detentionButton.interactable = interactable;
            if (treatmentButton != null) treatmentButton.interactable = interactable;
        }

        private void SetStatus(string message)
        {
            if (statusText != null)
            {
                statusText.text = message;
            }
        }

        private static void SetButtonLabel(Button button, string label, bool selected = false)
        {
            if (button == null)
            {
                return;
            }

            var text = button.GetComponentInChildren<TMP_Text>(true);
            if (text != null)
            {
                text.text = selected ? $"▶ {label}" : label;
            }
        }

        private static string GetDispositionLabel(CaseDisposition disposition)
        {
            return disposition switch
            {
                CaseDisposition.ExtractionApproval => "추출 승인",
                CaseDisposition.DocumentRejection => "서류 반려",
                CaseDisposition.Detention => "피검자 구금",
                CaseDisposition.TreatmentReferral => "치료 전환",
                _ => disposition.ToString()
            };
        }

        private enum ExaminationState
        {
            Waiting,
            Conversing,
            Inspecting,
            Result,
            Complete
        }
    }
}
