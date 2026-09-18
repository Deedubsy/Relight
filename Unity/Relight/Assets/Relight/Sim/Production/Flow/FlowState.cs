using System.Collections.Generic;

namespace Relight.Sim
{
    /// <summary>
    /// One item riding a conveyor (reference flow.ts <c>{ k: Item; p: number }</c>). <c>P</c> is the distance
    /// travelled along the device: 0 at the entry edge, 1 at the exit edge of a belt or an underground exit, and up
    /// to the span in tiles inside an underground entrance. A splitter's buffer keeps every item at 0.
    /// The item is stored as the <see cref="ItemId"/> ordinal so the save is a plain number, exactly as every other
    /// per-item field in the port is.
    /// </summary>
    public sealed class BeltItem : IVisitable
    {
        public int K;
        public double P;
        // Transient rendering snapshots; intentionally excluded from Visit/save and simulation decisions.
        public Vec2 PreviousPosition;
        public bool HasPreviousPosition;

        public ItemId Item => (ItemId)K;

        public void Visit(IStateVisitor v)
        {
            v.Field("k", ref K);
            v.Field("p", ref P);
        }
    }

    /// <summary>
    /// One flow machine's state, keyed by machine id. The reference keeps all of this on the machine itself
    /// (flow.ts <c>Machine.items/timer/phase/hold/filter/priority/routingNext/underground/pickupNext</c>), which
    /// this port cannot do: schema v3 upgrades a Phase B save by filling missing object members from a fresh state
    /// and cannot add members inside list elements (SaveUpgrade.cs). It is therefore a side table, the same idiom
    /// <see cref="MachineWork"/> uses for B-07.
    ///
    /// A fresh entry means "nothing has happened yet": an empty belt, an inserter at rest, a balanced splitter and
    /// an underground <em>entrance</em> — which is what reference <c>addMachine</c> gives a newly placed underground
    /// (flow.ts:1350 <c>if (kind === 'underground') m.underground = 'input';</c>). An upgraded Phase B save has no
    /// flow machines at all, so every default is unobservable there.
    /// </summary>
    public sealed class FlowMachine : IVisitable
    {
        public int Id;

        /// <summary>Items on the device, ordered by <c>P</c> ascending: index 0 is the tail, the last is the leader.</summary>
        public readonly List<BeltItem> Items = new List<BeltItem>();

        /// <summary>Seconds: an inserter's remaining swing, a splitter's accumulated service time (reference <c>m.timer</c>).</summary>
        public double Timer;

        /// <summary>Inserter swing phase (reference <c>m.phase</c>): 0 waiting to pick up, 1 swinging out, 2 swinging back.</summary>
        public int Phase;

        /// <summary>The item in an inserter's hand as an <see cref="ItemId"/> ordinal, or -1 for an empty hand (reference <c>m.hold</c>).</summary>
        public int Hold = -1;

        /// <summary>An inserter's item filter as an <see cref="ItemId"/> ordinal, or -1 for "any item" (reference <c>m.filter</c>).</summary>
        public int Filter = -1;

        /// <summary>Which splitter port a balanced splitter tries first next (reference <c>m.routingNext</c>): 0 left, 1 right.</summary>
        public int RoutingNext;

        /// <summary>Splitter output priority (reference <c>m.priority</c>): 0 balanced, 1 left, 2 right.</summary>
        public int Priority;

        /// <summary>Underground endpoint role (reference <c>m.underground</c>): 0 input (the entrance), 1 output (the exit).</summary>
        public int Mode;

        /// <summary>Round-robin cursor over ITEMS for direct conveyor loading (reference <c>m.pickupNext</c>).</summary>
        public int PickupNext;

