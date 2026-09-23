namespace Relight.Sim
{
    /// <summary>
    /// Combat's part of the state (Phase C, Wave 0 contract). One top-level object per sub-hook, each implemented
    /// once in its owner's file: <c>weapons</c> (C-03: owned weapons, equipped selection, magazine, reload;
    /// projectiles in flight), <c>turrets</c> (C-04: per-turret aim, cooldown and target keyed by machine id),
    /// <c>enemies</c> (C-08: living actors), <c>director</c> (C-08: raid timestamps and serials), <c>encounters</c> (batch 4, FRT-02: camps, squats and
    /// arenas).
    /// </summary>
    public sealed partial class SimState
    {
        partial void VisitCombat(IStateVisitor v)
        {
            VisitWeapons(v);
            VisitTurrets(v);
            VisitEnemies(v);
            VisitDirector(v);
            VisitEncounters(v);
        }

        partial void VisitWeapons(IStateVisitor v);
        partial void VisitTurrets(IStateVisitor v);
        partial void VisitEnemies(IStateVisitor v);
        partial void VisitDirector(IStateVisitor v);
        partial void VisitEncounters(IStateVisitor v);
    }
}
