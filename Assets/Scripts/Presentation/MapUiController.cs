using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using TaskbarTactics.Content;
using UnityEngine;
using UnityEngine.UI;

namespace TaskbarTactics.Presentation
{
    [Serializable]
    public sealed class MapNodeView
    {
        public string NodeId;
        public Image Marker;
        public TMP_Text Label;
        public Image CurrentFlag;
        public MapNodeHoverTooltip Tooltip;
    }

    [Serializable]
    public sealed class MapRouteView
    {
        public string FromNodeId;
        public string ToNodeId;
        public List<Image> Dashes = new List<Image>();
    }

    [Serializable]
    public sealed class MapRoutePreferenceLegend
    {
        public string PreferenceName;
        public TMP_Text Label;
    }

    [Serializable]
    public sealed class MapRouteArtworkOverlay
    {
        public string PreferenceName;
        public RawImage Image;
    }

    public sealed class MapUiController : MonoBehaviour
    {
        [SerializeField] private RawImage mapBackground;
        [SerializeField] private List<MapNodeView> nodes = new List<MapNodeView>();
        [SerializeField] private List<MapRouteView> routes = new List<MapRouteView>();
        [SerializeField] private List<MapRoutePreferenceLegend> legends =
            new List<MapRoutePreferenceLegend>();
        [SerializeField] private RawImage nodeArtworkOverlay;
        [SerializeField] private List<MapRouteArtworkOverlay> routeArtworkOverlays =
            new List<MapRouteArtworkOverlay>();
        [SerializeField] private DraggableMapView draggableMap;

        private string lastFocusedNodeId;
        private Texture originalMapTexture;
        private Texture actTwoMapTexture;
        private Button actOneButton;
        private Button actTwoButton;
        private int selectedAct = 1;
        private int lastAppliedAct;
        private bool actTwoUnlocked;
        private Image actTwoLockDim;
        private Image actTwoLockBadge;
        private Sprite generatedBlockMapSprite;
        private bool actOneOpeningRouteOffsetsApplied;
        private readonly List<MapNodeView> actTwoNodes = new List<MapNodeView>();
        private readonly List<MapRouteView> actTwoRoutes = new List<MapRouteView>();
        private RectTransform actTwoRoutesLayer;
        private RectTransform actTwoNodesLayer;
        private bool actTwoPreviewLayoutApplied;
        private string lastRoutePreferenceName = "Safety";

        private static readonly Color LockedNode = new Color(0.18f, 0.19f, 0.2f, 0.82f);
        private static readonly Color AvailableNode = new Color(0.92f, 0.88f, 0.72f, 0.95f);
        private static readonly Color CurrentNode = new Color(1f, 0.74f, 0.2f, 1f);
        private static readonly Color CompletedNode = new Color(0.32f, 0.83f, 0.5f, 1f);
        private static readonly Color ChallengeNode = new Color(1f, 1f, 1f, 1f);
        private static readonly Color LockedRoute = new Color(1f, 1f, 1f, 0.04f);
        private static readonly Color PlannedRoute = new Color(1f, 1f, 1f, 0.82f);
        private static readonly Color ActBadgeText = new Color(0.2f, 0.1f, 0.03f, 1f);
        private const float ActTwoMarkerOffsetX = -75f;

        public event Action ActSelectionChanged;

        public bool IsActTwoSelected => selectedAct == 2;
        public bool IsActTwoUnlocked => actTwoUnlocked;

        public void Configure(
            RawImage background,
            IEnumerable<MapNodeView> nodeViews,
            IEnumerable<MapRouteView> routeViews,
            IEnumerable<MapRoutePreferenceLegend> routeLegends,
            RawImage nodeOverlay,
            IEnumerable<MapRouteArtworkOverlay> routeOverlays)
        {
            mapBackground = background;
            nodes = nodeViews.ToList();
            routes = routeViews.ToList();
            legends = routeLegends.ToList();
            nodeArtworkOverlay = nodeOverlay;
            routeArtworkOverlays = routeOverlays.ToList();
            draggableMap = GetComponent<DraggableMapView>();
        }

        public void Configure(
            RawImage background,
            IEnumerable<MapNodeView> nodeViews,
            IEnumerable<MapRouteView> routeViews,
            IEnumerable<MapRoutePreferenceLegend> routeLegends)
        {
            Configure(
                background,
                nodeViews,
                routeViews,
                routeLegends,
                null,
                Enumerable.Empty<MapRouteArtworkOverlay>());
        }

        public void Configure(
            RawImage background,
            IEnumerable<MapNodeView> nodeViews,
            IEnumerable<MapRouteView> routeViews)
        {
            Configure(
                background,
                nodeViews,
                routeViews,
                Enumerable.Empty<MapRoutePreferenceLegend>());
        }

