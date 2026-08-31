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

        private static readonly Color LockedNode = new Color(0.18f, 0.19f, 0.2f, 0.82f);
        private static readonly Color AvailableNode = new Color(0.92f, 0.88f, 0.72f, 0.95f);
        private static readonly Color CurrentNode = new Color(1f, 0.74f, 0.2f, 1f);
        private static readonly Color CompletedNode = new Color(0.32f, 0.83f, 0.5f, 1f);
        private static readonly Color ChallengeNode = new Color(1f, 1f, 1f, 1f);
        private static readonly Color LockedRoute = new Color(1f, 1f, 1f, 0.04f);
        private static readonly Color PlannedRoute = new Color(1f, 1f, 1f, 0.82f);
        private static readonly Color ActBadgeText = new Color(0.2f, 0.1f, 0.03f, 1f);

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
            if (selectedAct == 2)
            {
                ApplyActPreview();
                return;
            }

            ApplyActPreview();

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

            Transform existingSelector = transform.Find("Act Selector");
            if (existingSelector != null)
            {
                actOneButton = existingSelector.Find("Act 1 Badge")?.GetComponent<Button>();
                actTwoButton = existingSelector.Find("Act 2 Badge")?.GetComponent<Button>();
            }
            else
            {
                GameObject selector = new GameObject("Act Selector", typeof(RectTransform));
                selector.transform.SetParent(transform, false);
                RectTransform selectorRect = selector.GetComponent<RectTransform>();
                selectorRect.anchorMin = selectorRect.anchorMax = new Vector2(1f, 1f);
                selectorRect.pivot = new Vector2(1f, 1f);
                selectorRect.anchoredPosition = new Vector2(-6f, -10f);
                selectorRect.sizeDelta = new Vector2(236f, 54f);

                actOneButton = CreateActButton(selector.transform, badgeSprite, "Act 1", new Vector2(-120f, 0f), 1);
                actTwoButton = CreateActButton(selector.transform, badgeSprite, "Act 2", Vector2.zero, 2);
                existingSelector = selector.transform;
            }

            ConfigureActButton(actOneButton, 1);
            ConfigureActButton(actTwoButton, 2);
            existingSelector.SetAsLastSibling();
            UpdateActButtonState();
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
            badgeRect.anchorMin = badgeRect.anchorMax = new Vector2(1f, 1f);
            badgeRect.pivot = new Vector2(1f, 1f);
            badgeRect.anchoredPosition = position;
            badgeRect.sizeDelta = new Vector2(116f, 49f);

            GameObject labelObject = new GameObject(
                "Act Label", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
            labelObject.transform.SetParent(badgeObject.transform, false);
            TextMeshProUGUI label = labelObject.GetComponent<TextMeshProUGUI>();
            label.text = labelText;
            label.font = Resources.Load<TMP_FontAsset>("UI/Fonts/VCR_OSD_MONO SDF");
            label.fontSize = 18f;
            label.fontStyle = FontStyles.Bold;
            label.color = ActBadgeText;
            label.alignment = TextAlignmentOptions.Center;
            label.raycastTarget = false;

            RectTransform labelRect = label.rectTransform;
            labelRect.anchorMin = labelRect.anchorMax = new Vector2(0.5f, 0.5f);
            labelRect.pivot = new Vector2(0.5f, 0.5f);
            labelRect.anchoredPosition = new Vector2(0f, -3f);
            labelRect.sizeDelta = new Vector2(82f, 26f);

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

        private void SelectAct(int actNumber)
        {
            selectedAct = Mathf.Clamp(actNumber, 1, 2);
            ApplyActPreview();
        }

        private void ApplyActPreview()
        {
            if (mapBackground != null && originalMapTexture == null)
            {
                originalMapTexture = mapBackground.texture;
            }

            bool showActOneGameplay = selectedAct == 1;
            if (mapBackground != null)
            {
                if (showActOneGameplay)
                {
                    mapBackground.texture = originalMapTexture;
                }
                else
                {
                    actTwoMapTexture ??= Resources.Load<Texture2D>("Maps/mapa2");
                    mapBackground.texture = actTwoMapTexture != null ? actTwoMapTexture : originalMapTexture;
                }
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

            UpdateActButtonState();
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
