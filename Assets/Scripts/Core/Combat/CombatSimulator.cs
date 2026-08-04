using System;
using System.Collections.Generic;
using System.Linq;
using TaskbarTactics.Core.Models;

namespace TaskbarTactics.Core.Combat
{
    public interface ICombatSimulator
    {
        CombatResult Simulate(CombatRequest request);
    }

    public sealed class CombatSimulator : ICombatSimulator
    {
        private const int AttackIntervalTicks = 10;

        public CombatResult Simulate(CombatRequest request)
        {
            if (request == null)
            {
                throw new ArgumentNullException(nameof(request));
            }

            List<CombatantState> heroes = request.Heroes.Select(item => item.Clone()).ToList();
            List<CombatantState> enemies = request.Enemies.Select(item => item.Clone()).ToList();
            Random random = new Random(request.Seed);
            CombatResult result = new CombatResult();

            for (int tick = 0; tick < request.MaxTicks; tick++)
            {
                if (!heroes.Any(hero => hero.IsAlive) || !enemies.Any(enemy => enemy.IsAlive))
                {
                    result.ElapsedTicks = tick;
                    break;
                }

                if (tick % AttackIntervalTicks != 0)
                {
                    continue;
                }

                List<CombatantState> turnOrder = heroes
                    .Concat(enemies)
                    .Where(item => item.IsAlive)
                    .OrderByDescending(item => item.Speed)
                    .ThenBy(item => item.Id)
                    .ToList();

                foreach (CombatantState actor in turnOrder)
                {
                    if (!actor.IsAlive)
                    {
                        continue;
                    }

                    List<CombatantState> opponents =
                        actor.Side == CombatSide.Hero ? enemies : heroes;
                    CombatantState target = TargetSelector.SelectTarget(actor, opponents);
                    if (target == null)
                    {
                        continue;
                    }

                    bool critical = random.Next(100) < 10;
                    int variance = random.Next(0, 3);
                    int damage = Math.Max(1, actor.Power + variance - target.Defense);
                    if (critical)
                    {
                        damage *= 2;
                    }

                    target.CurrentHealth = Math.Max(0, target.CurrentHealth - damage);
                    result.Events.Add(new CombatEvent
                    {
                        Tick = tick,
                        ActorSide = actor.Side,
                        ActorId = actor.Id,
                        TargetId = target.Id,
                        Amount = damage,
                        WasCritical = critical
                    });

                    if (!opponents.Any(item => item.IsAlive))
                    {
                        break;
                    }
                }

                result.ElapsedTicks = tick + 1;
            }

            bool heroesAlive = heroes.Any(hero => hero.IsAlive);
            bool enemiesAlive = enemies.Any(enemy => enemy.IsAlive);
            result.Outcome = !heroesAlive
                ? CombatOutcome.Defeat
                : !enemiesAlive
                    ? CombatOutcome.Victory
                    : CombatOutcome.Timeout;
            result.SurvivingHeroHealth = heroes
                .OrderBy(hero => hero.Id)
                .Select(hero => hero.CurrentHealth)
                .ToList();
            return result;
        }
    }
}
