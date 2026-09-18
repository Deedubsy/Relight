using System;
using System.Collections.Generic;
using System.Globalization;

namespace Relight.Sim
{
    /// <summary>
    /// Correction pass C6. Moves a loaded state from the region it was saved on to the region the game is running,
    /// or refuses it with a reason the player can act on.
    ///
    /// <b>Why this exists.</b> Phase C shipped a Home-only crop: 78x367 tiles taken out of the 864x576 city at
    /// (43, 91), with every exported coordinate made region-local. The correction pass imports the whole city, so
    /// the same authored place now has two names — the workshop door is (27, 269) in a Phase C save and (70, 360)
    /// on the full map. Both files say <c>mapId: riverfront-arc-v4-editor-ac16d9188c05</c>, because it IS the same
    /// city, so the C-10 map binding cannot tell them apart and would load a Phase C save 43 tiles west and 91
    /// tiles north of where the player left it — on top of a river, inside a wall, or nowhere at all.
    ///
    /// <b>What it moves.</b> Everything positional, without exception, because a half-moved save is worse than a
    /// refused one. Three kinds of value need three treatments:
    /// <list type="bullet">
    /// <item><b>Tile-space points</b> (<see cref="Vec2"/>: the engineer, every enemy, projectiles, shot traces, the
    ///       point a turret last fired at) are offset by the delta. Directions are NOT — <c>vel</c>, <c>face</c>,
    ///       <c>dir</c> and <c>dashDir</c> are unit vectors, and adding an origin to one would send the engineer
    ///       walking into the corner of the map.</item>
    /// <item><b>Tile coordinates</b> (machine x/y, the Home core rect, the tile being mined) are offset likewise.</item>
    /// <item><b>Tile INDICES</b> (<c>y * width + x</c>: dug tiles, raid approaches, an enemy's entry tile and its
    ///       waypoint, the opening encounter's approach) are decoded against the WIDTH THE SAVE WAS WRITTEN WITH,
    ///       offset, and re-formed against the running map's width. This is why <see cref="SaveRegion"/> records the
    ///       region's size and why a save that does not record it is refused rather than guessed at: an index moved
    ///       against the wrong width is not wrong by a tile, it is wrong by a row.</item>
    /// </list>
    ///
    /// <b>What it refuses.</b> The engineer and the machines are the player's game. If the move would put the
    /// engineer or any machine outside the map or inside a building wall, nothing is changed and the load is refused
    /// with the reason — the save is still on disk, still loadable on the region it was made on. Transients that the
    /// sim creates and destroys by itself (enemies, projectiles, shot traces, dug tiles) are DROPPED when they fall
    /// off the new map instead of failing the load: a raid that was half off the edge of the crop is not a thing the
    /// player would rather lose their save over. Widening the crop to the whole city drops nothing, because the crop
    /// lies entirely inside the city.
    ///
    /// <b>Where it runs.</b> After the state has been built and its checksum verified (<see cref="SaveSerializer"/>),
    /// never before: the hash is contract H1's anchor and it must go on describing the file as it was written.
    /// </summary>
    public static class SaveRelocate
    {
        /// <summary>True when a save from <paramref name="saved"/> has to be moved to run on <paramref name="running"/>.</summary>
        public static bool Needed(SaveRegion saved, SaveRegion running)
            => saved != null && running != null && saved.Known && running.Known && !saved.SameOrigin(running);

