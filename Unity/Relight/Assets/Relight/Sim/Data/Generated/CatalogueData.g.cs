// ------------------------------------------------------------------------------
// GENERATED — do not edit.
// Produced by Unity/Relight/Tools/export/exportCatalogue.ts from the reference tables in packages/sim/src.
// Regenerate with:  npx tsx Unity/Relight/Tools/export/exportCatalogue.ts   (run from the repository root)
// Scope filter: Unity/Docs/CONTENT_CATALOGUE.md §17. Every row carries the catalogue Kind and Source.
// ------------------------------------------------------------------------------

namespace Relight.Sim
{
    /// <summary>The exported content tables. <see cref="ReferenceData"/> and Relight.Data's asset generator both read this.</summary>
    public static class CatalogueData
    {
        /// <summary>Where the tables came from, for the validator's report.</summary>
        public const string Reference = "packages/sim/src/";

        private static readonly ItemStack[] NoStacks = new ItemStack[0];

        public static ItemDef[] Items() => new[]
        {
            new ItemDef(ItemId.Overclock, "overclock", "Overclock Module", 1, "current", "packages/sim/src/engineer.ts:35", false),
            new ItemDef(ItemId.AlienArtifact, "alienartifact", "Alien Artifact", 20, "current", "packages/sim/src/engineer.ts:35", false),
            new ItemDef(ItemId.IronOre, "ironore", "Iron ore", 50, "current", "packages/sim/src/engineer.ts:35", false),
            new ItemDef(ItemId.CopperOre, "copperore", "Copper ore", 50, "current", "packages/sim/src/engineer.ts:35", false),
            new ItemDef(ItemId.Crude, "crude", "Crude oil", 50, "current", "packages/sim/src/engineer.ts:35", false),
            new ItemDef(ItemId.Fuel, "fuel", "Refined fuel", 50, "current", "packages/sim/src/engineer.ts:35", false),
            new ItemDef(ItemId.Polymer, "polymer", "Polymer", 50, "current", "packages/sim/src/engineer.ts:35", false),
            new ItemDef(ItemId.Shell, "shell", "Shells", 20, "current", "packages/sim/src/engineer.ts:35", false),
            new ItemDef(ItemId.Core1, "core1", "Power core 1", 1, "current", "packages/sim/src/engineer.ts:36", false),
            new ItemDef(ItemId.Core2, "core2", "Power core 2", 1, "current", "packages/sim/src/engineer.ts:36", false),
            new ItemDef(ItemId.Core3, "core3", "Power core 3", 1, "current", "packages/sim/src/engineer.ts:36", false),
            new ItemDef(ItemId.Steel, "steel", "Steel plates", 50, "current", "packages/sim/src/engineer.ts:35", false),
            new ItemDef(ItemId.Copper, "copper", "Copper", 50, "current", "packages/sim/src/engineer.ts:35", false),
            new ItemDef(ItemId.Stone, "stone", "Stone", 50, "current", "packages/sim/src/engineer.ts:35", false),
            new ItemDef(ItemId.Coal, "coal", "Coal", 50, "current", "packages/sim/src/engineer.ts:35", false),
            new ItemDef(ItemId.Magazine, "magazine", "Bullets", 200, "current", "packages/sim/src/engineer.ts:35; packages/sim/src/flow.ts:156", false),
            new ItemDef(ItemId.Wire, "wire", "Wire", 50, "current", "packages/sim/src/engineer.ts:35", false),
            new ItemDef(ItemId.Frame, "frame", "Frame", 50, "current", "packages/sim/src/engineer.ts:35", false),
            new ItemDef(ItemId.Board, "board", "Board", 50, "current", "packages/sim/src/engineer.ts:35", false),
            new ItemDef(ItemId.Concrete, "concrete", "Concrete", 50, "current", "packages/sim/src/engineer.ts:35", false),
        };

