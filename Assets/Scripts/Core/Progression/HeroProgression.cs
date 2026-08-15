using System;

namespace TaskbarTactics.Core.Progression
{
    [Serializable]
    public sealed class HeroProgression
    {
        public int Level { get; private set; }
        public long Experience { get; private set; }

        public HeroProgression(int level, long experience)
        {
            Level = Math.Max(1, level);
            Experience = Math.Max(0L, experience);
            ResolveLevelUps();
        }

        public int AddExperience(long amount)
        {
            if (amount < 0L)
            {
                throw new ArgumentOutOfRangeException(nameof(amount));
            }

            Experience = SafeAdd(Experience, amount);
            return ResolveLevelUps();
        }

        public static long ExperienceRequiredForLevel(int currentLevel)
        {
            return Math.Max(1, currentLevel) * 100L;
        }

        private int ResolveLevelUps()
        {
            int gained = 0;
            while (Level < int.MaxValue)
            {
                long required = ExperienceRequiredForLevel(Level);
                if (Experience < required)
                {
                    break;
                }

                Experience -= required;
                Level++;
                gained++;
            }

            return gained;
        }

        private static long SafeAdd(long left, long right)
        {
            return right > long.MaxValue - left ? long.MaxValue : left + right;
        }
    }
}
