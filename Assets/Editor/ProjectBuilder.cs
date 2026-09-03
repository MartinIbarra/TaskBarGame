using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using TMPro;
using TaskbarTactics.Content;
using TaskbarTactics.Core.Models;
using TaskbarTactics.Presentation;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.Build.Reporting;
using UnityEditor.Localization;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.Localization;
using UnityEngine.Localization.Tables;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace TaskbarTactics.Editor
{
    public static class ProjectBuilder
    {
        private const string GeneratedRoot = "Assets/Generated";
        private const string CatalogPath = GeneratedRoot + "/Content/GameContentCatalog.asset";
        private const string UnitPrefabPath = "Assets/Prefabs/Gameplay/Unit.prefab";
        private const string MainScenePath = "Assets/Scenes/Main.unity";
        private const string TmpResourcesRoot = "Assets/TextMesh Pro/Resources";
        private const string TmpFontPath =
            TmpResourcesRoot + "/Fonts & Materials/LiberationSans SDF.asset";
        private const string UiFontSourcePath = "Assets/Resources/UI/Fonts/VCR_OSD_MONO.ttf";
        private const string UiFontAssetPath = "Assets/Resources/UI/Fonts/VCR_OSD_MONO SDF.asset";
        private static readonly Color Background = new Color(0.076f, 0.098f, 0.151f, 1f);
        private static readonly Color Panel = new Color(0.125f, 0.165f, 0.248f, 0.98f);
        private static readonly Color PanelLight = new Color(0.154f, 0.198f, 0.275f, 1f);
        private static readonly Color Accent = new Color(0.22f, 0.66f, 0.78f, 1f);
        private static readonly Color TextColor = new Color(0.96f, 0.98f, 1f, 1f);
        private static TMP_FontAsset defaultFont;

        [MenuItem("Taskbar Tactics/Build Editable Vertical Slice")]
        public static void BuildAll()
        {
            EnsureFolders();
            EnsureTextMeshProResources();
            CreateLocalizationAssets();
            GameContentCatalog catalog = CreateContentAssets();
            Sprite unitSprite = CreatePlaceholderSprite();
            Sprite circleSprite = CreateCircleSprite();
            RuntimeAnimatorController animator = CreateAnimationController();
            UnitView unitPrefab = CreateUnitPrefab(unitSprite, animator);
            CreateMainScene(catalog, unitPrefab, unitSprite, circleSprite);
            ConfigureProject();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("Taskbar Tactics vertical slice assets generated successfully.");
        }

        [MenuItem("Taskbar Tactics/Import TextMeshPro Essentials")]
        public static void ImportTextMeshProEssentials()
        {
            AssetDatabase.importPackageCompleted += OnTmpImportCompleted;
            AssetDatabase.importPackageCancelled += OnTmpImportCancelled;
            AssetDatabase.importPackageFailed += OnTmpImportFailed;
            TMP_PackageResourceImporter.ImportResources(true, false, false);
        }

        [MenuItem("Taskbar Tactics/Build Windows x64")]
        public static void BuildWindows()
        {
            BuildAll();
            const string outputDirectory = "Builds/Windows";
            if (!Directory.Exists(outputDirectory))
            {
                Directory.CreateDirectory(outputDirectory);
            }

            BuildPlayerOptions options = new BuildPlayerOptions
            {
                scenes = new[] { MainScenePath },
                locationPathName = outputDirectory + "/TaskbarTactics.exe",
                target = BuildTarget.StandaloneWindows64,
                options = BuildOptions.CompressWithLz4HC
            };
            BuildReport report = BuildPipeline.BuildPlayer(options);
            if (report.summary.result != BuildResult.Succeeded)
            {
                throw new InvalidOperationException(
                    $"Windows build failed with {report.summary.totalErrors} errors.");
            }

            Debug.Log($"Windows build completed: {report.summary.totalSize} bytes");
        }

        private static void EnsureFolders()
        {
            string[] folders =
            {
                "Assets/Scenes",
                "Assets/Prefabs",
                "Assets/Prefabs/Gameplay",
                "Assets/Art",
                "Assets/Art/Generated",
                "Assets/Animations",
                "Assets/Localization",
                "Assets/Resources",
                "Assets/Resources/Maps",
                "Assets/Resources/UI",
                "Assets/Resources/UI/Fonts",
                "Assets/Resources/Events",
                "Assets/Resources/Events/Town",
                "Assets/Resources/Audio",
                "Assets/Resources/Audio/Events",
                GeneratedRoot,
                GeneratedRoot + "/Content",
                GeneratedRoot + "/Content/Heroes",
                GeneratedRoot + "/Content/Skills",
                GeneratedRoot + "/Content/Items",
                GeneratedRoot + "/Content/ItemBonuses",
                GeneratedRoot + "/Content/StatusEffects",
                GeneratedRoot + "/Content/Synergies",
                GeneratedRoot + "/Content/Enemies",
                GeneratedRoot + "/Content/Encounters",
                GeneratedRoot + "/Content/Maps",
                GeneratedRoot + "/Content/Cosmetics"
            };

            foreach (string folder in folders)
            {
                EnsureFolder(folder);
            }
        }

        private static void EnsureFolder(string folder)
        {
            if (AssetDatabase.IsValidFolder(folder))
            {
                return;
            }

            string parent = Path.GetDirectoryName(folder)?.Replace('\\', '/');
            string name = Path.GetFileName(folder);
            if (!string.IsNullOrEmpty(parent))
            {
                EnsureFolder(parent);
                AssetDatabase.CreateFolder(parent, name);
            }
        }

        private static void EnsureTextMeshProResources()
        {
            defaultFont = LoadOrCreateUiFontAsset();
            if (defaultFont != null)
            {
                return;
            }

            defaultFont = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(TmpFontPath);
            if (defaultFont != null)
            {
                return;
            }

            throw new InvalidOperationException(
                "Faltan los recursos esenciales de TextMeshPro. Ejecute primero " +
                "Taskbar Tactics/Import TextMeshPro Essentials.");
        }

        private static TMP_FontAsset LoadOrCreateUiFontAsset()
        {
            TMP_FontAsset fontAsset = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(UiFontAssetPath);
            if (IsUsableFontAsset(fontAsset))
            {
                return fontAsset;
            }

            if (fontAsset != null)
            {
                AssetDatabase.DeleteAsset(UiFontAssetPath);
            }

            Font sourceFont = AssetDatabase.LoadAssetAtPath<Font>(UiFontSourcePath);
            if (sourceFont == null)
            {
                return null;
            }

            fontAsset = TMP_FontAsset.CreateFontAsset(sourceFont);
            if (!HasFontAtlas(fontAsset))
            {
                return null;
            }

            Texture2D atlas = fontAsset.atlasTextures.FirstOrDefault();
            Material material = fontAsset.material;
            if (material == null)
            {
                Shader shader = Shader.Find("TextMeshPro/Distance Field");
                if (shader == null)
                {
                    return null;
                }

                material = new Material(shader)
                {
                    name = "VCR_OSD_MONO Material"
                };
                material.SetTexture("_MainTex", atlas);
                fontAsset.material = material;
            }

            AssetDatabase.CreateAsset(fontAsset, UiFontAssetPath);
            if (atlas != null && !AssetDatabase.Contains(atlas))
            {
                atlas.name = "VCR_OSD_MONO Atlas";
                AssetDatabase.AddObjectToAsset(atlas, fontAsset);
            }

            if (material != null && !AssetDatabase.Contains(material))
            {
                AssetDatabase.AddObjectToAsset(material, fontAsset);
            }

            AssetDatabase.SaveAssets();
            return fontAsset;
        }

        private static bool IsUsableFontAsset(TMP_FontAsset fontAsset)
        {
            return HasFontAtlas(fontAsset) && fontAsset.material != null;
        }

        private static bool HasFontAtlas(TMP_FontAsset fontAsset)
        {
            return fontAsset != null &&
                   fontAsset.atlasTextures != null &&
                   fontAsset.atlasTextures.Length > 0 &&
                   fontAsset.atlasTextures[0] != null;
        }

        private static void OnTmpImportCompleted(string packageName)
        {
            UnsubscribeTmpImportCallbacks();
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            Debug.Log($"TextMeshPro essentials imported: {packageName}");
            if (Application.isBatchMode)
            {
                EditorApplication.Exit(0);
            }
        }

        private static void OnTmpImportCancelled(string packageName)
        {
            UnsubscribeTmpImportCallbacks();
            Debug.LogError($"TextMeshPro essentials import cancelled: {packageName}");
            if (Application.isBatchMode)
            {
                EditorApplication.Exit(1);
            }
        }

        private static void OnTmpImportFailed(string packageName, string errorMessage)
        {
            UnsubscribeTmpImportCallbacks();
            Debug.LogError($"TextMeshPro essentials import failed ({packageName}): {errorMessage}");
            if (Application.isBatchMode)
            {
                EditorApplication.Exit(1);
            }
        }

        private static void UnsubscribeTmpImportCallbacks()
        {
            AssetDatabase.importPackageCompleted -= OnTmpImportCompleted;
            AssetDatabase.importPackageCancelled -= OnTmpImportCancelled;
            AssetDatabase.importPackageFailed -= OnTmpImportFailed;
        }

        private static GameContentCatalog CreateContentAssets()
        {
            RemoveLegacyContentAssets();
            ContentBlueprint blueprint = ContentBlueprint.CreateVerticalSlice();
            Dictionary<string, SkillDefinition> skills = blueprint.Skills.ToDictionary(
                data => data.Id,
                data =>
                {
                    SkillDefinition asset = LoadOrCreate<SkillDefinition>(
                        $"{GeneratedRoot}/Content/Skills/{data.Id}.asset");
                    asset.Configure(data);
                    EditorUtility.SetDirty(asset);
                    return asset;
                });

            List<HeroDefinition> heroes = blueprint.Heroes.Select(data =>
            {
                HeroDefinition asset = LoadOrCreate<HeroDefinition>(
                    $"{GeneratedRoot}/Content/Heroes/{data.Id}.asset");
                asset.Configure(
                    data,
                    data.ActiveSkillIds.Select(id => skills[id]),
                    data.PassiveSkillIds.Select(id => skills[id]));
                AssignHeroArtwork(asset, data.Id);
                EditorUtility.SetDirty(asset);
                return asset;
            }).ToList();

            List<ItemDefinition> items = blueprint.Items.Select(data =>
            {
                ItemDefinition asset = LoadOrCreate<ItemDefinition>(
                    $"{GeneratedRoot}/Content/Items/{data.Id}.asset");
                asset.Configure(data);
                AssignItemArtwork(asset, data.Id);
                EditorUtility.SetDirty(asset);
                return asset;
            }).ToList();

            List<ItemBonusDefinition> itemBonuses = blueprint.ItemBonuses.Select(data =>
            {
                ItemBonusDefinition asset = LoadOrCreate<ItemBonusDefinition>(
                    $"{GeneratedRoot}/Content/ItemBonuses/{data.Id}.asset");
                asset.Configure(data);
                EditorUtility.SetDirty(asset);
                return asset;
            }).ToList();

            List<StatusEffectDefinition> statusEffects = blueprint.StatusEffects.Select(data =>
            {
                StatusEffectDefinition asset = LoadOrCreate<StatusEffectDefinition>(
                    $"{GeneratedRoot}/Content/StatusEffects/{data.Data.Id}.asset");
                asset.Configure(data);
                EditorUtility.SetDirty(asset);
                return asset;
            }).ToList();

            string[] tags =
            {
                "guard", "steel", "sacred", "mark", "poison",
                "bleed", "fire", "arcane"
            };
            List<SynergyDefinition> synergies = tags.Select(tag =>
            {
                SynergyDefinition asset = LoadOrCreate<SynergyDefinition>(
                    $"{GeneratedRoot}/Content/Synergies/{tag}.asset");
                asset.Configure(tag);
                EditorUtility.SetDirty(asset);
                return asset;
            }).ToList();

            List<EnemyDefinition> enemies = blueprint.Enemies.Select(data =>
            {
                EnemyDefinition asset = LoadOrCreate<EnemyDefinition>(
                    $"{GeneratedRoot}/Content/Enemies/{data.Id}.asset");
                asset.Configure(data);
                AssignEnemyArtwork(asset, data.Id);
                EditorUtility.SetDirty(asset);
                return asset;
            }).ToList();

            MapDefinition map = LoadOrCreate<MapDefinition>(
                $"{GeneratedRoot}/Content/Maps/barrow_road.asset");
            map.Configure("barrow_road", blueprint.MapNodes);
            EditorUtility.SetDirty(map);

            List<EncounterDefinition> encounters = new List<EncounterDefinition>();
            Dictionary<string, EnemyDefinition> enemyById = enemies.ToDictionary(enemy => enemy.Id);
            List<EnemyDefinition> normalEnemies = enemies.Where(enemy => !enemy.IsBoss).ToList();
            EnemyDefinition boss = enemies.First(enemy => enemy.IsBoss);
            foreach (MapNodeBlueprint node in blueprint.MapNodes.Where(node => IsEncounterNode(node)))
            {
                EncounterDefinition encounter = LoadOrCreate<EncounterDefinition>(
                    $"{GeneratedRoot}/Content/Encounters/{node.Id}.asset");
                List<EnemyDefinition> units = EnemyIdsForNode(node.Id)
                    .Where(enemyById.ContainsKey)
                    .Select(enemyId => enemyById[enemyId])
                    .ToList();
                if (units.Count == 0)
                {
                    units = node.Type == Core.Models.MapNodeType.Boss
                        ? new List<EnemyDefinition> { boss }
                        : Enumerable.Range(0, node.Type == Core.Models.MapNodeType.Elite ? 3 : 2)
                            .Select(index => normalEnemies[(node.Difficulty + index) % normalEnemies.Count])
                            .ToList();
                }

                encounter.Configure(node.Id, node.Difficulty, units);
                EditorUtility.SetDirty(encounter);
                encounters.Add(encounter);
            }

            CosmeticDefinition defaultCosmetic = LoadOrCreate<CosmeticDefinition>(
                $"{GeneratedRoot}/Content/Cosmetics/default_tint.asset");
            defaultCosmetic.Configure("default_tint", Color.white);
            EditorUtility.SetDirty(defaultCosmetic);

            GameContentCatalog catalog = LoadOrCreate<GameContentCatalog>(CatalogPath);
            catalog.Configure(
                heroes,
                skills.Values,
                items,
                itemBonuses,
                statusEffects,
                synergies,
                enemies,
                encounters,
                map,
                new[] { defaultCosmetic });
            EditorUtility.SetDirty(catalog);
            return catalog;
        }

        private static void RemoveLegacyContentAssets()
        {
            string[] legacyIds = { "guardian", "ranger", "pyromancer", "spellblade" };
            foreach (string legacyId in legacyIds)
            {
                string path = $"{GeneratedRoot}/Content/Heroes/{legacyId}.asset";
                if (AssetDatabase.LoadMainAssetAtPath(path) != null)
                {
                    AssetDatabase.DeleteAsset(path);
                }
            }

            string[] legacyItemIds =
            {
                "blood_brooch", "cinder_relic", "rangers_bow",
                "serpent_relic", "steel_relic"
            };
            foreach (string legacyItemId in legacyItemIds)
            {
                string path = $"{GeneratedRoot}/Content/Items/{legacyItemId}.asset";
                if (AssetDatabase.LoadMainAssetAtPath(path) != null)
                {
                    AssetDatabase.DeleteAsset(path);
                }
            }

            string legacyItemBonusFolder = GeneratedRoot + "/Content/Affixes";
            if (AssetDatabase.IsValidFolder(legacyItemBonusFolder))
            {
                AssetDatabase.DeleteAsset(legacyItemBonusFolder);
            }
        }

        private static void AssignHeroArtwork(HeroDefinition definition, string heroId)
        {
            string artId = LegacyHeroArtId(heroId);
            string spritePath = $"Assets/Art/Heroes/Concepts/{artId}.png";
            if (AssetDatabase.LoadAssetAtPath<Texture2D>(spritePath) == null)
            {
                return;
            }

            ConfigureCharacterSprite(spritePath);
            Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(spritePath);
            if (sprite == null)
            {
                return;
            }

            SerializedObject serialized = new SerializedObject(definition);
            serialized.FindProperty("artwork").objectReferenceValue = sprite;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static string LegacyHeroArtId(string heroId)
        {
            switch (heroId)
            {
                case "warrior": return "guardian";
                case "mage": return "pyromancer";
                case "archer": return "ranger";
                case "magic_warrior": return "spellblade";
                default: return heroId;
            }
        }

        private static IEnumerable<string> EnemyIdsForNode(string nodeId)
        {
            switch (nodeId)
            {
                case "narrow_bridge":
                    return new[] { "goblin", "wolf", "bog_slime" };
                case "cave":
                    return new[] { "bog_slime", "skeleton", "soul_fury", "ogre", "wyvern" };
                case "cemetery":
                    return new[] { "skeleton", "wraith", "cultist", "soul_fury" };
                case "goblin_village":
                    return new[] { "goblin", "goblin_archer", "shaman" };
                case "mountain_pass":
                    return new[] { "wolf", "wraith", "wyvern" };
                case "tomb_pass":
                    return new[] { "skeleton", "wraith", "bog_slime", "soul_fury" };
                case "lost_forest":
                    return new[] { "wolf", "wraith", "bog_slime" };
                case "last_bastion":
                    return new[] { "barrow_king" };
                case "corrupt_pass":
                    return new[] { "wolf", "wolf", "goblin_archer" };
                case "mt_secret":
                    return new[] { "wyvern", "wyvern", "ogre" };
                case "lo_hueso":
                    return new[] { "skeleton", "skeleton", "soul_fury" };
                case "arbol_morto":
                    return new[] { "soul_fury", "soul_fury", "shaman", "shaman" };
                case "mountain_pass_act2":
                    return new[] { "wyvern", "ogre", "bog_slime" };
                case "black_tower":
                    return new[] { "shaman", "shaman", "wraith", "skeleton" };
                case "port":
                    return new[] { "skeleton", "skeleton", "soul_fury", "soul_fury" };
                case "lost_bay":
                    return new[] { "barrow_king", "barrow_king" };
                default:
                    return Array.Empty<string>();
            }
        }

        private static bool IsEncounterNode(MapNodeBlueprint node)
        {
            return node != null &&
                   (node.Type == Core.Models.MapNodeType.Combat ||
                    node.Type == Core.Models.MapNodeType.Elite ||
                    node.Type == Core.Models.MapNodeType.Boss ||
                    node.Id == "cave" ||
                    node.Id == "mountain_pass");
        }

        private static void AssignEnemyArtwork(EnemyDefinition definition, string enemyId)
        {
            string spritePath = $"Assets/Art/Enemies/Concepts/{enemyId}.png";
            if (AssetDatabase.LoadAssetAtPath<Texture2D>(spritePath) == null)
            {
                return;
            }

            ConfigureCharacterSprite(spritePath);
            Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(spritePath);
            if (sprite == null)
            {
                return;
            }

            SerializedObject serializedDefinition = new SerializedObject(definition);
            SerializedProperty artwork = serializedDefinition.FindProperty("artwork");
            if (artwork != null)
            {
                artwork.objectReferenceValue = sprite;
                serializedDefinition.ApplyModifiedPropertiesWithoutUndo();
            }
        }

        private static void AssignItemArtwork(ItemDefinition definition, string itemId)
        {
            string spritePath = $"Assets/Resources/Items/{itemId}.png";
            if (AssetDatabase.LoadAssetAtPath<Texture2D>(spritePath) == null)
            {
                definition.SetIcon(null);
                return;
            }

            Sprite sprite = LoadPixelUiSprite(spritePath);
            definition.SetIcon(sprite);
        }

        private static void ConfigureCharacterSprite(string path)
        {
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
            TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer == null)
            {
                return;
            }

            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.spritePixelsPerUnit = 256f;
            importer.spritePivot = new Vector2(0.5f, 0f);
            importer.filterMode = FilterMode.Point;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.mipmapEnabled = false;
            importer.alphaIsTransparency = true;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            TextureImporterSettings settings = new TextureImporterSettings();
            importer.ReadTextureSettings(settings);
            settings.spriteAlignment = (int)SpriteAlignment.Custom;
            settings.spritePivot = new Vector2(0.5f, 0f);
            importer.SetTextureSettings(settings);
            importer.SaveAndReimport();
        }

        private static T LoadOrCreate<T>(string path) where T : ScriptableObject
        {
            T asset = AssetDatabase.LoadAssetAtPath<T>(path);
            if (asset != null)
            {
                return asset;
            }

            if (AssetDatabase.LoadMainAssetAtPath(path) != null)
            {
                AssetDatabase.DeleteAsset(path);
            }

            asset = ScriptableObject.CreateInstance<T>();
            AssetDatabase.CreateAsset(asset, path);
            return asset;
        }

        private static Sprite CreatePlaceholderSprite()
        {
            const string path = "Assets/Art/Generated/unit.png";
            Texture2D texture = new Texture2D(16, 16, TextureFormat.RGBA32, false);
            Color32 transparent = new Color32(0, 0, 0, 0);
            Color32 white = new Color32(255, 255, 255, 255);
            Color32 outline = new Color32(28, 34, 48, 255);
            Color32[] pixels = Enumerable.Repeat(transparent, 16 * 16).ToArray();
            for (int y = 2; y <= 13; y++)
            {
                for (int x = 4; x <= 11; x++)
                {
                    bool edge = x == 4 || x == 11 || y == 2 || y == 13;
                    pixels[y * 16 + x] = edge ? outline : white;
                }
            }

            pixels[10 * 16 + 6] = outline;
            pixels[10 * 16 + 9] = outline;
            texture.SetPixels32(pixels);
            texture.Apply();
            File.WriteAllBytes(path, texture.EncodeToPNG());
            Object.DestroyImmediate(texture);
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
            TextureImporter importer = (TextureImporter)AssetImporter.GetAtPath(path);
            importer.textureType = TextureImporterType.Sprite;
            importer.spritePixelsPerUnit = 16;
            importer.filterMode = FilterMode.Point;
            importer.mipmapEnabled = false;
            importer.alphaIsTransparency = true;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.SaveAndReimport();
            return AssetDatabase.LoadAssetAtPath<Sprite>(path);
        }

        private static RuntimeAnimatorController CreateAnimationController()
        {
            AnimationClip idle = CreateClip("Idle", true);
            SetCurve(idle, "m_LocalPosition.y",
                new Keyframe(0f, 0f), new Keyframe(0.5f, 0.035f), new Keyframe(1f, 0f));
            SetCurve(idle, "m_LocalScale.y",
                new Keyframe(0f, 1f), new Keyframe(0.5f, 1.018f), new Keyframe(1f, 1f));

            AnimationClip running = CreateClip("Running", true);
            SetCurve(running, "m_LocalPosition.y",
                new Keyframe(0f, 0f), new Keyframe(0.12f, 0.075f),
                new Keyframe(0.24f, 0f), new Keyframe(0.36f, 0.075f),
                new Keyframe(0.48f, 0f));
            SetCurve(running, "m_LocalPosition.x",
                new Keyframe(0f, -0.018f), new Keyframe(0.24f, 0.018f),
                new Keyframe(0.48f, -0.018f));
            SetCurve(running, "m_LocalScale.x",
                new Keyframe(0f, 1.015f), new Keyframe(0.12f, 0.985f),
                new Keyframe(0.24f, 1.015f), new Keyframe(0.36f, 0.985f),
                new Keyframe(0.48f, 1.015f));

            AnimationClip attack = CreateClip("Attack", false);
            SetCurve(attack, "m_LocalPosition.x",
                new Keyframe(0f, 0f), new Keyframe(0.08f, -0.035f),
                new Keyframe(0.18f, 0.055f), new Keyframe(0.38f, 0f));
            SetCurve(attack, "m_LocalPosition.y",
                new Keyframe(0f, 0f), new Keyframe(0.18f, 0.035f),
                new Keyframe(0.38f, 0f));
            SetCurve(attack, "m_LocalScale.x",
                new Keyframe(0f, 1f), new Keyframe(0.08f, 0.97f),
                new Keyframe(0.18f, 1.02f), new Keyframe(0.38f, 1f));

            AnimationClip hit = CreateClip("Hit", false);
            SetCurve(hit, "m_LocalPosition.x",
                new Keyframe(0f, 0f), new Keyframe(0.05f, -0.13f),
                new Keyframe(0.1f, 0.08f), new Keyframe(0.16f, -0.04f),
                new Keyframe(0.24f, 0f));
            SetCurve(hit, "m_LocalScale.y",
                new Keyframe(0f, 1f), new Keyframe(0.08f, 0.88f),
                new Keyframe(0.16f, 1.04f), new Keyframe(0.24f, 1f));

            AnimationClip death = CreateClip("Death", false);
            SetCurve(death, "m_LocalPosition.x",
                new Keyframe(0f, 0f), new Keyframe(0.18f, 0.08f),
                new Keyframe(0.65f, 0.16f));
            SetCurve(death, "m_LocalPosition.y",
                new Keyframe(0f, 0f), new Keyframe(0.18f, 0.05f),
                new Keyframe(0.65f, -0.5f));
            SetCurve(death, "m_LocalScale.x",
                new Keyframe(0f, 1f), new Keyframe(0.18f, 1.04f),
                new Keyframe(0.65f, 0.76f));
            SetCurve(death, "m_LocalScale.y",
                new Keyframe(0f, 1f), new Keyframe(0.18f, 0.92f),
                new Keyframe(0.65f, 0.08f));

            const string path = "Assets/Animations/Unit.controller";
            AssetDatabase.DeleteAsset(path);
            AnimatorController controller = AnimatorController.CreateAnimatorControllerAtPath(path);
            controller.AddParameter("Speed", AnimatorControllerParameterType.Float);
            controller.AddParameter("Attack", AnimatorControllerParameterType.Trigger);
            controller.AddParameter("Hit", AnimatorControllerParameterType.Trigger);
            controller.AddParameter("IsDead", AnimatorControllerParameterType.Bool);
            AnimatorStateMachine machine = controller.layers[0].stateMachine;
            AnimatorState idleState = machine.AddState("Idle", new Vector3(260, 80));
            idleState.motion = idle;
            machine.defaultState = idleState;
            AnimatorState runningState = machine.AddState("Running", new Vector3(520, 80));
            runningState.motion = running;
            AnimatorState attackState = machine.AddState("Attack", new Vector3(520, -40));
            attackState.motion = attack;
            AnimatorState hitState = machine.AddState("Hit", new Vector3(520, 200));
            hitState.motion = hit;
            AnimatorState deathState = machine.AddState("Death", new Vector3(780, 80));
            deathState.motion = death;

            AnimatorStateTransition idleToRunning = idleState.AddTransition(runningState);
            idleToRunning.hasExitTime = false;
            idleToRunning.duration = 0.06f;
            idleToRunning.AddCondition(AnimatorConditionMode.Greater, 0.1f, "Speed");

            AnimatorStateTransition runningToIdle = runningState.AddTransition(idleState);
            runningToIdle.hasExitTime = false;
            runningToIdle.duration = 0.06f;
            runningToIdle.AddCondition(AnimatorConditionMode.Less, 0.1f, "Speed");

            AnimatorStateTransition deathTransition = machine.AddAnyStateTransition(deathState);
            deathTransition.hasExitTime = false;
            deathTransition.duration = 0.04f;
            deathTransition.canTransitionToSelf = false;
            deathTransition.AddCondition(AnimatorConditionMode.If, 0, "IsDead");

            AnimatorStateTransition attackTransition = machine.AddAnyStateTransition(attackState);
            attackTransition.hasExitTime = false;
            attackTransition.duration = 0.03f;
            attackTransition.canTransitionToSelf = false;
            attackTransition.AddCondition(AnimatorConditionMode.If, 0, "Attack");
            AddReturnTransition(attackState, idleState);

            AnimatorStateTransition hitTransition = machine.AddAnyStateTransition(hitState);
            hitTransition.hasExitTime = false;
            hitTransition.duration = 0.02f;
            hitTransition.canTransitionToSelf = false;
            hitTransition.AddCondition(AnimatorConditionMode.If, 0, "Hit");
            AddReturnTransition(hitState, idleState);

            EditorUtility.SetDirty(controller);
            return controller;
        }

        private static AnimationClip CreateClip(string name, bool loop)
        {
            string path = $"Assets/Animations/Unit_{name}.anim";
            AnimationClip clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(path);
            if (clip == null)
            {
                clip = new AnimationClip { name = name };
                AssetDatabase.CreateAsset(clip, path);
            }

            clip.frameRate = 12;
            clip.ClearCurves();
            AnimationClipSettings settings = AnimationUtility.GetAnimationClipSettings(clip);
            settings.loopTime = loop;
            AnimationUtility.SetAnimationClipSettings(clip, settings);
            EditorUtility.SetDirty(clip);
            return clip;
        }

        private static void SetCurve(
            AnimationClip clip,
            string property,
            params Keyframe[] keys)
        {
            clip.SetCurve(string.Empty, typeof(Transform), property, new AnimationCurve(keys));
        }

        private static void AddReturnTransition(AnimatorState from, AnimatorState to)
        {
            AnimatorStateTransition transition = from.AddTransition(to);
            transition.hasExitTime = true;
            transition.exitTime = 0.95f;
            transition.duration = 0.04f;
        }

        private static UnitView CreateUnitPrefab(
            Sprite sprite,
            RuntimeAnimatorController controller)
        {
            GameObject root = new GameObject("Unit");
            UnitAnimationBridge bridge = root.AddComponent<UnitAnimationBridge>();
            UnitView view = root.AddComponent<UnitView>();
            Sprite healthBackgroundSprite = LoadUiSprite(
                "Assets/Resources/UI/HealthBars/background.png", Vector4.zero) ?? sprite;
            Sprite healthFillSprite = LoadUiSprite(
                "Assets/Resources/UI/HealthBars/hp.png", Vector4.zero) ?? sprite;

            GameObject visualRoot = new GameObject("Visual Root");
            visualRoot.transform.SetParent(root.transform, false);
            Animator animator = visualRoot.AddComponent<Animator>();
            animator.runtimeAnimatorController = controller;
            bridge.Configure(animator);

            GameObject artworkObject = new GameObject("Artwork");
            artworkObject.transform.SetParent(visualRoot.transform, false);
            SpriteRenderer body = artworkObject.AddComponent<SpriteRenderer>();
            body.sprite = sprite;
            body.sortingOrder = 10;

            GameObject hud = new GameObject("HUD");
            hud.transform.SetParent(root.transform, false);
            GameObject healthBackground = CreateSpriteChild(
                hud.transform, "Health Background", healthBackgroundSprite, Color.white, 20);
            healthBackground.transform.localPosition = new Vector3(0, 0.47f, 0);
            healthBackground.transform.localScale = new Vector3(0.168f, 0.28f, 1);

            GameObject healthFill = CreateSpriteChild(
                healthBackground.transform, "Health Fill", healthFillSprite, Color.white, 21);
            healthFill.transform.localPosition = new Vector3(0, 0.005f, 0);
            healthFill.transform.localScale = Vector3.one;

            GameObject labelObject = new GameObject("Name Label");
            labelObject.transform.SetParent(hud.transform, false);
            labelObject.transform.localPosition = new Vector3(0, 0.62f, 0);
            TextMeshPro label = labelObject.AddComponent<TextMeshPro>();
            label.font = defaultFont;
            label.fontSize = 0.34f;
            label.alignment = TextAlignmentOptions.Center;
            label.color = TextColor;
            label.sortingOrder = 22;
            label.rectTransform.sizeDelta = new Vector2(0.95f, 0.18f);
            view.ConfigureReferences(body, healthFill.GetComponent<SpriteRenderer>(), label, bridge);

            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, UnitPrefabPath);
            Object.DestroyImmediate(root);
            return prefab.GetComponent<UnitView>();
        }

        private static GameObject CreateSpriteChild(
            Transform parent,
            string name,
            Sprite sprite,
            Color color,
            int order)
        {
            GameObject child = new GameObject(name);
            child.transform.SetParent(parent, false);
            SpriteRenderer renderer = child.AddComponent<SpriteRenderer>();
            renderer.sprite = sprite;
            renderer.color = color;
            renderer.sortingOrder = order;
            return child;
        }

        private static Sprite CreateCircleSprite()
        {
            const string path = "Assets/Art/Generated/map_node_circle.png";
            const int size = 32;
            Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
            Color32 transparent = new Color32(0, 0, 0, 0);
            Color32[] pixels = Enumerable.Repeat(transparent, size * size).ToArray();
            Vector2 center = new Vector2((size - 1) * 0.5f, (size - 1) * 0.5f);
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float distance = Vector2.Distance(new Vector2(x, y), center);
                    if (distance <= 14.8f)
                    {
                        byte alpha = distance >= 13.2f ? (byte)230 : (byte)255;
                        pixels[y * size + x] = new Color32(255, 222, 148, alpha);
                    }

                    if (distance <= 8.2f)
                    {
                        pixels[y * size + x] = new Color32(39, 30, 18, 245);
                    }

                    if (distance <= 5.4f)
                    {
                        pixels[y * size + x] = new Color32(255, 229, 166, 255);
                    }
                }
            }

            texture.SetPixels32(pixels);
            texture.Apply();
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            File.WriteAllBytes(path, texture.EncodeToPNG());
            Object.DestroyImmediate(texture);
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
            TextureImporter importer = (TextureImporter)AssetImporter.GetAtPath(path);
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.spritePixelsPerUnit = size;
            importer.filterMode = FilterMode.Bilinear;
            importer.mipmapEnabled = false;
            importer.alphaIsTransparency = true;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.SaveAndReimport();
            return AssetDatabase.LoadAssetAtPath<Sprite>(path);
        }

        private static Sprite LoadUiSprite(string path, Vector4 border)
        {
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
            TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer != null)
            {
                importer.textureType = TextureImporterType.Sprite;
                importer.spriteImportMode = SpriteImportMode.Single;
                importer.spritePixelsPerUnit = 100;
                importer.filterMode = FilterMode.Bilinear;
                importer.mipmapEnabled = false;
                importer.alphaIsTransparency = true;
                importer.textureCompression = TextureImporterCompression.Uncompressed;
                TextureImporterSettings settings = new TextureImporterSettings();
                importer.ReadTextureSettings(settings);
                settings.spriteMeshType = SpriteMeshType.FullRect;
                settings.spriteBorder = border;
                importer.SetTextureSettings(settings);
                importer.SaveAndReimport();
            }

            return AssetDatabase.LoadAssetAtPath<Sprite>(path);
        }

        private static Sprite[] LoadCandleSprites()
        {
            string[] paths =
            {
                "Assets/Resources/UI/Candle/vela0.png",
                "Assets/Resources/UI/Candle/vela1.png",
                "Assets/Resources/UI/Candle/vela2.png"
            };
            return paths.Select(LoadPixelUiSprite).Where(sprite => sprite != null).ToArray();
        }

        private static Sprite[] LoadTorchLightSprites()
        {
            string[] paths =
            {
                "Assets/Resources/UI/TorchLight/light0.png",
                "Assets/Resources/UI/TorchLight/light1.png",
                "Assets/Resources/UI/TorchLight/light2.png"
            };
            return paths.Select(LoadSoftUiSprite).Where(sprite => sprite != null).ToArray();
        }

        private static Sprite[] LoadFormationHeroIcons()
        {
            string[] paths =
            {
                "Assets/Resources/HeroClasses/guardian.png",
                "Assets/Resources/HeroClasses/cleric.png",
                "Assets/Resources/HeroClasses/pyromancer.png",
                "Assets/Resources/HeroClasses/ranger.png",
                "Assets/Resources/HeroClasses/rogue.png",
                "Assets/Resources/HeroClasses/spellblade.png"
            };
            return paths.Select(LoadSoftUiSprite).Where(sprite => sprite != null).ToArray();
        }

        private static Sprite[] LoadMapFlagSprites()
        {
            string[] paths =
            {
                "Assets/Resources/UI/MapFlag/flag1.png",
                "Assets/Resources/UI/MapFlag/flag2.png",
                "Assets/Resources/UI/MapFlag/flag3.png"
            };
            return paths.Select(LoadPixelUiSprite).Where(sprite => sprite != null).ToArray();
        }

        private static Sprite LoadPixelUiSprite(string path)
        {
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
            TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer != null)
            {
                importer.textureType = TextureImporterType.Sprite;
                importer.spriteImportMode = SpriteImportMode.Single;
                importer.spritePixelsPerUnit = 100;
                importer.filterMode = FilterMode.Point;
                importer.mipmapEnabled = false;
                importer.alphaIsTransparency = true;
                importer.textureCompression = TextureImporterCompression.Uncompressed;
                importer.SaveAndReimport();
            }

            return AssetDatabase.LoadAssetAtPath<Sprite>(path);
        }

        private static Sprite LoadSoftUiSprite(string path)
        {
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
            TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer != null)
            {
                importer.textureType = TextureImporterType.Sprite;
                importer.spriteImportMode = SpriteImportMode.Single;
                importer.spritePixelsPerUnit = 100;
                importer.filterMode = FilterMode.Bilinear;
                importer.mipmapEnabled = false;
                importer.alphaIsTransparency = true;
                importer.textureCompression = TextureImporterCompression.Uncompressed;
                importer.SaveAndReimport();
            }

            return AssetDatabase.LoadAssetAtPath<Sprite>(path);
        }

        private static Texture2D LoadTexture(string path)
        {
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
            TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer != null)
            {
                importer.textureType = TextureImporterType.Default;
                importer.filterMode = FilterMode.Bilinear;
                importer.mipmapEnabled = false;
                importer.alphaIsTransparency = true;
                importer.textureCompression = TextureImporterCompression.Uncompressed;
                importer.SaveAndReimport();
            }

            return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        }

        private static void CreateMainScene(
            GameContentCatalog catalog,
            UnitView unitPrefab,
            Sprite cellSprite,
            Sprite circleSprite)
        {
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            GameObject systems = new GameObject("Systems");
            GameObject gameplay = new GameObject("Gameplay");
            GameObject stripUi = CreateCanvasRoot("Strip UI", new Vector2(960, 176));
            GameObject managementUi = CreateCanvasRoot("Management UI", new Vector2(960, 640));
            GameObject audio = new GameObject("Audio");
            audio.AddComponent<AudioSource>();

            Camera camera = CreateCamera(gameplay.transform);
            CombatPresenter combatPresenter = CreateCombatArea(
                gameplay.transform, unitPrefab, cellSprite);

            GameObject windowObject = new GameObject("Window Mode");
            windowObject.transform.SetParent(systems.transform);
            WindowModeController window = windowObject.AddComponent<WindowModeController>();

            StripHudController strip = BuildStripUi(stripUi.transform);
            NodeTransitionPresenter nodeTransition = BuildNodeTransition(stripUi.transform, strip);
            TownIntroPresenter townIntro = BuildTownIntro(stripUi.transform);
            DefeatOverlayPresenter defeatOverlay = BuildDefeatOverlay(stripUi.transform);
            Sprite hudVerticalSprite = LoadUiSprite("Assets/Resources/UI/Square.png", new Vector4(54, 54, 54, 54));
            Sprite hudHorizontalSprite = LoadUiSprite("Assets/Resources/UI/Square.png", new Vector4(54, 54, 54, 54));
            Sprite mapPanelSprite = LoadUiSprite("Assets/Resources/UI/MapSquare.png", new Vector4(54, 54, 54, 54));
            Sprite mapFrameSprite = LoadUiSprite("Assets/Resources/UI/MapFrame.png", new Vector4(16, 16, 16, 16));
            Sprite actParchmentSprite = LoadUiSprite("Assets/Resources/UI/ActParchment.png", Vector4.zero);
            Sprite titleWideSprite = LoadUiSprite("Assets/Resources/UI/TitleWide.png", new Vector4(32, 32, 32, 32));
            Sprite commandSprite = LoadUiSprite("Assets/Resources/UI/Command.png", new Vector4(42, 42, 42, 42));
            Sprite inventoryVerticalSprite = LoadUiSprite("Assets/Resources/UI/Vertical.png", new Vector4(34, 34, 34, 34));
            Sprite equipLayoutSprite = LoadUiSprite("Assets/Resources/UI/EquipLayout.png", new Vector4(16, 16, 16, 16));
            Sprite itemSlotSprite = LoadSoftUiSprite("Assets/Resources/UI/itemSlot.png");
            Texture2D skillTreeTexture = LoadTexture("Assets/Resources/UI/SkillTree/SkillTree.png");
            Sprite formationSlotSprite = LoadSoftUiSprite("Assets/Resources/UI/Formations/slotFormation.png");
            Sprite formationSlotBlueSprite = LoadSoftUiSprite("Assets/Resources/UI/Formations/slotFormationblue.png");
            Sprite settingsButtonSprite = LoadUiSprite("Assets/Resources/UI/SettingsButton.png", Vector4.zero);
            Sprite closeButtonSprite = LoadUiSprite("Assets/Resources/UI/CloseButton.png", Vector4.zero);
            Sprite barButtonSprite = LoadUiSprite("Assets/Resources/UI/BarButton.png", Vector4.zero);
            Sprite[] formationHeroIcons = LoadFormationHeroIcons();
            Sprite emptyClassSprite = LoadSoftUiSprite("Assets/Resources/HeroClasses/empty.png");
            Sprite[] candleFrames = LoadCandleSprites();
            Sprite[] torchLightFrames = LoadTorchLightSprites();
            Sprite[] mapFlagFrames = LoadMapFlagSprites();
            ManagementUiController management = BuildManagementUi(
                managementUi.transform,
                circleSprite,
                hudVerticalSprite,
                hudHorizontalSprite,
                mapPanelSprite,
                mapFrameSprite,
                actParchmentSprite,
                titleWideSprite,
                commandSprite,
                inventoryVerticalSprite,
                equipLayoutSprite,
                itemSlotSprite,
                skillTreeTexture,
                formationSlotSprite,
                formationSlotBlueSprite,
                settingsButtonSprite,
                closeButtonSprite,
                barButtonSprite,
                formationHeroIcons,
                emptyClassSprite,
                candleFrames,
                torchLightFrames,
                mapFlagFrames);

            GameObject appObject = new GameObject("Game Application");
            appObject.transform.SetParent(systems.transform);
            GameAppController app = appObject.AddComponent<GameAppController>();
            app.Configure(catalog, combatPresenter, strip, management, window, townIntro, defeatOverlay, nodeTransition);
            window.Configure(stripUi, managementUi, camera);

            GameObject eventSystemObject = new GameObject("Event System");
            eventSystemObject.transform.SetParent(systems.transform);
            eventSystemObject.AddComponent<EventSystem>();
            eventSystemObject.AddComponent<InputSystemUIInputModule>();

            EditorSceneManager.SaveScene(scene, MainScenePath);
            EditorBuildSettings.scenes = new[]
            {
                new EditorBuildSettingsScene(MainScenePath, true)
            };
        }

        private static Camera CreateCamera(Transform parent)
        {
            GameObject cameraObject = new GameObject("Main Camera");
            cameraObject.transform.SetParent(parent);
            cameraObject.tag = "MainCamera";
            Camera camera = cameraObject.AddComponent<Camera>();
            camera.orthographic = true;
            // The Windows strip is 960x176, but Editor play mode needs a wider camera
            // to preview the battle composition before the native transparent window is applied.
            camera.orthographicSize = 1.8f;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(1, 0, 1, 1);
            camera.transform.position = new Vector3(0, 0, -10);
            camera.allowHDR = false;
            camera.allowMSAA = false;
            cameraObject.AddComponent<AudioListener>();
            return camera;
        }

        private static CombatPresenter CreateCombatArea(
            Transform parent,
            UnitView unitPrefab,
            Sprite cellSprite)
        {
            GameObject combatObject = new GameObject("Combat Presentation");
            combatObject.transform.SetParent(parent);
            CombatPresenter presenter = combatObject.AddComponent<CombatPresenter>();
            SpriteRenderer battleback = CreateBattleback(combatObject.transform);
            Transform heroGrid = new GameObject("Hero Grid").transform;
            heroGrid.SetParent(combatObject.transform);
            Transform enemyGrid = new GameObject("Enemy Grid").transform;
            enemyGrid.SetParent(combatObject.transform);
            List<Transform> heroCells = CreateGrid(
                heroGrid, -2.75f, 0.76f, 0.4f, cellSprite, new Color(0.08f, 0.18f, 0.25f), 0f, 4);
            List<Transform> enemyCells = CreateGrid(
                enemyGrid, 1.0f, 0.9f, 0.26f, cellSprite, new Color(0.25f, 0.08f, 0.1f), 0.28f);
            presenter.Configure(unitPrefab, heroCells, enemyCells, battleback, 4, 3);
            return presenter;
        }

        private static SpriteRenderer CreateBattleback(Transform parent)
        {
            GameObject battlebackObject = new GameObject("Battleback");
            battlebackObject.transform.SetParent(parent, false);
            battlebackObject.transform.localPosition = new Vector3(0f, 0f, 2f);
            SpriteRenderer renderer = battlebackObject.AddComponent<SpriteRenderer>();
            Texture2D texture = AssetDatabase.LoadAssetAtPath<Texture2D>(
                "Assets/Resources/Battlebacks/default.png");
            if (texture != null)
            {
                renderer.sprite = Sprite.Create(
                    texture,
                    new Rect(0f, 0f, texture.width, texture.height),
                    new Vector2(0.5f, 0.5f),
                    100f);
            }

            renderer.sortingOrder = -20;
            renderer.color = renderer.sprite == null ? Color.clear : Color.white;
            return renderer;
        }

        private static List<Transform> CreateGrid(
            Transform parent,
            float startX,
            float spacingX,
            float spacingY,
            Sprite sprite,
            Color color,
            float rowOffsetX = 0f,
            int columns = 3)
        {
            List<Transform> cells = new List<Transform>();
            for (int row = 0; row < 3; row++)
            {
                for (int column = 0; column < columns; column++)
                {
                    GameObject cell = new GameObject($"Cell {row},{column}");
                    cell.transform.SetParent(parent);
                    cell.transform.position = new Vector3(
                        startX + column * spacingX + row * rowOffsetX,
                        0.1f - row * spacingY,
                        1);
                    SpriteRenderer renderer = cell.AddComponent<SpriteRenderer>();
                    renderer.sprite = sprite;
                    renderer.color = color;
                    renderer.sortingOrder = 0;
                    cell.transform.localScale = new Vector3(1.25f, 1.25f, 1);
                    cells.Add(cell.transform);
                }
            }

            return cells;
        }

        private static GameObject CreateCanvasRoot(string name, Vector2 referenceResolution)
        {
            GameObject root = new GameObject(name, typeof(RectTransform), typeof(Canvas),
                typeof(CanvasScaler), typeof(GraphicRaycaster));
            Canvas canvas = root.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            CanvasScaler scaler = root.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = referenceResolution;
            scaler.matchWidthOrHeight = 0.5f;
            return root;
        }

        private static StripHudController BuildStripUi(Transform root)
        {
            StripHudController controller = root.gameObject.AddComponent<StripHudController>();
            Sprite closeButtonSprite = LoadUiSprite("Assets/Resources/UI/CloseButton.png", Vector4.zero);
            Image bar = CreateImage(root, "Status Bar", Panel);
            RectTransform barRect = bar.rectTransform;
            barRect.anchorMin = new Vector2(0, 0);
            barRect.anchorMax = new Vector2(1, 0);
            barRect.pivot = new Vector2(0.5f, 0);
            barRect.anchoredPosition = new Vector2(0, 48);
            barRect.sizeDelta = new Vector2(0, 44);
            TMP_Text status = CreateText(bar.transform, "Status Label", "Escuadrón en el campamento",
                16, TextAlignmentOptions.Left, new Vector2(106, 7), new Vector2(520, 28));
            TMP_Text node = CreateText(bar.transform, "Node Label", "Campamento",
                16, TextAlignmentOptions.Center, new Vector2(636, 7), new Vector2(190, 28));
            Button manage = CreateButton(root, "Manage Button", closeButtonSprite == null ? "X" : string.Empty,
                Vector2.zero, new Vector2(36, 36), closeButtonSprite == null ? new Color(0.82f, 0.12f, 0.12f) : Color.white);
            if (closeButtonSprite != null)
            {
                manage.image.sprite = closeButtonSprite;
                manage.image.type = Image.Type.Simple;
                manage.image.preserveAspect = true;
            }

            TMP_Text manageLabel = manage.GetComponentInChildren<TMP_Text>();
            if (manageLabel != null)
            {
                manageLabel.color = Color.black;
            }

            manage.image.rectTransform.anchorMin = manage.image.rectTransform.anchorMax = new Vector2(1, 1);
            manage.image.rectTransform.pivot = new Vector2(1, 1);
            manage.image.rectTransform.anchoredPosition = new Vector2(-8, -8);
            Button menu = CreateButton(bar.transform, "Menu Button", "•••",
                new Vector2(852, 5), new Vector2(48, 30), PanelLight);
            Image attention = CreateImage(root, "Attention Indicator", new Color(1f, 0.72f, 0.18f));
            attention.raycastTarget = false;
            attention.rectTransform.anchorMin = attention.rectTransform.anchorMax = new Vector2(1, 1);
            attention.rectTransform.pivot = new Vector2(1, 1);
            attention.rectTransform.anchoredPosition = new Vector2(-8, -8);
            attention.rectTransform.sizeDelta = new Vector2(16, 16);

            Image menuPanel = CreateImage(root, "Compact Menu", Panel);
            menuPanel.rectTransform.anchorMin = menuPanel.rectTransform.anchorMax = new Vector2(1, 0);
            menuPanel.rectTransform.pivot = new Vector2(1, 0);
            menuPanel.rectTransform.anchoredPosition = new Vector2(-8, 44);
            menuPanel.rectTransform.sizeDelta = new Vector2(150, 52);
            Button quit = CreateButton(menuPanel.transform, "Quit Button", "Salir",
                new Vector2(10, 9), new Vector2(130, 34), new Color(0.62f, 0.18f, 0.22f));
            menuPanel.gameObject.SetActive(false);
            attention.gameObject.SetActive(false);
            manage.transform.SetAsLastSibling();
            controller.Configure(status, node, attention.gameObject, manage, menu, menuPanel.gameObject, quit);
            return controller;
        }

        private static TownIntroPresenter BuildTownIntro(Transform root)
        {
            Sprite backgroundSprite = LoadUiSprite("Assets/Resources/Events/Town/town1.png", Vector4.zero);
            Sprite gateSprite = LoadPixelUiSprite("Assets/Resources/Events/Town/rejatown.png");
            AssetDatabase.ImportAsset(
                "Assets/Resources/Audio/Events/gate_open.ogg",
                ImportAssetOptions.ForceSynchronousImport);
            AudioClip gateOpenClip = AssetDatabase.LoadAssetAtPath<AudioClip>(
                "Assets/Resources/Audio/Events/gate_open.ogg");

            GameObject overlay = new GameObject(
                "Town Intro Overlay",
                typeof(RectTransform),
                typeof(CanvasGroup),
                typeof(RectMask2D));
            overlay.transform.SetParent(root, false);
            RectTransform overlayRect = overlay.GetComponent<RectTransform>();
            overlayRect.anchorMin = Vector2.zero;
            overlayRect.anchorMax = Vector2.one;
            overlayRect.offsetMin = Vector2.zero;
            overlayRect.offsetMax = Vector2.zero;

            Image background = CreateImage(overlay.transform, "Town Background", Color.white);
            background.sprite = backgroundSprite;
            background.type = Image.Type.Simple;
            background.preserveAspect = true;
            background.raycastTarget = false;
            background.rectTransform.anchorMin = background.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            background.rectTransform.pivot = new Vector2(0.5f, 0.5f);
            background.rectTransform.anchoredPosition = new Vector2(0f, -2f);
            background.rectTransform.sizeDelta = new Vector2(620f, 252f);

            Image gate = CreateImage(overlay.transform, "Gate", Color.white);
            gate.sprite = gateSprite;
            gate.type = Image.Type.Simple;
            gate.preserveAspect = true;
            gate.raycastTarget = false;
            gate.rectTransform.anchorMin = gate.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            gate.rectTransform.pivot = new Vector2(0.5f, 0.5f);
            gate.rectTransform.anchoredPosition = new Vector2(18f, -7f);
            gate.rectTransform.sizeDelta = new Vector2(259f, 116f);

            GameObject heroRoot = new GameObject("Hero Runners", typeof(RectTransform));
            heroRoot.transform.SetParent(overlay.transform, false);
            RectTransform heroRect = heroRoot.GetComponent<RectTransform>();
            heroRect.anchorMin = Vector2.zero;
            heroRect.anchorMax = Vector2.one;
            heroRect.offsetMin = Vector2.zero;
            heroRect.offsetMax = Vector2.zero;
            heroRoot.transform.SetSiblingIndex(gate.transform.GetSiblingIndex());

            AudioSource source = overlay.AddComponent<AudioSource>();
            source.playOnAwake = false;
            source.loop = false;
            source.spatialBlend = 0f;
            source.volume = 0.85f;

            TownIntroPresenter presenter = overlay.AddComponent<TownIntroPresenter>();
            presenter.Configure(
                overlay.GetComponent<CanvasGroup>(),
                background,
                gate.rectTransform,
                heroRect,
                source,
                gateOpenClip);
            return presenter;
        }

        private static NodeTransitionPresenter BuildNodeTransition(
            Transform root,
            StripHudController strip)
        {
            Sprite transitionSprite = LoadUiSprite("Assets/Resources/Events/transition.png", Vector4.zero);
            GameObject overlay = new GameObject(
                "Node Transition Overlay",
                typeof(RectTransform),
                typeof(CanvasGroup));
            overlay.transform.SetParent(root, false);
            RectTransform overlayRect = overlay.GetComponent<RectTransform>();
            overlayRect.anchorMin = Vector2.zero;
            overlayRect.anchorMax = Vector2.one;
            overlayRect.offsetMin = Vector2.zero;
            overlayRect.offsetMax = Vector2.zero;

            Image banner = CreateImage(overlay.transform, "Transition Banner", Color.white);
            banner.sprite = transitionSprite;
            banner.type = Image.Type.Simple;
            banner.preserveAspect = false;
            banner.raycastTarget = false;
            banner.rectTransform.anchorMin = banner.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            banner.rectTransform.pivot = new Vector2(0.5f, 0.5f);
            banner.rectTransform.anchoredPosition = new Vector2(-960f, 0f);
            banner.rectTransform.sizeDelta = new Vector2(960f, 180f);

            TMP_Text title = CreateText(
                banner.transform,
                "Transition Node Name",
                "Cueva",
                24,
                TextAlignmentOptions.Center,
                Vector2.zero,
                new Vector2(620f, 42f));
            title.rectTransform.anchorMin = title.rectTransform.anchorMax = new Vector2(0.5f, 0.55f);
            title.rectTransform.pivot = new Vector2(0.5f, 0.5f);
            title.rectTransform.anchoredPosition = new Vector2(0f, 10f);
            title.rectTransform.sizeDelta = new Vector2(620f, 42f);

            TMP_Text subtitle = CreateText(
                banner.transform,
                "Transition Node Level",
                "Acto I · Nivel 3",
                14,
                TextAlignmentOptions.Center,
                Vector2.zero,
                new Vector2(520f, 30f));
            subtitle.rectTransform.anchorMin = subtitle.rectTransform.anchorMax = new Vector2(0.5f, 0.45f);
            subtitle.rectTransform.pivot = new Vector2(0.5f, 0.5f);
            subtitle.rectTransform.anchoredPosition = new Vector2(0f, -20f);
            subtitle.rectTransform.sizeDelta = new Vector2(520f, 30f);

            NodeTransitionPresenter presenter = overlay.AddComponent<NodeTransitionPresenter>();
            presenter.Configure(
                overlay.GetComponent<CanvasGroup>(),
                banner.rectTransform,
                title,
                subtitle,
                strip);
            overlay.transform.SetAsLastSibling();
            return presenter;
        }

        private static DefeatOverlayPresenter BuildDefeatOverlay(Transform root)
        {
            Image overlay = CreateImage(root, "Defeat Overlay", new Color(0f, 0f, 0f, 0.5f));
            overlay.raycastTarget = true;
            RectTransform rect = overlay.rectTransform;
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            CanvasGroup group = overlay.gameObject.AddComponent<CanvasGroup>();

            TMP_Text label = CreateText(
                overlay.transform,
                "You Died Label",
                "YOU DIED",
                42,
                TextAlignmentOptions.Center,
                Vector2.zero,
                new Vector2(420, 76));
            label.fontStyle = FontStyles.Bold;
            label.color = new Color(0.9f, 0.05f, 0.04f, 1f);

            DefeatOverlayPresenter presenter = overlay.gameObject.AddComponent<DefeatOverlayPresenter>();
            presenter.Configure(group, label);
            overlay.transform.SetAsLastSibling();
            return presenter;
        }

        private static ManagementUiController BuildManagementUi(
            Transform root,
            Sprite circleSprite,
            Sprite hudVerticalSprite,
            Sprite hudHorizontalSprite,
            Sprite mapPanelSprite,
            Sprite mapFrameSprite,
            Sprite actParchmentSprite,
            Sprite titleWideSprite,
            Sprite commandSprite,
            Sprite inventoryVerticalSprite,
            Sprite equipLayoutSprite,
            Sprite itemSlotSprite,
            Texture2D skillTreeTexture,
            Sprite formationSlotSprite,
            Sprite formationSlotBlueSprite,
            Sprite settingsButtonSprite,
            Sprite closeButtonSprite,
            Sprite barButtonSprite,
            Sprite[] formationHeroIcons,
            Sprite emptyClassSprite,
            Sprite[] candleFrames,
            Sprite[] torchLightFrames,
            Sprite[] mapFlagFrames)
        {
            Image background = CreateImage(root, "Management Background", Background);
            ApplyUiFrame(background, hudHorizontalSprite);
            background.color = new Color(0.85f, 0.85f, 0.85f, 1f);
            SetStretch(background.rectTransform, 0, 0, 0, 0);
            AddTorchShadowDim(background.transform);
            AddCandlePair(background.transform, candleFrames, new Vector2(12, 18), new Vector2(-12, 18));
            Image titleFrame = CreateImage(background.transform, "Title Frame", Color.white);
            ApplySimpleWideFrame(titleFrame, titleWideSprite);
            titleFrame.rectTransform.anchorMin = titleFrame.rectTransform.anchorMax = new Vector2(0, 1);
            titleFrame.rectTransform.pivot = new Vector2(0, 1);
            titleFrame.rectTransform.anchoredPosition = new Vector2(337, -10);
            titleFrame.rectTransform.sizeDelta = new Vector2(342, 60);
            TMP_Text title = CreateText(background.transform, "Title", "TASKBAR TACTICS",
                26, TextAlignmentOptions.Center, new Vector2(362, 24), new Vector2(292, 32));
            title.fontStyle = FontStyles.Bold;
            title.textWrappingMode = TextWrappingModes.NoWrap;
            Button close = CreateButton(background.transform, "Close Button", string.Empty,
                new Vector2(857, 12), new Vector2(48.3f, 48.3f), Color.white);
            ApplyIconButton(close, barButtonSprite);
            Button settingsShortcut = CreateButton(background.transform, "Settings Shortcut Button", string.Empty,
                new Vector2(910, 12), new Vector2(46, 46), Color.white);
            ApplyIconButton(settingsShortcut, settingsButtonSprite);
            Button quitShortcut = CreateButton(background.transform, "Quit Shortcut Button", string.Empty,
                new Vector2(962, 12), new Vector2(46, 46), Color.white);
            ApplyIconButton(quitShortcut, closeButtonSprite);

            string[] tabNames =
            {
                "Party", "Skills", "Inventory", "Map"
            };
            string[] panelNames =
            {
                "Escuadrón", "Habilidades", "Inventario", "Mapa", "Ajustes"
            };
            List<Button> tabs = new List<Button>();
            for (int i = 0; i < tabNames.Length; i++)
            {
                tabs.Add(CreateButton(background.transform, $"{tabNames[i]} Tab", tabNames[i],
                    new Vector2(33, 150 + i * 62), new Vector2(150.4f, 54.4f), Color.white));
                ApplyUiFrame(tabs[i].GetComponent<Image>(), commandSprite);
                InsetButtonLabel(tabs[i], new Vector2(34, 16), new Vector2(-34, -16));
            }

            List<GameObject> panels = new List<GameObject>();
            List<TMP_Text> summaries = new List<TMP_Text>();
            for (int i = 0; i < panelNames.Length; i++)
            {
                bool panelUsesOnlyMainBackground = i == 0 || i == 1 || i == 4;
                Image panel = CreateImage(background.transform, i == 0 ? "Squad Panel" : $"{panelNames[i]} Panel", panelUsesOnlyMainBackground ? Color.clear : Color.white);
                if (!panelUsesOnlyMainBackground)
                {
                    Sprite panelSprite = i == 2
                        ? inventoryVerticalSprite
                        : i == 3 && mapPanelSprite != null
                            ? mapPanelSprite
                            : hudHorizontalSprite;
                    ApplyUiFrame(panel, panelSprite);
                    panel.color = new Color(0.9f, 0.9f, 0.9f, 1f);
                }
                panel.rectTransform.anchorMin = new Vector2(0, 0);
                panel.rectTransform.anchorMax = new Vector2(1, 1);
                panel.rectTransform.offsetMin = new Vector2(200, 24);
                panel.rectTransform.offsetMax = new Vector2(-24, -76);
                if (i == 2)
                {
                    panel.rectTransform.anchorMin = new Vector2(0, 0);
                    panel.rectTransform.anchorMax = new Vector2(0, 1);
                    panel.rectTransform.offsetMin = new Vector2(220, 24);
                    panel.rectTransform.offsetMax = new Vector2(610, -76);
                }
                if (i != 0 && i != 1 && i != 2 && i != 3 && i != 4)
                {
                    CreateText(panel.transform, "Panel Title", panelNames[i].ToUpperInvariant(),
                        24, TextAlignmentOptions.Left, new Vector2(24, 18), new Vector2(500, 40));
                }
                Vector2 summaryPosition = i == 0
                    ? new Vector2(24, 42)
                    : i == 3
                        ? new Vector2(58, 72)
                        : new Vector2(24, 72);
                TMP_Text summary = CreateText(panel.transform, "Summary", string.Empty,
                    18, TextAlignmentOptions.TopLeft, summaryPosition, new Vector2(690, 360));
                summary.textWrappingMode = TextWrappingModes.Normal;
                panels.Add(panel.gameObject);
                summaries.Add(summary);
                panel.gameObject.SetActive(i == 0);
            }
            AddTorchShadowDim(panels[0].transform);
            AddTorchShadowDim(panels[1].transform);
            AddTorchShadowDim(panels[2].transform);
            AddTorchShadowDim(panels[3].transform);
            AddCandlePair(panels[0].transform, candleFrames, new Vector2(10, 8), new Vector2(-24, 8), torchLightFrames);
            AddCandlePair(panels[1].transform, candleFrames, new Vector2(10, 8), new Vector2(-24, 8), torchLightFrames);
            AddCandle(panels[2].transform, "Left Candle", candleFrames, new Vector2(0, 1), new Vector2(0, 1), new Vector2(10, 8), torchLightFrames);
            AddCandlePair(panels[3].transform, candleFrames, new Vector2(10, 8), new Vector2(-24, 8), torchLightFrames);
            summaries[1].gameObject.SetActive(false);
            CreateSkillTreeView(panels[1].transform, skillTreeTexture, mapFrameSprite);
            CreateEquipmentPreviewLayout(panels[2].transform, equipLayoutSprite);
            summaries[2].gameObject.SetActive(false);
            CreateInventorySlotGrid(panels[2].transform, itemSlotSprite);
            AddCenteredCandle(panels[2].transform, "Right Candle", candleFrames, new Vector2(560, -56), torchLightFrames);
            List<FormationSlotView> formationSlots = CreateFormationSlotHud(
                panels[0].transform,
                formationSlotSprite,
                formationSlotBlueSprite);

            List<Button> heroButtons = new List<Button>();
            for (int i = 0; i < 6; i++)
            {
                heroButtons.Add(CreateHeroClassButton(panels[0].transform, i, formationHeroIcons, emptyClassSprite));
            }

            List<Button> formationButtons = CreateFormationPresetButtons(panels[0].transform);

            Button cycleActive = null;
            Button cyclePassive = null;

            List<Button> equipButtons = new List<Button>();

            List<Button> routeButtons = new List<Button>
            {
                CreateButton(panels[3].transform, "Safety Route Button", "Seguridad",
                    new Vector2(54, 200), new Vector2(188, 68), PanelLight),
                CreateButton(panels[3].transform, "Loot Route Button", "Botín",
                    new Vector2(54, 262), new Vector2(188, 68), PanelLight),
                CreateButton(panels[3].transform, "Challenge Route Button", "Desafío",
                    new Vector2(54, 324), new Vector2(188, 68), PanelLight)
            };
            foreach (Button routeButton in routeButtons)
            {
                ApplyUiFrame(routeButton.GetComponent<Image>(), commandSprite);
            }

            Button start = CreateButton(panels[3].transform, "Start Expedition Button",
                "INICIAR EXPEDICIÓN", new Vector2(54, 378), new Vector2(150, 52), Accent);
            ApplyUiFrame(start.GetComponent<Image>(), commandSprite);
            start.GetComponent<RectTransform>().sizeDelta = new Vector2(188, 68);
            TMP_Text startLabel = start.GetComponentInChildren<TMP_Text>();
            if (startLabel != null)
            {
                startLabel.color = new Color(1f, 0.92f, 0.08f, 1f);
            }

            MapUiController mapVisual = BuildMapVisual(
                panels[3].transform, summaries[3], circleSprite, mapFlagFrames, mapFrameSprite, actParchmentSprite);

            Button language = CreateButton(panels[4].transform, "Language Button",
                "Cambiar ES / EN", new Vector2(24, 300), new Vector2(220, 44), Accent);
            Button reset = CreateButton(panels[4].transform, "Reset Expedition Button",
                "RESET", new Vector2(232, 300), new Vector2(188, 68),
                new Color(0.82f, 0.12f, 0.12f));
            Button quit = CreateButton(panels[4].transform, "Quit Button",
                "Salir del juego", new Vector2(260, 300), new Vector2(220, 44),
                new Color(0.62f, 0.18f, 0.22f));
            ApplyUiFrame(language.GetComponent<Image>(), commandSprite);
            ApplyUiFrame(reset.GetComponent<Image>(), commandSprite);
            ApplyUiFrame(quit.GetComponent<Image>(), commandSprite);
            language.GetComponent<RectTransform>().sizeDelta = new Vector2(188, 68);
            reset.GetComponent<RectTransform>().anchoredPosition = new Vector2(232, -300);
            reset.GetComponent<RectTransform>().sizeDelta = new Vector2(188, 68);
            quit.GetComponent<RectTransform>().anchoredPosition = new Vector2(24, -370);
            quit.GetComponent<RectTransform>().sizeDelta = new Vector2(188, 68);

            ApplyManagementUiSkin(background.transform, hudHorizontalSprite, commandSprite);
            ApplySimpleCommandFrame(cycleActive, commandSprite);
            ApplySimpleCommandFrame(cyclePassive, commandSprite);
            foreach (Button equipButton in equipButtons)
            {
                ApplySimpleCommandFrame(equipButton, commandSprite);
            }

            foreach (Button routeButton in routeButtons)
            {
                ApplySimpleCommandFrame(routeButton, commandSprite);
            }

            ApplySimpleCommandFrame(start, commandSprite);
            ApplySimpleCommandFrame(language, commandSprite);
            ApplySimpleCommandFrame(reset, commandSprite);
            reset.GetComponent<Image>().color = new Color(0.82f, 0.12f, 0.12f);
            TMP_Text resetLabel = reset.GetComponentInChildren<TMP_Text>();
            resetLabel.color = Color.black;
            resetLabel.fontSize = 18;
            resetLabel.fontStyle = FontStyles.Bold;
            InsetButtonLabel(reset, new Vector2(34, 16), new Vector2(-34, -16));
            ApplySimpleCommandFrame(quit, commandSprite);
            reset.GetComponent<CanvasRenderer>().SetAlpha(1f);
            reset.transform.SetAsLastSibling();
            reset.image.transform.SetAsLastSibling();

            ManagementUiController controller =
                root.gameObject.AddComponent<ManagementUiController>();
            controller.Configure(
                tabs,
                panels,
                close,
                settingsShortcut,
                quitShortcut,
                heroButtons,
                formationButtons,
                formationSlots,
                formationHeroIcons,
                summaries[0],
                summaries[1],
                null,
                summaries[2],
                summaries[3],
                summaries[4],
                mapVisual,
                cycleActive,
                cyclePassive,
                equipButtons,
                routeButtons,
                start,
                reset,
                language,
                quit);
            return controller;
        }

        private static List<Button> CreateFormationPresetButtons(Transform parent)
        {
            string[] labels = { "4+", "3+1", "2+2" };
            string[] spritePaths =
            {
                "Assets/Resources/UI/Formations/form_line4.png",
                "Assets/Resources/UI/Formations/form_1_plus_3.png",
                "Assets/Resources/UI/Formations/form_2_plus_2.png"
            };

            List<Button> buttons = new List<Button>();
            for (int i = 0; i < labels.Length; i++)
            {
                Button button = CreateButton(parent, $"Formation Preset {labels[i]} Button", string.Empty,
                    new Vector2(470 + i * 92, 90), new Vector2(72, 72), Color.clear);
                Image buttonImage = button.GetComponent<Image>();
                buttonImage.color = Color.clear;
                ColorBlock colors = button.colors;
                colors.normalColor = Color.clear;
                colors.highlightedColor = Color.clear;
                colors.pressedColor = Color.clear;
                colors.selectedColor = Color.clear;
                button.colors = colors;

                Image highlight = CreateImage(button.transform, "Highlight", new Color(0.25f, 0.86f, 1f, 0.55f));
                highlight.raycastTarget = false;
                highlight.rectTransform.anchorMin = highlight.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
                highlight.rectTransform.pivot = new Vector2(0.5f, 0.5f);
                highlight.rectTransform.anchoredPosition = new Vector2(0, 20);
                highlight.rectTransform.sizeDelta = new Vector2(60, 60);
                highlight.gameObject.SetActive(i == 0);

                Sprite iconSprite = LoadPixelUiSprite(spritePaths[i]);
                Image icon = CreateImage(button.transform, "Icon", Color.white);
                icon.sprite = iconSprite;
                icon.type = Image.Type.Simple;
                icon.preserveAspect = true;
                icon.raycastTarget = false;
                icon.rectTransform.anchorMin = icon.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
                icon.rectTransform.pivot = new Vector2(0.5f, 0.5f);
                icon.rectTransform.anchoredPosition = new Vector2(0, 20);
                icon.rectTransform.sizeDelta = new Vector2(54, 54);
                buttons.Add(button);
            }

            return buttons;
        }

        private static List<FormationSlotView> CreateFormationSlotHud(
            Transform parent,
            Sprite slotSprite,
            Sprite frontSlotSprite)
        {
            FormationPosition[] positions =
            {
                new FormationPosition(1, 0),
                new FormationPosition(1, 1),
                new FormationPosition(1, 2),
                new FormationPosition(1, 3)
            };
            List<FormationSlotView> slots = new List<FormationSlotView>();
            for (int i = 0; i < positions.Length; i++)
            {
                Image slot = CreateImage(parent, $"Formation Slot {i + 1}", Color.white);
                slot.sprite = slotSprite;
                slot.type = Image.Type.Simple;
                slot.preserveAspect = true;
                slot.raycastTarget = true;
                slot.rectTransform.anchorMin = slot.rectTransform.anchorMax = new Vector2(0, 1);
                slot.rectTransform.pivot = new Vector2(0.5f, 0.5f);
                slot.rectTransform.anchoredPosition = FormationSlotPosition(positions[i]);
                slot.rectTransform.sizeDelta = new Vector2(72, 72);

                Image hero = CreateImage(slot.transform, "Hero Icon", Color.clear);
                hero.preserveAspect = true;
                hero.raycastTarget = false;
                hero.rectTransform.anchorMin = hero.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
                hero.rectTransform.pivot = new Vector2(0.5f, 0.5f);
                hero.rectTransform.anchoredPosition = Vector2.zero;
                hero.rectTransform.sizeDelta = new Vector2(52, 52);

                FormationSlotView slotView = slot.gameObject.AddComponent<FormationSlotView>();
                slotView.Configure(null, slot, hero, positions[i], slotSprite, frontSlotSprite);
                slots.Add(slotView);
            }

            return slots;
        }

        private static Vector2 FormationSlotPosition(FormationPosition position)
        {
            const float startX = 84f;
            const float startY = -38f;
            const float gap = 60f;
            return new Vector2(startX + position.Column * gap, startY - position.Row * gap);
        }

        private static void CreateInventorySlotGrid(Transform parent, Sprite itemSlotSprite)
        {
            const int columns = 4;
            const int rows = 5;
            const float slotSize = 58f;
            const float gap = 66f;
            Vector2 start = new Vector2(86f, -113f);
            List<Image> slots = new List<Image>();

            for (int row = 0; row < rows; row++)
            {
                for (int column = 0; column < columns; column++)
                {
                    int index = row * columns + column + 1;
                    Image slot = CreateImage(parent, $"Inventory Slot {index:00}", Color.white);
                    slot.sprite = itemSlotSprite;
                    slot.type = Image.Type.Simple;
                    slot.preserveAspect = true;
                    slot.raycastTarget = false;
                    slot.rectTransform.anchorMin = slot.rectTransform.anchorMax = new Vector2(0, 1);
                    slot.rectTransform.pivot = new Vector2(0.5f, 0.5f);
                    slot.rectTransform.anchoredPosition = start + new Vector2(column * gap, -row * gap);
                    slot.rectTransform.sizeDelta = new Vector2(slotSize, slotSize);
                    slots.Add(slot);
                }
            }

            parent.gameObject.AddComponent<InventorySlotGridView>().Configure(slots);
        }

        private static void CreateEquipmentPreviewLayout(Transform parent, Sprite equipLayoutSprite)
        {
            if (equipLayoutSprite == null)
            {
                return;
            }

            Image layout = CreateImage(parent, "Equipment Preview Layout", Color.white);
            layout.sprite = equipLayoutSprite;
            layout.type = Image.Type.Sliced;
            layout.preserveAspect = false;
            layout.raycastTarget = false;
            layout.rectTransform.anchorMin = layout.rectTransform.anchorMax = new Vector2(0, 1);
            layout.rectTransform.pivot = new Vector2(0.5f, 0.5f);
            layout.rectTransform.anchoredPosition = new Vector2(560f, -245f);
            layout.rectTransform.sizeDelta = new Vector2(297f, 330f);
            layout.gameObject.AddComponent<EquipmentPreviewLayoutView>();
        }

        private static void CreateSkillTreeView(Transform parent, Texture2D skillTreeTexture, Sprite mapFrameSprite)
        {
            GameObject viewportObject = new GameObject("Skill Tree Viewport", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Mask));
            viewportObject.transform.SetParent(parent, false);
            RectTransform viewport = viewportObject.GetComponent<RectTransform>();
            viewport.anchorMin = new Vector2(0, 0);
            viewport.anchorMax = new Vector2(1, 1);
            viewport.offsetMin = new Vector2(18, 24);
            viewport.offsetMax = new Vector2(-18, -24);

            Image maskImage = viewportObject.GetComponent<Image>();
            maskImage.color = new Color(0f, 0f, 0f, 0.02f);
            Mask mask = viewportObject.GetComponent<Mask>();
            mask.showMaskGraphic = false;

            GameObject contentObject = new GameObject("Skill Tree Content", typeof(RectTransform));
            contentObject.transform.SetParent(viewportObject.transform, false);
            RectTransform content = contentObject.GetComponent<RectTransform>();
            content.anchorMin = content.anchorMax = new Vector2(0, 1);
            content.pivot = new Vector2(0, 1);
            content.anchoredPosition = Vector2.zero;

            GameObject imageObject = new GameObject("Skill Tree Image", typeof(RectTransform), typeof(CanvasRenderer), typeof(RawImage));
            imageObject.transform.SetParent(contentObject.transform, false);
            RawImage image = imageObject.GetComponent<RawImage>();
            image.texture = skillTreeTexture;
            image.color = Color.white;
            image.raycastTarget = false;
            RectTransform imageRect = image.rectTransform;
            imageRect.anchorMin = imageRect.anchorMax = new Vector2(0, 1);
            imageRect.pivot = new Vector2(0, 1);
            imageRect.anchoredPosition = Vector2.zero;
            Vector2 imageSize = skillTreeTexture != null
                ? new Vector2(skillTreeTexture.width, skillTreeTexture.height) * 0.5f
                : new Vector2(844, 563);
            imageRect.sizeDelta = imageSize;
            content.sizeDelta = imageSize;

            viewportObject.AddComponent<DraggableMapView>().ConfigureCentered(
                content,
                2.4f,
                0.65f,
                2.4f);

            AddSkillTreeFrame(parent, mapFrameSprite);
        }

        private static void CreateLegendBookRing(Transform parent)
        {
            Sprite slotSprite = LoadUiSprite("Assets/Resources/UI/Legends/bslot.png", new Vector4(8, 8, 8, 8));
            GameObject ring = new GameObject("Legend Book Ring", typeof(RectTransform));
            ring.transform.SetParent(parent, false);
            RectTransform ringRect = ring.GetComponent<RectTransform>();
            ringRect.anchorMin = ringRect.anchorMax = new Vector2(0.5f, 0.5f);
            ringRect.pivot = new Vector2(0.5f, 0.5f);
            ringRect.anchoredPosition = new Vector2(-70f, -2f);
            ringRect.sizeDelta = new Vector2(360f, 300f);

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
            string[] names =
            {
                "Player",
                "Guardian",
                "Rogue",
                "Spell Blade",
                "Cleric",
                "Archer",
                "Mage"
            };

            for (int i = 0; i < positions.Length; i++)
            {
                GameObject root = new GameObject($"Legend Book {i}", typeof(RectTransform));
                root.transform.SetParent(ring.transform, false);
                RectTransform rootRect = root.GetComponent<RectTransform>();
                rootRect.anchorMin = rootRect.anchorMax = new Vector2(0.5f, 0.5f);
                rootRect.pivot = new Vector2(0.5f, 0.5f);
                rootRect.anchoredPosition = positions[i];
                rootRect.sizeDelta = new Vector2(82f, 82f);

                Image slot = CreateImage(root.transform, "Book Slot", Color.white);
                slot.sprite = slotSprite;
                slot.type = slotSprite != null ? Image.Type.Sliced : Image.Type.Simple;
                slot.preserveAspect = false;
                slot.raycastTarget = false;
                slot.rectTransform.anchorMin = slot.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
                slot.rectTransform.pivot = new Vector2(0.5f, 0.5f);
                slot.rectTransform.anchoredPosition = Vector2.zero;
                slot.rectTransform.sizeDelta = new Vector2(76f, 76f);

                Image book = CreateImage(root.transform, "Book Icon", Color.white);
                book.sprite = LoadUiSprite($"Assets/Resources/UI/Legends/b{i}.png", Vector4.zero);
                book.type = Image.Type.Simple;
                book.preserveAspect = true;
                book.raycastTarget = false;
                book.rectTransform.anchorMin = book.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
                book.rectTransform.pivot = new Vector2(0.5f, 0.5f);
                book.rectTransform.anchoredPosition = new Vector2(0f, 2f);
                book.rectTransform.sizeDelta = new Vector2(58f, 68f);

                Image labelBackground = CreateImage(root.transform, "Tooltip Background", new Color(0f, 0f, 0f, 0.55f));
                labelBackground.raycastTarget = false;
                labelBackground.rectTransform.anchorMin = labelBackground.rectTransform.anchorMax =
                    new Vector2(0.5f, 0.5f);
                labelBackground.rectTransform.pivot = new Vector2(0.5f, 0.5f);
                labelBackground.rectTransform.anchoredPosition = new Vector2(0f, -52f);
                labelBackground.rectTransform.sizeDelta = new Vector2(112f, 30f);
                labelBackground.gameObject.SetActive(false);

                TMP_Text label = CreateText(root.transform, "Label", names[i], 10,
                    TextAlignmentOptions.Center, new Vector2(-56f, 37f), new Vector2(112f, 24f));
                label.rectTransform.anchorMin = label.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
                label.rectTransform.pivot = new Vector2(0.5f, 0.5f);
                label.rectTransform.anchoredPosition = new Vector2(0f, -52f);
                label.color = new Color(1f, 0.95f, 0.78f, 1f);
                label.gameObject.SetActive(false);

                Image hoverArea = CreateImage(root.transform, "Hover Area", new Color(1f, 1f, 1f, 0f));
                hoverArea.raycastTarget = true;
                hoverArea.rectTransform.anchorMin = hoverArea.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
                hoverArea.rectTransform.pivot = new Vector2(0.5f, 0.5f);
                hoverArea.rectTransform.anchoredPosition = Vector2.zero;
                hoverArea.rectTransform.sizeDelta = new Vector2(82f, 82f);
                MapNodeHoverTooltip tooltip = hoverArea.gameObject.AddComponent<MapNodeHoverTooltip>();
                tooltip.Configure(label, labelBackground.gameObject);
            }
        }

        private static void AddSkillTreeFrame(Transform parent, Sprite frameSprite)
        {
            if (frameSprite == null)
            {
                return;
            }

            Image frame = CreateImage(parent, "Skill Tree Frame", Color.white);
            frame.sprite = frameSprite;
            frame.type = Image.Type.Sliced;
            frame.preserveAspect = false;
            frame.raycastTarget = false;
            RectTransform rect = frame.rectTransform;
            rect.anchorMin = new Vector2(0, 0);
            rect.anchorMax = new Vector2(1, 1);
            rect.offsetMin = new Vector2(18, 24);
            rect.offsetMax = new Vector2(-18, -24);
            rect.pivot = new Vector2(0.5f, 0.5f);
            frame.transform.SetAsLastSibling();
        }

        private static Button CreateHeroClassButton(
            Transform parent,
            int index,
            Sprite[] formationHeroIcons,
            Sprite emptyClassSprite)
        {
            string heroId = HeroIdForClassCard(index);
            Vector2 position = new Vector2(24 + (index % 3) * 132, 206 + (index / 3) * 132);
            Button button = CreateButton(parent, $"Hero Class {heroId} Button", string.Empty,
                position, new Vector2(108, 124), Color.clear);
            ColorBlock buttonColors = button.colors;
            buttonColors.normalColor = Color.clear;
            buttonColors.highlightedColor = Color.clear;
            buttonColors.pressedColor = Color.clear;
            buttonColors.selectedColor = Color.clear;
            button.colors = buttonColors;

            Image emptyBackground = CreateImage(button.transform, "Empty Class Frame", Color.white);
            emptyBackground.sprite = emptyClassSprite;
            emptyBackground.type = Image.Type.Simple;
            emptyBackground.preserveAspect = true;
            emptyBackground.raycastTarget = false;
            emptyBackground.rectTransform.anchorMin = emptyBackground.rectTransform.anchorMax = new Vector2(0.5f, 1f);
            emptyBackground.rectTransform.pivot = new Vector2(0.5f, 1f);
            emptyBackground.rectTransform.anchoredPosition = new Vector2(0, 0);
            emptyBackground.rectTransform.sizeDelta = new Vector2(111, 111);

            Image icon = CreateImage(button.transform, "Class Icon", Color.white);
            icon.sprite = LoadPixelUiSprite(
                $"Assets/Resources/HeroClasses/{LegacyHeroArtId(heroId)}.png");
            icon.type = Image.Type.Simple;
            icon.preserveAspect = true;
            icon.raycastTarget = false;
            icon.rectTransform.anchorMin = icon.rectTransform.anchorMax = new Vector2(0.5f, 1f);
            icon.rectTransform.pivot = new Vector2(0.5f, 1f);
            icon.rectTransform.anchoredPosition = new Vector2(0, -37);
            icon.rectTransform.sizeDelta = new Vector2(58, 58);

            TMP_Text label = CreateText(button.transform, "Class Name", heroId,
                9.5f, TextAlignmentOptions.Center, new Vector2(0, 102), new Vector2(108, 24));
            HeroClassCardView card = button.gameObject.AddComponent<HeroClassCardView>();
            card.Configure(icon, label, null, emptyBackground);
            Sprite dragSprite = index < formationHeroIcons.Length ? formationHeroIcons[index] : icon.sprite;
            button.gameObject.AddComponent<HeroDragSource>().Configure(heroId, dragSprite, null);
            return button;
        }

        private static string HeroIdForClassCard(int index)
        {
            string[] ids =
            {
                "warrior",
                "cleric",
                "mage",
                "archer",
                "rogue",
                "magic_warrior"
            };
            return ids[Mathf.Clamp(index, 0, ids.Length - 1)];
        }

        private static MapUiController BuildMapVisual(
            Transform parent,
            TMP_Text summary,
            Sprite circleSprite,
            Sprite[] flagFrames,
            Sprite mapFrameSprite,
            Sprite actParchmentSprite)
        {
            summary.rectTransform.sizeDelta = new Vector2(230, 150);
            summary.rectTransform.anchoredPosition = new Vector2(54, -72);

            GameObject root = new GameObject("Map Visual", typeof(RectTransform));
            root.transform.SetParent(parent, false);
            RectTransform rootRect = root.GetComponent<RectTransform>();
            rootRect.anchorMin = rootRect.anchorMax = new Vector2(0, 1);
            rootRect.pivot = new Vector2(0, 1);
            rootRect.anchoredPosition = new Vector2(280, -58);
            rootRect.sizeDelta = new Vector2(420, 386);
            Image viewport = root.AddComponent<Image>();
            viewport.color = new Color(0.03f, 0.04f, 0.05f, 0.95f);
            root.AddComponent<RectMask2D>();

            Texture2D mapTexture = AssetDatabase.LoadAssetAtPath<Texture2D>(
                "Assets/Resources/Maps/map1.png");
            RectTransform artworkRoot = CreateMapArtworkRoot(root.transform);
            RawImage background = CreateMapBackground(artworkRoot, mapTexture);

            RectTransform artworkLayer = CreateMapLayer(artworkRoot, "Artist Overlays");
            RectTransform routesLayer = CreateMapLayer(artworkRoot, "Routes");
            RectTransform nodesLayer = CreateMapLayer(artworkRoot, "Nodes");
            List<MapRouteArtworkOverlay> routeArtworkOverlays = CreateMapRouteArtworkOverlays(artworkLayer);

            Dictionary<string, Vector2> positions = new Dictionary<string, Vector2>
            {
                ["town"] = new Vector2(368, 605),
                ["narrow_bridge"] = new Vector2(302, 535),
                ["cave"] = new Vector2(324, 456),
                ["cemetery"] = new Vector2(203, 506),
                ["goblin_village"] = new Vector2(179, 386),
                ["tomb_pass"] = new Vector2(210, 266),
                ["mountain_pass"] = new Vector2(80, 294),
                ["lost_forest"] = new Vector2(116, 202),
                ["last_bastion"] = new Vector2(98, 115)
            };

            string[,] routeIds =
            {
                { "town", "narrow_bridge" },
                { "narrow_bridge", "cave" },
                { "narrow_bridge", "cemetery" },
                { "cave", "goblin_village" },
                { "cemetery", "goblin_village" },
                { "goblin_village", "tomb_pass" },
                { "goblin_village", "mountain_pass" },
                { "tomb_pass", "lost_forest" },
                { "mountain_pass", "lost_forest" },
                { "lost_forest", "last_bastion" }
            };

            List<MapRouteView> routes = new List<MapRouteView>();
            for (int i = 0; i < routeIds.GetLength(0); i++)
            {
                string from = routeIds[i, 0];
                string to = routeIds[i, 1];
                routes.Add(CreateMapRoute(routesLayer, from, to, positions[from], positions[to]));
            }

            List<MapNodeView> nodes = positions.Select(pair =>
                CreateMapNode(nodesLayer, pair.Key, pair.Value, circleSprite, flagFrames)).ToList();
            List<MapRoutePreferenceLegend> legends = CreateMapRouteLegend(root.transform);
            AddMapFrame(parent, mapFrameSprite, new Vector2(272, -50), new Vector2(436, 402));
            AddMapActSelector(parent, actParchmentSprite);

            MapUiController controller = root.AddComponent<MapUiController>();
            controller.Configure(
                background,
                nodes,
                routes,
                legends,
                null,
                routeArtworkOverlays);
            root.AddComponent<DraggableMapView>().Configure(artworkRoot);
            return controller;
        }

        private static void AddMapFrame(Transform parent, Sprite frameSprite, Vector2 position, Vector2 size)
        {
            if (frameSprite == null)
            {
                return;
            }

            Image frame = CreateImage(parent, "Map Frame", Color.white);
            frame.sprite = frameSprite;
            frame.type = Image.Type.Sliced;
            frame.preserveAspect = false;
            frame.raycastTarget = false;
            frame.rectTransform.anchorMin = frame.rectTransform.anchorMax = new Vector2(0, 1);
            frame.rectTransform.pivot = new Vector2(0, 1);
            frame.rectTransform.anchoredPosition = position;
            frame.rectTransform.sizeDelta = size;
            frame.transform.SetAsLastSibling();
        }

        private static void AddMapActSelector(Transform parent, Sprite badgeSprite)
        {
            if (badgeSprite == null)
            {
                return;
            }

            GameObject selector = new GameObject("Act Selector", typeof(RectTransform));
            selector.transform.SetParent(parent, false);
            RectTransform selectorRect = selector.GetComponent<RectTransform>();
            selectorRect.anchorMin = selectorRect.anchorMax = new Vector2(0, 1);
            selectorRect.pivot = new Vector2(0, 1);
            selectorRect.anchoredPosition = new Vector2(708, -72);
            selectorRect.sizeDelta = new Vector2(96, 100);

            AddMapActBadge(selector.transform, badgeSprite, "Act 1", Vector2.zero);
            AddMapActBadge(selector.transform, badgeSprite, "Act 2", new Vector2(0, -54));
            selector.transform.SetAsLastSibling();
        }

        private static void AddMapActBadge(
            Transform parent,
            Sprite badgeSprite,
            string actText,
            Vector2 position)
        {
            Image badge = CreateImage(parent, "Act Badge", Color.white);
            badge.name = $"{actText} Badge";
            badge.sprite = badgeSprite;
            badge.type = Image.Type.Simple;
            badge.preserveAspect = false;
            badge.raycastTarget = true;
            badge.gameObject.AddComponent<Button>();
            badge.color = new Color(1f, 1f, 1f, 0.88f);
            badge.rectTransform.anchorMin = badge.rectTransform.anchorMax = new Vector2(0, 1);
            badge.rectTransform.pivot = new Vector2(0, 1);
            badge.rectTransform.anchoredPosition = position;
            badge.rectTransform.sizeDelta = new Vector2(92, 38);

            TMP_Text label = CreateText(
                badge.transform,
                "Act Label",
                actText,
                15,
                TextAlignmentOptions.Center,
                Vector2.zero,
                new Vector2(66, 21));
            label.color = new Color(0.2f, 0.1f, 0.03f, 1f);
            label.fontStyle = FontStyles.Bold;
            label.rectTransform.anchorMin = label.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            label.rectTransform.pivot = new Vector2(0.5f, 0.5f);
            label.rectTransform.anchoredPosition = new Vector2(-4, -2);
            label.rectTransform.sizeDelta = new Vector2(66, 21);
        }

        private static RectTransform CreateMapArtworkRoot(Transform parent)
        {
            GameObject root = new GameObject("Map Artwork", typeof(RectTransform));
            root.transform.SetParent(parent, false);
            RectTransform rect = root.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = new Vector2(0, 1);
            rect.pivot = new Vector2(0, 1);
            rect.anchoredPosition = new Vector2(0f, 148f);
            rect.sizeDelta = new Vector2(520, 650);
            return rect;
        }

        private static RawImage CreateMapBackground(Transform parent, Texture2D texture)
        {
            GameObject backgroundObject = new GameObject(
                "Map Background", typeof(RectTransform), typeof(CanvasRenderer), typeof(RawImage));
            backgroundObject.transform.SetParent(parent, false);
            RectTransform rect = backgroundObject.GetComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            RawImage background = backgroundObject.GetComponent<RawImage>();
            background.texture = texture;
            background.color = Color.white;
            return background;
        }

        private static List<MapRouteArtworkOverlay> CreateMapRouteArtworkOverlays(Transform parent)
        {
            return new List<MapRouteArtworkOverlay>
            {
                new MapRouteArtworkOverlay
                {
                    PreferenceName = "Safety",
                    Image = CreateMapArtworkOverlay(parent, "Safety Route Artwork", "Maps/map1_route_safety")
                },
                new MapRouteArtworkOverlay
                {
                    PreferenceName = "Loot",
                    Image = CreateMapArtworkOverlay(parent, "Loot Route Artwork", "Maps/map1_route_loot")
                }
            };
        }

        private static RawImage CreateMapArtworkOverlay(Transform parent, string name, string resourcePath)
        {
            GameObject overlay = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(RawImage));
            overlay.transform.SetParent(parent, false);
            RectTransform rect = overlay.GetComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            RawImage image = overlay.GetComponent<RawImage>();
            image.texture = Resources.Load<Texture2D>(resourcePath);
            image.color = image.texture == null
                ? new Color(1f, 1f, 1f, 0f)
                : new Color(1f, 1f, 1f, 0.95f);
            overlay.SetActive(image.texture != null);
            return image;
        }

        private static List<MapRoutePreferenceLegend> CreateMapRouteLegend(Transform parent)
        {
            GameObject root = new GameObject("Route Preference Legend", typeof(RectTransform));
            root.transform.SetParent(parent, false);
            RectTransform rootRect = root.GetComponent<RectTransform>();
            rootRect.anchorMin = rootRect.anchorMax = new Vector2(0, 1);
            rootRect.pivot = new Vector2(0, 1);
            rootRect.anchoredPosition = new Vector2(14, -324);
            rootRect.sizeDelta = new Vector2(372, 58);

            string[] names =
            {
                "Safety: ruta segura",
                "Loot: busca tesoros",
                "Challenge: busca elites"
            };
            List<MapRoutePreferenceLegend> legends = new List<MapRoutePreferenceLegend>();
            for (int i = 0; i < names.Length; i++)
            {
                TMP_Text label = CreateText(root.transform, $"Legend {i + 1}", names[i], 10,
                    TextAlignmentOptions.Left, new Vector2(0, i * 18), new Vector2(260, 18));
                string preferenceName = names[i].Split(':')[0];
                legends.Add(new MapRoutePreferenceLegend
                {
                    PreferenceName = preferenceName,
                    Label = label
                });
            }

            return legends;
        }

        private static RectTransform CreateMapLayer(Transform parent, string name)
        {
            GameObject layer = new GameObject(name, typeof(RectTransform));
            layer.transform.SetParent(parent, false);
            RectTransform rect = layer.GetComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            return rect;
        }

        private static MapRouteView CreateMapRoute(
            Transform parent,
            string from,
            string to,
            Vector2 start,
            Vector2 end)
        {
            GameObject routeRoot = new GameObject($"{from} to {to}", typeof(RectTransform));
            routeRoot.transform.SetParent(parent, false);
            RectTransform routeRect = routeRoot.GetComponent<RectTransform>();
            routeRect.anchorMin = routeRect.anchorMax = new Vector2(0, 1);
            routeRect.pivot = new Vector2(0, 1);
            routeRect.anchoredPosition = Vector2.zero;
            routeRect.sizeDelta = Vector2.zero;

            Vector2 uiStart = new Vector2(start.x, -start.y);
            Vector2 uiEnd = new Vector2(end.x, -end.y);
            Vector2 delta = uiEnd - uiStart;
            float distance = delta.magnitude;
            Vector2 direction = delta.normalized;
            float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
            int dashCount = Mathf.Max(2, Mathf.FloorToInt(distance / 18f));
            List<Image> dashes = new List<Image>();
            for (int i = 0; i < dashCount; i++)
            {
                float t = dashCount == 1 ? 0.5f : i / (dashCount - 1f);
                Vector2 position = Vector2.Lerp(uiStart, uiEnd, t);
                Image dash = CreateImage(routeRoot.transform, $"Dash {i + 1:00}", new Color(1, 1, 1, 0.18f));
                RectTransform rect = dash.rectTransform;
                rect.anchorMin = rect.anchorMax = new Vector2(0, 1);
                rect.pivot = new Vector2(0.5f, 0.5f);
                rect.anchoredPosition = position;
                rect.sizeDelta = new Vector2(10, 3);
                rect.localRotation = Quaternion.Euler(0, 0, angle);
                dashes.Add(dash);
            }

            return new MapRouteView
            {
                FromNodeId = from,
                ToNodeId = to,
                Dashes = dashes
            };
        }

        private static MapNodeView CreateMapNode(
            Transform parent,
            string nodeId,
            Vector2 position,
            Sprite circleSprite,
            Sprite[] flagFrames)
        {
            const float nodeMarkerOffsetX = 71f;
            const float nodeMarkerOffsetY = 10f;
            Vector2 fineTune = MapNodeFineTune(nodeId);
            GameObject nodeRoot = new GameObject(nodeId, typeof(RectTransform));
            nodeRoot.transform.SetParent(parent, false);
            RectTransform nodeRect = nodeRoot.GetComponent<RectTransform>();
            nodeRect.anchorMin = nodeRect.anchorMax = new Vector2(0, 1);
            nodeRect.pivot = new Vector2(0.5f, 0.5f);
            nodeRect.anchoredPosition = new Vector2(
                position.x + nodeMarkerOffsetX + fineTune.x,
                -position.y + nodeMarkerOffsetY + fineTune.y);
            nodeRect.sizeDelta = new Vector2(150, 46);

            Image marker = CreateImage(nodeRoot.transform, "Marker", new Color(0.18f, 0.19f, 0.2f, 0.82f));
            marker.sprite = circleSprite;
            marker.type = Image.Type.Simple;
            marker.preserveAspect = true;
            marker.rectTransform.anchorMin = marker.rectTransform.anchorMax = new Vector2(0, 0.5f);
            marker.rectTransform.pivot = new Vector2(0.5f, 0.5f);
            marker.rectTransform.anchoredPosition = new Vector2(0, 0);
            marker.rectTransform.sizeDelta = new Vector2(10, 10);

            Image hoverArea = CreateImage(nodeRoot.transform, "Hover Area", new Color(1f, 1f, 1f, 0f));
            hoverArea.rectTransform.anchorMin = hoverArea.rectTransform.anchorMax = new Vector2(0, 0.5f);
            hoverArea.rectTransform.pivot = new Vector2(0.5f, 0.5f);
            hoverArea.rectTransform.anchoredPosition = new Vector2(0, 0);
            hoverArea.rectTransform.sizeDelta = new Vector2(40, 32);

            Image flag = null;
            if (flagFrames != null && flagFrames.Length > 0)
            {
                flag = CreateImage(nodeRoot.transform, "Current Flag", Color.white);
                flag.sprite = flagFrames[0];
                flag.type = Image.Type.Simple;
                flag.preserveAspect = true;
                flag.raycastTarget = false;
                flag.gameObject.SetActive(false);
                flag.rectTransform.anchorMin = flag.rectTransform.anchorMax = new Vector2(0, 0.5f);
                flag.rectTransform.pivot = new Vector2(0f, 0f);
                flag.rectTransform.anchoredPosition = Vector2.zero;
                flag.rectTransform.sizeDelta = new Vector2(22f, 22f);
                CandleFlicker flicker = flag.gameObject.AddComponent<CandleFlicker>();
                flicker.Configure(flag, flagFrames, 5f);
            }

            Image labelBackground = CreateImage(
                nodeRoot.transform,
                "Tooltip Background",
                new Color(0f, 0f, 0f, 0.5f));
            labelBackground.raycastTarget = false;
            labelBackground.rectTransform.anchorMin = labelBackground.rectTransform.anchorMax = new Vector2(0, 0.5f);
            labelBackground.rectTransform.pivot = new Vector2(0f, 0.5f);
            labelBackground.rectTransform.anchoredPosition = new Vector2(10, -2);
            labelBackground.rectTransform.sizeDelta = new Vector2(104, 42);
            labelBackground.gameObject.SetActive(false);

            TMP_Text label = CreateText(nodeRoot.transform, "Label", nodeId, 10,
                TextAlignmentOptions.Left, new Vector2(14, 8), new Vector2(136, 36));
            label.textWrappingMode = TextWrappingModes.Normal;
            label.gameObject.SetActive(false);
            MapNodeHoverTooltip tooltip = hoverArea.gameObject.AddComponent<MapNodeHoverTooltip>();
            tooltip.Configure(label, labelBackground.gameObject);
            tooltip.SetSingleLineLayout();

            return new MapNodeView
            {
                NodeId = nodeId,
                Marker = marker,
                Label = label,
                CurrentFlag = flag,
                Tooltip = tooltip
            };
        }

        private static Vector2 MapNodeFineTune(string nodeId)
        {
            switch (nodeId)
            {
                case "town":
                case "narrow_bridge":
                case "cemetery":
                case "cave":
                    return new Vector2(2f, 2f);
                case "lost_forest":
                case "last_bastion":
                    return new Vector2(1f, -2f);
                default:
                    return Vector2.zero;
            }
        }

        private static void AddCandlePair(
            Transform parent,
            Sprite[] frames,
            Vector2 leftPosition,
            Vector2 rightPosition,
            Sprite[] lightFrames = null)
        {
            if (frames == null || frames.Length == 0)
            {
                return;
            }

            AddCandle(parent, "Left Candle", frames, new Vector2(0, 1), new Vector2(0, 1), leftPosition, lightFrames);
            AddCandle(parent, "Right Candle", frames, new Vector2(1, 1), new Vector2(1, 1), rightPosition, lightFrames);
        }

        private static void AddTorchShadowDim(Transform parent)
        {
            Image dim = CreateImage(parent, "Torch Shadow Dim", new Color(0f, 0f, 0f, 0.24f));
            dim.raycastTarget = false;
            SetStretch(dim.rectTransform, 0, 0, 0, 0);
            dim.transform.SetAsFirstSibling();
        }

        private static void AddCenteredCandle(
            Transform parent,
            string name,
            Sprite[] frames,
            Vector2 position,
            Sprite[] lightFrames = null)
        {
            if (lightFrames != null && lightFrames.Length > 0)
            {
                Image light = CreateImage(parent, $"{name} Light", new Color(1f, 0.62f, 0.08f, 0.16f));
                light.sprite = lightFrames[0];
                light.type = Image.Type.Simple;
                light.preserveAspect = false;
                light.raycastTarget = false;
                light.rectTransform.anchorMin = light.rectTransform.anchorMax = new Vector2(0, 1);
                light.rectTransform.pivot = new Vector2(0.5f, 0.5f);
                light.rectTransform.anchoredPosition = position + new Vector2(0f, 24f);
                light.rectTransform.sizeDelta = new Vector2(305, 237);
                light.gameObject.AddComponent<CandleFlicker>().Configure(light, lightFrames, 4f, false);
            }

            Image candle = CreateImage(parent, name, Color.white);
            candle.sprite = frames[0];
            candle.type = Image.Type.Simple;
            candle.preserveAspect = true;
            candle.raycastTarget = false;
            candle.rectTransform.anchorMin = candle.rectTransform.anchorMax = new Vector2(0, 1);
            candle.rectTransform.pivot = new Vector2(0.5f, 1f);
            candle.rectTransform.anchoredPosition = position;
            candle.rectTransform.sizeDelta = new Vector2(41, 66);
            candle.gameObject.AddComponent<CandleFlicker>().Configure(candle, frames, 7f);
            candle.transform.SetAsLastSibling();
        }

        private static void AddCandle(
            Transform parent,
            string name,
            Sprite[] frames,
            Vector2 anchor,
            Vector2 pivot,
            Vector2 position,
            Sprite[] lightFrames = null)
        {
            if (lightFrames != null && lightFrames.Length > 0)
            {
                Image light = CreateImage(parent, $"{name} Light", new Color(1f, 0.62f, 0.08f, 0.16f));
                light.sprite = lightFrames[0];
                light.type = Image.Type.Simple;
                light.preserveAspect = false;
                light.raycastTarget = false;
                light.rectTransform.anchorMin = light.rectTransform.anchorMax = anchor;
                light.rectTransform.pivot = pivot;
                light.rectTransform.anchoredPosition = position + new Vector2(pivot.x == 0f ? -78f : 78f, 24f);
                light.rectTransform.sizeDelta = new Vector2(305, 237);
                light.gameObject.AddComponent<CandleFlicker>().Configure(light, lightFrames, 4f, false);
            }

            Image candle = CreateImage(parent, name, Color.white);
            candle.sprite = frames[0];
            candle.type = Image.Type.Simple;
            candle.preserveAspect = true;
            candle.raycastTarget = false;
            candle.rectTransform.anchorMin = candle.rectTransform.anchorMax = anchor;
            candle.rectTransform.pivot = pivot;
            candle.rectTransform.anchoredPosition = position;
            candle.rectTransform.sizeDelta = new Vector2(41, 66);
            candle.gameObject.AddComponent<CandleFlicker>().Configure(candle, frames, 7f);
            candle.transform.SetAsLastSibling();
        }

        private static Image CreateImage(Transform parent, string name, Color color)
        {
            GameObject gameObject = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            gameObject.transform.SetParent(parent, false);
            Image image = gameObject.GetComponent<Image>();
            image.color = color;
            return image;
        }

        private static TMP_Text CreateText(
            Transform parent,
            string name,
            string value,
            float size,
            TextAlignmentOptions alignment,
            Vector2 position,
            Vector2 dimensions)
        {
            GameObject gameObject = new GameObject(
                name, typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
            gameObject.transform.SetParent(parent, false);
            TextMeshProUGUI text = gameObject.GetComponent<TextMeshProUGUI>();
            text.font = defaultFont;
            text.text = value;
            text.fontSize = size;
            text.color = TextColor;
            text.alignment = alignment;
            RectTransform rect = text.rectTransform;
            rect.anchorMin = rect.anchorMax = new Vector2(0, 1);
            rect.pivot = new Vector2(0, 1);
            rect.anchoredPosition = new Vector2(position.x, -position.y);
            rect.sizeDelta = dimensions;
            return text;
        }

        private static Button CreateButton(
            Transform parent,
            string name,
            string label,
            Vector2 position,
            Vector2 dimensions,
            Color color)
        {
            Image image = CreateImage(parent, name, color);
            RectTransform rect = image.rectTransform;
            rect.anchorMin = rect.anchorMax = new Vector2(0, 1);
            rect.pivot = new Vector2(0, 1);
            rect.anchoredPosition = new Vector2(position.x, -position.y);
            rect.sizeDelta = dimensions;
            Button button = image.gameObject.AddComponent<Button>();
            ColorBlock colors = button.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = new Color(1f, 1f, 1f, 0.95f);
            colors.pressedColor = new Color(0.9f, 0.9f, 0.9f, 1f);
            colors.selectedColor = Color.white;
            button.colors = colors;
            TMP_Text text = CreateText(image.transform, "Label", label, 16,
                TextAlignmentOptions.Center, Vector2.zero, dimensions);
            text.rectTransform.anchorMin = Vector2.zero;
            text.rectTransform.anchorMax = Vector2.one;
            text.rectTransform.pivot = new Vector2(0.5f, 0.5f);
            text.rectTransform.anchoredPosition = Vector2.zero;
            text.rectTransform.sizeDelta = Vector2.zero;
            return button;
        }

        private static void InsetButtonLabel(Button button, Vector2 min, Vector2 max)
        {
            TMP_Text label = button.GetComponentInChildren<TMP_Text>();
            if (label == null)
            {
                return;
            }

            label.rectTransform.offsetMin = min;
            label.rectTransform.offsetMax = max;
        }

        private static void ApplySimpleCommandFrame(Button button, Sprite commandSprite)
        {
            if (button == null || commandSprite == null)
            {
                return;
            }

            Image image = button.GetComponent<Image>();
            if (image == null)
            {
                return;
            }

            image.sprite = commandSprite;
            image.type = Image.Type.Simple;
            image.preserveAspect = true;
            image.color = Color.white;
            InsetButtonLabel(button, new Vector2(34, 16), new Vector2(-34, -16));
        }

        private static void AddButtonInteriorFill(Button button, string name, Color color)
        {
            if (button == null)
            {
                return;
            }

            Image fill = CreateImage(button.transform, name, color);
            fill.raycastTarget = false;
            fill.rectTransform.anchorMin = Vector2.zero;
            fill.rectTransform.anchorMax = Vector2.one;
            fill.rectTransform.offsetMin = new Vector2(36, 18);
            fill.rectTransform.offsetMax = new Vector2(-36, -18);
            fill.transform.SetAsFirstSibling();

            TMP_Text label = button.GetComponentInChildren<TMP_Text>();
            if (label != null)
            {
                label.color = new Color(0.02f, 0.12f, 0.03f, 1f);
                label.transform.SetAsLastSibling();
            }
        }

        private static void ApplyIconButton(Button button, Sprite sprite)
        {
            if (button == null || sprite == null)
            {
                return;
            }

            Image image = button.GetComponent<Image>();
            if (image != null)
            {
                image.sprite = sprite;
                image.type = Image.Type.Simple;
                image.preserveAspect = true;
                image.color = Color.white;
            }

            TMP_Text label = button.GetComponentInChildren<TMP_Text>();
            if (label != null)
            {
                label.text = string.Empty;
            }
        }

        private static void ApplySimpleWideFrame(Image image, Sprite sprite)
        {
            if (image == null || sprite == null)
            {
                return;
            }

            image.sprite = sprite;
            image.type = Image.Type.Simple;
            image.preserveAspect = true;
            image.color = Color.white;
        }

        private static void ApplyManagementUiSkin(
            Transform root,
            Sprite panelSprite,
            Sprite buttonSprite)
        {
            foreach (Image image in root.GetComponentsInChildren<Image>(true))
            {
                if (image.name == "Close Button" ||
                    image.name == "Management Background" ||
                    image.name == "Squad Panel" ||
                    image.name == "Habilidades Panel" ||
                    image.name == "Sinergias Panel" ||
                    image.name == "Inventario Panel" ||
                    image.name == "Ajustes Panel" ||
                    image.name.StartsWith("Escuadr", StringComparison.Ordinal) ||
                    image.name == "Map Frame" ||
                    image.name == "Title Frame")
                {
                    continue;
                }

                if (image.name.EndsWith("Panel", StringComparison.Ordinal))
                {
                    ApplyUiFrame(image, panelSprite);
                    image.color = new Color(0.9f, 0.9f, 0.9f, 1f);
                }
                else if (image.name.EndsWith("Tab", StringComparison.Ordinal))
                {
                    ApplyUiFrame(image, buttonSprite);
                }
            }
        }

        private static void ApplyUiFrame(Image image, Sprite sprite)
        {
            if (image == null || sprite == null)
            {
                return;
            }

            image.sprite = sprite;
            image.type = Image.Type.Sliced;
            image.preserveAspect = false;
            image.color = Color.white;
        }

        private static void SetStretch(
            RectTransform rect,
            float left,
            float right,
            float bottom,
            float top)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = new Vector2(left, bottom);
            rect.offsetMax = new Vector2(-right, -top);
        }

        private static void CreateLocalizationAssets()
        {
            Locale spanish = LoadOrCreateLocale("es", "Spanish");
            Locale english = LoadOrCreateLocale("en", "English");
            List<Locale> locales = new List<Locale> { spanish, english };
            StringTableCollection collection =
                LocalizationEditorSettings.GetStringTableCollection("UI") ??
                LocalizationEditorSettings.CreateStringTableCollection(
                    "UI", "Assets/Localization", locales);

            Dictionary<string, string> es = new Dictionary<string, string>
            {
                ["app.title"] = "Tácticas de la Barra",
                ["action.start"] = "Iniciar expedición",
                ["action.close"] = "Cerrar",
                ["action.quit"] = "Salir",
                ["route.safety"] = "Seguridad",
                ["route.loot"] = "Botín",
                ["route.challenge"] = "Desafío"
            };
            Dictionary<string, string> en = new Dictionary<string, string>
            {
                ["app.title"] = "Taskbar Tactics",
                ["action.start"] = "Start expedition",
                ["action.close"] = "Close",
                ["action.quit"] = "Quit",
                ["route.safety"] = "Safety",
                ["route.loot"] = "Loot",
                ["route.challenge"] = "Challenge"
            };
            PopulateTable(collection.GetTable("es") as StringTable, es);
            PopulateTable(collection.GetTable("en") as StringTable, en);
        }

        private static Locale LoadOrCreateLocale(string code, string displayName)
        {
            string path = $"Assets/Localization/{code}.asset";
            Locale locale = AssetDatabase.LoadAssetAtPath<Locale>(path);
            if (locale == null)
            {
                locale = Locale.CreateLocale(code);
                locale.name = displayName;
                AssetDatabase.CreateAsset(locale, path);
                LocalizationEditorSettings.AddLocale(locale);
            }

            return locale;
        }

        private static void PopulateTable(StringTable table, Dictionary<string, string> values)
        {
            if (table == null)
            {
                return;
            }

            foreach (KeyValuePair<string, string> pair in values)
            {
                StringTableEntry entry = table.GetEntry(pair.Key) ?? table.AddEntry(pair.Key, pair.Value);
                entry.Value = pair.Value;
            }

            EditorUtility.SetDirty(table);
        }

        private static void ConfigureProject()
        {
            PlayerSettings.companyName = "Independent";
            PlayerSettings.productName = "Taskbar Tactics";
            PlayerSettings.defaultScreenWidth = 960;
            PlayerSettings.defaultScreenHeight = 640;
            PlayerSettings.fullScreenMode = FullScreenMode.Windowed;
            PlayerSettings.runInBackground = true;
            PlayerSettings.resizableWindow = false;
            PlayerSettings.forceSingleInstance = true;
            PlayerSettings.allowFullscreenSwitch = false;
            PlayerSettings.visibleInBackground = true;
            PlayerSettings.colorSpace = ColorSpace.Gamma;
            PlayerSettings.SetApplicationIdentifier(
                UnityEditor.Build.NamedBuildTarget.Standalone,
                "com.independent.taskbartactics");

            Object settingsAsset = AssetDatabase.LoadAllAssetsAtPath(
                "ProjectSettings/ProjectSettings.asset").FirstOrDefault();
            if (settingsAsset != null)
            {
                SerializedObject settings = new SerializedObject(settingsAsset);
                SerializedProperty inputHandler = settings.FindProperty("activeInputHandler");
                if (inputHandler != null)
                {
                    inputHandler.intValue = 1;
                    settings.ApplyModifiedPropertiesWithoutUndo();
                }
            }
        }
    }
}
