using System.Collections;
using NUnit.Framework;
using Relight.Presentation;
using Relight.Sim;
using Relight.UI;
using Relight.World;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.TestTools;

namespace Relight.Tests.Play
{
    /// <summary>
    /// C-13 acceptance in the authored scene: the three things that sat between the finished sim rules and the
    /// player — a ghost that follows the cursor, a right-click that packs the machine under it, and the sim's own
    /// refusal text arriving on the HUD.
    ///
    /// Driven like <see cref="HeldKeyTests"/>: real virtual devices through the public low-level API rather than
    /// <c>InputTestFixture</c>, whose assembly is not a <c>testable</c> of this project.
    /// </summary>
    public sealed class WorldInputFeedbackTests
    {
        private InputSettings.BackgroundBehavior _background;
#if UNITY_EDITOR
        private InputSettings.EditorInputBehaviorInPlayMode _editorBehaviour;
#endif

        /// <summary>A headless run is never focused; without this every queued event applies to a disabled device.</summary>
        [SetUp]
        public void IgnoreApplicationFocus()
        {
            _background = InputSystem.settings.backgroundBehavior;
            InputSystem.settings.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus;
#if UNITY_EDITOR
            _editorBehaviour = InputSystem.settings.editorInputBehaviorInPlayMode;
            InputSystem.settings.editorInputBehaviorInPlayMode =
                InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
#endif
        }

        [TearDown]
        public void RestoreFocusBehaviour()
        {
            InputSystem.settings.backgroundBehavior = _background;
#if UNITY_EDITOR
            InputSystem.settings.editorInputBehaviorInPlayMode = _editorBehaviour;
#endif
        }

        /// <summary>
        /// The ghost is shown while a machine kind is in hand and gone when the hand is emptied. Nobody called
        /// <see cref="PlacementPreviewPresenter.Show"/> before C-13, so this is the whole of the preview path:
        /// action bar → digit → <c>WorldInput.Tool</c> → the presenter.
        /// </summary>
        [UnityTest, Timeout(60000)]
        public IEnumerator AMachineInHandShowsTheGhostAndPuttingItAwayHidesIt()
        {
            yield return SceneFixture.LoadWorld();
            var host = Object.FindAnyObjectByType<SimHost>();
            var shell = Object.FindAnyObjectByType<UiShell>();
            var input = Object.FindAnyObjectByType<WorldInput>();
            var preview = Object.FindAnyObjectByType<PlacementPreviewPresenter>();
            Assert.That(input, Is.Not.Null, "World.unity has no WorldInput.");
            Assert.That(preview, Is.Not.Null, "World.unity has no PlacementPreviewPresenter.");

            var keyboard = VirtualKeyboard();
            var mouse = VirtualMouse();
            if (shell != null) shell.CloseActive();
            yield return null;

            // Slot 1 holds a machine kind. The bar is sim state, so it is set the way the UI sets it.
            var assign = host.Simulation.Apply(new AssignBarCommand(0, "chest"));
            Assert.That(assign.Accepted, Is.True, assign.Problem);

            yield return Point(mouse, new Vector2(Screen.width * 0.5f, Screen.height * 0.5f));

            yield return Press(keyboard, Key.Digit1);
            yield return Release(keyboard);
            Assert.That(input.Tool, Is.EqualTo("chest"), "the digit did not put the machine in hand.");
            yield return null;
            Assert.That(preview.Visible, Is.True, "a machine in hand drew no ghost.");

            // The same digit puts it away again; the ghost must go with it.
            yield return Press(keyboard, Key.Digit1);
            yield return Release(keyboard);
            Assert.That(input.Tool, Is.EqualTo(""), "the second press did not empty the hand.");
            yield return null;
            Assert.That(preview.Visible, Is.False, "the ghost outlived the hand that held it.");
        }

