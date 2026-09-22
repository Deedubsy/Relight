using System;

namespace Relight.Sim
{
    /// <summary>Why the director spoke. The HUD picks its wording from this, never by parsing the text.</summary>
    /// <remarks><see cref="CoreHeld"/> (REL-75, U-D-68 b): the first small raid reached the Home core's floor and is breaking off.</remarks>
    public enum RaidNoticeKind { Announced = 0, Skipped = 1, Deferred = 2, MinorRaid = 3, Reserved = 4, Released = 5, CoreHeld = 6 }

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
        /// <summary>The raid this describes (REL-74), so the HUD can find its bodies; 0 when there is none.</summary>
        public readonly int RaidId;

        public RaidWarning(string kind, double secondsLeft, string direction, string notice, Vec2 at, string label = "",
            int raidId = 0)
        {
            Kind = kind; SecondsLeft = secondsLeft; Direction = direction; Notice = notice; At = at; Label = label;
            RaidId = raidId;
        }
    }

    /// <summary>
    /// Read-only views of the director for presentation and for C-09. Nothing here mutates; the phase alone
    /// advances the clock. Port of campaignThreat.ts <c>knownCampaignThreat</c> / <c>campaignWarning</c> /
    /// <c>openingDirection</c>, reduced to the single Home target the port keeps.
    /// </summary>
    /// <summary>
    /// E-19: the director's state as the Admin page shows it. Data, not a sentence, so a test can hold it against
    /// the sim; <see cref="DirectorQueries.Describe"/> is the one place it becomes text.
    /// </summary>
    /// <param name="Phase">waiting · held · recovery · warned · assault · withdrawing.</param>
    /// <param name="PhaseSeconds">Seconds left in that phase (to the start when warned, to the planned end in an assault); 0 when it has no clock.</param>
    /// <param name="NextStartIn">Seconds to <see cref="DirectorState.NextStart"/>, the clock's next large-raid second.</param>
    /// <param name="Survived">The number the next booked raid is sized by (<see cref="DirectorPhase.GrowthStep"/>).</param>
    /// <param name="Pinned">True when that number is an Admin pin rather than the history.</param>
    /// <param name="LastOutcome">A <see cref="RaidOutcome"/>, or -1 when no large raid has ended.</param>
    public sealed record DirectorReadout(string Phase, double PhaseSeconds, double NextStartIn, string Hold,
        bool HasTarget, int TargetX, int TargetY, double CoreHp, double CoreMaxHp,
        int Survived, bool Pinned, int NextBodies,
        int Wave, int Waves, int Remaining, int Cancelled,
        string Minor, int MajorAlive, int MinorAlive, int Living, int ActiveBudget, int LivingBudget,
        int LastId, int LastOutcome,
        string Pacing = "", int CycleMinors = 0, int MinorsPerCycle = 0);

    public static class DirectorQueries
    {
        /// <summary>E-19: everything the Admin director page shows, read straight from the sim.</summary>
        public static DirectorReadout Readout(SimContext ctx, SimState st)
        {
            var d = st.Director;
            var r = ctx.Data.Raids;
            var a = d.Major;
            var has = DirectorRules.Target(ctx, st, out var bx, out var by, out _);
            Live(st, out var major, out var minor, out var total);

            string phase; double left = 0;
            if (a != null && a.Committed && a.Retreat) phase = "withdrawing";
            else if (a != null && a.Committed) { phase = "assault"; left = Math.Max(0, a.EndsAt - st.T); }
            else if (a != null) { phase = "warned"; left = Math.Max(0, a.StartsAt - st.T); }
            else if (st.T < d.RecoveryUntil) { phase = "recovery"; left = d.RecoveryUntil - st.T; }
            else if (st.T < d.QuietUntil) { phase = "quiet"; left = d.QuietUntil - st.T; }     // REL-75
            else if (d.Reserved) phase = "held";
            else phase = "waiting";

            var m = d.Minor;
            var small = m == null ? "none" : !m.Spawned ? "warned" : m.Retreat ? "withdrawing" : "on the map";
            var last = d.History.Count > 0 ? d.History[d.History.Count - 1] : null;
            var step = DirectorPhase.GrowthStep(st);
            return new DirectorReadout(phase, left, Math.Max(0, d.NextStart - st.T), d.Reserved ? d.ReserveReason : "",
                has, bx, by, st.Home.Placed ? st.Home.Hp : 0, st.Home.Placed ? HomeQueries.CoreMaxHp(ctx.Data) : 0,
                step, st.Admin.RaidNumber >= 0, SiegePlan.TotalFor(ctx, step),
                a == null ? 0 : a.Wave + 1, a == null ? 0 : a.Waves, a == null ? 0 : a.Remaining, a == null ? 0 : a.Cancelled,
                small, major, minor, total, r.ActiveRaidBudget, r.LivingBudget,
                last == null ? 0 : last.Id, last == null ? -1 : last.Outcome,
                a == null ? DirectorPacing.WarningHold(ctx, st) : "", d.CycleMinors, ctx.Data.Siege.MinorsPerCycle);
        }

        /// <summary>The readout as the Admin page prints it. Developer text; nothing a player sees.</summary>
        public static string Describe(DirectorReadout v)
        {
            var phase = v.Phase == "assault" ? $"assault · wave {Math.Min(v.Wave, v.Waves)} of {v.Waves} · {v.Remaining} still to arrive · planned end in {v.PhaseSeconds:0} s"
                : v.Phase == "warned" ? $"warned · starts in {v.PhaseSeconds:0} s · {v.Waves} waves, {v.Remaining} bodies"
                : v.Phase == "recovery" ? $"recovery · {v.PhaseSeconds:0} s left"
                : v.Phase == "quiet" ? $"quiet spell · {v.PhaseSeconds:0} s left"
                : v.Phase == "held" ? "held · " + (v.Hold.Length > 0 ? v.Hold : "the approach is reserved")
                : v.Phase;
            var target = v.HasTarget ? $"Home core at {v.TargetX}, {v.TargetY}" + (v.CoreMaxHp > 0 ? " · " + HomeQueries.CoreHpText(v.CoreHp, v.CoreMaxHp) : " · authored site, no core placed")
                : "none · the core is down";
            var last = v.LastOutcome < 0 ? "none yet"
                : $"raid {v.LastId} · " + (v.LastOutcome == (int)RaidOutcome.Cleared ? "cleared" : v.LastOutcome == (int)RaidOutcome.Lost ? "lost (the core fell)" : "broke off");
            return "Phase: " + phase
                + $"\nNext large raid on the clock: {v.NextStartIn:0} s"
                + "\nTarget: " + target
                + $"\nRaids survived: {v.Survived}" + (v.Pinned ? " (pinned by Admin)" : "") + $" · the next one booked brings {v.NextBodies}"
                + "\nSmall raid: " + v.Minor
                + $"\nPacing: small raids this cycle {v.CycleMinors} of {v.MinorsPerCycle}"
                + (v.Pacing.Length > 0 ? " · the next warning waits: " + v.Pacing : "")
                + $"\nLarge-raid bodies alive: {v.MajorAlive} of {v.ActiveBudget} · small-raid: {v.MinorAlive} · all enemies: {v.Living} of {v.LivingBudget}"
                + "\nLast large raid: " + last;
        }

        /// <summary>Reference <c>knownCampaignThreat</c> + <c>campaignWarning</c>, as data rather than a sentence.</summary>
        public static RaidWarning Warning(SimContext ctx, SimState st)
        {
            var d = st.Director;
            var w = ctx.Geometry.Width;
            DirectorRules.Target(ctx, st, out var bx, out var by, out var size);
            var cx = bx + size / 2.0;
            var cy = by + size / 2.0;

            var m = d.Minor;
            // REL-74: a small raid that is only walking away gives way to a large one that is warned or on the
            // ground. The director turns the small raid back the moment an assault commits, so this is exactly the
            // assault's opening minutes, and the strip and the raid arrow used to follow the retreat instead.
            if (m != null && m.Retreat && d.Major != null && !d.Major.Retreat) m = null;
            if (m != null)
            {
                var at = new Vec2(m.Origin % w + .5, m.Origin / w + .5);
                // GP-W3: a warned raid is a countdown, not a wave. It reports the time its bodies are actually due
                // and the heading it was announced on, so the strip cannot claim an attack that has not arrived.
                var label = m.Scripted ? "Small enemy group" : "Raid";
                if (!m.Spawned)
                    return new RaidWarning("warning", Math.Max(0, m.StartsAt - st.T), Direction(at, cx, cy), d.Notice, at, label, m.Id);
                return new RaidWarning(m.Retreat ? "withdrawal" : "minor raid", 0,
                    Direction(at, cx, cy), d.Notice, at, label, m.Id);
            }
            var a = d.Major;
            if (a != null)
            {
                var at = new Vec2(a.Origin % w + .5, a.Origin / w + .5);
                var kind = a.Retreat ? "withdrawal" : st.T >= a.StartsAt ? "assault" : "warning";
                return new RaidWarning(kind, Math.Max(0, a.StartsAt - st.T), Direction(at, cx, cy), d.Notice, at, "Major assault", a.Id);
            }
            if (st.T < d.RecoveryUntil)
                return new RaidWarning("recovery", 0, "·", d.Notice, new Vec2(cx, cy));
            return new RaidWarning("", Math.Max(0, d.NextStart - st.T), "·", d.Notice, new Vec2(0, 0));
        }

        /// <summary>
        /// E-17 (U-D-61): is a raid warned or under way? True from the moment a raid is announced until it turns to
        /// withdraw — the window in which a turret that is merely low on ammunition is worth a notice.
        /// </summary>
        public static bool RaidExpected(SimState st)
        {
            var d = st?.Director;
            if (d == null) return false;
            return (d.Minor != null && !d.Minor.Retreat) || (d.Major != null && !d.Major.Retreat);
        }

        /// <summary>Compass word for the heading from the core to a point (reference <c>openingDirection</c>).</summary>
        public static string Direction(Vec2 at, double cx, double cy) =>
            DirectorRules.HeadingWord(DirectorRules.Heading(at.X - cx, at.Y - cy));

        /// <summary>
        /// REL-74: the sides a set of approach tiles lie on, seen from the raid's target, in the words a banner
        /// uses: "east", "east and south-west", "north, east and south". The same sides and order as
        /// <see cref="DirectorRules.HeadingsOf"/>, spelled out. "" when there is no target or nothing to name.
        /// </summary>
        public static string SideWords(SimContext ctx, SimState st, int[] origins)
        {
            if (origins == null || origins.Length == 0) return "";
            if (!DirectorRules.Target(ctx, st, out var bx, out var by, out var size)) return "";
            var w = ctx.Geometry.Width;
            var words = new System.Collections.Generic.List<string>();
            for (var i = 0; i < origins.Length; i++)
            {
                if (origins[i] < 0) continue;
                var word = SideWord(origins[i] % w + .5 - (bx + size / 2.0), origins[i] / w + .5 - (by + size / 2.0));
                if (word.Length > 0 && !words.Contains(word)) words.Add(word);
            }
            if (words.Count == 0) return "";
            if (words.Count == 1) return words[0];
            return string.Join(", ", words.GetRange(0, words.Count - 1).ToArray()) + " and " + words[words.Count - 1];
        }

        /// <summary>
        /// REL-74: where a raid on the ground is, for the raid arrow: its living body nearest the target, which is
        /// the front of the fight. A body walking home is not counted. False when none of the raid's bodies is on
        /// the map (before a wave is born, or once they are all dead or leaving).
        /// </summary>
        public static bool Front(SimContext ctx, SimState st, int raidId, out Vec2 at)
        {
            at = default;
            if (raidId <= 0 || st?.Enemies == null) return false;
            var hasTarget = DirectorRules.Target(ctx, st, out var bx, out var by, out var size);
            var cx = bx + size / 2.0;
            var cy = by + size / 2.0;
            var best = double.PositiveInfinity;
            var list = st.Enemies.Actors;
            for (var i = 0; i < list.Count; i++)
            {
                var e = list[i];
                if (e.Group != raidId || e.Layer == EnemyLayer.Site || e.Withdrawing || e.Hp <= 0) continue;
                var dx = e.Pos.X - cx;
                var dy = e.Pos.Y - cy;
                var d2 = hasTarget ? dx * dx + dy * dy : 0;
                if (d2 >= best) continue;
                best = d2;
                at = e.Pos;
            }
            return !double.IsPositiveInfinity(best);
        }

        /// <summary>
        /// REL-74: the side a point lies on, seen from the raid's target, as a word ("south-east"); "" with no target,
        /// or for a point at the target's own centre.
        /// </summary>
        public static string SideOf(SimContext ctx, SimState st, double x, double y) =>
            DirectorRules.Target(ctx, st, out var bx, out var by, out var size)
                ? SideWord(x - (bx + size / 2.0), y - (by + size / 2.0))
                : "";

        private static string SideWord(double dx, double dy)
        {
            var h = DirectorRules.Heading(dx, dy);
            return h < 0 ? "" : OpeningRules.Compass(DirectorRules.HeadingWord(h));
        }

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
