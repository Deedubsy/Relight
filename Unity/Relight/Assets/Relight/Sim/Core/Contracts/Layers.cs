namespace Relight.Sim
{
    /// <summary>
    /// The explicit replacement for the reference's module-global <c>tileHooks.current</c> (sim.ts) — the tile
    /// production layer as seen by the core. Implemented by the production subsystem (Phase C); Phase B ships a
    /// no-op implementation so the seam exists and is exercised.
    /// </summary>
    public interface ITileLayer
    {
        /// <summary>Called after every accepted command (reference `tileHooks.afterCommand`).</summary>
        void AfterCommand(SimContext ctx, SimState st, Command c);
        double SupplyKw(SimState st);
        double DemandKw(SimState st);
    }

    /// <summary>
    /// The explicit replacement for <c>threatHooks.current</c> (engineer.ts / threat.ts). Phase B ships a no-op.
    /// </summary>
    public interface IThreatLayer
    {
        void Tick(SimContext ctx, SimState st, double dt);
        /// <summary>Danger this second (an enemy within reach of the engineer) and whether the weapon fired in it.</summary>
        (bool danger, bool shot) Second(SimState st);
    }

    public sealed class NoTileLayer : ITileLayer
    {
        public void AfterCommand(SimContext ctx, SimState st, Command c) { }
        public double SupplyKw(SimState st) => 0;
        public double DemandKw(SimState st) => 0;
    }

    public sealed class NoThreatLayer : IThreatLayer
    {
        public void Tick(SimContext ctx, SimState st, double dt) { }
        public (bool danger, bool shot) Second(SimState st) => (false, false);
    }
}
