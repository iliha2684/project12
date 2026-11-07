using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Reflection;
using ProjectED.Examination;
using ProjectED.Cases;
using ProjectED.Days;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 기억관리국 세이브 슬롯 3개. 기존 DAY/NPC/엔딩 코드를 수정하지 않는다.
/// 기록되는 것: GameManager 상태, NPC/이벤트 진행 플래그, 검색 기록, 심사 위치.
/// Unity 오브젝트 참조나 디자인 설정값은 저장하지 않는다.
/// </summary>
[DisallowMultipleComponent]
public sealed class MemoryOfficeSaveLoadUI : MonoBehaviour
{
    [Serializable]
    private sealed class SavedField
    {
        public string owner;
        public string name;
        public string kind;
        public string value;
        public List<string> values = new List<string>();
    }

    [Serializable]
    private sealed class SavedValue
    {
        public int field;
        public string kind = "";
        public string value = "";
    }

    [Serializable]
    private sealed class SavedCondition
    {
        public string poolId = "";
        public int category;
        public string optionId = "";
        public string displayName = "";
    }

    [Serializable]
    private sealed class SavedCitizen
    {
        public string id = "";
        public bool isStoryCase;
        public bool isFake;
        public string sourceCaseName = "";
        public string fakeConditionId = "";
        public int expectedDisposition;
        public string judgmentReason = "";
        public List<SavedValue> values = new List<SavedValue>();
        public List<SavedValue> originalValues = new List<SavedValue>();
        public List<string> tags = new List<string>();
        public List<SavedCondition> selectedConditions = new List<SavedCondition>();
    }

    [Serializable]
    private sealed class SaveFile
    {
        public int version = 2;
        public string savedAtUtc = "";
        public int day;
        public int citizenPosition;
        public string examinationState = "";
        public string queueSignature = "";
        public bool hadAddict;
        public bool hadPriest;
        public bool hadWhiteAgent;
        public bool hadQuackDoctor;
        public List<SavedField> fields = new List<SavedField>();
        public List<SavedCitizen> citizens = new List<SavedCitizen>();
    }

    [SerializeField] private Button saveButton;
    [SerializeField] private Button loadButton;
    [SerializeField] private TMP_Text feedbackText;
    [SerializeField] private Button slot1Button;
    [SerializeField] private Button slot2Button;
    [SerializeField] private Button slot3Button;
    [SerializeField] private TMP_Text slotInfoText;
    [SerializeField] private GameObject slotDialog;
    [SerializeField] private TMP_Text slotDialogTitle;
    [SerializeField] private TMP_Text slotDialogHint;
    [SerializeField] private Button closeDialogButton;
    [SerializeField, Range(1, 3)] private int selectedSlot = 1;

    private enum SlotMode { None, Save, Load }
    private SlotMode currentMode;
    private bool lastOperationSucceeded;

    private static readonly BindingFlags FieldFlags =
        BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly;

    private static readonly Dictionary<string, string[]> ExplicitFields =
        new Dictionary<string, string[]>
        {
            { "GameManager", new[] { "lastMerchantProcessedDay", "lastReviewedDay", "lastTerminalClueDay" } },
            { "CampaignManager", new[] { "lastPriestRecordedDay", "lastDoctorRecordedDay" } },
            { "DayEventManager", new[] { "activeDay", "merchantOfferPending", "addictTriggeredToday",
                "day6AngerTriggered", "addictEncounterCount", "totalCitizens", "merchantTriggeredToday" } },
            { "StoryProgressController", new[] { "<ActiveDay>k__BackingField",
                "<OfferedWhiteMission>k__BackingField", "<LastReviewOutcome>k__BackingField",
                "lastReviewProcessedDay", "day13CompletionHandled" } },
            { "LateDayEventController", new[] { "lastDaySeen", "directorArrived",
                "deputyAnnouncedDirector", "agencyAgentWarned", "lateRuleViolations",
                "researchedTopics", "processedVerdicts" } },
            { "EndingManager", new[] { "resolvedEnding", "resolutionReason", "quackDoctorFatal",
                "illegalContactDiscovered", "day13ReviewCompleted" } }
        };

    private const int SlotCount = 3;
    private string LegacySavePath => Path.Combine(Application.persistentDataPath, "memory_office_save_v1.json");

