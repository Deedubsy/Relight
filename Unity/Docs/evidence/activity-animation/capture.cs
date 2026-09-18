var host=UnityEngine.Object.FindAnyObjectByType<Relight.Presentation.SimHost>();
var presenter=UnityEngine.Object.FindAnyObjectByType<Relight.Presentation.MachinePresenter>();
var power=UnityEngine.Object.FindAnyObjectByType<Relight.Presentation.PowerConnectionPresenter>();
UnityEngine.ScreenCapture.CaptureScreenshot("E:/Factorio2/Unity/Docs/evidence/activity-animation/first.png");
return new {power.Drawn,power.Energized,views=presenter.Views.Values.Select(v=>new {v.MachineId,v.Kind,v.ActivityState,v.ActivityMotion,scale=v.transform.localScale,visible=v.GetComponentInChildren<UnityEngine.SpriteRenderer>().isVisible}).ToArray()};
