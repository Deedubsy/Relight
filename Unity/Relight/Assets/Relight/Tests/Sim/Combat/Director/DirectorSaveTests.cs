using System.Collections.Generic;
using NUnit.Framework;
using Relight.Sim.Tests.Persistence;

namespace Relight.Sim.Tests.Combat
{
    /// <summary>
    /// GP-W3, "preserve encounter state across save/load so warnings and waves cannot duplicate or disappear".
    ///
    /// A minor raid is now a WARNING first and a WAVE second, and both halves live in the save. That creates two
    /// ways to break the brief's rule that what is announced is what arrives: a save taken inside a warning could
    /// come back without it (the raid disappears) or come back and be announced again (the raid duplicates); and a
    /// version-6 save, written before the split existed, carries bodies already on the map with none of the new
    /// members, so a careless default would have the director wait for a wave that is already there and then birth
    /// a second one. Both are pinned here, against the real phase rather than against the visitor.
    /// </summary>
    public sealed class DirectorSaveTests
    {
        private static List<ITickPhase> Clock() => new List<ITickPhase> { new DirectorPhase() };

        /// <summary>The state text of a save document, with <paramref name="drop"/> removed and re-stamped.</summary>
        private static string Downgrade(SimState st, int toVersion, params string[] drop)
        {
            var doc = JsonValue.Parse(PersistenceFixture.Canonical(st), out var error);
            Assert.That(error, Is.Null);
            var text = Without(doc, "", new HashSet<string>(drop));
            return PersistenceFixture.Retarget(text, "version", toVersion.ToString(System.Globalization.CultureInfo.InvariantCulture));
        }

        /// <summary>Canonical text of <paramref name="v"/> without the dotted paths in <paramref name="drop"/>.</summary>
        private static string Without(JsonValue v, string path, HashSet<string> drop)
        {
            if (!v.IsObject) return v.ToCanonicalJson();
            var parts = new List<string>();
            foreach (var key in v.Keys)
            {
                var child = path.Length == 0 ? key : path + "." + key;
                if (drop.Contains(child)) continue;
                parts.Add(CanonicalJsonWriter.QuoteString(key) + ":" + Without(v.Member(key), child, drop));
            }
            return "{" + string.Join(",", parts) + "}";
        }

        /// <summary>A whole save document carrying <paramref name="state"/>, stamped and checksummed as written.</summary>
        private static string Document(SimState st, GameData data, string state, int version)
        {
            var doc = SaveSerializer.WriteText(st, data);
            doc = PersistenceFixture.Retarget(doc, "state", state);
            doc = PersistenceFixture.Retarget(doc, "version", version.ToString(System.Globalization.CultureInfo.InvariantCulture));
            return PersistenceFixture.Retarget(doc, "hash", CanonicalJsonWriter.QuoteString(StateHash.Of(state)));
        }

