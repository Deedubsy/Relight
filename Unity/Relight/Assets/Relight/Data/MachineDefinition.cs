using UnityEngine;
using Relight.Sim;

namespace Relight.Data
{
    /// <summary>One row of CONTENT_CATALOGUE.md §4 (machines). Authoring asset for <see cref="MachineSpec"/>.</summary>
    [CreateAssetMenu(menuName = "Relight/Machine Definition", fileName = "Machine - New")]
    public sealed class MachineDefinition : DataDefinition
    {
        [Tooltip("Square footprint in tiles; the splitter's 2x1 is handled by Footprints.Dimensions.")]
        [SerializeField] private int size = 1;

        [SerializeField] private ItemAmount[] cost = new ItemAmount[0];

        [Tooltip("Negative kW means a supply (the Generator).")]
        [SerializeField] private double powerKw;

        [SerializeField] private bool hasInventory;

        [Tooltip("Chest: item capacity. Processor: distinct input slots. Otherwise 1 for a fuel/ammo holder, else 0.")]
        [SerializeField] private int inventorySlots;

        [SerializeField] private double fuelCap;
        [SerializeField] private int ammoCap;
        [SerializeField] private double ratePerS;
        [SerializeField] private double reachTiles;
        [SerializeField] private double lightRadiusTiles;
        [SerializeField] private double coneRangeTiles;
        [SerializeField] private double coneHalfAngleRad;
        [SerializeField] private double hp;

        [Tooltip("What unlocks it, as the catalogue words it. Empty means available from the start.")]
        [SerializeField] private string unlock = string.Empty;

        [Header("Processing (B-07)")]
        [Tooltip("The recipe station this kind runs, matching Recipe.Station. Empty means it processes nothing.")]
        [SerializeField] private string recipeStation = string.Empty;

        [Tooltip("The recipe a newly placed machine runs. Empty means the player must choose one.")]
        [SerializeField] private string defaultRecipe = string.Empty;

        [Tooltip("Processing-speed multiplier: the Assembler Mk2's 2. Leave at 1 elsewhere.")]
        [SerializeField] private double speedMul = 1;

        [Tooltip("Input buffer depth, in crafts' worth of each input (reference ASM_INPUT_MULT = 4).")]
        [SerializeField] private double inputBufferMul = 4;

        [Tooltip("Finished-output buffer, in batches (reference ASM_OUTPUT_CAP = 5).")]
        [SerializeField] private double outputBufferCap = 5;

        public int Size => size;
        public double PowerKw => powerKw;
        public bool HasInventory => hasInventory;
        public int InventorySlots => inventorySlots;
        public double Hp => hp;
        public string RecipeStation => recipeStation;

        public MachineSpec ToRecord() => new MachineSpec(Key, DisplayName, size, ItemAmount.ToStacks(cost), hasInventory,
            powerKw, inventorySlots, fuelCap, ammoCap, ratePerS, reachTiles, lightRadiusTiles,
            coneRangeTiles, coneHalfAngleRad, hp, unlock, recipeStation, defaultRecipe, speedMul, inputBufferMul,
            outputBufferCap, KindText, Source, Provisional);

        public void Fill(MachineSpec r)
        {
            SetCommon(r.Key, r.DisplayName, r.Kind, r.Source, r.Provisional);
            size = r.Size;
            cost = ItemAmount.From(r.Cost);
            powerKw = r.PowerKw;
            hasInventory = r.HasInventory;
            inventorySlots = r.InventorySlots;
            fuelCap = r.FuelCap;
            ammoCap = r.AmmoCap;
            ratePerS = r.RatePerS;
            reachTiles = r.ReachTiles;
            lightRadiusTiles = r.LightRadiusTiles;
            coneRangeTiles = r.ConeRangeTiles;
            coneHalfAngleRad = r.ConeHalfAngleRad;
            hp = r.Hp;
            unlock = r.Unlock;
            recipeStation = r.RecipeStation;
            defaultRecipe = r.DefaultRecipe;
            speedMul = r.SpeedMul;
            inputBufferMul = r.InputBufferMul;
            outputBufferCap = r.OutputBufferCap;
        }

        public override string Problem()
        {
            var baseProblem = base.Problem();
            if (baseProblem != null) return baseProblem;
            if (size <= 0) return "footprint size must be positive";
            if (hasInventory && inventorySlots <= 0) return "has an inventory but no slots";
            if (!hasInventory && inventorySlots > 0) return "has slots but no inventory";
            foreach (var a in cost) if (a.count <= 0) return $"cost entry {a.item} has a non-positive count";
            if (speedMul <= 0) return "processing speed multiplier must be positive";
            if (recipeStation.Length == 0 && defaultRecipe.Length > 0) return "has a default recipe but runs no station";
            if (recipeStation.Length > 0 && (inputBufferMul <= 0 || outputBufferCap <= 0))
                return "a processor needs a positive input and output buffer";
            return null;
        }
    }
}
