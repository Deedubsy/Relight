using System.Collections;
using Relight.Presentation;
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
    }
}
