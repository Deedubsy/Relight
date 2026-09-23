using System;
using System.Collections.Generic;
using NUnit.Framework;
using Relight.Sim.Tests.Combat;

namespace Relight.Sim.Tests.Campaign
{
    /// <summary>
    /// REL-131. The owner's note: <i>"Existing and placed walls should block light from the player torch, but not
    /// light from placed objects"</i>. Asked which half to build, they chose <b>only stop the torch</b> — so
    /// REL-116's lamps keep the blocking they already have (<see cref="LightBlockingTests"/> owns that half, and
    /// still passes unchanged) and the torch gains it.
    ///
    /// The torch cannot be a <see cref="Light"/>: it points wherever the mouse is, a continuous angle, while
    /// <c>Light.Dir</c> is one of eight. So the shadow was lifted out of <c>StampWindow</c> into
    /// <see cref="LightRules.Shadow"/> and the caller supplies the shape. These tests cover that shared piece —
    /// that it answers the way a lamp's stamp does, that it leaves alone what it was not asked about, and that it
    /// is still the drawing only: no mask moves and no sim query changes its answer.
    ///
    /// <c>LightingTorchShadowPlayTests</c> in the PlayMode suite covers the other half, that the cone on screen
    /// actually stops at the wall.
    /// </summary>
    public sealed class TorchShadowTests
    {
        private const int N = RaidFixture.Size;

        /// <summary>A box of ones, the way a caller marks "test every one of these".</summary>
        private static byte[] AllAsked(int bw, int bh)
        {
            var clear = new byte[bw * bh];
            for (var i = 0; i < clear.Length; i++) clear[i] = 1;
            return clear;
        }

        [Test]
        public void OnOpenGroundItSaysSoAndChangesNothing()
        {
            var ctx = RaidFixture.Context();
            var st = RaidFixture.State(ctx);
            var clear = AllAsked(9, 9);
            var before = (byte[])clear.Clone();

            var any = LightRules.Shadow(clear, 16, 16, 9, 9, ctx, st, 20.5, 20.5);

            Assert.That(any, Is.False, "there is nothing in the way, so no line should have been walked at all.");
            Assert.That(clear, Is.EqualTo(before), "open ground had its marks changed.");
        }

        [Test]
        public void AWallTakesTheGroundBehindItAndLeavesTheGroundInFrontOfIt()
        {
            var ctx = RaidFixture.Context();
            var st = RaidFixture.State(ctx);
            for (var y = 17; y <= 23; y++) RaidFixture.Add(ctx, st, "wall", 22, y);

            const int x0 = 14, y0 = 14, bw = 13, bh = 13;
            var clear = AllAsked(bw, bh);
            byte At(int tx, int ty) => clear[(ty - y0) * bw + (tx - x0)];

            var any = LightRules.Shadow(clear, x0, y0, bw, bh, ctx, st, 20.5, 20.5);

            Assert.That(any, Is.True, "a wall stands inside the box; the line tests should have run.");
            Assert.That(At(21, 20), Is.EqualTo(1), "the near side of the wall is still reached.");
            Assert.That(At(22, 20), Is.EqualTo(1), "the wall's own face is reached — the far end tile is exempt, as it is for a lamp.");
            Assert.That(At(23, 20), Is.EqualTo(0), "the ground directly behind the wall is still marked as reached.");
            Assert.That(At(25, 20), Is.EqualTo(0), "further behind the wall is still marked as reached.");
            Assert.That(At(16, 20), Is.EqualTo(1), "the open side of the source lost its light.");
            Assert.That(At(20, 16), Is.EqualTo(1), "the ground north of the source lost its light.");
        }

