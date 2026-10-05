using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using TMPro;
using TaskbarTactics.Content;
using TaskbarTactics.Core.Models;
using TaskbarTactics.Core.Stats;
using UnityEngine;
using UnityEngine.UI;

namespace TaskbarTactics.Presentation
{
    public sealed class ManagementUiController : MonoBehaviour
    {
        private const float UiSoundVolume = 0.65f;
        private static readonly Vector2 SkillTreePreviewCenter = new Vector2(320f, -320f);

        private enum SkillTreePreviewNodeKind
        {
            Square,
            Small,
            Circle
        }

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
        [SerializeField] private EquipmentPreviewLayoutView equipmentPreview;
        [SerializeField] private TMP_Text heroStatsSummary;
        [SerializeField] private TMP_Text heroIdentitySummary;
        [SerializeField] private List<Button> equipmentHeroTabs = new List<Button>();
        [SerializeField] private SilverCurrencyHud silverCurrencyHud;
        [SerializeField] private ItemTooltipView itemTooltip;

        [Header("Item rarity visuals")]
        [SerializeField, Range(0f, 1f), Tooltip("Opacity at the edges of non-Normal item slots.")]
        private float rarityGradientOpacity = 0.45f;
        [SerializeField, Range(0.05f, 1f), Tooltip("Inward reach as a fraction of the slot's half width.")]
        private float rarityGradientDepth = 0.7f;

        public void ConfigureItemTooltip(ItemTooltipView view) => itemTooltip = view;

        public void BindItemHover(Image icon, InventoryItem item)
        {
            ItemHoverTarget target = icon.GetComponent<ItemHoverTarget>() ??
                icon.gameObject.AddComponent<ItemHoverTarget>();
            target.Configure(itemTooltip, app.Catalog, item, app.State.LanguageCode);
            ItemRarityOverlay.Bind(icon, item, rarityGradientOpacity, rarityGradientDepth);
        }

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
        private Sprite menuCommandSprite;
        private AudioClip formationSelectClip;
        private static TMP_FontAsset shadowPixelTitleFont;
        private static Material shadowPixelTitleMaterial;

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
            tabButtons = navigation
                .Where(button => button != null && button.name != "Leyendas Tab")
                .ToList();
            panels = panelRoots
                .Where(panel => panel != null && panel.name != "Leyendas Panel")
                .ToList();
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
            EnsureSilverCurrencyHud();
            silverCurrencyHud?.Bind(app);
            tabButtons = tabButtons
                .Where(button => button != null && button.name != "Leyendas Tab")
                .ToList();
            panels = panels
                .Where(panel => panel != null && panel.name != "Leyendas Panel")
                .ToList();
            foreach (Transform child in GetComponentsInChildren<Transform>(true))
            {
                if (child.name == "Leyendas Tab" || child.name == "Leyendas Panel")
                {
                    child.gameObject.SetActive(false);
                }
            }
            ApplyInventoryPanelLayout();
            RemoveEquipmentPreviewLayoutBackground();
            EnsureHeroStatsSummary();
            RemoveMapSubLayoutBackground();
            ApplyMapCommandLayout();
            ApplyMapCommandLabels();
            ApplyMapSummaryPosition();
            ApplyCommandLayout();
            ApplyCommandLabels();
            ApplyTitleLayout();
            ApplyTitleFont();
            ApplyFormationSlotLayout();
            ApplyFormationClassFrameLayout();
            ApplyHeroClassButtonLayout();
            ApplyHeroClassContentLayout();
            EnsureTopRightControlsLayout();
            ApplyTopRightButtonOffset();
            ApplyUniformHudShadowDim();
            ApplyGlobalHudTorchLayout();
            HidePanelTorchPairs();
            EnsureNavigationChains();
            cycleActiveSkillButton?.gameObject.SetActive(false);
            cyclePassiveSkillButton?.gameObject.SetActive(false);
            LoadCommandSprites();
            ApplyCommandButtonStates();
            app.StateChanged += Refresh;
            closeButton.onClick.AddListener(window.ShowStrip);
            settingsShortcutButton?.onClick.AddListener(() => ShowPanel(4));
            quitShortcutButton?.onClick.AddListener(app.Quit);
            quitButton.onClick.AddListener(app.Quit);
            startExpeditionButton.onClick.AddListener(TryStartExpeditionFromSelectedAct);
            resetExpeditionButton?.onClick.AddListener(app.ResetExpeditionProgress);
            if (mapUi != null)
            {
                mapUi.ActSelectionChanged += ApplyStartButtonState;
            }

            cycleActiveSkillButton?.onClick.AddListener(() => app.CycleSkill(activeHeroId, false));
            cyclePassiveSkillButton?.onClick.AddListener(() => app.CycleSkill(activeHeroId, true));
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

        private void EnsureSilverCurrencyHud()
        {
            if (silverCurrencyHud == null)
            {
                silverCurrencyHud = GetComponentInChildren<SilverCurrencyHud>(true);
            }

            if (silverCurrencyHud == null)
            {
                Transform parent = panels.Count > 0 && panels[0] != null &&
                    panels[0].transform.parent != null
                    ? panels[0].transform.parent
                    : transform;
                GameObject currencyObject = new GameObject(
                    "Silver Currency HUD",
                    typeof(RectTransform));
                currencyObject.transform.SetParent(parent, false);
                silverCurrencyHud = currencyObject.AddComponent<SilverCurrencyHud>();
            }
        }

        private void RemoveMapSubLayoutBackground()
        {
            if (panels.Count <= 3 || panels[3] == null)
            {
                return;
            }

            Image mapPanelImage = panels[3].GetComponent<Image>();
            if (mapPanelImage == null)
            {
                return;
            }

            mapPanelImage.sprite = null;
            mapPanelImage.color = Color.clear;
            mapPanelImage.raycastTarget = false;
        }

        private void ApplyMapCommandLayout()
        {
            const float mapCommandWidth = 159.8f;
            const float mapCommandX = 64f;
            float[] routeYPositions = { -144f, -206f, -268f };

            for (int i = 0; i < routeButtons.Count && i < 3; i++)
            {
                RectTransform rect = routeButtons[i] != null
                    ? routeButtons[i].transform as RectTransform
                    : null;
                if (rect != null)
                {
                    rect.anchoredPosition = new Vector2(mapCommandX, routeYPositions[i]);
                    rect.sizeDelta = new Vector2(mapCommandWidth, rect.sizeDelta.y);
                }
            }

            if (startExpeditionButton != null)
            {
                RectTransform rect = startExpeditionButton.transform as RectTransform;
                if (rect != null)
                {
                    rect.anchoredPosition = new Vector2(mapCommandX, -322f);
                    rect.sizeDelta = new Vector2(mapCommandWidth, rect.sizeDelta.y);
                }
            }
        }

        private void ApplyMapCommandLabels()
        {
            const float routeFontSize = 15.84f;
            const float startFontSize = 20.592f;

            for (int i = 0; i < routeButtons.Count && i < 3; i++)
            {
                TMP_Text label = routeButtons[i]?.GetComponentInChildren<TMP_Text>(true);
                if (label != null)
                {
                    label.fontSize = routeFontSize;
                    label.enableAutoSizing = true;
                    label.fontSizeMin = 8f;
                    label.fontSizeMax = routeFontSize;
                    label.textWrappingMode = TextWrappingModes.NoWrap;
                    label.rectTransform.anchoredPosition = new Vector2(0f, 4f);
                }
            }

            TMP_Text startLabel = startExpeditionButton?.GetComponentInChildren<TMP_Text>(true);
            if (startLabel != null)
            {
                startLabel.fontSize = startFontSize;
                startLabel.enableAutoSizing = true;
                startLabel.fontSizeMin = 10f;
                startLabel.fontSizeMax = startFontSize;
                startLabel.textWrappingMode = TextWrappingModes.NoWrap;
                startLabel.rectTransform.anchoredPosition = new Vector2(0f, 4f);
            }
        }

        private void ApplyMapSummaryPosition()
        {
            if (mapSummary == null)
            {
                return;
            }

            RectTransform rect = mapSummary.rectTransform;
            rect.anchoredPosition = new Vector2(rect.anchoredPosition.x, -62f);
        }

        private void ApplyTitleFont()
        {
            if (shadowPixelTitleFont == null)
            {
                Font sourceFont = Resources.Load<Font>("UI/Fonts/ShadowPixel-Regular-v3");
                if (sourceFont != null)
                {
                    shadowPixelTitleFont = TMP_FontAsset.CreateFontAsset(sourceFont);
                }
            }

            if (shadowPixelTitleFont == null)
            {
                return;
            }

            TMP_Text[] texts = GetComponentsInChildren<TMP_Text>(true);
            foreach (TMP_Text text in texts)
            {
                if (text != null && text.name == "Title")
                {
                    text.font = shadowPixelTitleFont;
                    text.fontStyle = FontStyles.Bold;
                    text.color = Color.white;
                    if (shadowPixelTitleMaterial == null)
                    {
                        shadowPixelTitleMaterial = new Material(shadowPixelTitleFont.material)
                        {
                            name = "ShadowPixel Title Material"
                        };
                        shadowPixelTitleMaterial.SetColor("_FaceColor", new Color(0.82f, 0.96f, 0.16f, 1f));
                        shadowPixelTitleMaterial.SetColor("_OutlineColor", new Color(0.48f, 0.04f, 0.08f, 1f));
                        shadowPixelTitleMaterial.SetFloat("_OutlineWidth", 0.08f);
                    }

                    text.fontMaterial = shadowPixelTitleMaterial;
                    text.enableAutoSizing = false;
                    text.textWrappingMode = TextWrappingModes.NoWrap;
                }
            }
        }

