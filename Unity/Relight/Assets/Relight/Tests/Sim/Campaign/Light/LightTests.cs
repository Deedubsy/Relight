using System.Collections.Generic;
using NUnit.Framework;
using Relight.Sim.Tests.Combat;

namespace Relight.Sim.Tests.Campaign
{
    /// <summary>
    /// C-11's sim half against the reference: light.ts <c>stampLight</c>/<c>lightMask</c> and flow.ts
    /// <c>lightCovers</c>/<c>blockLights</c>.
    ///
    /// The fixture is <see cref="RaidFixture"/> (160x160 of plain ground, a Home core site at 76,76 8x8) because it
    /// is the only shared map big enough for a radius-12 floodlight and a core lot to have room around them.
    /// </summary>
    public sealed class LightTests
    {
        private static SimContext Ctx() => RaidFixture.Context();

        private static SimState Fresh(SimContext ctx)
        {
            var st = RaidFixture.State(ctx);
            HomeCore.Ensure(ctx, st);
            return st;
        }

        private static List<ITickPhase> Phases() =>
            new List<ITickPhase> { new PowerPhase(), new LightPhase() };

        // ---- stampLight / lightCovers ---------------------------------------------------------------------

        [Test]
        public void StampMatchesCoversOverTheWholeBoundingBox()
        {
            const int tw = 40, th = 40;
            var mask = new byte[tw * th];
            var l = new Light(20, 20, 7, LightKind.StreetLight, true);
            LightRules.Stamp(mask, tw, th, in l);

            var lit = 0;
            for (var y = 0; y < th; y++)
                for (var x = 0; x < tw; x++)
                {
                    var covers = LightRules.Covers(in l, x, y);
                    Assert.That(mask[y * tw + x] != 0, Is.EqualTo(covers),
                        $"tile {x},{y} disagrees with lightCovers");
                    if (covers) lit++;
                }
            Assert.That(lit, Is.GreaterThan(0));
            // The disc, never the bounding square: r = 7 lights 149 tiles, well short of 15 x 15 = 225.
            Assert.That(lit, Is.LessThan(15 * 15));
        }

        [Test]
        public void CoversIsTheRadiusRuleWithTheReferenceSlack()
        {
            var l = new Light(10, 10, 3, LightKind.Lamp, true);
            Assert.That(LightRules.Covers(in l, 13, 10), Is.True, "exactly r away is inside (d2 == r2)");
            Assert.That(LightRules.Covers(in l, 14, 10), Is.False);
            Assert.That(LightRules.Covers(in l, 12, 12), Is.True, "d2 = 8 <= r2 = 9");
            Assert.That(LightRules.Covers(in l, 13, 13), Is.False, "d2 = 18 > 9");
        }

        [Test]
        public void FloodlightConeCutsOutsideTheHalfAngleButNotAtTheOrigin()
        {
            // flow.ts lightCovers: the cone test is skipped for d2 <= 2, so the four tiles under the lamp glow.
            var l = new Light(20, 20, 12, LightKind.Floodlight, true, Dir.E, System.Math.PI / 6);
            Assert.That(LightRules.Covers(in l, 21, 20), Is.True, "straight ahead");
            Assert.That(LightRules.Covers(in l, 19, 20), Is.True, "behind, but inside the origin glow (d2 = 1)");
            Assert.That(LightRules.Covers(in l, 14, 20), Is.False, "behind and outside the glow");
            Assert.That(LightRules.Covers(in l, 26, 26), Is.False, "inside the radius, outside the 30 degree cone");
        }

        [Test]
        public void StampClipsToTheMask()
        {
            const int tw = 8, th = 8;
            var mask = new byte[tw * th];
            var l = new Light(0, 0, 4, LightKind.Lamp, true);
            Assert.DoesNotThrow(() => LightRules.Stamp(mask, tw, th, in l));
            Assert.That(mask[0], Is.EqualTo(1));
        }

        // ---- the Home lot ---------------------------------------------------------------------------------

        [Test]
        public void HomeLotIsLitOnAFreshWorld()
        {
            var ctx = Ctx();
            var st = Fresh(ctx);
            LightPhase.Ensure(ctx, st);

            var (x, y, w, h) = LightPhase.HomeLot(ctx, st);
            Assert.That(w, Is.EqualTo(RaidFixture.CoreSize + 2 * ctx.Data.World.MarginTiles));
            for (var ty = y; ty < y + h; ty++)
                for (var tx = x; tx < x + w; tx++)
                    Assert.That(LightQueries.LitAt(st, tx, ty), Is.True, $"lot tile {tx},{ty} must be lit");

            // Well outside the lot and out of every light's reach, the map is dark.
            Assert.That(LightQueries.LitAt(st, 5, 5), Is.False);
        }

