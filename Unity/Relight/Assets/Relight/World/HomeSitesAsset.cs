using System;
using System.Collections.Generic;
using UnityEngine;

namespace Relight.World
{
    /// <summary>What kind of authored thing a <see cref="WorldSite"/> marks (reference city/gameplaySites.ts, riverfront.ts).</summary>
    public enum WorldSiteKind
    {
        /// <summary>U-D-19: the Home core IS the Home workshop building.</summary>
        Core = 0,
        /// <summary>A 3×3 substation lot (reference ground.ts:630).</summary>
        Substation = 1,
        /// <summary>An authored resource rect (reference riverfront.ts resources, HQ_PATCHES).</summary>
        Resource = 2,
        /// <summary>A first-region enemy camp marker or one of its spawn groups (gameplaySites.ts FIRST_CAMPS).</summary>
        Camp = 3,
        /// <summary>The opening raid approach row (ground.ts homeRaidY).</summary>
        RaidLine = 4,
        /// <summary>A tram stop platform (riverfront.ts stops).</summary>
        TramStop = 5,
        /// <summary>A factory yard reservation (riverfront.ts yards).</summary>
        Yard = 6,
        /// <summary>A region name label (riverfront.ts regions).</summary>
        Label = 7,
        /// <summary>An authored kerb streetlight (C-11; reference ground.ts G.blocks[bi].lights).</summary>
        Light = 8,
        /// <summary>
        /// Batch 4 (U-D-73). A stronghold's power core (riverfront.ts cores, "Occupied freight depot"): the prize the
        /// guardian holds. Its own kind, never <see cref="Core"/>, because the first Core is the Home core.
        /// </summary>
        PowerCore = 9,
        /// <summary>Batch 4. A campaign power plant a Power core commissions (riverfront.ts plants).</summary>
        Plant = 10,
        /// <summary>
        /// Batch 4. A stronghold entrance (gameplaySites.ts FREIGHT_GATES), locked until the keys are carried. A site
        /// door, not the player's buildable Gate (U-D-72).
        /// </summary>
        StrongholdDoor = 11,
        /// <summary>Batch 4. A stronghold's floor rect, its guardian tile and its garrison groups (FREIGHT_ARENA).</summary>
        Arena = 12,
    }

    /// <summary>
    /// One authored site inside an imported region: a stable id from the reference sources plus a REGION-LOCAL,
    /// Y-DOWN tile rect. A point site is a 1×1 rect. Overridable by id (see <see cref="HomeSitesOverrides"/>).
    /// </summary>
    [Serializable]
    public sealed class WorldSite
    {
        [Tooltip("Stable id from the reference sources; the key overrides match on.")]
        public string id;
        public string name;
        public WorldSiteKind kind;
        [Tooltip("Region-local tile rect, Y down. A point site is 1x1.")]
        public RectInt rect;
        [Tooltip("Resource item id for Resource sites (steel / copper / coal / scrap); empty otherwise.")]
        public string item;
        [Tooltip("Units for Resource sites, defender count for Camp sites; 0 otherwise.")]
        public int amount;

        public WorldSite Clone() => new WorldSite
        {
            id = id, name = name, kind = kind, rect = rect, item = item, amount = amount,
        };
    }

    /// <summary>
    /// C-01. The authored sites of one imported region. Generated under
    /// <c>Assets/Relight/World/Generated/&lt;Region&gt;/</c> and regenerated wholesale on every import: put manual
    /// changes in a <see cref="HomeSitesOverrides"/> asset outside <c>Generated/</c> instead.
    /// </summary>
    [CreateAssetMenu(menuName = "Relight/Home Sites", fileName = "HomeSites")]
    public sealed class HomeSitesAsset : ScriptableObject
    {
        [SerializeField] private string regionId;
        [Tooltip("The region's north-west corner in whole-city tiles; every rect below is stated relative to it.")]
        [SerializeField] private int originX;
        [SerializeField] private int originY;
        [Tooltip("Generated; ordered by the exporter's order so two imports produce identical YAML.")]
        [SerializeField] private List<WorldSite> sites = new List<WorldSite>();

