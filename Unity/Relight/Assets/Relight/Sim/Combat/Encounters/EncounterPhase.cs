using System;
using System.Collections.Generic;

namespace Relight.Sim
{
    /// <summary>An encounter's garrison was born (§5.1). <c>Bodies</c> is how many.</summary>
    public sealed record EncounterResolvedEvent(double T, string Id, int Bodies) : SimEvent(T);

    /// <summary>The last body of an encounter's garrison died.</summary>
    public sealed record EncounterClearedEvent(double T, string Id) : SimEvent(T);

    /// <summary>
    /// A key camp was held long enough and its key went into the pouch (§5.2). <c>Text</c> is the HUD line
    /// ("West passage secured — key 1 of 3"); <c>Held</c> and <c>Of</c> are its two numbers.
    /// </summary>
    public sealed record EncounterClaimedEvent(double T, string Id, string Key, int Held, int Of, string Text) : SimEvent(T);

    /// <summary>
    /// Batch 4, FRT-02 (REL-137): the one tick every hostile place runs through (FREIGHT_STRONGHOLD_DESIGN §3,
    /// §5.1). It walks <see cref="EncounterCatalogue.All"/> in order and, for each row whose site the region has,
    /// births the garrison the first time the engineer comes close enough, then notes when the last of it dies.
    /// What the bodies DO once born is <see cref="EnemyPhase"/>'s camp-resident branch, unchanged: they are
    /// <see cref="EnemyLayer.Site"/> bodies like any other.
    ///
    /// A garrison is born WHOLE OR NOT AT ALL. Every body is placed before any is created; if one cannot be — its
    /// group has no room, the world is at <see cref="RaidTuning.LivingBudget"/>, or the only room is within
    /// <see cref="EncounterCatalogue.NoSurpriseBirthTiles"/> of the engineer — nothing is born and the next tick
    /// tries again. A half-born camp would be one whose count on the map disagrees with its row for the rest of the
    /// game, and a body born beside the engineer is an ambush the player could not have seen coming.
    /// </summary>
    public sealed class EncounterPhase : ITickPhase
    {
        public void Tick(SimContext ctx, SimState st, double dt)
        {
            if (ctx.Sites.Count == 0) return;
            var all = EncounterCatalogue.All;
            for (var i = 0; i < all.Count; i++)
            {
                var def = all[i];
                var site = ctx.Sites.Find(def.Site);
                if (site == null) continue;
                var rec = st.Encounters.Ensure(def.Id);
                if (!rec.Resolved)
                {
                    TryResolve(ctx, st, def, site, rec);
                    continue;
                }
                if (rec.ClearedAt < 0 && Living(st, def.Id) == 0)
                {
                    rec.ClearedAt = st.T;
                    st.Events.Add(new EncounterClearedEvent(st.T, def.Id));
                }
                if (!rec.Claimed && Occupies(def)) Occupy(st, def, site, rec);
            }
        }

        /// <summary>This row is claimed by being held (§5.2), not on its last kill.</summary>
        public static bool Occupies(EncounterDef def) => def.OccupySeconds > 0 && !string.IsNullOrEmpty(def.Key);

        /// <summary>
        /// §5.2: the hold clock runs while the engineer, up, is within <see cref="EncounterDef.OccupyRadius"/> of the
        /// marker and no living guard of this camp is within <see cref="EncounterDef.GuardRadius"/> of it. Stepping
        /// out, going down or a guard coming back stops it, and it starts again from nothing.
        /// </summary>
        private static void Occupy(SimState st, EncounterDef def, SiteRecord site, EncounterRecord rec)
        {
            var marker = site.Centre;
            var p = st.Engineer;
            var holding = !p.IsDown
                && DirectorRules.Distance(p.Pos.X, p.Pos.Y, marker.X, marker.Y) <= def.OccupyRadius
                && !GuardNear(st, def.Id, marker, def.GuardRadius);
            if (!holding) { rec.OccupiedSince = -1; return; }
            if (rec.OccupiedSince < 0) rec.OccupiedSince = st.T;
            if (st.T - rec.OccupiedSince < def.OccupySeconds) return;

            rec.Claimed = true;
            rec.OccupiedSince = -1;
            if (!st.Encounters.Holds(def.Key)) st.Encounters.Pouch.Add(def.Key);
            var held = st.Encounters.Pouch.Count;
            var of = KeyCount();
            st.Events.Add(new EncounterClaimedEvent(st.T, def.Id, def.Key, held, of,
                def.Name + " secured — key " + held + " of " + of));
        }

