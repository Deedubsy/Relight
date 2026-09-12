using System.Collections;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using Relight.Presentation;
using Relight.Sim;
using UnityEngine;
using UnityEngine.TestTools;

namespace Relight.Tests.Play
{
    /// <summary>
    /// B-04 acceptance (TASKS.md): the sim-timing check in Play Mode — ticks elapse at 20 per wall-clock second,
    /// pause halts ticks, no speed API exists, and per-tick CPU cost is logged separately from the interval.
    /// </summary>
    public sealed class SimHostTests
    {
        private static SimHost MakeHost(out GameObject go)
        {
            go = new GameObject("SimHost (test)");
            var host = go.AddComponent<SimHost>();
            var ctx = new SimContext(ReferenceData.Create(), SyntheticMap.Create());
            host.StartNewGame(ctx, 3);
            return host;
        }

        /// <summary>1,000 ticks in 50 s of wall time. The long form of the acceptance row; skipped unless RELIGHT_LONG_TIMING is set.</summary>
        [UnityTest, Timeout(120000)]
        public IEnumerator TicksFollowWallClock_Long()
        {
            if (System.Environment.GetEnvironmentVariable("RELIGHT_LONG_TIMING") != "1")
            {
                Assert.Ignore("Set RELIGHT_LONG_TIMING=1 to run the 50 s form; the 10 s form runs by default.");
                yield break;
            }
            yield return RunTiming(50.0, 1000, 20);
        }

        /// <summary>200 ticks in 10 s — the same check at a fifth of the length, run on every test pass.</summary>
        [UnityTest, Timeout(60000)]
        public IEnumerator TicksFollowWallClock()
        {
            yield return RunTiming(10.0, 200, 6);
        }

        private static IEnumerator RunTiming(double seconds, int expectedTicks, int tolerance)
        {
            var host = MakeHost(out var go);
            try
            {
                yield return null; // first frame carries the scene-setup delta; let it pass
                var t0 = Time.realtimeSinceStartupAsDouble;
                var ticks0 = host.TotalTicks;
                while (Time.realtimeSinceStartupAsDouble - t0 < seconds) yield return null;
                var ticks = host.TotalTicks - ticks0;
                var wall = Time.realtimeSinceStartupAsDouble - t0;
                Debug.Log($"SimHostTests: {ticks} ticks in {wall:F3} s wall ({host.AcceptedRealSeconds:F3} s accepted); " +
                          $"tick cost avg {host.AverageTickCostMs:F3} ms max {host.MaxTickCostMs:F3} ms");
                var expectedForWall = (long)System.Math.Round(wall * Simulation.TicksPerSecond);
                Assert.That(ticks, Is.InRange(expectedForWall - tolerance, expectedForWall + tolerance),
                    $"expected about {expectedTicks} ticks for {seconds} s");
                Assert.That(host.Simulation.State.Tick, Is.EqualTo(host.TotalTicks));
            }
            finally
            {
                Object.Destroy(go);
            }
        }

        [UnityTest, Timeout(30000)]
        public IEnumerator PauseHaltsTicks_AndUnpauseReleasesNoBacklog()
        {
            var host = MakeHost(out var go);
            try
            {
                yield return new WaitForSecondsRealtime(0.5f);
                Assert.That(host.TotalTicks, Is.GreaterThan(0));
                host.Paused = true;
                yield return null;
                var paused = host.TotalTicks;
                yield return new WaitForSecondsRealtime(1.0f);
                Assert.That(host.TotalTicks, Is.EqualTo(paused), "ticks ran while paused");
                Assert.That(Time.timeScale, Is.EqualTo(1f), "pause must not touch Time.timeScale");
                host.Paused = false;
                yield return null;
                Assert.That(host.LastFrameTicks, Is.LessThanOrEqualTo(2), "unpausing must not fast-forward a backlog");
            }
            finally
            {
                Object.Destroy(go);
            }
        }

        [Test]
        public void NoSpeedApiExists()
        {
            var members = typeof(SimHost).GetMembers(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static)
                .Select(m => m.Name.ToLowerInvariant()).ToArray();
            var offenders = members.Where(n => n.Contains("speed") || n.Contains("timescale") || n.Contains("fastforward") || n.Contains("multiplier")).ToArray();
            Assert.That(offenders, Is.Empty, "SimHost exposes a speed-like member: " + string.Join(", ", offenders));
            var simMembers = typeof(Simulation).GetMembers(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static)
                .Select(m => m.Name.ToLowerInvariant()).Where(n => n.Contains("speed")).ToArray();
            Assert.That(simMembers, Is.Empty, "Simulation exposes a speed-like member: " + string.Join(", ", simMembers));
        }

        [UnityTest, Timeout(30000)]
        public IEnumerator CommandsApplyWhilePaused()
        {
            var host = MakeHost(out var go);
            try
            {
                host.Paused = true;
                yield return null;
                var tick = host.Simulation.State.Tick;
                host.Submit(new NoOpCommand());
                yield return null;
                Assert.That(host.Simulation.PendingCount, Is.EqualTo(0), "queued command was not flushed while paused");
                Assert.That(host.Simulation.State.Tick, Is.EqualTo(tick));
            }
            finally
            {
                Object.Destroy(go);
            }
        }

        private sealed record NoOpCommand : Command;
    }
}
