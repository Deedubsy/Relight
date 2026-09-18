using System.Collections.Generic;
using Relight.Sim;
using Relight.World;
using UnityEngine;

namespace Relight.Presentation
{
    /// <summary>
    /// C-04 presentation. A turret is drawn as two pieces, exactly as the reference draws it (worldScene.ts turret
    /// drawing): a base that never moves, which <see cref="MachinePresenter"/> already places like any other
    /// machine, and a barrel that turns. This component owns only the turning half, so nothing here duplicates the
    /// machine pipeline: it finds each turret's existing <see cref="MachineView"/>, finds (or makes) its
    /// <see cref="BarrelName"/> child, and points that child where <see cref="TurretQueries.View"/> says the mount
    /// is aimed.
    ///
    /// The prefab contract is one line: a turret prefab may contain a child transform named "Cannon", pivoted at
    /// the mount's centre with its local +X down the barrel. If it does, this rotates it. If it does not — and no
    /// turret prefab exists yet — a stand-in barrel is built in code from a <see cref="LineRenderer"/>, so the
    /// turret reads correctly on screen before any art lands.
    ///
    /// Read-only (TA §2.5): it never touches <c>SimState</c> and submits no commands. Shots themselves are not
    /// drawn here — turret fire goes through <see cref="Ballistics.NoteShot"/> into the same shot list the rifle
    /// uses, so <see cref="TracerPresenter"/> already draws the tracer and there must not be a second one.
    ///
    /// Aim is not interpolated. The mount turns at its catalogue rate (<c>TurnSpeedRadPerS</c>) and a 20 Hz step of
    /// that is a fraction of a degree, which is below the eye's notice; interpolating would only add lag to the one
    /// thing the player uses to read what a turret is tracking.
    /// </summary>
    [DisallowMultipleComponent]
    [AddComponentMenu("Relight/Turret Presenter")]
    public sealed class TurretPresenter : MonoBehaviour
    {
        /// <summary>The child transform a turret prefab may supply for its barrel.</summary>
        public const string BarrelName = "Cannon";

        [Tooltip("The host whose simulation this draws. Found in the scene if left empty.")]
        [SerializeField] private SimHost host;

        [Tooltip("The machine views this reads to find each turret's base. Found in the scene if left empty.")]
        [SerializeField] private MachinePresenter machines;

        [Tooltip("Build a stand-in barrel when a turret prefab has no Cannon child. Off draws nothing for such a turret.")]
        [SerializeField] private bool standInBarrel = true;

        [Tooltip("Stand-in barrel thickness in world units.")]
        [SerializeField] private float barrelWidth = 0.12f;

        [Tooltip("Stand-in barrel colour.")]
        [SerializeField] private Color barrelColour = new Color(0.75f, 0.78f, 0.82f, 1f);

        [Tooltip("Colour of a barrel with no power or no ammunition: it is aimed but holding fire (U-D-12).")]
        [SerializeField] private Color holdingColour = new Color(0.45f, 0.47f, 0.5f, 1f);

        [Tooltip("Colour of a knocked-out mount (0 hp).")]
        [SerializeField] private Color deadColour = new Color(0.35f, 0.2f, 0.18f, 1f);

        [Tooltip("Muzzle flash colour. Shown while TurretView.Flashing, which the sim holds for the catalogue flash time.")]
        [SerializeField] private Color flashColour = new Color(1f, 0.9f, 0.55f, 0.95f);

        [Tooltip("Muzzle flash length in tiles.")]
        [SerializeField] private float flashLength = 0.45f;

        [Tooltip("Depth of the stand-in barrel and the flash; they draw over the machine sprite.")]
        [SerializeField] private float z = -2.5f;

        private readonly Dictionary<int, Transform> _barrels = new Dictionary<int, Transform>();
        private readonly Dictionary<int, LineRenderer> _standIns = new Dictionary<int, LineRenderer>();
        private readonly List<LineRenderer> _flashes = new List<LineRenderer>();
        private readonly List<int> _gone = new List<int>();
        private Material _material;

        /// <summary>Barrels pointed on the last frame. For tests.</summary>
        public int Aimed { get; private set; }

        /// <summary>Muzzle flashes drawn on the last frame. For tests.</summary>
        public int Flashes { get; private set; }

        private void Awake()
        {
            if (host == null) host = FindAnyObjectByType<SimHost>();
            if (machines == null) machines = FindAnyObjectByType<MachinePresenter>();
            _material = new Material(Shader.Find("Sprites/Default"));
        }

        private void OnDisable()
        {
            for (var i = 0; i < _flashes.Count; i++) if (_flashes[i] != null) _flashes[i].enabled = false;
            Aimed = 0;
            Flashes = 0;
        }

