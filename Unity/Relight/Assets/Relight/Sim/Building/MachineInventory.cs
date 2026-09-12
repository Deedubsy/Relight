using System;

namespace Relight.Sim
{
    /// <summary>What the UI knew about the source stack when the player started the drag (reference `MachineTransfer.expected`).</summary>
    public sealed record TransferExpectation(double Total, string Layout = null, int Slot = -1);

    /// <summary>
    /// Move `N` of `Item` between the engineer's Backpack and a machine (reference machineInventory.ts
    /// `MachineTransfer`). <c>Put</c> loads the machine; otherwise the machine is emptied into the Backpack.
    /// </summary>
    public sealed record MachineTransferCommand(int Id, ItemId Item, int N, bool Put,
        TransferTargetSpec Target = null, TransferExpectation Expected = null) : Command;

    /// <summary>
    /// Machine inventories and hand transfers, ported from reference machineInventory.ts. Refusal strings are
    /// verbatim (the UI shows them); the item and machine names come from <see cref="GameData"/> rather than the
    /// reference's hard-coded tables.
    /// One round is one bullet (U-D-08), so `machineAmmoUnit` is always 1 and its divisions collapse.
    /// </summary>
    public static class MachineInventory
    {
        /// <summary>Reference flow.ts GENERATOR_COAL_CAP (50), read from the machine spec so the data export owns it.</summary>
        public static double GeneratorFuelCap(GameData d) => d.TryMachine("generator", out var spec) && spec.FuelCap > 0 ? spec.FuelCap : 50;
        /// <summary>Reference recipes.ts TURRET_HOPPER (CONTENT_CATALOGUE §1.1: 50 rounds), read from the machine spec.</summary>
        public static double TurretHopper(GameData d) => d.TryMachine("turret", out var spec) && spec.AmmoCap > 0 ? spec.AmmoCap : 50;

        private const double Eps = 1e-9;

        /// <summary>
        /// Reference `hasMachineInventory`. Taken from the data (`MachineSpec.HasInventory`) instead of the
        /// reference's hard-coded kind list, so B-05's export owns the answer.
        /// Difference: the supply chest is included here. The reference routes chest transfers through the
        /// separate `handPool`/`chestTake`/`chestPut` path, whose other half is the Depot's abstract stock and the
        /// line buffer — both retired (U-D-32). RI-05 already gives a chest the same reach, stack and capacity
        /// predicates, so Phase B serves it through this one path.
        /// </summary>
        public static bool HasInventory(GameData d, Machine m) =>
            m != null && d.TryMachine(m.Kind, out var spec) && spec.HasInventory;


        /// <summary>
        /// Reference `machineInventory`: whole transferable items only. A turret keeps loose rounds until fired,
        /// but with one round per bullet there is never a loose remainder.
        /// </summary>
        public static void Contents(GameData d, Machine m, ItemCounts into)
        {
            into.Clear();
            if (string.Equals(m.Kind, "turret", StringComparison.Ordinal)) { into[ItemId.Magazine] = m.Rounds; return; }
            for (var i = 0; i < Items.Count; i++)
            {
                var n = m.Inv[(ItemId)i];
                if (n > 0) into[(ItemId)i] = n;
            }
            // A processor's finished output is its recipe's output item; recipes reach machines with Phase C, so
            // Phase B counts `out` as magazines (the reference's own default branch).
            if (m.Out > 0) into.Add(ItemId.Magazine, m.Out);
        }

        /// <summary>Reference flow.ts `accepts`, restricted to the kinds Phase B can place.</summary>
        public static bool Accepts(GameData d, Machine m, ItemId k)
        {
            switch (m.Kind)
            {
                case "turret": return k == ItemId.Magazine && m.Rounds + 1 <= TurretHopper(d) + Eps;
                case "generator": return (k == ItemId.Coal || k == ItemId.Fuel)
                    && m.Inv[ItemId.Coal] + m.Inv[ItemId.Fuel] < GeneratorFuelCap(d);
                case "chest": return m.Inv.Total < ChestCap(d);
                default: return false;   // processors accept their recipe's inputs — Phase C
            }
        }

        /// <summary>
        /// Reference flow.ts SUPPLY_CHEST_CAP (200). Read from the machine spec so B-05's export owns it;
        /// <c>MachineSpec.InventorySlots</c> is the chest's item capacity in the Phase B data.
        /// </summary>
        public static double ChestCap(GameData d) => d.TryMachine("chest", out var spec) && spec.InventorySlots > 0 ? spec.InventorySlots : 200;

        private static string Label(GameData d, Machine m) => d.TryMachine(m.Kind, out var spec) ? spec.DisplayName : m.Kind;

