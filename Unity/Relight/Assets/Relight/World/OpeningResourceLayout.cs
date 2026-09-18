using System;
using System.Collections.Generic;
using Relight.Sim;
using UnityEngine;
namespace Relight.World
{
    public static class OpeningResourceLayout
    {
        public const int Version=1;
        public static WorldGeometryAsset Build(WorldGeometryAsset source,WorldSites original,out WorldSites sites)
        {
            var g=UnityEngine.Object.Instantiate(source);
            g.name=source.name+" - ore opening v1";g.hideFlags=HideFlags.DontSave;
            var rows=new List<SiteRecord>();
            foreach(var s in original.All)
            {
                var x=s.X+source.OriginX;var y=s.Y+source.OriginY;
                if(s.Kind==SiteKind.Resource && ((x==59 && (y==352 || y==359)) || (x==76 && y==359)))
                {
                    Rect(g,s.X,s.Y,s.W,s.H,PatchType.None);continue;
                }
                if(s.Kind!=SiteKind.Resource) {rows.Add(s);continue;}
                var item=s.Item=="steel"?"ironore":s.Item=="copper"?"copperore":s.Item;
                var name=item=="ironore"?"Iron ore deposit":item=="copperore"?"Copper ore deposit":s.Name;
                rows.Add(new SiteRecord(s.Id,name,s.Kind,s.X,s.Y,s.W,s.H,item,s.Amount));
                var patch=Patch(item);if(patch!=PatchType.None)Rect(g,s.X,s.Y,s.W,s.H,patch);
            }
            // Rubble retains its geometry and clearing behaviour but cannot bypass metal processing.
            for(var i=0;i<g.Kind.Length;i++)
            {
                if(g.Patch[i]==(byte)PatchType.Steel || (g.Kind[i]==(byte)TileClass.Rubble && g.Patch[i]==0)) g.Patch[i]=(byte)PatchType.IronOre;
                else if(g.Patch[i]==(byte)PatchType.Copper)g.Patch[i]=(byte)PatchType.CopperOre;
            }
            Add(g,rows,"opening-iron-v1","Iron ore deposit",84,352,3,4,"ironore",7680);
            Add(g,rows,"opening-copper-v1","Copper ore deposit",96,352,3,4,"copperore",1200);
            Add(g,rows,"opening-coal-v1","Coal deposit",96,369,3,3,"coal",700);
            sites=new WorldSites(rows,original.RegionId,original.OriginX,original.OriginY);
            return g;
        }
        static PatchType Patch(string key)
        {
            switch(key) {case "ironore":return PatchType.IronOre;case "copperore":return PatchType.CopperOre;case "coal":return PatchType.Coal;case "crude":return PatchType.Crude;case "stone":return PatchType.Stone;default:return PatchType.None;}
        }
        static void Add(WorldGeometryAsset g,List<SiteRecord> rows,string id,string name,int cityX,int cityY,int w,int h,string item,int amount)
        {
            var x=cityX-g.OriginX;var y=cityY-g.OriginY;
            if(!g.InBounds(x,y)||!g.InBounds(x+w-1,y+h-1))return;
            for(var yy=y;yy<y+h;yy++)for(var xx=x;xx<x+w;xx++)
                if(g.SolidAt(xx,yy) || g.TileAt(xx,yy)!=TileClass.Ground)throw new InvalidOperationException("Opening deposit requires clear ground: "+id);
            Rect(g,x,y,w,h,Patch(item));
            rows.Add(new SiteRecord(id,name,SiteKind.Resource,x,y,w,h,item,amount));
        }
        static void Rect(WorldGeometryAsset g,int x,int y,int w,int h,PatchType patch)
        {
            for(var yy=y;yy<y+h;yy++)for(var xx=x;xx<x+w;xx++)
            {
                if(!g.InBounds(xx,yy)||g.SolidAt(xx,yy))continue;
                var i=g.Index(xx,yy);g.Kind[i]=(byte)(patch==PatchType.None?TileClass.Ground:TileClass.Patch);g.Patch[i]=(byte)patch;
            }
        }
    }
}
