var w=UnityEngine.Object.FindFirstObjectByType<Relight.World.SceneWorld>();var g=w.Compile(out _);
var bootstrap=UnityEngine.Object.FindFirstObjectByType<Relight.Presentation.WorldBootstrap>();var original=Relight.World.OpeningResourceLayout.Build(bootstrap.SourceGeometry,bootstrap.SourceSites,out _);var msg=new System.Text.StringBuilder();
foreach(var b in original.Buildings){var count=0;for(var y=b.rect.y;y<b.rect.yMax;y++)for(var x=b.rect.x;x<b.rect.xMax;x++){var i=g.Index(x,y);if(original.Solid[i]!=g.Solid[i])count++;}if(count>0)msg.AppendLine(b.id+" "+b.kind+" enterable="+b.enterable+" "+b.rect+" differences="+count);}
UnityEngine.Object.DestroyImmediate(g);UnityEngine.Object.DestroyImmediate(original);return msg.ToString();
