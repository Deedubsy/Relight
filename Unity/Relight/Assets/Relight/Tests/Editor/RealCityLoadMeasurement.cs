using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Text;
using NUnit.Framework;
using Relight.Sim;
using Relight.Sim.UI;
using Relight.World;
using UnityEngine;
using Light = Relight.Sim.Light;
using Object = UnityEngine.Object;
using Stopwatch = System.Diagnostics.Stopwatch;

namespace Relight.Authoring.Tests
{
    /// <summary>
    /// REL-64 (PER-04, U-D-66 (8), E-19): what the sim costs on the Mono runtime, on the REAL CITY, with the alien
    /// counts the design writes down. A MEASUREMENT, not a check: it is <c>Explicit</c>, so the suite never runs it,
    /// and it asserts only that each case was actually staged. It proves nothing about balance.
    ///
    /// The issue asks for four readings, and this writes all four to one file:
    /// <list type="number">
    /// <item><b>240 awake.</b> The world cap (<c>RaidTuning.LivingBudget</c>) as small-raid bodies, all of them
    /// ticked, converging on a defended Home from 40 to 70 route steps out and leaving by the raid's approach, as a
    /// director-born raid does. Then again (A2) with each body leaving by the tile it was born on, the stress case
    /// the first run of this file measured by accident.</item>
    /// <item><b>65 roamers mostly asleep plus full camp garrisons.</b> Roamers are not built (REL-42), so they are
    /// stood in by camp residents, the only body the sim lets sleep: 65 in packs of 2 to 4 at least 40 tiles
    /// apart (U-P-15), plus the three authored camps at the top of U-P-16's ranges (12 for the first, 22 for each
    /// later one). A resident further than <c>SiegeTuning.GuardSleepTiles</c> from the engineer is not ticked.</item>
    /// <item><b>The mask.</b> A real-city rebuild with the city's own lights, forced through the game's own
    /// <c>LightMaskPhase</c>; and the offline test's 360 lights stamped on the real streets, for comparison with its
    /// 18.28 ms on .NET 8.</item>
    /// <item><b>The route.</b> A cold route field (the cache emptied first) to the Home core, ordinary and breach,
    /// and to an approach tile, the field a withdrawing body walks.</item>
    /// </list>
    /// It also times the two HUD scans the issue names (<c>HudViewModel</c>'s problem list and
    /// <c>DefenceAlertSource</c>) with the ring's machines and with 200 more.
    ///
    /// The tick is timed phase by phase: the loop below is <c>Simulation.TickInternal</c> exactly (every phase in
    /// <c>SimComposition.Phases</c> order, then the tick counter and the clock), with a clock read around each phase.
    /// No commands are queued, so the flush a <c>Simulation.Tick</c> does first has nothing to do.
    /// </summary>
    public sealed class RealCityLoadMeasurement
    {
        private static readonly CultureInfo C = CultureInfo.InvariantCulture;
        private const int GroupBase = 100000;       // resident group ids, far from any raid id

        private WorldGeometryAsset _opening;

        [TearDown]
        public void Teardown()
        {
            if (_opening != null) Object.DestroyImmediate(_opening);
            _opening = null;
        }

        /// <summary>The project's ignored <c>Logs/</c> folder, as REL-85's run log; the reading is copied into the
        /// evidence tree by hand, with the commit it was taken on.</summary>
        public static string LogPath =>
            Path.GetFullPath(Path.Combine(Application.dataPath, "..", "Logs", "REL-64-real-city-load.txt"));

