using System;
using System.Collections.Generic;
using UnityEngine;

namespace ProjectED.Days
{
    [Serializable]
    public sealed class CitizenConditionOption
    {
        [SerializeField, InspectorName("선택지 ID")]
        private string optionId;

        [SerializeField, InspectorName("표시 이름")]
        private string displayName;

        [SerializeField, Min(1), InspectorName("선택 가중치")]
        private int selectionWeight = 1;

        [SerializeField, Tooltip("이 태그를 모두 가진 시민에게만 선택됩니다."), InspectorName("필요 태그")]
        private List<string> requiredTags = new();

        [SerializeField, Tooltip("이 태그 중 하나라도 가진 시민에게는 선택되지 않습니다."), InspectorName("금지 태그")]
        private List<string> excludedTags = new();

        [SerializeField, Tooltip("선택된 뒤 시민에게 추가할 태그입니다."), InspectorName("제공 태그")]
        private List<string> providedTags = new();

        [SerializeField, InspectorName("적용할 값")]
        private List<ConditionValueAssignment> values = new();

        public string OptionId => optionId;
        public string DisplayName => displayName;
        public int SelectionWeight => Mathf.Max(1, selectionWeight);
        public IReadOnlyList<string> RequiredTags => requiredTags;
        public IReadOnlyList<string> ExcludedTags => excludedTags;
        public IReadOnlyList<string> ProvidedTags => providedTags;
        public IReadOnlyList<ConditionValueAssignment> Values => values;

        public bool IsCompatible(ISet<string> citizenTags)
        {
            citizenTags ??= EmptyTagSet.Instance;

            foreach (var requiredTag in requiredTags)
            {
                if (!string.IsNullOrWhiteSpace(requiredTag) && !citizenTags.Contains(requiredTag))
                {
                    return false;
                }
            }

            foreach (var excludedTag in excludedTags)
            {
                if (!string.IsNullOrWhiteSpace(excludedTag) && citizenTags.Contains(excludedTag))
                {
                    return false;
                }
            }

            return true;
        }

        private sealed class EmptyTagSet : HashSet<string>
        {
            public static readonly EmptyTagSet Instance = new();
        }
    }

    [CreateAssetMenu(fileName = "ConditionPool_", menuName = "Project ED/Days/Citizen Condition Pool", order = 10)]
    public sealed class CitizenConditionPoolData : ScriptableObject
    {
        [SerializeField, InspectorName("조건 풀 ID")]
        private string poolId;

        [SerializeField, InspectorName("표시 이름")]
        private string displayName;

        [SerializeField, InspectorName("조건 분류")]
        private CitizenConditionCategory category;

        [SerializeField, Tooltip("이 기능이 해금된 날짜에만 풀을 사용합니다."), InspectorName("필요 기능")]
        private DayFeatureFlags requiredFeatures;

        [SerializeField, Tooltip("활성화된 경우 일반 시민마다 이 풀에서 반드시 하나를 선택해야 합니다."), InspectorName("모든 시민에게 필수")]
        private bool requiredForEveryCitizen = true;

        [SerializeField, InspectorName("선택지")]
        private List<CitizenConditionOption> options = new();

        public string PoolId => poolId;
        public string DisplayName => displayName;
        public CitizenConditionCategory Category => category;
        public DayFeatureFlags RequiredFeatures => requiredFeatures;
        public bool RequiredForEveryCitizen => requiredForEveryCitizen;
        public IReadOnlyList<CitizenConditionOption> Options => options;

        public bool TryChooseCompatible(System.Random random, ISet<string> citizenTags, out CitizenConditionOption selected)
        {
            selected = null;
            if (random == null)
            {
                throw new ArgumentNullException(nameof(random));
            }

            var totalWeight = 0;
            foreach (var option in options)
            {
                if (option != null && option.IsCompatible(citizenTags))
                {
                    totalWeight += option.SelectionWeight;
                }
            }

            if (totalWeight <= 0)
            {
                return false;
            }

            var roll = random.Next(totalWeight);
            foreach (var option in options)
            {
                if (option == null || !option.IsCompatible(citizenTags))
                {
                    continue;
                }

                roll -= option.SelectionWeight;
                if (roll < 0)
                {
                    selected = option;
                    return true;
                }
            }

            return false;
        }
    }
}
