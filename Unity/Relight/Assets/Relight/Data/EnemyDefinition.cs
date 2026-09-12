using UnityEngine;
using Relight.Sim;

namespace Relight.Data
{
    /// <summary>One row of CONTENT_CATALOGUE.md §7 (the enemy roster). Authoring asset for <see cref="EnemyDef"/>.</summary>
    [CreateAssetMenu(menuName = "Relight/Enemy Definition", fileName = "Enemy - New")]
    public sealed class EnemyDefinition : DataDefinition
    {
        [SerializeField] private double hp;
        [SerializeField] private double speedTilesPerS;
        [SerializeField] private double damage;
        [Tooltip("Seconds between attacks.")]
        [SerializeField] private double intervalS;
        [Tooltip("Telegraph before an attack lands.")]
        [SerializeField] private double windupS;
        [SerializeField] private double rangeTiles;
        [SerializeField] private bool ranged;
        [Tooltip("Its part in the roster, as §7.4 words it.")]
        [SerializeField] private string role = string.Empty;

        public EnemyDef ToRecord() => new EnemyDef(Key, DisplayName, hp, speedTilesPerS, damage, intervalS, windupS,
            rangeTiles, ranged, role, KindText, Source, Provisional);

        public void Fill(EnemyDef r)
        {
            SetCommon(r.Key, r.DisplayName, r.Kind, r.Source, r.Provisional);
            hp = r.Hp;
            speedTilesPerS = r.SpeedTilesPerS;
            damage = r.Damage;
            intervalS = r.IntervalS;
            windupS = r.WindupS;
            rangeTiles = r.RangeTiles;
            ranged = r.Ranged;
            role = r.Role;
        }

        public override string Problem()
        {
            var baseProblem = base.Problem();
            if (baseProblem != null) return baseProblem;
            if (hp <= 0) return "hp must be positive";
            if (speedTilesPerS <= 0) return "speed must be positive";
            if (intervalS <= 0) return "attack interval must be positive";
            if (rangeTiles <= 0) return "attack range must be positive";
            if (windupS < 0) return "windup cannot be negative";
            return null;
        }
    }
}
