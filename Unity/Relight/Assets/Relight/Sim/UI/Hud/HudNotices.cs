using System;
using System.Collections.Generic;

namespace Relight.Sim.UI
{
    /// <summary>How loud a notice is. Used to pick the USS class and to decide what may be dismissed.</summary>
    public enum HudNoticeKind
    {
        /// <summary>Neutral information (an autosave, a transfer result).</summary>
        Info = 0,
        /// <summary>Something the player should fix, but nothing is being destroyed (no fuel, output full).</summary>
        Warning = 1,
        /// <summary>Live danger. Never dismissible: UI_AND_ONBOARDING.md §8 — "dismissal never clears a live danger".</summary>
        Danger = 2,
    }

    /// <summary>One row of the HUD's alert area.</summary>
    public sealed class HudNotice
    {
        /// <summary>The inbox key. A second notice on the same key updates this row instead of stacking (§8).</summary>
        public string Key;
        public string Text;
        public HudNoticeKind Kind;
        /// <summary>1 the first time, then 2, 3 … — rendered as "× N", never as extra rows (§8).</summary>
        public int Repeats;
        /// <summary>Real (unscaled) seconds when the row was last written.</summary>
        public double At;
        /// <summary>Real seconds the row lives for; <see cref="double.PositiveInfinity"/> for a standing condition.</summary>
        public double Seconds;
        public bool Dismissed;

        public bool Live(double now) => !Dismissed && now - At < Seconds;
    }

    /// <summary>
    /// The HUD's keyed alert inbox (UI_AND_ONBOARDING.md §8): at most three transient rows beside the single urgent
    /// strip, a repeat on the same key updating one row with a count rather than stacking, and a dismissal that can
    /// never clear a live danger.
    ///
    /// Engine-free, so <c>Tests/Sim/UI/HudViewModelTests.cs</c> can hold it to those three rules without an editor.
    /// It keeps real (unscaled) seconds, never sim time: a paused game must not freeze a toast on screen.
    /// </summary>
    public sealed class HudNotices
    {
        /// <summary>UI_AND_ONBOARDING.md §4/§8: one urgent strip plus at most three transient rows.</summary>
        public const int MaxRows = 3;

        /// <summary>Default life of a transient row, in real seconds.</summary>
        public const double DefaultSeconds = 6;

        private readonly List<HudNotice> _rows = new List<HudNotice>();
        private readonly List<HudNotice> _live = new List<HudNotice>();

        /// <summary>The rows to draw, newest last, at most <see cref="MaxRows"/>. Re-used between calls.</summary>
        public IReadOnlyList<HudNotice> Rows => _live;

        /// <summary>
        /// Post a notice. An existing row with the same <paramref name="key"/> is updated, and its repeat count
        /// raised when the text and kind are both unchanged (a kind change alone keeps the count; new text starts
        /// it again at 1); a new key takes a new row and the oldest row is dropped once there are more than
        /// <see cref="MaxRows"/> live ones.
        /// </summary>
        public HudNotice Post(string key, string text, HudNoticeKind kind, double now, double seconds = DefaultSeconds)
        {
            if (string.IsNullOrEmpty(key)) key = text ?? "";
            for (var i = 0; i < _rows.Count; i++)
            {
                var r = _rows[i];
                if (!string.Equals(r.Key, key, StringComparison.Ordinal)) continue;
                var same = string.Equals(r.Text, text, StringComparison.Ordinal);
                // REL-6 (INT-02): only the same sentence said again is a repeat. The same sentence re-graded — a
                // standing defence row turning to danger as a raid is warned, and back when it is over — is one
                // notice changing its colour, not a second one, so its count stays where it was.
                if (!r.Live(now) || !same) r.Repeats = 1;
                else if (r.Kind == kind) r.Repeats++;
                r.Text = text;
                r.Kind = kind;
                r.At = now;
                r.Seconds = seconds;
                // A repeat on a dismissed key brings it back only when it is danger; §8's dismissal rule.
                if (r.Dismissed && kind == HudNoticeKind.Danger) r.Dismissed = false;
                Reap(now);
                return r;
            }

            var n = new HudNotice { Key = key, Text = text, Kind = kind, Repeats = 1, At = now, Seconds = seconds };
            _rows.Add(n);
            Reap(now);
            return n;
        }

        /// <summary>
        /// Dismiss one row. A <see cref="HudNoticeKind.Danger"/> row that is still live refuses — §8: "dismissal
        /// never clears a live danger". Returns whether the row was actually dismissed.
        /// </summary>
        public bool Dismiss(string key, double now)
        {
            for (var i = 0; i < _rows.Count; i++)
            {
                var r = _rows[i];
                if (!string.Equals(r.Key, key, StringComparison.Ordinal)) continue;
                if (r.Kind == HudNoticeKind.Danger && r.Live(now)) return false;
                r.Dismissed = true;
                Reap(now);
                return true;
            }
            return false;
        }

        /// <summary>Drop a standing row the moment its condition clears (an outage that was fixed).</summary>
        public void Clear(string key)
        {
            for (var i = _rows.Count - 1; i >= 0; i--)
                if (string.Equals(_rows[i].Key, key, StringComparison.Ordinal)) _rows.RemoveAt(i);
            for (var i = _live.Count - 1; i >= 0; i--)
                if (string.Equals(_live[i].Key, key, StringComparison.Ordinal)) _live.RemoveAt(i);
        }

        /// <summary>Expire what has timed out and refresh <see cref="Rows"/>. Call once per HUD refresh.</summary>
        public void Reap(double now)
        {
            for (var i = _rows.Count - 1; i >= 0; i--) if (!_rows[i].Live(now)) _rows.RemoveAt(i);
            _live.Clear();
            // Standing warnings and danger survive bursts of routine confirmations.
            for(int priority=(int)HudNoticeKind.Danger;priority>=0 && _live.Count<MaxRows;priority--)
                for(int i=_rows.Count-1;i>=0 && _live.Count<MaxRows;i--)
                    if((int)_rows[i].Kind==priority) _live.Add(_rows[i]);
            // Keep chronological ordering within each priority, including the existing all-info behavior.
            _live.Sort((a,b)=>a.Kind!=b.Kind?b.Kind.CompareTo(a.Kind):_rows.IndexOf(a).CompareTo(_rows.IndexOf(b)));
        }

        public void Reset()
        {
            _rows.Clear();
            _live.Clear();
        }
    }
}
