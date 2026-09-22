// REL-58 look (GP-UX-8): the Admin panel's own weapon grant puts a Rifle in the Backpack.
var host = UnityEngine.Object.FindAnyObjectByType<Relight.Presentation.SimHost>();
host.Submit(new Relight.Sim.AdminCommand("weapon", Key: "rifle"));
return "queued rifle";
