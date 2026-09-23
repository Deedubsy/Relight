using System;

namespace Relight.Sim
{
    /// <summary>
    /// The single global raid director. The port of campaignThreat.ts:36 <c>tickCampaignSchedule</c>, active-raid
    /// ruleset only: the legacy dusk schedule (<c>tickLegacyCampaignSchedule</c>), the block economy, the per-base
    /// list, radio intelligence and nominations are retired (CONTENT_CATALOGUE.md §17), so there is exactly one
    /// target — the Home core — and <c>eligible()</c> collapses to "the core still stands".
    ///
    /// Every stored time is an ABSOLUTE sim second, so a save resumes on the schedule it was taken on and a load
    /// cannot replay a wave: <see cref="DirectorState.MajorSpawned"/> is saved and <c>NextSpawn</c> is derived from
    /// it, exactly as the reference does.
    ///
    /// The reference's "opportunity skipped" rule is kept verbatim for ORDINARY raids (U-D-38: a raid that is
    /// missed or delayed never queues extra pressure for later). It is <see cref="Director.Defer"/> — the call
    /// C-09 makes for the introductory encounter — that moves the clock forward instead of dropping the wave
    /// (U-D-26, U-Q-21).
    /// </summary>
    public sealed class DirectorPhase : ITickPhase
    {
        public void Tick(SimContext ctx, SimState st, double dt)
        {
            var d = st.Director;
            var r = ctx.Data.Raids;
            if (d.Unseeded) Seed(ctx, st);
            Director.Expire(st);

            // A wave whose last body is gone is over, whoever killed it. A WARNED raid has no bodies yet and owes
            // the map some, so "none alive" is only the end once it has actually arrived and finished arriving.
            if (d.Minor != null && d.Minor.Spawned && d.Minor.Owed <= 0 && EnemyQueries.GroupAlive(st, d.Minor.Id) == 0)
            {
                // REL-85: the one place a small raid that fought ends, so the one place its log line closes. It
                // keeps no saved record; its outcome is the account's rule (cleared unless its retreat was called).
                var m = d.Minor;
                var outcome = m.Retreat ? DirectorRules.CalledOff(ctx, st) : RaidOutcome.Cleared;
                st.Events.Add(new RaidEndedEvent(st.T, m.Id, false, m.Scripted, (int)outcome, m.StartsAt));
                d.Minor = null;
                d.LastMinorEnd = st.T;                                  // REL-75: the gap before a warning runs from here
            }

            CoreFell(ctx, st);
            EndMajor(ctx, st);
            PurgeStragglers(ctx, st);

            // An unresolved previous assault never becomes a second simultaneous target or accumulated debt.
            if (d.Major != null && d.Major.Committed && st.T >= d.NextStart)
                Future(ctx, st, "Previous assault cleanup is still unresolved.");

            if (d.Major == null && st.T >= d.NextStart - r.WarningS && !HoldWarning(ctx, st)) Schedule(ctx, st);

            Commit(ctx, st);
            Spawn(ctx, st);
            Warn(ctx, st);
            MinorArrive(ctx, st);
            MinorAnnounce(ctx, st);
        }

        // ------------------------------------------------------------------ the failure path (E-18)

        /// <summary>
        /// A raid that has beaten its target is over (E-18, U-D-64 d). The retreat is made STICKY here: bodies
        /// already walk off when there is no target, but the raid itself must not come back to life if the core is
        /// repaired while they are still leaving, and a large raid must stop owing the map more bodies.
        /// </summary>
        private static void CoreFell(SimContext ctx, SimState st)
        {
            var d = st.Director;
            // FRT-09: an assault on a plant is beaten when that plant falls, whatever Home is doing; the Home core
            // falling beats the small raid and any assault on Home.
            var homeDown = EnemyCoreHook.Down(ctx, st);
            if (homeDown && d.Minor != null && d.Minor.Spawned && !d.Minor.Retreat) { d.Minor.Retreat = true; d.Minor.Owed = 0; }
            if (d.Major != null && d.Major.Committed && !d.Major.Retreat
                && (string.IsNullOrEmpty(d.Major.Plant) ? homeDown : Plants.Down(st, d.Major.Plant)))
            {
                d.Major.Retreat = true;
                d.Major.Cancelled += d.Major.Remaining;
                d.Major.Remaining = 0;
            }
        }

