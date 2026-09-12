using System;

namespace Relight.Sim
{
    /// <summary>
    /// A `Record&lt;Item, number&gt;` from the reference as an enum-indexed array (TECHNICAL_ARCHITECTURE.md §10.5:
    /// never a Dictionary in gameplay code). Counts are doubles because the reference keeps fractional
    /// quantities in a few places (ledger tolerances, coal burn); inventories that must be whole enforce that themselves.
    /// </summary>
    public sealed class ItemCounts : IVisitable
    {
        private readonly double[] _n = new double[Items.Count];

        public double this[ItemId id]
        {
            get => _n[(int)id];
            set => _n[(int)id] = value;
        }

        public double Add(ItemId id, double n) => _n[(int)id] += n;
        public void Clear() => Array.Clear(_n, 0, _n.Length);
        public bool IsEmpty { get { for (var i = 0; i < _n.Length; i++) if (_n[i] != 0) return false; return true; } }
        public double Total { get { double s = 0; for (var i = 0; i < _n.Length; i++) s += _n[i]; return s; } }

        public ItemCounts Clone()
        {
            var c = new ItemCounts();
            Array.Copy(_n, c._n, _n.Length);
            return c;
        }

        public void CopyFrom(ItemCounts other) => Array.Copy(other._n, _n, _n.Length);

        /// <summary>Raw view for hashing/serialisation. Do not keep the reference.</summary>
        public double[] Raw => _n;

        public void Visit(IStateVisitor v)
        {
            var raw = _n;
            v.Field("n", ref raw);
            if (!ReferenceEquals(raw, _n)) Array.Copy(raw, _n, Math.Min(raw.Length, _n.Length));
        }
    }
}
