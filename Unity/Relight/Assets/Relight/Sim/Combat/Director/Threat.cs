using System;

namespace Relight.Sim
{
    /// <summary>
    /// The raid director's public surface — what the reference reached through its <c>hooks</c> global and what
    /// C-09's opening encounter drives. Deliberately small: C-09 must be able to hold, defer, stage and count a
    /// group without knowing anything about the clock.
    ///
    /// NAMING — READ BEFORE INTEGRATING. Row C-08 and the wave-3 brief call this interface <c>IThreatLayer</c>,
    /// but <c>Sim/Core/Contracts/Layers.cs</c> (Wave 1, not W-B's file) already defines a DIFFERENT
    /// <c>IThreatLayer { void Tick(...); void Second(...) }</c> that <see cref="SimContext"/> takes in its
    /// constructor. Two unrelated interfaces cannot share a name in one flat namespace, so this one is
    /// <c>IRaidDirector</c> and the core's stays as it is; <see cref="EnemyThreatLayer"/> fills the core seam. The
    /// W-B report carries the exact rename patch should the core owner prefer to fold them together.
    /// </summary>
    public interface IRaidDirector
    {
        /// <summary>Hold every new wave, for the stated reason. Idempotent; the last reason wins.</summary>
        void Reserve(SimState st, string reason);

        /// <summary>Lift the hold. Safe to call when nothing is held.</summary>
        void Release(SimState st);

        /// <summary>
        /// Push the next major assault out to <paramref name="untilT"/> (an absolute sim second) and say why.
        /// Never moves the clock backwards and never drops the wave — this is the defer-not-skip path (U-D-26).
        /// </summary>
        void Defer(SimContext ctx, SimState st, double untilT, string notice);

        /// <summary>True while a wave is live, the director is reserved, or a major is inside its warning window.</summary>
        bool Blocked(SimContext ctx, SimState st);

        /// <summary>Absolute sim second the next major assault begins.</summary>
        double NextMajorAt(SimState st);

        /// <summary>
        /// Stage one group NOW from <paramref name="origin"/> (a tile index) and return its group id, or 0 if no
        /// body could be placed. It occupies the ordinary minor-raid slot, so cleanup, retreat and blocking all
        /// follow the ordinary rules, but it never advances <see cref="DirectorState.RaidsStarted"/>.
        /// </summary>
        int ScheduleGroup(SimContext ctx, SimState st, int origin, int count, bool basic, int targetId);

        /// <summary>Bodies still alive in a group.</summary>
        int GroupAlive(SimState st, int group);
    }

    /// <summary>
    /// The one director. Static because there is exactly one and it owns no state of its own — everything lives in
    /// <see cref="SimState.Director"/>, so a save round-trips the whole thing. <see cref="Instance"/> is the same
    /// behaviour behind <see cref="IRaidDirector"/> for callers that want to inject a fake in a test.
    /// </summary>
    public static class Director
    {
        /// <summary>The interface view of this class, for injection.</summary>
        public static readonly IRaidDirector Instance = new DirectorFacade();

        /// <summary>
        /// Put one sentence on the HUD and say how long it is worth reading (GP-W3). Every write to
        /// <see cref="DirectorState.Notice"/> goes through here, because a notice with no expiry is the stale
        /// notice the brief names: "assault announced" was still on screen long after the assault was over.
        /// <paramref name="untilT"/> is an ABSOLUTE sim second, so the hold survives a save like every other time.
        /// </summary>
        public static void Say(SimState st, string text, double untilT)
        {
            var d = st.Director;
            d.Notice = text ?? "";
            d.NoticeUntil = string.IsNullOrEmpty(d.Notice) ? 0 : Math.Max(untilT, st.T);
        }

        /// <summary>Say it, and hold it for the data-driven default (<see cref="SiegeTuning.NoticeHoldS"/>).</summary>
        public static void Say(SimContext ctx, SimState st, string text) =>
            Say(st, text, st.T + ctx.Data.Siege.NoticeHoldS);

