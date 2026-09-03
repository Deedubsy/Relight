/** §7 enemy table. The doc's enemy table is generated from ENEMIES (packages/tools/docsync). */
export interface Enemy {
  name: string; hp: number; costRounds: number; costShells: number; tilesPerSec: number; note: string;
  /** §7 enemy-table cells; the doc table is generated from these (npm run docsync). */
  footprint: string; appears: string; targets: string; special: string; punishes: string; sees: string;
}

export const ENEMIES: readonly Enemy[] = [
  { name: 'Crawler', hp: 12, costRounds: 3, costShells: 0, tilesPerSec: 3, note: '3 Shot rounds',
    footprint: '1 tile, 3 t/s', appears: 'every bloom',
    targets: 'nearest lamp, then turret, then the substation (40 unshot arrivals stop it)', special: 'none',
    punishes: 'ammo throughput and hopper distribution: an edge left unfed loses its block in about 7 min (4–20 across seeds) **[sim: E1-starve-substation-N40]**',
    sees: 'red supply pip, hoppers empty, crawlers eating lamps' },
  { name: 'Shade', hp: 40, costRounds: 10, costShells: 0, tilesPerSec: 2, note: '10 Shot rounds',
    footprint: '1 tile, 2 t/s', appears: '1 per 8 crawlers when d ≥ 0.3 **[sim: E3-block]**',
    targets: 'substation (disables 30 s each, stacking)', special: '**untargetable on unlit tiles**',
    punishes: 'light coverage: a single unlit gap in a street is a corridor to the substation',
    sees: 'a block goes dark with full hoppers and no visible fight' },
  { name: 'Hulk', hp: 600, costRounds: 0, costShells: 10, tilesPerSec: 0.8, note: 'Shot does 1, Shell does 60 → 10 shells',
    footprint: '3×3, 0.8 t/s', appears: '40 % of blooms when d ≥ 0.5 **[sim: E3-block, E9-district]**',
    targets: 'barricade → turret → substation', special: 'walks through barricades at 1 per 4 s',
    punishes: 'frontage length: one long thin front has nowhere with enough Cannons',
    sees: 'a 3×3 shape walking through a turret line that is plinking at it' },
];

/** Bloom composition thresholds (frontsim.py shade_thr / hulk_thr, DEFAULT_CONFIG). */
export const SHADE_THRESHOLD = 0.3, HULK_THRESHOLD = 0.5, SHADE_PER_CRAWLERS = 8, HULK_BLOOM_FRACTION = 0.4;
/** Rounds per crawler and per shade, shells per hulk — what the ring pays per arrival. */
export const ROUNDS_PER_CRAWLER = 3, ROUNDS_PER_SHADE = 10, SHELLS_PER_HULK = 10, SHADE_DISABLE_SECONDS = 30;
