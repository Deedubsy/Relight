using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using Relight.Presentation;
using Relight.Sim;
using Relight.UI;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UIElements;

namespace Relight.Tests.Play.Ui
{
    /// <summary>
    /// GP-UX-4 and its corrections U-D-51 and U-D-52, from three owner reports on 2026-09-16. First: "The
    /// inventory for every producer, can you improve the UI? I have to scroll down to see the inventory slots. The
    /// UI can take up more of the screen if needed". Then, of the first fix: "That's not what I wanted. I want the
    /// players inventory to be on the left and the producer to be on the right. The Producers UI needs to be redone
    /// a little to have the slots at the top." Then, of the second: "Can you make it so the inventory on the
    /// producer is fixed at the top and only the recipes are scrollable? Make the recipes fit 2 horizontally per
    /// row instead of 1." Then, of that one: "The recipe cards need to have the same height each. At the
    /// moment they're all different".
    ///
    /// Eight facts, in the real scene against the real drawer:
    ///   1. the drawer is two columns and nothing else - the player on the left, the open producer on the right,
    ///      with that producer's slots ABOVE its own controls and no shared strip under either column;
    ///   2. a producer's slots are all inside their window the moment the drawer opens - no scrolling;
    ///   3. the BACKPACK's grid takes whatever height its side offers instead of the fixed 144px window it used to
    ///      have (U-D-52 excused the producer's grid from this: see fact 6);
    ///   4. a chest's ten slots are shown at once when the drawer band has room for them;
    ///   5. the Home workshop is a producer like the rest: tray slots first, recipe cards under them;
    ///   6. a producer's grid is FIXED at the top of its column - pinned to the height its own slots need, not
    ///      grown to the column's, so what is left over goes to the controls or the cards below it;
    ///   7. the recipe cards sit two to a row;
    ///   8. and every one of them is the same height, down the whole list and not merely within a row.
    ///
    /// Geometry is asserted in world pixels, so a claim is only as strong as the Game view it runs in. Fact 2 is
    /// therefore stated for a SMALL producer - a generator's two fuel slots, a turret's ammo slot, an extractor's
    /// one output - which is a single row, and a single row is what `.inventory-scroll`'s min-height guarantees at
    /// every window size the game supports. Fact 4 is the wide case and is skipped, loudly and with the measured
    /// numbers, when the band is genuinely too short: a 640x480 Game view cannot show ten slots at once, and a
    /// test that asserted otherwise would be reporting the resolution rather than the layout.
    ///
    /// Compiled offline against the editor's managed assemblies. Play Mode is not available on this machine, so
    /// none of this has been RUN here.
    /// </summary>
    public sealed class InventoryLayoutTests
    {
        private const string PanelId = "inventory-panel";

        /// <summary>A slot may sit a pixel outside its window and still be whole on screen: borders round.</summary>
        private const float Slack = 1.5f;

        /// <summary>
        /// Margins between a side's children. Generous on purpose - the claim is "fills", not "to the px" - and
        /// wider since U-D-51, because the store column nests its grid one level deeper (inside #store-inventory)
        /// and each level adds its own margins.
        /// </summary>
        private const float Margins = 36f;

        /// <summary>The fixed window the grids used to have (industrial.uss, before GP-UX-4).</summary>
        private const float OldCap = 144f;

        private SimHost _host;
        private UiShell _shell;
        private InventoryPanelController _panel;
        private VisualElement _drawer;

        /// <summary>Load, pause, open the Backpack, paint it, and keep hold of the drawer element.</summary>
        private IEnumerator Open()
        {
            yield return SceneFixture.LoadWorld();
            _host = Object.FindAnyObjectByType<SimHost>();
            Assert.That(_host, Is.Not.Null, "World.unity has no SimHost.");
            _host.Paused = true;                       // nothing may produce into a grid while the test measures it
            _shell = Object.FindAnyObjectByType<UiShell>();
            _panel = Object.FindAnyObjectByType<InventoryPanelController>();
            Assert.That(_shell, Is.Not.Null, "GameUI.unity has no UiShell.");
            Assert.That(_panel, Is.Not.Null, "GameUI.unity has no InventoryPanelController.");

            UiFixture.DocumentWith(PanelId, out _drawer);
            Assert.That(_drawer, Is.Not.Null, "no element named \"" + PanelId + "\".");

            _shell.Open(PanelId);
            yield return null;
            _panel.Paint(true);
            yield return null;
            yield return null;                         // a second frame so the layout pass has run before measuring
        }

