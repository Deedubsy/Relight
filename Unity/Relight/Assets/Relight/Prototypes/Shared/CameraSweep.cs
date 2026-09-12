using Relight.World;
using UnityEngine;

namespace Relight.Prototypes
{
    /// <summary>
    /// B-14 (R7 and R2). Pans an orthographic camera across the whole authored map in a fixed wall-clock time, so the
    /// frame samples cover chunk streaming, mesh building and light culling rather than one static view.
    ///
    /// Framing is B-13's: <c>viewTilesHigh = 20</c> (CameraRig), one tile = one world unit, sim Y-down flipped once
    /// through <see cref="WorldSpace"/>. The path is the map diagonal — north-west corner to south-east corner —
    /// which crosses every tilemap chunk row and column the renderer has.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Camera))]
    [AddComponentMenu("Relight/Prototypes/Camera Sweep")]
    public sealed class CameraSweep : MonoBehaviour
    {
        [Tooltip("Map width in sim tiles (riverfront-arc-v4: 864).")]
        [SerializeField] private int mapWidth = 864;

        [Tooltip("Map height in sim tiles (riverfront-arc-v4: 576).")]
        [SerializeField] private int mapHeight = 576;

        [Tooltip("Seconds for one corner-to-corner sweep.")]
        [SerializeField] private float seconds = 10f;

        [Tooltip("Viewport height in tiles. B-13 CameraRig.viewTilesHigh.")]
        [SerializeField, Min(4f)] private float viewTilesHigh = 20f;

        [Tooltip("Run the sweep on Start. Tests drive it explicitly instead.")]
        [SerializeField] private bool autoRun = true;

        private Camera _camera;
        private float _elapsed;
        private bool _running;

        public float Seconds => seconds;
        public bool Running => _running;
        public float Progress => seconds <= 0f ? 1f : Mathf.Clamp01(_elapsed / seconds);

        private void Awake()
        {
            _camera = GetComponent<Camera>();
            _camera.orthographic = true;
            _camera.orthographicSize = viewTilesHigh * 0.5f * WorldSpace.UnitsPerTile;
            Place(0f);
        }

        private void Start() { if (autoRun) Begin(); }

        /// <summary>Restart the sweep from the north-west corner.</summary>
        public void Begin()
        {
            _elapsed = 0f;
            _running = true;
            Place(0f);
        }

        public void Stop() => _running = false;

        private void Update()
        {
            if (!_running) return;
            _elapsed += Time.unscaledDeltaTime;
            Place(Progress);
            if (_elapsed >= seconds) _running = false;
        }

        /// <summary>Camera centre at sweep fraction <paramref name="t"/>, clamped so the view never leaves the map.</summary>
        public void Place(float t)
        {
            var halfH = _camera == null ? viewTilesHigh * 0.5f : _camera.orthographicSize;
            var halfW = halfH * (_camera == null ? 16f / 9f : _camera.aspect);
            var x = Mathf.Lerp(halfW, mapWidth - halfW, t);
            var y = Mathf.Lerp(halfH, mapHeight - halfH, t);
            var world = WorldSpace.TileCentre(0, 0);
            transform.position = new Vector3(world.x - 0.5f + x, -(y - 0.5f) - 0.5f, transform.position.z);
        }

        public void Configure(int width, int height, float sweepSeconds)
        {
            mapWidth = width;
            mapHeight = height;
            seconds = sweepSeconds;
        }
    }
}
