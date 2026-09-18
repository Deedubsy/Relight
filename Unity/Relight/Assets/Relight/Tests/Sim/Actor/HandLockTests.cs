using System.Collections.Generic;
using NUnit.Framework;

namespace Relight.Sim.Tests
{
    /// <summary>
    /// The stationary-action lock, after U-D-44 (2026-09-15) narrowed what sets it.
    ///
    /// It used to be "a workshop batch is running". It is now "a Home repair is running" and nothing else:
    /// <see cref="HandCraft.HandLocked"/> delegates to <see cref="Home.RepairLocked"/>, because a queued batch is
    /// the workshop working, not the engineer. So this file holds two halves. The first is the point of U-D-44 —
    /// a queued batch leaves the engineer completely free, and the workshop finishes it anyway. The second is that
    /// the lock is still really enforced by everything that reads the predicate: the mover, a walk-here, the dodge
    /// and a machine transfer all refuse with the repair's own words.
    ///
    /// These go through real commands and real ticks, never through the predicate alone: the defect the file was
    /// written for was a predicate that existed and that nothing read. A repair's own lifecycle (its price, its
    /// seconds, its refund) belongs to the Home core tests; here it is engaged directly, as the one bit of state
    /// that makes the predicate true.
    /// </summary>
    public sealed class HandLockTests
    {
        private const double Eps = 1e-9;

        /// <summary>Movement and hand-craft phases in composition order (world before inventory).</summary>
        private static void Step(SimContext ctx, SimState st, int ticks)
        {
            var phases = new List<ITickPhase> { new EngineerMovementPhase(), new HandCraftPhase() };
            for (var i = 0; i < ticks; i++)
            {
                for (var p = 0; p < phases.Count; p++) phases[p].Tick(ctx, st, Fixture.Dt);
                st.Tick++;
                st.T += Fixture.Dt;
            }
        }

        /// <summary>Movement and hand-craft handlers in composition order.</summary>
        private static CommandResult Send(SimContext ctx, SimState st, Command c)
        {
            var handlers = new List<ICommandHandler> { new MovementCommandHandler(), new HandCraftHandler() };
            for (var i = 0; i < handlers.Count; i++)
                if (handlers[i].TryApply(ctx, st, c, out var r)) return r;
            return CommandResult.Refuse("no handler");
        }

        /// <summary>A state at the spawn with the workshop in reach and one batch queued and processing.</summary>
        private static (SimContext, SimState) Working()
        {
            var ctx = Fixture.Context();
            var st = Fixture.State(ctx);
            Fixture.Place(ctx, st, "depot", 10, 10);
            var q = Send(ctx, st, new HandCraftCommand(1));
            Assert.IsTrue(q.Accepted, q.Problem);
            Step(ctx, st, 1);
            Assert.Greater(st.Hand.Jobs[0].Progress, 0, "the batch is being processed");
            Assert.IsFalse(HandCraft.HandLocked(st), "U-D-44: and that does not pin the engineer");
            return (ctx, st);
        }

        /// <summary>Engages the one thing that still locks: a repair in progress on the Home state.</summary>
        private static void StartRepair(SimState st)
        {
            st.Home.RepairKind = RepairKinds.Machine;
            st.Home.RepairId = 1;
            st.Home.RepairRemaining = 10;
            Assert.IsTrue(HandCraft.HandLocked(st));
            Assert.AreEqual(Home.LockText, HandCraft.LockTextFor(st));
        }

        // ---- a queued batch does not lock anything ---------------------------------------------------------

        [Test]
        public void HeldKeysMoveTheEngineerWhileTheWorkshopProcesses()
        {
            var (ctx, st) = Working();
            var e = st.Engineer;
            var at = e.Pos;

            var w = Send(ctx, st, new WalkCommand(1, 0));
            Assert.IsTrue(w.Accepted, w.Problem);
            Step(ctx, st, 20);

            Assert.AreEqual(at.X + ctx.Data.Engineer.WalkTilesPerS, e.Pos.X, 1e-6, "a full second of walking");
            Assert.Greater(e.Walked, 0, "and it is counted as walked");
            Assert.AreEqual(1, HandCraft.QueuedBatches(st), "the batch is still queued, not cancelled by the walk");
            Assert.Greater(st.Hand.Jobs[0].Progress, 1.0, "and it kept being processed while the engineer left");
        }

