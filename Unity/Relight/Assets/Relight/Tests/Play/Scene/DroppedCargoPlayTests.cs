using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using Relight.Presentation;
using Relight.Sim;
using Relight.Sim.UI;
using Relight.UI;
using Relight.World;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.TestTools;

namespace Relight.Tests.Play
{
    /// <summary>
    /// INT-01 (REL-5) on the real World scene: die, walk back, collect. The sim has always dropped the Backpack as a
    /// pile and has always had the collect command (<c>LedgerTests</c> pins both); what was missing was everything
    /// the player touches — a marker where the pile lies, a notice that names the place, and a key that issues the
    /// command. So this test goes through those: the presenter's own list, the HUD's own inbox, and a real E press
    /// with the pointer on the pile. The walk out and the walk back are the sim's own <see cref="MoveCommand"/>,
    /// and the respawn is waited for, not skipped.
    /// </summary>
    public sealed class DroppedCargoPlayTests
    {
        [UnityTest, Timeout(120000)]
        public IEnumerator DieWalkBackAndPressE_TheCargoReturnsAndThePileIsGone()
        {
            yield return SceneFixture.LoadWorld();
            var host = Object.FindAnyObjectByType<SimHost>();
            var shell = Object.FindAnyObjectByType<UiShell>();
            var hud = Object.FindAnyObjectByType<HudController>();
            var marker = Object.FindAnyObjectByType<DropCachePresenter>();
            var camera = Camera.main;
            Assert.That(host, Is.Not.Null, "World.unity has no SimHost.");
            Assert.That(marker, Is.Not.Null, "World.unity has no DropCachePresenter: nothing would draw the pile.");
            Assert.That(hud, Is.Not.Null, "GameUI.unity has no HudController.");
            if (camera == null) Assert.Ignore("no main camera in the World scene.");
            var sim = host.Simulation;
            var ctx = sim.Context;
            var st = sim.State;
            var e = st.Engineer;
            var keyboard = VirtualKeyboard();
            var mouse = VirtualMouse();
            if (shell != null) shell.CloseActive();
            host.Paused = false;
            yield return null;
            Assert.That(marker.Drawn, Is.Empty, "a new game has no dropped cargo.");

            // Walk out of Home: far enough that the pile is out of reach from where the engineer gets back up.
            var start = e.Pos;
            var away = e.Reach + 5;
            var offsets = new[] { new Vec2(-1, 0), new Vec2(1, 0), new Vec2(0, 1), new Vec2(0, -1) };
            var out_ = false;
            for (var i = 0; i < offsets.Length && !out_; i++)
            {
                sim.Apply(new MoveCommand(start.X + offsets[i].X * (away + 3), start.Y + offsets[i].Y * (away + 3)));
                yield return Until(() => Dist(e.Pos, start) >= away, 8f);
                out_ = Dist(e.Pos, start) >= away && !HandCraft.NearDepot(ctx, st);
            }
            if (!out_) Assert.Ignore("the engineer could not walk " + away + " tiles from spawn in any of four directions.");
            sim.Apply(new MoveCommand(e.Pos.X, e.Pos.Y));

            // What they carry. Weapons are retained through death, so the comparison is of everything else.
            e.Inv[ItemId.Steel] = e.Inv[ItemId.Steel] + 20;
            e.Inv[ItemId.Copper] = e.Inv[ItemId.Copper] + 5;
            var before = Cargo(e);
            Assert.That(before.Count, Is.GreaterThanOrEqualTo(2));

            // Die. The sim drops the pile on the tile the body fell on.
            var fellX = (int)System.Math.Floor(e.Pos.X);
            var fellY = (int)System.Math.Floor(e.Pos.Y);
            e.TakeDamage(ctx, st, ctx.Data.Engineer.MaxHp);
            Assert.That(e.IsDown, Is.True, "the engineer did not go down (is Admin invulnerability on?).");
            Assert.That(Cargo(e), Is.Empty, "the cargo went to the ground.");
            var pile = DeathCache.OnTile(st, fellX, fellY);
            Assert.That(pile, Is.Not.Null, "the sim dropped no pile where the body fell.");
            var id = pile.Id;

            // 1. The pile is drawn where the sim says it is.
            yield return null;
            yield return null;
            Assert.That(marker.Drawn, Has.Count.EqualTo(1));
            Assert.That(marker.Drawn[0], Is.EqualTo((id, fellX, fellY)), "the marker is not on the sim's tile.");

            // 2. A notice names the place, and the goal row mentions the pile.
            yield return Until(() => Notice(hud) != null, 3f);
            var place = hud.Model.Defence.PlaceAt(ctx, fellX + 0.5, fellY + 0.5);
            Assert.That(Notice(hud), Is.Not.Null, "no dropped-cargo notice on the HUD.");
            Assert.That(Notice(hud).Text, Is.EqualTo(HudViewModel.DroppedCargoText(1, place)));
            Assert.That(place, Is.Not.Empty);
            var goal = OpeningQueries.Objective(ctx, st);
            Assert.That(goal.Title, Is.EqualTo("Recover at Home"));
            Assert.That(goal.Detail, Does.Contain("lying where you fell"));

            // Get back up — the real wait — and find the pile out of reach.
            yield return Until(() => !e.IsDown, (float)ctx.Data.Engineer.RespawnS + 5f);
            Assert.That(e.IsDown, Is.False, "the engineer never got back up.");
            Assert.That(DeathCache.InReach(ctx, st, pile), Is.False, "this test needs a walk back; the pile is already in reach.");
            Assert.That(sim.Apply(new CollectCacheCommand(id)).Problem, Is.EqualTo(DeathCache.ReachText));

            // Walk back.
            sim.Apply(new MoveCommand(fellX + 0.5, fellY + 0.5));
            yield return Until(() => DeathCache.InReach(ctx, st, pile), 15f);
            Assert.That(DeathCache.InReach(ctx, st, pile), Is.True, "the engineer could not walk back to the pile.");
            sim.Apply(new MoveCommand(e.Pos.X, e.Pos.Y));
            yield return null;

            // 3. E with the pointer on the pile issues the collect command.
            var screen = camera.WorldToScreenPoint(WorldSpace.TileCentre(fellX, fellY));
            if (screen.z <= 0 || screen.x < 0 || screen.y < 0 || screen.x > Screen.width || screen.y > Screen.height)
                Assert.Ignore("the pile's tile is off screen; the camera framing is not this test's subject.");
            yield return Point(mouse, new Vector2(screen.x, screen.y));
            // REL-125: when E does nothing, say why — the notice it raised, the tile the pointer resolved to (the
            // camera may still be settling after the walk back) and what is left in the pile.
            var notices = new List<string>();
            var noticeWas = WorldInput.InteractionNotice;
            WorldInput.InteractionNotice = text => { notices.Add(text); noticeWas?.Invoke(text); };
            var pointedAt = "";
            try
            {
                yield return Press(keyboard, Key.E);
                pointedAt = PointerTile(camera, mouse);
                yield return Release(keyboard);
                yield return Until(() => DeathCache.Find(st, id) == null, 3f);
            }
            finally { WorldInput.InteractionNotice = noticeWas; }

            Assert.That(DeathCache.Find(st, id), Is.Null, "E did not collect the pile (or it was not emptied). Pile at ("
                + fellX + ", " + fellY + "), pointer on " + pointedAt + ", in reach " + DeathCache.InReach(ctx, st, pile)
                + ", left in it " + Left(pile) + ", notices [" + string.Join(" | ", notices) + "].");
            Assert.That(st.Drops.Caches, Is.Empty, "4. an emptied pile is removed.");
            var after = Cargo(e);
            Assert.That(after.Count, Is.EqualTo(before.Count), "a different set of items came back.");
            foreach (var kv in before)
                Assert.That(after.TryGetValue(kv.Key, out var n) ? n : 0, Is.EqualTo(kv.Value), kv.Key + " did not all return.");
            Assert.That(string.IsNullOrEmpty(shell != null ? shell.Active : ""), Is.True, "collecting must not open a drawer.");

            yield return null;
            yield return null;
            Assert.That(marker.Drawn, Is.Empty, "the marker outlived its pile.");
            yield return Until(() => Notice(hud) == null, 3f);
            Assert.That(Notice(hud), Is.Null, "the standing notice outlived its pile.");
        }

