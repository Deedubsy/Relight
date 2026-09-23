using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace Relight.UI
{
    /// <summary>
    /// Item and machine icons for every slot, card and tooltip.
    ///
    /// <b>This is placeholder art.</b> <c>Presentation/Placeholders</c> ships engineer and machine PNGs only, and
    /// nothing for the twenty carried items, so until an artist fills an <see cref="ItemIconLibrary"/> every icon
    /// here is generated: a rounded square in the item's own colour with a two-letter code on it. The colours are
    /// lifted from the reference's vector illustrations (<c>packages/game/src/itemIcons.ts</c>) — the dominant
    /// fill of each shape, and the same aliases that file declares at its foot (iron→steel, concrete→stone,
    /// foundry/refinery/assembler2/alienworkbench→assembler, pumpjack→excavator, cannon→turret, fastbelt→belt,
    /// ironore→stone, copperore→copper, crude/fuel→coal, polymer→steel, shell→magazine, bigpole→pole,
    /// arclamp/floodlight→lamp). A generated square is never mistaken for finished art, which is the point.
    ///
    /// The placeholder is a styled <see cref="VisualElement"/>, not a generated texture: UI Toolkit cannot
    /// rasterise a two-letter code into a <see cref="Sprite"/> without a font atlas, and an element can carry the
    /// colour, the rounded corners and the code with no asset at all. <see cref="Paint"/> writes the background
    /// colour inline because it is a per-item runtime value — the same exception TECHNICAL_ARCHITECTURE.md §8.2
    /// grants the drag ghost and the progress fill. Everything else about the icon is USS.
    /// </summary>
    public static class ItemIcons
    {
        /// <summary>USS class on the icon square.</summary>
        public const string IconClass = "item-icon";

        /// <summary>USS class on the two-letter code label.</summary>
        public const string CodeClass = "item-icon-code";

        /// <summary>Colour used when a key is in neither the table nor the alias list.</summary>
        public static readonly Color Unknown = new Color32(0x8b, 0x93, 0x6b, 0xff);   // itemIcons.ts `chest`

        /// <summary>The optional art table. Set once from a controller's serialized field; may stay null.</summary>
        public static ItemIconLibrary Library;

        // Dominant fill per reference shape. Only the keys the shapes file actually draws are listed; the alias
        // table below routes everything else here.
        private static readonly Dictionary<string, Color32> Colours = new Dictionary<string, Color32>(StringComparer.Ordinal)
        {
            { "steel", new Color32(0xb6, 0xc9, 0xcc, 0xff) },
            { "copper", new Color32(0xdb, 0x9b, 0x64, 0xff) },
            { "stone", new Color32(0xb2, 0xb7, 0xaa, 0xff) },
            { "coal", new Color32(0x4c, 0x59, 0x61, 0xff) },
            { "belt", new Color32(0x83, 0x95, 0x95, 0xff) },
            { "inserter", new Color32(0xef, 0xbf, 0x61, 0xff) },
            { "excavator", new Color32(0xc6, 0x93, 0x37, 0xff) },
            { "assembler", new Color32(0x39, 0x4e, 0x53, 0xff) },
            { "pole", new Color32(0x95, 0xaa, 0xa7, 0xff) },
            { "generator", new Color32(0x7b, 0x8d, 0x7d, 0xff) },
            { "magazine", new Color32(0xb4, 0x93, 0x50, 0xff) },
            { "rifle", new Color32(0xa6, 0x80, 0x4d, 0xff) },
            { "turret", new Color32(0x65, 0x7e, 0x78, 0xff) },
            { "backpack", new Color32(0x95, 0x8a, 0x59, 0xff) },
            { "lamp", new Color32(0xd6, 0xbc, 0x68, 0xff) },
            { "wire", new Color32(0xd4, 0x95, 0x57, 0xff) },
            { "frame", new Color32(0x93, 0xaa, 0xa9, 0xff) },
            { "board", new Color32(0x50, 0x7d, 0x68, 0xff) },
            { "chest", new Color32(0x8b, 0x93, 0x6b, 0xff) },
            { "wall", new Color32(0x8b, 0x97, 0x90, 0xff) },
            { "mixer", new Color32(0xbb, 0xa3, 0x6b, 0xff) },
            { "splitter", new Color32(0x87, 0x9a, 0x91, 0xff) },
            { "underground", new Color32(0x65, 0x7d, 0x76, 0xff) },
            { "substation", new Color32(0x5c, 0x79, 0x77, 0xff) },
            { "barricade", new Color32(0xc4, 0x9e, 0x53, 0xff) },
            // REL-132. The Wall's grey warmed towards copper, because a Gate is a wall with a mechanism in it.
            { "gate", new Color32(0xa4, 0x93, 0x7e, 0xff) },
            { "core1", new Color32(0x76, 0xed, 0xd0, 0xff) },
            { "alienartifact", new Color32(0xd6, 0xa4, 0xff, 0xff) }
        };

        // itemIcons.ts:33-37, verbatim.
        private static readonly Dictionary<string, string> Aliases = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            { "iron", "steel" }, { "concrete", "stone" }, { "bigpole", "pole" }, { "arclamp", "lamp" },
            { "floodlight", "lamp" }, { "foundry", "assembler" }, { "refinery", "assembler" },
            { "assembler2", "assembler" }, { "alienworkbench", "assembler" }, { "pumpjack", "excavator" },
            { "cannon", "turret" }, { "fastbelt", "belt" }, { "ironore", "stone" }, { "copperore", "copper" },
            { "crude", "coal" }, { "fuel", "coal" }, { "polymer", "steel" }, { "shell", "magazine" },
            { "core2", "core1" }, { "core3", "core1" }, { "depot", "chest" }, { "overclock", "alienartifact" }
        };

        // Two-letter codes for the carried items, chosen so no two collide (copper/copperore/coal/concrete/core
        // all start "co"). Machine kinds fall back to the first two letters, and their display name is always
        // next to the square anyway.
        private static readonly Dictionary<string, string> Codes = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            { "overclock", "OC" }, { "alienartifact", "AA" }, { "ironore", "IO" }, { "copperore", "CO" },
            { "crude", "CR" }, { "fuel", "FU" }, { "polymer", "PO" }, { "shell", "SH" },
            { "core1", "C1" }, { "core2", "C2" }, { "core3", "C3" }, { "steel", "ST" },
            { "copper", "CU" }, { "stone", "SN" }, { "coal", "CL" }, { "magazine", "MG" },
            { "wire", "WR" }, { "frame", "FR" }, { "board", "BD" }, { "concrete", "CN" }
        };

        /// <summary>The placeholder colour for a key, following the reference's alias list.</summary>
        public static Color ColourOf(string key)
        {
            if (string.IsNullOrEmpty(key)) return Unknown;
            var k = Base(key);
            return Colours.TryGetValue(k, out var c) ? (Color)c : Unknown;
        }

        /// <summary>The two-letter code drawn on the placeholder square.</summary>
        public static string Code(string key)
        {
            if (string.IsNullOrEmpty(key)) return "?";
            if (Codes.TryGetValue(key, out var code)) return code;
            var k = key;
            var colon = k.IndexOf(':');
            if (colon > 0) k = k.Substring(0, colon);
            if (k.Length == 0) return "?";
            if (k.Length == 1) return k.ToUpperInvariant();
            return k.Substring(0, 2).ToUpperInvariant();
        }

        /// <summary>
        /// Draw <paramref name="key"/> into an already-built icon square and its code label. Passing an empty key
        /// blanks both, which is how an empty slot is drawn.
        /// </summary>
        public static void Paint(VisualElement icon, Label code, string key)
        {
            if (icon == null) return;
            if (string.IsNullOrEmpty(key))
            {
                icon.style.backgroundColor = new StyleColor(StyleKeyword.Null);
                icon.style.backgroundImage = new StyleBackground(StyleKeyword.Null);
                if (code != null) code.text = "";
                return;
            }

            Sprite sprite = null;
            if (Library == null) Library = Resources.Load<ItemIconLibrary>("RelightItemIcons");
            if (Library != null && !Library.TryGet(key, out sprite)) Library.TryGet(Base(key), out sprite);

            if (sprite != null)
            {
                icon.style.backgroundImage = new StyleBackground(sprite);
                icon.style.backgroundColor = new StyleColor(StyleKeyword.Null);
                if (code != null) code.text = "";
                return;
            }

            icon.style.backgroundImage = new StyleBackground(StyleKeyword.Null);
            icon.style.backgroundColor = ColourOf(key);
            if (code != null) code.text = Code(key);
        }

        /// <summary>Build the square + code pair. The caller adds the returned element to its slot.</summary>
        public static VisualElement Make(string name, out Label code)
        {
            var icon = new VisualElement { name = name, pickingMode = PickingMode.Ignore };
            icon.AddToClassList(IconClass);
            code = new Label { name = name + "-code", pickingMode = PickingMode.Ignore };
            code.AddToClassList(CodeClass);
            icon.Add(code);
            return icon;
        }

        private static string Base(string key)
        {
            var k = key;
            var colon = k.IndexOf(':');
            if (colon > 0) k = k.Substring(0, colon);
            return Aliases.TryGetValue(k, out var target) ? target : k;
        }
    }
}