        /// <summary>
        /// Move <paramref name="st"/> from <paramref name="saved"/> onto <paramref name="running"/> (the map
        /// <paramref name="map"/> describes). Returns false with <paramref name="reason"/> and leaves the state
        /// untouched when the move is not possible. A move that is not needed is a success that changes nothing.
        /// </summary>
        public static bool Apply(SimState st, SaveRegion saved, SaveRegion running, ICityGeometry map, out string reason)
        {
            reason = "";
            if (st == null) { reason = "there is no state to move"; return false; }
            if (!Needed(saved, running)) return true;

            // Saved coordinates are local to the region they were written in. The same authored tile is
            // city = local + origin, so local_running = local_saved + (origin_saved - origin_running). Home (43,91)
            // onto the whole city (0,0) is therefore +43/+91: the workshop door at (27, 269) becomes (70, 360).
            var dx = saved.OriginX - running.OriginX;
            var dy = saved.OriginY - running.OriginY;
            if (dx == 0 && dy == 0) return true;

            var where = "this save was made on " + saved + " and this game is on " + running + "; moving it would ";
            if (map == null || map.Width <= 0 || map.Height <= 0)
            {
                reason = where + "need a map to move it onto, and none was given";
                return false;
            }
            if (!saved.HasSize)
            {
                reason = where + "have to re-form the tiles it refers to, and it does not record how wide its region was";
                return false;
            }

            // ---- decide first, change nothing yet ------------------------------------------------------------
            var e = st.Engineer;
            var ex = Floor(e.Pos.X) + dx;
            var ey = Floor(e.Pos.Y) + dy;
            if (!Inside(map, ex, ey)) { reason = where + "put the engineer outside the map" + At(ex, ey); return false; }
            if (map.Solid(ex, ey)) { reason = where + "put the engineer inside a building" + At(ex, ey); return false; }

            for (var i = 0; i < st.Machines.Count; i++)
            {
                var m = st.Machines[i];
                var r = m.Rect;
                for (var y = r.Y + dy; y < r.Y + r.H + dy; y++)
                {
                    for (var x = r.X + dx; x < r.X + r.W + dx; x++)
                    {
                        if (!Inside(map, x, y))
                        {
                            reason = where + "put the " + Name(m) + " outside the map" + At(x, y);
                            return false;
                        }
                        if (map.Solid(x, y))
                        {
                            reason = where + "put the " + Name(m) + " inside a building" + At(x, y);
                            return false;
                        }
                    }
                }
            }

            var home = st.Home;
            if (home != null && home.Placed
                && (!Inside(map, home.X + dx, home.Y + dy)
                    || !Inside(map, home.X + home.W - 1 + dx, home.Y + home.H - 1 + dy)))
            {
                reason = where + "put the Home core outside the map" + At(home.X + dx, home.Y + dy);
                return false;
            }

            // ---- move ----------------------------------------------------------------------------------------
            var d = new Vec2(dx, dy);
            e.Pos += d;
            if (e.HasTarget) e.Target += d;
            if (e.HasAim) e.Aim += d;
            if (e.Mining) { e.MineX += dx; e.MineY += dy; }

            for (var i = 0; i < st.Machines.Count; i++)
            {
                st.Machines[i].X += dx;
                st.Machines[i].Y += dy;
            }

            if (home != null && home.Placed) { home.X += dx; home.Y += dy; }

            MoveDugTiles(st, saved, map, dx, dy);
            MoveEnemies(st, saved, map, d, dx, dy);
            MoveWeapons(st, d);
            MoveTurrets(st, d);
            MoveApproaches(st, saved, map, dx, dy);

            // Every derived cache keyed on the structure is now stale (machine occupancy, the light mask, the
            // path scratch's contents). Rev is what the sim already uses to say "the world moved".
            st.Rev++;
            return true;
        }

        /// <summary>Dug tiles are indices; a tile that falls off the new map is forgotten along with its units.</summary>
        private static void MoveDugTiles(SimState st, SaveRegion saved, ICityGeometry map, int dx, int dy)
        {
            var g = st.Ground;
            if (g == null) return;
            var tiles = g.DugTiles ?? Array.Empty<int>();
            var units = g.DugUnits ?? Array.Empty<double>();
            var keptTiles = new List<int>(tiles.Length);
            var keptUnits = new List<double>(tiles.Length);
            for (var i = 0; i < tiles.Length; i++)
            {
                var moved = Tile(tiles[i], saved, map, dx, dy);
                if (moved < 0) continue;
                keptTiles.Add(moved);
                keptUnits.Add(i < units.Length ? units[i] : 0);
            }
            g.SetDugLists(keptTiles.ToArray(), keptUnits.ToArray());
        }

