using System;
using System.Collections.Generic;

namespace Relight.Sim
{
    /// <summary>
    /// GP-W5: the port-owned combat rows the exported catalogue cannot carry.
    ///
    /// <c>Sim/Data/Generated/CatalogueData.g.cs</c> is generated from the TypeScript reference by
    /// <c>Unity/Relight/Tools/export/exportCatalogue.ts</c>, so it can only ever hold what the reference holds.
    /// Two things this port needs are not in the reference at all:
    ///
    /// 1. THE BREAKER. CONTENT_CATALOGUE.md §7.4 is the approved five-type roster (owner decisions 2026-09-11,
    ///    Q13/Q14/Q15); the reference only ever births skitters and spitters, so the exporter has only those two
    ///    rows to export. The Breaker is brought forward here from the approved row, not invented: HP 220 (Q14),
    ///    15 player damage, 2 s interval, 45% of the engineer's 6 tiles/s. Its heavier structure damage is NOT a
    ///    second mechanic — it falls out of the roster-wide rule in <see cref="Enemies.StructureDamage"/>, which
    ///    §7.4 explicitly requires.
    /// 2. STRUCTURE INTEGRITY FOR ORDINARY MACHINERY. The reference gives hit points to walls, barricades, turrets
    ///    and cannons only (<c>campaignDefence.ts</c>'s <c>defenceMax</c>), so every other machine has
    ///    <see cref="MachineSpec.Hp"/> 0 — and <see cref="TurretRules.IsDefence"/> false, which made
    ///    <see cref="DirectorRules.HostileOpen"/> treat an assembler, a chest or an inserter as ground no raid can
    ///    walk over, breach or destroy. A line of supply chests was therefore an invulnerable wall. Giving every
    ///    BUILDABLE machine an integrity row removes that barrier at its source: the existing damage, breach,
    ///    disable and repair paths then all apply to it without a special case anywhere.
    ///
    /// Applied AFTER <see cref="OpeningBalance"/> in both data paths (<see cref="ReferenceData.Create"/> and the
    /// Unity <c>GameDataRegistry.Build</c>) so it sees the costs that build actually charges.
    /// </summary>
    public static class CombatBalance
    {
        /// <summary>Integrity a buildable machine has before its cost is counted.</summary>
        public const double IntegrityBase = 20;
        /// <summary>Integrity added per item in its build cost.</summary>
        public const double IntegrityPerItem = 2;
        /// <summary>The most a derived integrity row reaches, so a big machine is not tougher than a barricade.</summary>
        public const double IntegrityCap = 120;

        /// <summary>
        /// The approved Breaker (CONTENT_CATALOGUE.md §7.4). The windup and the reach are the two values §7.4
        /// leaves to the implementer (Q06): 1.0 s is between the Spitter's 0.6 and the freight guardian's 1.2 and
        /// is the visible tell the role's "invites concentrated fire" depends on, and 1.5 tiles is the Skitter's
        /// 1.3 widened for a heavier body.
        /// </summary>
        public static EnemyDef Breaker() => new EnemyDef(
            "breaker", "Breaker",
            220.0, 2.7, 15.0, 2.0, 1.0, 1.5,
            false, "slow structure-breaker; invites concentrated fire, pushes through light",
            // Kind must be one of the three CatalogueDataTests.Provenance accepts, and must agree with the
            // Provisional flag. The role is APPROVED and the numbers are not, which is a Source note, not a
            // fourth kind: the row is provisional until the owner signs off the values.
            "provisional",
            "Unity/Docs/CONTENT_CATALOGUE.md §7.4; owner decisions 2026-09-11 Q14 (HP 220); GP-W5; "
            + "approved role, provisional values",
            true);

        public static GameData Apply(GameData d)
        {
            var enemies = new List<EnemyDef>(d.Enemies);
            var has = false;
            for (var i = 0; i < enemies.Count; i++) if (enemies[i].Key == "breaker") has = true;
            if (!has) enemies.Add(Breaker());

            var machines = new List<MachineSpec>(d.Machines);
            for (var i = 0; i < machines.Count; i++)
            {
                var m = machines[i];
                var hp = Integrity(m);
                if (hp > 0 && hp != m.Hp) machines[i] = m with { Hp = hp };
            }

            return new GameData(d.Items, machines, d.Recipes, d.Engineer, d.World, d.Weapons, enemies,
                d.Ammunition, d.Turrets, d.Power, d.Time, d.Raids, d.Opening, d.Stake, d.Defence, d.Siege);
        }

        /// <summary>
        /// What a machine's integrity row should be. 0 leaves the row alone, which covers three cases:
        ///
        /// * a kind that already has one (wall 120, barricade 240, turret 100, cannon 140 — the reference's own
        ///   numbers, which stay exactly as exported);
        /// * a WALK-THROUGH kind (belt, fast belt, pole, track, tram). A body walks over these, so they can never
        ///   block one, and giving them integrity would only invite a raid to stop and chew a 1-steel belt — the
        ///   brief's "avoid targeting every low-value belt unnecessarily". A cut supply lane is still reachable
        ///   through the inserters that feed it, which are ordinary buildable machines and do get a row;
        /// * a WORLD FIXTURE with no build cost (the Depot). It is scenery placed by the region, not something the
        ///   player put up, and it stays the solid landmark the map draws.
        /// </summary>
        public static double Integrity(MachineSpec m)
        {
            if (m == null || m.Hp > 0) return 0;
            if (Ground.WalkThrough(m.Key)) return 0;
            var items = 0.0;
            if (m.Cost != null) for (var i = 0; i < m.Cost.Count; i++) items += m.Cost[i].Count;
            if (items <= 0) return 0;
            return Math.Min(IntegrityCap, IntegrityBase + IntegrityPerItem * items);
        }
    }
}
