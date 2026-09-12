using UnityEngine;
using Relight.Sim;

namespace Relight.Data
{
    /// <summary>CONTENT_CATALOGUE.md §8.1 (the raid director's ACTIVE_RAIDS schedule) and the §8 threat constants.</summary>
    [CreateAssetMenu(menuName = "Relight/Tuning/Raid Director", fileName = "Tuning - Raids")]
    public sealed class RaidDirectorTuningAsset : DataDefinition
    {
        [Header("Schedule, seconds")]
        [SerializeField] private double firstMinS = 1500;
        [SerializeField] private double firstRangeS = 300;
        [SerializeField] private double intervalMinS = 1080;
        [SerializeField] private double intervalRangeS = 240;
        [SerializeField] private double warningS = 300;
        [SerializeField] private double graceS = 780;
        [SerializeField] private double windowS = 150;
        [SerializeField] private double recoveryS = 300;

        [Header("Budgets")]
        [SerializeField] private int total = 60;
        [SerializeField] private int activeRaidBudget = 48;
        [SerializeField] private int livingBudget = 240;

        [Header("Minor raids")]
        [SerializeField] private double minorMinS = 300;
        [SerializeField] private double minorRangeS = 120;
        [SerializeField] private int minorCountBase = 6;
        [SerializeField] private int minorCountRange = 4;
        [SerializeField] private double minorAfterMajorS = 300;

        [Header("Major assault")]
        [SerializeField] private int majorCount = 60;
        [SerializeField] private int majorSkitters = 40;
        [SerializeField] private int majorSpitters = 20;
        [SerializeField] private int majorSectors = 4;
        [SerializeField] private double spawnEveryS = 4;

        [Header("Damage and movement")]
        [SerializeField] private double contactDps = 5;
        [SerializeField] private double structureDps = 8;
        [SerializeField] private double breakerStructureMul = 3;
        [SerializeField] private double speedTilesPerS = 2;

        [Header("Guards and perception, tiles")]
        [SerializeField] private double guardLeashTiles = 12;
        [SerializeField] private double guardNoticeTiles = 8;
        [SerializeField] private double chaseEscapeTiles = 20;
        [SerializeField] private double patrolRadiusTiles = 6;
        [SerializeField] private double noticeTiles = 8;
        [SerializeField] private double escapeTiles = 18;
        [SerializeField] private double alertRadiusTiles = 6;
        [SerializeField] private double lightHesitateS = 0.65;

        [Header("Projectiles and radio")]
        [SerializeField] private double projectileSpeedTilesPerS = 8;
        [SerializeField] private double projectileLifeS = 2;
        [SerializeField] private int radioUpgradeSteel = 15;
        [SerializeField] private int radioUpgradeCopper = 10;
        [SerializeField] private int assaultHistory = 32;

        public RaidTuning ToRecord() => new RaidTuning(firstMinS, firstRangeS, intervalMinS, intervalRangeS, warningS,
            graceS, windowS, recoveryS, total, activeRaidBudget, livingBudget, minorMinS, minorRangeS, minorCountBase,
            minorCountRange, majorCount, majorSkitters, majorSpitters, majorSectors, spawnEveryS, contactDps,
            structureDps, breakerStructureMul, speedTilesPerS, minorAfterMajorS, guardLeashTiles, guardNoticeTiles,
            chaseEscapeTiles, patrolRadiusTiles, radioUpgradeSteel, radioUpgradeCopper, projectileSpeedTilesPerS,
            projectileLifeS, noticeTiles, escapeTiles, alertRadiusTiles, lightHesitateS, assaultHistory,
            KindText, Source, Provisional);

        public void Fill(RaidTuning r)
        {
            SetCommon("raids", "Raid director", r.Kind, r.Source, r.Provisional);
            firstMinS = r.FirstMinS; firstRangeS = r.FirstRangeS; intervalMinS = r.IntervalMinS;
            intervalRangeS = r.IntervalRangeS; warningS = r.WarningS; graceS = r.GraceS; windowS = r.WindowS;
            recoveryS = r.RecoveryS; total = r.Total; activeRaidBudget = r.ActiveRaidBudget; livingBudget = r.LivingBudget;
            minorMinS = r.MinorMinS; minorRangeS = r.MinorRangeS; minorCountBase = r.MinorCountBase;
            minorCountRange = r.MinorCountRange; majorCount = r.MajorCount; majorSkitters = r.MajorSkitters;
            majorSpitters = r.MajorSpitters; majorSectors = r.MajorSectors; spawnEveryS = r.SpawnEveryS;
            contactDps = r.ContactDps; structureDps = r.StructureDps; breakerStructureMul = r.BreakerStructureMul;
            speedTilesPerS = r.SpeedTilesPerS; minorAfterMajorS = r.MinorAfterMajorS; guardLeashTiles = r.GuardLeashTiles;
            guardNoticeTiles = r.GuardNoticeTiles; chaseEscapeTiles = r.ChaseEscapeTiles;
            patrolRadiusTiles = r.PatrolRadiusTiles; radioUpgradeSteel = r.RadioUpgradeSteel;
            radioUpgradeCopper = r.RadioUpgradeCopper; projectileSpeedTilesPerS = r.ProjectileSpeedTilesPerS;
            projectileLifeS = r.ProjectileLifeS; noticeTiles = r.NoticeTiles; escapeTiles = r.EscapeTiles;
            alertRadiusTiles = r.AlertRadiusTiles; lightHesitateS = r.LightHesitateS; assaultHistory = r.AssaultHistory;
        }

        public override string Problem()
        {
            var baseProblem = base.Problem();
            if (baseProblem != null) return baseProblem;
            if (total <= 0) return "assault roster must be positive";
            if (majorSkitters + majorSpitters != majorCount) return "the major composition does not add up to the roster";
            if (activeRaidBudget > livingBudget) return "the active budget exceeds the living budget";
            if (warningS <= 0 || graceS <= 0) return "warning and grace must be positive";
            if (escapeTiles <= noticeTiles) return "escape distance must exceed the notice distance";
            return null;
        }
    }
}
