using NUnit.Framework;
using TaskbarTactics.Core.Localization;

namespace TaskbarTactics.Tests
{
    public sealed class LocalizationTests
    {
        [Test]
        public void SpanishAndEnglishContainEveryRequiredKey()
        {
            LocalizationCatalog catalog = LocalizationCatalog.CreateBuiltIn();

            Assert.That(catalog.MissingKeys("es"), Is.Empty);
            Assert.That(catalog.MissingKeys("en"), Is.Empty);
        }

        [Test]
        public void PartyContextPanelEnglishTitlesUseRequestedWording()
        {
            LocalizationCatalog catalog = LocalizationCatalog.CreateBuiltIn();

            Assert.That(catalog.Get("en", "party.info.title.hero"), Is.EqualTo("Heroes"));
            Assert.That(catalog.Get("en", "party.info.title.formation"), Is.EqualTo("Formation"));
            Assert.That(catalog.Get("en", "party.info.hero.magic_warrior.description"),
                Does.Contain("Lost a bet and now she has to use both: magic and sword."));
            Assert.That(catalog.Get("en", "party.info.hero.magic_warrior.description"),
                Does.Contain("She is NOT allowed into Taverns."));
        }

        [TestCase("warrior")]
        [TestCase("cleric")]
        [TestCase("mage")]
        [TestCase("archer")]
        [TestCase("rogue")]
        [TestCase("magic_warrior")]
        public void PartyHeroInfoIsLocalizedInBothLanguages(string heroId)
        {
            LocalizationCatalog catalog = LocalizationCatalog.CreateBuiltIn();
            string roleKey = $"party.info.hero.{heroId}.role";
            string descriptionKey = $"party.info.hero.{heroId}.description";

            Assert.That(catalog.Get("en", roleKey), Is.Not.EqualTo(catalog.Get("es", roleKey)));
            Assert.That(catalog.Get("en", descriptionKey), Is.Not.EqualTo(catalog.Get("es", descriptionKey)));
        }

        [TestCase(0)]
        [TestCase(1)]
        [TestCase(2)]
        public void PartyFormationInfoIsLocalizedInBothLanguages(int presetIndex)
        {
            LocalizationCatalog catalog = LocalizationCatalog.CreateBuiltIn();
            string prefix = $"party.info.formation.{presetIndex}.";

            Assert.That(catalog.Get("en", prefix + "name"), Is.Not.EqualTo(catalog.Get("es", prefix + "name")));
            Assert.That(catalog.Get("en", prefix + "description"),
                Is.Not.EqualTo(catalog.Get("es", prefix + "description")));
        }
    }
}
