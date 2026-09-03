/** Tile-level flow (constitution Phase 4 M2): Excavator, belts, inserters, the Depot, the Mk1 Shot assembler,
 *  hand-mining and hand-crafting, ticking at a fixed 20 ticks/s. The block map stays the judge of territory: a
 *  machine runs only while its block is Held, digging draws the same pools the block economy draws, magazines
 *  reach the same line buffer the ring hoppers fill from, and one block tick (`step`) closes every 20 tile ticks.
 *
 *  Nothing here exists until `ensureFlow` is called (the game does; the harness and the fixtures do not), so a
 *  state without a flow layer behaves exactly as before M2.
 *
 *  Rates (all measured by test/flow.test.ts, run name M2-rates): Excavator 0.5 items/s onto the tile it faces,
 *  belt 7.5 items/s (4 items a tile at 1.875 tiles/s), inserter 1 item/s, Shot assembler 3 s a magazine (20/min). */
import { SimState, HELD, Command } from './types';
import { idxOf, step, applyCommands } from './sim';
import { RECIPES, START_COAL } from './recipes';
import {
  CELL_TILES, MARGIN_TILES, LOT_TILES, P_STEEL, P_COPPER, HQ_PATCHES, DEPOT_LOT, DEPOT_TILES,
  lotLayout, isStart, hqPatchAt, goneCount, tileCell, isMargin,
} from './tiles';
import { poolMax } from './queries';

export const TILE_TPS = 20;                    // constitution: fixed 20 ticks/s at tile level
export const TILE_DT = 1 / TILE_TPS;
/** float slack on second-counting timers (60 × 0.05 does not sum to 3 exactly) */
const EPS = 1e-9;
export type Dir = 0 | 1 | 2 | 3;               // N E S W
export const DX = [0, 1, 0, -1], DY = [-1, 0, 1, 0];
export const DIR_NAMES = ['north', 'east', 'south', 'west'];
export type Kind = 'excavator' | 'belt' | 'inserter' | 'assembler' | 'depot';
export const KINDS: readonly Kind[] = ['excavator', 'belt', 'inserter', 'assembler', 'depot'];
export type Item = 'steel' | 'copper' | 'stone' | 'coal' | 'magazine';

/** Constitution M2: belts carry 7.5 items/s (§13/§14 said 8; D-P4-6). GAME-ASSUMPTION: four items a tile, so the
 *  belt moves 1.875 tiles/s; belts are one lane, a side feed joins at the tile's start like a corner. */
export const BELT_PER_S = 7.5, BELT_SPACING = 0.25, BELT_SPEED = BELT_PER_S * BELT_SPACING;
/** §13: Excavator 3×3, mines the 5×5 under and around it at 0.5/s. */
export const EXCAVATOR_PER_S = 0.5;
/** §13: inserter 1 item/s. GAME-ASSUMPTION: half a second each way; it waits with the item if the target is full. */
export const INSERTER_PER_S = 1, INSERTER_SWING = 0.5 / INSERTER_PER_S;
export const SHOT = RECIPES.find(r => r.name === 'Shot magazine')!;
/** GAME-ASSUMPTION: an assembler holds four crafts' worth of each input and five finished magazines, then stops. */
export const ASM_INPUT_MULT = 4, ASM_OUTPUT_CAP = 5;
/** GAME-ASSUMPTION: hand-mining takes one unit a second straight into the Depot; hand-crafting a magazine takes the
 *  recipe's 3 s and its 2 steel + 1 Cu from the stock (§11 hand-feeds the first turrets). */
export const HAND_MINE_PER_S = 1;
export const MACHINE_SIZE: Record<Kind, number> = { excavator: 3, belt: 1, inserter: 1, assembler: 3, depot: DEPOT_TILES };
/** GAME-ASSUMPTION: machine costs in rubble (§13 gives none). The assembler costs what the block-level one does
 *  (§12: 20 Cu + 40 steel); the Excavator 10 steel; a belt tile 1 steel; an inserter 1 steel + 1 Cu. Removal refunds. */
