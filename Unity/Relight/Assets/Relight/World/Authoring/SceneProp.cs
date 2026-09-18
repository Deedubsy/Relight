using UnityEngine;
namespace Relight.World
{
    [AddComponentMenu("Relight/World/Scene Prop")]
    public sealed class SceneProp : MonoBehaviour
    {
        public string id;
        [Tooltip("Visual kind: container, furniture, tree, rock, fence, gate, tank, statue, crane or debris.")]
        public string kind="container";
        public Vector2Int size=Vector2Int.one;
        public bool blocksMovement;
        public bool clearable;
        public WorldGeometryAsset.PropRecord Record()=>new WorldGeometryAsset.PropRecord {id=id,kind=kind,rect=SceneWorld.Rect(transform,size),clearable=clearable};
        private void OnDrawGizmosSelected()=>SceneWorld.DrawRect(SceneWorld.Rect(transform,size),blocksMovement?Color.red:Color.yellow);
    }
}
