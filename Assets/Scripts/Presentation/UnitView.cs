using TMPro;
using UnityEngine;

namespace TaskbarTactics.Presentation
{
    public sealed class UnitView : MonoBehaviour
    {
        [SerializeField] private SpriteRenderer body;
        [SerializeField] private SpriteRenderer healthFill;
        [SerializeField] private TMP_Text label;
        [SerializeField] private UnitAnimationBridge animationBridge;
        [SerializeField, Min(0.1f), Tooltip("World-space height used for imported hero artwork.")]
        private float targetArtworkHeight = 1.35f;

        private int maxHealth;
        private int currentHealth;

        public void ConfigureReferences(
            SpriteRenderer bodyRenderer,
            SpriteRenderer healthRenderer,
            TMP_Text nameLabel,
            UnitAnimationBridge bridge)
        {
            body = bodyRenderer;
            healthFill = healthRenderer;
            label = nameLabel;
            animationBridge = bridge;
        }

        public void Initialize(string unitName, Color color, int health, Sprite artwork = null)
        {
            maxHealth = Mathf.Max(1, health);
            currentHealth = maxHealth;
            ApplyArtwork(artwork, color);

            if (label != null)
            {
                label.text = unitName;
            }

            SetHealth(maxHealth);
            animationBridge?.SetDead(false);
            animationBridge?.SetMovement(0f);
        }

        public void PlayAttack()
        {
            animationBridge?.PlayAttack();
        }

        public void ReceiveDamage(int amount)
        {
            currentHealth = Mathf.Max(0, currentHealth - amount);
            SetHealth(currentHealth);
            if (currentHealth == 0)
            {
                animationBridge?.SetDead(true);
            }
            else
            {
                animationBridge?.PlayHit();
            }
        }

        public void SetMovement(float normalizedSpeed)
        {
            animationBridge?.SetMovement(normalizedSpeed);
        }

        private void ApplyArtwork(Sprite artwork, Color fallbackColor)
        {
            if (body == null)
            {
                return;
            }

            if (artwork == null)
            {
                body.color = fallbackColor;
                body.transform.localScale = Vector3.one;
                return;
            }

            body.sprite = artwork;
            body.color = Color.white;
            float sourceHeight = Mathf.Max(0.01f, artwork.bounds.size.y);
            float scale = targetArtworkHeight / sourceHeight;
            body.transform.localScale = new Vector3(scale, scale, 1f);
        }

        private void SetHealth(int value)
        {
            if (healthFill == null)
            {
                return;
            }

            float ratio = Mathf.Clamp01(value / (float)maxHealth);
            Vector3 scale = healthFill.transform.localScale;
            scale.x = ratio;
            healthFill.transform.localScale = scale;
        }
    }
}
