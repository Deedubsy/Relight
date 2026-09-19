using NUnit.Framework;
using Relight.Sim.UI;

namespace Relight.Sim.Tests.UI
{
    /// <summary>ALWAYS_DARK_SPEC.md §3: unlit ground is a readable twilight, and brightness stays inside safe limits.</summary>
    public sealed class DarknessLookTests
    {
        [Test]
        public void TheDefaultIsTheSpecsTwilight()
        {
            Assert.That(DarknessLook.Strength(DarknessLook.DefaultUnlit, DarknessLook.DefaultBrightness),
                Is.EqualTo(0.55).Within(1e-9));
        }

        [TestCase(0.0, 0.70)]
        [TestCase(1.0, 0.375)]
        [TestCase(-5.0, 0.70)]
        [TestCase(9.0, 0.375)]
        public void BrightnessMovesTheStrengthInsideTheLimits(double brightness, double expected)
        {
            Assert.That(DarknessLook.Strength(0.55, brightness), Is.EqualTo(expected).Within(1e-9));
        }

        [Test]
        public void AnAuthoredStrengthOutsideTheLimitsIsClamped()
        {
            Assert.That(DarknessLook.Strength(0.95, 0.5), Is.EqualTo(DarknessLook.MaxStrength).Within(1e-9));
            Assert.That(DarknessLook.Strength(0.05, 0.5), Is.EqualTo(DarknessLook.MinStrength).Within(1e-9));
        }
    }
}
