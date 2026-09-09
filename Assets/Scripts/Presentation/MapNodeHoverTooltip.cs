using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace TaskbarTactics.Presentation
{
    public sealed class MapNodeHoverTooltip : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        [SerializeField] private TMP_Text label;
        [SerializeField] private GameObject background;
        private bool hovering;

        public void Configure(TMP_Text tooltipLabel, GameObject tooltipBackground = null)
        {
            label = tooltipLabel;
            background = tooltipBackground;
            EnsureBackground();
            Hide();
        }

        public void SetContent(string text, Color color)
        {
            if (label == null)
            {
                return;
            }

            EnsureBackground();
            label.text = text;
            label.color = color;
            SetVisible(hovering);
        }

        public void SetSingleLineLayout(float width = 136f, float height = 18f)
        {
            if (label == null)
            {
                return;
            }

            RectTransform labelRect = label.rectTransform;
            labelRect.anchoredPosition = new Vector2(14f, 3f);
            labelRect.sizeDelta = new Vector2(width, height);
            label.textWrappingMode = TextWrappingModes.NoWrap;
            label.verticalAlignment = VerticalAlignmentOptions.Middle;

            if (background == null)
            {
                EnsureBackground();
            }

            RectTransform backgroundRect = background != null
                ? background.transform as RectTransform
                : null;
            if (backgroundRect != null)
            {
                backgroundRect.anchoredPosition = new Vector2(10f, 13f);
                backgroundRect.sizeDelta = new Vector2(93f, 24f);
            }
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            hovering = true;
            SetVisible(true);
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            hovering = false;
            Hide();
        }

        private void Hide()
        {
            SetVisible(false);
        }

        private void SetVisible(bool visible)
        {
            EnsureBackground();
            if (label != null)
            {
                label.gameObject.SetActive(visible);
            }

            if (background != null)
            {
                background.SetActive(visible);
            }
        }

        private void EnsureBackground()
        {
            if (background != null || label == null)
            {
                return;
            }

            RectTransform labelRect = label.rectTransform;
            GameObject backgroundObject = new GameObject("Tooltip Background", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            backgroundObject.transform.SetParent(label.transform.parent, false);
            backgroundObject.transform.SetSiblingIndex(label.transform.GetSiblingIndex());

            RectTransform backgroundRect = backgroundObject.GetComponent<RectTransform>();
            backgroundRect.anchorMin = labelRect.anchorMin;
            backgroundRect.anchorMax = labelRect.anchorMax;
            backgroundRect.pivot = labelRect.pivot;
            backgroundRect.anchoredPosition = labelRect.anchoredPosition + new Vector2(-4f, 0f);
            backgroundRect.sizeDelta = new Vector2(93f, labelRect.sizeDelta.y + 6f);

            Image image = backgroundObject.GetComponent<Image>();
            image.color = new Color(0f, 0f, 0f, 0.5f);
            image.raycastTarget = false;
            background = backgroundObject;
        }
    }
}
