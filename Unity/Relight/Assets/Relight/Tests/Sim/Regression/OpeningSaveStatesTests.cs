using NUnit.Framework;
using Relight.Sim.Tests.Combat;
using Relight.Sim.Tests.Support;

namespace Relight.Sim.Tests.Regression
{
    /// <summary>
    /// C-12's per-state save/load stress. The owner's playtest must be able to save ANYWHERE, so every state the
    /// opening encounter can be in — and every half-finished action the engineer can be in the middle of — is
    /// saved, loaded into a fresh <see cref="SimState"/>, and then RUN ON. A state that merely looks right the
    /// instant it is loaded but diverges on the next tick is the defect class this file exists to catch, so the
    /// comparison is always the same three steps:
    ///
    ///   1. the loaded state is the saved state (canonical form, so a mismatch names the field);
    ///   2. both copies run 300 further ticks and still agree, by canonical form AND by hash;
    ///   3. the load re-armed nothing — no second wave, no second introductory encounter.
    ///
    /// (3) is asserted by name as well as by hash because "the load staged a second introductory attack" is exactly
    /// the failure a hash comparison reports as an unreadable offset into a JSON string.
    ///
    /// The map is a copy of <see cref="RaidFixture"/>'s 160×160 (wave-2 W-B's binding note: the director's radii do
    /// not fit on the synthetic 64×48 map), with one authored rubble field added so the mid-mine save is a real dig
    /// rather than a forged counter. RaidFixture itself is another owner's file: it is used, never edited.
    /// </summary>
    public sealed class OpeningSaveStatesTests
    {
        /// <summary>Ticks both copies run on after the load. 300 = 15 s, long enough for a staged group to move.</summary>
        private const int ContinueTicks = 300;

        /// <summary>The authored rubble field, in reach of the spawn and clear of every machine placed below.</summary>
        private const int RubbleX = 84, RubbleY = 83, RubbleW = 3, RubbleH = 3;

        // ------------------------------------------------------------------ the world

        /// <summary>
        /// <see cref="RaidFixture.Map"/> plus a small rubble field. Copied rather than shared because RaidFixture is
        /// wave-2 W-B's file and its map is deliberately featureless.
        /// </summary>
        private static SimContext Ctx()
        {
            const int size = RaidFixture.Size;
            var kind = new byte[size * size];
            for (var i = 0; i < kind.Length; i++) kind[i] = (byte)TileClass.Ground;
            for (var y = RubbleY; y < RubbleY + RubbleH; y++)
                for (var x = RubbleX; x < RubbleX + RubbleW; x++)
                    kind[y * size + x] = (byte)TileClass.Rubble;
            var geometry = new ArrayGeometry(size, size, kind, new bool[size * size], new Vec2(size / 2.0, size / 2.0));
            return RaidFixture.Context(geometry);
        }

        private static Simulation Game(SimContext ctx, int seed = 7) => Simulation.NewGame(ctx, seed);

        private static void Step(Simulation sim, int ticks) => SimTestUtil.Step(sim, ticks);

        private static void Seconds(Simulation sim, double seconds) =>
            Step(sim, (int)System.Math.Round(seconds / Simulation.TickSeconds));

        // ------------------------------------------------------------------ the check itself

