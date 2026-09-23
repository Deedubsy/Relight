using System.Collections.Generic;
using NUnit.Framework;
using Relight.Sim.Tests.Combat;
using Relight.Sim.Tests.Support;

namespace Relight.Sim.Tests.Persistence
{
    /// <summary>
    /// REL-65 (PER-05): the event autosaves of TECHNICAL_ARCHITECTURE.md §9.4.2 — one when a raid starts (its
    /// warning opens), one when a raid ends — through the same ring as the timed saves. An event save resets the
    /// five-minute timer; none is written while the engineer is down, and the one owed waits until they are up.
    /// FRT-08 (REL-143) adds a third moment, a plant commissioned. "Site restored" has no sim event yet.
    /// </summary>
    public sealed class EventAutosaveTests
    {
        private static (MemoryFileSystem Fs, SaveStore Store, AutosaveScheduler Sched, Simulation Sim) Setup(bool onEvents = true)
        {
            var (fs, store) = PersistenceFixture.Store();
            var sched = new AutosaveScheduler(store, new AutosaveSettings(AutosaveSettings.DefaultIntervalMinutes,
                AutosaveSettings.DefaultSlots, onEvents));
            var (sim, _) = Scenarios.ShortRun().Play(100);
            return (fs, store, sched, sim);
        }

        private static int Ring(MemoryFileSystem fs, SaveStore store)
        {
            var names = fs.ListFiles(store.Autosaves.Directory);
            var n = 0;
            for (var i = 0; i < names.Count; i++)
            {
                var f = names[i];
                if (!f.StartsWith(AutosaveStore.SlotPrefix, System.StringComparison.Ordinal)) continue;
                if (!f.EndsWith(SaveSchema.Extension, System.StringComparison.Ordinal)) continue;
                if (f == AutosaveStore.IndexFile || f == AutosaveStore.QuitFile) continue;
                n++;
            }
            return n;
        }

        private static List<SimEvent> One(SimEvent e) => new List<SimEvent> { e };

        private static RaidNoticeEvent Warned(double t, int id) =>
            new RaidNoticeEvent(t, RaidNoticeKind.Announced, "Major assault inbound from the north in 300 s.", id);

        private static RaidEndedEvent Ended(double t, int id) =>
            new RaidEndedEvent(t, id, true, false, (int)RaidOutcome.Cleared, t - 400);

        // ---------------------------------------------------------------- the moments

        /// <summary>
        /// Acceptance 1, on the real director: a large raid's warning opens and a save is written; its later wave
        /// notices write nothing more; it ends and a second save is written. Two ring files, both from events.
        /// </summary>
        [Test]
        public void OnTheRealDirector_ARaidsWarningAndItsEndEachWriteOneSave()
        {
            var ctx = RaidFixture.Context();
            var st = RaidFixture.State(ctx);
            new HomeCoreInitializer().Init(ctx, st);
            var sim = Simulation.Wrap(ctx, st);
            var (fs, store) = PersistenceFixture.Store();
            var sched = new AutosaveScheduler(store, AutosaveSettings.Default);
            var d = st.Director;
            var clock = new List<ITickPhase> { new DirectorPhase() };
            var saves = new List<string>();

            void Tick(int n)
            {
                for (var i = 0; i < n; i++)
                {
                    var from = st.Events.Count;
                    RaidFixture.Run(ctx, st, 1, clock);
                    var fresh = st.Events.GetRange(from, st.Events.Count - from);
                    var r = sched.Observe(fresh, sim);
                    if (r != null) { Assert.That(r.Ok, Is.True, r.Reason); saves.Add(st.T.ToString("0.0")); }
                }
            }

            Tick(1);
            Assert.That(saves, Is.Empty, "nothing to save before a raid");
            st.T = d.NextStart - ctx.Data.Raids.WarningS;
            Tick(1);
            Assert.That(d.Major, Is.Not.Null, "the fixture map stages an assault");
            Assert.That(saves.Count, Is.EqualTo(1), "the warning opening is a raid start");
            st.T = d.Major.StartsAt;
            Tick(40);
            var a = d.Major;
            Assert.That(a.Committed, Is.True);
            Assert.That(a.Waves, Is.GreaterThan(1), "the fixture's assault must have a later wave to announce");
            st.T = a.WaveStart[a.Waves - 1];                 // every later wave is announced on this tick
            Tick(40);
            var lines = 0;
            foreach (var e in st.Events)
                if (e is RaidNoticeEvent n && n.Kind == RaidNoticeKind.Announced && n.RaidId == a.Id) lines++;
            Assert.That(lines, Is.GreaterThan(1), "the warning and at least one wave line were announced");
            Assert.That(saves.Count, Is.EqualTo(1), "a wave line is not a new raid");
            a.Remaining = 0;
            st.Enemies.Actors.RemoveAll(e => e.Group == a.Id);
            Tick(1);
            Assert.That(d.Major, Is.Null, "the assault is over");
            Assert.That(saves.Count, Is.EqualTo(2), "the raid's end is the second save");
            Assert.That(Ring(fs, store), Is.EqualTo(2));
        }

