using System;
using System.Collections.Generic;
using Relight.Sim;

namespace Relight.Editor
{
    /// <summary>What <see cref="CityValidator.Validate"/> found. No errors means the city may be imported.</summary>
    public sealed class CityReport
    {
        public readonly List<string> Errors = new List<string>();
        public bool Ok => Errors.Count == 0;
        /// <summary>True when the whole-city rules ran (the region is <see cref="CityValidator.WholeCityRegion"/>).</summary>
        public bool WholeCity;
        /// <summary>Tiles the engineer can walk to from the spawn, by the same flood the exporter counts with.</summary>
        public int Accessible;
        /// <summary>Per tile, y * width + x: reached by that flood.</summary>
        public bool[] Reachable = Array.Empty<bool>();
        /// <summary>Per tile: inside the tram's swept corridor.</summary>
        public bool[] SweptRail = Array.Empty<bool>();
    }

    /// <summary>
    /// D-02b (EXP-01, REL-31). The reference's structural acceptance check for the authored city, ported so that a
    /// broken export FAILS the import instead of landing in the game. Ported from
    /// <c>packages/sim/src/city/validateRiverfront.ts</c> and <c>validateFirstRegion</c>
    /// (<c>packages/sim/src/firstRegion.ts</c>), with the geometry of <c>parcelGeometry.ts</c> and
    /// <c>riverfrontRail.ts</c>. The rule-by-rule list, with what was not ported and why, is
    /// <c>CityValidatorRules.md</c> beside this file.
    ///
    /// EDITOR ONLY, and pure: it reads a parsed <see cref="CityFile"/> plus the exported tile-kind and solid arrays,
    /// writes nothing and touches no asset. Every message starts with the id of the thing at fault (a building, prop,
    /// site, road node, yard or rail index), in the file's own REGION-LOCAL coordinates.
    ///
    /// <b>Crops.</b> A crop such as <c>home</c> carries only what touches its rect, so the rules that need the whole
    /// map (required parcels and sites, the four yards, the road graph, drive connections, rail continuity) run on
    /// the whole city only. Every other rule runs on any region and skips tiles that lie outside it.
    /// </summary>
    public static class CityValidator
    {
        /// <summary>The exporter's one whole-map region (<c>exportUnity.ts REGIONS.full.whole</c>).</summary>
        public const string WholeCityRegion = "full";

        /// <summary>How many errors <see cref="Describe"/> prints before it counts the rest.</summary>
        public const int DescribeLimit = 12;

        private static readonly string[] RequiredParcels =
        {
            "home-workshop", "riverside-pump", "ironworks-hall", "civic-utility",
            "freight-stronghold", "quarry-stronghold", "wharf-stronghold", "civic-dome",
        };

        private static readonly (string kind, int min)[] DowntownArchetypes =
        {
            ("townhall", 1), ("apartment", 2), ("office", 2), ("department", 1), ("arcade", 1), ("parking", 1),
        };

        private static readonly string[] RequiredPlants = {"plant:riverside", "plant:ironworks", "plant:civic"};
        private static readonly string[] RequiredCores = {"core:freight", "core:quarry", "core:wharf"};
        private static readonly string[] RequiredArtifacts = {"artifact:workshop", "artifact:quarry", "artifact:wharf"};
        private static readonly string[] RequiredStops = {"tram:home", "tram:riverside", "tram:ironworks", "tram:civic"};

        /// <summary>A half-open footprint in tiles; doubles because the entrance strips are 0.7 wide.</summary>
        private readonly struct Box
        {
            public readonly double X, Y, W, H;
            public Box(double x, double y, double w, double h) { X = x; Y = y; W = w; H = h; }
            public static Box Of(JRect r) => new Box(r.x, r.y, r.w, r.h);
            /// <summary>parcelGeometry.ts <c>overlaps</c>: touching an edge is allowed, penetration is not.</summary>
            public bool Overlaps(Box b) => X < b.X + b.W && X + W > b.X && Y < b.Y + b.H && Y + H > b.Y;
            /// <summary>parcelGeometry.ts <c>corridorRect</c>: the segment plus square caps of <paramref name="radius"/>.</summary>
            public static Box Corridor(double ax, double ay, double bx, double by, double radius) =>
                new Box(Math.Min(ax, bx) - radius, Math.Min(ay, by) - radius,
                    Math.Abs(ax - bx) + 2 * radius, Math.Abs(ay - by) + 2 * radius);
        }

