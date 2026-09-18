using System;
using System.Collections.Generic;

namespace Relight.Sim
{
    /// <summary>
    /// C-09's state machine: the port of reference campaignThreat.ts:204 <c>tickOpeningEncounter</c>, plus
    /// <c>noteTurretSupply</c>'s job (openingEncounter.ts:78) and the shot counter the reference keeps in its
    /// turret-fire path.
    ///
    /// Runs in the Campaign slot AFTER <see cref="DirectorPhase"/>, so it reads this tick's wave state: a group
    /// whose last body died this tick is already gone from <see cref="DirectorState.Minor"/>.
    ///
    /// THE TWO DELIBERATE DIFFERENCES FROM THE REFERENCE, both recorded in the wave-3 W-A report:
    /// 1. U-D-26 / U-Q-21 — where the reference sets a PERMANENT <c>skipped</c> because a raid happens to be live
    ///    or close (campaignThreat.ts:209 and :219), the port sets <see cref="OpeningStatus.Deferred"/> and resumes
    ///    once <see cref="Director.Blocked"/> clears. Ordinary raids keep their own cadence throughout (U-D-38: no
    ///    accumulated debt), and the deferral is BOUNDED — see <see cref="Capped"/>.
    /// 2. The end state consults BOTH the prepared turret and the Home core (U-D-54). The reference writes
    ///    <c>core &amp;&amp; core.hp === 0 ? 'lost' : 'repelled'</c>; the wave-3 brief restated that as "repelled if
    ///    the turret lives else lost", and the port implemented the brief — so a run whose attackers walked past an
    ///    out-of-range turret and flattened the core still announced "Attack repelled" over a core at 0 HP. That is
    ///    the one outcome a player cannot read as a win, so U-D-54 supersedes the wave-3 rule: repelled now needs
    ///    the turret alive AND the core operational. Row 0b still outranks the whole chain while the core is down,
    ///    and §7.8's <c>Rebuild your turret</c> still keys off the turret, so "lost" keeps its turret-shaped advice.
    /// </summary>
    public sealed class OpeningPhase : ITickPhase
    {
        /// <summary>Held by the phase, never saved: cleared and refilled inside a single call.</summary>
        private readonly List<Machine> _scratch = new List<Machine>();

        /// <summary>Reference campaignThreat.ts:222's retry window: the staging attempt gives up 60 s after <c>startsAt</c>.</summary>
        private const double StageRetryS = 60;

        /// <summary>PORT ADDITION (U-D-26). Why the encounter is waiting; the card adds "about N s" from the clock.</summary>
        public const string DeferReason = "A raid is under way; the introductory attack waits until the area is safe.";

        /// <summary>PORT ADDITION (U-Q-21). The bound: the encounter is dropped, without a reward, and the guide moves on.</summary>
        public const string CapNotice = "The introductory attack was dropped: your defences are already established.";

        /// <summary>PORT ADDITION. Why ordinary waves are held while the introductory group is announced or up.</summary>
        public const string ReserveReason = "The introductory encounter is under way.";

        public void Tick(SimContext ctx, SimState st, double dt)
        {
            var op = st.Opening;
            // A schema-v3 upgrade of an older save arrives with a fresh OpeningState; decide it from that save's
            // own facts on the first tick, exactly as DirectorPhase seeds its clock (reference initOpeningEncounter).
            if (op.Status == OpeningStatus.None) OpeningRules.Init(ctx, st);

            NoteProduction(ctx, st);
            NoteSupply(ctx, st);

            switch (op.Status)
            {
                case OpeningStatus.Pending:
                case OpeningStatus.Deferred:
                    Waiting(ctx, st);
                    break;
                case OpeningStatus.Scheduled:
                    Scheduled(ctx, st);
                    break;
                case OpeningStatus.Active:
                    Active(ctx, st);
                    break;
            }

            // U-Q-21's cap is checked AFTER the machine, so a tick that can schedule always schedules first.
            if (op.Status == OpeningStatus.Pending || op.Status == OpeningStatus.Deferred) Capped(ctx, st);
        }

        // ------------------------------------------------------------------ pending / deferred

