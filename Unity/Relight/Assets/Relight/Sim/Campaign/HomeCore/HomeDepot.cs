using System;

namespace Relight.Sim
{
    /// <summary>
    /// The Home workbench. Reference flow.ts:400 <c>addMachine(st, 'depot', ...lot(DEPOT_LOT, DEPOT_LOT), 0)</c> puts
    /// one free, unremovable Depot inside the HQ lot at new-game time; hand crafting and the rifle craft are refused
    /// away from it (<see cref="HandCraft.NearDepot"/>). Until 2026-09-14 the port never placed one, so on the
    /// imported region every craft answered "Walk closer to Home workshop" (found in the Phase C integrated run).
    ///
    /// Differences from the reference: the reference lot is a fixed 24x24 frame and the Depot sits at lot (9,9);
    /// the authored Home workshop (<c>ctx.Sites.Core</c>, 10x14 on the Founders Court region) is smaller, so the
    /// Depot is centred in it and, if the centre is blocked, takes the first legal footprint scanning the rect.
    /// A region with no core site (the synthetic map) gets no Depot, exactly as before, and crafting there still
    /// needs one placed by hand (Phase B behaviour, kept for the tests). The reference measures <c>nearDepot</c>
    /// against the lot rectangle; the port accepts either the Depot's reach or the authored workshop rect, so a
    /// save made before this fix crafts at Home too.
    /// </summary>
    public static partial class HomeCore
    {
        public const string DepotKind = "depot";

        /// <summary>The placed Depot, or null.</summary>
        public static Machine Depot(SimState st)
        {
            for (var i = 0; i < st.Machines.Count; i++)
                if (string.Equals(st.Machines[i].Kind, DepotKind, StringComparison.Ordinal)) return st.Machines[i];
            return null;
        }

        /// <summary>
        /// Places the Depot inside the authored core rect if there is none. Returns the Depot, or null when the core is
        /// the synthetic fallback, the catalogue has no depot, or nothing in the rect is free. Never throws.
        /// </summary>
        public static Machine EnsureDepot(SimContext ctx, SimState st)
        {
            var existing = Depot(st);
            if (existing != null) return existing;
            var h = st.Home;
            if (h == null || !h.Placed || h.Fallback) return null;
            if (!ctx.Data.TryMachine(DepotKind, out var spec)) return null;
            var size = Math.Max(1, spec.Size);
            if (h.W < size || h.H < size) return null;

            var cx = h.X + (h.W - size) / 2;
            var cy = h.Y + (h.H - size) / 2;
            if (Fits(ctx, st, cx, cy)) return Placement.Add(ctx, st, DepotKind, cx, cy, Dir.N);
            for (var y = h.Y; y + size <= h.Y + h.H; y++)
                for (var x = h.X; x + size <= h.X + h.W; x++)
                    if (Fits(ctx, st, x, y)) return Placement.Add(ctx, st, DepotKind, x, y, Dir.N);
            return null;
        }

        private static bool Fits(SimContext ctx, SimState st, int x, int y) =>
            Placement.GeometryProblem(ctx, st, DepotKind, x, y, Dir.N).Length == 0;
    }
}
