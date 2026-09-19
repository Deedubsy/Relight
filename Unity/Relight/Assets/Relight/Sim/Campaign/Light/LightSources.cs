using System.Collections.Generic;

namespace Relight.Sim
{
    /// <summary>
    /// Everything that can light a tile this tick, ported from reference flow.ts <c>blockLights</c> (:1799).
    ///
    /// Deliberate differences, all from the retired block economy (U-D-32):
    /// <list type="bullet">
    /// <item>There are no blocks, so the sources are collected for the whole map in one pass rather than per block.
    ///       <c>lightingMachines</c>' per-revision WeakMap cache is unnecessary for the same reason: the mask itself
    ///       is cached (<see cref="LightPhase.Ensure"/>) and this pass runs once per tick, not once per block.</item>
    /// <item>No broken/eaten lights and no contested switch-on sequence (<c>why</c>, <c>inSeq</c>): both are block
    ///       lifecycle, which the port does not have. Every authored light is intact.</item>
    /// <item>A streetlight's <c>on</c> is <c>subPowered</c> in the reference — the block is Held and its substation
    ///       throttle is above zero. Here it is the throttle of the circuit the light joined (see
    ///       <see cref="StreetLights"/>), which is the same question asked of the port's grid.</item>
    /// <item>A Lamp's <c>lit</c> is the reference's <c>running(st, m)</c>. A lamp is not a processor in the port, so
    ///       "running" is exactly "its circuit is delivering": <see cref="PowerQueries.Supplied"/>.</item>
    /// <item>A powered light's radius is scaled by LightRules.BrownoutScale of its circuit's throttle (U-D-58); the
    ///       reference lights at full radius whenever the throttle is above zero.</item>
    /// </list>
    /// Radii and cone angles come from <see cref="MachineSpec"/> (<c>LightRadiusTiles</c>, <c>ConeRangeTiles</c>,
    /// <c>ConeHalfAngleRad</c>), so the reference's hard-coded LAMP_RADIUS 4 / ARC_LAMP_RADIUS 6 / FLOODLIGHT_RANGE 12
    /// and π/6 arrive as data instead of constants.
    /// </summary>
    public static class LightSources
    {
        /// <summary>True for a machine kind that emits light at all (lamp, arc lamp, floodlight), lit or not.</summary>
        public static bool IsLightMachine(GameData d, Machine m) =>
            m != null && d != null && d.TryMachine(m.Kind, out var spec) &&
            (spec.LightRadiusTiles > 0 || spec.ConeRangeTiles > 0);

        /// <summary>
        /// Collect every light source on the map into <paramref name="into"/> (cleared first), lit and unlit, in a
        /// deterministic order: the authored streetlights in export order, then the machines in placement order.
        /// </summary>
        public static void Collect(SimContext ctx, SimState st, List<Light> into)
        {
            into.Clear();
            if (ctx == null || st == null) return;
            var d = ctx.Data;

            var sites = StreetLights.Sites(ctx);
            if (sites.Count > 0)
            {
                var grid = PowerGrid.Of(ctx, st);
                var r = StreetLights.RadiusTiles(d);
                for (var i = 0; i < sites.Count; i++)
                {
                    var s = sites[i];
                    var c = grid.OfSite(s.Id);
                    var throttle = c != null ? c.Throttle : 0;
                    var on = throttle > 0;                     // flow.ts:620 subPowered
                    into.Add(new Light(s.X, s.Y, r * LightRules.BrownoutScale(throttle), LightKind.StreetLight, on));
                }
            }

            for (var i = 0; i < st.Machines.Count; i++)
            {
                var m = st.Machines[i];
                if (!d.TryMachine(m.Kind, out var spec)) continue;
                if (spec.LightRadiusTiles > 0 || spec.ConeRangeTiles > 0)
                {
                    var throttle = PowerQueries.Throttle(ctx, st, m.Id);
                    var scale = LightRules.BrownoutScale(throttle);
                    if (spec.LightRadiusTiles > 0)
                    {
                        // flow.ts:1812 — a Lamp or Arc lamp sits on its tile index and lights a disc while it is running.
                        into.Add(new Light(m.X, m.Y, spec.LightRadiusTiles * scale, LightKind.Lamp,
                            throttle > 0, m.Dir, 0, m.Id));
                    }
                    else if (spec.ConeRangeTiles > 0)
                    {
                        // flow.ts:1814 — a Floodlight throws its cone from the footprint centre along its facing.
                        var (w, h) = m.Dimensions;
                        into.Add(new Light(m.X + w / 2.0, m.Y + h / 2.0, spec.ConeRangeTiles * scale, LightKind.Floodlight,
                            throttle > 0, m.Dir, spec.ConeHalfAngleRad, m.Id));
                    }
                }
            }
        }

        /// <summary>
        /// A value that changes whenever the lit picture would change: which sources exist, where they are, how far
        /// they reach and whether they are burning. The mask is re-stamped only when this moves, which is the port's
        /// answer to the reference recomputing <c>lightMask</c> every frame.
        /// </summary>
        public static int Fold(List<Light> lights)
        {
            var h = 17;
            unchecked
            {
                h = h * 31 + lights.Count;
                for (var i = 0; i < lights.Count; i++)
                {
                    var l = lights[i];
                    h = h * 31 + l.Tx.GetHashCode();
                    h = h * 31 + l.Ty.GetHashCode();
                    h = h * 31 + l.R.GetHashCode();
                    h = h * 31 + (int)l.Kind;
                    h = h * 31 + (int)l.Dir;
                    h = h * 31 + l.HalfAngleRad.GetHashCode();
                    h = h * 31 + (l.Lit ? 1 : 0);
                }
            }
            return h;
        }
    }
}