        /// <summary>
        /// The whole check, for one saved moment. <paramref name="what"/> names the moment, so a red run says which
        /// of the thirteen states broke without the reader opening this file.
        /// </summary>
        private static SimState SaveLoadRunOn(SimContext ctx, Simulation sim, string what)
        {
            var st = sim.State;
            st.Events.Clear();   // the host drains events every frame; a saved state never carries a frame's events

            var text = SaveSerializer.WriteText(st, ctx.Data, "2026-09-14T00:00:00Z", ctx.MapId);
            var load = SaveSerializer.ReadText(text, ctx.Data, ctx.MapId);
            Assert.That(load.Ok, Is.True, what + ": a save this build just wrote must load — " + load.Reason);
            Assert.That(load.Warning, Is.Null, what + ": the save was downgraded on load — " + load.Warning);

            var saved = SimTestUtil.Canonical(st);
            var read = SimTestUtil.Canonical(load.State);
            Assert.That(read, Is.EqualTo(saved),
                what + ": the loaded state is not the saved state; " + SimTestUtil.FirstDifference(saved, read));

            var majorsAtSave = st.Director.MajorSpawned;
            var groupAtSave = st.Opening.Group;
            var statusAtSave = st.Opening.Status;

            var resumed = Simulation.Wrap(ctx, load.State);
            Step(sim, ContinueTicks);
            Step(resumed, ContinueTicks);

            var a = SimTestUtil.Canonical(st);
            var b = SimTestUtil.Canonical(resumed.State);
            Assert.That(b, Is.EqualTo(a),
                what + ": " + ContinueTicks + " ticks after the load the two runs differ; "
                + SimTestUtil.FirstDifference(a, b));
            Assert.That(resumed.Hash(), Is.EqualTo(sim.Hash()), what + ": the two runs' hashes parted");

            // Nothing was re-armed by the load itself.
            Assert.That(resumed.State.Director.MajorSpawned, Is.EqualTo(majorsAtSave),
                what + ": a major wave was spawned in the " + ContinueTicks + " ticks after the load");
            Assert.That(resumed.State.Opening.Group, Is.EqualTo(st.Opening.Group),
                what + ": the loaded run staged a different introductory group");

            if (statusAtSave == OpeningStatus.Repelled || statusAtSave == OpeningStatus.Lost
                || statusAtSave == OpeningStatus.Skipped)
            {
                Assert.That(resumed.State.Opening.Status, Is.EqualTo(statusAtSave),
                    what + ": a finished encounter came back to life after the load");
                Assert.That(resumed.State.Opening.Group, Is.EqualTo(groupAtSave),
                    what + ": a second encounter was staged after the load");
            }

            return resumed.State;
        }

        // ------------------------------------------------------------------ helpers

        /// <summary>A powered, loaded Home turret — what <see cref="OpeningRules.ReadyTurret"/> waits for.</summary>
        private static Machine ReadyTurret(SimContext ctx, Simulation sim, int dx, int dy)
        {
            var t = RaidFixture.Turret(ctx, sim.State, RaidFixture.CoreX + dx, RaidFixture.CoreY + dy);
            Step(sim, 1);   // one PowerPhase tick, so the circuit exists and the turret reads as ready
            Assert.That(TurretQueries.Ready(ctx, sim.State, t.Id), Is.True, "the fixture turret must start ready");
            return t;
        }

        private static void AssertStatus(Simulation sim, OpeningStatus want)
        {
            Assert.That(sim.State.Opening.Status, Is.EqualTo(want),
                "the fixture did not reach " + want + "; it is " + sim.State.Opening.Status);
        }

        /// <summary>Every body of a group removed at once, the way a turret that killed them all would have left it.</summary>
        private static void KillGroup(SimState st, int group)
        {
            var list = st.Enemies.Actors;
            for (var i = list.Count - 1; i >= 0; i--) if (list[i].Group == group) list.RemoveAt(i);
        }

        /// <summary>A live minor wave, through the coordinator's named seam (wave-2 W-B note).</summary>
        private static void LiveWave(Simulation sim, int count = 3)
        {
            sim.State.Director.DebugAllowed = true;
            RegressionFixture.Accept(sim.Apply(new DebugRaidCommand(count, true)), "a live minor wave");
        }

        // ------------------------------------------------------------------ the opening statuses

        [Test]
        public void Pending_SurvivesASaveAndKeepsRunningIdentically()
        {
            var ctx = Ctx();
            var sim = Game(ctx);
            Step(sim, 5);
            AssertStatus(sim, OpeningStatus.Pending);
            SaveLoadRunOn(ctx, sim, "opening: pending (no turret yet)");
        }

        [Test]
        public void Scheduled_SurvivesASaveAndKeepsRunningIdentically()
        {
            var ctx = Ctx();
            var sim = Game(ctx);
            ReadyTurret(ctx, sim, 6, 0);
            Step(sim, 2);
            AssertStatus(sim, OpeningStatus.Scheduled);
            SaveLoadRunOn(ctx, sim, "opening: scheduled, inside the warning window");
        }

        [Test]
        public void Active_SurvivesASaveAndKeepsRunningIdentically()
        {
            var ctx = Ctx();
            var sim = Game(ctx);
            ReadyTurret(ctx, sim, 6, 0);
            Step(sim, 2);
            Seconds(sim, OpeningRules.Tuning(ctx.Data).WarningS + 0.5);
            AssertStatus(sim, OpeningStatus.Active);
            Assert.That(sim.State.Opening.Group, Is.Not.EqualTo(0), "a staged group has an id");
            SaveLoadRunOn(ctx, sim, "opening: active, bodies on the map");
        }

