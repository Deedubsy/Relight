var host = UnityEngine.Object.FindAnyObjectByType<Relight.Presentation.SimHost>(); host.Paused = true; return "paused T=" + host.Simulation.State.T.ToString("0.0");
