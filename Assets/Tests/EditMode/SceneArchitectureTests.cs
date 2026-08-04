using NUnit.Framework;
using TaskbarTactics.Presentation;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace TaskbarTactics.Tests
{
    public sealed class SceneArchitectureTests
    {
        [Test]
        public void MainSceneContainsEditableArchitectureRoots()
        {
            const string path = "Assets/Scenes/Main.unity";
            Assert.That(AssetDatabase.LoadAssetAtPath<SceneAsset>(path), Is.Not.Null);

            Scene scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
            string[] expectedRoots = { "Systems", "Gameplay", "Strip UI", "Management UI", "Audio" };
            foreach (string expectedRoot in expectedRoots)
            {
                Assert.That(GameObject.Find(expectedRoot), Is.Not.Null, expectedRoot);
            }

            Assert.That(Object.FindFirstObjectByType<GameAppController>(), Is.Not.Null);
            Assert.That(Object.FindFirstObjectByType<WindowModeController>(), Is.Not.Null);
            Assert.That(Object.FindFirstObjectByType<CombatPresenter>(), Is.Not.Null);
        }

        [Test]
        public void ReusableUnitPrefabHasAnimatorAndAnimationBridge()
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(
                "Assets/Prefabs/Gameplay/Unit.prefab");

            Assert.That(prefab, Is.Not.Null);
            Assert.That(prefab.GetComponentInChildren<Animator>(), Is.Not.Null);
            Assert.That(prefab.GetComponentInChildren<UnitAnimationBridge>(), Is.Not.Null);
        }
    }
}
