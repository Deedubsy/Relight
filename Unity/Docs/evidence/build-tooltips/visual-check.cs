var host=UnityEngine.Object.FindAnyObjectByType<Relight.Presentation.SimHost>();host.enabled=false;host.Paused=false;
var shell=UnityEngine.Object.FindAnyObjectByType<Relight.UI.UiShell>();var tips=UnityEngine.Object.FindAnyObjectByType<Relight.UI.TooltipController>();
var document=UnityEngine.Object.FindObjectsByType<UnityEngine.UIElements.UIDocument>().First(d=>d.visualTreeAsset!=null&&d.visualTreeAsset.name=="GameUI");var root=document.rootVisualElement;
UnityEngine.UIElements.VisualElement Q(string n)=>UnityEngine.UIElements.UQueryExtensions.Q<UnityEngine.UIElements.VisualElement>(root,n);
var game=UnityEditor.EditorWindow.GetWindow(typeof(UnityEditor.Editor).Assembly.GetType("UnityEditor.GameView"));
var property=game.GetType().GetProperty("selectedSizeIndex",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.Public|System.Reflection.BindingFlags.NonPublic);var original=(int)property.GetValue(game);
System.Collections.IEnumerator Run(){
 try{
 shell.Open("build-panel");using(var e=UnityEngine.UIElements.NavigationSubmitEvent.GetPooled()){e.target=Q("tab-production");Q("tab-production").SendEvent(e);}yield return null;
 foreach(var size in new[]{original,9}){
  property.SetValue(game,size);yield return new UnityEngine.WaitForSecondsRealtime(.5f);
  foreach(var kind in new[]{"foundry","assembler"}){
   var card=Q("card-"+kind);card.GetFirstAncestorOfType<UnityEngine.UIElements.ScrollView>()?.ScrollTo(card);yield return null;
   tips.HideNow();using(var e=UnityEngine.UIElements.PointerEnterEvent.GetPooled()){e.target=card;card.SendEvent(e);}yield return new UnityEngine.WaitForSecondsRealtime(.6f);
   var tip=Q("tooltip");var bounds=root.worldBound;
   var fits=tips.Visible&&tip.worldBound.xMin>=bounds.xMin&&tip.worldBound.xMax<=bounds.xMax+1&&tip.worldBound.yMin>=bounds.yMin&&tip.worldBound.yMax<=bounds.yMax+1;
   System.IO.File.AppendAllText("E:/Factorio2/Unity/Docs/evidence/build-tooltips/visual-checks.txt",(fits?"PASS ":"FAIL ")+kind+" visible and within "+UnityEngine.Screen.width+"x"+UnityEngine.Screen.height+"\n");
   UnityEngine.ScreenCapture.CaptureScreenshot("E:/Factorio2/Unity/Docs/evidence/build-tooltips/"+kind+"-"+UnityEngine.Screen.width+".png");yield return null;yield return null;
   tips.HideNow();
  }
 }
 }finally{property.SetValue(game,original);shell.CloseActive();}
 System.IO.File.AppendAllText("E:/Factorio2/Unity/Docs/evidence/build-tooltips/visual-checks.txt","DONE; original Game view size restored.\n");
}
host.StartCoroutine(Run());return "Capturing clean 1080p and 720p tooltip checks";
