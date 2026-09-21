using System.Collections.Generic;
using Relight.Sim;
using Relight.World;
using UnityEngine;

namespace Relight.Presentation
{
    /// <summary>
    /// INT-01 (REL-5). Draws the cargo a death drops. The sim has always spilled the Backpack into a
    /// <see cref="DropCache"/> and has always had <see cref="CollectCacheCommand"/>; nothing drew the pile, so to the
    /// player the Backpack simply vanished. This is the missing picture: one marker per pile that still holds
    /// something, on the tile the sim says it is on.
    ///
    /// It reads <c>st.Drops.Caches</c> and writes nothing (TA §2.5), and it owns no assets: the marker is a
    /// one-pixel sprite built in code, a dark diamond with an amber one inside it, so the component drops into the
    /// scene with no prefab. The marker sorts ABOVE the darkness overlay (<see cref="DrawOrder.DroppedCargo"/>):
    /// the player died there, usually in the dark, and has to be able to find it again (ALWAYS_DARK_SPEC.md §3).
    /// It is a picture rule only; reach and collection are the sim's.
    /// </summary>
    [DisallowMultipleComponent]
    [AddComponentMenu("Relight/Dropped Cargo")]
    public sealed class DropCachePresenter : MonoBehaviour
    {
        [Tooltip("The host whose simulation this draws. Found in the scene if left empty.")]
        [SerializeField] private SimHost host;

        [Tooltip("Marker size as a fraction of a tile.")]
        [SerializeField, Range(0.2f, 1f)] private float size = 0.5f;

        [Tooltip("Depth; with the other world feedback, in front of the tilemap.")]
        [SerializeField] private float z = -1.5f;

        [SerializeField] private Color colour = new Color(1f, 0.78f, 0.25f, 1f);
        [SerializeField] private Color rimColour = new Color(0.08f, 0.07f, 0.05f, 0.95f);

        private readonly List<SpriteRenderer> _rims = new List<SpriteRenderer>();
        private readonly List<SpriteRenderer> _bodies = new List<SpriteRenderer>();
        private readonly List<(int id, int x, int y)> _drawn = new List<(int id, int x, int y)>();
        private Sprite _square;

        /// <summary>Piles drawn on the last frame: the pile's id and its tile. For tests.</summary>
        public IReadOnlyList<(int id, int x, int y)> Drawn => _drawn;

        private void Awake()
        {
            if (host == null) host = FindAnyObjectByType<SimHost>();
            var tex = new Texture2D(1, 1, TextureFormat.RGBA32, false) { filterMode = FilterMode.Point };
            tex.SetPixel(0, 0, Color.white);
            tex.Apply();
            _square = Sprite.Create(tex, new Rect(0, 0, 1, 1), new Vector2(0.5f, 0.5f), 1f);
        }

        private void LateUpdate()
        {
            _drawn.Clear();
            var caches = host == null || host.Simulation == null ? null : host.Simulation.State.Drops?.Caches;
            var n = 0;
            if (caches != null)
            {
                // A slow breath so the eye finds it; unscaled, so it still reads while the game is paused.
                var breath = 0.85f + 0.15f * Mathf.Sin(Time.unscaledTime * 3f);
                for (var i = 0; i < caches.Count; i++)
                {
                    var c = caches[i];
                    if (c.Items.IsEmpty) continue;
                    var at = WorldSpace.TileCentre(c.X, c.Y);
                    at.z = z;
                    Place(Slot(_rims, n, "Dropped Cargo Rim", DrawOrder.DroppedCargoRim), at, size * 1.3f, rimColour);
                    Place(Slot(_bodies, n, "Dropped Cargo", DrawOrder.DroppedCargo), at, size * breath, colour);
                    _drawn.Add((c.Id, c.X, c.Y));
                    n++;
                }
            }
            for (var i = n; i < _bodies.Count; i++) { _bodies[i].enabled = false; _rims[i].enabled = false; }
        }

        private static void Place(SpriteRenderer sr, Vector3 at, float scale, Color tint)
        {
            sr.enabled = true;
            sr.color = tint;
            var t = sr.transform;
            t.position = at;
            t.localScale = new Vector3(scale, scale, 1f);
        }

        private SpriteRenderer Slot(List<SpriteRenderer> pool, int i, string label, int order)
        {
            while (pool.Count <= i)
            {
                var go = new GameObject(label + " " + pool.Count);
                go.transform.SetParent(transform, false);
                go.transform.localRotation = Quaternion.Euler(0f, 0f, 45f);   // a diamond reads as "pick me up", a square as a machine
                var sr = go.AddComponent<SpriteRenderer>();
                sr.sprite = _square;
                sr.sortingOrder = order;
                pool.Add(sr);
            }
            return pool[i];
        }
    }
}
