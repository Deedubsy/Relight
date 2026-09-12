namespace Relight.Sim
{
    /// <summary>
    /// The run's counters (reference flow.ts `FlowState.stats` plus the `SimStats` fields the ledger reads:
    /// `spentSteel`, `spentCopper`, `roundsLost`, `magsMade`). Every field is either a ledger source, a ledger
    /// sink, or telemetry; the ledger reads only the first two.
    /// Retired counters are absent: `handMined`/`handMinedOf` (hand mining needs ground deposits and is not in
    /// Phase B), `railCoal` (tram retired), `ringDraw`/`ringFired` (block/lattice economy retired, U-D-32).
    /// </summary>
    public sealed class Stats : IVisitable
    {
        // --- ledger sources -------------------------------------------------
        /// <summary>Items produced (recipes, hand crafting) — reference `stats.made`.</summary>
        public ItemCounts Made = new ItemCounts();
        /// <summary>Items mined out of the ground — reference `stats.minedOf`. Phase C fills this; Phase B has no mining.</summary>
        public ItemCounts MinedOf = new ItemCounts();

        // --- ledger sinks ---------------------------------------------------
        /// <summary>Items consumed as recipe inputs — reference `stats.consumed`.</summary>
        public ItemCounts Consumed = new ItemCounts();
        /// <summary>Items paid as machine build cost — reference `stats.placed`. Phase C pays; see Placement.cs.</summary>
        public ItemCounts Placed = new ItemCounts();
        /// <summary>Reference `SimStats.spentSteel` / `spentCopper` (campaign spends outside recipes).</summary>
        public double SpentSteel;
        public double SpentCopper;
        /// <summary>Repairs paid in copper — reference `f.repairs` × REPAIR_COPPER (1).</summary>
        public int Repairs;
        /// <summary>Coal burned by generators — reference `stats.coalBurned`.</summary>
        public double CoalBurned;
        /// <summary>Rounds fired by turrets — reference `stats.fired`. One round = one bullet (U-D-08).</summary>
        public int Fired;
        /// <summary>Rounds fired by the engineer — reference `st.engineer.fired`.</summary>
        public int EngineerFired;
        /// <summary>Rounds that no longer fitted anywhere when a turret was packed — reference `SimStats.roundsLost`.</summary>
        public int RoundsLost;

        // --- telemetry (never a ledger term) --------------------------------
        public int MagsMade;
        public int MagsDelivered;
        public int Mined;
        public int HandCrafted;
        public int HandFed;
        public int HandFedMags;
        public int HandFedCoal;
        public int ChestTrips;
        public int ReachRefused;
        public int TurretFed;
        public int GenFed;
        /// <summary>Items machines delivered / the engineer put back — reference `stats.delivered` / `stats.putBack`.</summary>
        public ItemCounts Delivered = new ItemCounts();
        public ItemCounts PutBack = new ItemCounts();

        public void Visit(IStateVisitor v)
        {
            v.Object("made", ref Made, () => new ItemCounts());
            v.Object("minedOf", ref MinedOf, () => new ItemCounts());
            v.Object("consumed", ref Consumed, () => new ItemCounts());
            v.Object("placed", ref Placed, () => new ItemCounts());
            v.Field("spentSteel", ref SpentSteel);
            v.Field("spentCopper", ref SpentCopper);
            v.Field("repairs", ref Repairs);
            v.Field("coalBurned", ref CoalBurned);
            v.Field("fired", ref Fired);
            v.Field("engineerFired", ref EngineerFired);
            v.Field("roundsLost", ref RoundsLost);
            v.Field("magsMade", ref MagsMade);
            v.Field("magsDelivered", ref MagsDelivered);
            v.Field("mined", ref Mined);
            v.Field("handCrafted", ref HandCrafted);
            v.Field("handFed", ref HandFed);
            v.Field("handFedMags", ref HandFedMags);
            v.Field("handFedCoal", ref HandFedCoal);
            v.Field("chestTrips", ref ChestTrips);
            v.Field("reachRefused", ref ReachRefused);
            v.Field("turretFed", ref TurretFed);
            v.Field("genFed", ref GenFed);
            v.Object("delivered", ref Delivered, () => new ItemCounts());
            v.Object("putBack", ref PutBack, () => new ItemCounts());
        }
    }

    /// <summary>
    /// The conservation ledger's opening stock (reference ledger.ts `openLedger` → `FlowState.ledger`):
    /// held − net flows at the moment the ledger was opened, so <c>conservation</c> reads zero from there on.
    /// </summary>
    public sealed class LedgerOpening : IVisitable
    {
        public int Tick;
        public ItemCounts Base = new ItemCounts();

        public void Visit(IStateVisitor v)
        {
            v.Field("tick", ref Tick);
            v.Object("base", ref Base, () => new ItemCounts());
        }
    }

    public sealed partial class SimState
    {
        /// <summary>The run's counters — the ledger's sources and sinks (reference `f.stats` + `st.stats`).</summary>
        public Stats Stats = new Stats();
        /// <summary>The ledger's opening stock, recorded once at new-game time (reference `f.ledger`).</summary>
        public LedgerOpening Ledger;

        partial void VisitInventory(IStateVisitor v)
        {
            v.Object("stats", ref Stats, () => new Stats());
            v.Object("ledger", ref Ledger, () => new LedgerOpening());
            v.Object("hand", ref Hand, () => new HandState());
        }
    }
}
