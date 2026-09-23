using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using Relight.Sim.Tests.Support;

namespace Relight.Sim.Tests.Persistence
{
    /// <summary>
    /// Older Unity saves still load (U-M-38, 2026-09-13; Phase C Wave 0, 2026-09-14). Version 1 lacks
    /// <c>state.hand.refundSteel</c> / <c>refundCopper</c>; version 2 lacks every state member Phase C adds. Both are
    /// upgraded on the parsed document by <see cref="SaveUpgrade"/> — v1 through v2 to the current version — and the
    /// file is never rewritten. These tests pin what makes that safe: the old checksum is checked as written, every
    /// member the old file carried survives the upgrade exactly, the file on disk is untouched by the load (and kept
    /// as the <c>.bak</c> by the next save), and anything that is not honestly the version it claims is refused.
    ///
    /// No real version-1 or version-2 file exists on the machine these run on, so the fixtures are forged from a
    /// current document by the inverse of the schema changes, independently of the emitter under test: the state is
    /// pruned to the member names version 2 actually wrote (the golden lists below, captured from a version-2 build
    /// on 2026-09-14), the version numbers are set back and the checksum is recomputed over the forged state text.
    /// The pruning is what keeps these tests honest as Phase C adds state: a member a later build adds is not one a
    /// version-2 file had.
    /// </summary>
    public sealed class SaveUpgradeTests
    {
        /// <summary>The two members version 2 added, in the canonical (ordinal) order they are written in.</summary>
        private const string RefundMembers = ",\"refundCopper\":0,\"refundSteel\":0";

        // ---------------------------------------------------------------- the version-2 shape, as written by that build

        private static readonly string[] V2State =
        {
            "engineer", "ground", "hand", "ledger", "machines", "next", "rev", "rng", "ruleset", "seed", "stats", "t", "tick", "version",
        };
        private static readonly string[] V2Engineer =
        {
            "aim", "cooldown", "dangerSeconds", "dangerShotSeconds", "dash", "dashCooldown", "dashDir", "down", "downs", "face",
            "hasAim", "hasPack", "hasTarget", "hp", "hurt", "iframes", "inv", "lastHit", "packCounts", "packItems", "pos", "reach",
            "sprint", "stamina", "target", "vel", "walked",
        };
        private static readonly string[] V2Hand = { "craftProg", "crafting", "crafts", "full", "refundCopper", "refundSteel" };
        private static readonly string[] V2Machine = { "dir", "id", "inv", "kind", "out", "rounds", "size", "x", "y" };
        private static readonly string[] V2Stats =
        {
            "chestTrips", "coalBurned", "consumed", "delivered", "engineerFired", "fired", "genFed", "handCrafted", "handFed",
            "handFedCoal", "handFedMags", "made", "magsDelivered", "magsMade", "mined", "minedOf", "placed", "putBack",
            "reachRefused", "repairs", "roundsLost", "spentCopper", "spentSteel", "turretFed",
        };

        /// <summary>
        /// The canonical text of <paramref name="state"/> with only the members a version-2 build wrote, at the places
        /// that build's shape is pinned above; members elsewhere are kept as they are. Member values are emitted by
        /// <see cref="JsonValue.ToCanonicalJson"/> so the result is exactly what the writer produces for that shape.
        /// </summary>
        private static readonly string[] V2MachineStamped = { "dir", "id", "inv", "kind", "out", "rounds", "site", "size", "x", "y" };

        private static readonly Dictionary<string, System.Func<JsonValue, string>> MachineStamps =
            new Dictionary<string, System.Func<JsonValue, string>> { ["site"] = _ => "\"\"" };

        private static string PruneToV2(JsonValue state, bool stampMachines = false)
        {
            return Obj(state, V2State, new Dictionary<string, System.Func<JsonValue, string>>
            {
                ["engineer"] = e => Obj(e, V2Engineer, null),
                ["hand"] = h => Obj(h, V2Hand, null),
                ["stats"] = s => Obj(s, V2Stats, null),
                ["machines"] = ms =>
                {
                    var parts = new List<string>();
                    foreach (var m in ms.Items)
                        parts.Add(stampMachines ? Obj(m, V2MachineStamped, MachineStamps) : Obj(m, V2Machine, null));
                    return "[" + string.Join(",", parts) + "]";
                },
            });
        }