    private string SlotPath(int slot)
    {
        return Path.Combine(Application.persistentDataPath,
            "memory_office_save_slot_" + Mathf.Clamp(slot, 1, SlotCount) + "_v1.json");
    }

    private string ExistingSlotPath(int slot)
    {
        string path = SlotPath(slot);
        // 이전 단일 저장 파일도 슬롯 1에서 불러올 수 있도록 유지한다.
        return !File.Exists(path) && slot == 1 && File.Exists(LegacySavePath)
            ? LegacySavePath : path;
    }

    // 저장/불러오기 버튼은 팝업만 열고, 팝업에서 슬롯을 클릭하면 작업한다.
    public void OpenSaveSlots() { OpenDialog(SlotMode.Save); }
    public void OpenLoadSlots() { OpenDialog(SlotMode.Load); }

    private void OpenDialog(SlotMode mode)
    {
        currentMode = mode;
        if (slotDialogTitle != null)
            slotDialogTitle.text = mode == SlotMode.Save ? "저장할 슬롯 선택" : "불러올 슬롯 선택";
        if (slotDialogHint != null)
            slotDialogHint.text = mode == SlotMode.Save
                ? "슬롯을 누르면 해당 위치에 저장합니다. 기존 저장은 덮어써집니다."
                : "불러올 기록을 선택하세요. 빈 슬롯은 선택할 수 없습니다.";
        if (slotDialog != null) slotDialog.SetActive(true);
        RefreshSlotDisplay();
    }

    public void CloseSlotDialog()
    {
        currentMode = SlotMode.None;
        if (slotDialog != null) slotDialog.SetActive(false);
    }

    public void SelectSlot1() { SelectSlot(1); }
    public void SelectSlot2() { SelectSlot(2); }
    public void SelectSlot3() { SelectSlot(3); }

    // 이전 버전과의 호환성을 위해 슬롯 번호 설정용 함수는 유지한다.
    public void SelectSlot(int slot)
    {
        selectedSlot = Mathf.Clamp(slot, 1, SlotCount);
        RefreshSlotDisplay();
    }

    public void ChooseSlot1() { ChooseSlot(1); }
    public void ChooseSlot2() { ChooseSlot(2); }
    public void ChooseSlot3() { ChooseSlot(3); }

    private void ChooseSlot(int slot)
    {
        if (currentMode == SlotMode.None) return;
        SelectSlot(slot);
        SlotMode mode = currentMode;
        lastOperationSucceeded = false;
        if (mode == SlotMode.Save) SaveGame();
        else if (mode == SlotMode.Load) LoadGame();
        if (lastOperationSucceeded) CloseSlotDialog();
        else RefreshSlotDisplay();
    }

    private bool TryReadSlot(int slot, out SaveFile data, out string dateLabel)
    {
        data = null;
        dateLabel = "";
        string path = ExistingSlotPath(slot);
        if (!File.Exists(path)) return false;
        try
        {
            data = JsonUtility.FromJson<SaveFile>(File.ReadAllText(path, System.Text.Encoding.UTF8));
            dateLabel = File.GetLastWriteTime(path).ToString("MM.dd HH:mm", CultureInfo.InvariantCulture);
            return data != null && (data.version == 1 || data.version == 2) &&
                data.day >= 1 && data.day <= 17;
        }
        catch (Exception)
        {
            data = null;
            return false;
        }
    }

    private void RefreshSlotDisplay()
    {
        Button[] buttons = { slot1Button, slot2Button, slot3Button };
        for (int i = 0; i < buttons.Length; i++)
        {
            Button button = buttons[i];
            if (button == null) continue;
            int slot = i + 1;
            string path = ExistingSlotPath(slot);
            bool exists = File.Exists(path);
            SaveFile data;
            string dateLabel;
            bool valid = TryReadSlot(slot, out data, out dateLabel);
            string description = !exists ? "빈 슬롯"
                : valid ? "DAY " + data.day + " · 시민 " + data.citizenPosition + " · " + dateLabel
                : "손상되었거나 지원하지 않는 기록";
            TMP_Text label = button.GetComponentInChildren<TMP_Text>(true);
            if (label != null) label.text = "슬롯 " + slot + "   |   " + description;
            button.interactable = currentMode != SlotMode.Load || valid;
            Image image = button.GetComponent<Image>();
            if (image != null)
                image.color = currentMode == SlotMode.Load && !valid
                    ? new Color(0.18f, 0.18f, 0.18f, 0.96f)
                    : new Color(0.19f, 0.29f, 0.25f, 0.98f);
        }
        if (slotInfoText != null)
            slotInfoText.text = "선택한 슬롯: " + selectedSlot;
    }

