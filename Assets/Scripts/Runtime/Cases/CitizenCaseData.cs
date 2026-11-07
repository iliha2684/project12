using System;
using System.Collections.Generic;
using UnityEngine;

namespace ProjectED.Cases
{
    [Serializable]
    public sealed class CaseAppearanceConditions
    {
        [SerializeField, Range(1, 18), InspectorName("최초 등장 가능일")]
        private int earliestDay = 1;

        [SerializeField, Range(1, 18), InspectorName("마지막 등장 가능일")]
        private int latestDay = 18;

        [SerializeField, Min(1), InspectorName("선택 가중치")]
        private int selectionWeight = 1;

        [SerializeField, InspectorName("캠페인 내 재등장 허용")]
        private bool allowRepeatInCampaign;

        [SerializeField, InspectorName("선행 사건")]
        private CitizenCaseData prerequisiteCase;

        [SerializeField, InspectorName("이전 처분 조건 사용")]
        private bool requirePreviousDisposition;

        [SerializeField, InspectorName("필요한 이전 처분")]
        private CaseDisposition requiredPreviousDisposition;

        public int EarliestDay => earliestDay;
        public int LatestDay => latestDay;
        public int SelectionWeight => selectionWeight;
        public bool AllowRepeatInCampaign => allowRepeatInCampaign;
        public CitizenCaseData PrerequisiteCase => prerequisiteCase;
        public bool RequirePreviousDisposition => requirePreviousDisposition;
        public CaseDisposition RequiredPreviousDisposition => requiredPreviousDisposition;
    }

    [Serializable]
    public sealed class CaseJudgmentData
    {
        [SerializeField, InspectorName("정답 처분")]
        private CaseDisposition expectedDisposition;

        [SerializeField, InspectorName("관련 규정 ID")]
        private List<string> violatedRuleIds = new();

        [SerializeField, TextArea(3, 8), InspectorName("판정 이유")]
        private string judgmentReason;

        public CaseDisposition ExpectedDisposition => expectedDisposition;
        public IReadOnlyList<string> ViolatedRuleIds => violatedRuleIds;
        public string JudgmentReason => judgmentReason;
    }

    [CreateAssetMenu(fileName = "CitizenCase_", menuName = "Project ED/Cases/Citizen Case", order = 1)]
    public sealed class CitizenCaseData : ScriptableObject
    {
        [SerializeField, InspectorName("사건 ID")]
        private string caseId;

        [SerializeField, InspectorName("시민 프로필")]
        private CitizenProfileData citizen;

        [SerializeField, InspectorName("사건 분류")]
        private CaseCategory category;

        [SerializeField, Tooltip("중요 캐릭터가 사건 설정상 가짜 시민인지 표시합니다. DAY의 가짜 일반 시민 수에는 포함되지 않습니다."), InspectorName("가짜 시민")]
        private bool isFakeCitizen;

        [SerializeField, InspectorName("등장 조건")]
        private CaseAppearanceConditions appearanceConditions = new();

        [SerializeField, InspectorName("시민 이미지 덮어쓰기")]
        private Sprite portraitOverride;

        [SerializeField, TextArea(3, 8), InspectorName("첫 대사")]
        private string openingDialogue;

        [SerializeField, TextArea(3, 8), InspectorName("추가 질문 대사")]
        private string questionDialogue;

        [SerializeField, InspectorName("대화 순서")]
        private List<CaseDialogueLine> dialogue = new();

        [SerializeField, InspectorName("시민증")]
        private CitizenIdCardData citizenIdCard = new();

        [SerializeField, InspectorName("기억 제거 신청서")]
        private MemoryRemovalApplicationData removalApplication = new();

        [SerializeField, InspectorName("의사 처방전")]
        private DoctorPrescriptionData prescription = new();

        [SerializeField, InspectorName("기억 구조 검사표")]
        private MemoryStructureReportData memoryReport = new();

        [SerializeField, InspectorName("관계인 동의서")]
        private RelatedPersonConsentData consentForm = new();

        [SerializeField, InspectorName("제작자 판정")]
        private CaseJudgmentData judgment = new();

        public string CaseId => caseId;
        public CitizenProfileData Citizen => citizen;
        public CaseCategory Category => category;
        public bool IsFakeCitizen => isFakeCitizen;
        public CaseAppearanceConditions AppearanceConditions => appearanceConditions;
        public Sprite Portrait => portraitOverride != null ? portraitOverride : citizen != null ? citizen.DefaultPortrait : null;
        public string OpeningDialogue => openingDialogue;
        public string QuestionDialogue => questionDialogue;
        public IReadOnlyList<CaseDialogueLine> Dialogue => dialogue;
        public CitizenIdCardData CitizenIdCard => citizenIdCard;
        public MemoryRemovalApplicationData RemovalApplication => removalApplication;
        public DoctorPrescriptionData Prescription => prescription;
        public MemoryStructureReportData MemoryReport => memoryReport;
        public RelatedPersonConsentData ConsentForm => consentForm;
        public CaseJudgmentData Judgment => judgment;
    }
}