        private static string Obj(JsonValue o, string[] keep, Dictionary<string, System.Func<JsonValue, string>> inner)
        {
            Assert.That(o.IsObject, Is.True);
            var parts = new List<string>();
            foreach (var key in keep)
            {
                var v = o.Member(key);
                Assert.That(v, Is.Not.Null, "the current shape lost member '" + key + "', which a version-2 file carries");
                var text = inner != null && inner.TryGetValue(key, out var f) ? f(v) : v.ToCanonicalJson();
                parts.Add(CanonicalJsonWriter.QuoteString(key) + ":" + text);
            }
            return "{" + string.Join(",", parts) + "}";
        }

        private static int Count(string text, string part)
        {
            var n = 0;
            for (var i = text.IndexOf(part, System.StringComparison.Ordinal); i >= 0; i = text.IndexOf(part, i + 1, System.StringComparison.Ordinal)) n++;
            return n;
        }

        private static string Ver(int v) => v.ToString(System.Globalization.CultureInfo.InvariantCulture);

        /// <summary>
        /// A played state with no refund owed; its current document; the version-2 document the 2026-09-13 build
        /// would have written for it; and the version-1 document the build before that would have written.
        /// </summary>
        private static (Simulation Sim, string Now, string V2, string V1) Forge(int ticks = 300)
        {
            var (sim, _) = Scenarios.ShortRun().Play(ticks);

            // The forged file has to be a shape those builds could actually write. U-D-44 gave the workshop a job
            // queue, reserved ingredients and an output tray; a version-1 or version-2 build had none of the three,
            // so a fixture carrying any of them would be pruned down to a document with items missing from it — the
            // ingredients would simply be gone, and conservation would fail on load through no fault of the upgrade.
            // The short run has batches queued by this tick, so they are cancelled first: that returns every
            // ingredient to the Backpack, which is exactly a shape version 2 wrote.
            if (sim.State.Hand.Jobs.Count > 0)
            {
                var stop = sim.Apply(new CancelCraftCommand());
                Assert.That(stop.Accepted, Is.True, stop.Problem);
            }
            Assert.That(sim.State.Hand.Jobs, Is.Empty, "no workshop queue: a v1/v2 file cannot carry one");
            Assert.That(sim.State.Hand.Reserved.IsEmpty, Is.True, "nor ingredients reserved for one");
            Assert.That(sim.State.Hand.Output.IsEmpty, Is.True, "nor finished goods waiting in the tray");
            Assert.That(sim.State.Hand.RefundSteel, Is.Zero, "the fixture must owe nothing: a v1 file cannot");
            Assert.That(sim.State.Hand.RefundCopper, Is.Zero);

            var now = SaveSerializer.WriteText(sim.State, sim.Context.Data);
            var stateNow = PersistenceFixture.Canonical(sim.State);
            Assert.That(Count(now, RefundMembers), Is.EqualTo(1), "exactly one hand object in the document");
            Assert.That(Count(stateNow, "\"version\":"), Is.EqualTo(1), "the state's own version is the only one inside it");

            var state2 = PruneToV2(JsonValue.Parse(stateNow, out var e)).Replace("\"version\":" + Ver(SaveSchema.Version), "\"version\":2");
            Assert.That(e, Is.Null);
            var v2 = PersistenceFixture.Retarget(now, "state", state2);
            v2 = PersistenceFixture.Retarget(v2, "version", "2");
            v2 = PersistenceFixture.Retarget(v2, "hash", CanonicalJsonWriter.QuoteString(StateHash.Of(state2)));

            var state1 = state2.Replace(RefundMembers, "").Replace("\"version\":2", "\"version\":1");
            var v1 = v2.Replace(RefundMembers, "");
            v1 = PersistenceFixture.RetargetInState(v1, "version", "1");
            v1 = PersistenceFixture.Retarget(v1, "version", "1");
            v1 = PersistenceFixture.Retarget(v1, "hash", CanonicalJsonWriter.QuoteString(StateHash.Of(state1)));
            Assert.That(v1, Is.Not.EqualTo(v2));
            return (sim, now, v2, v1);
        }

        /// <summary>What the old file carried, in the loaded state: the loaded state pruned to the v2 shape.</summary>
        private static string V2View(SimState st) =>
            PruneToV2(JsonValue.Parse(PersistenceFixture.Canonical(st), out _)).Replace("\"version\":" + Ver(SaveSchema.Version), "\"version\":2");

