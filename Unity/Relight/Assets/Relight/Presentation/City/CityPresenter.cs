using System.Collections.Generic;
using System.Diagnostics;
using Relight.Sim;
using Relight.World;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace Relight.Presentation
{
    /// <summary>
    /// Correction pass C6. Draws the authored city — river, roads, pavements, paths, drives, the tram boulevard,
    /// paved yards and squares, 479 buildings, 370 props, substations, tram stops, resource patches and the region
    /// name labels — from the <see cref="WorldGeometryAsset"/> and its sites, at session start.
    ///
    /// <b>Why procedurally.</b> The reference's artwork is 68 SVG files under
    /// <c>packages/game/public/art/riverfront/</c>. Unity cannot import SVG without <c>com.unity.vectorgraphics</c>,
    /// which this project's manifest does not carry, and there is no rasteriser here to convert them offline. So the
    /// whole city is drawn in the reference's own colours (<c>packages/game/src/riverfrontDraw.ts</c>, and the two
    /// generators the SVGs were themselves produced by) through <see cref="CityPalette"/> and
    /// <see cref="CitySprites"/>. Every roof and floor still asks <see cref="CityArtSet"/> for its sprite FIRST, so
    /// dropping the art package into that asset replaces the tints with no code change.
    ///
    /// <b>Why it is cheap.</b> One 80x64 runtime texture backs every shape, so all ~4,400 renderers share one
    /// texture and Unity's default sprite material and batch. Nothing is drawn per tile: a road is one quad per
    /// polyline segment, a building is four quads, a paved square is two. Tile-level ground (street/ground/rubble/
    /// river/patch colouring) is the tilemap's job — <see cref="WorldPainter"/> paints that from the same asset.
    ///
    /// <b>Sorting</b> is by Z alone (every renderer keeps sortingOrder 0, as the rest of the project's views do), in
    /// the order the brief fixes: ground &lt; roads &lt; paths &lt; tram &lt; yards &lt; building floors &lt;
    /// props/machines/engineer &lt; walls &lt; roofs &lt; labels. Machines sit at z −1 and the engineer at −2, so the
    /// props band at −0.95 puts city furniture just behind them and the walls at −3.5 in front of both, which is what
    /// the reference's depth pass does.
    ///
    /// The presenter owns nothing the sim reads. It is fed by <see cref="WorldBootstrap"/>.
    /// </summary>
    [DisallowMultipleComponent]
    [AddComponentMenu("Relight/City Presenter")]
    public sealed class CityPresenter : MonoBehaviour
    {
        // ------------------------------------------------------------------ depths (see the class comment)
        private const float ZResource = -0.46f;
        private const float ZGround = -0.10f;
        private const float ZDrive = -0.15f;
        private const float ZRoadPavement = -0.20f;
        private const float ZRoadCarriageway = -0.21f;
        private const float ZPath = -0.30f;
        private const float ZTram = -0.35f;
        private const float ZYard = -0.40f;
        private const float ZYardGrid = -0.41f;
        private const float ZStop = -0.45f;
        private const float ZFloor = -0.50f;
        private const float ZProp = -0.95f;
        private const float ZWall = -3.50f;
        private const float ZWallTop = -3.52f;
        private const float ZRoof = -3.60f;
        private const float ZDoor = -3.70f;
        private const float ZLabel = -7.00f;

        [Tooltip("The host whose engineer fades a roof out from under them. Found in the scene if left empty.")]
        [SerializeField] private SimHost host;

        [Tooltip("Optional roof/floor sprites keyed by the exporter's roofKey. Empty = every surface is a flat tint.")]
        [SerializeField] private CityArtSet art;

        [Tooltip("Font for the region name labels. Empty uses Unity's built-in LegacyRuntime.ttf.")]
        [SerializeField] private Font labelFont;

        [Tooltip("Cap height of a region label, in tiles.")]
        [SerializeField] private float labelHeightTiles = 2.5f;

        [Tooltip("Draw the 4-tile paving grid over yards and squares (riverfrontDraw.ts:29).")]
        [SerializeField] private bool drawPavingGrid = true;

        [Tooltip("Draw the region name labels.")]
        [SerializeField] private bool drawLabels = true;

        [Tooltip("Roof alpha while the engineer stands inside an enterable building (riverfrontDraw.ts:44).")]
        [SerializeField, Range(0f, 1f)] private float insideRoofAlpha = 0.35f;

        [Tooltip("How fast a roof approaches its target alpha (the reference's dt * 5).")]
        [SerializeField, Min(0f)] private float roofFadePerSecond = 5f;

        [Tooltip("Log the object count and build time once per build (the C6 evidence line).")]
        [SerializeField] private bool logBuildTime = true;

        [Tooltip("Straight-line tolerance, in tiles, when the 4,909-sample tram centreline is reduced to quads.")]
        [SerializeField, Min(0.001f)] private float tramSimplifyTiles = 0.05f;

        private CitySprites _sprites;
        private Transform _root;
        private WorldGeometryAsset _built;
        private readonly List<RoofFade> _roofs = new List<RoofFade>();
        private ResourceNodeArtSet _resourceArt;
        private readonly List<ResourceView> _resources=new List<ResourceView>();
        private SimState _resourceState;
        private SimState _resourceInitial;
        private long _resourceTick=-1;
        public int ResourceSpriteCount=>_resources.Count;
        private sealed class ResourceView
        {
            public SpriteRenderer Renderer;
            public int X,Y,Variant,Stage=-2;
            public ResourceNodeKind Kind;
            public double Initial;
        }
        private int _count;
        private int _buildings, _props, _segments, _areas, _labels;

        /// <summary>Renderers the last build created. The brief's budget: a few thousand, not one per tile.</summary>
        public int ObjectCount => _count;

        /// <summary>Milliseconds the last <see cref="Build"/> took.</summary>
        public double LastBuildMilliseconds { get; private set; }

        /// <summary>The region currently drawn, or null.</summary>
        public WorldGeometryAsset Built => _built;

        /// <summary>Enterable buildings whose roof fades while the engineer is inside (22 on the full city).</summary>
        public int FadingRoofs => _roofs.Count;

        /// <summary>How many streetlight lamp heads are drawn, and how many of them are drawn lit.</summary>
        public int StreetLamps => _lamps.Count;
        public int LitStreetLamps { get { var n = 0; for (var i = 0; i < _lamps.Count; i++) if (_lamps[i].Lit) n++; return n; } }

        private sealed class StreetLamp
        {
            public SiteRecord Site;
            public SpriteRenderer Head, Glow;
            public bool Lit;
        }
        private readonly List<StreetLamp> _lamps = new List<StreetLamp>();
        private SimState _lampState;
        private int _lampTick = -1;

        private sealed class RoofFade
        {
            public SpriteRenderer Renderer;
            public RectInt Rect;
            public Color Colour;
            public float Alpha;
        }

        private void Awake()
        {
            if (host == null) host = FindAnyObjectByType<SimHost>();
        }

        private void OnDestroy() => ReleaseResources();

        /// <summary>Also called by the editor preview, whose components do not enter the Play Mode lifecycle.</summary>
        public void ReleaseResources()
        {
            if (_sprites != null) _sprites.Dispose();
            _sprites = null;
        }

        /// <summary>
        /// Rebuild the whole city from <paramref name="geometry"/>. Safe to call again (the previous draw is
        /// destroyed first); called by <see cref="WorldBootstrap.StartSession"/> after the sim's context is built.
        /// <paramref name="sites"/> may be <see cref="WorldSites.Empty"/> — the geometry alone still draws.
        /// </summary>
        public void Build(WorldGeometryAsset geometry, WorldSites sites)
        {
            ClearCity();
            if (geometry == null) return;
            if (_sprites == null) _sprites = CitySprites.Create();
            if (sites == null) sites = WorldSites.Empty;

            var watch = Stopwatch.StartNew();
            var decor = geometry.City;
            _root = NewGroup(transform, "City");

            var ground = NewGroup(_root, "Ground");
            var roads = NewGroup(_root, "Roads");
            var paths = NewGroup(_root, "Paths");
            var tram = NewGroup(_root, "Tram");
            var paved = NewGroup(_root, "Paved areas");
            var floors = NewGroup(_root, "Building floors");
            var props = NewGroup(_root, "Props");
            var walls = NewGroup(_root, "Building walls");
            var roofs = NewGroup(_root, "Building roofs");
            var doors = NewGroup(_root, "Doors");
            var siteGroup = NewGroup(_root, "Sites");
            var labels = NewGroup(_root, "Labels");

            BuildGround(ground, geometry, decor, sites);
            BuildRoads(roads, decor);
            BuildPolylines(paths, decor.paths, 1.4f, CityPalette.Path, ZPath, "path");
            BuildDrives(paths, decor);
            BuildTram(tram, decor);
            BuildPaved(paved, decor, sites);
            BuildBuildings(floors, walls, roofs, doors, geometry);
            BuildProps(props, decor);
            BuildSites(siteGroup, sites);
            if (drawLabels) BuildLabels(labels, sites);

            watch.Stop();
            LastBuildMilliseconds = watch.Elapsed.TotalMilliseconds;
            _built = geometry;
            if (logBuildTime)
                Debug.Log($"Relight: drew city '{geometry.RegionId}' in {LastBuildMilliseconds:0.0} ms — " +
                          $"{_count} objects ({_buildings} buildings, {_props} props, {_segments} road/path/drive/tram " +
                          $"segments, {_areas} paved areas, {_labels} labels, {_roofs.Count} fading roofs). " +
                          $"Art set: {(art != null && art.HasAny ? art.name : "none — every surface is a flat tint")}.");
        }

        /// <summary>Destroy everything the last build made.</summary>
        public void ClearCity()
        {
            _roofs.Clear();
            _lamps.Clear(); _lampState = null; _lampTick = -1;
            _resources.Clear();_resourceState=null;_resourceInitial=null;_resourceTick=-1;
            _count = _buildings = _props = _segments = _areas = _labels = 0;
            _built = null;
            if (_root == null) return;
            var go = _root.gameObject;
            _root = null;
            if (Application.isPlaying) Destroy(go); else DestroyImmediate(go);
        }

        // ------------------------------------------------------------------ the passes

        /// <summary>
        /// The river beyond the tilemap. Every land tile is already painted by <see cref="WorldPainter"/>, so this is
        /// only the one tint the tilemap cannot express as well: a single quad from the river's north edge down.
        /// A crop that does not reach the river (Home ends 19 rows short of it) draws nothing.
        /// </summary>
        private void BuildGround(Transform parent, WorldGeometryAsset g, WorldGeometryAsset.CityDecor decor,
            WorldSites sites)
        {
            if (decor.riverY > 0 && decor.riverY < g.Height)
                Area(parent, "River", 0f, decor.riverY, g.Width, g.Height - decor.riverY,
                    _sprites.Quad, CityPalette.River, ZGround);

            _resourceArt=ResourceNodeArtSet.Load();
            if(_resourceArt==null)
            {
                foreach(var site in sites.OfKind(SiteKind.Resource))
                    Area(parent,"Resource "+site.Id,site.X,site.Y,site.W,site.H,_sprites.Quad,CityPalette.Resource(site.Item),ZResource);
                return;
            }
            // One independent sprite per mineable cell: partial extraction and depletion match the real tile.
            for(var y=0;y<g.Height;y++)for(var x=0;x<g.Width;x++)
            {
                var tile=g.TileAt(x,y);if(!Tiles.CanHoldUnits(tile))continue;
                var kind=ResourceNodeArtSet.Kind(tile,(PatchType)g.Patch[g.Index(x,y)]);
                var variant=ResourceNodeArtSet.Variant(x,y);var sprite=_resourceArt.Find(kind,variant,0);if(sprite==null)continue;
                string id=null;foreach(var site in sites.OfKind(SiteKind.Resource))if(site.Contains(x,y)){id=site.Id;break;}
                var label=id!=null?"Resource "+id+" tile "+x+","+y:"Mineable "+kind+" tile "+x+","+y;
                var renderer=Spawn(parent,label,sprite,Color.white);
                renderer.transform.localPosition=new Vector3(x+.5f,-y-.5f,-.7f);
                // Use a uniform scale so painted fragments retain their aspect ratio across depletion rows.
                renderer.transform.localScale=new Vector3(1f/sprite.bounds.size.x,1f/sprite.bounds.size.x,1f);
                _resources.Add(new ResourceView{Renderer=renderer,X=x,Y=y,Kind=kind,Variant=variant});
            }
        }

        /// <summary>
        /// Roads as two passes, exactly as the reference draws them (riverfrontDraw.ts:20-22): the full
        /// pavement+carriageway band first, then the carriageway inside it. Each segment is extended by its own half
        /// width at both ends, which fakes the reference's round joins without a disc per node (210 objects saved).
        /// </summary>
        private void BuildRoads(Transform parent, WorldGeometryAsset.CityDecor decor)
        {
            var half = Mathf.Max(1, decor.roadHalfWidth);
            var inner = Mathf.Max(1, half - Mathf.Max(0, decor.roadPavement));
            BuildPolylines(parent, decor.roads, half * 2f, CityPalette.Pavement, ZRoadPavement, "pavement");
            BuildPolylines(parent, decor.roads, inner * 2f, CityPalette.Carriageway, ZRoadCarriageway, "carriageway");

            // Founders Court is a bulb on the road graph, not a polyline (riverfront.ts court).
            if (decor.courtRadius > 0f)
            {
                var c = new Vector2(decor.court.x, decor.court.y);
                Dot(parent, "Court pavement", c, decor.courtRadius * 2f, CityPalette.Pavement, ZRoadPavement);
                Dot(parent, "Court carriageway", c, Mathf.Max(1f, decor.courtRadius - decor.roadPavement) * 2f,
                    CityPalette.Carriageway, ZRoadCarriageway);
            }
        }

        private void BuildPolylines(Transform parent, List<WorldGeometryAsset.Poly> lines, float width, Color colour,
            float z, string name)
        {
            if (lines == null) return;
            var extend = width * 0.5f;
            for (var i = 0; i < lines.Count; i++)
            {
                var pts = lines[i] != null ? lines[i].points : null;
                if (pts == null || pts.Count < 2) continue;
                for (var j = 1; j < pts.Count; j++)
                    Segment(parent, name, pts[j - 1], pts[j], width, colour, z, extend);
            }
        }

        /// <summary>Service drives: a 4-tile band with a round cap at each end (riverfrontDraw.ts:26).</summary>
        private void BuildDrives(Transform parent, WorldGeometryAsset.CityDecor decor)
        {
            if (decor.drives == null) return;
            BuildPolylines(parent, decor.drives, 4f, CityPalette.Pavement, ZDrive, "drive");
            foreach (var line in decor.drives)
            {
                var pts = line != null ? line.points : null;
                if (pts == null || pts.Count == 0) continue;
                Dot(parent, "drive cap", pts[0], 4f, CityPalette.Pavement, ZDrive);
                Dot(parent, "drive cap", pts[pts.Count - 1], 4f, CityPalette.Pavement, ZDrive);
            }
        }

        /// <summary>
        /// The tram boulevard, 2.8 tiles wide along the baked centreline (riverfrontDraw.ts:27). The bake is 4,909
        /// samples on the full city — one quad each would be absurd, so the polyline is reduced to the handful of
        /// straights and arc steps that stay within <see cref="tramSimplifyTiles"/> of it first.
        /// </summary>
        private void BuildTram(Transform parent, WorldGeometryAsset.CityDecor decor)
        {
            var pts = Simplify(decor.tramSamples, tramSimplifyTiles);
            for (var i = 1; i < pts.Count; i++)
                Segment(parent, "tram", pts[i - 1], pts[i], 2.8f, CityPalette.Tram, ZTram, 1.4f);
        }

        /// <summary>Factory yards, paved squares and civic service areas, each a tint plus a repeating grid.</summary>
        private void BuildPaved(Transform parent, WorldGeometryAsset.CityDecor decor, WorldSites sites)
        {
            foreach (var s in sites.OfKind(SiteKind.Yard))
                Paved(parent, "Yard " + s.Id, new RectInt(s.X, s.Y, s.W, s.H), CityPalette.Paving,
                    CityPalette.PavingGrid);

            if (decor.squares != null)
                foreach (var sq in decor.squares)
                    if (sq != null && sq.rect.width > 0 && sq.rect.height > 0)
                        Paved(parent, string.IsNullOrEmpty(sq.name) ? "Square" : sq.name, sq.rect,
                            CityPalette.Paving, CityPalette.PavingGrid);

            if (decor.serviceAreas != null)
                foreach (var a in decor.serviceAreas)
                    if (a.width > 0 && a.height > 0)
                        Paved(parent, "Service area", a, CityPalette.ServiceArea, CityPalette.ServiceGrid);
        }

        private void Paved(Transform parent, string name, RectInt r, Color fill, Color grid)
        {
            Area(parent, name, r.x, r.y, r.width, r.height, _sprites.Quad, fill, ZYard);
            if (drawPavingGrid) Grid(parent, name + " grid", r, grid, ZYardGrid);
            _areas++;
        }

        /// <summary>
        /// One building is four renderers: the wall block, its lit north run, the roof (inset half a tile so the
        /// wall reads as a rim, which is what the reference's depth offset produces) and the door marker. Only an
        /// enterable building gets a fifth, its floor, because only there can the roof fade away and reveal it.
        /// </summary>
        private void BuildBuildings(Transform floors, Transform walls, Transform roofs, Transform doors,
            WorldGeometryAsset g)
        {
            var list = g.Buildings;
            if (list == null) return;
            for (var i = 0; i < list.Count; i++)
            {
                var b = list[i];
                if (b == null) continue;
                var r = b.rect;
                if (r.width <= 0 || r.height <= 0) continue;
                _buildings++;
                var label = string.IsNullOrEmpty(b.id) ? "building" : b.id;

                if (b.enterable)
                {
                    var floorSprite = art != null ? art.Find(CityArtSet.FloorKey(b.kind)) : null;
                    Area(floors, label + " floor", r.x, r.y, r.width, r.height,
                        floorSprite != null ? floorSprite : _sprites.Quad,
                        floorSprite != null ? Color.white : CityPalette.Floor(b.kind), ZFloor);
                }

                if(!b.enterable)
                {
                    Area(walls,label+" walls",r.x,r.y,r.width,r.height,_sprites.Quad,CityPalette.WallSide,ZWall);
                    Area(walls,label+" wall top",r.x,r.y,r.width,1f,_sprites.Quad,CityPalette.WallTop,ZWallTop);
                }
                else
                {
                    for(var y=r.y;y<r.yMax;y++)
                    {
                        var run=-1;
                        for(var x=r.x;x<=r.xMax;x++)
                        {
                            var solid=x<r.xMax&&(y==r.y||y==r.yMax-1||x==r.x||x==r.xMax-1);
                            var point=new Vector2Int(x,y);
                            if(b.doorRect.Contains(point))solid=false;
                            foreach(var d in b.doors)if(d.Contains(point))solid=false;
                            if(solid&&run<0)run=x;
                            if(!solid&&run>=0){Area(walls,label+" walls "+y+" "+run,run,y,x-run,1,_sprites.Quad,y==r.y?CityPalette.WallTop:CityPalette.WallSide,ZWall);run=-1;}
                        }
                    }
                }

                var inset = Mathf.Min(0.5f, Mathf.Min(r.width, r.height) * 0.25f);
                var roofSprite = art != null ? art.Find(b.roofKey) : null;
                var roof = Area(roofs, label + " roof", r.x + inset, r.y + inset,
                    r.width - inset * 2f, r.height - inset * 2f,
                    roofSprite != null ? roofSprite : _sprites.Quad,
                    roofSprite != null ? Color.white : CityPalette.Roof(b.kind, b.variant), ZRoof);
                if (b.enterable)
                    _roofs.Add(new RoofFade {Renderer = roof, Rect = r, Colour = roof.color, Alpha = 1f});

                Door(doors, label, b.doorRect, b.enterable);
                if (b.doors != null)
                    for (var d = 0; d < b.doors.Count; d++) Door(doors, label, b.doors[d], b.enterable);
            }
        }

        private void Door(Transform parent, string label, RectInt d, bool enterable)
        {
            if (d.width <= 0 || d.height <= 0) return;
            Area(parent, label + " door", d.x, d.y, d.width, d.height,
                enterable ? _sprites.DoorOpen : _sprites.DoorSealed, Color.white, ZDoor);
        }

        private void BuildProps(Transform parent, WorldGeometryAsset.CityDecor decor)
        {
            if (decor.props == null) return;
            foreach (var p in decor.props)
            {
                if (p == null || p.rect.width <= 0 || p.rect.height <= 0) continue;
                _props++;
                Area(parent, string.IsNullOrEmpty(p.id) ? p.kind : p.id, p.rect.x, p.rect.y, p.rect.width,
                    p.rect.height, _sprites.Prop(p.kind, p.id), Color.white, ZProp);
            }
        }

        /// <summary>Substation lots and tram stop platforms (riverfrontDraw.ts:65-67).</summary>
        private void BuildSites(Transform parent, WorldSites sites)
        {
            foreach (var s in sites.OfKind(SiteKind.Substation))
                Area(parent, "Substation " + s.Id, s.X, s.Y, s.W, s.H, _sprites.Substation, Color.white, ZProp);

            // The authored streetlights. The post sits in the world under the darkness; the lamp head and its glow
            // are drawn over it, so a dead lamp can be found in the dark and a lit one reads as the light's source.
            // Which it is comes from the sim (StreetLights.Lit), once a tick, in RefreshStreetLights.
            var all = sites.All;
            for (var i = 0; i < all.Count; i++)
            {
                var s = all[i];
                if (!StreetLights.IsStreetLight(s)) continue;
                Area(parent, "Street light post " + s.Id, s.X + 0.36f, s.Y + 0.36f, 0.28f, 0.28f, _sprites.Quad,
                    CityPalette.LampPost, ZProp);
                var glow = Area(parent, "Street light glow " + s.Id, s.X - 0.4f, s.Y - 0.4f, 1.8f, 1.8f, _sprites.Disc,
                    CityPalette.LampGlow, ZProp);
                glow.sortingOrder = DrawOrder.StreetLampGlow;
                glow.enabled = false;
                var head = Area(parent, "Street light head " + s.Id, s.X + 0.2f, s.Y + 0.2f, 0.6f, 0.6f, _sprites.Disc,
                    CityPalette.LampDead, ZProp);
                head.sortingOrder = DrawOrder.StreetLamp;
                _lamps.Add(new StreetLamp { Site = s, Head = head, Glow = glow });
            }

            foreach (var s in sites.OfKind(SiteKind.TramStop))
            {
                Area(parent, "Stop " + s.Id, s.X - 1f, s.Y - 1f, 4f, 1f, _sprites.Quad,
                    CityPalette.StopPlatform, ZStop);
                Area(parent, "Stop marker " + s.Id, s.X - 1f, s.Y + 2f, 4f, 0.25f, _sprites.Quad,
                    CityPalette.StopMarker, ZStop);
            }
        }

        /// <summary>
        /// Region names as world-space text. Legacy <see cref="TextMesh"/> rather than UI Toolkit world labels: UI
        /// Toolkit has no world-space text in this project's setup, and a label here is nine static strings that must
        /// pan and zoom with the map — which is exactly what a TextMesh in the scene does for free.
        /// </summary>
        private void BuildLabels(Transform parent, WorldSites sites)
        {
            var font = labelFont != null ? labelFont : Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (font == null) return;
            const int fontSize = 40;
            foreach (var s in sites.OfKind(SiteKind.Label))
            {
                var go = new GameObject("Label " + s.Id);
                go.transform.SetParent(parent, false);
                var text = go.AddComponent<TextMesh>();
                text.text = string.IsNullOrEmpty(s.Name) ? s.Id : s.Name;
                text.font = font;
                text.fontSize = fontSize;
                // TextMesh sizes a line at roughly fontSize * characterSize / 10 world units.
                text.characterSize = Mathf.Max(0.01f, labelHeightTiles * 10f / fontSize);
                text.anchor = TextAnchor.MiddleCenter;
                text.alignment = TextAlignment.Center;
                text.color = CityPalette.Rgb(0xd8d3bb, 0.85f);
                var mesh = go.GetComponent<MeshRenderer>();
                if (mesh != null)
                {
                    mesh.sharedMaterial = font.material;
                    mesh.sortingOrder = 600;   // above every sprite, as LightingPresenter is at 500
                }
                var c = s.Centre;
                go.transform.localPosition = new Vector3((float)c.X, -(float)c.Y, ZLabel);
                _count++;
                _labels++;
            }
        }

        // ------------------------------------------------------------------ roof fade

        /// <summary>
        /// riverfrontDraw.ts:44 — a roof fades out while the engineer is inside the building, so the interior is
        /// visible without a separate interior scene. Only the 22 enterable buildings are checked, once a frame.
        /// </summary>
        private void LateUpdate()
        {
            RefreshResources();
            RefreshStreetLights();
            if (_roofs.Count == 0) return;
            var sim = host == null ? null : host.Simulation;
            if (sim == null) return;
            var e = WorldQueries.Engineer(sim.Context, sim.State).Pos;
            var ex = (float)e.X;
            var ey = (float)e.Y;
            var k = Mathf.Clamp01(Time.deltaTime * roofFadePerSecond);
            for (var i = 0; i < _roofs.Count; i++)
            {
                var roof = _roofs[i];
                if (roof.Renderer == null) continue;
                var r = roof.Rect;
                var inside = ex > r.x + 1 && ex < r.x + r.width - 1 && ey > r.y + 1 && ey < r.y + r.height - 1;
                var target = inside ? insideRoofAlpha : 1f;
                if (Mathf.Approximately(roof.Alpha, target)) continue;
                roof.Alpha = Mathf.Lerp(roof.Alpha, target, k);
                if (Mathf.Abs(roof.Alpha - target) < 0.004f) roof.Alpha = target;
                var c = roof.Colour;
                c.a = roof.Alpha;
                roof.Renderer.color = c;
            }
        }

        /// <summary>Lamp heads follow the sim's answer, once a tick. Picture only: nothing here lights a tile.</summary>
        private void RefreshStreetLights()
        {
            if (_lamps.Count == 0) return;
            var sim = host != null ? host.Simulation : null;
            if (sim == null) return;
            if (ReferenceEquals(_lampState, sim.State) && _lampTick == sim.State.Tick) return;
            _lampState = sim.State; _lampTick = sim.State.Tick;
            for (var i = 0; i < _lamps.Count; i++)
            {
                var lamp = _lamps[i];
                if (lamp.Head == null) continue;
                var lit = StreetLights.Lit(sim.Context, sim.State, lamp.Site);
                if (lit == lamp.Lit) continue;
                lamp.Lit = lit;
                lamp.Head.color = lit ? CityPalette.LampLit : CityPalette.LampDead;
                if (lamp.Glow != null) lamp.Glow.enabled = lit;
            }
        }

        /// <summary>Read actual remaining amounts, including loaded saves. Does not write gameplay state.</summary>
        public void RefreshResources(bool force=false)
        {
            var sim=host!=null?host.Simulation:null;
            if(sim==null||_resourceArt==null)return;
            if(!ReferenceEquals(_resourceState,sim.State))
            {
                _resourceState=sim.State;_resourceInitial=new SimState{OpeningResourceVersion=sim.State.OpeningResourceVersion};
                foreach(var node in _resources){node.Initial=Ground.UnitsAt(sim.Context,_resourceInitial,node.X,node.Y);node.Stage=-2;}
                force=true;
            }
            if(!force&&_resourceTick==sim.State.Tick)return;
            _resourceTick=sim.State.Tick;
            foreach(var node in _resources)
            {
                if(node.Renderer==null)continue;
                var stage=ResourceNodeArtSet.Stage(Ground.UnitsAt(sim.Context,sim.State,node.X,node.Y),node.Initial);
                if(stage==node.Stage)continue;node.Stage=stage;
                node.Renderer.sprite=_resourceArt.Find(node.Kind,node.Variant,stage);
                node.Renderer.enabled=stage>=0;
            }
        }

        // ------------------------------------------------------------------ primitives

        private static Transform NewGroup(Transform parent, string name)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            return go.transform;
        }

        private SpriteRenderer Spawn(Transform parent, string name, Sprite sprite, Color colour)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var r = go.AddComponent<SpriteRenderer>();
            r.sprite = sprite;
            r.color = colour;
            _count++;
            return r;
        }

        /// <summary>
        /// Draw <paramref name="sprite"/> over the sim rect whose north-west corner is (x, y). Pivot-agnostic: the
        /// sprite's own bounds place it, so an imported art sprite with a centred pivot lands in the same rect as the
        /// procedural quad, whose pivot is its bottom-left corner.
        /// </summary>
        private SpriteRenderer Area(Transform parent, string name, float x, float y, float w, float h, Sprite sprite,
            Color colour, float z)
        {
            var r = Spawn(parent, name, sprite, colour);
            var b = sprite.bounds;
            var sx = b.size.x > 0f ? w / b.size.x : w;
            var sy = b.size.y > 0f ? h / b.size.y : h;
            r.transform.localScale = new Vector3(sx, sy, 1f);
            // Sim (x, y) is Y-down from the top-left; the rect's bottom edge in Unity is -(y + h) (WorldSpace).
            r.transform.localPosition = new Vector3(x - b.min.x * sx, -(y + h) - b.min.y * sy, z);
            return r;
        }

        /// <summary>A quad along the sim-space segment a → b, <paramref name="width"/> tiles wide.</summary>
        private SpriteRenderer Segment(Transform parent, string name, Vector2 a, Vector2 b, float width, Color colour,
            float z, float extend)
        {
            var ax = a.x;
            var ay = -a.y;
            var bx = b.x;
            var by = -b.y;
            var dx = bx - ax;
            var dy = by - ay;
            var len = Mathf.Sqrt(dx * dx + dy * dy);
            var angle = len > 0.0001f ? Mathf.Atan2(dy, dx) * Mathf.Rad2Deg : 0f;
            var r = Spawn(parent, name, _sprites.QuadCentre, colour);
            r.transform.localPosition = new Vector3((ax + bx) * 0.5f, (ay + by) * 0.5f, z);
            r.transform.localRotation = Quaternion.Euler(0f, 0f, angle);
            r.transform.localScale = new Vector3(len + extend * 2f, width, 1f);
            _segments++;
            return r;
        }

        private SpriteRenderer Dot(Transform parent, string name, Vector2 centre, float diameter, Color colour, float z)
        {
            var r = Spawn(parent, name, _sprites.DiscCentre, colour);
            r.transform.localPosition = new Vector3(centre.x, -centre.y, z);
            r.transform.localScale = new Vector3(diameter, diameter, 1f);
            return r;
        }

        /// <summary>A repeating 4-tile grid over a rect, as one tiled renderer rather than a quad per grid line.</summary>
        private SpriteRenderer Grid(Transform parent, string name, RectInt rect, Color colour, float z)
        {
            var r = Spawn(parent, name, _sprites.PavingGrid, colour);
            r.drawMode = SpriteDrawMode.Tiled;
            r.tileMode = SpriteTileMode.Continuous;
            r.size = new Vector2(rect.width, rect.height);
            r.transform.localPosition = new Vector3(rect.x, -(rect.y + rect.height), z);
            return r;
        }

        /// <summary>
        /// Reduce a dense polyline to the fewest points that stay within <paramref name="tolerance"/> tiles of it.
        /// A forward-greedy pass, not Douglas-Peucker: it is O(n · run) with the run capped, which on the tram's
        /// 4,909 samples is a few hundred thousand comparisons once, at session start.
        /// </summary>
        private static List<Vector2> Simplify(List<Vector2> points, float tolerance)
        {
            var kept = new List<Vector2>();
            if (points == null || points.Count == 0) return kept;
            if (points.Count <= 2) { kept.AddRange(points); return kept; }
            const int maxRun = 256;
            var tol2 = tolerance * tolerance;
            kept.Add(points[0]);
            var anchor = 0;
            for (var i = 2; i < points.Count; i++)
            {
                var a = points[anchor];
                var b = points[i];
                var dx = b.x - a.x;
                var dy = b.y - a.y;
                var len2 = dx * dx + dy * dy;
                var straight = i - anchor <= maxRun;
                for (var j = anchor + 1; straight && j < i; j++)
                {
                    var p = points[j];
                    var t = len2 > 0f ? Mathf.Clamp01(((p.x - a.x) * dx + (p.y - a.y) * dy) / len2) : 0f;
                    var ex = p.x - (a.x + dx * t);
                    var ey = p.y - (a.y + dy * t);
                    if (ex * ex + ey * ey > tol2) straight = false;
                }
                if (straight) continue;
                kept.Add(points[i - 1]);
                anchor = i - 1;
            }
            kept.Add(points[points.Count - 1]);
            return kept;
        }
    }
}
