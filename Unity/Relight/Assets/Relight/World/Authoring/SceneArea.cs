using UnityEngine;
namespace Relight.World
{
    [AddComponentMenu("Relight/World/Scene Paved Area")]
    public sealed class SceneArea : MonoBehaviour
    {
        public string areaName;
        public bool serviceArea;
        public Vector2Int size=new Vector2Int(8,8);
        private void OnDrawGizmosSelected()=>SceneWorld.DrawRect(SceneWorld.Rect(transform,size),Color.cyan);
    }
}