        /// <summary>A small raid's warning is a raid start too, and its end a raid end.</summary>
        [Test]
        public void ASmallRaidsWarningAndItsEndEachWriteOneSave()
        {
            var (fs, store, sched, sim) = Setup();
            var t = sim.State.T;
            Assert.That(sched.Observe(One(new RaidNoticeEvent(t, RaidNoticeKind.MinorRaid, "Raid inbound from the east in 30 s.", 9)), sim)?.Ok, Is.True);
            Assert.That(sched.Observe(One(new RaidEndedEvent(t, 9, false, false, (int)RaidOutcome.Cleared, t)), sim)?.Ok, Is.True);
            Assert.That(Ring(fs, store), Is.EqualTo(2));
        }

        /// <summary>FRT-08: lighting a plant is a milestone, and it writes one save.</summary>
        [Test]
        public void APlantCommissionedWritesOneSave()
        {
            var (fs, store, sched, sim) = Setup();
            var t = sim.State.T;
            var r = sched.Observe(One(new PlantCommissionedEvent(t, "plant:riverside", "Riverside Works", 600, 40, 40)), sim);
            Assert.That(r?.Ok, Is.True);
            Assert.That(Ring(fs, store), Is.EqualTo(1));
            Assert.That(sched.Observe(One(new PlantPreparedEvent(t, "plant:riverside")), sim), Is.Null,
                "preparing it is a step, not the milestone");
        }

        /// <summary>
        /// A warning announced again (the Admin pull-in, a deferral) is the same raid, and a notice that is not a
        /// warning (deferred, skipped, reserved, the core held) is no moment at all.
        /// </summary>
        [Test]
        public void ARepeatedWarningAndOtherNoticesWriteNothing()
        {
            var (fs, store, sched, sim) = Setup();
            var t = sim.State.T;
            Assert.That(sched.Observe(One(Warned(t, 4)), sim), Is.Not.Null);
            Assert.That(sched.Observe(One(Warned(t + 1, 4)), sim), Is.Null, "the same raid warned again");
            foreach (var k in new[] { RaidNoticeKind.Skipped, RaidNoticeKind.Deferred, RaidNoticeKind.Reserved,
                                      RaidNoticeKind.Released, RaidNoticeKind.CoreHeld })
                Assert.That(sched.Observe(One(new RaidNoticeEvent(t, k, "x", 4)), sim), Is.Null, k.ToString());
            Assert.That(sched.Observe(new List<SimEvent>(), sim), Is.Null, "no events, no save");
            Assert.That(Ring(fs, store), Is.EqualTo(1));
        }

        /// <summary>Two moments in one frame are one save: a burst never spends two ring slots.</summary>
        [Test]
        public void TwoMomentsInOneFrameWriteOneSave()
        {
            var (fs, store, sched, sim) = Setup();
            var t = sim.State.T;
            var r = sched.Observe(new List<SimEvent> { Ended(t, 3), Warned(t, 5) }, sim);
            Assert.That(r?.Ok, Is.True);
            Assert.That(Ring(fs, store), Is.EqualTo(1));
        }

        // ---------------------------------------------------------------- the timer and the guard

