using System.Collections.Generic;
using NUnit.Framework;
using TaskbarTactics.Core.Combat;
using TaskbarTactics.Core.Equipment;
using TaskbarTactics.Core.Models;
using TaskbarTactics.Core.Progression;
using TaskbarTactics.Core.Stats;

namespace TaskbarTactics.Tests
{
    public sealed class HeroSystemsTests
    {
        [Test]
        public void EffectiveStatsApplyLevelFlatPercentAndCapsInOrder()
        {
            HeroStats baseStats = HeroStats.CreateDefault();
            baseStats.MaxHealth = 100;
            baseStats.AttackPower = 20f;
            baseStats.AttackSpeed = 1f;
            baseStats.CriticalChance = 10f;
            HeroStats growth = new HeroStats
            {
                MaxHealth = 10,
                AttackPower = 2f,
                AttackSpeed = 0.1f,
                CriticalChance = 1f
            };
            List<StatModifier> modifiers = new List<StatModifier>
            {
                new StatModifier(HeroStatType.AttackPower, StatModifierOperation.Flat, 3f, "sword"),
                new StatModifier(HeroStatType.AttackPower, StatModifierOperation.AdditivePercent, 20f, "bonus"),
                new StatModifier(HeroStatType.AttackSpeed, StatModifierOperation.Flat, 10f, "haste"),
                new StatModifier(HeroStatType.CriticalChance, StatModifierOperation.Flat, 200f, "critical")
            };

            HeroStats effective = HeroStatsCalculator.Calculate(baseStats, growth, 3, modifiers);

            Assert.That(effective.MaxHealth, Is.EqualTo(120));
            Assert.That(effective.AttackPower, Is.EqualTo(32.4f).Within(0.001f));
            Assert.That(effective.AttackSpeed, Is.EqualTo(HeroStatLimits.MaxAttackSpeed));
            Assert.That(effective.CriticalChance, Is.EqualTo(HeroStatLimits.MaxCriticalChance));
        }

        [Test]
        public void ExperienceHasNoLevelCapAndUsesOneHundredTimesCurrentLevel()
        {
            HeroProgression progression = new HeroProgression(1, 0L);

            progression.AddExperience(600L);

            Assert.That(progression.Level, Is.EqualTo(4));
            Assert.That(progression.Experience, Is.EqualTo(0L));
            Assert.That(HeroProgression.ExperienceRequiredForLevel(250), Is.EqualTo(25000L));
        }

        [Test]
        public void WarriorCanMixOneHandedWeaponFamiliesInDualWield()
        {
            HeroEquipmentProfile profile = HeroEquipmentProfiles.Create(HeroClass.Warrior);
            EquipmentLoadout loadout = new EquipmentLoadout();
            EquipmentDescriptor sword = EquipmentDescriptor.Weapon(
                "sword", WeaponType.Sword, Handedness.OneHanded);
            EquipmentDescriptor axe = EquipmentDescriptor.Weapon(
                "axe", WeaponType.Axe, Handedness.OneHanded);

            EquipmentResult mainResult = EquipmentService.TryEquip(
                profile, loadout, EquipmentSlot.MainWeapon, sword);
            EquipmentResult secondaryResult = EquipmentService.TryEquip(
                profile, loadout, EquipmentSlot.SecondaryWeapon, axe);

            Assert.That(mainResult.Succeeded, Is.True);
            Assert.That(secondaryResult.Succeeded, Is.True);
            Assert.That(EquipmentService.IsDualWielding(loadout), Is.True);
        }

        [Test]
        public void SecondaryWeaponRequiresAnAttackingOneHandedMainWeapon()
        {
            HeroEquipmentProfile warrior = HeroEquipmentProfiles.Create(HeroClass.Warrior);
            EquipmentLoadout loadout = new EquipmentLoadout();

            EquipmentResult result = EquipmentService.TryEquip(
                warrior,
                loadout,
                EquipmentSlot.SecondaryWeapon,
                EquipmentDescriptor.Weapon(
                    "offhand_sword", WeaponType.Sword, Handedness.OneHanded));

            Assert.That(result.Succeeded, Is.False);
            Assert.That(EquipmentService.IsDualWielding(loadout), Is.False);
        }

