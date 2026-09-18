var host=UnityEngine.Object.FindAnyObjectByType<Relight.Presentation.SimHost>();var sim=host.Simulation;var st=sim.State;
var shell=UnityEngine.Object.FindAnyObjectByType<Relight.UI.UiShell>();var input=UnityEngine.Object.FindAnyObjectByType<Relight.Presentation.WorldInput>();
var doc=UnityEngine.Object.FindObjectsByType<UnityEngine.UIElements.UIDocument>().First(d=>d.visualTreeAsset!=null&&d.visualTreeAsset.name=="GameUI");var root=doc.rootVisualElement;
var kb=UnityEngine.InputSystem.Keyboard.current;UnityEngine.InputSystem.InputSystem.EnableDevice(kb);
var log="E:/Factorio2/Unity/Docs/evidence/flow-power/runtime.txt";
void Check(bool c,string s){System.IO.File.AppendAllText(log,(c?"PASS ":"FAIL ")+s+"\n");if(!c)throw new System.Exception(s);}
System.Collections.IEnumerator Run(){
// An isolated dead-end copper line fills at normal simulation speed.
st.MachineById(12).Y=366;st.MachineById(12).Inv[Relight.Sim.ItemId.Copper]=50;
for(int i=13;i<=16;i++)st.MachineById(i).Y=367;st.Rev++;
st.Ledger=Relight.Sim.Ledger.Open(st,sim.Context.Data);
yield return new UnityEngine.WaitForSecondsRealtime(4);
Check(Relight.Sim.FlowQueries.Count(st,16)==4,"Dead-end conveyor visibly queues four items per tile");
Check(UnityEngine.Object.FindAnyObjectByType<Relight.Presentation.PowerConnectionPresenter>().Drawn==4,"Four real pole / generator / extractor cables render");
Check(st.MachineById(8).Inv[Relight.Sim.ItemId.Steel]>0,"Live extractor delivers steel into destination chest");
UnityEngine.ScreenCapture.CaptureScreenshot("E:/Factorio2/Unity/Docs/evidence/flow-power/queued-items.png");yield return null;yield return null;
// Remove the extractor's receiver with the real command; its next output is visible in storage.
var result=sim.Apply(new Relight.Sim.RemoveMachineCommand(3));Check(result.Accepted,"Remove receiver belt: "+result.Problem);
yield return new UnityEngine.WaitForSecondsRealtime(2.4f);
Check(st.MachineById(2).Inv.Total==1,"Blocked extractor has one real item in its output inventory");
Relight.Presentation.WorldInput.OpenMachine?.Invoke(2);yield return new UnityEngine.WaitForSecondsRealtime(.3f);
var status=UnityEngine.UIElements.UQueryExtensions.Q<UnityEngine.UIElements.Label>(root,"machine-status");
Check(status.text.Contains("powered")&&status.text.Contains("Output: 1"),"Interaction panel shows power and the buffered output");
UnityEngine.ScreenCapture.CaptureScreenshot("E:/Factorio2/Unity/Docs/evidence/flow-power/extractor-output.png");yield return null;yield return null;
var take=sim.Apply(new Relight.Sim.MachineTransferCommand(2,Relight.Sim.ItemId.Steel,1,false));Check(take.Accepted,"Collect extracted output through ordinary hand-transfer command: "+take.Problem);
shell.CloseActive();yield return new UnityEngine.WaitForSecondsRealtime(.2f);
var preview=UnityEngine.Object.FindAnyObjectByType<Relight.Presentation.PlacementPreviewPresenter>();
input.enabled=false;preview.Show("pole",63,360,Relight.Sim.Dir.N);yield return new UnityEngine.WaitForSecondsRealtime(.2f);
Check(preview.Drawn>=4,"Pole preview includes machine and existing pole connection candidates");
UnityEngine.ScreenCapture.CaptureScreenshot("E:/Factorio2/Unity/Docs/evidence/flow-power/pole-preview.png");yield return null;yield return null;preview.Hide();input.enabled=true;
Check(Relight.Sim.Ledger.Conservation(st,sim.Context.Data).Problems.Count==0,"Prepared scenario conserves real stock through extraction, belt removal and collection");
host.Paused=true;System.IO.File.AppendAllText(log,"DONE\n");
}
host.StartCoroutine(Run());return "Runtime checks started";
