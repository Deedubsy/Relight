using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using NUnit.Framework;
using Relight.Sim.Tests.Persistence;

namespace Relight.Sim.Tests.Combat
{
    /// <summary>
    /// Batch 4, FRT-05 (REL-140): the Freight warehouse's doors, its garrison and its fight clock. The accept line:
    /// the doors stay shut with two keys and open with three; the garrison of 60 is born whole across six squads,
    /// the two inside squads shut in by the doors; the stronghold fight holds the large-raid warning and ends 30 s
    /// after the engineer leaves or goes down; the save round-trips the doors and the clock.
    ///
    /// The map is <see cref="RaidFixture"/>'s open 160-tile square with the real warehouse's walls stood on it at
    /// the real tiles (the ring x 56–85, y 39–61, open at the south door (65–67, 61) and the west door
    /// (56, 49–51)), and the real sites: the floor rect, the two doors and the six group points. Only the encounter
    /// phase runs, so nothing moves and nothing dies unless a test does it by hand.
    /// </summary>
    public sealed class StrongholdTests
    {
        const string Arena = "freight:arena";
        const int PerSecond = 20;                        // RaidFixture.Dt is a twentieth of a second
        const double Cx = 71, Cy = 50.5;                 // the middle of the floor rect (57, 40) 28×21

        static readonly (int X, int Y)[] Groups = { (45, 35), (45, 58), (98, 35), (98, 61), (62, 58), (79, 46) };

        static readonly TileRect[] Walls =
        {
            new TileRect(56, 39, 30, 1),                 // north
            new TileRect(56, 61, 9, 1), new TileRect(68, 61, 18, 1),   // south, either side of the door
            new TileRect(56, 40, 1, 9), new TileRect(56, 52, 1, 9),    // west, either side of the door
            new TileRect(85, 40, 1, 21),                 // east
        };

        static SimContext Context()
        {
            var sites = new List<SiteRecord>
            {
                new SiteRecord("home", "Home Court", SiteKind.Core, RaidFixture.CoreX, RaidFixture.CoreY,
                    RaidFixture.CoreSize, RaidFixture.CoreSize),
                new SiteRecord("freight:door:0", "Freight warehouse door 0", SiteKind.StrongholdDoor, 65, 61, 3, 1),
                new SiteRecord("freight:door:1", "Freight warehouse door 1", SiteKind.StrongholdDoor, 56, 49, 1, 3),
                new SiteRecord(Arena, "Freight warehouse floor", SiteKind.Arena, 57, 40, 28, 21, "", 60),
                new SiteRecord(Arena + ":guardian", "Freight warehouse guardian", SiteKind.Arena, 77, 50, 1, 1),
            };
            for (var i = 0; i < Groups.Length; i++)
                sites.Add(new SiteRecord(Arena + ":group:" + i, "group", SiteKind.Arena, Groups[i].X, Groups[i].Y, 1, 1));
            return new SimContext(ReferenceData.Create(), RaidFixture.Map(Walls), null, null, new WorldSites(sites));
        }

        static List<ITickPhase> Phase() => new List<ITickPhase> { new EncounterPhase() };

        static void At(SimState st, double x, double y) => st.Engineer.Pos = new Vec2(x, y);

        static void Tick(SimContext ctx, SimState st) => RaidFixture.Run(ctx, st, 1, Phase());

        static void Seconds(SimContext ctx, SimState st, double s) => RaidFixture.Run(ctx, st, (int)(s * PerSecond), Phase());

        static StrongholdDef Freight => EncounterCatalogue.Strongholds.Single(s => s.Id == "freight");

        static void Keys(SimState st, int n)
        {
            for (var i = 1; i <= n; i++) st.Encounters.Pouch.Add("freight:camp:" + i);
        }

        /// <summary>Inside the ring: the floor rect.</summary>
        static bool Inside(double x, double y) => x >= 57 && x < 85 && y >= 40 && y < 61;

