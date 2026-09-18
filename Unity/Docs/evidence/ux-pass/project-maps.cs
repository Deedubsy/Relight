var a=UnityEngine.InputSystem.InputSystem.actions;
return string.Join("\n",a.actionMaps.Select(m=>m.name+"="+m.enabled+" "+string.Join(",",m.actions.Select(x=>x.name+"="+x.enabled))))+" focused="+UnityEngine.Application.isFocused;
