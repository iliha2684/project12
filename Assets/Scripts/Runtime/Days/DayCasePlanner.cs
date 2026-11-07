using System;
using System.Collections.Generic;
using ProjectED.Cases;

namespace ProjectED.Days
{
    // UI-independent runtime placement instructions for today's special NPCs.
    // Uses the same DayDefinition asset without mutating ScriptableObjects.
    public sealed class RuntimeStoryCaseEntry
    {
        public CitizenCaseData CaseData { get; }
        public int PreferredPosition { get; }

        public RuntimeStoryCaseEntry(CitizenCaseData caseData, int preferredPosition)
        {
            CaseData = caseData;
            PreferredPosition = preferredPosition;
        }
    }

    public static class RuntimeStoryCaseOverrides
    {
        private static DayDefinition configuredDay;
        private static readonly List<RuntimeStoryCaseEntry> additions = new List<RuntimeStoryCaseEntry>();
        private static readonly HashSet<CitizenCaseData> suppressedCases = new HashSet<CitizenCaseData>();

        public static void Configure(
            DayDefinition day,
            IEnumerable<RuntimeStoryCaseEntry> extraCases,
            IEnumerable<CitizenCaseData> disabledCases)
        {
            configuredDay = day;
            additions.Clear();
            suppressedCases.Clear();

            if (extraCases != null)
            {
                foreach (var entry in extraCases)
                {
                    if (entry != null && entry.CaseData != null)
                    {
                        additions.Add(entry);
                    }
                }
            }

            if (disabledCases != null)
            {
                foreach (var caseData in disabledCases)
                {
                    if (caseData != null)
                    {
                        suppressedCases.Add(caseData);
                    }
                }
            }
        }

        public static bool IsSuppressed(DayDefinition day, CitizenCaseData caseData)
        {
            return ReferenceEquals(configuredDay, day) &&
                   caseData != null && suppressedCases.Contains(caseData);
        }

        public static IReadOnlyList<RuntimeStoryCaseEntry> GetExtraCases(DayDefinition day)
        {
            return ReferenceEquals(configuredDay, day)
                ? additions
                : Array.Empty<RuntimeStoryCaseEntry>();
        }
    }

    public sealed class DayPlanBuildResult
    {
        internal DayPlanBuildResult(DayPlan plan, List<CitizenGenerationIssue> issues)
        {
            Plan = plan;
            Issues = issues;
        }

        public DayPlan Plan { get; }
        public IReadOnlyList<CitizenGenerationIssue> Issues { get; }

        public bool Success
        {
            get
            {
                if (Plan == null)
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

                return Plan.FakeGeneralCitizenCount == Plan.Definition.FakeCitizenCount;
            }
        }
    }

    public sealed class DayCasePlanner
    {
        private readonly GeneralCitizenGenerator citizenGenerator;
        private readonly FakeConditionInjector fakeInjector;

        public DayCasePlanner()
            : this(new GeneralCitizenGenerator(), new FakeConditionInjector())
        {
        }

        public DayCasePlanner(GeneralCitizenGenerator citizenGenerator, FakeConditionInjector fakeInjector)
        {
            this.citizenGenerator = citizenGenerator ?? throw new ArgumentNullException(nameof(citizenGenerator));
            this.fakeInjector = fakeInjector ?? throw new ArgumentNullException(nameof(fakeInjector));
        }

