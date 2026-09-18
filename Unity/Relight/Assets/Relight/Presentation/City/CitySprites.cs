using UnityEngine;

namespace Relight.Presentation
{
    /// <summary>
    /// Correction pass C6. The city's whole procedural art set: ONE 80x64 texture holding every shape the presenter
    /// draws, plus one small repeating grid texture for paved surfaces.
    ///
    /// Why an atlas rather than a sprite per shape: every <see cref="SpriteRenderer"/> that shares a texture and a
    /// material can batch. The city is ~4,800 renderers, so one texture is the difference between a handful of draw
    /// calls and thousands of them. Each cell is 16x16 pixels and every sprite is created at 16 pixels per unit, so
    /// a sprite is exactly one world unit (one tile) square with its pivot at the bottom-left corner: the presenter
    /// then sizes anything by setting <c>localScale = (w, h, 1)</c> and never touches a mesh.
    ///
    /// Flat shapes (the plain quad, the door cross, the door outline, the disc) are drawn white so the renderer's
    /// colour tints them. The props are drawn in the reference's own colours (riverfrontDraw.ts:52-60) because a
    /// single tint cannot carry a tree's trunk and three canopy greens; those renderers stay white.
    ///
    /// The textures are created at runtime, are never written to disk, and are released with the presenter.
    /// </summary>
    internal sealed class CitySprites
    {
        public const int Cell = 16;
        private const int Cols = 5;
        private const int Rows = 4;

        private readonly System.Collections.Generic.List<Sprite> _owned = new System.Collections.Generic.List<Sprite>();
        private Texture2D _atlas;
        private Texture2D _grid;
        private Color32[] _pixels;

        /// <summary>A plain white square, pivot at the bottom-left: the tinted quad nearly everything is drawn with.</summary>
        public Sprite Quad { get; private set; }
        /// <summary>The same square with a centred pivot, for rotated segments (roads, paths, drives, the tram).</summary>
        public Sprite QuadCentre { get; private set; }
        /// <summary>A sealed door: the dark doorway with the boarded cross across it (riverfrontDraw.ts:37).</summary>
        public Sprite DoorSealed { get; private set; }
        /// <summary>An enterable door: the dark doorway outlined in the interior teal.</summary>
        public Sprite DoorOpen { get; private set; }
        /// <summary>A white disc, pivot at the bottom-left of its 1x1 box.</summary>
        public Sprite Disc { get; private set; }
        /// <summary>The same disc with a centred pivot (drive end caps, the Founders Court bulb).</summary>
        public Sprite DiscCentre { get; private set; }
        public Sprite Statue { get; private set; }
        public Sprite Tree { get; private set; }
        public Sprite Container { get; private set; }
        public Sprite Excavator { get; private set; }
        public Sprite Tank { get; private set; }
        public Sprite Converter { get; private set; }
        public Sprite Furniture { get; private set; }
        public Sprite Bed { get; private set; }
        public Sprite Fence { get; private set; }
        public Sprite Gate { get; private set; }
        public Sprite Crane { get; private set; }
        public Sprite Rock { get; private set; }
        public Sprite Debris { get; private set; }
        public Sprite Substation { get; private set; }

        /// <summary>A 4x4-tile repeating paving grid (riverfrontDraw.ts:29), for <see cref="SpriteDrawMode.Tiled"/>.</summary>
        public Sprite PavingGrid { get; private set; }

        /// <summary>The one texture every flat shape and prop comes from.</summary>
        public Texture2D Atlas => _atlas;

        public static CitySprites Create()
        {
            var s = new CitySprites();
            s.Build();
            return s;
        }

        /// <summary>The prop sprite for a reference prop kind, or <see cref="Rock"/> for anything unrecognised.</summary>
        public Sprite Prop(string kind, string id)
        {
            switch (kind)
            {
                case "statue": return Statue;
                case "tree": return Tree;
                case "container": return Container;
                case "excavator": return Excavator;
                case "tank": return Tank;
                case "converter": return Converter;
                case "furniture": return id != null && id.EndsWith(":bed") ? Bed : Furniture;
                case "fence": return Fence;
                case "gate": return Gate;
                case "crane": return Crane;
                case "rock": return Rock;
                default: return Debris;
            }
        }

        public void Dispose()
        {
            foreach(var sprite in _owned)Release(sprite);
            _owned.Clear();
            Release(_atlas);
            Release(_grid);
            _atlas = null;
            _grid = null;
        }

        private static void Release(Object value)
        {
            if(value==null)return;
            if(Application.isPlaying)Object.Destroy(value);else Object.DestroyImmediate(value);
        }

