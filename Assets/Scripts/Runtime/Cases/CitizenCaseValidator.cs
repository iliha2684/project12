using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text.RegularExpressions;

namespace ProjectED.Cases
{
    public readonly struct CaseValidationIssue
    {
        public CaseValidationIssue(CaseValidationSeverity severity, string message, string propertyPath = null)
        {
            Severity = severity;
            Message = message;
            PropertyPath = propertyPath;
        }

        public CaseValidationSeverity Severity { get; }
        public string Message { get; }
        public string PropertyPath { get; }
    }

    public static class CitizenCaseValidator
    {
        private static readonly Regex CitizenNumberPattern = new(@"^\d{12}$", RegexOptions.Compiled);
        private const string DateFormat = "yyyy-MM-dd";

        public static IReadOnlyList<CaseValidationIssue> Validate(CitizenCaseData caseData)
        {
            var issues = new List<CaseValidationIssue>();

            if (caseData == null)
            {
                issues.Add(new CaseValidationIssue(CaseValidationSeverity.Error, "사건 데이터가 없습니다."));
                return issues;
            }

            Require(caseData.CaseId, "사건 ID가 비어 있습니다.", "caseId", issues, CaseValidationSeverity.Error);

            if (caseData.Citizen == null)
            {
                issues.Add(new CaseValidationIssue(CaseValidationSeverity.Error, "시민 프로필을 지정해야 합니다.", "citizen"));
            }
            else
            {
                Require(caseData.Citizen.ProfileId, "시민 프로필 ID가 비어 있습니다.", "citizen", issues);
                Require(caseData.Citizen.DisplayName, "시민 표시 이름이 비어 있습니다.", "citizen", issues);
            }

            var appearance = caseData.AppearanceConditions;
            if (appearance == null)
            {
                issues.Add(new CaseValidationIssue(CaseValidationSeverity.Error, "등장 조건 데이터가 없습니다.", "appearanceConditions"));
            }
            else
            {
                if (appearance.EarliestDay > appearance.LatestDay)
                {
                    issues.Add(new CaseValidationIssue(CaseValidationSeverity.Error, "최초 등장 가능일은 마지막 등장 가능일보다 늦을 수 없습니다.", "appearanceConditions"));
                }

                if (appearance.SelectionWeight < 1)
                {
                    issues.Add(new CaseValidationIssue(CaseValidationSeverity.Error, "선택 가중치는 1 이상이어야 합니다.", "appearanceConditions"));
                }

                if (caseData.Category == CaseCategory.RepeatVisit && appearance.PrerequisiteCase == null)
                {
                    issues.Add(new CaseValidationIssue(CaseValidationSeverity.Error, "재방문 사건에는 선행 사건이 필요합니다.", "appearanceConditions.prerequisiteCase"));
                }

                if (appearance.RequirePreviousDisposition && appearance.PrerequisiteCase == null)
                {
                    issues.Add(new CaseValidationIssue(CaseValidationSeverity.Error, "이전 처분 조건을 사용하려면 선행 사건을 지정해야 합니다.", "appearanceConditions.prerequisiteCase"));
                }
            }

            ValidateDocuments(caseData, issues);

            if (caseData.Judgment == null)
            {
                issues.Add(new CaseValidationIssue(CaseValidationSeverity.Error, "제작자 판정 데이터가 없습니다.", "judgment"));
            }
            else if (string.IsNullOrWhiteSpace(caseData.Judgment.JudgmentReason))
            {
                issues.Add(new CaseValidationIssue(CaseValidationSeverity.Warning, "판정 이유가 비어 있습니다.", "judgment.judgmentReason"));
            }

            var hasDialogue = false;
            for (var index = 0; index < caseData.Dialogue.Count; index++)
            {
                var line = caseData.Dialogue[index];
                if (line == null || string.IsNullOrWhiteSpace(line.Text))
                {
                    issues.Add(new CaseValidationIssue(
                        CaseValidationSeverity.Warning,
                        $"대화 {index + 1}번의 내용이 비어 있습니다.",
                        $"dialogue.Array.data[{index}].text"));
                    continue;
                }

                hasDialogue = true;
            }

            if (!hasDialogue && string.IsNullOrWhiteSpace(caseData.OpeningDialogue))
            {
                issues.Add(new CaseValidationIssue(CaseValidationSeverity.Info, "대화 내용이 비어 있습니다.", "dialogue"));
            }

            if (caseData.Portrait == null)
            {
                issues.Add(new CaseValidationIssue(CaseValidationSeverity.Info, "표시할 시민 이미지가 없습니다.", "portraitOverride"));
            }

            return issues;
        }