        public static MachineSpec[] Machines() => new[]
        {
            new MachineSpec("excavator", "Excavator", 3, new[] { new ItemStack(ItemId.Steel, 10) }, true, 60.0, 1,
                FuelCap: 0.0, AmmoCap: 0, RatePerS: 0.5, ReachTiles: 0.0, LightRadiusTiles: 0.0,
                ConeRangeTiles: 0.0, ConeHalfAngleRad: 0.0, Hp: 0.0, Unlock: "",
                Kind: "current", Source: "packages/sim/src/flow.ts:211; packages/sim/src/flow.ts:218; packages/sim/src/flow.ts:232; packages/sim/src/constants.ts:35", Provisional: false),
            new MachineSpec("pumpjack", "Pumpjack", 3, new[] { new ItemStack(ItemId.Steel, 20), new ItemStack(ItemId.Copper, 10) }, true, 60.0, 1,
                FuelCap: 0.0, AmmoCap: 0, RatePerS: 0.5, ReachTiles: 0.0, LightRadiusTiles: 0.0,
                ConeRangeTiles: 0.0, ConeHalfAngleRad: 0.0, Hp: 0.0, Unlock: "",
                Kind: "current", Source: "packages/sim/src/flow.ts:211; packages/sim/src/flow.ts:218; packages/sim/src/flow.ts:232; packages/sim/src/constants.ts:35; packages/sim/src/flow.ts:943", Provisional: false),
            new MachineSpec("foundry", "Foundry", 3, new[] { new ItemStack(ItemId.Steel, 30), new ItemStack(ItemId.Copper, 10) }, true, 80.0, 1,
                FuelCap: 0.0, AmmoCap: 0, RatePerS: 0.0, ReachTiles: 0.0, LightRadiusTiles: 0.0,
                ConeRangeTiles: 0.0, ConeHalfAngleRad: 0.0, Hp: 0.0, Unlock: "",
                Kind: "current", Source: "packages/sim/src/flow.ts:211; packages/sim/src/flow.ts:218; packages/sim/src/flow.ts:232", Provisional: false),
            new MachineSpec("refinery", "Refinery", 3, new[] { new ItemStack(ItemId.Steel, 40), new ItemStack(ItemId.Copper, 20) }, true, 100.0, 1,
                FuelCap: 0.0, AmmoCap: 0, RatePerS: 0.0, ReachTiles: 0.0, LightRadiusTiles: 0.0,
                ConeRangeTiles: 0.0, ConeHalfAngleRad: 0.0, Hp: 0.0, Unlock: "",
                Kind: "current", Source: "packages/sim/src/flow.ts:211; packages/sim/src/flow.ts:218; packages/sim/src/flow.ts:232", Provisional: false),
            new MachineSpec("assembler", "Assembler", 3, new[] { new ItemStack(ItemId.Steel, 40), new ItemStack(ItemId.Copper, 20) }, true, 100.0, 2,
                FuelCap: 0.0, AmmoCap: 0, RatePerS: 0.0, ReachTiles: 0.0, LightRadiusTiles: 0.0,
                ConeRangeTiles: 0.0, ConeHalfAngleRad: 0.0, Hp: 0.0, Unlock: "",
                Kind: "current", Source: "packages/sim/src/flow.ts:211; packages/sim/src/flow.ts:218; packages/sim/src/flow.ts:232", Provisional: false),
            new MachineSpec("assembler2", "Assembler Mk2", 3, new[] { new ItemStack(ItemId.Polymer, 4), new ItemStack(ItemId.Steel, 60), new ItemStack(ItemId.Copper, 30) }, true, 150.0, 2,
                FuelCap: 0.0, AmmoCap: 0, RatePerS: 0.0, ReachTiles: 0.0, LightRadiusTiles: 0.0,
                ConeRangeTiles: 0.0, ConeHalfAngleRad: 0.0, Hp: 0.0, Unlock: "material (Polymer)",
                Kind: "current", Source: "packages/sim/src/flow.ts:211; packages/sim/src/flow.ts:218; packages/sim/src/flow.ts:232", Provisional: false),
            new MachineSpec("mixer", "Mixer", 3, new[] { new ItemStack(ItemId.Steel, 20), new ItemStack(ItemId.Copper, 10) }, true, 60.0, 1,
                FuelCap: 0.0, AmmoCap: 0, RatePerS: 0.0, ReachTiles: 0.0, LightRadiusTiles: 0.0,
                ConeRangeTiles: 0.0, ConeHalfAngleRad: 0.0, Hp: 0.0, Unlock: "Concrete crew",
                Kind: "current", Source: "packages/sim/src/flow.ts:211; packages/sim/src/flow.ts:218; packages/sim/src/flow.ts:232", Provisional: false),
            new MachineSpec("alienworkbench", "Alien workbench", 3, new[] { new ItemStack(ItemId.Steel, 30), new ItemStack(ItemId.Copper, 10), new ItemStack(ItemId.Frame, 4), new ItemStack(ItemId.Board, 2) }, true, 80.0, 4,
                FuelCap: 0.0, AmmoCap: 0, RatePerS: 0.0, ReachTiles: 0.0, LightRadiusTiles: 0.0,
                ConeRangeTiles: 0.0, ConeHalfAngleRad: 0.0, Hp: 0.0, Unlock: "Schematic",
                Kind: "current", Source: "packages/sim/src/flow.ts:211; packages/sim/src/flow.ts:218; packages/sim/src/flow.ts:232", Provisional: false),
            new MachineSpec("chest", "Supply chest", 2, new[] { new ItemStack(ItemId.Steel, 10) }, true, 0.0, 200,
                FuelCap: 0.0, AmmoCap: 0, RatePerS: 0.0, ReachTiles: 0.0, LightRadiusTiles: 0.0,
                ConeRangeTiles: 0.0, ConeHalfAngleRad: 0.0, Hp: 0.0, Unlock: "",
                Kind: "current", Source: "packages/sim/src/flow.ts:211; packages/sim/src/flow.ts:218; packages/sim/src/flow.ts:232; packages/sim/src/flow.ts:244", Provisional: false),
            new MachineSpec("belt", "Belt", 1, new[] { new ItemStack(ItemId.Steel, 1) }, false, 0.0, 0,
                FuelCap: 0.0, AmmoCap: 0, RatePerS: 7.5, ReachTiles: 0.0, LightRadiusTiles: 0.0,
                ConeRangeTiles: 0.0, ConeHalfAngleRad: 0.0, Hp: 0.0, Unlock: "",
                Kind: "current", Source: "packages/sim/src/flow.ts:211; packages/sim/src/flow.ts:218; packages/sim/src/flow.ts:232; packages/sim/src/constants.ts:37", Provisional: false),
            new MachineSpec("fastbelt", "Fast belt", 1, new[] { new ItemStack(ItemId.Polymer, 1), new ItemStack(ItemId.Steel, 2), new ItemStack(ItemId.Copper, 1) }, false, 0.0, 0,
                FuelCap: 0.0, AmmoCap: 0, RatePerS: 15.0, ReachTiles: 0.0, LightRadiusTiles: 0.0,
                ConeRangeTiles: 0.0, ConeHalfAngleRad: 0.0, Hp: 0.0, Unlock: "",
                Kind: "current", Source: "packages/sim/src/flow.ts:211; packages/sim/src/flow.ts:218; packages/sim/src/flow.ts:232; packages/sim/src/constants.ts:37", Provisional: false),
            new MachineSpec("inserter", "Inserter", 1, new[] { new ItemStack(ItemId.Steel, 1), new ItemStack(ItemId.Copper, 1) }, false, 10.0, 0,
                FuelCap: 0.0, AmmoCap: 0, RatePerS: 1.0, ReachTiles: 0.0, LightRadiusTiles: 0.0,
                ConeRangeTiles: 0.0, ConeHalfAngleRad: 0.0, Hp: 0.0, Unlock: "",
                Kind: "current", Source: "packages/sim/src/flow.ts:211; packages/sim/src/flow.ts:218; packages/sim/src/flow.ts:232; packages/sim/src/constants.ts:37", Provisional: false),
            new MachineSpec("underground", "Underground belt", 1, new[] { new ItemStack(ItemId.Steel, 5), new ItemStack(ItemId.Copper, 2) }, false, 0.0, 0,
                FuelCap: 0.0, AmmoCap: 0, RatePerS: 0.0, ReachTiles: 0.0, LightRadiusTiles: 0.0,
                ConeRangeTiles: 0.0, ConeHalfAngleRad: 0.0, Hp: 0.0, Unlock: "",
                Kind: "current", Source: "packages/sim/src/flow.ts:211; packages/sim/src/flow.ts:218; packages/sim/src/flow.ts:232", Provisional: false),
            new MachineSpec("splitter", "Priority splitter", 1, new[] { new ItemStack(ItemId.Steel, 5), new ItemStack(ItemId.Copper, 2) }, false, 0.0, 0,
                FuelCap: 0.0, AmmoCap: 0, RatePerS: 0.0, ReachTiles: 0.0, LightRadiusTiles: 0.0,
                ConeRangeTiles: 0.0, ConeHalfAngleRad: 0.0, Hp: 0.0, Unlock: "",
                Kind: "current", Source: "packages/sim/src/flow.ts:211; packages/sim/src/flow.ts:218; packages/sim/src/flow.ts:232", Provisional: false),
            new MachineSpec("generator", "Generator", 2, new[] { new ItemStack(ItemId.Steel, 30), new ItemStack(ItemId.Copper, 10) }, true, -300.0, 1,
                FuelCap: 50.0, AmmoCap: 0, RatePerS: 0.0, ReachTiles: 0.0, LightRadiusTiles: 0.0,
                ConeRangeTiles: 0.0, ConeHalfAngleRad: 0.0, Hp: 0.0, Unlock: "",
                Kind: "current", Source: "packages/sim/src/flow.ts:211; packages/sim/src/flow.ts:218; packages/sim/src/flow.ts:232; packages/sim/src/recipes.ts:23; packages/sim/src/flow.ts:237", Provisional: false),
            new MachineSpec("pole", "Pole", 1, new[] { new ItemStack(ItemId.Steel, 1), new ItemStack(ItemId.Copper, 1) }, false, 0.0, 0,
                FuelCap: 0.0, AmmoCap: 0, RatePerS: 0.0, ReachTiles: 8.0, LightRadiusTiles: 0.0,
                ConeRangeTiles: 0.0, ConeHalfAngleRad: 0.0, Hp: 0.0, Unlock: "",
                Kind: "current", Source: "packages/sim/src/flow.ts:211; packages/sim/src/flow.ts:218; packages/sim/src/flow.ts:232; packages/sim/src/recipes.ts:38", Provisional: false),
            new MachineSpec("bigpole", "Big pole", 2, new[] { new ItemStack(ItemId.Steel, 4), new ItemStack(ItemId.Copper, 4) }, false, 0.0, 0,
                FuelCap: 0.0, AmmoCap: 0, RatePerS: 0.0, ReachTiles: 12.0, LightRadiusTiles: 0.0,
                ConeRangeTiles: 0.0, ConeHalfAngleRad: 0.0, Hp: 0.0, Unlock: "Electricians",
                Kind: "current", Source: "packages/sim/src/flow.ts:211; packages/sim/src/flow.ts:218; packages/sim/src/flow.ts:232; packages/sim/src/recipes.ts:43", Provisional: false),
            new MachineSpec("substation", "Substation", 3, new[] { new ItemStack(ItemId.Steel, 50), new ItemStack(ItemId.Copper, 25) }, false, 0.0, 0,
                FuelCap: 0.0, AmmoCap: 0, RatePerS: 0.0, ReachTiles: 8.0, LightRadiusTiles: 0.0,
                ConeRangeTiles: 0.0, ConeHalfAngleRad: 0.0, Hp: 0.0, Unlock: "",
                Kind: "current", Source: "packages/sim/src/flow.ts:211; packages/sim/src/flow.ts:218; packages/sim/src/flow.ts:232; packages/sim/src/campaignPower.ts:20", Provisional: false),
            new MachineSpec("lamp", "Lamp", 1, new[] { new ItemStack(ItemId.Steel, 1), new ItemStack(ItemId.Copper, 1) }, false, 5.0, 0,
                FuelCap: 0.0, AmmoCap: 0, RatePerS: 0.0, ReachTiles: 0.0, LightRadiusTiles: 4.0,
                ConeRangeTiles: 0.0, ConeHalfAngleRad: 0.0, Hp: 0.0, Unlock: "",
                Kind: "current", Source: "packages/sim/src/flow.ts:211; packages/sim/src/flow.ts:218; packages/sim/src/flow.ts:232; packages/sim/src/recipes.ts:38", Provisional: false),
            new MachineSpec("arclamp", "Arc lamp", 1, new[] { new ItemStack(ItemId.Steel, 4), new ItemStack(ItemId.Copper, 4) }, false, 12.0, 0,
                FuelCap: 0.0, AmmoCap: 0, RatePerS: 0.0, ReachTiles: 0.0, LightRadiusTiles: 6.0,
                ConeRangeTiles: 0.0, ConeHalfAngleRad: 0.0, Hp: 0.0, Unlock: "Lamplighters",
                Kind: "current", Source: "packages/sim/src/flow.ts:211; packages/sim/src/flow.ts:218; packages/sim/src/flow.ts:232; packages/sim/src/flow.ts:68", Provisional: false),
            new MachineSpec("floodlight", "Floodlight", 2, new[] { new ItemStack(ItemId.Steel, 10), new ItemStack(ItemId.Copper, 5) }, false, 40.0, 0,
                FuelCap: 0.0, AmmoCap: 0, RatePerS: 0.0, ReachTiles: 0.0, LightRadiusTiles: 0.0,
                ConeRangeTiles: 12.0, ConeHalfAngleRad: 0.5235987755982988, Hp: 0.0, Unlock: "Electricians",
                Kind: "current", Source: "packages/sim/src/flow.ts:211; packages/sim/src/flow.ts:218; packages/sim/src/flow.ts:232; packages/sim/src/recipes.ts:43", Provisional: false),
            new MachineSpec("turret", "Gun turret", 2, new[] { new ItemStack(ItemId.Steel, 15), new ItemStack(ItemId.Copper, 5) }, true, 20.0, 1,
                FuelCap: 0.0, AmmoCap: 50, RatePerS: 0.0, ReachTiles: 0.0, LightRadiusTiles: 0.0,
                ConeRangeTiles: 0.0, ConeHalfAngleRad: 0.0, Hp: 100.0, Unlock: "",
                Kind: "provisional", Source: "packages/sim/src/flow.ts:211; packages/sim/src/flow.ts:218; packages/sim/src/flow.ts:232; packages/sim/src/recipes.ts:37; packages/sim/src/constants.ts:42; packages/sim/src/campaignDefence.ts:13", Provisional: true),
            new MachineSpec("cannon", "Cannon", 2, new[] { new ItemStack(ItemId.Steel, 40), new ItemStack(ItemId.Copper, 20) }, true, 0.0, 1,
                FuelCap: 0.0, AmmoCap: 20, RatePerS: 0.0, ReachTiles: 0.0, LightRadiusTiles: 0.0,
                ConeRangeTiles: 0.0, ConeHalfAngleRad: 0.0, Hp: 140.0, Unlock: "Arsenal",
                Kind: "provisional", Source: "packages/sim/src/flow.ts:211; packages/sim/src/flow.ts:218; packages/sim/src/flow.ts:232; packages/sim/src/campaignDefence.ts:75; packages/sim/src/progression.ts:17", Provisional: true),
            new MachineSpec("wall", "Wall", 1, new[] { new ItemStack(ItemId.Steel, 2) }, false, 0.0, 0,
                FuelCap: 0.0, AmmoCap: 0, RatePerS: 0.0, ReachTiles: 0.0, LightRadiusTiles: 0.0,
                ConeRangeTiles: 0.0, ConeHalfAngleRad: 0.0, Hp: 120.0, Unlock: "",
                Kind: "current", Source: "packages/sim/src/flow.ts:211; packages/sim/src/flow.ts:218; packages/sim/src/flow.ts:232; packages/sim/src/campaignDefence.ts:13", Provisional: false),
            new MachineSpec("barricade", "Barricade", 1, new[] { new ItemStack(ItemId.Steel, 2), new ItemStack(ItemId.Concrete, 4) }, false, 0.0, 0,
                FuelCap: 0.0, AmmoCap: 0, RatePerS: 0.0, ReachTiles: 0.0, LightRadiusTiles: 0.0,
                ConeRangeTiles: 0.0, ConeHalfAngleRad: 0.0, Hp: 240.0, Unlock: "Concrete crew",
                Kind: "current", Source: "packages/sim/src/flow.ts:211; packages/sim/src/flow.ts:218; packages/sim/src/flow.ts:232; packages/sim/src/campaignDefence.ts:13", Provisional: false),
            new MachineSpec("depot", "Depot", 6, NoStacks, false, 0.0, 0,
                FuelCap: 0.0, AmmoCap: 0, RatePerS: 0.0, ReachTiles: 0.0, LightRadiusTiles: 0.0,
                ConeRangeTiles: 0.0, ConeHalfAngleRad: 0.0, Hp: 0.0, Unlock: "world",
                Kind: "current", Source: "packages/sim/src/flow.ts:211; packages/sim/src/flow.ts:218; packages/sim/src/flow.ts:232", Provisional: false),
        };

