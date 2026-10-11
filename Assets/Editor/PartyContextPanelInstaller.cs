using TMPro;
using TaskbarTactics.Presentation;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace TaskbarTactics.Editor
{
    public static class PartyContextPanelInstaller
    {
        private const string MainScenePath = "Assets/Scenes/Main.unity";
        private const string ContextPanelLayoutPath = "Assets/Resources/UI/PartyContextLayout.png";

        [MenuItem("Taskbar Tactics/UI/Install Party Context Panel")]
        public static void InstallFromMenu()
        {
            Install();
        }

        public static void InstallFromCommandLine()
        {
            Install();
            EditorApplication.Exit(0);
        }

        private static void Install()
        {
            Scene scene = EditorSceneManager.OpenScene(MainScenePath, OpenSceneMode.Single);
            ManagementUiController controller = Object.FindFirstObjectByType<ManagementUiController>(
                FindObjectsInactive.Include);
            if (controller == null)
            {
                throw new System.InvalidOperationException(
                    $"Could not find {nameof(ManagementUiController)} in {MainScenePath}.");
            }

            SerializedObject serializedController = new SerializedObject(controller);
            SerializedProperty panels = serializedController.FindProperty("panels");
            if (panels == null || panels.arraySize == 0 ||
                panels.GetArrayElementAtIndex(0).objectReferenceValue is not GameObject partyPanel)
            {
                throw new System.InvalidOperationException(
                    "The Management UI controller has no authored Party panel reference.");
            }

            PartyContextPanelView view = EnsurePanel(partyPanel.transform);
            SerializedProperty panelReference = serializedController.FindProperty("partyContextPanel");
            if (panelReference == null)
            {
                throw new System.InvalidOperationException(
                    "The Party Context Panel reference is missing from ManagementUiController.");
            }

            panelReference.objectReferenceValue = view;
            serializedController.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(controller);
            EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(scene))
            {
                throw new System.InvalidOperationException($"Could not save {MainScenePath}.");
            }

            Debug.Log("Party context panel installed in Main.unity.");
        }

        private static PartyContextPanelView EnsurePanel(Transform parent)
        {
            Transform existing = parent.Find("Party Context Panel");
            GameObject root = existing != null
                ? existing.gameObject
                : new GameObject("Party Context Panel", typeof(RectTransform), typeof(CanvasRenderer),
                    typeof(Image), typeof(PartyContextPanelView));
            root.transform.SetParent(parent, false);
            root.SetActive(true);

            RectTransform rootRect = (RectTransform)root.transform;
            rootRect.anchorMin = rootRect.anchorMax = new Vector2(1f, 0f);
            rootRect.pivot = new Vector2(1f, 0f);
            rootRect.anchoredPosition = new Vector2(-75f, 75f);
            rootRect.sizeDelta = new Vector2(700f, 492f);
            rootRect.localScale = new Vector3(0.55f, 0.55f, 1f);

            Image background = root.GetComponent<Image>();
            background.sprite = LoadSprite(ContextPanelLayoutPath);
            background.type = Image.Type.Simple;
            background.preserveAspect = true;
            background.raycastTarget = false;

            Sprite emptyFrameSprite = LoadSprite("Assets/Resources/HeroClasses/empty.png");
            Sprite headerSprite = LoadSprite("Assets/Resources/UI/TopCurrencySingleLayout.png");
            Sprite headerGoldSprite = LoadSprite("Assets/Resources/UI/TopCurrencyLayout.png");
            TMP_FontAsset headingFont = Resources.Load<TMP_FontAsset>("UI/Fonts/TMP/Cinzel SemiBold");
            TMP_FontAsset bodyFont = Resources.Load<TMP_FontAsset>("UI/Fonts/TMP/Alegreya Medium");

            Image iconFrame = EnsureImage(root.transform, "Subject Empty Frame", emptyFrameSprite);
            SetTopLeftRect(iconFrame.rectTransform, new Vector2(24f, -116f), new Vector2(150f, 210f));
            iconFrame.preserveAspect = true;
            iconFrame.type = Image.Type.Simple;
            iconFrame.raycastTarget = false;

            Image subjectIcon = EnsureImage(root.transform, "Subject Icon", null);
            SetTopLeftRect(subjectIcon.rectTransform, new Vector2(35f, -138f), new Vector2(128f, 166f));
            subjectIcon.preserveAspect = true;
            subjectIcon.raycastTarget = false;

            Image header = EnsureImage(root.transform, "Panel Title Background",
                headerSprite != null ? headerSprite : headerGoldSprite);
            RectTransform headerRect = header.rectTransform;
            headerRect.anchorMin = headerRect.anchorMax = new Vector2(0.5f, 1f);
            headerRect.pivot = new Vector2(0.5f, 0.5f);
            headerRect.anchoredPosition = new Vector2(0f, -7f);
            headerRect.sizeDelta = new Vector2(300f, 58f);
            header.type = Image.Type.Sliced;
            header.preserveAspect = false;
            header.raycastTarget = false;
            header.enabled = false;

            TMP_Text title = EnsureText(root.transform, "Panel Title", headingFont, 22f,
                new Color(0.95f, 0.78f, 0.46f, 1f), TextAlignmentOptions.Center);
            SetCenteredRect(title.rectTransform, new Vector2(0f, -88f), new Vector2(300f, 38f), true);
            title.fontStyle = FontStyles.Bold;

            TMP_Text name = EnsureText(root.transform, "Subject Name", headingFont, 25f,
                new Color(0.3f, 0.68f, 0.96f, 1f), TextAlignmentOptions.Left);
            SetTopLeftRect(name.rectTransform, new Vector2(204f, -122f), new Vector2(450f, 38f));
            name.fontStyle = FontStyles.Bold;

            TMP_Text role = EnsureText(root.transform, "Subject Role", bodyFont, 20f,
                new Color(0.9f, 0.72f, 0.4f, 1f), TextAlignmentOptions.Left);
            SetTopLeftRect(role.rectTransform, new Vector2(204f, -163f), new Vector2(450f, 31f));

            TMP_Text description = EnsureText(root.transform, "Subject Description", bodyFont, 18f,
                new Color(0.96f, 0.92f, 0.84f, 1f), TextAlignmentOptions.TopLeft);
            SetTopLeftRect(description.rectTransform, new Vector2(204f, -202f), new Vector2(450f, 112f));
            description.textWrappingMode = TextWrappingModes.Normal;
            description.overflowMode = TextOverflowModes.Ellipsis;

            Image separator = EnsureImage(root.transform, "Info Divider", headerGoldSprite);
            SetTopLeftRect(separator.rectTransform, new Vector2(198f, -320f), new Vector2(462f, 4f));
            separator.preserveAspect = false;
            separator.color = new Color(0.86f, 0.62f, 0.25f, 0.8f);
            separator.raycastTarget = false;

            TMP_Text footerLabel = EnsureText(root.transform, "Context Footer Label", bodyFont, 16f,
                new Color(0.9f, 0.72f, 0.4f, 1f), TextAlignmentOptions.Left);
            SetTopLeftRect(footerLabel.rectTransform, new Vector2(204f, -332f), new Vector2(450f, 24f));

            TMP_Text footerValue = EnsureText(root.transform, "Context Footer Value", bodyFont, 17f,
                new Color(0.96f, 0.92f, 0.84f, 1f), TextAlignmentOptions.Left);
            SetTopLeftRect(footerValue.rectTransform, new Vector2(204f, -359f), new Vector2(450f, 72f));
            footerValue.textWrappingMode = TextWrappingModes.Normal;
            footerValue.overflowMode = TextOverflowModes.Ellipsis;

            PartyContextPanelView view = root.GetComponent<PartyContextPanelView>();
            view.Configure(iconFrame, subjectIcon, title, name, role, description, footerLabel, footerValue);
            EditorUtility.SetDirty(root);
            return view;
        }

        private static Image EnsureImage(Transform parent, string name, Sprite sprite)
        {
            Transform existing = parent.Find(name);
            GameObject target = existing != null
                ? existing.gameObject
                : new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            target.transform.SetParent(parent, false);
            Image image = target.GetComponent<Image>();
            image.sprite = sprite;
            image.raycastTarget = false;
            return image;
        }

        private static TMP_Text EnsureText(
            Transform parent,
            string name,
            TMP_FontAsset font,
            float fontSize,
            Color color,
            TextAlignmentOptions alignment)
        {
            Transform existing = parent.Find(name);
            GameObject target = existing != null
                ? existing.gameObject
                : new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
            target.transform.SetParent(parent, false);
            TMP_Text text = target.GetComponent<TMP_Text>();
            text.font = font != null ? font : TMP_Settings.defaultFontAsset;
            text.fontSize = fontSize;
            text.color = color;
            text.alignment = alignment;
            text.raycastTarget = false;
            text.enableAutoSizing = false;
            text.textWrappingMode = TextWrappingModes.NoWrap;
            text.overflowMode = TextOverflowModes.Ellipsis;
            text.margin = Vector4.zero;
            return text;
        }

        private static void SetTopLeftRect(RectTransform rect, Vector2 position, Vector2 size)
        {
            rect.anchorMin = rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0f, 1f);
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
        }

        private static void SetCenteredRect(RectTransform rect, Vector2 position, Vector2 size, bool topAnchor)
        {
            Vector2 anchor = topAnchor ? new Vector2(0.5f, 1f) : new Vector2(0.5f, 0.5f);
            rect.anchorMin = rect.anchorMax = anchor;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
        }

        private static Sprite LoadSprite(string path)
        {
            TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer != null && importer.textureType != TextureImporterType.Sprite)
            {
                importer.textureType = TextureImporterType.Sprite;
                importer.spriteImportMode = SpriteImportMode.Single;
                importer.alphaIsTransparency = true;
                importer.SaveAndReimport();
            }

            return AssetDatabase.LoadAssetAtPath<Sprite>(path);
        }
    }
}
