using NUnit.Framework;
using Relight.Sim.Tests.Campaign;
using Relight.Sim.Tests.Combat;

namespace Relight.Sim.Tests.Regression
{
    /// <summary>
    /// U-D-55 — the owner reported "Connect your plate production" never completing over a line that works:
    /// Excavator → conveyor → storage → conveyor → Foundry → conveyor → storage. The row's reach test was a single
    /// belt hop, so the Supply chest standing between the two machines answered "chest" where the row wanted
    /// "foundry", and it held forever over a base that was smelting.
    ///
    /// These tests state both halves of the contract, because widening the reach put the U-D-54 shared-chest row at
    /// risk: a buffer chest must be transparent to the chain, AND the deadlock — both belts ending at ONE chest —
    /// must still be caught without accusing that buffer, which looks similar and is the opposite situation.
    ///
    /// Every layout is built on <see cref="OpeningFixture.ToRifle"/>: Generator at x, Pole at x+2, Excavator (3×3)
    /// at x+4, belts east at x+6 and x+7, Supply chest (2×2) at x+8. Belts PULL from the tile behind the arrow
    /// (<see cref="FlowPhase.LoadConveyor"/>), so each belt below is placed one tile beyond the machine it draws from.
    /// </summary>
    [TestFixture]
    public sealed class BufferedLineTests
    {
        private const string ConnectRow = "Connect your plate production";
        private const string DeadlockRow = "Send your ore through the Foundry";

        /// <summary>The rows before the plate line, satisfied: hand-smelting done and the opening ore patch surveyed.</summary>
        private static void Smelted(SimContext ctx, SimState st)
        {
            st.OpeningResourceVersion = 1;
            st.Stats.Made.Add(ItemId.Steel, 5);
            OpeningFixture.ToRifle(ctx, st);
        }

        private static Machine Find(SimState st, string kind)
        {
            for (var i = 0; i < st.Machines.Count; i++)
                if (st.Machines[i].Kind == kind) return st.Machines[i];
            Assert.Fail("the fixture should have placed a " + kind);
            return null;
        }

        /// <summary>A Foundry set to Steel plates and joined to nothing — exactly what <see cref="ConnectRow"/> is for.</summary>
        private static Machine Foundry(SimContext ctx, SimState st, int x, int y)
        {
            var foundry = RaidFixture.Add(ctx, st, "foundry", x, y);
            st.Production.Of(foundry.Id).Recipe = "steel-plates";
            st.Rev++;
            Assert.That(OpeningQueries.Objective(ctx, st).Title, Is.EqualTo(ConnectRow),
                "a Foundry that belts to nothing is where this row starts");
            return foundry;
        }

        /// <summary>
        /// The owner's build. The chest in the middle carries the ore the Foundry eats, which is what a Supply
        /// chest is for — <see cref="FlowRules.Wants"/> accepts anything into one and
        /// <see cref="FlowPhase.SourceCount"/> lets the next belt draw it straight back out.
        /// </summary>
        [Test]
        public void AChestBetweenTheExcavatorAndTheFoundryIsATransparentBuffer()
        {
            var ctx = OpeningFixture.Context();
            var st = OpeningFixture.State(ctx);
            Smelted(ctx, st);
            var x = RaidFixture.CoreX + 10;
            var y = RaidFixture.CoreY;
            Foundry(ctx, st, x + 11, y);                       // 3×3: x+11..x+13

            RaidFixture.Add(ctx, st, "belt", x + 10, y, Dir.E); // draws from the chest (x+8..x+9) → Foundry
            RaidFixture.Add(ctx, st, "belt", x + 14, y, Dir.E); // draws from the Foundry (x+13) → chest B
            RaidFixture.Add(ctx, st, "chest", x + 15, y);
            st.Rev++;

            Assert.That(OpeningQueries.Objective(ctx, st).Title, Is.Not.EqualTo(ConnectRow),
                "Excavator → chest → Foundry → chest is a connected plate line");
        }

