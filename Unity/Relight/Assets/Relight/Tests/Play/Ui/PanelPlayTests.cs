using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using Relight.Presentation;
using Relight.UI;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UIElements;

namespace Relight.Tests.Play.Ui
{
    /// <summary>
    /// The correction pass's four screen facts (W-B): the Backpack is reachable, the build menu exists and takes a
    /// machine in hand, the action bar survives an open drawer, and the objective is stated ONCE.
    ///
    /// Every one of them is a defect the owner's playtest reported, so each test names the defect it guards.
    ///
    /// Written WITHOUT Unity on this machine: never compiled, never run here. Where the check needs a scene
    /// component the coordinator has still to add, the test says so through <see cref="Assert.Ignore(string)"/>
    /// with the exact component and field it needs, rather than passing on an absence.
    /// </summary>
    public sealed class PanelPlayTests
    {
        private const string Inventory = "inventory-panel";
        private const string Build = "build-panel";
        private const string ActionBar = "action-bar";
        private const string Strip = "status-strip";

        /// <summary>
        /// Playtest defect: "inventory not reachable". W-A owns the Tab binding; this is the other half — the
        /// panel the binding targets must open, close and be visible through the shell's own id.
        /// </summary>
        [UnityTest, Timeout(30000)]
        public IEnumerator TheTabTargetPanelOpensAndClosesThroughTheShell()
        {
            yield return SceneFixture.LoadWorld();
            var shell = Object.FindAnyObjectByType<UiShell>();
            Assert.That(shell, Is.Not.Null, "GameUI.unity has no UiShell.");

            UiFixture.DocumentWith(Inventory, out var panel);
            Assert.That(panel, Is.Not.Null,
                "no element named \"" + Inventory + "\" in any loaded document: contract C1's Tab target is missing.");

            if (!string.IsNullOrEmpty(shell.Active)) shell.CloseActive();
            yield return null;
            Assert.That(UiFixture.Shown(panel), Is.False, "the Backpack was already on screen before Tab.");

            var opened = shell.Toggle(Inventory);
            yield return null;
            yield return null;
            Assert.That(opened, Is.True, "UiShell.Toggle(\"" + Inventory + "\") refused to open the panel.");
            Assert.That(shell.Active, Is.EqualTo(Inventory), "the shell opened a different panel.");
            Assert.That(UiFixture.Shown(panel), Is.True, "the Backpack is the active panel but is not on screen.");

            shell.Toggle(Inventory);
            yield return null;
            Assert.That(UiFixture.Shown(panel), Is.False, "toggling again did not close the Backpack.");
        }

