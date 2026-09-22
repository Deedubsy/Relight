using System;

namespace Relight.Sim
{
    /// <summary>
    /// What a turret standing at one spot would actually be able to shoot: how far its fire reaches on each
    /// bearing, how much of its range circle that adds up to, and one sentence of advice for the player.
    /// <see cref="Reach"/> is in ray order, starting due E and turning clockwise (Y-down), so a presenter can
    /// close it into a polygon without knowing the sampling.
    /// </summary>
    public readonly struct TurretCoverage
    {
        /// <summary>False when the kind is not a turret at all — nothing to draw, nothing to say.</summary>
        public bool Known { get; }
        public double RangeTiles { get; }
        /// <summary>The turret's centre in tiles, which is where the rays start.</summary>
        public double CentreX { get; }
        public double CentreY { get; }
        /// <summary>Clear reach in tiles per ray, in ray order.</summary>
        public double[] Reach { get; }
        /// <summary>Share of the range circle's AREA the turret can reach, 0..1.</summary>
        public double OpenFraction { get; }
        /// <summary>A plain sentence for the player, or empty when the position needs no warning.</summary>
        public string Advice { get; }

        public TurretCoverage(bool known, double range, double cx, double cy, double[] reach, double open, string advice)
        {
            Known = known; RangeTiles = range; CentreX = cx; CentreY = cy;
            Reach = reach; OpenFraction = open; Advice = advice ?? "";
        }
    }

    /// <summary>
    /// The placement question behind GP-W3's "show turret range and obstructed sight during placement; warn clearly
    /// about a blind position".
    ///
    /// The plain range ring the preview used to draw was a promise the turret could not keep: it ignored buildings
    /// and it ignored the player's own walls, so a turret tucked behind a warehouse advertised a full circle of
    /// cover and then never fired. This sweeps the real firing rule — <see cref="Sightline.Clear"/>, the same call
    /// <see cref="TurretPhase"/> makes at the trigger — so the shape drawn before the build is the shape the gun
    /// has after it.
    ///
    /// Cost: <see cref="Rays"/> × range/<see cref="StepTiles"/> sight tests, about 1 700 for a 9-tile gun turret.
    /// That is far too much per frame and entirely fine per placement, so callers cache on
    /// (kind, x, y, <see cref="SimState.Rev"/>) — the preview only moves when the cursor does.
    /// </summary>
    public static class TurretSight
    {
        /// <summary>Bearings sampled around the circle. 48 is 7.5° apart: smooth at a 9-tile radius.</summary>
        public const int Rays = 48;
        /// <summary>How finely each ray is walked outward before the first blocked step wins.</summary>
        public const double StepTiles = 0.5;

        /// <summary>Below this share of its range circle a turret is called blind rather than merely restricted.</summary>
        public const double BlindFraction = 0.40;
        /// <summary>At or above this share nothing is said: some loss of cover is normal in a built-up city.</summary>
        public const double ClearFraction = 0.70;

        /// <summary>
        /// Sweep the coverage a turret of <paramref name="kind"/> would have with its footprint's top-left corner
        /// at (<paramref name="x"/>,<paramref name="y"/>). The turret itself is NOT on the map yet, which is
        /// correct: it does not block its own fire.
        /// </summary>
        public static TurretCoverage Coverage(SimContext ctx, SimState st, string kind, int x, int y)
        {
            if (ctx == null || st == null || !ctx.Data.TryTurret(kind, out var def) || def.RangeTiles <= 0)
                return new TurretCoverage(false, 0, 0, 0, null, 0, "");

            var size = ctx.Data.TryMachine(kind, out var spec) ? spec.Size : 1;
            var cx = x + size / 2.0;
            var cy = y + size / 2.0;
            var range = def.RangeTiles;

            var reach = new double[Rays];
            var area = 0.0;
            // Reach and open area per compass point, so the advice can name where the turret DOES see.
            var sector = new double[8];
            var sectorN = new int[8];

            for (var i = 0; i < Rays; i++)
            {
                var a = i * (2.0 * Math.PI / Rays);
                var ux = Math.Cos(a);
                var uy = Math.Sin(a);
                var clear = 0.0;
                for (var d = StepTiles; d <= range + 1e-9; d = Math.Min(range, d + StepTiles))
                {
                    if (!Sightline.Clear(ctx, st, cx, cy, cx + ux * d, cy + uy * d)) break;
                    clear = d;
                    if (d >= range) break;
                }
                reach[i] = clear;
                var f = clear / range;
                area += f * f;
                var h = DirectorRules.Heading(ux, uy);
                if (h >= 0) { sector[h] += f; sectorN[h]++; }
            }

            var open = area / Rays;
            return new TurretCoverage(true, range, cx, cy, reach, open, Advise(open, sector, sectorN));
        }

