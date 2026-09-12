using System;
using System.Collections.Generic;

namespace Relight.Sim
{
    /// <summary>Put a machine of `Kind` down with its north-west corner at (X, Y) (reference flow.ts `place`).</summary>
    public sealed record PlaceMachineCommand(string Kind, int X, int Y, Dir Dir = Dir.N) : Command;

    /// <summary>Pack the machine with this id back into the Backpack, contents and all (reference flow.ts `remove`).</summary>
    public sealed record RemoveMachineCommand(int Id) : Command;

    /// <summary>
    /// Minimal Phase B placement and removal, ported from reference flow.ts `placementGeometryProblem`,
    /// `addMachine`, `pickUpItems`, `canPickUp` and `remove`.
    /// Only the physical rules the Phase B state can express are ported: map bounds, river and building tiles,
    /// machine-on-machine overlap and the engineer's reach. The reference's block ownership, street/margin rules,
    /// rubble, campaign installations, unlocks and the truck are Phase C.
    /// Build cost payment and refund are Phase C too (`stats.Placed` stays zero): a carried machine is spent and
    /// returned like the reference, but nothing is charged in steel or copper. Because a machine kind is not an
    /// item, placing creates nothing the ledger counts — a placed machine becomes a ledger place only once it
    /// holds items.
    /// </summary>
    public static class Placement
    {
        /// <summary>Reference `placementGeometryProblem`, Phase B subset. '' when the footprint is legal.</summary>
        public static string GeometryProblem(SimContext ctx, SimState st, string kind, int x, int y, Dir dir, int ignoreId = -1)
        {
            if (!ctx.Data.TryMachine(kind, out var spec)) return "no such machine";
            var (w, h) = Footprints.Dimensions(kind, dir, spec.Size);
            var g = ctx.Geometry;
            for (var ty = y; ty < y + h; ty++)
                for (var tx = x; tx < x + w; tx++)
                {
                    if (tx < 0 || ty < 0 || tx >= g.Width || ty >= g.Height) return "outside the city";
                    var t = g.TileAt(tx, ty);
                    if (t == TileClass.River) return "in the river";
                    if (t == TileClass.Inert || t == TileClass.Void) return "a city structure is there";
                    for (var i = 0; i < st.Machines.Count; i++)
                    {
                        var m = st.Machines[i];
                        if (m.Id == ignoreId) continue;
                        if (m.Rect.Contains(tx, ty)) return "another machine is there";
                    }
                }
            return "";
        }

        /// <summary>Reference `addMachine`: append in placement order, take the next id, bump the revision.</summary>
        public static Machine Add(SimContext ctx, SimState st, string kind, int x, int y, Dir dir)
        {
            ctx.Data.TryMachine(kind, out var spec);
            var m = new Machine { Id = st.NextId++, Kind = kind, X = x, Y = y, Dir = dir, Size = spec?.Size ?? 1 };
            st.Machines.Add(m);
            st.Rev++;
            return m;
        }

        /// <summary>
        /// Reference `pickUpItems`: the machine itself as one stack, plus everything it held, in a fixed order
        /// (the machine kind, a turret's rounds as bullets, then the inventory in item order, then the finished
        /// output). Belt contents, an inserter's hand, a tram's cargo and artifacts are Phase C.
        /// </summary>
        public static void PickUpItems(GameData d, Machine m, List<ItemKey> keys, List<double> counts)
        {
            keys.Clear();
            counts.Clear();
            keys.Add(new ItemKey(m.Kind));
            counts.Add(1);
            void Add(ItemKey k, double n) { if (n > 0) { keys.Add(k); counts.Add(n); } }
            if (string.Equals(m.Kind, "turret", StringComparison.Ordinal)) { Add(ItemKey.Of(ItemId.Magazine), m.Rounds); return; }
            for (var i = 0; i < Items.Count; i++) Add(ItemKey.Of((ItemId)i), Math.Floor(m.Inv[(ItemId)i]));
            Add(ItemKey.Of(ItemId.Magazine), m.Out);
        }

