using System;
using System.Collections.Generic;
using NUnit.Framework;
using Relight.World;
using Relight.Sim;
using UnityEngine;
using UnityEngine.Tilemaps;
using Object=UnityEngine.Object;
namespace Relight.Authoring.Tests
{
    public sealed class SceneWorldAuthoringTests
    {
        GameObject root;SceneWorld world;WorldGeometryAsset baseline;List<Object> disposable;
        [SetUp] public void Setup()
        {
            disposable=new List<Object>();root=new GameObject("Authoring test");world=root.AddComponent<SceneWorld>();
            baseline=ScriptableObject.CreateInstance<WorldGeometryAsset>();var k=new byte[400];for(var i=0;i<400;i++)k[i]=(byte)TileClass.Ground;
            baseline.Fill("test-map","test","Test","","","",1,1,20,20,0,0,Vector2Int.zero,k,new byte[400],new byte[400],new byte[400],new List<WorldGeometryAsset.BuildingRecord>(),new WorldGeometryAsset.RegionValidation());
            world.baseline=baseline;
        }
        [TearDown] public void Teardown(){foreach(var o in disposable)if(o!=null)Object.DestroyImmediate(o);Object.DestroyImmediate(root);Object.DestroyImmediate(baseline);}
        T Make<T>(int x,int y)where T:Component{var go=new GameObject(typeof(T).Name);go.transform.SetParent(root.transform);go.transform.position=new Vector3(x,-y,0);return go.AddComponent<T>();}
        SceneBuilding Building(){var b=Make<SceneBuilding>(3,3);b.id="building";b.size=new Vector2Int(6,6);b.doors=new List<RectInt>{new RectInt(2,5,2,1)};return b;}
        WorldGeometryAsset Compile(){var g=world.Compile(out _);disposable.Add(g);return g;}
        [Test] public void EnterableBuilding_HasWallsClearInteriorAndDoor()
        {Building();var g=Compile();Assert.That(g.SolidAt(3,3));Assert.That(g.SolidAt(4,4),Is.False);Assert.That(g.SolidAt(5,8),Is.False);Assert.That(g.SolidAt(4,8));Assert.That(baseline.SolidAt(3,3),Is.False);}
        [Test] public void MoveAndDeleteBuilding_ClearOldCollisionAndMoveChildProp()
        {var b=Building();var p=Make<SceneProp>(4,4);p.id="prop";p.blocksMovement=true;p.transform.SetParent(b.transform,true);var first=Compile();b.transform.position+=new Vector3(7,0,0);var moved=Compile();Assert.That(moved.SolidAt(3,3),Is.False);Assert.That(moved.SolidAt(10,3));Assert.That(moved.SolidAt(11,4));Assert.That(moved.MapId,Is.Not.EqualTo(first.MapId));Object.DestroyImmediate(b.gameObject);Assert.That(Compile().SolidAt(10,3),Is.False);}
        [Test] public void ResourceMoveAndAmount_ChangeActualMiningTiles()
        {var s=Make<SceneSite>(2,2);s.id="ore";s.kind=SiteKind.Resource;s.item="ironore";s.amount=120;s.size=new Vector2Int(2,2);var first=Compile();Assert.That(first.Patch[first.Index(2,2)],Is.EqualTo((byte)PatchType.IronOre));s.transform.position=new Vector3(12,-12);var moved=world.Compile(out var sites);disposable.Add(moved);Assert.That(moved.TileAt(2,2),Is.EqualTo(TileClass.Ground));Assert.That(moved.Patch[moved.Index(12,12)],Is.EqualTo((byte)PatchType.IronOre));Assert.That(sites.Find("ore").Amount,Is.EqualTo(120));}
        [Test] public void DisabledObjects_DoNotAffectMap()
        {var b=Building();b.enabled=false;Assert.That(Compile().SolidAt(3,3),Is.False);b.enabled=true;b.gameObject.SetActive(false);Assert.That(Compile().SolidAt(3,3),Is.False);}
        [Test] public void SolidPropToggle_ChangesCollisionOnlyWhenEnabled()
        {var p=Make<SceneProp>(10,10);p.id="p";Assert.That(Compile().SolidAt(10,10),Is.False);p.blocksMovement=true;Assert.That(Compile().SolidAt(10,10));}
        [Test] public void BlocksMovement_BoxCoversOnlyOccupiedCells()
        {var b=Make<BlocksMovement>(10,10);var box=b.GetComponent<BoxCollider2D>();box.offset=new Vector2(1,-.5f);box.size=new Vector2(2,1);var g=Compile();Assert.That(g.SolidAt(10,10));Assert.That(g.SolidAt(11,10));Assert.That(g.SolidAt(9,10),Is.False);Assert.That(g.SolidAt(12,10),Is.False);Assert.That(g.SolidAt(10,9),Is.False);}
        [Test] public void InvalidRotationDuplicateIdsAndResourceOverlap_AreRejected()
        {var b=Building();b.transform.rotation=Quaternion.Euler(0,0,45);Assert.Throws<InvalidOperationException>(()=>world.Compile(out _));b.transform.rotation=Quaternion.identity;var other=Building();Assert.Throws<InvalidOperationException>(()=>world.Compile(out _));Object.DestroyImmediate(other.gameObject);var s=Make<SceneSite>(3,3);s.id="ore";s.kind=SiteKind.Resource;s.item="ironore";Assert.Throws<InvalidOperationException>(()=>world.Compile(out _));}
        [Test] public void TerrainPaintAndErase_RoundTripWithoutChangingBaseline()
        {var go=new GameObject("Tiles",typeof(Grid));go.transform.SetParent(root.transform);var child=new GameObject("Edits",typeof(Tilemap));child.transform.SetParent(go.transform);world.terrainEdits=child.GetComponent<Tilemap>();var tile=ScriptableObject.CreateInstance<Tile>();var palette=ScriptableObject.CreateInstance<Relight.World.TilePalette>();disposable.Add(tile);disposable.Add(palette);palette.SetTiles(null,null,null,null,tile,null,null,null);world.palette=palette;world.terrainEdits.SetTile(WorldSpace.Cell(10,10),tile);Assert.That(Compile().TileAt(10,10),Is.EqualTo(TileClass.River));world.terrainEdits.SetTile(WorldSpace.Cell(10,10),null);Assert.That(Compile().TileAt(10,10),Is.EqualTo(TileClass.Ground));Assert.That(baseline.TileAt(10,10),Is.EqualTo(TileClass.Ground));}
        [Test] public void MapIdentity_IsStableAndRejectsSaveAfterLayoutChange()
        {var b=Building();var g=Compile();Assert.That(Compile().MapId,Is.EqualTo(g.MapId));var ctx=new SimContext(ReferenceData.Create(),ImportedGeometry.Build(g),sites:new WorldSites(Array.Empty<SiteRecord>(),"test"),mapId:g.MapId);var sim=Simulation.NewGame(ctx,1);sim.State.OpeningResourceVersion=1;var bytes=SaveSerializer.Write(sim.State,ctx,"test",out _);var load=SaveSerializer.Read(bytes,ctx);Assert.That(load.Ok,load.Reason);b.transform.position+=new Vector3(5,0);var moved=Compile();var next=new SimContext(ctx.Data,ImportedGeometry.Build(moved),sites:ctx.Sites,mapId:moved.MapId);Assert.That(SaveSerializer.Read(bytes,next).Ok,Is.False);}
    }
}
