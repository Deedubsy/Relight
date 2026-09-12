using System;
using System.Collections.Generic;

namespace Relight.Sim
{
    /// <summary>
    /// Where a taken item should land in the Backpack (reference engineer.ts `TransferTarget`).
    /// <c>Slot &lt; 0</c> means "no chosen slot" (the reference's `undefined`), as does a null <c>Layout</c>/<c>Item</c>.
    /// </summary>
    public sealed record TransferTargetSpec(int Slot = -1, string Layout = null, string Item = null);

    /// <summary>
    /// The engineer's pocket/Backpack rules, ported verbatim from reference engineer.ts
    /// (`invStacks`, `invCap`, `pocketSlots`, `pocketUsed`, `packRoom`, `transferTarget`, `allocateTransfer`,
    /// `packProblem`, `take`, `drop`). Static and stateless: everything it touches is passed in (TA §10.5).
    /// Retired here: legacy kits (KIT_STACKS, reserved cells) and the truck inventory path — the truck is not a
    /// Phase B buildable (CONTENT_CATALOGUE §17), so the capacity is always <c>data.Engineer.InvStacks</c>.
    /// </summary>
    public static class Pockets
    {
        /// <summary>Reference `invStacks`: whole stacks the pockets occupy, by stack size.</summary>
        public static int Stacks(GameData d, ItemBag inv)
        {
            var keys = new List<ItemKey>();
            inv.Keys(keys);
            var s = 0;
            for (var i = 0; i < keys.Count; i++)
            {
                var n = inv[keys[i]];
                if (n <= 0) continue;
                s += (int)Math.Ceiling(n / d.StackSize(keys[i]));
            }
            return s;
        }

        /// <summary>Reference `invCap`. The truck path is retired for Phase B, so this is always INV_STACKS (40).</summary>
        public static int Cap(GameData d) => d.Engineer.InvStacks;

        /// <summary>
        /// Reference `pocketSlots`: the Backpack cells the engineer's quantities actually justify.
        /// The player's allocation (`e.Pack`) is honoured first, cell by cell, then whatever is left over is
        /// topped into matching stacks and finally into empty cells, in ordinal key order.
        /// Never invents quantities: the result always sums to `e.Inv`.
        /// </summary>
        public static List<PackStack> Slots(GameData d, Engineer e) => Slots(d, e.Inv, e.Pack);

        public static List<PackStack> Slots(GameData d, ItemBag inv, List<PackStack> pack)
        {
            var cap = Cap(d);
            var slots = new List<PackStack>(cap);
            for (var i = 0; i < cap; i++) slots.Add(null);
            var left = inv.Clone();

            for (var i = 0; i < slots.Count; i++)
            {
                var old = pack != null && i < pack.Count ? pack[i] : null;
                if (slots[i] != null || old == null) continue;
                var key = new ItemKey(old.Item);
                var n = Math.Min(old.Count, Math.Min(Math.Max(0, left[key]), d.StackSize(key)));
                if (n <= 0) continue;
                slots[i] = new PackStack(old.Item, n);
                left[key] = left[key] - n;
            }

            var keys = new List<ItemKey>();
            left.Keys(keys);
            for (var k = 0; k < keys.Count; k++)
            {
                var key = keys[k];
                var stack = d.StackSize(key);
                var n = Math.Max(0, left[key]);
                for (var i = 0; i < slots.Count && n > 0; i++)
                {
                    var cell = slots[i];
                    if (cell == null || !string.Equals(cell.Item, key.Key, StringComparison.Ordinal)) continue;
                    var add = Math.Min(n, stack - cell.Count);
                    cell.Count += add;
                    n -= add;
                }
                for (var i = 0; i < slots.Count && n > 0; i++)
                {
                    if (slots[i] != null) continue;
                    var add = Math.Min(n, stack);
                    slots[i] = new PackStack(key.Key, add);
                    n -= add;
                }
            }
            return slots;
        }

