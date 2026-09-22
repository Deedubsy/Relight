using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace Relight.Sim
{
    /// <summary>
    /// One finished raid, as the log records it. <see cref="Text"/> is the log line.
    /// </summary>
    /// <param name="RaidId">The group id its bodies carried.</param>
    /// <param name="Major">Large raid (true) or small (false).</param>
    /// <param name="Scripted">The opening's scripted encounter.</param>
    /// <param name="Outcome">A <see cref="RaidOutcome"/> as an int, from the <see cref="RaidEndedEvent"/>.</param>
    /// <param name="StartedAt">Sim second the raid began (a large raid's start time, a small raid's arrival).</param>
    /// <param name="EndedAt">Sim second the director dropped it.</param>
    /// <param name="Spawned">Bodies born to this raid while the log was watching.</param>
    /// <param name="Killed">Bodies of this raid killed while the log was watching.</param>
    /// <param name="PeakAlive">The most of its bodies alive at any one look — where the 48 active-raid budget bites.</param>
    /// <param name="RoundsFired">Turret shots plus engineer shots on the whole map while it ran.</param>
    /// <param name="TurretsDry">Turrets that ran dry while it ran, not counting any already empty when it began.</param>
    /// <param name="CoreHpLost">Core hit points lost to hits while it ran; a repair does not take them back.</param>
    /// <param name="StructuresWrecked">Structures whose HP reached 0 while it ran.</param>
    /// <param name="Shared">True when another raid was on the ground at the same time, so the map-wide counts
    /// (rounds, dry turrets, damage) are shared between the two lines rather than divided.</param>
    public sealed record RaidLogLine(int RaidId, bool Major, bool Scripted, int Outcome, double StartedAt, double EndedAt,
        int Spawned, int Killed, int PeakAlive, int RoundsFired, int TurretsDry, double CoreHpLost, int StructuresWrecked,
        bool Shared)
    {
        /// <summary>Seconds from the raid's start to the director dropping it.</summary>
        public double Seconds => EndedAt - StartedAt;

        /// <summary>The director's word for how it ended.</summary>
        public string OutcomeWord => Outcome == (int)RaidOutcome.Cleared ? "cleared"
            : Outcome == (int)RaidOutcome.Lost ? "lost"
            : Outcome == (int)RaidOutcome.BrokeOff ? "broke off"
            : "ended";

        /// <summary>The one line. Fixed clause order, invariant culture, no line breaks: made to be grepped.</summary>
        public string Text
        {
            get
            {
                var c = CultureInfo.InvariantCulture;
                var sb = new StringBuilder(200);
                sb.Append("raid #").Append(RaidId.ToString(c));
                sb.Append(Scripted ? " scripted" : Major ? " large" : " small");
                sb.Append(' ').Append(OutcomeWord);
                sb.Append(" · T ").Append(StartedAt.ToString("0.0", c)).Append('→').Append(EndedAt.ToString("0.0", c));
                sb.Append(" · ").Append(Seconds.ToString("0.0", c)).Append(Outcome == (int)RaidOutcome.Cleared ? " s to clear" : " s until it ended");
                sb.Append(" · ").Append(Spawned.ToString(c)).Append(" spawned");
                sb.Append(" · ").Append(Killed.ToString(c)).Append(" killed");
                sb.Append(" · peak ").Append(PeakAlive.ToString(c)).Append(" alive");
                sb.Append(" · ").Append(RoundsFired.ToString(c)).Append(" rounds fired");
                sb.Append(" · ").Append(TurretsDry.ToString(c)).Append(TurretsDry == 1 ? " turret dry" : " turrets dry");
                sb.Append(" · core lost ").Append(Math.Ceiling(CoreHpLost).ToString("0", c)).Append(" HP");
                sb.Append(" · ").Append(StructuresWrecked.ToString(c)).Append(StructuresWrecked == 1 ? " structure wrecked" : " structures wrecked");
                if (Shared) sb.Append(" · map-wide counts shared with an overlapping raid");
                return sb.ToString();
            }
        }
    }

    /// <summary>
    /// REL-85 (CMB-09c, E-19): every raid leaves one log line. A per-session observer of the sim's events and
    /// state — never saved, never a phase, never a persistence channel — that opens a tally when a raid is on the
    /// ground and closes it, as one <see cref="RaidLogLine"/>, on the raid's <see cref="RaidEndedEvent"/>.
    ///
    /// It is the measuring twin of the HUD's <see cref="UI.RaidAccountSource"/>, and shares its rules where the
    /// two overlap (a kill counts by the dead body's group; a turret already dry when the raid began did not run
    /// dry in it; core HP lost is the hits, so a repair cannot hide them). It differs on purpose where a measurement
    /// wants more than a player's notice does: every raid gets a line, the scripted opening included; a raid the
    /// session joined part-way through gets a line marked by what it saw (a small <c>Spawned</c>); and rounds,
    /// dry turrets and wrecks are counted on the WHOLE MAP, not the raid's district, because a headless run has no
    /// player to mislead and the tuning question is "what did this raid cost", wherever it was paid. Two raids on
    /// the ground at once (a small one still leaving when a large one starts) both carry the map-wide counts and
    /// both say so (<see cref="RaidLogLine.Shared"/>); kills and spawns are always the raid's own.
    ///
    /// Engine-free, so the offline suite drives it; the host (<c>SimHost</c>) feeds it each frame's drained events
    /// and writes each line to the Unity log, and the real-city defence run writes them to a file.
    /// </summary>
    public sealed class RaidLog
    {
        /// <summary>What a column of lines means, for the head of a written file.</summary>
        public const string Header =
            "one line per raid: id, size, outcome · start→end sim seconds · length · bodies spawned · killed · " +
            "peak alive · rounds fired (turrets + engineer, whole map) · turrets that ran dry · core HP lost to hits · " +
            "structures wrecked";

        private sealed class Tally
        {
            public int Id;
            public bool Major, Scripted;
            public double StartedAt;
            public int Spawned, Killed, PeakAlive, Rounds;
            public double CoreLost;
            public bool Shared;
            public readonly HashSet<int> DryAtStart = new HashSet<int>();
            public readonly HashSet<int> WentDry = new HashSet<int>();
            public readonly HashSet<int> Wrecked = new HashSet<int>();
        }

        private readonly List<RaidLogLine> _lines = new List<RaidLogLine>();
        private readonly List<Tally> _open = new List<Tally>(2);

        /// <summary>Every line written this session, oldest first.</summary>
        public IReadOnlyList<RaidLogLine> Lines => _lines;

        /// <summary>Raised with each line as it is written.</summary>
        public event Action<RaidLogLine> Written;

        /// <summary>Raids on the ground and being tallied right now.</summary>
        public int Watching => _open.Count;

        /// <summary>
        /// Called once per tick (or once per frame with that frame's drained events) AFTER the tick has run. Order
        /// matters within: tallies open from the director's state first, then the live looks (peak alive, dry
        /// turrets), then the events, which may close a tally.
        /// </summary>
        public void Observe(SimContext ctx, SimState st, IReadOnlyList<SimEvent> events)
        {
            if (ctx == null || st == null || st.Director == null) return;
            var d = st.Director;
            if (d.Major != null && st.T >= d.Major.StartsAt) Open(ctx, st, d.Major.Id, true, false, d.Major.StartsAt);
            if (d.Minor != null && d.Minor.Spawned) Open(ctx, st, d.Minor.Id, false, d.Minor.Scripted, d.Minor.StartsAt);
            if (_open.Count > 1) for (var i = 0; i < _open.Count; i++) _open[i].Shared = true;

            for (var i = 0; i < _open.Count; i++)
            {
                var t = _open[i];
                var alive = EnemyQueries.GroupAlive(st, t.Id);
                if (alive > t.PeakAlive) t.PeakAlive = alive;
                DryTurrets(ctx, st, t.WentDry, t.DryAtStart);
            }

            if (events == null) return;
            for (var k = 0; k < events.Count; k++)
            {
                switch (events[k])
                {
                    case EnemySpawnedEvent s:
                        var ts = Find(s.Group);
                        if (ts != null) ts.Spawned++;
                        break;
                    case EnemyKilledEvent kill:
                        var tk = kill.Layer == EnemyLayer.Site ? null : Find(kill.Group);
                        if (tk != null) tk.Killed++;
                        break;
                    case TurretShotEvent _:
                    case WeaponFiredEvent _:
                        for (var i = 0; i < _open.Count; i++) _open[i].Rounds++;
                        break;
                    case CoreDamagedEvent c:
                        for (var i = 0; i < _open.Count; i++) _open[i].CoreLost += c.Lost;
                        break;
                    case StructureDamagedEvent sd when sd.Hp <= 0:
                        for (var i = 0; i < _open.Count; i++) _open[i].Wrecked.Add(sd.MachineId);
                        break;
                    case RaidEndedEvent e:
                        // A raid this session never saw on the ground still gets its line, from an empty tally.
                        var te = Find(e.RaidId) ?? Open(ctx, st, e.RaidId, e.Major, e.Scripted, e.StartedAt);
                        Close(te, e);
                        break;
                }
            }
        }

        private Tally Find(int id)
        {
            for (var i = 0; i < _open.Count; i++) if (_open[i].Id == id) return _open[i];
            return null;
        }

        private Tally Open(SimContext ctx, SimState st, int id, bool major, bool scripted, double startedAt)
        {
            var t = Find(id);
            if (t != null) return t;
            t = new Tally { Id = id, Major = major, Scripted = scripted, StartedAt = startedAt };
            DryTurrets(ctx, st, t.DryAtStart, null);
            _open.Add(t);
            return t;
        }

        /// <summary>Every turret on the map that is dry now and has ever been loaded, less <paramref name="except"/>.</summary>
        private static void DryTurrets(SimContext ctx, SimState st, HashSet<int> into, HashSet<int> except)
        {
            var d = ctx.Data;
            for (var i = 0; i < st.Machines.Count; i++)
            {
                var m = st.Machines[i];
                if (!TurretHopper.IsTurret(d, m)) continue;
                if (except != null && except.Contains(m.Id)) continue;
                if (TurretAmmo.State(d, m) != TurretAmmoState.Dry || !TurretAmmo.EverLoaded(st, m)) continue;
                into.Add(m.Id);
            }
        }

        private void Close(Tally t, RaidEndedEvent e)
        {
            _open.Remove(t);
            var line = new RaidLogLine(t.Id, e.Major, e.Scripted, e.Outcome, e.StartedAt, e.T,
                t.Spawned, t.Killed, t.PeakAlive, t.Rounds, t.WentDry.Count, t.CoreLost, t.Wrecked.Count, t.Shared);
            _lines.Add(line);
            Written?.Invoke(line);
        }
    }
}
