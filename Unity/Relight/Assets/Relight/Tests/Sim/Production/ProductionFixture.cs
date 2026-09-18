using System.Collections.Generic;
using NUnit.Framework;

namespace Relight.Sim.Tests.Production
{
    /// <summary>
    /// The shared setup for the B-07/B-09 checks: the synthetic map, the reference catalogue, machines placed by
    /// hand and only the two phases under test ticked.
    ///
    /// Machines are added straight to <see cref="SimState.Machines"/> rather than through <c>Placement</c> on
    /// purpose. Placement is another worker's file, it charges build costs out of the pockets and it validates
    /// terrain; none of that is what these tests are about, and going through it would make a production failure
    /// indistinguishable from a placement one. The ledger is opened (<see cref="Seal"/>) after the world is set up,
    /// so every check can still assert conservation over what production itself did.
    /// </summary>
    public static class ProductionFixture
    {
        public const double Dt = 1.0 / 20;

        public static SimContext Context() => new SimContext(ReferenceData.Create(), SyntheticMap.Create());

        public static SimState State(SimContext ctx)
        {
            var st = new SimState();
            st.Engineer.Pos = ctx.Geometry.Spawn;
            st.Engineer.Hp = ctx.Data.Engineer.MaxHp;
            return st;
        }

        /// <summary>
        /// Puts a machine of <paramref name="kind"/> on the map at its catalogue size, and chooses
        /// <paramref name="recipe"/> on it when the test needs one.
        /// </summary>
        public static Machine Add(SimContext ctx, SimState st, string kind, int x, int y, Dir dir = Dir.N, string recipe = null)
        {
            Assert.That(ctx.Data.TryMachine(kind, out var spec), Is.True, kind + " is not in the catalogue");
            var m = new Machine { Id = st.NextId++, Kind = kind, X = x, Y = y, Dir = dir, Size = spec.Size };
            st.Machines.Add(m);
            st.Rev++;
            if (recipe != null) Recipe(ctx, st, m, recipe);
            return m;
        }

        /// <summary>
        /// Chooses a machine's recipe the way <see cref="SetRecipeHandler"/> records it, without the reach check or
        /// the refund a live switch does. U-D-53 cleared the data default on the Foundry and both Assemblers, so a
        /// test about the production loop has to say what the machine is making; what it makes is the player's
        /// choice, and these tests are not about the catalogue.
        /// </summary>
        public static Machine Recipe(SimContext ctx, SimState st, Machine m, string key)
        {
            Assert.That(ProductionRules.Supports(ctx.Data, m, key), Is.True, m.Kind + " cannot run " + key);
            st.Production.Of(m.Id).Recipe = key;
            return m;
        }

        /// <summary>Opens the ledger over the world as it now stands, so conservation reads zero from here on.</summary>
        public static void Seal(SimContext ctx, SimState st) => st.Ledger = Ledger.Open(st, ctx.Data);

        /// <summary>Ticks power then machines — the production sub-slot order, and nothing else.</summary>
        public static void Run(SimContext ctx, SimState st, int ticks)
        {
            var phases = new List<ITickPhase> { new PowerPhase(), new MachinePhase() };
            for (var i = 0; i < ticks; i++)
            {
                for (var p = 0; p < phases.Count; p++) phases[p].Tick(ctx, st, Dt);
                st.Tick++;
                st.T += Dt;
            }
        }

        /// <summary>Every item out of ledger tolerance, as text — empty when production explained every unit.</summary>
        public static string Off(SimContext ctx, SimState st) =>
            string.Join(" | ", Ledger.Conservation(st, ctx.Data).Problems);

        /// <summary>How many events of a kind the run raised.</summary>
        public static int Count<T>(SimState st) where T : SimEvent
        {
            var n = 0;
            for (var i = 0; i < st.Events.Count; i++) if (st.Events[i] is T) n++;
            return n;
        }

        /// <summary>A power/production world: a fuelled generator, a pole and whatever the test hangs off it.</summary>
        public static (Machine Pole, Machine Gen) Grid(SimContext ctx, SimState st, int poleX, int poleY, int genX, int genY, int coal)
        {
            var pole = Add(ctx, st, "pole", poleX, poleY);
            var gen = Add(ctx, st, "generator", genX, genY);
            if (coal > 0) gen.Inv.Add(ItemId.Coal, coal);
            return (pole, gen);
        }
    }
}
