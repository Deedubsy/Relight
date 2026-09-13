using System.Collections;
using NUnit.Framework;
using Relight.Presentation;
using Relight.Sim;
using UnityEngine;
using UnityEngine.TestTools;

namespace Relight.Tests.Play
{
    /// <summary>
    /// The autosave scheduler belongs to one session (U-M-38, 2026-09-13). A new game or a loaded save attached
    /// behind the live <see cref="SimHost"/> — by <c>WorldBootstrap</c>, a menu, or a test, not only by
    /// <see cref="AutosaveController.Load"/> — must restart the autosave clock and forget the previous session's
    /// failure state. The defect these pin: the controller reset its scheduler only inside its own Load, so a
    /// session started elsewhere inherited the old elapsed interval (an autosave seconds into a new game) or a
    /// stopped ring (no autosave at all until a manual save) — the residue recorded on TASKS.md B-13.
    ///
    /// None of these writes a file: every advance stops one second short of the interval.
    /// </summary>
    public sealed class AutosaveSessionTests
    {
        private static IEnumerator Live(System.Action<SimHost, AutosaveController> body)
        {
            yield return SceneFixture.LoadWorld();
            var host = Object.FindAnyObjectByType<SimHost>();
            var saver = Object.FindAnyObjectByType<AutosaveController>();
            Assert.That(saver, Is.Not.Null, "World.unity has no AutosaveController.");
            Assert.That(saver.Scheduler, Is.Not.Null);
            // A known, timed policy whatever the scene's Inspector says; the scene is reloaded per test.
            saver.ApplySettings(new AutosaveSettings(AutosaveSettings.DefaultIntervalMinutes, AutosaveSettings.DefaultSlots));
            body(host, saver);
            yield return null;
        }

        /// <summary>Most of an interval elapsed, one second short of a write.</summary>
        private static double NearlyDue(AutosaveController saver, SimHost host)
        {
            var s = saver.Scheduler;
            var seconds = s.Settings.IntervalSeconds - 1;
            Assert.That(s.Advance(seconds, host.Simulation), Is.Null, "no autosave may be written by the setup");
            Assert.That(s.SecondsSinceSave, Is.EqualTo(seconds).Within(1e-9));
            return seconds;
        }

        [UnityTest, Timeout(30000)]
        public IEnumerator ANewGameAttachedBehindTheHostRestartsTheAutosaveClock()
        {
            yield return Live((host, saver) =>
            {
                NearlyDue(saver, host);
                host.Attach(Simulation.NewGame(host.Simulation.Context, 2));
                Assert.That(saver.Scheduler.SecondsSinceSave, Is.Zero,
                    "the previous session's elapsed interval carried into the new game; its first autosave would land seconds in");
                Assert.That(saver.LastSave, Is.Null, "a previous session's save result is not this session's");
                Assert.That(saver.LastLoad, Is.Null);
            });
        }

        [UnityTest, Timeout(30000)]
        public IEnumerator ASessionChangeWhileTheControllerIsDisabledIsCaughtWhenItIsEnabledAgain()
        {
            yield return Live((host, saver) =>
            {
                NearlyDue(saver, host);
                saver.enabled = false;
                host.Attach(Simulation.NewGame(host.Simulation.Context, 3));
                Assert.That(saver.Scheduler.SecondsSinceSave, Is.GreaterThan(0), "unsubscribed while disabled, as intended");
                saver.enabled = true;
                Assert.That(saver.Scheduler.SecondsSinceSave, Is.Zero, "the swap that happened while disabled was missed");
            });
        }

        [UnityTest, Timeout(30000)]
        public IEnumerator DetachingResetsTheSchedulerAndTheNextSessionStartsClean()
        {
            yield return Live((host, saver) =>
            {
                var ctx = host.Simulation.Context;
                NearlyDue(saver, host);
                host.Detach();
                Assert.That(saver.Scheduler.SecondsSinceSave, Is.Zero, "a detach is a session change");
                host.Attach(Simulation.NewGame(ctx, 4));     // leave the scene live for the frame that follows
                Assert.That(saver.Scheduler.SecondsSinceSave, Is.Zero);
            });
        }
    }
}
