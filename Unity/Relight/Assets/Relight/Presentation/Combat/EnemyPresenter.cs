using System.Collections.Generic;
using Relight.Sim;
using Relight.World;
using UnityEngine;

namespace Relight.Presentation
{
    /// <summary>
    /// C-08 presentation. Draws the raid: one pooled sprite per living body, one per glob in flight, and the
    /// attack tell the reference shows during a windup (gameplayCombat.ts draws the committed aim line while a
    /// body is charging, which is what makes an attack dodgeable rather than a surprise).
    ///
    /// It owns no assets. Bodies are quads tinted by kind from a one-pixel texture built in code, so a raid is
    /// legible before any art lands; assign real sprites through <see cref="body"/> and <see cref="glob"/> and the
    /// same pool uses them instead. Pooling is by slot, not by body id: bodies are born and killed constantly and
    /// a per-id dictionary would churn.
    ///
    /// Read-only (TA §2.5): everything comes from <see cref="EnemyQueries"/>; <c>SimState</c> is never touched and
    /// no command is submitted. Positions interpolate between the last two tick boundaries with
    /// <see cref="SimHost.Alpha"/>, the same trade <see cref="EngineerView"/> documents — a body walks at
    /// <c>raids.speedTilesPerS</c>, so the raw 20 Hz position is a visible staircase at 60 fps.
    /// </summary>
    [DisallowMultipleComponent]
    [AddComponentMenu("Relight/Enemy Presenter")]
    public sealed class EnemyPresenter : MonoBehaviour
    {
        [Tooltip("The host whose simulation this draws. Found in the scene if left empty.")]
        [SerializeField] private SimHost host;

        [Tooltip("Optional sprite for a body. Left empty, a tinted square is built in code.")]
        [SerializeField] private Sprite body;

        [Tooltip("Optional sprite for a glob in flight. Left empty, a tinted square is built in code.")]
        [SerializeField] private Sprite glob;

        [Tooltip("Smooth between tick boundaries with SimHost.Alpha. Off draws the raw 20 Hz position.")]
        [SerializeField] private bool interpolate = true;

        [Tooltip("Body depth; bodies draw over machines and under shots.")]
        [SerializeField] private float z = -2f;

        [Tooltip("Colour of a skitter — the fast contact body.")]
        [SerializeField] private Color skitterColour = new Color(0.85f, 0.35f, 0.3f, 1f);

        [Tooltip("Colour of a spitter — the slower ranged body.")]
        [SerializeField] private Color spitterColour = new Color(0.7f, 0.45f, 0.85f, 1f);

        [Tooltip("Colour of anything else in the roster, so a new kind is never invisible.")]
        [SerializeField] private Color otherColour = new Color(0.8f, 0.6f, 0.3f, 1f);

        [Tooltip("Tint mixed in while a body is winding up an attack: the tell the player reacts to.")]
        [SerializeField] private Color windupTint = new Color(1f, 0.95f, 0.7f, 1f);

        [Tooltip("Draw the committed aim line while a body is winding up or charging.")]
        [SerializeField] private bool drawTell = true;

        [Tooltip("Aim-line colour.")]
        [SerializeField] private Color tellColour = new Color(1f, 0.55f, 0.4f, 0.5f);

        [Tooltip("Body diameter in tiles.")]
        [SerializeField] private float bodyTiles = 0.7f;

        [Tooltip("Glob diameter in tiles.")]
        [SerializeField] private float globTiles = 0.3f;

        private readonly List<SpriteRenderer> _bodies = new List<SpriteRenderer>();
        private readonly List<SpriteRenderer> _globs = new List<SpriteRenderer>();
        private readonly List<LineRenderer> _tells = new List<LineRenderer>();
        private readonly Dictionary<int, Vec2> _previous = new Dictionary<int, Vec2>();
        private readonly Dictionary<int, Vec2> _current = new Dictionary<int, Vec2>();
        private Sprite _square;
        private Material _lineMaterial;
        /// <summary>The simulation the interpolation window belongs to; a different one is a different session.</summary>
        private Simulation _session;

        /// <summary>Bodies drawn on the last frame. For tests.</summary>
        public int Bodies { get; private set; }

        /// <summary>Globs drawn on the last frame. For tests.</summary>
        public int Globs { get; private set; }

        private void Awake()
        {
            if (host == null) host = FindAnyObjectByType<SimHost>();
            _lineMaterial = new Material(Shader.Find("Sprites/Default"));
            var tex = new Texture2D(1, 1, TextureFormat.RGBA32, false) { filterMode = FilterMode.Point };
            tex.SetPixel(0, 0, Color.white);
            tex.Apply();
            _square = Sprite.Create(tex, new Rect(0, 0, 1, 1), new Vector2(0.5f, 0.5f), 1f);
        }

        private void OnEnable()
        {
            if (host != null) host.TickBoundary += OnTickBoundary;
        }

        private void OnDisable()
        {
            if (host != null) host.TickBoundary -= OnTickBoundary;
            for (var i = 0; i < _bodies.Count; i++) if (_bodies[i] != null) _bodies[i].enabled = false;
            for (var i = 0; i < _globs.Count; i++) if (_globs[i] != null) _globs[i].enabled = false;
            for (var i = 0; i < _tells.Count; i++) if (_tells[i] != null) _tells[i].enabled = false;
            Bodies = 0;
            Globs = 0;
        }

