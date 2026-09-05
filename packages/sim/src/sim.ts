/** The front-rule sim: frontsim.py ported faithfully, power model included (config.power).
 *  Fixed 1 s tick. `step(state, commands)` is deterministic over its inputs and holds no module state;
 *  it mutates `state` in place (no per-tick allocation) and returns it. Snapshot with `cloneState`. */
import {
  Block, Edge, Engineer, SimState, SimConfig, Command, SimEvent, MapSpec, HourRow, District, LostEntry,
  DARK, CONTESTED, HELD, INERT, VOID,
} from './types';
import { hash01, rngNext, seedRng, pyRound } from './prng';
import { districtBase } from './districts';
import { latticeGraph, bfsHops, edgeId, LATTICE_AREA } from './graph';
import { createEngineer, tickEngineer, rifle, rifleHits, engineerCommand, bornFed, threatHooks } from './engineer';
import { SURVIVOR_UNLOCK_NAMES } from './map';
import { ASSEMBLER_MAG_PER_MIN, SHOT_MAGAZINE, SUBSTATION_KW, TURRET, EDGE_TURRETS } from './constants';

// ------------------------------------------------------------------ config

export const DEFAULT_CONFIG: SimConfig = {
  production: true,
  asmSchedule: [[600, 1], [1800, 2], [3000, 3], [10800, 4]],
  startAssemblers: 0,
  asmRate: ASSEMBLER_MAG_PER_MIN, asmEarlyRate: null, startAsmRate: null,
  hopper: EDGE_TURRETS * TURRET.hopper, bufferCap: 4000, startRounds: 200,   // D-B1-4-rider: the edge hopper is turrets on the segment × 50; C10: 20 magazines (§11; E1-start-min)
  unfed: 'substation', unfedN: 40, starveQuiet: false,
  scatter: true, scatterFrac: 0.09, validator: 'none',
  shadeThr: 0.3, hulkThr: 0.5, wakeCap: true, bloomBase: 4.0, jitter: 0.1, fallTiles: 30,
  hulkRoll: 'hash',
  bloomT: 120.0, bloomDrop: 0.9,
  relight: null,
  power: false, supply: 'track', headroom: 500, shortfall: null, draw: 'doc', interiorGrace: 0, asmTrack: 0,
  wellDeath: false, interleave: false,
  economy: false,
  walk: false,
  eco: {
    yieldPerMin: 1,
    claimCost: { copper: 5, steel: 10 },
    assemblerCost: { copper: 20, steel: 40 },
    startStock: { stone: 0, copper: 40, steel: 80 },
    startPatch: { steel: 240, perMin: 2, copper: 120, copperPerMin: 1 },
    magazineCost: { steel: SHOT_MAGAZINE.steel, copper: SHOT_MAGAZINE.copper },   // §12 Shot magazine (constants.ts)
    pool: { civ: 120, res: 120, ind: 120 },  // 120 min at yieldPerMin 1; the fixtures never draw on it (economy off)
  },
};

/** Fixture config (export_fixtures.py, snake_case) → SimConfig. */
export function configFromPython(py: Record<string, unknown>, over: Partial<SimConfig> = {}): SimConfig {
  const c: SimConfig = { ...DEFAULT_CONFIG, eco: { ...DEFAULT_CONFIG.eco } };
  const map: Record<string, keyof SimConfig> = {
    production: 'production', asm_schedule: 'asmSchedule', asm_rate: 'asmRate', asm_early_rate: 'asmEarlyRate',
    hopper: 'hopper', buffer_cap: 'bufferCap', start_rounds: 'startRounds', unfed: 'unfed', unfed_n: 'unfedN',
    starve_quiet: 'starveQuiet', scatter: 'scatter', scatter_frac: 'scatterFrac', validator: 'validator',
    shade_thr: 'shadeThr', hulk_thr: 'hulkThr', wake_cap: 'wakeCap', bloom_base: 'bloomBase', jitter: 'jitter',
    fall_tiles: 'fallTiles', hulk_roll: 'hulkRoll', bloom_t: 'bloomT', bloom_drop: 'bloomDrop',
    power: 'power', supply: 'supply', headroom: 'headroom', draw: 'draw', interior_grace: 'interiorGrace',
    asm_track: 'asmTrack',
  };
  for (const k in py) {
    const tk = map[k];
    if (tk !== undefined && py[k] !== null && py[k] !== undefined) (c as unknown as Record<string, unknown>)[tk] = py[k];
    if (k === 'relight' && Array.isArray(py[k])) {
      const r = py[k] as number[]; c.relight = { hour: r[0], minutes: r[1], mult: r[2] };
    }
    if (k === 'shortfall' && Array.isArray(py[k])) {
      const r = py[k] as number[]; c.shortfall = { pct: r[0], hour: r[1], minutes: r[2] };
    }
  }
  return { ...c, ...over };
}

// ------------------------------------------------------------------ topology (derived, not state)

/** D6: adjacency lives on the state (`st.nb`, ascending neighbour index; `st.deg` = max degree). Edge ids are
 *  `a * deg + slot` (graph.ts). This scratch mark array is the only derived topology left; it is keyed on the
 *  adjacency array so a cloned state gets its own. */
interface Scratch { mark: Int32Array; stamp: number }
const scratchCache = new WeakMap<number[][], Scratch>();
function scratch(st: SimState): Scratch {
  let sc = scratchCache.get(st.nb);
  if (!sc) { sc = { mark: new Int32Array(st.nb.length * st.deg), stamp: 0 }; scratchCache.set(st.nb, sc); }
  return sc;
}

/** Block index of a block coordinate: x*h+y on the lattice; a city's blocks are looked up by their centroid tile. */
const lookupCache = new WeakMap<Block[], Map<number, number>>();
export function idxOf(st: SimState, x: number, y: number): number {
  if (st.lattice) return x * st.h + y;
  let m = lookupCache.get(st.blocks);
  if (!m) { m = new Map(); st.blocks.forEach((b, i) => m!.set(b.x * st.h + b.y, i)); lookupCache.set(st.blocks, m); }
  return m.get(x * st.h + y) ?? -1;
}
export const inBounds = (st: SimState, x: number, y: number) => (st.lattice ? x >= 0 && x < st.w && y >= 0 && y < st.h : idxOf(st, x, y) >= 0);
export const isHostile = (s: number) => s === DARK || s === CONTESTED;
/** §5 step 4: a claimed block is Contested for 20 + 60·d seconds (d = its rot at the claim, after the wake bloom's
 *  drop); prompt B M5 sweeps the light along its ridges over the same time. [sim: B-M5-light] */
export const BURN_OFF_BASE_S = 20, BURN_OFF_PER_D_S = 60;
export const burnOffS = (d: number): number => BURN_OFF_BASE_S + BURN_OFF_PER_D_S * d;
export const isSolid = (s: number) => s === HELD || s === INERT;

/** M3 tile-layer hooks. flow.ts registers them at load (it imports this module, so this module cannot import it);
 *  the block tick calls them only on a state that has a flow layer, so a block-only state behaves exactly as before.
 *  syncEdges: mark edges covered by physical turrets (Edge.turrets/fire) and set their hopper to the turrets' sum.
 *  drainEdges: take this second's fired rounds out of those turrets. supplyKw: the Generators' output.
 *  demandKw: tile machines' draw (every placed machine when `all`, else those on powered cells).
 *  setLoad: the grid's numbers for this second, including the D-B3-4 throttle every tile machine runs at. */
