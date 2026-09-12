using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using Relight.Presentation;
using Relight.Sim;

namespace Relight.UI
{
    /// <summary>
    /// B-13. The view-model layer of TECHNICAL_ARCHITECTURE.md §8.2: it reads the simulation through the B-06/B-08
    /// selectors, formats, and exposes plain strings. It is not a MonoBehaviour, holds no VisualElement, and knows
    /// nothing about UXML — which is what lets the panel's layout be rearranged in UI Builder without touching C#.
    ///
    /// Content and formatting are the reference HUD's clock and status strip (hud.ts:47-56): "Day N · HH:MM",
    /// " · Paused" appended while the clock is stopped, and the engineer's condition beside it. The refresh throttle
    /// is the reference's too — <c>if (now - last &lt; 150) return</c>, hud.ts:47 — so the panel updates at most
    /// about seven times a second no matter the frame rate.
    /// </summary>
    public sealed class StatusPanelViewModel
    {
        /// <summary>Reference hud.ts:47 — the HUD's own refresh throttle, in seconds.</summary>
        public const double RefreshSeconds = 0.150;

        private readonly List<string> _pockets = new List<string>(8);
        private readonly StringBuilder _sb = new StringBuilder(64);
        private double _lastRefresh = double.NegativeInfinity;

        /// <summary>"Day 1 · 06:00", with " · Paused" while the host is paused (reference hud.ts:56).</summary>
        public string Clock { get; private set; } = "Day 1 · 00:00";

        /// <summary>"48 / 50 HP".</summary>
        public string Health { get; private set; } = "";

        /// <summary>"6.5, 6.5" — the engineer's sim tile position, Y-down (U-M-14).</summary>
        public string Position { get; private set; } = "";

        /// <summary>"Stamina 100%", plus " · sprinting" / " · down".</summary>
        public string Condition { get; private set; } = "";

        /// <summary>Carried items, "coal 12 · steel 4", or "Pockets empty".</summary>
        public string Pockets { get; private set; } = "";

        /// <summary>"3 machines placed".</summary>
        public string Machines { get; private set; } = "";

        /// <summary>Ticks run since the session started, for the "is it actually running" line.</summary>
        public string Ticks { get; private set; } = "";

        /// <summary>Number of completed refreshes, so a test can prove the throttle is doing something.</summary>
        public int Refreshes { get; private set; }

        /// <summary>
        /// Recompute if the throttle allows. <paramref name="now"/> is unscaled real time, not sim time.
        /// Returns true when the properties changed.
        /// </summary>
        public bool Refresh(SimHost host, double now, bool force = false)
        {
            if (!force && now - _lastRefresh < RefreshSeconds) return false;
            _lastRefresh = now;
            var sim = host == null ? null : host.Simulation;
            if (sim == null)
            {
                Clock = "No session";
                Health = Position = Condition = Pockets = Machines = Ticks = "";
                Refreshes++;
                return true;
            }

            var ctx = sim.Context;
            var st = sim.State;
            var e = WorldQueries.Engineer(ctx, st);

            Clock = FormatClock(st.T, ctx.Data.Time.DaySeconds, host.Paused);
            Health = string.Format(CultureInfo.InvariantCulture, "{0:0} / {1:0} HP", e.Hp, e.MaxHp);
            Position = string.Format(CultureInfo.InvariantCulture, "{0:0.0}, {1:0.0}", e.Pos.X, e.Pos.Y);

            _sb.Clear();
            _sb.AppendFormat(CultureInfo.InvariantCulture, "Stamina {0:0}%", e.Stamina * 100.0);
            if (e.IsDown) _sb.Append(" · down");
            else if (e.Sprinting) _sb.Append(" · sprinting");
            Condition = _sb.ToString();

            _pockets.Clear();
            var carried = InventoryQueries.Carried(ctx, st);
            for (var i = 0; i < carried.Count; i++)
            {
                var line = carried[i];
                if (line.Count <= 0) continue;
                _pockets.Add(string.Format(CultureInfo.InvariantCulture, "{0} {1:0.##}", line.DisplayName, line.Count));
            }
            Pockets = _pockets.Count == 0 ? "Pockets empty" : string.Join(" · ", _pockets);

            var placed = MachineSnapshot.Count(st);
            Machines = placed == 1 ? "1 machine placed" : placed + " machines placed";
            Ticks = string.Format(CultureInfo.InvariantCulture, "{0} ticks · {1:0.00} ms/tick",
                host.TotalTicks, host.AverageTickCostMs);

            Refreshes++;
            return true;
        }

        /// <summary>Reference hud.ts:56, with the campaign clock's day length taken from the data record.</summary>
        public static string FormatClock(double t, double daySeconds, bool paused)
        {
            if (daySeconds <= 0) daySeconds = 1;
            var day = (int)Math.Floor(t / daySeconds) + 1;
            var elapsed = t - (day - 1) * daySeconds;
            var minutes = (int)Math.Floor(elapsed / daySeconds * 1440.0);
            return string.Format(CultureInfo.InvariantCulture, "Day {0} · {1:00}:{2:00}{3}",
                day, minutes / 60, minutes % 60, paused ? " · Paused" : "");
        }
    }
}
