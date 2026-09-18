using System;
using System.Collections.Generic;

namespace Relight.Sim
{
    /// <summary>
    /// B-10's pure rules: what a conveyor is, where its items go next, and whether a machine would take an item.
    /// Ported from reference flow.ts (<c>isBelt</c>, <c>beltRoom</c>, <c>beltInsert</c>, <c>accepts</c>,
    /// <c>wants</c>, <c>giveItem</c>, <c>nextOf</c>, <c>entryDir</c>, <c>beltOrder</c>) and routing.ts
    /// (<c>isRouting</c>, <c>isConveyor</c>, <c>undergroundMate</c>, <c>undergroundSpan</c>, <c>splitterPorts</c>,
    /// <c>routingAccepts</c>, <c>routingInsert</c>, <c>routingEntry</c>) and directConveyor.ts
    /// (<c>conveyorDestinations</c>).
    ///
    /// Everything that is a rate or a price comes from <see cref="GameData"/>; the four routing shape constants the
    /// catalogue has no row for (belt spacing, hidden-tile span, splitter buffer and splitter service rate) are
    /// named here with their reference source.
    /// </summary>
    public static class FlowRules
    {
        /// <summary>Reference flow.ts:24 <c>EPS</c>.</summary>
        public const double Eps = 1e-9;

        /// <summary>Reference flow.ts <c>BELT_SPACING</c> (0.25): the gap a rigid chain of items keeps, in tiles.</summary>
        public const double BeltSpacing = 0.25;

        /// <summary>Reference routing.ts:8 <c>UNDERGROUND_HIDDEN</c> (4 hidden tiles, so a span of at most five).</summary>
        public const int UndergroundHidden = 4;

        /// <summary>Reference routing.ts:8 <c>SPLITTER_CAPACITY</c> (8 buffered items).</summary>
        public const int SplitterCapacity = 8;

        /// <summary>Reference routing.ts:8 <c>SPLITTER_PER_S</c> (15 items/s served).</summary>
        public const double SplitterPerS = 15;

        // ---- kinds ---------------------------------------------------------------------------------------------

        /// <summary>Reference flow.ts:147 <c>isBelt</c>.</summary>
        public static bool IsBelt(string kind) =>
            string.Equals(kind, "belt", StringComparison.Ordinal) || string.Equals(kind, "fastbelt", StringComparison.Ordinal);

        /// <summary>Reference routing.ts:13 <c>isRouting</c>.</summary>
        public static bool IsRouting(string kind) =>
            string.Equals(kind, "underground", StringComparison.Ordinal) || string.Equals(kind, "splitter", StringComparison.Ordinal);

        /// <summary>Reference routing.ts:14 <c>isConveyor</c>.</summary>
        public static bool IsConveyor(string kind) => IsBelt(kind) || IsRouting(kind);

        public static bool IsInserter(string kind) => string.Equals(kind, "inserter", StringComparison.Ordinal);

        public static bool IsSplitter(string kind) => string.Equals(kind, "splitter", StringComparison.Ordinal);

        public static bool IsUnderground(string kind) => string.Equals(kind, "underground", StringComparison.Ordinal);

        // ---- tuning read from the catalogue --------------------------------------------------------------------

        /// <summary>Seconds in one tile tick (reference constants.ts <c>TILE_DT</c>), from <see cref="TimeTuning"/>.</summary>
        public static double TileDt(GameData d) => d.Time != null && d.Time.TileDt > 0 ? d.Time.TileDt : 1.0 / 20;

        /// <summary>
        /// A device's travel speed in tiles/s. Reference flow.ts uses <c>BELT_SPEED * (kind==='fastbelt'?2:1)</c>
        /// with <c>BELT_SPEED = BELT_PER_S * BELT_SPACING</c>; the fast belt's 15/s is exactly twice the belt's
        /// 7.5/s, so reading <c>MachineSpec.RatePerS</c> is the same number with the catalogue owning it.
        /// An underground moves at plain belt speed in the reference, and the data gives it no rate of its own.
        /// </summary>
        public static double Speed(GameData d, Machine m)
        {
            var key = IsBelt(m.Kind) ? m.Kind : "belt";
            return d.TryMachine(key, out var spec) && spec.RatePerS > 0 ? spec.RatePerS * BeltSpacing : 7.5 * BeltSpacing;
        }

