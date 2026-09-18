var path="Assets/Relight/Art/ResourceNodes/crude.png";
UnityEditor.AssetDatabase.ImportAsset(path,UnityEditor.ImportAssetOptions.ForceSynchronousImport);
var importer=(UnityEditor.TextureImporter)UnityEditor.AssetImporter.GetAtPath(path);
importer.textureType=UnityEditor.TextureImporterType.Sprite;
importer.spriteImportMode=UnityEditor.SpriteImportMode.Multiple;
importer.spritePixelsPerUnit=418;
importer.alphaIsTransparency=true;importer.mipmapEnabled=false;
importer.filterMode=UnityEngine.FilterMode.Bilinear;
importer.textureCompression=UnityEditor.TextureImporterCompression.Uncompressed;
importer.npotScale=UnityEditor.TextureImporterNPOTScale.None;importer.maxTextureSize=2048;
var settings=new UnityEditor.TextureImporterSettings();importer.ReadTextureSettings(settings);
settings.spriteMeshType=UnityEngine.SpriteMeshType.FullRect;importer.SetTextureSettings(settings);
importer.SaveAndReimport();
var factory=new UnityEditor.U2D.Sprites.SpriteDataProviderFactories();factory.Init();
var provider=factory.GetSpriteEditorDataProviderFromObject(importer);
if(provider==null)throw new System.Exception("Sprite provider missing");
provider.InitSpriteEditorDataProvider();
var edit=provider.GetDataProvider<UnityEditor.U2D.Sprites.ISpriteFrameEditCapability>();
if(edit==null)throw new System.Exception("No sprite edit capability; aborted");
var capabilities=edit.GetEditCapability();
foreach(var required in new[]{UnityEditor.U2D.Sprites.EEditCapability.EditSpriteName,UnityEditor.U2D.Sprites.EEditCapability.EditSpriteRect,UnityEditor.U2D.Sprites.EEditCapability.EditPivot,UnityEditor.U2D.Sprites.EEditCapability.CreateAndDeleteSprite})
 if(!capabilities.HasCapability(required))throw new System.Exception("Unsupported capability "+required);
var existing=provider.GetSpriteRects();
var rects=new System.Collections.Generic.List<UnityEditor.SpriteRect>();
var pairs=new System.Collections.Generic.List<UnityEditor.SpriteNameFileIdPair>();
var cuts=new int[]{0,447,822,1254};
var stages=new[]{"full","partial","sparse"};
for(int row=0;row<3;row++)for(int col=0;col<3;col++){
 var name="crude_"+stages[row]+"_"+(char)('a'+col);
 var previous=System.Array.Find(existing,r=>r.name==name);
 var id=previous!=null?previous.spriteID:UnityEngine.GUID.Generate();
 rects.Add(new UnityEditor.SpriteRect{name=name,spriteID=id,rect=new UnityEngine.Rect(col*418,1254-cuts[row+1],418,cuts[row+1]-cuts[row]),pivot=new UnityEngine.Vector2(.5f,.5f),alignment=UnityEngine.SpriteAlignment.Center});
 pairs.Add(new UnityEditor.SpriteNameFileIdPair(name,id));
}
provider.SetSpriteRects(rects.ToArray());
var names=provider.GetDataProvider<UnityEditor.U2D.Sprites.ISpriteNameFileIdDataProvider>();
if(names==null)throw new System.Exception("Name/file-ID provider missing; aborted");
names.SetNameFileIdPairs(pairs);provider.Apply();importer.SaveAndReimport();
return path+" imported: "+UnityEditor.AssetDatabase.LoadAllAssetsAtPath(path).OfType<UnityEngine.Sprite>().Count()+" sprites";

