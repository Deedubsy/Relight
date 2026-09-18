using System;
using System.Text.RegularExpressions;
using NUnit.Framework;
namespace Relight.Sim.Tests
{
    /// <summary>
    /// The opening ore recipes (U-P-02) at the Home workshop, under U-D-44: the queue holds the ingredients, the
    /// workshop processes them wherever the engineer is, and the plates wait in its output tray to be collected.
    /// </summary>
    public sealed class OreOpeningTests
    {
        static (SimContext ctx,SimState st) Setup(ItemId ore,int count)
        {
            var ctx=Fixture.Context();var st=Fixture.State(ctx);Fixture.Place(ctx,st,"depot",10,10);
            Pockets.Take(ctx.Data,st.Engineer,ItemKey.Of(ore),count);st.Ledger=Ledger.Open(st,ctx.Data);return(ctx,st);
        }
        [TestCase("hand-steel",ItemId.IronOre,ItemId.Steel)]
        [TestCase("hand-copper",ItemId.CopperOre,ItemId.Copper)]
        public void OreBecomesTheSelectedPlateWithoutGridPower(string key,ItemId ore,ItemId plate)
        {
            var(ctx,st)=Setup(ore,5);var before=st.Engineer.Inv[plate];
            Assert.That(Fixture.Apply(ctx,st,new HandCraftCommand(5,key)).Accepted,Is.True);
            Fixture.Run(ctx,st,3.9);Assert.That(st.Hand.Output[plate],Is.Zero);
            Fixture.Run(ctx,st,16.1);
            Assert.That(st.Hand.Output[plate],Is.EqualTo(5),"the plates wait in the workshop tray");
            Assert.That(st.Engineer.Inv[plate],Is.EqualTo(before),"and none of them appeared in the Backpack on their own");
            Assert.That(st.Engineer.Inv[ore],Is.Zero);Assert.That(st.Engineer.Inv[ItemId.Magazine],Is.Zero);
            Assert.That(Fixture.Apply(ctx,st,new CollectWorkshopCommand()).Accepted,Is.True);
            Assert.That(st.Engineer.Inv[plate],Is.EqualTo(before+5));Assert.That(st.Hand.Output.IsEmpty,Is.True);
            Assert.That(Fixture.Off(ctx,st),Is.Empty);
        }
        /// <summary>
        /// Queued ingredients leave the Backpack at once, so a second order can only spend what is still carried
        /// and the same ore can never fund two batches.
        /// </summary>
        [Test]
        public void ReservedInputsCannotBeSpentTwice()
        {
            var(ctx,st)=Setup(ItemId.IronOre,5);
            Assert.That(Fixture.Apply(ctx,st,new HandCraftCommand(2,"hand-steel")).Accepted,Is.True);Fixture.Run(ctx,st,1);
            Assert.That(st.Engineer.Inv[ItemId.IronOre],Is.EqualTo(3),"the queue took its two ore out of the Backpack");
            Assert.That(st.Hand.Reserved[ItemId.IronOre],Is.EqualTo(2));
            Assert.That(Fixture.Apply(ctx,st,new HandCraftCommand(4,"hand-steel")).Accepted,Is.True);
            Assert.That(HandCraft.QueuedBatches(st),Is.EqualTo(5),"only the three ore still carried could be added");
            Assert.That(st.Hand.Jobs.Count,Is.EqualTo(1),"and they joined the one job rather than starting a second run");
            var full=Fixture.Apply(ctx,st,new HandCraftCommand(1,"hand-steel"));
            Assert.That(full.Accepted,Is.False);Assert.That(full.Problem,Is.EqualTo("Need recipe ingredients in Backpack"));
            Assert.That(Fixture.Apply(ctx,st,new CancelCraftCommand()).Accepted,Is.True);
            Assert.That(st.Engineer.Inv[ItemId.IronOre],Is.EqualTo(5));Assert.That(Fixture.Off(ctx,st),Is.Empty);
        }
        [Test]
        public void SmeltingProgressAndLayoutSurviveSaveLoad()
        {
            var(ctx,st)=Setup(ItemId.CopperOre,2);st.OpeningResourceVersion=1;
            Fixture.Apply(ctx,st,new HandCraftCommand(2,"hand-copper"));Fixture.Run(ctx,st,2);
            var loaded=SaveSerializer.ReadText(SaveSerializer.WriteText(st,ctx.Data),ctx.Data);
            Assert.That(loaded.Ok,Is.True,loaded.Reason);Assert.That(loaded.State.OpeningResourceVersion,Is.EqualTo(1));
            Assert.That(loaded.State.Hand.Jobs.Count,Is.EqualTo(1));
            Assert.That(loaded.State.Hand.Jobs[0].RecipeKey,Is.EqualTo("hand-copper"));
            Assert.That(loaded.State.Hand.Jobs[0].Batches,Is.EqualTo(2));
            Assert.That(loaded.State.Hand.Jobs[0].Progress,Is.EqualTo(st.Hand.Jobs[0].Progress).Within(1e-9));
            Assert.That(loaded.State.Hand.Reserved[ItemId.CopperOre],Is.EqualTo(2),"the reservation came back with it");
            Fixture.Run(ctx,loaded.State,6);
            Assert.That(loaded.State.Stats.Made[ItemId.Copper],Is.EqualTo(2));
            Assert.That(loaded.State.Hand.Output[ItemId.Copper],Is.EqualTo(2));
            Assert.That(loaded.State.Hand.Jobs,Is.Empty);Assert.That(Fixture.Off(ctx,loaded.State),Is.Empty);
        }
        /// <summary>
        /// Cancelling is lossless even with nowhere to put the refund: what the Backpack cannot take stays at the
        /// workshop, in its tray, and is collected once there is room. Nothing is destroyed and nothing is invented.
        /// </summary>
        [Test]
        public void CancelledIngredientsWaitInTheWorkshopWhenThePocketsAreFull()
        {
            var(ctx,st)=Setup(ItemId.IronOre,1);var e=st.Engineer;
            Assert.That(Fixture.Apply(ctx,st,new HandCraftCommand(1,"hand-steel")).Accepted,Is.True);Fixture.Run(ctx,st,1);
            e.Inv[ItemId.Steel]=0;e.Inv[ItemId.Copper]=0;e.Inv[ItemId.Magazine]=Pockets.Cap(ctx.Data)*200;
            st.Ledger=Ledger.Open(st,ctx.Data);
            var c=Fixture.Apply(ctx,st,new CancelCraftCommand());
            Assert.That(c.Accepted,Is.True);
            Assert.That(c.Problem,Is.EqualTo("Job cancelled. The ingredients are waiting in the workshop."));
            Assert.That(st.Hand.Output[ItemId.IronOre],Is.EqualTo(1));Assert.That(st.Hand.Reserved.IsEmpty,Is.True);
            Assert.That(st.Engineer.Inv[ItemId.IronOre],Is.Zero);Assert.That(Fixture.Off(ctx,st),Is.Empty);
            var loaded=SaveSerializer.ReadText(SaveSerializer.WriteText(st,ctx.Data),ctx.Data);Assert.That(loaded.Ok,Is.True,loaded.Reason);
            Assert.That(loaded.State.Hand.Output[ItemId.IronOre],Is.EqualTo(1));
            var full=Fixture.Apply(ctx,loaded.State,new CollectWorkshopCommand());
            Assert.That(full.Accepted,Is.False);Assert.That(full.Problem,Is.EqualTo("Backpack is full."));
            loaded.State.Engineer.Inv[ItemId.Magazine]-=200;loaded.State.Ledger=Ledger.Open(loaded.State,ctx.Data);
            Assert.That(Fixture.Apply(ctx,loaded.State,new CollectWorkshopCommand()).Accepted,Is.True);
            Assert.That(loaded.State.Engineer.Inv[ItemId.IronOre],Is.EqualTo(1));
            Assert.That(loaded.State.Hand.Output.IsEmpty,Is.True);Assert.That(Fixture.Off(ctx,loaded.State),Is.Empty);
        }
        /// <summary>
        /// A version-4 save: the v4 → v5 step restores the resource layout and the saved bullet queue, and the
        /// v5 → v6 step adds the U-D-44 members. The old single-batch fields are left exactly as they were, for
        /// <see cref="HandCraft.Normalise"/> to fold on the first tick.
        /// </summary>
        [Test]
        public void OldSaveDefaultsToBulletQueueAndOriginalResourceLayout()
        {
            var st=new SimState();st.Hand.Crafts=1;st.Hand.Crafting=true;st.Hand.CraftProg=7;
            var text=CanonicalJsonWriter.Write(st);
            text=Regex.Replace(text,"\"openingResourceVersion\":0,","");
            text=Regex.Replace(text,"\"recipeKey\":\"hand-bullets\",","");
            text=Regex.Replace(text,"\"refunds\":\\[[^\\]]*\\],","");
            text=Regex.Replace(text,"\"jobs\":\\[\\],","");
            text=Regex.Replace(text,"\"nextJobId\":1,","");
            text=Regex.Replace(text,"\"outputFull\":false,","");
            var doc=JsonValue.Parse(text,out var error);Assert.That(error,Is.Null.Or.Empty);
            Assert.That(SaveUpgrade.ToCurrent(doc,4,StateHash.Of(doc.ToCanonicalJson()),out var damaged,out _),Is.Empty);
            Assert.That(damaged,Is.False);Assert.That(doc.Member("openingResourceVersion").ToCanonicalJson(),Is.EqualTo("0"));
            Assert.That(doc.Member("hand").Member("recipeKey").ToCanonicalJson(),Is.EqualTo("\"hand-bullets\""));
            Assert.That(doc.Member("hand").Member("craftProg").ToCanonicalJson(),Is.EqualTo("7"));
            Assert.That(doc.Member("hand").Member("jobs").ToCanonicalJson(),Is.EqualTo("[]"),"the new queue arrives empty");
            Assert.That(doc.Member("hand").Member("nextJobId").ToCanonicalJson(),Is.EqualTo("1"));
            Assert.That(doc.Member("hand").Member("outputFull").ToCanonicalJson(),Is.EqualTo("false"));
        }
        [Test]
        public void AutomaticSmeltingAndStarterCostsMatchApprovedBalance()
        {
            var d=ReferenceData.Create();d.TryRecipe("steel-plates",out var steel);Assert.That(steel.Seconds,Is.EqualTo(2));Assert.That(steel.Inputs[0].Count,Is.EqualTo(1));
            d.TryRecipe("hand-bullets",out var ammo);Assert.That(ammo.Seconds,Is.EqualTo(12));
            d.TryMachine("generator",out var gen);Assert.That(gen.Cost[0].Count,Is.EqualTo(20));Assert.That(gen.PowerKw,Is.EqualTo(-300));
        }
    }
}
