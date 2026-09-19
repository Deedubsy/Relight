namespace Relight.Sim
{
    /// <summary>
    /// U-D-58: the port is always dark. <c>Sim/Data/Generated/CatalogueData.g.cs</c> is generated from the
    /// TypeScript reference, which has a sun (<c>daylightSeconds</c> 900), so the port's own value lives in this
    /// layer, applied after <see cref="CombatBalance"/> in both data paths (<see cref="ReferenceData.Create"/> and
    /// the Unity <c>GameDataRegistry.Build</c>). Zero daylight seconds means there is no sun:
    /// <see cref="LightQueries.Daylight(double,double,double)"/> answers dark at every time.
    /// Rules: Unity/Docs/ALWAYS_DARK_SPEC.md.
    /// </summary>
    public static class DarkWorld
    {
        public static GameData Apply(GameData d)
        {
            if (d == null || d.Time == null || d.Time.DaylightSeconds == 0) return d;
            return new GameData(d.Items, d.Machines, d.Recipes, d.Engineer, d.World, d.Weapons, d.Enemies,
                d.Ammunition, d.Turrets, d.Power, d.Time with { DaylightSeconds = 0 }, d.Raids, d.Opening,
                d.Stake, d.Defence, d.Siege);
        }
    }
}
