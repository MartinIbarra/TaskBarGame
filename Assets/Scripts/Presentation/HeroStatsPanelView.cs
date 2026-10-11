using System.Globalization;
using TMPro;
using TaskbarTactics.Core.Models;
using TaskbarTactics.Core.Stats;
using UnityEngine;
using UnityEngine.UI;

namespace TaskbarTactics.Presentation
{
    public sealed class HeroStatsPanelView : MonoBehaviour
    {
        private const float PanelWidth = 220f;
        private const float PanelHeight = 300f;
        private const float ColumnWidth = 110f;
        private const float ColumnOffset = 77f;
        private const float MoreButtonWidth = 132f;

        private TMP_FontAsset font;
        private TMP_Text hpLabel;
        private TMP_Text mpLabel;
        private TMP_Text[] mainLabels;
        private TMP_Text[] secondaryLabels;
        private TMP_Text[] moreLabels;
        private GameObject moreSection;
        private Button moreButton;
        private bool configured;
        private bool moreExpanded;

        public void Configure(TMP_FontAsset panelFont)
        {
            font = panelFont != null
                ? panelFont
                : Resources.Load<TMP_FontAsset>("UI/Fonts/TMP/Alegreya Medium") ??
                    Resources.Load<TMP_FontAsset>("UI/Fonts/VCR_OSD_MONO SDF");
            if (configured)
            {
                return;
            }

            RectTransform panelRect = GetComponent<RectTransform>();
            panelRect.anchorMin = panelRect.anchorMax = new Vector2(0f, 1f);
            panelRect.pivot = new Vector2(0.5f, 0.5f);
            panelRect.anchoredPosition = new Vector2(725f, -162f);
            panelRect.sizeDelta = new Vector2(PanelWidth, PanelHeight);

            Image panelBackground = GetComponent<Image>() ?? gameObject.AddComponent<Image>();
            panelBackground.color = new Color(0.025f, 0.02f, 0.018f, 0.2f);
            panelBackground.raycastTarget = false;

            hpLabel = CreateText("HP", transform, new Vector2(0f, -49f),
                new Vector2(PanelWidth, 18f), 15f, TextAlignmentOptions.Left);
            mpLabel = CreateText("MP", transform, new Vector2(0f, -72f),
                new Vector2(PanelWidth, 18f), 15f, TextAlignmentOptions.Left);

            CreateSectionHeader("MAIN", new Vector2(0f, -92f));
            mainLabels = new[]
            {
                CreateText("ATK", transform, new Vector2(0f, -109f),
                    new Vector2(ColumnWidth, 17f), 12f, TextAlignmentOptions.Left),
                CreateText("DEF", transform, new Vector2(0f, -128f),
                    new Vector2(ColumnWidth, 17f), 12f, TextAlignmentOptions.Left),
                CreateText("SPELL", transform, new Vector2(0f, -147f),
                    new Vector2(ColumnWidth, 17f), 12f, TextAlignmentOptions.Left),
                CreateText("M.RES", transform, new Vector2(ColumnOffset, -109f),
                    new Vector2(ColumnWidth, 17f), 12f, TextAlignmentOptions.Left),
                CreateText("ASPD", transform, new Vector2(ColumnOffset, -128f),
                    new Vector2(ColumnWidth, 17f), 12f, TextAlignmentOptions.Left),
                CreateText("C.SPD", transform, new Vector2(ColumnOffset, -147f),
                    new Vector2(ColumnWidth, 17f), 12f, TextAlignmentOptions.Left)
            };

            CreateSectionHeader("SECONDARY", new Vector2(0f, -165f));
            secondaryLabels = new[]
            {
                CreateText("CRIT", transform, new Vector2(0f, -182f),
                    new Vector2(ColumnWidth, 17f), 12f, TextAlignmentOptions.Left),
                CreateText("EVA", transform, new Vector2(0f, -201f),
                    new Vector2(ColumnWidth, 17f), 12f, TextAlignmentOptions.Left),
                CreateText("ACC", transform, new Vector2(ColumnOffset, -182f),
                    new Vector2(ColumnWidth, 17f), 12f, TextAlignmentOptions.Left),
                CreateText("RANGE", transform, new Vector2(ColumnOffset, -201f),
                    new Vector2(ColumnWidth, 17f), 12f, TextAlignmentOptions.Left)
            };

            moreButton = CreateMoreButton();
            moreSection = new GameObject("More Stats", typeof(RectTransform));
            moreSection.transform.SetParent(transform, false);
            moreLabels = new[]
            {
                CreateText("C.DMG", moreSection.transform, new Vector2(0f, -1f),
                    new Vector2(ColumnWidth, 17f), 12f, TextAlignmentOptions.Left),
                CreateText("HP.REGEN", moreSection.transform, new Vector2(0f, -20f),
                    new Vector2(ColumnWidth, 17f), 12f, TextAlignmentOptions.Left),
                CreateText("MP.REGEN", moreSection.transform, new Vector2(ColumnOffset, -1f),
                    new Vector2(ColumnWidth, 17f), 12f, TextAlignmentOptions.Left),
                CreateText("CDR", moreSection.transform, new Vector2(ColumnOffset, -20f),
                    new Vector2(ColumnWidth, 17f), 12f, TextAlignmentOptions.Left)
            };

            RectTransform moreRect = moreSection.GetComponent<RectTransform>();
            moreRect.anchorMin = moreRect.anchorMax = new Vector2(0f, 1f);
            moreRect.pivot = new Vector2(0f, 1f);
            moreRect.anchoredPosition = new Vector2(0f, -252f);
            moreRect.sizeDelta = new Vector2(PanelWidth, 42f);
            moreSection.SetActive(false);
            configured = true;
        }

