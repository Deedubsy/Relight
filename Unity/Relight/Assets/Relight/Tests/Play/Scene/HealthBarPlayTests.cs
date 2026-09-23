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
    /// REL-134, in the authored scene. <c>BuildingHealthTests</c> in the sim suite proves the READING — that the
    /// fraction a bar would be drawn from is the same one the repair card prints, in every damage state. This proves
    /// the other half, which a correct reading nobody consulted would still pass: that a bar is actually drawn on the
    /// building, that it goes away when the building is whole, that it clears the REL-133 repair mark on the sprite
    /// they share, and that it survives REL-130's wheel.
    ///
    /// The building is a chest for the same reason REL-133 chose one: it is outside
    /// <c>MachineActivityVisual.Supports</c>, so nothing else in the presenter draws on it and a stroke found here
    /// can only be this one.
    /// </summary>
    public sealed class HealthBarPlayTests
    {
        /// <summary>The child objects <c>BuildingHealthVisual</c> makes; counted enabled, which is what "drawn" means.</summary>
        private const string StrokeName = "Health bar detail";

        /// <summary>The REL-133 mark's strokes, for the one test that has to know where they are.</summary>
        private const string RepairStrokeName = "Repair detail";

        private static SimHost _host;
        private static MachinePresenter _machines;

        private static IEnumerator Ready()
        {
            yield return SceneFixture.LoadWorld();
            _host = Object.FindAnyObjectByType<SimHost>();
            _machines = Object.FindAnyObjectByType<MachinePresenter>();
            Assert.That(_host, Is.Not.Null, "World.unity has no SimHost.");
            Assert.That(_machines, Is.Not.Null, "World.unity has no MachinePresenter.");

            Object.FindAnyObjectByType<UiShell>()?.CloseActive();
            // Paused, so nothing heals or decays between the frame that sets a state up and the frame that reads it.
            _host.Paused = true;
            yield return null;

            var st = _host.Simulation.State;
            st.Engineer.Inv[ItemId.Steel] += 200;
            st.Engineer.Inv[ItemId.Copper] += 200;
            st.Ledger = Ledger.Open(st, _host.Simulation.Context.Data);
        }

        /// <summary>
        /// A chest standing beside the engineer, with a view the camera has actually rendered. The engineer is moved
        /// BEFORE the chest goes up: <c>MachineView.Animate</c> returns at once for a sprite that is not visible, and
        /// a renderer reports <c>isVisible</c> only after a camera has drawn it once — an off-screen chest would fail
        /// every test below for a reason that has nothing to do with health.
        /// </summary>
        private static IEnumerator AChestInView(System.Action<Machine, MachineView> then)
        {
            var sim = _host.Simulation;
            var (tx, ty) = SceneFixture.FreeTile(sim, "chest", 0);
            sim.State.Engineer.Pos = new Vec2(tx - 0.5, ty + 0.5);
            for (var i = 0; i < 4; i++) yield return null;

            var placed = sim.Apply(new PlaceMachineCommand("chest", tx, ty, Dir.N));
            Assert.That(placed.Accepted, Is.True, "could not stand a chest beside the engineer: " + placed.Problem);
            var chest = SceneFixture.Last(sim, "chest");
            Assert.That(chest, Is.Not.Null);
            for (var i = 0; i < 4; i++) yield return null;

            Assert.That(_machines.Views.TryGetValue(chest.Id, out var view), Is.True, "the chest has no view.");
            var sprite = view.GetComponentInChildren<SpriteRenderer>();
            Assert.That(sprite != null && sprite.isVisible, Is.True,
                "the chest is off camera, so nothing would animate it whatever this test did next.");
            then(chest, view);
        }

        [UnityTest, Timeout(120000)]
        public IEnumerator ADamagedBuildingWearsABarAndAWholeOneDoesNot()
        {
            yield return Ready();
            var sim = _host.Simulation;
            Machine chest = null; MachineView view = null;
            yield return AChestInView((m, v) => { chest = m; view = v; });

            Assert.That(view.HealthShowing, Is.False,
                "a chest that has never been hit is wearing a health bar; a base at full strength must not sprout " +
                "one over every wall and pole.");
            Assert.That(Strokes(view.transform, StrokeName), Is.Zero, "and nothing is drawn on it.");

            var max = TurretRules.MaxHp(sim.Context.Data, chest);
            Assert.That(max, Is.GreaterThan(0), "a chest can no longer be damaged; this test needs a building that can.");
            TurretRules.Damage(sim.Context, sim.State, chest, max * 0.5);
            yield return null;
            yield return null;

            Assert.That(view.HealthShowing, Is.True, "the chest has lost half its health and is not showing it.");
            Assert.That(Strokes(view.transform, StrokeName), Is.GreaterThan(0),
                "the flag is set but nothing was actually drawn on the chest.");

            // Healed through the hook RepairCommand itself calls, so this is the real path back to full and not a
            // field poked to make the assertion below come out.
            var healed = TurretRules.TurretRepairHook(sim.Context, sim.State, chest.Id, max);
            Assert.That(healed, Is.GreaterThan(0), "nothing was healed, so the next assertion would prove nothing.");
            Assert.That(TurretRules.Hp(sim.Context.Data, sim.State, chest), Is.EqualTo(max).Within(1e-9));
            yield return null;
            yield return null;

            Assert.That(view.HealthShowing, Is.False, "the chest is whole again and the bar is still on it.");
            Assert.That(Strokes(view.transform, StrokeName), Is.Zero, "the strokes were left enabled after the repair.");
        }

        /// <summary>
        /// The pairing both issues ask for: REL-133 draws INSIDE the footprint and REL-134 draws above it, so the two
        /// are on screen together — which they are nearly every time, a building being repaired being a damaged one —
        /// without touching. Measured from the strokes' own positions and widths rather than trusted to two offsets
        /// that happen to miss today.
        /// </summary>
        [UnityTest, Timeout(120000)]
        public IEnumerator TheBarHangsAboveTheFootprintAndClearOfTheRepairMark()
        {
            yield return Ready();
            var sim = _host.Simulation;
            Machine chest = null; MachineView view = null;
            yield return AChestInView((m, v) => { chest = m; view = v; });

            var max = TurretRules.MaxHp(sim.Context.Data, chest);
            TurretRules.Damage(sim.Context, sim.State, chest, max * 0.5);
            var started = sim.Apply(new RepairCommand(RepairKinds.Machine, chest.Id));
            Assert.That(started.Accepted, Is.True, "the repair was refused: " + started.Problem);
            yield return null;
            yield return null;

            Assert.That(view.HealthShowing, Is.True, "the damaged chest is not wearing its bar.");
            Assert.That(view.RepairShowing, Is.True,
                "the repair mark is not showing, so this test would prove nothing about the two of them together.");

            var (_, h) = chest.Dimensions;
            Assert.That(Bottom(view.transform, StrokeName), Is.GreaterThan(h * 0.5f),
                "the health bar is drawn over the building's own sprite instead of above it.");
            Assert.That(Bottom(view.transform, StrokeName), Is.GreaterThan(Top(view.transform, RepairStrokeName)),
                "the health bar and the repair mark overlap on the same sprite.");
        }

        /// <summary>
        /// REL-134's acceptance says a bar that is unreadable zoomed out is not done. The bar answers that by sizing
        /// its thickness from what the camera is showing, so this drives REL-130's wheel to the far notch and checks
        /// the bar opened with it — the whole point being that it stays the same size on SCREEN.
        /// </summary>
        [UnityTest, Timeout(120000)]
        public IEnumerator TheBarKeepsItsSizeOnScreenWhenTheWheelOpensTheView()
        {
            yield return Ready();
            var sim = _host.Simulation;
            var rig = Object.FindAnyObjectByType<CameraRig>();
            Assert.That(rig, Is.Not.Null, "World.unity has no CameraRig.");
            Machine chest = null; MachineView view = null;
            yield return AChestInView((m, v) => { chest = m; view = v; });

            TurretRules.Damage(sim.Context, sim.State, chest, TurretRules.MaxHp(sim.Context.Data, chest) * 0.5);
            yield return null;
            yield return null;
            var near = view.HealthThickness;
            Assert.That(near, Is.GreaterThan(0), "the bar was never drawn, so there is no thickness to compare.");
            var nearView = rig.GetComponent<Camera>().orthographicSize;

            for (var i = 0; i < CameraRig.ZoomNotches + 3; i++) rig.Wheel(-1f);
            for (var i = 0; i < 180 && Mathf.Abs(rig.ViewTilesHigh - rig.ViewTilesTarget) > 1e-3; i++) yield return null;
            // Two more for the presenter to draw at the settled size; it reads the camera, it is not read by it.
            yield return null;
            yield return null;

            var far = view.HealthThickness;
            var farView = rig.GetComponent<Camera>().orthographicSize;
            Assert.That(farView, Is.GreaterThan(nearView + 1e-3), "the wheel did not open the view; nothing was tested.");
            Assert.That(far / near, Is.EqualTo(farView / nearView).Within(1e-3),
                "the bar did not grow with the view, so it is smaller on screen at the far notch than at the near " +
                "one. If the bar is right and the framing has changed, check that neither end of the wheel's range " +
                "now reaches BuildingHealthVisual's MinThickness/MaxThickness clamp — inside a clamp the bar cannot " +
                "track the camera, and the claim this test makes would need new numbers rather than a new assertion.");
            Assert.That(view.HealthShowing, Is.True, "the bar stopped being drawn once the view opened.");
        }

        /// <summary>
        /// The core is a rect on <c>HomeState</c>, not a <c>Machine</c>: it has no <c>MachineView</c>, so this is a
        /// separate piece of wiring and gets a separate test, exactly as REL-133's mark did.
        /// </summary>
        [UnityTest, Timeout(120000)]
        public IEnumerator TheHomeCoreWearsTheSameBarWhenItHasLostHealth()
        {
            yield return Ready();
            var sim = _host.Simulation;
            var st = sim.State;
            var h = st.Home;
            Assert.That(h, Is.Not.Null);
            Assert.That(h.Placed, Is.True, "the Home core has not been placed.");
            Assert.That(_machines.CoreHealthShowing, Is.False, "the core is whole and already wearing a bar.");

            HomeCore.Damage(st, h.Hp * 0.5);
            Assert.That(h.Hp, Is.GreaterThan(0), "the core must still be standing; a knocked-out core is another case.");
            yield return null;
            yield return null;

            Assert.That(_machines.CoreHealthShowing, Is.True, "the core has lost half its health and is not showing it.");
            var mark = GameObject.Find("Home core condition");
            Assert.That(mark, Is.Not.Null, "no object was made to hang the core's bar on.");
            Assert.That(Strokes(mark.transform, StrokeName), Is.GreaterThan(0),
                "the core's bar was flagged but nothing was drawn.");

            // On the core, not at the origin and not on the engineer.
            var centre = WorldSpace.RectCentre(h.X, h.Y, h.W, h.H);
            Assert.That(Vector2.Distance(mark.transform.position, centre), Is.LessThan(0.01f),
                "the core's health bar is not standing on the core.");
            Assert.That(Bottom(mark.transform, StrokeName), Is.GreaterThan(h.H * 0.5f),
                "the core's bar is drawn over the core instead of above it.");
        }

        // ---- reading the strokes -----------------------------------------------------------------------------

        private static int Strokes(Transform parent, string named)
        {
            if (parent == null) return 0;
            var n = 0;
            foreach (Transform child in parent)
                if (child.name == named)
                {
                    var line = child.GetComponent<LineRenderer>();
                    if (line != null && line.enabled) n++;
                }
            return n;
        }

        /// <summary>The lowest edge any enabled stroke of that name reaches, its own thickness included.</summary>
        private static float Bottom(Transform parent, string named) => Edge(parent, named, false);

        /// <summary>The highest edge any enabled stroke of that name reaches, its own thickness included.</summary>
        private static float Top(Transform parent, string named) => Edge(parent, named, true);

        private static float Edge(Transform parent, string named, bool top)
        {
            var found = false;
            var best = 0f;
            foreach (Transform child in parent)
            {
                if (child.name != named) continue;
                var line = child.GetComponent<LineRenderer>();
                if (line == null || !line.enabled) continue;
                var half = line.widthMultiplier * 0.5f;
                for (var i = 0; i < line.positionCount; i++)
                {
                    var y = line.GetPosition(i).y + (top ? half : -half);
                    if (!found || (top ? y > best : y < best)) { best = y; found = true; }
                }
            }
            Assert.That(found, Is.True, "no enabled '" + named + "' stroke to measure.");
            return best;
        }
    }
}
