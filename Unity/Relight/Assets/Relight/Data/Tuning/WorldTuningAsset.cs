using UnityEngine;
using Relight.Sim;

namespace Relight.Data
{
    /// <summary>The tile and lot geometry the world generator uses (tiles.ts; WORLD_AND_ASSETS.md).</summary>
    [CreateAssetMenu(menuName = "Relight/Tuning/World", fileName = "Tuning - World")]
    public sealed class WorldTuningAsset : DataDefinition
    {
        [Tooltip("Pixels per tile at the authoring scale.")]
        [SerializeField] private int tilePx = 32;
        [SerializeField] private int cellTiles = 32;
        [SerializeField] private int lotTiles = 24;
        [SerializeField] private int marginTiles = 4;
        [SerializeField] private int streetTiles = 8;
        [SerializeField] private int depotTiles = 6;
        [SerializeField] private int depotLotTiles = 9;
        [SerializeField] private int substationTiles = 3;

        [Header("Rubble")]
        [SerializeField] private int rubbleUnitsPerTile = 300;
        [SerializeField] private int rubbleTilesMin = 250;
        [SerializeField] private int rubbleTilesMax = 350;

        public WorldTuning ToRecord() => new WorldTuning(tilePx, cellTiles, lotTiles, marginTiles, depotTiles,
            substationTiles, rubbleUnitsPerTile, rubbleTilesMin, rubbleTilesMax, streetTiles, depotLotTiles,
            KindText, Source, Provisional);

        public void Fill(WorldTuning r)
        {
            SetCommon("world", "World", r.Kind, r.Source, r.Provisional);
            tilePx = r.TilePx; cellTiles = r.CellTiles; lotTiles = r.LotTiles; marginTiles = r.MarginTiles;
            depotTiles = r.DepotTiles; substationTiles = r.SubstationTiles; rubbleUnitsPerTile = r.RubbleUnitsPerTile;
            rubbleTilesMin = r.RubbleTilesMin; rubbleTilesMax = r.RubbleTilesMax; streetTiles = r.StreetTiles;
            depotLotTiles = r.DepotLotTiles;
        }

        public override string Problem()
        {
            var baseProblem = base.Problem();
            if (baseProblem != null) return baseProblem;
            if (tilePx <= 0 || cellTiles <= 0 || lotTiles <= 0) return "tile geometry must be positive";
            if (lotTiles + streetTiles > cellTiles) return "the lot and street do not fit in a cell";
            if (rubbleTilesMin > rubbleTilesMax) return "the rubble range is inverted";
            return null;
        }
    }
}
