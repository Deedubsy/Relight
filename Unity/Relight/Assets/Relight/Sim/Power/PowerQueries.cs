using System;

namespace Relight.Sim
{
    /// <summary>What the HUD shows about the grid as a whole (C-07). kW everywhere; seconds for the fuel estimate.</summary>
    public readonly struct PowerSummary
    {
        /// <summary>Total draw of every connected machine that wants to run.</summary>
        public readonly double DemandKw;
        /// <summary>Total output of every connected generator that still has fuel.</summary>
        public readonly double SupplyKw;
        /// <summary>What is actually delivered: <c>min(supply, demand)</c> per circuit, summed.</summary>
        public readonly double LoadKw;
        /// <summary>Generators with fuel on a circuit.</summary>
        public readonly int Generators;
        /// <summary>Generators connected at all, fuelled or not.</summary>
        public readonly int RatedGenerators;
        /// <summary>Separate circuits with at least one machine.</summary>
        public readonly int Circuits;
        /// <summary>How long the connected fuel lasts at the current load; <see cref="double.PositiveInfinity"/> when nothing is drawing.</summary>
        public readonly double FuelSeconds;

        public PowerSummary(double demandKw, double supplyKw, double loadKw, int generators, int ratedGenerators,
            int circuits, double fuelSeconds)
        {
            DemandKw = demandKw; SupplyKw = supplyKw; LoadKw = loadKw;
            Generators = generators; RatedGenerators = ratedGenerators; Circuits = circuits; FuelSeconds = fuelSeconds;
        }
    }

    /// <summary>
    /// Read-only power questions for presentation, turrets (C-04) and the machine phase (B-07).
    /// Everything here derives from <see cref="PowerGrid.Of"/>, which is cached per tick — calling these in a
    /// per-frame path rebuilds nothing.
    /// </summary>
    public static class PowerQueries
    {
        /// <summary>Power feedback independent of production stalls, shared by hover and inspection.</summary>
        public static string Description(SimContext ctx, SimState st, int id)
        {
            var m = st.MachineById(id);
            if (m == null) return "";
            var source = PowerGrid.IsSource(ctx.Data, m);
            var node = PowerGrid.ReachOf(ctx.Data, m) > 0;
            if (!source && !node && PowerGrid.DemandKw(ctx.Data, m) <= 0) return "No power required";
            var c = Circuit(ctx, st, id);
            if (c == null) return "Power: disconnected — place a pole nearby";
            if (source)
            {
                if (FuelUnits(st, id) <= 0) return "Generator: out of fuel";
                var line = $"Generator: {GeneratorShareKw(ctx, st, id):0.#} kW supplied · {c.Supply:0} kW available";
                var left = GeneratorFuelSeconds(ctx, st, id);
                return double.IsPositiveInfinity(left) ? line + " · nothing is drawing on its fuel"
                    : line + " · fuel for " + FuelTimeText(left) + " at this load (estimate)";
            }
            if (c.Supply <= 0) return "Power: connected · no supply — fuel a connected generator";
            if (c.Throttle < 1) return $"Power: connected · low supply ({c.Throttle:P0})";
            return node ? $"Power: connected · {c.Supply:0} kW available" : "Power: powered (100%)";
        }

        /// <summary>Grid totals (reference campaignPower.ts:57 <c>grid.supply/demand/load</c>).</summary>
        public static PowerSummary Network(SimContext ctx, SimState st)
        {
            var g = PowerGrid.Of(ctx, st);
            var gens = 0;
            var rated = 0;
            for (var i = 0; i < g.Circuits.Count; i++) { gens += g.Circuits[i].Generators.Count; rated += g.Circuits[i].RatedGenerators; }
            return new PowerSummary(g.Demand, g.Supply, g.Load, gens, rated, g.Circuits.Count,
                FuelSecondsAt(ctx.Data, g.FuelUnits, g.Load));
        }

        /// <summary>
        /// REL-7 (INT-03). One circuit's figures in the grid summary's shape (<see cref="PowerSummary.Circuits"/> is 1),
        /// so the HUD can ask "which circuit is in trouble?" rather than average a dying circuit into a healthy one.
        /// </summary>
        public static PowerSummary CircuitSummary(GameData d, PowerCircuit c) =>
            c == null ? default
                : new PowerSummary(c.Demand, c.Supply, c.Load, c.Generators.Count, c.RatedGenerators, 1,
                    FuelSecondsAt(d, c.FuelUnits, c.Load));