        [Test, Explicit("REL-64: a measurement run, not a check; run it by name"), Timeout(3600000)]
        public void MeasureTheTickWith240AliensAndTheMaskAndRouteCost_OnTheRealCity()
        {
            const int seed = 1;
            var ctx = RealCityFixture.Context(out _opening);
            var sim = Simulation.NewGame(ctx, seed);
            var st = sim.State;
            var d = st.Director;
            st.OpeningResourceVersion = OpeningResourceLayout.Version;
            Assert.That(st.Home.Fallback, Is.False, "on its authored site");

            // The file is rewritten after every section, so a run that fails or is stopped keeps what it measured, and
            // each section says how long it took on the wall clock.
            var file = new StringBuilder();
            var wall = Stopwatch.StartNew();
            var lap = 0.0;
            void Done(string what)
            {
                var now = wall.Elapsed.TotalSeconds;
                file.AppendLine(string.Format(C, "   [{0}: {1:0.0} s of wall time; {2:0.0} s since the run began]", what, now - lap, now));
                file.AppendLine();
                lap = now;
                Directory.CreateDirectory(Path.GetDirectoryName(LogPath));
                File.WriteAllText(LogPath, file.ToString());
            }
            file.AppendLine("REL-64 (PER-04) real-city load measurement — written by " + nameof(MeasureTheTickWith240AliensAndTheMaskAndRouteCost_OnTheRealCity));
            file.AppendLine("A measurement, not a check and not a balance verdict. Times are wall-clock milliseconds.");
            Runtime(file);
            file.AppendLine(string.Format(C, "map {0} ({1} × {2}) · seed {3} · data {4}",
                ctx.MapId, ctx.Geometry.Width, ctx.Geometry.Height, seed, GameDataHash.Compute(ctx.Data)));

            // The fixture's defended Home, as REL-85 runs it; the opening's introductory encounter plays out first
            // (it holds the director), then the director's own raids are pushed out of the way.
            var defence = RealCityFixture.Defend(ctx, st);
            Run(sim, 1);
            Assert.That(RunUntil(sim, 600, () => !d.Reserved && (d.Minor == null || !d.Minor.Spawned)), Is.True,
                "the opening's encounter finished");
            d.NextStart = 1e9;
            d.NextMinor = 1e9;
            Clear(st);
            defence.Restore(ctx, st);
            file.AppendLine(string.Format(C,
                "defence: {0} gun turrets, {1} poles, {2} generators around the Home core; {3} machines on the map; sim T {4:0.0} s when measuring began",
                defence.Turrets.Count, defence.Poles.Count, defence.Generators.Count, st.Machines.Count, st.T));
            file.AppendLine(string.Format(C, "engineer at ({0:0.0}, {1:0.0}); Home core at ({2}, {3}) {4}×{5}",
                st.Engineer.Pos.X, st.Engineer.Pos.Y, st.Home.X, st.Home.Y, st.Home.W, st.Home.H));
            Done("setup, with the opening's encounter played out");

            // ---------------------------------------------------------------- 0: nothing alive
            Run(sim, 40);                                                   // warm
            var baseline = Time(sim, 600, "no aliens", CapSeconds);
            Report(file, baseline);
            Done("no aliens");

            // ---------------------------------------------------------------- B: roamers asleep + garrisons
            var packs = StageRoamers(ctx, st, 65, out var packCount);
            var guards = StageGarrisons(ctx, st, out var campLine);
            Assert.That(packs, Is.EqualTo(65), "all 65 roamer stand-ins placed");
            Assert.That(guards, Is.GreaterThan(0), "the camps were garrisoned");
            file.AppendLine(string.Format(C, "case B staged: {0} roamer stand-ins in {1} packs · {2} camp guards ({3}) · {4} awake at the start (within {5:0} tiles of the engineer)",
                packs, packCount, guards, campLine, Awake(ctx, st), ctx.Data.Siege.GuardSleepTiles));
            Run(sim, 40);
            var caseB = Time(sim, 1200, "65 roamers mostly asleep + full camp garrisons", CapSeconds);
            Report(file, caseB);
            Done("case B");

            // ---------------------------------------------------------------- A: 240 awake
            Clear(st);
            defence.Restore(ctx, st);
            Run(sim, 5);
            var born = StageAwake(ctx, st, ctx.Data.Raids.LivingBudget, ownExits: false);
            Assert.That(born, Is.EqualTo(ctx.Data.Raids.LivingBudget), "the whole world cap is alive");
            file.AppendLine(string.Format(C, "case A staged: {0} small-raid bodies (world cap {1}), every one ticked, all leaving by the raid's one approach as a director-born raid does; timed from the first tick, so the cold route fields are in it",
                born, ctx.Data.Raids.LivingBudget));
            var caseA = Time(sim, 1200, "240 awake, converging on the defended Home", CapSeconds);
            Report(file, caseA);

            // The HUD scans with the whole cap alive.
            HudScans(file, ctx, st, st.Enemies.Actors.Count + " alive, " + st.Machines.Count + " machines");
            Done("case A");

            // ---------------------------------------------------------------- A2: 240 awake, each with its own exit
            // The first run of this file staged every body with its own exit tile, and once the core fell each one
            // withdrew along its own route field: far more than the cache's 32, so it emptied and rebuilt them every
            // tick. No director-born raid has that many exits; kept as the stress reading it is, under a short cap.
            Clear(st);
            defence.Restore(ctx, st);
            file.AppendLine(RepairCore(sim, "case A"));
            Run(sim, 5);
            born = StageAwake(ctx, st, ctx.Data.Raids.LivingBudget, ownExits: true);
            file.AppendLine(string.Format(C, "case A2 staged: {0} small-raid bodies as case A, but each leaving by the tile it was born on (a stress case: {1} distinct exits against a field cache of 32)",
                born, DistinctExits(st)));
            var caseA2 = Time(sim, 1200, "240 awake, each with its own exit", StressCapSeconds);
            Report(file, caseA2);
            Done("case A2");

            // ---------------------------------------------------------------- mask and route
            // Case A's bodies may have broken the Home core, and a broken core is no raid target: the route field
            // to it is measured on a whole core, repaired the way a player repairs it.
            Clear(st);
            defence.Restore(ctx, st);
            file.AppendLine(RepairCore(sim, "case A2"));
            Run(sim, 5);
            Mask(file, ctx, st);
            Routes(file, ctx, st);
            Done("mask and route");

            // ---------------------------------------------------------------- 200 more machines
            var extra = Pad(ctx, st, 200);
            Run(sim, 40);
            var padded = Time(sim, 400, "no aliens, " + st.Machines.Count + " machines (" + extra + " unpowered, empty turrets added across the city)", CapSeconds);
            Report(file, padded);
            HudScans(file, ctx, st, "no aliens, " + st.Machines.Count + " machines");
            Done("200 more machines");

            TestContext.WriteLine(file.ToString());
            TestContext.WriteLine("written to " + LogPath);
        }