export const MACHINE_COST: Record<Kind, { steel: number; copper: number }> = {
  excavator: { steel: 10, copper: 0 }, belt: { steel: 1, copper: 0 }, inserter: { steel: 1, copper: 1 },
  assembler: { steel: 40, copper: 20 }, depot: { steel: 0, copper: 0 },
};
/** §13 power draw (kW), carried on the machine for M3; nothing draws power in M2 (GAME-ASSUMPTION: unpowered). */
export const MACHINE_KW: Record<Kind, number> = { excavator: 60, belt: 0, inserter: 10, assembler: 100, depot: 0 };

export interface BeltItem { k: Item; p: number }
export interface Machine {
  id: number; kind: Kind; x: number; y: number; dir: Dir; size: number;
  /** Belt: items by position along the tile, 0 = entry, 1 = exit; sorted ascending, the last one leads. */
  items: BeltItem[];
  /** Inserter: the item in hand. Excavator: the unit waiting at the output. */
  hold: Item | null;
  /** Inserter: swing time left. Excavator: mining progress (s). Assembler: craft progress (s). */
  timer: number;
  /** Inserter: 0 empty at the source, 1 carrying, 2 swinging back. */
  phase: 0 | 1 | 2;
  /** Assembler: input items on hand and finished magazines; `busy` while a craft's inputs are taken. */
  inv: Record<string, number>; out: number; busy: boolean;
}
export interface FlowState {
  version: 1;
  /** Tile ticks so far; a block tick closes every TILE_TPS of them. */
  tick: number;
  next: number;
  /** Bumped by every placement, removal or rotation; the belt order and the id index rebuild on it. */
  rev: number;
  tw: number;
  machines: Machine[];
  /** City tile (ty * tw + tx) → machine id. */
  occ: Record<number, number>;
  /** Block index → lot tiles dug out by machines or hands. */
  dug: Record<number, number[]>;
  /** "block:lot" → units left in a partly dug tile. */
  units: Record<string, number>;
  /** Items with no home in the block sim's stock. §11: the start's 40 coal (the Generator is M3). */
  store: { coal: number };
  hand: { mine: [number, number] | null; prog: number; crafts: number; crafting: boolean; craftProg: number };
  stats: { magsMade: number; magsDelivered: number; mined: number; handMined: number; handCrafted: number; delivered: Record<Item, number> };
}

/** The flow layer, created on first use: the Depot goes on the start lot, the block-level Mk1 stand-in retires
 *  (§12 C9: Phase 4 places the real machine), and from here the HQ patch is dug by machines and hands, not drained. */
export function ensureFlow(st: SimState): FlowState {
  if (st.flow) return st.flow;
  const f: FlowState = {
    version: 1, tick: 0, next: 1, rev: 0, tw: st.w * CELL_TILES, machines: [], occ: {}, dug: {}, units: {},
    store: { coal: START_COAL },
    hand: { mine: null, prog: 0, crafts: 0, crafting: false, craftProg: 0 },
    stats: { magsMade: 0, magsDelivered: 0, mined: 0, handMined: 0, handCrafted: 0, delivered: { steel: 0, copper: 0, stone: 0, coal: 0, magazine: 0 } },
  };
  st.flow = f;
  st.acc = 0;
  const [sx, sy] = st.start;
  const hq = st.blocks[idxOf(st, sx, sy)];
  if (hq.machine) { hq.machine = false; st.asmManual = Math.max(0, st.asmManual - st.config.startAssemblers); }
  addMachine(st, 'depot', sx * CELL_TILES + MARGIN_TILES + DEPOT_LOT, sy * CELL_TILES + MARGIN_TILES + DEPOT_LOT, 0);
  return f;
}

// ------------------------------------------------------------------ lookups

