var host = UnityEngine.Object.FindAnyObjectByType<Relight.Presentation.SimHost>();
var ctx = host.Simulation.Context;
var sb = new System.Text.StringBuilder();
foreach (var m in ctx.Data.Machines) sb.Append(m.Key + "=" + m.PowerKw + "/" + m.Size + " ");
return sb.ToString();
