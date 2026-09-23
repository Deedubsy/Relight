using System.Collections.Generic;
using NUnit.Framework;
using Relight.Sim.Tests.Combat;
using Relight.Sim.UI;

namespace Relight.Sim.Tests.Campaign
{
    /// <summary>
    /// REL-127. The owner's note: "Buildings should have their own light, not large, but big enough to illuminate a
    /// small area around them, excludes walls and power poles." Asked whether that light should be real or only seen,
    /// they chose **seen only** (U-D-71 b).
    ///
    /// So there are two things to prove, and the second one matters more than the first: that the right kinds glow,
    /// and that the glow is invisible to the simulation. The second is the whole point of the answer — raiders
    /// hesitate on lit ground, so a real glow on every building would have made raids measurably easier without
    /// anyone deciding to.
    /// </summary>
    public sealed class BuildingGlowTests
    {
        private static List<ITickPhase> Phases() =>
            new List<ITickPhase> { new PowerPhase(), new LightPhase() };

        /// <summary>Every kind the owner's "buildings" means, and nothing else. Read from the catalogue, not a list of keys.</summary>
        private static readonly string[] Buildings =
        {
            "excavator", "pumpjack", "foundry", "refinery", "assembler", "assembler2", "mixer", "alienworkbench",
            "chest", "generator", "turret", "cannon", "depot",
        };

        [Test]
        public void TheBuildingsGlowAndNothingElseInTheCatalogueDoes()
        {
            var d = ReferenceData.Create();
            var glowing = new List<string>();
            foreach (var spec in d.Machines)
                if (BuildingGlow.Glows(spec)) glowing.Add(spec.Key);

            Assert.That(glowing, Is.EquivalentTo(Buildings),
                "the set of glowing kinds has moved; if a kind was added to the catalogue, decide whether it is a " +
                "building before changing this list");
        }

        [Test]
        public void TheOwnersOwnExclusionsDrawNothing()
        {
            var d = ReferenceData.Create();
            foreach (var key in new[] { "wall", "barricade", "pole", "bigpole", "substation" })
            {
                Assert.That(d.TryMachine(key, out var spec), Is.True, key);
                Assert.That(BuildingGlow.Glows(spec), Is.False, key + " must draw no glow");
            }
        }

        [Test]
        public void ARealLightNeverCarriesAFakeOne()
        {
            // A Lamp's light comes from the mask and goes out with its power. A glow drawn under a dead lamp would be
            // a lie about the grid, so the light kinds are excluded even though the Floodlight is two tiles wide.
            var d = ReferenceData.Create();
            foreach (var key in new[] { "lamp", "arclamp", "floodlight" })
            {
                Assert.That(d.TryMachine(key, out var spec), Is.True, key);
                Assert.That(BuildingGlow.Glows(spec), Is.False, key + " lights for real; it must not also glow");
            }
        }

        [Test]
        public void TheOneTileLogisticsPartsDrawNothing()
        {
            // Forty belt tiles each spilling 2.5 tiles is the "large" light the note rules out.
            var d = ReferenceData.Create();
            foreach (var key in new[] { "belt", "fastbelt", "underground", "splitter", "inserter" })
            {
                Assert.That(d.TryMachine(key, out var spec), Is.True, key);
                Assert.That(BuildingGlow.Glows(spec), Is.False, key + " is not a building");
            }
        }

        [Test]
        public void TheGlowIsStrongestAgainstTheWallAndGoneAtItsReach()
        {
            Assert.That(BuildingGlow.Strength(0), Is.EqualTo(BuildingGlow.Peak).Within(1e-9));
            Assert.That(BuildingGlow.Strength(-1), Is.EqualTo(BuildingGlow.Peak).Within(1e-9), "inside the footprint");
            Assert.That(BuildingGlow.Strength(BuildingGlow.ReachTiles / 2),
                Is.EqualTo(BuildingGlow.Peak / 2).Within(1e-9));
            Assert.That(BuildingGlow.Strength(BuildingGlow.ReachTiles), Is.EqualTo(0).Within(1e-9));
            Assert.That(BuildingGlow.Strength(BuildingGlow.ReachTiles + 5), Is.EqualTo(0).Within(1e-9));
            Assert.That(BuildingGlow.Peak, Is.LessThan(1),
                "a building must never light its surroundings as well as a Lamp does, or the Lamp is pointless");
        }

