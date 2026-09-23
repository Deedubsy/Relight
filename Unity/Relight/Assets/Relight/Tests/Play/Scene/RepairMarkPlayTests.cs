using System.Collections;
using NUnit.Framework;
using Relight.Presentation;
using Relight.Sim;
using Relight.UI;
using Relight.World;
using UnityEngine;
using UnityEngine.TestTools;

namespace Relight.Tests.Play
{
    /// <summary>
    /// REL-133, in the authored scene. <c>BuildingConditionTests</c> in the sim suite proves the rule — which
    /// building is being repaired and how far through it is. This proves the other half: that something is actually
    /// DRAWN on that building, which a rule nobody consulted would still pass.
    ///
    /// Both tests look for the strokes themselves and not only for the flag, because a flag set by a presenter that
    /// draws nothing is exactly the defect these are here to catch.
    ///
    /// The machine tested is a chest on purpose. It is outside <c>MachineActivityVisual.Supports</c>, so it is one
    /// of the buildings the existing activity strokes never touch — which is the reason the repair mark has a gate
    /// of its own.
    /// </summary>
    public sealed class RepairMarkPlayTests
    {
        /// <summary>The child objects <c>BuildingConditionVisual</c> makes; counted enabled, which is what "drawn" means.</summary>
        private const string StrokeName = "Repair detail";

        private static int Strokes(Transform parent)
        {
            if (parent == null) return 0;
            var n = 0;
            foreach (Transform child in parent)
                if (child.name == StrokeName)
                {
                    var line = child.GetComponent<LineRenderer>();
                    if (line != null && line.enabled) n++;
                }
            return n;
        }

        private static SimHost _host;
        private static MachinePresenter _machines;

        /// <summary>Load the world, close the shell, freeze the clock and put enough in the pockets to pay a repair.</summary>
        private static IEnumerator Ready()
        {
            yield return SceneFixture.LoadWorld();
            _host = Object.FindAnyObjectByType<SimHost>();
            _machines = Object.FindAnyObjectByType<MachinePresenter>();
            Assert.That(_host, Is.Not.Null, "World.unity has no SimHost.");
            Assert.That(_machines, Is.Not.Null, "World.unity has no MachinePresenter.");

            var shell = Object.FindAnyObjectByType<UiShell>();
            if (shell != null) shell.CloseActive();
            // Paused, so the repair does not run itself out between the frame that starts it and the frame that
            // reads it, and so the engineer holds still and the camera rectangle with them.
            _host.Paused = true;
            yield return null;

            var st = _host.Simulation.State;
            st.Engineer.Inv[ItemId.Steel] += 200;
            st.Engineer.Inv[ItemId.Copper] += 200;
            st.Ledger = Ledger.Open(st, _host.Simulation.Context.Data);
        }

