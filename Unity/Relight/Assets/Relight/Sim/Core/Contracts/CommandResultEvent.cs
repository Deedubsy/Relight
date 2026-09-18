namespace Relight.Sim
{
    /// <summary>
    /// What a QUEUED command decided (C-13). A command applied synchronously through
    /// <see cref="Simulation.Apply"/> hands its <see cref="CommandResult"/> straight back to its caller, but a
    /// command handed to <see cref="Simulation.Queue"/> — which is how <c>SimHost.Submit</c>, and therefore all
    /// player input, reaches the sim — is applied at the next flush with nobody left to return to. Its refusal
    /// text would otherwise be lost, and the reference toasts every dispatch result, good or bad
    /// (game/src/worldScene.ts, <c>hooks.onToast(r.reason, r.ok ? 'good' : 'bad')</c>).
    ///
    /// Only results that carry TEXT are emitted (see <see cref="Simulation"/>'s flush): <c>WalkCommand</c>,
    /// <c>MoveCommand</c> and <c>AimCommand</c> are submitted every frame and accept silently, so a blanket event
    /// would flood the list. An accepted command with a note ("Stack moved.") does emit, with
    /// <see cref="Accepted"/> true; what to do with it is the HUD's decision (<c>HudViewModel.Intake</c>).
    ///
    /// Like every <see cref="SimEvent"/> this is drained once per frame and never saved, so it is not part of the
    /// state hash and cannot affect determinism.
    /// </summary>
    /// <param name="Command">The command's type name without its namespace, e.g. "PlaceMachineCommand".</param>
    /// <param name="Accepted"><see cref="CommandResult.Accepted"/>.</param>
    /// <param name="Text"><see cref="CommandResult.Problem"/> — the refusal, or the accepted note.</param>
    public sealed record CommandResultEvent(double T, string Command, bool Accepted, string Text) : SimEvent(T);
}