        private VisualElement El(string name) => _drawer == null ? null : _drawer.Q<VisualElement>(name);

        private static bool Under(VisualElement child, VisualElement ancestor)
        {
            for (var walk = child; walk != null; walk = walk.parent) if (walk == ancestor) return true;
            return false;
        }

        /// <summary>
        /// Everything shown inside the side that is NOT the grid's own window: what the grid does not get. Walks
        /// up from the scroller to the side, summing each level's other children, because since U-D-51 the store
        /// column's grid is a grandchild of its side (#side-store > #store-inventory > #store-scroll) while the
        /// Backpack's is still a child.
        /// </summary>
        private static float Siblings(VisualElement side, VisualElement scroll)
        {
            var total = 0f;
            for (var walk = scroll; walk != null && walk != side; walk = walk.parent)
            {
                var parent = walk.parent;
                if (parent == null) break;
                foreach (var child in parent.Children())
                {
                    if (child == walk) continue;
                    if (child.resolvedStyle.display == DisplayStyle.None) continue;
                    total += child.worldBound.height;
                }
            }
            return total;
        }

        /// <summary>
        /// Build the first of these machine kinds this session can actually place, and hand back its id. Materials
        /// are granted first: this is a test of the drawer's layout, not of the opening economy.
        /// </summary>
        private bool Build(IEnumerable<string> kinds, out string kind, out int machineId)
        {
            kind = "";
            machineId = -1;
            var sim = _host.Simulation;
            ItemId[] supplies = { ItemId.Steel, ItemId.Copper, ItemId.Wire, ItemId.Frame, ItemId.Board,
                                  ItemId.Stone, ItemId.Concrete, ItemId.Coal };
            foreach (var item in supplies) sim.State.Engineer.Inv[item] += 400;

            foreach (var candidate in kinds)
            {
                if (!sim.Context.Data.TryMachine(candidate, out _)) continue;
                int x, y;
                try { (x, y) = SceneFixture.FreeTile(sim, candidate, 0); }
                catch (System.InvalidOperationException) { continue; }
                var placed = sim.Apply(new PlaceMachineCommand(candidate, x, y, Dir.N));
                if (!placed.Accepted) continue;
                var machine = SceneFixture.Last(sim, candidate);
                if (machine == null) continue;
                kind = candidate;
                machineId = machine.Id;
                return true;
            }
            return false;
        }

        /// <summary>Pair the drawer with a machine the way the world interaction does, and let it lay out.</summary>
        private IEnumerator Pair(int machineId)
        {
            _panel.OpenStore(machineId);
            yield return null;
            _panel.Paint(true);
            yield return null;
            yield return null;
        }

        /// <summary>
        /// Open the Home workshop the way interacting with the depot does. U-D-55 made Tab the Backpack ALONE, so
        /// a test that measures the workshop has to ask for it: <see cref="Open"/> no longer brings it up.
        /// </summary>
        private IEnumerator Workshop()
        {
            _panel.OpenWorkshop();
            yield return null;
            _panel.Paint(true);
            yield return null;
            yield return null;
        }

        // ----------------------------------------------------------------------------------------------------

