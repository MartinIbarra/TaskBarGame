using System;
using System.Collections.Generic;
using System.Linq;
using TaskbarTactics.Core.Models;

namespace TaskbarTactics.Core.Progression
{
    public static class ExpeditionPath
    {
        public static readonly string[] ActOneNodeIds =
        {
            "town",
            "narrow_bridge",
            "cave",
            "cemetery",
            "goblin_village",
            "tomb_pass",
            "mountain_pass",
            "lost_forest",
            "last_bastion"
        };

        public static readonly string[] ActTwoNodeIds =
        {
            "city2",
            "corrupt_pass",
            "lo_hueso",
            "mt_secret",
            "ancient_ruins",
            "arbol_morto",
            "mountain_pass_act2",
            "black_tower",
            "port",
            "lost_bay"
        };

        public static bool TryGetNextNodeId(string nodeId, out string nextNodeId)
        {
            nextNodeId = NextNodeId(ActOneNodeIds, nodeId);
            if (!string.IsNullOrEmpty(nextNodeId))
            {
                return true;
            }

            nextNodeId = NextNodeId(ActTwoNodeIds, nodeId);
            return !string.IsNullOrEmpty(nextNodeId);
        }

        public static bool IsProgressionEdge(string fromNodeId, string toNodeId)
        {
            return IsAdjacent(ActOneNodeIds, fromNodeId, toNodeId) ||
                   IsAdjacent(ActTwoNodeIds, fromNodeId, toNodeId);
        }

        public static bool IsNodeInAct(string nodeId, int actNumber)
        {
            string[] path = actNumber == 2 ? ActTwoNodeIds : ActOneNodeIds;
            return Array.IndexOf(path, nodeId) >= 0;
        }

        private static string NextNodeId(IReadOnlyList<string> path, string nodeId)
        {
            int index = IndexOf(path, nodeId);
            return index >= 0 && index + 1 < path.Count ? path[index + 1] : string.Empty;
        }

        private static bool IsAdjacent(IReadOnlyList<string> path, string fromNodeId, string toNodeId)
        {
            int fromIndex = IndexOf(path, fromNodeId);
            return fromIndex >= 0 && fromIndex + 1 < path.Count && path[fromIndex + 1] == toNodeId;
        }

        private static int IndexOf(IReadOnlyList<string> path, string nodeId)
        {
            for (int i = 0; i < path.Count; i++)
            {
                if (path[i] == nodeId)
                {
                    return i;
                }
            }

            return -1;
        }
    }

    public interface IRouteSelector
    {
        MapNodeState SelectNext(IReadOnlyList<MapNodeState> choices, RoutePreference preference);
    }

    public sealed class RouteSelector : IRouteSelector
    {
        public MapNodeState SelectNext(
            IReadOnlyList<MapNodeState> choices,
            RoutePreference preference)
        {
            if (choices == null || choices.Count == 0)
            {
                throw new ArgumentException("At least one route choice is required.", nameof(choices));
            }

            switch (preference)
            {
                case RoutePreference.Loot:
                    return choices
                        .OrderByDescending(item => item.Type == MapNodeType.Treasure)
                        .ThenBy(item => item.Difficulty)
                        .ThenBy(item => item.Id)
                        .First();
                case RoutePreference.Challenge:
                    return choices
                        .OrderByDescending(item => item.Type == MapNodeType.Elite ||
                                                   item.Type == MapNodeType.Boss)
                        .ThenByDescending(item => item.Difficulty)
                        .ThenBy(item => item.Id)
                        .First();
                default:
                    return choices
                        .OrderBy(item => item.Difficulty)
                        .ThenBy(item => item.Id)
                        .First();
            }
        }
    }

    public sealed class OfflineProgressResult
    {
        public TimeSpan SimulatedDuration;
        public int ResolvedNodes;
        public bool ReachedLimit;
    }

    public interface IOfflineProgressService
    {
        OfflineProgressResult Calculate(DateTime lastSaveUtc, DateTime nowUtc, int secondsPerNode);
    }

    public sealed class OfflineProgressService : IOfflineProgressService
    {
        private readonly TimeSpan maximumDuration;

        public OfflineProgressService(TimeSpan maximumDuration)
        {
            this.maximumDuration = maximumDuration;
        }

        public OfflineProgressResult Calculate(
            DateTime lastSaveUtc,
            DateTime nowUtc,
            int secondsPerNode)
        {
            if (secondsPerNode <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(secondsPerNode));
            }

            TimeSpan elapsed = nowUtc - lastSaveUtc;
            if (elapsed < TimeSpan.Zero)
            {
                elapsed = TimeSpan.Zero;
            }

            bool reachedLimit = elapsed > maximumDuration;
            TimeSpan simulated = reachedLimit ? maximumDuration : elapsed;
            return new OfflineProgressResult
            {
                SimulatedDuration = simulated,
                ResolvedNodes = (int)(simulated.TotalSeconds / secondsPerNode),
                ReachedLimit = reachedLimit
            };
        }
    }

    public static class ExpeditionResolver
    {
        public static void ResolveDefeat(GameState state)
        {
            state.Expedition.IsActive = false;
            state.Expedition.CurrentNodeId = string.Empty;
            state.Party.IsFormationLocked = false;
        }
    }
}
