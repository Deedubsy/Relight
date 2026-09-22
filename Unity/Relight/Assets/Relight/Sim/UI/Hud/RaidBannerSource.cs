using System.Collections.Generic;
using System.Globalization;

namespace Relight.Sim.UI
{
    /// <summary>
    /// REL-74 (E-20, U-D-66 (7)): the wave banner. One short line across the top of the screen as a raid reaches the
    /// ground, and again as each later wave of a large raid opens: "Wave 3 of 4, from the east".
    ///
    /// <b>Each wave is named once.</b> The director already counts the waves it has named
    /// (<see cref="MajorRaid.WaveAnnounced"/>, saved), so this class follows that count and shows one banner per step
    /// of it, never a second for the same step. It keeps its own count of what it has shown, per session, because a
    /// banner is a thing this screen showed, not a thing the save remembers.
    ///
    /// <b>A raid met part-way gets no catch-up.</b> A game loaded mid-assault starts at the waves already named, so
    /// it does not fire a burst of banners for waves that opened before the session did; the next wave to open gets
    /// its banner as usual. The rule for "seen arriving" is the account's (<see cref="RaidAccountSource"/>): a raid
    /// this session saw waiting, or met within <see cref="RaidAccountSource.ArrivalGraceS"/> of its start.
    ///
    /// Skipped: the scripted opening group (the goal card owns it, U-D-54), and any raid already turning back.
    /// Per session and never saved; engine-free, so <c>Tests/Sim/UI/RaidFeedbackTests.cs</c> drives it without an
    /// editor.
    /// </summary>
    public sealed class RaidBannerSource
    {
        /// <summary>Real seconds a banner stays up.</summary>
        public const double Seconds = 5;

        /// <summary>The newest banner, "" before the first.</summary>
        public string Text { get; private set; } = "";

        /// <summary>Goes up by one per banner, so the view model shows each exactly once.</summary>
        public int Serial { get; private set; }

        private readonly HashSet<int> _seenWaiting = new HashSet<int>();
        private int _majorId = -1;
        private int _shown;                                  // waves of _majorId already bannered (or skipped)
        private int _minorId = -1;

        public void Refresh(SimContext ctx, SimState st)
        {
            if (ctx == null || st == null || st.Director == null) return;
            var d = st.Director;
            if (d.Major != null && st.T < d.Major.StartsAt) _seenWaiting.Add(d.Major.Id);
            if (d.Minor != null && !d.Minor.Spawned && !d.Minor.Scripted) _seenWaiting.Add(d.Minor.Id);

            var a = d.Major;
            if (a != null && st.T >= a.StartsAt)
            {
                if (a.Id != _majorId)
                {
                    _majorId = a.Id;
                    // Met on the ground: no banners for the waves that were named before this session saw it.
                    _shown = Seen(st, a.Id, a.StartsAt) ? 0 : a.WaveAnnounced;
                }
                if (!a.Retreat && a.WaveAnnounced > _shown)
                {
                    // The director names waves in order and can name two in one tick only when a wave gap is shorter
                    // than a tick; the newest is the one walking in, so it is the one worth the top of the screen.
                    var w = a.WaveAnnounced - 1;
                    var side = DirectorQueries.SideWords(ctx, st, SiegePlan.Sides(a, w));
                    Show(a.Waves > 1
                        ? "Wave " + Num(w + 1) + " of " + Num(a.Waves) + From(side)
                        : "Major assault" + From(side));
                }
                _shown = a.WaveAnnounced;
            }

            var m = d.Minor;
            if (m != null && m.Spawned && m.Id != _minorId)
            {
                _minorId = m.Id;
                if (!m.Scripted && !m.Retreat && Seen(st, m.Id, m.StartsAt))
                {
                    var side = m.Origin < 0 ? "" : DirectorQueries.SideWords(ctx, st, new[] { m.Origin });
                    Show("Raid" + From(side));
                }
            }
        }

        private bool Seen(SimState st, int id, double startsAt) =>
            _seenWaiting.Contains(id) || st.T - startsAt <= RaidAccountSource.ArrivalGraceS;

        private void Show(string text)
        {
            Text = text;
            Serial++;
        }

        private static string From(string side) => side.Length == 0 ? "" : ", from the " + side;

        private static string Num(int n) => n.ToString(CultureInfo.InvariantCulture);
    }
}
