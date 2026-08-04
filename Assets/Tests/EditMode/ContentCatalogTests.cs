using System.Linq;
using NUnit.Framework;
using TaskbarTactics.Content;
using TaskbarTactics.Core.Models;

namespace TaskbarTactics.Tests
{
    public sealed class ContentCatalogTests
    {
        [Test]
        public void VerticalSliceContainsSixCompleteHeroes()
        {
            ContentBlueprint blueprint = ContentBlueprint.CreateVerticalSlice();

            Assert.That(blueprint.Heroes, Has.Count.EqualTo(6));
            Assert.That(blueprint.Heroes.All(hero => hero.ActiveSkillIds.Count == 2), Is.True);
            Assert.That(blueprint.Heroes.All(hero => hero.PassiveSkillIds.Count == 2), Is.True);
            Assert.That(blueprint.Heroes.All(hero => hero.TagIds.Count == 2), Is.True);
        }

        [Test]
        public void AuthoredMapHasRequiredEighteenNodeDistribution()
        {
            ContentBlueprint blueprint = ContentBlueprint.CreateVerticalSlice();

            Assert.That(blueprint.MapNodes, Has.Count.EqualTo(18));
            Assert.That(blueprint.MapNodes.Count(node => node.Type == MapNodeType.Combat), Is.EqualTo(10));
            Assert.That(blueprint.MapNodes.Count(node => node.Type == MapNodeType.Treasure), Is.EqualTo(3));
            Assert.That(blueprint.MapNodes.Count(node => node.Type == MapNodeType.Event), Is.EqualTo(2));
            Assert.That(blueprint.MapNodes.Count(node => node.Type == MapNodeType.Elite), Is.EqualTo(2));
            Assert.That(blueprint.MapNodes.Count(node => node.Type == MapNodeType.Boss), Is.EqualTo(1));
        }

        [Test]
        public void BestiaryHasEightNormalEnemiesAndOneBoss()
        {
            ContentBlueprint blueprint = ContentBlueprint.CreateVerticalSlice();

            Assert.That(blueprint.Enemies.Count(enemy => !enemy.IsBoss), Is.EqualTo(8));
            Assert.That(blueprint.Enemies.Count(enemy => enemy.IsBoss), Is.EqualTo(1));
        }

        [Test]
        public void EveryMapExitReferencesAnExistingNode()
        {
            ContentBlueprint blueprint = ContentBlueprint.CreateVerticalSlice();
            string[] nodeIds = blueprint.MapNodes.Select(node => node.Id).ToArray();

            Assert.That(
                blueprint.MapNodes.SelectMany(node => node.NextNodeIds).All(nodeIds.Contains),
                Is.True);
        }
    }
}
