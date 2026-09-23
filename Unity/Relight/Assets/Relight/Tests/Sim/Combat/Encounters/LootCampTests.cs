using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using Relight.Sim.Tests.Persistence;

namespace Relight.Sim.Tests.Combat
{
    /// <summary>
    /// Batch 4, FRT-04 (REL-139): the small corridor camps. The accept line: a loot camp is claimed on its last
    /// kill; it refills 900 s after and not at 899; a player machine 19.5 tiles off stops the refill and one
    /// 20.5 off does not; a key camp stays cleared; the save round-trips the refill clock. §5.3's crate rule is here too: a
    /// refilled camp keeps the crate it had and puts nothing more in it.
    ///
    /// The map is <see cref="RaidFixture"/>'s open 160-tile square, with <c>freight:small:1</c> (six skitters
    /// round one group at the marker) standing at (120, 120). Only the encounter phase runs, so the garrison is
    /// removed by hand.
    /// </summary>
    public sealed class LootCampTests
    {
        const string Camp = "freight:small:1";
        const int Mx = 120, My = 120;
        const double Cx = Mx + .5, Cy = My + .5;
        const int PerSecond = 20;                        // RaidFixture.Dt is a twentieth of a second

        static SimContext Context(bool withKeyCamp = false)
        {
            var sites = new List<SiteRecord>
            {
                new SiteRecord("home", "Home Court", SiteKind.Core, RaidFixture.CoreX, RaidFixture.CoreY,
                    RaidFixture.CoreSize, RaidFixture.CoreSize),
                new SiteRecord(Camp, "Crossing camp", SiteKind.Camp, Mx, My, 1, 1),
            };
            if (withKeyCamp) sites.Add(new SiteRecord("freight:camp:1", "West passage camp", SiteKind.Camp, 30, 130, 1, 1));
            return new SimContext(ReferenceData.Create(), RaidFixture.Map(), null, null, new WorldSites(sites));
        }

        static List<ITickPhase> Phase() => new List<ITickPhase> { new EncounterPhase() };

        static void At(SimState st, double x, double y) => st.Engineer.Pos = new Vec2(x, y);

        static void Seconds(SimContext ctx, SimState st, double s) => RaidFixture.Run(ctx, st, (int)(s * PerSecond), Phase());

        static void Kill(SimState st, string id, int keep = 0)
        {
            var guards = st.Enemies.Actors.Where(e => e.Site == id).ToList();
            for (var i = keep; i < guards.Count; i++) st.Enemies.Actors.Remove(guards[i]);
        }

        /// <summary>The camp found from 12 tiles and cleared, the engineer sent far off, one tick run.</summary>
        static SimState Cleared(SimContext ctx)
        {
            var st = RaidFixture.State(ctx);
            At(st, Cx, Cy - 12);
            RaidFixture.Run(ctx, st, 1, Phase());
            Assert.That(st.Encounters.Find(Camp).Resolved, Is.True);
            Kill(st, Camp);
            At(st, 10, 150);
            RaidFixture.Run(ctx, st, 1, Phase());
            Assert.That(st.Encounters.Find(Camp).ClearedAt, Is.GreaterThanOrEqualTo(0));
            st.Events.Clear();
            return st;
        }

