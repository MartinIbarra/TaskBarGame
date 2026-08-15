using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace TaskbarTactics.Presentation
{
    public sealed class StripHudController : MonoBehaviour
    {
        [SerializeField] private TMP_Text statusLabel;
        [SerializeField] private TMP_Text nodeLabel;
        [SerializeField] private GameObject attentionIndicator;
        [SerializeField] private Button manageButton;
        [SerializeField] private Button menuButton;
        [SerializeField] private GameObject menuPanel;
        [SerializeField] private Button quitButton;
        private CanvasGroup labelsGroup;

        public void Configure(
            TMP_Text status,
            TMP_Text node,
            GameObject indicator,
            Button manage,
            Button menu,
            GameObject menuRoot,
            Button quit)
        {
            statusLabel = status;
            nodeLabel = node;
            attentionIndicator = indicator;
            manageButton = manage;
            menuButton = menu;
            menuPanel = menuRoot;
            quitButton = quit;
            labelsGroup = statusLabel != null
                ? statusLabel.transform.parent.GetComponent<CanvasGroup>()
                : null;
            if (labelsGroup == null && statusLabel != null)
            {
                labelsGroup = statusLabel.transform.parent.gameObject.AddComponent<CanvasGroup>();
            }
        }

        public void Bind(GameAppController app, WindowModeController window)
        {
            manageButton.onClick.AddListener(window.ShowManagement);
            menuButton.onClick.AddListener(() => menuPanel.SetActive(!menuPanel.activeSelf));
            quitButton.onClick.AddListener(app.Quit);
            app.StatusChanged += SetStatus;
            app.AttentionChanged += SetAttention;
            Refresh(app);
        }

        public void Refresh(GameAppController app)
        {
            string node = app.State.Expedition.CurrentNodeId;
            nodeLabel.text = string.IsNullOrEmpty(node) ? "Campamento" : node;
            statusLabel.text = app.CurrentStatus;
            attentionIndicator.SetActive(app.HasAttention);
        }

        private void SetStatus(string status, string node)
        {
            statusLabel.text = status;
            nodeLabel.text = string.IsNullOrEmpty(node) ? "Campamento" : node;
        }

        private void SetAttention(bool active)
        {
            attentionIndicator.SetActive(active);
        }

        public void SetCompactLabelsVisible(bool visible)
        {
            if (labelsGroup == null)
            {
                return;
            }

            labelsGroup.alpha = visible ? 1f : 0f;
        }
    }
}
