using System.Collections.Generic;
using Relight.Sim;

namespace Relight.World
{
    /// <summary>
    /// C-01. Converts the resolved <see cref="WorldSite"/> list (generated asset plus manual overrides) into the
    /// engine-free <see cref="WorldSites"/> the sim reads through <see cref="SimContext.Sites"/>. Pure conversion:
    /// no re-ordering, no interpretation, kinds map by value.
    /// </summary>
    public static class SiteBridge
    {
        public static WorldSites ToSim(IReadOnlyList<WorldSite> sites, string regionId = "", int originX = 0, int originY = 0)
        {
            if (sites == null || sites.Count == 0) return new WorldSites(System.Array.Empty<SiteRecord>(), regionId, originX, originY);
            var records = new List<SiteRecord>(sites.Count);
            foreach (var s in sites)
            {
                if (s == null || string.IsNullOrEmpty(s.id)) continue;
                var w = s.rect.width <= 0 ? 1 : s.rect.width;
                var h = s.rect.height <= 0 ? 1 : s.rect.height;
                records.Add(new SiteRecord(s.id, s.name, (SiteKind)(int)s.kind, s.rect.x, s.rect.y, w, h, s.item, s.amount));
            }
            return new WorldSites(records, regionId, originX, originY);
        }
    }
}
