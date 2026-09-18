using System;

namespace Relight.Sim
{
    /// <summary>
    /// Set a placed processor's recipe (reference flow.ts:987 <c>setRecipe</c>, RI-01 / D-B2-1 (b): "T on it in the
    /// world view"). The reference addresses the machine by tile; the port addresses it by id, as every other
    /// Phase B/C command does.
    /// </summary>
    public sealed record SetRecipeCommand(int Id, string RecipeKey) : Command;

    /// <summary>
    /// Reference flow.ts:987-999, ported with its refusals:
    /// <code>
    /// if (!m || !isProcessor(m)) return 'no Assembler there';
    /// if (!recipesFor(m).includes(id)) return 'recipe is not supported by this machine';
    /// if (id === 'shell' &amp;&amp; !arsenal) return 'Restore the Arsenal first';
    /// if (current === id) return '';
    /// if (m.out &gt; 0) { ...pockets or refuse... }
    /// if (m.busy) { give the inputs back, un-count them }
    /// m.timer = 0; m.recipe = id;
    /// </code>
    /// Deliberate differences:
    /// <list type="bullet">
    /// <item>The finished output lives in <c>Machine.Inv</c>, not in the single <c>m.out</c> slot, so there is
    ///       nothing to push into the Backpack and the "the Backpack is full (N o to take out first)" refusal
    ///       cannot arise. Items of the old output stay in the machine and come back on pick-up, which is what the
    ///       reference already does with inputs the new recipe does not take.</item>
    /// <item>The Arsenal gate on the Shell recipe needs campaign progression (C-05/C-09) and is not applied yet;
    ///       the refusal text is kept here so C-09 only has to supply the flag.</item>
    /// </list>
    /// </summary>
    public sealed class SetRecipeHandler : ICommandHandler
    {
        public const string NoMachineText = "no machine there to set a recipe on";
        public const string UnsupportedText = "that recipe is not supported by this machine";
        public const string ArsenalText = "restore the Arsenal first";

        public bool TryApply(SimContext ctx, SimState st, Command c, out CommandResult result)
        {
            if (!(c is SetRecipeCommand a)) { result = default; return false; }
            var d = ctx.Data;
            var m = st.MachineById(a.Id);
            if (m == null || !ProductionRules.IsProcessor(d, m)) { result = CommandResult.Refuse(NoMachineText); return true; }
            if (!Interaction.InReach(ctx, st, m)) { result = CommandResult.Refuse("Walk closer to configure the machine"); return true; }
            if (!ProductionRules.Supports(d, m, a.RecipeKey)) { result = CommandResult.Refuse(UnsupportedText); return true; }

            var old = ProductionRules.RecipeOf(d, st, m);
            if (old != null && string.Equals(old.Key, a.RecipeKey, StringComparison.Ordinal))
            {
                result = CommandResult.Ok("");   // reference: the same recipe is a no-op, not a refusal
                return true;
            }

            var work = st.Production.Of(m.Id);
            if (work.Busy && old != null)
            {
                for (var i = 0; i < old.Inputs.Count; i++)
                {
                    m.Inv.Add(old.Inputs[i].Item, old.Inputs[i].Count);
                    st.Stats.Consumed.Add(old.Inputs[i].Item, -old.Inputs[i].Count);
                }
                work.Busy = false;
            }
            work.Timer = 0;
            work.Stall = (int)MachineOperatingState.Idle;
            work.Recipe = a.RecipeKey;
            d.TryRecipe(a.RecipeKey, out var r);
            result = CommandResult.Ok(r != null ? $"{r.DisplayName} set" : "recipe set");
            return true;
        }
    }
}