        /// <summary>Clear the HUD notice once it has stopped being news.</summary>
        public static void Expire(SimState st)
        {
            var d = st.Director;
            if (d.NoticeUntil <= 0 || st.T < d.NoticeUntil) return;
            d.Notice = "";
            d.NoticeUntil = 0;
        }

        public static void Reserve(SimState st, string reason)
        {
            var d = st.Director;
            d.Reserved = true;
            d.ReserveReason = reason ?? "";
            st.Events.Add(new RaidNoticeEvent(st.T, RaidNoticeKind.Reserved, d.ReserveReason, -1));
        }

        public static void Release(SimState st)
        {
            var d = st.Director;
            if (!d.Reserved) return;
            d.Reserved = false;
            d.ReserveReason = "";
            st.Events.Add(new RaidNoticeEvent(st.T, RaidNoticeKind.Released, "", -1));
        }

        /// <summary>
        /// Move the next major out, never in, and announce it. The reference has no deferral at all: it marks the
        /// introductory encounter permanently <c>skipped</c> when a raid is close (campaignThreat.ts:209). U-D-26
        /// replaces that with this call, and U-Q-21 bounds how long C-09 may keep using it.
        /// </summary>
        public static void Defer(SimContext ctx, SimState st, double untilT, string notice)
        {
            var d = st.Director;
            if (untilT <= d.NextStart) return;
            var shift = untilT - d.NextStart;
            d.NextStart = untilT;
            // A major already announced but not yet committed moves with the clock; a committed one is untouched.
            // GP-W4: an assault is a saved WAVE PLAN, so a deferral slides the whole plan along the clock rather
            // than restating a length. The waves, their composition and their approaches are untouched: the player
            // is told the encounter has moved, not given a different one.
            if (d.Major != null && !d.Major.Committed) SiegePlan.Shift(ctx, d.Major, untilT);
            // The announcement this deferral invalidates must be made again with the corrected time (GP-W3).
            if (d.Major != null && !d.Major.Committed && d.WarnedId == d.Major.Id) d.WarnedId = 0;
            Say(st, string.IsNullOrEmpty(notice)
                    ? "First major assault deferred " + (int)Math.Ceiling(shift / 60) + " active min."
                    : notice,
                st.T + ctx.Data.Siege.NoticeHoldS);
            st.Events.Add(new RaidNoticeEvent(st.T, RaidNoticeKind.Deferred, d.Notice, d.Major?.Id ?? -1));
        }

        /// <summary>
        /// DEVELOPER TOOL (E-19; the Admin panel is its only caller). Pull the next large raid IN to
        /// <paramref name="startsAt"/> — the opposite of <see cref="Defer"/>, which gameplay may call and which only
        /// ever moves it out. It moves the CLOCK and nothing else: the raid is still booked by <c>Schedule</c>,
        /// announced by <c>Warn</c>, committed by <c>Commit</c> and spawned wave by wave, so what a developer
        /// watches is the path a player gets. A raid already warned keeps its plan and slides along the clock; a
        /// recovery spell that would refuse the new start is cut short, because a recovery spell IS the clock.
        /// It does not override the rules the director tests for: a reserved approach, a fallen core and a raid
        /// already under way are refused, with the reason. Returns "" when the clock was moved.
        /// </summary>
        public static string PullIn(SimContext ctx, SimState st, double startsAt)
        {
            var d = st.Director;
            if (d.Major != null && d.Major.Committed) return "A large raid is already under way.";
            if (d.Reserved) return d.ReserveReason.Length > 0 ? d.ReserveReason : "The approach is reserved.";
            if (!DirectorRules.Target(ctx, st, out _, out _, out _)) return "There is no standing core to assault. Repair Home first.";
            if (startsAt < st.T) startsAt = st.T;
            if (d.RecoveryUntil > startsAt) d.RecoveryUntil = startsAt;
            d.NextStart = startsAt;
            if (d.Major != null)
            {
                SiegePlan.Shift(ctx, d.Major, startsAt);
                d.WarnedId = 0;                         // announce it again, with the corrected time (as Defer does)
            }
            return "";
        }

