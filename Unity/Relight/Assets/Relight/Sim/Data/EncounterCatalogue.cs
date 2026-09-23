using System;
using System.Collections.Generic;

namespace Relight.Sim
{
    /// <summary>What an encounter is for (FREIGHT_STRONGHOLD_DESIGN §4.1 <c>Kind</c>).</summary>
    public enum EncounterKind
    {
        /// <summary>A stronghold key camp: held for a while once its guards are gone, and it gives up a key.</summary>
        KeyCamp = 0,
        /// <summary>A small corridor camp: loot only, claimed on its last kill.</summary>
        LootCamp = 1,
        /// <summary>A stronghold's warehouse floor and its guardian.</summary>
        Arena = 2,
        /// <summary>The bodies squatting in a power plant, cleared before the plant can be prepared.</summary>
        Squat = 3,
    }

    /// <summary>How the engineer comes to know an encounter is there (§4.1 <c>Discovery</c>, §5.1).</summary>
    public enum EncounterDiscovery
    {
        /// <summary>A rough search circle first, then the camp itself at <see cref="EncounterCatalogue.SearchResolveTiles"/>.</summary>
        SearchArea = 0,
        /// <summary>Nothing until the engineer is within <see cref="EncounterCatalogue.ProximityTiles"/>.</summary>
        Proximity = 1,
    }

    /// <summary>
    /// One hostile place (FREIGHT_STRONGHOLD_DESIGN §4.1). Everything that makes one encounter differ from another
    /// is a column here, so the phase that runs them never asks which encounter it is holding.
    ///
    /// <para><see cref="Site"/> is the <see cref="SiteRecord.Id"/> the encounter stands on: its marker, and the
    /// prefix of its spawn groups (<c>{Site}:group:N</c>, as the importer brings them in). An encounter whose site
    /// is not in the loaded region does not exist in that game, which is what keeps it off the synthetic test map
    /// and off any crop of the city that leaves it out. A site with no group records spawns round the marker, at
    /// <see cref="Offsets"/>.</para>
    ///
    /// <para><see cref="Bodies"/> is the whole garrison. It is dealt round the groups in turn; body <c>i</c>
    /// (from 0) is a spitter when <c>SpitterEvery &gt; 0</c> and <c>(i + 1) % SpitterEvery == 0</c>, and the
    /// <see cref="Extra"/> kinds take the LAST places, so the count stays the one U-D-64 (b) sets.</para>
    ///
    /// <para><see cref="Serial"/> keeps this encounter's squads apart from every raid and every other camp: group
    /// <c>g</c> is <see cref="Enemy.Group"/> <c>GroupBase + Serial * 100 + g</c>. It is fixed per row, never an
    /// index, so adding a row does not renumber the squads of a camp already standing in a save.</para>
    /// </summary>
    public sealed record EncounterDef(
        string Id, string Name, string Site, EncounterKind Kind, EncounterDiscovery Discovery,
        int Bodies, int SpitterEvery, IReadOnlyList<(string Kind, int Count)> Extra,
        IReadOnlyList<(int Dx, int Dy)> Offsets,
        double OccupyRadius, double OccupySeconds, double GuardRadius,
        IReadOnlyList<ItemStack> Cache, string Key, double RepeatSeconds, int Serial, string Source);

    /// <summary>
    /// One stronghold (FREIGHT_STRONGHOLD_DESIGN §4.3): the keys that open it, the locked doors they open and the
    /// arena encounter behind them. A stronghold whose <see cref="Arena"/> site is not in the loaded region does
    /// not exist in that game, exactly as an encounter whose site is missing does not. <see cref="Guardian"/> is
    /// the site its guardian is born at (FRT-06), one body beyond the arena row's <see cref="EncounterDef.Bodies"/>.
    /// </summary>
    public sealed record StrongholdDef(
        string Id, string Name, IReadOnlyList<string> Keys, IReadOnlyList<string> Doors, string Arena,
        string Guardian, string Source);

