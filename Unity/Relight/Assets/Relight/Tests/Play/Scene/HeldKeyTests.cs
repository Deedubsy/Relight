using System.Collections;
using NUnit.Framework;
using Relight.Presentation;
using Relight.UI;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.TestTools;

namespace Relight.Tests.Play
{
    /// <summary>
    /// B-13 acceptance, test 4 (TECHNICAL_ARCHITECTURE.md §8.3 row 2): a key held while a panel has input must not
    /// act on the world when the panel closes and the World map comes back.
    ///
    /// The reference needed an explicit guard for this — uiShell.ts:85-92 keeps a <c>held</c> set and a
    /// <c>suppressed</c> set, because in the DOM the keyup that happened while the UI had focus never reached the
    /// game. This port does not use <c>InputTestFixture</c> (the Input System's test fixture lives in a package test
    /// assembly that is not a `testable` of this project; enabling it would drag several hundred package tests into
    /// every run). It drives real virtual devices through the public low-level API instead, which exercises exactly
    /// the same code path as the fixture does.
    ///
    /// Three shapes of the rule are checked: a button action must not re-perform on re-enable; a movement key
    /// released while the map was off must leave the engineer standing still; and sprint — the one intent that
    /// persists as sim state rather than as a key level — must be cleared when the panel takes input, not left
    /// set until a release edge that never arrives (F6).
    /// </summary>
    public sealed class HeldKeyTests
    {
        private InputSettings.BackgroundBehavior _background;
#if UNITY_EDITOR
        private InputSettings.EditorInputBehaviorInPlayMode _editorBehaviour;
#endif

        /// <summary>
        /// A headless run is never focused, and the Input System's default background behaviour resets and disables
        /// every non-background device on an unfocused application: queued events then apply to no one and no action
        /// ever performs. Tell it to ignore focus for the duration of these two tests and put it back afterwards.
        /// </summary>
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

        [UnityTest, Timeout(30000)]
        public IEnumerator AHeldKeyDoesNotRefireWhenTheWorldMapComesBack()
        {
            yield return SceneFixture.LoadWorld();
            var router = Object.FindAnyObjectByType<InputRouter>();
            var shell = Object.FindAnyObjectByType<UiShell>();
            var host = Object.FindAnyObjectByType<SimHost>();
            Assert.That(router, Is.Not.Null, "GameUI.unity has no InputRouter.");
            Assert.That(router.Actions, Is.Not.Null, "the InputRouter has no action asset.");

            var world = router.Actions.FindActionMap("World", true);
            var dodge = world.FindAction("Dodge", true);
            var move = world.FindAction("Move", true);

            var keyboard = VirtualKeyboard();
            shell.CloseActive();
            yield return null;
            Assert.That(world.enabled, Is.True, "the World map should be live with no panel open.");

            var performed = 0;
            dodge.performed += _ => performed++;

            // Hold Space: one performed.
            yield return Press(keyboard, Key.Space);
            Assert.That(performed, Is.EqualTo(1), "the held key did not perform once.");

            // A panel takes input while the key is still held.
            shell.Open("status-panel");
            yield return null;
            Assert.That(world.enabled, Is.False, "opening a panel must disable the World map (TA 8.3 row 1).");
            yield return Hold(keyboard, Key.Space);

            // The panel closes with the key still held.
            shell.CloseActive();
            for (var i = 0; i < 3; i++) yield return null;
            Assert.That(world.enabled, Is.True);
            Assert.That(performed, Is.EqualTo(1),
                "the held key re-fired when the World map was re-enabled; a consume-until-release guard is needed.");

            yield return Release(keyboard);
            host.Paused = false;
        }

        [UnityTest, Timeout(30000)]
        public IEnumerator AMovementKeyReleasedWhileAPanelIsOpenLeavesTheEngineerStill()
        {
            yield return SceneFixture.LoadWorld();
            var router = Object.FindAnyObjectByType<InputRouter>();
            var shell = Object.FindAnyObjectByType<UiShell>();
            var host = Object.FindAnyObjectByType<SimHost>();
            var keyboard = VirtualKeyboard();

            shell.CloseActive();
            yield return null;

            yield return Press(keyboard, Key.D);          // walk east
            yield return Until(() => host.Simulation.State.Engineer.Vel.X != 0.0);
            var moved = host.Simulation.State.Engineer.Vel.X;
            Assert.That(moved, Is.Not.EqualTo(0.0), "the World map did not drive the engineer at all.");

            shell.Open("status-panel");                   // panel takes input; the engineer must stop
            yield return Until(() => host.Simulation.State.Engineer.Vel.X == 0.0);
            Assert.That(host.Simulation.State.Engineer.Vel.X, Is.EqualTo(0.0),
                "opening a panel left a held key driving the engineer.");

            yield return Release(keyboard);               // released while the panel had input
            shell.CloseActive();
            yield return Until(() => host.Simulation.State.Engineer.Vel.X != 0.0, 1f);
            Assert.That(host.Simulation.State.Engineer.Vel.X, Is.EqualTo(0.0),
                "a key released behind a panel moved the engineer when the World map came back.");
        }