        public static Recipe[] Recipes() => new[]
        {
            new Recipe("steel-plates", "Steel plates", new[] { new ItemStack(ItemId.IronOre, 2) }, new[] { new ItemStack(ItemId.Steel, 1) }, 3.0, "Foundry",
                OutputKey: "", Kind: "current", Source: "packages/sim/src/flow.ts:183", Provisional: false),
            new Recipe("refined-copper", "Refined copper", new[] { new ItemStack(ItemId.CopperOre, 2) }, new[] { new ItemStack(ItemId.Copper, 1) }, 3.0, "Foundry",
                OutputKey: "", Kind: "current", Source: "packages/sim/src/flow.ts:183", Provisional: false),
            new Recipe("refined-fuel", "Fuel", new[] { new ItemStack(ItemId.Crude, 1) }, new[] { new ItemStack(ItemId.Fuel, 4) }, 3.0, "Refinery",
                OutputKey: "", Kind: "current", Source: "packages/sim/src/recipes.ts:9", Provisional: false),
            new Recipe("polymer", "Polymer", new[] { new ItemStack(ItemId.Crude, 2) }, new[] { new ItemStack(ItemId.Polymer, 1) }, 3.0, "Refinery",
                OutputKey: "", Kind: "current", Source: "packages/sim/src/recipes.ts:9", Provisional: false),
            new Recipe("wire", "Wire", new[] { new ItemStack(ItemId.Copper, 1) }, new[] { new ItemStack(ItemId.Wire, 2) }, 1.0, "Assembler",
                OutputKey: "", Kind: "current", Source: "packages/sim/src/recipes.ts:9", Provisional: false),
            new Recipe("frame", "Frame", new[] { new ItemStack(ItemId.Steel, 2) }, new[] { new ItemStack(ItemId.Frame, 1) }, 2.0, "Assembler",
                OutputKey: "", Kind: "current", Source: "packages/sim/src/recipes.ts:9", Provisional: false),
            new Recipe("board", "Board", new[] { new ItemStack(ItemId.Steel, 1), new ItemStack(ItemId.Wire, 3) }, new[] { new ItemStack(ItemId.Board, 1) }, 4.0, "Assembler",
                OutputKey: "", Kind: "current", Source: "packages/sim/src/recipes.ts:9", Provisional: false),
            new Recipe("concrete", "Concrete", new[] { new ItemStack(ItemId.Stone, 2) }, new[] { new ItemStack(ItemId.Concrete, 1) }, 2.0, "Mixer",
                OutputKey: "", Kind: "current", Source: "packages/sim/src/recipes.ts:9", Provisional: false),
            new Recipe("bullet-batch", "Bullet batch", new[] { new ItemStack(ItemId.Steel, 2), new ItemStack(ItemId.Copper, 1) }, new[] { new ItemStack(ItemId.Magazine, 10) }, 6.0, "Assembler",
                OutputKey: "", Kind: "current", Source: "packages/sim/src/recipes.ts:9; packages/sim/src/constants.ts:18", Provisional: false),
            new Recipe("bullet-batch-mk2", "Bullet batch (Mk2)", new[] { new ItemStack(ItemId.Steel, 2), new ItemStack(ItemId.Copper, 1) }, new[] { new ItemStack(ItemId.Magazine, 10) }, 3.0, "Assembler Mk2",
                OutputKey: "", Kind: "current", Source: "packages/sim/src/constants.ts:17; packages/sim/src/constants.ts:18", Provisional: false),
            new Recipe("shell", "Shell", new[] { new ItemStack(ItemId.Steel, 2), new ItemStack(ItemId.Coal, 1) }, new[] { new ItemStack(ItemId.Shell, 1) }, 3.0, "Assembler",
                OutputKey: "", Kind: "current", Source: "packages/sim/src/recipes.ts:9", Provisional: false),
            new Recipe("overclock-module", "Overclock Module", new[] { new ItemStack(ItemId.AlienArtifact, 2), new ItemStack(ItemId.Wire, 4), new ItemStack(ItemId.Frame, 2), new ItemStack(ItemId.Board, 1) }, new[] { new ItemStack(ItemId.Overclock, 1) }, 20.0, "Alien workbench",
                OutputKey: "", Kind: "current", Source: "packages/sim/src/flow.ts:183", Provisional: false),
            new Recipe("hand-bullets", "Hand bullet batch", new[] { new ItemStack(ItemId.Steel, 2), new ItemStack(ItemId.Copper, 1) }, new[] { new ItemStack(ItemId.Magazine, 10) }, 20.0, "Home workshop",
                OutputKey: "", Kind: "provisional", Source: "packages/sim/src/flow.ts:158; packages/sim/src/recipes.ts:9", Provisional: true),
            new Recipe("rifle", "Rifle", new[] { new ItemStack(ItemId.Steel, 10), new ItemStack(ItemId.Copper, 4) }, NoStacks, 6.0, "Home workshop",
                OutputKey: "rifle", Kind: "current", Source: "packages/sim/src/equipment.ts:17", Provisional: false),
            new Recipe("alien-decode", "Alien workbench decode", NoStacks, new[] { new ItemStack(ItemId.AlienArtifact, 2) }, 15.0, "Alien workbench",
                OutputKey: "", Kind: "provisional", Source: "packages/sim/src/fabrication.ts:6", Provisional: true),
        };

