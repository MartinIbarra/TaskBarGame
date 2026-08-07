using System.Collections;
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
        private float targetArtworkHeight = 0.4f;

        private int maxHealth;
        private int currentHealth;
        private Sprite originalArtwork;
        private Sprite[] poseSprites;
        private Coroutine idleRoutine;
        private Coroutine temporaryPoseRoutine;
        private Color activeFallbackColor;
        private float activeArtworkHeightMultiplier = 1f;
        private bool activeFaceLeft;
        private bool isDead;

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

        public void Initialize(
            string unitName,
            Color color,
            int health,
            Sprite artwork = null,
            float artworkHeightMultiplier = 1f,
            bool faceLeft = false,
            bool usePoseAnimation = false,
            string poseResourcePath = null)
        {
            maxHealth = Mathf.Max(1, health);
            currentHealth = maxHealth;
            isDead = false;
            activeFallbackColor = color;
            activeArtworkHeightMultiplier = artworkHeightMultiplier;
            activeFaceLeft = faceLeft;
            originalArtwork = artwork;
            poseSprites = usePoseAnimation ? LoadPoseSprites(poseResourcePath) : null;
            ApplyArtwork(GetPoseSprite(0) ?? artwork, color, artworkHeightMultiplier, faceLeft);

            if (label != null)
            {
                label.text = unitName;
            }

            SetHealth(maxHealth);
            animationBridge?.SetDead(false);
            animationBridge?.SetMovement(0f);
            StartIdleAnimation();
        }

        public void PlayAttack()
        {
            animationBridge?.PlayAttack();
            PlayTemporaryPose(3, 0.24f);
        }

        public void ReceiveDamage(int amount)
        {
            currentHealth = Mathf.Max(0, currentHealth - amount);
            SetHealth(currentHealth);
            if (currentHealth == 0)
            {
                isDead = true;
                StopPoseRoutines();
                ApplyPose(4);
                animationBridge?.SetDead(true);
            }
            else
            {
                animationBridge?.PlayHit();
                PlayTemporaryPose(4, 0.18f);
            }
        }

        public void SetMovement(float normalizedSpeed)
        {
            animationBridge?.SetMovement(normalizedSpeed);
        }

        private void ApplyArtwork(
            Sprite artwork,
            Color fallbackColor,
            float artworkHeightMultiplier,
            bool faceLeft)
        {
            if (body == null)
            {
                return;
            }

            if (artwork == null)
            {
                body.color = fallbackColor;
                body.transform.localScale = new Vector3(faceLeft ? -1f : 1f, 1f, 1f);
                return;
            }

            body.sprite = artwork;
            body.color = Color.white;
            float sourceHeight = Mathf.Max(0.01f, artwork.bounds.size.y);
            float targetHeight = targetArtworkHeight * Mathf.Max(0.1f, artworkHeightMultiplier);
            float scale = targetHeight / sourceHeight;
            body.transform.localScale = new Vector3(faceLeft ? -scale : scale, scale, 1f);
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

        private void StartIdleAnimation()
        {
            if (poseSprites == null || poseSprites.Length < 3)
            {
                return;
            }

            if (idleRoutine != null)
            {
                StopCoroutine(idleRoutine);
            }

            idleRoutine = StartCoroutine(PlayIdleLoop());
        }

        private IEnumerator PlayIdleLoop()
        {
            int frame = 0;
            while (!isDead)
            {
                ApplyPose(frame);
                frame = (frame + 1) % 3;
                yield return new WaitForSecondsRealtime(0.36f);
            }
        }

        private void PlayTemporaryPose(int poseIndex, float seconds)
        {
            if (poseSprites == null || poseSprites.Length <= poseIndex || isDead)
            {
                return;
            }

            if (temporaryPoseRoutine != null)
            {
                StopCoroutine(temporaryPoseRoutine);
            }

            temporaryPoseRoutine = StartCoroutine(PlayTemporaryPoseRoutine(poseIndex, seconds));
        }

        private IEnumerator PlayTemporaryPoseRoutine(int poseIndex, float seconds)
        {
            if (idleRoutine != null)
            {
                StopCoroutine(idleRoutine);
                idleRoutine = null;
            }

            ApplyPose(poseIndex);
            yield return new WaitForSecondsRealtime(seconds);
            temporaryPoseRoutine = null;
            StartIdleAnimation();
        }

        private void ApplyPose(int poseIndex)
        {
            Sprite pose = GetPoseSprite(poseIndex);
            if (pose != null)
            {
                ApplyArtwork(
                    pose,
                    activeFallbackColor,
                    activeArtworkHeightMultiplier,
                    activeFaceLeft);
            }
        }

        private Sprite GetPoseSprite(int poseIndex)
        {
            return poseSprites != null && poseSprites.Length > poseIndex
                ? poseSprites[poseIndex]
                : null;
        }

        private Sprite[] LoadPoseSprites(string resourcePath)
        {
            if (string.IsNullOrWhiteSpace(resourcePath))
            {
                return null;
            }

            string[] poseNames =
            {
                "idle_1",
                "idle_2",
                "idle_3",
                "attack",
                "hit"
            };
            Sprite[] sprites = new Sprite[poseNames.Length];
            for (int i = 0; i < poseNames.Length; i++)
            {
                Texture2D texture = Resources.Load<Texture2D>($"{resourcePath}/{poseNames[i]}");
                if (texture == null)
                {
                    return null;
                }

                sprites[i] = Sprite.Create(
                    texture,
                    new Rect(0f, 0f, texture.width, texture.height),
                    new Vector2(0.5f, 0f),
                    256f);
            }

            return sprites;
        }

        private void StopPoseRoutines()
        {
            if (idleRoutine != null)
            {
                StopCoroutine(idleRoutine);
                idleRoutine = null;
            }

            if (temporaryPoseRoutine != null)
            {
                StopCoroutine(temporaryPoseRoutine);
                temporaryPoseRoutine = null;
            }
        }

        private void OnDestroy()
        {
            StopPoseRoutines();
            if (poseSprites == null)
            {
                return;
            }

            foreach (Sprite sprite in poseSprites)
            {
                if (sprite != null && sprite != originalArtwork)
                {
                    Destroy(sprite);
                }
            }
        }
    }
}
