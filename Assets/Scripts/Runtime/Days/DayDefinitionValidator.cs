using System;
using System.Collections.Generic;
using ProjectED.Cases;

namespace ProjectED.Days
{
    public static class DayDefinitionValidator
    {
        public static IReadOnlyList<CaseValidationIssue> Validate(DayDefinition day)
        {
            var issues = new List<CaseValidationIssue>();
            if (day == null)
            {
                issues.Add(new CaseValidationIssue(CaseValidationSeverity.Error, "DAY 데이터가 없습니다."));
                return issues;
            }

            if (string.IsNullOrWhiteSpace(day.DayId))
            {
                issues.Add(new CaseValidationIssue(CaseValidationSeverity.Error, "DAY ID가 비어 있습니다.", "dayId"));
            }

            if (day.TotalCitizenCount < 1)
            {
                issues.Add(new CaseValidationIssue(CaseValidationSeverity.Error, "전체 처리 인원은 1명 이상이어야 합니다.", "totalCitizenCount"));
            }

            ValidateStoryCases(day, issues);

            if (day.FakeCitizenCount < 0)
            {
                issues.Add(new CaseValidationIssue(CaseValidationSeverity.Error, "가짜 일반 시민 수는 음수일 수 없습니다.", "fakeCitizenCount"));
            }
            else if (day.FakeCitizenCount > day.GeneralCitizenCount)
            {
                issues.Add(new CaseValidationIssue(
                    CaseValidationSeverity.Error,
                    $"가짜 일반 시민 {day.FakeCitizenCount}명을 배치할 수 없습니다. 자동 생성 일반 시민은 {day.GeneralCitizenCount}명입니다.",
                    "fakeCitizenCount"));
            }

            ValidateConditionPools(day, issues);
            ValidateFakeConditions(day, issues);
            ValidateRegulations(day, issues);
            return issues;
        }

        private static void ValidateStoryCases(DayDefinition day, ICollection<CaseValidationIssue> issues)
        {
            var usedPositions = new HashSet<int>();
            for (var index = 0; index < day.StoryCases.Count; index++)
            {
                var slot = day.StoryCases[index];
                var path = $"storyCases.Array.data[{index}]";
                if (slot == null)
                {
                    issues.Add(new CaseValidationIssue(CaseValidationSeverity.Warning, $"중요 캐릭터 배치 {index + 1}번이 비어 있습니다.", path));
                    continue;
                }

                if (slot.CitizenCase == null)
                {
                    issues.Add(new CaseValidationIssue(CaseValidationSeverity.Error, $"중요 캐릭터 배치 {index + 1}번에 사건이 지정되지 않았습니다.", path));
                }

                if (slot.Position < 1 || slot.Position > day.TotalCitizenCount)
                {
                    issues.Add(new CaseValidationIssue(
                        CaseValidationSeverity.Error,
                        $"중요 캐릭터의 등장 순서 {slot.Position}는 1~{day.TotalCitizenCount} 범위여야 합니다.",
                        $"{path}.position"));
                }
                else if (!usedPositions.Add(slot.Position))
                {
                    issues.Add(new CaseValidationIssue(
                        CaseValidationSeverity.Error,
                        $"등장 순서 {slot.Position}에 중요 캐릭터가 중복 배치되었습니다.",
                        $"{path}.position"));
                }
            }

            if (day.StoryCitizenCount > day.TotalCitizenCount)
            {
                issues.Add(new CaseValidationIssue(CaseValidationSeverity.Error, "중요 캐릭터 수가 전체 처리 인원보다 많습니다.", "storyCases"));
            }
        }

        private static void ValidateConditionPools(DayDefinition day, ICollection<CaseValidationIssue> issues)
        {
            var ids = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            for (var index = 0; index < day.ActiveConditionPools.Count; index++)
            {
                var pool = day.ActiveConditionPools[index];
                var path = $"activeConditionPools.Array.data[{index}]";
                if (pool == null)
                {
                    issues.Add(new CaseValidationIssue(CaseValidationSeverity.Error, $"활성 조건 풀 {index + 1}번이 비어 있습니다.", path));
                    continue;
                }

                if (pool.Options.Count == 0)
                {
                    issues.Add(new CaseValidationIssue(CaseValidationSeverity.Error, $"조건 풀 '{pool.name}'에 선택지가 없습니다.", path));
                }

                if (!string.IsNullOrWhiteSpace(pool.PoolId) && !ids.Add(pool.PoolId))
                {
                    issues.Add(new CaseValidationIssue(CaseValidationSeverity.Error, $"조건 풀 ID '{pool.PoolId}'가 중복 등록되었습니다.", path));
                }

                var missingFeatures = pool.RequiredFeatures & ~day.UnlockedFeatures;
                if (missingFeatures != DayFeatureFlags.None)
                {
                    issues.Add(new CaseValidationIssue(
                        CaseValidationSeverity.Error,
                        $"조건 풀 '{pool.name}'에 필요한 기능이 해금되지 않았습니다: {missingFeatures}",
                        path));
                }
            }

            if (day.GeneralCitizenCount > 0 && day.ActiveConditionPools.Count == 0)
            {
                issues.Add(new CaseValidationIssue(CaseValidationSeverity.Warning, "일반 시민을 생성하지만 활성 조건 풀이 없습니다.", "activeConditionPools"));
            }
        }

