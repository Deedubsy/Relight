namespace Relight.Sim
{
    /// <summary>
    /// EDITOR DEBUG — NOT PLAYER-FACING. Stages one ordinary raid group immediately so the whole combat path
    /// (spawn → approach → breach → turret fire → kill/withdraw) can be exercised in Play Mode without waiting
    /// 25 active minutes. The reference has no equivalent: `packages/game/src/session.ts:21-45` exposes no
    /// raid-forcing parameter, so this is new Unity work (row C-08) and is deliberately gated.
    ///
    /// It is refused unless <see cref="DirectorState.DebugAllowed"/> is true, which nothing in gameplay ever sets —
    /// only the editor menu (<c>Relight/Debug/Trigger raid (editor only)</c>) does, immediately before submitting.
    /// The trigger proves behaviour, not natural pacing: it neither advances the clock nor counts as a raid start.
    /// </summary>
    /// <param name="Count">Bodies to stage; clamped to 1..<c>RaidTuning.LivingBudget</c>.</param>
    /// <param name="Basic">True for the gentle introductory roster (skitters only), as C-09's encounter uses.</param>
    /// <param name="Sector">0..3 = N/E/S/W approach to prefer, or -1 for the director's own choice.</param>
    /// <param name="Scripted">True (the default) keeps the group marked as staged, which the editor menu's "Clear
    /// staged raid" relies on and the raid account skips. REL-119: the Admin panel passes false, so its raid is an
    /// ordinary small raid to the account and the raid log.</param>
    public sealed record DebugRaidCommand(int Count, bool Basic, int Sector = -1, bool Scripted = true) : Command;

    /// <summary>Handles <see cref="DebugRaidCommand"/> only.</summary>
    public sealed class DebugRaidHandler : ICommandHandler
    {
        /// <summary>Shown wherever the command's outcome is surfaced, so a debug raid is never mistaken for a real one.</summary>
        public const string Label = "editor debug — not player-facing";

        public bool TryApply(SimContext ctx, SimState st, Command c, out CommandResult result)
        {
            if (!(c is DebugRaidCommand cmd)) { result = default; return false; }
            if (!st.Director.DebugAllowed)
            {
                result = CommandResult.Refuse("debug raids are disabled (" + Label + ")");
                return true;
            }
            if (st.Director.Minor != null)
            {
                result = CommandResult.Refuse("a raid group is already on the map (" + Label + ")");
                return true;
            }
            var budget = ctx.Data.Raids.LivingBudget;
            var count = cmd.Count < 1 ? 1 : cmd.Count > budget ? budget : cmd.Count;

            var origin = -1;
            if (cmd.Sector >= 0)
            {
                var list = DirectorRules.Approaches(ctx, st);
                if (list != null && list.Length > 0)
                {
                    DirectorRules.Target(ctx, st, out var bx, out var by, out var tw, out var th);
                    var cx = bx + tw / 2.0;
                    var cy = by + th / 2.0;
                    var w = ctx.Geometry.Width;
                    for (var i = 0; i < list.Length; i++)
                    {
                        if (DirectorRules.Sector(list[i] % w + .5 - cx, list[i] / w + .5 - cy) != (cmd.Sector & 3)) continue;
                        origin = list[i];
                        break;
                    }
                }
            }
            if (origin < 0) origin = DirectorRules.Origin(ctx, st);
            if (origin < 0)
            {
                result = CommandResult.Refuse("no reachable approach (" + Label + ")");
                return true;
            }

            var id = Director.ScheduleGroup(ctx, st, origin, count, cmd.Basic, 0, cmd.Scripted);
            result = id == 0
                ? CommandResult.Refuse("no open ground at the approach (" + Label + ")")
                : CommandResult.Ok("Debug raid staged: " + count + " bodies (" + Label + ")");
            return true;
        }
    }
}
