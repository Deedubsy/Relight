using Relight.Data;
using Relight.Sim;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Relight.Presentation
{
    /// <summary>
    /// B-13. Starts a session in <c>World.unity</c> and loads the UI scene on top of it.
    ///
    /// This is NOT the "monolithic runtime constructor" TECHNICAL_ARCHITECTURE.md §7.1 forbids: it creates no
    /// GameObject, no camera, no tilemap and no view. Everything visible is authored in the scene asset (the editor
    /// menu <c>Relight/Setup/Build World Scene</c> writes it once, a human edits it afterwards). All this does is
    /// hand <see cref="SimHost"/> the two things only code can build — the data record from the B-05 registry and
    /// the Phase B synthetic geometry — and pick the seed.
    ///
    /// The UI scene is loaded additively from here rather than from <c>Boot</c> because the world scene is what the
    /// Editor's Play button runs when a developer opens it directly, and the panel has to be there when it does.
    /// Boot loading both would work equally well and would be the better answer once there is a main menu flow that
    /// always precedes the world (B-14); the report records the choice.
    /// </summary>
    [DisallowMultipleComponent]
    [AddComponentMenu("Relight/World Bootstrap")]
    public sealed class WorldBootstrap : MonoBehaviour
    {
        [Tooltip("The host this starts. Found on this object if left empty.")]
        [SerializeField] private SimHost host;

        [Tooltip("The B-05 data registry asset. Its Build() is the sim's GameData.")]
        [SerializeField] private GameDataRegistry registry;

        [Tooltip("Campaign seed. Same seed plus same commands plus same data = same state (TA 10.4).")]
        [SerializeField] private int seed = 1;

        [Tooltip("Start a session on Awake. Off lets a menu or a load call StartSession() instead.")]
        [SerializeField] private bool startOnAwake = true;

        [Tooltip("Scene loaded additively for the UI. Empty loads nothing (it may already be open).")]
        [SerializeField] private string uiSceneName = "GameUI";

        /// <summary>The seed the session was started with, for the report line in a debug panel.</summary>
        public int Seed => seed;

        private void Awake()
        {
            if (host == null) host = GetComponent<SimHost>();
            if (startOnAwake) StartSession();
        }

        private void Start()
        {
            if (string.IsNullOrEmpty(uiSceneName)) return;
            if (SceneManager.GetSceneByName(uiSceneName).isLoaded) return;
            if (!Application.CanStreamedLevelBeLoaded(uiSceneName))
            {
                Debug.LogError($"Relight: UI scene '{uiSceneName}' is not in Build Settings.");
                return;
            }
            SceneManager.LoadSceneAsync(uiSceneName, LoadSceneMode.Additive);
        }

        /// <summary>Build the context and start a new campaign. Safe to call again; the host replaces its simulation.</summary>
        public Simulation StartSession()
        {
            if (host == null)
            {
                Debug.LogError("Relight: WorldBootstrap has no SimHost.");
                return null;
            }
            if (registry == null)
            {
                Debug.LogError("Relight: WorldBootstrap has no GameDataRegistry; assign the asset in the Inspector.");
                return null;
            }
            // A new session starts with fresh view state: the static survives a domain reload, the session does not.
            ViewState.Reset();
            var ctx = new SimContext(registry.Build(), SyntheticMap.Create());
            return host.StartNewGame(ctx, seed);
        }
    }
}