const idCache = new WeakMap<FlowState, { rev: number; n: number; map: Map<number, Machine> }>();
function byId(f: FlowState): Map<number, Machine> {
  const c = idCache.get(f);
  if (c && c.rev === f.rev && c.n === f.machines.length) return c.map;
  const map = new Map<number, Machine>();
  for (const m of f.machines) map.set(m.id, m);
  idCache.set(f, { rev: f.rev, n: f.machines.length, map });
  return map;
}
export function machineAt(st: SimState, tx: number, ty: number): Machine | undefined {
  const f = st.flow;
  if (!f || tx < 0 || ty < 0 || tx >= f.tw || ty >= st.h * CELL_TILES) return undefined;
  const id = f.occ[ty * f.tw + tx];
  return id === undefined ? undefined : byId(f).get(id);
}
export function machineById(st: SimState, id: number): Machine | undefined { return st.flow ? byId(st.flow).get(id) : undefined; }

/** The tile a machine hands its output to: the middle of the side it faces, just outside its footprint. */
export function outputTile(m: Machine): [number, number] {
  const c = (m.size - 1) / 2;
  const cx = m.x + c, cy = m.y + c, r = (m.size + 1) / 2;
  return [Math.round(cx + DX[m.dir] * r), Math.round(cy + DY[m.dir] * r)];
}
export function inputTile(m: Machine): [number, number] {
  const c = (m.size - 1) / 2;
  const cx = m.x + c, cy = m.y + c, r = (m.size + 1) / 2;
  return [Math.round(cx - DX[m.dir] * r), Math.round(cy - DY[m.dir] * r)];
}
function running(st: SimState, m: Machine): boolean {
  const cx = Math.floor(m.x / CELL_TILES), cy = Math.floor(m.y / CELL_TILES);
  return cy < st.h - 1 && st.blocks[idxOf(st, cx, cy)].state === HELD;
}

// ------------------------------------------------------------------ rubble under a tile

export interface TileRubble { type: Item; units: number; bi: number; li: number; patch: number }

/** What a tile holds for digging: an HQ patch tile (steel, copper, coal) or a standing district rubble tile, with
 *  the units left in it. Null for ground, street, a dug tile, or a tile the pool says is already gone. */
export function rubbleAt(st: SimState, tx: number, ty: number): TileRubble | null {
  const f = st.flow;
  if (!f) return null;
  const { x, y, lx, ly } = tileCell(tx, ty);
  if (x < 0 || y < 0 || x >= st.w || y >= st.h - 1 || isMargin(lx, ly)) return null;
  const b = st.blocks[idxOf(st, x, y)];
  if (b.state !== HELD) return null;
  const bi = idxOf(st, x, y), llx = lx - MARGIN_TILES, lly = ly - MARGIN_TILES, li = lly * LOT_TILES + llx;
  const dug = f.dug[bi];
  if (dug && dug.includes(li)) return null;
  const key = `${bi}:${li}`;
  if (isStart(st, b)) {
    const p = hqPatchAt(llx, lly);
    if (p !== 0) {
      const spec = HQ_PATCHES.find(q => q.type === p)!;
      const units = f.units[key] ?? (p === P_STEEL ? Math.min(spec.units, st.patch.steel) : spec.units);
      if (units <= 0) return null;
      return { type: p === P_STEEL ? 'steel' : p === P_COPPER ? 'copper' : 'coal', units, bi, li, patch: p };
    }
  }
  const lay = lotLayout(st.seed, b, isStart(st, b));
  if (!lay.rubble) return null;
  const r = lay.rank[li];
  if (r < 0 || r < goneCount(st, b, lay)) return null;
  const pm = poolMax(st, b.name);
  const units = f.units[key] ?? (pm > 0 ? pm / lay.tiles : 1);
  if (units <= 0) return null;
  return { type: lay.rubble, units, bi, li, patch: 0 };
}

/** Take one unit out of a tile: the block's pool (or the HQ steel patch) drops with it; an emptied tile is dug. */
function mineUnit(st: SimState, r: TileRubble): Item {
  const f = st.flow!;
  const key = `${r.bi}:${r.li}`;
  const left = r.units - 1;
  const b = st.blocks[r.bi];
  if (r.patch === P_STEEL) st.patch.steel = Math.max(0, st.patch.steel - 1);
  else if (r.patch === 0) b.pool = Math.max(0, b.pool - 1);
  if (left <= 1e-9) {
    delete f.units[key];
    (f.dug[r.bi] ??= []).push(r.li);
  } else f.units[key] = left;
  f.stats.mined++;
  return r.type;
}

