using System.Collections.Generic;

namespace Relight.Sim
{
    /// <summary>
    /// The conservation ledger as presentation sees it (reference ledger.ts `Ledger`). An immutable record over
    /// snapshots the sim no longer owns — no sim object references (TA §4.5). Index <c>where</c> with
    /// <see cref="LedgerPlace"/>.
    /// </summary>
    public sealed record LedgerView(
        bool Ok,
        double Tolerance,
        int OpenedAt,
        ItemCounts Opening,
        ItemCounts Sources,
        ItemCounts Sinks,
        ItemCounts Held,
        ItemCounts[] Where,
        ItemCounts Unexplained,
        IReadOnlyList<string> Problems)
    {
        public double At(LedgerPlace place, ItemId item) => Where[(int)place][item];
    }

    /// <summary>Read-only ledger selectors (MIGRATION_MAP S-07/S-08). Never mutates state.</summary>
    public static class LedgerQueries
    {
        /// <summary>The conservation check right now (reference `conservation(st)`).</summary>
        public static LedgerView Conservation(SimContext ctx, SimState st, double tolerance = 0.01) =>
            Ledger.Conservation(st, ctx.Data, tolerance);

        /// <summary>One line an item that is out of tolerance, empty when the ledger balances.</summary>
        public static IReadOnlyList<string> Problems(SimContext ctx, SimState st) => Conservation(ctx, st).Problems;
    }
}