        /// <summary>
        /// Every tile a body can walk to from (x, y) by the rule enemy movement uses, within the 160 square. The
        /// flood is how "the doors keep the inside squads in" is shown without running the enemies.
        /// </summary>
        static HashSet<(int, int)> Flood(SimContext ctx, SimState st, int x, int y, System.Func<int, int, bool> open)
        {
            var seen = new HashSet<(int, int)> { (x, y) };
            var q = new Queue<(int, int)>();
            q.Enqueue((x, y));
            while (q.Count > 0)
            {
                var (cx, cy) = q.Dequeue();
                for (var k = 0; k < 4; k++)
                {
                    var nx = cx + Dirs.DX[k];
                    var ny = cy + Dirs.DY[k];
                    if (!Ground.InBounds(ctx, nx, ny) || seen.Contains((nx, ny)) || !open(nx, ny)) continue;
                    seen.Add((nx, ny));
                    q.Enqueue((nx, ny));
                }
            }
            return seen;
        }

        /// <summary>The garrison born from 48 tiles south of the floor's middle, the engineer left there.</summary>
        static SimState Garrisoned(SimContext ctx)
        {
            var st = RaidFixture.State(ctx);
            At(st, Cx, Cy + 48);
            Tick(ctx, st);
            Assert.That(st.Encounters.Find(Arena).Resolved, Is.True);
            st.Events.Clear();
            return st;
        }

        [Test]
        public void TheDoorsStayShutWithTwoKeysAndOpenWithThree()
        {
            var ctx = Context();
            var st = RaidFixture.State(ctx);
            Keys(st, 2);
            At(st, 66.5, 64);                            // 2 tiles south of the south door
            Seconds(ctx, st, 2);
            Assert.That(st.Encounters.IsOpen("freight"), Is.False, "two keys do not open it");
            foreach (var (x, y) in new[] { (65, 61), (66, 61), (67, 61), (56, 49), (56, 50), (56, 51) })
            {
                Assert.That(Ground.Passable(ctx, st, x, y), Is.False, "door tile " + x + "," + y);
                Assert.That(Ground.PassableForEngineer(ctx, st, x, y), Is.False, "the engineer cannot walk a shut door either");
                Assert.That(DirectorRules.HostileOpen(ctx, st, x, y), Is.False, "nor can a body");
            }
            Assert.That(Flood(ctx, st, 66, 64, (x, y) => Ground.PassableForEngineer(ctx, st, x, y)).Contains((66, 60)),
                Is.False, "with the doors shut there is no way in");

            // The third key, 3.5 tiles off: still shut. Half a tile closer, both doors open.
            st.Encounters.Pouch.Add("freight:camp:3");
            At(st, 66.5, 65.5);
            Tick(ctx, st);
            Assert.That(st.Encounters.IsOpen("freight"), Is.False, "3.5 tiles is not within 3");
            var rev = st.Rev;
            At(st, 66.5, 64.9);
            Tick(ctx, st);
            Assert.That(st.Encounters.IsOpen("freight"), Is.True);
            Assert.That(st.Rev, Is.GreaterThan(rev), "opening moves the walk grid on");
            Assert.That(st.Events.OfType<StrongholdOpenedEvent>().Single().Text, Is.EqualTo("Freight warehouse doors open"));
            foreach (var (x, y) in new[] { (66, 61), (56, 50) })
            {
                Assert.That(Ground.Passable(ctx, st, x, y), Is.True, "both doors open together");
                Assert.That(DirectorRules.HostileOpen(ctx, st, x, y), Is.True);
            }
            Assert.That(Flood(ctx, st, 66, 64, (x, y) => Ground.PassableForEngineer(ctx, st, x, y)).Contains((66, 60)), Is.True);

            Seconds(ctx, st, 5);
            Assert.That(st.Encounters.Opened, Is.EqualTo(new[] { "freight" }), "opened once, and it stays open");
            Assert.That(st.Events.OfType<StrongholdOpenedEvent>().Count(), Is.EqualTo(1));

            // A downed engineer opens nothing, keys or not.
            var st2 = RaidFixture.State(ctx);
            Keys(st2, 3);
            At(st2, 66.5, 63);
            st2.Engineer.Down = st2.T + 100;
            Seconds(ctx, st2, 2);
            Assert.That(st2.Encounters.IsOpen("freight"), Is.False);
            Assert.That(StrongholdRules.KeysHeld(st2, Freight), Is.EqualTo(3));
        }

