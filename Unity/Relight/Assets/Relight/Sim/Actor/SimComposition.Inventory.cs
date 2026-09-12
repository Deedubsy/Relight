using System.Collections.Generic;

namespace Relight.Sim
{
    public static partial class SimComposition
    {
        static partial void AddInventoryPhases(List<ITickPhase> list)
        {
            list.Add(new HandCraftPhase());   // reference flow.ts tickHand (craft half; hand mining is Phase C)
        }

        static partial void AddInventoryInitializers(List<IStateInitializer> list)
        {
            list.Add(new InventoryInitializer());
        }

        static partial void AddInventoryHandlers(List<ICommandHandler> list)
        {
            list.Add(new InventoryCommandHandler());
            list.Add(new HandCraftHandler());
            list.Add(new MachineTransferHandler());
            list.Add(new PlacementHandler());
        }
    }

    /// <summary>
    /// New-game inventory state: the campaign's opening Backpack and the conservation ledger's opening stock.
    /// Reference rules.ts:10 `CAMPAIGN_START_POCKETS = { steel: 20, copper: 5 }`, applied at flow.ts:419
    /// (`st.engineer.inv = {...CAMPAIGN_START_POCKETS}`) and recorded as opening stock by `openLedger`
    /// (U-D-18). The retired `START_*` constants (CONTENT_CATALOGUE §17) are not ported.
    ///
    /// The stake is DATA, not a constant: the pockets come from <see cref="GameData.Stake"/>
    /// (CONTENT_CATALOGUE §15, authored as `Tuning - Starting stake`), so editing that asset changes what a new
    /// game starts with. <see cref="StartSteel"/>/<see cref="StartCopper"/> remain as the reference fallback used
    /// only when the data cannot be trusted — see <see cref="StakeProblem"/>, which is the port's "results are
    /// data" answer to a data fault (an initialiser has no result channel and never throws).
    /// </summary>
    public sealed class InventoryInitializer : IStateInitializer
    {
        /// <summary>Reference rules.ts:10 fallback, used only when <see cref="StakeProblem"/> is non-empty.</summary>
        public const int StartSteel = 20;
        public const int StartCopper = 5;

        /// <summary>
        /// Why the authored starting stake cannot be used, or "" when it is fine. A non-empty result means a new
        /// game falls back to the reference stake above rather than silently starting on bad or foreign data;
        /// the same string is what a boot check or a data screen shows (the shape of `Ledger` conservation
        /// problems and <see cref="CommandResult.Problem"/>: refusals are text, never exceptions).
        /// </summary>
        public static string StakeProblem(GameData d)
        {
            var stake = d?.Stake;
            if (stake == null) return "GameData carries no starting stake (CONTENT_CATALOGUE §15)";
            // The stake is written for one ruleset. Running exploration-v2 off a legacy-v1 stake would start the
            // campaign on retired numbers, so the mismatch is reported and the data refused (U-D-32, §17.2).
            if (string.IsNullOrEmpty(stake.Ruleset))
                return "the starting stake names no ruleset; " + SimVersion.Ruleset + " was expected";
            if (!string.Equals(stake.Ruleset, SimVersion.Ruleset, System.StringComparison.Ordinal))
                return $"the starting stake is authored for ruleset '{stake.Ruleset}' but the sim runs '{SimVersion.Ruleset}'";
            var pockets = stake.Pockets;
            if (pockets == null || pockets.Count == 0) return "the starting stake has no pockets";
            for (var i = 0; i < pockets.Count; i++)
                if (pockets[i].Count <= 0) return $"the starting stake holds a non-positive count of {Items.Key(pockets[i].Item)}";
            return "";
        }

        public void Init(SimContext ctx, SimState st)
        {
            var e = st.Engineer;
            e.Reach = ctx.Data.Engineer.ReachTiles;
            e.Inv.Items.Clear();   // the ItemBag's other keys (weapons, packed machines) do not exist at t = 0

            if (StakeProblem(ctx.Data).Length == 0)
            {
                var pockets = ctx.Data.Stake.Pockets;
                // Added, not assigned: a stake that names the same item twice is one stack of the sum.
                for (var i = 0; i < pockets.Count; i++) e.Inv.Items.Add(pockets[i].Item, pockets[i].Count);
            }
            else
            {
                e.Inv[ItemId.Steel] = StartSteel;
                e.Inv[ItemId.Copper] = StartCopper;
            }

            st.Stats = new Stats();
            st.Hand = new HandState();
            // After the pockets are filled, never before: the opening stock IS the stake, so conservation reads
            // zero at tick 0 (U-D-05 / U-D-18).
            st.Ledger = Ledger.Open(st, ctx.Data);
        }
    }
}
