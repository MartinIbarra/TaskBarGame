using System.Collections.Generic;
using System.Linq;
using TaskbarTactics.Core.Stats;
using UnityEngine;

namespace TaskbarTactics.Content
{
    [CreateAssetMenu(menuName = "Taskbar Tactics/Item Bonus")]
    public sealed class ItemBonusDefinition : StableDefinition
    {
        [SerializeField, Tooltip("Stat changes applied while the owning item is equipped.")]
        private List<StatModifier> modifiers = new List<StatModifier>();

        public IReadOnlyList<StatModifier> Modifiers => modifiers;

        public void Configure(ItemBonusBlueprint data)
        {
            SetId(data.Id);
            modifiers = data.Modifiers
                .Select(item => new StatModifier(
                    item.Stat,
                    item.Operation,
                    item.Value,
                    data.Id))
                .ToList();
        }
    }
}
