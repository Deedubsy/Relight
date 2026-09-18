using NUnit.Framework;
using Relight.Sim.Tests.Combat;

namespace Relight.Sim.Tests.Campaign
{
    /// <summary>
    /// Phase C integrated run (2026-09-14): an assembler fed by belt into a turret delivered 40 rounds while
    /// <c>ProducedAt</c> and <c>SuppliedAt</c> stayed -1, because the opening read the retired <c>Machine.Out</c>
    /// field, which nothing writes. These tests drive the real production, flow and opening phases together, the
    /// way the game does, so the check can only pass by reading what the machines actually do.
    /// </summary>
    [TestFixture]
    public sealed class OpeningSupplyTests
    {
        /// <summary>
        /// A powered assembler on <c>bullet-batch</c>, a belt into the turret, the whole composition ticking: the
        /// first finished batch records production and the first automated delivery records the supply chain.
        /// </summary>
        [Test]
        public void ARealAssemblerBeltAndTurretRecordProductionAndSupply()
        {
            var ctx = OpeningFixture.Context();
            var st = OpeningFixture.State(ctx);
            var phases = new System.Collections.Generic.List<ITickPhase>(SimComposition.Phases);

            var turret = RaidFixture.Turret(ctx, st, RaidFixture.CoreX + 6, RaidFixture.CoreY, rounds: 0);
            var asm = RaidFixture.Add(ctx, st, "assembler", turret.X - 4, turret.Y);      // 3x3: x-4..x-2
            st.Production.Of(asm.Id).Recipe = "bullet-batch";
            asm.Inv.Add(ItemId.Steel, 20);
            asm.Inv.Add(ItemId.Copper, 10);
            RaidFixture.Add(ctx, st, "belt", turret.X - 1, turret.Y, Dir.E);             // assembler -> turret
            st.Rev++;

            Assert.That(st.Opening.ProducedAt, Is.LessThan(0));
            Assert.That(st.Opening.SuppliedAt, Is.LessThan(0));

            OpeningFixture.Seconds(ctx, st, 30, phases);   // 6 s per batch; a belt tile carries a round in well under a second

            Assert.That(st.Stats.MagsMade, Is.GreaterThan(0), "the assembler must actually have produced");
            Assert.That(st.Stats.TurretFed, Is.GreaterThan(0), "the belt must actually have fed the turret");
            Assert.That(turret.Rounds, Is.GreaterThan(0));
            Assert.That(st.Opening.ProducedAt, Is.GreaterThanOrEqualTo(0), "the first batch is recorded even though the belt drains the buffer");
            Assert.That(st.Opening.SuppliedAt, Is.GreaterThanOrEqualTo(0), "the automated feed path is recognised");
            Assert.That(RaidFixture.Count<ResupplyWorkingEvent>(st), Is.EqualTo(1));
        }

        /// <summary>
        /// U-D-53, the escape hatch. A player who keeps the turret topped up by hand leaves the belt nothing to
        /// insert: <see cref="TurretHopper.Give"/> returns 0 without touching <c>Stats.TurretFed</c>, so watching
        /// that counter alone means "Automatic resupply working" never arrives however correct the line is. The
        /// chain is recognised anyway once the only thing stopping delivery is a full hopper.
        /// </summary>
        [Test]
        public void AFullTurretStillRecordsTheSupplyChainThatCannotReachIt()
        {
            var ctx = OpeningFixture.Context();
            var st = OpeningFixture.State(ctx);
            var phases = new System.Collections.Generic.List<ITickPhase>(SimComposition.Phases);

            var turret = RaidFixture.Turret(ctx, st, RaidFixture.CoreX + 6, RaidFixture.CoreY);
            Assert.That(TurretHopper.Room(ctx, st, turret.Id), Is.Zero, "the fixture turret starts full");

            var asm = RaidFixture.Add(ctx, st, "assembler", turret.X - 4, turret.Y);
            st.Production.Of(asm.Id).Recipe = "bullet-batch";
            asm.Inv.Add(ItemId.Steel, 20);
            asm.Inv.Add(ItemId.Copper, 10);
            RaidFixture.Add(ctx, st, "belt", turret.X - 1, turret.Y, Dir.E);
            st.Rev++;

            OpeningFixture.Seconds(ctx, st, 30, phases);

            Assert.That(st.Stats.MagsMade, Is.GreaterThan(0), "the assembler produced");
            Assert.That(st.Stats.TurretFed, Is.Zero, "and nothing could go in, which is the whole point");
            Assert.That(st.Opening.ProducedAt, Is.GreaterThanOrEqualTo(0));
            Assert.That(st.Opening.SuppliedAt, Is.GreaterThanOrEqualTo(0), "the line is recognised on its merits");
            Assert.That(RaidFixture.Count<ResupplyWorkingEvent>(st), Is.EqualTo(1));
        }

        /// <summary>
        /// The other side of that hatch: a full turret is not a supply chain by itself. Hand loading must still
        /// never record one, so with nothing that could ever feed it the opening stays where it was.
        /// </summary>
        [Test]
        public void AFullTurretWithNothingFeedingItRecordsNothing()
        {
            var ctx = OpeningFixture.Context();
            var st = OpeningFixture.State(ctx);
            var phases = new System.Collections.Generic.List<ITickPhase>(SimComposition.Phases);

            var turret = RaidFixture.Turret(ctx, st, RaidFixture.CoreX + 6, RaidFixture.CoreY);
            var asm = RaidFixture.Add(ctx, st, "assembler", turret.X - 4, turret.Y);
            st.Production.Of(asm.Id).Recipe = "bullet-batch";     // built and set, but never fed
            RaidFixture.Add(ctx, st, "belt", turret.X - 1, turret.Y, Dir.E);
            st.Rev++;

            OpeningFixture.Seconds(ctx, st, 30, phases);

            Assert.That(st.Opening.ProducedAt, Is.LessThan(0), "nothing was ever made");
            Assert.That(st.Opening.SuppliedAt, Is.LessThan(0));
        }

        /// <summary>The same chain never fires when the assembler has nothing to make (no ingredients, no output).</summary>
        [Test]
        public void AnIdleAssemblerRecordsNothing()
        {
            var ctx = OpeningFixture.Context();
            var st = OpeningFixture.State(ctx);
            var phases = new System.Collections.Generic.List<ITickPhase>(SimComposition.Phases);

            var turret = RaidFixture.Turret(ctx, st, RaidFixture.CoreX + 6, RaidFixture.CoreY, rounds: 0);
            var asm = RaidFixture.Add(ctx, st, "assembler", turret.X - 4, turret.Y);
            st.Production.Of(asm.Id).Recipe = "bullet-batch";
            RaidFixture.Add(ctx, st, "belt", turret.X - 1, turret.Y, Dir.E);
            st.Rev++;

            OpeningFixture.Seconds(ctx, st, 10, phases);

            Assert.That(st.Opening.ProducedAt, Is.LessThan(0));
            Assert.That(st.Opening.SuppliedAt, Is.LessThan(0));
        }
    }
}
