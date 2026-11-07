using System;
using System.Collections.Generic;
using ProjectED.Cases;

namespace ProjectED.Days
{
    public readonly struct CitizenGenerationIssue
    {
        public CitizenGenerationIssue(CaseValidationSeverity severity, string message)
        {
            Severity = severity;
            Message = message;
        }

        public CaseValidationSeverity Severity { get; }
        public string Message { get; }
    }

    public sealed class CitizenGenerationResult
    {
        internal CitizenGenerationResult(CitizenCaseInstance citizen, List<CitizenGenerationIssue> issues)
        {
            Citizen = citizen;
            Issues = issues;
        }

        public CitizenCaseInstance Citizen { get; }
        public IReadOnlyList<CitizenGenerationIssue> Issues { get; }

        public bool Success
        {
            get
            {
                if (Citizen == null)
                {
                    return false;
                }

                foreach (var issue in Issues)
                {
                    if (issue.Severity == CaseValidationSeverity.Error)
                    {
                        return false;
                    }
                }

                return true;
            }
        }
    }

    public sealed class GeneralCitizenGenerator
    {
        public CitizenGenerationResult Generate(DayDefinition day, int daySeed, int sequenceIndex)
        {
            if (day == null)
            {
                return Failed("?¼ë°? ?‹œë¯¼ì„ ?ƒ?„±?•  DayDefinition?´ ?—†?Šµ?‹ˆ?‹¤.");
            }

            var citizenSeed = CombineSeed(daySeed, day.DayNumber, sequenceIndex);
            return Generate(day, new Random(citizenSeed), sequenceIndex);
        }

        public CitizenGenerationResult Generate(DayDefinition day, Random random, int sequenceIndex)
        {
            if (day == null)
            {
                return Failed("?¼ë°? ?‹œë¯¼ì„ ?ƒ?„±?•  DayDefinition?´ ?—†?Šµ?‹ˆ?‹¤.");
            }

            if (random == null)
            {
                return Failed("?¼ë°? ?‹œë¯? ?ƒ?„±?š© ?‚œ?ˆ˜ê¸°ê?? ?—†?Šµ?‹ˆ?‹¤.");
            }

            if (sequenceIndex < 1)
            {
                return Failed("?¼ë°? ?‹œë¯? ?ˆœë²ˆì?? 1 ?´?ƒ?´?–´?•¼ ?•©?‹ˆ?‹¤.");
            }

            var issues = new List<CitizenGenerationIssue>();
            var dayId = string.IsNullOrWhiteSpace(day.DayId) ? $"DAY_{day.DayNumber:00}" : day.DayId.Trim();
            var citizen = new CitizenCaseInstance(
                $"{dayId}_GENERAL_{sequenceIndex:00}",
                dayId,
                sequenceIndex);

            AddBaseTags(citizen, day);

            for (var index = 0; index < day.ActiveConditionPools.Count; index++)
            {
                var pool = day.ActiveConditionPools[index];
                if (pool == null)
                {
                    issues.Add(new CitizenGenerationIssue(
                        CaseValidationSeverity.Error,
                        $"?™œ?„± ì¡°ê±´ ??? {index + 1}ë²ˆì´ ë¹„ì–´ ?ˆ?Šµ?‹ˆ?‹¤."));
                    continue;
                }

                var missingFeatures = pool.RequiredFeatures & ~day.UnlockedFeatures;
                if (missingFeatures != DayFeatureFlags.None)
                {
                    issues.Add(new CitizenGenerationIssue(
                        CaseValidationSeverity.Error,
                        $"ì¡°ê±´ ??? '{GetPoolName(pool)}'?— ?•„?š”?•œ ê¸°ëŠ¥?´ ?•´ê¸ˆë˜ì§? ?•Š?•˜?Šµ?‹ˆ?‹¤: {missingFeatures}"));
                    continue;
                }

                if (!pool.TryChooseCompatible(random, citizen.TagSet, out var option))
                {
                    if (pool.RequiredForEveryCitizen)
                    {
                        issues.Add(new CitizenGenerationIssue(
                            CaseValidationSeverity.Error,
                            $"?•„?ˆ˜ ì¡°ê±´ ??? '{GetPoolName(pool)}'?—?„œ ?˜„?¬ ?ƒœê·¸ì?? ?˜¸?™˜?˜?Š” ?„ ?ƒì§?ë¥? ì°¾ì?? ëª»í–ˆ?Šµ?‹ˆ?‹¤."));
                    }
                    else
                    {
                        issues.Add(new CitizenGenerationIssue(
                            CaseValidationSeverity.Info,
                            $"?„ ?ƒ ì¡°ê±´ ??? '{GetPoolName(pool)}'??? ?˜¸?™˜?˜?Š” ?„ ?ƒì§?ê°? ?—†?–´ ê±´ë„ˆ?›°?—ˆ?Šµ?‹ˆ?‹¤."));
                    }

                    continue;
                }

                ApplyOption(citizen, pool, option, issues);
            }

            ValidateGeneratedCitizen(citizen, day, issues);
            ValidateDailyRegulations(citizen, day, issues);
            return new CitizenGenerationResult(citizen, issues);
        }