        [Test]
        public void HomeLotIsLitAgainAfterAnUpgradedSaveArrivesWithAnEmptyLightState()
        {
            var ctx = Ctx();
            var st = Fresh(ctx);
            LightPhase.Ensure(ctx, st);
            Assert.That(LightQueries.LitAt(st, RaidFixture.CoreX, RaidFixture.CoreY), Is.True);

            // What a v3 save without a `light` key hands back: a brand new, empty LightState over a live world.
            st.Light = new LightState();
            Assert.That(LightQueries.LitAt(st, RaidFixture.CoreX, RaidFixture.CoreY), Is.False,
                "a fresh state has no mask, so the pure query answers 'dark' until the phase runs");

            LightPhase.Ensure(ctx, st);
            Assert.That(LightQueries.LitAt(st, RaidFixture.CoreX, RaidFixture.CoreY), Is.True);
        }

        [Test]
        public void LitAtIsFalseOutsideTheMapAndBeforeTheFirstBuild()
        {
            var ctx = Ctx();
            var st = Fresh(ctx);
            Assert.That(LightQueries.LitAt(st, 0, 0), Is.False, "no mask yet");
            LightPhase.Ensure(ctx, st);
            Assert.That(LightQueries.LitAt(st, -1, 0), Is.False);
            Assert.That(LightQueries.LitAt(st, RaidFixture.Size, 0), Is.False);
        }

        // ---- machines -------------------------------------------------------------------------------------

        [Test]
        public void ALampLightsItsRadiusOnlyWhenPowered()
        {
            var ctx = Ctx();
            var st = Fresh(ctx);
            // Far from the Home lot so nothing else can light these tiles.
            const int lx = 20, ly = 20;
            RaidFixture.Add(ctx, st, "lamp", lx, ly);

            RaidFixture.Run(ctx, st, 1, Phases());
            Assert.That(LightQueries.LitAt(st, lx, ly), Is.False, "an unpowered lamp lights nothing");

            RaidFixture.Power(ctx, st, lx + 2, ly);
            RaidFixture.Run(ctx, st, 1, Phases());

            Assert.That(ctx.Data.TryMachine("lamp", out var spec), Is.True);
            var r = (int)spec.LightRadiusTiles;
            Assert.That(r, Is.GreaterThan(0));
            Assert.That(LightQueries.LitAt(st, lx, ly), Is.True, "a powered lamp lights its own tile");
            Assert.That(LightQueries.LitAt(st, lx + r, ly), Is.True, "and out to its radius");
            Assert.That(LightQueries.LitAt(st, lx + r + 1, ly), Is.False, "and no further");
        }

        [Test]
        public void AnUnfuelledGeneratorLeavesTheLampDark()
        {
            var ctx = Ctx();
            var st = Fresh(ctx);
            const int lx = 20, ly = 20;
            RaidFixture.Add(ctx, st, "lamp", lx, ly);
            RaidFixture.Add(ctx, st, "pole", lx + 2, ly);
            RaidFixture.Add(ctx, st, "generator", lx + 4, ly);   // no coal

            RaidFixture.Run(ctx, st, 1, Phases());
            Assert.That(LightQueries.LitAt(st, lx, ly), Is.False);
        }

        // ---- the cache --------------------------------------------------------------------------------------

        [Test]
        public void TheMaskIsRebuiltOnlyWhenTheLitPictureChanges()
        {
            var ctx = Ctx();
            var st = Fresh(ctx);
            var phases = Phases();

            RaidFixture.Run(ctx, st, 1, phases);
            var afterFirst = LightQueries.Builds(st);
            Assert.That(afterFirst, Is.EqualTo(1));

            RaidFixture.Run(ctx, st, 40, phases);
            Assert.That(LightQueries.Builds(st), Is.EqualTo(afterFirst),
                "two seconds of ticks with nothing changing must not re-stamp the mask");

            RaidFixture.Add(ctx, st, "lamp", 20, 20);   // bumps st.Rev
            RaidFixture.Run(ctx, st, 1, phases);
            Assert.That(LightQueries.Builds(st), Is.EqualTo(afterFirst + 1));

            RaidFixture.Run(ctx, st, 20, phases);
            Assert.That(LightQueries.Builds(st), Is.EqualTo(afterFirst + 1));
        }