        /// <summary>Acceptance 2: an event save resets the five-minute timer (§9.4.2).</summary>
        [Test]
        public void AnEventSaveResetsTheTimedInterval()
        {
            var (fs, store, sched, sim) = Setup();
            Assert.That(sched.Advance(200, sim), Is.Null);
            Assert.That(sched.SecondsUntilSave, Is.EqualTo(100).Within(1e-9));
            Assert.That(sched.Observe(One(Warned(sim.State.T, 1)), sim)?.Ok, Is.True);
            Assert.That(sched.SecondsUntilSave, Is.EqualTo(300).Within(1e-9), "a full interval from the event save");
            Assert.That(sched.Advance(299, sim), Is.Null, "no timed save 100 s after the event save");
            Assert.That(sched.Advance(1, sim)?.Ok, Is.True, "the timed save comes a full interval later");
            Assert.That(Ring(fs, store), Is.EqualTo(2));
        }

        /// <summary>
        /// Acceptance 3: no event save while the engineer is down. The save owed is kept and written once they
        /// are up, so the moment is not lost and no slot ever holds a downed engineer from an event.
        /// </summary>
        [Test]
        public void NoEventSaveWhileTheEngineerIsDown_TheOwedOneIsWrittenWhenTheyAreUp()
        {
            var (fs, store, sched, sim) = Setup();
            var e = sim.State.Engineer;
            e.Down = sim.State.T;
            Assert.That(e.IsDown, Is.True);
            Assert.That(sched.Observe(One(Ended(sim.State.T, 2)), sim), Is.Null, "down: nothing is written");
            Assert.That(sched.Observe(new List<SimEvent>(), sim), Is.Null, "still down");
            Assert.That(sched.Pending, Is.Not.Null, "the save is owed");
            Assert.That(Ring(fs, store), Is.EqualTo(0));

            e.Down = -1;
            Assert.That(sched.Observe(new List<SimEvent>(), sim)?.Ok, Is.True, "up again: the owed save is written");
            Assert.That(sched.Pending, Is.Null);
            Assert.That(Ring(fs, store), Is.EqualTo(1));
            Assert.That(sched.Observe(new List<SimEvent>(), sim), Is.Null, "and only once");
        }

        /// <summary>The setting still rules: with event autosaves off, a raid writes nothing and owes nothing.</summary>
        [Test]
        public void WithEventAutosavesOffARaidWritesNothing()
        {
            var (fs, store, sched, sim) = Setup(onEvents: false);
            Assert.That(sched.Observe(One(Warned(sim.State.T, 1)), sim), Is.Null);
            Assert.That(sched.Observe(One(Ended(sim.State.T, 1)), sim), Is.Null);
            Assert.That(sched.Pending, Is.Null);
            Assert.That(Ring(fs, store), Is.EqualTo(0));
        }

        /// <summary>
        /// A game loaded mid-raid: the raid on the map was warned before the save, so its next wave line is not a
        /// raid start. Its end still saves.
        /// </summary>
        [Test]
        public void AGameLoadedMidRaidDoesNotSaveTheRaidsNextWaveAsAStart()
        {
            var (fs, store, sched, sim) = Setup();
            var t = sim.State.T;
            sim.State.Director.Major = new MajorRaid { Id = 12, Committed = true };
            sched.Reset(sim);
            Assert.That(sched.Observe(One(Warned(t, 12)), sim), Is.Null, "raid 12 was warned before the load");
            Assert.That(sched.Observe(One(Ended(t, 12)), sim)?.Ok, Is.True, "its end is still a moment");
            Assert.That(Ring(fs, store), Is.EqualTo(1));
        }

        /// <summary>A new session forgets what the old one owed and which raids it had seen.</summary>
        [Test]
        public void AResetForgetsTheOwedSaveAndTheRaidsSeen()
        {
            var (fs, store, sched, sim) = Setup();
            sim.State.Engineer.Down = sim.State.T;
            sched.Observe(One(Warned(sim.State.T, 7)), sim);
            Assert.That(sched.Pending, Is.Not.Null);
            sched.Reset();
            Assert.That(sched.Pending, Is.Null);
            sim.State.Engineer.Down = -1;
            Assert.That(sched.Observe(One(Warned(sim.State.T, 7)), sim)?.Ok, Is.True, "raid 7 is new to the new session");
            Assert.That(Ring(fs, store), Is.EqualTo(1));
        }
    }
}
