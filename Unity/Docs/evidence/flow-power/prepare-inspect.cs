var host=UnityEngine.Object.FindAnyObjectByType<Relight.Presentation.SimHost>();
var auto=UnityEngine.Object.FindAnyObjectByType<Relight.Presentation.AutosaveController>();
if(auto!=null){auto.GetType().GetField("saveOnQuit",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic).SetValue(auto,false);auto.ApplySettings(new Relight.Sim.AutosaveSettings(0,3,false,false));}
var sim=host.Simulation;host.Paused=true;
var pos=sim.State.Engineer.Pos;
var tiles=new System.Collections.Generic.List<object>();
for(var y=(int)pos.Y-12;y<pos.Y+12;y++)for(var x=(int)pos.X-12;x<pos.X+12;x++)if(Relight.Sim.Mining.TryTile(sim.Context,sim.State,x,y,out var item,out var n))tiles.Add(new{x,y,item=item.ToString(),n});
return new{position=new{pos.X,pos.Y},tiles=tiles.Take(12).ToArray(),machines=sim.State.Machines.Select(m=>new{m.Id,m.Kind,m.X,m.Y}).ToArray()};
