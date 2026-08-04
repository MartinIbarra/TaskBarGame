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
    }
}