        /// <summary>
        /// The three ways a large raid ends. Before E-18 there was one — every body dead — so a raid that WON never
        /// ended: its survivors stood on the wreck, the core could not be repaired, and every later opportunity
        /// was skipped with "Previous assault cleanup is still unresolved." (ENM-01).
        ///
        /// <list type="bullet">
        /// <item>CLEARED — it owes nothing and its last body is gone. Unchanged.</item>
        /// <item>CALLED OFF — the retreat has been called (the core fell, the target was lost, C-09 withdrew it).
        ///       It ends at once; the survivors keep walking off on their own.</item>
        /// <item>OVERRAN — it is <see cref="SiegeTuning.MajorOverrunS"/> past its planned end with bodies still
        ///       alive. It is called off. A hand-built test raid has no planned end (0) and never overruns.</item>
        /// </list>
        /// An ending that is not CLEARED also makes the next raid wait a full interval from now (U-D-64 d).
        /// </summary>
        private static void EndMajor(SimContext ctx, SimState st)
        {
            var d = st.Director;
            var r = ctx.Data.Raids;
            var a = d.Major;
            if (a == null || !a.Committed) return;

            var alive = EnemyQueries.GroupAlive(st, a.Id);
            RaidOutcome outcome;
            if (a.Retreat) outcome = DirectorRules.CalledOff(ctx, st, a.Plant);
            else if (a.Remaining == 0 && alive == 0) outcome = RaidOutcome.Cleared;
            else if (a.EndsAt > 0 && st.T >= a.EndsAt + ctx.Data.Siege.MajorOverrunS) outcome = RaidOutcome.BrokeOff;
            else return;

            d.History.Add(new RaidRecord
            {
                Id = a.Id, Started = a.StartsAt, Ended = st.T,
                Spawned = d.MajorSpawned, Outcome = (int)outcome,
            });
            // REL-84: the depth the saved record list is trimmed to is the exported RaidTuning.AssaultHistory (32),
            // the number the data tables already carried and nothing read.
            if (d.History.Count > r.AssaultHistory) d.History.RemoveAt(0);
            st.Events.Add(new RaidEndedEvent(st.T, a.Id, true, false, (int)outcome, a.StartsAt));   // REL-85
            d.LastMajorEnd = st.T;
            d.RecoveryUntil = Math.Max(d.RecoveryUntil, st.T + r.RecoveryS);
            d.Major = null;
            d.MajorSpawned = 0;
            // REL-75 (U-D-64 d): the quiet spell follows the recovery, and the cycle's small raid comes after it,
            // a short random while in, so it does not always land on the same second.
            var siege = ctx.Data.Siege;
            d.QuietUntil = Math.Max(d.QuietUntil, d.RecoveryUntil + siege.QuietAfterMajorS);
            d.NextMinor = d.QuietUntil + DirectorRules.RaidChoice(st.Seed, 4099 + d.Serial, (int)r.MinorRangeS);
            d.MinorDelayedSince = -1;

            if (outcome == RaidOutcome.Cleared) return;
            d.Serial++;
            d.NextStart = Math.Max(d.NextStart,
                st.T + r.IntervalMinS + DirectorRules.RaidChoice(st.Seed, d.Serial, (int)r.IntervalRangeS));
            if (alive > 0)
                Director.Say(ctx, st, outcome == RaidOutcome.Lost
                    ? "The core has fallen. The assault is withdrawing."
                    : "The assault has broken off.");
        }

