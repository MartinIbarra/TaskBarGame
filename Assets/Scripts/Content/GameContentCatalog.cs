using System;
using System.Collections.Generic;
using System.Linq;
using TaskbarTactics.Core.Combat;
using TaskbarTactics.Core.Equipment;
using TaskbarTactics.Core.Loot;
using TaskbarTactics.Core.Models;
using TaskbarTactics.Core.Stats;
using UnityEngine;

namespace TaskbarTactics.Content
{
    [CreateAssetMenu(menuName = "Taskbar Tactics/Content Catalog")]
    public sealed class GameContentCatalog : ScriptableObject
    {
        private static readonly string[] StarterWeaponLootIds =
        {
            "wooden_sword",
            "wooden_mace",
            "wooden_staff",
            "wooden_bow",
            "wooden_dagger"
        };

        [SerializeField] private List<HeroDefinition> heroes = new List<HeroDefinition>();
        [SerializeField] private List<SkillDefinition> skills = new List<SkillDefinition>();
        [SerializeField] private List<ItemDefinition> items = new List<ItemDefinition>();
        [SerializeField] private List<ItemBonusDefinition> itemBonuses =
            new List<ItemBonusDefinition>();
        [SerializeField] private List<StatusEffectDefinition> statusEffects =
            new List<StatusEffectDefinition>();
        [SerializeField] private List<SynergyDefinition> synergies = new List<SynergyDefinition>();
        [SerializeField] private List<EnemyDefinition> enemies = new List<EnemyDefinition>();
        [SerializeField] private List<EncounterDefinition> encounters =
            new List<EncounterDefinition>();
        [SerializeField] private MapDefinition map;
        [SerializeField] private List<CosmeticDefinition> cosmetics =
            new List<CosmeticDefinition>();

        public IReadOnlyList<HeroDefinition> Heroes => heroes;
        public IReadOnlyList<SkillDefinition> Skills => skills;
        public IReadOnlyList<ItemDefinition> Items => items;
        public IReadOnlyList<ItemBonusDefinition> ItemBonuses => itemBonuses;
        public IReadOnlyList<StatusEffectDefinition> StatusEffects => statusEffects;
        public IReadOnlyList<SynergyDefinition> Synergies => synergies;
        public IReadOnlyList<EnemyDefinition> Enemies => enemies;
        public IReadOnlyList<EncounterDefinition> Encounters => encounters;
        public MapDefinition Map => map;
        public IReadOnlyList<CosmeticDefinition> Cosmetics => cosmetics;

        public void Configure(
            IEnumerable<HeroDefinition> heroDefinitions,
            IEnumerable<SkillDefinition> skillDefinitions,
            IEnumerable<ItemDefinition> itemDefinitions,
            IEnumerable<ItemBonusDefinition> itemBonusDefinitions,
            IEnumerable<StatusEffectDefinition> statusEffectDefinitions,
            IEnumerable<SynergyDefinition> synergyDefinitions,
            IEnumerable<EnemyDefinition> enemyDefinitions,
            IEnumerable<EncounterDefinition> encounterDefinitions,
            MapDefinition mapDefinition,
            IEnumerable<CosmeticDefinition> cosmeticDefinitions)
        {
            heroes = heroDefinitions.ToList();
            skills = skillDefinitions.ToList();
            items = itemDefinitions.ToList();
            itemBonuses = itemBonusDefinitions.ToList();
            statusEffects = statusEffectDefinitions.ToList();
            synergies = synergyDefinitions.ToList();
            enemies = enemyDefinitions.ToList();
            encounters = encounterDefinitions.ToList();
            map = mapDefinition;
            cosmetics = cosmeticDefinitions.ToList();
        }

        public HeroDefinition FindHero(string id)
        {
            return heroes.Find(item => item.Id == id);
        }

        public ItemDefinition FindItem(string id)
        {
            return items.Find(item => item.Id == id);
        }

        public ItemBonusDefinition FindItemBonus(string id)
        {
            return itemBonuses.Find(item => item.Id == id);
        }

