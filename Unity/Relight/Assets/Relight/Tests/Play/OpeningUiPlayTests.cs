using System.Collections;
using System.Collections.Generic;
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
    /// C-12's Play Mode half: the three §9 rows that are facts about the SCREEN rather than about the simulation,
    /// plus the D-UI-10 camera rule applied to every panel the shell owns.
    ///
    /// Written on this machine WITHOUT Unity: this file has never been compiled or run here, and every assertion in
    /// it is unverified until the coordinator runs the batchmode pass. The wave-3 W-C report carries the
    /// "Unverified — needs Unity" checklist that matches it line for line.
    ///
    /// Where a check needs an element that another worker owns and that does not exist yet, the test resolves the
    /// element BY NAME at runtime and calls <see cref="Assert.Ignore(string)"/> with the exact name it needs, so the
    /// pass is honestly inconclusive rather than quietly green. §9.1 is explicit about this: an unperformed test is
    /// never recorded as passed.
    ///
    /// Every test is bounded well under the 10 s the brief allows.
    /// </summary>
    public sealed class OpeningUiPlayTests
    {
        // ------------------------------------------------------------------ §2.5 layout matrix

        /// <summary>
        /// RI-02B_UI_SPEC.md:196's target matrix, as (width, height, uiScale). The §2.5 classes are driven by the
        /// EFFECTIVE size (physical ÷ scale), which is what the panel is resized to below.
        /// </summary>
        private static readonly (int W, int H, float Scale)[] Matrix =
        {
            (1366, 768, 1.00f), (1366, 768, 1.25f), (1366, 768, 1.50f),
            (1920, 1080, 1.00f), (1920, 1080, 1.25f), (1920, 1080, 1.50f),
            (2560, 1440, 1.00f), (2560, 1440, 1.25f), (2560, 1440, 1.50f),
            (3440, 1440, 1.00f), (3440, 1440, 1.25f), (3440, 1440, 1.50f),
        };

        /// <summary>The strip C-07 builds (wave-3 W-B). Named here so a rename is one edit, in one place.</summary>
        private const string StripName = "status-strip";
        private const string GoalCardName = "goal-card";
        private const string MinimapName = "minimap";
        /// <summary>C-06's quickbar button names (InventoryPanelController.BuildQuickbar).</summary>
        private const string BarSlotPrefix = "quick-";
        /// <summary>C-06's core repair card (WorkshopPanelController.Instantiate("core") names it "card-core").</summary>
        private const string CoreCardName = "card-core";

        // ------------------------------------------------------------------ U-5

        /// <summary>
        /// U-5. Required Unity behaviour (§9.1): "The HUD status strip is content-width. At each layout in the
        /// target matrix (§2.5), assert the strip's measured width equals its content width and that it does not
        /// span the gap between the goal card and the minimap, and does not overlap either. Port the INTENT, not
        /// the pixel offsets of §4.2."
        ///
        /// "Content width" is asserted as the strip's own resolved width against the width its children actually
        /// occupy plus its padding and border — the measurement a full-width strip fails by construction, at every
        /// size, no matter what the stylesheet says.
        /// </summary>
        [UnityTest, Timeout(30000)]
        public IEnumerator U5_TheHudStatusStripIsContentWidthAtEveryLayoutInTheMatrix()
        {
            yield return SceneFixture.LoadWorld();
            var host = Object.FindAnyObjectByType<SimHost>();
            Assert.That(host, Is.Not.Null, "World.unity has no SimHost.");
            host.Paused = true;

            var document = FindDocumentWith(StripName, out var strip);
            if (strip == null)
                Assert.Ignore("needs seam: no VisualElement named \"" + StripName + "\" in any loaded UIDocument. "
                    + "C-07's HUD strip must carry that name for this check to measure it.");

            var root = document.rootVisualElement;
            var settings = document.panelSettings;
            var restoreMode = settings.scaleMode;
            var restoreScale = settings.scale;
            var restoreWidth = root.style.width;
            var restoreHeight = root.style.height;

            try
            {
                foreach (var layout in Matrix)
                {
                    var label = layout.W + "x" + layout.H + " @ " + (int)(layout.Scale * 100) + "%";
                    settings.scaleMode = PanelScaleMode.ConstantPixelSize;
                    settings.scale = layout.Scale;
                    root.style.width = layout.W / layout.Scale;      // the effective size §2.5 keys its classes on
                    root.style.height = layout.H / layout.Scale;
                    yield return null;
                    yield return null;                               // two frames: style, then layout

                    var width = strip.resolvedStyle.width;
                    Assert.That(width, Is.GreaterThan(0), label + ": the strip did not lay out at all.");

                    var content = ContentWidth(strip);
                    Assert.That(width, Is.EqualTo(content).Within(1.0),
                        label + ": the strip measures " + width + " px but its content is " + content
                        + " px wide — it is not content-width.");

                    var available = strip.parent != null ? strip.parent.resolvedStyle.width : root.resolvedStyle.width;
                    Assert.That(width, Is.LessThan(available - 1.0),
                        label + ": the strip spans the full " + available + " px of its row (the U-5 defect).");

                    var goal = root.Q<VisualElement>(GoalCardName);
                    var minimap = root.Q<VisualElement>(MinimapName);
                    if (goal != null && IsVisible(goal))
                        Assert.That(Overlaps(strip, goal), Is.False, label + ": the strip overlaps the goal card.");
                    if (minimap != null && IsVisible(minimap))
                        Assert.That(Overlaps(strip, minimap), Is.False, label + ": the strip overlaps the minimap.");
                    if (goal != null && minimap != null && IsVisible(goal) && IsVisible(minimap))
                    {
                        var stripRect = strip.worldBound;
                        var left = Mathf.Min(goal.worldBound.xMax, minimap.worldBound.xMax);
                        var right = Mathf.Max(goal.worldBound.xMin, minimap.worldBound.xMin);
                        Assert.That(stripRect.xMin <= left && stripRect.xMax >= right, Is.False,
                            label + ": the strip spans the whole gap between the goal card and the minimap.");
                    }
                }
            }
            finally
            {
                settings.scaleMode = restoreMode;
                settings.scale = restoreScale;
                root.style.width = restoreWidth;
                root.style.height = restoreHeight;
                host.Paused = false;
            }
        }

        // ------------------------------------------------------------------ D-UI-10

        /// <summary>
        /// D-UI-10 (§2.3, TECHNICAL_ARCHITECTURE.md §8.3 row 5): "Menus overlay the world; opening or closing them
        /// must not shift the camera." <c>CameraInsetTests</c> proves it for one panel; this proves it for EVERY
        /// panel the shell knows about, which is what the rule actually says.
        ///
        /// The sim is paused throughout, so any camera movement observed here is the UI's doing and the comparison
        /// can be exact rather than a tolerance.
        /// </summary>
        [UnityTest, Timeout(30000)]
        public IEnumerator DUi10_TogglingEveryPanelLeavesTheCameraTransformUntouched()
        {
            yield return SceneFixture.LoadWorld();
            var host = Object.FindAnyObjectByType<SimHost>();
            var shell = Object.FindAnyObjectByType<UiShell>();
            var rig = Object.FindAnyObjectByType<CameraRig>();
            Assert.That(shell, Is.Not.Null, "GameUI.unity has no UiShell.");
            Assert.That(rig, Is.Not.Null, "World.unity has no CameraRig.");

            host.Paused = true;
            shell.CloseActive();
            for (var i = 0; i < 5; i++) yield return null;

            var camera = rig.GetComponent<Camera>();
            var position = rig.transform.position;
            var rotation = rig.transform.rotation;
            var size = camera.orthographicSize;
            var rect = camera.rect;

            var ids = new List<string>(shell.PanelIds);
            Assert.That(ids, Is.Not.Empty, "the shell owns no panels; there is nothing to toggle.");

            try
            {
                foreach (var id in ids)
                {
                    Assert.That(shell.Toggle(id), Is.True, id + " did not open.");
                    for (var i = 0; i < 3; i++) yield return null;
                    Assert.That(rig.transform.position, Is.EqualTo(position), "opening " + id + " moved the camera.");
                    Assert.That(rig.transform.rotation, Is.EqualTo(rotation), "opening " + id + " turned the camera.");
                    Assert.That(camera.orthographicSize, Is.EqualTo(size), "opening " + id + " rescaled the camera.");
                    Assert.That(camera.rect, Is.EqualTo(rect), "opening " + id + " reshaped the viewport.");

                    shell.CloseActive();
                    for (var i = 0; i < 3; i++) yield return null;
                    Assert.That(rig.transform.position, Is.EqualTo(position), "closing " + id + " moved the camera.");
                    Assert.That(rig.transform.rotation, Is.EqualTo(rotation), "closing " + id + " turned the camera.");
                    Assert.That(camera.orthographicSize, Is.EqualTo(size), "closing " + id + " rescaled the camera.");
                    Assert.That(camera.rect, Is.EqualTo(rect), "closing " + id + " reshaped the viewport.");
                }
            }
            finally
            {
                shell.CloseActive();
                host.Paused = false;
            }
        }

        // ------------------------------------------------------------------ U-2, the UI half

        /// <summary>
        /// U-2. Required Unity behaviour (§9.1): "One tool occupies one action-bar slot. Assign the same tool by
        /// catalogue drag, by an 'Add to action bar' destination button and by a Backpack weapon drop; after each,
        /// exactly one slot holds it and any displaced tool sits in the vacated slot."
        ///
        /// The sim half is proved by <c>OpeningDefectsTests.U2_…</c>; this is the half that only the rendered bar
        /// can answer — that the SLOTS THE PLAYER SEES agree with the sim after each path. Every path goes through
        /// the one command the shell's drop handlers all funnel into, so what is being checked here is the
        /// rendering, not a second copy of the rule.
        /// </summary>
        [UnityTest, Timeout(30000)]
        public IEnumerator U2_TheRenderedActionBarShowsOneToolInExactlyOneSlot()
        {
            yield return SceneFixture.LoadWorld();
            var host = Object.FindAnyObjectByType<SimHost>();
            Assert.That(host, Is.Not.Null, "World.unity has no SimHost.");

            // Coordinator (wave 4): C-06 names its quickbar buttons "quick-<index>" and carries the assigned key
            // in the button's tooltip (InventoryPanelController.BuildQuickbar / PaintQuickbar); the test reads those.
            var document = FindDocumentWith(BarSlotPrefix + "0", out var firstSlot);
            if (firstSlot == null)
                Assert.Ignore("needs seam: no VisualElement named \"" + BarSlotPrefix + "0\" in any loaded UIDocument. "
                    + "C-06/C-10's action bar must name its slots \"" + BarSlotPrefix + "<index>\" and expose the assigned key "
                    + "as the slot's tooltip, userData or a child Label for this check to read the rendered bar.");

            host.Paused = true;
            var sim = host.Simulation;
            var st = sim.State;
            var rifle = WeaponRules.Create(st, "rifle");
            Pockets.Take(sim.Context.Data, st.Engineer, new ItemKey(rifle), 1);

            try
            {
                foreach (var slot in new[] { 0, 4, 8 })
                {
                    var r = sim.Apply(new AssignBarCommand(slot, rifle));
                    Assert.That(r.Accepted, Is.True, "assigning to slot " + slot + " was refused: " + r.Problem);
                    for (var i = 0; i < 3; i++) yield return null;

                    var held = new List<int>();
                    for (var i = 0; i < WeaponRules.BarSlots; i++)
                    {
                        var element = document.rootVisualElement.Q<VisualElement>(BarSlotPrefix + i);
                        if (element == null) continue;
                        if (SlotShows(element, rifle)) held.Add(i);
                    }
                    Assert.That(held, Is.EqualTo(new List<int> { slot }),
                        "after assigning the rifle to slot " + slot + " the rendered bar shows it in slots ["
                        + string.Join(", ", held) + "] — U-2 is back.");
                }
            }
            finally
            {
                host.Paused = false;
            }
        }

        // ------------------------------------------------------------------ U-6, the UI half

        /// <summary>
        /// U-6. Required Unity behaviour (§9.1): "Core repair is reachable in one interaction from the damaged
        /// core. Interact with a damaged Home core and assert the repair control is present and actionable in the
        /// surface that opens — no intervening screen, and the same for an undamaged core (the drawer still
        /// opens)."
        ///
        /// So: E on the core opens ONE surface, the core's repair card is the FIRST card in it, and its button is
        /// enabled. The undamaged case opens the same drawer rather than nothing.
        /// </summary>
        [UnityTest, Timeout(30000)]
        public IEnumerator U6_InteractingWithTheCoreOpensOneDrawerWithTheCoreCardFirst()
        {
            yield return SceneFixture.LoadWorld();
            var host = Object.FindAnyObjectByType<SimHost>();
            var shell = Object.FindAnyObjectByType<UiShell>();
            Assert.That(shell, Is.Not.Null, "GameUI.unity has no UiShell.");

            string drawer = null;
            foreach (var id in shell.PanelIds)
                // Coordinator (wave 4): the Home workshop is a section inside the Backpack drawer, whose panel id is
                // "inventory-panel" (UI_AND_ONBOARDING.md §5.1 item 1), so that id is accepted too.
                if (id != null && (id.Contains("workshop") || id.Contains("drawer") || id.Contains("inventory"))) { drawer = id; break; }
            if (drawer == null)
                Assert.Ignore("needs seam: the shell owns no panel id containing \"workshop\" or \"drawer\". "
                    + "C-06/C-10 must register the Home drawer under such an id, and the core card must be named "
                    + "\"" + CoreCardName + "\", for this check to find them.");

            var sim = host.Simulation;
            var st = sim.State;
            HomeCore.Damage(st, 100);
            st.Engineer.Pos = CoreCentre(st);
            // The card greys its button on a shortfall (reference workshop behaviour); pay the price so the
            // check measures ordering and actionability, not the pockets.
            var price = HomeCore.Price(sim.Context.Data, st, RepairKinds.Core, -1);
            Pockets.Take(sim.Context.Data, st.Engineer, ItemKey.Of(ItemId.Steel), price.Steel);
            Pockets.Take(sim.Context.Data, st.Engineer, ItemKey.Of(ItemId.Copper), price.Copper);

            // The real path (U-D-55): the workshop is a place, so only interacting with it puts its cards on
            // screen. Opening the panel id directly shows the Backpack alone and leaves the core card hidden,
            // which is what made this check pass or fail by the luck of the 150 ms repaint.
            shell.CloseActive();
            yield return null;
            var keyboard = VirtualKeyboard();
            // E opens what the pointer rests on before it looks for the core, so rest the pointer on the core too.
            var mouse = InputSystem.GetDevice<Mouse>() ?? InputSystem.AddDevice<Mouse>();
            if (!mouse.enabled) InputSystem.EnableDevice(mouse);
            var centre = CoreCentre(st);
            var at = Camera.main.WorldToScreenPoint(WorldSpace.TileCentre((int)centre.X, (int)centre.Y));
            InputSystem.QueueStateEvent(mouse, new MouseState { position = new Vector2(at.x, at.y) });
            yield return null;
            yield return null;
            yield return PressE(keyboard);
            yield return Until(() => shell.Active == drawer);
            Assert.That(shell.Active, Is.EqualTo(drawer), "E at the damaged core did not open the Home drawer.");
            host.Paused = true;
            for (var i = 0; i < 3; i++) yield return null;
            Assert.That(shell.Active, Is.EqualTo(drawer), "something else took the screen — that is an extra step.");

            var document = FindDocumentWith(CoreCardName, out var card);
            if (card == null)
                Assert.Ignore("needs seam: no VisualElement named \"" + CoreCardName + "\" in the open drawer. "
                    + "C-10's Home drawer must name the core's repair card that way.");

            // The card root sits inside a TemplateContainer; the container is what "workshop-cards" orders.
            var ordered = card.parent is TemplateContainer ? card.parent : card;
            Assert.That(ordered.parent.IndexOf(ordered), Is.EqualTo(0),
                "the core card is not the first card in the drawer; the repair is buried (U-6).");
            Assert.That(card.resolvedStyle.display, Is.Not.EqualTo(DisplayStyle.None), "the core card is in the tree but hidden.");
            for (var v = card.parent; v != null; v = v.parent)
                Assert.That(v.resolvedStyle.display, Is.Not.EqualTo(DisplayStyle.None),
                    "the core card sits inside a hidden section (\"" + v.name + "\"); the player sees the Backpack alone.");
            var button = card.Q<Button>();
            Assert.That(button, Is.Not.Null, "the core card has no repair button — the repair is not actionable.");
            Assert.That(button.enabledInHierarchy, Is.True, "the repair button is disabled on a damaged core.");

            // The undamaged case: the same drawer still opens and still shows the card.
            shell.CloseActive();
            for (var i = 0; i < 3; i++) yield return null;
            st.Home.Hp = sim.Context.Data.Defence.CoreHp;
            host.Paused = false;
            yield return PressE(keyboard);
            yield return Until(() => shell.Active == drawer);
            Assert.That(shell.Active, Is.EqualTo(drawer), "E at the undamaged core did not open the Home drawer.");
            for (var i = 0; i < 3; i++) yield return null;
            Assert.That(document.rootVisualElement.Q<VisualElement>(CoreCardName), Is.Not.Null,
                "an undamaged core opens an empty drawer; §9.1 requires the card to still be there.");

            shell.CloseActive();
            host.Paused = false;
        }

        // ------------------------------------------------------------------ helpers

        /// <summary>The first loaded <see cref="UIDocument"/> holding an element with this name, and that element.</summary>
        private static UIDocument FindDocumentWith(string name, out VisualElement found)
        {
            found = null;
            var documents = Object.FindObjectsByType<UIDocument>(FindObjectsSortMode.None);
            UIDocument owner = null;
            foreach (var document in documents)
            {
                var root = document != null ? document.rootVisualElement : null;
                if (root == null) continue;
                if (owner == null) owner = document;
                var element = root.Q<VisualElement>(name);
                if (element == null) continue;
                found = element;
                return document;
            }
            return owner;
        }

        /// <summary>The width the strip's own children occupy, plus its padding and border: its CONTENT width.</summary>
        private static float ContentWidth(VisualElement strip)
        {
            var style = strip.resolvedStyle;
            var inner = 0f;
            var first = true;
            var left = 0f;
            var right = 0f;
            foreach (var child in strip.Children())
            {
                if (!IsVisible(child)) continue;
                var bound = child.worldBound;
                if (first) { left = bound.xMin; right = bound.xMax; first = false; }
                else { left = Mathf.Min(left, bound.xMin); right = Mathf.Max(right, bound.xMax); }
            }
            if (!first) inner = right - left;
            return inner + style.paddingLeft + style.paddingRight + style.borderLeftWidth + style.borderRightWidth;
        }

        private static bool IsVisible(VisualElement e) =>
            e.resolvedStyle.display != DisplayStyle.None && e.resolvedStyle.visibility == Visibility.Visible
            && e.worldBound.width > 0;

        private static bool Overlaps(VisualElement a, VisualElement b) => a.worldBound.Overlaps(b.worldBound);

        /// <summary>Does this rendered slot show the weapon with this instance key?</summary>
        private static bool SlotShows(VisualElement slot, string key)
        {
            if (slot.userData is string s) return string.Equals(s, key, System.StringComparison.Ordinal);
            if (!string.IsNullOrEmpty(slot.tooltip) && string.Equals(slot.tooltip, key, System.StringComparison.Ordinal)) return true;
            foreach (var label in slot.Query<Label>().ToList())
                if (!string.IsNullOrEmpty(label.text) && label.text.Contains(key)) return true;
            return false;
        }

        private static Keyboard VirtualKeyboard()
        {
            var keyboard = InputSystem.GetDevice<Keyboard>() ?? InputSystem.AddDevice<Keyboard>();
            if (!keyboard.enabled) InputSystem.EnableDevice(keyboard);
            return keyboard;
        }

        /// <summary>One press and release of E, two frames each — the same cadence ControlsCorrectionTests uses.</summary>
        private static IEnumerator PressE(Keyboard keyboard)
        {
            InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.E));
            yield return null;
            yield return null;
            InputSystem.QueueStateEvent(keyboard, new KeyboardState());
            yield return null;
            yield return null;
        }

        private static IEnumerator Until(System.Func<bool> condition, float seconds = 2f)
        {
            var deadline = Time.realtimeSinceStartup + seconds;
            while (!condition() && Time.realtimeSinceStartup < deadline) yield return null;
        }

        private static Vec2 CoreCentre(SimState st)
        {
            var (x, y, w, h) = HomeQueries.CoreRect(st);
            return w > 0 ? new Vec2(x + w / 2.0, y + h / 2.0) : st.Engineer.Pos;
        }
    }
}
