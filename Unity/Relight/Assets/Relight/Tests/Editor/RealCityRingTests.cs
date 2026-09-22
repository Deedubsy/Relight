using System;
using System.Collections.Generic;
using NUnit.Framework;
using Relight.Sim;
using Relight.World;
using Object = UnityEngine.Object;

namespace Relight.Authoring.Tests
{
    /// <summary>
    /// REL-115 (E-24) on the REAL CITY. The tracker's acceptance: "A raid ringed by chests on the real-city fixture
    /// reaches the core; a raid ringed by walls still does". The flat fixture (<c>WreckRulesTests</c>) pins the rules;
    /// this shows them on the map the game is played on, with every phase of the sim running and the game's own
    /// large raid. As in <see cref="RealCityRaidTests"/>, the one liberty taken is the clock
    /// (<see cref="RealCityFixture.PullInLargeRaid"/>).
    ///
    /// A ring is only a test if it is CLOSED. Each run first shows the Home open to the street, then lays the ring
    /// and shows, by the raid's own passability rule (<see cref="DirectorRules.HostileOpen"/>), that nothing on foot
    /// can get from where a raider bites the core to the street past it. The check steps diagonally as well as
    /// straight, so it is stricter than any body's walk.
    ///
    /// The ring is measured from the raid's TARGET (<see cref="DirectorRules.Target"/>), not from the Home's own
    /// rectangle. The target is a square as wide as the Home's longer side, so for the 10×14 Home it runs four tiles
    /// past the core's east wall, and a raider standing beside that square bites the core. A ring drawn tight to the
    /// Home's east wall is therefore inside the target, and the first version of this test watched a skitter damage
    /// the core from outside an unbroken wall ring. That is a defect of its own (Linear REL-124), not REL-115's, and
    /// is left for the owner; this test rings the square so that it measures only what REL-115 changed.
    /// </summary>
    public sealed class RealCityRingTests
    {
        /// <summary>How far out the flood looks for the street: past this many tiles beyond the ring, it got out.</summary>
        private const int Beyond = 10;

        private WorldGeometryAsset _opening;

        [TearDown]
        public void Teardown()
        {
            if (_opening != null) Object.DestroyImmediate(_opening);
            _opening = null;
        }

        private Simulation NewGameOnTheRealCity(int seed)
        {
            var ctx = RealCityFixture.Context(out _opening);
            var sim = Simulation.NewGame(ctx, seed);
            sim.State.OpeningResourceVersion = OpeningResourceLayout.Version;
            return sim;
        }

        private static bool RunUntil(Simulation sim, double seconds, List<string> notices, Func<bool> done)
        {
            var ticks = (int)(seconds * Simulation.TicksPerSecond);
            for (var i = 0; i < ticks; i++)
            {
                if (done()) return true;
                sim.Tick();
                var ev = sim.State.Events;
                for (var k = 0; k < ev.Count; k++)
                    if (ev[k] is RaidNoticeEvent n) notices.Add(n.T.ToString("0") + " " + n.Kind + ": " + n.Text);
                ev.Clear();
            }
            return done();
        }

        private static string Tail(List<string> notices) => "\nraid notices:\n  " + string.Join("\n  ", notices);

        /// <summary>The raid's target square: where its route field is seeded and what a bite beside it damages.</summary>
        private static (int X, int Y, int Size) Target(SimContext ctx, SimState st)
        {
            Assert.That(DirectorRules.Target(ctx, st, out var x, out var y, out var size), Is.True, "the Home core is a target");
            return (x, y, size);
        }

        private static bool Inside((int X, int Y, int Size) t, int x, int y) =>
            x >= t.X && x < t.X + t.Size && y >= t.Y && y < t.Y + t.Size;