// ------------------------------------------------------------------ the Depot: the global stock

/** Anything that reaches the Depot joins the global stock (§14): rubble into `st.stock`, coal into the flow store,
 *  a magazine as ten rounds into the line's buffer the ring hoppers draw from. A full buffer refuses magazines. */
export function deliver(st: SimState, k: Item): boolean {
  const f = st.flow!;
  if (k === 'magazine') {
    if (st.buffer + SHOT.count > st.config.bufferCap + 1e-9) return false;
    st.buffer += SHOT.count;
    f.stats.magsDelivered++;
  } else if (k === 'coal') f.store.coal += 1;
  else st.stock[k] += 1;
  f.stats.delivered[k]++;
  return true;
}

// ------------------------------------------------------------------ giving an item to a machine

function beltRoom(m: Machine, p: number): boolean {
  for (const it of m.items) if (Math.abs(it.p - p) < BELT_SPACING - 1e-9) return false;
  return true;
}
function beltInsert(m: Machine, k: Item, p: number): void {
  const items = m.items;
  // GAME-ASSUMPTION: an item joining a moving chain closes up to exactly one spacing behind the tail, at most one
  // tick's travel ahead of where it was put; a rigid chain is what makes a saturated belt carry 7.5/s at 20 ticks/s.
  if (items.length) p = Math.min(items[0].p - BELT_SPACING, p + BELT_SPEED * TILE_DT);
  let i = items.length;
  while (i > 0 && items[i - 1].p > p) i--;
  items.splice(i, 0, { k, p });
}
/** Whether `m` would take item `k` now, entering a belt at `p`. */
export function accepts(st: SimState, m: Machine, k: Item, p = 0): boolean {
  switch (m.kind) {
    case 'belt': return beltRoom(m, p);
    case 'depot': return k !== 'magazine' || st.buffer + SHOT.count <= st.config.bufferCap + 1e-9;
    case 'assembler': { const need = SHOT.inputs[k]; return need !== undefined && (m.inv[k] ?? 0) < need * ASM_INPUT_MULT; }
    default: return false;
  }
}
/** Could this machine ever take this item (type only)? An inserter picks up by this and waits by `accepts`. */
export function wants(m: Machine, k: Item): boolean {
  switch (m.kind) {
    case 'belt': case 'depot': return true;
    case 'assembler': return SHOT.inputs[k] !== undefined;
    default: return false;
  }
}
export function giveItem(st: SimState, m: Machine, k: Item, p = 0): boolean {
  if (!accepts(st, m, k, p)) return false;
  if (m.kind === 'belt') beltInsert(m, k, p);
  else if (m.kind === 'depot') return deliver(st, k);
  else m.inv[k] = (m.inv[k] ?? 0) + 1;
  return true;
}

// ------------------------------------------------------------------ belts

function nextOf(st: SimState, m: Machine): Machine | undefined {
  const n = machineAt(st, m.x + DX[m.dir], m.y + DY[m.dir]);
  if (!n) return undefined;
  if (n.kind === 'belt' && (n.dir + 2) % 4 === m.dir) return undefined;   // head-on belts do not feed each other
  return n;
}
/** Where a belt's items come from, for drawing corners: the belt behind it if that one points here, else a side
 *  belt pointing here, else straight. Returns the direction the items travel when they enter. */
export function entryDir(st: SimState, m: Machine): Dir {
  const back = machineAt(st, m.x - DX[m.dir], m.y - DY[m.dir]);
  if (back?.kind === 'belt' && back.dir === m.dir) return m.dir;
  for (const d of [(m.dir + 1) % 4, (m.dir + 3) % 4] as Dir[]) {
    const side = machineAt(st, m.x - DX[d], m.y - DY[d]);
    if (side?.kind === 'belt' && side.dir === d) return d;
  }
  return m.dir;
}

