using System.Collections;
using System.IO;
using NUnit.Framework;
using Relight.Presentation;
using Relight.Sim;
using Relight.UI;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UIElements;

namespace Relight.Tests.Play
{
    /// <summary>
    /// B-13 evidence. Not part of the suite — it is <see cref="ExplicitAttribute"/>, so an ordinary headless run never
    /// touches it — and it needs a real graphics device, so it is run on its own without <c>-nographics</c>:
    /// <c>Unity.exe -batchmode -runTests -testPlatform PlayMode -testFilter Relight.Tests.Play.EvidenceCapture</c>.
    ///
    /// A batchmode run has no window, so <c>ScreenCapture</c> and <c>WaitForEndOfFrame</c> are unavailable (the
    /// latter never resumes at all). Both images are therefore rendered to a RenderTexture and read back: the world
    /// through the scene camera, and the UI Toolkit panel through <see cref="PanelSettings.targetTexture"/>.
    /// </summary>
    [Explicit]
    public sealed class EvidenceCapture
    {
        private const string Folder = "E:/Factorio2/Unity/Docs/evidence/phase-b";
        private const int Width = 1280;
        private const int Height = 720;

        [UnityTest, Timeout(120000)]
        public IEnumerator CaptureWorldAndPanel()
        {
            if (SystemInfo.graphicsDeviceType == UnityEngine.Rendering.GraphicsDeviceType.Null)
                Assert.Ignore("no graphics device; re-run without -nographics.");

            yield return SceneFixture.LoadWorld();
            Directory.CreateDirectory(Folder);
            var host = Object.FindAnyObjectByType<SimHost>();
            var document = Object.FindAnyObjectByType<UIDocument>();

            yield return new WaitForSecondsRealtime(1f);
            host.Submit(new PlaceMachineCommand("chest", 8, 8));
            yield return new WaitForSecondsRealtime(0.5f);

            // The world, through the scene camera.
            var camera = Camera.main;
            var rt = new RenderTexture(Width, Height, 24, RenderTextureFormat.ARGB32) { antiAliasing = 1 };
            var previous = camera.targetTexture;
            camera.targetTexture = rt;
            camera.Render();
            camera.targetTexture = previous;
            Write(rt, "b13-playmode-world.png");

            // The panel, through the runtime panel's own target texture.
            var settings = document.panelSettings;
            var panelRt = new RenderTexture(Width, Height, 24, RenderTextureFormat.ARGB32);
            var previousPanel = settings.targetTexture;
            settings.targetTexture = panelRt;
            for (var i = 0; i < 4; i++) yield return null;
            Write(panelRt, "b13-playmode-panel.png");
            settings.targetTexture = previousPanel;
            yield return null;

            Object.DestroyImmediate(rt);
            Object.DestroyImmediate(panelRt);
        }

        private static void Write(RenderTexture rt, string file)
        {
            var active = RenderTexture.active;
            RenderTexture.active = rt;
            var tex = new Texture2D(rt.width, rt.height, TextureFormat.RGBA32, false);
            tex.ReadPixels(new Rect(0, 0, rt.width, rt.height), 0, 0);
            tex.Apply();
            RenderTexture.active = active;
            var png = tex.EncodeToPNG();
            Object.DestroyImmediate(tex);
            var path = Path.Combine(Folder, file);
            File.WriteAllBytes(path, png);
            Debug.Log($"B13 evidence: wrote {path} ({png.Length} bytes)");
        }
    }
}
