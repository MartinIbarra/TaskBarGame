using System;
using System.Collections.Generic;
using System.Linq;
using TaskbarTactics.Core.Combat;

namespace TaskbarTactics.Presentation
{
    public sealed class CombatPlaybackFrame
    {
        public int SimulationTimeMilliseconds { get; }
        public float DelaySeconds { get; }
        public IReadOnlyList<CombatEvent> Events { get; }

        public CombatPlaybackFrame(
            int simulationTimeMilliseconds,
            float delaySeconds,
            IReadOnlyList<CombatEvent> events)
        {
            SimulationTimeMilliseconds = simulationTimeMilliseconds;
            DelaySeconds = Math.Max(0f, delaySeconds);
            Events = events ?? Array.Empty<CombatEvent>();
        }
    }

    public sealed class CombatPlaybackSchedule
    {
        public IReadOnlyList<CombatPlaybackFrame> Frames { get; }
        public float TailDelaySeconds { get; }
        public float TotalDurationSeconds { get; }

        public CombatPlaybackSchedule(
            IReadOnlyList<CombatPlaybackFrame> frames,
            float tailDelaySeconds,
            float totalDurationSeconds)
        {
            Frames = frames ?? Array.Empty<CombatPlaybackFrame>();
            TailDelaySeconds = Math.Max(0f, tailDelaySeconds);
            TotalDurationSeconds = Math.Max(0f, totalDurationSeconds);
        }
    }

    public static class CombatPlaybackTimeline
    {
        private const float MinimumPlaybackSpeed = 0.1f;

        public static CombatPlaybackSchedule Build(
            IEnumerable<CombatEvent> events,
            int elapsedCombatMilliseconds,
            float maximumDurationSeconds,
            float playbackSpeed)
        {
            List<IGrouping<int, CombatEvent>> eventGroups =
                (events ?? Array.Empty<CombatEvent>())
                .Where(item => item != null)
                .OrderBy(item => Math.Max(0, item.TimeMilliseconds))
                .GroupBy(item => Math.Max(0, item.TimeMilliseconds))
                .ToList();
            int lastEventMilliseconds = eventGroups.Count > 0
                ? eventGroups[eventGroups.Count - 1].Key
                : 0;
            int totalSimulationMilliseconds = Math.Max(
                Math.Max(0, elapsedCombatMilliseconds),
                lastEventMilliseconds);
            float safePlaybackSpeed = Math.Max(MinimumPlaybackSpeed, playbackSpeed);
            float naturalDurationSeconds =
                totalSimulationMilliseconds / 1000f / safePlaybackSpeed;
            float safeMaximumDuration = Math.Max(0f, maximumDurationSeconds);
            float compression = safeMaximumDuration > 0f &&
                                naturalDurationSeconds > safeMaximumDuration
                ? safeMaximumDuration / naturalDurationSeconds
                : 1f;

            List<CombatPlaybackFrame> frames = new List<CombatPlaybackFrame>();
            int previousMilliseconds = 0;
            foreach (IGrouping<int, CombatEvent> group in eventGroups)
            {
                int deltaMilliseconds = group.Key - previousMilliseconds;
                frames.Add(new CombatPlaybackFrame(
                    group.Key,
                    PlaybackSeconds(deltaMilliseconds, safePlaybackSpeed, compression),
                    group.ToList()));
                previousMilliseconds = group.Key;
            }

            float tailDelay = PlaybackSeconds(
                totalSimulationMilliseconds - previousMilliseconds,
                safePlaybackSpeed,
                compression);
            return new CombatPlaybackSchedule(
                frames,
                tailDelay,
                naturalDurationSeconds * compression);
        }

        private static float PlaybackSeconds(
            int simulationMilliseconds,
            float playbackSpeed,
            float compression)
        {
            return Math.Max(0, simulationMilliseconds) /
                   1000f /
                   playbackSpeed *
                   compression;
        }
    }
}
