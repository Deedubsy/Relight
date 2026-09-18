using System;
using System.Collections.Generic;

namespace Relight.Sim
{
    /// <summary>
    /// B-10's tick: belts move their items, routing devices serve theirs, machines next to a conveyor load it
    /// directly, and inserters swing. Ported from reference flow.ts <c>tickBelt</c>/<c>tickInserter</c>,
    /// routing.ts <c>tickRouting</c> and directConveyor.ts <c>loadConveyor</c>, in the order reference
    /// <c>stepFlow</c> (flow.ts:1075-1077, 1092) runs them:
    ///
    /// <code>
    ///   for (const m of beltOrder(st, f)) { tickBelt(st, m, dt); loadConveyor(st, m, destinations); }
    ///   for (const m of f.machines) if (isRouting(m)) { tickRouting(st, m, dt); loadConveyor(st, m, destinations); }
    ///   ... for (const m of f.machines) ... if (m.kind === 'inserter') tickInserter(st, m, dt * machineThrottle(st, m));
    /// </code>
    ///
    /// Difference from the reference: the reference runs all of this <em>before</em> its machine loop, while the
    /// Unity composition fixes Power → Machines → Flow (SimComposition.Production.cs, not this worker's file). The
    /// only observable effect is that an item a processor finishes this tick can be taken by a belt in the same
    /// tick instead of the next one; rates, ordering within the flow layer and conservation are unchanged.
    /// The reference's <c>running(st, m)</c> gate (a machine on a block that is not Held stands still) is retired
    /// with the block economy (U-D-32) and is always true here.
    /// </summary>
    public sealed class FlowPhase : ITickPhase
    {
        public void Tick(SimContext ctx, SimState st, double dt)
        {
            var flow = st.Flow;
            flow.Prune(st);
            if (st.Machines.Count == 0) return;

            // Snapshot before any belt advances, preserving identity across handoffs.
            foreach (var machine in st.Machines)
            {
                var lane = st.Flow.Find(machine.Id);
                if (lane == null) continue;
                foreach (var item in lane.Items)
                {
                    item.PreviousPosition = FlowQueries.Position(st, machine, item.P);
                    item.HasPreviousPosition = true;
                }
            }
            var routes = FlowRules.Destinations(st);

            var order = FlowRules.BeltOrder(st);
            for (var i = 0; i < order.Count; i++)
            {
                TickBelt(ctx, st, order[i], dt);
                LoadConveyor(ctx, st, order[i], routes);
            }

            for (var i = 0; i < st.Machines.Count; i++)
            {
                var m = st.Machines[i];
                if (!FlowRules.IsRouting(m.Kind)) continue;
                // GP-W5: splitters and undergrounds draw no power, so the throttle gate below never covers them.
                // They are buildable, so CombatBalance gives them integrity, so they can be broken — and a broken
                // splitter has to stop routing or the lane behind a wrecked junction keeps flowing as if nothing
                // happened.
                if (TurretRules.Wrecked(ctx.Data, st, m)) continue;
                TickRouting(ctx, st, m, dt);
                LoadConveyor(ctx, st, m, routes);
            }

            for (var i = 0; i < st.Machines.Count; i++)
            {
                var m = st.Machines[i];
                if (!FlowRules.IsInserter(m.Kind)) continue;
                // Reference flow.ts:1093 `|| !running(st, m)) continue;` — flow.ts:658 `running` refuses an
                // unpowered machine, so a dead circuit skips the arm entirely rather than ticking it with dt = 0
                // (tickInserter's phase-0 branch has no timer in it and would still grab). Same gate as W-B's
                // MachinePhase:32.
                var throttle = PowerQueries.Throttle(ctx, st, m.Id);
                if (throttle <= 0) continue;
                TickInserter(ctx, st, m, dt * throttle);
            }
        }

        // ---- belts -------------------------------------------------------------------------------------------

