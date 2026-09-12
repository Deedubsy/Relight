namespace Relight.Sim
{
    /// <summary>
    /// What survives of the reference's 1 Hz block tick (sim.ts <c>step</c>, lines 793–1085).
    ///
    /// Almost all of <c>step</c> is the retired block economy — contested/held transitions, rot and blooms, the ring
    /// ammunition distribution, well death, the lattice and the <c>hourRow</c> sample, every field of which
    /// (<c>held</c>, <c>front</c>, <c>interior</c>, <c>mags</c>, <c>shells</c>, <c>meanAwakeRot</c>,
    /// <c>production</c>) is computed from <c>st.blocks</c>, <c>st.ring</c> and <c>st.stock</c>. All of it is
    /// excluded by CONTENT_CATALOGUE.md §17, so no hourly row is recorded here.
    ///
    /// What is left is the per-second hook and its telemetry: <c>threatHooks.current.second(st)</c> (sim.ts:940) and
    /// the danger counters it feeds (sim.ts:941).
    ///
    /// TIMING. The reference runs <c>step</c> after the tile tick whose counter is a multiple of 20
    /// (flow.ts:1117 — <c>stepFlow; f.tick++; if (f.tick % TILE_TPS === 0) step(st)</c>), i.e. between the 20th and
    /// the 21st tile tick. This phase is registered first in <see cref="SimComposition.Phases"/> and the driver
    /// increments <c>Tick</c> after the phases, so firing when <c>Tick &gt; 0 &amp;&amp; Tick % 20 == 0</c> puts it in
    /// exactly that gap. The <c>Tick &gt; 0</c> guard is what stops it firing before the very first tile tick.
    /// </summary>
    public sealed class SecondPhase : ITickPhase
    {
        public void Tick(SimContext ctx, SimState st, double dt)
        {
            if (st.Tick <= 0 || st.Tick % Simulation.TicksPerSecond != 0) return;
            var r = ctx.Threat.Second(st);
            if (!r.danger) return;
            st.Engineer.DangerSeconds++;
            if (r.shot) st.Engineer.DangerShotSeconds++;
        }
    }
}