        /// <summary>The most wall time one timed case may take. The first run of this file spent over half an hour
        /// before it failed, so a case now stops at this cap and reports how many of its ticks it measured.</summary>
        private const double CapSeconds = 600;

        /// <summary>The cap for case A2, whose ticks after the core falls take seconds each.</summary>
        private const double StressCapSeconds = 120;

        /// <summary>
        /// Repairs the Home core when it is below full, as <c>RealCityRaidTests</c> does: the engineer back on
        /// their feet, at the core with its steel and copper, and the game's own repair command run to the end.
        /// </summary>
        private static string RepairCore(Simulation sim, string after)
        {
            var ctx = sim.Context;
            var st = sim.State;
            var hp = HomeQueries.CoreHp(st);
            if (hp >= ctx.Data.Defence.CoreHp) return "the Home core was whole after " + after;
            Assert.That(RunUntil(sim, 120, () => !st.Engineer.IsDown), Is.True, "the engineer got back up");
            st.Engineer.Pos = new Vec2(st.Home.X - 0.5, st.Home.Y + st.Home.H / 2.0);
            // A broken core is recommissioned at the core price, a damaged one repaired at the ordinary price.
            var t = ctx.Data.Defence;
            st.Engineer.Inv[ItemId.Steel] = Math.Max(st.Engineer.Inv[ItemId.Steel], Math.Max(t.CoreSteel, t.RepairSteel));
            st.Engineer.Inv[ItemId.Copper] = Math.Max(st.Engineer.Inv[ItemId.Copper], Math.Max(t.CoreCopper, t.RepairCopper));
            Assert.That(HomeCore.RepairProblem(ctx, st, RepairKinds.Core, 0), Is.Empty, "the core can be repaired");
            Assert.That(sim.Apply(new RepairCommand(RepairKinds.Core, 0)).Problem, Is.Empty);
            Assert.That(RunUntil(sim, Math.Max(t.CoreRepairSeconds, t.RepairSeconds) + 5,
                () => st.Home.RepairKind == RepairKinds.None && HomeQueries.CoreOperational(st)), Is.True, "the core was repaired");
            return string.Format(C, "the Home core was at {0:0} of {1:0} hp after {2}; repaired before the next reading",
                hp, ctx.Data.Defence.CoreHp, after);
        }

        private static int DistinctExits(SimState st)
        {
            var exits = new HashSet<int>();
            foreach (var e in st.Enemies.Actors) exits.Add(e.Origin);
            return exits.Count;
        }

        // ================================================================== timing

        private sealed class Sample
        {
            public string Label;
            public readonly List<double> Ms = new List<double>();
            public double[] PhaseTicks;
            public int AliveFirst, AliveLast, AwakeMin = int.MaxValue, AwakeMax, Gc0, EngineerDown, Wanted, CoreFell = -1;
            public double WallSeconds;
        }