        public void Refresh(GameAppController app)
        {
            if (app == null || app.Catalog?.Map == null)
            {
                return;
            }

            EnsureActSelector();
            lastRoutePreferenceName = app.State.Party.RoutePreference.ToString();
            actTwoUnlocked = IsActOneFinalBossComplete(app);
            if (selectedAct == 2)
            {
                ApplyActPreview();
                return;
            }

            ApplyActPreview();
            ApplyActOneOpeningRouteOffsets();

            HashSet<string> completed = new HashSet<string>(
                app.State.Expedition.CompletedNodeIds ?? Enumerable.Empty<string>());
            string currentNodeId = app.State.Expedition.CurrentNodeId;
            bool challengeMode = app.State.Party.RoutePreference.ToString()
                .Equals("Challenge", StringComparison.OrdinalIgnoreCase);
            if (!app.State.Expedition.IsActive && string.IsNullOrEmpty(currentNodeId))
            {
                currentNodeId = app.Catalog.Map.Nodes.FirstOrDefault()?.Id ?? string.Empty;
            }

            foreach (MapRouteView route in routes)
            {
                bool hasArtistRoutes = routeArtworkOverlays.Any(item =>
                    item.Image != null && item.Image.texture != null);
                bool plannedRoute = IsRouteOnPreferencePath(
                    route.FromNodeId,
                    route.ToNodeId,
                    app.State.Party.RoutePreference.ToString());
                foreach (Image dash in route.Dashes)
                {
                    if (dash != null)
                    {
                        dash.color = hasArtistRoutes || challengeMode
                            ? new Color(1f, 1f, 1f, 0f)
                            : plannedRoute ? PlannedRoute : LockedRoute;
                    }
                }
            }

            RectTransform currentNodeTransform = null;
            foreach (MapNodeView node in nodes)
            {
                MapNodeDefinition definition = app.Catalog.Map.FindNode(node.NodeId);
                bool isCurrent = node.NodeId == currentNodeId;
                bool isCompleted = completed.Contains(node.NodeId);
                bool isAvailable = app.Catalog.Map.Nodes.Any(item =>
                    completed.Contains(item.Id) && item.NextNodeIds.Contains(node.NodeId));

                Color color = challengeMode
                    ? ChallengeNode
                    : isCompleted ? CompletedNode :
                        isCurrent ? CurrentNode :
                        isAvailable ? AvailableNode :
                        LockedNode;
                if (node.Marker != null)
                {
                    node.Marker.gameObject.SetActive(true);
                    node.Marker.color = color;
                }

                if (node.CurrentFlag != null)
                {
                    node.CurrentFlag.gameObject.SetActive(isCurrent);
                }

                if (node.Label != null)
                {
                    node.Label.gameObject.SetActive(false);
                }

                if (node.Tooltip == null && node.Marker != null && node.Label != null)
                {
                    node.Tooltip = node.Marker.GetComponent<MapNodeHoverTooltip>() ??
                        node.Marker.gameObject.AddComponent<MapNodeHoverTooltip>();
                    node.Tooltip.Configure(node.Label);
                }

                if (node.Tooltip != null)
                {
                    string labelText = definition != null
                        ? $"{DisplayName(node.NodeId)}\n{definition.Type} - Dif. {definition.Difficulty}"
                        : DisplayName(node.NodeId);
                    node.Tooltip.SetContent(labelText, CurrentNode);
                }

                if (isCurrent && node.Marker != null)
                {
                    currentNodeTransform = node.Marker.transform.parent as RectTransform;
                }
            }

            if (currentNodeTransform != null && currentNodeId != lastFocusedNodeId)
            {
                draggableMap ??= GetComponent<DraggableMapView>();
                draggableMap?.FocusOnDefaultZoom(currentNodeTransform);
                lastFocusedNodeId = currentNodeId;
            }

            foreach (MapRoutePreferenceLegend legend in legends)
            {
                if (legend.Label == null)
                {
                    continue;
                }

                bool active = string.Equals(
                    legend.PreferenceName,
                    app.State.Party.RoutePreference.ToString(),
                    StringComparison.OrdinalIgnoreCase);
                legend.Label.color = active ? CurrentNode : AvailableNode;
                legend.Label.fontStyle = active ? FontStyles.Bold : FontStyles.Normal;
            }

            if (nodeArtworkOverlay != null)
            {
                nodeArtworkOverlay.gameObject.SetActive(nodeArtworkOverlay.texture != null);
            }

            foreach (MapRouteArtworkOverlay overlay in routeArtworkOverlays)
            {
                if (overlay.Image == null)
                {
                    continue;
                }

                bool active = string.Equals(
                    overlay.PreferenceName,
                    app.State.Party.RoutePreference.ToString(),
                    StringComparison.OrdinalIgnoreCase);
                overlay.Image.gameObject.SetActive(overlay.Image.texture != null);
                overlay.Image.color = active && !challengeMode
                    ? new Color(1f, 1f, 1f, 0.95f)
                    : new Color(1f, 1f, 1f, 0f);
            }
        }