        /// <summary>Push the recovery window and the next minor opportunity out by <paramref name="seconds"/>.</summary>
        public static void Recover(SimState st, double seconds)
        {
            var d = st.Director;
            d.RecoveryUntil = Math.Max(d.RecoveryUntil, st.T + seconds);
            d.NextMinor = Math.Max(d.NextMinor, st.T + seconds);
        }

        public static bool Blocked(SimContext ctx, SimState st)
        {
            var d = st.Director;
            if (d.Reserved) return true;
            if (d.Major != null || d.Minor != null) return true;
            if (st.T < d.RecoveryUntil) return true;
            return d.NextStart - st.T < ctx.Data.Raids.WarningS;
        }

        public static double NextMajorAt(SimState st) => st.Director.NextStart;

        /// <param name="scripted">True (the default) for the opening's encounter, which the goal card reports and the
        /// raid account skips. REL-119: the Admin panel's raid passes false, so the group it stages is an ordinary
        /// small raid to the account and the raid log. Either way it never advances <see cref="DirectorState.RaidsStarted"/>.</param>
        public static int ScheduleGroup(SimContext ctx, SimState st, int origin, int count, bool basic, int targetId,
            bool scripted = true)
        {
            var d = st.Director;
            if (origin < 0 || count <= 0) return 0;
            if (d.Minor != null) return 0;
            var staged = DirectorRules.Staging(ctx, st, origin);
            var from = staged >= 0 ? staged : origin;
            var id = d.NextId++;
            var born = 0;
            for (var n = 0; n < count; n++)
                if (DirectorRules.Birth(ctx, st, from, (int)EnemyLayer.Minor, id, basic) != 0) born++;
            if (born == 0) { d.NextId--; return 0; }
            // A staged group is staged the instant it is asked for, so its warning is already over: it is born
            // Spawned, owing nothing, and its heading is the one the bodies actually walked in on (GP-W3).
            d.Minor = new MinorRaid
            {
                Id = id,
                Origin = from,
                Retreat = false,
                Scripted = scripted,
                StartsAt = st.T,
                Owed = 0,
                Spawned = true,
                Heading = DirectorRules.HeadingsOf(ctx, st, new[] { from }),
            };
            return id;
        }

        public static int GroupAlive(SimState st, int group) => EnemyQueries.GroupAlive(st, group);

        /// <summary>Ask the current scripted or minor group to withdraw (C-09's <c>maxDuration</c> rule).</summary>
        public static void Withdraw(SimState st, int group)
        {
            var d = st.Director;
            if (d.Minor != null && d.Minor.Id == group) d.Minor.Retreat = true;
            if (d.Major != null && d.Major.Id == group) { d.Major.Retreat = true; d.Major.Remaining = 0; }
        }

        /// <summary>Remove every body in a group without a kill (C-09's purge at 2× the duration).</summary>
        public static void Purge(SimState st, int group)
        {
            var list = st.Enemies.Actors;
            for (var i = list.Count - 1; i >= 0; i--)
                if (list[i].Group == group) list.RemoveAt(i);
            if (st.Director.Minor != null && st.Director.Minor.Id == group) st.Director.Minor = null;
        }

        private sealed class DirectorFacade : IRaidDirector
        {
            public void Reserve(SimState st, string reason) => Director.Reserve(st, reason);
            public void Release(SimState st) => Director.Release(st);
            public void Defer(SimContext ctx, SimState st, double untilT, string notice) => Director.Defer(ctx, st, untilT, notice);
            public bool Blocked(SimContext ctx, SimState st) => Director.Blocked(ctx, st);
            public double NextMajorAt(SimState st) => Director.NextMajorAt(st);
            public int ScheduleGroup(SimContext ctx, SimState st, int origin, int count, bool basic, int targetId) =>
                Director.ScheduleGroup(ctx, st, origin, count, basic, targetId);
            public int GroupAlive(SimState st, int group) => Director.GroupAlive(st, group);
        }
    }
}