export interface TileHooks {
  syncEdges(st: SimState): void;
  /** D-B1-4: whether physical turrets already cover this edge (a new edge born covered is born kitted). */
  covered(st: SimState, edgeId: number): boolean;
  drainEdges(st: SimState, fired: Float64Array): void;
  supplyKw(st: SimState): number;
  demandKw(st: SimState, all: boolean): number;
  setLoad(st: SimState, supply: number, demand: number, load: number, throttle: number): void;
}
export const tileHooks: { current: TileHooks | null } = { current: null };
const tiles = (st: SimState): TileHooks | null => (st.flow ? tileHooks.current : null);

// ------------------------------------------------------------------ state construction

export function createState(spec: MapSpec, config: SimConfig, seed: number): SimState {
  const { w, h } = spec;
  const lattice = !spec.graph;
  const graph = spec.graph ?? latticeGraph(w, h);
  const n = graph.nb.length;
  const blocks: Block[] = new Array(n);
  const inertSet = new Set(graph.inert);
  spec.cells.forEach((c, k) => {
    const i = lattice ? c.x * h + c.y : k;
    const base = c.base ?? districtBase(c.x, c.y, spec.start, h).dmax, gBase = c.gBase ?? districtBase(c.x, c.y, spec.start, h).g;
    const area = graph.area[i];
    blocks[i] = {
      x: c.x, y: c.y, state: (lattice ? c.y === h - 1 : inertSet.has(i)) ? INERT : DARK,
      d: c.d0, dmax: c.dmax, g: c.g, name: c.name, well: c.well, base, gBase,
      timer: -1, contestUntil: 0, upto: 0, awake: false,
      subOn: true, creep: 0, unfed: 0, fedTimer: 0, shadeOff: 0, unfedSince: -1, starveSince: -1,
      machines: 0, slots: slotsOf(area), area,
      // D6: the rubble pool scales with the lot's area (GAME-ASSUMPTION: linearly; the lattice lot is the unit)
      pool: poolOf(config, c.name) * area / LATTICE_AREA,
      exposed: true, exposedAt: 0,
    };
  });
  if (config.scatter) {
    for (const [x, y] of spec.scatteredInert) {
      const i = lattice ? x * h + y : blocks.findIndex(b => b.x === x && b.y === y);
      if (i >= 0) blocks[i].state = config.validator === 'inert-dark' ? VOID : INERT;
    }
  }
  const startIdx = lattice ? spec.start[0] * h + spec.start[1] : blocks.findIndex(b => b.x === spec.start[0] && b.y === spec.start[1]);
  const targetIdx = lattice ? spec.target[0] * h + spec.target[1] : blocks.findIndex(b => b.x === spec.target[0] && b.y === spec.target[1]);
  const s0 = blocks[startIdx];
  s0.state = HELD; s0.d = 0;
  s0.machines = config.startAssemblers > 0 ? 1 : 0;   // §5: the HQ is the one non-interior block that hosts a machine (the Mk1)
  const deg = Math.max(1, ...graph.nb.map(ns => ns.length));
  const wellIdx = spec.wells.map(([x, y]) => (lattice ? x * h + y : blocks.findIndex(b => b.x === x && b.y === y)));
  const st: SimState = {
    version: 1, seed, t: 0, rng: seedRng(seed), speed: 1, acc: 0,
    w, h, start: [spec.start[0], spec.start[1]], target: [spec.target[0], spec.target[1]],
    wells: spec.wells.map(p => [p[0], p[1]] as [number, number]),
    facilities: spec.facilities.map(f => ({ ...f })),
    survivors: (spec.survivors ?? []).map(f => ({ ...f })),
    config: JSON.parse(JSON.stringify(config)),
    blocks,
    nb: graph.nb, deg, len: graph.len, lattice, tileScale: graph.pitch,
    hops: Array.from(bfsHops(graph.nb, startIdx)), hopsT: Array.from(bfsHops(graph.nb, targetIdx)),
    wellHops: wellIdx.map(i => Array.from(bfsHops(graph.nb, i))),
    city: spec.city ? { ...spec.city } : undefined,
    engineer: null as unknown as Engineer,
    ring: [], edgeAt: new Array(n * deg).fill(-1),
    engagements: [],
    // GAME-ASSUMPTION (D6): on a city the start buffer fills every HQ hopper — a 4-neighbour HQ on 200 rounds has two
    // unfed fronts and falls at minute 7 before the first assembler runs (measured, seed 3). The lattice keeps 200
    // (its fixtures' number: 2 of the HQ's 3 hoppers). Question for the human: is the start ammo a fixed 200, or "the HQ ring"?
    buffer: config.production ? (lattice ? config.startRounds : Math.max(config.startRounds, config.hopper * graph.nb[startIdx].filter(j => !inertSet.has(j)).length)) : 0,
    asmManual: config.startAssemblers,
    dirty: true,
    fallen: new Array(n).fill(false),
    recentRounds: new Array(600).fill(0), recentShells: new Array(600).fill(0),
    recentRoundsSum: 0, recentShellsSum: 0, totalRounds: 0, totalShells: 0,
    hourly: [],
    stats: { lost: 0, retakes: 0, claims: 0, firstInterior: -1, firstFall: -1, firstUnfed: -1, unfedTotal: 0,
             lostLog: [], ringReorders: 0, assemblersAdded: 0, claimsRejected: 0, magsMade: 0,
             machinesLost: 0, ranDry: 0, dryLog: [],
             firstBrownout: -1, brownoutS: 0, throttleMin: 1, lostInWindow: 0, lostAfterWindow: 0, wellsDead: 0,
             ringDraw: 0, ringFired: 0, spentSteel: 0, spentCopper: 0, roundsLost: 0 },
    stock: { ...config.eco.startStock },
    patch: { steel: config.eco.startPatch.steel, copper: config.eco.startPatch.copper ?? 0 },
    power: { supply: 300, throttle: 1, short: false, shortAt: -1, okAt: -999, asmActive: 0, demandKw: [], supplyKw: [] },
    asmTrack: { n: 0, at: -1 },
    wellDead: spec.wells.map(() => false),
    wellEnclosedSince: spec.wells.map(() => -1),
    events: [],
  };
  st.engineer = createEngineer(st, startIdx);
  stateChange(st, startIdx);
  return st;
}

export function cloneState(st: SimState): SimState { return JSON.parse(JSON.stringify(st)); }

/** D6: one machine slot per ~600 buildable tiles, at least one (GAME-ASSUMPTION; the lattice lot's 576 tiles give one). */
export const slotsOf = (area: number) => Math.max(1, Math.floor(area / 600));

function poolOf(config: SimConfig, name: District): number {
  return name === 'civ' ? config.eco.pool.civ : name === 'res' ? config.eco.pool.res : name === 'ind' ? config.eco.pool.ind : 0;
}

// ------------------------------------------------------------------ counts

export function frontage(st: SimState): number {
  const B = st.blocks;
  let f = 0;
  for (let i = 0; i < B.length; i++) {
    if (B[i].state !== HELD) continue;
    const ns = st.nb[i];
    for (let k = 0; k < ns.length; k++) if (isHostile(B[ns[k]].state)) f++;
  }
  return f;
}

export function interior(st: SimState): number {
  const B = st.blocks;
  let n = 0;
  for (let i = 0; i < B.length; i++) {
    if (B[i].state !== HELD) continue;
    const ns = st.nb[i];
    let all = true;
    for (let k = 0; k < ns.length; k++) if (!isSolid(B[ns[k]].state)) { all = false; break; }
    if (all) n++;
  }
  return n;
}

