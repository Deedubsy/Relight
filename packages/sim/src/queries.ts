/** Read-only queries for a renderer. Nothing here mutates state. */
import { SimState, Edge, DARK, CONTESTED, HELD, INERT, VOID, STATE_NAMES, District } from './types';
import { frontage, interior, heldCount, frontageIf, interiorIf, isCandidate, rotOf, asmCount, productionMagPerMin, idxOf, inBounds, isHostile, isInterior, freeSlot } from './sim';
import { hopsFrom } from './graph';
import { TURRET_HOPPER } from './recipes';

export type Pip = 'green' | 'amber' | 'red';

export interface FrontEdgeView {
  id: number; ringPos: number;
  from: { x: number; y: number }; to: { x: number; y: number };
  kit: boolean;               // D5: false = the edge's turrets have not been carried out yet (walk on)
  len: number;                // D6: the street segment's length in tiles
  hopper: number; level: number; pip: Pip;
  darkRot: number; darkDistrict: District; darkWell: boolean;
}

/** An edge's hopper capacity: its physical turrets' hoppers (M3, 50 rounds each) or the block-level stand-in. */
export function edgeCap(st: SimState, e: Edge): number { return e.turrets ? e.turrets * TURRET_HOPPER : st.config.hopper; }

/** GAME-ASSUMPTION: the doc's pip rule is for a block's hoppers as a set (green ≥ 50 % full, amber 10–50 %,
 *  red = at least one empty). The proto shows one pip per edge, so it is applied to that edge's hopper. */
export function pipOf(level: number): Pip { return level >= 0.5 ? 'green' : level >= 0.1 ? 'amber' : 'red'; }

