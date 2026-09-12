using System.Collections.Generic;
using NUnit.Framework;

namespace Relight.Sim.Tests
{
    /// <summary>
    /// The hand-crafting lock (reference walk.ts:181-183, GP-PLAYTEST-FIX 2): a running batch pins the engineer at
    /// the workbench until it finishes or is cancelled.
    ///
    /// These checks go through real commands and real ticks, never through <see cref="HandCraft.HandLocked"/> alone:
    /// the defect this covers was a predicate that existed and that nothing read. The phase order is the composition's
    /// — world (<see cref="EngineerMovementPhase"/>) before inventory (<see cref="HandCraftPhase"/>) — so the lock
    /// takes hold on the tick after the batch starts, and that is asserted rather than papered over.
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

        /// <summary>A state at the spawn with a workbench in reach and one batch already running.</summary>
        private static (SimContext, SimState) Crafting()
        {
            var ctx = Fixture.Context();
            var st = Fixture.State(ctx);
            Fixture.Place(ctx, st, "depot", 10, 10);
            var q = Send(ctx, st, new HandCraftCommand(1));
            Assert.IsTrue(q.Accepted, q.Problem);
            Assert.IsFalse(HandCraft.HandLocked(st), "queued is not yet running");
            Step(ctx, st, 1);
            Assert.IsTrue(st.Hand.Crafting, "the batch took its plates on the first tick");
            Assert.IsTrue(HandCraft.HandLocked(st));
            return (ctx, st);
        }

        [Test]
        public void HeldKeysDoNotMoveTheEngineerWhileABatchRuns()
        {
            var (ctx, st) = Crafting();
            var e = st.Engineer;
            var at = e.Pos;

            var w = Send(ctx, st, new WalkCommand(1, 0));
            Assert.IsTrue(w.Accepted, "the held key is still recorded: the presentation only sends it on a change");
            Step(ctx, st, 20);

            Assert.AreEqual(at.X, e.Pos.X, Eps, "a locked engineer does not walk");
            Assert.AreEqual(at.Y, e.Pos.Y, Eps);
            Assert.AreEqual(0.0, e.Walked, Eps, "and nothing is counted as walked");
            Assert.AreEqual(1.0, e.Vel.X, Eps, "the held-key intent is kept, not thrown away");
            Assert.IsFalse(e.HasTarget);
            Assert.IsTrue(st.Hand.Crafting, "the batch is still running");
        }

        [Test]
        public void CancellingHandsControlBackToAKeyThatIsStillHeld()
        {
            var (ctx, st) = Crafting();
            var e = st.Engineer;
            Assert.IsTrue(Send(ctx, st, new WalkCommand(1, 0)).Accepted);
            Step(ctx, st, 20);
            var at = e.Pos;

            var c = Send(ctx, st, new CancelCraftCommand());
            Assert.IsTrue(c.Accepted, "cancelling is always available while locked: " + c.Problem);
            Assert.IsFalse(HandCraft.HandLocked(st));

            // No new WalkCommand: the key never changed, so the presentation would never re-send one.
            Step(ctx, st, 1);
            Assert.AreEqual(at.X + ctx.Data.Engineer.WalkTilesPerS * Fixture.Dt, e.Pos.X, Eps,
                "the very next tick moves at the full walk speed");
            Assert.AreEqual(20, st.Engineer.Inv[ItemId.Steel], "and the batch's plates came back");
            Assert.AreEqual("", Fixture.Off(ctx, st));
        }

        [Test]
        public void AWalkHereIsRefusedWhileLockedAndAQueuedOneIsDropped()
        {
            var ctx = Fixture.Context();
            var st = Fixture.State(ctx);
            Fixture.Place(ctx, st, "depot", 10, 10);
            var e = st.Engineer;

            // Queued first, so the route is live when the batch starts on the next tick.
            Assert.IsTrue(Send(ctx, st, new MoveCommand(20.5, 16.5)).Accepted);
            Assert.IsTrue(e.HasTarget);
            Assert.IsTrue(Send(ctx, st, new HandCraftCommand(1)).Accepted);

            Step(ctx, st, 1);   // movement still free this tick; the batch starts after it
            Assert.IsTrue(HandCraft.HandLocked(st));
            Step(ctx, st, 1);
            Assert.IsFalse(e.HasTarget, "the walk-here is dropped when the lock engages");
            Assert.IsNull(e.Plan, "and so is its route");

            var at = e.Pos;
            var m = Send(ctx, st, new MoveCommand(20.5, 16.5));
            Assert.IsFalse(m.Accepted, "a new walk-here is refused while locked");
            Assert.AreEqual(HandCraft.LockText, m.Problem, "and it says why, so the UI can show it");
            Assert.IsFalse(e.HasTarget);

            Step(ctx, st, 20);
            Assert.AreEqual(at.X, e.Pos.X, Eps);
            Assert.AreEqual(at.Y, e.Pos.Y, Eps);
        }

