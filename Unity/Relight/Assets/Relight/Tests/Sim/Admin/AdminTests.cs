using NUnit.Framework;
using Relight.Sim.Tests.Production;

namespace Relight.Sim.Tests.Admin
{
    public sealed class AdminTests
    {
        private SimContext ctx; private SimState st;
        [SetUp]public void Setup(){ctx=ProductionFixture.Context();st=ProductionFixture.State(ctx);st.Engineer.Pos=new Vec2(30.5,40.5);ProductionFixture.Seal(ctx,st);}
        private CommandResult Run(AdminCommand c){new AdminHandler().TryApply(ctx,st,c,out var r);return r;}
        [TestCase(0)] [TestCase(-1)] [TestCase(10001)]
        public void InvalidGrantQuantitiesDoNotChangeTheSave(int n)
        {var before=CanonicalJsonWriter.Write(st);Assert.That(Run(new AdminCommand("grant","steel",n)).Accepted,Is.False);Assert.That(CanonicalJsonWriter.Write(st),Is.EqualTo(before));}
        [Test]public void GrantsRespectCapacityAndNeverPretendToBeMining()
        {
            Assert.That(Run(new AdminCommand("grant","steel",10000)).Accepted,Is.True);
            Assert.That(Pockets.Used(ctx.Data,st.Engineer),Is.LessThanOrEqualTo(Pockets.Cap(ctx.Data)));
            Assert.That(st.Engineer.Inv[ItemId.Steel],Is.LessThan(10000));
            Assert.That(Run(new AdminCommand("grant","copper",1)).Accepted,Is.False);
            Assert.That(st.Stats.Mined,Is.Zero);Assert.That(ProductionFixture.Off(ctx,st),Is.Empty);
        }
        [Test]public void GrantsDoNotHideAnExistingLedgerDiscrepancy()
        {
            st.Engineer.Inv[ItemId.Copper]=3;
            Run(new AdminCommand("grant","steel",15));
            var ledger=Ledger.Conservation(st,ctx.Data);
            Assert.That(ledger.Unexplained[ItemId.Copper],Is.EqualTo(3));
        }
        [Test]public void GrantAndPackedStructureCanBeUsedThroughNormalSystems()
        {
            Assert.That(Run(new AdminCommand("grant","belt",3)).Accepted,Is.True);
            var result=Placement.Place(ctx,st,"belt",31,40,Dir.E);
            Assert.That(result.ok,Is.True,result.reason);Assert.That(ProductionFixture.Off(ctx,st),Is.Empty);
            Assert.That(Run(new AdminCommand("weapon","rifle")).Accepted,Is.True);
            var w=st.Weapons.Owned[0];Assert.That(st.Engineer.Inv[new ItemKey(w.Id)],Is.EqualTo(1));
            Assert.That(WeaponRules.SlotChange(ctx,st,w.Id,0,true).ok,Is.True);
            Run(new AdminCommand("ammo"));Assert.That(w.Loaded,Is.GreaterThan(0));Assert.That(ProductionFixture.Off(ctx,st),Is.Empty);
        }
        [Test]public void SavingRetainsGrantsButResetsTemporaryCheats()
        {
            Run(new AdminCommand("grant","steel",12));Run(new AdminCommand("invulnerable"));Run(new AdminCommand("freeze"));Run(new AdminCommand("lighting",Amount:0));
            var loaded=SaveSerializer.ReadText(SaveSerializer.WriteText(st,ctx.Data),ctx.Data);
            Assert.That(loaded.Ok,Is.True,loaded.Reason);Assert.That(loaded.State.Engineer.Inv[ItemId.Steel],Is.EqualTo(12));
            Assert.That(loaded.State.Admin.Invulnerable,Is.False);Assert.That(loaded.State.Admin.FreezeEnemies,Is.False);Assert.That(loaded.State.Admin.Lighting,Is.EqualTo(-1));Assert.That(ProductionFixture.Off(ctx,loaded.State),Is.Empty);
        }
        [Test]public void SpawnsUseTheRequestedTypeAndClearDistinctTiles()
        {
            var result=Run(new AdminCommand("spawn","spitter",5,1,5));Assert.That(result.Accepted,Is.True,result.Problem);
            Assert.That(st.Enemies.Actors.Count,Is.EqualTo(5));
            foreach(var e in st.Enemies.Actors){Assert.That(e.Kind,Is.EqualTo("spitter"));Assert.That(Ground.CanStand(ctx,st,e.Pos.X,e.Pos.Y,.3),Is.True);}
            var ids=new System.Collections.Generic.HashSet<int>();foreach(var e in st.Enemies.Actors)Assert.That(ids.Add(e.Origin),Is.True);
        }
        [TestCase("unknown",5,8)] [TestCase("skitter",-5,8)] [TestCase("skitter",101,8)] [TestCase("skitter",5,100000)]
        public void InvalidSpawnsAreRejectedWithoutChangingState(string kind,int count,int distance)
        {var before=CanonicalJsonWriter.Write(st);Assert.That(Run(new AdminCommand("spawn",kind,count,0,distance)).Accepted,Is.False);Assert.That(CanonicalJsonWriter.Write(st),Is.EqualTo(before));}
        [Test]public void InvulnerabilityStopsDamageAndResetsIndependently()
        {
            Run(new AdminCommand("invulnerable"));var hp=st.Engineer.Hp;st.Engineer.TakeDamage(ctx,st,30);Assert.That(st.Engineer.Hp,Is.EqualTo(hp));
            var other=ProductionFixture.State(ctx);other.Engineer.TakeDamage(ctx,other,30);Assert.That(other.Engineer.Hp,Is.LessThan(hp));
            Run(new AdminCommand("reset-overrides"));st.Engineer.TakeDamage(ctx,st,30);Assert.That(st.Engineer.Hp,Is.LessThan(hp));
            Run(new AdminCommand("heal"));Assert.That(st.Engineer.Hp,Is.EqualTo(hp));
        }
        [Test]public void FreezeStopsActorsAndNightDoesNotSkipTime()
        {
            Run(new AdminCommand("spawn","skitter",1,0,4));Run(new AdminCommand("freeze"));
            var actor=st.Enemies.Actors[0];var before=actor.Pos;new EnemyPhase().Tick(ctx,st,.5);Assert.That(actor.Pos,Is.EqualTo(before));
            var time=st.T;Run(new AdminCommand("lighting",Amount:0));Assert.That(LightQueries.Daylight(ctx,st).Daylight,Is.Zero);Assert.That(st.T,Is.EqualTo(time));
            Run(new AdminCommand("clear-enemies"));Assert.That(st.Enemies.Actors,Is.Empty);
        }
        [Test]public void GeneratorAndTurretRefillsRemainConservedAndCapped()
        {
            var gen=ProductionFixture.Add(ctx,st,"generator",20,40);var turret=ProductionFixture.Add(ctx,st,"turret",24,40);ProductionFixture.Seal(ctx,st);
            Run(new AdminCommand("fuel"));Run(new AdminCommand("ammo"));
            Assert.That(gen.Inv[ItemId.Coal],Is.EqualTo(MachineInventory.GeneratorFuelCap(ctx.Data)));
            Assert.That(turret.Rounds,Is.EqualTo(TurretHopper.Capacity(ctx.Data,turret)));Assert.That(ProductionFixture.Off(ctx,st),Is.Empty);
        }
    }
}
