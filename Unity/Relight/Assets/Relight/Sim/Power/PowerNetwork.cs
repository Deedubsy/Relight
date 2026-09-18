using System;
using System.Collections.Generic;

namespace Relight.Sim
{
    /// <summary>
    /// One electrically connected group of machines (reference campaignPower.ts:11 <c>Circuit</c>).
    /// Supply, demand and load are kW; <see cref="Throttle"/> is the fraction of a second of work every drawing
    /// machine on the circuit gets (reference campaignPower.ts:57).
    /// </summary>
    public sealed class PowerCircuit
    {
        /// <summary>Stable within one build: the index of the circuit's root reach node.</summary>
        public int Id;
        public double Supply;
        public double Demand;
        public double Load;
        public double Throttle;
        /// <summary>Generators with fuel, in machine order — the reference's <c>c.generators</c>.</summary>
        public readonly List<int> Generators = new List<int>();
        /// <summary>Generators on the circuit whether fuelled or not (the reference's <c>c.rated</c>, counted).</summary>
        public int RatedGenerators;
    }

    /// <summary>
    /// The whole grid for one tick (reference campaignPower.ts:12 <c>CampaignGrid</c>), derived state rebuilt by
    /// <see cref="PowerGrid.Of"/> whenever the revision, the tick or a generator's fuel state changes.
    /// </summary>
    public sealed class PowerNetwork
    {
        public readonly List<PowerCircuit> Circuits = new List<PowerCircuit>();
        private readonly Dictionary<int, PowerCircuit> _of = new Dictionary<int, PowerCircuit>();
        private readonly Dictionary<int, double> _share = new Dictionary<int, double>();
        private readonly Dictionary<string, PowerCircuit> _ofSite = new Dictionary<string, PowerCircuit>(StringComparer.Ordinal);

        /// <summary>Grid totals in kW (reference campaignPower.ts:57 <c>grid.supply/demand/load</c>).</summary>
        public double Supply;
        public double Demand;
        public double Load;
        /// <summary>Fuel units left in every connected generator, for the "fuel seconds" estimate.</summary>
        public double FuelUnits;

        internal PowerCircuit Attach(int machineId, PowerCircuit c) { _of[machineId] = c; return c; }
        internal void SetShare(int generatorId, double kw) { _share[generatorId] = kw; }
        internal PowerCircuit AttachSite(string siteId, PowerCircuit c) { _ofSite[siteId] = c; return c; }

        /// <summary>The circuit a machine belongs to, or null when nothing in reach connects it.</summary>
        public PowerCircuit Of(int machineId) => _of.TryGetValue(machineId, out var c) ? c : null;

        /// <summary>A generator's share of its circuit's load in kW (reference campaignPower.ts:57 <c>grid.generation</c>).</summary>
        public double Share(int generatorId) => _share.TryGetValue(generatorId, out var kw) ? kw : 0;

        /// <summary>
        /// The circuit an authored non-machine draw joined, or null when nothing in reach connects it. Today that is
        /// C-11's streetlights (reference campaignPower.ts:51 bills the block's substation for them); the port bills
        /// the same circuit every consuming machine on that pole bills.
        /// </summary>
        public PowerCircuit OfSite(string siteId) => siteId != null && _ofSite.TryGetValue(siteId, out var c) ? c : null;
    }

    /// <summary>
    /// The power network, ported from reference campaignPower.ts <c>campaignGrid</c> (21-59).
    ///
    /// Ported exactly: the reach-node set and its union-find (:29-34), the GP-POWER-FIX link rule
    /// <c>nodesLinked</c> (:16-19) measured to footprints, the nearest-reach-node rule for a consumer (:41-44), the
    /// supply/demand accumulation (:46-47) and the per-circuit load, throttle and generator share (:57).
    ///
    /// Deliberate differences, all recorded in the wave-1 W-B report:
    /// <list type="bullet">
    /// <item>No authored per-district substations (campaignPower.ts:28): the block/district economy is retired
    ///       (U-D-32), so every reach node is a placed machine.</item>
    /// <item>No turbine hall or plant supply (:49-50) and no core/radio/encounter demand (:52-55): those are
    ///       campaign state that arrives with C-05/C-09. A circuit's load is therefore shared among its generators
    ///       alone, which is the reference's formula with the turbine and plant terms zero. The streetlight demand
    ///       of :51 IS carried (C-11), billed to the circuit that reaches each light instead of to its block.</item>
    /// <item>No defence damage or disabled blocks (:22, :46): <c>Machine.Hp</c> is C-04's.</item>
    /// <item>Node kinds come from the data, not a kind list: anything with <c>MachineSpec.ReachTiles &gt; 0</c> is a
    ///       reach node and anything with <c>PowerKw &lt; 0</c> is a source (the reference's pole/bigpole/substation
    ///       and generator).</item>
    /// </list>
    /// </summary>
    public static class PowerGrid
    {
        private struct Node
        {
            public int Machine;      // index into st.Machines
            public double Cx, Cy;    // footprint centre
            public int X, Y, W, H;
            public double Reach;
        }

