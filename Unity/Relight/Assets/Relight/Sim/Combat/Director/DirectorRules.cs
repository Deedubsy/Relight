using System;
using System.Collections.Generic;

namespace Relight.Sim
{
    /// <summary>
    /// The pure geometry and arithmetic of the raid director: the deterministic choice function, the traversability
    /// predicate the BFS uses, where a wave enters from, and how one body is born. Nothing here holds state; every
    /// function is a function of (context, state, arguments), so the same save always produces the same raid.
    /// </summary>
    public static class DirectorRules
    {
        /// <summary>
        /// Reference campaignThreat.ts:29 verbatim:
        /// <c>((Math.imul(seed+17,1103515245) ^ Math.imul(serial+31,2654435761)) &gt;&gt;&gt; 0) % (range+1)</c>.
        ///
        /// Deterministic WITHOUT <see cref="SimState.Rng"/> — that is the point: the schedule is a pure function of
        /// the game seed and a saved serial, so loading a save cannot reroll it and replaying cannot drift it.
        /// <c>Math.imul</c> is a wrapping 32-bit multiply, which is a plain multiply inside <c>unchecked</c>;
        /// <c>&gt;&gt;&gt; 0</c> reinterprets the result as unsigned before the modulus.
        /// </summary>
        public static int RaidChoice(int seed, int serial, int range)
        {
            if (range < 0) return 0;
            unchecked
            {
                var a = (int)((uint)(seed + 17) * 1103515245u);
                var b = (int)((uint)(serial + 31) * 2654435761u);
                return (int)((uint)(a ^ b) % (uint)(range + 1));
            }
        }

        /// <summary>
        /// Reference campaignThreat.ts:91 <c>hostileOpen</c>. On an imported region (a "city map" in the reference)
        /// this is plain passability; on the synthetic map the reference's older rule applies, under which
        /// production equipment is traversable to creatures and only a paid defence blocks — the comment there is
        /// "Protected production equipment is traversable to creatures; it cannot substitute for paid walls."
        ///
        /// Either way a defence whose hit points have reached 0 no longer blocks, which is the reference's
        /// <c>solidMap</c> rule (walk.ts:37) that <see cref="GroundState.SolidMap"/> could not implement in Phase B
        /// because structure HP did not exist yet. C-04 owns that HP, so the rule is applied here.
        /// </summary>
        public static bool HostileOpen(SimContext ctx, SimState st, int x, int y)
        {
            if (!Ground.Walkable(ctx, x, y)) return false;
            var m = st.Enemies.Index.At(ctx, st, x, y);
            if (m == null) return true;
            if (Ground.WalkThrough(m.Kind)) return true;
            var defence = TurretRules.IsDefence(ctx.Data, m);
            if (defence) return TurretRules.Hp(ctx.Data, st, m) <= 0;
            // No map id means the synthetic Phase B map, where the reference takes the non-city branch.
            return string.IsNullOrEmpty(ctx.MapId);
        }

        /// <summary>The raid's destination rect: the Home core when the region has one.</summary>
        public static bool Target(SimContext ctx, SimState st, out int x, out int y, out int size) =>
            EnemyCoreHook.Rect(ctx, st, out x, out y, out size);

        /// <summary>
        /// The <see cref="SiteKind.RaidLine"/> gate row: Home raid entry tiles must be at or below it (reference
        /// campaignThreat.ts:132 <c>y &lt; RIVERFRONT.homeRaidY</c>). <see cref="int.MinValue"/> when the region has
        /// no raid line, which disables the filter rather than rejecting every tile.
        /// </summary>
        public static int RaidLineY(SimContext ctx)
        {
            var best = int.MinValue;
            if (ctx.Sites == null) return best;
            foreach (var s in ctx.Sites.OfKind(SiteKind.RaidLine)) if (s.Y > best) best = s.Y;
            return best;
        }

        // ---------------------------------------------------------------- entry tiles

        /// <summary>The nearest a wave may enter, in BFS steps from the core. Below this it is already inside.</summary>
        public const int EntryNearSteps = 8;

