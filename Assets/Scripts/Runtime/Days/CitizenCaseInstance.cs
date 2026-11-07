using System;
using System.Collections.Generic;
using ProjectED.Cases;
using UnityEngine;

namespace ProjectED.Days
{
    public readonly struct SelectedCitizenCondition
    {
        public SelectedCitizenCondition(
            string poolId,
            CitizenConditionCategory category,
            string optionId,
            string displayName)
        {
            PoolId = poolId;
            Category = category;
            OptionId = optionId;
            DisplayName = displayName;
        }

        public string PoolId { get; }
        public CitizenConditionCategory Category { get; }
        public string OptionId { get; }
        public string DisplayName { get; }
    }

    public sealed class CitizenCaseInstance
    {
        private readonly Dictionary<GeneratedCitizenField, object> values = new();
        private readonly Dictionary<GeneratedCitizenField, object> originalValues = new();
        private readonly HashSet<string> tags = new(StringComparer.OrdinalIgnoreCase);
        private readonly List<SelectedCitizenCondition> selectedConditions = new();

        internal CitizenCaseInstance(string instanceId, string dayId, int sequenceIndex)
        {
            InstanceId = instanceId;
            DayId = dayId;
            SequenceIndex = sequenceIndex;
            ExpectedDisposition = CaseDisposition.ExtractionApproval;
        }

        public string InstanceId { get; }
        public string DayId { get; }
        public int SequenceIndex { get; }
        public bool IsStoryCase { get; private set; }
        public bool IsFake { get; private set; }
        public CitizenCaseData SourceCase { get; private set; }
        public FakeConditionData AppliedFakeCondition { get; private set; }
        public CaseDisposition ExpectedDisposition { get; internal set; }
        public string JudgmentReason { get; internal set; }
        public IReadOnlyDictionary<GeneratedCitizenField, object> Values => values;
        public IReadOnlyDictionary<GeneratedCitizenField, object> OriginalValues => originalValues;
        public IReadOnlyCollection<string> Tags => tags;
        public IReadOnlyList<SelectedCitizenCondition> SelectedConditions => selectedConditions;
        internal ISet<string> TagSet => tags;

        public string DisplayName
        {
            get
            {
                var generatedName = GetValue<string>(GeneratedCitizenField.DisplayName);
                if (!string.IsNullOrWhiteSpace(generatedName))
                {
                    return generatedName;
                }

                return SourceCase?.Citizen?.DisplayName;
            }
        }

        public Sprite Portrait =>
            GetValue<Sprite>(GeneratedCitizenField.Portrait) ?? SourceCase?.Portrait;

        public bool HasTag(string tag)
        {
            return !string.IsNullOrWhiteSpace(tag) && tags.Contains(tag);
        }

        public bool HasValue(GeneratedCitizenField field)
        {
            return values.ContainsKey(field);
        }

        public bool TryGetValue<T>(GeneratedCitizenField field, out T value)
        {
            if (values.TryGetValue(field, out var boxedValue) && boxedValue is T typedValue)
            {
                value = typedValue;
                return true;
            }

            value = default;
            return false;
        }

        public T GetValue<T>(GeneratedCitizenField field, T fallback = default)
        {
            return TryGetValue<T>(field, out var value) ? value : fallback;
        }

        public bool TryGetOriginalValue<T>(GeneratedCitizenField field, out T value)
        {
            if (originalValues.TryGetValue(field, out var boxedValue) && boxedValue is T typedValue)
            {
                value = typedValue;
                return true;
            }

            value = default;
            return false;
        }

        public string GetText(GeneratedCitizenField field, string fallback = "")
        {
            return GetValue(field, fallback);
        }

        internal bool SetValue(GeneratedCitizenField field, object value)
        {
            var overwroteExistingValue = values.ContainsKey(field);
            values[field] = value;
            return overwroteExistingValue;
        }

        internal void MutateValue(GeneratedCitizenField field, object value)
        {
            if (!originalValues.ContainsKey(field))
            {
                values.TryGetValue(field, out var originalValue);
                originalValues[field] = originalValue;
            }

            values[field] = value;
        }

        internal void AddTag(string tag)
        {
            if (!string.IsNullOrWhiteSpace(tag))
            {
                tags.Add(tag.Trim());
            }
        }

        internal void RecordSelection(CitizenConditionPoolData pool, CitizenConditionOption option)
        {
            selectedConditions.Add(new SelectedCitizenCondition(
                pool.PoolId,
                pool.Category,
                option.OptionId,
                option.DisplayName));
        }

        internal void MarkAsFake(FakeConditionData fakeCondition)
        {
            IsFake = fakeCondition != null;
            AppliedFakeCondition = fakeCondition;
            if (fakeCondition == null)
            {
                return;
            }

            ExpectedDisposition = fakeCondition.ExpectedDisposition;
            JudgmentReason = fakeCondition.DisplayName;
            AddTag("citizen:fake");
            AddTag($"fake:{fakeCondition.ConditionId}");
        }

        // Called only after the save system validates the entire reconstructed DAY.
        // Preserve the same CitizenCaseInstance so all event and UI references remain valid.
        internal void RestoreSavedDocuments(
            IDictionary<GeneratedCitizenField, object> restoredValues,
            IDictionary<GeneratedCitizenField, object> restoredOriginals,
            IEnumerable<string> restoredTags,
            IEnumerable<SelectedCitizenCondition> restoredConditions,
            bool isFake, FakeConditionData appliedFake,
            CaseDisposition expected, string reason)
        {
            values.Clear();
            foreach (var entry in restoredValues) values[entry.Key] = entry.Value;
            originalValues.Clear();
            foreach (var entry in restoredOriginals) originalValues[entry.Key] = entry.Value;
            tags.Clear();
            if (restoredTags != null)
                foreach (var tag in restoredTags) AddTag(tag);
            selectedConditions.Clear();
            if (restoredConditions != null)
                selectedConditions.AddRange(restoredConditions);
            IsFake = isFake;
            AppliedFakeCondition = appliedFake;
            ExpectedDisposition = expected;
            JudgmentReason = reason;
        }

        public static CitizenCaseInstance CreateStory(CitizenCaseData sourceCase, string dayId, int sequenceIndex)
        {
            if (sourceCase == null)
            {
                throw new ArgumentNullException(nameof(sourceCase));
            }

            var normalizedDayId = string.IsNullOrWhiteSpace(dayId) ? "DAY" : dayId.Trim();
            var instance = new CitizenCaseInstance(
                $"{normalizedDayId}_STORY_{sequenceIndex:00}",
                normalizedDayId,
                sequenceIndex)
            {
                IsStoryCase = true,
                IsFake = sourceCase.IsFakeCitizen,
                SourceCase = sourceCase,
                ExpectedDisposition = sourceCase.Judgment?.ExpectedDisposition ?? CaseDisposition.ExtractionApproval,
                JudgmentReason = sourceCase.Judgment?.JudgmentReason
            };
            instance.AddTag("citizen:story");
            if (sourceCase.IsFakeCitizen)
            {
                instance.AddTag("citizen:fake");
            }
            return instance;
        }
    }
}