        private static void ValidateDocuments(CitizenCaseData caseData, ICollection<CaseValidationIssue> issues)
        {
            var idCard = caseData.CitizenIdCard;
            if (idCard == null)
            {
                issues.Add(new CaseValidationIssue(CaseValidationSeverity.Error, "시민증 데이터가 없습니다.", "citizenIdCard"));
            }
            else if (idCard.Submitted)
            {
                Require(idCard.FullName, "제출된 시민증의 이름이 비어 있습니다.", "citizenIdCard.fullName", issues);
                ValidateCitizenNumber(idCard.CitizenNumber, "시민증", "citizenIdCard.citizenNumber", issues);
                ValidateDate(idCard.BirthDate, "시민증 생년월일", "citizenIdCard.birthDate", issues);
                ValidateDate(idCard.ExpirationDate, "시민증 유효기간", "citizenIdCard.expirationDate", issues);
            }

            var application = caseData.RemovalApplication;
            if (application == null)
            {
                issues.Add(new CaseValidationIssue(CaseValidationSeverity.Error, "기억 제거 신청서 데이터가 없습니다.", "removalApplication"));
            }
            else if (application.Submitted)
            {
                Require(application.ApplicantName, "제출된 신청서의 신청자 이름이 비어 있습니다.", "removalApplication.applicantName", issues);
                ValidateCitizenNumber(application.CitizenNumber, "기억 제거 신청서", "removalApplication.citizenNumber", issues);
                ValidateDate(application.EventDate, "신청서 사건일", "removalApplication.eventDate", issues);
            }

            var prescription = caseData.Prescription;
            if (prescription == null)
            {
                issues.Add(new CaseValidationIssue(CaseValidationSeverity.Error, "의사 처방전 데이터가 없습니다.", "prescription"));
            }
            else if (prescription.Submitted)
            {
                Require(prescription.DoctorName, "제출된 처방전의 의사 이름이 비어 있습니다.", "prescription.doctorName", issues);
                Require(prescription.LicenseNumber, "제출된 처방전의 면허번호가 비어 있습니다.", "prescription.licenseNumber", issues);
                ValidateDate(prescription.PrescriptionDate, "처방일", "prescription.prescriptionDate", issues);
            }

            var report = caseData.MemoryReport;
            if (report == null)
            {
                issues.Add(new CaseValidationIssue(CaseValidationSeverity.Error, "기억 구조 검사표 데이터가 없습니다.", "memoryReport"));
            }
            else if (report.Submitted)
            {
                Require(report.EquipmentNumber, "제출된 검사표의 장비번호가 비어 있습니다.", "memoryReport.equipmentNumber", issues);
            }

            var consent = caseData.ConsentForm;
            if (consent == null)
            {
                issues.Add(new CaseValidationIssue(CaseValidationSeverity.Error, "관계인 동의서 데이터가 없습니다.", "consentForm"));
            }
            else if (consent.Submitted)
            {
                if (consent.People.Count == 0)
                {
                    issues.Add(new CaseValidationIssue(CaseValidationSeverity.Warning, "동의서가 제출됨으로 설정됐지만 관계인이 없습니다. 의도적인 누락인지 확인하세요.", "consentForm.people"));
                }

                for (var index = 0; index < consent.People.Count; index++)
                {
                    var person = consent.People[index];
                    if (person == null)
                    {
                        issues.Add(new CaseValidationIssue(CaseValidationSeverity.Warning, $"관계인 {index + 1} 데이터가 비어 있습니다.", $"consentForm.people.Array.data[{index}]"));
                        continue;
                    }

                    ValidateCitizenNumber(person.CitizenNumber, $"관계인 {index + 1}", $"consentForm.people.Array.data[{index}].citizenNumber", issues);
                    ValidateDate(person.ConsentDate, $"관계인 {index + 1} 동의일", $"consentForm.people.Array.data[{index}].consentDate", issues);
                }
            }
        }

        private static void Require(
            string value,
            string message,
            string propertyPath,
            ICollection<CaseValidationIssue> issues,
            CaseValidationSeverity severity = CaseValidationSeverity.Warning)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                issues.Add(new CaseValidationIssue(severity, message, propertyPath));
            }
        }

        private static void ValidateCitizenNumber(
            string value,
            string label,
            string propertyPath,
            ICollection<CaseValidationIssue> issues)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                issues.Add(new CaseValidationIssue(CaseValidationSeverity.Warning, $"{label}의 시민번호가 비어 있습니다. 의도적인 누락인지 확인하세요.", propertyPath));
                return;
            }

            if (!CitizenNumberPattern.IsMatch(value))
            {
                issues.Add(new CaseValidationIssue(CaseValidationSeverity.Warning, $"{label}의 시민번호가 12자리 숫자가 아닙니다. 의도적인 위조 값인지 확인하세요.", propertyPath));
            }
        }

        private static void ValidateDate(
            string value,
            string label,
            string propertyPath,
            ICollection<CaseValidationIssue> issues)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return;
            }

            if (!DateTime.TryParseExact(value, DateFormat, CultureInfo.InvariantCulture, DateTimeStyles.None, out _))
            {
                issues.Add(new CaseValidationIssue(CaseValidationSeverity.Warning, $"{label}이 {DateFormat} 형식이 아닙니다. 의도적인 오류인지 확인하세요.", propertyPath));
            }
        }
    }
}