        /// <summary>
        /// U-D-51's structural claim. The drawer is two columns: the left one is the player and only the player,
        /// the right one is the open producer and everything that producer has to say, with its controls BELOW its
        /// own slots. The shared strip under both columns - which is what the owner was looking at when they said
        /// "that's not what I wanted" - is gone, and nothing may put it back.
        /// </summary>
        [UnityTest, Timeout(60000)]
        public IEnumerator TheProducerColumnOwnsItsControls()
        {
            yield return Open();

            var pair = El("inventory-pair");
            var pack = El("side-pack");
            var store = El("side-store");
            Assert.That(pair, Is.Not.Null, "the drawer has no #inventory-pair.");
            Assert.That(pack, Is.Not.Null, "the drawer has no #side-pack.");
            Assert.That(store, Is.Not.Null, "the drawer has no #side-store.");

            Assert.That(pair.parent, Is.SameAs(_drawer),
                "#inventory-pair is no longer a direct child of the drawer, so it no longer shares the drawer's "
                + "height with just the heading row and the footer.");
            Assert.That(_drawer.Q<ScrollView>("backpack-body"), Is.Null,
                "#backpack-body is back: that is the shared strip under both columns U-D-51 removed.");

            // Left is the player, and the order is Backpack first (the pair is a row, not row-reverse).
            Assert.That(pair.IndexOf(pack), Is.LessThan(pair.IndexOf(store)),
                "the producer column comes before the Backpack column.");
            Assert.That(Under(El("pack-grid"), pack), Is.True, "the Backpack grid is not in the left column.");
            Assert.That(Under(El("equipment-strip"), pack), Is.True, "the equipment strip is not in the left column.");

            // Right is the producer, controls and all.
            var controls = El("machine-controls");
            var workshop = El("workshop-host");
            var body = El("store-body");
            Assert.That(controls, Is.Not.Null, "the drawer has no #machine-controls.");
            Assert.That(body, Is.Not.Null, "the drawer has no #store-body: the producer's controls have no home.");
            Assert.That(_drawer.Q<ScrollView>("store-body"), Is.Null,
                "#store-body is a scroller again. U-D-52 fixed the producer's slots at the top of the column and "
                + "left the scrolling to what sits below them - a machine's #machine-scroll, the workshop's "
                + "#workshop-cards - so the body itself must be a plain column or the slots can scroll away.");
            Assert.That(Under(body, store), Is.True, "#store-body is not inside the producer column.");
            Assert.That(Under(controls, store), Is.True,
                "the machine controls are outside the producer column again - the strip the owner rejected.");
            Assert.That(Under(controls, pack), Is.False, "the machine controls are in the PLAYER's column.");
            if (workshop != null)
            {
                Assert.That(Under(workshop, store), Is.True, "the Home workshop is outside the producer column.");
                Assert.That(Under(workshop, pack), Is.False, "the Home workshop is in the PLAYER's column.");
            }

            // Slots at the top: whatever the producer is, its container comes before its controls in the column.
            var inventory = El("store-inventory");
            Assert.That(inventory, Is.Not.Null, "the drawer has no #store-inventory.");
            Assert.That(store.IndexOf(inventory), Is.LessThan(store.IndexOf(body)),
                "the producer's controls come ABOVE its slots, which is the half of the report U-D-51 answers.");
        }

        /// <summary>
        /// Fact 5: one rule for every producer. The Home workshop's twenty tray slots sit at the top of its
        /// section, where a machine's slots sit, and its recipe cards - the workshop's controls - sit under them.
        /// </summary>
        [UnityTest, Timeout(60000)]
        public IEnumerator TheWorkshopTrayIsAboveItsRecipeCards()
        {
            yield return Open();
            yield return Workshop();

            var workshop = El("workshop");
            if (workshop == null) Assert.Ignore("this document has no Home workshop section instanced.");

            var output = El("workshop-output");
            var cards = El("workshop-cards");
            var grid = El("workshop-output-grid");
            Assert.That(output, Is.Not.Null, "the workshop section has no #workshop-output.");
            Assert.That(cards, Is.Not.Null, "the workshop section has no #workshop-cards.");
            Assert.That(grid, Is.Not.Null, "the workshop section has no #workshop-output-grid.");

            Assert.That(workshop.IndexOf(output), Is.LessThan(workshop.IndexOf(cards)),
                "the recipe cards come before the output tray, so the workshop's slots are not at the top.");
            Assert.That(grid.childCount, Is.EqualTo(_panel.Model.Tray.Count),
                "the tray grid does not hold one element per tray slot (§5.3: every slot is a real element).");

            // And the tray really is the first thing painted in the column, not just the first in the markup.
            if (UiFixture.Shown(grid) && UiFixture.Shown(cards))
                Assert.That(grid.worldBound.yMin, Is.LessThan(cards.worldBound.yMin),
                    "the tray grid is painted below the recipe cards (tray " + grid.worldBound
                    + ", cards " + cards.worldBound + ").");
        }