    private void OnEnable()
    {
        if (saveButton != null) saveButton.onClick.AddListener(OpenSaveSlots);
        if (loadButton != null) loadButton.onClick.AddListener(OpenLoadSlots);
        if (slot1Button != null) slot1Button.onClick.AddListener(ChooseSlot1);
        if (slot2Button != null) slot2Button.onClick.AddListener(ChooseSlot2);
        if (slot3Button != null) slot3Button.onClick.AddListener(ChooseSlot3);
        if (closeDialogButton != null) closeDialogButton.onClick.AddListener(CloseSlotDialog);
        CloseSlotDialog();
        RefreshSlotDisplay();
    }

    private void OnDisable()
    {
        if (saveButton != null) saveButton.onClick.RemoveListener(OpenSaveSlots);
        if (loadButton != null) loadButton.onClick.RemoveListener(OpenLoadSlots);
        if (slot1Button != null) slot1Button.onClick.RemoveListener(ChooseSlot1);
        if (slot2Button != null) slot2Button.onClick.RemoveListener(ChooseSlot2);
        if (slot3Button != null) slot3Button.onClick.RemoveListener(ChooseSlot3);
        if (closeDialogButton != null) closeDialogButton.onClick.RemoveListener(CloseSlotDialog);
    }

    public void SaveGame()
    {
        lastOperationSucceeded = false;
        GameManager game = GameManager.Instance;
        CampaignManager campaign = CampaignManager.Instance;
        ExaminationController examination = campaign != null ? campaign.examinationController : null;
        if (game == null || campaign == null || examination == null)
        {
            Inform("저장 실패: 게임 시스템을 찾을 수 없습니다.", true);
            return;
        }

        EndingManager ending = game.GetComponent<EndingManager>();
        if ((ending != null && ending.HasResolvedEnding) || game.IsGameOver() || game.currentDay >= 18)
        {
            Inform("엔딩 확정 후에는 저장할 수 없습니다.", true);
            return;
        }

        if (examination.CurrentDayPlan == null || examination.TotalCitizenCount <= 0)
        {
            Inform("저장 실패: 현재 DAY의 시민이 아직 준비되지 않았습니다.", true);
            return;
        }

        var data = new SaveFile
        {
            day = game.currentDay,
            savedAtUtc = DateTime.UtcNow.ToString("o", CultureInfo.InvariantCulture),
            citizenPosition = examination.CurrentCitizenPosition,
            examinationState = ReadExaminationState(examination),
            queueSignature = BuildQueueSignature(examination),
            hadAddict = QueueHasCase(examination, campaign.GetComponent<DayEventManager>() != null
                ? campaign.GetComponent<DayEventManager>().AddictCase : null),
            hadPriest = QueueHasCase(examination, PrivateCase(campaign, "priestCase")) ||
                QueueHasCase(examination, PrivateCase(campaign, "priestDay12Case")),
            hadWhiteAgent = QueueHasCase(examination, PrivateCase(campaign, "whiteAgentCase")),
            hadQuackDoctor = QueueHasCase(examination, PrivateCase(campaign, "quackDoctorCase"))
        };
        foreach (var entry in EnumerateOwners(game, campaign))
            Capture(entry.Key, entry.Value, data);

        // v2 saves preserve all generated document values rather than relying on
        // re-running the procedural generator after loading a save slot.
        if (!CaptureCitizens(examination, data, out string captureError))
        {
            Inform("저장 실패: " + captureError, true);
            return;
        }

        try
        {
            string path = SlotPath(selectedSlot);
            string json = JsonUtility.ToJson(data, true);
            // 다른 슬롯에 손대지 않고 선택 슬롯만 교체한다.
            // 실패 시 백업으로 복구할 수 있도록 임시 파일과 백업을 사용한다.
            string temporary = path + ".tmp";
            string backup = path + ".bak";
            File.WriteAllText(temporary, json, System.Text.Encoding.UTF8);
            if (File.Exists(backup)) File.Delete(backup);
            if (File.Exists(path)) File.Move(path, backup);
            try { File.Move(temporary, path); }
            catch
            {
                if (File.Exists(backup) && !File.Exists(path)) File.Move(backup, path);
                throw;
            }
            Inform("슬롯 " + selectedSlot + " 저장 완료 · DAY " + data.day +
                " / 시민 " + data.citizenPosition);
            lastOperationSucceeded = true;
            RefreshSlotDisplay();
        }
        catch (Exception exception)
        {
            Inform("저장 실패: " + exception.Message, true);
        }
    }

