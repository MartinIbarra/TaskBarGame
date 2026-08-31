using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using TaskbarTactics.Content;
using TaskbarTactics.Core.Combat;
using TaskbarTactics.Core.Equipment;
using TaskbarTactics.Core.Localization;
using TaskbarTactics.Core.Loot;
using TaskbarTactics.Core.Models;
using TaskbarTactics.Core.Progression;
using TaskbarTactics.Core.Stats;
using TaskbarTactics.Infrastructure.Persistence;
using UnityEngine;

namespace TaskbarTactics.Presentation
{
    public sealed class GameAppController : MonoBehaviour
    {
        public const int PartySize = 4;
        public static string TestSaveDirectoryOverride { get; set; }

        [Header("Editable content and presentation")]
        [SerializeField] private GameContentCatalog catalog;
        [SerializeField] private CombatPresenter combatPresenter;
        [SerializeField] private StripHudController stripHud;
        [SerializeField] private ManagementUiController managementUi;
        [SerializeField] private WindowModeController windowMode;
        [SerializeField] private TownIntroPresenter townIntroPresenter;
        [SerializeField] private DefeatOverlayPresenter defeatOverlayPresenter;
        [SerializeField] private NodeTransitionPresenter nodeTransitionPresenter;

        [Header("Pacing")]
        [SerializeField, Min(1f), Tooltip("Maximum real-time duration of a combat replay.")]
        private float combatPresentationSeconds = 30f;
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
            WindowModeController window,
            TownIntroPresenter townIntro = null,
            DefeatOverlayPresenter defeatOverlay = null,
            NodeTransitionPresenter nodeTransition = null)
        {
            catalog = content;
            combatPresenter = combat;
            stripHud = strip;
            managementUi = management;
            windowMode = window;
            townIntroPresenter = townIntro;
            defeatOverlayPresenter = defeatOverlay;
            nodeTransitionPresenter = nodeTransition;
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
            EnsureSelectedPartyLimit();
            if (!State.Expedition.IsActive)
            {
                ClearSelectedParty();
                RestoreAllHeroResources();
            }
            ResolveOfflineProgress();
        }