        private readonly struct Obstacle
        {
            public readonly string Id;
            public readonly Box Box;
            public Obstacle(string id, Box box) { Id = id; Box = box; }
        }

        /// <summary>The errors as one message for the import dialog: the first few, then a count of the rest.</summary>
        public static string Describe(CityReport report)
        {
            if (report == null || report.Ok) return "The city passes every ported rule.";
            var n = report.Errors.Count;
            var text = "The exported city breaks " + n + (n == 1 ? " rule" : " rules") +
                       " ported from validateRiverfront.ts; nothing was imported:";
            for (var i = 0; i < n && i < DescribeLimit; i++) text += "\n  " + report.Errors[i];
            if (n > DescribeLimit) text += "\n  ... and " + (n - DescribeLimit) + " more.";
            return text;
        }

        /// <summary>
        /// Check one region. <paramref name="kind"/> and <paramref name="solid"/> are the exported side-cars, row-major,
        /// <c>y * width + x</c>, exactly as <see cref="RegionImporter"/> read them.
        /// </summary>
        public static CityReport Validate(CityFile city, byte[] kind, byte[] solid)
        {
            var report = new CityReport();
            var errors = report.Errors;
            if (city == null || city.region == null || city.region.size == null)
            {
                errors.Add("city.json has no region");
                return report;
            }
            var w = city.region.size.width;
            var h = city.region.size.height;
            if (w <= 0 || h <= 0 || kind == null || solid == null || kind.Length != w * h || solid.Length != w * h)
            {
                errors.Add($"region {city.region.id}: the terrain arrays do not cover its {w}x{h} tiles");
                return report;
            }
            report.WholeCity = city.region.id == WholeCityRegion;

            var buildings = city.buildings ?? Array.Empty<JBuilding>();
            var props = city.props ?? Array.Empty<JProp>();
            var sites = city.sites ?? new JSites();
            var yards = sites.yards ?? Array.Empty<JRect>();
            var resources = sites.resources ?? Array.Empty<JResource>();
            var roads = Segments(city.roads != null ? city.roads.polylines : null);
            var drives = city.drives ?? Array.Empty<JPolyline>();
            var sc = city.scalars ?? new JScalars();
            var samples = city.tram != null && city.tram.baked != null ? city.tram.baked : Array.Empty<JSample>();

            bool InRegion(int x, int y) => x >= 0 && y >= 0 && x < w && y < h;
            bool Passable(int x, int y) =>
                InRegion(x, y) && kind[y * w + x] != (byte)TileClass.River && solid[y * w + x] == 0;

            var reserved = report.SweptRail = SweptRail(samples, sc.railHalf, w, h);
            var reached = report.Reachable = Flood(city.spawn, w, h, Passable, out report.Accessible);

            // ------------------------------------------------------------ rule 33 (the part an import can check)
            if (string.IsNullOrEmpty(city.mapId)) errors.Add("city.json: authored city has no map ID");

            // ------------------------------------------------------------ rules 1-14, one building at a time
            var ids = new HashSet<string>(StringComparer.Ordinal);
            var margin = sc.roadHalf + sc.setback;
            foreach (var b in buildings)
            {
                if (b == null || b.rect == null) { errors.Add("a building has no rect"); continue; }
                var r = b.rect;
                var box = Box.Of(r);
                var tag = $"{b.id} ({r.x},{r.y})";

                if (!ids.Add(b.id ?? "")) errors.Add(tag + " duplicate");                                       // 1

                if (b.parcel == null) errors.Add(tag + " has no parcel");                                       // 2
                else
                {
                    if (!Within(r, b.parcel)) errors.Add(tag + " outside parcel");
                    if (b.visual != null && !Within(b.visual, b.parcel)) errors.Add(tag + " outside parcel");
                }

                if (sc.court != null && sc.court.radius > 0)                                                    // 3
                {
                    var dx = Math.Max(Math.Max(r.x - sc.court.x, 0), sc.court.x - r.x - r.w);
                    var dy = Math.Max(Math.Max(r.y - sc.court.y, 0), sc.court.y - r.y - r.h);
                    if (Math.Sqrt((double)dx * dx + (double)dy * dy) < sc.court.radius + sc.setback)
                        errors.Add(tag + " turning bulb setback");
                }

                foreach (var (a, z) in roads)                                                                   // 4
                    if (box.Overlaps(new Box(Math.Min(a.x, z.x) - margin, Math.Min(a.y, z.y) - margin,
                            Math.Abs(a.x - z.x) + 2 * margin, Math.Abs(a.y - z.y) + 2 * margin)))
                    {
                        errors.Add(tag + " road/pavement setback");
                        break;
                    }

                for (var i = 0; i < yards.Length; i++)                                                          // 5
                    if (yards[i] != null && box.Overlaps(Box.Of(yards[i]))) { errors.Add(tag + " factory yard"); break; }

                if (OnAServiceDrive(box, drives)) errors.Add(tag + " service drive");                           // 6

                foreach (var a in buildings)                                                                    // 7
                    if (a != null && a.rect != null && string.CompareOrdinal(a.id, b.id) < 0 &&
                        Box.Of(a.rect).Overlaps(box))
                    {
                        errors.Add(tag + " neighbouring building " + a.id);
                        break;
                    }

                foreach (var res in resources)                                                                  // 8
                    if (res != null && box.Overlaps(new Box(res.x, res.y, res.w, res.h)))
                        errors.Add(tag + " resource " + res.item + " at " + res.x + "," + res.y);

                foreach (var p in props)                                                                        // 9
                    if (p != null && p.rect != null && box.Overlaps(Box.Of(p.rect)) &&
                        !(b.enterable && Within(p.rect, r)))
                        errors.Add(tag + " solid prop " + p.id);

                var door = DoorRect(b);
                if (b.doorRect != null && (b.doorRect.w > 0 || b.doorRect.h > 0) && !Same(b.doorRect, door))
                    errors.Add(tag + $" exported door {b.doorRect.x},{b.doorRect.y} differs from the door rule's {door.x},{door.y}");
                if (!Within(door, r) || !OnFacingWall(b.facing, door, r)) errors.Add(tag + " door on wrong wall"); // 10

                var points = b.path != null && b.path.points != null ? b.path.points : Array.Empty<JPoint>();
                if (points.Length == 0) errors.Add(tag + " has no entrance path");
                else
                {
                    DoorOutside(b.facing, door, out var ox, out var oy);                                        // 11
                    if (points[0].x != (int)Math.Floor(ox) || points[0].y != (int)Math.Floor(oy))
                        errors.Add(tag + " entrance path faces wrong way");

                    for (var i = 1; i < points.Length; i++)                                                     // 12
                    {
                        var strip = Box.Corridor(points[i - 1].x + .5, points[i - 1].y + .5,
                            points[i].x + .5, points[i].y + .5, .7);
                        foreach (var q in Obstacles(b, buildings, props, yards, resources))
                            if (strip.Overlaps(q.Box)) errors.Add(tag + " path width obstructed by " + q.Id);
                    }
                }

                bool water = false, track = false;                                                              // 13
                for (var y = r.y; y < r.y + r.h; y++)
                for (var x = r.x; x < r.x + r.w; x++)
                {
                    if (!InRegion(x, y)) continue;
                    water |= kind[y * w + x] == (byte)TileClass.River;
                    track |= reserved[y * w + x];
                }
                if (water || track) errors.Add(tag + (water ? " water" : " swept tram corridor"));

                foreach (var (x, y) in LineTiles(points))                                                       // 14
                    if (InRegion(x, y) && !Passable(x, y))
                    {
                        errors.Add(tag + " obstructed entrance path at " + x + "," + y);
                        break;
                    }
            }

            // ------------------------------------------------------------ rules 19-20, 25-28, 32, 34-36: any region
            for (var i = 0; i < yards.Length; i++)
            {
                var yard = yards[i];
                if (yard == null) continue;
                foreach (var (a, z) in roads)                                                                   // 19
                    if (Box.Of(yard).Overlaps(Box.Corridor(a.x, a.y, z.x, z.y, sc.roadHalf)))
                    {
                        errors.Add("Yard " + i + " overlaps public pavement");
                        break;
                    }
                if (FirstTile(yard, (x, y) => InRegion(x, y) && reserved[y * w + x], out var yx, out var yy))   // 20
                    errors.Add("Yard " + i + " overlaps swept rail at " + yx + "," + yy);
            }

            foreach (var b in buildings)                                                                        // 25
            {
                if (b == null || b.rect == null) continue;
                DoorOutside(b.facing, DoorRect(b), out var ox, out var oy);
                int tx = (int)Math.Floor(ox), ty = (int)Math.Floor(oy);
                if (InRegion(tx, ty) && !reached[ty * w + tx]) errors.Add(b.id + " unreachable entrance approach");
            }

            foreach (var group in new[] {sites.plants, sites.cores, sites.artifacts, sites.stops})              // 26
            foreach (var s in group ?? Array.Empty<JSite>())
            {
                if (s == null) continue;
                var access = false;
                for (var y = s.y - 1; y < s.y + 4; y++)
                for (var x = s.x - 1; x < s.x + 4; x++)
                    access |= InRegion(x, y) && reached[y * w + x];
                if (!access) errors.Add(s.id + " inaccessible binding");
            }

            foreach (var p in props)
            {
                if (p == null || p.rect == null) continue;
                if (FirstTile(p.rect, (x, y) => InRegion(x, y) && reserved[y * w + x], out _, out _))            // 27
                    errors.Add(p.id + " prop on tram corridor");
                foreach (var (a, z) in roads)                                                                   // 28
                    if (Box.Of(p.rect).Overlaps(new Box(Math.Min(a.x, z.x) - sc.roadHalf, Math.Min(a.y, z.y) - sc.roadHalf,
                            Math.Abs(a.x - z.x) + 2 * sc.roadHalf + 1, Math.Abs(a.y - z.y) + 2 * sc.roadHalf + 1)))
                    {
                        errors.Add(p.id + " prop on public road/pavement");
                        break;
                    }
            }

            var angles = ChordAngles(samples);
            foreach (var s in sites.stops ?? Array.Empty<JSite>())                                              // 32
            {
                if (s == null) continue;
                bool any = false, bent = false;
                for (var i = 0; i < samples.Length; i++)
                {
                    var dx = samples[i].x - s.x - 1.0;
                    var dy = samples[i].y - s.y - 1.0;
                    if (Math.Sqrt(dx * dx + dy * dy) >= 4 || double.IsNaN(angles[i])) continue;
                    any = true;
                    bent |= Math.Abs(Math.Sin(angles[i] * 2)) > .001;
                }
                if (!any || bent) errors.Add(s.id + " not on a straight");
            }

            var combat = city.combat ?? new JCombat();
            foreach (var camp in combat.firstCamps ?? Array.Empty<JCamp>())                                     // 34-35
            {
                if (camp == null || !InRegion(camp.x, camp.y)) continue;
                if (!Passable(camp.x, camp.y)) errors.Add(camp.id + ": key marker solid");
                if (!reached[camp.y * w + camp.x]) errors.Add(camp.id + ": no exterior Home path");
            }
            foreach (var gate in combat.freightGates ?? Array.Empty<JRect>())                                   // 36
            {
                if (gate == null) continue;
                for (var y = gate.y; y < gate.y + gate.h; y++)
                for (var x = gate.x; x < gate.x + gate.w; x++)
                    if (InRegion(x, y) && solid[y * w + x] != 0) errors.Add($"Gate {x},{y}: not an existing door");
            }

            if (report.WholeCity) WholeCityRules(city, buildings, ids, sites, yards, roads, drives, samples, w, errors);
            return report;
        }

