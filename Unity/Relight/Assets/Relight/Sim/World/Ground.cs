using System;
using System.Collections.Generic;

namespace Relight.Sim
{
    /// <summary>
    /// The runtime tile grid: what the player has changed about the authored world, plus the derived caches the
    /// movement and path-finding code needs. Reference <c>ground.ts</c> <c>Ground</c> + <c>flow.dug</c>.
    ///
    /// STATE (visited, saved): the dug tiles and how many units are left on each. The reference keeps this as
    /// per-block lists (<c>st.flow.dug[blockIndex]</c>, ground.ts:479) with the remaining amount derived from the
    /// block's rubble pool; block indices are retired with the block economy (CONTENT_CATALOGUE.md §17), so the port
    /// keys it by tile instead — two parallel ordered lists, never a dictionary (TECHNICAL_ARCHITECTURE.md §10.5).
    ///
    /// DERIVED (never visited, rebuilt lazily): the machine occupancy mask, keyed on <see cref="SimState.Rev"/>
    /// exactly as the reference keys <c>solidMap</c> on <c>flow.rev</c> (walk.ts:31), and the tile→slot index for the
    /// dug lists. A save that restores state rebuilds both on first use; a snapshot restart therefore cannot change
    /// behaviour through a stale cache.
    /// </summary>
    public sealed class GroundState : IVisitable
    {
        /// <summary>Dug tile indices (<c>y * Width + x</c>) in the order they were first dug.</summary>
        public int[] DugTiles = Array.Empty<int>();
        /// <summary>Units still to take out of the matching tile in <see cref="DugTiles"/>; 0 means fully cleared.</summary>
        public double[] DugUnits = Array.Empty<double>();

        // ---- derived, not saved ----
        private byte[] _solid;
        private int _solidRev = int.MinValue;
        private int[] _slotOf;      // tile -> index into DugTiles, +1 (0 = untouched)
        private int _slotCount = -1;

        public void Visit(IStateVisitor v)
        {
            v.Field("dugTiles", ref DugTiles);
            v.Field("dugUnits", ref DugUnits);
            // Derived caches are deliberately absent; they are rebuilt from state on demand.
            _solidRev = int.MinValue;
            _slotCount = -1;
        }

        /// <summary>Slot of a tile in the dug lists, or -1. Rebuilds the lazy index when the lists changed.</summary>
        public int SlotOf(int tile, int tileCount)
        {
            if (_slotCount != DugTiles.Length || _slotOf == null || _slotOf.Length != tileCount)
            {
                _slotOf = new int[tileCount];
                for (var i = 0; i < DugTiles.Length; i++)
                {
                    var t = DugTiles[i];
                    if (t >= 0 && t < tileCount) _slotOf[t] = i + 1;
                }
                _slotCount = DugTiles.Length;
            }
            if (tile < 0 || tile >= tileCount) return -1;
            return _slotOf[tile] - 1;
        }

        /// <summary>Records that a tile has been dug down to <paramref name="unitsLeft"/> (0 = cleared).</summary>
        public void SetDug(int tile, int tileCount, double unitsLeft)
        {
            var slot = SlotOf(tile, tileCount);
            if (slot >= 0) { DugUnits[slot] = unitsLeft; return; }
            var n = DugTiles.Length;
            Array.Resize(ref DugTiles, n + 1);
            Array.Resize(ref DugUnits, n + 1);
            DugTiles[n] = tile;
            DugUnits[n] = unitsLeft;
            _slotCount = -1;
        }

        // ---- A* scratch (derived; reference walk.ts:60 scratchOf, a module WeakMap there — state lives here
        // instead so the sim keeps no module-global mutable state, TECHNICAL_ARCHITECTURE.md §10.5) ----
        private PathScratch _path;

        /// <summary>Reusable A* working arrays for a map of <paramref name="tileCount"/> tiles.</summary>
        public PathScratch Scratch(int tileCount)
        {
            if (_path == null || _path.Size != tileCount) _path = new PathScratch(tileCount);
            return _path;
        }