        /// <summary>Times up to <paramref name="ticks"/> ticks, stopping early when <paramref name="capSeconds"/> of
        /// wall time have passed; <see cref="Report"/> says when it stopped early.</summary>
        private static Sample Time(Simulation sim, int ticks, string label, double capSeconds)
        {
            var ctx = sim.Context;
            var st = sim.State;
            var phases = SimComposition.Phases;
            var s = new Sample { Label = label, PhaseTicks = new double[phases.Count], AliveFirst = st.Enemies.Actors.Count, Wanted = ticks };
            var gc = GC.CollectionCount(0);
            var clock = Stopwatch.StartNew();
            var coreUp = HomeQueries.CoreOperational(st);
            for (var i = 0; i < ticks; i++)
            {
                if (i > 0 && clock.Elapsed.TotalSeconds > capSeconds) break;
                if (coreUp && s.CoreFell < 0 && !HomeQueries.CoreOperational(st)) s.CoreFell = s.Ms.Count;
                var start = Stopwatch.GetTimestamp();
                for (var p = 0; p < phases.Count; p++)
                {
                    var t0 = Stopwatch.GetTimestamp();
                    phases[p].Tick(ctx, st, Simulation.TickSeconds);
                    s.PhaseTicks[p] += Stopwatch.GetTimestamp() - t0;
                }
                st.Tick++;
                st.T += Simulation.TickSeconds;
                s.Ms.Add((Stopwatch.GetTimestamp() - start) * 1000.0 / Stopwatch.Frequency);
                st.Events.Clear();
                var awake = Awake(ctx, st);
                if (awake < s.AwakeMin) s.AwakeMin = awake;
                if (awake > s.AwakeMax) s.AwakeMax = awake;
                if (st.Engineer.IsDown) s.EngineerDown++;
            }
            s.AliveLast = st.Enemies.Actors.Count;
            s.Gc0 = GC.CollectionCount(0) - gc;
            s.WallSeconds = clock.Elapsed.TotalSeconds;
            return s;
        }

        private static void Report(StringBuilder file, Sample s)
        {
            var a = s.Ms.ToArray();
            Array.Sort(a);
            double sum = 0;
            int over16 = 0, over50 = 0;
            foreach (var v in a)
            {
                sum += v;
                if (v > 1000.0 / 60) over16++;
                if (v > 1000.0 * Simulation.TickSeconds) over50++;
            }
            double P(double q) => a[Math.Min(a.Length - 1, (int)Math.Floor(q * a.Length))];
            file.AppendLine("== " + s.Label);
            file.AppendLine(string.Format(C,
                "  tick: mean {0:0.00} · median {1:0.00} · p95 {2:0.00} · p99 {3:0.00} · max {4:0.00} ms over {5} ticks ({6:0} s of game)",
                sum / a.Length, P(.5), P(.95), P(.99), a[a.Length - 1], a.Length, a.Length * Simulation.TickSeconds));
            if (a.Length < s.Wanted)
                file.AppendLine(string.Format(C, "  STOPPED AT THE WALL-CLOCK CAP: {0} of the {1} ticks asked for, in {2:0.0} s", a.Length, s.Wanted, s.WallSeconds));
            if (s.CoreFell >= 0)
                file.AppendLine(string.Format(C, "  the Home core fell after tick {0}, and the raid turned for home: before, {1}; after, {2}",
                    s.CoreFell, Part(s.Ms, 0, s.CoreFell), Part(s.Ms, s.CoreFell, s.Ms.Count)));
            file.AppendLine(string.Format(C,
                "  ticks over 16.7 ms (a 60 fps frame): {0} · over 50 ms (the tick's own game time): {1} · gen-0 collections: {2}",
                over16, over50, s.Gc0));
            file.AppendLine(string.Format(C, "  alive {0} → {1} · ticked bodies {2}–{3} · engineer down for {4} ticks",
                s.AliveFirst, s.AliveLast, s.AwakeMin, s.AwakeMax, s.EngineerDown));

            var phases = SimComposition.Phases;
            var order = new List<int>();
            for (var p = 0; p < phases.Count; p++) order.Add(p);
            order.Sort((x, y) => s.PhaseTicks[y].CompareTo(s.PhaseTicks[x]));
            double all = 0;
            foreach (var t in s.PhaseTicks) all += t;
            var parts = new List<string>();
            for (var k = 0; k < order.Count && k < 6; k++)
            {
                var p = order[k];
                var ms = s.PhaseTicks[p] * 1000.0 / Stopwatch.Frequency / a.Length;
                parts.Add(string.Format(C, "{0} {1:0.000} ({2:0}%)", phases[p].GetType().Name, ms,
                    all > 0 ? 100 * s.PhaseTicks[p] / all : 0));
            }
            file.AppendLine("  costliest phases, mean ms per tick: " + string.Join(" · ", parts));
            file.AppendLine();
        }

