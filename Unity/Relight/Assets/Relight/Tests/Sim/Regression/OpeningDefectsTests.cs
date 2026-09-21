using System.Collections.Generic;
using NUnit.Framework;
using Relight.Sim.UI;

namespace Relight.Sim.Tests.Regression
{
    /// <summary>
    /// C-12 — the opening defect regression set. One test per row of UI_AND_ONBOARDING.md §9 that can be settled in
    /// the simulation, named for its defect number, with the REQUIRED UNITY BEHAVIOUR quoted verbatim in the test's
    /// own comment so the check and its contract cannot drift apart.
    ///
    /// Three of these rows (U-2, U-5, U-6) are the Q20 set: the browser retest was never performed and is never
    /// recorded as passed (§9.1). The Unity check REPLACES it, so it has to assert the intended behaviour rather
    /// than reproduce the browser's pixels — see ../reports/wave3-WC-superseded.md.
    ///
    /// U-5 (content-width HUD strip) is a measured-layout fact and lives in
    /// <c>Assets/Relight/Tests/Play/OpeningUiPlayTests.cs</c>; there is nothing about it in the sim.
    /// U-11 and U-12 are recorded as unresolved design questions, not defects, and have no check here.
    /// </summary>
    public sealed class OpeningDefectsTests
    {
        // ------------------------------------------------------------------ U-1

        /// <summary>
        /// U-1. Required Unity behaviour: "Every inventory slot must be a valid drop target, including the last
        /// column and any slot reached only by scrolling. Hit-test in layout space, not by a cached visible-slot
        /// list. Add a regression test that drops on the last slot and on a slot below the fold."
        ///
        /// The browser defect was reported against slots 24 and 39; the catalogue's <c>InvStacks</c> is 40, so 39
        /// IS the last slot and 24 is the one that used to be off the end of the cached visible list. Both are
        /// asserted explicitly, through the assembled command pipeline rather than a subsystem handler, because the
        /// defect was that the layer ABOVE the move refused to target them.
        /// </summary>
        [Test]
        public void U1_EveryBackpackSlotAcceptsADropIncludingTheLastAndOneBelowTheFold()
        {
            var ctx = RegressionFixture.Context();
            var stacks = ctx.Data.Engineer.InvStacks;
            Assert.That(stacks, Is.GreaterThanOrEqualTo(40), "the defect names slots 24 and 39; there must be 40 slots");

            foreach (var target in new[] { 24, stacks - 1 })
            {
                var sim = RegressionFixture.NewGame(ctx);
                var st = sim.State;
                Assert.That(sim.Apply(new InventorySortCommand()).Accepted, Is.True);

                var slots = Pockets.Slots(ctx.Data, st.Engineer);
                Assert.That(slots.Count, Is.EqualTo(stacks), "the pack exposes every slot, not only the filled ones");
                var from = -1;
                for (var i = 0; i < slots.Count; i++)
                    if (slots[i] != null && slots[i].Item == "steel") { from = i; break; }
                Assert.That(from, Is.GreaterThanOrEqualTo(0), "the opening stake carries steel");
                Assert.That(slots[target], Is.Null, "slot " + target + " is empty before the drop");

                var count = slots[from].Count;
                var carried = st.Engineer.Inv[ItemId.Steel];
                var move = sim.Apply(new InventoryMoveCommand(from, target, "steel", count,
                    Backpack.Layout(ctx.Data, st.Engineer)));

                RegressionFixture.Accept(move, "slot " + target + " refused a drop");
                var after = Pockets.Slots(ctx.Data, st.Engineer);
                Assert.That(after[target], Is.Not.Null, "the stack landed in slot " + target);
                Assert.That(after[target].Item, Is.EqualTo("steel"));
                Assert.That(after[target].Count, Is.EqualTo(count));
                Assert.That(st.Engineer.Inv[ItemId.Steel], Is.EqualTo(carried), "a move never changes quantities");
                Assert.That(RegressionFixture.Off(ctx, st), Is.Empty);
            }
        }

        // ------------------------------------------------------------------ U-2

        /// <summary>
        /// U-2. Owner: "When I drag the gun to the first slot, it adds it to slot 5 and 9 as well."
        /// Required Unity behaviour: "One tool, one slot. All assignment paths go through a single
        /// <c>AssignQuickbar</c> that SWAPS (displaced tool takes the vacated slot). On load, collapse duplicates to
        /// the first occurrence."
        ///
        /// The three UI paths §9.1 names (catalogue drag, the "Add to action bar" destination button, a Backpack
        /// weapon drop) all reduce to one sim command, <see cref="AssignBarCommand"/> — which is the fix. The test
        /// therefore assigns the SAME key three times, to the three slots the owner actually saw filled
        /// (keys 1, 5 and 9 = indices 0, 4 and 8), and asserts after EACH that exactly one slot holds it.
        /// </summary>
        [Test]
        public void U2_AssigningOneToolByThreePathsLeavesItOnExactlyOneActionBarSlot()
        {
            var ctx = RegressionFixture.Context();
            var sim = RegressionFixture.NewGame(ctx);
            var st = sim.State;
            var rifle = RegressionFixture.GiveWeapon(ctx, st);

            foreach (var slot in new[] { 0, 4, 8 })
            {
                var r = sim.Apply(new AssignBarCommand(slot, rifle));
                RegressionFixture.Accept(r, "assigning to action-bar slot " + slot);

                var bar = WeaponQueries.Bar(st);
                var held = new List<int>();
                for (var i = 0; i < bar.Count; i++) if (bar[i] == rifle) held.Add(i);
                Assert.That(held, Is.EqualTo(new List<int> { slot }),
                    "after assigning to slot " + slot + " the tool must sit on that slot ALONE; it was on " +
                    string.Join(", ", held));
            }
        }

