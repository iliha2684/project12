using System.Collections.Generic;
using System.Text;
using ProjectED.Cases;
using ProjectED.Days;

namespace ProjectED.Examination
{
    public static class CitizenDocumentFormatter
    {
        public static string FormatIdCard(CitizenCaseInstance citizen)
        {
            if (citizen?.SourceCase != null)
            {
                return FormatIdCard(citizen.SourceCase.CitizenIdCard);
            }

            if (citizen == null)
            {
                return "[시민증 데이터 없음]";
            }

            if (!citizen.HasValue(GeneratedCitizenField.IdCardSubmitted))
            {
                return "[시민증 제출 여부 미설정]";
            }

            if (!citizen.GetValue(GeneratedCitizenField.IdCardSubmitted, false))
            {
                return "[시민증 미제출]";
            }

            return $"시민증\n\n이름  {citizen.GetText(GeneratedCitizenField.IdCardFullName)}" +
                   $"\n시민번호  {citizen.GetText(GeneratedCitizenField.IdCardCitizenNumber)}" +
                   $"\n생년월일  {citizen.GetText(GeneratedCitizenField.IdCardBirthDate)}" +
                   $"\n유효기간  {citizen.GetText(GeneratedCitizenField.IdCardExpirationDate)}";
        }

        public static string FormatPaper(CitizenCaseInstance citizen, DayFeatureFlags features)
        {
            if (citizen == null)
            {
                return "[서류 데이터 없음]";
            }

            var sections = new List<string>();
            if ((features & DayFeatureFlags.RemovalApplication) != 0)
            {
                sections.Add(citizen.SourceCase != null
                    ? FormatApplication(citizen.SourceCase.RemovalApplication)
                    : FormatGeneratedApplication(citizen));
            }

            if ((features & DayFeatureFlags.DoctorPrescription) != 0)
            {
                sections.Add(citizen.SourceCase != null
                    ? FormatPrescription(citizen.SourceCase.Prescription)
                    : FormatGeneratedPrescription(citizen));
            }

            if ((features & DayFeatureFlags.MemoryStructureReport) != 0)
            {
                sections.Add(citizen.SourceCase != null
                    ? FormatMemoryReport(citizen.SourceCase.MemoryReport)
                    : FormatGeneratedMemoryReport(citizen));
            }

            if ((features & DayFeatureFlags.RelatedPersonConsent) != 0)
            {
                sections.Add(citizen.SourceCase != null
                    ? FormatConsent(citizen.SourceCase.ConsentForm)
                    : FormatGeneratedConsent(citizen));
            }

            return sections.Count == 0
                ? "[오늘 확인할 추가 서류 없음]"
                : string.Join("\n\n────────────\n\n", sections);
        }

        public static string FormatLegacyApplication(CitizenCaseData caseData)
        {
            return caseData == null ? "[신청서 데이터 없음]" : FormatApplication(caseData.RemovalApplication);
        }

        private static string FormatIdCard(CitizenIdCardData data)
        {
            if (data == null) return "[시민증 데이터 없음]";
            if (!data.Submitted) return "[시민증 미제출]";
            return $"시민증\n\n이름  {data.FullName}\n시민번호  {data.CitizenNumber}\n생년월일  {data.BirthDate}\n유효기간  {data.ExpirationDate}";
        }

        private static string FormatApplication(MemoryRemovalApplicationData data)
        {
            if (data == null) return "[신청서 데이터 없음]";
            if (!data.Submitted) return "[기억 제거 신청서 미제출]";
            return $"기억 제거 신청서\n\n신청자  {data.ApplicantName}\n시민번호  {data.CitizenNumber}\n대상 사건  {data.TargetEvent}\n사건일  {data.EventDate}\n감정 코드  {data.EmotionCode}\n서명  {data.Signature}";
        }

        private static string FormatGeneratedApplication(CitizenCaseInstance citizen)
        {
            if (!citizen.HasValue(GeneratedCitizenField.ApplicationSubmitted)) return "[기억 제거 신청서 제출 여부 미설정]";
            if (!citizen.GetValue(GeneratedCitizenField.ApplicationSubmitted, false)) return "[기억 제거 신청서 미제출]";
            return $"기억 제거 신청서\n\n신청자  {citizen.GetText(GeneratedCitizenField.ApplicationApplicantName)}" +
                   $"\n시민번호  {citizen.GetText(GeneratedCitizenField.ApplicationCitizenNumber)}" +
                   $"\n대상 사건  {citizen.GetText(GeneratedCitizenField.ApplicationTargetEvent)}" +
                   $"\n사건일  {citizen.GetText(GeneratedCitizenField.ApplicationEventDate)}" +
                   $"\n감정 코드  {citizen.GetText(GeneratedCitizenField.ApplicationEmotionCode)}" +
                   $"\n서명  {citizen.GetText(GeneratedCitizenField.ApplicationSignature)}";
        }

        private static string FormatPrescription(DoctorPrescriptionData data)
        {
            if (data == null) return "[처방전 데이터 없음]";
            if (!data.Submitted) return "[의사 처방전 미제출]";
            return $"의사 처방전\n\n의사  {data.DoctorName}\n면허번호  {data.LicenseNumber}\n처방일  {data.PrescriptionDate}\n진단 코드  {data.DiagnosisCode}\n제거 범위  {data.RemovalScope}\n병원  {data.HospitalName}\n직인  {data.HospitalSeal}";
        }

