namespace Relight.Sim
{
    /// <summary>A body was born at (X, Y) (reference <c>T.stats.spawned++</c> in campaignThreat.ts <c>birth</c>).</summary>
    public sealed record EnemySpawnedEvent(double T, int EnemyId, string Kind, double X, double Y, int Group) : SimEvent(T);

    /// <summary>A body reached 0 hp and left the list. <paramref name="ByTurret"/> separates turret kills from the engineer's.</summary>
    public sealed record EnemyKilledEvent(double T, int EnemyId, string Kind, double X, double Y, bool ByTurret) : SimEvent(T);

    /// <summary>
    /// A structure with an HP row lost hit points (turret, cannon, wall, barricade). <c>Hp</c> is what is LEFT,
    /// <c>Max</c> the row's full value, so a presenter can draw the bar without another query. Raised by
    /// <see cref="TurretRules.Damage"/>, which is the single damage entry point for structures.
    /// </summary>
    public sealed record StructureDamagedEvent(double T, int MachineId, double Hp, double Max) : SimEvent(T);

    /// <summary>A structure was repaired (W-A's repair command through <see cref="TurretRules.TurretRepairHook"/>).</summary>
    public sealed record StructureRepairedEvent(double T, int MachineId, double Hp, double Max) : SimEvent(T);

    /// <summary>A structure's hit points reached 0. It stays on the map, disabled, until removed or repaired.</summary>
    public sealed record StructureDestroyedEvent(double T, int MachineId, string Kind) : SimEvent(T);

    /// <summary>A spitter glob was launched; the presenter tracks it through <see cref="EnemyQueries.Projectiles"/>.</summary>
    public sealed record EnemySpitEvent(double T, int EnemyId, double X, double Y, double AimX, double AimY) : SimEvent(T);
}