        private void Waiting(SimContext ctx, SimState st)
        {
            var op = st.Opening;
            var d = st.Director;
            var t = OpeningRules.Tuning(ctx.Data);

            var turret = OpeningRules.ReadyTurret(ctx, st, _scratch);
            if (turret == null || !st.Home.Placed)
            {
                // Nothing to announce yet. A deferral already taken keeps its countdown honest meanwhile.
                if (op.Status == OpeningStatus.Deferred) op.DeferredUntil = OpeningRules.SafeAt(ctx, st);
                return;
            }

            if (Director.Blocked(ctx, st))
            {
                var until = OpeningRules.SafeAt(ctx, st);
                if (op.Status != OpeningStatus.Deferred)
                {
                    op.Status = OpeningStatus.Deferred;
                    op.DeferCount++;
                    op.DeferNotice = DeferReason;
                    st.Events.Add(new OpeningDeferredEvent(st.T, until, DeferReason));
                }
                op.DeferredUntil = until;
                return;
            }

            // Reference campaignThreat.ts:211: the next major must still be a guard away, so its warning window can
            // never overlap the encounter and its recovery. This is a one-off push, not accumulated debt (U-D-38).
            if (Director.NextMajorAt(st) - st.T < t.GuardS)
            {
                var shift = st.T + t.GuardS - d.NextStart;
                Director.Defer(ctx, st, st.T + t.GuardS,
                    "First major assault deferred " + (int)Math.Ceiling(shift / 60)
                    + " active min so the introductory attack and its recovery finish first.");
            }

            var origin = DirectorQueries.OpeningOrigin(ctx, st, turret.Id);
            if (origin < 0)
            {
                op.Status = OpeningStatus.Skipped;
                Director.Say(ctx, st, "No reachable exterior approach for the introductory attack; it is skipped.");
                return;
            }

            op.Status = OpeningStatus.Scheduled;
            op.TurretId = turret.Id;
            op.Origin = origin;
            op.ScheduledAt = st.T;
            op.StartsAt = st.T + t.WarningS;
            op.Count = t.Count;
            op.Shots = 0;
            op.DeferredUntil = -1;
            // Reference openingEncounter.ts:50 openingBlocksRaids: nothing else may start while the encounter is
            // announced or up. The port says the same thing through the director's own hold.
            Director.Reserve(st, ReserveReason);
            st.Events.Add(new OpeningScheduledEvent(st.T, OpeningQueries.DirectionOf(ctx, st), op.StartsAt));
        }

        // ------------------------------------------------------------------ scheduled

        private void Scheduled(SimContext ctx, SimState st)
        {
            var op = st.Opening;
            if (st.T < op.StartsAt) return;

            // Defensive only: the reserve means no ordinary wave can have begun. If one somehow has, U-D-26 says
            // hold, never cancel.
            if (st.Director.Major != null || st.Director.Minor != null)
            {
                Director.Release(st);
                op.Status = OpeningStatus.Deferred;
                op.DeferCount++;
                op.DeferNotice = DeferReason;
                op.DeferredUntil = OpeningRules.SafeAt(ctx, st);
                op.StartsAt = -1;
                op.ScheduledAt = -1;
                st.Events.Add(new OpeningDeferredEvent(st.T, op.DeferredUntil, DeferReason));
                return;
            }

            var group = Director.ScheduleGroup(ctx, st, op.Origin, op.Count, true, op.TurretId);
            if (group == 0)
            {
                if (st.T > op.StartsAt + StageRetryS)
                {
                    Director.Release(st);
                    op.Status = OpeningStatus.Skipped;
                    Director.Say(ctx, st, "The introductory attack found no open ground and was skipped.");
                }
                return;
            }

            op.Group = group;
            op.Count = Director.GroupAlive(st, group);   // reference `op.count = born`
            op.Status = OpeningStatus.Active;
            st.Events.Add(new OpeningStartedEvent(st.T, OpeningQueries.DirectionOf(ctx, st), op.Count));
        }

        // ------------------------------------------------------------------ active

        private void Active(SimContext ctx, SimState st)
        {
            var op = st.Opening;
            var t = OpeningRules.Tuning(ctx.Data);

            CountShots(st);

            if (Director.GroupAlive(st, op.Group) > 0)
            {
                if (st.T > op.StartsAt + t.MaxDurationS) Director.Withdraw(st, op.Group);
                // A withdrawing body that cannot reach its exit would otherwise hold the raid slot forever.
                if (st.T > op.StartsAt + 2 * t.MaxDurationS) Director.Purge(st, op.Group);
                if (Director.GroupAlive(st, op.Group) > 0) return;
            }

            var turret = st.MachineById(op.TurretId);
            // U-D-54: a live turret alone is not a repulse. Attackers that ignored an out-of-range turret and
            // disabled the core were not repelled, and saying so over a core at 0 HP reads as a bug to the player.
            var turretHeld = turret != null && TurretQueries.Hp(ctx, st, turret.Id) > 0;
            var repelled = turretHeld && HomeQueries.CoreOperational(st);
            op.Status = repelled ? OpeningStatus.Repelled : OpeningStatus.Lost;
            op.EndedAt = st.T;

            Director.Recover(st, t.RecoveryS);
            Director.Release(st);
            if (st.Director.Major == null && st.Director.RecoveryUntil > st.Director.NextStart)
                Director.Defer(ctx, st, st.Director.RecoveryUntil + ctx.Data.Raids.WarningS,
                    "First major assault deferred so the introductory attack and its recovery finish first.");

            st.Events.Add(new OpeningEndedEvent(st.T, repelled, op.Shots));
        }

