using System.Diagnostics;
using Relight.Sim;
using UnityEngine;
using UnityEngine.Tilemaps;
using Debug = UnityEngine.Debug;

namespace Relight.World
{
    /// <summary>
    /// Correction pass C6 / U-M-35. Paints the Terrain and Solid tilemaps from a <see cref="WorldGeometryAsset"/>
    /// AT RUNTIME, so <c>World.unity</c> stores no painted cells at all.
    ///
    /// Why: the Phase C editor painter (<c>Relight/World/Paint Imported Region</c>) serialised every cell of the
    /// 78x367 Home crop into the scene asset. The whole city is 864x576 = 497,664 tiles, which as serialised tilemap
    /// cells is a scene file tens of megabytes long that has to be re-saved (and re-diffed, and re-merged) every time
    /// the region is re-exported. The geometry asset is already the single source of truth for those tiles
    /// (WORLD_AND_ASSETS.md §2.6: collision comes from the asset's <c>solid</c> mask, never from the scene), so the
    /// scene keeps only the two empty tilemaps and this component fills them at session start.
    ///
    /// The single Y flip still lives in <see cref="WorldSpace.Cell"/> alone (§2.2): sim tile (x, y) is cell
    /// (x, -y-1). A whole band of sim rows therefore becomes a contiguous cell block whose rows run the other way,
    /// which is what <see cref="Paint"/> assembles before each <see cref="Tilemap.SetTilesBlock"/> call.
    ///
    /// Cost: one <c>TileBase[width * rowsPerBand]</c> buffer per tilemap, reused for every band — no per-tile
    /// allocation and no per-tile <c>SetTile</c> call (which is what made the editor painter slow). The measured
    /// paint time is logged once, so a regression shows up in the console rather than as a mystery hitch.
    /// </summary>
    [DisallowMultipleComponent]
    [AddComponentMenu("Relight/World Painter")]
    public sealed class WorldPainter : MonoBehaviour
    {
        [Tooltip("The region to paint. Leave empty when WorldBootstrap drives the painter instead (it passes its own).")]
        [SerializeField] private WorldGeometryAsset geometry;

        [Tooltip("Tile class -> tile asset. Assets/Relight/World/Tiles/TilePalette.asset.")]
        [SerializeField] private TilePalette palette;

        [Tooltip("World/Generated/Grid/Terrain. Must be EMPTY in the scene asset: this component fills it.")]
        [SerializeField] private Tilemap terrain;

        [Tooltip("World/Generated/Grid/Solid. Must be EMPTY in the scene asset: this component fills it.")]
        [SerializeField] private Tilemap solid;

        [Tooltip("Paint in Awake. Off lets WorldBootstrap paint at session start instead; both is harmless (idempotent).")]
        [SerializeField] private bool paintOnAwake = true;

        [Tooltip("Fill the Solid tilemap with the collision overlay tile. Turn this OFF once CityPresenter draws the " +
                 "real walls: the overlay is a Phase B debug view and its tilemap sorts above every sprite.")]
        [SerializeField] private bool paintSolidOverlay = true;

        [Tooltip("Sim rows per SetTilesBlock call. 64 rows of the full city is a 55k-entry buffer (~440 KB).")]
        [SerializeField] private int rowsPerBand = 64;

        [Tooltip("Log the measured paint time and tile counts once per paint.")]
        [SerializeField] private bool logPaintTime = true;

        private WorldGeometryAsset _painted;
        private TileBase[] _terrainBuffer;
        private TileBase[] _solidBuffer;

        /// <summary>The region currently on the tilemaps, or null before the first paint.</summary>
        public WorldGeometryAsset Painted => _painted;

        /// <summary>Milliseconds the last <see cref="Paint"/> took, for the evidence line and the tests.</summary>
        public double LastPaintMilliseconds { get; private set; }

        /// <summary>Cells written by the last paint: terrain then solid.</summary>
        public int LastTerrainCells { get; private set; }
        public int LastSolidCells { get; private set; }

        private void Awake()
        {
            if (paintOnAwake) Paint(geometry);
        }

