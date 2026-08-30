using System.Collections.Generic;
using System.Linq;
using TaskbarTactics.Core.Equipment;
using TaskbarTactics.Core.Models;
using TaskbarTactics.Core.Stats;
using UnityEngine;

namespace TaskbarTactics.Content
{
    [CreateAssetMenu(menuName = "Taskbar Tactics/Item")]
    public sealed class ItemDefinition : StableDefinition
    {
        [SerializeField, Tooltip("Slot, weapon/armor type and base stat contribution.")]
        private EquipmentDescriptor descriptor = new EquipmentDescriptor();
        [SerializeField] private string tagId;
        [SerializeField] private Sprite icon;

        public EquipmentSlot Slot => descriptor.PrimarySlot;
        public EquipmentDescriptor Descriptor => descriptor;
        public string TagId => tagId;
        public Sprite Icon => icon;

        public void Configure(ItemBlueprint data)
        {
            SetId(data.Id);
            descriptor = data.Descriptor;
            tagId = data.TagId;
        }

        public void SetIcon(Sprite value)
        {
            icon = value;
        }

        public EquipmentDescriptor CreateDescriptor(
            string instanceId,
            IEnumerable<ItemBonusDefinition> bonuses)
        {
            EquipmentDescriptor result = descriptor.CloneForInstance(instanceId);
            List<StatModifier> modifiers = result.Modifiers.ToList();
            if (bonuses != null)
            {
                modifiers.AddRange(bonuses
                    .Where(item => item != null)
                    .SelectMany(item => item.Modifiers)
                    .Select(item => new StatModifier(
                        item.Stat,
                        item.Operation,
                        item.Value,
                        item.SourceId)));
            }

            result.Modifiers = modifiers;
            return result;
        }
    }
}
