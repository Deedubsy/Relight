using System;
using System.Collections.Generic;

namespace Relight.Sim
{
    /// <summary>
    /// The data-driven answers the machine phase, the inventory and the UI all need: which machines process, what
    /// recipe they run, how fast, how much they buffer and where their output goes.
    ///
    /// The reference hard-codes each of these as a kind list (flow.ts:146 <c>isProcessor</c>, :151
    /// <c>recipesFor</c>, :200 <c>recipeOf</c>, :207 <c>ASM_INPUT_MULT</c>/<c>ASM_OUTPUT_CAP</c>,
    /// progression.ts:49 <c>processingMultiplier</c>). The port reads them from <see cref="MachineSpec"/>
    /// (<c>RecipeStation</c>, <c>DefaultRecipe</c>, <c>SpeedMul</c>, <c>InputBufferMul</c>, <c>OutputBufferCap</c>)
    /// so the catalogue export owns the tuning, as the Phase C contract requires.
    /// </summary>
    public static class ProductionRules
    {
        /// <summary>Reference flow.ts:24 <c>EPS</c>.</summary>
        public const double Eps = 1e-9;

        // ---- kinds ---------------------------------------------------------------------------------------------

        /// <summary>Reference flow.ts:146 <c>isProcessor</c>: a machine that runs a recipe. Data: it has a station.</summary>
        public static bool IsProcessor(GameData d, Machine m) =>
            m != null && d.TryMachine(m.Kind, out var s) && s.RecipeStation.Length > 0;

        /// <summary>
        /// The Excavator and the Pumpjack: a machine with an inventory and a rate but no recipe station
        /// (reference flow.ts:1112 <c>m.kind==='excavator'||m.kind==='pumpjack'</c>).
        /// </summary>
        public static bool IsMiner(GameData d, Machine m) =>
            m != null && d.TryMachine(m.Kind, out var s) && s.HasInventory && s.RatePerS > 0 && s.RecipeStation.Length == 0;

        /// <summary>Reference progression.ts:49 <c>processingMultiplier</c>, machine half: the Mk2's 2×.
        /// The Overclock module's artifact multiplier is progression state (C-05/C-09) and is not applied yet.</summary>
        public static double SpeedMul(GameData d, Machine m) =>
            d.TryMachine(m.Kind, out var s) && s.SpeedMul > 0 ? s.SpeedMul : 1;

        /// <summary>Reference flow.ts:207 <c>ASM_INPUT_MULT</c> (4 crafts' worth of each input).</summary>
        public static double InputBufferMul(GameData d, Machine m) =>
            d.TryMachine(m.Kind, out var s) && s.InputBufferMul > 0 ? s.InputBufferMul : 4;

        // ---- recipes -------------------------------------------------------------------------------------------

        /// <summary>
        /// Reference flow.ts:151 <c>recipesFor</c>: the recipes this machine may be set to. Data: every recipe whose
        /// <c>Station</c> is the machine's <c>RecipeStation</c> and that has at least one input — the input-free
        /// alien decode (<c>alien-decode</c>) is a campaign action, not a settable production recipe.
        /// </summary>
        public static void RecipesFor(GameData d, Machine m, List<Recipe> into)
        {
            into.Clear();
            if (!d.TryMachine(m.Kind, out var s) || s.RecipeStation.Length == 0) return;
            for (var i = 0; i < d.Recipes.Count; i++)
            {
                var r = d.Recipes[i];
                if (!string.Equals(r.Station, s.RecipeStation, StringComparison.Ordinal)) continue;
                if (r.Inputs == null || r.Inputs.Count == 0) continue;
                into.Add(r);
            }
        }

        /// <summary>
        /// REL-26 (ECO-02): one line per recipe whose <c>Station</c> nothing runs, neither a machine's
        /// <c>RecipeStation</c> nor the Home workshop (<see cref="HandCraft.Station"/>). No player can make such a
        /// recipe. Empty when every station is provided. The data check (DataValidator) and the catalogue tests read it.
        /// </summary>
        public static List<string> UnprovidedStations(GameData d)
        {
            var provided = new HashSet<string>(StringComparer.Ordinal) { HandCraft.Station };
            foreach (var s in d.Machines)
                if (s.RecipeStation.Length > 0) provided.Add(s.RecipeStation);
            var problems = new List<string>();
            foreach (var r in d.Recipes)
                if (!provided.Contains(r.Station ?? ""))
                    problems.Add($"Recipe '{r.Key}': its station '{r.Station}' is run by no machine and is not the {HandCraft.Station}");
            return problems;
        }

        /// <summary>Whether a recipe key is one this machine may run.</summary>
        public static bool Supports(GameData d, Machine m, string key)
        {
            if (!d.TryMachine(m.Kind, out var s) || s.RecipeStation.Length == 0) return false;
            if (!d.TryRecipe(key, out var r)) return false;
            return r.Inputs != null && r.Inputs.Count > 0 && string.Equals(r.Station, s.RecipeStation, StringComparison.Ordinal);
        }

