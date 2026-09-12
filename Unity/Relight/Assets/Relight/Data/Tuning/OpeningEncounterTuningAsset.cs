using UnityEngine;
using Relight.Sim;

namespace Relight.Data
{
    /// <summary>CONTENT_CATALOGUE.md §8.4 (the Home opening encounter).</summary>
    [CreateAssetMenu(menuName = "Relight/Tuning/Opening Encounter", fileName = "Tuning - Opening")]
    public sealed class OpeningEncounterTuningAsset : DataDefinition
    {
        [Tooltip("Seconds of warning before the introductory attack.")]
        [SerializeField] private double warningS = 25;
        [Tooltip("How many attackers the opening sends.")]
        [SerializeField] private int count = 5;
        [SerializeField] private double maxDurationS = 300;
        [SerializeField] private double recoveryS = 300;
        [Tooltip("How long the prepared turret guards before the opening hands control over.")]
        [SerializeField] private double guardS = 660;
        [SerializeField] private double ackS = 60;
        [SerializeField] private double supplyAckS = 45;
        [Tooltip("How many links of the supply chain the resupply check follows.")]
        [SerializeField] private int supplyChainDepth = 4;
        [Tooltip("Turrets the opening asks the player to stand up.")]
        [SerializeField] private int turretObjective = 3;

        public OpeningEncounterTuning ToRecord() => new OpeningEncounterTuning(warningS, count, maxDurationS, recoveryS,
            guardS, ackS, supplyAckS, supplyChainDepth, turretObjective, KindText, Source, Provisional);

        public void Fill(OpeningEncounterTuning r)
        {
            SetCommon("opening", "Opening encounter", r.Kind, r.Source, r.Provisional);
            warningS = r.WarningS; count = r.Count; maxDurationS = r.MaxDurationS; recoveryS = r.RecoveryS;
            guardS = r.GuardS; ackS = r.AckS; supplyAckS = r.SupplyAckS; supplyChainDepth = r.SupplyChainDepth;
            turretObjective = r.TurretObjective;
        }

        public override string Problem()
        {
            var baseProblem = base.Problem();
            if (baseProblem != null) return baseProblem;
            if (count <= 0) return "the opening must send at least one attacker";
            if (warningS <= 0) return "warning must be positive";
            if (maxDurationS <= warningS) return "the encounter is shorter than its warning";
            if (supplyChainDepth <= 0) return "supply chain depth must be positive";
            if (turretObjective <= 0) return "turret objective must be positive";
            return null;
        }
    }
}