        private void ApplyTitleLayout()
        {
            RectTransform titleFrame = null;
            RectTransform title = null;
            RectTransform[] rects = GetComponentsInChildren<RectTransform>(true);
            foreach (RectTransform rect in rects)
            {
                if (rect.name == "Title Frame")
                {
                    titleFrame = rect;
                }
                else if (rect.name == "Title")
                {
                    title = rect;
                }
            }

            if (titleFrame != null)
            {
                titleFrame.gameObject.SetActive(false);
            }

            if (title != null)
            {
                title.gameObject.SetActive(false);
            }
        }

        private void EnsureTopRightControlsLayout()
        {
            if (closeButton == null || closeButton.transform.parent == null)
            {
                return;
            }

            Transform parent = closeButton.transform.parent;
            Transform existing = parent.Find("Top Right Controls Layout");
            Image layout = existing != null ? existing.GetComponent<Image>() : null;
            if (layout == null)
            {
                GameObject layoutObject = new GameObject(
                    "Top Right Controls Layout",
                    typeof(RectTransform),
                    typeof(CanvasRenderer),
                    typeof(Image));
                layoutObject.transform.SetParent(parent, false);
                layout = layoutObject.GetComponent<Image>();
            }

            layout.sprite = Resources.Load<Sprite>("UI/TopRightControlsLayout");
            layout.type = Image.Type.Simple;
            layout.preserveAspect = false;
            layout.raycastTarget = false;
            RectTransform rect = layout.rectTransform;
            rect.anchorMin = rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0f, 1f);
            rect.anchoredPosition = new Vector2(830f, -8f);
            rect.sizeDelta = new Vector2(115.967f, 41.584f);
            layout.transform.SetAsFirstSibling();
            layout.gameObject.SetActive(false);
        }

        private void ApplyTopRightButtonOffset()
        {
            Button[] buttons = { closeButton, settingsShortcutButton, quitShortcutButton };
            float[] xPositions = { 831f, 870f, 909f };
            for (int i = 0; i < buttons.Length; i++)
            {
                Button button = buttons[i];
                RectTransform rect = button != null ? button.transform as RectTransform : null;
                if (rect != null)
                {
                    rect.anchoredPosition = new Vector2(xPositions[i], -10f);
                    rect.localScale = new Vector3(0.8f, 0.8f, 1f);
                }
            }
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
            itemTooltip?.Hide();
            if (activePanelIndex != index)
            {
                InventorySlotGridView.ClearActiveDragVisuals();
                EquipmentPreviewLayoutView.ClearActiveDragVisuals();
            }

            activePanelIndex = index;
            for (int i = 0; i < panels.Count; i++)
            {
                panels[i].SetActive(i == index);
            }

            if (index == 1)
            {
                ApplySkillTreeInitialView();
            }

            RefreshTabSprites();
            BringResetButtonForward();
            Refresh();
        }

        public void ShowMapAct(int actNumber)
        {
            ShowPanel(3);
            mapUi?.SelectAct(actNumber);
        }

        public bool TryEquipInventoryItem(string itemInstanceId, EquipmentSlot? preferredSlot)
        {
            bool equipped = app != null &&
                app.TryEquipInventoryItem(activeHeroId, itemInstanceId, preferredSlot);
            if (equipped)
            {
                Refresh();
            }

            return equipped;
        }

        public bool TryUnequipItemToInventory(string heroId, EquipmentSlot slot)
        {
            if (app == null || inventoryGrid == null ||
                !inventoryGrid.HasVisibleSpace(app.Catalog, app.State.Inventory, app.State.Party.Heroes))
            {
                Refresh();
                return false;
            }

            bool unequipped = app.UnequipInventoryItem(heroId, slot);
            if (unequipped)
            {
                activeHeroId = heroId;
                Refresh();
            }

            return unequipped;
        }

