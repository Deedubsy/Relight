using System.Collections.Generic;
using Relight.Data;
using Relight.Sim;
using Relight.World;
using UnityEngine;

namespace Relight.Presentation
{
    /// <summary>
    /// B-10. Draws the items riding the conveyors, from <see cref="FlowQueries.AllItems"/>.
    ///
    /// Unlike <see cref="MachinePresenter"/> this cannot gate on the structural revision: a moving item changes
    /// nothing structural, so the whole set is re-read on every tick boundary (TA §4.5 mechanism 1). It is the
    /// pooled family TA §7.2 names — a busy line holds hundreds of sprites and they appear and vanish constantly —
    /// so sprites are taken from a free list and returned to it, never created and destroyed per item.
    ///
    /// The presentation holds ids and item keys, never sim objects (TA §4.5), and it never asks the sim to do
    /// anything: this is a read-only view.
    /// </summary>
    [DisallowMultipleComponent]
    [AddComponentMenu("Relight/Belt Item Presenter")]
    public sealed class BeltItemPresenter : MonoBehaviour
    {
        [Tooltip("The host whose simulation this draws. Found in the scene if left empty.")]
        [SerializeField] private SimHost host;

        [Tooltip("Item definitions, for their icons. The sim's ItemId ordinal picks the row by Key.")]
        [SerializeField] private GameDataRegistry registry;

        [Tooltip("Parent for the pooled sprites. This object if left empty.")]
        [SerializeField] private Transform container;

        [Tooltip("Sprite depth. Items draw over their conveyor and under the engineer.")]
        [SerializeField] private float z = -1.5f;

        [Tooltip("Side of one item sprite, in tiles. BELT_SPACING is 0.25, so anything above that overlaps.")]
        [SerializeField] private float size = 0.22f;

        [Tooltip("Drawn for an item whose definition has no icon yet (B-05 left them empty; C-06 rasterises them).")]
        [SerializeField] private Sprite fallbackIcon;

        private readonly List<BeltItemView> _items = new List<BeltItemView>(256);
        private readonly List<SpriteRenderer> _live = new List<SpriteRenderer>(256);
        private readonly List<SpriteRenderer> _free = new List<SpriteRenderer>(64);
        private Sprite[] _icons;
        private Simulation _session;

        /// <summary>How many sprites are currently drawn, for tests.</summary>
        public int Drawn => _live.Count;

        /// <summary>How many sprites are parked in the pool, for tests.</summary>
        public int Pooled => _free.Count;

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

        private void OnTickBoundary(int ticks) { if (ticks > 0) Sync(); }
        private void LateUpdate() => DrawPositions();

        private void DrawPositions()
        {
            var alpha = host == null || host.Paused ? 1f : host.Alpha;
            for (var i = 0; i < _items.Count && i < _live.Count; i++)
                _live[i].transform.position = Vector3.Lerp(WorldSpace.World(_items[i].PreviousPos, z), WorldSpace.World(_items[i].Pos, z), alpha);
        }

        /// <summary>A new game, a loaded save or a detach: park every sprite and read the state that is live now.</summary>
        private void OnSessionChanged(Simulation sim)
        {
            Release(0);
            _session = sim;
            if (sim != null) Sync();
        }

        /// <summary>Bring the drawn items in line with the state. Called once per tick boundary.</summary>
        public void Sync()
        {
            var sim = host == null ? null : host.Simulation;
            if (sim == null) { Release(0); return; }
            if (!ReferenceEquals(sim, _session)) { Release(0); _session = sim; }

            FlowQueries.AllItems(sim.State, _items);
            // Grow to what this frame needs, then park the surplus. The pool is never trimmed: a line that was
            // once busy will be busy again, and TA §7.2 wants the churn gone, not the memory.
            for (var i = _live.Count; i < _items.Count; i++) _live.Add(Take());
            Release(_items.Count);

            for (var i = 0; i < _items.Count; i++)
            {
                var it = _items[i];
                var sprite = _live[i];
                sprite.transform.position = WorldSpace.World(it.Pos, z);
                sprite.sprite = Icon(it.Item);
                sprite.enabled = sprite.sprite != null;
                sprite.sortingOrder = 12;
                if (sprite.sprite != null)
                {
                    var bounds = sprite.sprite.bounds.size;
                    var scale = Mathf.Min(size, .16f) / Mathf.Max(bounds.x, bounds.y);
                    sprite.transform.localScale = new Vector3(scale, scale, 1);
                }
            }
        }

        /// <summary>Park every sprite from <paramref name="keep"/> onwards.</summary>
        private void Release(int keep)
        {
            for (var i = _live.Count - 1; i >= keep; i--)
            {
                var sprite = _live[i];
                _live.RemoveAt(i);
                if (sprite == null) continue;
                sprite.enabled = false;
                _free.Add(sprite);
            }
        }

        private SpriteRenderer Take()
        {
            while (_free.Count > 0)
            {
                var pooled = _free[_free.Count - 1];
                _free.RemoveAt(_free.Count - 1);
                if (pooled != null) return pooled;
            }
            var go = new GameObject("Belt item");
            go.transform.SetParent(container, false);
            var made = go.AddComponent<SpriteRenderer>();
            made.transform.localScale = new Vector3(size, size, 1f);
            return made;
        }

        /// <summary>The icon for a sim item, by the definition whose Key matches the item's name.</summary>
        private Sprite Icon(ItemId item)
        {
            if (_icons == null)
            {
                _icons = new Sprite[Items.Count];
                var defs = registry == null ? null : registry.Items;
                for (var i = 0; defs != null && i < defs.Count; i++)
                {
                    var d = defs[i];
                    if (d == null || !Items.TryParse(d.Key, out var id)) continue;
                    _icons[(int)id] = d.Icon;
                }
            }
            var index = (int)item;
            if (index < 0 || index >= _icons.Length) return fallbackIcon;
            return _icons[index] != null ? _icons[index] : fallbackIcon;
        }
    }
}
