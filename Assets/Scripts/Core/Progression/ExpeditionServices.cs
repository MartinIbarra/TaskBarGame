using System;
using System.Collections.Generic;
using System.Linq;
using TaskbarTactics.Core.Models;

namespace TaskbarTactics.Core.Progression
{
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
