using System.Collections.Generic;
using NUnit.Framework;

namespace Relight.Sim.Tests.Production
{
    /// <summary>
    /// The fixed composition (Phase C contract): Core → World → Inventory → Production(Power, Machines, Flow) →
    /// Combat → Campaign, each sub-slot filled exactly once.
    ///
    /// The order inside Production is the reference <c>stepFlow</c>'s, matched by B-07/B-09:
    /// <list type="number">
    /// <item><c>campaignGrid(st)</c> is read at the top of the machine loop (flow.ts:1064) — the port builds it in
    ///       <see cref="PowerPhase"/> and burns the generators' fuel there (flow.ts:1045-1057), because the burn IS
    ///       the generator branch of that loop and needs the grid it just built.</item>
    /// <item>the drawing machines are ticked at <c>dt × machineThrottle(st, m)</c> (flow.ts:1099) — the port's
    ///       <see cref="MachinePhase"/>.</item>
    /// <item>belts, inserters and the routing pass run after them (flow.ts:1103 onwards) — B-10's Flow phase, next
    ///       wave, which is why its slot is last of the three.</item>
    /// </list>
    /// Hand crafting (flow.ts:1007 <c>tickHand</c>) runs before all of it, which the Inventory slot already
    /// guarantees by standing before Production.
    /// </summary>
    public sealed class CompositionOrderTests
    {
        private static int IndexOf<T>(IReadOnlyList<ITickPhase> phases)
        {
            for (var i = 0; i < phases.Count; i++) if (phases[i] is T) return i;
            return -1;
        }

        private static int CountOf<T>(IReadOnlyList<ITickPhase> phases)
        {
            var n = 0;
            for (var i = 0; i < phases.Count; i++) if (phases[i] is T) n++;
            return n;
        }

        [Test]
        public void PowerRunsBeforeMachinesAndBothRunAfterHandCrafting()
        {
            var phases = SimComposition.Phases;
            var hand = IndexOf<HandCraftPhase>(phases);
            var power = IndexOf<PowerPhase>(phases);
            var machines = IndexOf<MachinePhase>(phases);

            Assert.That(hand, Is.GreaterThanOrEqualTo(0), "the Inventory slot is filled");
            Assert.That(power, Is.GreaterThan(hand), "Production stands after Inventory");
            Assert.That(machines, Is.GreaterThan(power), "the grid is built before the machines that read its throttle");
        }

        [Test]
        public void EachProductionPhaseIsRegisteredExactlyOnce()
        {
            var phases = SimComposition.Phases;
            Assert.That(CountOf<PowerPhase>(phases), Is.EqualTo(1));
            Assert.That(CountOf<MachinePhase>(phases), Is.EqualTo(1));
        }

        [Test]
        public void TheRecipeHandlerIsRegisteredExactlyOnce()
        {
            var n = 0;
            for (var i = 0; i < SimComposition.Handlers.Count; i++)
                if (SimComposition.Handlers[i] is SetRecipeHandler) n++;
            Assert.That(n, Is.EqualTo(1));
        }

        /// <summary>
        /// A fresh state means "nothing has happened yet" (contract rule 2): no burn progress, nothing recorded as
        /// supplied and no machine work — so the first tick of a new game, or of an upgraded Phase B save, cannot
        /// report an outage or resume a craft nobody started.
        /// </summary>
        [Test]
        public void AFreshStateHasNoPowerOrProductionHistory()
        {
            var st = new SimState();
            Assert.That(st.Power.Burn, Is.Empty);
            Assert.That(st.Power.Supplied, Is.Empty);
            Assert.That(st.Production.Work, Is.Empty);
        }
    }
}
