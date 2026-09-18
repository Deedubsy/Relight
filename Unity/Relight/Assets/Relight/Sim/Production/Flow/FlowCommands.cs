using System;

namespace Relight.Sim
{
    /// <summary>
    /// Lay an underground pair in one action: an entrance at (FromX, FromY) and an exit at (ToX, ToY), both facing
    /// <see cref="Dir"/> (reference construction.ts:299 <c>'undergroundPair'</c> + <c>undergroundCheck</c>).
    /// A plain <see cref="PlaceMachineCommand"/> for "underground" still puts down a single entrance, exactly as
    /// reference <c>addMachine</c> does (flow.ts: a new underground is always <c>'input'</c>); the exit half only
    /// ever comes from this command, so the two endpoints are always paid for and validated together.
    /// </summary>
    public sealed record PlaceUndergroundPairCommand(int FromX, int FromY, int ToX, int ToY, Dir Dir) : Command;

    /// <summary>Set an inserter's item filter, or clear it with <c>Item = -1</c> (reference routing.ts <c>configureRouting</c>).</summary>
    public sealed record SetInserterFilterCommand(int Id, int Item) : Command;

    /// <summary>Set a splitter's output priority: 0 balanced, 1 left, 2 right (reference routing.ts <c>configureRouting</c>).</summary>
    public sealed record SetSplitterPriorityCommand(int Id, int Priority) : Command;

    /// <summary>Placement and configuration rules that belong to the flow layer.</summary>
    public static class FlowBuild
    {
        public const string Kind = "underground";

        /// <summary>
        /// Reference construction.ts:218 <c>undergroundCheck</c>, minus the truck builder and the blueprint edit
        /// pipeline (neither is ported): the span, both footprints, reach to both ends, the price of two, and no
        /// third endpoint of the same facing in between to steal the pairing.
        /// </summary>
        public static (bool ok, string reason) PairBuildable(SimContext ctx, SimState st, int fx, int fy, int tx, int ty, Dir dir)
        {
            var d = ctx.Data;
            if (!d.TryMachine(Kind, out var spec)) return (false, "no such machine");
            var span = FlowRules.UndergroundSpan(fx, fy, tx, ty, dir);
            if (span.Length > 0) return (false, span);

            var a = ProductionRules.MachineAt(st, fx, fy);
            var b = ProductionRules.MachineAt(st, tx, ty);
            if (a != null && b != null && FlowRules.IsUnderground(a.Kind) && FlowRules.IsUnderground(b.Kind)
                && a.Dir == dir && b.Dir == dir && !FlowRules.IsUndergroundOutput(st, a) && FlowRules.IsUndergroundOutput(st, b))
                return (false, "These underground endpoints are already built");

            var pa = Placement.GeometryProblem(ctx, st, Kind, fx, fy, dir);
            if (pa.Length > 0) return (false, pa);
            var pb = Placement.GeometryProblem(ctx, st, Kind, tx, ty, dir);
            if (pb.Length > 0) return (false, pb);

            var (w, h) = Footprints.Dimensions(Kind, dir, spec.Size);
            if (!Interaction.InReach(ctx, st, fx, fy, w, h) || !Interaction.InReach(ctx, st, tx, ty, w, h))
                return (false, "Move the builder closer to both underground endpoints");

            // Reference `undergroundMate` takes the FIRST same-facing underground along the run, so anything of the
            // same facing between the two ends would pair with the entrance instead of the exit.
            var steps = Math.Abs(tx - fx) + Math.Abs(ty - fy);
            for (var s = 1; s < steps; s++)
            {
                var n = ProductionRules.MachineAt(st, fx + Dirs.DX[(int)dir] * s, fy + Dirs.DY[(int)dir] * s);
                if (n != null && FlowRules.IsUnderground(n.Kind) && n.Dir == dir)
                    return (false, "Another underground endpoint interrupts this pair");
            }

            var inv = st.Engineer.Inv;
            var carried = Math.Floor(inv[new ItemKey(Kind)]);
            var toBuy = Math.Max(0, 2 - carried);
            for (var i = 0; i < spec.Cost.Count; i++)
                if (inv[spec.Cost[i].Item] < spec.Cost[i].Count * toBuy)
                    return (false, $"not enough in the Backpack ({Placement.CostText(d, spec.Cost)} × 2)");
            return (true, "");
        }

        /// <summary>Validate, then place both endpoints and mark the far one as the exit.</summary>
        public static (bool ok, string reason) PlacePair(SimContext ctx, SimState st, int fx, int fy, int tx, int ty, Dir dir)
        {
            var (ok, reason) = PairBuildable(ctx, st, fx, fy, tx, ty, dir);
            if (!ok) return (false, reason);
            var entrance = Placement.Place(ctx, st, Kind, fx, fy, dir);
            if (!entrance.ok) return entrance;
            var exit = Placement.Place(ctx, st, Kind, tx, ty, dir);
            if (!exit.ok) return exit;
            var m = ProductionRules.MachineAt(st, tx, ty);
            if (m != null) st.Flow.Of(m.Id).Mode = 1;
            st.Rev++;
            return (true, "");
        }

        /// <summary>Reference routing.ts:102 <c>configureRouting</c>, filter half.</summary>
        public static (bool ok, string reason) SetFilter(SimContext ctx, SimState st, int id, int item)
        {
            var m = st.MachineById(id);
            if (m == null) return (false, "nothing there");
            if (!Interaction.InReach(ctx, st, m)) return (false, "Walk closer to configure the machine");
            if (!FlowRules.IsInserter(m.Kind) || item < -1 || item >= Items.Count)
                return (false, "Choose an item filter on an inserter");
            st.Flow.Of(id).Filter = item;
            st.Rev++;
            return (true, "");
        }

        /// <summary>Reference routing.ts:102 <c>configureRouting</c>, priority half.</summary>
        public static (bool ok, string reason) SetPriority(SimContext ctx, SimState st, int id, int priority)
        {
            var m = st.MachineById(id);
            if (m == null) return (false, "nothing there");
            if (!Interaction.InReach(ctx, st, m)) return (false, "Walk closer to configure the machine");
            if (!FlowRules.IsSplitter(m.Kind) || priority < 0 || priority > 2)
                return (false, "Choose balanced, left or right splitter output");
            st.Flow.Of(id).Priority = priority;
            st.Rev++;
            return (true, "");
        }
    }

    public sealed class FlowHandler : ICommandHandler
    {
        public bool TryApply(SimContext ctx, SimState st, Command c, out CommandResult result)
        {
            switch (c)
            {
                case PlaceUndergroundPairCommand p:
                {
                    if (HandCraft.HandLocked(st)) { result = CommandResult.Refuse(HandCraft.LockTextFor(st)); return true; }
                    var (ok, reason) = FlowBuild.PlacePair(ctx, st, p.FromX, p.FromY, p.ToX, p.ToY, p.Dir);
                    result = ok ? CommandResult.Ok() : CommandResult.Refuse(reason);
                    return true;
                }
                case SetInserterFilterCommand f:
                {
                    var (ok, reason) = FlowBuild.SetFilter(ctx, st, f.Id, f.Item);
                    result = ok ? CommandResult.Ok() : CommandResult.Refuse(reason);
                    return true;
                }
                case SetSplitterPriorityCommand s:
                {
                    var (ok, reason) = FlowBuild.SetPriority(ctx, st, s.Id, s.Priority);
                    result = ok ? CommandResult.Ok() : CommandResult.Refuse(reason);
                    return true;
                }
                default:
                    result = default;
                    return false;
            }
        }
    }
}