        /// <summary>
        /// A save taken inside a raid warning resumes inside the same warning — same raid, same approach, same
        /// arrival second — and the bodies arrive once, on the announced heading. The unsaved run is played
        /// alongside as the control: a resumed game and a game that was never saved get the same raid.
        /// </summary>
        [Test]
        public void ASaveTakenInsideARaidWarningResumesOnTheSameWarningAndArrivesExactlyOnce()
        {
            var ctx = RaidFixture.Context();
            var st = RaidFixture.State(ctx, 3);
            st.T = st.Director.NextMinor;
            RaidFixture.Run(ctx, st, 1, Clock());

            var m = st.Director.Minor;
            Assert.That(m, Is.Not.Null, "a raid was announced");
            Assert.That(m.Spawned, Is.False, "and it is still a warning");

            var load = SaveSerializer.ReadText(SaveSerializer.WriteText(st, ctx.Data), ctx.Data);
            Assert.That(load.Ok, Is.True, load.Reason);
            var resumed = load.State;
            var saved = resumed.Director.Minor;

            Assert.That(saved, Is.Not.Null, "the warning survived the save");
            Assert.That(saved.Id, Is.EqualTo(m.Id));
            Assert.That(saved.Origin, Is.EqualTo(m.Origin));
            Assert.That(saved.StartsAt, Is.EqualTo(m.StartsAt).Within(1e-9), "it arrives when it said it would");
            Assert.That(saved.Owed, Is.EqualTo(m.Owed), "and brings what it said it would");
            Assert.That(saved.Spawned, Is.False);
            Assert.That(saved.Heading, Is.EqualTo(m.Heading));
            Assert.That(resumed.Director.NoticeUntil, Is.EqualTo(st.Director.NoticeUntil).Within(1e-9),
                "including how long the notice is worth reading");
            Assert.That(resumed.Director.Notice, Is.EqualTo(st.Director.Notice));
            Assert.That(resumed.Director.NextId, Is.EqualTo(st.Director.NextId), "no second raid can take this id");

            // Play both to the arrival. The resumed game must get exactly the raid it was promised, once.
            var ticks = (int)System.Math.Ceiling((m.StartsAt - st.T) / RaidFixture.Dt) + 2;
            RaidFixture.Run(ctx, resumed, ticks, Clock());
            RaidFixture.Run(ctx, st, ticks, Clock());

            Assert.That(resumed.Director.Minor, Is.Not.Null);
            Assert.That(resumed.Director.Minor.Id, Is.EqualTo(m.Id), "the same raid arrived, not a fresh one");
            Assert.That(resumed.Director.Minor.Spawned, Is.True);
            Assert.That(resumed.Director.NextId, Is.EqualTo(st.Director.NextId), "and no extra raid was created");
            Assert.That(EnemyQueries.GroupAlive(resumed, m.Id), Is.EqualTo(EnemyQueries.GroupAlive(st, m.Id)),
                "the resumed game gets the same bodies as the game that was never saved");
            Assert.That(resumed.Enemies.Actors.Count, Is.EqualTo(EnemyQueries.GroupAlive(resumed, m.Id)),
                "and nothing else was born alongside them");
            Assert.That(DirectorRules.HeadingsOf(ctx, resumed, new[] { resumed.Director.Minor.Origin }),
                Is.EqualTo(m.Heading), "from the quarter the pre-save warning named");

            var announcements = 0;
            for (var i = 0; i < resumed.Events.Count; i++)
                if (resumed.Events[i] is RaidNoticeEvent e && e.Kind == RaidNoticeKind.MinorRaid) announcements++;
            Assert.That(announcements, Is.Zero, "a warning that was already given is not given again after a load");
        }

