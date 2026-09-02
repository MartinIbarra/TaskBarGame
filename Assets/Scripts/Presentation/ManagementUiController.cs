using System.Collections.Generic;
using System.Linq;
using TMPro;
using TaskbarTactics.Content;
using TaskbarTactics.Core.Models;
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
        [SerializeField] private EquipmentPreviewLayoutView equipmentPreview;
        [SerializeField] private List<Button> equipmentHeroTabs = new List<Button>();

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
            startExpeditionButton.onClick.AddListener(TryStartExpeditionFromSelectedAct);
            resetExpeditionButton?.onClick.AddListener(app.ResetExpeditionProgress);
            if (mapUi != null)
            {
                mapUi.ActSelectionChanged += ApplyStartButtonState;
            }

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
            ShowPanel(4);
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

            app.StartExpedition();
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

            skillSummary.text = string.Empty;

            synergySummary.text = string.Empty;
            EnsureLegendBookRing();

            inventorySummary.text = string.Empty;
            RefreshInventoryGrid();
            RefreshEquipmentPreview();
            RefreshEquipmentHeroTabs();
            ApplyInventorySubmenuVisibility();
            ApplyInventoryTorchPosition();

            mapSummary.text =
                $"Nodo: {app.State.Expedition.CurrentNodeId}\n" +
                $"Completados: {app.State.Expedition.CompletedNodes}/{app.Catalog.Map.Nodes.Count}\n\n" +
                RouteDescription(app.State.Party.RoutePreference);
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

            inventoryGrid?.SetOwner(this);
            inventoryGrid?.Refresh(app.Catalog, app.State.Inventory, app.State.Party.Heroes);
        }

        private void RefreshEquipmentPreview()
        {
            if (equipmentPreview == null && panels.Count > 3 && panels[3] != null)
            {
                equipmentPreview = panels[3].GetComponentInChildren<EquipmentPreviewLayoutView>(true);
            }

            equipmentPreview?.SetOwner(this);
            equipmentPreview?.RefreshHero(activeHeroId);
            equipmentPreview?.RefreshEquippedItems(
                app.Catalog,
                app.State.Inventory,
                app.State.Party.GetHero(activeHeroId));
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
                    label.color = selected
                        ? new Color(1f, 0.96f, 0.82f, 1f)
                        : new Color(0.82f, 0.82f, 0.82f, 0.92f);
                    label.fontStyle = selected ? FontStyles.Bold : FontStyles.Normal;
                }
            }
        }

        private void ApplyInventorySubmenuVisibility()
        {
            bool showInventoryOnlyViews = activePanelIndex == 3;
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
            DraggableMapView skillTreeView = panels[1].transform
                .Find("Skill Tree Viewport")
                ?.GetComponent<DraggableMapView>();
            RectTransform content = panels[1].transform
                .Find("Skill Tree Viewport/Skill Tree Content") as RectTransform;
            skillTreeView?.ConfigureCentered(
                content,
                2.4f,
                0.65f,
                2.4f);
        }

        private static void EnsureSkillTreeFrame(Transform skillPanel)
        {
            if (skillPanel == null)
            {
                return;
            }

            Transform existing = skillPanel.Find("Skill Tree Frame");
            Image frame = existing != null ? existing.GetComponent<Image>() : null;
            if (frame == null)
            {
                GameObject frameObject = new GameObject(
                    "Skill Tree Frame",
                    typeof(RectTransform),
                    typeof(CanvasRenderer),
                    typeof(Image));
                frameObject.transform.SetParent(skillPanel, false);
                frame = frameObject.GetComponent<Image>();
            }

            frame.sprite = Resources.Load<Sprite>("UI/MapFrame");
            frame.color = frame.sprite != null ? Color.white : Color.clear;
            frame.type = Image.Type.Sliced;
            frame.preserveAspect = false;
            frame.raycastTarget = false;
            RectTransform rect = frame.rectTransform;
            rect.anchorMin = new Vector2(0f, 0f);
            rect.anchorMax = new Vector2(1f, 1f);
            rect.offsetMin = new Vector2(28f, 42f);
            rect.offsetMax = new Vector2(-28f, -64f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            frame.transform.SetAsLastSibling();
        }

        private void ApplyInventoryTorchPosition()
        {
            if (panels.Count <= 3 || panels[3] == null)
            {
                return;
            }

            SetChildRectPosition(panels[3].transform, "Right Candle", new Vector2(560f, -56f));
            SetChildRectPosition(panels[3].transform, "Right Candle Light", new Vector2(560f, -32f));
        }

        private static void SetChildRectPosition(Transform parent, string childName, Vector2 position)
        {
            RectTransform rect = parent.Find(childName) as RectTransform;
            if (rect != null)
            {
                rect.anchoredPosition = position;
            }
        }

        private void EnsureEquipmentHeroTabs()
        {
            if (equipmentHeroTabs.Count > 0 || panels.Count <= 3 || panels[3] == null || app?.Catalog == null)
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
                tabObject.transform.SetParent(panels[3].transform, false);

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
            const float startX = 412f;
            const float y = -432f;
            const float width = 48.4f;
            const float height = 30.8f;
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
                label.color = selectedActLocked
                    ? new Color(0.72f, 0.72f, 0.72f, 0.9f)
                    : new Color(1f, 0.92f, 0.08f, 1f);
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
                Button tab = tabButtons[i];
                Image image = tab.image;
                if (image == null)
                {
                    continue;
                }

                bool selected = i == activePanelIndex;
                image.sprite = selected && commandSelectedSprite != null
                    ? commandSelectedSprite
                    : commandNormalSprite;
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