/** §5: a Held block with no Dark, Contested or Void 4-neighbour (its streets are free ground for the factory). */
export function isInterior(st: SimState, i: number): boolean {
  const B = st.blocks;
  if (B[i].state !== HELD) return false;
  const ns = st.nb[i];
  for (let k = 0; k < ns.length; k++) if (!isSolid(B[ns[k]].state)) return false;
  return true;
}

/** The interior block that would take the next assembler: a free slot, nearest the start by street hops, index order on ties.
 *  GAME-ASSUMPTION: placement is automatic; the doc has the player place machines by hand. Returns -1 if none. */
export function freeSlot(st: SimState): number {
  const B = st.blocks;
  let best = -1, bd = 0;
  for (let i = 0; i < B.length; i++) {
    if (B[i].machines >= B[i].slots || !isInterior(st, i)) continue;
    const d = st.hops[i];
    if (best < 0 || d < bd) { best = i; bd = d; }
  }
  return best;
}

export function heldCount(st: SimState): number {
  let n = 0;
  for (const b of st.blocks) if (b.state === HELD) n++;
  return n;
}

/** Frontage if block i were Held (i is Dark): F − held neighbours + hostile neighbours. Equals the Python
 *  set-and-restore computation exactly. */
export function frontageIf(st: SimState, i: number, F: number): number {
  const B = st.blocks, ns = st.nb[i];
  let f = F;
  for (let k = 0; k < ns.length; k++) {
    const s = B[ns[k]].state;
    if (s === HELD) f--; else if (isHostile(s)) f++;
  }
  return f;
}

/** Interior count if block i were Held. */
export function interiorIf(st: SimState, i: number, I: number): number {
  const B = st.blocks, ns = st.nb[i];
  let n = I;
  let selfIn = true;
  for (let k = 0; k < ns.length; k++) {
    const j = ns[k];
    if (!isSolid(B[j].state)) selfIn = false;
    if (B[j].state === HELD) {
      const ms = st.nb[j];
      let all = true;
      for (let m = 0; m < ms.length; m++) { const q = ms[m]; if (q !== i && !isSolid(B[q].state)) { all = false; break; } }
      if (all) n++;   // j was not interior (i was hostile), becomes interior
    }
  }
  if (selfIn) n++;
  return n;
}

/** Dark blocks with a Held 4-neighbour, in grid order. Writes into `out`, returns count. */
export function candidates(st: SimState, out: number[]): number {
  const B = st.blocks;
  out.length = 0;
  for (let i = 0; i < B.length; i++) {
    if (B[i].state !== DARK) continue;
    const ns = st.nb[i];
    for (let k = 0; k < ns.length; k++) if (B[ns[k]].state === HELD) { out.push(i); break; }
  }
  return out.length;
}

export function isCandidate(st: SimState, i: number): boolean {
  const B = st.blocks;
  if (B[i].state !== DARK) return false;
  const ns = st.nb[i];
  for (let k = 0; k < ns.length; k++) if (B[ns[k]].state === HELD) return true;
  return false;
}

// ------------------------------------------------------------------ rot growth

function gOf(st: SimState, b: Block): number {
  const rl = st.config.relight;
  if (rl && b.well && rl.hour * 3600 <= st.t && st.t < rl.hour * 3600 + rl.minutes * 60) return b.g * rl.mult;
  return b.g;
}

function inRelight(st: SimState): boolean {
  const rl = st.config.relight;
  return !!rl && rl.hour * 3600 <= st.t && st.t < rl.hour * 3600 + rl.minutes * 60;
}

/** Lazy growth: asleep blocks are brought up to date in closed form when something looks at them. */
function catchUp(st: SimState, b: Block, upto: number): void {
  const n = upto - b.upto;
  if (n <= 0) return;
  const g = gOf(st, b);
  if (n === 1) b.d += g * (b.dmax - b.d);
  else b.d = b.dmax - (b.dmax - b.d) * Math.pow(1 - g, n);
  b.upto = upto;
}

/** Rot of block i as of now, without mutating (for tooltips on asleep blocks). */
export function rotOf(st: SimState, i: number): number {
  const b = st.blocks[i];
  if (b.state !== DARK) return b.d;
  const n = st.t - b.upto;
  if (n <= 0) return b.d;
  const g = gOf(st, b);
  return n === 1 ? b.d + g * (b.dmax - b.d) : b.dmax - (b.dmax - b.d) * Math.pow(1 - g, n);
}

function recomputeAwake(st: SimState, i: number): void {
  const b = st.blocks[i];
  if (b.state !== DARK) { b.awake = false; return; }
  const ns = st.nb[i], B = st.blocks;
  let aw = false;
  for (let k = 0; k < ns.length; k++) { const s = B[ns[k]].state; if (s === HELD || s === CONTESTED) { aw = true; break; } }
  if (!aw) b.timer = -1;
  b.awake = aw;
}

function stateChange(st: SimState, i: number): void {
  st.dirty = true;
  recomputeAwake(st, i);
  const ns = st.nb[i];
  for (let k = 0; k < ns.length; k++) recomputeAwake(st, ns[k]);
}

// ------------------------------------------------------------------ blooms

function roll(st: SimState, x: number, y: number): number {
  if (st.config.hulkRoll === 'hash') return hash01(st.seed, st.t, x, y);
  return rngNext(st);
}

function bloomSize(st: SimState, d: number, x: number, y: number): [number, number, number] {
  const cfg = st.config;
  const crawlers = pyRound(cfg.bloomBase + 36 * d);
  const shades = d >= cfg.shadeThr ? Math.floor(crawlers / 8) : 0;
  const hulk = (d >= cfg.hulkThr && roll(st, x, y) < 0.4) ? 1 : 0;
  return [crawlers, shades, hulk];
}

function wakeBloom(st: SimState, d: number, x: number, y: number): [number, number, number] {
  const [cr, sh, hu] = bloomSize(st, d, x, y);
  if (st.config.wakeCap) {
    const cr2 = Math.min(40, 2 * cr);
    const sh2 = d >= st.config.shadeThr ? Math.floor(cr2 / 8) : 0;
    return [cr2, sh2, 2 * hu];
  }
  return [2 * cr, 2 * sh, 2 * hu];
}

function recordBloom(st: SimState, i: number, cr: number, sh: number, hu: number, wake: boolean, id?: number): void {
  const b = st.blocks[i];
  const rounds = cr * 3 + sh * 10, shells = hu * 10;
  st.totalRounds += rounds; st.totalShells += shells;
  const slot = st.t % 600;
  st.recentRounds[slot] += rounds; st.recentRoundsSum += rounds;
  st.recentShells[slot] += shells; st.recentShellsSum += shells;
  const ev: SimEvent = { type: 'bloom', t: st.t, x: b.x, y: b.y, cr, sh, hu, wake };
  if (id !== undefined) ev.id = id;   // RI-03: an activation's wake bloom carries its commissioning id
  st.events.push(ev);
  if (!st.config.production) return;
  const ns = st.nb[i], B = st.blocks;
  let ne = 0;
  for (let k = 0; k < ns.length; k++) if (B[ns[k]].state === HELD) ne++;
  if (ne === 0) return;
  for (let k = 0; k < ns.length; k++) {
    const n = ns[k];
    if (B[n].state !== HELD) continue;
    st.engagements.push({ id: edgeId(st, n, i), cr: cr / ne, sh: sh / ne, rcr: cr / ne / 15.0, rsh: sh / ne / 15.0 });
  }
}

// ------------------------------------------------------------------ the ammo ring

