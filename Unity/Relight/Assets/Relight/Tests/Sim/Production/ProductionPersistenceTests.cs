using System.Collections.Generic;
using NUnit.Framework;
using Relight.Sim.Tests.Persistence;

namespace Relight.Sim.Tests.Production
{
    /// <summary>
    /// The two things the Phase C state contract asks of a new sub-state: a world with production and power history
    /// survives a save exactly, and a save written before those members existed still loads.
    ///
    /// The side tables are why both hold. Burn progress and craft progress are keyed by machine id in
    /// <c>power</c> / <c>production</c> rather than carried on <see cref="Machine"/>, because
    /// <see cref="SaveUpgrade"/> fills <i>missing object members</i> from a fresh state and cannot add members
    /// inside the elements of the <c>machines</c> array.
    /// </summary>
    public sealed class ProductionPersistenceTests
    {
        /// <summary>A world part-way through a craft and part-way through a coal: both side tables are non-empty.</summary>
        private static (SimContext Ctx, SimState St, Machine Foundry, Machine Gen) Played()
        {
            var ctx = ProductionFixture.Context();
            var st = ProductionFixture.State(ctx);
            var (_, gen) = ProductionFixture.Grid(ctx, st, 20, 18, 18, 14, 5);
            var foundry = ProductionFixture.Add(ctx, st, "foundry", 14, 17, recipe: "steel-plates");
            foundry.Inv.Add(ItemId.IronOre, 20);
            var asm = ProductionFixture.Add(ctx, st, "assembler", 24, 17);
            ProductionFixture.Seal(ctx, st);

            st.Engineer.Pos = new Vec2(asm.X - 1, asm.Y);
            new SetRecipeHandler().TryApply(ctx, st, new SetRecipeCommand(asm.Id, "shell"), out var configured);
            Assert.That(configured.Accepted, Is.True, configured.Problem);
            ProductionFixture.Run(ctx, st, 25);

            Assert.That(st.Power.Burn, Is.Not.Empty, "the generator has burn progress to save");
            Assert.That(st.Power.Burn[0].Timer, Is.GreaterThan(0));
            Assert.That(st.Power.Supplied, Is.Not.Empty, "the supplied set is what stops a spurious outage on load");
            Assert.That(st.Production.Work, Is.Not.Empty);
            Assert.That(st.Production.Of(foundry.Id).Busy, Is.True);
            return (ctx, st, foundry, gen);
        }

        [Test]
        public void AWorldWithCraftAndBurnProgressSurvivesASaveExactly()
        {
            var (ctx, st, foundry, gen) = Played();
            var before = CanonicalJsonWriter.Write(st);

            var doc = SaveSerializer.WriteText(st, ctx.Data);
            var loaded = SaveSerializer.ReadText(doc, ctx.Data);

            Assert.That(loaded.Ok, Is.True, loaded.Reason);
            Assert.That(loaded.Upgraded, Is.Null, "a current file needs no upgrade");
            Assert.That(CanonicalJsonWriter.Write(loaded.State), Is.EqualTo(before));
            Assert.That(StateHash.Compute(loaded.State), Is.EqualTo(StateHash.Compute(st)));

            var back = loaded.State;
            Assert.That(back.Power.BurnOf(gen.Id).Timer, Is.EqualTo(st.Power.BurnOf(gen.Id).Timer).Within(1e-12));
            Assert.That(back.Power.Supplied, Is.EqualTo(st.Power.Supplied));
            Assert.That(back.Production.Of(foundry.Id).Timer, Is.EqualTo(st.Production.Of(foundry.Id).Timer).Within(1e-12));
            Assert.That(back.Production.Of(foundry.Id).Busy, Is.True);
            Assert.That(ProductionQueries.Recipe(ctx, back, back.Machines[back.Machines.Count - 1].Id).Key, Is.EqualTo("shell"),
                "the chosen recipe is saved with the work entry, not on the machine");

            // The restored world carries on where it left off: the same tick count finishes the same craft.
            ProductionFixture.Run(ctx, back, 36);
            Assert.That(back.MachineById(foundry.Id).Inv[ItemId.Steel], Is.EqualTo(1));
            Assert.That(ProductionFixture.Count<PowerOutageEvent>(back), Is.Zero, "supply was already recorded, so nothing was lost");
        }

