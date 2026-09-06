using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TaskbarTactics.Content;
using TaskbarTactics.Core.Models;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace TaskbarTactics.Presentation
{
    public sealed class EquipmentPreviewLayoutView : MonoBehaviour
    {
        private const string SlotRoot = "UI/EquipmentSlots/";
        private const float SlotSize = 41.8f;
        private const float SlotPositionScale = 1.1f;
        private static readonly EquipmentSlot[] DisplaySlots =
        {
            EquipmentSlot.Head,
            EquipmentSlot.Shoulders,
            EquipmentSlot.Chest,
            EquipmentSlot.Bracers,
            EquipmentSlot.Hands,
            EquipmentSlot.Neck,
            EquipmentSlot.Cloak,
            EquipmentSlot.Belt,
            EquipmentSlot.Legs,
            EquipmentSlot.Boots,
            EquipmentSlot.Earring1,
            EquipmentSlot.Earring2,
            EquipmentSlot.MainWeapon,
            EquipmentSlot.Ring1,
            EquipmentSlot.Ring2,
            EquipmentSlot.SecondaryWeapon
        };

        private static readonly Dictionary<string, Sprite> SlotSpriteCache = new Dictionary<string, Sprite>();
        private Image heroPreview;
        private readonly List<Sprite> generatedHeroSprites = new List<Sprite>();
        private readonly Dictionary<EquipmentSlot, Image> equippedSlotIcons =
            new Dictionary<EquipmentSlot, Image>();
        private readonly Dictionary<EquipmentSlot, Image> emptySlotIcons =
            new Dictionary<EquipmentSlot, Image>();
        private Sprite[] heroIdleSprites;
        private Coroutine idleRoutine;
        private string activeHeroId;
        private string activeEquipmentHeroId;
        private ManagementUiController owner;

        public static void ClearActiveDragVisuals()
        {
            EquipmentItemDragSource.ClearActiveDragState();
        }

        private void Awake()
        {
            CreatePlaceholders();
        }

        public void SetOwner(ManagementUiController targetOwner)
        {
            owner = targetOwner;
            ConfigureLayoutDropTarget();
            ConfigureExistingSlotDropTargets();
        }

        private void OnEnable()
        {
            if (heroPreview != null && heroIdleSprites != null && heroIdleSprites.Length > 0)
            {
                heroPreview.sprite = heroIdleSprites[0];
                heroPreview.gameObject.SetActive(true);
                RestartIdleRoutine();
            }
        }

        public void RefreshHero(string heroId)
        {
            CreatePlaceholders();
            string animationId = LegacyHeroAnimationId(heroId);
            if (activeHeroId == animationId && heroPreview != null)
            {
                if (heroIdleSprites == null || heroIdleSprites.Length == 0)
                {
                    heroIdleSprites = LoadHeroIdleSprites(animationId);
                }

                if (heroIdleSprites.Length > 0)
                {
                    heroPreview.sprite = heroIdleSprites[0];
                    ApplyHeroPreviewProfile(animationId);
                    heroPreview.gameObject.SetActive(true);
                    RestartIdleRoutine();
                }

                return;
            }

            activeHeroId = animationId;
            StopIdleRoutine();
            ClearGeneratedSprites();
            heroIdleSprites = LoadHeroIdleSprites(animationId);
            if (heroPreview == null || heroIdleSprites.Length == 0)
            {
                return;
            }

            heroPreview.sprite = heroIdleSprites[0];
            ApplyHeroPreviewProfile(animationId);
            heroPreview.gameObject.SetActive(true);
            RestartIdleRoutine();
        }

        public void RefreshEquippedItems(
            GameContentCatalog catalog,
            IReadOnlyList<InventoryItem> inventory,
            HeroState hero)
        {
            CreatePlaceholders();
            ConfigureExistingSlotDropTargets();
            activeEquipmentHeroId = hero?.DefinitionId ?? string.Empty;

            foreach (KeyValuePair<EquipmentSlot, Image> pair in equippedSlotIcons)
            {
                string instanceId = hero?.GetEquippedItemId(pair.Key);
                InventoryItem item = inventory?.FirstOrDefault(candidate => candidate.InstanceId == instanceId);
                Sprite icon = ItemIcon(catalog, item);
                pair.Value.sprite = icon;
                pair.Value.color = icon != null ? Color.white : Color.clear;
                pair.Value.gameObject.SetActive(icon != null);
                pair.Value.raycastTarget = icon != null;

                EquipmentItemDragSource dragSource = pair.Value.GetComponent<EquipmentItemDragSource>() ??
                    pair.Value.gameObject.AddComponent<EquipmentItemDragSource>();
                dragSource.Configure(
                    activeEquipmentHeroId,
                    pair.Key,
                    icon != null ? instanceId : string.Empty,
                    icon);

                if (emptySlotIcons.TryGetValue(pair.Key, out Image emptyIcon) && emptyIcon != null)
                {
                    emptyIcon.gameObject.SetActive(icon == null);
                }
            }
        }

        public void CreatePlaceholders()
        {
            EnsureHeroPreview();
            foreach (EquipmentSlot slot in DisplaySlots)
            {
                if (transform.Find($"Equipment Slot {slot}") == null)
                {
                    CreateSlot(slot.ToString(), SlotIconName(slot), SlotPosition(slot));
                }
            }

            ArrangeEquipmentSlots();
            ConfigureExistingSlotDropTargets();
        }

        private void EnsureHeroPreview()
        {
            if (heroPreview != null)
            {
                return;
            }

            Transform existing = transform.Find("Hero Preview");
            if (existing != null)
            {
                heroPreview = existing.GetComponent<Image>();
            }

            if (heroPreview == null)
            {
                GameObject heroObject = new GameObject(
                    "Hero Preview",
                    typeof(RectTransform),
                    typeof(CanvasRenderer),
                    typeof(Image));
                heroObject.transform.SetParent(transform, false);
                heroPreview = heroObject.GetComponent<Image>();
            }

            heroPreview.preserveAspect = true;
            heroPreview.raycastTarget = false;
            heroPreview.color = new Color(1f, 1f, 1f, 0.82f);
            RectTransform heroRect = heroPreview.rectTransform;
            heroRect.anchorMin = heroRect.anchorMax = new Vector2(0.5f, 0.5f);
            heroRect.pivot = new Vector2(0.5f, 0.5f);
            heroRect.anchoredPosition = new Vector2(0f, -18f);
            heroRect.sizeDelta = new Vector2(118f, 196f);
            heroPreview.transform.SetSiblingIndex(0);
            ConfigureLayoutDropTarget();
        }

        private void ApplyHeroPreviewProfile(string heroId)
        {
            if (heroPreview == null)
            {
                return;
            }

            RectTransform heroRect = heroPreview.rectTransform;
            heroPreview.color = new Color(1f, 1f, 1f, 0.82f);
            heroPreview.preserveAspect = true;
            heroRect.anchoredPosition = new Vector2(0f, -18f);
            heroRect.sizeDelta = new Vector2(118f, 196f);

            switch (heroId)
            {
                case "rogue":
                    heroPreview.preserveAspect = false;
                    heroRect.sizeDelta = new Vector2(130.2f, 196f);
                    break;
                case "spellblade":
                    heroPreview.preserveAspect = false;
                    heroRect.sizeDelta = new Vector2(116.6f, 193.6f);
                    heroRect.anchoredPosition = new Vector2(0f, -18f);
                    break;
                case "pyromancer":
                    heroPreview.preserveAspect = false;
                    heroRect.sizeDelta = new Vector2(123.9f, 205.8f);
                    break;
                case "cleric":
                    heroPreview.preserveAspect = false;
                    heroRect.sizeDelta = new Vector2(118f, 194.25f);
                    break;
                case "guardian":
                    heroPreview.preserveAspect = false;
                    heroRect.sizeDelta = new Vector2(118.8f, 191.4f);
                    heroRect.anchoredPosition = new Vector2(0f, -18f);
                    break;
            }
        }

        private void CreateSlot(string slotName, string iconName, Vector2 position)
        {
            GameObject slot = new GameObject(
                $"Equipment Slot {slotName}",
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(Image));
            slot.transform.SetParent(transform, false);

            RectTransform slotRect = slot.GetComponent<RectTransform>();
            slotRect.anchorMin = slotRect.anchorMax = new Vector2(0.5f, 0.5f);
            slotRect.pivot = new Vector2(0.5f, 0.5f);
            slotRect.anchoredPosition = position;
            slotRect.sizeDelta = new Vector2(SlotSize, SlotSize);

            Image frame = slot.GetComponent<Image>();
            frame.sprite = LoadSlotSprite("frame64");
            frame.preserveAspect = true;
            frame.raycastTarget = true;

            GameObject icon = new GameObject(
                "Empty Icon",
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(Image));
            icon.transform.SetParent(slot.transform, false);

            RectTransform iconRect = icon.GetComponent<RectTransform>();
            iconRect.anchorMin = iconRect.anchorMax = new Vector2(0.5f, 0.5f);
            iconRect.pivot = new Vector2(0.5f, 0.5f);
            iconRect.anchoredPosition = Vector2.zero;
            iconRect.sizeDelta = new Vector2(SlotSize * 0.72f, SlotSize * 0.72f);

            Image iconImage = icon.GetComponent<Image>();
            iconImage.sprite = LoadSlotSprite(iconName);
            iconImage.color = iconImage.sprite != null ? Color.white : Color.clear;
            iconImage.preserveAspect = true;
            iconImage.raycastTarget = false;
            if (System.Enum.TryParse(slotName, out EquipmentSlot createdSlot))
            {
                ApplyEmptySlotIconTransform(iconImage, createdSlot);
            }

            GameObject equippedIcon = new GameObject(
                "Equipped Icon",
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(Image));
            equippedIcon.transform.SetParent(slot.transform, false);

            RectTransform equippedRect = equippedIcon.GetComponent<RectTransform>();
            equippedRect.anchorMin = equippedRect.anchorMax = new Vector2(0.5f, 0.5f);
            equippedRect.pivot = new Vector2(0.5f, 0.5f);
            equippedRect.anchoredPosition = Vector2.zero;
            equippedRect.sizeDelta = new Vector2(SlotSize * 0.78f, SlotSize * 0.78f);

            Image equippedImage = equippedIcon.GetComponent<Image>();
            equippedImage.color = Color.clear;
            equippedImage.preserveAspect = true;
            equippedImage.raycastTarget = false;
            equippedImage.gameObject.SetActive(false);

            if (System.Enum.TryParse(slotName, out EquipmentSlot equipmentSlot))
            {
                emptySlotIcons[equipmentSlot] = iconImage;
                equippedSlotIcons[equipmentSlot] = equippedImage;
                EquipmentDropTarget dropTarget = slot.GetComponent<EquipmentDropTarget>() ??
                    slot.AddComponent<EquipmentDropTarget>();
                dropTarget.Configure(owner, frame, equipmentSlot);
            }
        }

        private void ArrangeEquipmentSlots()
        {
            foreach (EquipmentSlot slot in DisplaySlots)
            {
                Transform slotTransform = transform.Find($"Equipment Slot {slot}");
                RectTransform rect = slotTransform as RectTransform;
                if (rect == null)
                {
                    continue;
                }

                rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
                rect.pivot = new Vector2(0.5f, 0.5f);
                rect.anchoredPosition = SlotPosition(slot);
                rect.sizeDelta = new Vector2(SlotSize, SlotSize);
                rect.SetAsLastSibling();
            }

            if (heroPreview != null)
            {
                heroPreview.transform.SetSiblingIndex(0);
            }
        }

        private static string SlotIconName(EquipmentSlot slot)
        {
            switch (slot)
            {
                case EquipmentSlot.Head:
                    return "head";
                case EquipmentSlot.Shoulders:
                    return "shoulders";
                case EquipmentSlot.Chest:
                    return "chest";
                case EquipmentSlot.Bracers:
                    return "braz";
                case EquipmentSlot.Hands:
                    return "hands_right";
                case EquipmentSlot.Legs:
                    return "trousers";
                case EquipmentSlot.Boots:
                    return "feet";
                case EquipmentSlot.Neck:
                    return "neck";
                case EquipmentSlot.Belt:
                    return "belt";
                case EquipmentSlot.Cloak:
                    return "cloak";
                case EquipmentSlot.Ring1:
                case EquipmentSlot.Ring2:
                    return "ring";
                case EquipmentSlot.Earring1:
                case EquipmentSlot.Earring2:
                    return "ear";
                case EquipmentSlot.MainWeapon:
                case EquipmentSlot.SecondaryWeapon:
                    return "mainhand";
                default:
                    return "frame64";
            }
        }

        private static Vector2 SlotPosition(EquipmentSlot slot)
        {
            Vector2 position;
            switch (slot)
            {
                case EquipmentSlot.Head:
                    position = new Vector2(-106f, 104f);
                    break;
                case EquipmentSlot.Shoulders:
                    position = new Vector2(-106f, 58f);
                    break;
                case EquipmentSlot.Chest:
                    position = new Vector2(-106f, 12f);
                    break;
                case EquipmentSlot.Bracers:
                    position = new Vector2(-106f, -34f);
                    break;
                case EquipmentSlot.Hands:
                    position = new Vector2(-106f, -80f);
                    break;
                case EquipmentSlot.Neck:
                    position = new Vector2(106f, 104f);
                    break;
                case EquipmentSlot.Cloak:
                    position = new Vector2(106f, 58f);
                    break;
                case EquipmentSlot.Belt:
                    position = new Vector2(106f, 12f);
                    break;
                case EquipmentSlot.Legs:
                    position = new Vector2(106f, -34f);
                    break;
                case EquipmentSlot.Boots:
                    position = new Vector2(106f, -80f);
                    break;
                case EquipmentSlot.Earring1:
                    position = new Vector2(-28f, 124f);
                    break;
                case EquipmentSlot.Earring2:
                    position = new Vector2(28f, 124f);
                    break;
                case EquipmentSlot.MainWeapon:
                    position = new Vector2(-78f, -126f);
                    break;
                case EquipmentSlot.Ring1:
                    position = new Vector2(-26f, -126f);
                    break;
                case EquipmentSlot.Ring2:
                    position = new Vector2(26f, -126f);
                    break;
                case EquipmentSlot.SecondaryWeapon:
                    position = new Vector2(78f, -126f);
                    break;
                default:
                    return Vector2.zero;
            }

            return position * SlotPositionScale;
        }

        private void ConfigureLayoutDropTarget()
        {
            Image layoutImage = GetComponent<Image>();
            if (layoutImage == null)
            {
                return;
            }

            layoutImage.raycastTarget = true;
            EquipmentDropTarget dropTarget = GetComponent<EquipmentDropTarget>() ??
                gameObject.AddComponent<EquipmentDropTarget>();
            dropTarget.Configure(owner, layoutImage, null);
        }

        private void ConfigureExistingSlotDropTargets()
        {
            foreach (EquipmentSlot slot in System.Enum.GetValues(typeof(EquipmentSlot)))
            {
                Transform slotTransform = transform.Find($"Equipment Slot {slot}");
                if (slotTransform == null)
                {
                    continue;
                }

                Image frame = slotTransform.GetComponent<Image>();
                if (frame == null)
                {
                    continue;
                }

                Transform empty = slotTransform.Find("Empty Icon");
                if (empty != null)
                {
                    Image emptyImage = empty.GetComponent<Image>();
                    if (emptyImage != null)
                    {
                        emptyImage.sprite = LoadSlotSprite(SlotIconName(slot));
                        emptyImage.color = emptyImage.sprite != null ? Color.white : Color.clear;
                        emptyImage.preserveAspect = true;
                        emptyImage.raycastTarget = false;
                        ApplyEmptySlotIconTransform(emptyImage, slot);
                        emptySlotIcons[slot] = emptyImage;
                    }
                }

                Transform equipped = slotTransform.Find("Equipped Icon");
                Image equippedImage = equipped != null ? equipped.GetComponent<Image>() : null;
                if (equippedImage == null)
                {
                    GameObject equippedObject = new GameObject(
                        "Equipped Icon",
                        typeof(RectTransform),
                        typeof(CanvasRenderer),
                        typeof(Image));
                    equippedObject.transform.SetParent(slotTransform, false);
                    equippedImage = equippedObject.GetComponent<Image>();
                    equippedImage.color = Color.clear;
                    equippedImage.preserveAspect = true;
                    equippedImage.raycastTarget = false;
                    equippedImage.rectTransform.anchorMin = equippedImage.rectTransform.anchorMax =
                        new Vector2(0.5f, 0.5f);
                    equippedImage.rectTransform.pivot = new Vector2(0.5f, 0.5f);
                    equippedImage.rectTransform.anchoredPosition = Vector2.zero;
                    equippedImage.rectTransform.sizeDelta = new Vector2(SlotSize * 0.78f, SlotSize * 0.78f);
                    equippedImage.gameObject.SetActive(false);
                }

                equippedSlotIcons[slot] = equippedImage;
                EquipmentItemDragSource dragSource = equippedImage.GetComponent<EquipmentItemDragSource>() ??
                    equippedImage.gameObject.AddComponent<EquipmentItemDragSource>();
                dragSource.Configure(activeEquipmentHeroId, slot, string.Empty, null);
                EquipmentDropTarget dropTarget = slotTransform.GetComponent<EquipmentDropTarget>() ??
                    slotTransform.gameObject.AddComponent<EquipmentDropTarget>();
                dropTarget.Configure(owner, frame, slot);
            }
        }

        private static void ApplyEmptySlotIconTransform(Image iconImage, EquipmentSlot slot)
        {
            if (iconImage == null)
            {
                return;
            }

            iconImage.rectTransform.localScale = slot == EquipmentSlot.Earring2
                ? new Vector3(-1f, 1f, 1f)
                : Vector3.one;
        }

        private static Sprite ItemIcon(GameContentCatalog catalog, InventoryItem item)
        {
            ItemDefinition definition = item == null ? null : catalog?.FindItem(item.DefinitionId);
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

        private static Sprite LoadSlotSprite(string iconName)
        {
            if (string.IsNullOrWhiteSpace(iconName))
            {
                return null;
            }

            if (SlotSpriteCache.TryGetValue(iconName, out Sprite cached))
            {
                return cached;
            }

            Sprite sprite = Resources.Load<Sprite>(SlotRoot + iconName);
            if (sprite == null)
            {
                Texture2D texture = Resources.Load<Texture2D>(SlotRoot + iconName);
                if (texture != null)
                {
                    sprite = Sprite.Create(
                        texture,
                        new Rect(0f, 0f, texture.width, texture.height),
                        new Vector2(0.5f, 0.5f),
                        100f);
                }
            }

            SlotSpriteCache[iconName] = sprite;
            return sprite;
        }

        private IEnumerator PlayHeroIdle()
        {
            int frame = 0;
            while (heroIdleSprites != null && heroIdleSprites.Length > 0)
            {
                heroPreview.sprite = heroIdleSprites[frame];
                frame = (frame + 1) % heroIdleSprites.Length;
                yield return new WaitForSecondsRealtime(0.36f);
            }
        }

        private Sprite[] LoadHeroIdleSprites(string heroId)
        {
            if (string.IsNullOrWhiteSpace(heroId))
            {
                return new Sprite[0];
            }

            List<Sprite> sprites = new List<Sprite>();
            for (int i = 1; i <= 3; i++)
            {
                Sprite sprite = LoadSprite($"Heroes/{heroId}/idle_{i}");
                if (sprite != null)
                {
                    sprites.Add(sprite);
                }
            }

            return sprites.ToArray();
        }

        private Sprite LoadSprite(string resourcePath)
        {
            Sprite sprite = Resources.Load<Sprite>(resourcePath);
            if (sprite != null)
            {
                return sprite;
            }

            Texture2D texture = Resources.Load<Texture2D>(resourcePath);
            if (texture == null)
            {
                return null;
            }

            Sprite generated = Sprite.Create(
                texture,
                new Rect(0f, 0f, texture.width, texture.height),
                new Vector2(0.5f, 0.5f),
                256f);
            generatedHeroSprites.Add(generated);
            return generated;
        }

        private static string LegacyHeroAnimationId(string heroId)
        {
            switch (heroId)
            {
                case "warrior": return "guardian";
                case "mage": return "pyromancer";
                case "archer": return "ranger";
                case "magic_warrior": return "spellblade";
                default: return heroId;
            }
        }

        private void StopIdleRoutine()
        {
            if (idleRoutine == null)
            {
                return;
            }

            StopCoroutine(idleRoutine);
            idleRoutine = null;
        }

        private void RestartIdleRoutine()
        {
            StopIdleRoutine();
            if (isActiveAndEnabled)
            {
                idleRoutine = StartCoroutine(PlayHeroIdle());
            }
        }

        private void ClearGeneratedSprites()
        {
            foreach (Sprite sprite in generatedHeroSprites)
            {
                if (sprite != null)
                {
                    Destroy(sprite);
                }
            }

            generatedHeroSprites.Clear();
        }

        private void OnDestroy()
        {
            StopIdleRoutine();
            ClearGeneratedSprites();
        }
    }

    public sealed class EquipmentDropTarget : MonoBehaviour, IDropHandler, IPointerEnterHandler, IPointerExitHandler
    {
        private ManagementUiController owner;
        private Image highlightTarget;
        private Color normalColor = Color.white;
        private EquipmentSlot targetSlot;
        private bool hasTargetSlot;

        public void Configure(ManagementUiController targetOwner, Image targetImage, EquipmentSlot? slot)
        {
            owner = targetOwner;
            highlightTarget = targetImage;
            hasTargetSlot = slot.HasValue;
            targetSlot = slot.GetValueOrDefault();
            if (highlightTarget != null)
            {
                normalColor = highlightTarget.color;
                highlightTarget.raycastTarget = true;
            }
        }

        public void OnDrop(PointerEventData eventData)
        {
            string itemInstanceId = InventoryItemDragSource.ActiveItemInstanceId;
            if (string.IsNullOrWhiteSpace(itemInstanceId) || owner == null)
            {
                RestoreHighlight();
                return;
            }

            owner.TryEquipInventoryItem(
                itemInstanceId,
                hasTargetSlot ? targetSlot : (EquipmentSlot?)null);
            RestoreHighlight();
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            if (!string.IsNullOrWhiteSpace(InventoryItemDragSource.ActiveItemInstanceId) && highlightTarget != null)
            {
                highlightTarget.color = new Color(1f, 0.92f, 0.55f, normalColor.a);
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

    public sealed class EquipmentItemDragSource : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        private static readonly List<EquipmentItemDragSource> ActiveSources =
            new List<EquipmentItemDragSource>();

        private Canvas rootCanvas;
        private Image sourceImage;
        private Image dragImage;
        private string itemInstanceId;
        private string heroId;
        private EquipmentSlot slot;

        public static bool HasActiveItem => !string.IsNullOrWhiteSpace(ActiveItemInstanceId);
        public static string ActiveHeroId { get; private set; }
        public static EquipmentSlot ActiveSlot { get; private set; }
        public static string ActiveItemInstanceId { get; private set; }

        public static void ClearActiveDragState()
        {
            ActiveHeroId = string.Empty;
            ActiveSlot = default;
            ActiveItemInstanceId = string.Empty;
            for (int i = ActiveSources.Count - 1; i >= 0; i--)
            {
                EquipmentItemDragSource source = ActiveSources[i];
                if (source == null)
                {
                    ActiveSources.RemoveAt(i);
                    continue;
                }

                source.ClearDragState();
            }
        }

        public void Configure(string sourceHeroId, EquipmentSlot sourceSlot, string instanceId, Sprite icon)
        {
            heroId = sourceHeroId ?? string.Empty;
            slot = sourceSlot;
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
            if (string.IsNullOrWhiteSpace(itemInstanceId) ||
                sourceImage == null ||
                sourceImage.sprite == null)
            {
                return;
            }

            ActiveHeroId = heroId;
            ActiveSlot = slot;
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
            ActiveHeroId = string.Empty;
            ActiveSlot = default;
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
                "Dragged Equipped Item", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
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
}
