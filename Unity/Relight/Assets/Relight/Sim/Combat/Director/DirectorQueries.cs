using System;

namespace Relight.Sim
{
    /// <summary>Why the director spoke. The HUD picks its wording from this, never by parsing the text.</summary>
    public enum RaidNoticeKind { Announced = 0, Skipped = 1, Deferred = 2, MinorRaid = 3, Reserved = 4, Released = 5 }

    /// <summary>
    /// A director notice, for the alert feed (C-07) and the guide (C-09). <see cref="Text"/> carries the reference's
    /// own strings where the reference has one; the deferral notice is new (the reference has none — U-D-26).
    /// </summary>
    public sealed record RaidNoticeEvent(double T, RaidNoticeKind Kind, string Text, int RaidId) : SimEvent(T);

    /// <summary>What the HUD needs to draw the raid warning strip.</summary>
    public readonly struct RaidWarning
    {
        /// <summary>"", "warning", "assault", "withdrawal", "minor raid" or "recovery" (reference <c>ThreatPhase</c>).</summary>
        public readonly string Kind;
        /// <summary>Seconds until the assault starts; 0 once it has.</summary>
        public readonly double SecondsLeft;
        /// <summary>Compass word of the approach, or "·" when there is no located threat.</summary>
        public readonly string Direction;
        /// <summary>The director's last notice, or "".</summary>
        public readonly string Notice;
        /// <summary>Tile centre the marker sits on; (0,0) when <see cref="Kind"/> is "".</summary>
        public readonly Vec2 At;
        /// <summary>
        /// The player-facing noun for what is coming — "Raid", "Major assault" — so the HUD never has to guess at
        /// the scale of an encounter from <see cref="Kind"/> and cannot describe an assault as a small group
        /// (GP-W3: what is announced must be what arrives). "" when there is nothing to name.
        /// </summary>
        public readonly string Label;

        public RaidWarning(string kind, double secondsLeft, string direction, string notice, Vec2 at, string label = "")
        {
            Kind = kind; SecondsLeft = secondsLeft; Direction = direction; Notice = notice; At = at; Label = label;
        }
    }

    /// <summary>
    /// Read-only views of the director for presentation and for C-09. Nothing here mutates; the phase alone
    /// advances the clock. Port of campaignThreat.ts <c>knownCampaignThreat</c> / <c>campaignWarning</c> /
    /// <c>openingDirection</c>, reduced to the single Home target the port keeps.
    /// </summary>
    public static class DirectorQueries
    {
        /// <summary>Reference <c>knownCampaignThreat</c> + <c>campaignWarning</c>, as data rather than a sentence.</summary>
        public static RaidWarning Warning(SimContext ctx, SimState st)
        {
            var d = st.Director;
            var w = ctx.Geometry.Width;
            DirectorRules.Target(ctx, st, out var bx, out var by, out var size);
            var cx = bx + size / 2.0;
            var cy = by + size / 2.0;

            var m = d.Minor;
            if (m != null)
            {
                var at = new Vec2(m.Origin % w + .5, m.Origin / w + .5);
                // GP-W3: a warned raid is a countdown, not a wave. It reports the time its bodies are actually due
                // and the heading it was announced on, so the strip cannot claim an attack that has not arrived.
                var label = m.Scripted ? "Small enemy group" : "Raid";
                if (!m.Spawned)
                    return new RaidWarning("warning", Math.Max(0, m.StartsAt - st.T), Direction(at, cx, cy), d.Notice, at, label);
                return new RaidWarning(m.Retreat ? "withdrawal" : "minor raid", 0,
                    Direction(at, cx, cy), d.Notice, at, label);
            }
            var a = d.Major;
            if (a != null)
            {
                var at = new Vec2(a.Origin % w + .5, a.Origin / w + .5);
                var kind = a.Retreat ? "withdrawal" : st.T >= a.StartsAt ? "assault" : "warning";
                return new RaidWarning(kind, Math.Max(0, a.StartsAt - st.T), Direction(at, cx, cy), d.Notice, at, "Major assault");
            }
            if (st.T < d.RecoveryUntil)
                return new RaidWarning("recovery", 0, "·", d.Notice, new Vec2(cx, cy));
            return new RaidWarning("", Math.Max(0, d.NextStart - st.T), "·", d.Notice, new Vec2(0, 0));
        }

        /// <summary>Compass word for the heading from the core to a point (reference <c>openingDirection</c>).</summary>
        public static string Direction(Vec2 at, double cx, double cy) =>
            DirectorRules.HeadingWord(DirectorRules.Heading(at.X - cx, at.Y - cy));

