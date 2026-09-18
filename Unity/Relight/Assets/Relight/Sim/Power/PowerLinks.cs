using System.Collections.Generic;

namespace Relight.Sim
{
    /// <summary>One cable the placement preview should draw: the existing node it reaches and both centres in tiles.</summary>
    public readonly struct PowerLink
    {
        /// <summary>The existing machine (a pole, big pole, substation or generator) at the far end.</summary>
        public readonly int MachineId;
        public readonly double FromX, FromY;
        public readonly double ToX, ToY;

        public PowerLink(int machineId, double fromX, double fromY, double toX, double toY)
        {
            MachineId = machineId; FromX = fromX; FromY = fromY; ToX = toX; ToY = toY;
        }
    }

    /// <summary>
    /// "What would this ghost cable to?" — the placement preview's purple link lines (C-11, brief: call the sim
    /// query, never re-derive the rule in presentation).
    ///
    /// It is the same reference rule the live network uses, reached through the one implementation
    /// <see cref="PowerGrid.NodesLinked"/> (campaignPower.ts:16 <c>nodesLinked</c>, GP-POWER-FIX): two nodes are
    /// cabled when either one's centre is within its own reach of the other's footprint. A ghost with no reach of
    /// its own (a machine rather than a pole) still shows the one node that would own it — the nearest reach node
    /// covering its footprint, campaignPower.ts:41-44 — because that is the link the player is actually looking for.
    /// </summary>
    public static class PowerLinks
    {
        /// <summary>Links a ghost of <paramref name="kind"/> at (<paramref name="x"/>, <paramref name="y"/>) would form.</summary>
        public static IReadOnlyList<PowerLink> At(SimContext ctx, SimState st, string kind, int x, int y,
            Dir dir = Dir.N, int size = 1) =>
            At(ctx, st, kind, x, y, dir, size, new List<PowerLink>());

        /// <summary>As above, into a caller-owned list (the preview runs every mouse move).</summary>
        public static IReadOnlyList<PowerLink> At(SimContext ctx, SimState st, string kind, int x, int y,
            Dir dir, int size, List<PowerLink> into)
        {
            into.Clear();
            if (ctx == null || st == null || string.IsNullOrEmpty(kind)) return into;
            var d = ctx.Data;
            if (!d.TryMachine(kind, out var spec)) return into;

            var (w, h) = Footprints.Dimensions(kind, dir, size);
            if (w <= 0 || h <= 0) return into;
            var cx = x + w / 2.0;
            var cy = y + h / 2.0;
            var reach = spec.ReachTiles;

            if (reach > 0)
            {
                // A node: every existing node it would be cabled to, both directions of the reach test.
                for (var i = 0; i < st.Machines.Count; i++)
                {
                    var m = st.Machines[i];
                    if (m.Kind == kind && m.X == x && m.Y == y) continue;
                    var r = m.Rect;
                    var mr = PowerGrid.ReachOf(d, m);
                    if (mr <= 0 && !PowerGrid.IsSource(d, m) && PowerGrid.DemandKw(d, m) <= 0) continue;
                    if (!PowerGrid.NodesLinked(x, y, w, h, reach, r.X, r.Y, r.W, r.H, mr)) continue;
                    into.Add(new PowerLink(m.Id, cx, cy, r.X + r.W / 2.0, r.Y + r.H / 2.0));
                }
                return into;
            }

            if (spec.PowerKw == 0) return into;

            // A consumer (or a fuelled generator, whose own reach is 0): the nearest reach node that covers it owns
            // it, and that single cable is what the preview draws.
            var nearest = -1;
            var best = double.PositiveInfinity;
            for (var i = 0; i < st.Machines.Count; i++)
            {
                var m = st.Machines[i];
                var mr = PowerGrid.ReachOf(d, m);
                if (mr <= 0) continue;
                var r = m.Rect;
                var dist = Reach.DistToRect(r.X + r.W / 2.0, r.Y + r.H / 2.0, x, y, w, h);
                if (dist <= mr && dist < best) { best = dist; nearest = i; }
            }
            if (nearest >= 0)
            {
                var m = st.Machines[nearest];
                var r = m.Rect;
                into.Add(new PowerLink(m.Id, cx, cy, r.X + r.W / 2.0, r.Y + r.H / 2.0));
            }
            return into;
        }

        /// <summary>True when a ghost there would be on a powered network at all (the preview's "not connected" case).</summary>
        public static bool WouldConnect(SimContext ctx, SimState st, string kind, int x, int y, Dir dir = Dir.N, int size = 1) =>
            At(ctx, st, kind, x, y, dir, size).Count > 0;
    }
}