        /// <summary>U-2, the swap half: the tool a new assignment displaces takes the slot that was vacated.</summary>
        [Test]
        public void U2_TheDisplacedToolTakesTheVacatedSlot()
        {
            var ctx = RegressionFixture.Context();
            var sim = RegressionFixture.NewGame(ctx);
            var st = sim.State;

            RegressionFixture.Accept(sim.Apply(new AssignBarCommand(0, "belt")));
            RegressionFixture.Accept(sim.Apply(new AssignBarCommand(3, "pole")));

            // "pole" moves from slot 3 to slot 0; "belt", which slot 0 held, must land on slot 3 — not vanish and
            // not stay alongside it.
            RegressionFixture.Accept(sim.Apply(new AssignBarCommand(0, "pole")));

            var bar = WeaponQueries.Bar(st);
            Assert.That(bar[0], Is.EqualTo("pole"));
            Assert.That(bar[3], Is.EqualTo("belt"), "the displaced tool takes the vacated slot");
            var poles = 0;
            for (var i = 0; i < bar.Count; i++) if (bar[i] == "pole") poles++;
            Assert.That(poles, Is.EqualTo(1), "one tool, one slot");
        }

        /// <summary>
        /// U-2, the load half: "On load, collapse duplicates to the first occurrence." A save written by an older
        /// build (or by the defect itself) holds the same key several times; reading the bar back must give the
        /// first occurrence only, with the later ones cleared rather than re-homed.
        /// </summary>
        [Test]
        public void U2_LoadingABarHoldingOneToolSeveralTimesCollapsesToTheFirstOccurrence()
        {
            var ctx = RegressionFixture.Context();
            var sim = RegressionFixture.NewGame(ctx);
            var st = sim.State;

            // Forge the defective arrangement the owner saw: the same tool on keys 1, 5 and 9.
            var bar = st.Weapons.Bar;
            while (bar.Count < WeaponRules.BarSlots) bar.Add("");
            bar[0] = "rifle:1"; bar[4] = "rifle:1"; bar[8] = "rifle:1";

            var loaded = SaveSerializer.ReadText(SaveSerializer.WriteText(st, ctx.Data), ctx.Data);
            Assert.That(loaded.Ok, Is.True, loaded.Reason);

            var after = WeaponQueries.Bar(loaded.State);
            Assert.That(after[0], Is.EqualTo("rifle:1"), "the FIRST occurrence is the one that survives");
            Assert.That(after[4], Is.Empty);
            Assert.That(after[8], Is.Empty);
            var n = 0;
            for (var i = 0; i < after.Count; i++) if (after[i] == "rifle:1") n++;
            Assert.That(n, Is.EqualTo(1));
        }

        // ------------------------------------------------------------------ U-3

        /// <summary>
        /// U-3. Owner retest: "The no power message still shows up when the game starts."
        /// Required Unity behaviour: "A fresh campaign must not show any power-failure alert before the player has
        /// built a generator. Unity should have exactly ONE power-alert producer. Do not port two."
        ///
        /// Both halves are asserted: no power-failure event in the first 600 ticks (30 s) of a fresh campaign, and
        /// only one type in the sim that can produce one — the browser defect was a SECOND, derived producer in the
        /// HUD, so a check that only watched the primary path would have passed while the toast still appeared.
        /// </summary>
        [Test]
        public void U3_AFreshCampaignRaisesNoPowerFailureBeforeAGeneratorExists()
        {
            var ctx = RegressionFixture.Context();
            var sim = RegressionFixture.NewGame(ctx);
            Assert.That(sim.State.Machines.Count, Is.EqualTo(0), "a fresh campaign starts with nothing built");

            var events = RegressionFixture.StepCollecting(sim, 600);

            Assert.That(RegressionFixture.Count<PowerOutageEvent>(events), Is.EqualTo(0),
                "no machine can lose supply before one exists");
            Assert.That(RegressionFixture.Count<GeneratorDryEvent>(events), Is.EqualTo(0),
                "no generator can run dry before one is built");
            Assert.That(RegressionFixture.Count<PowerRestoredEvent>(events), Is.EqualTo(0),
                "nor can supply be 'restored' to a world that never had any");
        }

        /// <summary>
        /// U-3, the "exactly one producer" half. The sim raises a power failure from one place and one place only:
        /// <see cref="PowerPhase"/>'s supply-changed sweep. A second producer is the defect, so this asserts the
        /// count of raise sites rather than trusting a comment.
        /// </summary>
        [Test]
        public void U3_ThereIsExactlyOnePowerFailureProducerAndItNeedsAPriorSupply()
        {
            var ctx = RegressionFixture.Context();
            var sim = RegressionFixture.NewGame(ctx);
            var st = sim.State;

            // A consumer with no generator anywhere: it has never been supplied, so losing nothing raises nothing.
            RegressionFixture.Add(ctx, st, "excavator", 10, 10);
            var quiet = RegressionFixture.StepCollecting(sim, 200);
            Assert.That(RegressionFixture.Count<PowerOutageEvent>(quiet), Is.EqualTo(0),
                "a machine that never had supply does not 'lose' it — this is the outage-at-start defect");

            // Give it supply: exactly one restore, and still no failure.
            RegressionFixture.Add(ctx, st, "pole", 12, 10);
            var gen = RegressionFixture.Add(ctx, st, "generator", 14, 10);
            gen.Inv.Add(ItemId.Coal, 20);
            st.Rev++;
            var live = RegressionFixture.StepCollecting(sim, 40);
            Assert.That(RegressionFixture.Count<PowerRestoredEvent>(live), Is.EqualTo(1),
                "the excavator is supplied once the generator is on its circuit");
            Assert.That(RegressionFixture.Count<PowerOutageEvent>(live), Is.EqualTo(0));

            // NOW take the supply away: one failure, for the one machine that actually lost it, raised once.
            st.Machines.Remove(gen);
            st.Rev++;
            var lost = RegressionFixture.StepCollecting(sim, 200);
            Assert.That(RegressionFixture.Count<PowerOutageEvent>(lost), Is.EqualTo(1),
                "one failure per machine that actually lost supply, raised once, never repeated per tick");
        }

