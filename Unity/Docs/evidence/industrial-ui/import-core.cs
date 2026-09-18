UnityEditor.AssetDatabase.Refresh();
var library=UnityEditor.AssetDatabase.LoadAssetAtPath<Relight.UI.ItemIconLibrary>("Assets/Relight/UI/Icons/Resources/RelightItemIcons.asset");
var so=new UnityEditor.SerializedObject(library);var entries=so.FindProperty("entries");
var names=new[]{"core1"};
foreach(var name in names){
 var path="Assets/Relight/UI/Art/Industrial/Items/"+name+".png";
 var ti=(UnityEditor.TextureImporter)UnityEditor.AssetImporter.GetAtPath(path);ti.textureType=UnityEditor.TextureImporterType.Sprite;ti.spriteImportMode=UnityEditor.SpriteImportMode.Single;ti.mipmapEnabled=false;ti.alphaIsTransparency=true;ti.filterMode=UnityEngine.FilterMode.Bilinear;ti.maxTextureSize=512;ti.textureCompression=UnityEditor.TextureImporterCompression.Uncompressed;ti.spritePixelsPerUnit=100;ti.SaveAndReimport();
 var sprite=UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.Sprite>(path);var key=name=="bullet"?"magazine":name;
 int index=-1;for(int i=0;i<entries.arraySize;i++)if(entries.GetArrayElementAtIndex(i).FindPropertyRelative("key").stringValue==key){index=i;break;}
 if(index<0){index=entries.arraySize;entries.arraySize++;}
 var entry=entries.GetArrayElementAtIndex(index);entry.FindPropertyRelative("key").stringValue=key;entry.FindPropertyRelative("sprite").objectReferenceValue=sprite;
}
so.ApplyModifiedPropertiesWithoutUndo();library.Invalidate();UnityEditor.EditorUtility.SetDirty(library);UnityEditor.AssetDatabase.SaveAssets();UnityEngine.Object.FindAnyObjectByType<Relight.UI.WorkshopPanelController>()?.Bind();return "Painted core icon bound";