        [Test]
        public void TheGlowIsMeasuredFromTheEdgeSoEverySizeSpillsTheSame()
        {
            // A 2x2 chest at 10,10 and a 6x6 depot at 30,30. Two tiles out from the near edge of either one is the
            // same distance, which is why the rule measures the footprint and not the centre.
            Assert.That(BuildingGlow.Distance(10, 10, 2, 2, 10.5, 10.5), Is.EqualTo(0).Within(1e-9), "inside");
            Assert.That(BuildingGlow.Distance(10, 10, 2, 2, 14, 11), Is.EqualTo(2).Within(1e-9));
            Assert.That(BuildingGlow.Distance(30, 30, 6, 6, 38, 31), Is.EqualTo(2).Within(1e-9));
            Assert.That(BuildingGlow.Distance(30, 30, 6, 6, 29, 29), Is.EqualTo(System.Math.Sqrt(2)).Within(1e-9),
                "a corner is measured diagonally, so the spill is a rounded rectangle and not a cross");
        }

        [Test]
        public void ABuildingChangesNothingTheSimulationCanSee()
        {
            // The same lamp, the same grid, the same tick — once on empty ground and once inside a yard of buildings.
            // Supply chests draw no power, so the only thing that could differ is the glow, and it must not.
            var bare = Lit(place: false);
            var built = Lit(place: true);

            Assert.That(built.Sources, Is.EqualTo(bare.Sources), "a building must not become a light source");
            Assert.That(built.Fold, Is.EqualTo(bare.Fold), "the light fold must not move");
            Assert.That(built.Mask, Is.EqualTo(bare.Mask), "the lit mask must be identical, byte for byte");
        }

        [Test]
        public void TheGroundBesideABuildingIsStillDarkToEveryRuleThatAsks()
        {
            // The picture the owner asked for shows light beside a Foundry. LitAt is what raider hesitation, turret
            // sight and the engineer's own lit flag all read, and it must still say dark.
            var ctx = RaidFixture.Context();
            var st = RaidFixture.State(ctx);
            RaidFixture.Add(ctx, st, "foundry", 40, 40);
            RaidFixture.Run(ctx, st, 1, Phases());

            for (var y = 38; y <= 45; y++)
                for (var x = 38; x <= 45; x++)
                    Assert.That(LightQueries.LitAt(st, x, y), Is.False,
                        $"({x},{y}) beside the foundry must read unlit to the simulation");
        }

        /// <summary>One tick of a powered lamp, optionally with a yard of chests around it, and what the sim can see of it.</summary>
        private static (int Sources, int Fold, byte[] Mask) Lit(bool place)
        {
            var ctx = RaidFixture.Context();
            var st = RaidFixture.State(ctx);
            RaidFixture.Add(ctx, st, "lamp", 20, 20);
            RaidFixture.Add(ctx, st, "pole", 20, 22);
            RaidFixture.Add(ctx, st, "generator", 20, 24).Inv.Add(ItemId.Coal, 50);
            if (place)
            {
                RaidFixture.Add(ctx, st, "chest", 17, 17);
                RaidFixture.Add(ctx, st, "chest", 22, 17);
                RaidFixture.Add(ctx, st, "chest", 17, 22);
                RaidFixture.Add(ctx, st, "chest", 23, 20);
            }

            RaidFixture.Run(ctx, st, 1, Phases());

            var lights = new List<Light>();
            LightSources.Collect(ctx, st, lights);
            var mask = LightQueries.Mask(st);
            var copy = new byte[mask.Length];
            System.Array.Copy(mask, copy, mask.Length);
            return (lights.Count, LightSources.Fold(lights), copy);
        }
    }
}
