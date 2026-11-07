using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Reflection;
using ProjectED.Cases;
using UnityEngine;

namespace ProjectED.Days
{
    // Pure game-rule results. No references to buttons, canvases, or scenes.
    public readonly struct DayRuleVerdict
    {
        public DayRuleVerdict(CaseDisposition expected, string reason, bool consentValid)
        {
            ExpectedDisposition = expected;
            Reason = reason;
            ConsentValid = consentValid;
        }

        public CaseDisposition ExpectedDisposition { get; }
        public string Reason { get; }
        public bool ConsentValid { get; }
    }

    public static class DayRuleEngine
    {
        // Based on the game's configured starting date, not the computer clock.
        private static readonly DateTime ReferenceDate = new DateTime(2048, 8, 11);

        public static DayRuleVerdict Evaluate(CitizenCaseInstance citizen, DayPlan plan)
        {
            if (citizen == null)
                return new DayRuleVerdict(CaseDisposition.ExtractionApproval, "", false);

            var original = citizen.ExpectedDisposition;
            if (plan == null || plan.Definition == null)
                return new DayRuleVerdict(original, "", false);

            int day = plan.Definition.DayNumber;

            // DAY 16: the designated pleading citizen is an exception to the
            // mandatory extraction rule. Special cases normally keep their own
            // authored verdict, so this must run BEFORE IsStoryCase returns.
            if (day == 16 && GameManager.Instance != null)
            {
                var lateEvents = GameManager.Instance.GetComponent<LateDayEventController>();
                if (lateEvents != null && lateEvents.IsPleadingCitizen(citizen))
                {
                    return new DayRuleVerdict(CaseDisposition.Detention,
                        "DAY 16 특별 대상자는 긴급 구금해야 합니다.", false);
                }
            }

            // Branching story choices must not be marked wrong merely because
            // CitizenCaseData can store only one ExpectedDisposition.
            if (citizen.IsStoryCase)
            {
                CampaignManager campaign = CampaignManager.Instance;
                if (campaign != null && day == 12 && campaign.IsPriestCase(citizen))
                {
                    return new DayRuleVerdict(CaseDisposition.DocumentRejection,
                        "DAY 12: 십자가 보관/거절/구금은 서로 다른 스토리 선택입니다.", false);
                }

                if (campaign != null && (day == 14 || day == 15) &&
                    campaign.IsQuackDoctorCase(citizen))
                {
                    return new DayRuleVerdict(CaseDisposition.Detention,
                        day == 14
                            ? "DAY 14: 돌팔이 의사는 구금하거나 반려할 수 있습니다. 추출 승인은 위험합니다."
                            : "DAY 15: 재등장한 돌팔이 의사는 긴급 구금해야 합니다.", false);
                }

                // All other story NPCs keep their authored dispositions.
                return new DayRuleVerdict(original, "", false);
            }

            // DAY 10: compare the citizen with actual configured archived records.
            // A configured DAY 10 archive contradiction takes precedence over
            // unrelated FakeCondition mutations on these four fixed citizens.
            if (day == 10 && GameManager.Instance != null)
            {
                var archive = GameManager.Instance.GetComponent<Day10ArchiveRecords>();
                if (archive != null && archive.TryEvaluate(citizen, out var archiveDisposition,
                    out var archiveReason))
                {
                    return new DayRuleVerdict(archiveDisposition, archiveReason, false);
                }
            }
            DayFeatureFlags features = plan.Definition.UnlockedFeatures;
            bool violated = false;
            string firstViolation = "";
            CaseDisposition violationDisposition = CaseDisposition.DocumentRejection;

            // The following fields are checked only if the generated citizen actually
            // contains them. Existing authored FakeCondition verdicts remain intact.
            if ((features & DayFeatureFlags.CitizenIdCard) != 0)
            {
                if (TryText(citizen, out var expiry,
                    "IdCardExpirationDate", "CitizenIdCardExpirationDate", "IdExpirationDate"))
                {
                    if (!TryDate(expiry, out var parsedDate) || parsedDate.Date < ReferenceDate)
                    {
                        RegisterViolation("신분증 유효기간이 만료되었거나 날짜 형식이 잘못되었습니다.");
                    }
                }

                if (TryText(citizen, out var idName, "IdCardFullName") &&
                    TryText(citizen, out var applicantName,
                        "ApplicationApplicantName", "ApplicationName", "ApplicantName") &&
                    !string.IsNullOrWhiteSpace(idName) && !string.IsNullOrWhiteSpace(applicantName) &&
                    !string.Equals(idName.Trim(), applicantName.Trim(), StringComparison.Ordinal))
                {
                    RegisterViolation("신분증 이름과 신청서 이름이 일치하지 않습니다.");
                }
            }

            if ((features & DayFeatureFlags.DoctorPrescription) != 0 && day >= 3)
            {
                if (TryFlag(citizen, out var prescriptionSubmitted,
                    "PrescriptionSubmitted", "DoctorPrescriptionSubmitted") && !prescriptionSubmitted)
                {
                    RegisterViolation("의사 처방전이 제출되지 않았습니다.");
                }
            }

            if ((features & DayFeatureFlags.MemoryStructureReport) != 0 && day >= 5)
            {
                if (TryFlag(citizen, out var memorySubmitted, "MemoryReportSubmitted") && !memorySubmitted)
                {
                    RegisterViolation("기억 구조 검사표가 제출되지 않았습니다.");
                }
            }

            // Existing DayRegulationData assets are evaluated here rather than only
            // during citizen generation. Honor required feature flags and each
            // regulation's configured disposition.
            bool foundConsentRule = false;
            foreach (var regulation in GetActiveRegulations(plan.Definition))
            {
                if (regulation == null ||
                    (features & regulation.RequiredFeatures) != regulation.RequiredFeatures)
                    continue;

                if (regulation.Kind == DayRegulationKind.RelatedPersonConsent)
                {
                    // The active story rule starts DAY 8 and ends DAY 14.
                    if (day < 8 || day > 14)
                        continue;
                    foundConsentRule = true;
                }

                var result = regulation.Evaluate(citizen);
                if (result.Status == RegulationEvaluationStatus.Violated)
                {
                    if (!violated)
                    {
                        firstViolation = result.Message;
                        violationDisposition = regulation.ViolationDisposition;
                    }
                    violated = true;
                }
                else if (result.Status == RegulationEvaluationStatus.MissingData)
                {
                    // Missing setup data must not silently penalize a player.
                    Debug.LogWarning("[DayRuleEngine] DAY " + day + ": " + result.Message);
                }
            }

            // Fallback consent check for days whose regulation asset is not yet
            // assigned. Only evaluate when the citizen actually has the fields.
            // Day 15 removes the ordinary consent requirement; Day 16+ reuses the
            // consent fields for mandatory extraction.
            bool consentValid = false;
            if (day >= 8 && !foundConsentRule)
            {
                if (TryConsent(citizen, out var requiresConsent,
                    out var fallbackSubmitted, out var fallbackSignedAll))
                {
                    // A citizen with fewer than two linked people has no mandatory
                    // related-person signatures. This is a valid consent state.
                    consentValid = !requiresConsent || (fallbackSubmitted && fallbackSignedAll);
                    if (day <= 14 && requiresConsent && !consentValid)
                        RegisterViolation("관계인 동의서가 없거나 필요한 서명이 부족합니다.");
                }
            }
            else if (day >= 8)
            {
                if (TryConsent(citizen, out var existingRuleRequiresConsent,
                    out var existingRuleSubmitted, out var existingRuleSignedAll))
                    consentValid = !existingRuleRequiresConsent ||
                                   (existingRuleSubmitted && existingRuleSignedAll);
            }

            // DAY 16-17: valid consent is the ONLY acceptance requirement.
            // Even citizens whose other documents are deliberately inconsistent
            // (the day's fake general citizens) must be approved.
            // The designated DAY 16 pleading citizen was handled above.
            if (day == 16 || day == 17)
            {
                if (consentValid)
                {
                    return new DayRuleVerdict(
                        CaseDisposition.ExtractionApproval,
                        "DAY " + day + ": 관계인 동의서 조건이 충족되어 다른 서류에 오류가 있어도 추출 승인이 의무입니다.",
                        true);
                }

                // If the citizen has enough linked people to require consent,
                // missing/invalid consent means the exceptional approval rule
                // does not apply; return a rejection rather than the fake's
                // unrelated authored verdict.
                if (TryInt(citizen, out var linkedPeople,
                    "MemoryReportLinkedPersonCount") && linkedPeople >= 2)
                {
                    return new DayRuleVerdict(
                        CaseDisposition.DocumentRejection,
                        "DAY " + day + ": 관계인 동의서가 제출되지 않았거나 필요한 서명이 부족합니다.",
                        false);
                }

                Debug.LogWarning("[DayRuleEngine] DAY " + day +
                    ": 관계인 수 또는 동의서 데이터가 없어 강제 추출 규칙을 확정할 수 없습니다.");
            }

            // Keep deliberately authored non-approval fake verdicts. Other
            // violations override only ordinary citizens otherwise approved.
            if (violated && original == CaseDisposition.ExtractionApproval)
                return new DayRuleVerdict(violationDisposition, firstViolation, consentValid);

            return new DayRuleVerdict(original, "", consentValid);

            void RegisterViolation(string message)
            {
                if (!violated)
                {
                    violated = true;
                    firstViolation = message;
                    violationDisposition = CaseDisposition.DocumentRejection;
                }
            }
        }

