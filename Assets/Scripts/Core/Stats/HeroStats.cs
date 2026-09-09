using System;
using System.Collections.Generic;
using System.Linq;
using TaskbarTactics.Core.Models;

namespace TaskbarTactics.Core.Stats
{
    [Serializable]
    public sealed class HeroStats
    {
        public int MaxHealth;
        public int MaxMana;
        public float AttackPower;
        public float SpellPower;
        public float Defense;
        public float MagicResistance;
        public float AttackSpeed;
        public float CastSpeed;
        public int AttackRange;
        public float CriticalChance;
        public float CriticalDamage;
        public float Accuracy;
        public float Evasion;
        public float HealthRegeneration;
        public float ManaRegeneration;
        public float CooldownReduction;

        public static HeroStats CreateDefault()
        {
            return new HeroStats
            {
                MaxHealth = 1,
                AttackSpeed = 1f,
                CastSpeed = 1f,
                AttackRange = 1,
                CriticalDamage = 1.5f,
                Accuracy = 100f
            };
        }

        public HeroStats Clone()
        {
            return (HeroStats)MemberwiseClone();
        }

        public float Get(HeroStatType stat)
        {
            switch (stat)
            {
                case HeroStatType.MaxHealth: return MaxHealth;
                case HeroStatType.MaxMana: return MaxMana;
                case HeroStatType.AttackPower: return AttackPower;
                case HeroStatType.SpellPower: return SpellPower;
                case HeroStatType.Defense: return Defense;
                case HeroStatType.MagicResistance: return MagicResistance;
                case HeroStatType.AttackSpeed: return AttackSpeed;
                case HeroStatType.CastSpeed: return CastSpeed;
                case HeroStatType.AttackRange: return AttackRange;
                case HeroStatType.CriticalChance: return CriticalChance;
                case HeroStatType.CriticalDamage: return CriticalDamage;
                case HeroStatType.Accuracy: return Accuracy;
                case HeroStatType.Evasion: return Evasion;
                case HeroStatType.HealthRegeneration: return HealthRegeneration;
                case HeroStatType.ManaRegeneration: return ManaRegeneration;
                case HeroStatType.CooldownReduction: return CooldownReduction;
                default: throw new ArgumentOutOfRangeException(nameof(stat), stat, null);
            }
        }

        public void Set(HeroStatType stat, float value)
        {
            switch (stat)
            {
                case HeroStatType.MaxHealth: MaxHealth = Round(value); break;
                case HeroStatType.MaxMana: MaxMana = Round(value); break;
                case HeroStatType.AttackPower: AttackPower = value; break;
                case HeroStatType.SpellPower: SpellPower = value; break;
                case HeroStatType.Defense: Defense = value; break;
                case HeroStatType.MagicResistance: MagicResistance = value; break;
                case HeroStatType.AttackSpeed: AttackSpeed = value; break;
                case HeroStatType.CastSpeed: CastSpeed = value; break;
                case HeroStatType.AttackRange: AttackRange = Round(value); break;
                case HeroStatType.CriticalChance: CriticalChance = value; break;
                case HeroStatType.CriticalDamage: CriticalDamage = value; break;
                case HeroStatType.Accuracy: Accuracy = value; break;
                case HeroStatType.Evasion: Evasion = value; break;
                case HeroStatType.HealthRegeneration: HealthRegeneration = value; break;
                case HeroStatType.ManaRegeneration: ManaRegeneration = value; break;
                case HeroStatType.CooldownReduction: CooldownReduction = value; break;
                default: throw new ArgumentOutOfRangeException(nameof(stat), stat, null);
            }
        }

        private static int Round(float value)
        {
            return (int)Math.Round(value, MidpointRounding.AwayFromZero);
        }
    }

    public static class HeroStatLimits
    {
        public const float MinAttackSpeed = 0.1f;
        public const float MaxAttackSpeed = 5f;
        public const float MinCastSpeed = 0.1f;
        public const float MaxCastSpeed = 4f;
        public const float MaxCooldownReduction = 75f;
        public const float MaxEvasion = 75f;
        public const float MaxCriticalChance = 100f;
        public const float MaxCriticalDamage = 5f;
        public const float MinHitChance = 5f;
        public const float MaxHitChance = 100f;
    }

    [Serializable]
    public sealed class StatModifier
    {
        public HeroStatType Stat;
        public StatModifierOperation Operation;
        public float Value;
        public string SourceId = string.Empty;

