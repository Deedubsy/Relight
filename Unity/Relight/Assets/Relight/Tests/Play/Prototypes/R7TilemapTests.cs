using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using NUnit.Framework;
using Relight.Prototypes;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.Tilemaps;

namespace Relight.Tests.Play.Prototypes
{
    /// <summary>
    /// B-14 / R7 — "does a Unity Tilemap hold the real map at the real size". Risk R7 in
    /// TECHNICAL_ARCHITECTURE.md: 864 × 576 = 497,664 tiles over the planned sprite layers
    /// (WORLD_AND_ASSETS.md §7.1 — Terrain, TerrainVariant, ResourcePatch and the baked road surface).
    ///
    /// The scene is painted by the editor menu (Relight/Prototypes/Paint R7) so the tiles live in the SCENE FILE and
    /// are visible with the editor stopped; these tests measure what that costs to load and to draw.
    /// </summary>
    public sealed class R7TilemapTests
    {
        public const int Width = 864;
        public const int Height = 576;
        public const int Tiles = Width * Height;

        /// <summary>The brief's sweep length: the whole map, corner to corner, in ten seconds.</summary>
        public const float SweepSeconds = 10f;

        private static Tilemap Layer(string name)
        {
            foreach (var t in Object.FindObjectsByType<Tilemap>(FindObjectsSortMode.None))
                if (t.name == name) return t;
            return null;
        }

        /// <summary>
        /// Painted CELLS, not distinct tile assets. <c>Tilemap.GetUsedTilesCount()</c> answers the second question
        /// (it returns 7 for the Terrain layer: the number of different tile ASSETS used), which is worth recording
        /// but is not the R7 figure. The R7 figure is how many of the 497,664 cells actually carry a tile.
        /// </summary>
        private static int PaintedCells(Tilemap map)
        {
            var block = map.GetTilesBlock(map.cellBounds);
            var painted = 0;
            for (var i = 0; i < block.Length; i++) if (block[i] != null) painted++;
            return painted;
        }

        [UnityTest, Timeout(600000)]
        public IEnumerator TheWholeMapIsInTheSceneFile()
        {
            PrototypeFixture.RequirePaintedMap();

            var load = Stopwatch.StartNew();
            yield return PrototypeFixture.Load(PrototypeFixture.R7);
            load.Stop();

            var maps = Object.FindObjectsByType<Tilemap>(FindObjectsSortMode.None);
            Assert.AreEqual(4, maps.Length, "the planned layer count (WORLD_AND_ASSETS.md §7.1)");

            var terrain = Layer("Terrain");
            Assert.NotNull(terrain, "R7-Tilemap.unity has no Terrain layer; run Relight/Prototypes/Paint R7.");
            var bounds = terrain.cellBounds;
            Assert.AreEqual(Width, bounds.size.x, "map width in cells");
            Assert.AreEqual(Height, bounds.size.y, "map height in cells");

            var count = Stopwatch.StartNew();
            var terrainTiles = PaintedCells(terrain);
            var variantTiles = PaintedCells(Layer("TerrainVariant"));
            var resourceTiles = PaintedCells(Layer("ResourcePatch"));
            var roadTiles = PaintedCells(Layer("RoadSurface"));
            count.Stop();

            Assert.AreEqual(Tiles, terrainTiles, "every one of the 497,664 cells carries a terrain tile");
            Assert.Greater(roadTiles, 0);
            Assert.Greater(variantTiles, 0);
            Assert.Greater(resourceTiles, 0);

            var values = new List<KeyValuePair<string, object>>
            {
                new KeyValuePair<string, object>("prototype", "R7 tilemap"),
                new KeyValuePair<string, object>("map_width", Width),
                new KeyValuePair<string, object>("map_height", Height),
                new KeyValuePair<string, object>("tiles_total", Tiles),
                new KeyValuePair<string, object>("tilemap_layers", maps.Length),
                new KeyValuePair<string, object>("terrain_tiles", terrainTiles),
                new KeyValuePair<string, object>("variant_tiles", variantTiles),
                new KeyValuePair<string, object>("resource_tiles", resourceTiles),
                new KeyValuePair<string, object>("road_tiles", roadTiles),
                new KeyValuePair<string, object>("tiles_painted_total", terrainTiles + variantTiles + resourceTiles + roadTiles),
                new KeyValuePair<string, object>("distinct_tile_assets_terrain", terrain.GetUsedTilesCount()),
                new KeyValuePair<string, object>("scene_load_ms", EvidenceWriter.Round(load.Elapsed.TotalMilliseconds)),
                new KeyValuePair<string, object>("count_cells_ms", EvidenceWriter.Round(count.Elapsed.TotalMilliseconds)),
                new KeyValuePair<string, object>("total_allocated_bytes", UnityEngine.Profiling.Profiler.GetTotalAllocatedMemoryLong()),
                new KeyValuePair<string, object>("total_reserved_bytes", UnityEngine.Profiling.Profiler.GetTotalReservedMemoryLong()),
                new KeyValuePair<string, object>("graphics_device", SystemInfo.graphicsDeviceType.ToString()),
            };
            EvidenceWriter.Write("b14-r7-load.json", values);
            UnityEngine.Debug.Log($"B14 R7: scene load {load.Elapsed.TotalMilliseconds:F1} ms, " +
                                  $"{terrainTiles + variantTiles + resourceTiles + roadTiles} painted cells over {maps.Length} layers.");
        }