    public void LoadGame()
    {
        lastOperationSucceeded = false;
        GameManager game = GameManager.Instance;
        CampaignManager campaign = CampaignManager.Instance;
        ExaminationController examination = campaign != null ? campaign.examinationController : null;
        if (game == null || campaign == null || examination == null)
        {
            Inform("불러오기 실패: SampleScene에서 실행하세요.", true);
            return;
        }
        string savePath = ExistingSlotPath(selectedSlot);
        if (!File.Exists(savePath))
        {
            Inform("슬롯 " + selectedSlot + "에 저장된 게임이 없습니다.", true);
            return;
        }

        SaveFile data;
        try
        {
            data = JsonUtility.FromJson<SaveFile>(File.ReadAllText(savePath, System.Text.Encoding.UTF8));
            if (data == null || (data.version != 1 && data.version != 2) ||
                data.day < 1 || data.day > 17 ||
                data.fields == null || data.fields.Count == 0)
            {
                Inform("불러오기 실패: 지원하지 않는 저장 파일입니다.", true);
                return;
            }
        }
        catch (Exception exception)
        {
            Inform("저장 파일 읽기 실패: " + exception.Message, true);
            return;
        }

        if (data.version == 2 && (data.citizens == null || data.citizens.Count == 0))
        {
            Inform("불러오기 실패: 저장된 시민 서류 정보가 없습니다.", true);
            return;
        }

        // 상태를 우선 복원해야 NPC 배치와 특수 분기가 저장 당시와 동일해진다.
        RestoreAll(data, game, campaign);
        game.currentDay = data.day;
        // 복원된 판정 기록은 같은 날 NPC의 배치 조건을 변화시킬 수 있다.
        // 재생성 중에만 처음 배치된 NPC가 다시 등장하도록 일시적으로 보정한다.
        ApplyOriginalScheduleFlags(data, game);
        campaign.LoadCurrentDay();
        if (examination.CurrentDayPlan == null || examination.DayDefinition == null ||
            examination.DayDefinition.DayNumber != data.day || examination.TotalCitizenCount == 0)
        {
            Inform("불러오기 실패: DAY " + data.day + " 시민 생성에 실패했습니다. Console을 확인하세요.", true);
            return;
        }

        if (!string.Equals(BuildQueueSignature(examination), data.queueSignature,
            StringComparison.Ordinal))
        {
            Inform("불러오기 실패: 저장 당시의 NPC 대기열과 다릅니다. " +
                "진행을 중단하고 Play를 다시 시작해 주세요.", true);
            return;
        }

        // Old saves have no per-citizen snapshots: support them with a warning.
        // New saves restore every recorded document value and fake-condition verdict.
        if (data.version == 2)
        {
            if (!RestoreCitizens(examination, data, out string restoreError))
            {
                Inform("불러오기 실패: " + restoreError +
                    " Play를 다시 시작해 주세요.", true);
                return;
            }

            // DAY 10 archive references actual generated citizen numbers. Rebuild
            // it after restoring saved citizen fields so searches and verdicts agree.
            if (data.day == 10)
            {
                Day10ArchiveRecords archive = game.GetComponent<Day10ArchiveRecords>();
                if (archive == null || !archive.PrepareFixedScenarios(examination.CurrentDayPlan))
                {
                    Inform("DAY 10 기록 복구에 실패했습니다. Play를 다시 시작해 주세요.", true);
                    return;
                }
            }
        }
        else
            Debug.LogWarning("[기억관리국 저장] 이전 v1 저장은 시민 서류 원본이 없어 " +
                "같은 시드로 재생성했습니다. v2 슬롯으로 다시 저장하는 것을 권장합니다.");

        // LoadCurrentDay()가 재설정하는 방문/이벤트 플래그를 저장 당시 값으로 복구한다.
        RestoreAll(data, game, campaign);
        game.currentDay = data.day;
        RestoreExamination(examination, campaign, data);
        Inform("슬롯 " + selectedSlot + " 불러오기 완료 · DAY " + data.day);
        lastOperationSucceeded = true;
        RefreshSlotDisplay();
    }