        /// <summary>Reference flow.ts <c>INSERTER_SWING = 0.5 / INSERTER_PER_S</c> — half a cycle out, half back.</summary>
        public static double InserterSwing(GameData d) =>
            0.5 / (d.TryMachine("inserter", out var spec) && spec.RatePerS > 0 ? spec.RatePerS : 1);

        // ---- geometry ------------------------------------------------------------------------------------------

        /// <summary>Reference routing.ts:28 <c>splitterPorts</c>: the two tiles in front (or, with <paramref name="front"/> false, behind), left then right.</summary>
        public static void SplitterPorts(Machine m, bool front, out int ax, out int ay, out int bx, out int by)
        {
            var sign = front ? 1 : -1;
            switch (m.Dir)
            {
                case Dir.N: ax = m.X; ay = m.Y - sign; bx = m.X + 1; by = m.Y - sign; return;
                case Dir.E: ax = m.X + sign; ay = m.Y; bx = m.X + sign; by = m.Y + 1; return;
                case Dir.S: ax = m.X + 1; ay = m.Y + sign; bx = m.X; by = m.Y + sign; return;
                default: ax = m.X - sign; ay = m.Y + 1; bx = m.X - sign; by = m.Y; return;
            }
        }

        /// <summary>The underground endpoint's role: true when this endpoint is the exit (reference <c>m.underground === 'output'</c>).</summary>
        public static bool IsUndergroundOutput(SimState st, Machine m) =>
            IsUnderground(m.Kind) && (st.Flow.Find(m.Id)?.Mode ?? 0) == 1;

        /// <summary>
        /// Reference routing.ts:15 <c>undergroundMate</c>: the opposite endpoint, facing the same way, within the
        /// hidden span. The entrance looks forward, the exit looks back.
        /// </summary>
        public static Machine UndergroundMate(SimState st, Machine m)
        {
            if (!IsUnderground(m.Kind)) return null;
            var output = IsUndergroundOutput(st, m);
            var sign = output ? -1 : 1;
            for (var d = 1; d <= UndergroundHidden + 1; d++)
            {
                var n = ProductionRules.MachineAt(st, m.X + Dirs.DX[(int)m.Dir] * d * sign, m.Y + Dirs.DY[(int)m.Dir] * d * sign);
                if (n == null || !IsUnderground(n.Kind) || n.Dir != m.Dir) continue;
                return IsUndergroundOutput(st, n) != output ? n : null;
            }
            return null;
        }

        /// <summary>Reference routing.ts:23 <c>undergroundSpan</c>: '' when the exit lies along the entrance within four hidden tiles.</summary>
        public static string UndergroundSpan(int fromX, int fromY, int toX, int toY, Dir dir)
        {
            var dx = toX - fromX;
            var dy = toY - fromY;
            var d = dx * Dirs.DX[(int)dir] + dy * Dirs.DY[(int)dir];
            return d >= 1 && d <= UndergroundHidden + 1 && dx == Dirs.DX[(int)dir] * d && dy == Dirs.DY[(int)dir] * d
                ? "" : "Face the output along the input: at most four hidden tiles";
        }

        /// <summary>The hidden run of a paired underground, in tiles (reference <c>Math.abs(dx)+Math.abs(dy)</c>).</summary>
        public static double UndergroundLength(Machine m, Machine mate) =>
            mate == null ? 1 : Math.Abs(m.X - mate.X) + Math.Abs(m.Y - mate.Y);

        /// <summary>
        /// Reference routing.ts:45 <c>routingEntry</c>: belts enter a splitter only through its rear ports and an
        /// underground only through the tile behind its entrance. Everything else is entered from anywhere.
        /// </summary>
        public static bool RoutingEntry(SimState st, Machine m, int x, int y)
        {
            if (IsSplitter(m.Kind))
            {
                SplitterPorts(m, false, out var ax, out var ay, out var bx, out var by);
                return (ax == x && ay == y) || (bx == x && by == y);
            }
            if (!IsUnderground(m.Kind)) return true;
            return !IsUndergroundOutput(st, m)
                   && x == m.X - Dirs.DX[(int)m.Dir] && y == m.Y - Dirs.DY[(int)m.Dir];
        }

