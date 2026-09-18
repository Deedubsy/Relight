var w=UnityEngine.Object.FindFirstObjectByType<Relight.World.SceneWorld>();
var b=UnityEngine.Object.FindFirstObjectByType<Relight.Presentation.WorldBootstrap>();
var original=Relight.World.OpeningResourceLayout.Build(b.SourceGeometry,b.SourceSites,out var oldSites);
var g=w.Compile(out var sites);var solid=0;var tiles=0;var patches=0;var examples=new System.Text.StringBuilder();
for(var y=0;y<g.Height;y++)for(var x=0;x<g.Width;x++){var i=g.Index(x,y);if(g.Solid[i]!=original.Solid[i]){solid++;if(solid<15)examples.AppendLine(x+","+y+": "+original.Solid[i]+" -> "+g.Solid[i]);}if(g.Kind[i]!=original.Kind[i])tiles++;if(g.Patch[i]!=original.Patch[i])patches++;}
var msg="Buildings "+g.Buildings.Count+", props "+g.City.props.Count+", sites "+sites.Count+", solid differences "+solid+", terrain differences "+tiles+", patch differences "+patches+"; map "+g.MapId+"; preview error "+Relight.Editor.SceneWorldEditor.LastError+"\n"+examples;
UnityEngine.Object.DestroyImmediate(original);UnityEngine.Object.DestroyImmediate(g);
System.IO.File.WriteAllText("E:/Factorio2/Unity/Docs/evidence/scene-authoring/baseline-comparison.txt",msg);
return msg;