        /// <summary>
        /// Records where every body stood at this tick boundary. A body born this tick has no previous position,
        /// so it is seeded to its birth position and appears where the sim put it rather than sliding in from the
        /// last body that happened to reuse its slot.
        /// </summary>
        private void OnTickBoundary(int ticks)
        {
            var sim = host == null ? null : host.Simulation;
            if (sim == null) return;
            _previous.Clear();
            foreach (var pair in _current) _previous[pair.Key] = pair.Value;
            _current.Clear();
            var all = EnemyQueries.All(sim.State);
            for (var i = 0; i < all.Count; i++) _current[all[i].Id] = all[i].Pos;
        }

        private void LateUpdate()
        {
            var sim = host == null ? null : host.Simulation;
            if (sim == null) { Clear(); return; }
            if (!ReferenceEquals(_session, sim))
            {
                _session = sim;
                _previous.Clear();
                _current.Clear();
            }
            var st = sim.State;
            var alpha = interpolate ? host.Alpha : 1f;
            var all = EnemyQueries.All(st);
            var tells = 0;

            for (var i = 0; i < all.Count; i++)
            {
                var e = all[i];
                var at = e.Pos;
                if (interpolate && _previous.TryGetValue(e.Id, out var was) && _current.ContainsKey(e.Id))
                    at = WorldSpace.Lerp(was, _current[e.Id], alpha);

                var sprite = Slot(_bodies, i, "Body", body);
                sprite.enabled = true;
                sprite.transform.position = WorldSpace.World(at, z);
                sprite.transform.localScale = Scale(body, bodyTiles);
                var winding = e.Phase == EnemyPhaseKind.Windup || e.Phase == EnemyPhaseKind.Charge;
                var c = Colour(e.Kind);
                sprite.color = winding ? Color.Lerp(c, windupTint, 0.5f) : c;

                if (!drawTell || !winding) continue;
                var line = Tell(tells++);
                line.enabled = true;
                line.SetPosition(0, WorldSpace.World(at, z));
                line.SetPosition(1, WorldSpace.World(e.Aim, z));
                line.startColor = tellColour;
                line.endColor = new Color(tellColour.r, tellColour.g, tellColour.b, 0f);
            }
            for (var i = all.Count; i < _bodies.Count; i++) if (_bodies[i] != null) _bodies[i].enabled = false;
            for (var i = tells; i < _tells.Count; i++) if (_tells[i] != null) _tells[i].enabled = false;
            Bodies = all.Count;

            // Globs are extrapolated, not interpolated: one is only alive for a couple of seconds and its velocity
            // is a saved fact, so a fraction of a tick along it is exact rather than a guess.
            var shots = EnemyQueries.Projectiles(st);
            for (var i = 0; i < shots.Count; i++)
            {
                var p = shots[i];
                var step = interpolate ? Simulation.TickSeconds * alpha : 0;
                var at = new Vec2(p.Pos.X + p.Vel.X * step, p.Pos.Y + p.Vel.Y * step);
                var sprite = Slot(_globs, i, "Glob", glob);
                sprite.enabled = true;
                sprite.transform.position = WorldSpace.World(at, z - 0.25f);
                sprite.transform.localScale = Scale(glob, globTiles);
                sprite.color = spitterColour;
            }
            for (var i = shots.Count; i < _globs.Count; i++) if (_globs[i] != null) _globs[i].enabled = false;
            Globs = shots.Count;
        }

        private void Clear()
        {
            for (var i = 0; i < _bodies.Count; i++) if (_bodies[i] != null) _bodies[i].enabled = false;
            for (var i = 0; i < _globs.Count; i++) if (_globs[i] != null) _globs[i].enabled = false;
            for (var i = 0; i < _tells.Count; i++) if (_tells[i] != null) _tells[i].enabled = false;
            Bodies = 0;
            Globs = 0;
        }

        private Color Colour(string kind) =>
            kind == "skitter" ? skitterColour : kind == "spitter" ? spitterColour : otherColour;

        /// <summary>A square built in code is one world unit across; an authored sprite brings its own size.</summary>
        private Vector3 Scale(Sprite authored, float tiles)
        {
            var size = tiles * WorldSpace.UnitsPerTile;
            return authored == null ? new Vector3(size, size, 1f) : Vector3.one;
        }

        private SpriteRenderer Slot(List<SpriteRenderer> pool, int i, string name, Sprite authored)
        {
            while (pool.Count <= i)
            {
                var go = new GameObject(name + " " + pool.Count);
                go.transform.SetParent(transform, false);
                pool.Add(go.AddComponent<SpriteRenderer>());
            }
            var sr = pool[i];
            sr.sprite = authored != null ? authored : _square;
            return sr;
        }

        private LineRenderer Tell(int i)
        {
            while (_tells.Count <= i)
            {
                var go = new GameObject("Attack Tell " + _tells.Count);
                go.transform.SetParent(transform, false);
                var lr = go.AddComponent<LineRenderer>();
                lr.useWorldSpace = true;
                lr.positionCount = 2;
                lr.widthMultiplier = 0.06f;
                lr.material = _lineMaterial;
                _tells.Add(lr);
            }
            return _tells[i];
        }
    }
}
