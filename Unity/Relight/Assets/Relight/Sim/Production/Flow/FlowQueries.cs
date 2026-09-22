using System;
using System.Collections.Generic;

namespace Relight.Sim
{
    /// <summary>One item on a conveyor, for presentation (C-06 / <c>BeltItemPresenter</c>).</summary>
    public readonly struct BeltItemView
    {
        /// <summary>The conveyor carrying it.</summary>
        public int MachineId { get; }
        public ItemId Item { get; }
        /// <summary>Distance travelled along the device, 0 at the entry edge and 1 at the exit edge of a belt.</summary>
        public double P { get; }
        /// <summary>Tile-space position of the item, Y-down, at the centre of its lane.</summary>
        public Vec2 Pos { get; }
        /// <summary>The direction items are travelling as they enter this device (reference <c>entryDir</c>), for corner art.</summary>
        public Dir Entry { get; }
        public Vec2 PreviousPos { get; }
        public BeltItemView(int machineId, ItemId item, double p, Vec2 pos, Dir entry, Vec2? previous = null)
        { MachineId = machineId; Item = item; P = p; Pos = pos; Entry = entry; PreviousPos = previous ?? pos; }
    }

    /// <summary>How a conveyor or inserter is doing, for the machine status panel (reference routing.ts <c>routingStatus</c>).</summary>
    public enum FlowStatus { Idle = 0, Running, Blocked, Starved }

    /// <summary>
    /// Read-only views over <see cref="FlowState"/>. Nothing here mutates; the transient topology caches are
    /// rebuilt by <see cref="FlowRules"/> on <see cref="SimState.Rev"/> and are safe to touch from a query.
    /// </summary>
    public static class FlowQueries
    {
        /// <summary>True when this machine is a belt, fast belt, underground endpoint or splitter.</summary>
        public static bool IsConveyor(Machine m) => m != null && FlowRules.IsConveyor(m.Kind);

        /// <summary>How many items this device is carrying right now.</summary>
        public static int Count(SimState st, int id) => st.Flow.Find(id)?.Items.Count ?? 0;

        /// <summary>
        /// Every item on one conveyor, tail first, with a tile-space position so the presenter can draw it without
        /// knowing the flow rules. A splitter buffers its items at the centre of its two tiles.
        /// </summary>
        public static void ItemsOn(SimState st, Machine m, List<BeltItemView> into)
        {
            into.Clear();
            if (m == null || !FlowRules.IsConveyor(m.Kind)) return;
            var lane = st.Flow.Find(m.Id);
            if (lane == null) return;
            var entry = FlowRules.IsBelt(m.Kind) ? FlowRules.EntryDir(st, m) : m.Dir;
            var (w, h) = m.Dimensions;
            var cx = m.X + w / 2.0;
            var cy = m.Y + h / 2.0;
            var dx = Dirs.DX[(int)m.Dir];
            var dy = Dirs.DY[(int)m.Dir];
            for (var i = 0; i < lane.Items.Count; i++)
            {
                var it = lane.Items[i];
                if (FlowRules.IsUnderground(m.Kind) && !FlowRules.IsUndergroundOutput(st, m) && it.P > 1) continue;
                var pos = Position(st, m, it.P);
                into.Add(new BeltItemView(m.Id, it.Item, it.P, pos, entry, it.HasPreviousPosition ? it.PreviousPosition : pos));
            }
        }

        /// <summary>Lane centreline, including a corner's entry and exit halves.</summary>
        public static Vec2 Position(SimState st, Machine m, double p)
        {
            var (w, h) = m.Dimensions;
            var cx = m.X + w / 2.0; var cy = m.Y + h / 2.0;
            if (FlowRules.IsSplitter(m.Kind)) return new Vec2(cx, cy);
            var dir = FlowRules.IsBelt(m.Kind) && p < .5 ? FlowRules.EntryDir(st, m) : m.Dir;
            return new Vec2(cx + Dirs.DX[(int)dir] * (p - .5), cy + Dirs.DY[(int)dir] * (p - .5));
        }

