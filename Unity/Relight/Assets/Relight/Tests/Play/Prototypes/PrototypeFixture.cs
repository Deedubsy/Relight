using System.Collections;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;
#if UNITY_EDITOR
using UnityEditor.SceneManagement;
#endif

namespace Relight.Tests.Play.Prototypes
{
    /// <summary>
    /// B-14. Loads the prototype scenes and fabricates pointer events.
    ///
    /// The prototype scenes are deliberately NOT in Build Settings — they are a Phase B measurement rig, not part of
    /// the game's scene flow — so they are loaded through <c>EditorSceneManager.LoadSceneAsyncInPlayMode</c>, the
    /// supported way to play a scene by asset path.
    /// </summary>
    public static class PrototypeFixture
    {
        public const string R6 = "Assets/Relight/Prototypes/Scenes/R6-SlotGrid.unity";
        public const string R7 = "Assets/Relight/Prototypes/Scenes/R7-Tilemap.unity";
        public const string R2 = "Assets/Relight/Prototypes/Scenes/R2-Lighting.unity";

        public static IEnumerator Load(string path, LoadSceneMode mode = LoadSceneMode.Single)
        {
#if UNITY_EDITOR
            var op = EditorSceneManager.LoadSceneAsyncInPlayMode(path, new LoadSceneParameters(mode));
            Assert.NotNull(op, $"could not start a play-mode load of {path}; run Relight/Prototypes/Build All first.");
            while (!op.isDone) yield return null;
#else
            yield return SceneManager.LoadSceneAsync(System.IO.Path.GetFileNameWithoutExtension(path), mode);
#endif
            // Awake/OnEnable have run; give UI Toolkit a frame to build its panel and resolve layout.
            yield return null;
            yield return null;
        }

        /// <summary>
        /// Headless batchmode can be run without a graphics device, and then a runtime UI Toolkit panel has no size
        /// and <see cref="IPanel.Pick"/> answers nothing. Tests that need real layout call this first; an IGNORED
        /// test is recorded honestly in the report, an invented figure would not be.
        /// </summary>
        public static void RequireLayout(VisualElement root)
        {
            Assert.NotNull(root, "the prototype UIDocument has no root element.");
            var w = root.worldBound.width;
            var h = root.worldBound.height;
            if (float.IsNaN(w) || w < 1f || float.IsNaN(h) || h < 1f)
                Assert.Ignore($"UI Toolkit layout is degenerate here ({w} x {h}); re-run this test without -nographics.");
        }

        public static void RequireGraphics()
        {
            if (SystemInfo.graphicsDeviceType == UnityEngine.Rendering.GraphicsDeviceType.Null)
                Assert.Ignore("no graphics device; re-run without -nographics.");
        }

        /// <summary>
        /// R7-Tilemap.unity is 188 MiB of YAML when it holds all 497,664 tiles — which is R7's own headline figure —
        /// so it is deliberately NOT kept in the repository. Regenerate it with Relight/Prototypes/Paint R7
        /// (about 3.5 s) before running the R7 and R2 measurements.
        /// </summary>
        public static void RequirePaintedMap()
        {
#if UNITY_EDITOR
            if (!System.IO.File.Exists(R7))
                Assert.Ignore("R7-Tilemap.unity is not in the repository by design (188.66 MiB of YAML, " +
                              "b14-r7-paint.json); run Relight/Prototypes/Paint R7 to regenerate it.");
#endif
        }
    }

    /// <summary>
    /// A batchmode run has no window and no swap chain, so its main loop spins uncapped and <c>Time.deltaTime</c>
    /// measures almost nothing that the player would pay for. This renders the camera into a real RenderTexture and
    /// then reads one pixel back, which forces the GPU to finish — so the number is an honest end-to-end cost of one
    /// frame of THIS content at THIS resolution, not a frame rate.
    /// </summary>
    public sealed class RenderProbe : System.IDisposable
    {
        private readonly RenderTexture _rt;
        private readonly Texture2D _readback;

        public int Width { get; }
        public int Height { get; }

        public RenderProbe(int width = 1920, int height = 1080)
        {
            Width = width;
            Height = height;
            _rt = new RenderTexture(width, height, 24, RenderTextureFormat.ARGB32) { antiAliasing = 1 };
            _rt.Create();
            _readback = new Texture2D(1, 1, TextureFormat.RGBA32, false);
        }

