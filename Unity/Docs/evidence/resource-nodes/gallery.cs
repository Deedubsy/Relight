var art=Relight.World.ResourceNodeArtSet.Load();var root=new UnityEngine.GameObject("Temporary resource sprite QA");
for(int k=0;k<6;k++){
 float bx=2000+(k%3)*4.8f,by=-(k/3)*5.2f;
 var label=new UnityEngine.GameObject(((Relight.World.ResourceNodeKind)k).ToString());label.transform.SetParent(root.transform);label.transform.position=new UnityEngine.Vector3(bx,by+1,-1);
 var text=label.AddComponent<UnityEngine.TextMesh>();text.text=label.name;text.font=UnityEngine.Resources.GetBuiltinResource<UnityEngine.Font>("LegacyRuntime.ttf");text.fontSize=36;text.characterSize=.15f;text.anchor=UnityEngine.TextAnchor.MiddleLeft;text.GetComponent<UnityEngine.MeshRenderer>().sharedMaterial=text.font.material;
 for(int s=0;s<3;s++)for(int v=0;v<3;v++){
  var sprite=art.Find((Relight.World.ResourceNodeKind)k,v,s);var go=new UnityEngine.GameObject(sprite.name);go.transform.SetParent(root.transform);go.transform.position=new UnityEngine.Vector3(bx+v*1.3f,by-s*1.3f,-1);go.transform.localScale=UnityEngine.Vector3.one/sprite.bounds.size.x;go.AddComponent<UnityEngine.SpriteRenderer>().sprite=sprite;
 }
}
var sv=UnityEditor.SceneView.lastActiveSceneView;sv.in2DMode=true;sv.LookAtDirect(new UnityEngine.Vector3(2006,-3.3f,0),UnityEngine.Quaternion.identity,5.4f);sv.Repaint();
return "54 native Unity sprites displayed for slice/transparency QA";