        /// <summary>
        /// Reference: the encounter's <c>shots</c> is incremented where the turret actually fires, never inferred
        /// from stock. The port's equivalent is W-B's <see cref="TurretShotEvent"/>, raised by <c>TurretPhase</c>
        /// earlier in the same tick.
        /// </summary>
        private static void CountShots(SimState st)
        {
            var op = st.Opening;
            for (var i = 0; i < st.Events.Count; i++)
                if (st.Events[i] is TurretShotEvent s && s.MachineId == op.TurretId) op.Shots++;
        }

        // ------------------------------------------------------------------ U-Q-21

        /// <summary>
        /// U-Q-21's resolution: the deferral is bounded. If the encounter has still not run once the player has
        /// three loaded turrets, or has set off for the first camp, it is dropped WITHOUT a reward and the guide
        /// moves on. Option (c) — holding ordinary raids until it runs — is rejected by U-D-38.
        /// </summary>
        private void Capped(SimContext ctx, SimState st)
        {
            var op = st.Opening;
            var t = OpeningRules.Tuning(ctx.Data);
            if (TurretQueries.Loaded(ctx, st) < t.TurretObjective && !OpeningRules.LeftForFirstCamp(ctx, st)) return;
            op.Status = OpeningStatus.Skipped;
            op.DeferredUntil = -1;
            Director.Say(ctx, st, CapNotice);
        }

        // ------------------------------------------------------------------ supply

        /// <summary>
        /// The sticky half of the reference's <c>observation.produced.magazine</c>: once a bullet processor has
        /// had finished output, that fact outlives the buffer a belt drains.
        /// </summary>
        private void NoteProduction(SimContext ctx, SimState st)
        {
            var op = st.Opening;
            if (op.ProducedAt >= 0) return;
            for (var i = 0; i < st.Machines.Count; i++)
            {
                var m = st.Machines[i];
                if (!OpeningRules.IsBulletProducer(ctx, st, m)) continue;
                // The live buffer or a batch finished this tick — never the retired Machine.Out (see OutputBuffer).
                if (OpeningRules.OutputBuffer(ctx, st, m) <= 0 && !OpeningRules.ProducedThisTick(st, m)) continue;
                op.ProducedAt = st.T;
                return;
            }
        }

        /// <summary>
        /// Reference openingEncounter.ts:78 <c>noteTurretSupply</c>, which the reference calls from inside the
        /// automated feed path so that "hand loading never records a supply chain". That path is W-A's
        /// (<c>FlowRules.GiveItem</c> / <c>MachinePhase.GiveItem</c>) and outside C-09's ownership, so the port
        /// watches the counter only that path moves: <see cref="Stats.TurretFed"/>. A hand load counts
        /// <see cref="Stats.HandFedMags"/> instead and is invisible here, exactly as the reference intends.
        ///
        /// PORT ADDITION (U-D-53), the escape hatch. Watching the counter alone deadlocks the chain: a player who
        /// keeps the turret topped up by hand leaves NO room in the hopper, the belt or inserter cannot insert,
        /// <see cref="TurretHopper.Give"/> returns 0 without touching the counter, and "Automatic resupply working"
        /// never arrives however correct the line is. So a working line ALSO counts when the only reason nothing
        /// moved is that the turret is full — <see cref="TurretHopper.Room"/> is 0 — which a hand load cannot fake,
        /// because the supplier still has to be a real bullet producer connected to that turret
        /// (<see cref="OpeningRules.TurretSupplier"/>, itself gated on production having happened).
        /// </summary>
        private void NoteSupply(SimContext ctx, SimState st)
        {
            var op = st.Opening;
            var fed = st.Stats.TurretFed;
            if (op.SuppliedAt >= 0) { op.FedSeen = fed; return; }
            var moved = fed - op.FedSeen;
            op.FedSeen = fed;
            // NoteProduction ran first this tick, so ProducedAt < 0 means no producer has ever had output and
            // TurretSupplier cannot return one: nothing below can latch, and the full-hopper walk is skipped.
            if (moved <= 0 && op.ProducedAt < 0) return;

            OpeningRules.LiveTurrets(ctx, st, _scratch);
            for (var i = 0; i < _scratch.Count; i++)
            {
                var m = _scratch[i];
                // Cheap test first: with nothing moving, only a turret the line physically cannot insert into
                // qualifies, and only then is the chain walk worth doing.
                if (moved <= 0 && TurretHopper.Room(ctx, st, m.Id) > 0) continue;
                if (OpeningRules.TurretSupplier(ctx, st, m) == null) continue;
                op.SuppliedAt = st.T;
                st.Events.Add(new ResupplyWorkingEvent(st.T, m.Id));
                return;
            }
        }
    }

    /// <summary>
    /// Applies the reference's <c>initOpeningEncounter</c> at new-game time. A loaded save arrives with its own
    /// decision already made and is left alone; a schema-v3 upgrade of an older save arrives as
    /// <see cref="OpeningStatus.None"/> and is decided by <see cref="OpeningPhase"/> on its first tick.
    /// </summary>
    public sealed class OpeningInitializer : IStateInitializer
    {
        public void Init(SimContext ctx, SimState st) => OpeningRules.Init(ctx, st);
    }
}
