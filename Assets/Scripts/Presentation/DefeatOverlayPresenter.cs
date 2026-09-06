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
            Show("YOU DIED", new Color(0.9f, 0.05f, 0.04f, 1f));

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

        public IEnumerator PlayActOneCompletion(int frameCount)
        {
            Show("ACT 1 Completed", new Color(0.35f, 0.8f, 1f, 1f));
            yield return WaitForFrames(frameCount);
            HideImmediate();
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            wasClicked = true;
            HideImmediate();
        }

        private void Show(string text, Color color)
        {
            if (message != null)
            {
                message.text = text;
                message.color = color;
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

        private static IEnumerator WaitForFrames(int frameCount)
        {
            for (int frame = 0; frame < Mathf.Max(0, frameCount); frame++)
            {
                yield return null;
            }
        }
    }
}
