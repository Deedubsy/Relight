using System;

namespace Relight.Sim.UI
{
    /// <summary>
    /// C-10. One confirmation dialogue: a question, a plain-words body and its buttons in order, left to right.
    /// UI_AND_ONBOARDING.md §2.6.4 fixes all five; building them here keeps their wording testable and keeps the
    /// rule that <b>every destructive step is confirmed and the confirmation says what will be lost</b> in one place.
    /// </summary>
    public sealed class Confirmation
    {
        public Confirmation(string title, string body, params string[] buttons)
        {
            Title = title ?? "";
            Body = body ?? "";
            Buttons = buttons ?? Array.Empty<string>();
        }

        public string Title { get; }
        public string Body { get; }

        /// <summary>In order. The last is always the one that changes nothing.</summary>
        public string[] Buttons { get; }
    }
}
