// List the Game view's size presets and the selected one. Read only.
var asm = typeof(UnityEditor.Editor).Assembly;
var bf = System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic;
var gvType = asm.GetType("UnityEditor.GameView");
var gv = UnityEngine.Resources.FindObjectsOfTypeAll(gvType);
var sizesType = asm.GetType("UnityEditor.GameViewSizes");
var single = typeof(UnityEditor.ScriptableSingleton<>).MakeGenericType(sizesType);
var inst = single.GetProperty("instance", bf).GetValue(null);
var groupType = sizesType.GetProperty("currentGroupType", bf).GetValue(inst);
var group = sizesType.GetMethod("GetGroup", bf).Invoke(inst, new object[] { groupType });
var total = (int)group.GetType().GetMethod("GetTotalCount", bf).Invoke(group, null);
var sb = new System.Text.StringBuilder("views=" + gv.Length + " group=" + groupType + " total=" + total);
for (var i = 0; i < total; i++)
{
    var s = group.GetType().GetMethod("GetGameViewSize", bf).Invoke(group, new object[] { i });
    sb.Append(" | " + i + ":" + s.GetType().GetProperty("displayText", bf).GetValue(s));
}
if (gv.Length > 0) sb.Append(" || selected=" + gvType.GetProperty("selectedSizeIndex", bf).GetValue(gv[0]));
sb.Append(" screen=" + UnityEngine.Screen.width + "x" + UnityEngine.Screen.height);
return sb.ToString();
