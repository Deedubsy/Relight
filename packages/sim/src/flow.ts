/** Tile-level flow (constitution Phase 4 M2 + M3): Excavator, belts, inserters, the Depot, the Mk1 Shot assembler,
 *  hand-mining and hand-crafting (M2); Gun turrets, Lamps, poles, Generators, the lot substations and streetlights,
 *  and power as one number with the §14 shed order (M3), ticking at a fixed 20 ticks/s. The block map stays the judge of territory: a
 *  machine runs only while its block is Held, digging draws the same pools the block economy draws, magazines
 *  reach the same line buffer the ring hoppers fill from, and one block tick (`step`) closes every 20 tile ticks.
 *
 *  Nothing here exists until `ensureFlow` is called (the game does; the harness and the fixtures do not), so a
 *  state without a flow layer behaves exactly as before M2.
 *
 *  Rates (all measured by test/flow.test.ts, run name M2-rates): Excavator 0.5 items/s onto the tile it faces,
 *  belt 7.5 items/s (4 items a tile at 1.875 tiles/s), inserter 1 item/s, Shot assembler 3 s a magazine (20/min). */
import { SimState, Block, HELD, CONTESTED, DARK, Command } from './types';
import { idxOf, step, applyCommands, tileHooks, effectiveSupply, syncEdges as rebuildRing } from './sim';
import { edgeId, edgeTo } from './graph';
import { RECIPES, START_COAL, COAL_MJ, GENERATOR_KW, TURRET_HOPPER, TURRET_RANGE, TURRET_ROUNDS_PER_S, LAMP_KW, LAMP_RADIUS, POLE_REACH } from './recipes';
import {
  CELL_TILES, MARGIN_TILES, LOT_TILES, P_STEEL, P_COPPER, P_COAL, HQ_PATCHES, DEPOT_LOT, DEPOT_TILES, SUBSTATION_TILES,
  T_STREET, T_RUBBLE, T_INERT, T_RIVER, T_DEPOSIT, T_PATCH,
  lotLayout, isStart, hqPatchAt, goneCount, tileCell, isMargin, substationLot, streetlights, cellTiles,
} from './tiles';
import { poolMax } from './queries';

export const TILE_TPS = 20;                    // constitution: fixed 20 ticks/s at tile level
export const TILE_DT = 1 / TILE_TPS;
/** float slack on second-counting timers (60 × 0.05 does not sum to 3 exactly) */
const EPS = 1e-9;
export type Dir = 0 | 1 | 2 | 3;               // N E S W
export const DX = [0, 1, 0, -1], DY = [-1, 0, 1, 0];
export const DIR_NAMES = ['north', 'east', 'south', 'west'];
export type Kind = 'excavator' | 'belt' | 'inserter' | 'assembler' | 'depot' | 'turret' | 'lamp' | 'pole' | 'generator';
export const KINDS: readonly Kind[] = ['excavator', 'belt', 'inserter', 'assembler', 'depot', 'turret', 'lamp', 'pole', 'generator'];
/** Flow directions (N E S W) to the block sim's edge directions (+x −x +y −y) and back. */
export const SIM_DIR: readonly number[] = [3, 0, 2, 1], FLOW_DIR: readonly Dir[] = [1, 3, 2, 0];
export const SIM_DIR_NAMES = ['east', 'west', 'south', 'north'];
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
export const MACHINE_SIZE: Record<Kind, number> = { excavator: 3, belt: 1, inserter: 1, assembler: 3, depot: DEPOT_TILES, turret: 2, lamp: 1, pole: 1, generator: 2 };
/** GAME-ASSUMPTION: machine costs in rubble (§13 gives none). The assembler costs what the block-level one does
 *  (§12: 20 Cu + 40 steel); the Excavator 10 steel; a belt tile 1 steel; an inserter 1 steel + 1 Cu; (M3) a Gun
 *  turret 15 steel + 5 Cu, a Lamp and a pole 1 steel + 1 Cu each, a Generator 30 steel + 10 Cu. Removal refunds. */
export const MACHINE_COST: Record<Kind, { steel: number; copper: number }> = {
  excavator: { steel: 10, copper: 0 }, belt: { steel: 1, copper: 0 }, inserter: { steel: 1, copper: 1 },
  assembler: { steel: 40, copper: 20 }, depot: { steel: 0, copper: 0 },
  turret: { steel: 15, copper: 5 }, lamp: { steel: 1, copper: 1 }, pole: { steel: 1, copper: 1 }, generator: { steel: 30, copper: 10 },
};
/** §13 power draw (kW). A machine with a draw runs only on a powered cell (M3); belts, turrets, poles and Generators
 *  draw nothing. GAME-ASSUMPTION: a placed machine draws its rated kW whether busy or idle (§13 has no idle draw). */
export const MACHINE_KW: Record<Kind, number> = { excavator: 60, belt: 0, inserter: 10, assembler: 100, depot: 0, turret: 0, lamp: LAMP_KW, pole: 0, generator: 0 };
/** GAME-ASSUMPTION: a Generator holds 50 coal (the §11 start's 40 fit); a turret's muzzle flash lasts half a second. */
export const GENERATOR_COAL_CAP = 50, TURRET_FLASH_S = 0.5;
/** §14 shed order among tile machines: Shot assemblers first, then other machines, Excavators feeding Generators
 *  last. GAME-ASSUMPTION: "feeding a Generator" = digging coal, and the inserter that puts coal into a Generator sheds
 *  with those Excavators (shedding it first starved the second Generator in the §18 drawing); Lamps count as other machines. */
const SHED_RANK: Record<Kind, number> = { assembler: 0, inserter: 1, lamp: 2, excavator: 3, belt: 9, depot: 9, turret: 9, pole: 9, generator: 9 };

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
  /** Assembler: input items on hand and finished magazines; `busy` while a craft's inputs are taken.
   *  Turret (M3): `inv.rounds` in the hopper, `out` rounds fired last second, `timer` flash left, `busy` = covers a live edge.
   *  Generator: `inv.coal`, `timer` the fraction of a coal burned, `busy` while burning. */
  inv: Record<string, number>; out: number; busy: boolean;
  /** M3: switched off by the brownout shedder (§14); back on when the supply allows. */
  shed: boolean;
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
  stats: { magsMade: number; magsDelivered: number; mined: number; handMined: number; handCrafted: number; delivered: Record<Item, number>;
           /** M3: rounds the turrets fired, coal the Generators burned, magazines and coal fed by hand. */
           fired: number; coalBurned: number; handFed: number };
  /** M3: commands the tile layer raises for the block map (a pole run reaching a Dark block's substation claims it). */
  pending: Command[];
  /** M3: the grid as of the last block tick — kW supplied, demanded, delivered; seconds demand exceeded supply. */
  power: { supply: number; demand: number; load: number; overS: number };
}

