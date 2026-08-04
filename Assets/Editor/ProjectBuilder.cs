using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using TMPro;
using TaskbarTactics.Content;
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
        private static readonly Color Background = new Color(0.055f, 0.071f, 0.11f, 1f);
        private static readonly Color Panel = new Color(0.09f, 0.12f, 0.18f, 0.98f);
        private static readonly Color PanelLight = new Color(0.14f, 0.18f, 0.25f, 1f);
        private static readonly Color Accent = new Color(0.22f, 0.66f, 0.78f, 1f);
        private static readonly Color TextColor = new Color(0.9f, 0.93f, 0.96f, 1f);
        private static TMP_FontAsset defaultFont;

        [MenuItem("Taskbar Tactics/Build Editable Vertical Slice")]
        public static void BuildAll()
        {
            EnsureFolders();
            EnsureTextMeshProResources();
            CreateLocalizationAssets();
            GameContentCatalog catalog = CreateContentAssets();
            Sprite unitSprite = CreatePlaceholderSprite();
            RuntimeAnimatorController animator = CreateAnimationController();
            UnitView unitPrefab = CreateUnitPrefab(unitSprite, animator);
            CreateMainScene(catalog, unitPrefab, unitSprite);
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
                GeneratedRoot,
                GeneratedRoot + "/Content",
                GeneratedRoot + "/Content/Heroes",
                GeneratedRoot + "/Content/Skills",
                GeneratedRoot + "/Content/Items",
                GeneratedRoot + "/Content/Affixes",
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
            defaultFont = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(TmpFontPath);
            if (defaultFont == null)
            {
                throw new InvalidOperationException(
                    "Faltan los recursos esenciales de TextMeshPro. Ejecute primero " +
                    "Taskbar Tactics/Import TextMeshPro Essentials.");
            }
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
                EditorUtility.SetDirty(asset);
                return asset;
            }).ToList();

            List<ItemDefinition> items = blueprint.Items.Select(data =>
            {
                ItemDefinition asset = LoadOrCreate<ItemDefinition>(
                    $"{GeneratedRoot}/Content/Items/{data.Id}.asset");
                asset.Configure(data);
                EditorUtility.SetDirty(asset);
                return asset;
            }).ToList();

            List<AffixDefinition> affixes = blueprint.Affixes.Select(data =>
            {
                AffixDefinition asset = LoadOrCreate<AffixDefinition>(
                    $"{GeneratedRoot}/Content/Affixes/{data.Id}.asset");
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
            List<EnemyDefinition> normalEnemies = enemies.Where(enemy => !enemy.IsBoss).ToList();
            EnemyDefinition boss = enemies.First(enemy => enemy.IsBoss);
            foreach (MapNodeBlueprint node in blueprint.MapNodes.Where(node =>
                         node.Type == Core.Models.MapNodeType.Combat ||
                         node.Type == Core.Models.MapNodeType.Elite ||
                         node.Type == Core.Models.MapNodeType.Boss))
            {
                EncounterDefinition encounter = LoadOrCreate<EncounterDefinition>(
                    $"{GeneratedRoot}/Content/Encounters/{node.Id}.asset");
                IEnumerable<EnemyDefinition> units = node.Type == Core.Models.MapNodeType.Boss
                    ? new[] { boss }
                    : Enumerable.Range(0, node.Type == Core.Models.MapNodeType.Elite ? 3 : 2)
                        .Select(index => normalEnemies[(node.Difficulty + index) % normalEnemies.Count]);
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
                affixes,
                synergies,
                enemies,
                encounters,
                map,
                new[] { defaultCosmetic });
            EditorUtility.SetDirty(catalog);
            return catalog;
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
                new Keyframe(0f, 0f), new Keyframe(0.08f, -0.08f),
                new Keyframe(0.18f, 0.32f), new Keyframe(0.38f, 0f));
            SetCurve(attack, "m_LocalPosition.y",
                new Keyframe(0f, 0f), new Keyframe(0.18f, 0.035f),
                new Keyframe(0.38f, 0f));
            SetCurve(attack, "m_LocalScale.x",
                new Keyframe(0f, 1f), new Keyframe(0.08f, 0.94f),
                new Keyframe(0.18f, 1.08f), new Keyframe(0.38f, 1f));

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
                hud.transform, "Health Background", sprite, new Color(0.16f, 0.04f, 0.06f), 20);
            healthBackground.transform.localPosition = new Vector3(0, 1.45f, 0);
            healthBackground.transform.localScale = new Vector3(0.9f, 0.08f, 1);

            GameObject healthFill = CreateSpriteChild(
                healthBackground.transform, "Health Fill", sprite, new Color(0.18f, 0.85f, 0.4f), 21);
            healthFill.transform.localPosition = Vector3.zero;
            healthFill.transform.localScale = Vector3.one;

            GameObject labelObject = new GameObject("Name Label");
            labelObject.transform.SetParent(hud.transform, false);
            labelObject.transform.localPosition = new Vector3(0, -0.18f, 0);
            TextMeshPro label = labelObject.AddComponent<TextMeshPro>();
            label.font = defaultFont;
            label.fontSize = 1.5f;
            label.alignment = TextAlignmentOptions.Center;
            label.color = TextColor;
            label.sortingOrder = 22;
            label.rectTransform.sizeDelta = new Vector2(3f, 0.5f);
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

        private static void CreateMainScene(
            GameContentCatalog catalog,
            UnitView unitPrefab,
            Sprite cellSprite)
        {
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            GameObject systems = new GameObject("Systems");
            GameObject gameplay = new GameObject("Gameplay");
            GameObject stripUi = CreateCanvasRoot("Strip UI", new Vector2(960, 192));
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
            ManagementUiController management = BuildManagementUi(managementUi.transform);

            GameObject appObject = new GameObject("Game Application");
            appObject.transform.SetParent(systems.transform);
            GameAppController app = appObject.AddComponent<GameAppController>();
            app.Configure(catalog, combatPresenter, strip, management, window);
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
            camera.orthographicSize = 3.6f;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(1, 0, 1, 1);
            camera.transform.position = new Vector3(0, 0, -10);
            camera.allowHDR = false;
            camera.allowMSAA = false;
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
            Transform heroGrid = new GameObject("Hero Grid").transform;
            heroGrid.SetParent(combatObject.transform);
            Transform enemyGrid = new GameObject("Enemy Grid").transform;
            enemyGrid.SetParent(combatObject.transform);
            List<Transform> heroCells = CreateGrid(heroGrid, -4.4f, cellSprite, new Color(0.08f, 0.18f, 0.25f));
            List<Transform> enemyCells = CreateGrid(enemyGrid, 1.1f, cellSprite, new Color(0.25f, 0.08f, 0.1f));
            presenter.Configure(unitPrefab, heroCells, enemyCells);
            return presenter;
        }

        private static List<Transform> CreateGrid(
            Transform parent,
            float startX,
            Sprite sprite,
            Color color)
        {
            List<Transform> cells = new List<Transform>();
            for (int row = 0; row < 3; row++)
            {
                for (int column = 0; column < 3; column++)
                {
                    GameObject cell = new GameObject($"Cell {row},{column}");
                    cell.transform.SetParent(parent);
                    cell.transform.position = new Vector3(
                        startX + column * 1.45f,
                        1.5f - row * 1.45f,
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
            Image bar = CreateImage(root, "Status Bar", Panel);
            SetStretch(bar.rectTransform, 0, 0, 0, 150);
            TMP_Text status = CreateText(bar.transform, "Status Label", "Escuadrón en el campamento",
                18, TextAlignmentOptions.Left, new Vector2(12, 7), new Vector2(500, 32));
            TMP_Text node = CreateText(bar.transform, "Node Label", "Campamento",
                18, TextAlignmentOptions.Center, new Vector2(510, 7), new Vector2(200, 32));
            Button manage = CreateButton(bar.transform, "Manage Button", "Gestionar",
                new Vector2(720, 3), new Vector2(125, 34), Accent);
            Button menu = CreateButton(bar.transform, "Menu Button", "•••",
                new Vector2(852, 3), new Vector2(48, 34), PanelLight);
            Image attention = CreateImage(root, "Attention Indicator", new Color(1f, 0.72f, 0.18f));
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
            controller.Configure(status, node, attention.gameObject, manage, menu, menuPanel.gameObject, quit);
            return controller;
        }

        private static ManagementUiController BuildManagementUi(Transform root)
        {
            Image background = CreateImage(root, "Management Background", Background);
            SetStretch(background.rectTransform, 0, 0, 0, 0);
            TMP_Text title = CreateText(background.transform, "Title", "TASKBAR TACTICS",
                26, TextAlignmentOptions.Left, new Vector2(24, 18), new Vector2(500, 42));
            title.fontStyle = FontStyles.Bold;
            Button close = CreateButton(background.transform, "Close Button", "Volver a la barra",
                new Vector2(770, 16), new Vector2(165, 38), Accent);

            string[] tabNames =
            {
                "Escuadrón", "Formación", "Habilidades", "Sinergias",
                "Inventario", "Mapa", "Ajustes"
            };
            List<Button> tabs = new List<Button>();
            for (int i = 0; i < tabNames.Length; i++)
            {
                tabs.Add(CreateButton(background.transform, $"{tabNames[i]} Tab", tabNames[i],
                    new Vector2(20, 80 + i * 58), new Vector2(160, 44), PanelLight));
            }

            List<GameObject> panels = new List<GameObject>();
            List<TMP_Text> summaries = new List<TMP_Text>();
            for (int i = 0; i < tabNames.Length; i++)
            {
                Image panel = CreateImage(background.transform, $"{tabNames[i]} Panel", Panel);
                panel.rectTransform.anchorMin = new Vector2(0, 0);
                panel.rectTransform.anchorMax = new Vector2(1, 1);
                panel.rectTransform.offsetMin = new Vector2(200, 24);
                panel.rectTransform.offsetMax = new Vector2(-24, -76);
                CreateText(panel.transform, "Panel Title", tabNames[i].ToUpperInvariant(),
                    24, TextAlignmentOptions.Left, new Vector2(24, 18), new Vector2(500, 40));
                TMP_Text summary = CreateText(panel.transform, "Summary", string.Empty,
                    18, TextAlignmentOptions.TopLeft, new Vector2(24, 72), new Vector2(690, 360));
                summary.textWrappingMode = TextWrappingModes.Normal;
                panels.Add(panel.gameObject);
                summaries.Add(summary);
                panel.gameObject.SetActive(i == 0);
            }

            List<Button> heroButtons = new List<Button>();
            for (int i = 0; i < 6; i++)
            {
                heroButtons.Add(CreateButton(panels[0].transform, $"Hero {i + 1} Button", $"Héroe {i + 1}",
                    new Vector2(24 + (i % 2) * 330, 300 + (i / 2) * 58),
                    new Vector2(300, 44), PanelLight));
            }

            List<Button> formationButtons = new List<Button>();
            for (int i = 0; i < 9; i++)
            {
                formationButtons.Add(CreateButton(panels[1].transform, $"Formation Cell {i / 3},{i % 3}",
                    $"{i / 3},{i % 3}",
                    new Vector2(310 + (i % 3) * 92, 130 + (i / 3) * 92),
                    new Vector2(76, 76), PanelLight));
            }

            Button cycleActive = CreateButton(panels[2].transform, "Cycle Active Skill Button",
                "Cambiar activa", new Vector2(24, 300), new Vector2(220, 44), Accent);
            Button cyclePassive = CreateButton(panels[2].transform, "Cycle Passive Skill Button",
                "Cambiar pasiva", new Vector2(260, 300), new Vector2(220, 44), Accent);

            List<Button> equipButtons = new List<Button>();
            string[] slots = { "Arma", "Armadura", "Accesorio", "Reliquia" };
            for (int i = 0; i < slots.Length; i++)
            {
                equipButtons.Add(CreateButton(panels[4].transform, $"Equip {slots[i]} Button",
                    $"Equipar {slots[i]}", new Vector2(24 + (i % 2) * 250, 390 + (i / 2) * 58),
                    new Vector2(220, 44), Accent));
            }

            List<Button> routeButtons = new List<Button>
            {
                CreateButton(panels[5].transform, "Safety Route Button", "Seguridad",
                    new Vector2(24, 250), new Vector2(220, 44), PanelLight),
                CreateButton(panels[5].transform, "Loot Route Button", "Botín",
                    new Vector2(24, 304), new Vector2(220, 44), PanelLight),
                CreateButton(panels[5].transform, "Challenge Route Button", "Desafío",
                    new Vector2(24, 358), new Vector2(220, 44), PanelLight)
            };
            Button start = CreateButton(panels[5].transform, "Start Expedition Button",
                "INICIAR EXPEDICIÓN", new Vector2(24, 420), new Vector2(220, 52), Accent);

            MapUiController mapVisual = BuildMapVisual(panels[5].transform, summaries[5]);

            Button language = CreateButton(panels[6].transform, "Language Button",
                "Cambiar ES / EN", new Vector2(24, 300), new Vector2(220, 44), Accent);
            Button quit = CreateButton(panels[6].transform, "Quit Button",
                "Salir del juego", new Vector2(260, 300), new Vector2(220, 44),
                new Color(0.62f, 0.18f, 0.22f));

            ManagementUiController controller =
                root.gameObject.AddComponent<ManagementUiController>();
            controller.Configure(
                tabs,
                panels,
                close,
                heroButtons,
                formationButtons,
                summaries[0],
                summaries[2],
                summaries[3],
                summaries[4],
                summaries[5],
                summaries[6],
                mapVisual,
                cycleActive,
                cyclePassive,
                equipButtons,
                routeButtons,
                start,
                language,
                quit);
            return controller;
        }

        private static MapUiController BuildMapVisual(Transform parent, TMP_Text summary)
        {
            summary.rectTransform.sizeDelta = new Vector2(230, 150);

            GameObject root = new GameObject("Map Visual", typeof(RectTransform));
            root.transform.SetParent(parent, false);
            RectTransform rootRect = root.GetComponent<RectTransform>();
            rootRect.anchorMin = rootRect.anchorMax = new Vector2(0, 1);
            rootRect.pivot = new Vector2(0, 1);
            rootRect.anchoredPosition = new Vector2(280, -58);
            rootRect.sizeDelta = new Vector2(420, 502);
            Image viewport = root.AddComponent<Image>();
            viewport.color = new Color(0f, 0f, 0f, 0.01f);
            root.AddComponent<RectMask2D>();

            Texture2D mapTexture = AssetDatabase.LoadAssetAtPath<Texture2D>(
                "Assets/Resources/Maps/map1.png");
            RectTransform artworkRoot = CreateMapArtworkRoot(root.transform);
            RawImage background = CreateMapBackground(artworkRoot, mapTexture);

            RectTransform artworkLayer = CreateMapLayer(artworkRoot, "Artist Overlays");
            RectTransform routesLayer = CreateMapLayer(artworkRoot, "Routes");
            RectTransform nodesLayer = CreateMapLayer(artworkRoot, "Nodes");
            RawImage nodeArtworkOverlay = CreateMapArtworkOverlay(
                artworkLayer, "Node Artwork Overlay", "Maps/map1_nodes");
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
                CreateMapNode(nodesLayer, pair.Key, pair.Value)).ToList();
            List<MapRoutePreferenceLegend> legends = CreateMapRouteLegend(root.transform);

            MapUiController controller = root.AddComponent<MapUiController>();
            controller.Configure(
                background,
                nodes,
                routes,
                legends,
                nodeArtworkOverlay,
                routeArtworkOverlays);
            root.AddComponent<DraggableMapView>().Configure(artworkRoot);
            return controller;
        }

        private static RectTransform CreateMapArtworkRoot(Transform parent)
        {
            GameObject root = new GameObject("Map Artwork", typeof(RectTransform));
            root.transform.SetParent(parent, false);
            RectTransform rect = root.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = new Vector2(0, 1);
            rect.pivot = new Vector2(0, 1);
            rect.anchoredPosition = new Vector2(-24, 150);
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
                },
                new MapRouteArtworkOverlay
                {
                    PreferenceName = "Challenge",
                    Image = CreateMapArtworkOverlay(parent, "Challenge Route Artwork", "Maps/map1_route_challenge")
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
            rootRect.anchoredPosition = new Vector2(14, -432);
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

        private static MapNodeView CreateMapNode(Transform parent, string nodeId, Vector2 position)
        {
            GameObject nodeRoot = new GameObject(nodeId, typeof(RectTransform));
            nodeRoot.transform.SetParent(parent, false);
            RectTransform nodeRect = nodeRoot.GetComponent<RectTransform>();
            nodeRect.anchorMin = nodeRect.anchorMax = new Vector2(0, 1);
            nodeRect.pivot = new Vector2(0.5f, 0.5f);
            nodeRect.anchoredPosition = new Vector2(position.x, -position.y);
            nodeRect.sizeDelta = new Vector2(150, 46);

            Image marker = CreateImage(nodeRoot.transform, "Marker", new Color(0.18f, 0.19f, 0.2f, 0.82f));
            marker.rectTransform.anchorMin = marker.rectTransform.anchorMax = new Vector2(0, 0.5f);
            marker.rectTransform.pivot = new Vector2(0.5f, 0.5f);
            marker.rectTransform.anchoredPosition = new Vector2(0, 0);
            marker.rectTransform.sizeDelta = new Vector2(18, 18);

            TMP_Text label = CreateText(nodeRoot.transform, "Label", nodeId, 10,
                TextAlignmentOptions.Left, new Vector2(14, 30), new Vector2(136, 40));
            label.textWrappingMode = TextWrappingModes.Normal;

            return new MapNodeView
            {
                NodeId = nodeId,
                Marker = marker,
                Label = label
            };
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
            colors.highlightedColor = Color.Lerp(color, Color.white, 0.15f);
            colors.pressedColor = Color.Lerp(color, Color.black, 0.2f);
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