        // ================================================================= rules 15-18, 21-24, 29-31: whole city

        private static void WholeCityRules(CityFile city, JBuilding[] buildings, HashSet<string> ids, JSites sites,
            JRect[] yards, List<(JPoint a, JPoint z)> roads, JPolyline[] drives, JSample[] samples, int w,
            List<string> errors)
        {
            foreach (var id in RequiredParcels)                                                                 // 15
                if (!ids.Contains(id)) errors.Add("Missing required parcel " + id);

            foreach (var (kind, min) in DowntownArchetypes)                                                     // 16
            {
                var n = 0;
                foreach (var b in buildings) if (b != null && b.kind == kind) n++;
                if (n < min) errors.Add("Missing downtown archetype " + kind);
            }

            RequireSites(sites.plants, RequiredPlants, errors);                                                 // 17
            RequireSites(sites.cores, RequiredCores, errors);
            RequireSites(sites.artifacts, RequiredArtifacts, errors);
            RequireSites(sites.stops, RequiredStops, errors);

            if (yards.Length != 4) errors.Add("Four factory yards required");                                   // 18

            var nodes = city.roads != null && city.roads.nodes != null ? city.roads.nodes : Array.Empty<JRoadNode>();
            var edges = city.roads != null && city.roads.edges != null ? city.roads.edges : Array.Empty<JRoadEdge>();
            var links = new Dictionary<string, List<string>>(StringComparer.Ordinal);
            foreach (var n in nodes) if (n != null && n.id != null) links[n.id] = new List<string>();
            foreach (var e in edges)                                                                            // 21
            {
                if (e == null) continue;
                if (e.from == null || e.to == null || !links.ContainsKey(e.from) || !links.ContainsKey(e.to))
                {
                    errors.Add("Missing road node " + e.from + "/" + e.to);
                    continue;
                }
                links[e.from].Add(e.to);
                links[e.to].Add(e.from);
            }
            if (nodes.Length == 0) errors.Add("Disconnected public road graph: the city has no road nodes");
            else                                                                                                // 22
            {
                var seen = new HashSet<string>(StringComparer.Ordinal);
                var todo = new List<string> {nodes[0].id};
                for (var i = 0; i < todo.Count; i++)
                    if (seen.Add(todo[i]) && links.TryGetValue(todo[i], out var next)) todo.AddRange(next);
                if (seen.Count != links.Count)
                {
                    var stray = "";
                    foreach (var n in nodes) if (n != null && !seen.Contains(n.id)) { stray = n.id; break; }
                    errors.Add("Disconnected public road graph: " + stray + " cannot be reached from " + nodes[0].id);
                }
            }
            foreach (var n in nodes)                                                                            // 23
                if (n != null && links.TryGetValue(n.id, out var l) && l.Count == 1 && string.IsNullOrEmpty(n.end))
                    errors.Add("Unintended road end " + n.id);

            for (var i = 0; i < drives.Length; i++)                                                             // 24
            {
                var pts = drives[i] != null ? drives[i].points : null;
                if (pts == null || pts.Length == 0) { errors.Add("Disconnected factory service drive " + i); continue; }
                var on = false;
                foreach (var (a, z) in roads)
                    on |= pts[0].x >= Math.Min(a.x, z.x) && pts[0].x <= Math.Max(a.x, z.x) &&
                          pts[0].y >= Math.Min(a.y, z.y) && pts[0].y <= Math.Max(a.y, z.y);
                if (!on) errors.Add("Disconnected factory service drive " + i);
            }

            RailRules(samples, w, errors);
        }

