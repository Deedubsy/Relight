using System;
using System.Collections.Generic;

namespace Relight.Sim
{
    /// <summary>Reference engineer.ts `InventoryAction` `{type:'sort'}`: rebuild the Backpack from scratch, quantities unchanged.</summary>
    public sealed record InventorySortCommand : Command;

    /// <summary>
    /// Reference `InventoryAction` `{type:'move'}`. <c>Layout</c> is the Backpack layout the UI had when the player
    /// picked the stack up (<see cref="PackLayout.Json"/>); a mismatch refuses, so a stack that moved underneath the
    /// player is never silently re-targeted.
    /// </summary>
    public sealed record InventoryMoveCommand(int From, int To, string Item, double Count, string Layout) : Command;

    /// <summary>
    /// Reference `InventoryAction` `{type:'split'}`. <c>N</c> is how many to split off; a negative value means the
    /// reference's default of half the stack (`a.n ?? Math.floor(src.count/2)`).
    /// </summary>
    public sealed record InventorySplitCommand(int From, int To, string Item, double Count, string Layout, int N = -1) : Command;

    /// <summary>
    /// Reference engineer.ts `inventoryCommand`, message for message. The kit branch
    /// ('Kits reserve ten slots…') is retired with kits (CONTENT_CATALOGUE §17).
    /// </summary>
    public sealed class InventoryCommandHandler : ICommandHandler
    {
        public bool TryApply(SimContext ctx, SimState st, Command c, out CommandResult result)
        {
            var d = ctx.Data;
            var e = st.Engineer;
            switch (c)
            {
                case InventorySortCommand _:
                    e.Pack = Pockets.Slots(d, e.Inv, null);
                    result = CommandResult.Ok("Backpack sorted; quantities unchanged.");
                    return true;
                case InventoryMoveCommand m:
                    result = Apply(d, e, m.From, m.To, m.Item, m.Count, m.Layout, false, 0);
                    return true;
                case InventorySplitCommand s:
                    result = Apply(d, e, s.From, s.To, s.Item, s.Count, s.Layout, true, s.N);
                    return true;
                default:
                    result = default;
                    return false;
            }
        }

        private static CommandResult Apply(GameData d, Engineer e, int from, int to, string item, double count,
            string layout, bool split, int n)
        {
            var slots = Pockets.Slots(d, e);
            if (layout == null || !string.Equals(layout, PackLayout.Json(slots), StringComparison.Ordinal))
                return CommandResult.Refuse("Stacks changed. Select the stack again.");
            if (from < 0 || to < 0 || from >= slots.Count || to >= slots.Count || from == to)
                return CommandResult.Refuse("Choose a different backpack slot.");
            var src = slots[from];
            var dst = slots[to];
            if (src == null || item == null || !string.Equals(src.Item, item, StringComparison.Ordinal) || src.Count != count)
                return CommandResult.Refuse("Source stack changed.");

            if (split)
            {
                var take = n >= 0 ? n : (int)Math.Floor(src.Count / 2);
                if (take < 1 || take >= src.Count) return CommandResult.Refuse("Choose fewer than the stack contains.");
                if (dst != null) return CommandResult.Refuse("Splitting needs an empty slot.");
                slots[to] = new PackStack(src.Item, take);
                src.Count -= take;
            }
            else if (dst == null)
            {
                slots[to] = src;
                slots[from] = null;
            }
            else if (string.Equals(dst.Item, src.Item, StringComparison.Ordinal))
            {
                var move = Math.Min(src.Count, d.StackSize(new ItemKey(src.Item)) - dst.Count);
                if (move <= 0) { slots[from] = dst; slots[to] = src; }
                else
                {
                    dst.Count += move;
                    src.Count -= move;
                    if (src.Count <= 0) slots[from] = null;
                }
            }
            else { slots[from] = dst; slots[to] = src; }

            e.Pack = slots;
            return CommandResult.Ok(split ? "Stack split." : "Stack moved.");
        }
    }

    /// <summary>Read-only Backpack helpers the presentation layer needs alongside the commands.</summary>
    public static class Backpack
    {
        /// <summary>The layout string to echo back with the next move/split (reference `JSON.stringify(pocketSlots(e))`).</summary>
        public static string Layout(GameData d, Engineer e) => PackLayout.Json(Pockets.Slots(d, e));

        public static List<PackStack> Cells(GameData d, Engineer e) => Pockets.Slots(d, e);
    }
}
