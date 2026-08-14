using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace TaskbarTactics.Presentation
{
    public sealed class DefeatOverlayPresenter : MonoBehaviour, IPointerClickHandler
    {
        [SerializeField] private CanvasGroup group;
        [SerializeField] private TMP_Text message;
        [SerializeField, Min(0.1f)] private float autoReturnSeconds = 5f;

        private bool wasClicked;

        public void Configure(CanvasGroup overlayGroup, TMP_Text label)
        {
            group = overlayGroup;
            message = label;
            HideImmediate();
        }

        public IEnumerator Play(WindowModeController windowMode)
        {
            wasClicked = false;
            Show();

            float elapsed = 0f;
            while (elapsed < autoReturnSeconds && !wasClicked)
            {
                elapsed += Time.unscaledDeltaTime;
                yield return null;
            }

            HideImmediate();
            if (!wasClicked)
            {
                windowMode?.ShowManagement();
            }
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            wasClicked = true;
            HideImmediate();
        }

        private void Show()
        {
            if (message != null)
            {
                message.text = "YOU DIED";
            }

            if (group == null)
            {
                return;
            }

            group.alpha = 1f;
            group.interactable = true;
            group.blocksRaycasts = true;
            group.gameObject.SetActive(true);
        }

        private void HideImmediate()
        {
            if (group == null)
            {
                return;
            }

            group.alpha = 0f;
            group.interactable = false;
            group.blocksRaycasts = false;
            group.gameObject.SetActive(false);
        }
    }
}
