UnityEngine.InputSystem.InputSystem.QueueStateEvent(UnityEngine.InputSystem.Keyboard.current,new UnityEngine.InputSystem.LowLevel.KeyboardState());
var host=UnityEngine.Object.FindAnyObjectByType<Relight.Presentation.SimHost>(); var ctx=host.Simulation.Context;
var bytes=System.IO.File.ReadAllBytes("E:/Factorio2/Unity/Docs/evidence/phase-c/integrated-run/slot-phasec-int.json");
var load=Relight.Sim.SaveSerializer.Read(bytes,ctx);
Relight.Sim.SaveHeader header;
var written=Relight.Sim.SaveSerializer.Write(host.Simulation.State,ctx,null,out header);
UnityEngine.ScreenCapture.CaptureScreenshot("E:/Factorio2/Unity/Docs/evidence/correction-review/build.png");
return new {bOpened=UnityEngine.Object.FindAnyObjectByType<Relight.UI.UiShell>().Active, liveRegion=Relight.Sim.SaveRegion.Of(ctx), writtenRegion=header.Region, oldSaveAccepted=load.Ok, oldEngineer=load.State==null?new double[0]:new[]{load.State.Engineer.Pos.X,load.State.Engineer.Pos.Y}, expectedEngineer=new[]{73.5,355.5}, loadReason=load.Reason};

