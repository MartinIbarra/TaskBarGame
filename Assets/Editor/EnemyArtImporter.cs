using System;
using TaskbarTactics.Content;
using UnityEditor;
using UnityEngine;

namespace TaskbarTactics.Editor
{
    public static class EnemyArtImporter
    {
        private static readonly string[] EnemyIds =
        {
            "goblin",
            "goblin_archer",
            "wolf",
            "shaman",
            "skeleton",
            "cultist",
            "ogre",
            "wraith",
            "barrow_king"
        };

        [MenuItem("Taskbar Tactics/Art/Import and Assign Enemy Artwork")]
        public static void ImportAndAssign()
        {
            int assigned = 0;
            foreach (string enemyId in EnemyIds)
            {
                string spritePath = $"Assets/Art/Enemies/Concepts/{enemyId}.png";
                if (AssetDatabase.LoadAssetAtPath<Texture2D>(spritePath) == null)
                {
                    continue;
                }

                string definitionPath = $"Assets/Generated/Content/Enemies/{enemyId}.asset";
                ConfigureSprite(spritePath);
                AssignArtwork(definitionPath, spritePath, enemyId);
                assigned++;
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log($"Imported and assigned artwork for {assigned} enemies.");
        }

        private static void ConfigureSprite(string path)
        {
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
            TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer == null)
            {
                throw new InvalidOperationException($"Enemy artwork was not found at {path}.");
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
            string enemyId)
        {
            EnemyDefinition definition =
                AssetDatabase.LoadAssetAtPath<EnemyDefinition>(definitionPath);
            Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(spritePath);
            if (definition == null || sprite == null)
            {
                throw new InvalidOperationException(
                    $"Could not connect artwork for {enemyId}.");
            }

            SerializedObject serializedDefinition = new SerializedObject(definition);
            SerializedProperty artwork = serializedDefinition.FindProperty("artwork");
            artwork.objectReferenceValue = sprite;
            serializedDefinition.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(definition);
        }
    }
}
