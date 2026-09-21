using System.Collections;
using System.IO;
using NUnit.Framework;
using Relight.Presentation;
using Relight.UI.FrontEnd;
using UnityEngine;
using UnityEngine.TestTools;

/// <summary>
/// PER-01, second line of defence. The editor's test-run hook redirects saves before Play Mode starts; if that hook
/// ever fails to fire, this fixture — in no namespace, so it covers every test in the assembly — redirects them
/// before the first test runs. Either way no PlayMode test can reach the owner's real saves.
/// </summary>
[SetUpFixture]
public sealed class SaveRootGuard
{
    [OneTimeSetUp]
    public void Redirect()
    {
        if (SaveRoot.IsRedirected) return;
        var root = Path.GetFullPath(Path.Combine(Application.dataPath, "../Temp/relight-test-saves")).Replace('\\', '/');
        Directory.CreateDirectory(root);
        SaveRoot.Override(root);
    }

    [OneTimeTearDown]
    public void Restore() => SaveRoot.Clear();
}

namespace Relight.Tests.Play
{
    /// <summary>PER-01: under a test run, both save-root sites point away from <c>Application.persistentDataPath</c>.</summary>
    public sealed class SaveRootTests
    {
        private static string Real => Application.persistentDataPath.Replace('\\', '/');

        [Test]
        public void ATestRun_RedirectsTheSaveRoot()
        {
            Assert.That(SaveRoot.IsRedirected, Is.True, "saves are not redirected during a test run.");
            Assert.That(SaveRoot.Path.Replace('\\', '/'), Does.Not.StartWith(Real));
        }

        [UnityTest, Timeout(120000)]
        public IEnumerator TheWorldsAutosaveController_UsesTheRedirectedRoot()
        {
            yield return SceneFixture.LoadWorld();
            var saver = Object.FindAnyObjectByType<AutosaveController>();
            Assert.That(saver, Is.Not.Null, "World.unity has no AutosaveController.");
            Assert.That(saver.Store.Root, Is.EqualTo(SaveRoot.Path));
            Assert.That(saver.Store.Directory.Replace('\\', '/'), Does.Not.StartWith(Real));
        }

        [UnityTest, Timeout(120000)]
        public IEnumerator TheTitleScreen_UsesTheRedirectedRoot()
        {
            // No UIDocument: the controller binds nothing and only builds its save gateway.
            var go = new GameObject("title-under-test");
            var title = go.AddComponent<TitleScreenController>();
            try
            {
                Assert.That(title.SaveDirectory, Is.Not.Null);
                Assert.That(title.SaveDirectory.Replace('\\', '/'), Does.Not.StartWith(Real));
            }
            finally
            {
                Object.Destroy(go);
            }
            yield return null;
        }
    }
}
