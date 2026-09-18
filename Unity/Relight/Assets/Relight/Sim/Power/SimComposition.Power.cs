using System.Collections.Generic;

namespace Relight.Sim
{
    public static partial class SimComposition
    {
        /// <summary>
        /// B-09. The grid is rebuilt and the generators burn before any machine asks for its throttle, which is the
        /// reference's order: <c>stepFlow</c> reads <c>campaignGrid(st)</c> (flow.ts:1064) ahead of the machine loop.
        /// </summary>
        static partial void AddPowerPhases(List<ITickPhase> list) => list.Add(new PowerPhase());

        static partial void AddPowerInitializers(List<IStateInitializer> list) { }

        static partial void AddPowerHandlers(List<ICommandHandler> list) { }
    }
}
