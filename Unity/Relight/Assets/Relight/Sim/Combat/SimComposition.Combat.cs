using System.Collections.Generic;

namespace Relight.Sim
{
    /// <summary>
    /// Combat's entries in the fixed composition (Phase C, Wave 0 contract). Sub-slots, each implemented in its
    /// owner's own file: <c>Weapon</c> (C-03, Sim/Combat/Weapons/), <c>Turret</c> (C-04, Sim/Combat/Turrets/),
    /// <c>Enemy</c> (C-08 actors, Sim/Combat/Enemies/), <c>Director</c> (C-08 raid director, Sim/Combat/Director/).
    /// Order per tick: the player's weapon fires, turrets track and fire, enemies move and strike, the director
    /// schedules — the reference threat.ts / campaignThreat.ts order; the C-08 worker may swap Enemy/Director if the
    /// reference steps the director first (record it here).
    /// </summary>
    public static partial class SimComposition
    {
        static partial void AddCombatPhases(List<ITickPhase> list)
        {
            AddWeaponPhases(list);
            AddTurretPhases(list);
            AddEnemyPhases(list);
            AddDirectorPhases(list);
        }

        static partial void AddCombatInitializers(List<IStateInitializer> list)
        {
            AddWeaponInitializers(list);
            AddTurretInitializers(list);
            AddEnemyInitializers(list);
            AddDirectorInitializers(list);
        }

        static partial void AddCombatHandlers(List<ICommandHandler> list)
        {
            AddWeaponHandlers(list);
            AddTurretHandlers(list);
            AddEnemyHandlers(list);
            AddDirectorHandlers(list);
        }

        static partial void AddWeaponPhases(List<ITickPhase> list);
        static partial void AddTurretPhases(List<ITickPhase> list);
        static partial void AddEnemyPhases(List<ITickPhase> list);
        static partial void AddDirectorPhases(List<ITickPhase> list);
        static partial void AddWeaponInitializers(List<IStateInitializer> list);
        static partial void AddTurretInitializers(List<IStateInitializer> list);
        static partial void AddEnemyInitializers(List<IStateInitializer> list);
        static partial void AddDirectorInitializers(List<IStateInitializer> list);
        static partial void AddWeaponHandlers(List<ICommandHandler> list);
        static partial void AddTurretHandlers(List<ICommandHandler> list);
        static partial void AddEnemyHandlers(List<ICommandHandler> list);
        static partial void AddDirectorHandlers(List<ICommandHandler> list);
    }
}