        public DayPlanBuildResult Build(DayDefinition day, int daySeed)
        {
            var issues = new List<CitizenGenerationIssue>();
            if (day == null)
            {
                issues.Add(new CitizenGenerationIssue(CaseValidationSeverity.Error, "하루 계획을 만들 DayDefinition이 없습니다."));
                return new DayPlanBuildResult(null, issues);
            }

            foreach (var validationIssue in DayDefinitionValidator.Validate(day))
            {
                issues.Add(new CitizenGenerationIssue(validationIssue.Severity, validationIssue.Message));
            }

            if (HasErrors(issues))
            {
                return new DayPlanBuildResult(null, issues);
            }

            var citizens = new CitizenCaseInstance[day.TotalCitizenCount];
            PlaceStoryCases(day, citizens);
            PlaceRuntimeStoryCases(day, citizens);
            GenerateGeneralCitizens(day, daySeed, citizens, issues);

            if (!HasErrors(issues))
            {
                ApplyFakeConditions(day, daySeed, citizens, issues);
            }

            // DAY 10: the four archive contradictions appear in fixed slots.
            // Keep the cases as general citizens so FakeCitizenCount remains 4.
            if (!HasErrors(issues) && day.DayNumber == 10)
                AssignDay10CitizenNumbers(citizens, issues);

            var plan = new DayPlan(day, daySeed, citizens);

            // Register the actual generated citizen numbers in the archive.
            // Unlike hard-coded Inspector data, these always match the same day's queue.
            if (!HasErrors(issues) && day.DayNumber == 10)
            {
                GameManager game = GameManager.Instance;
                Day10ArchiveRecords archive = game != null
                    ? game.GetComponent<Day10ArchiveRecords>() : null;
                if (archive == null || !archive.PrepareFixedScenarios(plan))
                    issues.Add(new CitizenGenerationIssue(CaseValidationSeverity.Error,
                        "DAY 10: 기록 4개 생성에 실패했습니다. GameManager의 Day10ArchiveRecords와 시민 필드를 확인하세요."));
            }

            return new DayPlanBuildResult(plan, issues);
        }

        private static void PlaceStoryCases(DayDefinition day, CitizenCaseInstance[] citizens)
        {
            int passes = day.DayNumber == 10 ? 2 : 1;
            for (int pass = 0; pass < passes; pass++)
            {
                foreach (var slot in day.StoryCases)
                {
                    if (slot?.CitizenCase == null || slot.Position < 1 ||
                        slot.Position > citizens.Length)
                        continue;

                    if (RuntimeStoryCaseOverrides.IsSuppressed(day, slot.CitizenCase))
                        continue;

                    // DAY10: retain existing fixed odd slots first. Only after
                    // that, relocate conflicting even-slot story NPCs.
                    bool reserved = IsDay10ArchivePosition(slot.Position);
                    if (day.DayNumber == 10 && reserved != (pass == 1))
                        continue;

                    int index = slot.Position - 1;
                // DAY 10 positions 2,4,6,8 are reserved for archive cases.
                // Story NPCs are moved to the nearest available odd slot.
                if (day.DayNumber == 10 && IsDay10ArchivePosition(slot.Position))
                {
                    index = FindClosestEmpty(citizens, slot.Position, true);
                    if (index < 0)
                    {
                        UnityEngine.Debug.LogError("DAY 10: 중요 NPC를 위한 홀수 자리가 없습니다.");
                        continue;
                    }
                }

                if (citizens[index] != null)
                {
                    UnityEngine.Debug.LogWarning("DAY " + day.DayNumber +
                        ": 중복된 중요 NPC 등장 순서 " + (index + 1));
                    continue;
                }

                    citizens[index] = CitizenCaseInstance.CreateStory(
                        slot.CitizenCase, day.DayId, index + 1);
                }
            }
        }

        private static void PlaceRuntimeStoryCases(DayDefinition day, CitizenCaseInstance[] citizens)
        {
            foreach (var entry in RuntimeStoryCaseOverrides.GetExtraCases(day))
            {
                if (entry.CaseData == null || AlreadyPlaced(citizens, entry.CaseData))
                {
                    continue;
                }

                // FakeCitizenCount counts ordinary citizens, not story NPCs.
                // Do not insert a story NPC if it would leave too few ordinary slots.
                if (CountEmpty(citizens) <= day.FakeCitizenCount)
                {
                    UnityEngine.Debug.LogWarning(
                        "DAY " + day.DayNumber + ": Not enough ordinary slots for " +
                        entry.CaseData.name + ". Check TotalCitizenCount / FakeCitizenCount / story NPCs.");
                    continue;
                }

                int index = FindClosestEmpty(citizens, entry.PreferredPosition,
                    day.DayNumber == 10);
                if (index < 0)
                {
                    UnityEngine.Debug.LogWarning("No free story slot for " + entry.CaseData.name);
                    continue;
                }

                citizens[index] = CitizenCaseInstance.CreateStory(entry.CaseData, day.DayId, index + 1);
                UnityEngine.Debug.Log("DAY " + day.DayNumber + " NPC scheduled: " +
                                      entry.CaseData.name + " at position " + (index + 1));
            }
        }

