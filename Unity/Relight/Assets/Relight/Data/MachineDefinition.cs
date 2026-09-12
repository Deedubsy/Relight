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

        public int Size => size;
        public double PowerKw => powerKw;
        public bool HasInventory => hasInventory;
        public int InventorySlots => inventorySlots;
        public double Hp => hp;

        public MachineSpec ToRecord() => new MachineSpec(Key, DisplayName, size, ItemAmount.ToStacks(cost), hasInventory,
            powerKw, inventorySlots, fuelCap, ammoCap, ratePerS, reachTiles, lightRadiusTiles,
            coneRangeTiles, coneHalfAngleRad, hp, unlock, KindText, Source, Provisional);

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
        }

        public override string Problem()
        {
            var baseProblem = base.Problem();
            if (baseProblem != null) return baseProblem;
            if (size <= 0) return "footprint size must be positive";
            if (hasInventory && inventorySlots <= 0) return "has an inventory but no slots";
            if (!hasInventory && inventorySlots > 0) return "has slots but no inventory";
            foreach (var a in cost) if (a.count <= 0) return $"cost entry {a.item} has a non-positive count";
            return null;
        }
    }
}
