using UnityEngine;
using UnityEngine.EventSystems;

namespace TaskbarTactics.Presentation
{
    public sealed class DraggableMapView :
        MonoBehaviour,
        IBeginDragHandler,
        IDragHandler,
        IScrollHandler
    {
        [SerializeField] private RectTransform content;
        [SerializeField] private RectTransform viewport;
        [SerializeField, Min(0.25f)] private float initialZoom = 1.3f;
        [SerializeField, Min(0.25f)] private float minZoom = 0.9f;
        [SerializeField, Min(0.25f)] private float maxZoom = 2.4f;
        [SerializeField, Min(0.01f)] private float zoomStep = 0.1f;

        private Vector2 dragStartPointer;
        private Vector2 dragStartPosition;
        private Vector2 initialPosition;
        private Vector2 panPadding;
        private bool useInitialPosition;
        private bool useTopLeftContentBounds;
        private bool zoomAroundPointer;
        private float zoom = 1f;

        public void Configure(RectTransform targetContent)
        {
            Configure(targetContent, initialZoom, minZoom, maxZoom, Vector2.zero);
        }

        public void Configure(RectTransform targetContent, float defaultZoom, float minimumZoom, float maximumZoom, Vector2 initialPosition)
        {
            content = targetContent;
            viewport = transform as RectTransform;
            panPadding = Vector2.zero;
            useTopLeftContentBounds = false;
            zoomAroundPointer = false;
            minZoom = Mathf.Max(0.25f, minimumZoom);
            maxZoom = Mathf.Max(minZoom, maximumZoom);
            initialZoom = Mathf.Clamp(defaultZoom, minZoom, maxZoom);
            zoom = Mathf.Clamp(initialZoom, minZoom, maxZoom);
            if (content != null)
            {
                content.localScale = new Vector3(zoom, zoom, 1f);
                content.anchoredPosition = initialPosition;
            }

            this.initialPosition = initialPosition;
            useInitialPosition = initialPosition != Vector2.zero;
            ClampContent();
        }

        public void ConfigureCentered(RectTransform targetContent, float defaultZoom, float minimumZoom, float maximumZoom)
        {
            ConfigureCentered(targetContent, defaultZoom, minimumZoom, maximumZoom, false);
        }

        public void ConfigureCentered(
            RectTransform targetContent,
            float defaultZoom,
            float minimumZoom,
            float maximumZoom,
            bool topLeftContentBounds)
        {
            ConfigureCentered(
                targetContent,
                defaultZoom,
                minimumZoom,
                maximumZoom,
                topLeftContentBounds,
                Vector2.zero,
                false);
        }

        public void ConfigureCentered(
            RectTransform targetContent,
            float defaultZoom,
            float minimumZoom,
            float maximumZoom,
            bool topLeftContentBounds,
            Vector2 contentPanPadding,
            bool preservePointerOnZoom)
        {
            content = targetContent;
            viewport = transform as RectTransform;
            panPadding = new Vector2(
                Mathf.Max(0f, contentPanPadding.x),
                Mathf.Max(0f, contentPanPadding.y));
            useTopLeftContentBounds = topLeftContentBounds;
            zoomAroundPointer = preservePointerOnZoom;
            minZoom = Mathf.Max(0.25f, minimumZoom);
            maxZoom = Mathf.Max(minZoom, maximumZoom);
            initialZoom = Mathf.Clamp(defaultZoom, minZoom, maxZoom);
            zoom = Mathf.Clamp(initialZoom, minZoom, maxZoom);
            if (content != null)
            {
                content.localScale = new Vector3(zoom, zoom, 1f);
                initialPosition = CenteredContentPosition();
                content.anchoredPosition = initialPosition;
            }

            useInitialPosition = true;
            ClampContent();
        }

        private void Start()
        {
            if (useInitialPosition && content != null)
            {
                content.anchoredPosition = initialPosition;
            }

            ClampContent();
        }

        public void FocusOn(RectTransform target)
        {
            FocusOn(target, new Vector2(0.5f, 0.5f));
        }

        public void FocusOnDefaultZoom(RectTransform target)
        {
            zoom = Mathf.Clamp(initialZoom, minZoom, maxZoom);
            if (content != null)
            {
                content.localScale = new Vector3(zoom, zoom, 1f);
            }

            FocusOn(target);
        }

        public void FocusOnAtZoom(RectTransform target, float targetZoom)
        {
            FocusOnAtZoom(target, targetZoom, new Vector2(0.5f, 0.5f));
        }

        public void FocusOnAtZoom(RectTransform target, float targetZoom, Vector2 viewportAnchor)
        {
            zoom = Mathf.Clamp(targetZoom, minZoom, maxZoom);
            if (content != null)
            {
                content.localScale = new Vector3(zoom, zoom, 1f);
            }

            FocusOn(target, viewportAnchor);
        }

        public void CenterOnContentPoint(Vector2 contentPoint, float targetZoom)
        {
            if (content == null)
            {
                return;
            }

            if (viewport == null)
            {
                viewport = transform as RectTransform;
            }

            if (viewport == null)
            {
                return;
            }

            zoom = Mathf.Clamp(targetZoom, minZoom, maxZoom);
            content.localScale = new Vector3(zoom, zoom, 1f);
            Vector2 viewportSize = viewport.rect.size;
            content.anchoredPosition = new Vector2(
                viewportSize.x * 0.5f - contentPoint.x * zoom,
                -viewportSize.y * 0.5f - contentPoint.y * zoom);
            ClampContent();
            initialPosition = content.anchoredPosition;
            useInitialPosition = true;
        }

        public void ResetView()
        {
            zoom = Mathf.Clamp(initialZoom, minZoom, maxZoom);
            if (content != null)
            {
                content.localScale = new Vector3(zoom, zoom, 1f);
                content.anchoredPosition = initialPosition;
            }

            ClampContent();
        }

        public void FocusOn(RectTransform target, Vector2 viewportAnchor)
        {
            if (content == null || target == null)
            {
                return;
            }

            if (viewport == null)
            {
                viewport = transform as RectTransform;
            }

            if (viewport == null)
            {
                return;
            }

            Vector2 targetPosition = target.anchoredPosition;
            Vector2 viewportSize = viewport.rect.size;
            Vector2 clampedAnchor = new Vector2(
                Mathf.Clamp01(viewportAnchor.x),
                Mathf.Clamp01(viewportAnchor.y));
            content.anchoredPosition = new Vector2(
                viewportSize.x * clampedAnchor.x - targetPosition.x * zoom,
                -viewportSize.y * clampedAnchor.y - targetPosition.y * zoom);
            ClampContent();
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
            content.anchoredPosition = dragStartPosition + delta;
            ClampContent();
        }

        public void OnScroll(PointerEventData eventData)
        {
            if (content == null)
            {
                return;
            }

            float nextZoom = Mathf.Clamp(
                zoom + eventData.scrollDelta.y * zoomStep,
                minZoom,
                maxZoom);
            if (Mathf.Approximately(nextZoom, zoom))
            {
                return;
            }

            Vector3 pointerWorld = Vector3.zero;
            Vector2 contentPoint = Vector2.zero;
            bool preservePointer = zoomAroundPointer && viewport != null &&
                RectTransformUtility.ScreenPointToWorldPointInRectangle(
                    viewport,
                    eventData.position,
                    eventData.pressEventCamera,
                    out pointerWorld) &&
                RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    content,
                    eventData.position,
                    eventData.pressEventCamera,
                    out contentPoint);

            zoom = nextZoom;
            content.localScale = new Vector3(zoom, zoom, 1f);

            if (preservePointer)
            {
                Vector3 contentPointWorld = content.TransformPoint(contentPoint);
                content.position += pointerWorld - contentPointWorld;
            }

            ClampContent();
        }

