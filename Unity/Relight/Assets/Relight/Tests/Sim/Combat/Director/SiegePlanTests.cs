using System.Collections.Generic;
using NUnit.Framework;

namespace Relight.Sim.Tests.Combat
{
    /// <summary>
    /// GP-W4, "use several waves, varied composition and pressure on multiple defensive positions".
    ///
    /// The old assault was a flat roster trickling down one rotating approach, which is why it read as nothing at
    /// all. These checks pin the SHAPE the redesign gives it — how big each wave is, what it carries, when it
    /// opens, how many approaches it uses — and, just as importantly, that the shape adds back up: the roster the
    /// player is warned about has to be the roster that arrives, all of it and only it.
    /// </summary>
    public sealed class SiegePlanTests
    {
        private static List<ITickPhase> Clock() => new List<ITickPhase> { new DirectorPhase() };

        /// <summary>Runs the clock to the moment the next assault is announced and returns it, plan and all.</summary>
        private static MajorRaid Announce(SimContext ctx, SimState st)
        {
            st.T = st.Director.NextStart - ctx.Data.Raids.WarningS;
            RaidFixture.Run(ctx, st, 1, Clock());
            Assert.That(st.Director.Major, Is.Not.Null, "an assault was announced");
            return st.Director.Major;
        }

        /// <summary>
        /// The shipped defaults, worked through: four waves of 9 / 12 / 18 / 21 out of the exported roster of 60,
        /// spitters held back until the second wave and then 5 / 7 / 8 of the exported 20, and each wave opening on
        /// one more approach than the last. The numbers are asserted literally because they are the balance
        /// decision — see SiegeTuning.Fallback for the arithmetic they come from — and a change to them should have
        /// to be made here as well as in the tuning.
        /// </summary>
        [Test]
        public void AnAssaultIsPlannedAsGrowingWavesOnMoreAndMoreApproaches()
        {
            var ctx = RaidFixture.Context();
            var st = RaidFixture.State(ctx, 3);
            var a = Announce(ctx, st);
            var s = ctx.Data.Siege;

            Assert.That(a.Waves, Is.EqualTo(4), "four waves");
            Assert.That(a.Origins.Length, Is.EqualTo(4), "and the map offers four staged approaches to use");
            Assert.That(a.WaveCount, Is.EqualTo(new[] { 9, 12, 18, 21 }));
            Assert.That(a.WaveSpitters, Is.EqualTo(new[] { 0, 5, 7, 8 }), "the ranged echelon builds up");
            Assert.That(a.WaveApproaches, Is.EqualTo(new[] { 1, 2, 3, 4 }), "and the pressure spreads");

            var total = 0;
            for (var i = 0; i < a.Waves; i++) total += a.WaveCount[i];
            Assert.That(total, Is.EqualTo(a.Total), "the waves are the roster, not a sample of it");
            Assert.That(a.Total, Is.EqualTo((int)ctx.Data.Raids.Total), "the head count is still the exported one");
            Assert.That(a.Remaining, Is.EqualTo(a.Total));

            var spitters = 0;
            for (var i = 0; i < a.Waves; i++) spitters += a.WaveSpitters[i];
            Assert.That(spitters, Is.EqualTo(ctx.Data.Raids.MajorSpitters), "and so is the composition");

            for (var i = 0; i < a.Waves; i++)
                Assert.That(a.WaveStart[i], Is.EqualTo(a.StartsAt + i * s.MajorWaveGapS).Within(1e-9));
            Assert.That(a.EndsAt - a.StartsAt, Is.EqualTo(400).Within(1e-9),
                "3 gaps of 110 s, a 40 s spread inside the last wave and a 30 s tail");
            Assert.That(a.EndsAt - a.StartsAt, Is.EqualTo(SiegePlan.Length(ctx, a)).Within(1e-9));
        }

        /// <summary>
        /// A wave arrives in ROWS — one body per approach at the same second — so a later wave genuinely threatens
        /// several positions at once instead of queueing down a single lane. That is the difference between "more
        /// enemies" and "more places to be", and it is the whole point of the approach count growing.
        /// </summary>
        [Test]
        public void ALaterWaveArrivesOnEveryOneOfItsApproachesAtOnce()
        {
            var ctx = RaidFixture.Context();
            var st = RaidFixture.State(ctx, 3);
            var a = Announce(ctx, st);

            const int w = 3;                                    // the four-approach wave
            var k = SiegePlan.Approaches(a, w);
            Assert.That(k, Is.EqualTo(4));

            var row = new HashSet<int>();
            for (var i = 0; i < k; i++)
            {
                Assert.That(SiegePlan.Due(ctx, a, w, i), Is.EqualTo(a.WaveStart[w]).Within(1e-9),
                    "the whole first row is due on the second the wave opens");
                row.Add(SiegePlan.Origin(a, w, i));
            }
            Assert.That(row.Count, Is.EqualTo(k), "on four different approaches");
            Assert.That(SiegePlan.Due(ctx, a, w, k), Is.GreaterThan(a.WaveStart[w]), "the second row follows");

            // The opening wave is the opposite: one approach, so the player is taught one lane before four.
            Assert.That(SiegePlan.Approaches(a, 0), Is.EqualTo(1));
            Assert.That(SiegePlan.Sides(a, 0).Length, Is.EqualTo(1));
        }

