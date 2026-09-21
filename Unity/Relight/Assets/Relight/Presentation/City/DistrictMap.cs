using System;
using System.Collections.Generic;
using Relight.Sim;
using UnityEngine;
using UnityEngine.Rendering;

namespace Relight.Presentation
{
    /// <summary>
    /// A developer's picture of the nine districts, shared by the Scene-view gizmo (<c>Assets/Editor/DistrictGizmo.cs</c>)
    /// and the in-game debug overlay (<see cref="DistrictOverlay"/>), so both draw the same answer.
    ///
    /// A district is every tile whose nearest substation site is the same one (ALWAYS_DARK_SPEC.md §6). The rule is
    /// <see cref="PowerGrid.SubstationOf"/>'s own: squared centre distance, ties to the earlier site in export
    /// order. A tile is measured from its centre.
    ///
    /// Read-only. It reads the geometry and the sites and owns nothing the sim reads; nothing here is saved.
    /// </summary>
    public sealed class DistrictMap
    {
        /// <summary>Tiles the game camera shows at once (about 35.6 × 20), the unit "screens" is counted in.</summary>
        public const float ScreenTilesWide = 35.6f, ScreenTilesHigh = 20f;
        /// <summary>The engineer's walking speed in tiles a second, for the walk-across figure.</summary>
        public const float WalkTilesPerSecond = 6f;

        public sealed class District
        {
            public int Index;
            public string Name;
            public SiteRecord Substation;
            public int Tiles, Walkable, Street;
            /// <summary>Where the stats go: the authored name label inside the cell, else the substation.</summary>
            public Vector2 LabelTile;
            public Color Colour;
            /// <summary>Street tiles in scan order, y * W + x: what a roamer preview picks from.</summary>
            public readonly List<int> StreetTiles = new List<int>();

            /// <summary>The side of a square with this much walkable ground, in tiles.</summary>
            public float Across => Mathf.Sqrt(Walkable);
            public float WalkSeconds => Across / WalkTilesPerSecond;
            public float Screens => Walkable / (ScreenTilesWide * ScreenTilesHigh);
        }

        public int W, H;
        /// <summary>District index per tile, y * W + x; 255 where there are no substations at all.</summary>
        public byte[] Owner = Array.Empty<byte>();
        public readonly List<District> Districts = new List<District>();
        /// <summary>Tile edges where the district changes: x, y, then 1 for an edge running south, 0 for east.</summary>
        public readonly List<Vector3Int> Edges = new List<Vector3Int>();

        private static readonly Color[] Palette =
        {
            new Color(1.00f, 0.80f, 0.30f), new Color(0.35f, 0.80f, 1.00f), new Color(1.00f, 0.45f, 0.35f),
            new Color(0.60f, 0.95f, 0.45f), new Color(0.85f, 0.55f, 1.00f), new Color(1.00f, 0.60f, 0.80f),
            new Color(0.45f, 0.95f, 0.85f), new Color(0.95f, 0.95f, 0.45f), new Color(0.65f, 0.70f, 1.00f),
        };

