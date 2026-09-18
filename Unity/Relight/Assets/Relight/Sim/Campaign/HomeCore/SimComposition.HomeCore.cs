using System.Collections.Generic;

namespace Relight.Sim
{
    /// <summary>C-05's entries in the fixed composition (Campaign slot, before Opening).</summary>
    public static partial class SimComposition
    {
        static partial void AddHomeCorePhases(List<ITickPhase> list) => list.Add(new HomeCorePhase());

        static partial void AddHomeCoreInitializers(List<IStateInitializer> list) => list.Add(new HomeCoreInitializer());

        static partial void AddHomeCoreHandlers(List<ICommandHandler> list) => list.Add(new HomeCoreHandler());
    }
}
