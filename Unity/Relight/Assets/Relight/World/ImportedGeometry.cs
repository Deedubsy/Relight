using Relight.Sim;
using UnityEngine;

namespace Relight.World
{
    /// <summary>
    /// C-01. The bridge from the imported asset to the sim's world (U-M-14: geometry is injected as data, never
    /// compiled in). Nothing here interprets the region — it hands the arrays straight to
    /// <see cref="RegionGeometry"/>, which is engine-free and unit-tested.
    ///
    /// No Y flip happens here: the sim is Y-down and region-local. The flip lives in
    /// <see cref="WorldSpace.Cell"/> alone (WORLD_AND_ASSETS.md §2.2).
    /// </summary>
    public static class ImportedGeometry
    {
        /// <summary>
        /// Build the sim geometry for an imported region. Returns null and logs when the asset is missing or its
        /// arrays do not agree with its size, so a bad asset fails at start-up instead of mid-tick.
        /// </summary>
        public static ICityGeometry Build(WorldGeometryAsset asset)
        {
            if (asset == null)
            {
                Debug.LogError("Relight: ImportedGeometry.Build got no WorldGeometryAsset.");
                return null;
            }
            var n = asset.Width * asset.Height;
            if (n <= 0 || asset.Kind == null || asset.Kind.Length != n)
            {
                Debug.LogError($"Relight: '{asset.name}' has {asset.Width}x{asset.Height} tiles but " +
                               $"{(asset.Kind == null ? 0 : asset.Kind.Length)} kind entries; re-run Relight/World/Import Home Region.");
                return null;
            }
            var solid = asset.Solid != null && asset.Solid.Length == n ? asset.Solid : new byte[n];
            var variant = asset.Variant != null && asset.Variant.Length == n ? asset.Variant : null;
            var patch = asset.Patch != null && asset.Patch.Length == n ? asset.Patch : null;
            var spawn = new Vec2(asset.Spawn.x + 0.5, asset.Spawn.y + 0.5);

            return RegionGeometry.FromArrays(asset.Width, asset.Height, asset.OriginX, asset.OriginY,
                asset.Kind, solid, variant, patch, spawn);
        }
    }
}