export function asmCount(st: SimState, t: number): number {
  if (st.config.asmTrack > 0) {
    // frontsim.py asm_track: every 10 min, add an assembler while demand over the last 10 min exceeds the fraction of production
    const a = st.asmTrack;
    if (t >= 600 && t % 600 === 0 && a.at !== t) {
      a.at = t;
      const recent = st.recentRoundsSum / 100.0;   // mag/min over the last 10 min
      if (a.n === 0) a.n = 1;
      else if (recent / (a.n * asmRate(st, t)) > st.config.asmTrack && a.n < 8) a.n++;
    }
    return a.n + st.asmManual;
  }
  let n = 0;
  for (const [at, k] of st.config.asmSchedule) if (t >= at) n = k;
  return n + st.asmManual;
}

/** Assemblers running: every one built. D-B3-4: nothing is switched off by power; a shortfall slows them all
 *  (`productionMagPerMin` carries the throttle). */
export function asmActive(st: SimState, t: number): number {
  return asmCount(st, t);
}

export function asmRate(st: SimState, t: number): number {
  const c = st.config;
  if (c.asmEarlyRate !== null && t < 3 * 3600) return c.asmEarlyRate;
  return c.asmRate;
}

/** Magazines per minute from every assembler. With `startAsmRate` set (proto: the starting "Mk1" assembler) the
 *  first `startAssemblers` hand-built assemblers run at that rate and every other one at `asmRate`. Under a
 *  brownout (D-B3-4) every assembler runs at supply ÷ demand, so the rate is scaled by the throttle. */
export function productionMagPerMin(st: SimState, t: number): number {
  const c = st.config;
  const n = asmActive(st, t);
  const thr = c.power ? st.power.throttle : 1;
  if (c.startAsmRate === null) return n * asmRate(st, t) * thr;
  const hq = st.blocks[idxOf(st, st.start[0], st.start[1])];
  const mk1 = Math.min(hq.machines > 0 ? Math.min(st.asmManual, c.startAssemblers) : 0, n);   // the Mk1 falls with the HQ
  return (mk1 * c.startAsmRate + (n - mk1) * asmRate(st, t)) * thr;
}

function rebuildEdgeAt(st: SimState): void {
  const ea = st.edgeAt;
  for (let i = 0; i < ea.length; i++) ea[i] = -1;
  for (let i = 0; i < st.ring.length; i++) ea[st.ring[i].id] = i;
}

/** Bring the ring in line with the map: drop edges that no longer face a hostile block (hopper back to the
 *  buffer), add new ones in sorted position order at the end of the ring. */
export function syncEdges(st: SimState): void {
  const B = st.blocks, sc = scratch(st), mark = sc.mark, deg = st.deg;
  const stamp = ++sc.stamp;
  for (let i = 0; i < B.length; i++) {
    if (B[i].state !== HELD) continue;
    const ns = st.nb[i];
    for (let k = 0; k < ns.length; k++) if (isHostile(B[ns[k]].state)) mark[i * deg + k] = stamp;
  }
  const ring = st.ring;
  let w = 0;
  for (let r = 0; r < ring.length; r++) {
    const e = ring[r];
    if (mark[e.id] !== stamp) { if (!e.turrets) st.buffer += e.hopper; st.edgeAt[e.id] = -1; continue; }   // M3: turrets keep their rounds
    ring[w++] = e;
  }
  ring.length = w;
  // D5: with `walk` on, a new edge starts unkitted unless the engineer stands on the block with a kit in their pockets
  // or (D-B1-4) physical turrets already cover it: the turrets are the kit, so an edge born covered is born kitted,
  // whichever block it is on and whenever it is born. (M1 had an HQ-at-t=0 special case here instead; D-B1-4 replaced
  // it with the rule, since every HQ segment now gets its turrets from its length.) A block-only state (the harness's
  // block sim, no tile layer) has no turrets to ask: there the §11 start ring is abstract (GAME-ASSUMPTION), so its HQ
  // edges at t = 0 are born kitted as the block-level stand-in for `startTurrets` and every later edge waits for the bots' kits.
  const eng = st.engineer, walk = st.config.walk, th = tiles(st), startIdx = idxOf(st, st.start[0], st.start[1]);
  for (let i = 0; i < B.length; i++) {
    if (B[i].state !== HELD) continue;
    const ns = st.nb[i];
    for (let k = 0; k < ns.length; k++) {
      const id = i * deg + k;
      if (mark[id] !== stamp || st.edgeAt[id] !== -1) continue;
      const e: Edge = { id, a: i, b: ns[k], hopper: 0, empty: 0, born: st.t };
      if (walk) {
        if (th ? th.covered(st, id) : (i === startIdx && st.t === 0)) e.kit = true;
        else if (eng.block === i && (eng.inv.kit ?? 0) >= 1) { eng.inv.kit -= 1; if (eng.inv.kit <= 0) delete eng.inv.kit; e.kit = true; }
        else e.kit = false;
      }
      if (e.kit !== false) bornFed(st, e);
      ring.push(e);
      st.edgeAt[id] = -2;   // placeholder, fixed below
    }
  }
  rebuildEdgeAt(st);
}

// ------------------------------------------------------------------ power model (frontsim.py, ported line for line)

/** §13/§5 substation draw. `unshed` = what it would draw if it were on. */
export function subDraw(st: SimState, b: Block, unshed = false): number {
  const c = st.config, t = st.t;
  if (!unshed && (!b.subOn || t < b.shadeOff)) return 0;
  if (c.draw === 'flat') return 120;
  const half = c.draw === 'half';   // D1: 100/20 kW (constants.ts SUBSTATION_KW); 'doc' is the pre-D1 200/40
  if (!b.exposed) return half ? SUBSTATION_KW.interior : 40;
  if (c.interiorGrace && t - b.exposedAt < c.interiorGrace) return half ? SUBSTATION_KW.interior : 40;
  return half ? SUBSTATION_KW.front : 200;
}

/** Demand with every substation and assembler on (what the supply tracks). */
export function demandUnshed(st: SimState): number {
  const half = st.config.draw === 'half';
  let d = 0;
  for (const b of st.blocks) {
    if (b.state === HELD) d += subDraw(st, b, true);
    else if (b.state === CONTESTED) d += half ? SUBSTATION_KW.front : 200;
  }
  const th = tiles(st);
  return d + asmCount(st, st.t) * 220 + (th ? th.demandKw(st, true) : 0);
}

/** Demand as drawn now (substations that are off — unfed, shade — excluded). */
export function demandKw(st: SimState): number {
  const half = st.config.draw === 'half';
  let d = 0;
  for (const b of st.blocks) {
    if (b.state === HELD) d += subDraw(st, b);
    else if (b.state === CONTESTED) d += half ? SUBSTATION_KW.front : 200;
  }
  const th = tiles(st);
  return d + st.power.asmActive * 220 + (th ? th.demandKw(st, false) : 0);
}

/** §15 generator schedule (kW available by tick) for supply = 'schedule'. */
export const SUPPLY_SCHEDULE: [number, number][] =
  [[0, 300], [1800, 600], [3600, 900], [7200, 1200], [10800, 1500], [14400, 1800], [18000, 2100], [21600, 7000]];

export function inWindow(st: SimState): boolean {
  const sf = st.config.shortfall;
  return sf !== null && sf.hour * 3600 <= st.t && st.t < sf.hour * 3600 + sf.minutes * 60;
}

export function effectiveSupply(st: SimState): number {
  let s = st.power.supply;
  if (st.config.supply === 'schedule') {
    s = 300;
    for (const [at, kw] of SUPPLY_SCHEDULE) if (st.t >= at) s = kw;
  }
  const sf = st.config.shortfall;
  if (sf && sf.hour * 3600 <= st.t && st.t < sf.hour * 3600 + sf.minutes * 60) s *= 1 - sf.pct / 100;
  return s;
}

