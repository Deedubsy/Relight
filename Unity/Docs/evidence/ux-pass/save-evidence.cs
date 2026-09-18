var h=UnityEngine.Object.FindAnyObjectByType<Relight.Presentation.SimHost>();
System.IO.File.WriteAllBytes("E:/Factorio2/Unity/Docs/evidence/ux-pass/normal-opening.json",Relight.Sim.SaveSerializer.Write(h.Simulation.State,h.Simulation.Context,null,out _));
return "Saved normal-input opening evidence, without touching player saves";
