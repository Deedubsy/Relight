namespace Relight.Sim
{
    /// <summary>
    /// Core-owned state (B-03). Nothing lives here yet: the per-second danger telemetry that
    /// <see cref="SecondPhase"/> records sits on <see cref="Engineer"/>, where the reference keeps it
    /// (sim.ts:941 <c>player.danger++</c>), and the hourly <c>hourRow</c> sample is retired with the block economy
    /// (CONTENT_CATALOGUE.md §17). The partial hook stays so a later core field has a home.
    /// </summary>
    public sealed partial class SimState
    {
        partial void VisitCore(IStateVisitor v)
        {
        }
    }
}