        /// <summary>
        /// Can something on foot get from beside the target square, where a raider bites the core, to
        /// <paramref name="reach"/> tiles out from it? A flood over the tiles <see cref="DirectorRules.HostileOpen"/>
        /// calls open, eight ways, never through the square itself (the route field never enters it either).
        /// </summary>
        private static bool Escapes(SimContext ctx, SimState st, int reach)
        {
            var t = Target(ctx, st);
            int x0 = t.X - reach, y0 = t.Y - reach, x1 = t.X + t.Size - 1 + reach, y1 = t.Y + t.Size - 1 + reach;
            var seen = new HashSet<long>();
            var open = new Queue<(int X, int Y)>();
            for (var y = t.Y - 1; y <= t.Y + t.Size; y++)
                for (var x = t.X - 1; x <= t.X + t.Size; x++)
                {
                    if (Inside(t, x, y) || !DirectorRules.HostileOpen(ctx, st, x, y)) continue;
                    if (seen.Add(((long)y << 20) | (uint)x)) open.Enqueue((x, y));
                }
            while (open.Count > 0)
            {
                var (x, y) = open.Dequeue();
                if (x <= x0 || y <= y0 || x >= x1 || y >= y1) return true;
                for (var dy = -1; dy <= 1; dy++)
                    for (var dx = -1; dx <= 1; dx++)
                    {
                        int nx = x + dx, ny = y + dy;
                        if ((dx == 0 && dy == 0) || Inside(t, nx, ny) || !DirectorRules.HostileOpen(ctx, st, nx, ny)) continue;
                        if (seen.Add(((long)ny << 20) | (uint)nx)) open.Enqueue((nx, ny));
                    }
            }
            return false;
        }

        /// <summary>
        /// A closed ring of <paramref name="kind"/> round the raid's target square: every tile
        /// <paramref name="offset"/> out from the square that a body could stand on is covered by one, placed where
        /// the game's own placement rule accepts it (<see cref="RealCityFixture.PlaceNear"/>). The first offset from
        /// two out whose ring the flood cannot get past is kept; a ring that leaves a gap is lifted again and the next
        /// offset tried.
        ///
        /// The ring is laid by walking round it. A piece wider than a tile (a 2×2 chest) takes the tile it is on and
        /// the next one along, and hangs outward, away from the square; laid row by row instead, two chests can leave
        /// a single tile between them that no chest fits. Where that spot is refused, the nearest accepted footprint
        /// that covers the tile is used.
        /// </summary>
        private static List<Machine> Ring(SimContext ctx, SimState st, string kind, out int offset)
        {
            Assert.That(ctx.Data.TryMachine(kind, out var spec), Is.True, kind + " is not in the catalogue");
            var t = Target(ctx, st);
            for (offset = 2; offset <= 9; offset++)
            {
                var ring = new List<Machine>();
                int x0 = t.X - offset, y0 = t.Y - offset, x1 = t.X + t.Size - 1 + offset, y1 = t.Y + t.Size - 1 + offset;
                var walk = Walk(x0, y0, x1, y1);
                for (var i = 0; i < walk.Count; i++)
                {
                    var (tx, ty) = walk[i];
                    if (!DirectorRules.HostileOpen(ctx, st, tx, ty)) continue;     // a city wall or a piece already down
                    Machine m = null;
                    if (spec.Size == 2 && i + 1 < walk.Count)
                    {
                        var (nx, ny) = walk[i + 1];
                        int cx = Math.Min(tx, nx), cy = Math.Min(ty, ny);
                        if (ty == ny) cy = ty == y0 ? y0 - 1 : y1;                  // along a row: hang out of the ring
                        else cx = tx == x0 ? x0 - 1 : x1;                           // down a column: the same
                        if (!Overlaps(t, cx, cy, spec.Size))
                            m = RealCityFixture.PlaceNear(ctx, st, kind, cx, cy, 0);
                    }
                    if (m == null)
                        m = RealCityFixture.PlaceNear(ctx, st, kind, tx, ty, spec.Size - 1,
                            (cx, cy) => cx <= tx && tx < cx + spec.Size && cy <= ty && ty < cy + spec.Size
                                && !Overlaps(t, cx, cy, spec.Size));
                    if (m != null) ring.Add(m);
                }
                if (!Escapes(ctx, st, offset + Beyond)) return ring;
                foreach (var m in ring) st.Machines.Remove(m);
                st.Rev++;
            }
            Assert.Fail("no offset from 2 to 9 tiles gave a closed ring of " + kind + " round the raid's target");
            return null;
        }

