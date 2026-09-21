using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using TaskbarTactics.Content;
using TaskbarTactics.Core.Equipment;
using TaskbarTactics.Core.Models;
using TaskbarTactics.Core.Stats;

namespace TaskbarTactics.Presentation
{
    public static class ItemTooltipFormatter
    {
        public static string Format(GameContentCatalog catalog, InventoryItem item, Func<string, string> text)
        {
            ItemDefinition definition = item == null ? null : catalog.FindItem(item.DefinitionId);
            if (definition == null) return string.Empty;
            ItemStatBreakdown stats = catalog.ResolveItemStats(item);
            string name = text(definition.DisplayNameKey);
            if (name == definition.DisplayNameKey)
                name = CultureInfo.InvariantCulture.TextInfo.ToTitleCase(definition.Id.Replace('_', ' '));
            var result = new StringBuilder();
            result.Append("<b><color=#").Append(RarityColor(item.Rarity)).Append('>')
                .Append(name).Append("</color></b>\n");
            result.Append(text($"item.rarity.{item.Rarity}"));
            result.Append(" · ").Append(text($"item.slot.{definition.Slot}"));
            if (definition.Descriptor.ArmorType != ArmorType.None)
                result.Append(" · ").Append(text($"item.armor.{definition.Descriptor.ArmorType}"));
            AppendSection(result, text("item.tooltip.base"), stats.Base, text);
            result.Append("\n\n<b><color=#").Append(RarityColor(item.Rarity)).Append('>')
                .Append(text("item.tooltip.rarity")).Append("</color></b>\n");
            if (stats.Rarity.Count == 0)
                result.Append(text("item.tooltip.no_rarity_bonus"));
            else
                AppendStats(result, stats.Rarity, text);
            if (stats.Extras.Count > 0)
                AppendSection(result, text("item.tooltip.extras"), stats.Extras, text);
            return result.ToString().TrimEnd();
        }

        public static string RarityColor(ItemRarity rarity)
        {
            switch (rarity)
            {
                case ItemRarity.Rare: return "66B5FF";
                case ItemRarity.Epic: return "C48CFF";
                case ItemRarity.Legendary: return "FFAD4D";
                default: return "F0F0E8";
            }
        }

        private static void AppendSection(StringBuilder result, string heading,
            IReadOnlyList<StatModifier> stats, Func<string, string> text)
        {
            if (stats.Count == 0) return;
            result.Append("\n\n<b>").Append(heading).Append("</b>\n");
            AppendStats(result, stats, text);
        }

        private static void AppendStats(StringBuilder result, IReadOnlyList<StatModifier> stats,
            Func<string, string> text)
        {
            for (int i = 0; i < stats.Count; i++)
            {
                StatModifier stat = stats[i];
                if (i > 0) result.Append('\n');
                result.Append(stat.Value >= 0f ? "+" : "")
                    .Append(stat.Value.ToString("0.###", CultureInfo.InvariantCulture));
                if (stat.Operation != StatModifierOperation.Flat) result.Append('%');
                else if (stat.Stat == HeroStatType.CriticalChance || stat.Stat == HeroStatType.Evasion ||
                         stat.Stat == HeroStatType.CooldownReduction)
                    result.Append(' ').Append(text("item.tooltip.percentage_points"));
                result.Append(' ').Append(text($"item.stat.{stat.Stat}"));
                if (stat.Operation == StatModifierOperation.MultiplicativePercent)
                    result.Append(" (").Append(text("item.tooltip.multiplicative")).Append(')');
            }
        }
    }
}
