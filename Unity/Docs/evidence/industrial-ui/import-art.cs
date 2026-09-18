UnityEditor.AssetDatabase.Refresh();
var names=new[]{"panel-frame","item-slot","enamel-plate","button-face","selection-rim"};
var rects=new[]{new UnityEngine.Rect(40,50,1176,1158),new UnityEngine.Rect(85,96,1085,1062),new UnityEngine.Rect(29,162,2114,411),new UnityEngine.Rect(43,141,2086,458),new UnityEngine.Rect(26,26,1204,1210)};
var borders=new[]{new UnityEngine.Vector4(110,110,110,110),new UnityEngine.Vector4(95,95,95,95),new UnityEngine.Vector4(130,65,130,65),new UnityEngine.Vector4(95,65,95,65),UnityEngine.Vector4.zero};
var result=new System.Collections.Generic.List<object>();
for(int i=0;i<names.Length;i++){
 var path="Assets/Relight/UI/Art/Industrial/"+names[i]+".png";
 var importer=(UnityEditor.TextureImporter)UnityEditor.AssetImporter.GetAtPath(path);
 importer.textureType=UnityEditor.TextureImporterType.Sprite;importer.spriteImportMode=UnityEditor.SpriteImportMode.Multiple;
 importer.mipmapEnabled=false;importer.alphaIsTransparency=true;importer.textureCompression=UnityEditor.TextureImporterCompression.Uncompressed;importer.maxTextureSize=4096;importer.filterMode=UnityEngine.FilterMode.Bilinear;importer.spritePixelsPerUnit=100;importer.SaveAndReimport();
 var factory=new UnityEditor.U2D.Sprites.SpriteDataProviderFactories();factory.Init();
 var provider=factory.GetSpriteEditorDataProviderFromObject(importer);provider.InitSpriteEditorDataProvider();
 var edit=provider.GetDataProvider<UnityEditor.U2D.Sprites.ISpriteFrameEditCapability>();
 if(edit==null)throw new System.Exception("Sprite capability absent: "+path);
 var caps=edit.GetEditCapability();
 if(!caps.HasCapability(UnityEditor.U2D.Sprites.EEditCapability.EditSpriteRect)||!caps.HasCapability(UnityEditor.U2D.Sprites.EEditCapability.EditBorder)||!caps.HasCapability(UnityEditor.U2D.Sprites.EEditCapability.CreateAndDeleteSprite))throw new System.Exception("Unsupported sprite edit: "+path);
 var old=provider.GetSpriteRects();var id=old.Length>0?old[0].spriteID:UnityEngine.GUID.Generate();
 var sr=new UnityEditor.SpriteRect{name=names[i],rect=rects[i],border=borders[i],pivot=new UnityEngine.Vector2(.5f,.5f),alignment=UnityEngine.SpriteAlignment.Center,spriteID=id};
 provider.SetSpriteRects(new[]{sr});
 var pairs=provider.GetDataProvider<UnityEditor.U2D.Sprites.ISpriteNameFileIdDataProvider>();
 pairs.SetNameFileIdPairs(new[]{new UnityEditor.SpriteNameFileIdPair(sr.name,sr.spriteID)});
 provider.Apply();importer.SaveAndReimport();
 var sprite=UnityEditor.AssetDatabase.LoadAllAssetsAtPath(path).OfType<UnityEngine.Sprite>().First();
 UnityEditor.AssetDatabase.TryGetGUIDAndLocalFileIdentifier(sprite,out string guid,out long fileID);
 result.Add(new{name=names[i],url="project://database/"+path+"?fileID="+fileID+"&guid="+guid+"&type=3#"+names[i],rect=sprite.rect.ToString(),border=sprite.border.ToString()});
}
var json=Newtonsoft.Json.JsonConvert.SerializeObject(result,Newtonsoft.Json.Formatting.Indented);System.IO.File.WriteAllText("E:/Factorio2/Unity/Docs/evidence/industrial-ui/imported-art.json",json);return json;

