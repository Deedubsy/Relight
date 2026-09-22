using NUnit.Framework;
using Relight.Sim.Tests.Combat;

namespace Relight.Sim.Tests.Core
{
    /// <summary>
    /// REL-84 (CMB-09b, E-19): a changed tuning value takes effect without a restart. The issue's own test line is
    /// "a test changes a value and sees the next raid use it", so the value changed here is one the director reads
    /// every tick — how long before a large raid the warning goes out — and the raid is scheduled by the real
    /// director phase on the swapped data, not by a fixture that poked the director.
    ///
    /// A twin game on the untouched data runs beside it to the same moment and schedules nothing, so the first
    /// test cannot pass on a raid that was due anyway.
    /// </summary>
    public sealed class DataReloadTests
    {
        /// <summary>The same catalogue with one raid number moved. <see cref="GameData"/> is a class, not a record.</summary>
        private static GameData WithWarning(GameData d, double warningS) => new GameData(
            d.Items, d.Machines, d.Recipes, d.Engineer, d.World, d.Weapons, d.Enemies, d.Ammunition, d.Turrets,
            d.Power, d.Time, d.Raids with { WarningS = warningS }, d.Opening, d.Stake, d.Defence, d.Siege);

        private static Simulation Game()
        {
            var ctx = RaidFixture.Context();
            var st = RaidFixture.State(ctx);
            var sim = Simulation.Wrap(ctx, st);
            RaidFixture.Run(ctx, st, 1);   // the director seeds its first start on its first tick
            st.Director.NextMinor = 1e9;   // one raid at a time: this is about the large one
            Assert.That(st.Director.NextStart, Is.GreaterThan(0), "the director has a first large raid on the calendar");
            return sim;
        }

        [Test]
        public void AChangedWarningTimeIsUsedByTheNextRaid_WithoutARestart()
        {
            var sim = Game();
            var twin = Game();
            var old = sim.Context.Data.Raids.WarningS;
            var longer = old * 2;
            Assert.That(longer, Is.GreaterThan(old), "the fixture's warning time must be positive for the test to mean anything");

            // The value changes while the game is already running: no new state, no new session.
            var swapped = WithWarning(sim.Context.Data, longer);
            Assert.That(sim.ReplaceData(swapped), Is.True);
            Assert.That(sim.Context.Data, Is.SameAs(swapped));
            Assert.That(sim.Context.Data.Raids.WarningS, Is.EqualTo(longer));

            // Stand both games at the moment the LONGER warning falls due: before the old one would have.
            var d = sim.State.Director;
            var t = d.NextStart - longer;
            Assert.That(t, Is.LessThan(d.NextStart - old));
            sim.State.T = t;
            twin.State.T = t;
            RaidFixture.Run(sim.Context, sim.State, 1);
            RaidFixture.Run(twin.Context, twin.State, 1);

            Assert.That(sim.State.Director.Major, Is.Not.Null, "the reloaded game scheduled its raid on the new warning time");
            Assert.That(twin.State.Director.Major, Is.Null, "the untouched twin is not yet inside its warning window");
        }

        [Test]
        public void AReloadKeepsTheWorld_AndTouchesNoSaveState()
        {
            var sim = Game();
            var was = sim.Context;
            var before = sim.Hash();
            sim.ReplaceData(WithWarning(sim.Context.Data, sim.Context.Data.Raids.WarningS + 1));

            Assert.That(sim.Context, Is.Not.SameAs(was), "a reload builds a new context rather than mutating one");
            Assert.That(sim.Context.Geometry, Is.SameAs(was.Geometry));
            Assert.That(sim.Context.Tiles, Is.SameAs(was.Tiles));
            Assert.That(sim.Context.Threat, Is.SameAs(was.Threat));
            Assert.That(sim.Context.Sites, Is.SameAs(was.Sites));
            Assert.That(sim.Context.MapId, Is.EqualTo(was.MapId));
            Assert.That(sim.Hash(), Is.EqualTo(before), "tuning values are data, not save state: the state hash is unmoved");
        }

        [Test]
        public void AReloadSaysWhatMoved_AndTheSameDataIsNotAReload()
        {
            var sim = Game();
            var same = sim.Context.Data;
            Assert.That(sim.ReplaceData(same), Is.False, "handing the game the data it already runs on changes nothing");
            Assert.That(RaidFixture.Count<DataReloadedEvent>(sim.State), Is.EqualTo(0));

            var swapped = WithWarning(same, same.Raids.WarningS + 1);
            Assert.That(sim.ReplaceData(swapped), Is.True);
            var e = RaidFixture.Last<DataReloadedEvent>(sim.State);
            Assert.That(e, Is.Not.Null, "the HUD and the log are told");
            Assert.That(e.FromHash, Is.EqualTo(GameDataHash.Compute(same)));
            Assert.That(e.ToHash, Is.EqualTo(GameDataHash.Compute(swapped)));
            Assert.That(e.FromHash, Is.Not.EqualTo(e.ToHash), "a save written now would carry a different dataVersion");
        }
    }
}