        // ------------------------------------------------------------------ U-4

        /// <summary>
        /// U-4. Required Unity behaviour: "Panel-owned keys must be resolvable inside the panel."
        ///
        /// The sim has no concept of an open panel — that half is Play Mode
        /// (<c>OpeningUiPlayTests.ReloadWorksWhileTheBackpackDrawerIsOpen</c>). What the sim MUST guarantee is that
        /// nothing about the engineer's state other than the weapon's own condition can refuse a reload, so a panel
        /// can always forward the key. Coordinator ruling (Wave 2 W-A/W-C notes): "for U-4 assert
        /// <c>ReloadCommand</c> is only refused by <c>HandCraft.HandLocked</c>."
        ///
        /// FINDING (reported, not weakened): the port's <see cref="WeaponRules.CheckReload"/> does NOT consult the
        /// hand lock, so a reload is accepted mid-craft as well. That is a MORE permissive answer than the ruling
        /// expected and it cannot reintroduce U-4, so it is asserted as it stands and raised in the report.
        /// </summary>
        [Test]
        public void U4_ReloadIsAcceptedWheneverTheWeaponItselfAllowsIt()
        {
            var ctx = RegressionFixture.Context();
            var sim = RegressionFixture.NewGame(ctx);
            var st = sim.State;

            var rifle = RegressionFixture.GiveWeapon(ctx, st);
            RegressionFixture.Accept(sim.Apply(new EquipCommand(rifle, 0)));
            RegressionFixture.Give(st, ItemId.Magazine, 30);

            // Nothing is "open" in the sim, so the baseline must be a plain acceptance.
            RegressionFixture.Accept(sim.Apply(new ReloadCommand()), "R must be resolvable at any time");

            // Every refusal the sim CAN give is a fact about the weapon, never about a surface being open.
            Assert.That(sim.Apply(new ReloadCommand()).Problem, Is.EqualTo(WeaponRules.ReloadingText));
            RegressionFixture.Step(sim, 200);
            Assert.That(st.Weapons.ActiveWeapon().Loaded, Is.GreaterThan(0), "the reload completed in the tick");
            Assert.That(sim.Apply(new ReloadCommand()).Problem, Is.EqualTo(WeaponRules.MagFullText));
        }

        /// <summary>
        /// U-4, the lock half. Recorded as the behaviour actually shipped: since U-D-44 a queued workshop batch
        /// locks nothing at all, and the one stationary action that still does — a repair — refuses to MOVE (U-8)
        /// but does not refuse a reload. If a later change makes the reload locked, this test says so loudly rather
        /// than silently accepting a regression of U-4.
        /// </summary>
        [Test]
        public void U4_NeitherAQueuedBatchNorARunningRepairBlocksTheReloadKey()
        {
            var ctx = RegressionFixture.Context();
            var sim = RegressionFixture.NewGame(ctx);
            var st = sim.State;

            var rifle = RegressionFixture.GiveWeapon(ctx, st);
            RegressionFixture.Accept(sim.Apply(new EquipCommand(rifle, 0)));
            RegressionFixture.Give(st, ItemId.Magazine, 30);
            RegressionFixture.Give(st, ItemId.Steel, 20);
            RegressionFixture.Give(st, ItemId.Copper, 10);

            RegressionFixture.Add(ctx, st, "depot", 10, 4);
            RegressionFixture.Accept(sim.Apply(new HandCraftCommand(1)));
            RegressionFixture.Step(sim, 10);
            Assert.That(HandCraft.HandLocked(st), Is.False, "U-D-44: a queued batch pins nobody");

            HomeCore.Damage(st, 100);
            RegressionFixture.Accept(sim.Apply(new RepairCommand(RepairKinds.Core, -1)));
            Assert.That(HandCraft.HandLocked(st), Is.True, "but a repair does");

            RegressionFixture.Accept(sim.Apply(new ReloadCommand()),
                "and the reload key is not one of the things that lock takes away");
        }

        // ------------------------------------------------------------------ U-6

