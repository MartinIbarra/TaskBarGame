using System.Collections.Generic;
using System.Linq;
using TMPro;
using TaskbarTactics.Content;
using TaskbarTactics.Core.Models;
using TaskbarTactics.Core.Progression;
using UnityEngine;
using UnityEngine.UI;

namespace TaskbarTactics.Presentation
{
    public sealed class ManagementUiController : MonoBehaviour
    {
        private const float UiSoundVolume = 0.65f;

        [Header("Navigation")]
        [SerializeField] private List<Button> tabButtons = new List<Button>();
        [SerializeField] private List<GameObject> panels = new List<GameObject>();
        [SerializeField] private Button closeButton;
        [SerializeField] private Button settingsShortcutButton;
        [SerializeField] private Button quitShortcutButton;

        [Header("Party and formation")]
        [SerializeField] private List<Button> heroButtons = new List<Button>();
        [SerializeField] private List<Button> formationButtons = new List<Button>();
        [SerializeField] private List<FormationSlotView> formationSlots = new List<FormationSlotView>();
        [SerializeField] private List<Sprite> formationHeroIcons = new List<Sprite>();
        [SerializeField] private TMP_Text partySummary;
        [SerializeField] private TMP_Text skillSummary;
        [SerializeField] private TMP_Text synergySummary;
        [SerializeField] private TMP_Text inventorySummary;
        [SerializeField] private TMP_Text mapSummary;
        [SerializeField] private TMP_Text settingsSummary;
        [SerializeField] private MapUiController mapUi;
        [SerializeField] private InventorySlotGridView inventoryGrid;

        [Header("Actions")]
        [SerializeField] private Button cycleActiveSkillButton;
        [SerializeField] private Button cyclePassiveSkillButton;
        [SerializeField] private List<Button> equipSlotButtons = new List<Button>();
        [SerializeField] private List<Button> routeButtons = new List<Button>();
        [SerializeField] private Button startExpeditionButton;
        [SerializeField] private Button resetExpeditionButton;
        [SerializeField] private Button languageButton;
        [SerializeField] private Button quitButton;

        private GameAppController app;
        private string activeHeroId = "warrior";
        private int activeFormationPreset;
        private int activePanelIndex;
        private Sprite commandNormalSprite;
        private Sprite commandPressedSprite;
        private Sprite commandSelectedSprite;
        private AudioClip formationSelectClip;

        public void Configure(
            IEnumerable<Button> navigation,
            IEnumerable<GameObject> panelRoots,
            Button close,
            Button settingsShortcut,
            Button quitShortcut,
            IEnumerable<Button> heroSelection,
            IEnumerable<Button> formation,
            IEnumerable<FormationSlotView> slots,
            IEnumerable<Sprite> slotHeroIcons,
            TMP_Text party,
            TMP_Text skills,
            TMP_Text synergies,
            TMP_Text inventory,
            TMP_Text map,
            TMP_Text settings,
            MapUiController mapVisual,
            Button cycleActive,
            Button cyclePassive,
            IEnumerable<Button> equipSlots,
            IEnumerable<Button> routes,
            Button start,
            Button reset,
            Button language,
            Button quit)
        {
            tabButtons = navigation.ToList();
            panels = panelRoots.ToList();
            closeButton = close;
            settingsShortcutButton = settingsShortcut;
            quitShortcutButton = quitShortcut;
            heroButtons = heroSelection.ToList();
            formationButtons = formation.ToList();
            formationSlots = slots.ToList();
            formationHeroIcons = slotHeroIcons.ToList();
            foreach (FormationSlotView slot in formationSlots)
            {
                slot.SetOwner(this);
            }
            partySummary = party;
            skillSummary = skills;
            synergySummary = synergies;
            inventorySummary = inventory;
            mapSummary = map;
            settingsSummary = settings;
            mapUi = mapVisual;
            cycleActiveSkillButton = cycleActive;
            cyclePassiveSkillButton = cyclePassive;
            equipSlotButtons = equipSlots.ToList();
            routeButtons = routes.ToList();
            startExpeditionButton = start;
            resetExpeditionButton = reset;
            languageButton = language;
            quitButton = quit;
        }

