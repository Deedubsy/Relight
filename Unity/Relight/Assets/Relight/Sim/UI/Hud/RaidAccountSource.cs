using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace Relight.Sim.UI
{
    /// <summary>
    /// GP-W6: the ONE producer of the post-attack account, on the same one-producer rule as
    /// <see cref="PowerAlertSource"/> and <see cref="DefenceAlertSource"/>.
    ///
    /// The director ends a raid without a word (<c>DirectorPhase</c> just drops the record), so until this class a
    /// player who lost three machines was never told why. The account is two short lines: what happened, then what
    /// went wrong. The rule that matters is the brief's last clause — <b>only causes supported by actual events</b>.
    /// So every clause below is a count of something this class itself saw happen between the raid's arrival and
    /// its end, and a clause whose count is zero is not written. Nothing here guesses at a reason.
    ///
    /// <b>Whose events (INT-04b).</b> The tally belongs to ONE raid at ONE place. A kill counts when the dead body
    /// was this raid's (<see cref="EnemyKilledEvent.Group"/>); a camp resident shot across town during the raid is
    /// not the raid's doing. A power cut, a wreck, a blinded or dry turret and a round fired count when they are in
    /// the raid's target place: the district (<see cref="Districts"/>, INT-09a) its target stands in. On a map with
    /// no districts there is one place and everything is in it. Rounds are counted from the shot events for that
    /// reason: <c>Stats.Fired</c> is a whole-map figure.
    ///
    /// Three consequences of that rule:
    /// <list type="bullet">
    /// <item>A raid this session did not see ARRIVE — the game was loaded part-way through it — gets no account at
    ///       all. Half a tally would be a false one. "Seen arriving" means one of two things (INT-04a): this session
    ///       watched the raid WAIT (its warning, or a small raid still looking for open ground) and then saw it
    ///       start, however late the director managed to stage it; or it first met the raid within
    ///       <see cref="ArrivalGraceS"/> of its start time.</item>
    /// <item>A small raid that a major assault replaces closes with no account (INT-04a). The director turns it
    ///       back the moment the assault commits, so "repelled" would credit the defence with a retreat it did not
    ///       cause, and the assault's own account is the one the player needs.</item>
    /// <item>The scripted opening encounter is skipped. The goal card already owns its "Attack repelled" text
    ///       (U-D-54), and two accounts of one fight is the duplication this pass removes elsewhere.</item>
    /// <item>The headline says "repelled" only when it is true (INT-04c): the raid was CLEARED, the core is working
    ///       as the raid closes, and nothing was lost. A raid that was called off with bodies still alive says
    ///       "broke off", the director's own word, which credits nobody (F1-30 is open). Everything else says
    ///       "over", which claims nothing.</item>
    /// </list>
    ///
    /// <b>Where the outcome comes from (INT-04c).</b> A large raid's is read off its saved
    /// <see cref="RaidRecord"/>. A small raid has no saved record, so its outcome is worked out here by the same
    /// rule (<see cref="DirectorRules.CalledOff"/>) from what this class watched: whether its retreat was called,
    /// and whether the core was down at any look during it.
    ///
    /// Per session and never saved, like <c>DefenceAlertSource._raised</c>: nothing here adds to the save.
    /// Engine-free, so <c>Tests/Sim/UI/RaidAccountTests.cs</c> can drive it without an editor.
    /// </summary>
    public sealed class RaidAccountSource
    {
        /// <summary>The inbox key, so a second raid's account replaces the first rather than stacking under it.</summary>
        public const string Key = "raid.account";

        /// <summary>Real seconds the account stays up: two lines take longer to read than a guide line.</summary>
        public const double Seconds = 20;

        /// <summary>
        /// A raid this session never saw waiting still counts as "seen arriving" if it is first met within this many
        /// sim seconds of its start time. A raid it DID see waiting needs no grace: the director retries a small
        /// raid's staging for up to <c>SiegeTuning.MinorStageRetryS</c> without moving its start time.
        /// </summary>
        public const double ArrivalGraceS = 2;

        /// <summary>The finished account, "" until a watched raid has ended.</summary>
        public string Text { get; private set; } = "";

        /// <summary>True when <see cref="Text"/> reports a loss, so the row is a warning rather than a note.</summary>
        public bool Losses { get; private set; }

        /// <summary>Goes up by one each time an account is finished, so the view model posts each exactly once.</summary>
        public int Serial { get; private set; }

        /// <summary>
        /// How the last finished account's raid ended, as a <see cref="RaidOutcome"/> number; -1 before any account,
        /// and for a large raid that left no record.
        /// </summary>
        public int Outcome { get; private set; } = -1;

        /// <summary>True while a raid is being tallied.</summary>
        public bool Watching => _open;

        private bool _open;
        private int _raidId;
        private readonly HashSet<int> _seenWaiting = new HashSet<int>();
        private readonly HashSet<int> _done = new HashSet<int>();
        private bool _major;
        private int _place;
        private int _rounds;
        private SimContext _ctx;
        private SimState _st;
        private bool _retreat;
        private bool _coreDownAtOpen;
        private bool _coreDownSeen;
        private int _kills;
        private double _coreLost;
        private bool _coreDisabled;
        private bool _outage;
        private readonly HashSet<int> _dryAtStart = new HashSet<int>();
        private readonly HashSet<int> _wentDry = new HashSet<int>();
        private readonly HashSet<int> _wrecked = new HashSet<int>();
        private readonly HashSet<int> _blind = new HashSet<int>();
        private readonly StringBuilder _sb = new StringBuilder(160);

        /// <summary>Open a tally when a raid arrives, keep the dry-turret watch while it runs, close it when it is gone.</summary>
        public void Refresh(SimContext ctx, SimState st)
        {
            if (ctx == null || st == null || st.Director == null) return;
            _ctx = ctx;                                                 // Intake is handed bare events; these say where
            _st = st;
            NoteWaiting(st);
            var live = UnderWay(st, out var id, out var major, out var startedAt);

            if (_open && (!live || id != _raidId))
            {
                // The raid we were watching is gone, or another has taken its place. A small raid whose record is
                // still there was not beaten: a major assault committed over it and the director turned it back.
                // That one closes with no account. Anything else ended, and is reported.
                var replaced = live && !_major && st.Director.Minor != null && st.Director.Minor.Id == _raidId;
                _done.Add(_raidId);
                if (replaced) _open = false;
                else Close(st);
            }

            if (live && !_open && !_done.Contains(id))
            {
                if (!_seenWaiting.Contains(id) && st.T - startedAt > ArrivalGraceS)
                {
                    _done.Add(id);                                     // joined part-way: no account
                    return;
                }
                Open(ctx, st, id, major);
            }

            if (_open)
            {
                WatchTurrets(ctx, st, _place, _wentDry, _dryAtStart);
                if (!_major && st.Director.Minor != null && st.Director.Minor.Id == _raidId)
                    _retreat = st.Director.Minor.Retreat;
                if (EnemyCoreHook.Down(ctx, st)) _coreDownSeen = true;
            }
        }

        /// <summary>
        /// Remember a raid seen BEFORE it is on the ground: a major inside its warning, or a small raid that is
        /// announced and has no bodies yet. Only such a raid may open its tally late.
        /// </summary>
        private void NoteWaiting(SimState st)
        {
            var d = st.Director;
            if (d.Major != null && st.T < d.Major.StartsAt) _seenWaiting.Add(d.Major.Id);
            if (d.Minor != null && !d.Minor.Spawned && !d.Minor.Scripted) _seenWaiting.Add(d.Minor.Id);
        }

        /// <summary>Tally one of this frame's events. Ignored unless a raid is being watched.</summary>
        public void Intake(SimEvent e)
        {
            if (!_open) return;
            switch (e)
            {
                case EnemyKilledEvent k:
                    if (k.Layer != EnemyLayer.Site && k.Group == _raidId) _kills++;
                    break;
                case StructureDamagedEvent s when s.Hp <= 0:
                    if (Here(s.MachineId)) _wrecked.Add(s.MachineId);
                    break;
                case TurretBlindEvent b:
                    if (Here(b.MachineId)) _blind.Add(b.MachineId);
                    break;
                case PowerOutageEvent p:
                    if (Here(p.MachineId)) _outage = true;
                    break;
                case TurretShotEvent t:
                    if (Here(t.MachineId)) _rounds++;
                    break;
                case WeaponFiredEvent _:
                    if (_st != null && PlaceAt(_ctx, _st.Engineer.Pos.X, _st.Engineer.Pos.Y) == _place) _rounds++;
                    break;
                case CoreDamagedEvent c:
                    _coreLost += c.Lost;                               // the hit itself: a repair cannot hide it
                    break;
                case CoreDisabledEvent _: _coreDisabled = true; break;
            }
        }

        /// <summary>
        /// Is an attack on the ground right now? A warned raid is a countdown, not an attack (GP-W3), so a major
        /// counts from its start time and a minor from the moment its bodies are spawned.
        /// </summary>
        public static bool UnderWay(SimState st, out int id, out bool major, out double startedAt)
        {
            var d = st.Director;
            if (d.Major != null && st.T >= d.Major.StartsAt)
            {
                id = d.Major.Id; major = true; startedAt = d.Major.StartsAt;
                return true;
            }
            if (d.Minor != null && d.Minor.Spawned && !d.Minor.Scripted)
            {
                id = d.Minor.Id; major = false; startedAt = d.Minor.StartsAt;
                return true;
            }
            id = -1; major = false; startedAt = 0;
            return false;
        }

        /// <summary>
        /// The place a position is in: its district's index, or -1 on a map with no districts, where everything
        /// is one place.
        /// </summary>
        private static int PlaceAt(SimContext ctx, double x, double y) => Districts.Of(ctx).IndexAt(x, y);

        private static int PlaceOf(SimContext ctx, Machine m)
        {
            var (w, h) = m.Dimensions;
            return PlaceAt(ctx, m.X + w / 2.0, m.Y + h / 2.0);
        }

        /// <summary>The place this raid is aimed at: where its target stands. With no target left, the whole map.</summary>
        private static int TargetPlace(SimContext ctx, SimState st) =>
            DirectorRules.Target(ctx, st, out var x, out var y, out var size)
                ? PlaceAt(ctx, x + size / 2.0, y + size / 2.0)
                : -1;

        /// <summary>Is this machine in the raid's place? A machine that is no longer on the map is not counted.</summary>
        private bool Here(int machineId)
        {
            if (_st == null) return false;
            var m = _st.MachineById(machineId);
            return m != null && (_place < 0 || PlaceOf(_ctx, m) == _place);
        }

        private void Open(SimContext ctx, SimState st, int id, bool major)
        {
            _open = true;
            _raidId = id;
            _major = major;
            _place = TargetPlace(ctx, st);
            _rounds = 0;
            _retreat = false;
            _coreDownAtOpen = EnemyCoreHook.Down(ctx, st);
            _coreDownSeen = _coreDownAtOpen;
            _kills = 0;
            _coreLost = 0;
            _coreDisabled = false;
            _outage = false;
            _dryAtStart.Clear();
            _wentDry.Clear();
            _wrecked.Clear();
            _blind.Clear();
            // A turret that was already empty when they arrived did not "run dry" in this fight; the defence row
            // (E-17) had been saying so for as long as it stood.
            WatchTurrets(ctx, st, _place, _dryAtStart, null);
        }

        private static void WatchTurrets(SimContext ctx, SimState st, int place, HashSet<int> into, HashSet<int> except)
        {
            var d = ctx.Data;
            for (var i = 0; i < st.Machines.Count; i++)
            {
                var m = st.Machines[i];
                if (!TurretHopper.IsTurret(d, m)) continue;
                if (place >= 0 && PlaceOf(ctx, m) != place) continue;
                if (except != null && except.Contains(m.Id)) continue;
                if (TurretAmmo.State(d, m) != TurretAmmoState.Dry || !TurretAmmo.EverLoaded(st, m)) continue;
                into.Add(m.Id);
            }
        }

        private void Close(SimState st)
        {
            _open = false;
            var coreDown = EnemyCoreHook.Down(_ctx, st);
            if (coreDown) _coreDownSeen = true;
            Outcome = Ended(st);
            Losses = _wentDry.Count > 0 || _blind.Count > 0 || _outage || _wrecked.Count > 0
                     || _coreLost > 0 || _coreDisabled || _coreDownAtOpen || coreDown;

            var head = Outcome == (int)RaidOutcome.BrokeOff ? "broke off"
                : Outcome == (int)RaidOutcome.Cleared && !Losses ? "repelled"
                : "over";
            _sb.Clear();
            _sb.Append(_major ? "Major assault " : "Raid ").Append(head);
            _sb.Append(" · ").Append(Count(_kills, "alien", "aliens")).Append(" killed");
            _sb.Append(" · ").Append(Count(_rounds, "round", "rounds")).Append(" fired");
            if (!Losses) _sb.Append(" · nothing lost");
            else
            {
                _sb.Append('\n');
                var first = true;
                if (_wentDry.Count > 0) Clause(ref first, Count(_wentDry.Count, "turret", "turrets") + " ran dry");
                if (_blind.Count > 0) Clause(ref first, Count(_blind.Count, "turret", "turrets") + " hit from the dark");
                if (_outage) Clause(ref first, "power failed");
                if (_wrecked.Count > 0) Clause(ref first, Count(_wrecked.Count, "structure", "structures") + " wrecked");
                if (_coreDisabled) Clause(ref first, "the core was disabled");
                else if (_coreDownAtOpen) Clause(ref first, "the core was already down");
                else if (coreDown) Clause(ref first, "the core is down");
                else if (_coreLost > 0)
                    Clause(ref first, "core lost " + Math.Ceiling(_coreLost).ToString("0", CultureInfo.InvariantCulture) + " HP");
            }
            Text = _sb.ToString();
            Serial++;
        }

        /// <summary>
        /// How the watched raid ended. A large raid: its saved record, or -1 if the director dropped it without
        /// one. A small raid keeps no record, so: cleared unless its retreat was called, and then the director's
        /// own called-off rule, with "the core is down" widened to "was down at any look", because a core put
        /// back while the raiders were still walking off does not turn a lost raid into one that broke off.
        /// </summary>
        private int Ended(SimState st)
        {
            if (_major)
            {
                var h = st.Director.History;
                for (var i = h.Count - 1; i >= 0; i--) if (h[i].Id == _raidId) return h[i].Outcome;
                return -1;
            }
            if (!_retreat) return (int)RaidOutcome.Cleared;
            return (int)(_coreDownSeen ? RaidOutcome.Lost : DirectorRules.CalledOff(_ctx, st));
        }

        private void Clause(ref bool first, string text)
        {
            if (!first) _sb.Append(" · ");
            _sb.Append(text);
            first = false;
        }

        private static string Count(int n, string one, string many) =>
            n.ToString(CultureInfo.InvariantCulture) + " " + (n == 1 ? one : many);
    }
}