        /// <summary>Every item on every conveyor, in machine order then tail-first, for a whole-screen presenter pass.</summary>
        public static void AllItems(SimState st, List<BeltItemView> into)
        {
            into.Clear();
            var one = new List<BeltItemView>();
            for (var i = 0; i < st.Machines.Count; i++)
            {
                var m = st.Machines[i];
                if (!FlowRules.IsConveyor(m.Kind)) continue;
                ItemsOn(st, m, one);
                for (var j = 0; j < one.Count; j++) into.Add(one[j]);
            }
        }

        /// <summary>The item an inserter is holding, or null while its hand is empty.</summary>
        public static ItemId? Holding(SimState st, int id)
        {
            var lane = st.Flow.Find(id);
            return lane == null || lane.Hold < 0 ? (ItemId?)null : (ItemId)lane.Hold;
        }

        /// <summary>An inserter's filter, or null for "any item".</summary>
        public static ItemId? Filter(SimState st, int id)
        {
            var lane = st.Flow.Find(id);
            return lane == null || lane.Filter < 0 ? (ItemId?)null : (ItemId)lane.Filter;
        }

        /// <summary>A splitter's output priority: 0 balanced, 1 left, 2 right (reference <c>m.priority</c>).</summary>
        public static int Priority(SimState st, int id) => st.Flow.Find(id)?.Priority ?? 0;

        /// <summary>True when this underground endpoint is the exit half of a pair.</summary>
        public static bool IsUndergroundExit(SimState st, Machine m) => FlowRules.IsUndergroundOutput(st, m);

        /// <summary>The paired underground endpoint's id, or -1 while the endpoint is unpaired.</summary>
        public static int MateId(SimState st, Machine m) => FlowRules.UndergroundMate(st, m)?.Id ?? -1;

        /// <summary>
        /// How far through its swing an inserter is, 0 at rest and 1 at full extension: it rises over the outward
        /// swing (phase 1) and falls over the return (phase 2), so the presenter can lerp the arm.
        /// </summary>
        public static double SwingFraction(GameData d, SimState st, int id)
        {
            var lane = st.Flow.Find(id);
            if (lane == null || lane.Phase == 0) return 0;
            var swing = FlowRules.InserterSwing(d);
            if (swing <= 0) return 0;
            var done = Math.Min(1, Math.Max(0, 1 - lane.Timer / swing));
            return lane.Phase == 1 ? done : 1 - done;
        }

        /// <summary>
        /// Reference routing.ts:58 <c>routingStatus</c>, widened to belts and inserters so one status panel covers
        /// the whole flow layer.
        /// </summary>
        public static FlowStatus Status(SimContext ctx, SimState st, Machine m)
        {
            if (m == null) return FlowStatus.Idle;
            var lane = st.Flow.Find(m.Id);

            if (FlowRules.IsInserter(m.Kind))
            {
                if (lane != null && lane.Phase != 0) return lane.Timer <= FlowRules.Eps ? FlowStatus.Blocked : FlowStatus.Running;
                return Pickupable(ctx, st, m) ? FlowStatus.Running : FlowStatus.Starved;
            }

            if (!FlowRules.IsConveyor(m.Kind)) return FlowStatus.Idle;

            var mate = FlowRules.IsUnderground(m.Kind) ? FlowRules.UndergroundMate(st, m) : null;
            if (FlowRules.IsUnderground(m.Kind) && !FlowRules.IsUndergroundOutput(st, m) && mate == null) return FlowStatus.Starved;
            if (lane == null || lane.Items.Count == 0) return FlowStatus.Idle;
            var leading = FlowRules.IsSplitter(m.Kind) ? lane.Items[0] : lane.Items[lane.Items.Count - 1];

            if (FlowRules.IsUnderground(m.Kind) && !FlowRules.IsUndergroundOutput(st, m))
            {
                var length = FlowRules.UndergroundLength(m, mate);
                return leading.P >= length - FlowRules.BeltSpacing / 2 && !FlowRules.BeltRoom(st.Flow.Find(mate.Id), 0)
                    ? FlowStatus.Blocked : FlowStatus.Running;
            }

            var blocked = true;
            if (FlowRules.IsSplitter(m.Kind))
            {
                FlowRules.SplitterPorts(m, true, out var ax, out var ay, out var bx, out var by);
                blocked = !PortOpen(ctx, st, m, ax, ay, leading.Item) && !PortOpen(ctx, st, m, bx, by, leading.Item);
            }
            else blocked = !PortOpen(ctx, st, m, m.X + Dirs.DX[(int)m.Dir], m.Y + Dirs.DY[(int)m.Dir], leading.Item);

            return blocked && (FlowRules.IsSplitter(m.Kind) || leading.P >= 1 - FlowRules.BeltSpacing / 2)
                ? FlowStatus.Blocked : FlowStatus.Running;
        }

