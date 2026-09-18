namespace Relight.Sim
{
    /// <summary>
    /// Reference openingEncounter.ts:14 <c>OpeningStatus</c>, plus the port's <see cref="Deferred"/> (U-D-26) and a
    /// <see cref="None"/> that the reference expresses as "<c>d.opening</c> is undefined".
    ///
    /// <see cref="None"/> exists because <c>new OpeningState()</c> must mean "nothing has happened yet" (Wave 0
    /// contract): a schema-v3 upgrade of a Phase B save fills the missing <c>opening</c> member from a fresh state,
    /// and <see cref="OpeningPhase"/> then applies the reference's <c>initOpeningEncounter</c> on its first tick.
    /// </summary>
    public enum OpeningStatus
    {
        /// <summary>Not yet decided: <c>initOpeningEncounter</c> has not run for this world.</summary>
        None = 0,
        /// <summary>Waiting for the first fully loaded operational Home turret.</summary>
        Pending = 1,
        /// <summary>Announced; bodies arrive at <see cref="OpeningState.StartsAt"/>.</summary>
        Scheduled = 2,
        /// <summary>Bodies are on the map.</summary>
        Active = 3,
        /// <summary>Over with the prepared turret still standing.</summary>
        Repelled = 4,
        /// <summary>Over with the prepared turret destroyed.</summary>
        Lost = 5,
        /// <summary>Never happens for this world: a progressed save, no usable approach, or the U-Q-21 cap.</summary>
        Skipped = 6,
        /// <summary>
        /// Held because the area is not safe (a raid is live, or one is inside its warning). U-D-26: a TEMPORARY
        /// obstruction defers the encounter, it never cancels it. Resumes once <see cref="Director.Blocked"/> clears.
        /// </summary>
        Deferred = 7,
    }

    /// <summary>
    /// C-09's saved state, visited as <c>opening</c>: the once-only introductory encounter and the two sticky facts
    /// the guide reads off it. Ported from reference openingEncounter.ts:15 <c>OpeningEncounter</c>.
    ///
    /// Saved facts only, exactly as the reference's constitution rule 8 demands — there is no quest flag here, and
    /// every guide line in <see cref="OpeningQueries.Objective"/> is derived from live state each time it is asked.
    /// <c>new OpeningState()</c> is "nothing has happened yet"; the geometry-dependent decision is
    /// <see cref="OpeningRules.Init"/>'s, run by <see cref="OpeningInitializer"/> on a new game and lazily by
    /// <see cref="OpeningPhase"/> on an upgraded save's first tick.
    /// </summary>
    public sealed class OpeningState : IVisitable
    {
        public OpeningStatus Status = OpeningStatus.None;

        /// <summary>Turret rounds actually spent while the group was up, counted from <see cref="TurretShotEvent"/>.</summary>
        public int Shots;

        /// <summary>The staged group's id (<see cref="Director.ScheduleGroup"/>), 0 before it is staged.</summary>
        public int Group;

        /// <summary>The prepared turret's machine id, 0 before one is chosen. Set once and never re-chosen.</summary>
        public int TurretId;

        /// <summary>The announced approach as a tile index, -1 for none.</summary>
        public int Origin = -1;

        /// <summary>Absolute sim seconds; -1 means "has not happened".</summary>
        public double ScheduledAt = -1;
        public double StartsAt = -1;
        public double EndedAt = -1;

        /// <summary>Bodies announced, then the number actually born (reference <c>op.count = born</c>).</summary>
        public int Count;

        /// <summary>First moment a produced bullet reached a turret through belts/inserters; -1 until then.</summary>
        public double SuppliedAt = -1;

        /// <summary>
        /// PORT ADDITION. First moment a bullet-recipe processor had finished output, kept after the buffer drains.
        /// The reference reads its per-machine <c>observation.produced.magazine</c> counter, which the port has no
        /// equivalent of: <c>ProductionState.MachineWork</c> holds only timer/busy/recipe/stall and
        /// <see cref="Machine.Out"/> is the live buffer. Without this, "Bullets produced; waiting for the first
        /// batch to reach the turret" would fall back to "waiting for the Assembler to produce" the moment a belt
        /// emptied the buffer.
        /// </summary>
        public double ProducedAt = -1;

        /// <summary>
        /// PORT ADDITION (U-D-26). While <see cref="OpeningStatus.Deferred"/>, the earliest second the clock can
        /// prove the area might be safe again. Re-derived every tick, so the guide's countdown stays honest.
        /// </summary>
        public double DeferredUntil = -1;

        /// <summary>PORT ADDITION (U-Q-21). How many separate times the encounter has been deferred.</summary>
        public int DeferCount;

        /// <summary>PORT ADDITION. The player-facing reason the last deferral was taken.</summary>
        public string DeferNotice = "";

        /// <summary>
        /// PORT ADDITION. <see cref="Stats.TurretFed"/> as this phase last saw it. The reference records a supply
        /// chain from a hook inside the automated feed path (<c>noteTurretSupply</c>, called by belts and inserters
        /// only); that path is W-A's and outside C-09's ownership, so the port watches the counter only the
        /// automated path moves — hand loading counts <see cref="Stats.HandFedMags"/> instead.
        /// </summary>
        public int FedSeen;

        public void Visit(IStateVisitor v)
        {
            var status = (int)Status;
            v.Field("status", ref status);
            Status = (OpeningStatus)status;
            v.Field("shots", ref Shots);
            v.Field("group", ref Group);
            v.Field("turretId", ref TurretId);
            v.Field("origin", ref Origin);
            v.Field("scheduledAt", ref ScheduledAt);
            v.Field("startsAt", ref StartsAt);
            v.Field("endedAt", ref EndedAt);
            v.Field("count", ref Count);
            v.Field("suppliedAt", ref SuppliedAt);
            v.Field("producedAt", ref ProducedAt);
            v.Field("deferredUntil", ref DeferredUntil);
            v.Field("deferCount", ref DeferCount);
            v.Field("deferNotice", ref DeferNotice);
            v.Field("fedSeen", ref FedSeen);
        }
    }

    public sealed partial class SimState
    {
        /// <summary>The introductory encounter and the guide's sticky facts (C-09), visited as <c>opening</c>.</summary>
        public OpeningState Opening = new OpeningState();

        partial void VisitOpening(IStateVisitor v) => v.Object("opening", ref Opening, () => new OpeningState());
    }
}
