using System.Collections.Generic;

namespace Relight.Sim
{
    public static partial class SimComposition
    {
        static partial void AddFlowPhases(List<ITickPhase> list) => list.Add(new FlowPhase());

        static partial void AddFlowInitializers(List<IStateInitializer> list) { }

        static partial void AddFlowHandlers(List<ICommandHandler> list) => list.Add(new FlowHandler());
    }
}
