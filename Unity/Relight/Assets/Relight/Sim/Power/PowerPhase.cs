using System;
using System.Collections.Generic;

namespace Relight.Sim
{
    /// <summary>
    /// B-09's tick: rebuild the grid, report supply that was lost or gained, then burn fuel in every generator that
    /// is actually delivering power.
    ///
    /// Order matches the reference: <c>stepFlow</c> calls <c>campaignGrid</c> (flow.ts:1064) before the machine loop,
    /// and the generator's burn is the machine loop's generator branch (flow.ts:1045-1057). Because the grid is
    /// rebuilt from the machine list at the top of the tick, a generator that empties during this tick still counts
    /// as supplying for this tick and drops out on the next — the reference's behaviour exactly.
    /// </summary>
    public sealed class PowerPhase : ITickPhase
    {
        /// <summary>Reference flow.ts:24 <c>EPS</c>.</summary>
        public const double Eps = 1e-9;

        public void Tick(SimContext ctx, SimState st, double dt)
        {
            st.Power.Prune(st);
            var grid = PowerGrid.Of(ctx, st);
            ReportChanges(ctx, st, grid);
            Burn(ctx, st, grid, dt);
        }

        /// <summary>
        /// One <see cref="PowerOutageEvent"/> / <see cref="PowerRestoredEvent"/> per machine whose supply changed,
        /// then the new supplied set is persisted so a save resumes without a spurious outage.
        /// </summary>
        private static void ReportChanges(SimContext ctx, SimState st, PowerNetwork grid)
        {
            var was = st.Power.Supplied;
            var now = new List<int>();
            for (var i = 0; i < st.Machines.Count; i++)
            {
                var m = st.Machines[i];
                if (PowerGrid.DemandKw(ctx.Data, m) <= 0) continue;   // belts, poles, chests: never "unpowered"
                // GP-W5: a wreck is not an outage. It leaves the supplied set silently, so the player is told
                // "the assembler was destroyed" by the thing that destroyed it rather than being handed a
                // power-failure notice about a machine that has no power problem at all.
                if (TurretRules.Wrecked(ctx.Data, st, m)) continue;
                var c = grid.Of(m.Id);
                var supplied = c != null && c.Throttle > 0;
                var had = Array.BinarySearch(was, m.Id) >= 0;
                if (supplied) now.Add(m.Id);
                if (supplied && !had) st.Events.Add(new PowerRestoredEvent(st.T, m.Id));
                else if (!supplied && had) st.Events.Add(new PowerOutageEvent(st.T, m.Id));
            }
            // st.Machines is kept in placement order, which is ascending id, so `now` is already sorted.
            if (!Same(was, now)) st.Power.Supplied = now.ToArray();
        }

        private static bool Same(int[] a, List<int> b)
        {
            if (a.Length != b.Count) return false;
            for (var i = 0; i < a.Length; i++) if (a[i] != b[i]) return false;
            return true;
        }

        /// <summary>
        /// Reference flow.ts:1045-1057, ported literally:
        /// <code>
        /// const shareKw = grid.generation.get(m.id) ?? 0;
        /// if (shareKw &lt;= 0 || (m.inv.coal ?? 0) + (m.inv.fuel ?? 0) &lt;= 0) { m.busy = false; return; }
        /// m.busy = true;
        /// m.timer += (shareKw / (COAL_MJ * 1000)) * dt;
        /// while (m.timer &gt;= 1 - EPS) {
        ///   m.timer -= 1;
        ///   if ((m.inv.coal ?? 0) &gt; 0) { m.inv.coal--; st.stats.coalBurned++; }
        ///   else { m.inv.fuel--; consumed.fuel = (consumed.fuel ?? 0) + 1; }
        ///   if ((m.inv.coal ?? 0) + (m.inv.fuel ?? 0) &lt;= 0) { m.timer = 0; m.busy = false; push('gen-dry'); break; }
        /// }
        /// </code>
        /// <c>shareKw / (CoalMj * 1000)</c> is units of fuel per second: one coal is CoalMj = 4 MJ = 4000 kJ, and a
        /// kW delivered for a second is a kJ. A lone Generator at its full 300 kW therefore takes 4000/300 = 13.33 s
        /// per coal, and a generator carrying only part of its circuit's load burns proportionally slower.
        /// </summary>
        private static void Burn(SimContext ctx, SimState st, PowerNetwork grid, double dt)
        {
            var coalMj = ctx.Data.Power != null && ctx.Data.Power.CoalMj > 0 ? ctx.Data.Power.CoalMj : 4;
            for (var i = 0; i < st.Machines.Count; i++)
            {
                var m = st.Machines[i];
                if (!PowerGrid.IsSource(ctx.Data, m)) continue;
                var burn = st.Power.BurnOf(m.Id);
                var shareKw = grid.Share(m.Id);
                if (shareKw <= 0 || PowerGrid.FuelUnits(m) <= 0) { burn.Busy = false; continue; }
                burn.Busy = true;
                burn.Timer += shareKw / (coalMj * 1000) * dt;
                while (burn.Timer >= 1 - Eps)
                {
                    burn.Timer -= 1;
                    if (m.Inv[ItemId.Coal] > 0)
                    {
                        m.Inv.Add(ItemId.Coal, -1);
                        st.Stats.CoalBurned++;
                    }
                    else
                    {
                        m.Inv.Add(ItemId.Fuel, -1);
                        st.Stats.Consumed.Add(ItemId.Fuel, 1);
                    }
                    if (PowerGrid.FuelUnits(m) <= 0)
                    {
                        burn.Timer = 0;
                        burn.Busy = false;
                        st.Events.Add(new GeneratorDryEvent(st.T, m.Id));
                        break;
                    }
                }
            }
        }
    }
}
