using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace TaskbarTactics.Presentation
{
    public sealed class SilverCurrencyHud : MonoBehaviour
    {
        private const int CoinFrameCount = 8;
        private const float CoinFrameSeconds = 0.12f;
        private const float HudWidth = 125.715f;
        private const float HudHeight = 42f;
        private const float CoinPositionX = 36f;

        // The source strip has slightly different transparent margins per frame.
        // These offsets keep the coin's visible center fixed while it rotates.
        private static readonly float[] CoinFramePositionOffsets =
        {
            4.2f, 0f, -1.8f, -0.6f, 0.6f, 1.2f, -0.6f, -3.6f
        };

        private readonly List<Sprite> generatedFrames = new List<Sprite>();
        private Image background;
        private Image coin;
        private TMP_Text amountLabel;
        private Coroutine animationRoutine;
        private GameAppController app;

        public void Bind(GameAppController targetApp)
        {
            if (app != null)
            {
                app.StateChanged -= Refresh;
            }

            app = targetApp;
            EnsureVisuals();
            if (app != null)
            {
                app.StateChanged += Refresh;
            }

            Refresh();
        }

        private void EnsureVisuals()
        {
            RectTransform root = transform as RectTransform;
            if (root != null)
            {
                root.anchorMin = root.anchorMax = new Vector2(0f, 1f);
                root.pivot = new Vector2(0f, 1f);
                root.anchoredPosition = new Vector2(78f, -8f);
                root.sizeDelta = new Vector2(HudWidth, HudHeight);
            }

            if (background == null)
            {
                GameObject backgroundObject = new GameObject(
                    "Silver Currency Layout",
                    typeof(RectTransform),
                    typeof(CanvasRenderer),
                    typeof(Image));
                backgroundObject.transform.SetParent(transform, false);
                background = backgroundObject.GetComponent<Image>();
                background.sprite = Resources.Load<Sprite>("UI/Command");
                background.type = Image.Type.Sliced;
                background.preserveAspect = false;
                background.raycastTarget = false;
                SetFullRect(background.rectTransform);
            }

            if (coin == null)
            {
                GameObject coinObject = new GameObject(
                    "Silver Coin",
                    typeof(RectTransform),
                    typeof(CanvasRenderer),
                    typeof(Image));
                coinObject.transform.SetParent(transform, false);
                coin = coinObject.GetComponent<Image>();
                coin.type = Image.Type.Simple;
                coin.preserveAspect = true;
                coin.raycastTarget = false;
                coin.rectTransform.anchorMin = coin.rectTransform.anchorMax = new Vector2(0f, 0.5f);
                coin.rectTransform.pivot = new Vector2(0f, 0.5f);
                coin.rectTransform.anchoredPosition = new Vector2(CoinPositionX, 0f);
                coin.rectTransform.sizeDelta = new Vector2(30f, 30f);
            }

            if (amountLabel == null)
            {
                GameObject labelObject = new GameObject(
                    "Silver Amount",
                    typeof(RectTransform),
                    typeof(CanvasRenderer),
                    typeof(TextMeshProUGUI));
                labelObject.transform.SetParent(transform, false);
                amountLabel = labelObject.GetComponent<TextMeshProUGUI>();
                amountLabel.font = Resources.Load<TMP_FontAsset>("UI/Fonts/VCR_OSD_MONO SDF");
                amountLabel.fontSize = 16f;
                amountLabel.color = new Color(0.9f, 0.9f, 0.9f, 1f);
                amountLabel.alignment = TextAlignmentOptions.MidlineLeft;
                amountLabel.textWrappingMode = TextWrappingModes.NoWrap;
                amountLabel.raycastTarget = false;
                amountLabel.rectTransform.anchorMin = amountLabel.rectTransform.anchorMax = new Vector2(0f, 0.5f);
                amountLabel.rectTransform.pivot = new Vector2(0f, 0.5f);
                amountLabel.rectTransform.anchoredPosition = new Vector2(70f, 0f);
                amountLabel.rectTransform.sizeDelta = new Vector2(90f, 30f);
            }

            background.transform.SetAsFirstSibling();
            coin.transform.SetAsLastSibling();
            amountLabel.transform.SetAsLastSibling();
        }

        private void Refresh()
        {
            if (amountLabel != null)
            {
                amountLabel.text = $"{app?.State?.Silver ?? 0}";
            }

            if (app != null && isActiveAndEnabled && animationRoutine == null)
            {
                StartCoinAnimation();
            }
        }

        private void StartCoinAnimation()
        {
            StopCoinAnimation();
            Texture2D strip = Resources.Load<Texture2D>("UI/SilverCoinFrames");
            if (strip == null || coin == null)
            {
                return;
            }

            ClearGeneratedFrames();
            float frameWidth = strip.width / (float)CoinFrameCount;
            for (int i = 0; i < CoinFrameCount; i++)
            {
                Sprite frame = Sprite.Create(
                    strip,
                    new Rect(i * frameWidth, 0f, frameWidth, strip.height),
                    new Vector2(0.5f, 0.5f),
                    100f);
                generatedFrames.Add(frame);
            }

            animationRoutine = StartCoroutine(AnimateCoin());
        }

        private IEnumerator AnimateCoin()
        {
            int frame = 0;
            while (coin != null && generatedFrames.Count > 0)
            {
                coin.sprite = generatedFrames[frame];
                coin.rectTransform.anchoredPosition = new Vector2(
                    CoinPositionX + CoinFramePositionOffsets[frame],
                    0f);
                frame = (frame + 1) % generatedFrames.Count;
                yield return new WaitForSecondsRealtime(CoinFrameSeconds);
            }
        }

        private void StopCoinAnimation()
        {
            if (animationRoutine != null)
            {
                StopCoroutine(animationRoutine);
                animationRoutine = null;
            }
        }

        private void ClearGeneratedFrames()
        {
            foreach (Sprite frame in generatedFrames)
            {
                if (frame != null)
                {
                    Destroy(frame);
                }
            }

            generatedFrames.Clear();
        }

        private static void SetFullRect(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }

        private void OnDestroy()
        {
            if (app != null)
            {
                app.StateChanged -= Refresh;
            }

            StopCoinAnimation();
            ClearGeneratedFrames();
        }

        private void OnEnable()
        {
            if (app != null)
            {
                StartCoinAnimation();
            }
        }
    }
}