        /// <summary>
        /// Playtest defect: "no build menu". The menu must exist, list real catalogue rows, and clicking a card
        /// must put that machine in hand through <see cref="WorldInput"/> (contract C3) and close the drawer while
        /// the action bar stays.
        /// </summary>
        [UnityTest, Timeout(30000)]
        public IEnumerator TheBuildMenuListsCardsAndACardClickTakesTheMachineInHand()
        {
            yield return SceneFixture.LoadWorld();
            var shell = Object.FindAnyObjectByType<UiShell>();
            Assert.That(shell, Is.Not.Null, "GameUI.unity has no UiShell.");

            var build = Object.FindAnyObjectByType<BuildPanelController>();
            if (build == null)
                Assert.Ignore("needs scene: GameUI.unity has no BuildPanelController. Add it to the GameUI object "
                    + "with document = the GameUI UIDocument, host = SimHost, shell = UiShell and "
                    + "buildCard = UI/Build/BuildCard.uxml.");

            var input = Object.FindAnyObjectByType<WorldInput>();
            Assert.That(input, Is.Not.Null, "World.unity has no WorldInput: contract C3 has nowhere to land.");
            input.ClearHand();

            var document = UiFixture.DocumentWith(Build, out var panel);
            Assert.That(panel, Is.Not.Null, "no element named \"" + Build + "\": GameUI.uxml must instance BuildPanel.uxml.");
            var root = document.rootVisualElement;

            Assert.That(shell.Open(Build), Is.True, "UiShell.Open(\"" + Build + "\") refused.");
            yield return null;
            build.Paint(true);
            yield return null;
            Assert.That(UiFixture.Shown(panel), Is.True, "the build menu is active but not on screen.");

            Assert.That(build.VisibleCardCount, Is.GreaterThanOrEqualTo(1),
                "the build menu shows no cards at all: the catalogue never reached it.");

            var grid = root.Q<VisualElement>("build-grid");
            Assert.That(grid, Is.Not.Null, "BuildPanel.uxml has no #build-grid.");
            var cards = grid.Query<Button>(className: "build-card").ToList();
            Assert.That(cards.Count, Is.GreaterThanOrEqualTo(1), "#build-grid holds no .build-card buttons.");

            // Only the open tab's cards are on screen; the rest are hidden by class, so click one the player
            // could actually have clicked.
            Button card = null;
            foreach (var candidate in cards)
                if (UiFixture.Shown(candidate)) { card = candidate; break; }
            Assert.That(card, Is.Not.Null, "every build card is hidden: the open tab shows nothing.");

            var bar = root.Q<VisualElement>(ActionBar);
            UiFixture.Submit(card);
            yield return null;
            yield return null;

            Assert.That(string.IsNullOrEmpty(input.Tool), Is.False,
                "clicking a build card left WorldInput.Tool empty: the card did not take the machine in hand.");
            Assert.That(input.PlacementActive, Is.True, "the hand is holding a tool but placement is not active.");
            Assert.That(shell.Active, Is.Not.EqualTo(Build), "the build menu stayed open over the ghost.");
            if (bar != null)
                Assert.That(UiFixture.Shown(bar), Is.True, "the action bar vanished when the build menu closed.");

            input.ClearHand();
            yield return null;
        }

        /// <summary>
        /// U-D-55, the owner's first bullet: "The build menu UI items aren't all the same height. They should be."
        /// Two claims, the same pair U-D-52 rule (d) makes about the recipe cards, because it is the same fix. The
        /// card fills its host, which is what matches the cards in one wrapped row to each other; and every host
        /// is one height, which is what matches one ROW to the next — flex cannot do the second on its own, since
        /// wrapped rows are measured independently, so it rests on the `min-height` on `.build-host` clearing the
        /// tallest card. The cost line is what grows: a machine with a longer recipe is exactly the change that
        /// breaks this, and it fails here with the height to raise the floor to rather than going ragged again.
        ///
        /// It also guards the trap that arrives WITH a host: a host claims its floor whether or not the card
        /// inside it is on the open tab, so a tab switch has to hide the host and not just the card.
        /// </summary>
        [UnityTest, Timeout(30000)]
        public IEnumerator TheBuildCardsAreAllTheSameHeight()
        {
            const float Slack = 1.5f;

            yield return SceneFixture.LoadWorld();
            var shell = Object.FindAnyObjectByType<UiShell>();
            Assert.That(shell, Is.Not.Null, "GameUI.unity has no UiShell.");
            var build = Object.FindAnyObjectByType<BuildPanelController>();
            if (build == null) Assert.Ignore("needs scene: GameUI.unity has no BuildPanelController.");

            var document = UiFixture.DocumentWith(Build, out var panel);
            Assert.That(panel, Is.Not.Null, "no element named \"" + Build + "\".");
            Assert.That(shell.Open(Build), Is.True, "UiShell.Open(\"" + Build + "\") refused.");
            yield return null;
            build.Paint(true);
            yield return null;
            yield return null;                     // a second frame, so the layout pass has run before measuring

            var grid = document.rootVisualElement.Q<VisualElement>("build-grid");
            Assert.That(grid, Is.Not.Null, "BuildPanel.uxml has no #build-grid.");

            var hosts = new List<VisualElement>();
            foreach (var child in grid.Children())
                if (UiFixture.Shown(child)) hosts.Add(child);
            if (hosts.Count < 2)
                Assert.Ignore("the " + build.Tab + " tab shows " + hosts.Count + " cards, so there is nothing to "
                    + "be the same height as.");

            // Measure every card before asserting, so a failure can name the height the floor would have to
            // clear rather than the tallest card seen up to that point.
            var needed = 0f;
            for (var i = 0; i < hosts.Count; i++)
            {
                var c = hosts[i].Q<Button>(className: "build-card");   // named card-<kind> at run time
                Assert.That(c, Is.Not.Null,
                    "a shown .build-host holds no visible .build-card. A host claims its `min-height` whether or "
                    + "not the card inside it is on the open tab, so hiding the card alone leaves the grid pocked "
                    + "with blank squares - the host is what a tab switch must hide (U-D-55).");
                var want = c.worldBound.height + hosts[i].resolvedStyle.paddingTop
                    + hosts[i].resolvedStyle.paddingBottom;
                if (want > needed) needed = want;
            }

            var first = hosts[0].worldBound.height;
            for (var i = 0; i < hosts.Count; i++)
            {
                var host = hosts[i];
                var card = host.Q<Button>(className: "build-card");

                // The card fills its host: what is left over is the host's own gutter, nothing more.
                var gutter = host.resolvedStyle.paddingTop + host.resolvedStyle.paddingBottom;
                Assert.That(card.worldBound.height, Is.EqualTo(host.worldBound.height - gutter).Within(Slack),
                    "build card " + i + " (" + card.name + ") is " + card.worldBound.height + "px inside a "
                    + host.worldBound.height + "px host, so it is sized to its own text rather than filling the "
                    + "row: `.build-card { flex-grow: 1 }` is not reaching it.");

                Assert.That(host.worldBound.height, Is.EqualTo(first).Within(Slack),
                    "build card " + i + " (" + card.name + ") is " + host.worldBound.height + "px where the "
                    + "first is " + first + "px. The cards hold one height only while none outgrows the "
                    + "`min-height` on `.build-host` (BuildPanel.uss); raise it past " + needed + "px.");
            }
        }

