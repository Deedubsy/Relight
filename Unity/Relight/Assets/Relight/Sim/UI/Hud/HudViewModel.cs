using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace Relight.Sim.UI
{
    /// <summary>One actionable machine problem the HUD can show, already worded.</summary>
    public readonly struct HudProblem
    {
        public readonly int MachineId;
        public readonly MachineOperatingState State;
        /// <summary>"Generator: out of fuel — put coal in the generator".</summary>
        public readonly string Text;

        public HudProblem(int machineId, MachineOperatingState state, string text)
        {
            MachineId = machineId; State = state; Text = text;
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
        private bool _taughtHesitation;
        private bool _taughtBlindTurret;

        /// <summary>The single power-alert producer (defect U-3). Nothing else may raise one.</summary>
        public readonly PowerAlertSource Power = new PowerAlertSource();

        /// <summary>The keyed alert inbox: one urgent strip plus at most three transient rows (§8).</summary>
        public readonly HudNotices Notices = new HudNotices();

        // ---- status strip (top centre, content width) --------------------------------------------------------

        /// <summary>Elapsed play time, "H:MM:SS" (U-D-58: there is no sun, so there are no days to count).</summary>
        public string Clock { get; private set; } = "0:00:00";

        /// <summary>"In light" or "In the dark", from the sim's lit mask at the engineer's tile (ALWAYS_DARK_SPEC.md §3).</summary>
        public string LightText { get; private set; } = "";
        public bool InDark { get; private set; }

        /// <summary>"Home: 40 / 300 kW", or one of the two GP-POWER-FIX sentences (hud.ts:58-60).</summary>
        public string PowerText { get; private set; } = PowerAlertSource.NoSourceText;

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
                Core = Engineer = Weapon = Ammo = Backpack = HandLock = Threat = Alert = "";
                LightText = ""; InDark = false;
                MiningVisible = false;
                _problems.Clear();
                Notices.Reap(now);
                return true;
            }

            var d = ctx.Data;

            // --- status strip ---------------------------------------------------------------------------------
            Clock = FormatClock(st.T, paused);
            var at = WorldQueries.Engineer(ctx, st).Pos;
            InDark = !LightQueries.LitAt(st, (int)Math.Floor(at.X), (int)Math.Floor(at.Y));
            LightText = InDark ? "In the dark" : "In light";

            Power.Refresh(ctx, st);
            PowerText = Power.StripText;
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
                    case MinedEvent gained when gained.MachineId < 0:
                        Notices.Post("mined:"+Items.Key(gained.Item), "+1 "+Items.Key(gained.Item)+" to Backpack", HudNoticeKind.Info, now, NoteSeconds);
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

        /// <summary>
        /// Machine problems worth acting on, worst first. The reason word is
        /// <see cref="ProductionQueries.StateText"/> verbatim — the brief forbids a second vocabulary — and the
        /// remedy clause after the dash is §8's actionable half.
        /// </summary>
        private void CollectProblems(SimContext ctx, SimState st)
        {
            _problems.Clear();
            var d = ctx.Data;
            for (var i = 0; i < st.Machines.Count && _problems.Count < MaxProblems; i++)
            {
                var m = st.Machines[i];
                var s = ProductionQueries.OperatingState(ctx, st, m.Id);
                var remedy = Remedy(s);
                if (remedy == null) continue;
                var name = d.TryMachine(m.Kind, out var spec) ? spec.DisplayName : m.Kind;
                _problems.Add(new HudProblem(m.Id, s,
                    name + ": " + ProductionQueries.StateText(s) + " — " + remedy));
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
                default: return null;   // running, throttled and idle are not problems the strip raises
            }
        }

        private static double Clamp01(double v) => v < 0 ? 0 : v > 1 ? 1 : v;

        private static string ItemName(GameData d, ItemId id)
        {
            var def = d?.Item(id);
            return def != null && !string.IsNullOrEmpty(def.DisplayName)
                ? def.DisplayName.ToLowerInvariant()
                : Items.Key(id);
        }
    }
}
