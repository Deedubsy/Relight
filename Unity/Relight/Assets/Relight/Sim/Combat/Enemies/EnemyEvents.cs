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
    /// <summary>
    /// L-02, ALWAYS_DARK_SPEC §5.1/§5.6: an uncommitted body has stopped at the edge of lit ground. Raised once as
    /// the pause begins; (X, Y) is the lit tile it is looking at. The guide shows its light line on the first one.
    /// </summary>
    public sealed record LightHesitationEvent(double T, int EnemyId, int Layer, int Group, int X, int Y) : SimEvent(T);

    public sealed record EnemySpitEvent(double T, int EnemyId, double X, double Y, double AimX, double AimY) : SimEvent(T);
}
