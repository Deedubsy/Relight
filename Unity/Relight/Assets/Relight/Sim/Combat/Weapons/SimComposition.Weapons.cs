using System.Collections.Generic;

namespace Relight.Sim
{
    /// <summary>
    /// The Weapon sub-slot of the fixed composition (contracts.md §Composition; C-02 / C-03, W-C).
    ///
    /// DELIBERATE DIFFERENCE: <see cref="MiningPhase"/> and <see cref="MiningHandler"/> belong beside
    /// <see cref="HandCraftPhase"/> in the Inventory slot — the reference ticks both halves inside one
    /// <c>tickHand</c> (flow.ts:995-1030). The Weapon sub-slot is the only one W-C owns, and
    /// <c>Sim/Actor/SimComposition.Inventory.cs</c> is another worker's file this wave, so they are registered here
    /// instead and run after production rather than before it. Nothing in the tick reads mining state, so the order
    /// is not observable; the W-C report asks the Inventory slot's owner to move the two lines.
    /// </summary>
    public static partial class SimComposition
    {
        static partial void AddWeaponPhases(List<ITickPhase> list)
        {
            list.Add(new MiningPhase());
            list.Add(new WeaponPhase());
        }

        static partial void AddWeaponInitializers(List<IStateInitializer> list)
        {
            list.Add(new WeaponInitializer());
        }

        static partial void AddWeaponHandlers(List<ICommandHandler> list)
        {
            list.Add(new MiningHandler());
            list.Add(new EquipmentHandler());
            list.Add(new WeaponHandler());
        }
    }

    /// <summary>
    /// New-game equipment, ported from reference equipment.ts <c>initEquipment(st, fresh = true)</c> (26-41).
    /// A fresh campaign gets the record and NOTHING ELSE: no weapon, no slot filled, no ammunition — the rifle is
    /// crafted at the Home workshop for 10 steel + 4 copper (catalogue recipe "rifle"). The <c>fresh === false</c>
    /// branch exists only to migrate legacy saves that had the abstract pre-equipment rifle, which the port never
    /// had, so it is not carried over.
    /// Fresh games receive construction shortcuts, which grant no items. Save loading preserves the saved bar.
    /// </summary>
    public sealed class WeaponInitializer : IStateInitializer
    {
        public void Init(SimContext ctx, SimState st)
        {
            WeaponRules.NormaliseBar(st);
            var defaults = WeaponRules.DefaultBar;
            for(int i=0;i<defaults.Length;i++)
                if(ctx.Data.TryMachine(defaults[i],out _)) WeaponRules.AssignBar(st,i,defaults[i]);
        }
    }
}
