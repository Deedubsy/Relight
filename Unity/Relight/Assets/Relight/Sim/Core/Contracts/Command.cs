namespace Relight.Sim
{
    /// <summary>
    /// The only way to mutate gameplay (TECHNICAL_ARCHITECTURE.md §4.3). One sealed record per retained
    /// reference `Command` variant (types.ts), declared beside the subsystem that handles it.
    /// Records are plain data: no engine types, no callbacks.
    /// </summary>
    public abstract record Command;

    /// <summary>Refusals are data the UI shows, never exceptions (reference construction.ts `actionResult`).</summary>
    public readonly struct CommandResult
    {
        public bool Accepted { get; }
        /// <summary>Why the command was refused, or a short confirmation text when accepted ("Stack moved.").</summary>
        public string Problem { get; }
        private CommandResult(bool accepted, string problem) { Accepted = accepted; Problem = problem ?? ""; }
        public static CommandResult Ok(string note = "") => new CommandResult(true, note);
        public static CommandResult Refuse(string problem) => new CommandResult(false, problem);
        public override string ToString() => Accepted ? $"ok {Problem}" : $"refused: {Problem}";
    }

    /// <summary>
    /// A subsystem's command handler. The dispatcher (B-03) tries the registered handlers in the fixed order of
    /// <c>SimComposition</c>; the first that recognises the command decides it. A handler must return false, without
    /// touching state, for commands it does not own.
    /// </summary>
    public interface ICommandHandler
    {
        bool TryApply(SimContext ctx, SimState st, Command c, out CommandResult result);
    }

    /// <summary>
    /// One phase of the 20 Hz tile tick (reference flow.ts stepFlow). <c>SimComposition</c> lists the phases in the
    /// reference order; the driver (B-03) calls them in that order with <c>dt = 1/20</c>.
    /// </summary>
    public interface ITickPhase
    {
        void Tick(SimContext ctx, SimState st, double dt);
    }

    /// <summary>Fills one subsystem's part of a fresh state at new-game time (reference createCampaign/createEngineer/ensureFlow pieces).</summary>
    public interface IStateInitializer
    {
        void Init(SimContext ctx, SimState st);
    }
}