        /// <summary>
        /// Reference flow.ts:850 <c>tickBelt</c>. Back to front so the leader frees its slot first; the leader
        /// leaves at p ≥ 1 and otherwise stops half a spacing short of the edge; every follower stops one spacing
        /// behind the item ahead. <c>Math.max(it.p, np)</c> means an item never slides backwards.
        /// The reference's <c>beltDeliver</c> branch (RI-05: a belt facing a Dark block's substation commits steel
        /// or copper to the claim) is retired with the block economy (U-D-32), so a blocked leader simply waits.
        /// </summary>
        public static void TickBelt(SimContext ctx, SimState st, Machine m, double dt)
        {
            var lane = st.Flow.Find(m.Id);
            if (lane == null || lane.Items.Count == 0) return;
            var adv = FlowRules.Speed(ctx.Data, m) * dt;
            for (var i = lane.Items.Count - 1; i >= 0; i--)
            {
                var it = lane.Items[i];
                var np = it.P + adv;
                if (i == lane.Items.Count - 1)
                {
                    var next = FlowRules.NextOf(st, m);
                    var nextLane = next != null && FlowRules.IsBelt(next.Kind) ? st.Flow.Find(next.Id) : null;
                    // The gap also spans tile boundaries, including a backed-up line.
                    if (nextLane != null && nextLane.Items.Count > 0)
                        np = Math.Min(np, 1 + nextLane.Items[0].P - FlowRules.BeltSpacing);
                    if (np >= 1)
                    {
                        var n = FlowRules.NextOf(st, m);
                        if (n != null && FlowRules.GiveItem(ctx, st, n, it.Item, np - 1, it))
                        {
                            lane.Items.RemoveAt(lane.Items.Count - 1);
                            continue;
                        }
                        np = 1 - FlowRules.BeltSpacing / 2;
                    }
                }
                else np = Math.Min(np, lane.Items[i + 1].P - FlowRules.BeltSpacing);
                it.P = Math.Max(it.P, np);
            }
        }

        // ---- routing -----------------------------------------------------------------------------------------

        /// <summary>Reference routing.ts:69 <c>tickRouting</c>: a splitter's timed alternation and an underground's two halves.</summary>
        public static void TickRouting(SimContext ctx, SimState st, Machine m, double dt)
        {
            var lane = st.Flow.Find(m.Id);
            if (FlowRules.IsSplitter(m.Kind))
            {
                if (lane == null) return;
                lane.Timer = Math.Min(2 / FlowRules.SplitterPerS, lane.Timer + dt);
                if (lane.Timer + FlowRules.Eps < 1 / FlowRules.SplitterPerS || lane.Items.Count == 0) return;
                FlowRules.SplitterPorts(m, true, out var ax, out var ay, out var bx, out var by);
                var first = lane.Priority == 1 ? 0 : lane.Priority == 2 ? 1 : lane.RoutingNext;
                for (var t = 0; t < 2; t++)
                {
                    var side = t == 0 ? first : 1 - first;
                    var px = side == 0 ? ax : bx;
                    var py = side == 0 ? ay : by;
                    if (!FlowRules.Forward(ctx, st, m, px, py, lane.Items[0].Item)) continue;
                    lane.Items.RemoveAt(0);
                    lane.RoutingNext = 1 - side;
                    lane.Timer -= 1 / FlowRules.SplitterPerS;
                    return;
                }
                return;
            }

            if (!FlowRules.IsUnderground(m.Kind) || lane == null || lane.Items.Count == 0) return;
            var speed = FlowRules.Speed(ctx.Data, m) * dt;

            if (FlowRules.IsUndergroundOutput(st, m))
            {
                for (var i = lane.Items.Count - 1; i >= 0; i--)
                {
                    var it = lane.Items[i];
                    var np = it.P + speed;
                    if (i == lane.Items.Count - 1 && np >= 1)
                    {
                        if (FlowRules.Forward(ctx, st, m, m.X + Dirs.DX[(int)m.Dir], m.Y + Dirs.DY[(int)m.Dir], it.Item))
                        {
                            lane.Items.RemoveAt(lane.Items.Count - 1);
                            continue;
                        }
                        np = 1 - FlowRules.BeltSpacing / 2;
                    }
                    else if (i != lane.Items.Count - 1) np = Math.Min(np, lane.Items[i + 1].P - FlowRules.BeltSpacing);
                    it.P = Math.Max(it.P, np);
                }
                return;
            }

            var mate = FlowRules.UndergroundMate(st, m);
            if (mate == null) return;
            var length = FlowRules.UndergroundLength(m, mate);
            for (var i = lane.Items.Count - 1; i >= 0; i--)
            {
                var it = lane.Items[i];
                var np = it.P + speed;
                if (i == lane.Items.Count - 1 && np >= length)
                {
                    var exit = st.Flow.Find(mate.Id);
                    if (FlowRules.BeltRoom(exit, 0))
                    {
                        FlowRules.BeltInsert(ctx.Data, mate, st.Flow.Of(mate.Id), it.Item, 0);
                        lane.Items.RemoveAt(lane.Items.Count - 1);
                        continue;
                    }
                    np = length - FlowRules.BeltSpacing / 2;
                }
                else if (i != lane.Items.Count - 1) np = Math.Min(np, lane.Items[i + 1].P - FlowRules.BeltSpacing);
                it.P = Math.Max(it.P, np);
            }
        }

