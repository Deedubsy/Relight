using System.Collections.Generic;

namespace Relight.Sim
{
    /// <summary>
    /// One encounter's life so far (FREIGHT_STRONGHOLD_DESIGN §3 <c>EncounterState</c>), keyed by
    /// <see cref="EncounterDef.Id"/>. How many of its bodies still live is never stored: it is counted off
    /// <see cref="Enemy.Site"/>, so the two cannot disagree.
    /// </summary>
    public sealed class EncounterRecord : IVisitable
    {
        public string Id = "";
        /// <summary>
        /// Its search circle is on the map. Set by the objective chain that sends the engineer looking (FRT-10);
        /// finding the place does not wait for it, so a camp stumbled on early still has its guards in it.
        /// </summary>
        public bool Discovered;
        /// <summary>Its garrison has been born (§5.1). Once per life of the encounter.</summary>
        public bool Resolved;
        /// <summary>Sim time it was resolved; -1 before.</summary>
        public double ResolvedAt = -1;
        /// <summary>Sim time its last body died; -1 while any still lives or before it was resolved.</summary>
        public double ClearedAt = -1;
        /// <summary>Its key or its loot has been taken (§5.2, FRT-03).</summary>
        public bool Claimed;
        /// <summary>Sim time the engineer began holding it with no guard near (§5.2, FRT-03); -1 when not holding.</summary>
        public double OccupiedSince = -1;
        /// <summary>The machine id of its cache crate (§5.3, FRT-03); 0 when it has none.</summary>
        public int Cache;

        public void Visit(IStateVisitor v)
        {
            v.Field("id", ref Id);
            v.Field("discovered", ref Discovered);
            v.Field("resolved", ref Resolved);
            v.Field("resolvedAt", ref ResolvedAt);
            v.Field("clearedAt", ref ClearedAt);
            v.Field("claimed", ref Claimed);
            v.Field("occupiedSince", ref OccupiedSince);
            v.Field("cache", ref Cache);
        }
    }

    /// <summary>
    /// Batch 4's encounters, visited as <c>encounters</c> (save v13). A record is added the first tick its row
    /// runs, in catalogue order, so a fresh game, an upgraded v12 save and a crop that lacks a camp all start from
    /// "nothing found yet" without a list to keep in step with the catalogue.
    /// </summary>
    public sealed class EncounterState : IVisitable
    {
        public List<EncounterRecord> Records = new List<EncounterRecord>();
        /// <summary>
        /// The engineer's quest pouch (§5.2, FRT-03, save v14): the stronghold keys claimed so far, in the order
        /// they were claimed. It is not the Backpack: a key takes no slot, cannot be dropped or stored, and is not
        /// lost when the engineer goes down.
        /// </summary>
        public List<string> Pouch = new List<string>();

        public bool Holds(string key)
        {
            for (var i = 0; i < Pouch.Count; i++) if (string.CompareOrdinal(Pouch[i], key) == 0) return true;
            return false;
        }

        public EncounterRecord Find(string id)
        {
            for (var i = 0; i < Records.Count; i++)
                if (string.CompareOrdinal(Records[i].Id, id) == 0) return Records[i];
            return null;
        }

        public EncounterRecord Ensure(string id)
        {
            var r = Find(id);
            if (r != null) return r;
            r = new EncounterRecord { Id = id };
            Records.Add(r);
            return r;
        }

        public void Visit(IStateVisitor v)
        {
            v.List("records", Records, () => new EncounterRecord());
            v.StringList("pouch", Pouch);
        }
    }

    public sealed partial class SimState
    {
        /// <summary>Batch 4's camps, squats and arenas (FRT-02), visited as <c>encounters</c>.</summary>
        public EncounterState Encounters = new EncounterState();

        partial void VisitEncounters(IStateVisitor v) =>
            v.Object("encounters", ref Encounters, () => new EncounterState());
    }
}