        /// <summary>
        /// A survivor of a finished large raid that has still not left the map <see cref="SiegeTuning.WithdrawPurgeS"/>
        /// after it ended is removed without a kill — the C-09 purge, for the same reason: a body that cannot find
        /// its way out must not block the core's repair, or join the next raid, for ever.
        /// </summary>
        private static void PurgeStragglers(SimContext ctx, SimState st)
        {
            var d = st.Director;
            if (d.History.Count == 0) return;
            var purge = ctx.Data.Siege.WithdrawPurgeS;
            var list = st.Enemies.Actors;
            for (var i = list.Count - 1; i >= 0; i--)
            {
                var e = list[i];
                if (e.Layer != EnemyLayer.Major) continue;
                if (d.Major != null && d.Major.Id == e.Group) continue;
                for (var h = d.History.Count - 1; h >= 0; h--)
                {
                    if (d.History[h].Id != e.Group) continue;
                    if (st.T > d.History[h].Ended + purge) list.RemoveAt(i);
                    break;
                }
            }
        }

        /// <summary>
        /// Large raids the player actually saw off. Growth (U-D-47: "assaults survived") counts these and only
        /// these. INTERIM, owned by F1-30: a lost or broken-off raid earns no credit until the owner says what it is.
        /// </summary>
        public static int Survived(SimState st)
        {
            var n = 0;
            var h = st.Director.History;
            for (var i = 0; i < h.Count; i++) if (h[i].Outcome == (int)RaidOutcome.Cleared) n++;
            return n;
        }

        /// <summary>
        /// The number the next large raid is sized by: <see cref="Survived"/>, unless a developer has pinned it from
        /// the Admin panel (E-19). The pin is an Admin override like the others — never saved, reset on load — and
        /// it only matters at the moment a raid is BOOKED, because the plan is decided once and saved with the raid.
        /// </summary>
        public static int GrowthStep(SimState st) => st.Admin.RaidNumber >= 0 ? st.Admin.RaidNumber : Survived(st);

        // ------------------------------------------------------------------ clock

        /// <summary>
        /// Reference campaignThreat.ts:30 <c>initActiveRaidClock</c>. Called once, lazily: a Phase B save upgraded
        /// to schema v3 arrives with a fresh <see cref="DirectorState"/>, and seeding from that save's own
        /// <c>st.T</c> is what stops it inheriting a schedule that was never rolled.
        /// </summary>
        public static void Seed(SimContext ctx, SimState st)
        {
            var d = st.Director;
            var r = ctx.Data.Raids;
            var fresh = st.T <= 0;
            var min = fresh ? r.FirstMinS : r.IntervalMinS;
            var range = fresh ? r.FirstRangeS : r.IntervalRangeS;
            d.NextStart = st.T + min + DirectorRules.RaidChoice(st.Seed, 0, (int)range);
            d.NextMinor = st.T + r.GraceS;
            d.RecoveryUntil = st.T;
            d.Serial = 0;
        }

        /// <summary>
        /// Reference <c>future(reason)</c>: the opportunity is dropped and the clock rolls to a fresh interval with
        /// its own full warning. This is the no-accumulated-debt rule (U-D-38) and is deliberately NOT a deferral.
        /// </summary>
        private static void Future(SimContext ctx, SimState st, string reason)
        {
            var d = st.Director;
            var r = ctx.Data.Raids;
            d.Serial++;
            d.NextStart = st.T + r.IntervalMinS + DirectorRules.RaidChoice(st.Seed, d.Serial, (int)r.IntervalRangeS);
            Director.Say(ctx, st, reason + " Opportunity skipped; a future assault will receive a fresh warning.");
            st.Events.Add(new RaidNoticeEvent(st.T, RaidNoticeKind.Skipped, d.Notice, -1));
        }

        // ------------------------------------------------------------------ pacing (REL-75)

        /// <summary>
        /// The large-raid warning is due, but a pacing rule says it must wait (<see cref="DirectorPacing.WarningHold"/>).
        /// The clock is carried along one warning ahead of now, silently, so when the hold lifts the raid is booked
        /// with its full warning rather than a short one, and <see cref="Schedule"/> does not read the wait as a
        /// missed start. Nothing is said: a warning that has not opened is not news.
        /// </summary>
        private static bool HoldWarning(SimContext ctx, SimState st)
        {
            if (DirectorPacing.WarningHold(ctx, st).Length == 0) return false;
            st.Director.NextStart = st.T + ctx.Data.Raids.WarningS;
            return true;
        }

