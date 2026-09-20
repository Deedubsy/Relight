using System;
using System.Collections.Generic;
using System.Text;

namespace Relight.Sim.UI
{
    /// <summary>One place's standing defence row: what is dry, low and wrecked there, and the sentence that says so.</summary>
    public sealed class DefenceAlertRow
    {
        /// <summary>The inbox key, <see cref="DefenceAlertSource.KeyPrefix"/> plus the place, so the row updates in place.</summary>
        public string Key;
        public string Place;
        public int Dry;
        public int Low;
        public int Wrecks;
        /// <summary>True while a raid is warned or under way: the row is danger, not a chore.</summary>
        public bool Urgent;
        public string Text;
    }

    /// <summary>
    /// E-17 (U-D-61). The ONE producer of the dry-turret and wreck notice, on the pattern of
    /// <see cref="PowerAlertSource"/> (UI_AND_ONBOARDING.md §9 defect U-3: one producer, never two). One row per
    /// place, never one per gun:
    /// <list type="bullet">
    /// <item>RAISED when a turret at the place is dry, or is low while a raid is warned or under way
    ///       (<see cref="DirectorQueries.RaidExpected"/>). A low turret in peacetime is the world badge's job.</item>
    /// <item>COUNTS the wrecks at the same place — every machine at 0 hit points, not only turrets.</item>
    /// <item>CLEARS ITSELF once the turrets are reloaded and the wrecks repaired: a row that has been raised stays
    ///       while anything dry or wrecked is left at its place. Wrecks alone never raise one.</item>
    /// <item>SILENT for a turret that has never been loaded (<see cref="TurretAmmo.EverLoaded"/>), so nothing shows
    ///       at game start or for a turret the player has just put down. An unsupplied turret is the power
    ///       alert's business and is not counted here: unpowered reads first.</item>
    /// </list>
    /// A place is a lighting district: the authored substation site nearest the machine (ALWAYS_DARK_SPEC §6, the
    /// rule <see cref="PowerGrid.SubstationOf"/> assigns streetlights by), or <see cref="FallbackPlace"/> on a map
    /// with no substation sites.
    ///
    /// "Raised" is remembered here, not in the save. After a load a dry turret raises its row again at once; a
    /// place left with wrecks only comes back silent, and its wrecks still carry their world badges.
    /// </summary>
    public sealed class DefenceAlertSource
    {
        public const string KeyPrefix = "defence:";

        /// <summary>The place name used where the region has no substation sites. Set from the loaded region.</summary>
        public string FallbackPlace = "Home";

        private readonly List<DefenceAlertRow> _rows = new List<DefenceAlertRow>();
        private readonly List<DefenceAlertRow> _scratch = new List<DefenceAlertRow>();
        private readonly HashSet<string> _raised = new HashSet<string>(StringComparer.Ordinal);
        private readonly StringBuilder _sb = new StringBuilder(64);

        /// <summary>The rows that stand now, in place-name order. Re-used between calls.</summary>
        public IReadOnlyList<DefenceAlertRow> Rows => _rows;

        /// <summary>Forget what was raised (a new session).</summary>
        public void Reset() { _rows.Clear(); _raised.Clear(); }

