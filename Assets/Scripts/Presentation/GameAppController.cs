using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using TaskbarTactics.Content;
using TaskbarTactics.Core.Combat;
using TaskbarTactics.Core.Localization;
using TaskbarTactics.Core.Loot;
using TaskbarTactics.Core.Models;
using TaskbarTactics.Core.Progression;
using TaskbarTactics.Infrastructure.Persistence;
using UnityEngine;

namespace TaskbarTactics.Presentation
{
    public sealed class GameAppController : MonoBehaviour
    {
        public static string TestSaveDirectoryOverride { get; set; }

        [Header("Editable content and presentation")]
        [SerializeField] private GameContentCatalog catalog;
        [SerializeField] private CombatPresenter combatPresenter;
        [SerializeField] private StripHudController stripHud;
        [SerializeField] private ManagementUiController managementUi;
        [SerializeField] private WindowModeController windowMode;

        [Header("Pacing")]
        [SerializeField, Min(30f)] private float combatPresentationSeconds = 30f;
        [SerializeField, Min(1f)] private float nonCombatNodeSeconds = 4f;

        private readonly CombatSimulator combatSimulator = new CombatSimulator();
        private readonly LootGenerator lootGenerator = new LootGenerator();
        private readonly RouteSelector routeSelector = new RouteSelector();
        private readonly OfflineProgressService offlineProgress =
            new OfflineProgressService(TimeSpan.FromHours(8));
        private readonly LocalizationCatalog localization = LocalizationCatalog.CreateBuiltIn();

        private JsonSaveStore saveStore;
        private Coroutine expeditionRoutine;

        public GameContentCatalog Catalog => catalog;
        public GameState State { get; private set; }
        public string CurrentStatus { get; private set; } = "Preparando el campamento";
        public bool HasAttention { get; private set; }

        public event Action StateChanged;
        public event Action<string, string> StatusChanged;
        public event Action<bool> AttentionChanged;

        public void Configure(
            GameContentCatalog content,
            CombatPresenter combat,
            StripHudController strip,
            ManagementUiController management,
            WindowModeController window)
        {
            catalog = content;
            combatPresenter = combat;
            stripHud = strip;
            managementUi = management;
            windowMode = window;
        }

        private void Awake()
        {
            Application.runInBackground = true;
            QualitySettings.antiAliasing = 0;
            string saveDirectory = string.IsNullOrEmpty(TestSaveDirectoryOverride)
                ? Path.Combine(Application.persistentDataPath, "Saves")
                : TestSaveDirectoryOverride;
            saveStore = new JsonSaveStore(saveDirectory);
            State = saveStore.LoadOrDefault();
            EnsureRosterAndStarterItems();
            ResolveOfflineProgress();
        }

        private void Start()
        {
            stripHud.Bind(this, windowMode);
            managementUi.Bind(this, windowMode);
            if (State.Expedition.IsActive)
            {
                expeditionRoutine = StartCoroutine(RunExpedition());
            }
            else
            {
                SetStatus("Escuadrón en el campamento");
            }

            RaiseStateChanged();
        }

        private void OnApplicationQuit()
        {
            Save();
        }

        public void StartExpedition()
        {
            if (State.Expedition.IsActive ||
                State.Party.Heroes.Count(hero => hero.IsSelected) != 3)
            {
                return;
            }

            HasAttention = false;
            AttentionChanged?.Invoke(false);
            State.Expedition = new ExpeditionState
            {
                IsActive = true,
                CurrentNodeId = catalog.Map.Nodes.First().Id,
                Seed = unchecked((int)DateTime.UtcNow.Ticks),
                CompletedNodes = 0,
                CompletedNodeIds = new List<string>()
            };
            Save();
            RaiseStateChanged();
            expeditionRoutine = StartCoroutine(RunExpedition());
        }

        public void SetRoutePreference(RoutePreference preference)
        {
            State.Party.RoutePreference = preference;
            SaveAndRefresh();
        }

        public void SetFormation(string heroId, FormationPosition position)
        {
            if (State.Party.TrySetFormation(heroId, position))
            {
                SaveAndRefresh();
            }
        }

        public void SelectOrReplaceHero(string heroId, string replaceHeroId)
        {
            if (State.Party.IsFormationLocked)
            {
                return;
            }

            HeroState hero = State.Party.GetHero(heroId);
            if (hero == null || hero.IsSelected)
            {
                return;
            }

            List<HeroState> selected = State.Party.Heroes.Where(item => item.IsSelected).ToList();
            if (selected.Count >= 3)
            {
                HeroState replacement = State.Party.GetHero(replaceHeroId);
                if (replacement == null || !replacement.IsSelected)
                {
                    replacement = selected[selected.Count - 1];
                }

                replacement.IsSelected = false;
            }

            hero.IsSelected = true;
            EnsureUniqueSelectedPositions();
            SaveAndRefresh();
        }

