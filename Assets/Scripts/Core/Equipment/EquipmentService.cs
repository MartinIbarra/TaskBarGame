using System;
using System.Collections.Generic;
using System.Linq;
using TaskbarTactics.Core.Models;

namespace TaskbarTactics.Core.Equipment
{
    public static class EquipmentService
    {
        private static readonly HashSet<EquipmentSlot> ArmorSlots = new HashSet<EquipmentSlot>
        {
            EquipmentSlot.Head,
            EquipmentSlot.Shoulders,
            EquipmentSlot.Chest,
            EquipmentSlot.Bracers,
            EquipmentSlot.Hands,
            EquipmentSlot.Legs,
            EquipmentSlot.Boots
        };

        private static readonly HashSet<EquipmentSlot> AccessorySlots = new HashSet<EquipmentSlot>
        {
            EquipmentSlot.Neck,
            EquipmentSlot.Belt,
            EquipmentSlot.Cloak,
            EquipmentSlot.Ring1,
            EquipmentSlot.Ring2,
            EquipmentSlot.Earring1,
            EquipmentSlot.Earring2
        };

        public static bool CanEquip(
            HeroEquipmentProfile profile,
            EquipmentLoadout loadout,
            EquipmentSlot slot,
            EquipmentDescriptor item)
        {
            return Validate(profile, loadout, slot, item).Succeeded;
        }

        public static EquipmentResult TryEquip(
            HeroEquipmentProfile profile,
            EquipmentLoadout loadout,
            EquipmentSlot slot,
            EquipmentDescriptor item)
        {
            EquipmentResult validation = Validate(profile, loadout, slot, item);
            if (!validation.Succeeded)
            {
                return validation;
            }

            if (item.Handedness == Handedness.TwoHanded)
            {
                loadout.Set(EquipmentSlot.SecondaryWeapon, null);
            }

            loadout.Set(slot, item);
            return EquipmentResult.Success();
        }

        public static bool IsDualWielding(EquipmentLoadout loadout)
        {
            EquipmentDescriptor main = loadout?.Get(EquipmentSlot.MainWeapon);
            EquipmentDescriptor secondary = loadout?.Get(EquipmentSlot.SecondaryWeapon);
            return main != null &&
                   secondary != null &&
                   main.Category == EquipmentCategory.Weapon &&
                   secondary.Category == EquipmentCategory.Weapon &&
                   main.Handedness == Handedness.OneHanded &&
                   secondary.Handedness == Handedness.OneHanded;
        }

        private static EquipmentResult Validate(
            HeroEquipmentProfile profile,
            EquipmentLoadout loadout,
            EquipmentSlot slot,
            EquipmentDescriptor item)
        {
            if (profile == null || loadout == null || item == null)
            {
                return EquipmentResult.Failure("Profile, loadout and item are required.");
            }

            if (loadout.Equipped.Any(pair =>
                    pair.Key != slot &&
                    !string.IsNullOrEmpty(item.InstanceId) &&
                    pair.Value.InstanceId == item.InstanceId))
            {
                return EquipmentResult.Failure("The same item instance is already equipped.");
            }

            switch (slot)
            {
                case EquipmentSlot.MainWeapon:
                    return ValidateMainWeapon(profile, item);
                case EquipmentSlot.SecondaryWeapon:
                    return ValidateSecondary(profile, loadout, item);
                default:
                    return ValidateWearable(profile, slot, item);
            }
        }

        private static EquipmentResult ValidateMainWeapon(
            HeroEquipmentProfile profile,
            EquipmentDescriptor item)
        {
            if (item.Category != EquipmentCategory.Weapon ||
                !profile.AllowsWeapon(item.WeaponType, item.Handedness))
            {
                return EquipmentResult.Failure("The class cannot use this main weapon.");
            }

            return EquipmentResult.Success();
        }

        private static EquipmentResult ValidateSecondary(
            HeroEquipmentProfile profile,
            EquipmentLoadout loadout,
            EquipmentDescriptor item)
        {
            EquipmentDescriptor main = loadout.Get(EquipmentSlot.MainWeapon);
            if (main != null && main.Handedness == Handedness.TwoHanded)
            {
                return EquipmentResult.Failure("A two-handed weapon blocks the secondary slot.");
            }

            if (item.Category == EquipmentCategory.Weapon)
            {
                bool hasOneHandedMainWeapon = main != null &&
                                              main.Category == EquipmentCategory.Weapon &&
                                              main.Handedness == Handedness.OneHanded;
                bool allowed = hasOneHandedMainWeapon &&
                               profile.CanDualWieldWeapons &&
                               item.Handedness == Handedness.OneHanded &&
                               profile.AllowsWeapon(item.WeaponType, Handedness.OneHanded);
                return allowed
                    ? EquipmentResult.Success()
                    : EquipmentResult.Failure("The class cannot dual wield this weapon.");
            }

            if (item.Category == EquipmentCategory.OffHand &&
                profile.AllowsOffHand(item.OffHandType))
            {
                return EquipmentResult.Success();
            }

            return EquipmentResult.Failure("The class cannot use this secondary item.");
        }

        private static EquipmentResult ValidateWearable(
            HeroEquipmentProfile profile,
            EquipmentSlot slot,
            EquipmentDescriptor item)
        {
            if (ArmorSlots.Contains(slot))
            {
                bool allowed = item.Category == EquipmentCategory.Armor &&
                               item.PrimarySlot == slot &&
                               profile.AllowsArmor(item.ArmorType);
                return allowed
                    ? EquipmentResult.Success()
                    : EquipmentResult.Failure("The armor type or slot is incompatible.");
            }

            if (AccessorySlots.Contains(slot))
            {
                bool allowed = item.Category == EquipmentCategory.Accessory &&
                               AccessorySlotsAreCompatible(item.PrimarySlot, slot);
                return allowed
                    ? EquipmentResult.Success()
                    : EquipmentResult.Failure("The accessory slot is incompatible.");
            }

            return EquipmentResult.Failure("Unsupported equipment slot.");
        }

        private static bool AccessorySlotsAreCompatible(
            EquipmentSlot itemSlot,
            EquipmentSlot targetSlot)
        {
            bool bothRings = (itemSlot == EquipmentSlot.Ring1 ||
                              itemSlot == EquipmentSlot.Ring2) &&
                             (targetSlot == EquipmentSlot.Ring1 ||
                              targetSlot == EquipmentSlot.Ring2);
            bool bothEarrings = (itemSlot == EquipmentSlot.Earring1 ||
                                 itemSlot == EquipmentSlot.Earring2) &&
                                (targetSlot == EquipmentSlot.Earring1 ||
                                 targetSlot == EquipmentSlot.Earring2);
            return itemSlot == targetSlot || bothRings || bothEarrings;
        }
    }
}