        private void LateUpdate()
        {
            var sim = host == null ? null : host.Simulation;
            if (sim == null || machines == null) { OnDisable(); return; }
            var ctx = sim.Context;
            var st = sim.State;
            var ids = TurretQueries.All(ctx, st);
            var flashes = 0;

            Aimed = 0;
            for (var i = 0; i < ids.Count; i++)
            {
                var id = ids[i];
                if (!machines.Views.TryGetValue(id, out var view) || view == null) continue;
                var v = TurretQueries.View(ctx, st, id);
                if (!v.Exists) continue;

                // The mount's centre. MachineView is placed on its footprint's centre, which is the same point the
                // sim measures range and muzzle offset from, so the view's own transform is read back into tile
                // space rather than reaching into SimState for the footprint (TA §4.5).
                var centre = WorldSpace.Position(view.transform.position);
                var cx = centre.X;
                var cy = centre.Y;
                // The sim's angle is measured in tile space, where +Y is south. Endpoints stay in tile space and
                // WorldSpace does the flip; only the Unity rotation needs the sign changed by hand.
                var dx = Mathf.Cos((float)v.Angle);
                var dy = Mathf.Sin((float)v.Angle);

                var barrel = Barrel(id, view);
                if (barrel != null)
                {
                    barrel.rotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(-dy, dx) * Mathf.Rad2Deg);
                    Aimed++;
                }

                if (_standIns.TryGetValue(id, out var stand) && stand != null)
                {
                    var reach = Mathf.Max(0.35f, (float)v.MuzzleTiles);
                    var from = new Vec2(cx, cy);
                    var to = new Vec2(cx + dx * reach, cy + dy * reach);
                    stand.enabled = true;
                    stand.SetPosition(0, WorldSpace.World(from, z));
                    stand.SetPosition(1, WorldSpace.World(to, z));
                    var c = v.Hp <= 0 ? deadColour : TurretQueries.Ready(ctx, st, id) || v.Rounds > 0 ? barrelColour : holdingColour;
                    stand.startColor = c;
                    stand.endColor = c;
                }

                if (!v.Flashing) continue;
                var muzzleX = cx + dx * v.MuzzleTiles;
                var muzzleY = cy + dy * v.MuzzleTiles;
                var flash = Flash(flashes++);
                flash.enabled = true;
                flash.SetPosition(0, WorldSpace.World(new Vec2(muzzleX, muzzleY), z));
                flash.SetPosition(1, WorldSpace.World(new Vec2(muzzleX + dx * flashLength, muzzleY + dy * flashLength), z));
                flash.startColor = flashColour;
                flash.endColor = new Color(flashColour.r, flashColour.g, flashColour.b, 0f);
            }

            for (var i = flashes; i < _flashes.Count; i++) if (_flashes[i] != null) _flashes[i].enabled = false;
            Flashes = flashes;
            Forget(ids);
        }

        /// <summary>The prefab's barrel child, or the stand-in built for a prefab that has none.</summary>
        private Transform Barrel(int id, MachineView view)
        {
            if (_barrels.TryGetValue(id, out var t) && t != null && t.parent == view.transform) return t;
            var found = view.transform.Find(BarrelName);
            if (found == null && standInBarrel)
            {
                var go = new GameObject(BarrelName + " (stand-in)");
                go.transform.SetParent(view.transform, false);
                var lr = go.AddComponent<LineRenderer>();
                lr.useWorldSpace = true;              // the line is positioned in world space, so the child never rotates it
                lr.positionCount = 2;
                lr.widthMultiplier = barrelWidth;
                lr.numCapVertices = 2;
                lr.material = _material;
                _standIns[id] = lr;
                found = null;                          // a stand-in is drawn, not rotated
            }
            _barrels[id] = found;
            return found;
        }

        private LineRenderer Flash(int i)
        {
            while (_flashes.Count <= i)
            {
                var go = new GameObject("Muzzle Flash " + _flashes.Count);
                go.transform.SetParent(transform, false);
                var lr = go.AddComponent<LineRenderer>();
                lr.useWorldSpace = true;
                lr.positionCount = 2;
                lr.widthMultiplier = barrelWidth * 1.6f;
                lr.numCapVertices = 2;
                lr.material = _material;
                _flashes.Add(lr);
            }
            return _flashes[i];
        }

        /// <summary>Drop the bookkeeping for turrets that have been removed, so a long session does not leak entries.</summary>
        private void Forget(List<int> live)
        {
            _gone.Clear();
            foreach (var pair in _standIns)
            {
                if (live.Contains(pair.Key) && pair.Value != null) continue;
                _gone.Add(pair.Key);
            }
            for (var i = 0; i < _gone.Count; i++)
            {
                if (_standIns.TryGetValue(_gone[i], out var lr) && lr != null) Destroy(lr.gameObject);
                _standIns.Remove(_gone[i]);
                _barrels.Remove(_gone[i]);
            }
        }
    }
}