        [Test]
        public void AWalkHereAndTheDodgeStayAvailableWhileTheWorkshopProcesses()
        {
            var (ctx, st) = Working();
            var e = st.Engineer;

            var m = Send(ctx, st, new MoveCommand(20.5, 16.5));
            Assert.IsTrue(m.Accepted, m.Problem);
            Assert.IsTrue(e.HasTarget);
            Step(ctx, st, 2);
            Assert.IsTrue(e.HasTarget, "the route survives: nothing drops it any more");

            var dodge = Send(ctx, st, new DodgeCommand());
            Assert.IsTrue(dodge.Accepted, dodge.Problem);
            Assert.Greater(e.Dash, 0);
            Step(ctx, st, 1);
            Assert.Greater(e.Dash, 0, "a 0.25 s dash outlasts one 0.05 s tick and is not cancelled");
        }

        /// <summary>
        /// The queue used to be protected by refusing transfers: its ingredients were still in the Backpack while a
        /// batch ran, so moving them into a chest would have stolen them. Under U-D-44 they are not there to steal —
        /// they were reserved at the workshop when the job was queued — so the transfer is simply allowed and the
        /// job is unaffected. That is the same protection, done by accounting rather than by a refusal.
        /// </summary>
        [Test]
        public void ALoadIntoAMachineIsAllowedAndCannotTakeTheQueuesIngredients()
        {
            var (ctx, st) = Working();
            var chest = Fixture.Place(ctx, st, "chest", 18, 16);
            var reserved = st.Hand.Reserved[ItemId.Steel];
            Assert.AreEqual(2, reserved);

            var t = Fixture.Apply(ctx, st, new MachineTransferCommand(chest.Id, ItemId.Steel, 999, true));
            Assert.IsTrue(t.Accepted, t.Problem);
            Assert.AreEqual(0.0, st.Engineer.Inv[ItemId.Steel], Eps, "everything the Backpack held went in");
            Assert.AreEqual(reserved, st.Hand.Reserved[ItemId.Steel], "and the queue's own plates were never there to take");

            Step(ctx, st, (int)(HandCraft.Recipe(ctx.Data, "hand-bullets").Seconds / Fixture.Dt) + 2);
            Assert.AreEqual(1, st.Stats.HandCrafted, "so the batch still finished");
            Assert.AreEqual(10, st.Hand.Output[ItemId.Magazine]);
            Assert.AreEqual("", Fixture.Off(ctx, st));
        }

        /// <summary>
        /// Going down no longer cancels the batch (the reference's flow.ts:1007 belonged to the pinned model). The
        /// workshop is a place: it keeps processing, and the goods are waiting when the engineer gets back up.
        /// </summary>
        [Test]
        public void ADownedEngineerIsNotLockedAndTheWorkshopKeepsWorking()
        {
            var (ctx, st) = Working();
            var e = st.Engineer;

            e.Down = st.T + ctx.Data.Engineer.RespawnS;
            Assert.IsTrue(e.IsDown);
            Assert.IsFalse(HandCraft.HandLocked(st), "being down is not being pinned at a repair");

            Step(ctx, st, (int)(HandCraft.Recipe(ctx.Data, "hand-bullets").Seconds / Fixture.Dt) + 2);
            Assert.AreEqual(1, st.Stats.HandCrafted, "the workshop does not care where the engineer is");
            Assert.AreEqual(10, st.Hand.Output[ItemId.Magazine]);
            Assert.AreEqual(0, e.Inv[ItemId.Magazine], "and nothing was delivered to a downed engineer");
            Assert.AreEqual("", Fixture.Off(ctx, st));
        }

        // ---- a repair still locks, and it is really enforced -----------------------------------------------

        [Test]
        public void HeldKeysDoNotMoveTheEngineerWhileARepairRuns()
        {
            var (ctx, st) = Working();
            var e = st.Engineer;
            Assert.IsTrue(Send(ctx, st, new WalkCommand(1, 0)).Accepted);
            StartRepair(st);
            var at = e.Pos;

            Step(ctx, st, 20);
            Assert.AreEqual(at.X, e.Pos.X, Eps, "a locked engineer does not walk");
            Assert.AreEqual(at.Y, e.Pos.Y, Eps);
            Assert.AreEqual(0.0, e.Walked, Eps, "and nothing is counted as walked");
            Assert.AreEqual(1.0, e.Vel.X, Eps, "the held-key intent is kept, not thrown away");
            Assert.IsFalse(e.HasTarget);
        }