        /// <summary>
        /// Paint <paramref name="asset"/> onto the two tilemaps. Does nothing when the same asset is already on them,
        /// so calling this from both <c>Awake</c> and the session start costs one paint, not two. Pass
        /// <paramref name="force"/> to repaint anyway (after a hot re-import in the editor).
        /// </summary>
        public bool Paint(WorldGeometryAsset asset, bool force = false)
        {
            if (asset == null) asset = geometry;
            if (asset == null)
            {
                Debug.LogWarning("Relight: WorldPainter has no WorldGeometryAsset; the tilemaps stay empty.");
                return false;
            }
            if (!force && ReferenceEquals(_painted, asset)) return true;
            if (terrain == null || solid == null)
            {
                Debug.LogError("Relight: WorldPainter needs both the Terrain and the Solid tilemap assigned.");
                return false;
            }
            if (palette == null)
            {
                Debug.LogError("Relight: WorldPainter has no TilePalette; assign Assets/Relight/World/Tiles/TilePalette.asset.");
                return false;
            }
            var w = asset.Width;
            var h = asset.Height;
            if (w <= 0 || h <= 0 || asset.Kind == null || asset.Kind.Length != w * h)
            {
                Debug.LogError($"Relight: WorldPainter cannot paint '{asset.name}': it is {w}x{h} but carries " +
                               $"{(asset.Kind == null ? 0 : asset.Kind.Length)} tile classes. Re-import the region.");
                return false;
            }

            var band = Mathf.Clamp(rowsPerBand <= 0 ? 64 : rowsPerBand, 1, h);
            var size = w * band;
            if (_terrainBuffer == null || _terrainBuffer.Length != size) _terrainBuffer = new TileBase[size];
            if (_solidBuffer == null || _solidBuffer.Length != size) _solidBuffer = new TileBase[size];

            // One palette lookup per tile class instead of one per tile: TilePalette.For is only a switch, but it is
            // also 497,664 calls on the full city. 256 entries covers every byte the side-car can hold, including
            // TileClass.Void (255), which For answers with null — an unpainted cell, exactly as before.
            var byClass = new TileBase[256];
            for (var k = 0; k < byClass.Length; k++) byClass[k] = palette.For((TileClass)k);
            var solidTile = palette.Solid;
            var nodeArt=ResourceNodeArtSet.Load();

            var watch = Stopwatch.StartNew();
            terrain.ClearAllTiles();
            solid.ClearAllTiles();

            var terrainCells = 0;
            var solidCells = 0;
            var kind = asset.Kind;
            var mask = paintSolidOverlay && asset.Solid != null && asset.Solid.Length == w * h ? asset.Solid : null;

            for (var y0 = 0; y0 < h; y0 += band)
            {
                var rows = Mathf.Min(band, h - y0);
                var used = w * rows;
                for (var i = 0; i < used; i++) { _terrainBuffer[i] = null; _solidBuffer[i] = null; }

                for (var r = 0; r < rows; r++)
                {
                    var y = y0 + r;
                    // WorldSpace.Cell(x, y).y == -y-1, and the block's lowest cell row is -(y0+rows-1)-1, so sim row
                    // y sits at block row (y0 + rows - 1 - y). The rows are simply reversed inside the band.
                    var dst = (y0 + rows - 1 - y) * w;
                    var src = y * w;
                    for (var x = 0; x < w; x++)
                    {
                        var tileClass=(TileClass)kind[src+x];
                        var illustrated=nodeArt!=null&&Tiles.CanHoldUnits(tileClass)&&
                            nodeArt.Has(ResourceNodeArtSet.Kind(tileClass,(PatchType)asset.Patch[src+x]));
                        // Resource art has alpha. Keep plain ground underneath, including after depletion.
                        var t = byClass[illustrated?(int)TileClass.Ground:kind[src+x]];
                        if (t != null) { _terrainBuffer[dst + x] = t; terrainCells++; }
                        if (mask != null && mask[src + x] != 0 && solidTile != null)
                        {
                            _solidBuffer[dst + x] = solidTile;
                            solidCells++;
                        }
                    }
                }

                var block = new BoundsInt(0, -(y0 + rows), 0, w, rows, 1);
                if (used == _terrainBuffer.Length)
                {
                    terrain.SetTilesBlock(block, _terrainBuffer);
                    solid.SetTilesBlock(block, _solidBuffer);
                }
                else
                {
                    // The last band is short: SetTilesBlock requires an array exactly as long as the block.
                    var tail = new TileBase[used];
                    System.Array.Copy(_terrainBuffer, tail, used);
                    terrain.SetTilesBlock(block, tail);
                    var tailSolid = new TileBase[used];
                    System.Array.Copy(_solidBuffer, tailSolid, used);
                    solid.SetTilesBlock(block, tailSolid);
                }
            }

            terrain.CompressBounds();
            solid.CompressBounds();
            watch.Stop();

            _painted = asset;
            LastPaintMilliseconds = watch.Elapsed.TotalMilliseconds;
            LastTerrainCells = terrainCells;
            LastSolidCells = solidCells;
            if (logPaintTime)
                Debug.Log($"Relight: painted region '{asset.RegionId}' {w}x{h} ({w * h} tiles) in " +
                          $"{LastPaintMilliseconds:0.0} ms — {terrainCells} terrain cells, {solidCells} solid cells, " +
                          $"{Mathf.CeilToInt(h / (float)band)} SetTilesBlock bands of {band} rows.");
            return true;
        }

        /// <summary>Empty both tilemaps (used by the editor menu and before a region change).</summary>
        public void Clear()
        {
            if (terrain != null) { terrain.ClearAllTiles(); terrain.CompressBounds(); }
            if (solid != null) { solid.ClearAllTiles(); solid.CompressBounds(); }
            _painted = null;
        }
    }
}