        // ------------------------------------------------------------------ major

        /// <summary>
        /// Book the next major assault — and, since GP-W3, VALIDATE IT BEFORE SAYING IT EXISTS.
        ///
        /// It used to announce first and look for ground second: <see cref="Approaches"/> could hand back a tile
        /// <see cref="DirectorRules.Staging"/> rejected, and the player was told an assault was coming that
        /// <see cref="Commit"/> then dropped. Now every approach is filtered through the same staging call the
        /// spawner will make, an assault with none is never created, and the announcement itself has moved to
        /// <see cref="Warn"/> so it can carry the approaches and the time.
        /// </summary>
        private static void Schedule(SimContext ctx, SimState st)
        {
            var d = st.Director;
            var r = ctx.Data.Raids;
            if (st.T > d.NextStart || d.RecoveryUntil > d.NextStart) { Future(ctx, st, "Recovery overlaps the next start."); return; }
            if (d.Reserved)
            {
                Future(ctx, st, d.ReserveReason.Length > 0 ? d.ReserveReason : "The approach is reserved.");
                return;
            }
            // FRT-09 (design §5.8): the first assault booked after a commissioning goes for the newest plant; after
            // it commits, normal selection — the Home core — resumes.
            var aim = Plants.RaidAim(st);
            if (!DirectorRules.Target(ctx, st, aim, out _, out _, out _))
            {
                Future(ctx, st, "No operational core with a reachable assault approach.");
                return;
            }
            var origins = Stageable(ctx, st, DirectorRules.Approaches(ctx, st, aim), aim);
            if (origins.Length == 0) { Future(ctx, st, "No operational core with a reachable assault approach."); return; }
            var a = new MajorRaid
            {
                Id = d.NextId++,
                Origin = origins[0],
                Origins = origins,
                StartsAt = d.NextStart,
                Retreat = false,
                Committed = false,
                Plant = aim,
            };
            // GP-W4: the whole encounter is planned here, once, and saved — head count, composition, wave times,
            // approaches and length. Spawning afterwards only reads the plan, so what the player is warned about
            // is what arrives, and a save taken inside the assault resumes the same assault.
            SiegePlan.Build(ctx, st, a, GrowthStep(st));
            d.Major = a;
        }

        /// <summary>The approaches a wave can actually be born on, in order. Empty when the map offers none.</summary>
        private static int[] Stageable(SimContext ctx, SimState st, int[] origins, string aim)
        {
            if (origins == null || origins.Length == 0) return Array.Empty<int>();
            var keep = new System.Collections.Generic.List<int>(origins.Length);
            for (var i = 0; i < origins.Length; i++)
                if (DirectorRules.Staging(ctx, st, origins[i], aim) >= 0) keep.Add(origins[i]);
            return keep.ToArray();
        }

