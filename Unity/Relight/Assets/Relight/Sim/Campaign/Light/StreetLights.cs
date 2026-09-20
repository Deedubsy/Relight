using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace Relight.Sim
{
    /// <summary>
    /// The authored kerb streetlights of the loaded region (reference ground.ts <c>G.blocks[bi].lights</c>).
    ///
    /// The reference keeps a light list per city block and bills the block's substation for
    /// <c>lights.length * RIVERFRONT.lightKw</c> (campaignPower.ts:51). The port retired the block economy (U-D-32),
    /// so a streetlight is an authored 1×1 <see cref="SiteRecord"/>. Court D55 restores the reference's ownership
    /// without the blocks: each light is billed to the circuit of its nearest authored substation site
    /// (<see cref="PowerGrid.SubstationOf"/>) and is unattached while that substation is on no circuit. A region with
    /// no substation sites (the synthetic map, most fixtures) keeps the earlier rule: the light joins the nearest
    /// placed reach node that covers it (campaignPower.ts:41-44). Either way it is lit when its circuit is throttled
    /// above zero — the port's reading of <c>subPowered</c> (flow.ts:620).
    ///
    /// <c>Editor/World/RegionImporter.cs</c> emits the lights as <c>light:N</c> sites of kind
    /// <see cref="SiteKind.Light"/>; this file accepts either the id convention or the <see cref="SiteKindLight"/>
    /// enum value.
    /// </summary>
    public static class StreetLights
    {
        /// <summary>Site id prefix the importer patch emits (<c>light:0</c>, <c>light:1</c>, …).</summary>
        public const string IdPrefix = "light:";

        /// <summary>
        /// The <c>SiteKind.Light</c> the importer patch adds as value 8. Held as an int because the enum member is
        /// in <c>Sim/World/WorldSites.cs</c>, which C-11 does not own; the comparison below is value-identical.
        /// </summary>
        public const int SiteKindLight = 8;

        private static readonly SiteRecord[] None = new SiteRecord[0];
        private static readonly ConditionalWeakTable<WorldSites, SiteRecord[]> Cache =
            new ConditionalWeakTable<WorldSites, SiteRecord[]>();

        /// <summary>True for an authored streetlight site, by either the id convention or the enum value.</summary>
        public static bool IsStreetLight(SiteRecord s) =>
            s != null && ((int)s.Kind == SiteKindLight || s.Id.StartsWith(IdPrefix, System.StringComparison.Ordinal));

        /// <summary>
        /// The region's streetlight sites in export order, cached per <see cref="WorldSites"/> instance (the sites
        /// are immutable, so the list is built once — the reference caches the same way with a WeakMap, flow.ts:1794).
        /// </summary>
        public static IReadOnlyList<SiteRecord> Sites(SimContext ctx)
        {
            var sites = ctx == null ? null : ctx.Sites;
            if (sites == null || sites.Count == 0) return None;
            if (Cache.TryGetValue(sites, out var cached)) return cached;
            var list = new List<SiteRecord>();
            var all = sites.All;
            for (var i = 0; i < all.Count; i++) if (IsStreetLight(all[i])) list.Add(all[i]);
            var arr = list.Count == 0 ? None : list.ToArray();
            Cache.Add(sites, arr);
            return arr;
        }

        /// <summary>How many authored streetlights a substation site owns (their nearest substation is this one).</summary>
        public static int OwnedBy(SimContext ctx, SiteRecord substation)
        {
            if (substation == null) return 0;
            var lights = Sites(ctx);
            var n = 0;
            for (var i = 0; i < lights.Count; i++)
                if (ReferenceEquals(PowerGrid.SubstationOf(ctx, lights[i]), substation)) n++;
            return n;
        }

        /// <summary>
        /// True while this streetlight is on: its circuit is throttled above zero, the same test
        /// <see cref="LightSources.Collect"/> lights it by. The picture asks this to draw the lamp head lit or dead.
        /// </summary>
        public static bool Lit(SimContext ctx, SimState st, SiteRecord s) =>
            ctx != null && st != null && Throttle(PowerGrid.Of(ctx, st), s) > 0;

        /// <summary>The throttle of the circuit this streetlight is billed to, or 0 when it is on none.</summary>
        public static double Throttle(PowerNetwork grid, SiteRecord s)
        {
            var c = grid == null || s == null ? null : grid.OfSite(s.Id);
            return c != null ? c.Throttle : 0;
        }

        /// <summary>
        /// One streetlight's draw in kW. <see cref="PowerTuning"/> has no streetlight row yet (the report carries the
        /// patch that adds <c>StreetLightKw</c>), so this is <see cref="LightRules.StreetLightKw"/> — the imported
        /// region's own <c>scalars.lightKw</c> = 2.
        /// </summary>
        public static double Kw(GameData d) =>
            d?.Power != null && d.Power.StreetLightKw > 0 ? d.Power.StreetLightKw : LightRules.StreetLightKw;

        /// <summary>
        /// One streetlight's lit radius in tiles. Same story as <see cref="Kw"/>: reference constants.ts:58.
        /// </summary>
        public static double RadiusTiles(GameData d) =>
            d?.Power != null && d.Power.StreetLightRadiusTiles > 0 ? d.Power.StreetLightRadiusTiles : LightRules.StreetLightRadiusTiles;
    }
}
