using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using Relight.Sim.Tests.Persistence;

namespace Relight.Sim.Tests.Combat
{
    /// <summary>
    /// Batch 4, FRT-09 (REL-144): the plant raid. The accept line: before commissioning the raid targets Home;
    /// after it, the next major raid targets the plant; the raid size is unchanged.
    ///
    /// The map is <see cref="RaidFixture"/>'s open square with the Home core at (76, 76) and Riverside Works on a
    /// hand-placed 1×1 site at (40, 130). The plant is lit by hand here; <see cref="PlantTests"/> covers the command.
    /// </summary>
    public sealed class PlantRaidTests
    {
        const string Id = "plant:riverside";
        const int Px = 40, Py = 130;

        static SimContext Context() => new SimContext(ReferenceData.Create(), RaidFixture.Map(), null, null,
            new WorldSites(new List<SiteRecord>
            {
                new SiteRecord("home", "Home Court", SiteKind.Core, RaidFixture.CoreX, RaidFixture.CoreY,
                    RaidFixture.CoreSize, RaidFixture.CoreSize),
                new SiteRecord(Id, "Riverside Works", SiteKind.Plant, Px, Py, 1, 1),
            }));

        static List<ITickPhase> Clock() => new List<ITickPhase> { new DirectorPhase() };

        /// <summary>The plant commissioned as <see cref="Plants.Commission"/> leaves it: lit, and owed a raid.</summary>
        static PlantRecord Light(SimContext ctx, SimState st)
        {
            var rec = new PlantRecord { Id = Id, Prepared = true, CommissionedAt = st.T, Hp = Plants.MaxHp(ctx.Data) };
            st.Encounters.Plants.Add(rec);
            st.Encounters.NewestPlant = Id;
            st.Director.PlantRaid = Id;
            st.Rev++;
            return rec;
        }

        /// <summary>Tick the director to the next assault's booking and return it.</summary>
        static MajorRaid Book(SimContext ctx, SimState st)
        {
            st.T = st.Director.NextStart - ctx.Data.Raids.WarningS;
            RaidFixture.Run(ctx, st, 1, Clock());
            Assert.That(st.Director.Major, Is.Not.Null, st.Director.Notice);
            return st.Director.Major;
        }

        [Test]
        public void BeforeCommissioningTheAssaultIsForHome()
        {
            var ctx = Context();
            var st = RaidFixture.State(ctx, 3);
            Assert.That(Plants.RaidAim(st), Is.Empty);
            var a = Book(ctx, st);
            Assert.That(a.Plant, Is.Empty, "no plant is lit, so the Home core is the aim");
        }

        [Test]
        public void AfterCommissioningTheNextAssaultIsForThePlantAtTheSameSize()
        {
            var ctx = Context();
            var home = RaidFixture.State(ctx, 3);
            var h = Book(ctx, home);

            var st = RaidFixture.State(ctx, 3);
            Light(ctx, st);
            var a = Book(ctx, st);
            Assert.That(a.Plant, Is.EqualTo(Id), "the first assault after commissioning goes for the plant");
            Assert.That(a.Total, Is.EqualTo(h.Total), "the raid size is unchanged");
            Assert.That(a.WaveCount.Sum(), Is.EqualTo(h.WaveCount.Sum()));

            Assert.That(DirectorRules.Target(ctx, st, a.Plant, out var x, out var y, out var size), Is.True);
            Assert.That((x, y, size), Is.EqualTo((Px, Py, 1)), "its destination is the plant's footprint");
            Assert.That(st.Director.PlantRaid, Is.EqualTo(Id), "still owed until the assault commits");

            st.T = a.StartsAt;
            RaidFixture.Run(ctx, st, 1, Clock());
            Assert.That(a.Committed, Is.True, st.Director.Notice);
            Assert.That(st.Director.PlantRaid, Is.Empty, "the plant has had its raid");
            Assert.That(Plants.RaidAim(st), Is.Empty, "so the next booking is the Home core again");
        }

        [Test]
        public void AFallenPlantIsNotBookedAgainst()
        {
            var ctx = Context();
            var st = RaidFixture.State(ctx, 3);
            Light(ctx, st).Hp = 0;
            Assert.That(Plants.RaidAim(st), Is.Empty);
            Assert.That(Book(ctx, st).Plant, Is.Empty, "a plant already at 0 is not an assault's aim");
        }

