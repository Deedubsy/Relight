var host = UnityEngine.Object.FindAnyObjectByType<Relight.Presentation.SimHost>(); host.Paused = false; return "running T=" + host.Simulation.State.T.ToString("0.0");
