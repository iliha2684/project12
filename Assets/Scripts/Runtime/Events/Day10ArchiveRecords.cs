using System;
using System.Collections.Generic;
using ProjectED.Cases;
using ProjectED.Days;
using UnityEngine;

// DAY 10: compares a citizen's generated data against independently registered
// death and incident records. The registry is game logic, not a UI panel.
[DisallowMultipleComponent]
public sealed class Day10ArchiveRecords : MonoBehaviour
{
    [Serializable]
    public sealed class DeathRecord
    {
        public string citizenNumber;
        public string note;
    }

    [Serializable]
    public sealed class IncidentRecord
    {
        public string citizenNumber;
        public string recordedName;
        public string recordedApplicationEvent;
    }

    [Header("생성 시민 필드명 - GeneratedCitizenField enum 이름과 동일해야 함")]
    [SerializeField] private string citizenNumberField = "IdCardCitizenNumber";
    [SerializeField] private string citizenNameField = "IdCardFullName";
    [SerializeField] private string applicationEventField = "ApplicationTargetEvent";

    [Header("DAY 10 사망자 명부")]
    [SerializeField] private List<DeathRecord> deathRecords = new List<DeathRecord>();

    [Header("DAY 10 과거 사건 기록")]
    [SerializeField] private List<IncidentRecord> incidentRecords = new List<IncidentRecord>();

    private bool warnedMissingField;
    // Runtime records generated from the final DAY 10 queue (not serialized).
    private readonly List<DeathRecord> runtimeDeathRecords = new List<DeathRecord>();
    private readonly List<IncidentRecord> runtimeIncidentRecords = new List<IncidentRecord>();
    private DayPlan preparedPlan;

    public bool HasPreparedDay10Records => preparedPlan != null;

    public bool PrepareFixedScenarios(DayPlan plan)
    {
        runtimeDeathRecords.Clear();
        runtimeIncidentRecords.Clear();
        preparedPlan = null;

        if (plan == null || plan.Definition == null ||
            plan.Definition.DayNumber != 10 || plan.Definition.TotalCitizenCount != 8)
        {
            Debug.LogError("DAY 10: 기록 자동 생성에 필요한 8명의 시민이 없습니다.");
            return false;
        }

        for (int position = 2; position <= 8; position += 2)
        {
            CitizenCaseInstance citizen = plan.Citizens[position - 1];
            if (citizen == null || citizen.IsStoryCase ||
                !TryReadField(citizen, citizenNumberField, out var number))
            {
                Debug.LogError("DAY 10: " + position +
                    "번째 시민번호를 찾지 못해 기록을 생성할 수 없습니다.");
                return false;
            }

            if (position == 2 || position == 6)
            {
                runtimeDeathRecords.Add(new DeathRecord
                {
                    citizenNumber = number,
                    note = position == 2 ? "확정 사망 등록" : "사망 기록 관련 재방문"
                });
            }
            else if (position == 4)
            {
                string currentName;
                if (!TryReadField(citizen, citizenNameField, out currentName))
                {
                    Debug.LogError("DAY 10: 4번째 시민의 이름 필드가 없습니다.");
                    return false;
                }
                runtimeIncidentRecords.Add(new IncidentRecord
                {
                    citizenNumber = number,
                    recordedName = currentName + " (과거 등록)",
                    recordedApplicationEvent = ""
                });
            }
            else // position == 8
            {
                string currentEvent;
                if (!TryReadField(citizen, applicationEventField, out currentEvent))
                {
                    Debug.LogError("DAY 10: 8번째 시민의 신청 사건 필드가 없습니다.");
                    return false;
                }
                runtimeIncidentRecords.Add(new IncidentRecord
                {
                    citizenNumber = number,
                    recordedName = "",
                    recordedApplicationEvent = currentEvent + " (과거 기록)"
                });
            }
        }

        preparedPlan = plan;
        Debug.Log("DAY 10: 2·4·6·8번째 시민의 사망/사건 기록이 자동 연결되었습니다.");
        return true;
    }

    // Old Inspector data may still contain the name used by an earlier script.
    private void OnValidate()
    {
        if (citizenNameField == "IdCardName")
            citizenNameField = nameof(GeneratedCitizenField.IdCardFullName);
    }


