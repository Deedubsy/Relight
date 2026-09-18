using NUnit.Framework;
using Relight.Sim.Tests.Combat;
using Relight.Sim.UI;

namespace Relight.Sim.Tests.UI
{
    /// <summary>
    /// C-07's view model, held to UI_AND_ONBOARDING.md §4 and §8 without an editor: the three power-strip
    /// sentences (hud.ts:58-60, GP-POWER-FIX), the paused clock, the 150 ms throttle (hud.ts:47), the keyed alert
    /// inbox's three rules, and the single power-alert path (defect U-3).
    /// </summary>
    public sealed class HudViewModelTests
    {
        [Test]
        public void OnlyHandMiningReportsAnItemToBackpack()
        {
            var vm = new HudViewModel();
            vm.Intake(new SimEvent[] { new MinedEvent(0,10,15,ItemId.Steel,false,7) },0);
            vm.Notices.Reap(0);
            Assert.That(vm.Notices.Rows,Is.Empty);
            vm.Intake(new SimEvent[] { new MinedEvent(0,10,15,ItemId.Steel,false) },0);
            vm.Notices.Reap(0);
            Assert.That(vm.Notices.Rows.Count,Is.EqualTo(1));
            Assert.That(vm.Notices.Rows[0].Text,Does.Contain("to Backpack"));
        }

        private static PowerSummary Summary(double demand, double supply, int rated, int circuits) =>
            new PowerSummary(demand, supply, System.Math.Min(demand, supply), rated, rated, circuits, 0);

        // ---- the status strip -------------------------------------------------------------------------------

        [Test]
        public void TheStripPrintsTheNameAndTheTwoNumbersWhenThereIsACircuitWithAGenerator()
        {
            Assert.That(PowerAlertSource.Strip("Founders Court", Summary(40, 300, 1, 1)),
                Is.EqualTo("Founders Court: 40 / 300 kW"));
        }

        [Test]
        public void TheStripSaysNoGeneratorLinkedWhenACircuitHasNoSource()
        {
            Assert.That(PowerAlertSource.Strip("Founders Court", Summary(5, 0, 0, 1)),
                Is.EqualTo("Power: 0 / 0 kW · no Generator linked"));
        }

        [Test]
        public void TheStripSaysDisconnectedWhenNothingIsOnACircuitAtAll()
        {
            Assert.That(PowerAlertSource.Strip("Founders Court", Summary(0, 0, 0, 0)),
                Is.EqualTo("Power: disconnected"));
        }

        [Test]
        public void TheStripFallsBackToThePlainWordWhenThereIsNoPlaceName()
        {
            Assert.That(PowerAlertSource.Strip("", Summary(1, 2, 1, 1)), Is.EqualTo("Power: 1 / 2 kW"));
        }

        // ---- the clock --------------------------------------------------------------------------------------

        [Test]
        public void TheClockCountsDaysAndPrintsPausedOnlyWhenPaused()
        {
            Assert.That(HudViewModel.FormatClock(0, 1200, false), Is.EqualTo("Day 1 · 00:00"));
            Assert.That(HudViewModel.FormatClock(600, 1200, false), Is.EqualTo("Day 1 · 12:00"));
            Assert.That(HudViewModel.FormatClock(1200, 1200, false), Is.EqualTo("Day 2 · 00:00"));
            Assert.That(HudViewModel.FormatClock(600, 1200, true), Is.EqualTo("Day 1 · 12:00 · Paused"));
        }

        // ---- the throttle -----------------------------------------------------------------------------------

        [Test]
        public void RefreshIsThrottledToOneHundredAndFiftyMillisecondsAndForceSkipsIt()
        {
            var vm = new HudViewModel();
            Assert.That(vm.Refresh(null, null, 0, false, false), Is.True, "the first refresh always runs");
            Assert.That(vm.Refreshes, Is.EqualTo(1));

            Assert.That(vm.Refresh(null, null, 0.100, false, false), Is.False, "100 ms is inside the window");
            Assert.That(vm.Refreshes, Is.EqualTo(1));

            Assert.That(vm.Refresh(null, null, 0.149, false, false), Is.False);
            Assert.That(vm.Refresh(null, null, HudViewModel.RefreshSeconds, false, false), Is.True);
            Assert.That(vm.Refreshes, Is.EqualTo(2));

            Assert.That(vm.Refresh(null, null, HudViewModel.RefreshSeconds, false, false, force: true), Is.True,
                "force ignores the window");
            Assert.That(vm.Refreshes, Is.EqualTo(3));
        }

        // ---- the keyed inbox (§8) ---------------------------------------------------------------------------

