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

        [Test] public void EveryDataPathTheGameLoadsGivesAPlacedLightItsQuarterMoreReach()
        {
            // REL-126. The tuning asset still carries the reference's radii, so — exactly as with the sun above —
            // any path that skipped DarkWorld would hand the game the smaller lights the owner asked us to leave
            // behind. Both halves are checked: the machine rows the light mask reads, and the tuning record the
            // opening advice quotes and the authored kerb streetlights take their radius from.
            var registry = AssetDatabase.LoadAssetAtPath<GameDataRegistry>(RegistryPath);
            Assert.That(registry, Is.Not.Null);

            var original = registry.BuildOriginal();
            Assert.That(original.TryMachine("lamp", out var referenceLamp), Is.True);
            Assert.That(referenceLamp.LightRadiusTiles, Is.EqualTo(4), "the asset still carries the reference's Lamp");

            foreach (var data in new[] { registry.Build(), registry.BuildLegacy() })
            {
                Assert.That(data.TryMachine("lamp", out var lamp), Is.True);
                Assert.That(data.TryMachine("arclamp", out var arc), Is.True);
                Assert.That(data.TryMachine("floodlight", out var flood), Is.True);

                Assert.That(lamp.LightRadiusTiles, Is.EqualTo(5).Within(1e-9));
                Assert.That(arc.LightRadiusTiles, Is.EqualTo(7.5).Within(1e-9));
                Assert.That(flood.ConeRangeTiles, Is.EqualTo(15).Within(1e-9));
                Assert.That(flood.ConeHalfAngleRad, Is.EqualTo(0.5235987755982988).Within(1e-9),
                    "wider reach, same cone — the owner asked for radius, not for a wider beam");

                Assert.That(data.Power.LampRadiusTiles, Is.EqualTo(lamp.LightRadiusTiles).Within(1e-9),
                    "the advice text quotes this and must not contradict the machine");
                Assert.That(data.Power.ArcLampRadiusTiles, Is.EqualTo(arc.LightRadiusTiles).Within(1e-9));
                Assert.That(data.Power.FloodlightRangeTiles, Is.EqualTo(flood.ConeRangeTiles).Within(1e-9));
                Assert.That(data.Power.StreetLightRadiusTiles, Is.EqualTo(8.75).Within(1e-9));
            }
        }
    }
}
