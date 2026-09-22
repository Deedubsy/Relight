using System.Collections.Generic;

namespace Relight.Sim
{
    /// <summary>
    /// C-11's entries in the fixed composition (Campaign slot, after Opening). The light phase runs last in the
    /// campaign slot because it only reads: it derives the mask from the machines, sites and power network that the
    /// earlier phases have already settled this tick.
    /// </summary>
    public static partial class SimComposition
    {
        static partial void AddLightPhases(List<ITickPhase> list) => list.Add(new LightPhase());
        /// <summary>REL-9: opens the Combat slot (<see cref="LightMaskPhase"/>).</summary>
        static partial void AddLightMaskPhases(List<ITickPhase> list) => list.Add(new LightMaskPhase());

        static partial void AddLightInitializers(List<IStateInitializer> list) => list.Add(new LightInitializer());
    }
}