        /// <summary>The furthest a wave may enter. Past this the walk in is a wait, not a raid (fair travel, GP-W3).</summary>
        public const int EntryFarSteps = 76;

        /// <summary>A wave never enters this close to the engineer (reference campaignThreat.ts:132's 28 tiles).</summary>
        public const double SafeFromEngineerTiles = 28;

        /// <summary>
        /// Can a wave enter the map here? ONE predicate, shared by <see cref="Origin"/>, <see cref="Approaches"/>
        /// and <see cref="Staging"/>, which is the whole point: before GP-W3 the three disagreed, so the director
        /// could announce an approach that staging then rejected, and the assault it had already told the player
        /// about was dropped at the moment it was due. A tile this returns true for is one <see cref="Staging"/>
        /// accepts unchanged, so choosing an approach IS validating it.
        ///
        /// <paramref name="lo"/>/<paramref name="hi"/> are the BFS band the caller wants; everything else is fixed:
        /// on the map, reachable, standable, at or beyond the region's raid line, and not on top of the player.
        /// </summary>
        public static bool EntryTile(SimContext ctx, SimState st, RaidField fld, int line, int x, int y, int lo, int hi)
        {
            if (!Ground.InBounds(ctx, x, y)) return false;
            var d = fld.At(x, y);
            if (d < lo || d > hi) return false;
            if (!Ground.Passable(ctx, st, x, y)) return false;
            if (line != int.MinValue && y < line) return false;
            return Distance(st.Engineer.Pos.X, st.Engineer.Pos.Y, x + .5, y + .5) >= SafeFromEngineerTiles;
        }

        /// <summary>
        /// L-02, ALWAYS_DARK_SPEC §5.2: what light adds to an entry tile's score, in the score's own unit (100 per
        /// step off the ideal distance). It is the part of the cheapest route's cost that light put there
        /// (<see cref="RaidField.LightPenalty"/>), plus the same charge for standing on a lit entry tile. It only
        /// ever ranks tiles INSIDE a tier: the tiers are written in plain steps and do not look at light, so light
        /// can move where a wave enters but can never leave it without an entry.
        /// </summary>
        private static double LightScore(SimContext ctx, SimState st, RaidField fld, int x, int y)
        {
            if (!fld.Weighted) return 0;
            var extra = fld.LightPenalty(x, y);
            if (LightQueries.LitAt(st, x, y)) extra += Math.Max(0, ctx.Data.Raids.LitStepCost - 1);
            return extra * 100.0;
        }

        /// <summary>
        /// How good an entry tile this is for a single wave: 0 is the reference's own 20–60 step window, and 1 and 2
        /// are the map saying it has nothing that good. <see cref="int.MaxValue"/> means "not an entry tile".
        ///
        /// The tiers exist because the alternative — what the code did before GP-W3 — was to give up and hand back
        /// <see cref="FallbackOrigin"/>, a map-edge tile near the player's spawn that is not on any approach to the
        /// base, may be the wrong side of the raid line and may not even reach the core. The brief calls that an
        /// invalid fallback. A cramped map now yields a worse but real approach, and a map with none at all yields
        /// -1 and an honest "no reachable approach" notice.
        /// </summary>
        private static int OriginTier(SimContext ctx, SimState st, RaidField fld, int line, int x, int y)
        {
            if (EntryTile(ctx, st, fld, line, x, y, 20, 60)) return 0;
            if (EntryTile(ctx, st, fld, line, x, y, 12, EntryFarSteps)) return 1;
            if (EntryTile(ctx, st, fld, line, x, y, EntryNearSteps, EntryFarSteps)) return 2;
            return int.MaxValue;
        }

        /// <summary>As <see cref="OriginTier"/>, for a sector approach: further out, and clear of placed defences.</summary>
        private static int ApproachTier(SimContext ctx, SimState st, RaidField fld, int line, int x, int y)
        {
            if (EntryTile(ctx, st, fld, line, x, y, 32, EntryFarSteps) && !NearDefence(ctx, st, x, y, 12)) return 0;
            if (EntryTile(ctx, st, fld, line, x, y, 20, EntryFarSteps) && !NearDefence(ctx, st, x, y, 8)) return 1;
            if (EntryTile(ctx, st, fld, line, x, y, EntryNearSteps, EntryFarSteps)) return 2;
            return int.MaxValue;
        }