// ------------------------------------------------------------------ §5 rules the Python sim never had (config-gated)

/** §5 "timers interleaved so no two adjacent cells bloom within 10 s": push `when` past any awake neighbour's timer. */
function interleaved(st: SimState, i: number, when: number): number {
  if (!st.config.interleave) return when;
  const ns = st.nb[i], B = st.blocks;
  for (let guard = 0; guard < 8; guard++) {
    let moved = false;
    for (let k = 0; k < ns.length; k++) {
      const n = B[ns[k]];
      if (n.state !== DARK || !n.awake || n.timer < 0) continue;
      if (Math.abs(when - n.timer) < 10) { when = n.timer + 10; moved = true; }
    }
    if (!moved) break;
  }
  return when;
}

/** §5 well death, part 1 (on a dirty map): note when every in-bounds neighbour of a live well turned solid. */
function wellEnclosure(st: SimState): void {
  for (let k = 0; k < st.wells.length; k++) {
    if (st.wellDead[k]) continue;
    const [wx, wy] = st.wells[k];
    const wi = idxOf(st, wx, wy);
    if (st.blocks[wi].state !== DARK) { st.wellEnclosedSince[k] = -1; continue; }
    const ns = st.nb[wi];
    let enclosed = true;
    for (let q = 0; q < ns.length; q++) if (!isSolid(st.blocks[ns[q]].state)) { enclosed = false; break; }
    if (!enclosed) st.wellEnclosedSince[k] = -1;
    else if (st.wellEnclosedSince[k] < 0) st.wellEnclosedSince[k] = st.t;
  }
}

/** §5 well death, part 2: after five minutes enclosed the well dies and its blocks fall back to the district's cap and growth. */
function wellDeaths(st: SimState): void {
  for (let k = 0; k < st.wells.length; k++) {
    if (st.wellDead[k] || st.wellEnclosedSince[k] < 0 || st.t - st.wellEnclosedSince[k] < 300) continue;
    st.wellDead[k] = true; st.stats.wellsDead++;
    const [wx, wy] = st.wells[k];
    st.events.push({ type: 'well-dead', t: st.t, x: wx, y: wy });
    for (let i = 0; i < st.blocks.length; i++) {
      const b = st.blocks[i];
      if (st.wellHops[k][i] > 3 || st.wellHops[k][i] < 0) continue;
      // GAME-ASSUMPTION: the block keeps the influence of any other live well in range; the doc has no rule for overlap.
      let infl = 0;
      for (let j = 0; j < st.wells.length; j++) {
        if (st.wellDead[j]) continue;
        const wd = st.wellHops[j][i];
        if (wd >= 0 && wd <= 3) infl = Math.max(infl, 1 - wd / 4);
      }
      if (b.state === DARK && b.awake) catchUp(st, b, st.t);
      b.dmax = Math.min(1, b.base + 0.3 * infl);
      b.g = b.gBase * (1 + 3 * infl);
      b.well = infl > 0;
    }
  }
}

function fall(st: SimState, i: number, reason: LostEntry['reason']): void {
  const b = st.blocks[i];
  // diagnostics for the fall event (frontsim.py fall_delays): first-unfed → fall delay and the starved edge's district
  const delay = b.unfedSince >= 0 ? st.t - b.unfedSince : -1;
  let starved = '-';
  for (let k = 0; k < st.deg; k++) {
    const ri = st.edgeAt[i * st.deg + k];
    if (ri >= 0 && st.ring[ri].hopper <= 1e-9) { const n = st.blocks[st.ring[ri].b]; starved = n.well ? 'well' : n.name; break; }
  }
  b.state = DARK; b.d = 0.3; b.upto = st.t + 1; b.timer = -1; b.creep = 0; b.unfed = 0;
  b.subOn = true; b.shadeOff = 0; b.fedTimer = 0;
  b.unfedSince = -1; b.starveSince = -1;
  if (st.config.shortfall) {
    const sf = st.config.shortfall;
    if (inWindow(st)) st.stats.lostInWindow++;
    else if (st.t >= sf.hour * 3600 + sf.minutes * 60) st.stats.lostAfterWindow++;
  }
  if (b.machines > 0) {   // §5: a block that falls loses its machines
    st.asmManual = Math.max(0, st.asmManual - b.machines); st.stats.machinesLost += b.machines; b.machines = 0;
    st.events.push({ type: 'machine-lost', t: st.t, x: b.x, y: b.y, count: asmCount(st, st.t) });
  }
  st.stats.lost++;
  st.stats.lostLog.push({ t: st.t, reason, x: b.x, y: b.y });
  if (st.stats.firstFall < 0) st.stats.firstFall = st.t;
  st.fallen[i] = true;
  st.events.push({ type: 'fall', t: st.t, x: b.x, y: b.y, reason, delay, starved });
  stateChange(st, i);
  if (st.config.production) syncEdges(st);   // edges change now, not at the next claim
}

// ------------------------------------------------------------------ commands

function facilityAt(st: SimState, x: number, y: number): string | null {
  for (const f of st.facilities) if (f.x === x && f.y === y) return f.name;
  return null;
}
function survivorAt(st: SimState, x: number, y: number): string | null {
  for (const f of st.survivors) if (f.x === x && f.y === y) return f.name;
  return null;
}

function rubbleOf(name: District): 'stone' | 'copper' | 'steel' | null {
  return name === 'civ' ? 'stone' : name === 'res' ? 'copper' : name === 'ind' ? 'steel' : null;
}

/** The legacy map claim of block (x, y): a candidate (Dark next to Held), paid from the Depot chest's stock, Contested
 *  at once. The block-level bots and the snapshot fixture call it; the harness's labelled legacy E-hour comparison
 *  sends it. RI-03 (plan §4.1): the tile layer's one claim path is `activate` (flow.ts) — the game's map view no
 *  longer sends `claim`; selecting a Dark block there charges nothing. Both paths end in `startContested`. */
export function claim(st: SimState, x: number, y: number): boolean {
  if (!inBounds(st, x, y)) return reject(st, x, y, 'out of bounds');
  const i = idxOf(st, x, y);
  if (!isCandidate(st, i)) return reject(st, x, y, st.blocks[i].state === DARK ? 'not adjacent to a Held block' : 'not Dark');
  if (st.config.economy) {
    const c = st.config.eco.claimCost;
    if (st.stock.copper < c.copper || st.stock.steel < c.steel) return reject(st, x, y, 'cannot afford 10 wire, 5 frames');
    st.stock.copper -= c.copper; st.stock.steel -= c.steel;
    st.stats.spentCopper = (st.stats.spentCopper ?? 0) + c.copper; st.stats.spentSteel = (st.stats.spentSteel ?? 0) + c.steel;   // ?? 0: a pre-RI-01 snapshot
  }
  startContested(st, i, 'map');
  return true;
}

/** RI-03: the one Contested start both paths share — the wake bloom (§5 step 3, once: the commissioning event's
 *  configured response), the burn-off timer (§5 step 4), the claim counters and the `claim` event. The caller has
 *  checked the prerequisites and taken the payment; `id` is an activation's commissioning id, stamped on the claim
 *  event and its wake bloom so telemetry shows one response per attempt and never a bloom and an encounter twice. */
