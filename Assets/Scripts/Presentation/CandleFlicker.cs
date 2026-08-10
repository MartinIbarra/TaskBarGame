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

        private float timer;
        private int frameIndex;

        public void Configure(Image image, IEnumerable<Sprite> animationFrames, float fps = 6f)
        {
            target = image;
            frames = new List<Sprite>(animationFrames);
            framesPerSecond = Mathf.Max(1f, fps);
            ApplyFrame(0);
        }

        private void Update()
        {
            if (target == null || frames.Count <= 1)
            {
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
            target.preserveAspect = true;
        }
    }
}
