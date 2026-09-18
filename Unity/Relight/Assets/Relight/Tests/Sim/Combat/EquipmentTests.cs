using NUnit.Framework;

namespace Relight.Sim.Tests
{
    /// <summary>
    /// C-02's equipment: owning a rifle, the Home-workshop craft, the two equipment slots and the ten action-bar
    /// slots (reference equipment.ts, UI_AND_ONBOARDING §5.5).
    /// </summary>
    [TestFixture]
    public sealed class EquipmentTests
    {
        [Test]
        public void ARifleIsCraftedAtHomeAndWaitsInTheTrayUntilItIsCollected()
        {
            var ctx = CombatFixture.Context();
            var st = CombatFixture.State(ctx);
            Assert.AreEqual(0, st.Weapons.Owned.Count, "a new campaign owns no weapon");
            Assert.IsFalse(WeaponQueries.Equipped(st, ctx.Data).Equipped);

            var away = CombatFixture.Apply(ctx, st, new CraftRifleCommand());
            Assert.IsFalse(away.Accepted);
            Assert.AreEqual(WeaponRules.WalkHomeText, away.Problem);

            CombatFixture.Home(ctx, st);
            st.Engineer.Inv[ItemId.Copper] = 1;
            var poor = CombatFixture.Apply(ctx, st, new CraftRifleCommand());
            Assert.IsFalse(poor.Accepted);
            Assert.AreEqual("Carry 10 Steel plates + 4 Copper plate", poor.Problem);

            st.Engineer.Inv[ItemId.Copper] = 5;
            var steel = st.Engineer.Inv[ItemId.Steel];
            Assert.IsTrue(CombatFixture.Apply(ctx, st, new CraftRifleCommand()).Accepted);
            Assert.AreEqual(steel - 10, st.Engineer.Inv[ItemId.Steel], 1e-9, "the plates are reserved up front");
            Assert.AreEqual(1, st.Engineer.Inv[ItemId.Copper], 1e-9);

            var busy = CombatFixture.Apply(ctx, st, new CraftRifleCommand());
            Assert.IsFalse(busy.Accepted);
            Assert.AreEqual(WeaponRules.BusyCraftText, busy.Problem);

            ctx.Data.TryRecipe(WeaponRules.RifleRecipe, out var recipe);
            Assert.AreEqual(6.0, recipe.Seconds, 1e-9, "the catalogue's 6 seconds");
            CombatFixture.Run(ctx, st, 119);
            Assert.IsTrue(st.Weapons.Crafting, "not finished a tick early");
            CombatFixture.Run(ctx, st, 1);

            Assert.IsFalse(st.Weapons.Crafting);
            Assert.AreEqual(1, st.Weapons.Owned.Count);
            Assert.AreEqual("rifle:1", st.Weapons.Owned[0].Id, "kind:serial, from 1");
            Assert.AreEqual(0, st.Engineer.Inv[new ItemKey("rifle:1")], 1e-9,
                "U-D-44: nothing is delivered remotely into the Backpack");
            Assert.AreEqual(1, st.Hand.Output[new ItemKey("rifle:1")], 1e-9, "it waits in the workshop's output tray");
            Assert.AreEqual(1, CombatFixture.Count<WeaponCraftedEvent>(st));

            var got = CombatFixture.Apply(ctx, st, new CollectWorkshopCommand("rifle:1", 1));
            Assert.IsTrue(got.Accepted, got.Problem);
            Assert.AreEqual(1, st.Engineer.Inv[new ItemKey("rifle:1")], 1e-9, "collected, and carried, not equipped");
            Assert.AreEqual("", Fixture.Off(ctx, st), "the ledger balances across the craft");
        }

