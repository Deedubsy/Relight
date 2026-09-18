using NUnit.Framework;

namespace Relight.Sim.Tests
{
    /// <summary>
    /// C-03's rifle runtime: cadence, magazine, reload out of carried bullets one for one (U-D-08), the range
    /// curve from the catalogue (U-P-03), bolts in flight, and a save taken in the middle of all of it.
    /// </summary>
    [TestFixture]
    public sealed class RifleTests
    {
        private static WeaponDef Rifle(GameData d)
        {
            Assert.IsTrue(d.TryWeapon("rifle", out var p));
            return p;
        }

        [Test]
        public void TheCatalogueCarriesTheProvisionalRifleAndItsAmmunition()
        {
            var d = ReferenceData.Create();
            var p = Rifle(d);
            Assert.AreEqual(18, p.EffectiveTiles, 1e-9, "U-P-03 effective");
            Assert.AreEqual(30, p.MaxTiles, 1e-9, "U-P-03 maximum");
            Assert.AreEqual(2.5, p.RatePerS, 1e-9);
            Assert.AreEqual(10, p.Capacity);
            Assert.AreEqual(1.5, p.ReloadSeconds, 1e-9);
            Assert.AreEqual(0, p.ProjectileSpeed, 1e-9, "the rifle is hitscan");

            var a = WeaponRules.Ammo(d);
            Assert.IsNotNull(a);
            Assert.AreEqual(1, a.RoundsPerItem, 1e-9, "U-D-08: one ammunition item is one bullet");
            Assert.AreEqual(ItemId.Magazine, a.Item);
            Assert.AreEqual(200, d.StackSize(ItemKey.Of(ItemId.Magazine)), 1e-9, "U-D-08 stack of 200");
        }

        [Test]
        public void AReloadTakesOneCarriedBulletPerRound()
        {
            var ctx = CombatFixture.Context();
            var st = CombatFixture.State(ctx);
            CombatFixture.Armed(ctx, st, 40);
            var w = st.Weapons.ActiveWeapon();
            Assert.AreEqual(0, w.Loaded, 1e-9, "a fresh rifle is empty");

            var r = CombatFixture.Apply(ctx, st, new ReloadCommand());
            Assert.IsTrue(r.Accepted, r.Problem);
            Assert.AreEqual(1.5, w.Reload, 1e-9);

            var busy = CombatFixture.Apply(ctx, st, new ReloadCommand());
            Assert.IsFalse(busy.Accepted);
            Assert.AreEqual(WeaponRules.ReloadingText, busy.Problem);

            CombatFixture.Run(ctx, st, 29);
            Assert.AreEqual(0, w.Loaded, 1e-9, "nothing arrives early");
            Assert.AreEqual(40, st.Engineer.Inv[ItemId.Magazine], 1e-9);

            CombatFixture.Run(ctx, st, 1);
            Assert.AreEqual(10, w.Loaded, 1e-9, "the whole magazine at 1.5 s");
            Assert.AreEqual(30, st.Engineer.Inv[ItemId.Magazine], 1e-9, "ten items for ten rounds");
            Assert.AreEqual(0, w.Reload, 1e-9);
            var ev = CombatFixture.Last<ReloadedEvent>(st);
            Assert.IsNotNull(ev);
            Assert.AreEqual(10, ev.Rounds, 1e-9);

            var full = CombatFixture.Apply(ctx, st, new ReloadCommand());
            Assert.IsFalse(full.Accepted);
            Assert.AreEqual(WeaponRules.MagFullText, full.Problem);
            // Known gap (see the report's request to the owner of Sim/Actor/Ledger.cs): Ledger.Held does not yet
            // count a weapon's `loaded` rounds as magazines in the pockets, so the ten rounds that moved out of the
            // Backpack read as unexplained. Assert the conservation the ledger cannot see yet, by hand.
            Assert.AreEqual(40, st.Engineer.Inv[ItemId.Magazine] + w.Loaded, 1e-9, "bullets moved, none were created");
            Assert.AreEqual("", Fixture.Off(ctx, st), "loaded rounds are counted as pocket magazines");
        }

