namespace Relight.Sim
{
    /// <summary>
    /// Start a paid repair (reference campaignDefence.ts <c>startRepair</c>). <paramref name="Kind"/> is
    /// <see cref="RepairKinds"/>; <paramref name="Id"/> is the machine id for <see cref="RepairKinds.Machine"/> and
    /// ignored for the core, which has no id.
    /// </summary>
    public sealed record RepairCommand(int Kind, int Id) : Command;

    /// <summary>
    /// Stop the repair and refund what was charged. The reference has no cancel — its repair runs to completion or
    /// pauses forever — but the port's repair hand-locks the engineer (brief C-05), so there must be a way out, the
    /// same way <c>CancelCraftCommand</c> ends a craft batch.
    /// </summary>
    public sealed record CancelRepairCommand : Command;

    /// <summary>
    /// Reference <c>repairCheck</c> + <c>startRepair</c>. Refusal texts are the brief's, which differ from the
    /// reference's wording in three places (reference text in brackets):
    /// "the core is already at full health" ["nothing damaged here"], "already repairing"
    /// ["a repair is already in progress"], "repairing needs N steel and N copper"
    /// ["repair needs N Steel plates and N Copper in your Backpack"]. The remaining two are the reference's own:
    /// "walk closer to repair" and "wait for the attackers to leave".
    /// </summary>
    public sealed class HomeCoreHandler : ICommandHandler
    {
        public bool TryApply(SimContext ctx, SimState st, Command c, out CommandResult result)
        {
            switch (c)
            {
                case RepairCommand r: result = Start(ctx, st, r.Kind, r.Id); return true;
                case CancelRepairCommand: result = Cancel(ctx, st); return true;
                default: result = default; return false;
            }
        }

        private static CommandResult Start(SimContext ctx, SimState st, int kind, int id)
        {
            // GP-W5: every refusal above lives in HomeCore.RepairProblem now, in this same order and with these
            // same words, so the machine repair card can say why the button is greyed without a second copy of
            // the rules. Nothing about what the player is told has changed.
            var problem = HomeCore.RepairProblem(ctx, st, kind, id);
            if (problem.Length != 0) return CommandResult.Refuse(problem);

            var h = st.Home;
            var p = HomeCore.Price(ctx.Data, st, kind, id);
            var e = st.Engineer;

            // Charged at start (U-D-05): the cost leaves the pockets and is recorded as a ledger sink.
            var steel = Pockets.Drop(ctx.Data, e, ItemKey.Of(ItemId.Steel), p.Steel);
            var copper = Pockets.Drop(ctx.Data, e, ItemKey.Of(ItemId.Copper), p.Copper);
            st.Stats.SpentSteel += steel;
            st.Stats.SpentCopper += copper;

            h.RepairKind = kind;
            h.RepairId = kind == RepairKinds.Core ? -1 : id;
            h.RepairRemaining = p.Seconds;
            h.RepairRecommission = p.Recommission;
            h.PaidSteel = (int)steel;
            h.PaidCopper = (int)copper;
            return CommandResult.Ok();
        }

        private static CommandResult Cancel(SimContext ctx, SimState st)
        {
            var h = st.Home;
            if (h == null || h.RepairKind == RepairKinds.None) return CommandResult.Refuse("no repair is in progress");

            // Refund exactly what was charged, and unwind the sink by however much actually fitted back in the
            // pockets — anything that does not fit stays spent, so the ledger still balances (U-D-05).
            var back = Pockets.Take(ctx.Data, st.Engineer, ItemKey.Of(ItemId.Steel), h.PaidSteel);
            st.Stats.SpentSteel -= back;
            back = Pockets.Take(ctx.Data, st.Engineer, ItemKey.Of(ItemId.Copper), h.PaidCopper);
            st.Stats.SpentCopper -= back;

            h.RepairKind = RepairKinds.None;
            h.RepairId = -1;
            h.RepairRemaining = 0;
            h.RepairRecommission = false;
            h.PaidSteel = 0;
            h.PaidCopper = 0;
            return CommandResult.Ok();
        }
    }
}
