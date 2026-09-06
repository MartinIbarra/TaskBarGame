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
        [SerializeField, Min(0.05f)] private float holdSeconds = 0.9167f;
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
                subtitleLabel.text = $"{(IsActTwoNode(nextNode.Id) ? "Acto II" : "Acto I")} · Nivel {nextNode.Difficulty}";
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
                case "town": return "City 1";
                case "narrow_bridge": return "Narrow Bridge";
                case "cave": return "M. Cave";
                case "cemetery": return "Dim Graveyard";
                case "goblin_village": return "Goblin Village";
                case "mountain_pass": return "Mountain Pass";
                case "barrow_road":
                case "tomb_pass": return "Pass-a-Deth";
                case "lost_forest": return "Lost Forest";
                case "last_bastion": return "Last Bastion";
                case "city2": return "City2";
                case "corrupt_pass": return "Corrupt Pass";
                case "lo_hueso": return "BoneYard";
                case "mt_secret": return "Mt. Secret";
                case "ancient_ruins": return "Ancient Ruins";
                case "arbol_morto": return "Big Dead Tree";
                case "mountain_pass_act2": return "Mountain Pass";
                case "black_tower": return "Black Tower";
                case "port": return "Port...?";
                case "lost_bay": return "Lost Bay";
                default: return nodeId;
            }
        }

        private static bool IsActTwoNode(string nodeId)
        {
            switch (nodeId)
            {
                case "city2":
                case "corrupt_pass":
                case "lo_hueso":
                case "mt_secret":
                case "ancient_ruins":
                case "arbol_morto":
                case "mountain_pass_act2":
                case "black_tower":
                case "port":
                case "lost_bay":
                    return true;
                default:
                    return false;
            }
        }
    }
}
