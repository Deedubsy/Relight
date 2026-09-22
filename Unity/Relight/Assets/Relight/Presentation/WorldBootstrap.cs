using Relight.Data;
using Relight.Sim;
using Relight.World;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Relight.Presentation
{
    /// <summary>
    /// B-13. Starts a session in <c>World.unity</c> and loads the UI scene on top of it.
    ///
    /// This is NOT the "monolithic runtime constructor" TECHNICAL_ARCHITECTURE.md §7.1 forbids: it creates no
    /// GameObject, no camera, no tilemap and no view. Everything visible is authored in the scene asset (the editor
    /// menu <c>Relight/Setup/Build World Scene</c> writes it once, a human edits it afterwards). All this does is
    /// hand <see cref="SimHost"/> the two things only code can build — the data record from the B-05 registry and
    /// the world geometry (the C-01 imported region when one is assigned, the Phase B synthetic map otherwise) —
    /// and pick the seed.
    ///
    /// The UI scene is loaded additively from here rather than from <c>Boot</c> because the world scene is what the
    /// Editor's Play button runs when a developer opens it directly, and the panel has to be there when it does.
    /// Boot loading both would work equally well and would be the better answer once there is a main menu flow that
    /// always precedes the world (B-14); the report records the choice.
    /// </summary>
    [DisallowMultipleComponent]
    [AddComponentMenu("Relight/World Bootstrap")]
    public sealed class WorldBootstrap : MonoBehaviour
    {
        [Tooltip("The host this starts. Found on this object if left empty.")]
        [SerializeField] private SimHost host;

        [Tooltip("The B-05 data registry asset. Its Build() is the sim's GameData.")]
        [SerializeField] private GameDataRegistry registry;

        [Tooltip("Imported authored region (C-01). Leave empty to run the Phase B synthetic map instead.")]
        [SerializeField] private WorldGeometryAsset geometry;

        [Tooltip("Generated sites of the imported region (C-01). Read only when Geometry is set.")]
        [SerializeField] private HomeSitesAsset sites;

        [Tooltip("Optional manual site overrides (outside Generated/); win per site id.")]
        [SerializeField] private HomeSitesOverrides siteOverrides;

        [Tooltip("Fills the Terrain/Solid tilemaps from Geometry at session start (U-M-35: the scene stores no cells). " +
                 "Found in the scene when left empty.")]
        [SerializeField] private WorldPainter painter;

        [Tooltip("Draws the city (roads, buildings, props, labels) from Geometry. Found in the scene when left empty.")]
        [SerializeField] private CityPresenter city;

        [Tooltip("Campaign seed. Same seed plus same commands plus same data = same state (TA 10.4).")]
        [SerializeField] private int seed = 1;

        [Tooltip("Start a session on Awake. Off lets a menu or a load call StartSession() instead.")]
        [SerializeField] private bool startOnAwake = true;

        [Tooltip("Scene loaded additively for the UI. Empty loads nothing (it may already be open).")]
        [SerializeField] private string uiSceneName = "GameUI";

        /// <summary>The seed the session was started with, for the report line in a debug panel.</summary>
        public int Seed => seed;

        /// <summary>
        /// The authored city the session is running on (riverfront.ts <c>RIVERFRONT_ID</c>), or empty on the
        /// synthetic map. A save written from this session belongs to this map: the binding belongs in the save
        /// HEADER, next to the schema version, not in <see cref="SimState"/> — the coordinator owns
        /// <c>SaveSerializer</c>, so this property is the hand-off (see the Wave 1 W-A report).
        /// </summary>
        public string MapId => host != null && host.Simulation != null ? host.Simulation.Context.MapId : geometry != null ? geometry.MapId : string.Empty;
        public WorldGeometryAsset SourceGeometry => geometry;
        public WorldSites SourceSites => geometry != null ? SiteBridge.ToSim(HomeSites.Resolve(sites,siteOverrides),geometry.RegionId,geometry.OriginX,geometry.OriginY) : WorldSites.Empty;

        /// <summary>Which region of that city is loaded (<c>city.json</c> <c>region.id</c>), or empty.</summary>
        public string RegionId => geometry != null ? geometry.RegionId : string.Empty;

        /// <summary>The sha256 of the reference sources the region was exported from, or empty.</summary>
        public string MapSourceSha256 => geometry != null ? geometry.SourceSha256 : string.Empty;

        /// <summary>True when the session runs on imported authored geometry rather than the synthetic map.</summary>
        public bool HasImportedRegion => geometry != null;

        private void Awake()
        {
            if (host == null) host = GetComponent<SimHost>();
            if (startOnAwake) StartSession();
        }

        // REL-84 (E-19): a tuning asset edited in the Inspector while the game runs is rebuilt into the live
        // simulation on the next frame. OnValidate can fire several times for one edit, and from the editor's own
        // thread of events, so the reload is only marked here and done in Update, once, between two frames' ticks.
        private bool _reloadPending;

        private void OnEnable() => DataDefinition.Edited += MarkReload;
        private void OnDisable() => DataDefinition.Edited -= MarkReload;
        private void MarkReload(DataDefinition _) => _reloadPending = true;

        private void Update()
        {
            if (!_reloadPending) return;
            _reloadPending = false;
            ReloadData();
        }

        /// <summary>
        /// Rebuild the data record from the registry's assets as they are now and hand it to the running game
        /// (<see cref="Simulation.ReplaceData"/>). The same build the session started with is used — the opening
        /// balance for a versioned opening, the legacy build for a save from before it — so a reload changes only
        /// what was edited. Returns true when the live game now runs on different data. Public so an Admin control
        /// can call it too; nothing here is a debug grant, it re-reads the assets a build already shipped with.
        /// </summary>
        public bool ReloadData()
        {
            if (host == null || host.Simulation == null || registry == null) return false;
            var sim = host.Simulation;
            var data = sim.State.OpeningResourceVersion > 0 ? registry.Build() : registry.BuildLegacy();
            if (data == null) return false;
            var before = GameDataHash.Compute(sim.Context.Data);
            var after = GameDataHash.Compute(data);
            if (before == after) return false;
            sim.ReplaceData(data);
            Debug.Log($"Relight: tuning reloaded at T={sim.State.T:0.0}: dataVersion {before} → {after}. " +
                      "A save written now records the new hash; loading it against the old data warns only.");
            return true;
        }

        private void Start()
        {
            if (string.IsNullOrEmpty(uiSceneName)) return;
            if (SceneManager.GetSceneByName(uiSceneName).isLoaded) return;
            if (!Application.CanStreamedLevelBeLoaded(uiSceneName))
            {
                Debug.LogError($"Relight: UI scene '{uiSceneName}' is not in Build Settings.");
                return;
            }
            SceneManager.LoadSceneAsync(uiSceneName, LoadSceneMode.Additive);
        }

        /// <summary>Build the context and start a new campaign. Safe to call again; the host replaces its simulation.</summary>
        /// <summary>Start a new campaign on an explicit seed (C-10 New Game). Same contract as StartSession().</summary>
        public Simulation StartSession(int withSeed) { seed = withSeed; return StartSession(); }

        public Simulation StartSession()
        {
            if (host == null)
            {
                Debug.LogError("Relight: WorldBootstrap has no SimHost.");
                return null;
            }
            if (registry == null)
            {
                Debug.LogError("Relight: WorldBootstrap has no GameDataRegistry; assign the asset in the Inspector.");
                return null;
            }
            // A new session starts with fresh view state: the static survives a domain reload, the session does not.
            ViewState.Reset();
            // C-01: the imported Home region when one is assigned, the Phase B synthetic map when not. Build()
            // returns null and logs when the asset is inconsistent, and the synthetic map then keeps the scene
            // runnable rather than starting a session on half a world.
            var ctx=ContextForLayout(OpeningResourceLayout.Version);
            if(ctx==null)return null;
            var sim=Simulation.NewGame(ctx,seed);
            sim.State.OpeningResourceVersion=OpeningResourceLayout.Version;
            return host.Attach(sim);
        }

        private WorldGeometryAsset _openingGeometry;
        public SimContext ContextForLayout(int version, bool useScene = true)
        {
            var active=geometry;
            var siteList=geometry!=null ? SiteBridge.ToSim(HomeSites.Resolve(sites,siteOverrides),geometry.RegionId,geometry.OriginX,geometry.OriginY):WorldSites.Empty;
            var authored=FindFirstObjectByType<SceneWorld>();
            if(version>0 && useScene && authored!=null && authored.useForNewGames)
            {
                if(_openingGeometry!=null) Destroy(_openingGeometry);
                _openingGeometry=authored.Compile(out siteList);
                active=_openingGeometry;
            }
            else if(version>0 && geometry!=null)
            {
                if(_openingGeometry!=null) Destroy(_openingGeometry);
                _openingGeometry=OpeningResourceLayout.Build(geometry,siteList,out siteList);
                active=_openingGeometry;
            }
            if(Application.isPlaying&&authored!=null)authored.SetRuntimeVisuals(version>0&&useScene&&authored.useForNewGames);
            var map=active!=null ? ImportedGeometry.Build(active):null;
            if(active!=null && map==null)return null;
            if(map!=null)
            {
                if(painter==null)painter=FindFirstObjectByType<WorldPainter>(FindObjectsInactive.Include);
                if(painter!=null)painter.Paint(active);
                if(city==null)city=FindFirstObjectByType<CityPresenter>(FindObjectsInactive.Include);
                if(city!=null)city.Build(active,siteList);
            }
            return new SimContext(version>0 ? registry.Build() : registry.BuildLegacy(),map??SyntheticMap.Create(),threat:new EnemyThreatLayer(),sites:siteList,mapId:active!=null?active.MapId:null);

        }
    }
}
