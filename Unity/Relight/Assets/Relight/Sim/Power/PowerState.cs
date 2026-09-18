using System;
using System.Collections.Generic;

namespace Relight.Sim
{
    /// <summary>
    /// A generator's burn progress towards the next unit of fuel (reference flow.ts:1049 <c>m.timer</c>).
    /// It is a side table entry keyed by machine id, not a field on <see cref="Machine"/>: schema v3's upgrade
    /// fills missing objects from a fresh state but cannot invent members inside list elements (SaveUpgrade.cs).
    /// </summary>
    public sealed class GeneratorBurn : IVisitable
    {
        public int Id;
        /// <summary>Fraction of the next coal/fuel unit already burned, 0 &lt;= t &lt; 1.</summary>
        public double Timer;
        /// <summary>The generator delivered power on the last tick (reference <c>m.busy</c>).</summary>
        public bool Busy;

        public void Visit(IStateVisitor v)
        {
            v.Field("id", ref Id);
            v.Field("timer", ref Timer);
            v.Field("busy", ref Busy);
        }
    }

    /// <summary>
    /// B-09's saved power state: what the reference keeps on the machines themselves (burn progress) plus the one
    /// thing the reference never needed — which machines were supplied at the end of the last tick, so an outage is
    /// reported only when supply that existed is lost (U-D-12: "Power outage at start" was a defect).
    /// A fresh <see cref="PowerState"/> means "nothing has happened yet": no burn progress and nothing supplied, so
    /// the first tick of a new game — or of a Phase B save upgraded to v3 — raises no outage.
    /// Circuits themselves are derived and rebuilt on demand (reference campaignPower.ts:13 caches the same way).
    /// </summary>
    public sealed class PowerState : IVisitable
    {
        /// <summary>Burn progress per generator, in machine-id order (kept sorted so the canonical text is stable).</summary>
        public readonly List<GeneratorBurn> Burn = new List<GeneratorBurn>();

        /// <summary>Machine ids that had supply at the end of the last tick, ascending. Empty = nothing yet.</summary>
        public int[] Supplied = Array.Empty<int>();

        // ---- derived, not saved -------------------------------------------------------------------------------
        private PowerNetwork _grid;
        private NetworkKey _key;
        private bool _hasKey;

        public void Visit(IStateVisitor v)
        {
            v.List("burn", Burn, () => new GeneratorBurn());
            v.Field("supplied", ref Supplied);
            _hasKey = false;   // a load invalidates the derived grid
            _grid = null;
        }

        /// <summary>The burn entry for a generator, created on first use (ids ascending, so the save order is stable).</summary>
        public GeneratorBurn BurnOf(int id)
        {
            var lo = 0;
            var hi = Burn.Count - 1;
            while (lo <= hi)
            {
                var mid = (lo + hi) / 2;
                if (Burn[mid].Id == id) return Burn[mid];
                if (Burn[mid].Id < id) lo = mid + 1; else hi = mid - 1;
            }
            var e = new GeneratorBurn { Id = id };
            Burn.Insert(lo, e);
            return e;
        }

        /// <summary>Drops burn entries whose generator no longer exists (called when the structural revision moves).</summary>
        public void Prune(SimState st)
        {
            for (var i = Burn.Count - 1; i >= 0; i--)
                if (st.MachineById(Burn[i].Id) == null) Burn.RemoveAt(i);
        }

        internal bool TryCached(NetworkKey key, out PowerNetwork grid)
        {
            if (_hasKey && _key.Equals(key) && _grid != null) { grid = _grid; return true; }
            grid = null;
            return false;
        }

        internal void Cache(NetworkKey key, PowerNetwork grid)
        {
            _key = key;
            _hasKey = true;
            _grid = grid;
        }

        /// <summary>The grid built during the current tick, or null before the first <see cref="PowerPhase"/> tick.</summary>
        internal PowerNetwork Last => _grid;
    }

    /// <summary>
    /// S-25: the reference caches the grid under a composed string key (campaignPower.ts:24). The port uses a value
    /// struct instead — same invalidation, no per-tick string building. <see cref="Fuel"/> folds "which generators
    /// have fuel" the way the reference's key does, so a generator running dry rebuilds the grid at once.
    /// </summary>
    internal readonly struct NetworkKey : IEquatable<NetworkKey>
    {
        public readonly int Rev;
        public readonly int Tick;
        public readonly int Count;
        public readonly int Fuel;

        public NetworkKey(int rev, int tick, int count, int fuel) { Rev = rev; Tick = tick; Count = count; Fuel = fuel; }

        public bool Equals(NetworkKey o) => Rev == o.Rev && Tick == o.Tick && Count == o.Count && Fuel == o.Fuel;
        public override bool Equals(object o) => o is NetworkKey k && Equals(k);
        public override int GetHashCode() => (((Rev * 397) ^ Tick) * 397 ^ Count) * 397 ^ Fuel;
    }

    public sealed partial class SimState
    {
        /// <summary>The power sub-state (B-09), visited as <c>power</c>.</summary>
        public PowerState Power = new PowerState();

        partial void VisitPower(IStateVisitor v) => v.Object("power", ref Power, () => new PowerState());
    }
}
