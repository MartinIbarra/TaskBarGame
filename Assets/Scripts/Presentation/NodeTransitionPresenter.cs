using System.Collections;
using TMPro;
using TaskbarTactics.Content;
using UnityEngine;
using UnityEngine.UI;

namespace TaskbarTactics.Presentation
{
    public sealed class NodeTransitionPresenter : MonoBehaviour
    {
        [SerializeField] private CanvasGroup rootGroup;
        [SerializeField] private RectTransform banner;
        [SerializeField] private TMP_Text titleLabel;
        [SerializeField] private TMP_Text subtitleLabel;
        [SerializeField] private StripHudController stripHud;

        [Header("Timing")]
        [SerializeField, Min(0.05f)] private float enterSeconds = 0.45f;
        [SerializeField, Min(0.05f)] private float holdSeconds = 0.75f;
        [SerializeField, Min(0.05f)] private float exitSeconds = 0.45f;

        [Header("Layout")]
        [SerializeField] private float hiddenLeftX = -960f;
        [SerializeField] private float centerX = 0f;
        [SerializeField] private float hiddenRightX = 960f;

        public void Configure(
            CanvasGroup group,
            RectTransform bannerTransform,
            TMP_Text title,
            TMP_Text subtitle,
            StripHudController strip)
        {
            rootGroup = group;
            banner = bannerTransform;
            titleLabel = title;
            subtitleLabel = subtitle;
            stripHud = strip;
            HideImmediate();
        }

        public IEnumerator Play(MapNodeDefinition nextNode)
        {
            if (rootGroup == null || banner == null || nextNode == null)
            {
                yield break;
            }

            stripHud?.SetCompactLabelsVisible(false);
            gameObject.SetActive(true);
            rootGroup.alpha = 1f;
            rootGroup.blocksRaycasts = false;
            rootGroup.interactable = false;

            if (titleLabel != null)
            {
                titleLabel.text = DisplayName(nextNode.Id);
            }

            if (subtitleLabel != null)
            {
                subtitleLabel.text = $"Acto I · Nivel {nextNode.Difficulty}";
            }

            yield return Slide(hiddenLeftX, centerX, enterSeconds);
            yield return new WaitForSecondsRealtime(holdSeconds);
            yield return Slide(centerX, hiddenRightX, exitSeconds);
            HideImmediate();
            stripHud?.SetCompactLabelsVisible(true);
        }

        private IEnumerator Slide(float startX, float endX, float seconds)
        {
            float elapsed = 0f;
            while (elapsed < seconds)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / seconds);
                Vector2 position = banner.anchoredPosition;
                position.x = Mathf.Lerp(startX, endX, Smooth(t));
                banner.anchoredPosition = position;
                yield return null;
            }
        }

        private void HideImmediate()
        {
            if (banner != null)
            {
                Vector2 position = banner.anchoredPosition;
                position.x = hiddenLeftX;
                banner.anchoredPosition = position;
            }

            if (rootGroup != null)
            {
                rootGroup.alpha = 0f;
                rootGroup.blocksRaycasts = false;
                rootGroup.interactable = false;
            }

            gameObject.SetActive(false);
        }

        private static float Smooth(float value)
        {
            return value * value * (3f - 2f * value);
        }

        private static string DisplayName(string nodeId)
        {
            switch (nodeId)
            {
                case "town": return "Inicio / Town";
                case "narrow_bridge": return "Puente estrecho";
                case "cave": return "Cueva";
                case "cemetery": return "Cementerio";
                case "goblin_village": return "Aldea goblin";
                case "mountain_pass": return "Paso entre montañas";
                case "barrow_road": return "Paso del Tomuer";
                case "lost_forest": return "Lost Forest";
                case "last_bastion": return "Último Bastión";
                default: return nodeId;
            }
        }
    }
}
