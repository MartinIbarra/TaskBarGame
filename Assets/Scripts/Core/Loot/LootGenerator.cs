using System;
using System.Collections.Generic;
using TaskbarTactics.Core.Models;

namespace TaskbarTactics.Core.Loot
{
    public sealed class LootEntry
    {
        public string DefinitionId;
        public EquipmentSlot Slot;
        public int Weight;

        public LootEntry(string definitionId, EquipmentSlot slot, int weight)
        {
            DefinitionId = definitionId;
            Slot = slot;
            Weight = weight;
        }
    }

    public sealed class LootTable
    {
        public List<LootEntry> Entries = new List<LootEntry>();
        public List<string> AffixIds = new List<string>();
    }

    public interface ILootGenerator
    {
        IReadOnlyList<InventoryItem> Generate(LootTable table, int seed, int count);
    }

    public sealed class LootGenerator : ILootGenerator
    {
        public IReadOnlyList<InventoryItem> Generate(LootTable table, int seed, int count)
        {
            if (table == null || table.Entries.Count == 0)
            {
                throw new ArgumentException("Loot table must contain at least one entry.", nameof(table));
            }

            Random random = new Random(seed);
            List<InventoryItem> items = new List<InventoryItem>();
            int totalWeight = 0;
            foreach (LootEntry entry in table.Entries)
            {
                totalWeight += Math.Max(0, entry.Weight);
            }

            for (int i = 0; i < count; i++)
            {
                LootEntry entry = PickEntry(table.Entries, totalWeight, random);
                int rarityRoll = random.Next(100);
                ItemRarity rarity = rarityRoll < 10
                    ? ItemRarity.Epic
                    : rarityRoll < 40
                        ? ItemRarity.Rare
                        : ItemRarity.Common;
                int affixCount = rarity == ItemRarity.Common ? 1 : 2;
                List<string> affixes = PickAffixes(table.AffixIds, affixCount, random);

                items.Add(new InventoryItem
                {
                    InstanceId = $"{seed:x8}-{i:x2}-{random.Next():x8}",
                    DefinitionId = entry.DefinitionId,
                    Slot = entry.Slot,
                    Rarity = rarity,
                    AffixIds = affixes
                });
            }

            return items;
        }

        private static LootEntry PickEntry(
            IReadOnlyList<LootEntry> entries,
            int totalWeight,
            Random random)
        {
            int roll = random.Next(totalWeight);
            int accumulated = 0;
            foreach (LootEntry entry in entries)
            {
                accumulated += Math.Max(0, entry.Weight);
                if (roll < accumulated)
                {
                    return entry;
                }
            }

            return entries[entries.Count - 1];
        }

        private static List<string> PickAffixes(
            IReadOnlyList<string> affixIds,
            int count,
            Random random)
        {
            List<string> available = new List<string>(affixIds);
            List<string> selected = new List<string>();
            while (selected.Count < count && available.Count > 0)
            {
                int index = random.Next(available.Count);
                selected.Add(available[index]);
                available.RemoveAt(index);
            }

            return selected;
        }
    }
}
