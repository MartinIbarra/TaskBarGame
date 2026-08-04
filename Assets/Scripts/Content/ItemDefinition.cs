using TaskbarTactics.Core.Models;
using UnityEngine;

namespace TaskbarTactics.Content
{
    [CreateAssetMenu(menuName = "Taskbar Tactics/Item")]
    public sealed class ItemDefinition : StableDefinition
    {
        [SerializeField] private EquipmentSlot slot;
        [SerializeField] private string tagId;
        [SerializeField] private int basePower = 2;

        public EquipmentSlot Slot => slot;
        public string TagId => tagId;
        public int BasePower => basePower;

        public void Configure(ItemBlueprint data)
        {
            SetId(data.Id);
            slot = data.Slot;
            tagId = data.TagId;
        }
    }
}
