using System.Collections.Generic;

namespace Relight.Sim
{
    /// <summary>
    /// Campaign's entries in the fixed composition (Phase C, Wave 0 contract). Sub-slots, each in its owner's file:
    /// <c>HomeCore</c> (C-05, Sim/Campaign/HomeCore/), <c>Opening</c> (C-09, Sim/Campaign/Opening/),
    /// <c>Light</c> (C-11, Sim/Campaign/Light/). The opening state machine runs after combat so it sees this tick's
    /// turret and enemy state (reference: goal/opening stepped after threat in sim.ts).
    /// </summary>
    public static partial class SimComposition
    {
        static partial void AddCampaignPhases(List<ITickPhase> list)
        {
            AddHomeCorePhases(list);
            AddOpeningPhases(list);
            AddLightPhases(list);
        }

        static partial void AddCampaignInitializers(List<IStateInitializer> list)
        {
            AddHomeCoreInitializers(list);
            AddOpeningInitializers(list);
            AddLightInitializers(list);
        }

        static partial void AddCampaignHandlers(List<ICommandHandler> list)
        {
            AddHomeCoreHandlers(list);
            AddOpeningHandlers(list);
            AddLightHandlers(list);
        }

        static partial void AddHomeCorePhases(List<ITickPhase> list);
        static partial void AddOpeningPhases(List<ITickPhase> list);
        static partial void AddLightPhases(List<ITickPhase> list);
        static partial void AddHomeCoreInitializers(List<IStateInitializer> list);
        static partial void AddOpeningInitializers(List<IStateInitializer> list);
        static partial void AddLightInitializers(List<IStateInitializer> list);
        static partial void AddHomeCoreHandlers(List<ICommandHandler> list);
        static partial void AddOpeningHandlers(List<ICommandHandler> list);
        static partial void AddLightHandlers(List<ICommandHandler> list);
    }
}