    private static bool CaptureCitizens(ExaminationController examination,
        SaveFile data, out string error)
    {
        error = "";
        foreach (CitizenCaseInstance citizen in examination.CurrentDayPlan.Citizens)
        {
            if (citizen == null)
            {
                error = "빈 시민 데이터가 포함되어 있습니다.";
                return false;
            }
            var saved = new SavedCitizen
            {
                id = citizen.InstanceId,
                isStoryCase = citizen.IsStoryCase,
                isFake = citizen.IsFake,
                sourceCaseName = citizen.SourceCase != null ? citizen.SourceCase.name : "",
                fakeConditionId = citizen.AppliedFakeCondition != null
                    ? citizen.AppliedFakeCondition.ConditionId : "",
                expectedDisposition = (int)citizen.ExpectedDisposition,
                judgmentReason = citizen.JudgmentReason ?? ""
            };
            foreach (KeyValuePair<GeneratedCitizenField, object> entry in citizen.Values)
            {
                if (!EncodeValue(entry.Key, entry.Value, out SavedValue value))
                {
                    error = citizen.InstanceId + "의 서류 필드를 저장할 수 없습니다: " + entry.Key;
                    return false;
                }
                saved.values.Add(value);
            }
            foreach (KeyValuePair<GeneratedCitizenField, object> entry in citizen.OriginalValues)
            {
                if (!EncodeValue(entry.Key, entry.Value, out SavedValue value))
                {
                    error = citizen.InstanceId + "의 원본 서류를 저장할 수 없습니다: " + entry.Key;
                    return false;
                }
                saved.originalValues.Add(value);
            }
            foreach (string tag in citizen.Tags) saved.tags.Add(tag);
            foreach (SelectedCitizenCondition condition in citizen.SelectedConditions)
            {
                saved.selectedConditions.Add(new SavedCondition
                {
                    poolId = condition.PoolId ?? "",
                    category = (int)condition.Category,
                    optionId = condition.OptionId ?? "",
                    displayName = condition.DisplayName ?? ""
                });
            }
            data.citizens.Add(saved);
        }
        return true;
    }

    private static bool EncodeValue(GeneratedCitizenField field, object raw,
        out SavedValue saved)
    {
        saved = new SavedValue { field = (int)field };
        if (raw == null) saved.kind = "null";
        else if (raw is string str) { saved.kind = "string"; saved.value = str; }
        else if (raw is int integer) { saved.kind = "int"; saved.value = integer.ToString(CultureInfo.InvariantCulture); }
        else if (raw is bool boolean) { saved.kind = "bool"; saved.value = boolean ? "true" : "false"; }
        else if (raw is float number) { saved.kind = "float"; saved.value = number.ToString("R", CultureInfo.InvariantCulture); }
        else if (raw is Sprite sprite)
        {
            // Unity sprite references are not JSON assets. Keep the regenerated
            // sprite reference rather than trying to serialize a fragile GUID.
            saved.kind = "sprite";
            saved.value = sprite.name;
        }
        else return false;
        return true;
    }

    private static bool DecodeValues(List<SavedValue> source,
        Dictionary<GeneratedCitizenField, object> regenerated,
        out Dictionary<GeneratedCitizenField, object> result)
    {
        result = new Dictionary<GeneratedCitizenField, object>();
        if (source == null) return false;
        foreach (SavedValue saved in source)
        {
            if (saved == null || !Enum.IsDefined(typeof(GeneratedCitizenField), saved.field))
                return false;
            GeneratedCitizenField field = (GeneratedCitizenField)saved.field;
            try
            {
                switch (saved.kind)
                {
                    case "null": result[field] = null; break;
                    case "string": result[field] = saved.value ?? ""; break;
                    case "int": result[field] = int.Parse(saved.value, CultureInfo.InvariantCulture); break;
                    case "bool": result[field] = saved.value == "true"; break;
                    case "float": result[field] = float.Parse(saved.value, CultureInfo.InvariantCulture); break;
                    case "sprite":
                        if (!regenerated.TryGetValue(field, out object originalSprite) ||
                            !(originalSprite is Sprite sprite) || sprite.name != saved.value)
                            return false;
                        result[field] = originalSprite;
                        break;
                    default: return false;
                }
            }
            catch (Exception) { return false; }
        }
        return true;
    }

