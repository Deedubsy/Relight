using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using NUnit.Framework;
using Relight.Sim.Tests.Persistence;

namespace Relight.Sim.Tests.Combat
{
    /// <summary>
    /// Batch 4, FRT-03 (REL-138): hold a camp, get a key. The accept line: 30 s inside the radius claims the key;
    /// stepping out resets the clock; a guard at 19 tiles blocks the claim and one at 21 does not; the crate
    /// refuses pick-up; the save round-trips the pouch and the clock.
    ///
    /// The map and sites are <see cref="EncounterPhaseTests"/>': the West passage marker at (130, 130) and its four
    /// groups. The garrison is found from 30 tiles out and then removed by hand, since only the encounter phase
    /// runs here and nothing would kill it.
    /// </summary>
    public sealed class KeyCampTests
    {
        const string Camp = "freight:camp:1";
        const int Mx = 130, My = 130;
        const double Cx = Mx + .5, Cy = My + .5;
        const int PerSecond = 20;                        // RaidFixture.Dt is a twentieth of a second

        static readonly (int X, int Y)[] Groups = { (118, 126), (132, 124), (118, 138), (138, 136) };

        static SimContext Context()
        {
            var sites = new List<SiteRecord>
            {
                new SiteRecord("home", "Home Court", SiteKind.Core, RaidFixture.CoreX, RaidFixture.CoreY,
                    RaidFixture.CoreSize, RaidFixture.CoreSize),
                new SiteRecord(Camp, "West passage camp", SiteKind.Camp, Mx, My, 1, 1, "", 20),
            };
            for (var i = 0; i < Groups.Length; i++)
                sites.Add(new SiteRecord(Camp + ":group:" + i, "group", SiteKind.Camp, Groups[i].X - 4, Groups[i].Y - 4, 9, 9));
            return new SimContext(ReferenceData.Create(), RaidFixture.Map(), null, null, new WorldSites(sites));
        }

        static List<ITickPhase> Phase() => new List<ITickPhase> { new EncounterPhase() };

        static void At(SimState st, double x, double y) => st.Engineer.Pos = new Vec2(x, y);

        static void Seconds(SimContext ctx, SimState st, double s) => RaidFixture.Run(ctx, st, (int)(s * PerSecond), Phase());

        /// <summary>The camp found and its garrison gone: all that is left is to hold it.</summary>
        static SimState Cleared(SimContext ctx, int keep = 0)
        {
            var st = RaidFixture.State(ctx);
            At(st, Cx, Cy - 30);
            RaidFixture.Run(ctx, st, 1, Phase());
            Assert.That(st.Encounters.Find(Camp).Resolved, Is.True);
            var guards = st.Enemies.Actors.Where(e => e.Site == Camp).ToList();
            for (var i = keep; i < guards.Count; i++) st.Enemies.Actors.Remove(guards[i]);
            st.Events.Clear();
            return st;
        }

        [Test]
        public void ThirtySecondsOnTheCampClaimsTheKey()
        {
            var ctx = Context();
            var st = Cleared(ctx);
            At(st, Cx + 2, Cy);
            Seconds(ctx, st, 29.5);
            var rec = st.Encounters.Find(Camp);
            Assert.That(rec.Claimed, Is.False, "29.5 s is not yet 30");
            Assert.That(rec.OccupiedSince, Is.GreaterThanOrEqualTo(0), "the clock is running");
            Assert.That(st.Encounters.Pouch, Is.Empty);

            Seconds(ctx, st, 1);
            Assert.That(rec.Claimed, Is.True);
            Assert.That(rec.OccupiedSince, Is.EqualTo(-1), "a claimed camp has no clock");
            Assert.That(st.Encounters.Pouch, Is.EqualTo(new[] { "freight:camp:1" }));
            var e = st.Events.OfType<EncounterClaimedEvent>().Single();
            Assert.That(e.Text, Is.EqualTo("West passage secured — key 1 of 3"));
            Assert.That((e.Held, e.Of), Is.EqualTo((1, 3)));

            Seconds(ctx, st, 40);
            Assert.That(st.Encounters.Pouch.Count, Is.EqualTo(1), "one key per camp, however long it is held");
            Assert.That(st.Events.OfType<EncounterClaimedEvent>().Count(), Is.EqualTo(1));
        }

