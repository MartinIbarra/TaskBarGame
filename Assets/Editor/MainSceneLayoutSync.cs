using TaskbarTactics.Presentation;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace TaskbarTactics.Editor
{
    public static class MainSceneLayoutSync
    {
        private const string MainScenePath = "Assets/Scenes/Main.unity";

        [MenuItem("Taskbar Tactics/Sync Main Scene With Runtime HUD")]
        public static void Sync()
        {
            Scene scene = EditorSceneManager.OpenScene(MainScenePath, OpenSceneMode.Single);
            ManagementUiController controller = Object.FindFirstObjectByType<ManagementUiController>();
            if (controller == null)
            {
                Debug.LogError("Could not find ManagementUiController in Main.unity.");
                return;
            }

            controller.SyncInitialHudLayoutForEditor();
            EditorUtility.SetDirty(controller.gameObject);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();
            Debug.Log("Main.unity synchronized with the initial runtime HUD layout.");
        }
    }
}
