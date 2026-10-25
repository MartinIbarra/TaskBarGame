using System;
using System.IO;
using System.Linq;
using TaskbarTactics.Presentation;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace TaskbarTactics.Editor
{
    public static class ManagementLayoutRepair
    {
        private const string MainScenePath = "Assets/Scenes/Main.unity";
        private const string ResultPath = "Temp/ManagementLayoutRepair.result";

        [MenuItem("Taskbar Tactics/UI/Repair Management Window Layout")]
        public static void Apply()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                throw new InvalidOperationException("Exit Play Mode before repairing the authored layout.");

            Scene scene = SceneManager.GetActiveScene();
            if (scene.path != MainScenePath)
            {
                if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
                scene = EditorSceneManager.OpenScene(MainScenePath);
            }

            GameObject root = scene.GetRootGameObjects().Single(item => item.name == "Management UI");
            CanvasScaler scaler = root.GetComponent<CanvasScaler>();
            Undo.RecordObject(scaler, "Fit management reference resolution");
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(960f, 640f);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.Expand;

            RectTransform background = root.transform.Find("Management Background") as RectTransform;
            if (background == null) throw new InvalidOperationException("Management Background is missing.");
            Undo.RecordObject(background, "Fix management design bounds");
            background.anchorMin = background.anchorMax = new Vector2(0.5f, 0.5f);
            background.pivot = new Vector2(0.5f, 0.5f);
            background.anchoredPosition = Vector2.zero;
            background.sizeDelta = new Vector2(960f, 640f);

            ManagementUiController controller = root.GetComponent<ManagementUiController>();
            Undo.RegisterFullObjectHierarchyUndo(root, "Repair inventory columns and stats");
            controller.SyncInventoryContentLayout();
            HeroStatsPanelView stats = root.GetComponentInChildren<HeroStatsPanelView>(true);
            if (stats == null) throw new InvalidOperationException("Hero Stats Panel was not authored.");
            EditorUtility.SetDirty(stats);
            EditorUtility.SetDirty(controller);
            Canvas.ForceUpdateCanvases();
            Validate(root);
            EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(scene))
                throw new InvalidOperationException("Could not save the repaired Main scene.");
            File.WriteAllText(ResultPath, "LAYOUT_OK: 960x640 design bounds; stats authored and contained.");
            Debug.Log("Management layout repaired and saved. Statistics are authored inside the inventory bounds.");
        }

        private static void Validate(GameObject root)
        {
            RectTransform background = root.transform.Find("Management Background") as RectTransform;
            HeroStatsPanelView stats = root.GetComponentInChildren<HeroStatsPanelView>(true);
            RectTransform statsRect = stats.transform as RectTransform;
            RectTransform inventory = statsRect.parent as RectTransform;
            Vector3[] corners = new Vector3[4];
            statsRect.GetWorldCorners(corners);
            foreach (Vector3 corner in corners)
            {
                Vector3 point = background.InverseTransformPoint(corner);
                if (!Contains(background.rect, point))
                    throw new InvalidOperationException("Statistics extend outside the management window.");
                point = inventory.InverseTransformPoint(corner);
                if (!Contains(inventory.rect, point))
                    throw new InvalidOperationException("Statistics extend outside their inventory container.");
            }

            SerializedObject serialized = new SerializedObject(stats);
            foreach (string field in new[] { "hpLabel", "mpLabel", "moreSection", "moreButton" })
                if (serialized.FindProperty(field).objectReferenceValue == null)
                    throw new InvalidOperationException($"Statistics reference is missing: {field}.");
        }

        private static bool Contains(Rect rect, Vector3 point)
        {
            const float tolerance = 0.1f;
            return point.x >= rect.xMin - tolerance && point.x <= rect.xMax + tolerance &&
                   point.y >= rect.yMin - tolerance && point.y <= rect.yMax + tolerance;
        }

        [MenuItem("Taskbar Tactics/UI/Build Repaired Windows Player")]
        public static void BuildWindows()
        {
            Apply();
            const string outputPath = "Builds/WindowsLayoutFix/TaskbarTactics.exe";
            Directory.CreateDirectory(Path.GetDirectoryName(outputPath));
            BuildReport report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
            {
                scenes = new[] { MainScenePath },
                locationPathName = outputPath,
                target = BuildTarget.StandaloneWindows64,
                options = BuildOptions.CompressWithLz4HC
            });
            string result = $"BUILD_{report.summary.result}: errors={report.summary.totalErrors}; " +
                $"warnings={report.summary.totalWarnings}; path={outputPath}";
            File.WriteAllText(ResultPath, result);
            if (report.summary.result != BuildResult.Succeeded)
                throw new InvalidOperationException(result);
            Debug.Log(result);
        }
    }
}
