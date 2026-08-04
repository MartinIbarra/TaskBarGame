using System;
using System.Collections.Generic;
using System.Linq;
using TaskbarTactics.Core.Combat;
using TaskbarTactics.Core.Loot;
using TaskbarTactics.Core.Models;
using UnityEngine;

namespace TaskbarTactics.Content
{
    [CreateAssetMenu(menuName = "Taskbar Tactics/Content Catalog")]
    public sealed class GameContentCatalog : ScriptableObject
    {
        [SerializeField] private List<HeroDefinition> heroes = new List<HeroDefinition>();
        [SerializeField] private List<SkillDefinition> skills = new List<SkillDefinition>();
        [SerializeField] private List<ItemDefinition> items = new List<ItemDefinition>();
        [SerializeField] private List<AffixDefinition> affixes = new List<AffixDefinition>();
        [SerializeField] private List<SynergyDefinition> synergies = new List<SynergyDefinition>();
        [SerializeField] private List<EnemyDefinition> enemies = new List<EnemyDefinition>();
        [SerializeField] private List<EncounterDefinition> encounters = new List<EncounterDefinition>();
        [SerializeField] private MapDefinition map;
        [SerializeField] private List<CosmeticDefinition> cosmetics = new List<CosmeticDefinition>();

        public IReadOnlyList<HeroDefinition> Heroes => heroes;
        public IReadOnlyList<SkillDefinition> Skills => skills;
        public IReadOnlyList<ItemDefinition> Items => items;
        public IReadOnlyList<AffixDefinition> Affixes => affixes;
        public IReadOnlyList<SynergyDefinition> Synergies => synergies;
        public IReadOnlyList<EnemyDefinition> Enemies => enemies;
        public IReadOnlyList<EncounterDefinition> Encounters => encounters;
        public MapDefinition Map => map;
        public IReadOnlyList<CosmeticDefinition> Cosmetics => cosmetics;

        public void Configure(
            IEnumerable<HeroDefinition> heroDefinitions,
            IEnumerable<SkillDefinition> skillDefinitions,
            IEnumerable<ItemDefinition> itemDefinitions,
            IEnumerable<AffixDefinition> affixDefinitions,
            IEnumerable<SynergyDefinition> synergyDefinitions,
            IEnumerable<EnemyDefinition> enemyDefinitions,
            IEnumerable<EncounterDefinition> encounterDefinitions,
            MapDefinition mapDefinition,
            IEnumerable<CosmeticDefinition> cosmeticDefinitions)
        {
            heroes = heroDefinitions.ToList();
            skills = skillDefinitions.ToList();
            items = itemDefinitions.ToList();
            affixes = affixDefinitions.ToList();
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

        public EnemyDefinition FindEnemy(string id)
        {
            return enemies.Find(item => item.Id == id);
        }

        public LootTable CreateLootTable()
        {
            return new LootTable
            {
                Entries = items.Select(item => new LootEntry(item.Id, item.Slot, 10)).ToList(),
                AffixIds = affixes.Select(item => item.Id).ToList()
            };
        }

        public CombatRequest CreateCombatRequest(
            IReadOnlyList<HeroState> selectedHeroes,
            MapNodeDefinition node,
            int seed)
        {
            List<CombatantState> heroUnits = selectedHeroes.Select((state, index) =>
            {
                HeroDefinition definition = FindHero(state.DefinitionId);
                return new CombatantState
                {
                    Id = state.DefinitionId,
                    Side = CombatSide.Hero,
                    Position = state.Position,
                    MaxHealth = definition.MaxHealth + (state.Level - 1) * 8,
                    CurrentHealth = definition.MaxHealth + (state.Level - 1) * 8,
                    Power = definition.Power + state.Level - 1,
                    Defense = definition.Defense,
                    Range = definition.Range,
                    Speed = 10 + index,
                    HasTaunt = state.DefinitionId == "guardian"
                };
            }).ToList();

            int enemyCount = node.Type == MapNodeType.Boss ? 1 : node.Type == MapNodeType.Elite ? 3 : 2;
            List<EnemyDefinition> pool = enemies
                .Where(enemy => enemy.IsBoss == (node.Type == MapNodeType.Boss)).ToList();
            List<CombatantState> enemyUnits = new List<CombatantState>();
            for (int i = 0; i < enemyCount; i++)
            {
                EnemyDefinition enemy = pool[Math.Abs(seed + i) % pool.Count];
                int scale = Math.Max(1, node.Difficulty);
                enemyUnits.Add(new CombatantState
                {
                    Id = $"{enemy.Id}-{i}",
                    Side = CombatSide.Enemy,
                    Position = new FormationPosition(i % 3, i / 3),
                    MaxHealth = enemy.MaxHealth + scale * 8,
                    CurrentHealth = enemy.MaxHealth + scale * 8,
                    Power = enemy.Power + scale,
                    Defense = enemy.Defense + scale / 3,
                    Range = enemy.Range,
                    Speed = 9 + i
                });
            }

            return new CombatRequest
            {
                Seed = seed,
                MaxTicks = 900,
                Heroes = heroUnits,
                Enemies = enemyUnits
            };
        }
    }
}