        /// <summary>Rule 17: exactly the required ids, each once.</summary>
        private static void RequireSites(JSite[] actual, string[] required, List<string> errors)
        {
            actual = actual ?? Array.Empty<JSite>();
            var seen = new HashSet<string>(StringComparer.Ordinal);
            foreach (var s in actual) if (s != null) seen.Add(s.id ?? "");
            var bad = actual.Length != required.Length || seen.Count != actual.Length;
            var missing = "";
            foreach (var id in required) if (!seen.Contains(id)) { bad = true; if (missing == "") missing = id; }
            if (!bad) return;
            errors.Add("Missing/duplicate required sites " + string.Join(", ", required) +
                       (missing != "" ? " (no " + missing + ")" : ""));
        }

        /// <summary>
        /// Rules 29-31. The export carries the baked centreline as positions only, so the distance along the line is
        /// re-summed here exactly as riverfrontRail.ts defines it (<c>d += hypot</c>) and the heading is the chord
        /// between neighbouring samples rather than the reference's analytic tangent. See CityValidatorRules.md.
        /// </summary>
        private static void RailRules(JSample[] samples, int w, List<string> errors)
        {
            if (samples.Length < 2) { errors.Add("Rail discontinuity 0: the tram has no baked centreline"); return; }

            var d = new double[samples.Length];
            for (var i = 1; i < samples.Length; i++)
                d[i] = d[i - 1] + Hypot(samples[i].x - (double)samples[i - 1].x, samples[i].y - (double)samples[i - 1].y);

            // riverfrontRail.ts:19-24: the logical rail cells. A diagonal step gets its corner cell, so neighbours in
            // the list are always edge-adjacent on a sound line.
            var tiles = new List<int>();
            void Add(int x, int y) { var t = y * w + x; if (tiles.Count == 0 || tiles[tiles.Count - 1] != t) tiles.Add(t); }
            foreach (var p in samples)
            {
                int x = (int)Math.Floor(p.x), y = (int)Math.Floor(p.y);
                if (tiles.Count > 0)
                {
                    var last = tiles[tiles.Count - 1];
                    if (x != last % w && y != last / w) Add(x, last / w);
                }
                Add(x, y);
            }

            var distances = new double[tiles.Count];
            var cursor = 0;
            for (var k = 0; k < tiles.Count; k++)
            {
                double cx = tiles[k] % w + .5, cy = tiles[k] / w + .5, best = double.PositiveInfinity;
                var at = cursor;
                for (var j = cursor; j < Math.Min(samples.Length, cursor + 18); j++)
                {
                    var dd = (cx - samples[j].x) * (cx - samples[j].x) + (cy - samples[j].y) * (cy - samples[j].y);
                    if (dd < best) { best = dd; at = j; }
                }
                cursor = at;
                distances[k] = Math.Max(d[at], (k > 0 ? distances[k - 1] : -.02) + .02);
            }
            distances[distances.Length - 1] = d[d.Length - 1];

            for (var i = 1; i < tiles.Count; i++)                                                               // 29
            {
                int ax = tiles[i - 1] % w, ay = tiles[i - 1] / w, bx = tiles[i] % w, by = tiles[i] / w;
                if (Math.Abs(ax - bx) + Math.Abs(ay - by) != 1 || distances[i] <= distances[i - 1])
                    errors.Add("Rail discontinuity " + i + " at " + bx + "," + by);
            }

            var unique = new HashSet<int>();                                                                    // 30
            foreach (var t in tiles)
                if (!unique.Add(t)) { errors.Add("Duplicate rail cell " + t % w + "," + t / w); break; }

            var angles = ChordAngles(samples);                                                                  // 31
            for (var i = 1; i < samples.Length; i++)
            {
                var turn = i >= 2 && !double.IsNaN(angles[i]) && !double.IsNaN(angles[i - 1])
                    ? Math.Abs(Math.Atan2(Math.Sin(angles[i] - angles[i - 1]), Math.Cos(angles[i] - angles[i - 1])))
                    : 0;
                if (d[i] - d[i - 1] > .251 || turn > .03)
                    errors.Add($"Rail tangent/position discontinuity {i} at {samples[i].x:0.##},{samples[i].y:0.##}");
            }
        }

