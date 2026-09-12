using NUnit.Framework;

namespace Relight.Sim.Tests
{
    public sealed class AssemblyBoundaryTests
    {
        [Test]
        public void SimAssemblyDoesNotReferenceUnityEngineOrRelightData()
        {
            var asm = typeof(SimVersion).Assembly;
            foreach (var r in asm.GetReferencedAssemblies())
            {
                Assert.IsFalse(r.Name.StartsWith("UnityEngine"), $"Relight.Sim references {r.Name}");
                Assert.IsFalse(r.Name == "Relight.Data" || r.Name == "Relight.World", $"Relight.Sim references {r.Name}");
            }
        }
    }
}
