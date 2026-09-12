namespace Relight.Sim
{
    /// <summary>
    /// What happened this tick (reference types.ts `SimEvent`). Appended to <see cref="SimState.Events"/>, drained
    /// once per frame by the host, never saved and never a persistence channel (TECHNICAL_ARCHITECTURE.md §4.3).
    /// </summary>
    public abstract record SimEvent(double T);

    public sealed record EngineerDownEvent(double T, double X, double Y) : SimEvent(T);
    public sealed record EngineerUpEvent(double T) : SimEvent(T);
}
