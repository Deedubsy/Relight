// Select Game view size preset 7 (3 = Full HD, the owner's setting; 7 = the existing "cli 1280x720").
var asm = typeof(UnityEditor.Editor).Assembly;
var bf = System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic;
var gvType = asm.GetType("UnityEditor.GameView");
var gv = UnityEngine.Resources.FindObjectsOfTypeAll(gvType)[0];
var p = gvType.GetProperty("selectedSizeIndex", bf);
p.SetValue(gv, 7);
((UnityEditor.EditorWindow)gv).Repaint();
return "selected=" + p.GetValue(gv) + " screen=" + UnityEngine.Screen.width + "x" + UnityEngine.Screen.height;
