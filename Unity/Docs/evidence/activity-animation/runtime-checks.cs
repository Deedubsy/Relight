var host=UnityEngine.Object.FindAnyObjectByType<Relight.Presentation.SimHost>();var sim=host.Simulation;var st=sim.State;
var presenter=UnityEngine.Object.FindAnyObjectByType<Relight.Presentation.MachinePresenter>();var power=UnityEngine.Object.FindAnyObjectByType<Relight.Presentation.PowerConnectionPresenter>();
var log="E:/Factorio2/Unity/Docs/evidence/activity-animation/runtime.txt";System.IO.File.WriteAllText(log,"");
void Check(bool ok,string text){System.IO.File.AppendAllText(log,(ok?"PASS ":"FAIL ")+text+"\n");if(!ok)throw new System.Exception(text);}
Relight.Presentation.MachineView V(int id)=>presenter.Views[id];
System.Collections.IEnumerator Run(){
yield return null;yield return null;
Check(power.Energized>0,"Live circuit has current highlights");
var current=power.GetComponentsInChildren<UnityEngine.LineRenderer>().First(l=>l.name=="Live current"&&l.enabled);var p=current.GetPosition(4);
var moving=V(21).ActivityMotion;var rotor=V(11).ActivityMotion;
yield return new UnityEngine.WaitForSecondsRealtime(.45f);
Check(UnityEngine.Vector3.Distance(current.GetPosition(4),p)>.005f,"Current highlight travels along its cable");
Check(V(21).ActivityMotion>moving,"Empty unpowered-independent belt tread moves");
Check(V(11).ActivityMotion>rotor&&V(11).ActivityState==Relight.Sim.MachineOperatingState.Running,"Loaded generator rotor animates while supplying power");
Check(V(17).ActivityState==Relight.Sim.MachineOperatingState.OutputFull,"Full extractor shows full state");
Check(V(18).ActivityState==Relight.Sim.MachineOperatingState.Unpowered,"Disconnected extractor shows no power");
Check(V(19).ActivityState==Relight.Sim.MachineOperatingState.OutOfFuel,"Empty generator shows off state");
Check(V(21).GetComponentsInChildren<UnityEngine.LineRenderer>().Where(l=>l.name=="Activity detail"&&l.enabled).All(l=>l.sortingOrder>V(21).GetComponentInChildren<UnityEngine.SpriteRenderer>().sortingOrder),"Conveyor tread is drawn above the deck");
UnityEngine.ScreenCapture.CaptureScreenshot("E:/Factorio2/Unity/Docs/evidence/activity-animation/running-1080.png");yield return null;yield return null;
host.Paused=true;yield return null;yield return null;
var clock=power.AnimationTime;var frozen=V(21).ActivityMotion;var serialized=Relight.Sim.CanonicalJsonWriter.Write(st);
yield return new UnityEngine.WaitForSecondsRealtime(.3f);
Check(power.AnimationTime==clock&&V(21).ActivityMotion==frozen,"Pause freezes cable and machine motion");
Check(Relight.Sim.CanonicalJsonWriter.Write(st)==serialized,"Rendering while paused leaves serialized gameplay unchanged");
foreach(var m in st.Machines)if(Relight.Sim.PowerGrid.IsSource(sim.Context.Data,m)){m.Inv[Relight.Sim.ItemId.Coal]=0;m.Inv[Relight.Sim.ItemId.Fuel]=0;}
yield return null;yield return null;
Check(power.Energized==0&&!power.GetComponentsInChildren<UnityEngine.LineRenderer>().Any(l=>l.name=="Live current"&&l.enabled),"Power loss removes every current highlight even while paused");
Check(V(2).ActivityState==Relight.Sim.MachineOperatingState.Unpowered&&V(11).ActivityState==Relight.Sim.MachineOperatingState.OutOfFuel,"Machines and generator change to outage/off indicators");
host.Paused=false;yield return new UnityEngine.WaitForSecondsRealtime(.3f);
Check(V(21).ActivityMotion>frozen,"Conveyor continues during an electrical outage");
UnityEngine.ScreenCapture.CaptureScreenshot("E:/Factorio2/Unity/Docs/evidence/activity-animation/outage-1080.png");yield return null;yield return null;
host.Paused=true;st.Flow.Of(21).Items.Add(new Relight.Sim.BeltItem{K=(int)Relight.Sim.ItemId.Steel,P=.875});yield return null;yield return null;
Check(V(21).ActivityState==Relight.Sim.MachineOperatingState.OutputFull,"Blocked belt shows amber jam state");
var jammed=V(21).ActivityMotion;host.Paused=false;yield return new UnityEngine.WaitForSecondsRealtime(.4f);
Check(V(21).ActivityMotion==jammed,"Jammed belt tread stops");
host.Paused=true;st.Flow.Of(21).Items.Clear();yield return null;yield return null;host.Paused=false;yield return new UnityEngine.WaitForSecondsRealtime(.3f);
Check(V(21).ActivityMotion>jammed,"Clearing the blockage restarts tread motion");
host.Paused=true;st.MachineById(11).Inv[Relight.Sim.ItemId.Coal]=10;yield return null;yield return null;
Check(power.Energized>0&&V(11).ActivityState==Relight.Sim.MachineOperatingState.Running,"Refuelling immediately restores live lines and generator state");
st.Production.Of(2).Stall=(int)Relight.Sim.MachineOperatingState.NoInput;yield return null;yield return null;
Check(V(2).ActivityState==Relight.Sim.MachineOperatingState.NoInput,"Waiting for input is distinct from no power and output full");
var gen=st.MachineById(20);gen.Inv[Relight.Sim.ItemId.Coal]=5;gen.X=800;gen.Y=550;st.Rev++;yield return null;yield return null;
Check(Relight.Sim.ProductionQueries.OperatingState(sim.Context,st,20)==Relight.Sim.MachineOperatingState.Idle,"Fuelled disconnected generator is standby, not running");
System.IO.File.AppendAllText(log,"DONE\n");host.Paused=false;
}
host.StartCoroutine(Run());return "Live activity checks started";