        /// <summary>
        /// Right-clicking a machine with the empty hand packs it (worldScene.ts:728-739). The placement is made
        /// through the sim so the test does not depend on the region's contents; if the engineer cannot afford or
        /// reach a chest in the authored start the test is ignored rather than failed — the removal rule itself is
        /// covered by the wave-1 sim tests, and this only proves the INPUT path reaches it.
        /// </summary>
        [UnityTest, Timeout(60000)]
        public IEnumerator RightClickWithTheEmptyHandPacksTheMachineUnderTheCursor()
        {
            yield return SceneFixture.LoadWorld();
            var host = Object.FindAnyObjectByType<SimHost>();
            var shell = Object.FindAnyObjectByType<UiShell>();
            var input = Object.FindAnyObjectByType<WorldInput>();
            var camera = Camera.main;
            Assert.That(input, Is.Not.Null, "World.unity has no WorldInput.");
            if (camera == null) Assert.Ignore("no main camera in the World scene.");

            var mouse = VirtualMouse();
            if (shell != null) shell.CloseActive();
            host.Paused = false;
            yield return null;

            var sim = host.Simulation;
            // The nearest free 2x2 footprint to the spawn: the tile one east of the imported spawn overlaps the
            // Home wall, which Placement.GeometryProblem refuses as a city structure (Phase C solid-tile rule).
            var (tx, ty) = SceneFixture.FreeTile(sim, "chest", 0);
            var placed = sim.Apply(new PlaceMachineCommand("chest", tx, ty, Dir.N));
            if (!placed.Accepted) Assert.Ignore("could not stand a chest beside the engineer: " + placed.Problem);

            var machine = ProductionRules.MachineAt(sim.State, tx, ty);
            Assert.That(machine, Is.Not.Null, "the chest was accepted but is not on the tile.");
            var id = machine.Id;

            // Put the cursor on the chest's own tile, in screen space, through the same projection WorldInput uses.
            var screen = camera.WorldToScreenPoint(WorldSpace.TileCentre(tx, ty));
            if (screen.z <= 0 || screen.x < 0 || screen.y < 0 || screen.x > Screen.width || screen.y > Screen.height)
                Assert.Ignore("the chest's tile is off screen; the camera framing is not this test's subject.");
            yield return Point(mouse, new Vector2(screen.x, screen.y));

            Assert.That(input.Tool, Is.EqualTo(""), "the hand must be empty for a pick-up.");
            yield return RightClick(mouse, new Vector2(screen.x, screen.y));
            yield return Until(() => sim.State.MachineById(id) == null);

            Assert.That(sim.State.MachineById(id), Is.Null,
                "right-clicking a machine with the empty hand did not submit a RemoveMachineCommand.");
        }

        /// <summary>
        /// A refusal the sim returns for a QUEUED command reaches the HUD's notice inbox. Placement is submitted
        /// far out of reach, so the answer is <c>Placement.Validity</c>'s "Walk closer to place the …".
        /// </summary>
        [UnityTest, Timeout(60000)]
        public IEnumerator ARefusedQueuedCommandReachesTheHudAsANotice()
        {
            yield return SceneFixture.LoadWorld();
            var host = Object.FindAnyObjectByType<SimHost>();
            var hud = Object.FindAnyObjectByType<HudController>();
            if (hud == null) Assert.Ignore("GameUI.unity has no HudController.");
            host.Paused = false;
            yield return null;

            var sim = host.Simulation;
            var e = sim.State.Engineer.Pos;
            var far = new PlaceMachineCommand("chest", (int)System.Math.Floor(e.X) + 60,
                                              (int)System.Math.Floor(e.Y) + 60, Dir.N);
            var expected = sim.Apply(far);            // the same question, answered synchronously
            if (expected.Accepted) Assert.Ignore("the far tile was buildable; this region has no out-of-reach corner here.");

            host.Submit(far);                          // and now the queued path the player uses
            yield return Until(() => Has(hud, expected.Problem));
            Assert.That(Has(hud, expected.Problem), Is.True,
                "the sim refused the queued placement and the HUD never said so.");
        }

