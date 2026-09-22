// Capture the Game view back buffer (UI Toolkit overlay included) to a file outside Assets. Assistant Play session only.
var path = @"C:\Users\Admin\AppData\Local\Temp\claude-rel58\ui.png";
if (System.IO.File.Exists(path)) System.IO.File.Delete(path);
UnityEngine.ScreenCapture.CaptureScreenshot(path);
return "queued " + UnityEngine.Screen.width + "x" + UnityEngine.Screen.height;