        // ---------------------------------------------------------------- the emitter the upgrade relies on

        /// <summary>
        /// <see cref="JsonValue.ToCanonicalJson"/> must reproduce <see cref="CanonicalJsonWriter"/> byte for byte,
        /// or an old checksum could never be verified: the played state (positions, playtime and every other double)
        /// and the whole document round-trip through parse → emit unchanged, and a hand-written document comes
        /// out in canonical order.
        /// </summary>
        [Test]
        public void TheCanonicalEmitterReproducesTheWriterExactly()
        {
            var (sim, now, _, _) = Forge();
            var state = PersistenceFixture.Canonical(sim.State);
            Assert.That(JsonValue.Parse(state, out var e1).ToCanonicalJson(), Is.EqualTo(state), e1);
            Assert.That(JsonValue.Parse(now, out var e2).ToCanonicalJson(), Is.EqualTo(now), e2);
            Assert.That(StateHash.Of(JsonValue.Parse(now, out _).Member("state").ToCanonicalJson()),
                Is.EqualTo(sim.Hash()), "the emitted state hashes as the writer's does");

            var loose = JsonValue.Parse("{ \"b\" : 1.5 , \"a\" : [ 2 , true , null , \"x\\\"y\\u0001\" ], \"c\": {} }", out var e3);
            Assert.That(loose, Is.Not.Null, e3);
            Assert.That(loose.ToCanonicalJson(), Is.EqualTo("{\"a\":[2,true,null,\"x\\\"y\\u0001\"],\"b\":1.5,\"c\":{}}"));
        }

        /// <summary>The forged fixtures are what they claim: their own checksums verify over their own state text.</summary>
        [Test]
        public void TheForgedFixturesAreInternallyConsistent()
        {
            var (_, now, v2, v1) = Forge();
            foreach (var (doc, version) in new[] { (now, SaveSchema.Version), (v2, 2), (v1, 1) })
            {
                var root = JsonValue.Parse(doc, out var e);
                Assert.That(root, Is.Not.Null, e);
                Assert.That((int)root.Member("version").Number, Is.EqualTo(version));
                Assert.That((int)root.Member("state").Member("version").Number, Is.EqualTo(version));
                Assert.That(StateHash.Of(root.Member("state").ToCanonicalJson()), Is.EqualTo(root.Member("hash").Text), "version " + version);
            }
            Assert.That(JsonValue.Parse(v2, out _).Member("state").Member("hand").Member("refundSteel"), Is.Not.Null);
            Assert.That(JsonValue.Parse(v1, out _).Member("state").Member("hand").Member("refundSteel"), Is.Null);
        }

        // ---------------------------------------------------------------- the fill rule, on its own

        /// <summary>
        /// The v2 → v3 step is one rule: add what is missing with the fresh value, recurse through objects, never
        /// into arrays, never over what is there. Pinned on a synthetic pair so it holds whatever the state shape is.
        /// </summary>
        [Test]
        public void FillingMissingMembersAddsOnlyWhatIsAbsentAndNeverReachesIntoArrays()
        {
            var target = JsonValue.Parse("{\"a\":1,\"o\":{\"x\":5},\"arr\":[{\"id\":1}],\"keep\":[1,2]}", out var e1);
            var fresh = JsonValue.Parse("{\"a\":0,\"b\":\"new\",\"o\":{\"x\":0,\"y\":[]},\"arr\":[{\"id\":0,\"extra\":9}],\"keep\":[],\"n\":{\"p\":{\"q\":true}}}", out var e2);
            Assert.That(target, Is.Not.Null, e1);
            Assert.That(fresh, Is.Not.Null, e2);

            var added = SaveUpgrade.FillMissing(target, fresh);

            Assert.That(added, Is.EqualTo(3), "b, o.y and n");
            Assert.That(target.ToCanonicalJson(),
                Is.EqualTo("{\"a\":1,\"arr\":[{\"id\":1}],\"b\":\"new\",\"keep\":[1,2],\"n\":{\"p\":{\"q\":true}},\"o\":{\"x\":5,\"y\":[]}}"));
            Assert.That(SaveUpgrade.FillMissing(target, fresh), Is.Zero, "a second pass has nothing to add");
        }

