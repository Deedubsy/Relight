using System;
using System.Collections.Generic;
using System.Globalization;

namespace Relight.Sim
{
    /// <summary>
    /// Where the ledger counts an item (reference ledger.ts `LedgerPlace`).
    /// Phase B fills <see cref="Pockets"/> and <see cref="Machines"/> only.
    /// <see cref="Belts"/> (belt contents, an inserter's hand, a tram's cargo) and <see cref="Committed"/>
    /// (materials delivered to a restoration site and not yet activated) are Phase C and are always zero here.
    /// The reference's `chest` (the Depot's abstract stock) and `buffer` (the line buffer) places, and its `ring`
    /// place, belong to the retired block/lattice economy (U-D-32) and are not ported: the physical supply chest
    /// is a machine and counts under <see cref="Machines"/>.
    /// </summary>
    public enum LedgerPlace
    {
        Pockets = 0,
        Machines = 1,
        Belts = 2,
        Committed = 3,
    }

    /// <summary>
    /// RI-01 / TA §2.7 / U-D-05 conservation: every item is counted where it is now (`held`) against what entered
    /// the game (`sources`) and left it (`sinks`) since the ledger opened.
    /// <c>unexplained[k] = held[k] + sinks[k] − sources[k] − opening[k]</c>; zero means every unit is explained.
    /// Ported from reference ledger.ts (`heldItems`, `ledgerFlows`, `openLedger`, `conservation`).
    /// One round is one bullet (U-D-08), so the reference's ROUNDS_PER_MAG divisions collapse to 1.
    /// </summary>
    public static class Ledger
    {
        public const int PlaceCount = 4;
        /// <summary>Reference flow.ts REPAIR_COPPER.</summary>
        public const double RepairCopper = 1;

        /// <summary>Reference `heldItems`: every item where it stands now, by place and in total.</summary>
        public static void Held(SimState st, GameData d, ItemCounts[] where, ItemCounts total)
        {
            for (var p = 0; p < PlaceCount; p++) where[p].Clear();
            total.Clear();

            var pockets = where[(int)LedgerPlace.Pockets];
            var inv = st.Engineer.Inv;
            for (var i = 0; i < Items.Count; i++)
            {
                var n = inv[(ItemId)i];
                if (n > 0) pockets.Add((ItemId)i, n);
            }
            // Phase C equipment: a holstered weapon's `loaded` rounds count as magazines in the pockets.
            // Phase B has no equipment, so that term is fixed at zero.

            // A cancelled hand-craft batch's plates that the Backpack could not take back yet are still the
            // engineer's: they are owed to the pockets and HandCraft.Drain pays them in as space appears, so they
            // are counted here rather than left as a sink. No new LedgerPlace — the four slots are unchanged.
            var hand = st.Hand;
            if (hand != null)
            {
                if (hand.RefundSteel > 0) pockets.Add(ItemId.Steel, hand.RefundSteel);
                if (hand.RefundCopper > 0) pockets.Add(ItemId.Copper, hand.RefundCopper);
            }

            var machines = where[(int)LedgerPlace.Machines];
            for (var mi = 0; mi < st.Machines.Count; mi++)
            {
                var m = st.Machines[mi];
                if (string.Equals(m.Kind, "turret", StringComparison.Ordinal))
                {
                    if (m.Rounds > 0) machines.Add(ItemId.Magazine, m.Rounds);
                    continue;
                }
                for (var i = 0; i < Items.Count; i++)
                {
                    var n = m.Inv[(ItemId)i];
                    if (n > 0) machines.Add((ItemId)i, n);
                }
                // Reference: a processor's finished output is its recipe's output item, anything else is magazines.
                // Phase B machines carry no recipe (production is Phase C), so `out` counts as magazines throughout.
                if (m.Out > 0) machines.Add(ItemId.Magazine, m.Out);
            }

            for (var p = 0; p < PlaceCount; p++)
                for (var i = 0; i < Items.Count; i++) total.Add((ItemId)i, where[p][(ItemId)i]);
        }

        /// <summary>Reference `ledgerFlows`: what the counters say entered and left the game.</summary>
        public static void Flows(SimState st, ItemCounts sources, ItemCounts sinks)
        {
            sources.Clear();
            sinks.Clear();
            var s = st.Stats;
            for (var i = 0; i < Items.Count; i++)
            {
                var k = (ItemId)i;
                sources.Add(k, s.MinedOf[k] + s.Made[k]);
                sinks.Add(k, s.Consumed[k] + s.Placed[k]);
            }
            sinks.Add(ItemId.Steel, s.SpentSteel);
            sinks.Add(ItemId.Copper, s.SpentCopper + s.Repairs * RepairCopper);
            sinks.Add(ItemId.Coal, s.CoalBurned);
            sinks.Add(ItemId.Magazine, s.Fired + s.EngineerFired + s.RoundsLost);
        }

        /// <summary>Reference `openLedger`: held − net now, so <see cref="Conservation"/> reads zero from here on.</summary>
        public static LedgerOpening Open(SimState st, GameData d)
        {
            var where = NewPlaces();
            var total = new ItemCounts();
            var sources = new ItemCounts();
            var sinks = new ItemCounts();
            Held(st, d, where, total);
            Flows(st, sources, sinks);
            var opening = new LedgerOpening { Tick = st.Tick };
            for (var i = 0; i < Items.Count; i++)
            {
                var k = (ItemId)i;
                opening.Base[k] = total[k] - (sources[k] - sinks[k]);
            }
            return opening;
        }

        /// <summary>Reference `conservation`. `tolerance` absorbs floating-point drift (a hundredth of an item).</summary>
        public static LedgerView Conservation(SimState st, GameData d, double tolerance = 0.01)
        {
            var where = NewPlaces();
            var held = new ItemCounts();
            var sources = new ItemCounts();
            var sinks = new ItemCounts();
            Held(st, d, where, held);
            Flows(st, sources, sinks);
            var opening = st.Ledger?.Base?.Clone() ?? new ItemCounts();
            var openedAt = st.Ledger?.Tick ?? 0;
            var unexplained = new ItemCounts();
            var problems = new List<string>();
            var inv = CultureInfo.InvariantCulture;
            for (var i = 0; i < Items.Count; i++)
            {
                var k = (ItemId)i;
                var v = held[k] + sinks[k] - sources[k] - opening[k];
                unexplained[k] = v;
                if (Math.Abs(v) <= tolerance) continue;
                problems.Add($"{Items.Key(k)}: {(v > 0 ? "+" : "")}{v.ToString("F2", inv)} unexplained " +
                             $"(opening {opening[k].ToString("F1", inv)} + sources {sources[k].ToString("F1", inv)} = " +
                             $"held {held[k].ToString("F1", inv)} + sinks {sinks[k].ToString("F1", inv)})");
            }
            return new LedgerView(problems.Count == 0, tolerance, openedAt, opening, sources, sinks, held, where, unexplained, problems);
        }

        public static ItemCounts[] NewPlaces()
        {
            var a = new ItemCounts[PlaceCount];
            for (var i = 0; i < PlaceCount; i++) a[i] = new ItemCounts();
            return a;
        }
    }
}