        public void CycleSkill(string heroId, bool passive)
        {
            if (State.Party.IsFormationLocked)
            {
                return;
            }

            HeroState state = State.Party.GetHero(heroId);
            HeroDefinition definition = catalog.FindHero(heroId);
            if (state == null || definition == null)
            {
                return;
            }

            IReadOnlyList<SkillDefinition> choices =
                passive ? definition.PassiveSkills : definition.ActiveSkills;
            string current = passive ? state.PassiveSkillId : state.ActiveSkillId;
            int next = (IndexOf(choices, current) + 1) % choices.Count;
            if (passive)
            {
                state.PassiveSkillId = choices[next].Id;
            }
            else
            {
                state.ActiveSkillId = choices[next].Id;
            }

            SaveAndRefresh();
        }

        public void CycleEquipment(string heroId, EquipmentSlot slot)
        {
            if (State.Party.IsFormationLocked)
            {
                return;
            }

            HeroState hero = State.Party.GetHero(heroId);
            List<InventoryItem> candidates = State.Inventory.Where(item => item.Slot == slot).ToList();
            if (hero == null || candidates.Count == 0)
            {
                return;
            }

            string currentId = hero.EquippedItemIds
                .FirstOrDefault(id => State.Inventory.Find(item => item.InstanceId == id)?.Slot == slot);
            int currentIndex = candidates.FindIndex(item => item.InstanceId == currentId);
            InventoryItem next = candidates[(currentIndex + 1) % candidates.Count];
            hero.EquippedItemIds.RemoveAll(id =>
                State.Inventory.Find(item => item.InstanceId == id)?.Slot == slot);
            hero.EquippedItemIds.Add(next.InstanceId);
            SaveAndRefresh();
        }

        public void ToggleLanguage()
        {
            State.LanguageCode = State.LanguageCode == "es" ? "en" : "es";
            SaveAndRefresh();
        }

        public string HeroName(string heroId)
        {
            HeroDefinition definition = catalog.FindHero(heroId);
            if (definition == null)
            {
                return heroId;
            }

            return State.LanguageCode == "en" ? definition.DisplayNameEn : definition.DisplayNameEs;
        }

        public void Quit()
        {
            Save();
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }

        private IEnumerator RunExpedition()
        {
            while (State.Expedition.IsActive)
            {
                MapNodeDefinition node = catalog.Map.FindNode(State.Expedition.CurrentNodeId);
                if (node == null)
                {
                    CompleteExpedition();
                    yield break;
                }

                SetStatus(NodeStatus(node));
                CombatOutcome outcome = CombatOutcome.Victory;
                if (node.Type == MapNodeType.Combat ||
                    node.Type == MapNodeType.Elite ||
                    node.Type == MapNodeType.Boss)
                {
                    State.Party.IsFormationLocked = true;
                    CombatRequest request = catalog.CreateCombatRequest(
                        SelectedHeroes(),
                        node,
                        State.Expedition.Seed + State.Expedition.CompletedNodes);
                    CombatResult result = combatSimulator.Simulate(request);
                    yield return combatPresenter.Play(request, result, catalog, combatPresentationSeconds);
                    outcome = result.Outcome;
                    State.Party.IsFormationLocked = false;
                }
                else
                {
                    yield return new WaitForSecondsRealtime(nonCombatNodeSeconds);
                }

                if (outcome != CombatOutcome.Victory)
                {
                    ExpeditionResolver.ResolveDefeat(State);
                    SetAttention("strip.defeat");
                    SetStatus(Localize("strip.defeat"));
                    SaveAndRefresh();
                    yield break;
                }

                RewardNode(node);
                Advance(node);
                SaveAndRefresh();
                yield return new WaitForSecondsRealtime(0.5f);
            }
        }

        private void RewardNode(MapNodeDefinition node)
        {
            State.Expedition.CompletedNodes++;
            State.Expedition.CompletedNodeIds ??= new List<string>();
            if (!State.Expedition.CompletedNodeIds.Contains(node.Id))
            {
                State.Expedition.CompletedNodeIds.Add(node.Id);
            }

            int itemCount = node.Difficulty <= 0 ? 0 :
                node.Type == MapNodeType.Treasure ? 2 :
                node.Type == MapNodeType.Elite || node.Type == MapNodeType.Boss ? 2 : 1;
            IReadOnlyList<InventoryItem> loot = lootGenerator.Generate(
                catalog.CreateLootTable(),
                State.Expedition.Seed + State.Expedition.CompletedNodes * 31,
                itemCount);
            State.Inventory.AddRange(loot);
            State.Expedition.CollectedItemIds.AddRange(loot.Select(item => item.InstanceId));
            foreach (HeroState hero in SelectedHeroes())
            {
                hero.Experience += node.Difficulty <= 0 ? 0 : 20 + node.Difficulty * 5;
                while (hero.Experience >= hero.Level * 100)
                {
                    hero.Experience -= hero.Level * 100;
                    hero.Level++;
                }
            }

            if (loot.Any(item => item.Rarity == ItemRarity.Epic))
            {
                SetAttention("strip.rare_loot");
            }
        }

