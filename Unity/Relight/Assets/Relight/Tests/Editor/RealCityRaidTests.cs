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
    /// E-18 on the REAL CITY. The flat 160×160 fixture pins the rules; the tracker's acceptance ("a long run on the
    /// real city never reports 'Previous assault cleanup is still unresolved' twice running") can only be shown on
    /// the map the game is played on, with its walls, streets and authored approaches, and with EVERY phase of the
    /// sim running — so this is the first real-city raid fixture. REL-85 (CMB-09c) widens it with a DEFENDED Home
    /// run across three large-raid cycles that writes the raid log (<see cref="RaidLog"/>) as its output.
    ///
    /// The context is <see cref="RealCityFixture"/>'s: the city exactly as a new game builds it.
    ///
    /// The one liberty taken is the CLOCK: the first large raid is ~25 minutes into a new game, so each cycle pulls
    /// <c>NextStart</c> in to one warning from now, waiving REL-75's pacing holds as the Admin pull-in does
    /// (<see cref="RealCityFixture.PullInLargeRaid"/>). Nothing else about the raid is touched — the warning, the
    /// commit, the waves, the walk in, the damage and the ending are the game's own.
    /// </summary>
    public sealed class RealCityRaidTests
    {
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
            var ctx = RealCityFixture.Context(out _opening);
            var sim = Simulation.NewGame(ctx, seed);
            sim.State.OpeningResourceVersion = OpeningResourceLayout.Version;
            return sim;
        }

        /// <summary>
        /// REL-85: what a measured run watches beside the notices. The raid log is fed each tick's events as
        /// <c>SimHost</c> feeds it each frame's; the rest is the test's own check on the log.
        /// </summary>
        private sealed class Measure
        {
            public readonly RaidLog Log = new RaidLog();
            /// <summary>Every raid that reached the ground: a committed large raid, a small raid whose bodies arrived.</summary>
            public readonly HashSet<int> Raids = new HashSet<int>();
            public int Ended, PeakMajor, PeakLiving;
        }

        /// <summary>Tick until <paramref name="done"/> or the time runs out; every raid notice is kept in <paramref name="notices"/>.</summary>
        private static bool RunUntil(Simulation sim, double seconds, List<string> notices, Func<bool> done, Measure m = null)
        {
            var ticks = (int)(seconds * Simulation.TicksPerSecond);
            for (var i = 0; i < ticks; i++)
            {
                if (done()) return true;
                sim.Tick();
                var st = sim.State;
                var ev = st.Events;
                for (var k = 0; k < ev.Count; k++)
                {
                    if (ev[k] is RaidNoticeEvent n) notices.Add(n.T.ToString("0") + " " + n.Kind + ": " + n.Text);
                    else if (m != null && ev[k] is RaidEndedEvent) m.Ended++;
                }
                if (m != null)
                {
                    m.Log.Observe(sim.Context, st, ev);
                    var d = st.Director;
                    if (d.Major != null && d.Major.Committed) m.Raids.Add(d.Major.Id);
                    if (d.Minor != null && d.Minor.Spawned) m.Raids.Add(d.Minor.Id);
                    DirectorQueries.Live(st, out var major, out _, out var total);
                    m.PeakMajor = Math.Max(m.PeakMajor, major);
                    m.PeakLiving = Math.Max(m.PeakLiving, total);
                }
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
        /// ENM-03's repair, by the player's own command: the engineer is stood at the core's west face with the
        /// materials, and the core is repaired (a fallen one recommissioned) to full. Returns false when there was
        /// nothing to repair.
        /// </summary>
        private static bool RepairTheCore(Simulation sim, List<string> notices, string label, Measure m = null)
        {
            var ctx = sim.Context;
            var st = sim.State;
            if (HomeQueries.CoreHp(st) >= ctx.Data.Defence.CoreHp) return false;
            Assert.That(RunUntil(sim, 120, notices, () => !st.Engineer.IsDown, m), Is.True, label + "the engineer got back up");
            st.Engineer.Pos = new Vec2(st.Home.X - 0.5, st.Home.Y + st.Home.H / 2.0);
            st.Engineer.Inv[ItemId.Steel] = Math.Max(st.Engineer.Inv[ItemId.Steel], ctx.Data.Defence.CoreSteel);
            st.Engineer.Inv[ItemId.Copper] = Math.Max(st.Engineer.Inv[ItemId.Copper], ctx.Data.Defence.CoreCopper);
            Assert.That(HomeCore.RepairProblem(ctx, st, RepairKinds.Core, 0), Is.Empty, label + "the core can be repaired");
            Assert.That(sim.Apply(new RepairCommand(RepairKinds.Core, 0)).Problem, Is.Empty);
            Assert.That(RunUntil(sim, ctx.Data.Defence.CoreRepairSeconds + 5, notices,
                () => st.Home.RepairKind == RepairKinds.None && HomeQueries.CoreOperational(st), m), Is.True,
                label + "the repair finished");
            return true;
        }

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
                RealCityFixture.PullInLargeRaid(ctx, st);       // REL-75: waives the pacing holds too

                Assert.That(RunUntil(sim, RealCityFixture.WarningWait(ctx), notices, () => d.Major != null && d.Major.Committed), Is.True,
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
                Assert.That(RunUntil(sim, ctx.Data.Siege.WithdrawPurgeS + 5, notices, () => MajorBodies(st) == 0), Is.True,
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
            RealCityFixture.PullInLargeRaid(ctx, st);
            Assert.That(RunUntil(sim, RealCityFixture.WarningWait(ctx), notices, () => d.Major != null && d.Major.Committed), Is.True,
                "a third large raid was warned and committed" + Tail(notices));

            var unresolved = notices.FindAll(n => n.Contains(Unresolved));
            Assert.That(unresolved, Is.Empty, "the director never had an unresolved assault in its way" + Tail(notices));
        }

        /// <summary>Where the defended run writes its log: the project's ignored <c>Logs/</c> folder, never the
        /// evidence tree (REL-85: kept outside <c>Unity/Docs/evidence/</c> until the owner approves adding it).</summary>
        public static string DefenceRunLogPath =>
            Path.GetFullPath(Path.Combine(UnityEngine.Application.dataPath, "..", "Logs", "REL-85-real-city-defence-run.txt"));

        /// <summary>
        /// REL-85 (CMB-09c, E-19): a DEFENDED Home on the real city faces three large raids, every phase of the sim
        /// running, and every raid that reaches the ground leaves exactly one log line. The defence is the fixture's
        /// ring (<see cref="RealCityFixture.Defend"/>: eight gun turrets on a pole ring, two fuelled generators),
        /// restored to full between raids as a player's resupply and repair would leave it, and the core is repaired
        /// by the player's own command. The raids themselves are the game's own; only the clock is pulled in, as in
        /// the test above. Whatever each raid does — cleared, lost or broken off — is what the log records; the test
        /// asserts the log and the budgets, not a balance verdict.
        /// </summary>
        [Test, Timeout(2400000)]
        public void ADefendedHomeFacesThreeLargeRaids_AndEveryRaidLeavesOneLogLine()
        {
            const int seed = 1;
            var sim = NewGameOnTheRealCity(seed);
            var ctx = sim.Context;
            var st = sim.State;
            var d = st.Director;
            var r = ctx.Data.Raids;
            var notices = new List<string>();
            var m = new Measure();
            var c = CultureInfo.InvariantCulture;
            Assert.That(st.Home.Fallback, Is.False, "on its authored site, not the synthetic fall-back");

            var defence = RealCityFixture.Defend(ctx, st);
            Assert.That(defence.Generators.Count, Is.EqualTo(2), "both generators found a legal footprint");
            Assert.That(defence.Turrets.Count, Is.GreaterThanOrEqualTo(6), "most of the turret ring found a legal footprint");
            RunUntil(sim, 1, notices, () => false, m);                 // the director seeds its clock; power settles
            foreach (var t in defence.Turrets)
                Assert.That(PowerQueries.Supplied(ctx, st, t.Id), Is.True, "turret " + t.Id + " is on the powered ring");

            var cycles = new List<string>();
            for (var cycle = 1; cycle <= 3; cycle++)
            {
                var label = "cycle " + cycle + ": ";
                // A turret at the Home starts the opening's introductory encounter (GP-OPENING), which holds the
                // director until it is over. The run lets it play out — it is a raid, so it is logged — and lets any
                // small raid on the ground finish, then pulls the large raid in.
                Assert.That(RunUntil(sim, 600, notices, () => !d.Reserved && (d.Minor == null || !d.Minor.Spawned), m), Is.True,
                    label + "the director came free for a large raid" + Tail(notices));
                defence.Restore(ctx, st);
                RealCityFixture.PullInLargeRaid(ctx, st);       // REL-75: waives the pacing holds too

                Assert.That(RunUntil(sim, RealCityFixture.WarningWait(ctx), notices, () => d.Major != null && d.Major.Committed, m), Is.True,
                    label + "the large raid was warned and committed" + Tail(notices));
                var id = d.Major.Id;
                var coreAtStart = HomeQueries.CoreHp(st);

                Assert.That(RunUntil(sim, 2400, notices, () => d.Major == null, m), Is.True,
                    label + "the large raid ended within 40 minutes" + Tail(notices));
                var rec = d.History[d.History.Count - 1];
                Assert.That(rec.Id, Is.EqualTo(id), label + "the raid that ended is the one that committed");
                var rounds = 0;
                var standing = 0;
                foreach (var t in defence.Turrets)
                {
                    rounds += t.Rounds;
                    if (!TurretRules.Wrecked(ctx.Data, st, t)) standing++;
                }
                cycles.Add(string.Format(c,
                    "cycle {0}: raid #{1} {2} after {3:0.0} s · core {4:0} → {5:0} HP · {6}/{7} turrets standing · " +
                    "{8} rounds left in hoppers · {9:0.0} coal left",
                    cycle, id, ((RaidOutcome)rec.Outcome).ToString().ToLowerInvariant(), rec.Ended - rec.Started,
                    coreAtStart, HomeQueries.CoreHp(st), standing, defence.Turrets.Count, rounds, defence.Coal));

                // The survivors (if any) walk off; any that cannot are removed at the purge time.
                Assert.That(RunUntil(sim, ctx.Data.Siege.WithdrawPurgeS + 5, notices, () => MajorBodies(st) == 0, m), Is.True,
                    label + MajorBodies(st) + " survivors never left");
                RepairTheCore(sim, notices, label, m);
            }

            // Let any small raid still on the ground finish, so every raid that reached it has had its chance to end.
            RunUntil(sim, 300, notices, () => d.Minor == null || !d.Minor.Spawned, m);

            var ids = new HashSet<int>();
            var large = 0;
            foreach (var line in m.Log.Lines)
            {
                Assert.That(ids.Add(line.RaidId), Is.True, "raid #" + line.RaidId + " has more than one line");
                if (line.Major) large++;
            }

            var file = new StringBuilder();
            file.AppendLine("REL-85 (CMB-09c) real-city defence run — written by " + nameof(ADefendedHomeFacesThreeLargeRaids_AndEveryRaidLeavesOneLogLine));
            file.AppendLine("Not evidence: kept outside Unity/Docs/evidence/ until the owner approves adding it.");
            file.AppendLine(string.Format(c, "map {0} · seed {1} · data {2} · sim T {3:0.0} s at the end",
                ctx.MapId, seed, GameDataHash.Compute(ctx.Data), st.T));
            file.AppendLine(string.Format(c,
                "defence: {0} gun turrets, {1} poles, {2} generators ({3} ideal spots skipped); restored to full before each raid; core repaired by command between raids",
                defence.Turrets.Count, defence.Poles.Count, defence.Generators.Count, defence.Skipped));
            file.AppendLine(string.Format(c, "peak large-raid bodies alive {0} (budget {1}) · peak living bodies {2} (budget {3})",
                m.PeakMajor, r.ActiveRaidBudget, m.PeakLiving, r.LivingBudget));
            file.AppendLine();
            foreach (var row in cycles) file.AppendLine(row);
            file.AppendLine();
            file.AppendLine(RaidLog.Header);
            foreach (var line in m.Log.Lines) file.AppendLine(line.Text);
            Directory.CreateDirectory(Path.GetDirectoryName(DefenceRunLogPath));
            File.WriteAllText(DefenceRunLogPath, file.ToString());
            TestContext.WriteLine(file.ToString());
            TestContext.WriteLine("written to " + DefenceRunLogPath);

            Assert.That(m.Log.Watching, Is.EqualTo(0), "no raid is left half-tallied" + Tail(notices));
            Assert.That(m.Log.Lines.Count, Is.EqualTo(m.Ended), "one line per raid-ended event");
            foreach (var raid in m.Raids)
                Assert.That(ids.Contains(raid), Is.True, "raid #" + raid + " reached the ground and has no line" + Tail(notices));
            Assert.That(ids.Count, Is.EqualTo(m.Raids.Count), "every line is a raid that reached the ground");
            Assert.That(large, Is.GreaterThanOrEqualTo(3), "three large-raid cycles, three large lines");
            Assert.That(m.PeakMajor, Is.LessThanOrEqualTo(r.ActiveRaidBudget), "the 48 active-raid budget held");
            Assert.That(m.PeakLiving, Is.LessThanOrEqualTo(r.LivingBudget), "the 240 living budget held");
            Assert.That(notices.FindAll(n => n.Contains(Unresolved)), Is.Empty,
                "the director never had an unresolved assault in its way" + Tail(notices));
        }
    }
}
