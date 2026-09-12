using UnityEngine;
using UnityEngine.SceneManagement;

namespace Relight.Presentation
{
    /// <summary>
    /// Entry point of the Boot scene (TECHNICAL_ARCHITECTURE.md §7.1). It performs the
    /// one-off boot work and then loads the next scene. It builds nothing in the world:
    /// scenes are authored in the Editor, never constructed at runtime.
    /// </summary>
    public sealed class BootController : MonoBehaviour
    {
        [Tooltip("Scene loaded once boot work is complete.")]
        [SerializeField] private string nextSceneName = "MainMenu";

        [Tooltip("Frames to wait before loading the next scene, so the Boot scene is visible in Play Mode.")]
        [SerializeField, Min(0)] private int holdFrames = 1;

        private void Start()
        {
            StartCoroutine(Boot());
        }

        private System.Collections.IEnumerator Boot()
        {
            for (var i = 0; i < holdFrames; i++) yield return null;
            if (string.IsNullOrEmpty(nextSceneName)) yield break;
            if (Application.CanStreamedLevelBeLoaded(nextSceneName))
                SceneManager.LoadScene(nextSceneName, LoadSceneMode.Single);
            else
                Debug.LogError($"BootController: scene '{nextSceneName}' is not in Build Settings.");
        }
    }
}
