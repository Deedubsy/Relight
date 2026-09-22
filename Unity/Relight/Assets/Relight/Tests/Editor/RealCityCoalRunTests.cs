using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using NUnit.Framework;
using Relight.Sim;
using Relight.World;
using Object = UnityEngine.Object;

namespace Relight.Authoring.Tests
{
    /// <summary>
    /// REL-88 (ECO-03a), "Measure how long Home's coal really lasts". Its accept line: "a recorded run states the
    /// minute coal runs out under 300 kW and 600 kW loads, and the earliest minute a player could reach more coal on
    /// today's build." This is the measuring step only: REL-27 owns solving the shortage.
    ///
    /// On the REAL CITY, as a new game builds it (<see cref="RealCityFixture"/>), the test writes one log with three
    /// parts:
    /// <list type="number">
    /// <item>a survey of every tile that yields Coal, grouped into patches, with the walking route to each from the
    /// engineer's new-game spot and the enemy camps beside it;</item>
    /// <item>where an Excavator can stand on Home's yard patch so that everything it digs is Coal (it digs any
    /// resource in the ring around it, and a Generator refuses anything but Coal and fuel);</item>
    /// <item>two measured runs, one at a fixed 300 kW and one at 600 kW, sampled every minute until the power is
    /// out.</item>
    /// </list>
    ///
    /// What the runs are, stated once so the log never overstates them. The line is the one the opening asks for
    /// ("Place 1 Excavator at a resource patch edge", then a Generator it feeds), placed free as REL-85's defence is
    /// placed; the load is idle Foundries and Assemblers, because a machine's draw does not depend on its work. The
    /// engineer stands in reach and does the rest by the player's own commands: <see cref="MineCommand"/> on the
    /// coal the Excavator cannot reach, <see cref="MachineTransferCommand"/> to keep every Generator at least half
    /// full, carrying Coal from one Generator to another when the Backpack is empty. Enemies are frozen, so a raid
    /// cannot break the line. The opening's own steps before the first burn are NOT played: minute 0 is the moment
    /// the first Coal goes into a Generator, and how long a player takes to get there is the owner session's
    /// question, not this run's.
    /// </summary>
    public sealed class RealCityCoalRunTests
    {
        private const int Seed = 7;
        private const string YardCoal = "opening-coal-v1";
        private const double CapMinutes = 200;
        /// <summary>Coal the engineer digs by hand for each Generator's first fuel.</summary>
        private const int SeedCoal = 10;
        /// <summary>How near a camp or a living alien must be to a patch to be counted as guarding it.</summary>
        private const double GuardTiles = 30;

        private readonly List<WorldGeometryAsset> _made = new List<WorldGeometryAsset>();

        [TearDown]
        public void Teardown()
        {
            foreach (var g in _made)
                if (g != null) Object.DestroyImmediate(g);
            _made.Clear();
        }

        private Simulation NewGame()
        {
            var ctx = RealCityFixture.Context(out var opening);
            _made.Add(opening);
            var sim = Simulation.NewGame(ctx, Seed);
            sim.State.OpeningResourceVersion = OpeningResourceLayout.Version;
            return sim;
        }

        /// <summary>The run's log: the project's ignored <c>Logs/</c> folder, never the evidence tree.</summary>
        private static string LogPath =>
            Path.GetFullPath(Path.Combine(UnityEngine.Application.dataPath, "..", "Logs", "REL-88-home-coal-run.txt"));

        private static string N(double v, string f = "0.#") => v.ToString(f, CultureInfo.InvariantCulture);

        private static bool CoalAt(SimContext ctx, SimState st, int x, int y, out double units) =>
            Mining.TryTile(ctx, st, x, y, out var item, out units) && item == ItemId.Coal;

        private static SiteRecord Site(SimContext ctx, string id)
        {
            foreach (var s in ctx.Sites.All)
                if (s.Id == id) return s;
            return null;
        }

        private static bool Overlaps(int ax, int ay, int aw, int ah, int bx, int by, int bw, int bh) =>
            ax < bx + bw && bx < ax + aw && ay < by + bh && by < ay + ah;

        private static double Dist(double ax, double ay, double bx, double by) =>
            Math.Sqrt((ax - bx) * (ax - bx) + (ay - by) * (ay - by));

        /// <summary>
        /// True when a node at this rect would cable to an authored substation: that circuit then bills the
        /// district's streetlights, and the run's load would no longer be the fixed figure it claims.
        /// </summary>
        private static bool LinksSubstation(SimContext ctx, int x, int y, int w, int h, double reach)
        {
            var subs = PowerGrid.SubstationSites(ctx);
            var siteReach = PowerGrid.SiteReach(ctx.Data);
            for (var i = 0; i < subs.Count; i++)
            {
                var s = subs[i];
                if (PowerGrid.NodesLinked(x, y, w, h, reach, s.X, s.Y, s.W, s.H, siteReach)) return true;
            }
            return false;
        }

        private static TilePoint Spawn(SimContext ctx, SimState st)
        {
            var sx = (int)Math.Floor(st.Engineer.Pos.X);
            var sy = (int)Math.Floor(st.Engineer.Pos.Y);
            Assert.That(Ground.NearestOpen(ctx, st, sx, sy, 6, out var p), Is.True, "the engineer's new-game spot has open ground");
            return p;
        }

        // ------------------------------------------------------------------ part 1: every coal source on the map