        /// <summary>
        /// Turn the announced assault into a live one, or drop it and say WHY.
        ///
        /// Two GP-W3 changes. The reason is now specific — "blocked by cleanup, recovery or its approach" told the
        /// player nothing they could act on, and named three causes for one event. And a minor raid still on the
        /// map no longer cancels an assault the player has already been warned about: the brief's rule is that
        /// what is announced is what arrives, so the raiders are told to withdraw and the assault goes ahead. It is
        /// still one wave at a time, which is the reference's constraint; it is simply the assault that keeps the
        /// slot, rather than the leftovers that were there first.
        /// </summary>
        private static void Commit(SimContext ctx, SimState st)
        {
            var d = st.Director;
            var r = ctx.Data.Raids;
            var a = d.Major;
            if (a == null || a.Committed || st.T < a.StartsAt) return;

            var problem = d.Reserved
                    ? (d.ReserveReason.Length > 0 ? d.ReserveReason : "The approach is reserved.")
                : st.T < d.RecoveryUntil ? "The last attack is still being cleared up."
                : !DirectorRules.Target(ctx, st, a.Plant, out _, out _, out _)
                    ? (string.IsNullOrEmpty(a.Plant) ? "There is no core left to assault." : "There is no plant left to assault.")
                : !HasStaging(ctx, st, a) ? "The announced approaches are no longer open."
                : "";
            if (problem.Length > 0)
            {
                d.Major = null;
                d.MajorSpawned = 0;
                Future(ctx, st, problem);
                return;
            }

            // The wave that was already here gives way to the one the player was warned about.
            if (d.Minor != null && !d.Minor.Retreat)
            {
                if (!d.Minor.Spawned) d.Minor = null;                 // a warning that never became bodies
                else { d.Minor.Retreat = true; d.Minor.Owed = 0; }
            }

            a.Committed = true;
            if (!string.IsNullOrEmpty(a.Plant) && a.Plant == d.PlantRaid) d.PlantRaid = "";   // FRT-09: its raid came
            d.CycleMinors = 0;                                          // REL-75: a new cycle starts with this raid
            d.Serial++;
            d.NextStart = a.StartsAt + r.IntervalMinS + DirectorRules.RaidChoice(st.Seed, d.Serial, (int)r.IntervalRangeS);
        }

        private static bool HasStaging(SimContext ctx, SimState st, MajorRaid a)
        {
            var list = a.Origins != null && a.Origins.Length > 0 ? a.Origins : new[] { a.Origin };
            for (var i = 0; i < list.Length; i++)
                if (DirectorRules.Staging(ctx, st, list[i], a.Plant) >= 0) return true;
            return false;
        }

        /// <summary>
        /// Walk the assault's saved wave plan on (GP-W4).
        ///
        /// It used to emit one flat roster: <c>Total</c> identical bodies evenly spaced across the exported
        /// <c>WindowS</c>, one approach per body in rotation, the kind decided by the legacy every-third-body rule.
        /// Now every body's second, side and kind comes from <see cref="SiegePlan"/>, which decided them when the
        /// assault was scheduled. Bodies arrive in ROWS — one per approach at the same second — so a wave presses
        /// several positions at once instead of queueing down one lane.
        /// </summary>
        private static void Spawn(SimContext ctx, SimState st)
        {
            var d = st.Director;
            var r = ctx.Data.Raids;
            var a = d.Major;
            if (a == null || !a.Committed) return;
            if (st.T >= a.EndsAt) { a.Cancelled += a.Remaining; a.Remaining = 0; }
            if (a.Retreat || a.Remaining <= 0) return;
            if (!SiegePlan.Planned(a)) SiegePlan.Residual(a, st.T);

            // A wave that has finished walking on gives way to the next; the last one simply runs out.
            while (a.Wave < a.Waves - 1 && a.WaveSpawned >= a.WaveCount[a.Wave])
            {
                a.Wave++;
                a.WaveSpawned = 0;
            }
            AnnounceWave(ctx, st, a);

            if (a.WaveSpawned >= a.WaveCount[a.Wave]) return;
            if (st.T < SiegePlan.Due(ctx, a, a.Wave, a.WaveSpawned)) return;
            if (st.Enemies.Actors.Count >= r.LivingBudget) return;
            if (MajorAlive(st) >= r.ActiveRaidBudget) return;
            if (!DirectorRules.Target(ctx, st, a.Plant, out _, out _, out _)) { a.Retreat = true; a.Remaining = 0; return; }

            // The planned side first. If the map has closed it since the warning, the body takes another of the
            // SAME wave's announced approaches rather than stalling the assault or inventing a new one.
            var born = 0;
            var sides = SiegePlan.Approaches(a, a.Wave);
            for (var attempt = 0; attempt < sides && born == 0; attempt++)
            {
                var from = DirectorRules.Staging(ctx, st, SiegePlan.Origin(a, a.Wave, a.WaveSpawned + attempt), a.Plant);
                if (from < 0) continue;
                born = DirectorRules.Birth(ctx, st, from, (int)EnemyLayer.Major, a.Id, false,
                    SiegePlan.Kind(a, a.Wave, a.WaveSpawned));
            }
            if (born == 0) return;

            a.Remaining--;
            a.WaveSpawned++;
            d.MajorSpawned++;
            a.NextSpawn = SiegePlan.Due(ctx, a, a.Wave, a.WaveSpawned);
        }

