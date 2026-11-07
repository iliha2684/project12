using System.Collections.Generic;
using ProjectED.Cases;
using UnityEngine;

namespace ProjectED.Days
{
    [CreateAssetMenu(fileName = "FakeCondition_", menuName = "Project ED/Days/Fake Condition", order = 11)]
    public sealed class FakeConditionData : ScriptableObject
    {
        [SerializeField, InspectorName("가짜 조건 ID")]
        private string conditionId;

        [SerializeField, InspectorName("표시 이름")]
        private string displayName;

        [SerializeField, Min(1), InspectorName("선택 가중치")]
        private int selectionWeight = 1;

        [SerializeField, InspectorName("필요 기능")]
        private DayFeatureFlags requiredFeatures;

        [SerializeField, InspectorName("필요 태그")]
        private List<string> requiredTags = new();

        [SerializeField, InspectorName("금지 태그")]
        private List<string> excludedTags = new();

        [SerializeField, InspectorName("변조할 필드")]
        private GeneratedCitizenField targetField;

        [SerializeField, InspectorName("변조 방식")]
        private FakeMutationMode mutationMode = FakeMutationMode.ReplaceFromPool;

        [SerializeField, Tooltip("Replace From Pool 방식에서 다른 값을 가져올 풀입니다."), InspectorName("대체 값 조건 풀")]
        private CitizenConditionPoolData replacementPool;

        [SerializeField, Tooltip("Use Fixed Value 방식에서 적용할 값입니다."), InspectorName("고정 대체 값")]
        private ConditionValueAssignment fixedValue = new();

        [SerializeField, InspectorName("정답 처분")]
        private CaseDisposition expectedDisposition = CaseDisposition.DocumentRejection;

        [SerializeField, InspectorName("관련 규정 ID")]
        private string violatedRuleId;

        [SerializeField, TextArea(3, 8), InspectorName("제작자 메모")]
        private string designerNotes;

        public string ConditionId => conditionId;
        public string DisplayName => displayName;
        public int SelectionWeight => Mathf.Max(1, selectionWeight);
        public DayFeatureFlags RequiredFeatures => requiredFeatures;
        public IReadOnlyList<string> RequiredTags => requiredTags;
        public IReadOnlyList<string> ExcludedTags => excludedTags;
        public GeneratedCitizenField TargetField => targetField;
        public FakeMutationMode MutationMode => mutationMode;
        public CitizenConditionPoolData ReplacementPool => replacementPool;
        public ConditionValueAssignment FixedValue => fixedValue;
        public CaseDisposition ExpectedDisposition => expectedDisposition;
        public string ViolatedRuleId => violatedRuleId;
        public string DesignerNotes => designerNotes;

        public bool IsCompatible(DayDefinition day, ISet<string> citizenTags)
        {
            if (day == null || (requiredFeatures & ~day.UnlockedFeatures) != DayFeatureFlags.None)
            {
                return false;
            }

            foreach (var requiredTag in requiredTags)
            {
                if (!string.IsNullOrWhiteSpace(requiredTag) &&
                    (citizenTags == null || !citizenTags.Contains(requiredTag)))
                {
                    return false;
                }
            }

            foreach (var excludedTag in excludedTags)
            {
                if (!string.IsNullOrWhiteSpace(excludedTag) &&
                    citizenTags != null && citizenTags.Contains(excludedTag))
                {
                    return false;
                }
            }

            return true;
        }
    }
}
