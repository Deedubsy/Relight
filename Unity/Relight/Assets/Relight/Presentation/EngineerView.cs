using Relight.Sim;
using Relight.World;
using UnityEngine;

namespace Relight.Presentation
{
    /// <summary>
    /// B-13. Draws the engineer where the sim says they are, reading <see cref="WorldQueries.Engineer"/> — never
    /// <c>SimState</c> — and flipping to Unity space through <see cref="WorldSpace"/>.
    ///
    /// Interpolation is an intentional addition, not a port. The reference renders inside the same frame that
    /// stepped the sim and simply draws <c>e.x * TILE_PX</c> (worldScene.ts:1330), because in the browser the tick
    /// and the draw are the same loop. Here the host runs a fixed 20 Hz clock independent of the frame rate
    /// (B-04), so at 60 fps the raw position would be a 20 Hz staircase. This lerps between the position at the
    /// last two tick boundaries by <see cref="SimHost.Alpha"/>, which costs one tick (50 ms) of display lag and is
    /// the standard fixed-step presentation trade. Nothing the sim reads is interpolated: this is a transform.
    /// </summary>
    [DisallowMultipleComponent]
    [AddComponentMenu("Relight/Engineer View")]
    public sealed class EngineerView : MonoBehaviour
    {
        [Tooltip("The host whose simulation this draws. Found in the scene if left empty.")]
        [SerializeField] private SimHost host;

        [Tooltip("Sprite depth; the engineer draws over the tilemap and over machines.")]
        [SerializeField] private float z = -2f;

        [Tooltip("Smooth between tick boundaries with SimHost.Alpha. Off draws the raw 20 Hz position, as the reference does.")]
        [SerializeField] private bool interpolate = true;

        private SpriteRenderer _sprite;
        private Vec2 _previous;
        private Vec2 _current;
        private bool _seeded;
        /// <summary>The simulation the interpolation window belongs to; a different one is a different session.</summary>
        private Simulation _session;

        /// <summary>The sim position last read at a tick boundary, in tile space (Y-down). For tests.</summary>
        public Vec2 SimPosition => _current;

        /// <summary>The other end of the interpolation window — the position at the previous tick boundary. For tests.</summary>
        public Vec2 PreviousSimPosition => _previous;

        /// <summary>False until a tick boundary of the CURRENT session has been read. For tests.</summary>
        public bool Seeded => _seeded;

        private void Awake()
        {
            _sprite = GetComponentInChildren<SpriteRenderer>();
            if (host == null) host = FindAnyObjectByType<SimHost>();
        }

        private void OnEnable()
        {
            if (host == null) return;
            host.TickBoundary += OnTickBoundary;
            host.SessionChanged += OnSessionChanged;
        }

        private void OnDisable()
        {
            if (host == null) return;
            host.TickBoundary -= OnTickBoundary;
            host.SessionChanged -= OnSessionChanged;
        }

        /// <summary>
        /// A new game or a loaded save: the interpolation window belongs to the session that is gone, so it is
        /// dropped rather than lerped across. Without this the engineer would slide from where the previous
        /// session left them to where the loaded save puts them, for one tick, every load.
        /// </summary>
        private void OnSessionChanged(Simulation sim) => Reseed(sim);

        private void Reseed(Simulation sim)
        {
            _session = sim;
            _seeded = false;
        }

        private void OnTickBoundary(int ticks)
        {
            // SimHost raises this every frame, with the number of ticks that ran (0 on most frames at 60 fps).
            // Only a frame that actually ticked moves the interpolation window; otherwise a zero-tick frame would
            // collapse previous onto current and the smoothing would do nothing.
            if (ticks <= 0) return;
            var sim = host == null ? null : host.Simulation;
            if (sim == null) return;
            if (!ReferenceEquals(sim, _session)) Reseed(sim);   // a swap this component was disabled for
            var e = WorldQueries.Engineer(sim.Context, sim.State);
            _previous = _seeded ? _current : e.Pos;
            _current = e.Pos;
            _seeded = true;
            // Facing is the reference's e.face (engineer.ts); the placeholder square only mirrors on it.
            if (_sprite != null && e.Face.X != 0) _sprite.flipX = e.Face.X < 0;
        }

        private void LateUpdate()
        {
            var sim = host == null ? null : host.Simulation;
            if (sim == null) return;
            if (!ReferenceEquals(sim, _session)) Reseed(sim);
            if (!_seeded)
            {
                _current = _previous = WorldQueries.Engineer(sim.Context, sim.State).Pos;
                _seeded = true;
            }
            var p = interpolate ? WorldSpace.Lerp(_previous, _current, host.Alpha) : _current;
            transform.position = WorldSpace.World(p, z);
        }
    }
}
