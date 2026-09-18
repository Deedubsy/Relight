using System;
using System.Globalization;

namespace Relight.Sim.UI
{
    /// <summary>
    /// C-06. The Home workshop section's sentences, ported from <c>inventoryPanel.ts</c>'s <c>coreCard</c> and
    /// <c>recipeCard</c> and fixed by UI_AND_ONBOARDING.md §6.
    ///
    /// The workshop section is recipe cards only. The old "Equipment · two slots" block that used to sit here was
    /// removed by GP-PLAYTEST-FIX 3 and must not come back (U-D-11): the two equipped slots live in the equipment
    /// strip under the Backpack grid (§5.6), and duplicating them in the workshop is the defect that fix removed.
    /// </summary>
    public static class WorkshopText
    {
        /// <summary>The workshop section's heading.</summary>
        public const string Heading = "Home workshop";

        /// <summary>coreCard: the first card of the section is always the Base core (GP-HOME-REPAIR).</summary>
        public const string CoreTitle = "Base core";
        public const string CoreDisabledSuffix = " · DISABLED";

        /// <summary>coreCard, said once a repair is queued.</summary>
        public const string RepairStarted =
            "Repair started. Stay within reach of the Home; the materials pay for it once.";

        /// <summary>coreCard, when there is nothing to repair.</summary>
        public const string Intact = "Intact.";

        /// <summary>
        /// recipeCard: the batch is finished but the workshop's own output tray is full, so the job HOLDS —
        /// nothing is consumed and nothing is made twice (U-D-44). Collecting resumes it.
        /// </summary>
        public const string FinishedNoRoom = "Output tray full · collect the finished goods to resume";

        /// <summary>
        /// recipeCard, under the Craft button. U-D-44: queueing a job spends the ingredients into the workshop
        /// and the workshop keeps processing while the engineer is anywhere else, so this line no longer claims
        /// the player has to stand still.
        /// </summary>
        public static string UsesCarried(double seconds)
            => "Uses Backpack materials · " + Secs(seconds) + " s a batch · keeps working while you are away";

        /// <summary>
        /// §6 — the prompt while the engineer is locked in place. The first clause is the sim's own
        /// <c>Home.LockText</c>, shown verbatim; the panel only adds how to get out of it. Since U-D-44 the only
        /// thing that pins the engineer at Home is a running core repair; a workshop job never does.
        /// </summary>
        public static string HandLockPrompt(string lockText) => lockText + " · Escape cancels";

        /// <summary>Buttons.</summary>
        public const string Craft = "Craft";
        public const string Cancel = "Cancel";
        public const string Collect = "Collect all";

        /// <summary>The output tray row under the cards, when it holds nothing (U-D-44).</summary>
        public const string OutputEmpty = "Output tray: empty. Finished goods wait here until you collect them.";

        /// <summary>
        /// The tray's capacity line (GP-UX-2). It used to list the contents as well — "Bullets ×40 · Rifle ×1" —
        /// because the tray had nothing but this one sentence to speak through. The tray is drawn as a slot grid
        /// now, so the contents are on screen as items and this row says only how full it is; repeating the list
        /// above the grid would be the same fact twice, in a worse form.
        /// </summary>
        public static string OutputStacksLine(int usedStacks, int stacks)
            => "Output tray · " + usedStacks.ToString(CultureInfo.InvariantCulture)
               + " / " + stacks.ToString(CultureInfo.InvariantCulture) + " stacks";

        /// <summary>The line under the tray grid: how to get one stack out rather than all of it.</summary>
        public const string OutputGridHint =
            "Drag a stack into the Backpack, or click it to take that stack. Collect all empties the tray.";

        /// <summary>The Collect button's reason when the engineer is not standing at the workshop.</summary>
        public const string CollectAway = "Walk to the Home workshop to collect.";