        /// <summary>The ring's tiles in order round it: east along the top, south, west along the bottom, north.</summary>
        private static List<(int X, int Y)> Walk(int x0, int y0, int x1, int y1)
        {
            var walk = new List<(int X, int Y)>();
            for (var x = x0; x <= x1; x++) walk.Add((x, y0));
            for (var y = y0 + 1; y <= y1; y++) walk.Add((x1, y));
            for (var x = x1 - 1; x >= x0; x--) walk.Add((x, y1));
            for (var y = y1 - 1; y > y0; y--) walk.Add((x0, y));
            return walk;
        }

        /// <summary>A footprint that reaches into the target square or the seed ring beside it.</summary>
        private static bool Overlaps((int X, int Y, int Size) t, int x, int y, int size) =>
            x < t.X + t.Size + 1 && x + size > t.X - 1 && y < t.Y + t.Size + 1 && y + size > t.Y - 1;

        /// <summary>
        /// The whole acceptance, once for chests and once for walls. A large raid meets a closed ring, breaks a piece
        /// of it (a chest BLOCKS its path, which is exactly when U-D-69 (h) lets it break a machine) and damages the
        /// core. For chests, every broken one still holds the five plates it was given (U-D-69 (f)).
        /// </summary>
        [TestCase("chest", 5)]
        [TestCase("wall", 0)]
        [Timeout(900000)]
        public void ARaidRingedOnTheRealCityBreaksThroughAndReachesTheCore(string kind, int plates)
        {
            var sim = NewGameOnTheRealCity(1);
            var ctx = sim.Context;
            var st = sim.State;
            var d = st.Director;
            var notices = new List<string>();
            Assert.That(st.Home.Fallback, Is.False, "on its authored site, not the synthetic fall-back");
            Assert.That(HomeQueries.CoreOperational(st), Is.True);
            Assert.That(Escapes(ctx, st, 2 + Beyond), Is.True, "before the ring, the Home is open to the street");

            var ring = Ring(ctx, st, kind, out var offset);
            Assert.That(ring, Is.Not.Empty);
            foreach (var m in ring)
            {
                Assert.That(TurretRules.BlocksRaiders(ctx.Data, m), Is.True, kind + " is a thing a raider must break to pass");
                if (plates > 0) m.Inv.Add(ItemId.Steel, plates);
            }
            var t = Target(ctx, st);
            TestContext.WriteLine($"{kind}: {ring.Count} pieces, {offset} tiles out from the raid's {t.Size}×{t.Size} target " +
                $"at ({t.X}, {t.Y}) round the {st.Home.W}×{st.Home.H} Home, {TurretRules.MaxHp(ctx.Data, ring[0])} HP each");

            RunUntil(sim, 1, notices, () => false);          // the director seeds its clock on the first tick
            RealCityFixture.PullInLargeRaid(ctx, st);
            Assert.That(RunUntil(sim, RealCityFixture.WarningWait(ctx), notices, () => d.Major != null && d.Major.Committed),
                Is.True, "the large raid was warned and committed" + Tail(notices));

            var full = HomeQueries.CoreHp(st);
            var start = st.T;
            Assert.That(RunUntil(sim, 900, notices, () => HomeQueries.CoreHp(st) < full), Is.True,
                "the ring kept the raid off the core" + Tail(notices));

            var broken = ring.FindAll(m => TurretRules.Wrecked(ctx.Data, st, m));
            TestContext.WriteLine($"{kind}: core first damaged {st.T - start:0} s after the commit; " +
                $"{broken.Count} of {ring.Count} pieces broken by then");
            Assert.That(broken, Is.Not.Empty, "it got to the core by breaking the ring");
            foreach (var m in broken)
                Assert.That(m.Inv[ItemId.Steel], Is.EqualTo((double)plates), "the wreck kept what was in it");
        }
    }
}