        [Test]
        public void AWalkHereAndTheDodgeAreRefusedWhileARepairRunsAndOnesInFlightAreDropped()
        {
            var (ctx, st) = Working();
            var e = st.Engineer;
            Assert.IsTrue(Send(ctx, st, new MoveCommand(20.5, 16.5)).Accepted);
            Assert.IsTrue(e.HasTarget);
            Step(ctx, st, 2);
            Assert.IsNotNull(e.Plan, "the walk-here is under way, with a route");

            Assert.IsTrue(Send(ctx, st, new DodgeCommand()).Accepted);
            Assert.Greater(e.Dash, 0);
            Assert.IsFalse(e.HasTarget, "the dodge itself drops the walk-here — reference engineer.ts:330");
            Assert.IsTrue(Send(ctx, st, new MoveCommand(20.5, 16.5)).Accepted, "re-issued, so both are in flight");
            Assert.IsTrue(e.HasTarget);

            StartRepair(st);
            Step(ctx, st, 1);
            Assert.IsFalse(e.HasTarget, "the walk-here is dropped when the lock engages");
            Assert.IsNull(e.Plan, "and so is its route");
            Assert.AreEqual(0.0, e.Dash, Eps, "and the dash in flight with it");

            var at = e.Pos;
            var m = Send(ctx, st, new MoveCommand(20.5, 16.5));
            Assert.IsFalse(m.Accepted, "a new walk-here is refused while locked");
            Assert.AreEqual(Home.LockText, m.Problem, "and it says why, so the UI can show it");

            var dodge = Send(ctx, st, new DodgeCommand());
            Assert.IsFalse(dodge.Accepted);
            Assert.AreEqual(Home.LockText, dodge.Problem);

            Step(ctx, st, 20);
            Assert.AreEqual(at.X, e.Pos.X, Eps);
            Assert.AreEqual(at.Y, e.Pos.Y, Eps);
        }

        [Test]
        public void ALoadIntoAMachineIsRefusedWhileARepairRuns()
        {
            var (ctx, st) = Working();
            var chest = Fixture.Place(ctx, st, "chest", 18, 16);
            StartRepair(st);
            var steel = st.Engineer.Inv[ItemId.Steel];

            var t = Fixture.Apply(ctx, st, new MachineTransferCommand(chest.Id, ItemId.Steel, 2, true));
            Assert.IsFalse(t.Accepted, "a repair has both hands busy");
            Assert.AreEqual(Home.LockText, t.Problem);
            Assert.AreEqual(steel, st.Engineer.Inv[ItemId.Steel], Eps, "and nothing left the pockets");
            Assert.AreEqual(0.0, chest.Inv[ItemId.Steel], Eps);

            var take = Fixture.Apply(ctx, st, new MachineTransferCommand(chest.Id, ItemId.Steel, 2, false));
            Assert.IsFalse(take.Accepted, "taking from the machine is refused by the same rule");
            Assert.AreEqual(Home.LockText, take.Problem);
        }

        /// <summary>
        /// Cancelling is never gated on the lock — that is what makes the lock escapable. The workshop's own cancel
        /// works during a repair, and gives the queue's reserved ingredients straight back.
        /// </summary>
        [Test]
        public void CancellingTheQueueStaysAvailableWhileARepairRuns()
        {
            var (ctx, st) = Working();
            StartRepair(st);

            var c = Send(ctx, st, new CancelCraftCommand());
            Assert.IsTrue(c.Accepted, "cancelling is always available while locked: " + c.Problem);
            Assert.AreEqual(20, st.Engineer.Inv[ItemId.Steel], "and the batch's plates came back");
            Assert.AreEqual(5, st.Engineer.Inv[ItemId.Copper]);
            Assert.IsTrue(HandCraft.HandLocked(st), "the repair itself is untouched by it");
            Assert.AreEqual("", Fixture.Off(ctx, st));
        }

        [Test]
        public void HealthAndTheDodgeCooldownKeepRunningWhileLocked()
        {
            var ctx = Fixture.Context();
            var st = Fixture.State(ctx);
            var d = ctx.Data.Engineer;
            var e = st.Engineer;

            Assert.IsTrue(Send(ctx, st, new DodgeCommand()).Accepted);
            Assert.AreEqual(d.DashCooldownS, e.DashCooldown, Eps);
            e.Hp = d.MaxHp - 20;
            e.LastHit = st.T - d.RegenDelayS - 1;   // the regen delay is already behind us
            StartRepair(st);

            Step(ctx, st, 1);
            var hp = e.Hp;
            var cooldown = e.DashCooldown;

            Step(ctx, st, 10);
            Assert.AreEqual(hp + d.RegenPerS * 10 * Fixture.Dt, e.Hp, 1e-9, "health still regenerates while locked");
            Assert.AreEqual(cooldown - 10 * Fixture.Dt, e.DashCooldown, 1e-9,
                "and the dodge cooldown still counts down (the reference freezes it; the port does not)");
        }
    }
}
