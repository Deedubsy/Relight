var game=UnityEditor.EditorWindow.GetWindow(typeof(UnityEditor.Editor).Assembly.GetType("UnityEditor.GameView"));
var original=Newtonsoft.Json.Linq.JObject.Parse(System.IO.File.ReadAllText("E:/Factorio2/Unity/Docs/evidence/ui-repair/original-view.json"));
game.GetType().GetProperty("selectedSizeIndex",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.Public|System.Reflection.BindingFlags.NonPublic).SetValue(game,(int)original["index"]);
return new{playing=UnityEditor.EditorApplication.isPlaying};
