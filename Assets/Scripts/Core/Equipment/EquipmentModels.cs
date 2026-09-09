using System;
using System.Collections.Generic;
using System.Linq;
using TaskbarTactics.Core.Models;
using TaskbarTactics.Core.Stats;

namespace TaskbarTactics.Core.Equipment
{
    [Serializable]
    public sealed class WeaponPermission
    {
        public WeaponType WeaponType;
        public Handedness Handedness;

        public WeaponPermission()
        {
        }

        public WeaponPermission(WeaponType weaponType, Handedness handedness)
        {
            WeaponType = weaponType;
            Handedness = handedness;
        }
    }

    [Serializable]
    public sealed class HeroEquipmentProfile
    {
        public HeroClass HeroClass;
        public bool CanDualWieldWeapons;
        public List<WeaponPermission> AllowedWeapons = new List<WeaponPermission>();
        public List<OffHandType> AllowedOffHands = new List<OffHandType>();
        public List<ArmorType> AllowedArmorTypes = new List<ArmorType>();

        public bool AllowsWeapon(WeaponType type, Handedness handedness)
        {
            return AllowedWeapons.Any(item =>
                item.WeaponType == type && item.Handedness == handedness);
        }

        public bool AllowsOffHand(OffHandType type)
        {
            return AllowedOffHands.Contains(type);
        }

        public bool AllowsArmor(ArmorType type)
        {
            return AllowedArmorTypes.Contains(type);
        }
    }

    public static class HeroEquipmentProfiles
    {
        public static HeroEquipmentProfile Create(HeroClass heroClass)
        {
            switch (heroClass)
            {
                case HeroClass.Warrior:
                    return Profile(
                        heroClass,
                        true,
                        Weapons(
                            WeaponType.Sword, Handedness.OneHanded, Handedness.TwoHanded,
                            WeaponType.Mace, Handedness.OneHanded, Handedness.TwoHanded,
                            WeaponType.Axe, Handedness.OneHanded, Handedness.TwoHanded),
                        new[] { OffHandType.Shield },
                        new[] { ArmorType.Plate });
                case HeroClass.Cleric:
                    return Profile(
                        heroClass,
                        false,
                        Weapons(WeaponType.Mace, Handedness.OneHanded),
                        new[] { OffHandType.Shield, OffHandType.Book },
                        new[] { ArmorType.Mail, ArmorType.Plate });
                case HeroClass.Mage:
                    return Profile(
                        heroClass,
                        false,
                        Weapons(
                            WeaponType.Staff, Handedness.TwoHanded,
                            WeaponType.Wand, Handedness.OneHanded),
                        new[] { OffHandType.Book },
                        new[] { ArmorType.Cloth });
                case HeroClass.Archer:
                    return Profile(
                        heroClass,
                        false,
                        Weapons(
                            WeaponType.Bow, Handedness.TwoHanded,
                            WeaponType.Crossbow, Handedness.TwoHanded),
                        Array.Empty<OffHandType>(),
                        new[] { ArmorType.Leather });
                case HeroClass.Rogue:
                    return Profile(
                        heroClass,
                        true,
                        Weapons(WeaponType.Dagger, Handedness.OneHanded),
                        Array.Empty<OffHandType>(),
                        new[] { ArmorType.Leather });
                case HeroClass.MagicWarrior:
                    return Profile(
                        heroClass,
                        true,
                        Weapons(
                            WeaponType.Sword, Handedness.OneHanded, Handedness.TwoHanded),
                        Array.Empty<OffHandType>(),
                        new[] { ArmorType.Mail });
                default:
                    throw new ArgumentOutOfRangeException(nameof(heroClass), heroClass, null);
            }
        }

        private static HeroEquipmentProfile Profile(
            HeroClass heroClass,
            bool canDualWield,
            IEnumerable<WeaponPermission> weapons,
            IEnumerable<OffHandType> offHands,
            IEnumerable<ArmorType> armorTypes)
        {
            return new HeroEquipmentProfile
            {
                HeroClass = heroClass,
                CanDualWieldWeapons = canDualWield,
                AllowedWeapons = weapons.ToList(),
                AllowedOffHands = offHands.ToList(),
                AllowedArmorTypes = armorTypes.ToList()
            };
        }

        private static IEnumerable<WeaponPermission> Weapons(params object[] values)
        {
            List<WeaponPermission> permissions = new List<WeaponPermission>();
            WeaponType activeType = WeaponType.None;
            foreach (object value in values)
            {
                if (value is WeaponType weaponType)
                {
                    activeType = weaponType;
                }
                else if (value is Handedness handedness && activeType != WeaponType.None)
                {
                    permissions.Add(new WeaponPermission(activeType, handedness));
                }
            }

            return permissions;
        }
    }