        [Test]
        public void ARepeatOnTheSameKeyCountsInsteadOfStacking()
        {
            var n = new HudNotices();
            n.Post("craft.done", "Crafted 1 gear", HudNoticeKind.Info, 0);
            n.Post("craft.done", "Crafted 1 gear", HudNoticeKind.Info, 1);
            n.Post("craft.done", "Crafted 1 gear", HudNoticeKind.Info, 2);
            n.Reap(2);

            Assert.That(n.Rows.Count, Is.EqualTo(1));
            Assert.That(n.Rows[0].Repeats, Is.EqualTo(3));
        }

        [Test]
        public void AtMostThreeRowsAreShownAndTheOldestFallsOff()
        {
            var n = new HudNotices();
            n.Post("a", "A", HudNoticeKind.Info, 0);
            n.Post("b", "B", HudNoticeKind.Info, 0);
            n.Post("c", "C", HudNoticeKind.Info, 0);
            n.Post("d", "D", HudNoticeKind.Info, 0);
            n.Reap(0);

            Assert.That(n.Rows.Count, Is.EqualTo(HudNotices.MaxRows));
            Assert.That(n.Rows[0].Key, Is.EqualTo("b"), "the oldest row is the one that goes");
            Assert.That(n.Rows[2].Key, Is.EqualTo("d"));
        }

        [Test]
        public void DismissalNeverClearsALiveDanger()
        {
            var n = new HudNotices();
            n.Post("raid", "Base under attack", HudNoticeKind.Danger, 0);
            Assert.That(n.Dismiss("raid", 1), Is.False, "§8: dismissal never clears a live danger");
            n.Reap(1);
            Assert.That(n.Rows.Count, Is.EqualTo(1));

            // A warning on the same rules is dismissible.
            n.Post("fuel", "Generator out of fuel", HudNoticeKind.Warning, 1);
            Assert.That(n.Dismiss("fuel", 1), Is.True);
            n.Reap(1);
            Assert.That(n.Rows.Count, Is.EqualTo(1));
        }

        [Test]
        public void ATransientRowExpiresOnRealSecondsAndAStandingOneDoesNot()
        {
            var n = new HudNotices();
            n.Post("toast", "Autosaved", HudNoticeKind.Info, 0);
            n.Post("standing", "No power · Home", HudNoticeKind.Warning, 0, double.PositiveInfinity);

            n.Reap(HudNotices.DefaultSeconds - 0.001);
            Assert.That(n.Rows.Count, Is.EqualTo(2));

            n.Reap(HudNotices.DefaultSeconds);
            Assert.That(n.Rows.Count, Is.EqualTo(1));
            Assert.That(n.Rows[0].Key, Is.EqualTo("standing"));

            n.Clear("standing");
            n.Reap(HudNotices.DefaultSeconds);
            Assert.That(n.Rows.Count, Is.EqualTo(0), "a standing row goes when its condition clears");
        }

        // ---- the single power-alert path (defect U-3) --------------------------------------------------------

        [Test]
        public void ThereIsNoOutageAlertAtGameStart()
        {
            var ctx = RaidFixture.Context();
            var st = RaidFixture.State(ctx);
            HomeCore.Ensure(ctx, st);

            var src = new PowerAlertSource();
            Assert.That(src.Refresh(ctx, st), Is.False);
            Assert.That(src.AlertText, Is.EqualTo(""));
            Assert.That(PowerAlertSource.EverHadPower(ctx, st), Is.False);
        }