        /// <summary>
        /// F6: sprint is the one intent that PERSISTS in the sim (<c>e.sprint</c>), so a Shift released while a
        /// panel owns input — no release edge ever reaches <c>WorldInput</c> — used to leave the engineer
        /// sprinting with nothing held, draining stamina, for the rest of the session.
        /// </summary>
        [UnityTest, Timeout(30000)]
        public IEnumerator SprintReleasedBehindAPanelDoesNotLeaveTheEngineerSprinting()
        {
            yield return SceneFixture.LoadWorld();
            var router = Object.FindAnyObjectByType<InputRouter>();
            var shell = Object.FindAnyObjectByType<UiShell>();
            var host = Object.FindAnyObjectByType<SimHost>();
            var world = router.Actions.FindActionMap("World", true);
            var keyboard = VirtualKeyboard();

            host.Paused = false;
            shell.CloseActive();
            yield return null;
            Assert.That(world.enabled, Is.True, "the World map should be live with no panel open.");

            yield return Press(keyboard, Key.LeftShift);
            yield return Until(() => host.Simulation.State.Engineer.Sprint);
            Assert.That(host.Simulation.State.Engineer.Sprint, Is.True, "Shift never reached the simulation.");

            // A panel takes input while Shift is still held.
            shell.Open("status-panel");
            yield return Until(() => !host.Simulation.State.Engineer.Sprint);
            Assert.That(world.enabled, Is.False, "opening a panel must disable the World map (TA 8.3 row 1).");
            Assert.That(host.Simulation.State.Engineer.Sprint, Is.False,
                "the panel took input with the sprint flag still set; a Shift released behind it can never clear it.");

            yield return Release(keyboard);          // released while the panel had input
            shell.CloseActive();
            for (var i = 0; i < 5; i++) yield return null;
            Assert.That(world.enabled, Is.True);
            Assert.That(host.Simulation.State.Engineer.Sprint, Is.False,
                "the engineer is sprinting with no key held after the panel closed.");

            // Control is recovered: a fresh press still sprints.
            yield return Press(keyboard, Key.LeftShift);
            yield return Until(() => host.Simulation.State.Engineer.Sprint);
            Assert.That(host.Simulation.State.Engineer.Sprint, Is.True,
                "the world stopped responding to Shift after the panel round trip.");
            yield return Release(keyboard);
        }

        /// <summary>
        /// A keyboard the test can drive. Batchmode has no real devices, and a headless player is never focused, so
        /// the Input System's background behaviour leaves an added device disabled: its state applies but its change
        /// monitors never run, and no action ever performs. Enabling it explicitly is what the package's own test
        /// fixture does for us when it is available.
        /// </summary>
        private static Keyboard VirtualKeyboard()
        {
            var keyboard = InputSystem.GetDevice<Keyboard>() ?? InputSystem.AddDevice<Keyboard>();
            if (!keyboard.enabled) InputSystem.EnableDevice(keyboard);
            return keyboard;
        }

        // Events are queued and left to the player loop's own input update; calling InputSystem.Update() by hand is
        // only supported in ProcessEventsManually mode and flips the state buffers out from under the loop.
        private static IEnumerator Press(Keyboard keyboard, Key key)
        {
            InputSystem.QueueStateEvent(keyboard, new KeyboardState(key));
            yield return null;
            yield return null;
        }

        private static IEnumerator Hold(Keyboard keyboard, Key key)
        {
            for (var i = 0; i < 3; i++)
            {
                InputSystem.QueueStateEvent(keyboard, new KeyboardState(key));
                yield return null;
            }
        }

        private static IEnumerator Release(Keyboard keyboard)
        {
            InputSystem.QueueStateEvent(keyboard, new KeyboardState());
            yield return null;
            yield return null;
        }

        /// <summary>Wait, up to <paramref name="seconds"/>, for a sim condition a queued command has to tick to reach.</summary>
        private static IEnumerator Until(System.Func<bool> condition, float seconds = 2f)
        {
            var deadline = Time.realtimeSinceStartup + seconds;
            while (!condition() && Time.realtimeSinceStartup < deadline) yield return null;
        }
    }
}
