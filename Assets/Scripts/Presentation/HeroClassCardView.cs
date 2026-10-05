using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace TaskbarTactics.Presentation
{
    public sealed class HeroClassCardView : MonoBehaviour
    {
        [SerializeField] private Image icon;
        [SerializeField] private TMP_Text label;
        [SerializeField] private Image selectionFrame;
        [SerializeField] private Image emptyBackground;
        [SerializeField] private Sprite classSprite;

        public void Configure(Image classIcon, TMP_Text classLabel, Image frame, Image empty = null)
        {
            icon = classIcon;
            label = classLabel;
            selectionFrame = frame;
            emptyBackground = empty;
            classSprite = classIcon != null ? classIcon.sprite : null;
        }

        public void Refresh(
            string heroName,
            bool selected,
            bool active,
            bool dimUnselectedIcon,
            Color classNameColor)
        {
            if (label != null)
            {
                label.text = heroName;
                label.color = classNameColor;
            }

            if (selectionFrame != null)
            {
                selectionFrame.color = Color.clear;
            }

            if (icon != null)
            {
                icon.sprite = classSprite;
                icon.color = selected
                    ? Color.clear
                    : dimUnselectedIcon
                        ? new Color(0.72f, 0.75f, 0.8f, 0.82f)
                        : Color.white;
            }

            if (emptyBackground != null)
            {
                emptyBackground.color = Color.white;
            }
        }
    }
}
