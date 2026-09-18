using System.Collections.Generic;

namespace Relight.Sim
{
    /// <summary>
    /// The Director sub-slot of the fixed composition (C-08, W-B). It runs last in Combat: the clock reacts to the
    /// state the bodies have just reached this tick (a wave whose last body died is closed in the same tick), which
    /// is the reference's own order inside <c>tickCampaignThreat</c>.
    /// </summary>
    public static partial class SimComposition
    {
        static partial void AddDirectorPhases(List<ITickPhase> list)
        {
            list.Add(new DirectorPhase());
        }

        static partial void AddDirectorInitializers(List<IStateInitializer> list)
        {
            list.Add(new DirectorInitializer());
        }

        static partial void AddDirectorHandlers(List<ICommandHandler> list)
        {
            list.Add(new DebugRaidHandler());
        }
    }

    /// <summary>
    /// Rolls the active-raid clock once, for a new game. A loaded save arrives with its clock already seeded and is
    /// left alone: the selected starts are saved, never rerolled on load (reference campaignThreat.ts:26). A Phase B
    /// save upgraded to schema v3 arrives unseeded and is seeded by <see cref="DirectorPhase"/> on its first tick,
    /// from that save's own <c>st.T</c>.
    /// </summary>
    public sealed class DirectorInitializer : IStateInitializer
    {
        public void Init(SimContext ctx, SimState st)
        {
            if (st.Director.Unseeded) DirectorPhase.Seed(ctx, st);
        }
    }
}
