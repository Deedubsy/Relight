var keyboard=UnityEngine.InputSystem.Keyboard.current;
if (!keyboard.enabled) UnityEngine.InputSystem.InputSystem.EnableDevice(keyboard);
var router=UnityEngine.Object.FindAnyObjectByType<Relight.UI.InputRouter>();
UnityEngine.InputSystem.InputSystem.QueueStateEvent(keyboard,new UnityEngine.InputSystem.LowLevel.KeyboardState(UnityEngine.InputSystem.Key.B));
return new { keyboard=keyboard.enabled, key=keyboard.deviceId, build=router.ToggleBuild.enabled,bindings=router.ToggleBuild.bindings.Select(x=>x.effectivePath).ToArray(), controls=router.ToggleBuild.controls.Select(x=>x.path).ToArray() };