        // ---- direct conveyor loading -------------------------------------------------------------------------

        /// <summary>
        /// Reference directConveyor.ts:52 <c>loadConveyor</c>: a conveyor pulls one whole item per tick from the
        /// machine behind it (or, for a splitter, from either rear port), round-robin over the item list so a mixed
        /// line does not starve one ingredient, and only after reserving room at an actual destination.
        /// </summary>
        public static void LoadConveyor(SimContext ctx, SimState st, Machine m, List<ConveyorRoute> routes)
        {
            if (FlowRules.IsUndergroundOutput(st, m)) return;
            int ax, ay, bx, by, points;
            if (FlowRules.IsSplitter(m.Kind)) { FlowRules.SplitterPorts(m, false, out ax, out ay, out bx, out by); points = 2; }
            else { ax = m.X - Dirs.DX[(int)m.Dir]; ay = m.Y - Dirs.DY[(int)m.Dir]; bx = 0; by = 0; points = 1; }

            for (var pi = 0; pi < points; pi++)
            {
                var px = pi == 0 ? ax : bx;
                var py = pi == 0 ? ay : by;
                var src = ProductionRules.MachineAt(st, px, py);
                if (src == null || FlowRules.IsConveyor(src.Kind) || FlowRules.IsInserter(src.Kind)) continue;
                var lane = st.Flow.Of(m.Id);
                var start = lane.PickupNext;
                for (var i = 0; i < Items.Count; i++)
                {
                    var index = (start + i) % Items.Count;
                    var k = (ItemId)index;
                    if (SourceCount(ctx, st, src, k) < 1) continue;
                    if (!FlowRules.Accepts(ctx, st, m, k)) continue;
                    if (!RoomAtEnd(ctx, st, m, k, routes)) continue;
                    if (!FlowRules.GiveItem(ctx, st, m, k, 0)) continue;
                    TakeSource(ctx, st, src, k);
                    lane.PickupNext = (index + 1) % Items.Count;
                    break;
                }
            }
        }

