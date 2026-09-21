using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace Relight.Sim
{
    /// <summary>One district: a substation site and every tile nearer to it than to any other (INT-09a, REL-81).</summary>
    public sealed class District
    {
        private readonly Districts _owner;
        internal int TileCount, WalkableCount, StreetCount;
        internal readonly List<int> Streets = new List<int>();

        internal District(Districts owner, int index, SiteRecord substation, string name, Vec2 label)
        {
            _owner = owner;
            Index = index;
            Substation = substation;
            Name = name;
            Label = label;
        }

        /// <summary>Place in <see cref="Districts.All"/>: the substation sites' export order.</summary>
        public int Index { get; }
        public SiteRecord Substation { get; }
        /// <summary>What the player is told: see <see cref="Districts"/> for the rule.</summary>
        public string Name { get; }
        /// <summary>The centre of the label that named it, else the substation's centre. In tiles.</summary>
        public Vec2 Label { get; }

        /// <summary>Every tile of the district, walkable or not.</summary>
        public int Tiles { get { _owner.Survey(); return TileCount; } }
        /// <summary><see cref="Ground.Walkable(SimContext,int,int)"/> tiles: not solid, not river.</summary>
        public int Walkable { get { _owner.Survey(); return WalkableCount; } }
        /// <summary>Walkable tiles of class <see cref="TileClass.Street"/>.</summary>
        public int Street { get { _owner.Survey(); return StreetCount; } }
        /// <summary>The street tiles in scan order, each y * Width + x.</summary>
        public IReadOnlyList<int> StreetTiles { get { _owner.Survey(); return Streets; } }
    }

    /// <summary>
    /// THE district query (INT-09a, REL-81): tile to district, district to name, district to substation, and the
    /// street-tile count of each. Power, the opening goals, the HUD's place names and the developer overlay all read
    /// this, so they cannot disagree about where a district ends or what it is called.
    ///
    /// <b>The rule</b> (ALWAYS_DARK_SPEC.md §6). A district is every position whose nearest substation SITE is the
    /// same one: squared distance to the site's centre, and a tie goes to the earlier site in export order. A tile
    /// is measured from its centre. With no substation sites there are no districts and every query answers "none".
    ///
    /// <b>The name.</b> The named <see cref="SiteKind.Label"/> nearest the substation's centre; with no named label
    /// on the map, the substation's own name; failing that its id. On the real city each of the nine substations
    /// ("Substation 0" to "Substation 8") has exactly one label, inside its own district and nearest to it.
    ///
    /// Read-only and derived from the immutable sites and the authored geometry, so nothing here is saved or hashed.
    /// The point queries cost one pass over the substations. The per-tile table and the counts are worked out once,
    /// on first ask, and kept with the context.
    /// </summary>
    public sealed class Districts
    {
        /// <summary><see cref="Owner"/>'s value for a tile on a map with no substation sites.</summary>
        public const byte None = 255;

        private static readonly ConditionalWeakTable<SimContext, Districts> Cache =
            new ConditionalWeakTable<SimContext, Districts>();
        private static readonly byte[] NoTiles = new byte[0];

        private readonly SimContext _ctx;
        private readonly List<District> _all = new List<District>();
        private readonly double[] _cx, _cy;
        private byte[] _tiles;

        public IReadOnlyList<District> All => _all;
        public int Count => _all.Count;
        public int Width => _ctx.Geometry.Width;
        public int Height => _ctx.Geometry.Height;

        /// <summary>The districts of a context's map. Never null; empty when the map has no substation sites.</summary>
        public static Districts Of(SimContext ctx)
        {
            if (ctx == null) throw new ArgumentNullException(nameof(ctx));
            return Cache.GetValue(ctx, c => new Districts(c));
        }

        private Districts(SimContext ctx)
        {
            _ctx = ctx;
            var subs = new List<SiteRecord>();
            var labels = new List<SiteRecord>();
            if (ctx.Sites != null)
            {
                foreach (var s in ctx.Sites.OfKind(SiteKind.Substation))
                    if (subs.Count < None) subs.Add(s);
                foreach (var l in ctx.Sites.OfKind(SiteKind.Label))
                    if (!string.IsNullOrEmpty(l.Name)) labels.Add(l);
            }
            _cx = new double[subs.Count];
            _cy = new double[subs.Count];
            for (var i = 0; i < subs.Count; i++)
            {
                var c = subs[i].Centre;
                _cx[i] = c.X;
                _cy[i] = c.Y;
                var label = NearestLabel(labels, c);
                var name = label != null ? label.Name : string.IsNullOrEmpty(subs[i].Name) ? subs[i].Id : subs[i].Name;
                _all.Add(new District(this, i, subs[i], name, label != null ? label.Centre : c));
            }
        }

        private static SiteRecord NearestLabel(List<SiteRecord> labels, Vec2 at)
        {
            SiteRecord best = null;
            var score = double.PositiveInfinity;
            for (var i = 0; i < labels.Count; i++)
            {
                var c = labels[i].Centre;
                var dd = (c.X - at.X) * (c.X - at.X) + (c.Y - at.Y) * (c.Y - at.Y);
                if (dd >= score) continue;
                score = dd;
                best = labels[i];
            }
            return best;
        }

        // ------------------------------------------------------------------ tile to district

        /// <summary>The index of the district a position (in tiles) is in, or -1 when the map has none.</summary>
        public int IndexAt(double x, double y)
        {
            var best = -1;
            var score = double.PositiveInfinity;
            for (var i = 0; i < _cx.Length; i++)
            {
                var dx = _cx[i] - x;
                var dy = _cy[i] - y;
                var dd = dx * dx + dy * dy;
                if (dd >= score) continue;   // strict, so the earlier site keeps a tie
                score = dd;
                best = i;
            }
            return best;
        }

        /// <summary>The district a position (in tiles) is in, or null when the map has none.</summary>
        public District At(double x, double y)
        {
            var i = IndexAt(x, y);
            return i < 0 ? null : _all[i];
        }

        /// <summary>The district of a tile, measured from the tile's centre.</summary>
        public District OfTile(int x, int y) => At(x + 0.5, y + 0.5);

        // ------------------------------------------------------------------ the three questions, in one call each

        public static District At(SimContext ctx, double x, double y) => Of(ctx).At(x, y);

        /// <summary>District to substation: the substation site whose district holds the position, or null.</summary>
        public static SiteRecord SubstationAt(SimContext ctx, double x, double y) => Of(ctx).At(x, y)?.Substation;

        /// <summary>District to name: what the player is told the place is called, or null when the map has none.</summary>
        public static string NameAt(SimContext ctx, double x, double y) => Of(ctx).At(x, y)?.Name;

        // ------------------------------------------------------------------ the per-tile table and the counts

        /// <summary>District index per tile, y * Width + x; <see cref="None"/> everywhere on a map with no districts.</summary>
        public byte[] Owner { get { Survey(); return _tiles; } }

        /// <summary>The street tiles of every district together: the city's street total.</summary>
        public int StreetTotal
        {
            get
            {
                Survey();
                var n = 0;
                for (var i = 0; i < _all.Count; i++) n += _all[i].StreetCount;
                return n;
            }
        }

        internal void Survey()
        {
            if (_tiles != null) return;
            var g = _ctx.Geometry;
            int w = g.Width, h = g.Height;
            var tiles = w * h == 0 ? NoTiles : new byte[w * h];
            for (var y = 0; y < h; y++)
            for (var x = 0; x < w; x++)
            {
                var i = IndexAt(x + 0.5, y + 0.5);
                tiles[y * w + x] = i < 0 ? None : (byte)i;
                if (i < 0) continue;
                var d = _all[i];
                d.TileCount++;
                if (!Ground.Walkable(_ctx, x, y)) continue;
                d.WalkableCount++;
                if (g.TileAt(x, y) != TileClass.Street) continue;
                d.StreetCount++;
                d.Streets.Add(y * w + x);
            }
            _tiles = tiles;
        }
    }
}