        [Test]
        public void TheGarrisonOfSixtyIsBornWholeFromFortyEightTilesAndTheInsideSquadsAreShutIn()
        {
            var ctx = Context();
            var st = RaidFixture.State(ctx);
            At(st, Cx, Cy + 48.5);
            Tick(ctx, st);
            Assert.That(st.Encounters.Find(Arena).Resolved, Is.False, "48.5 tiles is not yet 48");

            At(st, Cx, Cy + 48);
            Tick(ctx, st);
            Assert.That(st.Encounters.Find(Arena).Resolved, Is.True);
            Assert.That(st.Events.OfType<EncounterResolvedEvent>().Single().Bodies, Is.EqualTo(61), "60 and the guardian (FRT-06)");
            var all = st.Enemies.Actors.Where(e => e.Site == Arena).ToList();
            var guardian = all.Single(e => e.Kind == GuardianRules.Kind);
            Assert.That(Inside(guardian.Pos.X, guardian.Pos.Y), Is.True, "the guardian waits on the floor");
            var bodies = all.Where(e => e != guardian).ToList();
            Assert.That(bodies.Count, Is.EqualTo(60));
            Assert.That(bodies.Count(e => e.Kind == "spitter"), Is.EqualTo(12), "every fifth a spitter");
            var serial = EncounterCatalogue.Find(Arena).Serial;
            for (var g = 0; g < 6; g++)
            {
                var squad = bodies.Where(e => e.Group == EncounterCatalogue.GroupBase + serial * 100 + g).ToList();
                Assert.That(squad.Count, Is.EqualTo(10), "squad " + g);
                Assert.That(squad.Count(e => e.Kind == "spitter"), Is.EqualTo(2), "squad " + g);
                var inside = g >= 4;
                Assert.That(squad.All(e => Inside(e.Pos.X, e.Pos.Y) == inside), Is.True,
                    "squad " + g + (inside ? " waits on the floor" : " waits in the yard"));
            }

            // Nothing inside can walk out while the doors are shut, by the rule enemy movement uses.
            foreach (var e in bodies.Where(b => Inside(b.Pos.X, b.Pos.Y)))
            {
                var reach = Flood(ctx, st, (int)e.Pos.X, (int)e.Pos.Y, (x, y) => DirectorRules.HostileOpen(ctx, st, x, y));
                Assert.That(reach.All(t => Inside(t.Item1, t.Item2)), Is.True, "body " + e.Id + " found a way out");
            }
            Assert.That(EncounterPhase.ResolveTiles(EncounterCatalogue.Find(Arena)), Is.EqualTo(48));
            Assert.That(EncounterCatalogue.Find(Arena).Key, Is.Null, "the warehouse gives no key");
            Assert.That(EncounterCatalogue.Find(Arena).RepeatSeconds, Is.EqualTo(0), "and never refills");
        }

        [Test]
        public void TheFightHoldsTheWarningAndEndsThirtySecondsAfterTheEngineerLeaves()
        {
            var ctx = Context();
            var st = Garrisoned(ctx);
            Seconds(ctx, st, 1);
            Assert.That(DirectorPacing.StrongholdFight(ctx, st), Is.False, "37 tiles off with nobody chasing is no fight");

            At(st, Cx, 72);                              // 11 tiles south of the floor
            Tick(ctx, st);
            Assert.That(DirectorPacing.StrongholdFight(ctx, st), Is.True);
            Assert.That(DirectorPacing.WarningHold(ctx, st), Is.EqualTo("a stronghold fight is on"));
            Assert.That(DirectorPacing.Unfair(ctx, st), Is.True, "no small raid starts in the middle of it either");

            At(st, Cx, Cy + 48);
            Seconds(ctx, st, 29.5);
            Assert.That(DirectorPacing.StrongholdFight(ctx, st), Is.True, "29.5 s after leaving is not yet 30");
            Seconds(ctx, st, 1);
            Assert.That(DirectorPacing.StrongholdFight(ctx, st), Is.False);
            Assert.That(DirectorPacing.WarningHold(ctx, st), Is.Not.EqualTo("a stronghold fight is on"));

            // A body chasing the engineer is a fight wherever they are; one breaking off is not.
            var chaser = st.Enemies.Actors.First(e => e.Site == Arena);
            chaser.OnPlayer = true;
            Tick(ctx, st);
            Assert.That(DirectorPacing.StrongholdFight(ctx, st), Is.True);
            chaser.Withdrawing = true;
            Seconds(ctx, st, 30.5);
            Assert.That(DirectorPacing.StrongholdFight(ctx, st), Is.False);
            chaser.OnPlayer = false;
            chaser.Withdrawing = false;

            // Going down ends it the same way, though the engineer lies right beside the floor.
            At(st, Cx, 72);
            Tick(ctx, st);
            st.Engineer.Down = st.T + 1000;
            Seconds(ctx, st, 29.5);
            Assert.That(DirectorPacing.StrongholdFight(ctx, st), Is.True);
            Seconds(ctx, st, 1);
            Assert.That(DirectorPacing.StrongholdFight(ctx, st), Is.False, "a downed engineer is fighting nobody");

            // And a cleared warehouse is no fight at all.
            st.Engineer.Down = 0;
            st.Enemies.Actors.RemoveAll(e => e.Site == Arena);
            Seconds(ctx, st, 1);
            Assert.That(DirectorPacing.StrongholdFight(ctx, st), Is.False);
            Assert.That(st.Encounters.Find(Arena).ClearedAt, Is.GreaterThanOrEqualTo(0));
        }

