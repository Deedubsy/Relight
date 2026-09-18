var h=UnityEngine.Object.FindAnyObjectByType<Relight.Presentation.SimHost>();var r=UnityEngine.Object.FindAnyObjectByType<Relight.UI.InputRouter>();var input=UnityEngine.Object.FindAnyObjectByType<Relight.Presentation.WorldInput>();var key=UnityEngine.InputSystem.Keyboard.current;
var field=input.GetType().GetField("_move",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic);var move=(UnityEngine.InputSystem.InputAction)field.GetValue(input);
string log="E:/Factorio2/Unity/Docs/evidence/industrial-ui/held-input.txt";
System.Collections.IEnumerator Run(){
for(int i=0;i<12;i++){
UnityEngine.InputSystem.InputSystem.QueueStateEvent(key,new UnityEngine.InputSystem.LowLevel.KeyboardState(UnityEngine.InputSystem.Key.S));yield return new UnityEngine.WaitForSecondsRealtime(.08f);
System.IO.File.AppendAllText(log,"key="+key.sKey.isPressed+" input="+move.ReadValue<UnityEngine.Vector2>()+" router="+r.Actions.FindAction("World/Move").ReadValue<UnityEngine.Vector2>()+" pos="+h.Simulation.State.Engineer.Pos+" vel="+h.Simulation.State.Engineer.Vel+" paused="+h.Paused+System.Environment.NewLine);
}
UnityEngine.InputSystem.InputSystem.QueueStateEvent(key,new UnityEngine.InputSystem.LowLevel.KeyboardState());}
h.StartCoroutine(Run());return "Held input diagnostic running";
