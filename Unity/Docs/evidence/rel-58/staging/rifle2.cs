// REL-58 look (GP-UX-8): put the Rifle on action-bar slot 9 (staged directly) and press it through the method a
// digit key calls (WorldInput.SelectBarSlot), which equips it and puts it in hand.
var host = UnityEngine.Object.FindAnyObjectByType<Relight.Presentation.SimHost>();
var sim = host.Simulation; var st = sim.State;
string key = null; foreach (var w in st.Weapons.Owned) if (w.Kind == "rifle") key = w.Id;
if (key == null) return "no rifle owned";
Relight.Sim.WeaponRules.NormaliseBar(st); st.Weapons.Bar[8] = key;
var wi = UnityEngine.Object.FindAnyObjectByType<Relight.Presentation.WorldInput>();
wi.SelectBarSlot(8);
return "rifle " + key + " tool=" + wi.Tool + " equipped=" + Relight.Sim.WeaponQueries.Equipped(st, sim.Context.Data).Item;
