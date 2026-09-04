/** Map generator: the fixture generator (frontsim.py / export_fixtures.py) ported to TS.
 *  District, cap and growth are bit-identical to Python. Jitter and scatter use the TS PRNG,
 *  so a TS seed is a different map from the same Python seed; parity is tested from fixtures. */
import { District, MapSpec, CellSpec, Facility, Survivor, SimConfig } from './types';
import { rngInt, rngUniform, seedRng, pyRound } from './prng';
import { districtBase, wellInfluence } from './districts';

/** The canonical city is 24×24 block cells (D-P1-1, 2026-09-03); the last row is the river (inert), so 23 rows
 *  are claimable. The Python fixtures are the older 24×22 map and carry their own MapSpec, so this only shapes
 *  generateMap(). Everything below is anchored to the HQ row so the 24×22 zoning shifts south with it. */
export const W = 24, H = 24;               // city blocks; row H-1 = river (inert)
const DH = H - 22;                          // rows added since the 24×22 fixture map
export const START: [number, number] = [12, 20 + DH];
export const TARGET: [number, number] = [12, 10 + DH];   // "the foundry", 10 blocks north
/** GAME-ASSUMPTION: the two northern corner wells stay in the corners; the Foundry well and the two southern wells
 *  move with the HQ row. */
export const WELLS: [number, number][] = [[12, 8 + DH], [2, 2], [21, 3], [2, 18 + DH], [22, 19 + DH]];

export function district(x: number, y: number): { dmax: number; g: number; name: District } {
  const { dmax: base, g, name } = districtBase(x, y, START, H);
  const infl = wellInfluence(x, y, WELLS);
  return { dmax: Math.min(1.0, base + 0.3 * infl), g: g * (1 + 3 * infl), name };
}

export function wellName(x: number, y: number): boolean {
  for (const [wx, wy] of WELLS) if (Math.abs(x - wx) + Math.abs(y - wy) <= 3) return true;
  return false;
}

/** Seeded scattered inert cells; same map for every policy at a given seed. */
export function makeScatter(seed: number, frac: number, validator: SimConfig['validator']): [number, number][] {
  const rng = { rng: seedRng(Math.imul(seed, 7919) + 13) };
  const cells: [number, number][] = [];
  for (let x = 0; x < W; x++) for (let y = 0; y < H - 1; y++) cells.push([x, y]);
  const protect = new Set<number>();
  const key = (x: number, y: number) => x * H + y;
  protect.add(key(START[0], START[1])); protect.add(key(TARGET[0], TARGET[1]));
  for (const [wx, wy] of WELLS) protect.add(key(wx, wy));
  for (const [dx, dy] of [[1, 0], [-1, 0], [0, 1], [0, -1]]) {
    const nx = START[0] + dx, ny = START[1] + dy;
    if (nx >= 0 && nx < W && ny >= 0 && ny < H) protect.add(key(nx, ny));
  }
  const target = pyRound(frac * cells.length);
  const inert = new Set<number>();
  const isInert = (x: number, y: number) => y === H - 1 || inert.has(key(x, y));
  let tries = 0;
  while (inert.size < target && tries < 20000) {
    tries++;
    const [x, y] = cells[rngInt(rng, cells.length)];
    const k = key(x, y);
    if (inert.has(k) || protect.has(k)) continue;
    let ok = true;
    if (validator === 'no2x2') {
      for (const dx of [-1, 1]) for (const dy of [-1, 1]) {
        if (isInert(x + dx, y) && isInert(x, y + dy) && isInert(x + dx, y + dy)) ok = false;
      }
    } else if (validator === 'maxrun2') {
      for (const [ax, ay] of [[1, 0], [0, 1]]) {
        let run = 1;
        for (const sgn of [1, -1]) {
          let kk = 1;
          while (inert.has(key(x + ax * kk * sgn, y + ay * kk * sgn))) { run++; kk++; }
        }
        if (run > 2) ok = false;
      }
    }
    if (ok) inert.add(k);
  }
  const out: [number, number][] = [];
  for (const k of inert) out.push([Math.floor(k / H), k % H]);
  out.sort((p, q) => p[0] - q[0] || p[1] - q[1]);
  return out;
}

/** GAME-ASSUMPTION: facility placement. The doc (§8, §17) gives Foundry 5–9 blocks out, Arsenal in a
 *  different octant, Power station ≥ 15 opposite; the sim only has TARGET. The proto shows the four
 *  nearest silhouettes and toasts when one is Held, so positions only need to be plausible and seeded. */