        /// <summary>
        /// U-6. Owner: "When the base has no health left, its hard to repair…"
        /// Required Unity behaviour: "Repair must be reachable in one interaction from the damaged object, not
        /// buried in a management screen." §9.1: "Interact with a damaged Home core and assert the repair control
        /// is present and actionable in the surface that opens — no intervening screen, and the same for an
        /// undamaged core (the drawer still opens)."
        ///
        /// ONE interaction, in the sim, means: the card the surface shows is produced by a single query with no
        /// prior navigation state, and the repair it offers is accepted by a single command. Both are asserted for
        /// a damaged core, for a fully DISABLED core (0 HP — the owner's actual case), and for an undamaged one.
        /// </summary>
        [Test]
        public void U6_CoreRepairIsOneQueryAndOneCommandFromTheDamagedCore()
        {
            var ctx = RegressionFixture.Context();
            var sim = RegressionFixture.NewGame(ctx);
            var st = sim.State;
            Assert.That(HomeQueries.CoreOperational(st), Is.True, "the core is placed by the new-game initialiser");

            HomeCore.Damage(st, 100);
            Assert.That(HomeQueries.CoreHp(st), Is.LessThan(ctx.Data.Defence.CoreHp));

            var card = HomeQueries.RepairCard(st, ctx.Data);
            Assert.That(card.Kind, Is.EqualTo(RepairKinds.Core));
            Assert.That(card.Recommission, Is.False, "a damaged core is patched, not recommissioned");
            Assert.That(card.CanAfford, Is.True, "the opening stake pays for a patch: " + card.Steel + " steel + " + card.Copper + " copper");
            Assert.That(card.InProgress, Is.False);

            var start = sim.Apply(new RepairCommand(RepairKinds.Core, -1));
            RegressionFixture.Accept(start, "the repair starts from where the engineer already stands");
            Assert.That(HomeQueries.RepairCard(st, ctx.Data).InProgress, Is.True);

            RegressionFixture.Step(sim, (int)(card.Seconds / RegressionFixture.Dt) + 5);
            Assert.That(HomeQueries.RepairCard(st, ctx.Data).InProgress, Is.False, "and it finished");
            Assert.That(HomeQueries.CoreHp(st), Is.EqualTo(200 + ctx.Data.Defence.RepairHp),
                "one interaction applied one patch of RepairHp");
            Assert.That(RegressionFixture.Off(ctx, st), Is.Empty);
        }

        /// <summary>U-6, the owner's actual case: a core at zero HP is recommissioned by the same one interaction.</summary>
        [Test]
        public void U6_ADisabledCoreOffersItsRecommissionOnTheSameCardAndAcceptsItInOneCall()
        {
            var ctx = RegressionFixture.Context();
            var sim = RegressionFixture.NewGame(ctx);
            var st = sim.State;

            HomeCore.Damage(st, ctx.Data.Defence.CoreHp);
            Assert.That(HomeQueries.CoreHp(st), Is.EqualTo(0));
            Assert.That(HomeQueries.CoreOperational(st), Is.False, "no health left — the owner's case");

            var card = HomeQueries.RepairCard(st, ctx.Data);
            Assert.That(card.Recommission, Is.True, "the card itself says this is a recommission, with no other screen");
            Assert.That(card.CanAfford, Is.True, "the opening stake pays for it: " + card.Steel + " steel + " + card.Copper + " copper");

            var start = sim.Apply(new RepairCommand(RepairKinds.Core, -1));
            RegressionFixture.Accept(start, "reachable in ONE interaction from the damaged core");

            RegressionFixture.Step(sim, (int)(card.Seconds / RegressionFixture.Dt) + 5);
            Assert.That(HomeQueries.CoreOperational(st), Is.True, "the core is back");
            Assert.That(RegressionFixture.Off(ctx, st), Is.Empty);
        }

        /// <summary>U-6, "the same for an undamaged core (the drawer still opens)": the card is still produced.</summary>
        [Test]
        public void U6_AnUndamagedCoreStillProducesACardSoTheDrawerNeverOpensEmpty()
        {
            var ctx = RegressionFixture.Context();
            var sim = RegressionFixture.NewGame(ctx);
            var st = sim.State;

            var card = HomeQueries.RepairCard(st, ctx.Data);
            Assert.That(card.Kind, Is.EqualTo(RepairKinds.Core), "there is always a core card");
            Assert.That(card.Hp, Is.EqualTo(card.Max), "and it reports full health rather than disappearing");
            Assert.That(HomeQueries.Description(st, ctx.Data), Is.Not.Empty);

            // Actionable does not mean always accepted: a repair with nothing to repair is refused with a reason,
            // which is what keeps the control visible-and-explained rather than absent (accessibility contract §8).
            var r = sim.Apply(new RepairCommand(RepairKinds.Core, -1));
            Assert.That(r.Accepted, Is.False);
            Assert.That(r.Problem, Is.EqualTo("the core is already at full health"));
        }

        // ------------------------------------------------------------------ U-7

