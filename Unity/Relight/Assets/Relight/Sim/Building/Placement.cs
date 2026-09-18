using System;
using System.Collections.Generic;

namespace Relight.Sim
{
    /// <summary>Put a machine of `Kind` down with its north-west corner at (X, Y) (reference flow.ts `place`).</summary>
    public sealed record PlaceMachineCommand(string Kind, int X, int Y, Dir Dir = Dir.N) : Command;

    /// <summary>Pack the machine with this id back into the Backpack, contents and all (reference flow.ts `remove`).</summary>
    public sealed record RemoveMachineCommand(int Id) : Command;

    /// <summary>
    /// Turn a placed machine a quarter clockwise (reference flow.ts `rotate` 1439-1444, the R key on a machine).
    /// The ghost's rotation before it is placed is the cursor's, not the sim's: it arrives as
    /// <see cref="PlaceMachineCommand.Dir"/>.
    /// </summary>
    public sealed record RotatePlacementCommand(int Id) : Command;

    /// <summary>
    /// Minimal Phase B placement and removal, ported from reference flow.ts `placementGeometryProblem`,
    /// `addMachine`, `pickUpItems`, `canPickUp` and `remove`.
    /// Only the physical rules the Phase B state can express are ported: map bounds, river and building tiles,
    /// machine-on-machine overlap and the engineer's reach. The reference's block ownership, street/margin rules,
    /// rubble, campaign installations, unlocks and the truck are Phase C.
    /// C-02 completes the payment side: the build cost is charged from the pockets exactly as reference
    /// `buildAffordability` / `place` (1333-1375) do — the Depot is free, a carried machine is spent instead of the
    /// cost, and what is paid is counted in `stats.placed`, which <see cref="Ledger.Flows"/> already treats as a
    /// sink, so conservation holds. Nothing is charged partially: the whole cost is checked before anything moves.
    ///
    /// DELIBERATE DIFFERENCE (removal): the brief asks for a cost refund on removal, but the reference refunds the
    /// MACHINE, not its price — `pickUpItems` returns `{[m.kind]: 1}` plus the contents (flow.ts:1381) — and the
    /// returned machine can be placed again for free, so refunding the price as well would mint resources. The
    /// port keeps the reference's behaviour; <see cref="Remove"/> is unchanged.
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
                    // Reference flow.ts:1293 `if (G.urban?.solid[t]) return 'a city structure is there'`: the authored
                    // building mask (walls of the imported region) is a second mask beside the terrain class.
                    if (g.Solid(tx, ty)) return "a city structure is there";
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
        /// Reference `pickUpItems` (flow.ts:1380): the machine itself as one stack, plus everything it held, in the
        /// reference's order — the machine kind, then the items riding it and the one in an inserter's hand (B-10),
        /// then a turret's rounds as bullets, then the inventory in item order. A tram's cargo and artifacts are not
        /// ported; `Machine.Out` is retired (see <see cref="MachineInventory.Contents"/>).
        /// </summary>
        public static void PickUpItems(GameData d, SimState st, Machine m, List<ItemKey> keys, List<double> counts)
        {
            keys.Clear();
            counts.Clear();
            keys.Add(new ItemKey(m.Kind));
            counts.Add(1);
            void Add(ItemKey k, double n) { if (n > 0) { keys.Add(k); counts.Add(n); } }
            // B-10: `for (const it of m.items) add(it.k, 1); if (m.hold) add(m.hold, 1);` — one entry per item so a
            // belt carrying six plates asks for six, and the flow side table is the only place they live.
            var lane = st.Flow.Find(m.Id);
            if (lane != null)
            {
                for (var i = 0; i < lane.Items.Count; i++) Add(ItemKey.Of(lane.Items[i].Item), 1);
                if (lane.Hold >= 0) Add(ItemKey.Of((ItemId)lane.Hold), 1);
            }
            // W-B's hopper API, so a cannon gives back shells rather than the turret's magazines.
            if (TurretHopper.IsTurret(d, m)) { Add(ItemKey.Of(TurretHopper.Ammo(d, m)), m.Rounds); return; }
            for (var i = 0; i < Items.Count; i++) Add(ItemKey.Of((ItemId)i), Math.Floor(m.Inv[(ItemId)i]));
        }

