const string path="Assets/Relight/World/Manual/Relight Terrain.prefab";
var palette=UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.GameObject>(path);
if(palette==null)palette=UnityEditor.Tilemaps.GridPaletteUtility.CreateNewPalette("Assets/Relight/World/Manual","Relight Terrain",UnityEngine.GridLayout.CellLayout.Rectangle,UnityEditor.GridPalette.CellSizing.Automatic,new UnityEngine.Vector3(1,1,0),UnityEngine.GridLayout.CellSwizzle.XYZ,UnityEngine.TransparencySortMode.Default,new UnityEngine.Vector3(0,0,1));
var contents=UnityEditor.PrefabUtility.LoadPrefabContents(path);
try{var map=contents.GetComponentInChildren<UnityEngine.Tilemaps.Tilemap>();var data=UnityEngine.Object.FindFirstObjectByType<Relight.World.SceneWorld>().palette;
map.ClearAllTiles();for(var i=0;i<7;i++)map.SetTile(new UnityEngine.Vector3Int(i,0,0),data.For((Relight.Sim.TileClass)i));
UnityEditor.PrefabUtility.SaveAsPrefabAsset(contents,path);}
finally{UnityEditor.PrefabUtility.UnloadPrefabContents(contents);}
UnityEditor.AssetDatabase.SaveAssets();
return "Created Relight Terrain palette with 7 terrain tiles.";