    private static bool RestoreCitizens(ExaminationController examination,
        SaveFile data, out string error)
    {
        error = "";
        var citizens = examination.CurrentDayPlan.Citizens;
        if (data.citizens.Count != citizens.Count)
        {
            error = "저장된 시민 수와 현재 시민 수가 다릅니다.";
            return false;
        }
        // Validate the entire saved queue BEFORE applying any document updates.
        var restoredValues = new List<Dictionary<GeneratedCitizenField, object>>();
        var restoredOriginals = new List<Dictionary<GeneratedCitizenField, object>>();
        var fakeConditions = new List<FakeConditionData>();
        for (int i = 0; i < citizens.Count; i++)
        {
            CitizenCaseInstance citizen = citizens[i];
            SavedCitizen saved = data.citizens[i];
            if (saved == null || citizen == null || saved.id != citizen.InstanceId ||
                saved.isStoryCase != citizen.IsStoryCase ||
                saved.sourceCaseName != (citizen.SourceCase != null ? citizen.SourceCase.name : "") ||
                !Enum.IsDefined(typeof(CaseDisposition), saved.expectedDisposition))
            {
                error = (i + 1) + "번째 시민의 NPC 정보가 저장 당시와 다릅니다.";
                return false;
            }
            FakeConditionData fake = null;
            if (!string.IsNullOrEmpty(saved.fakeConditionId))
            {
                foreach (FakeConditionData item in examination.DayDefinition.AvailableFakeConditions)
                    if (item != null && item.ConditionId == saved.fakeConditionId)
                    {
                        fake = item;
                        break;
                    }
                if (fake == null)
                {
                    error = "가짜 조건 에셋을 찾지 못했습니다: " + saved.fakeConditionId;
                    return false;
                }
            }
            // Prevent accidentally treating a story fake as a generated fake.
            if (!saved.isStoryCase && saved.isFake != (fake != null))
            {
                error = "가짜 시민 설정이 맞지 않습니다: " + saved.id;
                return false;
            }
            var currentValues = new Dictionary<GeneratedCitizenField, object>();
            foreach (var entry in citizen.Values) currentValues[entry.Key] = entry.Value;
            if (!DecodeValues(saved.values, currentValues, out var values) ||
                !DecodeValues(saved.originalValues, currentValues, out var originals))
            {
                error = "서류 데이터 형식이 변경되었거나 원본 이미지가 달라졌습니다: " + saved.id;
                return false;
            }
            restoredValues.Add(values);
            restoredOriginals.Add(originals);
            fakeConditions.Add(fake);
        }
        for (int i = 0; i < citizens.Count; i++)
        {
            SavedCitizen saved = data.citizens[i];
            var conditions = new List<SelectedCitizenCondition>();
            if (saved.selectedConditions != null)
            {
                foreach (SavedCondition item in saved.selectedConditions)
                    conditions.Add(new SelectedCitizenCondition(item.poolId,
                        (CitizenConditionCategory)item.category, item.optionId, item.displayName));
            }
            citizens[i].RestoreSavedDocuments(restoredValues[i], restoredOriginals[i],
                saved.tags, conditions, saved.isFake, fakeConditions[i],
                (CaseDisposition)saved.expectedDisposition, saved.judgmentReason);
        }
        return true;
    }

    private static ProjectED.Cases.CitizenCaseData PrivateCase(CampaignManager campaign, string fieldName)
    {
        FieldInfo field = typeof(CampaignManager).GetField(fieldName, FieldFlags);
        return field != null ? field.GetValue(campaign) as ProjectED.Cases.CitizenCaseData : null;
    }