        [Test]
        public void AnAuthoredBuildingStopsItTheSameWayAWallDoes()
        {
            var ctx = RaidFixture.Context(RaidFixture.Map(new[] { new TileRect(22, 17, 1, 7) }));
            var st = RaidFixture.State(ctx);

            const int x0 = 14, y0 = 14, bw = 13, bh = 13;
            var clear = AllAsked(bw, bh);
            byte At(int tx, int ty) => clear[(ty - y0) * bw + (tx - x0)];

            LightRules.Shadow(clear, x0, y0, bw, bh, ctx, st, 20.5, 20.5);

            Assert.That(At(22, 20), Is.EqualTo(1), "the facade catches the light.");
            Assert.That(At(23, 20), Is.EqualTo(0), "the street behind the building is dark.");
        }

        [Test]
        public void NoOtherMachineStopsIt()
        {
            var ctx = RaidFixture.Context();
            var st = RaidFixture.State(ctx);
            RaidFixture.Add(ctx, st, "chest", 22, 20);

            var clear = AllAsked(13, 13);
            var before = (byte[])clear.Clone();

            var any = LightRules.Shadow(clear, 14, 14, 13, 13, ctx, st, 20.5, 20.5);

            Assert.That(any, Is.False, "a chest stops neither a bullet nor light, so nothing should be occluding.");
            Assert.That(clear, Is.EqualTo(before));
        }

        [Test]
        public void ATileTheCallerDidNotAskAboutIsNeverReportedAsReached()
        {
            var ctx = RaidFixture.Context();
            var st = RaidFixture.State(ctx);
            for (var y = 17; y <= 23; y++) RaidFixture.Add(ctx, st, "wall", 22, y);

            const int x0 = 14, y0 = 14, bw = 13, bh = 13;
            var clear = new byte[bw * bh];
            clear[(20 - y0) * bw + (23 - x0)] = 1;        // one tile, behind the wall

            LightRules.Shadow(clear, x0, y0, bw, bh, ctx, st, 20.5, 20.5);

            for (var i = 0; i < clear.Length; i++)
                Assert.That(clear[i], Is.EqualTo(0),
                    "a tile the caller never asked about came back marked as reached.");
        }

        /// <summary>
        /// The point of sharing the rule: put the torch's question and a lamp's question to the same wall and they
        /// must give the same answer, tile for tile. If these two ever drift apart, the player sees a wall that
        /// stops the torch but not the lamp standing beside it.
        /// </summary>
        [Test]
        public void ItAgreesWithTheStampALampGetsThroughTheSameWall()
        {
            var ctx = RaidFixture.Context();
            var st = RaidFixture.State(ctx);
            for (var y = 14; y <= 26; y++) RaidFixture.Add(ctx, st, "wall", 23, y);
            for (var x = 14; x <= 26; x++) RaidFixture.Add(ctx, st, "wall", x, 17);

            var l = new Light(20, 20, 7, LightKind.StreetLight, true);
            var mask = new byte[N * N];
            LightRules.Stamp(mask, N, N, in l, ctx, st);

            var x0 = (int)Math.Floor(l.Tx - l.R);
            var y0 = (int)Math.Floor(l.Ty - l.R);
            var bw = (int)Math.Ceiling(l.Tx + l.R) - x0 + 1;
            var bh = (int)Math.Ceiling(l.Ty + l.R) - y0 + 1;

            // Marked exactly where the disc reaches, so the only difference left between the two is the shadow.
            var clear = new byte[bw * bh];
            for (var by = 0; by < bh; by++)
                for (var bx = 0; bx < bw; bx++)
                    if (LightRules.Covers(in l, x0 + bx, y0 + by)) clear[by * bw + bx] = 1;

            LightRules.Shadow(clear, x0, y0, bw, bh, ctx, st, l.Tx + 0.5, l.Ty + 0.5);

            var disagreed = new List<string>();
            for (var by = 0; by < bh; by++)
                for (var bx = 0; bx < bw; bx++)
                {
                    var tx = x0 + bx;
                    var ty = y0 + by;
                    if (tx < 0 || ty < 0 || tx >= N || ty >= N) continue;
                    if (!LightRules.Covers(in l, tx, ty)) continue;
                    if (mask[ty * N + tx] != clear[by * bw + bx]) disagreed.Add($"({tx},{ty})");
                }

            Assert.That(disagreed, Is.Empty,
                "the torch's shadow and the lamp's stamp disagree about these tiles: " + string.Join(", ", disagreed));
        }