        [Test]
        public void Deferred_SurvivesASaveAndKeepsRunningIdentically()
        {
            var ctx = Ctx();
            var sim = Game(ctx);
            var st = sim.State;

            // U-D-26: a live wave DEFERS the encounter — the port resumes it, where the reference skipped it.
            // The wave comes FIRST: a ready turret with a quiet director schedules on the very next tick.
            LiveWave(sim);
            Step(sim, 2);
            var turret = RaidFixture.Turret(ctx, st, RaidFixture.CoreX + 6, RaidFixture.CoreY);
            Step(sim, 2);
            Assert.That(TurretQueries.Ready(ctx, st, turret.Id), Is.True, "the encounter has something to wait for");

            AssertStatus(sim, OpeningStatus.Deferred);
            Assert.That(st.Opening.DeferCount, Is.GreaterThan(0));
            Assert.That(st.Opening.DeferNotice, Is.EqualTo(OpeningPhase.DeferReason), "the player is told why it waits");
            SaveLoadRunOn(ctx, sim, "opening: deferred behind a live wave");
        }

        [Test]
        public void Repelled_SurvivesASaveAndKeepsRunningIdentically()
        {
            var ctx = Ctx();
            var sim = Game(ctx);
            var st = sim.State;
            ReadyTurret(ctx, sim, 6, 0);
            Step(sim, 2);
            Seconds(sim, OpeningRules.Tuning(ctx.Data).WarningS + 0.5);
            AssertStatus(sim, OpeningStatus.Active);

            KillGroup(st, st.Opening.Group);
            Step(sim, 2);
            AssertStatus(sim, OpeningStatus.Repelled);
            SaveLoadRunOn(ctx, sim, "opening: repelled, inside the recovery window");
        }

        [Test]
        public void Lost_SurvivesASaveAndKeepsRunningIdentically()
        {
            var ctx = Ctx();
            var sim = Game(ctx);
            var st = sim.State;
            var turret = ReadyTurret(ctx, sim, 6, 0);
            Step(sim, 2);
            Seconds(sim, OpeningRules.Tuning(ctx.Data).WarningS + 0.5);
            AssertStatus(sim, OpeningStatus.Active);

            // The end state is keyed off the PREPARED TURRET, not the core (OpeningPhase's second deliberate
            // difference): a destroyed turret is one that is no longer on the map.
            st.Machines.Remove(turret);
            st.Rev++;
            KillGroup(st, st.Opening.Group);
            Step(sim, 2);
            AssertStatus(sim, OpeningStatus.Lost);
            SaveLoadRunOn(ctx, sim, "opening: lost, the prepared turret gone");
        }

        [Test]
        public void Skipped_SurvivesASaveAndKeepsRunningIdentically()
        {
            var ctx = Ctx();
            var sim = Game(ctx);
            var st = sim.State;

            // U-Q-21's cap: held behind a live wave, and by the time it could run the player already has the three
            // loaded turrets the objective asks for, so the encounter is dropped rather than stockpiled.
            LiveWave(sim);
            Step(sim, 2);
            RaidFixture.Turret(ctx, st, RaidFixture.CoreX + 6, RaidFixture.CoreY);
            RaidFixture.Turret(ctx, st, RaidFixture.CoreX - 8, RaidFixture.CoreY + 6);
            RaidFixture.Turret(ctx, st, RaidFixture.CoreX + 6, RaidFixture.CoreY + 12);
            Step(sim, 3);
            Assert.That(TurretQueries.Loaded(ctx, st), Is.GreaterThanOrEqualTo(3), "the objective is already met");

            AssertStatus(sim, OpeningStatus.Skipped);
            Assert.That(st.Director.Notice, Is.EqualTo(OpeningPhase.CapNotice));
            SaveLoadRunOn(ctx, sim, "opening: skipped by the U-Q-21 cap");
        }

        // ------------------------------------------------------------------ mid-action saves

        [Test]
        public void MidCraft_SurvivesASaveAndKeepsRunningIdentically()
        {
            var ctx = Ctx();
            var sim = Game(ctx);
            var st = sim.State;
            RaidFixture.Add(ctx, st, "depot", RaidFixture.CoreX, RaidFixture.CoreY + 8);   // in reach of the spawn

            RegressionFixture.Accept(sim.Apply(new HandCraftCommand(2)), "queue two workshop batches");
            Step(sim, 40);   // 2 s in
            Assert.That(st.Hand.Jobs.Count, Is.EqualTo(1), "one job is queued at the moment of the save");
            Assert.That(st.Hand.Jobs[0].Batches, Is.EqualTo(2), "with both batches still to run");
            Assert.That(st.Hand.Jobs[0].Progress, Is.GreaterThan(0), "part-way through the first");
            Assert.That(st.Hand.Reserved[ItemId.Steel], Is.GreaterThan(0), "its ingredients held at the workshop");
            Assert.That(HandCraft.HandLocked(st), Is.False, "U-D-44: and the engineer is free to walk away");

            SaveLoadRunOn(ctx, sim, "mid-craft");
        }