        [Test]
        public void TheDodgeIsRefusedWhileLockedAndOneInFlightIsDropped()
        {
            var ctx = Fixture.Context();
            var st = Fixture.State(ctx);
            Fixture.Place(ctx, st, "depot", 10, 10);
            var e = st.Engineer;

            Assert.IsTrue(Send(ctx, st, new HandCraftCommand(1)).Accepted);
            Assert.IsTrue(Send(ctx, st, new DodgeCommand()).Accepted, "not locked yet: the dodge is allowed");
            Assert.Greater(e.Dash, 0);

            Step(ctx, st, 1);   // the dash still moves this tick; the batch starts after it
            Assert.IsTrue(HandCraft.HandLocked(st));
            Assert.Greater(e.Dash, 0, "a 0.25 s dash outlasts one 0.05 s tick");

            Step(ctx, st, 1);
            Assert.AreEqual(0.0, e.Dash, Eps, "the dash in flight is dropped when the lock engages");
            var at = e.Pos;

            var dodge = Send(ctx, st, new DodgeCommand());
            Assert.IsFalse(dodge.Accepted, "a new dodge is refused while locked");
            Assert.AreEqual(HandCraft.LockText, dodge.Problem);
            Assert.AreEqual(0.0, e.Dash, Eps);

            Step(ctx, st, 10);
            Assert.AreEqual(at.X, e.Pos.X, Eps, "and nothing moved");
            Assert.AreEqual(at.Y, e.Pos.Y, Eps);
        }

        [Test]
        public void HealthAndTheDodgeCooldownKeepRunningWhileLocked()
        {
            var ctx = Fixture.Context();
            var st = Fixture.State(ctx);
            var d = ctx.Data.Engineer;
            Fixture.Place(ctx, st, "depot", 10, 10);
            var e = st.Engineer;

            Assert.IsTrue(Send(ctx, st, new HandCraftCommand(1)).Accepted);
            Assert.IsTrue(Send(ctx, st, new DodgeCommand()).Accepted);
            Assert.AreEqual(d.DashCooldownS, e.DashCooldown, Eps);
            e.Hp = d.MaxHp - 20;
            e.LastHit = st.T - d.RegenDelayS - 1;   // the regen delay is already behind us

            Step(ctx, st, 2);   // tick 1 starts the batch, tick 2 is the first locked tick
            Assert.IsTrue(HandCraft.HandLocked(st));
            var hp = e.Hp;
            var cooldown = e.DashCooldown;

            Step(ctx, st, 10);
            Assert.AreEqual(hp + d.RegenPerS * 10 * Fixture.Dt, e.Hp, 1e-9, "health still regenerates while locked");
            Assert.AreEqual(cooldown - 10 * Fixture.Dt, e.DashCooldown, 1e-9,
                "and the dodge cooldown still counts down (the reference freezes it; the port does not)");
        }

        [Test]
        public void ADownedEngineerIsNotLockedAndTheBatchIsCancelled()
        {
            var (ctx, st) = Crafting();
            var e = st.Engineer;

            e.Down = st.T + ctx.Data.Engineer.RespawnS;
            Assert.IsTrue(e.IsDown);
            Assert.IsFalse(HandCraft.HandLocked(st), "being down is not being pinned at the workbench");

            Step(ctx, st, 1);
            Assert.IsFalse(st.Hand.Crafting, "going down cancels the batch (reference flow.ts:1007)");
            Assert.AreEqual(20, e.Inv[ItemId.Steel], "and returns its plates");
            Assert.AreEqual(5, e.Inv[ItemId.Copper]);
            Assert.AreEqual("", Fixture.Off(ctx, st));
        }

        [Test]
        public void AFinishedBatchReleasesTheEngineerOnItsOwn()
        {
            var (ctx, st) = Crafting();
            var e = st.Engineer;
            Assert.IsTrue(Send(ctx, st, new WalkCommand(1, 0)).Accepted);
            var at = e.Pos;

            Step(ctx, st, (int)(ctx.Data.Engineer.HandBulletSeconds / Fixture.Dt) + 2);
            Assert.IsFalse(st.Hand.Crafting, "the batch completed");
            Assert.AreEqual(ctx.Data.Engineer.HandBulletsPerCraft, e.Inv[ItemId.Magazine]);
            Assert.Greater(e.Pos.X, at.X, "the still-held key moves the engineer again with no new command");
        }
    }
}
