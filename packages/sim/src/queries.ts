/** Read-only queries for a renderer. Nothing here mutates state. */
import { SimState, Edge, DARK, CONTESTED, HELD, INERT, VOID, STATE_NAMES, District } from './types';
import { frontage, interior, heldCount, frontageIf, interiorIf, isCandidate, rotOf, asmCount, productionMagPerMin, idxOf, inBounds, topo, isHostile, isInterior, freeSlot } from './sim';

export type Pip = 'green' | 'amber' | 'red';

export interface FrontEdgeView {
  id: number; ringPos: number;
  from: { x: number; y: number }; to: { x: number; y: number };
  dir: number;                // 0 +x, 1 -x, 2 +y, 3 -y (from -> to)
  hopper: number; level: number; pip: Pip;
  darkRot: number; darkDistrict: District; darkWell: boolean;
}

/** PROTO-ASSUMPTION: the doc's pip rule is for a block's hoppers as a set (green ≥ 50 % full, amber 10–50 %,
 *  red = at least one empty). The proto shows one pip per edge, so it is applied to that edge's hopper. */
export function pipOf(level: number): Pip { return level >= 0.5 ? 'green' : level >= 0.1 ? 'amber' : 'red'; }

export function frontList(st: SimState): FrontEdgeView[] {
  const out: FrontEdgeView[] = [];
  const cap = st.config.hopper;
  for (let r = 0; r < st.ring.length; r++) {
    const e: Edge = st.ring[r];
    const a = st.blocks[e.a], b = st.blocks[e.b];
    const level = e.hopper / cap;
    out.push({ id: e.id, ringPos: r, from: { x: a.x, y: a.y }, to: { x: b.x, y: b.y }, dir: e.id % 4,
               hopper: e.hopper, level, pip: pipOf(level),
               darkRot: b.state === DARK ? b.d : 0, darkDistrict: b.name, darkWell: b.well });
  }
  return out;
}

export interface ClaimInfo {
  ok: boolean; reason: string | null;
  x: number; y: number; district: District; well: boolean;
  rot: number;              // current d (asleep blocks projected)
  frontDelta: number;       // "front +N"
  closes: number;           // PROTO-ASSUMPTION: "closes N" = Held blocks that go Interior (the claimed block included); the doc uses the phrase without defining it
  fAfter: number; iAfter: number;
  cost: { copper: number; steel: number } | null;
  affordable: boolean;
  wakeBloomCrawlers: number;  // approximate size of the wake bloom at the current rot
}

/** Tooltip data for the Claim tool: `Claim — rot 31 % · front +2 · closes 1`. */
export function claimInfo(st: SimState, x: number, y: number): ClaimInfo {
  const base = { x, y, district: 'out' as District, well: false, rot: 0, frontDelta: 0, closes: 0, fAfter: 0, iAfter: 0,
                 cost: st.config.economy ? { ...st.config.eco.claimCost } : null, affordable: true, wakeBloomCrawlers: 0 };
  if (!inBounds(st, x, y)) return { ...base, ok: false, reason: 'out of bounds' };
  const i = idxOf(st, x, y), b = st.blocks[i];
  base.district = b.name; base.well = b.well; base.rot = rotOf(st, i);
  const F = frontage(st), I = interior(st);
  if (b.state !== DARK) return { ...base, ok: false, reason: STATE_NAMES[b.state] };
  base.fAfter = frontageIf(st, i, F); base.iAfter = interiorIf(st, i, I);
  base.frontDelta = base.fAfter - F; base.closes = base.iAfter - I;
  base.wakeBloomCrawlers = Math.min(40, 2 * Math.round(st.config.bloomBase + 36 * base.rot));
  if (!isCandidate(st, i)) return { ...base, ok: false, reason: 'not adjacent to a Held block' };
  if (st.config.economy) {
    const c = st.config.eco.claimCost;
    base.affordable = st.stock.copper >= c.copper && st.stock.steel >= c.steel;
    if (!base.affordable) return { ...base, ok: false, reason: 'cannot afford 10 wire, 5 frames' };
  }
  return { ...base, ok: true, reason: null };
}