        /// <summary>
        /// Reference campaignThreat.ts:125 <c>campaignOrigin</c>: an exterior tile 20–60 BFS steps from the core,
        /// empty, at least 28 tiles from the engineer, scored <c>|d-24|*100 + euclid(entrance)</c> so a wave enters
        /// at a consistent distance by the shortest approach. Returns a global tile index, or -1.
        ///
        /// GP-W3 fixed the scan box. It used to be the core's own ±30 tiles, which cannot contain a 60-step
        /// approach at all and, on a region whose <see cref="SiteKind.RaidLine"/> sits well south of the core, does
        /// not overlap the half of the map the raid line allows — the band and the filter were disjoint, every tile
        /// was rejected, and the director fell back to a map-edge tile. The box is now derived from the band
        /// (<see cref="EntryFarSteps"/>), which is what "use the actual map" means here.
        ///
        /// 2026-09-18: a tile whose tier is <see cref="int.MaxValue"/> is skipped outright. Before this the loop
        /// only compared tiers, so when NO tile qualified the first cheapest non-entry tile "won" — on Founders
        /// Court that was (62, 347), three tiles west of the Home workshop, and the introductory attack was born
        /// inside the fence. The <see cref="RaidField"/> box was the reason no tile qualified (see its remarks);
        /// this guard is what keeps a future map fault an honest -1 instead of an interior spawn.
        /// </summary>
        public static int Origin(SimContext ctx, SimState st)
        {
            if (!Target(ctx, st, out var bx, out var by, out var size)) return FallbackOrigin(ctx, st);
            var w = ctx.Geometry.Width;
            var h = ctx.Geometry.Height;
            var fld = st.Director.Fields.Field(ctx, st, bx, by, size, true);
            var line = RaidLineY(ctx);
            var best = -1;
            var bestTier = int.MaxValue;
            var score = double.PositiveInfinity;
            var reach = EntryFarSteps + 1;
            for (var y = Math.Max(0, by - reach); y < Math.Min(h, by + size + reach); y++)
                for (var x = Math.Max(0, bx - reach); x < Math.Min(w, bx + size + reach); x++)
                {
                    var tier = OriginTier(ctx, st, fld, line, x, y);
                    if (tier == int.MaxValue || tier > bestTier) continue;
                    var s = Math.Abs(fld.At(x, y) - 24) * 100 + LightScore(ctx, st, fld, x, y) + Distance(x, y, bx, by);
                    if (tier == bestTier && s >= score) continue;
                    bestTier = tier;
                    score = s;
                    best = y * w + x;
                }
            return best;
        }