        private void ClampContent()
        {
            if (content == null)
            {
                return;
            }

            if (viewport == null)
            {
                viewport = transform as RectTransform;
            }

            if (viewport == null)
            {
                return;
            }

            Vector2 viewportSize = viewport.rect.size;
            Vector2 contentSize = content.rect.size * zoom;
            Vector2 position = content.anchoredPosition;

            position.x = ClampHorizontal(position.x, viewportSize.x, contentSize.x);
            position.y = ClampVertical(position.y, viewportSize.y, contentSize.y);
            content.anchoredPosition = position;
        }

        private float ClampHorizontal(float value, float viewportSize, float contentSize)
        {
            if (contentSize <= viewportSize)
            {
                float centered = (viewportSize - contentSize) * 0.5f;
                return Mathf.Clamp(value, centered - panPadding.x, centered + panPadding.x);
            }

            float min = viewportSize - contentSize - panPadding.x;
            return Mathf.Clamp(value, min, panPadding.x);
        }

        private float ClampVertical(float value, float viewportSize, float contentSize)
        {
            if (contentSize <= viewportSize)
            {
                float centered = useTopLeftContentBounds
                    ? (viewportSize - contentSize) * 0.5f
                    : (contentSize - viewportSize) * 0.5f;
                return Mathf.Clamp(value, centered - panPadding.y, centered + panPadding.y);
            }

            if (useTopLeftContentBounds)
            {
                float min = viewportSize - contentSize - panPadding.y;
                return Mathf.Clamp(value, min, panPadding.y);
            }

            float max = contentSize - viewportSize + panPadding.y;
            return Mathf.Clamp(value, -panPadding.y, max);
        }

        private Vector2 CenteredContentPosition()
        {
            if (content == null || viewport == null)
            {
                return Vector2.zero;
            }

            Vector2 viewportSize = viewport.rect.size;
            Vector2 contentSize = content.rect.size * zoom;
            return new Vector2(
                (viewportSize.x - contentSize.x) * 0.5f,
                useTopLeftContentBounds
                    ? (viewportSize.y - contentSize.y) * 0.5f
                    : (contentSize.y - viewportSize.y) * 0.5f);
        }
    }
}
