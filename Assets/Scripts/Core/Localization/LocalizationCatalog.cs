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
            "offline.summary",
            "party.info.title.hero",
            "party.info.title.formation",
            "party.info.label.strong_in",
            "party.info.label.best_for",
            "party.info.hero.warrior.role",
            "party.info.hero.warrior.description",
            "party.info.hero.warrior.strengths",
            "party.info.hero.cleric.role",
            "party.info.hero.cleric.description",
            "party.info.hero.cleric.strengths",
            "party.info.hero.mage.role",
            "party.info.hero.mage.description",
            "party.info.hero.mage.strengths",
            "party.info.hero.archer.role",
            "party.info.hero.archer.description",
            "party.info.hero.archer.strengths",
            "party.info.hero.rogue.role",
            "party.info.hero.rogue.description",
            "party.info.hero.rogue.strengths",
            "party.info.hero.magic_warrior.role",
            "party.info.hero.magic_warrior.description",
            "party.info.hero.magic_warrior.strengths",
            "party.info.formation.0.name",
            "party.info.formation.0.type",
            "party.info.formation.0.description",
            "party.info.formation.0.best_for",
            "party.info.formation.1.name",
            "party.info.formation.1.type",
            "party.info.formation.1.description",
            "party.info.formation.1.best_for",
            "party.info.formation.2.name",
            "party.info.formation.2.type",
            "party.info.formation.2.description",
            "party.info.formation.2.best_for"
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
                        ["offline.summary"] = "{0} nodos resueltos en {1}",
                        ["party.info.title.hero"] = "Héroes",
                        ["party.info.title.formation"] = "Formación",
                        ["party.info.label.strong_in"] = "Destaca en:",
                        ["party.info.label.best_for"] = "Ideal para:",
                        ["party.info.hero.warrior.role"] = "Tanque de primera línea",
                        ["party.info.hero.warrior.description"] = "Su alta defensa y salud protegen a los aliados frágiles y sostienen la línea frontal.",
                        ["party.info.hero.warrior.strengths"] = "Defensa, salud",
                        ["party.info.hero.cleric.role"] = "Sanadora y apoyo",
                        ["party.info.hero.cleric.description"] = "Mantiene al grupo en combate con curación y apoyo mágico.",
                        ["party.info.hero.cleric.strengths"] = "Curación, maná",
                        ["party.info.hero.mage.role"] = "Daño mágico a distancia",
                        ["party.info.hero.mage.description"] = "Lanza magia ofensiva desde la retaguardia y castiga grupos de enemigos.",
                        ["party.info.hero.mage.strengths"] = "Poder mágico, alcance",
                        ["party.info.hero.archer.role"] = "Atacante a distancia",
                        ["party.info.hero.archer.description"] = "Inflige daño físico constante desde una posición segura.",
                        ["party.info.hero.archer.strengths"] = "Precisión, alcance",
                        ["party.info.hero.rogue.role"] = "Asesina de ráfaga",
                        ["party.info.hero.rogue.description"] = "Busca objetivos vulnerables y concentra daño rápido en un solo enemigo.",
                        ["party.info.hero.rogue.strengths"] = "Crítico, evasión",
                        ["party.info.hero.magic_warrior.role"] = "Combatiente híbrido",
                        ["party.info.hero.magic_warrior.description"] = "Combina combate cercano con técnicas mágicas para adaptarse a distintas amenazas.",
                        ["party.info.hero.magic_warrior.strengths"] = "Ataque, versatilidad",
                        ["party.info.formation.0.name"] = "4+",
                        ["party.info.formation.0.type"] = "Columna concentrada",
                        ["party.info.formation.0.description"] = "Alinea a cuatro héroes en una sola columna para mantener un frente compacto.",
                        ["party.info.formation.0.best_for"] = "Avance directo y grupos con un frente resistente",
                        ["party.info.formation.1.name"] = "3+1",
                        ["party.info.formation.1.type"] = "Vanguardia con apoyo",
                        ["party.info.formation.1.description"] = "Tres héroes ocupan la línea frontal mientras uno queda detrás.",
                        ["party.info.formation.1.best_for"] = "Proteger a un atacante o apoyo en retaguardia",
                        ["party.info.formation.2.name"] = "2+2",
                        ["party.info.formation.2.type"] = "Dos líneas equilibradas",
                        ["party.info.formation.2.description"] = "Distribuye al grupo en dos filas cortas para repartir la presión.",
                        ["party.info.formation.2.best_for"] = "Equipos mixtos y progreso versátil"
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
                        ["offline.summary"] = "{0} nodes resolved in {1}",
                        ["party.info.title.hero"] = "Heroes",
                        ["party.info.title.formation"] = "Formation",
                        ["party.info.label.strong_in"] = "Strong in:",
                        ["party.info.label.best_for"] = "Best for:",
                        ["party.info.hero.warrior.role"] = "Frontline Tank",
                        ["party.info.hero.warrior.description"] = "High defense and HP protect fragile allies and hold the front line.",
                        ["party.info.hero.warrior.strengths"] = "Defense, HP",
                        ["party.info.hero.cleric.role"] = "Healer and Support",
                        ["party.info.hero.cleric.description"] = "Keeps the party fighting with healing and supportive magic.",
                        ["party.info.hero.cleric.strengths"] = "Healing, MP",
                        ["party.info.hero.mage.role"] = "Ranged Magic Damage",
                        ["party.info.hero.mage.description"] = "Casts offensive magic from the rear and punishes groups of enemies.",
                        ["party.info.hero.mage.strengths"] = "Spell power, range",
                        ["party.info.hero.archer.role"] = "Ranged Attacker",
                        ["party.info.hero.archer.description"] = "Deals steady physical damage from a safer position.",
                        ["party.info.hero.archer.strengths"] = "Accuracy, range",
                        ["party.info.hero.rogue.role"] = "Burst Assassin",
                        ["party.info.hero.rogue.description"] = "Targets vulnerable foes and focuses quick damage on a single enemy.",
                        ["party.info.hero.rogue.strengths"] = "Critical hits, evasion",
                        ["party.info.hero.magic_warrior.role"] = "Hybrid Fighter",
                        ["party.info.hero.magic_warrior.description"] = "Blends close combat with magic techniques to adapt to different threats. Lost a bet and now she has to use both: magic and sword. She is NOT allowed into Taverns.",
                        ["party.info.hero.magic_warrior.strengths"] = "Attack, versatility",
                        ["party.info.formation.0.name"] = "4+",
                        ["party.info.formation.0.type"] = "Focused column",
                        ["party.info.formation.0.description"] = "Lines up four heroes in one column to keep a compact front.",
                        ["party.info.formation.0.best_for"] = "Direct advances and parties with a sturdy frontline",
                        ["party.info.formation.1.name"] = "3+1",
                        ["party.info.formation.1.type"] = "Frontline with support",
                        ["party.info.formation.1.description"] = "Three heroes take the front rank while one stays behind them.",
                        ["party.info.formation.1.best_for"] = "Protecting a rear-line damage dealer or support",
                        ["party.info.formation.2.name"] = "2+2",
                        ["party.info.formation.2.type"] = "Two balanced ranks",
                        ["party.info.formation.2.description"] = "Spreads the party across two short rows to share incoming pressure.",
                        ["party.info.formation.2.best_for"] = "Mixed teams and versatile progression"
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