        private void TryStartExpeditionFromSelectedAct()
        {
            if (mapUi != null && mapUi.IsActTwoSelected && !mapUi.IsActTwoUnlocked)
            {
                Refresh();
                return;
            }

            app.StartExpedition(mapUi != null && mapUi.IsActTwoSelected ? 2 : 1);
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

            ApplyCloseButtonState();

            List<HeroState> selected = app.State.Party.Heroes.Where(hero => hero.IsSelected).ToList();
            partySummary.text = string.Empty;
            RefreshFormationSlots(selected);

            skillSummary.text = string.Empty;

            synergySummary?.SetText(string.Empty);

            inventorySummary.text = string.Empty;
            RefreshInventoryGrid();
            RefreshEquipmentPreview();
            RefreshEquipmentHeroTabs();
            RefreshHeroStatsSummary();
            ApplyInventorySubmenuVisibility();
            ApplyGlobalHudTorchLayout();
            HidePanelTorchPairs();

            mapSummary.text =
                $"{app.State.Expedition.CurrentNodeId}\n" +
                $"Explored {app.State.Expedition.CompletedNodes}/{app.Catalog.Map.Nodes.Count}";
            mapUi?.Refresh(app);
            ApplyStartButtonState();
            RefreshFormationButtonHighlight();
            for (int i = 0; i < routeButtons.Count && i < 3; i++)
            {
                bool selectedRoute = (int)app.State.Party.RoutePreference == i;
                routeButtons[i].image.sprite = selectedRoute && commandSelectedSprite != null
                    ? commandSelectedSprite
                    : commandNormalSprite;
                routeButtons[i].image.color = selectedRoute ? Color.white : new Color(1f, 1f, 1f, 0.88f);
                TMP_Text label = routeButtons[i].GetComponentInChildren<TMP_Text>(true);
                if (label != null)
                {
                    label.text = i == 0 ? "Easy" : i == 1 ? "Normal" : "Hard";
                    label.color = RouteDifficultyColor(i);
                }
            }

            settingsSummary.text =
                $"Idioma: {app.State.LanguageCode.ToUpperInvariant()}\n" +
                "Avisos: visuales y silenciosos\n" +
                "Progreso offline máximo: 8 horas";

            bool partyIsFull = app.State.Party.Heroes.Count(hero => hero.IsSelected) >= GameAppController.PartySize;
            bool dimUnselectedClassIcons = app.State.Expedition.IsActive && partyIsFull;
            for (int i = 0; i < heroButtons.Count && i < app.Catalog.Heroes.Count; i++)
            {
                HeroDefinition definition = app.Catalog.Heroes[i];
                HeroState state = app.State.Party.GetHero(definition.Id);
                bool isHeroSelected = state != null && state.IsSelected;
                bool isActiveHero = definition.Id == activeHeroId;
                HeroClassCardView card = heroButtons[i].GetComponent<HeroClassCardView>();
                if (card != null)
                {
                    card.Refresh(
                        PartyClassDisplayName(definition.Id, app.HeroName(definition.Id)),
                        isHeroSelected,
                        isActiveHero,
                        dimUnselectedClassIcons,
                        HeroTabTextColor(definition.Id, true));
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
            if (inventoryGrid == null && panels.Count > 2 && panels[2] != null)
            {
                inventoryGrid = panels[2].GetComponent<InventorySlotGridView>() ??
                    panels[2].AddComponent<InventorySlotGridView>();
            }

            inventoryGrid?.SetOwner(this);
            inventoryGrid?.Refresh(app.Catalog, app.State.Inventory, app.State.Party.Heroes);
        }

        private void RefreshEquipmentPreview()
        {
            if (equipmentPreview == null && panels.Count > 2 && panels[2] != null)
            {
                equipmentPreview = panels[2].GetComponentInChildren<EquipmentPreviewLayoutView>(true);
            }

            equipmentPreview?.SetOwner(this);
            ApplyEquipmentPreviewPosition();
            equipmentPreview?.RefreshHero(activeHeroId);
            equipmentPreview?.RefreshEquippedItems(
                app.Catalog,
                app.State.Inventory,
                app.State.Party.GetHero(activeHeroId));
        }

        private void EnsureHeroStatsSummary()
        {
            if (heroStatsSummary != null || panels.Count <= 2 || panels[2] == null)
            {
                return;
            }

            Transform existing = panels[2].transform.Find("Hero Stats Summary");
            heroStatsSummary = existing != null
                ? existing.GetComponent<TMP_Text>()
                : null;
            if (heroStatsSummary == null)
            {
                GameObject statsObject = new GameObject(
                    "Hero Stats Summary",
                    typeof(RectTransform),
                    typeof(CanvasRenderer),
                    typeof(TextMeshProUGUI));
                statsObject.transform.SetParent(panels[2].transform, false);
                heroStatsSummary = statsObject.GetComponent<TextMeshProUGUI>();
            }

            heroStatsSummary.font = inventorySummary != null
                ? inventorySummary.font
                : heroStatsSummary.font;
            heroStatsSummary.fontSize = 13f;
            heroStatsSummary.color = new Color(0.96f, 0.98f, 1f, 1f);
            heroStatsSummary.alignment = TextAlignmentOptions.TopLeft;
            heroStatsSummary.textWrappingMode = TextWrappingModes.NoWrap;
            heroStatsSummary.overflowMode = TextOverflowModes.Overflow;
            heroStatsSummary.raycastTarget = false;

            RectTransform rect = heroStatsSummary.rectTransform;
            rect.anchorMin = rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = new Vector2(740f, -122f);
            rect.sizeDelta = new Vector2(220f, 220f);
            heroStatsSummary.transform.SetAsLastSibling();
        }

        private void EnsureHeroIdentitySummary()
        {
            if (heroIdentitySummary != null || panels.Count <= 2 || panels[2] == null)
            {
                return;
            }

            Transform existing = panels[2].transform.Find("Hero Identity Summary");
            heroIdentitySummary = existing != null
                ? existing.GetComponent<TMP_Text>()
                : null;
            if (heroIdentitySummary == null)
            {
                GameObject identityObject = new GameObject(
                    "Hero Identity Summary",
                    typeof(RectTransform),
                    typeof(CanvasRenderer),
                    typeof(TextMeshProUGUI));
                identityObject.transform.SetParent(panels[2].transform, false);
                heroIdentitySummary = identityObject.GetComponent<TextMeshProUGUI>();
            }

            heroIdentitySummary.font = inventorySummary != null
                ? inventorySummary.font
                : heroIdentitySummary.font;
            heroIdentitySummary.fontSize = 15f;
            heroIdentitySummary.fontStyle = FontStyles.Bold;
            heroIdentitySummary.color = new Color(0.96f, 0.98f, 1f, 1f);
            heroIdentitySummary.alignment = TextAlignmentOptions.Center;
            heroIdentitySummary.textWrappingMode = TextWrappingModes.NoWrap;
            heroIdentitySummary.overflowMode = TextOverflowModes.Overflow;
            heroIdentitySummary.raycastTarget = false;

            RectTransform rect = heroIdentitySummary.rectTransform;
            rect.anchorMin = rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = new Vector2(460f, -240f);
            rect.sizeDelta = new Vector2(250f, 30f);
            heroIdentitySummary.transform.SetAsLastSibling();
        }

        private void RefreshHeroStatsSummary()
        {
            EnsureHeroStatsSummary();
            EnsureHeroIdentitySummary();
            if (heroStatsSummary == null || heroIdentitySummary == null)
            {
                return;
            }

            HeroState hero = app.State.Party.GetHero(activeHeroId);
            if (hero == null)
            {
                heroStatsSummary.text = string.Empty;
                heroIdentitySummary.text = string.Empty;
                return;
            }

            HeroStats stats = app.Catalog.ResolveHeroStats(hero, app.State.Inventory);
            string heroDisplayName =
                PartyClassDisplayName(hero.DefinitionId, app.HeroName(hero.DefinitionId)).ToUpperInvariant();
            if (hero.DefinitionId == "magic_warrior")
            {
                heroDisplayName = $"<size=14>{heroDisplayName}</size>";
            }

            heroIdentitySummary.text =
                $"{heroDisplayName}  Lvl. {hero.Level}";
            heroStatsSummary.text =
                $"HP {stats.MaxHealth.ToString(CultureInfo.InvariantCulture)}\n" +
                $"MP {stats.MaxMana.ToString(CultureInfo.InvariantCulture)}\n" +
                $"ATK {FormatStat(stats.AttackPower)}\n" +
                $"SPELL {FormatStat(stats.SpellPower)}\n" +
                $"DEF {FormatStat(stats.Defense)}\n" +
                $"M.RES {FormatStat(stats.MagicResistance)}\n" +
                $"ASPD {FormatStat(stats.AttackSpeed)}\n" +
                $"C.SPD {FormatStat(stats.CastSpeed)}\n" +
                $"CRIT {FormatStat(stats.CriticalChance)}%\n" +
                $"EVA {FormatStat(stats.Evasion)}%";
            bool showInventory = activePanelIndex == 2;
            heroStatsSummary.gameObject.SetActive(showInventory);
            heroIdentitySummary.gameObject.SetActive(showInventory);
        }

        private static string FormatStat(float value)
        {
            return value.ToString("0.##", CultureInfo.InvariantCulture);
        }

        private static Color RouteDifficultyColor(int routeIndex)
        {
            switch (routeIndex)
            {
                case 0:
                    return new Color(0.35f, 0.95f, 0.35f, 1f);
                case 1:
                    return new Color(1f, 0.9f, 0.1f, 1f);
                default:
                    return new Color(1f, 0.32f, 0.03f, 1f);
            }
        }

        private void RemoveEquipmentPreviewLayoutBackground()
        {
            if (panels.Count <= 2 || panels[2] == null)
            {
                return;
            }

            Transform layoutTransform = panels[2].transform.Find("Equipment Preview Layout");
            Image layout = layoutTransform != null
                ? layoutTransform.GetComponent<Image>()
                : null;
            if (layout == null)
            {
                return;
            }

            layout.sprite = null;
            layout.color = Color.clear;
            layout.raycastTarget = true;
        }

        private void ApplyEquipmentPreviewPosition()
        {
            if (equipmentPreview == null)
            {
                return;
            }

            RectTransform rect = equipmentPreview.transform as RectTransform;
            if (rect != null)
            {
                rect.anchoredPosition = new Vector2(460f, -145f);
            }
        }

        private void RefreshEquipmentHeroTabs()
        {
            EnsureEquipmentHeroTabs();
            ApplyEquipmentHeroTabLayout();
            for (int i = 0; i < equipmentHeroTabs.Count && i < app.Catalog.Heroes.Count; i++)
            {
                HeroDefinition definition = app.Catalog.Heroes[i];
                Button button = equipmentHeroTabs[i];
                bool selected = definition.Id == activeHeroId;
                if (button.image != null)
                {
                    button.image.color = selected
                        ? new Color(0.52f, 0.08f, 0.08f, 0.78f)
                        : new Color(0.05f, 0.04f, 0.04f, 0.62f);
                }

                TMP_Text label = button.GetComponentInChildren<TMP_Text>(true);
                if (label != null)
                {
                    label.text = ShortHeroLabel(definition.Id);
                    label.color = HeroTabTextColor(definition.Id, selected);
                    label.fontStyle = selected ? FontStyles.Bold : FontStyles.Normal;
                }
            }
        }

        private void ApplyInventorySubmenuVisibility()
        {
            bool showInventoryOnlyViews = activePanelIndex == 2;

            if (equipmentPreview != null)
            {
                equipmentPreview.gameObject.SetActive(showInventoryOnlyViews);
            }

            foreach (Button tab in equipmentHeroTabs)
            {
                if (tab != null)
                {
                    tab.gameObject.SetActive(showInventoryOnlyViews);
                }
            }
        }

        private void ApplyCommandLayout()
        {
            const float commandStartY = 150f;
            const float commandSpacing = 62f;
            const float commandWidth = 150.4f;
            const float commandHeight = 54.4f;

            for (int i = 0; i < tabButtons.Count; i++)
            {
                Button button = tabButtons[i];
                RectTransform rect = button != null ? button.transform as RectTransform : null;
                if (rect != null)
                {
                    rect.anchorMin = rect.anchorMax = new Vector2(0f, 1f);
                    rect.pivot = new Vector2(0f, 1f);
                    rect.anchoredPosition = new Vector2(33f, -(commandStartY + i * commandSpacing));
                    rect.sizeDelta = new Vector2(commandWidth, commandHeight);
                }
            }
        }

        private void ApplyCommandLabels()
        {
            string[] labels = { "Party", "Skills", "Inventory", "Map" };
            for (int i = 0; i < tabButtons.Count && i < labels.Length; i++)
            {
                TMP_Text label = tabButtons[i]?.GetComponentInChildren<TMP_Text>(true);
                if (label != null)
                {
                    label.text = labels[i];
                    if (i == 2)
                    {
                        label.fontSize = 15f;
                    }
                }
            }
        }

        private void ApplyInventoryPanelLayout()
        {
            if (panels.Count <= 2 || panels[2] == null)
            {
                return;
            }

            Image panelImage = panels[2].GetComponent<Image>();
            Sprite equipLayout = Resources.Load<Sprite>("UI/EquipLayout");
            RectTransform panelRect = panels[2].transform as RectTransform;
            if (panelImage == null || panelRect == null || equipLayout == null)
            {
                return;
            }

            panelImage.sprite = equipLayout;
            panelImage.type = Image.Type.Sliced;
            panelImage.preserveAspect = false;
            panelRect.anchoredPosition = new Vector2(panelRect.anchoredPosition.x, -18f);
            if (panelRect.sizeDelta.y > -150f)
            {
                panelRect.sizeDelta += Vector2.up * (-panelRect.rect.height * 0.15f);
            }

            ApplyInventoryBackgroundScale(panelImage, panelRect);
            HideInventoryItemListFrame(panelRect);
        }

        private static void HideInventoryItemListFrame(RectTransform panelRect)
        {
            Transform layoutTransform = panelRect.Find("Inventory Layout Visual");
            if (layoutTransform != null)
            {
                layoutTransform.gameObject.SetActive(false);
            }
        }

        private static void ApplyInventoryBackgroundScale(Image panelImage, RectTransform panelRect)
        {
            Transform existing = panelRect.Find("Inventory Layout Visual");
            Image layout = existing != null
                ? existing.GetComponent<Image>()
                : null;
            if (layout == null)
            {
                GameObject layoutObject = new GameObject(
                    "Inventory Layout Visual",
                    typeof(RectTransform),
                    typeof(CanvasRenderer),
                    typeof(Image));
                layoutObject.transform.SetParent(panelRect, false);
                layout = layoutObject.GetComponent<Image>();
            }

            layout.sprite = panelImage.sprite;
            layout.type = Image.Type.Sliced;
            layout.preserveAspect = false;
            layout.color = panelImage.color;
            layout.raycastTarget = false;
            RectTransform layoutRect = layout.rectTransform;
            layoutRect.anchorMin = layoutRect.anchorMax = new Vector2(0.5f, 0.5f);
            layoutRect.pivot = new Vector2(0.5f, 0.5f);
            layoutRect.anchoredPosition = new Vector2(0f, 5f);
            layoutRect.sizeDelta = panelRect.rect.size * 0.9f;
            layout.transform.SetAsFirstSibling();
            panelImage.enabled = false;
            layout.gameObject.SetActive(true);
        }

        private void EnsureLegendBookRing()
        {
            if (panels.Count <= 2 || panels[2] == null)
            {
                return;
            }

            Transform panel = panels[2].transform;
            HideLegacyLegendBooks(panel);
            TMP_Text title = panel.Find("Panel Title")?.GetComponent<TMP_Text>();
            if (title != null)
            {
                title.gameObject.SetActive(false);
            }

            Transform existing = panel.Find("Legend Book Ring");
            if (existing != null)
            {
                ArrangeLegendBookRing(existing as RectTransform);
                return;
            }

            GameObject ring = new GameObject("Legend Book Ring", typeof(RectTransform));
            ring.transform.SetParent(panel, false);
            RectTransform ringRect = ring.GetComponent<RectTransform>();
            ArrangeLegendBookRing(ringRect);

            Sprite slotSprite = LoadLegendSprite("bslot");
            Vector2[] positions =
            {
                new Vector2(0f, 118f),
                new Vector2(116f, 72f),
                new Vector2(144f, -30f),
                new Vector2(64f, -112f),
                new Vector2(-64f, -112f),
                new Vector2(-144f, -30f),
                new Vector2(-116f, 72f)
            };

            for (int i = 0; i < positions.Length; i++)
            {
                CreateLegendBook(ring.transform, i, positions[i], slotSprite);
            }
        }

        private static void ArrangeLegendBookRing(RectTransform ringRect)
        {
            if (ringRect == null)
            {
                return;
            }

            ringRect.anchorMin = ringRect.anchorMax = new Vector2(0.5f, 0.5f);
            ringRect.pivot = new Vector2(0.5f, 0.5f);
            ringRect.anchoredPosition = new Vector2(-70f, -2f);
            ringRect.sizeDelta = new Vector2(360f, 300f);
        }

        private static void HideLegacyLegendBooks(Transform panel)
        {
            if (panel == null)
            {
                return;
            }

            for (int i = 0; i < panel.childCount; i++)
            {
                Transform child = panel.GetChild(i);
                if (child.name.StartsWith("Legend Book ", System.StringComparison.Ordinal) &&
                    child.name != "Legend Book Ring")
                {
                    child.gameObject.SetActive(false);
                }
            }
        }

        private static void CreateLegendBook(Transform parent, int index, Vector2 position, Sprite slotSprite)
        {
            GameObject root = new GameObject($"Legend Book {index}", typeof(RectTransform));
            root.transform.SetParent(parent, false);
            RectTransform rootRect = root.GetComponent<RectTransform>();
            rootRect.anchorMin = rootRect.anchorMax = new Vector2(0.5f, 0.5f);
            rootRect.pivot = new Vector2(0.5f, 0.5f);
            rootRect.anchoredPosition = position;
            rootRect.sizeDelta = new Vector2(82f, 82f);

            Image slot = CreateRuntimeImage(root.transform, "Book Slot", slotSprite);
            slot.type = slotSprite != null ? Image.Type.Sliced : Image.Type.Simple;
            slot.preserveAspect = false;
            slot.rectTransform.sizeDelta = new Vector2(76f, 76f);

            Image book = CreateRuntimeImage(
                root.transform,
                "Book Icon",
                LoadLegendSprite($"b{index}"));
            book.preserveAspect = true;
            book.rectTransform.anchoredPosition = new Vector2(0f, 2f);
            book.rectTransform.sizeDelta = new Vector2(58f, 68f);

            Image labelBackground = CreateRuntimeImage(root.transform, "Tooltip Background", null);
            labelBackground.color = new Color(0f, 0f, 0f, 0.55f);
            labelBackground.rectTransform.anchoredPosition = new Vector2(0f, -52f);
            labelBackground.rectTransform.sizeDelta = new Vector2(112f, 30f);
            labelBackground.gameObject.SetActive(false);

            TMP_Text label = CreateRuntimeText(root.transform, "Label", LegendBookName(index));
            label.rectTransform.anchoredPosition = new Vector2(0f, -52f);
            label.rectTransform.sizeDelta = new Vector2(112f, 24f);
            label.gameObject.SetActive(false);

            Image hoverArea = CreateRuntimeImage(root.transform, "Hover Area", null);
            hoverArea.color = new Color(1f, 1f, 1f, 0f);
            hoverArea.raycastTarget = true;
            hoverArea.rectTransform.sizeDelta = new Vector2(82f, 82f);
            MapNodeHoverTooltip tooltip = hoverArea.gameObject.AddComponent<MapNodeHoverTooltip>();
            tooltip.Configure(label, labelBackground.gameObject);
        }

        private static string LegendBookName(int index)
        {
            switch (index)
            {
                case 0: return "Player";
                case 1: return "Guardian";
                case 2: return "Rogue";
                case 3: return "Spell Blade";
                case 4: return "Cleric";
                case 5: return "Archer";
                case 6: return "Mage";
                default: return string.Empty;
            }
        }

        private static Sprite LoadLegendSprite(string spriteName)
        {
            Sprite sprite = Resources.Load<Sprite>($"UI/Legends/{spriteName}");
            if (sprite != null)
            {
                return sprite;
            }

            Texture2D texture = Resources.Load<Texture2D>($"UI/Legends/{spriteName}");
            return texture == null
                ? null
                : Sprite.Create(
                    texture,
                    new Rect(0f, 0f, texture.width, texture.height),
                    new Vector2(0.5f, 0.5f),
                    100f);
        }

        private static Image CreateRuntimeImage(Transform parent, string name, Sprite sprite)
        {
            GameObject imageObject = new GameObject(
                name,
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(Image));
            imageObject.transform.SetParent(parent, false);
            Image image = imageObject.GetComponent<Image>();
            image.sprite = sprite;
            image.color = sprite != null ? Color.white : Color.clear;
            image.raycastTarget = false;
            RectTransform rect = image.rectTransform;
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = Vector2.zero;
            return image;
        }

        private static TMP_Text CreateRuntimeText(Transform parent, string name, string value)
        {
            GameObject textObject = new GameObject(
                name,
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(TextMeshProUGUI));
            textObject.transform.SetParent(parent, false);
            TextMeshProUGUI label = textObject.GetComponent<TextMeshProUGUI>();
            label.font = Resources.Load<TMP_FontAsset>("UI/Fonts/VCR_OSD_MONO SDF");
            label.text = value;
            label.fontSize = 10f;
            label.alignment = TextAlignmentOptions.Center;
            label.color = new Color(1f, 0.95f, 0.78f, 1f);
            label.raycastTarget = false;
            RectTransform rect = label.rectTransform;
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = Vector2.zero;
            return label;
        }

        private void ApplySkillTreeInitialView()
        {
            if (panels.Count <= 1 || panels[1] == null)
            {
                return;
            }

            EnsureSkillTreeFrame(panels[1].transform);
            RectTransform viewportRect = panels[1].transform
                .Find("Skill Tree Viewport") as RectTransform;
            if (viewportRect != null)
            {
                viewportRect.offsetMin = new Vector2(18f, 24f);
                viewportRect.offsetMax = new Vector2(-18f, -24f);
            }
            DraggableMapView skillTreeView = panels[1].transform
                .Find("Skill Tree Viewport")
                ?.GetComponent<DraggableMapView>();
            RectTransform content = panels[1].transform
                .Find("Skill Tree Viewport/Skill Tree Content") as RectTransform;
            EnsureInitialSkillTreeNodes(content);
            EnsureSkillTreeCanvasSize(content);
            skillTreeView?.ConfigureCentered(
                content,
                0.72f,
                0.35f,
                1.8f,
                true,
                new Vector2(420f, 420f),
                true);

            RectTransform nodeLayer = content?.Find("Interactive Skill Tree Nodes") as RectTransform;
            skillTreeView?.CenterOnContentPoint(SkillTreeNodeCenter(nodeLayer), 0.72f);
        }

        private static Vector2 SkillTreeNodeCenter(RectTransform nodeLayer)
        {
            if (nodeLayer == null || nodeLayer.childCount == 0)
            {
                return SkillTreePreviewCenter;
            }

            float minX = float.PositiveInfinity;
            float maxX = float.NegativeInfinity;
            float minY = float.PositiveInfinity;
            float maxY = float.NegativeInfinity;
            bool foundNode = false;

            for (int i = 0; i < nodeLayer.childCount; i++)
            {
                Transform child = nodeLayer.GetChild(i);
                if (child == null || child.name.Contains("Connector"))
                {
                    continue;
                }

                RectTransform rect = child as RectTransform;
                if (rect == null)
                {
                    continue;
                }

                Vector2 position = rect.anchoredPosition;
                Vector2 halfSize = rect.rect.size * 0.5f;
                minX = Mathf.Min(minX, position.x - halfSize.x);
                maxX = Mathf.Max(maxX, position.x + halfSize.x);
                minY = Mathf.Min(minY, position.y - halfSize.y);
                maxY = Mathf.Max(maxY, position.y + halfSize.y);
                foundNode = true;
            }

            return foundNode
                ? new Vector2((minX + maxX) * 0.5f, (minY + maxY) * 0.5f)
                : SkillTreePreviewCenter;
        }

        private static void EnsureSkillTreeCanvasSize(RectTransform content)
        {
            if (content == null)
            {
                return;
            }

            Vector2 minimumSize = new Vector2(1100f, 800f);
            content.sizeDelta = new Vector2(
                Mathf.Max(content.sizeDelta.x, minimumSize.x),
                Mathf.Max(content.sizeDelta.y, minimumSize.y));

            RectTransform nodeLayer = content.Find("Interactive Skill Tree Nodes") as RectTransform;
            if (nodeLayer != null)
            {
                nodeLayer.anchoredPosition = Vector2.zero;
                nodeLayer.sizeDelta = content.sizeDelta;
            }
        }

        private void EnsureInitialSkillTreeNodes(RectTransform content)
        {
            if (content == null || content.Find("Interactive Skill Tree Nodes") != null)
            {
                return;
            }

            Transform image = content.Find("Skill Tree Image");
            if (image != null)
            {
                image.gameObject.SetActive(false);
            }

            GameObject nodeLayer = new GameObject("Interactive Skill Tree Nodes", typeof(RectTransform));
            nodeLayer.transform.SetParent(content, false);
            RectTransform layerRect = nodeLayer.GetComponent<RectTransform>();
            layerRect.anchorMin = layerRect.anchorMax = new Vector2(0f, 1f);
            layerRect.pivot = new Vector2(0f, 1f);
            layerRect.anchoredPosition = Vector2.zero;
            layerRect.sizeDelta = content.sizeDelta;

            CreateSkillTreeNode(layerRect, "HUD/User", LoadSkillTreeSprite("UI/SkillTree/Skill1"), new Vector2(320f, -238f), true);
            CreateSkillTreeNode(layerRect, "Mage", Resources.Load<Sprite>("HeroClasses/pyromancer"), new Vector2(268f, -274f), false);
            CreateSkillTreeNode(layerRect, "Cleric", Resources.Load<Sprite>("HeroClasses/cleric"), new Vector2(372f, -274f), false);
            CreateSkillTreeNode(layerRect, "Spellblade", Resources.Load<Sprite>("HeroClasses/spellblade"), new Vector2(216f, -320f), false);
            CreateSkillTreeNode(layerRect, "Archer", Resources.Load<Sprite>("HeroClasses/ranger"), new Vector2(424f, -320f), false);
            CreateSkillTreeNode(layerRect, "Warrior", Resources.Load<Sprite>("HeroClasses/guardian"), new Vector2(268f, -366f), false);
            CreateSkillTreeNode(layerRect, "Rogue", Resources.Load<Sprite>("HeroClasses/rogue"), new Vector2(372f, -366f), false);
        }

        private void CreateSkillTreeNode(
            RectTransform parent,
            string displayName,
            Sprite icon,
            Vector2 position,
            bool isHudNode)
        {
            GameObject node = new GameObject(
                $"Skill Node {displayName}",
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(Image),
                typeof(Button),
                typeof(SkillTreeNodeView));
            node.transform.SetParent(parent, false);
            RectTransform rect = node.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = position;
            rect.sizeDelta = new Vector2(58f, 58f);

            SkillTreeNodeView view = node.GetComponent<SkillTreeNodeView>();
            view.Configure(icon, displayName);

            Vector2 direction = position - SkillTreePreviewCenter;
            if (direction.sqrMagnitude < 0.01f)
            {
                direction = Vector2.up;
            }

            direction.Normalize();
            SkillTreePreviewNodeKind firstNodeKind = isHudNode
                ? SkillTreePreviewNodeKind.Small
                : SkillTreePreviewNodeKind.Square;
            string pathId = displayName.Replace("/", "-");
            node.GetComponent<Button>().onClick.AddListener(() =>
                CreateSkillTreePreviewNode(parent, rect, pathId, direction, firstNodeKind, 0));

            if (!isHudNode)
            {
                CreateSkillTreePreviewNode(
                    parent,
                    rect,
                    pathId,
                    direction,
                    SkillTreePreviewNodeKind.Square,
                    0);
            }
        }

        private RectTransform CreateSkillTreePreviewNode(
            RectTransform parent,
            RectTransform sourceNode,
            string pathId,
            Vector2 direction,
            SkillTreePreviewNodeKind nodeKind,
            int depth)
        {
            string nodeName = $"Skill Preview {pathId} {depth} {nodeKind}";
            Transform existingNode = parent.Find(nodeName);
            if (existingNode != null)
            {
                return existingNode as RectTransform;
            }

            float spacing = GetSkillTreePreviewSpacing(nodeKind);
            Vector2 nodePosition = sourceNode.anchoredPosition + direction * spacing;
            CreateSkillTreeConnector(
                parent,
                sourceNode.anchoredPosition,
                nodePosition,
                pathId,
                depth,
                nodeKind == SkillTreePreviewNodeKind.Small);

            GameObject previewNode = new GameObject(
                nodeName,
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(Image),
                typeof(Button),
                typeof(SkillTreeNodeView));
            previewNode.transform.SetParent(parent, false);
            RectTransform previewRect = previewNode.GetComponent<RectTransform>();
            previewRect.anchorMin = previewRect.anchorMax = new Vector2(0f, 1f);
            previewRect.pivot = new Vector2(0.5f, 0.5f);
            previewRect.anchoredPosition = nodePosition;
            previewRect.sizeDelta = GetSkillTreePreviewSize(nodeKind);

            bool isClericHealNode = pathId == "Cleric" &&
                                    depth == 0 &&
                                    nodeKind == SkillTreePreviewNodeKind.Square;
            bool isClericHealUnlocked = isClericHealNode &&
                                        app != null &&
                                        app.IsSkillUnlocked("cleric", "healing_light");
            string spritePath = isClericHealNode
                ? "UI/SkillTree/heal1"
                : GetSkillTreePreviewSpritePath(nodeKind, false);
            string tooltip = isClericHealNode ? "Heal Ally" : pathId;

            Image previewImage = previewNode.GetComponent<Image>();
            previewImage.sprite = LoadSkillTreeSprite(spritePath);
            previewImage.preserveAspect = true;
            previewImage.color = nodeKind == SkillTreePreviewNodeKind.Small ||
                                 (isClericHealNode && !isClericHealUnlocked)
                ? new Color(0.42f, 0.42f, 0.42f, 1f)
                : Color.white;
            previewImage.raycastTarget = true;
            previewNode.GetComponent<SkillTreeNodeView>().Configure(previewImage.sprite, tooltip);

            previewNode.GetComponent<Button>().onClick.AddListener(() =>
            {
                if (isClericHealNode &&
                    app != null &&
                    app.UnlockSkill("cleric", "healing_light"))
                {
                    previewImage.color = Color.white;
                }

                if (nodeKind == SkillTreePreviewNodeKind.Small)
                {
                    previewImage.sprite = LoadSkillTreeSprite(GetSkillTreePreviewSpritePath(nodeKind, true));
                    previewImage.color = Color.white;
                }

                SkillTreePreviewNodeKind nextNodeKind = nodeKind == SkillTreePreviewNodeKind.Square
                    ? SkillTreePreviewNodeKind.Small
                    : nodeKind == SkillTreePreviewNodeKind.Small
                        ? SkillTreePreviewNodeKind.Circle
                        : SkillTreePreviewNodeKind.Small;

                if (nodeKind == SkillTreePreviewNodeKind.Square && depth == 0)
                {
                    CreateSkillTreePreviewFork(
                        parent,
                        previewRect,
                        pathId,
                        direction,
                        nextNodeKind,
                        depth + 1);
                    return;
                }

                if (depth == 3)
                {
                    CreateSkillTreePreviewFork(
                        parent,
                        previewRect,
                        pathId,
                        direction,
                        nextNodeKind,
                        depth + 1);
                    return;
                }

                CreateSkillTreePreviewNode(
                    parent,
                    previewRect,
                    pathId,
                    direction,
                    nextNodeKind,
                    depth + 1);
            });

            return previewRect;
        }

        private void CreateSkillTreePreviewFork(
            RectTransform parent,
            RectTransform sourceNode,
            string pathId,
            Vector2 direction,
            SkillTreePreviewNodeKind nodeKind,
            int depth)
        {
            Vector2 leftDirection = RotateSkillTreeDirection(direction, 28f);
            Vector2 rightDirection = direction;
            RectTransform leftNode = CreateSkillTreePreviewNode(
                parent,
                sourceNode,
                $"{pathId}-Left",
                leftDirection,
                nodeKind,
                depth);
            RectTransform rightNode = CreateSkillTreePreviewNode(
                parent,
                sourceNode,
                $"{pathId}-Right",
                rightDirection,
                nodeKind,
                depth);

            if (leftNode == null || rightNode == null)
            {
                return;
            }

            CreateSkillTreeConnector(
                parent,
                leftNode.anchoredPosition,
                rightNode.anchoredPosition,
                $"{pathId}-ForkBridge",
                depth,
                false);
        }

        private static Vector2 RotateSkillTreeDirection(Vector2 direction, float degrees)
        {
            float radians = degrees * Mathf.Deg2Rad;
            float cosine = Mathf.Cos(radians);
            float sine = Mathf.Sin(radians);
            return new Vector2(
                direction.x * cosine - direction.y * sine,
                direction.x * sine + direction.y * cosine).normalized;
        }

        private static float GetSkillTreePreviewSpacing(SkillTreePreviewNodeKind nodeKind)
        {
            return nodeKind == SkillTreePreviewNodeKind.Small ? 64f : 70f;
        }

        private static Vector2 GetSkillTreePreviewSize(SkillTreePreviewNodeKind nodeKind)
        {
            switch (nodeKind)
            {
                case SkillTreePreviewNodeKind.Square:
                    return new Vector2(46f, 46f);
                case SkillTreePreviewNodeKind.Small:
                    return new Vector2(32f, 32f);
                default:
                    return new Vector2(55f, 55f);
            }
        }

        private static string GetSkillTreePreviewSpritePath(
            SkillTreePreviewNodeKind nodeKind,
            bool isActivated)
        {
            switch (nodeKind)
            {
                case SkillTreePreviewNodeKind.Square:
                    return "UI/SkillTree/Skill2";
                case SkillTreePreviewNodeKind.Small:
                    return "UI/SkillTree/ss0";
                default:
                    return "UI/SkillTree/Skill1";
            }
        }

        private static void CreateSkillTreeConnector(
            RectTransform parent,
            Vector2 sourcePosition,
            Vector2 targetPosition,
            string pathId,
            int depth,
            bool connectsToSmallNode)
        {
            string connectorName = $"Skill Preview Connector {pathId} {depth}";
            if (parent.Find(connectorName) != null)
            {
                return;
            }

            GameObject connector = new GameObject(
                connectorName,
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(Image));
            connector.transform.SetParent(parent, false);
            connector.transform.SetAsFirstSibling();
            RectTransform connectorRect = connector.GetComponent<RectTransform>();
            connectorRect.anchorMin = connectorRect.anchorMax = new Vector2(0f, 1f);
            connectorRect.pivot = new Vector2(0.5f, 0.5f);
            Vector2 delta = targetPosition - sourcePosition;
            connectorRect.anchoredPosition = sourcePosition + delta * 0.5f;
            connectorRect.sizeDelta = new Vector2(connectsToSmallNode ? 3f : 4f, delta.magnitude);
            float angle = Mathf.Atan2(delta.y, delta.x) * Mathf.Rad2Deg - 90f;
            connectorRect.localRotation = Quaternion.Euler(0f, 0f, angle);
            Image connectorImage = connector.GetComponent<Image>();
            connectorImage.color = new Color(0.72f, 0.65f, 0.35f, 0.9f);
            connectorImage.raycastTarget = false;
        }

        private static Sprite LoadSkillTreeSprite(string resourcePath)
        {
            Sprite sprite = Resources.Load<Sprite>(resourcePath);
            if (sprite != null)
            {
                return sprite;
            }

            Texture2D texture = Resources.Load<Texture2D>(resourcePath);
            if (texture == null)
            {
                return null;
            }

            return Sprite.Create(
                texture,
                new Rect(0f, 0f, texture.width, texture.height),
                new Vector2(0.5f, 0.5f),
                100f);
        }

        private static void EnsureSkillTreeFrame(Transform skillPanel)
        {
            if (skillPanel == null)
            {
                return;
            }

            Transform existing = skillPanel.Find("Skill Tree Frame");
            if (existing != null)
            {
                existing.gameObject.SetActive(false);
            }
        }

        private void ApplyInventoryTorchPosition()
        {
            if (panels.Count <= 2 || panels[2] == null)
            {
                return;
            }

            ApplyStandardTorchPair(panels[2].transform);
        }

        private void ApplyPartyTorchPosition()
        {
            if (panels.Count == 0 || panels[0] == null)
            {
                return;
            }

            ApplyStandardTorchPair(panels[0].transform);
        }

        private void ApplyAllPanelTorchPositions()
        {
            for (int i = 0; i < 4 && i < panels.Count; i++)
            {
                if (panels[i] != null)
                {
                    ApplyStandardTorchPair(panels[i].transform);
                }
            }
        }

        private void ApplyFormationClassFrameLayout()
        {
            const float classFrameSize = 89.91f;
            for (int i = 0; i < heroButtons.Count; i++)
            {
                Button button = heroButtons[i];
                if (button == null)
                {
                    continue;
                }

                RectTransform frame = button.transform.Find("Empty Class Frame") as RectTransform;
                if (frame != null)
                {
                    frame.sizeDelta = new Vector2(classFrameSize, classFrameSize);
                }
            }
        }

        private void ApplyHeroClassButtonLayout()
        {
            const float startX = 24f;
            const float startY = 205f;
            const float columnSpacing = 120f;
            const float rowSpacing = 125f;

            for (int i = 0; i < heroButtons.Count; i++)
            {
                Button button = heroButtons[i];
                RectTransform rect = button != null ? button.transform as RectTransform : null;
                if (rect == null)
                {
                    continue;
                }

                rect.anchorMin = rect.anchorMax = new Vector2(0f, 1f);
                rect.pivot = new Vector2(0f, 1f);
                rect.anchoredPosition = new Vector2(
                    startX + (i % 3) * columnSpacing,
                    -(startY + (i / 3) * rowSpacing));
            }
        }

        private void ApplyFormationSlotLayout()
        {
            const float slotSize = 83.16f;
            for (int i = 0; i < formationSlots.Count; i++)
            {
                FormationSlotView slot = formationSlots[i];
                RectTransform rect = slot != null ? slot.transform as RectTransform : null;
                if (rect != null)
                {
                    rect.sizeDelta = new Vector2(slotSize, slotSize);
                }
            }
        }

        private void ApplyHeroClassContentLayout()
        {
            const float classFrameTopOffset = 15f;
            const float iconTopOffset = -12f;
            const float labelTopOffset = -77f;
            for (int i = 0; i < heroButtons.Count; i++)
            {
                Button button = heroButtons[i];
                if (button == null)
                {
                    continue;
                }

                RectTransform icon = button.transform.Find("Class Icon") as RectTransform;
                if (icon != null)
                {
                    icon.anchoredPosition = new Vector2(icon.anchoredPosition.x, iconTopOffset);
                }

                RectTransform emptyFrame = button.transform.Find("Empty Class Frame") as RectTransform;
                if (emptyFrame != null)
                {
                    emptyFrame.anchoredPosition = new Vector2(
                        emptyFrame.anchoredPosition.x,
                        classFrameTopOffset);
                }

                TMP_Text label = button.transform.Find("Class Name")?.GetComponent<TMP_Text>();
                if (label != null)
                {
                    label.fontSize = 12.5f;
                    label.fontStyle = FontStyles.Bold;
                    RectTransform labelRect = label.rectTransform;
                    labelRect.anchoredPosition = new Vector2(labelRect.anchoredPosition.x, labelTopOffset);
                }
            }
        }

        private void ApplyGlobalHudTorchLayout()
        {
            Transform background = transform.Find("Management Background") ?? transform;

            Transform left = background.Find("Left Candle");
            Transform right = background.Find("Right Candle");
            Transform leftLight = background.Find("Left Candle Light");
            Transform rightLight = background.Find("Right Candle Light");

            if (left == null)
            {
                left = CloneTorchAtWorldPosition(
                    FindPanelTorch("Left Candle", "Skill Tree Candle"),
                    background,
                    "Left Candle");
            }

            if (right == null)
            {
                right = CloneTorchAtWorldPosition(
                    FindPanelTorch("Right Candle", null),
                    background,
                    "Right Candle");
            }

            if (leftLight == null)
            {
                leftLight = CloneTorchAtWorldPosition(
                    FindPanelTorch("Left Candle Light", "Skill Tree Candle Light"),
                    background,
                    "Left Candle Light");
            }

            if (rightLight == null)
            {
                rightLight = CloneTorchAtWorldPosition(
                    FindPanelTorch("Right Candle Light", null),
                    background,
                    "Right Candle Light");
            }

            SetTorchVisibleOnTop(left);
            SetTorchVisibleOnTop(leftLight);
            SetTorchVisibleOnTop(right);
            SetTorchVisibleOnTop(rightLight);
            SetTorchLightLayout(leftLight, left, -88f);
            SetTorchLightLayout(rightLight, right, 88f);
        }

        private void HidePanelTorchPairs()
        {
            foreach (GameObject panel in panels)
            {
                if (panel == null)
                {
                    continue;
                }

                Transform panelTransform = panel.transform;
                SetChildActive(panelTransform, "Left Candle", false);
                SetChildActive(panelTransform, "Left Candle Light", false);
                SetChildActive(panelTransform, "Right Candle", false);
                SetChildActive(panelTransform, "Right Candle Light", false);
                SetChildActive(panelTransform, "Skill Tree Candle", false);
                SetChildActive(panelTransform, "Skill Tree Candle Light", false);
            }
        }

        private Transform FindPanelTorch(string name, string legacyName)
        {
            for (int i = 0; i < panels.Count; i++)
            {
                GameObject panel = panels[i];
                if (panel == null)
                {
                    continue;
                }

                Transform torch = panel.transform.Find(name);
                if (torch == null && !string.IsNullOrWhiteSpace(legacyName))
                {
                    torch = panel.transform.Find(legacyName);
                }

                if (torch != null)
                {
                    return torch;
                }
            }

            return null;
        }

        private static Transform CloneTorchAtWorldPosition(
            Transform source,
            Transform parent,
            string name)
        {
            if (source == null || parent == null)
            {
                return null;
            }

            GameObject clone = UnityEngine.Object.Instantiate(source.gameObject, parent, true);
            clone.name = name;
            return clone.transform;
        }

        private static void SetTorchVisibleOnTop(Transform torch)
        {
            if (torch == null)
            {
                return;
            }

            torch.gameObject.SetActive(true);
            torch.SetAsLastSibling();
        }

        private void ApplyUniformHudShadowDim()
        {
            Transform background = transform.Find("Management Background") ?? transform;
            RectTransform globalDim = background.Find("Torch Shadow Dim") as RectTransform;
            if (globalDim != null)
            {
                globalDim.gameObject.SetActive(true);
                globalDim.anchorMin = Vector2.zero;
                globalDim.anchorMax = Vector2.one;
                globalDim.offsetMin = Vector2.zero;
                globalDim.offsetMax = Vector2.zero;
                globalDim.SetAsFirstSibling();
            }

            foreach (GameObject panel in panels)
            {
                if (panel != null)
                {
                    SetChildActive(panel.transform, "Torch Shadow Dim", false);
                }
            }
        }

        private static void SetChildActive(Transform parent, string childName, bool isActive)
        {
            Transform child = parent.Find(childName);
            if (child != null)
            {
                child.gameObject.SetActive(isActive);
            }
        }

        private void ApplySkillTreeTorchLayout()
        {
            if (panels.Count <= 1 || panels[1] == null)
            {
                return;
            }

            ApplyStandardTorchPair(panels[1].transform);
        }

        private static void ApplyStandardTorchPair(Transform panel)
        {
            if (panel == null)
            {
                return;
            }

            Transform left = EnsureTorch(panel, "Left Candle", "Skill Tree Candle");
            Transform leftLight = EnsureTorch(panel, "Left Candle Light", "Skill Tree Candle Light");
            Transform right = EnsureTorch(panel, "Right Candle", null);
            Transform rightLight = EnsureTorch(panel, "Right Candle Light", null);

            if (right == null && left != null)
            {
                right = CloneTorch(left, panel, "Right Candle");
            }

            if (rightLight == null && leftLight != null)
            {
                rightLight = CloneTorch(leftLight, panel, "Right Candle Light");
            }

            SetTorchLayout(left, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(-98f, 8f));
            SetTorchLayout(leftLight, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(-176f, 32f));
            SetTorchLayout(right, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-38f, 8f));
            SetTorchLayout(rightLight, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(40f, 32f));
        }

        private static Transform EnsureTorch(Transform parent, string name, string legacyName)
        {
            Transform torch = parent.Find(name);
            if (torch == null && !string.IsNullOrWhiteSpace(legacyName))
            {
                torch = parent.Find(legacyName);
                if (torch != null)
                {
                    torch.name = name;
                }
            }

            return torch;
        }

        private static Transform CloneTorch(Transform source, Transform parent, string name)
        {
            GameObject clone = UnityEngine.Object.Instantiate(source.gameObject, parent, false);
            clone.name = name;
            return clone.transform;
        }

        private static void SetTorchLayout(
            Transform torch,
            Vector2 anchor,
            Vector2 pivot,
            Vector2 position)
        {
            RectTransform rect = torch as RectTransform;
            if (rect == null)
            {
                return;
            }

            rect.anchorMin = rect.anchorMax = anchor;
            rect.pivot = pivot;
            rect.anchoredPosition = position;
            torch.gameObject.SetActive(true);
            torch.SetAsLastSibling();
        }

        private static void SetTorchLightLayout(Transform light, Transform torch, float horizontalOffset)
        {
            RectTransform lightRect = light as RectTransform;
            RectTransform torchRect = torch as RectTransform;
            if (lightRect == null || torchRect == null)
            {
                return;
            }

            lightRect.anchorMin = lightRect.anchorMax = torchRect.anchorMin;
            lightRect.pivot = torchRect.pivot;
            lightRect.anchoredPosition = torchRect.anchoredPosition +
                new Vector2(horizontalOffset, 24f);
        }

        private void EnsureEquipmentHeroTabs()
        {
            if (equipmentHeroTabs.Count > 0 || panels.Count <= 2 || panels[2] == null || app?.Catalog == null)
            {
                return;
            }

            for (int i = 0; i < app.Catalog.Heroes.Count; i++)
            {
                HeroDefinition definition = app.Catalog.Heroes[i];
                GameObject tabObject = new GameObject(
                    $"Equipment Hero Tab {definition.Id}",
                    typeof(RectTransform),
                    typeof(CanvasRenderer),
                    typeof(Image),
                    typeof(Button));
                tabObject.transform.SetParent(panels[2].transform, false);

                Image image = tabObject.GetComponent<Image>();
                image.sprite = commandNormalSprite;
                image.type = commandNormalSprite != null ? Image.Type.Simple : Image.Type.Sliced;
                image.raycastTarget = true;

                RectTransform rect = image.rectTransform;
                rect.anchorMin = rect.anchorMax = new Vector2(0f, 1f);
                rect.pivot = new Vector2(0.5f, 0.5f);

                GameObject labelObject = new GameObject(
                    "Label", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
                labelObject.transform.SetParent(tabObject.transform, false);
                TextMeshProUGUI label = labelObject.GetComponent<TextMeshProUGUI>();
                label.font = Resources.Load<TMP_FontAsset>("UI/Fonts/VCR_OSD_MONO SDF");
                label.text = ShortHeroLabel(definition.Id);
                label.fontSize = 10f;
                label.alignment = TextAlignmentOptions.Center;
                label.raycastTarget = false;

                RectTransform labelRect = label.rectTransform;
                labelRect.anchorMin = Vector2.zero;
                labelRect.anchorMax = Vector2.one;
                labelRect.pivot = new Vector2(0.5f, 0.5f);
                labelRect.anchoredPosition = Vector2.zero;
                labelRect.sizeDelta = Vector2.zero;

                Button button = tabObject.GetComponent<Button>();
                string heroId = definition.Id;
                button.onClick.AddListener(() => SelectEquipmentHero(heroId));
                equipmentHeroTabs.Add(button);
            }

            ApplyEquipmentHeroTabLayout();
        }

        private void ApplyEquipmentHeroTabLayout()
        {
            const float startX = 322f;
            const float y = -330f;
            const float width = 54f;
            const float height = 35f;
            const float gap = 52.8f;

            for (int i = 0; i < equipmentHeroTabs.Count; i++)
            {
                Button button = equipmentHeroTabs[i];
                if (button == null)
                {
                    continue;
                }

                RectTransform rect = button.GetComponent<RectTransform>();
                if (rect != null)
                {
                    rect.anchorMin = rect.anchorMax = new Vector2(0f, 1f);
                    rect.pivot = new Vector2(0.5f, 0.5f);
                    rect.anchoredPosition = new Vector2(startX + i * gap, y);
                    rect.sizeDelta = new Vector2(width, height);
                }

                TMP_Text label = button.GetComponentInChildren<TMP_Text>(true);
                if (label != null)
                {
                    label.fontSize = 11f;
                }
            }
        }

        private void SelectEquipmentHero(string heroId)
        {
            if (string.IsNullOrWhiteSpace(heroId))
            {
                return;
            }

            activeHeroId = heroId;
            Refresh();
        }

        private static string ShortHeroLabel(string heroId)
        {
            switch (heroId)
            {
                case "warrior": return "WAR";
                case "rogue": return "ROG";
                case "mage": return "MAG";
                case "cleric": return "CLE";
                case "archer": return "ARC";
                case "magic_warrior": return "SPB";
                default: return heroId;
            }
        }

        private static Color HeroTabTextColor(string heroId, bool selected)
        {
            Color color;
            switch (heroId)
            {
                case "warrior": color = new Color(0.35f, 0.62f, 0.8f, 1f); break;
                case "cleric": color = new Color(0.82f, 0.68f, 0.28f, 1f); break;
                case "mage": color = new Color(0.78f, 0.32f, 0.34f, 1f); break;
                case "archer": color = new Color(0.38f, 0.7f, 0.42f, 1f); break;
                case "rogue": color = new Color(0.48f, 0.49f, 0.52f, 1f); break;
                case "magic_warrior": color = new Color(0.62f, 0.4f, 0.72f, 1f); break;
                default: color = new Color(0.78f, 0.78f, 0.78f, 1f); break;
            }

            color.a = selected ? 1f : 0.9f;
            return color;
        }

        private string Marker(string heroId)
        {
            return heroId == activeHeroId ? ">" : "•";
        }

        private void LoadCommandSprites()
        {
            commandNormalSprite = Resources.Load<Sprite>("UI/Command");
            commandPressedSprite = Resources.Load<Sprite>("UI/CommandPressed");
            commandSelectedSprite = Resources.Load<Sprite>("UI/CommandSelected");
            menuCommandSprite = Resources.Load<Sprite>("UI/MenuCommand");
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
            foreach (Button tab in tabButtons)
            {
                ApplyMenuCommandButtonState(tab);
            }

            IEnumerable<Button> commandButtons = new[] { cycleActiveSkillButton, cyclePassiveSkillButton }
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

        private void ApplyMenuCommandButtonState(Button button)
        {
            if (button == null || button.image == null || menuCommandSprite == null)
            {
                return;
            }

            Image image = button.image;
            image.sprite = menuCommandSprite;
            image.type = Image.Type.Sliced;
            image.preserveAspect = false;
            image.color = Color.white;
            button.targetGraphic = image;
            button.transition = Selectable.Transition.SpriteSwap;
            button.spriteState = new SpriteState
            {
                highlightedSprite = menuCommandSprite,
                pressedSprite = menuCommandSprite,
                selectedSprite = menuCommandSprite,
                disabledSprite = menuCommandSprite
            };

            ColorBlock colors = button.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = Color.white;
            colors.pressedColor = Color.white;
            colors.selectedColor = Color.white;
            colors.disabledColor = new Color(1f, 1f, 1f, 0.42f);
            button.colors = colors;
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

            bool selectedActLocked = mapUi != null && mapUi.IsActTwoSelected && !mapUi.IsActTwoUnlocked;
            startExpeditionButton.interactable = !selectedActLocked;
            startExpeditionButton.image.color = selectedActLocked
                ? new Color(0.36f, 0.36f, 0.36f, 0.68f)
                : new Color(0.72f, 1f, 0.68f, 1f);
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
                label.text = "START CAMPAIGN";
                label.color = selectedActLocked
                    ? new Color(0.72f, 0.72f, 0.72f, 0.9f)
                    : new Color(1f, 0.92f, 0.08f, 1f);
            }
        }

        private void ApplyCloseButtonState()
        {
            if (closeButton == null || closeButton.image == null || app?.State == null)
            {
                return;
            }

            bool expeditionActive = app.State.Expedition != null && app.State.Expedition.IsActive;
            ColorBlock colors = closeButton.colors;
            colors.disabledColor = new Color(0.82f, 0.12f, 0.12f, 1f);
            closeButton.colors = colors;
            closeButton.interactable = expeditionActive;
            closeButton.image.color = expeditionActive
                ? Color.white
                : new Color(0.82f, 0.12f, 0.12f, 1f);
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
                Button tab = tabButtons[i];
                Image image = tab.image;
                if (image == null)
                {
                    continue;
                }

                bool selected = i == activePanelIndex;
                image.sprite = menuCommandSprite != null
                    ? menuCommandSprite
                    : selected && commandSelectedSprite != null
                        ? commandSelectedSprite
                        : commandNormalSprite;
                image.type = menuCommandSprite != null ? Image.Type.Sliced : Image.Type.Simple;
                image.preserveAspect = menuCommandSprite == null;
                image.color = selected
                    ? Color.white
                    : new Color(0.78f, 0.78f, 0.78f, 0.92f);

                TMP_Text label = tab.GetComponentInChildren<TMP_Text>(true);
                if (label != null)
                {
                    label.color = selected
                        ? new Color(1f, 0.92f, 0.08f, 1f)
                        : new Color(0.96f, 0.98f, 1f, 1f);
                }
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
                formationSlots[i].SetFormationColor(FormationSlotColor(
                    activeFormationPreset,
                    activePositions[i]));
                HeroState occupant = selected.FirstOrDefault(hero => hero.Position.Equals(activePositions[i]));
                Sprite icon = occupant != null ? HeroFormationIcon(occupant.DefinitionId) : null;
                formationSlots[i].SetHero(icon, occupant != null ? occupant.DefinitionId : string.Empty);
            }

        }

        private void EnsureNavigationChains()
        {
            if (tabButtons.Count < 4 || tabButtons[0] == null ||
                tabButtons[0].transform.parent == null)
            {
                return;
            }

            Transform parent = tabButtons[0].transform.parent;
            Sprite chainSprite = Resources.Load<Sprite>("UI/FormationChain");
            if (chainSprite == null)
            {
                return;
            }

            List<Image> chains = new List<Image>();
            for (int i = 0; i < 3; i++)
            {
                Transform existing = parent.Find($"Navigation Chain {i + 1}");
                Image chain = existing != null ? existing.GetComponent<Image>() : null;
                if (chain == null)
                {
                    GameObject chainObject = new GameObject(
                        $"Navigation Chain {i + 1}",
                        typeof(RectTransform),
                        typeof(CanvasRenderer),
                        typeof(Image));
                    chainObject.transform.SetParent(parent, false);
                    chain = chainObject.GetComponent<Image>();
                }

                chain.sprite = chainSprite;
                chain.type = Image.Type.Simple;
                chain.preserveAspect = true;
                chain.raycastTarget = false;
                chains.Add(chain);
                chain.transform.SetAsFirstSibling();
            }

            for (int i = 0; i < chains.Count; i++)
            {
                RectTransform from = tabButtons[i].transform as RectTransform;
                RectTransform to = tabButtons[i + 1].transform as RectTransform;
                RectTransform chain = chains[i].rectTransform;
                if (from == null || to == null || chain == null)
                {
                    continue;
                }

                Vector2 fromCenter = from.anchoredPosition + new Vector2(from.rect.width * 0.5f, -from.rect.height * 0.5f);
                Vector2 toCenter = to.anchoredPosition + new Vector2(to.rect.width * 0.5f, -to.rect.height * 0.5f);
                chain.anchoredPosition = (fromCenter + toCenter) * 0.5f;
                chain.sizeDelta = new Vector2(18f, 24f);
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
            const float startX = 110f;
            const float startY = -43f;
            const float gap = 60f;
            return new Vector2(startX + position.Column * gap, startY - position.Row * gap);
        }

        private static string PartyClassDisplayName(string heroId, string localizedName)
        {
            return heroId == "magic_warrior" ? "Spell Blade" : localizedName;
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

        private static Color FormationSlotColor(int presetIndex, FormationPosition position)
        {
            switch (presetIndex)
            {
                case 1:
                    if (position.Equals(new FormationPosition(0, 1)))
                    {
                        return new Color(0.28f, 0.62f, 0.32f, 1f);
                    }

                    if (position.Equals(new FormationPosition(1, 1)))
                    {
                        return new Color(0.82f, 0.65f, 0.22f, 1f);
                    }

                    if (position.Equals(new FormationPosition(2, 1)))
                    {
                        return new Color(0.76f, 0.2f, 0.22f, 1f);
                    }

                    return new Color(0.3f, 0.48f, 0.82f, 1f);
                case 2:
                    if (position.Equals(new FormationPosition(0, 1)))
                    {
                        return new Color(0.76f, 0.2f, 0.22f, 1f);
                    }

                    if (position.Equals(new FormationPosition(0, 2)))
                    {
                        return new Color(0.82f, 0.65f, 0.22f, 1f);
                    }

                    if (position.Equals(new FormationPosition(1, 1)))
                    {
                        return new Color(0.28f, 0.62f, 0.32f, 1f);
                    }

                    return new Color(0.3f, 0.48f, 0.82f, 1f);
                default:
                    switch (position.Column)
                    {
                        case 0:
                            return new Color(0.76f, 0.2f, 0.22f, 1f);
                        case 1:
                            return new Color(0.82f, 0.65f, 0.22f, 1f);
                        case 2:
                            return new Color(0.28f, 0.62f, 0.32f, 1f);
                        default:
                            return new Color(0.3f, 0.48f, 0.82f, 1f);
                    }
            }
        }

        private static FormationPosition FrontSlot(int presetIndex)
        {
            switch (presetIndex)
            {
                case 1:
                    return new FormationPosition(1, 2);
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
                        new FormationPosition(1, 1),
                        new FormationPosition(0, 1),
                        new FormationPosition(2, 1),
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

        private void OnDestroy()
        {
            if (mapUi != null)
            {
                mapUi.ActSelectionChanged -= ApplyStartButtonState;
            }
        }
    }
}