        /// <summary>How many keys the catalogue hands out: one per key camp.</summary>
        public static int KeyCount()
        {
            var n = 0;
            var all = EncounterCatalogue.All;
            for (var i = 0; i < all.Count; i++) if (Occupies(all[i])) n++;
            return n;
        }

        /// <summary>A living guard of <paramref name="id"/> within <paramref name="radius"/> of <paramref name="at"/>.</summary>
        public static bool GuardNear(SimState st, string id, Vec2 at, double radius)
        {
            var actors = st.Enemies.Actors;
            for (var i = 0; i < actors.Count; i++)
            {
                var e = actors[i];
                if (e.Hp <= 0 || string.CompareOrdinal(e.Site, id) != 0) continue;
                if (DirectorRules.Distance(e.Pos.X, e.Pos.Y, at.X, at.Y) <= radius) return true;
            }
            return false;
        }

        /// <summary>The machine kind a cache crate is: the ordinary Supply chest, opened with the ordinary panel.</summary>
        public const string CrateKind = "chest";

        /// <summary>
        /// Where <paramref name="def"/>'s cache crate would stand: the free Supply-chest footprint nearest the marker
        /// that leaves the marker tile itself clear, so the engineer can still stand on the camp to hold it. Null
        /// when the camp carries no cache or nothing fits within <see cref="EncounterCatalogue.GroupSpreadTiles"/>.
        /// </summary>
        public static TileRect? CrateSpot(SimContext ctx, SimState st, EncounterDef def, SiteRecord site)
        {
            if (def.Cache == null || def.Cache.Count == 0) return null;
            if (!ctx.Data.TryMachine(CrateKind, out var spec)) return null;
            var (w, h) = Footprints.Dimensions(CrateKind, Dir.N, spec.Size);
            var mx = (int)Math.Floor(site.Centre.X);
            var my = (int)Math.Floor(site.Centre.Y);
            var reach = (int)EncounterCatalogue.GroupSpreadTiles;
            TileRect? best = null;
            var bestD = double.PositiveInfinity;
            for (var y = my - reach; y <= my + reach; y++)
                for (var x = mx - reach; x <= mx + reach; x++)
                {
                    var r = new TileRect(x, y, w, h);
                    if (r.Contains(mx, my)) continue;
                    var d = DirectorRules.Distance(x + w * .5, y + h * .5, mx + .5, my + .5);
                    if (d >= bestD) continue; // strictly nearer, so ties keep the first in row order
                    if (Placement.GeometryProblem(ctx, st, CrateKind, x, y, Dir.N).Length != 0) continue;
                    if (!Clear(ctx, st, r)) continue;
                    best = r;
                    bestD = d;
                }
            return best;
        }

        private static bool Clear(SimContext ctx, SimState st, TileRect r)
        {
            for (var y = r.Y; y < r.Y + r.H; y++)
                for (var x = r.X; x < r.X + r.W; x++)
                    if (!Ground.Passable(ctx, st, x, y)) return false;
            var actors = st.Enemies.Actors;
            for (var i = 0; i < actors.Count; i++)
                if (actors[i].Hp > 0 && r.Contains((int)Math.Floor(actors[i].Pos.X), (int)Math.Floor(actors[i].Pos.Y))) return false;
            return true;
        }

