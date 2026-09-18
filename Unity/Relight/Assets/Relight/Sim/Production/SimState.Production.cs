namespace Relight.Sim
{
    /// <summary>
    /// Production's part of the state (Phase C, Wave 0 contract). Each sub-hook is implemented once, in the owning
    /// task's own partial file, and visits exactly one top-level object: <c>power</c> (B-09), <c>production</c>
    /// (B-07, per-machine work keyed by machine id), <c>flow</c> (B-10, belt/inserter contents). Per-machine state
    /// lives in those objects, never as new fields on <see cref="Machine"/>: schema v3's upgrade fills missing
    /// objects with fresh-state defaults but cannot invent members inside list elements (SaveUpgrade.cs).
    /// </summary>
    public sealed partial class SimState
    {
        partial void VisitProduction(IStateVisitor v)
        {
            VisitPower(v);
            VisitMachines(v);
            VisitFlow(v);
        }

        partial void VisitPower(IStateVisitor v);
        partial void VisitMachines(IStateVisitor v);
        partial void VisitFlow(IStateVisitor v);
    }
}