    private static bool QueueHasCase(ExaminationController examination,
        ProjectED.Cases.CitizenCaseData target)
    {
        if (target == null || examination.CurrentDayPlan == null) return false;
        foreach (var citizen in examination.CurrentDayPlan.Citizens)
            if (citizen != null && citizen.IsStoryCase && citizen.SourceCase == target) return true;
        return false;
    }

    private static string BuildQueueSignature(ExaminationController examination)
    {
        if (examination.CurrentDayPlan == null) return "";
        var items = new List<string>();
        foreach (var citizen in examination.CurrentDayPlan.Citizens)
            items.Add(citizen != null && citizen.IsStoryCase && citizen.SourceCase != null
                ? "S:" + citizen.SourceCase.name : "G");
        return string.Join("|", items.ToArray());
    }

    private static void ApplyOriginalScheduleFlags(SaveFile data, GameManager game)
    {
        if (data.hadAddict && data.day >= 6 && data.day <= 12)
        {
            game.addictMet = true;
            game.addictDetained = false;
            if (game.addictLastVisitDay >= data.day)
                game.addictLastVisitDay = data.day - 1;
        }
        if ((data.hadPriest || (data.hadWhiteAgent && data.day == 13)) &&
            data.day >= 11 && data.day <= 13)
            game.priestDetained = false;
        if (data.hadQuackDoctor && data.day == 15)
        {
            game.quackDoctorRejected = true;
            game.quackDoctorDetained = false;
        }
    }

    private static void RestoreExamination(ExaminationController examination,
        CampaignManager campaign, SaveFile data)
    {
        int count = examination.TotalCitizenCount;
        int position = Mathf.Clamp(data.citizenPosition, 1, count);
        if (data.examinationState == "Complete")
        {
            examination.LoadCase(count - 1);
            StopRestoredDialogue(examination);
            SetExaminationState(examination, "Complete");
            return; // DAY 13 백색서고 제안 팝업 등은 기존 UI가 상태를 감지한다.
        }

        examination.LoadCase(position - 1);
        if (data.examinationState == "Result")
        {
            StopRestoredDialogue(examination);
            SetExaminationState(examination, "Result");
            DayEventManager events = campaign.GetComponent<DayEventManager>();
            if (events == null || !events.merchantOfferPending)
            {
                // 이미 판정한 시민에게 재판정하지 않고 다음 시민로 이동.
                examination.StampSelectedDisposition();
            }
            // 상인 제안 대기 시에는 팝업에서 선택한 다음 기존 결과 단계가 이어진다.
        }
        // 대화 중/심사 중에 저장한 경우 해당 시민의 대화부터 재시작한다.
        // 원래 심사 결과는 아직 기록되지 않았으므로 중복 판정이 발생하지 않는다.
    }

    private static void StopRestoredDialogue(ExaminationController examination)
    {
        CaseDialogueController dialogue = examination.GetComponent<CaseDialogueController>();
        if (dialogue == null) dialogue = examination.GetComponentInChildren<CaseDialogueController>(true);
        if (dialogue != null) dialogue.StopDialogue();
    }

    private static string ReadExaminationState(ExaminationController examination)
    {
        FieldInfo field = typeof(ExaminationController).GetField("state", FieldFlags);
        object value = field != null ? field.GetValue(examination) : null;
        return value != null ? value.ToString() : "Waiting";
    }

    private static void SetExaminationState(ExaminationController examination, string state)
    {
        FieldInfo field = typeof(ExaminationController).GetField("state", FieldFlags);
        if (field == null || !field.FieldType.IsEnum) return;
        try { field.SetValue(examination, Enum.Parse(field.FieldType, state)); }
        catch (ArgumentException) { }
    }

    private static IEnumerable<KeyValuePair<string, MonoBehaviour>> EnumerateOwners(
        GameManager game, CampaignManager campaign)
    {
        yield return new KeyValuePair<string, MonoBehaviour>("GameManager", game);
        yield return new KeyValuePair<string, MonoBehaviour>("CampaignManager", campaign);
        yield return new KeyValuePair<string, MonoBehaviour>("DayEventManager", campaign.GetComponent<DayEventManager>());
        yield return new KeyValuePair<string, MonoBehaviour>("StoryProgressController", campaign.GetComponent<StoryProgressController>());
        yield return new KeyValuePair<string, MonoBehaviour>("LateDayEventController", game.GetComponent<LateDayEventController>());
        yield return new KeyValuePair<string, MonoBehaviour>("EndingManager", game.GetComponent<EndingManager>());
    }