        /// <summary>Tick times <paramref name="from"/> to <paramref name="to"/>, in the tick order they ran.</summary>
        private static string Part(List<double> ms, int from, int to)
        {
            if (to <= from) return "no ticks";
            var a = ms.GetRange(from, to - from).ToArray();
            Array.Sort(a);
            double sum = 0;
            foreach (var v in a) sum += v;
            return string.Format(C, "mean {0:0.00} · median {1:0.00} · p95 {2:0.00} · max {3:0.00} ms over {4} ticks",
                sum / a.Length, a[a.Length / 2], a[Math.Min(a.Length - 1, (int)Math.Floor(.95 * a.Length))], a[a.Length - 1], a.Length);
        }

        /// <summary>Bodies the enemy phase actually works on this tick: every raid body, and a resident that is not asleep.</summary>
        private static int Awake(SimContext ctx, SimState st)
        {
            var n = 0;
            var p = st.Engineer.Pos;
            foreach (var e in st.Enemies.Actors)
            {
                if (e.Layer != EnemyLayer.Site) { n++; continue; }
                var far = DirectorRules.Distance(p.X, p.Y, e.Pos.X, e.Pos.Y) > ctx.Data.Siege.GuardSleepTiles;
                if (!(far && e.Phase == EnemyPhaseKind.Idle && !e.OnPlayer && !e.Withdrawing)) n++;
            }
            return n;
        }

        private static void Run(Simulation sim, int ticks)
        {
            for (var i = 0; i < ticks; i++)
            {
                sim.Tick();
                sim.State.Events.Clear();
            }
        }

        private static bool RunUntil(Simulation sim, double seconds, Func<bool> done)
        {
            var ticks = (int)(seconds * Simulation.TicksPerSecond);
            for (var i = 0; i < ticks; i++)
            {
                if (done()) return true;
                Run(sim, 1);
            }
            return done();
        }

        private static void Clear(SimState st)
        {
            st.Enemies.Actors.Clear();
            st.Director.Minor = null;
            st.Director.Major = null;
            st.Events.Clear();
        }

        // ================================================================== staging

        /// <summary>A body of <paramref name="kind"/> on the nearest free standable tile to the point, ringing out.</summary>
        private static int Settle(SimContext ctx, SimState st, double cx, double cy, int count, int layer, int group,
            Func<int, string> kind)
        {
            var placed = 0;
            var ox = (int)Math.Floor(cx);
            var oy = (int)Math.Floor(cy);
            for (var radius = 0; radius <= 12 && placed < count; radius++)
                for (var y = oy - radius; y <= oy + radius && placed < count; y++)
                    for (var x = ox - radius; x <= ox + radius && placed < count; x++)
                    {
                        if (Math.Max(Math.Abs(x - ox), Math.Abs(y - oy)) != radius) continue;
                        if (!Ground.CanStand(ctx, st, x + .5, y + .5, .3)) continue;
                        var taken = false;
                        foreach (var o in st.Enemies.Actors)
                            if (Math.Abs(o.Pos.X - (x + .5)) < .8 && Math.Abs(o.Pos.Y - (y + .5)) < .8) { taken = true; break; }
                        if (taken) continue;
                        var key = kind(placed);
                        Assert.That(ctx.Data.TryEnemy(key, out var def), Is.True, key);
                        var pos = new Vec2(x + .5, y + .5);
                        st.Enemies.Actors.Add(new Enemy
                        {
                            Id = st.Enemies.Next++, Kind = key, Hp = def.Hp, Pos = pos, Home = pos, Aim = pos,
                            Origin = y * ctx.Geometry.Width + x, Layer = layer, Group = group, Waypoint = -1,
                        });
                        placed++;
                    }
            return placed;
        }

