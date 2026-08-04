using System.Linq;
using NUnit.Framework;
using TaskbarTactics.Presentation;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

namespace TaskbarTactics.Tests
{
    public sealed class HeroAnimationArchitectureTests
    {
        private const string ControllerPath = "Assets/Animations/Unit.controller";
        private const string PrefabPath = "Assets/Prefabs/Gameplay/Unit.prefab";

        [Test]
        public void SharedControllerHasCompleteAnimationContract()
        {
            AnimatorController controller =
                AssetDatabase.LoadAssetAtPath<AnimatorController>(ControllerPath);

            Assert.That(controller, Is.Not.Null);
            AssertParameter(controller, "Speed", AnimatorControllerParameterType.Float);
            AssertParameter(controller, "Attack", AnimatorControllerParameterType.Trigger);
            AssertParameter(controller, "Hit", AnimatorControllerParameterType.Trigger);
            AssertParameter(controller, "IsDead", AnimatorControllerParameterType.Bool);

            string[] stateNames = controller.layers[0].stateMachine.states
                .Select(child => child.state.name)
                .ToArray();
            Assert.That(
                stateNames,
                Is.EquivalentTo(new[] { "Idle", "Running", "Attack", "Hit", "Death" }));
        }

        [TestCase("Idle", true)]
        [TestCase("Running", true)]
        [TestCase("Attack", false)]
        [TestCase("Hit", false)]
        [TestCase("Death", false)]
        public void ClipsUseCorrectLoopSettings(string stateName, bool shouldLoop)
        {
            AnimatorController controller =
                AssetDatabase.LoadAssetAtPath<AnimatorController>(ControllerPath);
            AnimatorState state = controller.layers[0].stateMachine.states
                .Select(child => child.state)
                .Single(candidate => candidate.name == stateName);

            Assert.That(state.motion, Is.TypeOf<AnimationClip>());
            AnimationClip clip = (AnimationClip)state.motion;
            Assert.That(
                AnimationUtility.GetAnimationClipSettings(clip).loopTime,
                Is.EqualTo(shouldLoop));
        }

        [Test]
        public void UnitPrefabKeepsAnimatedVisualSeparateFromWorldSpaceHud()
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);

            Assert.That(prefab, Is.Not.Null);
            Transform visualRoot = prefab.transform.Find("Visual Root");
            Assert.That(visualRoot, Is.Not.Null);
            Assert.That(visualRoot.GetComponent<Animator>(), Is.Not.Null);
            Assert.That(visualRoot.Find("Artwork")?.GetComponent<SpriteRenderer>(), Is.Not.Null);
            Assert.That(prefab.transform.Find("HUD/Health Background"), Is.Not.Null);
            Assert.That(prefab.transform.Find("HUD/Name Label"), Is.Not.Null);
            Assert.That(prefab.GetComponent<UnitAnimationBridge>(), Is.Not.Null);
            Assert.That(prefab.GetComponent<UnitView>(), Is.Not.Null);
        }

        private static void AssertParameter(
            AnimatorController controller,
            string name,
            AnimatorControllerParameterType type)
        {
            AnimatorControllerParameter parameter =
                controller.parameters.SingleOrDefault(candidate => candidate.name == name);
            Assert.That(parameter, Is.Not.Null, $"Missing Animator parameter {name}.");
            Assert.That(parameter.type, Is.EqualTo(type));
        }
    }
}
