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

        /// <summary>The one standing row for cargo dropped at a death (INT-01).</summary>
        public const string DroppedCargoKey = "cargo:dropped";

        private readonly StringBuilder _sb = new StringBuilder(96);
        private readonly List<HudProblem> _problems = new List<HudProblem>();
        private double _lastRefresh = double.NegativeInfinity;
        private string _brownoutPosted = "";
        private string _lowFuelPosted = "";
        private string _cargoPosted = "";
        private int _accountShown;
        private double _reportUntil = double.NegativeInfinity;
        private int _reportRaid = -1;
        private int _bannerShown;
        private double _bannerUntil = double.NegativeInfinity;
        private GameData _data;
        private readonly Dictionary<ItemId, (int n, double at)> _mined = new Dictionary<ItemId, (int n, double at)>();
        private readonly List<ProblemGroup> _groups = new List<ProblemGroup>();
        private bool _taughtHesitation;
        private bool _taughtBlindTurret;
        // REL-118: the once-only guide lines that are still owed their time on screen. See PumpGuides.
        private readonly List<GuideLine> _guides = new List<GuideLine>();
        private double _guideAt = double.NegativeInfinity;
        // REL-60: the sim time of the core's last hit, and what the core and engineer-down rows last said.
        private double _coreHitT = double.NegativeInfinity;
        private string _corePosted = "";
        private bool _downPosted;

        /// <summary>The single power-alert producer (defect U-3). Nothing else may raise one.</summary>
        public readonly PowerAlertSource Power = new PowerAlertSource();

        /// <summary>The single dry-turret and wreck producer (E-17, U-D-61), on the same one-producer rule.</summary>
        public readonly DefenceAlertSource Defence = new DefenceAlertSource();

        // What each defence row last said, so a standing row is posted on a change and never per refresh (U-D-55).
        private readonly Dictionary<string, string> _defencePosted = new Dictionary<string, string>(StringComparer.Ordinal);
        private readonly List<string> _defenceGone = new List<string>();

        /// <summary>The single post-attack account producer (GP-W6).</summary>
        public readonly RaidAccountSource Account = new RaidAccountSource();

        /// <summary>The single wave-banner producer (REL-74).</summary>
        public readonly RaidBannerSource Banners = new RaidBannerSource();

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

        /// <summary>
        /// "about 4 min at this load (estimate)", or "" when nothing is burning: the power tooltip's "Fuel left" row
        /// (REL-10; it had no reader before). The time is <see cref="PowerAlertSource.FuelText"/>'s.
        /// </summary>
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

        /// <summary>"Rifle · 12 / 20 rounds", "Reloading" while it reloads, "" with nothing equipped.</summary>
        public string Weapon { get; private set; } = "";

        /// <summary>0 when not reloading, else 0..1 through the reload, for a meter.</summary>
        public double ReloadFraction { get; private set; }

        /// <summary>"74 rounds carried" — the reserve the Backpack can still feed (§4).</summary>
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

        // ---- raid feedback (REL-74, E-20, U-D-66 (7)) --------------------------------------------------------

        /// <summary>True while a raid is warned or on the ground: the edge arrow is drawn.</summary>
        public bool RaidPointerVisible { get; private set; }

        /// <summary>
        /// Where the arrow points, in world tiles (y down, as the sim counts): the warned raid's approach, then the
        /// front of the raid once its bodies are on the map.
        /// </summary>
        public double RaidPointerX { get; private set; }
        public double RaidPointerY { get; private set; }

        /// <summary>The arrow's label: "Raid · 30 s", "Major assault · 45 s", "Wave 2 of 4", "Raid".</summary>
        public string RaidPointerLabel { get; private set; } = "";

        /// <summary>The newest wave banner, "Wave 3 of 4, from the east"; shown while <see cref="BannerVisible"/>.</summary>
        public string Banner { get; private set; } = "";

        /// <summary>True for <see cref="RaidBannerSource.Seconds"/> real seconds after each banner.</summary>
        public bool BannerVisible { get; private set; }

        /// <summary>
        /// True for <see cref="RaidAccountSource.Seconds"/> real seconds after a watched raid ends, until the player
        /// closes it (<see cref="DismissReport"/>) or the next raid reaches the ground. Hidden while a drawer is
        /// open, and those seconds do not count.
        /// </summary>
        public bool ReportVisible { get; private set; }

        /// <summary>The report card's title, "Major assault repelled".</summary>
        public string ReportTitle => Account.CardTitle;

        /// <summary>The report card's rows (<see cref="RaidAccountSource.CardRows"/>).</summary>
        public IReadOnlyList<RaidReportRow> ReportRows => Account.CardRows;

        /// <summary>The line that teaches, "East turrets ran dry in wave 3".</summary>
        public string ReportLesson => Account.CardLesson;

        /// <summary>True when the card reports a loss, so it is framed as a warning.</summary>
        public bool ReportLoss => Account.Losses;

        /// <summary>Close the report card now (its close button). It does not come back for the same raid.</summary>
        public void DismissReport()
        {
            _reportUntil = double.NegativeInfinity;
            ReportVisible = false;
        }

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
            var sinceLast = now - _lastRefresh;
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
                RaidPointerVisible = false; RaidPointerLabel = "";
                BannerVisible = false; ReportVisible = false;
                _problems.Clear();
                ReapNotices(now);
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
            FuelText = Power.FuelText.Length > 0 ? Power.FuelText + " at this load (estimate)" : "";
            FuelLow = Power.LowFuelText.Length > 0;
            // U-D-55. An outage is a STANDING state, not an event, so it lives in the urgent strip and NOWHERE
            // else. It used to be posted into the notice inbox as well, with an infinite lifetime, on every
            // 150 ms refresh — and <see cref="HudNotices.Post"/> counts a repeat whenever the row it replaces is
            // still live, so the toast beside the strip churned "No power · Home × 47" several times a second,
            // saying a second time what the strip above it already said.
            Alert = Power.AlertText;
            Notices.Clear(PowerAlertSource.AlertKey);
            // L-02 (§5.7): a brownout is posted when it starts and cleared when it ends — never re-posted per
            // refresh, which is the repeat-count churn U-D-55 removed for the outage. REL-7: it STANDS while true,
            // like low fuel, and both rows are re-posted only when the place they name changes.
            PostPower(PowerAlertSource.BrownoutKey, Power.BrownoutText, ref _brownoutPosted, now);
            // GP-W6: low fuel is a standing state on the same edge rule. The sentence does not count down, so it
            // is posted once; a player who dismisses it has been told, and the strip's fuel line keeps the time.
            PostPower(PowerAlertSource.LowFuelKey, Power.LowFuelText, ref _lowFuelPosted, now);

            // E-17 (U-D-61): one standing row per place for dry turrets and wrecks, from the one producer.
            Defence.FallbackPlace = Power.PlaceName;
            Defence.Refresh(ctx, st);
            PostDefence(now);
            PostDroppedCargo(ctx, st, now);

            // GP-W6: the post-attack account, shown once when the raid it watched is over. REL-74: as the report
            // card, which replaces the notice row it used to be; posting both would say the same thing twice.
            Account.Refresh(ctx, st);
            if (Account.Serial != _accountShown)
            {
                _accountShown = Account.Serial;
                _reportUntil = now + RaidAccountSource.Seconds;
                _reportRaid = RaidAccountSource.UnderWay(st, out var onGround, out _, out _) ? onGround : -1;
            }
            // The card belongs to the quiet after a raid: the next one reaching the ground puts it away.
            if (RaidAccountSource.UnderWay(st, out var raidNow, out _, out _) && raidNow != _reportRaid)
                _reportUntil = double.NegativeInfinity;
            // A drawer covers the top of the screen, so the card steps aside while one is open and its time waits:
            // a player who opened the Backpack as the raid ended still gets the whole card after.
            if (menuOpen && now < _reportUntil && sinceLast > 0 && !double.IsInfinity(sinceLast))
                _reportUntil += Math.Min(sinceLast, 1);
            ReportVisible = now < _reportUntil && !menuOpen;

            // REL-74: one banner per wave, each shown once.
            Banners.Refresh(ctx, st);
            if (Banners.Serial != _bannerShown)
            {
                _bannerShown = Banners.Serial;
                _bannerUntil = now + RaidBannerSource.Seconds;
            }
            Banner = Banners.Text;
            BannerVisible = now < _bannerUntil && Banner.Length > 0;

            var maxCore = HomeQueries.CoreMaxHp(d);
            var hp = HomeQueries.CoreHp(st);
            if (st.Home != null && st.Home.Placed)
            {
                CoreDisabled = hp <= 0;
                CoreFraction = maxCore > 0 ? Clamp01(hp / maxCore) : 0;
                Core = CoreDisabled
                    ? "Home core · DISABLED"
                    : "Home core · " + HomeQueries.CoreHpText(hp, maxCore);
            }
            else { Core = ""; CoreFraction = 0; CoreDisabled = false; }

            // REL-60 (UI-10): the core's standing row. It is read from STATE, so a loaded game with a downed core
            // says so too, and posted only when what it says changes (U-D-55): a raid's hundred hits are one row.
            // A hit is recent while the sim clock is at or past it, so a load back to an earlier save cannot keep
            // saying "under attack" from the session before.
            var hitRecently = st.T >= _coreHitT && st.T - _coreHitT < CoreHitSeconds;
            PostCore(st.Home != null && st.Home.Placed ? CoreRowText(hp, maxCore, hitRecently) : "", now);

            // --- bottom left ----------------------------------------------------------------------------------
            var e = WorldQueries.Engineer(ctx, st);
            PostEngineerDown(e.IsDown, now);
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
                // §5.7 / §7.5 correction 7.0-b: the player-facing word is always "rounds", never "magazine" or
                // "bullets" (U-D-68 (c), REL-55).
                _sb.AppendFormat(CultureInfo.InvariantCulture, " · {0:0} / {1:0} rounds", w.Loaded, w.Capacity);
                if (w.Reloading) _sb.Append(" · reloading");
                Weapon = _sb.ToString();
                ReloadFraction = w.Reloading ? Clamp01(w.ReloadFraction) : 0;
                Ammo = string.Format(CultureInfo.InvariantCulture, "{0:0} rounds carried", w.Reserve);
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
            Pointer(ctx, st);

            // --- machine problems -----------------------------------------------------------------------------
            CollectProblems(ctx, st);

            ReapNotices(now);
            return true;
        }

        /// <summary>Notice key prefix for a queued command's result; the suffix is the command's type name.</summary>
        public const string CommandNoticeKey = "cmd:";

        /// <summary>
        /// INT-01. A standing row while cargo dropped at a death lies uncollected, naming the place. It is read from
        /// STATE, not from <see cref="CargoDroppedEvent"/>: the pile is saved and the event is not, so a loaded game
        /// must still say where the cargo is. Posted when what it says changes and never per refresh (U-D-55); a
        /// player who dismisses it has been told, and the marker in the world still shows the pile.
        /// </summary>
        private void PostDroppedCargo(SimContext ctx, SimState st, double now)
        {
            var n = DeathCache.Live(st, out var newest);
            var text = n == 0 ? "" : DroppedCargoText(n, Defence.PlaceAt(ctx, newest.X + 0.5, newest.Y + 0.5));
            if (text == _cargoPosted) return;
            if (text.Length == 0) Notices.Clear(DroppedCargoKey);
            else Notices.Post(DroppedCargoKey, text, HudNoticeKind.Warning, now, double.PositiveInfinity);
            _cargoPosted = text;
        }

        /// <summary>REL-60 (UI-10): the one standing row for the Home core — under attack, disabled or damaged.</summary>
        public const string CoreNoticeKey = "core:home";
        /// <summary>REL-60: a patch repair the core fell during. The sim's own sentence, once.</summary>
        public const string RepairAbortedKey = "core:repair-aborted";
        /// <summary>REL-60: the standing row while the engineer is down.</summary>
        public const string EngineerDownKey = "engineer:down";
        /// <summary>REL-60: the moment the engineer is back up. Its own key, so the down row clearing cannot wipe it.</summary>
        public const string EngineerUpKey = "engineer:up";

        /// <summary>
        /// Reference <c>campaignAlerts.ts:24</c>, whose "under attack" lasts while <c>st.t - e.lastHit &lt; 5</c>: the
        /// core row says the core is being hit for this many SIM seconds after the last hit, then falls back to
        /// "damaged".
        /// </summary>
        public const double CoreHitSeconds = 5;

        public const string CoreUnderAttackText = "Home core under attack";
        public const string CoreDisabledText =
            "Home core disabled · press E at the core, then " + OpeningQueries.RecommissionButton + " in the workshop";
        public const string CoreDamagedText = "Home core damaged · press E at the core to repair it";
        public const string EngineerDownText = "Engineer down · wait to recover at Home";
        public const string EngineerUpText = "Back on your feet at Home";

        /// <summary>
        /// The core row's sentence, "" at full health. §8 puts base damage above everything but engineer danger, so
        /// a core being hit or knocked out is a <see cref="HudNoticeKind.Danger"/> row; one merely left damaged
        /// after the hits stop is a warning, so a raid survived does not leave a red row until the repair.
        /// </summary>
        public static string CoreRowText(double hp, double max, bool hitRecently) =>
            max <= 0 || hp >= max ? ""
            : hp <= 0 ? CoreDisabledText
            : hitRecently ? CoreUnderAttackText
            : CoreDamagedText;

        private static HudNoticeKind CoreRowKind(string text) =>
            string.Equals(text, CoreDamagedText, StringComparison.Ordinal) ? HudNoticeKind.Warning : HudNoticeKind.Danger;

        // One edge memory for the core row, shared by Intake (the frame it happens) and Refresh (from state), so the
        // two never post the same sentence twice and the row never counts a repeat for a hit.
        /// <summary>A standing power warning, posted when its sentence changes and cleared when it goes.</summary>
        private void PostPower(string key, string text, ref string posted, double now)
        {
            if (string.Equals(text, posted, StringComparison.Ordinal)) return;
            if (text.Length == 0) Notices.Clear(key);
            else Notices.Post(key, text, HudNoticeKind.Warning, now, double.PositiveInfinity);
            posted = text;
        }

        private void PostCore(string text, double now)
        {
            if (string.Equals(text, _corePosted, StringComparison.Ordinal)) return;
            if (text.Length == 0) Notices.Clear(CoreNoticeKey);
            else Notices.Post(CoreNoticeKey, text, CoreRowKind(text), now, double.PositiveInfinity);
            _corePosted = text;
        }

        private void PostEngineerDown(bool down, double now)
        {
            if (down == _downPosted) return;
            if (down) Notices.Post(EngineerDownKey, EngineerDownText, HudNoticeKind.Danger, now, double.PositiveInfinity);
            else Notices.Clear(EngineerDownKey);
            _downPosted = down;
        }

        /// <summary>The row's sentence. One pile names its place; several say how many and name the newest.</summary>
        public static string DroppedCargoText(int piles, string place) =>
            piles <= 1
                ? "Dropped cargo at " + place + " · walk back and press E beside it to collect"
                : piles + " piles of dropped cargo · the newest at " + place + " · press E beside one to collect";

        /// <summary>Notice key for a dig that stopped with a reason.</summary>
        public const string MiningNoticeKey = "mining";
        /// <summary>REL-84: the balance data was swapped under the running game (an editor tuning reload).</summary>
        public const string DataReloadKey = "data-reload";

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

        /// <summary>REL-118: one guide line still owed its <see cref="GuideSeconds"/> in front of the player.</summary>
        private sealed class GuideLine
        {
            public string Key;
            /// <summary>Real seconds it has actually been among the rows the HUD draws.</summary>
            public double Shown;
        }

        /// <summary>
        /// REL-118: say a once-only teaching line. It is posted as a STANDING row and retired by
        /// <see cref="PumpGuides"/> once it has been ON SCREEN for <see cref="GuideSeconds"/>, not once that many
        /// seconds have passed. A raid's own Danger and Warning rows outrank it, so before this it could be pushed
        /// into the overflow ("+1 more") within a second or two of being posted and, being once-only, never read.
        /// Now the crowd only delays the lesson: the line waits in the overflow and comes back as a place frees.
        ///
        /// The ranking is untouched (U-D-55): a lesson never outranks live danger.
        /// </summary>
        private void Teach(string key, string line, HudNoticeKind kind, double now)
        {
            Notices.Post(key, line, kind, now, double.PositiveInfinity);
            for (var i = 0; i < _guides.Count; i++) if (string.Equals(_guides[i].Key, key, StringComparison.Ordinal)) return;
            _guides.Add(new GuideLine { Key = key });
        }

        /// <summary>
        /// Charge every pending guide line for the time it spent on screen, and clear it when it has had its full
        /// <see cref="GuideSeconds"/>. Real (unscaled) seconds, like every other row; a clock that jumps backwards
        /// (a load) charges nothing.
        /// </summary>
        private void PumpGuides(double now)
        {
            var dt = now - _guideAt;
            _guideAt = now;
            if (_guides.Count == 0) return;
            if (!(dt > 0) || double.IsInfinity(dt)) dt = 0;
            var rows = Notices.Rows;
            for (var i = _guides.Count - 1; i >= 0; i--)
            {
                var g = _guides[i];
                var shown = false;
                for (var r = 0; r < rows.Count && !shown; r++) shown = string.Equals(rows[r].Key, g.Key, StringComparison.Ordinal);
                if (!shown) continue;
                g.Shown += dt;
                if (g.Shown < GuideSeconds) continue;
                Notices.Clear(g.Key);
                _guides.RemoveAt(i);
            }
        }

        /// <summary>
        /// Expire what has timed out, refresh the drawn rows, then account for the guide lines (REL-118). Every
        /// HUD refresh ends here, and a test that drives <see cref="Intake"/> by hand calls it in place of
        /// <c>Notices.Reap</c>.
        /// </summary>
        public void ReapNotices(double now)
        {
            Notices.Reap(now);
            PumpGuides(now);
        }

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
                    // REL-84: a tuning asset edited in the editor took effect. Developer-facing, so it names the hashes
                    // a save would record, and a second edit within the window replaces the row rather than stacking.
                    case DataReloadedEvent r:
                        Notices.Post(DataReloadKey, "Tuning reloaded · data " + r.FromHash + " → " + r.ToHash,
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
                        Teach(HesitationKey, HesitationLine, HudNoticeKind.Info, now);
                        break;
                    // L-02 (§5.7): one line, the first time a turret is hit from the dark. The badge repeats it.
                    case TurretBlindEvent _:
                        if (_taughtBlindTurret) break;
                        _taughtBlindTurret = true;
                        Teach(BlindTurretKey, BlindTurretLine, HudNoticeKind.Warning, now);
                        break;
                    // L-02 (§5.4): the moment that replaces dawn gets a line as well as its sweep and cue.
                    case DistrictLitEvent lit:
                        Notices.Post(DistrictLitKey,
                            lit.Name + " connected · " + lit.Lights.ToString(CultureInfo.InvariantCulture)
                            + (lit.Lights == 1 ? " streetlight on" : " streetlights on"),
                            HudNoticeKind.Info, now, GuideSeconds);
                        break;
                    // REL-60 (UI-10): the worst moments of a raid. Each raises its row the frame it happens; Refresh
                    // keeps the standing ones true to state on the same edge memory, so none is posted twice.
                    case CoreDamagedEvent hit:
                        _coreHitT = hit.T;
                        if (hit.Hp > 0) PostCore(CoreUnderAttackText, now);
                        break;
                    case CoreDisabledEvent _:
                        PostCore(CoreDisabledText, now);
                        break;
                    // The sim's own sentence, verbatim ("Core knocked out during repair; a full recovery kit is
                    // required."). It is a moment, not a state — the core row already says the core is disabled.
                    case CoreRepairAbortedEvent aborted:
                        if (string.IsNullOrEmpty(aborted.Text)) break;
                        Notices.Post(RepairAbortedKey, aborted.Text, HudNoticeKind.Warning, now, GuideSeconds);
                        break;
                    case EngineerDownEvent _:
                        Notices.Clear(EngineerUpKey);
                        PostEngineerDown(true, now);
                        break;
                    case EngineerUpEvent _:
                        PostEngineerDown(false, now);
                        Notices.Post(EngineerUpKey, EngineerUpText, HudNoticeKind.Info, now, NoteSeconds);
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
        ///
        /// REL-12 — one rule decides every line: the strip describes the STATE the sim is in, and is red only while
        /// something is attacking. The director's last sentence is printed in exactly one place, the recovery line,
        /// where there is no fight to describe and that sentence is the account of the one just finished. Every
        /// other kind has its own wording, pinned by <c>RaidFeedbackTests</c>, so none of them can fall through to
        /// "Base under attack" again.
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
            var beside = LargeRaidBeside(wv);
            if (wv.SecondsLeft > 0)
                return string.Format(CultureInfo.InvariantCulture,
                    "{0} approaching from the {1} · Arrives in {2:0} s",
                    string.IsNullOrEmpty(wv.Label) ? "Small enemy group" : wv.Label,
                    string.IsNullOrEmpty(wv.Direction) ? "·" : wv.Direction, Math.Ceiling(wv.SecondsLeft)) + beside;
            urgent = true;
            var from = string.IsNullOrEmpty(wv.Direction) || wv.Direction == "·" ? "" : " · from the " + wv.Direction;
            // REL-74: a raid on the ground is described from state. The director's notice is the last thing it
            // SAID, and for an assault's first wave, or a small raid, that is still the countdown ("inbound … in
            // 45 s"), which the line kept printing in red for as long as the fight lasted.
            if (wv.Kind == "assault" && st.Director.Major != null)
            {
                var a = st.Director.Major;
                var w = Math.Max(1, Math.Min(a.WaveAnnounced, a.Waves));
                var sides = DirectorRules.HeadingsOf(ctx, st, a.Waves > 0 ? SiegePlan.Sides(a, w - 1) : a.Origins);
                var waveFrom = sides == "·" ? "" : " · from the " + sides;
                return a.Waves > 1
                    ? string.Format(CultureInfo.InvariantCulture, "Major assault · wave {0} of {1}{2}", w, a.Waves, waveFrom)
                    : "Major assault under way" + waveFrom;
            }
            if (wv.Kind == "minor raid")
                return (string.IsNullOrEmpty(wv.Label) ? "Raid" : wv.Label) + " attacking" + from + beside;
            // REL-12: a raid whose second has come but whose bodies are not on the map yet — the tick between the
            // countdown reaching zero and the spawn. It used to fall past every branch below to "Base under attack",
            // a fight that was not happening; it is a raid landing, and says so.
            if (wv.Kind == "warning")
                return (string.IsNullOrEmpty(wv.Label) ? "Small enemy group" : wv.Label) + " arriving now" + from + beside;
            // REL-74: after a raid nothing is attacking. Once the director's last word had expired, the recovery
            // fell through to "Base under attack" in red for the rest of it; a withdrawal did the same.
            if (wv.Kind == "recovery")
            {
                urgent = false;
                return wv.Notice ?? "";
            }
            // REL-12: a withdrawal is described from state, whatever the director last said. Its notice is as often
            // as not the countdown that brought the raid in ("Raid inbound from E in 30 s."), which printed in red
            // over a raid that was walking away; what the director says at the END of a raid is said again through
            // the recovery line moments later, so nothing a player needs is lost here.
            if (wv.Kind == "withdrawal")
            {
                urgent = false;
                return (string.IsNullOrEmpty(wv.Label) ? "Raid" : wv.Label) + " withdrawing";
            }
            // Nothing above matched, so the kind is one this method does not know. Say the plain thing the strip is
            // for, never the director's last sentence — printing that is the stale-notice defect this row names.
            return "Base under attack";
        }

        /// <summary>
        /// REL-12 (ENM-06): the tail that keeps an announced large raid on the strip while a small one holds the
        /// line. The small raid is the fight in front of the player and keeps the sentence; the large one rides
        /// beside it rather than waiting, unannounced, until the small one is over.
        /// </summary>
        private static string LargeRaidBeside(RaidWarning wv) =>
            wv.AlsoKind == "warning"
                ? string.Format(CultureInfo.InvariantCulture, " · Major assault in {0:0} s", Math.Ceiling(wv.AlsoSecondsLeft))
                : wv.AlsoKind == "assault" ? " · Major assault under way" : "";

        /// <summary>
        /// REL-74: the raid arrow. It is up while a raid is warned or on the ground, never while one only walks
        /// away. It points at the warned approach until bodies are on the map, then at the raid's front
        /// (<see cref="DirectorQueries.Front"/>), and back at the approach between waves.
        /// </summary>
        private void Pointer(SimContext ctx, SimState st)
        {
            var wv = DirectorQueries.Warning(ctx, st);
            var warned = wv.Kind == "warning";
            var onGround = wv.Kind == "assault" || wv.Kind == "minor raid";
            RaidPointerVisible = (warned || onGround) && wv.RaidId > 0;
            if (!RaidPointerVisible) { RaidPointerLabel = ""; return; }

            var at = wv.At;
            if (onGround && DirectorQueries.Front(ctx, st, wv.RaidId, out var front)) at = front;
            RaidPointerX = at.X;
            RaidPointerY = at.Y;
            var label = string.IsNullOrEmpty(wv.Label) ? "Raid" : wv.Label;
            var a = st.Director.Major;
            if (warned)
                RaidPointerLabel = label + " · " + Math.Ceiling(wv.SecondsLeft).ToString("0", CultureInfo.InvariantCulture) + " s";
            else if (wv.Kind == "assault" && a != null && a.Waves > 1)
                RaidPointerLabel = string.Format(CultureInfo.InvariantCulture, "Wave {0} of {1}",
                    Math.Max(1, Math.Min(a.WaveAnnounced, a.Waves)), a.Waves);
            else RaidPointerLabel = label;
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
