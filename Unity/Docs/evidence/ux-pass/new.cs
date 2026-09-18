var d=UnityEngine.Object.FindObjectsByType<UnityEngine.UIElements.UIDocument>(UnityEngine.FindObjectsSortMode.None).First(d=>d.visualTreeAsset!=null && d.visualTreeAsset.name=="TitleScreen");
var b=UnityEngine.UIElements.UQueryExtensions.Q<UnityEngine.UIElements.Button>(d.rootVisualElement,"menu-new");
using(var e=UnityEngine.UIElements.NavigationSubmitEvent.GetPooled()){e.target=b;b.SendEvent(e);}
return "New game screen selected through UI submit";
