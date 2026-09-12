using System.Collections.Generic;

namespace Relight.Sim
{
    /// <summary>Core's entries in the fixed composition (B-03).</summary>
    public static partial class SimComposition
    {
        static partial void AddCorePhases(List<ITickPhase> list)
        {
            list.Add(new SecondPhase());
        }

        static partial void AddCoreInitializers(List<IStateInitializer> list)
        {
            // Nothing. Simulation.NewGame sets the header fields (seed, rng, t, tick) the reference sets outside any
            // subsystem; everything else belongs to the subsystem that owns it.
        }

        static partial void AddCoreHandlers(List<ICommandHandler> list)
        {
            // Nothing. The core owns no commands: the reference's four inline cases in applyCommands are `claim`,
            // `ringOrder` and `addAssembler` (retired block economy, CONTENT_CATALOGUE.md §17) and `setSpeed`
            // (retired for players, U-D-04).
        }
    }
}
