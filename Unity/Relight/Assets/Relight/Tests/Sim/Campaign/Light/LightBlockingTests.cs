using System.Collections.Generic;
using System.Diagnostics;
using NUnit.Framework;
using Relight.Sim.Tests.Combat;

namespace Relight.Sim.Tests.Campaign
{
    /// <summary>
    /// L-02, ALWAYS_DARK_SPEC §5.3: "what stops a bullet stops light". Authored solid building tiles and the
    /// player's own Walls stop a light; no other machine does; the tiles at both ends of the line are exempt.
    /// </summary>
    public sealed class LightBlockingTests
    {
        private static List<ITickPhase> Phases() =>
            new List<ITickPhase> { new PowerPhase(), new LightPhase() };

        /// <summary>A powered Lamp (radius 4) at 20,20; the pole and generator stand to the south, out of the way.</summary>
        private static void Lamp(SimContext ctx, SimState st)
        {
            RaidFixture.Add(ctx, st, "lamp", 20, 20);
            RaidFixture.Add(ctx, st, "pole", 20, 22);
            RaidFixture.Add(ctx, st, "generator", 20, 24).Inv.Add(ItemId.Coal, 50);
        }

        [Test]
        public void ALampBehindAWallDoesNotLightTheFarSide()
        {
            var ctx = RaidFixture.Context();
            var st = RaidFixture.State(ctx);
            Lamp(ctx, st);
            for (var y = 17; y <= 23; y++) RaidFixture.Add(ctx, st, "wall", 22, y);

            RaidFixture.Run(ctx, st, 1, Phases());

            Assert.That(LightQueries.LitAt(st, 21, 20), Is.True, "the lamp's own side is lit");
            Assert.That(LightQueries.LitAt(st, 22, 20), Is.True, "the wall's near face catches the light (end tile exempt)");
            Assert.That(LightQueries.LitAt(st, 23, 20), Is.False, "the ground behind the wall is dark");
            Assert.That(LightQueries.LitAt(st, 24, 20), Is.False);
            Assert.That(LightQueries.LitAt(st, 16, 20), Is.True, "the open side still reaches the full radius");
        }

        [Test]
        public void ALampBehindASolidBuildingDoesNotLightTheFarSide()
        {
            var ctx = RaidFixture.Context(RaidFixture.Map(new[] { new TileRect(22, 17, 1, 7) }));
            var st = RaidFixture.State(ctx);
            Lamp(ctx, st);

            RaidFixture.Run(ctx, st, 1, Phases());

            Assert.That(LightQueries.LitAt(st, 21, 20), Is.True);
            Assert.That(LightQueries.LitAt(st, 22, 20), Is.True, "the facade catches the light");
            Assert.That(LightQueries.LitAt(st, 23, 20), Is.False, "the street behind the building is dark");
            Assert.That(LightQueries.LitAt(st, 24, 20), Is.False);
        }

        [Test]
        public void ALampAgainstAWallStillLightsItsOwnSide()
        {
            var ctx = RaidFixture.Context();
            var st = RaidFixture.State(ctx);
            Lamp(ctx, st);
            for (var y = 17; y <= 23; y++) RaidFixture.Add(ctx, st, "wall", 21, y);   // hard against the lamp

            RaidFixture.Run(ctx, st, 1, Phases());

            Assert.That(LightQueries.LitAt(st, 20, 20), Is.True);
            Assert.That(LightQueries.LitAt(st, 17, 20), Is.True);
            Assert.That(LightQueries.LitAt(st, 20, 17), Is.True);
            Assert.That(LightQueries.LitAt(st, 22, 20), Is.False);
        }

        [Test]
        public void NoOtherMachineBlocksLight()
        {
            var ctx = RaidFixture.Context();
            var st = RaidFixture.State(ctx);
            Lamp(ctx, st);
            RaidFixture.Add(ctx, st, "chest", 22, 20);

            RaidFixture.Run(ctx, st, 1, Phases());

            Assert.That(LightQueries.LitAt(st, 23, 20), Is.True, "a chest stops neither a bullet nor light");
            Assert.That(LightQueries.LitAt(st, 24, 20), Is.True);
        }

