using NUnit.Framework;

namespace Relight.Sim.Tests.Persistence
{
    /// <summary>
    /// REL-43 (ENM-09): the data hash a save carries as <c>dataVersion</c> must move when a balance number moves,
    /// or the "this save was made with different balance data" warning on load can never fire for that number.
    /// The defence and siege rows were the two the hash left out. These tests change one number in each and
    /// expect a different hash; the third pins that an untouched catalogue hashes the same twice, so the first two
    /// are not passing on noise.
    /// </summary>
    public sealed class GameDataHashTests
    {
        private static GameData Data(DefenceTuning defence = null, SiegeTuning siege = null) => new GameData(
            CatalogueData.Items(), CatalogueData.Machines(), CatalogueData.Recipes(),
            CatalogueData.Engineer(), CatalogueData.World(),
            CatalogueData.Weapons(), CatalogueData.Enemies(), CatalogueData.Ammunition(), CatalogueData.Turrets(),
            CatalogueData.Power(), CatalogueData.Time(), CatalogueData.Raids(), CatalogueData.Opening(),
            CatalogueData.Stake(), defence, siege);

        [Test]
        public void TheSameCatalogueHashesTheSameTwice()
        {
            Assert.That(GameDataHash.Compute(Data()), Is.EqualTo(GameDataHash.Compute(Data())));
        }

        [Test]
        public void ChangingASiegeNumberChangesTheDataHash()
        {
            var before = GameDataHash.Compute(Data());
            var siege = SiegeTuning.Fallback with { MajorWaves = SiegeTuning.Fallback.MajorWaves + 1 };
            var after = GameDataHash.Compute(Data(siege: siege));
            Assert.That(after, Is.Not.EqualTo(before), "one more wave in a large assault is different balance data");
            Assert.That(GameDataHash.Text(Data(siege: siege)), Does.Contain("\"siege\":"));
        }

        [Test]
        public void ChangingADefenceNumberChangesTheDataHash()
        {
            var before = GameDataHash.Compute(Data());
            var defence = DefenceTuning.Fallback with { CoreHp = DefenceTuning.Fallback.CoreHp + 1 };
            var after = GameDataHash.Compute(Data(defence: defence));
            Assert.That(after, Is.Not.EqualTo(before), "a stronger core is different balance data");
            Assert.That(GameDataHash.Text(Data(defence: defence)), Does.Contain("\"defence\":"));
        }
    }
}