        [Test]
        public void AnUnfuelledGeneratorWithALoadRaisesTheStripAndNeverAToast()
        {
            var ctx = RaidFixture.Context();
            var st = RaidFixture.State(ctx);
            HomeCore.Ensure(ctx, st);
            RaidFixture.Add(ctx, st, "lamp", 20, 20);
            RaidFixture.Add(ctx, st, "pole", 22, 20);
            RaidFixture.Add(ctx, st, "generator", 24, 20);   // placed, never fuelled

            var src = new PowerAlertSource { PlaceName = "Founders Court" };
            Assert.That(PowerAlertSource.EverHadPower(ctx, st), Is.True, "a source on the map is the derived fact");
            Assert.That(src.Refresh(ctx, st), Is.True);
            Assert.That(src.AlertText, Is.EqualTo("No power · Founders Court"));

            var vm = new HudViewModel();
            vm.Power.PlaceName = "Founders Court";
            vm.Refresh(ctx, st, 0, false, false, force: true);
            Assert.That(vm.Alert, Is.EqualTo("No power · Founders Court"));

            // U-D-55: the outage is a standing state, so it belongs to the urgent strip alone. Holding it for a
            // while is what used to churn a toast beside the strip — the inbox row was re-posted every refresh
            // and counted a repeat each time — so the outage is held here across many refreshes on purpose.
            for (var tick = 0; tick < 40; tick++) vm.Refresh(ctx, st, tick * 0.15, false, false, force: true);
            Assert.That(vm.Alert, Is.EqualTo("No power · Founders Court"), "the strip still says it");
            for (var i = 0; i < vm.Notices.Rows.Count; i++)
                Assert.That(vm.Notices.Rows[i].Key, Is.Not.EqualTo(PowerAlertSource.AlertKey),
                    "defect U-3 / U-D-55: one producer, one place — the strip, never the notice inbox");

            // Fix the outage and the strip goes with it.
            st.Machines[2].Inv.Add(ItemId.Coal, 20);
            vm.Refresh(ctx, st, 7, false, false, force: true);
            Assert.That(vm.Alert, Is.EqualTo(""));
            for (var i = 0; i < vm.Notices.Rows.Count; i++)
                Assert.That(vm.Notices.Rows[i].Key, Is.Not.EqualTo(PowerAlertSource.AlertKey));
        }

        // ---- the problem vocabulary -------------------------------------------------------------------------

        [Test]
        public void EveryActionableStateHasARemedyAndTheHealthyOnesHaveNone()
        {
            Assert.That(HudViewModel.Remedy(MachineOperatingState.OutOfFuel), Is.EqualTo("put coal in the generator"));
            Assert.That(HudViewModel.Remedy(MachineOperatingState.Unpowered), Is.EqualTo("connect it to a powered pole"));
            Assert.That(HudViewModel.Remedy(MachineOperatingState.OutputFull), Is.EqualTo("empty the storage"));
            Assert.That(HudViewModel.Remedy(MachineOperatingState.NoInput), Is.EqualTo("bring it materials"));
            Assert.That(HudViewModel.Remedy(MachineOperatingState.Disabled), Is.EqualTo("walk to it and repair it"));
            Assert.That(HudViewModel.Remedy(MachineOperatingState.Running), Is.Null);
        }

        [Test]
        public void AProblemLineReusesProductionQueriesWording()
        {
            var ctx = RaidFixture.Context();
            var st = RaidFixture.State(ctx);
            HomeCore.Ensure(ctx, st);
            // A processor, not a lamp: ProductionQueries only reasons about machines that process, so an unpowered
            // decoration is "idle", not a problem — the HUD inherits that vocabulary rather than inventing one.
            RaidFixture.Add(ctx, st, "assembler", 20, 20);

            var vm = new HudViewModel();
            vm.Refresh(ctx, st, 0, false, false, force: true);

            Assert.That(vm.Problems.Count, Is.GreaterThan(0));
            Assert.That(vm.Problems[0].State, Is.EqualTo(MachineOperatingState.Unpowered));
            var p = vm.Problems[0];
            Assert.That(p.Text, Does.Contain(ProductionQueries.StateText(p.State)),
                "the reason word is ProductionQueries' own, never a second vocabulary");
            Assert.That(p.Text, Does.Contain(HudViewModel.Remedy(p.State)));
        }

        [Test]
        public void ProblemsStopAtThree()
        {
            var ctx = RaidFixture.Context();
            var st = RaidFixture.State(ctx);
            HomeCore.Ensure(ctx, st);
            for (var i = 0; i < 8; i++) RaidFixture.Add(ctx, st, "assembler", 20 + i * 4, 20);

            var vm = new HudViewModel();
            vm.Refresh(ctx, st, 0, false, false, force: true);
            Assert.That(vm.Problems.Count, Is.EqualTo(HudViewModel.MaxProblems));
        }

        // ---- the rest of the strip ---------------------------------------------------------------------------

        [Test]
        public void AFreshSessionFillsTheStripFromTheSelectors()
        {
            var ctx = RaidFixture.Context();
            var st = RaidFixture.State(ctx);
            HomeCore.Ensure(ctx, st);

            var vm = new HudViewModel();
            Assert.That(vm.Refresh(ctx, st, 0, paused: true, menuOpen: false), Is.True);

            Assert.That(vm.Clock, Is.EqualTo("Day 1 · 00:00 · Paused"));
            Assert.That(vm.PowerText, Is.EqualTo(PowerAlertSource.DisconnectedText));
            Assert.That(vm.Core, Does.StartWith("Home core · "));
            Assert.That(vm.CoreFraction, Is.EqualTo(1).Within(1e-9));
            Assert.That(vm.CoreDisabled, Is.False);
            Assert.That(vm.Engineer, Does.StartWith("Engineer · "));
            Assert.That(vm.Backpack, Does.StartWith("Backpack "));
            Assert.That(vm.MiningVisible, Is.False);
            Assert.That(vm.HandLock, Is.EqualTo(""));
        }

