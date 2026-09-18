using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace Relight.Sim
{
    /// <summary>
    /// The authored kerb streetlights of the loaded region (reference ground.ts <c>G.blocks[bi].lights</c>).
    ///
    /// The reference keeps a light list per city block and bills the block's substation for
    /// <c>lights.length * RIVERFRONT.lightKw</c> (campaignPower.ts:51). The port retired the block economy (U-D-32),
    /// so a streetlight is an authored 1×1 <see cref="SiteRecord"/> and joins the power network through exactly the
    /// same "nearest reach node that covers me" path every consuming machine uses (campaignPower.ts:41-44). It is
    /// lit when that circuit is throttled above zero — the port's reading of <c>subPowered</c> (flow.ts:620).
    ///
    /// The region importer does not export the light positions yet: <c>Editor/World/CityFile.cs</c>'s <c>JSites</c>
    /// has no <c>lights</c> member and <c>HomeRegionImporter.SiteRecords</c> never reads one, so
    /// <see cref="SimContext.Sites"/> carries none today and every query here answers "no streetlights". The
    /// wave-3 W-B report carries the exact importer patch. This file already accepts what that patch will emit —
    /// an id of the form <c>light:0</c>, or the <see cref="SiteKindLight"/> enum value — so nothing here changes
    /// when it lands.
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