        /// <summary>
        /// REL-120: the grid holds two columns at the narrow drawer as well as the wide one, and no line of the
        /// panel's text crosses its border. Both were broken at 1280×720 and at 1920 with the interface at 150%,
        /// which are the same layout — the drawer is 680 there rather than 760 (`.compact-ui #build-panel`), and
        /// the detail pane's fixed 260 took so much of it that two 200px hosts no longer fit. The card is not
        /// what changed: the pane beside it is narrower when the drawer is, and a host GROWS, so a row that can
        /// still hold only one is filled by it instead of leaving the rest of the row empty.
        ///
        /// The size is forced on the panel here rather than on the screen: a Play test cannot resize the Game
        /// view, and the interface scale lives on a PanelSettings ASSET, which a test must not dirty. Adding the
        /// class and the width to the live elements puts the same numbers through the same layout engine, and
        /// both are taken off again at the end.
        ///
        /// <see cref="HudController"/> is stopped for that window. It repaints every frame and its dock sets
        /// `compact-ui` from the root's OWN resolved size, so on a big Game view it strips the class back off
        /// between the test adding it and the layout pass — which made this test pass or fail by the editor's
        /// window size rather than by the sheets. It is switched back on in the `finally`.
        /// </summary>
        [UnityTest, Timeout(30000)]
        public IEnumerator TheBuildGridKeepsTwoColumnsWhenTheDrawerIsNarrow()
        {
            const float Compact = 680f;                 // `.compact-ui #build-panel` in industrial.uss

            yield return SceneFixture.LoadWorld();
            var shell = Object.FindAnyObjectByType<UiShell>();
            Assert.That(shell, Is.Not.Null, "GameUI.unity has no UiShell.");
            var build = Object.FindAnyObjectByType<BuildPanelController>();
            if (build == null) Assert.Ignore("needs scene: GameUI.unity has no BuildPanelController.");

            var document = UiFixture.DocumentWith(Build, out var panel);
            Assert.That(panel, Is.Not.Null, "no element named \"" + Build + "\".");
            var root = document.rootVisualElement;
            var wasCompact = root.ClassListContains("compact-ui");
            var hud = Object.FindAnyObjectByType<HudController>();
            var hudWasOn = hud != null && hud.enabled;

            Assert.That(shell.Open(Build), Is.True, "UiShell.Open(\"" + Build + "\") refused.");
            yield return null;
            build.Paint(true);
            yield return null;

            if (hud != null) hud.enabled = false;      // or its dock takes `compact-ui` straight back off again
            root.AddToClassList("compact-ui");
            panel.style.width = Compact;
            yield return null;
            yield return null;                         // a second frame, so the layout pass has run before measuring

            try
            {
                var grid = root.Q<VisualElement>("build-grid");
                Assert.That(grid, Is.Not.Null, "BuildPanel.uxml has no #build-grid.");

                var hosts = new List<VisualElement>();
                foreach (var child in grid.Children())
                    if (UiFixture.Shown(child)) hosts.Add(child);
                if (hosts.Count < 2)
                    Assert.Ignore("the " + build.Tab + " tab shows " + hosts.Count + " cards, so there is no second "
                        + "column to look for.");

                var columns = 0;
                foreach (var host in hosts)
                    if (Mathf.Abs(host.worldBound.y - hosts[0].worldBound.y) < 1f) columns++;
                Assert.That(columns, Is.GreaterThanOrEqualTo(2),
                    "the build grid is " + grid.worldBound.width.ToString("0") + "px wide in a " + Compact
                    + "px drawer and holds " + columns + " column(s). Two 200px hosts need 400: either the detail "
                    + "pane beside the grid has grown back (`.compact-ui .build-detail`, industrial.uss) or the "
                    + "`min-width` on `.build-host` has.");

                // Nothing is left half empty either: the row the hosts occupy reaches the grid's own right edge.
                var used = 0f;
                foreach (var host in hosts) if (host.worldBound.xMax > used) used = host.worldBound.xMax;
                Assert.That(used, Is.EqualTo(grid.worldBound.xMax).Within(2f),
                    "the cards stop " + (grid.worldBound.xMax - used).ToString("0") + "px short of the grid's right "
                    + "edge, which is the dead space REL-120 removed: `.build-host` is not growing into its row.");

                // And no line of text crosses the drawer's inner edge (the stat line "… · 40 HP" did). A label's
                // BOX cannot show that on its own: a `nowrap` label keeps its box and draws the text straight
                // through it, which is exactly why REL-120 went unseen. So both are checked — the box against the
                // panel's inner edge, and the text against the column the label actually has.
                var inner = panel.worldBound.xMax - panel.resolvedStyle.paddingRight;
                foreach (var label in panel.Query<Label>().ToList())
                {
                    if (!UiFixture.Shown(label) || string.IsNullOrEmpty(label.text)) continue;

                    Assert.That(label.worldBound.xMax, Is.LessThanOrEqualTo(inner + 1f),
                        "#" + label.name + " (\"" + label.text + "\") reaches "
                        + label.worldBound.xMax.ToString("0") + ", past the panel's inner edge at "
                        + inner.ToString("0") + ". A sub-line of this panel is not wrapping (REL-120).");

                    var onOneLine = label.MeasureTextSize(label.text, 0f, VisualElement.MeasureMode.Undefined,
                        0f, VisualElement.MeasureMode.Undefined).x;
                    var column = label.contentRect.width;
                    if (onOneLine <= column + 1f) continue;          // it fits; how it is set does not matter

                    // It does not fit, so it must be allowed to wrap or to ellipse. Running on is the defect.
                    // (`overflow` is not on IResolvedStyle; the ellipsis rules in this panel set it alongside
                    // `text-overflow`, so the one property is the honest thing to read.)
                    var wraps = label.resolvedStyle.whiteSpace == WhiteSpace.Normal;
                    var ellipses = label.resolvedStyle.textOverflow == TextOverflow.Ellipsis;
                    Assert.That(wraps || ellipses, Is.True,
                        "#" + label.name + " (\"" + label.text + "\") is " + onOneLine.ToString("0")
                        + "px of text in a " + column.ToString("0") + "px column, and it neither wraps nor "
                        + "ellipses, so it draws " + (onOneLine - column).ToString("0")
                        + "px past its own box (REL-120: `#build-panel .panel-sub { white-space: normal }`).");
                }
            }
            finally
            {
                panel.style.width = StyleKeyword.Null;
                if (!wasCompact) root.RemoveFromClassList("compact-ui");
                if (hud != null) hud.enabled = hudWasOn;
            }
        }