        /// <summary>
        /// U-P-15's roamers, stood in by residents: packs of 2, 3 and 4 in turn, each at least 40 tiles from every
        /// other pack and 30 from the engineer, on street tiles picked in a seeded order.
        /// </summary>
        private static int StageRoamers(SimContext ctx, SimState st, int count, out int packs)
        {
            var w = ctx.Geometry.Width;
            var h = ctx.Geometry.Height;
            var spots = new List<(int X, int Y)>();
            for (var y = 4; y < h - 4; y += 7)
                for (var x = 4; x < w - 4; x += 7)
                    if (Ground.CanStand(ctx, st, x + .5, y + .5, .3)) spots.Add((x, y));
            Shuffle(spots, new System.Random(65));
            var chosen = new List<(int X, int Y)>();
            var placed = 0;
            packs = 0;
            var p = st.Engineer.Pos;
            foreach (var s in spots)
            {
                if (placed >= count) break;
                if (DirectorRules.Distance(p.X, p.Y, s.X + .5, s.Y + .5) < 30) continue;
                var clear = true;
                foreach (var c in chosen)
                    if (DirectorRules.Distance(c.X, c.Y, s.X, s.Y) < 40) { clear = false; break; }
                if (!clear) continue;
                var size = Math.Min(2 + packs % 3, count - placed);
                var n = Settle(ctx, st, s.X + .5, s.Y + .5, size, EnemyLayer.Site, GroupBase + packs, _ => "skitter");
                if (n == 0) continue;
                chosen.Add(s);
                placed += n;
                packs++;
            }
            return placed;
        }

        /// <summary>
        /// U-P-16 at the top of its ranges: the first authored camp 12, each later camp 22, shared across the camp's
        /// authored spawn groups (its marker when it has none); every third a spitter, as the raid roster.
        /// </summary>
        private static int StageGarrisons(SimContext ctx, SimState st, out string line)
        {
            var markers = new List<SiteRecord>();
            foreach (var s in ctx.Sites.OfKind(SiteKind.Camp))
                if (s.Id.IndexOf(":group:", StringComparison.Ordinal) < 0) markers.Add(s);
            var total = 0;
            var parts = new List<string>();
            for (var k = 0; k < markers.Count; k++)
            {
                var m = markers[k];
                var groups = new List<SiteRecord>();
                foreach (var s in ctx.Sites.OfKind(SiteKind.Camp))
                    if (s.Id.StartsWith(m.Id + ":group:", StringComparison.Ordinal)) groups.Add(s);
                if (groups.Count == 0) groups.Add(m);
                var want = k == 0 ? 12 : 22;
                var got = 0;
                for (var g = 0; g < groups.Count; g++)
                {
                    var share = want / groups.Count + (g < want % groups.Count ? 1 : 0);
                    var c = groups[g].Centre;
                    var start = got;
                    got += Settle(ctx, st, c.X, c.Y, share, EnemyLayer.Site, GroupBase + 1000 + k,
                        i => (start + i) % 3 == 2 ? "spitter" : "skitter");
                }
                total += got;
                parts.Add(string.Format(C, "{0} {1}/{2}", m.Name, got, want));
            }
            line = markers.Count + " camps: " + string.Join(", ", parts);
            return total;
        }

        /// <summary>
        /// The world cap as one small raid: the director's own staged group at its own approach, then the rest born
        /// on tiles 40 to 70 route steps from the Home core in a seeded order, all in that raid's group, so the
        /// director and the enemy phase treat every body as the raid's. A director-born body leaves by the approach it
        /// came in by, so unless <paramref name="ownExits"/> each staged body is given the raid's approach as its exit,
        /// as if it had walked in from there; with it, each keeps the tile it was born on.
        /// </summary>
        private static int StageAwake(SimContext ctx, SimState st, int cap, bool ownExits)
        {
            var origin = DirectorRules.Origin(ctx, st);
            Assert.That(origin, Is.GreaterThanOrEqualTo(0), "an approach");
            var id = Director.ScheduleGroup(ctx, st, origin, 20, false, 0, false);
            Assert.That(id, Is.Not.Zero, "the raid was staged");
            Assert.That(DirectorRules.Target(ctx, st, out var bx, out var by, out var size), Is.True);
            var field = st.Director.Fields.Field(ctx, st, bx, by, size, false);
            var w = ctx.Geometry.Width;
            var tiles = new List<int>();
            for (var y = 0; y < ctx.Geometry.Height; y++)
                for (var x = 0; x < w; x++)
                {
                    var steps = field.At(x, y);
                    if (steps >= 40 && steps <= 70 && Ground.CanStand(ctx, st, x + .5, y + .5, .3)) tiles.Add(y * w + x);
                }
            Shuffle(tiles, new System.Random(240));
            foreach (var t in tiles)
            {
                if (st.Enemies.Actors.Count >= cap) break;
                DirectorRules.Birth(ctx, st, t, EnemyLayer.Minor, id, false);
            }
            if (!ownExits)
                foreach (var e in st.Enemies.Actors) e.Origin = origin;
            st.Events.Clear();
            return st.Enemies.Actors.Count;
        }

