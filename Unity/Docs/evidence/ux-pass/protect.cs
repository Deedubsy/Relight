var a=UnityEngine.Object.FindAnyObjectByType<Relight.Presentation.AutosaveController>();
if(a!=null){a.GetType().GetField("saveOnQuit",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic).SetValue(a,false);a.ApplySettings(new Relight.Sim.AutosaveSettings(0,3,false,false));}
return "Runtime autosaves disabled for inspection";
