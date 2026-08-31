using System.Collections.Generic;
using System.Linq;
using TaskbarTactics.Content;
using TaskbarTactics.Core.Models;
using UnityEngine;
using UnityEngine.UI;

namespace TaskbarTactics.Presentation
{
    public sealed class InventorySlotGridView : MonoBehaviour
    {
        private readonly List<Image> slots = new List<Image>();
        private readonly List<Image> itemIcons = new List<Image>();

        public void Configure(IEnumerable<Image> slotImages)
        {
            slots.Clear();
            slots.AddRange(slotImages.Where(slot => slot != null));
            EnsureItemIcons();
        }

        public void Refresh(GameContentCatalog catalog, IReadOnlyList<InventoryItem> inventory)
        {
            if (catalog == null)
            {
                return;
            }

            if (slots.Count == 0)
            {
                DiscoverSlots();
            }

            EnsureItemIcons();
            List<Sprite> visibleItemIcons = (inventory ?? new List<InventoryItem>())
                .Select(item => catalog.FindItem(item.DefinitionId))
                .Where(definition => definition != null)
                .Select(ItemIcon)
                .Where(icon => icon != null)
                .ToList();

            for (int i = 0; i < itemIcons.Count; i++)
            {
                Image icon = itemIcons[i];
                icon.sprite = i < visibleItemIcons.Count ? visibleItemIcons[i] : null;
                icon.color = icon.sprite != null ? Color.white : Color.clear;
                icon.gameObject.SetActive(icon.sprite != null);
            }
        }

        private static Sprite ItemIcon(ItemDefinition definition)
        {
            if (definition.Icon != null)
            {
                return definition.Icon;
            }

            return Resources.Load<Sprite>($"Items/{definition.Id}");
        }

        private void DiscoverSlots()
        {
            slots.Clear();
            slots.AddRange(GetComponentsInChildren<Image>(true)
                .Where(image => image.name.StartsWith("Inventory Slot"))
                .OrderBy(image => image.name));
        }

        private void EnsureItemIcons()
        {
            while (itemIcons.Count < slots.Count)
            {
                Image slot = slots[itemIcons.Count];
                Transform existing = slot.transform.Find("Item Icon");
                Image icon = existing != null
                    ? existing.GetComponent<Image>() ?? CreateItemIcon(slot.transform)
                    : CreateItemIcon(slot.transform);

                itemIcons.Add(icon);
            }
        }

        private static Image CreateItemIcon(Transform parent)
        {
            GameObject iconObject = new GameObject(
                "Item Icon", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            iconObject.transform.SetParent(parent, false);
            Image icon = iconObject.GetComponent<Image>();
            icon.color = Color.clear;
            icon.preserveAspect = true;
            icon.raycastTarget = false;

            RectTransform rect = icon.rectTransform;
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = Vector2.zero;
            rect.sizeDelta = new Vector2(38f, 38f);
            return icon;
        }
    }
}