        private sealed class Patch
        {
            public readonly List<TilePoint> Tiles = new List<TilePoint>();
            public double Units, Cx, Cy, Straight, Walk = -1;
            public string Sites = "", District = "";
            public bool Yard, Routed;
            public int Camps, Defenders, Aliens, RouteCamps, RouteDefenders;
        }

        private static List<Patch> Survey(SimContext ctx, SimState st, TilePoint from, int routes)
        {
            var g = ctx.Geometry;
            var w = g.Width;
            var h = g.Height;
            var units = new double[w * h];
            for (var y = 0; y < h; y++)
                for (var x = 0; x < w; x++)
                    if (CoalAt(ctx, st, x, y, out var u)) units[y * w + x] = u;

            var yard = Site(ctx, YardCoal);
            var seen = new bool[w * h];
            var queue = new Queue<int>();
            var list = new List<Patch>();
            for (var i = 0; i < units.Length; i++)
            {
                if (units[i] <= 0 || seen[i]) continue;
                var p = new Patch();
                double sx = 0, sy = 0;
                seen[i] = true;
                queue.Enqueue(i);
                while (queue.Count > 0)
                {
                    var k = queue.Dequeue();
                    int kx = k % w, ky = k / w;
                    p.Tiles.Add(new TilePoint(kx, ky));
                    p.Units += units[k];
                    sx += kx + 0.5;
                    sy += ky + 0.5;
                    if (yard != null && yard.Contains(kx, ky)) p.Yard = true;
                    for (var dy = -1; dy <= 1; dy++)
                        for (var dx = -1; dx <= 1; dx++)
                        {
                            int nx = kx + dx, ny = ky + dy;
                            if (nx < 0 || ny < 0 || nx >= w || ny >= h) continue;
                            var j = ny * w + nx;
                            if (units[j] <= 0 || seen[j]) continue;
                            seen[j] = true;
                            queue.Enqueue(j);
                        }
                }
                p.Cx = sx / p.Tiles.Count;
                p.Cy = sy / p.Tiles.Count;
                p.Straight = Dist(from.X + 0.5, from.Y + 0.5, p.Cx, p.Cy);
                p.District = Districts.NameAt(ctx, p.Cx, p.Cy) ?? "(no district)";
                var names = new List<string>();
                foreach (var s in ctx.Sites.OfKind(SiteKind.Resource))
                {
                    if (names.Contains(s.Name)) continue;
                    foreach (var t in p.Tiles)
                        if (s.Contains(t.X, t.Y)) { names.Add(s.Name + " [" + s.Id + "]"); break; }
                }
                p.Sites = names.Count == 0 ? "(no authored site)" : string.Join(", ", names);
                foreach (var c in ctx.Sites.OfKind(SiteKind.Camp))
                    if (Dist(c.Centre.X, c.Centre.Y, p.Cx, p.Cy) <= GuardTiles) { p.Camps++; p.Defenders += c.Amount; }
                foreach (var e in st.Enemies.Actors)
                    if (Dist(e.Pos.X, e.Pos.Y, p.Cx, p.Cy) <= GuardTiles) p.Aliens++;
                list.Add(p);
            }
            list.Sort((a, b) => a.Straight.CompareTo(b.Straight));

            // Walking routes for the nearest patches and for every authored coal site: a full-map search each.
            for (var i = 0; i < list.Count; i++)
            {
                var p = list[i];
                if (i >= routes && p.Sites == "(no authored site)") continue;
                p.Routed = true;
                if (!Ground.NearestOpen(ctx, st, (int)p.Cx, (int)p.Cy, 12, out var goal)) continue;
                var path = PathFinder.FindPath(ctx, st, from.X, from.Y, goal.X, goal.Y, w * h);
                if (path == null) continue;
                double len = 0;
                int px = from.X, py = from.Y;
                foreach (var t in path)
                {
                    len += Dist(px, py, t.X, t.Y);
                    px = t.X;
                    py = t.Y;
                }
                p.Walk = len;
                foreach (var c in ctx.Sites.OfKind(SiteKind.Camp))
                    foreach (var t in path)
                        if (Dist(c.Centre.X, c.Centre.Y, t.X + 0.5, t.Y + 0.5) <= GuardTiles)
                        {
                            p.RouteCamps++;
                            p.RouteDefenders += c.Amount;
                            break;
                        }
            }
            return list;
        }

        // ------------------------------------------------------------------ part 2: where an Excavator can dig only Coal

        private sealed class Spot
        {
            public int X, Y, GX, GY;
            public Dir Dir;
            public double Units;
            public int Tiles;
            public bool OnPatch;
        }