        private static bool AlreadyPlaced(CitizenCaseInstance[] citizens, CitizenCaseData caseData)
        {
            foreach (var citizen in citizens)
            {
                if (citizen != null && citizen.SourceCase == caseData)
                {
                    return true;
                }
            }
            return false;
        }

        private static int CountEmpty(CitizenCaseInstance[] citizens)
        {
            int count = 0;
            foreach (var citizen in citizens)
            {
                if (citizen == null) count++;
            }
            return count;
        }

        // PreferredPosition uses the same 1-based indexing as DayDefinition.StoryCases.
        private static int FindClosestEmpty(CitizenCaseInstance[] citizens,
            int preferredPosition, bool reserveDay10ArchiveSlots = false)
        {
            if (citizens.Length == 0) return -1;
            int preferred = Math.Max(0, Math.Min(citizens.Length - 1, preferredPosition - 1));
            for (int distance = 0; distance < citizens.Length; distance++)
            {
                int right = preferred + distance;
                if (right < citizens.Length && citizens[right] == null &&
                    (!reserveDay10ArchiveSlots || !IsDay10ArchivePosition(right + 1)))
                    return right;

                int left = preferred - distance;
                if (left >= 0 && citizens[left] == null &&
                    (!reserveDay10ArchiveSlots || !IsDay10ArchivePosition(left + 1)))
                    return left;
            }
            return -1;
        }

        private static bool IsDay10ArchivePosition(int position)
        {
            return position == 2 || position == 4 || position == 6 || position == 8;
        }

        // DAY 10 may reuse the same identity pool option for multiple citizens.
        // Give all general citizens consistent identifiers (DAY10 visitor 6
        // deliberately revisits as visitor 2). Archive uses these final values.
        private static void AssignDay10CitizenNumbers(
            CitizenCaseInstance[] citizens, ICollection<CitizenGenerationIssue> issues)
        {
            if (citizens.Length != 8)
                return;

            foreach (int position in new[] { 2, 4, 6, 8 })
            {
                if (citizens[position - 1] == null || citizens[position - 1].IsStoryCase)
                {
                    issues.Add(new CitizenGenerationIssue(CaseValidationSeverity.Error,
                        "DAY 10 기록 대상 " + position + "번째 일반 시민이 없습니다."));
                }
            }
            if (HasErrors(issues)) return;

            for (int index = 0; index < citizens.Length; index++)
            {
                CitizenCaseInstance citizen = citizens[index];
                if (citizen == null || citizen.IsStoryCase)
                    continue;

                // The sixth visitor returns under the same identifier as the
                // second visitor: a real revisit tied to the death register.
                int identityPosition = index + 1 == 6 ? 2 : index + 1;
                string number = "D10-2048-" + identityPosition.ToString("D3");
                citizen.SetValue(GeneratedCitizenField.IdCardCitizenNumber, number);
                citizen.SetValue(GeneratedCitizenField.ApplicationCitizenNumber, number);
                citizen.SetValue(GeneratedCitizenField.SearchCitizenNumber, number);

                // The two incident scenarios require genuine visible fields to
                // compare with the archive; provide deterministic defaults when
                // an older DAY 1 pool has not generated them.
                if (index + 1 == 4)
                {
                    string name;
                    if (!citizen.TryGetValue<string>(GeneratedCitizenField.IdCardFullName,
                        out name) || string.IsNullOrWhiteSpace(name))
                    {
                        citizen.SetValue(GeneratedCitizenField.IdCardFullName, "10일차 시민 4");
                        citizen.SetValue(GeneratedCitizenField.ApplicationApplicantName, "10일차 시민 4");
                    }
                }
                if (index + 1 == 8)
                {
                    string eventName;
                    if (!citizen.TryGetValue<string>(GeneratedCitizenField.ApplicationTargetEvent,
                        out eventName) || string.IsNullOrWhiteSpace(eventName))
                        citizen.SetValue(GeneratedCitizenField.ApplicationTargetEvent,
                            "기억 제거 신청 사건 D10-08");
                }
            }
        }

