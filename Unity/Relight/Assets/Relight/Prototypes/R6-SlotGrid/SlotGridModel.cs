using System.Collections.Generic;

namespace Relight.Prototypes
{
    /// <summary>Which of the two grids a slot belongs to. The reference's <c>side</c> ('pack' | 'store').</summary>
    public enum SlotSide { Pack = 0, Store = 1 }

    /// <summary>A slot address: a side plus an index into that side's slot list.</summary>
    public readonly struct SlotAddress
    {
        public readonly SlotSide Side;
        public readonly int Index;
        public SlotAddress(SlotSide side, int index) { Side = side; Index = index; }
        public override string ToString() => $"{Side}[{Index}]";
        public bool Equals(SlotAddress o) => Side == o.Side && Index == o.Index;
    }

    /// <summary>One stack: an item id and a count. Count 0 means the slot is empty.</summary>
    public struct SlotStack
    {
        public string Item;
        public int Count;
        public bool Empty => string.IsNullOrEmpty(Item) || Count <= 0;
        public static readonly SlotStack None = new SlotStack { Item = null, Count = 0 };
    }

    /// <summary>
    /// B-14 / R6. The prototype's data: two fixed-length slot lists held in memory, nothing else. This deliberately
    /// does NOT talk to the simulation — C-06 replaces every mutation here with the real B-06 inventory commands
    /// (<c>InventoryCommand</c> move/split and the chest/machine transfers), and the drag layer above it is written
    /// so that swapping this class for a sim-backed one touches no drag code.
    ///
    /// Rule ported from the reference (inventoryPanel.ts <c>relocate</c> → sim <c>move</c>): dropping a stack onto an
    /// occupied slot exchanges the two stacks; dropping onto an empty slot moves it.
    /// </summary>
    public sealed class SlotGridModel
    {
        private readonly SlotStack[] _pack;
        private readonly SlotStack[] _store;

        /// <summary>Backpack slots. The reference's pocket grid; 40 (8 x 5) in this prototype.</summary>
        public int PackSlots => _pack.Length;

        /// <summary>Storage slots. The second grid the drag must cross; 20 in this prototype.</summary>
        public int StoreSlots => _store.Length;

        /// <summary>Incremented on every accepted mutation, so a view can rebuild only when something changed.</summary>
        public int Revision { get; private set; }

        public SlotGridModel(int packSlots, int storeSlots)
        {
            _pack = new SlotStack[packSlots];
            _store = new SlotStack[storeSlots];
        }

        private SlotStack[] Side(SlotSide side) => side == SlotSide.Pack ? _pack : _store;

        public bool InRange(SlotAddress a) => a.Index >= 0 && a.Index < Side(a.Side).Length;

        public SlotStack Get(SlotAddress a) => InRange(a) ? Side(a.Side)[a.Index] : SlotStack.None;

        public void Set(SlotAddress a, string item, int count)
        {
            if (!InRange(a)) return;
            Side(a.Side)[a.Index] = new SlotStack { Item = item, Count = count };
            Revision++;
        }

        /// <summary>
        /// Move or swap. Returns false (and changes nothing) for an out-of-range address, a move onto itself, or a
        /// move from an empty slot — the three refusals the reference's snapshot/relocate pair also makes.
        /// </summary>
        public bool MoveOrSwap(SlotAddress from, SlotAddress to)
        {
            if (!InRange(from) || !InRange(to)) return false;
            if (from.Equals(to)) return false;
            var a = Side(from.Side);
            var b = Side(to.Side);
            if (a[from.Index].Empty) return false;
            var moved = a[from.Index];
            a[from.Index] = b[to.Index];      // empty or the swapped stack
            b[to.Index] = moved;
            Revision++;
            return true;
        }

        /// <summary>Fill every slot of both grids from a repeating item list (the "all 60 slots populated" fixture).</summary>
        public void FillAll(IReadOnlyList<string> items, int countSeed = 1)
        {
            var n = 0;
            for (var i = 0; i < _pack.Length; i++)
                _pack[i] = new SlotStack { Item = items[n++ % items.Count], Count = countSeed + (i * 7) % 97 };
            for (var i = 0; i < _store.Length; i++)
                _store[i] = new SlotStack { Item = items[n++ % items.Count], Count = countSeed + (i * 13) % 97 };
            Revision++;
        }

        public int OccupiedCount()
        {
            var n = 0;
            for (var i = 0; i < _pack.Length; i++) if (!_pack[i].Empty) n++;
            for (var i = 0; i < _store.Length; i++) if (!_store[i].Empty) n++;
            return n;
        }
    }
}
