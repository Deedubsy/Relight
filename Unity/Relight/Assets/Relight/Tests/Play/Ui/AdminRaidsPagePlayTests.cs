using System.Collections;
using NUnit.Framework;
using Relight.Presentation;
using Relight.Sim;
using Relight.UI;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UIElements;

namespace Relight.Tests.Play.Ui
{
    /// <summary>
    /// E-19 part 1 (REL-83): the Admin panel's Raids page on the real World scene. The rules the buttons obey are
    /// pinned offline in <c>Tests/Sim/Admin/AdminDirectorTests.cs</c>; this is the screen half of the acceptance:
    /// the page shows director state that MATCHES the sim, and its button books a real large raid at once.
    /// </summary>
    public sealed class AdminRaidsPagePlayTests
    {
        private const string Panel = "admin-panel";

        [UnityTest, Timeout(60000)]
        public IEnumerator ThePageShowsTheSimsOwnReadout_AndWarnNowBooksALargeRaid()
        {
            yield return SceneFixture.LoadWorld();
            var host = Object.FindAnyObjectByType<SimHost>();
            var shell = Object.FindAnyObjectByType<UiShell>();
            var admin = Object.FindAnyObjectByType<AdminPanelController>();
            Assert.That(admin, Is.Not.Null, "GameUI.unity has no AdminPanelController.");
            Assert.That(AdminPanelController.Available, Is.True, "Play Mode tests run in the editor, where Admin is offered.");
            var sim = host.Simulation;
            var st = sim.State;

            yield return null;   // the controller binds on its first Update
            if (!string.IsNullOrEmpty(shell.Active)) shell.CloseActive();
            admin.Toggle();
            yield return null;
            Assert.That(shell.Active, Is.EqualTo(Panel), "the Admin panel did not open.");

            UiFixture.DocumentWith("admin-tab-raids", out var tab);
            UiFixture.DocumentWith("admin-page-raids", out var page);
            UiFixture.DocumentWith("admin-director-state", out var stateElement);
            UiFixture.DocumentWith("admin-major-now", out var warnNow);
            UiFixture.DocumentWith("admin-status", out var statusElement);
            Assert.That(tab, Is.Not.Null, "no Raids tab.");
            Assert.That(page, Is.Not.Null, "no Raids page.");
            var state = stateElement as Label;
            var status = statusElement as Label;
            Assert.That(state, Is.Not.Null, "no director-state label.");
            Assert.That(warnNow, Is.Not.Null, "no Warn-now button.");

            // The page must be read against a clock that is not moving: the text carries whole seconds.
            host.Paused = true;
            yield return null;
            UiFixture.Submit(tab);
            yield return new WaitForSecondsRealtime(0.4f);   // the panel repaints every 0.15 s, paused or not
            Assert.That(UiFixture.Shown(page), Is.True, "the Raids tab did not show its page.");
            Assert.That(state.text, Is.EqualTo(DirectorQueries.Describe(DirectorQueries.Readout(sim.Context, st))),
                "the page does not say what the sim says.");
            Assert.That(state.text, Does.Contain("Phase:").And.Contain("of " + sim.Context.Data.Raids.ActiveRaidBudget)
                .And.Contain("of " + sim.Context.Data.Raids.LivingBudget), "the phase and both body limits are on the page.");

            // A new game may have the opening holding the approach. That refusal is the director's own rule and the
            // offline suite pins it; here the hold is lifted so the button's ordinary path can be watched.
            if (st.Director.Reserved) Director.Release(st);
            Assert.That(st.Director.Major, Is.Null, "a new game has no large raid booked yet.");

            UiFixture.Submit(warnNow);
            yield return null;
            Assert.That(status.ClassListContains("admin-error"), Is.False, "Warn now was refused: " + status.text);
            Assert.That(status.text, Does.Contain("will be warned on the next tick"));

            // "Within one tick": let the sim run and the director book it through its own Schedule.
            host.Paused = false;
            var deadline = Time.realtimeSinceStartup + 2f;
            while (st.Director.Major == null && Time.realtimeSinceStartup < deadline) yield return null;
            host.Paused = true;
            Assert.That(st.Director.Major, Is.Not.Null, "no large raid was booked after Warn now.");
            Assert.That(st.Director.Major.Committed, Is.False, "it is WARNED, with the normal warning still to run.");
            Assert.That(st.Director.Major.StartsAt - st.T, Is.GreaterThan(sim.Context.Data.Raids.WarningS - 3),
                "the full warning, not a side door.");

            yield return new WaitForSecondsRealtime(0.4f);
            Assert.That(state.text, Is.EqualTo(DirectorQueries.Describe(DirectorQueries.Readout(sim.Context, st))),
                "after the booking the page still says what the sim says.");
            Assert.That(state.text, Does.Contain("Phase: warned"));

            host.Paused = false;
            shell.CloseActive();
        }
    }
}
