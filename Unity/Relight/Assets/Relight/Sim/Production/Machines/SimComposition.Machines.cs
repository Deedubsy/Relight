using System.Collections.Generic;

namespace Relight.Sim
{
    public static partial class SimComposition
    {
        /// <summary>B-07: the excavators and the processors, after the grid they draw from.</summary>
        static partial void AddMachinePhases(List<ITickPhase> list) => list.Add(new MachinePhase());

        static partial void AddMachineInitializers(List<IStateInitializer> list) { }

        static partial void AddMachineHandlers(List<ICommandHandler> list) => list.Add(new SetRecipeHandler());
    }
}
