using TaskbarTactics.Core.Models;
using UnityEngine;
using UnityEngine.UI;

namespace TaskbarTactics.Presentation
{
    // A small UI mesh shares the default UI material and respects canvas masking.
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class ItemRarityOverlay : MaskableGraphic
    {
        private const int GradientSteps = 8;
        private const float FrameInset = 2f;
        [SerializeField, Range(0.05f, 1f)] private float gradientDepth = 0.7f;

        public static void Bind(Image icon, InventoryItem item, float opacity, float depth)
        {
            ItemRarityOverlay overlay = icon.GetComponentInChildren<ItemRarityOverlay>(true);
            bool visible = item != null && item.Rarity != ItemRarity.Common &&
                icon.sprite != null && opacity > 0f;
            if (!visible)
            {
                if (overlay != null) overlay.gameObject.SetActive(false);
                return;
            }

            if (overlay == null)
            {
                // Slots are repeated dynamic views. The overlay follows its icon's lifecycle.
                var overlayObject = new GameObject("Item Rarity Gradient", typeof(RectTransform),
                    typeof(CanvasRenderer), typeof(ItemRarityOverlay));
                overlayObject.layer = icon.gameObject.layer;
                overlayObject.transform.SetParent(icon.transform, false);
                overlay = overlayObject.GetComponent<ItemRarityOverlay>();
            }

            overlay.raycastTarget = false;
            ColorUtility.TryParseHtmlString("#" + ItemRarityColors.Hex(item.Rarity), out Color tint);
            tint.a = Mathf.Clamp01(opacity);
            overlay.color = tint;
            overlay.gradientDepth = Mathf.Clamp(depth, 0.05f, 1f);
            RectTransform frame = icon.rectTransform.parent as RectTransform;
            if (frame == null) frame = icon.rectTransform;
            RectTransform rect = overlay.rectTransform;
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = new Vector2(Mathf.Max(0f, frame.rect.width - FrameInset * 2f),
                Mathf.Max(0f, frame.rect.height - FrameInset * 2f));
            rect.position = frame.TransformPoint(frame.rect.center);
            overlay.gameObject.SetActive(true);
            overlay.SetVerticesDirty();
        }

        protected override void OnPopulateMesh(VertexHelper vertices)
        {
            vertices.Clear();
            Rect area = rectTransform.rect;
            float depth = Mathf.Min(area.width, area.height) * 0.5f * gradientDepth;
            if (depth <= 0f || color.a <= 0f) return;

            // Concentric rings fade to zero alpha; the center remains completely clear.
            for (int ring = 0; ring <= GradientSteps; ring++)
            {
                float t = ring / (float)GradientSteps;
                float inset = depth * t;
                Color tint = color;
                tint.a *= (1f - t) * (1f - t);
                vertices.AddVert(new Vector3(area.xMin + inset, area.yMin + inset), tint, Vector2.zero);
                vertices.AddVert(new Vector3(area.xMin + inset, area.yMax - inset), tint, Vector2.zero);
                vertices.AddVert(new Vector3(area.xMax - inset, area.yMax - inset), tint, Vector2.zero);
                vertices.AddVert(new Vector3(area.xMax - inset, area.yMin + inset), tint, Vector2.zero);
                if (ring == 0) continue;
                int outer = (ring - 1) * 4;
                int inner = ring * 4;
                for (int side = 0; side < 4; side++)
                {
                    int next = (side + 1) % 4;
                    vertices.AddTriangle(outer + side, outer + next, inner + next);
                    vertices.AddTriangle(outer + side, inner + next, inner + side);
                }
            }
        }
    }
}