        /// <summary>
        /// Reference directConveyor.ts:23 <c>roomAtEnd</c>: reserve room for everything already travelling to the
        /// same endpoint, so a mixed line cannot overfill one ingredient. A conveyor whose line ends nowhere yet
        /// may still buffer.
        /// </summary>
        public static bool RoomAtEnd(SimContext ctx, SimState st, Machine belt, ItemId item, List<ConveyorRoute> routes)
        {
            var route = FlowRules.RouteOf(routes, belt.Id);
            if (route == null || route.Ends.Count == 0) return true;
            for (var e = 0; e < route.Ends.Count; e++)
            {
                var end = route.Ends[e];
                var copy = new Machine
                {
                    Id = end.Id, Kind = end.Kind, X = end.X, Y = end.Y, Dir = end.Dir, Size = end.Size,
                    Inv = end.Inv.Clone(), Out = end.Out, Rounds = end.Rounds
                };
                var magazines = 0;
                var ammo = TurretHopper.Ammo(ctx.Data, end);
                for (var i = 0; i < st.Machines.Count; i++)
                {
                    var other = st.Machines[i];
                    if (!FlowRules.IsConveyor(other.Kind)) continue;
                    var r = FlowRules.RouteOf(routes, other.Id);
                    if (r == null || !Reaches(r, end.Id)) continue;
                    var lane = st.Flow.Find(other.Id);
                    if (lane == null) continue;
                    for (var c = 0; c < lane.Items.Count; c++)
                    {
                        copy.Inv.Add(lane.Items[c].Item, 1);
                        if (lane.Items[c].Item == ammo) magazines++;
                    }
                }
                // One round is one bullet (U-D-08), so a magazine in transit is one round in the hopper.
                if (TurretHopper.IsTurret(ctx.Data, end)) copy.Rounds = end.Rounds + magazines;
                if (MachineInventory.Accepts(ctx.Data, st, copy, item)) return true;
            }
            return false;
        }

        private static bool Reaches(ConveyorRoute r, int id)
        {
            for (var i = 0; i < r.Ends.Count; i++) if (r.Ends[i].Id == id) return true;
            return false;
        }

        /// <summary>
        /// Reference directConveyor.ts:33 <c>sourceCount</c>: how many whole <paramref name="k"/> this machine has
        /// spare. The Depot and tram-stop branches are retired (U-D-32, RI-05). A miner exposes its
        /// one extracted item through the same inventory used by transfers and saves.
        /// </summary>
        public static double SourceCount(SimContext ctx, SimState st, Machine m, ItemId k)
        {
            var d = ctx.Data;
            if (TurretHopper.IsTurret(d, m)) return k == TurretHopper.Ammo(d, m) ? m.Rounds : 0;
            if (ProductionRules.IsMiner(d, m)) return Math.Floor(m.Inv[k]);
            if (ProductionRules.IsProcessor(d, m))
                return k == ProductionRules.OutputItem(ProductionRules.RecipeOf(d, st, m)) ? Math.Floor(m.Inv[k]) : 0;
            if (string.Equals(m.Kind, "chest", StringComparison.Ordinal)
                || string.Equals(m.Kind, "generator", StringComparison.Ordinal)
                || string.Equals(m.Kind, "cannon", StringComparison.Ordinal)) return Math.Floor(m.Inv[k]);
            return 0;
        }

        /// <summary>Reference directConveyor.ts:45 <c>takeSource</c>, the matching removal.</summary>
        public static void TakeSource(SimContext ctx, SimState st, Machine m, ItemId k)
        {
            if (TurretHopper.IsTurret(ctx.Data, m)) { m.Rounds -= 1; return; }
            m.Inv.Add(k, -1);
            if (m.Inv[k] < FlowRules.Eps) m.Inv[k] = 0;
        }

        // ---- inserters ---------------------------------------------------------------------------------------