        /// <summary>
        /// Name each wave as it opens, once. <see cref="DirectorState.WarnedId"/> already covers the assault's
        /// inbound warning, which names wave 1; waves after it get their own line so the player can hear the
        /// pressure move rather than discovering a new side by losing a turret on it.
        /// </summary>
        private static void AnnounceWave(SimContext ctx, SimState st, MajorRaid a)
        {
            while (a.WaveAnnounced <= a.Wave && a.WaveAnnounced < a.Waves)
            {
                var w = a.WaveAnnounced;
                if (st.T < a.WaveStart[w]) return;
                a.WaveAnnounced = w + 1;
                // The HUD marker follows the wave that is actually walking in.
                a.Origin = SiegePlan.Origin(a, w, 0);
                if (w == 0) continue;                       // the inbound warning already named the opening wave
                var text = "Assault wave " + (w + 1) + " of " + a.Waves + " from "
                    + DirectorRules.HeadingsOf(ctx, st, SiegePlan.Sides(a, w), a.Plant) + ".";
                Director.Say(st, text, a.EndsAt + ctx.Data.Siege.NoticeHoldS);
                st.Events.Add(new RaidNoticeEvent(st.T, RaidNoticeKind.Announced, text, a.Id));
            }
        }

        private static int MajorAlive(SimState st)
        {
            var n = 0;
            for (var i = 0; i < st.Enemies.Actors.Count; i++)
                if (st.Enemies.Actors[i].Layer == EnemyLayer.Major) n++;
            return n;
        }

        /// <summary>
        /// Reference <c>receiveWarning</c>, reduced to what the port keeps: the radio network and its composition
        /// intelligence are retired, so the warning is announced once per assault.
        ///
        /// GP-W3 gave it something to say. The old body set <see cref="DirectorState.WarnedId"/> and emitted
        /// nothing at all, while <see cref="Schedule"/> published a bare "Assault announced." before it knew where
        /// the assault could come from. The announcement is here now, after the approaches have been staged, and
        /// names them and the time — so what the player is told is what arrives.
        /// </summary>
        private static void Warn(SimContext ctx, SimState st)
        {
            var d = st.Director;
            var a = d.Major;
            if (a == null || d.WarnedId == a.Id) return;
            d.WarnedId = a.Id;
            // GP-W4: name the side the FIRST wave actually opens on, not every side the assault will eventually
            // use — an assault announced from four quarters at once is not something a player can prepare for.
            // The scale is stated separately, so preparation can be proportionate.
            var opening = DirectorRules.HeadingsOf(ctx, st, SiegePlan.Sides(a, 0), a.Plant);
            var text = "Major assault" + OnPlant(a) + " inbound from " + opening + " in " + Seconds(a.StartsAt - st.T) + ".";
            if (a.Waves > 1)
            {
                var all = DirectorRules.HeadingsOf(ctx, st, a.Origins, a.Plant);
                text += " " + a.Waves + " waves over " + Minutes(a.EndsAt - a.StartsAt)
                    + (all == opening ? "." : ", later ones from " + all + ".");
            }
            Director.Say(st, text, a.EndsAt + ctx.Data.Siege.NoticeHoldS);
            st.Events.Add(new RaidNoticeEvent(st.T, RaidNoticeKind.Announced, text, a.Id));
        }

        /// <summary>FRT-09: " on Riverside Works" for an assault on a plant, so the player knows where to stand; "" for Home.</summary>
        private static string OnPlant(MajorRaid a) =>
            string.IsNullOrEmpty(a.Plant) ? "" : " on " + (EncounterCatalogue.Plant(a.Plant)?.Name ?? a.Plant);

