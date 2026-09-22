// REL-58 look: the Admin panel's own "clear enemies" (cancels the booked major assault and pushes the next one back).
var host = UnityEngine.Object.FindAnyObjectByType<Relight.Presentation.SimHost>();
host.Submit(new Relight.Sim.AdminCommand("clear-enemies"));
return "submitted";
