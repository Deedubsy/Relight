using System;
using System.Collections;
using System.IO;
using NUnit.Framework;
using Relight.Presentation;
using Relight.Sim;
using UnityEngine;
using UnityEngine.TestTools;

namespace Relight.Tests.Play
{
    /// <summary>
    /// The one persistence check the in-memory tests cannot give: a save written by the running scene through the
    /// real <see cref="AutosaveController"/> (so <see cref="SystemFileSystem"/> and <c>Application.persistentDataPath</c>)
    /// and read back by a <b>different session</b>. The two phases are separate tests so they can run in two
    /// separate editor processes:
    ///
    /// <code>
    /// -runTests -testPlatform PlayMode -testFilter Relight.Tests.Play.DiskRoundTrip.Phase1_ChangeStateAndSaveToDisk
    /// -runTests -testPlatform PlayMode -testFilter Relight.Tests.Play.DiskRoundTrip.Phase2_AFreshSessionLoadsTheSaveFromDisk
    /// </code>
    ///
    /// Phase 1 plays: it walks the engineer, places a chest and puts steel into it, then saves to the manual slot
    /// <c>roundtrip-check</c> and records what it expects next to the save. Phase 2 starts the scene again (a new
    /// game — it first proves the fresh state is <i>not</i> the saved one), loads the slot through the controller,
    /// and checks position, inventory, the machine and its contents, the tick and the whole-state hash, then lets
    /// the loaded state run on and checks the views follow it. Phase 2 without Phase 1's record is ignored, not
    /// failed, so the pair is <see cref="ExplicitAttribute"/> and never part of the ordinary suite.
    /// </summary>
    [Explicit]
    public sealed class DiskRoundTrip
    {
        public const string Slot = "roundtrip-check";
        private const string Evidence = "E:/Factorio2/Unity/Docs/evidence/phase-b";
        private const int SteelMoved = 7;

        [Serializable]
        public sealed class Record
        {
            public string when;
            public string persistentDataPath;
            public string savePath;
            public long saveBytes;
            public string hash;
            public int tick;
            public double posX;
            public double posY;
            public double startX;
            public double startY;
            public int machineCount;
            public int machineId;
            public string machineKind;
            public int machineX;
            public int machineY;
            public double steelCarried;
            public double copperCarried;
            public double steelInChest;
            public double playSeconds;
            // Phase 2 only.
            public string freshHash;
            public string loadedHash;
            public int ticksAfterLoad;
            public string verdict;
        }

        private static string ExpectedPath => Path.Combine(Application.persistentDataPath, Slot + ".expected.json");

        [UnityTest, Timeout(120000)]
        public IEnumerator Phase1_ChangeStateAndSaveToDisk()
        {
            yield return SceneFixture.LoadWorld();
            var host = UnityEngine.Object.FindAnyObjectByType<SimHost>();
            var saver = UnityEngine.Object.FindAnyObjectByType<AutosaveController>();
            Assert.That(saver, Is.Not.Null, "World.unity has no AutosaveController.");
            var st = host.Simulation.State;
            var start = st.Engineer.Pos;
            Assert.That(st.Machines.Count, Is.Zero, "a new game on the synthetic map starts with no machines.");
            Assert.That(st.Engineer.Inv[ItemId.Steel], Is.EqualTo(20), "the starting stake carries 20 steel.");

            // 1. Change the position: walk three tiles east and two north, then stand still.
            host.Submit(new MoveCommand(start.X + 3, start.Y + 2));
            yield return new WaitForSecondsRealtime(2.0f);
            var moved = st.Engineer.Pos;
            Assert.That(Math.Abs(moved.X - start.X) + Math.Abs(moved.Y - start.Y), Is.GreaterThan(2.0),
                $"the engineer did not walk: start {start.X},{start.Y} now {moved.X},{moved.Y}");

            // 2. Change the world and the inventory: a chest one tile away, then seven steel into it.
            var cx = (int)Math.Floor(moved.X) + 1;
            var cy = (int)Math.Floor(moved.Y);
            host.Submit(new PlaceMachineCommand("chest", cx, cy));
            yield return new WaitForSecondsRealtime(0.3f);
            Assert.That(st.Machines.Count, Is.EqualTo(1), "the chest was not placed.");
            var chest = st.Machines[0];
            host.Submit(new MachineTransferCommand(chest.Id, ItemId.Steel, SteelMoved, true));
            yield return new WaitForSecondsRealtime(0.3f);
            Assert.That(st.Engineer.Inv[ItemId.Steel], Is.EqualTo(20 - SteelMoved), "the steel did not leave the Backpack.");
            Assert.That(chest.Inv[ItemId.Steel], Is.EqualTo(SteelMoved), "the steel did not arrive in the chest.");

            // 3. Save through the controller, at a tick boundary like the game would: pause so nothing moves while
            //    the record is taken, then the save is exactly the state the record describes.
            host.Paused = true;
            yield return null;
            var result = saver.Save(Slot);
            Assert.That(result.Ok, Is.True, "the save was refused: " + result.Reason);
            Assert.That(File.Exists(result.Path), Is.True, "the store reported a path that does not exist: " + result.Path);
            Assert.That(result.Path.Replace('\\', '/'),
                Does.StartWith(Application.persistentDataPath.Replace('\\', '/')), "the save is not under persistentDataPath");

            var record = new Record
            {
                when = DateTime.UtcNow.ToString("o"),
                persistentDataPath = Application.persistentDataPath,
                savePath = result.Path,
                saveBytes = new FileInfo(result.Path).Length,
                hash = host.Simulation.Hash(),
                tick = st.Tick,
                posX = st.Engineer.Pos.X,
                posY = st.Engineer.Pos.Y,
                startX = start.X,
                startY = start.Y,
                machineCount = st.Machines.Count,
                machineId = chest.Id,
                machineKind = chest.Kind,
                machineX = chest.X,
                machineY = chest.Y,
                steelCarried = st.Engineer.Inv[ItemId.Steel],
                copperCarried = st.Engineer.Inv[ItemId.Copper],
                steelInChest = chest.Inv[ItemId.Steel],
                playSeconds = st.PlaySeconds,
                verdict = "saved",
            };
            var json = JsonUtility.ToJson(record, true);
            File.WriteAllText(ExpectedPath, json);
            Directory.CreateDirectory(Evidence);
            File.WriteAllText(Path.Combine(Evidence, "b11-disk-roundtrip-phase1.json"), json);
            Debug.Log("DISK-ROUNDTRIP phase 1: " + json);
            host.Paused = false;
        }

