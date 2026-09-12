using System.Collections.Generic;

namespace Relight.Sim
{
    /// <summary>
    /// The fixed order of tick phases and command handlers (reference flow.ts stepFlow / sim.ts advance order,
    /// TECHNICAL_ARCHITECTURE.md §2.4). Owned by the core (B-03). Each subsystem registers its phase and handler
    /// here in its own <c>partial</c> file so the composition is explicit and reviewable; nothing is discovered by
    /// reflection. Phase C subsystems append their entries at the marked slots.
    /// </summary>
    public static partial class SimComposition
    {
        /// <summary>Tick phases in execution order for one 1/20 s tick.</summary>
        public static IReadOnlyList<ITickPhase> Phases => _phases ??= BuildPhases();
        /// <summary>New-game initialisers in execution order (each fills its own part of a fresh <see cref="SimState"/>).</summary>
        public static IReadOnlyList<IStateInitializer> Initializers => _inits ??= BuildInitializers();
        /// <summary>Command handlers in the order they are tried.</summary>
        public static IReadOnlyList<ICommandHandler> Handlers => _handlers ??= BuildHandlers();

        private static IReadOnlyList<ITickPhase> _phases;
        private static IReadOnlyList<IStateInitializer> _inits;
        private static IReadOnlyList<ICommandHandler> _handlers;

        private static IReadOnlyList<ITickPhase> BuildPhases()
        {
            var list = new List<ITickPhase>();
            AddCorePhases(list);        // B-03: campaign clock / hourly sampling
            AddWorldPhases(list);       // B-08: engineer movement on tiles
            AddInventoryPhases(list);   // B-06: hand mining / hand crafting
            // Phase C: production, power, combat, campaign — appended here in their own partial files.
            return list;
        }

        private static IReadOnlyList<IStateInitializer> BuildInitializers()
        {
            var list = new List<IStateInitializer>();
            AddCoreInitializers(list);       // B-03: seed, rng, clock
            AddWorldInitializers(list);      // B-08: ground grid, engineer spawn position
            AddInventoryInitializers(list);  // B-06: starting pockets, stats, ledger opening
            return list;
        }

        private static IReadOnlyList<ICommandHandler> BuildHandlers()
        {
            var list = new List<ICommandHandler>();
            AddCoreHandlers(list);
            AddWorldHandlers(list);
            AddInventoryHandlers(list);
            return list;
        }

        static partial void AddCorePhases(List<ITickPhase> list);
        static partial void AddWorldPhases(List<ITickPhase> list);
        static partial void AddInventoryPhases(List<ITickPhase> list);
        static partial void AddCoreInitializers(List<IStateInitializer> list);
        static partial void AddWorldInitializers(List<IStateInitializer> list);
        static partial void AddInventoryInitializers(List<IStateInitializer> list);
        static partial void AddCoreHandlers(List<ICommandHandler> list);
        static partial void AddWorldHandlers(List<ICommandHandler> list);
        static partial void AddInventoryHandlers(List<ICommandHandler> list);
    }
}
