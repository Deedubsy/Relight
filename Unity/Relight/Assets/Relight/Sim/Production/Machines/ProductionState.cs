using System.Collections.Generic;

namespace Relight.Sim
{
    /// <summary>
    /// One machine's work in progress: the reference keeps <c>timer</c>, <c>busy</c> and <c>recipe</c> on the
    /// machine itself (flow.ts <c>Machine</c>), which this port cannot do — schema v3 upgrades a Phase B save by
    /// filling missing object members from a fresh state and cannot add members inside list elements
    /// (SaveUpgrade.cs). Per-machine production state is therefore a side table keyed by machine id.
    /// </summary>
    public sealed class MachineWork : IVisitable
    {
        public int Id;
        /// <summary>Seconds of the current craft, or of the current mining cycle (reference <c>m.timer</c>).</summary>
        public double Timer;
        /// <summary>A craft's inputs have been taken and not yet returned as output (reference <c>m.busy</c>).</summary>
        public bool Busy;
        /// <summary>The chosen recipe key, or "" for the machine's default (reference <c>m.recipe</c>, flow.ts:200).</summary>
        public string Recipe = "";
        /// <summary>Why the machine did nothing on the last tick, for the UI (see <see cref="MachineOperatingState"/>).</summary>
        public int Stall;

        public void Visit(IStateVisitor v)
        {
            v.Field("id", ref Id);
            v.Field("timer", ref Timer);
            v.Field("busy", ref Busy);
            v.Field("recipe", ref Recipe);
            v.Field("stall", ref Stall);
        }
    }

    /// <summary>
    /// B-07's saved production state, visited as <c>production</c>. A fresh instance means "nothing has happened
    /// yet": no machine has a craft in progress and every machine runs its data default recipe, which is exactly
    /// what a Phase B save upgraded to v3 should resume as.
    /// </summary>
    public sealed class ProductionState : IVisitable
    {
        /// <summary>Work entries in machine-id order, so the canonical text is stable.</summary>
        public readonly List<MachineWork> Work = new List<MachineWork>();

        public void Visit(IStateVisitor v) => v.List("work", Work, () => new MachineWork());

        /// <summary>The entry for a machine, created on first use (kept sorted by id).</summary>
        public MachineWork Of(int id)
        {
            var lo = 0;
            var hi = Work.Count - 1;
            while (lo <= hi)
            {
                var mid = (lo + hi) / 2;
                if (Work[mid].Id == id) return Work[mid];
                if (Work[mid].Id < id) lo = mid + 1; else hi = mid - 1;
            }
            var e = new MachineWork { Id = id };
            Work.Insert(lo, e);
            return e;
        }

        /// <summary>The entry for a machine if one exists, without creating it (queries must not mutate).</summary>
        public MachineWork Find(int id)
        {
            var lo = 0;
            var hi = Work.Count - 1;
            while (lo <= hi)
            {
                var mid = (lo + hi) / 2;
                if (Work[mid].Id == id) return Work[mid];
                if (Work[mid].Id < id) lo = mid + 1; else hi = mid - 1;
            }
            return null;
        }

        /// <summary>Drops entries whose machine has been removed.</summary>
        public void Prune(SimState st)
        {
            for (var i = Work.Count - 1; i >= 0; i--)
                if (st.MachineById(Work[i].Id) == null) Work.RemoveAt(i);
        }
    }

    public sealed partial class SimState
    {
        /// <summary>The machine-processing sub-state (B-07), visited as <c>production</c>.</summary>
        public ProductionState Production = new ProductionState();

        partial void VisitMachines(IStateVisitor v) => v.Object("production", ref Production, () => new ProductionState());
    }
}