        /// <summary><paramref name="count"/> gun turrets on a coarse grid across the city, away from Home: unpowered
        /// and empty, so each is a standing problem for the HUD scan.</summary>
        private static int Pad(SimContext ctx, SimState st, int count)
        {
            var placed = 0;
            var h = st.Home;
            for (var y = 20; y < ctx.Geometry.Height - 20 && placed < count; y += 23)
                for (var x = 20; x < ctx.Geometry.Width - 20 && placed < count; x += 23)
                {
                    if (DirectorRules.Distance(x, y, h.X, h.Y) < 40) continue;
                    if (RealCityFixture.PlaceNear(ctx, st, "turret", x, y, 3) == null) continue;
                    placed++;
                }
            return placed;
        }

        private static void Shuffle<T>(List<T> list, System.Random r)
        {
            for (var i = list.Count - 1; i > 0; i--)
            {
                var j = r.Next(i + 1);
                (list[i], list[j]) = (list[j], list[i]);
            }
        }

        // ================================================================== mask, route, HUD

        private static void Mask(StringBuilder file, SimContext ctx, SimState st)
        {
            // LightMaskPhase is the mask rebuild alone (LightPhase.Ensure); LightPhase also does the relief events.
            ITickPhase light = null;
            foreach (var p in SimComposition.Phases) if (p is LightMaskPhase) light = p;
            Assert.That(light, Is.Not.Null, "the mask phase is composed");
            var real = new List<double>();
            for (var i = 0; i < 10; i++)
            {
                var before = st.Light.Builds;
                st.Rev++;                                   // a placement moves Rev, which is what forces a rebuild
                var t0 = Stopwatch.GetTimestamp();
                light.Tick(ctx, st, Simulation.TickSeconds);
                real.Add((Stopwatch.GetTimestamp() - t0) * 1000.0 / Stopwatch.Frequency);
                Assert.That(st.Light.Builds, Is.EqualTo(before + 1), "the mask was rebuilt");
            }
            file.AppendLine("== mask");
            file.AppendLine("  real-city rebuild with the city's own lights, through LightMaskPhase: " + BestMean(real));

            // The offline test's 300 street discs (radius 7) and 60 floodlight cones (radius 12), on real streets.
            var w = ctx.Geometry.Width;
            var hgt = ctx.Geometry.Height;
            var spots = new List<(int X, int Y)>();
            for (var y = 4; y < hgt - 4; y += 5)
                for (var x = 4; x < w - 4; x += 5)
                    if (Ground.CanStand(ctx, st, x + .5, y + .5, .3)) spots.Add((x, y));
            Shuffle(spots, new System.Random(360));
            var lights = new List<Light>();
            for (var i = 0; i < 360 && i < spots.Count; i++)
                lights.Add(i < 300
                    ? new Light(spots[i].X + .5, spots[i].Y + .5, 7, LightKind.StreetLight, true)
                    : new Light(spots[i].X + .5, spots[i].Y + .5, 12, LightKind.Floodlight, true, Dir.E, Math.PI / 6));
            var mask = new byte[w * hgt];
            void Rebuild()
            {
                Array.Clear(mask, 0, mask.Length);
                for (var i = 0; i < lights.Count; i++)
                {
                    var l = lights[i];
                    LightRules.Stamp(mask, w, hgt, in l, ctx, st);
                }
            }
            Rebuild();
            var synthetic = new List<double>();
            for (var i = 0; i < 5; i++)
            {
                var t0 = Stopwatch.GetTimestamp();
                Rebuild();
                synthetic.Add((Stopwatch.GetTimestamp() - t0) * 1000.0 / Stopwatch.Frequency);
            }
            file.AppendLine(string.Format(C, "  {0} lights (300 discs r7, 60 cones r12) stamped with blocking on the real map: {1}",
                lights.Count, BestMean(synthetic)));
            file.AppendLine("  (the offline .NET 8 figure for the same 360 lights on a synthetic 864 × 864 map: 18.28 ms)");
            file.AppendLine();
        }

