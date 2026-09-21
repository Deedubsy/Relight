using System.Collections.Generic;
using Relight.Sim;
using UnityEngine;

namespace Relight.Presentation
{
    /// <summary>
    /// Developer overlay: the district borders, a faint tint per district, each district's size, and an optional
    /// roamer-density preview, drawn over the running game. Switched from the Admin panel (F8, World page), so it
    /// exists only in the editor and in development builds.
    ///
    /// It draws from <see cref="DistrictMap"/> and sits above the darkness (<see cref="DrawOrder.Darkness"/>) so it
    /// reads at night. It sends no command, changes no state and is never saved. The preview dots are markers, not
    /// enemies: nothing is spawned.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class DistrictOverlay : MonoBehaviour
    {
        private const int OrderTint = 590, OrderBorder = 591, OrderDot = 592, OrderText = 601;
        private const float ZOverlay = -7.5f;
        private const float BorderTiles = 0.35f, TintAlpha = 0.10f, DotTiles = 1.6f, StatsHeightTiles = 1.3f;

        private static DistrictOverlay _instance;

        private SimHost _host;
        private WorldSites _builtFor;
        private DistrictMap _map;
        private Material _material;
        private Mesh _dotMesh;
        private Transform _drawn, _dots;
        private readonly List<Object> _owned = new List<Object>();
        private DistrictMap.Preview _preview;

        public static bool Shown => _instance != null && _instance.gameObject.activeSelf;
        public static DistrictMap.Preview Preview => _instance == null ? DistrictMap.Preview.Off : _instance._preview;

        /// <summary>Shows or hides the overlay, creating it the first time. Refused outside editor/development builds.</summary>
        public static void Show(SimHost host, bool on)
        {
            if (!(Application.isEditor || Debug.isDebugBuild)) return;
            if (_instance == null)
            {
                if (!on || host == null) return;
                _instance = new GameObject("District Overlay (debug)").AddComponent<DistrictOverlay>();
                _instance._host = host;
            }
            _instance.gameObject.SetActive(on);
        }

        /// <summary>Steps the roamer preview: off, 4 each, 1 per 5,000 street tiles, 1 per 2,500. Returns the new setting.</summary>
        public static DistrictMap.Preview CyclePreview()
        {
            if (_instance == null) return DistrictMap.Preview.Off;
            _instance._preview = (DistrictMap.Preview)(((int)_instance._preview + 1) % 4);
            _instance.BuildDots();
            return _instance._preview;
        }

        /// <summary>One line for the Admin readout: the district the engineer stands in. Empty while hidden.</summary>
        public static string Describe(Vec2 engineer)
        {
            if (!Shown || _instance._map == null) return "";
            var d = _instance._map.At(engineer.X, engineer.Y);
            if (d == null) return "";
            var line = $"District: {d.Name} · {d.Walkable:N0} walkable · {d.Screens:0} screens";
            if (_instance._preview != DistrictMap.Preview.Off)
                line += $" · preview {DistrictMap.PreviewCount(d, _instance._preview)} roamers here";
            return line;
        }

        private void LateUpdate()
        {
            var sim = _host == null ? null : _host.Simulation;
            if (sim == null) { Clear(); return; }
            if (ReferenceEquals(_builtFor, sim.Context.Sites) && _drawn != null) return;
            Clear();
            _builtFor = sim.Context.Sites;
            _map = DistrictMap.Build(sim.Context);
            Draw();
        }

        private void Draw()
        {
            if (_material == null) _material = new Material(Shader.Find("Sprites/Default"));
            _drawn = new GameObject("Drawn").transform;
            _drawn.SetParent(transform, false);

            // Tint: one texel a tile, stretched over the map. Pivot top-left, so the sprite hangs down from (0, 0).
            var tex = Own(_map.TintTexture(TintAlpha));
            var sprite = Own(Sprite.Create(tex, new Rect(0, 0, _map.W, _map.H), new Vector2(0f, 1f), 1f, 0, SpriteMeshType.FullRect));
            var tint = Child("Tint", _drawn).AddComponent<SpriteRenderer>();
            tint.sprite = sprite;
            tint.sortingOrder = OrderTint;
            tint.transform.localPosition = new Vector3(0f, 0f, ZOverlay);

            var border = Child("Borders", _drawn);
            border.AddComponent<MeshFilter>().sharedMesh = Own(_map.BorderMesh(BorderTiles, ZOverlay, false));
            var mr = border.AddComponent<MeshRenderer>();
            mr.sharedMaterial = _material;
            mr.sortingOrder = OrderBorder;

            var font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (font != null)
                foreach (var d in _map.Districts)
                {
                    // Under the city's own name label, which CityPresenter already draws at this site.
                    var go = Child("Stats " + d.Name, _drawn);
                    var text = go.AddComponent<TextMesh>();
                    text.text = _map.Stats(d);
                    text.font = font;
                    text.fontSize = 40;
                    text.characterSize = StatsHeightTiles * 10f / 40f;
                    text.anchor = TextAnchor.UpperCenter;
                    text.alignment = TextAlignment.Center;
                    text.color = d.Colour;
                    var r = go.GetComponent<MeshRenderer>();
                    r.sharedMaterial = font.material;
                    r.sortingOrder = OrderText;
                    go.transform.localPosition = new Vector3(d.LabelTile.x, -(d.LabelTile.y + 2f), ZOverlay);
                }
            BuildDots();
        }

        private void BuildDots()
        {
            if (_dots != null) Destroy(_dots.gameObject);
            if (_dotMesh != null) { _owned.Remove(_dotMesh); Destroy(_dotMesh); }
            _dots = null; _dotMesh = null;
            if (_map == null || _drawn == null || _preview == DistrictMap.Preview.Off) return;
            _dots = Child("Roamer preview", _drawn).transform;
            var quads = new List<Vector3>();
            var h = DotTiles * 0.5f;
            foreach (var d in _map.Districts)
            foreach (var p in _map.PreviewTiles(d, _preview))
            {
                // A diamond, so a marker is never mistaken for a tile or a machine.
                quads.Add(new Vector3(p.x, -(p.y - h), ZOverlay));
                quads.Add(new Vector3(p.x + h, -p.y, ZOverlay));
                quads.Add(new Vector3(p.x, -(p.y + h), ZOverlay));
                quads.Add(new Vector3(p.x - h, -p.y, ZOverlay));
            }
            var mesh = _dotMesh = Own(DistrictMap.QuadMesh(quads, false));
            var colours = new Color[quads.Count];
            for (var i = 0; i < colours.Length; i++) colours[i] = new Color(1f, 0.25f, 0.85f, 0.95f);
            mesh.colors = colours;
            _dots.gameObject.AddComponent<MeshFilter>().sharedMesh = mesh;
            var mr = _dots.gameObject.AddComponent<MeshRenderer>();
            mr.sharedMaterial = _material;
            mr.sortingOrder = OrderDot;
        }

        private GameObject Child(string name, Transform parent)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            return go;
        }

        private T Own<T>(T o) where T : Object { _owned.Add(o); return o; }

        private void Clear()
        {
            if (_drawn != null) Destroy(_drawn.gameObject);
            _drawn = null; _dots = null; _map = null; _builtFor = null;
            foreach (var o in _owned) if (o != null) Destroy(o);
            _owned.Clear();
            _dotMesh = null;
        }

        private void OnDestroy()
        {
            Clear();
            if (_material != null) Destroy(_material);
            if (_instance == this) _instance = null;
        }
    }
}