        // ----------------------------------------------------------------- construction

        private void Build()
        {
            var w = Cols * Cell;
            var h = Rows * Cell;
            _atlas = new Texture2D(w, h, TextureFormat.RGBA32, false)
            {
                name = "Relight City Atlas",
                filterMode = FilterMode.Point,
                wrapMode = TextureWrapMode.Clamp,
                hideFlags = HideFlags.HideAndDontSave,
            };
            _pixels = new Color32[w * h];   // starts fully transparent (all zero)

            // 0 flat quad ------------------------------------------------- tinted by the renderer
            Box(0, 0, 0, Cell, Cell, Color.white);
            // 1 sealed door: the dark doorway with the boarded cross (riverfrontDraw.ts:37), colours baked in so one
            //   renderer draws the whole marker instead of a base quad plus an overlay (479 objects saved).
            Box(1, 0, 0, Cell, Cell, CityPalette.Door);
            Line(1, 1, 1, Cell - 2, Cell - 2, 2f, CityPalette.DoorSealed);
            Line(1, Cell - 2, 1, 1, Cell - 2, 2f, CityPalette.DoorSealed);
            // 2 enterable door: the dark doorway outlined in the interior teal
            Box(2, 0, 0, Cell, Cell, CityPalette.Door);
            Border(2, 2f, CityPalette.DoorEnterable);
            // 3 disc
            Disc3(3, Cell * 0.5f, Cell * 0.5f, Cell * 0.5f - 0.5f, Color.white);

            // 4 statue: plinth, pale body, robe, head
            Box(4, 0, 0, Cell, Cell, CityPalette.Rgb(0x485a58));
            Box(4, 2, 2, Cell - 4, Cell - 4, CityPalette.Rgb(0xb4b6a0));
            Ellipse(4, 8, 7, 3.2f, 4.8f, CityPalette.Rgb(0x50665e));
            Disc3(4, 8, 12.5f, 2.1f, CityPalette.Rgb(0x9da994));

            // 5 tree: shadow, trunk, three canopy greens (drawn bottom-up, so the trunk is at the bottom)
            Ellipse(5, 8, 3.5f, 6.5f, 2.6f, CityPalette.Rgb(0x243d2c, 0.6f));
            Box(5, 7, 2, 2, 6, CityPalette.Rgb(0x6e5f45));
            Disc3(5, 6, 9, 4.4f, CityPalette.Rgb(0x718057));
            Disc3(5, 10, 10, 4.2f, CityPalette.Rgb(0x51683e));
            Disc3(5, 8, 13, 3.6f, CityPalette.Rgb(0x405f38));

            // 6 container: dark frame, crate body, ribs, base bar
            Box(6, 0, 0, Cell, Cell, CityPalette.Rgb(0x303f3e));
            Box(6, 2, 2, Cell - 4, Cell - 4, CityPalette.Rgb(0x876c50));
            for (var x = 3; x < Cell - 3; x += 3) Box(6, x, 3, 1, Cell - 6, CityPalette.Rgb(0xc0a27c, 0.6f));
            Box(6, 2, 1, Cell - 4, 2, CityPalette.Rgb(0x243a3b));

            // 7 excavator: tracks, yellow body, cab, boom, counterweight
            Box(7, 0, 0, Cell, Cell, CityPalette.Rgb(0x343e3c));
            Box(7, 1, 2, 7, Cell - 4, CityPalette.Rgb(0xb59546));
            Box(7, 2, 8, 5, 5, CityPalette.Rgb(0x344c4e));
            Line(7, 6, 6, 13, 13, 2.4f, CityPalette.Rgb(0xc8a65e));
            Box(7, Cell - 5, Cell - 5, 5, 5, CityPalette.Rgb(0x757863));

            // 8 / 9 tank and converter: the same silhouette, two fills (riverfrontDraw.ts:56)
            TankShape(8, CityPalette.Rgb(0xa1ada1));
            TankShape(9, CityPalette.Rgb(0x496d67));

            // 10 furniture, 11 bed
            Box(10, 0, 0, Cell, Cell, CityPalette.Rgb(0x937452));
            Box(10, 2, 0, 3, 4, CityPalette.Rgb(0x3e4135));
            Box(10, Cell - 5, 0, 3, 4, CityPalette.Rgb(0x3e4135));
            Box(11, 0, 0, Cell, Cell, CityPalette.Rgb(0x526a70));
            Box(11, 1, Cell - 6, Cell - 2, 5, CityPalette.Rgb(0xd9d1b6));

            // 12 fence, 17 gate: posts and diagonal slats behind a light frame
            Box(12, 0, 0, Cell, Cell, CityPalette.Rgb(0x405347));
            for (var x = 0; x < Cell; x += 4) Line(12, x, 0, x + 3, Cell - 1, 1.4f, CityPalette.Rgb(0xb0ab8a, 0.75f));
            Border(12, 1.6f, CityPalette.Rgb(0xb0ab8a));
            Box(17, 0, 0, Cell, Cell, CityPalette.Rgb(0x405347));
            Box(17, Cell / 2 - 1, 0, 2, Cell, CityPalette.Rgb(0xb0ab8a));
            Border(17, 1.6f, CityPalette.Rgb(0xb0ab8a));

            // 13 crane: yard pad, mast and jib
            Box(13, 0, 0, Cell, Cell, CityPalette.Rgb(0x7f704d));
            Box(13, 2, 2, 3, Cell - 4, CityPalette.Rgb(0xc1a56a));
            Box(13, 2, Cell - 5, Cell - 4, 3, CityPalette.Rgb(0xc1a56a));
            Box(13, Cell - 4, 4, 1, Cell - 9, CityPalette.Rgb(0x263d3e));

            // 14 rock, 15 debris: the reference's default triangle
            Triangle(14, 0.5f, 1, 5.5f, 14, 15, 3, CityPalette.Rgb(0x898779));
            Triangle(15, 0.5f, 1, 5.5f, 14, 15, 3, CityPalette.Rgb(0x8d8265));

            // 16 substation: three ribbed stacks with an indicator corner (riverfrontDraw.ts:67)
            Box(16, 0, 0, Cell, Cell, CityPalette.Substation);
            for (var j = 0; j < 3; j++)
            {
                var x = 2 + j * 5;
                Box(16, x, 2, 3, Cell - 5, CityPalette.SubstationRib);
                Box(16, x, Cell - 6, 2, 1, CityPalette.Rgb(0x2f494a));
            }
            Box(16, Cell - 3, Cell - 3, 2, 2, CityPalette.Rgb(0x2a4143));

            _atlas.SetPixels32(_pixels);
            _atlas.Apply(false, false);

            Quad = Make(0); DoorSealed = Make(1); DoorOpen = Make(2); Disc = Make(3);
            QuadCentre = Make(0, 0.5f); DiscCentre = Make(3, 0.5f);
            Statue = Make(4); Tree = Make(5); Container = Make(6); Excavator = Make(7);
            Tank = Make(8); Converter = Make(9); Furniture = Make(10); Bed = Make(11);
            Fence = Make(12); Crane = Make(13); Rock = Make(14); Debris = Make(15);
            Substation = Make(16); Gate = Make(17);

            BuildGrid();
            _pixels = null;
        }

