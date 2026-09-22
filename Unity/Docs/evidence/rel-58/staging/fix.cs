var host = UnityEngine.Object.FindAnyObjectByType<Relight.Presentation.SimHost>();
host.Submit(new Relight.Sim.AdminCommand("clear-enemies"));
host.Submit(new Relight.Sim.AdminCommand("repair"));
return "queued";
