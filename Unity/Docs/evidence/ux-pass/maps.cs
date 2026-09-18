var r=UnityEngine.Object.FindAnyObjectByType<Relight.UI.InputRouter>();
return string.Join("\n",r.Actions.actionMaps.Select(m=>m.name+" "+m.enabled+": "+string.Join(",",m.actions.Select(a=>a.name+"="+a.enabled))))+"\nModules: "+string.Join(",",UnityEngine.Object.FindObjectsByType<UnityEngine.EventSystems.BaseInputModule>().Select(m=>m.GetType().FullName));
