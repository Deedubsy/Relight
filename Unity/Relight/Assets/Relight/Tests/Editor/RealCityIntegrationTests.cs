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
    /// IC-1 (IMPLEMENTATION-READINESS.md): the whole raid loop on the REAL CITY, in one run. "On the real city, force
    /// a major and lose on purpose. The raid ends, the core can be repaired, the next raid is scheduled. Die during
    /// it and recover the pile. Save mid-major, load, carry on."
    ///
    /// The pieces are each pinned elsewhere, most of them on the flat fixture: the ending (E-18,
    /// <see cref="RealCityRaidTests"/>), the pile (GP-W5), the save (B-11). This test is the one place they meet on
    /// the map the game is played on, with every phase of the sim running.
    ///
    /// Liberties taken, and nothing else: the CLOCK (the first large raid is pulled in to one warning from now), the
    /// engineer's POSITION at two moments (stood on the first raider to come near the core, because a raider bites
    /// only what is in its reach, and at the core's face for the repair — the walk back to the pile is walked, by
    /// the player's own <see cref="MoveCommand"/>), and the repair
    /// MATERIALS, topped up as a player's supply would. The raid, the death, the pile, the save and the repair are
    /// the game's own.
    /// </summary>
    public sealed class RealCityIntegrationTests
    {
        private WorldGeometryAsset _opening;

        [TearDown]
        public void Teardown()
        {
            if (_opening != null) Object.DestroyImmediate(_opening);
            _opening = null;
        }

        /// <summary>Where the run writes its log: the project's ignored <c>Logs/</c> folder, never the evidence tree.</summary>
        public static string LogPath =>
            Path.GetFullPath(Path.Combine(UnityEngine.Application.dataPath, "..", "Logs", "IC-1-real-city-run.txt"));

        private static string F(double v) => v.ToString("0.0", CultureInfo.InvariantCulture);

        /// <summary>Tick until <paramref name="done"/> or the time runs out, keeping every raid notice.</summary>
        private static bool RunUntil(Simulation sim, double seconds, List<string> log, Func<bool> done)
        {
            var ticks = (int)(seconds * Simulation.TicksPerSecond);
            for (var i = 0; i < ticks; i++)
            {
                if (done()) return true;
                sim.Tick();
                var ev = sim.State.Events;
                for (var k = 0; k < ev.Count; k++)
                {
                    if (ev[k] is RaidNoticeEvent n) log.Add(F(n.T) + " notice " + n.Kind + ": " + n.Text);
                    else if (ev[k] is EngineerDownEvent down) log.Add(F(down.T) + " engineer down at (" + F(down.X) + ", " + F(down.Y) + ")");
                    else if (ev[k] is EngineerUpEvent up) log.Add(F(up.T) + " engineer up");
                    else if (ev[k] is CargoDroppedEvent c) log.Add(F(c.T) + " cargo dropped: pile " + c.Id + " at (" + c.X + ", " + c.Y + ")");
                    else if (ev[k] is RaidEndedEvent end)
                        log.Add(F(end.T) + " raid " + end.RaidId + " ended: " + (RaidOutcome)end.Outcome
                                + (end.Major ? " (large)" : " (small)") + ", core " + F(HomeQueries.CoreHp(sim.State)) + " HP");
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

        private static Dictionary<string, double> Snapshot(ItemBag bag)
        {
            var keys = new List<ItemKey>();
            bag.Keys(keys);
            var into = new Dictionary<string, double>(StringComparer.Ordinal);
            for (var i = 0; i < keys.Count; i++) if (!keys[i].IsWeapon && bag[keys[i]] > 0) into[keys[i].Key] = bag[keys[i]];
            return into;
        }

        private static string Show(Dictionary<string, double> items)
        {
            var parts = new List<string>();
            foreach (var kv in items) parts.Add(kv.Key + " " + kv.Value.ToString("0.##", CultureInfo.InvariantCulture));
            parts.Sort(StringComparer.Ordinal);
            return parts.Count == 0 ? "nothing" : string.Join(", ", parts);
        }

        private static string Tail(List<string> log) => "\nrun log:\n  " + string.Join("\n  ", log);

        [Test, Timeout(1200000)]
        public void IC1_OnTheRealCity_ALostRaid_ADeathAndItsPile_AndASaveMidRaid_AllCarryOn()
        {
            var ctx = RealCityFixture.Context(out _opening);
            var sim = Simulation.NewGame(ctx, 1);
            var st = sim.State;
            st.OpeningResourceVersion = OpeningResourceLayout.Version;
            var d = st.Director;
            var r = ctx.Data.Raids;
            var log = new List<string>();
            var started = DateTime.UtcNow;
            Assert.That(HomeQueries.CoreOperational(st), Is.True, "the real city places a Home core");
            Assert.That(st.Home.Fallback, Is.False, "on its authored site, not the synthetic fall-back");

            RunUntil(sim, 1, log, () => false);           // the director seeds its clock on the first tick

            // Cargo worth walking back for. Steel and copper are what the repair will want, so the pile matters.
            st.Engineer.Inv[ItemId.Steel] = st.Engineer.Inv[ItemId.Steel] + 7;
            st.Engineer.Inv[ItemId.Copper] = st.Engineer.Inv[ItemId.Copper] + 3;
            st.Engineer.Inv[ItemId.Coal] = st.Engineer.Inv[ItemId.Coal] + 11;
            var carried = Snapshot(st.Engineer.Inv);
            log.Add(F(st.T) + " carrying " + Show(carried));

            // 1. Force a large raid: the clock is pulled in, the warning and commit are the game's own.
            RealCityFixture.PullInLargeRaid(ctx, st);           // REL-75: waives the pacing holds too
            Assert.That(RunUntil(sim, RealCityFixture.WarningWait(ctx), log, () => d.Major != null && d.Major.Committed), Is.True,
                "the large raid was warned and committed" + Tail(log));
            var raidId = d.Major.Id;
            var history = d.History.Count;
            log.Add(F(st.T) + " large raid " + raidId + " committed, " + d.Major.Total + " planned");

            // 2. Die during it. A raider bites only what is in its reach (EnemyPhase.Decide), so the unarmed engineer
            // is stood where the first raider to come within ActIn tiles of the core is standing, and left there.
            const double ActIn = 12;
            Assert.That(DirectorRules.Target(ctx, st, out var gx, out var gy, out var gw, out var gh), Is.True, "the raid has a target");
            var coreX = gx + gw / 2.0;
            var coreY = gy + gh / 2.0;
            log.Add(F(st.T) + " raid target at (" + gx + ", " + gy + ") " + gw + "x" + gh + "; Home rect (" + st.Home.X + ", " + st.Home.Y
                    + ") " + st.Home.W + "x" + st.Home.H);
            Enemy first = null;
            var lookAt = st.T;
            Assert.That(RunUntil(sim, 600, log, () =>
            {
                first = null;
                var nearest = double.MaxValue;
                var actors = st.Enemies.Actors;
                for (var i = 0; i < actors.Count; i++)
                {
                    if (actors[i].Layer != EnemyLayer.Major) continue;
                    var dist = DirectorRules.Distance(actors[i].Pos.X, actors[i].Pos.Y, coreX, coreY);
                    nearest = Math.Min(nearest, dist);
                    if (first == null && dist <= ActIn) first = actors[i];
                }
                if (st.T >= lookAt)
                {
                    lookAt = st.T + 5;
                    log.Add(F(st.T) + " look: " + MajorBodies(st) + " raiders, nearest " + F(nearest) + " tiles from the target, core "
                            + F(HomeQueries.CoreHp(st)) + " HP");
                }
                return first != null || d.Major == null;
            }), Is.True, "a raider came near the core" + Tail(log));
            Assert.That(first, Is.Not.Null, "the raid ended before any raider reached the core" + Tail(log));
            st.Engineer.Pos = first.Pos;
            log.Add(F(st.T) + " engineer stood on a raider at (" + F(first.Pos.X) + ", " + F(first.Pos.Y) + "), "
                    + F(DirectorRules.Distance(first.Pos.X, first.Pos.Y, coreX, coreY)) + " tiles from the core");
            Assert.That(RunUntil(sim, 300, log, () => st.Engineer.IsDown || d.Major == null), Is.True,
                "the raid reached the engineer" + Tail(log));
            Assert.That(st.Engineer.IsDown, Is.True, "the raid ended before it killed the engineer" + Tail(log));
            Assert.That(d.Major, Is.Not.Null, "the raid is still on when the engineer goes down");
            Assert.That(DeathCache.Live(st, out var pile), Is.EqualTo(1), "one pile where the engineer fell" + Tail(log));
            var pileId = pile.Id;
            var inPile = Snapshot(pile.Items);
            foreach (var kv in carried)
                Assert.That(inPile.TryGetValue(kv.Key, out var n) ? n : 0, Is.EqualTo(kv.Value).Within(1e-9),
                    "the pile holds every " + kv.Key + " carried");
            Assert.That(Snapshot(st.Engineer.Inv), Is.Empty, "nothing but weapons stays in the pockets");
            log.Add(F(st.T) + " pile " + pileId + " at (" + pile.X + ", " + pile.Y + ") holds " + Show(inPile)
                    + "; " + MajorBodies(st) + " raiders alive; core " + F(HomeQueries.CoreHp(st)) + " HP");

            // 3. Save mid-major, with the engineer down and the pile on the ground; load it onto the same city.
            // The game's own pair: the bytes SaveStore writes (with the map id and the region) and the read it loads with.
            var bytes = SaveSerializer.Write(st, ctx, null, out var header);
            var loaded = SaveSerializer.Read(bytes, ctx);
            Assert.That(loaded.Ok, Is.True, loaded.Reason);
            Assert.That(StateHash.Compute(loaded.State), Is.EqualTo(StateHash.Compute(st)), "the load is the state saved");
            var resumed = Simulation.Wrap(ctx, loaded.State);
            log.Add(F(st.T) + " saved mid-raid (" + bytes.Length + " bytes, schema " + header.Version + ") and loaded");

            // Carry on: the saved game and the loaded one run on side by side and stay the same game.
            const double SideBySideS = 30;
            var sideBySide = (int)(SideBySideS * Simulation.TicksPerSecond);
            for (var i = 0; i < sideBySide; i++)
            {
                sim.Tick(); st.Events.Clear();
                resumed.Tick(); resumed.State.Events.Clear();
            }
            Assert.That(StateHash.Compute(resumed.State), Is.EqualTo(StateHash.Compute(st)),
                "the loaded game plays on exactly as the saved one does, " + SideBySideS + " s later");
            log.Add(F(st.T) + " after " + SideBySideS + " s side by side the saved and loaded games hash the same");

            // From here the LOADED game is the one played.
            sim = resumed;
            st = sim.State;
            d = st.Director;
            Assert.That(d.Major, Is.Not.Null, "the raid survived the save");
            Assert.That(d.Major.Id, Is.EqualTo(raidId));

            // 4. Lose on purpose: nobody defends, the core falls, and the raid ENDS.
            Assert.That(RunUntil(sim, 900, log, () => !HomeQueries.CoreOperational(st) || d.Major == null), Is.True,
                "an undefended core falls to a large raid" + Tail(log));
            Assert.That(HomeQueries.CoreOperational(st), Is.False, "the raid ended some other way before it could win" + Tail(log));
            Assert.That(RunUntil(sim, 1, log, () => d.Major == null), Is.True, "a raid that has won is over");
            var rec = d.History[d.History.Count - 1];
            Assert.That(d.History.Count, Is.EqualTo(Math.Min(32, history + 1)));
            Assert.That(rec.Id, Is.EqualTo(raidId));
            Assert.That(rec.Outcome, Is.EqualTo((int)RaidOutcome.Lost));
            Assert.That(d.NextStart, Is.GreaterThanOrEqualTo(st.T + r.IntervalMinS - 1), "the next waits a full interval");
            log.Add(F(st.T) + " raid " + raidId + " lost; next large raid booked for " + F(d.NextStart));

            Assert.That(RunUntil(sim, ctx.Data.Siege.WithdrawPurgeS + 5, log, () => MajorBodies(st) == 0), Is.True,
                MajorBodies(st) + " survivors never left" + Tail(log));
            log.Add(F(st.T) + " the last survivor was gone");

            // 5. Recover the pile: the engineer, up again at the spawn, WALKS back to it on the real streets.
            Assert.That(RunUntil(sim, 120, log, () => !st.Engineer.IsDown), Is.True, "the engineer got back up");
            pile = DeathCache.Find(st, pileId);
            Assert.That(pile, Is.Not.Null, "the pile is still on the ground after the raid" + Tail(log));
            Assert.That(sim.Apply(new MoveCommand(pile.X + 0.5, pile.Y + 0.5)).Problem, Is.Empty);
            var walkFrom = st.Engineer.Pos;
            Assert.That(RunUntil(sim, 300, log, () => st.Engineer.IsDown || DeathCache.InReach(ctx, st, pile)), Is.True,
                "the engineer walked back to the pile from (" + F(walkFrom.X) + ", " + F(walkFrom.Y) + ")" + Tail(log));
            Assert.That(st.Engineer.IsDown, Is.False, "nothing killed the engineer on the walk back");
            log.Add(F(st.T) + " walked from (" + F(walkFrom.X) + ", " + F(walkFrom.Y) + ") to the pile");
            var collected = sim.Apply(new CollectCacheCommand(pileId));
            Assert.That(collected.Accepted, Is.True, collected.Problem);
            Assert.That(DeathCache.Find(st, pileId), Is.Null, "the whole pile fits the Backpack it came out of");
            var back = Snapshot(st.Engineer.Inv);
            foreach (var kv in inPile)
                Assert.That(back.TryGetValue(kv.Key, out var n) ? n : 0, Is.GreaterThanOrEqualTo(kv.Value - 1e-9),
                    "every " + kv.Key + " came back");
            log.Add(F(st.T) + " collected pile " + pileId + " (\"" + collected.Problem + "\"); carrying " + Show(back));

            // 6. Repair the fallen core by the player's own command.
            st.Engineer.Pos = new Vec2(st.Home.X - 0.5, st.Home.Y + st.Home.H / 2.0);
            st.Engineer.Inv[ItemId.Steel] = Math.Max(st.Engineer.Inv[ItemId.Steel], ctx.Data.Defence.CoreSteel);
            st.Engineer.Inv[ItemId.Copper] = Math.Max(st.Engineer.Inv[ItemId.Copper], ctx.Data.Defence.CoreCopper);
            Assert.That(HomeCore.RepairProblem(ctx, st, RepairKinds.Core, 0), Is.Empty, "the fallen core can be repaired");
            Assert.That(sim.Apply(new RepairCommand(RepairKinds.Core, 0)).Problem, Is.Empty);
            Assert.That(RunUntil(sim, ctx.Data.Defence.CoreRepairSeconds + 5, log, () => HomeQueries.CoreOperational(st)), Is.True,
                "the recommission finished" + Tail(log));
            log.Add(F(st.T) + " core recommissioned at " + F(HomeQueries.CoreHp(st)) + " HP");

            // 7. The next large raid is scheduled and commits on the ordinary clock's terms.
            RealCityFixture.PullInLargeRaid(ctx, st);
            Assert.That(RunUntil(sim, RealCityFixture.WarningWait(ctx), log, () => d.Major != null && d.Major.Committed), Is.True,
                "the next large raid was warned and committed" + Tail(log));
            Assert.That(d.Major.Id, Is.Not.EqualTo(raidId));
            log.Add(F(st.T) + " next large raid " + d.Major.Id + " committed");

            WriteLog(log, started);
        }

        private static void WriteLog(List<string> log, DateTime started)
        {
            var sb = new StringBuilder();
            sb.AppendLine("IC-1 real-city run — " + DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm", CultureInfo.InvariantCulture) + " UTC");
            sb.AppendLine("Seed 1, the full 864x576 city, every sim phase running. Wall clock "
                          + F((DateTime.UtcNow - started).TotalSeconds) + " s.");
            sb.AppendLine("Liberties: the raid clock is pulled in; the engineer is placed twice (on the first raider near the core, then at the core for the repair); repair materials topped up.");
            sb.AppendLine();
            for (var i = 0; i < log.Count; i++) sb.AppendLine(log[i]);
            var path = LogPath;
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            File.WriteAllText(path, sb.ToString());
        }
    }
}
