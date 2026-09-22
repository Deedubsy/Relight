namespace Relight.Sim
{
    /// <summary>
    /// REL-84 (E-19): the balance data under a running game was swapped by <see cref="Simulation.ReplaceData"/>.
    /// Carries the data hash before and after — the same text a save records as <c>dataVersion</c> — so the log
    /// line and the HUD can say what moved without the sim knowing why. Never saved; not part of the state hash.
    /// </summary>
    /// <param name="FromHash">The data hash the game was running on.</param>
    /// <param name="ToHash">The data hash it runs on from the next tick.</param>
    public sealed record DataReloadedEvent(double T, string FromHash, string ToHash) : SimEvent(T);
}
