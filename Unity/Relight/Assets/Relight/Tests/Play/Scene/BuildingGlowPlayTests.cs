using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using Relight.Presentation;
using Relight.Sim;
using Relight.UI;
using UnityEngine;
using UnityEngine.TestTools;

namespace Relight.Tests.Play
{
    /// <summary>
    /// REL-127, in the authored scene. The owner asked for buildings to give off a small light of their own, and
    /// chose "seen only" when asked whether the simulation should see it (U-D-71 b).
    ///
    /// <c>BuildingGlowTests</c> in the sim suite proves the rule and proves the mask does not move. This
    /// proves the other half — that the drawing is actually wired up — because a glow nobody draws would pass every
    /// one of those tests.
    ///
    /// Both readings are taken as a difference across one placement rather than as absolute values, because the
    /// authored Home already stands inside the visible rectangle and its own buildings glow too. The difference is
    /// the chest's contribution and nothing else's.
    /// </summary>
    public sealed class BuildingGlowPlayTests
    {
        [UnityTest, Timeout(60000)]
        public IEnumerator AChestLightsTheGroundBesideItAndNothingFurtherOff()
        {
            yield return SceneFixture.LoadWorld();
            var host = Object.FindAnyObjectByType<SimHost>();
            var lighting = Object.FindAnyObjectByType<LightingPresenter>();
            Assert.That(host, Is.Not.Null, "World.unity has no SimHost.");
            Assert.That(lighting, Is.Not.Null, "World.unity has no LightingPresenter.");

            var shell = Object.FindAnyObjectByType<UiShell>();
            if (shell != null) shell.CloseActive();
            host.Paused = true;               // freeze the engineer, so the camera rectangle holds still between reads
            yield return null;
            yield return null;

            if (!lighting.Dark) Assert.Ignore("the darkness overlay is not being drawn; that is not this test's subject.");

            var sim = host.Simulation;
            var ctx = sim.Context;
            var st = sim.State;
            var (tx, ty) = SceneFixture.FreeTile(sim, "chest", 0);

            // One tile clear of the 2x2 chest's east edge, and eight tiles clear of it.
            var beside = new Vec2(tx + 3.0, ty + 1.0);
            var away = new Vec2(tx + 10.0, ty + 1.0);

            // The flashlight follows the pointer and also subtracts darkness, so it is cleared immediately before
            // each pair of reads. No frame runs in between, so nothing points it again.
            lighting.ClearBeam();
            var besideBefore = lighting.Reveal(beside);
            var awayBefore = lighting.Reveal(away);

            // Fully qualified: UnityEngine.Light is also in scope here, and a sim light is not a scene light.
            var lights = new List<Relight.Sim.Light>();
            LightSources.Collect(ctx, st, lights);
            var sourcesBefore = lights.Count;

            var placed = sim.Apply(new PlaceMachineCommand("chest", tx, ty, Dir.N));
            if (!placed.Accepted) Assert.Ignore("could not stand a chest beside the engineer: " + placed.Problem);
            yield return null;
            yield return null;

            lighting.ClearBeam();
            var besideAfter = lighting.Reveal(beside);
            var awayAfter = lighting.Reveal(away);

            // What the owner asked for: the ground next to the building is lighter than it was.
            var expected = (float)Relight.Sim.UI.BuildingGlow.Strength(1.0);
            Assert.That(besideAfter, Is.GreaterThan(besideBefore),
                "the ground one tile east of the chest is no lighter than it was before the chest was there.");
            Assert.That(besideAfter, Is.GreaterThanOrEqualTo(expected - 1e-4),
                "the glow beside the chest is weaker than BuildingGlow.Strength says it should be.");

            // "Not large": eight tiles off, the picture is exactly as it was.
            Assert.That(awayAfter, Is.EqualTo(awayBefore).Within(1e-6),
                "the chest changed the picture eight tiles away; the glow is not staying inside its reach.");

            // "Seen only": the simulation gained no light source.
            LightSources.Collect(ctx, st, lights);
            Assert.That(lights.Count, Is.EqualTo(sourcesBefore),
                "placing a chest added a light source; the glow has reached the simulation.");
        }
    }
}
