using System;

namespace Relight.Sim
{
    /// <summary>
    /// B-07: the machine loop of reference <c>stepFlow</c> (flow.ts:1085-1101).
    ///
    /// The reference's loop, in its own order: cannon skipped, turret timer counted down, generator burned, tram
    /// moved, then every remaining drawing machine ticked at <c>dt × machineThrottle(st, m)</c> — inserter,
    /// excavator/pumpjack, else assembler. This phase runs the excavator/pumpjack and the processors; the generator
    /// is <see cref="PowerPhase"/>'s (it needs the grid it just built), the turret's timer is C-04's, the inserter,
    /// the belts and the routing machines are B-10's, and the tram and the cannon are not ported.
    /// Chests, poles, lamps and the Depot are passive here exactly as they are in the reference.
    /// </summary>
    public sealed class MachinePhase : ITickPhase
    {
        public void Tick(SimContext ctx, SimState st, double dt)
        {
            st.Production.Prune(st);
            var d = ctx.Data;
            for (var i = 0; i < st.Machines.Count; i++)
            {
                var m = st.Machines[i];
                var miner = ProductionRules.IsMiner(d, m);
                var processor = ProductionRules.IsProcessor(d, m);
                if (!miner && !processor) continue;

                // Reference flow.ts:630 `powered` and :1099 `dt * machineThrottle(st, m)`: a machine short of supply
                // does not stop, its clock runs slower; with no supply at all the clock stops.
                var throttle = PowerQueries.Throttle(ctx, st, m.Id);
                var work = st.Production.Of(m.Id);
                if (throttle <= 0) { work.Stall = (int)MachineOperatingState.Unpowered; continue; }
                var localDt = dt * throttle;
                if (miner) TickMiner(ctx, st, m, work, localDt);
                else TickProcessor(ctx, st, m, work, localDt);
            }
        }

        // ------------------------------------------------------------------ excavator / pumpjack

        /// <summary>
        /// Reference flow.ts:936-949 <c>tickExcavator</c>:
        /// <code>
        /// if (m.hold) { const t = machineAt(st, ...outputTile(m)); if (t &amp;&amp; giveItem(st, t, m.hold, 0)) m.hold = null; else return; }
        /// const cycle = 1 / (EXCAVATOR_PER_S * processingMultiplier(m));
        /// m.timer += dt;
        /// if (m.timer &lt; cycle - EPS) return;
        /// const r = findRubble(st, m);
        /// if (!r) { m.timer = cycle; return; }
        /// m.timer -= cycle; m.hold = mineUnit(st, r);
        /// </code>
        /// One extracted item waits in the ordinary machine inventory, so it is visible, transferable,
        /// conserved and saved. Conveyors may pull it from an adjacent edge; the facing port also pushes.
        /// </summary>
        private static void TickMiner(SimContext ctx, SimState st, Machine m, MachineWork work, double dt)
        {
            var d = ctx.Data;
            var rate = d.TryMachine(m.Kind, out var spec) && spec.RatePerS > 0 ? spec.RatePerS : 0.5;
            var cycle = 1 / (rate * ProductionRules.SpeedMul(d, m));
            // Empty the visible one-item buffer before beginning the next dig cycle.
            for (var i = 0; i < Items.Count; i++)
            {
                var k = (ItemId)i;
                if (m.Inv[k] < 1) continue;
                var (ox, oy) = ProductionRules.OutputTile(m);
                var target = ProductionRules.MachineAt(st, ox, oy);
                if (target == null || !GiveItem(ctx, st, target, k))
                {
                    work.Stall = (int)MachineOperatingState.OutputFull;
                    return;
                }
                m.Inv.Add(k, -1);
            }
            work.Timer += dt;
            if (work.Timer < cycle - ProductionRules.Eps) { work.Stall = (int)MachineOperatingState.Running; return; }
            if (!TryFindRubble(ctx, st, m, out var x, out var y, out var item, out var units))
            {
                work.Timer = cycle;
                work.Stall = (int)MachineOperatingState.NoInput;
                return;
            }
            work.Timer -= cycle;
            Mining.TakeUnit(ctx, st, x, y, item, units, m.Id);
            m.Inv.Add(item, 1);
            work.Stall = (int)MachineOperatingState.Running;
        }

        /// <summary>
        /// Reference flow.ts:929-935 <c>findRubble</c>: the first diggable tile in the ring one tile around the
        /// footprint, scanned north-to-south then west-to-east. A Pumpjack takes only crude; anything else takes
        /// everything but crude.
        /// </summary>
        public static bool TryFindRubble(SimContext ctx, SimState st, Machine m, out int fx, out int fy, out ItemId item, out double units)
        {
            var pump = string.Equals(m.Kind, "pumpjack", StringComparison.Ordinal);
            var (w, h) = m.Dimensions;
            for (var ty = m.Y - 1; ty <= m.Y + h; ty++)
                for (var tx = m.X - 1; tx <= m.X + w; tx++)
                {
                    if (!Mining.TryTile(ctx, st, tx, ty, out var k, out var n)) continue;
                    if (pump != (k == ItemId.Crude)) continue;
                    fx = tx; fy = ty; item = k; units = n;
                    return true;
                }
            fx = 0; fy = 0; item = ItemId.Steel; units = 0;
            return false;
        }

