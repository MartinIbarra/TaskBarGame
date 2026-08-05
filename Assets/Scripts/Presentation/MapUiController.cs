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

        private static readonly Color LockedNode = new Color(0.18f, 0.19f, 0.2f, 0.82f);
        private static readonly Color AvailableNode = new Color(0.92f, 0.88f, 0.72f, 0.95f);
        private static readonly Color CurrentNode = new Color(1f, 0.74f, 0.2f, 1f);
        private static readonly Color CompletedNode = new Color(0.32f, 0.83f, 0.5f, 1f);
        private static readonly Color LockedRoute = new Color(1f, 1f, 1f, 0.04f);
        private static readonly Color AvailableRoute = new Color(1f, 1f, 1f, 0.22f);

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

            HashSet<string> completed = new HashSet<string>(
                app.State.Expedition.CompletedNodeIds ?? Enumerable.Empty<string>());
            string currentNodeId = app.State.Expedition.CurrentNodeId;
            if (!app.State.Expedition.IsActive && string.IsNullOrEmpty(currentNodeId))
            {
                currentNodeId = app.Catalog.Map.Nodes.FirstOrDefault()?.Id ?? string.Empty;
            }

            foreach (MapRouteView route in routes)
            {
                bool unlocked = completed.Contains(route.FromNodeId);
                bool hasArtistRoutes = routeArtworkOverlays.Any(item =>
                    item.Image != null && item.Image.texture != null);
                foreach (Image dash in route.Dashes)
                {
                    if (dash != null)
                    {
                        dash.color = hasArtistRoutes
                            ? new Color(1f, 1f, 1f, 0f)
                            : unlocked ? AvailableRoute : LockedRoute;
                    }
                }
            }

            foreach (MapNodeView node in nodes)
            {
                MapNodeDefinition definition = app.Catalog.Map.FindNode(node.NodeId);
                bool isCurrent = node.NodeId == currentNodeId;
                bool isCompleted = completed.Contains(node.NodeId);
                bool isAvailable = app.Catalog.Map.Nodes.Any(item =>
                    completed.Contains(item.Id) && item.NextNodeIds.Contains(node.NodeId));

                Color color = isCompleted ? CompletedNode :
                    isCurrent ? CurrentNode :
                    isAvailable ? AvailableNode :
                    LockedNode;
                if (node.Marker != null)
                {
                    node.Marker.gameObject.SetActive(true);
                    node.Marker.color = color;
                }

                if (node.Label != null)
                {
                    node.Label.gameObject.SetActive(true);
                    node.Label.color = color;
                    node.Label.text = definition != null
                        ? $"{DisplayName(node.NodeId)}\n{definition.Type} - Dif. {definition.Difficulty}"
                        : DisplayName(node.NodeId);
                }
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
                overlay.Image.color = active
                    ? new Color(1f, 1f, 1f, 0.95f)
                    : new Color(1f, 1f, 1f, 0.25f);
            }
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
    }
}
