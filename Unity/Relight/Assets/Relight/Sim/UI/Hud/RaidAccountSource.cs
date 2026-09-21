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
    /// Three consequences of that rule:
    /// <list type="bullet">
    /// <item>A raid this session did not see ARRIVE — the game was loaded part-way through it — gets no account at
    ///       all. Half a tally would be a false one.</item>
    /// <item>The scripted opening encounter is skipped. The goal card already owns its "Attack repelled" text
    ///       (U-D-54), and two accounts of one fight is the duplication this pass removes elsewhere.</item>
    /// <item>The headline says "repelled" only when nothing was lost. Otherwise it says "over", which claims
    ///       nothing.</item>
    /// </list>
    ///
    /// Per session and never saved, like <c>DefenceAlertSource._raised</c>: the save schema stays at version 9.
    /// Engine-free, so <c>Tests/Sim/UI/RaidAccountTests.cs</c> can drive it without an editor.
    /// </summary>
    public sealed class RaidAccountSource
    {
        /// <summary>The inbox key, so a second raid's account replaces the first rather than stacking under it.</summary>
        public const string Key = "raid.account";

        /// <summary>Real seconds the account stays up: two lines take longer to read than a guide line.</summary>
        public const double Seconds = 20;

        /// <summary>A raid counts as "seen arriving" if this session first met it within this many sim seconds of its start.</summary>
        public const double ArrivalGraceS = 2;

        /// <summary>The finished account, "" until a watched raid has ended.</summary>
        public string Text { get; private set; } = "";

        /// <summary>True when <see cref="Text"/> reports a loss, so the row is a warning rather than a note.</summary>
        public bool Losses { get; private set; }

        /// <summary>Goes up by one each time an account is finished, so the view model posts each exactly once.</summary>
        public int Serial { get; private set; }

        /// <summary>True while a raid is being tallied.</summary>
        public bool Watching => _open;

        private bool _open;
        private int _raidId;
        private int _skipId = -1;
        private bool _major;
        private double _fired0;
        private double _coreHp;
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
            var live = UnderWay(st, out var id, out var major, out var startedAt);

            if (_open && (!live || id != _raidId))
            {
                // The raid we were watching is gone — or another one has taken its place, which also ends it.
                Close(st);
            }

            if (live && !_open && id != _skipId)
            {
                if (st.T - startedAt > ArrivalGraceS) { _skipId = id; return; }   // joined part-way: no account
                Open(ctx, st, id, major);
            }

            if (_open) WatchTurrets(ctx, st, _wentDry, _dryAtStart);
        }

        /// <summary>Tally one of this frame's events. Ignored unless a raid is being watched.</summary>
        public void Intake(SimEvent e)
        {
            if (!_open) return;
            switch (e)
            {
                case EnemyKilledEvent _: _kills++; break;
                case StructureDamagedEvent s when s.Hp <= 0: _wrecked.Add(s.MachineId); break;
                case TurretBlindEvent b: _blind.Add(b.MachineId); break;
                case PowerOutageEvent _: _outage = true; break;
                case CoreDamagedEvent c:
                    if (c.Hp < _coreHp) _coreLost += _coreHp - c.Hp;
                    _coreHp = c.Hp;
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

        private void Open(SimContext ctx, SimState st, int id, bool major)
        {
            _open = true;
            _raidId = id;
            _major = major;
            _fired0 = Fired(st);
            _coreHp = HomeQueries.CoreHp(st);
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
            WatchTurrets(ctx, st, _dryAtStart, null);
        }

        private static void WatchTurrets(SimContext ctx, SimState st, HashSet<int> into, HashSet<int> except)
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

        private static double Fired(SimState st) => st.Stats == null ? 0 : st.Stats.Fired + st.Stats.EngineerFired;

        private void Close(SimState st)
        {
            _open = false;
            var rounds = Math.Max(0, Fired(st) - _fired0);
            Losses = _wentDry.Count > 0 || _blind.Count > 0 || _outage || _wrecked.Count > 0
                     || _coreLost > 0 || _coreDisabled;

            _sb.Clear();
            _sb.Append(_major ? "Major assault " : "Raid ").Append(Losses ? "over" : "repelled");
            _sb.Append(" · ").Append(Count(_kills, "alien", "aliens")).Append(" killed");
            _sb.Append(" · ").Append(Count((int)Math.Round(rounds), "round", "rounds")).Append(" fired");
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
                else if (_coreLost > 0)
                    Clause(ref first, "core lost " + Math.Ceiling(_coreLost).ToString("0", CultureInfo.InvariantCulture) + " HP");
            }
            Text = _sb.ToString();
            Serial++;
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
