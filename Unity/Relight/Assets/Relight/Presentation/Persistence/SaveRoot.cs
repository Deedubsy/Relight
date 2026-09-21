using UnityEngine;

namespace Relight.Presentation
{
    /// <summary>
    /// The one place that says which folder the game's saves hang under (<c>{root}/saves/{profile}/…</c>, §9.4.1).
    /// A build and the owner's own Play sessions get <see cref="Application.persistentDataPath"/>. A test run or an
    /// assistant's editor session can point it somewhere else, so neither writes into the owner's real saves
    /// (TASKS.md log 2026-09-20; PER-01).
    ///
    /// Two ways to redirect it, both editor-side and neither reachable from a build's player-facing path:
    /// <list type="bullet">
    /// <item><see cref="Override"/>: a static, for a test fixture. It is cleared whenever Play Mode starts, so a
    ///       stale value cannot outlive its run even with domain reload off;</item>
    /// <item>the editor session key <see cref="SessionKey"/>: it survives the domain reload that entering Play Mode
    ///       causes, which a static does not. The test-run hook in <c>Relight.Editor</c> sets it when a run starts
    ///       and erases it when the run ends; it dies with the editor process either way.</item>
    /// </list>
    /// Every component that builds a <c>SaveStore</c> or a <c>SaveGateway</c> from a folder asks here.
    /// </summary>
    public static class SaveRoot
    {
        /// <summary>The <c>UnityEditor.SessionState</c> key holding a redirected root; empty means none.</summary>
        public const string SessionKey = "Relight.SaveRootOverride";

        private static string _override;

        /// <summary>The folder the saves tree hangs under, right now.</summary>
        public static string Path
        {
            get
            {
                if (!string.IsNullOrEmpty(_override)) return _override;
#if UNITY_EDITOR
                var session = UnityEditor.SessionState.GetString(SessionKey, "");
                if (!string.IsNullOrEmpty(session)) return session;
#endif
                return Application.persistentDataPath;
            }
        }

        /// <summary>True while saves go anywhere but the real folder.</summary>
        public static bool IsRedirected => Path != Application.persistentDataPath;

        /// <summary>Point saves at <paramref name="root"/> until <see cref="Clear"/> or the next Play Mode start.</summary>
        public static void Override(string root) => _override = root;

        public static void Clear() => _override = null;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetOnPlay()
        {
            _override = null;
            if (IsRedirected) Debug.LogWarning("Saves are redirected for this editor session: " + Path);
        }
    }
}