        /// <summary>
        /// The Excavator spot on the yard patch that reaches the most Coal with nothing but Coal in its dig area
        /// (<see cref="MachinePhase.TryFindRubble"/>: its footprint and the ring around it), with a Generator on its
        /// output tile. The Generator may not stand on Coal the Excavator cannot reach, or the hands lose it.
        /// <paramref name="onPatch"/> false keeps the Excavator off the patch, as the opening's "at a resource patch
        /// edge" asks.
        /// </summary>
        private static Spot BestSpot(SimContext ctx, SimState st, SiteRecord yard, bool onPatch)
        {
            ctx.Data.TryMachine("excavator", out var ex);
            ctx.Data.TryMachine("generator", out var gen);
            var size = ex.Size;
            var gs = gen.Size;
            Spot best = null;
            for (var y = yard.Y - size - 1; y <= yard.Y + yard.H + 1; y++)
                for (var x = yard.X - size - 1; x <= yard.X + yard.W + 1; x++)
                {
                    var on = Overlaps(x, y, size, size, yard.X, yard.Y, yard.W, yard.H);
                    if (on != onPatch) continue;
                    if (RealCityFixture.TouchesCore(st, x, y, size)) continue;
                    if (Placement.GeometryProblem(ctx, st, "excavator", x, y, Dir.N) != "") continue;
                    double units = 0;
                    var tiles = 0;
                    var clean = true;
                    for (var ty = y - 1; ty <= y + size && clean; ty++)
                        for (var tx = x - 1; tx <= x + size; tx++)
                        {
                            if (!Mining.TryTile(ctx, st, tx, ty, out var item, out var u)) continue;
                            if (item != ItemId.Coal) { clean = false; break; }
                            units += u;
                            tiles++;
                        }
                    if (!clean || units <= 0) continue;
                    if (best != null && units <= best.Units + 1e-9) continue;

                    foreach (var dir in new[] { Dir.N, Dir.E, Dir.S, Dir.W })
                    {
                        var (ox, oy) = ProductionRules.OutputTile(new Machine { Kind = "excavator", X = x, Y = y, Dir = dir, Size = size });
                        var placed = false;
                        for (var gy = oy - gs + 1; gy <= oy && !placed; gy++)
                            for (var gx = ox - gs + 1; gx <= ox && !placed; gx++)
                            {
                                if (Overlaps(gx, gy, gs, gs, x, y, size, size)) continue;
                                if (RealCityFixture.TouchesCore(st, gx, gy, gs)) continue;
                                if (Placement.GeometryProblem(ctx, st, "generator", gx, gy, Dir.N) != "") continue;
                                if (LinksSubstation(ctx, gx, gy, gs, gs, 0)) continue;
                                var strands = false;
                                for (var ty = gy; ty < gy + gs; ty++)
                                    for (var tx = gx; tx < gx + gs; tx++)
                                        if (CoalAt(ctx, st, tx, ty, out _) && !Overlaps(tx, ty, 1, 1, x - 1, y - 1, size + 2, size + 2))
                                            strands = true;
                                if (strands) continue;
                                best = new Spot { X = x, Y = y, Dir = dir, GX = gx, GY = gy, Units = units, Tiles = tiles, OnPatch = on };
                                placed = true;
                            }
                        if (placed) break;
                    }
                }
            return best;
        }

        // ------------------------------------------------------------------ part 3: the measured runs

        private sealed class Run
        {
            public string Name;
            public double TargetKw;
            public Simulation Sim;
            public Machine Excavator;
            public readonly List<Machine> Generators = new List<Machine>();
            public readonly List<Machine> Loads = new List<Machine>();
            public readonly List<Machine> Poles = new List<Machine>();
            public readonly List<TilePoint> Yard = new List<TilePoint>();
            public readonly List<TilePoint> Hand = new List<TilePoint>();
            public double T0, SeedSeconds;
            public double ReachGone = -1, HandDone = -1, PatchEmpty = -1, Short = -1, Out = -1;
            public double MinedByHand, Moved;
            public int Refused;
            public string FirstRefusal = "";
            public readonly StringBuilder Log = new StringBuilder();
        }

        private static double Minutes(Run r) => (r.Sim.State.T - r.T0) / 60.0;

        private static void Apply(Run r, Command c)
        {
            var res = r.Sim.Apply(c);
            if (res.Accepted) return;
            r.Refused++;
            if (r.FirstRefusal.Length == 0) r.FirstRefusal = c.GetType().Name + ": " + res.Problem;
        }

        private static double YardLeft(Run r)
        {
            double left = 0;
            foreach (var t in r.Yard)
                if (CoalAt(r.Sim.Context, r.Sim.State, t.X, t.Y, out var u)) left += u;
            return left;
        }

        private static double HandLeft(Run r)
        {
            double left = 0;
            foreach (var t in r.Hand)
                if (CoalAt(r.Sim.Context, r.Sim.State, t.X, t.Y, out var u)) left += u;
            return left;
        }

        /// <summary>A pole node's rect is its footprint; its reach is its machine's.</summary>
        private static bool Covered(SimContext ctx, List<Machine> poles, int x, int y, int w, int h)
        {
            foreach (var p in poles)
            {
                var reach = PowerGrid.ReachOf(ctx.Data, p);
                if (Reach.DistToRect(p.X + 0.5, p.Y + 0.5, x, y, w, h) <= reach - 0.5) return true;
            }
            return false;
        }