        private static bool PortOpen(SimContext ctx, SimState st, Machine m, int x, int y, ItemId k)
        {
            var n = FlowRules.ForwardTarget(st, m, x, y);
            return n != null && FlowRules.Accepts(ctx, st, n, k);
        }

        private static bool Pickupable(SimContext ctx, SimState st, Machine m) =>
            FlowPhase.Pickup(ctx, st, m, out _, out _);

        /// <summary>
        /// Reference routing.ts:118 <c>routingDescription</c> — the one-line machine panel text. The item names come
        /// from <see cref="GameData"/> rather than the reference's raw keys.
        /// </summary>
        public static string Description(SimContext ctx, SimState st, Machine m)
        {
            var d = ctx.Data;
            var lane = st.Flow.Find(m.Id);
            if (FlowRules.IsInserter(m.Kind))
            {
                var f = lane == null || lane.Filter < 0 ? "any item" : d.Item((ItemId)lane.Filter).DisplayName;
                var h = lane == null || lane.Hold < 0 ? "none" : d.Item((ItemId)lane.Hold).DisplayName;
                return $"Filter: {f} · held: {h}";
            }
            if (FlowRules.IsSplitter(m.Kind))
            {
                var p = lane == null ? 0 : lane.Priority;
                var name = p == 1 ? "left" : p == 2 ? "right" : "balanced";
                return $"Splitter → {Compass(m.Dir)} · priority: {name} · {(lane?.Items.Count ?? 0)}/{FlowRules.SplitterCapacity} buffered · blocked priority falls back";
            }
            if (FlowRules.IsUnderground(m.Kind))
            {
                var mate = FlowRules.UndergroundMate(st, m);
                var role = FlowRules.IsUndergroundOutput(st, m) ? "output" : "input";
                var pair = mate != null
                    ? $"{FlowRules.UndergroundLength(m, mate) - 1} hidden tiles; paired"
                    : "unpaired: stock waits";
                return $"Underground {role} · {pair} · {(lane?.Items.Count ?? 0)} buffered";
            }
            return $"{(string.Equals(m.Kind, "fastbelt", StringComparison.Ordinal) ? "Fast conveyor" : "Conveyor")} → {Compass(m.Dir)} · {(lane?.Items.Count ?? 0)} carried";
        }

        private static string Compass(Dir d) => d == Dir.N ? "north" : d == Dir.E ? "east" : d == Dir.S ? "south" : "west";

        /// <summary>
        /// Every item the flow layer is holding — on belts, in undergrounds and splitters, and in inserters' hands —
        /// added (not assigned) into <paramref name="into"/>. This is <c>LedgerPlace.Belts</c>: see the report for
        /// the two-line <c>Ledger.Held</c> patch that fills it (Ledger.cs is not this worker's file).
        /// </summary>
        public static void HeldItems(SimState st, ItemCounts into)
        {
            var lanes = st.Flow.Lanes;
            for (var i = 0; i < lanes.Count; i++)
            {
                var lane = lanes[i];
                for (var j = 0; j < lane.Items.Count; j++) into.Add(lane.Items[j].Item, 1);
                if (lane.Hold >= 0) into.Add((ItemId)lane.Hold, 1);
            }
        }

        /// <summary>The total number of items the flow layer is holding (tests and the ledger tolerance check).</summary>
        public static double HeldCount(SimState st)
        {
            double n = 0;
            var lanes = st.Flow.Lanes;
            for (var i = 0; i < lanes.Count; i++)
            {
                n += lanes[i].Items.Count;
                if (lanes[i].Hold >= 0) n++;
            }
            return n;
        }
    }
}
