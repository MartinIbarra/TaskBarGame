using System;
using System.Collections.Generic;
using System.Linq;
using TaskbarTactics.Core.Models;
using TaskbarTactics.Core.Stats;

namespace TaskbarTactics.Core.Equipment
{
    // Equipment and tooltip consume the same breakdown. Random bonuses are never rarity-scaled.
    public sealed class ItemStatBreakdown
    {
        public IReadOnlyList<StatModifier> Base { get; }
        public IReadOnlyList<StatModifier> Rarity { get; }
        public IReadOnlyList<StatModifier> Extras { get; }

        public ItemStatBreakdown(IEnumerable<StatModifier> baseStats, float multiplier,
            IEnumerable<StatModifier> extras)
        {
            Base = Copy(baseStats);
            Extras = Copy(extras);
            var improvements = new List<StatModifier>();
            foreach (StatModifier stat in Base)
            {
                float total = stat.Value * Math.Max(1f, multiplier);
                if (stat.Operation == StatModifierOperation.Flat &&
                    (stat.Stat == HeroStatType.MaxHealth || stat.Stat == HeroStatType.MaxMana))
                    total = (float)Math.Round(total, MidpointRounding.AwayFromZero);
                float bonus = total - stat.Value;
                if (Math.Abs(bonus) > 0.00001f)
                    improvements.Add(new StatModifier(stat.Stat, stat.Operation, bonus, "item_rarity"));
            }
            Rarity = improvements;
        }

        public List<StatModifier> CollectModifiers() => Base.Concat(Rarity).Concat(Extras)
            .Select(stat => stat.Scaled(1f)).ToList();

        private static List<StatModifier> Copy(IEnumerable<StatModifier> stats) =>
            stats?.Where(stat => stat != null).Select(stat => stat.Scaled(1f)).ToList()
            ?? new List<StatModifier>();
    }
}
