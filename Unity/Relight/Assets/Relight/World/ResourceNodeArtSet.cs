using System;
using Relight.Sim;
using UnityEngine;
namespace Relight.World
{
    public enum ResourceNodeKind { IronOre, CopperOre, Coal, Stone, Crude, Rubble }
    [CreateAssetMenu(menuName="Relight/Resource Node Art",fileName="ResourceNodes")]
    public sealed class ResourceNodeArtSet : ScriptableObject
    {
        [Serializable] public sealed class Entry
        {
            public ResourceNodeKind kind;
            [Tooltip("Row-major: full A/B/C, partial A/B/C, nearly exhausted A/B/C.")]
            public Sprite[] sprites=new Sprite[9];
        }
        public Entry[] entries=Array.Empty<Entry>();
        public static ResourceNodeArtSet Load()=>Resources.Load<ResourceNodeArtSet>("ResourceNodes");
        public Sprite Find(ResourceNodeKind kind,int variant,int stage)
        {
            if(stage<0)return null;
            foreach(var e in entries)if(e!=null&&e.kind==kind)
            {var index=Mathf.Clamp(stage,0,2)*3+Mathf.Clamp(variant,0,2);return e.sprites!=null&&index<e.sprites.Length?e.sprites[index]:null;}
            return null;
        }
        public bool Has(ResourceNodeKind kind)=>Find(kind,0,0)!=null;
        public static ResourceNodeKind Kind(TileClass tile,PatchType patch)
        {
            // Urban rubble keeps a salvage silhouette even when its current yield is ore.
            if(tile==TileClass.Rubble)return ResourceNodeKind.Rubble;
            switch(patch)
            {
                case PatchType.IronOre:return ResourceNodeKind.IronOre;
                case PatchType.CopperOre:return ResourceNodeKind.CopperOre;
                case PatchType.Coal:return ResourceNodeKind.Coal;
                case PatchType.Stone:return ResourceNodeKind.Stone;
                case PatchType.Crude:return ResourceNodeKind.Crude;
                case PatchType.Steel:case PatchType.Copper:return ResourceNodeKind.Rubble;
                default:return tile==TileClass.Deposit?ResourceNodeKind.Coal:ResourceNodeKind.Rubble;
            }
        }
        public static int Variant(int x,int y)=>unchecked((int)((uint)(x*73856093^y*19349663)%3));
        public static int Stage(double remaining,double initial)
        {
            if(remaining<=0||initial<=0)return -1;
            var fraction=remaining/initial;return fraction>2.0/3.0?0:fraction>1.0/3.0?1:2;
        }
    }
}
