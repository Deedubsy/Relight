// REL-58 look: undo blind6's freeze.
var host = UnityEngine.Object.FindAnyObjectByType<Relight.Presentation.SimHost>();
typeof(Relight.Presentation.SimHost).GetField("_paused", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic).SetValue(host, false);
return "running paused=" + host.Paused;