        /// <summary>Reference `canPickUp`: all of it or none — a full Backpack refuses, with the same message.</summary>
        public static (bool ok, string reason) CanPickUp(SimContext ctx, SimState st, Machine m)
        {
            var d = ctx.Data;
            if (m == null) return (false, "nothing there");
            // Reference flow.ts:1402 `if(isProcessor(m)&&m.busy)return ... 'wait for the processor to finish before
            // packing it'` (W-B's request, wave-2 integration note 2): a processor mid-batch has already consumed
            // its inputs, so packing it would destroy them. The reference's `m.busy` is B-07's `MachineWork.Busy`.
            if (ProductionRules.IsProcessor(d, m) && (st.Production.Find(m.Id)?.Busy ?? false))
                return (false, "wait for the processor to finish before packing it");
            if (string.Equals(m.Kind, "depot", StringComparison.Ordinal)) return (false, "the Depot stays");
            if (!Interaction.InReach(ctx, st, m)) return (false, $"Walk closer to the {(d.TryMachine(m.Kind, out var s) ? s.DisplayName : m.Kind)}");

            var keys = new List<ItemKey>();
            var counts = new List<double>();
            PickUpItems(d, st, m, keys, counts);
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
            PickUpItems(d, st, m, keys, counts);
            for (var i = 0; i < keys.Count; i++) Pockets.Take(d, st.Engineer, keys[i], counts[i]);
            // A turret's loose rounds (rounds that do not make a whole magazine) went to the line buffer in the
            // reference and any remainder was counted in `roundsLost`. One round is one bullet (U-D-08), so there
            // is never a remainder and `roundsLost` stays zero here.
            m.Inv.Clear();
            m.Out = 0;
            m.Rounds = 0;
            // B-10: the belt's items and the inserter's hand went into the pockets above; drop the side-table entry
            // so a machine placed later on the same id cannot inherit them (reference deletes the machine outright).
            st.Flow.Forget(m.Id);
            st.Machines.Remove(m);
            st.Rev++;
            return (true, "");
        }

        /// <summary>Reference `costStr` (flow.ts:1263), built from the spec so a tuning edit reaches the text.</summary>
        public static string CostText(GameData d, IReadOnlyList<ItemStack> cost)
        {
            var s = "";
            for (var i = 0; i < cost.Count; i++)
            {
                if (cost[i].Count <= 0) continue;
                if (s.Length > 0) s += " + ";
                s += cost[i].Count + " " + d.Item(cost[i].Item).DisplayName;
            }
            return s.Length > 0 ? s : "nothing";
        }

        /// <summary>
        /// Reference `buildAffordability` (flow.ts:1333-1340): the pockets alone pay, never a chest and never the
        /// retired Depot stock. The Depot is free, a carried machine is spent instead of its price, and a short
        /// Backpack refuses with the whole price named.
        /// </summary>
        public static (bool ok, string reason, bool carried) Affordability(GameData d, string kind, ItemBag inv)
        {
            if (!d.TryMachine(kind, out var spec)) return (false, "no such machine", false);
            if (string.Equals(kind, "depot", StringComparison.Ordinal)) return (true, "", false);
            if (inv[new ItemKey(kind)] >= 1) return (true, "", true);
            for (var i = 0; i < spec.Cost.Count; i++)
                if (inv[spec.Cost[i].Item] < spec.Cost[i].Count)
                    return (false, $"not enough in the Backpack ({CostText(d, spec.Cost)})", false);
            return (true, "", false);
        }

        /// <summary>
        /// The whole placement question in one pure call, for the ghost: hand lock, geometry, reach and price.
        /// Nothing is mutated, so the cursor may ask it every frame (reference `canPlace`, flow.ts:1326).
        /// </summary>
        public static (bool ok, string reason) Validity(SimContext ctx, SimState st, string kind, int x, int y, Dir dir)
        {
            if (HandCraft.HandLocked(st)) return (false, HandCraft.LockTextFor(st));
            return Buildable(ctx, st, kind, x, y, dir);
        }

