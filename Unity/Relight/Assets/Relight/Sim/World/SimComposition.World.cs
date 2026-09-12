using System.Collections.Generic;

namespace Relight.Sim
{
    /// <summary>
    /// The world subsystem's entries in the fixed composition (B-08). One phase, one initialiser, one handler —
    /// nothing is discovered by reflection.
    ///
    /// Order inside the tick: the reference runs the engineer's body first in <c>stepFlow</c>, before the machines,
    /// so what the player does with the keys this tick is what the rest of the tick sees. The world slot is already
    /// second in <c>SimComposition.BuildPhases</c> (after the core clock, before inventory), which preserves that.
    /// </summary>
    public static partial class SimComposition
    {
        static partial void AddWorldPhases(List<ITickPhase> list)
        {
            list.Add(new EngineerMovementPhase());
        }

        static partial void AddWorldInitializers(List<IStateInitializer> list)
        {
            list.Add(new EngineerSpawnInitializer());
        }

        static partial void AddWorldHandlers(List<ICommandHandler> list)
        {
            list.Add(new MovementCommandHandler());
        }
    }
}
