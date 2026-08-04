using System;
using System.Collections;
using System.IO;
using System.Linq;
using NUnit.Framework;
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
            Assert.That(app.State.Party.Heroes.Count(hero => hero.IsSelected), Is.EqualTo(3));
            Assert.That(app.State.Inventory, Is.Not.Empty);
            Assert.That(window.CurrentMode, Is.EqualTo(WindowMode.Management));
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
            start.onClick.Invoke();
            yield return null;

            GameAppController app = Object.FindFirstObjectByType<GameAppController>();
            Assert.That(app.State.Expedition.IsActive, Is.True);
            Assert.That(app.State.Expedition.CurrentNodeId, Is.EqualTo("node-01"));
            Assert.That(app.State.Party.IsFormationLocked, Is.True);
        }
    }
}