        private static void MoveEnemies(SimState st, SaveRegion saved, ICityGeometry map, Vec2 d, int dx, int dy)
        {
            var es = st.Enemies;
            if (es == null) return;
            for (var i = es.Actors.Count - 1; i >= 0; i--)
            {
                var a = es.Actors[i];
                var x = Floor(a.Pos.X) + dx;
                var y = Floor(a.Pos.Y) + dy;
                if (!Inside(map, x, y)) { es.Actors.RemoveAt(i); continue; }
                a.Pos += d;
                a.Home += d;
                a.Aim += d;
                a.LastKnown += d;
                a.Origin = Tile(a.Origin, saved, map, dx, dy);
                a.Waypoint = Tile(a.Waypoint, saved, map, dx, dy);
            }
            for (var i = es.Projectiles.Count - 1; i >= 0; i--)
            {
                var p = es.Projectiles[i];
                if (!Inside(map, Floor(p.Pos.X) + dx, Floor(p.Pos.Y) + dy)) { es.Projectiles.RemoveAt(i); continue; }
                p.Pos += d;
            }
        }

        private static void MoveWeapons(SimState st, Vec2 d)
        {
            var w = st.Weapons;
            if (w == null) return;
            for (var i = 0; i < w.Projectiles.Count; i++)
            {
                var p = w.Projectiles[i];
                p.Pos += d;
                p.Origin += d;
            }
            for (var i = 0; i < w.Shots.Count; i++)
            {
                var s = w.Shots[i];
                s.From += d;
                s.To += d;
            }
        }

        private static void MoveTurrets(SimState st, Vec2 d)
        {
            var t = st.Turrets;
            if (t == null) return;
            // Shot is only a place while ShotT names a moment; a turret that has never fired carries (0, 0) as
            // "nothing", and moving that would invent a shot 43 tiles away.
            for (var i = 0; i < t.Units.Count; i++) if (t.Units[i].ShotT > 0) t.Units[i].Shot += d;
        }

        /// <summary>Raid approaches and the opening encounter's approach are tile indices; -1 is "none".</summary>
        private static void MoveApproaches(SimState st, SaveRegion saved, ICityGeometry map, int dx, int dy)
        {
            var op = st.Opening;
            if (op != null) op.Origin = Tile(op.Origin, saved, map, dx, dy);

            var d = st.Director;
            if (d == null) return;
            if (d.Major != null)
            {
                d.Major.Origin = Tile(d.Major.Origin, saved, map, dx, dy);
                var origins = d.Major.Origins;
                if (origins != null && origins.Length > 0)
                {
                    var kept = new List<int>(origins.Length);
                    for (var i = 0; i < origins.Length; i++)
                    {
                        var moved = Tile(origins[i], saved, map, dx, dy);
                        if (moved >= 0) kept.Add(moved);
                    }
                    d.Major.Origins = kept.ToArray();
                }
            }
            if (d.Minor != null) d.Minor.Origin = Tile(d.Minor.Origin, saved, map, dx, dy);
        }

        /// <summary>
        /// One tile index moved between regions: decoded against the width it was written with, offset, and
        /// re-formed against the running map's width. -1 for "none", and for a tile that is no longer on the map.
        /// </summary>
        public static int Tile(int tile, SaveRegion saved, ICityGeometry map, int dx, int dy)
        {
            if (tile < 0 || saved == null || !saved.HasSize || map == null) return -1;
            var w = saved.Width;
            if (tile >= w * saved.Height) return -1;
            var x = tile % w + dx;
            var y = tile / w + dy;
            if (!Inside(map, x, y)) return -1;
            return y * map.Width + x;
        }

        private static bool Inside(ICityGeometry map, int x, int y)
            => x >= 0 && y >= 0 && x < map.Width && y < map.Height;

        /// <summary>Floor, not truncation: a point at x = -0.25 is on tile -1, and truncation would call it tile 0.</summary>
        private static int Floor(double v) => (int)Math.Floor(v);

        private static string At(int x, int y)
            => " (" + x.ToString(CultureInfo.InvariantCulture) + ", " + y.ToString(CultureInfo.InvariantCulture) + ")";

        private static string Name(Machine m) => string.IsNullOrEmpty(m.Kind) ? "machine" : m.Kind;
    }
}
