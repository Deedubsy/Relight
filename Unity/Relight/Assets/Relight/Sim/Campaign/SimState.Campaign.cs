namespace Relight.Sim
{
    /// <summary>
    /// Campaign's part of the state (Phase C, Wave 0 contract). One top-level object per sub-hook, each implemented
    /// once in its owner's file: <c>home</c> (C-05: core HP, disabled/recovery), <c>opening</c> (C-09: the
    /// introductory-encounter state machine and guide progress), <c>light</c> (C-11: day/night and light sources
    /// that are state rather than derived from machines).
    /// </summary>
    public sealed partial class SimState
    {
        partial void VisitCampaign(IStateVisitor v)
        {
            VisitHomeCore(v);
            VisitOpening(v);
            VisitLight(v);
        }

        partial void VisitHomeCore(IStateVisitor v);
        partial void VisitOpening(IStateVisitor v);
        partial void VisitLight(IStateVisitor v);
    }
}