        public void Visit(IStateVisitor v)
        {
            v.Field("id", ref Id);
            v.List("items", Items, () => new BeltItem());
            v.Field("timer", ref Timer);
            v.Field("phase", ref Phase);
            v.Field("hold", ref Hold);
            v.Field("filter", ref Filter);
            v.Field("next", ref RoutingNext);
            v.Field("priority", ref Priority);
            v.Field("mode", ref Mode);
            v.Field("pickup", ref PickupNext);
        }
    }

    /// <summary>
    /// B-10's saved flow state, visited as <c>flow</c>: what every belt, underground, splitter and inserter is
    /// carrying and doing. A fresh instance means an empty factory, which is exactly how a Phase B save upgraded to
    /// v3 must resume.
    ///
    /// The two transient caches the reference keeps in module-level <c>WeakMap</c>s keyed on the flow state
    /// (flow.ts <c>orderCache</c>, directConveyor.ts <c>cache</c>) live here as unvisited fields keyed on
    /// <see cref="SimState.Rev"/> and the machine count, so they are per-game exactly as the reference's are, are
    /// never saved and never enter the state hash.
    /// </summary>
    public sealed class FlowState : IVisitable
    {
        /// <summary>Entries in machine-id order, so the canonical text is stable.</summary>
        public readonly List<FlowMachine> Lanes = new List<FlowMachine>();

        public void Visit(IStateVisitor v) => v.List("lanes", Lanes, () => new FlowMachine());

        /// <summary>The entry for a machine, created on first use (kept sorted by id).</summary>
        public FlowMachine Of(int id)
        {
            var lo = 0;
            var hi = Lanes.Count - 1;
            while (lo <= hi)
            {
                var mid = (lo + hi) / 2;
                if (Lanes[mid].Id == id) return Lanes[mid];
                if (Lanes[mid].Id < id) lo = mid + 1; else hi = mid - 1;
            }
            var e = new FlowMachine { Id = id };
            Lanes.Insert(lo, e);
            return e;
        }

        /// <summary>The entry for a machine if one exists, without creating it (queries must not mutate).</summary>
        public FlowMachine Find(int id)
        {
            var lo = 0;
            var hi = Lanes.Count - 1;
            while (lo <= hi)
            {
                var mid = (lo + hi) / 2;
                if (Lanes[mid].Id == id) return Lanes[mid];
                if (Lanes[mid].Id < id) lo = mid + 1; else hi = mid - 1;
            }
            return null;
        }

        /// <summary>Forgets a machine's flow state (used by removal, which returns the contents first).</summary>
        public void Forget(int id)
        {
            for (var i = 0; i < Lanes.Count; i++)
                if (Lanes[i].Id == id) { Lanes.RemoveAt(i); return; }
        }

        /// <summary>Drops entries whose machine has been removed.</summary>
        public void Prune(SimState st)
        {
            for (var i = Lanes.Count - 1; i >= 0; i--)
                if (st.MachineById(Lanes[i].Id) == null) Lanes.RemoveAt(i);
        }

        // ---- transient caches (never visited, never hashed) -----------------------------------------------------

        internal int OrderRev = -1;
        internal int OrderCount = -1;
        internal readonly List<Machine> Order = new List<Machine>();

        internal int RouteRev = -1;
        internal int RouteCount = -1;
        /// <summary>One entry per conveyor, in machine-id order: where the items it carries can end up.</summary>
        internal readonly List<ConveyorRoute> Routes = new List<ConveyorRoute>();
    }

    /// <summary>A conveyor's reachable non-conveyor endpoints (reference directConveyor.ts <c>conveyorDestinations</c>). Transient.</summary>
    public sealed class ConveyorRoute
    {
        public int Id;
        public readonly List<Machine> Ends = new List<Machine>();
    }

    public sealed partial class SimState
    {
        /// <summary>The belt/inserter/routing sub-state (B-10), visited as <c>flow</c>.</summary>
        public FlowState Flow = new FlowState();

        partial void VisitFlow(IStateVisitor v) => v.Object("flow", ref Flow, () => new FlowState());
    }
}