        [Test]
        public void ShieldAndBookUseSecondarySlotWithoutCountingAsDualWield()
        {
            HeroEquipmentProfile cleric = HeroEquipmentProfiles.Create(HeroClass.Cleric);
            EquipmentLoadout shieldLoadout = new EquipmentLoadout();
            EquipmentLoadout bookLoadout = new EquipmentLoadout();

            Assert.That(EquipmentService.TryEquip(
                cleric,
                shieldLoadout,
                EquipmentSlot.MainWeapon,
                EquipmentDescriptor.Weapon("mace", WeaponType.Mace, Handedness.OneHanded)).Succeeded,
                Is.True);
            Assert.That(EquipmentService.TryEquip(
                cleric,
                shieldLoadout,
                EquipmentSlot.SecondaryWeapon,
                EquipmentDescriptor.OffHand("shield", OffHandType.Shield)).Succeeded,
                Is.True);
            Assert.That(EquipmentService.IsDualWielding(shieldLoadout), Is.False);

            Assert.That(EquipmentService.TryEquip(
                cleric,
                bookLoadout,
                EquipmentSlot.MainWeapon,
                EquipmentDescriptor.Weapon("mace", WeaponType.Mace, Handedness.OneHanded)).Succeeded,
                Is.True);
            Assert.That(EquipmentService.TryEquip(
                cleric,
                bookLoadout,
                EquipmentSlot.SecondaryWeapon,
                EquipmentDescriptor.OffHand("book", OffHandType.Book)).Succeeded,
                Is.True);
            Assert.That(EquipmentService.IsDualWielding(bookLoadout), Is.False);
        }

        [Test]
        public void TwoHandedWeaponClearsAndBlocksSecondarySlot()
        {
            HeroEquipmentProfile warrior = HeroEquipmentProfiles.Create(HeroClass.Warrior);
            EquipmentLoadout loadout = new EquipmentLoadout();
            EquipmentService.TryEquip(
                warrior,
                loadout,
                EquipmentSlot.MainWeapon,
                EquipmentDescriptor.Weapon("sword", WeaponType.Sword, Handedness.OneHanded));
            EquipmentService.TryEquip(
                warrior,
                loadout,
                EquipmentSlot.SecondaryWeapon,
                EquipmentDescriptor.OffHand("shield", OffHandType.Shield));

            EquipmentResult twoHanded = EquipmentService.TryEquip(
                warrior,
                loadout,
                EquipmentSlot.MainWeapon,
                EquipmentDescriptor.Weapon("greatsword", WeaponType.Sword, Handedness.TwoHanded));
            EquipmentResult secondary = EquipmentService.TryEquip(
                warrior,
                loadout,
                EquipmentSlot.SecondaryWeapon,
                EquipmentDescriptor.Weapon("axe", WeaponType.Axe, Handedness.OneHanded));

            Assert.That(twoHanded.Succeeded, Is.True);
            Assert.That(loadout.Get(EquipmentSlot.SecondaryWeapon), Is.Null);
            Assert.That(secondary.Succeeded, Is.False);
        }

        [Test]
        public void ArmorCompatibilityUsesClassProfileAndUniversalAccessories()
        {
            HeroEquipmentProfile mage = HeroEquipmentProfiles.Create(HeroClass.Mage);

            Assert.That(EquipmentService.CanEquip(
                mage,
                new EquipmentLoadout(),
                EquipmentSlot.Chest,
                EquipmentDescriptor.Armor("robe", EquipmentSlot.Chest, ArmorType.Cloth)),
                Is.True);
            Assert.That(EquipmentService.CanEquip(
                mage,
                new EquipmentLoadout(),
                EquipmentSlot.Chest,
                EquipmentDescriptor.Armor("plate", EquipmentSlot.Chest, ArmorType.Plate)),
                Is.False);
            Assert.That(EquipmentService.CanEquip(
                mage,
                new EquipmentLoadout(),
                EquipmentSlot.Earring1,
                EquipmentDescriptor.Accessory("earring", EquipmentSlot.Earring1)),
                Is.True);
            Assert.That(EquipmentService.CanEquip(
                mage,
                new EquipmentLoadout(),
                EquipmentSlot.Earring2,
                EquipmentDescriptor.Accessory("earring", EquipmentSlot.Earring1)),
                Is.True);
            Assert.That(EquipmentService.CanEquip(
                mage,
                new EquipmentLoadout(),
                EquipmentSlot.Ring2,
                EquipmentDescriptor.Accessory("ring", EquipmentSlot.Ring1)),
                Is.True);
        }