        [Test]
        public void APartialReloadTopsUpOnlyWhatIsCarried()
        {
            var ctx = CombatFixture.Context();
            var st = CombatFixture.State(ctx);
            CombatFixture.Armed(ctx, st, 3);
            var w = st.Weapons.ActiveWeapon();

            Assert.IsTrue(CombatFixture.Apply(ctx, st, new ReloadCommand()).Accepted);
            CombatFixture.Run(ctx, st, 30);
            Assert.AreEqual(3, w.Loaded, 1e-9, "three carried bullets, three rounds");
            Assert.AreEqual(0, st.Engineer.Inv[ItemId.Magazine], 1e-9);

            var none = CombatFixture.Apply(ctx, st, new ReloadCommand());
            Assert.IsFalse(none.Accepted);
            Assert.AreEqual(WeaponRules.NoBulletsText, none.Problem);
            Assert.AreEqual(3, st.Engineer.Inv[ItemId.Magazine] + w.Loaded, 1e-9, "only what was carried moved");
            Assert.AreEqual("", Fixture.Off(ctx, st), "loaded rounds are counted as pocket magazines");
        }

        [Test]
        public void TheTriggerFiresAtTheCatalogueCadenceUntilTheMagazineIsEmpty()
        {
            var ctx = CombatFixture.Context();
            var st = CombatFixture.State(ctx);
            CombatFixture.Armed(ctx, st, 40);
            var w = st.Weapons.ActiveWeapon();
            CombatFixture.Apply(ctx, st, new ReloadCommand());
            CombatFixture.Run(ctx, st, 30);
            Assert.AreEqual(10, w.Loaded, 1e-9);

            Assert.IsTrue(CombatFixture.Apply(ctx, st, new FireCommand(24, 16)).Accepted);
            Assert.IsTrue(st.Weapons.Firing);
            Assert.IsTrue(st.Engineer.HasAim, "the aim the body already faces along");

            CombatFixture.Run(ctx, st, 1);
            Assert.AreEqual(1, st.Stats.EngineerFired, "the first round leaves at once");
            Assert.AreEqual(9, w.Loaded, 1e-9);
            Assert.AreEqual(0.4, w.Cooldown, 1e-9, "1 / 2.5 per second");
            Assert.AreEqual(w.Cooldown, st.Engineer.Cooldown, 1e-9, "the view reads it off the engineer");

            CombatFixture.Run(ctx, st, 7);
            Assert.AreEqual(1, st.Stats.EngineerFired, "still on cooldown");
            CombatFixture.Run(ctx, st, 1);
            Assert.AreEqual(2, st.Stats.EngineerFired, "and again exactly 8 ticks later");

            CombatFixture.Run(ctx, st, 8 * 8);
            Assert.AreEqual(10, st.Stats.EngineerFired, "ten rounds, then the magazine is dry");
            Assert.AreEqual(0, w.Loaded, 1e-9);
            CombatFixture.Run(ctx, st, 40);
            Assert.AreEqual(10, st.Stats.EngineerFired, "an empty rifle fires nothing");

            Assert.IsTrue(CombatFixture.Apply(ctx, st, new HoldFireCommand()).Accepted);
            Assert.IsFalse(st.Weapons.Firing);
            Assert.AreEqual("", Fixture.Off(ctx, st), "fired rounds are a ledger sink");
        }

        [Test]
        public void FiringNeedsAWeaponFreeHandsAndAnEngineerOnTheirFeet()
        {
            var ctx = CombatFixture.Context();
            var st = CombatFixture.State(ctx);
            var none = CombatFixture.Apply(ctx, st, new FireCommand(20, 16));
            Assert.IsFalse(none.Accepted);
            Assert.AreEqual(WeaponRules.NoWeaponText, none.Problem);
            Assert.AreEqual(WeaponRules.NoWeaponText, CombatFixture.Apply(ctx, st, new ReloadCommand()).Problem);

            CombatFixture.Armed(ctx, st, 40);
            st.Home.RepairKind = RepairKinds.Machine;
            st.Home.RepairId = 1;
            st.Home.RepairRemaining = 10;
            var locked = CombatFixture.Apply(ctx, st, new FireCommand(20, 16));
            Assert.IsFalse(locked.Accepted);
            Assert.AreEqual(Home.LockText, locked.Problem);

            st.Home.RepairKind = RepairKinds.None;
            st.Engineer.Down = 0;
            var down = CombatFixture.Apply(ctx, st, new FireCommand(20, 16));
            Assert.IsFalse(down.Accepted);
            Assert.AreEqual(WeaponRules.OnFootText, down.Problem);
        }

