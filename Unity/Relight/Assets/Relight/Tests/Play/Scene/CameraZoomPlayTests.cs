using System.Collections;
using NUnit.Framework;
using Relight.Presentation;
using Relight.UI;
using Relight.World;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.TestTools;
using UnityEngine.UIElements;

namespace Relight.Tests.Play
{
    /// <summary>
    /// REL-130. The owner's note: <i>"Can we have a zoom out functionality, mouse scroll. Not too far, just about
    /// another 50%"</i>. There was no zoom at all before this — <c>CameraRig</c> set <c>orthographicSize</c> once in
    /// <c>Awake</c> and never touched it again.
    ///
    /// Driven in the authored scene rather than on a bare GameObject, because the two things that could go wrong are
    /// both scene-shaped: the map clamp has to hold at the wider view on the real 864×576 city, and the darkness
    /// overlay has to keep covering the screen. A rig built in a test would have neither a city nor an overlay.
    ///
    /// The device half is driven through a real virtual mouse in the idiom <c>ControlsCorrectionTests</c>
    /// established, so the wheel is tested the way a player turns it, not by calling the handler underneath.
    /// </summary>
    public sealed class CameraZoomPlayTests
    {
        private InputSettings.BackgroundBehavior _background;
#if UNITY_EDITOR
        private InputSettings.EditorInputBehaviorInPlayMode _editorBehaviour;
#endif

        /// <summary>A headless run is never focused; without this every queued event applies to a disabled device.</summary>
        [SetUp]
        public void IgnoreApplicationFocus()
        {
            _background = InputSystem.settings.backgroundBehavior;
            InputSystem.settings.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus;
#if UNITY_EDITOR
            _editorBehaviour = InputSystem.settings.editorInputBehaviorInPlayMode;
            InputSystem.settings.editorInputBehaviorInPlayMode =
                InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
#endif
        }

        [TearDown]
        public void RestoreFocusBehaviour()
        {
            InputSystem.settings.backgroundBehavior = _background;
#if UNITY_EDITOR
            InputSystem.settings.editorInputBehaviorInPlayMode = _editorBehaviour;
#endif
        }

        [UnityTest, Timeout(60000)]
        public IEnumerator TheWheelOpensTheViewByHalfAgainAndStopsThere()
        {
            yield return Settled();
            var rig = Object.FindAnyObjectByType<CameraRig>();

            Assert.That(rig.ViewTilesWidest, Is.EqualTo(rig.ViewTilesClosest * 1.5f).Within(1e-4),
                "the widest view is no longer half again the closest; that is the owner's \"about another 50%\".");
            Assert.That(rig.ViewTilesHigh, Is.EqualTo(rig.ViewTilesClosest).Within(1e-4),
                "the scene must open at the closest framing, as it always has.");

            // More notches than there are, so the clamp is what stops it and not the count.
            for (var i = 0; i < CameraRig.ZoomNotches + 3; i++) rig.Wheel(-1f);
            Assert.That(rig.ViewTilesTarget, Is.EqualTo(rig.ViewTilesWidest).Within(1e-4),
                "wheeling out past the end opened the view further than half again.");

            yield return Glide(rig);
            Assert.That(rig.ViewTilesHigh, Is.EqualTo(rig.ViewTilesWidest).Within(1e-3), "the glide never arrived.");
            Assert.That(rig.GetComponent<Camera>().orthographicSize,
                Is.EqualTo(rig.ViewTilesWidest * 0.5f * WorldSpace.UnitsPerTile).Within(1e-3),
                "the camera is not showing the number of tiles the rig says it is.");
        }

        [UnityTest, Timeout(60000)]
        public IEnumerator TheWheelBackNeverGoesCloserThanTheFramingTheGameOpensAt()
        {
            yield return Settled();
            var rig = Object.FindAnyObjectByType<CameraRig>();

            for (var i = 0; i < CameraRig.ZoomNotches + 3; i++) rig.Wheel(-1f);
            yield return Glide(rig);

            // The owner asked for zoom OUT. Wheeling in returns to the opening framing and finds nothing past it.
            for (var i = 0; i < CameraRig.ZoomNotches + 3; i++) rig.Wheel(1f);
            Assert.That(rig.ViewTilesTarget, Is.EqualTo(rig.ViewTilesClosest).Within(1e-4),
                "the wheel zoomed IN past the framing the game opens at; there is no zoom in.");

            yield return Glide(rig);
            Assert.That(rig.ViewTilesHigh, Is.EqualTo(rig.ViewTilesClosest).Within(1e-3));
        }

