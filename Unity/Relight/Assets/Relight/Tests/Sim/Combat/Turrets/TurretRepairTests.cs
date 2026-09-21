using System.Collections.Generic;
using NUnit.Framework;

namespace Relight.Sim.Tests.Combat
{
    /// <summary>
    /// The C-04/C-05 seams the coordinator closed in <c>Sim/Combat/Turrets/HomeCore.Turrets.cs</c>: a damaged
    /// turret is priced and repaired through <c>RepairCommand(Machine, id)</c>, and a knocked-out core refuses a
    /// recommission while a raid body is still on the map (reference campaignDefence.ts <c>repairCheck</c>).
    /// </summary>
    public sealed class TurretRepairTests
    {
        private static SimState State(SimContext ctx)
        {
            var st = RaidFixture.State(ctx);
            st.Engineer.Pos = new Vec2(RaidFixture.CoreX + 4, RaidFixture.CoreY + 4);
            new HomeCoreInitializer().Init(ctx, st);
            return st;
        }

        private static CommandResult Apply(SimContext ctx, SimState st, Command c)
        {
            Assert.That(new HomeCoreHandler().TryApply(ctx, st, c, out var r), Is.True);
            return r;
        }

        private static void RunHome(SimContext ctx, SimState st, double seconds)
        {
            var phases = new List<ITickPhase> { new HomeCorePhase() };
            RaidFixture.Run(ctx, st, (int)(seconds / RaidFixture.Dt) + 1, phases);
        }

        [Test]
        public void ADamagedTurretIsPricedFromItsHitPointsAndRepairedByRepairHp()
        {
            var ctx = RaidFixture.Context();
            var st = State(ctx);
            var t = RaidFixture.Turret(ctx, st, RaidFixture.CoreX + 4, RaidFixture.CoreY + 4);
            st.Engineer.Inv[ItemId.Steel] = 10;
            st.Engineer.Inv[ItemId.Copper] = 10;
            st.Ledger = Ledger.Open(st, ctx.Data);            // opened once the fixture's stock is on the map
            var max = TurretRules.MaxHp(ctx.Data, t);
            Assert.That(max, Is.GreaterThan(0), "a turret is a defence structure");

            Assert.That(Apply(ctx, st, new RepairCommand(RepairKinds.Machine, t.Id)).Problem,
                Is.EqualTo("the core is already at full health").Or.EqualTo("nothing damaged here"),
                "an undamaged turret has nothing to repair");

            TurretRules.Damage(ctx, st, t, 60);
            var price = HomeCore.Price(ctx.Data, st, RepairKinds.Machine, t.Id);
            Assert.That(price.Max, Is.EqualTo(max));
            Assert.That(price.Hp, Is.EqualTo(max - 60));
            Assert.That(price.Recommission, Is.False);

            var r = Apply(ctx, st, new RepairCommand(RepairKinds.Machine, t.Id));
            Assert.That(r.Problem, Is.Empty, "the repair starts: " + r.Problem);
            Assert.That(HandCraft.HandLocked(st), Is.True, "a repair pins the engineer");
            Assert.That(HandCraft.LockTextFor(st), Is.EqualTo(Home.LockText));

            RunHome(ctx, st, ctx.Data.Defence.RepairSeconds);
            Assert.That(HandCraft.HandLocked(st), Is.False);
            Assert.That(TurretRules.Hp(ctx.Data, st, t), Is.EqualTo(max - 60 + ctx.Data.Defence.RepairHp).Within(1e-9));
            Assert.That(st.Engineer.Inv[ItemId.Steel], Is.EqualTo(10 - ctx.Data.Defence.RepairSteel));
            Assert.That(st.Engineer.Inv[ItemId.Copper], Is.EqualTo(10 - ctx.Data.Defence.RepairCopper));
            Assert.That(RaidFixture.Count<StructureRepairedEvent>(st), Is.EqualTo(1));
            Assert.That(Ledger.Conservation(st, ctx.Data).Problems, Is.Empty);
        }