        /// <summary>
        /// Reference flow.ts:200 <c>recipeOf</c>: the chosen recipe, or the machine's default when nothing is
        /// chosen (a Phase B save has no choice recorded, exactly as a pre-RI-01 save had no <c>m.recipe</c>).
        /// </summary>
        public static Recipe RecipeOf(GameData d, SimState st, Machine m)
        {
            var chosen = st.Production.Find(m.Id)?.Recipe;
            if (!string.IsNullOrEmpty(chosen) && d.TryRecipe(chosen, out var r) && Supports(d, m, chosen)) return r;
            return DefaultRecipe(d, m);
        }

        /// <summary>The machine's data default (<c>MachineSpec.DefaultRecipe</c>), or null for a non-processor.</summary>
        public static Recipe DefaultRecipe(GameData d, Machine m)
        {
            if (!d.TryMachine(m.Kind, out var s) || s.DefaultRecipe.Length == 0) return null;
            return d.TryRecipe(s.DefaultRecipe, out var r) ? r : null;
        }

        /// <summary>How many of item <paramref name="k"/> one craft needs, or 0.</summary>
        public static int Need(Recipe r, ItemId k)
        {
            if (r?.Inputs == null) return 0;
            for (var i = 0; i < r.Inputs.Count; i++) if (r.Inputs[i].Item == k) return r.Inputs[i].Count;
            return 0;
        }

        /// <summary>The item a craft puts out (reference flow.ts:203 <c>recipeOutput</c>; 'rounds' is a Magazine).</summary>
        public static ItemId OutputItem(Recipe r) => r != null && r.Outputs != null && r.Outputs.Count > 0 ? r.Outputs[0].Item : ItemId.Steel;

        /// <summary>
        /// Units per craft. U-D-08 makes one round one bullet, so the reference's <c>ammoVersion===1</c> branch is
        /// the port's only branch: the Shot magazine's craft yields its full <c>count</c> (ten), not one magazine
        /// (reference flow.ts:972 <c>r.output==='rounds'&amp;&amp;m.ammoVersion===1?r.count:recipeYield(r)</c>).
        /// </summary>
        public static int Yield(Recipe r) => r != null && r.Outputs != null && r.Outputs.Count > 0 ? r.Outputs[0].Count : 0;

        /// <summary>
        /// Reference flow.ts:953 output cap: ammunition stops at its <c>AmmoDef.OutputBuffer</c> (50 rounds, 5
        /// shells), anything else at <c>ASM_OUTPUT_CAP</c> (5), read here from <c>MachineSpec.OutputBufferCap</c>.
        /// </summary>
        public static double OutputCap(GameData d, Machine m, Recipe r)
        {
            var item = OutputItem(r);
            for (var i = 0; i < d.Ammunition.Count; i++)
                if (d.Ammunition[i].Item == item && d.Ammunition[i].OutputBuffer > 0) return d.Ammunition[i].OutputBuffer;
            return d.TryMachine(m.Kind, out var s) && s.OutputBufferCap > 0 ? s.OutputBufferCap : 5;
        }

        // ---- geometry ------------------------------------------------------------------------------------------

        /// <summary>Reference flow.ts:601 <c>outputTile</c>: the middle of the side the machine faces, just outside it.</summary>
        public static (int x, int y) OutputTile(Machine m)
        {
            var c = (m.Size - 1) / 2.0;
            var cx = m.X + c;
            var cy = m.Y + c;
            var r = (m.Size + 1) / 2.0;
            return ((int)Math.Round(cx + Dirs.DX[(int)m.Dir] * r, MidpointRounding.AwayFromZero),
                    (int)Math.Round(cy + Dirs.DY[(int)m.Dir] * r, MidpointRounding.AwayFromZero));
        }

        /// <summary>Reference flow.ts:606 <c>inputTile</c>: the opposite side.</summary>
        public static (int x, int y) InputTile(Machine m)
        {
            var c = (m.Size - 1) / 2.0;
            var cx = m.X + c;
            var cy = m.Y + c;
            var r = (m.Size + 1) / 2.0;
            return ((int)Math.Round(cx - Dirs.DX[(int)m.Dir] * r, MidpointRounding.AwayFromZero),
                    (int)Math.Round(cy - Dirs.DY[(int)m.Dir] * r, MidpointRounding.AwayFromZero));
        }

        /// <summary>Reference flow.ts:594 <c>machineAt</c>. The port has no occupancy grid yet, so this scans the
        /// machine list; the list is short and the scan happens once per miner per cycle.</summary>
        public static Machine MachineAt(SimState st, int x, int y)
        {
            for (var i = 0; i < st.Machines.Count; i++)
                if (st.Machines[i].Rect.Contains(x, y)) return st.Machines[i];
            return null;
        }
    }
}
