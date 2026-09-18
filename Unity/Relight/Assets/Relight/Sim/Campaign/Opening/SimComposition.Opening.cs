using System.Collections.Generic;

namespace Relight.Sim
{
    /// <summary>
    /// C-09's composition slot. The Campaign order is HomeCore → <b>Opening</b> → Light, and inside the campaign
    /// slot the opening phase must run AFTER <c>DirectorPhase</c> so it reads the wave state this tick produced.
    /// </summary>
    public static partial class SimComposition
    {
        static partial void AddOpeningPhases(List<ITickPhase> list) => list.Add(new OpeningPhase());

        /// <summary>
        /// New games only. An upgraded save arrives with a default <see cref="OpeningState"/>
        /// (<see cref="OpeningStatus.None"/>) and <see cref="OpeningPhase"/> applies the same fresh rule on its
        /// first tick, exactly as the reference's <c>initOpeningEncounter</c> runs against whatever the save holds.
        /// </summary>
        static partial void AddOpeningInitializers(List<IStateInitializer> list) =>
            list.Add(new OpeningInitializer());

        /// <summary>C-09 adds no commands: the opening is derived from state, never driven by the player (rule 8).</summary>
        static partial void AddOpeningHandlers(List<ICommandHandler> list) { }
    }
}
