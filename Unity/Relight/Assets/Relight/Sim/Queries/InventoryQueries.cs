using System.Collections.Generic;

namespace Relight.Sim
{
    /// <summary>One Backpack cell for the inventory panel. <c>Item</c> is null for an empty cell.</summary>
    public sealed record PocketSlotView(int Index, string Item, string DisplayName, double Count, int StackSize);

    /// <summary>One item line in a machine's inventory panel.</summary>
    public sealed record ItemAmountView(ItemId Item, string Key, string DisplayName, double Count);

    /// <summary>
    /// A machine's transferable contents (reference machineInventory.ts `machineInventory`), addressed by id:
    /// presentation holds the id, never the <see cref="Machine"/> (TA §4.5).
    /// </summary>
    public sealed record MachineInventoryView(int MachineId, string Kind, string DisplayName, bool HasInventory,
        bool InReach, IReadOnlyList<ItemAmountView> Items);

    /// <summary>The moved/refused answer a transfer button shows before the player commits (reference `machineTransferPreview`).</summary>
    public sealed record TransferPreviewView(double Moved, string Reason);

    /// <summary>Read-only inventory selectors (MIGRATION_MAP S-08). Never mutates state.</summary>
    public static class InventoryQueries
    {
        /// <summary>
        /// Every Backpack cell in slot order, all <c>InvStacks</c> of them (UI_AND_ONBOARDING §5.3: the panel
        /// renders true slots, reconciled from `engineer.pack` against `engineer.inv`).
        /// </summary>
        public static IReadOnlyList<PocketSlotView> Slots(SimContext ctx, SimState st)
        {
            var d = ctx.Data;
            var cells = Pockets.Slots(d, st.Engineer);
            var views = new List<PocketSlotView>(cells.Count);
            for (var i = 0; i < cells.Count; i++)
            {
                var c = cells[i];
                if (c == null) { views.Add(new PocketSlotView(i, null, null, 0, 0)); continue; }
                var key = new ItemKey(c.Item);
                var name = key.IsItem(out var id) ? d.Item(id).DisplayName
                    : d.TryMachine(c.Item, out var spec) ? spec.DisplayName : c.Item;
                views.Add(new PocketSlotView(i, c.Item, name, c.Count, d.StackSize(key)));
            }
            return views;
        }

        /// <summary>The layout token the UI must echo back with a move/split or a storage-to-Backpack drop.</summary>
        public static string Layout(SimContext ctx, SimState st) => Backpack.Layout(ctx.Data, st.Engineer);

        /// <summary>Stacks used / capacity (reference `pocketUsed` / `invCap`).</summary>
        public static (int used, int cap) Capacity(SimContext ctx, SimState st) =>
            (Pockets.Used(ctx.Data, st.Engineer), Pockets.Cap(ctx.Data));

        /// <summary>What the engineer carries, in ordinal key order.</summary>
        public static IReadOnlyList<ItemAmountView> Carried(SimContext ctx, SimState st)
        {
            var d = ctx.Data;
            var keys = new List<ItemKey>();
            st.Engineer.Inv.Keys(keys);
            var views = new List<ItemAmountView>(keys.Count);
            for (var i = 0; i < keys.Count; i++)
            {
                if (!keys[i].IsItem(out var id)) continue;   // packed machines and Phase C weapons are not item lines
                views.Add(new ItemAmountView(id, keys[i].Key, d.Item(id).DisplayName, st.Engineer.Inv[id]));
            }
            return views;
        }

        /// <summary>Reference `machineInventory` + `hasMachineInventory`, as a view.</summary>
        public static MachineInventoryView Machine(SimContext ctx, SimState st, int id)
        {
            var d = ctx.Data;
            var m = st.MachineById(id);
            if (m == null) return null;
            var label = d.TryMachine(m.Kind, out var spec) ? spec.DisplayName : m.Kind;
            var has = MachineInventory.HasInventory(d, m);
            var counts = new ItemCounts();
            if (has) MachineInventory.Contents(d, m, counts);
            var items = new List<ItemAmountView>();
            for (var i = 0; i < Items.Count; i++)
            {
                var k = (ItemId)i;
                if (counts[k] > 0) items.Add(new ItemAmountView(k, Items.Key(k), d.Item(k).DisplayName, counts[k]));
            }
            return new MachineInventoryView(m.Id, m.Kind, label, has, Interaction.InReach(ctx, st, m), items);
        }

        /// <summary>Reference `machineTransferPreview`.</summary>
        public static TransferPreviewView Preview(SimContext ctx, SimState st, int id, ItemId item, int n, bool put)
        {
            var (moved, reason) = MachineInventory.Preview(ctx, st, id, item, n, put);
            return new TransferPreviewView(moved, reason);
        }
    }
}
