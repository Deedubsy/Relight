using UnityEngine;
using Relight.Sim;

namespace Relight.Data
{
    /// <summary>One row of CONTENT_CATALOGUE.md §6.2 (turrets, Unity column). Authoring asset for <see cref="TurretDef"/>.</summary>
    [CreateAssetMenu(menuName = "Relight/Turret Definition", fileName = "Turret - New")]
    public sealed class TurretDefinition : DataDefinition
    {
        [SerializeField] private double rangeTiles;
        [SerializeField] private double roundsPerS;
        [SerializeField] private double damagePerRound;
        [Tooltip("Rounds the hopper holds before the upgrade multiplier.")]
        [SerializeField] private int hopper;
        [SerializeField] private double hopperUpgradeMul = 1;
        [SerializeField] private double powerKw;
        [SerializeField] private double hp;
        [SerializeField] private double turnSpeedRadPerS;
        [Tooltip("Where the shot leaves the barrel, in tiles from the centre.")]
        [SerializeField] private double muzzleTiles;
        [SerializeField] private double shotFlashS;
        [SerializeField] private ItemId ammo = ItemId.Magazine;
        [Tooltip("U-D-59: reach against an alien on an UNLIT tile. 0 = take DarkWorld's value for this turret.")]
        [SerializeField] private double darkSightTiles;

        public ItemId Ammo => ammo;

        public TurretDef ToRecord() => new TurretDef(Key, DisplayName, rangeTiles, roundsPerS, damagePerRound, hopper,
            hopperUpgradeMul, powerKw, hp, turnSpeedRadPerS, muzzleTiles, shotFlashS, ammo, KindText, Source, Provisional,
            DarkSightTiles: darkSightTiles);

        public void Fill(TurretDef r)
        {
            SetCommon(r.Key, r.DisplayName, r.Kind, r.Source, r.Provisional);
            rangeTiles = r.RangeTiles;
            roundsPerS = r.RoundsPerS;
            damagePerRound = r.DamagePerRound;
            hopper = r.Hopper;
            hopperUpgradeMul = r.HopperUpgradeMul;
            powerKw = r.PowerKw;
            hp = r.Hp;
            turnSpeedRadPerS = r.TurnSpeedRadPerS;
            muzzleTiles = r.MuzzleTiles;
            shotFlashS = r.ShotFlashS;
            ammo = r.Ammo;
            darkSightTiles = r.DarkSightTiles;
        }

        public override string Problem()
        {
            var baseProblem = base.Problem();
            if (baseProblem != null) return baseProblem;
            if (rangeTiles <= 0) return "range must be positive";
            if (roundsPerS <= 0) return "rate of fire must be positive";
            if (damagePerRound <= 0) return "damage per round must be positive";
            if (hopper <= 0) return "hopper capacity must be positive";
            if (hopperUpgradeMul < 1) return "hopper upgrade multiplier must be at least 1";
            if (hp <= 0) return "hp must be positive";
            if (darkSightTiles < 0) return "dark sight cannot be negative";
            if (darkSightTiles > rangeTiles) return "dark sight cannot exceed the range";
            return null;
        }
    }
}
