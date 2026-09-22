// REL-58 look: press E on the real keyboard device (released by kup.cs), as ControlsCorrectionTests does.
var kb = UnityEngine.InputSystem.Keyboard.current;
if (kb == null) return "no keyboard";
UnityEngine.InputSystem.InputSystem.QueueStateEvent(kb, new UnityEngine.InputSystem.LowLevel.KeyboardState(UnityEngine.InputSystem.Key.E));
return "down E";
