var title = UnityEngine.Object.FindAnyObjectByType<Relight.UI.FrontEnd.TitleScreenController>();
if (title == null) return "No title controller";
title.GetType().GetMethod("OnStart", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic).Invoke(title, null);
return "Started a fresh audit session through the title controller's Start handler";
