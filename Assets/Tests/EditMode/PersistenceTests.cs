using System;
using System.IO;
using System.Text.RegularExpressions;
using NUnit.Framework;
using TaskbarTactics.Core.Models;
using TaskbarTactics.Core.Progression;
using TaskbarTactics.Infrastructure.Persistence;

namespace TaskbarTactics.Tests
{
    public sealed class PersistenceTests
    {
        private string directory;

        [SetUp]
        public void SetUp()
        {
            directory = Path.Combine(Path.GetTempPath(), "TaskbarTacticsTests", Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(directory);
        }

        [TearDown]
        public void TearDown()
        {
            if (Directory.Exists(directory))
            {
                Directory.Delete(directory, true);
            }
        }

        [Test]
        public void SaveAndLoadPreserveStableIdsAndExpedition()
        {
            JsonSaveStore store = new JsonSaveStore(directory);
            GameState expected = TestFixtures.CreateGameState();

            store.Save(expected);
            GameState actual = store.LoadOrDefault();

            Assert.That(actual.Version, Is.EqualTo(GameState.CurrentVersion));
            Assert.That(actual.Party.Heroes[0].DefinitionId, Is.EqualTo("warrior"));
            Assert.That(actual.Party.Heroes[0].EquippedItems[0].ItemInstanceId,
                Is.EqualTo("item-001"));
            Assert.That(actual.Party.Heroes[0].CurrentHealth, Is.EqualTo(180));
            Assert.That(actual.Party.Heroes[0].UnlockedSkillIds,
                Does.Contain("healing_light"));
            Assert.That(actual.Expedition.CurrentNodeId, Is.EqualTo("node-03"));
        }

        [Test]
        public void CorruptPrimarySaveRecoversFromBackup()
        {
            JsonSaveStore store = new JsonSaveStore(directory);
            GameState first = TestFixtures.CreateGameState();
            store.Save(first);
            first.Gold = 250;
            store.Save(first);
            File.WriteAllText(store.PrimaryPath, "{not valid json");

            GameState recovered = store.LoadOrDefault();

            Assert.That(recovered.Gold, Is.EqualTo(100));
        }

        [Test]
        public void VersionOneSaveStartsFreshVersionTwoProfile()
        {
            JsonSaveStore store = new JsonSaveStore(directory);
            GameState legacy = TestFixtures.CreateGameState();
            store.Save(legacy);
            string json = File.ReadAllText(store.PrimaryPath);
            json = Regex.Replace(json, "\\\"Version\\\"\\s*:\\s*" + GameState.CurrentVersion,
                "\"Version\": 1");
            File.WriteAllText(store.PrimaryPath, json);

            GameState loaded = store.LoadOrDefault();

            Assert.That(loaded.Version, Is.EqualTo(GameState.CurrentVersion));
            Assert.That(loaded.Party.Heroes, Is.Empty);
            Assert.That(loaded.Inventory, Is.Empty);
        }

        [Test]
        public void VersionThreeMigrationPreservesSilver()
        {
            JsonSaveStore store = new JsonSaveStore(directory);
            GameState legacy = TestFixtures.CreateGameState();
            legacy.Silver = 13;
            store.Save(legacy);
            string json = File.ReadAllText(store.PrimaryPath);
            json = Regex.Replace(json, "\\\"Version\\\"\\s*:\\s*" + GameState.CurrentVersion,
                "\"Version\": 3");
            File.WriteAllText(store.PrimaryPath, json);

            GameState loaded = store.LoadOrDefault();

            Assert.That(loaded.Version, Is.EqualTo(GameState.CurrentVersion));
            Assert.That(loaded.Silver, Is.EqualTo(13));
        }

        [Test]
        public void DefeatReturnsToCampWithoutDiscardingInventory()
        {
            GameState state = TestFixtures.CreateGameState();
            int itemCount = state.Inventory.Count;

            ExpeditionResolver.ResolveDefeat(state);

            Assert.That(state.Expedition.IsActive, Is.False);
            Assert.That(state.Expedition.CurrentNodeId, Is.Empty);
            Assert.That(state.Inventory, Has.Count.EqualTo(itemCount));
        }
    }
}