const orderCache = new WeakMap<FlowState, { rev: number; n: number; belts: Machine[] }>();
/** Belts downstream first, so a gap opens ahead before the belt behind moves into it (full throughput per tick). */
function beltOrder(st: SimState, f: FlowState): Machine[] {
  const c = orderCache.get(f);
  if (c && c.rev === f.rev && c.n === f.machines.length) return c.belts;
  const belts: Machine[] = [];
  const mark = new Map<number, number>();   // 1 visiting, 2 done
  const visit = (m: Machine) => {
    const s = mark.get(m.id);
    if (s) return;
    mark.set(m.id, 1);
    const n = nextOf(st, m);
    if (n && n.kind === 'belt') visit(n);
    mark.set(m.id, 2);
    belts.push(m);
  };
  for (const m of f.machines) if (m.kind === 'belt') visit(m);
  orderCache.set(f, { rev: f.rev, n: f.machines.length, belts });
  return belts;
}

function tickBelt(st: SimState, m: Machine, dt: number): void {
  const items = m.items;
  if (!items.length) return;
  const adv = BELT_SPEED * dt;
  for (let i = items.length - 1; i >= 0; i--) {
    const it = items[i];
    let np = it.p + adv;
    if (i === items.length - 1) {
      if (np >= 1) {
        const n = nextOf(st, m);
        if (n && giveItem(st, n, it.k, np - 1)) { items.pop(); continue; }
        np = 1 - BELT_SPACING / 2;
      }
    } else np = Math.min(np, items[i + 1].p - BELT_SPACING);
    it.p = Math.max(it.p, np);
  }
}

// ------------------------------------------------------------------ inserters, excavators, assemblers

function tickInserter(st: SimState, m: Machine, dt: number): void {
  if (m.phase !== 0) {
    m.timer -= dt;
    if (m.timer > EPS) return;
    if (m.phase === 1) {
      const [dx, dy] = outputTile(m);
      const dst = machineAt(st, dx, dy);
      if (dst && m.hold && giveItem(st, dst, m.hold, 0.5)) { m.hold = null; m.phase = 2; m.timer += INSERTER_SWING; }
      else m.timer = 0;
      return;
    }
    m.phase = 0;   // the return swing is over: pick up again in this same tick, the leftover carries
  }
  {
    const [sx, sy] = inputTile(m), [dx, dy] = outputTile(m);
    const src = machineAt(st, sx, sy), dst = machineAt(st, dx, dy);
    if (!src || !dst) return;
    let k: Item | null = null;
    if (src.kind === 'belt') {
      // GAME-ASSUMPTION: an inserter takes the front-most item its target could ever use and, if the target is full
      // right now, swings over and waits holding it (the Factorio behaviour); it never picks an item the target has no use for.
      for (let i = src.items.length - 1; i >= 0; i--) if (wants(dst, src.items[i].k)) { k = src.items[i].k; src.items.splice(i, 1); break; }
    } else if (src.kind === 'assembler' && src.out > 0 && wants(dst, 'magazine')) { src.out--; k = 'magazine'; }
    if (!k) return;
    m.hold = k; m.phase = 1; m.timer = INSERTER_SWING + Math.min(0, m.timer);
  }
}

function findRubble(st: SimState, m: Machine): TileRubble | null {
  for (let ty = m.y - 1; ty <= m.y + m.size; ty++) for (let tx = m.x - 1; tx <= m.x + m.size; tx++) {
    const r = rubbleAt(st, tx, ty);
    if (r) return r;
  }
  return null;
}
function tickExcavator(st: SimState, m: Machine, dt: number): void {
  if (m.hold) {
    const [ox, oy] = outputTile(m);
    const t = machineAt(st, ox, oy);
    if (t && giveItem(st, t, m.hold, 0)) m.hold = null;
    else return;   // output blocked: the drill stops with one unit waiting
  }
  const cycle = 1 / EXCAVATOR_PER_S;
  m.timer += dt;
  if (m.timer < cycle - EPS) return;
  const r = findRubble(st, m);
  if (!r) { m.timer = cycle; return; }   // nothing left in reach: idle, ready
  m.timer -= cycle;
  m.hold = mineUnit(st, r);
}