        [Test]
        public void BuffStacksAndRefreshesAccordingToDefinition()
        {
            StatusEffectDefinitionData effect = new StatusEffectDefinitionData
            {
                Id = "battle_focus",
                Kind = StatusEffectKind.Buff,
                DurationMilliseconds = 5000,
                MaxStacks = 3,
                StackPolicy = StatusStackPolicy.StackAndRefresh,
                Modifiers = new List<StatModifier>
                {
                    new StatModifier(HeroStatType.AttackPower, StatModifierOperation.Flat, 2f, "battle_focus")
                }
            };
            StatusEffectCollection effects = new StatusEffectCollection();

            effects.Apply(effect, "warrior");
            effects.Advance(2000);
            effects.Apply(effect, "warrior");

            Assert.That(effects.Active, Has.Count.EqualTo(1));
            Assert.That(effects.Active[0].Stacks, Is.EqualTo(2));
            Assert.That(effects.Active[0].RemainingMilliseconds, Is.EqualTo(5000));
            Assert.That(effects.CollectModifiers()[0].Value, Is.EqualTo(4f));
        }

        [Test]
        public void PeriodicEffectsAndExpirationUseContinuousTime()
        {
            StatusEffectDefinitionData poison = new StatusEffectDefinitionData
            {
                Id = "poison",
                Kind = StatusEffectKind.Debuff,
                DurationMilliseconds = 3000,
                MaxStacks = 1,
                StackPolicy = StatusStackPolicy.RefreshDuration,
                PeriodicEffect = StatusPeriodicEffect.Damage,
                PeriodicAmount = 3f,
                PeriodMilliseconds = 1000
            };
            StatusEffectCollection effects = new StatusEffectCollection();
            effects.Apply(poison, "rogue");

            StatusAdvanceResult first = effects.Advance(2500);
            StatusAdvanceResult second = effects.Advance(500);

            Assert.That(first.Damage, Is.EqualTo(6f));
            Assert.That(second.Damage, Is.EqualTo(3f));
            Assert.That(effects.Active, Is.Empty);
        }

        [Test]
        public void FasterCombatantAttacksMoreOftenWithoutGlobalTurns()
        {
            CombatRequest request = new CombatRequest
            {
                Seed = 7,
                MaxDurationMilliseconds = 5000,
                Heroes = new List<CombatantState>
                {
                    TestFixtures.Combatant("fast", CombatSide.Hero, 0, 0, 1000, 1, 10)
                },
                Enemies = new List<CombatantState>
                {
                    TestFixtures.Combatant("slow", CombatSide.Enemy, 0, 0, 1000, 1, 10)
                }
            };
            request.Heroes[0].AttackSpeed = 2f;
            request.Enemies[0].AttackSpeed = 0.5f;

            CombatResult result = new CombatSimulator().Simulate(request);

            int fastAttacks = result.Events.FindAll(item => item.ActorId == "fast").Count;
            int slowAttacks = result.Events.FindAll(item => item.ActorId == "slow").Count;
            Assert.That(fastAttacks, Is.GreaterThan(slowAttacks));
            Assert.That(result.Events.TrueForAll(item => item.TimeMilliseconds >= 0), Is.True);
        }

        [Test]
        public void DualWieldAttacksAlternateHands()
        {
            CombatRequest request = new CombatRequest
            {
                Seed = 11,
                MaxDurationMilliseconds = 2200,
                Heroes = new List<CombatantState>
                {
                    TestFixtures.Combatant("rogue", CombatSide.Hero, 0, 0, 1000, 1, 10)
                },
                Enemies = new List<CombatantState>
                {
                    TestFixtures.Combatant("target", CombatSide.Enemy, 0, 0, 1000, 1, 10)
                }
            };
            request.Heroes[0].AttackSpeed = 2f;
            request.Heroes[0].IsDualWielding = true;

            CombatResult result = new CombatSimulator().Simulate(request);
            List<CombatEvent> attacks = result.Events.FindAll(item => item.ActorId == "rogue");

            Assert.That(attacks, Has.Count.GreaterThanOrEqualTo(3));
            Assert.That(attacks[0].AttackHand, Is.EqualTo(AttackHand.Main));
            Assert.That(attacks[1].AttackHand, Is.EqualTo(AttackHand.Secondary));
            Assert.That(attacks[2].AttackHand, Is.EqualTo(AttackHand.Main));
        }
    }
}
