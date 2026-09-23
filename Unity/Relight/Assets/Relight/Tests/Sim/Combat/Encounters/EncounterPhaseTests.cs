using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using NUnit.Framework;
using Relight.Sim.Tests.Persistence;

namespace Relight.Sim.Tests.Combat
{
    /// <summary>
    /// Batch 4, FRT-02 (REL-137): camps come alive. The accept line: a key camp is found by its search area and a
    /// small place by proximity; a garrison is born whole or not at all, and never beside the engineer; the save
    /// round-trips at v13 and a v12 save upgrades with no camp found.
    ///
    /// The map is <see cref="RaidFixture"/>'s 160×160 of open ground, with the real catalogue rows standing on
    /// hand-placed sites: the West passage marker at (130, 130) with four groups round it, and the Riverside plant
    /// at (40, 130), which has no groups and so spawns its squat at the row's two offsets.
    /// </summary>
    public sealed class EncounterPhaseTests
    {
        const string Camp = "freight:camp:1";
        const string Squat = "plant:riverside:squat";
        const int Mx = 130, My = 130;           // camp marker; its centre is (130.5, 130.5)
        const int Px = 40, Py = 130;            // plant; centre (40.5, 130.5)

        static readonly (int X, int Y)[] Groups = { (118, 126), (132, 124), (118, 138), (138, 136) };

        static SimContext Context(ArrayGeometry map = null)
        {
            var sites = new List<SiteRecord>
            {
                new SiteRecord("home", "Home Court", SiteKind.Core, RaidFixture.CoreX, RaidFixture.CoreY,
                    RaidFixture.CoreSize, RaidFixture.CoreSize),
                new SiteRecord(Camp, "West passage camp", SiteKind.Camp, Mx, My, 1, 1, "", 20),
                new SiteRecord("plant:riverside", "Riverside Works", SiteKind.Plant, Px, Py, 1, 1),
            };
            for (var i = 0; i < Groups.Length; i++)
                sites.Add(new SiteRecord(Camp + ":group:" + i, "group", SiteKind.Camp, Groups[i].X - 4, Groups[i].Y - 4, 9, 9));
            return new SimContext(ReferenceData.Create(), map ?? RaidFixture.Map(), null, null, new WorldSites(sites));
        }

        static List<ITickPhase> Phase() => new List<ITickPhase> { new EncounterPhase() };

        static void At(SimState st, double x, double y) => st.Engineer.Pos = new Vec2(x, y);

        static List<Enemy> Guards(SimState st, string id) =>
            st.Enemies.Actors.Where(e => e.Site == id).ToList();

        [Test]
        public void AKeyCampIsFoundAtThirtyTwoTilesAndNotBefore()
        {
            var ctx = Context();
            var st = RaidFixture.State(ctx);
            At(st, Mx + .5, My + .5 + 32.5);
            RaidFixture.Run(ctx, st, 5, Phase());
            Assert.That(st.Encounters.Find(Camp).Resolved, Is.False, "32.5 tiles out, the camp is still unfound");
            Assert.That(Guards(st, Camp), Is.Empty);

            At(st, Mx + .5, My + .5 + 31.5);
            RaidFixture.Run(ctx, st, 1, Phase());
            var rec = st.Encounters.Find(Camp);
            Assert.That(rec.Resolved, Is.True);
            Assert.That(rec.ResolvedAt, Is.GreaterThan(0));
            var guards = Guards(st, Camp);
            Assert.That(guards.Count, Is.EqualTo(12), "U-D-64 (b): the West passage holds 12, not the reference's 20");
            Assert.That(guards.All(g => g.Layer == EnemyLayer.Site), Is.True, "camp residents, never raiders");
            Assert.That(guards.Count(g => g.Kind == "spitter"), Is.EqualTo(2), "every fifth body: bodies 5 and 10");
            Assert.That(guards.Select(g => g.Group).Distinct().Count(), Is.EqualTo(4), "one squad per group");
            Assert.That(guards.All(g => g.Group >= EncounterCatalogue.GroupBase), Is.True, "no squad shares a raid's id");
            Assert.That(st.Events.OfType<EncounterResolvedEvent>().Single().Bodies, Is.EqualTo(12));
            foreach (var g in guards)
            {
                var near = Groups.Min(c => DirectorRules.Distance(g.Pos.X, g.Pos.Y, c.X + .5, c.Y + .5));
                Assert.That(near, Is.LessThanOrEqualTo(EncounterCatalogue.GroupSpreadTiles + .8), "each body stands with its group");
            }

            // Found once per life: walking away and back births nobody new.
            At(st, 20, 20);
            RaidFixture.Run(ctx, st, 2, Phase());
            At(st, Mx + .5, My + 20);
            RaidFixture.Run(ctx, st, 2, Phase());
            Assert.That(Guards(st, Camp).Count, Is.EqualTo(12));
        }