        /// <summary>
        /// A version-6 save whose bodies were already walking keeps them as the wave they are. The four members the
        /// split added are absent from such a file; filling them from a fresh state would say "not arrived yet,
        /// owing nothing", and the director would neither clean the wave up nor let the next one be announced.
        /// </summary>
        [Test]
        public void AVersionSixSaveWhoseRaidWasAlreadyOnTheMapKeepsItAsAWaveInsteadOfAnnouncingItAgain()
        {
            var ctx = RaidFixture.Context();
            var st = RaidFixture.State(ctx, 3);
            st.T = 1234.5;                                  // a distinctive time: the wave must arrive at THIS one
            st.Tick = (int)(st.T * 20);
            for (var i = 0; i < 7; i++)
                RaidFixture.Body(st, "biter", RaidFixture.CoreX + 24 + i, RaidFixture.CoreY + 24, EnemyLayer.Minor, 4);
            st.Director.NextId = 5;
            // Nothing else is due: this test is about the upgrade, not about the clock.
            st.Director.NextStart = st.T + 5000;
            st.Director.NextMinor = st.T + 5000;
            var wave = st.Director.Minor;
            Assert.That(wave, Is.Not.Null);
            Assert.That(EnemyQueries.GroupAlive(st, wave.Id), Is.EqualTo(7));

            var state6 = Downgrade(st, 6,
                "director.noticeUntil", "director.minor.startsAt", "director.minor.owed",
                "director.minor.spawned", "director.minor.heading");
            Assert.That(state6, Does.Not.Contain("\"spawned\""), "the forged file is honestly a version-6 shape");
            Assert.That(state6, Does.Not.Contain("\"noticeUntil\""));

            var load = SaveSerializer.ReadText(Document(st, ctx.Data, state6, 6), ctx.Data);
            Assert.That(load.Ok, Is.True, load.Reason);
            Assert.That(load.Header.Version, Is.EqualTo(6));
            Assert.That(load.Upgraded, Does.Contain("version 6"));
            Assert.That(load.Upgraded, Does.Contain("kept as a wave"));
            Assert.That(load.Warning, Is.Null);

            var r = load.State.Director;
            Assert.That(r.Minor, Is.Not.Null, "the raid that was on the map is still on the map");
            Assert.That(r.Minor.Id, Is.EqualTo(wave.Id));
            Assert.That(r.Minor.Spawned, Is.True, "it has arrived: the bodies are in the same file");
            Assert.That(r.Minor.Owed, Is.Zero, "and it owes the map no more");
            Assert.That(r.Minor.StartsAt, Is.EqualTo(st.T).Within(1e-9), "it arrived at the save's own time");
            Assert.That(r.NoticeUntil, Is.Zero, "a file with no expiry has nothing to expire");
            Assert.That(EnemyQueries.GroupAlive(load.State, wave.Id), Is.EqualTo(7));

            // The director must now treat it as an ordinary live wave: no second raid while it is up, and the slot
            // released once the last body is gone.
            RaidFixture.Run(ctx, load.State, 20, Clock());
            Assert.That(load.State.Director.Minor, Is.Not.Null, "the wave was not thrown away");
            Assert.That(load.State.Director.Minor.Id, Is.EqualTo(wave.Id));
            Assert.That(load.State.Enemies.Actors.Count, Is.EqualTo(7), "and no second wave was born on top of it");
            Assert.That(load.State.Director.NextId, Is.EqualTo(5), "nothing new was even booked");

            load.State.Enemies.Actors.Clear();
            RaidFixture.Run(ctx, load.State, 1, Clock());
            Assert.That(load.State.Director.Minor, Is.Null, "the last body gone ends the wave, so the next can come");
        }

        /// <summary>
        /// GP-W4. A save taken in the middle of a major assault resumes THAT assault: the same waves, on the same
        /// seconds, from the same sides, owing exactly what it still owed. Spawning is a pure function of the saved
        /// plan and its two counters, so nothing it already sent can be sent again and nothing it still owes can be
        /// dropped.
        /// </summary>
        [Test]
        public void ASaveTakenInsideAnAssaultResumesTheSameWavesOwingExactlyWhatItStillOwed()
        {
            var ctx = RaidFixture.Context();
            var st = RaidFixture.State(ctx, 3);
            var d = st.Director;
            st.T = d.NextStart - ctx.Data.Raids.WarningS;
            RaidFixture.Run(ctx, st, 1, Clock());
            var a = d.Major;
            Assert.That(a, Is.Not.Null);

            st.T = a.StartsAt;
            Open(ctx, st, 4);
            Assert.That(a.Committed, Is.True, "it is under way");
            Assert.That(a.Wave, Is.Zero, "and still inside its first wave");

            var load = SaveSerializer.ReadText(SaveSerializer.WriteText(st, ctx.Data), ctx.Data);
            Assert.That(load.Ok, Is.True, load.Reason);
            var b = load.State.Director.Major;

            Assert.That(b, Is.Not.Null, "the assault survived the save");
            Assert.That(b.Id, Is.EqualTo(a.Id));
            Assert.That(b.Remaining, Is.EqualTo(a.Remaining), "owing exactly what it owed");
            Assert.That(b.WaveCount, Is.EqualTo(a.WaveCount), "the same waves");
            Assert.That(b.WaveSpitters, Is.EqualTo(a.WaveSpitters), "carrying the same enemies");
            Assert.That(b.WaveBreakers, Is.EqualTo(a.WaveBreakers), "and the same heavies among them");
            Assert.That(b.WaveApproaches, Is.EqualTo(a.WaveApproaches), "on the same number of approaches");
            Assert.That(b.WaveOffset, Is.EqualTo(a.WaveOffset), "starting from the same one");
            Assert.That(b.WaveStart, Is.EqualTo(a.WaveStart), "at the same absolute seconds");
            Assert.That(b.Wave, Is.EqualTo(a.Wave));
            Assert.That(b.WaveSpawned, Is.EqualTo(a.WaveSpawned), "and having already sent what it sent");
            Assert.That(b.WaveAnnounced, Is.EqualTo(a.WaveAnnounced));
            Assert.That(load.State.Director.MajorSpawned, Is.EqualTo(d.MajorSpawned));

            // Play both on together: the resumed game must get the rest of the assault, once, and no more.
            var resumed = load.State;
            var ticks = (int)((a.EndsAt - st.T + 60) / RaidFixture.Dt);
            var bornResumed = Spend(ctx, resumed, ticks);
            var bornControl = Spend(ctx, st, ticks);
            Assert.That(bornResumed, Is.EqualTo(bornControl),
                "a resumed assault sends exactly what the assault that was never saved sends");
            Assert.That(bornResumed, Is.EqualTo(a.Total - 4), "the roster less the four already on the map");
            Assert.That(resumed.Director.History.Count, Is.EqualTo(1), "and it ends once, like the control");
        }