export function placeFacilities(seed: number, inert: Set<number>): Facility[] {
  const rng = { rng: seedRng(Math.imul(seed, 31337) + 5) };
  const out: Facility[] = [{ name: 'Foundry', x: TARGET[0], y: TARGET[1] }];
  const taken = new Set<number>([TARGET[0] * H + TARGET[1]]);
  const dist = (x: number, y: number) => Math.abs(x - START[0]) + Math.abs(y - START[1]);
  const place = (name: string, ok: (x: number, y: number) => boolean) => {
    for (let tries = 0; tries < 2000; tries++) {
      const x = rngInt(rng, W), y = rngInt(rng, H - 1);
      const k = x * H + y;
      if (inert.has(k) || taken.has(k) || !ok(x, y)) continue;
      if (wellName(x, y)) continue;
      taken.add(k); out.push({ name, x, y }); return;
    }
  };
  place('Arsenal', (x, y) => x <= 8 && y >= 12 + DH && dist(x, y) >= 5 && dist(x, y) <= 9);
  place('Turbine hall', (x, y) => x >= 16 && y >= 12 + DH && dist(x, y) >= 5 && dist(x, y) <= 9);
  place('Refinery', (x, y) => y <= 13 + DH && Math.abs(x - START[0]) >= 3 && dist(x, y) >= 8 && dist(x, y) <= 12);
  place('Power station', (x, y) => y <= 4 && dist(x, y) >= 15);
  place('Tram depot', (x, y) => dist(x, y) >= 3 && dist(x, y) <= 5);   // D5: where the truck is found; 3–5 blocks (GAME-ASSUMPTION, was §8's 5–10: an hour-3 find, see city/generate.ts TRAM_HOPS)
  return out;
}

/** GAME-ASSUMPTION: survivor placement. §8 gives each group a distance band from the HQ (Electricians ≤ 3, Concrete
 *  crew ≤ 4, Gunsmith 4–8, Rail crew 5–10, Foreman 6–10) and says a block's contents show once a 4-neighbour is Held.
 *  The optional groups (Chemist near oil, Lamplighters, Surveyors) need the world view's oil and dead-end pockets and
 *  are left out. Question for the human: does a group's block have to be Held, or only reached, for "we're in"?
 *  The proto marks the block, reveals it per §8 and toasts on Held; no unlock effect exists yet (§8 is world view). */
export const SURVIVOR_BANDS: { name: string; tag: string; min: number; max: number }[] = [
  { name: 'Electricians', tag: 'E', min: 1, max: 3 },
  { name: 'Concrete crew', tag: 'N', min: 2, max: 4 },
  { name: 'Gunsmith', tag: 'G', min: 4, max: 8 },
  { name: 'Rail crew', tag: 'K', min: 5, max: 10 },
  { name: 'Foreman', tag: 'M', min: 6, max: 10 },
];
/** §8's unlock column for the groups the slice builds (prompt B M3): the Electricians' three land on the toolbar
 *  when their block turns Held; the flow layer's `SURVIVOR_UNLOCKS` holds the kinds. M4: the Arsenal's Rifle Mk2 is
 *  named here (the build menu lists it locked) but is a toolbar entry only — the upgrade is outside the hour. */
export const SURVIVOR_UNLOCK_NAMES: Record<string, readonly string[]> = { Electricians: ['Floodlight', 'Big pole', 'Substation'], Arsenal: ['Rifle Mk2'] };
export function placeSurvivors(seed: number, inert: Set<number>, facilities: Facility[]): Survivor[] {
  const rng = { rng: seedRng(Math.imul(seed, 48611) + 11) };
  const taken = new Set<number>([START[0] * H + START[1]]);
  for (const f of facilities) taken.add(f.x * H + f.y);
  for (const [wx, wy] of WELLS) taken.add(wx * H + wy);
  const dist = (x: number, y: number) => Math.abs(x - START[0]) + Math.abs(y - START[1]);
  const out: Survivor[] = [];
  for (const band of SURVIVOR_BANDS) {
    for (let tries = 0; tries < 2000; tries++) {
      const x = rngInt(rng, W), y = rngInt(rng, H - 1);
      const k = x * H + y;
      if (inert.has(k) || taken.has(k)) continue;
      const d = dist(x, y);
      if (d < band.min || d > band.max) continue;
      taken.add(k); out.push({ name: band.name, tag: band.tag, x, y }); break;
    }
  }
  return out;
}

export function generateMap(seed: number, cfg: Pick<SimConfig, 'jitter' | 'scatter' | 'scatterFrac' | 'validator'>): MapSpec {
  const jr = { rng: seedRng(Math.imul(seed, 104729) + 7) };
  const cells: CellSpec[] = [];
  for (let x = 0; x < W; x++) for (let y = 0; y < H; y++) {
    const { dmax, g, name } = district(x, y);
    const d0 = cfg.jitter ? 0.5 * dmax * (1 + rngUniform(jr, -cfg.jitter, cfg.jitter)) : 0.5 * dmax;
    cells.push({ x, y, name, well: wellName(x, y), dmax, g, d0 });
  }
  const scatteredInert = cfg.scatter ? makeScatter(seed, cfg.scatterFrac, cfg.validator) : [];
  const inertSet = new Set<number>(scatteredInert.map(([x, y]) => x * H + y));
  const facilities = placeFacilities(seed, inertSet);
  return { w: W, h: H, start: START, target: TARGET, wells: WELLS, cells, scatteredInert,
           facilities, survivors: placeSurvivors(seed, inertSet, facilities) };
}