        // ================================================================= geometry ported from the reference

        /// <summary>
        /// The heading at each sample, as the chord INTO it (the chord out of it for the first of a run). NaN where a
        /// sample has no neighbour within 0.3 tiles, which only happens at the cut edges of a crop.
        /// </summary>
        private static double[] ChordAngles(JSample[] samples)
        {
            var angles = new double[samples.Length];
            for (var i = 0; i < samples.Length; i++)
            {
                angles[i] = double.NaN;
                if (i > 0 && Chord(samples[i - 1], samples[i], out var back)) angles[i] = back;
                else if (i + 1 < samples.Length && Chord(samples[i], samples[i + 1], out var ahead)) angles[i] = ahead;
            }
            return angles;
        }

        private static bool Chord(JSample a, JSample b, out double angle)
        {
            double dx = b.x - (double)a.x, dy = b.y - (double)a.y;
            var len = Hypot(dx, dy);
            angle = Math.Atan2(dy, dx);
            return len > 1e-6 && len <= .3;
        }

        /// <summary>
        /// riverfrontRail.ts:27 — every tile whose CENTRE lies within <c>railHalf + √½</c> of a baked sample. The ±4
        /// window only bounds the search.
        /// </summary>
        private static bool[] SweptRail(JSample[] samples, int railHalf, int w, int h)
        {
            var reserved = new bool[w * h];
            var reach = railHalf + Math.Sqrt(.5);
            foreach (var p in samples)
            {
                int px = (int)Math.Floor(p.x), py = (int)Math.Floor(p.y);
                for (var y = py - 4; y <= py + 4; y++)
                for (var x = px - 4; x <= px + 4; x++)
                    if (x >= 0 && y >= 0 && x < w && y < h && Hypot(x + .5 - p.x, y + .5 - p.y) <= reach)
                        reserved[y * w + x] = true;
            }
            return reserved;
        }