        private static Run Build(Simulation sim, string name, double targetKw, string[] loads, int generators, Spot spot,
            List<TilePoint> yardTiles)
        {
            var ctx = sim.Context;
            var st = sim.State;
            var d = ctx.Data;
            st.Admin.FreezeEnemies = true;
            var r = new Run { Name = name, TargetKw = targetKw, Sim = sim };
            r.Yard.AddRange(yardTiles);

            // The coal line: Excavator at the chosen spot, its Generator on the output tile.
            r.Excavator = Placement.Add(ctx, st, "excavator", spot.X, spot.Y, spot.Dir);
            r.Generators.Add(Placement.Add(ctx, st, "generator", spot.GX, spot.GY, Dir.N));
            d.TryMachine("excavator", out var ex);
            var digX = spot.X - 1;
            var digY = spot.Y - 1;
            var digS = ex.Size + 2;
            foreach (var t in yardTiles)
                if (!Overlaps(t.X, t.Y, 1, 1, digX, digY, digS, digS)) r.Hand.Add(t);

            // Nothing else may stand on the yard's coal, on the Excavator's dig area or on any resource site, and no
            // node may cable to a substation (its streetlights would add to the load).
            var sites = new List<SiteRecord>(ctx.Sites.OfKind(SiteKind.Resource));
            bool Clear(int x, int y, int w, int h)
            {
                if (Overlaps(x, y, w, h, digX, digY, digS, digS)) return false;
                foreach (var s in sites)
                    if (Overlaps(x, y, w, h, s.X - 1, s.Y - 1, s.W + 2, s.H + 2)) return false;
                return true;
            }

            var g0 = r.Generators[0];
            d.TryMachine("generator", out var gen);
            for (var i = 1; i < generators; i++)
            {
                var g = RealCityFixture.PlaceNear(ctx, st, "generator", g0.X, g0.Y, 8,
                    (x, y) => Clear(x, y, gen.Size, gen.Size) && !LinksSubstation(ctx, x, y, gen.Size, gen.Size, 0));
                Assert.That(g, Is.Not.Null, name + ": a second Generator beside the first");
                r.Generators.Add(g);
            }

            d.TryMachine("pole", out var pole);
            var poleReach = pole.ReachTiles;
            // First pole: one that reaches every Generator and the Excavator.
            var first = RealCityFixture.PlaceNear(ctx, st, "pole", g0.X, g0.Y, 8, (x, y) =>
            {
                if (!Clear(x, y, 1, 1) || LinksSubstation(ctx, x, y, 1, 1, poleReach)) return false;
                foreach (var g in r.Generators)
                    if (Reach.DistToRect(x + 0.5, y + 0.5, g.X, g.Y, g.Size, g.Size) > poleReach - 0.5) return false;
                return Reach.DistToRect(x + 0.5, y + 0.5, r.Excavator.X, r.Excavator.Y, ex.Size, ex.Size) <= poleReach - 0.5;
            });
            Assert.That(first, Is.Not.Null, name + ": a pole reaching the Generators and the Excavator");
            r.Poles.Add(first);

            foreach (var kind in loads)
            {
                d.TryMachine(kind, out var spec);
                Machine m = null;
                for (var attempt = 0; attempt < 8 && m == null; attempt++)
                {
                    var anchor = r.Poles[r.Poles.Count - 1];
                    m = RealCityFixture.PlaceNear(ctx, st, kind, anchor.X - spec.Size / 2, anchor.Y - spec.Size / 2, 10,
                        (x, y) => Clear(x, y, spec.Size, spec.Size) && Covered(ctx, r.Poles, x, y, spec.Size, spec.Size));
                    if (m != null) break;
                    // No room in reach of the poles so far: chain one more pole off the last, at least 5 tiles on.
                    var next = RealCityFixture.PlaceNear(ctx, st, "pole", anchor.X, anchor.Y, 8, (x, y) =>
                    {
                        if (!Clear(x, y, 1, 1) || LinksSubstation(ctx, x, y, 1, 1, poleReach)) return false;
                        var links = false;
                        foreach (var p in r.Poles)
                        {
                            var dd = Dist(x, y, p.X, p.Y);
                            if (dd < 5) return false;
                            if (dd <= poleReach - 0.5) links = true;
                        }
                        return links;
                    });
                    Assert.That(next, Is.Not.Null, name + ": room for one more pole to reach the " + kind);
                    r.Poles.Add(next);
                }
                Assert.That(m, Is.Not.Null, name + ": a spot for the " + kind);
                r.Loads.Add(m);
            }

            // The engineer's spot: open ground from which every hand tile and every Generator is in reach.
            var home = r.Generators[0];
            var found = false;
            for (var rad = 0; rad <= 10 && !found; rad++)
                for (var dy = -rad; dy <= rad && !found; dy++)
                    for (var dx = -rad; dx <= rad && !found; dx++)
                    {
                        if (Math.Max(Math.Abs(dx), Math.Abs(dy)) != rad) continue;
                        int ex0 = home.X + dx, ey0 = home.Y + dy;
                        if (!Ground.Passable(ctx, st, ex0, ey0)) continue;
                        st.Engineer.Pos = new Vec2(ex0 + 0.5, ey0 + 0.5);
                        var ok = true;
                        foreach (var t in r.Hand) ok &= Ground.InReach(ctx, st, t.X, t.Y);
                        foreach (var g in r.Generators) ok &= Interaction.InReach(ctx, st, g);
                        found = ok;
                    }
            Assert.That(found, Is.True, name + ": a spot where the engineer reaches the hand coal and every Generator");
            return r;
        }

