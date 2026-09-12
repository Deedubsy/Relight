using System.Collections;
using NUnit.Framework;
using Relight.Presentation;
using Relight.UI;
using UnityEngine;
using UnityEngine.TestTools;

namespace Relight.Tests.Play
{
    /// <summary>
    /// B-13 acceptance, test 2 (D-UI-10, TECHNICAL_ARCHITECTURE.md §8.3 row 5; the reference's own note at
    /// worldScene.ts:921 "Menus overlay the city. Their dimensions must not move the follow target"): opening and
    /// closing a panel must not move the camera by any amount at all.
    ///
    /// The sim is paused for the duration so the engineer cannot move; anything the camera does here is the UI's
    /// doing, and the assertion is exact equality rather than a tolerance.
    /// </summary>
    public sealed class CameraInsetTests
    {
        [UnityTest, Timeout(30000)]
        public IEnumerator OpeningAndClosingAPanelNeverMovesTheCamera()
        {
            yield return SceneFixture.LoadWorld();
            var host = Object.FindAnyObjectByType<SimHost>();
            var shell = Object.FindAnyObjectByType<UiShell>();
            var rig = Object.FindAnyObjectByType<CameraRig>();
            Assert.That(shell, Is.Not.Null, "GameUI.unity has no UiShell.");
            Assert.That(rig, Is.Not.Null, "World.unity has no CameraRig.");

            host.Paused = true;
            shell.CloseActive();
            for (var i = 0; i < 5; i++) yield return null;   // let the follow settle on the resting engineer

            var camera = rig.GetComponent<Camera>();
            var position = rig.transform.position;
            var rotation = rig.transform.rotation;
            var size = camera.orthographicSize;
            var rect = camera.rect;

            Assert.That(shell.Open("status-panel"), Is.True, "the status panel did not open.");
            for (var i = 0; i < 5; i++) yield return null;
            Assert.That(rig.transform.position, Is.EqualTo(position), "opening the panel moved the camera.");
            Assert.That(camera.orthographicSize, Is.EqualTo(size), "opening the panel rescaled the camera.");
            Assert.That(camera.rect, Is.EqualTo(rect), "opening the panel reshaped the viewport.");

            Assert.That(shell.CloseActive(), Is.True, "the status panel did not close.");
            for (var i = 0; i < 5; i++) yield return null;
            Assert.That(rig.transform.position, Is.EqualTo(position), "closing the panel moved the camera.");
            Assert.That(rig.transform.rotation, Is.EqualTo(rotation));
            Assert.That(camera.orthographicSize, Is.EqualTo(size));
            Assert.That(camera.rect, Is.EqualTo(rect));

            host.Paused = false;
        }

        /// <summary>The structural half of the same rule: nothing in the camera rig can even name a UI inset.</summary>
        [Test]
        public void TheCameraRigHasNoUiReference()
        {
            var fields = typeof(CameraRig).GetFields(System.Reflection.BindingFlags.Instance
                | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic);
            foreach (var f in fields)
            {
                var assembly = f.FieldType.Assembly.GetName().Name;
                Assert.That(assembly, Is.Not.EqualTo("Relight.UI"),
                    $"CameraRig.{f.Name} reaches into the UI assembly (D-UI-10).");
                Assert.That(f.FieldType.Namespace ?? "", Does.Not.StartWith("UnityEngine.UIElements"),
                    $"CameraRig.{f.Name} is a UI Toolkit type (D-UI-10).");
                Assert.That(f.Name.ToLowerInvariant(), Does.Not.Contain("inset"),
                    $"CameraRig.{f.Name} looks like a UI inset (D-UI-10).");
            }
        }
    }
}