    /// <summary>
    /// Batch 4 (U-D-73): the port-owned encounter rows, the <see cref="CombatBalance"/> pattern. The reference keeps
    /// its camps as a hand-written list in gameplaySites.ts (FIRST_CAMPS) and its garrisons as numbers on the city
    /// json; neither reaches the exported catalogue, so the rows live here.
    ///
    /// Every number is a starting value under U-D-28 (U-P-37). What the owner decided is where they come from: the
    /// West passage has 12 guards, not the reference's 20, because it is the rifle-only teaching fight
    /// (U-D-64 (b)); Old utility and Northwood keep the reference's 22 and 20; Old utility's <c>DarkStart</c> is
    /// gone because every camp is already dark (U-D-58), so it differs by its spitter share alone; and no row
    /// carries the design's <c>RaidWeight</c>, because living camps do not change raid size (U-D-73 (1)).
    ///
    /// Rows arrive with the commits that use them: the key camps and the Riverside squat with FRT-02 (REL-137),
    /// the small camps with FRT-04 (REL-139) and the warehouse with FRT-05 (REL-140). Key camps, the warehouse and
    /// the squat never refill (<see cref="EncounterDef.RepeatSeconds"/> 0): a key is taken once, and a plant is
    /// cleared once.
    /// </summary>
    public static class EncounterCatalogue
    {
        /// <summary>§5.1: a search-area encounter is found when the engineer is this close to its marker.</summary>
        public const double SearchResolveTiles = 32;
        /// <summary>§5.1: a proximity encounter is found this close.</summary>
        public const double ProximityTiles = 12;
        /// <summary>
        /// §5.1: no body is born this close to the engineer. The whole garrison waits, and tries again next tick,
        /// rather than any part of it appearing at the engineer's elbow.
        /// </summary>
        public const double NoSurpriseBirthTiles = 12;
        /// <summary>How far from its group's centre a body may be placed, the same bound raid staging uses.</summary>
        public const double GroupSpreadTiles = 6;
        /// <summary>
        /// §3 (the reference's <c>FABRICATION.repeatSeconds</c>): a cleared loot camp comes back this long after its
        /// last death, unless the player has built within <see cref="BuiltNearTiles"/> of it.
        /// </summary>
        public const double LootRepeatSeconds = 900;
        /// <summary>§3: a player machine this close to a cleared camp's marker keeps it from being refilled.</summary>
        public const double BuiltNearTiles = 20;
        /// <summary>
        /// FRT-05 (U-P-40): the warehouse garrison is born when the engineer comes this close to the middle of the
        /// warehouse floor, not on the first step inside it. Born at the door, its two inside squads would be about
        /// four tiles from the engineer and §5.1's no-surprise rule could never let them in; born at the start of
        /// the game, 60 bodies would sit in <see cref="RaidTuning.LivingBudget"/> all game. From 48 tiles every
        /// squad, inside or out, is well over 12 tiles off.
        /// </summary>
        public const double ArenaResolveTiles = 48;
        /// <summary>§5.4 (the reference's openFreight): the engineer opens a stronghold within this of either door.</summary>
        public const double DoorReachTiles = 3;
        /// <summary>
        /// U-D-69 (e), FRT-05 (U-P-40): the engineer is in a stronghold fight within this of its arena floor, or
        /// while any of its garrison is chasing them.
        /// </summary>
        public const double FightNearTiles = 12;
        /// <summary>U-D-69 (e): the fight is over this long after the engineer leaves or goes down.</summary>
        public const double FightTailSeconds = 30;
        /// <summary>The first group number an encounter squad takes; raid groups are raid ids and stay far below.</summary>
        public const int GroupBase = 1000000;

        static readonly (string, int)[] None = Array.Empty<(string, int)>();
        static readonly (int, int)[] AtMarker = Array.Empty<(int, int)>();

        const string Design = "Unity/Docs/FREIGHT_STRONGHOLD_DESIGN_2026-09-18.md §4.2; U-D-73; U-P-37";