        [Test]
        public void SteppingOutStartsTheClockAgain()
        {
            var ctx = Context();
            var st = Cleared(ctx);
            var rec = st.Encounters.Find(Camp);
            At(st, Cx, Cy);
            Seconds(ctx, st, 20);
            At(st, Cx + 3.5, Cy);                           // half a tile past the 3-tile radius
            RaidFixture.Run(ctx, st, 1, Phase());
            Assert.That(rec.OccupiedSince, Is.EqualTo(-1), "one step out and the hold is lost");

            At(st, Cx, Cy);
            Seconds(ctx, st, 20);
            Assert.That(rec.Claimed, Is.False, "20 s + 20 s is not 30 s held");
            Seconds(ctx, st, 10.5);
            Assert.That(rec.Claimed, Is.True);

            // Going down is stepping out.
            var ctx2 = Context();
            var st2 = Cleared(ctx2);
            At(st2, Cx, Cy);
            Seconds(ctx2, st2, 20);
            st2.Engineer.Down = st2.T + 10;
            RaidFixture.Run(ctx2, st2, 1, Phase());
            Assert.That(st2.Encounters.Find(Camp).OccupiedSince, Is.EqualTo(-1), "a downed engineer holds nothing");
        }

        [Test]
        public void AGuardAtNineteenTilesBlocksTheClaimAndOneAtTwentyOneDoesNot()
        {
            var ctx = Context();
            var st = Cleared(ctx, keep: 1);
            var guard = st.Enemies.Actors.Single(e => e.Site == Camp);
            var rec = st.Encounters.Find(Camp);
            At(st, Cx, Cy);

            guard.Pos = new Vec2(Cx + 19, Cy);
            Seconds(ctx, st, 35);
            Assert.That(rec.Claimed, Is.False, "a living guard 19 tiles off still holds the camp");
            Assert.That(rec.OccupiedSince, Is.EqualTo(-1));

            guard.Pos = new Vec2(Cx + 21, Cy);
            Seconds(ctx, st, 30.5);
            Assert.That(rec.Claimed, Is.True, "one 21 tiles off has left it");

            // A raider or another camp's body does not count: only this camp's guards hold it.
            var ctx2 = Context();
            var st2 = Cleared(ctx2);
            RaidFixture.Body(st2, "skitter", Cx + 5, Cy, EnemyLayer.Site, 99);
            At(st2, Cx, Cy);
            Seconds(ctx2, st2, 30.5);
            Assert.That(st2.Encounters.Find(Camp).Claimed, Is.True);
        }

        [Test]
        public void TheCacheCrateHoldsTheRowsContentsAndCannotBePickedUp()
        {
            var ctx = Context();
            var st = Cleared(ctx);
            var rec = st.Encounters.Find(Camp);
            Assert.That(rec.Cache, Is.GreaterThan(0), "the crate is set down when the camp is found");
            var crate = st.MachineById(rec.Cache);
            Assert.That(crate, Is.Not.Null);
            Assert.That(crate.Kind, Is.EqualTo("chest"));
            Assert.That(crate.Site, Is.EqualTo(Camp));
            Assert.That(crate.Rect.Contains(Mx, My), Is.False, "the marker tile stays clear to stand on");
            Assert.That(DirectorRules.Distance(crate.X + 1, crate.Y + 1, Cx, Cy), Is.LessThan(4));
            Assert.That((crate.Inv[ItemId.Magazine], crate.Inv[ItemId.Steel]), Is.EqualTo((40.0, 10.0)), "§4.2");
            Assert.That((st.Stats.Found[ItemId.Magazine], st.Stats.Found[ItemId.Steel]), Is.EqualTo((40.0, 10.0)),
                "what the crate held entered the game, so the ledger counts it as found");

            At(st, crate.X + 1, crate.Y + 3);
            var (ok, reason) = Placement.CanPickUp(ctx, st, crate);
            Assert.That(ok, Is.False, "the crate stays with the camp");
            Assert.That(reason, Does.Contain("crate stays"));
            var (removed, _) = Placement.Remove(ctx, st, crate.Id);
            Assert.That(removed, Is.False);
            Assert.That(st.MachineById(rec.Cache), Is.Not.Null);
            Assert.That(OpeningRules.HomeStock(st, ItemId.Magazine), Is.EqualTo(0), "a camp crate is not Home stock");

            // A garrison never stands inside its crate.
            foreach (var g in st.Enemies.Actors)
                Assert.That(crate.Rect.Contains((int)g.Pos.X, (int)g.Pos.Y), Is.False);
        }

