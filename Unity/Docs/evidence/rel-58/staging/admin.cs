// REL-58 look (GP-UX-7 d): the method the B binding calls (UiShell.Update -> Toggle(buildPanel)).
var shell = UnityEngine.Object.FindAnyObjectByType<Relight.UI.UiShell>();
var r = shell.Toggle("admin-panel");
return "toggled " + r + " active=" + shell.GetType().GetField("_active", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic).GetValue(shell);
