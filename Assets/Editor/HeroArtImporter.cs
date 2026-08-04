using System;
using TaskbarTactics.Content;
using UnityEditor;
using UnityEngine;

namespace TaskbarTactics.Editor
{
    public static class HeroArtImporter
    {
        private static readonly string[] HeroIds =
        {
            "guardian",
            "cleric",
            "ranger",
            "rogue",
            "pyromancer",
            "spellblade"
        };

        [MenuItem("Taskbar Tactics/Art/Import and Assign Hero Artwork")]
        public static void ImportAndAssign()
        {
            foreach (string heroId in HeroIds)
            {
                string spritePath = $"Assets/Art/Heroes/Concepts/{heroId}.png";
                string definitionPath = $"Assets/Generated/Content/Heroes/{heroId}.asset";
                ConfigureSprite(spritePath);
                AssignArtwork(definitionPath, spritePath, heroId);
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("Imported and assigned artwork for all six heroes.");
        }

        private static void ConfigureSprite(string path)
        {
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
            TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer == null)
            {
                throw new InvalidOperationException($"Hero artwork was not found at {path}.");
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

        private static void AssignArtwork(
            string definitionPath,
            string spritePath,
            string heroId)
        {
            HeroDefinition definition =
                AssetDatabase.LoadAssetAtPath<HeroDefinition>(definitionPath);
            Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(spritePath);
            if (definition == null || sprite == null)
            {
                throw new InvalidOperationException(
                    $"Could not connect artwork for {heroId}.");
            }

            SerializedObject serializedDefinition = new SerializedObject(definition);
            SerializedProperty artwork = serializedDefinition.FindProperty("artwork");
            artwork.objectReferenceValue = sprite;
            serializedDefinition.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(definition);
        }
    }
}