        private void EnsureActSelector()
        {
            if (actOneButton != null && actTwoButton != null)
            {
                return;
            }

            Sprite badgeSprite = Resources.Load<Sprite>("UI/ActParchment");
            if (badgeSprite == null)
            {
                return;
            }

            Transform oldBadge = transform.Find("Act Badge");
            if (oldBadge != null)
            {
                Destroy(oldBadge.gameObject);
            }

            Transform selectorParent = transform.parent != null ? transform.parent : transform;
            Transform existingSelector = selectorParent.Find("Act Selector");
            Transform maskedSelector = transform.Find("Act Selector");
            if (existingSelector == null && maskedSelector != null)
            {
                existingSelector = maskedSelector;
                existingSelector.SetParent(selectorParent, false);
            }

            if (existingSelector != null)
            {
                actOneButton = existingSelector.Find("Act 1 Badge")?.GetComponent<Button>();
                actTwoButton = existingSelector.Find("Act 2 Badge")?.GetComponent<Button>();
                ArrangeActSelector(existingSelector as RectTransform);
            }
            else
            {
                GameObject selector = new GameObject("Act Selector", typeof(RectTransform));
                selector.transform.SetParent(selectorParent, false);
                RectTransform selectorRect = selector.GetComponent<RectTransform>();
                ArrangeActSelector(selectorRect);

                actOneButton = CreateActButton(selector.transform, badgeSprite, "Act 1", Vector2.zero, 1);
                actTwoButton = CreateActButton(selector.transform, badgeSprite, "Act 2", new Vector2(0f, -54f), 2);
                existingSelector = selector.transform;
            }

            ArrangeActButton(actOneButton, Vector2.zero);
            ArrangeActButton(actTwoButton, new Vector2(0f, -54f));
            ConfigureActButton(actOneButton, 1);
            ConfigureActButton(actTwoButton, 2);
            existingSelector.SetAsLastSibling();
            UpdateActButtonState();
        }

        private static void ArrangeActSelector(RectTransform selectorRect)
        {
            if (selectorRect == null)
            {
                return;
            }

            selectorRect.anchorMin = selectorRect.anchorMax = new Vector2(0f, 1f);
            selectorRect.pivot = new Vector2(0f, 1f);
            selectorRect.anchoredPosition = new Vector2(708f, -72f);
            selectorRect.sizeDelta = new Vector2(96f, 100f);
        }

        private static void ArrangeActButton(Button button, Vector2 position)
        {
            if (button == null)
            {
                return;
            }

            RectTransform badgeRect = button.image != null
                ? button.image.rectTransform
                : button.GetComponent<RectTransform>();
            badgeRect.anchorMin = badgeRect.anchorMax = new Vector2(0f, 1f);
            badgeRect.pivot = new Vector2(0f, 1f);
            badgeRect.anchoredPosition = position;
            badgeRect.sizeDelta = new Vector2(92f, 38f);

            TMP_Text label = button.GetComponentInChildren<TMP_Text>(true);
            if (label != null)
            {
                label.fontSize = 15f;
                label.rectTransform.anchoredPosition = new Vector2(-4f, -2f);
                label.rectTransform.sizeDelta = new Vector2(66f, 21f);
            }
        }