/** The flow layer, created on first use: the Depot goes on the start lot, the block-level Mk1 stand-in retires
 *  (§12 C9: Phase 4 places the real machine), and from here the HQ patch is dug by machines and hands, not drained. */
export function ensureFlow(st: SimState): FlowState {
  if (st.flow) return upgrade(st.flow);
  const f: FlowState = {
    version: 1, tick: 0, next: 1, rev: 0, tw: st.w * CELL_TILES, machines: [], occ: {}, dug: {}, units: {},
    store: { coal: 0 },
    hand: { mine: null, prog: 0, crafts: 0, crafting: false, craftProg: 0 },
    stats: { magsMade: 0, magsDelivered: 0, mined: 0, handMined: 0, handCrafted: 0, delivered: { steel: 0, copper: 0, stone: 0, coal: 0, magazine: 0 },
             fired: 0, coalBurned: 0, handFed: 0 },
    pending: [],
    power: { supply: 0, demand: 0, load: 0, overS: 0 },
  };
  st.flow = f;
  st.acc = 0;
  const [sx, sy] = st.start;
  const hq = st.blocks[idxOf(st, sx, sy)];
  if (hq.machines > 0) { hq.machines = 0; st.asmManual = Math.max(0, st.asmManual - st.config.startAssemblers); }
  const lot = (lx: number, ly: number): [number, number] => [sx * CELL_TILES + MARGIN_TILES + lx, sy * CELL_TILES + MARGIN_TILES + ly];
  addMachine(st, 'depot', ...lot(DEPOT_LOT, DEPOT_LOT), 0);
  // §11: "two Gun turrets on the north edge with 20 magazines in stock", and later "you have hand-fed the west and east
  // turrets twice each". GAME-ASSUMPTION (M3): the HQ starts with two turrets on each of its three street sides (§5
  // prices two a side; the block sim's 100-round edge hopper is two turrets' worth), the north pair where the §18
  // sketch draws them (lot columns 3 and 16), each facing its street.
  for (const [lx, ly, d] of [[3, 0, 0], [16, 0, 0], [0, 3, 3], [0, 16, 3], [22, 3, 1], [22, 16, 1]] as [number, number, Dir][]) addMachine(st, 'turret', ...lot(lx, ly), d);
  // §11: one Generator (300 kW) with 40 coal. GAME-ASSUMPTION: the 40 coal is in the Generator's hopper, not the
  // Depot (§18: "coal by hand, 40 left"); it stands at lot (19,8), where the §18 sketch draws it.
  const g = addMachine(st, 'generator', ...lot(19, 8), 0);
  g.inv.coal = START_COAL;
  prefillTurrets(st);
  return f;
}

/** A flow layer saved before M3 gets the M3 fields. */
function upgrade(f: FlowState): FlowState {
  f.pending ??= []; f.power ??= { supply: 0, demand: 0, load: 0, overS: 0 };
  f.stats.fired ??= 0; f.stats.coalBurned ??= 0; f.stats.handFed ??= 0;
  for (const m of f.machines) m.shed ??= false;
  return f;
}

/** GAME-ASSUMPTION: the HQ's turrets start stocked the way the block sim's first ring fill stocks its edges (C10: the
 *  20 starting magazines go round the ring in order, two edges full and the third empty — the pip ladder's first
 *  lesson); a block-only snapshot's stand-in hoppers on the HQ pour into its turrets the same way. */