        [Test]
        public void ACancelledCraftReturnsEveryPlate()
        {
            var ctx = CombatFixture.Context();
            var st = CombatFixture.State(ctx);
            CombatFixture.Home(ctx, st);

            var none = CombatFixture.Apply(ctx, st, new CancelRifleCraftCommand());
            Assert.IsFalse(none.Accepted);
            Assert.AreEqual(WeaponRules.NoCraftText, none.Problem);

            var steel = st.Engineer.Inv[ItemId.Steel];
            var copper = st.Engineer.Inv[ItemId.Copper];
            Assert.IsTrue(CombatFixture.Apply(ctx, st, new CraftRifleCommand()).Accepted);
            CombatFixture.Run(ctx, st, 40);
            var cancel = CombatFixture.Apply(ctx, st, new CancelRifleCraftCommand());
            Assert.IsTrue(cancel.Accepted);
            StringAssert.Contains("returned", cancel.Problem);

            Assert.AreEqual(steel, st.Engineer.Inv[ItemId.Steel], 1e-9);
            Assert.AreEqual(copper, st.Engineer.Inv[ItemId.Copper], 1e-9);
            Assert.AreEqual(0, st.Weapons.CraftRefundSteel, 1e-9, "nothing is left owing when the Backpack has room");
            Assert.AreEqual(0, st.Weapons.Owned.Count);
            Assert.AreEqual("", Fixture.Off(ctx, st));

            // U-D-44 retired the other half of this: walking away no longer cancels the craft and refunds it.
            // The workshop finishes the rifle with nobody there and it waits in the tray to be collected in reach.
            var home = st.Engineer.Pos;
            Assert.IsTrue(CombatFixture.Apply(ctx, st, new CraftRifleCommand()).Accepted);
            st.Engineer.Pos = new Vec2(2, 2);
            CombatFixture.Run(ctx, st, 1);
            Assert.IsTrue(st.Weapons.Crafting, "the workshop keeps working while the engineer is elsewhere");

            ctx.Data.TryRecipe(WeaponRules.RifleRecipe, out var rifleRecipe);
            CombatFixture.Run(ctx, st, (int)(rifleRecipe.Seconds / Fixture.Dt) + 2);
            Assert.IsFalse(st.Weapons.Crafting, "and it finished without him");
            Assert.AreEqual(1, st.Weapons.Owned.Count);
            var made = new ItemKey(st.Weapons.Owned[0].Id);
            Assert.AreEqual(1, st.Hand.Output[made], 1e-9, "waiting in the workshop's output tray");
            Assert.IsFalse(CombatFixture.Apply(ctx, st, new CollectWorkshopCommand()).Accepted,
                "and out of reach there is nothing to collect it with");

            st.Engineer.Pos = home;
            var back = CombatFixture.Apply(ctx, st, new CollectWorkshopCommand());
            Assert.IsTrue(back.Accepted, back.Problem);
            Assert.AreEqual(1, st.Engineer.Inv[made], 1e-9, "collected on the way back");
            Assert.AreEqual("", Fixture.Off(ctx, st));
        }

