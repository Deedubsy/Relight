namespace Relight.Sim
{
    /// <summary>
    /// C-11's sub-state, visited as <c>light</c>.
    ///
    /// It saves nothing, and that is the port of the reference rather than an omission: reference light.ts derives
    /// the whole light map from the machines, the authored kerb lights and the power network every time it is asked
    /// (<c>lightMask</c>, <c>litAt</c>), and keeps no light record in the save. The one thing the reference does keep
    /// — the broken/eaten light set and the contested block's switch-on sequence — belongs to the retired block
    /// economy (U-D-32) and has no port. So <see cref="Visit"/> writes an empty object: the member exists so the
    /// save schema is stable if C-11 ever does gain a saved fact, and an older v3 save without it simply keeps this
    /// fresh instance.
    ///
    /// What it does hold is the transient mask cache. It lives on the state (not in a static) so that two simulations
    /// in one process — a replay beside a live game — never share a buffer, and so that
    /// <see cref="LightQueries.LitAt(SimState,int,int)"/> can stay a pure function of the state, which is what C-06's
    /// enemy hesitation rule needs.
    /// </summary>
    public sealed class LightState : IVisitable
    {
        // ---- derived, never saved ---------------------------------------------------------------------------

        /// <summary>One byte per tile, <c>ty * W + tx</c>: 1 lit, 0 unlit. Null until the first build.</summary>
        internal byte[] Mask;
        internal int W;
        internal int H;
        internal bool Built;
        internal int BuiltRev;
        internal int BuiltFold;

        // The relief events' memory (LightPhase.Relief). Transient for the same reason the mask is: it is a
        // comparison with the previous tick, and the first tick after a load only takes the baseline.
        internal bool ReliefKnown;
        internal bool EngineerLit;
        internal double EnteredAt = double.NegativeInfinity;
        internal readonly System.Collections.Generic.HashSet<string> LiveDistricts =
            new System.Collections.Generic.HashSet<string>();

        /// <summary>
        /// REL-11: the districts that have said "connected" this session. A district in here that comes back after
        /// losing its supply is returning, not connecting. Transient on purpose (U-D-31): a load takes whatever is
        /// live as already announced, so nothing here is saved and the save schema is unchanged.
        /// </summary>
        internal readonly System.Collections.Generic.HashSet<string> AnnouncedDistricts =
            new System.Collections.Generic.HashSet<string>();

        /// <summary>How many times the mask has actually been stamped. A test hook; the reference has no counter.</summary>
        public int Builds { get; internal set; }

        /// <summary>True once the mask has been built for this world at least once.</summary>
        public bool HasMask => Built && Mask != null;

        public void Visit(IStateVisitor v)
        {
            // Nothing persistent. A load hands us a different world, so whatever we had cached is meaningless —
            // but ONLY a load. A save and a state hash walk this too, on the live state, and the combat slot reads
            // the mask before LightPhase can rebuild it: clearing it there left every tile unlit for the tick after
            // each autosave (turrets dropped lit targets, raiders stopped avoiding light), and forgetting the
            // relief memory made that tick a silent baseline that could swallow a district's connection.
            if (!v.IsReading) return;
            Built = false;
            Mask = null;
            W = 0;
            H = 0;
            ReliefKnown = false;
            EngineerLit = false;
            EnteredAt = double.NegativeInfinity;
            LiveDistricts.Clear();
            AnnouncedDistricts.Clear();
        }
    }

    public sealed partial class SimState
    {
        /// <summary>The light sub-state (C-11), visited as <c>light</c>.</summary>
        public LightState Light = new LightState();

        partial void VisitLight(IStateVisitor v) => v.Object("light", ref Light, () => new LightState());
    }
}
