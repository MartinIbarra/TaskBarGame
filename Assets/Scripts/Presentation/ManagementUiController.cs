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
        [Header("Navigation")]
        [SerializeField] private List<Button> tabButtons = new List<Button>();
        [SerializeField] private List<GameObject> panels = new List<GameObject>();
        [SerializeField] private Button closeButton;

        [Header("Party and formation")]
        [SerializeField] private List<Button> heroButtons = new List<Button>();
        [SerializeField] private List<Button> formationButtons = new List<Button>();
        [SerializeField] private TMP_Text partySummary;
        [SerializeField] private TMP_Text skillSummary;
        [SerializeField] private TMP_Text synergySummary;
        [SerializeField] private TMP_Text inventorySummary;
        [SerializeField] private TMP_Text mapSummary;
        [SerializeField] private TMP_Text settingsSummary;

        [Header("Actions")]
        [SerializeField] private Button cycleActiveSkillButton;
        [SerializeField] private Button cyclePassiveSkillButton;
        [SerializeField] private List<Button> equipSlotButtons = new List<Button>();
        [SerializeField] private List<Button> routeButtons = new List<Button>();
        [SerializeField] private Button startExpeditionButton;
        [SerializeField] private Button languageButton;
        [SerializeField] private Button quitButton;

        private GameAppController app;
        private string activeHeroId = "guardian";

        public void Configure(
            IEnumerable<Button> navigation,
            IEnumerable<GameObject> panelRoots,
            Button close,
            IEnumerable<Button> heroSelection,
            IEnumerable<Button> formation,
            TMP_Text party,
            TMP_Text skills,
            TMP_Text synergies,
            TMP_Text inventory,
            TMP_Text map,
            TMP_Text settings,
            Button cycleActive,
            Button cyclePassive,
            IEnumerable<Button> equipSlots,
            IEnumerable<Button> routes,
            Button start,
            Button language,
            Button quit)
        {
            tabButtons = navigation.ToList();
            panels = panelRoots.ToList();
            closeButton = close;
            heroButtons = heroSelection.ToList();
            formationButtons = formation.ToList();
            partySummary = party;
            skillSummary = skills;
            synergySummary = synergies;
            inventorySummary = inventory;
            mapSummary = map;
            settingsSummary = settings;
            cycleActiveSkillButton = cycleActive;
            cyclePassiveSkillButton = cyclePassive;
            equipSlotButtons = equipSlots.ToList();
            routeButtons = routes.ToList();
            startExpeditionButton = start;
            languageButton = language;
            quitButton = quit;
        }

        public void Bind(GameAppController targetApp, WindowModeController window)
        {
            app = targetApp;
            app.StateChanged += Refresh;
            closeButton.onClick.AddListener(window.ShowStrip);
            quitButton.onClick.AddListener(app.Quit);
            startExpeditionButton.onClick.AddListener(app.StartExpedition);
            cycleActiveSkillButton.onClick.AddListener(() => app.CycleSkill(activeHeroId, false));
            cyclePassiveSkillButton.onClick.AddListener(() => app.CycleSkill(activeHeroId, true));
            languageButton.onClick.AddListener(app.ToggleLanguage);

            for (int i = 0; i < tabButtons.Count; i++)
            {
                int captured = i;
                tabButtons[i].onClick.AddListener(() => ShowPanel(captured));
            }

            for (int i = 0; i < heroButtons.Count; i++)
            {
                int captured = i;
                heroButtons[i].onClick.AddListener(() => SelectHero(captured));
            }

            for (int i = 0; i < formationButtons.Count; i++)
            {
                int captured = i;
                formationButtons[i].onClick.AddListener(() =>
                    app.SetFormation(activeHeroId, new FormationPosition(captured / 3, captured % 3)));
            }

            for (int i = 0; i < routeButtons.Count && i < 3; i++)
            {
                RoutePreference preference = (RoutePreference)i;
                routeButtons[i].onClick.AddListener(() => app.SetRoutePreference(preference));
            }

            for (int i = 0; i < equipSlotButtons.Count && i < 4; i++)
            {
                EquipmentSlot slot = (EquipmentSlot)i;
                equipSlotButtons[i].onClick.AddListener(() => app.CycleEquipment(activeHeroId, slot));
            }

            ShowPanel(0);
            Refresh();
        }

        private void SelectHero(int index)
        {
            if (index >= app.Catalog.Heroes.Count)
            {
                return;
            }

            string heroId = app.Catalog.Heroes[index].Id;
            app.SelectOrReplaceHero(heroId, activeHeroId);
            activeHeroId = heroId;
            Refresh();
        }

        private void ShowPanel(int index)
        {
            for (int i = 0; i < panels.Count; i++)
            {
                panels[i].SetActive(i == index);
            }
        }

        private void Refresh()
        {
            if (app == null)
            {
                return;
            }

            List<HeroState> selected = app.State.Party.Heroes.Where(hero => hero.IsSelected).ToList();
            partySummary.text = string.Join("\n", selected.Select(hero =>
                $"{Marker(hero.DefinitionId)} {app.HeroName(hero.DefinitionId)} · Nv. {hero.Level} · {hero.Position}"));

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

            inventorySummary.text = app.State.Inventory.Count == 0
                ? "Inventario vacío."
                : string.Join("\n", app.State.Inventory.Take(14).Select(item =>
                    $"{item.Rarity} · {item.DefinitionId} · {string.Join(", ", item.AffixIds)}"));

            mapSummary.text =
                $"Nodo: {app.State.Expedition.CurrentNodeId}\n" +
                $"Completados: {app.State.Expedition.CompletedNodes}/18\n" +
                $"Prioridad: {app.State.Party.RoutePreference}\n\n" +
                "El mapa contiene combates, tesoros, eventos, élites y un jefe.";
            settingsSummary.text =
                $"Idioma: {app.State.LanguageCode.ToUpperInvariant()}\n" +
                "Avisos: visuales y silenciosos\n" +
                "Progreso offline máximo: 8 horas";

            for (int i = 0; i < heroButtons.Count && i < app.Catalog.Heroes.Count; i++)
            {
                HeroDefinition definition = app.Catalog.Heroes[i];
                HeroState state = app.State.Party.GetHero(definition.Id);
                TMP_Text text = heroButtons[i].GetComponentInChildren<TMP_Text>();
                text.text = $"{(state != null && state.IsSelected ? "●" : "○")} {app.HeroName(definition.Id)}";
                heroButtons[i].image.color = definition.Id == activeHeroId
                    ? new Color(0.25f, 0.7f, 0.8f)
                    : new Color(0.18f, 0.21f, 0.27f);
            }
        }

        private string Marker(string heroId)
        {
            return heroId == activeHeroId ? ">" : "•";
        }
    }
}
