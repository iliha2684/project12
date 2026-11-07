using System;
using System.Collections;
using System.Collections.Generic;
using ProjectED.Days;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace ProjectED.Examination
{
    /// <summary>
    /// A purely visual hand-off / desk-document layer. It neither generates nor edits
    /// CitizenCaseInstance, judges a citizen, or changes save data.
    /// Click a desk thumbnail to open the EXISTING IDcard/Paper detailed UI.
    /// </summary>
    public sealed class MemoryOfficeDeskDocuments : MonoBehaviour
    {
        private const int DocumentCount = 5;
        private const string PaperSeparator = "\n\n────────────\n\n";

        [SerializeField] private ExaminationController examination;
        [SerializeField] private GameObject idCardPanel;
        [SerializeField] private GameObject paperPanel;
        [SerializeField] private TMP_Text paperSourceText;
        [SerializeField] private Button[] deskButtons = new Button[DocumentCount];
        [SerializeField] private RectTransform[] deskCards = new RectTransform[DocumentCount];
        [SerializeField] private Button closeIdCardButton;
        [SerializeField] private Button closePaperButton;
        [SerializeField, Range(0.05f, 2f)] private float handoffDuration = 0.65f;

        private readonly Vector2[] landingPositions = new Vector2[DocumentCount];
        private readonly UnityAction[] clickActions = new UnityAction[DocumentCount];
        private readonly bool[] available = new bool[DocumentCount];
        private readonly int[] paperPage = new int[DocumentCount];
        private CitizenCaseInstance observedCitizen;
        private bool previousInspecting;
        private bool delivered;
        private bool detailsOpen;
        private bool initialized;
        private Coroutine handoffRoutine;

        private void Awake()
        {
            Initialize();
        }

        private void OnEnable()
        {
            Initialize();
            for (int index = 0; index < DocumentCount; index++)
            {
                if (deskButtons != null && index < deskButtons.Length && deskButtons[index] != null)
                    deskButtons[index].onClick.AddListener(clickActions[index]);
            }
            if (closeIdCardButton != null)
                closeIdCardButton.onClick.AddListener(CloseDetails);
            if (closePaperButton != null)
                closePaperButton.onClick.AddListener(CloseDetails);
            ResetForCitizen();
        }

        private void OnDisable()
        {
            if (handoffRoutine != null)
            {
                StopCoroutine(handoffRoutine);
                handoffRoutine = null;
            }
            for (int index = 0; index < DocumentCount; index++)
            {
                if (deskButtons != null && index < deskButtons.Length && deskButtons[index] != null && clickActions[index] != null)
                    deskButtons[index].onClick.RemoveListener(clickActions[index]);
            }
            if (closeIdCardButton != null)
                closeIdCardButton.onClick.RemoveListener(CloseDetails);
            if (closePaperButton != null)
                closePaperButton.onClick.RemoveListener(CloseDetails);
        }

        private void LateUpdate()
        {
            if (examination == null) return;

            CitizenCaseInstance citizen = examination.CurrentCitizen;
            bool inspecting = examination.HasActiveCase;

            // LoadDay/LoadCase and save-file loads can replace the citizen object.
            if (!ReferenceEquals(citizen, observedCitizen))
            {
                observedCitizen = citizen;
                ResetForCitizen();
                previousInspecting = false;
            }

            if (citizen == null || !inspecting)
            {
                if (previousInspecting)
                    ResetForCitizen();
                previousInspecting = false;
                return;
            }

            // The conversation has just finished: the NPC hands over documents.
            if (!previousInspecting && !delivered && handoffRoutine == null)
                BeginHandoff();

            previousInspecting = true;
        }

        private void Initialize()
        {
            if (initialized) return;
            initialized = true;
            if (examination == null)
                examination = FindFirstObjectByType<ExaminationController>();

            for (int index = 0; index < DocumentCount; index++)
            {
                int copy = index;
                clickActions[index] = () => OpenDocument(copy);
                if (deskCards != null && index < deskCards.Length && deskCards[index] != null)
                    landingPositions[index] = deskCards[index].anchoredPosition;
            }
        }

        private void ResetForCitizen()
        {
            if (handoffRoutine != null)
            {
                StopCoroutine(handoffRoutine);
                handoffRoutine = null;
            }
            delivered = false;
            detailsOpen = false;
            HideDetails();
            for (int index = 0; index < DocumentCount; index++)
            {
                available[index] = false;
                if (deskCards == null || index >= deskCards.Length || deskCards[index] == null)
                    continue;
                deskCards[index].anchoredPosition = landingPositions[index];
                deskCards[index].gameObject.SetActive(false);
                if (deskButtons != null && index < deskButtons.Length && deskButtons[index] != null)
                    deskButtons[index].interactable = false;
            }
        }

        private void BeginHandoff()
        {
            if (examination == null || examination.CurrentCitizen == null) return;
            ResolveAvailableDocuments();
            // Never show the detailed panels automatically for a new citizen.
            HideDetails();
            handoffRoutine = StartCoroutine(AnimateHandoff());
        }

        private void ResolveAvailableDocuments()
        {
            DayFeatureFlags features = examination.CurrentDayPlan != null &&
                examination.CurrentDayPlan.Definition != null
                ? examination.CurrentDayPlan.Definition.UnlockedFeatures
                : DayFeatureFlags.CitizenIdCard | DayFeatureFlags.RemovalApplication;

            DayFeatureFlags[] flags =
            {
                DayFeatureFlags.CitizenIdCard,
                DayFeatureFlags.RemovalApplication,
                DayFeatureFlags.DoctorPrescription,
                DayFeatureFlags.MemoryStructureReport,
                DayFeatureFlags.RelatedPersonConsent
            };

            var documentSections = new List<string>();
            if (paperSourceText != null && !string.IsNullOrEmpty(paperSourceText.text))
                documentSections.AddRange(paperSourceText.text.Split(
                    new[] { PaperSeparator }, StringSplitOptions.None));

            int section = 0;
            for (int i = 0; i < DocumentCount; i++)
            {
                available[i] = (features & flags[i]) != 0;
                if (i == 0) continue;

                if (available[i])
                {
                    paperPage[i] = Mathf.Clamp(section / 2, 0, 1);
                    // A physically missing document should not be handed over.
                    // The existing regulation/assessment system still detects it.
                    if (section < documentSections.Count)
                    {
                        string content = documentSections[section].TrimStart();
                        if (content.StartsWith("[", StringComparison.Ordinal) &&
                            (content.Contains("미제출") || content.Contains("데이터 없음") ||
                             content.Contains("제출 여부 미설정")))
                            available[i] = false;
                    }
                    section++;
                }
            }

            if (available[0] && examination.CurrentCitizen != null)
            {
                string id = CitizenDocumentFormatter.FormatIdCard(examination.CurrentCitizen);
                if (id.StartsWith("[", StringComparison.Ordinal) &&
                    (id.Contains("미제출") || id.Contains("데이터 없음") || id.Contains("여부 미설정")))
                    available[0] = false;
            }
        }

        private IEnumerator AnimateHandoff()
        {
            var active = new List<int>();
            for (int i = 0; i < DocumentCount; i++)
            {
                if (!available[i] || deskCards == null || i >= deskCards.Length || deskCards[i] == null)
                    continue;
                active.Add(i);
                deskCards[i].gameObject.SetActive(true);
                deskCards[i].anchoredPosition = new Vector2(0f, -90f);
                if (deskButtons != null && i < deskButtons.Length && deskButtons[i] != null)
                    deskButtons[i].interactable = false;
            }

            float duration = Mathf.Max(0.05f, handoffDuration);
            float elapsed = 0f;
            while (elapsed < duration)
            {
                if (examination == null || !examination.HasActiveCase)
                {
                    handoffRoutine = null;
                    yield break;
                }

                elapsed += Time.unscaledDeltaTime;
                for (int j = 0; j < active.Count; j++)
                {
                    int index = active[j];
                    float fraction = Mathf.Clamp01((elapsed / duration - j * 0.09f) / 0.65f);
                    float ease = fraction * fraction * (3f - 2f * fraction);
                    Vector2 start = new Vector2((j - active.Count * 0.5f) * 24f, -90f);
                    deskCards[index].anchoredPosition = Vector2.LerpUnclamped(start, landingPositions[index], ease);
                }
                yield return null;
            }

            for (int j = 0; j < active.Count; j++)
            {
                int index = active[j];
                deskCards[index].anchoredPosition = landingPositions[index];
                if (deskButtons != null && index < deskButtons.Length && deskButtons[index] != null)
                    deskButtons[index].interactable = true;
            }
            delivered = true;
            handoffRoutine = null;
        }

        public void OpenDocument(int index)
        {
            if (!delivered || examination == null || !examination.HasActiveCase ||
                index < 0 || index >= DocumentCount || !available[index])
                return;

            HideDetails();
            detailsOpen = true;
            if (index == 0)
            {
                if (idCardPanel != null)
                {
                    idCardPanel.SetActive(true);
                    idCardPanel.transform.SetAsLastSibling();
                }
            }
            else if (paperPanel != null)
            {
                paperPanel.SetActive(true);
                var pager = paperPanel.GetComponent<MemoryOfficeDocumentPager>();
                if (paperPage[index] == 1 && pager != null)
                    pager.NextPage();
                paperPanel.transform.SetAsLastSibling();
            }
        }

        public void CloseDetails()
        {
            detailsOpen = false;
            HideDetails();
        }

        private void HideDetails()
        {
            if (idCardPanel != null && idCardPanel.activeSelf)
                idCardPanel.SetActive(false);
            if (paperPanel != null && paperPanel.activeSelf)
                paperPanel.SetActive(false);
        }
    }
}
