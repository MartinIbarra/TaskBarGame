using NUnit.Framework;
using TaskbarTactics.Presentation;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace TaskbarTactics.Tests
{
    public sealed class PartyContextPanelTests
    {
        private GameObject root;
        private Image frame;
        private Image icon;
        private TMP_Text title;
        private TMP_Text name;
        private TMP_Text role;
        private TMP_Text description;
        private TMP_Text footerLabel;
        private TMP_Text footerValue;
        private PartyContextPanelView view;
        private Texture2D texture;
        private Sprite sprite;
        private Sprite secondSprite;

        [SetUp]
        public void SetUp()
        {
            root = new GameObject("Party Context Panel Test");
            view = root.AddComponent<PartyContextPanelView>();
            frame = CreateImage("Frame");
            icon = CreateImage("Icon");
            title = CreateText("Title");
            name = CreateText("Name");
            role = CreateText("Role");
            description = CreateText("Description");
            footerLabel = CreateText("Footer Label");
            footerValue = CreateText("Footer Value");
            view.Configure(frame, icon, title, name, role, description, footerLabel, footerValue);

            texture = new Texture2D(2, 2);
            sprite = Sprite.Create(texture, new Rect(0f, 0f, 2f, 2f), new Vector2(0.5f, 0.5f));
            secondSprite = Sprite.Create(texture, new Rect(0f, 0f, 1f, 1f), new Vector2(0.5f, 0.5f));
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(root);
            Object.DestroyImmediate(sprite);
            Object.DestroyImmediate(secondSprite);
            Object.DestroyImmediate(texture);
        }

        [Test]
        public void HeroAndFormationSelectionReplaceTheSharedPanelContent()
        {
            view.ShowHero(sprite, sprite, new[] { sprite, secondSprite }, "Heroes", "Warrior", "Frontline Tank",
                "Protects the party.", "Strong in:", "Defense, HP", Color.blue);

            Assert.That(title.text, Is.EqualTo("Heroes"));
            Assert.That(name.text, Is.EqualTo("Warrior"));
            Assert.That(name.alignment, Is.EqualTo(TextAlignmentOptions.Left));
            Assert.That(description.fontSize, Is.EqualTo(22f));
            Assert.That(description.rectTransform.anchoredPosition.y, Is.EqualTo(-6f));
            Assert.That(role.text, Is.EqualTo("Frontline Tank"));
            Assert.That(description.text, Is.EqualTo("Protects the party."));
            Assert.That(footerLabel.text, Is.EqualTo("Strong in:"));
            Assert.That(footerValue.text, Is.EqualTo("Defense, HP"));
            Assert.That(frame.gameObject.activeSelf, Is.False);
            Assert.That(icon.sprite, Is.EqualTo(sprite));

            view.ShowFormation(sprite, sprite, "Formation", "3+1", "Frontline with support",
                "Three heroes take the front rank.", "Best for:", "Rear-line support");

            Assert.That(title.text, Is.EqualTo("Formation"));
            Assert.That(name.text, Is.EqualTo("3+1"));
            Assert.That(name.alignment, Is.EqualTo(TextAlignmentOptions.Center));
            Assert.That(role.text, Is.EqualTo("Frontline with support"));
            Assert.That(description.text, Is.EqualTo("Three heroes take the front rank."));
            Assert.That(footerLabel.text, Is.EqualTo("Best for:"));
            Assert.That(footerValue.text, Is.EqualTo("Rear-line support"));
        }

        private Image CreateImage(string objectName)
        {
            GameObject target = new GameObject(objectName, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            target.transform.SetParent(root.transform, false);
            return target.GetComponent<Image>();
        }

        private TMP_Text CreateText(string objectName)
        {
            GameObject target = new GameObject(objectName, typeof(RectTransform), typeof(CanvasRenderer),
                typeof(TextMeshProUGUI));
            target.transform.SetParent(root.transform, false);
            return target.GetComponent<TMP_Text>();
        }
    }
}