        /// <summary>validateRiverfront.ts:48-49 and exportUnity.ts accessibleFromSpawn: four-way flood from the spawn.</summary>
        private static bool[] Flood(JPoint spawn, int w, int h, Func<int, int, bool> passable, out int count)
        {
            var seen = new bool[w * h];
            count = 0;
            if (spawn == null || spawn.x < 0 || spawn.y < 0 || spawn.x >= w || spawn.y >= h) return seen;
            var queue = new int[w * h];
            var end = 1;
            queue[0] = spawn.y * w + spawn.x;
            seen[queue[0]] = true;
            for (var at = 0; at < end; at++)
            {
                int x = queue[at] % w, y = queue[at] / w;
                count++;
                Visit(x - 1, y); Visit(x + 1, y); Visit(x, y - 1); Visit(x, y + 1);
            }
            return seen;

            void Visit(int x, int y)
            {
                if (x < 0 || y < 0 || x >= w || y >= h || seen[y * w + x] || !passable(x, y)) return;
                seen[y * w + x] = true;
                queue[end++] = y * w + x;
            }
        }

        /// <summary>riverfront.ts <c>lineTiles</c>: the tiles a polyline crosses, in order.</summary>
        private static List<(int x, int y)> LineTiles(JPoint[] points)
        {
            var tiles = new List<(int, int)>();
            if (points == null || points.Length == 0) return tiles;
            for (var i = 1; i < points.Length; i++)
            {
                int x = points[i - 1].x, y = points[i - 1].y, gx = points[i].x, gy = points[i].y;
                int dx = Math.Abs(gx - x), dy = Math.Abs(gy - y), sx = Math.Sign(gx - x), sy = Math.Sign(gy - y);
                var error = dx - dy;
                while (x != gx || y != gy)
                {
                    if (tiles.Count == 0 || tiles[tiles.Count - 1] != (x, y)) tiles.Add((x, y));
                    var e = error * 2;
                    if (e > -dy) { error -= dy; x += sx; } else { error += dx; y += sy; }
                }
            }
            tiles.Add((points[points.Length - 1].x, points[points.Length - 1].y));
            return tiles;
        }