        /// <summary>Absolute sim second the next major assault begins.</summary>
        public static double NextMajorAt(SimState st) => st.Director.NextStart;

        /// <summary>Seconds until the next major; 0 once it has started.</summary>
        public static double SecondsToMajor(SimState st) => Math.Max(0, st.Director.NextStart - st.T);

        /// <summary>The director's last notice, or "".</summary>
        public static string Notice(SimState st) => st.Director.Notice;

        /// <summary>Bodies alive in the current major wave, the current minor wave and in total.</summary>
        public static void Live(SimState st, out int major, out int minor, out int total)
        {
            major = 0; minor = 0; total = st.Enemies.Actors.Count;
            for (var i = 0; i < st.Enemies.Actors.Count; i++)
            {
                var layer = st.Enemies.Actors[i].Layer;
                if (layer == EnemyLayer.Major) major++;
                else if (layer == EnemyLayer.Minor) minor++;
            }
        }

        /// <summary>
        /// True when the region gave the director no authored approaches and it is walking map-edge tiles instead
        /// (<see cref="WorldSites.Empty"/>, i.e. the synthetic test map). The coordinator needs to see this: a raid
        /// on the synthetic map is exercising the fallback, not the authored Home approaches.
        /// </summary>
        public static bool UsingFallbackApproach(SimContext ctx) =>
            ctx.Sites == null || !Any(ctx.Sites.OfKind(SiteKind.Camp));

        private static bool Any(System.Collections.Generic.IEnumerable<SiteRecord> list)
        {
            foreach (var s in list) return true;
            return false;
        }

        /// <summary>
        /// The approach the prepared turret covers best — reference campaignThreat.ts:253 <c>openingOrigin</c>,
        /// scored by <c>approachClearance</c> (the closest the shortest breach path from that origin to the core
        /// passes to the turret's centre). C-09 calls this to place its introductory group. -1 when there is none.
        /// </summary>
        public static int OpeningOrigin(SimContext ctx, SimState st, int turretId)
        {
            var m = st.MachineById(turretId);
            if (m == null) return -1;
            if (!DirectorRules.Target(ctx, st, out var bx, out var by, out var size)) return -1;
            var best = -1;
            var score = double.PositiveInfinity;
            Consider(ctx, st, DirectorRules.Origin(ctx, st), m, bx, by, size, ref best, ref score);
            var list = DirectorRules.Approaches(ctx, st);
            if (list != null)
                for (var i = 0; i < list.Length; i++)
                    Consider(ctx, st, list[i], m, bx, by, size, ref best, ref score);
            return best;
        }

        private static void Consider(SimContext ctx, SimState st, int origin, Machine m,
            int bx, int by, int size, ref int best, ref double score)
        {
            if (origin < 0) return;
            var c = Clearance(ctx, st, origin, m, bx, by, size);
            if (c >= score) return;
            score = c;
            best = origin;
        }

        /// <summary>
        /// Reference campaignThreat.ts:241 <c>approachClearance</c>: walk the breach field down from
        /// <paramref name="origin"/> to the core and report the closest that path comes to the machine's centre.
        /// </summary>
        public static double Clearance(SimContext ctx, SimState st, int origin, Machine m, int bx, int by, int size)
        {
            var fld = st.Director.Fields.Field(ctx, st, bx, by, size, true);
            var w = ctx.Geometry.Width;
            var cx = m.X + m.Size / 2.0;
            var cy = m.Y + m.Size / 2.0;
            var t = origin;
            var best = double.PositiveInfinity;
            if (fld.AtTile(t, w) < 0) return double.PositiveInfinity;
            for (var guard = 0; guard < 400; guard++)
            {
                var x = t % w;
                var y = t / w;
                var here = fld.At(x, y);
                var dist = DirectorRules.Distance(x + .5, y + .5, cx, cy);
                if (dist < best) best = dist;
                if (here == 0) break;
                var next = -1;
                var nd = here;
                for (var k = 0; k < 4; k++)
                {
                    var xx = x + Dirs.DX[k];
                    var yy = y + Dirs.DY[k];
                    if (!Ground.InBounds(ctx, xx, yy)) continue;
                    var dq = fld.At(xx, yy);
                    if (dq < 0 || dq >= nd) continue;
                    nd = dq;
                    next = yy * w + xx;
                }
                if (next < 0) break;
                t = next;
            }
            return best;
        }
    }
}