        /// <summary>Reference `canPickUp`: all of it or none — a full Backpack refuses, with the same message.</summary>
        public static (bool ok, string reason) CanPickUp(SimContext ctx, SimState st, Machine m)
        {
            var d = ctx.Data;
            if (m == null) return (false, "nothing there");
            if (string.Equals(m.Kind, "depot", StringComparison.Ordinal)) return (false, "the Depot stays");
            if (!Interaction.InReach(ctx, st, m)) return (false, $"Walk closer to the {(d.TryMachine(m.Kind, out var s) ? s.DisplayName : m.Kind)}");

            var keys = new List<ItemKey>();
            var counts = new List<double>();
            PickUpItems(d, m, keys, counts);
            var trial = Pockets.Trial(st.Engineer);
            var before = Pockets.Stacks(d, trial.Inv);
            var need = 0;
            for (var i = 0; i < keys.Count; i++)
            {
                need += (int)Math.Ceiling(counts[i] / d.StackSize(keys[i]));
                if (Pockets.Take(d, trial, keys[i], counts[i]) < counts[i])
                    return (false, $"the Backpack is full ({need} stack{(need == 1 ? "" : "s")} to carry, {Math.Max(0, Pockets.Cap(d) - before)} free)");
            }
            return (true, "");
        }

        /// <summary>Reference `remove`: the contents to the Backpack, the machine out of the list, the revision bumped.</summary>
        public static (bool ok, string reason) Remove(SimContext ctx, SimState st, int id)
        {
            var d = ctx.Data;
            var m = st.MachineById(id);
            var (ok, reason) = CanPickUp(ctx, st, m);
            if (!ok) return (false, reason);

            var keys = new List<ItemKey>();
            var counts = new List<double>();
            PickUpItems(d, m, keys, counts);
            for (var i = 0; i < keys.Count; i++) Pockets.Take(d, st.Engineer, keys[i], counts[i]);
            // A turret's loose rounds (rounds that do not make a whole magazine) went to the line buffer in the
            // reference and any remainder was counted in `roundsLost`. One round is one bullet (U-D-08), so there
            // is never a remainder and `roundsLost` stays zero here.
            m.Inv.Clear();
            m.Out = 0;
            m.Rounds = 0;
            st.Machines.Remove(m);
            st.Rev++;
            return (true, "");
        }

        /// <summary>Reference `place` minus the Phase C cost payment: a carried machine is spent, nothing else is.</summary>
        public static (bool ok, string reason) Place(SimContext ctx, SimState st, string kind, int x, int y, Dir dir)
        {
            var d = ctx.Data;
            if (!d.TryMachine(kind, out var spec)) return (false, "no such machine");
            var problem = GeometryProblem(ctx, st, kind, x, y, dir);
            if (problem.Length > 0) return (false, problem);
            var (w, h) = Footprints.Dimensions(kind, dir, spec.Size);
            if (!Interaction.InReach(ctx, st, x, y, w, h)) return (false, $"Walk closer to place the {spec.DisplayName}");
            var carried = new ItemKey(kind);
            if (st.Engineer.Inv[carried] >= 1) Pockets.Drop(d, st.Engineer, carried, 1);
            Add(ctx, st, kind, x, y, dir);
            return (true, "");
        }
    }

    public sealed class PlacementHandler : ICommandHandler
    {
        public bool TryApply(SimContext ctx, SimState st, Command c, out CommandResult result)
        {
            switch (c)
            {
                case PlaceMachineCommand p:
                {
                    var (ok, reason) = Placement.Place(ctx, st, p.Kind, p.X, p.Y, p.Dir);
                    result = ok ? CommandResult.Ok() : CommandResult.Refuse(reason);
                    return true;
                }
                case RemoveMachineCommand r:
                {
                    var (ok, reason) = Placement.Remove(ctx, st, r.Id);
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
