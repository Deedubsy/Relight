var path="Assets/Relight/Resources/ResourceNodes.asset";
if(!UnityEditor.AssetDatabase.IsValidFolder("Assets/Relight/Resources"))UnityEditor.AssetDatabase.CreateFolder("Assets/Relight","Resources");
var set=UnityEditor.AssetDatabase.LoadAssetAtPath<Relight.World.ResourceNodeArtSet>(path);
if(set==null){set=UnityEngine.ScriptableObject.CreateInstance<Relight.World.ResourceNodeArtSet>();UnityEditor.AssetDatabase.CreateAsset(set,path);}
var keys=new[]{"ironore","copperore","coal","stone","crude","rubble"};
var entries=new Relight.World.ResourceNodeArtSet.Entry[6];var stages=new[]{"full","partial","sparse"};
for(int k=0;k<6;k++){
 var assets=UnityEditor.AssetDatabase.LoadAllAssetsAtPath("Assets/Relight/Art/ResourceNodes/"+keys[k]+".png").OfType<UnityEngine.Sprite>().ToArray();
 if(assets.Length!=9)throw new System.Exception(keys[k]+" requires 9 sprites");
 var entry=new Relight.World.ResourceNodeArtSet.Entry{kind=(Relight.World.ResourceNodeKind)k};
 for(int r=0;r<3;r++)for(int c=0;c<3;c++)entry.sprites[r*3+c]=assets.First(s=>s.name==keys[k]+"_"+stages[r]+"_"+(char)('a'+c));
 entries[k]=entry;
}
set.entries=entries;UnityEditor.EditorUtility.SetDirty(set);UnityEditor.AssetDatabase.SaveAssets();
return "ResourceNodes.asset: 6 kinds, 54 sprite references";
