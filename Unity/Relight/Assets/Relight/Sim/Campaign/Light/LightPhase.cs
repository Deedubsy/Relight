using System.Collections.Generic;

namespace Relight.Sim
{
    /// <summary>
    /// Rebuilds the lit-tile mask, ported from reference light.ts <c>lightMask</c>.
    ///
    /// The reference recomputes the whole mask every frame the renderer asks for it. The port cannot: the mask is a
    /// byte per tile over a 900 x 600 map and C-06's enemy hesitation asks <see cref="LightQueries.LitAt"/> once per
    /// enemy per tick. So the mask is cached on <see cref="LightState"/> and re-stamped only when the lit picture
    /// would actually differ — <see cref="SimState.Rev"/> (a placement, removal or rotation) or
    /// <see cref="LightSources.Fold"/> (a light switched on or off, a Floodlight turned). That is a port decision,
    /// not a reference behaviour; <see cref="LightState.Builds"/> counts the real stamping passes so a test can hold
    /// it to it.
    ///
    /// Deliberate differences from <c>lightMask</c>:
    /// <list type="bullet">
    /// <item>No per-block loop (U-D-32): <see cref="LightSources.Collect"/> gathers the map's sources in one pass.</item>
    /// <item>The Home lot is lit <b>always</b>, not only on the synthetic map. The reference gates its last line on
    ///       <c>st.campaign &amp;&amp; !st.city?.mapId</c> and fills <c>G.blocks[homeBlock].tiles</c>; the port has no
    ///       blocks and no second map, and brief C-11 requires "Home lot fully lit always, including upgraded saves".
    ///       The lot is derived as the core footprint inflated by <c>World.MarginTiles</c> on every side — the
    ///       tiles.ts sense of a lot (building plus margin), since no authored lot rectangle survives the import
    ///       (the same gap <c>HandCraft.cs</c> records: "Phase B has no authored HQ lot in the sim").</item>
    /// <item>Solid things block light (L-02, ALWAYS_DARK_SPEC §5.3): the reference stamps whole discs. A Wall going
    ///       up, coming down, being wrecked or being repaired all move <see cref="SimState.Rev"/>
    ///       (the damage and repair hooks in <c>TurretRules</c>), so the mask is already rebuilt whenever an occluder changes.</item>
    /// <item>No night gate. The reference's <c>lightMask</c> and <c>litAt</c> are time-independent; daylight is the
    ///       renderer's tint and is answered separately by <see cref="LightQueries.Daylight"/>.</item>
    /// </list>
    /// </summary>
    public sealed class LightPhase : ITickPhase
    {
        // Scratch, reused across ticks: the source list is rebuilt every tick but never escapes.
        private readonly List<Light> _sources = new List<Light>();

        public void Tick(SimContext ctx, SimState st, double dt)
        {
            Ensure(ctx, st, _sources);
            Relief(ctx, st);
        }

        /// <summary>How long the engineer must wait before stepping into light is worth a second cue (§5.4).</summary>
        public const double EnteredLightEveryS = 10;

        /// <summary>
        /// L-02, ALWAYS_DARK_SPEC §5.4 — the two moments that replace dawn. Both compare this tick with the last,
        /// and the memory is transient: the first tick after a new game or a load only takes the baseline, so a save
        /// made inside a lit district does not replay its sweep on load.
        /// </summary>
        private static void Relief(SimContext ctx, SimState st)
        {
            if (ctx == null || st == null) return;
            var s = st.Light;
            var first = !s.ReliefKnown;
            s.ReliefKnown = true;

            // DistrictLitEvent: a substation site's circuit starts delivering, so the streetlights it owns come on.
            // Once per connection — it is raised on the change, and nothing is raised while the district stays lit.
            var subs = PowerGrid.SubstationSites(ctx);
            if (subs.Count > 0)
            {
                var grid = PowerGrid.Of(ctx, st);
                for (var i = 0; i < subs.Count; i++)
                {
                    var site = subs[i];
                    var c = grid.OfSite(site.Id);
                    var live = c != null && c.Throttle > 0;
                    var was = s.LiveDistricts.Contains(site.Id);
                    if (live == was) continue;
                    if (!live) { s.LiveDistricts.Remove(site.Id); continue; }
                    s.LiveDistricts.Add(site.Id);
                    if (first) continue;
                    var lights = StreetLights.OwnedBy(ctx, site);
                    if (lights > 0) st.Events.Add(new DistrictLitEvent(st.T, site.Id, site.Name, lights, site.Centre.X, site.Centre.Y));
                }
            }

            // EnteredLightEvent: the engineer's own tile goes from unlit to lit, at most once per ten seconds.
            var p = st.Engineer;
            var lit = !p.IsDown && LightQueries.LitAt(st, (int)System.Math.Floor(p.Pos.X), (int)System.Math.Floor(p.Pos.Y));
            if (!first && lit && !s.EngineerLit && st.T - s.EnteredAt >= EnteredLightEveryS)
            {
                s.EnteredAt = st.T;
                st.Events.Add(new EnteredLightEvent(st.T, p.Pos.X, p.Pos.Y));
            }
            s.EngineerLit = lit;
        }

