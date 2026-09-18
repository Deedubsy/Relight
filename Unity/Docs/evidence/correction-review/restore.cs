var original = Newtonsoft.Json.Linq.JObject.Parse(System.IO.File.ReadAllText("E:/Factorio2/Unity/Docs/evidence/correction-review/input-original.json"));
UnityEngine.InputSystem.InputSystem.settings.backgroundBehavior = (UnityEngine.InputSystem.InputSettings.BackgroundBehavior)(int)original["background"];
UnityEngine.InputSystem.InputSystem.settings.editorInputBehaviorInPlayMode = (UnityEngine.InputSystem.InputSettings.EditorInputBehaviorInPlayMode)(int)original["editor"];
if (UnityEngine.InputSystem.Keyboard.current != null) UnityEngine.InputSystem.InputSystem.QueueStateEvent(UnityEngine.InputSystem.Keyboard.current, new UnityEngine.InputSystem.LowLevel.KeyboardState());
return new { background = UnityEngine.InputSystem.InputSystem.settings.backgroundBehavior.ToString(), editor = UnityEngine.InputSystem.InputSystem.settings.editorInputBehaviorInPlayMode.ToString() };
