using System.Collections.Generic;
using System.Linq;
using TaskbarTactics.Core.Combat;
using TaskbarTactics.Core.Stats;
using UnityEngine;

namespace TaskbarTactics.Content
{
    [CreateAssetMenu(menuName = "Taskbar Tactics/Status Effect")]
    public sealed class StatusEffectDefinition : StableDefinition
    {
        [SerializeField, Tooltip("Runtime-safe, data-driven buff or debuff definition.")]
        private StatusEffectDefinitionData data = new StatusEffectDefinitionData();

        public StatusEffectDefinitionData Data => data;

        public void Configure(StatusEffectBlueprint blueprint)
        {
            SetId(blueprint.Data.Id);
            data = Clone(blueprint.Data);
        }

        public StatusEffectDefinitionData CreateRuntimeData()
        {
            return Clone(data);
        }

        private static StatusEffectDefinitionData Clone(StatusEffectDefinitionData source)
        {
            return new StatusEffectDefinitionData
            {
                Id = source.Id,
                Kind = source.Kind,
                DurationMilliseconds = source.DurationMilliseconds,
                MaxStacks = source.MaxStacks,
                StackPolicy = source.StackPolicy,
                CanBeDispelled = source.CanBeDispelled,
                PersistsBetweenCombats = source.PersistsBetweenCombats,
                PreventsBasicAttacks = source.PreventsBasicAttacks,
                PreventsCasting = source.PreventsCasting,
                PeriodicEffect = source.PeriodicEffect,
                PeriodicAmount = source.PeriodicAmount,
                PeriodMilliseconds = source.PeriodMilliseconds,
                Tags = new List<string>(source.Tags),
                Modifiers = source.Modifiers.Select(item => new StatModifier(
                    item.Stat,
                    item.Operation,
                    item.Value,
                    item.SourceId)).ToList()
            };
        }
    }
}
