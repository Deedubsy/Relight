using System.Collections.Generic;

namespace Relight.Sim
{
    /// <summary>
    /// The whole gameplay state (reference types.ts `SimState` + flow.ts `FlowState`, campaign profile only).
    /// A partial class: each subsystem adds its own sub-state in the file it owns and hooks it into
    /// <see cref="Visit"/> through the partial methods below. Mutated only by command handlers and tick phases.
    /// Presentation reads it through selectors, never writes it.
    /// </summary>
    public sealed partial class SimState : IVisitable
    {
        /// <summary>Save schema version of the Unity port (starts at 1; no reference-save compatibility, U-M-06).</summary>
        public int Version = SimVersion.SchemaVersion;
        public string Ruleset = SimVersion.Ruleset;

        public int Seed;
        /// <summary>Sim seconds elapsed (reference `st.t`).</summary>
        public double T;
        /// <summary>Tile ticks elapsed (reference `f.tick`); 20 per sim second.</summary>
        public int Tick;
        /// <summary>The mulberry32 word (reference `st.rng`).</summary>
        public uint Rng;
        /// <summary>Structural revision: bumped on every placement/removal/rotation (reference `f.rev`); presentation rebuilds indexes when it changes.</summary>
        public int Rev;
        /// <summary>Next machine id (reference `f.next`).</summary>
        public int NextId = 1;

        /// <summary>Events of the current tick; drained by the host each frame; never saved.</summary>
        public readonly List<SimEvent> Events = new List<SimEvent>();

        public Engineer Engineer = new Engineer();

        public void Visit(IStateVisitor v)
        {
            v.Field("version", ref Version);
            v.Field("ruleset", ref Ruleset);
            v.Field("seed", ref Seed);
            v.Field("t", ref T);
            v.Field("tick", ref Tick);
            v.Field("rng", ref Rng);
            v.Field("rev", ref Rev);
            v.Field("next", ref NextId);
            v.Object("engineer", ref Engineer, () => new Engineer());
            v.List("machines", Machines, () => new Machine());
            VisitCore(v);       // B-03: campaign clock, hourly rows, anything else core
            VisitWorld(v);      // B-08: tiles, dug/units, occupancy
            VisitInventory(v);  // B-06: store, stats, ledger opening
            VisitProduction(v); // Phase C
            VisitCombat(v);     // Phase C
            VisitCampaign(v);   // Phase C
        }

        partial void VisitCore(IStateVisitor v);
        partial void VisitWorld(IStateVisitor v);
        partial void VisitInventory(IStateVisitor v);
        partial void VisitProduction(IStateVisitor v);
        partial void VisitCombat(IStateVisitor v);
        partial void VisitCampaign(IStateVisitor v);
    }
}