        /// <summary>The machine occupancy mask for the current <see cref="SimState.Rev"/> (reference walk.ts:32 solidMap).</summary>
        public byte[] SolidMap(SimContext ctx, SimState st)
        {
            if (_solid != null && _solidRev == st.Rev) return _solid;
            var w = ctx.Geometry.Width;
            var h = ctx.Geometry.Height;
            if (_solid == null || _solid.Length != w * h) _solid = new byte[w * h];
            else Array.Clear(_solid, 0, _solid.Length);
            for (var i = 0; i < st.Machines.Count; i++)
            {
                var m = st.Machines[i];
                if (Ground.WalkThrough(m.Kind)) continue;
                // Reference walk.ts:37 also skips wall/barricade/turret machines whose `hp === 0` (destroyed but
                // not removed). Machine.Hp is Phase C; a machine that exists is treated as present and intact.
                var r = m.Rect;
                for (var y = r.Y; y < r.Y + r.H; y++)
                    for (var x = r.X; x < r.X + r.W; x++)
                        if (x >= 0 && y >= 0 && x < w && y < h) _solid[y * w + x] = 1;
            }
            _solidRev = st.Rev;
            return _solid;
        }
    }

    public sealed partial class SimState
    {
        /// <summary>The runtime tile grid (B-08).</summary>
        public GroundState Ground = new GroundState();

        partial void VisitWorld(IStateVisitor v)
        {
            v.Object("ground", ref Ground, () => new GroundState());
        }
    }

    /// <summary>
    /// Tile queries. All of them are pure functions of (context, state, tile); nothing here holds mutable state, so
    /// the same inputs always give the same answer (TECHNICAL_ARCHITECTURE.md §10.5).
    /// </summary>
    public static class Ground
    {
        /// <summary>
        /// GAME-ASSUMPTION carried over verbatim from walk.ts:29 <c>PASSABLE</c>: the engineer walks over belts and
        /// poles (thin), and over track and a tram (RI-05). Track and tram are retired as player-buildables
        /// (CONTENT_CATALOGUE.md §17) so they cannot occur, but keeping them keeps the predicate identical.
        /// A switch, not a set — no dictionary lookups in tick code.
        /// </summary>
        public static bool WalkThrough(string kind)
        {
            switch (kind)
            {
                case "belt":
                case "fastbelt":
                case "pole":
                case "track":
                case "tram":
                    return true;
                default:
                    return false;
            }
        }

        /// <summary>Reference ground.ts:47 <c>inGround</c>.</summary>
        public static bool InBounds(SimContext ctx, int x, int y) =>
            x >= 0 && y >= 0 && x < ctx.Geometry.Width && y < ctx.Geometry.Height;

        /// <summary>
        /// Reference ground.ts:440 <c>walkable</c>: <c>inGround(G,tx,ty) &amp;&amp; base[t] !== T_RIVER &amp;&amp;
        /// !G.urban?.solid[t]</c>. The single authored-passability predicate (WORLD_AND_ASSETS.md §2.6).
        /// Note that <see cref="TileClass.Inert"/> is WALKABLE here, because the reference only excludes river —
        /// see the B-08 report.
        /// </summary>
        public static bool Walkable(SimContext ctx, int x, int y)
        {
            if (!InBounds(ctx, x, y)) return false;
            if (ctx.Geometry.TileAt(x, y) == TileClass.River) return false;
            return !ctx.Geometry.Solid(x, y);
        }

        /// <summary>Authored walkability does not depend on run-time state; the overload exists for call symmetry.</summary>
        public static bool Walkable(SimContext ctx, SimState st, int x, int y) => Walkable(ctx, x, y);

        /// <summary>True when a machine that is not walk-through covers the tile (reference walk.ts:32 solidMap).</summary>
        public static bool Occupied(SimContext ctx, SimState st, int x, int y)
        {
            if (!InBounds(ctx, x, y)) return false;
            var solid = st.Ground.SolidMap(ctx, st);
            return solid[y * ctx.Geometry.Width + x] != 0;
        }

        /// <summary>
        /// Reference walk.ts:50 <c>passable</c>: walkable terrain with no solid machine on it. The reference also
        /// excludes tiles the truck stands on (<c>truckOccupies</c>); the truck is Phase C.
        /// </summary>
        public static bool Passable(SimContext ctx, SimState st, int x, int y) =>
            Walkable(ctx, x, y) && !Occupied(ctx, st, x, y);