        [UnityTest, Timeout(120000)]
        public IEnumerator Phase2_AFreshSessionLoadsTheSaveFromDisk()
        {
            if (!File.Exists(ExpectedPath)) Assert.Ignore("run Phase1_ChangeStateAndSaveToDisk first; no record at " + ExpectedPath);
            var expected = JsonUtility.FromJson<Record>(File.ReadAllText(ExpectedPath));

            yield return SceneFixture.LoadWorld();
            var host = UnityEngine.Object.FindAnyObjectByType<SimHost>();
            var saver = UnityEngine.Object.FindAnyObjectByType<AutosaveController>();
            var fresh = host.Simulation.State;

            // The new session is a new game, not the saved one — otherwise a passing load would prove nothing.
            var freshHash = host.Simulation.Hash();
            Assert.That(fresh.Machines.Count, Is.Zero, "the fresh session already has a machine.");
            Assert.That(fresh.Engineer.Inv[ItemId.Steel], Is.EqualTo(20), "the fresh session does not carry the stake.");
            Assert.That(freshHash, Is.Not.EqualTo(expected.hash), "the fresh session already equals the save.");

            host.Paused = true;
            yield return null;
            var loaded = saver.Load(Slot);
            Assert.That(loaded.Ok, Is.True, "the load was refused: " + loaded.Reason);
            Assert.That(loaded.Warning, Is.Null, "the load warned: " + loaded.Warning);
            Assert.That(loaded.Recovered, Is.Null, "the load recovered something: " + loaded.Recovered);
            Assert.That(loaded.Path?.Replace('\\', '/'), Is.EqualTo(expected.savePath.Replace('\\', '/')).Or.Null,
                "a different file was read");

            var st = host.Simulation.State;
            Assert.That(ReferenceEquals(st, fresh), Is.False, "the host still runs the fresh state.");
            var loadedHash = host.Simulation.Hash();
            Assert.That(loadedHash, Is.EqualTo(expected.hash), "the whole-state hash did not survive the disk.");
            Assert.That(st.Tick, Is.EqualTo(expected.tick), "tick");
            Assert.That(st.Engineer.Pos.X, Is.EqualTo(expected.posX).Within(1e-9), "engineer x");
            Assert.That(st.Engineer.Pos.Y, Is.EqualTo(expected.posY).Within(1e-9), "engineer y");
            Assert.That(st.Engineer.Inv[ItemId.Steel], Is.EqualTo(expected.steelCarried), "steel carried");
            Assert.That(st.Engineer.Inv[ItemId.Copper], Is.EqualTo(expected.copperCarried), "copper carried");
            Assert.That(st.Machines.Count, Is.EqualTo(expected.machineCount), "machine count");
            var chest = st.MachineById(expected.machineId);
            Assert.That(chest, Is.Not.Null, "the chest came back with a different id or not at all");
            Assert.That(chest.Kind, Is.EqualTo(expected.machineKind));
            Assert.That((chest.X, chest.Y), Is.EqualTo((expected.machineX, expected.machineY)), "chest tile");
            Assert.That(chest.Inv[ItemId.Steel], Is.EqualTo(expected.steelInChest), "steel in the chest");
            Assert.That(st.PlaySeconds, Is.EqualTo(expected.playSeconds).Within(1e-9), "play clock");

            // The loaded state must run and be shown: unpause, let it tick, and check the views follow it.
            host.Paused = false;
            yield return new WaitForSecondsRealtime(0.5f);
            var ticksAfter = st.Tick - expected.tick;
            Assert.That(ticksAfter, Is.GreaterThan(3), "the loaded state does not tick.");
            var view = UnityEngine.Object.FindAnyObjectByType<Relight.Presentation.EngineerView>();
            Assert.That(Math.Abs(view.SimPosition.X - st.Engineer.Pos.X) + Math.Abs(view.SimPosition.Y - st.Engineer.Pos.Y),
                Is.LessThan(0.01), "the engineer view did not move to the loaded position.");
            var presenter = UnityEngine.Object.FindAnyObjectByType<MachinePresenter>();
            Assert.That(presenter.Views.ContainsKey(chest.Id), "no MachineView was spawned for the loaded chest.");

            expected.freshHash = freshHash;
            expected.loadedHash = loadedHash;
            expected.ticksAfterLoad = ticksAfter;
            expected.verdict = "restored in a fresh session";
            var json = JsonUtility.ToJson(expected, true);
            Directory.CreateDirectory(Evidence);
            File.WriteAllText(Path.Combine(Evidence, "b11-disk-roundtrip-phase2.json"), json);
            Debug.Log("DISK-ROUNDTRIP phase 2: " + json);
        }
    }
}