        /// <summary>
        /// The approaches a major roster rotates through: one tile per compass sector, ordered N, E, S, W so the
        /// rotation is stable across runs, each of them an entry tile <see cref="Staging"/> accepts as it stands.
        ///
        /// GP-W3 changed how authored <see cref="SiteKind.Camp"/> sites take part. They used to REPLACE the map
        /// scan outright, with no distance band at all, so a camp three hundred tiles away became a legal approach
        /// and the "raid" was a four-minute walk; and because a camp is an expedition destination first, a region
        /// that authored camps lost every other approach to the base. A camp is now offered on the same terms as
        /// any other tile and wins its sector only when it is genuinely on a fair approach — in which case it is
        /// strictly preferred, which is the Sites contract's intent. A camp that is not gets no say.
        /// </summary>
        public static int[] Approaches(SimContext ctx, SimState st)
        {
            if (!Target(ctx, st, out var bx, out var by, out var size))
            {
                var only = FallbackOrigin(ctx, st);
                return only < 0 ? Array.Empty<int>() : new[] { only };
            }
            var w = ctx.Geometry.Width;
            var h = ctx.Geometry.Height;
            var fld = st.Director.Fields.Field(ctx, st, bx, by, size, true);
            var cx = bx + size / 2.0;
            var cy = by + size / 2.0;
            var line = RaidLineY(ctx);
            var tile = new int[4];
            var tier = new int[4];
            var score = new double[4];
            for (var i = 0; i < 4; i++) { tile[i] = -1; tier[i] = int.MaxValue; score[i] = double.PositiveInfinity; }

            void Offer(int x, int y, bool authored)
            {
                var t = ApproachTier(ctx, st, fld, line, x, y);
                if (t == int.MaxValue) return;
                if (authored && t == 0) t = -1;              // an authored camp on a fair approach IS the approach
                var sector = Sector(x - cx, y - cy);
                if (t > tier[sector]) return;
                var s = Math.Abs(fld.At(x, y) - 42) * 100 + LightScore(ctx, st, fld, x, y) + Distance(x + .5, y + .5, cx, cy);
                if (t == tier[sector] && s >= score[sector]) return;
                tier[sector] = t;
                score[sector] = s;
                tile[sector] = y * w + x;
            }

            var reach = EntryFarSteps + 1;
            for (var y = Math.Max(0, by - reach); y < Math.Min(h, by + size + reach); y++)
                for (var x = Math.Max(0, bx - reach); x < Math.Min(w, bx + size + reach); x++)
                    Offer(x, y, false);

            if (ctx.Sites != null)
                foreach (var camp in ctx.Sites.OfKind(SiteKind.Camp))
                    Offer((int)Math.Floor(camp.Centre.X), (int)Math.Floor(camp.Centre.Y), true);

            var found = new List<int>();
            for (var i = 0; i < 4; i++) if (tile[i] >= 0) found.Add(tile[i]);
            if (found.Count > 0) return found.ToArray();
            // No sector qualified — take whatever single entry tile the map does have, and say nothing it cannot keep.
            var one = Origin(ctx, st);
            return one < 0 ? Array.Empty<int>() : new[] { one };
        }

        /// <summary>
        /// Reference campaignThreat.ts:158 <c>campaignStaging</c>: keep the advertised compass approach, and if the
        /// exact entry tile is obstructed search at most 12 steps outward for one that is no closer to the core, so
        /// a wave can never bypass the defences it was meant to walk into. -1 when nothing qualifies.
        /// </summary>
        public static int Staging(SimContext ctx, SimState st, int origin)
        {
            if (origin < 0) return -1;
            if (!Target(ctx, st, out var bx, out var by, out var size)) return Ground.Passable(ctx, st, origin % ctx.Geometry.Width, origin / ctx.Geometry.Width) ? origin : -1;
            var w = ctx.Geometry.Width;
            var toCore = st.Director.Fields.Field(ctx, st, bx, by, size, true);
            var ox = origin % w;
            var oy = origin / w;
            var line = RaidLineY(ctx);
            var approach = Heading(ox - bx, oy - by);

            bool Valid(int x, int y) =>
                EntryTile(ctx, st, toCore, line, x, y, EntryNearSteps, EntryFarSteps)
                && Heading(x - bx, y - by) == approach;

            if (Valid(ox, oy)) return origin;

            var queue = new List<int> { origin };
            var steps = new List<int> { 0 };
            var seen = new HashSet<int> { origin };
            var best = -1;
            var bestSteps = int.MaxValue;
            for (var head = 0; head < queue.Count; head++)
            {
                var t = queue[head];
                var d = steps[head];
                if (d > bestSteps) break;
                var x = t % w;
                var y = t / w;
                if (toCore.At(x, y) >= toCore.At(ox, oy) && Valid(x, y) && (d < bestSteps || t < best)) { best = t; bestSteps = d; }
                if (d >= 12) continue;
                for (var k = 0; k < 4; k++)
                {
                    var xx = x + Dirs.DX[k];
                    var yy = y + Dirs.DY[k];
                    if (!Ground.InBounds(ctx, xx, yy)) continue;
                    var q = yy * w + xx;
                    if (seen.Contains(q) || !HostileOpen(ctx, st, xx, yy)) continue;
                    seen.Add(q);
                    queue.Add(q);
                    steps.Add(d + 1);
                }
            }
            return best;
        }

