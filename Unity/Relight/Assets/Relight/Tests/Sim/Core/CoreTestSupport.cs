using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;

namespace Relight.Sim.Tests.Core
{
    /// <summary>Locates the B-03 fixture files under both runners (Unity's edit-mode runner and a plain dotnet run).</summary>
    internal static class CoreFixtures
    {
        private const string RelativeFolder = "Assets/Relight/Tests/Sim/Core/Fixtures";

        /// <summary>The absolute path of a file in Tests/Sim/Core/Fixtures, or an explanatory failure.</summary>
        public static string Path(string name, [CallerFilePath] string callerFile = null)
        {
            var tried = new List<string>();

            // 1. Beside this source file — works wherever the sources are compiled from (dotnet and Unity alike).
            if (!string.IsNullOrEmpty(callerFile))
            {
                var beside = System.IO.Path.Combine(System.IO.Path.GetDirectoryName(callerFile) ?? ".", "Fixtures", name);
                if (File.Exists(beside)) return beside;
                tried.Add(beside);
            }

            // 2. Walking up from the runner's directories — Unity's edit-mode cwd is the project root.
            foreach (var start in new[] { AppContext.BaseDirectory, Directory.GetCurrentDirectory() })
            {
                if (string.IsNullOrEmpty(start)) continue;
                var dir = new DirectoryInfo(start);
                while (dir != null)
                {
                    var candidate = System.IO.Path.Combine(dir.FullName, RelativeFolder.Replace('/', System.IO.Path.DirectorySeparatorChar), name);
                    if (File.Exists(candidate)) return candidate;
                    dir = dir.Parent;
                }
                tried.Add(start);
            }

            throw new FileNotFoundException($"Fixture '{name}' not found under '{RelativeFolder}'. Tried: {string.Join(", ", tried)}");
        }
    }

    /// <summary>
    /// A flat synthetic map for the core tests: every tile is street, nothing blocks sight. The world subsystem
    /// (B-08) owns the real synthetic map; this one exists only so a <see cref="SimContext"/> can be constructed.
    /// </summary>
    internal sealed class FlatStreetGeometry : ICityGeometry
    {
        public FlatStreetGeometry(int width = 32, int height = 32)
        {
            Width = width;
            Height = height;
            Spawn = new Vec2(width / 2.0, height / 2.0);
        }

        public int Width { get; }
        public int Height { get; }
        public Vec2 Spawn { get; }
        public bool Solid(int x, int y) => false;
        public TileClass TileAt(int x, int y) => x < 0 || y < 0 || x >= Width || y >= Height ? TileClass.Void : TileClass.Street;
        public bool Sight(double x0, double y0, double x1, double y1) => true;
    }

    /// <summary>A threat layer that reports danger on demand, so <see cref="SecondPhase"/> can be observed.</summary>
    internal sealed class ScriptedThreatLayer : IThreatLayer
    {
        public bool Danger;
        public bool Shot;
        public int SecondCalls;
        public int TickCalls;

        public void Tick(SimContext ctx, SimState st, double dt) => TickCalls++;

        public (bool danger, bool shot) Second(SimState st)
        {
            SecondCalls++;
            return (Danger, Shot);
        }
    }

    internal static class CoreTest
    {
        public static SimContext Context(IThreatLayer threat = null)
            => new SimContext(ReferenceData.Create(), new FlatStreetGeometry(), null, threat);
    }
}