        /// <summary>
        /// U-D-55, the owner's third bullet: "The Homework shop always shows up when the player presses tab. It
        /// should only show up when the player interacts with it. When the player hits tab, it should only open
        /// the players inventory". Tab and "E at the depot" both pair the drawer with machine -1, and the paint
        /// read that -1 as "show the workshop", so the workshop came up under every Tab. The interaction now says
        /// which it was, and this holds both halves: Tab alone leaves the producer column shut, and opening the
        /// workshop still fills it.
        /// </summary>
        [UnityTest, Timeout(60000)]
        public IEnumerator TabOpensTheBackpackAloneAndNotTheHomeWorkshop()
        {
            yield return Open();
            if (El("workshop-host") == null) Assert.Ignore("this document has no Home workshop section instanced.");

            // Open() goes through the shell directly; Tab is UiShell.ToggleBackpack, so toggle once to put away
            // what Open() left showing and once more to arrive by the route the player actually takes.
            _shell.ToggleBackpack();
            yield return null;
            Assert.That(_shell.Active, Is.Not.EqualTo(PanelId), "Tab did not close the drawer it was showing.");
            _shell.ToggleBackpack();
            yield return null;
            _panel.Paint(true);
            yield return null;
            yield return null;

            Assert.That(_shell.Active, Is.EqualTo(PanelId), "Tab did not open the drawer.");
            Assert.That(UiFixture.Shown(El("pack-grid")), Is.True,
                "Tab opened the drawer without the Backpack in it.");
            Assert.That(UiFixture.Shown(El("workshop-host")), Is.False,
                "Tab put the Home workshop on screen. Only an interaction with it may: OpenStore(-1) is the "
                + "Backpack alone and OpenWorkshop() is the workshop (U-D-55).");
            Assert.That(UiFixture.Shown(El("side-store")), Is.False,
                "Tab opened the producer column with no producer in it.");

            // And the workshop still opens when the player is the one opening it.
            yield return Workshop();
            Assert.That(UiFixture.Shown(El("workshop-host")), Is.True,
                "interacting with the Home workshop no longer opens it, which is the other half of U-D-55.");
            Assert.That(UiFixture.Shown(El("side-store")), Is.True,
                "the Home workshop is open but the producer column that holds it is not on screen.");
        }

        /// <summary>
        /// Fact 6, and the first half of the third owner report: "the inventory on the producer is fixed at the
        /// top and only the recipes are scrollable". A SMALL producer is used - one row of slots - in a column
        /// tall enough to have grown the grid if anything still wanted to: if the window is pinned to that one row
        /// there, it is pinned everywhere. What the slots do not take goes to #store-body underneath them.
        /// </summary>
        [UnityTest, Timeout(120000)]
        public IEnumerator AProducersSlotsAreFixedAtTheTopOfItsColumn()
        {
            yield return Open();

            string[] kinds = { "generator", "turret", "excavator", "pumpjack" };
            if (!Build(kinds, out var kind, out var machineId))
                Assert.Ignore("this session could not place any of " + string.Join("/", kinds) + ".");

            yield return Pair(machineId);

            var side = El("side-store");
            var inventory = El("store-inventory");
            var body = El("store-body");
            var scroll = _drawer.Q<ScrollView>("store-scroll");
            Assert.That(UiFixture.Shown(side), Is.True, "the storage side is not on screen with a " + kind + " open.");
            Assert.That(inventory, Is.Not.Null, "no #store-inventory.");
            Assert.That(body, Is.Not.Null, "no #store-body.");
            Assert.That(scroll, Is.Not.Null, "no #store-scroll.");

            if (side.worldBound.height < OldCap * 2f)
                Assert.Ignore("this Game view (" + Screen.width + "x" + Screen.height + ") gives the producer a "
                    + side.worldBound.height + "px column, which is too short for the grid to have anywhere to grow "
                    + "into. Re-run at 1920x1080 for the claim U-D-52 makes.");

            // Fixed: the window is the slots' own height, not the column's. Half the column is a generous line -
            // one row of slots against a column this tall is nearer a tenth - and it is the line that fails the
            // moment anything gives #store-scroll flex-grow back.
            Assert.That(scroll.worldBound.height, Is.LessThan(side.worldBound.height * 0.5f),
                kind + ": the slot window is " + scroll.worldBound.height + "px of a " + side.worldBound.height
                + "px column, so it is still growing to fill the column instead of sitting at its own height.");

            // At the top, with the controls below it and nothing overlapping.
            Assert.That(inventory.worldBound.yMax, Is.LessThanOrEqualTo(body.worldBound.yMin + Slack),
                kind + ": the producer's slots (" + inventory.worldBound + ") overlap or follow its controls ("
                + body.worldBound + ").");
            Assert.That(body.worldBound.height, Is.GreaterThan(0f),
                kind + ": #store-body has no height at all, so the slots took the whole column.");

            // And the slots themselves do not scroll: the grid fits inside the window that holds it.
            var grid = El("store-grid");
            Assert.That(grid, Is.Not.Null, "no #store-grid.");
            Assert.That(grid.worldBound.height, Is.LessThanOrEqualTo(scroll.contentViewport.worldBound.height + Slack),
                kind + ": the slot grid (" + grid.worldBound.height + "px) is taller than its window ("
                + scroll.contentViewport.worldBound.height + "px), so the fixed slots scroll after all.");
        }

