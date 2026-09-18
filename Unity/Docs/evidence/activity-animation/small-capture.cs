var host=UnityEngine.Object.FindAnyObjectByType<Relight.Presentation.SimHost>();
var game=UnityEditor.EditorWindow.GetWindow(typeof(UnityEditor.Editor).Assembly.GetType("UnityEditor.GameView"));
var size=game.GetType().GetProperty("selectedSizeIndex",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.Public|System.Reflection.BindingFlags.NonPublic);
var camera=UnityEngine.Camera.main;var sim=host.Simulation;
sim.State.MachineById(17).Y=354;sim.State.Rev++;
UnityEngine.InputSystem.InputSystem.QueueStateEvent(UnityEngine.InputSystem.Mouse.current,new UnityEngine.InputSystem.LowLevel.MouseState{position=new UnityEngine.Vector2(700,100)});
System.Collections.IEnumerator Run(){size.SetValue(game,9);yield return new UnityEngine.WaitForSecondsRealtime(.3f);
UnityEngine.ScreenCapture.CaptureScreenshot("E:/Factorio2/Unity/Docs/evidence/activity-animation/running-720.png");yield return null;yield return null;size.SetValue(game,3);}
host.StartCoroutine(Run());return "720p capture started";
