using System.Globalization;

namespace Relight.Sim.UI
{
    /// <summary>
    /// The ONE producer of a power alert anywhere in the UI (UI_AND_ONBOARDING.md §9 defect U-3: the reference had
    /// several and they disagreed). Nothing else in the HUD, the drawers or the map may raise one — they read
    /// <see cref="Text"/> and <see cref="Key"/> from here.
    ///
    /// Two rules, both from §8 row "Power outage / connection messages":
    /// <list type="bullet">
    /// <item>The alert stands only while a circuit that wants power has none, from
    ///       <see cref="PowerQueries.Network"/> — the same summary the status strip prints, so the strip and the
    ///       alert can never disagree.</item>
    /// <item>REL-7 (INT-03): the brownout notice, the strip's " · short" and the low-fuel warning each read the WORST
    ///       circuit, never the whole map, and the two rows name the place they are about. An average hid a turret
    ///       circuit with half a minute of coal behind a generous one elsewhere.</item>
    /// <item>"No outage toast at game start." A player who has never had power is not suffering an outage, so the
    ///       alert is suppressed until the player has ever run a generator. There is no saved flag for that and the
    ///       brief forbids adding one, so the fact is DERIVED (see <see cref="EverHadPower"/>): a generator standing
    ///       on the map now, or a fuelling/burn record in <see cref="Stats"/>, which are both already saved.</item>
    /// </list>
    /// </summary>
    public sealed class PowerAlertSource
    {
        /// <summary>The inbox key the alert always uses, so a standing outage updates one row (§8).</summary>
        public const string AlertKey = "power.outage";

        /// <summary>Reference campaignAlerts.ts:17 title, with the port's single site name.</summary>
        public const string TitlePrefix = "No power";

        // GP-W6. The strip's wording below is the wording the PLAYER has been reading since 2026-09-18, when
        // HudController began overwriting this class's text with a formula of its own ("Power 300 kW · Need 40").
        // Two formulas for one figure is the defect this class exists to prevent, and the tests here were pinning
        // sentences nobody could see. The on-screen wording moved in, the HUD's copy is gone, and this is again the
        // only place the strip's text is made.

        /// <summary>The strip's text when no generator is on any circuit.</summary>
        public const string NoSourceText = "Power · no Generator linked";

        /// <summary>The strip's text when nothing is on a circuit at all.</summary>
        public const string DisconnectedText = "Power · disconnected";

        /// <summary>The strip's text when generators are linked and every one of them is empty.</summary>
        public const string NoFuelText = "Power · no fuel";

        /// <summary>What the strip adds while demand is above generation: the standing mark of a shortage.</summary>
        public const string ShortSuffix = " · short";

        /// <summary>The inbox key of the low-fuel warning.</summary>
        public const string LowFuelKey = "power.fuel-low";

        /// <summary>
        /// Warn when the connected fuel will not last this long at the present load. Two minutes is long enough to
        /// walk to a Generator with Coal and short enough that the row is not standing all evening.
        /// </summary>
        public const double LowFuelSeconds = 120;

        /// <summary>The place name the outage alert uses. Set once from the loaded region; defaults to "Home".</summary>
        public string PlaceName = "Home";

        /// <summary>The strip's live text and the standing alert's text, both from one <see cref="PowerSummary"/>.</summary>
        public string StripText { get; private set; } = NoSourceText;

        /// <summary>"" when there is no outage; else "No power · {place}".</summary>
        public string AlertText { get; private set; } = "";

        /// <summary>The inbox key of the brownout notice (L-02, ALWAYS_DARK_SPEC §5.7).</summary>
        public const string BrownoutKey = "power.brownout";

        /// <summary>
        /// §5.7 "a brownout now costs twice": the notice names both consequences — the lights shrink, and the
        /// turrets slow and lose the reach those lights gave them. <see cref="BrownoutSentence"/> puts the place first.
        /// </summary>
        public const string BrownoutLine = "lights are shrinking, and turrets fire slower and see less far";

        /// <summary>
        /// "" unless a light, an authored streetlight or a powered turret stands on a circuit that is supplied but
        /// short; else the sentence for the worst such circuit, naming where its light or turret stands. A brownout
        /// that only slows a Foundry is the machine's own "running slowly" line and raises nothing here.
        /// </summary>
        public string BrownoutText { get; private set; } = "";

        /// <summary>The summary the last <see cref="Refresh"/> read, so the caller need not ask the grid twice.</summary>
        public PowerSummary Summary { get; private set; }

