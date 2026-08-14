using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace TaskbarTactics.Presentation
{
    public sealed class CandleFlicker : MonoBehaviour
    {
        [SerializeField] private Image target;
        [SerializeField] private List<Sprite> frames = new List<Sprite>();
        [SerializeField] private float framesPerSecond = 6f;
        [SerializeField] private bool preserveFrameAspect = true;

        private float timer;
        private int frameIndex;
        private Vector2 stableFrameSize;
        private bool hasStarted;

        private void Awake()
        {
            CaptureCurrentSize();
            ResetPlayback();
        }

        public void Configure(Image image, IEnumerable<Sprite> animationFrames, float fps = 6f, bool preserveAspect = true)
        {
            target = image;
            frames = new List<Sprite>(animationFrames);
            framesPerSecond = Mathf.Max(1f, fps);
            preserveFrameAspect = preserveAspect;
            CaptureCurrentSize();
            ResetPlayback();
            ApplyFrame(0);
        }

        private void Update()
        {
            if (target == null || frames.Count <= 1)
            {
                return;
            }

            if (!hasStarted)
            {
                hasStarted = true;
                return;
            }

            timer += Time.unscaledDeltaTime;
            float frameDuration = 1f / framesPerSecond;
            if (timer < frameDuration)
            {
                return;
            }

            timer -= frameDuration;
            ApplyFrame((frameIndex + 1) % frames.Count);
        }

        private void ApplyFrame(int index)
        {
            if (target == null || frames.Count == 0)
            {
                return;
            }

            frameIndex = Mathf.Clamp(index, 0, frames.Count - 1);
            target.sprite = frames[frameIndex];
            target.preserveAspect = preserveFrameAspect;
            if (stableFrameSize != Vector2.zero)
            {
                target.rectTransform.sizeDelta = stableFrameSize;
            }
        }

        private void CaptureCurrentSize()
        {
            if (target == null)
            {
                target = GetComponent<Image>();
            }

            if (target != null)
            {
                stableFrameSize = target.rectTransform.sizeDelta;
            }
        }

        private void ResetPlayback()
        {
            timer = 0f;
            frameIndex = 0;
            hasStarted = false;
        }
    }
}
