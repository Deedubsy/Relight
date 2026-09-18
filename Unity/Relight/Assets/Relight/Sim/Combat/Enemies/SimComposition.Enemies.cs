using System.Collections.Generic;

namespace Relight.Sim
{
    /// <summary>
    /// The Enemy sub-slot of the fixed composition (C-08 actors, W-B). One phase and one initialiser. The
    /// initialiser exists solely to point <see cref="WeaponState.Targets"/> at the bodies: the seam is deliberately
    /// never saved, so a load would otherwise leave the player's rifle with nothing to hit until the first tick.
    /// No command handlers: bodies are created by the director, never by the player.
    /// </summary>
    public static partial class SimComposition
    {
        static partial void AddEnemyPhases(List<ITickPhase> list)
        {
            list.Add(new EnemyPhase());
        }

        static partial void AddEnemyInitializers(List<IStateInitializer> list)
        {
            list.Add(new EnemyInitializer());
        }

        static partial void AddEnemyHandlers(List<ICommandHandler> list)
        {
        }
    }

    /// <summary>Re-points the transient ballistics seam after a new game or a load.</summary>
    public sealed class EnemyInitializer : IStateInitializer
    {
        public void Init(SimContext ctx, SimState st)
        {
            st.Enemies.Seam = new EnemyTargets(st);
            st.Weapons.Targets = st.Enemies.Seam;
        }
    }
}
