var h = UnityEngine.Object.FindAnyObjectByType<Relight.Presentation.SimHost>();
if (h == null) return "No host";
h.Paused = true;
var a = UnityEngine.Object.FindAnyObjectByType<Relight.Presentation.AutosaveController>();
if (a != null) { a.GetType().GetField("saveOnQuit", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic).SetValue(a, false); a.ApplySettings(new Relight.Sim.AutosaveSettings(0, 3, false, false)); }
UnityEngine.ScreenCapture.CaptureScreenshot("E:/Factorio2/Unity/Docs/evidence/phase-c/audit-opening-full.png");
var shell = UnityEngine.Object.FindAnyObjectByType<Relight.UI.UiShell>();
var bootstrap = UnityEngine.Object.FindAnyObjectByType<Relight.Presentation.WorldBootstrap>();
return new { map = bootstrap.MapId, region=bootstrap.RegionId, t=h.Simulation.State.T, panel=shell.Active, panels=shell.PanelIds, bar=Relight.Sim.WeaponQueries.Bar(h.Simulation.State), components=UnityEngine.Object.FindObjectsByType<UnityEngine.MonoBehaviour>(UnityEngine.FindObjectsSortMode.None).Where(x=> x != null && x.GetType().Namespace != null && x.GetType().Namespace.StartsWith("Relight")).Select(x=>x.GetType().FullName).ToArray() };
