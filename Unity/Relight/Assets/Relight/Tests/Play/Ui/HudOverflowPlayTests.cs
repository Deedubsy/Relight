using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using Relight.Presentation;
using Relight.Sim.UI;
using Relight.UI;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UIElements;

namespace Relight.Tests.Play.Ui
{
    /// <summary>
    /// REL-86 (UI-02a): the screen half of the acceptance. "A sim or UI test raises five problems and sees three rows
    /// plus "+2 more". The count drops as problems clear. Nothing overlaps at ordinary and small window sizes." The
    /// inbox rules themselves are pinned offline in <c>HudViewModelTests</c>; this test reads the DRAWN labels on the
    /// real HUD.
    ///
    /// The two sizes are set the way <c>OpeningUiPlayTests.U5</c> sets its matrix: the HUD document's root is resized
    /// and the panel scale held at 1, so 1280×720 is the compact layout (<c>GameplayDock</c> keys it on the root).
    /// </summary>
    public sealed class HudOverflowPlayTests
    {
        private static readonly (int W, int H)[] Sizes = { (1920, 1080), (1280, 720) };

        /// <summary>Five standing alerts, the first a danger. Long ones on purpose, so the rows wrap.</summary>
        private static readonly (string Key, string Text, HudNoticeKind Kind)[] Five =
        {
            ("rel86:dry", "Founders Court: 1 turret dry", HudNoticeKind.Danger),
            ("rel86:fuel", "Low fuel: about 40 s left at this load (estimate). A full slot of 50 runs a Generator about 11 min flat out — keep it fed by belt.", HudNoticeKind.Warning),
            ("rel86:cargo", "Dropped cargo at Founders Court · walk back and press E beside it to collect", HudNoticeKind.Warning),
            ("rel86:power", "Low power: 4 machines running slowly — add a Generator or remove load", HudNoticeKind.Warning),
            ("rel86:wreck", "Ironworks: 1 wreck", HudNoticeKind.Warning),
        };

        /// <summary>Every other HUD block a notice row could land on, looked up by name in the HUD's own document.</summary>
        private static readonly string[] Neighbours =
        {
            "status-strip", "threat", "alert", "problem-0", "problem-1", "problem-2", "mining", "engineer-block",
            "hand-lock", "placement-toolbar", "world-target", "action-bar", "goal-card", "minimap", "admin-launcher",
        };

        [UnityTest, Timeout(60000)]
        public IEnumerator FiveAlertsDrawThreeRowsAndPlusTwoMore_TheCountDrops_AndNothingOverlapsAtTwoSizes()
        {
            yield return SceneFixture.LoadWorld();
            var host = Object.FindAnyObjectByType<SimHost>();
            var hud = Object.FindAnyObjectByType<HudController>();
            Assert.That(hud, Is.Not.Null, "GameUI.unity has no HudController.");
            host.Paused = true;

            var document = UiFixture.DocumentWith("notice-more", out var moreElement);
            Assert.That(moreElement, Is.Not.Null, "Hud.uxml has no notice-more label.");
            var more = (Label)moreElement;
            var root = document.rootVisualElement;
            var rows = new Label[HudNotices.MaxRows];
            for (var i = 0; i < rows.Length; i++) rows[i] = root.Q<Label>("notice-" + i);

            var settings = document.panelSettings;
            var restoreMode = settings.scaleMode;
            var restoreScale = settings.scale;
            var restoreWidth = root.style.width;
            var restoreHeight = root.style.height;
            var notices = hud.Model.Notices;

            try
            {
                settings.scaleMode = PanelScaleMode.ConstantPixelSize;
                settings.scale = 1f;
                notices.Reset();
                var now = Time.unscaledTimeAsDouble;
                foreach (var n in Five) notices.Post(n.Key, n.Text, n.Kind, now, double.PositiveInfinity);

                foreach (var size in Sizes)
                {
                    var label = size.W + "x" + size.H;
                    root.style.width = size.W;
                    root.style.height = size.H;
                    yield return new WaitForSecondsRealtime(0.4f);   // the HUD repaints every 0.15 s, paused or not
                    yield return null;

                    for (var i = 0; i < rows.Length; i++)
                        Assert.That(UiFixture.Shown(rows[i]), Is.True, label + ": notice-" + i + " is not shown.");
                    Assert.That(rows[0].text, Is.EqualTo(Five[0].Text), label + ": the danger row is not first.");
                    Assert.That(UiFixture.Shown(more), Is.True, label + ": the overflow line is not shown.");
                    Assert.That(more.text, Is.EqualTo("+2 more"), label);
                    if (size.W < 1100 || size.H < 800)
                        Assert.That(root.ClassListContains("compact-ui"), Is.True, label + ": not the compact layout.");

                    var drawn = new List<VisualElement>(rows) { more };
                    var bounds = root.worldBound;
                    foreach (var e in drawn)
                        Assert.That(e.worldBound.xMin >= bounds.xMin - 0.5f && e.worldBound.xMax <= bounds.xMax + 0.5f
                                    && e.worldBound.yMin >= bounds.yMin - 0.5f && e.worldBound.yMax <= bounds.yMax + 0.5f,
                            Is.True, label + ": " + e.name + " " + e.worldBound + " runs outside the window " + bounds + ".");
                    for (var a = 0; a < drawn.Count; a++)
                        for (var b = a + 1; b < drawn.Count; b++)
                            Assert.That(drawn[a].worldBound.Overlaps(drawn[b].worldBound), Is.False,
                                label + ": " + drawn[a].name + " overlaps " + drawn[b].name + ".");
                    foreach (var name in Neighbours)
                    {
                        var other = root.Q<VisualElement>(name);
                        if (other == null || !UiFixture.Shown(other)) continue;
                        foreach (var e in drawn)
                            Assert.That(e.worldBound.Overlaps(other.worldBound), Is.False,
                                label + ": " + e.name + " " + e.worldBound + " overlaps " + name + " " + other.worldBound + ".");
                    }
                    Debug.Log("REL-86 " + label + (root.ClassListContains("compact-ui") ? " (compact)" : "") + ": rows y "
                        + rows[0].worldBound.yMin.ToString("0") + "–" + rows[2].worldBound.yMax.ToString("0")
                        + ", \"" + more.text + "\" y " + more.worldBound.yMin.ToString("0") + "–" + more.worldBound.yMax.ToString("0"));
                }

                // The count drops as problems clear: a shown row goes, a hidden one takes its place.
                notices.Clear("rel86:wreck");
                yield return new WaitForSecondsRealtime(0.4f);
                Assert.That(more.text, Is.EqualTo("+1 more"), "one cleared: the count drops by one");
                Assert.That(UiFixture.Shown(rows[2]), Is.True, "the freed place is taken by a hidden row");

                notices.Clear("rel86:power");
                yield return new WaitForSecondsRealtime(0.4f);
                Assert.That(UiFixture.Shown(more), Is.False, "everything live is shown: no overflow line");
                for (var i = 0; i < rows.Length; i++)
                    Assert.That(UiFixture.Shown(rows[i]), Is.True, "notice-" + i + " after two cleared");
            }
            finally
            {
                foreach (var n in Five) notices.Clear(n.Key);
                settings.scaleMode = restoreMode;
                settings.scale = restoreScale;
                root.style.width = restoreWidth;
                root.style.height = restoreHeight;
                host.Paused = false;
            }
        }
    }
}