        /// <summary>True while any circuit's generation is above zero and below its demand: machines are being slowed.</summary>
        public bool Short { get; private set; }

        /// <summary>True while no power is being generated at all. The strip's meter and colour read this.</summary>
        public bool Off { get; private set; } = true;

        /// <summary>0..1 of demand that is actually delivered, for the strip's meter; 1 when nothing is asked for.</summary>
        public double Delivered { get; private set; }

        /// <summary>
        /// "about 4 min", or "" when nothing is burning. Always shown beside the word "estimate". REL-7: the time
        /// the FIRST circuit to run dry has left, so the strip and the low-fuel row always read the same figure.
        /// </summary>
        public string FuelText { get; private set; } = "";

        /// <summary>
        /// "" unless some circuit with a load will run out of fuel within <see cref="LowFuelSeconds"/>. The sentence
        /// names where that circuit's generator stands and is otherwise the same for as long as it stands — it does
        /// not count down — so it is posted once and never churns (U-D-55). It ends on the reserve worth keeping:
        /// what one full slot is worth, from the same formula.
        /// </summary>
        public string LowFuelText { get; private set; } = "";

        /// <summary>Recompute both strings. Returns true while an outage stands.</summary>
        public bool Refresh(SimContext ctx, SimState st)
        {
            if (ctx == null || st == null)
            {
                StripText = NoSourceText;
                AlertText = "";
                BrownoutText = "";
                FuelText = "";
                LowFuelText = "";
                Short = false;
                Off = true;
                Delivered = 0;
                return false;
            }

            // The strip's figures stay the grid totals; every warning below reads one circuit at a time (REL-7).
            var n = PowerQueries.Network(ctx, st);
            var grid = PowerGrid.Of(ctx, st);
            Summary = n;
            Off = n.SupplyKw <= 0;
            Delivered = Off ? 0 : n.DemandKw > 0 ? System.Math.Min(1, n.LoadKw / n.DemandKw) : 1;

            Short = false;
            PowerCircuit first = null;
            var firstSeconds = double.PositiveInfinity;
            for (var i = 0; i < grid.Circuits.Count; i++)
            {
                var c = PowerQueries.CircuitSummary(ctx.Data, grid.Circuits[i]);
                if (IsShort(c)) Short = true;
                // Low fuel: the circuit with a load whose fuel runs out first (ties to the earlier circuit).
                if (c.LoadKw > 0 && c.FuelSeconds < firstSeconds) { firstSeconds = c.FuelSeconds; first = grid.Circuits[i]; }
            }
            StripText = Strip(n, Short);

            FuelText = first != null ? PowerQueries.FuelTimeText(firstSeconds) : "";
            LowFuelText = first != null && firstSeconds < LowFuelSeconds
                ? LowFuelLine(ctx.Data, GeneratorPlace(ctx, st, first)) : "";

            BrownoutText = WorstDefenceBrownout(ctx, st, grid, out var bx, out var by)
                ? BrownoutSentence(DefenceAlertSource.NameAt(ctx, bx, by, PlaceName)) : "";

            // An outage is "a circuit that wants power and is not getting it". Demand with no supply is the whole of
            // it: a circuit with supply below demand is a brownout, which the machine's own "running slowly — not
            // enough power" line already says, and §8 reserves the persistent strip for a real loss of power.
            var outage = n.DemandKw > 0 && n.SupplyKw <= 0;
            AlertText = outage && EverHadPower(ctx, st) ? TitlePrefix + " · " + PlaceName : "";
            return AlertText.Length > 0;
        }

        /// <summary>
        /// REL-7 (INT-03). True when a light, a powered turret or an authored streetlight is on a circuit throttled
        /// below 1 but above 0; <paramref name="x"/>,<paramref name="y"/> is where the one on the most starved such
        /// circuit stands (ties to the first found: placed machines in order, then streetlights in export order).
        /// Streetlights count because a district lit only by them goes dark in a brownout just the same, and the
        /// old machine-only walk never saw it.
        /// </summary>
        public static bool WorstDefenceBrownout(SimContext ctx, SimState st, PowerNetwork grid, out double x, out double y)
        {
            var d = ctx.Data;
            var worst = 1.0;
            x = y = 0;
            for (var i = 0; i < st.Machines.Count; i++)
            {
                var m = st.Machines[i];
                var defence = LightSources.IsLightMachine(d, m) || (d.TryTurret(m.Kind, out var t) && t.PowerKw > 0);
                if (!defence) continue;
                var throttle = PowerQueries.Throttle(ctx, st, m.Id);
                if (throttle <= 0 || throttle >= worst) continue;
                worst = throttle;
                var (w, h) = m.Dimensions;
                x = m.X + w / 2.0;
                y = m.Y + h / 2.0;
            }
            var lights = StreetLights.Sites(ctx);
            for (var i = 0; i < lights.Count; i++)
            {
                var throttle = StreetLights.Throttle(grid, lights[i]);
                if (throttle <= 0 || throttle >= worst) continue;
                worst = throttle;
                var c = lights[i].Centre;
                x = c.X;
                y = c.Y;
            }
            return worst < 1;
        }