        [Test]
        public void TheSaveRoundTripsTheDoorsAndTheFightClock()
        {
            var ctx = Context();
            var st = Garrisoned(ctx);
            Keys(st, 3);
            At(st, 66.5, 63);
            Seconds(ctx, st, 4);
            Assert.That(st.Encounters.IsOpen("freight"), Is.True);
            var until = st.Encounters.FightUntil;
            Assert.That(until, Is.GreaterThan(st.T));

            var load = SaveSerializer.ReadText(SaveSerializer.WriteText(st, ctx.Data), ctx.Data);
            Assert.That(load.Ok, Is.True, load.Reason);
            Assert.That(load.Header.Version, Is.EqualTo(SaveSchema.Version));
            var b = load.State;
            Assert.That(b.Encounters.Opened, Is.EqualTo(new[] { "freight" }));
            Assert.That(b.Encounters.FightUntil, Is.EqualTo(until));
            Assert.That(Ground.Passable(ctx, b, 66, 61), Is.True, "a loaded game has the doors it saved");
            Assert.That(StateHash.Compute(b), Is.EqualTo(StateHash.Compute(st)));
        }

        [Test]
        public void AVersionFourteenSaveUpgradesWithTheDoorsShutAndNoFight()
        {
            var ctx = Context();
            var st = RaidFixture.State(ctx);
            var state = PersistenceFixture.Canonical(st);
            Assert.That(Regex.Matches(state, "\"opened\"").Count, Is.EqualTo(1));
            Assert.That(Regex.Matches(state, "\"fightUntil\"").Count, Is.EqualTo(1));
            var state14 = Regex.Replace(state, "\"opened\":\\[[^\\]]*\\],?", "");
            state14 = Regex.Replace(state14, "\"fightUntil\":[-0-9.eE]+,?", "");
            state14 = state14.Replace(",}", "}");
            Assert.That(state14, Does.Not.Contain("\"opened\""));
            Assert.That(state14, Does.Not.Contain("\"fightUntil\""));

            var file = SaveSerializer.WriteText(st, ctx.Data);
            file = PersistenceFixture.Retarget(file, "state", state14);
            file = PersistenceFixture.Retarget(file, "version", "14");
            file = PersistenceFixture.Retarget(file, "hash", CanonicalJsonWriter.QuoteString(StateHash.Of(state14)));

            var old = SaveSerializer.ReadText(file, ctx.Data);
            Assert.That(old.Ok, Is.True, old.Reason);
            Assert.That(old.Header.Version, Is.EqualTo(14));
            Assert.That(old.Upgraded, Does.Contain("the warehouse doors still shut"));
            Assert.That(old.State.Encounters.Opened, Is.Empty);
            Assert.That(old.State.Encounters.FightUntil, Is.EqualTo(-1));
            Assert.That(Ground.Passable(ctx, old.State, 66, 61), Is.False);
        }
    }
}
