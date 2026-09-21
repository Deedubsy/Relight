using NUnit.Framework;
using Relight.Data;
using Relight.Sim;
using Relight.World;
using UnityEditor;

namespace Relight.Authoring.Tests
{
    /// <summary>
    /// The REAL CITY as a sim context, for tests that can only be shown on the map the game is played on.
    ///
    /// The context is built exactly as <c>WorldBootstrap.ContextForLayout(version, useScene: false)</c> builds it
    /// for a new game: the same four assets World.unity points at, the same opening resource layout, the same
    /// balance tables. It needs the asset database, so it lives in the Editor test assembly and the offline dotnet
    /// suite cannot run it.
    /// </summary>
    public static class RealCityFixture
    {
        public const string Geometry = "Assets/Relight/World/Generated/Full/FullGeometry.asset";
        public const string Sites = "Assets/Relight/World/Generated/Full/FullSites.asset";
        public const string Overrides = "Assets/Relight/World/Manual/HomeSitesOverrides.asset";
        public const string Registry = "Assets/Relight/Data/GameDataRegistry.asset";

        /// <summary>
        /// The context. <paramref name="opening"/> is a geometry asset made for it: the caller destroys it
        /// (<c>Object.DestroyImmediate</c>) when the test is over.
        /// </summary>
        public static SimContext Context(out WorldGeometryAsset opening)
        {
            var geometry = AssetDatabase.LoadAssetAtPath<WorldGeometryAsset>(Geometry);
            var sites = AssetDatabase.LoadAssetAtPath<HomeSitesAsset>(Sites);
            var overrides = AssetDatabase.LoadAssetAtPath<HomeSitesOverrides>(Overrides);
            var registry = AssetDatabase.LoadAssetAtPath<GameDataRegistry>(Registry);
            Assert.That(geometry, Is.Not.Null, Geometry);
            Assert.That(sites, Is.Not.Null, Sites);
            Assert.That(overrides, Is.Not.Null, Overrides);
            Assert.That(registry, Is.Not.Null, Registry);

            var siteList = SiteBridge.ToSim(HomeSites.Resolve(sites, overrides), geometry.RegionId, geometry.OriginX, geometry.OriginY);
            opening = OpeningResourceLayout.Build(geometry, siteList, out siteList);
            var map = ImportedGeometry.Build(opening);
            Assert.That(map, Is.Not.Null, "the full-city geometry asset did not build");
            Assert.That((map.Width, map.Height), Is.EqualTo((864, 576)), "this must be the real city, not a crop");
            return new SimContext(registry.Build(), map, threat: new EnemyThreatLayer(), sites: siteList, mapId: opening.MapId);
        }
    }
}