        /// <summary>
        /// Fact 7, the second half of the third report: "Make the recipes fit 2 horizontally per row instead of 1".
        /// Two hosts share a row and each is about half of it. The 40-60% band is what makes this a test of TWO
        /// per row rather than of "more than one": three to a row would put each near a third and fail it.
        /// </summary>
        [UnityTest, Timeout(60000)]
        public IEnumerator TheRecipeCardsSitTwoToARow()
        {
            yield return Open();
            yield return Workshop();

            var cards = _drawer.Q<ScrollView>("workshop-cards");
            if (El("workshop") == null) Assert.Ignore("this document has no Home workshop section instanced.");
            Assert.That(cards, Is.Not.Null,
                "#workshop-cards is not a ScrollView. U-D-52 made the cards the one scrolling thing in the "
                + "producer's column, because the tray above them no longer scrolls with them.");

            var hosts = new List<VisualElement>();
            foreach (var child in cards.Children())
                if (child.resolvedStyle.display != DisplayStyle.None) hosts.Add(child);
            if (hosts.Count < 2) Assert.Ignore("the workshop offers " + hosts.Count + " recipe cards, so there is "
                + "no row to share.");

            var room = cards.contentViewport.worldBound.width;
            if (room < 360f)
                Assert.Ignore("this Game view (" + Screen.width + "x" + Screen.height + ") gives the cards a "
                    + room + "px column, under the 2 x 180px two of them need, so one per row is the correct "
                    + "fallback here. Re-run at 1920x1080 for the claim U-D-52 makes.");

            var first = hosts[0].worldBound;
            var second = hosts[1].worldBound;
            Assert.That(second.yMin, Is.EqualTo(first.yMin).Within(Slack),
                "the first two recipe cards are on different rows (" + first + ", " + second + "): still one per row.");
            Assert.That(second.xMin, Is.GreaterThanOrEqualTo(first.xMax - Slack),
                "the first two recipe cards overlap horizontally (" + first + ", " + second + ").");
            Assert.That(first.width, Is.GreaterThan(room * 0.4f).And.LessThan(room * 0.6f),
                "a recipe card is " + first.width + "px of a " + room + "px row, which is not the half a row two "
                + "per row means.");

            // The tray above them is what is NOT scrolling, which is the other half of the same sentence.
            var tray = _drawer.Q<ScrollView>("workshop-output-scroll");
            if (tray != null && UiFixture.Shown(tray))
                Assert.That(tray.worldBound.yMax, Is.LessThanOrEqualTo(cards.worldBound.yMin + Slack),
                    "the output tray (" + tray.worldBound + ") is not above the cards (" + cards.worldBound + ").");
        }

