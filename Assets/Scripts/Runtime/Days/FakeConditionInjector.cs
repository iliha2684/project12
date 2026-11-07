using System;
using System.Collections.Generic;
using ProjectED.Cases;

namespace ProjectED.Days
{
    public sealed class FakeInjectionResult
    {
        internal FakeInjectionResult(
            CitizenCaseInstance citizen,
            FakeConditionData condition,
            List<CitizenGenerationIssue> issues)
        {
            Citizen = citizen;
            AppliedCondition = condition;
            Issues = issues;
        }

        public CitizenCaseInstance Citizen { get; }
        public FakeConditionData AppliedCondition { get; }
        public IReadOnlyList<CitizenGenerationIssue> Issues { get; }
        public bool Success => Citizen != null && AppliedCondition != null && Citizen.IsFake;
    }

    public sealed class FakeConditionInjector
    {
        public FakeInjectionResult ApplyRandom(DayDefinition day, CitizenCaseInstance citizen, Random random)
        {
            var issues = new List<CitizenGenerationIssue>();
            if (day == null)
            {
                return Failed(citizen, "가짜 조건을 적용할 DayDefinition이 없습니다.");
            }

            if (citizen == null)
            {
                return Failed(null, "가짜 조건을 적용할 시민이 없습니다.");
            }

            if (citizen.IsStoryCase)
            {
                return Failed(citizen, "중요 캐릭터에는 DAY의 랜덤 가짜 조건을 적용하지 않습니다.");
            }

            if (citizen.IsFake)
            {
                return Failed(citizen, "이미 가짜 조건이 적용된 시민입니다.");
            }

            if (random == null)
            {
                return Failed(citizen, "가짜 조건 선택용 난수기가 없습니다.");
            }

            var candidates = new List<FakeConditionData>();
            var totalWeight = 0;
            foreach (var condition in day.AvailableFakeConditions)
            {
                if (condition == null || !condition.IsCompatible(day, citizen.TagSet) || !CanApply(condition, citizen, day))
                {
                    continue;
                }

                candidates.Add(condition);
                totalWeight += condition.SelectionWeight;
            }

            if (totalWeight <= 0)
            {
                return Failed(citizen, "현재 시민에게 적용할 수 있는 가짜 조건이 없습니다.");
            }

            var roll = random.Next(totalWeight);
            FakeConditionData selected = null;
            foreach (var condition in candidates)
            {
                roll -= condition.SelectionWeight;
                if (roll < 0)
                {
                    selected = condition;
                    break;
                }
            }

            if (selected == null)
            {
                return Failed(citizen, "가짜 조건 가중치 선택에 실패했습니다.");
            }

            if (!TryResolveMutation(selected, citizen, day, random, out var replacementValue, out var error))
            {
                return Failed(citizen, error ?? "가짜 조건의 대체 값을 결정하지 못했습니다.");
            }

            citizen.MutateValue(selected.TargetField, replacementValue);
            citizen.MarkAsFake(selected);
            return new FakeInjectionResult(citizen, selected, issues);
        }

        private static bool CanApply(FakeConditionData condition, CitizenCaseInstance citizen, DayDefinition day)
        {
            if (!citizen.HasValue(condition.TargetField))
            {
                return false;
            }

            var currentValue = citizen.Values[condition.TargetField];
            switch (condition.MutationMode)
            {
                case FakeMutationMode.ClearValue:
                    return !ValuesEqual(currentValue, GetClearedValue(condition.TargetField));

                case FakeMutationMode.UseFixedValue:
                    return condition.FixedValue != null &&
                           condition.FixedValue.Field == condition.TargetField &&
                           condition.FixedValue.TryResolveValue(citizen, out var fixedValue, out _) &&
                           !ValuesEqual(currentValue, fixedValue);

                case FakeMutationMode.ReplaceFromPool:
                    return HasReplacementCandidate(condition, citizen, day, currentValue);

                default:
                    return false;
            }
        }