export function startContested(st: SimState, i: number, via: 'map' | 'activate', id?: number): void {
  const t = st.t, b = st.blocks[i], x = b.x, y = b.y;
  const F = frontage(st), I = interior(st);
  // GAME-ASSUMPTION: the claim event's F/I "after" are projections at claim time (as if the block were Held now),
  // not the values when it actually turns Held; the brief's telemetry does not say which.
  const fAfter = frontageIf(st, i, F), iAfter = interiorIf(st, i, I);
  catchUp(st, b, t);
  const [cr, sh, hu] = wakeBloom(st, b.d, x, y);
  const dBefore = b.d;
  recordBloom(st, i, cr, sh, hu, true, id);
  b.state = CONTESTED; b.contestUntil = t + burnOffS(b.d);   // §5 step 4: 20 + 60·d s of burn-off [sim: B-M5-light]
  b.awake = false;
  const retake = st.fallen[i];
  if (retake) st.stats.retakes++;
  st.stats.claims++;
  stateChange(st, i);
  const ev: SimEvent = { type: 'claim', t, x, y, district: b.name, well: b.well, d: dBefore,
                         fBefore: F, fAfter, iBefore: I, iAfter, retake, cr, sh, hu, via };
  if (id !== undefined) ev.id = id;
  st.events.push(ev);
}

function reject(st: SimState, x: number, y: number, reason: string): boolean {
  st.stats.claimsRejected++;
  st.events.push({ type: 'claim-rejected', t: st.t, x, y, reason });
  return false;
}

/** Reorder the ring: listed ids first in the given order, unlisted edges after in their current order. */
export function setRingOrder(st: SimState, ids: number[]): void {
  const ring = st.ring;
  const byId = new Map<number, typeof ring[number]>();
  for (const e of ring) byId.set(e.id, e);
  const next: typeof ring = [];
  const seen = new Set<number>();
  for (const id of ids) { const e = byId.get(id); if (e && !seen.has(id)) { next.push(e); seen.add(id); } }
  for (const e of ring) if (!seen.has(e.id)) next.push(e);
  ring.length = 0;
  for (const e of next) ring.push(e);
  rebuildEdgeAt(st);
  st.stats.ringReorders++;
  st.events.push({ type: 'reorder', t: st.t, ids: ring.map(e => e.id) });
}

/** §5: an assembler needs an empty machine slot, and only Interior blocks have one (a front block's slot is its
 *  defence ring; the HQ's holds the Mk1). The slot rule is a doc rule, not an economy rule: it applies with economy=0 too. */
export function addAssembler(st: SimState): boolean {
  // RI-01 (D-P4-5): on a city with the tile layer every Assembler is a placed machine (flow.ts `place`); the
  // block-level stand-in that made abstract magazines from a slot is refused there. Block-only runs keep it.
  if (st.flow) { st.events.push({ type: 'assembler-rejected', t: st.t, reason: 'place an Assembler on the tiles (RI-01, D-P4-5)' }); return false; }
  const i = freeSlot(st);
  if (i < 0) { st.events.push({ type: 'assembler-rejected', t: st.t, reason: 'no free interior slot' }); return false; }
  if (st.config.economy) {
    const c = st.config.eco.assemblerCost;
    if (st.stock.copper < c.copper || st.stock.steel < c.steel) { st.events.push({ type: 'assembler-rejected', t: st.t, reason: 'cannot afford' }); return false; }
    st.stock.copper -= c.copper; st.stock.steel -= c.steel;
    st.stats.spentCopper = (st.stats.spentCopper ?? 0) + c.copper; st.stats.spentSteel = (st.stats.spentSteel ?? 0) + c.steel;
  }
  const b = st.blocks[i];
  b.machines++;
  st.asmManual++;
  st.stats.assemblersAdded++;
  st.events.push({ type: 'assembler', t: st.t, count: asmCount(st, st.t), x: b.x, y: b.y });
  return true;
}

export function applyCommands(st: SimState, commands: readonly Command[]): void {
  for (const c of commands) {
    switch (c.type) {
      case 'claim': claim(st, c.x, c.y); break;
      case 'ringOrder': setRingOrder(st, c.ids); break;
      case 'addAssembler': addAssembler(st); break;
      case 'setSpeed': st.speed = Math.max(0, c.mult); break;
      default: engineerCommand(st, c);
    }
  }
}

// ------------------------------------------------------------------ one tick

const NO_COMMANDS: readonly Command[] = [];