        /// <summary>
        /// parcelGeometry.ts <c>doorRect</c>: a door is 1x3 on an east or west wall and 3x1 on a north or south one;
        /// a building with no authored door gets one centred on the wall it faces.
        /// </summary>
        public static JRect DoorRect(JBuilding b)
        {
            var r = b.rect;
            var side = b.facing == "E" || b.facing == "W";
            int x, y;
            if (b.hasDoor && b.door != null) { x = b.door.x; y = b.door.y; }
            else if (side) { x = b.facing == "E" ? r.x + r.w - 1 : r.x; y = r.y + r.h / 2 - 1; }
            else { x = r.x + r.w / 2 - 1; y = b.facing == "S" ? r.y + r.h - 1 : r.y; }
            return new JRect {x = x, y = y, w = side ? 1 : 3, h = side ? 3 : 1};
        }

        /// <summary>parcelGeometry.ts <c>doorOutside</c>: the door's centre, one tile out through the facing wall.</summary>
        private static void DoorOutside(string facing, JRect door, out double x, out double y)
        {
            int dx = facing == "E" ? 1 : facing == "W" ? -1 : 0, dy = facing == "S" ? 1 : facing == "N" ? -1 : 0;
            x = door.x + door.w / 2.0 + dx;
            y = door.y + door.h / 2.0 + dy;
        }

