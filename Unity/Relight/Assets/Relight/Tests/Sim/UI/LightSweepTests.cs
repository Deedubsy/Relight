using NUnit.Framework;
using Relight.Sim.UI;

namespace Relight.Sim.Tests.UI
{
    /// <summary>
    /// REL-117, ALWAYS_DARK_SPEC.md §5.4: a district's light is DRAWN arriving outward from its substation, at the
    /// speed <c>CityPresenter</c> staggers the lamp heads, so the ground comes on under them instead of a frame
    /// early. Picture only — the sim's mask still changes on the tick, which
    /// <c>LightReliefTests.TheSimLightsTheWholeDistrictOnTheTickTheEventFires</c> holds.
    /// </summary>
    public sealed class LightSweepTests
    {
        [Test]
        public void TheFrontTravelsAtTheLampSweepsSpeed()
        {
            Assert.That(LightSweep.TilesPerSecond, Is.EqualTo(30.0), "the lamp heads' own speed");
            Assert.That(LightSweep.Radius(1.0), Is.EqualTo(30.0).Within(1e-9));
            Assert.That(LightSweep.Radius(0.0), Is.Zero);
            Assert.That(LightSweep.Radius(-3.0), Is.Zero, "a sweep never runs backwards");
        }

        [Test]
        public void GroundAtTheSubstationLightsAtOnceAndGroundFurtherOutWaitsItsTurn()
        {
            Assert.That(LightSweep.Hold(0, 0), Is.Zero, "the substation's own tile");
            Assert.That(LightSweep.Hold(30, 0.0), Is.EqualTo(1.0), "30 tiles out, nothing has arrived");
            Assert.That(LightSweep.Hold(30, 0.5), Is.EqualTo(1.0), "halfway there, still dark");
            Assert.That(LightSweep.Hold(30, 1.0), Is.Zero, "one second, one second's travel");
            Assert.That(LightSweep.Hold(30, 5.0), Is.Zero, "and it stays lit");
        }

        [Test]
        public void TheFrontHasASoftEdgeRatherThanAGrowingDisc()
        {
            // EdgeTiles of part-lit ground ahead of the front, so the sweep reads as light spreading and not as a
            // circle stamped on the street.
            Assert.That(LightSweep.Hold(31, 1.0), Is.EqualTo(0.5).Within(1e-9));
            Assert.That(LightSweep.Hold(30 + LightSweep.EdgeTiles, 1.0), Is.EqualTo(1.0).Within(1e-9));
            Assert.That(LightSweep.Hold(40, 1.0), Is.EqualTo(1.0), "well beyond the edge is simply dark");
        }

        [Test]
        public void ASweepLastsAsLongAsItsFurthestTileTakesToLight()
        {
            Assert.That(LightSweep.Seconds(60), Is.EqualTo(2.0).Within(1e-9));
            Assert.That(LightSweep.Hold(60, LightSweep.Seconds(60)), Is.Zero, "and everything is lit when it ends");
        }

        [Test]
        public void ADistrictThatGainsNoNewGroundSweepsNothing()
        {
            // REL-11: the same DistrictLitEvent fires again on a refuel. A sweep with no newly lit tile has no
            // furthest tile, so it takes no time and the picture does not stutter.
            Assert.That(LightSweep.Seconds(0), Is.Zero);
            Assert.That(LightSweep.Seconds(-1), Is.Zero);
            Assert.That(LightSweep.Seconds(double.NaN), Is.Zero);
        }
    }
}
