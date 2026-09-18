using UnityEngine;
namespace Relight.World
{
    [AddComponentMenu("Relight/World/Blocks Movement")]
    [RequireComponent(typeof(BoxCollider2D))]
    public sealed class BlocksMovement : MonoBehaviour
    {
        [Tooltip("Tile collision uses this box's world bounds. Keep the object axis aligned; use multiple boxes for separate walls and leave doors clear.")]
        public BoxCollider2D shape;
        public RectInt Footprint()
        {
            var box=shape!=null?shape:GetComponent<BoxCollider2D>();
            if(box==null||!box.enabled)return default;
            var c=box.transform.TransformPoint(box.offset);
            var s=Vector2.Scale(box.size,box.transform.lossyScale);
            var x0=Mathf.FloorToInt(c.x-Mathf.Abs(s.x)*.5f+.001f);
            var y0=Mathf.FloorToInt(-c.y-Mathf.Abs(s.y)*.5f+.001f);
            var x1=Mathf.CeilToInt(c.x+Mathf.Abs(s.x)*.5f-.001f);
            var y1=Mathf.CeilToInt(-c.y+Mathf.Abs(s.y)*.5f-.001f);
            return new RectInt(x0,y0,x1-x0,y1-y0);
        }
        private void OnDrawGizmosSelected()=>SceneWorld.DrawRect(Footprint(),Color.red);
    }
}