function asmCanStart(m: Machine): boolean {
  if (m.out >= ASM_OUTPUT_CAP) return false;
  for (const k in SHOT.inputs) if ((m.inv[k] ?? 0) < SHOT.inputs[k]) return false;
  return true;
}
function asmStart(m: Machine): void {
  for (const k in SHOT.inputs) m.inv[k] -= SHOT.inputs[k];
  m.busy = true;
}
function tickAssembler(st: SimState, m: Machine, dt: number): void {
  if (!m.busy) {
    if (!asmCanStart(m)) return;
    asmStart(m); m.timer = 0;
  }
  m.timer += dt;
  if (m.timer < SHOT.seconds - EPS) return;
  m.out++; m.busy = false;
  st.flow!.stats.magsMade++; st.stats.magsMade++;
  const rem = m.timer - SHOT.seconds;
  if (asmCanStart(m)) { asmStart(m); m.timer = rem; } else m.timer = 0;
}

function tickHand(st: SimState, f: FlowState, dt: number): void {
  const h = f.hand;
  if (h.mine) {
    const r = rubbleAt(st, h.mine[0], h.mine[1]);
    if (!r) { h.mine = null; h.prog = 0; }
    else {
      h.prog += dt * HAND_MINE_PER_S;
      if (h.prog >= 1 - EPS) { h.prog -= 1; deliver(st, mineUnit(st, r)); f.stats.handMined++; }
    }
  }
  if (h.crafts > 0) {
    if (!h.crafting) {
      if (st.stock.steel >= SHOT.inputs.steel && st.stock.copper >= SHOT.inputs.copper) {
        st.stock.steel -= SHOT.inputs.steel; st.stock.copper -= SHOT.inputs.copper; h.crafting = true; h.craftProg = 0;
      } else return;
    }
    h.craftProg += dt;
    if (h.craftProg >= SHOT.seconds - EPS && deliver(st, 'magazine')) {
      h.crafting = false; h.crafts--; h.craftProg = 0; f.stats.handCrafted++; st.stats.magsMade++;
    }
  }
}

// ------------------------------------------------------------------ the tile tick

/** One tile tick of `dt` seconds (TILE_DT). Machines on a block that is not Held stand still. */
export function stepFlow(st: SimState, dt = TILE_DT): void {
  const f = st.flow;
  if (!f) return;
  for (const m of beltOrder(st, f)) if (running(st, m)) tickBelt(st, m, dt);
  for (const m of f.machines) {
    if (m.kind === 'belt' || m.kind === 'depot' || !running(st, m)) continue;
    if (m.kind === 'inserter') tickInserter(st, m, dt);
    else if (m.kind === 'excavator') tickExcavator(st, m, dt);
    else tickAssembler(st, m, dt);
  }
  tickHand(st, f, dt);
}

/** Real-time driver with the flow layer: tile ticks at TILE_TPS × speed, a block tick (`step`) every TILE_TPS of
 *  them. Commands apply first. Returns the tile ticks run; capped at `maxTicks` block ticks' worth, like `advance`. */
export function advanceFlow(st: SimState, realSeconds: number, commands: readonly Command[] = [], maxTicks = 256): number {
  const f = ensureFlow(st);
  if (commands.length) applyCommands(st, commands);
  st.acc += realSeconds * st.speed * TILE_TPS;
  let n = Math.floor(st.acc + 1e-6);   // 0.045 s × 4 × 20 is not 3.6 exactly; never lose a tick to rounding
  const cap = maxTicks * TILE_TPS;
  if (n > cap) { n = cap; st.acc = 0; } else st.acc -= n;
  for (let k = 0; k < n; k++) {
    stepFlow(st, TILE_DT);
    f.tick++;
    if (f.tick % TILE_TPS === 0) step(st);
  }
  return n;
}

