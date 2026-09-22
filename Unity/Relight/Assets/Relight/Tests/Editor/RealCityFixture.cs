using System;
using System.Collections.Generic;
using NUnit.Framework;
using Relight.Data;
using Relight.Sim;
using Relight.World;
using UnityEditor;

namespace Relight.Authoring.Tests
{
    /// <summary>
    /// The REAL CITY as a sim context, for tests that can only be shown on the map the game is played on.
    ///
    /// The context is built exactly as <c>WorldBootstrap.ContextForLayout(version, useScene: false)</c> builds it
    /// for a new game: the same four assets World.unity points at, the same opening resource layout, the same
    /// balance tables. It needs the asset database, so it lives in the Editor test assembly and the offline dotnet
    /// suite cannot run it.
    /// </summary>
    public static class RealCityFixture
    {
        public const string Geometry = "Assets/Relight/World/Generated/Full/FullGeometry.asset";
        public const string Sites = "Assets/Relight/World/Generated/Full/FullSites.asset";
        public const string Overrides = "Assets/Relight/World/Manual/HomeSitesOverrides.asset";
        public const string Registry = "Assets/Relight/Data/GameDataRegistry.asset";

        /// <summary>
        /// The context. <paramref name="opening"/> is a geometry asset made for it: the caller destroys it
        /// (<c>Object.DestroyImmediate</c>) when the test is over.
        /// </summary>
        public static SimContext Context(out WorldGeometryAsset opening)
        {
            var geometry = AssetDatabase.LoadAssetAtPath<WorldGeometryAsset>(Geometry);
            var sites = AssetDatabase.LoadAssetAtPath<HomeSitesAsset>(Sites);
            var overrides = AssetDatabase.LoadAssetAtPath<HomeSitesOverrides>(Overrides);
            var registry = AssetDatabase.LoadAssetAtPath<GameDataRegistry>(Registry);
            Assert.That(geometry, Is.Not.Null, Geometry);
            Assert.That(sites, Is.Not.Null, Sites);
            Assert.That(overrides, Is.Not.Null, Overrides);
            Assert.That(registry, Is.Not.Null, Registry);

            var siteList = SiteBridge.ToSim(HomeSites.Resolve(sites, overrides), geometry.RegionId, geometry.OriginX, geometry.OriginY);
            opening = OpeningResourceLayout.Build(geometry, siteList, out siteList);
            var map = ImportedGeometry.Build(opening);
            Assert.That(map, Is.Not.Null, "the full-city geometry asset did not build");
            Assert.That((map.Width, map.Height), Is.EqualTo((864, 576)), "this must be the real city, not a crop");
            return new SimContext(registry.Build(), map, threat: new EnemyThreatLayer(), sites: siteList, mapId: opening.MapId);
        }

        // ------------------------------------------------------------------ REL-85: a defence on the real streets

        /// <summary>The machines <see cref="Defend"/> placed, so a run can refill and repair them between raids.</summary>
        public sealed class Defence
        {
            public readonly List<Machine> Turrets = new List<Machine>();
            public readonly List<Machine> Poles = new List<Machine>();
            public readonly List<Machine> Generators = new List<Machine>();
            /// <summary>Ideal spots the placement rule refused within the search radius, for the run's record.</summary>
            public int Skipped;

            /// <summary>Full hoppers, full fuel, no damage: what a player's resupply and repair between raids leave.</summary>
            public void Restore(SimContext ctx, SimState st)
            {
                foreach (var t in Turrets)
                {
                    TurretRules.TurretRepairHook(ctx, st, t.Id, 1e9);
                    t.Rounds = TurretHopper.Capacity(ctx.Data, t);
                }
                foreach (var p in Poles) TurretRules.TurretRepairHook(ctx, st, p.Id, 1e9);
                foreach (var g in Generators)
                {
                    TurretRules.TurretRepairHook(ctx, st, g.Id, 1e9);
                    g.Inv[ItemId.Coal] = MachineInventory.GeneratorFuelCap(ctx.Data);
                }
            }

            /// <summary>Coal left in all the generators together.</summary>
            public double Coal
            {
                get { double c = 0; foreach (var g in Generators) c += g.Inv[ItemId.Coal]; return c; }
            }
        }