        [Test]
        public void AKnockedOutCoreWaitsForTheAttackersToLeave()
        {
            var ctx = RaidFixture.Context();
            var st = State(ctx);
            st.Engineer.Inv[ItemId.Steel] = 20;
            st.Engineer.Inv[ItemId.Copper] = 20;
            HomeCore.Damage(st, 1000);
            Assert.That(HomeQueries.CoreOperational(st), Is.False);

            // E-18: a raid body far across the map no longer blocks the repair; one near the core does.
            RaidFixture.Body(st, "skitter", 40.5, 40.5);
            Assert.That(Home.AttackersNearby(ctx, st), Is.False, "35 tiles away is not 'nearby'");
            var (cx, cy, _, _) = HomeQueries.CoreRect(st);
            RaidFixture.Body(st, "skitter", cx - 5.5, cy + 0.5);
            Assert.That(Home.AttackersNearby(ctx, st), Is.True);
            Assert.That(Apply(ctx, st, new RepairCommand(RepairKinds.Core, 0)).Problem,
                Is.EqualTo("wait for the attackers to leave"));

            st.Enemies.Actors.Clear();
            Assert.That(Home.AttackersNearby(ctx, st), Is.False);
            Assert.That(Apply(ctx, st, new RepairCommand(RepairKinds.Core, 0)).Problem, Is.Empty);
        }

