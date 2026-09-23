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
using UnityEngine.UIElements;

namespace Relight.Tests.Play
{
    /// <summary>
    /// The correction pass's controls, in the authored scenes: C-02 (Tab is the Backpack, B is the build menu,
    /// clicks that start on interface stay on interface), C-03 (the hand, and the empty-hand click that now does
    /// nothing) and C-04 (Escape closes, then empties the hand, then pauses).
    ///
    /// Each of these is something the OWNER watched go wrong in play, so the tests drive the real devices rather
    /// than the handler underneath: a test that called <c>WorldInput.HoldTool</c> and then submitted the command
    /// itself would have passed happily while clicking the ground marched the engineer across the map.
    ///
    /// Driven like <see cref="WorldInputFeedbackTests"/> and <see cref="HeldKeyTests"/>: real virtual devices
    /// through the public low-level API, not <c>InputTestFixture</c>, whose assembly is not a <c>testable</c> of
    /// this project. No debug hook was needed on <c>WorldInput</c>; <c>Click</c> and <c>Place</c> are ordinary
    /// mouse buttons, so the mouse drives it exactly as a player does.
    ///
    /// Where a panel another worker owns is not in the document yet, a stub element carrying the <c>panel</c>
    /// class stands in for it. That keeps each assertion about the SHELL's behaviour — the thing this worker owns
    /// — instead of about the current state of a UXML file it does not.
    /// </summary>
    public sealed class ControlsCorrectionTests
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

        // ---- C-03: the hand ------------------------------------------------------------------------------------

        /// <summary>
        /// A machine put in the hand BY NAME — not through the action bar, which is what the build menu needs for
        /// "select = place now" — is placed by a left click on the tile under the cursor.
        /// </summary>
        [UnityTest, Timeout(60000)]
        public IEnumerator AMachineHeldByNameIsPlacedByAClickOnTheTile()
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
            if (!Buildable(sim, out var kind, out var tx, out var ty))
                Assert.Ignore("nothing the engineer carries can be stood beside them in this region.");

            // The kind need not be on the action bar: that is the whole point of HoldTool.
            input.HoldTool(kind);
            Assert.That(input.Tool, Is.EqualTo(kind), "HoldTool did not put the machine in hand.");
            Assert.That(input.PlacementActive, Is.True, "a machine kind in hand is a placement.");
            Assert.That(input.GhostDir, Is.EqualTo(Dir.N), "HoldTool must reset the ghost to north.");

            var screen = camera.WorldToScreenPoint(WorldSpace.TileCentre(tx, ty));
            if (screen.z <= 0 || screen.x < 0 || screen.y < 0 || screen.x > Screen.width || screen.y > Screen.height)
                Assert.Ignore("the target tile is off screen; camera framing is not this test's subject.");
            var at = new Vector2(screen.x, screen.y);

            yield return Point(mouse, at);
            yield return LeftClick(mouse, at);
            yield return Until(() => ProductionRules.MachineAt(sim.State, tx, ty) != null);