        /// <summary>
        /// Playtest defect: "action bar disappears / panels cover everything". A drawer may cover the world; it
        /// may not cover the two buttons that open the drawers.
        /// </summary>
        [UnityTest, Timeout(30000)]
        public IEnumerator TheActionBarStaysVisibleAndUncoveredWhileAPanelIsOpen()
        {
            yield return SceneFixture.LoadWorld();
            var shell = Object.FindAnyObjectByType<UiShell>();
            Assert.That(shell, Is.Not.Null, "GameUI.unity has no UiShell.");

            var document = UiFixture.DocumentWith(ActionBar, out var bar);
            Assert.That(bar, Is.Not.Null, "Hud.uxml has no #" + ActionBar + ": the owner had nothing to click.");
            var root = document.rootVisualElement;

            shell.Open(Inventory);
            yield return null;
            yield return null;

            Assert.That(UiFixture.Shown(bar), Is.True, "the action bar is not on screen with the Backpack open.");
            var panel = root.Q<VisualElement>(Inventory);
            if (panel != null && UiFixture.Shown(panel))
                Assert.That(panel.worldBound.Overlaps(bar.worldBound), Is.False,
                    "the Backpack covers the action bar: " + panel.worldBound + " over " + bar.worldBound + ".");

            foreach (var name in new[] { "open-inventory", "open-build" })
            {
                var button = root.Q<Button>(name);
                Assert.That(button, Is.Not.Null, "Hud.uxml has no #" + name + " button.");
                Assert.That(UiFixture.Shown(button), Is.True, "#" + name + " is not on screen.");
                Assert.That(string.IsNullOrEmpty(button.text), Is.False, "#" + name + " has no label.");
            }

            shell.CloseActive();
            yield return null;
        }

