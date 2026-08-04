using System.Collections.Generic;
using System.Linq;

namespace TaskbarTactics.Core.Localization
{
    public sealed class LocalizationCatalog
    {
        private static readonly string[] RequiredKeys =
        {
            "app.title",
            "strip.manage",
            "strip.rare_loot",
            "strip.defeat",
            "strip.complete",
            "nav.party",
            "nav.formation",
            "nav.skills",
            "nav.synergies",
            "nav.inventory",
            "nav.map",
            "nav.settings",
            "route.safety",
            "route.loot",
            "route.challenge",
            "action.start",
            "action.close",
            "action.quit",
            "offline.title",
            "offline.summary"
        };

        private readonly Dictionary<string, Dictionary<string, string>> tables;

        private LocalizationCatalog(Dictionary<string, Dictionary<string, string>> tables)
        {
            this.tables = tables;
        }

        public static LocalizationCatalog CreateBuiltIn()
        {
            return new LocalizationCatalog(new Dictionary<string, Dictionary<string, string>>
            {
                {
                    "es", new Dictionary<string, string>
                    {
                        ["app.title"] = "Tácticas de la Barra",
                        ["strip.manage"] = "Gestionar",
                        ["strip.rare_loot"] = "¡Botín raro!",
                        ["strip.defeat"] = "El escuadrón regresó",
                        ["strip.complete"] = "Expedición completada",
                        ["nav.party"] = "Escuadrón",
                        ["nav.formation"] = "Formación",
                        ["nav.skills"] = "Habilidades",
                        ["nav.synergies"] = "Sinergias",
                        ["nav.inventory"] = "Inventario",
                        ["nav.map"] = "Mapa",
                        ["nav.settings"] = "Ajustes",
                        ["route.safety"] = "Seguridad",
                        ["route.loot"] = "Botín",
                        ["route.challenge"] = "Desafío",
                        ["action.start"] = "Iniciar expedición",
                        ["action.close"] = "Cerrar",
                        ["action.quit"] = "Salir",
                        ["offline.title"] = "Mientras no estabas",
                        ["offline.summary"] = "{0} nodos resueltos en {1}"
                    }
                },
                {
                    "en", new Dictionary<string, string>
                    {
                        ["app.title"] = "Taskbar Tactics",
                        ["strip.manage"] = "Manage",
                        ["strip.rare_loot"] = "Rare loot!",
                        ["strip.defeat"] = "The party returned",
                        ["strip.complete"] = "Expedition complete",
                        ["nav.party"] = "Party",
                        ["nav.formation"] = "Formation",
                        ["nav.skills"] = "Skills",
                        ["nav.synergies"] = "Synergies",
                        ["nav.inventory"] = "Inventory",
                        ["nav.map"] = "Map",
                        ["nav.settings"] = "Settings",
                        ["route.safety"] = "Safety",
                        ["route.loot"] = "Loot",
                        ["route.challenge"] = "Challenge",
                        ["action.start"] = "Start expedition",
                        ["action.close"] = "Close",
                        ["action.quit"] = "Quit",
                        ["offline.title"] = "While you were away",
                        ["offline.summary"] = "{0} nodes resolved in {1}"
                    }
                }
            });
        }

        public IReadOnlyList<string> MissingKeys(string languageCode)
        {
            if (!tables.TryGetValue(languageCode, out Dictionary<string, string> table))
            {
                return RequiredKeys.ToList();
            }

            return RequiredKeys.Where(key => !table.ContainsKey(key)).ToList();
        }

        public string Get(string languageCode, string key)
        {
            if (tables.TryGetValue(languageCode, out Dictionary<string, string> table) &&
                table.TryGetValue(key, out string value))
            {
                return value;
            }

            return tables["en"].TryGetValue(key, out string fallback) ? fallback : key;
        }
    }
}
