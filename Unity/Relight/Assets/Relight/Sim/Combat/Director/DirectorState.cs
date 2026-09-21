using System.Collections.Generic;

namespace Relight.Sim
{
    /// <summary>
    /// A committed major assault (reference campaignDefence.ts <c>Assault</c>, the fields the active-raid clock uses).
    /// Every time is an ABSOLUTE sim second (<see cref="SimState.T"/>), never a countdown and never wall-clock, so a
    /// save resumes on exactly the same schedule it was taken on.
    /// </summary>
    public sealed class MajorRaid : IVisitable
    {
        public int Id;
        /// <summary>Tile index of the locked approach (<c>y * width + x</c>).</summary>
        public int Origin = -1;
        /// <summary>The compass approaches the roster rotates through; always contains <see cref="Origin"/>.</summary>
        public int[] Origins = System.Array.Empty<int>();
        public double StartsAt;
        public double EndsAt;
        /// <summary>Sim second the next body is due (reference <c>a.nextSpawn</c>).</summary>
        public double NextSpawn;
        /// <summary>Bodies still owed by the roster (reference <c>a.remaining</c>).</summary>
        public int Remaining;
        public int Total;
        /// <summary>The roster was cut short (window elapsed or the core fell); survivors withdraw.</summary>
        public bool Retreat;
        /// <summary>It has started and the clock has already rolled to the following assault.</summary>
        public bool Committed;
        /// <summary>Bodies the window expired on before they could be born (reference <c>a.cancelled</c>).</summary>
        public int Cancelled;

        // ---- GP-W4: the wave plan ----
        //
        // The whole shape of the assault is decided ONCE, when it is scheduled, and then saved. Spawning is a pure
        // function of these arrays and the two counters below, so a save taken mid-assault resumes the same waves,
        // on the same approaches, at the same seconds — a wave cannot duplicate itself or quietly disappear.

        /// <summary>The wave currently walking on, indexing <see cref="WaveCount"/>.</summary>
        public int Wave;
        /// <summary>Bodies of <see cref="Wave"/> already born. Reset to 0 as each wave opens.</summary>
        public int WaveSpawned;
        /// <summary>How many waves have been named to the player, so none is announced twice.</summary>
        public int WaveAnnounced;
        /// <summary>Bodies in each wave; sums to <see cref="Total"/>.</summary>
        public int[] WaveCount = System.Array.Empty<int>();
        /// <summary>Spitters inside each wave's count; the rest are skitters.</summary>
        public int[] WaveSpitters = System.Array.Empty<int>();
        /// <summary>
        /// Breakers inside each wave's count (GP-W5), taken OUT of its skitters and leading it. Not a second
        /// roster: the head count the player was warned about is still the head count that arrives, and what
        /// changes is how much of it is 220 hp rather than 20.
        /// </summary>
        public int[] WaveBreakers = System.Array.Empty<int>();
        /// <summary>Absolute sim second each wave opens.</summary>
        public double[] WaveStart = System.Array.Empty<double>();
        /// <summary>How many of <see cref="Origins"/> each wave uses; this is the multi-position pressure.</summary>
        public int[] WaveApproaches = System.Array.Empty<int>();
        /// <summary>Where in <see cref="Origins"/> each wave starts counting, so the first side varies.</summary>
        public int[] WaveOffset = System.Array.Empty<int>();

        /// <summary>How many waves this assault was planned with. 0 for a plan that was never built.</summary>
        public int Waves => WaveCount == null ? 0 : WaveCount.Length;

        public void Visit(IStateVisitor v)
        {
            v.Field("id", ref Id);
            v.Field("origin", ref Origin);
            v.Field("origins", ref Origins);
            v.Field("startsAt", ref StartsAt);
            v.Field("endsAt", ref EndsAt);
            v.Field("nextSpawn", ref NextSpawn);
            v.Field("remaining", ref Remaining);
            v.Field("total", ref Total);
            v.Field("retreat", ref Retreat);
            v.Field("committed", ref Committed);
            v.Field("cancelled", ref Cancelled);
            v.Field("wave", ref Wave);
            v.Field("waveSpawned", ref WaveSpawned);
            v.Field("waveAnnounced", ref WaveAnnounced);
            v.Field("waveCount", ref WaveCount);
            v.Field("waveSpitters", ref WaveSpitters);
            v.Field("waveBreakers", ref WaveBreakers);
            v.Field("waveStart", ref WaveStart);
            v.Field("waveApproaches", ref WaveApproaches);
            v.Field("waveOffset", ref WaveOffset);
        }
    }

