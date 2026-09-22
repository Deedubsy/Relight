// REL-58 look (GP-UX-7 c): the method the Tab binding calls (UiShell.Update -> ToggleBackpack).
var shell = UnityEngine.Object.FindAnyObjectByType<Relight.UI.UiShell>();
var r = shell.ToggleBackpack();
return "toggled " + r + " active=" + shell.GetType().GetField("_active", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic).GetValue(shell);