        private void Advance(MapNodeDefinition node)
        {
            if (node.NextNodeIds.Count == 0)
            {
                CompleteExpedition();
                return;
            }

            List<MapNodeState> choices = node.NextNodeIds
                .Select(id => catalog.Map.FindNode(id))
                .Where(item => item != null)
                .Select(item => new MapNodeState(item.Id, item.Type, item.Difficulty))
                .ToList();
            State.Expedition.CurrentNodeId =
                routeSelector.SelectNext(choices, State.Party.RoutePreference).Id;
        }

        private void CompleteExpedition()
        {
            State.Expedition.IsActive = false;
            State.Expedition.CurrentNodeId = string.Empty;
            State.Party.IsFormationLocked = false;
            SetAttention("strip.complete");
            SetStatus(Localize("strip.complete"));
        }

        private void ResolveOfflineProgress()
        {
            if (!State.Expedition.IsActive || State.LastSavedUtcTicks <= 0)
            {
                return;
            }

            DateTime lastSave = new DateTime(State.LastSavedUtcTicks, DateTimeKind.Utc);
            OfflineProgressResult progress = offlineProgress.Calculate(lastSave, DateTime.UtcNow, 45);
            int nodesToResolve = Mathf.Min(progress.ResolvedNodes, catalog.Map.Nodes.Count);
            for (int i = 0; i < nodesToResolve && State.Expedition.IsActive; i++)
            {
                MapNodeDefinition node = catalog.Map.FindNode(State.Expedition.CurrentNodeId);
                if (node == null)
                {
                    CompleteExpedition();
                    break;
                }

                if (node.Type == MapNodeType.Combat ||
                    node.Type == MapNodeType.Elite ||
                    node.Type == MapNodeType.Boss)
                {
                    CombatRequest request = catalog.CreateCombatRequest(
                        SelectedHeroes(),
                        node,
                        State.Expedition.Seed + State.Expedition.CompletedNodes);
                    if (combatSimulator.Simulate(request).Outcome != CombatOutcome.Victory)
                    {
                        ExpeditionResolver.ResolveDefeat(State);
                        break;
                    }
                }

                RewardNode(node);
                Advance(node);
            }

            if (nodesToResolve > 0)
            {
                CurrentStatus = string.Format(
                    localization.Get(State.LanguageCode, "offline.summary"),
                    nodesToResolve,
                    progress.SimulatedDuration);
                HasAttention = true;
            }
        }

        private void EnsureRosterAndStarterItems()
        {
            if (State.Party.Heroes.Count == 0)
            {
                FormationPosition[] starterPositions =
                {
                    new FormationPosition(1, 0),
                    new FormationPosition(0, 2),
                    new FormationPosition(2, 2),
                    new FormationPosition(0, 0),
                    new FormationPosition(1, 2),
                    new FormationPosition(2, 0)
                };
                for (int i = 0; i < catalog.Heroes.Count; i++)
                {
                    HeroDefinition definition = catalog.Heroes[i];
                    State.Party.Heroes.Add(new HeroState
                    {
                        DefinitionId = definition.Id,
                        IsSelected = i < 3,
                        Position = starterPositions[i],
                        ActiveSkillId = definition.ActiveSkills[0].Id,
                        PassiveSkillId = definition.PassiveSkills[0].Id,
                        EquippedItemIds = new List<string>()
                    });
                }
            }

            if (State.Inventory.Count == 0)
            {
                State.Inventory.AddRange(lootGenerator.Generate(catalog.CreateLootTable(), 1337, 8));
            }
        }

        private void EnsureUniqueSelectedPositions()
        {
            FormationPosition[] defaults =
            {
                new FormationPosition(1, 0),
                new FormationPosition(0, 2),
                new FormationPosition(2, 2)
            };
            List<HeroState> selected = SelectedHeroes();
            for (int i = 0; i < selected.Count; i++)
            {
                selected[i].Position = defaults[i];
            }
        }

        private List<HeroState> SelectedHeroes()
        {
            return State.Party.Heroes.Where(hero => hero.IsSelected).Take(3).ToList();
        }

        private static int IndexOf(IReadOnlyList<SkillDefinition> choices, string id)
        {
            for (int i = 0; i < choices.Count; i++)
            {
                if (choices[i].Id == id)
                {
                    return i;
                }
            }

            return -1;
        }

        private string NodeStatus(MapNodeDefinition node)
        {
            return $"{node.Id} · {node.Type} · Dificultad {node.Difficulty}";
        }

        private string Localize(string key)
        {
            return localization.Get(State.LanguageCode, key);
        }

        private void SetAttention(string localizationKey)
        {
            HasAttention = true;
            CurrentStatus = Localize(localizationKey);
            AttentionChanged?.Invoke(true);
        }

        private void SetStatus(string status)
        {
            CurrentStatus = status;
            StatusChanged?.Invoke(status, State.Expedition.CurrentNodeId);
        }

        private void SaveAndRefresh()
        {
            Save();
            RaiseStateChanged();
        }

        private void Save()
        {
            saveStore?.Save(State);
        }

        private void RaiseStateChanged()
        {
            StateChanged?.Invoke();
            stripHud?.Refresh(this);
        }
    }
}
