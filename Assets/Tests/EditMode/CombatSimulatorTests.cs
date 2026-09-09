using System.Collections.Generic;
using NUnit.Framework;
using TaskbarTactics.Core.Combat;
using TaskbarTactics.Core.Models;

namespace TaskbarTactics.Tests
{
    public sealed class CombatSimulatorTests
    {
        [Test]
        public void SameSeedAndFormationProduceIdenticalCombat()
        {
            CombatRequest request = TestFixtures.CreateCombatRequest(481516);
            CombatSimulator simulator = new CombatSimulator();

            CombatResult first = simulator.Simulate(request);
            CombatResult second = simulator.Simulate(request);

            Assert.That(second.Outcome, Is.EqualTo(first.Outcome));
            Assert.That(second.ElapsedMilliseconds, Is.EqualTo(first.ElapsedMilliseconds));
            Assert.That(second.Events, Is.EqualTo(first.Events));
            Assert.That(second.SurvivingHeroHealth, Is.EqualTo(first.SurvivingHeroHealth));
        }

        [Test]
        public void FrontRowTauntIsTargetedBeforeUnprotectedBackRow()
        {
            CombatRequest request = TestFixtures.CreateCombatRequest(42);
            request.Heroes[0].Position = new FormationPosition(1, 0);
            request.Heroes[0].HasTaunt = true;
            request.Heroes[1].Position = new FormationPosition(1, 2);
            request.Heroes[1].CurrentHealth = 1;

            CombatResult result = new CombatSimulator().Simulate(request);

            CombatEvent firstEnemyAttack = result.Events.Find(item => item.ActorSide == CombatSide.Enemy);
            Assert.That(firstEnemyAttack.TargetId, Is.EqualTo(request.Heroes[0].Id));
        }

        [Test]
        public void RangedUnitCannotTargetBeyondItsRange()
        {
            List<CombatantState> candidates = new List<CombatantState>
            {
                TestFixtures.Combatant("near", CombatSide.Enemy, 1, 1),
                TestFixtures.Combatant("far", CombatSide.Enemy, 2, 2)
            };
            CombatantState attacker = TestFixtures.Combatant("attacker", CombatSide.Hero, 0, 0);
            attacker.Range = 2;

            CombatantState target = TargetSelector.SelectTarget(attacker, candidates);

            Assert.That(target.Id, Is.EqualTo("near"));
        }

        [Test]
        public void FormationRejectsChangesWhileCombatIsRunning()
        {
            PartyState party = TestFixtures.CreateParty();
            party.IsFormationLocked = true;

            bool changed = party.TrySetFormation("warrior", new FormationPosition(2, 2));

            Assert.That(changed, Is.False);
            Assert.That(party.GetHero("warrior").Position, Is.EqualTo(new FormationPosition(1, 0)));
        }

        [Test]
        public void UnlockedHealingLightReplacesClericAttackForWoundedAlly()
        {
            CombatantState cleric = TestFixtures.Combatant(
                "cleric", CombatSide.Hero, 1, 1, 100, 10, 3);
            cleric.AttackSpeed = 5f;
            cleric.SpellPower = 20f;
            cleric.ActiveSkillId = "healing_light";
            cleric.ActiveSkillMagnitude = 1.5f;
            cleric.UnlockedSkillIds.Add("healing_light");

            CombatantState woundedAlly = TestFixtures.Combatant(
                "ally", CombatSide.Hero, 1, 0, 100, 1, 3);
            woundedAlly.CurrentHealth = 40;
            woundedAlly.AttackSpeed = 0.1f;
            CombatantState enemy = TestFixtures.Combatant(
                "enemy", CombatSide.Enemy, 1, 0, 1000, 1, 3);
            enemy.AttackSpeed = 0.1f;

            CombatResult result = new CombatSimulator().Simulate(new CombatRequest
            {
                Seed = 1,
                MaxDurationMilliseconds = 200,
                Heroes = new List<CombatantState> { cleric, woundedAlly },
                Enemies = new List<CombatantState> { enemy }
            });

            CombatEvent healing = result.Events.Find(item =>
                item.Kind == CombatEventKind.Healing);
            Assert.That(healing, Is.Not.Null);
            Assert.That(healing.ActorId, Is.EqualTo("cleric"));
            Assert.That(healing.TargetId, Is.EqualTo("ally"));
            Assert.That(healing.Amount, Is.EqualTo(30));
            Assert.That(result.HeroResources.Find(item => item.Id == "ally").CurrentHealth,
                Is.EqualTo(70));
        }
    }
}