function prefillTurrets(st: SimState): void {
  const f = st.flow!;
  rebuildRing(st);
  const hqIdx = idxOf(st, st.start[0], st.start[1]);
  for (const e of st.ring) if (e.a === hqIdx) { st.buffer = Math.min(st.config.bufferCap, st.buffer + e.hopper); e.hopper = 0; }
  hookSyncEdges(st);
  for (const e of st.ring) {
    if (!e.turrets) continue;
    for (const m of f.machines) {
      if (m.kind !== 'turret' || turretEdge(st, m) !== e.id) continue;
      const give = Math.min(TURRET_HOPPER - (m.inv.rounds ?? 0), st.buffer);
      if (give > 0) { m.inv.rounds = (m.inv.rounds ?? 0) + give; st.buffer -= give; }
    }
  }
  hookSyncEdges(st);
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
function blockOf(st: SimState, m: Machine): Block {
  return st.blocks[idxOf(st, Math.floor(m.x / CELL_TILES), Math.floor(m.y / CELL_TILES))];
}
/** §14: each substation powers its whole cell. It is powered when its block is claimed (Held or Contested), the
 *  block sim has not switched it off (unfed, shed, shade), and the grid has any supply at all. GAME-ASSUMPTION: a
 *  grid with no Generator burning is dead, not browned out — everything on it stops at once. */
export function subPowered(st: SimState, b: Block): boolean {
  if (b.state !== HELD && b.state !== CONTESTED) return false;
  if (!b.subOn || st.t < b.shadeOff) return false;
  if (st.config.power && effectiveSupply(st) <= 0) return false;
  return true;
}
/** A machine with a draw runs only while its cell is powered and the shedder has not switched it off. */
export function powered(st: SimState, m: Machine): boolean {
  if (MACHINE_KW[m.kind] === 0) return true;
  if (m.shed) return false;
  return subPowered(st, blockOf(st, m));
}
function running(st: SimState, m: Machine): boolean {
  const cy = Math.floor(m.y / CELL_TILES);
  return cy < st.h - 1 && blockOf(st, m).state === HELD && powered(st, m);
}
function stopReason(st: SimState, m: Machine): string {
  if (Math.floor(m.y / CELL_TILES) >= st.h - 1 || blockOf(st, m).state !== HELD) return ' · stopped (block not Held)';
  if (MACHINE_KW[m.kind] === 0) return '';
  if (m.shed) return ' · shed (brownout)';
  if (!subPowered(st, blockOf(st, m))) return ' · no power';
  return '';
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
    case 'turret': return k === 'magazine' && (m.inv.rounds ?? 0) + SHOT.count <= TURRET_HOPPER + 1e-9;
    case 'generator': return k === 'coal' && (m.inv.coal ?? 0) < GENERATOR_COAL_CAP;
    default: return false;
  }
}
/** Could this machine ever take this item (type only)? An inserter picks up by this and waits by `accepts`. */
export function wants(m: Machine, k: Item): boolean {
  switch (m.kind) {
    case 'belt': case 'depot': return true;
    case 'assembler': return SHOT.inputs[k] !== undefined;
    case 'turret': return k === 'magazine';
    case 'generator': return k === 'coal';
    default: return false;
  }
}
export function giveItem(st: SimState, m: Machine, k: Item, p = 0): boolean {
  if (!accepts(st, m, k, p)) return false;
  if (m.kind === 'belt') beltInsert(m, k, p);
  else if (m.kind === 'depot') return deliver(st, k);
  else if (m.kind === 'turret') m.inv.rounds = (m.inv.rounds ?? 0) + SHOT.count;   // a magazine is ten rounds in the hopper
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

/** M3: a Generator burns coal in proportion to what it delivers: the grid's load (the last block tick's
 *  min(demand, supply)) shared equally among the Generators that have coal, 4 MJ a coal (§11), so one at full load
 *  burns 0.075 coal/s and the §11 start's 40 coal last 8.9 minutes. GAME-ASSUMPTION: equal shares; no idle burn. */
function tickGenerator(st: SimState, m: Machine, dt: number, shareKw: number): void {
  if (shareKw <= 0 || (m.inv.coal ?? 0) <= 0) { m.busy = false; return; }
  m.busy = true;
  m.timer += shareKw / (COAL_MJ * 1000) * dt;
  while (m.timer >= 1 - EPS) {
    m.timer -= 1; m.inv.coal--; st.flow!.stats.coalBurned++;
    if (m.inv.coal <= 0) {
      m.inv.coal = 0; m.timer = 0; m.busy = false;
      const b = blockOf(st, m);
      st.events.push({ type: 'gen-dry', t: st.t, x: b.x, y: b.y });
      break;
    }
  }
}

/** One tile tick of `dt` seconds (TILE_DT). Machines on a block that is not Held stand still. */
export function stepFlow(st: SimState, dt = TILE_DT): void {
  const f = st.flow;
  if (!f) return;
  for (const m of beltOrder(st, f)) if (running(st, m)) tickBelt(st, m, dt);
  let gens = 0;
  for (const m of f.machines) if (m.kind === 'generator' && (m.inv.coal ?? 0) > 0 && running(st, m)) gens++;
  const share = gens ? f.power.load / gens : 0;
  for (const m of f.machines) {
    if (m.kind === 'turret') { m.timer = Math.max(0, m.timer - dt); continue; }
    if (m.kind === 'generator') { if (running(st, m)) tickGenerator(st, m, dt, share); continue; }
    if (m.kind === 'belt' || m.kind === 'depot' || m.kind === 'lamp' || m.kind === 'pole' || !running(st, m)) continue;
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
  let ev0 = st.events.length;
  if (commands.length) applyCommands(st, commands);
  if (f.pending.length) { const p = f.pending; f.pending = []; applyCommands(st, p); }
  stringClaims(st, ev0);
  st.acc += realSeconds * st.speed * TILE_TPS;
  let n = Math.floor(st.acc + 1e-6);   // 0.045 s × 4 × 20 is not 3.6 exactly; never lose a tick to rounding
  const cap = maxTicks * TILE_TPS;
  if (n > cap) { n = cap; st.acc = 0; } else st.acc -= n;
  for (let k = 0; k < n; k++) {
    stepFlow(st, TILE_DT);
    f.tick++;
    if (f.tick % TILE_TPS === 0) { ev0 = st.events.length; step(st); stringClaims(st, ev0); }
  }
  return n;
}
/** Every claim the block map accepted since event `from` gets its pole run (a claim raised by a pole run is already strung). */
function stringClaims(st: SimState, from: number): void {
  for (let i = from; i < st.events.length; i++) { const ev = st.events[i]; if (ev.type === 'claim') layPoles(st, ev.x, ev.y); }
}

// ------------------------------------------------------------------ placement

export interface PlaceCheck { ok: boolean; reason: string; cost: { steel: number; copper: number } }

/** GAME-ASSUMPTION: a machine goes on any tile of a Held block's cell (lot or its street margin: §14 lets belts run
 *  on streets; an inserter is belt furniture and may too; excavators, assemblers and Generators stay on the lot;
 *  (M3) turrets and Lamps may stand on the street too — §14 keeps room for "a lamp line" there; a pole goes on any
 *  street tile of the city and on any lot that is not Inert — Dark included, since a pole run is how the crew
 *  strings a claim ahead of itself),
 *  never on a tile another machine or the lot's substation holds, and only the Excavator may stand on rubble (it digs
 *  what it stands on) — except posts: a pole or a Lamp is thin enough to stand among rubble. */
export function placeable(st: SimState, kind: Kind, tx: number, ty: number): string {
  const f = ensureFlow(st), size = MACHINE_SIZE[kind];
  const post = kind === 'pole' || kind === 'lamp';
  for (let y = ty; y < ty + size; y++) for (let x = tx; x < tx + size; x++) {
    if (x < 0 || y < 0 || x >= f.tw || y >= (st.h - 1) * CELL_TILES) return 'outside the city';
    const c = tileCell(x, y);
    const b = st.blocks[idxOf(st, c.x, c.y)], margin = isMargin(c.lx, c.ly);
    if (b.state !== HELD && !(kind === 'pole' && (margin || b.state === CONTESTED || b.state === DARK))) return 'the block is not Held';
    if (f.occ[y * f.tw + x] !== undefined) return 'another machine is there';
    if (margin && kind !== 'belt' && kind !== 'inserter' && kind !== 'turret' && !post) return 'not on the street';
    if (!margin && isSubstationTile(st, x, y)) return 'the substation is there';
    if (kind !== 'excavator' && kind !== 'depot' && !post && rubbleAt(st, x, y)) return 'rubble in the way';
  }
  return '';
}
export function canPlace(st: SimState, kind: Kind, tx: number, ty: number): PlaceCheck {
  const cost = MACHINE_COST[kind];
  const reason = placeable(st, kind, tx, ty);
  if (reason) return { ok: false, reason, cost };
  if (kind !== 'depot' && (st.stock.steel < cost.steel || st.stock.copper < cost.copper)) return { ok: false, reason: `not enough in the Depot (${cost.steel} steel${cost.copper ? ` + ${cost.copper} Cu` : ''})`, cost };
  return { ok: true, reason: '', cost };
}

function addMachine(st: SimState, kind: Kind, tx: number, ty: number, dir: Dir): Machine {
  const f = st.flow!;
  const m: Machine = { id: f.next++, kind, x: tx, y: ty, dir, size: MACHINE_SIZE[kind], items: [], hold: null, timer: 0, phase: 0, inv: {}, out: 0, busy: false, shed: false };
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
  const m = addMachine(st, kind, tx, ty, dir);
  if (kind === 'pole') poleClaims(st, m);
  return m;
}

/** Remove the machine on a tile (never the Depot): the cost comes back, and whatever it held goes to the Depot. */
export function remove(st: SimState, tx: number, ty: number): Machine | null {
  const f = st.flow, m = machineAt(st, tx, ty);
  if (!f || !m || m.kind === 'depot') return null;
  const cost = MACHINE_COST[m.kind];
  st.stock.steel += cost.steel; st.stock.copper += cost.copper;
  for (const it of m.items) deliver(st, it.k);
  if (m.hold) deliver(st, m.hold);
  if (m.kind === 'turret') st.buffer = Math.min(st.config.bufferCap, st.buffer + (m.inv.rounds ?? 0));   // rounds back to the line buffer
  else if (m.kind === 'generator') f.store.coal += m.inv.coal ?? 0;
  else {
    for (const k in m.inv) for (let i = 0; i < m.inv[k]; i++) deliver(st, k as Item);
    for (let i = 0; i < m.out; i++) deliver(st, 'magazine');
  }
  for (let y = m.y; y < m.y + m.size; y++) for (let x = m.x; x < m.x + m.size; x++) delete f.occ[y * f.tw + x];
  f.machines.splice(f.machines.indexOf(m), 1);
  f.rev++;
  return m;
}

/** Turn a placed machine a quarter clockwise. */
export function rotate(st: SimState, tx: number, ty: number): Machine | null {
  const m = machineAt(st, tx, ty);
  if (!m || !st.flow || m.kind === 'depot' || m.kind === 'pole' || m.kind === 'lamp') return null;
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
  /** Coal in the Depot and in the Generators' hoppers together. */
  magsMade: number; magsDelivered: number; mined: number; coal: number;
  craftsQueued: number;
  /** M3: defence and power. */
  turrets: number; lamps: number; lampsLit: number; poles: number; polesConnected: number; generators: number; generatorsBurning: number;
  turretRounds: number; turretCap: number; genCoal: number; beltAmmo: number;
  supplyKw: number; demandKw: number; loadKw: number; brownoutS: number; fired: number; coalBurned: number; handFed: number; shedMachines: number;
}
export function flowSummary(st: SimState): FlowSummary {
  const f = st.flow;
  const s: FlowSummary = { excavators: 0, belts: 0, inserters: 0, assemblers: 0, beltItems: 0, assemblersBusy: 0, productionMagPerMin: 0,
                           magsMade: 0, magsDelivered: 0, mined: 0, coal: 0, craftsQueued: 0,
                           turrets: 0, lamps: 0, lampsLit: 0, poles: 0, polesConnected: 0, generators: 0, generatorsBurning: 0, turretRounds: 0, turretCap: 0, genCoal: 0, beltAmmo: 0,
                           supplyKw: 0, demandKw: 0, loadKw: 0, brownoutS: 0, fired: 0, coalBurned: 0, handFed: 0, shedMachines: 0 };
  if (!f) return s;
  for (const m of f.machines) {
    if (m.shed) s.shedMachines++;
    if (m.kind === 'excavator') s.excavators++;
    else if (m.kind === 'belt') { s.belts++; s.beltItems += m.items.length; for (const it of m.items) if (it.k === 'magazine') s.beltAmmo++; }
    else if (m.kind === 'inserter') s.inserters++;
    else if (m.kind === 'assembler') { s.assemblers++; if (m.busy) s.assemblersBusy++; }
    else if (m.kind === 'turret') { s.turrets++; s.turretRounds += m.inv.rounds ?? 0; s.turretCap += TURRET_HOPPER; }
    else if (m.kind === 'lamp') { s.lamps++; if (running(st, m)) s.lampsLit++; }
    else if (m.kind === 'pole') s.poles++;
    else if (m.kind === 'generator') { s.generators++; s.genCoal += m.inv.coal ?? 0; if (m.busy) s.generatorsBurning++; }
  }
  s.productionMagPerMin = s.assemblers * 60 / SHOT.seconds;
  s.magsMade = f.stats.magsMade; s.magsDelivered = f.stats.magsDelivered; s.mined = f.stats.mined; s.coal = f.store.coal + s.genCoal; s.craftsQueued = f.hand.crafts;
  s.supplyKw = f.power.supply; s.demandKw = f.power.demand; s.loadKw = f.power.load; s.brownoutS = f.power.overS;
  s.fired = f.stats.fired; s.coalBurned = f.stats.coalBurned; s.handFed = f.stats.handFed;
  if (s.poles) s.polesConnected = poleGrid(st).connected.size;
  return s;
}

/** One line for a tooltip. */
export function describeMachine(st: SimState, m: Machine): string {
  const on = stopReason(st, m);
  switch (m.kind) {
    case 'turret': {
      const id = turretEdge(st, m), b = blockOf(st, m);
      const side = id < 0 ? '' : streetName(st, m, id);
      const where = id < 0 ? 'too far from any street' : st.edgeAt[id] >= 0 ? `covers the ${side} street` : `${side} street: nothing to shoot at`;
      return `Gun turret · ${Math.floor(m.inv.rounds ?? 0)} / ${TURRET_HOPPER} rounds · range ${TURRET_RANGE} · ${where}${m.out > 0 ? ` · firing ${Math.round(m.out)}/s` : ''}${(m.inv.rounds ?? 0) <= 0 && b.state === HELD ? ' · EMPTY: feed it (hand or inserter)' : ''}${on}`;
    }
    case 'lamp': return `Lamp · ${LAMP_KW} kW · radius ${LAMP_RADIUS} · ${running(st, m) ? 'lit' : 'dark'}${on}`;
    case 'pole': return `Pole · reach ${POLE_REACH} · ${poleGrid(st).connected.has(m.id) ? 'on the grid' : 'not connected'}`;
    case 'generator': return `Generator · ${GENERATOR_KW} kW · ${Math.floor(m.inv.coal ?? 0)} / ${GENERATOR_COAL_CAP} coal · ${m.busy ? `burning (${Math.round(st.flow!.power.load / Math.max(1, st.flow!.machines.filter(g => g.kind === 'generator' && g.busy).length))} kW)` : (m.inv.coal ?? 0) > 0 ? 'idle' : 'OUT OF COAL'}${on}`;
    case 'belt': return `belt → ${DIR_NAMES[m.dir]} · ${m.items.length} item${m.items.length === 1 ? '' : 's'}${on}`;
    case 'inserter': return `inserter → ${DIR_NAMES[m.dir]} · ${m.hold ? `carrying ${m.hold}` : 'empty'}${on}`;
    case 'excavator': { const r = findRubble(st, m); return `Excavator → ${DIR_NAMES[m.dir]} · ${r ? `digging ${r.type} (${Math.ceil(r.units)} left in the tile)` : 'nothing in reach'}${m.hold ? ` · output blocked (${m.hold})` : ''}${on}`; }
    case 'assembler': return `Shot assembler → ${DIR_NAMES[m.dir]} · steel ${m.inv.steel ?? 0} · Cu ${m.inv.copper ?? 0} · ${m.out} magazine${m.out === 1 ? '' : 's'} out${m.busy ? ` · ${Math.round(m.timer / SHOT.seconds * 100)} %` : ''}${on}`;
    case 'depot': return `Depot · steel ${Math.floor(st.stock.steel)} · Cu ${Math.floor(st.stock.copper)} · stone ${Math.floor(st.stock.stone)} · coal ${Math.floor(st.flow!.store.coal)} · ${Math.floor(st.buffer / SHOT.count)} magazines in the line buffer`;
  }
}

// ------------------------------------------------------------------ M3: turrets and the ring

/** The street a turret covers: the nearest of its cell's four street centre lines, if within range 9 of the turret's
 *  centre. GAME-ASSUMPTION: one street a turret, the nearest; a turret deeper in the lot than its range covers
 *  nothing. Returns the block sim's edge id (graph.ts) or -1. */
/** The compass name of the street an edge crosses from the turret's block (lattice) or 'front' in a city. */
function streetName(st: SimState, m: Machine, id: number): string {
  if (!st.lattice) return 'front';
  const b = st.blocks[edgeTo(st, id)], bx = Math.floor(m.x / CELL_TILES), by = Math.floor(m.y / CELL_TILES);
  return SIM_DIR_NAMES[b.x > bx ? 0 : b.x < bx ? 1 : b.y > by ? 2 : 3];
}

export function turretEdge(st: SimState, m: Machine): number {
  const bx = Math.floor(m.x / CELL_TILES), by = Math.floor(m.y / CELL_TILES);
  if (by >= st.h - 1) return -1;
  const cx = m.x + m.size / 2 - bx * CELL_TILES, cy = m.y + m.size / 2 - by * CELL_TILES;
  const d = [cy, CELL_TILES - cx, CELL_TILES - cy, cx];   // N E S W (flow dirs)
  let best = 0;
  for (let k = 1; k < 4; k++) if (d[k] < d[best]) best = k;
  if (d[best] > TURRET_RANGE) return -1;
  const dx = [0, 1, 0, -1][best], dy = [-1, 0, 1, 0][best];   // the neighbour across that street
  if (bx + dx < 0 || bx + dx >= st.w || by + dy < 0 || by + dy >= st.h) return -1;
  return edgeId(st, idxOf(st, bx, by), idxOf(st, bx + dx, by + dy));
}

/** GAME-ASSUMPTION: on the HQ block — the one lot Phase 4 really builds on — an edge fires only through physical
 *  turrets; every other Held block keeps the block-level stand-in hopper until M6 decides (D-P4-5). */
function hookSyncEdges(st: SimState): void {
  const f = st.flow!, ring = st.ring, hqIdx = idxOf(st, st.start[0], st.start[1]);
  for (const e of ring) {
    if (e.a === hqIdx) { e.turrets = 0; e.fire = 0; e.hopper = 0; }
    else if (e.turrets !== undefined) { e.turrets = 0; e.fire = 0; e.hopper = 0; }
  }
  for (const m of f.machines) {
    if (m.kind !== 'turret') continue;
    m.busy = false; m.out = 0;
    if (!running(st, m)) continue;
    const id = turretEdge(st, m);
    if (id < 0) continue;
    const ri = st.edgeAt[id];
    if (ri < 0) continue;
    const e = ring[ri];
    if (e.turrets === undefined) { e.turrets = 0; e.fire = 0; e.hopper = 0; }
    const rounds = m.inv.rounds ?? 0;
    e.turrets!++; e.fire! += Math.min(rounds, TURRET_ROUNDS_PER_S); e.hopper += rounds;
    m.busy = true;
  }
  for (const e of ring) if (e.turrets === 0 && e.a !== hqIdx) { delete e.turrets; delete e.fire; }
}

/** This second's fired rounds come out of the edge's turrets in equal shares, at most 5 each; a turret with less than
 *  its share gives what it has and the others cover it. */
function hookDrainEdges(st: SimState, fired: Float64Array): void {
  const f = st.flow!;
  for (let r = 0; r < st.ring.length; r++) {
    let left = fired[r];
    const e = st.ring[r];
    if (left <= 1e-9 || !e.turrets) continue;
    const ts = f.machines.filter(m => m.kind === 'turret' && m.busy && turretEdge(st, m) === e.id).sort((a, b) => (a.inv.rounds ?? 0) - (b.inv.rounds ?? 0));
    for (let i = 0; i < ts.length; i++) {
      const m = ts[i];
      const take = Math.min(left / (ts.length - i), TURRET_ROUNDS_PER_S, m.inv.rounds ?? 0);
      if (take <= 0) continue;
      m.inv.rounds = (m.inv.rounds ?? 0) - take; m.out = take; m.timer = TURRET_FLASH_S; f.stats.fired += take; left -= take;
      if (left <= 1e-9) break;
    }
  }
}

// ------------------------------------------------------------------ M3: power

function hookSupplyKw(st: SimState): number {
  const f = st.flow!;
  let kw = 0;
  for (const m of f.machines) if (m.kind === 'generator' && (m.inv.coal ?? 0) > 0 && running(st, m)) kw += GENERATOR_KW;
  return kw;
}
function hookDemandKw(st: SimState, unshed: boolean): number {
  const f = st.flow!;
  let kw = 0;
  for (const m of f.machines) {
    const w = MACHINE_KW[m.kind];
    if (!w) continue;
    if (Math.floor(m.y / CELL_TILES) >= st.h - 1) continue;
    const b = blockOf(st, m);
    if (b.state !== HELD) continue;
    if (unshed || (!m.shed && b.subOn && st.t >= b.shadeOff)) kw += w;
  }
  return kw;
}
function shedRank(st: SimState, m: Machine): number {
  if (m.kind === 'excavator') { const r = findRubble(st, m); return r && r.type === 'coal' ? 4 : 3; }
  if (m.kind === 'inserter') { const [dx, dy] = outputTile(m); if (machineAt(st, dx, dy)?.kind === 'generator') return 4; }
  return SHED_RANK[m.kind];
}
function hookShedOne(st: SimState): { id: number; kind: string } | null {
  const f = st.flow!;
  let best: Machine | null = null, br = 99;
  for (const m of f.machines) {
    if (!MACHINE_KW[m.kind] || m.shed || !running(st, m)) continue;
    const r = shedRank(st, m);
    if (r < br || (r === br && best && m.id > best.id)) { best = m; br = r; }
  }
  if (!best) return null;
  best.shed = true;
  return { id: best.id, kind: best.kind };
}
function hookRestoreOne(st: SimState, id: number, room: number): { ok: boolean; kind: string } {
  const m = machineById(st, id);
  if (!m) return { ok: true, kind: '' };
  if (MACHINE_KW[m.kind] <= room + 1e-9) { m.shed = false; return { ok: true, kind: m.kind }; }
  return { ok: false, kind: m.kind };
}
function hookSetLoad(st: SimState, supply: number, demand: number, load: number, over: boolean): void {
  const f = st.flow!;
  f.power.supply = supply; f.power.demand = demand; f.power.load = load;
  if (over) f.power.overS++;
}
tileHooks.current = { syncEdges: hookSyncEdges, drainEdges: hookDrainEdges, supplyKw: hookSupplyKw, demandKw: hookDemandKw, shedOne: hookShedOne, restoreOne: hookRestoreOne, setLoad: hookSetLoad };

// ------------------------------------------------------------------ M3: substations, streetlights, light

export interface SubstationView { tx: number; ty: number; on: boolean; kw: number }
/** The cell's pre-existing substation (tiles.ts places it), with whether it is powered and what it draws now. */
export function substationAt(st: SimState, bx: number, by: number): SubstationView | null {
  if (bx < 0 || by < 0 || bx >= st.w || by >= st.h - 1) return null;
  const b = st.blocks[idxOf(st, bx, by)];
  if (b.state !== HELD && b.state !== CONTESTED && b.state !== DARK) return null;
  const [lx, ly] = substationLot(st.seed, b, isStart(st, b));
  const half = st.config.draw === 'half';
  const kw = b.state === DARK ? 0 : b.state === CONTESTED ? (half ? 100 : 200) : st.config.draw === 'flat' ? 120 : b.exposed ? (half ? 100 : 200) : (half ? 20 : 40);
  return { tx: bx * CELL_TILES + MARGIN_TILES + lx, ty: by * CELL_TILES + MARGIN_TILES + ly, on: subPowered(st, b), kw };
}
export function isSubstationTile(st: SimState, tx: number, ty: number): boolean {
  const c = tileCell(tx, ty);
  if (c.x < 0 || c.y < 0 || c.x >= st.w || c.y >= st.h - 1 || isMargin(c.lx, c.ly)) return false;
  const b = st.blocks[idxOf(st, c.x, c.y)];
  const [lx, ly] = substationLot(st.seed, b, isStart(st, b));
  const llx = c.lx - MARGIN_TILES, lly = c.ly - MARGIN_TILES;
  return llx >= lx && llx < lx + SUBSTATION_TILES && lly >= ly && lly < ly + SUBSTATION_TILES;
}

export interface Light { tx: number; ty: number; r: number; lit: boolean; broken: boolean; kind: 'streetlight' | 'lamp' }
/** Everything that can light a cell: its streetlights (lit when the substation powers and they are not broken) and
 *  the Lamps on it (lit while powered). Radii are §13's 4 tiles; the light texture itself is M5. */
export function cellLights(st: SimState, bx: number, by: number): Light[] {
  if (bx < 0 || by < 0 || bx >= st.w || by >= st.h - 1) return [];
  const b = st.blocks[idxOf(st, bx, by)];
  if (b.state !== HELD && b.state !== CONTESTED && b.state !== DARK) return [];
  const on = subPowered(st, b);
  const out: Light[] = streetlights(st.seed, b).map(l => ({ tx: l.tx, ty: l.ty, r: LAMP_RADIUS, lit: on && !l.broken, broken: l.broken, kind: 'streetlight' as const }));
  const f = st.flow;
  if (f) for (const m of f.machines) {
    if (m.kind !== 'lamp' || Math.floor(m.x / CELL_TILES) !== bx || Math.floor(m.y / CELL_TILES) !== by) continue;
    out.push({ tx: m.x, ty: m.y, r: LAMP_RADIUS, lit: running(st, m), broken: false, kind: 'lamp' });
  }
  return out;
}
/** Is a tile lit by any light within its radius (this cell's or a neighbour's)? */
export function litAt(st: SimState, tx: number, ty: number): boolean {
  const bx = Math.floor(tx / CELL_TILES), by = Math.floor(ty / CELL_TILES);
  for (let dy = -1; dy <= 1; dy++) for (let dx = -1; dx <= 1; dx++) {
    for (const l of cellLights(st, bx + dx, by + dy)) {
      if (!l.lit) continue;
      const ex = l.tx - tx, ey = l.ty - ty;
      if (ex * ex + ey * ey <= l.r * l.r + 1e-9) return true;
    }
  }
  return false;
}

// ------------------------------------------------------------------ M3: poles

export interface PoleGrid {
  /** Pole ids linked (through poles within reach 8 of each other) to a claimed block's substation. */
  connected: Set<number>;
  /** Wires to draw: from a pole's centre to what it hangs from (a pole or a substation), in tile units. */
  links: { x0: number; y0: number; x1: number; y1: number }[];
  /** Dark blocks whose substation a connected pole reaches. */
  reached: number[];
}
const gridCache = new WeakMap<FlowState, { rev: number; t: number; grid: PoleGrid }>();
function distToRect(px: number, py: number, rx: number, ry: number, size: number): number {
  const qx = Math.max(rx, Math.min(rx + size, px)), qy = Math.max(ry, Math.min(ry + size, py));
  return Math.hypot(px - qx, py - qy);
}
/** GAME-ASSUMPTION: a pole hangs from any pole or claimed substation within reach 8 (centre to centre, or to the
 *  substation's nearest edge). The block map stays the judge of power: the poles are how a claim is strung and shown,
 *  and a Dark substation a connected pole reaches raises the claim; a claimed block keeps its power whether or not
 *  its poles still stand. */
export function poleGrid(st: SimState): PoleGrid {
  const f = ensureFlow(st);
  const c = gridCache.get(f);
  if (c && c.rev === f.rev && c.t === st.t) return c.grid;
  const grid: PoleGrid = { connected: new Set(), links: [], reached: [] };
  const poles = f.machines.filter(m => m.kind === 'pole');
  const subs: { bi: number; x: number; y: number; claimed: boolean }[] = [];
  for (const b of st.blocks) {
    if (!b || b.y >= st.h - 1 || (b.state !== HELD && b.state !== CONTESTED && b.state !== DARK)) continue;
    const [lx, ly] = substationLot(st.seed, b, isStart(st, b));
    subs.push({ bi: idxOf(st, b.x, b.y), x: b.x * CELL_TILES + MARGIN_TILES + lx, y: b.y * CELL_TILES + MARGIN_TILES + ly, claimed: b.state !== DARK });
  }
  const queue: Machine[] = [];
  for (const m of poles) {
    for (const s of subs) {
      if (!s.claimed || distToRect(m.x + 0.5, m.y + 0.5, s.x, s.y, SUBSTATION_TILES) > POLE_REACH) continue;
      grid.connected.add(m.id); queue.push(m);
      grid.links.push({ x0: m.x + 0.5, y0: m.y + 0.5, x1: s.x + SUBSTATION_TILES / 2, y1: s.y + SUBSTATION_TILES / 2 });
      break;
    }
  }
  for (let q = 0; q < queue.length; q++) {
    const a = queue[q];
    for (const m of poles) {
      if (grid.connected.has(m.id) || Math.hypot(m.x - a.x, m.y - a.y) > POLE_REACH) continue;
      grid.connected.add(m.id); queue.push(m);
      grid.links.push({ x0: m.x + 0.5, y0: m.y + 0.5, x1: a.x + 0.5, y1: a.y + 0.5 });
    }
  }
  for (const s of subs) {
    if (s.claimed) continue;
    for (const m of queue) if (distToRect(m.x + 0.5, m.y + 0.5, s.x, s.y, SUBSTATION_TILES) <= POLE_REACH) { grid.reached.push(s.bi); break; }
  }
  gridCache.set(f, { rev: f.rev, t: st.t, grid });
  return grid;
}
/** A just-placed pole that joins the grid and reaches a Dark block's substation raises that block's claim; the block
 *  sim accepts or rejects it by its own rules (adjacency, cost) on the next tick. */
function poleClaims(st: SimState, m: Machine): void {
  const f = st.flow!, g = poleGrid(st);
  if (!g.connected.has(m.id)) return;
  for (const bi of g.reached) {
    const b = st.blocks[bi];
    if (f.pending.some(c => c.type === 'claim' && c.x === b.x && c.y === b.y)) continue;
    if (distToRect(m.x + 0.5, m.y + 0.5, b.x * CELL_TILES + MARGIN_TILES + substationLot(st.seed, b, false)[0], b.y * CELL_TILES + MARGIN_TILES + substationLot(st.seed, b, false)[1], SUBSTATION_TILES) > POLE_REACH) continue;
    f.pending.push({ type: 'claim', x: b.x, y: b.y });
  }
}
/** A claim made from the map view strings its poles: from the nearest connected point (a pole, or a claimed
 *  neighbour's substation) straight towards the new block's substation, one pole every 7 tiles, each shifted to the
 *  nearest free tile. GAME-ASSUMPTION: the claim's 10 wire + 5 frames already paid for them; a run that finds no room
 *  simply stops short (the map still powers the block). */
export function layPoles(st: SimState, x: number, y: number): number {
  const f = ensureFlow(st), b = st.blocks[idxOf(st, x, y)];
  if (b.state !== HELD && b.state !== CONTESTED) return 0;
  const [lx, ly] = substationLot(st.seed, b, isStart(st, b));
  const tx = x * CELL_TILES + MARGIN_TILES + lx, ty = y * CELL_TILES + MARGIN_TILES + ly, tcx = tx + 1.5, tcy = ty + 1.5;
  const g = poleGrid(st);
  let from: [number, number] | null = null, best = Infinity;
  for (const m of f.machines) {
    if (m.kind !== 'pole' || !g.connected.has(m.id)) continue;
    const d = distToRect(m.x + 0.5, m.y + 0.5, tx, ty, SUBSTATION_TILES);
    if (d <= POLE_REACH) return 0;   // already strung (a world-view claim)
    if (d < best) { best = d; from = [m.x + 0.5, m.y + 0.5]; }
  }
  for (const [dx, dy] of [[0, -1], [1, 0], [0, 1], [-1, 0]]) {
    const nx = x + dx, ny = y + dy;
    if (nx < 0 || ny < 0 || nx >= st.w || ny >= st.h - 1) continue;
    const nb = st.blocks[idxOf(st, nx, ny)];
    if (nb.state !== HELD && nb.state !== CONTESTED) continue;
    if (nb.x === x && nb.y === y) continue;
    const [sx, sy] = substationLot(st.seed, nb, isStart(st, nb));
    const cx = nx * CELL_TILES + MARGIN_TILES + sx + 1.5, cy = ny * CELL_TILES + MARGIN_TILES + sy + 1.5;
    const d = Math.hypot(cx - tcx, cy - tcy);
    if (d < best) { best = d; from = [cx, cy]; }
  }
  if (!from) return 0;
  let [cx, cy] = from, laid = 0;
  for (let n = 0; n < 16; n++) {
    if (distToRect(cx, cy, tx, ty, SUBSTATION_TILES) <= POLE_REACH) break;
    const d = Math.hypot(tcx - cx, tcy - cy), ux = (tcx - cx) / d, uy = (tcy - cy) / d;
    const stepLen = Math.min(POLE_REACH - 1, d);
    const gx = Math.floor(cx + ux * stepLen), gy = Math.floor(cy + uy * stepLen);
    let put: [number, number] | null = null;
    for (let ring = 0; ring <= 3 && !put; ring++) {
      for (let oy = -ring; oy <= ring && !put; oy++) for (let ox = -ring; ox <= ring; ox++) {
        if (Math.max(Math.abs(ox), Math.abs(oy)) !== ring) continue;
        const px = gx + ox, py = gy + oy;
        if (Math.hypot(px + 0.5 - cx, py + 0.5 - cy) > POLE_REACH) continue;
        if (placeable(st, 'pole', px, py) === '') { put = [px, py]; break; }
      }
    }
    if (!put) break;
    addMachine(st, 'pole', put[0], put[1], 0);
    laid++; cx = put[0] + 0.5; cy = put[1] + 0.5;
  }
  return laid;
}

// ------------------------------------------------------------------ M3: hands

/** Hand-feed a turret from the Depot's magazines (the line buffer) or a Generator from the Depot's coal, filling it.
 *  GAME-ASSUMPTION: instant — §11's "hand-fed the west and east turrets twice each" is two clicks. */
export function handFeed(st: SimState, tx: number, ty: number): { kind: 'turret' | 'generator'; moved: number; reason: string } | null {
  const f = st.flow, m = machineAt(st, tx, ty);
  if (!f || !m) return null;
  if (m.kind === 'turret') {
    const room = Math.floor((TURRET_HOPPER - (m.inv.rounds ?? 0)) / SHOT.count), have = Math.floor(st.buffer / SHOT.count);
    const mags = Math.max(0, Math.min(room, have));
    if (mags > 0) { m.inv.rounds = (m.inv.rounds ?? 0) + mags * SHOT.count; st.buffer -= mags * SHOT.count; f.stats.handFed += mags; }
    return { kind: 'turret', moved: mags, reason: mags ? '' : room <= 0 ? 'the hopper is full' : 'no magazines in the Depot (craft with C, or build the line)' };
  }
  if (m.kind === 'generator') {
    const n = Math.max(0, Math.min(GENERATOR_COAL_CAP - (m.inv.coal ?? 0), Math.floor(f.store.coal)));
    if (n > 0) { m.inv.coal = (m.inv.coal ?? 0) + n; f.store.coal -= n; f.stats.handFed += n; }
    return { kind: 'generator', moved: n, reason: n ? '' : (m.inv.coal ?? 0) >= GENERATOR_COAL_CAP ? 'the Generator is full' : 'no coal in the Depot (mine the coal patch by hand or Excavator)' };
  }
  return null;
}

/** The bot's hands (autoplay only, a dev aid like the bot itself): each frame, hand-feed every turret and Generator at
 *  or under half from the Depot, the way §11's player does in the first minutes. GAME-ASSUMPTION: the bot never builds
 *  a line, so with the flow layer on the HQ's 200 rounds are its whole game unless the harness places one; a soak that
 *  measures the hour places §11's line and lets these hands carry magazines from the Depot to six turrets. */
export function botHands(st: SimState): number {
  const f = st.flow;
  if (!f) return 0;
  let moved = 0;
  for (const m of f.machines) {
    if (m.kind === 'turret' && (m.inv.rounds ?? 0) <= TURRET_HOPPER / 2) moved += handFeed(st, m.x, m.y)?.moved ?? 0;
    else if (m.kind === 'generator' && (m.inv.coal ?? 0) <= GENERATOR_COAL_CAP / 2) moved += handFeed(st, m.x, m.y)?.moved ?? 0;
  }
  return moved;
}

/** Lot tiles on the start lot with a patch: for tests and the renderer. */
export function isDepotTile(lx: number, ly: number): boolean {
  return lx >= DEPOT_LOT && lx < DEPOT_LOT + DEPOT_TILES && ly >= DEPOT_LOT && ly < DEPOT_LOT + DEPOT_TILES;
}

// ------------------------------------------------------------------ world-view drawing (§18, docsync)

/** §18 legend for `renderLot`. */
export const LOT_LEGEND =
  '`:` street, `.` ground, `r` rubble, `#` steel patch, `&` copper patch, `*` coal patch, `~` inert/river; ' +
  '`L` streetlight lit, `l` streetlight dark, `b` streetlight broken; `T` Gun turret (hopper > 0), `t` turret with an empty hopper; ' +
  '`G` Generator burning, `g` Generator dry; `S` substation powered, `s` unpowered; `D` Depot; `X` Excavator running, `x` stopped; ' +
  '`A` Shot assembler crafting, `a` idle; `^ > v <` belt by direction, `I` inserter; `@` Lamp lit, `o` Lamp dark; `P` pole; ' +
  'a machine shed by the brownout is drawn in lowercase.';

/** One character per tile of a cell (default the HQ), the whole 32×32 cell with its street margins, rows ty 0 (north)
 *  down, columns tx: the world view's tile-scale drawing for §18, generated by `npm run docsync` (M3). */
export function renderLot(st: SimState, bx = st.start[0], by = st.start[1]): string {
  const c = cellTiles(st, bx, by);
  const ox = bx * CELL_TILES, oy = by * CELL_TILES;
  const rows: string[][] = [];
  for (let ty = 0; ty < CELL_TILES; ty++) {
    const row: string[] = [];
    for (let tx = 0; tx < CELL_TILES; tx++) {
      const i = ty * CELL_TILES + tx, k = c.kind[i];
      row.push(k === T_STREET ? ':' : k === T_RUBBLE || k === T_DEPOSIT ? 'r' : k === T_INERT || k === T_RIVER ? '~'
        : k === T_PATCH ? (c.patch[i] === P_STEEL ? '#' : c.patch[i] === P_COPPER ? '&' : c.patch[i] === P_COAL ? '*' : '.') : '.');
    }
    rows.push(row);
  }
  const put = (tx: number, ty: number, ch: string) => { if (tx >= 0 && ty >= 0 && tx < CELL_TILES && ty < CELL_TILES) rows[ty][tx] = ch; };
  for (const l of cellLights(st, bx, by)) if (l.kind === 'streetlight') put(l.tx - ox, l.ty - oy, l.broken ? 'b' : l.lit ? 'L' : 'l');
  const sub = substationAt(st, bx, by);
  if (sub) for (let dy = 0; dy < SUBSTATION_TILES; dy++) for (let dx = 0; dx < SUBSTATION_TILES; dx++) put(sub.tx + dx - ox, sub.ty + dy - oy, sub.on ? 'S' : 's');
  const f = st.flow;
  if (f) for (const m of f.machines) {
    let ch: string;
    switch (m.kind) {
      case 'depot': ch = 'D'; break;
      case 'turret': ch = (m.inv.rounds ?? 0) > 0 ? 'T' : 't'; break;
      case 'generator': ch = m.busy ? 'G' : 'g'; break;
      case 'excavator': ch = running(st, m) && !m.shed ? 'X' : 'x'; break;
      case 'assembler': ch = m.busy && running(st, m) ? 'A' : 'a'; break;
      case 'belt': ch = '^>v<'[m.dir]; break;
      case 'inserter': ch = m.shed ? 'i' : 'I'; break;
      case 'lamp': ch = running(st, m) ? '@' : 'o'; break;
      case 'pole': ch = 'P'; break;
    }
    if (m.shed) ch = ch.toLowerCase();
    for (let dy = 0; dy < m.size; dy++) for (let dx = 0; dx < m.size; dx++) put(m.x + dx - ox, m.y + dy - oy, ch);
  }
  const head = ' col: ' + Array.from({ length: CELL_TILES }, (_, i) => String(i % 10)).join(' ');
  return [head, ...rows.map((r, ty) => `${String(ty).padStart(4)}  ${r.join(' ')}`)].join('\n');
}