        [Test]
        public void TheSaveRoundTripsThePouchTheClockAndTheCrate()
        {
            var ctx = Context();
            var st = Cleared(ctx);
            At(st, Cx, Cy);
            Seconds(ctx, st, 12);
            st.Encounters.Pouch.Add("freight:camp:9");   // a key from elsewhere, to show order is kept
            var since = st.Encounters.Find(Camp).OccupiedSince;

            var text = SaveSerializer.WriteText(st, ctx.Data);
            var load = SaveSerializer.ReadText(text, ctx.Data);
            Assert.That(load.Ok, Is.True, load.Reason);
            Assert.That(load.Header.Version, Is.EqualTo(SaveSchema.Version));
            var b = load.State;
            Assert.That(b.Encounters.Pouch, Is.EqualTo(new[] { "freight:camp:9" }));
            Assert.That(b.Encounters.Find(Camp).OccupiedSince, Is.EqualTo(since));
            var crate = b.MachineById(b.Encounters.Find(Camp).Cache);
            Assert.That(crate.Site, Is.EqualTo(Camp));
            Assert.That(b.Stats.Found[ItemId.Magazine], Is.EqualTo(40));
            Assert.That(StateHash.Compute(b), Is.EqualTo(StateHash.Compute(st)));

            // The loaded clock keeps counting from where it was: 18.5 more seconds finishes the 30.
            Seconds(ctx, b, 18.5);
            Assert.That(b.Encounters.Find(Camp).Claimed, Is.True);
            Assert.That(b.Encounters.Pouch, Is.EqualTo(new[] { "freight:camp:9", "freight:camp:1" }));
        }

        [Test]
        public void AVersionThirteenSaveUpgradesWithAnEmptyPouchAndThePlayersOwnMachines()
        {
            var ctx = Context();
            var st = RaidFixture.State(ctx);
            RaidFixture.Add(ctx, st, "chest", 20, 20).Inv.Add(ItemId.Steel, 5);

            // Forge the v13 shape: no pouch, no found, no site on any machine (there are no bodies to confuse).
            var state = PersistenceFixture.Canonical(st);
            var state13 = Regex.Replace(state, "\"pouch\":\\[[^\\]]*\\],?", "");
            state13 = Regex.Replace(state13, "\"found\":\\{\"n\":\\[[^\\]]*\\]\\},?", "");
            state13 = Regex.Replace(state13, "\"site\":\"[^\"]*\",?", "");
            state13 = state13.Replace(",}", "}");
            Assert.That(state13, Does.Not.Contain("\"pouch\""));
            Assert.That(state13, Does.Not.Contain("\"found\""));
            Assert.That(state13, Does.Not.Contain("\"site\""));

            var file = SaveSerializer.WriteText(st, ctx.Data);
            file = PersistenceFixture.Retarget(file, "state", state13);
            file = PersistenceFixture.Retarget(file, "version", "13");
            file = PersistenceFixture.Retarget(file, "hash", CanonicalJsonWriter.QuoteString(StateHash.Of(state13)));

            var old = SaveSerializer.ReadText(file, ctx.Data);
            Assert.That(old.Ok, Is.True, old.Reason);
            Assert.That(old.Header.Version, Is.EqualTo(13));
            Assert.That(old.State.Encounters.Pouch, Is.Empty, "no key held");
            Assert.That(old.State.Machines.Single().Site, Is.EqualTo(""), "the chest is the player's");
            Assert.That(old.State.Machines.Single().Inv[ItemId.Steel], Is.EqualTo(5));
            Assert.That(old.State.Stats.Found[ItemId.Steel], Is.EqualTo(0));
        }
    }
}