        /// <summary>
        /// The repeating paving grid. Its own texture because it must wrap, and an atlas cell cannot: the sprite is
        /// four world units square and is drawn with <see cref="SpriteDrawMode.Tiled"/> over a yard or a square.
        /// </summary>
        private void BuildGrid()
        {
            const int size = 16;
            _grid = new Texture2D(size, size, TextureFormat.RGBA32, false)
            {
                name = "Relight Paving Grid",
                filterMode = FilterMode.Point,
                wrapMode = TextureWrapMode.Repeat,
                hideFlags = HideFlags.HideAndDontSave,
            };
            var px = new Color32[size * size];
            var line = (Color32)Color.white;
            for (var i = 0; i < size; i++)
            {
                px[i] = line;                    // bottom edge
                px[i * size] = line;             // left edge
            }
            _grid.SetPixels32(px);
            _grid.Apply(false, false);
            // 4 pixels per unit over a 16 px texture = one repeat every 4 tiles, which is the reference's grid step.
            PavingGrid = Sprite.Create(_grid, new Rect(0, 0, size, size), new Vector2(0f, 0f), 4f, 0,
                SpriteMeshType.FullRect);
            _owned.Add(PavingGrid);
            PavingGrid.name = "paving-grid";
        }

        private Sprite Make(int cell, float pivot = 0f)
        {
            var col = cell % Cols;
            var row = cell / Cols;
            var s = Sprite.Create(_atlas, new Rect(col * Cell, row * Cell, Cell, Cell), new Vector2(pivot, pivot),
                Cell, 0, SpriteMeshType.FullRect);
            s.name = "city-" + cell + (pivot > 0f ? "-c" : string.Empty);
            s.hideFlags = HideFlags.HideAndDontSave;
            _owned.Add(s);
            return s;
        }

