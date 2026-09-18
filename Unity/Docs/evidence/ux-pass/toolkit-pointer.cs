var host=UnityEngine.Object.FindAnyObjectByType<Relight.Presentation.SimHost>();
var doc=UnityEngine.Object.FindObjectsByType<UnityEngine.UIElements.UIDocument>().First(d=>d.visualTreeAsset!=null && d.visualTreeAsset.name=="GameUI");
var b=UnityEngine.UIElements.UQueryExtensions.Q<UnityEngine.UIElements.Button>(doc.rootVisualElement,"open-inventory");
System.Collections.IEnumerator Run(){
var p=b.worldBound.center;
using(var e=UnityEngine.UIElements.PointerDownEvent.GetPooled(new UnityEngine.Event{type=UnityEngine.EventType.MouseDown,mousePosition=p,button=0})){e.target=b;b.SendEvent(e);}
yield return null;
using(var e=UnityEngine.UIElements.PointerUpEvent.GetPooled(new UnityEngine.Event{type=UnityEngine.EventType.MouseUp,mousePosition=p,button=0})){e.target=b;b.SendEvent(e);}
yield return null;yield return null;
System.IO.File.WriteAllText("E:/Factorio2/Unity/Docs/evidence/ux-pass/toolkit-pointer.txt",UnityEngine.Object.FindAnyObjectByType<Relight.UI.UiShell>().Active??"closed");
}
host.StartCoroutine(Run());return "Toolkit pointer gesture queued";