        public string RegionId => regionId;
        public int OriginX => originX;
        public int OriginY => originY;
        public IReadOnlyList<WorldSite> Sites => sites;

        /// <summary>Written by the importer only.</summary>
        public void Fill(string newRegionId, int newOriginX, int newOriginY, List<WorldSite> newSites)
        {
            regionId = newRegionId;
            originX = newOriginX;
            originY = newOriginY;
            sites = newSites;
        }
    }

    /// <summary>Resolving generated sites against the manual overrides. Pure, deterministic, no asset writes.</summary>
    public static class HomeSites
    {
        /// <summary>
        /// The sites the game should use: the generated list in its generated order, each replaced by the override
        /// with the same id when one exists, minus the removed ids, plus any override id that is not generated (in
        /// the overrides' own order). Either argument may be null.
        ///
        /// <b>Coordinates (correction-pass C6).</b> Every rect in the result is stated in the RUNNING region's tiles,
        /// i.e. the generated asset's own tiles. An overrides asset that names a different region (the Phase C
        /// overrides are Home-crop tiles at (43,91); the game now runs the whole city at (0,0)) has each of its rects
        /// shifted by (the overrides' origin − the generated asset's origin), so a crop-authored override lands on the
        /// same world tile it always did. An overrides asset with an empty region id is taken to be stated in the
        /// running region already and is never shifted.
        /// </summary>
        public static List<WorldSite> Resolve(HomeSitesAsset generated, HomeSitesOverrides overrides)
        {
            var shift = OverrideShift(generated, overrides);
            var result = new List<WorldSite>();
            var replaced = new HashSet<string>();
            var dropped = new HashSet<string>();
            if (overrides != null)
                foreach (var id in overrides.Removed)
                    if (!string.IsNullOrEmpty(id)) dropped.Add(id);

            if (generated != null)
            {
                foreach (var site in generated.Sites)
                {
                    if (site == null || string.IsNullOrEmpty(site.id)) continue;
                    if (dropped.Contains(site.id)) continue;
                    var over = Find(overrides, site.id);
                    if (over != null) { replaced.Add(site.id); result.Add(Shifted(over, shift)); }
                    else result.Add(site.Clone());
                }
            }

            if (overrides != null)
            {
                foreach (var site in overrides.Sites)
                {
                    if (site == null || string.IsNullOrEmpty(site.id)) continue;
                    if (replaced.Contains(site.id) || dropped.Contains(site.id)) continue;
                    result.Add(Shifted(site, shift));
                }
            }
            return result;
        }

        /// <summary>
        /// How far every override rect must move to be stated in the generated asset's tiles. Zero when there are no
        /// overrides, when they name no region, or when both assets share an origin.
        /// </summary>
        public static Vector2Int OverrideShift(HomeSitesAsset generated, HomeSitesOverrides overrides)
        {
            if (generated == null || overrides == null) return Vector2Int.zero;
            if (string.IsNullOrEmpty(overrides.RegionId)) return Vector2Int.zero;
            return new Vector2Int(overrides.OriginX - generated.OriginX, overrides.OriginY - generated.OriginY);
        }

        private static WorldSite Shifted(WorldSite site, Vector2Int shift)
        {
            var copy = site.Clone();
            if (shift.x == 0 && shift.y == 0) return copy;
            copy.rect = new RectInt(copy.rect.x + shift.x, copy.rect.y + shift.y, copy.rect.width, copy.rect.height);
            return copy;
        }

        private static WorldSite Find(HomeSitesOverrides overrides, string id)
        {
            if (overrides == null) return null;
            foreach (var s in overrides.Sites)
                if (s != null && s.id == id) return s;
            return null;
        }
    }
}
