using Relight.Sim;
using Relight.World;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Relight.Presentation
{
    /// <summary>
    /// B-13, extended by C-02/C-03. Turns the <c>World</c> and the always-live <c>Global</c> action maps into sim
    /// commands, and nothing else: it draws nothing and never writes <see cref="SimState"/>. Every effect on the
    /// world is a <see cref="Command"/> handed to <see cref="SimHost.Submit"/> (TECHNICAL_ARCHITECTURE.md §2.5).
    /// It reads the sim only through queries (<see cref="MiningQueries"/>, <see cref="WeaponQueries"/>), never the
    /// state object.
    ///
    /// Bindings are ported from the reference's world controls (worldScene.ts:660-760). WASD/arrows drive the
    /// engineer's velocity (<c>WalkCommand</c>), Shift sprints, Space dodges. The left button "does what the hand
    /// holds" (worldScene.ts:743 D-B1-5), which is the one rule that decides mine / fire / place / nothing:
    /// <list type="bullet">
    /// <item>empty hand over a minable tile → hold to mine (worldScene.ts:810), release stops the hands;</item>
    /// <item>empty hand anywhere else → NOTHING (C-02 correction; see below);</item>
    /// <item>a weapon in hand → hold the trigger, the cursor is the aim (worldScene.ts:748, :811);</item>
    /// <item>a machine kind in hand → place it at the clicked tile with the ghost's facing (worldScene.ts:756).</item>
    /// </list>
    /// The empty-hand click used to submit a <c>MoveCommand</c> ("walk here"). The correction pass deletes it: the
    /// engineer walks on WASD and on nothing else, so a misjudged click on the ground can no longer march him off
    /// across the map. There is no <c>MoveCommand</c> anywhere in this class any more, by design — if one
    /// reappears, click-to-move is back.
    ///
    /// The right button empties the hand, exactly as the reference does (worldScene.ts:728), and with the hand
    /// already empty it packs the machine under the cursor back into the Backpack (worldScene.ts:728-739). C-13
    /// adds the other two halves of "placement, rotation and removal with validity feedback": the ghost follows
    /// the cursor through <see cref="PlacementPreviewPresenter"/> while a machine kind is in hand, and every
    /// refusal the sim returns for a queued command reaches the HUD as a notice (<c>CommandResultEvent</c>), so
    /// no rule is ever restated here.
    ///
    /// What is in the hand comes from two places: the action bar (digits 1-9 and 0 select bar slot 0..9,
    /// <c>WeaponQueries.Bar</c>, UI_AND_ONBOARDING §5.5) and the build menu, which calls <see cref="HoldTool"/>
    /// with a machine kind that need not be on the bar at all ("select = place now"). The bar is sim state; what
    /// the hand holds is not, so it lives here and is published as <see cref="Tool"/>/<see cref="PlacementActive"/>
    /// with a <see cref="HandChanged"/> event for the toolbar to follow.
    ///
    /// Clicks that begin on interface never reach the world (C-02). While a drawer is open the whole World map is
    /// off, so that half needs no guard; but the HUD and the build toolbar are visible with the world live, and a
    /// press on them used to place a machine or fire the rifle underneath. <see cref="UiPointerProbe"/> is the
    /// test, assigned by <c>UiShell</c> (Relight.Presentation cannot reference Relight.UI, so the delegate is
    /// pushed in rather than pulled out). A press that starts over interface is remembered and BOTH it and its
    /// release are ignored, which is what makes a drag out of the Backpack end harmlessly over the world.
    ///
    /// R is the documented conflict (C-02): it rotates while a machine ghost is in hand, and otherwise reloads. It
    /// is one action, resolved here, so the two can never both fire. It sits on the <c>Global</c> map because
    /// <c>InputRouter.Route</c> disables the whole <c>World</c> map while a panel is open and the C-03 acceptance
    /// criterion is that reload works with the Backpack open (see the report's note for C-06).
    ///
    /// Held intents (TA §8.3 row 2): the walk vector, the sprint flag, the mining hands and the held trigger are
    /// all state IN THE SIM. Their release edge happens behind an open panel and never reaches here, so all four
    /// are cleared once as the map goes off (reference uiShell.ts:85-92 <c>held</c>/<c>suppressed</c>).
    /// </summary>
    [DisallowMultipleComponent]
    [AddComponentMenu("Relight/World Input")]
    public sealed class WorldInput : MonoBehaviour
    {
        [Tooltip("The host commands are submitted to. Found on this object if left empty.")]
        [SerializeField] private SimHost host;

        [Tooltip("The shared RelightControls asset. This component touches its World and Global maps.")]
        [SerializeField] private InputActionAsset actions;

        [Tooltip("Camera used to turn a screen point into a world point. Main camera if left empty.")]
        [SerializeField] private Camera worldCamera;

        [Tooltip("Draws the ghost while a machine is in hand. Found on this object, then in the scene, if left empty.")]
        [SerializeField] private PlacementPreviewPresenter preview;

        private InputActionMap _world, _global;
        private InputAction _move, _pointer, _click, _place, _sprint, _dodge, _slots, _interact, _reload;
        private Vector2 _lastWalk = Vector2.positiveInfinity;
        /// <summary>Sprint is a PERSISTENT intent; it must be cleared, not just stopped.</summary>
        private bool _sprinting;
        /// <summary>So is the mining hand, and so is the held trigger.</summary>
        private bool _mining, _firing;
        private int _mineX, _mineY;
        /// <summary>The action-bar slot in hand, or -1 for the empty hand (reference <c>this.tool==='hand'</c>).</summary>
        private int _tool = -1;
        /// <summary>The ghost's facing. The ghost is presentation-only, so its rotation never leaves this class.</summary>
        private Dir _ghostDir = Dir.N;
        /// <summary>Set when the current left-button press began over interface; it and its release are ignored.</summary>
        private bool _pressOverUi;
        private Vector2Int? _undergroundStart;
        private int _inputSession = -1;
        public bool ChoosingUndergroundExit => _undergroundStart.HasValue;

        /// <summary>
        /// C-02. "Is this screen point on a piece of interface?", assigned by <c>UiShell.OnEnable</c> and cleared
        /// by its <c>OnDisable</c>. Static because the shell lives on the GameUI document object and this
        /// component lives in World.unity, and because Relight.Presentation must not reference Relight.UI. Null
        /// (tests, Boot, a scene with no shell) means "nothing is over the pointer", which is the old behaviour.
        /// </summary>
        public static System.Func<Vector2, bool> UiPointerProbe;
        public static System.Func<bool> UiTextInputFocused;
        public static System.Action<int> OpenMachine;
        public static System.Action<string> InteractionNotice;

        /// <summary>
        /// C-03. What the hand holds: "" when empty, else a machine kind id ("generator") or a weapon item key
        /// ("rifle"). Read by the HUD, the action bar and the build toolbar.
        /// </summary>
        public string Tool { get; private set; } = "";

        /// <summary>C-03. The ghost's facing while a machine is in hand; for whatever draws the ghost.</summary>
        public Dir GhostDir => _ghostDir;
        public int SelectedBarSlot => _tool;
        public bool TryHoverTile(out int x, out int y)
        {
            x = y = 0;
            return _world != null && _world.enabled && host?.Simulation != null
                && !host.Paused && UiPointerProbe?.Invoke(_pointer.ReadValue<Vector2>()) != true
                && TryPointerTile(out x, out y) && Ground.InBounds(host.Simulation.Context, x, y);
        }

        /// <summary>C-03. True when what is in the hand is a machine kind, i.e. a left click would place it.</summary>
        public bool PlacementActive => IsMachineKind(Tool);

        /// <summary>C-03. Raised after any change of <see cref="Tool"/> or <see cref="GhostDir"/>.</summary>
        public event System.Action HandChanged;

        /// <summary>
        /// C-03. Put <paramref name="kind"/> in the hand; "" (or null) empties it. The kind does NOT have to be on
        /// the action bar — the build menu's "select = place now" passes a machine kind straight through — so the
        /// action-bar slot is forgotten, and the next press of that digit selects rather than clears.
        /// Resets <see cref="GhostDir"/> to <see cref="Dir.N"/> and raises <see cref="HandChanged"/>.
        /// </summary>
        public void HoldTool(string kind) => SetHand(kind, -1);

        /// <summary>C-03. Empty the hand. Identical to <c>HoldTool("")</c>.</summary>
        public void ClearHand() => HoldTool("");

        /// <summary>
        /// C-03. The quarter turn R performs while a machine ghost is in hand. Rotating a GHOST is presentation
        /// state — the machine does not exist yet — so it is local and travels with the eventual place command.
        /// Does nothing with an empty hand or a weapon in hand.
        /// </summary>
        public void RotateGhost()
        {
            if (!PlacementActive) return;
            _ghostDir = (Dir)(((int)_ghostDir + 1) & 3);
            HandChanged?.Invoke();
        }

        /// <summary>
        /// C-04 link 400. Escape with no panel open and something in the hand puts it away, and consumes the key
        /// so it does not also pause. Returns false with an empty hand, which lets the chain fall through to pause.
        /// The held trigger is dropped with it: a weapon put away mid-burst must not keep firing.
        /// </summary>
        public bool CancelHand()
        {
            if (Tool.Length == 0) return false;
            if (_firing && host != null && host.Simulation != null) host.Submit(new HoldFireCommand());
            _firing = false;
            ClearHand();
            return true;
        }

        private void SetHand(string kind, int slot)
        {
            if (_mining && host?.Simulation != null) host.Submit(new StopMiningCommand());
            if (_firing && host?.Simulation != null) host.Submit(new HoldFireCommand());
            _mining = _firing = false;
            _undergroundStart = null;
            var next = kind ?? "";
            var changed = !string.Equals(next, Tool, System.StringComparison.Ordinal) || _ghostDir != Dir.N;
            _tool = slot;
            Tool = next;
            _ghostDir = Dir.N;
            if (changed) HandChanged?.Invoke();
        }

        private void Awake()
        {
            if (host == null) host = GetComponent<SimHost>();
            if (worldCamera == null) worldCamera = Camera.main;
            // C-13: nobody drove the ghost before. It sits on the Sim object beside this component in World.unity,
            // so the local lookup answers; the scene search is the fallback and null is allowed (tests, Boot).
            if (preview == null) preview = GetComponent<PlacementPreviewPresenter>();
            if (preview == null) preview = FindAnyObjectByType<PlacementPreviewPresenter>();
            if (actions == null) return;
            _world = actions.FindActionMap("World", false);
            if (_world == null) { Debug.LogError("Relight: RelightControls has no 'World' action map."); return; }
            _move = _world.FindAction("Move", false);
            _pointer = _world.FindAction("Point", false);
            _click = _world.FindAction("Click", false);
            _place = _world.FindAction("Place", false);
            _sprint = _world.FindAction("Sprint", false);
            _dodge = _world.FindAction("Dodge", false);
            // U-D-56: the action is still ASSET-named "Equip" because that id is what a saved rebinding and
            // BindingMap's "interact" row key off; renaming it would silently drop every player's own binding.
            // The key itself only interacts.
            _interact = _world.FindAction("Equip", false);
            _global = actions.FindActionMap("Global", false);
            _slots = _global?.FindAction("Slot", false);
            _reload = _global?.FindAction("Reload", false);
        }

        private void OnEnable()
        {
            if (_world != null) _world.Enable();
        }

        private void OnDisable()
        {
            if (_world != null) _world.Disable();
            ClearHeld();
            HidePreview();
            _lastWalk = Vector2.positiveInfinity;
        }

        /// <summary>
        /// Drop every intent that is STATE in the sim. Called when a panel takes input and when this is disabled:
        /// without it a held key would keep the engineer sprinting, digging or shooting with nothing pressed.
        /// </summary>
        private void ClearHeld()
        {
            if (host != null && host.Simulation != null)
            {
                if (_lastWalk != Vector2.zero) host.Submit(new WalkCommand(0, 0));
                if (_sprinting) host.Submit(new SprintCommand(false));
                if (_mining) host.Submit(new StopMiningCommand());
                if (_firing) host.Submit(new HoldFireCommand());
            }
            _lastWalk = Vector2.zero;
            _sprinting = false;
            _mining = false;
            _firing = false;
            _pressOverUi = false;
        }

        private void Update()
        {
            if (host == null || host.Simulation == null || _world == null) { HidePreview(); return; }
            var sim = host.Simulation;
            if (_inputSession != host.Session)
            {
                _inputSession = host.Session;
                ClearHand();
                ClearHeld();
            }

            if(host.Paused) { ClearHeld(); HidePreview(); return; }

            // One probe per frame, shared by every branch below: the pointer cannot be over the HUD for the right
            // button and not for R. Null probe (no shell in the scene) means "never over UI".
            var overUi = UiPointerProbe != null && _pointer != null && UiPointerProbe(_pointer.ReadValue<Vector2>());

            // The Global map is never disabled, so these two run whether or not a panel is open. Reload with the
            // Backpack open is a C-03 acceptance criterion; the digits pick the hand, which the panels also show.
            if (UiTextInputFocused?.Invoke() != true && _slots != null && _slots.WasPressedThisFrame()) SelectSlot(sim, DigitPressed());
            if (UiTextInputFocused?.Invoke() != true && _reload != null && _reload.WasPressedThisFrame())
            {
                // R: rotate the ghost when a machine is in hand, else turn the machine under the cursor, else
                // reload. Rotating a GHOST is presentation state — the machine does not exist yet — so it is a
                // local quarter turn, sent with the place. Rotating a PLACED machine is the sim's
                // (worldScene.ts:505-508); its refusals — "this does not turn", "Walk closer to rotate" — arrive
                // as HUD notices, so nothing is re-decided here. The world branch needs the World map live: a
                // panel holding input must not turn a belt behind it, while reload with the Backpack open stays.
                if (PlacementActive) RotateGhost();
                else if (Tool.Length == 0 && _world.enabled && !overUi && TryPointerTile(out var rx, out var ry)
                         && ProductionRules.MachineAt(sim.State, rx, ry) is Machine turn)
                    host.Submit(new RotatePlacementCommand(turn.Id));
                else host.Submit(new ReloadCommand());
            }

            if (!_world.enabled) { ClearHeld(); HidePreview(); return; }

            // Movement is a level, not an edge: the sim holds a velocity until told otherwise, so only a CHANGE is
            // submitted, and a key held across a map disable/enable therefore re-fires nothing (TA §8.3 row 2).
            var walk = _move != null ? _move.ReadValue<Vector2>() : Vector2.zero;
            if (walk.sqrMagnitude > 1f) walk = walk.normalized;
            if (walk != _lastWalk)
            {
                _lastWalk = walk;
                // Screen up is sim -Y (U-M-14): the flip belongs to WorldSpace's convention, spelled once here.
                host.Submit(new WalkCommand(walk.x, -walk.y));
            }

            if (_sprint != null && _sprint.WasPressedThisFrame()) { _sprinting = true; host.Submit(new SprintCommand(true)); }
            if (_sprint != null && _sprint.WasReleasedThisFrame()) { _sprinting = false; host.Submit(new SprintCommand(false)); }
            if (_dodge != null && _dodge.WasPressedThisFrame()) host.Submit(new DodgeCommand());
            if (_interact != null && _interact.WasPressedThisFrame() && !overUi) Interact(sim);

            // The pointer is resolved before the right button so a pick-up knows which tile it is over. Resolving
            // it submits nothing, so the order in which commands reach the sim is unchanged.
            var hasPointer = TryPointer(out var at);
            var tx = hasPointer ? (int)System.Math.Floor(at.X) : 0;
            var ty = hasPointer ? (int)System.Math.Floor(at.Y) : 0;

            // The right button empties the hand (worldScene.ts:728). The old B-13 "right click places a chest" is
            // retired: placement now goes through the action bar like every other tool. With the hand ALREADY
            // empty it picks the machine under the cursor back up (worldScene.ts:728-739). The reference tests
            // reach and pocket space itself before dispatching; here the sim alone decides — Placement.CanPickUp
            // words "the Depot stays", "Walk closer to the …" and "the Backpack is full (…)", and C-13 puts those
            // on the HUD — so presentation never holds a second copy of the rule.
            if (_place != null && _place.WasPressedThisFrame() && !overUi)
            {
                if (Tool.Length > 0) ClearHand();
                else if (hasPointer && ProductionRules.MachineAt(sim.State, tx, ty) is Machine pick)
                    host.Submit(new RemoveMachineCommand(pick.Id));
            }

            // The ghost follows the cursor while a machine kind is in hand, and goes away the moment it is not.
            UpdatePreview(sim, hasPointer && !overUi, tx, ty);

            var down = _click != null && _click.IsPressed();
            var pressed = _click != null && _click.WasPressedThisFrame();
            var released = _click != null && _click.WasReleasedThisFrame();

            // C-02. A press is owned by whoever it STARTED on. Deciding once, on the press edge, is what makes a
            // drag that begins in the Backpack and ends over the world drop nothing into the world: the press was
            // the UI's, so its release is too. The reverse also holds — a press that starts on the world keeps the
            // world's attention even if the cursor crosses the HUD, so mining does not stutter at the screen edge.
            if (pressed) _pressOverUi = overUi;
            var ignoreClick = _pressOverUi;
            if (released) _pressOverUi = false;
            if (ignoreClick) { pressed = false; down = false; released = false; }

            if (!hasPointer) return;
            var tool = Tool;

            if (tool.Length > 0 && IsMachineKind(tool))
            {
                if (pressed)
                {
                    if (tool == FlowBuild.Kind) PlaceUnderground(sim, tx, ty);
                    else host.Submit(new PlaceMachineCommand(tool, tx, ty, _ghostDir));
                }
                return;
            }

            if (tool.Length > 0)
            {
                // A weapon in hand: hold to fire, and the cursor is the aim every frame it is held
                // (worldScene.ts:748 press, :811 drag). Release lets go of the trigger. Aiming at a button is not
                // aiming at the world, so a trigger held over interface stops rather than shoots through it.
                if (down && !overUi) { _firing = true; host.Submit(new FireCommand(at.X, at.Y)); }
                else if (_firing) { _firing = false; host.Submit(new HoldFireCommand()); }
                return;
            }

            // The empty hand. Hold on rubble to mine it; a click on anything else does NOTHING — no walk-here
            // (C-02): the engineer moves on WASD alone, so a stray click on the ground is simply a stray click.
            if (down && !overUi && WorldTargetQueries.Resource(sim.Context, sim.State, tx, ty, out _, out _))
            {
                if (!_mining || tx != _mineX || ty != _mineY)
                {
                    _mining = true; _mineX = tx; _mineY = ty;
                    host.Submit(new MineCommand(tx, ty));   // worldScene.ts:810 re-aims as the cursor crosses tiles
                }
                return;
            }
            if (_mining) { _mining = false; host.Submit(new StopMiningCommand()); }
        }

        /// <summary>Which digit produced the press: 1-9 are bar slots 0-8 and 0 is slot 9 (§5.5). -1 if unknown.</summary>
        private int DigitPressed()
        {
            var name = _slots?.activeControl?.name;
            if (string.IsNullOrEmpty(name) || name.Length != 1) return -1;
            var c = name[0];
            if (c < '0' || c > '9') return -1;
            return c == '0' ? 9 : c - '1';
        }

        public void SelectBarSlot(int slot)
        {
            if (host != null && host.Simulation != null) SelectSlot(host.Simulation, slot);
        }

        private void SelectSlot(Simulation sim, int slot)
        {
            if (slot < 0) return;
            var bar = WeaponQueries.Bar(sim.State);
            var key = slot < bar.Count ? bar[slot] : "";
            if (string.IsNullOrEmpty(key)) { SetHand("", -1); return; }
            if (_tool == slot) { SetHand("", -1); return; }   // the same digit puts it away again
            if (new ItemKey(key).IsWeapon && WeaponQueries.Equipped(sim.State, sim.Context.Data).Item != key)
            {
                var result = sim.Apply(new EquipCommand(key, 0));
                if (!result.Accepted) { InteractionNotice?.Invoke(result.Problem); return; }
            }
            SetHand(key, slot);
        }

        /// <summary>
        /// True when <paramref name="key"/> names a machine kind rather than a weapon instance (<c>rifle:1</c>).
        /// Resolves the session itself so <see cref="PlacementActive"/> can be read at any time — before the first
        /// Update, from a UI callback, from a test. No session yet means "not a machine", not a crash.
        /// </summary>
        private bool IsMachineKind(string key)
        {
            if (string.IsNullOrEmpty(key)) return false;
            var sim = host == null ? null : host.Simulation;
            if (sim == null) return false;
            return !new ItemKey(key).IsWeapon && sim.Context.Data.TryMachine(key, out _);
        }

        private void PlaceUnderground(Simulation sim, int x, int y)
        {
            if (!_undergroundStart.HasValue)
            {
                var valid = Placement.Validity(sim.Context, sim.State, FlowBuild.Kind, x, y, _ghostDir);
                if (!valid.ok) { InteractionNotice?.Invoke(valid.reason); return; }
                _undergroundStart = new Vector2Int(x, y);
                HandChanged?.Invoke();
                return;
            }
            var start = _undergroundStart.Value;
            var result = sim.Apply(new PlaceUndergroundPairCommand(start.x, start.y, x, y, _ghostDir));
            if (result.Accepted) { _undergroundStart = null; HandChanged?.Invoke(); }
            else InteractionNotice?.Invoke(result.Problem);
        }

        /// <summary>
        /// U-D-56. E interacts, and interacting is ALL it does — reference worldScene.ts:28 "E interacts (the Depot
        /// chest, the workbench, a machine, the truck, survivors)" and :546. What is in the hand is not consulted at
        /// all, so the same press means the same thing with a rifle out as with an empty hand.
        ///
        /// It used to consult the hand. A weapon in it took the key — <c>Equip</c> toggled that weapon in and out of
        /// equipment slot 0 — and that branch sat ABOVE both the depot and the fallback notice, so with the rifle
        /// selected E only ever opened a machine when the POINTER was already resting on one: anywhere else it
        /// silently holstered, the Home workshop could not be opened at all (the HUD promises "[E] Home workshop"
        /// regardless), and no notice was ever shown to say why nothing had happened. Worse, the unholster half left
        /// the rifle IN HAND while unequipping it, so <c>WeaponPhase</c> then refused every shot ("Equip a Rifle")
        /// while the dock still showed the weapon selected. Equipping keeps its two honest routes — an action-bar
        /// digit (<see cref="SelectSlot"/> equips what it selects) and the Backpack's equipment slots.
        ///
        /// Every path ends in something the player can see: the machine opens, or a notice says why it did not.
        /// </summary>
        private void Interact(Simulation sim)
        {
            if (TryPointerTile(out var x, out var y))
            {
                var target = ProductionRules.MachineAt(sim.State, x, y);
                if (target != null)
                {
                    if (!Interaction.InReach(sim.Context, sim.State, target))
                    {
                        InteractionNotice?.Invoke("Walk closer to interact with this machine.");
                        return;
                    }
                    OpenMachine?.Invoke(target.Id);
                    return;
                }
                // INT-01: cargo dropped at a death. Pointing at the pile is the explicit way to pick one.
                var pointed = DeathCache.OnTile(sim.State, x, y);
                if (pointed != null)
                {
                    if (!DeathCache.InReach(sim.Context, sim.State, pointed)) InteractionNotice?.Invoke(DeathCache.ReachText);
                    else Collect(pointed);
                    return;
                }
            }
            // The workshop comes before a pile that merely lies nearby: a pile the Backpack has no room for stays
            // where it is, and it must never stand between the player and the Home workshop.
            if (HandCraft.NearDepot(sim.Context, sim.State)) { OpenMachine?.Invoke(-1); return; }
            // FRT-07: E puts a carried Power core down, or lifts one in reach. It comes before a cargo pile because
            // a death leaves both on one spot, and a pile can still be picked by pointing at it.
            if (CoreCarry.Holding(sim.State.Engineer)) { host.Submit(new DropCoreCommand()); return; }
            if (CoreCarry.NearestInReach(sim.Context, sim.State) != null) { host.Submit(new PickUpCoreCommand()); return; }
            var near = DeathCache.NearestInReach(sim.Context, sim.State);
            if (near != null) { Collect(near); return; }
            InteractionNotice?.Invoke("Point at a nearby machine and press E to open it.");
        }

        /// <summary>
        /// Issue the sim's own collect command, queued like every other gameplay input. Its result comes back as a
        /// <see cref="CommandResultEvent"/>, so "Recovered 40 from the dropped cargo." and "Backpack is full." both
        /// reach the HUD with no text of this class's own. What fits returns; the rest stays in the pile.
        /// </summary>
        private void Collect(DropCache pile)
        {
            host.Submit(new CollectCacheCommand(pile.Id));
        }

        /// <summary>
        /// C-13. Drive the ghost: it is shown only while a machine kind is in hand and the pointer resolves, and
        /// hidden the instant any of that stops being true. <see cref="PlacementPreviewPresenter"/> asks the sim
        /// whether the placement would be accepted and colours itself; this class decides nothing about validity.
        /// <c>Hide</c> is called only when something is showing, so an empty hand costs nothing per frame.
        /// </summary>
        private void UpdatePreview(Simulation sim, bool hasPointer, int tx, int ty)
        {
            if (preview == null) return;
            if (hasPointer && IsMachineKind(Tool))
            {
                if (Tool == FlowBuild.Kind && _undergroundStart.HasValue)
                    preview.ShowPair(_undergroundStart.Value.x, _undergroundStart.Value.y, tx, ty, _ghostDir);
                else preview.Show(Tool, tx, ty, _ghostDir);
            }
            else if (preview.Visible) preview.Hide();
        }

        /// <summary>Stop previewing, whatever the reason (no session, a panel took input, this was disabled).</summary>
        private void HidePreview()
        {
            if (preview != null && preview.Visible) preview.Hide();
        }

        /// <summary>The tile under the pointer. False when there is no pointer or no camera to project through.</summary>
        private bool TryPointerTile(out int tx, out int ty)
        {
            tx = 0;
            ty = 0;
            if (!TryPointer(out var p)) return false;
            tx = (int)System.Math.Floor(p.X);
            ty = (int)System.Math.Floor(p.Y);
            return true;
        }

        /// <summary>The sim position under the pointer (reference worldScene.ts:711 <c>aimAt</c>).</summary>
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