        public static WeaponDef[] Weapons() => new[]
        {
            new WeaponDef("rifle", "Rifle", 18.0, 30.0, 10.0, 2.5,
                1, 0.0, 0.65, 0.0, 10, 1.5,
                "provisional", "packages/sim/src/weaponProfiles.ts:2; packages/sim/src/equipment.ts:17", true),
            new WeaponDef("double", "Two-barrel shotgun", 6.0, 12.0, 6.0, 1.25,
                5, 0.36, 0.45, 0.0, 0, 0.0,
                "provisional", "packages/sim/src/weaponProfiles.ts:2", true),
            new WeaponDef("arc", "Arc projector", 9.0, 9.0, 10.0, 2.5,
                1, 0.0, 0.6, 0.0, 0, 0.0,
                "provisional", "packages/sim/src/weaponProfiles.ts:2", true),
            new WeaponDef("plasma", "Plasma lance", 14.0, 14.0, 25.0, 1.0,
                1, 0.0, 0.45, 12.0, 0, 0.0,
                "provisional", "packages/sim/src/weaponProfiles.ts:2", true),
        };

        public static EnemyDef[] Enemies() => new[]
        {
            new EnemyDef("skitter", "Skitter", 20.0, 5.4, 5.0, 1.0, 0.3, 1.3,
                false, "common swarm type", "provisional", "packages/sim/src/gameplayCombat.ts:13; packages/sim/src/campaignThreat.ts:198", true),
            new EnemyDef("spitter", "Spitter", 50.0, 3.9, 10.0, 2.0, 0.6, 7.0,
                true, "ranged support", "provisional", "packages/sim/src/gameplayCombat.ts:13; packages/sim/src/campaignThreat.ts:198", true),
        };