        [Test]
        public void EveryShotLeavesATracerThatFadesAndTheRangeCurveComesFromTheData()
        {
            var ctx = CombatFixture.Context();
            var st = CombatFixture.State(ctx);
            var p = Rifle(ctx.Data);
            Assert.AreEqual(p.Damage, Ballistics.Damage(p, 0), 1e-9);
            Assert.AreEqual(p.Damage, Ballistics.Damage(p, p.EffectiveTiles), 1e-9, "full damage to 18 tiles");
            Assert.AreEqual(p.Damage / 2, Ballistics.Damage(p, 24), 1e-9, "half way to the maximum");
            Assert.AreEqual(0, Ballistics.Damage(p, p.MaxTiles), 1e-9, "zero at 30");
            Assert.AreEqual(0, Ballistics.Damage(p, p.MaxTiles + 1), 1e-9, "and beyond");

            CombatFixture.Armed(ctx, st, 40);
            CombatFixture.Apply(ctx, st, new ReloadCommand());
            CombatFixture.Run(ctx, st, 30);
            CombatFixture.Apply(ctx, st, new FireCommand(26, 16));
            CombatFixture.Run(ctx, st, 1);

            Assert.AreEqual(1, ProjectileQueries.Tracers(st).Count, "a hitscan shot draws a tracer, not a bolt");
            Assert.AreEqual(0, ProjectileQueries.InFlight(st).Count);
            var shot = ProjectileQueries.Tracers(st)[0];
            Assert.IsFalse(shot.Hit, "nothing to hit yet — C-08 supplies the bodies");
            Assert.AreEqual(st.Engineer.Pos.X, shot.From.X, 1e-9, "the muzzle is the engineer");
            Assert.AreEqual(16, shot.To.Y, 1e-9, "straight down the aim line");
            Assert.Greater(shot.To.X, st.Engineer.Pos.X);

            CombatFixture.Apply(ctx, st, new HoldFireCommand());
            CombatFixture.Run(ctx, st, (int)(Ballistics.ShotTraceSeconds / CombatFixture.Dt) + 1);
            Assert.AreEqual(0, ProjectileQueries.Tracers(st).Count, "tracers fade after half a second");
        }

        [Test]
        public void ABoltFliesInStepsAndExpiresAtItsMaximumRange()
        {
            var ctx = CombatFixture.Context();
            var st = CombatFixture.State(ctx);
            Assert.IsTrue(ctx.Data.TryWeapon("plasma", out var bolt), "the only projectile profile in the catalogue");
            Assert.Greater(bolt.ProjectileSpeed, 0);

            Ballistics.Fire(ctx, st, bolt, st.Engineer.Pos.X + 1, st.Engineer.Pos.Y);
            Assert.AreEqual(1, ProjectileQueries.InFlight(st).Count);
            var p = ProjectileQueries.InFlight(st)[0];
            Assert.AreEqual("plasma", p.Kind);
            Assert.AreEqual(st.Engineer.Pos.X, p.Origin.X, 1e-9);

            Ballistics.TickProjectiles(ctx, st, CombatFixture.Dt);
            Assert.AreEqual(bolt.ProjectileSpeed * CombatFixture.Dt, p.Distance, 1e-9, "speed × dt, in 0.2-tile steps");
            Assert.AreEqual(1, ProjectileQueries.InFlight(st).Count);

            for (var i = 0; i < 200 && ProjectileQueries.InFlight(st).Count > 0; i++)
            {
                Ballistics.TickProjectiles(ctx, st, CombatFixture.Dt);
                st.T += CombatFixture.Dt;
            }
            Assert.AreEqual(0, ProjectileQueries.InFlight(st).Count, "it expires rather than flying for ever");
            var done = CombatFixture.Last<ProjectileExpiredEvent>(st);
            Assert.IsNotNull(done);
            Assert.IsFalse(done.Stopped, "it ran out of range, nothing stopped it");
        }

