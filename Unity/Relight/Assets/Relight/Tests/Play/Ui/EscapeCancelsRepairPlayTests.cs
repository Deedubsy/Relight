using System.Collections;
using NUnit.Framework;
using Relight.Presentation;
using Relight.Sim;
using Relight.UI;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.TestTools;

namespace Relight.Tests.Play.Ui
{
    /// <summary>
    /// INT-13 (REL-17): the HUD and the workshop both print "Escape cancels" under a running repair, and until this
    /// pass no link of the escape chain sent <see cref="CancelRepairCommand"/>: Escape closed the drawer, which hid
    /// the only Cancel button, and the engineer stayed rooted. The repair here is the Home core's, started through
    /// the sim's own <see cref="RepairCommand"/>; Escape goes in through the real keyboard, because the thing under
    /// test is what one PRESS does, ladder and all.
    /// </summary>
    public sealed class EscapeCancelsRepairPlayTests
    {
        private const string Drawer = "inventory-panel";

        [UnityTest, Timeout(60000)]
        public IEnumerator OneEscapeCancelsTheRepairAndLeavesTheDrawerOpen_TheNextOneClosesIt()
        {
            yield return SceneFixture.LoadWorld();
            var host = Object.FindAnyObjectByType<SimHost>();
            var shell = Object.FindAnyObjectByType<UiShell>();
            Assert.That(host, Is.Not.Null, "World.unity has no SimHost.");
            Assert.That(shell, Is.Not.Null, "GameUI.unity has no UiShell.");
            var sim = host.Simulation;
            var ctx = sim.Context;
            var st = sim.State;
            var e = st.Engineer;
            var keyboard = VirtualKeyboard();
            host.Paused = false;
            shell.CloseActive();
            yield return null;

            // A damaged core, the engineer beside it with the price in the Backpack, and the repair under way.
            Assert.That(HomeQueries.CoreOperational(st), Is.True, "the real city places a Home core");
            HomeCore.Damage(st, 50);
            e.Pos = new Vec2(st.Home.X - 0.5, st.Home.Y + st.Home.H / 2.0);
            e.Inv[ItemId.Steel] = System.Math.Max(e.Inv[ItemId.Steel], ctx.Data.Defence.CoreSteel);
            e.Inv[ItemId.Copper] = System.Math.Max(e.Inv[ItemId.Copper], ctx.Data.Defence.CoreCopper);
            var steel = e.Inv[ItemId.Steel];
            var copper = e.Inv[ItemId.Copper];
            Assert.That(sim.Apply(new RepairCommand(RepairKinds.Core, 0)).Problem, Is.Empty, "the repair starts");
            Assert.That(Home.RepairLocked(st), Is.True, "and roots the engineer");
            Assert.That(e.Inv[ItemId.Steel] + e.Inv[ItemId.Copper], Is.LessThan(steel + copper), "paid for up front");
            Assert.That(sim.Apply(new MoveCommand(e.Pos.X - 3, e.Pos.Y)).Accepted, Is.False, "rooted: walk-here is refused");

            Assert.That(shell.Open(Drawer), Is.True, "the Backpack drawer would not open.");
            yield return null;

            // W/A/S/D while rooted (the agent-9 claim): the key cannot walk the engineer, so it must not close the
            // drawer that holds the Cancel button either.
            yield return Press(keyboard, Key.W);
            yield return Release(keyboard);
            Assert.That(shell.Active, Is.EqualTo(Drawer), "a movement key closed the drawer while the engineer was rooted.");
            Assert.That(Home.RepairLocked(st), Is.True);

            // The first Escape: the repair is cancelled with the Cancel button's refund, and nothing closed.
            yield return Press(keyboard, Key.Escape);
            yield return Release(keyboard);
            Assert.That(Home.RepairLocked(st), Is.False, "Escape did not cancel the repair.");
            Assert.That(st.Home.RepairKind, Is.EqualTo(RepairKinds.None));
            Assert.That(e.Inv[ItemId.Steel], Is.EqualTo(steel), "the steel came back, as the Cancel button gives it back");
            Assert.That(e.Inv[ItemId.Copper], Is.EqualTo(copper), "and the copper");
            Assert.That(shell.Active, Is.EqualTo(Drawer), "the press that cancelled the repair also closed the drawer.");
            Assert.That(host.Paused, Is.False, "or paused the game.");

            // The engineer can walk. The World map is off while a drawer is open, so the walk is sent to the sim.
            var from = e.Pos;
            var moved = 0.0;
            Assert.That(sim.Apply(new MoveCommand(from.X - 3, from.Y)).Accepted, Is.True, "free again: walk-here is accepted");
            var dirs = new[] { (-1, 0), (0, -1), (0, 1), (1, 0) };
            for (var i = 0; i < dirs.Length && moved < 0.3; i++)
            {
                sim.Apply(new WalkCommand(dirs[i].Item1, dirs[i].Item2));
                var target = host.TotalTicks + 20;
                yield return Until(() => host.TotalTicks >= target, 5f);
                moved = (e.Pos - from).Length;
            }
            sim.Apply(new WalkCommand(0, 0));
            Assert.That(moved, Is.GreaterThan(0.3), "the repair was cancelled but the engineer never moved.");

            // The second Escape behaves as it always did: it closes the drawer.
            yield return Press(keyboard, Key.Escape);
            yield return Release(keyboard);
            Assert.That(shell.Active, Is.Null, "with no repair running, Escape closes the drawer as before.");
            Assert.That(host.Paused, Is.False, "one press, one rung.");
        }

        [UnityTest, Timeout(60000)]
        public IEnumerator WithNoRepairRunning_EscapeIsUnchanged()
        {
            yield return SceneFixture.LoadWorld();
            var host = Object.FindAnyObjectByType<SimHost>();
            var shell = Object.FindAnyObjectByType<UiShell>();
            var escape = Object.FindAnyObjectByType<EscapeChain>();
            Assert.That(escape, Is.Not.Null, "GameUI.unity has no EscapeChain.");
            var st = host.Simulation.State;
            host.Paused = false;
            shell.CloseActive();
            yield return null;
            Assert.That(Home.RepairLocked(st), Is.False);

            Assert.That(shell.Open(Drawer), Is.True);
            yield return null;
            var rev = st.Rev;
            var consumed = escape.Escape();
            Assert.That(consumed, Is.Not.Null);
            Assert.That(consumed.Order, Is.EqualTo(EscapeOrder.CloseDrawer), "the repair link declined and the drawer link took the press.");
            Assert.That(shell.Active, Is.Null);
            Assert.That(st.Rev, Is.EqualTo(rev), "no command reached the sim.");
        }

        private static Keyboard VirtualKeyboard()
        {
            var keyboard = InputSystem.GetDevice<Keyboard>() ?? InputSystem.AddDevice<Keyboard>();
            if (!keyboard.enabled) InputSystem.EnableDevice(keyboard);
            return keyboard;
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
