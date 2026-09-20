using UnityEngine;
using Relight.Sim;

namespace Relight.Data
{
    /// <summary>
    /// C-05: the defence and repair constants (reference campaignDefence.ts:13 <c>DEFENCE</c>, plus the Cannon's
    /// 140 HP, which <c>defenceMax</c> on the same file's line 75 keeps as a literal).
    /// </summary>
    [CreateAssetMenu(menuName = "Relight/Tuning/Defence", fileName = "Tuning - Defence")]
    public sealed class DefenceTuningAsset : DataDefinition
    {
        [Header("Structure hit points")]
        [SerializeField] private int barricadeHp = 240;
        [SerializeField] private int wallHp = 120;
        [SerializeField] private int turretHp = 100;
        [SerializeField] private int cannonHp = 140;
        [Tooltip("The Home core's hit points. At zero the core is disabled and needs a recommission kit.")]
        [SerializeField] private int coreHp = 300;

        [Header("Patch repair")]
        [Tooltip("Hit points one patch repair restores, capped at the target's maximum.")]
        [SerializeField] private int repairHp = 40;
        [SerializeField] private double repairSeconds = 4;
        [SerializeField] private int repairSteel = 2;
        [SerializeField] private int repairCopper = 1;

        [Header("Recommission kit (a core at zero)")]
        [SerializeField] private int coreSteel = 10;
        [SerializeField] private int coreCopper = 5;
        [SerializeField] private double coreRepairSeconds = 12;

        [Header("Turret ammunition alert (E-17, U-P-14)")]
        [Tooltip("A turret is low on ammunition under this share of its hopper. A quarter is the owner's starting value.")]
        [SerializeField] private double lowAmmoFraction = 0.25;

        public DefenceTuning ToRecord() => new DefenceTuning(barricadeHp, wallHp, turretHp, cannonHp, coreHp,
            repairHp, repairSeconds, repairSteel, repairCopper, coreSteel, coreCopper, coreRepairSeconds,
            KindText, Source, Provisional, lowAmmoFraction);

        public void Fill(DefenceTuning d)
        {
            SetCommon("defence", "Defence", d.Kind, d.Source, d.Provisional);
            barricadeHp = d.BarricadeHp; wallHp = d.WallHp; turretHp = d.TurretHp; cannonHp = d.CannonHp;
            coreHp = d.CoreHp; repairHp = d.RepairHp; repairSeconds = d.RepairSeconds;
            repairSteel = d.RepairSteel; repairCopper = d.RepairCopper;
            coreSteel = d.CoreSteel; coreCopper = d.CoreCopper; coreRepairSeconds = d.CoreRepairSeconds;
            lowAmmoFraction = d.LowAmmoFraction;
        }

        public override string Problem()
        {
            var baseProblem = base.Problem();
            if (baseProblem != null) return baseProblem;
            if (coreHp <= 0) return "the core needs hit points";
            if (repairHp <= 0 || repairSeconds <= 0) return "a repair must restore something and take time";
            if (repairSteel < 0 || repairCopper < 0 || coreSteel < 0 || coreCopper < 0) return "a repair cannot cost less than nothing";
            if (lowAmmoFraction <= 0 || lowAmmoFraction >= 1) return "the low-ammunition share must be between nothing and a full hopper";
            if (coreRepairSeconds < repairSeconds) return "a recommission cannot be quicker than a patch";
            if (coreSteel < repairSteel || coreCopper < repairCopper) return "a recommission cannot be cheaper than a patch";
            return null;
        }
    }
}
