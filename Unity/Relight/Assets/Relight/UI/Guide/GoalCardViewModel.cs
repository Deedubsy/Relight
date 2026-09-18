using System.Collections.Generic;
using System.Globalization;
using Relight.Presentation;
using Relight.Sim;

namespace Relight.UI
{
    /// <summary>
    /// C-09. The view-model half of the goal card (TECHNICAL_ARCHITECTURE.md §8.2): it reads the objective chain
    /// through <see cref="OpeningQueries.Objective"/>, formats, and exposes plain strings. It is not a
    /// MonoBehaviour, holds no VisualElement and knows nothing about UXML.
    ///
    /// Nothing here decides what the objective IS. The chain is a priority list evaluated from live state on every
    /// call (reference goal.ts:80 <c>campaignNext</c>), so this class never remembers a step, never advances one and
    /// never caches a completion: it asks again and paints the answer.
    ///
    /// The refresh throttle is the reference HUD's own — <c>if (now - last &lt; 150) return</c>, hud.ts:47.
    /// </summary>
    public sealed class GoalCardViewModel
    {
        /// <summary>Reference hud.ts:47 — the HUD's refresh throttle, in seconds.</summary>
        public const double RefreshSeconds = 0.150;

        /// <summary>Authored material rows in GoalCard.uxml. A longer list is truncated rather than generated.</summary>
        public const int MaxMaterials = 6;

        private readonly List<string> _materials = new List<string>(MaxMaterials);
        private readonly List<bool> _ready = new List<bool>(MaxMaterials);
        private double _lastRefresh = double.NegativeInfinity;

        /// <summary>Where the engineer is, as the region labels of the imported map name it; "" when unnamed.</summary>
        public string Place { get; private set; } = "";

        /// <summary>The objective title, e.g. "1 · Build a Generator".</summary>
        public string Title { get; private set; } = "";

        /// <summary>The next action line.</summary>
        public string Text { get; private set; } = "";

        /// <summary>The "Why this next?" body; "" hides the foldout.</summary>
        public string Detail { get; private set; } = "";

        /// <summary>Reference goal.ts <c>NextAction.location</c>: the tile "Show location" centres on.</summary>
        public bool HasLocation { get; private set; }

        /// <summary>Sim tile X of <see cref="HasLocation"/>, Y-down (U-M-14).</summary>
        public double LocationX { get; private set; }

        /// <summary>Sim tile Y of <see cref="HasLocation"/>, Y-down (U-M-14).</summary>
        public double LocationY { get; private set; }

        /// <summary>Rows for the "Required resources in Backpack" list, reference hud.ts:54.</summary>
        public IReadOnlyList<string> Materials => _materials;

        /// <summary>Per-row: the Backpack already holds enough (reference hud.ts:54 class "ready").</summary>
        public IReadOnlyList<bool> MaterialReady => _ready;
        public readonly List<float> MaterialFractions = new List<float>();

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
                Place = Title = Text = Detail = "";
                HasLocation = false;
                _materials.Clear();
                MaterialFractions.Clear();
                _ready.Clear();
                Refreshes++;
                return true;
            }

            var ctx = sim.Context;
            var st = sim.State;
            var next = OpeningQueries.Objective(ctx, st);

            Place = PlaceName(ctx, st);
            Title = next.Title ?? "";
            Text = next.Text ?? "";
            Detail = next.Detail ?? "";
            foreach(var material in next.Materials)
                if(material.Available < material.Required && !string.IsNullOrEmpty(material.Source))
                {
                    Text = "Need " + (material.Required-material.Available) + " " + material.DisplayName + " — " + material.Source + ".";
                    break;
                }
            HasLocation = next.HasLocation;
            LocationX = next.Location.X;
            LocationY = next.Location.Y;

            _materials.Clear();
                MaterialFractions.Clear();
            _ready.Clear();
            var list = next.Materials;
            for (var i = 0; i < list.Count && i < MaxMaterials; i++)
            {
                var m = list[i];
                _materials.Add(Row(m));
                _ready.Add(m.Available >= m.Required);
                MaterialFractions.Add(m.Required > 0 ? (float)System.Math.Min(1, System.Math.Max(0, m.Available / (double)m.Required)) : 1);
            }

            Refreshes++;
            return true;
        }

        /// <summary>
        /// Reference hud.ts:54 — "<c>{label} — {available}/{required}</c>", with the port's two additions, both
        /// from the owner playtest (correction pass R5): when the Backpack is short and a Home chest holds some,
        /// the row says so rather than leaving the player to guess; and the row then says WHERE to get the rest,
        /// e.g. "Steel — 20/30 · 4 at Home · mine salvage rubble 12 tiles north-east". The short form comes from
        /// <see cref="ObjectiveMaterial.Source"/>, which the sim derives from the loaded map — this class only
        /// pastes it in. The whole sentence, with the action, is already on <see cref="Detail"/>.
        /// </summary>
        private static string Row(ObjectiveMaterial m)
        {
            var row = string.Format(CultureInfo.InvariantCulture, "{0} — {1}/{2}",
                m.DisplayName, m.Available, m.Required);
            return (m.Available >= m.Required ? "✓  " : "○  ") + row;

        }

        /// <summary>
        /// The goal card's place line (UI_AND_ONBOARDING.md §4). The imported region carries its own names as
        /// <see cref="SiteKind.Label"/> sites (riverfront.ts regions), so the place is the label whose centre is
        /// nearest the engineer; a map with no labels falls back to the Home core's name, then to "".
        ///
        /// This is presentation naming, not a sim rule, which is why it lives here and not in
        /// <see cref="OpeningQueries"/>: no sim decision reads it.
        /// </summary>
        private static string PlaceName(SimContext ctx, SimState st)
        {
            if (ctx.Sites == null) return "";
            var at = WorldQueries.Engineer(ctx, st).Pos;
            string best = null;
            var bestD = double.MaxValue;
            foreach (var s in ctx.Sites.OfKind(SiteKind.Label))
            {
                var c = s.Centre;
                var dx = c.X - at.X;
                var dy = c.Y - at.Y;
                var d = dx * dx + dy * dy;
                if (d >= bestD) continue;
                bestD = d;
                best = s.Name;
            }
            if (!string.IsNullOrEmpty(best)) return best;
            var core = ctx.Sites.Core;
            return core != null && !string.IsNullOrEmpty(core.Name) ? core.Name : "";
        }
    }
}
