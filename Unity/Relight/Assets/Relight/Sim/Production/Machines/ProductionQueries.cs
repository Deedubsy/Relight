using System;
using System.Collections.Generic;

namespace Relight.Sim
{
    /// <summary>
    /// What a machine is doing, for the HUD and the machine panel (C-07) and for C-04's turret rule.
    /// <see cref="Idle"/> is first so that the default of a freshly created side-table entry — and of a Phase B save
    /// upgraded to v3, where nothing has run yet — reads as "idle", never as "running".
    /// </summary>
    public enum MachineOperatingState
    {
        /// <summary>Nothing to do, or a machine that never processes (a chest, a pole, the Depot).</summary>
        Idle = 0,
        /// <summary>Working at full speed.</summary>
        Running = 1,
        /// <summary>Working, but its clock runs at the circuit's throttle (reference D-B3-4).</summary>
        Throttled = 2,
        /// <summary>Draws power and has none: no pole in reach, or no generator burning on its circuit.</summary>
        Unpowered = 3,
        /// <summary>A generator with no coal and no refined fuel left.</summary>
        OutOfFuel = 4,
        /// <summary>The output buffer is full; it will start again when the output is taken away.</summary>
        OutputFull = 5,
        /// <summary>Missing an input — or, for an excavator, nothing left to dig in reach.</summary>
        NoInput = 6,
        /// <summary>
        /// A defence knocked out: its hit points are 0, so it holds fire until it is repaired (C-04). Added last so
        /// every saved numeric value keeps its meaning.
        /// </summary>
        Disabled = 7,
    }

    /// <summary>A machine's status line: the state, its player-facing text and how far the current job has got.</summary>
    public readonly struct MachineStatus
    {
        public readonly MachineOperatingState State;
        /// <summary>Short, lower-case, player-facing (the machine panel puts it under the machine's name).</summary>
        public readonly string Text;
        /// <summary>0..1 through the current craft or dig cycle; 0 when nothing is in progress.</summary>
        public readonly double Progress;
        /// <summary>The recipe key the machine is set to, or "" (a miner, a chest).</summary>
        public readonly string Recipe;
        /// <summary>The circuit's throttle, 1 for a machine that draws nothing.</summary>
        public readonly double Throttle;

        public MachineStatus(MachineOperatingState state, string text, double progress, string recipe, double throttle)
        {
            State = state; Text = text; Progress = progress; Recipe = recipe; Throttle = throttle;
        }
    }

    /// <summary>Read-only production questions for presentation and other subsystems.</summary>
    public static class ProductionQueries
    {
        /// <summary>The player-facing line for a state.</summary>
        public static string StateText(MachineOperatingState s)
        {
            switch (s)
            {
                case MachineOperatingState.Running: return "running";
                case MachineOperatingState.Throttled: return "running slowly — not enough power";
                case MachineOperatingState.Unpowered: return "no power";
                case MachineOperatingState.OutOfFuel: return "out of fuel";
                case MachineOperatingState.OutputFull: return "output full";
                case MachineOperatingState.NoInput: return "waiting for materials";
                case MachineOperatingState.Disabled: return "disabled (0 hp)";
                default: return "idle";
            }
        }

        /// <summary>
        /// What the machine is doing right now. Power is read live from the grid; the reason a processor or a miner
        /// is stopped is the one its last tick recorded (<see cref="MachineWork.Stall"/>), so the answer is exactly
        /// what the machine loop decided rather than a second, possibly disagreeing, copy of the rules.
        /// </summary>
        public static MachineOperatingState OperatingState(SimContext ctx, SimState st, int id)
        {
            var d = ctx.Data;
            var m = st.MachineById(id);
            if (m == null) return MachineOperatingState.Idle;

            // GP-W5: a wreck answers first, for every kind. Since CombatBalance gave every buildable machine an
            // integrity row, an assembler CAN now be broken, and a broken one is not "waiting for materials" —
            // it is down until it is repaired. This is the same answer PowerQueries.Throttle gives the loops.
            if (TurretRules.Wrecked(d, st, m)) return MachineOperatingState.Disabled;

            if (PowerGrid.IsSource(d, m))
            {
                if (PowerGrid.FuelUnits(m) <= 0) return MachineOperatingState.OutOfFuel;
                return PowerQueries.GeneratorShareKw(ctx, st, id) > 0 ? MachineOperatingState.Running : MachineOperatingState.Idle;
            }

            // C-04: a turret is not a processor, but it has the same three answers a player needs — knocked out,
            // no power, or standing ready. Ammunition is shown separately (the hopper gauge), so an empty but
            // powered turret still reads as "running": it is working, it simply has nothing to fire.
            //
            // GP-W5 narrowed the gate from IsDefence to IsTurret. IsDefence is now true of every buildable
            // machine, so this branch would have swallowed every assembler and miner and answered "idle"; a wall,
            // which has hit points and nothing else, still falls through to the `processes` test below and reads
            // as idle there, exactly as it did before.
            if (TurretHopper.IsTurret(d, m))
            {
                var power = PowerQueries.Throttle(ctx, st, id);
                if (power <= 0) return MachineOperatingState.Unpowered;
                return power < 1 ? MachineOperatingState.Throttled : MachineOperatingState.Running;
            }

            var processes = ProductionRules.IsProcessor(d, m) || ProductionRules.IsMiner(d, m);
            if (!processes) return MachineOperatingState.Idle;

            var throttle = PowerQueries.Throttle(ctx, st, id);
            if (throttle <= 0) return MachineOperatingState.Unpowered;

            var stall = (MachineOperatingState)(st.Production.Find(id)?.Stall ?? 0);
            if (stall == MachineOperatingState.OutputFull || stall == MachineOperatingState.NoInput) return stall;
            if (stall == MachineOperatingState.Idle) return MachineOperatingState.Idle;
            return throttle < 1 ? MachineOperatingState.Throttled : MachineOperatingState.Running;
        }

