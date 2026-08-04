using UnityEngine;

namespace TaskbarTactics.Content
{
    [CreateAssetMenu(menuName = "Taskbar Tactics/Cosmetic")]
    public sealed class CosmeticDefinition : StableDefinition
    {
        [SerializeField] private Color tint = Color.white;

        public Color Tint => tint;

        public void Configure(string cosmeticId, Color cosmeticTint)
        {
            SetId(cosmeticId);
            tint = cosmeticTint;
        }
    }
}
