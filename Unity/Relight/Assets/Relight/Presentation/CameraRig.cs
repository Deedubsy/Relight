using Relight.Sim;
using Relight.World;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Relight.Presentation
{
    /// <summary>
    /// B-13. The world camera follows the engineer, ported from worldScene.ts:909-926: an exponential approach at
    /// <c>FOLLOW_PER_S = 14</c> per second (worldScene.ts:89) towards the engineer's position, snapping exactly
    /// once on the first frame rather than sliding in from wherever the camera happened to sit.
    ///
    /// <b>The camera never reads a UI inset.</b> That is the rule the reference had to fix twice (uiShell.ts:45-49
    /// reserves layout space but the follow target ignores it; worldScene.ts:921 "Menus overlay the city. Their
    /// dimensions must not move the follow target."; D-UI-10, TECHNICAL_ARCHITECTURE.md §8.3 row 5). There is no
    /// field, no reference and no code path here through which a panel could move the camera, and a play-mode test
    /// asserts the transform across an open/close cycle.
    ///
    /// Zoom is a plain framing choice: the reference's <c>fitZoom</c> sizes the view against the HQ block lot
    /// (worldScene.ts:392), and the block lattice is retired (CONTENT_CATALOGUE.md §17), so there is nothing to port.
    ///
    /// <b>REL-130: the wheel zooms out, and only out.</b> The owner asked for *"a zoom out functionality, mouse
    /// scroll. Not too far, just about another 50%"*. So <see cref="viewTilesHigh"/> is no longer the view — it is
    /// the CLOSEST view, the framing the game has always opened at, and the wheel widens from there to
    /// <see cref="ZoomOutFactor"/> of it in <see cref="ZoomNotches"/> steps and back. Wheel up never goes past the
    /// framing it started at, because the note asks to see more, not less.
    ///
    /// Zoom is a view preference and nothing else: it is not in <see cref="SimState"/>, it is not saved, and it
    /// survives a load rather than being re-decided by one — a player who zoomed out to look at their base should
    /// not be snapped back in by loading. It also cannot move the follow target, which is the rule the class header
    /// opens with: the wheel changes how much of the world is on screen, never which part of it is centred.
    ///
    /// The overlay that has to keep up with it needs no change. <c>LightingPresenter.Rect</c> reads
    /// <c>orthographicSize</c> live each frame and pads its tile rectangle by three tiles; the glide moves the view
    /// by well under a tile per frame, so the padding absorbs the one frame of ordering slack between the two
    /// <c>LateUpdate</c>s and no seam of unshaded map can appear at an edge.
    ///
    /// <b>Correction pass C6: the view is clamped to the map.</b> On the 78x367 Home crop the engineer could never
    /// reach an edge, so nothing stopped the camera walking off one; on the whole 864x576 city the spawn is 16 tiles
    /// from the north edge and the river is the south one, and without a clamp the player sees black past them. The
    /// bound comes from <see cref="ICityGeometry.Width"/>/<see cref="ICityGeometry.Height"/> — the map the SIM is
    /// running, not a serialized asset — so it is right on the synthetic map, on either imported region, and after a
    /// load that swapped the region underneath the rig. A map smaller than the viewport (the 64x64 synthetic map at
    /// wide aspect ratios) centres on the map instead of clamping, which is the only sane answer there.
    ///
    /// This is NOT a UI inset by another name: the bound is the world's, it is symmetric, and no panel can change it.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Camera))]
    [AddComponentMenu("Relight/Camera Rig")]
    public sealed class CameraRig : MonoBehaviour
    {
        /// <summary>Reference worldScene.ts:89 <c>FOLLOW_PER_S</c>.</summary>
        public const float FollowPerSecond = 14f;

        /// <summary>Reference worldScene.ts:909 — the follow step never integrates more than 100 ms of frame.</summary>
        public const float MaxFollowStepSeconds = 0.1f;

        /// <summary>
        /// REL-130. How much wider than <see cref="viewTilesHigh"/> the wheel can open the view: the owner's
        /// "about another 50%". A ratio rather than a tile count, so the scene stays the one place the framing is
        /// authored — move <see cref="viewTilesHigh"/> and the zoom-out moves with it.
        /// </summary>
        public const float ZoomOutFactor = 1.5f;

        /// <summary>REL-130. Wheel notches from the closest framing to the widest.</summary>
        public const int ZoomNotches = 4;

        /// <summary>
        /// REL-130. How fast the view glides to the notch the wheel chose, per second — the same exponential form
        /// as the follow, so a notch settles in a few frames rather than jumping.
        /// </summary>
        public const float ZoomPerSecond = 16f;

        [Tooltip("The host whose simulation this follows. Found in the scene if left empty.")]
        [SerializeField] private SimHost host;

        [Tooltip("What the camera follows. Usually the EngineerView; its transform is already interpolated.")]
        [SerializeField] private Transform target;

        [Tooltip("How many sim tiles the viewport is high. One tile is one world unit (WorldSpace).")]
        [SerializeField, Min(4f)] private float viewTilesHigh = 20f;

        [Tooltip("Keep the viewport inside the map the sim is running, so no frame shows black past an edge.")]
        [SerializeField] private bool clampToMapBounds = true;

        [Tooltip("Tiles of map the clamp keeps beyond the viewport edge. 0 puts the map edge exactly on the screen edge.")]
        [SerializeField, Min(0f)] private float edgePaddingTiles;

        private Camera _camera;
        private bool _snapped;
        private int _session = -1;
        /// <summary>REL-130. How many tiles high the view is showing right now, and the notch it is gliding to.</summary>
        private float _viewTiles, _viewTarget;

        private void Awake()
        {
            _camera = GetComponent<Camera>();
            if (host == null) host = FindAnyObjectByType<SimHost>();
            _camera.orthographic = true;
            _viewTiles = _viewTarget = viewTilesHigh;
            _camera.orthographicSize = viewTilesHigh * 0.5f * WorldSpace.UnitsPerTile;
        }

        /// <summary>REL-130. The framing the game opens at, and the closest the wheel can bring the view back to.</summary>
        public float ViewTilesClosest => viewTilesHigh;

        /// <summary>REL-130. The widest the wheel can open the view: the owner's "about another 50%".</summary>
        public float ViewTilesWidest => viewTilesHigh * ZoomOutFactor;

        /// <summary>REL-130. How many tiles high the view is showing this frame, mid-glide included.</summary>
        public float ViewTilesHigh => _viewTiles;

        /// <summary>REL-130. The notch the wheel last chose, which <see cref="ViewTilesHigh"/> is gliding towards.</summary>
        public float ViewTilesTarget => _viewTarget;

        /// <summary>
        /// REL-130. One wheel notch. Takes only the SIGN of <paramref name="scrollY"/>, because what a device
        /// reports is not comparable between devices — a wheel sends ±120 a notch on Windows and ±1 elsewhere, and
        /// a trackpad sends a stream of small deltas. The sign gives every one of them the same step, and the range
        /// is four notches wide, so even a fast flick simply arrives at the end of it.
        ///
        /// Wheel up is the conventional "closer", which here means back towards the framing the game opens at and
        /// no further: the owner asked for zoom out, so there is nothing on the other side of it.
        ///
        /// Public because the wheel is one line of device reading away, and a test that drove only the device would
        /// leave the clamping untested on any machine without a mouse.
        /// </summary>
        public void Wheel(float scrollY)
        {
            if (scrollY == 0f) return;
            var step = (ViewTilesWidest - ViewTilesClosest) / ZoomNotches;
            _viewTarget = Mathf.Clamp(_viewTarget + (scrollY > 0f ? -step : step), ViewTilesClosest, ViewTilesWidest);
        }

        /// <summary>Re-snap on the next frame (a load, or a session start).</summary>
        public void Unsnap() => _snapped = false;

        private void LateUpdate()
        {
            // Before the follow, so the map clamp below is computed against the size the frame will actually draw.
            Zoom();
            // A new game or a loaded save (SimHost.Session moves) is a cut, not a glide across the map: snap once,
            // and snap to the state's own position — the followed view may not have synced yet on the attach frame.
            if (host != null && host.Session != _session)
            {
                _session = host.Session;
                _snapped = false;
            }
            var goal = _snapped ? Goal() : (SimGoal() ?? Goal());
            if (!goal.HasValue) return;
            var g = goal.Value;
            var p = transform.position;
            if (!_snapped)
            {
                transform.position = Clamped(new Vector3(g.x, g.y, p.z));
                _snapped = true;
                return;
            }
            // worldScene.ts:926 — k = min(1, FOLLOW_PER_S * dt), applied to each axis.
            var dt = Mathf.Min(MaxFollowStepSeconds, Time.unscaledDeltaTime);
            var k = Mathf.Min(1f, FollowPerSecond * dt);
            // The clamp is applied to the RESULT, not to the goal: clamping the goal first would make the approach
            // ease to a stop against the edge, and the reference's constant-rate follow is what the player is used to.
            transform.position = Clamped(new Vector3(p.x + (g.x - p.x) * k, p.y + (g.y - p.y) * k, p.z));
        }

        /// <summary>
        /// REL-130. Read the wheel, then move the view one glide step towards the notch it chose.
        ///
        /// The wheel is ignored while the pointer is over interface, through the same probe <c>WorldInput</c>
        /// consults before it turns a click into a world command: a drawer's own list must scroll rather than
        /// zoom the city behind it, and a wheel over the HUD is not aimed at the world either. A null probe
        /// (a test scene, Boot, no shell) means "nothing is over the pointer", which is the existing convention.
        ///
        /// <c>orthographicSize</c> is written only when it actually differs, so a still frame leaves it bit for bit
        /// as it was. That is not tidiness: <c>CameraInsetTests</c> and <c>OpeningUiPlayTests</c> assert exact
        /// equality of that field across an open/close cycle (D-UI-10, "menus must not shift the camera"), and a
        /// per-frame assignment of an equal float would be a needless way to put those at risk.
        /// </summary>
        private void Zoom()
        {
            var mouse = Mouse.current;
            if (mouse != null)
            {
                var scrollY = mouse.scroll.ReadValue().y;
                if (scrollY != 0f && WorldInput.UiPointerProbe?.Invoke(mouse.position.ReadValue()) != true)
                    Wheel(scrollY);
            }

            if (_viewTiles != _viewTarget)
            {
                var dt = Mathf.Min(MaxFollowStepSeconds, Time.unscaledDeltaTime);
                var k = Mathf.Min(1f, ZoomPerSecond * dt);
                _viewTiles = Mathf.Abs(_viewTarget - _viewTiles) < 0.01f
                    ? _viewTarget
                    : _viewTiles + (_viewTarget - _viewTiles) * k;
            }

            if (_camera == null) _camera = GetComponent<Camera>();
            if (_camera == null || !_camera.orthographic) return;
            var size = _viewTiles * 0.5f * WorldSpace.UnitsPerTile;
            if (_camera.orthographicSize != size) _camera.orthographicSize = size;
        }

        /// <summary>
        /// The nearest camera centre that keeps the viewport inside the map. The map spans x ∈ [0, Width] and, after
        /// the single Y flip (<see cref="WorldSpace"/>), y ∈ [−Height, 0].
        /// </summary>
        public Vector3 Clamped(Vector3 p)
        {
            if (!clampToMapBounds) return p;
            var sim = host == null ? null : host.Simulation;
            var map = sim == null ? null : sim.Context.Geometry;
            if (map == null || map.Width <= 0 || map.Height <= 0) return p;
            if (_camera == null) _camera = GetComponent<Camera>();
            if (_camera == null || !_camera.orthographic) return p;

            var halfH = _camera.orthographicSize;
            var halfW = halfH * Mathf.Max(0.01f, _camera.aspect);
            var pad = edgePaddingTiles * WorldSpace.UnitsPerTile;
            var minX = 0f + pad;
            var maxX = map.Width * WorldSpace.UnitsPerTile - pad;
            var minY = -map.Height * WorldSpace.UnitsPerTile + pad;
            var maxY = 0f - pad;

            // A map narrower or shorter than the viewport cannot be clamped into: centre on it instead.
            p.x = maxX - minX <= halfW * 2f
                ? (minX + maxX) * 0.5f
                : Mathf.Clamp(p.x, minX + halfW, maxX - halfW);
            p.y = maxY - minY <= halfH * 2f
                ? (minY + maxY) * 0.5f
                : Mathf.Clamp(p.y, minY + halfH, maxY - halfH);
            return p;
        }

        /// <summary>The world point the camera centres on: the follow target, else the engineer's sim position.</summary>
        private Vector3? Goal()
        {
            if (target != null) return target.position;
            return SimGoal();
        }

        /// <summary>The engineer's position straight from the state — what a snap uses.</summary>
        private Vector3? SimGoal()
        {
            var sim = host == null ? null : host.Simulation;
            if (sim == null) return null;
            return WorldSpace.World(WorldQueries.Engineer(sim.Context, sim.State).Pos);
        }
    }
}
