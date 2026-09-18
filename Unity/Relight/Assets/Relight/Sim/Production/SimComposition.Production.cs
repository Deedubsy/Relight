using System.Collections.Generic;

namespace Relight.Sim
{
    /// <summary>
    /// Production's entries in the fixed composition (Phase C, Wave 0 contract). The three sub-slots are implemented
    /// in their own files: <c>Power</c> by B-09 (Sim/Power/), <c>Machines</c> by B-07 (Sim/Production/Machines/),
    /// <c>Flow</c> by B-10 (Sim/Production/Flow/). The call order below is the reference <c>stepFlow</c> order as far
    /// as the B-07/B-09 worker establishes it; that worker owns this file and may reorder the three calls to match
    /// the reference exactly (record the order in the file when you do).
    /// </summary>
    public static partial class SimComposition
    {
        static partial void AddProductionPhases(List<ITickPhase> list)
        {
            AddPowerPhases(list);
            AddMachinePhases(list);
            AddFlowPhases(list);
        }

        static partial void AddProductionInitializers(List<IStateInitializer> list)
        {
            AddPowerInitializers(list);
            AddMachineInitializers(list);
            AddFlowInitializers(list);
        }

        static partial void AddProductionHandlers(List<ICommandHandler> list)
        {
            AddPowerHandlers(list);
            AddMachineHandlers(list);
            AddFlowHandlers(list);
        }

        static partial void AddPowerPhases(List<ITickPhase> list);
        static partial void AddMachinePhases(List<ITickPhase> list);
        static partial void AddFlowPhases(List<ITickPhase> list);
        static partial void AddPowerInitializers(List<IStateInitializer> list);
        static partial void AddMachineInitializers(List<IStateInitializer> list);
        static partial void AddFlowInitializers(List<IStateInitializer> list);
        static partial void AddPowerHandlers(List<ICommandHandler> list);
        static partial void AddMachineHandlers(List<ICommandHandler> list);
        static partial void AddFlowHandlers(List<ICommandHandler> list);
    }
}