        /// <summary>A fresh state's document is a well-formed object: the source every v3 default comes from.</summary>
        [Test]
        public void AFreshStateSerialisesToAnObjectThatUpgradesNothingInACurrentDocument()
        {
            var (sim, now, _, _) = Forge();
            var fresh = JsonValue.Parse(CanonicalJsonWriter.Write(new SimState()), out var e);
            Assert.That(fresh, Is.Not.Null, e);
            Assert.That(fresh.IsObject, Is.True);
            var current = JsonValue.Parse(now, out _).Member("state");
            Assert.That(SaveUpgrade.FillMissing(current, fresh), Is.Zero, "a current document lacks nothing a fresh state has");
            Assert.That(StateHash.Of(current.ToCanonicalJson()), Is.EqualTo(sim.Hash()));
        }

        // ---------------------------------------------------------------- old saves load

        [Test]
        public void AVersionOneSaveLoadsWithEverythingItCarriedAndNoRefundOwed()
        {
            var (sim, _, _, v1) = Forge();
            var r = SaveSerializer.ReadText(v1, sim.Context.Data);

            Assert.That(r.Ok, Is.True, "a v1 file this port wrote must load: " + r.Reason);
            Assert.That(r.Header.Version, Is.EqualTo(1), "the header reports the file as it is");
            Assert.That(r.Upgraded, Does.Contain("version 1"), "the player is told the file was older");
            Assert.That(r.Upgraded, Does.Contain("read as version " + Ver(SaveSchema.Version)));
            Assert.That(r.Upgraded, Does.Contain("no pending hand-craft refund"));
            Assert.That(r.Upgraded, Does.Contain("unchanged"), "and that the file was not touched");
            Assert.That(r.Warning, Is.Null, "an upgrade is information, not a warning");
            Assert.That(r.State.Hand.RefundSteel, Is.Zero);
            Assert.That(r.State.Hand.RefundCopper, Is.Zero);
            Assert.That(r.State.Version, Is.EqualTo(SaveSchema.Version), "in memory it is a current state");
            Assert.That(V2View(r.State), Is.EqualTo(V2View(sim.State)), "every member the old file carried survives exactly");
            Assert.That(SimTestUtil.ConservationProblems(sim.Context, r.State), Is.Empty);
        }

        [Test]
        public void AVersionTwoSaveLoadsWithEverythingItCarriedAndTheNewSystemsAtTheirDefaults()
        {
            var (sim, _, v2, _) = Forge();
            var r = SaveSerializer.ReadText(v2, sim.Context.Data);

            Assert.That(r.Ok, Is.True, "a v2 file this port wrote must load: " + r.Reason);
            Assert.That(r.Header.Version, Is.EqualTo(2));
            Assert.That(r.Upgraded, Does.Contain("version 2"));
            Assert.That(r.Upgraded, Does.Not.Contain("hand-craft refund"), "v2 already carried the refund; the note says only what changed");
            Assert.That(r.Upgraded, Does.Contain("unchanged"));
            Assert.That(r.Warning, Is.Null);
            Assert.That(r.State.Version, Is.EqualTo(SaveSchema.Version));
            Assert.That(V2View(r.State), Is.EqualTo(V2View(sim.State)), "every member the old file carried survives exactly");
            Assert.That(SimTestUtil.ConservationProblems(sim.Context, r.State), Is.Empty);

            // The members the file lacked hold a fresh state's values: the loaded document, pruned back to the v2
            // shape and re-filled from a fresh state, is the loaded document.
            var loaded = JsonValue.Parse(PersistenceFixture.Canonical(r.State), out _);
            // The fill does not reach into list items, so a member added to each machine is stamped at the value the
            // upgrade stamps (v14's site: every machine an old file carried is the player's).
            var pruned = JsonValue.Parse(PruneToV2(loaded, stampMachines: true), out _);
            SaveUpgrade.FillMissing(pruned, JsonValue.Parse(CanonicalJsonWriter.Write(new SimState()), out _));
            Assert.That(pruned.ToCanonicalJson(), Is.EqualTo(loaded.ToCanonicalJson()));
        }

