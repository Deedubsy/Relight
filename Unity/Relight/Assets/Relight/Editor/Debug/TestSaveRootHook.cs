using System.IO;
using Relight.Presentation;
using UnityEditor;
using UnityEditor.TestTools.TestRunner.Api;
using UnityEngine;

namespace Relight.Editor
{
    /// <summary>
    /// PER-01. While a test run is in progress the game's saves go to a throwaway folder under the project's
    /// <c>Temp/</c>, never to the owner's real saves. The run starts in the editor domain and a PlayMode run then
    /// reloads the domain, so the redirect travels in <see cref="SessionState"/> (<see cref="SaveRoot.SessionKey"/>),
    /// not in a static. Each run starts from an empty folder, so a test never sees another run's — or the owner's —
    /// files.
    /// </summary>
    [InitializeOnLoad]
    internal static class TestSaveRootHook
    {
        private static readonly Callbacks Hook = new Callbacks();

        static TestSaveRootHook()
        {
            var api = ScriptableObject.CreateInstance<TestRunnerApi>();
            api.RegisterCallbacks(Hook);
        }

        /// <summary>The throwaway root: <c>{project}/Temp/relight-test-saves</c>.</summary>
        public static string Root =>
            Path.GetFullPath(Path.Combine(Application.dataPath, "../Temp/relight-test-saves")).Replace('\\', '/');

        private sealed class Callbacks : ICallbacks
        {
            public void RunStarted(ITestAdaptor testsToRun)
            {
                var root = Root;
                try
                {
                    if (Directory.Exists(root)) Directory.Delete(root, true);
                    Directory.CreateDirectory(root);
                }
                catch (System.Exception e)
                {
                    Debug.LogWarning("Could not empty the test save folder " + root + ": " + e.Message);
                }
                SessionState.SetString(SaveRoot.SessionKey, root);
            }

            public void RunFinished(ITestResultAdaptor result) => SessionState.EraseString(SaveRoot.SessionKey);

            public void TestStarted(ITestAdaptor test) { }

            public void TestFinished(ITestResultAdaptor result) { }
        }
    }
}
