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

        /// <summary>
        /// GP-W6. The row printed the item's key ("+1 iron-ore to Backpack") and, posted once a unit, had the inbox
        /// count repeats onto it ("… × 14"). It names the item and keeps the total itself.
        /// </summary>
        [Test]
        public void TheMinedRowNamesTheItemAndAddsUpInsteadOfRepeating()
        {
            var ctx = RaidFixture.Context();
            var st = RaidFixture.State(ctx);
            HomeCore.Ensure(ctx, st);
            var vm = new HudViewModel();
            vm.Refresh(ctx, st, 0, false, false, force: true);

            for (var i = 0; i < 3; i++)
                vm.Intake(new SimEvent[] { new MinedEvent(0, 10, 15, ItemId.IronOre, false) }, 0.5 * i);
            vm.Notices.Reap(1);

            var name = ctx.Data.Item(ItemId.IronOre).DisplayName;
            Assert.That(vm.Notices.Rows.Count, Is.EqualTo(1));
            Assert.That(vm.Notices.Rows[0].Text, Is.EqualTo("+3 " + name + " to Backpack"));
            Assert.That(vm.Notices.Rows[0].Text, Does.Not.Contain(Items.Key(ItemId.IronOre)), "the key is not player text");
            Assert.That(vm.Notices.Rows[0].Repeats, Is.LessThanOrEqualTo(1), "the total is in the words, not in a × N");

            // A fresh dig after the row has lapsed starts again at one.
            vm.Intake(new SimEvent[] { new MinedEvent(0, 10, 15, ItemId.IronOre, false) }, 60);
            vm.Notices.Reap(60);
            Assert.That(vm.Notices.Rows[0].Text, Is.EqualTo("+1 " + name + " to Backpack"));
        }

        private static PowerSummary Summary(double demand, double supply, int rated, int circuits) =>
            new PowerSummary(demand, supply, System.Math.Min(demand, supply), rated, rated, circuits, 0);

        // ---- the status strip -------------------------------------------------------------------------------

        // GP-W6: these four used to pin "Founders Court: 40 / 300 kW" and two sentences like it — text that had
        // not been on screen since 2026-09-18, when the HUD began printing a formula of its own over the top.
        // The wording below is the one the player reads, and PowerAlertSource is again the only place it is made.

        [Test]
        public void TheStripPrintsGenerationThenNeed()
        {
            Assert.That(PowerAlertSource.Strip(Summary(40, 300, 1, 1)), Is.EqualTo("Power 300 kW · Need 40"));
        }

        [Test]
        public void TheStripMarksAShortageForAsLongAsDemandIsAboveGeneration()
        {
            Assert.That(PowerAlertSource.Strip(Summary(400, 300, 1, 1)), Is.EqualTo("Power 300 kW · Need 400 · short"));
            Assert.That(PowerAlertSource.IsShort(Summary(400, 300, 1, 1)), Is.True);
            Assert.That(PowerAlertSource.IsShort(Summary(300, 300, 1, 1)), Is.False, "meeting demand exactly is not a shortage");
            Assert.That(PowerAlertSource.IsShort(Summary(400, 0, 1, 1)), Is.False, "no generation at all is an outage, not a shortage");
        }

        [Test]
        public void TheStripSaysNoGeneratorLinkedWhenACircuitHasNoSource()
        {
            Assert.That(PowerAlertSource.Strip(Summary(5, 0, 0, 1)), Is.EqualTo(PowerAlertSource.NoSourceText));
        }

        [Test]
        public void TheStripSaysNoFuelWhenEveryLinkedGeneratorIsEmpty()
        {
            Assert.That(PowerAlertSource.Strip(Summary(5, 0, 1, 1)), Is.EqualTo(PowerAlertSource.NoFuelText));
        }

        [Test]
        public void TheStripSaysDisconnectedWhenNothingIsOnACircuitAtAll()
        {
            Assert.That(PowerAlertSource.Strip(Summary(0, 0, 0, 0)), Is.EqualTo(PowerAlertSource.DisconnectedText));
        }

        // ---- the clock --------------------------------------------------------------------------------------

        [Test]
        public void TheClockCountsElapsedTimeAndPrintsPausedOnlyWhenPaused()
        {
            Assert.That(HudViewModel.FormatClock(0, false), Is.EqualTo("0:00:00"));
            Assert.That(HudViewModel.FormatClock(600, false), Is.EqualTo("0:10:00"));
            Assert.That(HudViewModel.FormatClock(5050.9, false), Is.EqualTo("1:24:10"));
            Assert.That(HudViewModel.FormatClock(600, true), Is.EqualTo("0:10:00 · Paused"));
            Assert.That(HudViewModel.FormatClock(-4, false), Is.EqualTo("0:00:00"));
        }

        [Test]
        public void TheStripSaysWhetherTheEngineerStandsInLight()
        {
            var ctx = RaidFixture.Context();
            var st = RaidFixture.State(ctx);
            HomeCore.Ensure(ctx, st);
            LightPhase.Ensure(ctx, st);
            var vm = new HudViewModel();

            st.Engineer.Pos = new Vec2(RaidFixture.CoreX + 0.5, RaidFixture.CoreY + 0.5);   // on the always-lit core lot
            vm.Refresh(ctx, st, 0, paused: false, menuOpen: false, force: true);
            Assert.That(vm.InDark, Is.False);
            Assert.That(vm.LightText, Is.EqualTo("In light"));

            st.Engineer.Pos = new Vec2(10.5, 10.5);                                         // far from any light
            vm.Refresh(ctx, st, 1, paused: false, menuOpen: false, force: true);
            Assert.That(vm.InDark, Is.True);
            Assert.That(vm.LightText, Is.EqualTo("In the dark"));
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
        public void AKindChangeOnUnchangedTextIsNotARepeat()
        {
            // REL-6 (INT-02): re-grading a row is not saying it twice; saying it twice still is.
            var n = new HudNotices();
            n.Post("defence:Home", "Home: 1 turret dry", HudNoticeKind.Warning, 0, double.PositiveInfinity);
            n.Post("defence:Home", "Home: 1 turret dry", HudNoticeKind.Danger, 1, double.PositiveInfinity);
            Assert.That(n.Rows[0].Repeats, Is.EqualTo(1), "warning to danger");
            Assert.That(n.Rows[0].Kind, Is.EqualTo(HudNoticeKind.Danger));
            n.Post("defence:Home", "Home: 1 turret dry", HudNoticeKind.Warning, 2, double.PositiveInfinity);
            Assert.That(n.Rows[0].Repeats, Is.EqualTo(1), "and back");
            Assert.That(n.Rows[0].Kind, Is.EqualTo(HudNoticeKind.Warning));

            n.Post("defence:Home", "Home: 1 turret dry", HudNoticeKind.Warning, 3, double.PositiveInfinity);
            Assert.That(n.Rows[0].Repeats, Is.EqualTo(2), "the same sentence at the same grade is a repeat");
            n.Post("defence:Home", "Home: 1 turret dry", HudNoticeKind.Danger, 4, double.PositiveInfinity);
            Assert.That(n.Rows[0].Repeats, Is.EqualTo(2), "a re-grade keeps the count it had");
            n.Post("defence:Home", "Home: 2 turrets dry", HudNoticeKind.Danger, 5, double.PositiveInfinity);
            Assert.That(n.Rows[0].Repeats, Is.EqualTo(1), "a new sentence starts again");
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

        /// <summary>
        /// REL-86 (UI-02a) acceptance: "a sim or UI test raises five problems and sees three rows plus "+2 more". The
        /// count drops as problems clear." The drawn line and the two window sizes are
        /// <c>Tests/Play/Ui/HudOverflowPlayTests.cs</c>.
        /// </summary>
        [Test]
        public void FiveStandingAlertsShowThreeRowsAndPlusTwoMore_AndTheCountDropsAsTheyClear()
        {
            var n = new HudNotices();
            var inf = double.PositiveInfinity;
            n.Post("fuel", "Low fuel", HudNoticeKind.Warning, 0, inf);
            n.Post("cargo", "Dropped cargo", HudNoticeKind.Warning, 1, inf);
            n.Post("defence:a", "Founders Court: 1 turret dry", HudNoticeKind.Danger, 2, inf);
            n.Post("brownout", "Low power", HudNoticeKind.Warning, 3, inf);
            n.Post("defence:b", "Ironworks: 1 wreck", HudNoticeKind.Warning, 4, inf);
            n.Reap(5);

            Assert.That(n.Rows.Count, Is.EqualTo(HudNotices.MaxRows));
            Assert.That(n.Hidden, Is.EqualTo(2));
            Assert.That(n.MoreText, Is.EqualTo("+2 more"));
            Assert.That(n.Rows[0].Key, Is.EqualTo("defence:a"), "the existing severity order: danger first");
            Assert.That(n.Rows[1].Key, Is.EqualTo("brownout"), "then the warnings, in the order they were posted");
            Assert.That(n.Rows[2].Key, Is.EqualTo("defence:b"));

            n.Clear("brownout");
            Assert.That(n.MoreText, Is.EqualTo("+2 more"), "until the next reap two rows are shown and two are not");
            n.Reap(6);
            Assert.That(n.Rows.Count, Is.EqualTo(3));
            Assert.That(n.MoreText, Is.EqualTo("+1 more"), "a hidden row takes the freed place and the count drops");
            Assert.That(n.Rows[1].Key, Is.EqualTo("cargo"), "the newest hidden warning comes back first");

            n.Clear("defence:a");
            n.Reap(7);
            Assert.That(n.Rows.Count, Is.EqualTo(3));
            Assert.That(n.Hidden, Is.EqualTo(0));
            Assert.That(n.MoreText, Is.EqualTo(""), "everything live is shown, so there is no overflow line");

            n.Clear("fuel");
            n.Reap(8);
            Assert.That(n.Rows.Count, Is.EqualTo(2));
            Assert.That(n.MoreText, Is.EqualTo(""));
        }

        [Test]
        public void TransientRowsThatDidNotFitAreCountedUntilTheyExpire()
        {
            var n = new HudNotices();
            for (var i = 0; i < 4; i++) n.Post("toast" + i, "Toast " + i, HudNoticeKind.Info, i * 0.5, 6);
            n.Reap(2);
            Assert.That(n.Rows.Count, Is.EqualTo(3));
            Assert.That(n.MoreText, Is.EqualTo("+1 more"), "the oldest toast is out of sight but still live");
            n.Reap(6.2);
            Assert.That(n.Rows.Count, Is.EqualTo(3), "toast0 expired at 6 s; the other three are shown");
            Assert.That(n.MoreText, Is.EqualTo(""), "an expired row is gone, not hidden");
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

        /// <summary>
        /// REL-53 (UI-03): one machine at 0 hit points is never described with two different words in two rows,
        /// and the two counts never disagree without saying why. A wrecked turret and a wrecked wall, with a dry
        /// turret beside them to raise the defence row: the problem rows name the KIND, the defence row names the
        /// PLACE and says it is counting structures, and both call 0 hit points "wrecked".
        /// </summary>
        [Test]
        public void AWreckedTurretAndAWreckedWallReadTheSameWayInBothRows()
        {
            var ctx = RaidFixture.Context();
            var st = RaidFixture.State(ctx);
            HomeCore.Ensure(ctx, st);
            var dry = RaidFixture.Turret(ctx, st, 40, 40, 0);
            st.Turrets.Of(dry.Id).ShotT = 1;                      // it has fired, so its empty hopper counts
            var gun = RaidFixture.Turret(ctx, st, 44, 40, 50);
            var wall = RaidFixture.Add(ctx, st, "wall", 48, 40);
            RaidFixture.Run(ctx, st, 2, new System.Collections.Generic.List<ITickPhase> { new PowerPhase() });
            TurretRules.Damage(ctx, st, gun, TurretRules.MaxHp(ctx.Data, gun) + 1);
            TurretRules.Damage(ctx, st, wall, TurretRules.MaxHp(ctx.Data, wall) + 1);

            var vm = new HudViewModel();
            vm.Refresh(ctx, st, 0, false, false, force: true);

            var wrecked = 0;
            for (var i = 0; i < vm.Problems.Count; i++)
            {
                Assert.That(vm.Problems[i].Text, Does.Not.Contain("disabled"),
                    "the old second vocabulary is gone from every row, not only the wreck ones");
                if (vm.Problems[i].State != MachineOperatingState.Disabled) continue;
                wrecked += vm.Problems[i].Count;
                Assert.That(vm.Problems[i].Text, Does.Contain("wrecked (0 hp)"));
                Assert.That(vm.Problems[i].Text, Does.Contain("walk to it and repair it"));
            }
            Assert.That(wrecked, Is.EqualTo(2), "one turret and one wall, each on its own row because they differ in kind");
            Assert.That(vm.Problems[0].State, Is.EqualTo(MachineOperatingState.Disabled), "a wreck ranks first");

            var defence = new DefenceAlertSource();
            Assert.That(defence.Refresh(ctx, st), Is.True);
            Assert.That(defence.Rows.Count, Is.EqualTo(1), "one row per place, however many machines are down");
            Assert.That(defence.Rows[0].Wrecks, Is.EqualTo(wrecked), "the same two machines, counted the same way");
            Assert.That(defence.Rows[0].Text, Is.EqualTo("Home: 1 turret dry, 2 structures wrecked"));
            Assert.That(defence.Rows[0].Text, Does.Contain("wrecked"), "the same word as the problem rows");
            Assert.That(defence.Rows[0].Text, Does.Not.Contain("turrets wrecked"),
                "it counts every structure at the place, and says so, so the wall is not read as a turret");
        }

        [Test]
        public void ProblemsStopAtThree()
        {
            var ctx = RaidFixture.Context();
            var st = RaidFixture.State(ctx);
            HomeCore.Ensure(ctx, st);
            // Four kinds, all unpowered: four rows' worth since GP-W6 put machines of one kind on one row.
            var kinds = new[] { "assembler", "foundry", "refinery", "turret" };
            for (var k = 0; k < kinds.Length; k++)
                for (var i = 0; i < 2; i++) RaidFixture.Add(ctx, st, kinds[k], 20 + i * 4, 20 + k * 4);

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

            Assert.That(vm.Clock, Is.EqualTo("0:00:00 · Paused"));
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

        // ---- L-02: the light's guide lines and the brownout notice (ALWAYS_DARK_SPEC §5.4, §5.6, §5.7) -----------

        private static int RowsWith(HudViewModel vm, string key)
        {
            var n = 0;
            for (var i = 0; i < vm.Notices.Rows.Count; i++) if (vm.Notices.Rows[i].Key == key) n++;
            return n;
        }

        private static HudNotice Row(HudViewModel vm, string key)
        {
            for (var i = 0; i < vm.Notices.Rows.Count; i++) if (vm.Notices.Rows[i].Key == key) return vm.Notices.Rows[i];
            return null;
        }

        [Test]
        public void TheFirstAttackersToBaulkAtLightTeachTheRuleOnceAndACampPatrolNever()
        {
            var vm = new HudViewModel();
            vm.Intake(new SimEvent[] { new LightHesitationEvent(1, 7, EnemyLayer.Site, 0, 30, 30) }, 1);
            vm.ReapNotices(1);
            Assert.That(RowsWith(vm, HudViewModel.HesitationKey), Is.Zero, "a camp resident on its beat is not an attack");

            vm.Intake(new SimEvent[] { new LightHesitationEvent(2, 8, EnemyLayer.Minor, 1, 50, 50) }, 2);
            vm.ReapNotices(2);
            Assert.That(Row(vm, HudViewModel.HesitationKey).Text, Is.EqualTo(HudViewModel.HesitationLine));
            Assert.That(Row(vm, HudViewModel.HesitationKey).Kind, Is.EqualTo(HudNoticeKind.Info));

            vm.ReapNotices(2 + HudViewModel.GuideSeconds + 1);
            Assert.That(RowsWith(vm, HudViewModel.HesitationKey), Is.Zero, "it goes once it has been read");
            vm.Intake(new SimEvent[] { new LightHesitationEvent(40, 9, EnemyLayer.Minor, 2, 50, 50) }, 40);
            vm.ReapNotices(40);
            Assert.That(RowsWith(vm, HudViewModel.HesitationKey), Is.Zero, "and is said once, not once per raid");
        }

        [Test]
        public void TheFirstBlindTurretSaysTheSpecsLineOnce()
        {
            var vm = new HudViewModel();
            vm.Intake(new SimEvent[] { new TurretBlindEvent(1, 5, 41, 41) }, 1);
            vm.ReapNotices(1);
            Assert.That(Row(vm, HudViewModel.BlindTurretKey).Text,
                Is.EqualTo("This turret can't see into the dark. Light the ground it guards."));
            Assert.That(Row(vm, HudViewModel.BlindTurretKey).Kind, Is.EqualTo(HudNoticeKind.Warning));

            vm.ReapNotices(1 + HudViewModel.GuideSeconds + 1);
            vm.Intake(new SimEvent[] { new TurretBlindEvent(30, 6, 60, 41) }, 30);
            vm.ReapNotices(30);
            Assert.That(RowsWith(vm, HudViewModel.BlindTurretKey), Is.Zero, "the badge carries it from then on");
        }

        // REL-118: a once-only line is not spent until the player has had it in front of them. Before this, a
        // raid's own Danger rows pushed the lesson into "+1 more" within a second or two and it never came back.

        private static void FillWithDanger(HudViewModel vm, double now)
        {
            for (var i = 0; i < HudNotices.MaxRows; i++)
                vm.Notices.Post("raid:" + i, "raiders", HudNoticeKind.Danger, now, double.PositiveInfinity);
        }

        private static void RaidOver(HudViewModel vm)
        {
            for (var i = 0; i < HudNotices.MaxRows; i++) vm.Notices.Clear("raid:" + i);
        }

        [Test]
        public void ARaidDelaysTheLightLessonInsteadOfSwallowingIt()
        {
            var vm = new HudViewModel();
            vm.Intake(new SimEvent[] { new LightHesitationEvent(2, 8, EnemyLayer.Minor, 1, 50, 50) }, 2);
            vm.ReapNotices(2);
            Assert.That(Row(vm, HudViewModel.HesitationKey), Is.Not.Null, "it is said the moment they baulk");

            FillWithDanger(vm, 3);
            vm.ReapNotices(3);
            Assert.That(Row(vm, HudViewModel.HesitationKey), Is.Null, "danger still outranks a lesson (U-D-55)");
            Assert.That(vm.Notices.Hidden, Is.EqualTo(1), "it waits in the overflow");

            var t = 3 + HudViewModel.GuideSeconds * 5;
            vm.ReapNotices(t);
            RaidOver(vm);
            vm.ReapNotices(t);
            Assert.That(Row(vm, HudViewModel.HesitationKey).Text, Is.EqualTo(HudViewModel.HesitationLine),
                "the raid over, the lesson comes back");

            vm.ReapNotices(t + HudViewModel.GuideSeconds - 1);
            Assert.That(Row(vm, HudViewModel.HesitationKey), Is.Not.Null, "and stays for its whole time on screen");
            vm.ReapNotices(t + HudViewModel.GuideSeconds + 1);
            Assert.That(Row(vm, HudViewModel.HesitationKey), Is.Null, "then it is spent");

            vm.Intake(new SimEvent[] { new LightHesitationEvent(200, 9, EnemyLayer.Minor, 2, 50, 50) }, 200);
            vm.ReapNotices(200);
            Assert.That(RowsWith(vm, HudViewModel.HesitationKey), Is.Zero, "and it is still said only once");
        }

        [Test]
        public void TheBlindTurretLineWaitsForItsPlaceToo()
        {
            var vm = new HudViewModel();
            FillWithDanger(vm, 1);
            vm.Intake(new SimEvent[] { new TurretBlindEvent(1, 5, 41, 41) }, 1);
            vm.ReapNotices(1);
            Assert.That(Row(vm, HudViewModel.BlindTurretKey), Is.Null, "danger fills the column");

            var t = 1 + HudViewModel.GuideSeconds * 3;
            vm.ReapNotices(t);
            RaidOver(vm);
            vm.ReapNotices(t);
            Assert.That(Row(vm, HudViewModel.BlindTurretKey).Text, Is.EqualTo(HudViewModel.BlindTurretLine),
                "the line is read after the danger, not lost to it");

            vm.ReapNotices(t + HudViewModel.GuideSeconds + 1);
            Assert.That(RowsWith(vm, HudViewModel.BlindTurretKey), Is.Zero, "and is spent once it has been read");
        }

        [Test]
        public void ADistrictComingOnIsNamedWithItsStreetlights()
        {
            var vm = new HudViewModel();
            vm.Intake(new SimEvent[] { new DistrictLitEvent(1, "sub:2", "Foundry Row substation", 2, 61, 29) }, 1);
            vm.Notices.Reap(1);
            Assert.That(Row(vm, HudViewModel.DistrictLitKey).Text,
                Is.EqualTo("Foundry Row substation connected · 2 streetlights on"));

            vm.Intake(new SimEvent[] { new EnteredLightEvent(2, 10, 10) }, 2);
            vm.Notices.Reap(2);
            Assert.That(vm.Notices.Rows.Count, Is.EqualTo(1), "stepping into light is a sound, never a HUD line");
        }

        [Test]
        public void ABrownoutOnALampOrATurretIsPostedOnceAndClearedWhenItEnds()
        {
            var ctx = RaidFixture.Context();
            var st = RaidFixture.State(ctx);
            var phases = new System.Collections.Generic.List<ITickPhase> { new PowerPhase() };
            RaidFixture.Add(ctx, st, "lamp", 30, 40);
            RaidFixture.Power(ctx, st, 32, 40);
            var vm = new HudViewModel();
            RaidFixture.Run(ctx, st, 5, phases);
            vm.Refresh(ctx, st, 0, false, false, force: true);
            Assert.That(RowsWith(vm, PowerAlertSource.BrownoutKey), Is.Zero, "a healthy circuit says nothing");

            var load = new System.Collections.Generic.List<Machine>();
            foreach (var (x, y) in new[] { (28, 43), (32, 43), (36, 43), (28, 47), (32, 47), (36, 47) })
                load.Add(RaidFixture.Add(ctx, st, "assembler", x, y));
            RaidFixture.Run(ctx, st, 5, phases);
            for (var i = 1; i <= 20; i++) vm.Refresh(ctx, st, i * 0.15, false, false, force: true);
            vm.Notices.Reap(3);

            var row = Row(vm, PowerAlertSource.BrownoutKey);
            Assert.That(row, Is.Not.Null);
            Assert.That(row.Text, Does.Contain("lights").And.Contain("turrets"), "§5.7: the notice names both consequences");
            Assert.That(row.Kind, Is.EqualTo(HudNoticeKind.Warning));
            Assert.That(row.Repeats, Is.EqualTo(1), "U-D-55: a standing state is posted on its edge, never per refresh");
            Assert.That(vm.Alert, Is.EqualTo(""), "a brownout is not an outage");

            foreach (var m in load) st.Machines.Remove(m);
            st.Rev++;
            RaidFixture.Run(ctx, st, 5, phases);
            vm.Refresh(ctx, st, 4, false, false, force: true);
            vm.Notices.Reap(4);
            Assert.That(RowsWith(vm, PowerAlertSource.BrownoutKey), Is.Zero, "and it goes when the power is back");
        }

        [Test]
        public void ADryTurretIsOneStandingRowThatTurnsToDangerInARaidAndClearsOnAReload()
        {
            // E-17 (U-D-61). The row is a standing state: posted when what it says changes, never per refresh.
            var ctx = RaidFixture.Context();
            var st = RaidFixture.State(ctx);
            var phases = new System.Collections.Generic.List<ITickPhase> { new PowerPhase() };
            var a = RaidFixture.Turret(ctx, st, 40, 40, 0);
            var b = RaidFixture.Turret(ctx, st, 50, 40, 0);
            st.Turrets.Of(a.Id).ShotT = 1;                           // both have fired: they ran dry, they were not
            st.Turrets.Of(b.Id).ShotT = 1;                           // simply never loaded
            RaidFixture.Run(ctx, st, 2, phases);

            var vm = new HudViewModel();
            for (var i = 0; i <= 20; i++) vm.Refresh(ctx, st, i * 0.15, false, false, force: true);
            var key = DefenceAlertSource.KeyPrefix + "Home";
            var row = Row(vm, key);
            Assert.That(row, Is.Not.Null);
            Assert.That(row.Text, Is.EqualTo("Home: 2 turrets dry"), "one row for the place, never one per gun");
            Assert.That(row.Kind, Is.EqualTo(HudNoticeKind.Warning), "no raid: a chore");
            Assert.That(row.Repeats, Is.EqualTo(1), "U-D-55: never re-posted per refresh");
            Assert.That(RowsWith(vm, key), Is.EqualTo(1));

            st.Director.Minor = new MinorRaid { StartsAt = st.T + 30 };
            vm.Refresh(ctx, st, 4, false, false, force: true);
            Assert.That(Row(vm, key).Kind, Is.EqualTo(HudNoticeKind.Danger), "dry with a raid warned is danger");

            a.Rounds = 50;
            vm.Refresh(ctx, st, 5, false, false, force: true);
            Assert.That(Row(vm, key).Text, Is.EqualTo("Home: 1 turret dry"), "the same row, updated in place");

            b.Rounds = 50;
            vm.Refresh(ctx, st, 6, false, false, force: true);
            vm.Notices.Reap(6);
            Assert.That(RowsWith(vm, key), Is.Zero, "reloaded: the row clears itself");
        }

        [Test]
        public void ADryTurretRowStaysAtOneRepeatAsARaidIsWarnedAndEnds()
        {
            // REL-6 (INT-02). The row is posted when it turns to danger and when it turns back, on the same text,
            // and used to read "Home: 1 turret dry × 2", then "× 3" (seen 2026-09-21).
            var ctx = RaidFixture.Context();
            var st = RaidFixture.State(ctx);
            var phases = new System.Collections.Generic.List<ITickPhase> { new PowerPhase() };
            var t = RaidFixture.Turret(ctx, st, 40, 40, 0);
            st.Turrets.Of(t.Id).ShotT = 1;                           // it has fired: it ran dry
            RaidFixture.Run(ctx, st, 2, phases);
            var key = DefenceAlertSource.KeyPrefix + "Home";
            var vm = new HudViewModel();
            var now = 0.0;
            void Look(HudNoticeKind kind, string when)
            {
                for (var i = 0; i < 5; i++, now += 0.15) vm.Refresh(ctx, st, now, false, false, force: true);
                var row = Row(vm, key);
                Assert.That(row, Is.Not.Null, when);
                Assert.That(row.Text, Is.EqualTo("Home: 1 turret dry"), when);
                Assert.That(row.Kind, Is.EqualTo(kind), when);
                Assert.That(row.Repeats, Is.EqualTo(1), when + ": never \"× 2\"");
                Assert.That(RowsWith(vm, key), Is.EqualTo(1), when);
            }

            Look(HudNoticeKind.Warning, "dry turret");
            st.Director.Minor = new MinorRaid { StartsAt = st.T + 30 };
            Look(HudNoticeKind.Danger, "raid warned");
            st.Director.Minor = null;
            Look(HudNoticeKind.Warning, "raid over");
            st.Director.Minor = new MinorRaid { StartsAt = st.T + 30 };
            Look(HudNoticeKind.Danger, "the next raid warned");
        }

        [Test]
        public void ABrownoutThatOnlySlowsAFactoryIsNotTheDefenceNotice()
        {
            var ctx = RaidFixture.Context();
            var st = RaidFixture.State(ctx);
            RaidFixture.Power(ctx, st, 32, 40);
            foreach (var (x, y) in new[] { (28, 43), (32, 43), (36, 43), (28, 47) })
                RaidFixture.Add(ctx, st, "assembler", x, y);
            RaidFixture.Run(ctx, st, 5, new System.Collections.Generic.List<ITickPhase> { new PowerPhase() });

            var src = new PowerAlertSource();
            src.Refresh(ctx, st);
            Assert.That(src.BrownoutText, Is.EqualTo(""), "the machines' own 'running slowly' line owns that case");
        }

        // ---- REL-60 (UI-10): the worst events raise a row -------------------------------------------------------

        /// <summary>A state with the Home core placed on the fixture's core site and an empty event list.</summary>
        private static (SimContext ctx, SimState st) WithCore()
        {
            var ctx = RaidFixture.Context();
            var st = RaidFixture.State(ctx);
            HomeCore.Ensure(ctx, st);
            st.Events.Clear();
            return (ctx, st);
        }

        [Test]
        public void ACoreHitIsADangerRow_AHundredHitsAreOneRow_AndItFallsBackToDamagedWhenTheHitsStop()
        {
            var (ctx, st) = WithCore();
            var vm = new HudViewModel();
            vm.Refresh(ctx, st, 0, false, false, force: true);
            Assert.That(Row(vm, HudViewModel.CoreNoticeKey), Is.Null, "a core at full health has no row");

            HomeCore.Damage(st, 10);
            vm.Intake(st.Events, 1);
            var row = Row(vm, HudViewModel.CoreNoticeKey);
            Assert.That(row, Is.Not.Null, "the CoreDamagedEvent raised the row the frame it happened");
            Assert.That(row.Text, Is.EqualTo(HudViewModel.CoreUnderAttackText));
            Assert.That(row.Kind, Is.EqualTo(HudNoticeKind.Danger), "§8: base damage outranks everything but the engineer");

            for (var i = 0; i < 100; i++)
            {
                st.Events.Clear();
                HomeCore.Damage(st, 1);
                st.T += 0.02;
                vm.Intake(st.Events, 1 + i * 0.02);
                vm.Refresh(ctx, st, 1 + i * 0.02, false, false, force: true);
            }
            Assert.That(RowsWith(vm, HudViewModel.CoreNoticeKey), Is.EqualTo(1));
            Assert.That(Row(vm, HudViewModel.CoreNoticeKey).Repeats, Is.EqualTo(1), "a hit is never counted as a repeat");

            st.T += HudViewModel.CoreHitSeconds + 0.1;
            vm.Refresh(ctx, st, 5, false, false, force: true);
            row = Row(vm, HudViewModel.CoreNoticeKey);
            Assert.That(row.Text, Is.EqualTo(HudViewModel.CoreDamagedText), "no hit for 5 s: damaged, not under attack");
            Assert.That(row.Kind, Is.EqualTo(HudNoticeKind.Warning));

            st.Home.Hp = ctx.Data.Defence.CoreHp;   // the repair's result; the repair itself is HomeCoreTests'
            vm.Refresh(ctx, st, 6, false, false, force: true);
            Assert.That(Row(vm, HudViewModel.CoreNoticeKey), Is.Null, "a whole core clears its row");
        }

        [Test]
        public void ACoreKnockedOutRaisesTheDisabledRow_WhichSaysHowToRecommission()
        {
            var (ctx, st) = WithCore();
            var vm = new HudViewModel();
            HomeCore.Damage(st, 1000);
            Assert.That(RaidFixture.Count<CoreDisabledEvent>(st), Is.EqualTo(1));

            vm.Intake(st.Events, 1);
            var row = Row(vm, HudViewModel.CoreNoticeKey);
            Assert.That(row.Text, Is.EqualTo(HudViewModel.CoreDisabledText));
            Assert.That(row.Text, Does.Contain(OpeningQueries.RecommissionButton), "the workshop button's own name");
            Assert.That(row.Kind, Is.EqualTo(HudNoticeKind.Danger));

            vm.Refresh(ctx, st, 1, false, false, force: true);
            Assert.That(RowsWith(vm, HudViewModel.CoreNoticeKey), Is.EqualTo(1), "Refresh agrees with the event: one row");
            Assert.That(Row(vm, HudViewModel.CoreNoticeKey).Repeats, Is.EqualTo(1));
        }

        [Test]
        public void APatchRepairTheCoreFellDuringShowsTheSimsSentenceOnce()
        {
            var (ctx, st) = WithCore();
            var (hx, hy, hw, hh) = HomeQueries.CoreRect(st);
            st.Engineer.Pos = new Vec2(hx + hw / 2.0, hy + hh / 2.0);
            st.Engineer.Inv[ItemId.Steel] = 10;
            st.Engineer.Inv[ItemId.Copper] = 10;
            st.Ledger = Ledger.Open(st, ctx.Data);
            HomeCore.Damage(st, 100);
            Assert.That(new HomeCoreHandler().TryApply(ctx, st, new RepairCommand(RepairKinds.Core, -1), out var r)
                        && r.Accepted, Is.True, "the patch repair starts");

            HomeCore.Damage(st, 1000);   // knocked out while the patch is under way
            var phase = new HomeCorePhase();
            for (var i = 0; i < 20 * 60 && RaidFixture.Count<CoreRepairAbortedEvent>(st) == 0; i++)
            {
                phase.Tick(ctx, st, RaidFixture.Dt);
                st.Tick++;
                st.T += RaidFixture.Dt;
            }
            var aborted = RaidFixture.Last<CoreRepairAbortedEvent>(st);
            Assert.That(aborted, Is.Not.Null, "the sim voided the patch");

            var vm = new HudViewModel();
            vm.Intake(st.Events, 1);
            vm.Refresh(ctx, st, 1, false, false, force: true);
            var row = Row(vm, HudViewModel.RepairAbortedKey);
            Assert.That(row, Is.Not.Null, "the event's text used to reach no one");
            Assert.That(row.Text, Is.EqualTo(aborted.Text));
            Assert.That(row.Text, Is.EqualTo("Core knocked out during repair; a full recovery kit is required."));
            Assert.That(row.Kind, Is.EqualTo(HudNoticeKind.Warning));
            Assert.That(row.Repeats, Is.EqualTo(1));
            Assert.That(Row(vm, HudViewModel.CoreNoticeKey).Text, Is.EqualTo(HudViewModel.CoreDisabledText));

            vm.Notices.Reap(1 + HudViewModel.GuideSeconds + 0.1);
            Assert.That(Row(vm, HudViewModel.RepairAbortedKey), Is.Null, "a moment, not a state: it goes");
            Assert.That(Row(vm, HudViewModel.CoreNoticeKey), Is.Not.Null, "the disabled core's row stands");
        }

        [Test]
        public void AnEngineerDownIsADangerRowUntilTheyAreUp_ThenANoteSaysSo()
        {
            var (ctx, st) = WithCore();
            var vm = new HudViewModel();
            st.Engineer.TakeDamage(ctx, st, ctx.Data.Engineer.MaxHp);
            Assert.That(RaidFixture.Count<EngineerDownEvent>(st), Is.EqualTo(1));

            vm.Intake(st.Events, 1);
            var down = Row(vm, HudViewModel.EngineerDownKey);
            Assert.That(down, Is.Not.Null, "the EngineerDownEvent raised the row");
            Assert.That(down.Text, Is.EqualTo(HudViewModel.EngineerDownText));
            Assert.That(down.Kind, Is.EqualTo(HudNoticeKind.Danger));
            vm.Refresh(ctx, st, 1, false, false, force: true);
            Assert.That(RowsWith(vm, HudViewModel.EngineerDownKey), Is.EqualTo(1));
            Assert.That(Row(vm, HudViewModel.EngineerDownKey).Repeats, Is.EqualTo(1));

            st.Events.Clear();
            var phase = new EngineerMovementPhase();
            for (var i = 0; i < 20 * 60 && st.Engineer.IsDown; i++)
            {
                phase.Tick(ctx, st, RaidFixture.Dt);
                st.Tick++;
                st.T += RaidFixture.Dt;
            }
            Assert.That(RaidFixture.Count<EngineerUpEvent>(st), Is.EqualTo(1), "the sim stood them back up");

            vm.Intake(st.Events, 2);
            vm.Refresh(ctx, st, 2, false, false, force: true);
            Assert.That(Row(vm, HudViewModel.EngineerDownKey), Is.Null, "up: the danger row is gone");
            var up = Row(vm, HudViewModel.EngineerUpKey);
            Assert.That(up, Is.Not.Null, "and the EngineerUpEvent raised its own row, which the clear did not wipe");
            Assert.That(up.Text, Is.EqualTo(HudViewModel.EngineerUpText));
            Assert.That(up.Kind, Is.EqualTo(HudNoticeKind.Info));

            vm.Notices.Reap(2 + HudViewModel.NoteSeconds + 0.1);
            Assert.That(Row(vm, HudViewModel.EngineerUpKey), Is.Null, "the note is brief");
        }

        [Test]
        public void ALoadedGameWithTheCoreAndEngineerDownSaysSoWithoutAnyEvent_AndAnOldHitIsNotAnAttack()
        {
            var (ctx, st) = WithCore();
            st.T = 200;
            st.Home.Hp = 0;
            st.Home.DisabledAt = 150;
            st.Engineer.Hp = 0;
            st.Engineer.Down = st.T + 5;

            var vm = new HudViewModel();
            vm.Notices.Post("rel60:warning", "some warning", HudNoticeKind.Warning, 0, double.PositiveInfinity);
            vm.Refresh(ctx, st, 1, false, false, force: true);
            Assert.That(Row(vm, HudViewModel.CoreNoticeKey).Text, Is.EqualTo(HudViewModel.CoreDisabledText));
            Assert.That(Row(vm, HudViewModel.EngineerDownKey), Is.Not.Null);
            Assert.That(vm.Notices.Rows[0].Kind, Is.EqualTo(HudNoticeKind.Danger), "danger ranks above the warning");
            Assert.That(vm.Notices.Rows[1].Kind, Is.EqualTo(HudNoticeKind.Danger));
            Assert.That(vm.Notices.Rows[2].Key, Is.EqualTo("rel60:warning"));

            // A hit heard in the session before a load back to an earlier save is not "under attack" now.
            var seen = new HudViewModel();
            seen.Intake(new System.Collections.Generic.List<SimEvent> { new CoreDamagedEvent(500, 250, 10) }, 1);
            st.Home.Hp = 250;
            st.Engineer.Down = -1;
            st.Engineer.Hp = ctx.Data.Engineer.MaxHp;
            seen.Refresh(ctx, st, 2, false, false, force: true);
            Assert.That(Row(seen, HudViewModel.CoreNoticeKey).Text, Is.EqualTo(HudViewModel.CoreDamagedText));
            Assert.That(Row(seen, HudViewModel.CoreNoticeKey).Kind, Is.EqualTo(HudNoticeKind.Warning));
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
