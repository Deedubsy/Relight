var host = UnityEngine.Object.FindAnyObjectByType<Relight.Presentation.SimHost>();
var st = host.Simulation.State;
return "paused=" + host.Paused + " enemies=" + st.Enemies.Actors.Count + " timeScale=" + UnityEngine.Time.timeScale + " ortho=" + UnityEngine.Camera.main.orthographicSize + " admin=" + st.Admin.LastAction + " saveRoot=" + UnityEditor.SessionState.GetString("Relight.SaveRootOverride", "(none)");