        [Test]
        public void ASaveTakenMidReloadWithABoltInFlightResumesExactly()
        {
            var ctx = CombatFixture.Context();
            var st = CombatFixture.State(ctx);
            var id = CombatFixture.Armed(ctx, st, 40);
            CombatFixture.Apply(ctx, st, new ReloadCommand());
            CombatFixture.Run(ctx, st, 30);
            CombatFixture.Apply(ctx, st, new FireCommand(26, 16));
            CombatFixture.Run(ctx, st, 1);
            CombatFixture.Apply(ctx, st, new HoldFireCommand());
            CombatFixture.Apply(ctx, st, new ReloadCommand());
            CombatFixture.Run(ctx, st, 10);
            ctx.Data.TryWeapon("plasma", out var bolt);
            Ballistics.Fire(ctx, st, bolt, st.Engineer.Pos.X + 1, st.Engineer.Pos.Y + 1);
            CombatFixture.Apply(ctx, st, new AssignBarCommand(3, id));

            var w = st.Weapons.ActiveWeapon();
            Assert.Greater(w.Reload, 0, "a reload is running");
            Assert.AreEqual(1, ProjectileQueries.InFlight(st).Count);
            Assert.AreEqual(1, ProjectileQueries.Tracers(st).Count);

            var text = SaveSerializer.WriteText(st, ctx.Data);
            var loaded = SaveSerializer.ReadText(text, ctx.Data);
            Assert.IsTrue(loaded.Ok, loaded.Reason);
            var back = loaded.State;

            Assert.AreEqual(w.Reload, back.Weapons.ActiveWeapon().Reload, 1e-12, "the reload resumes where it was");
            Assert.AreEqual(w.Loaded, back.Weapons.ActiveWeapon().Loaded, 1e-12);
            Assert.AreEqual(id, back.Weapons.Slot0);
            Assert.AreEqual(1, back.Weapons.Owned.Count);
            Assert.AreEqual(1, ProjectileQueries.InFlight(back).Count, "the bolt is still in the air");
            Assert.AreEqual(ProjectileQueries.InFlight(st)[0].Distance, ProjectileQueries.InFlight(back)[0].Distance, 1e-12);
            Assert.AreEqual(1, ProjectileQueries.Tracers(back).Count, "and the tracer does not blink out");
            Assert.AreEqual(id, WeaponQueries.Bar(back)[3]);
            Assert.IsNull(back.Weapons.Targets, "the C-08 seam is never saved");

            // The two states run on identically from here.
            CombatFixture.Run(ctx, st, 60);
            CombatFixture.Run(ctx, back, 60);
            Assert.AreEqual(CanonicalJsonWriter.Write(st), CanonicalJsonWriter.Write(back));
        }

        [Test]
        public void TwoLoadsOfOneSaveFireTheSameShots()
        {
            var ctx = CombatFixture.Context();
            var st = CombatFixture.State(ctx);
            CombatFixture.Armed(ctx, st, 40);
            CombatFixture.Apply(ctx, st, new ReloadCommand());
            CombatFixture.Run(ctx, st, 30);
            var text = SaveSerializer.WriteText(st, ctx.Data);

            var a = SaveSerializer.ReadText(text, ctx.Data);
            var b = SaveSerializer.ReadText(text, ctx.Data);
            Assert.IsTrue(a.Ok, a.Reason);
            Assert.IsTrue(b.Ok, b.Reason);

            foreach (var s in new[] { a.State, b.State })
            {
                CombatFixture.Apply(ctx, s, new FireCommand(26, 16));
                CombatFixture.Run(ctx, s, 100);
                CombatFixture.Apply(ctx, s, new HoldFireCommand());   // off the trigger, or the reload feeds it again
                CombatFixture.Apply(ctx, s, new ReloadCommand());
                CombatFixture.Run(ctx, s, 40);
            }

            Assert.AreEqual(10, a.State.Stats.EngineerFired);
            Assert.AreEqual(CanonicalJsonWriter.Write(a.State), CanonicalJsonWriter.Write(b.State));
        }
    }
}