        // ----------------------------------------------------------------- pixel helpers (cell-local, Y up)

        private void Plot(int cell, int x, int y, Color c)
        {
            if (x < 0 || y < 0 || x >= Cell || y >= Cell) return;
            var col = cell % Cols;
            var row = cell / Cols;
            var i = (row * Cell + y) * (Cols * Cell) + col * Cell + x;
            if (c.a >= 0.999f) { _pixels[i] = c; return; }
            // Straight source-over, so a translucent shadow reads against what is already in the cell.
            var dst = _pixels[i];
            var da = dst.a / 255f;
            var outA = c.a + da * (1f - c.a);
            if (outA <= 0f) { _pixels[i] = new Color32(0, 0, 0, 0); return; }
            var r = (c.r * c.a + dst.r / 255f * da * (1f - c.a)) / outA;
            var g = (c.g * c.a + dst.g / 255f * da * (1f - c.a)) / outA;
            var b = (c.b * c.a + dst.b / 255f * da * (1f - c.a)) / outA;
            _pixels[i] = new Color(r, g, b, outA);
        }

        private void Box(int cell, int x, int y, int w, int h, Color c)
        {
            for (var yy = y; yy < y + h; yy++)
            for (var xx = x; xx < x + w; xx++)
                Plot(cell, xx, yy, c);
        }

        private void Border(int cell, float thickness, Color c)
        {
            var t = Mathf.Max(1, Mathf.RoundToInt(thickness));
            Box(cell, 0, 0, Cell, t, c);
            Box(cell, 0, Cell - t, Cell, t, c);
            Box(cell, 0, 0, t, Cell, c);
            Box(cell, Cell - t, 0, t, Cell, c);
        }

        private void Disc3(int cell, float cx, float cy, float r, Color c)
        {
            var r2 = r * r;
            for (var y = 0; y < Cell; y++)
            for (var x = 0; x < Cell; x++)
            {
                var dx = x + 0.5f - cx;
                var dy = y + 0.5f - cy;
                if (dx * dx + dy * dy <= r2) Plot(cell, x, y, c);
            }
        }

        private void Ellipse(int cell, float cx, float cy, float rx, float ry, Color c)
        {
            for (var y = 0; y < Cell; y++)
            for (var x = 0; x < Cell; x++)
            {
                var dx = (x + 0.5f - cx) / rx;
                var dy = (y + 0.5f - cy) / ry;
                if (dx * dx + dy * dy <= 1f) Plot(cell, x, y, c);
            }
        }

        private void Line(int cell, float x0, float y0, float x1, float y1, float thickness, Color c)
        {
            var half = thickness * 0.5f;
            var vx = x1 - x0;
            var vy = y1 - y0;
            var len2 = vx * vx + vy * vy;
            if (len2 <= 0f) return;
            for (var y = 0; y < Cell; y++)
            for (var x = 0; x < Cell; x++)
            {
                var px = x + 0.5f - x0;
                var py = y + 0.5f - y0;
                var t = Mathf.Clamp01((px * vx + py * vy) / len2);
                var dx = px - vx * t;
                var dy = py - vy * t;
                if (dx * dx + dy * dy <= half * half) Plot(cell, x, y, c);
            }
        }

        private void Triangle(int cell, float ax, float ay, float bx, float by, float cx, float cy, Color c)
        {
            for (var y = 0; y < Cell; y++)
            for (var x = 0; x < Cell; x++)
            {
                var px = x + 0.5f;
                var py = y + 0.5f;
                var d1 = Side(px, py, ax, ay, bx, by);
                var d2 = Side(px, py, bx, by, cx, cy);
                var d3 = Side(px, py, cx, cy, ax, ay);
                var neg = d1 < 0 || d2 < 0 || d3 < 0;
                var pos = d1 > 0 || d2 > 0 || d3 > 0;
                if (!(neg && pos)) Plot(cell, x, y, c);
            }
        }

        private static float Side(float px, float py, float ax, float ay, float bx, float by) =>
            (px - bx) * (ay - by) - (ax - bx) * (py - by);

        private void TankShape(int cell, Color body)
        {
            Ellipse(cell, 8, 6, 8, 6, CityPalette.Rgb(0x273d40));
            Ellipse(cell, 8, 8, 7.2f, 5.2f, body);
            Ellipse(cell, 8, 8, 5.2f, 3.2f, CityPalette.Rgb(0x32494a, 0.55f));
            Ellipse(cell, 8, 8, 4.2f, 2.4f, body);
        }
    }
}
