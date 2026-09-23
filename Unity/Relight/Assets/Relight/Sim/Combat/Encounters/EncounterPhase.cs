using System;
using System.Collections.Generic;

namespace Relight.Sim
{
    /// <summary>An encounter's garrison was born (§5.1). <c>Bodies</c> is how many.</summary>
    public sealed record EncounterResolvedEvent(double T, string Id, int Bodies) : SimEvent(T);

    /// <summary>The last body of an encounter's garrison died.</summary>
    public sealed record EncounterClearedEvent(double T, string Id) : SimEvent(T);

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
            }
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
            if (!TryPlace(ctx, st, def, site, out var places)) return;

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
            rec.Resolved = true;
            rec.ResolvedAt = st.T;
            rec.ClearedAt = -1;
            st.Events.Add(new EncounterResolvedEvent(st.T, def.Id, places.Count));
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
            out List<(Vec2 Pos, int Group)> places)
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
                byGroup[g] = Gather(ctx, st, centres[g], want[g], chosen);
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

        private static List<Vec2> Gather(SimContext ctx, SimState st, Vec2 centre, int want, List<Vec2> chosen)
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
                if (Ground.Passable(ctx, st, x, y) && Fits(st, x + .5, y + .5, chosen))
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
