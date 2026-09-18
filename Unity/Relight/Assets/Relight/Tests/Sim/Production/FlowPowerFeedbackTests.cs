using NUnit.Framework;
using System.Collections.Generic;
namespace Relight.Sim.Tests.Production
{
    public sealed class FlowPowerFeedbackTests
    {
        private static void Run(SimContext ctx, SimState st, int ticks)
        {
            for (var i=0;i<ticks;i++) { new PowerPhase().Tick(ctx,st,.05); new MachinePhase().Tick(ctx,st,.05); new FlowPhase().Tick(ctx,st,.05); st.Tick++; st.T+=.05; }
        }
        [TestCase(Dir.N,11,15)] [TestCase(Dir.E,13,17)] [TestCase(Dir.S,11,19)] [TestCase(Dir.W,9,17)]
        public void AnAdjacentBeltPullsRealExtractorOutputOnEachEdge(Dir dir,int x,int y)
        {
            var ctx=ProductionFixture.Context(); var st=ProductionFixture.State(ctx);
            var dig=ProductionFixture.Add(ctx,st,"excavator",10,16,Dir.S);
            var belt=ProductionFixture.Add(ctx,st,"belt",x,y,dir);
            ProductionFixture.Grid(ctx,st,14,16,16,16,50); ProductionFixture.Seal(ctx,st);
            Run(ctx,st,90);
            Assert.That(FlowQueries.Count(st,belt.Id),Is.GreaterThan(0));
            Assert.That(st.Stats.Mined,Is.EqualTo(FlowQueries.HeldCount(st)+dig.Inv.Total));
            Assert.That(ProductionFixture.Off(ctx,st),Is.Empty);
        }
        [Test]
        public void BlockedOutputSurvivesSavingAndCanBeCollected()
        {
            var ctx=ProductionFixture.Context(); var st=ProductionFixture.State(ctx);
            var dig=ProductionFixture.Add(ctx,st,"excavator",10,16,Dir.S);
            ProductionFixture.Grid(ctx,st,14,16,16,16,50); ProductionFixture.Seal(ctx,st);
            Run(ctx,st,200);
            Assert.That(dig.Inv.Total,Is.EqualTo(1));
            var loaded=SaveSerializer.ReadText(SaveSerializer.WriteText(st,ctx.Data),ctx.Data);
            Assert.That(loaded.Ok,Is.True,loaded.Reason);
            Assert.That(loaded.State.MachineById(dig.Id).Inv.Total,Is.EqualTo(1));
            loaded.State.Engineer.Pos=new Vec2(9,17);
            new MachineTransferHandler().TryApply(ctx,loaded.State,new MachineTransferCommand(dig.Id,ItemId.Steel,1,false),out var taken);
            Assert.That(taken.Accepted,Is.True,taken.Problem);
            Assert.That(loaded.State.MachineById(dig.Id).Inv.Total,Is.Zero);
            Assert.That(ProductionFixture.Off(ctx,loaded.State),Is.Empty);
            Assert.That(ProductionQueries.Description(ctx,st,dig.Id),Does.Contain("Output: 1"));
            Assert.That(ProductionQueries.Description(ctx,st,dig.Id),Does.Contain("powered"));
        }
        [Test]
        public void ABackedUpTwoTileLineKeepsItsGapAcrossTheSeam()
        {
            var ctx=ProductionFixture.Context(); var st=ProductionFixture.State(ctx);
            var chest=ProductionFixture.Add(ctx,st,"chest",2,6); chest.Inv.Add(ItemId.Steel,50);
            var a=ProductionFixture.Add(ctx,st,"belt",4,6,Dir.E);
            var b=ProductionFixture.Add(ctx,st,"belt",5,6,Dir.E); ProductionFixture.Seal(ctx,st);
            Run(ctx,st,200);
            var left=st.Flow.Of(a.Id).Items; var right=st.Flow.Of(b.Id).Items;
            Assert.That(left.Count,Is.EqualTo(4)); Assert.That(right.Count,Is.EqualTo(4));
            Assert.That(1+right[0].P-left[left.Count-1].P,Is.GreaterThanOrEqualTo(FlowRules.BeltSpacing-1e-9));
            Assert.That(ProductionFixture.Off(ctx,st),Is.Empty);
        }
        [Test]
        public void ATransferredItemKeepsItsPreviousPositionForSmoothRendering()
        {
            var ctx=ProductionFixture.Context(); var st=ProductionFixture.State(ctx);
            var a=ProductionFixture.Add(ctx,st,"belt",4,6,Dir.E);
            var b=ProductionFixture.Add(ctx,st,"belt",5,6,Dir.E);
            FlowRules.GiveItem(ctx,st,a,ItemId.Steel,.95);
            var before=FlowQueries.Position(st,a,.95);
            new FlowPhase().Tick(ctx,st,.05);
            var views=new List<BeltItemView>(); FlowQueries.ItemsOn(st,b,views);
            Assert.That(views.Count,Is.EqualTo(1));
            Assert.That(views[0].PreviousPos.X,Is.EqualTo(before.X).Within(1e-9));
            Assert.That(views[0].Pos.X,Is.GreaterThan(before.X));
        }
        [Test]
        public void HoverPowerDistinguishesMissingPoleDeadCircuitAndPoweredMachine()
        {
            var ctx=ProductionFixture.Context(); var st=ProductionFixture.State(ctx);
            var dig=ProductionFixture.Add(ctx,st,"excavator",10,16);
            Assert.That(PowerQueries.Description(ctx,st,dig.Id),Does.Contain("disconnected"));
            var (_,gen)=ProductionFixture.Grid(ctx,st,14,16,16,16,0);
            Assert.That(PowerQueries.Description(ctx,st,dig.Id),Does.Contain("connected · no supply"));
            gen.Inv.Add(ItemId.Coal,5);
            Assert.That(PowerQueries.Description(ctx,st,dig.Id),Does.Contain("powered"));
            Assert.That(PowerLinks.At(ctx,st,"pole",14,17),Has.Some.Matches<PowerLink>(l=>l.MachineId==dig.Id));
            var links=PowerLinks.At(ctx,st,dig.Kind,dig.X,dig.Y, dig.Dir,dig.Size);
            Assert.That(links.Count,Is.EqualTo(1),"a consumer belongs to one nearest pole");
        }
    }
}
