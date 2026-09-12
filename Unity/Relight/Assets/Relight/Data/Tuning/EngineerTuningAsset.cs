using UnityEngine;
using Relight.Sim;

namespace Relight.Data
{
    /// <summary>CONTENT_CATALOGUE.md §14 (the engineer). Authoring asset for <see cref="EngineerTuning"/>.</summary>
    [CreateAssetMenu(menuName = "Relight/Tuning/Engineer", fileName = "Tuning - Engineer")]
    public sealed class EngineerTuningAsset : DataDefinition
    {
        [Header("Movement")]
        [SerializeField] private double walkTilesPerS = 6;
        [SerializeField] private double sprintMul = 1.6;
        [SerializeField] private double sprintDrainPerS = 0.25;
        [SerializeField] private double staminaRegenPerS = 0.16666666666666666;
        [SerializeField] private double truckMul = 3;
        [SerializeField] private double bodyRadiusTiles = 0.28;

        [Header("Dash")]
        [SerializeField] private double dashTiles = 3;
        [SerializeField] private double dashSeconds = 0.25;
        [SerializeField] private double dashCooldownS = 1;
        [SerializeField] private double iFramesS = 0.25;
        [SerializeField] private double dashCost = 0.25;

        [Header("Survival")]
        [SerializeField] private double maxHp = 100;
        [SerializeField] private double regenPerS = 5;
        [SerializeField] private double regenDelayS = 5;
        [SerializeField] private double respawnS = 10;
        [SerializeField] private double retaliateHpPerS = 5;

        [Header("Carrying and reach")]
        [SerializeField] private int invStacks = 40;
        [SerializeField] private int truckStacks = 200;
        [SerializeField] private double reachTiles = 8;

        [Header("Hand work")]
        [SerializeField] private double handMinePerS = 0.5;
        [SerializeField] private double handBulletSeconds = 20;
        [SerializeField] private int handBulletsPerCraft = 10;
        [SerializeField] private int handBulletSteel = 2;
        [SerializeField] private int handBulletCopper = 1;

        public EngineerTuning ToRecord() => new EngineerTuning(walkTilesPerS, sprintMul, sprintDrainPerS,
            staminaRegenPerS, dashTiles, dashSeconds, dashCooldownS, iFramesS, dashCost, maxHp, regenPerS, regenDelayS,
            respawnS, invStacks, truckStacks, reachTiles, handMinePerS, handBulletSeconds, handBulletsPerCraft,
            handBulletSteel, handBulletCopper, bodyRadiusTiles, truckMul, retaliateHpPerS, KindText, Source, Provisional);

        public void Fill(EngineerTuning r)
        {
            SetCommon("engineer", "Engineer", r.Kind, r.Source, r.Provisional);
            walkTilesPerS = r.WalkTilesPerS; sprintMul = r.SprintMul; sprintDrainPerS = r.SprintDrainPerS;
            staminaRegenPerS = r.StaminaRegenPerS; dashTiles = r.DashTiles; dashSeconds = r.DashSeconds;
            dashCooldownS = r.DashCooldownS; iFramesS = r.IFramesS; dashCost = r.DashCost; maxHp = r.MaxHp;
            regenPerS = r.RegenPerS; regenDelayS = r.RegenDelayS; respawnS = r.RespawnS; invStacks = r.InvStacks;
            truckStacks = r.TruckStacks; reachTiles = r.ReachTiles; handMinePerS = r.HandMinePerS;
            handBulletSeconds = r.HandBulletSeconds; handBulletsPerCraft = r.HandBulletsPerCraft;
            handBulletSteel = r.HandBulletSteel; handBulletCopper = r.HandBulletCopper;
            bodyRadiusTiles = r.BodyRadiusTiles; truckMul = r.TruckMul; retaliateHpPerS = r.RetaliateHpPerS;
        }

        public override string Problem()
        {
            var baseProblem = base.Problem();
            if (baseProblem != null) return baseProblem;
            if (walkTilesPerS <= 0) return "walk speed must be positive";
            if (sprintMul < 1) return "sprint multiplier must be at least 1";
            if (maxHp <= 0) return "max hp must be positive";
            if (invStacks <= 0 || truckStacks <= 0) return "stack counts must be positive";
            if (reachTiles <= 0) return "reach must be positive";
            if (iFramesS > dashSeconds) return "invulnerability outlasts the dash";
            return null;
        }
    }
}
