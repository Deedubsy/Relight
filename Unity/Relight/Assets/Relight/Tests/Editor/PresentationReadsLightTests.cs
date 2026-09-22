using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using NUnit.Framework;
using UnityEngine;

namespace Relight.Authoring.Tests
{
    /// <summary>
    /// REL-9 (INT-05), "Drawing the screen can change what the sim decides". Its acceptance: "no presentation code
    /// path reaches <c>Ensure</c>". The overloads that built the mask for presentation (<c>LightQueries.Mask(ctx, st)</c>
    /// and <c>LitAt(ctx, st, x, y)</c>) are gone, so the compiler already stops the old path; this catches a new one.
    /// The sim builds the lit mask at fixed points (new game, <c>Simulation.Wrap</c>, the Combat slot, the end of the
    /// tick) and presentation and UI only read it.
    /// </summary>
    public sealed class PresentationReadsLightTests
    {
        static readonly string[] Folders = { "Presentation", "UI" };
        static readonly string[] Builders = { "LightPhase.Ensure", "new LightPhase", "new LightMaskPhase", "LightInitializer" };

        static string Root => Path.Combine(Application.dataPath, "Relight");

        [Test] public void NoPresentationOrUiSourceBuildsTheLitMask()
        {
            var bad = new List<string>();
            var scanned = 0;
            foreach (var folder in Folders)
            {
                var dir = Path.Combine(Root, folder);
                Assert.That(Directory.Exists(dir), Is.True, dir);
                foreach (var file in Directory.GetFiles(dir, "*.cs", SearchOption.AllDirectories))
                {
                    scanned++;
                    var lines = File.ReadAllLines(file, Encoding.UTF8);
                    for (var i = 0; i < lines.Length; i++)
                    {
                        var code = lines[i];
                        var comment = code.IndexOf("//", StringComparison.Ordinal);
                        if (comment >= 0) code = code.Substring(0, comment);
                        foreach (var b in Builders)
                            if (code.IndexOf(b, StringComparison.Ordinal) >= 0)
                                bad.Add(file.Substring(Root.Length + 1).Replace('\\', '/') + ":" + (i + 1) + " " + b);
                    }
                }
            }
            Assert.That(scanned, Is.GreaterThan(20), "the scan must have read the presentation and UI sources");
            Assert.That(bad, Is.Empty, "REL-9: presentation reads the mask and never builds it");
        }
    }
}