        /// <summary>
        /// Playtest defect: "objective duplicated". The goal card states it; the HUD strip must not, and no other
        /// element may repeat the goal's title anywhere on screen.
        /// </summary>
        [UnityTest, Timeout(30000)]
        public IEnumerator TheObjectiveIsStatedExactlyOnceAndNotInTheHudStrip()
        {
            yield return SceneFixture.LoadWorld();
            var document = UiFixture.DocumentWith("goal-title", out var titleElement);
            Assert.That(titleElement, Is.Not.Null, "GoalCard.uxml has no #goal-title.");
            var root = document.rootVisualElement;
            var title = titleElement as Label;
            Assert.That(title, Is.Not.Null, "#goal-title is not a Label.");

            // Give the card a frame or two to take its first objective from the sim.
            yield return null;
            yield return null;
            if (string.IsNullOrEmpty(title.text))
                Assert.Ignore("the goal card has no objective yet, so there is nothing to count.");

            var strip = root.Q<VisualElement>(Strip);
            Assert.That(strip, Is.Not.Null, "Hud.uxml has no #" + Strip + ".");
            foreach (var gone in new[] { "objective", "objective-row", "objective-title", "objective-text" })
                Assert.That(strip.Q<VisualElement>(gone), Is.Null,
                    "the HUD strip still carries #" + gone + ": the objective is shown twice (playtest defect).");

            var seen = 0;
            foreach (var label in root.Query<Label>().ToList())
            {
                if (!UiFixture.Shown(label)) continue;
                if (string.Equals(label.text, title.text, System.StringComparison.Ordinal)) seen++;
            }
            Assert.That(seen, Is.EqualTo(1),
                "the objective \"" + title.text + "\" is on screen " + seen + " times; it must be stated once.");
        }
    }
}