        /// <summary>
        /// The cost of panning the whole map corner to corner, which is what makes the tilemap renderer build and cull
        /// chunks. Two different numbers are recorded and they must not be confused:
        ///
        ///   * <c>sweep_*</c> — <c>Time.unscaledDeltaTime</c>. Batchmode has no window and no swap chain, so the loop
        ///     runs uncapped and this is NOT a frame budget; it is only evidence that the sweep ran and did not stall.
        ///   * <c>probe_*</c> — <see cref="RenderProbe"/>: the camera rendered into a real 1920×1080 RenderTexture with
        ///     a one-pixel readback afterwards, which blocks until the GPU has finished. That is an honest end-to-end
        ///     cost for one frame of this content, and it is what the report compares against the reference's worst
        ///     recorded frames (116.8 / 150.1 / 266.8 / 300.2 ms, TECHNICAL_ARCHITECTURE.md §3.3).
        /// </summary>
        [UnityTest, Timeout(600000)]
        public IEnumerator SweepingTheWholeMapIsMeasured()
        {
            PrototypeFixture.RequireGraphics();
            PrototypeFixture.RequirePaintedMap();
            yield return PrototypeFixture.Load(PrototypeFixture.R7);
            var sweep = Object.FindAnyObjectByType<CameraSweep>();
            Assert.NotNull(sweep, "R7-Tilemap.unity has no CameraSweep on its camera.");
            var camera = sweep.GetComponent<Camera>();
            var renderer = Object.FindAnyObjectByType<TilemapRenderer>();
            Assert.NotNull(renderer, "no TilemapRenderer in the R7 scene.");
            var chunkMode = renderer.mode.ToString();
            var chunkSize = renderer.chunkSize.ToString();
            var maxChunks = renderer.maxChunkCount;

            // Let the first frames settle: shader warm-up and the first chunk build are not steady-state cost.
            for (var i = 0; i < 30; i++) yield return null;

            var sampler = new FrameSampler("r7-sweep");
            var probeSampler = new FrameSampler("r7-probe");
            using (var probe = new RenderProbe())
            {
                probe.RenderMs(camera);                     // warm the render target and the shader variants
                sweep.Configure(Width, Height, SweepSeconds);
                sweep.Begin();
                while (sweep.Running)
                {
                    yield return null;
                    sampler.Add(Time.unscaledDeltaTime * 1000.0);
                    probeSampler.Add(probe.RenderMs(camera));
                }

                sweep.Place(0.5f);
                var png = probe.Capture(camera, "b14-r7-map.png");

                var values = new List<KeyValuePair<string, object>>
                {
                    new KeyValuePair<string, object>("prototype", "R7 tilemap"),
                    new KeyValuePair<string, object>("screenshot", System.IO.Path.GetFileName(png)),
                    new KeyValuePair<string, object>("pass", "camera sweep, no point lights"),
                    new KeyValuePair<string, object>("tiles_total", Tiles),
                    new KeyValuePair<string, object>("sweep_seconds", SweepSeconds),
                    new KeyValuePair<string, object>("view_tiles_high", 20),
                    new KeyValuePair<string, object>("graphics_device", SystemInfo.graphicsDeviceType.ToString()),
                    new KeyValuePair<string, object>("screen_width", Screen.width),
                    new KeyValuePair<string, object>("screen_height", Screen.height),
                    new KeyValuePair<string, object>("probe_width", probe.Width),
                    new KeyValuePair<string, object>("probe_height", probe.Height),
                    new KeyValuePair<string, object>("total_allocated_bytes", UnityEngine.Profiling.Profiler.GetTotalAllocatedMemoryLong()),
                    new KeyValuePair<string, object>("total_reserved_bytes", UnityEngine.Profiling.Profiler.GetTotalReservedMemoryLong()),
                    new KeyValuePair<string, object>("renderer_chunk_mode", chunkMode),
                    new KeyValuePair<string, object>("renderer_chunk_size", chunkSize),
                    new KeyValuePair<string, object>("renderer_max_chunk_count", maxChunks),
                    new KeyValuePair<string, object>("note_sweep", "unscaledDeltaTime in batchmode is uncapped and is not a frame budget"),
                    new KeyValuePair<string, object>("note_probe", "camera.Render into a RenderTexture plus a 1-px ReadPixels GPU fence"),
                };
                EvidenceWriter.AddFrames(values, "sweep", sampler);
                EvidenceWriter.AddFrames(values, "probe", probeSampler);
                EvidenceWriter.Write("b14-r7-frames.json", values);
                UnityEngine.Debug.Log("B14 R7: " + sampler.Summary());
                UnityEngine.Debug.Log("B14 R7 render probe: " + probeSampler.Summary());
            }

            Assert.Greater(sampler.Count, 10, "the sweep produced frames");
            Assert.Greater(probeSampler.Count, 10, "the render probe produced frames");
        }
    }
}
