using System;
using System.IO;
using System.Linq;
using TMPro;
using TaskbarTactics.Content;
using TaskbarTactics.Core.Models;
using TaskbarTactics.Presentation;
using UnityEditor;
using UnityEditor.Localization;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Localization.Tables;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace TaskbarTactics.Editor
{
    [InitializeOnLoad]
    public static class ItemStatsSetup
    {
        private const string RequestPath = "Temp/ItemStatsSetup.request";
        private const string MainPath = "Assets/Scenes/Main.unity";

        static ItemStatsSetup() => EditorApplication.delayCall += ProcessRequest;

        private static void ProcessRequest()
        {
            if (!File.Exists(RequestPath)) return;
            if (EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling)
            {
                EditorApplication.delayCall += ProcessRequest;
                return;
            }
            File.Delete(RequestPath);
            try
            {
                Install();
                File.WriteAllText("Temp/ItemStatsSetup.result", "SUCCESS");
            }
            catch (Exception exception)
            {
                File.WriteAllText("Temp/ItemStatsSetup.result", exception.ToString());
                Debug.LogException(exception);
            }
        }

        [MenuItem("Taskbar Tactics/Items/Install Basic Stats and Tooltip")]
        public static void Install()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                throw new InvalidOperationException("Exit Play Mode before installing item UI.");
            Scene scene = SceneManager.GetSceneByPath(MainPath);
            if (scene.IsValid() && scene.isDirty)
                throw new InvalidOperationException("Main has unsaved edits. Save it before installing item UI.");
            if (!scene.IsValid() || !scene.isLoaded)
                scene = EditorSceneManager.OpenScene(MainPath, OpenSceneMode.Additive);

            var roots = scene.GetRootGameObjects();
            ManagementUiController owner = roots.SelectMany(root =>
                root.GetComponentsInChildren<ManagementUiController>(true)).Single();
            GameAppController app = roots.SelectMany(root =>
                root.GetComponentsInChildren<GameAppController>(true)).Single();
            GameContentCatalog catalog = app.Catalog;
            ContentBlueprint source = ContentBlueprint.CreateVerticalSlice();
            foreach (ItemDefinition definition in catalog.Items.Where(item => item.Icon != null))
            {
                ItemBlueprint data = source.Items.Single(item => item.Id == definition.Id);
                definition.Configure(data);
                EditorUtility.SetDirty(definition);
            }
            EditorUtility.SetDirty(catalog);
            InstallLocalization(catalog);

            Canvas canvas = owner.GetComponentInParent<Canvas>();
            if (canvas == null) canvas = owner.GetComponentInChildren<Canvas>(true);
            if (canvas == null) throw new InvalidOperationException("Management canvas was not found.");
            RectTransform bounds = (RectTransform)canvas.transform;
            ItemTooltipView view = canvas.GetComponentInChildren<ItemTooltipView>(true);
            if (view == null)
            {
                var panel = new GameObject("Item Tooltip", typeof(RectTransform), typeof(CanvasRenderer),
                    typeof(Image), typeof(CanvasGroup), typeof(ItemTooltipView));
                panel.transform.SetParent(bounds, false);
                RectTransform rect = (RectTransform)panel.transform;
                rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
                rect.pivot = new Vector2(0f, 1f);
                rect.sizeDelta = new Vector2(280f, 190f);
                Image background = panel.GetComponent<Image>();
                background.color = new Color(0.025f, 0.035f, 0.065f, 0.97f);
                background.raycastTarget = false;
                Outline outline = panel.AddComponent<Outline>();
                outline.effectColor = new Color(0.55f, 0.48f, 0.29f, 1f);
                outline.effectDistance = new Vector2(1f, -1f);
                var labelObject = new GameObject("Item Details", typeof(RectTransform), typeof(TextMeshProUGUI));
                labelObject.transform.SetParent(panel.transform, false);
                TMP_Text label = labelObject.GetComponent<TMP_Text>();
                label.font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(
                    "Assets/Resources/UI/Fonts/VCR_OSD_MONO SDF.asset");
                label.fontSize = 14f;
                label.color = new Color(0.9f, 0.91f, 0.95f);
                label.richText = true;
                label.textWrappingMode = TextWrappingModes.Normal;
                label.raycastTarget = false;
                RectTransform labelRect = label.rectTransform;
                labelRect.anchorMin = Vector2.zero;
                labelRect.anchorMax = Vector2.one;
                labelRect.offsetMin = new Vector2(12f, 12f);
                labelRect.offsetMax = new Vector2(-12f, -12f);
                view = panel.GetComponent<ItemTooltipView>();
                view.Configure(label, panel.GetComponent<CanvasGroup>(), bounds);
            }
            var tables = LocalizationEditorSettings.GetStringTableCollection("UI").StringTables;
            view.ConfigureLocalization(tables.First(table => table.LocaleIdentifier.Code == "en"),
                tables.First(table => table.LocaleIdentifier.Code == "es"));
            EditorUtility.SetDirty(view);
            owner.ConfigureItemTooltip(view);
            ItemInventoryPreview preview = app.GetComponent<ItemInventoryPreview>() ??
                app.gameObject.AddComponent<ItemInventoryPreview>();
            preview.Configure(app);
            EditorUtility.SetDirty(owner);
            EditorUtility.SetDirty(preview);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();
            Debug.Log("Basic item stats, localized shared tooltip and inventory preview installed.");
        }

        private static void InstallLocalization(GameContentCatalog catalog)
        {
            StringTableCollection collection = LocalizationEditorSettings.GetStringTableCollection("UI");
            if (collection == null) throw new InvalidOperationException("UI localization collection is missing.");
            StringTable english = collection.StringTables.First(table => table.LocaleIdentifier.Code == "en");
            StringTable spanish = collection.StringTables.First(table => table.LocaleIdentifier.Code == "es");
            void Add(string key, string en, string es)
            {
                english.AddEntry(key, en);
                spanish.AddEntry(key, es);
            }
            Add("item.tooltip.base", "Base stats", "Estadísticas base");
            Add("item.tooltip.rarity", "Rarity improvements", "Mejoras por rareza");
            Add("item.tooltip.no_rarity_bonus", "No additional bonus", "Sin mejora adicional");
            Add("item.tooltip.extras", "Extra attributes", "Atributos extra");
            Add("item.tooltip.percentage_points", "pp", "pp");
            Add("item.tooltip.multiplicative", "multiplicative", "multiplicativo");
            Add("item.rarity.Common", "Normal", "Normal");
            Add("item.rarity.Rare", "Rare", "Raro");
            Add("item.rarity.Epic", "Epic", "Épico");
            Add("item.rarity.Legendary", "Legendary", "Legendario");
            string[] statsEn = { "Health", "Mana", "Attack power", "Spell power", "Armor", "Magic resistance",
                "Attack speed", "Cast speed", "Attack range", "Critical chance", "Critical damage multiplier",
                "Accuracy", "Evasion", "Health / second", "Mana / second", "Cooldown reduction" };
            string[] statsEs = { "Vida", "Maná", "Ataque", "Poder mágico", "Armadura", "Resistencia mágica",
                "Velocidad de ataque", "Velocidad de lanzamiento", "Alcance", "Probabilidad de crítico",
                "Multiplicador de daño crítico", "Precisión", "Evasión", "Vida / segundo", "Maná / segundo",
                "Reducción de enfriamiento" };
            foreach (HeroStatType stat in Enum.GetValues(typeof(HeroStatType)))
                Add($"item.stat.{stat}", statsEn[(int)stat], statsEs[(int)stat]);
            string[] slotsEn = { "Head", "Shoulders", "Neck", "Chest", "Bracers", "Hands", "Legs", "Boots",
                "Belt", "Cloak", "Main hand", "Off hand", "Ring", "Ring", "Earring", "Earring" };
            string[] slotsEs = { "Cabeza", "Hombros", "Cuello", "Pecho", "Brazales", "Manos", "Piernas", "Botas",
                "Cinturón", "Capa", "Mano principal", "Mano secundaria", "Anillo", "Anillo", "Pendiente", "Pendiente" };
            foreach (EquipmentSlot slot in Enum.GetValues(typeof(EquipmentSlot)))
                Add($"item.slot.{slot}", slotsEn[(int)slot], slotsEs[(int)slot]);
            Add("item.armor.Cloth", "Cloth", "Tela");
            Add("item.armor.Leather", "Leather", "Cuero");
            Add("item.armor.Mail", "Mail", "Malla");
            Add("item.armor.Plate", "Plate", "Placas");
            string[] ids = { "wooden_sword", "wooden_mace", "wooden_staff", "wooden_bow", "wooden_dagger",
                "leather_hood", "leather_chest", "leather_legs", "plate_chest", "plate_legs", "tela_chest",
                "tela_glove", "blue_legs", "iron_boots", "iron_helmet" };
            string[] namesEn = { "Wooden Sword", "Wooden Mace", "Apprentice Staff", "Training Bow", "Wooden Dagger",
                "Leather Hood", "Leather Vest", "Leather Leggings", "Iron Breastplate", "Iron Legguards",
                "Apprentice Robe", "Cloth Gloves", "Blue Cloth Trousers", "Iron Boots", "Iron Helmet" };
            string[] namesEs = { "Espada de madera", "Maza de madera", "Bastón de aprendiz", "Arco de entrenamiento",
                "Daga de madera", "Capucha de cuero", "Chaleco de cuero", "Pantalones de cuero", "Coraza de hierro",
                "Grebas de hierro", "Túnica de aprendiz", "Guantes de tela", "Pantalones de tela azul",
                "Botas de hierro", "Casco de hierro" };
            for (int i = 0; i < ids.Length; i++) Add($"item.{ids[i]}.name", namesEn[i], namesEs[i]);
            foreach (ItemDefinition item in catalog.Items)
            {
                if (english.GetEntry(item.DisplayNameKey) != null) continue;
                string name = System.Globalization.CultureInfo.InvariantCulture.TextInfo.ToTitleCase(item.Id.Replace('_', ' '));
                Add(item.DisplayNameKey, name, name);
            }
            EditorUtility.SetDirty(english);
            EditorUtility.SetDirty(spanish);
            EditorUtility.SetDirty(collection.SharedData);
        }
    }
}