        public void Refresh(HeroState hero, HeroStats stats)
        {
            if (!configured || stats == null)
            {
                return;
            }

            int currentHealth = hero != null && hero.ResourcesInitialized
                ? hero.CurrentHealth
                : stats.MaxHealth;
            int currentMana = hero != null && hero.ResourcesInitialized
                ? hero.CurrentMana
                : stats.MaxMana;
            hpLabel.text = $"HP <color=#D95B5B><size=16>{currentHealth.ToString(CultureInfo.InvariantCulture)} / " +
                $"{stats.MaxHealth.ToString(CultureInfo.InvariantCulture)}</size></color>";
            mpLabel.text = $"MP <color=#5B8FD9><size=16>{currentMana.ToString(CultureInfo.InvariantCulture)} / " +
                $"{stats.MaxMana.ToString(CultureInfo.InvariantCulture)}</size></color>";

            mainLabels[0].text = $"ATK {FormatStatValue(stats.AttackPower)}";
            mainLabels[1].text = $"DEF {FormatStatValue(stats.Defense)}";
            mainLabels[2].text = $"SPELL {FormatStatValue(stats.SpellPower)}";
            mainLabels[3].text = $"M.RES {FormatStatValue(stats.MagicResistance)}";
            mainLabels[4].text = $"ASPD {FormatStatValue(stats.AttackSpeed)}";
            mainLabels[5].text = $"C.SPD {FormatStatValue(stats.CastSpeed)}";

            secondaryLabels[0].text = $"CRIT {FormatStatValue(FormatStat(stats.CriticalChance) + "%")}";
            secondaryLabels[1].text = $"EVA {FormatStatValue(FormatStat(stats.Evasion) + "%")}";
            secondaryLabels[2].text = $"ACC {FormatStatValue(FormatStat(stats.Accuracy) + "%")}";
            secondaryLabels[3].text = $"RANGE {FormatStatValue(stats.AttackRange.ToString(CultureInfo.InvariantCulture))}";

            moreLabels[0].text = $"C.DMG {FormatStatValue(FormatStat(stats.CriticalDamage) + "x")}";
            moreLabels[1].text = $"HP.REGEN {FormatStatValue(FormatStat(stats.HealthRegeneration))}";
            moreLabels[2].text = $"MP.REGEN {FormatStatValue(FormatStat(stats.ManaRegeneration))}";
            moreLabels[3].text = $"CDR {FormatStatValue(FormatStat(stats.CooldownReduction) + "%")}";
        }

        private void CreateSectionHeader(string title, Vector2 position)
        {
            TMP_Text label = CreateText(title, transform, position,
                new Vector2(PanelWidth, 16f), 9f, TextAlignmentOptions.Left);
            label.fontStyle = FontStyles.Bold;
            label.color = new Color(1f, 0.8f, 0.35f, 0.95f);
        }

        private Button CreateMoreButton()
        {
            GameObject buttonObject = new GameObject(
                "More Stats Button",
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(Image),
                typeof(Button));
            buttonObject.transform.SetParent(transform, false);
            RectTransform rect = buttonObject.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0f, 1f);
            rect.anchoredPosition = new Vector2(0f, -226f);
            rect.sizeDelta = new Vector2(MoreButtonWidth, 22f);

            Image image = buttonObject.GetComponent<Image>();
            image.color = new Color(0.1f, 0.08f, 0.05f, 0.78f);
            Button button = buttonObject.GetComponent<Button>();
            button.onClick.AddListener(ToggleMore);

            TMP_Text label = CreateText("Label", buttonObject.transform, Vector2.zero,
                new Vector2(MoreButtonWidth, 22f), 10f, TextAlignmentOptions.Center);
            label.text = "+";
            label.color = new Color(1f, 0.88f, 0.55f, 1f);
            return button;
        }

        private void ToggleMore()
        {
            moreExpanded = !moreExpanded;
            moreSection.SetActive(moreExpanded);
            TMP_Text label = moreButton.GetComponentInChildren<TMP_Text>();
            if (label != null)
            {
                label.text = moreExpanded ? "−" : "+";
            }
        }

        private TMP_Text CreateText(
            string name,
            Transform parent,
            Vector2 position,
            Vector2 size,
            float fontSize,
            TextAlignmentOptions alignment)
        {
            GameObject textObject = new GameObject(
                name,
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(TextMeshProUGUI));
            textObject.transform.SetParent(parent, false);
            TMP_Text label = textObject.GetComponent<TMP_Text>();
            label.font = font;
            label.fontSize = fontSize;
            label.color = new Color(0.96f, 0.98f, 1f, 1f);
            label.alignment = alignment;
            label.textWrappingMode = TextWrappingModes.NoWrap;
            label.overflowMode = TextOverflowModes.Overflow;
            label.raycastTarget = false;
            label.margin = Vector4.zero;

            RectTransform rect = label.rectTransform;
            rect.anchorMin = rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0f, 1f);
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
            return label;
        }

        private static string FormatStat(float value)
        {
            return value.ToString("0.##", CultureInfo.InvariantCulture);
        }

        private static string FormatStatValue(string value)
        {
            return $"<size=13>{value}</size>";
        }

        private static string FormatStatValue(float value)
        {
            return FormatStatValue(FormatStat(value));
        }
    }
}
