using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace Relight.Sim
{
    /// <summary>
    /// The engineer's pockets (reference engineer.ts `Engineer.inv`, a `Record&lt;string, number&gt;`).
    /// The 20 sim items live in an enum-indexed <see cref="ItemCounts"/>; anything else a pocket can hold
    /// (a packed machine kind, a Phase C weapon instance `rifle:3`) lives in a parallel pair of ordered lists.
    /// <see cref="Keys"/> merges the two in ordinal key order, which is the canonical order everywhere
    /// (reference `Object.keys(left).sort()` in pocketSlots) — no dictionary is ever iterated (TA §10.5).
    /// A key with a count of 0 or less is absent, mirroring the reference's `delete inv[k]`.
    /// </summary>
    public sealed class ItemBag : IVisitable
    {
        /// <summary>Item keys in ordinal string order — the canonical sort order for the 20 sim items.</summary>
        private static readonly ItemId[] Ordinal = BuildOrdinal();

        public ItemCounts Items = new ItemCounts();
        private readonly List<string> _otherKeys = new List<string>();   // ordinal-ascending, counts always > 0
        private readonly List<double> _otherCounts = new List<double>();

        private static ItemId[] BuildOrdinal()
        {
            var a = Sim.Items.All();
            for (var i = 1; i < a.Length; i++)   // insertion sort: deterministic, no LINQ
            {
                var v = a[i];
                var j = i - 1;
                while (j >= 0 && string.CompareOrdinal(Sim.Items.Key(a[j]), Sim.Items.Key(v)) > 0) { a[j + 1] = a[j]; j--; }
                a[j + 1] = v;
            }
            return a;
        }

        public double this[ItemId id]
        {
            get => Items[id];
            set => Items[id] = value;
        }

        public double this[ItemKey key]
        {
            get
            {
                if (key.IsItem(out var id)) return Items[id];
                var i = IndexOfOther(key.Key);
                return i < 0 ? 0 : _otherCounts[i];
            }
            set
            {
                if (key.IsItem(out var id)) { Items[id] = value > 0 ? value : 0; return; }
                var i = IndexOfOther(key.Key);
                if (value > 0)
                {
                    if (i >= 0) { _otherCounts[i] = value; return; }
                    var at = 0;
                    while (at < _otherKeys.Count && string.CompareOrdinal(_otherKeys[at], key.Key) < 0) at++;
                    _otherKeys.Insert(at, key.Key);
                    _otherCounts.Insert(at, value);
                }
                else if (i >= 0) { _otherKeys.RemoveAt(i); _otherCounts.RemoveAt(i); }
            }
        }

        private int IndexOfOther(string key)
        {
            for (var i = 0; i < _otherKeys.Count; i++) if (string.Equals(_otherKeys[i], key, StringComparison.Ordinal)) return i;
            return -1;
        }

        public bool IsEmpty
        {
            get
            {
                if (_otherKeys.Count > 0) return false;
                for (var i = 0; i < Sim.Items.Count; i++) if (Items[(ItemId)i] > 0) return false;
                return true;
            }
        }

        /// <summary>Every key the bag actually holds (count &gt; 0), in ordinal key order.</summary>
        public void Keys(List<ItemKey> into)
        {
            into.Clear();
            int a = 0, b = 0;
            while (a < Ordinal.Length || b < _otherKeys.Count)
            {
                if (a < Ordinal.Length && Items[Ordinal[a]] <= 0) { a++; continue; }
                if (a >= Ordinal.Length) { into.Add(new ItemKey(_otherKeys[b++])); continue; }
                if (b >= _otherKeys.Count) { into.Add(ItemKey.Of(Ordinal[a++])); continue; }
                into.Add(string.CompareOrdinal(Sim.Items.Key(Ordinal[a]), _otherKeys[b]) < 0
                    ? ItemKey.Of(Ordinal[a++]) : new ItemKey(_otherKeys[b++]));
            }
        }

        public ItemBag Clone()
        {
            var c = new ItemBag { Items = Items.Clone() };
            c._otherKeys.AddRange(_otherKeys);
            c._otherCounts.AddRange(_otherCounts);
            return c;
        }

        public void Visit(IStateVisitor v)
        {
            v.Object("items", ref Items, () => new ItemCounts());
            v.StringList("keys", _otherKeys);
            var counts = _otherCounts.ToArray();
            v.Field("counts", ref counts);
            if (v.IsReading)
            {
                _otherCounts.Clear();
                for (var i = 0; i < _otherKeys.Count; i++) _otherCounts.Add(i < counts.Length ? counts[i] : 0);
            }
        }
    }

    /// <summary>
    /// One Backpack cell (reference engineer.ts `PackStack`). The reference's `reserved` flag existed only for
    /// legacy kits, which are retired (CONTENT_CATALOGUE §17), so it is not ported.
    /// </summary>
    public sealed class PackStack
    {
        public string Item;
        public double Count;
        public PackStack(string item, double count) { Item = item; Count = count; }
        public PackStack Clone() => new PackStack(Item, Count);
    }

    /// <summary>The Backpack layout string the UI echoes back with a move/split (reference `JSON.stringify(slots)`).</summary>
    public static class PackLayout
    {
        public static string Json(List<PackStack> slots)
        {
            var sb = new StringBuilder();
            sb.Append('[');
            for (var i = 0; i < slots.Count; i++)
            {
                if (i > 0) sb.Append(',');
                var s = slots[i];
                if (s == null) { sb.Append("null"); continue; }
                sb.Append("{\"item\":\"");
                for (var c = 0; c < s.Item.Length; c++)
                {
                    var ch = s.Item[c];
                    if (ch == '"' || ch == '\\') sb.Append('\\');
                    sb.Append(ch);
                }
                sb.Append("\",\"count\":").Append(Num(s.Count)).Append('}');
            }
            sb.Append(']');
            return sb.ToString();
        }

        /// <summary>JSON number text, matching JavaScript's integer rendering for whole counts.</summary>
        public static string Num(double n) =>
            n == Math.Floor(n) && Math.Abs(n) < 1e15
                ? ((long)n).ToString(CultureInfo.InvariantCulture)
                : n.ToString("R", CultureInfo.InvariantCulture);
    }

    public sealed partial class Engineer
    {
        /// <summary>What the engineer carries (reference `e.inv`).</summary>
        public ItemBag Inv = new ItemBag();
        /// <summary>
        /// The player's explicit Backpack allocation, or null when the Backpack is auto-arranged
        /// (reference `e.pack`). Entries may be null (an empty cell). Always <c>data.Engineer.InvStacks</c> long.
        /// </summary>
        public List<PackStack> Pack;
        /// <summary>Interaction reach in tiles (reference `e.reach`, REACH 8).</summary>
        public double Reach = 8;

        partial void VisitInventory(IStateVisitor v)
        {
            v.Object("inv", ref Inv, () => new ItemBag());
            v.Field("reach", ref Reach);
            var has = Pack != null;
            v.Field("hasPack", ref has);
            var n = Pack?.Count ?? 0;
            var keys = new List<string>(n);
            var counts = new double[n];
            if (!v.IsReading && Pack != null)
                for (var i = 0; i < n; i++) { var s = Pack[i]; keys.Add(s == null ? "" : s.Item); counts[i] = s?.Count ?? 0; }
            v.StringList("packItems", keys);
            v.Field("packCounts", ref counts);
            if (!v.IsReading) return;
            if (!has) { Pack = null; return; }
            Pack = new List<PackStack>(keys.Count);
            for (var i = 0; i < keys.Count; i++)
                Pack.Add(string.IsNullOrEmpty(keys[i]) ? null : new PackStack(keys[i], i < counts.Length ? counts[i] : 0));
        }
    }
}
