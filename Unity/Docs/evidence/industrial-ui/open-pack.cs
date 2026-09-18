var root=UnityEngine.Object.FindObjectsByType<UnityEngine.UIElements.UIDocument>().First(d=>d.visualTreeAsset!=null && d.visualTreeAsset.name=="GameUI").rootVisualElement;
var b=UnityEngine.UIElements.UQueryExtensions.Q<UnityEngine.UIElements.Button>(root,"open-inventory");using(var e=UnityEngine.UIElements.NavigationSubmitEvent.GetPooled()){e.target=b;b.SendEvent(e);}
UnityEngine.UIElements.UQueryExtensions.Q<UnityEngine.UIElements.Foldout>(root,"workshop-foldout").value=true;
System.Collections.IEnumerator Run(){yield return new UnityEngine.WaitForSecondsRealtime(.3f);UnityEngine.ScreenCapture.CaptureScreenshot("E:/Factorio2/Unity/Docs/evidence/industrial-ui/first-pack.png");}
UnityEngine.Object.FindAnyObjectByType<Relight.Presentation.SimHost>().StartCoroutine(Run());return "Backpack + workshop open";
