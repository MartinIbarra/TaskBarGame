using System;
using System.Collections.Generic;
using System.Linq;
using TaskbarTactics.Core.Models;
using TaskbarTactics.Core.Stats;

namespace TaskbarTactics.Core.Combat
{
    public interface ICombatSimulator
    {
        CombatResult Simulate(CombatRequest request);
    }

    public sealed class CombatSimulator : ICombatSimulator
    {
        public CombatResult Simulate(CombatRequest request)
        {
            if (request == null)
            {
                throw new ArgumentNullException(nameof(request));
            }

            List<CombatantState> heroes = CloneAndInitialize(request.Heroes);
            List<CombatantState> enemies = CloneAndInitialize(request.Enemies);
            List<CombatantState> all = heroes.Concat(enemies).ToList();
            Random random = new Random(request.Seed);
            CombatResult result = new CombatResult();
            int maximumDuration = Math.Max(0, request.MaxDurationMilliseconds);
            int currentTime = 0;

            while (HasLiving(heroes) && HasLiving(enemies))
            {
                int nextActionTime = all
                    .Where(item => item.IsAlive)
                    .Min(item => item.NextAttackMilliseconds);
                if (nextActionTime > maximumDuration)
                {
                    currentTime = maximumDuration;
                    break;
                }

                int elapsed = Math.Max(0, nextActionTime - currentTime);
                AdvanceTime(all, elapsed);
                currentTime = nextActionTime;
                if (!HasLiving(heroes) || !HasLiving(enemies))
                {
                    break;
                }

                List<CombatantState> ready = all
                    .Where(item => item.IsAlive && item.NextAttackMilliseconds <= currentTime)
                    .OrderBy(item => item.Id, StringComparer.Ordinal)
                    .ToList();
                foreach (CombatantState actor in ready)
                {
                    if (!actor.IsAlive)
                    {
                        continue;
                    }

                    HeroStats actorStats = EffectiveStats(actor);
                    actor.NextAttackMilliseconds = SafeAdd(
                        currentTime,
                        AttackIntervalMilliseconds(actorStats.AttackSpeed));
                    if (actor.StatusEffects != null && actor.StatusEffects.PreventsBasicAttacks)
                    {
                        continue;
                    }

                    List<CombatantState> opponents =
                        actor.Side == CombatSide.Hero ? enemies : heroes;
                    int baseRange = actor.Range;
                    actor.Range = actorStats.AttackRange;
                    CombatantState target = TargetSelector.SelectTarget(actor, opponents);
                    actor.Range = baseRange;
                    if (target == null)
                    {
                        continue;
                    }

                    AttackHand hand = actor.NextAttackHand;
                    if (actor.IsDualWielding)
                    {
                        actor.NextAttackHand = hand == AttackHand.Main
                            ? AttackHand.Secondary
                            : AttackHand.Main;
                    }

                    HeroStats targetStats = EffectiveStats(target);
                    float hitChance = HeroStatsCalculator.CalculateHitChance(
                        actorStats.Accuracy,
                        targetStats.Evasion);
                    bool missed = random.NextDouble() * 100d >= hitChance;
                    bool critical = !missed &&
                                    random.NextDouble() * 100d < actorStats.CriticalChance;
                    int damage = 0;
                    if (!missed)
                    {
                        float variance = random.Next(0, 3);
                        float rawDamage = Math.Max(1f, actorStats.AttackPower + variance);
                        if (critical)
                        {
                            rawDamage *= Math.Max(1f, actorStats.CriticalDamage);
                        }

                        float mitigated = HeroStatsCalculator.MitigateDamage(
                            rawDamage,
                            targetStats.Defense);
                        damage = Math.Max(1, Round(mitigated));
                        target.CurrentHealth = Math.Max(0, target.CurrentHealth - damage);
                    }

                    result.Events.Add(new CombatEvent
                    {
                        TimeMilliseconds = currentTime,
                        ActorSide = actor.Side,
                        ActorId = actor.Id,
                        TargetId = target.Id,
                        Amount = damage,
                        WasCritical = critical,
                        WasMiss = missed,
                        DamageType = DamageType.Physical,
                        AttackHand = hand
                    });

                    if (!opponents.Any(item => item.IsAlive))
                    {
                        break;
                    }
                }

                if (currentTime == maximumDuration)
                {
                    break;
                }
            }

            result.ElapsedMilliseconds = currentTime;
            bool heroesAlive = HasLiving(heroes);
            bool enemiesAlive = HasLiving(enemies);
            result.Outcome = !heroesAlive
                ? CombatOutcome.Defeat
                : !enemiesAlive
                    ? CombatOutcome.Victory
                    : CombatOutcome.Timeout;
            result.DefeatedEnemyIds = enemies
                .Where(enemy => !enemy.IsAlive)
                .Select(enemy => enemy.Id)
                .ToList();
            List<CombatantState> orderedHeroes = heroes
                .OrderBy(hero => hero.Id, StringComparer.Ordinal)
                .ToList();
            result.SurvivingHeroHealth = orderedHeroes
                .Select(hero => hero.CurrentHealth)
                .ToList();
            result.SurvivingHeroMana = orderedHeroes
                .Select(hero => hero.CurrentMana)
                .ToList();
            result.HeroResources = orderedHeroes
                .Select(hero => new CombatantResourceResult
                {
                    Id = hero.Id,
                    CurrentHealth = hero.CurrentHealth,
                    CurrentMana = hero.CurrentMana,
                    PersistentStatusEffects = hero.StatusEffects?
                        .PersistentStates()
                        .ToList() ?? new List<ActiveStatusEffectState>()
                })
                .ToList();
            return result;
        }