export function frontList(st: SimState): FrontEdgeView[] {
  const out: FrontEdgeView[] = [];
  for (let r = 0; r < st.ring.length; r++) {
    const e: Edge = st.ring[r];
    const a = st.blocks[e.a], b = st.blocks[e.b];
    const level = e.hopper / edgeCap(st, e);
    out.push({ id: e.id, ringPos: r, from: { x: a.x, y: a.y }, to: { x: b.x, y: b.y }, kit: e.kit !== false, len: st.len[e.a][e.id % st.deg],
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
  closes: number;           // GAME-ASSUMPTION: "closes N" = Held blocks that go Interior (the claimed block included); the doc uses the phrase without defining it
  fAfter: number; iAfter: number;
  cost: { copper: number; steel: number } | null;
  affordable: boolean;
  wakeBloomCrawlers: number;  // approximate size of the wake bloom at the current rot
}

/** Tooltip data for the map's block preview: `rot 31 % · front +2 · closes 1`. RI-03 (plan §4.1): the map only previews
 *  — it charges nothing and grants nothing; `ok` here is the legacy map claim's answer (adjacency and the chest's
 *  stock), which the game's map no longer sends. Whether the physical Activate is available is `activationCheck`. */
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
  let hsum = 0, hcap = 0, empty = 0;
  for (const e of st.ring) { hsum += e.hopper; hcap += edgeCap(st, e); if (e.hopper <= 1e-9) empty++; }
  const win = Math.min(600, Math.max(1, t));
  return {
    assemblers: asmCount(st, t),
    productionMagPerMin: st.config.production ? productionMagPerMin(st, t) : 0,
    demandMagPerMin: st.recentRoundsSum / 10 / (win / 60),
    demandMagPerMin1: r60 / 10,
    shellsPerMin: st.recentShellsSum / (win / 60),
    stockMags: st.buffer / 10,
    hopperMean: hcap > 0 ? hsum / hcap : 1,
    emptyHoppers: empty,
  };
}

export interface ShapeMetrics {
  held: number; front: number; interior: number; contested: number;
  bbox: { x0: number; y0: number; x1: number; y1: number; w: number; h: number; aspect: number };
  perimeter: number;           // Held sides (graph edges) facing a non-Held, non-inert block
  perimeterOverArea: number;   // sides per Held block
  areaTiles: number;           // D6: buildable tiles of the Held blocks
  perimeterTiles: number;      // D6: tiles of street segment along those sides
  lost: number;
}

/** GAME-ASSUMPTION: shape metrics for the test plan. Perimeter counts Held sides that face a Dark, Contested or
 *  Void block (inert and the map edge are walls and do not count); area = Held blocks. D6 adds the tile forms:
 *  area = buildable tiles, perimeter = front-segment tiles. */
export function shapeMetrics(st: SimState): ShapeMetrics {
  const B = st.blocks;
  let x0 = 1e9, y0 = 1e9, x1 = -1, y1 = -1, held = 0, contested = 0, per = 0, areaT = 0, perT = 0;
  for (let i = 0; i < B.length; i++) {
    const b = B[i];
    if (b.state === CONTESTED) contested++;
    if (b.state !== HELD) continue;
    held++;
    if (b.x < x0) x0 = b.x; if (b.x > x1) x1 = b.x; if (b.y < y0) y0 = b.y; if (b.y > y1) y1 = b.y;
    areaT += b.area;
    const ns = st.nb[i];
    for (let k = 0; k < ns.length; k++) { const s = B[ns[k]].state; if (isHostile(s) || s === VOID) { per++; perT += st.len[i][k]; } }
  }
  const w = x1 - x0 + 1, h = y1 - y0 + 1;
  return {
    held, front: frontage(st), interior: interior(st), contested,
    bbox: { x0, y0, x1, y1, w, h, aspect: h ? w / h : 0 },
    perimeter: per, perimeterOverArea: held ? per / held : 0, areaTiles: areaT, perimeterTiles: perT,
    lost: st.stats.lost,
  };
}

export interface SlotInfo {
  used: number;      // machines standing on Held blocks (the HQ's Mk1 included)
  free: number;      // free slots on Interior blocks (D6: a big lot has several)
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
    if (b.machines > 0) { used += b.machines; if (!inner && i !== hq) atRisk += b.machines; }
    if (inner) free += Math.max(0, b.slots - b.machines);
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
  const slot = b.machines > 0 ? (hq ? 'Mk1' : inner ? 'assembler' : 'at risk') : inner ? 'free' : max > 0 || b.name === 'out' ? 'front' : 'none';
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
/** §8 scouting: a facility's silhouette shows when it is within SKYLINE_RANGE blocks of any Held block. */
export const SKYLINE_RANGE = 6;
function heldWithin(st: SimState, x: number, y: number, range: number): boolean {
  const i = idxOf(st, x, y);
  if (i < 0) return false;
  const d = hopsFrom(st, i);
  for (let j = 0; j < st.blocks.length; j++) if (st.blocks[j].state === HELD && d[j] >= 0 && d[j] <= range) return true;
  return false;
}
export interface FacilityView { name: string; x: number; y: number; dist: number; held: boolean; visible: boolean }
export function facilityList(st: SimState): FacilityView[] {
  return st.facilities
    .map(f => ({ ...f, dist: st.hops[idxOf(st, f.x, f.y)], held: st.blocks[idxOf(st, f.x, f.y)].state === HELD,
                 visible: heldWithin(st, f.x, f.y, SKYLINE_RANGE) }))
    .sort((a, b) => a.dist - b.dist);
}
/** §8 scouting: a block's contents (here, a survivor group) show once the block or any 4-neighbour is Held. */
export interface SurvivorView { name: string; tag: string; x: number; y: number; dist: number; held: boolean; revealed: boolean }
export function survivorList(st: SimState): SurvivorView[] {
  return st.survivors
    .map(f => ({ ...f, dist: st.hops[idxOf(st, f.x, f.y)], held: st.blocks[idxOf(st, f.x, f.y)].state === HELD,
                 revealed: heldWithin(st, f.x, f.y, 1) }))
    .sort((a, b) => a.dist - b.dist);
}

/** Nearest Held block to (x, y) (for the decorative pole line), or null. */
export function nearestHeld(st: SimState, x: number, y: number): { x: number; y: number } | null {
  const i = idxOf(st, x, y);
  if (i < 0) return null;
  const hd = hopsFrom(st, i);
  let best: { x: number; y: number } | null = null, bd = 1e9;
  for (let j = 0; j < st.blocks.length; j++) {
    const b = st.blocks[j];
    if (b.state !== HELD || hd[j] < 0) continue;
    if (hd[j] < bd) { bd = hd[j]; best = { x: b.x, y: b.y }; }
  }
  return best;
}

/** Rot density tier for the map's mottle (0 none .. 3 dense). */
export function rotTier(d: number): 0 | 1 | 2 | 3 { return d < 0.15 ? 0 : d < 0.3 ? 1 : d < 0.5 ? 2 : 3; }

export { INERT };

/** §18 map-view drawing: one character per block cell, the doc's legend. `H` held (front), `I` interior, `C` contested,
 *  `.` dark, `~` river / inert, `Q` HQ, `W` live well, `w` neutralised well (Held or dead), facilities `F` Foundry,
 *  `A` Arsenal, `U` Turbine hall, `R` Refinery, `P` Power station and survivors `E`/`N`/`G`/`K`/`M` — uppercase when
 *  their block is Held, lowercase while it is dark. Rows are y (0 north, river last), columns x. */
export function renderMap(st: SimState): string {
  const FAC: Record<string, string> = { Foundry: 'F', Arsenal: 'A', 'Turbine hall': 'U', Refinery: 'R', 'Power station': 'P' };
  const mark = new Map<number, string>();
  st.wells.forEach(([x, y], k) => { const b = st.blocks[idxOf(st, x, y)]; mark.set(x * st.h + y, b.state === HELD || st.wellDead[k] ? 'w' : 'W'); });
  for (const f of st.facilities) { const c = FAC[f.name] ?? '?'; mark.set(f.x * st.h + f.y, st.blocks[idxOf(st, f.x, f.y)].state === HELD ? c : c.toLowerCase()); }
  for (const s of st.survivors) mark.set(s.x * st.h + s.y, st.blocks[idxOf(st, s.x, s.y)].state === HELD ? s.tag : s.tag.toLowerCase());
  mark.set(st.start[0] * st.h + st.start[1], 'Q');
  const lines: string[] = [' col: ' + Array.from({ length: st.w }, (_, x) => x % 10).join(' ')];
  for (let y = 0; y < st.h; y++) {
    const row: string[] = [];
    for (let x = 0; x < st.w; x++) {
      const i = idxOf(st, x, y), b = st.blocks[i];
      if (b.state === INERT || b.state === VOID) { row.push('~'); continue; }
      const m = mark.get(x * st.h + y);
      if (m) { row.push(m); continue; }
      row.push(b.state === HELD ? (isInterior(st, i) ? 'I' : 'H') : b.state === CONTESTED ? 'C' : '.');
    }
    lines.push(String(y).padStart(3) + '   ' + row.join(' '));
  }
  return lines.join('\n');
}