        /// <summary>How close the engineer must come for <paramref name="def"/> to be found.</summary>
        public static double ResolveTiles(EncounterDef def) =>
            def.Discovery == EncounterDiscovery.SearchArea
                ? EncounterCatalogue.SearchResolveTiles
                : EncounterCatalogue.ProximityTiles;

        /// <summary>Living bodies that guard <paramref name="id"/>.</summary>
        public static int Living(SimState st, string id)
        {
            var n = 0;
            var actors = st.Enemies.Actors;
            for (var i = 0; i < actors.Count; i++)
                if (actors[i].Hp > 0 && string.CompareOrdinal(actors[i].Site, id) == 0) n++;
            return n;
        }

        private static void TryResolve(SimContext ctx, SimState st, EncounterDef def, SiteRecord site, EncounterRecord rec)
        {
            var p = st.Engineer;
            if (p.IsDown) return;
            var marker = site.Centre;
            if (DirectorRules.Distance(p.Pos.X, p.Pos.Y, marker.X, marker.Y) > ResolveTiles(def)) return;
            var crate = CrateSpot(ctx, st, def, site);
            if (!TryPlace(ctx, st, def, site, out var places, crate)) return;

            for (var i = 0; i < places.Count; i++)
            {
                var (pos, g) = places[i];
                var key = EncounterCatalogue.KindOf(def, i);
                var hp = ctx.Data.TryEnemy(key, out var body) ? body.Hp : 20;
                var group = EncounterCatalogue.GroupBase + def.Serial * 100 + g;
                var id = st.Enemies.Next++;
                st.Enemies.Actors.Add(new Enemy
                {
                    Id = id,
                    Kind = key,
                    Pos = pos,
                    Hp = hp,
                    Layer = EnemyLayer.Site,
                    Group = group,
                    Site = def.Id,
                    Origin = (int)pos.Y * ctx.Geometry.Width + (int)pos.X,
                    Home = pos,
                    Aim = pos,
                    Phase = EnemyPhaseKind.Idle,
                    Waypoint = -1,
                });
                st.Events.Add(new EnemySpawnedEvent(st.T, id, key, pos.X, pos.Y, group));
            }
            if (crate.HasValue && rec.Cache == 0) rec.Cache = PutCrate(ctx, st, def, crate.Value);
            rec.Resolved = true;
            rec.ResolvedAt = st.T;
            rec.ClearedAt = -1;
            st.Events.Add(new EncounterResolvedEvent(st.T, def.Id, places.Count));
        }

        /// <summary>
        /// §5.3: the sim sets the crate down, bound to the camp, with the row's cache in it. The cache enters the
        /// game here, so it is counted in <see cref="Stats.Found"/> for the ledger.
        /// </summary>
        private static int PutCrate(SimContext ctx, SimState st, EncounterDef def, TileRect at)
        {
            var m = Placement.Add(ctx, st, CrateKind, at.X, at.Y, Dir.N);
            m.Site = def.Id;
            for (var i = 0; i < def.Cache.Count; i++)
            {
                var s = def.Cache[i];
                if (s.Count <= 0) continue;
                m.Inv.Add(s.Item, s.Count);
                st.Stats.Found.Add(s.Item, s.Count);
            }
            return m.Id;
        }

        /// <summary>
        /// The centres <paramref name="def"/>'s squads gather round: its site's <c>{Site}:group:N</c> records, in
        /// order, or the marker plus <see cref="EncounterDef.Offsets"/> when the site has none.
        /// </summary>
        public static List<Vec2> GroupCentres(SimContext ctx, EncounterDef def, SiteRecord site)
        {
            var list = new List<Vec2>();
            for (var n = 0; ; n++)
            {
                var g = ctx.Sites.Find(def.Site + ":group:" + n.ToString(System.Globalization.CultureInfo.InvariantCulture));
                if (g == null) break;
                list.Add(g.Centre);
            }
            if (list.Count > 0) return list;
            var m = site.Centre;
            if (def.Offsets == null || def.Offsets.Count == 0) { list.Add(m); return list; }
            for (var k = 0; k < def.Offsets.Count; k++) list.Add(new Vec2(m.X + def.Offsets[k].Dx, m.Y + def.Offsets[k].Dy));
            return list;
        }

