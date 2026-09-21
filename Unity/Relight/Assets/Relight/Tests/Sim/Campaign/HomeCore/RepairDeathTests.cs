using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using Relight.Sim.Tests.Combat;
using Relight.Sim.Tests.Core;

namespace Relight.Sim.Tests.Campaign
{
    /// <summary>
    /// REL-16 (INT-12): dying during a repair used to root the engineer for good. <c>Engineer.TakeDamage</c> left
    /// <c>Home.RepairKind</c> set, the body respawned across the map, <see cref="Home.RepairLocked"/> came back on
    /// the moment it stood up, and the repair could neither finish (its target was out of reach) nor be walked
    /// away from. Every autosave then carried the lock.
    ///
    /// The rule now: going down always ends a running repair, through the Cancel path and before the death
    /// handling, so the refund shares the Backpack's fate (the pile). And a save written in the locked state is
    /// healed as it loads. These run real commands and the real movement and Home phases, away from the spawn,
    /// because the defect lived between the three of them.
    /// </summary>
    public sealed class RepairDeathTests
    {
        private const string LockedSave = "locked-repair.save.json";
        private const int Tx = 20, Ty = 20;               // the turret: some 50 tiles from the spawn
        private const double SpawnX = 72.5, SpawnY = 72.5; // beside the core at 76,76 and within reach of it

        /// <summary>
        /// <see cref="RaidFixture"/>'s flat map and core, with the spawn moved off the core's footprint: the
        /// fixture spawns in the middle of the map, which is INSIDE its 8×8 core, where a body cannot walk.
        /// </summary>
        private static SimContext Context()
        {
            const int size = RaidFixture.Size;
            var kind = new byte[size * size];
            for (var i = 0; i < kind.Length; i++) kind[i] = (byte)TileClass.Ground;
            return RaidFixture.Context(new ArrayGeometry(size, size, kind, new bool[size * size], new Vec2(SpawnX, SpawnY)));
        }

        private static readonly List<ITickPhase> Phases = new List<ITickPhase> { new EngineerMovementPhase(), new HomeCorePhase() };

        private static void Run(SimContext ctx, SimState st, double seconds) =>
            RaidFixture.Run(ctx, st, (int)(seconds / RaidFixture.Dt) + 1, Phases);

        private static CommandResult Send(SimContext ctx, SimState st, Command c)
        {
            var handlers = new List<ICommandHandler> { new MovementCommandHandler(), new HomeCoreHandler() };
            for (var i = 0; i < handlers.Count; i++)
                if (handlers[i].TryApply(ctx, st, c, out var r)) return r;
            return CommandResult.Refuse("no handler");
        }

        private static string Off(SimContext ctx, SimState st) =>
            string.Join(" | ", Ledger.Conservation(st, ctx.Data).Problems);

        /// <summary>The engineer beside a damaged turret far from the spawn, holding 10 steel and 10 copper, repair running.</summary>
        private static SimState RepairingATurretAwayFromTheSpawn(SimContext ctx, out Machine turret)
        {
            var st = RaidFixture.State(ctx);
            new HomeCoreInitializer().Init(ctx, st);
            turret = RaidFixture.Turret(ctx, st, Tx, Ty);
            st.Engineer.Pos = new Vec2(Tx - 0.5, Ty - 0.5);
            st.Engineer.Inv[ItemId.Steel] = 10;
            st.Engineer.Inv[ItemId.Copper] = 10;
            st.Ledger = Ledger.Open(st, ctx.Data);
            TurretRules.Damage(ctx, st, turret, 60);

            var r = Send(ctx, st, new RepairCommand(RepairKinds.Machine, turret.Id));
            Assert.That(r.Problem, Is.Empty, "the repair starts");
            Assert.That(st.Engineer.Inv[ItemId.Steel], Is.EqualTo(10 - ctx.Data.Defence.RepairSteel), "and is paid for up front");
            Run(ctx, st, 1);
            Assert.That(Home.RepairLocked(st), Is.True);
            return st;
        }

        private static void AssertWalks(SimContext ctx, SimState st)
        {
            var from = st.Engineer.Pos;
            var move = Send(ctx, st, new MoveCommand(from.X + 6, from.Y));
            Assert.That(move.Problem, Is.Empty, "a move is accepted");
            Run(ctx, st, 1);
            Assert.That(st.Engineer.Pos.X, Is.GreaterThan(from.X + 1), "and the engineer WALKS");
        }

        // ---------------------------------------------------------------- going down ends the repair

