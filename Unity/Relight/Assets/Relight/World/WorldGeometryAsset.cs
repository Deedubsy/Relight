using System;
using System.Collections.Generic;
using Relight.Sim;
using UnityEngine;

namespace Relight.World
{
    /// <summary>
    /// C-01 / D-02a. One imported authored region, as an asset. Written by
    /// <c>Relight/World/Import Home Region</c> from <c>Unity/Import/&lt;region&gt;/</c> and never hand-edited:
    /// everything under <c>Assets/Relight/World/Generated/</c> is regenerated wholesale (per-site overrides live
    /// outside <c>Generated/</c>, see <see cref="HomeSitesOverrides"/>).
    ///
    /// Coordinates are REGION-LOCAL and Y-DOWN, exactly as the sim wants them (U-M-14: geometry is data).
    /// <see cref="originX"/>/<see cref="originY"/> say where the region sits in the whole city so a tile can still be
    /// named in city coordinates. The Y flip to Unity cell space happens only in <see cref="WorldSpace.Cell"/>
    /// (WORLD_AND_ASSETS.md §2.2).
    /// </summary>
    [CreateAssetMenu(menuName = "Relight/World Geometry", fileName = "WorldGeometry")]
    public sealed class WorldGeometryAsset : ScriptableObject
    {
        [Header("Provenance")]
        [Tooltip("The authored city this came from (riverfront.ts RIVERFRONT_ID).")]
        [SerializeField] private string mapId;
        [Tooltip("Which region of it (city.json region.id), e.g. 'home'.")]
        [SerializeField] private string regionId;
        [Tooltip("Human name of the region (city.json region.name).")]
        [SerializeField] private string regionName;
        [Tooltip("sha256 over riverfront.ts + gameplaySites.ts + firstRegion.ts at export time.")]
        [SerializeField] private string sourceSha256;
        [Tooltip("maps/phaser/manifest.json base.city.id.")]
        [SerializeField] private string manifestId;
        [Tooltip("maps/phaser/manifest.json sourceHash.")]
        [SerializeField] private string manifestHash;
        [Tooltip("maps/phaser/manifest.json version.")]
        [SerializeField] private int manifestVersion;
        [Tooltip("city.json schema number the importer read.")]
        [SerializeField] private int schema;

        [Header("Region")]
        [SerializeField] private int width;
        [SerializeField] private int height;
        [Tooltip("The region's north-west corner in whole-city tiles.")]
        [SerializeField] private int originX;
        [SerializeField] private int originY;
        [Tooltip("Region-local engineer start (city.json spawn).")]
        [SerializeField] private Vector2Int spawn;

        [Header("Tiles (row-major, t = y * width + x, Y down)")]
        [Tooltip("Relight.Sim.TileClass values: Street=0 Ground=1 Rubble=2 Inert=3 River=4 Deposit=5 Patch=6.")]
        [SerializeField] private byte[] kind = Array.Empty<byte>();
        [Tooltip("Renderer terrain variant per tile (reference ground.ts paint).")]
        [SerializeField] private byte[] variant = Array.Empty<byte>();
        [Tooltip("Relight.Sim.PatchType per tile: None=0 Steel=1 Copper=2 Coal=3.")]
        [SerializeField] private byte[] patch = Array.Empty<byte>();
        [Tooltip("Authored building collision, 0 or 1 (reference ground.ts:628-630 urban.solid).")]
        [SerializeField] private byte[] solid = Array.Empty<byte>();

        [Header("Content")]
        [Tooltip("Building rects only; prefabs and visuals are D-02b.")]
        [SerializeField] private List<BuildingRecord> buildings = new List<BuildingRecord>();
        [Tooltip("Counts the exporter recorded; the importer re-derives and compares them.")]
        [SerializeField] private RegionValidation validation = new RegionValidation();

