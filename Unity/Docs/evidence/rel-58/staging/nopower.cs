// REL-58 look (GP-UX-7 b): empty every generator so the Home grid goes dark; Admin "fuel" puts it back.
var host = UnityEngine.Object.FindAnyObjectByType<Relight.Presentation.SimHost>();
var st = host.Simulation.State; var n = 0;
foreach (var m in st.Machines) if (m.Kind == "generator") { m.Inv[Relight.Sim.ItemId.Coal] = 0; n++; }
return n + " generators emptied T=" + st.T.ToString("0");