        /// <summary>An upgraded state is deterministic and runs: two loads of the same old file step identically.</summary>
        [Test]
        public void AnUpgradedStateRunsDeterministically()
        {
            var (sim, _, v2, v1) = Forge();
            foreach (var old in new[] { v1, v2 })
            {
                var a = SaveSerializer.ReadText(old, sim.Context.Data);
                var b = SaveSerializer.ReadText(old, sim.Context.Data);
                Assert.That(a.Ok, Is.True, a.Reason);
                var copyA = Simulation.Wrap(sim.Context, a.State);
                var copyB = Simulation.Wrap(sim.Context, b.State);
                SimTestUtil.Step(copyA, 200);
                SimTestUtil.Step(copyB, 200);
                Assert.That(copyA.Hash(), Is.EqualTo(copyB.Hash()), "two loads of the same old file diverged");
                Assert.That(SimTestUtil.ConservationProblems(sim.Context, copyA.State), Is.Empty);
            }
        }

        [Test]
        public void ASaveMadeAfterAnUpgradeIsAnOrdinaryCurrentFile()
        {
            var (sim, _, _, v1) = Forge();
            var r = SaveSerializer.ReadText(v1, sim.Context.Data);
            Assert.That(r.Ok, Is.True, r.Reason);

            var again = SaveSerializer.WriteText(r.State, sim.Context.Data, "2026-09-13T00:00:00.0000000Z");
            var header = SaveSerializer.ReadHeader(System.Text.Encoding.UTF8.GetBytes(again), out var problem);
            Assert.That(header, Is.Not.Null, problem);
            Assert.That(header.Version, Is.EqualTo(SaveSchema.Version));
            Assert.That(again, Does.Contain(RefundMembers));
            Assert.That(again, Does.Contain("\"version\":" + Ver(SaveSchema.Version) + "}"), "the state's own version is current too");
            var back = SaveSerializer.ReadText(again, sim.Context.Data);
            Assert.That(back.Ok, Is.True, back.Reason);
            Assert.That(back.Upgraded, Is.Null, "the second load needs no upgrade");
            Assert.That(StateHash.Compute(back.State), Is.EqualTo(StateHash.Compute(r.State)));
        }

        [Test]
        public void OldHeadersAreListable()
        {
            var (_, _, v2, v1) = Forge();
            var h1 = SaveSerializer.ReadHeader(System.Text.Encoding.UTF8.GetBytes(v1), out var p1);
            Assert.That(h1, Is.Not.Null, p1);
            Assert.That(h1.Version, Is.EqualTo(1));
            var h2 = SaveSerializer.ReadHeader(System.Text.Encoding.UTF8.GetBytes(v2), out var p2);
            Assert.That(h2, Is.Not.Null, p2);
            Assert.That(h2.Version, Is.EqualTo(2));
        }

        // ---------------------------------------------------------------- the file is preserved

        /// <summary>
        /// Loading through the store must leave the old file byte for byte as it was: no rewrite, no <c>.corrupt</c>,
        /// no <c>.bak</c>. The slot still lists, and the next save keeps the original as the <c>.bak</c>.
        /// </summary>
        [Test]
        public void LoadingAnOldSlotLeavesTheFileUntouchedAndTheNextSaveKeepsItAsTheBackup()
        {
            var (sim, _, v2, v1) = Forge();
            foreach (var (old, version) in new[] { (v1, 1), (v2, 2) })
            {
                var (fs, store) = PersistenceFixture.Store();
                var path = store.PathOf("old");
                fs.PutText(path, old);

                var r = store.Load("old", sim.Context.Data);
                Assert.That(r.Ok, Is.True, r.Reason);
                Assert.That(r.Upgraded, Is.Not.Null);
                Assert.That(fs.Text(path), Is.EqualTo(old), "the load rewrote the file");
                Assert.That(fs.AllPaths().Count, Is.EqualTo(1), "the load created or renamed something: " + string.Join(", ", fs.AllPaths()));
                Assert.That(fs.Has(path), Is.True);

                var list = store.List();
                Assert.That(list.Count, Is.EqualTo(1));
                Assert.That(list[0].Ok, Is.True, list[0].Problem);
                Assert.That(list[0].Header.Version, Is.EqualTo(version));

                Assert.That(store.Save("old", r.State, sim.Context.Data).Ok, Is.True);
                Assert.That(fs.Text(path + AtomicWrite.BackupSuffix), Is.EqualTo(old), "the original survives as the .bak");
                var header = SaveSerializer.ReadHeader(fs.Bytes(path), out var problem);
                Assert.That(header, Is.Not.Null, problem);
                Assert.That(header.Version, Is.EqualTo(SaveSchema.Version));
            }
        }