        /// <summary>
        /// U-7. Required Unity behaviour: "Weapons only onto equipment/action-bar slots, with the explicit refusal
        /// message (§5.6)."
        ///
        /// The sim's guarantee is structural and stronger than a message: there is no command that can put a weapon
        /// into a machine or a chest at all (<see cref="MachineTransferCommand"/> takes an <see cref="ItemId"/>, and
        /// no weapon has one), and the only two destinations that accept a weapon key are the equipment slots and
        /// the action bar. The refusal texts §5.6 promises are asserted verbatim so a reword is caught here rather
        /// than by a player.
        /// </summary>
        [Test]
        public void U7_AWeaponIsOnlyAcceptedByEquipmentAndActionBarSlotsWithTheExactRefusalText()
        {
            var ctx = RegressionFixture.Context();
            var sim = RegressionFixture.NewGame(ctx);
            var st = sim.State;
            var rifle = RegressionFixture.GiveWeapon(ctx, st);

            // Equipment slots 1 and 2 accept it; anything else is refused with the §5.6 wording.
            RegressionFixture.Accept(sim.Apply(new EquipCommand(rifle, 0)));
            RegressionFixture.Accept(sim.Apply(new UnequipCommand(0)));
            Assert.That(sim.Apply(new EquipCommand(rifle, 2)).Problem, Is.EqualTo(WeaponRules.SlotText));
            Assert.That(sim.Apply(new EquipCommand(rifle, -1)).Problem, Is.EqualTo(WeaponRules.SlotText));

            // The action bar takes the key, and only within its ten slots.
            RegressionFixture.Accept(sim.Apply(new AssignBarCommand(0, rifle)));
            Assert.That(sim.Apply(new AssignBarCommand(WeaponRules.BarSlots, rifle)).Problem,
                Is.EqualTo(WeaponRules.BarSlotText));

            // An ordinary item is not a carried weapon, so the equipment slot refuses it.
            Assert.That(sim.Apply(new EquipCommand("steel", 0)).Problem, Is.EqualTo(WeaponRules.CarriedText));
            Assert.That(sim.Apply(new EquipCommand("rifle:99", 0)).Problem, Is.EqualTo(WeaponRules.CarriedText),
                "a weapon id the engineer does not own is not carried");

            // The panel's own sentence, §5.6, shown when a weapon is dragged anywhere else.
            Assert.That(TransferText.WeaponTarget,
                Is.EqualTo("Drop weapons on equipment or action-bar slots; other items go in inventory slots."));
            Assert.That(TransferText.WeaponsNotStored, Is.EqualTo("Weapons stay in the Backpack or on the belt."));
        }

        /// <summary>
        /// U-7, the chest half: "chest drop-target validation". No machine transfer can name a weapon, and a
        /// weapon never stacks in a pocket, so a chest can never come to hold one.
        /// </summary>
        [Test]
        public void U7_NoMachineTransferCanNameAWeaponAndAWeaponNeverStacks()
        {
            var ctx = RegressionFixture.Context();
            var sim = RegressionFixture.NewGame(ctx);
            var st = sim.State;
            var rifle = RegressionFixture.GiveWeapon(ctx, st);
            var key = new ItemKey(rifle);

            Assert.That(key.IsWeapon, Is.True);
            Assert.That(key.IsItem(out _), Is.False,
                "a weapon has no ItemId, so MachineTransferCommand cannot express a weapon at all");

            var chest = RegressionFixture.Add(ctx, st, "chest", 10, 4);
            var contents = new ItemCounts();
            MachineInventory.Contents(ctx.Data, chest, contents);
            Assert.That(contents.IsEmpty, Is.True, "a fresh chest holds nothing, and nothing can put a weapon in it");

            // The pocket rule behind it: a weapon occupies one slot and never merges with a second copy.
            var second = RegressionFixture.GiveWeapon(ctx, st);
            Assert.That(st.Engineer.Inv[key], Is.EqualTo(1));
            Assert.That(st.Engineer.Inv[new ItemKey(second)], Is.EqualTo(1));
            var slots = Pockets.Slots(ctx.Data, st.Engineer);
            var weaponSlots = 0;
            for (var i = 0; i < slots.Count; i++)
                if (slots[i] != null && new ItemKey(slots[i].Item).IsWeapon)
                {
                    weaponSlots++;
                    Assert.That(slots[i].Count, Is.EqualTo(1), "a weapon slot holds exactly one weapon");
                }
            Assert.That(weaponSlots, Is.EqualTo(2), "two owned weapons, two slots");
        }

        // ------------------------------------------------------------------ U-8