        public static AmmoDef[] Ammunition() => new[]
        {
            new AmmoDef("bullet", "Bullets", ItemId.Magazine, 1, 200, 10, 50,
                "approved", "packages/sim/src/flow.ts:156; packages/sim/src/engineer.ts:35; packages/sim/src/constants.ts:18; packages/sim/src/flow.ts:953", false),
            new AmmoDef("shell", "Shells", ItemId.Shell, 1, 20, 1, 5,
                "current", "packages/sim/src/engineer.ts:35; packages/sim/src/flow.ts:207", false),
        };

        public static TurretDef[] Turrets() => new[]
        {
            new TurretDef("turret", "Gun turret", 9.0, 1.0, 10.0, 50, 1.25,
                20.0, 100.0, 6.283185307179586, 1.0, 0.08, ItemId.Magazine,
                "provisional", "packages/sim/src/turretTracking.ts:4; packages/sim/src/constants.ts:42; packages/sim/src/recipes.ts:37; packages/sim/src/campaignDefence.ts:13; packages/sim/src/turretTracking.ts:5; packages/sim/src/progression.ts:50", true),
            new TurretDef("cannon", "Cannon", 12.0, 0.5, 50.0, 20, 1.0,
                0.0, 140.0, 6.283185307179586, 1.0, 0.08, ItemId.Shell,
                "provisional", "packages/sim/src/progression.ts:17; packages/sim/src/campaignDefence.ts:75; packages/sim/src/turretTracking.ts:5", true),
        };

