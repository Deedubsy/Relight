using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using Relight.Sim;
using UnityEngine;
using UnityEngine.Tilemaps;
namespace Relight.World
{
    /// <summary>Authored scene objects are the input. A fresh engine-free map is built at session start.</summary>
    [AddComponentMenu("Relight/World/Editable World")]
    public sealed class SceneWorld : MonoBehaviour
    {
        [Tooltip("Isolated starting terrain; original imported assets are never modified.")]
        public WorldGeometryAsset baseline;
        public TilePalette palette;
        [Tooltip("Paint changes here with the Tile Palette. Erasing a change reveals the baseline terrain.")]
        public Tilemap terrainEdits;
        public Transform playerSpawn;
        public bool useForNewGames=true;
        [Tooltip("Editor only: hide roofs to furnish enterable interiors.")]
        public bool showRoofs=true;
        private readonly Dictionary<Renderer,bool> authoredVisibility=new Dictionary<Renderer,bool>();
        public void SetRuntimeVisuals(bool active)
        {
            // The compiled terrain is painted by WorldPainter. Authored overrides must not draw twice or leak into legacy saves.
            foreach(var renderer in GetComponentsInChildren<Renderer>(true))
            {
                if(!authoredVisibility.TryGetValue(renderer,out var configured)){configured=renderer.enabled;authoredVisibility[renderer]=configured;}
                renderer.enabled=active&&configured&&(terrainEdits==null||renderer.gameObject!=terrainEdits.gameObject);
            }
        }
        public static RectInt Rect(Transform t,Vector2Int size)=>new RectInt(Mathf.RoundToInt(t.position.x),Mathf.RoundToInt(-t.position.y),Mathf.Max(1,size.x),Mathf.Max(1,size.y));
        public static void DrawRect(RectInt r,Color c){Gizmos.color=c;Gizmos.DrawWireCube(new Vector3(r.x+r.width*.5f,-r.y-r.height*.5f,-8),new Vector3(r.width,r.height,0));}
        static void CheckTransform(Transform t)
        {
            if(Quaternion.Angle(t.rotation,Quaternion.identity)>.01f||Vector3.Distance(t.lossyScale,Vector3.one)>.001f)
                throw new InvalidOperationException(t.name+": use Position and Size; grid objects must have zero rotation and unit scale.");
        }
        static void CheckRect(WorldGeometryAsset g,RectInt r,string label)
        {
            if(r.width<1||r.height<1||!g.InBounds(r.x,r.y)||!g.InBounds(r.xMax-1,r.yMax-1))throw new InvalidOperationException(label+": footprint is outside the map or empty.");
        }
        public WorldGeometryAsset Compile(out WorldSites sites)
        {
            if(baseline==null)throw new InvalidOperationException("Editable World needs its baseline asset.");
            var g=Instantiate(baseline);g.hideFlags=HideFlags.DontSave;
            try
            {
                var buildings=new List<WorldGeometryAsset.BuildingRecord>();
                var decor=JsonUtility.FromJson<WorldGeometryAsset.CityDecor>(JsonUtility.ToJson(baseline.City));
                decor.props.Clear();decor.roads.Clear();decor.paths.Clear();decor.drives.Clear();decor.squares.Clear();decor.serviceAreas.Clear();
                // Baseline contains terrain and non-building obstacles only. Every authored footprint is rebuilt.
                if(terrainEdits!=null)
                {
                    var bounds=terrainEdits.cellBounds;
                    foreach(var cell in bounds.allPositionsWithin)
                    {
                        var tile=terrainEdits.GetTile(cell);if(tile==null)continue;
                        var x=cell.x;var y=-cell.y-1;if(!g.InBounds(x,y))throw new InvalidOperationException("Terrain edit outside the map.");
                        var found=false;
                        for(var k=0;k<=6;k++)if(palette.For((TileClass)k)==tile){g.Kind[g.Index(x,y)]=(byte)k;g.Patch[g.Index(x,y)]=0;found=true;break;}
                        if(!found)throw new InvalidOperationException("Use Relight terrain tiles in Terrain Edits: "+tile.name);
                    }
                }
                var ids=new HashSet<string>();
                foreach(var b in GetComponentsInChildren<SceneBuilding>())
                {
                    if(!b.enabled)continue;CheckTransform(b.transform);
                    if(string.IsNullOrEmpty(b.id)||!ids.Add(b.id))throw new InvalidOperationException("Building IDs must be unique: "+b.name);
                    var r=b.Record();CheckRect(g,r.rect,b.name);buildings.Add(r);
                    foreach(var d in b.doors)
                        if(b.enterable && (d.width<1||d.height<1||d.x<0||d.y<0||d.xMax>b.size.x||d.yMax>b.size.y))throw new InvalidOperationException(b.name+": door must be within the building footprint.");
                    for(var y=r.rect.y;y<r.rect.yMax;y++)for(var x=r.rect.x;x<r.rect.xMax;x++)
                    {
                        var point=new Vector2Int(x,y);var wall=!r.enterable||x==r.rect.x||y==r.rect.y||x==r.rect.xMax-1||y==r.rect.yMax-1;
                        if(r.enterable){if(r.doorRect.Contains(point))wall=false;foreach(var d in r.doors)if(d.Contains(point))wall=false;}
                        if(wall)g.Solid[g.Index(x,y)]=1;
                    }
                }
                foreach(var p in GetComponentsInChildren<SceneProp>())
                {
                    if(!p.enabled)continue;CheckTransform(p.transform);var r=p.Record();CheckRect(g,r.rect,p.name);decor.props.Add(r);if(p.blocksMovement)FillSolid(g,r.rect);
                }
                foreach(var b in GetComponentsInChildren<BlocksMovement>())
                {
                    if(!b.enabled)continue;
                    if(Quaternion.Angle(b.transform.rotation,Quaternion.identity)>.01f)throw new InvalidOperationException(b.name+": Blocks Movement boxes must be axis aligned.");
                    var r=b.Footprint();if(r.width==0||r.height==0)continue;CheckRect(g,r,b.name);FillSolid(g,r);
                }
                var rows=new List<SiteRecord>();ids.Clear();
                foreach(var s in GetComponentsInChildren<SceneSite>())
                {
                    if(!s.enabled)continue;CheckTransform(s.transform);var r=s.Record();CheckRect(g,new RectInt(r.X,r.Y,r.W,r.H),s.name);
                    if(!ids.Add(r.Id))throw new InvalidOperationException("Site IDs must be unique: "+r.Id);
                    rows.Add(r);
                    if(r.Kind==SiteKind.Substation)FillSolid(g,new RectInt(r.X,r.Y,r.W,r.H));
                    if(r.Kind!=SiteKind.Resource)continue;
                    var patch=ResourcePatch(r.Item);if(patch==PatchType.None)throw new InvalidOperationException(s.name+": unsupported resource key "+r.Item);
                    for(var y=r.Y;y<r.Y+r.H;y++)for(var x=r.X;x<r.X+r.W;x++)
                    {
                        if(g.SolidAt(x,y))throw new InvalidOperationException(s.name+": resource overlaps a solid object.");
                        var i=g.Index(x,y);g.Kind[i]=(byte)TileClass.Patch;g.Patch[i]=(byte)patch;
                    }
                }
                foreach(var p in GetComponentsInChildren<ScenePath>())
                {
                    if(!p.enabled)continue;CheckTransform(p.transform);var r=p.Record();
                    (p.kind==ScenePathKind.Road?decor.roads:p.kind==ScenePathKind.Path?decor.paths:decor.drives).Add(r);
                }
                foreach(var a in GetComponentsInChildren<SceneArea>())
                {
                    if(!a.enabled)continue;CheckTransform(a.transform);var r=Rect(a.transform,a.size);CheckRect(g,r,a.name);
                    if(a.serviceArea)decor.serviceAreas.Add(r);else decor.squares.Add(new WorldGeometryAsset.NamedRect{name=a.areaName,rect=r});
                }
                var spawn=playerSpawn!=null?new Vector2Int(Mathf.FloorToInt(playerSpawn.position.x),Mathf.FloorToInt(-playerSpawn.position.y)):g.Spawn;
                if(!g.InBounds(spawn.x,spawn.y)||g.SolidAt(spawn.x,spawn.y))throw new InvalidOperationException("Player Spawn is outside the map or blocked.");
                sites=new WorldSites(rows,g.RegionId,g.OriginX,g.OriginY);
                g.Fill(g.MapId,g.RegionId,g.RegionName,g.SourceSha256,g.ManifestId,g.ManifestHash,g.ManifestVersion,g.Schema,g.Width,g.Height,g.OriginX,g.OriginY,spawn,g.Kind,g.Variant,g.Patch,g.Solid,buildings,g.Validation);
                g.FillCity(decor);
                var mapId=baseline.MapId+"-scene-"+Fingerprint(g,rows);
                g.Fill(mapId,g.RegionId,g.RegionName,g.SourceSha256,g.ManifestId,g.ManifestHash,g.ManifestVersion,g.Schema,g.Width,g.Height,g.OriginX,g.OriginY,spawn,g.Kind,g.Variant,g.Patch,g.Solid,buildings,g.Validation);
                return g;
            }
            catch {if(Application.isPlaying)Destroy(g);else DestroyImmediate(g);throw;}
        }
        static void FillSolid(WorldGeometryAsset g,RectInt r){for(var y=r.y;y<r.yMax;y++)for(var x=r.x;x<r.xMax;x++)g.Solid[g.Index(x,y)]=1;}
        public static PatchType ResourcePatch(string key)
        {
            switch(key){case "ironore":return PatchType.IronOre;case "copperore":return PatchType.CopperOre;case "coal":return PatchType.Coal;case "stone":return PatchType.Stone;case "crude":return PatchType.Crude;case "steel":return PatchType.Steel;case "copper":return PatchType.Copper;default:return PatchType.None;}
        }
        static string Fingerprint(WorldGeometryAsset g,List<SiteRecord> sites)
        {
            using(var bytes=new MemoryStream())using(var w=new BinaryWriter(bytes))
            {
                w.Write(g.Width);w.Write(g.Height);w.Write(g.Spawn.x);w.Write(g.Spawn.y);w.Write(g.Kind);w.Write(g.Patch);w.Write(g.Solid);
                foreach(var s in sites){w.Write(s.Id);w.Write((int)s.Kind);w.Write(s.X);w.Write(s.Y);w.Write(s.W);w.Write(s.H);w.Write(s.Item??"");w.Write(s.Amount);}
                w.Flush();using(var hash=SHA256.Create())return BitConverter.ToString(hash.ComputeHash(bytes.ToArray())).Replace("-","").Substring(0,16).ToLowerInvariant();
            }
        }
    }
}