        [Test]
        public void ThePlantAssaultHitsThePlantAndNotHome()
        {
            var ctx = Context();
            var st = RaidFixture.State(ctx);
            var rec = Light(ctx, st);
            var body = RaidFixture.Body(st, "skitter", Px + 1.5, Py + .5, EnemyLayer.Major, 5);
            st.Director.Major.Plant = Id;
            Assert.That(DirectorRules.AimOf(st, body), Is.EqualTo(Id));
            var homeHp = st.Home.Hp;

            RaidFixture.Run(ctx, st, 600, RaidFixture.Phases());
            Assert.That(rec.Hp, Is.LessThan(Plants.MaxHp(ctx.Data)), "the plant took the hits");
            Assert.That(st.Home.Hp, Is.EqualTo(homeHp), "and the Home core took none");
        }

        [Test]
        public void APlantAtZeroFallsAndStopsSupplying()
        {
            var ctx = Context();
            var st = RaidFixture.State(ctx);
            var rec = Light(ctx, st);
            Assert.That(PowerGrid.Of(ctx, st).OfSite(Id)?.PlantKw, Is.EqualTo(600));

            var rev = st.Rev;
            Plants.Damage(ctx, st, Id, rec.Hp - 1);
            Assert.That(st.Rev, Is.EqualTo(rev), "a hit that leaves it standing does not rebuild the network");
            Assert.That(RaidFixture.Count<PlantFellEvent>(st), Is.Zero);

            Plants.Damage(ctx, st, Id, 5);
            Assert.That(rec.Hp, Is.Zero);
            Assert.That(Plants.Down(st, Id), Is.True);
            Assert.That(RaidFixture.Count<PlantFellEvent>(st), Is.EqualTo(1));
            Assert.That(RaidFixture.Last<PlantFellEvent>(st).Name, Is.EqualTo("Riverside Works"));
            Assert.That(PowerGrid.Of(ctx, st).OfSite(Id)?.PlantKw ?? 0, Is.Zero, "it no longer supplies");

            Plants.Damage(ctx, st, Id, 5);
            Assert.That(RaidFixture.Count<PlantFellEvent>(st), Is.EqualTo(1), "it falls once");
        }

        [Test]
        public void TheSaveKeepsTheAimAndTheOwedRaid()
        {
            var ctx = Context();
            var st = RaidFixture.State(ctx, 3);
            Light(ctx, st);
            var a = Book(ctx, st);
            var load = SaveSerializer.ReadText(SaveSerializer.WriteText(st, ctx.Data), ctx.Data);
            Assert.That(load.Ok, Is.True, load.Reason);
            Assert.That(load.State.Director.Major.Plant, Is.EqualTo(Id));
            Assert.That(load.State.Director.PlantRaid, Is.EqualTo(Id));
            Assert.That(StateHash.Compute(load.State), Is.EqualTo(StateHash.Compute(st)));
        }

        [Test]
        public void AVersionEighteenAssaultUpgradesAimedAtHome()
        {
            var ctx = Context();
            var st = RaidFixture.State(ctx, 3);
            Book(ctx, st);
            var state = PersistenceFixture.Canonical(st);
            Assert.That(state, Does.Contain("\"plant\":\"\"").And.Contain("\"plantRaid\":\"\""));
            var state18 = state.Replace("\"plant\":\"\",", "").Replace(",\"plant\":\"\"", "")
                .Replace("\"plantRaid\":\"\",", "").Replace(",\"plantRaid\":\"\"", "");
            Assert.That(state18, Does.Not.Contain("\"plant\"").And.Not.Contain("\"plantRaid\""));

            var file = SaveSerializer.WriteText(st, ctx.Data);
            file = PersistenceFixture.Retarget(file, "state", state18);
            file = PersistenceFixture.Retarget(file, "version", "18");
            file = PersistenceFixture.Retarget(file, "hash", CanonicalJsonWriter.QuoteString(StateHash.Of(state18)));

            var old = SaveSerializer.ReadText(file, ctx.Data);
            Assert.That(old.Ok, Is.True, old.Reason);
            Assert.That(old.Header.Version, Is.EqualTo(18));
            Assert.That(old.Upgraded, Does.Contain("every assault on the Home core"));
            Assert.That(old.State.Director.Major, Is.Not.Null, "the booked assault survived the upgrade");
            Assert.That(old.State.Director.Major.Plant, Is.Empty);
            Assert.That(old.State.Director.PlantRaid, Is.Empty);
        }
    }
}
