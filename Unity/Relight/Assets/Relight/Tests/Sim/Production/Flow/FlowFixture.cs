using System.Collections.Generic;

namespace Relight.Sim.Tests.Production
{
    /// <summary>
    /// B-10's setup: the same synthetic map and reference catalogue <see cref="ProductionFixture"/> uses, with the
    /// Flow phase added after the machine phase so the checks run the real production sub-slot order
    /// (Power -> Machines -> Flow, SimComposition.Production.cs).
    /// Machines are added straight to the state for the same reason the production fixture does it: placement
    /// charges build costs and validates terrain, neither of which is what a belt test is about.
    /// </summary>
    public static class FlowFixture
    {
        public const double Dt = ProductionFixture.Dt;

        public static SimContext Context() => ProductionFixture.Context();

        public static SimState State(SimContext ctx) => ProductionFixture.State(ctx);

        public static Machine Add(SimContext ctx, SimState st, string kind, int x, int y, Dir dir = Dir.N) =>
            ProductionFixture.Add(ctx, st, kind, x, y, dir);

        public static void Seal(SimContext ctx, SimState st) => ProductionFixture.Seal(ctx, st);

        public static string Off(SimContext ctx, SimState st) => ProductionFixture.Off(ctx, st);

        /// <summary>Power, machines, then flow — the production sub-slot order, and nothing else.</summary>
        public static void Run(SimContext ctx, SimState st, int ticks)
        {
            var phases = new List<ITickPhase> { new PowerPhase(), new MachinePhase(), new FlowPhase() };
            for (var i = 0; i < ticks; i++)
            {
                for (var p = 0; p < phases.Count; p++) phases[p].Tick(ctx, st, Dt);
                st.Tick++;
                st.T += Dt;
            }
        }

        /// <summary>Ticks until <paramref name="done"/> or <paramref name="limit"/> ticks; returns the ticks taken (-1 if it never happened).</summary>
        public static int RunUntil(SimContext ctx, SimState st, int limit, System.Func<bool> done)
        {
            for (var i = 0; i < limit; i++)
            {
                if (done()) return i;
                Run(ctx, st, 1);
            }
            return done() ? limit : -1;
        }

        /// <summary>Every item anywhere: machine inventories, turret rounds and belt lanes.</summary>
        public static double TotalItems(SimState st, ItemId k)
        {
            double n = 0;
            for (var i = 0; i < st.Machines.Count; i++) n += st.Machines[i].Inv[k];
            var belts = new ItemCounts();
            FlowQueries.HeldItems(st, belts);
            return n + belts[k];
        }
    }
}
