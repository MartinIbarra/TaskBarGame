using System.Collections.Generic;
using System.Linq;

namespace TaskbarTactics.Core.Combat
{
    public static class TargetSelector
    {
        public static CombatantState SelectTarget(
            CombatantState attacker,
            IReadOnlyList<CombatantState> candidates)
        {
            List<CombatantState> inRange = candidates
                .Where(candidate =>
                    candidate.IsAlive &&
                    attacker.Position.ManhattanDistance(candidate.Position) <= attacker.Range)
                .ToList();

            if (inRange.Count == 0)
            {
                return null;
            }

            List<CombatantState> taunting = inRange.Where(candidate => candidate.HasTaunt).ToList();
            IEnumerable<CombatantState> pool = taunting.Count > 0 ? taunting : inRange;

            return pool
                .OrderBy(candidate => candidate.Position.Column)
                .ThenBy(candidate => attacker.Position.ManhattanDistance(candidate.Position))
                .ThenBy(candidate => candidate.CurrentHealth)
                .ThenBy(candidate => candidate.Id)
                .First();
        }
    }
}
