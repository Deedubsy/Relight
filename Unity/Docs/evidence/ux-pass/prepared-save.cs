var h=UnityEngine.Object.FindAnyObjectByType<Relight.Presentation.SimHost>();var s=h.Simulation;
System.IO.File.WriteAllBytes("E:/Factorio2/Unity/Docs/evidence/ux-pass/prepared-latest.json",Relight.Sim.SaveSerializer.Write(s.State,s.Context,null,out _));
return "Prepared inspection state preserved";
