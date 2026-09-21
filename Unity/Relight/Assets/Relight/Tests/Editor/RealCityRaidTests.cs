using System;
using System.Collections.Generic;
using NUnit.Framework;
using Relight.Data;
using Relight.Sim;
using Relight.World;
using UnityEditor;
using Object = UnityEngine.Object;

namespace Relight.Authoring.Tests
{
    /// <summary>
    /// E-18 on the REAL CITY. The flat 160×160 fixture pins the rules; the tracker's acceptance ("a long run on the
    /// real city never reports 'Previous assault cleanup is still unresolved' twice running") can only be shown on
    /// the map the game is played on, with its walls, streets and authored approaches, and with EVERY phase of the
    /// sim running — so this is the first real-city raid fixture (REL-85 widens it).
    ///
    /// The context is built exactly as <c>WorldBootstrap.ContextForLayout(version, useScene: false)</c> builds it
    /// for a new game: the same four assets World.unity points at, the same opening resource layout, the same
    /// balance tables. It needs the asset database, so it lives in the Editor test assembly and the offline dotnet
    /// suite cannot run it.
    ///
    /// The one liberty taken is the CLOCK: the first large raid is ~25 minutes into a new game, so each cycle pulls
    /// <c>NextStart</c> in to one warning from now. Nothing else about the raid is touched — the warning, the
    /// commit, the waves, the walk in, the damage and the ending are the game's own.
    /// </summary>
    public sealed class RealCityRaidTests
    {
        private const string Geometry = "Assets/Relight/World/Generated/Full/FullGeometry.asset";
        private const string Sites = "Assets/Relight/World/Generated/Full/FullSites.asset";
        private const string Overrides = "Assets/Relight/World/Manual/HomeSitesOverrides.asset";
        private const string Registry = "Assets/Relight/Data/GameDataRegistry.asset";
        private const string Unresolved = "Previous assault cleanup is still unresolved";

        private WorldGeometryAsset _opening;

        [TearDown]
        public void Teardown()
        {
            if (_opening != null) Object.DestroyImmediate(_opening);
            _opening = null;
        }

        private Simulation NewGameOnTheRealCity(int seed)
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
            _opening = OpeningResourceLayout.Build(geometry, siteList, out siteList);
            var map = ImportedGeometry.Build(_opening);
            Assert.That(map, Is.Not.Null, "the full-city geometry asset did not build");
            Assert.That((map.Width, map.Height), Is.EqualTo((864, 576)), "this must be the real city, not a crop");

            var ctx = new SimContext(registry.Build(), map, threat: new EnemyThreatLayer(), sites: siteList, mapId: _opening.MapId);
            var sim = Simulation.NewGame(ctx, seed);
            sim.State.OpeningResourceVersion = OpeningResourceLayout.Version;
            return sim;
        }

        /// <summary>Tick until <paramref name="done"/> or the time runs out; every raid notice is kept in <paramref name="notices"/>.</summary>
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

        private static int MajorBodies(SimState st)
        {
            DirectorQueries.Live(st, out var major, out _, out _);
            return major;
        }

        private static string Tail(List<string> notices) =>
            "\nraid notices:\n  " + string.Join("\n  ", notices);

