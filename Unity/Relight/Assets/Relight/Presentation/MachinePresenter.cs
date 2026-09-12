using System.Collections.Generic;
using Relight.Sim;
using UnityEngine;

namespace Relight.Presentation
{
    /// <summary>
    /// B-13. Keeps one <see cref="MachineView"/> alive per placed machine, keyed by sim id (TA §4.5: the
    /// presentation holds ids, never sim objects). It rebuilds only when the sim's structural revision changes —
    /// the reference's <c>f.rev</c> pattern (flow.ts:313, TA §4.5 mechanism 2) — so a machine that merely ticks
    /// costs nothing here, and it does that work at a tick boundary, never mid-tick.
    ///
    /// Not pooled: machines are long-lived and bounded (TA §7.2). Items, projectiles and enemies are the pooled
    /// families and none of them exist yet.
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
