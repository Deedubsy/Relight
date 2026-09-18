var host=UnityEngine.Object.FindFirstObjectByType<Relight.Presentation.SimHost>();var city=UnityEngine.Object.FindFirstObjectByType<Relight.Presentation.CityPresenter>();var flags=System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic;
var sim=host.Simulation;var node=city.GetComponentsInChildren<UnityEngine.SpriteRenderer>().First(r=>r.name.Contains(" tile 96,369"));
var resources=(System.Collections.IEnumerable)city.GetType().GetField("_resources",flags).GetValue(city);var notes="";
foreach(var n in resources){var t=n.GetType();if((int)t.GetField("X").GetValue(n)==96&&(int)t.GetField("Y").GetValue(n)==369)notes=" initial="+t.GetField("Initial").GetValue(n)+" stage="+t.GetField("Stage").GetValue(n);}
return "host assigned="+(city.GetType().GetField("host",flags).GetValue(city)!=null)+" sprite="+node.sprite.name+" remaining="+Relight.Sim.Ground.UnitsAt(sim.Context,sim.State,96,369)+notes;