        [Test]
        public void ALootCampIsFoundUpCloseAndClaimedOnItsLastKill()
        {
            var ctx = Context();
            var st = RaidFixture.State(ctx);
            At(st, Cx, Cy - 12.5);
            RaidFixture.Run(ctx, st, 1, Phase());
            Assert.That(st.Encounters.Find(Camp).Resolved, Is.False, "a proximity camp is not found from 12.5 tiles");

            At(st, Cx, Cy - 12);
            RaidFixture.Run(ctx, st, 1, Phase());
            var rec = st.Encounters.Find(Camp);
            Assert.That(rec.Resolved, Is.True);
            var guards = st.Enemies.Actors.Where(e => e.Site == Camp).ToList();
            Assert.That(guards.Count, Is.EqualTo(6));
            Assert.That(guards.All(e => e.Kind == "skitter"), Is.True, "small camps are skitters only");
            Assert.That(st.MachineById(rec.Cache).Inv[ItemId.Magazine], Is.EqualTo(20));

            Kill(st, Camp, keep: 1);
            RaidFixture.Run(ctx, st, 1, Phase());
            Assert.That(rec.Claimed, Is.False, "one guard still lives");
            Kill(st, Camp);
            RaidFixture.Run(ctx, st, 1, Phase());
            Assert.That(rec.Claimed, Is.True, "claimed on the last kill, with no hold");
            Assert.That(rec.ClearedAt, Is.GreaterThanOrEqualTo(0));
            Assert.That(st.Encounters.Pouch, Is.Empty, "a loot camp has no key");
        }

        [Test]
        public void ItRefillsAtNineHundredSecondsAndNotAtEightNinetyNineAndKeepsItsCrate()
        {
            var ctx = Context();
            var st = Cleared(ctx);
            var rec = st.Encounters.Find(Camp);
            var crate = st.MachineById(rec.Cache);
            crate.Inv.Add(ItemId.Magazine, -20);     // the player emptied it (only the crate is looked at here)
            var found = st.Stats.Found[ItemId.Magazine];

            Seconds(ctx, st, 899);
            Assert.That(rec.Resolved, Is.True, "899 s is not yet 900");
            Assert.That(RaidFixture.Count<EncounterRefilledEvent>(st), Is.EqualTo(0));
            Seconds(ctx, st, 1.1);
            Assert.That(RaidFixture.Count<EncounterRefilledEvent>(st), Is.EqualTo(1));
            Assert.That((rec.Resolved, rec.Claimed, rec.ClearedAt), Is.EqualTo((false, false, -1.0)));

            // It is found again like the first time, and the same crate stands there, still empty.
            At(st, Cx, Cy - 12);
            RaidFixture.Run(ctx, st, 1, Phase());
            Assert.That(rec.Resolved, Is.True);
            Assert.That(EncounterPhase.Living(st, Camp), Is.EqualTo(6));
            Assert.That(st.Machines.Count(m => m.Site == Camp), Is.EqualTo(1), "one crate, not a second");
            Assert.That(rec.Cache, Is.EqualTo(crate.Id));
            Assert.That(crate.Inv[ItemId.Magazine], Is.EqualTo(0), "§5.3: a refilled camp does not refill the crate");
            Assert.That(st.Stats.Found[ItemId.Magazine], Is.EqualTo(found));
            foreach (var g in st.Enemies.Actors)
                Assert.That(crate.Rect.Contains((int)g.Pos.X, (int)g.Pos.Y), Is.False, "nobody is born inside the crate");
        }

        [Test]
        public void AMachineInsideTwentyTilesStopsTheRefillAndOneJustOutsideDoesNot()
        {
            var ctx = Context();
            var st = Cleared(ctx);
            var rec = st.Encounters.Find(Camp);
            // A chest is 2×2, so one set down at (Mx + 19, My) has its centre (140, 121): 19.5 tiles off.
            var near = RaidFixture.Add(ctx, st, "chest", Mx + 19, My);
            Assert.That(DirectorRules.Distance(near.X + 1, near.Y + 1, Cx, Cy), Is.EqualTo(19.5).Within(.01));
            Seconds(ctx, st, 950);
            Assert.That(rec.Resolved, Is.True, "a player's machine within 20 tiles keeps the camp cleared");
            Assert.That(RaidFixture.Count<EncounterRefilledEvent>(st), Is.EqualTo(0));

            // Taken up and put down a tile further, 20.5 tiles off, the camp is no longer anyone's outpost.
            st.Machines.Remove(near);
            RaidFixture.Add(ctx, st, "chest", Mx + 20, My);
            RaidFixture.Run(ctx, st, 1, Phase());
            Assert.That(RaidFixture.Count<EncounterRefilledEvent>(st), Is.EqualTo(1));
            Assert.That(rec.Resolved, Is.False);

            // The camp's own crate never counts as building near it.
            Assert.That(EncounterPhase.BuiltNear(st, new Vec2(Cx, Cy), 20), Is.False);
        }