        /// <summary>
        /// Everything <see cref="Validity"/> asks except the hand lock: the physical and price rules a placement
        /// must satisfy however it is reached. <see cref="Place"/> uses this, so a dev hook or a test fixture that
        /// calls it directly behaves like reference `place` (which has no hand-lock test at all, flow.ts:1365).
        /// </summary>
        public static (bool ok, string reason) Buildable(SimContext ctx, SimState st, string kind, int x, int y, Dir dir)
        {
            var d = ctx.Data;
            if (!d.TryMachine(kind, out var spec)) return (false, "no such machine");
            var problem = GeometryProblem(ctx, st, kind, x, y, dir);
            if (problem.Length > 0) return (false, problem);
            var (w, h) = Footprints.Dimensions(kind, dir, spec.Size);
            if (!Interaction.InReach(ctx, st, x, y, w, h)) return (false, $"Walk closer to place the {spec.DisplayName}");
            var (ok, reason, _) = Affordability(d, kind, st.Engineer.Inv);
            return ok ? (true, "") : (false, reason);
        }

        /// <summary>
        /// Reference `place` (1367-1375): validate everything first, then spend — a carried machine, or the whole
        /// price out of the pockets, counted in `stats.placed`.
        /// </summary>
        public static (bool ok, string reason) Place(SimContext ctx, SimState st, string kind, int x, int y, Dir dir)
        {
            var d = ctx.Data;
            var (ok, reason) = Buildable(ctx, st, kind, x, y, dir);
            if (!ok) return (false, reason);
            d.TryMachine(kind, out var spec);
            var (_, _, carried) = Affordability(d, kind, st.Engineer.Inv);
            if (carried) Pockets.Drop(d, st.Engineer, new ItemKey(kind), 1);
            else
                for (var i = 0; i < spec.Cost.Count; i++)
                {
                    var c = spec.Cost[i];
                    if (c.Count <= 0) continue;
                    Pockets.Drop(d, st.Engineer, ItemKey.Of(c.Item), c.Count);
                    st.Stats.Placed.Add(c.Item, c.Count);
                }
            Add(ctx, st, kind, x, y, dir);
            return (true, "");
        }

        /// <summary>Reference `rotate` (1439-1443): the kinds that have no facing refuse outright.</summary>
        public static bool Rotatable(string kind)
        {
            switch (kind)
            {
                case "depot":
                case "pole":
                case "bigpole":
                case "substation":
                case "lamp":
                case "arclamp":
                case "chest":
                case "track":
                case "tramstop":
                case "tram":
                    return false;
                default:
                    return true;
            }
        }

        /// <summary>
        /// Reference `rotateTo` (1447-1457): only a non-square footprint has to be re-validated, and reach is
        /// checked against the footprint the machine will have. A square machine just turns.
        /// </summary>
        public static (bool ok, string reason) RotateTo(SimContext ctx, SimState st, Machine m, Dir dir)
        {
            if (m == null) return (false, "nothing there");
            if (!Rotatable(m.Kind)) return (false, "this does not turn");
            // Reference flow.ts:1448: turning an underground endpoint would strand whatever is inside the hidden
            // run, because the run itself moves. B-10's only added placement rule.
            if (FlowRules.IsUnderground(m.Kind) && (st.Flow.Find(m.Id)?.Items.Count ?? 0) > 0)
                return (false, "Pack buffered items before rotating this underground endpoint");
            var (w, h) = Footprints.Dimensions(m.Kind, dir, m.Size);
            if (!Interaction.InReach(ctx, st, m.X, m.Y, w, h)) return (false, "Walk closer to rotate");
            var (ow, oh) = m.Dimensions;
            if (w != ow || h != oh)
            {
                var why = GeometryProblem(ctx, st, m.Kind, m.X, m.Y, dir, m.Id);
                if (why.Length > 0) return (false, why);
            }
            m.Dir = dir;
            st.Rev++;
            return (true, "");
        }

        /// <summary>Reference `rotate`: a quarter clockwise.</summary>
        public static (bool ok, string reason) Rotate(SimContext ctx, SimState st, int id)
        {
            var m = st.MachineById(id);
            if (m == null) return (false, "nothing there");
            return RotateTo(ctx, st, m, (Dir)(((int)m.Dir + 1) % 4));
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
                    if (HandCraft.HandLocked(st)) { result = CommandResult.Refuse(HandCraft.LockTextFor(st)); return true; }
                    var (ok, reason) = Placement.Place(ctx, st, p.Kind, p.X, p.Y, p.Dir);
                    result = ok ? CommandResult.Ok() : CommandResult.Refuse(reason);
                    return true;
                }
                case RotatePlacementCommand t:
                {
                    if (HandCraft.HandLocked(st)) { result = CommandResult.Refuse(HandCraft.LockTextFor(st)); return true; }
                    var (ok, reason) = Placement.Rotate(ctx, st, t.Id);
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