        /// <summary>The rows, in the order the phase walks them and a save lists them.</summary>
        public static readonly IReadOnlyList<EncounterDef> All = new[]
        {
            new EncounterDef("freight:camp:1", "West passage", "freight:camp:1",
                EncounterKind.KeyCamp, EncounterDiscovery.SearchArea,
                12, 5, None, AtMarker,
                3, 30, 20,
                new[] { new ItemStack(ItemId.Magazine, 40), new ItemStack(ItemId.Steel, 10) },
                "freight:camp:1", 0, 1,
                Design + "; U-D-64 (b) (12 guards, the rifle-only teaching fight)"),
            new EncounterDef("freight:camp:2", "Old utility", "freight:camp:2",
                EncounterKind.KeyCamp, EncounterDiscovery.SearchArea,
                22, 3, None, AtMarker,
                3, 30, 20,
                new[] { new ItemStack(ItemId.Magazine, 60), new ItemStack(ItemId.Wire, 6), new ItemStack(ItemId.Board, 2) },
                "freight:camp:2", 0, 2,
                Design + "; DarkStart withdrawn by U-D-58"),
            new EncounterDef("freight:camp:3", "Northwood approach", "freight:camp:3",
                EncounterKind.KeyCamp, EncounterDiscovery.SearchArea,
                20, 5, new[] { ("breaker", 1) }, AtMarker,
                3, 30, 20,
                new[] { new ItemStack(ItemId.Magazine, 60), new ItemStack(ItemId.Copper, 10) },
                "freight:camp:3", 0, 3,
                Design),
            // The four small corridor camps (FRT-04, §4.2 freight:small:1..4). The design leaves their tiles to the
            // port, so their sites are port-authored (RegionImporter, U-P-39): street crossings on the Freight
            // corridor, each at least 25 tiles from a key camp. Skitters only; found only up close; loot, no key;
            // claimed on the last kill; refilled LootRepeatSeconds after, unless the player has built near.
            new EncounterDef("freight:small:1", "Crossing camp", "freight:small:1",
                EncounterKind.LootCamp, EncounterDiscovery.Proximity,
                6, 0, None, AtMarker,
                0, 0, 0,
                new[] { new ItemStack(ItemId.Magazine, 20), new ItemStack(ItemId.Steel, 10) },
                null, LootRepeatSeconds, 11,
                Design + "; U-P-39"),
            new EncounterDef("freight:small:2", "East street camp", "freight:small:2",
                EncounterKind.LootCamp, EncounterDiscovery.Proximity,
                7, 0, None, new[] { (0, -3), (0, 3) },
                0, 0, 0,
                new[] { new ItemStack(ItemId.IronOre, 20), new ItemStack(ItemId.Steel, 10) },
                null, LootRepeatSeconds, 12,
                Design + "; U-P-39"),
            new EncounterDef("freight:small:3", "North crossing camp", "freight:small:3",
                EncounterKind.LootCamp, EncounterDiscovery.Proximity,
                5, 0, None, AtMarker,
                0, 0, 0,
                new[] { new ItemStack(ItemId.CopperOre, 20), new ItemStack(ItemId.Copper, 10) },
                null, LootRepeatSeconds, 13,
                Design + "; U-P-39"),
            new EncounterDef("freight:small:4", "Warehouse road camp", "freight:small:4",
                EncounterKind.LootCamp, EncounterDiscovery.Proximity,
                8, 0, None, new[] { (0, -3), (0, 3) },
                0, 0, 0,
                new[] { new ItemStack(ItemId.Magazine, 30) },
                null, LootRepeatSeconds, 14,
                Design + "; U-P-39"),
            // The warehouse floor (FRT-05, §4.2 freight:arena): 60 guards in six squads, four round the outside and
            // two shut in by the doors, every fifth a spitter as the reference's per-squad `% 5 == 4`. No key and no
            // crate; the guardian and the core it drops are FRT-06's.
            new EncounterDef("freight:arena", "Freight warehouse", "freight:arena",
                EncounterKind.Arena, EncounterDiscovery.Proximity,
                60, 5, None, AtMarker,
                0, 0, 0,
                Array.Empty<ItemStack>(), null, 0, 20,
                Design + "; U-D-73 (60 guards, inside U-D-64 (c)); U-P-40"),
            new EncounterDef("plant:riverside:squat", "Riverside squat", "plant:riverside",
                EncounterKind.Squat, EncounterDiscovery.Proximity,
                8, 0, None, new[] { (-3, 0), (3, 0) },
                0, 0, 0,
                Array.Empty<ItemStack>(), null, 0, 50,
                Design + "; §4.4"),
        };

        /// <summary>The strongholds (§4.3). One in batch 4: the Freight warehouse.</summary>
        public static readonly IReadOnlyList<StrongholdDef> Strongholds = new[]
        {
            new StrongholdDef("freight", "Freight warehouse",
                new[] { "freight:camp:1", "freight:camp:2", "freight:camp:3" },
                new[] { "freight:door:0", "freight:door:1" },
                "freight:arena", "freight:arena:guardian",
                "Unity/Docs/FREIGHT_STRONGHOLD_DESIGN_2026-09-18.md §4.3, §5.4, §5.5; packages/sim/src/firstRegion.ts "
                + "openFreight; U-P-40, U-P-41"),
        };

        public static EncounterDef Find(string id)
        {
            for (var i = 0; i < All.Count; i++) if (string.CompareOrdinal(All[i].Id, id) == 0) return All[i];
            return null;
        }

        /// <summary>The stronghold whose arena row is <paramref name="def"/>, or null for every other row.</summary>
        public static StrongholdDef StrongholdOf(EncounterDef def)
        {
            if (def == null) return null;
            for (var i = 0; i < Strongholds.Count; i++)
                if (string.CompareOrdinal(Strongholds[i].Arena, def.Id) == 0) return Strongholds[i];
            return null;
        }

        /// <summary>
        /// How many bodies <paramref name="def"/>'s garrison is born with: its <see cref="EncounterDef.Bodies"/>,
        /// and one more, the guardian, for a stronghold's arena (FRT-06).
        /// </summary>
        public static int Garrison(EncounterDef def) => def.Bodies + (StrongholdOf(def) != null ? 1 : 0);

        /// <summary>
        /// The roster key body <paramref name="i"/> of <paramref name="def"/>'s garrison is born as. The one body
        /// past <see cref="EncounterDef.Bodies"/> of an arena is its guardian.
        /// </summary>
        public static string KindOf(EncounterDef def, int i)
        {
            if (i >= def.Bodies) return GuardianRules.Kind;
            var fromEnd = def.Bodies - 1 - i;
            if (def.Extra != null)
                for (var k = def.Extra.Count - 1; k >= 0; k--)
                {
                    if (fromEnd < def.Extra[k].Count) return def.Extra[k].Kind;
                    fromEnd -= def.Extra[k].Count;
                }
            return def.SpitterEvery > 0 && (i + 1) % def.SpitterEvery == 0 ? "spitter" : "skitter";
        }
    }
}
