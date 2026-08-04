namespace TaskbarTactics.Core.Models
{
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
        Weapon,
        Armor,
        Accessory,
        Charm
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
}