        private void Start()
        {
            stripHud.Bind(this, windowMode);
            managementUi.Bind(this, windowMode);
            combatPresenter?.ShowBattleback(State.Expedition.CurrentNodeId);
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
                State.Party.Heroes.Count(hero => hero.IsSelected) != PartySize)
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
            windowMode?.ShowStrip();
            expeditionRoutine = StartCoroutine(RunExpedition());
        }

        public void ResetExpeditionProgress()
        {
            if (expeditionRoutine != null)
            {
                StopCoroutine(expeditionRoutine);
                expeditionRoutine = null;
            }

            State.Expedition = new ExpeditionState
            {
                IsActive = false,
                CurrentNodeId = string.Empty,
                Seed = 0,
                CompletedNodes = 0,
                CompletedNodeIds = new List<string>()
            };
            State.Party.IsFormationLocked = false;
            State.Inventory.Clear();
            ClearSelectedParty();
            RestoreAllHeroResources();
            combatPresenter?.Clear();
            combatPresenter?.ShowBattleback("default");
            SetStatus("Escuadrón en el campamento");
            SaveAndRefresh();
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

        public bool AssignHeroToFormationSlot(string heroId, FormationPosition position)
        {
            if (State.Party.IsFormationLocked)
            {
                return false;
            }

            HeroState hero = State.Party.GetHero(heroId);
            if (hero == null)
            {
                return false;
            }

            HeroState previousOccupant = State.Party.Heroes.FirstOrDefault(item =>
                item.DefinitionId != heroId &&
                item.IsSelected &&
                item.Position.Equals(position));
            if (previousOccupant != null)
            {
                previousOccupant.IsSelected = false;
            }

            List<HeroState> selected = State.Party.Heroes
                .Where(item => item.IsSelected && item.DefinitionId != heroId)
                .ToList();
            if (!hero.IsSelected && selected.Count >= PartySize)
            {
                selected[selected.Count - 1].IsSelected = false;
            }

            hero.IsSelected = true;
            hero.Position = position;
            SaveAndRefresh();
            return true;
        }

        public bool UnequipHeroFromFormationSlot(string heroId, FormationPosition position)
        {
            if (State.Party.IsFormationLocked)
            {
                return false;
            }

            HeroState hero = State.Party.GetHero(heroId);
            if (hero == null ||
                !hero.IsSelected ||
                !hero.Position.Equals(position))
            {
                return false;
            }

            hero.IsSelected = false;
            SaveAndRefresh();
            return true;
        }

        public void SelectOrReplaceHero(string heroId, string replaceHeroId)
        {
            if (State.Party.IsFormationLocked)
            {
                return;
            }

            HeroState hero = State.Party.GetHero(heroId);
            if (hero == null)
            {
                return;
            }

            List<HeroState> selected = State.Party.Heroes.Where(item => item.IsSelected).ToList();
            if (hero.IsSelected)
            {
                if (selected.Count > 1)
                {
                    hero.IsSelected = false;
                }

                SaveAndRefresh();
                return;
            }

            if (selected.Count >= PartySize)
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
            HeroDefinition definition = catalog.FindHero(heroId);
            if (hero == null || definition == null)
            {
                return;
            }

            EquipmentLoadout loadout = catalog.CreateLoadout(hero, State.Inventory);
            List<InventoryItem> candidates = State.Inventory
                .Where(item => !IsEquippedByAnotherHero(heroId, item.InstanceId))
                .Where(item => EquipmentService.CanEquip(
                    definition.EquipmentProfile,
                    loadout,
                    slot,
                    catalog.CreateItemDescriptor(item)))
                .ToList();
            if (candidates.Count == 0)
            {
                return;
            }

            string currentId = hero.GetEquippedItemId(slot);
            int currentIndex = candidates.FindIndex(item => item.InstanceId == currentId);
            InventoryItem next = candidates[(currentIndex + 1) % candidates.Count];
            EquipmentDescriptor descriptor = catalog.CreateItemDescriptor(next);
            EquipmentResult result = EquipmentService.TryEquip(
                definition.EquipmentProfile,
                loadout,
                slot,
                descriptor);
            if (!result.Succeeded)
            {
                return;
            }

            hero.SetEquippedItem(slot, next.InstanceId);
            if (slot == EquipmentSlot.MainWeapon &&
                descriptor.Handedness == Handedness.TwoHanded)
            {
                hero.SetEquippedItem(EquipmentSlot.SecondaryWeapon, null);
            }

            ClampHeroResources(hero);
            SaveAndRefresh();
        }

        public void ToggleLanguage()
        {
            State.LanguageCode = State.LanguageCode == "es" ? "en" : "es";
            SaveAndRefresh();
        }

        public void SavePartyChanges()
        {
            SaveAndRefresh();
        }

        public void ApplyFormationPreset(int presetIndex)
        {
            if (State.Party.IsFormationLocked)
            {
                return;
            }

            List<HeroState> selected = SelectedHeroes();
            FormationPosition[] positions = FormationPreset(presetIndex);
            for (int i = 0; i < selected.Count && i < positions.Length; i++)
            {
                selected[i].Position = positions[i];
            }

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

                combatPresenter?.ShowBattleback(node.Id);
                SetStatus(NodeStatus(node));
                CombatOutcome outcome = CombatOutcome.Victory;
                if (RunsCombat(node))
                {
                    State.Party.IsFormationLocked = true;
                    CombatRequest request = catalog.CreateCombatRequest(
                        SelectedHeroes(),
                        State.Inventory,
                        node,
                        State.Expedition.Seed + State.Expedition.CompletedNodes);
                    CombatResult result = combatSimulator.Simulate(request);
                    ApplyCombatResult(result);
                    yield return combatPresenter.Play(
                        request,
                        result,
                        catalog,
                        combatPresentationSeconds,
                        node.Id);
                    outcome = result.Outcome;
                    State.Party.IsFormationLocked = false;
                }
                else
                {
                    if (node.Id == "town")
                    {
                        RestoreAllHeroResources();
                        if (townIntroPresenter != null)
                        {
                            yield return townIntroPresenter.Play(SelectedHeroes(), catalog);
                        }
                        else
                        {
                            yield return new WaitForSecondsRealtime(nonCombatNodeSeconds);
                        }
                    }
                    else
                    {
                        yield return new WaitForSecondsRealtime(nonCombatNodeSeconds);
                    }
                }

                if (outcome != CombatOutcome.Victory)
                {
                    ExpeditionResolver.ResolveDefeat(State);
                    RestoreAllHeroResources();
                    HasAttention = true;
                    CurrentStatus = string.Empty;
                    AttentionChanged?.Invoke(true);
                    SaveAndRefresh();
                    if (defeatOverlayPresenter != null)
                    {
                        yield return defeatOverlayPresenter.Play(windowMode);
                    }

                    yield break;
                }

                IReadOnlyList<InventoryItem> rewardLoot = GenerateRewardLoot(node);
                if (ShowsChestReward(node))
                {
                    string rewardItemId = rewardLoot.FirstOrDefault()?.DefinitionId;
                    yield return combatPresenter.ShowRewardChest(rewardItemId);
                }

                RewardNode(node, rewardLoot);
                Advance(node);
                SaveAndRefresh();
                MapNodeDefinition nextNode = catalog.Map.FindNode(State.Expedition.CurrentNodeId);
                if (State.Expedition.IsActive && nextNode != null && nodeTransitionPresenter != null)
                {
                    yield return nodeTransitionPresenter.Play(nextNode);
                }
                else
                {
                    yield return new WaitForSecondsRealtime(0.5f);
                }
            }
        }

        private IReadOnlyList<InventoryItem> GenerateRewardLoot(MapNodeDefinition node)
        {
            int itemCount = node.Difficulty <= 0 ? 0 :
                node.Type == MapNodeType.Treasure ? 2 :
                node.Type == MapNodeType.Elite || node.Type == MapNodeType.Boss ? 2 : 1;
            if (ShowsChestReward(node))
            {
                itemCount = 1;
            }

            LootTable lootTable = ShowsChestReward(node)
                ? catalog.CreateStarterWeaponLootTable()
                : catalog.CreateLootTable();
            return lootGenerator.Generate(
                lootTable,
                State.Expedition.Seed + (State.Expedition.CompletedNodes + 1) * 31,
                itemCount);
        }

        private void RewardNode(MapNodeDefinition node, IReadOnlyList<InventoryItem> loot)
        {
            State.Expedition.CompletedNodes++;
            State.Expedition.CompletedNodeIds ??= new List<string>();
            if (!State.Expedition.CompletedNodeIds.Contains(node.Id))
            {
                State.Expedition.CompletedNodeIds.Add(node.Id);
            }

            loot ??= Array.Empty<InventoryItem>();
            State.Inventory.AddRange(loot);
            State.Expedition.CollectedItemIds.AddRange(loot.Select(item => item.InstanceId));
            foreach (HeroState hero in SelectedHeroes())
            {
                AddHeroExperience(
                    hero,
                    node.Difficulty <= 0 ? 0L : 20L + node.Difficulty * 5L);
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
            RestoreAllHeroResources();
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

                if (RunsCombat(node))
                {
                    CombatRequest request = catalog.CreateCombatRequest(
                        SelectedHeroes(),
                        State.Inventory,
                        node,
                        State.Expedition.Seed + State.Expedition.CompletedNodes);
                    CombatResult result = combatSimulator.Simulate(request);
                    ApplyCombatResult(result);
                    if (result.Outcome != CombatOutcome.Victory)
                    {
                        ExpeditionResolver.ResolveDefeat(State);
                        RestoreAllHeroResources();
                        break;
                    }
                }
                else if (node.Id == "town")
                {
                    RestoreAllHeroResources();
                }

                RewardNode(node, GenerateRewardLoot(node));
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

        private static bool RunsCombat(MapNodeDefinition node)
        {
            return node != null &&
                   (node.Type == MapNodeType.Combat ||
                    node.Type == MapNodeType.Elite ||
                    node.Type == MapNodeType.Boss ||
                    node.Id == "cave" ||
                    node.Id == "mountain_pass");
        }

        private static bool ShowsChestReward(MapNodeDefinition node)
        {
            return node != null && (node.Id == "cave" || node.Id == "last_bastion");
        }

        private void EnsureRosterAndStarterItems()
        {
            if (State.Party.Heroes.Count == 0)
            {
                FormationPosition[] starterPositions =
                {
                    new FormationPosition(1, 0),
                    new FormationPosition(1, 1),
                    new FormationPosition(1, 2),
                    new FormationPosition(1, 3),
                    new FormationPosition(2, 2),
                    new FormationPosition(2, 3)
                };
                for (int i = 0; i < catalog.Heroes.Count; i++)
                {
                    HeroDefinition definition = catalog.Heroes[i];
                    State.Party.Heroes.Add(new HeroState
                    {
                        DefinitionId = definition.Id,
                        IsSelected = false,
                        Position = starterPositions[i],
                        ActiveSkillId = definition.ActiveSkills[0].Id,
                        PassiveSkillId = definition.PassiveSkills[0].Id,
                        EquippedItems = new List<EquippedItemState>(),
                        ActiveStatusEffects = new List<ActiveStatusEffectState>()
                    });
                }
            }

            foreach (HeroState hero in State.Party.Heroes.Where(item =>
                         !item.ResourcesInitialized))
            {
                RestoreHeroResources(hero);
            }
        }

        private bool IsEquippedByAnotherHero(string heroId, string instanceId)
        {
            return State.Party.Heroes.Any(hero =>
                hero.DefinitionId != heroId &&
                hero.EquippedItems != null &&
                hero.EquippedItems.Any(item => item.ItemInstanceId == instanceId));
        }

        private void AddHeroExperience(HeroState hero, long amount)
        {
            if (hero == null || amount <= 0L)
            {
                return;
            }

            HeroStats before = catalog.ResolveHeroStats(hero, State.Inventory);
            HeroProgression progression = new HeroProgression(hero.Level, hero.Experience);
            progression.AddExperience(amount);
            hero.Level = progression.Level;
            hero.Experience = progression.Experience;
            HeroStats after = catalog.ResolveHeroStats(hero, State.Inventory);
            hero.CurrentHealth = Mathf.Clamp(
                hero.CurrentHealth + Mathf.Max(0, after.MaxHealth - before.MaxHealth),
                0,
                after.MaxHealth);
            hero.CurrentMana = Mathf.Clamp(
                hero.CurrentMana + Mathf.Max(0, after.MaxMana - before.MaxMana),
                0,
                after.MaxMana);
            hero.ResourcesInitialized = true;
        }

        private void ApplyCombatResult(CombatResult result)
        {
            if (result == null)
            {
                return;
            }

            foreach (CombatantResourceResult resource in result.HeroResources)
            {
                HeroState hero = State.Party.GetHero(resource.Id);
                if (hero == null)
                {
                    continue;
                }

                hero.CurrentHealth = resource.CurrentHealth;
                hero.CurrentMana = resource.CurrentMana;
                hero.ResourcesInitialized = true;
                hero.ActiveStatusEffects = resource.PersistentStatusEffects ??
                    new List<ActiveStatusEffectState>();
                ClampHeroResources(hero);
            }
        }

        private void RestoreAllHeroResources()
        {
            foreach (HeroState hero in State.Party.Heroes)
            {
                RestoreHeroResources(hero);
            }
        }

        private void RestoreHeroResources(HeroState hero)
        {
            if (hero == null || catalog.FindHero(hero.DefinitionId) == null)
            {
                return;
            }

            HeroStats stats = catalog.ResolveHeroStats(hero, State.Inventory);
            hero.CurrentHealth = stats.MaxHealth;
            hero.CurrentMana = stats.MaxMana;
            hero.ResourcesInitialized = true;
            hero.ActiveStatusEffects = new List<ActiveStatusEffectState>();
        }

        private void ClampHeroResources(HeroState hero)
        {
            if (hero == null || catalog.FindHero(hero.DefinitionId) == null)
            {
                return;
            }

            HeroStats stats = catalog.ResolveHeroStats(hero, State.Inventory);
            hero.CurrentHealth = Mathf.Clamp(hero.CurrentHealth, 0, stats.MaxHealth);
            hero.CurrentMana = Mathf.Clamp(hero.CurrentMana, 0, stats.MaxMana);
        }

        private void EnsureSelectedPartyLimit()
        {
            List<HeroState> selected = State.Party.Heroes.Where(hero => hero.IsSelected).ToList();
            if (selected.Count > PartySize)
            {
                foreach (HeroState hero in selected.Skip(PartySize))
                {
                    hero.IsSelected = false;
                }
            }

            if (selected.Count == PartySize &&
                selected.Select(hero => hero.Position).Distinct().Count() != PartySize)
            {
                EnsureUniqueSelectedPositions();
            }
        }

        private void ClearSelectedParty()
        {
            foreach (HeroState hero in State.Party.Heroes)
            {
                hero.IsSelected = false;
            }
        }

        private void EnsureUniqueSelectedPositions()
        {
            FormationPosition[] defaults =
            {
                new FormationPosition(1, 0),
                new FormationPosition(1, 1),
                new FormationPosition(1, 2),
                new FormationPosition(1, 3)
            };
            List<HeroState> selected = SelectedHeroes();
            for (int i = 0; i < selected.Count; i++)
            {
                selected[i].Position = defaults[i];
            }
        }

        private List<HeroState> SelectedHeroes()
        {
            return State.Party.Heroes.Where(hero => hero.IsSelected).Take(PartySize).ToList();
        }

        private static FormationPosition[] FormationPreset(int presetIndex)
        {
            switch (presetIndex)
            {
                case 1:
                    return new[]
                    {
                        new FormationPosition(0, 1),
                        new FormationPosition(1, 0),
                        new FormationPosition(1, 1),
                        new FormationPosition(1, 2)
                    };
                case 2:
                    return new[]
                    {
                        new FormationPosition(0, 1),
                        new FormationPosition(1, 1),
                        new FormationPosition(0, 2),
                        new FormationPosition(1, 2)
                    };
                default:
                    return new[]
                    {
                        new FormationPosition(1, 0),
                        new FormationPosition(1, 1),
                        new FormationPosition(1, 2),
                        new FormationPosition(1, 3)
                    };
            }
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