        /// <summary>
        /// "Seen only", the same standing rule the building glow was built under (U-D-71 b): this is geometry the
        /// drawing asks for, and the simulation's own answers do not move because it exists.
        /// </summary>
        [Test]
        public void AskingItChangesNoSimulationAnswer()
        {
            var ctx = RaidFixture.Context();
            var st = RaidFixture.State(ctx);
            RaidFixture.Add(ctx, st, "lamp", 20, 20);
            RaidFixture.Add(ctx, st, "pole", 20, 22);
            RaidFixture.Add(ctx, st, "generator", 20, 24).Inv.Add(ItemId.Coal, 50);
            for (var y = 17; y <= 23; y++) RaidFixture.Add(ctx, st, "wall", 22, y);
            RaidFixture.Run(ctx, st, 1, new List<ITickPhase> { new PowerPhase(), new LightPhase() });

            var (mw, mh) = LightQueries.MaskSize(st);
            var before = (byte[])LightQueries.Mask(st).Clone();
            var builds = LightQueries.Builds(st);
            var rev = st.Rev;

            var clear = AllAsked(13, 13);
            LightRules.Shadow(clear, 14, 14, 13, 13, ctx, st, 20.5, 20.5);

            Assert.That(LightQueries.Mask(st), Is.EqualTo(before), "the lit mask moved.");
            Assert.That(LightQueries.MaskSize(st), Is.EqualTo((mw, mh)));
            Assert.That(LightQueries.Builds(st), Is.EqualTo(builds), "the mask was rebuilt.");
            Assert.That(st.Rev, Is.EqualTo(rev), "the state was revised.");
            Assert.That(LightQueries.LitAt(st, 23, 20), Is.False);
            Assert.That(LightQueries.LitAt(st, 21, 20), Is.True);
        }

        /// <summary>
        /// The torch is asked for every frame, not every tick, so the open-ground case has to be cheap. The reach is
        /// the real one (<c>MouseFlashlightPresenter.ReachTiles</c> = 14, a 30-tile box) and the map is the full
        /// city's, with a building on every block so the wall case pays its line tests properly. The bound is a
        /// frame's whole budget at 60 Hz, which this must fit inside many times over.
        /// </summary>
        [Test]
        public void OneTorchShadowIsCheapEnoughForEveryFrame()
        {
            const int size = 864;
            var solids = new List<TileRect>();
            for (var y = 8; y < size - 20; y += 24)
                for (var x = 8; x < size - 20; x += 24)
                    solids.Add(new TileRect(x, y, 16, 16));
            var ctx = RaidFixture.Context(RaidFixture.Map(solids, size), withCore: false);
            var st = RaidFixture.State(ctx);

            const int bw = 30, bh = 30;
            var clear = new byte[bw * bh];
            var worst = 0.0;
            for (var run = 0; run < 200; run++)
            {
                // Walked across the map, so the box lands on open street and on a building's corner by turns.
                var sx = 40.5 + run * 3;
                var sy = 40.5 + run;
                for (var i = 0; i < clear.Length; i++) clear[i] = 1;
                var sw = System.Diagnostics.Stopwatch.StartNew();
                LightRules.Shadow(clear, (int)sx - 15, (int)sy - 15, bw, bh, ctx, st, sx, sy);
                sw.Stop();
                if (run > 10 && sw.Elapsed.TotalMilliseconds > worst) worst = sw.Elapsed.TotalMilliseconds;
            }
            TestContext.Out.WriteLine($"worst single torch shadow, 30x30 box: {worst:0.000} ms");

            Assert.That(worst, Is.LessThan(16.0), "a frame at 60 Hz is 16 ms and this is one of many things in it.");
        }
    }
}