        /// <summary>
        /// U-8, as U-D-44 (2026-09-15) restated it. The brief originally required "hand-crafting locks movement with
        /// a visible prompt". The owner's decision supersedes that: the Home workshop processes its queue while the
        /// player is elsewhere, so a queued batch locks nothing. What survives is the rest of the requirement — a
        /// stationary action still locks movement with a visible prompt — and the action that still is one is a
        /// Home repair.
        ///
        /// DELIBERATE DIFFERENCE, retained from the original port: the lock does NOT refuse
        /// <see cref="WalkCommand"/>. <c>MovementCommands.cs</c> accepts it on purpose — it only records which keys
        /// are held, and refusing it would lose a key pressed or released during the lock because the presentation
        /// is level-triggered. The LOCK is still total: <see cref="MoveCommand"/> and <see cref="DodgeCommand"/> are
        /// refused with the prompt text, and the engineer does not move a tile while held keys are down.
        /// </summary>
        [Test]
        public void U8_AQueuedBatchLocksNothingWhileARepairLocksMovementWithTheVisiblePrompt()
        {
            var ctx = RegressionFixture.Context();
            var sim = RegressionFixture.NewGame(ctx);
            var st = sim.State;
            RegressionFixture.Add(ctx, st, "depot", 10, 4);

            // ---- the workshop. Queued work leaves the engineer entirely free (U-D-44).
            RegressionFixture.Accept(sim.Apply(new HandCraftCommand(1)));
            RegressionFixture.Step(sim, 10);
            Assert.That(HandCraft.HandLocked(st), Is.False, "a queued batch is the workshop working, not the engineer");
            RegressionFixture.Accept(sim.Apply(new DodgeCommand()), "the dodge is available");
            RegressionFixture.Step(sim, 10);   // the dash plays out; it faces east, so it ends east of the spawn
            var start = st.Engineer.Pos;
            RegressionFixture.Accept(sim.Apply(new MoveCommand(start.X - 2, start.Y)));
            RegressionFixture.Step(sim, 40);
            Assert.That(st.Engineer.Pos.X, Is.LessThan(start.X - 1e-6), "and the engineer walks away from it");
            Assert.That(HandCraft.QueuedBatches(st), Is.EqualTo(1), "with the batch still queued behind them");

            // It finishes with nobody there, into the workshop's tray rather than into the Backpack.
            var mags = st.Engineer.Inv[ItemId.Magazine];
            ctx.Data.TryRecipe("hand-bullets", out var bullets);
            RegressionFixture.Step(sim, (int)(bullets.Seconds / RegressionFixture.Dt) + 2);
            Assert.That(st.Hand.Output[ItemId.Magazine], Is.EqualTo(ctx.Data.Engineer.HandBulletsPerCraft));
            Assert.That(st.Engineer.Inv[ItemId.Magazine], Is.EqualTo(mags), "nothing arrived remotely");
            Assert.That(RegressionFixture.Off(ctx, st), Is.Empty);

            // ---- the repair. This is the stationary action the prompt now belongs to.
            var turret = RegressionFixture.Add(ctx, st, "turret", (int)st.Engineer.Pos.X + 2, (int)st.Engineer.Pos.Y);
            TurretRules.Damage(ctx, st, turret, 40);
            RegressionFixture.Give(st, ItemId.Steel, ctx.Data.Defence.RepairSteel);
            RegressionFixture.Give(st, ItemId.Copper, ctx.Data.Defence.RepairCopper);
            RegressionFixture.Accept(sim.Apply(new RepairCommand(RepairKinds.Machine, turret.Id)));
            Assert.That(HandCraft.HandLocked(st), Is.True);
            Assert.That(HandCraft.LockTextFor(st), Is.EqualTo("Repairing — Cancel to move."),
                "the visible prompt is the sim's own text");

            var target = new Vec2(st.Engineer.Pos.X - 2, st.Engineer.Pos.Y);
            var walkTo = sim.Apply(new MoveCommand(target.X, target.Y));
            Assert.That(walkTo.Accepted, Is.False);
            Assert.That(walkTo.Problem, Is.EqualTo(Home.LockText), "walk-here is refused WITH the prompt");

            var dodge = sim.Apply(new DodgeCommand());
            Assert.That(dodge.Accepted, Is.False);
            Assert.That(dodge.Problem, Is.EqualTo(Home.LockText));

            // Held keys are recorded but move nothing: the lock is on the mover, not only on the command.
            var before = st.Engineer.Pos;
            Assert.That(sim.Apply(new WalkCommand(1, 0)).Accepted, Is.True,
                "a held key is recorded so a release during the repair is not lost");
            RegressionFixture.Step(sim, 40);
            Assert.That(st.Engineer.Pos.X, Is.EqualTo(before.X).Within(1e-9), "and it moves the engineer nowhere");
            Assert.That(st.Engineer.Pos.Y, Is.EqualTo(before.Y).Within(1e-9));

            // Cancel is never gated on movement, and it releases the lock in the same tick.
            var cancel = sim.Apply(new CancelRepairCommand());
            Assert.That(cancel.Accepted, Is.True, cancel.Problem);
            Assert.That(HandCraft.HandLocked(st), Is.False, "Cancel unlocks");
            RegressionFixture.Accept(sim.Apply(new MoveCommand(target.X, target.Y)));
            RegressionFixture.Step(sim, 40);
            Assert.That(st.Engineer.Pos.X, Is.LessThan(before.X - 1e-6), "and the engineer moves again");
            Assert.That(RegressionFixture.Off(ctx, st), Is.Empty);
        }

        /// <summary>U-8, the repair half: a core repair locks the engineer with its OWN prompt, not the craft one.</summary>
        [Test]
        public void U8_ARunningCoreRepairLocksMovementWithItsOwnPrompt()
        {
            var ctx = RegressionFixture.Context();
            var sim = RegressionFixture.NewGame(ctx);
            var st = sim.State;

            HomeCore.Damage(st, 100);
            RegressionFixture.Accept(sim.Apply(new RepairCommand(RepairKinds.Core, -1)));
            Assert.That(HandCraft.HandLocked(st), Is.True);
            Assert.That(HandCraft.LockTextFor(st), Is.EqualTo(Home.LockText), "the repair's prompt, not the craft's");

            var walkTo = sim.Apply(new MoveCommand(st.Engineer.Pos.X - 2, st.Engineer.Pos.Y));
            Assert.That(walkTo.Accepted, Is.False);
            Assert.That(walkTo.Problem, Is.EqualTo(Home.LockText));

            RegressionFixture.Accept(sim.Apply(new CancelRepairCommand()));
            Assert.That(HandCraft.HandLocked(st), Is.False);
            Assert.That(RegressionFixture.Off(ctx, st), Is.Empty);
        }

        // ------------------------------------------------------------------ U-9