export function step(st: SimState, commands: readonly Command[] = NO_COMMANDS): SimState {
  const cfg = st.config, B = st.blocks, t = st.t;
  const prod = cfg.production;
  if (commands.length) applyCommands(st, commands);

  // contested -> held
  for (let i = 0; i < B.length; i++) {
    const b = B[i];
    if (b.state === CONTESTED && t >= b.contestUntil) {
      b.state = HELD; b.d = 0; b.timer = -1; b.subOn = true; b.creep = 0;
      b.unfed = 0; b.shadeOff = 0; b.unfedSince = -1; b.starveSince = -1;
      st.fallen[i] = false;
      stateChange(st, i);
      // GAME-ASSUMPTION: a facility is "reached" when its own block turns Held; the proto only toasts it.
      // GAME-ASSUMPTION: a survivor group joins ("we're in", §8) when its block turns Held; the Electricians' unlocks
      // land on the toolbar from this event (prompt B M3, flow.ts `survivorJoined`); the other groups have no effect yet.
      const fac = facilityAt(st, b.x, b.y), sv = survivorAt(st, b.x, b.y);
      st.events.push({ type: 'held', t, x: b.x, y: b.y, facility: fac, survivor: sv, unlocks: sv ? [...(SURVIVOR_UNLOCK_NAMES[sv] ?? [])] : [] });
      if (fac === 'Tram depot' && !st.engineer.truckFound) { st.engineer.truckFound = true; st.events.push({ type: 'truck', t }); }   // D5
    }
  }

  // rot growth + blooms in awake dark blocks (asleep blocks grow lazily, closed form)
  const relight = inRelight(st);
  for (let i = 0; i < B.length; i++) {
    const b = B[i];
    if (b.state !== DARK || !b.awake) continue;
    catchUp(st, b, t + 1);
    if (b.timer < 0) b.timer = interleaved(st, i, t + cfg.bloomT / (0.5 + b.d));
    if (t >= b.timer) {
      const [cr, sh, hu] = relight ? wakeBloom(st, b.d, b.x, b.y) : bloomSize(st, b.d, b.x, b.y);
      recordBloom(st, i, cr, sh, hu, false);
      b.d = Math.max(0.05, b.d * cfg.bloomDrop);
      b.timer = interleaved(st, i, t + cfg.bloomT / (0.5 + b.d));
    }
  }

  // exposure bookkeeping (power) and edges (production)
  if (st.dirty) {
    if (cfg.power) {
      for (let i = 0; i < B.length; i++) {
        const b = B[i];
        if (b.state !== HELD) continue;
        const ns = st.nb[i];
        let ex = false;
        for (let k = 0; k < ns.length; k++) if (isHostile(B[ns[k]].state)) { ex = true; break; }
        if (ex && !b.exposed) b.exposedAt = t;
        b.exposed = ex;
      }
    }
    if (cfg.wellDeath) wellEnclosure(st);
    if (prod) syncEdges(st);
  }
  if (cfg.wellDeath) wellDeaths(st);

  // D5: the engineer walks (and lays kits) before the ring fills, so an edge kitted this second is filled this second
  // and never reads red for the one tick between the kit landing and the fill (D-R2). The tile layer ticks the
  // engineer 20× a second ahead of this step when it is present (flow.ts).
  if (!st.flow) tickEngineer(st, 1);

  // ---- ammo production and the ring ----
  if (prod) {
    let made = productionMagPerMin(st, t) / 6.0;   // rounds per second
    // Doc-derived (§12): a magazine is 2 steel + 1 Cu. With the economy on the assemblers make only what the stock
    // can pay for, and pay only for what the ring and the buffer take (an assembler whose output is full stops).
    if (cfg.economy && made > 0) {
      const mc = cfg.eco.magazineCost;
      const bySteel = mc.steel > 0 ? st.stock.steel / mc.steel * 10 : Infinity;
      const byCopper = mc.copper > 0 ? st.stock.copper / mc.copper * 10 : Infinity;
      made = Math.max(0, Math.min(made, bySteel, byCopper));
    }
    let avail = st.buffer + made;
    st.buffer = 0;
    const ring = st.ring, startIdx = idxOf(st, st.start[0], st.start[1]);
    const th = tiles(st);
    if (th) th.syncEdges(st);   // M3: edges with physical turrets read their hoppers from the turrets
    for (let r = 0; r < ring.length; r++) {
      const e = ring[r];
      if (e.turrets) continue;   // M3: fed by inserters and hands, not by the ring
      if (e.kit === false) continue;   // D5: no kit laid, no turret, nothing to fill
      if (e.cut !== undefined && t < e.cut) continue;   // E-rifle rescue: the belt has not arrived yet
      if (cfg.starveQuiet && e.a !== startIdx && B[e.b].d < 0.3 && B[e.b].state === DARK) {
        if (B[e.a].starveSince < 0) B[e.a].starveSince = t;
        continue;
      }
      const need = cfg.hopper - e.hopper;
      const give = Math.min(need, avail);
      if (give > 0) { e.hopper += give; avail -= give; st.stats.ringDraw = (st.stats.ringDraw ?? 0) + give; }
    }
    if (cfg.economy && made > 0) {
      const unmade = Math.min(made, Math.max(0, avail - cfg.bufferCap));   // rounds with nowhere to go: not made, not paid
      const mags = (made - unmade) / 10;
      st.stock.steel -= mags * cfg.eco.magazineCost.steel; st.stock.copper -= mags * cfg.eco.magazineCost.copper;
      st.stats.magsMade += mags;
    }
    if (avail - made > cfg.bufferCap) st.stats.roundsLost = (st.stats.roundsLost ?? 0) + avail - made - cfg.bufferCap;   // RI-01 ledger: buffer rounds over the cap are gone (production over the cap was never made)
    st.buffer = Math.min(cfg.bufferCap, avail);
    // engagements: crawlers arrive over 15 s; fed ones die, unfed ones proceed
    const eng = st.engagements;
    let w = 0;
    // M3: an edge with physical turrets fires at most what they can this second (5 rounds/s each, §13); the rounds
    // come out of the turrets' hoppers below. A stand-in edge has no rate limit, as before.
    const fired = th ? new Float64Array(ring.length) : null;
    // D-B1-5 telemetry (§19): a second is "danger" when a crawler is on the player — retaliation from a rifle kill,
    // or crawlers past the turrets on the block the engineer stands on. dangerShot: of those, seconds the rifle fired.
    const player = st.engineer;
    let dangerNow = false, shotNow = false;
    // GAME-ASSUMPTION (M4): on a city with the tile layer, an edge with physical turrets hands its arrivals to the tiles (threat.ts):
    // the turrets shoot them there, out of the same hoppers, and what reaches the substation counts below. A stand-in
    // edge (D-P4-9, a claimed block's abstract kit) still feeds here and hands only what got past it to the tiles.
    const thr = th && !st.lattice ? threatHooks.current : null;
    for (let q = 0; q < eng.length; q++) {
      const en = eng[q];
      const ri = st.edgeAt[en.id];
      if (ri < 0) continue;
      const e = ring[ri], hp = B[e.a];
      const a = Math.min(en.cr, en.rcr); en.cr -= a;
      const s_ = Math.min(en.sh, en.rsh); en.sh -= s_;
      if (thr && e.turrets && thr.spawn(st, en.id, a, s_, false)) { if (en.cr > 1e-9 || en.sh > 1e-9) eng[w++] = en; continue; }
      let budget = e.kit === false ? 0 : e.turrets && fired ? Math.min(e.hopper, (e.fire ?? 0) - fired[ri]) : e.hopper;   // D5: an unkitted edge fires nothing
      const fed = Math.min(a, budget / 3.0); e.hopper -= fed * 3.0; budget -= fed * 3.0;
      let un = a - fed;
      if (un > 1e-9) {   // D5: the rifle takes what the turrets missed — the bots' targeted rifle, or the aimed rounds walk.ts put on this edge
        const k = player.firing === en.id ? rifle(st, en.id, un, 1) : rifleHits(st, en.id, un);
        if (k > 1e-9) { dangerNow = true; shotNow = true; }
        un -= k;
      }
      if (un > 1e-9 && e.a === player.block && player.down < 0 && !thr) dangerNow = true;
      const fedS = Math.min(s_, budget / 10.0); e.hopper -= fedS * 10.0;
      if (fired) fired[ri] += fed * 3.0 + fedS * 10.0;
      if (!e.turrets) st.stats.ringFired = (st.stats.ringFired ?? 0) + fed * 3.0 + fedS * 10.0;   // RI-01 ledger: a stand-in edge's rounds leave the game here
      let unS = s_ - fedS;
      if (thr && (un > 1e-9 || unS > 1e-9) && thr.spawn(st, en.id, un, unS, true)) { un = 0; unS = 0; }   // past the stand-in: they walk to the substation
      if (un > 1e-9 || unS > 1e-9) {
        st.stats.unfedTotal += un;
        if (st.stats.firstUnfed < 0) st.stats.firstUnfed = t;
        if (hp.unfedSince < 0) hp.unfedSince = t;
        if (cfg.unfed === 'substation') {
          hp.unfed += un;
          if (unS > 1e-9) hp.shadeOff = Math.max(hp.shadeOff, t) + 30 * unS;
        }
      }
      if (en.cr > 1e-9 || en.sh > 1e-9) eng[w++] = en;
    }
    eng.length = w;
    if (thr) { const r = thr.second(st); if (r.danger) dangerNow = true; if (r.shot) shotNow = true; }
    if (dangerNow) { player.danger++; const h = Math.floor(t / 3600); player.dangerHour[h] = (player.dangerHour[h] ?? 0) + 1; if (shotNow) player.dangerShot++; }
    player.shots = {}; player.iframes = 0;   // rounds that found no crawler this second are gone; the dodge's cover is spent
    if (th && fired) th.drainEdges(st, fired);
    for (let r = 0; r < ring.length; r++) {
      const e = ring[r];
      if (e.hopper <= 1e-9) {
        // M3: the map pip turns red on this same transition (§4); the world view's turret hopper is the same number
        if (e.empty === 0) st.events.push({ type: 'hopper-empty', t, x: B[e.a].x, y: B[e.a].y, nx: B[e.b].x, ny: B[e.b].y });
        e.empty++;
      } else e.empty = 0;
    }
    if (cfg.unfed !== 'none') {
      for (let i = 0; i < B.length; i++) {
        const b = B[i];
        if (b.state !== HELD) continue;
        if (cfg.unfed === 'substation') {
          let allFed = true;
          for (let k = 0; k < st.deg; k++) {
            const ri = st.edgeAt[i * st.deg + k];
            if (ri >= 0 && ring[ri].hopper <= 1e-9) { allFed = false; break; }
          }
          if (allFed) {
            b.fedTimer++;
            if (b.fedTimer >= 60) { b.unfed = 0; b.unfedSince = -1; }
          } else b.fedTimer = 0;
          if (b.unfed >= cfg.unfedN) {
            if (b.subOn) st.events.push({ type: 'sub-off', t, x: b.x, y: b.y });
            b.subOn = false;
          } else if (!b.subOn && b.fedTimer >= 60) {
            b.subOn = true;
            st.events.push({ type: 'sub-on', t, x: b.x, y: b.y });
          }
        } else if (cfg.unfed === 'creep') {
          let anyEmpty = false;
          for (let k = 0; k < st.deg; k++) {
            const ri = st.edgeAt[i * st.deg + k];
            if (ri >= 0 && ring[ri].empty > 60) { anyEmpty = true; break; }
          }
          if (anyEmpty) {
            if (b.unfedSince < 0) b.unfedSince = t - 60;
            b.creep += 1 / 3.0;
            if (b.creep >= cfg.fallTiles) fall(st, i, 'starved');
          }
        }
      }
    }
  }

  // ---- power (§14, D-B3-4): one pool, no shedding; short of supply, every machine runs at supply ÷ demand ----
  if (cfg.power) {
    const pw = st.power, th = tiles(st);
    pw.asmActive = asmActive(st, t);
    // M3: with supply = 'generators' the grid is what the tile layer's Generators put out this second
    if (cfg.supply === 'generators') pw.supply = th ? th.supplyKw(st) : 0;
    const dem = demandKw(st);
    if (cfg.supply === 'track' && t % 600 === 0 && !inWindow(st)) pw.supply = Math.max(pw.supply, demandUnshed(st) + cfg.headroom);
    const eff = effectiveSupply(st);
    if (t % 60 === 0) { pw.demandKw.push(demandUnshed(st)); pw.supplyKw.push(eff); }
    const short = dem > eff + 1e-9;
    if (short) {
      st.stats.brownoutS++;
      if (st.stats.firstBrownout < 0) st.stats.firstBrownout = t;
      // one toast per shortfall: a grid flickering around its demand (a Generator running dry as another is fed)
      // does not re-announce itself within a minute of the last all-clear
      if (!pw.short) { pw.shortAt = t; if (t - pw.okAt >= 60 || st.stats.firstBrownout === t) st.events.push({ type: 'brownout', t, demandKw: dem, supplyKw: eff }); }
    } else if (pw.short) {
      if (t - pw.shortAt >= 20) st.events.push({ type: 'power-ok', t });
      pw.okAt = t; pw.shortAt = -1;
    }
    pw.short = short;
    pw.throttle = short ? Math.max(0, eff) / dem : 1;
    if (short) st.stats.throttleMin = Math.min(st.stats.throttleMin, pw.throttle);
    if (th) th.setLoad(st, eff, dem, Math.min(dem, eff), pw.throttle);
  }

  // ---- creep and fall (substation off for any reason) ----
  if (prod || cfg.power) {
    for (let i = 0; i < B.length; i++) {
      const b = B[i];
      if (b.state !== HELD) continue;
      const on = b.subOn && t >= b.shadeOff;
      if (!on) {
        b.creep += 1 / 3.0;
        if (b.creep >= cfg.fallTiles) fall(st, i, t < b.shadeOff ? 'shade' : 'unfed');
      } else if (b.creep > 0) {
        b.creep = Math.max(0, b.creep - 1 / 20.0);
      }
    }
  }

  if (st.dirty) {
    if (st.stats.firstInterior < 0 && interior(st) > 0) st.stats.firstInterior = t;
    st.dirty = false;
  }

  // ---- economy (proto only; off in the regression) ----
  // GAME-ASSUMPTION: rubble comes flat per Held block by district (civ stone, res copper, ind steel, outskirts
  // nothing) and stone has no sink; the doc has no yield numbers. Magazines cost the §12 recipe (ring block above).
  if (cfg.economy) {
    // with the M2 flow layer the HQ patch is dug by machines and hands (flow.ts), not drained here
    if (st.patch.steel > 0 && !st.flow) {
      const take = Math.min(cfg.eco.startPatch.perMin / 60, st.patch.steel);
      st.patch.steel -= take; st.stock.steel += take;
    }
    if ((st.patch.copper ?? 0) > 0 && !st.flow) {   // D6: the HQ lot's copper (see SimConfig.eco.startPatch)
      const take = Math.min((cfg.eco.startPatch.copperPerMin ?? 0) / 60, st.patch.copper);
      st.patch.copper -= take; st.stock.copper += take;
    }
    // §12: rubble is finite. The flat yield draws the block's pool down; at zero the block yields nothing.
    // RI-01 (D-P4-2, D-P4-5): on a city with the tile layer no block yields flat — every unit is dug from a tile by
    // an Excavator or the hands (flow.ts `mineUnit`, which draws the pool down a tile's share as each tile is dug
    // out). Before RI-01 only the start lot was exempt and a claim's copper or coal arrived in the chest abstractly.
    // Block-only runs (the fixtures, E1–E9) keep the flat yield.
    const per = cfg.eco.yieldPerMin / 60;
    for (let i = 0; i < B.length && !st.flow; i++) {
      const b = B[i];
      if (b.state !== HELD || b.pool <= 0) continue;
      const r = rubbleOf(b.name);
      if (!r) continue;
      const take = Math.min(per, b.pool);
      st.stock[r] += take; b.pool -= take;
      if (b.pool <= 1e-9) {
        b.pool = 0; st.stats.ranDry++;
        st.stats.dryLog.push({ t, x: b.x, y: b.y, district: b.name });
        st.events.push({ type: 'run-dry', t, x: b.x, y: b.y, district: b.name });
      }
    }
  }

  st.t = t + 1;
  const t1 = st.t;
  const slot = t1 % 600;   // the slot for tick t1-600 leaves the 10-minute window
  st.recentRoundsSum -= st.recentRounds[slot]; st.recentRounds[slot] = 0;
  st.recentShellsSum -= st.recentShells[slot]; st.recentShells[slot] = 0;
  if (t1 % 3600 === 0) {
    const row = hourRow(st);
    st.hourly.push(row);
    st.events.push({ type: 'hour', t: t1, row });
  }
  return st;
}

