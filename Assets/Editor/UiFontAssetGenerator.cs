using System.IO;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.TextCore.LowLevel;

namespace TaskbarTactics.Editor
{
    internal static class UiFontAssetGenerator
    {
        private const string SourceFolder = "Assets/Resources/UI/Fonts/Source";
        private const string OutputFolder = "Assets/Resources/UI/Fonts/TMP";

        [MenuItem("Taskbar Tactics/Generate UI Font Assets")]
        public static void GenerateFontAssets()
        {
            Directory.CreateDirectory(Path.Combine(Application.dataPath, "Resources/UI/Fonts/TMP"));
            AssetDatabase.Refresh();

            CreateFontAsset(
                SourceFolder + "/Cinzel-SemiBold.ttf",
                OutputFolder + "/Cinzel SemiBold.asset");
            CreateFontAsset(
                SourceFolder + "/Alegreya-Medium.ttf",
                OutputFolder + "/Alegreya Medium.asset");

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("Generated Taskbar Tactics UI font assets.");
        }

        private static void CreateFontAsset(string sourcePath, string outputPath)
        {
            Font sourceFont = AssetDatabase.LoadAssetAtPath<Font>(sourcePath);
            if (sourceFont == null)
            {
                Debug.LogError($"Unable to load UI font source: {sourcePath}");
                return;
            }

            if (AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(outputPath) != null)
            {
                Debug.Log($"UI font asset already exists, leaving it unchanged: {outputPath}");
                return;
            }

            TMP_FontAsset fontAsset = TMP_FontAsset.CreateFontAsset(
                sourceFont,
                90,
                9,
                GlyphRenderMode.SDFAA,
                1024,
                1024,
                AtlasPopulationMode.Dynamic,
                true);
            if (fontAsset == null)
            {
                Debug.LogError($"Unable to create TMP font asset from: {sourcePath}");
                return;
            }

            fontAsset.name = Path.GetFileNameWithoutExtension(outputPath);
            AssetDatabase.CreateAsset(fontAsset, outputPath);
            if (fontAsset.atlasTextures != null && fontAsset.atlasTextures.Length > 0 &&
                fontAsset.atlasTextures[0] != null)
            {
                AssetDatabase.AddObjectToAsset(fontAsset.atlasTextures[0], fontAsset);
            }

            if (fontAsset.material != null)
            {
                AssetDatabase.AddObjectToAsset(fontAsset.material, fontAsset);
            }

            EditorUtility.SetDirty(fontAsset);
        }
    }
}