        /// <summary>
        /// The compass words of a set of approach tiles, distinct and in sector order, as the warning says them:
        /// "N", "N and E", "N, E and S". "·" when there is nothing to name.
        /// </summary>
        public static string HeadingsOf(SimContext ctx, SimState st, int[] origins)
        {
            if (origins == null || origins.Length == 0) return "·";
            if (!Target(ctx, st, out var bx, out var by, out var size)) return "·";
            var w = ctx.Geometry.Width;
            var cx = bx + size / 2.0;
            var cy = by + size / 2.0;
            var words = new List<string>();
            for (var i = 0; i < origins.Length; i++)
            {
                if (origins[i] < 0) continue;
                var word = HeadingWord(Heading(origins[i] % w + .5 - cx, origins[i] / w + .5 - cy));
                if (word != "·" && !words.Contains(word)) words.Add(word);
            }
            if (words.Count == 0) return "·";
            if (words.Count == 1) return words[0];
            var head = string.Join(", ", words.GetRange(0, words.Count - 1).ToArray());
            return head + " and " + words[words.Count - 1];
        }

        /// <summary>
        /// Reference campaignThreat.ts:190 <c>birth</c>, campaign-gameplay branch only (the legacy crawler/shade/
        /// breaker/conductor roster is retired, CONTENT_CATALOGUE.md §17). A body is placed on the first passable
        /// tile found by a bounded BFS out of the entry tile that no other body is standing on and that is not next
        /// to the engineer. Returns the new enemy's id, or 0 when nowhere would take it.
        ///
        /// The roster rule is the reference's exactly: it reads the counter AFTER the id was taken, so with ids
        /// 1, 2, 3 … the spitters are 2, 5, 8 … — <c>!basic &amp;&amp; next % 3 == 0</c>.
        /// </summary>
        /// <param name="kind">
        /// GP-W4: the roster key to birth, for a caller that has already decided the composition — a major
        /// assault's wave plan names every body, so it cannot be left to the legacy every-third-body rule. Null
        /// keeps that rule, which is what minor raids and C-09's scripted group still use.
        /// </param>
        public static int Birth(SimContext ctx, SimState st, int origin, int layer, int group, bool basic,
            string kind = null)
        {
            if (origin < 0) return 0;
            var w = ctx.Geometry.Width;
            var sx = origin % w;
            var sy = origin / w;
            if (!Place(ctx, st, origin, layer, ref sx, ref sy)) return 0;

            var id = st.Enemies.Next++;
            var spitter = !basic && st.Enemies.Next % 3 == 0;
            var key = string.IsNullOrEmpty(kind) ? (spitter ? "spitter" : "skitter") : kind;
            var hp = ctx.Data.TryEnemy(key, out var def) ? def.Hp : 20;
            var body = new Enemy
            {
                Id = id,
                Kind = key,
                Pos = new Vec2(sx + .5, sy + .5),
                Hp = hp,
                Layer = layer,
                Group = group,
                Origin = origin,
                Home = new Vec2(sx + .5, sy + .5),
                Aim = new Vec2(sx + .5, sy + .5),
                Phase = EnemyPhaseKind.Idle,
                Waypoint = -1,
            };
            st.Enemies.Actors.Add(body);
            st.Events.Add(new EnemySpawnedEvent(st.T, id, key, body.Pos.X, body.Pos.Y, group));
            return id;
        }

