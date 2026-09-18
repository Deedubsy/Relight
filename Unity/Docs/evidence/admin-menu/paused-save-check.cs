var host=UnityEngine.Object.FindAnyObjectByType<Relight.Presentation.SimHost>();
var session=UnityEngine.Object.FindAnyObjectByType<Relight.UI.FrontEnd.FrontEndBootstrap>();
var doc=UnityEngine.Object.FindObjectsByType<UnityEngine.UIElements.UIDocument>().First(d=>d.visualTreeAsset!=null&&d.visualTreeAsset.name=="GameUI");var root=doc.rootVisualElement;
T Q<T>(string n)where T:UnityEngine.UIElements.VisualElement=>UnityEngine.UIElements.UQueryExtensions.Q<T>(root,n);
var log="E:/Factorio2/Unity/Docs/evidence/admin-menu/paused-save.txt";System.IO.File.WriteAllText(log,"");
void Check(bool ok,string message){System.IO.File.AppendAllText(log,(ok?"PASS ":"FAIL ")+message+"\n");if(!ok)throw new System.Exception(message);}
void Click(string name){var button=Q<UnityEngine.UIElements.Button>(name);using(var e=UnityEngine.UIElements.NavigationSubmitEvent.GetPooled()){e.target=button;button.SendEvent(e);}}
Check(Q<UnityEngine.UIElements.DropdownField>("admin-item").value.ToLowerInvariant().Contains("steel"),"Steel is the default resource");
Click("admin-tab-world");Click("admin-pause");Check(host.Paused,"Simulation paused through admin button");
session.Dirty.Saved(host.TotalTicks,"test-only-no-disk-write");var ticks=host.TotalTicks;
Check(!session.Dirty.IsDirty(ticks),"Saved baseline is clean at the paused tick");
Click("admin-tab-supplies");Q<UnityEngine.UIElements.IntegerField>("admin-amount").value=5;Click("admin-give");
Check(host.TotalTicks==ticks&&session.Dirty.IsDirty(ticks),"Paused admin grant is unsaved progress without a simulation tick");
Check(Relight.Sim.Ledger.Conservation(host.Simulation.State,host.Simulation.Context.Data).Problems.Count==0,"Grant preserves inventory accounting");
System.IO.File.AppendAllText(log,"DONE\n");return System.IO.File.ReadAllText(log);