        /// <summary>The grid for the current tick, built once and cached on <see cref="PowerState"/> (S-25 struct key).</summary>
        public static PowerNetwork Of(SimContext ctx, SimState st)
        {
            var key = KeyOf(ctx, st);
            if (st.Power.TryCached(key, out var cached)) return cached;
            var grid = Build(ctx, st);
            st.Power.Cache(key, grid);
            return grid;
        }

        private static NetworkKey KeyOf(SimContext ctx, SimState st)
        {
            // Reference campaignPower.ts:24 keys the cache on the revision, the tick and each generator's "has fuel"
            // bit; the fold below is the same information as a value.
            var fuel = 17;
            for (var i = 0; i < st.Machines.Count; i++)
            {
                var m = st.Machines[i];
                if (!IsSource(ctx.Data, m)) continue;
                fuel = unchecked(fuel * 31 + m.Id * 2 + (FuelUnits(m) > 0 ? 1 : 0));
            }
            return new NetworkKey(st.Rev, st.Tick, st.Machines.Count, fuel);
        }

        /// <summary>Coal plus refined fuel in a generator (reference flow.ts:1046 <c>(m.inv.coal??0)+(m.inv.fuel??0)</c>).</summary>
        public static double FuelUnits(Machine m) => m.Inv[ItemId.Coal] + m.Inv[ItemId.Fuel];

        /// <summary>A machine that supplies power: <c>MachineSpec.PowerKw</c> is negative (the Generator's -300).</summary>
        public static bool IsSource(GameData d, Machine m) => d.TryMachine(m.Kind, out var s) && s.PowerKw < 0;

        /// <summary>Supply in kW when fuelled (reference recipes.ts GENERATOR_KW 300).</summary>
        public static double SourceKw(GameData d, Machine m) =>
            d.TryMachine(m.Kind, out var s) && s.PowerKw < 0 ? -s.PowerKw
            : d.Power != null ? d.Power.GeneratorKw : 300;

        /// <summary>Draw in kW (reference flow.ts:232 MACHINE_KW); 0 for belts, poles, chests and the Depot.</summary>
        public static double DemandKw(GameData d, Machine m) =>
            d.TryMachine(m.Kind, out var s) && s.PowerKw > 0 ? s.PowerKw : 0;

        /// <summary>Network reach in tiles (reference campaignPower.ts:20 <c>nodeReach</c>), from the machine data.</summary>
        public static double ReachOf(GameData d, Machine m) => d.TryMachine(m.Kind, out var s) ? s.ReachTiles : 0;

        /// <summary>Reference campaignPower.ts:16 <c>nodesLinked</c> (GP-POWER-FIX): either node's centre within its own reach of the other's footprint.</summary>
        private static bool Linked(in Node a, in Node b) =>
            NodesLinked(a.X, a.Y, a.W, a.H, a.Reach, b.X, b.Y, b.W, b.H, b.Reach);

        /// <summary>
        /// The same rule in plain rect terms so <see cref="PowerLinks"/> can answer "what would this ghost cable to"
        /// from the one implementation instead of a second, possibly disagreeing, copy (GP-POWER-FIX preview).
        /// </summary>
        public static bool NodesLinked(int ax, int ay, int aw, int ah, double aReach,
            int bx, int by, int bw, int bh, double bReach) =>
            (aReach > 0 && Reach.DistToRect(ax + aw / 2.0, ay + ah / 2.0, bx, by, bw, bh) <= aReach) ||
            (bReach > 0 && Reach.DistToRect(bx + bw / 2.0, by + bh / 2.0, ax, ay, aw, ah) <= bReach);