        private static bool Place(SimContext ctx, SimState st, int origin, int layer, ref int sx, ref int sy)
        {
            var w = ctx.Geometry.Width;
            var queue = new List<int> { origin };
            var seen = new HashSet<int> { origin };
            var ox = origin % w;
            var oy = origin / w;
            for (var head = 0; head < queue.Count && head < 100; head++)
            {
                var t = queue[head];
                var x = t % w;
                var y = t / w;
                if (Ground.Passable(ctx, st, x, y)
                    && (layer == EnemyLayer.Site || Distance(st.Engineer.Pos.X, st.Engineer.Pos.Y, x + .5, y + .5) >= 28)
                    && !Crowded(st, x + .5, y + .5, 1))
                {
                    sx = x; sy = y; return true;
                }
                for (var k = 0; k < 4; k++)
                {
                    var xx = x + Dirs.DX[k];
                    var yy = y + Dirs.DY[k];
                    if (!Ground.InBounds(ctx, xx, yy)) continue;
                    var q = yy * w + xx;
                    if (seen.Contains(q) || Distance(xx, yy, ox, oy) >= 6 || !Ground.Passable(ctx, st, xx, yy)) continue;
                    seen.Add(q);
                    queue.Add(q);
                }
            }
            return false;
        }

        private static bool Crowded(SimState st, double x, double y, double r)
        {
            for (var i = 0; i < st.Enemies.Actors.Count; i++)
            {
                var o = st.Enemies.Actors[i];
                if (Distance(o.Pos.X, o.Pos.Y, x, y) < r) return true;
            }
            return false;
        }

        private static bool NearDefence(SimContext ctx, SimState st, int x, int y, double r)
        {
            for (var i = 0; i < st.Machines.Count; i++)
            {
                var m = st.Machines[i];
                // GP-W5: FORTIFICATIONS, not "anything with hit points". CombatBalance gave every buildable
                // machine an integrity row, so IsDefence now answers true for a supply chest, and staging that
                // avoided chests would reject every near tile and push the assault out to the worst tier.
                if (!TurretRules.IsFortification(ctx.Data, m)) continue;
                if (Distance(m.X, m.Y, x, y) < r) return true;
            }
            return false;
        }

        /// <summary>Reference sector split: the dominant axis decides. 0 N, 1 E, 2 S, 3 W (Y-down).</summary>
        public static int Sector(double dx, double dy) =>
            Math.Abs(dx) > Math.Abs(dy) ? (dx > 0 ? 1 : 3) : (dy > 0 ? 2 : 0);

        /// <summary>Reference move.ts <c>headingWord</c> as an index: 0 E, 1 SE, 2 S, 3 SW, 4 W, 5 NW, 6 N, 7 NE; -1 for none.</summary>
        public static int Heading(double dx, double dy)
        {
            if (Math.Sqrt(dx * dx + dy * dy) < 1e-6) return -1;
            return (int)Math.Round(Math.Atan2(dy, dx) / (Math.PI / 4)) & 7;
        }

        private static readonly string[] HeadingWords = { "E", "SE", "S", "SW", "W", "NW", "N", "NE" };

        /// <summary>The compass word for a heading index, "·" for none.</summary>
        public static string HeadingWord(int heading) => heading < 0 ? "·" : HeadingWords[heading & 7];

        /// <summary>
        /// The no-core, no-sites fallback the brief requires: the walkable map-edge tile nearest the engineer's
        /// spawn. It exists so the synthetic map and every pre-C-01 test can still run a raid instead of throwing;
        /// <see cref="DirectorQueries.UsingFallbackApproach"/> reports when it is in use.
        /// </summary>
        public static int FallbackOrigin(SimContext ctx, SimState st)
        {
            var w = ctx.Geometry.Width;
            var h = ctx.Geometry.Height;
            var from = ctx.Geometry.Spawn;
            var best = -1;
            var score = double.PositiveInfinity;
            void Try(int x, int y)
            {
                if (!Ground.Passable(ctx, st, x, y)) return;
                var s = Distance(x + .5, y + .5, from.X, from.Y);
                if (s >= score) return;
                score = s;
                best = y * w + x;
            }
            for (var x = 0; x < w; x++) { Try(x, 0); Try(x, h - 1); }
            for (var y = 0; y < h; y++) { Try(0, y); Try(w - 1, y); }
            return best;
        }

        public static double Distance(double ax, double ay, double bx, double by)
        {
            var dx = ax - bx;
            var dy = ay - by;
            return Math.Sqrt(dx * dx + dy * dy);
        }
    }
}