        [UnityTest, Timeout(60000)]
        public IEnumerator TheMapStillFillsTheScreenAtFullZoomOut()
        {
            yield return Settled();
            var host = Object.FindAnyObjectByType<SimHost>();
            var rig = Object.FindAnyObjectByType<CameraRig>();
            var map = host.Simulation.Context.Geometry;

            for (var i = 0; i < CameraRig.ZoomNotches + 3; i++) rig.Wheel(-1f);
            yield return Glide(rig);
            for (var i = 0; i < 10; i++) yield return null;   // let the follow re-clamp at the wider view

            var cam = rig.GetComponent<Camera>();
            var halfH = cam.orthographicSize;
            var halfW = halfH * cam.aspect;
            var c = cam.transform.position;
            var w = map.Width * WorldSpace.UnitsPerTile;
            var h = map.Height * WorldSpace.UnitsPerTile;

            // The map spans x in [0, w] and, after the single Y flip, y in [-h, 0]. A map narrower or shorter than
            // the viewport centres instead of clamping, which is the only sane answer there, so each axis is only
            // asserted where clamping is possible at all.
            if (w > halfW * 2f)
            {
                Assert.That(c.x - halfW, Is.GreaterThanOrEqualTo(-1e-3), "black past the west edge at full zoom out.");
                Assert.That(c.x + halfW, Is.LessThanOrEqualTo(w + 1e-3), "black past the east edge at full zoom out.");
            }
            if (h > halfH * 2f)
            {
                Assert.That(c.y + halfH, Is.LessThanOrEqualTo(1e-3), "black past the north edge at full zoom out.");
                Assert.That(c.y - halfH, Is.GreaterThanOrEqualTo(-h - 1e-3), "black past the south edge at full zoom out.");
            }
        }

        [UnityTest, Timeout(60000)]
        public IEnumerator TheDarknessStillCoversTheScreenAtFullZoomOut()
        {
            yield return Settled();
            var rig = Object.FindAnyObjectByType<CameraRig>();
            var lighting = Object.FindAnyObjectByType<LightingPresenter>();
            Assert.That(lighting, Is.Not.Null, "World.unity has no LightingPresenter.");
            if (!lighting.Dark) Assert.Ignore("the darkness overlay is not being drawn; that is not this test's subject.");

            for (var i = 0; i < CameraRig.ZoomNotches + 3; i++) rig.Wheel(-1f);
            yield return Glide(rig);
            for (var i = 0; i < 10; i++) yield return null;   // the overlay rebuilds its rectangle from the camera

            var cam = rig.GetComponent<Camera>();
            var halfH = cam.orthographicSize;
            var halfW = halfH * cam.aspect;
            var c = cam.transform.position;
            var sr = lighting.GetComponentInChildren<SpriteRenderer>();
            Assert.That(sr, Is.Not.Null, "the darkness overlay has no SpriteRenderer.");
            Assert.That(sr.enabled, Is.True, "the darkness overlay stopped drawing at full zoom out.");

            // The overlay is clamped to the map, so it can only fall short of the screen where the map does.
            var b = sr.bounds;
            var map = Object.FindAnyObjectByType<SimHost>().Simulation.Context.Geometry;
            var left = Mathf.Max(c.x - halfW, 0f);
            var right = Mathf.Min(c.x + halfW, map.Width * WorldSpace.UnitsPerTile);
            var top = Mathf.Min(c.y + halfH, 0f);
            var bottom = Mathf.Max(c.y - halfH, -map.Height * WorldSpace.UnitsPerTile);
            Assert.That(b.min.x, Is.LessThanOrEqualTo(left + 1e-2), "unshaded map at the west edge of the wider view.");
            Assert.That(b.max.x, Is.GreaterThanOrEqualTo(right - 1e-2), "unshaded map at the east edge of the wider view.");
            Assert.That(b.min.y, Is.LessThanOrEqualTo(bottom + 1e-2), "unshaded map at the south edge of the wider view.");
            Assert.That(b.max.y, Is.GreaterThanOrEqualTo(top - 1e-2), "unshaded map at the north edge of the wider view.");
        }

        /// <summary>
        /// The device half: a real wheel event, turned by a real virtual mouse, reaches the rig. Everything above
        /// calls <c>Wheel</c>, which would pass just as happily if nobody had wired the mouse to it.
        /// </summary>
        [UnityTest, Timeout(60000)]
        public IEnumerator TurningTheRealWheelOverTheWorldOpensTheView()
        {
            yield return Settled();
            var rig = Object.FindAnyObjectByType<CameraRig>();
            var shell = Object.FindAnyObjectByType<UiShell>();
            var at = new Vector2(Screen.width * 0.5f, Screen.height * 0.5f);
            if (shell != null && shell.PointerOverUi(at))
                Assert.Ignore("the middle of the screen is covered by interface in this build; nowhere to turn the wheel over the world.");

            var before = rig.ViewTilesTarget;
            yield return Scroll(VirtualMouse(), at, -120f);
            Assert.That(rig.ViewTilesTarget, Is.GreaterThan(before),
                "a real wheel event over the world did not reach the camera rig.");
        }

