var host=UnityEngine.Object.FindAnyObjectByType<Relight.Presentation.SimHost>();
var shell=UnityEngine.Object.FindAnyObjectByType<Relight.UI.UiShell>();
var input=UnityEngine.Object.FindAnyObjectByType<Relight.Presentation.WorldInput>();
var inventory=UnityEngine.Object.FindAnyObjectByType<Relight.UI.InventoryPanelController>();
var flags=System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic;
var before=shell.Active;
input.GetType().GetMethod("Equip",flags).Invoke(input,new object[]{host.Simulation});
var afterE=shell.Active;
var tab=(string)shell.GetType().GetField("tabPanel",flags).GetValue(shell);
shell.Toggle(tab);
var afterTab=shell.Active;
var reg=UnityEditor.AssetDatabase.LoadAssetAtPath<Relight.Presentation.PrefabRegistry>("Assets/Relight/Prefabs/PrefabRegistry.asset");
var asset=UnityEditor.AssetDatabase.LoadAssetAtPath<Relight.Data.GameDataRegistry>("Assets/Relight/Data/GameDataRegistry.asset");
var result=new { before, tool=input.Tool, afterE, tabTarget=tab, afterTab, pairedMachine=inventory.Model.MachineId, machines=host.Simulation.Context.Data.Machines.Select(m=>new {kind=m.Key,prefab=UnityEditor.AssetDatabase.GetAssetPath(reg.Get(m.Key))}).ToArray(), itemIcons=asset.GetType().GetFields().Select(f=>f.Name).ToArray() };
shell.CloseActive();
var pause=UnityEngine.Object.FindAnyObjectByType<Relight.UI.PauseMenu.PauseMenuController>(); pause.GetType().GetMethod("Resume",flags).Invoke(pause,null);
UnityEngine.ScreenCapture.CaptureScreenshot("E:/Factorio2/Unity/Docs/evidence/phase-c/audit-opening-ui.png");
return result;

