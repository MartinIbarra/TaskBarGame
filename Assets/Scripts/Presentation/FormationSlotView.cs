using TaskbarTactics.Core.Models;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace TaskbarTactics.Presentation
{
    public sealed class FormationSlotView : MonoBehaviour, IDropHandler
    {
        [SerializeField] private Image slotFrame;
        [SerializeField] private Image heroIcon;
        [SerializeField] private FormationPosition position;
        [SerializeField] private Sprite defaultSprite;
        [SerializeField] private Sprite frontSprite;
        [SerializeField] private string heroId = string.Empty;

        private ManagementUiController owner;
        private HeroDragSource dragSource;

        public FormationPosition Position => position;

        public void Configure(
            ManagementUiController controller,
            Image frame,
            Image icon,
            FormationPosition slotPosition,
            Sprite normal,
            Sprite front)
        {
            owner = controller;
            slotFrame = frame;
            heroIcon = icon;
            position = slotPosition;
            defaultSprite = normal;
            frontSprite = front;
            EnsureDragSource();
        }

        public void SetOwner(ManagementUiController controller)
        {
            owner = controller;
        }

        public void SetPosition(FormationPosition slotPosition, Vector2 anchoredPosition)
        {
            position = slotPosition;
            RectTransform rect = transform as RectTransform;
            if (rect != null)
            {
                rect.anchoredPosition = anchoredPosition;
            }
        }

        public void SetHero(Sprite sprite, string assignedHeroId)
        {
            heroId = assignedHeroId ?? string.Empty;
            if (heroIcon == null)
            {
                return;
            }

            heroIcon.sprite = sprite;
            heroIcon.color = sprite == null ? Color.clear : Color.white;
            heroIcon.raycastTarget = sprite != null;
            EnsureDragSource();
            dragSource.Configure(heroId, sprite, null, this);
        }

        public void SetFront(bool isFront)
        {
            if (slotFrame == null)
            {
                return;
            }

            slotFrame.sprite = isFront && frontSprite != null ? frontSprite : defaultSprite;
        }

        public void OnDrop(PointerEventData eventData)
        {
            HeroDragSource source = eventData.pointerDrag != null
                ? eventData.pointerDrag.GetComponent<HeroDragSource>()
                : null;
            if (source == null && eventData.pointerDrag != null)
            {
                source = eventData.pointerDrag.GetComponentInParent<HeroDragSource>();
            }
            source ??= HeroDragSource.ActiveDrag;

            if (source != null && source.SourceSlot != this)
            {
                AssignHero(source.HeroId);
            }
        }

        public bool AssignHero(string heroId)
        {
            if (owner == null)
            {
                owner = FindFirstObjectByType<ManagementUiController>();
            }

            return owner != null && owner.AssignHeroToSlot(heroId, position);
        }

        public bool UnequipHero()
        {
            if (string.IsNullOrWhiteSpace(heroId))
            {
                return false;
            }

            if (owner == null)
            {
                owner = FindFirstObjectByType<ManagementUiController>();
            }

            return owner != null && owner.UnequipHeroFromSlot(heroId, position);
        }

        private void EnsureDragSource()
        {
            if (heroIcon == null)
            {
                return;
            }

            dragSource = heroIcon.GetComponent<HeroDragSource>();
            if (dragSource == null)
            {
                dragSource = heroIcon.gameObject.AddComponent<HeroDragSource>();
            }
        }
    }
}