        private static bool OnFacingWall(string facing, JRect d, JRect b) =>
            facing == "N" ? d.y == b.y :
            facing == "S" ? d.y + d.h == b.y + b.h :
            facing == "E" ? d.x + d.w == b.x + b.w :
            d.x == b.x;

        /// <summary>Rule 6: a drive's clearance is the reference's own lopsided rect, 2 tiles before and 3 after.</summary>
        private static bool OnAServiceDrive(Box box, JPolyline[] drives)
        {
            foreach (var drive in drives)
            {
                var pts = drive != null ? drive.points : null;
                if (pts == null) continue;
                for (var i = 1; i < pts.Length; i++)
                    if (box.Overlaps(new Box(Math.Min(pts[i - 1].x, pts[i].x) - 2, Math.Min(pts[i - 1].y, pts[i].y) - 2,
                            Math.Abs(pts[i - 1].x - pts[i].x) + 5, Math.Abs(pts[i - 1].y - pts[i].y) + 5)))
                        return true;
            }
            return false;
        }

        /// <summary>Rule 12: everything an entrance strip must stay clear of, each with the id the message names.</summary>
        private static IEnumerable<Obstacle> Obstacles(JBuilding self, JBuilding[] buildings, JProp[] props,
            JRect[] yards, JResource[] resources)
        {
            foreach (var q in buildings)
                if (q != null && q.rect != null && q.id != self.id) yield return new Obstacle(q.id, Box.Of(q.rect));
            foreach (var p in props)
                if (p != null && p.rect != null) yield return new Obstacle(p.id, Box.Of(p.rect));
            for (var i = 0; i < yards.Length; i++)
                if (yards[i] != null) yield return new Obstacle("yard:" + i, Box.Of(yards[i]));
            foreach (var res in resources)
                if (res != null) yield return new Obstacle("resource:" + res.item, new Box(res.x, res.y, res.w, res.h));
        }

        /// <summary>The reference treats a road as its first two points; every authored road is one straight segment.</summary>
        private static List<(JPoint a, JPoint z)> Segments(JPolyline[] polylines)
        {
            var list = new List<(JPoint, JPoint)>();
            foreach (var p in polylines ?? Array.Empty<JPolyline>())
                if (p != null && p.points != null && p.points.Length >= 2) list.Add((p.points[0], p.points[1]));
            return list;
        }

        private static bool FirstTile(JRect r, Func<int, int, bool> hit, out int fx, out int fy)
        {
            for (var y = r.y; y < r.y + r.h; y++)
            for (var x = r.x; x < r.x + r.w; x++)
                if (hit(x, y)) { fx = x; fy = y; return true; }
            fx = fy = 0;
            return false;
        }

        private static bool Within(JRect inner, JRect outer) =>
            inner.x >= outer.x && inner.y >= outer.y &&
            inner.x + inner.w <= outer.x + outer.w && inner.y + inner.h <= outer.y + outer.h;

        private static bool Same(JRect a, JRect b) => a.x == b.x && a.y == b.y && a.w == b.w && a.h == b.h;

        private static double Hypot(double x, double y) => Math.Sqrt(x * x + y * y);
    }
}