        private static Dictionary<string, double> Cargo(Engineer e)
        {
            var keys = new List<ItemKey>();
            e.Inv.Keys(keys);
            var held = new Dictionary<string, double>();
            for (var i = 0; i < keys.Count; i++)
                if (!keys[i].IsWeapon) held[keys[i].Key] = e.Inv[keys[i]];
            return held;
        }

        /// <summary>The tile WorldInput resolves the pointer to, by its own projection (WorldInput.TryPointer).</summary>
        private static string PointerTile(Camera camera, Mouse mouse)
        {
            var at = mouse.position.ReadValue();
            var world = camera.ScreenToWorldPoint(new Vector3(at.x, at.y, -camera.transform.position.z));
            var p = WorldSpace.Position(world);
            return "(" + (int)System.Math.Floor(p.X) + ", " + (int)System.Math.Floor(p.Y) + ")";
        }

        private static string Left(DropCache pile)
        {
            var keys = new List<ItemKey>();
            pile.Items.Keys(keys);
            var parts = new List<string>();
            for (var i = 0; i < keys.Count; i++) parts.Add(keys[i].Key + " " + pile.Items[keys[i]]);
            return parts.Count == 0 ? "nothing" : string.Join(", ", parts);
        }

        private static double Dist(Vec2 a, Vec2 b) =>
            System.Math.Sqrt((a.X - b.X) * (a.X - b.X) + (a.Y - b.Y) * (a.Y - b.Y));

        private static HudNotice Notice(HudController hud)
        {
            var rows = hud.Model.Notices.Rows;
            for (var i = 0; i < rows.Count; i++) if (rows[i].Key == HudViewModel.DroppedCargoKey) return rows[i];
            return null;
        }

        // ---- devices -------------------------------------------------------------------------------------------

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
