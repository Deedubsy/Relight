using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace Relight.Sim.UI
{
    /// <summary>One actionable machine problem the HUD can show, already worded.</summary>
    public readonly struct HudProblem
    {
        /// <summary>The first machine the row speaks for.</summary>
        public readonly int MachineId;
        public readonly MachineOperatingState State;
        /// <summary>"Generator: out of fuel — put coal in the generator", or "3 × Assembler: …" for several.</summary>
        public readonly string Text;
        /// <summary>How many machines the row speaks for (GP-W6: one row per kind and state, not one per machine).</summary>
        public readonly int Count;

        public HudProblem(int machineId, MachineOperatingState state, string text, int count = 1)
        {
            MachineId = machineId; State = state; Text = text; Count = count;
        }
    }

    /// <summary>
    /// C-07's view model: the HUD as plain strings, with no VisualElement, no MonoBehaviour and no UnityEngine
    /// reference at all. It lives in <c>Relight.Sim</c> (namespace <c>Relight.Sim.UI</c>, the existing home of
    /// engine-free UI logic — <c>SaveRowFormatter</c>, <c>WorkshopText</c>, <c>FrontEndText</c>) for one reason:
    /// <c>Tests/Sim/UI/HudViewModelTests.cs</c> compiles in an assembly that references only <c>Relight.Sim</c>,
    /// so a view model in <c>Relight.UI</c> would be untestable. <c>HudController</c> in
    /// <c>Assets/Relight/UI/Hud/</c> owns the elements and copies these strings into them.
    ///
    /// Content is the reference HUD (<c>packages/game/src/hud.ts</c>, 101 lines) as
    /// UI_AND_ONBOARDING.md §4 tabulates it, and the refresh throttle is the reference's own
    /// <c>if (now - last &lt; 150) return</c> (hud.ts:47), the same 150 ms
    /// <c>StatusPanelViewModel.RefreshSeconds</c> uses.
    ///
    /// Every number here comes from a selector — <see cref="PowerQueries"/>, <see cref="HomeQueries"/>,
    /// <see cref="WeaponQueries"/>, <see cref="MiningQueries"/>, <see cref="DirectorQueries"/>,
    /// <see cref="OpeningQueries"/>, <see cref="InventoryQueries"/> — so the HUD can never state a fact the
    /// simulation does not hold.
    /// </summary>
    public sealed class HudViewModel
    {
        /// <summary>Reference hud.ts:47 — the HUD's refresh throttle, in real seconds.</summary>
        public const double RefreshSeconds = 0.150;

        /// <summary>UI_AND_ONBOARDING.md §4: the lock prompt's suffix, exactly. Since U-D-44 the only thing
        /// that locks the engineer at Home is a running core repair — a workshop job never does.</summary>
        public const string HandLockSuffix = " · Escape cancels";

        /// <summary>The notice key a finished workshop batch updates, so ten batches are one row, not ten.</summary>
        public const string WorkshopNoticeKey = "workshop:done";

        private readonly StringBuilder _sb = new StringBuilder(96);
        private readonly List<HudProblem> _problems = new List<HudProblem>();
        private double _lastRefresh = double.NegativeInfinity;
        private bool _brownout;
        private bool _lowFuel;
        private int _accountPosted;
        private GameData _data;
        private readonly Dictionary<ItemId, (int n, double at)> _mined = new Dictionary<ItemId, (int n, double at)>();
        private readonly List<ProblemGroup> _groups = new List<ProblemGroup>();
        private bool _taughtHesitation;
        private bool _taughtBlindTurret;

        /// <summary>The single power-alert producer (defect U-3). Nothing else may raise one.</summary>
        public readonly PowerAlertSource Power = new PowerAlertSource();

        /// <summary>The single dry-turret and wreck producer (E-17, U-D-61), on the same one-producer rule.</summary>
        public readonly DefenceAlertSource Defence = new DefenceAlertSource();

        // What each defence row last said, so a standing row is posted on a change and never per refresh (U-D-55).
        private readonly Dictionary<string, string> _defencePosted = new Dictionary<string, string>(StringComparer.Ordinal);
        private readonly List<string> _defenceGone = new List<string>();

        /// <summary>The single post-attack account producer (GP-W6).</summary>
        public readonly RaidAccountSource Account = new RaidAccountSource();

        /// <summary>The keyed alert inbox: one urgent strip plus at most three transient rows (§8).</summary>
        public readonly HudNotices Notices = new HudNotices();

        // ---- status strip (top centre, content width) --------------------------------------------------------

        /// <summary>Elapsed play time, "H:MM:SS" (U-D-58: there is no sun, so there are no days to count).</summary>
        public string Clock { get; private set; } = "0:00:00";

        /// <summary>"In light" or "In the dark", from the sim's lit mask at the engineer's tile (ALWAYS_DARK_SPEC.md §3).</summary>
        public string LightText { get; private set; } = "";
        public bool InDark { get; private set; }

        /// <summary>"Power 300 kW · Need 40", with " · short" while demand is above generation, or one of
        /// <see cref="PowerAlertSource"/>'s three sentences. The HUD prints this and makes no text of its own.</summary>
        public string PowerText { get; private set; } = PowerAlertSource.NoSourceText;

        /// <summary>True while machines are being slowed by a shortage: the strip's standing amber state.</summary>
        public bool PowerShort { get; private set; }

        /// <summary>True while nothing is being generated: the strip's standing red state.</summary>
        public bool PowerOff { get; private set; } = true;

        /// <summary>0..1 of demand actually delivered, for the strip's meter.</summary>
        public double PowerFraction { get; private set; }

        /// <summary>"Fuel · about 4 min at this load (estimate)", or "" when nothing is burning.</summary>
        public string FuelText { get; private set; } = "";

        /// <summary>True while <see cref="FuelText"/> is under the low-fuel line.</summary>
        public bool FuelLow { get; private set; }

        /// <summary>"Home core · 500/500 HP", "" before the core is placed.</summary>
        public string Core { get; private set; } = "";

        /// <summary>0..1 of the core's hit points, for a meter.</summary>
        public double CoreFraction { get; private set; }

        /// <summary>True while the core is at 0 HP: the HUD paints the core line as danger.</summary>
        public bool CoreDisabled { get; private set; }

        // ---- bottom left -------------------------------------------------------------------------------------

        /// <summary>"Engineer · 48/50 HP" or "Engineer down · 6 s" (§4 table).</summary>
        public string Engineer { get; private set; } = "";

        /// <summary>0..1 of the engineer's hit points.</summary>
        public double EngineerFraction { get; private set; }

        /// <summary>"Rifle · 12 / 20 bullets", "Reloading" while it reloads, "" with nothing equipped.</summary>
        public string Weapon { get; private set; } = "";

        /// <summary>0 when not reloading, else 0..1 through the reload, for a meter.</summary>
        public double ReloadFraction { get; private set; }

        /// <summary>"74 bullets carried" — the reserve the Backpack can still feed (§4).</summary>
        public string Ammo { get; private set; } = "";

        /// <summary>"Backpack 12/40".</summary>
        public string Backpack { get; private set; } = "";

        // ---- mining ------------------------------------------------------------------------------------------

        /// <summary>False while a menu is open, or when the hands are not digging (brief C-07).</summary>
        public bool MiningVisible { get; private set; }

        /// <summary>"Mining coal".</summary>
        public string MiningTitle { get; private set; } = "";

        /// <summary>0..1 towards the next whole item.</summary>
        public double MiningFraction { get; private set; }

        /// <summary>"12 left · 0.5/s" or "Backpack full".</summary>
        public string MiningDetail { get; private set; } = "";

        // ---- prompts, threat, objective ----------------------------------------------------------------------

        /// <summary>"Repairing — Cancel to move. · Escape cancels" while the engineer is pinned, else "".</summary>
        public string HandLock { get; private set; } = "";

        /// <summary>The upper-right threat line, or "" when nothing is coming.</summary>
        public string Threat { get; private set; } = "";

        /// <summary>True while the threat line describes something already attacking.</summary>
        public bool ThreatUrgent { get; private set; }

        // W-B R1 (correction pass): the one-line objective MIRROR is gone. The owner saw the objective twice,
        // once in the HUD strip and once on the goal card, and the card is the one that can carry the materials
        // and the "Why this next?" detail. OpeningQueries.Objective is now read only by GoalCardViewModel.

        /// <summary>The one urgent strip: the power outage today, "" when there is none.</summary>
        public string Alert { get; private set; } = "";

        /// <summary>Actionable machine problems, worst first, at most <see cref="MaxProblems"/>.</summary>
        public IReadOnlyList<HudProblem> Problems => _problems;

        /// <summary>How many machine problems the strip will list before it stops (the rest are in inspection).</summary>
        public const int MaxProblems = 3;

        /// <summary>Completed refreshes, so a test can prove the throttle does something.</summary>
        public int Refreshes { get; private set; }

        /// <summary>
        /// Recompute if the throttle allows. <paramref name="now"/> is unscaled real time (never sim time) so the
        /// throttle and the notice timers keep running while the game is paused.
        /// Returns true when the properties changed.
        /// </summary>
        public bool Refresh(SimContext ctx, SimState st, double now, bool paused, bool menuOpen, bool force = false)
        {
            if (!force && now - _lastRefresh < RefreshSeconds) return false;
            _lastRefresh = now;
            Refreshes++;

            if (ctx == null || st == null)
            {
                Clock = "No session";
                PowerText = PowerAlertSource.NoSourceText;
                PowerShort = false; PowerOff = true; PowerFraction = 0; FuelText = ""; FuelLow = false;
                Core = Engineer = Weapon = Ammo = Backpack = HandLock = Threat = Alert = "";
                LightText = ""; InDark = false;
                MiningVisible = false;
                _problems.Clear();
                Notices.Reap(now);
                return true;
            }

            var d = ctx.Data;
            _data = d;

            // --- status strip ---------------------------------------------------------------------------------
            Clock = FormatClock(st.T, paused);
            var at = WorldQueries.Engineer(ctx, st).Pos;
            InDark = !LightQueries.LitAt(st, (int)Math.Floor(at.X), (int)Math.Floor(at.Y));
            LightText = InDark ? "In the dark" : "In light";

            Power.Refresh(ctx, st);
            PowerText = Power.StripText;
            PowerShort = Power.Short;
            PowerOff = Power.Off;
            PowerFraction = Power.Delivered;
            FuelText = Power.FuelText.Length > 0 ? "Fuel · " + Power.FuelText + " at this load (estimate)" : "";
            FuelLow = Power.LowFuelText.Length > 0;
            // U-D-55. An outage is a STANDING state, not an event, so it lives in the urgent strip and NOWHERE
            // else. It used to be posted into the notice inbox as well, with an infinite lifetime, on every
            // 150 ms refresh — and <see cref="HudNotices.Post"/> counts a repeat whenever the row it replaces is
            // still live, so the toast beside the strip churned "No power · Home × 47" several times a second,
            // saying a second time what the strip above it already said.
            Alert = Power.AlertText;
            Notices.Clear(PowerAlertSource.AlertKey);
            // L-02 (§5.7): a brownout is posted ONCE, when it starts, and cleared when it ends — never re-posted per
            // refresh, which is the repeat-count churn U-D-55 removed for the outage.
            var brownout = Power.BrownoutText.Length > 0;
            if (brownout && !_brownout)
                Notices.Post(PowerAlertSource.BrownoutKey, Power.BrownoutText, HudNoticeKind.Warning, now, GuideSeconds);
            if (!brownout && _brownout) Notices.Clear(PowerAlertSource.BrownoutKey);
            _brownout = brownout;
            // GP-W6: low fuel is a standing state on the same edge rule. The sentence does not count down, so it
            // is posted once; a player who dismisses it has been told, and the strip's fuel line keeps the time.
            if (FuelLow && !_lowFuel)
                Notices.Post(PowerAlertSource.LowFuelKey, Power.LowFuelText, HudNoticeKind.Warning, now, double.PositiveInfinity);
            if (!FuelLow && _lowFuel) Notices.Clear(PowerAlertSource.LowFuelKey);
            _lowFuel = FuelLow;

            // E-17 (U-D-61): one standing row per place for dry turrets and wrecks, from the one producer.
            Defence.FallbackPlace = Power.PlaceName;
            Defence.Refresh(ctx, st);
            PostDefence(now);

            // GP-W6: the post-attack account, posted once when the raid it watched is over.
            Account.Refresh(ctx, st);
            if (Account.Serial != _accountPosted)
            {
                _accountPosted = Account.Serial;
                Notices.Post(RaidAccountSource.Key, Account.Text,
                    Account.Losses ? HudNoticeKind.Warning : HudNoticeKind.Info, now, RaidAccountSource.Seconds);
            }

            var maxCore = d.Defence != null ? d.Defence.CoreHp : 0;
            var hp = HomeQueries.CoreHp(st);
            if (st.Home != null && st.Home.Placed)
            {
                CoreDisabled = hp <= 0;
                CoreFraction = maxCore > 0 ? Clamp01(hp / maxCore) : 0;
                Core = CoreDisabled
                    ? "Home core · DISABLED"
                    : string.Format(CultureInfo.InvariantCulture, "Home core · {0:0}/{1:0} HP", Math.Ceiling(hp), maxCore);
            }
            else { Core = ""; CoreFraction = 0; CoreDisabled = false; }

            // --- bottom left ----------------------------------------------------------------------------------
            var e = WorldQueries.Engineer(ctx, st);
            if (e.IsDown)
            {
                var left = Math.Max(0, e.UpAt - st.T);
                Engineer = string.Format(CultureInfo.InvariantCulture, "Engineer down · {0:0} s", Math.Ceiling(left));
                EngineerFraction = 0;
            }
            else
            {
                Engineer = string.Format(CultureInfo.InvariantCulture, "Engineer · {0:0}/{1:0} HP", Math.Ceiling(e.Hp), e.MaxHp);
                EngineerFraction = e.MaxHp > 0 ? Clamp01(e.Hp / e.MaxHp) : 0;
            }

            var w = WeaponQueries.Equipped(st, d);
            if (!w.Equipped)
            {
                Weapon = "";
                Ammo = "";
                ReloadFraction = 0;
            }
            else
            {
                _sb.Clear();
                _sb.Append(w.DisplayName);
                // §5.7 / §7.5 correction 7.0-b: the player-facing word is always "bullets", never "magazine".
                _sb.AppendFormat(CultureInfo.InvariantCulture, " · {0:0} / {1:0} bullets", w.Loaded, w.Capacity);
                if (w.Reloading) _sb.Append(" · reloading");
                Weapon = _sb.ToString();
                ReloadFraction = w.Reloading ? Clamp01(w.ReloadFraction) : 0;
                Ammo = string.Format(CultureInfo.InvariantCulture, "{0:0} bullets carried", w.Reserve);
            }

            var (used, cap) = InventoryQueries.Capacity(ctx, st);
            Backpack = string.Format(CultureInfo.InvariantCulture, "Backpack {0}/{1}", used, cap);

            // --- mining ---------------------------------------------------------------------------------------
            var mine = MiningQueries.Progress(ctx, st);
            MiningVisible = mine.Active && !menuOpen;   // brief C-07: hidden while a menu is open
            if (MiningVisible)
            {
                MiningTitle = "Mining " + ItemName(d, mine.Item);
                MiningFraction = Clamp01(mine.Fraction);
                MiningDetail = mine.Full
                    ? "Backpack full"
                    : string.Format(CultureInfo.InvariantCulture, "{0:0} left · {1:0.##}/s", Math.Ceiling(mine.UnitsLeft), mine.RatePerS);
            }
            else { MiningTitle = ""; MiningDetail = ""; MiningFraction = 0; }

            // --- prompts --------------------------------------------------------------------------------------
            HandLock = HandCraft.HandLocked(st) ? HandCraft.LockTextFor(st) + HandLockSuffix : "";

            // --- threat ---------------------------------------------------------------------------------------
            Threat = ThreatLine(ctx, st, out var urgent);
            ThreatUrgent = urgent;

            // --- machine problems -----------------------------------------------------------------------------
            CollectProblems(ctx, st);

            Notices.Reap(now);
            return true;
        }

        /// <summary>Notice key prefix for a queued command's result; the suffix is the command's type name.</summary>
        public const string CommandNoticeKey = "cmd:";

        /// <summary>Notice key for a dig that stopped with a reason.</summary>
        public const string MiningNoticeKey = "mining";

        /// <summary>Real seconds a refusal stays on screen.</summary>
        public const double RefusalSeconds = 4;

        /// <summary>Real seconds an accepted note stays on screen — shorter: nothing has to be acted on.</summary>
        public const double NoteSeconds = 3;

        /// <summary>Real seconds a once-only guide line stays on screen — long enough to read a whole sentence.</summary>
        public const double GuideSeconds = 10;

        /// <summary>ALWAYS_DARK_SPEC §5.6: shown the first time a group of attackers hesitates at a lit edge.</summary>
        public const string HesitationLine = "Aliens avoid light. They pause at its edge and look for a dark way in.";

        /// <summary>ALWAYS_DARK_SPEC §5.7, verbatim: shown the first time a turret is hit by something it cannot see.</summary>
        public const string BlindTurretLine = "This turret can't see into the dark. Light the ground it guards.";

        public const string HesitationKey = "guide:light-hesitation";
        public const string BlindTurretKey = "guide:blind-turret";
        public const string DistrictLitKey = "light:district";

        /// <summary>
        /// Put the defence rows into the inbox. A row is a STANDING state (U-D-55), so it is posted only when what
        /// it says changes — its sentence, or whether it is danger — and cleared the refresh its place clears.
        /// Danger while a raid is warned or under way and a turret there is dry: §8's "dismissal never clears a
        /// live danger". Otherwise a warning, which the player may dismiss and which stays dismissed.
        /// </summary>
        private void PostDefence(double now)
        {
            var rows = Defence.Rows;
            _defenceGone.Clear();
            foreach (var key in _defencePosted.Keys)
            {
                var stands = false;
                for (var i = 0; i < rows.Count && !stands; i++) stands = string.Equals(rows[i].Key, key, StringComparison.Ordinal);
                if (!stands) _defenceGone.Add(key);
            }
            for (var i = 0; i < _defenceGone.Count; i++)
            {
                Notices.Clear(_defenceGone[i]);
                _defencePosted.Remove(_defenceGone[i]);
            }

            for (var i = 0; i < rows.Count; i++)
            {
                var r = rows[i];
                var danger = r.Urgent && r.Dry > 0;
                var says = danger ? "!" + r.Text : r.Text;
                if (_defencePosted.TryGetValue(r.Key, out var was) && string.Equals(was, says, StringComparison.Ordinal)) continue;
                Notices.Post(r.Key, r.Text, danger ? HudNoticeKind.Danger : HudNoticeKind.Warning, now, double.PositiveInfinity);
                _defencePosted[r.Key] = says;
            }
        }

        /// <summary>
        /// C-13. Turn this frame's events into notices, so what the SIM decided about a queued command reaches the
        /// player. The reference toasts every dispatch result (worldScene.ts
        /// <c>hooks.onToast(r.reason, r.ok ? 'good' : 'bad')</c>); here a refusal is a
        /// <see cref="HudNoticeKind.Warning"/> row and an accepted note an <see cref="HudNoticeKind.Info"/> one.
        ///
        /// The key is the COMMAND TYPE, not the text: a placement refused once a frame while the button is held
        /// updates one row with a repeat count rather than stacking three identical toasts (§8).
        ///
        /// <paramref name="now"/> is unscaled real time, like every other timer here, so a paused game does not
        /// freeze a toast on screen. Engine-free: the caller passes <c>SimHost.LastFrameEvents</c>.
        /// </summary>
        public void Intake(IReadOnlyList<SimEvent> events, double now)
        {
            if (events == null) return;
            for (var i = 0; i < events.Count; i++)
            {
                Account.Intake(events[i]);
                switch (events[i])
                {
                    case CommandResultEvent c:
                        if (string.IsNullOrEmpty(c.Text)) break;
                        Notices.Post(CommandNoticeKey + c.Command, c.Text,
                            c.Accepted ? HudNoticeKind.Info : HudNoticeKind.Warning, now,
                            c.Accepted ? NoteSeconds : RefusalSeconds);
                        break;
                    // A dig the sim called off — a full Backpack, the tile ran out — is otherwise silent: mining
                    // is a held intent, so the stop never returns a result to anyone.
                    // GP-W6. This row used to print the item's KEY ("+1 iron-ore to Backpack") and, posted once per
                    // unit with the same text, let the inbox count repeats onto it ("… × 14"). It now names the item
                    // as every other surface does and keeps its own running total for as long as the dig goes on.
                    case MinedEvent gained when gained.MachineId < 0:
                        _mined.TryGetValue(gained.Item, out var run);
                        var total = run.n > 0 && now - run.at <= NoteSeconds ? run.n + 1 : 1;
                        _mined[gained.Item] = (total, now);
                        Notices.Post("mined:" + Items.Key(gained.Item),
                            "+" + total.ToString(CultureInfo.InvariantCulture) + " " + ItemTitle(_data, gained.Item) + " to Backpack",
                            HudNoticeKind.Info, now, NoteSeconds);
                        break;
                    case MiningStoppedEvent m:
                        // Releasing mining is shown by the target card; keep real refusal notices intact.
                        if (string.IsNullOrEmpty(m.Reason)) break;
                        Notices.Post(MiningNoticeKey, m.Reason, HudNoticeKind.Warning, now, RefusalSeconds);
                        break;
                    // L-02 (§5.6): one line, the first time attackers baulk at a lit edge. A camp resident turning
                    // back on its beat is not "a group of attackers", so it does not spend the line.
                    case LightHesitationEvent h:
                        if (_taughtHesitation || h.Layer == EnemyLayer.Site) break;
                        _taughtHesitation = true;
                        Notices.Post(HesitationKey, HesitationLine, HudNoticeKind.Info, now, GuideSeconds);
                        break;
                    // L-02 (§5.7): one line, the first time a turret is hit from the dark. The badge repeats it.
                    case TurretBlindEvent _:
                        if (_taughtBlindTurret) break;
                        _taughtBlindTurret = true;
                        Notices.Post(BlindTurretKey, BlindTurretLine, HudNoticeKind.Warning, now, GuideSeconds);
                        break;
                    // L-02 (§5.4): the moment that replaces dawn gets a line as well as its sweep and cue.
                    case DistrictLitEvent lit:
                        Notices.Post(DistrictLitKey,
                            lit.Name + " connected · " + lit.Lights.ToString(CultureInfo.InvariantCulture)
                            + (lit.Lights == 1 ? " streetlight on" : " streetlights on"),
                            HudNoticeKind.Info, now, GuideSeconds);
                        break;
                    // U-D-44: the workshop finishes batches while the player is anywhere else, so the HUD is the
                    // only place that can say so. One row, updated in place, naming where the goods are waiting.
                    case WorkshopFinishedEvent w:
                        Notices.Post(WorkshopNoticeKey,
                            w.DisplayName + " finished at the Home workshop"
                            + (w.Remaining > 0
                                ? " · " + w.Remaining.ToString(CultureInfo.InvariantCulture) + " batches to go"
                                : " · waiting in the output tray"),
                            HudNoticeKind.Info, now, NoteSeconds);
                        break;
                }
            }
        }

        // ------------------------------------------------------------------------------------------------------

        /// <summary>
        /// U-D-58: there is no sun, so the clock counts elapsed play time instead of days, "H:MM:SS". The
        /// canonical implementation: <c>Relight.UI</c>'s <c>StatusPanelViewModel.FormatClock</c> is a
        /// character-for-character copy that predates this file, and the wave-3 W-B report carries the one-line
        /// patch that makes it delegate here so the two can never drift.
        /// </summary>
        public static string FormatClock(double t, bool paused)
        {
            if (double.IsNaN(t) || t < 0) t = 0;
            var s = (long)Math.Floor(t);
            return string.Format(CultureInfo.InvariantCulture, "{0}:{1:00}:{2:00}{3}",
                s / 3600, s / 60 % 60, s % 60, paused ? " · Paused" : "");
        }

        /// <summary>
        /// The upper-right threat line. The director owns the live raid (<see cref="DirectorQueries.Warning"/>);
        /// the opening encounter owns the scripted first arrival (<see cref="OpeningQueries.Encounter"/>) and takes
        /// precedence while it is pending, because §12.6's truthfulness rule allows exactly one announced attack.
        /// Wording: §7.4, "Small enemy group approaching from the {dir} · Arrives in N s".
        /// </summary>
        public static string ThreatLine(SimContext ctx, SimState st, out bool urgent)
        {
            urgent = false;
            if (ctx == null || st == null) return "";

            var o = OpeningQueries.Encounter(ctx, st);
            if (o.Status == OpeningStatus.Scheduled && o.SecondsLeft > 0)
            {
                var dir = string.IsNullOrEmpty(o.Direction) ? "·" : o.Direction;
                return string.Format(CultureInfo.InvariantCulture,
                    "Small enemy group approaching from the {0} · Arrives in {1} s", dir, o.SecondsLeft);
            }
            if (o.Status == OpeningStatus.Active)
            {
                urgent = true;
                return "Small enemy group attacking";
            }

            var wv = DirectorQueries.Warning(ctx, st);
            if (string.IsNullOrEmpty(wv.Kind)) return "";
            if (wv.SecondsLeft > 0)
                return string.Format(CultureInfo.InvariantCulture,
                    "{0} approaching from the {1} · Arrives in {2:0} s",
                    string.IsNullOrEmpty(wv.Label) ? "Small enemy group" : wv.Label,
                    string.IsNullOrEmpty(wv.Direction) ? "·" : wv.Direction, Math.Ceiling(wv.SecondsLeft));
            urgent = true;
            return string.IsNullOrEmpty(wv.Notice) ? "Base under attack" : wv.Notice;
        }

        /// <summary>One row's worth of machines: the same kind in the same state, or one of the two circuit-wide rows.</summary>
        private struct ProblemGroup
        {
            public string Kind;
            public MachineOperatingState State;
            public bool Unsupplied;
            public int Count;
            public int FirstId;
            public int Rank;
        }

        /// <summary>The remedy for machines that ARE on a circuit which is generating nothing: a pole is not the answer.</summary>
        public const string UnsuppliedRemedy = "link and fuel a Generator";

        /// <summary>The remedy for a shortage. There are only two ways out of one.</summary>
        public const string ThrottledRemedy = "add a Generator or remove load";

        /// <summary>
        /// Machine problems worth acting on, worst first. The reason word is
        /// <see cref="ProductionQueries.StateText"/> verbatim — the brief forbids a second vocabulary — and the
        /// remedy clause after the dash is §8's actionable half.
        ///
        /// GP-W6 made the two promises in that first sentence true. The list used to be the first three machines in
        /// placement order, one row each, so three unpowered belts-worth of Assemblers hid the wreck behind them.
        /// Now machines of one kind in one state share a row ("3 × Assembler: …"), rows are ranked by
        /// <see cref="Rank"/>, and the two CIRCUIT-wide conditions get one row each however many machines they
        /// touch: connected machines whose circuit generates nothing, and machines slowed by a shortage — the
        /// brief's "power shortages visibly affecting the machines they constrain".
        /// </summary>
        private void CollectProblems(SimContext ctx, SimState st)
        {
            _problems.Clear();
            _groups.Clear();
            var d = ctx.Data;
            for (var i = 0; i < st.Machines.Count; i++)
            {
                var m = st.Machines[i];
                var s = ProductionQueries.OperatingState(ctx, st, m.Id);
                var throttled = s == MachineOperatingState.Throttled;
                if (!throttled && Remedy(s) == null) continue;
                var unsupplied = s == MachineOperatingState.Unpowered && PowerQueries.Connected(ctx, st, m.Id);
                var kind = throttled || unsupplied ? "" : m.Kind;

                var at = -1;
                for (var g = 0; g < _groups.Count && at < 0; g++)
                    if (_groups[g].State == s && _groups[g].Unsupplied == unsupplied
                        && string.Equals(_groups[g].Kind, kind, StringComparison.Ordinal)) at = g;
                if (at < 0)
                {
                    _groups.Add(new ProblemGroup { Kind = kind, State = s, Unsupplied = unsupplied, Count = 1, FirstId = m.Id, Rank = Rank(s) });
                    continue;
                }
                var grp = _groups[at];
                grp.Count++;
                _groups[at] = grp;
            }

            // Worst first; within a rank the larger group, then the older machine, so the order never flickers.
            _groups.Sort((a, b) => a.Rank != b.Rank ? a.Rank.CompareTo(b.Rank)
                : a.Count != b.Count ? b.Count.CompareTo(a.Count) : a.FirstId.CompareTo(b.FirstId));

            for (var i = 0; i < _groups.Count && _problems.Count < MaxProblems; i++)
            {
                var g = _groups[i];
                var many = g.Count.ToString(CultureInfo.InvariantCulture) + (g.Count == 1 ? " machine" : " machines");
                string text;
                if (g.State == MachineOperatingState.Throttled)
                    text = "Low power: " + many + " " + ProductionQueries.StateText(g.State) + " — " + ThrottledRemedy;
                else if (g.Unsupplied)
                    text = "No supply: " + many + " connected, " + ProductionQueries.StateText(g.State) + " — " + UnsuppliedRemedy;
                else
                {
                    var name = d.TryMachine(g.Kind, out var spec) && !string.IsNullOrEmpty(spec.DisplayName)
                        ? spec.DisplayName : Title(g.Kind);
                    text = (g.Count > 1 ? g.Count.ToString(CultureInfo.InvariantCulture) + " × " : "") + name + ": "
                           + ProductionQueries.StateText(g.State) + " — " + Remedy(g.State);
                }
                _problems.Add(new HudProblem(g.FirstId, g.State, text, g.Count));
            }
        }

        /// <summary>
        /// Worst first. A wreck does nothing until someone walks to it; an empty Generator stops everything behind
        /// it; then the machines with no power, the ones being slowed, and last the two routine stalls.
        /// </summary>
        public static int Rank(MachineOperatingState s)
        {
            switch (s)
            {
                case MachineOperatingState.Disabled: return 0;
                case MachineOperatingState.OutOfFuel: return 1;
                case MachineOperatingState.Unpowered: return 2;
                case MachineOperatingState.Throttled: return 3;
                case MachineOperatingState.NoInput: return 4;
                case MachineOperatingState.OutputFull: return 5;
                default: return 9;
            }
        }

        /// <summary>The actionable half of a problem line; null for a state the player need not act on.</summary>
        public static string Remedy(MachineOperatingState s)
        {
            switch (s)
            {
                case MachineOperatingState.OutOfFuel: return "put coal in the generator";
                case MachineOperatingState.Unpowered: return "connect it to a powered pole";
                case MachineOperatingState.OutputFull: return "empty the storage";
                case MachineOperatingState.NoInput: return "bring it materials";
                // GP-W5 corrected this. A machine is repaired AT THE MACHINE — HomeCore.RepairProblem asks for
                // reach of its own footprint — and the Home workshop card is the CORE's, which is not a machine
                // and never appears in this list. The old text sent the player to the one place that cannot fix it.
                case MachineOperatingState.Disabled: return "walk to it and repair it";
                // Running and idle are not problems. Throttled is one, but it is a circuit's problem rather than a
                // machine's, so CollectProblems gives it a single row of its own (ThrottledRemedy) instead of a remedy here.
                default: return null;
            }
        }

        private static double Clamp01(double v) => v < 0 ? 0 : v > 1 ? 1 : v;

        /// <summary>The item's display name. A key never reaches the player: with no data it is at least made to read as words.</summary>
        private static string ItemTitle(GameData d, ItemId id)
        {
            var def = d?.Item(id);
            return def != null && !string.IsNullOrEmpty(def.DisplayName) ? def.DisplayName : Title(Items.Key(id));
        }

        /// <summary>"assembler-mk2" → "Assembler mk2": the last resort when data has no display name for a key.</summary>
        public static string Title(string key)
        {
            if (string.IsNullOrEmpty(key)) return "";
            var words = key.Replace('-', ' ').Replace('_', ' ');
            return char.ToUpperInvariant(words[0]) + words.Substring(1);
        }

        private static string ItemName(GameData d, ItemId id)
        {
            var def = d?.Item(id);
            return def != null && !string.IsNullOrEmpty(def.DisplayName)
                ? def.DisplayName.ToLowerInvariant()
                : Title(Items.Key(id)).ToLowerInvariant();
        }
    }
}