        /// <summary>Reference `pocketUsed`: occupied cells when the player allocated, else the stack count.</summary>
        public static int Used(GameData d, Engineer e)
        {
            if (e.Pack == null) return Stacks(d, e.Inv);
            var slots = Slots(d, e);
            var n = 0;
            for (var i = 0; i < slots.Count; i++) if (slots[i] != null) n++;
            return n;
        }

        /// <summary>Reference `packRoom`: room for `item` across the whole Backpack — slack in its stacks plus empty cells.</summary>
        public static double Room(GameData d, List<PackStack> slots, ItemKey item)
        {
            var weapon = item.IsWeapon;
            double cap = weapon ? 1 : d.StackSize(item);
            double room = 0;
            for (var i = 0; i < slots.Count; i++)
            {
                var c = slots[i];
                if (c == null) room += cap;
                else if (!weapon && string.Equals(c.Item, item.Key, StringComparison.Ordinal)) room += cap - c.Count;
            }
            return room;
        }

        /// <summary>Reference `transferTarget`: how much of a drop the destination will take, before anything moves.</summary>
        public static (double n, string reason) Target(GameData d, Engineer e, ItemKey item, double n, TransferTargetSpec target, bool put)
        {
            if (target == null) return (n, "");
            if (put)
            {
                if (target.Item != null && !string.Equals(target.Item, item.Key, StringComparison.Ordinal))
                    return (0, "Choose a compatible stack or empty storage space.");
                return (n, "");   // the store's own capacity rule decides how much fits
            }
            var slots = Slots(d, e);
            var i = target.Slot;
            if (target.Layout == null || !string.Equals(target.Layout, PackLayout.Json(slots), StringComparison.Ordinal)
                || i < 0 || i >= slots.Count)
                return (0, "Backpack changed; try the drop again.");
            var cell = slots[i];
            if (cell != null && (!string.Equals(cell.Item, item.Key, StringComparison.Ordinal) || item.IsWeapon))
                return (0, "Choose a compatible stack or empty Backpack slot.");
            return (Math.Min(n, Room(d, slots, item)), "Backpack is full.");
        }

        /// <summary>
        /// Reference `allocateTransfer`: place `n` taken items — the dropped-on cell first, then matching stacks
        /// with room, then empty cells (U-D-09: a drop merges repeatedly and only what has no room stays behind).
        /// </summary>
        public static void Allocate(GameData d, Engineer e, ItemKey item, double n, TransferTargetSpec target, List<PackStack> before)
        {
            if (target == null || target.Slot < 0 || n <= 0 || before == null) return;
            double cap = item.IsWeapon ? 1 : d.StackSize(item);
            var left = n;

            void Fill(int i, bool allowEmpty)
            {
                if (i < 0 || i >= before.Count) return;
                var c = before[i];
                if (left <= 0) return;
                if (c != null && !string.Equals(c.Item, item.Key, StringComparison.Ordinal)) return;
                if (c == null && !allowEmpty) return;
                var add = Math.Min(left, cap - (c?.Count ?? 0));
                if (add <= 0) return;
                before[i] = c != null ? new PackStack(c.Item, c.Count + add) : new PackStack(item.Key, add);
                left -= add;
            }

            Fill(target.Slot, true);
            for (var i = 0; i < before.Count; i++) if (i != target.Slot) Fill(i, false);
            for (var i = 0; i < before.Count; i++) if (i != target.Slot) Fill(i, true);
            e.Pack = before;
        }

        /// <summary>Reference `packProblem`: the save/command validator for a supplied allocation.</summary>
        public static string PackProblem(GameData d, Engineer e)
        {
            if (e.Pack == null) return "";
            if (e.Pack.Count > d.Engineer.TruckStacks) return "Invalid backpack allocation";
            for (var i = 0; i < e.Pack.Count; i++)
            {
                var s = e.Pack[i];
                if (s == null) continue;
                if (s.Item == null || double.IsNaN(s.Count) || double.IsInfinity(s.Count)
                    || s.Count < 0 || s.Count > d.StackSize(new ItemKey(s.Item)) || s.Count == 0)
                    return "Invalid backpack allocation";
            }
            return "";
        }

