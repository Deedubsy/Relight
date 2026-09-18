var host=UnityEngine.Object.FindAnyObjectByType<Relight.Presentation.SimHost>();host.Paused=false;host.enabled=false;
var sim=host.Simulation;var st=sim.State;var ctx=sim.Context;
foreach(var doc in UnityEngine.Object.FindObjectsByType<UnityEngine.UIElements.UIDocument>(UnityEngine.FindObjectsSortMode.None)){var pause=UnityEngine.UIElements.UQueryExtensions.Q<UnityEngine.UIElements.VisualElement>(doc.rootVisualElement,"pause-menu");if(pause!=null)pause.style.display=UnityEngine.UIElements.DisplayStyle.None;}
var rig=UnityEngine.Object.FindAnyObjectByType<Relight.Presentation.CameraRig>();rig.enabled=false;
var cam=rig.GetComponent<UnityEngine.Camera>();cam.orthographicSize=18;cam.transform.position=new UnityEngine.Vector3(85,-360,-10);
var shell=UnityEngine.Object.FindAnyObjectByType<Relight.UI.UiShell>();shell.CloseActive();
UnityEngine.ScreenCapture.CaptureScreenshot("E:/Factorio2/Unity/Docs/evidence/ore-opening/deposits.png");
return "Both ore strips and the clear processing yard framed; paused-menu overlay hidden for screenshot; save writes remain disabled.";