        /// <summary>
        /// Where every body of <paramref name="def"/>'s garrison would stand, with the group it would join, or
        /// false when the whole garrison cannot be placed right now. Changes nothing.
        /// </summary>
        public static bool TryPlace(SimContext ctx, SimState st, EncounterDef def, SiteRecord site,
            out List<(Vec2 Pos, int Group)> places, TileRect? keepClear = null)
        {
            places = new List<(Vec2, int)>(def.Bodies);
            if (def.Bodies <= 0) return true;
            if (st.Enemies.Actors.Count + def.Bodies > ctx.Data.Raids.LivingBudget) return false;

            var centres = GroupCentres(ctx, def, site);
            var want = new int[centres.Count];
            for (var i = 0; i < def.Bodies; i++) want[i % centres.Count]++;

            var chosen = new List<Vec2>();
            var byGroup = new List<Vec2>[centres.Count];
            for (var g = 0; g < centres.Count; g++)
            {
                byGroup[g] = Gather(ctx, st, centres[g], want[g], chosen, keepClear);
                if (byGroup[g].Count < want[g]) { places.Clear(); return false; }
            }
            // Dealt round the groups in turn, so body i is in group i % groups — the same deal KindOf assumes.
            var next = new int[centres.Count];
            for (var i = 0; i < def.Bodies; i++)
            {
                var g = i % centres.Count;
                places.Add((byGroup[g][next[g]++], g));
            }
            return true;
        }

        private static List<Vec2> Gather(SimContext ctx, SimState st, Vec2 centre, int want, List<Vec2> chosen,
            TileRect? keepClear)
        {
            var got = new List<Vec2>(want);
            if (want <= 0) return got;
            var w = ctx.Geometry.Width;
            var cx = (int)Math.Floor(centre.X);
            var cy = (int)Math.Floor(centre.Y);
            if (!Ground.InBounds(ctx, cx, cy)) return got;
            var spread = EncounterCatalogue.GroupSpreadTiles;
            var queue = new List<int> { cy * w + cx };
            var seen = new HashSet<int> { cy * w + cx };
            for (var head = 0; head < queue.Count && got.Count < want; head++)
            {
                var t = queue[head];
                var x = t % w;
                var y = t / w;
                if (Ground.Passable(ctx, st, x, y) && !(keepClear?.Contains(x, y) ?? false) && Fits(st, x + .5, y + .5, chosen))
                {
                    var at = new Vec2(x + .5, y + .5);
                    got.Add(at);
                    chosen.Add(at);
                }
                for (var k = 0; k < 4; k++)
                {
                    var xx = x + Dirs.DX[k];
                    var yy = y + Dirs.DY[k];
                    if (!Ground.InBounds(ctx, xx, yy)) continue;
                    var q = yy * w + xx;
                    if (seen.Contains(q) || DirectorRules.Distance(xx, yy, cx, cy) > spread
                        || !Ground.Passable(ctx, st, xx, yy)) continue;
                    seen.Add(q);
                    queue.Add(q);
                }
            }
            return got;
        }

        private static bool Fits(SimState st, double x, double y, List<Vec2> chosen)
        {
            var p = st.Engineer.Pos;
            if (DirectorRules.Distance(p.X, p.Y, x, y) < EncounterCatalogue.NoSurpriseBirthTiles) return false;
            for (var i = 0; i < chosen.Count; i++)
                if (DirectorRules.Distance(chosen[i].X, chosen[i].Y, x, y) < 1) return false;
            var actors = st.Enemies.Actors;
            for (var i = 0; i < actors.Count; i++)
                if (DirectorRules.Distance(actors[i].Pos.X, actors[i].Pos.Y, x, y) < 1) return false;
            return true;
        }
    }
}