        [Test]
        public void ASquatIsFoundOnlyWithinTwelveTiles()
        {
            var ctx = Context();
            var st = RaidFixture.State(ctx);
            At(st, Px + .5, Py + .5 + 12.5);
            RaidFixture.Run(ctx, st, 5, Phase());
            Assert.That(st.Encounters.Find(Squat).Resolved, Is.False, "proximity: nothing at 12.5 tiles");

            At(st, Px + .5, Py + .5 + 11.5);
            RaidFixture.Run(ctx, st, 1, Phase());
            var squat = Guards(st, Squat);
            Assert.That(squat.Count, Is.EqualTo(8));
            Assert.That(squat.All(g => g.Kind == "skitter"), Is.True, "a squat is skitters only");
            Assert.That(squat.Select(g => g.Group).Distinct().Count(), Is.EqualTo(2), "the row's two offsets are two squads");
        }

        [Test]
        public void NoBodyIsBornWithinTwelveTilesOfTheEngineerAndTheGarrisonWaitsWhole()
        {
            var ctx = Context();
            var st = RaidFixture.State(ctx);
            // Standing on the plant: the squat's whole spawn room is within 12 tiles, so nothing is born — not
            // even the bodies that would have fitted on the far side.
            At(st, Px + .5, Py + .5);
            RaidFixture.Run(ctx, st, 20, Phase());
            Assert.That(Guards(st, Squat), Is.Empty, "the squat waits rather than appearing at the engineer's elbow");
            Assert.That(st.Encounters.Find(Squat).Resolved, Is.False, "and it is still unfound, so it will try again");

            At(st, Px + .5, Py + .5 + 11.5);
            RaidFixture.Run(ctx, st, 1, Phase());
            var squat = Guards(st, Squat);
            Assert.That(squat.Count, Is.EqualTo(8), "stepping back lets the whole squat be born at once");
            foreach (var g in squat)
                Assert.That(DirectorRules.Distance(g.Pos.X, g.Pos.Y, st.Engineer.Pos.X, st.Engineer.Pos.Y),
                    Is.GreaterThanOrEqualTo(EncounterCatalogue.NoSurpriseBirthTiles));
        }

        [Test]
        public void AGroupWithNoRoomHoldsBackTheWholeGarrison()
        {
            // Group 3's whole spread is solid, so the other three groups — which have room — must not be born alone.
            var blocked = new[] { new TileRect(Groups[3].X - 7, Groups[3].Y - 7, 15, 15) };
            var ctx = Context(RaidFixture.Map(blocked));
            var st = RaidFixture.State(ctx);
            At(st, Mx + .5, My + .5 - 30);
            RaidFixture.Run(ctx, st, 10, Phase());
            Assert.That(Guards(st, Camp), Is.Empty, "three quarters of a camp is never born");
            Assert.That(st.Encounters.Find(Camp).Resolved, Is.False);
            Assert.That(EncounterPhase.TryPlace(ctx, st, EncounterCatalogue.Find(Camp), ctx.Sites.Find(Camp), out var none),
                Is.False);
            Assert.That(none, Is.Empty, "a refused placement hands back nothing half-chosen");
        }

        [Test]
        public void AGarrisonThatWouldBreakTheWorldCapWaitsWhole()
        {
            var ctx = Context();
            var st = RaidFixture.State(ctx);
            var cap = ctx.Data.Raids.LivingBudget;
            for (var i = 0; i < cap - 11; i++) RaidFixture.Body(st, "skitter", 2 + i % 50, 2 + i / 50, EnemyLayer.Site, 99);
            At(st, Mx + .5, My + .5 - 30);
            RaidFixture.Run(ctx, st, 3, Phase());
            Assert.That(Guards(st, Camp), Is.Empty, $"12 more would make {cap + 1}, over the {cap} cap");

            st.Enemies.Actors.RemoveAt(0);
            RaidFixture.Run(ctx, st, 1, Phase());
            Assert.That(Guards(st, Camp).Count, Is.EqualTo(12), "with room for all twelve, all twelve come");
            Assert.That(st.Enemies.Actors.Count, Is.EqualTo(cap));
        }

        [Test]
        public void TheLastDeathIsNotedOnceAndTheCampIsNotRefilled()
        {
            var ctx = Context();
            var st = RaidFixture.State(ctx);
            At(st, Mx + .5, My + .5 - 30);
            RaidFixture.Run(ctx, st, 1, Phase());
            Assert.That(Guards(st, Camp).Count, Is.EqualTo(12));
            st.Enemies.Actors.RemoveAll(e => e.Site == Camp);
            st.Events.Clear();
            RaidFixture.Run(ctx, st, 3, Phase());
            var rec = st.Encounters.Find(Camp);
            Assert.That(rec.ClearedAt, Is.GreaterThan(0));
            Assert.That(st.Events.OfType<EncounterClearedEvent>().Count(), Is.EqualTo(1));
            Assert.That(Guards(st, Camp), Is.Empty, "a key camp never refills (RepeatSeconds 0)");
        }

        [Test]
        public void AMapWithoutTheSitesHasNoEncounters()
        {
            var ctx = RaidFixture.Context();
            var st = RaidFixture.State(ctx);
            RaidFixture.Run(ctx, st, 5, Phase());
            Assert.That(st.Encounters.Records, Is.Empty);
            Assert.That(st.Enemies.Actors, Is.Empty);
        }

