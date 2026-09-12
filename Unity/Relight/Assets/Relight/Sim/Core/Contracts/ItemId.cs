using System;

namespace Relight.Sim
{
    /// <summary>
    /// The retained item vocabulary (reference: packages/sim/src/flow.ts ITEMS, filtered by
    /// CONTENT_CATALOGUE.md §17.2: `iron` and `artifact1/2/3` are retired and absent).
    /// The enum value is the index into every <see cref="ItemCounts"/> array; order is the
    /// reference ITEMS order, so it is stable and must not be reordered.
    /// </summary>
    public enum ItemId
    {
        Overclock = 0,
        AlienArtifact,
        IronOre,
        CopperOre,
        Crude,
        Fuel,
        Polymer,
        Shell,
        Core1,
        Core2,
        Core3,
        Steel,
        Copper,
        Stone,
        Coal,
        Magazine,   // one item = one bullet (U-D-08)
        Wire,
        Frame,
        Board,
        Concrete,
    }

    public static class Items
    {
        public const int Count = 20;

        /// <summary>Stable string ids, identical to the reference ids (save/data/doc keys).</summary>
        private static readonly string[] Keys =
        {
            "overclock", "alienartifact", "ironore", "copperore", "crude", "fuel", "polymer", "shell",
            "core1", "core2", "core3", "steel", "copper", "stone", "coal", "magazine", "wire", "frame", "board", "concrete",
        };

        public static string Key(ItemId id) => Keys[(int)id];

        public static bool TryParse(string key, out ItemId id)
        {
            for (var i = 0; i < Keys.Length; i++)
            {
                if (string.Equals(Keys[i], key, StringComparison.Ordinal)) { id = (ItemId)i; return true; }
            }
            id = default;
            return false;
        }

        public static ItemId Parse(string key) =>
            TryParse(key, out var id) ? id : throw new ArgumentException($"unknown item '{key}'", nameof(key));

        /// <summary>All ids in enum order — iterate this, never a dictionary.</summary>
        public static ItemId[] All()
        {
            var a = new ItemId[Count];
            for (var i = 0; i < Count; i++) a[i] = (ItemId)i;
            return a;
        }
    }

    /// <summary>
    /// A key for anything a pocket can hold: an <see cref="ItemId"/>, an owned weapon instance
    /// (`rifle:3`, `double:1`, `arc:2`, `plasma:1` — reference weaponTypes.ts) or a packed machine
    /// (its machine kind key). Ordinal string order is the canonical order wherever keys are sorted
    /// (reference: `Object.keys(left).sort()` in engineer.ts pocketSlots).
    /// </summary>
    public readonly struct ItemKey : IEquatable<ItemKey>, IComparable<ItemKey>
    {
        public string Key { get; }
        public ItemKey(string key) { Key = key ?? throw new ArgumentNullException(nameof(key)); }
        public static ItemKey Of(ItemId id) => new ItemKey(Items.Key(id));

        public bool IsItem(out ItemId id) => Items.TryParse(Key, out id);
        public bool IsWeapon =>
            Key.StartsWith("rifle:", StringComparison.Ordinal) || Key.StartsWith("double:", StringComparison.Ordinal) ||
            Key.StartsWith("arc:", StringComparison.Ordinal) || Key.StartsWith("plasma:", StringComparison.Ordinal);

        public bool Equals(ItemKey other) => string.Equals(Key, other.Key, StringComparison.Ordinal);
        public override bool Equals(object obj) => obj is ItemKey k && Equals(k);
        public override int GetHashCode() => StringComparer.Ordinal.GetHashCode(Key);
        public int CompareTo(ItemKey other) => string.CompareOrdinal(Key, other.Key);
        public override string ToString() => Key;
        public static bool operator ==(ItemKey a, ItemKey b) => a.Equals(b);
        public static bool operator !=(ItemKey a, ItemKey b) => !a.Equals(b);
    }
}