        /// <summary>
        /// U-D-56. E interacts, and a weapon in the hand changes nothing about that. The old code asked what the
        /// hand held FIRST and, for a weapon, toggled it in and out of equipment slot 0 instead — so with the rifle
        /// selected the machine under the cursor only opened when the pointer happened to be resting exactly on it,
        /// the Home workshop could not be opened at all, no notice ever said why, and the holstered half left the
        /// rifle in hand but unequipped, after which <c>WeaponPhase</c> silently refused every shot.
        ///
        /// Both halves are asserted here: the machine opens, and the equipment is untouched by the press.
        /// </summary>
        [UnityTest, Timeout(60000)]
        public IEnumerator PressingEWithAWeaponInHandOpensTheMachineAndNeverUnequipsIt()
        {
            yield return SceneFixture.LoadWorld();
            var host = Object.FindAnyObjectByType<SimHost>();
            var shell = Object.FindAnyObjectByType<UiShell>();
            var input = Object.FindAnyObjectByType<WorldInput>();
            var camera = Camera.main;
            Assert.That(input, Is.Not.Null, "World.unity has no WorldInput.");
            if (camera == null) Assert.Ignore("no main camera in the World scene.");

            var keyboard = VirtualKeyboard();
            var mouse = VirtualMouse();
            if (shell != null) shell.CloseActive();
            host.Paused = false;
            yield return null;

            // The opening leaves the Rifle in the workshop tray (U-D-44), so the test mints its own through the
            // admin grant rather than depending on how far a fresh save has got.
            var sim = host.Simulation;
            var given = sim.Apply(new AdminCommand("weapon", "rifle"));
            if (!given.Accepted) Assert.Ignore("could not put a rifle in the pack: " + given.Problem);
            var owned = WeaponQueries.Owned(sim.State);
            var rifle = owned[owned.Count - 1].Id;
            var assign = sim.Apply(new AssignBarCommand(0, rifle));
            if (!assign.Accepted) Assert.Ignore("could not put the rifle on the action bar: " + assign.Problem);

            var (tx, ty) = SceneFixture.FreeTile(sim, "chest", 0);
            var placed = sim.Apply(new PlaceMachineCommand("chest", tx, ty, Dir.N));
            if (!placed.Accepted) Assert.Ignore("could not stand a chest beside the engineer: " + placed.Problem);
            var machine = ProductionRules.MachineAt(sim.State, tx, ty);
            Assert.That(machine, Is.Not.Null, "the chest was accepted but is not on the tile.");
            var id = machine.Id;

            var screen = camera.WorldToScreenPoint(WorldSpace.TileCentre(tx, ty));
            if (screen.z <= 0 || screen.x < 0 || screen.y < 0 || screen.x > Screen.width || screen.y > Screen.height)
                Assert.Ignore("the chest's tile is off screen; the camera framing is not this test's subject.");
            yield return Point(mouse, new Vector2(screen.x, screen.y));

            // Selecting a weapon on the bar equips it AND puts it in hand — that is SelectSlot's own rule, and it
            // is the state the player is in when they walk up to a machine with the rifle out.
            yield return Press(keyboard, Key.Digit1);
            yield return Release(keyboard);
            Assert.That(input.Tool, Is.EqualTo(rifle), "the digit did not put the rifle in hand.");
            Assert.That(WeaponQueries.Equipped(sim.State, sim.Context.Data).Item, Is.EqualTo(rifle),
                "selecting a weapon on the bar must equip it.");

            // The shell's own handler would open the drawer and disable the World map; intercepting asks the
            // narrower question this test is about — what the key DECIDED.
            var opened = -2;
            var handler = WorldInput.OpenMachine;
            WorldInput.OpenMachine = m => opened = m;
            try
            {
                yield return Press(keyboard, Key.E);
                yield return Release(keyboard);
                yield return null;
            }
            finally { WorldInput.OpenMachine = handler; }

            Assert.That(opened, Is.EqualTo(id),
                "E with a weapon in hand did not open the machine under the cursor.");
            Assert.That(WeaponQueries.Equipped(sim.State, sim.Context.Data).Item, Is.EqualTo(rifle),
                "E unequipped the rifle: interacting must never touch equipment (U-D-56).");
            Assert.That(input.Tool, Is.EqualTo(rifle), "E must not change what the hand holds.");
        }

        private static bool Has(HudController hud, string text)
        {
            var rows = hud.Model.Notices.Rows;
            for (var i = 0; i < rows.Count; i++) if (rows[i].Text == text) return true;
            return false;
        }

        // ---- devices -------------------------------------------------------------------------------------------

        private static Keyboard VirtualKeyboard()
        {
            var keyboard = InputSystem.GetDevice<Keyboard>() ?? InputSystem.AddDevice<Keyboard>();
            if (!keyboard.enabled) InputSystem.EnableDevice(keyboard);
            return keyboard;
        }

        private static Mouse VirtualMouse()
        {
            var mouse = InputSystem.GetDevice<Mouse>() ?? InputSystem.AddDevice<Mouse>();
            if (!mouse.enabled) InputSystem.EnableDevice(mouse);
            return mouse;
        }

        private static IEnumerator Point(Mouse mouse, Vector2 at)
        {
            InputSystem.QueueStateEvent(mouse, new MouseState { position = at });
            yield return null;
            yield return null;
        }

        private static IEnumerator RightClick(Mouse mouse, Vector2 at)
        {
            InputSystem.QueueStateEvent(mouse, new MouseState { position = at }.WithButton(MouseButton.Right));
            yield return null;
            yield return null;
            InputSystem.QueueStateEvent(mouse, new MouseState { position = at });
            yield return null;
        }

        private static IEnumerator Press(Keyboard keyboard, Key key)
        {
            InputSystem.QueueStateEvent(keyboard, new KeyboardState(key));
            yield return null;
            yield return null;
        }

        private static IEnumerator Release(Keyboard keyboard)
        {
            InputSystem.QueueStateEvent(keyboard, new KeyboardState());
            yield return null;
            yield return null;
        }

        /// <summary>Wait, up to <paramref name="seconds"/>, for something a queued command has to reach.</summary>
        private static IEnumerator Until(System.Func<bool> condition, float seconds = 2f)
        {
            var deadline = Time.realtimeSinceStartup + seconds;
            while (!condition() && Time.realtimeSinceStartup < deadline) yield return null;
        }
    }
}