function hourRow(st: SimState): HourRow {
  const B = st.blocks;
  const mags = st.recentRoundsSum / 10 / 10;
  const shells = st.recentShellsSum / 10;
  let sum = 0, n = 0;
  for (let i = 0; i < B.length; i++) {
    if (B[i].state !== HELD) continue;
    const ns = st.nb[i];
    for (let k = 0; k < ns.length; k++) { const nb = B[ns[k]]; if (nb.state === DARK) { sum += nb.d; n++; } }
  }
  return {
    h: st.t / 3600, held: heldCount(st), front: frontage(st), interior: interior(st),
    mags, shells, meanAwakeRot: n ? sum / n : 0, lost: st.stats.lost,
    production: st.config.production ? productionMagPerMin(st, st.t) : 0,
  };
}

/** Real-time driver: run as many ticks as `realSeconds × speed` allows (capped), applying `commands` first. */
export function advance(st: SimState, realSeconds: number, commands: readonly Command[] = NO_COMMANDS, maxTicks = 256): number {
  if (commands.length) applyCommands(st, commands);
  st.acc += realSeconds * st.speed;
  let n = Math.floor(st.acc);
  if (n > maxTicks) { n = maxTicks; st.acc = 0; } else st.acc -= n;
  for (let k = 0; k < n; k++) step(st);
  return n;
}

export function takeEvents(st: SimState): SimEvent[] {
  const ev = st.events;
  st.events = [];
  return ev;
}