        [Test]
        public void MidMine_SurvivesASaveAndKeepsRunningIdentically()
        {
            var ctx = Ctx();
            var sim = Game(ctx);
            var st = sim.State;

            RegressionFixture.Accept(sim.Apply(new MineCommand(RubbleX, RubbleY)), "the hands go on the rubble");
            Step(sim, 5);
            SaveLoadRunOn(ctx, sim, "mid-mine");
        }

        [Test]
        public void MidReload_SurvivesASaveAndKeepsRunningIdentically()
        {
            var ctx = Ctx();
            var sim = Game(ctx);
            var st = sim.State;

            var rifle = RegressionFixture.GiveWeapon(ctx, st);
            RegressionFixture.Accept(sim.Apply(new EquipCommand(rifle, 0)), "equip the rifle");
            RegressionFixture.Give(st, ItemId.Magazine, 30);
            RegressionFixture.Accept(sim.Apply(new ReloadCommand()), "start the reload");

            Step(sim, 5);
            var w = st.Weapons.ActiveWeapon();
            Assert.That(w, Is.Not.Null);
            Assert.That(w.Reload, Is.GreaterThan(0), "the reload is still running at the moment of the save");

            SaveLoadRunOn(ctx, sim, "mid-reload");
        }

        [Test]
        public void MidBeltTransit_SurvivesASaveAndKeepsRunningIdentically()
        {
            var ctx = Ctx();
            var sim = Game(ctx);
            var st = sim.State;

            // Lane positions are the fiddliest thing the save schema carries; a rounding loss there shows up only
            // once both copies run on, which is why the items are part-way along rather than at a belt's end.
            var x = RaidFixture.CoreX + 8;
            var y = RaidFixture.CoreY + 4;
            for (var i = 0; i < 6; i++) RaidFixture.Add(ctx, st, "belt", x + i, y, Dir.E);
            RaidFixture.Add(ctx, st, "chest", x + 6, y);

            var belt = ProductionRules.MachineAt(st, x, y);
            Assert.That(belt, Is.Not.Null, "the belt run starts where it was placed");
            for (var i = 0; i < 4; i++)
            {
                FlowRules.GiveItem(ctx, st, belt, ItemId.Steel);
                Step(sim, 4);
            }
            Assert.That(FlowQueries.HeldCount(st), Is.GreaterThan(0), "items are in transit at the moment of the save");

            SaveLoadRunOn(ctx, sim, "mid-belt-transit");
        }

        [Test]
        public void MidRepair_SurvivesASaveAndKeepsRunningIdentically()
        {
            var ctx = Ctx();
            var sim = Game(ctx);
            var st = sim.State;

            // Stand at the core, damage it, and save with the paid repair part-way through.
            var core = HomeQueries.CoreRect(st);
            st.Engineer.Pos = new Vec2(core.X + core.W / 2.0, core.Y + core.H / 2.0);
            HomeCore.Damage(st, 100);
            RegressionFixture.Give(st, ItemId.Steel, 40);
            RegressionFixture.Give(st, ItemId.Copper, 20);
            RegressionFixture.Accept(sim.Apply(new RepairCommand(RepairKinds.Core, -1)), "start the core repair");

            Step(sim, 20);   // 1 s in
            Assert.That(st.Home.RepairKind, Is.EqualTo(RepairKinds.Core));
            Assert.That(st.Home.RepairRemaining, Is.GreaterThan(0), "the repair is still running at the save");

            SaveLoadRunOn(ctx, sim, "mid-repair");
        }

        [Test]
        public void WithALiveEnemyGroup_SurvivesASaveAndKeepsRunningIdentically()
        {
            var ctx = Ctx();
            var sim = Game(ctx);
            var st = sim.State;

            LiveWave(sim, 4);
            Step(sim, 40);
            Assert.That(st.Enemies.Actors.Count, Is.GreaterThan(0), "bodies are on the map at the moment of the save");
            Assert.That(st.Director.Minor, Is.Not.Null, "and the wave itself is live");

            var after = SaveLoadRunOn(ctx, sim, "a live enemy group");
            Assert.That(after.Enemies.Actors.Count, Is.EqualTo(st.Enemies.Actors.Count),
                "the same bodies, in the same places, and no second wave");
        }
    }
}
