/** Map generator: the fixture generator (frontsim.py / export_fixtures.py) ported to TS.
 *  District, cap and growth are bit-identical to Python. Jitter and scatter use the TS PRNG,
 *  so a TS seed is a different map from the same Python seed; parity is tested from fixtures. */
import { District, MapSpec, CellSpec, Facility, SimConfig } from './types';
import { rngInt, rngUniform, seedRng, pyRound } from './prng';
import { districtBase, wellInfluence } from './districts';

export const W = 24, H = 22;               // city blocks; row 21 = river (inert)
export const START: [number, number] = [12, 20];
export const TARGET: [number, number] = [12, 10];   // "the foundry", 10 blocks north
export const WELLS: [number, number][] = [[12, 8], [2, 2], [21, 3], [2, 18], [22, 19]];

export function district(x: number, y: number): { dmax: number; g: number; name: District } {
  const { dmax: base, g, name } = districtBase(x, y, START);
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

/** PROTO-ASSUMPTION: facility placement. The doc (§8, §17) gives Foundry 5–9 blocks out, Arsenal in a
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
  place('Arsenal', (x, y) => x <= 8 && y >= 12 && dist(x, y) >= 5 && dist(x, y) <= 9);
  place('Turbine hall', (x, y) => x >= 16 && y >= 12 && dist(x, y) >= 5 && dist(x, y) <= 9);
  place('Refinery', (x, y) => y <= 13 && Math.abs(x - START[0]) >= 3 && dist(x, y) >= 8 && dist(x, y) <= 12);
  place('Power station', (x, y) => y <= 4 && dist(x, y) >= 15);
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
  return { w: W, h: H, start: START, target: TARGET, wells: WELLS, cells, scatteredInert,
           facilities: placeFacilities(seed, inertSet) };
}
