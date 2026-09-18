using NUnit.Framework;

namespace Relight.Sim.Tests.Production
{
    /// <summary>
    /// B-10's saved state: <see cref="FlowState"/> is a side table keyed by machine id (no new <c>Machine</c>
    /// fields, schema rule v3), the route and order caches are transient, and a default <c>FlowState</c> means
    /// "nothing has happened yet" — which is exactly what a Phase B save upgraded to v3 arrives with.
    /// </summary>
    public sealed class FlowPersistenceTests
    {
        private static (SimContext Ctx, SimState St) MidTransit()
        {
            var ctx = FlowFixture.Context();
            var st = FlowFixture.State(ctx);
            var src = FlowFixture.Add(ctx, st, "chest", 2, 6);
            for (var i = 0; i < 6; i++) FlowFixture.Add(ctx, st, "belt", 4 + i, 6, Dir.E);
            var split = FlowFixture.Add(ctx, st, "splitter", 10, 6, Dir.E);
            var exit = FlowFixture.Add(ctx, st, "underground", 12, 7, Dir.E);
            st.Flow.Of(exit.Id).Mode = 1;
            st.Flow.Of(split.Id).Priority = 2;
            src.Inv.Add(ItemId.Steel, 40);
            FlowFixture.Seal(ctx, st);
            FlowFixture.Run(ctx, st, 60);
            return (ctx, st);
        }

        [Test]
        public void ItemsInTransitSurviveASaveAndLoad()
        {
            var (ctx, st) = MidTransit();
            var held = FlowQueries.HeldCount(st);
            Assert.That(held, Is.GreaterThan(0), "the fixture must actually have items on belts");

            // A fixed `savedAt` so the two documents are comparable: the header stamps the wall clock otherwise.
            const string at = "2026-09-14T00:00:00.0000000Z";
            var text = SaveSerializer.WriteText(st, ctx.Data, at);
            var loaded = SaveSerializer.ReadText(text, ctx.Data);
            Assert.That(loaded.Ok, Is.True, loaded.Reason);

            Assert.That(FlowQueries.HeldCount(loaded.State), Is.EqualTo(held), "every item came back");
            Assert.That(StateHash.Compute(loaded.State), Is.EqualTo(StateHash.Compute(st)), "H1 over the flow state");
            Assert.That(SaveSerializer.WriteText(loaded.State, ctx.Data, at), Is.EqualTo(text), "byte for byte");

            // A lazily rebuilt cache restored into the wrong shape is invisible to a single-frame comparison, so
            // both copies are stepped on and compared again (the same argument as SaveRoundTripTests).
            FlowFixture.Run(ctx, st, 100);
            FlowFixture.Run(ctx, loaded.State, 100);
            Assert.That(StateHash.Compute(loaded.State), Is.EqualTo(StateHash.Compute(st)), "and they run the same");
        }

        /// <summary>The splitter's priority and the underground's role are saved, not re-derived.</summary>
        [Test]
        public void SplitterPriorityAndUndergroundRoleRoundTrip()
        {
            var (ctx, st) = MidTransit();
            var loaded = SaveSerializer.ReadText(SaveSerializer.WriteText(st, ctx.Data), ctx.Data);
            for (var i = 0; i < st.Machines.Count; i++)
            {
                var id = st.Machines[i].Id;
                Assert.That(FlowQueries.Priority(loaded.State, id), Is.EqualTo(FlowQueries.Priority(st, id)));
                Assert.That(st.Flow.Find(id)?.Mode ?? 0, Is.EqualTo(loaded.State.Flow.Find(id)?.Mode ?? 0));
            }
        }

        /// <summary>
        /// A Phase B (v2) save carries no <c>flow</c> member at all, so the state arrives with <c>new FlowState()</c>.
        /// It must tick without throwing and simply start from empty.
        /// </summary>
        [Test]
        public void ADefaultFlowStateTicksWithoutThrowingAndStartsEmpty()
        {
            var ctx = FlowFixture.Context();
            var st = FlowFixture.State(ctx);
            Assert.That(st.Flow.Lanes, Is.Empty);
            Assert.That(FlowQueries.HeldCount(st), Is.Zero);

            Assert.DoesNotThrow(() => FlowFixture.Run(ctx, st, 40), "no machines at all");

            var src = FlowFixture.Add(ctx, st, "chest", 2, 6);
            FlowFixture.Add(ctx, st, "belt", 4, 6, Dir.E);
            var dst = FlowFixture.Add(ctx, st, "chest", 5, 6);
            src.Inv.Add(ItemId.Steel, 4);
            FlowFixture.Seal(ctx, st);
            FlowFixture.Run(ctx, st, 200);
            Assert.That(dst.Inv[ItemId.Steel], Is.EqualTo(4), "the lanes were created on demand");
        }

        /// <summary>
        /// Wave 1 integration note: <c>Placement.Remove</c> must give belt items and the inserter's hand back too
        /// (reference flow.ts:1381 <c>pickUpItems</c>), and <c>FlowState</c> must forget the lane.
        /// </summary>
        [Test]
        public void PackingABeltReturnsTheItemsRidingOnIt()
        {
            var ctx = FlowFixture.Context();
            var st = FlowFixture.State(ctx);
            st.Engineer.Pos = new Vec2(4.5, 6.5);
            var src = FlowFixture.Add(ctx, st, "chest", 2, 6);
            var belt = FlowFixture.Add(ctx, st, "belt", 4, 6, Dir.E);   // dead end: it fills and stays full
            src.Inv.Add(ItemId.Steel, 10);
            FlowFixture.Seal(ctx, st);
            FlowFixture.Run(ctx, st, 200);
            Assert.That(FlowQueries.Count(st, belt.Id), Is.EqualTo(4));

            var before = st.Engineer.Inv[ItemId.Steel];
            var removed = Placement.Remove(ctx, st, belt.Id);
            Assert.That(removed.ok, Is.True, removed.reason);
            // Reference `pickUpItems` gives the machine back as one carried machine item plus everything it held,
            // so the four plates riding the belt come back as plates and the belt itself comes back as a belt.
            Assert.That(st.Engineer.Inv[ItemId.Steel], Is.EqualTo(before + 4), "the four items riding it");
            Assert.That(st.Engineer.Inv[new ItemKey("belt")], Is.EqualTo(1), "and the belt itself, carried");
            Assert.That(st.Flow.Find(belt.Id), Is.Null, "the lane is forgotten with the machine");
        }
    }
}
