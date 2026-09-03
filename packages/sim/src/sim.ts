/** The front-rule sim: frontsim.py ported faithfully, power model included (config.power).
 *  Fixed 1 s tick. `step(state, commands)` is deterministic over its inputs and holds no module state;
 *  it mutates `state` in place (no per-tick allocation) and returns it. Snapshot with `cloneState`. */
import {
  Block, SimState, SimConfig, Command, SimEvent, MapSpec, HourRow, District, LostEntry,
  DARK, CONTESTED, HELD, INERT, VOID,
} from './types';
import { hash01, rngNext, seedRng, pyRound } from './prng';
import { districtBase } from './districts';

// ------------------------------------------------------------------ config

export const DEFAULT_CONFIG: SimConfig = {
  production: true,
  asmSchedule: [[600, 1], [1800, 2], [3000, 3], [10800, 4]],
  startAssemblers: 0,
  asmRate: 20.0, asmEarlyRate: null, startAsmRate: null,
  hopper: 100, bufferCap: 4000, startRounds: 200,   // C10: 20 magazines (§11; E1-start-min)
  unfed: 'substation', unfedN: 40, starveQuiet: false,
  scatter: true, scatterFrac: 0.09, validator: 'none',
  shadeThr: 0.3, hulkThr: 0.5, wakeCap: true, bloomBase: 4.0, jitter: 0.1, fallTiles: 30,
  hulkRoll: 'hash',
  bloomT: 120.0, bloomDrop: 0.9,
  relight: null,
  power: false, supply: 'track', headroom: 500, shortfall: null, draw: 'doc', shed: 'substations', interiorGrace: 0, asmTrack: 0,
  wellDeath: false, interleave: false,
  economy: false,
  eco: {
    yieldPerMin: 1,
    claimCost: { copper: 5, steel: 10 },
    assemblerCost: { copper: 20, steel: 40 },
    startStock: { stone: 0, copper: 40, steel: 80 },
    startPatch: { steel: 240, perMin: 2 },
    magazineCost: { steel: 2, copper: 1 },   // §12: "Shot magazine (2 steel + 1 Cu → 1 magazine of 10 rounds)"
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
    power: 'power', supply: 'supply', headroom: 'headroom', draw: 'draw', shed: 'shed', interior_grace: 'interiorGrace',
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

const DX = [1, -1, 0, 0], DY = [0, 0, 1, -1];
/** Neighbour order for ring insertion: Python sorts new edges by ((ax,ay),(nx,ny)); for one block the
 *  neighbours in ascending (x,y) are (x-1,y), (x,y-1), (x,y+1), (x+1,y) = dirs 1, 3, 2, 0. */
const SORTED_DIRS = [1, 3, 2, 0];

interface Topo { w: number; h: number; nb: number[][]; nbDir: number[][]; mark: Int32Array; stamp: number }
const topoCache = new Map<string, Topo>();
export function topo(w: number, h: number): Topo {
  const key = w + 'x' + h;
  let tp = topoCache.get(key);
  if (tp) return tp;
  const nb: number[][] = [], nbDir: number[][] = [];
  for (let x = 0; x < w; x++) for (let y = 0; y < h; y++) {
    const ns: number[] = [], ds: number[] = [];
    for (let k = 0; k < 4; k++) {
      const nx = x + DX[k], ny = y + DY[k];
      if (nx >= 0 && nx < w && ny >= 0 && ny < h) { ns.push(nx * h + ny); ds.push(k); }
    }
    nb.push(ns); nbDir.push(ds);
  }
  tp = { w, h, nb, nbDir, mark: new Int32Array(w * h * 4), stamp: 0 };
  topoCache.set(key, tp);
  return tp;
}

export const idxOf = (st: SimState, x: number, y: number) => x * st.h + y;
export const inBounds = (st: SimState, x: number, y: number) => x >= 0 && x < st.w && y >= 0 && y < st.h;
export const isHostile = (s: number) => s === DARK || s === CONTESTED;
export const isSolid = (s: number) => s === HELD || s === INERT;

// ------------------------------------------------------------------ state construction

export function createState(spec: MapSpec, config: SimConfig, seed: number): SimState {
  const { w, h } = spec;
  const blocks: Block[] = new Array(w * h);
  for (const c of spec.cells) {
    blocks[c.x * h + c.y] = {
      x: c.x, y: c.y, state: c.y === h - 1 ? INERT : DARK,
      d: c.d0, dmax: c.dmax, g: c.g, name: c.name, well: c.well,
      timer: -1, contestUntil: 0, upto: 0, awake: false,
      subOn: true, creep: 0, unfed: 0, fedTimer: 0, shadeOff: 0, unfedSince: -1, starveSince: -1,
      machine: false, pool: poolOf(config, c.name),
      exposed: true, exposedAt: 0, shed: false,
    };
  }
  if (config.scatter) {
    for (const [x, y] of spec.scatteredInert) blocks[x * h + y].state = config.validator === 'inert-dark' ? VOID : INERT;
  }
  const s0 = blocks[spec.start[0] * h + spec.start[1]];
  s0.state = HELD; s0.d = 0;
  s0.machine = config.startAssemblers > 0;   // §5: the HQ is the one non-interior block that hosts a machine (the Mk1)
  const st: SimState = {
    version: 1, seed, t: 0, rng: seedRng(seed), speed: 1, acc: 0,
    w, h, start: [spec.start[0], spec.start[1]], target: [spec.target[0], spec.target[1]],
    wells: spec.wells.map(p => [p[0], p[1]] as [number, number]),
    facilities: spec.facilities.map(f => ({ ...f })),
    survivors: (spec.survivors ?? []).map(f => ({ ...f })),
    config: JSON.parse(JSON.stringify(config)),
    blocks,
    ring: [], edgeAt: new Array(w * h * 4).fill(-1),
    engagements: [],
    buffer: config.production ? config.startRounds : 0,
    asmManual: config.startAssemblers,
    dirty: true,
    fallen: new Array(w * h).fill(false),
    recentRounds: new Array(600).fill(0), recentShells: new Array(600).fill(0),
    recentRoundsSum: 0, recentShellsSum: 0, totalRounds: 0, totalShells: 0,
    hourly: [],
    stats: { lost: 0, retakes: 0, claims: 0, firstInterior: -1, firstFall: -1, firstUnfed: -1, unfedTotal: 0,
             lostLog: [], ringReorders: 0, assemblersAdded: 0, claimsRejected: 0, magsMade: 0,
             machinesLost: 0, ranDry: 0, dryLog: [],
             firstBrownout: -1, shedEvents: 0, shedLog: [], lostInWindow: 0, lostAfterWindow: 0, wellsDead: 0 },
    stock: { ...config.eco.startStock },
    patch: { steel: config.eco.startPatch.steel },
    power: { supply: 300, overTimer: 0, shedStack: [], lastShed: -999, asmShed: 0, asmActive: 0, demandKw: [], supplyKw: [] },
    asmTrack: { n: 0, at: -1 },
    wellDead: spec.wells.map(() => false),
    wellEnclosedSince: spec.wells.map(() => -1),
    events: [],
  };
  stateChange(st, spec.start[0] * h + spec.start[1]);
  return st;
}

export function cloneState(st: SimState): SimState { return JSON.parse(JSON.stringify(st)); }

function poolOf(config: SimConfig, name: District): number {
  return name === 'civ' ? config.eco.pool.civ : name === 'res' ? config.eco.pool.res : name === 'ind' ? config.eco.pool.ind : 0;
}

// ------------------------------------------------------------------ counts

export function frontage(st: SimState): number {
  const tp = topo(st.w, st.h), B = st.blocks;
  let f = 0;
  for (let i = 0; i < B.length; i++) {
    if (B[i].state !== HELD) continue;
    const ns = tp.nb[i];
    for (let k = 0; k < ns.length; k++) if (isHostile(B[ns[k]].state)) f++;
  }
  return f;
}

export function interior(st: SimState): number {
  const tp = topo(st.w, st.h), B = st.blocks;
  let n = 0;
  for (let i = 0; i < B.length; i++) {
    if (B[i].state !== HELD) continue;
    const ns = tp.nb[i];
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
  const ns = topo(st.w, st.h).nb[i];
  for (let k = 0; k < ns.length; k++) if (!isSolid(B[ns[k]].state)) return false;
  return true;
}

/** The interior block that would take the next assembler: no machine yet, nearest the start, grid order on ties.
 *  GAME-ASSUMPTION: placement is automatic; the doc has the player place machines by hand. Returns -1 if none. */
export function freeSlot(st: SimState): number {
  const B = st.blocks, [sx, sy] = st.start;
  let best = -1, bd = 0;
  for (let i = 0; i < B.length; i++) {
    if (B[i].machine || !isInterior(st, i)) continue;
    const d = Math.abs(B[i].x - sx) + Math.abs(B[i].y - sy);
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
  const tp = topo(st.w, st.h), B = st.blocks, ns = tp.nb[i];
  let f = F;
  for (let k = 0; k < ns.length; k++) {
    const s = B[ns[k]].state;
    if (s === HELD) f--; else if (isHostile(s)) f++;
  }
  return f;
}

/** Interior count if block i were Held. */
export function interiorIf(st: SimState, i: number, I: number): number {
  const tp = topo(st.w, st.h), B = st.blocks, ns = tp.nb[i];
  let n = I;
  let selfIn = true;
  for (let k = 0; k < ns.length; k++) {
    const j = ns[k];
    if (!isSolid(B[j].state)) selfIn = false;
    if (B[j].state === HELD) {
      const ms = tp.nb[j];
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
  const tp = topo(st.w, st.h), B = st.blocks;
  out.length = 0;
  for (let i = 0; i < B.length; i++) {
    if (B[i].state !== DARK) continue;
    const ns = tp.nb[i];
    for (let k = 0; k < ns.length; k++) if (B[ns[k]].state === HELD) { out.push(i); break; }
  }
  return out.length;
}

export function isCandidate(st: SimState, i: number): boolean {
  const B = st.blocks;
  if (B[i].state !== DARK) return false;
  const ns = topo(st.w, st.h).nb[i];
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
  const ns = topo(st.w, st.h).nb[i], B = st.blocks;
  let aw = false;
  for (let k = 0; k < ns.length; k++) { const s = B[ns[k]].state; if (s === HELD || s === CONTESTED) { aw = true; break; } }
  if (!aw) b.timer = -1;
  b.awake = aw;
}

function stateChange(st: SimState, i: number): void {
  st.dirty = true;
  recomputeAwake(st, i);
  const ns = topo(st.w, st.h).nb[i];
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

function dirFromTo(tp: Topo, from: number, to: number): number {
  const ns = tp.nb[from];
  for (let k = 0; k < ns.length; k++) if (ns[k] === to) return tp.nbDir[from][k];
  return -1;
}

function recordBloom(st: SimState, i: number, cr: number, sh: number, hu: number, wake: boolean): void {
  const b = st.blocks[i];
  const rounds = cr * 3 + sh * 10, shells = hu * 10;
  st.totalRounds += rounds; st.totalShells += shells;
  const slot = st.t % 600;
  st.recentRounds[slot] += rounds; st.recentRoundsSum += rounds;
  st.recentShells[slot] += shells; st.recentShellsSum += shells;
  st.events.push({ type: 'bloom', t: st.t, x: b.x, y: b.y, cr, sh, hu, wake });
  if (!st.config.production) return;
  const tp = topo(st.w, st.h), ns = tp.nb[i], B = st.blocks;
  let ne = 0;
  for (let k = 0; k < ns.length; k++) if (B[ns[k]].state === HELD) ne++;
  if (ne === 0) return;
  for (let k = 0; k < ns.length; k++) {
    const n = ns[k];
    if (B[n].state !== HELD) continue;
    st.engagements.push({ id: n * 4 + dirFromTo(tp, n, i), cr: cr / ne, sh: sh / ne, rcr: cr / ne / 15.0, rsh: sh / ne / 15.0 });
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

/** Assemblers actually running: the count less any the brownout shedder turned off. */
export function asmActive(st: SimState, t: number): number {
  return Math.max(0, asmCount(st, t) - st.power.asmShed);
}

export function asmRate(st: SimState, t: number): number {
  const c = st.config;
  if (c.asmEarlyRate !== null && t < 3 * 3600) return c.asmEarlyRate;
  return c.asmRate;
}

/** Magazines per minute from every assembler. With `startAsmRate` set (proto: the starting "Mk1" assembler) the
 *  first `startAssemblers` hand-built assemblers run at that rate and every other one at `asmRate`. */
export function productionMagPerMin(st: SimState, t: number): number {
  const c = st.config;
  const n = asmActive(st, t);
  if (c.startAsmRate === null) return n * asmRate(st, t);
  const hq = st.blocks[idxOf(st, st.start[0], st.start[1])];
  const mk1 = Math.min(hq.machine ? Math.min(st.asmManual, c.startAssemblers) : 0, n);   // the Mk1 falls with the HQ; shed last
  return mk1 * c.startAsmRate + (n - mk1) * asmRate(st, t);
}

function rebuildEdgeAt(st: SimState): void {
  const ea = st.edgeAt;
  for (let i = 0; i < ea.length; i++) ea[i] = -1;
  for (let i = 0; i < st.ring.length; i++) ea[st.ring[i].id] = i;
}

/** Bring the ring in line with the map: drop edges that no longer face a hostile block (hopper back to the
 *  buffer), add new ones in sorted position order at the end of the ring. */
function syncEdges(st: SimState): void {
  const tp = topo(st.w, st.h), B = st.blocks, mark = tp.mark;
  const stamp = ++tp.stamp;
  for (let i = 0; i < B.length; i++) {
    if (B[i].state !== HELD) continue;
    const ns = tp.nb[i], ds = tp.nbDir[i];
    for (let k = 0; k < ns.length; k++) if (isHostile(B[ns[k]].state)) mark[i * 4 + ds[k]] = stamp;
  }
  const ring = st.ring;
  let w = 0;
  for (let r = 0; r < ring.length; r++) {
    const e = ring[r];
    if (mark[e.id] !== stamp) { st.buffer += e.hopper; st.edgeAt[e.id] = -1; continue; }
    ring[w++] = e;
  }
  ring.length = w;
  for (let i = 0; i < B.length; i++) {
    if (B[i].state !== HELD) continue;
    const x = B[i].x, y = B[i].y;
    for (let q = 0; q < 4; q++) {
      const dir = SORTED_DIRS[q];
      const id = i * 4 + dir;
      if (mark[id] !== stamp || st.edgeAt[id] !== -1) continue;
      const nx = x + DX[dir], ny = y + DY[dir];
      ring.push({ id, a: i, b: nx * st.h + ny, hopper: 0, empty: 0 });
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
  const half = c.draw === 'half';   // D1: 100/20 kW
  if (!b.exposed) return half ? 20 : 40;
  if (c.interiorGrace && t - b.exposedAt < c.interiorGrace) return half ? 20 : 40;
  return half ? 100 : 200;
}

/** Demand with every substation and assembler on (what the supply tracks). */
export function demandUnshed(st: SimState): number {
  const half = st.config.draw === 'half';
  let d = 0;
  for (const b of st.blocks) {
    if (b.state === HELD) d += subDraw(st, b, true);
    else if (b.state === CONTESTED) d += half ? 100 : 200;
  }
  return d + asmCount(st, st.t) * 220;
}

/** Demand as drawn now (shed substations and assemblers excluded). */
export function demandKw(st: SimState): number {
  const half = st.config.draw === 'half';
  let d = 0;
  for (const b of st.blocks) {
    if (b.state === HELD) d += subDraw(st, b);
    else if (b.state === CONTESTED) d += half ? 100 : 200;
  }
  return d + st.power.asmActive * 220;
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
  const ns = topo(st.w, st.h).nb[i], B = st.blocks;
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
  const tp = topo(st.w, st.h);
  for (let k = 0; k < st.wells.length; k++) {
    if (st.wellDead[k]) continue;
    const [wx, wy] = st.wells[k];
    const wi = idxOf(st, wx, wy);
    if (st.blocks[wi].state !== DARK) { st.wellEnclosedSince[k] = -1; continue; }
    const ns = tp.nb[wi];
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
    for (const b of st.blocks) {
      if (Math.abs(b.x - wx) + Math.abs(b.y - wy) > 3) continue;
      // GAME-ASSUMPTION: the block keeps the influence of any other live well in range; the doc has no rule for overlap.
      let infl = 0;
      for (let j = 0; j < st.wells.length; j++) {
        if (st.wellDead[j]) continue;
        const wd = Math.abs(b.x - st.wells[j][0]) + Math.abs(b.y - st.wells[j][1]);
        if (wd <= 3) infl = Math.max(infl, 1 - wd / 4);
      }
      const base = districtBase(b.x, b.y, st.start, st.h);
      if (b.state === DARK && b.awake) catchUp(st, b, st.t);
      b.dmax = Math.min(1, base.dmax + 0.3 * infl);
      b.g = base.g * (1 + 3 * infl);
      b.well = infl > 0;
    }
  }
}

function fall(st: SimState, i: number, reason: LostEntry['reason']): void {
  const b = st.blocks[i];
  // diagnostics for the fall event (frontsim.py fall_delays): first-unfed → fall delay and the starved edge's district
  const delay = b.unfedSince >= 0 ? st.t - b.unfedSince : -1;
  let starved = '-';
  for (let k = 0; k < 4; k++) {
    const ri = st.edgeAt[i * 4 + k];
    if (ri >= 0 && st.ring[ri].hopper <= 1e-9) { const n = st.blocks[st.ring[ri].b]; starved = n.well ? 'well' : n.name; break; }
  }
  b.state = DARK; b.d = 0.3; b.upto = st.t + 1; b.timer = -1; b.creep = 0; b.unfed = 0;
  b.subOn = true; b.shed = false; b.shadeOff = 0; b.fedTimer = 0;
  b.unfedSince = -1; b.starveSince = -1;
  const si = st.power.shedStack.indexOf(i);
  if (si >= 0) st.power.shedStack.splice(si, 1);
  if (st.config.shortfall) {
    const sf = st.config.shortfall;
    if (inWindow(st)) st.stats.lostInWindow++;
    else if (st.t >= sf.hour * 3600 + sf.minutes * 60) st.stats.lostAfterWindow++;
  }
  if (b.machine) {   // §5: a block that falls loses its machine
    b.machine = false; st.asmManual = Math.max(0, st.asmManual - 1); st.stats.machinesLost++;
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

/** Claim block (x, y). Bots call this with a valid candidate; the proto's click goes through the same checks. */
export function claim(st: SimState, x: number, y: number): boolean {
  const t = st.t;
  if (!inBounds(st, x, y)) return reject(st, x, y, 'out of bounds');
  const i = idxOf(st, x, y);
  if (!isCandidate(st, i)) return reject(st, x, y, st.blocks[i].state === DARK ? 'not adjacent to a Held block' : 'not Dark');
  if (st.config.economy) {
    const c = st.config.eco.claimCost;
    if (st.stock.copper < c.copper || st.stock.steel < c.steel) return reject(st, x, y, 'cannot afford 10 wire, 5 frames');
    st.stock.copper -= c.copper; st.stock.steel -= c.steel;
  }
  const b = st.blocks[i];
  const F = frontage(st), I = interior(st);
  // GAME-ASSUMPTION: the claim event's F/I "after" are projections at claim time (as if the block were Held now),
  // not the values when it actually turns Held; the brief's telemetry does not say which.
  const fAfter = frontageIf(st, i, F), iAfter = interiorIf(st, i, I);
  catchUp(st, b, t);
  const [cr, sh, hu] = wakeBloom(st, b.d, x, y);
  const dBefore = b.d;
  recordBloom(st, i, cr, sh, hu, true);
  b.state = CONTESTED; b.contestUntil = t + 20 + 60 * b.d;
  b.awake = false;
  const retake = st.fallen[i];
  if (retake) st.stats.retakes++;
  st.stats.claims++;
  stateChange(st, i);
  st.events.push({ type: 'claim', t, x, y, district: b.name, well: b.well, d: dBefore,
                   fBefore: F, fAfter, iBefore: I, iAfter, retake, cr, sh, hu });
  return true;
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
  const i = freeSlot(st);
  if (i < 0) { st.events.push({ type: 'assembler-rejected', t: st.t, reason: 'no free interior slot' }); return false; }
  if (st.config.economy) {
    const c = st.config.eco.assemblerCost;
    if (st.stock.copper < c.copper || st.stock.steel < c.steel) { st.events.push({ type: 'assembler-rejected', t: st.t, reason: 'cannot afford' }); return false; }
    st.stock.copper -= c.copper; st.stock.steel -= c.steel;
  }
  const b = st.blocks[i];
  b.machine = true;
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
    }
  }
}

// ------------------------------------------------------------------ one tick

const NO_COMMANDS: readonly Command[] = [];

export function step(st: SimState, commands: readonly Command[] = NO_COMMANDS): SimState {
  const cfg = st.config, B = st.blocks, tp = topo(st.w, st.h), t = st.t;
  const prod = cfg.production;
  if (commands.length) applyCommands(st, commands);

  // contested -> held
  for (let i = 0; i < B.length; i++) {
    const b = B[i];
    if (b.state === CONTESTED && t >= b.contestUntil) {
      b.state = HELD; b.d = 0; b.timer = -1; b.subOn = true; b.creep = 0;
      b.unfed = 0; b.shed = false; b.shadeOff = 0; b.unfedSince = -1; b.starveSince = -1;
      st.fallen[i] = false;
      stateChange(st, i);
      // GAME-ASSUMPTION: a facility is "reached" when its own block turns Held; the proto only toasts it.
      // GAME-ASSUMPTION: a survivor group joins ("we're in", §8) when its block turns Held; no unlock effect yet.
      st.events.push({ type: 'held', t, x: b.x, y: b.y, facility: facilityAt(st, b.x, b.y), survivor: survivorAt(st, b.x, b.y) });
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
        const ns = tp.nb[i];
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
    for (let r = 0; r < ring.length; r++) {
      const e = ring[r];
      if (cfg.starveQuiet && e.a !== startIdx && B[e.b].d < 0.3 && B[e.b].state === DARK) {
        if (B[e.a].starveSince < 0) B[e.a].starveSince = t;
        continue;
      }
      const need = cfg.hopper - e.hopper;
      const give = Math.min(need, avail);
      if (give > 0) { e.hopper += give; avail -= give; }
    }
    if (cfg.economy && made > 0) {
      const unmade = Math.min(made, Math.max(0, avail - cfg.bufferCap));   // rounds with nowhere to go: not made, not paid
      const mags = (made - unmade) / 10;
      st.stock.steel -= mags * cfg.eco.magazineCost.steel; st.stock.copper -= mags * cfg.eco.magazineCost.copper;
      st.stats.magsMade += mags;
    }
    st.buffer = Math.min(cfg.bufferCap, avail);
    // engagements: crawlers arrive over 15 s; fed ones die, unfed ones proceed
    const eng = st.engagements;
    let w = 0;
    for (let q = 0; q < eng.length; q++) {
      const en = eng[q];
      const ri = st.edgeAt[en.id];
      if (ri < 0) continue;
      const e = ring[ri], hp = B[e.a];
      const a = Math.min(en.cr, en.rcr); en.cr -= a;
      const fed = Math.min(a, e.hopper / 3.0); e.hopper -= fed * 3.0;
      const un = a - fed;
      const s_ = Math.min(en.sh, en.rsh); en.sh -= s_;
      const fedS = Math.min(s_, e.hopper / 10.0); e.hopper -= fedS * 10.0;
      const unS = s_ - fedS;
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
    for (let r = 0; r < ring.length; r++) { const e = ring[r]; e.empty = e.hopper <= 1e-9 ? e.empty + 1 : 0; }
    if (cfg.unfed !== 'none') {
      for (let i = 0; i < B.length; i++) {
        const b = B[i];
        if (b.state !== HELD) continue;
        if (cfg.unfed === 'substation') {
          let allFed = true;
          for (let k = 0; k < 4; k++) {
            const ri = st.edgeAt[i * 4 + k];
            if (ri >= 0 && ring[ri].hopper <= 1e-9) { allFed = false; break; }
          }
          if (allFed) {
            b.fedTimer++;
            if (b.fedTimer >= 60) { b.unfed = 0; b.unfedSince = -1; }
          } else b.fedTimer = 0;
          if (b.unfed >= cfg.unfedN) {
            if (b.subOn) st.events.push({ type: 'sub-off', t, x: b.x, y: b.y });
            b.subOn = false;
          } else if (!b.shed && !b.subOn && b.fedTimer >= 60) {
            b.subOn = true;
            st.events.push({ type: 'sub-on', t, x: b.x, y: b.y });
          }
        } else if (cfg.unfed === 'creep') {
          let anyEmpty = false;
          for (let k = 0; k < 4; k++) {
            const ri = st.edgeAt[i * 4 + k];
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

  // ---- power (frontsim.py power=True): demand vs supply, shedding after 20 s over, restore 20 s after the last shed ----
  if (cfg.power) {
    const pw = st.power;
    pw.asmActive = asmActive(st, t);
    const dem = demandKw(st);
    if (cfg.supply === 'track' && t % 600 === 0 && !inWindow(st)) pw.supply = Math.max(pw.supply, demandUnshed(st) + cfg.headroom);
    const eff = effectiveSupply(st);
    if (t % 60 === 0) { pw.demandKw.push(demandUnshed(st)); pw.supplyKw.push(eff); }
    if (dem > eff) {
      pw.overTimer++;
      if (st.stats.firstBrownout < 0) { st.stats.firstBrownout = t; st.events.push({ type: 'brownout', t, demandKw: dem, supplyKw: eff }); }
      if (pw.overTimer >= 20) {
        pw.overTimer = 0; pw.lastShed = t; st.stats.shedEvents++; st.stats.shedLog.push(t);
        if (cfg.shed === 'machines-first' && pw.asmShed < asmCount(st, t)) {
          pw.asmShed++; pw.shedStack.push(-1);
          st.events.push({ type: 'shed', t, x: -1, y: -1 });
        } else {
          // the most exposed substation, then the one farthest from the start (Python max(): first of equals)
          let v = -1, vh = -1, vd = -1;
          for (let i = 0; i < B.length; i++) {
            const b = B[i];
            if (b.state !== HELD || !b.subOn || b.shed) continue;
            const ns = tp.nb[i];
            let hc = 0;
            for (let k = 0; k < ns.length; k++) if (isHostile(B[ns[k]].state)) hc++;
            const dd = Math.abs(b.x - st.start[0]) + Math.abs(b.y - st.start[1]);
            if (v < 0 || hc > vh || (hc === vh && dd > vd)) { v = i; vh = hc; vd = dd; }
          }
          if (v >= 0) {
            const b = B[v];
            b.subOn = false; b.shed = true; pw.shedStack.push(v);
            st.events.push({ type: 'shed', t, x: b.x, y: b.y });
          }
        }
      }
    } else {
      pw.overTimer = 0;
      if (pw.shedStack.length && t - pw.lastShed >= 20) {
        const u = pw.shedStack[pw.shedStack.length - 1];
        if (u === -1) {
          if (dem + 220 <= eff) { pw.shedStack.pop(); pw.asmShed--; pw.lastShed = t; st.events.push({ type: 'restore', t, x: -1, y: -1 }); }
        } else {
          const b = B[u];
          if (b.state !== HELD) pw.shedStack.pop();
          else {
            b.subOn = true; b.shed = false;
            if (demandKw(st) <= eff) { pw.shedStack.pop(); pw.lastShed = t; st.events.push({ type: 'restore', t, x: b.x, y: b.y }); }
            else { b.subOn = false; b.shed = true; }
          }
        }
      }
    }
    pw.asmActive = asmActive(st, t);
  }

  // ---- creep and fall (substation off for any reason) ----
  if (prod || cfg.power) {
    for (let i = 0; i < B.length; i++) {
      const b = B[i];
      if (b.state !== HELD) continue;
      const on = b.subOn && t >= b.shadeOff;
      if (!on) {
        b.creep += 1 / 3.0;
        if (b.creep >= cfg.fallTiles) fall(st, i, b.shed ? 'brownout' : t < b.shadeOff ? 'shade' : 'unfed');
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
    // §12: rubble is finite. The flat yield draws the block's pool down; at zero the block yields nothing.
    const per = cfg.eco.yieldPerMin / 60;
    const startIdx = idxOf(st, st.start[0], st.start[1]);
    for (let i = 0; i < B.length; i++) {
      const b = B[i];
      if (b.state !== HELD || b.pool <= 0) continue;
      if (st.flow && i === startIdx) continue;   // M2: the start lot's rubble is dug by its machines, not yielded flat
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
  const tp = topo(st.w, st.h), B = st.blocks;
  const mags = st.recentRoundsSum / 10 / 10;
  const shells = st.recentShellsSum / 10;
  let sum = 0, n = 0;
  for (let i = 0; i < B.length; i++) {
    if (B[i].state !== HELD) continue;
    const ns = tp.nb[i];
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