        [Test]
        public void AKeyCampStaysCleared()
        {
            var ctx = Context(withKeyCamp: true);
            var st = RaidFixture.State(ctx);
            At(st, 30.5, 130.5 - 30);
            RaidFixture.Run(ctx, st, 1, Phase());
            var rec = st.Encounters.Find("freight:camp:1");
            Assert.That(rec.Resolved, Is.True);
            Kill(st, "freight:camp:1");
            At(st, 150, 10);
            Seconds(ctx, st, 1000);
            Assert.That(rec.ClearedAt, Is.GreaterThanOrEqualTo(0));
            Assert.That(rec.Resolved, Is.True, "a key camp never comes back");
            Assert.That(rec.Claimed, Is.False, "and it is claimed by holding it, not by its last kill");
            Assert.That(RaidFixture.Count<EncounterRefilledEvent>(st), Is.EqualTo(0));
            Assert.That(EncounterCatalogue.Find("plant:riverside:squat").RepeatSeconds, Is.EqualTo(0), "nor does the squat");
        }

        [Test]
        public void TheSaveRoundTripsTheRefillClock()
        {
            var ctx = Context();
            var st = Cleared(ctx);
            Seconds(ctx, st, 400);

            var text = SaveSerializer.WriteText(st, ctx.Data);
            var load = SaveSerializer.ReadText(text, ctx.Data);
            Assert.That(load.Ok, Is.True, load.Reason);
            var b = load.State;
            Assert.That(b.Encounters.Find(Camp).ClearedAt, Is.EqualTo(st.Encounters.Find(Camp).ClearedAt));
            Assert.That(b.Encounters.Find(Camp).Claimed, Is.True);
            Assert.That(StateHash.Compute(b), Is.EqualTo(StateHash.Compute(st)));

            Seconds(ctx, b, 499);
            Assert.That(b.Encounters.Find(Camp).Resolved, Is.True, "400 + 499 is not 900");
            Seconds(ctx, b, 1.1);
            Assert.That(b.Encounters.Find(Camp).Resolved, Is.False, "the loaded clock ran on from where it was");
        }

        [Test]
        public void TheFourSmallCampsAreSkittersOnlyLootOnlyAndComeBack()
        {
            var small = EncounterCatalogue.All.Where(d => d.Kind == EncounterKind.LootCamp).ToList();
            Assert.That(small.Select(d => d.Id), Is.EqualTo(new[] { "freight:small:1", "freight:small:2", "freight:small:3", "freight:small:4" }));
            foreach (var d in small)
            {
                Assert.That(d.Discovery, Is.EqualTo(EncounterDiscovery.Proximity), d.Id);
                Assert.That(d.Bodies, Is.InRange(5, 8), d.Id);
                Assert.That(Enumerable.Range(0, d.Bodies).All(i => EncounterCatalogue.KindOf(d, i) == "skitter"), Is.True, d.Id);
                Assert.That(d.Key, Is.Null, d.Id);
                Assert.That(EncounterPhase.Occupies(d), Is.False, d.Id);
                Assert.That(d.RepeatSeconds, Is.EqualTo(900), d.Id);
                Assert.That(d.Cache.All(c => c.Count >= 10 && c.Count <= 30), Is.True, d.Id + ": §4.2's 10–30");
            }
            Assert.That(EncounterPhase.KeyCount(), Is.EqualTo(3), "the small camps add no keys");
        }
    }
}
