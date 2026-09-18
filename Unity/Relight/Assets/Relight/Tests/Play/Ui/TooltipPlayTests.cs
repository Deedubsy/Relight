using System.Collections;
using NUnit.Framework;
using Relight.UI;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UIElements;

namespace Relight.Tests.Play.Ui
{
    /// <summary>
    /// Contract C5, the playtest's "no tooltips" finding: the pointer rests on a thing, and after
    /// <see cref="TooltipController.DelayMs"/> the UI says what it is. Leaving takes it away again.
    ///
    /// The delay is the point of the test as much as the appearance is: a tooltip that shows on the first frame
    /// flickers across a slot grid, which is the behaviour C5 exists to forbid. So each test asserts the absence
    /// first and the presence second.
    ///
    /// Written WITHOUT Unity on this machine: never compiled, never run here.
    /// </summary>
    public sealed class TooltipPlayTests
    {
        /// <summary>Comfortably past the 300 ms delay, and still far inside the test timeout.</summary>
        private const float PastTheDelay = 0.4f;

        /// <summary>
        /// The scene may not carry the controller yet (the coordinator adds it). A test that skipped for that
        /// reason would never check C5 at all, so it adds one to the GameUI document's own object instead —
        /// exactly the component and the wiring the report asks the coordinator to author.
        /// </summary>
        private static TooltipController Controller(UIDocument document)
        {
            // OnEnable binds, whether the component came from the scene or from AddComponent below, so nothing
            // here calls Bind(): a second Bind would register a second GeometryChangedEvent callback.
            var existing = Object.FindAnyObjectByType<TooltipController>();
            if (existing != null) return existing;
            Assert.That(document, Is.Not.Null, "no UIDocument to host a TooltipController.");
            // Awake takes `document` from this same GameObject, which is why it must be added here and not to a
            // bare object: TooltipController's serialized field is private and the scene is not ours to edit.
            return document.gameObject.AddComponent<TooltipController>();
        }

        private static VisualElement Tip(UIDocument document)
        {
            var tip = document.rootVisualElement.Q<VisualElement>("tooltip");
            Assert.That(tip, Is.Not.Null, "GameUI.uxml has no #tooltip element: C5 has nothing to show.");
            return tip;
        }

        /// <summary>
        /// R4 over the Backpack: hovering a slot that holds something names it after the delay, and the tooltip
        /// goes when the pointer leaves.
        /// </summary>
        [UnityTest, Timeout(30000)]
        public IEnumerator RestingOnAnInventorySlotShowsATooltipAfterTheDelayAndLeavingHidesIt()
        {
            yield return SceneFixture.LoadWorld();
            var document = UiFixture.DocumentWith("tooltip", out _);
            var controller = Controller(document);
            var tip = Tip(document);

            var shell = Object.FindAnyObjectByType<UiShell>();
            var panel = Object.FindAnyObjectByType<InventoryPanelController>();
            Assert.That(shell, Is.Not.Null, "GameUI.unity has no UiShell.");
            Assert.That(panel, Is.Not.Null, "GameUI.unity has no InventoryPanelController.");

            shell.Open("inventory-panel");
            yield return null;
            panel.Paint(true);
            yield return null;

            var index = -1;
            var cells = panel.Model.Pack;
            for (var i = 0; i < cells.Count; i++)
                if (!cells[i].Empty) { index = cells[i].Index; break; }
            if (index < 0) Assert.Ignore("the Backpack is empty in this save, so no slot has anything to describe.");

            var slot = document.rootVisualElement.Q<Button>("pack-" + index);
            Assert.That(slot, Is.Not.Null, "no #pack-" + index + " button in the drawer.");
            Assert.That(UiFixture.Shown(slot), Is.True, "the slot is not on screen, so it cannot be hovered.");

            controller.HideNow();
            UiFixture.Enter(slot);
            yield return null;
            Assert.That(controller.Visible, Is.False,
                "the tooltip appeared on the first frame: C5's " + TooltipController.DelayMs + " ms delay is not being honoured.");

            yield return WaitForReveal(controller);
            Assert.That(controller.Visible, Is.True,
                "no tooltip after " + PastTheDelay + " s over slot " + index + " (the playtest's \"no tooltips\").");
            Assert.That(controller.ShownFor, Is.SameAs(slot), "the tooltip belongs to a different element.");
            Assert.That(tip.ClassListContains(TooltipController.HiddenClass), Is.False,
                "#tooltip still carries ." + TooltipController.HiddenClass + " while the controller says it is visible.");
            Assert.That(UiFixture.Shown(tip), Is.True, "#tooltip is not actually on screen.");
            Assert.That(tip.pickingMode, Is.EqualTo(PickingMode.Ignore),
                "C5: the tooltip must ignore the pointer or it steals the hover that keeps it open.");

            var title = tip.Q<Label>("tooltip-title");
            Assert.That(title, Is.Not.Null, "#tooltip has no #tooltip-title.");
            Assert.That(string.IsNullOrEmpty(title.text), Is.False, "the tooltip is blank.");

            UiFixture.Leave(slot);
            yield return null;
            Assert.That(controller.Visible, Is.False, "the tooltip stayed up after the pointer left the slot.");
            Assert.That(tip.ClassListContains(TooltipController.HiddenClass), Is.True,
                "#tooltip was not hidden by its USS class.");

            shell.CloseActive();
            yield return null;
        }

        /// <summary>
        /// R4 over the HUD: the three strip blocks explain themselves too, and they are reachable without opening
        /// anything — the case the owner meets first.
        /// </summary>
        [UnityTest, Timeout(30000)]
        public IEnumerator RestingOnAHudBlockExplainsIt()
        {
            yield return SceneFixture.LoadWorld();
            var document = UiFixture.DocumentWith("tooltip", out _);
            var controller = Controller(document);
            var tip = Tip(document);
            var root = document.rootVisualElement;

            foreach (var name in new[] { "clock-block", "power-block", "core-block" })
            {
                var block = root.Q<VisualElement>(name);
                Assert.That(block, Is.Not.Null, "Hud.uxml has no #" + name + ".");
                if (!UiFixture.Shown(block)) Assert.Ignore("#" + name + " is not on screen in this scene.");

                controller.HideNow();
                UiFixture.Enter(block);
                yield return WaitForReveal(controller);

                Assert.That(controller.Visible, Is.True, "no tooltip over #" + name + ".");
                Assert.That(controller.ShownFor, Is.SameAs(block), "the tooltip over #" + name + " belongs elsewhere.");
                var title = tip.Q<Label>("tooltip-title");
                Assert.That(string.IsNullOrEmpty(title == null ? null : title.text), Is.False,
                    "the tooltip over #" + name + " is blank.");

                UiFixture.Leave(block);
                yield return null;
                Assert.That(controller.Visible, Is.False, "the tooltip over #" + name + " outlived the pointer.");
            }
        }
        private static IEnumerator WaitForReveal(TooltipController controller)
        {
            var deadline = Time.realtimeSinceStartupAsDouble + 2;
            while (!controller.Visible && Time.realtimeSinceStartupAsDouble < deadline) yield return null;
        }
    }
}