        /// <summary>
        /// Reference flow.ts:798 <c>giveItem</c>. The Depot's abstract delivery is retired (U-D-32); B-10 added the
        /// conveyor destinations, which the reference answers in the same function and this port answers in
        /// <see cref="FlowRules.GiveItem"/>. The delegation is kind-guarded on both sides (this one only for a
        /// conveyor, that one only for a non-conveyor), so the pair cannot recurse.
        /// </summary>
        public static bool GiveItem(SimContext ctx, SimState st, Machine target, ItemId item)
        {
            if (FlowRules.IsConveyor(target.Kind)) return FlowRules.GiveItem(ctx, st, target, item, 0);
            if (!MachineInventory.Accepts(ctx.Data, st, target, item)) return false;
            if (TurretHopper.IsTurret(ctx.Data, target))               // any hopper kind: a cannon eats shells the same way
            {
                target.Rounds += 1;
                st.Stats.TurretFed++;
                return true;
            }
            target.Inv.Add(item, 1);
            if (string.Equals(target.Kind, "generator", StringComparison.Ordinal)) st.Stats.GenFed++;
            return true;
        }

        // ------------------------------------------------------------------ foundry / refinery / assembler / mixer

        /// <summary>
        /// Reference flow.ts:951-956 <c>asmCanStart</c>: room in the output buffer and a full set of inputs.
        /// </summary>
        public static bool CanStart(GameData d, Machine m, Recipe r)
        {
            if (r == null) return false;
            if (m.Inv[ProductionRules.OutputItem(r)] >= ProductionRules.OutputCap(d, m, r)) return false;
            for (var i = 0; i < r.Inputs.Count; i++)
                if (m.Inv[r.Inputs[i].Item] < r.Inputs[i].Count) return false;
            return true;
        }

        /// <summary>Why a processor that cannot start is stopped: a full output buffer, else a missing input.</summary>
        private static MachineOperatingState StallReason(GameData d, Machine m, Recipe r) =>
            m.Inv[ProductionRules.OutputItem(r)] >= ProductionRules.OutputCap(d, m, r)
                ? MachineOperatingState.OutputFull
                : MachineOperatingState.NoInput;

        /// <summary>Reference flow.ts:957-960 <c>asmStart</c>: the inputs leave the machine and are counted as consumed.</summary>
        private static void Start(SimState st, Machine m, Recipe r, MachineWork work)
        {
            for (var i = 0; i < r.Inputs.Count; i++)
            {
                m.Inv.Add(r.Inputs[i].Item, -r.Inputs[i].Count);
                st.Stats.Consumed.Add(r.Inputs[i].Item, r.Inputs[i].Count);
            }
            work.Busy = true;
        }

        /// <summary>
        /// Reference flow.ts:961-978 <c>tickAssembler</c>:
        /// <code>
        /// if (!m.busy) { if (!asmCanStart(m)) return; asmStart(st, m); m.timer = 0; }
        /// m.timer += dt * processingMultiplier(m);
        /// if (m.timer &lt; r.seconds - EPS) return;
        /// const n = ..., o = recipeOutput(r);
        /// m.out += n; m.busy = false; stats.made[o] += n;
        /// if (o === 'magazine') stats.magsMade += n;
        /// const rem = m.timer - r.seconds;
        /// if (asmCanStart(m)) { asmStart(st, m); m.timer = rem; } else m.timer = 0;
        /// </code>
        /// Deliberate difference: the finished craft lands in <c>Machine.Inv[output]</c> rather than the reference's
        /// single untyped <c>m.out</c> slot. <c>Out</c> is read as magazines by the ledger and by pick-up
        /// (Ledger.cs, Placement.cs — other workers' files), which would mis-count a Foundry's steel; no recipe has
        /// its own output among its inputs, so an inventory slot is an exact substitute and the ledger stays
        /// truthful. The output cap is enforced against the same slot.
        /// The alien workbench's decode branch and the Mk2's overclock artifact multiplier are campaign state
        /// (C-05/C-09) and are not ported here.
        /// </summary>
        private static void TickProcessor(SimContext ctx, SimState st, Machine m, MachineWork work, double dt)
        {
            var d = ctx.Data;
            var r = ProductionRules.RecipeOf(d, st, m);
            if (r == null) { work.Stall = (int)MachineOperatingState.Idle; return; }

            if (!work.Busy)
            {
                if (!CanStart(d, m, r)) { work.Stall = (int)StallReason(d, m, r); return; }
                Start(st, m, r, work);
                work.Timer = 0;
            }

            work.Stall = (int)MachineOperatingState.Running;
            work.Timer += dt * ProductionRules.SpeedMul(d, m);
            if (work.Timer < r.Seconds - ProductionRules.Eps) return;

            var n = ProductionRules.Yield(r);
            var o = ProductionRules.OutputItem(r);
            m.Inv.Add(o, n);
            work.Busy = false;
            st.Stats.Made.Add(o, n);
            if (o == ItemId.Magazine) st.Stats.MagsMade += n;
            st.Events.Add(new MachineProducedEvent(st.T, m.Id, o, n));

            var rem = work.Timer - r.Seconds;
            if (CanStart(d, m, r)) { Start(st, m, r, work); work.Timer = rem; }
            else { work.Timer = 0; work.Stall = (int)StallReason(d, m, r); }
        }
    }

    /// <summary>A processor finished a craft (the reference's <c>observeOutput</c> hook, flow.ts:974).</summary>
    public sealed record MachineProducedEvent(double T, int MachineId, ItemId Item, int Count) : SimEvent(T);
}