    private static void Capture(string owner, MonoBehaviour component, SaveFile data)
    {
        if (component == null) return;
        HashSet<string> allowed = new HashSet<string>();
        string[] listed;
        if (ExplicitFields.TryGetValue(owner, out listed))
            foreach (string name in listed) allowed.Add(name);

        foreach (FieldInfo field in component.GetType().GetFields(FieldFlags))
        {
            if (field.IsStatic) continue;
            if (!(owner == "GameManager" && field.IsPublic) && !allowed.Contains(field.Name))
                continue;
            object value = field.GetValue(component);
            if (value == null && field.FieldType != typeof(string)) continue;
            var record = new SavedField { owner = owner, name = field.Name };
            if (field.FieldType == typeof(int))
            {
                record.kind = "int";
                record.value = ((int)value).ToString(CultureInfo.InvariantCulture);
            }
            else if (field.FieldType == typeof(bool))
            {
                record.kind = "bool";
                record.value = ((bool)value) ? "true" : "false";
            }
            else if (field.FieldType == typeof(float))
            {
                record.kind = "float";
                record.value = ((float)value).ToString("R", CultureInfo.InvariantCulture);
            }
            else if (field.FieldType == typeof(string))
            {
                record.kind = "string";
                record.value = (string)value ?? "";
            }
            else if (field.FieldType.IsEnum)
            {
                record.kind = "enum";
                record.value = value.ToString();
            }
            else if (field.FieldType == typeof(HashSet<string>))
            {
                record.kind = "stringSet";
                record.values.AddRange((HashSet<string>)value);
            }
            else continue;
            data.fields.Add(record);
        }
    }

    private static void RestoreAll(SaveFile data, GameManager game, CampaignManager campaign)
    {
        var owners = new Dictionary<string, MonoBehaviour>();
        foreach (var entry in EnumerateOwners(game, campaign))
            if (entry.Value != null) owners[entry.Key] = entry.Value;
        foreach (SavedField record in data.fields)
        {
            MonoBehaviour component;
            if (!owners.TryGetValue(record.owner, out component)) continue;
            FieldInfo field = component.GetType().GetField(record.name, FieldFlags);
            if (field == null || field.IsStatic) continue;
            // 직접 수정 가능한 런타임 필드만 복원하고, Unity 오브젝트 참조는 유지한다.
            try
            {
                switch (record.kind)
                {
                    case "int":
                        if (field.FieldType == typeof(int))
                            field.SetValue(component, int.Parse(record.value, CultureInfo.InvariantCulture));
                        break;
                    case "bool":
                        if (field.FieldType == typeof(bool)) field.SetValue(component, record.value == "true");
                        break;
                    case "float":
                        if (field.FieldType == typeof(float))
                            field.SetValue(component, float.Parse(record.value, CultureInfo.InvariantCulture));
                        break;
                    case "string":
                        if (field.FieldType == typeof(string)) field.SetValue(component, record.value ?? "");
                        break;
                    case "enum":
                        if (field.FieldType.IsEnum)
                            field.SetValue(component, Enum.Parse(field.FieldType, record.value));
                        break;
                    case "stringSet":
                        if (field.FieldType == typeof(HashSet<string>))
                        {
                            HashSet<string> current = field.GetValue(component) as HashSet<string>;
                            if (current != null)
                            {
                                current.Clear();
                                if (record.values != null)
                                    foreach (string item in record.values) current.Add(item);
                            }
                        }
                        break;
                }
            }
            catch (Exception exception)
            {
                Debug.LogWarning("[저장] 복원할 수 없는 필드: " + record.owner + "." + record.name +
                                 " (" + exception.Message + ")");
            }
        }
    }

    private void Inform(string message, bool isError = false)
    {
        if (feedbackText != null) feedbackText.text = message;
        if (isError && slotDialogHint != null && slotDialog != null && slotDialog.activeSelf)
            slotDialogHint.text = message;
        if (isError) Debug.LogWarning("[기억관리국 저장] " + message);
        else Debug.Log("[기억관리국 저장] " + message);
    }
}