        [Test]
        public void TheCatalogueDealsSpittersAndTheBreakerAsWritten()
        {
            var c1 = EncounterCatalogue.Find("freight:camp:1");
            var c2 = EncounterCatalogue.Find("freight:camp:2");
            var c3 = EncounterCatalogue.Find("freight:camp:3");
            Assert.That((c1.Bodies, c2.Bodies, c3.Bodies), Is.EqualTo((12, 22, 20)), "U-D-64 (b) and the reference");
            string[] Roster(EncounterDef d) => Enumerable.Range(0, d.Bodies).Select(i => EncounterCatalogue.KindOf(d, i)).ToArray();
            Assert.That(Roster(c2).Count(k => k == "spitter"), Is.EqualTo(7), "every third of 22");
            var r3 = Roster(c3);
            Assert.That(r3.Last(), Is.EqualTo("breaker"), "the Breaker takes the last place");
            Assert.That(r3.Count(k => k == "breaker"), Is.EqualTo(1));
            Assert.That(r3.Count(k => k == "spitter"), Is.EqualTo(3), "bodies 5, 10 and 15; 20's place is the Breaker's");
            Assert.That(EncounterCatalogue.All.Select(d => d.Serial).Distinct().Count(), Is.EqualTo(EncounterCatalogue.All.Count));
            Assert.That(EncounterCatalogue.All.Select(d => d.Id).Distinct().Count(), Is.EqualTo(EncounterCatalogue.All.Count));
        }

        [Test]
        public void AFoundCampRoundTripsAtTheCurrentVersion()
        {
            var ctx = Context();
            var st = RaidFixture.State(ctx);
            At(st, Mx + .5, My + .5 - 30);
            RaidFixture.Run(ctx, st, 2, Phase());
            st.Encounters.Find(Camp).Discovered = true;

            var text = SaveSerializer.WriteText(st, ctx.Data);
            var load = SaveSerializer.ReadText(text, ctx.Data);
            Assert.That(load.Ok, Is.True, load.Reason);
            Assert.That(load.Header.Version, Is.EqualTo(SaveSchema.Version), "13 when this was written; 14 since REL-138");
            var a = st.Encounters.Find(Camp);
            var b = load.State.Encounters.Find(Camp);
            Assert.That(b, Is.Not.Null);
            Assert.That((b.Discovered, b.Resolved, b.ResolvedAt, b.ClearedAt, b.Claimed, b.OccupiedSince, b.Cache),
                Is.EqualTo((a.Discovered, a.Resolved, a.ResolvedAt, a.ClearedAt, a.Claimed, a.OccupiedSince, a.Cache)));
            Assert.That(Guards(load.State, Camp).Count, Is.EqualTo(12), "every guard still knows its camp");
            Assert.That(StateHash.Compute(load.State), Is.EqualTo(StateHash.Compute(st)));

            RaidFixture.Run(ctx, load.State, 5, Phase());
            Assert.That(Guards(load.State, Camp).Count, Is.EqualTo(12), "a loaded, found camp is not found again");
        }

        [Test]
        public void AVersionTwelveSaveUpgradesWithNoCampFound()
        {
            var ctx = Context();
            var st = RaidFixture.State(ctx);
            RaidFixture.Body(st, "skitter", 20, 20, EnemyLayer.Site, 0);      // a v12 Admin spawn: camp layer, no camp
            RaidFixture.Body(st, "skitter", 24, 20, EnemyLayer.Site, 0);

            // Forge the v12 shape: no `encounters`, and no `site` on any body.
            var state = PersistenceFixture.Canonical(st);
            var doc = JsonValue.Parse(state, out var error);
            Assert.That(error, Is.Null);
            var parts = new List<string>();
            foreach (var key in doc.Keys)
                if (key != "encounters")
                    parts.Add(CanonicalJsonWriter.QuoteString(key) + ":" + doc.Member(key).ToCanonicalJson());
            var state12 = Regex.Replace("{" + string.Join(",", parts) + "}", "\"site\":\"[^\"]*\",?", "");
            Assert.That(state12, Does.Not.Contain("\"site\""));
            Assert.That(state12, Does.Not.Contain("\"encounters\""));

            var file = SaveSerializer.WriteText(st, ctx.Data);
            file = PersistenceFixture.Retarget(file, "state", state12);
            file = PersistenceFixture.Retarget(file, "version", "12");
            file = PersistenceFixture.Retarget(file, "hash", CanonicalJsonWriter.QuoteString(StateHash.Of(state12)));

            var old = SaveSerializer.ReadText(file, ctx.Data);
            Assert.That(old.Ok, Is.True, old.Reason);
            Assert.That(old.Header.Version, Is.EqualTo(12));
            Assert.That(old.State.Encounters.Records, Is.Empty, "no camp found");
            Assert.That(old.State.Enemies.Actors.Count, Is.EqualTo(2));
            Assert.That(old.State.Enemies.Actors.All(e => e.Site == ""), Is.True, "and no body guards one");
        }
    }
}