        public void Bind(GameAppController targetApp, WindowModeController window)
        {
            app = targetApp;
            LoadCommandSprites();
            ApplyCommandButtonStates();
            app.StateChanged += Refresh;
            closeButton.onClick.AddListener(window.ShowStrip);
            settingsShortcutButton?.onClick.AddListener(() => ShowPanel(5));
            quitShortcutButton?.onClick.AddListener(app.Quit);
            quitButton.onClick.AddListener(app.Quit);
            startExpeditionButton.onClick.AddListener(app.StartExpedition);
            resetExpeditionButton?.onClick.AddListener(app.ResetExpeditionProgress);
            cycleActiveSkillButton.onClick.AddListener(() => app.CycleSkill(activeHeroId, false));
            cyclePassiveSkillButton.onClick.AddListener(() => app.CycleSkill(activeHeroId, true));
            languageButton.onClick.AddListener(app.ToggleLanguage);

            for (int i = 0; i < tabButtons.Count; i++)
            {
                int captured = i;
                tabButtons[i].onClick.AddListener(() => ShowPanel(captured));
            }

            for (int i = 0; i < formationButtons.Count; i++)
            {
                int captured = i;
                formationButtons[i].onClick.AddListener(() => SelectFormationPreset(captured));
            }

            for (int i = 0; i < routeButtons.Count && i < 3; i++)
            {
                RoutePreference preference = (RoutePreference)i;
                routeButtons[i].onClick.AddListener(() => app.SetRoutePreference(preference));
            }

            EquipmentSlot[] compactEquipmentSlots =
            {
                EquipmentSlot.MainWeapon,
                EquipmentSlot.Chest,
                EquipmentSlot.Neck,
                EquipmentSlot.Earring1
            };
            for (int i = 0; i < equipSlotButtons.Count && i < compactEquipmentSlots.Length; i++)
            {
                EquipmentSlot slot = compactEquipmentSlots[i];
                equipSlotButtons[i].onClick.AddListener(() => app.CycleEquipment(activeHeroId, slot));
            }

            ShowPanel(0);
            Refresh();
        }

        public bool AssignHeroToSlot(string heroId, FormationPosition position)
        {
            if (app == null || string.IsNullOrWhiteSpace(heroId))
            {
                return false;
            }

            if (!app.AssignHeroToFormationSlot(heroId, position))
            {
                return false;
            }

            activeHeroId = heroId;
            Refresh();
            return true;
        }

        public bool UnequipHeroFromSlot(string heroId, FormationPosition position)
        {
            bool changed = app.UnequipHeroFromFormationSlot(heroId, position);
            if (!changed)
            {
                return false;
            }

            if (activeHeroId == heroId)
            {
                activeHeroId = app.State.Party.Heroes.FirstOrDefault(hero => hero.IsSelected)?.DefinitionId ?? heroId;
            }

            Refresh();
            return true;
        }

        private void SelectHero(int index)
        {
            if (index >= app.Catalog.Heroes.Count)
            {
                return;
            }

            string heroId = app.Catalog.Heroes[index].Id;
            app.SelectOrReplaceHero(heroId, activeHeroId);
            HeroState clicked = app.State.Party.GetHero(heroId);
            activeHeroId = clicked != null && clicked.IsSelected
                ? heroId
                : app.State.Party.Heroes.FirstOrDefault(hero => hero.IsSelected)?.DefinitionId ?? heroId;
            Refresh();
        }

        private void ShowPanel(int index)
        {
            activePanelIndex = index;
            for (int i = 0; i < panels.Count; i++)
            {
                panels[i].SetActive(i == index);
            }

            RefreshTabSprites();
            BringResetButtonForward();
        }

        private void SelectFormationPreset(int index)
        {
            RemapSelectedHeroesToFormation(index);
            activeFormationPreset = index;
            PlayUiSound(formationSelectClip);
            app.SavePartyChanges();
        }

        private void Refresh()
        {
            if (app == null)
            {
                return;
            }

            List<HeroState> selected = app.State.Party.Heroes.Where(hero => hero.IsSelected).ToList();
            partySummary.text = string.Empty;
            RefreshFormationSlots(selected);

            HeroState active = app.State.Party.GetHero(activeHeroId);
            skillSummary.text = active == null
                ? "Elegí un héroe"
                : $"{app.HeroName(activeHeroId)}\nActiva: {active.ActiveSkillId}\nPasiva: {active.PassiveSkillId}";

            SynergyResolver resolver = new SynergyResolver();
            List<TagSource> sources = selected.Select(hero =>
                new TagSource(hero.DefinitionId, app.Catalog.FindHero(hero.DefinitionId).TagIds)).ToList();
            IReadOnlyList<ActiveSynergy> synergies = resolver.Resolve(sources);
            synergySummary.text = synergies.Count == 0
                ? "No hay sinergias activas."
                : string.Join("\n", synergies.Select(item =>
                    $"{item.TagId.ToUpperInvariant()} · Nivel {item.Tier} ({item.SourceCount})"));

            inventorySummary.text = string.Empty;
            RefreshInventoryGrid();

            mapSummary.text =
                $"Nodo: {app.State.Expedition.CurrentNodeId}\n" +
                $"Completados: {app.State.Expedition.CompletedNodes}/{app.Catalog.Map.Nodes.Count}\n\n" +
                RouteDescription(app.State.Party.RoutePreference);
            mapUi?.Refresh(app);
            RefreshFormationButtonHighlight();
            for (int i = 0; i < routeButtons.Count && i < 3; i++)
            {
                bool selectedRoute = (int)app.State.Party.RoutePreference == i;
                routeButtons[i].image.sprite = selectedRoute && commandSelectedSprite != null
                    ? commandSelectedSprite
                    : commandNormalSprite;
                routeButtons[i].image.color = selectedRoute ? Color.white : new Color(1f, 1f, 1f, 0.88f);
            }

            settingsSummary.text =
                $"Idioma: {app.State.LanguageCode.ToUpperInvariant()}\n" +
                "Avisos: visuales y silenciosos\n" +
                "Progreso offline máximo: 8 horas";

            for (int i = 0; i < heroButtons.Count && i < app.Catalog.Heroes.Count; i++)
            {
                HeroDefinition definition = app.Catalog.Heroes[i];
                HeroState state = app.State.Party.GetHero(definition.Id);
                bool isHeroSelected = state != null && state.IsSelected;
                bool isActiveHero = definition.Id == activeHeroId;
                HeroClassCardView card = heroButtons[i].GetComponent<HeroClassCardView>();
                if (card != null)
                {
                    card.Refresh(app.HeroName(definition.Id), isHeroSelected, isActiveHero);
                }

                HeroDragSource dragSource = heroButtons[i].GetComponent<HeroDragSource>();
                if (dragSource != null)
                {
                    dragSource.Configure(
                        definition.Id,
                        HeroFormationIcon(definition.Id),
                        null);
                }

                heroButtons[i].image.color = Color.clear;
            }
        }