        private Button CreateActButton(
            Transform parent,
            Sprite badgeSprite,
            string labelText,
            Vector2 position,
            int actNumber)
        {
            GameObject badgeObject = new GameObject(
                $"{labelText} Badge", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
            badgeObject.transform.SetParent(parent, false);
            Image badge = badgeObject.GetComponent<Image>();
            badge.sprite = badgeSprite;
            badge.color = new Color(1f, 1f, 1f, 0.88f);
            badge.preserveAspect = false;
            badge.raycastTarget = true;

            RectTransform badgeRect = badge.rectTransform;
            badgeRect.anchorMin = badgeRect.anchorMax = new Vector2(0f, 1f);
            badgeRect.pivot = new Vector2(0f, 1f);
            badgeRect.anchoredPosition = position;
            badgeRect.sizeDelta = new Vector2(92f, 38f);

            GameObject labelObject = new GameObject(
                "Act Label", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
            labelObject.transform.SetParent(badgeObject.transform, false);
            TextMeshProUGUI label = labelObject.GetComponent<TextMeshProUGUI>();
            label.text = labelText;
            label.font = Resources.Load<TMP_FontAsset>("UI/Fonts/VCR_OSD_MONO SDF");
            label.fontSize = 15f;
            label.fontStyle = FontStyles.Bold;
            label.color = ActBadgeText;
            label.alignment = TextAlignmentOptions.Center;
            label.raycastTarget = false;

            RectTransform labelRect = label.rectTransform;
            labelRect.anchorMin = labelRect.anchorMax = new Vector2(0.5f, 0.5f);
            labelRect.pivot = new Vector2(0.5f, 0.5f);
            labelRect.anchoredPosition = new Vector2(-4f, -2f);
            labelRect.sizeDelta = new Vector2(66f, 21f);

            Button button = badgeObject.GetComponent<Button>();
            ConfigureActButton(button, actNumber);
            return button;
        }

        private void ConfigureActButton(Button button, int actNumber)
        {
            if (button == null)
            {
                return;
            }

            button.transition = Selectable.Transition.ColorTint;
            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(() => SelectAct(actNumber));
        }

        public void SelectAct(int actNumber)
        {
            selectedAct = Mathf.Clamp(actNumber, 1, 2);
            ApplyActPreview();
            ActSelectionChanged?.Invoke();
        }

        private void ApplyActPreview()
        {
            if (mapBackground != null && originalMapTexture == null)
            {
                originalMapTexture = Resources.Load<Texture2D>("Maps/map1") ?? mapBackground.texture;
            }

            bool showActOneGameplay = selectedAct == 1;
            bool actChanged = lastAppliedAct != selectedAct;
            lastAppliedAct = selectedAct;
            if (mapBackground != null)
            {
                if (showActOneGameplay)
                {
                    originalMapTexture ??= Resources.Load<Texture2D>("Maps/map1");
                    mapBackground.texture = originalMapTexture;
                }
                else
                {
                    actTwoMapTexture ??= Resources.Load<Texture2D>("Maps/mapa2");
                    mapBackground.texture = actTwoMapTexture != null ? actTwoMapTexture : originalMapTexture;
                }
            }

            if (actChanged)
            {
                draggableMap ??= GetComponent<DraggableMapView>();
                draggableMap?.ResetView();
                lastFocusedNodeId = string.Empty;
            }

            foreach (MapNodeView node in nodes)
            {
                if (node.Marker != null && node.Marker.transform.parent != null)
                {
                    node.Marker.transform.parent.gameObject.SetActive(showActOneGameplay);
                }
            }

            foreach (MapRouteView route in routes)
            {
                foreach (Image dash in route.Dashes)
                {
                    if (dash != null)
                    {
                        dash.gameObject.SetActive(showActOneGameplay);
                    }
                }
            }

            foreach (MapRoutePreferenceLegend legend in legends)
            {
                if (legend.Label != null)
                {
                    legend.Label.gameObject.SetActive(showActOneGameplay);
                }
            }

            if (nodeArtworkOverlay != null)
            {
                nodeArtworkOverlay.gameObject.SetActive(showActOneGameplay && nodeArtworkOverlay.texture != null);
            }

            foreach (MapRouteArtworkOverlay overlay in routeArtworkOverlays)
            {
                if (overlay.Image != null)
                {
                    overlay.Image.gameObject.SetActive(showActOneGameplay);
                }
            }

            EnsureActTwoPreview();
            bool showActTwoPreview = !showActOneGameplay;
            if (actTwoRoutesLayer != null)
            {
                actTwoRoutesLayer.gameObject.SetActive(showActTwoPreview);
            }

            if (actTwoNodesLayer != null)
            {
                actTwoNodesLayer.gameObject.SetActive(showActTwoPreview);
            }

            if (showActTwoPreview)
            {
                RefreshActTwoPreview();
            }

            EnsureActTwoLockOverlay();
            bool showLockedOverlay = showActTwoPreview && !actTwoUnlocked;
            if (actTwoLockDim != null)
            {
                actTwoLockDim.gameObject.SetActive(showLockedOverlay);
                actTwoLockDim.transform.SetAsLastSibling();
            }

            if (actTwoLockBadge != null)
            {
                actTwoLockBadge.gameObject.SetActive(showLockedOverlay);
                actTwoLockBadge.transform.SetAsLastSibling();
            }

            UpdateActButtonState();
        }

        private void EnsureActTwoLockOverlay()
        {
            if (actTwoLockDim != null && actTwoLockBadge != null)
            {
                return;
            }

            RectTransform parent = transform as RectTransform;
            if (parent == null)
            {
                return;
            }

            Image dim = CreateRuntimeImage(parent, "Act 2 Locked Dim");
            dim.color = new Color(0f, 0f, 0f, 0.5f);
            dim.raycastTarget = false;
            dim.gameObject.SetActive(false);

            RectTransform dimRect = dim.rectTransform;
            dimRect.anchorMin = Vector2.zero;
            dimRect.anchorMax = Vector2.one;
            dimRect.pivot = new Vector2(0.5f, 0.5f);
            dimRect.offsetMin = Vector2.zero;
            dimRect.offsetMax = Vector2.zero;

            Image badge = CreateRuntimeImage(parent, "Act 2 Locked Badge");
            badge.sprite = LoadBlockMapSprite();
            badge.color = new Color(1f, 1f, 1f, 0.92f);
            badge.preserveAspect = false;
            badge.raycastTarget = false;
            badge.gameObject.SetActive(false);

            RectTransform badgeRect = badge.rectTransform;
            badgeRect.anchorMin = badgeRect.anchorMax = new Vector2(0.5f, 0.5f);
            badgeRect.pivot = new Vector2(0.5f, 0.5f);
            badgeRect.anchoredPosition = new Vector2(0f, 8f);
            badgeRect.sizeDelta = new Vector2(428f, 149f);

            actTwoLockDim = dim;
            actTwoLockBadge = badge;
        }

        private static bool IsActOneFinalBossComplete(GameAppController app)
        {
            return app?.State?.Expedition?.CompletedNodeIds != null &&
                app.State.Expedition.CompletedNodeIds.Contains("last_bastion");
        }

        private Sprite LoadBlockMapSprite()
        {
            Sprite importedSprite = Resources.Load<Sprite>("UI/blockmap");
            if (importedSprite != null)
            {
                return importedSprite;
            }

            Texture2D texture = Resources.Load<Texture2D>("UI/blockmap");
            if (texture == null)
            {
                return null;
            }

            generatedBlockMapSprite = Sprite.Create(
                texture,
                new Rect(0f, 0f, texture.width, texture.height),
                new Vector2(0.5f, 0.5f),
                100f);
            return generatedBlockMapSprite;
        }

        private void OnDestroy()
        {
            if (generatedBlockMapSprite != null)
            {
                Destroy(generatedBlockMapSprite);
                generatedBlockMapSprite = null;
            }
        }

        private void EnsureActTwoPreview()
        {
            if (actTwoNodesLayer != null && actTwoRoutesLayer != null && actTwoNodes.Count > 0)
            {
                if (!actTwoPreviewLayoutApplied)
                {
                    ArrangeActTwoPreview();
                    actTwoPreviewLayoutApplied = true;
                }
                return;
            }

            RectTransform parent = mapBackground != null
                ? mapBackground.transform.parent as RectTransform
                : transform as RectTransform;
            if (parent == null)
            {
                return;
            }

            Sprite markerSprite = nodes.FirstOrDefault(node => node.Marker != null)?.Marker.sprite;
            actTwoRoutesLayer = CreateActTwoLayer(parent, "Act 2 Routes");
            actTwoNodesLayer = CreateActTwoLayer(parent, "Act 2 Nodes");
            actTwoRoutes.Clear();
            actTwoNodes.Clear();

            Dictionary<string, Vector2> positions = ActTwoNodePositions();
            RebuildActTwoRoutes(positions);

            foreach (KeyValuePair<string, Vector2> node in positions)
            {
                actTwoNodes.Add(CreateActTwoNode(actTwoNodesLayer, node.Key, node.Value, markerSprite));
            }

            actTwoPreviewLayoutApplied = true;
        }

        private void ArrangeActTwoPreview()
        {
            Dictionary<string, Vector2> positions = ActTwoNodePositions();
            foreach (MapNodeView node in actTwoNodes)
            {
                if (node == null ||
                    string.IsNullOrWhiteSpace(node.NodeId) ||
                    !positions.TryGetValue(node.NodeId, out Vector2 position))
                {
                    continue;
                }

                RectTransform nodeRect = node.Marker != null
                    ? node.Marker.transform.parent as RectTransform
                    : null;
                if (nodeRect != null)
                {
                    nodeRect.anchoredPosition = new Vector2(position.x, -position.y);
                }
            }

            RebuildActTwoRoutes(positions);
        }

        private void RebuildActTwoRoutes(Dictionary<string, Vector2> positions)
        {
            if (actTwoRoutesLayer == null || positions == null)
            {
                return;
            }

            for (int i = actTwoRoutesLayer.childCount - 1; i >= 0; i--)
            {
                DestroyRouteObject(actTwoRoutesLayer.GetChild(i).gameObject);
            }

            actTwoRoutes.Clear();
            string[,] routeIds = ActTwoRouteIds();
            for (int i = 0; i < routeIds.GetLength(0); i++)
            {
                string from = routeIds[i, 0];
                string to = routeIds[i, 1];
                if (positions.TryGetValue(from, out Vector2 start) &&
                    positions.TryGetValue(to, out Vector2 end))
                {
                    actTwoRoutes.Add(CreateActTwoRoute(actTwoRoutesLayer, from, to, start, end));
                }
            }
        }

        private static RectTransform CreateActTwoLayer(Transform parent, string name)
        {
            GameObject layer = new GameObject(name, typeof(RectTransform));
            layer.transform.SetParent(parent, false);
            RectTransform rect = layer.GetComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            return rect;
        }

        private static void DestroyRouteObject(GameObject routeObject)
        {
            if (routeObject == null)
            {
                return;
            }

            if (Application.isPlaying)
            {
                Destroy(routeObject);
            }
            else
            {
                DestroyImmediate(routeObject);
            }
        }

        private static Dictionary<string, Vector2> ActTwoNodePositions()
        {
            return new Dictionary<string, Vector2>
            {
                ["city2"] = new Vector2(266.2f, 568.4f),
                ["corrupt_pass"] = new Vector2(313.9f, 491.6f),
                ["lo_hueso"] = new Vector2(378.7f, 444.4f),
                ["mt_secret"] = new Vector2(458.5f, 495.3f),
                ["ancient_ruins"] = new Vector2(446.3f, 395f),
                ["arbol_morto"] = new Vector2(393.9f, 355.7f),
                ["mountain_pass_act2"] = new Vector2(366.8f, 274.9f),
                ["black_tower"] = new Vector2(304.4f, 159.8f),
                ["port"] = new Vector2(227.8f, 219.3f),
                ["lost_bay"] = new Vector2(158.2f, 293.1f)
            };
        }

        private static string[,] ActTwoRouteIds()
        {
            return new[,]
            {
                { "city2", "corrupt_pass" },
                { "corrupt_pass", "lo_hueso" },
                { "lo_hueso", "mt_secret" },
                { "mt_secret", "ancient_ruins" },
                { "lo_hueso", "ancient_ruins" },
                { "ancient_ruins", "arbol_morto" },
                { "arbol_morto", "mountain_pass_act2" },
                { "mountain_pass_act2", "black_tower" },
                { "black_tower", "port" },
                { "mountain_pass_act2", "port" },
                { "port", "lost_bay" }
            };
        }

        private static MapRouteView CreateActTwoRoute(
            Transform parent,
            string from,
            string to,
            Vector2 start,
            Vector2 end)
        {
            GameObject routeRoot = new GameObject($"{from} to {to}", typeof(RectTransform));
            routeRoot.transform.SetParent(parent, false);
            RectTransform routeRootRect = routeRoot.GetComponent<RectTransform>();
            routeRootRect.anchorMin = Vector2.zero;
            routeRootRect.anchorMax = Vector2.one;
            routeRootRect.offsetMin = Vector2.zero;
            routeRootRect.offsetMax = Vector2.zero;

            Vector2 uiStart = new Vector2(start.x + ActTwoMarkerOffsetX, -start.y);
            Vector2 uiEnd = new Vector2(end.x + ActTwoMarkerOffsetX, -end.y);
            Vector2 delta = uiEnd - uiStart;
            float distance = delta.magnitude;
            Vector2 direction = delta.normalized;
            float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
            int dashCount = Mathf.Max(2, Mathf.FloorToInt(distance / 18f));
            List<Image> dashes = new List<Image>();
            for (int i = 0; i < dashCount; i++)
            {
                float t = dashCount == 1 ? 0.5f : i / (dashCount - 1f);
                Vector2 position = Vector2.Lerp(uiStart, uiEnd, t);
                GameObject dashObject = new GameObject($"Dash {i + 1:00}", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
                dashObject.transform.SetParent(routeRoot.transform, false);
                Image dash = dashObject.GetComponent<Image>();
                dash.color = LockedRoute;
                dash.raycastTarget = false;

                RectTransform rect = dash.rectTransform;
                rect.anchorMin = rect.anchorMax = new Vector2(0f, 1f);
                rect.pivot = new Vector2(0.5f, 0.5f);
                rect.anchoredPosition = position;
                rect.sizeDelta = new Vector2(10f, 3f);
                rect.localRotation = Quaternion.Euler(0f, 0f, angle);
                dashes.Add(dash);
            }

            return new MapRouteView
            {
                FromNodeId = from,
                ToNodeId = to,
                Dashes = dashes
            };
        }

        private static MapNodeView CreateActTwoNode(Transform parent, string nodeId, Vector2 position, Sprite markerSprite)
        {
            GameObject nodeRoot = new GameObject(nodeId, typeof(RectTransform));
            nodeRoot.transform.SetParent(parent, false);
            RectTransform nodeRect = nodeRoot.GetComponent<RectTransform>();
            nodeRect.anchorMin = nodeRect.anchorMax = new Vector2(0f, 1f);
            nodeRect.pivot = new Vector2(0.5f, 0.5f);
            nodeRect.anchoredPosition = new Vector2(position.x, -position.y);
            nodeRect.sizeDelta = new Vector2(150f, 46f);

            Image marker = CreateRuntimeImage(nodeRoot.transform, "Marker");
            marker.sprite = markerSprite;
            marker.color = AvailableNode;
            marker.preserveAspect = true;
            marker.rectTransform.anchorMin = marker.rectTransform.anchorMax = new Vector2(0f, 0.5f);
            marker.rectTransform.pivot = new Vector2(0.5f, 0.5f);
            marker.rectTransform.anchoredPosition = Vector2.zero;
            marker.rectTransform.sizeDelta = new Vector2(10f, 10f);

            Image hoverArea = CreateRuntimeImage(nodeRoot.transform, "Hover Area");
            hoverArea.color = new Color(1f, 1f, 1f, 0f);
            hoverArea.rectTransform.anchorMin = hoverArea.rectTransform.anchorMax = new Vector2(0f, 0.5f);
            hoverArea.rectTransform.pivot = new Vector2(0.5f, 0.5f);
            hoverArea.rectTransform.anchoredPosition = Vector2.zero;
            hoverArea.rectTransform.sizeDelta = new Vector2(44f, 34f);

            Image labelBackground = CreateRuntimeImage(nodeRoot.transform, "Tooltip Background");
            labelBackground.color = new Color(0f, 0f, 0f, 0.5f);
            labelBackground.raycastTarget = false;
            labelBackground.rectTransform.anchorMin = labelBackground.rectTransform.anchorMax = new Vector2(0f, 0.5f);
            labelBackground.rectTransform.pivot = new Vector2(0f, 0.5f);
            labelBackground.rectTransform.anchoredPosition = new Vector2(10f, -2f);
            labelBackground.rectTransform.sizeDelta = new Vector2(116f, 42f);
            labelBackground.gameObject.SetActive(false);

            GameObject labelObject = new GameObject("Label", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
            labelObject.transform.SetParent(nodeRoot.transform, false);
            TMP_Text label = labelObject.GetComponent<TMP_Text>();
            label.font = Resources.Load<TMP_FontAsset>("UI/Fonts/VCR_OSD_MONO SDF");
            label.fontSize = 10f;
            label.alignment = TextAlignmentOptions.Left;
            label.textWrappingMode = TextWrappingModes.Normal;
            label.raycastTarget = false;
            label.rectTransform.anchorMin = label.rectTransform.anchorMax = new Vector2(0f, 0.5f);
            label.rectTransform.pivot = new Vector2(0f, 0.5f);
            label.rectTransform.anchoredPosition = new Vector2(14f, 8f);
            label.rectTransform.sizeDelta = new Vector2(136f, 36f);
            label.gameObject.SetActive(false);

            MapNodeHoverTooltip tooltip = hoverArea.gameObject.AddComponent<MapNodeHoverTooltip>();
            tooltip.Configure(label, labelBackground.gameObject);

            return new MapNodeView
            {
                NodeId = nodeId,
                Marker = marker,
                Label = label,
                Tooltip = tooltip
            };
        }

        private static Image CreateRuntimeImage(Transform parent, string name)
        {
            GameObject imageObject = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            imageObject.transform.SetParent(parent, false);
            Image image = imageObject.GetComponent<Image>();
            image.raycastTarget = true;
            return image;
        }

        private void ApplyActOneOpeningRouteOffsets()
        {
            if (actOneOpeningRouteOffsetsApplied)
            {
                return;
            }

            OffsetRoute("town", "narrow_bridge", 7f);
            LimitRouteDashes("town", "narrow_bridge", 4);
            OffsetRoute("narrow_bridge", "cave", 4f);
            actOneOpeningRouteOffsetsApplied = true;
        }

        private void OffsetRoute(string fromNodeId, string toNodeId, float yOffset)
        {
            MapRouteView route = routes.FirstOrDefault(candidate =>
                candidate != null &&
                string.Equals(candidate.FromNodeId, fromNodeId, StringComparison.OrdinalIgnoreCase) &&
                string.Equals(candidate.ToNodeId, toNodeId, StringComparison.OrdinalIgnoreCase));
            if (route == null)
            {
                return;
            }

            foreach (Image dash in route.Dashes)
            {
                if (dash != null)
                {
                    dash.rectTransform.anchoredPosition += new Vector2(0f, yOffset);
                }
            }
        }

        private void LimitRouteDashes(string fromNodeId, string toNodeId, int visibleDashCount)
        {
            MapRouteView route = routes.FirstOrDefault(candidate =>
                candidate != null &&
                string.Equals(candidate.FromNodeId, fromNodeId, StringComparison.OrdinalIgnoreCase) &&
                string.Equals(candidate.ToNodeId, toNodeId, StringComparison.OrdinalIgnoreCase));
            if (route == null)
            {
                return;
            }

            for (int i = 0; i < route.Dashes.Count; i++)
            {
                if (route.Dashes[i] != null)
                {
                    route.Dashes[i].gameObject.SetActive(i < visibleDashCount);
                }
            }
        }

        private void RefreshActTwoPreview()
        {
            foreach (MapRouteView route in actTwoRoutes)
            {
                bool showRoutes = !string.Equals(
                    lastRoutePreferenceName,
                    "Challenge",
                    StringComparison.OrdinalIgnoreCase);
                bool plannedRoute = IsActTwoRouteOnPreferencePath(
                    route.FromNodeId,
                    route.ToNodeId,
                    lastRoutePreferenceName);
                foreach (Image dash in route.Dashes)
                {
                    if (dash != null)
                    {
                        dash.gameObject.SetActive(showRoutes);
                        dash.color = plannedRoute ? PlannedRoute : LockedRoute;
                    }
                }
            }

            foreach (MapNodeView node in actTwoNodes)
            {
                if (node.Marker != null)
                {
                    node.Marker.color = IsActTwoNodeOnPreferencePath(node.NodeId, lastRoutePreferenceName)
                        ? AvailableNode
                        : LockedNode;
                }

                node.Tooltip?.SetContent(ActTwoDisplayName(node.NodeId), CurrentNode);
            }
        }

        private void UpdateActButtonState()
        {
            TintActButton(actOneButton, selectedAct == 1);
            TintActButton(actTwoButton, selectedAct == 2);
        }

        private static void TintActButton(Button button, bool selected)
        {
            if (button == null || button.image == null)
            {
                return;
            }

            button.image.color = selected
                ? new Color(1f, 1f, 1f, 0.98f)
                : new Color(0.78f, 0.78f, 0.78f, 0.72f);
        }

        private static string DisplayName(string nodeId)
        {
            switch (nodeId)
            {
                case "town":
                    return "Inicio / Town";
                case "narrow_bridge":
                    return "Puente estrecho";
                case "cave":
                    return "Cueva";
                case "cemetery":
                    return "Cementerio";
                case "goblin_village":
                    return "Aldea goblin";
                case "tomb_pass":
                    return "Paso del Tomuer";
                case "mountain_pass":
                    return "Paso entre montanas";
                case "lost_forest":
                    return "Lost Forest";
                case "last_bastion":
                    return "Ultimo bastion";
                default:
                    return nodeId;
            }
        }

        private static bool IsRouteOnPreferencePath(string fromNodeId, string toNodeId, string preferenceName)
        {
            switch (preferenceName)
            {
                case "Loot":
                    return IsRoute(
                        fromNodeId,
                        toNodeId,
                        "town",
                        "narrow_bridge",
                        "cave",
                        "goblin_village",
                        "mountain_pass",
                        "lost_forest",
                        "last_bastion");
                case "Safety":
                    return IsRoute(
                        fromNodeId,
                        toNodeId,
                        "town",
                        "narrow_bridge",
                        "cemetery",
                        "goblin_village",
                        "mountain_pass",
                        "lost_forest",
                        "last_bastion");
                default:
                    return false;
            }
        }

        private static string ActTwoDisplayName(string nodeId)
        {
            switch (nodeId)
            {
                case "city2":
                    return "City2";
                case "corrupt_pass":
                    return "Corrupt Pass";
                case "lo_hueso":
                    return "Lo'Hueso'";
                case "mt_secret":
                    return "Mt. Secret";
                case "ancient_ruins":
                    return "Ancient Ruins";
                case "arbol_morto":
                    return "Arbol Morto";
                case "mountain_pass_act2":
                    return "Mountain Pass";
                case "black_tower":
                    return "Black Tower";
                case "port":
                    return "Port...?";
                case "lost_bay":
                    return "Lost Bay";
                default:
                    return nodeId;
            }
        }

        private static bool IsActTwoNodeOnPreferencePath(string nodeId, string preferenceName)
        {
            switch (preferenceName)
            {
                case "Loot":
                    return IsNodeInPath(
                        nodeId,
                        "city2",
                        "corrupt_pass",
                        "lo_hueso",
                        "mt_secret",
                        "ancient_ruins",
                        "arbol_morto",
                        "mountain_pass_act2",
                        "port",
                        "lost_bay");
                case "Challenge":
                    return IsNodeInPath(
                        nodeId,
                        "city2",
                        "corrupt_pass",
                        "lo_hueso",
                        "mt_secret",
                        "ancient_ruins",
                        "arbol_morto",
                        "mountain_pass_act2",
                        "black_tower",
                        "port",
                        "lost_bay");
                case "Safety":
                default:
                    return IsNodeInPath(
                        nodeId,
                        "city2",
                        "corrupt_pass",
                        "lo_hueso",
                        "ancient_ruins",
                        "arbol_morto",
                        "mountain_pass_act2",
                        "port",
                        "lost_bay");
            }
        }

        private static bool IsActTwoRouteOnPreferencePath(string fromNodeId, string toNodeId, string preferenceName)
        {
            switch (preferenceName)
            {
                case "Loot":
                    return IsRoute(
                        fromNodeId,
                        toNodeId,
                        "city2",
                        "corrupt_pass",
                        "lo_hueso",
                        "mt_secret",
                        "ancient_ruins",
                        "arbol_morto",
                        "mountain_pass_act2",
                        "port",
                        "lost_bay");
                case "Challenge":
                    return IsRoute(
                        fromNodeId,
                        toNodeId,
                        "city2",
                        "corrupt_pass",
                        "lo_hueso",
                        "mt_secret",
                        "ancient_ruins",
                        "arbol_morto",
                        "mountain_pass_act2",
                        "black_tower",
                        "port",
                        "lost_bay");
                case "Safety":
                default:
                    return IsRoute(
                        fromNodeId,
                        toNodeId,
                        "city2",
                        "corrupt_pass",
                        "lo_hueso",
                        "ancient_ruins",
                        "arbol_morto",
                        "mountain_pass_act2",
                        "port",
                        "lost_bay");
            }
        }

        private static bool IsNodeInPath(string nodeId, params string[] path)
        {
            return path.Any(item => item == nodeId);
        }

        private static bool IsRoute(string fromNodeId, string toNodeId, params string[] path)
        {
            for (int i = 0; i < path.Length - 1; i++)
            {
                if (path[i] == fromNodeId && path[i + 1] == toNodeId)
                {
                    return true;
                }
            }

            return false;
        }
    }
}
