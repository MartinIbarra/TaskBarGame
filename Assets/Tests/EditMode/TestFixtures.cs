using System.Collections.Generic;
using TaskbarTactics.Core.Combat;
using TaskbarTactics.Core.Loot;
using TaskbarTactics.Core.Models;

namespace TaskbarTactics.Tests
{
    internal static class TestFixtures
    {
        public static CombatRequest CreateCombatRequest(int seed)
        {
            return new CombatRequest
            {
                Seed = seed,
                MaxDurationMilliseconds = 60000,
                Heroes = new List<CombatantState>
                {
                    Combatant("warrior", CombatSide.Hero, 1, 0, 180, 18, 4),
                    Combatant("archer", CombatSide.Hero, 0, 2, 90, 24, 4),
                    Combatant("cleric", CombatSide.Hero, 2, 2, 105, 14, 3)
                },
                Enemies = new List<CombatantState>
                {
                    Combatant("goblin-a", CombatSide.Enemy, 1, 0, 80, 13, 2),
                    Combatant("goblin-b", CombatSide.Enemy, 0, 1, 70, 12, 3),
                    Combatant("shaman", CombatSide.Enemy, 2, 2, 65, 17, 4)
                }
            };
        }

        public static CombatantState Combatant(
            string id,
            CombatSide side,
            int row,
            int column,
            int health = 100,
            int power = 10,
            int range = 3)
        {
            return new CombatantState
            {
                Id = id,
                Side = side,
                Position = new FormationPosition(row, column),
                MaxHealth = health,
                CurrentHealth = health,
                MaxMana = 100,
                CurrentMana = 100,
                AttackPower = power,
                Defense = 20f,
                Range = range,
                AttackSpeed = 1f,
                CastSpeed = 1f,
                CriticalChance = 10f,
                CriticalDamage = 2f,
                Accuracy = 100f
            };
        }

        public static PartyState CreateParty()
        {
            return new PartyState
            {
                Heroes = new List<HeroState>
                {
                    new HeroState
                    {
                        DefinitionId = "warrior",
                        Position = new FormationPosition(1, 0),
                        EquippedItems = new List<EquippedItemState>()
                    }
                }
            };
        }

        public static LootTable CreateLootTable()
        {
            return new LootTable
            {
                Entries = new List<LootEntry>
                {
                    new LootEntry("iron-sword", EquipmentSlot.MainWeapon, 50),
                    new LootEntry("oak-shield", EquipmentSlot.SecondaryWeapon, 35),
                    new LootEntry("sun-medallion", EquipmentSlot.Neck, 15)
                },
                ItemBonusIds = new List<string> { "attack_power", "guard", "critical" }
            };
        }

        public static GameState CreateGameState()
        {
            GameState state = GameState.CreateDefault();
            state.Gold = 100;
            state.Party.Heroes.Add(new HeroState
            {
                DefinitionId = "warrior",
                Level = 3,
                CurrentHealth = 180,
                CurrentMana = 60,
                ResourcesInitialized = true,
                Position = new FormationPosition(1, 0),
                EquippedItems = new List<EquippedItemState>
                {
                    new EquippedItemState
                    {
                        Slot = EquipmentSlot.MainWeapon,
                        ItemInstanceId = "item-001"
                    }
                }
            });
            state.Inventory.Add(new InventoryItem
            {
                InstanceId = "item-001",
                DefinitionId = "iron-sword",
                Slot = EquipmentSlot.MainWeapon,
                Rarity = ItemRarity.Rare,
                ItemBonusIds = new List<string> { "attack_power" }
            });
            state.Expedition.IsActive = true;
            state.Expedition.CurrentNodeId = "node-03";
            return state;
        }
    }
}
