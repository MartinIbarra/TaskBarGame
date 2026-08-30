using UnityEngine;
using UnityEngine.UI;

namespace TaskbarTactics.Presentation
{
    public sealed class EquipmentPreviewLayoutView : MonoBehaviour
    {
        private const string SlotRoot = "UI/EquipmentSlots/";
        private const float SlotSize = 38f;

        private void Awake()
        {
            CreatePlaceholders();
        }

        public void CreatePlaceholders()
        {
            if (transform.Find("Equipment Slot MainWeapon") != null)
            {
                return;
            }

            CreateSlot("Head", "head", new Vector2(0f, 112f));
            CreateSlot("Shoulders", "shoulders", new Vector2(64f, 88f));
            CreateSlot("Neck", "neck", new Vector2(-64f, 60f));
            CreateSlot("Chest", "chest", new Vector2(0f, 32f));
            CreateSlot("Cloak", "cloak", new Vector2(96f, 32f));
            CreateSlot("MainWeapon", "mainhand", new Vector2(-95f, -8f));
            CreateSlot("Belt", "belt", new Vector2(0f, -20f));
            CreateSlot("Hands", "hands_right", new Vector2(64f, -48f));
            CreateSlot("Legs", "trousers", new Vector2(0f, -82f));
            CreateSlot("Boots", "feet", new Vector2(0f, -122f));
            CreateSlot("Ring1", "ring", new Vector2(-54f, -122f));
            CreateSlot("Ring2", "ring", new Vector2(54f, -122f));
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
            frame.sprite = Resources.Load<Sprite>(SlotRoot + "frame64");
            frame.preserveAspect = true;
            frame.raycastTarget = false;

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
            iconImage.sprite = Resources.Load<Sprite>(SlotRoot + iconName);
            iconImage.preserveAspect = true;
            iconImage.raycastTarget = false;
        }
    }
}