        /// <summary>
        /// Reference walk.ts:57 <c>canStand</c>: all four corners of the body square must be passable, so a thin
        /// sprite still cannot slide its centre through a diagonal gap.
        /// </summary>
        public static bool CanStand(SimContext ctx, SimState st, double x, double y, double r)
        {
            return Passable(ctx, st, (int)Math.Floor(x - r), (int)Math.Floor(y - r))
                && Passable(ctx, st, (int)Math.Floor(x + r), (int)Math.Floor(y - r))
                && Passable(ctx, st, (int)Math.Floor(x - r), (int)Math.Floor(y + r))
                && Passable(ctx, st, (int)Math.Floor(x + r), (int)Math.Floor(y + r));
        }

        /// <summary>Body radius from data (reference default <c>r = .28</c>, ported as EngineerTuning.BodyRadiusTiles).</summary>
        public static bool CanStand(SimContext ctx, SimState st, double x, double y) =>
            CanStand(ctx, st, x, y, ctx.Data.Engineer.BodyRadiusTiles);

        /// <summary>
        /// The tile class as the player sees it, after digging: reference ground.ts:508 <c>tileAt</c> reads a dug
        /// rubble/deposit/patch tile back as plain ground.
        /// </summary>
        public static TileClass TileAt(SimContext ctx, SimState st, int x, int y)
        {
            var k = ctx.Geometry.TileAt(x, y);
            if (!Tiles.CanHoldUnits(k)) return k;
            var w = ctx.Geometry.Width;
            var slot = st.Ground.SlotOf(y * w + x, w * ctx.Geometry.Height);
            if (slot >= 0 && st.Ground.DugUnits[slot] <= 0) return TileClass.Ground;
            return k;
        }

        /// <summary>Units left on a minable tile; the authored full amount until it has been dug.</summary>
        public static double UnitsAt(SimContext ctx, SimState st, int x, int y)
        {
            var k = ctx.Geometry.TileAt(x, y);
            if (!Tiles.CanHoldUnits(k)) return 0;
            var w = ctx.Geometry.Width;
            var slot = st.Ground.SlotOf(y * w + x, w * ctx.Geometry.Height);
            if (slot >= 0) return st.Ground.DugUnits[slot];
            return ctx.Data.World.RubbleUnitsPerTile;
        }

        /// <summary>
        /// Reference ground.ts:596 <c>inReach</c>: D5's 8-tile reach from where the engineer stands to the nearest
        /// edge of the target rectangle, and line of sight to the point on that rectangle nearest the engineer
        /// (the ±0.01 clamp is the reference's, kept exactly).
        /// </summary>
        public static bool InReach(SimContext ctx, SimState st, double tx, double ty, double w, double h)
        {
            var e = st.Engineer;
            if (Reach.DistToRect(e.Pos.X, e.Pos.Y, tx, ty, w, h) > ctx.Data.Engineer.ReachTiles) return false;
            var bx = Math.Max(tx - .01, Math.Min(tx + w + .01, e.Pos.X));
            var by = Math.Max(ty - .01, Math.Min(ty + h + .01, e.Pos.Y));
            return ctx.Geometry.Sight(e.Pos.X, e.Pos.Y, bx, by);
        }

        /// <summary>Square target of side <paramref name="size"/>, the reference's default shape.</summary>
        public static bool InReach(SimContext ctx, SimState st, int tx, int ty, int size = 1) =>
            InReach(ctx, st, tx, ty, size, size);

        /// <summary>
        /// Reference walk.ts:157 <c>nearestOpen</c>: the goal itself when it is passable, otherwise the closest
        /// passable tile within <paramref name="r"/> by squared distance, scanning dy then dx so ties resolve the
        /// same way every run.
        /// </summary>
        public static bool NearestOpen(SimContext ctx, SimState st, int gx, int gy, int r, out TilePoint found)
        {
            if (Passable(ctx, st, gx, gy)) { found = new TilePoint(gx, gy); return true; }
            var bd = double.PositiveInfinity;
            var ok = false;
            found = default;
            for (var dy = -r; dy <= r; dy++)
                for (var dx = -r; dx <= r; dx++)
                {
                    var d = dx * dx + dy * dy;
                    if (d >= bd || !Passable(ctx, st, gx + dx, gy + dy)) continue;
                    bd = d;
                    found = new TilePoint(gx + dx, gy + dy);
                    ok = true;
                }
            return ok;
        }
    }
}