export interface AmmoStatus {
  assemblers: number;
  productionMagPerMin: number;
  demandMagPerMin: number;     // blooms in the last 10 minutes, as the sim's hourly log measures it
  demandMagPerMin1: number;    // same over the last 60 s (responsive HUD number)
  shellsPerMin: number;
  stockMags: number;           // buffer, in magazines
  hopperMean: number;          // mean hopper fill 0..1 across the ring
  emptyHoppers: number;
}

export function ammoStatus(st: SimState): AmmoStatus {
  const t = st.t;
  let r60 = 0;
  for (let k = 1; k <= 60; k++) { const tk = t - k; if (tk < 0) break; r60 += st.recentRounds[tk % 600]; }
  let hsum = 0, empty = 0;
  for (const e of st.ring) { hsum += e.hopper; if (e.hopper <= 1e-9) empty++; }
  const win = Math.min(600, Math.max(1, t));
  return {
    assemblers: asmCount(st, t),
    productionMagPerMin: st.config.production ? productionMagPerMin(st, t) : 0,
    demandMagPerMin: st.recentRoundsSum / 10 / (win / 60),
    demandMagPerMin1: r60 / 10,
    shellsPerMin: st.recentShellsSum / (win / 60),
    stockMags: st.buffer / 10,
    hopperMean: st.ring.length ? hsum / (st.ring.length * st.config.hopper) : 1,
    emptyHoppers: empty,
  };
}

export interface ShapeMetrics {
  held: number; front: number; interior: number; contested: number;
  bbox: { x0: number; y0: number; x1: number; y1: number; w: number; h: number; aspect: number };
  perimeter: number;           // Held sides facing an in-bounds non-Held, non-inert cell
  perimeterOverArea: number;
  lost: number;
}

/** PROTO-ASSUMPTION: shape metrics for the test plan. Perimeter counts Held sides that face a Dark, Contested or
 *  Void cell (inert and the map edge are walls and do not count); area = Held blocks. */
export function shapeMetrics(st: SimState): ShapeMetrics {
  const tp = topo(st.w, st.h), B = st.blocks;
  let x0 = 1e9, y0 = 1e9, x1 = -1, y1 = -1, held = 0, contested = 0, per = 0;
  for (let i = 0; i < B.length; i++) {
    const b = B[i];
    if (b.state === CONTESTED) contested++;
    if (b.state !== HELD) continue;
    held++;
    if (b.x < x0) x0 = b.x; if (b.x > x1) x1 = b.x; if (b.y < y0) y0 = b.y; if (b.y > y1) y1 = b.y;
    const ns = tp.nb[i];
    for (let k = 0; k < ns.length; k++) { const s = B[ns[k]].state; if (isHostile(s) || s === VOID) per++; }
  }
  const w = x1 - x0 + 1, h = y1 - y0 + 1;
  return {
    held, front: frontage(st), interior: interior(st), contested,
    bbox: { x0, y0, x1, y1, w, h, aspect: h ? w / h : 0 },
    perimeter: per, perimeterOverArea: held ? per / held : 0,
    lost: st.stats.lost,
  };
}

export interface SlotInfo {
  used: number;      // Held blocks with a machine (the HQ's Mk1 included)
  free: number;      // Interior blocks without a machine
  atRisk: number;    // machines on a block that is no longer Interior (a neighbour fell) — the HQ's Mk1 excluded
  next: { x: number; y: number } | null;   // where the next assembler would go
  dry: number;       // Held blocks whose rubble pool is empty
  dryFrac: number;   // mean pool fraction left over Held blocks with a pool (1 = untouched)
}