        /// <summary>
        /// Escalation follows assaults SURVIVED, never machines built: the brief's rule that improving the base
        /// must not silently cancel itself. It is also capped, so the tenth assault is not unanswerable.
        /// </summary>
        [Test]
        public void TheAssaultGrowsWithAssaultsSurvivedAndStopsGrowing()
        {
            var ctx = RaidFixture.Context();
            var st = RaidFixture.State(ctx, 3);
            var a = Announce(ctx, st);
            var first = a.Total;

            SiegePlan.Build(ctx, st, a, 4);
            Assert.That(a.Total, Is.EqualTo(96), "15% per assault survived: 60 x 1.6");

            SiegePlan.Build(ctx, st, a, 40);
            Assert.That(a.Total, Is.EqualTo(first * 2), "capped at twice the opening roster");

            var sum = 0;
            for (var i = 0; i < a.Waves; i++) sum += a.WaveCount[i];
            Assert.That(sum, Is.EqualTo(a.Total), "an escalated roster still adds up");
        }

        /// <summary>
        /// The plan is not just a description: run the clock through a whole assault and exactly the planned roster
        /// walks on — every body, no body twice — and the assault then ends and is recorded. Bodies are cleared each
        /// tick so the active-raid budget cannot mask a miscount; nothing else is ticked, so nothing dies early.
        /// </summary>
        [Test]
        public void TheWholeRosterArrivesExactlyOnceAndTheAssaultThenEnds()
        {
            var ctx = RaidFixture.Context();
            var st = RaidFixture.State(ctx, 3);
            var d = st.Director;
            var a = Announce(ctx, st);
            var planned = a.Total;
            var length = a.EndsAt - a.StartsAt;
            st.T = a.StartsAt;                                  // skip the warning window; this is about the waves

            var born = 0;
            var spitters = 0;
            var sides = new HashSet<int>();
            var clock = Clock();
            for (var i = 0; i < (int)((length + 60) / RaidFixture.Dt); i++)
            {
                RaidFixture.Run(ctx, st, 1, clock);
                for (var e = 0; e < st.Enemies.Actors.Count; e++)
                {
                    born++;
                    if (st.Enemies.Actors[e].Kind == "spitter") spitters++;
                    sides.Add(st.Enemies.Actors[e].Origin);
                }
                st.Enemies.Actors.Clear();
                if (d.Major == null) break;
            }

            Assert.That(born, Is.EqualTo(planned), "every planned body arrived, and none arrived twice");
            Assert.That(spitters, Is.EqualTo(ctx.Data.Raids.MajorSpitters), "with the planned composition");
            Assert.That(sides.Count, Is.GreaterThan(1), "and from more than one approach");
            Assert.That(d.Major, Is.Null, "the assault ended once the roster was spent and the map was clear");
            Assert.That(d.History.Count, Is.EqualTo(1), "and it was recorded");
            Assert.That(d.History[0].Spawned, Is.EqualTo(planned));
        }

        /// <summary>
        /// A deferral moves the encounter, it does not reroll it. C-09 holds the assault while the opening
        /// encounter runs, and the player must get the assault they were warned about, later — not a different one.
        /// </summary>
        [Test]
        public void DeferringAnAssaultSlidesItsWholePlanWithoutChangingIt()
        {
            var ctx = RaidFixture.Context();
            var st = RaidFixture.State(ctx, 3);
            var a = Announce(ctx, st);
            var counts = (int[])a.WaveCount.Clone();
            var spitters = (int[])a.WaveSpitters.Clone();
            var offsets = (int[])a.WaveOffset.Clone();
            var opening = a.StartsAt;
            var gaps = new double[a.Waves];
            for (var i = 0; i < a.Waves; i++) gaps[i] = a.WaveStart[i] - opening;

            var until = opening + 600;
            Director.Defer(ctx, st, until, "Hold the assault: the opening encounter is still running.");

            Assert.That(a.StartsAt, Is.EqualTo(until).Within(1e-9));
            Assert.That(a.WaveCount, Is.EqualTo(counts), "the same waves");
            Assert.That(a.WaveSpitters, Is.EqualTo(spitters), "carrying the same enemies");
            Assert.That(a.WaveOffset, Is.EqualTo(offsets), "from the same sides");
            for (var i = 0; i < a.Waves; i++)
                Assert.That(a.WaveStart[i] - until, Is.EqualTo(gaps[i]).Within(1e-9), "spaced the same way");
            Assert.That(a.EndsAt - a.StartsAt, Is.EqualTo(SiegePlan.Length(ctx, a)).Within(1e-9),
                "and as long as it always was");
        }

