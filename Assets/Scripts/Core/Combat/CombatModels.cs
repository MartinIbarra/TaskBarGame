using System;
using System.Collections.Generic;
using TaskbarTactics.Core.Models;
using TaskbarTactics.Core.Stats;

namespace TaskbarTactics.Core.Combat
{
    [Serializable]
    public sealed class CombatantState
    {
        public string Id = string.Empty;
        public CombatSide Side;
        public FormationPosition Position;
        public int MaxHealth;
        public int CurrentHealth;
        public int MaxMana;
        public int CurrentMana;
        public float AttackPower;
        public float SpellPower;
        public float Defense;
        public float MagicResistance;
        public int Range = 1;
        public float AttackSpeed = 1f;
        public float CastSpeed = 1f;
        public float CriticalChance = 10f;
        public float CriticalDamage = 2f;
        public float Accuracy = 100f;
        public float Evasion;
        public float HealthRegeneration;
        public float ManaRegeneration;
        public float CooldownReduction;
        public bool HasTaunt;
        public bool IsDualWielding;
        public string ActiveSkillId = string.Empty;
        public float ActiveSkillMagnitude;
        public List<string> UnlockedSkillIds = new List<string>();
        public AttackHand NextAttackHand = AttackHand.Main;
        public StatusEffectCollection StatusEffects = new StatusEffectCollection();

        internal int NextAttackMilliseconds;
        internal float HealthRegenerationCarry;
        internal float ManaRegenerationCarry;

        public bool IsAlive => CurrentHealth > 0;

        public HeroStats SnapshotStats()
        {
            return new HeroStats
            {
                MaxHealth = MaxHealth,
                MaxMana = MaxMana,
                AttackPower = AttackPower,
                SpellPower = SpellPower,
                Defense = Defense,
                MagicResistance = MagicResistance,
                AttackSpeed = AttackSpeed,
                CastSpeed = CastSpeed,
                AttackRange = Range,
                CriticalChance = CriticalChance,
                CriticalDamage = CriticalDamage,
                Accuracy = Accuracy,
                Evasion = Evasion,
                HealthRegeneration = HealthRegeneration,
                ManaRegeneration = ManaRegeneration,
                CooldownReduction = CooldownReduction
            };
        }

        public CombatantState Clone()
        {
            CombatantState clone = (CombatantState)MemberwiseClone();
            clone.StatusEffects = StatusEffects?.Clone() ?? new StatusEffectCollection();
            clone.UnlockedSkillIds = UnlockedSkillIds != null
                ? new List<string>(UnlockedSkillIds)
                : new List<string>();
            return clone;
        }
    }

    public sealed class CombatRequest
    {
        public int Seed;
        public int MaxDurationMilliseconds = 60000;
        public List<CombatantState> Heroes = new List<CombatantState>();
        public List<CombatantState> Enemies = new List<CombatantState>();
    }

    public sealed class CombatResult
    {
        public CombatOutcome Outcome;
        public int ElapsedMilliseconds;
        public List<CombatEvent> Events = new List<CombatEvent>();
        public List<string> DefeatedEnemyIds = new List<string>();
        public List<int> SurvivingHeroHealth = new List<int>();
        public List<int> SurvivingHeroMana = new List<int>();
        public List<CombatantResourceResult> HeroResources = new List<CombatantResourceResult>();
    }

    [Serializable]
    public sealed class CombatantResourceResult
    {
        public string Id = string.Empty;
        public int CurrentHealth;
        public int CurrentMana;
        public List<ActiveStatusEffectState> PersistentStatusEffects =
            new List<ActiveStatusEffectState>();
    }

    public enum CombatEventKind
    {
        Damage,
        Healing
    }

    public sealed class CombatEvent : IEquatable<CombatEvent>
    {
        public CombatEventKind Kind;
        public int TimeMilliseconds;
        public CombatSide ActorSide;
        public string ActorId = string.Empty;
        public string TargetId = string.Empty;
        public int Amount;
        public bool WasCritical;
        public bool WasMiss;
        public DamageType DamageType = DamageType.Physical;
        public AttackHand AttackHand = AttackHand.Main;

        public bool Equals(CombatEvent other)
        {
            return other != null &&
                   Kind == other.Kind &&
                   TimeMilliseconds == other.TimeMilliseconds &&
                   ActorSide == other.ActorSide &&
                   ActorId == other.ActorId &&
                   TargetId == other.TargetId &&
                   Amount == other.Amount &&
                   WasCritical == other.WasCritical &&
                   WasMiss == other.WasMiss &&
                   DamageType == other.DamageType &&
                   AttackHand == other.AttackHand;
        }

        public override bool Equals(object obj)
        {
            return Equals(obj as CombatEvent);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                int hashCode = (int)Kind;
                hashCode = (hashCode * 397) ^ TimeMilliseconds;
                hashCode = (hashCode * 397) ^ (int)ActorSide;
                hashCode = (hashCode * 397) ^ (ActorId != null ? ActorId.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ (TargetId != null ? TargetId.GetHashCode() : 0);
                return hashCode;
            }
        }

        public override string ToString()
        {
            return $"{TimeMilliseconds}ms:{Kind}:{ActorId}>{TargetId}:{Amount}:" +
                   $"{WasCritical}:{WasMiss}:{AttackHand}";
        }
    }
}