        /// <summary>
        /// The first fuel, as the opening's "Fuel your Generator" row asks: dig Coal by hand, then put it in. Minute 0
        /// is the moment it goes in.
        /// </summary>
        private static void FirstFuel(Run r)
        {
            var sim = r.Sim;
            var st = sim.State;
            var need = SeedCoal * r.Generators.Count;
            var start = st.T;
            var limit = (int)(600 * Simulation.TicksPerSecond);
            for (var i = 0; i < limit && st.Engineer.Inv[ItemId.Coal] < need; i++)
            {
                if (!st.Engineer.Mining)
                    foreach (var t in r.Hand)
                        if (CoalAt(sim.Context, st, t.X, t.Y, out _)) { Apply(r, new MineCommand(t.X, t.Y)); break; }
                sim.Tick();
                st.Events.Clear();
            }
            Assert.That(st.Engineer.Inv[ItemId.Coal], Is.GreaterThanOrEqualTo(need),
                r.Name + ": the engineer dug the first fuel by hand" + (r.FirstRefusal.Length > 0 ? " (" + r.FirstRefusal + ")" : ""));
            Apply(r, new StopMiningCommand());
            r.MinedByHand += need;
            foreach (var g in r.Generators) Apply(r, new MachineTransferCommand(g.Id, ItemId.Coal, SeedCoal, true));
            r.SeedSeconds = st.T - start;
            r.T0 = st.T;
        }

        /// <summary>Once a second: the attentive player's hands.</summary>
        private static void Hands(Run r, double cap)
        {
            var sim = r.Sim;
            var st = sim.State;
            var e = st.Engineer;
            if (!e.Mining)
                foreach (var t in r.Hand)
                    if (CoalAt(sim.Context, st, t.X, t.Y, out _)) { Apply(r, new MineCommand(t.X, t.Y)); break; }

            foreach (var g in r.Generators)
            {
                var fuel = g.Inv[ItemId.Coal];
                if (fuel > cap / 2) continue;
                var carried = (int)Math.Floor(e.Inv[ItemId.Coal]);
                if (carried < 1)
                {
                    Machine donor = null;
                    foreach (var o in r.Generators)
                        if (o != g && o.Inv[ItemId.Coal] >= fuel + 2 && (donor == null || o.Inv[ItemId.Coal] > donor.Inv[ItemId.Coal]))
                            donor = o;
                    if (donor == null) continue;
                    var n = (int)Math.Floor((donor.Inv[ItemId.Coal] - fuel) / 2);
                    if (n < 1) continue;
                    Apply(r, new MachineTransferCommand(donor.Id, ItemId.Coal, n, false));
                    r.Moved += n;
                    carried = (int)Math.Floor(e.Inv[ItemId.Coal]);
                }
                var put = Math.Min(carried, (int)Math.Floor(cap - g.Inv[ItemId.Coal]));
                if (put >= 1) Apply(r, new MachineTransferCommand(g.Id, ItemId.Coal, put, true));
            }
        }

        private static string Header(Run r)
        {
            var gens = new StringBuilder();
            for (var i = 0; i < r.Generators.Count; i++) gens.Append("  gen" + (i + 1));
            return "  min  yard-left  hand-left  backpack" + gens + "  burned  demand  supply  excavator";
        }

        private static string Sample(Run r)
        {
            var sim = r.Sim;
            var st = sim.State;
            var p = PowerQueries.Network(sim.Context, st);
            var gens = new StringBuilder();
            foreach (var g in r.Generators) gens.Append("  " + N(g.Inv[ItemId.Coal], "0").PadLeft(4));
            return "  " + N(Minutes(r), "0").PadLeft(3)
                + "  " + N(YardLeft(r), "0").PadLeft(9)
                + "  " + N(HandLeft(r), "0").PadLeft(9)
                + "  " + N(st.Engineer.Inv[ItemId.Coal], "0").PadLeft(8)
                + gens
                + "  " + N(st.Stats.CoalBurned, "0").PadLeft(6)
                + "  " + N(p.DemandKw, "0").PadLeft(6)
                + "  " + N(p.SupplyKw, "0").PadLeft(6)
                + "  " + ProductionQueries.OperatingState(sim.Context, st, r.Excavator.Id);
        }

        private static void Measure(Run r)
        {
            var sim = r.Sim;
            var ctx = sim.Context;
            var st = sim.State;
            var cap = MachineInventory.GeneratorFuelCap(ctx.Data);
            var ticksPerSecond = (int)Simulation.TicksPerSecond;

            sim.Tick();
            st.Events.Clear();
            var p0 = PowerQueries.Network(ctx, st);
            Assert.That(p0.DemandKw, Is.EqualTo(r.TargetKw).Within(1e-6), r.Name + ": the fixed load is the one the run claims");
            Assert.That(p0.SupplyKw, Is.GreaterThanOrEqualTo(r.TargetKw - 1e-6), r.Name + ": the Generators cover it once fuelled");
            Assert.That(PowerQueries.Supplied(ctx, st, r.Excavator.Id), Is.True, r.Name + ": the Excavator is on the circuit");
            foreach (var m in r.Loads)
                Assert.That(PowerQueries.Supplied(ctx, st, m.Id), Is.True, r.Name + ": the " + m.Kind + " at " + m.X + "," + m.Y + " is on the circuit");

            r.Log.AppendLine(Header(r));
            r.Log.AppendLine(Sample(r));
            var after = -1.0;
            var capTicks = (int)(CapMinutes * 60 * ticksPerSecond);
            for (var tick = 1; tick <= capTicks; tick++)
            {
                sim.Tick();
                st.Events.Clear();
                if (tick % ticksPerSecond != 0) continue;

                Hands(r, cap);
                var min = Minutes(r);
                if (r.ReachGone < 0 && !MachinePhase.TryFindRubble(ctx, st, r.Excavator, out _, out _, out _, out _)) r.ReachGone = min;
                if (r.HandDone < 0 && HandLeft(r) <= 0) r.HandDone = min;
                if (r.PatchEmpty < 0 && YardLeft(r) <= 0) r.PatchEmpty = min;
                var p = PowerQueries.Network(ctx, st);
                if (r.Short < 0 && p.SupplyKw < p.DemandKw - 1e-6) r.Short = min;
                if (r.Out < 0 && p.SupplyKw <= 0) { r.Out = min; after = min + 2; }
                if (tick % (60 * ticksPerSecond) == 0) r.Log.AppendLine(Sample(r));
                if (after > 0 && min >= after) break;
            }
            r.Log.AppendLine(Sample(r));
        }