        /// <summary>
        /// A version-7 save taken inside an assault resumes the assault it was in, not a fresh four-wave one. The
        /// plan members are absent from such a file and a fresh state has no <c>major</c> object to default them
        /// from, so the schema-8 step has to stamp them — as the one wave the old build was already sending.
        /// </summary>
        [Test]
        public void AVersionSevenSaveInsideAnAssaultKeepsItAsTheSingleWaveItWasAlreadySending()
        {
            var ctx = RaidFixture.Context();
            var st = RaidFixture.State(ctx, 3);
            var d = st.Director;
            st.T = d.NextStart - ctx.Data.Raids.WarningS;
            RaidFixture.Run(ctx, st, 1, Clock());
            var a = d.Major;
            st.T = a.StartsAt;
            Open(ctx, st, 4);

            var owed = a.Remaining;
            var alive = EnemyQueries.GroupAlive(st, a.Id);
            var sides = a.Origins.Length;
            var nextId = d.NextId;
            // Nothing else may be due: this test is about the upgrade, not about the clock.
            d.NextStart = st.T + 5000;
            d.NextMinor = st.T + 5000;

            var state7 = Downgrade(st, 7,
                "director.major.wave", "director.major.waveSpawned", "director.major.waveAnnounced",
                "director.major.waveCount", "director.major.waveSpitters", "director.major.waveStart",
                "director.major.waveApproaches", "director.major.waveOffset");
            Assert.That(state7, Does.Not.Contain("\"waveCount\""), "the forged file is honestly a version-7 shape");

            var load = SaveSerializer.ReadText(Document(st, ctx.Data, state7, 7), ctx.Data);
            Assert.That(load.Ok, Is.True, load.Reason);
            Assert.That(load.Header.Version, Is.EqualTo(7));
            Assert.That(load.Upgraded, Does.Contain("version 7"));
            Assert.That(load.Upgraded, Does.Contain("single wave"));

            var b = load.State.Director.Major;
            Assert.That(b, Is.Not.Null, "the assault that was under way is still under way");
            Assert.That(b.Id, Is.EqualTo(a.Id));
            Assert.That(b.Remaining, Is.EqualTo(owed), "owing what the file said it owed");
            Assert.That(b.Total, Is.EqualTo(a.Total), "and no more than the file's own roster");
            Assert.That(b.StartsAt, Is.EqualTo(a.StartsAt).Within(1e-9));
            Assert.That(b.EndsAt, Is.EqualTo(a.EndsAt).Within(1e-9), "on the clock it was already running on");
            Assert.That(b.Waves, Is.EqualTo(1), "as one wave, not a fresh four-wave encounter");
            Assert.That(b.WaveCount[0], Is.EqualTo(owed));
            Assert.That(b.WaveSpitters[0], Is.EqualTo(owed / 3), "with the old build's every-third-body split");
            Assert.That(b.WaveStart[0], Is.EqualTo(st.T).Within(1e-9), "resuming now");
            Assert.That(b.WaveApproaches[0], Is.EqualTo(sides), "from every approach it was announced from");
            Assert.That(b.WaveAnnounced, Is.EqualTo(1), "already announced, so it is not announced again");
            Assert.That(EnemyQueries.GroupAlive(load.State, a.Id), Is.EqualTo(alive), "its bodies came with it");

            var born = Spend(ctx, load.State, (int)((a.EndsAt - st.T + 60) / RaidFixture.Dt));
            Assert.That(born, Is.EqualTo(owed), "exactly what it still owed arrived — no more, no fewer");
            Assert.That(load.State.Director.NextId, Is.EqualTo(nextId), "and no second assault was booked");
            var announced = 0;
            for (var i = 0; i < load.State.Events.Count; i++)
                if (load.State.Events[i] is RaidNoticeEvent e && e.Kind == RaidNoticeKind.Announced) announced++;
            Assert.That(announced, Is.Zero, "an assault already announced is not announced a second time");
        }

