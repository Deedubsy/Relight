using System.Collections.Generic;
using Relight.Sim;
using Relight.Sim.UI;
using Relight.World;
using UnityEngine;

namespace Relight.Presentation
{
    /// <summary>
    /// B-13. Keeps one <see cref="MachineView"/> alive per placed machine, keyed by sim id (TA §4.5: the
    /// presentation holds ids, never sim objects). It rebuilds only when the sim's structural revision changes —
    /// the reference's <c>f.rev</c> pattern (flow.ts:313, TA §4.5 mechanism 2). Placement sync also catches paused
    /// structural changes; read-only activity details animate between ticks with one shared material.
    /// Machines are long-lived; their activity strokes are retained and reused (TA §7.2).
    /// </summary>
    [DisallowMultipleComponent]
    [AddComponentMenu("Relight/Machine Presenter")]
    public sealed class MachinePresenter : MonoBehaviour
    {
        [Tooltip("The host whose simulation this draws. Found in the scene if left empty.")]
        [SerializeField] private SimHost host;

        [Tooltip("Kind to prefab. Interim until MachineDefinition carries its own prefab reference (TA 7.2).")]
        [SerializeField] private PrefabRegistry prefabs;

        [Tooltip("Parent for spawned machine views. This object if left empty.")]
        [SerializeField] private Transform container;

        private readonly Dictionary<int, MachineView> _views = new Dictionary<int, MachineView>();
        private readonly List<MachinePlacement> _placements = new List<MachinePlacement>(64);
        private readonly List<int> _stale = new List<int>(8);
        private int _revision = -1;
        /// <summary>The simulation the views were last built from; a different one means a different session.</summary>
        private Simulation _session;
        private Material _activityMaterial;
        private Transform _coreMark;
        private BuildingConditionVisual _coreCondition;
        private BuildingHealthVisual _coreHealth;
        private Camera _eye;

        /// <summary>REL-133: true while the Home core is drawing the repair mark. For the play tests.</summary>
        public bool CoreRepairShowing => _coreCondition != null && _coreCondition.Showing;

        /// <summary>REL-134: true while the Home core is wearing a health bar. For the play tests.</summary>
        public bool CoreHealthShowing => _coreHealth != null && _coreHealth.Showing;

        /// <summary>
        /// REL-134. Half the height of what the camera is showing, in world units — what a screen-sized measurement
        /// has to be written in terms of, now that REL-130's wheel changes it. Resolved lazily and re-resolved when
        /// the camera goes, because a session change destroys and remakes the world scene around this component.
        ///
        /// The fallback is <see cref="CameraRig"/>'s opening framing, so a scene with no camera at all — a headless
        /// test, Boot — draws the same bar the player opens the game to rather than nothing.
        /// </summary>
        private const float DefaultHalfView = 10f;

        private float HalfView()
        {
            if(_eye==null)_eye=Camera.main;
            return _eye!=null&&_eye.orthographic?_eye.orthographicSize:DefaultHalfView;
        }

        private void LateUpdate()
        {
            Sync();
            var sim=host==null?null:host.Simulation;if(sim==null)return;
            if(_activityMaterial==null)_activityMaterial=new Material(Shader.Find("Sprites/Default"));
            var dt=host.Paused?0:Mathf.Min(Time.deltaTime,.1f);
            var halfView=HalfView();
            foreach(var view in _views.Values)if(view!=null)view.Animate(sim,dt,_activityMaterial,halfView);
            AnimateCore(sim,dt,halfView);
        }

