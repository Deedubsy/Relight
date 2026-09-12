using Relight.Sim;
using Relight.World;
using UnityEngine;

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

        [Tooltip("The host whose simulation this follows. Found in the scene if left empty.")]
        [SerializeField] private SimHost host;

        [Tooltip("What the camera follows. Usually the EngineerView; its transform is already interpolated.")]
        [SerializeField] private Transform target;

        [Tooltip("How many sim tiles the viewport is high. One tile is one world unit (WorldSpace).")]
        [SerializeField, Min(4f)] private float viewTilesHigh = 20f;

        private Camera _camera;
        private bool _snapped;

        private void Awake()
        {
            _camera = GetComponent<Camera>();
            if (host == null) host = FindAnyObjectByType<SimHost>();
            _camera.orthographic = true;
            _camera.orthographicSize = viewTilesHigh * 0.5f * WorldSpace.UnitsPerTile;
        }

        /// <summary>Re-snap on the next frame (a load, or a session start).</summary>
        public void Unsnap() => _snapped = false;

        private void LateUpdate()
        {
            var goal = Goal();
            if (!goal.HasValue) return;
            var g = goal.Value;
            var p = transform.position;
            if (!_snapped)
            {
                transform.position = new Vector3(g.x, g.y, p.z);
                _snapped = true;
                return;
            }
            // worldScene.ts:926 — k = min(1, FOLLOW_PER_S * dt), applied to each axis.
            var dt = Mathf.Min(MaxFollowStepSeconds, Time.unscaledDeltaTime);
            var k = Mathf.Min(1f, FollowPerSecond * dt);
            transform.position = new Vector3(p.x + (g.x - p.x) * k, p.y + (g.y - p.y) * k, p.z);
        }

        /// <summary>The world point the camera centres on: the follow target, else the engineer's sim position.</summary>
        private Vector3? Goal()
        {
            if (target != null) return target.position;
            var sim = host == null ? null : host.Simulation;
            if (sim == null) return null;
            return WorldSpace.World(WorldQueries.Engineer(sim.Context, sim.State).Pos);
        }
    }
}
