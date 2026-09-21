namespace Relight.Sim
{
    /// <summary>What a repair in progress is working on (reference campaignDefence.ts <c>DefenceState.repair.kind</c>).</summary>
    public static class RepairKinds
    {
        public const int None = 0;
        public const int Core = 1;
        public const int Machine = 2;
    }

    /// <summary>
    /// C-05's saved state, visited as <c>home</c>: the Home core and the one repair that may be running.
    /// Ported from reference campaignDefence.ts <c>BaseCore</c> and <c>DefenceState.repair</c>, narrowed to the
    /// single Home core — the reference's <c>bases[]</c> exists because a campaign restores outlying stations, and
    /// C-05's scope is the Home core alone. The core is not a <see cref="Machine"/>: the reference's is a record on
    /// the defence state, placed from the map rather than built, and nothing may pack it.
    ///
    /// <c>new HomeState()</c> means "nothing has happened yet": no core placed, no repair. A Phase B save upgraded
    /// to v3 gets exactly that, and <see cref="HomeCore.Ensure"/> places the core on the first tick.
    /// </summary>
    public sealed class HomeState : IVisitable
    {
        /// <summary>False until <see cref="HomeCore.Ensure"/> has put the core down.</summary>
        public bool Placed;

        /// <summary>True when the core is the synthetic 3×3 at the engineer's spawn rather than <c>ctx.Sites.Core</c>.</summary>
        public bool Fallback;

        public int X;
        public int Y;
        public int W = 1;
        public int H = 1;

        /// <summary>Reference <c>BaseCore.hp</c>: 0 means disabled, and only a recommission kit brings it back.</summary>
        public double Hp;

        /// <summary>Sim time the core was commissioned (reference <c>BaseCore.commissionedAt</c>).</summary>
        public double CommissionedAt;

        /// <summary>Sim time the core last fell to 0, or -1 while it has never been knocked out (reference <c>coreDisabledAt</c>).</summary>
        public double DisabledAt = -1;

        /// <summary>0 none, 1 core, 2 machine (reference <c>repair.kind</c>). 0 is "no repair running".</summary>
        public int RepairKind;

        /// <summary>The machine being repaired; -1 for the core, which has no machine id (reference <c>repair.id</c>).</summary>
        public int RepairId = -1;

        /// <summary>Seconds of work left (reference <c>repair.remaining</c>).</summary>
        public double RepairRemaining;

        /// <summary>True when this is a recommission of a disabled core rather than a patch (reference <c>repair.recommission</c>).</summary>
        public bool RepairRecommission;

        /// <summary>
        /// What was actually taken from the pockets when the repair started, so a cancel refunds exactly that even
        /// if the tuning changed under a save. The reference has no cancel, so it stores nothing.
        /// </summary>
        public int PaidSteel;
        public int PaidCopper;

        public void Visit(IStateVisitor v)
        {
            v.Field("placed", ref Placed);
            v.Field("fallback", ref Fallback);
            v.Field("x", ref X);
            v.Field("y", ref Y);
            v.Field("w", ref W);
            v.Field("h", ref H);
            v.Field("hp", ref Hp);
            v.Field("commissionedAt", ref CommissionedAt);
            v.Field("disabledAt", ref DisabledAt);
            v.Field("repairKind", ref RepairKind);
            v.Field("repairId", ref RepairId);
            v.Field("repairRemaining", ref RepairRemaining);
            v.Field("repairRecommission", ref RepairRecommission);
            v.Field("paidSteel", ref PaidSteel);
            v.Field("paidCopper", ref PaidCopper);
        }
    }

    public sealed partial class SimState
    {
        /// <summary>The Home core and its repair (C-05), visited as <c>home</c>.</summary>
        public HomeState Home = new HomeState();

        partial void VisitHomeCore(IStateVisitor v) => v.Object("home", ref Home, () => new HomeState());
    }

    /// <summary>
    /// The core lost HP (reference <c>damageCore</c>); <c>Hp</c> is what is left and <c>Lost</c> is what this hit
    /// took. <c>Lost</c> is carried (INT-04c) so that a listener adding up a fight's damage does not have to know
    /// what the core stood at before the hit: a repair in between would make that sum come out short.
    /// </summary>
    public sealed record CoreDamagedEvent(double T, double Hp, double Lost) : SimEvent(T);

    /// <summary>The core reached 0 HP and stopped counting as commissioned (reference <c>damageCore</c>'s hp === 0 branch).</summary>
    public sealed record CoreDisabledEvent(double T) : SimEvent(T);

    /// <summary>A repair or recommission finished (reference <c>tickRepair</c>).</summary>
    public sealed record CoreRepairedEvent(double T) : SimEvent(T);
}