        /// <summary>The state, its text, the progress bar and the recipe in one call.</summary>
        public static MachineStatus Status(SimContext ctx, SimState st, int id)
        {
            var s = OperatingState(ctx, st, id);
            var m = st.MachineById(id);
            var throttle = m == null ? 0 : PowerQueries.Throttle(ctx, st, id);
            var work = st.Production.Find(id);
            double progress = 0;
            var recipe = "";
            if (m != null && ProductionRules.IsProcessor(ctx.Data, m))
            {
                var r = ProductionRules.RecipeOf(ctx.Data, st, m);
                if (r != null)
                {
                    recipe = r.Key;
                    if (work != null && work.Busy && r.Seconds > 0) progress = Math.Min(1, work.Timer / r.Seconds);
                }
            }
            else if (m != null && ProductionRules.IsMiner(ctx.Data, m))
            {
                var rate = ctx.Data.TryMachine(m.Kind, out var spec) && spec.RatePerS > 0 ? spec.RatePerS : 0.5;
                var cycle = 1 / (rate * ProductionRules.SpeedMul(ctx.Data, m));
                if (work != null && cycle > 0) progress = Math.Min(1, work.Timer / cycle);
            }
            return new MachineStatus(s, StateText(s), progress, recipe, throttle);
        }

        /// <summary>Actual operation, power and output, for both inspection and world hover.</summary>
        public static string Description(SimContext ctx, SimState st, int id)
        {
            var m = st.MachineById(id);
            if (m == null) return "";
            var power = PowerQueries.Description(ctx, st, id);
            string text;
            if (FlowRules.IsConveyor(m.Kind))
            {
                text = FlowQueries.Description(ctx, st, m) + " · " + FlowQueries.Status(ctx, st, m) + "\n" + power;
            }
            else
            {
                text = Status(ctx, st, id).Text + " · " + power;
                if (ProductionRules.IsMiner(ctx.Data, m))
                {
                    var output = "empty";
                    for (var i = 0; i < Items.Count; i++)
                        if (m.Inv[(ItemId)i] >= 1) { output = $"{m.Inv[(ItemId)i]:0} {ctx.Data.Item((ItemId)i).DisplayName} / 1"; break; }
                    text += "\nOutput: " + output + " · adjacent conveyor pulls away; arrow marks direct output";
                }
            }
            // GP-W5: a damaged or destroyed machine says so here, with its price, for EVERY kind — a wrecked
            // conveyor is as repairable as a wrecked turret and the two read the same. The state word above is
            // still StateText's; this is only the repair half (HomeQueries.MachineRepairLine).
            var repair = HomeQueries.MachineRepairLine(ctx, st, id);
            if (repair.Length != 0) text += "\n" + repair;
            return text;
        }

        /// <summary>The recipe a machine runs (null for a machine that runs none).</summary>
        public static Recipe Recipe(SimContext ctx, SimState st, int id)
        {
            var m = st.MachineById(id);
            return m == null ? null : ProductionRules.RecipeOf(ctx.Data, st, m);
        }

        /// <summary>The recipes a machine may be set to, in catalogue order (reference flow.ts:151).</summary>
        public static IReadOnlyList<Recipe> Choices(SimContext ctx, SimState st, int id)
        {
            var list = new List<Recipe>();
            var m = st.MachineById(id);
            if (m != null) ProductionRules.RecipesFor(ctx.Data, m, list);
            return list;
        }
    }
}
