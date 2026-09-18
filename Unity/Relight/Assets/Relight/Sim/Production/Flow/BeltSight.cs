using System;

namespace Relight.Sim
{
    /// <summary>
    /// The belt equivalent of <see cref="TurretSight"/>'s blind-position warning: advice for the placement
    /// preview, never a refusal, so <c>Placement.Validity</c> is untouched by it.
    ///
    /// A conveyor does not receive what a machine beside it pushes out. <see cref="FlowPhase"/>'s
    /// <c>LoadConveyor</c> PULLS from the one tile BEHIND the arrow — <c>m.X - Dirs.DX[m.Dir]</c>,
    /// <c>m.Y - Dirs.DY[m.Dir]</c> — and <c>FlowRules.Accepts</c> tests only that the belt has room, with no
    /// direction test at all. A belt laid alongside an Excavator, or rotated a quarter turn off its source, is
    /// therefore accepted in silence and then carries nothing for the rest of the run.
    ///
    /// That is the most expensive mistake available in the opening, because every other readout agrees with the
    /// player: the objective chain calls the line "connected" from the route, and the machine reports "output
    /// full", and neither of them names the rotation. So this says it once, at the only moment it is cheap to fix.
    ///
    /// Deliberately quiet. It speaks only when all three hold: the tile behind the arrow is empty, a machine a
    /// belt could actually drain sits to one SIDE of it, and the arrow does not point into that machine (which is
    /// an ordinary delivery). Anything else — a belt fed by the belt upstream of it, a splitter's two ports, a
    /// half-laid line out in the open — returns "".
    /// </summary>
    public static class BeltSight
    {
        /// <summary>One sentence for the player, or "" when the placement needs no warning.</summary>
        public static string Advise(SimContext ctx, SimState st, string kind, int x, int y, Dir dir)
        {
            if (ctx == null || st == null || ctx.Data == null) return "";
            if (!FlowRules.IsConveyor(kind) || FlowRules.IsSplitter(kind)) return "";

            // Fed by something already — a machine, or the belt upstream. Nothing to say.
            if (ProductionRules.MachineAt(st, x - Dirs.DX[(int)dir], y - Dirs.DY[(int)dir]) != null) return "";

            Machine beside = null;
            for (var k = 0; k < 4; k++)
            {
                if (k == (int)dir) continue;                       // the output side: a delivery, not a mistake
                var m = ProductionRules.MachineAt(st, x + Dirs.DX[k], y + Dirs.DY[k]);
                if (m == null || !Drains(ctx, m)) continue;
                beside = m;
                break;
            }
            if (beside == null) return "";

            return "This belt would pull from the empty tile behind its arrow. " + Name(ctx, beside)
                + " is beside it, not behind it, so nothing would ever load: rotate with R until the arrow points "
                + "away from " + Name(ctx, beside) + ".";
        }

        /// <summary>
        /// The kinds <see cref="FlowPhase.SourceCount"/> lets a belt take from, narrowed to the ones a player
        /// actually builds a belt to empty. Turrets, cannons and generators are drainable too, but a belt laid
        /// next to one is nearly always meant to FILL it, and warning there would be noise.
        /// </summary>
        private static bool Drains(SimContext ctx, Machine m)
        {
            var d = ctx.Data;
            return ProductionRules.IsMiner(d, m)
                || ProductionRules.IsProcessor(d, m)
                || string.Equals(m.Kind, "chest", StringComparison.Ordinal);
        }

        private static string Name(SimContext ctx, Machine m) =>
            ctx.Data.TryMachine(m.Kind, out var spec) ? "The " + spec.DisplayName : "That machine";
    }
}
