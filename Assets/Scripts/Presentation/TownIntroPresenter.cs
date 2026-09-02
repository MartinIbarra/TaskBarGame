using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TaskbarTactics.Content;
using TaskbarTactics.Core.Models;
using UnityEngine;
using UnityEngine.UI;

namespace TaskbarTactics.Presentation
{
    public sealed class TownIntroPresenter : MonoBehaviour
    {
        private const float GateOpenVolume = 0.85f;

        [Header("Scene references")]
        [SerializeField] private CanvasGroup rootGroup;
        [SerializeField] private Image background;
        [SerializeField] private RectTransform gate;
        [SerializeField] private RectTransform heroRoot;
        [SerializeField] private AudioSource audioSource;
        [SerializeField] private AudioClip gateOpenClip;

        [Header("Timing")]
        [SerializeField, Min(0.1f)] private float gateOpenSeconds = 2.2f;
        [SerializeField, Min(0.1f)] private float heroRunSeconds = 1.25f;
        [SerializeField, Min(0.05f)] private float fadeOutSeconds = 0.45f;

        [Header("Layout")]
        [SerializeField] private Vector2 closedGatePosition = new Vector2(18f, -7f);
        [SerializeField] private Vector2 openGatePosition = new Vector2(18f, 72f);
        [SerializeField] private float heroStartX = 318f;
        [SerializeField] private float heroEndX = 1002f;
        [SerializeField] private float heroBaseY = -58f;
        [SerializeField] private float heroColumnSpacing = 28f;
        [SerializeField] private float heroStartDelay = 0.12f;
        [SerializeField] private float hopHeight = 16f;
        [SerializeField] private float zigzagWidth = 10f;
        [SerializeField] private float targetHeroHeight = 74f;

        private readonly List<Image> heroViews = new List<Image>();

        public void Configure(
            CanvasGroup group,
            Image sceneBackground,
            RectTransform gateTransform,
            RectTransform heroesParent,
            AudioSource source,
            AudioClip gateClip)
        {
            rootGroup = group;
            background = sceneBackground;
            gate = gateTransform;
            heroRoot = heroesParent;
            audioSource = source;
            gateOpenClip = gateClip != null
                ? gateClip
                : Resources.Load<AudioClip>("Audio/Events/gate_open");
            ConfigureAudioSource();
            HideImmediate();
        }

        public IEnumerator Play(
            IReadOnlyList<HeroState> selectedHeroes,
            GameContentCatalog catalog)
        {
            if (rootGroup == null || gate == null || heroRoot == null)
            {
                yield break;
            }

            BuildHeroViews(selectedHeroes, catalog);
            gameObject.SetActive(true);
            rootGroup.alpha = 1f;
            rootGroup.blocksRaycasts = true;
            rootGroup.interactable = false;
            gate.anchoredPosition = closedGatePosition;
            gate.SetAsLastSibling();

            if (audioSource != null && gateOpenClip != null)
            {
                ConfigureAudioSource();
                audioSource.PlayOneShot(gateOpenClip);
            }

            float elapsed = 0f;
            while (elapsed < gateOpenSeconds)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / gateOpenSeconds);
                gate.anchoredPosition = Vector2.Lerp(closedGatePosition, openGatePosition, Smooth(t));
                yield return null;
            }

            yield return RunHeroes();
            yield return FadeOut();
            HideImmediate();
        }

        private void BuildHeroViews(
            IReadOnlyList<HeroState> selectedHeroes,
            GameContentCatalog catalog)
        {
            ClearHeroViews();
            List<HeroState> heroes = selectedHeroes?.Take(GameAppController.PartySize).ToList() ?? new List<HeroState>();
            for (int i = 0; i < heroes.Count; i++)
            {
                HeroDefinition definition = catalog != null
                    ? catalog.FindHero(heroes[i].DefinitionId)
                    : null;
                GameObject heroObject = new GameObject(
                    $"Runner {i + 1} {heroes[i].DefinitionId}",
                    typeof(RectTransform),
                    typeof(CanvasRenderer),
                    typeof(Image));
                heroObject.transform.SetParent(heroRoot, false);
                Image image = heroObject.GetComponent<Image>();
                image.sprite = definition != null ? definition.Artwork : null;
                image.color = image.sprite != null
                    ? Color.white
                    : definition != null ? definition.Color : Color.white;
                image.preserveAspect = true;
                image.raycastTarget = false;

                RectTransform rect = image.rectTransform;
                rect.anchorMin = rect.anchorMax = new Vector2(0, 0.5f);
                rect.pivot = new Vector2(0.5f, 0f);
                rect.anchoredPosition = HeroStartPosition(i);
                rect.sizeDelta = HeroSize(image.sprite);
                heroViews.Add(image);
            }
        }

        private IEnumerator RunHeroes()
        {
            float elapsed = 0f;
            while (elapsed < heroRunSeconds)
            {
                elapsed += Time.unscaledDeltaTime;
                for (int i = 0; i < heroViews.Count; i++)
                {
                    float heroElapsed = elapsed - i * heroStartDelay;
                    float t = Mathf.Clamp01(heroElapsed / heroRunSeconds);
                    RectTransform rect = heroViews[i].rectTransform;
                    Vector2 start = HeroStartPosition(i);
                    float x = Mathf.Lerp(heroStartX, heroEndX, Smooth(t));
                    float hop = Mathf.Abs(Mathf.Sin((t * 6f + i * 0.4f) * Mathf.PI)) * hopHeight;
                    float zigzag = Mathf.Sin((t * 5f + i) * Mathf.PI) * zigzagWidth;
                    rect.anchoredPosition = new Vector2(x + zigzag, start.y + hop);
                }

                yield return null;
            }
        }

        private IEnumerator FadeOut()
        {
            float elapsed = 0f;
            while (elapsed < fadeOutSeconds)
            {
                elapsed += Time.unscaledDeltaTime;
                rootGroup.alpha = 1f - Mathf.Clamp01(elapsed / fadeOutSeconds);
                yield return null;
            }
        }

        private Vector2 HeroStartPosition(int index)
        {
            return new Vector2(heroStartX - index * heroColumnSpacing, heroBaseY);
        }

        private Vector2 HeroSize(Sprite sprite)
        {
            if (sprite == null)
            {
                return new Vector2(34f, targetHeroHeight);
            }

            float aspect = Mathf.Max(0.1f, sprite.rect.width / sprite.rect.height);
            return new Vector2(targetHeroHeight * aspect, targetHeroHeight);
        }

        private void HideImmediate()
        {
            if (rootGroup != null)
            {
                rootGroup.alpha = 0f;
                rootGroup.blocksRaycasts = false;
                rootGroup.interactable = false;
            }

            if (gate != null)
            {
                gate.anchoredPosition = closedGatePosition;
            }

            ClearHeroViews();
            gameObject.SetActive(false);
        }

        private void ClearHeroViews()
        {
            foreach (Image heroView in heroViews)
            {
                if (heroView != null)
                {
                    Destroy(heroView.gameObject);
                }
            }

            heroViews.Clear();
        }

        private void ConfigureAudioSource()
        {
            if (audioSource == null)
            {
                return;
            }

            audioSource.playOnAwake = false;
            audioSource.loop = false;
            audioSource.spatialBlend = 0f;
            audioSource.volume = GateOpenVolume;
            audioSource.mute = false;
        }

        private static float Smooth(float value)
        {
            return value * value * (3f - 2f * value);
        }
    }
}
