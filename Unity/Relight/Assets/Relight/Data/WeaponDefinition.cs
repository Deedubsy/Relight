using UnityEngine;
using Relight.Sim;

namespace Relight.Data
{
    /// <summary>One row of CONTENT_CATALOGUE.md §5 (weapons). Authoring asset for <see cref="WeaponDef"/>.</summary>
    [CreateAssetMenu(menuName = "Relight/Weapon Definition", fileName = "Weapon - New")]
    public sealed class WeaponDefinition : DataDefinition
    {
        [SerializeField] private double effectiveTiles;
        [SerializeField] private double maxTiles;
        [SerializeField] private double damage;
        [SerializeField] private double ratePerS;
        [SerializeField] private int pellets = 1;
        [SerializeField] private double spreadRad;
        [SerializeField] private double hitRadiusTiles;
        [Tooltip("0 means hitscan; a positive value is a travelling projectile in tiles per second.")]
        [SerializeField] private double projectileSpeed;
        [Tooltip("0 means the catalogue states no magazine for this weapon.")]
        [SerializeField] private int capacity;
        [SerializeField] private double reloadSeconds;

        public WeaponDef ToRecord() => new WeaponDef(Key, DisplayName, effectiveTiles, maxTiles, damage, ratePerS,
            pellets, spreadRad, hitRadiusTiles, projectileSpeed, capacity, reloadSeconds, KindText, Source, Provisional);

        public void Fill(WeaponDef r)
        {
            SetCommon(r.Key, r.DisplayName, r.Kind, r.Source, r.Provisional);
            effectiveTiles = r.EffectiveTiles;
            maxTiles = r.MaxTiles;
            damage = r.Damage;
            ratePerS = r.RatePerS;
            pellets = r.Pellets;
            spreadRad = r.SpreadRad;
            hitRadiusTiles = r.HitRadiusTiles;
            projectileSpeed = r.ProjectileSpeed;
            capacity = r.Capacity;
            reloadSeconds = r.ReloadSeconds;
        }

        public override string Problem()
        {
            var baseProblem = base.Problem();
            if (baseProblem != null) return baseProblem;
            if (damage <= 0) return "damage must be positive";
            if (ratePerS <= 0) return "rate of fire must be positive";
            if (pellets <= 0) return "pellet count must be positive";
            if (maxTiles < effectiveTiles) return "maximum range is shorter than the effective range";
            return null;
        }
    }
}
