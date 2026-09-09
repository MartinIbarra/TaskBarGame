using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace TaskbarTactics.Presentation
{
    public sealed class SkillTreeNodeView : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        private Image tooltipBackground;
        private TMP_Text tooltipLabel;

        public void Configure(Sprite icon, string displayName)
        {
            Image nodeImage = GetComponent<Image>();
            nodeImage.sprite = icon;
            nodeImage.preserveAspect = true;
            nodeImage.raycastTarget = true;

            GameObject tooltip = new GameObject(
                "Skill Node Tooltip",
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(Image),
                typeof(Canvas));
            tooltip.transform.SetParent(transform, false);
            Canvas tooltipCanvas = tooltip.GetComponent<Canvas>();
            tooltipCanvas.overrideSorting = true;
            tooltipCanvas.sortingOrder = 500;
            tooltipBackground = tooltip.GetComponent<Image>();
            tooltipBackground.color = new Color(0f, 0f, 0f, 0.72f);
            tooltipBackground.raycastTarget = false;
            tooltipBackground.maskable = false;
            RectTransform tooltipRect = tooltip.GetComponent<RectTransform>();
            tooltipRect.anchorMin = tooltipRect.anchorMax = new Vector2(0.5f, 0f);
            tooltipRect.pivot = new Vector2(0.5f, 1f);
            tooltipRect.anchoredPosition = new Vector2(0f, -42f);
            tooltipRect.sizeDelta = new Vector2(150f, 28f);

            GameObject labelObject = new GameObject(
                "Skill Node Name",
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(TextMeshProUGUI));
            labelObject.transform.SetParent(tooltip.transform, false);
            tooltipLabel = labelObject.GetComponent<TextMeshProUGUI>();
            tooltipLabel.font = Resources.Load<TMP_FontAsset>("UI/Fonts/VCR_OSD_MONO SDF");
            tooltipLabel.fontSize = 12f;
            tooltipLabel.alignment = TextAlignmentOptions.Center;
            tooltipLabel.color = new Color(1f, 0.95f, 0.78f, 1f);
            tooltipLabel.text = displayName;
            tooltipLabel.raycastTarget = false;
            tooltipLabel.maskable = false;
            RectTransform labelRect = tooltipLabel.rectTransform;
            labelRect.anchorMin = Vector2.zero;
            labelRect.anchorMax = Vector2.one;
            labelRect.offsetMin = new Vector2(6f, 0f);
            labelRect.offsetMax = new Vector2(-6f, 0f);
            labelRect.anchoredPosition = Vector2.zero;
            tooltip.SetActive(false);
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            if (tooltipBackground != null)
            {
                transform.SetAsLastSibling();
                tooltipBackground.transform.SetAsLastSibling();
                tooltipBackground.gameObject.SetActive(true);
            }
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            if (tooltipBackground != null)
            {
                tooltipBackground.gameObject.SetActive(false);
            }
        }
    }
}
