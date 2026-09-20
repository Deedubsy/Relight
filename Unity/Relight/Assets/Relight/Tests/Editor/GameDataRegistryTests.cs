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

        [Test] public void EveryDataPathTheGameLoadsGivesTheGunTurretItsDarkSight()
        {
            // U-D-59: the turret asset carries no dark sight (0 = "take DarkWorld's"), so a data path that skipped
            // DarkWorld would hand the game a turret that sees 9 tiles into the dark and the rule would be off.
            var registry = AssetDatabase.LoadAssetAtPath<GameDataRegistry>(RegistryPath);
            Assert.That(registry, Is.Not.Null);
            foreach (var data in new[] { registry.Build(), registry.BuildLegacy() })
            {
                Assert.That(data.TryTurret("turret", out var gun), Is.True);
                Assert.That(gun.RangeTiles, Is.EqualTo(9));
                Assert.That(gun.DarkSightTiles, Is.EqualTo(6));
                Assert.That(data.Raids.LitStepCost, Is.EqualTo(4));
                Assert.That(data.Raids.LitNoticeMul, Is.EqualTo(0.6).Within(1e-9));
            }
        }
    }
}