        private static PowerNetwork Build(SimContext ctx, SimState st)
        {
            var d = ctx.Data;
            var grid = new PowerNetwork();
            var nodes = new List<Node>();
            var nodeOfMachine = new Dictionary<int, int>();

            // campaignPower.ts:29 — every pole, big pole, substation and generator is a node; a generator's reach is 0,
            // so it joins a network but never extends one.
            for (var i = 0; i < st.Machines.Count; i++)
            {
                var m = st.Machines[i];
                // GP-W5: a wrecked pole carries nothing. This is the mechanism behind the Breaker's role —
                // "pushes through light, pressure on production connections" — because breaking one big pole
                // severs the circuit behind it, and the machines it fed read as "no power" until it is repaired.
                if (TurretRules.Wrecked(d, st, m)) continue;
                var reach = ReachOf(d, m);
                if (reach <= 0 && !IsSource(d, m)) continue;
                var r = m.Rect;
                nodeOfMachine[m.Id] = nodes.Count;
                nodes.Add(new Node
                {
                    Machine = i,
                    X = r.X, Y = r.Y, W = r.W, H = r.H,
                    Cx = r.X + r.W / 2.0, Cy = r.Y + r.H / 2.0,
                    Reach = reach,
                });
            }

            // campaignPower.ts:30-34 — union-find over the nodes, in the same nested order, so the roots match.
            var parent = new int[nodes.Count];
            for (var i = 0; i < parent.Length; i++) parent[i] = i;
            int Root(int i) { while (parent[i] != i) { parent[i] = parent[parent[i]]; i = parent[i]; } return i; }
            for (var i = 0; i < nodes.Count; i++)
                for (var j = 0; j < i; j++)
                {
                    var a = nodes[i];
                    var b = nodes[j];
                    if (Linked(in a, in b)) parent[Root(i)] = Root(j);
                }

            var byRoot = new Dictionary<int, PowerCircuit>();
            PowerCircuit Circuit(int root)
            {
                if (byRoot.TryGetValue(root, out var c)) return c;
                c = new PowerCircuit { Id = root };
                byRoot[root] = c;
                grid.Circuits.Add(c);
                return c;
            }

            // campaignPower.ts:41-44 — the NEAREST reach node whose reach covers a footprint owns that consumer.
            // Extracted so the authored streetlights below join the very same way (no second ledger).
            PowerCircuit Join(int rx, int ry, int rw, int rh)
            {
                var nearest = -1;
                var best = double.PositiveInfinity;
                for (var k = 0; k < nodes.Count; k++)
                {
                    var p = nodes[k];
                    if (p.Reach <= 0) continue;
                    var dist = Reach.DistToRect(p.Cx, p.Cy, rx, ry, rw, rh);
                    if (dist <= p.Reach && dist < best) { best = dist; nearest = k; }
                }
                return nearest < 0 ? null : Circuit(Root(nearest));   // campaignPower.ts:45 `if(!c)continue`
            }

            for (var i = 0; i < st.Machines.Count; i++)
            {
                var m = st.Machines[i];
                // GP-W5: a wreck neither supplies nor draws. It is not attached to a circuit either, so
                // PowerQueries.Connected reports it honestly as disconnected rather than as a healthy consumer
                // dragging its circuit's throttle down while it does no work.
                if (TurretRules.Wrecked(d, st, m)) continue;
                PowerCircuit c;
                if (nodeOfMachine.TryGetValue(m.Id, out var ni)) c = Circuit(Root(ni));
                else
                {
                    var r = m.Rect;
                    c = Join(r.X, r.Y, r.W, r.H);
                    if (c == null) continue;
                }
                grid.Attach(m.Id, c);

                if (IsSource(d, m))
                {
                    c.RatedGenerators++;
                    var fuel = FuelUnits(m);
                    if (fuel > 0)
                    {
                        c.Supply += SourceKw(d, m);      // campaignPower.ts:46
                        c.Generators.Add(m.Id);
                        grid.FuelUnits += fuel;
                    }
                }
                else c.Demand += DemandKw(d, m);          // campaignPower.ts:47
            }

            // campaignPower.ts:51 — the authored streetlights draw before the load is worked out, and they draw
            // whether or not they happen to be lit, exactly as the reference bills `lights.length * lightKw` to the
            // block. C-11 reads the resulting throttle back as "the streetlights on this circuit are on"
            // (flow.ts:620 `subPowered`), so there is no circular dependency and no second demand ledger.
            var lights = StreetLights.Sites(ctx);
            if (lights.Count > 0)
            {
                var lightKw = StreetLights.Kw(d);
                for (var i = 0; i < lights.Count; i++)
                {
                    var s = lights[i];
                    var c = Join(s.X, s.Y, s.W, s.H);
                    if (c == null) continue;
                    c.Demand += lightKw;
                    grid.AttachSite(s.Id, c);
                }
            }

            // campaignPower.ts:57 — load, throttle and each generator's equal share of what the circuit delivers.
            for (var i = 0; i < grid.Circuits.Count; i++)
            {
                var c = grid.Circuits[i];
                c.Load = Math.Min(c.Supply, c.Demand);
                c.Throttle = c.Supply > 0 ? Math.Min(1, c.Supply / Math.Max(1, c.Demand)) : 0;
                grid.Supply += c.Supply;
                grid.Demand += c.Demand;
                grid.Load += c.Load;
                for (var g = 0; g < c.Generators.Count; g++) grid.SetShare(c.Generators[g], c.Load / c.Generators.Count);
            }
            return grid;
        }
    }
}
