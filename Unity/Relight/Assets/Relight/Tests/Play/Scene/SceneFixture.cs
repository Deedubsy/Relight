using System.Collections;
using System.Collections.Generic;
using Relight.Presentation;
using Relight.Sim;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace Relight.Tests.Play
{
    /// <summary>Loads the authored scenes the way the game does and waits until the session is live.</summary>
    public static class SceneFixture
    {
        public const string World = "World";
        public const string GameUi = "GameUI";

        /// <summary>Load World (which loads GameUI additively itself) and wait for the host and the UI document.</summary>
        public static IEnumerator LoadWorld()
        {
            yield return SceneManager.LoadSceneAsync(World, LoadSceneMode.Single);
            // WorldBootstrap.Start begins the additive UI load; give it frames to finish.
            var deadline = Time.realtimeSinceStartupAsDouble + 10.0;
            while (Time.realtimeSinceStartupAsDouble < deadline)
            {
                var host = Object.FindAnyObjectByType<SimHost>();
                if (host != null && host.Simulation != null && SceneManager.GetSceneByName(GameUi).isLoaded)
                {
                    yield return null;  // one more frame so OnEnable/Bind has run in the UI scene
                    yield return null;
                    yield break;
                }
                yield return null;
            }
            throw new System.TimeoutException("World/GameUI did not finish loading within 10 s.");
        }
            /// <summary>
        /// Phase C: World.unity runs the imported Home region (C-01), not the Phase B synthetic map. A fresh game
        /// there already holds the Home Depot, and the tiles near the map origin are city structures that
        /// <see cref="Placement.GeometryProblem"/> refuses. A test that places a machine therefore asks for the
        /// <paramref name="index"/>-th free footprint near the engineer's spawn instead of a hard-coded tile; the
        /// candidates are kept three tiles apart so two of them never overlap. Works on both maps.
        /// </summary>
        public static (int x, int y) FreeTile(Simulation sim, string kind, int index)
        {
            var ctx = sim.Context;
            var st = sim.State;
            var cx = (int)System.Math.Floor(st.Engineer.Pos.X);
            var cy = (int)System.Math.Floor(st.Engineer.Pos.Y);
            var kept = new List<(int x, int y)>();
            for (var r = 0; r <= 24; r++)
            for (var y = cy - r; y <= cy + r; y++)
            for (var x = cx - r; x <= cx + r; x++)
            {
                if (System.Math.Max(System.Math.Abs(x - cx), System.Math.Abs(y - cy)) != r) continue;
                if (Placement.GeometryProblem(ctx, st, kind, x, y, Dir.N).Length != 0) continue;
                var clash = false;
                foreach (var k in kept)
                    if (System.Math.Abs(k.x - x) < 3 && System.Math.Abs(k.y - y) < 3) { clash = true; break; }
                if (clash) continue;
                kept.Add((x, y));
                if (kept.Count > index) return kept[index];
            }
            throw new System.InvalidOperationException($"no free {kind} footprint #{index} within 24 tiles of the spawn.");
        }

        /// <summary>The last machine of <paramref name="kind"/> in the state: the one a test just placed.</summary>
        public static Machine Last(Simulation sim, string kind)
        {
            for (var i = sim.State.Machines.Count - 1; i >= 0; i--)
                if (sim.State.Machines[i].Kind == kind) return sim.State.Machines[i];
            return null;
        }
    }
}