        private static List<CombatantState> CloneAndInitialize(
            IEnumerable<CombatantState> source)
        {
            return (source ?? Array.Empty<CombatantState>())
                .Where(item => item != null)
                .Select(item =>
                {
                    CombatantState clone = item.Clone();
                    clone.MaxHealth = Math.Max(1, clone.MaxHealth);
                    clone.CurrentHealth = Math.Max(0,
                        Math.Min(clone.MaxHealth, clone.CurrentHealth));
                    clone.MaxMana = Math.Max(0, clone.MaxMana);
                    clone.CurrentMana = Math.Max(0,
                        Math.Min(clone.MaxMana, clone.CurrentMana));
                    clone.AttackSpeed = Math.Max(
                        HeroStatLimits.MinAttackSpeed,
                        Math.Min(HeroStatLimits.MaxAttackSpeed, clone.AttackSpeed));
                    clone.NextAttackMilliseconds = AttackIntervalMilliseconds(
                        EffectiveStats(clone).AttackSpeed);
                    clone.NextAttackHand = AttackHand.Main;
                    clone.HealthRegenerationCarry = 0f;
                    clone.ManaRegenerationCarry = 0f;
                    return clone;
                })
                .ToList();
        }

        private static bool HasLiving(IEnumerable<CombatantState> combatants)
        {
            return combatants.Any(item => item.IsAlive);
        }

        private static int AttackIntervalMilliseconds(float attacksPerSecond)
        {
            float safeSpeed = Math.Max(
                HeroStatLimits.MinAttackSpeed,
                Math.Min(HeroStatLimits.MaxAttackSpeed, attacksPerSecond));
            return Math.Max(1, Round(1000f / safeSpeed));
        }

        private static void AdvanceTime(
            IEnumerable<CombatantState> combatants,
            int elapsedMilliseconds)
        {
            if (elapsedMilliseconds <= 0)
            {
                return;
            }

            float seconds = elapsedMilliseconds / 1000f;
            foreach (CombatantState combatant in combatants.Where(item => item.IsAlive))
            {
                HeroStats effective = EffectiveStats(combatant);
                combatant.HealthRegenerationCarry +=
                    Math.Max(0f, effective.HealthRegeneration) * seconds;
                int healthFromRegeneration = (int)combatant.HealthRegenerationCarry;
                combatant.HealthRegenerationCarry -= healthFromRegeneration;
                combatant.CurrentHealth = Math.Min(
                    effective.MaxHealth,
                    combatant.CurrentHealth + healthFromRegeneration);

                combatant.ManaRegenerationCarry +=
                    Math.Max(0f, effective.ManaRegeneration) * seconds;
                int manaFromRegeneration = (int)combatant.ManaRegenerationCarry;
                combatant.ManaRegenerationCarry -= manaFromRegeneration;
                combatant.CurrentMana = Math.Min(
                    effective.MaxMana,
                    combatant.CurrentMana + manaFromRegeneration);

                StatusAdvanceResult status = combatant.StatusEffects?
                    .Advance(elapsedMilliseconds) ?? new StatusAdvanceResult();
                combatant.CurrentHealth = Math.Max(0, Math.Min(
                    effective.MaxHealth,
                    combatant.CurrentHealth + Round(status.Healing) - Round(status.Damage)));
                combatant.CurrentMana = Math.Max(0, Math.Min(
                    effective.MaxMana,
                    combatant.CurrentMana + Round(status.ManaGain) - Round(status.ManaLoss)));
            }
        }

        private static int SafeAdd(int left, int right)
        {
            return right > int.MaxValue - left ? int.MaxValue : left + right;
        }

        private static HeroStats EffectiveStats(CombatantState combatant)
        {
            return HeroStatsCalculator.Calculate(
                combatant.SnapshotStats(),
                null,
                1,
                combatant.StatusEffects?.CollectModifiers());
        }

        private static int Round(float value)
        {
            return (int)Math.Round(value, MidpointRounding.AwayFromZero);
        }
    }
}