        /// <summary>
        /// REL-133. The Home core is a rect on <see cref="HomeState"/>, not a <see cref="Machine"/> — nothing places
        /// it and nothing may pack it — so it has no <see cref="MachineView"/> to hang the repair mark on and gets
        /// one object of its own here. It is drawn from this presenter rather than from one of its own because the
        /// core IS a building to the player: it is the thing they repair most, and the owner's note says "when a
        /// building is being repaired" without carving it out.
        ///
        /// The object is made on the first core repair — REL-134 added "or the first time the core is damaged" —
        /// and then kept, like every other stroke in this presenter.
        /// </summary>
        private void AnimateCore(Simulation sim,float dt,float halfView)
        {
            var st=sim.State;
            var repairing=BuildingCondition.RepairingCore(st);
            var health=BuildingCondition.CoreHealth(sim.Context.Data,st);
            var h=st.Home;
            if(h==null||(!repairing&&!health.Damaged)){_coreCondition?.Hide();_coreHealth?.Hide();return;}
            if(_coreMark==null)
            {
                var go=new GameObject("Home core condition");
                go.transform.SetParent(container==null?transform:container,false);
                _coreMark=go.transform;
            }
            // Followed every frame rather than set once: HomeCore.Ensure can place the core on a later tick, and a
            // loaded save brings a core that may stand somewhere else entirely.
            _coreMark.position=WorldSpace.RectCentre(h.X,h.Y,h.W,h.H);
            if(repairing)
            {
                if(_coreCondition==null)_coreCondition=new BuildingConditionVisual(_coreMark,_activityMaterial);
                _coreCondition.Draw(h.W,h.H,BuildingCondition.RepairProgress(sim.Context.Data,st),dt);
            }
            else _coreCondition?.Hide();
            // REL-134. The core is the building the player watches hardest, and the one the raid is FOR; it gets the
            // same bar as a Wall, from the same code, for the same reason REL-133 gave it the same repair mark.
            if(health.Damaged)
            {
                if(_coreHealth==null)_coreHealth=new BuildingHealthVisual(_coreMark,_activityMaterial);
                _coreHealth.Draw(h.W,h.H,health.Fraction,halfView);
            }
            else _coreHealth?.Hide();
        }
        private void OnDestroy(){if(_activityMaterial!=null)Destroy(_activityMaterial);}

        /// <summary>Live views by machine id, for tests.</summary>
        public IReadOnlyDictionary<int, MachineView> Views => _views;

        private void Awake()
        {
            if (host == null) host = FindAnyObjectByType<SimHost>();
            if (container == null) container = transform;
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

        private void OnTickBoundary(int ticks) => Sync();

        /// <summary>
        /// A new game, a loaded save or a detach. The revision is NOT a session-wide identity — two states can
        /// each be at revision 1 with a machine on different tiles — so every view is dropped and rebuilt from
        /// the state that is live now rather than compared against a number that happens to match.
        /// </summary>
        private void OnSessionChanged(Simulation sim)
        {
            Clear();
            if (sim != null) Sync();
        }

        /// <summary>Drop every view and forget the revision, so the next Sync rebuilds from scratch.</summary>
        private void Clear()
        {
            foreach (var view in _views.Values)
                if (view != null) Destroy(view.gameObject);
            _views.Clear();
            _placements.Clear();
            _revision = -1;
            _session = null;
        }

        /// <summary>Bring the views in line with the state. Cheap when the revision has not moved.</summary>
        public void Sync()
        {
            var sim = host == null ? null : host.Simulation;
            if (sim == null) return;
            // The belt to the event's braces: a swap that happened while this component was disabled, or before
            // it subscribed, is caught here instead of being hidden behind an equal revision.
            if (!ReferenceEquals(sim, _session))
            {
                if (_session != null) Clear();
                _session = sim;
            }
            var rev = MachineSnapshot.Revision(sim.State);
            if (rev == _revision) return;
            _revision = rev;

            MachineSnapshot.All(sim.State, _placements);

            _stale.Clear();
            foreach (var id in _views.Keys)
            {
                var live = false;
                for (var i = 0; i < _placements.Count && !live; i++) live = _placements[i].Id == id;
                if (!live) _stale.Add(id);
            }
            for (var i = 0; i < _stale.Count; i++)
            {
                if (_views.TryGetValue(_stale[i], out var dead) && dead != null) Destroy(dead.gameObject);
                _views.Remove(_stale[i]);
            }

            for (var i = 0; i < _placements.Count; i++)
            {
                var m = _placements[i];
                if (!_views.TryGetValue(m.Id, out var view) || view == null)
                {
                    var prefab = prefabs == null ? null : prefabs.Get(m.Kind);
                    if (prefab == null)
                    {
                        Debug.LogWarning($"Relight: no prefab for machine kind '{m.Kind}'; it will not be drawn.");
                        continue;
                    }
                    var go = Instantiate(prefab, container);
                    view = go.GetComponent<MachineView>();
                    if (view == null)
                    {
                        Debug.LogWarning($"Relight: prefab for '{m.Kind}' has no MachineView component.");
                        Destroy(go);
                        continue;
                    }
                    _views[m.Id] = view;
                }
                view.Bind(m);
            }
        }
    }
}