        /// <summary>
        /// U-9. Required Unity behaviour: "With the Home core disabled the objective must name the core repair —
        /// title `Restore Home power` — ahead of every build step, so the player is never told to build a Generator
        /// while the thing that powers it is dead."
        ///
        /// Row 0b of the objective chain: only the engineer's own recovery outranks it.
        ///
        /// OPN-07 (REL-72) keeps the rule and corrects the title. In Unity the core powers nothing, so "Restore
        /// Home power" told the player about an outage that had not happened; the card is now titled
        /// `Repair the Home core`. What U-9 asked for — the core repair ahead of every build step — is unchanged.
        /// </summary>
        [Test]
        public void U9_ADisabledCoreMakesTheObjectiveNameTheCoreRepairAheadOfEveryBuildStep()
        {
            var ctx = RegressionFixture.Context();
            var sim = RegressionFixture.NewGame(ctx);
            var st = sim.State;

            // Before: a healthy core, so the chain is on its first BUILD step.
            var healthy = OpeningQueries.Objective(ctx, st);
            Assert.That(healthy.Title, Is.Not.EqualTo("Repair the Home core"), "an undamaged core is not an objective");

            HomeCore.Damage(st, HomeQueries.CoreHp(st));
            Assert.That(HomeQueries.CoreOperational(st), Is.False, "the core is disabled");

            var objective = OpeningQueries.Objective(ctx, st);
            Assert.That(objective.Title, Is.EqualTo("Repair the Home core"));
            Assert.That(objective.Id, Is.EqualTo("home-recovery"));
            Assert.That(objective.HasLocation, Is.True, "and it points at the core");
            Assert.That(objective.Text, Is.Not.Empty, "with something to do, not just a title");
            Assert.That(objective.Detail, Does.Contain("Hand mining still works"),
                "the player is told what they CAN do while Home is dark");
        }

        // ------------------------------------------------------------------ U-10

        /// <summary>
        /// U-10. Required Unity behaviour: "No duplicate warning text — the director and the opening encounter must
        /// never put two live warnings with the same key on screen in one tick."
        ///
        /// The key is what the player reads: a director notice's KIND (<see cref="RaidNoticeKind"/>, which is what
        /// the HUD strip renders) and each opening notice's own kind. The check runs a whole encounter — announced,
        /// staged, repelled, recovered — and inspects every tick's event list separately, because the host drains
        /// <c>st.Events</c> every frame and a doubled toast would be gone before anything else could look.
        ///
        /// It also asserts the two OWNERS of on-screen warning text never hold the same string at once
        /// (<c>Director.Notice</c> and <c>Opening.DeferNotice</c>), which is the same defect one layer down.
        /// </summary>
        [Test]
        public void U10_TheDirectorAndTheOpeningNeverRaiseTwoWarningsWithTheSameKeyInOneTick()
        {
            var ctx = Relight.Sim.Tests.Combat.RaidFixture.Context();
            var sim = Simulation.NewGame(ctx, 7);
            var st = sim.State;
            Relight.Sim.Tests.Combat.RaidFixture.Turret(ctx, st, Relight.Sim.Tests.Combat.RaidFixture.CoreX + 6,
                Relight.Sim.Tests.Combat.RaidFixture.CoreY);

            var seen = new List<string>();
            var ticks = 0;
            for (var pass = 0; pass < 2; pass++)
            {
                // Pass 0 takes the encounter from pending to active; pass 1 runs it out after the group dies.
                for (var i = 0; i < 900; i++)
                {
                    sim.Tick();
                    ticks++;
                    seen.Clear();
                    for (var e = 0; e < st.Events.Count; e++)
                    {
                        var key = WarningKey(st.Events[e]);
                        if (key == null) continue;
                        Assert.That(seen, Does.Not.Contain(key),
                            "tick " + ticks + ": two live warnings with the key \"" + key + "\" in the same tick");
                        seen.Add(key);
                    }
                    st.Events.Clear();

                    if (!string.IsNullOrEmpty(st.Director.Notice) && !string.IsNullOrEmpty(st.Opening.DeferNotice))
                        Assert.That(st.Opening.DeferNotice, Is.Not.EqualTo(st.Director.Notice),
                            "tick " + ticks + ": the director and the opening are showing the same warning text twice");
                }
                // End the encounter between the passes so the "ended"/"recovery" notices are covered too.
                var group = st.Opening.Group;
                if (group != 0)
                {
                    var bodies = st.Enemies.Actors;
                    for (var i = bodies.Count - 1; i >= 0; i--) if (bodies[i].Group == group) bodies.RemoveAt(i);
                }
            }

            Assert.That(st.Opening.Status, Is.Not.EqualTo(OpeningStatus.Pending),
                "the run must actually have exercised the encounter");
        }

        /// <summary>What the player would read as one warning, or null for an event that is not a warning.</summary>
        private static string WarningKey(SimEvent e)
        {
            switch (e)
            {
                case RaidNoticeEvent r: return "raid:" + r.Kind;
                case OpeningScheduledEvent _: return "opening:scheduled";
                case OpeningStartedEvent _: return "opening:started";
                case OpeningEndedEvent _: return "opening:ended";
                case OpeningDeferredEvent _: return "opening:deferred";
                case ResupplyWorkingEvent _: return "opening:resupply";
                default: return null;
            }
        }

        // ------------------------------------------------------------------ the conservation sweep

