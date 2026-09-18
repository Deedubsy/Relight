using System.Reflection;
using UnityEngine;
using UnityEngine.UIElements;

namespace Relight.Tests.Play.Ui
{
    /// <summary>
    /// Shared plumbing for the correction-pass UI Play Mode tests (W-B).
    ///
    /// Two jobs: find an element in whichever loaded <see cref="UIDocument"/> holds it, and drive a runtime panel
    /// with synthetic pointer and submit events. UI Toolkit's pooled events expose their fields through protected
    /// setters, so the pointer events are filled by reflection — the same approach
    /// <c>Tests/Play/Prototypes/PrototypeFixture.cs</c> already uses and the standard way to exercise a runtime
    /// panel without a device, a window or an EventSystem. It is duplicated rather than shared because the
    /// prototype tests live in their own assembly (Relight.Tests.Play.Prototypes) which this one does not
    /// reference.
    ///
    /// Written without the editor on this machine. It now COMPILES offline against the editor's managed
    /// assemblies, but Play Mode is unavailable here, so none of it has been RUN on this machine.
    /// </summary>
    internal static class UiFixture
    {
        internal const int MouseId = 0;

        /// <summary>The first loaded document holding an element with this name, and that element.</summary>
        internal static UIDocument DocumentWith(string name, out VisualElement found)
        {
            found = null;
            var documents = Object.FindObjectsByType<UIDocument>(FindObjectsSortMode.None);
            UIDocument owner = null;
            foreach (var document in documents)
            {
                var root = document != null ? document.rootVisualElement : null;
                if (root == null) continue;
                if (owner == null) owner = document;
                var element = root.Q<VisualElement>(name);
                if (element == null) continue;
                found = element;
                return document;
            }
            return owner;
        }

        /// <summary>Is this element actually on screen — laid out, displayed and visible?</summary>
        internal static bool Shown(VisualElement e)
        {
            if (e == null || e.panel == null) return false;
            for (var walk = e; walk != null; walk = walk.parent)
            {
                if (walk.resolvedStyle.display == DisplayStyle.None) return false;
                if (walk.resolvedStyle.visibility != Visibility.Visible) return false;
            }
            return e.worldBound.width > 0 && e.worldBound.height > 0;
        }

        /// <summary>Press a button the way the keyboard does: the route that does not need a pointer position.</summary>
        internal static void Submit(VisualElement button)
        {
            if (button == null) return;
            using (var submit = NavigationSubmitEvent.GetPooled())
            {
                submit.target = button;
                button.SendEvent(submit);
            }
        }

        internal static void Enter(VisualElement e) => Send(e, Make<PointerEnterEvent>(Centre(e), 0, 0));
        internal static void Leave(VisualElement e) => Send(e, Make<PointerLeaveEvent>(Centre(e), 0, 0));
        internal static void Down(VisualElement e, Vector2 at) => Send(e, Make<PointerDownEvent>(at, 0, 1));
        internal static void Move(VisualElement e, Vector2 at) => Send(e, Make<PointerMoveEvent>(at, 0, 1));
        internal static void Up(VisualElement e, Vector2 at) => Send(e, Make<PointerUpEvent>(at, 0, 0));

        internal static Vector2 Centre(VisualElement e) => e == null ? Vector2.zero : e.worldBound.center;

        private static void Send(VisualElement target, EventBase e)
        {
            if (target == null) return;
            // Unity 6000.6: VisualElement.SendEvent does not assign the event's target, and a pointer event with
            // no target reaches PointerEnterEvent.PreDispatch with a null elementTarget (NullReferenceException,
            // seen in the first correction-pass run). Name the target the way Submit already does.
            e.target = target;
            target.SendEvent(e);
            e.Dispose();
        }

        private static T Make<T>(Vector2 p, int button, int pressed) where T : PointerEventBase<T>, new()
        {
            var e = PointerEventBase<T>.GetPooled();
            SetProperty(e, "pointerId", MouseId);
            SetProperty(e, "pointerType", UnityEngine.UIElements.PointerType.mouse);
            SetProperty(e, "position", new Vector3(p.x, p.y, 0f));
            SetProperty(e, "localPosition", new Vector3(p.x, p.y, 0f));
            SetProperty(e, "button", button);
            SetProperty(e, "pressedButtons", pressed);
            SetProperty(e, "isPrimary", true);
            SetProperty(e, "clickCount", 1);
            return e;
        }

        private static void SetProperty(object target, string name, object value)
        {
            var p = target.GetType().GetProperty(name, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            var setter = p?.GetSetMethod(true);
            if (setter == null)
            {
                var f = target.GetType().GetField("m_" + name, BindingFlags.NonPublic | BindingFlags.Instance);
                f?.SetValue(target, value);
                return;
            }
            setter.Invoke(target, new[] { value });
        }
    }
}