        [Test]
        public void DyingDuringAMachineRepairEndsIt_AndTheRespawnedEngineerWalks()
        {
            var ctx = Context();
            var st = RepairingATurretAwayFromTheSpawn(ctx, out var turret);
            var hp = TurretRules.Hp(ctx.Data, st, turret);

            st.Engineer.TakeDamage(ctx, st, 100000);
            Assert.That(st.Engineer.IsDown, Is.True);
            Assert.That(st.Home.RepairKind, Is.EqualTo(RepairKinds.None), "going down ends the repair at once");
            Assert.That(st.Home.RepairId, Is.EqualTo(-1));
            Assert.That(st.Home.RepairRemaining, Is.Zero);

            Run(ctx, st, ctx.Data.Engineer.RespawnS + 1);
            Assert.That(st.Engineer.IsDown, Is.False, "respawned");
            Assert.That(st.Engineer.Pos.X, Is.EqualTo(ctx.Geometry.Spawn.X).Within(1e-9), "at the spawn, far from the turret");
            Assert.That(st.Home.RepairKind, Is.EqualTo(RepairKinds.None));
            Assert.That(Home.RepairLocked(st), Is.False);
            Assert.That(HandCraft.HandLocked(st), Is.False);
            Assert.That(TurretRules.Hp(ctx.Data, st, turret), Is.EqualTo(hp), "no work was banked");

            AssertWalks(ctx, st);
        }

        [Test]
        public void TheDeathRefundsThroughTheCancelPath_AndTheLedgerBalances()
        {
            var ctx = Context();
            var st = RepairingATurretAwayFromTheSpawn(ctx, out _);
            Assert.That(st.Stats.SpentSteel, Is.EqualTo((double)ctx.Data.Defence.RepairSteel));
            Assert.That(st.Stats.SpentCopper, Is.EqualTo((double)ctx.Data.Defence.RepairCopper));

            st.Engineer.TakeDamage(ctx, st, 100000);

            Assert.That(st.Stats.SpentSteel, Is.Zero, "the sink is unwound, as Cancel unwinds it");
            Assert.That(st.Stats.SpentCopper, Is.Zero);
            Assert.That(st.Home.PaidSteel, Is.Zero);
            Assert.That(st.Home.PaidCopper, Is.Zero);

            // The refund happened BEFORE the death handling, so it shares the Backpack's fate: the pile.
            Assert.That(st.Engineer.Inv[ItemId.Steel], Is.Zero);
            Assert.That(st.Drops.Caches, Has.Count.EqualTo(1));
            var pile = st.Drops.Caches[0];
            Assert.That(pile.Items[ItemKey.Of(ItemId.Steel)], Is.EqualTo(10), "all ten steel, the refunded two included");
            Assert.That(pile.Items[ItemKey.Of(ItemId.Copper)], Is.EqualTo(10));
            Assert.That(Off(ctx, st), Is.Empty);

            Run(ctx, st, ctx.Data.Engineer.RespawnS + 1);
            Assert.That(Off(ctx, st), Is.Empty, "and it still balances after the respawn");
        }

        [Test]
        public void ACoreRepairBehavesTheSameWay()
        {
            var ctx = Context();
            var st = RaidFixture.State(ctx);
            new HomeCoreInitializer().Init(ctx, st);
            st.Engineer.Pos = new Vec2(RaidFixture.CoreX - 0.5, RaidFixture.CoreY - 0.5);
            st.Engineer.Inv[ItemId.Steel] = 10;
            st.Engineer.Inv[ItemId.Copper] = 10;
            st.Ledger = Ledger.Open(st, ctx.Data);
            HomeCore.Damage(st, 100);
            var hp = HomeQueries.CoreHp(st);
            Assert.That(Send(ctx, st, new RepairCommand(RepairKinds.Core, -1)).Problem, Is.Empty);
            Run(ctx, st, 1);
            Assert.That(Home.RepairLocked(st), Is.True);

            st.Engineer.TakeDamage(ctx, st, 100000);
            Assert.That(st.Home.RepairKind, Is.EqualTo(RepairKinds.None));
            Assert.That(st.Stats.SpentSteel, Is.Zero);
            Assert.That(st.Stats.SpentCopper, Is.Zero);
            Assert.That(st.Drops.Caches[0].Items[ItemKey.Of(ItemId.Steel)], Is.EqualTo(10));
            Assert.That(Off(ctx, st), Is.Empty);

            // The spawn is beside this core, so the OLD build quietly finished the repair after the respawn. It
            // is ended like any other now: the core is no better off and the engineer is free.
            Run(ctx, st, ctx.Data.Engineer.RespawnS + ctx.Data.Defence.RepairSeconds + 1);
            Assert.That(HomeQueries.CoreHp(st), Is.EqualTo(hp), "no work was banked");
            Assert.That(Home.RepairLocked(st), Is.False);
            AssertWalks(ctx, st);
        }

        [Test]
        public void AHitThatDoesNotKillLeavesTheRepairRunning()
        {
            var ctx = Context();
            var st = RepairingATurretAwayFromTheSpawn(ctx, out _);
            st.Engineer.TakeDamage(ctx, st, 1);
            Assert.That(st.Engineer.IsDown, Is.False);
            Assert.That(st.Home.RepairKind, Is.EqualTo(RepairKinds.Machine));
            Assert.That(Home.RepairLocked(st), Is.True);
        }

        // ---------------------------------------------------------------- a save written in the locked state

