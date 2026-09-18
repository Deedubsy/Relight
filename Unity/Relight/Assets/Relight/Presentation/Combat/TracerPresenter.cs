using System.Collections.Generic;
using Relight.Sim;
using Relight.World;
using UnityEngine;

namespace Relight.Presentation
{
    /// <summary>
    /// C-03. Draws what the player must be able to see of a shot (U-D-15 "visible shots"): the rifle's hitscan
    /// tracers, fading over <see cref="Ballistics.ShotTraceSeconds"/>, and any bolt still in flight. It is the port
    /// of the reference's muzzle line and projectile sprites (worldScene.ts tracer drawing), with the same two
    /// sources: <see cref="ProjectileQueries.Tracers"/> and <see cref="ProjectileQueries.InFlight"/>.
    ///
    /// Both sources are SHARED with the turrets — <c>TurretPhase</c> notes its shots through the same
    /// <see cref="Ballistics.NoteShot"/>, and <see cref="TurretPresenter"/> deliberately draws no line of its own —
    /// so nothing here may assume a tracer belongs to the engineer (U-D-56).
    ///
    /// Read-only: it never touches <c>SimState</c> and submits nothing (TA §2.5). It owns no assets either — every
    /// line is a <see cref="LineRenderer"/> created in code and kept in a pool, so no <c>ProjectileView</c> prefab
    /// and no material need exist for this to work. The coordinator may assign a proper additive material and a
    /// bolt sprite later (see the report's Unity steps); nothing here depends on them.
    ///
    /// Bolts interpolate with <see cref="SimHost.Alpha"/>, as <see cref="EngineerView"/> does, because a bolt that
    /// moved on a 20 Hz staircase is the one thing on screen fast enough for the staircase to be obvious. Tracers
    /// do not: their endpoints are a fact about one shot and never move.
    /// </summary>
    [DisallowMultipleComponent]
    [AddComponentMenu("Relight/Tracer Presenter")]
    public sealed class TracerPresenter : MonoBehaviour
    {
        [Tooltip("The host whose simulation this draws. Found in the scene if left empty.")]
        [SerializeField] private SimHost host;

        [Tooltip("Tracer thickness in world units.")]
        [SerializeField] private float width = 0.05f;

        [Tooltip("Bolt length in tiles, drawn back along its direction of travel.")]
        [SerializeField] private float boltLength = 0.5f;

        [Tooltip("Depth; shots draw over the engineer.")]
        [SerializeField] private float z = -3f;

        [Tooltip("Tracer colour at full strength. It fades to transparent over Ballistics.ShotTraceSeconds.")]
        [SerializeField] private Color colour = new Color(1f, 0.95f, 0.6f, 0.9f);

        [Tooltip("Colour of a shot that connected (reference marks hits differently from misses).")]
        [SerializeField] private Color hitColour = new Color(1f, 0.6f, 0.35f, 0.95f);

        [Tooltip("Tiles to push each tracer's start along its OWN line, if the art wants the line to begin at a drawn barrel rather than at the shooter's centre. 0 matches the sim exactly.")]
        [SerializeField] private float muzzleOffset = 0f;

        private readonly List<LineRenderer> _pool = new List<LineRenderer>();
        private Material _material;

        /// <summary>Lines drawn on the last frame. For tests.</summary>
        public int Drawn { get; private set; }

        private void Awake()
        {
            if (host == null) host = FindAnyObjectByType<SimHost>();
            _material = new Material(Shader.Find("Sprites/Default"));
        }

        private void OnDisable()
        {
            for (var i = 0; i < _pool.Count; i++) if (_pool[i] != null) _pool[i].enabled = false;
            Drawn = 0;
        }

        private LineRenderer Line(int i)
        {
            while (_pool.Count <= i)
            {
                var go = new GameObject("Tracer " + _pool.Count);
                go.transform.SetParent(transform, false);
                var lr = go.AddComponent<LineRenderer>();
                lr.useWorldSpace = true;
                lr.positionCount = 2;
                lr.widthMultiplier = width;
                lr.numCapVertices = 1;
                lr.textureMode = LineTextureMode.Stretch;
                lr.material = _material;
                _pool.Add(lr);
            }
            return _pool[i];
        }

        private void LateUpdate()
        {
            var sim = host == null ? null : host.Simulation;
            if (sim == null) { OnDisable(); return; }
            var st = sim.State;
            var n = 0;

            var tracers = ProjectileQueries.Tracers(st);
            for (var i = 0; i < tracers.Count; i++)
            {
                var s = tracers[i];
                var fade = (float)ProjectileQueries.Fade(st, s);
                if (fade <= 0f) continue;
                // U-D-56. Every tracer begins where the SIM recorded it beginning, and the offset pushes that origin
                // along the shot's OWN line. This used to re-anchor the newest tracer to the ENGINEER's position —
                // which was wrong twice over, because `Weapons.Shots` is shared: TurretPhase:86 appends through the
                // same Ballistics.NoteShot, so a turret's shot is normally the newest entry and was therefore drawn
                // as a line leaving the player. (It also slid the player's own newest tracer along with them for the
                // half second it lived, since it was re-anchored every frame rather than at the moment of firing.)
                var from = s.From;
                if (muzzleOffset != 0f)
                {
                    double dx = s.To.X - s.From.X, dy = s.To.Y - s.From.Y;
                    var len = System.Math.Sqrt(dx * dx + dy * dy);
                    if (len > muzzleOffset)
                        from = new Vec2(s.From.X + dx / len * muzzleOffset, s.From.Y + dy / len * muzzleOffset);
                }
                var lr = Line(n++);
                lr.enabled = true;
                lr.SetPosition(0, WorldSpace.World(from, z));
                lr.SetPosition(1, WorldSpace.World(s.To, z));
                var c = s.Hit ? hitColour : colour;
                c.a *= fade;
                lr.startColor = c;
                lr.endColor = new Color(c.r, c.g, c.b, c.a * 0.25f);
            }

            var bolts = ProjectileQueries.InFlight(st);
            var alpha = host.Alpha;
            for (var i = 0; i < bolts.Count; i++)
            {
                var p = bolts[i];
                if (!sim.Context.Data.TryWeapon(p.Kind, out var def)) continue;
                // Where the bolt will be at the end of the tick in progress: one tick of travel scaled by Alpha.
                // A bolt that dies this tick simply disappears next frame; nothing is extrapolated past its range.
                var step = def.ProjectileSpeed * Simulation.TickSeconds * alpha;
                var head = new Vec2(p.Pos.X + p.Dir.X * step, p.Pos.Y + p.Dir.Y * step);
                var tail = new Vec2(head.X - p.Dir.X * boltLength, head.Y - p.Dir.Y * boltLength);
                var lr = Line(n++);
                lr.enabled = true;
                lr.SetPosition(0, WorldSpace.World(tail, z));
                lr.SetPosition(1, WorldSpace.World(head, z));
                lr.startColor = new Color(colour.r, colour.g, colour.b, 0f);
                lr.endColor = colour;
            }

            for (var i = n; i < _pool.Count; i++) if (_pool[i] != null) _pool[i].enabled = false;
            Drawn = n;
        }
    }
}