        /// <summary>
        /// Reference flow.ts:810 <c>nextOf</c>: the machine a conveyor hands to — nothing when a routing device
        /// would be entered through the wrong face, and nothing when two conveyors point head-on at each other.
        /// </summary>
        public static Machine NextOf(SimState st, Machine m)
        {
            var n = ProductionRules.MachineAt(st, m.X + Dirs.DX[(int)m.Dir], m.Y + Dirs.DY[(int)m.Dir]);
            if (n == null) return null;
            if (IsRouting(n.Kind) && !RoutingEntry(st, n, m.X, m.Y)) return null;
            if (IsConveyor(n.Kind) && (int)Dirs.Opposite(n.Dir) == (int)m.Dir) return null;
            return n;
        }

        /// <summary>
        /// Reference flow.ts:819 <c>entryDir</c>: the direction items travel as they enter this belt, so the
        /// presentation can draw a corner. The belt behind wins, then a side belt pointing in, else straight.
        /// </summary>
        public static Dir EntryDir(SimState st, Machine m)
        {
            var back = ProductionRules.MachineAt(st, m.X - Dirs.DX[(int)m.Dir], m.Y - Dirs.DY[(int)m.Dir]);
            if (back != null && IsBelt(back.Kind) && back.Dir == m.Dir) return m.Dir;
            for (var q = 1; q <= 3; q += 2)
            {
                var d = Dirs.Rotate(m.Dir, q);
                var side = ProductionRules.MachineAt(st, m.X - Dirs.DX[(int)d], m.Y - Dirs.DY[(int)d]);
                if (side != null && IsBelt(side.Kind) && side.Dir == d) return d;
            }
            return m.Dir;
        }

        // ---- acceptance ----------------------------------------------------------------------------------------

        /// <summary>Reference flow.ts:755 <c>beltRoom</c>: an item may join only where no other item is within one spacing.</summary>
        public static bool BeltRoom(FlowMachine lane, double p)
        {
            if (lane == null) return true;
            for (var i = 0; i < lane.Items.Count; i++)
                if (Math.Abs(lane.Items[i].P - p) < BeltSpacing - Eps) return false;
            return true;
        }

        /// <summary>Reference routing.ts:35 <c>routingAccepts</c>.</summary>
        public static bool RoutingAccepts(SimState st, Machine m)
        {
            var lane = st.Flow.Find(m.Id);
            var n = lane?.Items.Count ?? 0;
            if (IsSplitter(m.Kind)) return n < SplitterCapacity;
            if (IsUndergroundOutput(st, m)) return false;
            var length = UndergroundLength(m, UndergroundMate(st, m));
            return n < length / BeltSpacing && (n == 0 || lane.Items[0].P >= BeltSpacing - Eps);
        }

        /// <summary>
        /// Reference flow.ts:769 <c>accepts</c>, whole. The conveyor kinds are answered here; every other kind is
        /// <see cref="MachineInventory.Accepts(GameData, SimState, Machine, ItemId)"/>, which is the same switch.
        /// The retired Depot and tram-stop branches (U-D-32, RI-05) are not ported.
        /// </summary>
        public static bool Accepts(SimContext ctx, SimState st, Machine m, ItemId k, double p = 0)
        {
            if (IsRouting(m.Kind)) return RoutingAccepts(st, m);
            if (IsBelt(m.Kind)) return BeltRoom(st.Flow.Find(m.Id), p);
            return MachineInventory.Accepts(ctx.Data, st, m, k);
        }

        /// <summary>
        /// Reference flow.ts:786 <c>wants</c>: could this machine ever take this item, ignoring how full it is.
        /// An inserter picks up by this and waits by <see cref="Accepts"/>.
        /// </summary>
        public static bool Wants(SimContext ctx, SimState st, Machine m, ItemId k)
        {
            var d = ctx.Data;
            if (IsSplitter(m.Kind)) return true;
            if (IsUnderground(m.Kind)) return !IsUndergroundOutput(st, m);
            if (IsBelt(m.Kind)) return true;
            // Reference flow.ts:789 `m.kind === 'turret' ? k === 'magazine'`, via W-B's kind-agnostic hopper so the
            // cannon answers for shells with the same one line.
            if (TurretHopper.IsTurret(d, m)) return k == TurretHopper.Ammo(d, m);
            switch (m.Kind)
            {
                case "chest": return true;
                case "generator": return k == ItemId.Coal || k == ItemId.Fuel;
                default:
                    return ProductionRules.IsProcessor(d, m) && ProductionRules.Need(ProductionRules.RecipeOf(d, st, m), k) > 0;
            }
        }

