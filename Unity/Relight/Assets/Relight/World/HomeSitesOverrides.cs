using System.Collections.Generic;
using UnityEngine;

namespace Relight.World
{
    /// <summary>
    /// Hand-authored corrections that survive re-import, because they live OUTSIDE <c>Generated/</c>
    /// (<c>Assets/Relight/World/Manual/</c>). Lives in its own file because Unity only binds a
    /// ScriptableObject asset to a class whose file name matches (Phase C fix: the first import wrote an asset with no script). Each entry wins over the generated site with the same id; ids listed in
    /// <see cref="removed"/> are dropped; entries whose id is not generated are added after the generated ones.
    ///
    /// <b>Coordinates.</b> Every rect in <see cref="sites"/> is stated in the tiles of the region named by
    /// <see cref="regionId"/>, whose north-west corner in whole-city tiles is
    /// (<see cref="originX"/>, <see cref="originY"/>). The overrides authored during Phase C are Home-crop tiles,
    /// i.e. region <c>home</c> at (43,91). When the game runs on a DIFFERENT region — the full 864x576 city at (0,0)
    /// — <see cref="HomeSites.Resolve"/> shifts each override by (this origin − the running region's origin), so a
    /// crop-local override lands on the same world tile it always did (correction-pass contract C6). An override
    /// asset with an empty <see cref="regionId"/> is assumed to be stated in the running region's own tiles and is
    /// not shifted, which is what an override authored after the move will be.
    /// </summary>
    [CreateAssetMenu(menuName = "Relight/Home Sites Overrides", fileName = "HomeSitesOverrides")]
    public sealed class HomeSitesOverrides : ScriptableObject
    {
        [Tooltip("The region these overrides' coordinates are stated in, e.g. 'home'. Empty = the running region.")]
        [SerializeField] private string regionId = "";
        [Tooltip("That region's north-west corner in whole-city tiles (home is 43,91).")]
        [SerializeField] private int originX;
        [SerializeField] private int originY;
        [Tooltip("Each wins over the generated site with the same id; an unknown id is appended.")]
        [SerializeField] private List<WorldSite> sites = new List<WorldSite>();
        [Tooltip("Generated site ids to drop entirely.")]
        [SerializeField] private List<string> removed = new List<string>();

        public string RegionId => regionId;
        public int OriginX => originX;
        public int OriginY => originY;
        public IReadOnlyList<WorldSite> Sites => sites;
        public IReadOnlyList<string> Removed => removed;

        /// <summary>
        /// Stamp the region these overrides are stated in. Written ONCE, by the importer, when it first creates the
        /// empty asset: an existing overrides asset is never rewritten, so a hand-authored origin is never lost.
        /// </summary>
        public void StampRegion(string newRegionId, int newOriginX, int newOriginY)
        {
            regionId = newRegionId ?? "";
            originX = newOriginX;
            originY = newOriginY;
        }
    }
}