        // ---- C-13: the sim's answer to a queued command reaches the player ------------------------------------

        [Test]
        public void ARefusedCommandBecomesOneWarningNoticeInTheSimsOwnWords()
        {
            var vm = new HudViewModel();
            vm.Intake(new SimEvent[]
            {
                new CommandResultEvent(0, "PlaceMachineCommand", false, "Walk closer to place the Supply chest"),
            }, 0);
            vm.Notices.Reap(0);

            Assert.That(vm.Notices.Rows.Count, Is.EqualTo(1));
            Assert.That(vm.Notices.Rows[0].Text, Is.EqualTo("Walk closer to place the Supply chest"));
            Assert.That(vm.Notices.Rows[0].Kind, Is.EqualTo(HudNoticeKind.Warning));
            Assert.That(vm.Notices.Rows[0].Key, Is.EqualTo(HudViewModel.CommandNoticeKey + "PlaceMachineCommand"));
            Assert.That(vm.Notices.Rows[0].Repeats, Is.EqualTo(1));
        }

        /// <summary>
        /// The button is held and the sim refuses once a frame. HudNotices.Post keys by command type, so the
        /// second refusal updates the one row and counts it (Repeats 1 the first time, then 2) rather than
        /// stacking two identical toasts - UI_AND_ONBOARDING.md 8.
        /// </summary>
        [Test]
        public void TwoIdenticalRefusalsUpdateOneRowWithACount()
        {
            var vm = new HudViewModel();
            var e = new CommandResultEvent(0, "PlaceMachineCommand", false, "not enough in the Backpack (10 Steel plates)");
            vm.Intake(new SimEvent[] { e }, 0);
            vm.Intake(new SimEvent[] { e }, 0.1);
            vm.Notices.Reap(0.1);

            Assert.That(vm.Notices.Rows.Count, Is.EqualTo(1), "one row, not two");
            Assert.That(vm.Notices.Rows[0].Repeats, Is.EqualTo(2));
        }

        [Test]
        public void AnAcceptedNoteIsInfoAndAnEmptyOneIsNotShownAtAll()
        {
            var vm = new HudViewModel();
            vm.Intake(new SimEvent[]
            {
                new CommandResultEvent(0, "MoveStackCommand", true, "Stack moved."),
                new CommandResultEvent(0, "WalkCommand", true, ""),
            }, 0);
            vm.Notices.Reap(0);

            Assert.That(vm.Notices.Rows.Count, Is.EqualTo(1), "an accepted command with nothing to say posts nothing");
            Assert.That(vm.Notices.Rows[0].Text, Is.EqualTo("Stack moved."));
            Assert.That(vm.Notices.Rows[0].Kind, Is.EqualTo(HudNoticeKind.Info));
        }

        [Test]
        public void ARefusedDigIsNotSilent()
        {
            var vm = new HudViewModel();
            vm.Intake(new SimEvent[]
            {
                new MiningStoppedEvent(0, 4, 4, "the Backpack is full"),
                new MiningStoppedEvent(0, 5, 5, ""),
            }, 0);
            vm.Notices.Reap(0);

            Assert.That(vm.Notices.Rows.Count, Is.EqualTo(1));
            Assert.That(vm.Notices.Rows[0].Key, Is.EqualTo(HudViewModel.MiningNoticeKey));
            Assert.That(vm.Notices.Rows[0].Text, Is.EqualTo("the Backpack is full"));
            Assert.That(vm.Notices.Rows[0].Kind, Is.EqualTo(HudNoticeKind.Warning));
        }

        [Test]
        public void IntakeToleratesNoEventsAtAll()
        {
            var vm = new HudViewModel();
            Assert.DoesNotThrow(() => vm.Intake(null, 0));
            vm.Notices.Reap(0);
            Assert.That(vm.Notices.Rows.Count, Is.EqualTo(0));
        }

        [Test]
        public void ANullSessionSaysSoInsteadOfPrintingAFalseClock()
        {
            var vm = new HudViewModel();
            vm.Refresh(null, null, 0, false, false, force: true);
            Assert.That(vm.Clock, Is.EqualTo("No session"));
            Assert.That(vm.PowerText, Is.EqualTo(PowerAlertSource.NoSourceText));
            Assert.That(vm.Alert, Is.EqualTo(""));
        }
    }
}