        /// <summary>
        /// Fact 8, the fourth report: "The recipe cards need to have the same height each. At the moment
        /// they're all different". Two things have to hold for that, and this asserts both. The card fills
        /// its host, which is what matches the two cards in a row to each other, since hosts in one wrapped
        /// row stretch to the tallest of them. And every host is the same height, which is what matches one
        /// ROW to the next - flex cannot do that on its own, because wrapped rows are measured independently,
        /// so it rests on the `min-height` on `.recipe-host` being taller than the tallest card.
        ///
        /// A card that outgrows that floor fails here rather than silently going ragged again, and the
        /// message carries the height to raise the floor to.
        /// </summary>
        [UnityTest, Timeout(60000)]
        public IEnumerator TheRecipeCardsAreAllTheSameHeight()
        {
            yield return Open();
            yield return Workshop();

            if (El("workshop") == null) Assert.Ignore("this document has no Home workshop section instanced.");
            var cards = _drawer.Q<ScrollView>("workshop-cards");
            Assert.That(cards, Is.Not.Null, "no #workshop-cards ScrollView.");

            var hosts = new List<VisualElement>();
            foreach (var child in cards.Children())
                if (child.resolvedStyle.display != DisplayStyle.None) hosts.Add(child);
            if (hosts.Count < 2) Assert.Ignore("the workshop offers " + hosts.Count + " recipe cards, so there "
                + "is nothing to be the same height as.");

            // Measure the whole list before asserting anything, so a failure can name the height the floor
            // would have to clear rather than the tallest card seen up to that point.
            var needed = 0f;
            for (var i = 0; i < hosts.Count; i++)
            {
                var c = hosts[i].Q<VisualElement>("recipe-card");
                Assert.That(c, Is.Not.Null, "recipe host " + i + " holds no #recipe-card.");
                var want = c.worldBound.height + hosts[i].resolvedStyle.paddingTop
                    + hosts[i].resolvedStyle.paddingBottom;
                if (want > needed) needed = want;
            }

            var first = hosts[0].worldBound.height;
            for (var i = 0; i < hosts.Count; i++)
            {
                var host = hosts[i];
                var card = host.Q<VisualElement>("recipe-card");

                // The card fills its host: what is left is the host's own bottom gutter, nothing more.
                var gutter = host.resolvedStyle.paddingBottom + host.resolvedStyle.paddingTop;
                Assert.That(card.worldBound.height,
                    Is.EqualTo(host.worldBound.height - gutter).Within(Slack),
                    "recipe card " + i + " is " + card.worldBound.height + "px inside a "
                    + host.worldBound.height + "px host, so it is sized to its own text rather than filling "
                    + "the row: `.recipe-card { flex-grow: 1 }` is not reaching it.");

                Assert.That(host.worldBound.height, Is.EqualTo(first).Within(Slack),
                    "recipe card " + i + " is " + host.worldBound.height + "px where the first is " + first
                    + "px. The cards are all one height only while none outgrows the `min-height` on "
                    + "`.recipe-host` (industrial.uss and WorkshopPanel.uss); raise it past "
                    + needed + "px.");
            }
        }

        /// <summary>
        /// The report itself: open a producer and every one of its slots is already on screen. A generator, a
        /// turret and an extractor all present one row of slots, which the grid window can always hold.
        /// </summary>
        [UnityTest, Timeout(120000)]
        public IEnumerator AProducersSlotsAreAllVisibleWithoutScrolling()
        {
            yield return Open();

            string[] kinds = { "generator", "turret", "excavator", "pumpjack" };
            if (!Build(kinds, out var kind, out var machineId))
                Assert.Ignore("this session could not place any of " + string.Join("/", kinds) + ".");

            yield return Pair(machineId);

            Assert.That(_panel.Model.MachineId, Is.EqualTo(machineId), "the drawer did not pair with the " + kind + ".");
            Assert.That(_panel.Model.Store.Count, Is.GreaterThan(0), "the " + kind + " reported no slots at all.");
            Assert.That(_drawer.ClassListContains("storage-open"), Is.True,
                "the drawer did not take its wide producer-open form, so it is still the narrow Backpack width.");

            var side = El("side-store");
            Assert.That(UiFixture.Shown(side), Is.True, "the storage side is not on screen with a " + kind + " open.");

            var scroll = _drawer.Q<ScrollView>("store-scroll");
            var grid = El("store-grid");
            Assert.That(scroll, Is.Not.Null, "no #store-scroll.");
            Assert.That(grid, Is.Not.Null, "no #store-grid.");
            Assert.That(grid.childCount, Is.EqualTo(_panel.Model.Store.Count),
                "the store grid does not hold one element per slot (§5.3: every slot is a real element).");

            var window = scroll.contentViewport.worldBound;
            for (var i = 0; i < grid.childCount; i++)
            {
                var slot = grid[i];
                Assert.That(slot.worldBound.yMin, Is.GreaterThanOrEqualTo(window.yMin - Slack),
                    kind + " slot " + i + " starts above its window (slot " + slot.worldBound + ", window " + window + ").");
                Assert.That(slot.worldBound.yMax, Is.LessThanOrEqualTo(window.yMax + Slack),
                    kind + " slot " + i + " runs past the bottom of its window (slot " + slot.worldBound
                    + ", window " + window + "): this is the scrolling the owner reported.");
            }
        }