// ------------------------------------------------------------------ placement

export interface PlaceCheck { ok: boolean; reason: string; cost: { steel: number; copper: number } }

/** GAME-ASSUMPTION: a machine goes on any tile of a Held block's cell (lot or its street margin: §14 lets belts run
 *  on streets; an inserter is belt furniture and may too; excavators and assemblers stay on the lot), never on a
 *  tile another machine holds, and only the Excavator may stand on rubble (it digs what it stands on). */
export function canPlace(st: SimState, kind: Kind, tx: number, ty: number): PlaceCheck {
  const f = ensureFlow(st), size = MACHINE_SIZE[kind], cost = MACHINE_COST[kind];
  const fail = (reason: string) => ({ ok: false, reason, cost });
  for (let y = ty; y < ty + size; y++) for (let x = tx; x < tx + size; x++) {
    if (x < 0 || y < 0 || x >= f.tw || y >= (st.h - 1) * CELL_TILES) return fail('outside the city');
    const c = tileCell(x, y);
    const b = st.blocks[idxOf(st, c.x, c.y)];
    if (b.state !== HELD) return fail('the block is not Held');
    if (f.occ[y * f.tw + x] !== undefined) return fail('another machine is there');
    if (isMargin(c.lx, c.ly) && kind !== 'belt' && kind !== 'inserter') return fail('not on the street');
    if (kind !== 'excavator' && kind !== 'depot' && rubbleAt(st, x, y)) return fail('rubble in the way');
  }
  if (kind !== 'depot' && (st.stock.steel < cost.steel || st.stock.copper < cost.copper)) return fail(`not enough in the Depot (${cost.steel} steel${cost.copper ? ` + ${cost.copper} Cu` : ''})`);
  return { ok: true, reason: '', cost };
}

function addMachine(st: SimState, kind: Kind, tx: number, ty: number, dir: Dir): Machine {
  const f = st.flow!;
  const m: Machine = { id: f.next++, kind, x: tx, y: ty, dir, size: MACHINE_SIZE[kind], items: [], hold: null, timer: 0, phase: 0, inv: {}, out: 0, busy: false };
  f.machines.push(m);
  for (let y = ty; y < ty + m.size; y++) for (let x = tx; x < tx + m.size; x++) f.occ[y * f.tw + x] = m.id;
  f.rev++;
  return m;
}

/** Place a machine, paying its cost from the Depot. Returns the machine, or null with the reason in `canPlace`. */
export function place(st: SimState, kind: Kind, tx: number, ty: number, dir: Dir = 0): Machine | null {
  const chk = canPlace(st, kind, tx, ty);
  if (!chk.ok) return null;
  st.stock.steel -= chk.cost.steel; st.stock.copper -= chk.cost.copper;
  return addMachine(st, kind, tx, ty, dir);
}

/** Remove the machine on a tile (never the Depot): the cost comes back, and whatever it held goes to the Depot. */
export function remove(st: SimState, tx: number, ty: number): Machine | null {
  const f = st.flow, m = machineAt(st, tx, ty);
  if (!f || !m || m.kind === 'depot') return null;
  const cost = MACHINE_COST[m.kind];
  st.stock.steel += cost.steel; st.stock.copper += cost.copper;
  for (const it of m.items) deliver(st, it.k);
  if (m.hold) deliver(st, m.hold);
  for (const k in m.inv) for (let i = 0; i < m.inv[k]; i++) deliver(st, k as Item);
  for (let i = 0; i < m.out; i++) deliver(st, 'magazine');
  for (let y = m.y; y < m.y + m.size; y++) for (let x = m.x; x < m.x + m.size; x++) delete f.occ[y * f.tw + x];
  f.machines.splice(f.machines.indexOf(m), 1);
  f.rev++;
  return m;
}