        [Header("City (presentation only — never collision)")]
        [Tooltip("Props, roads, paths, drives, the tram centreline and the paved areas the CityPresenter draws. " +
                 "Collision still comes from `solid` alone; nothing here is ever consulted by the sim.")]
        [SerializeField] private CityDecor city = new CityDecor();

        public string MapId => mapId;
        public string RegionId => regionId;
        public string RegionName => regionName;
        public string SourceSha256 => sourceSha256;
        public string ManifestId => manifestId;
        public string ManifestHash => manifestHash;
        public int ManifestVersion => manifestVersion;
        public int Schema => schema;
        public int Width => width;
        public int Height => height;
        public int OriginX => originX;
        public int OriginY => originY;
        public Vector2Int Spawn => spawn;
        public byte[] Kind => kind;
        public byte[] Variant => variant;
        public byte[] Patch => patch;
        public byte[] Solid => solid;
        public IReadOnlyList<BuildingRecord> Buildings => buildings;
        public RegionValidation Validation => validation;
        /// <summary>Presentation-only city furniture (never collision). Never null.</summary>
        public CityDecor City => city ?? (city = new CityDecor());

        /// <summary>The region's rect in whole-city tiles, Y down.</summary>
        public RectInt CityRect => new RectInt(originX, originY, width, height);

        /// <summary>A region-local tile in whole-city tiles.</summary>
        public Vector2Int ToCity(int x, int y) => new Vector2Int(x + originX, y + originY);

        /// <summary>A whole-city tile in region-local tiles (may be out of bounds; check with <see cref="InBounds"/>).</summary>
        public Vector2Int ToRegion(int x, int y) => new Vector2Int(x - originX, y - originY);

        /// <summary>Row-major index of a region-local tile (WORLD_AND_ASSETS.md §2.1).</summary>
        public int Index(int x, int y) => y * width + x;

        public bool InBounds(int x, int y) => x >= 0 && y >= 0 && x < width && y < height;

        /// <summary>The tile class at a region-local tile; off-map is <see cref="TileClass.Void"/>.</summary>
        public TileClass TileAt(int x, int y) => InBounds(x, y) ? (TileClass)kind[Index(x, y)] : TileClass.Void;

        /// <summary>Authored building collision at a region-local tile.</summary>
        public bool SolidAt(int x, int y) => InBounds(x, y) && solid.Length == kind.Length && solid[Index(x, y)] != 0;

        /// <summary>Written by the importer only (editor code); the asset is otherwise read-only at runtime.</summary>
        public void Fill(string newMapId, string newRegionId, string newRegionName, string newSourceSha256,
            string newManifestId, string newManifestHash, int newManifestVersion, int newSchema,
            int newWidth, int newHeight, int newOriginX, int newOriginY, Vector2Int newSpawn,
            byte[] newKind, byte[] newVariant, byte[] newPatch, byte[] newSolid,
            List<BuildingRecord> newBuildings, RegionValidation newValidation)
        {
            mapId = newMapId; regionId = newRegionId; regionName = newRegionName; sourceSha256 = newSourceSha256;
            manifestId = newManifestId; manifestHash = newManifestHash; manifestVersion = newManifestVersion;
            schema = newSchema;
            width = newWidth; height = newHeight; originX = newOriginX; originY = newOriginY; spawn = newSpawn;
            kind = newKind; variant = newVariant; patch = newPatch; solid = newSolid;
            buildings = newBuildings; validation = newValidation;
        }

        /// <summary>Written by the importer only: the presentation-only city furniture.</summary>
        public void FillCity(CityDecor newCity) => city = newCity ?? new CityDecor();

