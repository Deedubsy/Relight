namespace Relight.Sim
{
    /// <summary>
    /// Put the hands on the tile at (X, Y) and keep them there (reference flow.ts <c>setHandMine(st, [x, y])</c>,
    /// driven by hold-left-click in game/src/worldScene.ts:766). Re-sending the same tile is harmless.
    /// </summary>
    public sealed record MineCommand(int X, int Y) : Command;

    /// <summary>Hands off (reference <c>setHandMine(st, null)</c>, worldScene.ts:791 on button release).</summary>
    public sealed record StopMiningCommand : Command;

    /// <summary>One item came out of the ground (presentation: a puff over the tile, a Backpack flash).</summary>
    public sealed record MinedEvent(double T, int X, int Y, ItemId Item, bool Cleared, int MachineId = -1) : SimEvent(T);

    /// <summary>The hands came off the tile and why (presentation: the toast the reference HUD shows).</summary>
    public sealed record MiningStoppedEvent(double T, int X, int Y, string Reason) : SimEvent(T);

    public sealed class MiningHandler : ICommandHandler
    {
        public bool TryApply(SimContext ctx, SimState st, Command c, out CommandResult result)
        {
            switch (c)
            {
                case MineCommand m:
                {
                    var (ok, reason) = Mining.Start(ctx, st, m.X, m.Y);
                    result = ok ? CommandResult.Ok() : CommandResult.Refuse(reason);
                    return true;
                }
                case StopMiningCommand _:
                    Mining.Stop(st, "");
                    result = CommandResult.Ok();
                    return true;
                default:
                    result = default;
                    return false;
            }
        }
    }
}
