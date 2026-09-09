using System;
using System.Collections.Generic;
using System.Linq;
using TaskbarTactics.Core.Models;
using TaskbarTactics.Core.Stats;

namespace TaskbarTactics.Core.Combat
{
    [Serializable]
    public sealed class StatusEffectDefinitionData
    {
        public string Id = string.Empty;
        public StatusEffectKind Kind;
        public int DurationMilliseconds = 1000;
        public int MaxStacks = 1;
        public StatusStackPolicy StackPolicy = StatusStackPolicy.RefreshDuration;
        public bool CanBeDispelled = true;
        public bool PersistsBetweenCombats;
        public bool PreventsBasicAttacks;
        public bool PreventsCasting;
        public StatusPeriodicEffect PeriodicEffect;
        public float PeriodicAmount;
        public int PeriodMilliseconds;
        public List<string> Tags = new List<string>();
        public List<StatModifier> Modifiers = new List<StatModifier>();
    }

    [Serializable]
    public sealed class ActiveStatusEffectState
    {
        public string DefinitionId = string.Empty;
        public string SourceId = string.Empty;
        public int Stacks = 1;
        public int RemainingMilliseconds;
        public int MillisecondsUntilNextPeriod;
    }

    public sealed class ActiveStatusEffect
    {
        public StatusEffectDefinitionData Definition;
        public string SourceId = string.Empty;
        public int Stacks = 1;
        public int RemainingMilliseconds;
        public int MillisecondsUntilNextPeriod;

        public ActiveStatusEffectState ToState()
        {
            return new ActiveStatusEffectState
            {
                DefinitionId = Definition?.Id ?? string.Empty,
                SourceId = SourceId,
                Stacks = Stacks,
                RemainingMilliseconds = RemainingMilliseconds,
                MillisecondsUntilNextPeriod = MillisecondsUntilNextPeriod
            };
        }
    }

    public sealed class StatusAdvanceResult
    {
        public float Damage;
        public float Healing;
        public float ManaGain;
        public float ManaLoss;
    }

    public sealed class StatusEffectCollection
    {
        private readonly List<ActiveStatusEffect> active = new List<ActiveStatusEffect>();

        public IReadOnlyList<ActiveStatusEffect> Active => active;
        public bool PreventsBasicAttacks => active.Any(item => item.Definition.PreventsBasicAttacks);
        public bool PreventsCasting => active.Any(item => item.Definition.PreventsCasting);

        public void Apply(StatusEffectDefinitionData definition, string sourceId)
        {
            if (definition == null || string.IsNullOrWhiteSpace(definition.Id))
            {
                throw new ArgumentException("A status effect definition with an id is required.",
                    nameof(definition));
            }

            int duration = Math.Max(0, definition.DurationMilliseconds);
            int period = Math.Max(0, definition.PeriodMilliseconds);
            int maxStacks = Math.Max(1, definition.MaxStacks);
            List<ActiveStatusEffect> matching = active
                .Where(item => item.Definition.Id == definition.Id)
                .ToList();

            if (definition.StackPolicy == StatusStackPolicy.IndependentInstances)
            {
                if (matching.Count >= maxStacks)
                {
                    ActiveStatusEffect oldest = matching
                        .OrderBy(item => item.RemainingMilliseconds)
                        .First();
                    ResetDuration(oldest, duration, period);
                    return;
                }

                active.Add(CreateActive(definition, sourceId, duration, period));
                return;
            }

            ActiveStatusEffect existing = matching.FirstOrDefault();
            if (existing == null)
            {
                active.Add(CreateActive(definition, sourceId, duration, period));
                return;
            }

            if (definition.StackPolicy == StatusStackPolicy.StackAndRefresh)
            {
                existing.Stacks = Math.Min(maxStacks, existing.Stacks + 1);
            }

            existing.SourceId = sourceId ?? string.Empty;
            ResetDuration(existing, duration, period);
        }

        public void Restore(
            ActiveStatusEffectState state,
            StatusEffectDefinitionData definition)
        {
            if (state == null || definition == null || state.DefinitionId != definition.Id)
            {
                return;
            }

            active.Add(new ActiveStatusEffect
            {
                Definition = definition,
                SourceId = state.SourceId ?? string.Empty,
                Stacks = Math.Max(1, Math.Min(Math.Max(1, definition.MaxStacks), state.Stacks)),
                RemainingMilliseconds = Math.Max(0, state.RemainingMilliseconds),
                MillisecondsUntilNextPeriod = Math.Max(0, state.MillisecondsUntilNextPeriod)
            });
        }

