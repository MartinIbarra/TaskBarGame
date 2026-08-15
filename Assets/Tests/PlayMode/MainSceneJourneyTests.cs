using System;
using System.Collections;
using System.IO;
using System.Linq;
using NUnit.Framework;
using TaskbarTactics.Core.Models;
using TaskbarTactics.Core.Services;
using TaskbarTactics.Presentation;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace TaskbarTactics.Tests
{
    public sealed class MainSceneJourneyTests
    {
        private string saveDirectory;

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            saveDirectory = Path.Combine(
                Path.GetTempPath(), "TaskbarTacticsPlayMode", Guid.NewGuid().ToString("N"));
            GameAppController.TestSaveDirectoryOverride = saveDirectory;
            yield return SceneManager.LoadSceneAsync("Main", LoadSceneMode.Single);
            yield return null;
            yield return null;
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            GameAppController.TestSaveDirectoryOverride = null;
            if (Directory.Exists(saveDirectory))
            {
                Directory.Delete(saveDirectory, true);
            }

            yield return null;
        }

        [UnityTest]
        public IEnumerator MainSceneBootsWithCompleteRosterAndEditableUi()
        {
            GameAppController app = Object.FindFirstObjectByType<GameAppController>();
            WindowModeController window = Object.FindFirstObjectByType<WindowModeController>();

            Assert.That(app, Is.Not.Null);
            Assert.That(app.State.Party.Heroes, Has.Count.EqualTo(6));
            Assert.That(app.State.Party.Heroes.Count(hero => hero.IsSelected), Is.EqualTo(0));
            Assert.That(app.State.Inventory, Is.Not.Empty);
            Assert.That(window.CurrentMode, Is.EqualTo(WindowMode.Management));
            yield return null;
        }

        [UnityTest]
        public IEnumerator FormationRosterDragSourcesUseCurrentCatalogHeroIds()
        {
            GameAppController app = Object.FindFirstObjectByType<GameAppController>();
            HeroDragSource[] rosterSources = Resources.FindObjectsOfTypeAll<HeroDragSource>()
                .Where(source => source.GetComponent<Button>() != null &&
                                 source.name.StartsWith("Hero Class", StringComparison.Ordinal))
                .ToArray();
            string[] catalogHeroIds = app.Catalog.Heroes
                .Select(hero => hero.Id)
                .ToArray();

            Assert.That(rosterSources.Select(source => source.HeroId),
                Is.EquivalentTo(catalogHeroIds));

            HeroDragSource warriorSource = rosterSources.Single(source =>
                source.HeroId == "warrior");
            FormationSlotView targetSlot = Resources.FindObjectsOfTypeAll<FormationSlotView>()
                .Single(slot => slot.gameObject.activeInHierarchy &&
                                slot.Position.Equals(new FormationPosition(1, 0)));

            Assert.That(targetSlot.AssignHero("guardian"), Is.False);
            Assert.That(targetSlot.AssignHero(warriorSource.HeroId), Is.True);
            Assert.That(app.State.Party.GetHero("warrior").IsSelected, Is.True);
            yield return null;
        }

        [UnityTest]
        public IEnumerator UserCanReturnToStripAndStartExpeditionFromVisibleButton()
        {
            WindowModeController window = Object.FindFirstObjectByType<WindowModeController>();
            window.ShowStrip();
            yield return null;
            yield return null;
            Assert.That(window.CurrentMode, Is.EqualTo(WindowMode.Strip));

            window.ShowManagement();
            yield return null;
            Button start = Resources.FindObjectsOfTypeAll<Button>()
                .First(button => button.name == "Start Expedition Button");
            GameAppController app = Object.FindFirstObjectByType<GameAppController>();
            app.AssignHeroToFormationSlot("warrior", new TaskbarTactics.Core.Models.FormationPosition(1, 0));
            app.AssignHeroToFormationSlot("cleric", new TaskbarTactics.Core.Models.FormationPosition(1, 1));
            app.AssignHeroToFormationSlot("archer", new TaskbarTactics.Core.Models.FormationPosition(1, 2));
            app.AssignHeroToFormationSlot("rogue", new TaskbarTactics.Core.Models.FormationPosition(1, 3));
            start.onClick.Invoke();
            yield return null;

            Assert.That(app.State.Expedition.IsActive, Is.True);
            Assert.That(app.State.Expedition.CurrentNodeId, Is.EqualTo("town"));
            Assert.That(app.State.Party.IsFormationLocked, Is.False);
        }
    }
}