        /// <summary>Recount every place. Returns true while any row stands.</summary>
        public bool Refresh(SimContext ctx, SimState st)
        {
            _rows.Clear();
            if (ctx == null || st == null) { _raised.Clear(); return false; }

            var d = ctx.Data;
            var raid = DirectorQueries.RaidExpected(st);
            _scratch.Clear();
            for (var i = 0; i < st.Machines.Count; i++)
            {
                var m = st.Machines[i];
                var wreck = TurretRules.Wrecked(d, st, m);
                var ammo = TurretAmmoState.Ok;
                if (!wreck && TurretHopper.IsTurret(d, m) && Supplied(ctx, st, m))
                {
                    ammo = TurretAmmo.State(d, m);
                    if (ammo == TurretAmmoState.Dry && !TurretAmmo.EverLoaded(st, m)) ammo = TurretAmmoState.Ok;
                }
                if (!wreck && ammo == TurretAmmoState.Ok) continue;

                var row = RowFor(PlaceOf(ctx, m));
                if (wreck) row.Wrecks++;
                else if (ammo == TurretAmmoState.Dry) row.Dry++;
                else row.Low++;
            }

            for (var i = 0; i < _scratch.Count; i++)
            {
                var row = _scratch[i];
                var raise = row.Dry > 0 || (raid && row.Low > 0);
                if (raise) _raised.Add(row.Place);
                var stands = raise || (_raised.Contains(row.Place) && row.Wrecks > 0);
                if (!stands) continue;
                if (!raid) row.Low = 0;                     // peacetime: a low turret is the badge's job, not the row's
                row.Urgent = raid;
                row.Text = Sentence(row);
                _rows.Add(row);
            }
            // A place with nothing dry, low or wrecked left has cleared itself and must be raised afresh next time.
            _raised.RemoveWhere(p => !Stands(p));
            _rows.Sort((a, b) => string.CompareOrdinal(a.Place, b.Place));
            return _rows.Count > 0;
        }

        private bool Stands(string place)
        {
            for (var i = 0; i < _rows.Count; i++) if (string.Equals(_rows[i].Place, place, StringComparison.Ordinal)) return true;
            return false;
        }

        private DefenceAlertRow RowFor(string place)
        {
            for (var i = 0; i < _scratch.Count; i++)
                if (string.Equals(_scratch[i].Place, place, StringComparison.Ordinal)) return _scratch[i];
            var row = new DefenceAlertRow { Place = place, Key = KeyPrefix + place };
            _scratch.Add(row);
            return row;
        }

        /// <summary>An unpowered turret is the power alert's and the unpowered badge's to report, not this row's.</summary>
        private static bool Supplied(SimContext ctx, SimState st, Machine m) =>
            !ctx.Data.TryTurret(m.Kind, out var def) || def.PowerKw <= 0 || PowerQueries.Supplied(ctx, st, m.Id);

        /// <summary>The name of the substation site nearest the machine's centre, or <see cref="FallbackPlace"/>.</summary>
        public string PlaceOf(SimContext ctx, Machine m)
        {
            var subs = PowerGrid.SubstationSites(ctx);
            string best = null;
            var score = double.PositiveInfinity;
            var (w, h) = m.Dimensions;
            var cx = m.X + w / 2.0;
            var cy = m.Y + h / 2.0;
            for (var i = 0; i < subs.Count; i++)
            {
                var c = subs[i].Centre;
                var dd = (c.X - cx) * (c.X - cx) + (c.Y - cy) * (c.Y - cy);
                if (dd >= score || string.IsNullOrEmpty(subs[i].Name)) continue;
                score = dd;
                best = subs[i].Name;
            }
            return best ?? (string.IsNullOrEmpty(FallbackPlace) ? "Home" : FallbackPlace);
        }

        /// <summary>"Ironworks: 3 turrets dry, 1 low, 5 wrecks" — only the parts that are not zero.</summary>
        private string Sentence(DefenceAlertRow r)
        {
            _sb.Clear();
            _sb.Append(r.Place).Append(": ");
            var parts = 0;
            if (r.Dry > 0) { _sb.Append(r.Dry).Append(r.Dry == 1 ? " turret dry" : " turrets dry"); parts++; }
            if (r.Low > 0)
            {
                if (parts++ > 0) _sb.Append(", ").Append(r.Low).Append(" low");
                else _sb.Append(r.Low).Append(r.Low == 1 ? " turret low on ammunition" : " turrets low on ammunition");
            }
            if (r.Wrecks > 0)
            {
                if (parts > 0) _sb.Append(", ");
                _sb.Append(r.Wrecks).Append(r.Wrecks == 1 ? " wreck" : " wrecks");
            }
            return _sb.ToString();
        }
    }
}
