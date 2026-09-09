namespace TaskbarTactics.Core.Models
{
    public enum HeroClass
    {
        Warrior,
        Cleric,
        Mage,
        Archer,
        Rogue,
        MagicWarrior
    }

    public enum RoutePreference
    {
        Safety,
        Loot,
        Challenge
    }

    public enum MapNodeType
    {
        Combat,
        Treasure,
        Event,
        Elite,
        Boss
    }

    public enum EquipmentSlot
    {
        Head,
        Shoulders,
        Neck,
        Chest,
        Bracers,
        Hands,
        Legs,
        Boots,
        Belt,
        Cloak,
        MainWeapon,
        SecondaryWeapon,
        Ring1,
        Ring2,
        Earring1,
        Earring2
    }

    public enum EquipmentCategory
    {
        Armor,
        Weapon,
        OffHand,
        Accessory
    }

    public enum WeaponType
    {
        None,
        Sword,
        Mace,
        Axe,
        Staff,
        Wand,
        Bow,
        Crossbow,
        Dagger
    }

    public enum Handedness
    {
        None,
        OneHanded,
        TwoHanded
    }

    public enum OffHandType
    {
        None,
        Shield,
        Book
    }

    public enum ArmorType
    {
        None,
        Cloth,
        Leather,
        Mail,
        Plate
    }

    public enum ItemRarity
    {
        Common,
        Rare,
        Epic
    }

    public enum CombatSide
    {
        Hero,
        Enemy
    }

    public enum CombatOutcome
    {
        Victory,
        Defeat,
        Timeout
    }

    public enum AttackHand
    {
        Main,
        Secondary
    }

    public enum DamageType
    {
        Physical,
        Magical,
        True
    }

    public enum HeroStatType
    {
        MaxHealth,
        MaxMana,
        AttackPower,
        SpellPower,
        Defense,
        MagicResistance,
        AttackSpeed,
        CastSpeed,
        AttackRange,
        CriticalChance,
        CriticalDamage,
        Accuracy,
        Evasion,
        HealthRegeneration,
        ManaRegeneration,
        CooldownReduction
    }

    public enum StatModifierOperation
    {
        Flat,
        AdditivePercent,
        MultiplicativePercent
    }

    public enum StatusEffectKind
    {
        Buff,
        Debuff
    }

    public enum StatusStackPolicy
    {
        RefreshDuration,
        StackAndRefresh,
        IndependentInstances
    }

    public enum StatusPeriodicEffect
    {
        None,
        Damage,
        Healing,
        ManaGain,
        ManaLoss
    }
}