        [UnityTest, Timeout(120000)]
        public IEnumerator RepairingAPlacedBuildingDrawsOnIt_AndCancellingTakesItOff()
        {
            yield return Ready();
            var sim = _host.Simulation;
            var (tx, ty) = SceneFixture.FreeTile(sim, "chest", 0);

            // Stand against the tile BEFORE the chest goes up, so the camera has settled on it by the time the view
            // exists: MachineView.Animate skips a machine whose sprite is not visible, and an off-screen chest would
            // fail this test for a reason that has nothing to do with repairs.
            sim.State.Engineer.Pos = new Vec2(tx - 0.5, ty + 0.5);
            yield return null;
            yield return null;
            yield return null;

            var placed = sim.Apply(new PlaceMachineCommand("chest", tx, ty, Dir.N));
            Assert.That(placed.Accepted, Is.True, "could not stand a chest beside the engineer: " + placed.Problem);
            var chest = SceneFixture.Last(sim, "chest");
            Assert.That(chest, Is.Not.Null);
            // One frame for the presenter to make the view, one for a camera to draw it (a renderer reports
            // isVisible only after it has been rendered once), one to spare.
            yield return null;
            yield return null;
            yield return null;

            Assert.That(_machines.Views.TryGetValue(chest.Id, out var view), Is.True, "the chest has no view.");
            var sprite = view.GetComponentInChildren<SpriteRenderer>();
            Assert.That(sprite != null && sprite.isVisible, Is.True,
                "the chest is off camera, so nothing would animate it whatever this test did next.");
            Assert.That(view.RepairShowing, Is.False, "nothing is being repaired yet.");
            Assert.That(Strokes(view.transform), Is.Zero, "and nothing is drawn yet.");

            TurretRules.Damage(sim.Context, sim.State, chest, TurretRules.MaxHp(sim.Context.Data, chest) * 0.5);
            var started = sim.Apply(new RepairCommand(RepairKinds.Machine, chest.Id));
            Assert.That(started.Accepted, Is.True, "the repair was refused: " + started.Problem);
            yield return null;
            yield return null;

            Assert.That(view.RepairShowing, Is.True,
                "a repair is running on this chest and the building is not showing it.");
            Assert.That(Strokes(view.transform), Is.GreaterThan(0),
                "the flag is set but nothing was actually drawn on the chest.");

            var cancelled = sim.Apply(new CancelRepairCommand());
            Assert.That(cancelled.Accepted, Is.True, "the cancel was refused: " + cancelled.Problem);
            yield return null;
            yield return null;

            Assert.That(view.RepairShowing, Is.False, "the repair was cancelled and the mark is still showing.");
            Assert.That(Strokes(view.transform), Is.Zero, "the strokes were left enabled after the cancel.");
        }

        [UnityTest, Timeout(120000)]
        public IEnumerator RepairingTheHomeCoreDrawsOnTheCore()
        {
            yield return Ready();
            var sim = _host.Simulation;
            var st = sim.State;
            Assert.That(st.Home, Is.Not.Null);
            Assert.That(st.Home.Placed, Is.True, "the Home core has not been placed.");
            Assert.That(_machines.CoreRepairShowing, Is.False, "nothing is being repaired yet.");

            var h = st.Home;
            // Against the core's west edge, so the repair is in reach wherever the authored core stands.
            st.Engineer.Pos = new Vec2(h.X - 0.5, h.Y + h.H / 2.0);
            // A patch, not a recommission: leave the core standing so the repair needs only reach and materials,
            // and so AttackersNearby — which only guards a recommission — cannot refuse it.
            HomeCore.Damage(st, h.Hp * 0.5);
            Assert.That(h.Hp, Is.GreaterThan(0), "the core must still be standing for this to be a patch.");

            var started = sim.Apply(new RepairCommand(RepairKinds.Core, -1));
            Assert.That(started.Accepted, Is.True, "the core repair was refused: " + started.Problem);
            yield return null;
            yield return null;

            Assert.That(_machines.CoreRepairShowing, Is.True,
                "the core is being repaired and nothing is showing it. The core is not a Machine, so it has no " +
                "MachineView, and this is the half of the wiring that covers it.");

            var mark = GameObject.Find("Home core condition");
            Assert.That(mark, Is.Not.Null, "no object was made to hang the core's repair mark on.");
            Assert.That(Strokes(mark.transform), Is.GreaterThan(0), "the core's mark was made but nothing was drawn.");

            // It stands on the core, not at the origin and not on the engineer.
            var centre = WorldSpace.RectCentre(h.X, h.Y, h.W, h.H);
            Assert.That(Vector2.Distance(mark.transform.position, centre), Is.LessThan(0.01f),
                "the core's repair mark is not standing on the core.");

            Assert.That(sim.Apply(new CancelRepairCommand()).Accepted, Is.True);
            yield return null;
            yield return null;

            Assert.That(_machines.CoreRepairShowing, Is.False, "the core's mark outlived the repair.");
            Assert.That(Strokes(mark.transform), Is.Zero, "the core's strokes were left enabled after the cancel.");
        }
    }
}