        /// <summary>Runs the director until the assault has put <paramref name="bodies"/> on the map.</summary>
        private static void Open(SimContext ctx, SimState st, int bodies)
        {
            var clock = Clock();
            for (var i = 0; i < 20 * 600 && st.Director.MajorSpawned < bodies; i++)
                RaidFixture.Run(ctx, st, 1, clock);
            Assert.That(st.Director.MajorSpawned, Is.EqualTo(bodies), "the assault started sending bodies");
        }

        /// <summary>
        /// Runs the director alone and returns how many bodies the assault puts on the map from here on. The map is
        /// emptied first and after every tick, which is what stops the active-raid budget from masking a miscount;
        /// nothing else is ticked, so nothing dies early and no body is ever counted twice.
        /// </summary>
        private static int Spend(SimContext ctx, SimState st, int ticks)
        {
            var clock = Clock();
            var born = 0;
            st.Enemies.Actors.Clear();
            for (var i = 0; i < ticks; i++)
            {
                RaidFixture.Run(ctx, st, 1, clock);
                born += st.Enemies.Actors.Count;
                st.Enemies.Actors.Clear();
                if (st.Director.Major == null) break;
            }
            return born;
        }

        /// <summary>
        /// A notice about an encounter that has been and gone is cleared. Every notice says how long it is worth
        /// reading, the hold is an absolute sim second, and it survives a save — so a game resumed an hour later
        /// does not open on "assault inbound".
        /// </summary>
        [Test]
        public void AStaleNoticeIsClearedOnceItHasStoppedBeingNewsAndItsHoldSurvivesASave()
        {
            var ctx = RaidFixture.Context();
            var st = RaidFixture.State(ctx, 3);
            st.T = 100;
            Director.Say(st, "Raid inbound from N in 40 s.", st.T + 5);
            Assert.That(st.Director.Notice, Is.Not.Empty);
            Assert.That(st.Director.NoticeUntil, Is.EqualTo(105).Within(1e-9));

            RaidFixture.Run(ctx, st, 20, Clock());          // one second: still news
            Assert.That(st.Director.Notice, Is.Not.Empty, "a notice is not cleared the tick after it is written");

            var load = SaveSerializer.ReadText(SaveSerializer.WriteText(st, ctx.Data), ctx.Data);
            Assert.That(load.Ok, Is.True, load.Reason);
            Assert.That(load.State.Director.Notice, Is.EqualTo(st.Director.Notice));
            Assert.That(load.State.Director.NoticeUntil, Is.EqualTo(105).Within(1e-9), "the hold is saved, not restarted");

            load.State.T = 106;
            RaidFixture.Run(ctx, load.State, 1, Clock());
            Assert.That(load.State.Director.Notice, Is.Empty, "past its hold it is cleared");
            Assert.That(load.State.Director.NoticeUntil, Is.Zero);
        }