        /// <summary>Reference `take`: put up to `n` of `item` in the pockets, as many as fit. Returns how many went in.</summary>
        public static double Take(GameData d, Engineer e, ItemKey item, double n)
        {
            var slots = Slots(d, e);
            double room = Math.Max(0, Cap(d) - Used(d, e));
            if (double.IsNaN(n) || double.IsInfinity(n) || n <= 0) return 0;
            if (item.IsWeapon && (n != 1 || n != Math.Floor(n))) return 0;
            var stack = d.StackSize(item);
            var have = e.Inv[item];
            double slack = 0;
            for (var i = 0; i < slots.Count; i++)
            {
                var c = slots[i];
                if (c != null && string.Equals(c.Item, item.Key, StringComparison.Ordinal)) slack += stack - c.Count;
            }
            var fit = Math.Min(n, slack + Math.Floor(room * stack));
            if (fit <= 0) return 0;
            e.Inv[item] = have + fit;
            if (e.Pack != null) e.Pack = Slots(d, e);
            return fit;
        }

        /// <summary>Reference `drop`: take up to `n` of `item` out of the pockets. Returns how many came out.</summary>
        public static double Drop(GameData d, Engineer e, ItemKey item, double n)
        {
            if (item.IsWeapon && (n != 1 || n != Math.Floor(n))) return 0;
            var have = e.Inv[item];
            var drop = Math.Min(have, n);
            e.Inv[item] = have - drop;
            if (e.Pack != null) e.Pack = Slots(d, e);
            return drop;
        }

        /// <summary>
        /// A throwaway engineer carrying a copy of the pockets and the allocation, for the reference's
        /// "would it fit?" trials (`machineTransferPreview`, `canPickUp`, `tickHand`). Only the inventory
        /// fields matter: <see cref="Take"/> and <see cref="Drop"/> read nothing else.
        /// </summary>
        public static Engineer Trial(Engineer e)
        {
            var t = new Engineer { Inv = e.Inv.Clone(), Reach = e.Reach };
            if (e.Pack != null)
            {
                t.Pack = new List<PackStack>(e.Pack.Count);
                for (var i = 0; i < e.Pack.Count; i++) t.Pack.Add(e.Pack[i]?.Clone());
            }
            return t;
        }

        public static List<PackStack> ClonePack(List<PackStack> pack)
        {
            if (pack == null) return null;
            var c = new List<PackStack>(pack.Count);
            for (var i = 0; i < pack.Count; i++) c.Add(pack[i]?.Clone());
            return c;
        }
    }

    /// <summary>
    /// The engineer's reach test, ported from reference ground.ts `inReach` (8 tiles to the nearest edge of the
    /// target rect, plus line of sight through authored buildings).
    /// </summary>
    public static class Interaction
    {
        public static bool InReach(SimContext ctx, SimState st, double tx, double ty, double w, double h)
        {
            var e = st.Engineer;
            if (Reach.DistToRect(e.Pos.X, e.Pos.Y, tx, ty, w, h) > ctx.Data.Engineer.ReachTiles) return false;
            var cx = Math.Max(tx - 0.01, Math.Min(tx + w + 0.01, e.Pos.X));
            var cy = Math.Max(ty - 0.01, Math.Min(ty + h + 0.01, e.Pos.Y));
            return ctx.Geometry.Sight(e.Pos.X, e.Pos.Y, cx, cy);
        }

        public static bool InReach(SimContext ctx, SimState st, Machine m)
        {
            var (w, h) = m.Dimensions;
            return InReach(ctx, st, m.X, m.Y, w, h);
        }
    }
}
