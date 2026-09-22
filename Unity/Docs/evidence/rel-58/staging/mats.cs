var host = UnityEngine.Object.FindAnyObjectByType<Relight.Presentation.SimHost>();
host.Submit(new Relight.Sim.AdminCommand("materials"));
return "queued";
