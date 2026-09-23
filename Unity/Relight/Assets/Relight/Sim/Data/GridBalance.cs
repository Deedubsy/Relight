using System.Collections.Generic;

namespace Relight.Sim
{
    /// <summary>
    /// REL-128 / U-D-71 (c), the owner's "Power pole connection range needs to be increased by about 50%": how far
    /// the port's power carries.
    ///
    /// It is its own layer rather than a line in <see cref="DarkWorld"/> because reach is not light, and rather than
    /// an edit to the tuning asset because <c>Sim/Data/Generated/CatalogueData.g.cs</c> is generated from the
    /// TypeScript reference in <c>packages/</c> — which this project may not edit — and is what every offline sim
    /// test reads, while the <c>.asset</c> is what the game reads. A number that moved in only one of them would
    /// leave the tests and the game disagreeing. This layer is applied in BOTH paths
    /// (<see cref="ReferenceData.Create"/> and the Unity <c>GameDataRegistry.Build</c>), so there is one answer.
    ///
    /// It is deliberately NOT in <c>GameDataRegistry.BuildLegacy</c>. That path means "the original balance, but
    /// still no sun", and reach is balance: a save laid out under eight-tile poles keeps the grid it was built for.
    ///
    /// **The 50% is the owner's; the substation is the implementer's** (U-P-30). The note says "power pole". The
    /// substation is included because leaving it behind would break something concrete rather than merely look
    /// untidy: the authored city's substation lots are reach nodes in their own right
    /// (<see cref="PowerGrid.SiteReach"/>), and the opening's own advice describes pole-to-pole linking while
    /// quoting that site reach. Raise the pole alone and the tutorial says eight tiles while poles link at twelve.
    /// </summary>
    public static class GridBalance
    {
        /// <summary>The owner's "about 50%", applied to all three distribution reaches.</summary>
        public const double ReachScale = 1.5;

        /// <summary>Pole: the reference's 8 tiles at <see cref="ReachScale"/>.</summary>
        public const double PoleReachTiles = 12;

        /// <summary>Big pole: the reference's 12 tiles at <see cref="ReachScale"/>.</summary>
        public const double BigPoleReachTiles = 18;

        /// <summary>
        /// Substation: the reference's 8 tiles at <see cref="ReachScale"/>. The implementer's extension, not the
        /// owner's request — see the class remarks for why it is not merely tidiness.
        /// </summary>
        public const double SubstationReachTiles = 12;

        /// <summary>
        /// The reach this layer gives a distribution machine, or 0 for a kind it has no value for — which leaves
        /// that kind's own row exactly as it arrived, so a node added later is not silently rescaled by a layer
        /// that has never heard of it.
        /// </summary>
        public static double Reach(string machineKey)
        {
            switch (machineKey)
            {
                case "pole": return PoleReachTiles;
                case "bigpole": return BigPoleReachTiles;
                case "substation": return SubstationReachTiles;
                default: return 0;
            }
        }

        public static GameData Apply(GameData d)
        {
            if (d == null) return null;
            var changed = false;

            // Written as absolute values rather than multiplied in place, so applying this layer twice lands on the
            // same numbers as applying it once; a multiplier would compound to 2.25.
            var machines = d.Machines;
            if (machines != null)
            {
                List<MachineSpec> copy = null;
                for (var i = 0; i < machines.Count; i++)
                {
                    var m = machines[i];
                    if (m == null) continue;
                    var reach = Reach(m.Key);
                    // A row that carries no reach at all is not a distribution node whatever its key says, and is
                    // left alone.
                    if (reach <= 0 || m.ReachTiles <= 0 || m.ReachTiles == reach) continue;
                    if (copy == null) copy = new List<MachineSpec>(machines);
                    copy[i] = m with { ReachTiles = reach };
                }
                if (copy != null) { machines = copy; changed = true; }
            }

            // The same three numbers in the tuning record. They must agree with the machine rows above: the
            // authored city's substation lots have no machine row at all and read their reach from here
            // (PowerGrid.SiteReach), and the opening advice quotes a reach in words.
            var power = d.Power;
            if (power != null && (power.PoleReachTiles != PoleReachTiles
                || power.BigPoleReachTiles != BigPoleReachTiles
                || power.SubstationReachTiles != SubstationReachTiles))
            {
                power = power with
                {
                    PoleReachTiles = PoleReachTiles,
                    BigPoleReachTiles = BigPoleReachTiles,
                    SubstationReachTiles = SubstationReachTiles,
                };
                changed = true;
            }

            if (!changed) return d;
            return new GameData(d.Items, machines, d.Recipes, d.Engineer, d.World, d.Weapons, d.Enemies,
                d.Ammunition, d.Turrets, power, d.Time, d.Raids, d.Opening, d.Stake, d.Defence, d.Siege);
        }
    }
}
