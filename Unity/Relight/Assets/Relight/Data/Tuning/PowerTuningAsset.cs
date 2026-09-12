using UnityEngine;
using Relight.Sim;

namespace Relight.Data
{
    /// <summary>CONTENT_CATALOGUE.md §12 (power), minus the retired SUBSTATION_KW row (§17.2).</summary>
    [CreateAssetMenu(menuName = "Relight/Tuning/Power", fileName = "Tuning - Power")]
    public sealed class PowerTuningAsset : DataDefinition
    {
        [Header("Supply")]
        [SerializeField] private double generatorKw = 300;
        [SerializeField] private double coalMj = 4;
        [SerializeField] private double generatorFuelCap = 50;
        [SerializeField] private double plantKw = 600;
        [SerializeField] private double turbineHallKw = 600;
        [SerializeField] private double coreKw = 100;
        [SerializeField] private double radioKw = 20;

        [Header("Distribution reach, tiles")]
        [SerializeField] private double poleReachTiles = 8;
        [SerializeField] private double bigPoleReachTiles = 12;
        [SerializeField] private double substationReachTiles = 8;

        [Header("Draw")]
        [SerializeField] private double turretKw = 20;
        [SerializeField] private double lampKw = 5;
        [SerializeField] private double lampRadiusTiles = 4;
        [SerializeField] private double arcLampKw = 12;
        [SerializeField] private double arcLampRadiusTiles = 6;
        [SerializeField] private double floodlightKw = 40;
        [SerializeField] private double floodlightRangeTiles = 12;
        [SerializeField] private double floodlightHalfAngleRad = 0.5235987755982988;

        [Tooltip("How a shortfall is shared out. 'proportional' is the reference rule.")]
        [SerializeField] private string brownoutRule = "proportional";

        public PowerTuning ToRecord() => new PowerTuning(generatorKw, coalMj, generatorFuelCap, plantKw, turbineHallKw,
            coreKw, radioKw, poleReachTiles, bigPoleReachTiles, substationReachTiles, turretKw, lampKw, lampRadiusTiles,
            arcLampKw, arcLampRadiusTiles, floodlightKw, floodlightRangeTiles, floodlightHalfAngleRad, brownoutRule,
            KindText, Source, Provisional);

        public void Fill(PowerTuning r)
        {
            SetCommon("power", "Power", r.Kind, r.Source, r.Provisional);
            generatorKw = r.GeneratorKw; coalMj = r.CoalMj; generatorFuelCap = r.GeneratorFuelCap;
            plantKw = r.PlantKw; turbineHallKw = r.TurbineHallKw; coreKw = r.CoreKw; radioKw = r.RadioKw;
            poleReachTiles = r.PoleReachTiles; bigPoleReachTiles = r.BigPoleReachTiles;
            substationReachTiles = r.SubstationReachTiles; turretKw = r.TurretKw; lampKw = r.LampKw;
            lampRadiusTiles = r.LampRadiusTiles; arcLampKw = r.ArcLampKw; arcLampRadiusTiles = r.ArcLampRadiusTiles;
            floodlightKw = r.FloodlightKw; floodlightRangeTiles = r.FloodlightRangeTiles;
            floodlightHalfAngleRad = r.FloodlightHalfAngleRad; brownoutRule = r.BrownoutRule;
        }

        public override string Problem()
        {
            var baseProblem = base.Problem();
            if (baseProblem != null) return baseProblem;
            if (generatorKw <= 0) return "generator supply must be positive";
            if (coalMj <= 0) return "coal energy must be positive";
            if (poleReachTiles <= 0 || bigPoleReachTiles <= 0) return "pole reach must be positive";
            if (string.IsNullOrEmpty(brownoutRule)) return "brownout rule is empty";
            return null;
        }
    }
}