        /// <summary>
        /// Make sure <see cref="LightState.Mask"/> is current for this tick, stamping it again only if the lit
        /// picture changed. Safe to call more than once per tick; the second call is a fold comparison.
        /// </summary>
        public static void Ensure(SimContext ctx, SimState st) => Ensure(ctx, st, new List<Light>());

        private static void Ensure(SimContext ctx, SimState st, List<Light> scratch)
        {
            if (ctx == null || st == null) return;
            var g = ctx.Geometry;
            var tw = g.Width;
            var th = g.Height;
            if (tw <= 0 || th <= 0) return;

            var s = st.Light;
            LightSources.Collect(ctx, st, scratch);
            var fold = LightSources.Fold(scratch);
            if (s.Built && s.Mask != null && s.W == tw && s.H == th && s.BuiltRev == st.Rev && s.BuiltFold == fold) return;

            if (s.Mask == null || s.Mask.Length != tw * th) s.Mask = new byte[tw * th];
            else System.Array.Clear(s.Mask, 0, s.Mask.Length);   // light.ts:23 mask.fill(0)
            s.W = tw;
            s.H = th;

            for (var i = 0; i < scratch.Count; i++)
            {
                var l = scratch[i];
                if (l.Lit) LightRules.Stamp(s.Mask, tw, th, in l, ctx, st);   // light.ts:24, plus §5.3 blocking
            }

            var (lx, ly, lw, lh) = HomeLot(ctx, st);
            if (lw > 0 && lh > 0) LightRules.StampRect(s.Mask, tw, th, lx, ly, lw, lh);   // light.ts:26

            s.Built = true;
            s.BuiltRev = st.Rev;
            s.BuiltFold = fold;
            s.Builds++;
        }

        /// <summary>
        /// The Home lot: the placed core's footprint inflated by <c>World.MarginTiles</c> on every side. Empty
        /// (0,0,0,0) before the core is placed. See the class remarks for why this is a derivation and not authored
        /// data.
        /// </summary>
        public static (int X, int Y, int W, int H) HomeLot(SimContext ctx, SimState st)
        {
            var (x, y, w, h) = HomeQueries.CoreRect(st);
            if (w <= 0 || h <= 0) return (0, 0, 0, 0);
            var m = ctx?.Data?.World?.MarginTiles ?? 0;
            if (m < 0) m = 0;
            return (x - m, y - m, w + 2 * m, h + 2 * m);
        }
    }

    /// <summary>
    /// ALWAYS_DARK_SPEC §5.4: a substation site gained supply and the <paramref name="Lights"/> streetlights it owns
    /// came on. Presentation answers with a sweep of light across the district and a cue. (X, Y) is the site centre.
    /// </summary>
    public sealed record DistrictLitEvent(double T, string SiteId, string Name, int Lights, double X, double Y) : SimEvent(T);

    /// <summary>ALWAYS_DARK_SPEC §5.4: the engineer stepped from unlit ground onto lit ground. At most one per 10 s.</summary>
    public sealed record EnteredLightEvent(double T, double X, double Y) : SimEvent(T);

    /// <summary>Places the core-derived mask before the first tick so a fresh game is never momentarily dark.</summary>
    public sealed class LightInitializer : IStateInitializer
    {
        public void Init(SimContext ctx, SimState st) => LightPhase.Ensure(ctx, st);
    }
}