        public static DistrictMap Build(SimContext ctx)
        {
            var g = ctx.Geometry;
            var subs = PowerGrid.SubstationSites(ctx);
            var map = new DistrictMap { W = g.Width, H = g.Height };
            map.Owner = new byte[map.W * map.H];
            var n = Mathf.Min(subs.Count, 255);
            if (n == 0) { for (var i = 0; i < map.Owner.Length; i++) map.Owner[i] = 255; return map; }

            var cx = new double[n];
            var cy = new double[n];
            for (var i = 0; i < n; i++)
            {
                var c = subs[i].Centre;
                cx[i] = c.X; cy[i] = c.Y;
                map.Districts.Add(new District
                {
                    Index = i, Substation = subs[i], Colour = Palette[i % Palette.Length],
                    Name = string.IsNullOrEmpty(subs[i].Name) ? subs[i].Id : subs[i].Name,
                    LabelTile = new Vector2((float)c.X, (float)c.Y),
                });
            }

            for (var y = 0; y < map.H; y++)
            for (var x = 0; x < map.W; x++)
            {
                var best = Nearest(cx, cy, n, x + 0.5, y + 0.5);
                map.Owner[y * map.W + x] = (byte)best;
                var d = map.Districts[best];
                d.Tiles++;
                if (!Ground.Walkable(ctx, x, y)) continue;   // the sim's own rule: not solid, not river
                d.Walkable++;
                if (g.TileAt(x, y) != TileClass.Street) continue;
                d.Street++;
                d.StreetTiles.Add(y * map.W + x);
            }

            // The city's own name for the cell: the authored label that falls inside it.
            foreach (var label in ctx.Sites.OfKind(SiteKind.Label))
            {
                var c = label.Centre;
                var d = map.Districts[Nearest(cx, cy, n, c.X, c.Y)];
                if (!string.IsNullOrEmpty(label.Name)) d.Name = label.Name;
                d.LabelTile = new Vector2((float)c.X, (float)c.Y);
            }

            for (var y = 0; y < map.H; y++)
            for (var x = 0; x < map.W; x++)
            {
                var o = map.Owner[y * map.W + x];
                if (x > 0 && map.Owner[y * map.W + x - 1] != o) map.Edges.Add(new Vector3Int(x, y, 1));
                if (y > 0 && map.Owner[(y - 1) * map.W + x] != o) map.Edges.Add(new Vector3Int(x, y, 0));
            }
            return map;
        }

        private static int Nearest(double[] cx, double[] cy, int n, double px, double py)
        {
            var best = 0;
            var score = double.PositiveInfinity;
            for (var i = 0; i < n; i++)
            {
                var dx = cx[i] - px;
                var dy = cy[i] - py;
                var dd = dx * dx + dy * dy;
                if (dd >= score) continue;   // strict, so the earlier site keeps a tie, as SubstationOf does
                score = dd;
                best = i;
            }
            return best;
        }

        /// <summary>The district a tile position is in, or null off the map.</summary>
        public District At(double tx, double ty)
        {
            var x = (int)Math.Floor(tx);
            var y = (int)Math.Floor(ty);
            if (x < 0 || y < 0 || x >= W || y >= H) return null;
            var o = Owner[y * W + x];
            return o < Districts.Count ? Districts[o] : null;
        }

        public string Stats(District d) =>
            $"{d.Walkable:N0} walkable · {d.Street:N0} street\n{d.Screens:0} screens · {d.WalkSeconds:0} s to walk across";

        // ------------------------------------------------------------------ roamer preview

        /// <summary>
        /// How many roamers a preview puts in a district. A PREVIEW of a proposal in ENEMY-THREAT-AUDIT.md §3.3, to
        /// judge density by eye. It is not a game rule and places no enemy.
        /// </summary>
        public enum Preview { Off = 0, FlatFour = 1, PerFiveThousandStreet = 2, PerTwoAndAHalfThousandStreet = 3 }

        public static string PreviewName(Preview p)
        {
            switch (p)
            {
                case Preview.FlatFour: return "4 in every district";
                case Preview.PerFiveThousandStreet: return "1 per 5,000 street tiles";
                case Preview.PerTwoAndAHalfThousandStreet: return "1 per 2,500 street tiles";
                default: return "off";
            }
        }

        public static int PreviewCount(District d, Preview p)
        {
            switch (p)
            {
                case Preview.FlatFour: return 4;
                case Preview.PerFiveThousandStreet: return Mathf.Max(1, Mathf.RoundToInt(d.Street / 5000f));
                case Preview.PerTwoAndAHalfThousandStreet: return Mathf.Max(1, Mathf.RoundToInt(d.Street / 2500f));
                default: return 0;
            }
        }

