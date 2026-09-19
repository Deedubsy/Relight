using System.Collections;
using NUnit.Framework;
using Relight.Presentation;
using Relight.Sim;
using Relight.UI;
using Relight.World;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UIElements;

namespace Relight.Tests.Play
{
    /// <summary>
    /// B-13 acceptance, test 1: the authored scene actually runs. Loads <c>World</c> (which loads <c>GameUI</c> on
    /// top of itself), lets it play, and checks the three seams the task is about — the clock runs, the engineer
    /// view follows the selector, and a machine placed by command becomes a prefab instance.
    /// </summary>
    public sealed class WorldSceneTests
    {
        private const float Settle = 2.0f;

        [UnityTest, Timeout(30000)]
        public IEnumerator SceneRunsAndViewsFollowTheSimulation()
        {
            yield return SceneFixture.LoadWorld();
            var host = Object.FindAnyObjectByType<SimHost>();
            Assert.That(host, Is.Not.Null, "World.unity has no SimHost.");
            Assert.That(host.Simulation, Is.Not.Null, "WorldBootstrap did not start a session.");

            var ticks0 = host.TotalTicks;
            yield return new WaitForSecondsRealtime(Settle);

            // 1. The clock runs at the host's fixed rate.
            var ticks = host.TotalTicks - ticks0;
            Assert.That(ticks, Is.GreaterThan(Simulation.TicksPerSecond),
                $"only {ticks} ticks in {Settle} s of play.");

            // 2. The engineer view is where the selector says the engineer is (within one tile: the view
            //    interpolates by SimHost.Alpha, so it trails the tick boundary by up to one tick of movement).
            var view = Object.FindAnyObjectByType<Relight.Presentation.EngineerView>();
            Assert.That(view, Is.Not.Null, "World.unity has no EngineerView.");
            var sim = host.Simulation;
            var expected = WorldSpace.World(WorldQueries.Engineer(sim.Context, sim.State).Pos);
            var actual = view.transform.position;
            Assert.That(Vector2.Distance(new Vector2(expected.x, expected.y), new Vector2(actual.x, actual.y)),
                Is.LessThan(WorldSpace.UnitsPerTile),
                $"engineer view at {actual} but the selector says {expected}.");

            // 3. The camera is centred on the engineer (it has had two seconds to catch up).
            var rig = Object.FindAnyObjectByType<CameraRig>();
            Assert.That(rig, Is.Not.Null, "World.unity has no CameraRig.");
            Assert.That(Vector2.Distance(
                    new Vector2(rig.transform.position.x, rig.transform.position.y),
                    new Vector2(actual.x, actual.y)),
                Is.LessThan(1f), "the camera is not following the engineer.");
        }

        [UnityTest, Timeout(30000)]
        public IEnumerator PlacedMachineAppearsAsAPrefabBoundToItsDataAsset()
        {
            yield return SceneFixture.LoadWorld();
            var host = Object.FindAnyObjectByType<SimHost>();
            var presenter = Object.FindAnyObjectByType<MachinePresenter>();
            Assert.That(presenter, Is.Not.Null, "World.unity has no MachinePresenter.");
            // Phase C: the imported region starts with the Home Depot placed (HomeCoreInitializer), so the baseline
            // is whatever the fresh game holds, and the chest goes on free ground near the spawn.
            var st0 = host.Simulation.State;
            var baseline = st0.Machines.Count;
            Assert.That(presenter.Views.Count, Is.EqualTo(baseline), "one view per machine the fresh game starts with.");

            // Phase C placement rules (C-10): the engineer must be in reach and carry the machine or its price.
            var (tx, ty) = SceneFixture.FreeTile(host.Simulation, "chest", 0);
            st0.Engineer.Pos = new Vec2(tx + 0.5, ty + 0.5);
            Pockets.Take(host.Simulation.Context.Data, st0.Engineer, new ItemKey("chest"), 1);
            host.Submit(new PlaceMachineCommand("chest", tx, ty));
            // One frame applies the queued command; the presenter syncs on the same frame's tick boundary.
            yield return null;
            yield return null;

            var st = host.Simulation.State;
            Assert.That(st.Machines.Count, Is.EqualTo(baseline + 1), "the place command was refused.");
            var id = SceneFixture.Last(host.Simulation, "chest").Id;
            Assert.That(presenter.Views.ContainsKey(id), "no MachineView was spawned for the placed machine.");

            var machineView = presenter.Views[id];
            Assert.That(machineView.MachineId, Is.EqualTo(id));
            Assert.That(machineView.Definition, Is.Not.Null,
                "the machine prefab is not bound to a MachineDefinition asset.");
            Assert.That(machineView.Kind, Is.EqualTo("chest"),
                "the prefab's data asset is not the one the sim placed.");

            // The view sits on the machine's footprint, in Unity space.
            var expected = WorldSpace.RectCentre(tx, ty, 2, 2);
            Assert.That(Vector2.Distance(
                    new Vector2(machineView.transform.position.x, machineView.transform.position.y),
                    new Vector2(expected.x, expected.y)),
                Is.LessThan(0.01f));

            host.Submit(new RemoveMachineCommand(id));
            yield return null;
            yield return null;
            Assert.That(presenter.Views.ContainsKey(id), Is.False, "the view outlived the machine.");
        }

