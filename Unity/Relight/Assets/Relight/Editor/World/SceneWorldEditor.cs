using System;
using System.Collections.Generic;
using System.Text;
using Relight.Presentation;
using Relight.Sim;
using Relight.World;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Tilemaps;
using Object=UnityEngine.Object;
namespace Relight.Editor
{
    /// <summary>Persistent authored objects, disposable visual children. Never serializes the generated city.</summary>
    [InitializeOnLoad]
    public static class SceneWorldEditor
    {
        static GameObject preview;
        static WorldGeometryAsset compiled;
        static readonly List<GameObject> pieces=new List<GameObject>();
        static readonly Dictionary<GameObject,GameObject> selectionTargets=new Dictionary<GameObject,GameObject>();
        static string signature;
        static double next;
        static bool busy;
        static int tileRevision;
        public static string LastError {get;private set;}
        static SceneWorldEditor()
        {
            EditorApplication.update+=Update;
            AssemblyReloadEvents.beforeAssemblyReload+=ClearPreview;
            EditorApplication.playModeStateChanged+=s=>{if(s==PlayModeStateChange.ExitingEditMode)ClearPreview();if(s==PlayModeStateChange.EnteredEditMode)signature=null;};
            EditorSceneManager.sceneClosing+=(s,removing)=>ClearPreview();
            Selection.selectionChanged+=()=>{var go=Selection.activeGameObject;if(go!=null&&selectionTargets.TryGetValue(go,out var target)&&target!=null)Selection.activeGameObject=target;};
            Tilemap.tilemapTileChanged+=(map,changes)=>{if(!busy && map!=null && map.GetComponentInParent<SceneWorld>()!=null)tileRevision++;};
        }
        static SceneWorld Find()=>Object.FindFirstObjectByType<SceneWorld>();
        static void Update()
        {
            if(busy||EditorApplication.isPlayingOrWillChangePlaymode||EditorApplication.isCompiling||EditorApplication.timeSinceStartup<next)return;
            next=EditorApplication.timeSinceStartup+.6;
            var world=Find();if(world==null){if(preview!=null)ClearPreview();return;}
            RepairIds(world);
            var current=Signature(world);if(current==signature)return;signature=current;
            RefreshNow();
        }
        static string Signature(SceneWorld w)
        {
            var s=new StringBuilder();s.Append(JsonUtility.ToJson(w));s.Append(tileRevision);
            foreach(var c in w.GetComponentsInChildren<MonoBehaviour>(true))
                s.Append(c.GetEntityId().ToString()).Append(c.gameObject.activeInHierarchy).Append(c.enabled).Append(JsonUtility.ToJson(c)).Append(c.transform.position.ToString("R")).Append(c.transform.rotation.ToString("R")).Append(c.transform.lossyScale.ToString("R"));
            foreach(var box in w.GetComponentsInChildren<BoxCollider2D>(true))s.Append(JsonUtility.ToJson(box));
            if(w.playerSpawn!=null)s.Append(w.playerSpawn.position.ToString("R"));
            return s.ToString();
        }
        static void RepairIds(SceneWorld w)
        {
            var ids=new HashSet<string>();
            foreach(var b in w.GetComponentsInChildren<SceneBuilding>(true))
                if(string.IsNullOrEmpty(b.id)||!ids.Add(b.id)){Undo.RecordObject(b,"Assign building ID");b.id="building-"+Guid.NewGuid().ToString("N");ids.Add(b.id);EditorUtility.SetDirty(b);}
            ids.Clear();
            foreach(var p in w.GetComponentsInChildren<SceneProp>(true))
                if(string.IsNullOrEmpty(p.id)||!ids.Add(p.id)){Undo.RecordObject(p,"Assign prop ID");p.id="prop-"+Guid.NewGuid().ToString("N");ids.Add(p.id);EditorUtility.SetDirty(p);}
            ids.Clear();
            foreach(var site in w.GetComponentsInChildren<SceneSite>(true))
                if(string.IsNullOrEmpty(site.id)||!ids.Add(site.id)){Undo.RecordObject(site,"Assign site ID");site.id="site-"+Guid.NewGuid().ToString("N");ids.Add(site.id);EditorUtility.SetDirty(site);}
        }
        [MenuItem("Relight/World/Refresh Editable World",false,31)]
        public static void RefreshNow()
        {
            if(EditorApplication.isPlayingOrWillChangePlaymode)return;
            var world=Find();if(world==null)return;
            busy=true;
            try
            {
                // Compile first: an invalid edit leaves the last good world visible and logs a concrete error.
                var nextMap=world.Compile(out var sites);
                ClearPreview();compiled=nextMap;
                preview=new GameObject("Editable world display (generated)");preview.hideFlags=HideFlags.HideAndDontSave;
                var grid=new GameObject("Terrain preview",typeof(Grid));grid.transform.SetParent(preview.transform,false);
                var terrain=new GameObject("Terrain",typeof(Tilemap),typeof(TilemapRenderer));terrain.transform.SetParent(grid.transform,false);
                var solid=new GameObject("Solid",typeof(Tilemap),typeof(TilemapRenderer));solid.transform.SetParent(grid.transform,false);
                var painter=preview.AddComponent<WorldPainter>();
                Set(painter,"palette",world.palette);Set(painter,"terrain",terrain.GetComponent<Tilemap>());Set(painter,"solid",solid.GetComponent<Tilemap>());
                SetBool(painter,"paintOnAwake",false);SetBool(painter,"paintSolidOverlay",false);SetBool(painter,"logPaintTime",false);
                painter.Paint(compiled);
                var city=preview.AddComponent<CityPresenter>();
                var source=Object.FindFirstObjectByType<WorldBootstrap>();
                var live=source!=null?source.GetComponent<CityPresenter>():null;
                if(live!=null)EditorUtility.CopySerialized(live,city);
                SetBool(city,"logBuildTime",false);city.Build(compiled,sites);
                var targets=new Dictionary<string,GameObject>();
                foreach(var b in world.GetComponentsInChildren<SceneBuilding>())if(b.enabled)targets[b.id]=b.gameObject;
                foreach(var p in world.GetComponentsInChildren<SceneProp>())if(p.enabled)targets[p.id]=p.gameObject;
                foreach(var s in world.GetComponentsInChildren<SceneSite>())if(s.enabled)
                {
                    targets["Resource "+s.id]=s.gameObject;targets["Substation "+s.id]=s.gameObject;targets["Stop "+s.id]=s.gameObject;targets["Stop marker "+s.id]=s.gameObject;targets["Yard "+s.id]=s.gameObject;targets["Yard "+s.id+" grid"]=s.gameObject;
                }
                var ordered=new Dictionary<string,Queue<GameObject>>();
                void AddTarget(string key,GameObject go,int count){if(!ordered.TryGetValue(key,out var q)){q=new Queue<GameObject>();ordered.Add(key,q);}for(var i=0;i<count;i++)q.Enqueue(go);}
                foreach(var path in world.GetComponentsInChildren<ScenePath>())
                {
                    if(!path.enabled)continue;var count=Mathf.Max(0,path.points.Count-1);
                    if(path.kind==ScenePathKind.Road){AddTarget("pavement",path.gameObject,count);AddTarget("carriageway",path.gameObject,count);}
                    else if(path.kind==ScenePathKind.Path)AddTarget("path",path.gameObject,count);
                    else{AddTarget("drive",path.gameObject,count);if(path.points.Count>0)AddTarget("drive cap",path.gameObject,2);}
                }
                foreach(var area in world.GetComponentsInChildren<SceneArea>())if(area.enabled)
                {
                    var key=area.serviceArea?"Service area":string.IsNullOrEmpty(area.areaName)?"Square":area.areaName;
                    AddTarget(key,area.gameObject,1);AddTarget(key+" grid",area.gameObject,1);
                }
                foreach(var render in preview.GetComponentsInChildren<SpriteRenderer>())
                {
                    GameObject owner=null;var n=render.name;
                    if(!targets.TryGetValue(n,out owner))
                    {
                        foreach(var suffix in new[]{" floor"," roof"," walls"," wall top"," door"})
                        {var index=n.IndexOf(suffix,StringComparison.Ordinal);if(index>0&&targets.TryGetValue(n.Substring(0,index),out owner))break;}
                    }
                    if(owner==null&&n.StartsWith("Resource ",StringComparison.Ordinal))
                    {var tile=n.LastIndexOf(" tile ",StringComparison.Ordinal);if(tile>0)targets.TryGetValue(n.Substring(0,tile),out owner);}
                    if(owner==null&&ordered.TryGetValue(n,out var queue)&&queue.Count>0)owner=queue.Dequeue();
                    if(!world.showRoofs&&n.EndsWith(" roof",StringComparison.Ordinal))render.enabled=false;
                    if(owner==null)continue;
                    render.transform.SetParent(owner.transform,true);pieces.Add(render.gameObject);selectionTargets[render.gameObject]=owner;
                }
                MarkTransient(preview);
                SceneVisibilityManager.instance.DisablePicking(preview,true);
                foreach(var piece in pieces)MarkTransient(piece);
                LastError=null;signature=Signature(world);SceneView.RepaintAll();
            }
            catch(Exception e){if(LastError!=e.Message)Debug.LogError("Editable World: "+e.Message);LastError=e.Message;}
            finally{busy=false;}
        }
        static void MarkTransient(GameObject go)
        {
            foreach(var t in go.GetComponentsInChildren<Transform>(true))
            {t.gameObject.hideFlags=HideFlags.DontSave|HideFlags.HideInHierarchy;foreach(var c in t.GetComponents<Component>())c.hideFlags=HideFlags.DontSave;}
        }
        public static void ClearPreview()
        {
            selectionTargets.Clear();foreach(var go in pieces)if(go!=null)Object.DestroyImmediate(go);pieces.Clear();
            if(preview!=null){preview.GetComponent<CityPresenter>()?.ReleaseResources();Object.DestroyImmediate(preview);}preview=null;
            if(compiled!=null)Object.DestroyImmediate(compiled);compiled=null;
        }
        static void Set(Object o,string field,Object value){var s=new SerializedObject(o);s.FindProperty(field).objectReferenceValue=value;s.ApplyModifiedPropertiesWithoutUndo();}
        static void SetBool(Object o,string field,bool value){var s=new SerializedObject(o);s.FindProperty(field).boolValue=value;s.ApplyModifiedPropertiesWithoutUndo();}
        static GameObject Group(string name,Transform parent){var g=new GameObject(name);g.transform.SetParent(parent,false);return g;}
        static T At<T>(string name,Transform parent,int x,int y) where T:Component
        {var go=Group(name,parent);go.transform.position=new Vector3(x,-y,0);return go.AddComponent<T>();}
        [MenuItem("Relight/World/Create Editable World From Imported Map",false,30)]
        public static void Create()
        {
            if(EditorApplication.isPlayingOrWillChangePlaymode)throw new InvalidOperationException("Stop Play Mode first.");
            if(Find()!=null){Selection.activeGameObject=Find().gameObject;RefreshNow();return;}
            var bootstrap=Object.FindFirstObjectByType<WorldBootstrap>();if(bootstrap==null)throw new InvalidOperationException("Open World.unity first.");
            var map=OpeningResourceLayout.Build(bootstrap.SourceGeometry,bootstrap.SourceSites,out var sites);
            var root=Group("Editable World",null);Undo.RegisterCreatedObjectUndo(root,"Create editable world");
            var world=root.AddComponent<SceneWorld>();
            world.palette=AssetDatabase.LoadAssetAtPath<Relight.World.TilePalette>("Assets/Relight/World/Tiles/TilePalette.asset");
            var buildingGroup=Group("Buildings",root.transform).transform;
            var propGroup=Group("Props",root.transform).transform;
            var siteGroup=Group("Sites and resources",root.transform).transform;
            var pathGroup=Group("Roads and paths",root.transform).transform;
            var areaGroup=Group("Paved areas",root.transform).transform;
            var buildingObjects=new List<SceneBuilding>();
            foreach(var b in map.Buildings)
            {
                var c=At<SceneBuilding>(b.name+" ["+b.id+"]",buildingGroup,b.rect.x,b.rect.y);
                c.id=b.id;c.buildingName=b.name;c.kind=b.kind;c.size=b.rect.size;c.enterable=b.enterable;c.variant=b.variant;c.roofKey=b.roofKey;c.campaignBuilding=b.fixedBuilding;c.doors.Clear();
                var all=new List<RectInt>();if(b.doorRect.width>0&&b.doorRect.height>0)all.Add(b.doorRect);all.AddRange(b.doors);
                foreach(var d in all)c.doors.Add(new RectInt(d.x-b.rect.x,d.y-b.rect.y,d.width,d.height));
                buildingObjects.Add(c);ClearRect(map,b.rect,false);
            }
            foreach(var p in map.City.props)
            {
                var parent=propGroup;
                foreach(var b in buildingObjects)if(b.enterable&&Contains(b.Record().rect,p.rect)){parent=b.transform;break;}
                var c=At<SceneProp>(p.kind+" ["+p.id+"]",parent,p.rect.x,p.rect.y);c.id=p.id;c.kind=p.kind;c.size=p.rect.size;c.clearable=p.clearable;
            }
            foreach(var s in sites.All)
            {
                var parent=siteGroup;var rect=new RectInt(s.X,s.Y,s.W,s.H);
                if(s.Kind==SiteKind.Core)foreach(var b in buildingObjects)if(Contains(b.Record().rect,rect)){parent=b.transform;break;}
                var c=At<SceneSite>(s.Name+" ["+s.Id+"]",parent,s.X,s.Y);c.id=s.Id;c.siteName=s.Name;c.kind=s.Kind;c.size=rect.size;c.item=s.Item;c.amount=s.Amount;
                if(s.Kind==SiteKind.Substation)ClearRect(map,rect,false);
                if(s.Kind==SiteKind.Resource)ClearRect(map,rect,true);
            }
            CreatePaths(map.City.roads,ScenePathKind.Road,pathGroup);CreatePaths(map.City.paths,ScenePathKind.Path,pathGroup);CreatePaths(map.City.drives,ScenePathKind.Drive,pathGroup);
            foreach(var a in map.City.squares){var c=At<SceneArea>(a.name??"Square",areaGroup,a.rect.x,a.rect.y);c.areaName=a.name;c.size=a.rect.size;}
            foreach(var a in map.City.serviceAreas){var c=At<SceneArea>("Service area",areaGroup,a.x,a.y);c.serviceArea=true;c.size=a.size;}
            world.playerSpawn=Group("Player Spawn",root.transform).transform;world.playerSpawn.position=WorldSpace.TileCentre(map.Spawn.x,map.Spawn.y);
            // Keep the spawn with Home if its core follows that building.
            foreach(var s in root.GetComponentsInChildren<SceneSite>())if(s.kind==SiteKind.Core&&s.GetComponentInParent<SceneBuilding>()!=null){world.playerSpawn.SetParent(s.transform.parent,true);break;}
            var grid=Group("Terrain editing",root.transform);grid.AddComponent<Grid>();
            var edits=Group("Terrain Edits - paint here",grid.transform);world.terrainEdits=edits.AddComponent<Tilemap>();edits.AddComponent<TilemapRenderer>();
            // Source terrain is copied once. Sparse painted changes live in the scene, avoiding 500k serialized cells.
            map.hideFlags=HideFlags.None;map.name="Scene Base Terrain";
            var path=AssetDatabase.GenerateUniqueAssetPath("Assets/Relight/World/Manual/SceneBaseGeometry.asset");AssetDatabase.CreateAsset(map,path);world.baseline=map;
            EditorUtility.SetDirty(world);EditorSceneManager.MarkSceneDirty(root.scene);AssetDatabase.SaveAssets();
            RestoreImportedInteriors();Selection.activeGameObject=root;RefreshNow();FocusHome();
        }
        public static void RestoreImportedInteriors()
        {
            var w=Find();var bootstrap=Object.FindFirstObjectByType<WorldBootstrap>();
            var original=OpeningResourceLayout.Build(bootstrap.SourceGeometry,bootstrap.SourceSites,out _);
            var current=w.Compile(out _);
            try
            {
                // Where a whole imported prop was solid, attach that collision to the editable prop itself.
                foreach(var p in w.GetComponentsInChildren<SceneProp>())
                {
                    var r=p.Record().rect;var all=true;var anyMissing=false;
                    for(var y=r.y;y<r.yMax;y++)for(var x=r.x;x<r.xMax;x++){if(!original.SolidAt(x,y))all=false;if(!current.SolidAt(x,y))anyMissing=true;}
                    if(!all||!anyMissing)continue;
                    Undo.RecordObject(p,"Import prop collision");p.blocksMovement=true;EditorUtility.SetDirty(p);
                    for(var y=r.y;y<r.yMax;y++)for(var x=r.x;x<r.xMax;x++)current.Solid[current.Index(x,y)]=1;
                }
                // Preserve authored internal walls not represented by a complete prop as editable collision boxes.
                foreach(var b in w.GetComponentsInChildren<SceneBuilding>())
                {
                    var r=b.Record().rect;
                    for(var y=r.y;y<r.yMax;y++)
                    {
                        var start=-1;
                        for(var x=r.x;x<=r.xMax;x++)
                        {
                            var missing=x<r.xMax&&original.SolidAt(x,y)&&!current.SolidAt(x,y);
                            if(missing&&start<0)start=x;
                            if(missing||start<0)continue;
                            var c=At<BlocksMovement>("Interior collision "+(y-r.y)+" "+(start-r.x),b.transform,start,y);
                            var box=c.GetComponent<BoxCollider2D>();box.size=new Vector2(x-start,1);box.offset=new Vector2((x-start)*.5f,-.5f);box.isTrigger=true;
                            Undo.RegisterCreatedObjectUndo(c.gameObject,"Import interior collision");
                            for(var xx=start;xx<x;xx++)current.Solid[current.Index(xx,y)]=1;start=-1;
                        }
                    }
                }
            }
            finally{Object.DestroyImmediate(current);Object.DestroyImmediate(original);}
        }
        /// <summary>
        /// Batch 4 (REL-136). Adds a Scene Site for every imported site whose id the editable world lacks, and touches
        /// nothing else: an existing site keeps its authored position, size and amount. Re-runnable; returns the ids added.
        /// Reads the same opening layout <see cref="Create"/> does, and never adds a Resource: resources are authored in
        /// the scene (the opening removes three reference patches near Home), so a missing one was taken out on purpose.
        /// </summary>
        [MenuItem("Relight/World/Add Missing Imported Sites",false,34)]
        public static void AddMissingSitesMenu()=>Debug.Log("Relight: added sites "+string.Join(", ",AddMissingSites()));
        public static List<string> AddMissingSites()
        {
            if(EditorApplication.isPlayingOrWillChangePlaymode)throw new InvalidOperationException("Stop Play Mode first.");
            var w=Find();if(w==null)throw new InvalidOperationException("Open World.unity with its Editable World first.");
            var bootstrap=Object.FindFirstObjectByType<WorldBootstrap>();if(bootstrap==null)throw new InvalidOperationException("Open World.unity first.");
            var have=new HashSet<string>();foreach(var s in w.GetComponentsInChildren<SceneSite>(true))have.Add(s.id);
            var parent=w.transform.Find("Sites and resources")??w.transform;var added=new List<string>();
            var layout=OpeningResourceLayout.Build(bootstrap.SourceGeometry,bootstrap.SourceSites,out var sites);Object.DestroyImmediate(layout);
            foreach(var s in sites.All)
            {
                if(s.Kind==SiteKind.Resource||have.Contains(s.Id))continue;
                var c=At<SceneSite>(s.Name+" ["+s.Id+"]",parent,s.X,s.Y);c.id=s.Id;c.siteName=s.Name;c.kind=s.Kind;c.size=new Vector2Int(s.W,s.H);c.item=s.Item;c.amount=s.Amount;
                Undo.RegisterCreatedObjectUndo(c.gameObject,"Add imported site");added.Add(s.Id);
            }
            if(added.Count>0){EditorUtility.SetDirty(w);EditorSceneManager.MarkSceneDirty(w.gameObject.scene);RefreshNow();}
            return added;
        }
        static bool Contains(RectInt outer,RectInt inner)=>inner.x>=outer.x&&inner.y>=outer.y&&inner.xMax<=outer.xMax&&inner.yMax<=outer.yMax;
        static void ClearRect(WorldGeometryAsset g,RectInt r,bool resource){for(var y=r.y;y<r.yMax;y++)for(var x=r.x;x<r.xMax;x++)if(g.InBounds(x,y)){var i=g.Index(x,y);if(resource){g.Kind[i]=(byte)TileClass.Ground;g.Patch[i]=0;}else g.Solid[i]=0;}}
        static void CreatePaths(List<WorldGeometryAsset.Poly> paths,ScenePathKind kind,Transform parent)
        {
            var i=0;foreach(var p in paths){if(p.points.Count==0)continue;var origin=p.points[0];var c=At<ScenePath>(kind+" "+(++i),parent,origin.x,origin.y);c.kind=kind;foreach(var v in p.points)c.points.Add(v-origin);}
        }
        [MenuItem("Relight/World/Focus Home",false,32)]
        public static void FocusHome()
        {
            var w=Find();if(w==null)return;var view=SceneView.lastActiveSceneView??EditorWindow.GetWindow<SceneView>();view.in2DMode=true;
            view.LookAtDirect(w.playerSpawn.position+new Vector3(12,0,0),Quaternion.identity,25);view.Focus();
        }
        [MenuItem("Relight/World/Toggle Roofs in Scene",false,33)]
        public static void ToggleRoofs(){var w=Find();if(w==null)return;Undo.RecordObject(w,"Toggle editor roofs");w.showRoofs=!w.showRoofs;EditorUtility.SetDirty(w);RefreshNow();}
        static Vector2Int NewPosition(){var v=SceneView.lastActiveSceneView;return v!=null?new Vector2Int(Mathf.RoundToInt(v.pivot.x),Mathf.RoundToInt(-v.pivot.y)):new Vector2Int(105,355);}
        [MenuItem("Relight/World/Add Enterable Building",false,35)]
        public static void AddBuilding(){var w=Find();if(w==null)return;var p=NewPosition();var b=At<SceneBuilding>("New building",w.transform.Find("Buildings"),p.x,p.y);b.id="building-"+Guid.NewGuid().ToString("N");Undo.RegisterCreatedObjectUndo(b.gameObject,"Add building");Selection.activeGameObject=b.gameObject;RefreshNow();}
        [MenuItem("Relight/World/Add Solid Prop",false,36)]
        public static void AddProp(){var w=Find();if(w==null)return;var p=NewPosition();var prop=At<SceneProp>("New solid prop",w.transform.Find("Props"),p.x,p.y);prop.id="prop-"+Guid.NewGuid().ToString("N");prop.blocksMovement=true;Undo.RegisterCreatedObjectUndo(prop.gameObject,"Add prop");Selection.activeGameObject=prop.gameObject;RefreshNow();}
        [MenuItem("Relight/World/Add Iron Deposit",false,37)]
        public static void AddResource(){var w=Find();if(w==null)return;var p=NewPosition();var s=At<SceneSite>("New iron deposit",w.transform.Find("Sites and resources"),p.x,p.y);s.id="resource-"+Guid.NewGuid().ToString("N");s.kind=SiteKind.Resource;s.siteName="Iron ore deposit";s.item="ironore";s.size=new Vector2Int(3,3);s.amount=900;Undo.RegisterCreatedObjectUndo(s.gameObject,"Add resource");Selection.activeGameObject=s.gameObject;RefreshNow();}
    }
}
