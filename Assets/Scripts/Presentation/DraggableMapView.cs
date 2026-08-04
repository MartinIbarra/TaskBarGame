using UnityEngine;
using UnityEngine.EventSystems;

namespace TaskbarTactics.Presentation
{
    public sealed class DraggableMapView : MonoBehaviour, IBeginDragHandler, IDragHandler
    {
        [SerializeField] private RectTransform content;
        [SerializeField] private Vector2 minPosition = new Vector2(-180f, -40f);
        [SerializeField] private Vector2 maxPosition = new Vector2(80f, 160f);

        private Vector2 dragStartPointer;
        private Vector2 dragStartPosition;

        public void Configure(RectTransform targetContent)
        {
            content = targetContent;
        }

        public void OnBeginDrag(PointerEventData eventData)
        {
            if (content == null)
            {
                return;
            }

            dragStartPointer = eventData.position;
            dragStartPosition = content.anchoredPosition;
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (content == null)
            {
                return;
            }

            Vector2 delta = eventData.position - dragStartPointer;
            Vector2 next = dragStartPosition + delta;
            next.x = Mathf.Clamp(next.x, minPosition.x, maxPosition.x);
            next.y = Mathf.Clamp(next.y, minPosition.y, maxPosition.y);
            content.anchoredPosition = next;
        }
    }
}