        [UnityTest, Timeout(30000)]
        public IEnumerator PanelReadsTheSimulationAndItsButtonSubmitsACommand()
        {
            yield return SceneFixture.LoadWorld();
            var host = Object.FindAnyObjectByType<SimHost>();
            var status = Object.FindAnyObjectByType<StatusPanelController>();
            Assert.That(status, Is.Not.Null, "GameUI.unity has no StatusPanelController.");

            yield return new WaitForSecondsRealtime(0.5f);

            // Reads: the view-model formatted real state through the selectors.
            Assert.That(status.Model.Clock, Does.StartWith("0:0"), "clock: " + status.Model.Clock);
            Assert.That(status.Model.Health, Does.Contain("HP"), "health: " + status.Model.Health);
            Assert.That(status.Model.Position, Is.Not.Empty);
            Assert.That(status.Model.Refreshes, Is.GreaterThan(1));

            // Writes: the button submits a real command and the sim acts on it.
            // Correction pass: the panel's write seam used to be #walk-here. Click-to-move is gone (the empty hand
            // does nothing on a click now), so the button and its MoveCommand handler went with it, and the panel's
            // remaining action button — #sort-pockets — is the seam this half of the test exercises instead.
            // Phase C added a second UIDocument (the pause menu); the button sits in the one that instances StatusPanel.uxml.
            Button button = null;
            foreach (var document in Object.FindObjectsByType<UIDocument>(FindObjectsSortMode.None))
            {
                var root = document != null ? document.rootVisualElement : null;
                button = root?.Q<Button>("sort-pockets");
                if (button != null) break;
            }
            Assert.That(button, Is.Not.Null, "StatusPanel.uxml has no #sort-pockets button.");

            // InventorySortCommand rebuilds Engineer.Pack from the inventory (InventoryCommands.cs:36). A fresh game
            // has never laid the pack out, so Pack is null until the command actually reaches the sim: that null →
            // not-null flip is the observable effect of this click, and it cannot happen on the UI side alone.
            var engineer = host.Simulation.State.Engineer;
            engineer.Pack = null;
            var clicked = 0;
            button.clicked += () => clicked++;
            using (var submit = NavigationSubmitEvent.GetPooled())
            {
                submit.target = button;
                button.SendEvent(submit);
            }
            yield return null;   // the panel dispatches queued events on its own update
            Assert.That(clicked, Is.EqualTo(1), "the button's click handler did not run at all.");

            // Submit() queues; the command lands on the next sim tick (20 Hz), not on the next frame.
            var deadline = Time.realtimeSinceStartup + 2f;
            while (engineer.Pack == null && Time.realtimeSinceStartup < deadline) yield return null;

            Assert.That(engineer.Pack, Is.Not.Null, "the button click did not reach the simulation.");

            // …and the panel is still reading the live sim afterwards.
            var refreshes = status.Model.Refreshes;
            yield return new WaitForSecondsRealtime(0.5f);
            Assert.That(status.Model.Refreshes, Is.GreaterThan(refreshes),
                "the panel stopped reading the simulation after the command.");
        }
    }
}
