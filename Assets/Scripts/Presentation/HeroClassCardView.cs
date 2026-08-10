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

        public void Configure(Image classIcon, TMP_Text classLabel, Image frame)
        {
            icon = classIcon;
            label = classLabel;
            selectionFrame = frame;
        }

        public void Refresh(string heroName, bool selected, bool active)
        {
            if (label != null)
            {
                label.text = heroName;
                label.color = selected ? Color.white : new Color(0.78f, 0.82f, 0.9f, 0.9f);
            }

            if (selectionFrame != null)
            {
                selectionFrame.color = active
                    ? new Color(0.25f, 0.7f, 0.8f, 0.95f)
                    : selected
                        ? new Color(0.35f, 0.75f, 0.45f, 0.82f)
                        : new Color(1f, 1f, 1f, 0.1f);
            }

            if (icon != null)
            {
                icon.color = selected ? Color.white : new Color(0.72f, 0.75f, 0.8f, 0.82f);
            }
        }
    }
}