        [Test]
        public void SwitchingALampOnRebuildsTheMaskWithoutARevChange()
        {
            var ctx = Ctx();
            var st = Fresh(ctx);
            var phases = Phases();
            RaidFixture.Add(ctx, st, "lamp", 20, 20);
            RaidFixture.Add(ctx, st, "pole", 22, 20);
            var gen = RaidFixture.Add(ctx, st, "generator", 24, 20);
            RaidFixture.Run(ctx, st, 1, phases);
            var before = LightQueries.Builds(st);
            var rev = st.Rev;

            gen.Inv.Add(ItemId.Coal, 10);               // fuel is not a structural change
            RaidFixture.Run(ctx, st, 2, phases);

            Assert.That(st.Rev, Is.EqualTo(rev), "fuelling a generator is not a Rev change");
            Assert.That(LightQueries.Builds(st), Is.EqualTo(before + 1), "but the lamp came on, so the mask must follow");
            Assert.That(LightQueries.LitAt(st, 20, 20), Is.True);
        }

        // ---- daylight ---------------------------------------------------------------------------------------

        [Test]
        public void DaylightBoundariesFollowTheDataRecord()
        {
            var ctx = Ctx();
            var day = ctx.Data.Time.DaySeconds;
            var light = ctx.Data.Time.DaylightSeconds;
            Assert.That(day, Is.EqualTo(1200));
            Assert.That(light, Is.EqualTo(900));

            Assert.That(LightQueries.Daylight(0, day, light).IsDay, Is.True);
            Assert.That(LightQueries.Daylight(899.9, day, light).IsDay, Is.True);
            Assert.That(LightQueries.Daylight(900, day, light).IsDay, Is.False, "daylightSeconds is the first dark instant");
            Assert.That(LightQueries.Daylight(1199.9, day, light).IsDay, Is.False);
            Assert.That(LightQueries.Daylight(1200, day, light).IsDay, Is.True, "dawn of day 2");

            Assert.That(LightQueries.Daylight(0, day, light).Day, Is.EqualTo(1));
            Assert.That(LightQueries.Daylight(1200, day, light).Day, Is.EqualTo(2));
            Assert.That(LightQueries.Daylight(1200, day, light).TimeOfDay, Is.EqualTo(0).Within(1e-9));

            Assert.That(LightQueries.Daylight(600, day, light).Daylight, Is.EqualTo(1).Within(1e-9));
            Assert.That(LightQueries.Daylight(1050, day, light).Daylight, Is.EqualTo(0).Within(1e-9),
                "midnight is the darkest point, halfway through the night");
        }

        [Test]
        public void DaylightIsAlwaysDayWhenTheRecordIsEmpty()
        {
            var v = LightQueries.Daylight(123, 0, 0);
            Assert.That(v.IsDay, Is.True);
            Assert.That(v.Daylight, Is.EqualTo(1));
        }

        // ---- street lights ----------------------------------------------------------------------------------

        [Test]
        public void ARegionWithNoExportedLightsDrawsNoLightPower()
        {
            // Today's importer exports no `sites.lights`, so nothing here changes until the report's patch lands.
            var ctx = Ctx();
            var st = Fresh(ctx);
            Assert.That(StreetLights.Sites(ctx).Count, Is.EqualTo(0));

            RaidFixture.Power(ctx, st, 30, 30);
            RaidFixture.Run(ctx, st, 1, Phases());
            var n = PowerQueries.Network(ctx, st);
            Assert.That(n.DemandKw, Is.EqualTo(0), "no exported lights means no light demand");
        }

        [Test]
        public void AnAuthoredStreetLightDrawsItsKwAndLightsItsRadiusWhenTheCircuitIsUp()
        {
            var geometry = RaidFixture.Map();
            var sites = new WorldSites(new List<SiteRecord>
            {
                new SiteRecord("home", "Home Court", SiteKind.Core, RaidFixture.CoreX, RaidFixture.CoreY,
                    RaidFixture.CoreSize, RaidFixture.CoreSize, "", 0),
                new SiteRecord(StreetLights.IdPrefix + "0", "Street light", SiteKind.Label, 30, 30, 1, 1, "", 0),
            });
            var ctx = new SimContext(ReferenceData.Create(), geometry, null, null, sites);
            var st = Fresh(ctx);

            Assert.That(StreetLights.Sites(ctx).Count, Is.EqualTo(1), "the id convention is enough - the enum member is not needed");

            RaidFixture.Run(ctx, st, 1, Phases());
            Assert.That(LightQueries.LitAt(st, 30, 30), Is.False, "unreachable, so unlit");

            RaidFixture.Power(ctx, st, 32, 30);
            RaidFixture.Run(ctx, st, 1, Phases());

            var n = PowerQueries.Network(ctx, st);
            Assert.That(n.DemandKw, Is.EqualTo(LightRules.StreetLightKw).Within(1e-9),
                "campaignPower.ts:51 - the light's draw is billed through the ordinary demand path");
            var r = (int)LightRules.StreetLightRadiusTiles;
            Assert.That(LightQueries.LitAt(st, 30, 30), Is.True);
            Assert.That(LightQueries.LitAt(st, 30 + r, 30), Is.True);
            Assert.That(LightQueries.LitAt(st, 30 + r + 1, 30), Is.False);
        }

