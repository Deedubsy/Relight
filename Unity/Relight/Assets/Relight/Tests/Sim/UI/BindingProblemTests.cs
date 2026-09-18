using System.Collections.Generic;
using NUnit.Framework;
using Relight.Sim.UI;

namespace Relight.Sim.Tests
{
    /// <summary>
    /// C-10. <c>controls.ts:bindingProblem</c>, ported (UI_AND_ONBOARDING.md §3.2). Each case below is one row of
    /// that table, and the assertion is on the exact sentence a player would read.
    /// </summary>
    public sealed class BindingProblemTests
    {
        private static readonly Dictionary<string, string> None = new Dictionary<string, string>();

        [Test]
        public void FixedActionsAndUnknownActionsRefuseWithTheSameSentence()
        {
            Assert.AreEqual(BindingProblem.FixedText, BindingProblem.Problem("cancel", "k", None));
            Assert.AreEqual(BindingProblem.FixedText, BindingProblem.Problem("belt", "k", None));
            Assert.AreEqual(BindingProblem.FixedText, BindingProblem.Problem("floodlight", "k", None));
            // Speed control does not exist in the port (U-D-04); asking about it answers as the reference did,
            // because both actions were FIXED there too.
            Assert.AreEqual(BindingProblem.FixedText, BindingProblem.Problem("slower", "k", None));
            Assert.AreEqual(BindingProblem.FixedText, BindingProblem.Problem("faster", "k", None));
        }

        [Test]
        public void OnlyTheReferencesKeySetIsAccepted()
        {
            foreach (var key in new[] { "k", "Z", "ArrowUp", "ArrowRight", "F1", "F12", "Shift", " ", "-", "[", "`", "/" })
                Assert.IsTrue(BindingProblem.Accepted(key), key + " should be accepted");
            foreach (var key in new[] { "", "1", "F0", "F13", "Control", "ArrowIn", "ab", "+" })
                Assert.IsFalse(BindingProblem.Accepted(key), key + " should be rejected");
            Assert.AreEqual(BindingProblem.KeyText, BindingProblem.Problem("map", "1", None));
        }

        [Test]
        public void EscapeAndTabAreReservedForTheInterface()
        {
            Assert.AreEqual(BindingProblem.ReservedText, BindingProblem.Problem("map", "Escape", None));
            Assert.AreEqual(BindingProblem.ReservedText, BindingProblem.Problem("map", "Tab", None));
        }

        [Test]
        public void ACollisionInsideOneContextNamesTheOtherAction()
        {
            // 'm' is map, a world action.
            Assert.AreEqual("Already used by map in this context.", BindingProblem.Problem("threat", "m", None));
            // Ctrl/Cmd actions collide only with each other: 'z' is undo.
            Assert.AreEqual("Already used by undo in this context.", BindingProblem.Problem("copy", "z", None));
            // ...and never with a world action: 'm' is map, but copy is modified.
            Assert.AreEqual("", BindingProblem.Problem("copy", "m", None));
        }

        [Test]
        public void CaseDoesNotMatterToTheCollisionCheck()
        {
            Assert.AreEqual("Already used by map in this context.", BindingProblem.Problem("threat", "M", None));
        }

        [Test]
        public void BlueprintTransformsClashWithWorldActionsExceptTheThreeExemptOnes()
        {
            // mirrorX is blueprint; 'm' is the world action map, so it clashes.
            Assert.AreEqual("Already used by map in this context.", BindingProblem.Problem("mirrorX", "m", None));
            // ...but chest, tramstop and tram are exempt, so their keys stay available to a transform:
            // 'c' is chest (world) and copy (modified), and a blueprint transform clashes with neither.
            Assert.AreEqual("", BindingProblem.Problem("mirrorX", "c", None));
        }

        [Test]
        public void AnOverrideIsWhatTheCollisionCheckComparesAgainst()
        {
            var overrides = new Dictionary<string, string> { { "map", "y" } };
            // map has moved off 'm', so 'm' is free again...
            Assert.AreEqual("", BindingProblem.Problem("threat", "m", overrides));
            // ...and 'y' now clashes with map, not only with redo.
            Assert.AreEqual("Already used by map in this context.", BindingProblem.Problem("threat", "y", overrides));
        }

        [Test]
        public void AnOverrideReplacesOnlyTheFirstDefaultKey()
        {
            var overrides = new Dictionary<string, string> { { "north", "u" } };
            var keys = Bindings.Keys("north", overrides);
            CollectionAssert.AreEqual(new[] { "u", "ArrowUp" }, (System.Collections.ICollection)keys);
        }

        [Test]
        public void ParseKeepsGoodEntriesAndSilentlyDropsBadOnes()
        {
            var parsed = BindingProblem.Parse(new[]
            {
                new KeyValuePair<string, string>("map", "n"),
                new KeyValuePair<string, string>("belt", "n"),      // FIXED
                new KeyValuePair<string, string>("threat", "1"),    // not an accepted key
                new KeyValuePair<string, string>("nonsense", "n"),  // unknown action
                new KeyValuePair<string, string>("inspect", "n"),   // collides with the accepted map override
            });
            Assert.AreEqual(1, parsed.Count);
            Assert.AreEqual("n", parsed["map"]);
        }

        [Test]
        public void ShortcutLabelsMatchTheReference()
        {
            Assert.AreEqual("R", Bindings.Shortcut("rotate"));
            Assert.AreEqual("Space", Bindings.Shortcut("dodge"));
            Assert.AreEqual("", Bindings.Shortcut("wall"));
        }

        [Test]
        public void TheRebindableListExcludesEveryFixedAction()
        {
            var rebindable = Bindings.Rebindable;
            foreach (var action in Bindings.Fixed)
                CollectionAssert.DoesNotContain((System.Collections.ICollection)rebindable, action);
            CollectionAssert.Contains((System.Collections.ICollection)rebindable, "map");
        }
    }
}
