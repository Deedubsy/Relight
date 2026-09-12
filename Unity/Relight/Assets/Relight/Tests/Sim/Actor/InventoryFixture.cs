using System.Collections.Generic;

namespace Relight.Sim.Tests
{
    /// <summary>
    /// A flat 32×32 street map with open sight, as brief B-06 §11 prescribes. Self-sufficient: these tests do not
    /// depend on the concurrently written world (B-08) or driver (B-03) code.
    /// </summary>
    public sealed class FlatGeometry : ICityGeometry
    {
        public int Width => 32;
        public int Height => 32;
        public bool Solid(int x, int y) => false;
        public TileClass TileAt(int x, int y) =>
            x < 0 || y < 0 || x >= Width || y >= Height ? TileClass.Void : TileClass.Street;
        public Vec2 Spawn => new Vec2(16, 16);
        public bool Sight(double x0, double y0, double x1, double y1) => true;
    }

    /// <summary>Builds a fresh Phase B state through the registered inventory initialiser and ticks it by hand.</summary>
    public static class Fixture
    {
        public const double Dt = 1.0 / 20;

        public static SimContext Context() => new SimContext(ReferenceData.Create(), new FlatGeometry());

        /// <summary>A state with the campaign opening Backpack and an opened ledger, the engineer at the spawn.</summary>
        public static SimState State(SimContext ctx)
        {
            var st = new SimState();
            st.Engineer.Pos = ctx.Geometry.Spawn;
            st.Engineer.Hp = ctx.Data.Engineer.MaxHp;
            new InventoryInitializer().Init(ctx, st);
            return st;
        }

        /// <summary>Runs only the inventory phases — the other subsystems are other workers' and not needed here.</summary>
        public static void Run(SimContext ctx, SimState st, double seconds)
        {
            var phases = new List<ITickPhase> { new HandCraftPhase() };
            var ticks = (int)System.Math.Round(seconds / Dt);
            for (var i = 0; i < ticks; i++)
            {
                for (var p = 0; p < phases.Count; p++) phases[p].Tick(ctx, st, Dt);
                st.Tick++;
                st.T += Dt;
            }
        }

        /// <summary>Applies a command through the inventory handlers in composition order.</summary>
        public static CommandResult Apply(SimContext ctx, SimState st, Command c)
        {
            var handlers = new List<ICommandHandler>
            {
                new InventoryCommandHandler(), new HandCraftHandler(), new MachineTransferHandler(), new PlacementHandler(),
            };
            for (var i = 0; i < handlers.Count; i++)
                if (handlers[i].TryApply(ctx, st, c, out var r)) return r;
            return CommandResult.Refuse("no handler");
        }

        /// <summary>Every item that is out of ledger tolerance, as "k v" text — empty when the ledger balances.</summary>
        public static string Off(SimContext ctx, SimState st)
        {
            var c = Ledger.Conservation(st, ctx.Data);
            return string.Join(" | ", c.Problems);
        }

        public static Machine Place(SimContext ctx, SimState st, string kind, int x, int y)
        {
            var r = Placement.Place(ctx, st, kind, x, y, Dir.N);
            NUnit.Framework.Assert.IsTrue(r.ok, $"place {kind}: {r.reason}");
            return st.Machines[st.Machines.Count - 1];
        }
    }
}