        private void GenerateGeneralCitizens(
            DayDefinition day,
            int daySeed,
            CitizenCaseInstance[] citizens,
            ICollection<CitizenGenerationIssue> issues)
        {
            for (var index = 0; index < citizens.Length; index++)
            {
                if (citizens[index] != null)
                {
                    continue;
                }

                var position = index + 1;
                var result = citizenGenerator.Generate(day, daySeed, position);
                citizens[index] = result.Citizen;
                foreach (var issue in result.Issues)
                {
                    issues.Add(new CitizenGenerationIssue(
                        issue.Severity,
                        $"{position}번째 일반 시민: {issue.Message}"));
                }
            }
        }

        private void ApplyFakeConditions(
            DayDefinition day,
            int daySeed,
            CitizenCaseInstance[] citizens,
            ICollection<CitizenGenerationIssue> issues)
        {
            if (day.FakeCitizenCount == 0)
            {
                return;
            }

            var candidates = new List<CitizenCaseInstance>();
            foreach (var citizen in citizens)
            {
                // DAYS 1-4: the first visitor is always normal by design.
                if (citizen != null && !citizen.IsStoryCase &&
                    !(day.DayNumber >= 1 && day.DayNumber <= 4 && citizen.SequenceIndex == 1))
                {
                    candidates.Add(citizen);
                }
            }

            var random = new Random(CombineSeed(daySeed, day.DayNumber, 7919));

            if (day.DayNumber == 10)
            {
                if (day.TotalCitizenCount != 8 || day.FakeCitizenCount != 4)
                {
                    issues.Add(new CitizenGenerationIssue(CaseValidationSeverity.Error,
                        "DAY 10: 전체 시민 8명, 가짜 일반 시민 4명으로 설정해야 합니다."));
                    return;
                }

                // Use exactly the four narrative positions, while preserving
                // each fake citizen's FakeCondition metadata and count.
                candidates.Clear();
                foreach (int position in new[] { 2, 4, 6, 8 })
                {
                    CitizenCaseInstance target = citizens[position - 1];
                    if (target == null || target.IsStoryCase)
                    {
                        issues.Add(new CitizenGenerationIssue(CaseValidationSeverity.Error,
                            "DAY 10: " + position +
                            "번째 자리는 일반 시민이어야 합니다. 중요 NPC 배치를 확인하세요."));
                        return;
                    }
                    candidates.Add(target);
                }
            }
            else
            {
                Shuffle(candidates, random);
            }

            var appliedCount = 0;
            foreach (var citizen in candidates)
            {
                if (appliedCount >= day.FakeCitizenCount)
                {
                    break;
                }

                var result = fakeInjector.ApplyRandom(day, citizen, random);
                if (!result.Success)
                {
                    continue;
                }

                appliedCount++;
                foreach (var issue in result.Issues)
                {
                    issues.Add(new CitizenGenerationIssue(
                        issue.Severity,
                        $"{citizen.SequenceIndex}번째 시민 가짜 조건: {issue.Message}"));
                }
            }

            if (appliedCount != day.FakeCitizenCount)
            {
                issues.Add(new CitizenGenerationIssue(
                    CaseValidationSeverity.Error,
                    $"가짜 일반 시민을 {day.FakeCitizenCount}명 만들어야 하지만 {appliedCount}명만 만들었습니다. " +
                    "일반 시민의 태그, 변조 대상 필드, 가짜 조건의 호환 설정을 확인하세요."));
            }
        }

        private static void Shuffle<T>(IList<T> items, Random random)
        {
            for (var index = items.Count - 1; index > 0; index--)
            {
                var swapIndex = random.Next(index + 1);
                (items[index], items[swapIndex]) = (items[swapIndex], items[index]);
            }
        }

        private static bool HasErrors(IEnumerable<CitizenGenerationIssue> issues)
        {
            foreach (var issue in issues)
            {
                if (issue.Severity == CaseValidationSeverity.Error)
                {
                    return true;
                }
            }

            return false;
        }

        private static int CombineSeed(int daySeed, int dayNumber, int salt)
        {
            unchecked
            {
                var seed = daySeed;
                seed = (seed * 397) ^ dayNumber;
                seed = (seed * 397) ^ salt;
                return seed;
            }
        }
    }
}
