using System.Collections;
using NUnit.Framework;
using Relight.Presentation;
using Relight.Sim;
using Relight.World;
using UnityEngine;
using UnityEngine.TestTools;

namespace Relight.Tests.Play
{
    /// <summary>
    /// A session swap behind a live <see cref="SimHost"/> — a new game, or a loaded save adopted by
    /// <c>AutosaveController</c> (the scene is never reloaded, §9.4.5) — must rebuild the views from the state
    /// that is live now.
    ///
    /// The defect these tests pin: the machine presenter keyed its rebuild on the sim's structural revision
    /// (<c>f.rev</c>) alone, and a revision is per-STATE, not per-session. Two saves that each placed one machine
    /// are both at the same revision, so loading one over the other left the previous session's machine views on
    /// screen, on the wrong tiles, until something else happened to place or remove a machine. The engineer view
    /// had the same shape of bug in its interpolation window: it would lerp the engineer from where the old
    /// session left them to where the loaded save puts them.
    /// </summary>
    public sealed class SessionChangeTests
    {
        private const string Chest = "chest";
        private const int ChestSize = 2;

        [UnityTest, Timeout(30000)]
        public IEnumerator LoadingAStateWithTheSameRevisionRebuildsTheMachineViews()
        {
            yield return SceneFixture.LoadWorld();
            var host = Object.FindAnyObjectByType<SimHost>();
            var presenter = Object.FindAnyObjectByType<MachinePresenter>();
            Assert.That(presenter, Is.Not.Null, "World.unity has no MachinePresenter.");
            var ctx = host.Simulation.Context;

            // Two sessions that are structurally identical — one machine each — but on different tiles.
            var first = Simulation.NewGame(ctx, 1);
            var placedFirst = first.Apply(new PlaceMachineCommand(Chest, 8, 8));
            Assert.That(placedFirst.Accepted, placedFirst.Problem);

            var second = Simulation.NewGame(ctx, 1);
            var placedSecond = second.Apply(new PlaceMachineCommand(Chest, 10, 4));
            Assert.That(placedSecond.Accepted, placedSecond.Problem);

            Assert.That(MachineSnapshot.Revision(second.State), Is.EqualTo(MachineSnapshot.Revision(first.State)),
                "the premise of this test is two states at the same revision.");

            host.Attach(first);
            yield return null;
            yield return null;
            var firstId = first.State.Machines[0].Id;
            Assert.That(presenter.Views.ContainsKey(firstId), "the first session's machine was never drawn.");

            host.Attach(second);
            yield return null;
            yield return null;

            var secondId = second.State.Machines[0].Id;
            Assert.That(presenter.Views.Count, Is.EqualTo(1), "one machine is placed in the loaded state.");
            Assert.That(presenter.Views.ContainsKey(secondId), "no view for the loaded state's machine.");

            var expected = WorldSpace.RectCentre(10, 4, ChestSize, ChestSize);
            var stale = WorldSpace.RectCentre(8, 8, ChestSize, ChestSize);
            foreach (var view in Object.FindObjectsByType<MachineView>())
            {
                var p = new Vector2(view.transform.position.x, view.transform.position.y);
                Assert.That(Vector2.Distance(p, new Vector2(stale.x, stale.y)), Is.GreaterThan(0.5f),
                    "a machine view is still standing where the PREVIOUS session had one.");
                Assert.That(Vector2.Distance(p, new Vector2(expected.x, expected.y)), Is.LessThan(0.01f),
                    $"machine view at {p}, but the loaded state puts it at {expected}.");
            }
        }

        [UnityTest, Timeout(30000)]
        public IEnumerator TheEngineerViewDoesNotInterpolateAcrossASessionChange()
        {
            yield return SceneFixture.LoadWorld();
            var host = Object.FindAnyObjectByType<SimHost>();
            var view = Object.FindAnyObjectByType<Presentation.EngineerView>();
            Assert.That(view, Is.Not.Null, "World.unity has no EngineerView.");
            var ctx = host.Simulation.Context;

            var first = Simulation.NewGame(ctx, 1);
            host.Attach(first);
            for (var i = 0; i < 3; i++) yield return null;
            var before = view.SimPosition;

            // A loaded save puts the engineer somewhere else entirely.
            var second = Simulation.NewGame(ctx, 1);
            second.State.Engineer.Pos = new Vec2(before.X + 12.0, before.Y + 2.0);

            host.Attach(second);
            Assert.That(view.Seeded, Is.False, "the interpolation window survived the session change.");

            yield return null;
            var target = second.State.Engineer.Pos;
            Assert.That(view.SimPosition.X, Is.EqualTo(target.X).Within(1e-6));
            Assert.That(view.PreviousSimPosition.X, Is.EqualTo(target.X).Within(1e-6),
                "both ends of the interpolation window must be seeded from the new session.");

            var expected = WorldSpace.World(target);
            var actual = view.transform.position;
            Assert.That(Vector2.Distance(new Vector2(expected.x, expected.y), new Vector2(actual.x, actual.y)),
                Is.LessThan(0.01f),
                $"the engineer is drawn at {actual}, between the old session's position and the loaded one.");
        }

        [UnityTest, Timeout(30000)]
        public IEnumerator DetachingRemovesTheViewsAndANewSessionRebuildsThem()
        {
            yield return SceneFixture.LoadWorld();
            var host = Object.FindAnyObjectByType<SimHost>();
            var presenter = Object.FindAnyObjectByType<MachinePresenter>();
            var ctx = host.Simulation.Context;

            var sim = Simulation.NewGame(ctx, 1);
            Assert.That(sim.Apply(new PlaceMachineCommand(Chest, 8, 8)).Accepted);
            host.Attach(sim);
            yield return null;
            yield return null;
            Assert.That(presenter.Views.Count, Is.EqualTo(1));

            var session = host.Session;
            host.Detach();
            Assert.That(host.Session, Is.GreaterThan(session), "a detach is a session change.");
            Assert.That(presenter.Views.Count, Is.Zero, "the views outlived the session they were drawn from.");
            yield return null;

            var next = Simulation.NewGame(ctx, 2);
            Assert.That(next.Apply(new PlaceMachineCommand(Chest, 10, 4)).Accepted);
            host.Attach(next);
            yield return null;
            yield return null;
            Assert.That(presenter.Views.Count, Is.EqualTo(1), "the new session was never drawn.");
            Assert.That(presenter.Views.ContainsKey(next.State.Machines[0].Id));
        }
    }
}