        /// <summary>Reference `machineTransferPreview`: how much would move, or why nothing would.</summary>
        public static (double moved, string reason) Preview(SimContext ctx, SimState st, int id, ItemId item, double n, bool put)
        {
            var d = ctx.Data;
            var m = st.MachineById(id);
            if (st.Engineer.IsDown) return (0, "Wait until you recover");
            if (m == null || !HasInventory(d, m)) return (0, "Machine inventory unavailable");
            if (n <= 0 || n != Math.Floor(n) || double.IsInfinity(n)) return (0, "Choose a whole positive quantity");
            if (!Interaction.InReach(ctx, st, m)) return (0, $"Walk closer to the {Label(d, m)}");
            var name = d.Item(item).DisplayName;

            if (put)
            {
                var have = Math.Min(n, Math.Floor(st.Engineer.Inv[item]));
                var copy = new Machine { Id = m.Id, Kind = m.Kind, X = m.X, Y = m.Y, Dir = m.Dir, Size = m.Size,
                    Inv = m.Inv.Clone(), Out = m.Out, Rounds = m.Rounds };
                double moved = 0;
                while (moved < have && Accepts(d, copy, item))
                {
                    if (string.Equals(m.Kind, "turret", StringComparison.Ordinal)) copy.Rounds += 1;
                    else copy.Inv.Add(item, 1);
                    moved++;
                }
                return (moved, moved > 0 ? "" : have <= 0
                    ? $"No {name} in Backpack"
                    : $"Cannot load {name}: wrong input or inventory full");
            }

            var counts = new ItemCounts();
            Contents(d, m, counts);
            var available = Math.Min(n, Math.Floor(counts[item]));
            var taken = Pockets.Take(d, Pockets.Trial(st.Engineer), ItemKey.Of(item), available);
            return (taken, taken > 0 ? "" : available <= 0 ? $"No whole {name} available" : "Backpack is full");
        }

        /// <summary>Reference `machineTransfer`.</summary>
        public static (bool ok, double moved, string reason) Transfer(SimContext ctx, SimState st, MachineTransferCommand a)
        {
            var d = ctx.Data;
            var m = st.MachineById(a.Id);
            var slots = Pockets.Slots(d, st.Engineer);
            if (m == null) return (false, 0, "Machine removed; select another machine");
            var key = ItemKey.Of(a.Item);

            if (a.Expected != null)
            {
                double total;
                if (a.Put) total = st.Engineer.Inv[a.Item];
                else { var counts = new ItemCounts(); Contents(d, m, counts); total = counts[a.Item]; }
                if (a.Put ? total != a.Expected.Total : total < 1)
                    return (false, 0, a.Put ? "Source changed; select the stack again" : "That stack has already left the machine");
                var i = a.Expected.Slot;
                var cell = i >= 0 && i < slots.Count ? slots[i] : null;
                if (a.Put && (!string.Equals(a.Expected.Layout, PackLayout.Json(slots), StringComparison.Ordinal)
                              || cell == null || !string.Equals(cell.Item, key.Key, StringComparison.Ordinal) || cell.Count < a.N))
                    return (false, 0, "Source stack changed; select it again");
            }

            var (allowed, why) = Pockets.Target(d, st.Engineer, key, a.N, a.Target, a.Put);
            if (allowed <= 0) return (false, 0, why);
            var (moved, reason) = Preview(ctx, st, a.Id, a.Item, allowed, a.Put);
            if (moved <= 0) return (false, 0, reason);
            var name = d.Item(a.Item).DisplayName;

            if (a.Put)
            {
                if (string.Equals(m.Kind, "turret", StringComparison.Ordinal))
                {
                    m.Rounds += (int)moved;
                    st.Stats.HandFed += (int)moved;
                    st.Stats.HandFedMags += (int)moved;
                }
                else
                {
                    m.Inv.Add(a.Item, moved);
                    if (string.Equals(m.Kind, "generator", StringComparison.Ordinal))
                    {
                        st.Stats.HandFed += (int)moved;
                        if (a.Item == ItemId.Coal) st.Stats.HandFedCoal += (int)moved;
                    }
                }
                Pockets.Drop(d, st.Engineer, key, moved);
                if (a.Expected != null && a.Expected.Slot >= 0 && a.Expected.Slot < slots.Count && slots[a.Expected.Slot] != null)
                {
                    slots[a.Expected.Slot].Count -= moved;
                    if (slots[a.Expected.Slot].Count <= 0) slots[a.Expected.Slot] = null;
                    st.Engineer.Pack = slots;
                }
            }
            else
            {
                Pockets.Take(d, st.Engineer, key, moved);
                Pockets.Allocate(d, st.Engineer, key, moved, a.Target, slots);
                var left = moved;
                if (string.Equals(m.Kind, "turret", StringComparison.Ordinal)) { m.Rounds -= (int)left; left = 0; }
                if (left > 0 && m.Out > 0 && a.Item == ItemId.Magazine)
                {
                    var take = Math.Min(left, m.Out);
                    m.Out -= (int)take;
                    left -= take;
                }
                if (left > 0) m.Inv.Add(a.Item, -left);
            }
            return (true, moved, $"{PackLayout.Num(moved)} {name} {(a.Put ? "loaded" : "taken")}");
        }
    }

    public sealed class MachineTransferHandler : ICommandHandler
    {
        public bool TryApply(SimContext ctx, SimState st, Command c, out CommandResult result)
        {
            if (!(c is MachineTransferCommand t)) { result = default; return false; }
            var (ok, _, reason) = MachineInventory.Transfer(ctx, st, t);
            result = ok ? CommandResult.Ok(reason) : CommandResult.Refuse(reason);
            return true;
        }
    }
}
