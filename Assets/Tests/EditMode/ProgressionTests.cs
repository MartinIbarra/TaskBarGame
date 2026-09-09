using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using TaskbarTactics.Core.Loot;
using TaskbarTactics.Core.Models;
using TaskbarTactics.Core.Progression;

namespace TaskbarTactics.Tests
{
    public sealed class ProgressionTests
    {
        [Test]
        public void SynergyActivatesAtTwoSourcesAndUpgradesAtThree()
        {
            SynergyResolver resolver = new SynergyResolver();
            List<TagSource> sources = new List<TagSource>
            {
                new TagSource("warrior", new[] { "guard", "steel" }),
                new TagSource("cleric", new[] { "guard", "sacred" }),
                new TagSource("shield", new[] { "guard" })
            };

            IReadOnlyList<ActiveSynergy> active = resolver.Resolve(sources);

            Assert.That(active, Has.Count.EqualTo(1));
            Assert.That(active[0].TagId, Is.EqualTo("guard"));
            Assert.That(active[0].Tier, Is.EqualTo(2));
        }

        [Test]
        public void LootGenerationIsStableForTheSameSeed()
        {
            LootTable table = TestFixtures.CreateLootTable();
            LootGenerator generator = new LootGenerator();

            IReadOnlyList<InventoryItem> first = generator.Generate(table, 9191, 4);
            IReadOnlyList<InventoryItem> second = generator.Generate(table, 9191, 4);

            Assert.That(second, Is.EqualTo(first));
        }

        [Test]
        public void ItemBonusCountFollowsRarityRules()
        {
            LootTable table = TestFixtures.CreateLootTable();
            LootGenerator generator = new LootGenerator();

            IReadOnlyList<InventoryItem> items = generator.Generate(table, 9191, 100);

            Assert.That(items.Where(item => item.Rarity == ItemRarity.Common)
                .All(item => item.ItemBonusIds.Count == 0), Is.True);
            Assert.That(items.Where(item => item.Rarity == ItemRarity.Rare)
                .All(item => item.ItemBonusIds.Count == 1), Is.True);
            Assert.That(items.Where(item => item.Rarity == ItemRarity.Epic)
                .All(item => item.ItemBonusIds.Count == 2), Is.True);
        }

        [TestCase(RoutePreference.Safety, "safe")]
        [TestCase(RoutePreference.Loot, "treasure")]
        [TestCase(RoutePreference.Challenge, "elite")]
        public void RouteSelectorUsesConfiguredPriority(RoutePreference preference, string expectedNode)
        {
            List<MapNodeState> choices = new List<MapNodeState>
            {
                new MapNodeState("safe", MapNodeType.Combat, 1),
                new MapNodeState("treasure", MapNodeType.Treasure, 2),
                new MapNodeState("elite", MapNodeType.Elite, 4)
            };

            MapNodeState selected = new RouteSelector().SelectNext(choices, preference);

            Assert.That(selected.Id, Is.EqualTo(expectedNode));
        }

        [Test]
        public void OfflineProgressClampsToEightHoursAndIgnoresNegativeTime()
        {
            DateTime lastSave = new DateTime(2026, 7, 20, 8, 0, 0, DateTimeKind.Utc);
            OfflineProgressService service = new OfflineProgressService(TimeSpan.FromHours(8));

            OfflineProgressResult longAbsence = service.Calculate(lastSave, lastSave.AddDays(3), 45);
            OfflineProgressResult clockMovedBack = service.Calculate(lastSave, lastSave.AddMinutes(-10), 45);

            Assert.That(longAbsence.SimulatedDuration, Is.EqualTo(TimeSpan.FromHours(8)));
            Assert.That(clockMovedBack.SimulatedDuration, Is.EqualTo(TimeSpan.Zero));
            Assert.That(clockMovedBack.ResolvedNodes, Is.Zero);
        }
    }
}