        private static bool TryResolveMutation(
            FakeConditionData condition,
            CitizenCaseInstance citizen,
            DayDefinition day,
            Random random,
            out object replacementValue,
            out string error)
        {
            replacementValue = null;
            error = null;
            var currentValue = citizen.Values[condition.TargetField];

            switch (condition.MutationMode)
            {
                case FakeMutationMode.ClearValue:
                    replacementValue = GetClearedValue(condition.TargetField);
                    return true;

                case FakeMutationMode.UseFixedValue:
                    return condition.FixedValue.TryResolveValue(citizen, out replacementValue, out error);

                case FakeMutationMode.ReplaceFromPool:
                    return TryChooseReplacement(condition, citizen, day, currentValue, random, out replacementValue, out error);

                default:
                    error = $"지원하지 않는 가짜 변조 방식입니다: {condition.MutationMode}";
                    return false;
            }
        }

        private static bool HasReplacementCandidate(
            FakeConditionData condition,
            CitizenCaseInstance citizen,
            DayDefinition day,
            object currentValue)
        {
            return CollectReplacementCandidates(condition, citizen, day, currentValue).Count > 0;
        }

        private static bool TryChooseReplacement(
            FakeConditionData condition,
            CitizenCaseInstance citizen,
            DayDefinition day,
            object currentValue,
            Random random,
            out object replacementValue,
            out string error)
        {
            replacementValue = null;
            error = null;
            var candidates = CollectReplacementCandidates(condition, citizen, day, currentValue);
            var totalWeight = 0;
            foreach (var candidate in candidates)
            {
                totalWeight += candidate.Option.SelectionWeight;
            }

            if (totalWeight <= 0)
            {
                error = $"가짜 조건 '{condition.DisplayName}'의 대체 풀에서 다른 값을 찾지 못했습니다.";
                return false;
            }

            var roll = random.Next(totalWeight);
            foreach (var candidate in candidates)
            {
                roll -= candidate.Option.SelectionWeight;
                if (roll < 0)
                {
                    replacementValue = candidate.Value;
                    return true;
                }
            }

            error = "대체 값 가중치 선택에 실패했습니다.";
            return false;
        }

        private static List<ReplacementCandidate> CollectReplacementCandidates(
            FakeConditionData condition,
            CitizenCaseInstance citizen,
            DayDefinition day,
            object currentValue)
        {
            var candidates = new List<ReplacementCandidate>();
            var pool = condition.ReplacementPool;
            if (pool == null || (pool.RequiredFeatures & ~day.UnlockedFeatures) != DayFeatureFlags.None)
            {
                return candidates;
            }

            foreach (var option in pool.Options)
            {
                if (option == null || !option.IsCompatible(citizen.TagSet))
                {
                    continue;
                }

                foreach (var assignment in option.Values)
                {
                    if (assignment == null ||
                        assignment.Field != condition.TargetField ||
                        !assignment.TryResolveValue(citizen, out var resolvedValue, out _) ||
                        ValuesEqual(currentValue, resolvedValue))
                    {
                        continue;
                    }

                    candidates.Add(new ReplacementCandidate(option, resolvedValue));
                    break;
                }
            }

            return candidates;
        }

        private static object GetClearedValue(GeneratedCitizenField field)
        {
            return GeneratedCitizenFieldSchema.GetValueType(field) switch
            {
                ConditionValueType.Integer => 0,
                ConditionValueType.Boolean => false,
                ConditionValueType.Sprite => null,
                _ => string.Empty
            };
        }

        private static bool ValuesEqual(object left, object right)
        {
            return Equals(left, right);
        }

        private static FakeInjectionResult Failed(CitizenCaseInstance citizen, string message)
        {
            return new FakeInjectionResult(
                citizen,
                null,
                new List<CitizenGenerationIssue>
                {
                    new(CaseValidationSeverity.Error, message)
                });
        }

        private readonly struct ReplacementCandidate
        {
            public ReplacementCandidate(CitizenConditionOption option, object value)
            {
                Option = option;
                Value = value;
            }

            public CitizenConditionOption Option { get; }
            public object Value { get; }
        }
    }
}