        /// <summary>One authored building's footprint (D-02b turns these into prefabs; here they only explain the mask).</summary>
        [Serializable]
        public sealed class BuildingRecord
        {
            public string id;
            public string name;
            public string kind;
            /// <summary>'N' / 'E' / 'S' / 'W' (reference riverfront.ts facing).</summary>
            public string facing;
            /// <summary>Region-local collision rect.</summary>
            public RectInt rect;
            /// <summary>Region-local parcel rect (the plot, reference riverfront.ts parcel).</summary>
            public RectInt parcel;
            /// <summary>Region-local drawn rect (reference riverfront.ts visual).</summary>
            public RectInt visual;
            /// <summary>Region-local doorway rect (reference parcelGeometry.ts:8 doorRect); meaningless unless enterable.</summary>
            public RectInt doorRect;
            public bool enterable;
            /// <summary>True when the building declares an explicit door tile (reference riverfront.ts b.door).</summary>
            public bool hasDoor;
            public int variant;
            /// <summary>Roof sprite key (WORLD_AND_ASSETS.md §6.2), precomputed by the exporter.</summary>
            public string roofKey;
            /// <summary>In maps/phaser/manifest.json fixedBuildings (a campaign/interior building).</summary>
            public bool fixedBuilding;
            /// <summary>Extra doorway rects on the collision ring (reference riverfront.ts b.doors); usually empty.</summary>
            public List<RectInt> doors = new List<RectInt>();
        }

        // ------------------------------------------------------------------ presentation-only city furniture
        //
        // Everything below exists so Presentation/City can redraw the reference's riverfrontDraw.ts pass from the
        // asset. It is DATA ABOUT DRAWING: the sim never reads it, and collision never comes from it (U-M-14).

        /// <summary>A tile-space polyline (road, path, drive). Points are region-local tile corners, Y down.</summary>
        [Serializable] public sealed class Poly { public List<Vector2Int> points = new List<Vector2Int>(); }

        /// <summary>One authored prop (riverfront.ts RIVERFRONT_PROPS). `clearable` props can be removed in play.</summary>
        [Serializable]
        public sealed class PropRecord
        {
            public string id;
            public string kind;
            public RectInt rect;
            public bool clearable;
        }

        /// <summary>A road graph node (riverfront.ts roadNodes): junctions get a carriageway disc drawn on them.</summary>
        [Serializable] public sealed class RoadNode { public string id; public Vector2Int tile; }

        /// <summary>A named paved rect (riverfront.ts squares); `name` may be empty.</summary>
        [Serializable] public sealed class NamedRect { public string name; public RectInt rect; }

        /// <summary>Presentation-only city furniture. Every list is non-null and in exporter order.</summary>
        [Serializable]
        public sealed class CityDecor
        {
            public List<PropRecord> props = new List<PropRecord>();
            public List<RoadNode> roadNodes = new List<RoadNode>();
            public List<Poly> roads = new List<Poly>();
            [Tooltip("riverfront.ts roadHalf: half the carriageway+pavement width, in tiles.")]
            public int roadHalfWidth = 5;
            [Tooltip("riverfront.ts pavement: the pavement band inside roadHalfWidth, in tiles.")]
            public int roadPavement = 2;
            public List<Poly> paths = new List<Poly>();
            public List<Poly> drives = new List<Poly>();
            [Tooltip("The baked tram centreline (riverfrontRail samples), region-local tile coordinates.")]
            public List<Vector2> tramSamples = new List<Vector2>();
            public float tramLength;
            [Tooltip("riverfront.ts railHalf: half the reserved rail corridor, in tiles.")]
            public int railHalfWidth = 3;
            public List<RectInt> serviceAreas = new List<RectInt>();
            public List<NamedRect> squares = new List<NamedRect>();
            [Tooltip("Founders Court bulb centre, region-local.")]
            public Vector2Int court;
            public float courtRadius;
            [Tooltip("riverfront.ts riverY, region-local: the river's north edge row.")]
            public int riverY;
        }

        /// <summary>The exporter's region counts; the importer re-derives every one of them.</summary>
        [Serializable]
        public sealed class RegionValidation
        {
            public int buildings;
            public int props;
            public int land;
            public int accessible;
            public int tiles;
        }
    }
}
