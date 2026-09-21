using System;
using TaskbarTactics.Core.Models;
using UnityEngine;

namespace TaskbarTactics.Content
{
    [Serializable]
    public sealed class ItemRarityScaling
    {
        public ItemRarity Rarity;
        [Min(1f)] public float BaseStatMultiplier = 1f;

        public ItemRarityScaling(ItemRarity rarity, float multiplier)
        {
            Rarity = rarity;
            BaseStatMultiplier = multiplier;
        }
    }
}
