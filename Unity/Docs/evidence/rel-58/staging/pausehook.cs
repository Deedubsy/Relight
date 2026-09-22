// REL-58 look (GP-UX-8): pause the sim the first frame a shot trace exists, so the tracer can be photographed.
var host = UnityEngine.Object.FindAnyObjectByType<Relight.Presentation.SimHost>();
UnityEditor.EditorApplication.CallbackFunction cb = null;
cb = () => {
    if (host == null) { UnityEditor.EditorApplication.update -= cb; return; }
    if (host.Simulation.State.Weapons.Shots.Count > 0) { host.Paused = true; UnityEditor.EditorApplication.update -= cb; }
};
UnityEditor.EditorApplication.update += cb;
return "hook set";