        public StatModifier()
        {
        }

        public StatModifier(
            HeroStatType stat,
            StatModifierOperation operation,
            float value,
            string sourceId = "")
        {
            Stat = stat;
            Operation = operation;
            Value = value;
            SourceId = sourceId ?? string.Empty;
        }

        public StatModifier Scaled(float multiplier)
        {
            return new StatModifier(Stat, Operation, Value * multiplier, SourceId);
        }
    }

    public static class HeroStatsCalculator
    {
        public static HeroStats Calculate(
            HeroStats baseStats,
            HeroStats growthPerLevel,
            int level,
            IEnumerable<StatModifier> modifiers = null)
        {
            if (baseStats == null)
            {
                throw new ArgumentNullException(nameof(baseStats));
            }

            int normalizedLevel = Math.Max(1, level);
            HeroStats result = baseStats.Clone();
            if (growthPerLevel != null)
            {
                foreach (HeroStatType stat in Enum.GetValues(typeof(HeroStatType)))
                {
                    result.Set(stat, result.Get(stat) +
                        growthPerLevel.Get(stat) * (normalizedLevel - 1));
                }
            }

            List<StatModifier> materialized = modifiers?
                .Where(item => item != null)
                .ToList() ?? new List<StatModifier>();
            foreach (HeroStatType stat in Enum.GetValues(typeof(HeroStatType)))
            {
                float value = result.Get(stat);
                value += materialized
                    .Where(item => item.Stat == stat &&
                                   item.Operation == StatModifierOperation.Flat)
                    .Sum(item => item.Value);
                float additivePercent = materialized
                    .Where(item => item.Stat == stat &&
                                   item.Operation == StatModifierOperation.AdditivePercent)
                    .Sum(item => item.Value);
                value *= 1f + additivePercent / 100f;
                foreach (StatModifier modifier in materialized.Where(item =>
                             item.Stat == stat &&
                             item.Operation == StatModifierOperation.MultiplicativePercent))
                {
                    value *= 1f + modifier.Value / 100f;
                }

                result.Set(stat, value);
            }

            Clamp(result);
            return result;
        }

        public static float CalculateHitChance(float accuracy, float evasion)
        {
            return Clamp(accuracy - evasion,
                HeroStatLimits.MinHitChance,
                HeroStatLimits.MaxHitChance);
        }

        public static float MitigateDamage(float rawDamage, float resistance)
        {
            float safeDamage = Math.Max(0f, rawDamage);
            float safeResistance = Math.Max(0f, resistance);
            return safeDamage * 100f / (100f + safeResistance);
        }

        private static void Clamp(HeroStats stats)
        {
            stats.MaxHealth = Math.Max(1, stats.MaxHealth);
            stats.MaxMana = Math.Max(0, stats.MaxMana);
            stats.AttackPower = Math.Max(0f, stats.AttackPower);
            stats.SpellPower = Math.Max(0f, stats.SpellPower);
            stats.Defense = Math.Max(0f, stats.Defense);
            stats.MagicResistance = Math.Max(0f, stats.MagicResistance);
            stats.AttackSpeed = Clamp(stats.AttackSpeed,
                HeroStatLimits.MinAttackSpeed,
                HeroStatLimits.MaxAttackSpeed);
            stats.CastSpeed = Clamp(stats.CastSpeed,
                HeroStatLimits.MinCastSpeed,
                HeroStatLimits.MaxCastSpeed);
            stats.AttackRange = Math.Max(1, stats.AttackRange);
            stats.CriticalChance = Clamp(stats.CriticalChance, 0f,
                HeroStatLimits.MaxCriticalChance);
            stats.CriticalDamage = Clamp(stats.CriticalDamage, 1f,
                HeroStatLimits.MaxCriticalDamage);
            stats.Accuracy = Math.Max(0f, stats.Accuracy);
            stats.Evasion = Clamp(stats.Evasion, 0f, HeroStatLimits.MaxEvasion);
            stats.HealthRegeneration = Math.Max(0f, stats.HealthRegeneration);
            stats.ManaRegeneration = Math.Max(0f, stats.ManaRegeneration);
            stats.CooldownReduction = Clamp(stats.CooldownReduction, 0f,
                HeroStatLimits.MaxCooldownReduction);
        }

        private static float Clamp(float value, float minimum, float maximum)
        {
            return Math.Max(minimum, Math.Min(maximum, value));
        }
    }
}