        /// <summary>
        /// The other half of the same wiring: a drawer's own list scrolls, and the city behind it holds still. The
        /// guard is the very probe <c>WorldInput</c> uses to decide whether a click belongs to the world.
        /// </summary>
        [UnityTest, Timeout(60000)]
        public IEnumerator TurningTheWheelOverAnOpenPanelLeavesTheCityWhereItWas()
        {
            yield return Settled();
            var rig = Object.FindAnyObjectByType<CameraRig>();
            var shell = Object.FindAnyObjectByType<UiShell>();
            Assert.That(shell, Is.Not.Null, "GameUI.unity has no UiShell.");
            Assert.That(shell.Open("status-panel"), Is.True, "the status panel did not open.");
            for (var i = 0; i < 5; i++) yield return null;

            // Not the middle of the screen: the status drawer does not cover it, and a test that aims there only
            // skips itself. The wheel is aimed at the drawer's own centre, worked back into screen coordinates.
            Assert.That(ScreenPointOver("status-panel", out var at), Is.True,
                "the open status panel could not be located on screen.");
            Assert.That(shell.PointerOverUi(at), Is.True,
                "the point taken from the panel's own bounds is not over interface; the mapping is wrong.");

            var before = rig.ViewTilesTarget;
            yield return Scroll(VirtualMouse(), at, -120f);
            Assert.That(rig.ViewTilesTarget, Is.EqualTo(before).Within(1e-4),
                "a wheel turned over an open panel zoomed the city behind it.");
            shell.CloseActive();
        }

        // ---- fixtures ------------------------------------------------------------------------------------------

        /// <summary>The authored world with nothing open, the engineer frozen, and the camera settled on him.</summary>
        private static IEnumerator Settled()
        {
            yield return SceneFixture.LoadWorld();
            var host = Object.FindAnyObjectByType<SimHost>();
            var rig = Object.FindAnyObjectByType<CameraRig>();
            Assert.That(host, Is.Not.Null, "World.unity has no SimHost.");
            Assert.That(rig, Is.Not.Null, "World.unity has no CameraRig.");
            Object.FindAnyObjectByType<UiShell>()?.CloseActive();
            host.Paused = true;
            for (var i = 0; i < 5; i++) yield return null;
        }

        /// <summary>Run frames until the view has reached the notch it was sent to, or give up and let the assert say so.</summary>
        private static IEnumerator Glide(CameraRig rig)
        {
            for (var i = 0; i < 180 && Mathf.Abs(rig.ViewTilesHigh - rig.ViewTilesTarget) > 1e-3; i++) yield return null;
        }

        /// <summary>
        /// Where a named panel actually sits on screen. Panel space is screen space flipped in Y and scaled by the
        /// panel settings' own scale mode, so the mapping is pinned by probing its two screen corners rather than
        /// assumed — aiming at the middle of the screen instead is what used to make this test skip itself.
        /// </summary>
        private static bool ScreenPointOver(string id, out Vector2 screenPoint)
        {
            screenPoint = default;
            foreach (var doc in Object.FindObjectsByType<UIDocument>(FindObjectsSortMode.None))
            {
                var root = doc.rootVisualElement;
                var panel = root == null ? null : root.panel;
                if (panel == null) continue;
                var element = root.Q<VisualElement>(id, className: "panel");
                if (element == null || element.resolvedStyle.display == DisplayStyle.None) continue;

                var a = RuntimePanelUtils.ScreenToPanel(panel, new Vector2(0f, Screen.height));   // screen (0, 0)
                var b = RuntimePanelUtils.ScreenToPanel(panel, new Vector2(Screen.width, 0f));    // screen (W, H)
                if (Mathf.Approximately(b.x - a.x, 0f) || Mathf.Approximately(b.y - a.y, 0f)) continue;

                var c = element.worldBound.center;
                if (float.IsNaN(c.x) || float.IsNaN(c.y)) continue;
                screenPoint = new Vector2((c.x - a.x) / (b.x - a.x) * Screen.width,
                                          (c.y - a.y) / (b.y - a.y) * Screen.height);
                return true;
            }
            return false;
        }

        private static Mouse VirtualMouse()
        {
            var mouse = InputSystem.GetDevice<Mouse>() ?? InputSystem.AddDevice<Mouse>();
            if (!mouse.enabled) InputSystem.EnableDevice(mouse);
            return mouse;
        }

        /// <summary>
        /// One notch of a real wheel at <paramref name="at"/>. 120 is what a Windows wheel reports per notch; the rig
        /// takes only the sign, so the figure is the device's convention and not a number it depends on.
        /// </summary>
        private static IEnumerator Scroll(Mouse mouse, Vector2 at, float y)
        {
            InputSystem.QueueStateEvent(mouse, new MouseState { position = at, scroll = new Vector2(0f, y) });
            yield return null;
            yield return null;
        }
    }
}