        /// <summary>The player-facing sentence. Empty above <see cref="ClearFraction"/>: no news is good news.</summary>
        private static string Advise(double open, double[] sector, int[] sectorN)
        {
            if (open >= ClearFraction) return "";
            var pct = (int)Math.Round(open * 100);
            var best = -1;
            var bestF = 0.0;
            for (var h = 0; h < 8; h++)
            {
                if (sectorN[h] == 0) continue;
                var f = sector[h] / sectorN[h];
                if (f <= bestF) continue;
                bestF = f;
                best = h;
            }
            // Under a tenth of a range in every direction there is no "best view" worth naming.
            var where = best >= 0 && bestF > 0.1 ? " Its clearest field of fire is to the " + DirectorRules.HeadingWord(best) + "." : "";
            return open < BlindFraction
                ? "Blind position: buildings block this turret's line of fire. It covers about " + pct + "% of its range." + where
                : "Restricted position: it covers about " + pct + "% of its range." + where;
        }

        /// <summary>
        /// REL-15 (INT-11): the same sentence for a turret that is ALREADY STANDING THERE, for its hover and its
        /// panel. The audit's case is a turret dropped inside the Home workshop's courtyard, where the buildings
        /// take its line of fire away: placement warns about that position (the red coverage shape and
        /// <see cref="TurretCoverage.Advice"/> under the build cursor, GP-W3), but once the gun was built nothing
        /// said it again, so a player who did not read the cursor had a turret that never fired and no way left to
        /// find out why. The wording is the placement sentence VERBATIM — the same state must not get a second
        /// vocabulary — and it is empty above <see cref="ClearFraction"/>, so an ordinary turret says nothing.
        ///
        /// Placement still ALLOWS it. A gun covering one doorway is a real choice, and GP-W3 already decided to
        /// warn rather than refuse; this only makes sure the warning is still there afterwards.
        /// </summary>
        public static string BuiltAdvice(SimContext ctx, SimState st, Machine m)
        {
            if (ctx == null || st == null || m == null) return "";
            if (!ctx.Data.TryTurret(m.Kind, out var def) || def.RangeTiles <= 0) return "";
            if (TurretRules.Wrecked(ctx.Data, st, m)) return "";   // a wreck has a louder problem; the repair line owns it
            return st.TurretSightCache.Of(ctx, st, m).Advice;
        }
    }

    /// <summary>
    /// One built turret's coverage, remembered until the world changes. <see cref="TurretSight.Coverage"/> is about
    /// 1,700 sight tests — fine once per placement, far too much for a hover card that asks every frame — so the
    /// answer is kept per machine and thrown away whole on the next <see cref="SimState.Rev"/>, the tick anything
    /// could have been built, wrecked or removed in the gun's way. Derived and keyed on <c>Rev</c> exactly like
    /// <see cref="WallMask"/>: never visited, so a save carries nothing about it and a load rebuilds it on the
    /// first question anyone asks.
    /// </summary>
    public sealed class TurretSightCache
    {
        private int _rev = int.MinValue;
        private readonly System.Collections.Generic.Dictionary<int, TurretCoverage> _by =
            new System.Collections.Generic.Dictionary<int, TurretCoverage>();

        public TurretCoverage Of(SimContext ctx, SimState st, Machine m)
        {
            if (_rev != st.Rev) { _by.Clear(); _rev = st.Rev; }
            if (_by.TryGetValue(m.Id, out var c)) return c;
            c = TurretSight.Coverage(ctx, st, m.Kind, m.X, m.Y);
            _by[m.Id] = c;
            return c;
        }
    }

    public sealed partial class SimState
    {
        /// <summary>Built turrets' coverage, keyed on <see cref="Rev"/> (see <see cref="TurretSightCache"/>).</summary>
        public readonly TurretSightCache TurretSightCache = new TurretSightCache();
    }
}