        [Test]
        public void EquippingMovesTheRifleBetweenThePocketsAndTheSlot()
        {
            var ctx = CombatFixture.Context();
            var st = CombatFixture.State(ctx);
            CombatFixture.Home(ctx, st);
            var id = CombatFixture.CraftRifle(ctx, st);

            Assert.AreEqual(WeaponRules.SlotText, CombatFixture.Apply(ctx, st, new EquipCommand(id, 2)).Problem);
            Assert.AreEqual(WeaponRules.CarriedText, CombatFixture.Apply(ctx, st, new EquipCommand("rifle:9", 0)).Problem);
            Assert.AreEqual(WeaponRules.CarriedText, CombatFixture.Apply(ctx, st, new EquipCommand("steel", 0)).Problem,
                "a plate is not a weapon");

            var r = CombatFixture.Apply(ctx, st, new EquipCommand(id, 0));
            Assert.IsTrue(r.Accepted, r.Problem);
            Assert.AreEqual(0, st.Engineer.Inv[new ItemKey(id)], 1e-9, "it left the Backpack");
            Assert.AreEqual(id, st.Weapons.Slot0);
            Assert.AreEqual(0, st.Weapons.Active, "equipping takes it in hand");
            Assert.AreEqual(id, st.Weapons.ActiveWeapon().Id);
            Assert.AreEqual(1, CombatFixture.Count<EquipmentChangedEvent>(st));

            var q = WeaponQueries.Equipped(st, ctx.Data);
            Assert.IsTrue(q.Equipped);
            Assert.AreEqual("rifle", q.Kind);
            Assert.AreEqual(10, q.Capacity, 1e-9);
            Assert.AreEqual(18, q.EffectiveTiles, 1e-9, "U-P-03 effective range");
            Assert.AreEqual(30, q.MaxTiles, 1e-9, "U-P-03 maximum range");

            var back = CombatFixture.Apply(ctx, st, new UnequipCommand(0));
            Assert.IsTrue(back.Accepted, back.Problem);
            Assert.IsNull(st.Weapons.Slot0);
            Assert.AreEqual(1, st.Engineer.Inv[new ItemKey(id)], 1e-9, "it is carried again");
            Assert.AreEqual(1, st.Weapons.Owned.Count, "ownership never left the engineer");
        }

        [Test]
        public void SwapPutsTheOtherSlotInHandAndKeepsBothMagazines()
        {
            var ctx = CombatFixture.Context();
            var st = CombatFixture.State(ctx);
            CombatFixture.Home(ctx, st);
            st.Engineer.Inv[ItemId.Steel] = 40;
            st.Engineer.Inv[ItemId.Copper] = 20;
            var first = CombatFixture.CraftRifle(ctx, st);
            var second = CombatFixture.CraftRifle(ctx, st);
            Assert.AreNotEqual(first, second, "each craft is its own instance");

            Assert.IsTrue(CombatFixture.Apply(ctx, st, new EquipCommand(first, 0)).Accepted);
            var alone = CombatFixture.Apply(ctx, st, new SwapWeaponCommand());
            Assert.IsFalse(alone.Accepted);
            Assert.AreEqual(WeaponRules.OtherEmptyText, alone.Problem);

            Assert.IsTrue(CombatFixture.Apply(ctx, st, new EquipCommand(second, 1)).Accepted);
            Assert.AreEqual(1, st.Weapons.Active);
            st.Weapons.Find(first).Loaded = 7;
            st.Weapons.Find(second).Loaded = 3;

            var swap = CombatFixture.Apply(ctx, st, new SwapWeaponCommand());
            Assert.IsTrue(swap.Accepted, swap.Problem);
            Assert.AreEqual(0, st.Weapons.Active);
            Assert.AreEqual(7, WeaponQueries.Equipped(st, ctx.Data).Loaded, 1e-9, "each magazine stays with its rifle");
            CombatFixture.Apply(ctx, st, new SwapWeaponCommand());
            Assert.AreEqual(3, WeaponQueries.Equipped(st, ctx.Data).Loaded, 1e-9);
        }