        /// <summary>A countdown as the HUD says it: whole seconds, never negative.</summary>
        private static string Seconds(double s) => Math.Max(0, (int)Math.Round(s)) + " s";

        /// <summary>A duration in whole minutes, never under one, for stating the scale of an encounter.</summary>
        private static string Minutes(double s) => Math.Max(1, (int)Math.Round(s / 60)) + " min";

        // ------------------------------------------------------------------ minor

        /// <summary>
        /// A warned raid whose moment has come. It re-stages on the approach it was announced on — the map may
        /// have changed during the warning, which is the point of a warning — and keeps trying for
        /// <see cref="SiegeTuning.MinorStageRetryS"/> before it is honestly dropped rather than left holding the
        /// slot. The bodies arrive on the announced heading or not at all.
        /// </summary>
        private static void MinorArrive(SimContext ctx, SimState st)
        {
            var d = st.Director;
            var m = d.Minor;
            if (m == null || m.Owed <= 0 || st.T < m.StartsAt) return;
            var giveUp = st.T > m.StartsAt + ctx.Data.Siege.MinorStageRetryS;

            var from = DirectorRules.Staging(ctx, st, m.Origin);
            if (from < 0)
            {
                // The announced approach has closed. Look for another on the SAME heading before giving up, so a
                // raid announced from the north never arrives from the south.
                var alt = DirectorRules.Origin(ctx, st);
                if (alt >= 0 && DirectorRules.HeadingsOf(ctx, st, new[] { alt }) == m.Heading)
                    from = DirectorRules.Staging(ctx, st, alt);
                if (from >= 0) m.Origin = from;
            }

            if (from < 0)
            {
                if (!giveUp) return;
                Director.Say(ctx, st, "The raid from " + m.Heading + " could not reach the base and turned back.");
                st.Events.Add(new RaidNoticeEvent(st.T, RaidNoticeKind.Skipped, d.Notice, m.Id));
                d.Minor = null;
                return;
            }

            while (m.Owed > 0 && st.Enemies.Actors.Count < ctx.Data.Raids.LivingBudget)
            {
                if (DirectorRules.Birth(ctx, st, from, (int)EnemyLayer.Minor, m.Id, false) == 0) break;
                m.Owed--;
                // REL-75: the cycle's small raid is the one that actually arrives, not the one that was announced.
                if (!m.Spawned && !m.Scripted) d.CycleMinors++;
                m.Spawned = true;
            }
            // Whatever the map would not take is written off here, not carried as a debt (U-D-38).
            if (giveUp || m.Owed <= 0)
            {
                if (!m.Spawned)
                {
                    Director.Say(ctx, st, "The raid from " + m.Heading + " could not reach the base and turned back.");
                    st.Events.Add(new RaidNoticeEvent(st.T, RaidNoticeKind.Skipped, d.Notice, m.Id));
                    d.Minor = null;
                    return;
                }
                m.Owed = 0;
            }
        }