        /// <summary>
        /// GP-W5's Breakers, which are the assault's "varied composition" half made to cost something.
        ///
        /// They are held back TWICE over, and both holds are the balance decision rather than an implementation
        /// detail: not at all until one whole assault has been survived (<c>MajorBreakerAssault</c> 1), and then
        /// only from the third wave (<c>MajorBreakerWave</c> 2). So the first siege stays the teaching encounter,
        /// and when the heavies do appear they appear as an escalation the player can point at.
        ///
        /// They are also taken OUT of the skitters, not added on top: the head count the warning quoted is still
        /// the head count that walks in, and only its weight changes. And they LEAD their wave — <c>index 0</c> —
        /// because a Breaker marches at 1.0 tiles/s against a skitter's 2.0, so putting it first is what makes the
        /// wave arrive together instead of the heavy trailing in alone after the light bodies have already died.
        ///
        /// The literal arrays are the shipped balance, worked through from the second assault's roster of 69
        /// (60 × 1.15): waves of 10 / 15 / 19 / 25, spitters 0 / 6 / 7 / 10, and 5% of 69 = 3 Breakers, which the
        /// same cumulative rounding puts down as 0 / 0 / 1 / 2.
        /// </summary>
        [Test]
        public void BreakersAppearOnlyAfterAnAssaultIsSurvivedComeOutOfTheSkittersAndLeadTheirWave()
        {
            var ctx = RaidFixture.Context();
            var st = RaidFixture.State(ctx, 3);
            var a = Announce(ctx, st);
            var s = ctx.Data.Siege;

            Assert.That(ctx.Data.TryEnemy("breaker", out var breaker), Is.True, "the approved heavy is in the roster");
            Assert.That(ctx.Data.TryEnemy("skitter", out var skitter), Is.True);
            Assert.That(Enemies.MarchSpeed(ctx.Data, breaker), Is.LessThan(Enemies.MarchSpeed(ctx.Data, skitter)),
                "it is slower than the bodies behind it, which is why it has to lead");

            // The first siege is the teaching encounter and carries no heavies at all.
            Assert.That(a.WaveBreakers, Is.EqualTo(new[] { 0, 0, 0, 0 }));
            for (var w = 0; w < a.Waves; w++)
                Assert.That(SiegePlan.Kind(a, w, 0), Is.Not.EqualTo("breaker"), "nothing heavy in the first assault");

            // The second one carries them, from MajorBreakerWave onwards.
            SiegePlan.Build(ctx, st, a, 1);
            Assert.That(a.Total, Is.EqualTo(69), "60 grown by one assault's 15%");
            Assert.That(a.WaveCount, Is.EqualTo(new[] { 10, 15, 19, 25 }));
            Assert.That(a.WaveSpitters, Is.EqualTo(new[] { 0, 6, 7, 10 }));
            Assert.That(a.WaveBreakers, Is.EqualTo(new[] { 0, 0, 1, 2 }));

            var heads = 0;
            var heavies = 0;
            for (var w = 0; w < a.Waves; w++)
            {
                heads += a.WaveCount[w];
                heavies += a.WaveBreakers[w];
                Assert.That(a.WaveCount[w] - a.WaveSpitters[w] - a.WaveBreakers[w], Is.GreaterThanOrEqualTo(0),
                    "a wave can only ever be cut up, never over-subscribed");
                Assert.That(w >= s.MajorBreakerWave || a.WaveBreakers[w] == 0,
                    "no heavy arrives before the wave the tuning names");
            }
            Assert.That(heads, Is.EqualTo(a.Total), "the head count is untouched: heavies replace skitters");
            Assert.That(heavies, Is.EqualTo(3), "5% of 69, rounded");

            // And they lead: the first bodies out of a carrying wave are the Breakers, then skitters, then spitters.
            Assert.That(SiegePlan.Kind(a, 2, 0), Is.EqualTo("breaker"));
            Assert.That(SiegePlan.Kind(a, 2, 1), Is.EqualTo("skitter"));
            Assert.That(SiegePlan.Kind(a, 2, a.WaveCount[2] - 1), Is.EqualTo("spitter"));
            Assert.That(SiegePlan.Kind(a, 3, 0), Is.EqualTo("breaker"));
            Assert.That(SiegePlan.Kind(a, 3, 1), Is.EqualTo("breaker"));
            Assert.That(SiegePlan.Kind(a, 3, 2), Is.EqualTo("skitter"));
            Assert.That(SiegePlan.Kind(a, 1, 0), Is.EqualTo("skitter"), "a wave with no heavies opens as it always did");
        }
    }
}