        /// <summary>
        /// GP-W5. The player-facing half of "a wreck is repairable". The sim could always repair a machine; nothing
        /// ever SHOWED it, so a flattened turret read "disabled (0 hp)" and looked permanent — and the cheapest
        /// answer to something permanent is to bulldoze it and rebuild, which is the expensive, wrong one.
        ///
        /// The card is asserted as the PLAYER READS IT, one sentence at a time, because every number on it is a
        /// decision: the price of one instalment, what that instalment buys, and — the part a single price hides —
        /// how many instalments a wreck actually takes. It also carries the sim's OWN refusal, so a greyed button
        /// can never drift away from what <see cref="RepairCommand"/> would say if it were pressed.
        ///
        /// The last case is the boundary: a walk-through pole has no integrity row at all, so it has no card, no
        /// sentence and nothing to repair — a machine that cannot be broken must not be advertised as mendable.
        /// </summary>
        [Test]
        public void AWreckedMachineOffersARepairCardPricedInInstalmentsAndCarryingTheSimsOwnRefusal()
        {
            var ctx = RaidFixture.Context();
            var st = State(ctx);
            var t = RaidFixture.Turret(ctx, st, RaidFixture.CoreX + 4, RaidFixture.CoreY + 4);
            var d = ctx.Data.Defence;

            // Whole: no card, no sentence. A healthy machine is not sold a repair.
            var whole = HomeQueries.MachineRepairCard(ctx, st, t.Id);
            Assert.That(whole.Max, Is.EqualTo(100), "the turret's integrity row, straight from the catalogue");
            Assert.That(whole.Damaged, Is.False);
            Assert.That(whole.RepairsToFull, Is.Zero);
            Assert.That(whole.Problem, Is.EqualTo("nothing damaged here"));
            Assert.That(HomeQueries.MachineRepairLine(ctx, st, t.Id), Is.Empty);

            TurretRules.Damage(ctx, st, t, whole.Max);
            Assert.That(TurretRules.Wrecked(ctx.Data, st, t), Is.True);

            // Wrecked and penniless: the price is quoted anyway, and the refusal names what is missing.
            var broke = HomeQueries.MachineRepairCard(ctx, st, t.Id);
            Assert.That(broke.Wrecked, Is.True);
            Assert.That(broke.Hp, Is.Zero);
            Assert.That(broke.Steel, Is.EqualTo(d.RepairSteel));
            Assert.That(broke.Copper, Is.EqualTo(d.RepairCopper));
            Assert.That(broke.Seconds, Is.EqualTo(d.RepairSeconds));
            Assert.That(broke.RepairHp, Is.EqualTo(d.RepairHp));
            Assert.That(broke.RepairsToFull, Is.EqualTo(3), "100 HP restored 40 at a time is three payments");
            Assert.That(broke.InReach, Is.True, "the engineer is standing at it");
            Assert.That(broke.CanAfford, Is.False);
            Assert.That(broke.Problem, Is.EqualTo("repairing needs 2 steel and 1 copper"));
            Assert.That(HomeQueries.MachineRepairLine(ctx, st, t.Id), Is.EqualTo(
                "DESTROYED — repairable: 2 steel + 1 copper restores 40 HP (4s) · 3 repairs to full"
                + " · repairing needs 2 steel and 1 copper"));

            st.Engineer.Inv[ItemId.Steel] = 10;
            st.Engineer.Inv[ItemId.Copper] = 10;
            st.Ledger = Ledger.Open(st, ctx.Data);            // opened once the fixture's stock is on the map

            // Out of reach: repairs happen AT the machine, and the card says so rather than greying silently.
            var here = st.Engineer.Pos;
            st.Engineer.Pos = new Vec2(5.5, 5.5);
            var far = HomeQueries.MachineRepairCard(ctx, st, t.Id);
            Assert.That(far.CanAfford, Is.True, "the money is not the problem now");
            Assert.That(far.InReach, Is.False);
            Assert.That(far.Problem, Is.EqualTo("walk closer to repair"));
            Assert.That(HomeQueries.MachineRepairLine(ctx, st, t.Id), Does.EndWith(" · walk closer to repair"));

            // In reach and paid for: no refusal at all, which is what un-greys the button.
            st.Engineer.Pos = here;
            Assert.That(HomeQueries.MachineRepairCard(ctx, st, t.Id).Problem, Is.Empty);
            Assert.That(HomeQueries.MachineRepairLine(ctx, st, t.Id), Is.EqualTo(
                "DESTROYED — repairable: 2 steel + 1 copper restores 40 HP (4s) · 3 repairs to full"));

            var r = Apply(ctx, st, new RepairCommand(RepairKinds.Machine, t.Id));
            Assert.That(r.Problem, Is.Empty, "the repair starts: " + r.Problem);
            var running = HomeQueries.MachineRepairCard(ctx, st, t.Id);
            Assert.That(running.InProgress, Is.True);
            Assert.That(running.Problem, Is.Empty, "a repair under way has nothing to refuse");
            Assert.That(HomeQueries.MachineRepairLine(ctx, st, t.Id),
                Is.EqualTo("Repairing — 4s left (stay in reach)"));

            // One instalment buys exactly one instalment, and the sentence counts down with it.
            RunHome(ctx, st, d.RepairSeconds);
            Assert.That(TurretRules.Wrecked(ctx.Data, st, t), Is.False, "it is a machine again, not rubble");
            Assert.That(TurretRules.Hp(ctx.Data, st, t), Is.EqualTo((double)d.RepairHp).Within(1e-9));
            Assert.That(HomeQueries.MachineRepairLine(ctx, st, t.Id), Is.EqualTo(
                "Damaged 40/100 HP — repairable: 2 steel + 1 copper restores 40 HP (4s) · 2 repairs to full"));
            Assert.That(Ledger.Conservation(st, ctx.Data).Problems, Is.Empty);

            // And the boundary: a walk-through pole has no integrity, so it is never offered a repair.
            var pole = RaidFixture.Add(ctx, st, "pole", RaidFixture.CoreX + 6, RaidFixture.CoreY + 4);
            var none = HomeQueries.MachineRepairCard(ctx, st, pole.Id);
            Assert.That(none.Max, Is.Zero, "a raid can never stall itself chewing on a 2-item pole");
            Assert.That(none.Damaged, Is.False);
            Assert.That(HomeQueries.MachineRepairLine(ctx, st, pole.Id), Is.Empty);
        }
    }
}
