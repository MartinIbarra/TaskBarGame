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
            Assert.That(
                blueprint.Heroes.Select(hero => hero.Id),
                Is.EqualTo(new[]
                {
                    "warrior", "cleric", "mage", "archer", "rogue", "magic_warrior"
                }));
            Assert.That(blueprint.Heroes.All(hero => hero.BaseStats.AttackSpeed > 0f), Is.True);
            Assert.That(blueprint.Heroes.All(hero => hero.EquipmentProfile != null), Is.True);
        }

        [Test]
        public void AuthoredMapMatchesFirstPlayableRoute()
        {
            ContentBlueprint blueprint = ContentBlueprint.CreateVerticalSlice();

            Assert.That(blueprint.MapNodes, Has.Count.EqualTo(9));
            Assert.That(blueprint.MapNodes.Count(node => node.Type == MapNodeType.Combat), Is.EqualTo(3));
            Assert.That(blueprint.MapNodes.Count(node => node.Type == MapNodeType.Treasure), Is.EqualTo(2));
            Assert.That(blueprint.MapNodes.Count(node => node.Type == MapNodeType.Event), Is.EqualTo(1));
            Assert.That(blueprint.MapNodes.Count(node => node.Type == MapNodeType.Elite), Is.EqualTo(2));
            Assert.That(blueprint.MapNodes.Count(node => node.Type == MapNodeType.Boss), Is.EqualTo(1));
            Assert.That(blueprint.MapNodes[0].Id, Is.EqualTo("town"));
            Assert.That(blueprint.MapNodes[8].Id, Is.EqualTo("last_bastion"));
        }

        [Test]
        public void BestiaryHasNineNormalEnemiesAndOneBoss()
        {
            ContentBlueprint blueprint = ContentBlueprint.CreateVerticalSlice();

            Assert.That(blueprint.Enemies.Count(enemy => !enemy.IsBoss), Is.EqualTo(9));
            Assert.That(blueprint.Enemies.Count(enemy => enemy.IsBoss), Is.EqualTo(1));
        }

        [Test]
        public void VerticalSliceContainsStarterWeaponLootSet()
        {
            ContentBlueprint blueprint = ContentBlueprint.CreateVerticalSlice();
            string[] starterWeaponIds =
            {
                "wooden_sword",
                "wooden_mace",
                "wooden_staff",
                "wooden_bow",
                "wooden_dagger"
            };

            Assert.That(
                starterWeaponIds.All(id => blueprint.Items.Any(item =>
                    item.Id == id &&
                    item.Descriptor.PrimarySlot == EquipmentSlot.MainWeapon)),
                Is.True);
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