        public StatusEffectDefinition FindStatusEffect(string id)
        {
            return statusEffects.Find(item => item.Id == id);
        }

        public EnemyDefinition FindEnemy(string id)
        {
            return enemies.Find(item => item.Id == id);
        }

        public EncounterDefinition FindEncounter(string id)
        {
            return encounters.Find(item => item.Id == id);
        }

        public LootTable CreateLootTable()
        {
            return new LootTable
            {
                Entries = items.Select(item => new LootEntry(item.Id, item.Slot, 10)).ToList(),
                ItemBonusIds = itemBonuses.Select(item => item.Id).ToList()
            };
        }

        public LootTable CreateStarterWeaponLootTable()
        {
            List<LootEntry> entries = items
                .Where(item => StarterWeaponLootIds.Contains(item.Id))
                .Select(item => new LootEntry(item.Id, item.Slot, 10))
                .ToList();

            if (entries.Count == 0)
            {
                return CreateLootTable();
            }

            return new LootTable
            {
                Entries = entries,
                ItemBonusIds = itemBonuses.Select(item => item.Id).ToList()
            };
        }

        public HeroStats ResolveHeroStats(
            HeroState state,
            IReadOnlyList<InventoryItem> inventory)
        {
            if (state == null)
            {
                throw new ArgumentNullException(nameof(state));
            }

            HeroDefinition definition = FindHero(state.DefinitionId);
            if (definition == null)
            {
                throw new InvalidOperationException($"Unknown hero definition '{state.DefinitionId}'.");
            }

            EquipmentLoadout loadout = CreateLoadout(state, inventory);
            return HeroStatsCalculator.Calculate(
                definition.BaseStats,
                definition.GrowthPerLevel,
                state.Level,
                loadout.CollectModifiers());
        }

        public EquipmentLoadout CreateLoadout(
            HeroState state,
            IReadOnlyList<InventoryItem> inventory)
        {
            EquipmentLoadout loadout = new EquipmentLoadout();
            HeroDefinition hero = state == null ? null : FindHero(state.DefinitionId);
            if (hero == null || state.EquippedItems == null || inventory == null)
            {
                return loadout;
            }

            IEnumerable<EquippedItemState> ordered = state.EquippedItems
                .OrderBy(item => EquipmentOrder(item.Slot));
            foreach (EquippedItemState equipped in ordered)
            {
                InventoryItem instance = inventory.FirstOrDefault(item =>
                    item.InstanceId == equipped.ItemInstanceId);
                EquipmentDescriptor descriptor = CreateItemDescriptor(instance);
                if (descriptor != null)
                {
                    EquipmentService.TryEquip(
                        hero.EquipmentProfile,
                        loadout,
                        equipped.Slot,
                        descriptor);
                }
            }

            return loadout;
        }

        public EquipmentDescriptor CreateItemDescriptor(InventoryItem instance)
        {
            if (instance == null)
            {
                return null;
            }

            ItemDefinition definition = FindItem(instance.DefinitionId);
            if (definition == null)
            {
                return null;
            }

            IEnumerable<ItemBonusDefinition> bonuses = (instance.ItemBonusIds ??
                new List<string>())
                .Select(FindItemBonus)
                .Where(item => item != null);
            return definition.CreateDescriptor(instance.InstanceId, bonuses);
        }