        private static bool TryConsent(
            CitizenCaseInstance citizen, out bool required, out bool submitted, out bool allSigned)
        {
            required = false;
            submitted = false;
            allSigned = false;
            if (!TryInt(citizen, out var linked, "MemoryReportLinkedPersonCount"))
                return false;

            required = linked >= 2;
            if (!required)
                return true;

            if (!TryFlag(citizen, out submitted, "ConsentFormSubmitted"))
                return false;

            if (!submitted)
                return true;

            if (!TryInt(citizen, out var signatures, "ConsentFormSignedPersonCount"))
                return false;

            allSigned = signatures >= linked;
            return true;
        }

        private static bool TryFlag(CitizenCaseInstance citizen, out bool result, params string[] names)
        {
            foreach (var name in names)
            {
                if (Enum.TryParse(name, false, out GeneratedCitizenField field) &&
                    citizen.TryGetValue<bool>(field, out result))
                    return true;
            }
            result = false;
            return false;
        }

        private static bool TryInt(CitizenCaseInstance citizen, out int result, params string[] names)
        {
            foreach (var name in names)
            {
                if (Enum.TryParse(name, false, out GeneratedCitizenField field) &&
                    citizen.TryGetValue<int>(field, out result))
                    return true;
            }
            result = 0;
            return false;
        }