    [Serializable]
    public sealed class EquipmentDescriptor
    {
        public string InstanceId = string.Empty;
        public string DefinitionId = string.Empty;
        public EquipmentSlot PrimarySlot;
        public EquipmentCategory Category;
        public WeaponType WeaponType;
        public Handedness Handedness;
        public OffHandType OffHandType;
        public ArmorType ArmorType;
        public List<StatModifier> Modifiers = new List<StatModifier>();

        public static EquipmentDescriptor Weapon(
            string id,
            WeaponType weaponType,
            Handedness handedness,
            IEnumerable<StatModifier> modifiers = null)
        {
            return Create(id, EquipmentSlot.MainWeapon, EquipmentCategory.Weapon,
                weaponType, handedness, OffHandType.None, ArmorType.None, modifiers);
        }

        public static EquipmentDescriptor OffHand(
            string id,
            OffHandType offHandType,
            IEnumerable<StatModifier> modifiers = null)
        {
            return Create(id, EquipmentSlot.SecondaryWeapon, EquipmentCategory.OffHand,
                WeaponType.None, Handedness.OneHanded, offHandType, ArmorType.None, modifiers);
        }

        public static EquipmentDescriptor Armor(
            string id,
            EquipmentSlot slot,
            ArmorType armorType,
            IEnumerable<StatModifier> modifiers = null)
        {
            return Create(id, slot, EquipmentCategory.Armor,
                WeaponType.None, Handedness.None, OffHandType.None, armorType, modifiers);
        }

        public static EquipmentDescriptor Accessory(
            string id,
            EquipmentSlot slot,
            IEnumerable<StatModifier> modifiers = null)
        {
            return Create(id, slot, EquipmentCategory.Accessory,
                WeaponType.None, Handedness.None, OffHandType.None, ArmorType.None, modifiers);
        }

        public EquipmentDescriptor CloneForInstance(string instanceId)
        {
            EquipmentDescriptor clone = Create(
                string.IsNullOrWhiteSpace(DefinitionId) ? InstanceId : DefinitionId,
                PrimarySlot,
                Category,
                WeaponType,
                Handedness,
                OffHandType,
                ArmorType,
                Modifiers);
            clone.InstanceId = instanceId ?? string.Empty;
            return clone;
        }

        private static EquipmentDescriptor Create(
            string id,
            EquipmentSlot slot,
            EquipmentCategory category,
            WeaponType weaponType,
            Handedness handedness,
            OffHandType offHandType,
            ArmorType armorType,
            IEnumerable<StatModifier> modifiers)
        {
            return new EquipmentDescriptor
            {
                InstanceId = id ?? string.Empty,
                DefinitionId = id ?? string.Empty,
                PrimarySlot = slot,
                Category = category,
                WeaponType = weaponType,
                Handedness = handedness,
                OffHandType = offHandType,
                ArmorType = armorType,
                Modifiers = modifiers?.Select(item => new StatModifier(
                    item.Stat, item.Operation, item.Value, item.SourceId)).ToList() ??
                    new List<StatModifier>()
            };
        }
    }

    public sealed class EquipmentLoadout
    {
        private readonly Dictionary<EquipmentSlot, EquipmentDescriptor> equipped =
            new Dictionary<EquipmentSlot, EquipmentDescriptor>();

        public IReadOnlyDictionary<EquipmentSlot, EquipmentDescriptor> Equipped => equipped;

        public EquipmentDescriptor Get(EquipmentSlot slot)
        {
            equipped.TryGetValue(slot, out EquipmentDescriptor item);
            return item;
        }

        internal void Set(EquipmentSlot slot, EquipmentDescriptor item)
        {
            if (item == null)
            {
                equipped.Remove(slot);
            }
            else
            {
                equipped[slot] = item;
            }
        }

        public IReadOnlyList<StatModifier> CollectModifiers()
        {
            return equipped.Values
                .SelectMany(item => item.Modifiers)
                .ToList();
        }
    }

    public sealed class EquipmentResult
    {
        public bool Succeeded;
        public string Reason = string.Empty;

        public static EquipmentResult Success()
        {
            return new EquipmentResult { Succeeded = true };
        }

        public static EquipmentResult Failure(string reason)
        {
            return new EquipmentResult { Succeeded = false, Reason = reason ?? string.Empty };
        }
    }
}
