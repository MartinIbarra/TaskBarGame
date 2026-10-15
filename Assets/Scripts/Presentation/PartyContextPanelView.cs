using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace TaskbarTactics.Presentation
{
    public sealed class PartyContextPanelView : MonoBehaviour
    {
        [SerializeField] private Image subjectFrame;
        [SerializeField] private Image subjectIcon;
        [SerializeField] private TMP_Text title;
        [SerializeField] private TMP_Text subjectName;
        [SerializeField] private TMP_Text subjectRole;
        [SerializeField] private TMP_Text description;
        [SerializeField] private TMP_Text footerLabel;
        [SerializeField] private TMP_Text footerValue;
        private Sprite[] heroAnimationFrames;
        private Coroutine heroAnimationRoutine;
        private RectTransform informationDivider;
        private Vector2 subjectIconBasePosition;
        private bool hasSubjectIconBasePosition;
        private Vector2 descriptionBasePosition;
        private bool hasDescriptionBasePosition;

        public void Configure(
            Image frame,
            Image icon,
            TMP_Text panelTitle,
            TMP_Text name,
            TMP_Text role,
            TMP_Text details,
            TMP_Text label,
            TMP_Text value)
        {
            subjectFrame = frame;
            subjectIcon = icon;
            title = panelTitle;
            subjectName = name;
            subjectRole = role;
            description = details;
            footerLabel = label;
            footerValue = value;
        }

        public void ShowHero(
            Sprite frameSprite,
            Sprite iconSprite,
            Sprite[] idleFrames,
            string panelTitle,
            string name,
            string role,
            string details,
            string label,
            string value,
            Color classColor)
        {
            Show(frameSprite, iconSprite, panelTitle, name, role, details, label, value, classColor);
            heroAnimationFrames = idleFrames;
            if (subjectIcon != null && heroAnimationFrames != null && heroAnimationFrames.Length > 0)
            {
                subjectIcon.sprite = heroAnimationFrames[0];
                subjectIcon.enabled = true;
                StartHeroAnimation();
            }
        }

        public void ShowFormation(
            Sprite frameSprite,
            Sprite iconSprite,
            string panelTitle,
            string name,
            string formationType,
            string details,
            string label,
            string value)
        {
            Show(frameSprite, iconSprite, panelTitle, name, formationType, details, label, value,
                new Color(0.91f, 0.75f, 0.42f, 1f));
            if (subjectName != null)
            {
                subjectName.alignment = TextAlignmentOptions.Center;
            }
        }

        private void Show(
            Sprite frameSprite,
            Sprite iconSprite,
            string panelTitle,
            string name,
            string role,
            string details,
            string label,
            string value,
            Color nameColor)
        {
            StopHeroAnimation();
            heroAnimationFrames = null;
            ApplyTextSizing();
            if (subjectFrame != null)
            {
                subjectFrame.gameObject.SetActive(false);
            }

            if (subjectIcon != null)
            {
                subjectIcon.sprite = iconSprite;
                subjectIcon.enabled = iconSprite != null;
                RectTransform iconRect = subjectIcon.rectTransform;
                if (!hasSubjectIconBasePosition)
                {
                    subjectIconBasePosition = iconRect.anchoredPosition;
                    hasSubjectIconBasePosition = true;
                }

                iconRect.anchoredPosition = new Vector2(
                    subjectIconBasePosition.x,
                    subjectIconBasePosition.y - 2f);
            }

            SetText(title, panelTitle);
            SetText(subjectName, name);
            if (subjectName != null)
            {
                subjectName.color = nameColor;
                subjectName.alignment = TextAlignmentOptions.Left;
            }

            SetText(subjectRole, role);
            SetText(description, details);
            SetText(footerLabel, label);
            SetText(footerValue, value);
            ApplyDescriptionAlignment();
            PositionContextFooter();
        }

        private void ApplyTextSizing()
        {
            if (title != null)
            {
                title.fontSize = 23f;
                RectTransform titleRect = title.rectTransform;
                titleRect.anchoredPosition = new Vector2(12f, titleRect.anchoredPosition.y);
            }
            if (subjectName != null) subjectName.fontSize = 26f;
            if (subjectRole != null) subjectRole.fontSize = 21f;
            if (description != null)
            {
                description.fontSize = 22f;
                RectTransform descriptionRect = description.rectTransform;
                if (!hasDescriptionBasePosition)
                {
                    descriptionBasePosition = descriptionRect.anchoredPosition;
                    hasDescriptionBasePosition = true;
                }

                descriptionRect.anchoredPosition = new Vector2(
                    descriptionBasePosition.x,
                    descriptionBasePosition.y - 6f);
            }
            if (footerLabel != null) footerLabel.fontSize = 20f;
            if (footerValue != null) footerValue.fontSize = 18f;
        }

        private void ApplyDescriptionAlignment()
        {
            if (description != null)
            {
                description.textWrappingMode = TextWrappingModes.Normal;
                description.overflowMode = TextOverflowModes.Ellipsis;
                description.alignment = TextAlignmentOptions.Top | TextAlignmentOptions.Left;
                description.enableAutoSizing = description.text.IndexOf('\n') >= 0;
                description.fontSizeMin = 14f;
                description.fontSizeMax = 22f;
                description.ForceMeshUpdate();
            }

            if (footerValue != null)
            {
                footerValue.textWrappingMode = TextWrappingModes.Normal;
                footerValue.overflowMode = TextOverflowModes.Ellipsis;
                footerValue.alignment = TextAlignmentOptions.Top | TextAlignmentOptions.Center;
            }
        }

        private void PositionContextFooter()
        {
            if (description == null || footerLabel == null || footerValue == null)
            {
                return;
            }

            informationDivider ??= transform.Find("Info Divider") as RectTransform;
            RectTransform descriptionRect = description.rectTransform;
            float contentHeight = Mathf.Min(description.preferredHeight, descriptionRect.rect.height);
            float dividerTop = descriptionRect.anchoredPosition.y - contentHeight - 10f;

            if (informationDivider != null)
            {
                informationDivider.anchoredPosition = new Vector2(
                    informationDivider.anchoredPosition.x,
                    dividerTop);
                dividerTop -= informationDivider.rect.height + 8f;
            }
            else
            {
                dividerTop -= 12f;
            }

            RectTransform labelRect = footerLabel.rectTransform;
            labelRect.anchoredPosition = new Vector2(labelRect.anchoredPosition.x, dividerTop);
            RectTransform valueRect = footerValue.rectTransform;
            valueRect.anchoredPosition = new Vector2(
                valueRect.anchoredPosition.x,
                dividerTop - labelRect.rect.height - 6f);
        }

        private void OnEnable()
        {
            StartHeroAnimation();
        }

        private void OnDisable()
        {
            StopHeroAnimation();
        }

        private void StartHeroAnimation()
        {
            if (!isActiveAndEnabled || subjectIcon == null || heroAnimationFrames == null ||
                heroAnimationFrames.Length < 2 || heroAnimationRoutine != null)
            {
                return;
            }

            heroAnimationRoutine = StartCoroutine(AnimateHeroIdle());
        }

        private IEnumerator AnimateHeroIdle()
        {
            int frameIndex = 1;
            WaitForSecondsRealtime frameDelay = new WaitForSecondsRealtime(0.36f);
            while (heroAnimationFrames != null && heroAnimationFrames.Length > 1 && subjectIcon != null)
            {
                yield return frameDelay;
                if (!isActiveAndEnabled || heroAnimationFrames == null || subjectIcon == null)
                {
                    break;
                }

                subjectIcon.sprite = heroAnimationFrames[frameIndex];
                frameIndex = (frameIndex + 1) % heroAnimationFrames.Length;
            }

            heroAnimationRoutine = null;
        }

        private void StopHeroAnimation()
        {
            if (heroAnimationRoutine == null)
            {
                return;
            }

            StopCoroutine(heroAnimationRoutine);
            heroAnimationRoutine = null;
        }

        private static void SetText(TMP_Text target, string value)
        {
            if (target != null)
            {
                target.text = value ?? string.Empty;
            }
        }
    }
}