        private static void Routes(StringBuilder file, SimContext ctx, SimState st)
        {
            Assert.That(DirectorRules.Target(ctx, st, out var bx, out var by, out var size), Is.True, "the Home core is a raid target");
            var origin = DirectorRules.Origin(ctx, st);
            var w = ctx.Geometry.Width;
            List<double> Cold(int x, int y, int s, bool breach)
            {
                var ms = new List<double>();
                for (var i = 0; i < 5; i++)
                {
                    st.Rev++;                               // empties the field cache (it is keyed on Rev)
                    var t0 = Stopwatch.GetTimestamp();
                    var f = st.Director.Fields.Field(ctx, st, x, y, s, breach);
                    ms.Add((Stopwatch.GetTimestamp() - t0) * 1000.0 / Stopwatch.Frequency);
                    Assert.That(f, Is.Not.Null);
                }
                return ms;
            }
            file.AppendLine("== route (one cold field, the cache emptied first)");
            file.AppendLine("  to the Home core, ordinary: " + BestMean(Cold(bx, by, size, false)));
            file.AppendLine("  to the Home core, breach:   " + BestMean(Cold(bx, by, size, true)));
            file.AppendLine("  to an approach tile (a withdrawing body's field): " + BestMean(Cold(origin % w, origin / w, 0, false)));
            file.AppendLine("  A raid needs one ordinary field per target and a breach field only for a body the ordinary one cannot reach; they are cached until a placement or a mask rebuild.");
            file.AppendLine();
        }

        private static void HudScans(StringBuilder file, SimContext ctx, SimState st, string label)
        {
            var vm = new HudViewModel();
            var alerts = new DefenceAlertSource();
            vm.Refresh(ctx, st, st.T, false, false, force: true);
            alerts.Refresh(ctx, st);
            var a = new List<double>();
            var b = new List<double>();
            for (var i = 0; i < 20; i++)
            {
                var t0 = Stopwatch.GetTimestamp();
                vm.Refresh(ctx, st, st.T + i, false, false, force: true);
                a.Add((Stopwatch.GetTimestamp() - t0) * 1000.0 / Stopwatch.Frequency);
                t0 = Stopwatch.GetTimestamp();
                alerts.Refresh(ctx, st);
                b.Add((Stopwatch.GetTimestamp() - t0) * 1000.0 / Stopwatch.Frequency);
            }
            file.AppendLine("== HUD scans, every 150 ms in play (" + label + ")");
            file.AppendLine("  HudViewModel.Refresh (with CollectProblems): " + BestMean(a));
            file.AppendLine("  DefenceAlertSource.Refresh: " + BestMean(b));
            file.AppendLine();
        }

        private static string BestMean(List<double> ms)
        {
            double best = double.MaxValue, sum = 0, worst = 0;
            foreach (var v in ms)
            {
                best = Math.Min(best, v);
                worst = Math.Max(worst, v);
                sum += v;
            }
            return string.Format(C, "best {0:0.00} · mean {1:0.00} · worst {2:0.00} ms over {3} runs", best, sum / ms.Count, worst, ms.Count);
        }

        // ================================================================== what it ran on

        private static void Runtime(StringBuilder file)
        {
            var mono = Type.GetType("Mono.Runtime");
            var monoName = mono?.GetMethod("GetDisplayName", BindingFlags.NonPublic | BindingFlags.Static)?.Invoke(null, null) as string;
            file.AppendLine(string.Format(C, "runtime: {0} · CLR {1} · Unity {2} · {3} · editor code optimisation {4}",
                mono != null ? "Mono " + (monoName ?? "(version not reported)") : "not Mono",
                Environment.Version, Application.unityVersion, Application.isEditor ? "in the editor (EditMode)" : "player",
                UnityEditor.Compilation.CompilationPipeline.codeOptimization));
            file.AppendLine(string.Format(C, "machine: {0} · {1} logical processors at {2} MHz · {3} MB RAM · {4}",
                SystemInfo.processorType, SystemInfo.processorCount, SystemInfo.processorFrequency, SystemInfo.systemMemorySize,
                SystemInfo.operatingSystem));
            file.AppendLine("build: git " + GitHead() + " (HEAD when run; this measurement file may be uncommitted)");
        }

        private static string GitHead()
        {
            try
            {
                var git = Path.GetFullPath(Path.Combine(Application.dataPath, "..", "..", "..", ".git"));
                var head = File.ReadAllText(Path.Combine(git, "HEAD")).Trim();
                if (!head.StartsWith("ref: ", StringComparison.Ordinal)) return head.Substring(0, Math.Min(10, head.Length));
                var name = head.Substring(5);
                var loose = Path.Combine(git, name.Replace('/', Path.DirectorySeparatorChar));
                if (File.Exists(loose)) return File.ReadAllText(loose).Trim().Substring(0, 10) + " (" + name + ")";
                foreach (var line in File.ReadAllLines(Path.Combine(git, "packed-refs")))
                    if (line.EndsWith(" " + name, StringComparison.Ordinal)) return line.Substring(0, 10) + " (" + name + ")";
            }
            catch (Exception) { }
            return "unknown";
        }
    }
}
