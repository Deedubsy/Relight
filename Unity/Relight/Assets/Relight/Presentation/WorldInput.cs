using Relight.Sim;
using Relight.World;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Relight.Presentation
{
    /// <summary>
    /// B-13. Turns the <c>World</c> action map into sim commands, and nothing else: it reads no state, draws
    /// nothing, and never touches <see cref="SimState"/>. Every effect on the world is a
    /// <see cref="Command"/> handed to <see cref="SimHost.Submit"/> (TECHNICAL_ARCHITECTURE.md §2.5).
    ///
    /// Bindings are ported from the reference's world controls (worldScene.ts:660-760): WASD/arrows drive the
    /// engineer's velocity (<c>WalkCommand</c>, reference <c>cmd('walk', …)</c>), Shift sprints, Space dodges, and a
    /// left click walks to the clicked point (<c>MoveCommand</c>, reference <c>cmd('move', wx, wy)</c> after
    /// <c>worldToTile</c>). The right click places the <see cref="placeKind"/> machine at the clicked tile, which is
    /// the placeholder for C-02's ghost placement — there is no ghost, no cost preview and no rotation here.
    ///
    /// The whole map is disabled by <c>InputRouter</c> while a panel is open (TA §8.3 row 1). The one thing this
    /// component must still do about that is drop the intents that PERSIST in the sim — the walk vector and the
    /// sprint flag — once, as the map goes off: their release edge happens behind the panel and never reaches
    /// here (TA §8.3 row 2, reference uiShell.ts:85-92 <c>held</c>/<c>suppressed</c>). Dodge, click and place are
    /// one-shot commands and need nothing.
    /// </summary>
    [DisallowMultipleComponent]
    [AddComponentMenu("Relight/World Input")]
    public sealed class WorldInput : MonoBehaviour
    {
        [Tooltip("The host commands are submitted to. Found on this object if left empty.")]
        [SerializeField] private SimHost host;

        [Tooltip("The shared RelightControls asset. This component only ever touches its World map.")]
        [SerializeField] private InputActionAsset actions;

        [Tooltip("Camera used to turn a screen point into a world point. Main camera if left empty.")]
        [SerializeField] private Camera worldCamera;

        [Tooltip("Machine kind the placeholder place binding builds. C-02 replaces this with real ghost placement.")]
        [SerializeField] private string placeKind = "chest";

        private InputActionMap _world;
        private InputAction _move, _pointer, _click, _place, _sprint, _dodge;
        private Vector2 _lastWalk = Vector2.positiveInfinity;
        /// <summary>Sprint is the one PERSISTENT intent this component sends; it must be cleared, not just stopped.</summary>
        private bool _sprinting;

        private void Awake()
        {
            if (host == null) host = GetComponent<SimHost>();
            if (worldCamera == null) worldCamera = Camera.main;
            if (actions == null) return;
            _world = actions.FindActionMap("World", false);
            if (_world == null) { Debug.LogError("Relight: RelightControls has no 'World' action map."); return; }
            _move = _world.FindAction("Move", false);
            _pointer = _world.FindAction("Point", false);
            _click = _world.FindAction("Click", false);
            _place = _world.FindAction("Place", false);
            _sprint = _world.FindAction("Sprint", false);
            _dodge = _world.FindAction("Dodge", false);
        }

        private void OnEnable()
        {
            if (_world != null) _world.Enable();
        }

        private void OnDisable()
        {
            if (_world != null) _world.Disable();
            _lastWalk = Vector2.positiveInfinity;
            _sprinting = false;
        }

        private void Update()
        {
            if (host == null || host.Simulation == null || _world == null) return;
            if (!_world.enabled)
            {
                // A panel owns input. Stop the engineer rather than leaving a held key driving them while the
                // player types in a panel — the reference does the same by clearing its held set (uiShell.ts:85).
                if (_lastWalk != Vector2.zero)
                {
                    _lastWalk = Vector2.zero;
                    host.Submit(new WalkCommand(0, 0));
                }
                // Sprint is state in the sim (`e.sprint`), set by a press edge and cleared by a release edge. A
                // Shift released behind an open panel produces no release edge here — the map is off — so without
                // this the engineer would keep sprinting, draining stamina, with nothing held. Cleared once, with
                // the same "drop what is held" rule as the walk vector above (TA §8.3 row 2, uiShell.ts:85-92).
                if (_sprinting)
                {
                    _sprinting = false;
                    host.Submit(new SprintCommand(false));
                }
                return;
            }

            // Movement is a level, not an edge: the sim holds a velocity until told otherwise (MovementCommands.cs),
            // so only a CHANGE is submitted. A key held across a map disable/enable therefore cannot re-fire
            // anything — the value is the same and no command is sent (TA §8.3 row 2).
            var walk = _move != null ? _move.ReadValue<Vector2>() : Vector2.zero;
            if (walk.sqrMagnitude > 1f) walk = walk.normalized;
            if (walk != _lastWalk)
            {
                _lastWalk = walk;
                // Screen up is sim -Y (U-M-14): the flip belongs to WorldSpace's convention, spelled once here.
                host.Submit(new WalkCommand(walk.x, -walk.y));
            }

            // Edges, not levels: a key still held when the map comes back does not re-press (the Input System
            // re-enables an action in its default state), which is the held-key rule the router documents.
            if (_sprint != null && _sprint.WasPressedThisFrame()) { _sprinting = true; host.Submit(new SprintCommand(true)); }
            if (_sprint != null && _sprint.WasReleasedThisFrame()) { _sprinting = false; host.Submit(new SprintCommand(false)); }
            // Dodge, click and place carry no held state in the sim — one press, one command — so they need no
            // equivalent of the sprint clear above.
            if (_dodge != null && _dodge.WasPressedThisFrame()) host.Submit(new DodgeCommand());

            if (_click != null && _click.WasPressedThisFrame() && TryPointer(out var moveTo))
                host.Submit(new MoveCommand(moveTo.X, moveTo.Y));

            if (_place != null && _place.WasPressedThisFrame() && TryPointer(out var at))
                host.Submit(new PlaceMachineCommand(placeKind, (int)System.Math.Floor(at.X), (int)System.Math.Floor(at.Y)));
        }

        /// <summary>The sim position under the pointer (reference worldScene.ts:713 <c>aimAt</c> / :719 worldToTile).</summary>
        private bool TryPointer(out Vec2 p)
        {
            p = default;
            if (_pointer == null || worldCamera == null) return false;
            var screen = _pointer.ReadValue<Vector2>();
            var world = worldCamera.ScreenToWorldPoint(new Vector3(screen.x, screen.y, -worldCamera.transform.position.z));
            p = WorldSpace.Position(world);
            return true;
        }
    }
}
