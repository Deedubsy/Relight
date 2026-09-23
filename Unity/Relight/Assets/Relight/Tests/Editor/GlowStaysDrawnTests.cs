using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using NUnit.Framework;
using UnityEngine;

namespace Relight.Authoring.Tests
{
    /// <summary>
    /// REL-127, U-D-71 b. The owner chose "seen only" for the building glow: it lights the picture and the
    /// simulation does not see it. <c>BuildingGlowTests.ABuildingChangesNothingTheSimulationCanSee</c> proves that
    /// of today's code by comparing masks. This stops tomorrow's.
    ///
    /// The danger is specific and easy to walk into. Raiders hesitate on lit ground
    /// (<c>RaidDirectorTuning.LightHesitateS</c>), turrets see further in it, and the engineer's own lit flag drives
    /// events — so the moment any rule in <c>Sim</c> asks <c>Relight.Sim.UI.BuildingGlow</c> a question, every
    /// building in the base quietly becomes a hesitation field and raids get easier without anyone deciding that.
    /// The class is deliberately parked in <c>Sim/UI</c> beside <c>DarknessLook</c> and <c>LightSweep</c>, which are
    /// picture rules for the same reason; nothing but presentation may read it.
    ///
    /// Written as a source scan, in the idiom <see cref="PresentationReadsLightTests"/> established for REL-9's
    /// mirror-image rule ("no presentation code path builds the mask").
    /// </summary>
    public sealed class GlowStaysDrawnTests
    {
        private static string Root => Path.Combine(Application.dataPath, "Relight");

        /// <summary>The one file allowed to name it: its own.</summary>
        private const string Owner = "Sim/UI/BuildingGlow.cs";

        [Test]
        public void NoSimulationRuleAsksTheBuildingGlow()
        {
            var dir = Path.Combine(Root, "Sim");
            Assert.That(Directory.Exists(dir), Is.True, dir);

            var bad = new List<string>();
            var scanned = 0;
            foreach (var file in Directory.GetFiles(dir, "*.cs", SearchOption.AllDirectories))
            {
                var rel = file.Substring(Root.Length + 1).Replace('\\', '/');
                if (rel == Owner) continue;
                scanned++;
                var lines = File.ReadAllLines(file, Encoding.UTF8);
                for (var i = 0; i < lines.Length; i++)
                {
                    var code = lines[i];
                    var comment = code.IndexOf("//", StringComparison.Ordinal);
                    if (comment >= 0) code = code.Substring(0, comment);
                    if (code.IndexOf("BuildingGlow", StringComparison.Ordinal) >= 0)
                        bad.Add(rel + ":" + (i + 1));
                }
            }

            Assert.That(scanned, Is.GreaterThan(100), "the scan must have read the simulation sources");
            Assert.That(File.Exists(Path.Combine(Root, Owner)), Is.True, Owner + " is missing");
            Assert.That(bad, Is.Empty,
                "U-D-71 b: the building glow is drawn, never simulated. A rule that reads it makes every building " +
                "lit ground, which is a raid difficulty change nobody asked for.");
        }
    }
}
