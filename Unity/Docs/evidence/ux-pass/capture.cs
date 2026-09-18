UnityEngine.ScreenCapture.CaptureScreenshot("E:/Factorio2/Unity/Docs/evidence/ux-pass/after-hud-first.png");
var h=UnityEngine.Object.FindAnyObjectByType<Relight.Presentation.SimHost>();
return new { pos=h.Simulation.State.Engineer.Pos,steel=h.Simulation.State.Engineer.Inv[Relight.Sim.ItemId.Steel],copper=h.Simulation.State.Engineer.Inv[Relight.Sim.ItemId.Copper]};