        private void RefreshInventoryGrid()
        {
            if (inventoryGrid == null && panels.Count > 3 && panels[3] != null)
            {
                inventoryGrid = panels[3].GetComponent<InventorySlotGridView>() ??
                    panels[3].AddComponent<InventorySlotGridView>();
            }

            inventoryGrid?.Refresh(app.Catalog, app.State.Inventory);
        }

        private string Marker(string heroId)
        {
            return heroId == activeHeroId ? ">" : "•";
        }

        private static string RouteDescription(RoutePreference preference)
        {
            switch (preference)
            {
                case RoutePreference.Loot:
                    return "Campaña que prioriza la obtención de botín";
                case RoutePreference.Challenge:
                    return "Ultra-violento: Desafío 100%";
                default:
                    return "El camino más seguro posible";
            }
        }

        private void LoadCommandSprites()
        {
            commandNormalSprite = Resources.Load<Sprite>("UI/Command");
            commandPressedSprite = Resources.Load<Sprite>("UI/CommandPressed");
            commandSelectedSprite = Resources.Load<Sprite>("UI/CommandSelected");
            formationSelectClip = Resources.Load<AudioClip>("Audio/UI/formation_select");
        }

        private static void PlayUiSound(AudioClip clip)
        {
            if (clip == null)
            {
                return;
            }

            AudioSource source = FindFirstObjectByType<AudioSource>();
            if (source != null)
            {
                source.PlayOneShot(clip, UiSoundVolume);
            }
        }

        private void ApplyCommandButtonStates()
        {
            IEnumerable<Button> commandButtons = tabButtons
                .Concat(new[] { cycleActiveSkillButton, cyclePassiveSkillButton })
                .Concat(equipSlotButtons)
                .Concat(routeButtons)
                .Concat(new[] { startExpeditionButton, resetExpeditionButton, languageButton, quitButton })
                .Where(button => button != null);

            foreach (Button button in commandButtons)
            {
                ApplyCommandButtonState(button);
            }

            ApplyResetButtonState();
            ApplyStartButtonState();
        }

        private void ApplyCommandButtonState(Button button)
        {
            Image image = button.image;
            if (image == null)
            {
                return;
            }

            if (commandNormalSprite != null)
            {
                image.sprite = commandNormalSprite;
            }

            image.type = Image.Type.Simple;
            image.preserveAspect = true;
            image.color = Color.white;
            button.targetGraphic = image;
            button.transition = Selectable.Transition.SpriteSwap;
            button.spriteState = new SpriteState
            {
                highlightedSprite = commandNormalSprite,
                pressedSprite = commandPressedSprite,
                selectedSprite = commandSelectedSprite,
                disabledSprite = commandNormalSprite
            };

            ColorBlock colors = button.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = Color.white;
            colors.pressedColor = Color.white;
            colors.selectedColor = Color.white;
            colors.disabledColor = new Color(1f, 1f, 1f, 0.42f);
            button.colors = colors;
        }

        private void ApplyResetButtonState()
        {
            if (resetExpeditionButton == null || resetExpeditionButton.image == null)
            {
                return;
            }

            resetExpeditionButton.image.color = new Color(0.82f, 0.12f, 0.12f);
            TMP_Text label = resetExpeditionButton.GetComponentInChildren<TMP_Text>();
            if (label != null)
            {
                label.color = Color.black;
            }

            BringResetButtonForward();
        }