        /// <summary>
        /// A ring of gun turrets around the Home core on the real streets: <paramref name="turrets"/> turrets at
        /// <paramref name="ringTiles"/> outside the core's edge, a pole ring two tiles out that they hang off, and
        /// <paramref name="generators"/> fuelled generators beside the first poles (two, so a full hopper of coal
        /// outlasts one raid cycle and power is not what the run measures by accident). Every footprint is one the
        /// game's own placement rule accepts (<see cref="Placement.GeometryProblem"/>) and that keeps a tile clear of
        /// the core, found as the nearest such spot to the ideal one; a spot with none within
        /// <paramref name="searchTiles"/> is skipped and counted. Nothing is charged for: the run measures the
        /// defence, not the economy that paid for it.
        /// </summary>
        public static Defence Defend(SimContext ctx, SimState st, int turrets = 8, int ringTiles = 5, int generators = 2,
            int searchTiles = 4)
        {
            var home = st.Home;
            var cx = home.X + home.W / 2.0;
            var cy = home.Y + home.H / 2.0;
            var d = new Defence();
            const int poles = 8;
            for (var k = 0; k < poles; k++)
            {
                var a = 2 * Math.PI * k / poles;
                var r = home.W / 2.0 + 2;
                var p = Place(ctx, st, "pole", cx + r * Math.Cos(a), cy + r * Math.Sin(a), searchTiles);
                if (p == null) { d.Skipped++; continue; }
                d.Poles.Add(p);
                if (d.Generators.Count < generators)
                {
                    var g = Place(ctx, st, "generator", p.X + 2, p.Y, searchTiles);
                    if (g == null) d.Skipped++;
                    else d.Generators.Add(g);
                }
            }
            for (var k = 0; k < turrets; k++)
            {
                var a = 2 * Math.PI * k / turrets;
                var r = home.W / 2.0 + ringTiles;
                var t = Place(ctx, st, "turret", cx + r * Math.Cos(a), cy + r * Math.Sin(a), searchTiles);
                if (t == null) { d.Skipped++; continue; }
                d.Turrets.Add(t);
            }
            d.Restore(ctx, st);
            return d;
        }

        /// <summary>The nearest legal footprint for <paramref name="kind"/> to (<paramref name="x"/>, <paramref name="y"/>), or null.</summary>
        private static Machine Place(SimContext ctx, SimState st, string kind, double x, double y, int searchTiles)
        {
            Assert.That(ctx.Data.TryMachine(kind, out var spec), Is.True, kind + " is not in the catalogue");
            return PlaceNear(ctx, st, kind, (int)Math.Round(x - spec.Size / 2.0), (int)Math.Round(y - spec.Size / 2.0),
                searchTiles);
        }

        /// <summary>
        /// REL-88: the nearest footprint to the north-west corner (<paramref name="ox"/>, <paramref name="oy"/>) that
        /// the game's placement rule accepts, keeps a tile clear of the core and passes <paramref name="accept"/>
        /// (given the corner), or null. Placed free, as <see cref="Defend"/> places.
        /// </summary>
        public static Machine PlaceNear(SimContext ctx, SimState st, string kind, int ox, int oy, int searchTiles,
            Func<int, int, bool> accept = null, Dir dir = Dir.N)
        {
            Assert.That(ctx.Data.TryMachine(kind, out var spec), Is.True, kind + " is not in the catalogue");
            int bx = 0, by = 0;
            var best = double.MaxValue;
            for (var dy = -searchTiles; dy <= searchTiles; dy++)
                for (var dx = -searchTiles; dx <= searchTiles; dx++)
                {
                    var dist = dx * dx + dy * dy;
                    if (dist >= best) continue;
                    if (TouchesCore(st, ox + dx, oy + dy, spec.Size)) continue;
                    if (Placement.GeometryProblem(ctx, st, kind, ox + dx, oy + dy, dir) != "") continue;
                    if (accept != null && !accept(ox + dx, oy + dy)) continue;
                    best = dist; bx = ox + dx; by = oy + dy;
                }
            if (best == double.MaxValue) return null;
            var m = new Machine { Id = st.NextId++, Kind = kind, X = bx, Y = by, Dir = dir, Size = spec.Size };
            st.Machines.Add(m);
            st.Rev++;
            return m;
        }

        /// <summary>
        /// The placement rule checks machines and city walls, not the core's own rectangle, so the fixture keeps
        /// its machines a tile clear of it: stricter than the game, never looser.
        /// </summary>
        public static bool TouchesCore(SimState st, int x, int y, int size)
        {
            var h = st.Home;
            return x < h.X + h.W + 1 && x + size > h.X - 1 && y < h.Y + h.H + 1 && y + size > h.Y - 1;
        }

        /// <summary>
        /// The one liberty the real-city raid runs take: pull the next large raid in to one warning from now (never
        /// inside the recovery spell). REL-75 added pacing holds that are only a matter of time — the quiet spell,
        /// the wait for the cycle's small raid and the gap after it — so they are waived here exactly as the Admin
        /// pull-in waives them (<see cref="Director.PullIn"/>). A small raid still on the map keeps holding the
        /// warning, as it does in the game; wait <see cref="WarningWait"/> for the commit.
        /// </summary>
        public static void PullInLargeRaid(SimContext ctx, SimState st)
        {
            var d = st.Director;
            var siege = ctx.Data.Siege;
            d.NextStart = Math.Max(st.T + ctx.Data.Raids.WarningS + 1, d.RecoveryUntil + 1);
            d.QuietUntil = Math.Min(d.QuietUntil, st.T);
            d.CycleMinors = Math.Max(d.CycleMinors, siege.MinorsPerCycle);
            if (d.LastMinorEnd >= 0 && d.LastMinorEnd > st.T - siege.MinorMajorGapS) d.LastMinorEnd = st.T - siege.MinorMajorGapS;
        }

        /// <summary>
        /// How long a pulled-in large raid may take to be warned and committed: the full warning, plus, when a small
        /// raid was already announced, its own longest warning, the REL-75 hold for it and the gap after it.
        /// </summary>
        public static double WarningWait(SimContext ctx)
        {
            var siege = ctx.Data.Siege;
            return ctx.Data.Raids.WarningS + 30 + siege.MinorHoldCapS + siege.MinorMajorGapS + 120;
        }
    }
}