        /// <summary>Street-tile centres for a preview, the same every time for the same map.</summary>
        public List<Vector2> PreviewTiles(District d, Preview p)
        {
            var list = new List<Vector2>();
            var count = Mathf.Min(PreviewCount(d, p), d.StreetTiles.Count);
            if (count == 0) return list;
            var rng = new System.Random(7919 * (d.Index + 1) + 17);
            for (var k = 0; k < count; k++)
            {
                var t = d.StreetTiles[rng.Next(d.StreetTiles.Count)];
                list.Add(new Vector2(t % W + 0.5f, t / W + 0.5f));
            }
            return list;
        }

        // ------------------------------------------------------------------ meshes

        /// <summary>
        /// The borders as one mesh of quads, <paramref name="thickness"/> tiles wide, vertex-coloured white.
        /// <paramref name="bothWindings"/> with back normals is what <c>Gizmos.DrawMesh</c> needs to be seen at all.
        /// </summary>
        public Mesh BorderMesh(float thickness, float z, bool bothWindings)
        {
            var h = thickness * 0.5f;
            var quads = new List<Vector3>(Edges.Count * 4);
            foreach (var e in Edges)
            {
                float x0, y0, x1, y1;
                if (e.z == 1) { x0 = e.x - h; x1 = e.x + h; y0 = e.y - h; y1 = e.y + 1 + h; }
                else { x0 = e.x - h; x1 = e.x + 1 + h; y0 = e.y - h; y1 = e.y + h; }
                quads.Add(new Vector3(x0, -y0, z));
                quads.Add(new Vector3(x1, -y0, z));
                quads.Add(new Vector3(x1, -y1, z));
                quads.Add(new Vector3(x0, -y1, z));
            }
            return QuadMesh(quads, bothWindings);
        }

        public static Mesh QuadMesh(List<Vector3> quads, bool bothWindings)
        {
            var mesh = new Mesh { hideFlags = HideFlags.HideAndDontSave, indexFormat = IndexFormat.UInt32 };
            var per = bothWindings ? 12 : 6;
            var tris = new int[quads.Count / 4 * per];
            for (int q = 0, t = 0; q < quads.Count; q += 4)
            {
                tris[t++] = q; tris[t++] = q + 1; tris[t++] = q + 2;
                tris[t++] = q; tris[t++] = q + 2; tris[t++] = q + 3;
                if (!bothWindings) continue;
                tris[t++] = q; tris[t++] = q + 2; tris[t++] = q + 1;
                tris[t++] = q; tris[t++] = q + 3; tris[t++] = q + 2;
            }
            var normals = new Vector3[quads.Count];
            var colours = new Color[quads.Count];
            for (var i = 0; i < normals.Length; i++) { normals[i] = Vector3.back; colours[i] = Color.white; }
            mesh.SetVertices(quads);
            mesh.SetTriangles(tris, 0);
            mesh.normals = normals;
            mesh.colors = colours;
            mesh.RecalculateBounds();
            return mesh;
        }

        /// <summary>One texel per tile, each district in its own colour at <paramref name="alpha"/>. Row 0 is the SOUTH row.</summary>
        public Texture2D TintTexture(float alpha)
        {
            var tex = new Texture2D(W, H, TextureFormat.RGBA32, false)
            {
                hideFlags = HideFlags.HideAndDontSave, filterMode = FilterMode.Point, wrapMode = TextureWrapMode.Clamp,
            };
            var px = new Color32[W * H];
            for (var y = 0; y < H; y++)
            for (var x = 0; x < W; x++)
            {
                var o = Owner[y * W + x];
                var c = o < Districts.Count ? Districts[o].Colour : Color.clear;
                c.a = o < Districts.Count ? alpha : 0f;
                px[(H - 1 - y) * W + x] = c;
            }
            tex.SetPixels32(px);
            tex.Apply(false, true);
            return tex;
        }
    }
}
