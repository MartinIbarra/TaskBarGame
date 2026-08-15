using System.Linq;
using NUnit.Framework;
using TaskbarTactics.Content;
using UnityEditor;
using UnityEngine;

namespace TaskbarTactics.Tests
{
    public sealed class HeroArtworkTests
    {
        private static readonly string[] HeroIds =
        {
            "warrior",
            "cleric",
            "mage",
            "archer",
            "rogue",
            "magic_warrior"
        };

        private static readonly string[] HeroArtIds =
        {
            "guardian",
            "cleric",
            "pyromancer",
            "ranger",
            "rogue",
            "spellblade"
        };

        [Test]
        public void EveryHeroDefinitionHasAssignedArtwork()
        {
            HeroDefinition[] heroes = AssetDatabase
                .FindAssets("t:HeroDefinition", new[] { "Assets/Generated/Content/Heroes" })
                .Select(AssetDatabase.GUIDToAssetPath)
                .Select(AssetDatabase.LoadAssetAtPath<HeroDefinition>)
                .Where(hero => hero != null)
                .ToArray();

            Assert.That(heroes, Has.Length.EqualTo(HeroIds.Length));
            foreach (HeroDefinition hero in heroes)
            {
                SerializedProperty artwork = new SerializedObject(hero).FindProperty("artwork");
                Assert.That(artwork, Is.Not.Null, $"{hero.Id} does not expose an artwork field.");
                Assert.That(
                    artwork.objectReferenceValue,
                    Is.Not.Null,
                    $"{hero.Id} does not have artwork assigned.");
            }
        }

        [TestCaseSource(nameof(HeroArtIds))]
        public void HeroArtworkIsImportedAsCrispTransparentSprite(string heroId)
        {
            string path = $"Assets/Art/Heroes/Concepts/{heroId}.png";
            TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;

            Assert.That(importer, Is.Not.Null, $"{path} was not imported.");
            Assert.That(importer.textureType, Is.EqualTo(TextureImporterType.Sprite));
            Assert.That(importer.spriteImportMode, Is.EqualTo(SpriteImportMode.Single));
            Assert.That(importer.filterMode, Is.EqualTo(FilterMode.Point));
            Assert.That(importer.mipmapEnabled, Is.False);
            Assert.That(importer.alphaIsTransparency, Is.True);
            Assert.That(importer.textureCompression, Is.EqualTo(TextureImporterCompression.Uncompressed));
            Assert.That(importer.spritePixelsPerUnit, Is.EqualTo(256f));
        }
    }
}
