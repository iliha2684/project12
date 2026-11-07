using System;
using System.Collections.Generic;
using ProjectED.Cases;
using UnityEngine;

namespace ProjectED.Days
{
    [Serializable]
    public sealed class StoryCaseSlot
    {
        [SerializeField, Min(1), Tooltip("1부터 시작하는 그날의 등장 순서입니다."), InspectorName("등장 순서")]
        private int position = 1;

        [SerializeField, InspectorName("중요 캐릭터 사건")]
        private CitizenCaseData citizenCase;

        public int Position => position;
        public CitizenCaseData CitizenCase => citizenCase;
    }

    [CreateAssetMenu(fileName = "Day_", menuName = "Project ED/Days/Day Definition", order = 0)]
    public sealed class DayDefinition : ScriptableObject
    {
        [SerializeField, InspectorName("DAY ID")]
        private string dayId;

        [SerializeField, Range(1, 18), InspectorName("날짜")]
        private int dayNumber = 1;

        [SerializeField, InspectorName("표시 제목")]
        private string displayTitle;

        [Header("하루 인원")]
        [SerializeField, Min(1), InspectorName("전체 처리 인원")]
        private int totalCitizenCount = 1;

        [SerializeField, Min(0), Tooltip("중요 캐릭터를 제외한 일반 시민 중 가짜인 사람의 정확한 수입니다."), InspectorName("가짜 일반 시민 수")]
        private int fakeCitizenCount;

        [SerializeField, Tooltip("중요 캐릭터는 가짜 일반 시민 수에 포함되지 않습니다."), InspectorName("중요 캐릭터 배치")]
        private List<StoryCaseSlot> storyCases = new();

        [Header("날짜별 기능과 생성 조건")]
        [SerializeField, InspectorName("해금 기능")]
        private DayFeatureFlags unlockedFeatures = DayFeatureFlags.CitizenIdCard | DayFeatureFlags.RemovalApplication;

        [SerializeField, Tooltip("목록 순서대로 각 풀에서 호환되는 조건 하나를 뽑습니다."), InspectorName("활성 조건 풀")]
        private List<CitizenConditionPoolData> activeConditionPools = new();

        [SerializeField, Tooltip("가짜 일반 시민에게 넣을 수 있는 모순 조건입니다."), InspectorName("사용 가능한 가짜 조건")]
        private List<FakeConditionData> availableFakeConditions = new();

        [SerializeField, Tooltip("이 DAY에 적용할 규정입니다. 관계인 동의서 규정은 5~14일차에 넣고 15일차부터 제거합니다."), InspectorName("활성 규정")]
        private List<DayRegulationData> activeRegulations = new();

        [SerializeField, TextArea(3, 8), InspectorName("제작자 메모")]
        private string designerNotes;

        public string DayId => dayId;
        public int DayNumber => dayNumber;
        public string DisplayTitle => displayTitle;
        public int TotalCitizenCount => totalCitizenCount;
        public int FakeCitizenCount => fakeCitizenCount;
        public IReadOnlyList<StoryCaseSlot> StoryCases => storyCases;
        public DayFeatureFlags UnlockedFeatures => unlockedFeatures;
        public IReadOnlyList<CitizenConditionPoolData> ActiveConditionPools => activeConditionPools;
        public IReadOnlyList<FakeConditionData> AvailableFakeConditions => availableFakeConditions;
        public IReadOnlyList<DayRegulationData> ActiveRegulations => activeRegulations;
        public string DesignerNotes => designerNotes;

        public int StoryCitizenCount
        {
            get
            {
                var count = 0;
                foreach (var slot in storyCases)
                {
                    if (slot?.CitizenCase != null)
                    {
                        count++;
                    }
                }

                return count;
            }
        }

        public int GeneralCitizenCount => Mathf.Max(0, totalCitizenCount - StoryCitizenCount);
        public int NormalGeneralCitizenCount => Mathf.Max(0, GeneralCitizenCount - fakeCitizenCount);

        public bool HasFeature(DayFeatureFlags feature)
        {
            return (unlockedFeatures & feature) == feature;
        }
    }
}