        /// <summary>Milliseconds for one complete render of <paramref name="camera"/>, GPU included.</summary>
        public double RenderMs(Camera camera)
        {
            var previousTarget = camera.targetTexture;
            var previousActive = RenderTexture.active;
            var watch = System.Diagnostics.Stopwatch.StartNew();
            camera.targetTexture = _rt;
            camera.Render();
            RenderTexture.active = _rt;
            _readback.ReadPixels(new Rect(0f, 0f, 1f, 1f), 0, 0, false);   // stalls until the GPU is done
            _readback.Apply(false);
            watch.Stop();
            RenderTexture.active = previousActive;
            camera.targetTexture = previousTarget;
            return watch.Elapsed.TotalMilliseconds;
        }

        /// <summary>
        /// One full-resolution frame written as a PNG beside the JSON evidence, so R7 and R2 can be INSPECTED and not
        /// only counted. <c>ScreenCapture</c> and <c>WaitForEndOfFrame</c> do not work in batchmode; a RenderTexture
        /// readback does.
        /// </summary>
        public string Capture(Camera camera, string fileName)
        {
            var previousTarget = camera.targetTexture;
            var previousActive = RenderTexture.active;
            camera.targetTexture = _rt;
            camera.Render();
            RenderTexture.active = _rt;
            var tex = new Texture2D(Width, Height, TextureFormat.RGB24, false);
            tex.ReadPixels(new Rect(0f, 0f, Width, Height), 0, 0, false);
            tex.Apply(false);
            RenderTexture.active = previousActive;
            camera.targetTexture = previousTarget;
            var bytes = tex.EncodeToPNG();
            Object.DestroyImmediate(tex);
            System.IO.Directory.CreateDirectory(Relight.Prototypes.EvidenceWriter.Folder);
            var path = System.IO.Path.Combine(Relight.Prototypes.EvidenceWriter.Folder, fileName).Replace('\\', '/');
            System.IO.File.WriteAllBytes(path, bytes);
            Debug.Log($"B14 evidence: wrote {path}");
            return path;
        }

        public void Dispose()
        {
            if (_rt != null) { _rt.Release(); Object.DestroyImmediate(_rt); }
            if (_readback != null) Object.DestroyImmediate(_readback);
        }
    }

    /// <summary>
    /// Synthesised UI Toolkit pointer events. <c>PointerEventBase&lt;T&gt;</c> exposes its fields with non-public
    /// setters, so the pooled event is filled through its own setters by reflection — the standard way to drive a
    /// runtime panel from a test without an EventSystem, a device or a window.
    /// </summary>
    public static class UiPointer
    {
        public const int MouseId = 0;

        public static PointerDownEvent Down(Vector2 p, int button = 0) => Make<PointerDownEvent>(p, button, 1 << button);
        public static PointerMoveEvent Move(Vector2 p, int button = 0) => Make<PointerMoveEvent>(p, button, 1 << button);
        public static PointerUpEvent Up(Vector2 p, int button = 0) => Make<PointerUpEvent>(p, button, 0);
        public static PointerCancelEvent CancelEvent(Vector2 p) => Make<PointerCancelEvent>(p, 0, 0);

        private static T Make<T>(Vector2 p, int button, int pressed) where T : PointerEventBase<T>, new()
        {
            var e = PointerEventBase<T>.GetPooled();
            SetProperty(e, "pointerId", MouseId);
            SetProperty(e, "pointerType", UnityEngine.UIElements.PointerType.mouse);
            SetProperty(e, "position", new Vector3(p.x, p.y, 0f));
            SetProperty(e, "localPosition", new Vector3(p.x, p.y, 0f));
            SetProperty(e, "button", button);
            SetProperty(e, "pressedButtons", pressed);
            SetProperty(e, "isPrimary", true);
            SetProperty(e, "clickCount", 1);
            return e;
        }

        private static void SetProperty(object target, string name, object value)
        {
            var p = target.GetType().GetProperty(name, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            var setter = p?.GetSetMethod(true);
            if (setter == null)
            {
                var f = target.GetType().GetField("m_" + name, BindingFlags.NonPublic | BindingFlags.Instance);
                f?.SetValue(target, value);
                return;
            }
            setter.Invoke(target, new[] { value });
        }

        /// <summary>Send one event to an element and dispose the pooled instance.</summary>
        public static void Send(VisualElement target, EventBase e)
        {
            target.SendEvent(e);
            e.Dispose();
        }

        /// <summary>The panel-space centre of an element.</summary>
        public static Vector2 Centre(VisualElement e) => e.worldBound.center;
    }
}
