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
    /// A plant's stage (§5.7 <c>PlantState</c>, FRT-08, save v18), keyed by <see cref="PlantDef.Id"/>. Added when it
    /// is prepared, so an untouched plant has no record and a fresh game and an upgraded save agree.
    /// </summary>
    public sealed class PlantRecord : IVisitable
    {
        public string Id = "";
        /// <summary>Its squat is cleared and its repair paid (§4.4 <c>Prepared</c>).</summary>
        public bool Prepared;
        /// <summary>Sim time its Power core went in; -1 before.</summary>
        public double CommissionedAt = -1;
        /// <summary>Its hit points once commissioned; it supplies only while above 0. FRT-09's raid is what lowers it.</summary>
        public double Hp;

        public bool Commissioned => CommissionedAt >= 0;

        public void Visit(IStateVisitor v)
        {
            v.Field("id", ref Id);
            v.Field("prepared", ref Prepared);
            v.Field("commissionedAt", ref CommissionedAt);
            v.Field("hp", ref Hp);
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
        /// <summary>
        /// The strongholds whose doors are open (§5.4, FRT-05, save v15), by <see cref="StrongholdDef.Id"/>, in the
        /// order they were opened. A door never shuts again.
        /// </summary>
        public List<string> Opened = new List<string>();
        /// <summary>
        /// U-D-69 (e), FRT-05 (save v15): sim time a stronghold fight runs until; -1 when none has been fought.
        /// Pushed on every tick the engineer is engaged, so the fight ends
        /// <see cref="EncounterCatalogue.FightTailSeconds"/> after they leave or go down.
        /// </summary>
        public double FightUntil = -1;
        /// <summary>
        /// The strongholds whose guardian has died (§5.5, FRT-06, save v16), by <see cref="StrongholdDef.Id"/>, in
        /// the order they fell. A guardian dies once.
        /// </summary>
        public List<string> Felled = new List<string>();
        /// <summary>
        /// Power cores lying on the ground (§4.3 <c>CoreDrop</c>, FRT-06, save v16): one where each guardian fell
        /// until the engineer picks it up (FRT-07).
        /// </summary>
        public List<LooseCore> Cores = new List<LooseCore>();
        /// <summary>The plants prepared so far (§5.7, FRT-08, save v18), in the order they were prepared.</summary>
        public List<PlantRecord> Plants = new List<PlantRecord>();
        /// <summary>
        /// The plant commissioned most recently (§5.7 <c>NewestPlant</c>, FRT-08, save v18); "" before any. FRT-09's
        /// next major raid reads it.
        /// </summary>
        public string NewestPlant = "";

        public PlantRecord Plant(string id)
        {
            for (var i = 0; i < Plants.Count; i++)
                if (string.CompareOrdinal(Plants[i].Id, id) == 0) return Plants[i];
            return null;
        }

        public bool HasFallen(string stronghold)
        {
            for (var i = 0; i < Felled.Count; i++) if (string.CompareOrdinal(Felled[i], stronghold) == 0) return true;
            return false;
        }

        public bool IsOpen(string stronghold)
        {
            for (var i = 0; i < Opened.Count; i++) if (string.CompareOrdinal(Opened[i], stronghold) == 0) return true;
            return false;
        }

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
            v.StringList("opened", Opened);
            v.Field("fightUntil", ref FightUntil);
            v.StringList("felled", Felled);
            v.List("cores", Cores, () => new LooseCore());
            v.List("plants", Plants, () => new PlantRecord());
            v.Field("newestPlant", ref NewestPlant);
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
