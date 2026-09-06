using System.Collections.Generic;
using System.Linq;
using TaskbarTactics.Content;
using TaskbarTactics.Core.Models;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace TaskbarTactics.Presentation
{
    public sealed class InventorySlotGridView : MonoBehaviour
    {
        private const int Columns = 4;
        private const float Gap = 66f;
        private const float SlotSize = 58f;
        private static readonly Vector2 SlotStart = new Vector2(90f, -90f);
        private static readonly Vector2 ItemIconOffset = new Vector2(2f, -2f);

        private readonly List<Image> slots = new List<Image>();
        private readonly List<Image> itemIcons = new List<Image>();
        private ManagementUiController owner;

        public static void ClearActiveDragVisuals()
        {
            InventoryItemDragSource.ClearActiveDragState();
        }

        public void SetOwner(ManagementUiController targetOwner)
        {
            owner = targetOwner;
            ConfigureDropTargets();
        }

        public void Configure(IEnumerable<Image> slotImages)
        {
            slots.Clear();
            slots.AddRange(slotImages.Where(slot => slot != null));
            EnsureItemIcons();
            ArrangeSlots();
            ConfigureDropTargets();
        }

        public void Refresh(
            GameContentCatalog catalog,
            IReadOnlyList<InventoryItem> inventory,
            IReadOnlyList<HeroState> heroes)
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
            ArrangeSlots();
            ConfigureDropTargets();
            HashSet<string> equippedItemIds = EquippedItemIds(heroes);
            List<InventoryItem> visibleItems = (inventory ?? new List<InventoryItem>())
                .Where(item => !equippedItemIds.Contains(item.InstanceId))
                .Where(item => ItemIcon(catalog.FindItem(item.DefinitionId)) != null)
                .ToList();

            for (int i = 0; i < itemIcons.Count; i++)
            {
                Image icon = itemIcons[i];
                icon.sprite = i < visibleItems.Count
                    ? ItemIcon(catalog.FindItem(visibleItems[i].DefinitionId))
                    : null;
                icon.color = icon.sprite != null ? Color.white : Color.clear;
                icon.gameObject.SetActive(icon.sprite != null);
                icon.raycastTarget = icon.sprite != null;

                InventoryItemDragSource dragSource = icon.GetComponent<InventoryItemDragSource>() ??
                    icon.gameObject.AddComponent<InventoryItemDragSource>();
                dragSource.Configure(
                    i < visibleItems.Count ? visibleItems[i].InstanceId : string.Empty,
                    icon.sprite);
            }
        }

        public bool HasVisibleSpace(
            GameContentCatalog catalog,
            IReadOnlyList<InventoryItem> inventory,
            IReadOnlyList<HeroState> heroes)
        {
            if (catalog == null)
            {
                return false;
            }

            if (slots.Count == 0)
            {
                DiscoverSlots();
            }

            HashSet<string> equippedItemIds = EquippedItemIds(heroes);
            int visibleCount = (inventory ?? new List<InventoryItem>())
                .Count(item => !equippedItemIds.Contains(item.InstanceId) &&
                               ItemIcon(catalog.FindItem(item.DefinitionId)) != null);
            return visibleCount < slots.Count;
        }

        private static HashSet<string> EquippedItemIds(IReadOnlyList<HeroState> heroes)
        {
            HashSet<string> equippedIds = new HashSet<string>();
            if (heroes == null)
            {
                return equippedIds;
            }

            foreach (HeroState hero in heroes)
            {
                if (hero?.EquippedItems == null)
                {
                    continue;
                }

                foreach (EquippedItemState item in hero.EquippedItems)
                {
                    if (!string.IsNullOrWhiteSpace(item.ItemInstanceId))
                    {
                        equippedIds.Add(item.ItemInstanceId);
                    }
                }
            }

            return equippedIds;
        }

        private static Sprite ItemIcon(ItemDefinition definition)
        {
            if (definition == null)
            {
                return null;
            }

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

        private void ArrangeSlots()
        {
            for (int i = 0; i < slots.Count; i++)
            {
                Image slot = slots[i];
                if (slot == null)
                {
                    continue;
                }

                RectTransform slotRect = slot.rectTransform;
                slotRect.anchorMin = slotRect.anchorMax = new Vector2(0f, 1f);
                slotRect.pivot = new Vector2(0.5f, 0.5f);
                slotRect.anchoredPosition = SlotStart + new Vector2((i % Columns) * Gap, -(i / Columns) * Gap);
                slotRect.sizeDelta = new Vector2(SlotSize, SlotSize);

                if (i < itemIcons.Count && itemIcons[i] != null)
                {
                    itemIcons[i].rectTransform.anchoredPosition = ItemIconOffset;
                }
            }
        }

        private void ConfigureDropTargets()
        {
            InventoryDropTarget rootDropTarget = GetComponent<InventoryDropTarget>() ??
                gameObject.AddComponent<InventoryDropTarget>();
            rootDropTarget.Configure(owner);

            foreach (Image slot in slots)
            {
                if (slot == null)
                {
                    continue;
                }

                slot.raycastTarget = true;
                InventoryDropTarget dropTarget = slot.GetComponent<InventoryDropTarget>() ??
                    slot.gameObject.AddComponent<InventoryDropTarget>();
                dropTarget.Configure(owner);
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
            rect.anchoredPosition = ItemIconOffset;
            rect.sizeDelta = new Vector2(38f, 38f);
            return icon;
        }
    }

    public sealed class InventoryItemDragSource : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        private static readonly List<InventoryItemDragSource> ActiveSources =
            new List<InventoryItemDragSource>();

        private Canvas rootCanvas;
        private Image sourceImage;
        private Image dragImage;
        private string itemInstanceId;

        public static string ActiveItemInstanceId { get; private set; }

        public static void ClearActiveDragState()
        {
            ActiveItemInstanceId = string.Empty;
            for (int i = ActiveSources.Count - 1; i >= 0; i--)
            {
                InventoryItemDragSource source = ActiveSources[i];
                if (source == null)
                {
                    ActiveSources.RemoveAt(i);
                    continue;
                }

                source.ClearDragState();
            }
        }

        public void Configure(string instanceId, Sprite icon)
        {
            itemInstanceId = instanceId ?? string.Empty;
            sourceImage = GetComponent<Image>();
            if (sourceImage != null)
            {
                sourceImage.sprite = icon;
                sourceImage.raycastTarget = !string.IsNullOrWhiteSpace(itemInstanceId);
            }

            rootCanvas = GetComponentInParent<Canvas>();
        }

        private void OnEnable()
        {
            if (!ActiveSources.Contains(this))
            {
                ActiveSources.Add(this);
            }
        }

        public void OnBeginDrag(PointerEventData eventData)
        {
            if (string.IsNullOrWhiteSpace(itemInstanceId) || sourceImage == null || sourceImage.sprite == null)
            {
                return;
            }

            ActiveItemInstanceId = itemInstanceId;
            sourceImage.raycastTarget = false;
            CreateDragImage(eventData);
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (dragImage != null)
            {
                dragImage.rectTransform.position = eventData.position;
            }
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            ClearDragState();
        }

        private void OnDisable()
        {
            ClearDragState();
            ActiveSources.Remove(this);
        }

        private void ClearDragState()
        {
            ActiveItemInstanceId = string.Empty;
            if (sourceImage != null)
            {
                sourceImage.raycastTarget = !string.IsNullOrWhiteSpace(itemInstanceId);
            }

            if (dragImage != null)
            {
                Destroy(dragImage.gameObject);
                dragImage = null;
            }
        }

        private void CreateDragImage(PointerEventData eventData)
        {
            Transform parent = rootCanvas != null ? rootCanvas.transform : transform.root;
            GameObject dragObject = new GameObject(
                "Dragged Inventory Item", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            dragObject.transform.SetParent(parent, false);
            dragObject.transform.SetAsLastSibling();

            dragImage = dragObject.GetComponent<Image>();
            dragImage.sprite = sourceImage.sprite;
            dragImage.color = new Color(1f, 1f, 1f, 0.88f);
            dragImage.preserveAspect = true;
            dragImage.raycastTarget = false;

            RectTransform rect = dragImage.rectTransform;
            rect.sizeDelta = sourceImage.rectTransform.sizeDelta;
            rect.position = eventData.position;
        }
    }

    public sealed class InventoryDropTarget : MonoBehaviour, IDropHandler, IPointerEnterHandler, IPointerExitHandler
    {
        private ManagementUiController owner;
        private Image highlightTarget;
        private Color normalColor = Color.white;

        public void Configure(ManagementUiController targetOwner)
        {
            owner = targetOwner;
            highlightTarget = GetComponent<Image>();
            if (highlightTarget != null)
            {
                normalColor = highlightTarget.color;
                highlightTarget.raycastTarget = true;
            }
        }

        public void OnDrop(PointerEventData eventData)
        {
            if (!EquipmentItemDragSource.HasActiveItem || owner == null)
            {
                RestoreHighlight();
                return;
            }

            owner.TryUnequipItemToInventory(
                EquipmentItemDragSource.ActiveHeroId,
                EquipmentItemDragSource.ActiveSlot);
            RestoreHighlight();
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            if (EquipmentItemDragSource.HasActiveItem && highlightTarget != null)
            {
                highlightTarget.color = new Color(0.7f, 1f, 0.62f, normalColor.a);
            }
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            RestoreHighlight();
        }

        private void RestoreHighlight()
        {
            if (highlightTarget != null)
            {
                highlightTarget.color = normalColor;
            }
        }
    }
}
