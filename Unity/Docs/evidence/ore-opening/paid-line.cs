var source=UnityEditor.AssetDatabase.LoadAssetAtPath<Relight.World.WorldGeometryAsset>("Assets/Relight/World/Generated/Full/FullGeometry.asset");
var siteAsset=UnityEditor.AssetDatabase.LoadAssetAtPath<Relight.World.HomeSitesAsset>("Assets/Relight/World/Generated/Full/FullSites.asset");
var original=Relight.World.SiteBridge.ToSim(Relight.World.HomeSites.Resolve(siteAsset,null),source.RegionId,source.OriginX,source.OriginY);
var revised=Relight.World.OpeningResourceLayout.Build(source,original,out var sites);
var data=UnityEditor.AssetDatabase.LoadAssetAtPath<Relight.Data.GameDataRegistry>("Assets/Relight/Data/GameDataRegistry.asset").Build();
var ctx=new Relight.Sim.SimContext(data,Relight.World.ImportedGeometry.Build(revised),sites:sites,mapId:source.MapId);
var sim=Relight.Sim.Simulation.NewGame(ctx,1);var st=sim.State;st.OpeningResourceVersion=1;
var log=new System.Text.StringBuilder();
void Check(bool ok,string note){log.AppendLine((ok?"PASS ":"FAIL ")+note);if(!ok)throw new System.Exception(note);}
void Apply(Relight.Sim.Command c){var r=sim.Apply(c);Check(r.Accepted,c.GetType().Name+": "+r.Problem);}
void Run(int ticks){for(var i=0;i<ticks;i++)sim.Tick();}
void Mine(int x,int y,int count,Relight.Sim.ItemId item){st.Engineer.Pos=new Relight.Sim.Vec2(x-.5,y+.5);var before=st.Engineer.Inv[item];Apply(new Relight.Sim.MineCommand(x,y));Run(count*40);Apply(new Relight.Sim.StopMiningCommand());Check(st.Engineer.Inv[item]==before+count,"hand-mined "+count+" "+item);}
void Craft(string key,int count){st.Engineer.Pos=ctx.Geometry.Spawn;Apply(new Relight.Sim.HandCraftCommand(count,key));Run(count*80);Check(!st.Hand.Crafting && st.Hand.Crafts==0,"completed "+key);}
Relight.Sim.Machine Place(string kind,int x,int y,Relight.Sim.Dir dir=Relight.Sim.Dir.S){st.Engineer.Pos=new Relight.Sim.Vec2(x-.5,y+.5);Apply(new Relight.Sim.PlaceMachineCommand(kind,x,y,dir));return st.Machines[st.Machines.Count-1];}
Check(source.TileAt(59,352)==Relight.Sim.TileClass.Patch,"original layout untouched");
Check(ctx.Geometry.TileAt(59,352)==Relight.Sim.TileClass.Ground && ctx.Geometry.TileAt(59,359)==Relight.Sim.TileClass.Ground && ctx.Geometry.TileAt(76,359)==Relight.Sim.TileClass.Ground,"old patches clear");
foreach(var s in sites.OfKind(Relight.Sim.SiteKind.Resource))if(s.Id.StartsWith("opening-"))
{
 double total=0;for(var y=s.Y;y<s.Y+s.H;y++)for(var x=s.X;x<s.X+s.W;x++)total+=Relight.Sim.Ground.UnitsAt(ctx,st,x,y);
 Check(total==s.Amount,"resource total "+s.Name+"="+total);
}
Mine(84,352,45,Relight.Sim.ItemId.IronOre);Mine(96,352,12,Relight.Sim.ItemId.CopperOre);Mine(96,369,6,Relight.Sim.ItemId.Coal);
Craft("hand-steel",45);Craft("hand-copper",12);
Check(Relight.Sim.OpeningQueries.Objective(ctx,st).Title.Contains("Generator"),"smelting advances opening");
var gen=Place("generator",78,354);Apply(new Relight.Sim.MachineTransferCommand(gen.Id,Relight.Sim.ItemId.Coal,5,true));
Place("pole",81,357);Place("pole",88,360);
var dig=Place("excavator",84,356);Place("belt",85,359);
var foundry=Place("foundry",84,360);Place("belt",85,363);
var chest=Place("chest",84,364);
st.Engineer.Pos=new Relight.Sim.Vec2(82,363);Run(1200);
Check(chest.Inv[Relight.Sim.ItemId.Steel]>=25,"automatic line delivers >=25 plates/min: "+chest.Inv[Relight.Sim.ItemId.Steel]);
Check(Relight.Sim.PowerQueries.Supplied(ctx,st,dig.Id)&&Relight.Sim.PowerQueries.Supplied(ctx,st,foundry.Id),"one Generator powers first ore line");
Check(Relight.Sim.Ledger.Conservation(st,data).Ok,"all mining, crafting, construction and flow conserved");
// Validate physical space for the parallel Copper line without granting or spending materials.
foreach(var p in new[]{("excavator",96,356),("belt",97,359),("foundry",96,360),("belt",97,363),("chest",96,364),("generator",90,366),("assembler",84,368)})
 {st.Engineer.Pos=new Relight.Sim.Vec2(p.Item2-.5,p.Item3+.5);var reason=Relight.Sim.Placement.GeometryProblem(ctx,st,p.Item1,p.Item2,p.Item3,Relight.Sim.Dir.S);Check(string.IsNullOrEmpty(reason),"clear footprint "+p.Item1+" "+p.Item2+","+p.Item3+" "+reason);}
Check(st.Director.Minor==null,"no incidental minor raid before tutorial defence");
var loaded=Relight.Sim.SaveSerializer.Read(Relight.Sim.SaveSerializer.Write(st,data),ctx);Check(loaded.Ok,"new-layout save round-trip "+loaded.Reason);
Check(loaded.State.OpeningResourceVersion==1,"layout version survives load");
log.AppendLine("DONE. Engineer positions were set for bounded reach checks; all resources were mined, smelted and paid, with no grants. Native walking and campaign pacing were not simulated.");
System.IO.File.WriteAllText("E:/Factorio2/Unity/Docs/evidence/ore-opening/paid-line.txt",log.ToString());
UnityEngine.Object.DestroyImmediate(revised);
return log.ToString();
