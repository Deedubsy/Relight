using System.Collections.Generic;

namespace Relight.Sim
{
    /// <summary>
    /// Batch 4's Encounter sub-slot (FRT-02): one phase, after the enemies and before the director, so a garrison
    /// born this tick is counted by the director's world cap on the same tick and is first ticked on the next.
    /// </summary>
    public static partial class SimComposition
    {
        static partial void AddEncounterPhases(List<ITickPhase> list) => list.Add(new EncounterPhase());

        /// <summary>FRT-07: lifting and putting down a Power core. FRT-08: preparing and commissioning a plant.</summary>
        static partial void AddEncounterHandlers(List<ICommandHandler> list)
        {
            list.Add(new CoreCarryHandler());
            list.Add(new PlantHandler());
        }
    }
}
