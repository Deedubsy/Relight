using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

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
        /// The circuit an authored site is on, or null when it is on none. Two kinds of site answer here (court D55):
        /// a <see cref="SiteKind.Substation"/> lot, once a placed node links to it, and a streetlight, which is billed
        /// to its nearest substation's circuit (reference campaignPower.ts:51 bills the block's substation for its
        /// lights). A region with no substation sites falls back to the light joining the nearest pole in reach.
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
    /// <item>Authored substations (campaignPower.ts:28) are reach nodes again under court D55, but without the
    ///       block economy (U-D-32): a <see cref="SiteKind.Substation"/> site is a node with the substation reach, no
    ///       supply and no draw of its own. Placed nodes cable to it and through it, and every authored streetlight is
    ///       billed to its nearest substation's circuit. Unlike the reference, a site never owns a consuming machine:
    ///       consumers join placed nodes only, so a machine standing beside an unconnected substation is not
    ///       captured by a dead circuit when a pole in reach would have powered it.</item>
    /// <item>No turbine hall or plant supply (:49-50) and no core/radio/encounter demand (:52-55): those are
    ///       campaign state that arrives with C-05/C-09. A circuit's load is therefore shared among its generators
    ///       alone, which is the reference's formula with the turbine and plant terms zero. The streetlight demand
    ///       of :51 IS carried (C-11), billed to the circuit of each light's nearest substation site.</item>
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
            public int Machine;      // index into st.Machines; -1 for an authored substation site
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

        /// <summary>
        /// The region's authored substation lots in export order, cached per <see cref="WorldSites"/> instance (the
        /// sites are immutable). Empty on the synthetic map and in any fixture without them.
        /// </summary>
        public static IReadOnlyList<SiteRecord> SubstationSites(SimContext ctx)
        {
            var sites = ctx == null ? null : ctx.Sites;
            if (sites == null || sites.Count == 0) return NoSites;
            if (SubstationCache.TryGetValue(sites, out var cached)) return cached;
            var list = new List<SiteRecord>(sites.OfKind(SiteKind.Substation));
            var arr = list.Count == 0 ? NoSites : list.ToArray();
            SubstationCache.Add(sites, arr);
            return arr;
        }

        private static readonly SiteRecord[] NoSites = new SiteRecord[0];
        private static readonly ConditionalWeakTable<WorldSites, SiteRecord[]> SubstationCache =
            new ConditionalWeakTable<WorldSites, SiteRecord[]>();

        /// <summary>An authored substation's reach (reference <c>nodeReach('substation')</c> = POLE_REACH, 8).</summary>
        public static double SiteReach(GameData d) =>
            d?.Power != null && d.Power.SubstationReachTiles > 0 ? d.Power.SubstationReachTiles : 8;

        /// <summary>
        /// The substation site an authored streetlight belongs to: the substation of the district the light's
        /// centre is in (<see cref="Districts"/> owns the rule: nearest by centre distance, ties to the earlier
        /// site in export order). Null when the region has no substation sites.
        /// </summary>
        public static SiteRecord SubstationOf(SimContext ctx, SiteRecord light)
        {
            if (ctx == null || light == null) return null;
            var lc = light.Centre;
            return Districts.SubstationAt(ctx, lc.X, lc.Y);
        }

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

            // Court D55 — the authored substation lots, after the placed nodes so a circuit's root (its Id) is still
            // a placed machine wherever one exists.
            var subs = SubstationSites(ctx);
            var firstSite = nodes.Count;
            var siteReach = SiteReach(d);
            for (var i = 0; i < subs.Count; i++)
            {
                var s = subs[i];
                nodes.Add(new Node
                {
                    Machine = -1,
                    X = s.X, Y = s.Y, W = s.W, H = s.H,
                    Cx = s.X + s.W / 2.0, Cy = s.Y + s.H / 2.0,
                    Reach = siteReach,
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
            // Placed nodes only: a substation site cables but never owns a machine (see the class remarks). Also the
            // streetlights' rule on a region with no substation sites, so there is no second ledger.
            PowerCircuit Join(int rx, int ry, int rw, int rh)
            {
                var nearest = -1;
                var best = double.PositiveInfinity;
                for (var k = 0; k < firstSite; k++)
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

            // Court D55 — a substation site is on a circuit once a placed node is in its group. A lot no pole has
            // reached stays off the grid entirely: no circuit, no phantom demand for its lights.
            if (subs.Count > 0)
            {
                var placedRoot = new HashSet<int>();
                for (var k = 0; k < firstSite; k++) placedRoot.Add(Root(k));
                for (var i = 0; i < subs.Count; i++)
                {
                    var root = Root(firstSite + i);
                    if (placedRoot.Contains(root)) grid.AttachSite(subs[i].Id, Circuit(root));
                }
            }

            // campaignPower.ts:51 — the authored streetlights draw before the load is worked out, and they draw
            // whether or not they happen to be lit, exactly as the reference bills `lights.length * lightKw` to the
            // block's substation. Court D55: each light is billed to its nearest substation site's circuit, and is
            // unattached (and dark) while that substation is on none. C-11 reads the resulting throttle back as "the
            // streetlights on this circuit are on" (flow.ts:620 `subPowered`), so there is no circular dependency
            // and no second demand ledger.
            var lights = StreetLights.Sites(ctx);
            if (lights.Count > 0)
            {
                var lightKw = StreetLights.Kw(d);
                for (var i = 0; i < lights.Count; i++)
                {
                    var s = lights[i];
                    var c = subs.Count > 0 ? grid.OfSite(SubstationOf(ctx, s)?.Id) : Join(s.X, s.Y, s.W, s.H);
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