        /// <summary>
        /// Court D55: with substation sites on the map a light is billed to its NEAREST SUBSTATION's circuit, not to a
        /// pole that happens to be beside it, and stays dark and unbilled until that substation is reached.
        /// </summary>
        [Test]
        public void AStreetLightRunsFromItsNearestSubstationNotTheNearestPole()
        {
            var geometry = RaidFixture.Map();
            var sites = new WorldSites(new List<SiteRecord>
            {
                new SiteRecord("home", "Home Court", SiteKind.Core, RaidFixture.CoreX, RaidFixture.CoreY,
                    RaidFixture.CoreSize, RaidFixture.CoreSize, "", 0),
                new SiteRecord(StreetLights.IdPrefix + "0", "Street light", SiteKind.Light, 30, 30, 1, 1),
                new SiteRecord("substation:0", "Substation 0", SiteKind.Substation, 60, 28, 3, 3),
                new SiteRecord("substation:1", "Substation 1", SiteKind.Substation, 140, 140, 3, 3),
            });
            var ctx = new SimContext(ReferenceData.Create(), geometry, null, null, sites);
            var st = Fresh(ctx);
            var light = StreetLights.Sites(ctx)[0];
            Assert.That(PowerGrid.SubstationOf(ctx, light).Id, Is.EqualTo("substation:0"), "nearest by centre distance");

            RaidFixture.Power(ctx, st, 32, 30);        // a pole two tiles from the light, with a fuelled generator
            RaidFixture.Run(ctx, st, 1, Phases());
            Assert.That(PowerGrid.Of(ctx, st).OfSite(light.Id), Is.Null, "the pole beside it does not own the light");
            Assert.That(PowerQueries.Network(ctx, st).DemandKw, Is.Zero, "an unattached light draws nothing");
            Assert.That(LightQueries.LitAt(st, 30, 30), Is.False);

            // Chain Poles east to the substation lot at (60,28): each is 7.5 tiles from the last, the last 3.5 from the lot.
            RaidFixture.Add(ctx, st, "pole", 40, 30);
            RaidFixture.Add(ctx, st, "pole", 48, 30);
            RaidFixture.Add(ctx, st, "pole", 56, 30);
            RaidFixture.Run(ctx, st, 1, Phases());

            var grid = PowerGrid.Of(ctx, st);
            Assert.That(grid.OfSite(light.Id), Is.SameAs(grid.OfSite("substation:0")));
            Assert.That(PowerQueries.Network(ctx, st).DemandKw, Is.EqualTo(LightRules.StreetLightKw).Within(1e-9),
                "the light is billed to its substation's circuit once that is live");
            Assert.That(LightQueries.LitAt(st, 30, 30), Is.True);
        }

        // ---- state ------------------------------------------------------------------------------------------

        [Test]
        public void LightStateSavesNothingAndSurvivesARoundTrip()
        {
            var ctx = Ctx();
            var st = Fresh(ctx);
            LightPhase.Ensure(ctx, st);
            Assert.That(st.Light.HasMask, Is.True);

            var json = SaveSerializer.WriteText(st, ctx.Data);
            Assert.That(json, Does.Contain("\"light\""));

            var load = SaveSerializer.ReadText(json, ctx.Data);
            Assert.That(load.Ok, Is.True, load.Reason);
            var back = load.State;
            Assert.That(back.Light, Is.Not.Null);
            Assert.That(back.Light.HasMask, Is.False, "the mask is transient and never travels in a save");
            LightPhase.Ensure(ctx, back);
            Assert.That(LightQueries.LitAt(back, RaidFixture.CoreX, RaidFixture.CoreY), Is.True);
        }
    }
}