/** §5 machine slots and §12 finite rubble, as the HUD shows them. */
export function slotInfo(st: SimState): SlotInfo {
  const B = st.blocks, hq = idxOf(st, st.start[0], st.start[1]);
  let used = 0, free = 0, atRisk = 0, dry = 0, fracSum = 0, nPool = 0;
  for (let i = 0; i < B.length; i++) {
    const b = B[i];
    if (b.state !== HELD) continue;
    const inner = isInterior(st, i);
    if (b.machine) { used++; if (!inner && i !== hq) atRisk++; }
    else if (inner) free++;
    const max = poolMax(st, b.name);
    if (max > 0) { nPool++; fracSum += b.pool / max; if (b.pool <= 0) dry++; }
  }
  const n = freeSlot(st);
  return { used, free, atRisk, next: n >= 0 ? { x: B[n].x, y: B[n].y } : null, dry, dryFrac: nPool ? fracSum / nPool : 1 };
}

export function poolMax(st: SimState, name: District): number {
  const p = st.config.eco.pool;
  return name === 'civ' ? p.civ : name === 'res' ? p.res : name === 'ind' ? p.ind : 0;
}

export interface HeldInfo { held: true; x: number; y: number; district: District; poolLeft: number; poolFrac: number; slot: 'free' | 'assembler' | 'at risk' | 'Mk1' | 'front' | 'none' }

/** Tooltip for a Held block: rubble left and what its machine slot holds. */
export function heldInfo(st: SimState, x: number, y: number): HeldInfo | null {
  if (!inBounds(st, x, y)) return null;
  const i = idxOf(st, x, y), b = st.blocks[i];
  if (b.state !== HELD) return null;
  const max = poolMax(st, b.name), inner = isInterior(st, i), hq = x === st.start[0] && y === st.start[1];
  const slot = b.machine ? (hq ? 'Mk1' : inner ? 'assembler' : 'at risk') : inner ? 'free' : max > 0 || b.name === 'out' ? 'front' : 'none';
  return { held: true, x, y, district: b.name, poolLeft: b.pool, poolFrac: max > 0 ? b.pool / max : 0, slot };
}

export interface Hud {
  t: number; clock: string; speed: number;
  held: number; front: number; interior: number; lost: number;
  ammo: AmmoStatus;
  stock: { stone: number; copper: number; steel: number };
}

export function hud(st: SimState): Hud {
  return {
    t: st.t, clock: clockOf(st.t), speed: st.speed,
    held: heldCount(st), front: frontage(st), interior: interior(st), lost: st.stats.lost,
    ammo: ammoStatus(st), stock: st.stock,
  };
}

export function clockOf(t: number): string {
  const h = Math.floor(t / 3600), m = Math.floor((t % 3600) / 60), s = t % 60;
  return `${h}:${String(m).padStart(2, '0')}:${String(s).padStart(2, '0')}`;
}

/** Facilities sorted by distance from the start, with the Held flag. */
export function facilityList(st: SimState): { name: string; x: number; y: number; dist: number; held: boolean }[] {
  const [sx, sy] = st.start;
  return st.facilities
    .map(f => ({ ...f, dist: Math.abs(f.x - sx) + Math.abs(f.y - sy), held: st.blocks[idxOf(st, f.x, f.y)].state === HELD }))
    .sort((a, b) => a.dist - b.dist);
}

/** Nearest Held block to (x, y) (for the decorative pole line), or null. */
export function nearestHeld(st: SimState, x: number, y: number): { x: number; y: number } | null {
  let best: { x: number; y: number } | null = null, bd = 1e9;
  for (const b of st.blocks) {
    if (b.state !== HELD) continue;
    const d = Math.abs(b.x - x) + Math.abs(b.y - y);
    if (d < bd) { bd = d; best = { x: b.x, y: b.y }; }
  }
  return best;
}

/** Rot density tier for the map's mottle (0 none .. 3 dense). */
export function rotTier(d: number): 0 | 1 | 2 | 3 { return d < 0.15 ? 0 : d < 0.3 ? 1 : d < 0.5 ? 2 : 3; }

export { INERT };