    /// <summary>
    /// A minor raid or the C-09 introductory encounter: a single group from one approach.
    ///
    /// GP-W3 split it into a WARNING and a WAVE. A scheduled raid is announced at <see cref="StartsAt"/> minus its
    /// preparation warning and exists, unspawned, for the whole of it — that is what gives the player the 30–45 s
    /// the brief asks for, and it is saved, so a warning cannot be lost or doubled by a save taken inside it.
    /// <see cref="Spawned"/> is the difference between the two: until it is set there are no bodies, so the
    /// "every body is gone, the raid is over" rule must not fire.
    /// </summary>
    public sealed class MinorRaid : IVisitable
    {
        public int Id;
        public int Origin = -1;
        public bool Retreat;
        /// <summary>
        /// True when C-09 scheduled this group through <see cref="Director.ScheduleGroup"/>. It then counts as an
        /// ordinary minor raid for blocking and cleanup (the reference's <c>d.minor.id === d.opening.id</c> rule)
        /// but never advances <see cref="DirectorState.RaidsStarted"/>.
        /// </summary>
        public bool Scripted;
        /// <summary>Absolute sim second the bodies arrive. A scripted group is staged at once, so it is its birth.</summary>
        public double StartsAt;
        /// <summary>Bodies the raid still owes the map. 0 once they are all born.</summary>
        public int Owed;
        /// <summary>The bodies are on the map. Until then this raid is a warning, not a wave.</summary>
        public bool Spawned;
        /// <summary>The announced approach, as a compass word, so the arriving wave matches what was said.</summary>
        public string Heading = "";

        public void Visit(IStateVisitor v)
        {
            v.Field("id", ref Id);
            v.Field("origin", ref Origin);
            v.Field("retreat", ref Retreat);
            v.Field("scripted", ref Scripted);
            v.Field("startsAt", ref StartsAt);
            v.Field("owed", ref Owed);
            v.Field("spawned", ref Spawned);
            v.Field("heading", ref Heading);
        }
    }

    /// <summary>
    /// How a large raid ended (E-18). The numbers are saved, so they never change.
    ///
    /// <see cref="BrokeOff"/> is deliberately NEUTRAL: what it should count as is the owner's open question F1-30,
    /// so until that is answered nothing treats it as a win — it earns no growth credit and no "repelled".
    /// </summary>
    public enum RaidOutcome
    {
        /// <summary>Every body was killed or the wave ran its course and the last body is gone.</summary>
        Cleared = 0,
        /// <summary>The raid beat its target: the Home core fell.</summary>
        Lost = 1,
        /// <summary>The raid was called off with bodies still alive: it lost its target another way, or overran.</summary>
        BrokeOff = 2,
    }

    /// <summary>One finished assault (reference <c>d.history</c>, capped at 32 entries).</summary>
    public sealed class RaidRecord : IVisitable
    {
        public int Id;
        public double Started;
        public double Ended;
        public int Spawned;
        /// <summary>
        /// A <see cref="RaidOutcome"/>, as its number: the one account of how the assault ended. Save v10; an older
        /// record reads as Cleared. Until INT-04c a <c>defeated</c> flag sat beside it. It copied the retreat flag,
        /// nothing read it, and its name said the opposite of what it held, so it is gone (save v11, which takes
        /// the member off an older file's records as it is read: <c>SaveUpgrade.TenToEleven</c>).
        /// </summary>
        public int Outcome;

        public void Visit(IStateVisitor v)
        {
            v.Field("id", ref Id);
            v.Field("started", ref Started);
            v.Field("ended", ref Ended);
            v.Field("spawned", ref Spawned);
            v.Field("outcome", ref Outcome);
        }
    }

