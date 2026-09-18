using System.Collections.Generic;
using NUnit.Framework;

namespace Relight.Sim.Tests.Combat
{
    /// <summary>
    /// The shared setup for the C-04 and C-08 checks.
    ///
    /// It deliberately does NOT reuse <c>SyntheticMap</c> (64×48). The director's own rules put an assault origin
    /// 20–60 tiles from the core, at least 28 tiles from the engineer, and its approach scan looks out to 68 tiles;
    /// on a 64×48 map every candidate is starved and a real failure would be indistinguishable from "the map is too
    /// small". This map is 160×160 of plain ground with a real line of sight, which is the smallest size on which
    /// the reference's verbatim radii all have room.
    /// </summary>
    public static class RaidFixture
    {
        public const double Dt = 1.0 / 20;
        public const int Size = 160;
        /// <summary>Where the tests put the Home core: the middle of the map.</summary>
        public const int CoreX = 76, CoreY = 76, CoreSize = 8;

        /// <summary>A flat ground map with an optional set of solid building tiles.</summary>
        public static ArrayGeometry Map(IEnumerable<TileRect> solids = null, int size = Size)
        {
            var kind = new byte[size * size];
            var solid = new bool[size * size];
            for (var i = 0; i < kind.Length; i++) kind[i] = (byte)TileClass.Ground;
            if (solids != null)
                foreach (var r in solids)
                    for (var y = r.Y; y < r.Y + r.H; y++)
                        for (var x = r.X; x < r.X + r.W; x++)
                            if (x >= 0 && y >= 0 && x < size && y < size) solid[y * size + x] = true;
            return new ArrayGeometry(size, size, kind, solid, new Vec2(size / 2.0, size / 2.0));
        }

        /// <summary>A context whose <see cref="WorldSites"/> names a Home core at <see cref="CoreX"/>,<see cref="CoreY"/>.</summary>
        public static SimContext Context(ArrayGeometry geometry = null, bool withCore = true)
        {
            var g = geometry ?? Map();
            WorldSites sites = null;
            if (withCore)
                sites = new WorldSites(new List<SiteRecord>
                {
                    new SiteRecord("home", "Home Court", SiteKind.Core, CoreX, CoreY, CoreSize, CoreSize, "", 0),
                });
            return new SimContext(ReferenceData.Create(), g, null, null, sites);
        }

        /// <summary>A fresh state with the engineer at the spawn and every C-04/C-08 initialiser run.</summary>
        public static SimState State(SimContext ctx, int seed = 7)
        {
            var st = new SimState();
            st.Seed = seed;
            st.Engineer.Pos = ctx.Geometry.Spawn;
            st.Engineer.Hp = ctx.Data.Engineer.MaxHp;
            new EnemyInitializer().Init(ctx, st);
            new DirectorInitializer().Init(ctx, st);
            return st;
        }

        /// <summary>The combat sub-slots in composition order, with power ahead of them so turrets can be supplied.</summary>
        public static List<ITickPhase> Phases() =>
            new List<ITickPhase> { new PowerPhase(), new TurretPhase(), new EnemyPhase(), new DirectorPhase() };

        public static void Run(SimContext ctx, SimState st, int ticks, List<ITickPhase> phases = null)
        {
            phases = phases ?? Phases();
            for (var i = 0; i < ticks; i++)
            {
                for (var p = 0; p < phases.Count; p++) phases[p].Tick(ctx, st, Dt);
                st.Tick++;
                st.T += Dt;
            }
        }

        /// <summary>Puts a machine on the map at its catalogue size, without charging a build cost.</summary>
        public static Machine Add(SimContext ctx, SimState st, string kind, int x, int y, Dir dir = Dir.N)
        {
            Assert.That(ctx.Data.TryMachine(kind, out var spec), Is.True, kind + " is not in the catalogue");
            var m = new Machine { Id = st.NextId++, Kind = kind, X = x, Y = y, Dir = dir, Size = spec.Size };
            st.Machines.Add(m);
            st.Rev++;
            return m;
        }

        /// <summary>A fuelled generator and a pole beside <paramref name="x"/>,<paramref name="y"/>.</summary>
        public static void Power(SimContext ctx, SimState st, int x, int y, int coal = 50)
        {
            Add(ctx, st, "pole", x, y);
            var gen = Add(ctx, st, "generator", x + 2, y);
            gen.Inv.Add(ItemId.Coal, coal);
        }

        /// <summary>A powered, loaded turret at <paramref name="x"/>,<paramref name="y"/> with its pole and generator.</summary>
        public static Machine Turret(SimContext ctx, SimState st, int x, int y, int rounds = 50)
        {
            var t = Add(ctx, st, "turret", x, y);
            Power(ctx, st, x + 2, y + 2);
            t.Rounds = rounds;
            return t;
        }

        /// <summary>
        /// A body placed by hand, bypassing the director, so a behaviour test controls exactly one thing. It also
        /// registers the wave it belongs to: a raid body whose group no longer exists is, correctly, a body whose
        /// orders have been cancelled — it withdraws on its first tick and leaves the map.
        /// </summary>
        public static Enemy Body(SimState st, string kind, double x, double y,
            int layer = EnemyLayer.Minor, int group = 1)
        {
            var e = new Enemy
            {
                Id = st.Enemies.Next++,
                Kind = kind,
                Pos = new Vec2(x, y),
                Layer = layer,
                Group = group,
                Home = new Vec2(x, y),
                Origin = (int)y * 160 + (int)x,
                Waypoint = -1,
            };
            e.Hp = kind == "spitter" ? 50 : 20;
            st.Enemies.Actors.Add(e);
            if (layer == EnemyLayer.Minor && st.Director.Minor == null)
                // A body placed by hand IS the wave — Spawned, owing nothing — so the director's "the last body is
                // gone, the raid is over" rule still fires for it (GP-W3 made that rule depend on arrival).
                st.Director.Minor = new MinorRaid { Id = group, Origin = e.Origin, Spawned = true, Owed = 0, StartsAt = st.T };
            if (layer == EnemyLayer.Major && st.Director.Major == null)
                st.Director.Major = new MajorRaid { Id = group, Origin = e.Origin, Committed = true };
            return e;
        }

        /// <summary>
        /// A camp resident: it patrols its birthplace instead of marching on the core, so a turret test can keep a
        /// live target inside its range for as long as it likes without freezing the movement code.
        /// </summary>
        public static Enemy Guard(SimState st, string kind, double x, double y, double hp = 1e9)
        {
            var e = Body(st, kind, x, y, EnemyLayer.Site, 99);
            e.Hp = hp;
            return e;
        }

        public static int Count<T>(SimState st) where T : SimEvent
        {
            var n = 0;
            for (var i = 0; i < st.Events.Count; i++) if (st.Events[i] is T) n++;
            return n;
        }

        public static T Last<T>(SimState st) where T : class
        {
            for (var i = st.Events.Count - 1; i >= 0; i--) if (st.Events[i] is T t) return t;
            return null;
        }
    }
}