        private void ApplyStartButtonState()
        {
            if (startExpeditionButton == null || startExpeditionButton.image == null)
            {
                return;
            }

            startExpeditionButton.image.color = new Color(0.72f, 1f, 0.68f, 1f);
            Image fill = startExpeditionButton.transform
                .Find("Start Expedition Fill")
                ?.GetComponent<Image>();
            if (fill != null)
            {
                fill.gameObject.SetActive(false);
            }

            TMP_Text label = startExpeditionButton.GetComponentInChildren<TMP_Text>();
            if (label != null)
            {
                label.color = new Color(1f, 0.92f, 0.08f, 1f);
            }
        }

        private void BringResetButtonForward()
        {
            if (resetExpeditionButton == null)
            {
                return;
            }

            resetExpeditionButton.gameObject.SetActive(true);
            resetExpeditionButton.transform.SetAsLastSibling();
        }

        private void RefreshTabSprites()
        {
            for (int i = 0; i < tabButtons.Count; i++)
            {
                Image image = tabButtons[i].image;
                if (image == null)
                {
                    continue;
                }

                image.sprite = i == activePanelIndex && commandSelectedSprite != null
                    ? commandSelectedSprite
                    : commandNormalSprite;
                image.color = i == activePanelIndex
                    ? Color.white
                    : new Color(0.78f, 0.78f, 0.78f, 0.92f);
            }
        }

        private void RemapSelectedHeroesToFormation(int nextPreset)
        {
            if (app == null || nextPreset == activeFormationPreset)
            {
                return;
            }

            FormationPosition[] currentPositions = FormationPreset(activeFormationPreset);
            FormationPosition[] nextPositions = FormationPreset(nextPreset);
            List<HeroState> selected = app.State.Party.Heroes.Where(hero => hero.IsSelected).ToList();
            for (int i = 0; i < currentPositions.Length && i < nextPositions.Length; i++)
            {
                HeroState hero = selected.FirstOrDefault(item => item.Position.Equals(currentPositions[i]));
                if (hero == null)
                {
                    continue;
                }

                hero.Position = nextPositions[i];
            }
        }

        private void RefreshFormationSlots(List<HeroState> selected)
        {
            FormationPosition[] activePositions = FormationPreset(activeFormationPreset);
            for (int i = 0; i < formationSlots.Count; i++)
            {
                bool slotEnabled = i < activePositions.Length;
                formationSlots[i].gameObject.SetActive(slotEnabled);
                if (!slotEnabled)
                {
                    continue;
                }

                formationSlots[i].SetPosition(activePositions[i], FormationSlotAnchoredPosition(activePositions[i]));
                formationSlots[i].SetFront(IsFrontSlot(activePositions[i]));
                HeroState occupant = selected.FirstOrDefault(hero => hero.Position.Equals(activePositions[i]));
                Sprite icon = occupant != null ? HeroFormationIcon(occupant.DefinitionId) : null;
                formationSlots[i].SetHero(icon, occupant != null ? occupant.DefinitionId : string.Empty);
            }
        }

        private void RefreshFormationButtonHighlight()
        {
            for (int i = 0; i < formationButtons.Count; i++)
            {
                Transform highlight = formationButtons[i].transform.Find("Highlight");
                if (highlight == null)
                {
                    continue;
                }

                highlight.gameObject.SetActive(i == activeFormationPreset);
            }
        }

        private static Vector2 FormationSlotAnchoredPosition(FormationPosition position)
        {
            const float startX = 84f;
            const float startY = -38f;
            const float gap = 60f;
            return new Vector2(startX + position.Column * gap, startY - position.Row * gap);
        }

        private Sprite HeroFormationIcon(string heroId)
        {
            for (int i = 0; i < app.Catalog.Heroes.Count && i < formationHeroIcons.Count; i++)
            {
                if (app.Catalog.Heroes[i].Id == heroId)
                {
                    return formationHeroIcons[i];
                }
            }

            return null;
        }

        private bool IsFrontSlot(FormationPosition position)
        {
            return position.Equals(FrontSlot(activeFormationPreset));
        }

        private static FormationPosition FrontSlot(int presetIndex)
        {
            switch (presetIndex)
            {
                case 1:
                    return new FormationPosition(1, 1);
                case 2:
                    return new FormationPosition(1, 2);
                default:
                    return new FormationPosition(1, 3);
            }
        }

        private static FormationPosition[] FormationPreset(int presetIndex)
        {
            switch (presetIndex)
            {
                case 1:
                    return new[]
                    {
                        new FormationPosition(1, 0),
                        new FormationPosition(0, 0),
                        new FormationPosition(2, 0),
                        new FormationPosition(1, 1)
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
    }
}