        [Test]
        public void AnOldAutosaveIsReadInTheRingAndNotTreatedAsRubbish()
        {
            var (sim, _, _, v1) = Forge();
            var (fs, store) = PersistenceFixture.Store();
            var slot1 = Path.Combine(store.Autosaves.Directory, AutosaveStore.SlotFile(1));
            fs.PutText(slot1, v1);

            var list = store.Autosaves.ListRecoverable();
            Assert.That(list.Count, Is.EqualTo(1));
            Assert.That(string.IsNullOrEmpty(list[0].Problem), Is.True, "a v1 autosave has a readable header: " + list[0].Problem);
            Assert.That(store.Autosaves.ChooseSlot(store.Autosaves.ReadIndex(), 3), Is.EqualTo(AutosaveStore.SlotFile(2)),
                "an empty slot is taken before a v1 autosave, which is a working save");

            var r = store.Autosaves.LoadNewest(sim.Context.Data);
            Assert.That(r.Ok, Is.True, r.Reason);
            Assert.That(r.Upgraded, Is.Not.Null);
            Assert.That(fs.Text(slot1), Is.EqualTo(v1));
        }

        // ---------------------------------------------------------------- what is refused

        /// <summary>The old checksum is verified over the document as written, before anything is defaulted.</summary>
        [Test]
        public void ATamperedOldSaveIsRefusedAsDamagedNotUpgraded()
        {
            var (sim, _, v2, v1) = Forge();
            foreach (var old in new[] { v1, v2 })
            {
                var tampered = PersistenceFixture.RetargetInState(old, "seed", "999999");
                var r = SaveSerializer.ReadText(tampered, sim.Context.Data);
                Assert.That(r.Ok, Is.False);
                Assert.That(r.Reason, Does.Contain("do not match its checksum"));
                Assert.That(r.Damaged, Is.True);
            }
        }

        [Test]
        public void AnOldSaveWithNoChecksumIsRefused()
        {
            var (sim, _, v2, v1) = Forge();
            foreach (var old in new[] { v1, v2 })
            {
                var r = SaveSerializer.ReadText(PersistenceFixture.Retarget(old, "hash", "\"\""), sim.Context.Data);
                Assert.That(r.Ok, Is.False);
                Assert.That(r.Reason, Does.Contain("integrity check"));
            }
        }

        /// <summary>A file that calls itself version 1 but carries version-2 members is not honest; it is left alone.</summary>
        [Test]
        public void AFileThatClaimsVersionOneButCarriesTheRefundFieldsIsRefusedAndLeftAlone()
        {
            var (sim, _, v2, _) = Forge();
            var mislabelled = PersistenceFixture.Retarget(v2, "version", "1");
            var r = SaveSerializer.ReadText(mislabelled, sim.Context.Data);
            Assert.That(r.Ok, Is.False);
            Assert.That(r.Reason, Does.Contain("version 1"));
            Assert.That(r.Reason, Does.Contain("not upgraded"));
            Assert.That(r.Damaged, Is.False, "nothing is recovered from a file that is simply not what it says");

            var (fs, store) = PersistenceFixture.Store();
            fs.PutText(store.PathOf("odd"), mislabelled);
            Assert.That(store.Load("odd", sim.Context.Data).Ok, Is.False);
            Assert.That(fs.AllPaths().Count, Is.EqualTo(1), "left exactly where it was: " + string.Join(", ", fs.AllPaths()));
            Assert.That(fs.Has(store.PathOf("odd")), Is.True);
        }

        [Test]
        public void VersionZeroAndTheFutureAreStillRefusedByNumber()
        {
            var (sim, now, _, _) = Forge();
            var zero = SaveSerializer.ReadText(PersistenceFixture.Retarget(now, "version", "0"), sim.Context.Data);
            Assert.That(zero.Ok, Is.False);
            Assert.That(zero.Reason, Does.Contain("unsupported save version 0"));
            Assert.That(zero.Reason, Does.Contain("upgrades version 1"), "the message says what it can read");
            var next = Ver(SaveSchema.Version + 1);
            var future = SaveSerializer.ReadText(PersistenceFixture.Retarget(now, "version", next), sim.Context.Data);
            Assert.That(future.Ok, Is.False);
            Assert.That(future.Reason, Does.Contain("unsupported save version " + next));
            Assert.That(future.Damaged, Is.False);
        }
    }
}