        /// <summary>The brownout notice for one place: "Low power at Ironworks · lights are shrinking, ...".</summary>
        public static string BrownoutSentence(string place) => "Low power at " + place + " · " + BrownoutLine;

        /// <summary>Where a circuit's first fuelled generator stands, by the defence row's label rule: where the coal goes.</summary>
        private string GeneratorPlace(SimContext ctx, SimState st, PowerCircuit c)
        {
            var m = c.Generators.Count > 0 ? st.MachineById(c.Generators[0]) : null;
            if (m == null) return string.IsNullOrEmpty(PlaceName) ? "Home" : PlaceName;
            var (w, h) = m.Dimensions;
            return DefenceAlertSource.NameAt(ctx, m.X + w / 2.0, m.Y + h / 2.0, PlaceName);
        }

        /// <summary>Generation above zero and below demand.</summary>
        public static bool IsShort(PowerSummary n) => n.SupplyKw > 0 && n.DemandKw > n.SupplyKw;

        /// <summary>The strip's text, as a pure function so a test can drive every branch.</summary>
        public static string Strip(PowerSummary n) => Strip(n, IsShort(n));

        /// <summary>
        /// The strip's text with the shortage decided by the caller: REL-7 marks " · short" when ANY circuit is
        /// supplied and short, which the grid totals can hide behind a surplus on another circuit.
        /// </summary>
        public static string Strip(PowerSummary n, bool shortSomewhere)
        {
            if (n.Circuits <= 0) return DisconnectedText;
            if (n.RatedGenerators <= 0) return NoSourceText;
            if (n.SupplyKw <= 0) return NoFuelText;
            return string.Format(CultureInfo.InvariantCulture, "Power {0:0} kW · Need {1:0}{2}",
                n.SupplyKw, n.DemandKw, shortSomewhere ? ShortSuffix : "");
        }

        /// <summary>
        /// The low-fuel sentence for the place whose generator is running dry. Both times in it come from
        /// <see cref="PowerQueries.FuelTimeText"/>.
        /// </summary>
        public static string LowFuelLine(GameData d, string place) =>
            "Fuel low at " + place + " · under " + PowerQueries.FuelTimeText(LowFuelSeconds).Replace("about ", "")
            + " left at this load (estimate). A full slot of "
            + MachineInventory.GeneratorFuelCap(d).ToString("0", CultureInfo.InvariantCulture)
            + " runs a Generator " + PowerQueries.FuelTimeText(PowerQueries.FullSlotSeconds(d))
            + " flat out — keep it fed by belt.";

        /// <summary>
        /// "Has the player ever had power?" — the fact the brief asks be FOUND, not added.
        ///
        /// Three already-saved witnesses, any one of which is enough:
        /// a power source standing on the map now (<see cref="PowerGrid.IsSource"/> over
        /// <see cref="SimState.Machines"/>), a generator the player has fuelled by hand
        /// (<see cref="Stats.GenFed"/>), or coal a generator has burned (<see cref="Stats.CoalBurned"/>).
        /// The first covers "built it and it is still there", the second and third cover "built it, ran it and lost
        /// it", which is exactly the case the outage alert exists for. None of them is a new flag.
        ///
        /// The report asks the <c>Stats</c>/placement owner for a first-class "generators placed" counter; until it
        /// exists, this derivation is the single answer and every caller uses it.
        /// </summary>
        public static bool EverHadPower(SimContext ctx, SimState st)
        {
            if (ctx == null || st == null) return false;
            var s = st.Stats;
            if (s != null && (s.GenFed > 0 || s.CoalBurned > 0)) return true;
            for (var i = 0; i < st.Machines.Count; i++)
                if (PowerGrid.IsSource(ctx.Data, st.Machines[i])) return true;
            return false;
        }
    }
}
