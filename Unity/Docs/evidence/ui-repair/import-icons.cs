UnityEditor.AssetDatabase.Refresh();
var folder="Assets/Relight/UI/Icons/Textures";
var paths=System.IO.Directory.GetFiles(folder,"*.png");
foreach(var path in paths){var importer=(UnityEditor.TextureImporter)UnityEditor.AssetImporter.GetAtPath(path.Replace('\\','/')); if(importer==null) continue; importer.textureType=UnityEditor.TextureImporterType.Sprite; importer.spriteImportMode=UnityEditor.SpriteImportMode.Single; importer.spritePixelsPerUnit=128; importer.mipmapEnabled=false; importer.alphaIsTransparency=true; importer.filterMode=UnityEngine.FilterMode.Bilinear; importer.textureCompression=UnityEditor.TextureImporterCompression.Uncompressed; importer.SaveAndReimport();}
const string destination="Assets/Relight/UI/Icons/Resources";
if(!UnityEditor.AssetDatabase.IsValidFolder(destination))UnityEditor.AssetDatabase.CreateFolder("Assets/Relight/UI/Icons","Resources");
var library=UnityEditor.AssetDatabase.LoadAssetAtPath<Relight.UI.ItemIconLibrary>(destination+"/RelightItemIcons.asset");
if(library==null){library=UnityEngine.ScriptableObject.CreateInstance<Relight.UI.ItemIconLibrary>();UnityEditor.AssetDatabase.CreateAsset(library,destination+"/RelightItemIcons.asset");}
var serialized=new UnityEditor.SerializedObject(library);var entries=serialized.FindProperty("entries");entries.arraySize=paths.Length;
for(int i=0;i<paths.Length;i++){var entry=entries.GetArrayElementAtIndex(i);entry.FindPropertyRelative("key").stringValue=System.IO.Path.GetFileNameWithoutExtension(paths[i]);entry.FindPropertyRelative("sprite").objectReferenceValue=UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.Sprite>(paths[i].Replace('\\','/'));}
serialized.ApplyModifiedPropertiesWithoutUndo();library.Invalidate();UnityEditor.EditorUtility.SetDirty(library);UnityEditor.AssetDatabase.SaveAssets();return new{icons=paths.Length};