        /// <summary>
        /// Reference flow.ts:887 <c>inserterPickup</c>: the exact next pick-up, without moving anything, so the
        /// status panel and the tick agree. A belt is read leader-first (the item about to leave), a splitter
        /// oldest-first.
        /// </summary>
        public static bool Pickup(SimContext ctx, SimState st, Machine m, out ItemId item, out int index)
        {
            item = ItemId.Steel;
            index = -1;
            var (sx, sy) = ProductionRules.InputTile(m);
            var (dx, dy) = ProductionRules.OutputTile(m);
            var src = ProductionRules.MachineAt(st, sx, sy);
            var dst = ProductionRules.MachineAt(st, dx, dy);
            if (src == null || dst == null) return false;
            var lane = st.Flow.Find(m.Id);
            var filter = lane != null ? lane.Filter : -1;

            if (FlowRules.IsConveyor(src.Kind)
                && (!FlowRules.IsUnderground(src.Kind) || FlowRules.IsUndergroundOutput(st, src)))
            {
                var s = st.Flow.Find(src.Id);
                if (s == null) return false;
                for (var n = 0; n < s.Items.Count; n++)
                {
                    var i = FlowRules.IsSplitter(src.Kind) ? n : s.Items.Count - 1 - n;
                    var k = s.Items[i].Item;
                    if (!Allows(ctx, st, dst, filter, k)) continue;
                    item = k;
                    index = i;
                    return true;
                }
                return false;
            }

            if (ProductionRules.IsProcessor(ctx.Data, src))
            {
                var k = ProductionRules.OutputItem(ProductionRules.RecipeOf(ctx.Data, st, src));
                if (src.Inv[k] < 1 - FlowRules.Eps || !Allows(ctx, st, dst, filter, k)) return false;
                item = k;
                return true;
            }

            if (string.Equals(src.Kind, "chest", StringComparison.Ordinal))
            {
                for (var i = 0; i < Items.Count; i++)
                {
                    var k = (ItemId)i;
                    if (src.Inv[k] < 1 - FlowRules.Eps || !Allows(ctx, st, dst, filter, k)) continue;
                    if (!FlowRules.Accepts(ctx, st, dst, k)) continue;
                    item = k;
                    return true;
                }
            }
            return false;
        }

        private static bool Allows(SimContext ctx, SimState st, Machine dst, int filter, ItemId k) =>
            (filter < 0 || filter == (int)k) && FlowRules.Wants(ctx, st, dst, k);

        /// <summary>
        /// Reference flow.ts:901 <c>tickInserter</c>. Phase 1 is the swing out: at the end of it the item is handed
        /// over at p = 0.5 and the return swing is charged; a blocked destination holds the arm there (timer 0) and
        /// retries every tick. Phase 2 ends the return swing and falls straight through to a pick-up in the same
        /// tick, the leftover time carried by <c>INSERTER_SWING + Math.min(0, m.timer)</c>.
        /// </summary>
        public static void TickInserter(SimContext ctx, SimState st, Machine m, double dt)
        {
            var swing = FlowRules.InserterSwing(ctx.Data);
            var lane = st.Flow.Of(m.Id);
            if (lane.Phase != 0)
            {
                lane.Timer -= dt;
                if (lane.Timer > FlowRules.Eps) return;
                if (lane.Phase == 1)
                {
                    var (dx, dy) = ProductionRules.OutputTile(m);
                    var dst = ProductionRules.MachineAt(st, dx, dy);
                    if (dst != null && lane.Hold >= 0 && FlowRules.GiveItem(ctx, st, dst, (ItemId)lane.Hold, 0.5))
                    {
                        lane.Hold = -1;
                        lane.Phase = 2;
                        lane.Timer += swing;
                    }
                    else lane.Timer = 0;
                    return;
                }
                lane.Phase = 0;
            }

            if (!Pickup(ctx, st, m, out var item, out var index)) return;
            var (sx, sy) = ProductionRules.InputTile(m);
            var src = ProductionRules.MachineAt(st, sx, sy);
            if (src == null) return;
            if (index >= 0)
            {
                var s = st.Flow.Find(src.Id);
                if (s == null || index >= s.Items.Count) return;
                s.Items.RemoveAt(index);
            }
            else if (ProductionRules.IsProcessor(ctx.Data, src)) src.Inv.Add(item, -1);
            else src.Inv.Add(item, -1);
            if (src.Inv[item] < FlowRules.Eps) src.Inv[item] = 0;
            lane.Hold = (int)item;
            lane.Phase = 1;
            lane.Timer = swing + Math.Min(0, lane.Timer);
        }
    }
}
