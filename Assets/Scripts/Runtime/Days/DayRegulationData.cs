using ProjectED.Cases;
using UnityEngine;

namespace ProjectED.Days
{
    public readonly struct RegulationEvaluationResult
    {
        public RegulationEvaluationResult(
            DayRegulationData regulation,
            RegulationEvaluationStatus status,
            string message)
        {
            Regulation = regulation;
            Status = status;
            Message = message;
        }

        public DayRegulationData Regulation { get; }
        public RegulationEvaluationStatus Status { get; }
        public string Message { get; }
        public bool IsViolation => Status == RegulationEvaluationStatus.Violated;
    }

    [CreateAssetMenu(fileName = "Regulation_", menuName = "Project ED/Days/Day Regulation", order = 12)]
    public sealed class DayRegulationData : ScriptableObject
    {
        [SerializeField, InspectorName("규정 ID")]
        private string regulationId;

        [SerializeField, InspectorName("표시 이름")]
        private string displayName;

        [SerializeField, TextArea(2, 6), InspectorName("규정 문구")]
        private string description;

        [SerializeField, InspectorName("규정 종류")]
        private DayRegulationKind kind;

        [SerializeField, Tooltip("이 기능들이 해금된 DAY에서만 규정을 활성화할 수 있습니다."), InspectorName("필요 기능")]
        private DayFeatureFlags requiredFeatures;

        [SerializeField, InspectorName("위반 시 처분")]
        private CaseDisposition violationDisposition = CaseDisposition.DocumentRejection;

        [Header("관계인 동의서 규정")]
        [SerializeField, Min(1), Tooltip("이 수 이상 타인 연결자가 있을 때 동의서를 요구합니다."), InspectorName("동의 필요 연결자 수")]
        private int minimumLinkedPersonCount = 2;

        [SerializeField, Tooltip("활성화하면 타인 연결자 수만큼 서명이 모두 있어야 합니다."), InspectorName("전원 서명 필요")]
        private bool requireAllRelatedPersonSignatures = true;

        public string RegulationId => regulationId;
        public string DisplayName => displayName;
        public string Description => description;
        public DayRegulationKind Kind => kind;
        public DayFeatureFlags RequiredFeatures => requiredFeatures;
        public CaseDisposition ViolationDisposition => violationDisposition;
        public int MinimumLinkedPersonCount => Mathf.Max(1, minimumLinkedPersonCount);
        public bool RequireAllRelatedPersonSignatures => requireAllRelatedPersonSignatures;

        public RegulationEvaluationResult Evaluate(CitizenCaseInstance citizen)
        {
            if (kind != DayRegulationKind.RelatedPersonConsent)
            {
                return Result(RegulationEvaluationStatus.NotApplicable, "자동 판정이 없는 참고용 규정입니다.");
            }

            if (citizen == null)
            {
                return Result(RegulationEvaluationStatus.MissingData, "판정할 시민 데이터가 없습니다.");
            }

            if (!citizen.TryGetValue<int>(GeneratedCitizenField.MemoryReportLinkedPersonCount, out var linkedPersonCount))
            {
                return Result(RegulationEvaluationStatus.MissingData, "기억 구조 검사표의 타인 연결자 수가 설정되지 않았습니다.");
            }

            if (linkedPersonCount < MinimumLinkedPersonCount)
            {
                return Result(RegulationEvaluationStatus.NotApplicable, $"타인 연결자 {linkedPersonCount}명으로 동의서 대상이 아닙니다.");
            }

            if (!citizen.TryGetValue<bool>(GeneratedCitizenField.ConsentFormSubmitted, out var submitted))
            {
                return Result(RegulationEvaluationStatus.MissingData, "관계인 동의서 제출 여부가 설정되지 않았습니다.");
            }

            if (!submitted)
            {
                return Result(RegulationEvaluationStatus.Violated, $"타인 연결자 {linkedPersonCount}명이지만 관계인 동의서가 제출되지 않았습니다.");
            }

            if (!requireAllRelatedPersonSignatures)
            {
                return Result(RegulationEvaluationStatus.Compliant, "관계인 동의서가 제출되었습니다.");
            }

            if (!citizen.TryGetValue<int>(GeneratedCitizenField.ConsentFormSignedPersonCount, out var signedPersonCount))
            {
                return Result(RegulationEvaluationStatus.MissingData, "관계인 동의서의 서명 완료 인원 수가 설정되지 않았습니다.");
            }

            if (signedPersonCount < linkedPersonCount)
            {
                return Result(
                    RegulationEvaluationStatus.Violated,
                    $"타인 연결자 {linkedPersonCount}명 중 {Mathf.Max(0, signedPersonCount)}명만 서명했습니다.");
            }

            return Result(
                RegulationEvaluationStatus.Compliant,
                $"타인 연결자 {linkedPersonCount}명의 서명이 모두 확인되었습니다.");
        }

        private RegulationEvaluationResult Result(RegulationEvaluationStatus status, string message)
        {
            return new RegulationEvaluationResult(this, status, message);
        }
    }
}