    public bool TryEvaluate(CitizenCaseInstance citizen, out CaseDisposition expected, out string reason)
    {
        expected = CaseDisposition.ExtractionApproval;
        reason = "";

        if (citizen == null || citizen.IsStoryCase ||
            GameManager.Instance == null || GameManager.Instance.currentDay != 10)
            return false;

        if (deathRecords.Count == 0 && incidentRecords.Count == 0 &&
            runtimeDeathRecords.Count == 0 && runtimeIncidentRecords.Count == 0)
            return false;

        string citizenNumber;
        if (!TryReadField(citizen, citizenNumberField, out citizenNumber))
        {
            if (!warnedMissingField)
            {
                warnedMissingField = true;
                Debug.LogWarning("DAY 10: 시민번호 필드를 읽지 못했습니다. " +
                    "Day10ArchiveRecords Inspector의 필드명을 GeneratedCitizenField와 대조하세요.");
            }
            return false;
        }

        foreach (var record in AllDeathRecords())
        {
            if (record != null && Same(record.citizenNumber, citizenNumber))
            {
                expected = CaseDisposition.DocumentRejection;
                reason = citizen.SequenceIndex == 6
                    ? "앞서 사망자로 등록된 시민번호로 같은 사람이 다시 방문했습니다."
                    : "사망자 명부의 시민번호가 신청자의 시민번호와 일치합니다.";
                return true;
            }
        }

        foreach (var record in AllIncidentRecords())
        {
            if (record == null || !Same(record.citizenNumber, citizenNumber))
                continue;

            string actualName;
            if (!string.IsNullOrWhiteSpace(record.recordedName) &&
                TryReadField(citizen, citizenNameField, out actualName) &&
                !Same(record.recordedName, actualName))
            {
                expected = CaseDisposition.DocumentRejection;
                reason = "과거 사건 기록과 현재 시민의 이름이 일치하지 않습니다.";
                return true;
            }

            string actualEvent;
            if (!string.IsNullOrWhiteSpace(record.recordedApplicationEvent) &&
                TryReadField(citizen, applicationEventField, out actualEvent) &&
                !Same(record.recordedApplicationEvent, actualEvent))
            {
                expected = CaseDisposition.DocumentRejection;
                reason = "과거 사건 기록과 현재 기억 제거 신청 내용이 일치하지 않습니다.";
                return true;
            }
        }

        return false;
    }

    private IEnumerable<DeathRecord> AllDeathRecords()
    {
        // Auto-generated DAY 10 entries take priority over Inspector entries.
        foreach (var record in runtimeDeathRecords) yield return record;
        foreach (var record in deathRecords) yield return record;
    }

    private IEnumerable<IncidentRecord> AllIncidentRecords()
    {
        foreach (var record in runtimeIncidentRecords) yield return record;
        foreach (var record in incidentRecords) yield return record;
    }

    // DAY 10 search terminal: read-only lookups. Runtime-generated records are included.
    // A UI must not invent archive entries or alter the automatic judgement rules.
    public bool TryFindDeathRecord(string citizenNumber, out string note)
    {
        note = null;
        if (string.IsNullOrWhiteSpace(citizenNumber)) return false;
        foreach (var record in AllDeathRecords())
        {
            if (record == null || !Same(record.citizenNumber, citizenNumber)) continue;
            note = record.note ?? string.Empty;
            return true;
        }
        return false;
    }

    public bool TryFindIncidentRecord(string citizenNumber, out string recordedName,
        out string recordedApplicationEvent)
    {
        recordedName = null;
        recordedApplicationEvent = null;
        if (string.IsNullOrWhiteSpace(citizenNumber)) return false;
        foreach (var record in AllIncidentRecords())
        {
            if (record == null || !Same(record.citizenNumber, citizenNumber)) continue;
            recordedName = record.recordedName ?? string.Empty;
            recordedApplicationEvent = record.recordedApplicationEvent ?? string.Empty;
            return true;
        }
        return false;
    }

    // Future terminal/database UI can call these methods directly.
    public void AddDeathRecord(string citizenNumber, string note = "")
    {
        if (string.IsNullOrWhiteSpace(citizenNumber)) return;
        deathRecords.Add(new DeathRecord { citizenNumber = citizenNumber.Trim(), note = note });
    }

    public void AddIncidentRecord(string citizenNumber, string recordedName,
        string recordedApplicationEvent)
    {
        if (string.IsNullOrWhiteSpace(citizenNumber)) return;
        incidentRecords.Add(new IncidentRecord
        {
            citizenNumber = citizenNumber.Trim(),
            recordedName = recordedName,
            recordedApplicationEvent = recordedApplicationEvent
        });
    }

    private static bool TryReadField(CitizenCaseInstance citizen, string name, out string value)
    {
        value = null;
        GeneratedCitizenField field;
        // Accept the earlier incorrect field spelling in saved Inspector data.
        if (string.Equals(name, "IdCardName", StringComparison.Ordinal))
            name = nameof(GeneratedCitizenField.IdCardFullName);

        if (string.IsNullOrWhiteSpace(name) ||
            !Enum.TryParse(name.Trim(), false, out field) ||
            !Enum.IsDefined(typeof(GeneratedCitizenField), field))
            return false;

        string text;
        if (citizen.TryGetValue<string>(field, out text) &&
            !string.IsNullOrWhiteSpace(text))
        {
            value = text.Trim();
            return true;
        }

        int number;
        if (citizen.TryGetValue<int>(field, out number))
        {
            value = number.ToString();
            return true;
        }

        return false;
    }

    private static bool Same(string left, string right)
    {
        return !string.IsNullOrWhiteSpace(left) && !string.IsNullOrWhiteSpace(right) &&
               string.Equals(left.Trim(), right.Trim(), StringComparison.OrdinalIgnoreCase);
    }
}