        public IReadOnlyList<StatModifier> CollectModifiers()
        {
            return active
                .SelectMany(effect => effect.Definition.Modifiers.Select(modifier =>
                    modifier.Scaled(effect.Stacks)))
                .ToList();
        }

        public IReadOnlyList<ActiveStatusEffectState> PersistentStates()
        {
            return active
                .Where(item => item.Definition.PersistsBetweenCombats)
                .Select(item => item.ToState())
                .ToList();
        }

        public StatusAdvanceResult Advance(int elapsedMilliseconds)
        {
            if (elapsedMilliseconds < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(elapsedMilliseconds));
            }

            StatusAdvanceResult result = new StatusAdvanceResult();
            foreach (ActiveStatusEffect effect in active.ToList())
            {
                int activeDuration = Math.Min(elapsedMilliseconds, effect.RemainingMilliseconds);
                ResolvePeriodic(effect, activeDuration, result);
                effect.RemainingMilliseconds -= activeDuration;
                if (effect.RemainingMilliseconds <= 0)
                {
                    active.Remove(effect);
                }
            }

            return result;
        }

        public int Dispel(StatusEffectKind kind)
        {
            return active.RemoveAll(item =>
                item.Definition.Kind == kind && item.Definition.CanBeDispelled);
        }

        public StatusEffectCollection Clone()
        {
            StatusEffectCollection clone = new StatusEffectCollection();
            foreach (ActiveStatusEffect effect in active)
            {
                clone.active.Add(new ActiveStatusEffect
                {
                    Definition = effect.Definition,
                    SourceId = effect.SourceId,
                    Stacks = effect.Stacks,
                    RemainingMilliseconds = effect.RemainingMilliseconds,
                    MillisecondsUntilNextPeriod = effect.MillisecondsUntilNextPeriod
                });
            }

            return clone;
        }

        private static ActiveStatusEffect CreateActive(
            StatusEffectDefinitionData definition,
            string sourceId,
            int duration,
            int period)
        {
            return new ActiveStatusEffect
            {
                Definition = definition,
                SourceId = sourceId ?? string.Empty,
                Stacks = 1,
                RemainingMilliseconds = duration,
                MillisecondsUntilNextPeriod = period
            };
        }

        private static void ResetDuration(
            ActiveStatusEffect effect,
            int duration,
            int period)
        {
            effect.RemainingMilliseconds = duration;
            effect.MillisecondsUntilNextPeriod = period;
        }

        private static void ResolvePeriodic(
            ActiveStatusEffect effect,
            int elapsedMilliseconds,
            StatusAdvanceResult result)
        {
            if (effect.Definition.PeriodicEffect == StatusPeriodicEffect.None ||
                effect.Definition.PeriodMilliseconds <= 0 ||
                elapsedMilliseconds <= 0)
            {
                return;
            }

            int remaining = elapsedMilliseconds;
            int untilTick = effect.MillisecondsUntilNextPeriod <= 0
                ? effect.Definition.PeriodMilliseconds
                : effect.MillisecondsUntilNextPeriod;
            while (remaining >= untilTick)
            {
                remaining -= untilTick;
                ApplyPeriodic(effect, result);
                untilTick = effect.Definition.PeriodMilliseconds;
            }

            effect.MillisecondsUntilNextPeriod = untilTick - remaining;
        }

        private static void ApplyPeriodic(
            ActiveStatusEffect effect,
            StatusAdvanceResult result)
        {
            float amount = Math.Max(0f, effect.Definition.PeriodicAmount) * effect.Stacks;
            switch (effect.Definition.PeriodicEffect)
            {
                case StatusPeriodicEffect.Damage:
                    result.Damage += amount;
                    break;
                case StatusPeriodicEffect.Healing:
                    result.Healing += amount;
                    break;
                case StatusPeriodicEffect.ManaGain:
                    result.ManaGain += amount;
                    break;
                case StatusPeriodicEffect.ManaLoss:
                    result.ManaLoss += amount;
                    break;
            }
        }
    }
}