        [Test]
        public void TheActionBarHoldsOneToolPerSlotAndSwapsRatherThanDuplicating()
        {
            var ctx = CombatFixture.Context();
            var st = CombatFixture.State(ctx);
            Assert.AreEqual(WeaponRules.BarSlots, WeaponQueries.Bar(st).Count, "keys 1..9 then 0");

            Assert.IsTrue(CombatFixture.Apply(ctx, st, new AssignBarCommand(0, "rifle:1")).Accepted);
            Assert.IsTrue(CombatFixture.Apply(ctx, st, new AssignBarCommand(1, "chest")).Accepted);
            Assert.AreEqual("rifle:1", WeaponQueries.Bar(st)[0]);
            Assert.AreEqual("chest", WeaponQueries.Bar(st)[1]);

            // Slot 3 asks for the rifle, which is already on slot 1: the two slots exchange contents.
            var displaced = WeaponQueries.Bar(st)[2];
            Assert.IsTrue(CombatFixture.Apply(ctx, st, new AssignBarCommand(2, "rifle:1")).Accepted);
            var bar = WeaponQueries.Bar(st);
            Assert.AreEqual("rifle:1", bar[2]);
            Assert.AreEqual(displaced, bar[0], "the displaced tool moves to the rifle's old slot");
            var seen = 0;
            for (var i = 0; i < bar.Count; i++) if (bar[i] == "rifle:1") seen++;
            Assert.AreEqual(1, seen);

            Assert.IsTrue(CombatFixture.Apply(ctx, st, new AssignBarCommand(2, "")).Accepted);
            Assert.AreEqual("", WeaponQueries.Bar(st)[2], "an empty key clears the slot");

            var bad = CombatFixture.Apply(ctx, st, new AssignBarCommand(10, "chest"));
            Assert.IsFalse(bad.Accepted);
            Assert.AreEqual(WeaponRules.BarSlotText, bad.Problem);

            // A loaded arrangement with a duplicate collapses to the first occurrence (§5.5).
            st.Weapons.Bar.Clear();
            for (var i = 0; i < WeaponRules.BarSlots; i++) st.Weapons.Bar.Add("chest");
            var fixedBar = WeaponQueries.Bar(st);
            Assert.AreEqual("chest", fixedBar[0]);
            for (var i = 1; i < fixedBar.Count; i++) Assert.AreEqual("", fixedBar[i]);
        }

        [Test]
        public void AWeaponIsACarriedItemThatTakesAWholeBackpackSlot()
        {
            var ctx = CombatFixture.Context();
            var st = CombatFixture.State(ctx);
            CombatFixture.Home(ctx, st);
            var id = CombatFixture.CraftRifle(ctx, st);

            var key = new ItemKey(id);
            Assert.IsTrue(key.IsWeapon);
            Assert.AreEqual(1, ctx.Data.StackSize(key), 1e-9, "one rifle fills a slot");

            var slots = Pockets.Slots(ctx.Data, st.Engineer);
            var cells = 0;
            for (var i = 0; i < slots.Count; i++)
                if (slots[i] != null && slots[i].Item == id) { cells++; Assert.AreEqual(1, slots[i].Count, 1e-9); }
            Assert.AreEqual(1, cells, "exactly one cell");

            // A second rifle never stacks with the first: each instance is its own key.
            Assert.AreEqual(0, Pockets.Take(ctx.Data, Pockets.Trial(st.Engineer), key, 2), 1e-9,
                "a weapon only ever moves one at a time");

            // Equipping frees the cell again.
            Assert.IsTrue(CombatFixture.Apply(ctx, st, new EquipCommand(id, 0)).Accepted);
            slots = Pockets.Slots(ctx.Data, st.Engineer);
            for (var i = 0; i < slots.Count; i++) Assert.AreNotEqual(id, slots[i]?.Item);
        }

        [Test]
        public void ADownedEngineerTouchesNoEquipment()
        {
            var ctx = CombatFixture.Context();
            var st = CombatFixture.State(ctx);
            CombatFixture.Home(ctx, st);
            var id = CombatFixture.CraftRifle(ctx, st);
            st.Engineer.Down = 0;

            Assert.AreEqual(WeaponRules.OnFootText, CombatFixture.Apply(ctx, st, new CraftRifleCommand()).Problem);
            Assert.AreEqual(WeaponRules.OnFootText, CombatFixture.Apply(ctx, st, new EquipCommand(id, 0)).Problem);
            Assert.AreEqual(WeaponRules.OnFootText, CombatFixture.Apply(ctx, st, new SwapWeaponCommand()).Problem);
            Assert.AreEqual(WeaponRules.OnFootText, CombatFixture.Apply(ctx, st, new ReloadCommand()).Problem);
        }
    }
}