        /// <summary>
        /// GP-W6. The ONE fuel-time formula: the inverse of <see cref="PowerPhase"/>'s burn, where a load of
        /// <paramref name="kw"/> draws <c>kw / (CoalMj × 1000)</c> units a second. <see cref="double.PositiveInfinity"/>
        /// when nothing is drawing. It is an ESTIMATE by nature — the load moves — and every caller says so.
        /// </summary>
        public static double FuelSecondsAt(GameData d, double units, double kw)
        {
            var coalMj = d?.Power != null && d.Power.CoalMj > 0 ? d.Power.CoalMj : 4;
            return kw > 0 ? Math.Max(0, units) * (coalMj * 1000) / kw : double.PositiveInfinity;
        }

        /// <summary>One generator's fuel at its present share of its circuit's load.</summary>
        public static double GeneratorFuelSeconds(SimContext ctx, SimState st, int machineId) =>
            FuelSecondsAt(ctx.Data, FuelUnits(st, machineId), GeneratorShareKw(ctx, st, machineId));

        /// <summary>How long one full fuel slot runs one generator flat out — the practical reserve the warning teaches.</summary>
        public static double FullSlotSeconds(GameData d)
        {
            var kw = d != null && d.TryMachine("generator", out var s) && s.PowerKw < 0 ? -s.PowerKw
                : d?.Power != null && d.Power.GeneratorKw > 0 ? d.Power.GeneratorKw : 300;
            return FuelSecondsAt(d, MachineInventory.GeneratorFuelCap(d), kw);
        }

        /// <summary>
        /// A fuel time as the player reads it, deliberately coarse because it is an estimate: "under 10 s",
        /// "about 40 s", "about 4 min", "over an hour". The one wording every surface uses.
        /// </summary>
        public static string FuelTimeText(double seconds)
        {
            if (double.IsNaN(seconds) || seconds < 10) return "under 10 s";
            if (seconds < 90) return "about " + (Math.Round(seconds / 10) * 10).ToString("0", System.Globalization.CultureInfo.InvariantCulture) + " s";
            if (seconds < 3600) return "about " + Math.Round(seconds / 60).ToString("0", System.Globalization.CultureInfo.InvariantCulture) + " min";
            return "over an hour";
        }

        /// <summary>The machine's circuit, or null when nothing in reach connects it.</summary>
        public static PowerCircuit Circuit(SimContext ctx, SimState st, int machineId) =>
            PowerGrid.Of(ctx, st).Of(machineId);

        /// <summary>
        /// The fraction of a second of work a machine gets (reference campaignPower.ts:61 <c>machineThrottle</c>):
        /// a machine that draws nothing always runs at 1, everything else gets its circuit's throttle or 0.
        ///
        /// GP-W5 added the WRECK gate here, and only here. Miners, processors, inserters and turrets all ask this
        /// before they do a tick of work, so one test stops every one of them when a machine has been broken past
        /// its integrity — no per-loop wreck check, and no chance of one loop being taught the rule and another
        /// missing it. A wreck that draws nothing (a wall, a chest) is caught before the "draws nothing runs at 1"
        /// shortcut, so the answer does not depend on whether the thing happened to need power.
        /// </summary>
        public static double Throttle(SimContext ctx, SimState st, int machineId)
        {
            var m = st.MachineById(machineId);
            if (m == null) return 0;
            if (TurretRules.Wrecked(ctx.Data, st, m)) return 0;
            if (PowerGrid.DemandKw(ctx.Data, m) <= 0) return 1;
            var c = PowerGrid.Of(ctx, st).Of(machineId);
            return c?.Throttle ?? 0;
        }

        /// <summary>
        /// Whether the machine may run at all (reference flow.ts:630 <c>powered</c>: no draw, or a non-zero throttle).
        /// C-04's turrets hold fire when this is false.
        /// </summary>
        public static bool Supplied(SimContext ctx, SimState st, int machineId) => Throttle(ctx, st, machineId) > 0;

        /// <summary>True when the machine is connected to a circuit at all (a pole in reach), fuelled or not.</summary>
        public static bool Connected(SimContext ctx, SimState st, int machineId) =>
            PowerGrid.Of(ctx, st).Of(machineId) != null;

        /// <summary>A generator's share of its circuit's load in kW (reference campaignPower.ts:57).</summary>
        public static double GeneratorShareKw(SimContext ctx, SimState st, int machineId) =>
            PowerGrid.Of(ctx, st).Share(machineId);

        /// <summary>A generator's remaining coal + refined fuel.</summary>
        public static double FuelUnits(SimState st, int machineId)
        {
            var m = st.MachineById(machineId);
            return m == null ? 0 : PowerGrid.FuelUnits(m);
        }
    }
}