        private static bool TryText(CitizenCaseInstance citizen, out string result, params string[] names)
        {
            foreach (var name in names)
            {
                if (Enum.TryParse(name, false, out GeneratedCitizenField field) &&
                    citizen.TryGetValue<string>(field, out result) &&
                    !string.IsNullOrWhiteSpace(result))
                    return true;
            }
            result = null;
            return false;
        }

        private static bool TryDate(string value, out DateTime date)
        {
            string[] formats =
            {
                "yyyy-MM-dd", "yyyy.MM.dd", "yyyy/MM/dd",
                "yyyy-M-d", "yyyy.M.d", "yyyy/M/d"
            };
            return DateTime.TryParseExact(
                value.Trim(), formats, CultureInfo.InvariantCulture,
                DateTimeStyles.None, out date);
        }

        // DayDefinition.cs was not uploaded, so retrieve the existing serialized
        // regulation list without assuming an unverified property name.
        private static IEnumerable<DayRegulationData> GetActiveRegulations(DayDefinition day)
        {
            const BindingFlags flags =
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
            var type = day.GetType();
            string[] members =
            {
                "ActiveRegulations", "Regulations", "DayRegulations",
                "activeRegulations", "regulations", "dayRegulations"
            };

            foreach (string name in members)
            {
                object value = null;
                var property = type.GetProperty(name, flags);
                if (property != null && property.GetIndexParameters().Length == 0)
                    value = property.GetValue(day, null);
                else
                {
                    var field = type.GetField(name, flags);
                    if (field != null) value = field.GetValue(day);
                }

                if (value is IEnumerable collection)
                {
                    foreach (var item in collection)
                    {
                        if (item is DayRegulationData regulation)
                            yield return regulation;
                    }
                    yield break;
                }
            }
        }
    }
}
