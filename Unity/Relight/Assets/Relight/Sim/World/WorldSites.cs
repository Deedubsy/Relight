using System;
using System.Collections.Generic;

namespace Relight.Sim
{
    /// <summary>The authored site kinds an imported region carries. Mirrors <c>Relight.World.WorldSiteKind</c> one to one.</summary>
    public enum SiteKind
    {
        /// <summary>U-D-19: the Home core is the Home workshop building.</summary>
        Core = 0,
        /// <summary>A 3x3 substation lot (reference ground.ts:630).</summary>
        Substation = 1,
        /// <summary>An authored resource rect (reference riverfront.ts resources, HQ_PATCHES).</summary>
        Resource = 2,
        /// <summary>A first-region enemy camp marker or one of its spawn groups (gameplaySites.ts FIRST_CAMPS).</summary>
        Camp = 3,
        /// <summary>The opening raid approach row (ground.ts homeRaidY).</summary>
        RaidLine = 4,
        /// <summary>A tram stop platform (riverfront.ts stops).</summary>
        TramStop = 5,
        /// <summary>A factory yard reservation (riverfront.ts yards).</summary>
        Yard = 6,
        /// <summary>A region name label (riverfront.ts regions).</summary>
        Label = 7,
        /// <summary>An authored kerb streetlight (C-11; reference ground.ts G.blocks[bi].lights).</summary>
        Light = 8,
        /// <summary>
        /// Batch 4 (U-D-73). A stronghold's power core (riverfront.ts cores, "Occupied freight depot"): the prize the
        /// guardian holds. Its own kind, never <see cref="Core"/>, because the first Core is the Home core.
        /// </summary>
        PowerCore = 9,
        /// <summary>Batch 4. A campaign power plant a Power core commissions (riverfront.ts plants).</summary>
        Plant = 10,
        /// <summary>
        /// Batch 4. A stronghold entrance (gameplaySites.ts FREIGHT_GATES), locked until the keys are carried. A site
        /// door, not the player's buildable Gate (U-D-72).
        /// </summary>
        StrongholdDoor = 11,
        /// <summary>Batch 4. A stronghold's floor rect, its guardian tile and its garrison groups (FREIGHT_ARENA).</summary>
        Arena = 12,
    }

    /// <summary>
    /// One authored site: a stable id from the reference sources and a REGION-LOCAL, Y-DOWN tile rect (a point site
    /// is 1x1). Immutable; built once at boot from the imported region (C-01) and handed to the sim through
    /// <see cref="SimContext.Sites"/>. Never serialised into a save: the save binds to the map by header, and the
    /// sites are re-read from the assigned region on load.
    /// </summary>
    public sealed class SiteRecord
    {
        public string Id { get; }
        public string Name { get; }
        public SiteKind Kind { get; }
        public int X { get; }
        public int Y { get; }
        public int W { get; }
        public int H { get; }
        /// <summary>Resource item id for <see cref="SiteKind.Resource"/> sites; null otherwise.</summary>
        public string Item { get; }
        /// <summary>Units for Resource sites, defender count for Camp sites; 0 otherwise.</summary>
        public int Amount { get; }

        public SiteRecord(string id, string name, SiteKind kind, int x, int y, int w, int h, string item = null, int amount = 0)
        {
            if (string.IsNullOrEmpty(id)) throw new ArgumentException("a site needs an id", nameof(id));
            if (w <= 0 || h <= 0) throw new ArgumentException($"site '{id}' has an empty rect {w}x{h}", nameof(w));
            Id = id; Name = name ?? id; Kind = kind; X = x; Y = y; W = w; H = h;
            Item = string.IsNullOrEmpty(item) ? null : item; Amount = amount;
        }

        /// <summary>Tile-centre of the rect (the point sims aim at and measure from).</summary>
        public Vec2 Centre => new Vec2(X + W / 2.0, Y + H / 2.0);

        public bool Contains(int tx, int ty) => tx >= X && ty >= Y && tx < X + W && ty < Y + H;
    }

    /// <summary>
    /// The authored sites of the loaded region, in export order (deterministic). <see cref="Empty"/> is what the
    /// synthetic map and every pre-C-01 test run with: consumers must cope with a missing site (fall back and say so
    /// in a log line or a query, never throw at tick time).
    /// </summary>
    public sealed class WorldSites
    {
        public static readonly WorldSites Empty = new WorldSites(Array.Empty<SiteRecord>());

        private readonly List<SiteRecord> _all;
        private readonly Dictionary<string, SiteRecord> _byId;

        /// <summary>
        /// The imported region these sites came from ("home", "full"); "" on the synthetic map. Correction pass C6:
        /// the sim has to be able to say WHICH crop of the city it is running, because a save records it too and a
        /// save from another crop has to be moved onto this one (<see cref="SaveRelocate"/>).
        /// </summary>
        public string RegionId { get; }
        /// <summary>The region's origin in whole-city tiles: (43, 91) for the Home crop, (0, 0) for the whole city.</summary>
        public int OriginX { get; }
        public int OriginY { get; }

        public WorldSites(IEnumerable<SiteRecord> sites, string regionId = null, int originX = 0, int originY = 0)
        {
            RegionId = regionId ?? "";
            OriginX = originX;
            OriginY = originY;
            _all = new List<SiteRecord>();
            _byId = new Dictionary<string, SiteRecord>(StringComparer.Ordinal);
            if (sites == null) return;
            foreach (var s in sites)
            {
                if (s == null) continue;
                if (_byId.ContainsKey(s.Id)) throw new ArgumentException($"duplicate site id '{s.Id}'", nameof(sites));
                _all.Add(s);
                _byId.Add(s.Id, s);
            }
        }

        public IReadOnlyList<SiteRecord> All => _all;
        public int Count => _all.Count;

        /// <summary>The Home core site, or null when the region has none (synthetic map).</summary>
        public SiteRecord Core
        {
            get
            {
                foreach (var s in _all) if (s.Kind == SiteKind.Core) return s;
                return null;
            }
        }

        public SiteRecord Find(string id) => id != null && _byId.TryGetValue(id, out var s) ? s : null;

        public IEnumerable<SiteRecord> OfKind(SiteKind kind)
        {
            foreach (var s in _all) if (s.Kind == kind) yield return s;
        }
    }
}
