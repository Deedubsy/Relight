using NUnit.Framework;
using Relight.Data;
using UnityEditor;
namespace Relight.Authoring.Tests
{
    public sealed class GameDataRegistryTests
    {
        const string RegistryPath = "Assets/Relight/Data/GameDataRegistry.asset";

        [Test] public void EveryDataPathTheGameLoadsHasNoSun()
        {
            // U-D-58: the tuning asset keeps the reference's sun (daylightSeconds 900), so every path the game
            // builds its data through must apply DarkWorld, including the one a pre-layout save loads with.
            var registry = AssetDatabase.LoadAssetAtPath<GameDataRegistry>(RegistryPath);
            Assert.That(registry, Is.Not.Null);
            Assert.That(registry.BuildOriginal().Time.DaylightSeconds, Is.GreaterThan(0), "the asset still carries the reference's sun");
            Assert.That(registry.Build().Time.DaylightSeconds, Is.Zero, "new games");
            Assert.That(registry.BuildLegacy().Time.DaylightSeconds, Is.Zero, "saves from before the opening resource layout");
        }
    }
}
