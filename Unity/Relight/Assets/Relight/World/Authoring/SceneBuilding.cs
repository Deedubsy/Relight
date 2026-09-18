using System;
using System.Collections.Generic;
using UnityEngine;
namespace Relight.World
{
    [AddComponentMenu("Relight/World/Scene Building")]
    public sealed class SceneBuilding : MonoBehaviour
    {
        public string id;
        public string buildingName = "Building";
        public string kind = "workshop";
        public Vector2Int size = new Vector2Int(8,8);
        public bool enterable = true;
        [Tooltip("Door rectangles relative to the top-left corner, in tiles; Y points south.")]
        public List<RectInt> doors = new List<RectInt>{new RectInt(3,7,2,1)};
        public int variant;
        public string roofKey;
        public bool campaignBuilding;
        public WorldGeometryAsset.BuildingRecord Record()
        {
            var r=SceneWorld.Rect(transform,size);
            var ds=new List<RectInt>();
            foreach(var d in doors)ds.Add(new RectInt(r.x+d.x,r.y+d.y,d.width,d.height));
            return new WorldGeometryAsset.BuildingRecord {id=id,name=buildingName,kind=kind,rect=r,parcel=r,visual=r,
                facing="S",enterable=enterable,hasDoor=ds.Count>0,doorRect=ds.Count>0?ds[0]:default,
                doors=ds.Count>1?ds.GetRange(1,ds.Count-1):new List<RectInt>(),variant=variant,roofKey=roofKey,fixedBuilding=campaignBuilding};
        }
        private void OnDrawGizmosSelected()
        {
            SceneWorld.DrawRect(SceneWorld.Rect(transform,size),Color.cyan);
            var r=SceneWorld.Rect(transform,size);
            foreach(var d in doors)SceneWorld.DrawRect(new RectInt(r.x+d.x,r.y+d.y,d.width,d.height),Color.green);
        }
    }
}
