var host=UnityEngine.Object.FindAnyObjectByType<Relight.Presentation.SimHost>();host.Paused=true;
var build=UnityEngine.Object.FindAnyObjectByType<Relight.UI.BuildPanelController>();
var shell=UnityEngine.Object.FindAnyObjectByType<Relight.UI.UiShell>();
var tips=UnityEngine.Object.FindAnyObjectByType<Relight.UI.TooltipController>();
var document=UnityEngine.Object.FindObjectsByType<UnityEngine.UIElements.UIDocument>().First(d=>d.visualTreeAsset!=null&&d.visualTreeAsset.name=="GameUI");var root=document.rootVisualElement;
UnityEngine.UIElements.VisualElement Q(string name)=>UnityEngine.UIElements.UQueryExtensions.Q<UnityEngine.UIElements.VisualElement>(root,name);
var log="E:/Factorio2/Unity/Docs/evidence/build-tooltips/live-checks.txt";System.IO.File.WriteAllText(log,"");
void Check(bool ok,string note){System.IO.File.AppendAllText(log,(ok?"PASS ":"FAIL ")+note+"\n");if(!ok)throw new System.Exception(note);}
System.Collections.IEnumerator Run(){
 shell.Open("build-panel");yield return null;build.Paint(true);yield return null;
 var cards=Relight.Sim.UI.BuildCatalogue.Cards(host.Simulation.Context,host.Simulation.State);
 foreach(var view in cards){
  var tab=Q("tab-"+view.Category.ToString().ToLowerInvariant());using(var e=UnityEngine.UIElements.NavigationSubmitEvent.GetPooled()){e.target=tab;tab.SendEvent(e);}yield return null;
  var card=Q("card-"+view.Kind);Check(card!=null,view.Kind+" card exists");
  var scroll=card.GetFirstAncestorOfType<UnityEngine.UIElements.ScrollView>();scroll?.ScrollTo(card);yield return null;
  tips.HideNow();using(var e=UnityEngine.UIElements.PointerEnterEvent.GetPooled()){e.target=card;card.SendEvent(e);}
  var deadline=UnityEngine.Time.realtimeSinceStartupAsDouble+2;while(!tips.Visible&&UnityEngine.Time.realtimeSinceStartupAsDouble<deadline)yield return null;
  var body=UnityEngine.UIElements.UQueryExtensions.Q<UnityEngine.UIElements.Label>(root,"tooltip-body").text;
  Check(tips.Visible&&tips.ShownFor==card&&view.Description.Length>0&&body.Contains(view.Description),view.Kind+" hover describes purpose");
  Check(view.OutputSummary.Length==0||body.Contains(view.OutputSummary),view.Kind+" hover shows current outputs");
  if(view.Kind=="foundry")Check(body.Contains("Steel plates")&&body.Contains("Copper plate")&&!body.Contains("Produces: Iron"),"Foundry lists smelted metals, not raw ore");
  if(view.Kind=="assembler")Check(body.Contains("Bullets")&&body.Contains("Shells")&&body.Contains("Wire"),"Assembler lists ammunition and components");
  if(view.Kind=="belt")Check(body.Contains("without electricity"),"Conveyor independence is explicit");
  if(view.Kind=="foundry"||view.Kind=="assembler"){
   yield return null;var tip=Q("tooltip");var bounds=root.worldBound;
   Check(tip.worldBound.xMin>=bounds.xMin&&tip.worldBound.xMax<=bounds.xMax+1&&tip.worldBound.yMin>=bounds.yMin&&tip.worldBound.yMax<=bounds.yMax+1,view.Kind+" tooltip stays inside screen");
   UnityEngine.ScreenCapture.CaptureScreenshot("E:/Factorio2/Unity/Docs/evidence/build-tooltips/"+view.Kind+".png");yield return null;yield return null;
  }
  using(var e=UnityEngine.UIElements.PointerLeaveEvent.GetPooled()){e.target=card;card.SendEvent(e);}yield return null;
  Check(!tips.Visible,view.Kind+" tooltip hides on leave");
 }
 shell.CloseActive();Check(!tips.Visible,"No tooltip after closing Build");
 System.IO.File.AppendAllText(log,"DONE: "+cards.Count+" build cards at "+UnityEngine.Screen.width+"x"+UnityEngine.Screen.height+". Synthetic UI Toolkit pointer events; no gameplay commands or save writes.\n");
}
System.Collections.IEnumerator Guard(){var run=Run();while(true){bool more;try{more=run.MoveNext();}catch(System.Exception ex){System.IO.File.AppendAllText(log,"ERROR "+ex+"\n");yield break;}if(!more)yield break;yield return run.Current;}}
host.StartCoroutine(Guard());return "Build hover validation started";