        public static PowerTuning Power() => new PowerTuning(
            300.0, 4.0, 50.0,
            600.0, 600.0, 100.0, 20.0,
            8.0, 12.0, 8.0,
            20.0, 5.0, 4.0, 12.0, 6.0,
            40.0, 12.0, 0.5235987755982988,
            "proportional", "current", "packages/sim/src/recipes.ts:23; packages/sim/src/recipes.ts:22; packages/sim/src/flow.ts:237; packages/sim/src/progression.ts:17; packages/sim/src/campaignPower.ts:10; packages/sim/src/recipes.ts:38; packages/sim/src/recipes.ts:43; packages/sim/src/flow.ts:68; packages/sim/src/campaignPower.ts:20; packages/sim/src/recipes.ts:37; packages/sim/src/constants.ts:65", false);

        public static TimeTuning Time() => new TimeTuning(
            20, 0.05, 1200.0, 900.0, "culdesac-v1",
            900.0, 15.0, 900.0, 1080.0,
            "current", "packages/sim/src/flow.ts:61; packages/sim/src/rules.ts:5; packages/sim/src/fabrication.ts:6; packages/sim/src/progression.ts:17", false);

        public static RaidTuning Raids() => new RaidTuning(
            1500.0, 300.0, 1080.0, 240.0,
            300.0, 780.0, 150.0, 300.0,
            60, 48, 240,
            300.0, 120.0, 6, 4,
            60, 40, 20, 4,
            4.0, 5.0, 8.0, 3.0,
            2.0, 300.0,
            12.0, 8.0, 20.0, 6.0,
            15, 10,
            8.0, 2.0, 8.0, 18.0,
            6.0, 0.65, 32,
            "current", "packages/sim/src/campaignThreat.ts:27; packages/sim/src/campaignThreat.ts:23; packages/sim/src/campaignThreat.ts:83; packages/sim/src/campaignThreat.ts:198; packages/sim/src/campaignThreat.ts:49; packages/sim/src/gameplayCombat.ts:13; packages/sim/src/progression.ts:17", false);