/** Turn a placed machine a quarter clockwise. */
export function rotate(st: SimState, tx: number, ty: number): Machine | null {
  const m = machineAt(st, tx, ty);
  if (!m || !st.flow || m.kind === 'depot') return null;
  m.dir = ((m.dir + 1) % 4) as Dir;
  st.flow.rev++;
  return m;
}

export function setHandMine(st: SimState, at: [number, number] | null): void {
  const f = ensureFlow(st);
  if (at && rubbleAt(st, at[0], at[1])) f.hand.mine = at; else f.hand.mine = null;
  f.hand.prog = 0;
}
export function queueCraft(st: SimState, n = 1): void { ensureFlow(st).hand.crafts = Math.max(0, ensureFlow(st).hand.crafts + n); }

// ------------------------------------------------------------------ queries

export interface FlowSummary {
  excavators: number; belts: number; inserters: number; assemblers: number;
  beltItems: number; assemblersBusy: number;
  /** Capacity: magazines a minute the tile assemblers make when fed (20 each), the block-level summary's meaning too. */
  productionMagPerMin: number;
  magsMade: number; magsDelivered: number; mined: number; coal: number;
  craftsQueued: number;
}
export function flowSummary(st: SimState): FlowSummary {
  const f = st.flow;
  const s: FlowSummary = { excavators: 0, belts: 0, inserters: 0, assemblers: 0, beltItems: 0, assemblersBusy: 0, productionMagPerMin: 0,
                           magsMade: 0, magsDelivered: 0, mined: 0, coal: 0, craftsQueued: 0 };
  if (!f) return s;
  for (const m of f.machines) {
    if (m.kind === 'excavator') s.excavators++;
    else if (m.kind === 'belt') { s.belts++; s.beltItems += m.items.length; }
    else if (m.kind === 'inserter') s.inserters++;
    else if (m.kind === 'assembler') { s.assemblers++; if (m.busy) s.assemblersBusy++; }
  }
  s.productionMagPerMin = s.assemblers * 60 / SHOT.seconds;
  s.magsMade = f.stats.magsMade; s.magsDelivered = f.stats.magsDelivered; s.mined = f.stats.mined; s.coal = f.store.coal; s.craftsQueued = f.hand.crafts;
  return s;
}

/** One line for a tooltip. */
export function describeMachine(st: SimState, m: Machine): string {
  const on = running(st, m) ? '' : ' · stopped (block not Held)';
  switch (m.kind) {
    case 'belt': return `belt → ${DIR_NAMES[m.dir]} · ${m.items.length} item${m.items.length === 1 ? '' : 's'}${on}`;
    case 'inserter': return `inserter → ${DIR_NAMES[m.dir]} · ${m.hold ? `carrying ${m.hold}` : 'empty'}${on}`;
    case 'excavator': { const r = findRubble(st, m); return `Excavator → ${DIR_NAMES[m.dir]} · ${r ? `digging ${r.type} (${Math.ceil(r.units)} left in the tile)` : 'nothing in reach'}${m.hold ? ` · output blocked (${m.hold})` : ''}${on}`; }
    case 'assembler': return `Shot assembler → ${DIR_NAMES[m.dir]} · steel ${m.inv.steel ?? 0} · Cu ${m.inv.copper ?? 0} · ${m.out} magazine${m.out === 1 ? '' : 's'} out${m.busy ? ` · ${Math.round(m.timer / SHOT.seconds * 100)} %` : ''}${on}`;
    case 'depot': return `Depot · steel ${Math.floor(st.stock.steel)} · Cu ${Math.floor(st.stock.copper)} · stone ${Math.floor(st.stock.stone)} · coal ${Math.floor(st.flow!.store.coal)} · ${Math.floor(st.buffer / SHOT.count)} magazines in the line buffer`;
  }
}

/** Lot tiles on the start lot with a patch: for tests and the renderer. */
export function isDepotTile(lx: number, ly: number): boolean {
  return lx >= DEPOT_LOT && lx < DEPOT_LOT + DEPOT_TILES && ly >= DEPOT_LOT && ly < DEPOT_LOT + DEPOT_TILES;
}
