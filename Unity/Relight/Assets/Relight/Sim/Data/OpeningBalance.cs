using System.Collections.Generic;
namespace Relight.Sim
{
    // Unity opening balance, independent of the preserved Phaser export.
    public static class OpeningBalance
    {
        /// <summary>
        /// REL-129 / U-D-71 note (4), the owner's "'Automatic resupply working' sticks for a minute after completing
        /// before changing to 'Expand your defences'". The reference's 45 s, which the owner read as a minute.
        ///
        /// This card is not like the "Attack repelled" one beside it (<c>AckS</c>, 60 s). That one reports an event
        /// and the player is waiting for nothing; this one stands *between* a finished objective and the next one, so
        /// every second of it is a second the player has finished the task and cannot see what to do next. It is a
        /// completion acknowledgement, and what it advises — watch the Assembler's inputs — is neither time-critical
        /// nor only available here.
        ///
        /// **The figure is the implementer's** (U-P-31): the owner reported the symptom, not a number.
        /// </summary>
        public const double SupplyAckSeconds = 10;

        public static GameData Apply(GameData d)
        {
            var items = new List<ItemDef>(d.Items);
            for(var i=0;i<items.Count;i++) if(items[i].Id==ItemId.Copper) items[i]=items[i] with { DisplayName="Copper plate" };
            var machines = new List<MachineSpec>(d.Machines);
            for(var i=0;i<machines.Count;i++)
            {
                var m=machines[i];
                if(m.Key=="generator" || m.Key=="foundry") m=m with { Cost=new[]{new ItemStack(ItemId.Steel,20),new ItemStack(ItemId.Copper,5)} };
                if(m.Key=="chest") m=m with { Cost=new[]{new ItemStack(ItemId.Steel,5)} };
                // U-D-53: the opening teaches "choose a recipe", so the three machines its chain names arrive
                // unset. A default recipe made "Set up the Foundry" and "set it to Bullets" complete themselves
                // the instant the machine was placed. Every consumer already handles a null recipe
                // (ProductionRules.RecipeOf → null, MachineInventory.Accepts, ProductionQueries.Status).
                if(m.Key=="foundry" || m.Key=="assembler" || m.Key=="assembler2") m=m with { DefaultRecipe="" };
                machines[i]=m;
            }
            var recipes=new List<Recipe>();
            foreach(var r in d.Recipes)
            {
                if(r.Key=="steel-plates" || r.Key=="refined-copper") recipes.Add(r with {Inputs=new[]{new ItemStack(r.Inputs[0].Item,1)},Seconds=2});
                else recipes.Add(r.Key=="hand-bullets" ? r with {Seconds=12} : r);
            }
            if(!d.TryRecipe("hand-steel",out _)) recipes.Add(new Recipe("hand-steel","Smelt Steel plates",new[]{new ItemStack(ItemId.IronOre,1)},new[]{new ItemStack(ItemId.Steel,1)},4,"Home workshop",Kind:"provisional",Source:"Unity/Docs/RECIPE_REVIEW_2026-09-15.md; owner approval 2026-09-15",Provisional:true));
            if(!d.TryRecipe("hand-copper",out _)) recipes.Add(new Recipe("hand-copper","Smelt Copper plates",new[]{new ItemStack(ItemId.CopperOre,1)},new[]{new ItemStack(ItemId.Copper,1)},4,"Home workshop",Kind:"provisional",Source:"Unity/Docs/RECIPE_REVIEW_2026-09-15.md; owner approval 2026-09-15",Provisional:true));
            // Written as an absolute value, so applying the layer twice lands where applying it once did.
            var opening = d.Opening != null && d.Opening.SupplyAckS != SupplyAckSeconds
                ? d.Opening with { SupplyAckS = SupplyAckSeconds }
                : d.Opening;
            return new GameData(items,machines,recipes,d.Engineer with {HandBulletSeconds=12},d.World,d.Weapons,d.Enemies,d.Ammunition,d.Turrets,d.Power,d.Time,d.Raids,opening,d.Stake,d.Defence,d.Siege);
        }
    }
}