        public static OpeningEncounterTuning Opening() => new OpeningEncounterTuning(
            25.0, 5, 300.0, 300.0, 660.0, 60.0, 45.0,
            4, 3,
            "provisional", "packages/sim/src/openingEncounter.ts:27; packages/sim/src/openingEncounter.ts:55", true);

        public static StartingStake Stake() => new StartingStake(
            new[] { new ItemStack(ItemId.Steel, 20), new ItemStack(ItemId.Copper, 5) }, "exploration-v2", "approved", "packages/sim/src/rules.ts:10; packages/sim/src/rules.ts:5", false);

        public static EngineerTuning Engineer() => new EngineerTuning(
            6.0, 1.6, 0.25, 0.16666666666666666,
            3.0, 0.25, 1.0, 0.25, 0.25,
            100.0, 5.0, 5.0, 10.0,
            40, 200, 8.0,
            0.5, 20.0, 10, 2, 1,
            0.28, 3.0, 5.0,
            "current", "packages/sim/src/engineer.ts:16; packages/sim/src/engineer.ts:17; packages/sim/src/engineer.ts:18; packages/sim/src/engineer.ts:19; packages/sim/src/engineer.ts:25; packages/sim/src/engineer.ts:29; packages/sim/src/flow.ts:210; packages/sim/src/flow.ts:158; packages/sim/src/walk.ts:61", false);

        public static WorldTuning World() => new WorldTuning(
            32, 32, 24, 4, 6, 3,
            300, 250, 350,
            8, 9,
            "current", "packages/sim/src/tiles.ts:17; packages/sim/src/tiles.ts:18; packages/sim/src/tiles.ts:38; packages/sim/src/tiles.ts:58; packages/sim/src/tiles.ts:110; packages/sim/src/tiles.ts:114", false);

        /// <summary>The whole exported catalogue as one <see cref="GameData"/>.</summary>
        public static GameData Build() => new GameData(
            Items(), Machines(), Recipes(), Engineer(), World(),
            Weapons(), Enemies(), Ammunition(), Turrets(),
            Power(), Time(), Raids(), Opening(), Stake());
    }
}