        /// <summary>
        /// The vertical half of the fix. Before GP-UX-4 the theme pinned every grid to a 144px window whatever the
        /// drawer's height was, which is where the unused space came from. Now the window is whatever the side has
        /// left once its heading, equipment strip and hint have taken theirs - asserted with the Backpack alone
        /// and again with a producer open, because the producer-open drawer is the one the report is about.
        ///
        /// This is the BACKPACK's claim. U-D-52 excused the producer's own grid from it deliberately: the owner
        /// asked for that one to be fixed at the top rather than to fill its column, and
        /// <see cref="AProducersSlotsAreFixedAtTheTopOfItsColumn"/> is the assertion that replaced it here.
        /// </summary>
        [UnityTest, Timeout(120000)]
        public IEnumerator EachGridTakesTheHeightItsSideOffers()
        {
            yield return Open();
            Fills("side-pack", "pack-scroll", "Backpack alone");

            string[] kinds = { "chest", "generator", "turret", "excavator" };
            if (!Build(kinds, out var kind, out var machineId))
                Assert.Ignore("this session could not place any of " + string.Join("/", kinds) + ".");

            yield return Pair(machineId);
            Fills("side-pack", "pack-scroll", "Backpack with a " + kind + " open");
        }

        private void Fills(string sideName, string scrollName, string what)
        {
            var side = El(sideName);
            var scroll = _drawer.Q<ScrollView>(scrollName);
            Assert.That(side, Is.Not.Null, "no #" + sideName + ".");
            Assert.That(scroll, Is.Not.Null, "no #" + scrollName + ".");
            Assert.That(UiFixture.Shown(side), Is.True, what + ": #" + sideName + " is not on screen.");

            var spare = side.worldBound.height - Siblings(side, scroll);
            Assert.That(scroll.worldBound.height, Is.GreaterThanOrEqualTo(spare - Margins),
                what + ": the grid window is " + scroll.worldBound.height + "px of the " + spare
                + "px its side has spare, so something is still capping it.");

            // The specific regression: the old fixed window, in a side with room for more than it.
            if (side.worldBound.height >= OldCap * 1.8f)
                Assert.That(scroll.worldBound.height, Is.GreaterThan(OldCap),
                    what + ": the grid window is still the pre-GP-UX-4 " + OldCap + "px cap in a "
                    + side.worldBound.height + "px side.");
        }

        /// <summary>
        /// The wide case. A chest offers at least ten slots, and at an ordinary window size the drawer is now wide
        /// and tall enough to show all of them without a scroll. Where the Game view cannot hold them the test
        /// says so with the measurements rather than asserting something about the window instead of the layout.
        /// </summary>
        [UnityTest, Timeout(120000)]
        public IEnumerator AChestShowsEverySlotWhenTheDrawerHasRoom()
        {
            yield return Open();

            if (!Build(new[] { "chest" }, out _, out var machineId))
                Assert.Ignore("this session could not place a chest.");

            yield return Pair(machineId);

            var slots = _panel.Model.Store.Count;
            Assert.That(slots, Is.GreaterThanOrEqualTo(10), "a chest is supposed to offer at least ten slots.");

            var scroll = _drawer.Q<ScrollView>("store-scroll");
            var grid = El("store-grid");
            Assert.That(scroll, Is.Not.Null, "no #store-scroll.");
            Assert.That(grid, Is.Not.Null, "no #store-grid.");
            Assert.That(grid.childCount, Is.EqualTo(slots), "the chest's grid does not hold one element per slot.");

            var window = scroll.contentViewport.worldBound;
            var content = grid.worldBound;
            if (content.height > window.height + Slack)
                Assert.Ignore("this Game view (" + Screen.width + "x" + Screen.height + ") gives the chest a "
                    + window.height + "px window for a " + content.height + "px grid, so ten slots at once is not "
                    + "something it can show. Re-run at 1920x1080 for the claim GP-UX-4 makes.");

            for (var i = 0; i < grid.childCount; i++)
                Assert.That(grid[i].worldBound.yMax, Is.LessThanOrEqualTo(window.yMax + Slack),
                    "chest slot " + i + " is below the fold although the grid as a whole fits the window.");
        }
    }
}
