// REL-58 look: interface scale 100% on the running game's PanelSettings (restored to 100 before Play stops).
var set = new System.Collections.Generic.HashSet<UnityEngine.UIElements.PanelSettings>();
foreach (var doc in UnityEngine.Object.FindObjectsByType<UnityEngine.UIElements.UIDocument>()) if (doc.panelSettings != null) set.Add(doc.panelSettings);
var sb = new System.Text.StringBuilder();
foreach (var p in set) { sb.Append(p.name + " was " + p.scale + "; "); p.scale = 100 / 100f; }
return sb.ToString();
