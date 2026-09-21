using TaskbarTactics.Content;
using TaskbarTactics.Core.Models;
using TMPro;
using UnityEngine;
using UnityEngine.Localization.Tables;

namespace TaskbarTactics.Presentation
{
    // One authored panel is shared by all item slots, including future content.
    public sealed class ItemTooltipView : MonoBehaviour
    {
        [SerializeField] private TMP_Text label;
        [SerializeField] private CanvasGroup visibility;
        [SerializeField] private RectTransform bounds;
        [SerializeField] private StringTable english;
        [SerializeField] private StringTable spanish;
        [SerializeField, Min(120f), Tooltip("Maximum panel width, including padding, in canvas units.")]
        private float maxWidth = 420f;
        [SerializeField] private Vector2 pointerOffset = new Vector2(16f, -16f);
        [SerializeField, Min(0f)] private float padding = 12f;
        [SerializeField, Min(0f)] private float screenMargin = 12f;
        private ItemHoverTarget currentSource;
        private RectTransform panel;
        private Vector2 measuredBoundsSize;
        private Vector2 lastPointer;
        private Camera lastEventCamera;
        private bool sizing;

        public void ConfigureLocalization(StringTable en, StringTable es)
        {
            english = en;
            spanish = es;
        }

        public void Configure(TMP_Text text, CanvasGroup group, RectTransform screenBounds)
        {
            label = text;
            visibility = group;
            bounds = screenBounds;
            Hide();
        }

        private void Awake()
        {
            panel = (RectTransform)transform;
            Hide();
        }

        public void Show(ItemHoverTarget source, GameContentCatalog catalog, InventoryItem item,
            string language, Vector2 pointer, Camera eventCamera)
        {
            if (label == null || visibility == null || bounds == null || item == null) return;
            panel = (RectTransform)transform;
            StringTable table = language == "es" ? spanish : english;
            label.text = ItemTooltipFormatter.Format(catalog, item,
                key => table?.GetEntry(key)?.Value ?? english?.GetEntry(key)?.Value ?? key);
            ResizeToContent();
            currentSource = source;
            visibility.alpha = 1f;
            visibility.blocksRaycasts = false;
            visibility.interactable = false;
            transform.SetAsLastSibling();
            Move(source, pointer, eventCamera);
        }

        private void ResizeToContent()
        {
            sizing = true;
            measuredBoundsSize = bounds.rect.size;
            float inset = Mathf.Max(0f, padding);
            RectTransform textRect = label.rectTransform;
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = new Vector2(inset, inset);
            textRect.offsetMax = new Vector2(-inset, -inset);
            // The panel owns padding; TMP margins must not add a second inset.
            label.margin = Vector4.zero;
            label.enableAutoSizing = false;
            label.textWrappingMode = TextWrappingModes.Normal;

            float widthLimit = Mathf.Min(maxWidth, measuredBoundsSize.x - screenMargin * 2f);
            float textWidthLimit = Mathf.Max(1f, widthLimit - inset * 2f);
            float naturalWidth = label.GetPreferredValues(label.text).x;
            float textWidth = Mathf.Min(Mathf.Ceil(naturalWidth), textWidthLimit);
            textWidth = Mathf.Max(1f, textWidth);
            // Measure height only after choosing the width, including any wrapped lines.
            float textHeight = label.GetPreferredValues(label.text, textWidth, 0f).y;
            panel.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, textWidth + inset * 2f);
            panel.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, Mathf.Ceil(textHeight) + inset * 2f);
            sizing = false;
        }

        private void OnRectTransformDimensionsChange()
        {
            if (sizing || currentSource == null || bounds == null || label == null || panel == null ||
                measuredBoundsSize == bounds.rect.size) return;
            ResizeToContent();
            Move(currentSource, lastPointer, lastEventCamera);
        }

        public void Move(ItemHoverTarget source, Vector2 pointer, Camera eventCamera)
        {
            if (currentSource != source || bounds == null || panel == null) return;
            lastPointer = pointer;
            lastEventCamera = eventCamera;
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(bounds, pointer, eventCamera,
                    out Vector2 point)) return;
            Rect area = bounds.rect;
            point += pointerOffset;
            if (point.x + panel.rect.width > area.xMax - screenMargin)
                point.x -= panel.rect.width + pointerOffset.x * 2f;
            float left = area.xMin + screenMargin;
            float right = Mathf.Max(left, area.xMax - panel.rect.width - screenMargin);
            float top = area.yMax - screenMargin;
            float bottom = Mathf.Min(top, area.yMin + panel.rect.height + screenMargin);
            point.x = Mathf.Clamp(point.x, left, right);
            point.y = Mathf.Clamp(point.y, bottom, top);
            panel.localPosition = new Vector3(point.x, point.y, 0f);
        }

        public void Hide(ItemHoverTarget source = null)
        {
            if (source != null && currentSource != source) return;
            if (visibility != null) visibility.alpha = 0f;
            currentSource = null;
        }

        private void OnDisable() => Hide();
    }
}
