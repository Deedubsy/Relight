var a=UnityEngine.Object.FindAnyObjectByType<Relight.Presentation.AutosaveController>();
if(a!=null){a.GetType().GetField("saveOnQuit",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic).SetValue(a,false);a.ApplySettings(new Relight.Sim.AutosaveSettings(0,3,false,false));}
var shell=UnityEngine.Object.FindAnyObjectByType<Relight.UI.UiShell>();
shell.CloseActive();shell.ToggleBackpack();
var lib=UnityEngine.Resources.Load<Relight.UI.ItemIconLibrary>("RelightItemIcons");
UnityEngine.Sprite sprite;var loaded=lib!=null&&lib.TryGet("steel",out sprite);
UnityEngine.ScreenCapture.CaptureScreenshot("E:/Factorio2/Unity/Docs/evidence/ui-repair/inventory-1080.png");
return new{iconsLoaded=loaded,width=UnityEngine.Screen.width,height=UnityEngine.Screen.height,active=shell.Active};