        private static void ApplyOption(
            CitizenCaseInstance citizen,
            CitizenConditionPoolData pool,
            CitizenConditionOption option,
            ICollection<CitizenGenerationIssue> issues)
        {
            citizen.RecordSelection(pool, option);
            citizen.AddTag($"pool:{pool.PoolId}");
            citizen.AddTag($"option:{option.OptionId}");

            foreach (var assignment in option.Values)
            {
                if (assignment == null)
                {
                    issues.Add(new CitizenGenerationIssue(
                        CaseValidationSeverity.Error,
                        $"ì¡°ê±´ '{GetOptionName(option)}'?— ë¹„ì–´ ?ˆ?Š” ?•„?“œ ê°’ì´ ?ˆ?Šµ?‹ˆ?‹¤."));
                    continue;
                }

                if (!assignment.TryResolveValue(citizen, out var resolvedValue, out var resolveError))
                {
                    issues.Add(new CitizenGenerationIssue(
                        CaseValidationSeverity.Error,
                        $"ì¡°ê±´ '{GetOptionName(option)}': {resolveError}"));
                    continue;
                }

                if (citizen.SetValue(assignment.Field, resolvedValue))
                {
                    issues.Add(new CitizenGenerationIssue(
                        CaseValidationSeverity.Warning,
                        $"?•„?“œ '{assignment.Field}'ê°? ?’¤ìª? ì¡°ê±´ '{GetOptionName(option)}'?˜ ê°’ìœ¼ë¡? ?®?–´?¨ì¡ŒìŠµ?‹ˆ?‹¤."));
                }
            }

            foreach (var tag in option.ProvidedTags)
            {
                citizen.AddTag(tag);
            }
        }

        private static void ValidateGeneratedCitizen(
            CitizenCaseInstance citizen,
            DayDefinition day,
            ICollection<CitizenGenerationIssue> issues)
        {
            if (string.IsNullOrWhiteSpace(citizen.DisplayName))
            {
                issues.Add(new CitizenGenerationIssue(
                    CaseValidationSeverity.Warning,
                    "?ƒ?„±?œ ?‹œë¯¼ì˜ ?‘œ?‹œ ?´ë¦„ì´ ?—†?Šµ?‹ˆ?‹¤. Identity ì¡°ê±´ ????—?„œ DisplayName?„ ì§?? •?•˜?Š” ê²ƒì„ ê¶Œì¥?•©?‹ˆ?‹¤."));
            }

            if (day.HasFeature(DayFeatureFlags.CitizenIdCard) &&
                !citizen.HasValue(GeneratedCitizenField.IdCardSubmitted))
            {
                issues.Add(new CitizenGenerationIssue(
                    CaseValidationSeverity.Info,
                    "?‹œë¯¼ì¦ ê¸°ëŠ¥?´ ?•´ê¸ˆë˜?—ˆì§?ë§? IdCardSubmitted ê°’ì´ ?„¤? •?˜ì§? ?•Š?•˜?Šµ?‹ˆ?‹¤."));
            }
        }

        private static void AddBaseTags(CitizenCaseInstance citizen, DayDefinition day)
        {
            citizen.AddTag("citizen:general");
            citizen.AddTag($"day:{day.DayNumber}");

            foreach (DayFeatureFlags feature in Enum.GetValues(typeof(DayFeatureFlags)))
            {
                if (feature != DayFeatureFlags.None && day.HasFeature(feature))
                {
                    citizen.AddTag($"feature:{feature}");
                }
            }
        }

        private static void ValidateDailyRegulations(
            CitizenCaseInstance citizen,
            DayDefinition day,
            ICollection<CitizenGenerationIssue> issues)
        {
            foreach (var regulation in day.ActiveRegulations)
            {
                if (regulation == null)
                {
                    continue;
                }

                var result = regulation.Evaluate(citizen);
                if (result.Status == RegulationEvaluationStatus.MissingData)
                {
                    issues.Add(new CitizenGenerationIssue(
                        CaseValidationSeverity.Error,
                        $"ê·œì • '{GetRegulationName(regulation)}'?„ ?Œ? •?•  ?ˆ˜ ?—†?Šµ?‹ˆ?‹¤. {result.Message}"));
                }
                else if (result.Status == RegulationEvaluationStatus.Violated)
                {
                    issues.Add(new CitizenGenerationIssue(
                        CaseValidationSeverity.Error,
                        $"? •?ƒ ?¼ë°? ?‹œë¯¼ì´ ê·œì • '{GetRegulationName(regulation)}'?„ ?œ„ë°˜í–ˆ?Šµ?‹ˆ?‹¤. {result.Message}"));
                }
            }
        }

        private static int CombineSeed(int daySeed, int dayNumber, int sequenceIndex)
        {
            unchecked
            {
                var seed = daySeed;
                seed = (seed * 397) ^ dayNumber;
                seed = (seed * 397) ^ sequenceIndex;
                return seed;
            }
        }

        private static CitizenGenerationResult Failed(string message)
        {
            return new CitizenGenerationResult(
                null,
                new List<CitizenGenerationIssue>
                {
                    new(CaseValidationSeverity.Error, message)
                });
        }

        private static string GetPoolName(CitizenConditionPoolData pool)
        {
            return string.IsNullOrWhiteSpace(pool.DisplayName) ? pool.name : pool.DisplayName;
        }

        private static string GetOptionName(CitizenConditionOption option)
        {
            return string.IsNullOrWhiteSpace(option.DisplayName) ? option.OptionId : option.DisplayName;
        }

        private static string GetRegulationName(DayRegulationData regulation)
        {
            return string.IsNullOrWhiteSpace(regulation.DisplayName)
                ? regulation.RegulationId
                : regulation.DisplayName;
        }
    }

}
