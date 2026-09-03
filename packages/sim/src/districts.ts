/** §7 district table and the well rule. The doc's district table is generated from DISTRICTS (packages/tools/docsync). */
import { District } from './types';

export interface DistrictRow {
  name: District;
  label: string;        // doc name
  rubble: string;       // §7 rubble type column
  base: number;         // rot cap before depth and wells
  g: number;            // growth per second toward the cap
}

/** §7 rows in doc order. Rail yard is Residential's numbers under another name (RAIL_YARD); the block sim has no rail yard. */
export const RAIL_YARD = { label: 'Rail yard', rubble: 'coal', sameAs: 'res' as District };
export const DISTRICTS: readonly DistrictRow[] = [
  { name: 'civ', label: 'Civic (halls, hospital, library)', rubble: 'stone/concrete', base: 0.30, g: 0.0004 },
  { name: 'res', label: 'Residential',                      rubble: 'copper',         base: 0.45, g: 0.0005 },
  { name: 'ind', label: 'Industrial',                       rubble: 'steel',          base: 0.60, g: 0.0006 },
  { name: 'out', label: 'Outskirts',                        rubble: 'none (deposits)', base: 1.00, g: 0.0008 },
];

/** §7 depth multiplier: cap × (1 + DEPTH_PER_BLOCK × blocks from the start), clamped to 1. */
export const DEPTH_PER_BLOCK = 0.3 / 20;
/** §7 well: within WELL_RANGE blocks the cap gains WELL_CAP_BONUS × influence and growth is ×(1 + WELL_G_MULT × influence),
 *  influence = 1 − distance/4. */
export const WELL_RANGE = 3, WELL_CAP_BONUS = 0.3, WELL_G_MULT = 3;
/** §5: a well enclosed by Held blocks on every side dies after this long. */
export const WELL_DEATH_SECONDS = 300;

export function districtOf(name: District): DistrictRow {
  for (const r of DISTRICTS) if (r.name === name) return r;
  throw new Error(`unknown district ${name}`);
}

/** The canonical map's zoning (frontsim.py district()); the district a block belongs to by its coordinates. */
export function zoneOf(x: number, y: number): District {
  if (y < 4 || x < 3 || x > 20) return 'out';
  if (y < 11) return 'ind';
  if (y < 17) return x >= 9 && x <= 15 ? 'ind' : 'res';
  return x % 3 === 0 ? 'res' : 'civ';
}

/** Cap and growth before any well: district base with the depth multiplier. Bit-identical to frontsim.py. */
export function districtBase(x: number, y: number, start: readonly [number, number]): { dmax: number; g: number; name: District } {
  const name = zoneOf(x, y);
  const r = districtOf(name);
  const d = Math.abs(x - start[0]) + Math.abs(y - start[1]);
  return { dmax: Math.min(1.0, r.base * (1 + 0.3 * d / 20)), g: r.g, name };
}

export function wellInfluence(x: number, y: number, wells: readonly (readonly [number, number])[]): number {
  let infl = 0;
  for (const [wx, wy] of wells) {
    const wd = Math.abs(x - wx) + Math.abs(y - wy);
    if (wd <= WELL_RANGE) infl = Math.max(infl, 1 - wd / 4);
  }
  return infl;
}