        [Test]
        public void TakingTheWallDownLetsTheLightThroughAgain()
        {
            var ctx = RaidFixture.Context();
            var st = RaidFixture.State(ctx);
            Lamp(ctx, st);
            var walls = new List<Machine>();
            for (var y = 17; y <= 23; y++) walls.Add(RaidFixture.Add(ctx, st, "wall", 22, y));
            RaidFixture.Run(ctx, st, 1, Phases());
            Assert.That(LightQueries.LitAt(st, 23, 20), Is.False);

            foreach (var w in walls) st.Machines.Remove(w);
            st.Rev++;
            RaidFixture.Run(ctx, st, 1, Phases());

            Assert.That(LightQueries.LitAt(st, 23, 20), Is.True);
        }

        [Test]
        public void WithNothingInTheWayTheBlockedStampIsTheReferenceDisc()
        {
            var ctx = RaidFixture.Context();
            var st = RaidFixture.State(ctx);
            const int n = RaidFixture.Size;
            var plain = new byte[n * n];
            var blocked = new byte[n * n];
            var l = new Light(40.0, 40.0, 12, LightKind.Floodlight, true, Dir.E, System.Math.PI / 6);

            LightRules.Stamp(plain, n, n, in l);
            LightRules.Stamp(blocked, n, n, in l, ctx, st);

            Assert.That(blocked, Is.EqualTo(plain));
        }

        /// <summary>
        /// §5.3 / §7: "a mask rebuild on the full map stays inside the tick budget". The map is the full city's
        /// size (864 wide; square here, so larger than the real 864 × 576) with a building on every block so that
        /// every light pays for its line tests, and carries far more lights than the city has: the full import
        /// authors 18 streetlights, and this stamps 300 radius-7 discs and 60 radius-12 floodlight cones. One tick
        /// is 50 ms; the generous bound keeps a loaded CI machine from failing a rule about design, not hardware.
        /// </summary>
        [Test]
        public void AFullMapRebuildWithBlockingStaysInsideOneTick()
        {
            const int size = 864;
            var solids = new List<TileRect>();
            for (var y = 8; y < size - 20; y += 24)
                for (var x = 8; x < size - 20; x += 24)
                    solids.Add(new TileRect(x, y, 16, 16));
            var ctx = RaidFixture.Context(RaidFixture.Map(solids, size), withCore: false);
            var st = RaidFixture.State(ctx);

            var lights = new List<Light>();
            for (var i = 0; i < 300; i++)       // on the street, one tile off a building's corner
                lights.Add(new Light(6 + 24 * (i % 34), 6 + 24 * (i / 34 % 34), 7, LightKind.StreetLight, true));
            for (var i = 0; i < 60; i++)
                lights.Add(new Light(7.0 + 24 * (i % 30), 31.0 + 24 * (i / 30) * 10, 12, LightKind.Floodlight, true,
                    Dir.E, System.Math.PI / 6));

            var mask = new byte[size * size];
            void Rebuild()
            {
                System.Array.Clear(mask, 0, mask.Length);
                for (var i = 0; i < lights.Count; i++)
                {
                    var l = lights[i];
                    LightRules.Stamp(mask, size, size, in l, ctx, st);
                }
            }

            Rebuild();                                   // warm the JIT
            var best = double.MaxValue;
            for (var run = 0; run < 5; run++)
            {
                var sw = Stopwatch.StartNew();
                Rebuild();
                sw.Stop();
                if (sw.Elapsed.TotalMilliseconds < best) best = sw.Elapsed.TotalMilliseconds;
            }
            TestContext.Out.WriteLine($"full-map blocked rebuild, 360 lights: {best:0.00} ms");

            var lit = 0;
            for (var i = 0; i < mask.Length; i++) lit += mask[i];
            Assert.That(lit, Is.GreaterThan(0));
            Assert.That(best, Is.LessThan(50), "one tick is 50 ms");
        }
    }
}