        /// <summary>The line the row was written for still completes it, so the widening took nothing away.</summary>
        [Test]
        public void AFoundryBeltedStraightToTheExcavatorStillCompletesTheRow()
        {
            var ctx = OpeningFixture.Context();
            var st = OpeningFixture.State(ctx);
            Smelted(ctx, st);
            var x = RaidFixture.CoreX + 10;
            var y = RaidFixture.CoreY;
            Foundry(ctx, st, x + 3, y + 4);                          // 3×3: x+3..x+5, y+4..y+6

            RaidFixture.Add(ctx, st, "belt", x + 4, y + 3, Dir.S);   // Excavator (y..y+2) → Foundry
            RaidFixture.Add(ctx, st, "belt", x + 3, y + 7, Dir.S);   // Foundry (y+6) → chest
            RaidFixture.Add(ctx, st, "chest", x + 3, y + 8);
            st.Rev++;

            Assert.That(OpeningQueries.Objective(ctx, st).Title, Is.Not.EqualTo(ConnectRow));
        }

        /// <summary>
        /// U-D-54's deadlock, which this pass re-aimed at the chest both machines deliver INTO: the step-4 ore belt
        /// and the Foundry's plate belt end at the same 200-item chest, raw ore fills it and the Foundry stops at
        /// "output full" for the rest of the run. The line is fully connected, which is why the chain has to say
        /// something — the fault is where it joins.
        /// </summary>
        [Test]
        public void TwoBeltsEndingAtOneChestIsStillCalledOut()
        {
            var ctx = OpeningFixture.Context();
            var st = OpeningFixture.State(ctx);
            Smelted(ctx, st);
            var x = RaidFixture.CoreX + 10;
            var y = RaidFixture.CoreY;
            var chest = Find(st, "chest");
            Foundry(ctx, st, x + 8, y + 4);                          // 3×3: x+8..x+10, y+4..y+6

            RaidFixture.Add(ctx, st, "belt", x + 5, y + 3, Dir.S);   // Excavator (y+2) → south, then east
            RaidFixture.Add(ctx, st, "belt", x + 5, y + 4, Dir.E);
            RaidFixture.Add(ctx, st, "belt", x + 6, y + 4, Dir.E);
            RaidFixture.Add(ctx, st, "belt", x + 7, y + 4, Dir.E);   // → Foundry (x+8, y+4)
            RaidFixture.Add(ctx, st, "belt", x + 8, y + 3, Dir.N);   // Foundry (y+4) → north
            RaidFixture.Add(ctx, st, "belt", x + 8, y + 2, Dir.N);   // → the SAME chest (y+1)
            st.Rev++;

            Assert.That(OpeningQueries.Objective(ctx, st).Title, Is.Not.EqualTo(ConnectRow),
                "the ore reaches the Foundry and the plates reach a chest");
            Assert.That(OpeningQueries.Objective(ctx, st).Title, Is.Not.EqualTo(DeadlockRow),
                "and while the shared chest has room the base is working — nothing to say yet");

            chest.Inv.Add(ItemId.IronOre, MachineInventory.ChestCap(ctx.Data));
            st.Rev++;
            Assert.That(OpeningQueries.Objective(ctx, st).Title, Is.EqualTo(DeadlockRow));
        }

        /// <summary>
        /// The trap the re-aiming exists to avoid. Under a transitive reach every chest downstream of the Excavator
        /// is "reached" from it, so judging the deadlock on reachability would have told this player — whose base
        /// runs — to pick up the belt feeding their Foundry. A full buffer is ore waiting its turn.
        /// </summary>
        [Test]
        public void AFullBufferChestOnAWorkingLineIsNotTheDeadlock()
        {
            var ctx = OpeningFixture.Context();
            var st = OpeningFixture.State(ctx);
            Smelted(ctx, st);
            var x = RaidFixture.CoreX + 10;
            var y = RaidFixture.CoreY;
            var buffer = Find(st, "chest");
            Foundry(ctx, st, x + 11, y);

            RaidFixture.Add(ctx, st, "belt", x + 10, y, Dir.E);
            RaidFixture.Add(ctx, st, "belt", x + 14, y, Dir.E);
            RaidFixture.Add(ctx, st, "chest", x + 15, y);
            buffer.Inv.Add(ItemId.IronOre, MachineInventory.ChestCap(ctx.Data));
            st.Rev++;

            var row = OpeningQueries.Objective(ctx, st);
            Assert.That(row.Title, Is.Not.EqualTo(DeadlockRow), "the buffer is doing its job");
            Assert.That(row.Title, Is.Not.EqualTo(ConnectRow));
        }
    }
}