        /// <summary>
        /// Reference flow.ts:798 <c>giveItem</c>: the acceptance test and the hand-over in one, for every kind.
        /// Non-conveyor destinations are <see cref="MachinePhase.GiveItem"/>, which is the same code.
        /// </summary>
        public static bool GiveItem(SimContext ctx, SimState st, Machine m, ItemId k, double p = 0, BeltItem moving = null)
        {
            if (!IsConveyor(m.Kind)) return MachinePhase.GiveItem(ctx, st, m, k);
            if (!Accepts(ctx, st, m, k, p)) return false;
            var lane = st.Flow.Of(m.Id);
            if (IsSplitter(m.Kind)) lane.Items.Add(new BeltItem { K = (int)k, P = 0 });   // routing.ts:41 routingInsert
            else if (IsRouting(m.Kind)) BeltInsert(ctx.Data, m, lane, k, 0);
            else
            {
                moving = moving ?? new BeltItem { PreviousPosition = FlowQueries.Position(st, m, p), HasPreviousPosition = true };
                BeltInsert(ctx.Data, m, lane, k, p, moving);
            }
            return true;
        }

        /// <summary>
        /// Reference flow.ts:758 <c>beltInsert</c>: an item joining a moving chain closes up to exactly one spacing
        /// behind the tail, at most one tick's travel ahead of where it was put, and the list stays sorted by
        /// position so index 0 is the tail and the last entry is the leader.
        /// </summary>
        public static void BeltInsert(GameData d, Machine m, FlowMachine lane, ItemId k, double p, BeltItem moving = null)
        {
            if (lane.Items.Count > 0) p = Math.Min(lane.Items[0].P - BeltSpacing, p + Speed(d, m) * TileDt(d));
            var i = lane.Items.Count;
            while (i > 0 && lane.Items[i - 1].P > p) i--;
            var item = moving ?? new BeltItem();
            item.K = (int)k; item.P = p;
            lane.Items.Insert(i, item);
        }

        // ---- caches --------------------------------------------------------------------------------------------

        /// <summary>
        /// Reference flow.ts:831 <c>beltOrder</c>: belts downstream first, so a gap opens ahead before the belt
        /// behind moves into it and a saturated line carries its full rate every tick. Cached on
        /// <see cref="SimState.Rev"/> and the machine count, exactly as the reference's <c>orderCache</c> WeakMap is
        /// cached on <c>f.rev</c> and <c>f.machines.length</c>. The reference's recursion is an explicit stack here
        /// so a long line cannot overflow.
        /// </summary>
        public static List<Machine> BeltOrder(SimState st)
        {
            var f = st.Flow;
            if (f.OrderRev == st.Rev && f.OrderCount == st.Machines.Count) return f.Order;
            f.Order.Clear();
            var mark = new byte[Math.Max(1, st.NextId)];
            var stack = new List<(Machine m, int stage)>();
            for (var i = 0; i < st.Machines.Count; i++)
            {
                var start = st.Machines[i];
                if (!IsBelt(start.Kind)) continue;
                stack.Add((start, 0));
                while (stack.Count > 0)
                {
                    var (cur, stage) = stack[stack.Count - 1];
                    stack.RemoveAt(stack.Count - 1);
                    if (stage == 0)
                    {
                        if (cur.Id < mark.Length && mark[cur.Id] != 0) continue;
                        if (cur.Id < mark.Length) mark[cur.Id] = 1;
                        stack.Add((cur, 1));
                        var n = NextOf(st, cur);
                        if (n != null && IsBelt(n.Kind) && (n.Id >= mark.Length || mark[n.Id] == 0)) stack.Add((n, 0));
                    }
                    else
                    {
                        if (cur.Id < mark.Length) mark[cur.Id] = 2;
                        f.Order.Add(cur);
                    }
                }
            }
            f.OrderRev = st.Rev;
            f.OrderCount = st.Machines.Count;
            return f.Order;
        }

