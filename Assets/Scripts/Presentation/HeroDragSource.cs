using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using System.Collections.Generic;

namespace TaskbarTactics.Presentation
{
    public sealed class HeroDragSource :
        MonoBehaviour,
        IPointerDownHandler,
        IPointerUpHandler,
        IBeginDragHandler,
        IDragHandler,
        IEndDragHandler
    {
        private const float UiSoundVolume = 0.595f;
        private const float UiSoundPitch = 0.9f;

        [SerializeField] private string heroId = string.Empty;
        [SerializeField] private Sprite dragSprite;
        [SerializeField] private Canvas canvas;
        [SerializeField] private AudioClip grabClip;
        [SerializeField] private AudioClip slotClip;
        [SerializeField] private FormationSlotView sourceSlot;

        private Image dragImage;
        private RectTransform dragCanvasRect;
        private Vector2 dragStartPosition;
        private bool isDragging;

        public static HeroDragSource ActiveDrag { get; private set; }
        public string HeroId => heroId;
        public FormationSlotView SourceSlot => sourceSlot;

        public void Configure(string id, Sprite sprite, Canvas ownerCanvas)
        {
            Configure(id, sprite, ownerCanvas, null);
        }

        public void Configure(string id, Sprite sprite, Canvas ownerCanvas, FormationSlotView originSlot)
        {
            heroId = id;
            dragSprite = sprite;
            canvas = ownerCanvas;
            sourceSlot = originSlot;
            grabClip = Resources.Load<AudioClip>("Audio/UI/hero_slot");
            slotClip = Resources.Load<AudioClip>("Audio/UI/hero_grab");
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            ActiveDrag = this;
            PlayUiSound(grabClip);
            if (sourceSlot != null && eventData.clickCount >= 2)
            {
                UnequipFromSourceSlot();
            }
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            if (sourceSlot == null)
            {
                TryDropOnSlot(eventData);
            }

            if (ActiveDrag == this && dragImage == null)
            {
                ActiveDrag = null;
            }
        }

        public void OnBeginDrag(PointerEventData eventData)
        {
            if (dragSprite == null)
            {
                return;
            }

            isDragging = true;
            dragStartPosition = eventData.position;
            ActiveDrag = this;
            Canvas rootCanvas = canvas != null ? canvas : GetComponentInParent<Canvas>();
            if (rootCanvas == null)
            {
                ActiveDrag = null;
                return;
            }

            dragCanvasRect = rootCanvas.transform as RectTransform;
            GameObject ghost = new GameObject($"{heroId} Drag Icon", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            ghost.transform.SetParent(rootCanvas.transform, false);
            dragImage = ghost.GetComponent<Image>();
            dragImage.sprite = dragSprite;
            dragImage.preserveAspect = true;
            dragImage.raycastTarget = false;
            dragImage.rectTransform.sizeDelta = new Vector2(72, 72);
            ghost.transform.SetAsLastSibling();
            MoveGhost(eventData);
        }

        public void OnDrag(PointerEventData eventData)
        {
            MoveGhost(eventData);
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            bool handled = TryDropOnSlot(eventData);
            if (!handled && sourceSlot != null && (WasDroppedOnRoster(eventData) || WasDroppedBelowStart(eventData)))
            {
                handled = UnequipFromSourceSlot();
            }

            if (dragImage != null)
            {
                Destroy(dragImage.gameObject);
                dragImage = null;
                dragCanvasRect = null;
            }

            if (ActiveDrag == this)
            {
                ActiveDrag = null;
            }

            isDragging = false;
        }

        private void MoveGhost(PointerEventData eventData)
        {
            if (dragImage == null)
            {
                return;
            }

            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                dragCanvasRect,
                eventData.position,
                eventData.pressEventCamera,
                out Vector2 localPosition);
            dragImage.rectTransform.anchoredPosition = localPosition;
        }

        private bool TryDropOnSlot(PointerEventData eventData)
        {
            if (EventSystem.current == null)
            {
                return false;
            }

            List<RaycastResult> results = new List<RaycastResult>();
            EventSystem.current.RaycastAll(eventData, results);
            foreach (RaycastResult result in results)
            {
                FormationSlotView slot = result.gameObject.GetComponentInParent<FormationSlotView>();
                if (slot == null)
                {
                    continue;
                }

                if (slot.AssignHero(heroId))
                {
                    PlayUiSound(slotClip);
                }
                return true;
            }

            foreach (FormationSlotView slot in FindObjectsByType<FormationSlotView>(
                FindObjectsInactive.Exclude,
                FindObjectsSortMode.None))
            {
                RectTransform slotRect = slot.transform as RectTransform;
                if (slotRect == null ||
                    !slot.gameObject.activeInHierarchy)
                {
                    continue;
                }

                Rect expandedRect = ExpandedScreenRect(slotRect, SlotEventCamera(slotRect, eventData), 18f);
                if (!expandedRect.Contains(eventData.position))
                {
                    continue;
                }

                if (slot.AssignHero(heroId))
                {
                    PlayUiSound(slotClip);
                }
                return true;
            }

            return false;
        }

        private bool UnequipFromSourceSlot()
        {
            if (sourceSlot == null)
            {
                return false;
            }

            bool handled = sourceSlot.UnequipHero();
            if (handled)
            {
                PlayUiSound(slotClip);
            }

            return handled;
        }

        private bool WasDroppedOnRoster(PointerEventData eventData)
        {
            if (EventSystem.current == null)
            {
                return false;
            }

            List<RaycastResult> results = new List<RaycastResult>();
            EventSystem.current.RaycastAll(eventData, results);
            foreach (RaycastResult result in results)
            {
                if (result.gameObject.GetComponentInParent<HeroClassCardView>() != null)
                {
                    return true;
                }
            }

            return false;
        }

        private bool WasDroppedBelowStart(PointerEventData eventData)
        {
            return isDragging && eventData.position.y < dragStartPosition.y - 28f;
        }

        private static void PlayUiSound(AudioClip clip)
        {
            if (clip == null)
            {
                return;
            }

            AudioSource source = FindFirstObjectByType<AudioSource>();
            if (source != null)
            {
                float previousPitch = source.pitch;
                source.pitch = UiSoundPitch;
                source.PlayOneShot(clip, UiSoundVolume);
                source.pitch = previousPitch;
            }
        }

        private static Camera SlotEventCamera(RectTransform rect, PointerEventData eventData)
        {
            Canvas slotCanvas = rect.GetComponentInParent<Canvas>();
            if (slotCanvas != null && slotCanvas.renderMode == RenderMode.ScreenSpaceOverlay)
            {
                return null;
            }

            return eventData.pressEventCamera;
        }

        private static Rect ExpandedScreenRect(RectTransform rect, Camera camera, float padding)
        {
            if (rect == null)
            {
                return Rect.zero;
            }

            Vector3[] corners = new Vector3[4];
            rect.GetWorldCorners(corners);
            Vector2 min = RectTransformUtility.WorldToScreenPoint(camera, corners[0]);
            Vector2 max = min;
            for (int i = 1; i < corners.Length; i++)
            {
                Vector2 point = RectTransformUtility.WorldToScreenPoint(camera, corners[i]);
                min = Vector2.Min(min, point);
                max = Vector2.Max(max, point);
            }

            min -= Vector2.one * padding;
            max += Vector2.one * padding;
            return Rect.MinMaxRect(min.x, min.y, max.x, max.y);
        }
    }
}
