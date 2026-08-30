using System.Collections;
using System.Collections.Generic;
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
        private float activeArtworkReferenceHeight;
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
            activeArtworkReferenceHeight = ArtworkReferenceHeight(GetPoseSprite(0) ?? artwork);
            ApplyArtwork(GetPoseSprite(0) ?? artwork, color, artworkHeightMultiplier, faceLeft);

            if (label != null)
            {
                label.text = unitName;
                label.gameObject.SetActive(false);
            }

            SetHealth(maxHealth);
            animationBridge?.SetDead(false);
            animationBridge?.SetMovement(0f);
            StartIdleAnimation();
        }

        public void PlayAttack()
        {
            animationBridge?.PlayAttack();
            PlayTemporaryPoses(new[] { 3, 4 }, 0.12f);
        }

        public void ReceiveDamage(int amount)
        {
            currentHealth = Mathf.Max(0, currentHealth - amount);
            SetHealth(currentHealth);
            if (currentHealth == 0)
            {
                isDead = true;
                StopPoseRoutines();
                ApplyPose(5);
                animationBridge?.SetDead(true);
            }
            else
            {
                animationBridge?.PlayHit();
                PlayTemporaryPose(5, 0.18f);
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
            float sourceHeight = Mathf.Max(0.01f, activeArtworkReferenceHeight > 0f
                ? activeArtworkReferenceHeight
                : artwork.bounds.size.y);
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
            if (healthFill.transform.parent != null)
            {
                healthFill.transform.parent.gameObject.SetActive(ratio > 0f);
            }

            Vector3 scale = healthFill.transform.localScale;
            scale.x = ratio;
            healthFill.transform.localScale = scale;

            Sprite fillSprite = healthFill.sprite;
            if (fillSprite != null)
            {
                Vector3 position = healthFill.transform.localPosition;
                position.x = -fillSprite.bounds.size.x * (1f - ratio) * 0.5f;
                healthFill.transform.localPosition = position;
            }
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
            PlayTemporaryPoses(new[] { poseIndex }, seconds);
        }

        private void PlayTemporaryPoses(int[] poseIndexes, float secondsPerPose)
        {
            if (poseSprites == null || poseIndexes == null || poseIndexes.Length == 0 || isDead)
            {
                return;
            }

            if (temporaryPoseRoutine != null)
            {
                StopCoroutine(temporaryPoseRoutine);
            }

            temporaryPoseRoutine = StartCoroutine(PlayTemporaryPoseRoutine(poseIndexes, secondsPerPose));
        }

        private IEnumerator PlayTemporaryPoseRoutine(int[] poseIndexes, float secondsPerPose)
        {
            if (idleRoutine != null)
            {
                StopCoroutine(idleRoutine);
                idleRoutine = null;
            }

            foreach (int poseIndex in poseIndexes)
            {
                if (GetPoseSprite(poseIndex) == null)
                {
                    continue;
                }

                ApplyPose(poseIndex);
                yield return new WaitForSecondsRealtime(secondsPerPose);
            }

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
                "attack_1",
                "attack_2",
                "hit",
                "defense"
            };
            string[][] fallbackNames =
            {
                new[] { "idle_1" },
                new[] { "idle_2" },
                new[] { "idle_3" },
                new[] { "attack_1", "attack" },
                new[] { "attack_2" },
                new[] { "hit", "death" },
                new[] { "defense" }
            };
            Sprite[] sprites = new Sprite[poseNames.Length];
            for (int i = 0; i < poseNames.Length; i++)
            {
                Texture2D texture = LoadFirstTexture(resourcePath, fallbackNames[i]);
                if (texture == null && i < 3)
                {
                    return null;
                }

                if (texture == null)
                {
                    continue;
                }

                sprites[i] = Sprite.Create(
                    texture,
                    new Rect(0f, 0f, texture.width, texture.height),
                    new Vector2(0.5f, 0f),
                    256f);
            }

            return sprites;
        }

        private static float ArtworkReferenceHeight(Sprite artwork)
        {
            return artwork != null ? artwork.bounds.size.y : 0f;
        }

        private static Texture2D LoadFirstTexture(string resourcePath, IEnumerable<string> names)
        {
            foreach (string name in names)
            {
                Texture2D texture = Resources.Load<Texture2D>($"{resourcePath}/{name}");
                if (texture != null)
                {
                    return texture;
                }
            }

            return null;
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