        private static string When(double minute) => minute < 0 ? "not within " + N(CapMinutes, "0") + " min" : "minute " + N(minute, "0.0");

        [Test, Timeout(3600000)]
        public void HomeCoalRunsOutAt300And600Kw()
        {
            var file = new StringBuilder();
            try
            {
                Measured(file);
            }
            finally
            {
                Directory.CreateDirectory(Path.GetDirectoryName(LogPath));
                File.WriteAllText(LogPath, file.ToString());
                TestContext.WriteLine(file.ToString());
                TestContext.WriteLine("written to " + LogPath);
            }
        }

        private void Measured(StringBuilder file)
        {
            var survey = NewGame();
            var ctx = survey.Context;
            var st = survey.State;
            var d = ctx.Data;
            var eng = d.Engineer;
            var coalMj = d.Power.CoalMj;
            var yard = Site(ctx, YardCoal);
            Assert.That(yard, Is.Not.Null, "the opening resource layout adds the yard coal site " + YardCoal);

            file.AppendLine("REL-88 (ECO-03a): how long Home's coal lasts, on the real city");
            file.AppendLine("Build: HEAD at the time of the run; game data hash " + GameDataHash.Compute(d) + "; seed " + Seed
                + "; opening resource layout v" + OpeningResourceLayout.Version + ".");
            file.AppendLine("What this is: a scripted headless measurement. It is not an owner session and not a verdict.");
            file.AppendLine();

            // ---- part 1
            var from = Spawn(ctx, st);
            var walk = eng.WalkTilesPerS;
            var sprintShare = eng.StaminaRegenPerS / (eng.SprintDrainPerS + eng.StaminaRegenPerS);
            var sprint = walk * (1 + (eng.SprintMul - 1) * sprintShare);
            var patches = Survey(ctx, st, from, 12);
            double total = 0;
            foreach (var p in patches) total += p.Units;
            file.AppendLine("1. EVERY COAL SOURCE (tiles where the ground yields Coal, grouped 8-connected)");
            file.AppendLine("   Engineer's new-game spot: tile " + from.X + "," + from.Y + " in " + (Districts.NameAt(ctx, from.X + 0.5, from.Y + 0.5) ?? "(no district)") + ".");
            file.AppendLine("   " + patches.Count + " patches, " + N(total, "0") + " units in all. Walking " + N(walk) + " tiles/s; a sprint-and-recover"
                + " average of " + N(sprint, "0.00") + " tiles/s (estimate: sprinting " + N(sprintShare * 100, "0") + "% of the time, the share"
                + " its drain " + N(eng.SprintDrainPerS, "0.##") + "/s and regeneration " + N(eng.StaminaRegenPerS, "0.###") + "/s balance at).");
            file.AppendLine("   Routes are the engineer's own path-finder to open ground beside the patch, measured along the path.");
            var campSites = 0;
            var campDefenders = 0;
            foreach (var c in ctx.Sites.OfKind(SiteKind.Camp)) { campSites++; campDefenders += c.Amount; }
            file.AppendLine("   Guards: enemy camp sites (with their defender count) within " + N(GuardTiles, "0") + " tiles of the patch centre,"
                + " and within " + N(GuardTiles, "0") + " tiles of any tile of the walking route; living aliens within " + N(GuardTiles, "0")
                + " tiles of the patch. The whole map has " + campSites + " camp sites (" + campDefenders + " defenders) and "
                + st.Enemies.Actors.Count + " living aliens at new-game time.");
            file.AppendLine();
            file.AppendLine("   units  tiles  centre      straight  walk      walk min  sprint min  camps(def)  route camps(def)  aliens  district / site");
            Patch nearestOther = null;
            foreach (var p in patches)
            {
                if (!p.Routed) continue;
                if (!p.Yard && p.Walk >= 0 && (nearestOther == null || p.Walk < nearestOther.Walk)) nearestOther = p;
                file.AppendLine("   " + N(p.Units, "0").PadLeft(5) + "  " + p.Tiles.Count.ToString(CultureInfo.InvariantCulture).PadLeft(5)
                    + "  " + (N(p.Cx, "0") + "," + N(p.Cy, "0")).PadRight(10)
                    + "  " + N(p.Straight, "0").PadLeft(8)
                    + "  " + (p.Walk < 0 ? "no route" : N(p.Walk, "0")).PadLeft(8)
                    + "  " + (p.Walk < 0 ? "-" : N(p.Walk / walk / 60, "0.0")).PadLeft(8)
                    + "  " + (p.Walk < 0 ? "-" : N(p.Walk / sprint / 60, "0.0")).PadLeft(10)
                    + "  " + (p.Camps + "(" + p.Defenders + ")").PadLeft(10)
                    + "  " + (p.Walk < 0 ? "-" : p.RouteCamps + "(" + p.RouteDefenders + ")").PadLeft(16)
                    + "  " + p.Aliens.ToString(CultureInfo.InvariantCulture).PadLeft(6)
                    + "  " + p.District + " / " + p.Sites + (p.Yard ? "  <- Home's yard patch" : ""));
            }
            var unrouted = 0;
            double unroutedUnits = 0;
            foreach (var p in patches)
                if (!p.Routed) { unrouted++; unroutedUnits += p.Units; }
            if (unrouted > 0)
                file.AppendLine("   (" + unrouted + " further patches, " + N(unroutedUnits, "0") + " units, are farther in a straight line than the"
                    + " twelve nearest and carry no authored site; not routed.)");
            file.AppendLine();

            var stacks = eng.InvStacks;
            var perStack = d.Item(ItemId.Coal).StackSize;
            file.AppendLine("   Carrying: the Backpack holds " + stacks + " stacks of " + perStack + " Coal = " + (stacks * perStack) + " Coal ("
                + N(stacks * perStack * coalMj, "0") + " MJ) if it holds nothing else. Hand mining is " + N(eng.HandMinePerS) + " Coal/s,"
                + " so 100 Coal takes " + N(100 / eng.HandMinePerS / 60, "0.0") + " min at the face. The truck is not ported, so walking is the"
                + " only way to move Coal today.");
            Assert.That(nearestOther, Is.Not.Null, "some coal outside the yard has a walking route from Home");
            var reachMin = nearestOther.Walk / walk / 60;
            file.AppendLine("   EARLIEST MORE COAL: " + nearestOther.District + " / " + nearestOther.Sites + ", " + N(nearestOther.Units, "0")
                + " units, " + N(nearestOther.Walk, "0") + " tiles on foot: minute " + N(reachMin, "0.0") + " of a new game at a walk ("
                + N(nearestOther.Walk / sprint / 60, "0.0") + " sprinting), if the player leaves at once and nothing stops them. "
                + nearestOther.Camps + " camp site(s) with " + nearestOther.Defenders + " defenders are within " + N(GuardTiles, "0")
                + " tiles of it, and " + nearestOther.RouteCamps + " camp site(s) with " + nearestOther.RouteDefenders
                + " defenders lie within " + N(GuardTiles, "0") + " tiles of the route there. The round trip with 100 Coal is about "
                + N(2 * reachMin + 100 / eng.HandMinePerS / 60, "0") + " min.");
            file.AppendLine();

            // ---- part 2
            var yardTiles = new List<TilePoint>();
            double yardUnits = 0;
            foreach (var p in patches)
                if (p.Yard) { yardTiles.AddRange(p.Tiles); yardUnits += p.Units; }
            var edge = BestSpot(ctx, st, yard, onPatch: false);
            var on = BestSpot(ctx, st, yard, onPatch: true);
            file.AppendLine("2. HOME'S YARD PATCH AND WHERE AN EXCAVATOR CAN DIG ONLY COAL");
            file.AppendLine("   Site " + yard.Id + " \"" + yard.Name + "\" at " + yard.X + "," + yard.Y + ", " + yard.W + "x" + yard.H + ", " + yard.Amount
                + " units in the site record; the ground holds " + N(yardUnits, "0") + " units on " + yardTiles.Count + " tiles.");
            file.AppendLine("   An Excavator digs the first resource in its footprint and the ring around it, whatever it is. A spot counts only"
                + " when all of that is Coal, with a Generator on its output tile that covers no Coal the Excavator cannot reach.");
            Assert.That(edge, Is.Not.Null, "an Excavator can stand at the yard patch's edge digging only Coal, with a Generator on its output");
            file.AppendLine("   At the edge (what the opening asks): " + edge.X + "," + edge.Y + " facing " + edge.Dir + ", reaches " + edge.Tiles + " tiles, "
                + N(edge.Units, "0") + " units; " + N(yardUnits - edge.Units, "0") + " units are left for the hands.");
            file.AppendLine(on == null
                ? "   On the patch: no spot where the dig area is Coal only."
                : "   On the patch (the placement rule allows it; the opening does not suggest it): " + on.X + "," + on.Y + " facing " + on.Dir
                  + ", reaches " + on.Tiles + " tiles, " + N(on.Units, "0") + " units.");
            file.AppendLine();

            // ---- part 3
            file.AppendLine("3. THE MEASURED RUNS");
            file.AppendLine("   Line: the edge Excavator above, its Generator on the output tile; a second Generator beside it at 600 kW. Load:"
                + " the Excavator (" + N(Kw(d, "excavator"), "0") + " kW) plus idle Foundries (" + N(Kw(d, "foundry"), "0") + " kW each)"
                + " and Assemblers (" + N(Kw(d, "assembler"), "0") + " kW each); a machine draws its full rating whatever it is doing.");
            file.AppendLine("   Hands, once a second: dig the next Coal tile the Excavator cannot reach; keep each Generator above half ("
                + N(MachineInventory.GeneratorFuelCap(d) / 2, "0") + ") from the Backpack, or from the fuller Generator when the Backpack is"
                + " empty. The engineer stands in reach throughout (no walking), and enemies are frozen.");
            file.AppendLine("   Minute 0 is the first fuel going in. The opening before it is not played; see the note at the end.");
            file.AppendLine();

            var runs = new[]
            {
                Build(NewGame(), "300 kW", 300, new[] { "foundry", "foundry", "foundry" }, 1, edge, yardTiles),
                Build(NewGame(), "600 kW", 600, new[] { "foundry", "foundry", "foundry", "assembler", "assembler", "assembler" }, 2, edge, yardTiles),
            };
            foreach (var r in runs)
            {
                FirstFuel(r);
                Measure(r);
                var paper = yardUnits * coalMj * 1000 / r.TargetKw / 60;
                file.AppendLine("   --- " + r.Name + ": " + r.Generators.Count + " Generator(s), " + r.Poles.Count + " pole(s), loads "
                    + string.Join(", ", r.Loads.ConvertAll(m => m.Kind + "@" + m.X + "," + m.Y)) + "; engineer at "
                    + N(r.Sim.State.Engineer.Pos.X, "0") + "," + N(r.Sim.State.Engineer.Pos.Y, "0") + ".");
                file.AppendLine("   The first " + SeedCoal * r.Generators.Count + " Coal by hand took " + N(r.SeedSeconds, "0") + " s.");
                file.Append(r.Log);
                file.AppendLine("   Excavator's reach dug out: " + When(r.ReachGone) + ". Hand coal dug out: " + When(r.HandDone)
                    + ". Yard patch empty: " + When(r.PatchEmpty) + ".");
                file.AppendLine("   Supply first below demand: " + When(r.Short) + ". Power fully out: " + When(r.Out)
                    + ". Paper figure (" + N(yardUnits, "0") + " Coal x " + N(coalMj) + " MJ at " + N(r.TargetKw, "0") + " kW): minute " + N(paper, "0.0") + ".");
                var st2 = r.Sim.State;
                double inGens = 0;
                foreach (var g in r.Generators) inGens += g.Inv[ItemId.Coal];
                file.AppendLine("   Coal burned " + N(st2.Stats.CoalBurned, "0") + "; left: yard " + N(YardLeft(r), "0") + ", Backpack "
                    + N(st2.Engineer.Inv[ItemId.Coal], "0") + ", Generators " + N(inGens, "0") + ", Excavator " + N(r.Excavator.Inv[ItemId.Coal], "0")
                    + ". Coal carried Generator to Generator: " + N(r.Moved, "0") + ". Refused commands: " + r.Refused
                    + (r.FirstRefusal.Length > 0 ? " (first: " + r.FirstRefusal + ")" : "") + ".");
                file.AppendLine();
                Assert.That(r.Out, Is.GreaterThan(0), r.Name + ": the run reached the power going out within " + CapMinutes + " min");
            }

            // ---- the opening's own load, from the data, for scale
            var home = st.Home;
            var sub = Districts.SubstationAt(ctx, home.X + home.W / 2.0, home.Y + home.H / 2.0);
            var lights = 0;
            if (sub != null)
            {
                var all = StreetLights.Sites(ctx);
                for (var i = 0; i < all.Count; i++)
                    if (PowerGrid.SubstationOf(ctx, all[i])?.Id == sub.Id) lights++;
            }
            var turrets = OpeningRules.Tuning(d).TurretObjective;
            var asked = Kw(d, "excavator") + Kw(d, "foundry") + d.Power.LampKw + turrets * Kw(d, "turret") + lights * d.Power.StreetLightKw;
            var withCoal = asked + Kw(d, "excavator");
            file.AppendLine("4. FOR SCALE: WHAT THE OPENING ITSELF ASKS THE PLAYER TO POWER (from the data, not measured)");
            file.AppendLine("   The iron line (Excavator " + N(Kw(d, "excavator"), "0") + " + Foundry " + N(Kw(d, "foundry"), "0") + " kW), one Lamp "
                + N(d.Power.LampKw) + " kW, " + turrets + " Gun turrets (the opening's turret objective) at " + N(Kw(d, "turret"), "0") + " kW each, "
                + (sub == null ? "and no substation for Home's district" : "and Home's district substation (" + sub.Name + ") with " + lights
                  + " streetlights at " + N(d.Power.StreetLightKw) + " kW")
                + ": " + N(asked, "0") + " kW. The fuel step also suggests a coal Excavator belted into the Generator: "
                + N(withCoal, "0") + " kW with it. The Home core draws nothing (the CoreKw tuning row is not read).");
            file.AppendLine("   Both runs burned every unit at the paper rate, so the paper figure is the one to expect at these loads too"
                + " (not run): minute " + N(yardUnits * coalMj * 1000 / asked / 60, "0") + " at " + N(asked, "0") + " kW, minute "
                + N(yardUnits * coalMj * 1000 / withCoal / 60, "0") + " at " + N(withCoal, "0") + " kW.");
            file.AppendLine();
            file.AppendLine("NOT MEASURED HERE");
            file.AppendLine("   - The opening's own steps before minute 0 (mining ore, smelting, building) and the minutes they take: owner session.");
            file.AppendLine("   - Walking between the patch and the Generators, and fights on the way to other coal: the engineer stood still and"
                + " enemies were frozen.");
            file.AppendLine("   - Whether coal 'felt short' to a player: REL-88's owner session, recorded as an observation, not a verdict.");
        }

        private static double Kw(GameData d, string kind) => d.TryMachine(kind, out var s) && s.PowerKw > 0 ? s.PowerKw : 0;
    }
}