    /// <summary>
    /// The raid director's saved clock — the reference <c>DefenceState.clock</c> plus the assault/minor/history
    /// fields the active-raid ruleset reads (campaignThreat.ts:30 <c>initActiveRaidClock</c> onward). The block
    /// economy is retired (CONTENT_CATALOGUE.md §17), so there is one target — the Home core — and the per-base
    /// list, nominations, radio warning and the legacy dusk schedule are not carried.
    ///
    /// A fresh instance means NOTHING HAS HAPPENED YET: <see cref="NextStart"/> 0 is the lazy-init trigger, so an
    /// upgraded Phase B save that fills this member from a fresh state seeds its clock on the first tick from the
    /// save's own <c>st.T</c> — it does not inherit a schedule that was never rolled.
    /// </summary>
    public sealed class DirectorState : IVisitable
    {
        /// <summary>Absolute sim second the next major assault begins; 0 until the clock is seeded.</summary>
        public double NextStart;
        /// <summary>Absolute sim second the next minor raid opportunity comes round.</summary>
        public double NextMinor;
        /// <summary>Raids are blocked until this absolute sim second (reference <c>clock.recoveryUntil</c>).</summary>
        public double RecoveryUntil;
        /// <summary>The serial fed to <see cref="DirectorRules.RaidChoice"/>; saved, never rerolled on load.</summary>
        public int Serial;
        /// <summary>Minor raids started so far; also the serial for the minor count and target rotation.</summary>
        public int RaidsStarted;
        /// <summary>
        /// Bodies the CURRENT major has already put on the map. Saved: it is what stops a wave that spawned before
        /// the save from spawning again after the load, because <c>NextSpawn</c> is derived from it.
        /// </summary>
        public int MajorSpawned;
        /// <summary>Next group id (reference <c>d.nextId</c>), from 1.</summary>
        public int NextId = 1;
        /// <summary>The last director notice for the HUD (reference <c>d.notice</c>); "" when there is none.</summary>
        public string Notice = "";
        /// <summary>
        /// Absolute sim second <see cref="Notice"/> stops being news and is cleared; 0 when there is nothing to
        /// clear. GP-W3: a notice about an encounter that has been and gone is the "stale notice" the brief names,
        /// so every notice now says how long it is worth reading.
        /// </summary>
        public double NoticeUntil;
        /// <summary>Absolute sim second the last major ended, or -1.</summary>
        public double LastMajorEnd = -1;
        /// <summary>The assault whose warning has been announced, so <see cref="RaidNoticeEvent"/> is raised once.</summary>
        public int WarnedId;

        public MajorRaid Major;
        public MinorRaid Minor;
        public List<RaidRecord> History = new List<RaidRecord>();

        /// <summary>
        /// C-09's hold. While it is set the director starts no major and no minor raid, exactly as a live raid
        /// would block one; <see cref="ReserveReason"/> is shown in the HUD notice. Saved, because the opening
        /// tutorial's hold must survive a save taken in the middle of it.
        /// </summary>
        public bool Reserved;
        public string ReserveReason = "";

        /// <summary>
        /// EDITOR DEBUG ONLY — never set by gameplay, never by a save that did not already have it, and never
        /// exposed in the player-facing UI. The Unity editor menu sets it before submitting
        /// <see cref="DebugRaidCommand"/>; with it false the command is refused.
        /// </summary>
        public bool DebugAllowed;

        // ---- derived, never visited ----

        /// <summary>BFS distance fields, keyed on <see cref="SimState.Rev"/> (see <see cref="RaidFieldCache"/>).</summary>
        public readonly RaidFieldCache Fields = new RaidFieldCache();

        /// <summary>True when the clock has never been rolled (a new game, or an upgraded Phase B save).</summary>
        public bool Unseeded => NextStart <= 0;

        public void Visit(IStateVisitor v)
        {
            v.Field("nextStart", ref NextStart);
            v.Field("nextMinor", ref NextMinor);
            v.Field("recoveryUntil", ref RecoveryUntil);
            v.Field("serial", ref Serial);
            v.Field("raidsStarted", ref RaidsStarted);
            v.Field("majorSpawned", ref MajorSpawned);
            v.Field("nextId", ref NextId);
            v.Field("notice", ref Notice);
            v.Field("noticeUntil", ref NoticeUntil);
            v.Field("lastMajorEnd", ref LastMajorEnd);
            v.Field("warnedId", ref WarnedId);
            v.Object("major", ref Major, () => new MajorRaid());
            v.Object("minor", ref Minor, () => new MinorRaid());
            v.List("history", History, () => new RaidRecord());
            v.Field("reserved", ref Reserved);
            v.Field("reserveReason", ref ReserveReason);
            v.Field("debugAllowed", ref DebugAllowed);
        }
    }

    public sealed partial class SimState
    {
        /// <summary>The raid director's clock and current waves (C-08), visited as <c>director</c>.</summary>
        public DirectorState Director = new DirectorState();

        partial void VisitDirector(IStateVisitor v) => v.Object("director", ref Director, () => new DirectorState());
    }
}
