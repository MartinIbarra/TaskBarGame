using System;
using System.Collections.Generic;
using TaskbarTactics.Core.Models;

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
        public int Power;
        public int Defense;
        public int Range = 1;
        public int Speed = 10;
        public bool HasTaunt;

        public bool IsAlive => CurrentHealth > 0;

        public CombatantState Clone()
        {
            return (CombatantState)MemberwiseClone();
        }
    }

    public sealed class CombatRequest
    {
        public int Seed;
        public int MaxTicks = 600;
        public List<CombatantState> Heroes = new List<CombatantState>();
        public List<CombatantState> Enemies = new List<CombatantState>();
    }

    public sealed class CombatResult
    {
        public CombatOutcome Outcome;
        public int ElapsedTicks;
        public List<CombatEvent> Events = new List<CombatEvent>();
        public List<int> SurvivingHeroHealth = new List<int>();
    }

    public sealed class CombatEvent : IEquatable<CombatEvent>
    {
        public int Tick;
        public CombatSide ActorSide;
        public string ActorId = string.Empty;
        public string TargetId = string.Empty;
        public int Amount;
        public bool WasCritical;

        public bool Equals(CombatEvent other)
        {
            return other != null &&
                   Tick == other.Tick &&
                   ActorSide == other.ActorSide &&
                   ActorId == other.ActorId &&
                   TargetId == other.TargetId &&
                   Amount == other.Amount &&
                   WasCritical == other.WasCritical;
        }

        public override bool Equals(object obj)
        {
            return Equals(obj as CombatEvent);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                int hashCode = Tick;
                hashCode = (hashCode * 397) ^ (int)ActorSide;
                hashCode = (hashCode * 397) ^ (ActorId != null ? ActorId.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ (TargetId != null ? TargetId.GetHashCode() : 0);
                return hashCode;
            }
        }

        public override string ToString()
        {
            return $"{Tick}:{ActorId}>{TargetId}:{Amount}:{WasCritical}";
        }
    }
}
