using System;
using System.Collections.Generic;
using TaskbarTactics.Core.Combat;

namespace TaskbarTactics.Core.Models
{
    [Serializable]
    public sealed class GameState
    {
        public const int CurrentVersion = 4;

        public int Version = CurrentVersion;
        public int Gold;
        public int Silver;
        public long LastSavedUtcTicks;
        public PartyState Party = new PartyState();
        public List<InventoryItem> Inventory = new List<InventoryItem>();
        public ExpeditionState Expedition = new ExpeditionState();
        public bool ActTwoUnlocked;
        public AppearanceState Appearance = new AppearanceState();
        public string LanguageCode = "es";

        public static GameState CreateDefault()
        {
            return new GameState
            {
                LastSavedUtcTicks = DateTime.UtcNow.Ticks,
                Party = new PartyState(),
                Inventory = new List<InventoryItem>(),
                Expedition = new ExpeditionState(),
                Appearance = new AppearanceState()
            };
        }
    }

    [Serializable]
    public sealed class PartyState
    {
        public List<HeroState> Heroes = new List<HeroState>();
        public bool IsFormationLocked;
        public RoutePreference RoutePreference = RoutePreference.Safety;

        public HeroState GetHero(string definitionId)
        {
            return Heroes.Find(hero => hero.DefinitionId == definitionId);
        }

        public bool TrySetFormation(string definitionId, FormationPosition position)
        {
            if (IsFormationLocked || Heroes.Exists(hero =>
                    hero.DefinitionId != definitionId && hero.Position.Equals(position)))
            {
                return false;
            }

            HeroState hero = GetHero(definitionId);
            if (hero == null)
            {
                return false;
            }

            hero.Position = position;
            return true;
        }
    }

    [Serializable]
    public sealed class HeroState
    {
        public string DefinitionId = string.Empty;
        public bool IsSelected;
        public int Level = 1;
        public long Experience;
        public int CurrentHealth;
        public int CurrentMana;
        public bool ResourcesInitialized;
        public FormationPosition Position;
        public string ActiveSkillId = string.Empty;
        public string PassiveSkillId = string.Empty;
        public List<string> UnlockedSkillIds = new List<string>();
        public List<EquippedItemState> EquippedItems = new List<EquippedItemState>();
        public List<ActiveStatusEffectState> ActiveStatusEffects =
            new List<ActiveStatusEffectState>();

        public string GetEquippedItemId(EquipmentSlot slot)
        {
            return EquippedItems?.Find(item => item.Slot == slot)?.ItemInstanceId;
        }

        public void SetEquippedItem(EquipmentSlot slot, string instanceId)
        {
            EquippedItems ??= new List<EquippedItemState>();
            EquippedItems.RemoveAll(item =>
                item.Slot == slot ||
                (!string.IsNullOrWhiteSpace(instanceId) &&
                 item.ItemInstanceId == instanceId));
            if (!string.IsNullOrWhiteSpace(instanceId))
            {
                EquippedItems.Add(new EquippedItemState
                {
                    Slot = slot,
                    ItemInstanceId = instanceId
                });
            }
        }
    }

    [Serializable]
    public sealed class EquippedItemState
    {
        public EquipmentSlot Slot;
        public string ItemInstanceId = string.Empty;
    }

    [Serializable]
    public sealed class InventoryItem : IEquatable<InventoryItem>
    {
        public string InstanceId = string.Empty;
        public string DefinitionId = string.Empty;
        public EquipmentSlot Slot;
        public ItemRarity Rarity;
        public List<string> ItemBonusIds = new List<string>();

        public bool Equals(InventoryItem other)
        {
            if (other == null ||
                InstanceId != other.InstanceId ||
                DefinitionId != other.DefinitionId ||
                Slot != other.Slot ||
                Rarity != other.Rarity ||
                ItemBonusIds.Count != other.ItemBonusIds.Count)
            {
                return false;
            }

            for (int i = 0; i < ItemBonusIds.Count; i++)
            {
                if (ItemBonusIds[i] != other.ItemBonusIds[i])
                {
                    return false;
                }
            }

            return true;
        }

        public override bool Equals(object obj)
        {
            return Equals(obj as InventoryItem);
        }

        public override int GetHashCode()
        {
            return InstanceId != null ? InstanceId.GetHashCode() : 0;
        }
    }

    [Serializable]
    public sealed class ExpeditionState
    {
        public bool IsActive;
        public string CurrentNodeId = string.Empty;
        public int Seed;
        public int CompletedNodes;
        public List<string> CompletedNodeIds = new List<string>();
        public List<string> CollectedItemIds = new List<string>();
    }

    [Serializable]
    public sealed class AppearanceState
    {
        public List<string> EquippedCosmeticIds = new List<string>();
    }

    [Serializable]
    public sealed class MapNodeState
    {
        public string Id = string.Empty;
        public MapNodeType Type;
        public int Difficulty;

        public MapNodeState()
        {
        }

        public MapNodeState(string id, MapNodeType type, int difficulty)
        {
            Id = id;
            Type = type;
            Difficulty = difficulty;
        }
    }
}