        /// <summary>
        /// The v2 → v3 smoke: a document whose state has no <c>power</c> and no <c>production</c> member — which is
        /// every save written before this task — loads, is reported as upgraded, and comes back with both
        /// sub-states at their "nothing has happened yet" defaults. The forging idiom is
        /// <c>Tests/Sim/Persistence/SaveUpgradeTests.cs</c>'s: prune the state text, set the versions back and
        /// recompute the checksum over the forged text, so the old checksum is verified as an old build wrote it.
        /// </summary>
        [Test]
        public void ASaveWrittenBeforeTheseMembersExistedStillLoads()
        {
            var (ctx, st, foundry, _) = Played();
            var doc = SaveSerializer.WriteText(st, ctx.Data);

            var state2 = WithoutMembers(JsonValue.Parse(CanonicalJsonWriter.Write(st), out var e), "power", "production");
            Assert.That(e, Is.Null);
            Assert.That(state2, Does.Not.Contain("\"power\""));
            Assert.That(state2, Does.Not.Contain("\"production\""));

            var v2 = PersistenceFixture.Retarget(doc, "state", state2);
            v2 = PersistenceFixture.Retarget(v2, "version", "2");
            v2 = PersistenceFixture.Retarget(v2, "hash", CanonicalJsonWriter.QuoteString(StateHash.Of(state2)));

            var loaded = SaveSerializer.ReadText(v2, ctx.Data);
            Assert.That(loaded.Ok, Is.True, loaded.Reason);
            Assert.That(loaded.Upgraded, Is.Not.Null.And.Not.Empty, "the load says what it defaulted");
            Assert.That(loaded.State.Version, Is.EqualTo(SaveSchema.Version));
            Assert.That(loaded.State.Power.Burn, Is.Empty);
            Assert.That(loaded.State.Power.Supplied, Is.Empty);
            Assert.That(loaded.State.Production.Work, Is.Empty);
            Assert.That(loaded.State.Machines.Count, Is.EqualTo(st.Machines.Count), "the machines themselves came through");

            // The upgraded world ticks without a spurious outage (U-D-12).
            var back = loaded.State;
            ProductionFixture.Run(ctx, back, 61);
            Assert.That(ProductionFixture.Count<PowerOutageEvent>(back), Is.Zero, "nothing was supplied before, so nothing was lost");

            // U-D-53's consequence for an old save, stated rather than hidden: the dropped side table was the ONLY
            // record of what the Foundry was making, and the catalogue no longer supplies a fallback, so the
            // machine comes back idle instead of silently resuming a recipe the player never chose. Choosing one
            // starts it, which is the whole recovery.
            Assert.That(back.MachineById(foundry.Id).Inv[ItemId.Steel], Is.Zero, "no recipe survived, so nothing was smelted");
            Assert.That(ProductionQueries.OperatingState(ctx, back, foundry.Id), Is.EqualTo(MachineOperatingState.Idle));
            ProductionFixture.Recipe(ctx, back, back.MachineById(foundry.Id), "steel-plates");
            ProductionFixture.Run(ctx, back, 41);
            Assert.That(back.MachineById(foundry.Id).Inv[ItemId.Steel], Is.EqualTo(1), "a craft restarted from the beginning");
        }

        /// <summary>The canonical state text with some top-level members left out, and its own version set back to 2.</summary>
        private static string WithoutMembers(JsonValue state, params string[] drop)
        {
            Assert.That(state, Is.Not.Null);
            Assert.That(state.IsObject, Is.True);
            var parts = new List<string>();
            foreach (var key in state.Keys)
            {
                if (System.Array.IndexOf(drop, key) >= 0) continue;
                var text = string.CompareOrdinal(key, "version") == 0 ? "2" : state.Member(key).ToCanonicalJson();
                parts.Add(CanonicalJsonWriter.QuoteString(key) + ":" + text);
            }
            return "{" + string.Join(",", parts) + "}";
        }
    }
}
