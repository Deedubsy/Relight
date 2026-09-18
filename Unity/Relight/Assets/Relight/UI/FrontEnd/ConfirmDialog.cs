using System;
using Relight.Sim.UI;
using UnityEngine.UIElements;

namespace Relight.UI.FrontEnd
{
    /// <summary>
    /// C-10. The one modal card, shared by the title screen and the pause menu: the five confirmations of
    /// UI_AND_ONBOARDING.md §2.6.4 and the save-failure modal of §2.7.3 are all the same shape — a question, a
    /// body that says what will be lost, and buttons in order, left to right.
    ///
    /// The wording is never built here: it comes from <see cref="Confirmation"/> objects made by
    /// <see cref="FrontEndText"/>, which are engine-free and tested. This class only puts them on screen. Buttons
    /// are created per showing because their number varies (three for "Save and start · Start without saving ·
    /// Cancel", two for "Delete · Cancel"); everything else is authored in UXML.
    ///
    /// Escape chooses the <b>last</b> button, which <see cref="Confirmation"/> guarantees is the one that changes
    /// nothing. That is the whole reason the last-is-safe rule is written into that type rather than left to each
    /// caller: a player who panics and hits Escape must never destroy a save.
    /// </summary>
    public sealed class ConfirmDialog : IEscapeHandler
    {
        /// <summary>Applied to the overlay to show it; the base class is <c>display: none</c>.</summary>
        public const string OpenClass = "open";

        private readonly VisualElement _overlay;
        private readonly Label _title;
        private readonly Label _body;
        private readonly VisualElement _buttons;
        private Action<int> _chosen;

        public ConfirmDialog(VisualElement overlay)
        {
            _overlay = overlay;
            _title = overlay?.Q<Label>("modal-title");
            _body = overlay?.Q<Label>("modal-body");
            _buttons = overlay?.Q<VisualElement>("modal-buttons");
        }

        /// <summary>True while a question is on screen.</summary>
        public bool IsOpen { get; private set; }

        /// <summary>
        /// The port has no nav bar, so the reference's <c>details[open]</c> link (uiShell.ts:118) has no feature
        /// to close. This dialogue takes the slot: it is the innermost overlay after the Backpack's slot menu, so
        /// Escape reaching it here dismisses the question before it would close a screen behind the question.
        /// The ORDER of the reference's ladder is preserved exactly; only what occupies this rung differs, and the
        /// report records it.
        /// </summary>
        public int Order => EscapeOrder.CloseNavMenu;

        public bool OnEscape()
        {
            if (!IsOpen) return false;
            Cancel();
            return true;
        }

        /// <summary>
        /// Show <paramref name="c"/>. <paramref name="chosen"/> is called with the index of the button pressed,
        /// which is the index into <see cref="Confirmation.Buttons"/> — so a caller reads its own choices back in
        /// the order it declared them and never matches on button text.
        /// </summary>
        public void Show(Confirmation c, Action<int> chosen)
        {
            if (_overlay == null || c == null) return;
            _chosen = chosen;
            if (_title != null) _title.text = c.Title;
            if (_body != null) _body.text = c.Body;
            if (_buttons != null)
            {
                _buttons.Clear();
                for (var i = 0; i < c.Buttons.Length; i++)
                {
                    var index = i;                       // captured per button, not per loop
                    var button = new Button(() => Choose(index)) { text = c.Buttons[i] };
                    button.AddToClassList("action-button");
                    _buttons.Add(button);
                }
            }
            _overlay.AddToClassList(OpenClass);
            IsOpen = true;
            // Focus the safe choice, so Return on a keyboard does what Escape does.
            var last = _buttons != null && _buttons.childCount > 0 ? _buttons[_buttons.childCount - 1] : null;
            last?.Focus();
        }

        /// <summary>Dismiss with the safe choice — the last button — exactly as Escape does.</summary>
        public void Cancel()
        {
            if (!IsOpen) return;
            var index = _buttons == null ? -1 : _buttons.childCount - 1;
            Choose(index);
        }

        /// <summary>Take the card down without answering. Used when the screen behind it is torn down.</summary>
        public void Close()
        {
            _chosen = null;
            IsOpen = false;
            _overlay?.RemoveFromClassList(OpenClass);
            _buttons?.Clear();
        }

        private void Choose(int index)
        {
            var fn = _chosen;
            Close();
            if (index >= 0) fn?.Invoke(index);
        }
    }
}
