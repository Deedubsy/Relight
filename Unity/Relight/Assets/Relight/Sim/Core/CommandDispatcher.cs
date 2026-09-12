namespace Relight.Sim
{
    /// <summary>
    /// The single dispatch point for commands (reference sim.ts <c>applyCommands</c>, lines 776–789;
    /// TECHNICAL_ARCHITECTURE.md §4.3). The handlers registered in <see cref="SimComposition.Handlers"/> are tried in
    /// that fixed order and the first that recognises the command decides it; an unrecognised command is refused
    /// rather than ignored. Commands are applied only between ticks — <see cref="Simulation"/> owns that rule.
    /// </summary>
    public static class CommandDispatcher
    {
        public const string UnknownCommand = "Unknown command";

        public static CommandResult Apply(SimContext ctx, SimState st, Command c)
        {
            if (c == null) return CommandResult.Refuse(UnknownCommand);
            var handlers = SimComposition.Handlers;
            for (var i = 0; i < handlers.Count; i++)
            {
                if (!handlers[i].TryApply(ctx, st, c, out var result)) continue;
                // Reference: `tiles(st)?.afterCommand?.(st)` runs after every command. Here it runs only after an
                // accepted one — a refusal changed nothing, so the tile layer has nothing to resynchronise.
                if (result.Accepted) ctx.Tiles.AfterCommand(ctx, st, c);
                return result;
            }
            return CommandResult.Refuse(UnknownCommand);
        }
    }
}