        /// <summary>
        /// Reference directConveyor.ts:7 <c>conveyorDestinations</c>: for each conveyor, the non-conveyor machines
        /// its items can reach, following splitter ports, an underground's mate and <see cref="NextOf"/>. Rebuilt
        /// only for topology changes (<see cref="SimState.Rev"/> and the machine count), never for stock changes.
        /// </summary>
        public static List<ConveyorRoute> Destinations(SimState st)
        {
            var f = st.Flow;
            if (f.RouteRev == st.Rev && f.RouteCount == st.Machines.Count) return f.Routes;
            f.Routes.Clear();
            var size = Math.Max(1, st.NextId);
            var seen = new int[size];
            var got = new int[size];
            var stamp = 0;
            var todo = new List<Machine>();
            for (var i = 0; i < st.Machines.Count; i++)
            {
                var start = st.Machines[i];
                if (!IsConveyor(start.Kind)) continue;
                stamp++;
                var route = new ConveyorRoute { Id = start.Id };
                todo.Clear();
                todo.Add(start);
                while (todo.Count > 0)
                {
                    var m = todo[todo.Count - 1];
                    todo.RemoveAt(todo.Count - 1);
                    if (m.Id < size && seen[m.Id] == stamp) continue;
                    if (m.Id < size) seen[m.Id] = stamp;
                    if (!IsConveyor(m.Kind))
                    {
                        if (m.Id >= size || got[m.Id] != stamp)
                        {
                            if (m.Id < size) got[m.Id] = stamp;
                            route.Ends.Add(m);
                        }
                        continue;
                    }
                    if (IsSplitter(m.Kind))
                    {
                        SplitterPorts(m, true, out var ax, out var ay, out var bx, out var by);
                        PushPort(st, m, ax, ay, todo);
                        PushPort(st, m, bx, by, todo);
                    }
                    else if (IsUnderground(m.Kind) && !IsUndergroundOutput(st, m))
                    {
                        var n = UndergroundMate(st, m);
                        if (n != null) todo.Add(n);
                    }
                    else
                    {
                        var n = NextOf(st, m);
                        if (n != null) todo.Add(n);
                    }
                }
                f.Routes.Add(route);
            }
            f.RouteRev = st.Rev;
            f.RouteCount = st.Machines.Count;
            return f.Routes;
        }

        private static void PushPort(SimState st, Machine m, int x, int y, List<Machine> todo)
        {
            var n = ProductionRules.MachineAt(st, x, y);
            if (n == null) return;
            if (IsConveyor(n.Kind) && (int)n.Dir == (int)Dirs.Opposite(m.Dir)) return;
            if (IsRouting(n.Kind) && !RoutingEntry(st, n, x - Dirs.DX[(int)m.Dir], y - Dirs.DY[(int)m.Dir])) return;
            todo.Add(n);
        }

        /// <summary>
        /// Reference routing.ts:50 <c>forwardTarget</c>: the machine at (x,y) a routing device may hand to.
        /// </summary>
        public static Machine ForwardTarget(SimState st, Machine m, int x, int y)
        {
            var n = ProductionRules.MachineAt(st, x, y);
            if (n == null) return null;
            if (IsConveyor(n.Kind) && (int)Dirs.Opposite(n.Dir) == (int)m.Dir) return null;
            if (IsRouting(n.Kind) && !RoutingEntry(st, n, x - Dirs.DX[(int)m.Dir], y - Dirs.DY[(int)m.Dir])) return null;
            return n;
        }

        /// <summary>Reference routing.ts:56 <c>forward</c>.</summary>
        public static bool Forward(SimContext ctx, SimState st, Machine m, int x, int y, ItemId k)
        {
            var n = ForwardTarget(st, m, x, y);
            return n != null && GiveItem(ctx, st, n, k);
        }

        /// <summary>The cached destinations of one conveyor, or null when it has none recorded.</summary>
        public static ConveyorRoute RouteOf(List<ConveyorRoute> routes, int id)
        {
            for (var i = 0; i < routes.Count; i++) if (routes[i].Id == id) return routes[i];
            return null;
        }
    }
}