        /// <summary>
        /// <c>Fixtures/locked-repair.save.json</c> was written by the build BEFORE this fix (HEAD c8db6bef), by the
        /// steps of the first test: repair a turret at 20,20, die, respawn at 72.5,72.5. It holds a standing engineer
        /// with a Machine repair running some 50 tiles away, 2 steel and 1 copper paid, and the pile of 8 and 9.
        /// </summary>
        [Test]
        public void ASaveWrittenInTheLockedStateLoadsWithTheEngineerFreeToMove()
        {
            var ctx = Context();
            var text = File.ReadAllText(CoreFixtures.Path(LockedSave));

            // Read the way a tool reads it, with no running game: exactly as written, lock and all.
            var raw = SaveSerializer.ReadText(text, ctx.Data);
            Assert.That(raw.Ok, Is.True, raw.Reason);
            Assert.That(raw.State.Home.RepairKind, Is.EqualTo(RepairKinds.Machine), "the fixture really is the locked state");
            Assert.That(Home.RepairLocked(raw.State), Is.True);
            Assert.That(raw.State.Engineer.IsDown, Is.False);

            // Loaded into the running game, as SaveStore loads it.
            var load = SaveSerializer.ReadText(text, ctx.Data, null, ctx);
            Assert.That(load.Ok, Is.True, load.Reason);
            var st = load.State;
            Assert.That(load.Upgraded, Does.Contain("could never finish"), "and the load says what it did");
            Assert.That(st.Home.RepairKind, Is.EqualTo(RepairKinds.None));
            Assert.That(st.Home.RepairId, Is.EqualTo(-1));
            Assert.That(st.Home.PaidSteel, Is.Zero);
            Assert.That(st.Home.PaidCopper, Is.Zero);
            Assert.That(Home.RepairLocked(st), Is.False);
            Assert.That(st.Engineer.Inv[ItemId.Steel], Is.EqualTo(ctx.Data.Defence.RepairSteel), "refunded through the Cancel path");
            Assert.That(st.Engineer.Inv[ItemId.Copper], Is.EqualTo(ctx.Data.Defence.RepairCopper));
            Assert.That(st.Stats.SpentSteel, Is.Zero);
            Assert.That(Off(ctx, st), Is.Empty);

            AssertWalks(ctx, st);
        }

        [Test]
        public void ASaveWithARepairInReachLoadsUnchanged()
        {
            var ctx = Context();
            var st = RepairingATurretAwayFromTheSpawn(ctx, out _);
            st.Events.Clear();
            const string at = "2026-09-22T00:00:00.0000000Z";
            var text = SaveSerializer.WriteText(st, ctx.Data, at);

            var load = SaveSerializer.ReadText(text, ctx.Data, null, ctx);
            Assert.That(load.Ok, Is.True, load.Reason);
            Assert.That(load.Upgraded, Is.Null, "nothing was healed");
            Assert.That(load.State.Home.RepairKind, Is.EqualTo(RepairKinds.Machine));
            Assert.That(SaveSerializer.WriteText(load.State, ctx.Data, at), Is.EqualTo(text), "byte for byte");
        }

        [Test]
        public void ASaveWithNoRepairLoadsUnchanged()
        {
            var ctx = Context();
            var st = RaidFixture.State(ctx);
            new HomeCoreInitializer().Init(ctx, st);
            RaidFixture.Turret(ctx, st, Tx, Ty);
            const string at = "2026-09-22T00:00:00.0000000Z";
            var text = SaveSerializer.WriteText(st, ctx.Data, at);

            var load = SaveSerializer.ReadText(text, ctx.Data, null, ctx);
            Assert.That(load.Ok, Is.True, load.Reason);
            Assert.That(load.Upgraded, Is.Null);
            Assert.That(SaveSerializer.WriteText(load.State, ctx.Data, at), Is.EqualTo(text), "byte for byte");
        }

        /// <summary>
        /// A save caught while the engineer is DOWN mid-repair (the old build wrote these too, in the ten seconds
        /// before the respawn). Nobody is locked yet, but the death rule cannot help: the death already happened,
        /// in the old build. The load ends the repair, so the respawn does not turn it into the lock.
        /// </summary>
        [Test]
        public void ASaveCaughtWhileDownMidRepairDoesNotBecomeTheLockAtTheRespawn()
        {
            var ctx = Context();
            var st = RepairingATurretAwayFromTheSpawn(ctx, out var turret);
            // The old build's death: down, with the repair still set.
            st.Engineer.TakeDamage(ctx, st, 100000);
            st.Home.RepairKind = RepairKinds.Machine;
            st.Home.RepairId = turret.Id;
            st.Home.RepairRemaining = 3;
            st.Events.Clear();
            var text = SaveSerializer.WriteText(st, ctx.Data, "2026-09-22T00:00:00.0000000Z");

            var load = SaveSerializer.ReadText(text, ctx.Data, null, ctx);
            Assert.That(load.Ok, Is.True, load.Reason);
            st = load.State;
            Assert.That(st.Home.RepairKind, Is.EqualTo(RepairKinds.None));
            Assert.That(Off(ctx, st), Is.Empty);
            Run(ctx, st, ctx.Data.Engineer.RespawnS + 1);
            Assert.That(st.Engineer.IsDown, Is.False);
            Assert.That(Home.RepairLocked(st), Is.False, "the respawned engineer is not rooted");
            AssertWalks(ctx, st);
        }
    }
}