        /// <summary>
        /// Two lost large raids in a row on an undefended Home, with the core recommissioned in between. Each one
        /// must END, let the core be repaired, and leave the director free to schedule and COMMIT the next.
        /// </summary>
        [Test, Timeout(900000)]
        public void ALostLargeRaidEnds_TheCoreCanBeRepaired_AndTheNextOneIsScheduled_TwiceRunning()
        {
            var sim = NewGameOnTheRealCity(1);
            var ctx = sim.Context;
            var st = sim.State;
            var d = st.Director;
            var r = ctx.Data.Raids;
            var notices = new List<string>();
            Assert.That(HomeQueries.CoreOperational(st), Is.True, "the real city places a Home core");
            Assert.That(st.Home.Fallback, Is.False, "on its authored site, not the synthetic fall-back");

            RunUntil(sim, 1, notices, () => false);      // the director seeds its clock on the first tick
            for (var cycle = 1; cycle <= 2; cycle++)
            {
                var label = "cycle " + cycle + ": ";
                d.NextStart = Math.Max(st.T + r.WarningS + 1, d.RecoveryUntil + 1);

                Assert.That(RunUntil(sim, r.WarningS + 30, notices, () => d.Major != null && d.Major.Committed), Is.True,
                    label + "the large raid was warned and committed" + Tail(notices));
                var id = d.Major.Id;
                var history = d.History.Count;

                // Nobody defends. The raid walks in through the real streets and takes the core down.
                Assert.That(RunUntil(sim, 900, notices, () => !HomeQueries.CoreOperational(st) || d.Major == null), Is.True,
                    label + "an undefended core falls to a large raid" + Tail(notices));
                Assert.That(HomeQueries.CoreOperational(st), Is.False,
                    label + "the raid ended some other way before it could win" + Tail(notices));
                notices.Add(st.T.ToString("0") + " test: " + label + "the core fell with " + MajorBodies(st) + " raiders alive, engineer down " + st.Engineer.IsDown);

                // ENM-01/02: it ENDS — within a tick, with its survivors still on the map.
                Assert.That(RunUntil(sim, 1, notices, () => d.Major == null), Is.True, label + "a raid that has won is over");
                Assert.That(d.History.Count, Is.EqualTo(Math.Min(32, history + 1)));
                var rec = d.History[d.History.Count - 1];
                Assert.That(rec.Id, Is.EqualTo(id));
                Assert.That(rec.Outcome, Is.EqualTo((int)RaidOutcome.Lost));
                Assert.That(d.NextStart, Is.GreaterThanOrEqualTo(st.T + r.IntervalMinS - 1), label + "the next waits a full interval");

                // The survivors walk off through the real city; any that cannot are removed at the purge time.
                Assert.That(RunUntil(sim, DirectorRules.WithdrawPurgeS + 5, notices, () => MajorBodies(st) == 0), Is.True,
                    label + MajorBodies(st) + " survivors never left");
                notices.Add(st.T.ToString("0") + " test: " + label + "the last survivor was gone");

                // ENM-03: the core can be recommissioned. The engineer may have been killed by the raid; wait for them.
                Assert.That(RunUntil(sim, 120, notices, () => !st.Engineer.IsDown), Is.True, label + "the engineer got back up");
                st.Engineer.Pos = new Vec2(st.Home.X - 0.5, st.Home.Y + st.Home.H / 2.0);
                st.Engineer.Inv[ItemId.Steel] = Math.Max(st.Engineer.Inv[ItemId.Steel], ctx.Data.Defence.CoreSteel);
                st.Engineer.Inv[ItemId.Copper] = Math.Max(st.Engineer.Inv[ItemId.Copper], ctx.Data.Defence.CoreCopper);
                Assert.That(HomeCore.RepairProblem(ctx, st, RepairKinds.Core, 0), Is.Empty, label + "the fallen core can be repaired");
                Assert.That(sim.Apply(new RepairCommand(RepairKinds.Core, 0)).Problem, Is.Empty);
                Assert.That(RunUntil(sim, ctx.Data.Defence.CoreRepairSeconds + 5, notices, () => HomeQueries.CoreOperational(st)), Is.True,
                    label + "the recommission finished");
            }

            // And a third is scheduled on the ordinary clock's terms.
            d.NextStart = Math.Max(st.T + r.WarningS + 1, d.RecoveryUntil + 1);
            Assert.That(RunUntil(sim, r.WarningS + 30, notices, () => d.Major != null && d.Major.Committed), Is.True,
                "a third large raid was warned and committed" + Tail(notices));

            var unresolved = notices.FindAll(n => n.Contains(Unresolved));
            Assert.That(unresolved, Is.Empty, "the director never had an unresolved assault in its way" + Tail(notices));
        }
    }
}