        /// <summary>A card whose recipe has batches queued behind the one being processed.</summary>
        public static string QueuedBatches(int batches)
            => batches.ToString(CultureInfo.InvariantCulture)
               + (batches == 1 ? " batch queued · " : " batches queued · ")
               + "processing continues while you are away";

        /// <summary>The line under a running batch: what Cancel does to the ingredients still held for it.</summary>
        public const string CancelRefunds = "Cancel returns the unused ingredients.";

        /// <summary>"Base core" / "Base core · DISABLED" — coreCard's title (hp of 0 means the core is down).</summary>
        public static string CoreCardTitle(double hp)
            => hp <= 0 ? CoreTitle + CoreDisabledSuffix : CoreTitle;

        /// <summary>coreCard: <c>`${Math.ceil(cost.hp)} / ${cost.max} HP`</c>.</summary>
        public static string CoreHp(double hp, double max)
            => Ceil(hp) + " / " + Ceil(max) + " HP";

        /// <summary>
        /// coreCard's one button, in its three states: a running repair counts down, a dead core is
        /// recommissioned, and a damaged core is repaired by a fixed amount.
        /// </summary>
        public static string CoreButton(bool inProgress, double remainingSeconds,
                                        bool recommission, double seconds, double repairHp)
        {
            if (inProgress) return "Repairing · " + Ceil(remainingSeconds) + " s left";
            if (recommission) return "Recommission core · " + Secs(seconds) + " s";
            return "Repair +" + Secs(repairHp) + " HP · " + Secs(seconds) + " s";
        }

        /// <summary>
        /// coreCard's note when the Backpack is short. It names the two materials and where to get them, because
        /// the answer at this point in the opening is always "drag them out of Home storage".
        /// </summary>
        public static string CoreShortfall(double steel, double copper)
            => "Move " + Secs(steel) + " Steel plates and " + Secs(copper)
               + " Copper into the Backpack (drag from Home storage) to repair.";

        /// <summary>recipeCard: <c>`${name(r.output)} ×${r.count}`</c>, e.g. "Bullets ×10".</summary>
        public static string RecipeTitle(string outputName, double count)
            => outputName + " ×" + PackLayout.Num(count);

        /// <summary>recipeCard: the duration chip, "12 s".</summary>
        public static string Duration(double seconds) => Secs(seconds) + " s";

        /// <summary>
        /// recipeCard while a batch runs:
        /// <c>`${Math.ceil(Math.max(0,secs-p.done))} s left${p.queued>1?` · ${p.queued-1} more queued`:''}`</c>.
        /// </summary>
        public static string Progress(double seconds, double done, int queued)
        {
            var left = seconds - done;
            if (left < 0) left = 0;
            var text = Ceil(left) + " s left";
            if (queued > 1) text += " · " + (queued - 1).ToString(CultureInfo.InvariantCulture) + " more queued";
            return text;
        }

        /// <summary>recipeCard: <c>`Need ${c.need-have} more ${name(c.item)}`</c>.</summary>
        public static string NeedMore(double need, double have, string itemName)
        {
            var missing = need - have;
            if (missing < 0) missing = 0;
            return "Need " + PackLayout.Num(missing) + " more " + itemName;
        }

        /// <summary>recipeCard's ingredient chip text, "2 /20" — carried over required.</summary>
        public static string Chip(double have, double need)
            => PackLayout.Num(have) + " /" + PackLayout.Num(need);

        /// <summary>recipeCard's ingredient tooltip: <c>`${name} · ${need} required · ${have} in Backpack`</c>.</summary>
        public static string ChipTooltip(string itemName, double need, double have)
            => itemName + " · " + PackLayout.Num(need) + " required · " + PackLayout.Num(have) + " in Backpack";

        private static string Ceil(double v)
            => ((long)Math.Ceiling(v)).ToString(CultureInfo.InvariantCulture);

        private static string Secs(double v) => PackLayout.Num(v);
    }
}
