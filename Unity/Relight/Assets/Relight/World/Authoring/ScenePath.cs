using System.Collections.Generic;
using UnityEngine;
namespace Relight.World
{
    public enum ScenePathKind { Road, Path, Drive }
    [AddComponentMenu("Relight/World/Scene Path")]
    public sealed class ScenePath : MonoBehaviour
    {
        public ScenePathKind kind;
        [Tooltip("Local tile coordinates, Y points south. Move the object to move the whole path.")]
        public List<Vector2Int> points=new List<Vector2Int>();
        public WorldGeometryAsset.Poly Record(){var p=SceneWorld.Rect(transform,Vector2Int.one).position;var r=new WorldGeometryAsset.Poly();foreach(var v in points)r.points.Add(v+p);return r;}
        private void OnDrawGizmosSelected(){Gizmos.color=Color.cyan;var p=Record().points;for(var i=1;i<p.Count;i++)Gizmos.DrawLine(new Vector3(p[i-1].x,-p[i-1].y,-8),new Vector3(p[i].x,-p[i].y,-8));}
    }
}
