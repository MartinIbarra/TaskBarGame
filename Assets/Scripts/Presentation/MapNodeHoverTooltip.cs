using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

namespace TaskbarTactics.Presentation
{
    public sealed class MapNodeHoverTooltip : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        [SerializeField] private TMP_Text label;
        private bool hovering;

        public void Configure(TMP_Text tooltipLabel)
        {
            label = tooltipLabel;
            Hide();
        }

        public void SetContent(string text, Color color)
        {
            if (label == null)
            {
                return;
            }

            label.text = text;
            label.color = color;
            label.gameObject.SetActive(hovering);
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            hovering = true;
            if (label != null)
            {
                label.gameObject.SetActive(true);
            }
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            hovering = false;
            Hide();
        }

        private void Hide()
        {
            if (label != null)
            {
                label.gameObject.SetActive(false);
            }
        }
    }
}
