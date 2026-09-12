using System;

namespace Relight.Sim
{
    /// <summary>Walk-here: set a tile-space target and drop held keys (reference engineer.ts:325 <c>case 'move'</c>).</summary>
    public sealed record MoveCommand(double X, double Y) : Command;

    /// <summary>Held movement keys as a direction, (0,0) to stop (reference engineer.ts:326 <c>case 'walk'</c>).</summary>
    public sealed record WalkCommand(double Dx, double Dy) : Command;

    /// <summary>Shift held or released (reference engineer.ts:329 <c>case 'sprint'</c>).</summary>
    public sealed record SprintCommand(bool On) : Command;

    /// <summary>The dodge (reference engineer.ts:330 <c>case 'dodge'</c>).</summary>
    public sealed record DodgeCommand : Command;

    /// <summary>Cursor aim, or aim cleared (reference engineer.ts:337 <c>case 'aim'</c>). What is aimed comes in Phase C.</summary>
    public sealed record AimCommand(bool HasAim, double X, double Y) : Command;

    /// <summary>
    /// Applies the movement commands (reference engineer.ts:321 <c>engineerCommand</c>, the movement cases only).
    /// Deliberately not handled here: <c>walkTo</c> (a BLOCK index — the block lattice is retired,
    /// CONTENT_CATALOGUE.md §17; the player's click-to-move is <see cref="MoveCommand"/>), <c>fire</c> and
    /// <c>enterTruck</c> (Phase C combat / truck), and the passenger guard, which reads Phase C state.
    ///
    /// The hand-crafting lock (reference walk.ts:181-183) IS enforced: while a batch runs, a walk-here and a dodge
    /// are refused with <see cref="HandCraft.LockText"/> so the UI can say why, and held keys are still recorded but
    /// produce no movement until the batch ends (<see cref="EngineerMovementPhase"/>). Cancelling stays available:
    /// <c>CancelCraftCommand</c> belongs to the hand-craft handler and is never gated on movement.
    /// </summary>
    public sealed class MovementCommandHandler : ICommandHandler
    {
        public bool TryApply(SimContext ctx, SimState st, Command c, out CommandResult result)
        {
            var e = st.Engineer;
            var locked = HandCraft.HandLocked(st);
            switch (c)
            {
                case MoveCommand m:
                    if (locked) { result = CommandResult.Refuse(HandCraft.LockText); return true; }
                    if (!e.IsDown)
                    {
                        e.HasTarget = true;
                        e.Target = new Vec2(m.X, m.Y);
                        e.Vel = Vec2.Zero;
                    }
                    result = CommandResult.Ok();
                    return true;

                case WalkCommand w:
                    // Accepted even while locked: it only records which keys are held, and the mover ignores that
                    // until the batch ends. Refusing it would lose a key pressed (or released) during the batch,
                    // because the presentation is level-triggered and would never re-send it.
                    if (!e.IsDown)
                    {
                        e.Vel = new Vec2(w.Dx, w.Dy);
                        if (w.Dx != 0 || w.Dy != 0) e.HasTarget = false;
                    }
                    result = CommandResult.Ok();
                    return true;

                case SprintCommand s:
                    e.Sprint = s.On && !e.IsDown;
                    result = CommandResult.Ok();
                    return true;

                case DodgeCommand _:
                    if (locked) { result = CommandResult.Refuse(HandCraft.LockText); return true; }
                    // The guard is the reference's, including its 1e-9 slack on the stamina cost.
                    var d = ctx.Data.Engineer;
                    if (!e.IsDown && e.Dash <= 0 && e.DashCooldown <= 0
                        && e.Stamina >= d.DashCost - 1e-9)
                    {
                        var l = Math.Sqrt(e.Vel.X * e.Vel.X + e.Vel.Y * e.Vel.Y);
                        e.DashDir = l > 0 ? new Vec2(e.Vel.X / l, e.Vel.Y / l) : e.Face;
                        e.Dash = d.DashSeconds;
                        e.DashCooldown = d.DashCooldownS;
                        e.Stamina -= d.DashCost;
                        e.HasTarget = false;
                        result = CommandResult.Ok();
                        return true;
                    }
                    result = CommandResult.Refuse(e.IsDown ? "The engineer is down." : "Not ready to dodge.");
                    return true;

                case AimCommand a:
                    e.HasAim = !e.IsDown && a.HasAim;
                    e.Aim = e.HasAim ? new Vec2(a.X, a.Y) : Vec2.Zero;
                    result = CommandResult.Ok();
                    return true;

                default:
                    result = default;
                    return false;
            }
        }
    }
}
