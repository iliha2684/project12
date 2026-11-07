using System;
using System.Collections.Generic;

namespace ProjectED.Days
{
    public sealed class DayPlan
    {
        private readonly IReadOnlyList<CitizenCaseInstance> citizens;

        internal DayPlan(DayDefinition definition, int seed, IList<CitizenCaseInstance> citizens)
        {
            Definition = definition ?? throw new ArgumentNullException(nameof(definition));
            Seed = seed;
            var snapshot = new CitizenCaseInstance[citizens.Count];
            citizens.CopyTo(snapshot, 0);
            this.citizens = Array.AsReadOnly(snapshot);
        }

        public DayDefinition Definition { get; }
        public int Seed { get; }
        public IReadOnlyList<CitizenCaseInstance> Citizens => citizens;
        public int TotalCitizenCount => citizens.Count;

        public int StoryCitizenCount => Count(citizen => citizen.IsStoryCase);
        public int GeneralCitizenCount => Count(citizen => !citizen.IsStoryCase);
        public int FakeGeneralCitizenCount => Count(citizen => !citizen.IsStoryCase && citizen.IsFake);
        public int NormalGeneralCitizenCount => Count(citizen => !citizen.IsStoryCase && !citizen.IsFake);

        public CitizenCaseInstance GetCitizenAtPosition(int position)
        {
            if (position < 1 || position > citizens.Count)
            {
                throw new ArgumentOutOfRangeException(nameof(position), position, $"등장 순서는 1~{citizens.Count} 범위여야 합니다.");
            }

            return citizens[position - 1];
        }

        private int Count(Func<CitizenCaseInstance, bool> predicate)
        {
            var count = 0;
            foreach (var citizen in citizens)
            {
                if (citizen != null && predicate(citizen))
                {
                    count++;
                }
            }

            return count;
        }
    }
}