        /// <summary>
        /// Book the next minor raid and WARN ABOUT IT. Before GP-W3 this function birthed the bodies in the same
        /// tick as the "Minor raid inbound." notice, which is not a warning — the brief asks for 30–45 s of
        /// preparation and an accurate approach, so the raid is now created unspawned, with its arrival time, its
        /// body count and the compass word its bodies will actually walk in on, and <see cref="MinorArrive"/>
        /// makes good on all three. The whole warning is saved, so a save taken inside one resumes inside it.
        /// </summary>
        private static void MinorAnnounce(SimContext ctx, SimState st)
        {
            var d = st.Director;
            var r = ctx.Data.Raids;
            var siege = ctx.Data.Siege;
            if (st.T < d.NextMinor) return;
            d.NextMinor = st.T + r.MinorMinS
                + DirectorRules.RaidChoice(st.Seed, d.RaidsStarted + (int)Math.Floor(st.T), (int)r.MinorRangeS);
            if (!MinorMayStart(ctx, st, out var warn, out var origin, out var count))
            {
                d.MinorDelayedSince = -1;
                return;
            }

            // REL-75 (U-D-66 (5)): not while the engineer is down or fighting a camp or a stronghold. The raid is
            // held, not dropped, and tried again each second, for at most MinorDelayCapS; then it starts anyway.
            if (DirectorPacing.Unfair(ctx, st))
            {
                if (d.MinorDelayedSince < 0) d.MinorDelayedSince = st.T;
                if (st.T < d.MinorDelayedSince + siege.MinorDelayCapS)
                {
                    d.NextMinor = st.T + 1;
                    return;
                }
            }
            d.MinorDelayedSince = -1;

            var id = d.NextId++;
            var heading = DirectorRules.HeadingsOf(ctx, st, new[] { origin });
            d.Minor = new MinorRaid
            {
                Id = id,
                Origin = origin,
                Retreat = false,
                Scripted = false,
                StartsAt = st.T + warn,
                Owed = count,
                Spawned = false,
                Heading = heading,
                // REL-75 (U-D-68 b): the campaign's first ordinary small raid cannot take the core below its floor.
                Floor = d.RaidsStarted == 0 ? DirectorPacing.FirstFloor(ctx, st) : 0,
            };
            d.RaidsStarted++;
            var text = "Raid inbound from " + heading + " in " + Seconds(warn) + ".";
            Director.Say(st, text, d.Minor.StartsAt + siege.NoticeHoldS);
            st.Events.Add(new RaidNoticeEvent(st.T, RaidNoticeKind.MinorRaid, text, id));
        }

        /// <summary>
        /// Everything that decides whether a small raid can be announced now, and if so its warning, approach and
        /// size. Split out of <see cref="MinorAnnounce"/> (REL-75) so the fair-timing delay is asked only of a raid
        /// that would otherwise start: a delay clock must not run while nothing was due.
        /// </summary>
        private static bool MinorMayStart(SimContext ctx, SimState st, out double warn, out int origin, out int count)
        {
            var d = st.Director;
            var r = ctx.Data.Raids;
            var siege = ctx.Data.Siege;
            warn = 0; origin = -1; count = 0;
            if (st.OpeningResourceVersion > 0 && st.Opening.EndedAt < 0) return false;
            if (d.Major != null || d.Minor != null || d.Reserved) return false;
            if (st.T < d.RecoveryUntil) return false;
            // REL-75 (U-D-64 d): nothing in the quiet spell, and one small raid per cycle once large raids have begun.
            if (st.T < d.QuietUntil) return false;
            var cycle = DirectorPacing.InCycle(st);
            if (cycle && d.CycleMinors >= siege.MinorsPerCycle) return false;

            warn = siege.MinorWarningS
                + DirectorRules.RaidChoice(st.Seed, d.RaidsStarted + 1013, (int)siege.MinorWarningRangeS);
            // REL-75 (U-D-66 (5)): a raid on a target far from the engineer gives them time to get back to it.
            if (DirectorPacing.Far(ctx, st)) warn += siege.FarWarningExtraS;
            // Before the first large raid, the raid must still be clear of that raid's own warning window once its
            // warning has run out. Inside a cycle the large-raid warning waits for the small raid instead
            // (DirectorPacing.WarningHold), so the clock is carried along and this test would refuse every raid.
            if (!cycle && d.NextStart - (st.T + warn) < r.WarningS + siege.MinorMajorGapS) return false;
            if (!DirectorRules.Target(ctx, st, out _, out _, out _)) return false;

            origin = DirectorRules.Origin(ctx, st);
            if (origin < 0 || DirectorRules.Staging(ctx, st, origin) < 0) return false;
            count = (int)r.MinorCountBase + DirectorRules.RaidChoice(st.Seed, d.RaidsStarted, (int)r.MinorCountRange);
            return st.Enemies.Actors.Count + count <= r.LivingBudget;
        }
    }
}
