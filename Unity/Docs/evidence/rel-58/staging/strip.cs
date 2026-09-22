var hud = UnityEngine.Object.FindAnyObjectByType<Relight.UI.HudController>();
var m = hud.Model; var s = "";
foreach (var p in m.GetType().GetProperties()) if (p.Name.Contains("Urgent") || p.Name.Contains("Strip") || p.Name.Contains("Power")) { try { s += p.Name + "=" + p.GetValue(m) + " | "; } catch {} }
foreach (var f in m.GetType().GetFields()) if (f.Name.Contains("Urgent") || f.Name.Contains("Strip") || f.Name.Contains("Power")) { try { s += f.Name + "=" + f.GetValue(m) + " | "; } catch {} }
return s;
