using Relight.Sim;
using Relight.World;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Relight.Presentation
{
    /// <summary>
    /// C-11, D-UI-11: the engineer's mouse-aimed flashlight. "A mouse-aimed visibility beam — provisional 12-tile
    /// reach, 60 degree width, soft falloff, small origin glow. It <b>only improves visibility</b>: no power, no rot
    /// clearing, no shade vulnerability. UI input capture holds its previous direction."
    ///
    /// That is the whole contract, and this class is deliberately small because of it. It computes a direction and
    /// hands it to <see cref="LightingPresenter.SetBeam"/>; the beam is subtracted from the darkness overlay there,
    /// so there is one overlay on screen and the flashlight cannot possibly become a light source the sim can see.
    /// It submits no command, reads no <c>SimState</c> field other than the engineer's position, and never calls
    /// <see cref="LightQueries"/> — the sim's <c>litAt</c> must stay ignorant of it (the reference is explicit:
    /// "the engineer carries no light (D-B5-1; the game can preview a 2-tile hand lamp, which is drawing only)",
    /// light.ts header).
    ///
    /// Holding the direction while the UI has input: the shared controls asset disables its <c>World</c> action map
    /// whenever a panel takes over (see <see cref="WorldInput"/>), so that same flag is the test here. No reference
    /// to the UI assembly is needed or wanted.
    /// </summary>
    [DisallowMultipleComponent]
    [AddComponentMenu("Relight/Mouse Flashlight Presenter")]
    public sealed class MouseFlashlightPresenter : MonoBehaviour
    {
        /// <summary>
        /// D-UI-11's provisional reach, in tiles. REL-126 took it from 12 to 14 with the placed lights: the owner
        /// asked for "a little bigger" and answered "both", so the torch moved too. **The figure is the
        /// implementer's under U-D-28 (U-P-29), not the owner's.** It is deliberately not the placed lights'
        /// quarter — a torch that grew to 15 would out-reach an Arc lamp, and the point of a lamp is that it beats
        /// carrying your own.
        /// </summary>
        public const double ReachTiles = 14;

        /// <summary>D-UI-11's provisional width: 60 degrees across, so 30 degrees each side of the aim.</summary>
        public const double HalfAngleRad = System.Math.PI / 6;

        /// <summary>The "small origin glow" of D-UI-11, in tiles: enough to see one's own feet.</summary>
        public const double GlowTiles = 1.75;

        [Tooltip("The host whose engineer carries the light. Found in the scene if left empty.")]
        [SerializeField] private SimHost host;

        [Tooltip("The overlay the beam is cut out of. Found in the scene if left empty.")]
        [SerializeField] private LightingPresenter lighting;

        [Tooltip("Camera used to turn the pointer into a world point. Main camera if left empty.")]
        [SerializeField] private Camera worldCamera;

        [Tooltip("The shared RelightControls asset. Only its World map's enabled flag is read, never its actions.")]
        [SerializeField] private InputActionAsset actions;

        [Tooltip("Off while the sun is up: the beam would be invisible and the texture upload wasted.")]
        [SerializeField] private bool nightOnly = true;

        private InputActionMap _world;
        /// <summary>The held direction. D-UI-11: input capture keeps the last aim rather than snapping the beam.</summary>
        private Vec2 _dir = new Vec2(0, 1);

        /// <summary>The direction last drawn, in sim tile space. A readback for tests; nothing consumes it.</summary>
        public Vec2 Direction => _dir;

        /// <summary>True on the last frame the beam was handed to the overlay.</summary>
        public bool Lit { get; private set; }

        private void Awake()
        {
            if (host == null) host = FindAnyObjectByType<SimHost>();
            if (lighting == null) lighting = FindAnyObjectByType<LightingPresenter>();
            if (worldCamera == null) worldCamera = Camera.main;
            _world = actions == null ? null : actions.FindActionMap("World", false);
        }

        private void OnDisable()
        {
            if (lighting != null) lighting.ClearBeam();
            Lit = false;
        }

        // Update, not LateUpdate: LightingPresenter paints in LateUpdate and must see this frame's aim.
        private void Update()
        {
            var sim = host == null ? null : host.Simulation;
            var cam = worldCamera != null ? worldCamera : Camera.main;
            if (sim == null || cam == null || lighting == null) { OnDisable(); return; }

            var ctx = sim.Context;
            var st = sim.State;
            if (nightOnly && LightQueries.Daylight(ctx, st).IsDay) { lighting.ClearBeam(); Lit = false; return; }

            var from = WorldQueries.Engineer(ctx, st).Pos;

            // A panel has input: hold the previous direction (D-UI-11). Same for a pointer that does not exist,
            // which is how this behaves on a pad or a touch screen until something aims it.
            var captured = _world != null && !_world.enabled;
            var mouse = Mouse.current;
            if (!captured && mouse != null)
            {
                var screen = mouse.position.ReadValue();
                // The world plane is z = 0, so the distance from the camera is its own z.
                var world = cam.ScreenToWorldPoint(new Vector3(screen.x, screen.y, Mathf.Abs(cam.transform.position.z)));
                var at = WorldSpace.Position(world);
                var d = new Vec2(at.X - from.X, at.Y - from.Y);
                if (d.Length > 1e-6) _dir = d;
            }

            lighting.SetBeam(from, _dir, ReachTiles, HalfAngleRad, GlowTiles);
            Lit = true;
        }
    }
}