        private static void ValidateFakeConditions(DayDefinition day, ICollection<CaseValidationIssue> issues)
        {
            if (day.FakeCitizenCount > 0 && day.AvailableFakeConditions.Count == 0)
            {
                issues.Add(new CaseValidationIssue(CaseValidationSeverity.Error, "가짜 일반 시민이 있지만 사용 가능한 가짜 조건이 없습니다.", "availableFakeConditions"));
                return;
            }

            var ids = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            for (var index = 0; index < day.AvailableFakeConditions.Count; index++)
            {
                var condition = day.AvailableFakeConditions[index];
                var path = $"availableFakeConditions.Array.data[{index}]";
                if (condition == null)
                {
                    issues.Add(new CaseValidationIssue(CaseValidationSeverity.Error, $"가짜 조건 {index + 1}번이 비어 있습니다.", path));
                    continue;
                }

                if (!string.IsNullOrWhiteSpace(condition.ConditionId) && !ids.Add(condition.ConditionId))
                {
                    issues.Add(new CaseValidationIssue(CaseValidationSeverity.Error, $"가짜 조건 ID '{condition.ConditionId}'가 중복 등록되었습니다.", path));
                }
                else if (string.IsNullOrWhiteSpace(condition.ConditionId))
                {
                    issues.Add(new CaseValidationIssue(CaseValidationSeverity.Error, $"가짜 조건 {index + 1}번의 ID가 비어 있습니다.", path));
                }

                var missingFeatures = condition.RequiredFeatures & ~day.UnlockedFeatures;
                if (missingFeatures != DayFeatureFlags.None)
                {
                    issues.Add(new CaseValidationIssue(
                        CaseValidationSeverity.Error,
                        $"가짜 조건 '{condition.name}'에 필요한 기능이 해금되지 않았습니다: {missingFeatures}",
                        path));
                }

                if (condition.MutationMode == FakeMutationMode.ReplaceFromPool && condition.ReplacementPool == null)
                {
                    issues.Add(new CaseValidationIssue(
                        CaseValidationSeverity.Error,
                        $"가짜 조건 '{condition.name}'은 대체 값 조건 풀이 필요합니다.",
                        path));
                }

                else if (condition.MutationMode == FakeMutationMode.ReplaceFromPool)
                {
                    var replacementMissingFeatures = condition.ReplacementPool.RequiredFeatures & ~day.UnlockedFeatures;
                    if (replacementMissingFeatures != DayFeatureFlags.None)
                    {
                        issues.Add(new CaseValidationIssue(
                            CaseValidationSeverity.Error,
                            $"가짜 조건 '{condition.name}'의 대체 값 풀에 필요한 기능이 해금되지 않았습니다: {replacementMissingFeatures}",
                            path));
                    }
                }

                if (condition.MutationMode == FakeMutationMode.UseFixedValue &&
                    (condition.FixedValue == null ||
                     condition.FixedValue.Field != condition.TargetField ||
                     !condition.FixedValue.HasExpectedValueType))
                {
                    issues.Add(new CaseValidationIssue(
                        CaseValidationSeverity.Error,
                        $"가짜 조건 '{condition.name}'의 고정 대체 값 필드와 형식을 확인하세요.",
                        path));
                }
            }
        }

        private static void ValidateRegulations(DayDefinition day, ICollection<CaseValidationIssue> issues)
        {
            var ids = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            for (var index = 0; index < day.ActiveRegulations.Count; index++)
            {
                var regulation = day.ActiveRegulations[index];
                var path = $"activeRegulations.Array.data[{index}]";
                if (regulation == null)
                {
                    issues.Add(new CaseValidationIssue(CaseValidationSeverity.Error, $"활성 규정 {index + 1}번이 비어 있습니다.", path));
                    continue;
                }

                if (string.IsNullOrWhiteSpace(regulation.RegulationId))
                {
                    issues.Add(new CaseValidationIssue(CaseValidationSeverity.Error, $"규정 '{regulation.name}'의 ID가 비어 있습니다.", path));
                }
                else if (!ids.Add(regulation.RegulationId))
                {
                    issues.Add(new CaseValidationIssue(CaseValidationSeverity.Error, $"규정 ID '{regulation.RegulationId}'가 중복 등록되었습니다.", path));
                }

                var missingFeatures = regulation.RequiredFeatures & ~day.UnlockedFeatures;
                if (missingFeatures != DayFeatureFlags.None)
                {
                    issues.Add(new CaseValidationIssue(
                        CaseValidationSeverity.Error,
                        $"규정 '{regulation.name}'에 필요한 기능이 해금되지 않았습니다: {missingFeatures}",
                        path));
                }

                if (regulation.Kind == DayRegulationKind.RelatedPersonConsent)
                {
                    const DayFeatureFlags consentFeatures =
                        DayFeatureFlags.MemoryStructureReport | DayFeatureFlags.RelatedPersonConsent;
                    var undeclaredFeatures = consentFeatures & ~regulation.RequiredFeatures;
                    if (undeclaredFeatures != DayFeatureFlags.None)
                    {
                        issues.Add(new CaseValidationIssue(
                            CaseValidationSeverity.Warning,
                            $"관계인 동의서 규정 '{regulation.name}'의 필요 기능에 {undeclaredFeatures}도 지정하는 것을 권장합니다.",
                            path));
                    }
                }
            }
        }
    }
}