        /// <summary>
        /// GP-W5. The Breakers in a live assault are part of the SAVED PLAN, not something re-derived on load.
        ///
        /// Both directions are the same rule: what is announced is what arrives. A save taken inside an assault
        /// that carries heavies must bring exactly those heavies back, body for body; and a version-8 file, written
        /// by a build that had no Breakers at all, must resume as the assault it actually was — the player must not
        /// find heavy bodies in the wave they are already fighting because they saved and reloaded.
        ///
        /// The zero-stamp also has to be the right LENGTH. <see cref="SiegePlan.Planned"/> rejects a plan whose
        /// per-wave arrays disagree with <c>waveCount</c> and falls back to a residual single wave, which would
        /// quietly throw the rest of the encounter away, so the upgrade is asserted against the whole plan and not
        /// just against the new member.
        /// </summary>
        [Test]
        public void ASaveInsideABreakerAssaultResumesTheSameHeaviesAndAVersionEightFileResumesWithNone()
        {
            var ctx = RaidFixture.Context();
            var st = RaidFixture.State(ctx, 3);
            var d = st.Director;
            st.T = d.NextStart - ctx.Data.Raids.WarningS;
            RaidFixture.Run(ctx, st, 1, Clock());
            var a = d.Major;
            Assert.That(a, Is.Not.Null);

            SiegePlan.Build(ctx, st, a, 1);                  // replan it as the second assault, which carries heavies
            Assert.That(a.WaveBreakers, Is.EqualTo(new[] { 0, 0, 1, 2 }), "there are heavies in this one");
            d.NextMinor = st.T + 5000;                       // nothing else is due: this test is about the plan

            var load = SaveSerializer.ReadText(SaveSerializer.WriteText(st, ctx.Data), ctx.Data);
            Assert.That(load.Ok, Is.True, load.Reason);
            var b = load.State.Director.Major;
            Assert.That(b, Is.Not.Null);
            Assert.That(b.WaveBreakers, Is.EqualTo(a.WaveBreakers), "the heavies came back with the plan");
            for (var w = 0; w < a.Waves; w++)
                for (var i = 0; i < a.WaveCount[w]; i++)
                    Assert.That(SiegePlan.Kind(b, w, i), Is.EqualTo(SiegePlan.Kind(a, w, i)),
                        "every body of every wave is still the body it was going to be");

            // A version-8 file predates the member entirely.
            var state8 = Downgrade(st, 8, "director.major.waveBreakers", "drops");
            Assert.That(state8, Does.Not.Contain("\"waveBreakers\""), "the forged file is honestly a version-8 shape");

            var old = SaveSerializer.ReadText(Document(st, ctx.Data, state8, 8), ctx.Data);
            Assert.That(old.Ok, Is.True, old.Reason);
            Assert.That(old.Header.Version, Is.EqualTo(8));
            var c = old.State.Director.Major;
            Assert.That(c, Is.Not.Null, "the assault under way survived the upgrade");
            Assert.That(c.WaveBreakers, Is.EqualTo(new[] { 0, 0, 0, 0 }),
                "an assault begun without heavies finishes without them");
            Assert.That(c.WaveCount, Is.EqualTo(a.WaveCount), "and the rest of the plan is untouched");
            Assert.That(c.WaveSpitters, Is.EqualTo(a.WaveSpitters));
            Assert.That(c.Waves, Is.EqualTo(a.Waves), "so it was not reduced to a residual single wave");
            Assert.That(c.Remaining, Is.EqualTo(a.Remaining), "still owing what it owed");
            Assert.That(old.State.Drops, Is.Not.Null, "and the file gained an empty cargo list, which is the truth");
            Assert.That(old.State.Drops.Caches, Is.Empty);
            for (var w = 0; w < c.Waves; w++)
                Assert.That(SiegePlan.Kind(c, w, 0), Is.Not.EqualTo("breaker"));
        }
    }
}