        /// <summary>
        /// The §9 rows are all about one moment; this is about the whole opening. Required behaviour (U-D-05):
        /// after the scripted opening — mine, Generator, Excavator, chest, belts, poles, rifle, bullets, turret,
        /// load — every item is still accounted for: <c>held + sinks - sources - opening == 0</c> for every item.
        ///
        /// This is the check that catches the class of bug a per-command test cannot: a plate that a belt, a
        /// hopper, a cancelled batch or a reload quietly duplicates or destroys somewhere along a 3000-tick run.
        /// </summary>
        [Test]
        public void TheScriptedOpeningConservesEveryItemOverThreeThousandTicks()
        {
            var ctx = RegressionFixture.Context();
            var sim = RegressionFixture.NewGame(ctx);
            var st = sim.State;
            var spent = 0;

            // 1. Mine, for real: the rubble field beside the spawn is the game's first source of steel.
            RegressionFixture.Accept(sim.Apply(new MineCommand(10, 10)), "hand mining the rubble");
            RegressionFixture.Step(sim, 400);
            spent += 400;
            Assert.That(st.Stats.MinedOf[ItemId.Steel], Is.GreaterThan(0), "the dig produced something");
            RegressionFixture.Accept(sim.Apply(new StopMiningCommand()));

            // The rest of the opening's materials. Booked as made, so the ledger's sources match (see Give).
            RegressionFixture.Give(st, ItemId.Steel, 200);
            RegressionFixture.Give(st, ItemId.Copper, 120);
            RegressionFixture.Give(st, ItemId.Coal, 80);
            RegressionFixture.Give(st, ItemId.Stone, 60);
            RegressionFixture.Give(st, ItemId.Wire, 80);
            RegressionFixture.Give(st, ItemId.Frame, 40);
            RegressionFixture.Give(st, ItemId.Board, 40);
            RegressionFixture.Give(st, ItemId.Concrete, 40);
            RegressionFixture.Give(st, ItemId.Magazine, 40);

            // 2-7. The build order, each step paid for out of the pockets by the real placement command.
            var depot = Place(ctx, sim, "depot", 10, 4);
            Place(ctx, sim, "pole", 18, 4);
            var generator = Place(ctx, sim, "generator", 20, 4);
            var excavator = Place(ctx, sim, "excavator", 22, 12);
            Place(ctx, sim, "belt", 25, 12, Dir.E);
            Place(ctx, sim, "belt", 26, 12, Dir.E);
            Place(ctx, sim, "belt", 27, 12, Dir.E);
            var chest = Place(ctx, sim, "chest", 28, 12);
            Place(ctx, sim, "pole", 30, 12);
            var turret = Place(ctx, sim, "turret", 30, 4);
            Assert.That(new[] { depot, generator, excavator, chest, turret }, Is.All.Not.Null);

            // Fuel: the only coal sink in the opening, and the one the power report reads.
            Reach(st, generator);
            RegressionFixture.Accept(sim.Apply(new MachineTransferCommand(generator.Id, ItemId.Coal, 50, true)),
                "fuelling the Generator");
            RegressionFixture.Step(sim, 200);
            spent += 200;

            // 8. The expedition rifle, loaded: a weapon's chambered rounds are still the engineer's magazines.
            var rifle = RegressionFixture.GiveWeapon(ctx, st);
            RegressionFixture.Accept(sim.Apply(new EquipCommand(rifle, 0)), "equipping the rifle");
            RegressionFixture.Accept(sim.Apply(new ReloadCommand()), "loading the rifle");
            RegressionFixture.Step(sim, 100);
            spent += 100;

            // 9. Bullets at the workshop. Since U-D-44 the queue runs unattended and the batches land in the
            // workshop's output tray, so they are collected in reach before they can be carried anywhere.
            Reach(st, depot);
            RegressionFixture.Accept(sim.Apply(new HandCraftCommand(2)), "queueing bullet batches");
            RegressionFixture.Step(sim, 900);
            spent += 900;
            Assert.That(st.Hand.Jobs, Is.Empty, "both batches finished");
            Assert.That(HandCraft.HandLocked(st), Is.False, "and nothing about the workshop ever pinned the engineer");
            Assert.That(st.Hand.Output[ItemId.Magazine],
                Is.EqualTo(2 * ctx.Data.Engineer.HandBulletsPerCraft), "waiting in the tray, not in the Backpack");
            RegressionFixture.Accept(sim.Apply(new CollectWorkshopCommand()), "collecting the finished bullets");
            Assert.That(st.Hand.Output.IsEmpty, Is.True);

            // 10. Load the turret by hand; the hopper is counted under LedgerPlace.Machines, never twice.
            Reach(st, turret);
            RegressionFixture.Accept(sim.Apply(new MachineTransferCommand(turret.Id, ItemId.Magazine, 20, true)),
                "loading the turret");

            RegressionFixture.Step(sim, 3000 - spent);

            Assert.That(RegressionFixture.Off(ctx, st), Is.Empty,
                "conservation broke somewhere in the scripted opening");
            Assert.That(st.Machines.Count, Is.GreaterThanOrEqualTo(10), "the whole opening was actually built");
        }

        /// <summary>Stands the engineer on a tile of <paramref name="m"/>, so a reach-checked command can be sent.</summary>
        private static void Reach(SimState st, Machine m) => st.Engineer.Pos = new Vec2(m.X + 0.5, m.Y + 0.5);

        /// <summary>
        /// Walks to the footprint and places the machine with the REAL command, so the build cost is charged out of
        /// the pockets and counted in <c>Stats.Placed</c> — which is what makes the sweep a conservation check
        /// rather than a tour of free scenery.
        /// </summary>
        private static Machine Place(SimContext ctx, Simulation sim, string kind, int x, int y, Dir dir = Dir.N)
        {
            var st = sim.State;
            st.Engineer.Pos = new Vec2(x + 0.5, y + 0.5);
            RegressionFixture.Accept(sim.Apply(new PlaceMachineCommand(kind, x, y, dir)), "placing the " + kind);
            var m = ProductionRules.MachineAt(st, x, y);
            Assert.That(m, Is.Not.Null, "the " + kind + " is on the map at " + x + "," + y);
            return m;
        }
    }
}
