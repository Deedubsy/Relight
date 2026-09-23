using System.Collections;
using NUnit.Framework;
using Relight.Presentation;
using Relight.Sim;
using Relight.UI;
using UnityEngine;
using UnityEngine.TestTools;

namespace Relight.Tests.Play
{
    /// <summary>
    /// REL-131, in the authored scene. <c>TorchShadowTests</c> in the sim suite proves the shared shadow rule;
    /// this proves the other half — that the cone on screen is actually gated by it, which a rule nobody consulted
    /// would still pass.
    ///
    /// It uses an existing building rather than a placed wall, so it needs no inventory and cannot skip itself:
    /// the owner asked for <i>"existing and placed walls"</i>, and both stand on the one <c>LightRules.Opaque</c>
    /// rule the sim tests cover from the placed side.
    ///
    /// The facade it picks is kept well away from any placed machine, so the buildings' own glow (REL-127) is zero
    /// at every point read and the whole of each reading is the beam. Each test checks that before it checks
    /// anything else.
    /// </summary>
    public sealed class TorchShadowPlayTests
    {
        /// <summary>Far enough that no placed machine's glow can reach the row: the largest footprint is 6 and the glow spills 2.5.</summary>
        private const int GlowFree = 12;

        /// <summary>
        /// A building facade with clear ground to its west: the tile itself stops light and the five tiles west of
        /// it do not, so the only thing that can stop a beam fired east along that row is the facade. Nothing placed
        /// stands within <see cref="GlowFree"/> tiles, so REL-127's glow contributes nothing to the readings.
        /// </summary>
        private static bool Facade(SimContext ctx, SimState st, Vec2 near, out int wx, out int wy)
        {
            wx = 0;
            wy = 0;
            for (var r = 2; r <= 60; r++)
                for (var y = Mathf.FloorToInt((float)near.Y) - r; y <= Mathf.FloorToInt((float)near.Y) + r; y++)
                    for (var x = Mathf.FloorToInt((float)near.X) - r; x <= Mathf.FloorToInt((float)near.X) + r; x++)
                    {
                        if (Mathf.Max(Mathf.Abs(x - Mathf.FloorToInt((float)near.X)),
                                Mathf.Abs(y - Mathf.FloorToInt((float)near.Y))) != r) continue;
                        if (x - 5 < 0 || y < 0 || x + 4 >= ctx.Geometry.Width || y >= ctx.Geometry.Height) continue;
                        if (!LightRules.Opaque(ctx, st, x, y)) continue;

                        var clear = true;
                        for (var k = 1; k <= 5 && clear; k++) clear = !LightRules.Opaque(ctx, st, x - k, y);
                        if (!clear) continue;

                        var quiet = true;
                        foreach (var m in st.Machines)
                            if (Mathf.Abs(m.X - x) < GlowFree && Mathf.Abs(m.Y - y) < GlowFree) { quiet = false; break; }
                        if (!quiet) continue;

                        wx = x;
                        wy = y;
                        return true;
                    }
            return false;
        }

        /// <summary>Load the scene, stop the pointer pointing the beam, and find the facade. Shared by both tests.</summary>
        private static IEnumerator Setup(System.Action<LightingPresenter, SimContext, SimState, int, int> body)
        {
            yield return SceneFixture.LoadWorld();
            var host = Object.FindAnyObjectByType<SimHost>();
            var lighting = Object.FindAnyObjectByType<LightingPresenter>();
            var flash = Object.FindAnyObjectByType<MouseFlashlightPresenter>();
            Assert.That(host, Is.Not.Null, "World.unity has no SimHost.");
            Assert.That(lighting, Is.Not.Null, "World.unity has no LightingPresenter.");
            Assert.That(flash, Is.Not.Null, "World.unity has no MouseFlashlightPresenter.");

            var shell = Object.FindAnyObjectByType<UiShell>();
            if (shell != null) shell.CloseActive();
            host.Paused = true;
            // The beam is pointed by hand below, so the pointer must not point it again between the frame that
            // builds the shadow and the frame that reads it.
            flash.enabled = false;
            yield return null;

            var sim = host.Simulation;
            Assert.That(Facade(sim.Context, sim.State, sim.State.Engineer.Pos, out var wx, out var wy), Is.True,
                "no building facade with open ground to its west and nothing placed nearby was found within 60 tiles of the engineer.");
            body(lighting, sim.Context, sim.State, wx, wy);
        }

        private static void Aim(LightingPresenter lighting, Vec2 from) =>
            lighting.SetBeam(from, new Vec2(1, 0), MouseFlashlightPresenter.ReachTiles,
                MouseFlashlightPresenter.HalfAngleRad, MouseFlashlightPresenter.GlowTiles);

        [UnityTest, Timeout(60000)]
        public IEnumerator TheConeStopsAtAWallAndTheFaceOfTheWallIsStillLit()
        {
            LightingPresenter lighting = null;
            var wx = 0;
            var wy = 0;
            yield return Setup((l, c, s, x, y) => { lighting = l; wx = x; wy = y; });

            var front = new Vec2(wx - 0.5, wy + 0.5);       // the open tile in front of the facade
            var face = new Vec2(wx + 0.5, wy + 0.5);        // the facade itself
            var behind = new Vec2(wx + 1.5, wy + 0.5);      // one tile past it

            // Nothing placed is near, so with no beam there is no light here at all; that is what makes each
            // reading below the beam's own contribution and nothing else's.
            lighting.ClearBeam();
            Assert.That(lighting.Reveal(front), Is.EqualTo(0f).Within(1e-4), "something other than the beam is lighting this row.");
            Assert.That(lighting.Reveal(behind), Is.EqualTo(0f).Within(1e-4), "something other than the beam is lighting this row.");

            Aim(lighting, new Vec2(wx - 3.5, wy + 0.5));
            yield return null;                               // LateUpdate builds the shadow for this beam

            Assert.That(lighting.Reveal(front), Is.GreaterThan(0.01f),
                "the beam is not reaching the ground in front of the wall, so the reading behind it proves nothing.");
            Assert.That(lighting.Reveal(face), Is.GreaterThan(0.01f),
                "the face of the wall is dark — the near end tile should still catch the beam, as it does for a lamp.");
            Assert.That(lighting.Reveal(behind), Is.EqualTo(0f).Within(1e-4),
                "the cone is still showing on the far side of the wall.");
        }

        [UnityTest, Timeout(60000)]
        public IEnumerator PressedAgainstAWallTheLightAtYourOwnFeetStays()
        {
            LightingPresenter lighting = null;
            var wx = 0;
            var wy = 0;
            yield return Setup((l, c, s, x, y) => { lighting = l; wx = x; wy = y; });

            // Standing in the tile touching the facade, nose to the wall. 1.6 tiles ahead is past the facade and
            // still inside the 1.75-tile origin glow, which is the one place the two rules meet.
            var underfoot = new Vec2(wx + 1.1, wy + 0.5);
            var further = new Vec2(wx + 3.5, wy + 0.5);

            Aim(lighting, new Vec2(wx - 0.5, wy + 0.5));
            yield return null;

            Assert.That(lighting.Reveal(underfoot), Is.GreaterThan(0.01f),
                "standing with your nose to a wall put out the light at your own feet.");
            Assert.That(lighting.Reveal(further), Is.EqualTo(0f).Within(1e-4),
                "past the origin glow the wall should stop the beam, and it did not.");
        }
    }
}
