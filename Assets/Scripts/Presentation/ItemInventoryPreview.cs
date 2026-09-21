using System.Linq;
using TaskbarTactics.Core.Models;
using UnityEngine;

namespace TaskbarTactics.Presentation
{
    // Authoring aid: never grants equipment automatically in a release player.
    public sealed class ItemInventoryPreview : MonoBehaviour
    {
        private const string PreviewPrefix = "item-preview-";
        [SerializeField] private GameAppController app;
        [SerializeField] private bool addMissingItemsOnStart = true;
        [SerializeField] private ItemRarity previewRarity = ItemRarity.Common;

        public void Configure(GameAppController controller) => app = controller;

        private void Start()
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            if (addMissingItemsOnStart) AddMissingItems();
#endif
        }

        [ContextMenu("Add Missing Icon Items (Play Mode)")]
        public void AddMissingItems()
        {
            if (!Application.isPlaying || app?.State == null) return;
            foreach (var definition in app.Catalog.Items)
            {
                if (definition.Icon == null || app.State.Inventory.Any(item => item.DefinitionId == definition.Id))
                    continue;
                app.State.Inventory.Add(new InventoryItem
                {
                    InstanceId = PreviewPrefix + definition.Id,
                    DefinitionId = definition.Id,
                    Slot = definition.Slot,
                    Rarity = previewRarity
                });
            }
            app.RefreshInventoryAfterAuthoring();
        }

        [ContextMenu("Apply Preview Rarity (Play Mode)")]
        public void ApplyPreviewRarity()
        {
            if (!Application.isPlaying || app?.State == null) return;
            foreach (InventoryItem item in app.State.Inventory.Where(item =>
                         item.InstanceId.StartsWith(PreviewPrefix, System.StringComparison.Ordinal)))
                item.Rarity = previewRarity;
            app.RefreshInventoryAfterAuthoring();
        }
    }
}