        private static string FormatGeneratedPrescription(CitizenCaseInstance citizen)
        {
            if (!citizen.HasValue(GeneratedCitizenField.PrescriptionSubmitted)) return "[의사 처방전 제출 여부 미설정]";
            if (!citizen.GetValue(GeneratedCitizenField.PrescriptionSubmitted, false)) return "[의사 처방전 미제출]";
            return $"의사 처방전\n\n의사  {citizen.GetText(GeneratedCitizenField.PrescriptionDoctorName)}" +
                   $"\n면허번호  {citizen.GetText(GeneratedCitizenField.PrescriptionLicenseNumber)}" +
                   $"\n처방일  {citizen.GetText(GeneratedCitizenField.PrescriptionDate)}" +
                   $"\n진단 코드  {citizen.GetText(GeneratedCitizenField.PrescriptionDiagnosisCode)}" +
                   $"\n제거 범위  {citizen.GetText(GeneratedCitizenField.PrescriptionRemovalScope)}" +
                   $"\n병원  {citizen.GetText(GeneratedCitizenField.PrescriptionHospitalName)}" +
                   $"\n직인  {citizen.GetText(GeneratedCitizenField.PrescriptionHospitalSeal)}";
        }

        private static string FormatMemoryReport(MemoryStructureReportData data)
        {
            if (data == null) return "[검사표 데이터 없음]";
            if (!data.Submitted) return "[기억 구조 검사표 미제출]";
            return $"기억 구조 검사표\n\n노드 수  {data.NodeCount}\n자아 연결도  {data.SelfConnection}\n타인 연결자  {data.LinkedPersonCount}\n내부기억 반응  {data.InternalMemoryResponse}\n장비번호  {data.EquipmentNumber}\n흔적 코드  {data.TraceCode}";
        }

        private static string FormatGeneratedMemoryReport(CitizenCaseInstance citizen)
        {
            if (!citizen.HasValue(GeneratedCitizenField.MemoryReportSubmitted)) return "[기억 구조 검사표 제출 여부 미설정]";
            if (!citizen.GetValue(GeneratedCitizenField.MemoryReportSubmitted, false)) return "[기억 구조 검사표 미제출]";
            return $"기억 구조 검사표\n\n노드 수  {citizen.GetValue(GeneratedCitizenField.MemoryReportNodeCount, 0)}" +
                   $"\n자아 연결도  {citizen.GetValue(GeneratedCitizenField.MemoryReportSelfConnection, 0)}" +
                   $"\n타인 연결자  {citizen.GetValue(GeneratedCitizenField.MemoryReportLinkedPersonCount, 0)}" +
                   $"\n내부기억 반응  {citizen.GetValue(GeneratedCitizenField.MemoryReportInternalResponse, 0)}" +
                   $"\n장비번호  {citizen.GetText(GeneratedCitizenField.MemoryReportEquipmentNumber)}" +
                   $"\n흔적 코드  {citizen.GetText(GeneratedCitizenField.MemoryReportTraceCode)}";
        }

        private static string FormatConsent(RelatedPersonConsentData data)
        {
            if (data == null) return "[동의서 데이터 없음]";
            if (!data.Submitted) return "[관계인 동의서 미제출]";
            var builder = new StringBuilder("관계인 동의서\n");
            if (data.People.Count == 0)
            {
                builder.Append("\n[등록된 관계인 없음]");
                return builder.ToString();
            }

            for (var index = 0; index < data.People.Count; index++)
            {
                var person = data.People[index];
                if (person == null) continue;
                builder.Append($"\n{index + 1}. {person.FullName} / {person.Relationship}");
                builder.Append($"\n   시민번호 {person.CitizenNumber}");
                builder.Append($"\n   동의일 {person.ConsentDate} / 서명 {person.Signature}\n");
            }

            return builder.ToString();
        }

        private static string FormatGeneratedConsent(CitizenCaseInstance citizen)
        {
            if (!citizen.HasValue(GeneratedCitizenField.ConsentFormSubmitted)) return "[관계인 동의서 제출 여부 미설정]";
            if (!citizen.GetValue(GeneratedCitizenField.ConsentFormSubmitted, false)) return "[관계인 동의서 미제출]";

            var builder = new StringBuilder("관계인 동의서\n");
            var hasLinkedPersonCount = citizen.TryGetValue<int>(
                GeneratedCitizenField.MemoryReportLinkedPersonCount,
                out var linkedPersonCount);
            var hasSignedPersonCount = citizen.TryGetValue<int>(
                GeneratedCitizenField.ConsentFormSignedPersonCount,
                out var signedPersonCount);

            if (hasLinkedPersonCount)
            {
                builder.Append($"\n타인 연결자  {linkedPersonCount}명");
            }

            if (hasSignedPersonCount)
            {
                builder.Append($"\n서명 완료  {signedPersonCount}명");
            }

            var details = citizen.GetText(GeneratedCitizenField.ConsentFormDetails);
            if (!string.IsNullOrWhiteSpace(details))
            {
                builder.Append($"\n\n{details.Trim()}");
            }
            else if (!hasLinkedPersonCount && !hasSignedPersonCount)
            {
                builder.Append("\n\n[관계인 세부 데이터 미설정]");
            }

            return builder.ToString();
        }
    }
}