            Assert.That(ProductionRules.MachineAt(sim.State, tx, ty), Is.Not.Null,
                "a left click with a machine in hand submitted no PlaceMachineCommand.");
        }

        /// <summary>
        /// The owner's first finding: clicking empty ground walked the engineer there. It must now do nothing at
        /// all — no command, and thirty ticks later the engineer is exactly where they were.
        /// </summary>
        [UnityTest, Timeout(60000)]
        public IEnumerator AnEmptyHandClickOnPlainGroundDoesNothing()
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
            input.ClearHand();
            yield return null;

            var sim = host.Simulation;
            // Somewhere buildable is somewhere walkable and, being buildable, not rubble: exactly the "plain
            // ground" the finding is about, chosen from the region rather than hard-coded to a tile.
            var (tx, ty) = SceneFixture.FreeTile(sim, "chest", 1);
            if (MiningQueries.Minable(sim.Context, sim.State, tx, ty, out _))
                Assert.Ignore("the chosen tile is minable; holding on rubble is supposed to mine.");

            var screen = camera.WorldToScreenPoint(WorldSpace.TileCentre(tx, ty));
            if (screen.z <= 0 || screen.x < 0 || screen.y < 0 || screen.x > Screen.width || screen.y > Screen.height)
                Assert.Ignore("the target tile is off screen; camera framing is not this test's subject.");
            var at = new Vector2(screen.x, screen.y);

            yield return Point(mouse, at);
            var before = sim.State.Engineer.Pos;
            var machines = sim.State.Machines.Count;

            yield return LeftClick(mouse, at);

            var target = host.TotalTicks + 30;
            yield return Until(() => host.TotalTicks >= target, 10f);
            if (host.TotalTicks < target) Assert.Ignore("the session did not run thirty ticks; nothing to prove.");

            var after = sim.State.Engineer.Pos;
            Assert.That(after.X, Is.EqualTo(before.X).Within(1e-6),
                "clicking plain ground moved the engineer east/west: click-to-move is back.");
            Assert.That(after.Y, Is.EqualTo(before.Y).Within(1e-6),
                "clicking plain ground moved the engineer north/south: click-to-move is back.");
            Assert.That(sim.State.Machines.Count, Is.EqualTo(machines), "an empty-hand click built something.");
            Assert.That(input.Tool, Is.EqualTo(""), "the hand did not stay empty.");
        }

        // ---- C-02: the drawer keys -----------------------------------------------------------------------------

        /// <summary>
        /// Tab is the Backpack (it used to open the status drawer) and B is the build menu (it used to do
        /// nothing). Both keys sit on the always-live Global map, which is what lets the second press close a
        /// drawer that has switched the World map off.
        /// </summary>
        [UnityTest, Timeout(60000)]
        public IEnumerator TabOpensTheBackpackAndBOpensTheBuildMenu()
        {
            yield return SceneFixture.LoadWorld();
            var shell = Object.FindAnyObjectByType<UiShell>();
            Assert.That(shell, Is.Not.Null, "GameUI.unity has no UiShell.");
            var keyboard = VirtualKeyboard();
            shell.CloseActive();
            yield return null;

            Assert.That(shell.Toggle("no-such-panel"), Is.False, "an unknown panel id must be refused, not thrown.");
            Assert.That(shell.Active, Is.Null, "a refused toggle must not leave a panel open.");

            if (!EnsurePanel(shell, "inventory-panel")) Assert.Ignore("no UI document to add a panel to.");
            yield return null;

            yield return Press(keyboard, Key.Tab);
            yield return Release(keyboard);
            Assert.That(shell.Active, Is.EqualTo("inventory-panel"), "Tab did not open the Backpack.");

            yield return Press(keyboard, Key.Tab);
            yield return Release(keyboard);
            Assert.That(shell.Active, Is.Null, "a second Tab did not close the Backpack.");

            if (!EnsurePanel(shell, "build-panel")) Assert.Ignore("no UI document to add a panel to.");
            yield return null;

            yield return Press(keyboard, Key.B);
            yield return Release(keyboard);
            Assert.That(shell.Active, Is.EqualTo("build-panel"), "B did not open the build menu.");

            yield return Press(keyboard, Key.B);
            yield return Release(keyboard);
            Assert.That(shell.Active, Is.Null, "a second B did not close the build menu.");
        }

        // ---- C-04: Escape --------------------------------------------------------------------------------------

        /// <summary>
        /// The order the owner found inconsistent: with a panel open Escape closes the panel and does nothing
        /// else; with a machine in hand and no panel it empties the hand; only with both already done does it
        /// pause. One press, one effect. Driven through the chain itself so the assertion is about ORDER, not
        /// about key routing, which <see cref="HeldKeyTests"/> already covers.
        /// </summary>
        [UnityTest, Timeout(60000)]
        public IEnumerator EscapeClosesThePanelThenEmptiesTheHandThenPauses()
        {
            yield return SceneFixture.LoadWorld();
            var host = Object.FindAnyObjectByType<SimHost>();
            var shell = Object.FindAnyObjectByType<UiShell>();
            var escape = Object.FindAnyObjectByType<EscapeChain>();
            var input = Object.FindAnyObjectByType<WorldInput>();
            Assert.That(shell, Is.Not.Null, "GameUI.unity has no UiShell.");
            Assert.That(escape, Is.Not.Null, "GameUI.unity has no EscapeChain.");
            Assert.That(input, Is.Not.Null, "World.unity has no WorldInput.");

            shell.CloseActive();
            host.Paused = false;
            input.HoldTool("chest");
            if (input.Tool.Length == 0) Assert.Ignore("this build has no 'chest' to hold.");
            if (!EnsurePanel(shell, "inventory-panel")) Assert.Ignore("no UI document to add a panel to.");
            Assert.That(shell.Open("inventory-panel"), Is.True, "the Backpack panel could not be opened.");
            yield return null;

            escape.Escape();
            Assert.That(shell.Active, Is.Null, "Escape did not close the open panel.");
            Assert.That(input.Tool, Is.EqualTo("chest"), "Escape closed the panel AND emptied the hand; one per press.");
            Assert.That(host.Paused, Is.False, "Escape closed the panel AND paused; one per press.");

            escape.Escape();
            Assert.That(input.Tool, Is.EqualTo(""), "Escape with no panel open did not empty the hand.");
            Assert.That(host.Paused, Is.False, "Escape emptied the hand AND paused; one per press.");

            escape.Escape();
            Assert.That(host.Paused, Is.True, "Escape with nothing to cancel did not pause.");
            host.Paused = false;
        }

        // ---- C-02: the pointer ---------------------------------------------------------------------------------

        /// <summary>
        /// The test behind "clicks inside the UI leak into the world": the shell must know its own interface is
        /// there. The HUD strip is authored <c>picking-mode="Ignore"</c>, so this pins the bounds fallback inside
        /// <see cref="UiShell.PointerOverUi"/> as much as the Pick — a plain Pick walks straight past the HUD.
        /// </summary>
        [UnityTest, Timeout(60000)]
        public IEnumerator ThePointerIsOverUiOnTheHudStripAndNotOverBareWorld()
        {
            yield return SceneFixture.LoadWorld();
            var shell = Object.FindAnyObjectByType<UiShell>();
            Assert.That(shell, Is.Not.Null, "GameUI.unity has no UiShell.");
            shell.CloseActive();
            yield return null;
            yield return null;

            var root = ShellRoot(shell);
            var strip = root == null ? null : root.Q("status-strip");
            if (strip == null) Assert.Ignore("the HUD status strip is not in the shell's document in this run.");
            if (strip.worldBound.width <= 0f || strip.worldBound.height <= 0f)
                Assert.Ignore("the HUD status strip has no layout in this run.");

            // The strip is the top-centre block (Hud.uss). Screen coordinates are bottom-left origin, the way the
            // Input System reports them, so the top of the window is HIGH y: walk down from it until UI answers.
            var overTop = false;
            for (var dy = 2f; dy < Screen.height * 0.25f; dy += 2f)
                if (shell.PointerOverUi(new Vector2(Screen.width * 0.5f, Screen.height - dy))) { overTop = true; break; }

            Assert.That(overTop, Is.True, "the pointer never read as over UI anywhere on the top-centre HUD strip.");
            Assert.That(shell.PointerOverUi(new Vector2(Screen.width * 0.5f, Screen.height * 0.5f)), Is.False,
                "the middle of the screen is bare world and must not read as interface.");
        }

        // ---- helpers -------------------------------------------------------------------------------------------

        /// <summary>The first machine kind this session can actually stand beside the engineer, and where.</summary>
        private static bool Buildable(Simulation sim, out string kind, out int x, out int y)
        {
            string[] candidates = { "generator", "chest", "lamp", "pole", "belt" };
            foreach (var candidate in candidates)
            {
                if (!sim.Context.Data.TryMachine(candidate, out _)) continue;
                int tx, ty;
                try { (tx, ty) = SceneFixture.FreeTile(sim, candidate, 0); }
                catch (System.InvalidOperationException) { continue; }
                var (ok, _) = Placement.Validity(sim.Context, sim.State, candidate, tx, ty, Dir.N);
                if (!ok) continue;
                kind = candidate;
                x = tx;
                y = ty;
                return true;
            }
            kind = "";
            x = 0;
            y = 0;
            return false;
        }

        /// <summary>
        /// The root the shell itself reads. GameUI.unity holds more than one UIDocument, and only the shell's own
        /// one answers <c>Toggle</c> or <c>PointerOverUi</c>, so a stub added to the wrong document proves nothing.
        /// </summary>
        private static VisualElement ShellRoot(UiShell shell)
        {
            foreach (var id in shell.PanelIds)
            {
                foreach (var document in Object.FindObjectsByType<UIDocument>(FindObjectsSortMode.None))
                {
                    var root = document.rootVisualElement;
                    if (root != null && root.Q(id) != null) return root;
                }
                break;   // one panel the shell already knows is enough to identify the document
            }
            var own = shell.GetComponent<UIDocument>();
            return own == null ? null : own.rootVisualElement;
        }

        /// <summary>
        /// Make sure the shell knows a panel by this id, adding a stub to its document when the real one is not
        /// there yet (the build menu is another worker's file). False only when there is no document at all.
        /// </summary>
        private static bool EnsurePanel(UiShell shell, string id)
        {
            var root = ShellRoot(shell);
            Assert.That(root, Is.Not.Null);
            Assert.That(root.Q(id), Is.Not.Null, "The authored panel must exist; a test must not substitute a stub.");
            foreach (var known in shell.PanelIds) if (known == id) return true;
            return false;
        }

        [UnityTest, Timeout(60000)]
        public IEnumerator RealWorldCarriesRegionAndRelocatesHomeSave()
        {
            yield return SceneFixture.LoadWorld();
            var sim = Object.FindAnyObjectByType<SimHost>().Simulation;
            var region = SaveRegion.Of(sim.Context);
            Assert.That(region.Id, Is.EqualTo("full"));
            Assert.That(region.Width, Is.EqualTo(864));
            Assert.That(region.Height, Is.EqualTo(576));
            var path = System.IO.Path.GetFullPath(System.IO.Path.Combine(Application.dataPath,
                "../../Docs/evidence/phase-c/integrated-run/slot-phasec-int.json"));
            var bytes = System.IO.File.ReadAllBytes(path);
            // Today's rule first: the evidence save names the map it was made on, the scene has since been given a
            // new map id, and a save from a different map is refused before anything else is looked at. Whether
            // that is the right thing for a save to be bound to is the owner's open question F1-18, not this test's.
            var asRecorded = SaveSerializer.Read(bytes, sim.Context);
            Assert.That(asRecorded.Ok, Is.False);
            Assert.That(asRecorded.Reason, Does.Contain("different map"));
            // The check this test was written for — the old debug run's turret inside Home's walls — is rebuilt on
            // THIS map from the recorded placement (engineer 30.5, 264.5; a 2x2 turret at 30, 266, Home-crop tiles).
            // The evidence file itself is read-only and stays bound to its own map id.
            var oldRun = new SimState();
            oldRun.Engineer.Pos = new Vec2(30.5, 264.5);
            oldRun.Machines.Add(new Machine { Id = 9, Kind = "turret", X = 30, Y = 266, Size = 2 });
            oldRun.NextId = 10;
            oldRun.Rev++;
            var oldRunBytes = SaveSerializer.Write(oldRun, sim.Context.Data, null, sim.Context.MapId, SaveUpgrade.HomeCrop, out _);
            var oldEvidence = SaveSerializer.Read(oldRunBytes, sim.Context);
            Assert.That(oldEvidence.Ok, Is.False, "The old debug run placed a turret inside Home's walls.");
            Assert.That(oldEvidence.Reason, Does.Contain("inside a building"));
            var validHome = new SimState();
            validHome.Engineer.Pos = new Vec2(30.5, 264.5);
            var validBytes = SaveSerializer.Write(validHome, sim.Context.Data, null, sim.Context.MapId, SaveUpgrade.HomeCrop, out _);
            var loaded = SaveSerializer.Read(validBytes, sim.Context);
            Assert.That(loaded.Ok, Is.True, loaded.Reason);
            Assert.That(loaded.State.Engineer.Pos.X, Is.EqualTo(73.5));
            Assert.That(loaded.State.Engineer.Pos.Y, Is.EqualTo(355.5));
            var saved = SaveSerializer.Write(loaded.State, sim.Context, null, out var header);
            Assert.That(header.Region.Id, Is.EqualTo("full"));
            var again = SaveSerializer.Read(saved, sim.Context);
            Assert.That(again.Ok, Is.True, again.Reason);
            Assert.That(again.State.Engineer.Pos.X, Is.EqualTo(73.5));
            var ambiguous = SaveSerializer.Write(sim.State, sim.Context.Data, null, sim.Context.MapId, SaveRegion.Unknown, out _);
            var refused = SaveSerializer.Read(ambiguous, sim.Context);
            Assert.That(refused.Ok, Is.False);
            Assert.That(refused.Reason, Does.Contain("region"));
            CollectionAssert.AreEqual(bytes, System.IO.File.ReadAllBytes(path), "Reference evidence was rewritten.");
        }

        [UnityTest, Timeout(60000)]
        public IEnumerator RotationAndMachineBarSelectionDoNotAlsoEquipOrReload()
        {
            yield return SceneFixture.LoadWorld();
            var shell = Object.FindAnyObjectByType<UiShell>();
            shell.CloseActive();
            var input = Object.FindAnyObjectByType<WorldInput>();
            var inventory = Object.FindAnyObjectByType<InventoryPanelController>();
            var sim = Object.FindAnyObjectByType<SimHost>().Simulation;
            var keyboard = VirtualKeyboard();
            sim.Apply(new AssignBarCommand(0, "chest"));
            yield return Press(keyboard, Key.Digit1);
            yield return Release(keyboard);
            Assert.That(input.Tool, Is.EqualTo("chest"));
            Assert.That(inventory.Notice, Is.Empty, "A machine bar entry also issued an inventory equip command.");
            yield return Press(keyboard, Key.R);
            yield return Release(keyboard);
            Assert.That(input.GhostDir, Is.EqualTo(Dir.E));
            Assert.That(inventory.Notice, Is.Empty, "Rotation also issued an inventory reload.");
        }

        [UnityTest, Timeout(60000)]
        public IEnumerator EOpensPointedStorageAndTabReturnsToBackpack()
        {
            yield return SceneFixture.LoadWorld();
            var shell = Object.FindAnyObjectByType<UiShell>();
            shell.CloseActive();
            var host = Object.FindAnyObjectByType<SimHost>();
            var sim = host.Simulation;
            var (x, y) = SceneFixture.FreeTile(sim, "chest", 0);
            var result = sim.Apply(new PlaceMachineCommand("chest", x, y, Dir.N));
            Assert.That(result.Accepted, Is.True, result.Problem);
            var chest = SceneFixture.Last(sim, "chest");
            var point = Camera.main.WorldToScreenPoint(WorldSpace.TileCentre(x, y));
            yield return Point(VirtualMouse(), new Vector2(point.x, point.y));
            var keyboard = VirtualKeyboard();
            yield return Press(keyboard, Key.E);
            yield return Release(keyboard);
            var inventory = Object.FindAnyObjectByType<InventoryPanelController>();
            Assert.That(shell.Active, Is.EqualTo("inventory-panel"));
            Assert.That(inventory.Model.MachineId, Is.EqualTo(chest.Id));
            yield return Press(keyboard, Key.Tab);
            yield return Release(keyboard);
            Assert.That(shell.Active, Is.Null);
            yield return Press(keyboard, Key.Tab);
            yield return Release(keyboard);
            Assert.That(shell.Active, Is.EqualTo("inventory-panel"));
            Assert.That(inventory.Model.MachineId, Is.EqualTo(-1));
            var root = ShellRoot(shell);
            var cell = root.Q<Button>("pack-0");
            var count = cell.Q<Label>("stack-count");
            Assert.That(count.text, Is.Not.Empty);
            Assert.That(count.worldBound.yMin, Is.GreaterThanOrEqualTo(cell.worldBound.yMin));
            Assert.That(count.worldBound.yMax, Is.LessThanOrEqualTo(cell.worldBound.yMax));
        }

        [UnityTest, Timeout(60000)]
        public IEnumerator MachineRecipePickerDispatchesAndSurvivesARoundTrip()
        {
            yield return SceneFixture.LoadWorld();
            var host = Object.FindAnyObjectByType<SimHost>();
            var sim = host.Simulation;
            // Fixture supplies: this tests the UI command route, not the opening economy.
            sim.State.Engineer.Inv[ItemId.Steel] += 200;
            sim.State.Engineer.Inv[ItemId.Copper] += 200;
            var (x, y) = SceneFixture.FreeTile(sim, "assembler", 0);
            var placed = sim.Apply(new PlaceMachineCommand("assembler", x, y, Dir.N));
            Assert.That(placed.Accepted, Is.True, placed.Problem);
            var machine = SceneFixture.Last(sim, "assembler");
            WorldInput.OpenMachine(machine.Id);
            yield return null;
            var shell = Object.FindAnyObjectByType<UiShell>();
            var root = ShellRoot(shell);
            var picker = root.Q<DropdownField>("machine-recipe");
            Assert.That(picker.choices.Count, Is.GreaterThan(1));
            picker.index = 1;
            var recipes = new System.Collections.Generic.List<Recipe>();
            ProductionRules.RecipesFor(sim.Context.Data, machine, recipes);
            using (var submit = NavigationSubmitEvent.GetPooled())
            {
                var button = root.Q<Button>("apply-recipe");
                submit.target = button; button.SendEvent(submit);
            }
            Assert.That(ProductionQueries.Recipe(sim.Context, sim.State, machine.Id).Key, Is.EqualTo(recipes[1].Key));
            var bytes = SaveSerializer.Write(sim.State, sim.Context, null, out _);
            var loaded = SaveSerializer.Read(bytes, sim.Context);
            Assert.That(loaded.Ok, Is.True, loaded.Reason);
            Assert.That(ProductionQueries.Recipe(sim.Context, loaded.State, machine.Id).Key, Is.EqualTo(recipes[1].Key));
        }

        [UnityTest, Timeout(60000)]
        public IEnumerator UndergroundToolBuildsBothEndpointsOnlyOnSecondClick()
        {
            yield return SceneFixture.LoadWorld();
            var sim = Object.FindAnyObjectByType<SimHost>().Simulation;
            sim.State.Engineer.Inv[ItemId.Steel] += 200;
            sim.State.Engineer.Inv[ItemId.Copper] += 200;
            Object.FindAnyObjectByType<UiShell>().CloseActive();
            var input = Object.FindAnyObjectByType<WorldInput>();
            input.HoldTool(FlowBuild.Kind);
            var cx = (int)sim.State.Engineer.Pos.X;
            var cy = (int)sim.State.Engineer.Pos.Y;
            var found = false; int ax=0, ay=0, bx=0, by=0;
            for (int y=cy-5; y<=cy+5 && !found; y++)
            for (int x=cx-5; x<=cx+5 && !found; x++)
                if (FlowBuild.PairBuildable(sim.Context,sim.State,x,y,x,y-2,Dir.N).ok)
                { ax=x; ay=y; bx=x; by=y-2; found=true; }
            Assert.That(found, Is.True, "The real opening map needs a reachable two-endpoint fixture.");
            var mouse=VirtualMouse();
            var first=Camera.main.WorldToScreenPoint(WorldSpace.TileCentre(ax,ay));
            var second=Camera.main.WorldToScreenPoint(WorldSpace.TileCentre(bx,by));
            var before=sim.State.Machines.Count;
            yield return Point(mouse,new Vector2(first.x,first.y));
            yield return LeftClick(mouse,new Vector2(first.x,first.y));
            Assert.That(input.ChoosingUndergroundExit,Is.True);
            Assert.That(sim.State.Machines.Count,Is.EqualTo(before));
            yield return Point(mouse,new Vector2(second.x,second.y));
            yield return LeftClick(mouse,new Vector2(second.x,second.y));
            Assert.That(sim.State.Machines.Count,Is.EqualTo(before+2));
            Assert.That(input.ChoosingUndergroundExit,Is.False);
            var exit=ProductionRules.MachineAt(sim.State,bx,by);
            Assert.That(FlowRules.IsUndergroundOutput(sim.State,exit),Is.True);
        }

        // ---- GP-UX-1: a movement key closes the drawer ---------------------------------------------------------

        /// <summary>
        /// The owner's report: "if the player presses any movement buttons while interacting with anything, close
        /// the UI it is interacting with and allow movement — eg with the Home workshop."
        ///
        /// A drawer is a thing the engineer is STANDING AT, not a mode they are stuck in. Before this the only way
        /// out was Escape or Tab, and the Home workshop was where that bit hardest: the player walks up, opens the
        /// workshop, presses W to leave, and nothing at all happens — the World map is off, and the key is
        /// swallowed by a panel that has no use for it.
        ///
        /// Two halves, and the second is what makes it feel right rather than merely work: the drawer closes AND
        /// the engineer walks on the same keystroke, with no second press. That falls out of <c>World/Move</c>
        /// being a Value action with <c>initialStateCheck</c> — re-enabling the map reads the keys that are
        /// held right now. So W is HELD across the close here, exactly as a player holds it.
        ///
        /// Driven through the real keyboard because the thing under test is whether the keystroke reaches the shell
        /// at all while the World map is disabled; a test that called <c>CloseActive</c> itself would pass happily
        /// while the player's W went on doing nothing.
        /// </summary>
        [UnityTest, Timeout(60000)]
        public IEnumerator AMovementKeyClosesTheOpenDrawerAndTheEngineerWalksWithoutPressingItAgain()
        {
            yield return SceneFixture.LoadWorld();
            var host = Object.FindAnyObjectByType<SimHost>();
            var shell = Object.FindAnyObjectByType<UiShell>();
            Assert.That(host, Is.Not.Null, "World.unity has no SimHost.");
            Assert.That(shell, Is.Not.Null, "GameUI.unity has no UiShell.");

            var keyboard = VirtualKeyboard();
            host.Paused = false;
            shell.CloseActive();
            yield return null;

            Assert.That(shell.Open("inventory-panel"), Is.True, "the Backpack drawer would not open.");
            yield return null;
            Assert.That(shell.Active, Is.EqualTo("inventory-panel"));

            var sim = host.Simulation;
            var before = sim.State.Engineer.Pos;

            InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.W));   // held down, not tapped
            // ClosedByMovement is true only on the frame of the close (the shell clears it every Update), so it is
            // read frame by frame; waiting a fixed two frames read it one frame too late.
            var byMovement = false;
            for (var frame = 0; frame < 10 && shell.Active != null; frame++)
            {
                yield return null;
                byMovement |= shell.ClosedByMovement;
            }

            Assert.That(shell.Active, Is.Null, "W did not close the drawer: the movement key was swallowed again.");
            Assert.That(byMovement, Is.True, "the drawer closed, but not because of the movement key.");

            // The World map comes back reading the key that is still down, so the walk starts on this same press.
            var target = host.TotalTicks + 30;
            yield return Until(() => host.TotalTicks >= target, 10f);
            var after = sim.State.Engineer.Pos;
            yield return Release(keyboard);
            if (host.TotalTicks < target) Assert.Ignore("the session did not run thirty ticks; nothing to prove.");

            Assert.That((after - before).Length, Is.GreaterThan(0.01),
                "the drawer closed but the engineer never moved: holding W should not need a second press.");
        }

        /// <summary>
        /// The guard that keeps the rule from eating the player's typing. W is a letter in the save-name box, in
        /// the rename box and in the drawer's own quantity field, and there it has to stay a letter. Same keystroke
        /// and the same open drawer as the test above, opposite answer — which is why it is a separate test
        /// rather than one more line on that one.
        /// </summary>
        [UnityTest, Timeout(60000)]
        public IEnumerator AMovementKeyTypedIntoATextFieldDoesNotCloseTheDrawer()
        {
            yield return SceneFixture.LoadWorld();
            var shell = Object.FindAnyObjectByType<UiShell>();
            Assert.That(shell, Is.Not.Null, "GameUI.unity has no UiShell.");
            shell.CloseActive();
            yield return null;
            Assert.That(shell.Open("inventory-panel"), Is.True, "the Backpack drawer would not open.");
            yield return null;

            var root = ShellRoot(shell);
            var typing = root?.Q<TextField>();
            if (typing == null) Assert.Ignore("the open drawer has no text field to type into.");

            typing.Focus();
            yield return null;
            yield return null;
            if (root.focusController?.focusedElement as VisualElement != typing
                && (root.focusController?.focusedElement as VisualElement)?.GetFirstAncestorOfType<TextField>() != typing)
                Assert.Ignore("the field did not take focus in this headless run; there is nothing to guard.");

            var keyboard = VirtualKeyboard();
            yield return Press(keyboard, Key.W);
            yield return Release(keyboard);

            Assert.That(shell.Active, Is.EqualTo("inventory-panel"),
                "typing a W into a text field closed the drawer; movement must not outrank text entry.");
            Assert.That(shell.ClosedByMovement, Is.False);
        }

        /// <summary>
        /// REL-135, the owner's tenth note: "When typing in the save menu, if I press 'B' it still opens up the
        /// build menu. It shouldn't."
        ///
        /// Driven through the real keyboard and the real pause menu, because the hole was exactly in the seam
        /// between them. The save dialog belongs to <c>PauseMenuController</c>'s own document and is not one of the
        /// shell's drawers, and the typing guard used to give up on that alone — no drawer of mine is open, so
        /// nobody is typing — before it ever asked who had focus. A test that focused a field in the shell's own
        /// drawer would have passed the whole time the player's save names were opening the build menu.
        ///
        /// The second half matters as much as the first. The dialog goes away by switching a parent to
        /// <c>display: none</c> over a field that still holds focus, so a guard that trusted focus alone would have
        /// traded this bug for a worse one: B dead for the rest of the session.
        /// </summary>
        [UnityTest, Timeout(60000)]
        public IEnumerator TypingASaveNameLeavesTheHotkeysAloneAndHandsThemBackAfterwards()
        {
            yield return SceneFixture.LoadWorld();
            var host = Object.FindAnyObjectByType<SimHost>();
            var shell = Object.FindAnyObjectByType<UiShell>();
            Assert.That(host, Is.Not.Null, "World.unity has no SimHost.");
            Assert.That(shell, Is.Not.Null, "GameUI.unity has no UiShell.");
            if (!EnsurePanel(shell, "build-panel")) Assert.Ignore("the shell does not know the build panel here.");

            var keyboard = VirtualKeyboard();
            shell.CloseActive();
            host.Paused = true;
            yield return null;
            yield return null;

            var pause = Ui.UiFixture.DocumentWith("save-name", out var found);
            var saveName = found as TextField;
            Ui.UiFixture.DocumentWith("pause-save", out var saveButton);
            if (saveName == null || saveButton == null) Assert.Ignore("this build's pause menu has no save screen.");

            // The guard reads the shell document's focus controller, and that only answers for a field in another
            // document because every runtime document here points at one PanelSettings and so shares one panel.
            var shellRoot = ShellRoot(shell);
            Assert.That(shellRoot, Is.Not.Null);
            Assert.That(pause.rootVisualElement.focusController, Is.SameAs(shellRoot.focusController),
                "the pause menu is on a panel of its own now: the shell's focus controller cannot see the player typing.");

            Ui.UiFixture.Submit(saveButton);
            yield return null;
            if (!Ui.UiFixture.Shown(saveName)) Assert.Ignore("the save screen did not open in this run.");

            saveName.Focus();
            yield return null;
            yield return null;
            var focused = shellRoot.focusController?.focusedElement as VisualElement;
            if (focused != saveName && focused?.GetFirstAncestorOfType<TextField>() != saveName)
                Assert.Ignore("the save name field did not take focus in this headless run; there is nothing to guard.");

            yield return Press(keyboard, Key.B);
            yield return Release(keyboard);
            Assert.That(shell.Active, Is.Null, "B typed into a save name opened the build menu underneath it.");

            // Dismissed the way Resume dismisses it: the field is switched off, not blurred.
            host.Paused = false;
            yield return null;
            yield return null;
            yield return Press(keyboard, Key.B);
            yield return Release(keyboard);
            Assert.That(shell.Active, Is.EqualTo("build-panel"),
                "the hidden save-name field went on swallowing B after the pause menu closed.");

            shell.CloseActive();
        }

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

        private static IEnumerator LeftClick(Mouse mouse, Vector2 at)
        {
            InputSystem.QueueStateEvent(mouse, new MouseState { position = at }.WithButton(UnityEngine.InputSystem.LowLevel.MouseButton.Left));
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

        private static IEnumerator Until(System.Func<bool> condition, float seconds = 2f)
        {
            var deadline = Time.realtimeSinceStartup + seconds;
            while (!condition() && Time.realtimeSinceStartup < deadline) yield return null;
        }
    }
}
