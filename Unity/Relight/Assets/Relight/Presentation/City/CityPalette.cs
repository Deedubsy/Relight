using UnityEngine;

namespace Relight.Presentation
{
    /// <summary>
    /// Correction pass C6. Every colour the city is drawn in, copied from the reference draw pass
    /// (<c>packages/game/src/riverfrontDraw.ts</c>) and the two art generators
    /// (<c>packages/game/public/art/riverfront/build-assets.py</c> and <c>build-downtown.py</c>, which are what the
    /// 68 SVGs were generated from — so these ARE the artwork's colours, not an approximation of it).
    ///
    /// This exists because the SVGs cannot be used: Unity has no vector-graphics package in this project's manifest
    /// and this machine has no rasteriser, so the city is drawn procedurally and every roof still carries its
    /// <c>roofKey</c> for the day the sprites arrive (see <see cref="Relight.World.CityArtSet"/>).
    /// </summary>
    internal static class CityPalette
    {
        public static Color Rgb(int hex) => new Color(
            ((hex >> 16) & 0xFF) / 255f, ((hex >> 8) & 0xFF) / 255f, (hex & 0xFF) / 255f, 1f);

        public static Color Rgb(int hex, float alpha)
        {
            var c = Rgb(hex);
            c.a = alpha;
            return c;
        }

        // ---------------------------------------------------------------- ground and surfaces (riverfrontDraw.ts)
        /// <summary>riverfrontDraw.ts:15 — the river's base tile.</summary>
        public static readonly Color River = Rgb(0x204853);
        /// <summary>riverfrontDraw.ts:15 — every non-river base tile (the tilemap already paints these).</summary>
        public static readonly Color Land = Rgb(0x52614b);
        /// <summary>riverfrontDraw.ts:17 — civic forecourts and service alleys.</summary>
        public static readonly Color ServiceArea = Rgb(0x70776a);
        public static readonly Color ServiceGrid = Rgb(0x434f47, 0.40f);
        /// <summary>riverfrontDraw.ts:29 — factory yards and paved squares.</summary>
        public static readonly Color Paving = Rgb(0x7c7b68);
        public static readonly Color PavingGrid = Rgb(0x4b5751, 0.55f);
        public static readonly Color PavingBorder = Rgb(0xa69e82, 0.60f);
        /// <summary>riverfrontDraw.ts:20-22 — the pavement band and the carriageway inside it.</summary>
        public static readonly Color Pavement = Rgb(0x858b7e);
        public static readonly Color Carriageway = Rgb(0x46504d);
        /// <summary>riverfrontDraw.ts:28 — garden/approach paths, 1.4 tiles wide at 80% alpha.</summary>
        public static readonly Color Path = Rgb(0x929683, 0.80f);
        /// <summary>riverfrontDraw.ts:27 — the reserved tram boulevard, 2.8 tiles wide.</summary>
        public static readonly Color Tram = Rgb(0x68736b);

        // ---------------------------------------------------------------- buildings (riverfrontDraw.ts:35-38)
        /// <summary>The north wall run reads as the lit face.</summary>
        public static readonly Color WallTop = Rgb(0xb4ab8e);
        public static readonly Color WallSide = Rgb(0x686e5d);
        public static readonly Color Door = Rgb(0x20383a);
        /// <summary>An enterable door is outlined in the interior teal.</summary>
        public static readonly Color DoorEnterable = Rgb(0x67ddc7);
        /// <summary>A sealed door is crossed out in the boarded tan.</summary>
        public static readonly Color DoorSealed = Rgb(0xc3a878);
        /// <summary>A secondary (compound side) door outline.</summary>
        public static readonly Color DoorSecondary = Rgb(0xdbcfaa);
        /// <summary>riverfrontDraw.ts:39 — the threshold slab outside a door.</summary>
        public static readonly Color Threshold = Rgb(0xb2aa8d);

        // ---------------------------------------------------------------- sites (riverfrontDraw.ts:65-67)
        public static readonly Color Substation = Rgb(0x677a76);
        public static readonly Color SubstationRib = Rgb(0x9da890);
        public static readonly Color StopPlatform = Rgb(0xb9b39a);
        public static readonly Color StopMarker = Rgb(0xe3bb69);
        public static readonly Color LampPost = Rgb(0x30352f);
        public static readonly Color LampDead = Rgb(0x71808a);
        public static readonly Color LampLit = Rgb(0xffe6a8);
        public static readonly Color LampGlow = new Color(1f, 0.87f, 0.55f, 0.28f);

        // ---------------------------------------------------------------- resources (riverfrontDraw.ts:62)
        public static Color Resource(string item)
        {
            if (string.IsNullOrEmpty(item)) return Rgb(0x92978a, 0.55f);
            if (item.Contains("iron") || item.Contains("steel")) return Rgb(0xa4b2b0, 0.55f);
            if (item.Contains("copper")) return Rgb(0xc08854, 0.55f);
            if (item.Contains("coal")) return Rgb(0x273338, 0.55f);
            if (item.Contains("crude")) return Rgb(0x3a3b4b, 0.55f);
            return Rgb(0x92978a, 0.55f);
        }

        // ---------------------------------------------------------------- roofs and floors
        //
        // build-assets.py (suburban/industrial) and build-downtown.py (downtown) generate the SVGs from exactly these
        // two tables. A kind not in the downtown list uses the suburban one, which is also the sensible default for
        // anything the city gains later.

        private static bool Downtown(string kind)
        {
            switch (kind)
            {
                case "townhall":
                case "apartment":
                case "office":
                case "department":
                case "arcade":
                case "parking": return true;
                default: return false;
            }
        }

        /// <summary>The roof colour for a building kind and its authored variant (0, 1 or 2).</summary>
        public static Color Roof(string kind, int variant)
        {
            var v = variant < 0 ? 0 : variant % 3;
            if (Downtown(kind))
            {
                switch (v)
                {
                    case 0: return Rgb(0x746854);
                    case 1: return Rgb(0x526767);
                    default: return Rgb(0x697061);
                }
            }
            switch (v)
            {
                case 0: return Rgb(0x795e47);
                case 1: return Rgb(0x586968);
                default: return Rgb(0x626c4b);
            }
        }

        /// <summary>The interior floor colour of an enterable building.</summary>
        public static Color Floor(string kind) => Downtown(kind) ? Rgb(0x797c6b) : Rgb(0x7a7764);

        /// <summary>The wall body colour the roof sits on (the SVGs' body fill).</summary>
        public static Color Body(string kind) => Downtown(kind) ? Rgb(0x9b947d) : Rgb(0x958f77);
    }
}
