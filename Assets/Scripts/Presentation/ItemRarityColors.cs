using TaskbarTactics.Core.Models;

namespace TaskbarTactics.Presentation
{
    public static class ItemRarityColors
    {
        public static string Hex(ItemRarity rarity)
        {
            switch (rarity)
            {
                case ItemRarity.Rare: return "66B5FF";
                case ItemRarity.Epic: return "C48CFF";
                case ItemRarity.Legendary: return "FFAD4D";
                default: return "F0F0E8";
            }
        }
    }
}
