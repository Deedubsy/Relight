using Relight.Sim;
using UnityEngine;
namespace Relight.World
{
    [AddComponentMenu("Relight/World/Scene Site")]
    public sealed class SceneSite : MonoBehaviour
    {
        public string id;
        public string siteName;
        public SiteKind kind;
        public Vector2Int size=Vector2Int.one;
        [Tooltip("Resource key, for example ironore, copperore, coal, stone or crude.")]
        public string item;
        [Min(0)] public int amount;
        public SiteRecord Record(){var r=SceneWorld.Rect(transform,size);return new SiteRecord(id,siteName,kind,r.x,r.y,r.width,r.height,item,amount);}
        private void OnDrawGizmosSelected()=>SceneWorld.DrawRect(SceneWorld.Rect(transform,size),kind==SiteKind.Resource?Color.yellow:Color.cyan);
    }
}
