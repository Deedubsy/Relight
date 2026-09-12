namespace Relight.Sim
{
    /// <summary>
    /// The balance data every sim test and the headless boot path uses.
    /// It is now a thin wrapper over the export-generated <see cref="CatalogueData"/>
    /// (`Unity/Tools/export/exportCatalogue.ts` → `Sim/Data/Generated/CatalogueData.g.cs`), so the numbers
    /// come from the reference tables rather than from hand-typed copies.
    /// Runtime content in Unity comes from the ScriptableObject registry (`GameDataRegistry.Build()`), which the
    /// generator fills from the same <see cref="CatalogueData"/>; this class stays the engine-independent source
    /// for tests and for any code that must build <see cref="GameData"/> without the editor or an asset database.
    /// Retired content (iron, artifact1/2/3, KIT_STACKS, legacy rifle constants) is absent by scope filter
    /// (CONTENT_CATALOGUE §17).
    /// </summary>
    public static class ReferenceData
    {
        /// <summary>The full exported catalogue as a <see cref="GameData"/>.</summary>
        public static GameData Create() => CatalogueData.Build();
    }
}