        public CombatRequest CreateCombatRequest(
            IReadOnlyList<HeroState> selectedHeroes,
            IReadOnlyList<InventoryItem> inventory,
            MapNodeDefinition node,
            int seed)
        {
            List<CombatantState> heroUnits = selectedHeroes.Select(state =>
            {
                HeroDefinition definition = FindHero(state.DefinitionId);
                HeroStats stats = ResolveHeroStats(state, inventory);
                EquipmentLoadout loadout = CreateLoadout(state, inventory);
                StatusEffectCollection effects = RestoreStatusEffects(state.ActiveStatusEffects);
                bool resourcesInitialized = state.ResourcesInitialized;
                return new CombatantState
                {
                    Id = state.DefinitionId,
                    Side = CombatSide.Hero,
                    Position = state.Position,
                    MaxHealth = stats.MaxHealth,
                    CurrentHealth = resourcesInitialized
                        ? Mathf.Clamp(state.CurrentHealth, 0, stats.MaxHealth)
                        : stats.MaxHealth,
                    MaxMana = stats.MaxMana,
                    CurrentMana = resourcesInitialized
                        ? Mathf.Clamp(state.CurrentMana, 0, stats.MaxMana)
                        : stats.MaxMana,
                    AttackPower = stats.AttackPower,
                    SpellPower = stats.SpellPower,
                    Defense = stats.Defense,
                    MagicResistance = stats.MagicResistance,
                    Range = stats.AttackRange,
                    AttackSpeed = stats.AttackSpeed,
                    CastSpeed = stats.CastSpeed,
                    CriticalChance = stats.CriticalChance,
                    CriticalDamage = stats.CriticalDamage,
                    Accuracy = stats.Accuracy,
                    Evasion = stats.Evasion,
                    HealthRegeneration = stats.HealthRegeneration,
                    ManaRegeneration = stats.ManaRegeneration,
                    CooldownReduction = stats.CooldownReduction,
                    IsDualWielding = EquipmentService.IsDualWielding(loadout),
                    HasTaunt = definition.HeroClass == HeroClass.Warrior,
                    StatusEffects = effects
                };
            }).ToList();

            EncounterDefinition encounter = FindEncounter(node.Id);
            List<EnemyDefinition> pool = encounter != null && encounter.Enemies.Count > 0
                ? encounter.Enemies.ToList()
                : enemies.Where(enemy => enemy.IsBoss == (node.Type == MapNodeType.Boss)).ToList();
            int enemyCount = node.Type == MapNodeType.Boss
                ? 1
                : node.Type == MapNodeType.Elite
                    ? Mathf.Min(3, pool.Count)
                    : Mathf.Min(2, pool.Count);
            List<CombatantState> enemyUnits = new List<CombatantState>();
            for (int i = 0; i < enemyCount; i++)
            {
                EnemyDefinition enemy = pool[Math.Abs(seed + i) % pool.Count];
                int scale = Math.Max(1, node.Difficulty);
                int health = enemy.MaxHealth + scale * 8;
                enemyUnits.Add(new CombatantState
                {
                    Id = $"{enemy.Id}-{i}",
                    Side = CombatSide.Enemy,
                    Position = new FormationPosition(i % 3, i / 3),
                    MaxHealth = health,
                    CurrentHealth = health,
                    AttackPower = enemy.Power + scale,
                    Defense = enemy.Defense * 5f + scale,
                    MagicResistance = enemy.Defense * 3f + scale,
                    Range = enemy.Range,
                    AttackSpeed = 0.8f + i * 0.05f,
                    CastSpeed = 1f,
                    CriticalChance = 10f,
                    CriticalDamage = 2f,
                    Accuracy = 100f
                });
            }

            return new CombatRequest
            {
                Seed = seed,
                MaxDurationMilliseconds = 90000,
                Heroes = heroUnits,
                Enemies = enemyUnits
            };
        }

        private StatusEffectCollection RestoreStatusEffects(
            IEnumerable<ActiveStatusEffectState> states)
        {
            StatusEffectCollection collection = new StatusEffectCollection();
            if (states == null)
            {
                return collection;
            }

            foreach (ActiveStatusEffectState state in states)
            {
                StatusEffectDefinition definition = FindStatusEffect(state.DefinitionId);
                if (definition != null)
                {
                    collection.Restore(state, definition.CreateRuntimeData());
                }
            }

            return collection;
        }

        private static int EquipmentOrder(EquipmentSlot slot)
        {
            if (slot == EquipmentSlot.MainWeapon)
            {
                return 0;
            }

            return slot == EquipmentSlot.SecondaryWeapon ? 1 : 2 + (int)slot;
        }
    }
}
