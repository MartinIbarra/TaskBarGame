using System.Collections.Generic;
using NUnit.Framework;
using TaskbarTactics.Core.Combat;
using TaskbarTactics.Core.Models;
using TaskbarTactics.Presentation;

namespace TaskbarTactics.Tests
{
    public sealed class CombatPlaybackTimelineTests
    {
        [Test]
        public void EventsAtTheSameTimestampShareOnePlaybackFrame()
        {
            List<CombatEvent> events = new List<CombatEvent>
            {
                EventAt(1000, "late"),
                EventAt(500, "first"),
                EventAt(500, "simultaneous")
            };

            CombatPlaybackSchedule schedule = CombatPlaybackTimeline.Build(
                events,
                2000,
                10f,
                2f);

            Assert.That(schedule.Frames, Has.Count.EqualTo(2));
            Assert.That(schedule.Frames[0].SimulationTimeMilliseconds, Is.EqualTo(500));
            Assert.That(schedule.Frames[0].DelaySeconds, Is.EqualTo(0.25f).Within(0.001f));
            Assert.That(schedule.Frames[0].Events, Has.Count.EqualTo(2));
            Assert.That(schedule.Frames[1].DelaySeconds, Is.EqualTo(0.25f).Within(0.001f));
            Assert.That(schedule.TailDelaySeconds, Is.EqualTo(0.5f).Within(0.001f));
            Assert.That(schedule.TotalDurationSeconds, Is.EqualTo(1f).Within(0.001f));
        }

        [Test]
        public void LongTimelineIsProportionallyCompressedToMaximumDuration()
        {
            List<CombatEvent> events = new List<CombatEvent>
            {
                EventAt(5000, "middle"),
                EventAt(10000, "last")
            };

            CombatPlaybackSchedule schedule = CombatPlaybackTimeline.Build(
                events,
                10000,
                2f,
                1f);

            Assert.That(schedule.Frames[0].DelaySeconds, Is.EqualTo(1f).Within(0.001f));
            Assert.That(schedule.Frames[1].DelaySeconds, Is.EqualTo(1f).Within(0.001f));
            Assert.That(schedule.TotalDurationSeconds, Is.EqualTo(2f).Within(0.001f));
        }

        [Test]
        public void EmptyTimelineStillHonorsElapsedCombatTime()
        {
            CombatPlaybackSchedule schedule = CombatPlaybackTimeline.Build(
                new List<CombatEvent>(),
                3000,
                10f,
                1.5f);

            Assert.That(schedule.Frames, Is.Empty);
            Assert.That(schedule.TailDelaySeconds, Is.EqualTo(2f).Within(0.001f));
            Assert.That(schedule.TotalDurationSeconds, Is.EqualTo(2f).Within(0.001f));
        }

        private static CombatEvent EventAt(int milliseconds, string actorId)
        {
            return new CombatEvent
            {
                TimeMilliseconds = milliseconds,
                ActorSide = CombatSide.Hero,
                ActorId = actorId,
                TargetId = "target",
                Amount = 1
            };
        }
    }
}
