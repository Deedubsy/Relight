/** Tile-level flow (constitution Phase 4 M2 + M3): Excavator, belts, inserters, the Depot, the Mk1 Shot assembler,
 *  hand-mining and hand-crafting (M2); Gun turrets, Lamps, poles, Generators, the lot substations and streetlights,
 *  and power as one number with the §14 proportional brownout (M3, D-B3-4), ticking at a fixed 20 ticks/s. The block map stays the judge of territory: a
 *  machine runs only while its block is Held, digging draws the same pools the block economy draws, magazines
 *  reach the same line buffer the ring hoppers fill from, and one block tick (`step`) closes every 20 tile ticks.
 *
 *  Nothing here exists until `ensureFlow` is called (the game does; the harness and the fixtures do not), so a
 *  state without a flow layer behaves exactly as before M2.
 *
 *  Rates (all measured by test/flow.test.ts, run name M2-rates): Excavator 0.5 items/s onto the tile it faces,
 *  belt 7.5 items/s (4 items a tile at 1.875 tiles/s), inserter 1 item/s, Mk1 Shot assembler 6 s a magazine (10/min, D-P4-4). */
import { SimState, Block, HELD, CONTESTED, DARK, INERT, Command, Engineer } from './types';
import { idxOf, step, applyCommands, tileHooks, effectiveSupply, syncEdges as rebuildRing, burnOffS } from './sim';
import { edgeId, edgeFrom, edgeTo } from './graph';
import { RECIPES, START_COAL, COAL_MJ, GENERATOR_KW, TURRET_HOPPER, TURRET_RANGE, TURRET_ROUNDS_PER_S, LAMP_KW, LAMP_RADIUS, POLE_REACH, FLOODLIGHT_KW, FLOODLIGHT_RANGE, FLOODLIGHT_HALF_ANGLE, BIG_POLE_REACH } from './recipes';
import { BELT_PER_S, EXCAVATOR_PER_S, INSERTER_PER_S, START_CHEST, START_TURRETS, STREETLIGHT_RADIUS } from './constants';
import {
  CELL_TILES, P_STEEL, P_COPPER, P_COAL, HQ_PATCHES, DEPOT_LOT, DEPOT_TILES, SUBSTATION_TILES,
  T_STREET, T_RUBBLE, T_INERT, T_RIVER, T_DEPOSIT, T_PATCH,
  cellTiles,
} from './tiles';
import { ground, inGround, hqLot, blockOfTile, goneOf, poolCap, substationOwner, blocksNear, inReach, cityGeomOf, segAxis, segLength } from './ground';
import { tickEngineerTiles, workbenchTile } from './walk';
import { take as pocketTake, drop as pocketDrop, invStacks, invCap, handHook, hqIdx as hqIndex, upgradeEngineer, threatHooks } from './engineer';
import type { ThreatState } from './threat';
import { segBetween, frontTiles, CitySeg } from './city';

export const TILE_TPS = 20;                    // constitution: fixed 20 ticks/s at tile level
export const TILE_DT = 1 / TILE_TPS;
/** float slack on second-counting timers (60 × 0.05 does not sum to 3 exactly) */
const EPS = 1e-9;
export type Dir = 0 | 1 | 2 | 3;               // N E S W
export const DX = [0, 1, 0, -1], DY = [-1, 0, 1, 0];
export const DIR_NAMES = ['north', 'east', 'south', 'west'];
export type Kind = 'excavator' | 'belt' | 'inserter' | 'assembler' | 'depot' | 'turret' | 'lamp' | 'pole' | 'generator' | 'floodlight' | 'bigpole' | 'substation';
export const KINDS: readonly Kind[] = ['excavator', 'belt', 'inserter', 'assembler', 'depot', 'turret', 'lamp', 'pole', 'generator', 'floodlight', 'bigpole', 'substation'];
/** Prompt B M3 (run name B-M3-unlocks): the Electricians' unlocks (§8, §13). GAME-ASSUMPTION: a group's unlocks land
 *  on the toolbar when its block turns Held and stay if the block later falls (§11: "the Electricians walk into the
 *  Depot"); the other groups' unlocks are not in the slice. */
export const SURVIVOR_UNLOCKS: Record<string, readonly Kind[]> = { Electricians: ['floodlight', 'bigpole', 'substation'] };
export const KIND_LABEL: Record<Kind, string> = {
  excavator: 'Excavator', belt: 'belt', inserter: 'inserter', assembler: 'Shot assembler', depot: 'Depot', turret: 'Gun turret', lamp: 'Lamp',
  pole: 'pole', generator: 'Generator', floodlight: 'Floodlight', bigpole: 'Big pole', substation: 'Substation',
};
export function unlockedBy(kind: Kind): string | null {
  for (const [name, ks] of Object.entries(SURVIVOR_UNLOCKS)) if (ks.includes(kind)) return name;
  return null;
}
/** Has a survivor group joined: its block Held now, or Held once and since lost (`st.fallen`). */
export function survivorJoined(st: SimState, name: string): boolean {
  for (const sv of st.survivors) {
    if (sv.name !== name) continue;
    const i = idxOf(st, sv.x, sv.y);
    if (i >= 0 && (st.blocks[i].state === HELD || st.fallen[i])) return true;
  }
  return false;
}
/** Why a kind cannot be placed yet ('' when it can): the group that unlocks it has not joined. */
export function lockReason(st: SimState, kind: Kind): string {
  const who = unlockedBy(kind);
  return who && !survivorJoined(st, who) ? `the ${who} unlock it — hold their block` : '';
}
export function unlockedKinds(st: SimState): Kind[] { return KINDS.filter(k => !lockReason(st, k)); }
export function isKind(s: string): s is Kind { return (KINDS as readonly string[]).includes(s); }
/** Flow directions (N E S W) to the block sim's edge directions (+x −x +y −y) and back. */
export const SIM_DIR: readonly number[] = [3, 0, 2, 1], FLOW_DIR: readonly Dir[] = [1, 3, 2, 0];
export const SIM_DIR_NAMES = ['east', 'west', 'south', 'north'];
export type Item = 'steel' | 'copper' | 'stone' | 'coal' | 'magazine';

/** Constitution M2: belts carry 7.5 items/s (§13/§14 said 8; D-P4-6). GAME-ASSUMPTION: four items a tile, so the
 *  belt moves 1.875 tiles/s; belts are one lane, a side feed joins at the tile's start like a corner. */
export { BELT_PER_S, EXCAVATOR_PER_S, INSERTER_PER_S, TURRET_PER_TILES, START_CHEST, START_TURRETS } from './constants';
export const BELT_SPACING = 0.25, BELT_SPEED = BELT_PER_S * BELT_SPACING;
/** §13: Excavator 3×3, mines the 5×5 under and around it at 0.5/s. */
/** §13: inserter 1 item/s. GAME-ASSUMPTION: half a second each way; it waits with the item if the target is full. */
export const INSERTER_SWING = 0.5 / INSERTER_PER_S;
export const SHOT = RECIPES.find(r => r.name === 'Shot magazine')!;
/** GAME-ASSUMPTION: an assembler holds four crafts' worth of each input and five finished magazines, then stops. */
export const ASM_INPUT_MULT = 4, ASM_OUTPUT_CAP = 5;
/** GAME-ASSUMPTION: hand-mining takes one unit a second straight into the Depot; hand-crafting a magazine takes the
 *  recipe's 3 s and its 2 steel + 1 Cu from the stock (§11 hand-feeds the first turrets). */
export const HAND_MINE_PER_S = 1;
export const MACHINE_SIZE: Record<Kind, number> = { excavator: 3, belt: 1, inserter: 1, assembler: 3, depot: DEPOT_TILES, turret: 2, lamp: 1, pole: 1, generator: 2, floodlight: 2, bigpole: 2, substation: SUBSTATION_TILES };
/** GAME-ASSUMPTION: machine costs in rubble (§13 gives none). The assembler costs what the block-level one does
 *  (§12: 20 Cu + 40 steel); the Excavator 10 steel; a belt tile 1 steel; an inserter 1 steel + 1 Cu; (M3) a Gun
 *  turret 15 steel + 5 Cu, a Lamp and a pole 1 steel + 1 Cu each, a Generator 30 steel + 10 Cu. Pick-up returns the machine itself
 *  to the pockets (prompt B M2), never a rubble refund. Prompt B M3, the Electricians' unlocks: the craftable Substation is
 *  §13's 20 frames + 20 wire + 10 boards paid as the raw rubble those recipes take (50 steel + 25 Cu; frames, wire and
 *  boards are Phase 5 items); a Floodlight 10 steel + 5 Cu and a Big pole 4 steel + 4 Cu are GAME-ASSUMPTIONS like the rest. */
export const MACHINE_COST: Record<Kind, { steel: number; copper: number }> = {
  excavator: { steel: 10, copper: 0 }, belt: { steel: 1, copper: 0 }, inserter: { steel: 1, copper: 1 },
  assembler: { steel: 40, copper: 20 }, depot: { steel: 0, copper: 0 },
  turret: { steel: 15, copper: 5 }, lamp: { steel: 1, copper: 1 }, pole: { steel: 1, copper: 1 }, generator: { steel: 30, copper: 10 },
  floodlight: { steel: 10, copper: 5 }, bigpole: { steel: 4, copper: 4 }, substation: { steel: 50, copper: 25 },
};
/** §13 power draw (kW). A machine with a draw runs only on a powered cell (M3); belts, turrets, poles and Generators
 *  draw nothing. GAME-ASSUMPTION: a placed machine draws its rated kW whether busy or idle (§13 has no idle draw). */
export const MACHINE_KW: Record<Kind, number> = { excavator: 60, belt: 0, inserter: 10, assembler: 100, depot: 0, turret: 0, lamp: LAMP_KW, pole: 0, generator: 0, floodlight: FLOODLIGHT_KW, bigpole: 0, substation: 0 };
/** GAME-ASSUMPTION: a Generator holds 50 coal (the §11 start's 40 fit); a turret's muzzle flash lasts half a second. */
export const GENERATOR_COAL_CAP = 50, TURRET_FLASH_S = 0.5;

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
  /** Block index → tiles (global index ty * tw + tx) dug out by machines or hands. */
  dug: Record<number, number[]>;
  /** "block:tile" → units left in a partly dug tile. */
  units: Record<string, number>;
  /** Items with no home in the block sim's stock. §11: the start's 40 coal (the Generator is M3). */
  store: { coal: number };
  /** M1 (prompt B): `full` is raised when the pockets refused a mined unit (the game toasts it and clears it). */
  hand: { mine: [number, number] | null; prog: number; crafts: number; crafting: boolean; craftProg: number; full: boolean;
          /** M4 telemetry: the engineer has been out of the Depot's reach since the last chest transaction (a "trip"). */
          away: boolean };
  stats: { magsMade: number; magsDelivered: number; mined: number; handMined: number; handCrafted: number; delivered: Record<Item, number>;
           /** M3: rounds the turrets fired, coal the Generators burned, magazines and coal fed by hand. */
           fired: number; coalBurned: number; handFed: number;
           /** M4 telemetry (§19): trips to the chest, placements the reach refused. */
           chestTrips: number; reachRefused: number };
  /** M3: commands the tile layer raises for the block map (a pole run reaching a Dark block's substation claims it). */
  pending: Command[];
  /** M3: the grid as of the last block tick — kW supplied, demanded, delivered; seconds demand exceeded supply. */
  /** §14 as one number: kW the Generators give, kW asked, kW carried, seconds short so far, and the D-B3-4 throttle
   *  (supply ÷ demand, 1 when covered) every drawing machine runs at this second. */
  power: { supply: number; demand: number; load: number; overS: number; throttle: number };
  /** M4: the tile threat (threat.ts): crawlers, eaten lights, hand-fired engagements. Created on first use. */
  threat?: ThreatState;
  /** M5: streetlights the engineer repaired (global tile index) — the §13 3-in-8 broken ones, once E has been on them
   *  with copper in the pockets. Eaten lights (`threat.broken`) are repaired by leaving that list. Created on first use. */
  repaired?: number[];
  /** M5 telemetry: lights repaired by hand (both kinds). */
  repairs?: number;
}

/** The flow layer, created on first use: the Depot goes on the start lot, the block-level Mk1 stand-in retires
 *  (§12 C9: Phase 4 places the real machine), and from here the HQ patch is dug by machines and hands, not drained. */
export function ensureFlow(st: SimState): FlowState {
  upgradeEngineer(st.engineer);   // D-B1-5: a snapshot from before the body fields
  if (st.flow) return upgrade(st.flow);
  const f: FlowState = {
    version: 1, tick: 0, next: 1, rev: 0, tw: ground(st).tw, machines: [], occ: {}, dug: {}, units: {},
    store: { coal: 0 },
    hand: { mine: null, prog: 0, crafts: 0, crafting: false, craftProg: 0, full: false, away: false },
    stats: { magsMade: 0, magsDelivered: 0, mined: 0, handMined: 0, handCrafted: 0, delivered: { steel: 0, copper: 0, stone: 0, coal: 0, magazine: 0 },
             fired: 0, coalBurned: 0, handFed: 0, chestTrips: 0, reachRefused: 0 },
    pending: [],
    power: { supply: 0, demand: 0, load: 0, overS: 0, throttle: 1 },
  };
  st.flow = f;
  st.acc = 0;
  const [sx, sy] = st.start;
  const hq = st.blocks[idxOf(st, sx, sy)];
  if (hq.machines > 0) { hq.machines = 0; st.asmManual = Math.max(0, st.asmManual - st.config.startAssemblers); }
  const lot = (lx: number, ly: number): [number, number] => hqLot(st, lx, ly);   // the HQ lot's 24×24 frame (ground.ts)
  addMachine(st, 'depot', ...lot(DEPOT_LOT, DEPOT_LOT), 0);
  // D-B1-4: on a city the start turrets are placed by the HQ's face geometry — per street segment, one per 16 tiles
  // of its length, at least one (§5), on the buildable lot tiles nearest evenly spaced points along the segment's
  // ridge, each within the turret's range of its point and facing its street. Every live segment is covered, so no
  // edge of the HQ is a stand-in and the HQ holds without a special case (M3's six fixed lot spots left seed 3's
  // fourth segment uncovered and the HQ fell at minute 20). The lattice keeps M3's six (two a side on its three
  // street sides, the north pair at the §18 sketch's lot columns 3 and 16) — the lattice fixtures are its record.
  if (st.lattice) for (const [lx, ly, d] of [[3, 0, 0], [16, 0, 0], [0, 3, 3], [0, 16, 3], [22, 3, 1], [22, 16, 1]] as [number, number, Dir][]) addMachine(st, 'turret', ...lot(lx, ly), d);
  else startTurrets(st);
  // §11: one Generator (300 kW) with 40 coal. GAME-ASSUMPTION: the 40 coal is in the Generator's hopper, not the
  // Depot (§18: "coal by hand, 40 left"); it stands at lot (19,8), where the §18 sketch draws it.
  const g = addMachine(st, 'generator', ...lot(19, 8), 0);
  g.inv.coal = START_COAL;
  prefillTurrets(st);
  // Prompt B M1: the chest (the Depot) holds §11's start stock once the start turrets are stocked, and the engineer
  // starts at the workbench. GAME-ASSUMPTION: the block sim keeps its calibrated start (80/40/0, config 5f3417b9)
  // for the harness; the game's chest is §11's 200 steel, 100 copper, 50 stone, 40 coal and 20 magazines (D-P4-4/7/8).
  // (the 20 magazines are the line buffer `prefillTurrets` leaves after the six hoppers are filled)
  st.stock.steel = START_CHEST.steel; st.stock.copper = START_CHEST.copper; st.stock.stone = START_CHEST.stone;
  f.store.coal = START_CHEST.coal;
  const [wx, wy] = workbenchTile(st);
  st.engineer.x = wx + 0.5; st.engineer.y = wy + 0.5; st.engineer.block = hqIndex(st); st.engineer.dest = -1; st.engineer.remaining = 0;
  return f;
}

/** D-P4-8: six start turrets, spread over the HQ's live street segments in proportion to their lengths (`segLength`,
 *  largest remainder), at least one a segment — the same count on every seed, so §11's 0–10 is one script.
 *  (D-B1-4's one per 16 tiles gave 5–7 by seed; `TURRET_PER_TILES` stays as the doc's rule of thumb for the count.)
 *  START_TURRETS and TURRET_PER_TILES live in constants.ts. */
/** Share `total` among weights by largest remainder, at least `min` each. */
export function apportion(weights: number[], total: number, min = 1): number[] {
  const sum = weights.reduce((a, b) => a + b, 0) || 1;
  const exact = weights.map(w => w / sum * total), out = exact.map(x => Math.max(min, Math.floor(x)));
  let left = total - out.reduce((a, b) => a + b, 0);
  const order = exact.map((x, i) => [x - Math.floor(x), i] as [number, number]).sort((a, b) => b[0] - a[0]);
  for (const [, i] of order) { if (left <= 0) break; out[i]++; left--; }
  return out;
}

/** D-B1-4: which of a face's street segments a 2×2 at (x, y) serves — the segment whose front ring (`frontTiles`,
 *  the lot tiles that border that street) holds most of the footprint; ties and footprints off every ring go to the
 *  segment with the nearest ridge tile within TURRET_RANGE of the centre. Returns the neighbour index or -1.
 *  The ring rule is what lets a corner sliver (a 7-tile segment with a 2-tile front) be covered at all. */
export function faceSegOf(st: SimState, bi: number, x: number, y: number, size: number): number {
  const cg = cityGeomOf(st), tw = cg.tw, cx = x + size / 2, cy = y + size / 2;
  let best = -1, bestN = 0, bestD = TURRET_RANGE;
  for (const j of st.nb[bi]) {
    const sg = segBetween(cg, bi, j);
    if (!sg) continue;
    const front = frontTiles(cg, bi, j);
    let n = 0;
    for (let dy = 0; dy < size; dy++) for (let dx = 0; dx < size; dx++) { const t = (y + dy) * tw + x + dx; for (let k = 0; k < front.length; k++) if (front[k] === t) { n++; break; } }
    let d = Infinity;
    for (let k = 0; k < sg.ridge.length; k++) { const t = sg.ridge[k], tx = t % tw, ty = (t - tx) / tw, dd = Math.hypot(tx + 0.5 - cx, ty + 0.5 - cy); if (dd < d) d = dd; }
    if (n > bestN || (n === bestN && d < bestD)) { best = j; bestN = n; bestD = d; }
  }
  return best;
}

/** D-B1-4: the HQ's start turrets by its segments' lengths. For each segment to a live neighbour, `n` target points
 *  are spread evenly along its front ring (`frontTiles`, ordered along the segment's axis) and each turret goes on
 *  the placeable 2×2 nearest its point that serves this segment (`faceSegOf`) and has the segment's ridge in range,
 *  facing the ridge. A segment no turret reaches afterwards (a corner sliver whose front cannot hold a 2×2) gets one
 *  more on the placeable 2×2 nearest its front that reaches its ridge — "at least one a segment" by reach.
 *  Returns the turrets serving each HQ edge id. */
export function startTurrets(st: SimState): Map<number, number> {
  const cg = cityGeomOf(st), tw = cg.tw, hq = cg.hq, out = new Map<number, number>();
  const lotTiles = cg.blocks[hq].tiles;
  const segs = st.nb[hq].map(j => ({ j, sg: segBetween(cg, hq, j)! })).filter(x => x.sg && st.blocks[x.j].state !== INERT);
  const ridgeDist = (sg: CitySeg, cx: number, cy: number): number => {
    let d = Infinity;
    for (let k = 0; k < sg.ridge.length; k++) { const t = sg.ridge[k], tx = t % tw, ty = (t - tx) / tw, dd = Math.hypot(tx + 0.5 - cx, ty + 0.5 - cy); if (dd < d) d = dd; }
    return d;
  };
  const placeOne = (sg: CitySeg, j: number, px: number, py: number, mustServe: boolean): boolean => {
    // clear ground first; failing that, a rubble pad (GAME-ASSUMPTION: the HQ's turret pads were laid before the
    // rubble fell, so a start turret with no clear tile for its point stands on rubble it clears)
    let best: [number, number] | null = null;
    for (const onRubble of [false, true]) {
      let bd = Infinity;
      for (let q = 0; q < lotTiles.length; q++) {
        const t = lotTiles[q], tx = t % tw, ty = (t - tx) / tw, cx = tx + 1, cy = ty + 1, d = Math.hypot(cx - px, cy - py);
        if (d >= bd || ridgeDist(sg, cx, cy) >= TURRET_RANGE || (mustServe && faceSegOf(st, hq, tx, ty, MACHINE_SIZE.turret) !== j)) continue;
        const why = placeable(st, 'turret', tx, ty);
        if (why && !(onRubble && why === 'rubble in the way')) continue;
        bd = d; best = [tx, ty];
      }
      if (best) break;
    }
    if (!best) return false;
    for (let y = best[1]; y < best[1] + MACHINE_SIZE.turret; y++) for (let x = best[0]; x < best[0] + MACHINE_SIZE.turret; x++) if (rubbleAt(st, x, y)) (st.flow!.dug[hq] ??= []).push(y * tw + x);
    const cx = best[0] + 1, cy = best[1] + 1;
    let nx = sg.mx, ny = sg.my, nd = Infinity;   // face the nearest ridge tile
    for (let k = 0; k < sg.ridge.length; k++) { const t = sg.ridge[k], tx = t % tw, ty = (t - tx) / tw, d = Math.hypot(tx + 0.5 - cx, ty + 0.5 - cy); if (d < nd) { nd = d; nx = tx + 0.5; ny = ty + 0.5; } }
    const dx = nx - cx, dy = ny - cy;
    const dir: Dir = Math.abs(dx) >= Math.abs(dy) ? (dx > 0 ? 1 : 3) : (dy > 0 ? 2 : 0);
    addMachine(st, 'turret', best[0], best[1], dir);
    return true;
  };
  const counts = apportion(segs.map(x => segLength(x.sg, tw)), START_TURRETS);
  for (const [k, { j, sg }] of segs.entries()) {
    const [ux, uy] = segAxis(sg, tw);
    const front = Array.from(frontTiles(cg, hq, j)).map(t => { const tx = t % tw, ty = (t - tx) / tw; return { tx, ty, s: (tx - sg.mx) * ux + (ty - sg.my) * uy }; }).sort((a, b) => a.s - b.s);
    if (!front.length) continue;
    const lo = front[0].s, hi = front[front.length - 1].s, n = counts[k];
    for (let k = 0; k < n; k++) {
      const want = lo + (k + 0.5) / n * (hi - lo);
      let p = front[0];
      for (const f of front) if (Math.abs(f.s - want) < Math.abs(p.s - want)) p = f;
      placeOne(sg, j, p.tx + 0.5, p.ty + 0.5, true);
    }
  }
  const f = st.flow!;
  for (const { j, sg } of segs) {
    const reach = f.machines.filter(m => m.kind === 'turret' && ridgeDist(sg, m.x + m.size / 2, m.y + m.size / 2) < TURRET_RANGE).length;
    if (reach === 0) {
      const fr = frontTiles(cg, hq, j);
      let px = 0, py = 0;
      for (let k = 0; k < fr.length; k++) { const t = fr[k]; px += t % tw + 0.5; py += Math.floor(t / tw) + 0.5; }
      if (fr.length) placeOne(sg, j, px / fr.length, py / fr.length, false);
    }
  }
  // the count is START_TURRETS on every seed (D-P4-8): a turret its segment could not hold (a corner sliver with no
  // 2×2 of its own, and no turret of its own placed by reach) goes to the longest segment, on the front tile
  // farthest from the turrets already standing there
  const short = START_TURRETS - f.machines.filter(m => m.kind === 'turret').length;
  if (short > 0 && segs.length) {
    const longest = segs.reduce((a, b) => segLength(b.sg, tw) > segLength(a.sg, tw) ? b : a);
    const front = Array.from(frontTiles(cg, hq, longest.j)).map(t => { const tx = t % tw, ty = (t - tx) / tw; return { tx, ty }; });
    for (let k = 0; k < short && front.length; k++) {
      const tur = f.machines.filter(m => m.kind === 'turret');
      let p = front[0], pd = -1;
      for (const q of front) { let d = Infinity; for (const m of tur) d = Math.min(d, Math.hypot(m.x + 1 - q.tx - 0.5, m.y + 1 - q.ty - 0.5)); if (d > pd) { pd = d; p = q; } }
      if (!placeOne(longest.sg, longest.j, p.tx + 0.5, p.ty + 0.5, true)) break;
    }
  }
  for (const { j, sg } of segs) out.set(edgeId(st, hq, j), f.machines.filter(m => m.kind === 'turret' && ridgeDist(sg, m.x + m.size / 2, m.y + m.size / 2) < TURRET_RANGE).length);
  return out;
}

/** §11: what the chest holds at the start — START_CHEST in constants.ts (D-P4-4: 200 steel stays; D-P4-7: 40 coal;
 *  D-P4-8: 20 magazines on top of six full turret hoppers). */

/** A flow layer saved before M3 gets the M3 fields. */
function upgrade(f: FlowState): FlowState {
  f.pending ??= []; f.power ??= { supply: 0, demand: 0, load: 0, overS: 0, throttle: 1 }; f.power.throttle ??= 1;
  f.stats.fired ??= 0; f.stats.coalBurned ??= 0; f.stats.handFed ??= 0;
  f.hand.full ??= false; f.hand.away ??= false;
  f.stats.chestTrips ??= 0; f.stats.reachRefused ??= 0;
  for (const m of f.machines) delete (m as { shed?: boolean }).shed;   // pre-D-B3-4 snapshots carried a shed flag
  return f;
}

/** D-P4-8: the HQ's start turrets stand with full hoppers (six × 50 rounds) and the chest holds 20 magazines on top —
 *  the block sim's start buffer (config.startRounds, C10's 20 magazines) becomes the chest's 20 and the hoppers are
 *  filled outright, so the first red pip is the ~6-minute hand-feed beat §11 is written around, not a 0:00 shortfall.
 *  (Before: the 20 magazines went round the ring in order, two edges full and the third empty.) A lattice or
 *  block-only snapshot's stand-in hoppers on the HQ are filled the same way. */
function prefillTurrets(st: SimState): void {
  const f = st.flow!;
  rebuildRing(st);
  const hqIdx = idxOf(st, st.start[0], st.start[1]);
  for (const e of st.ring) if (e.a === hqIdx) e.hopper = 0;
  hookSyncEdges(st);
  for (const m of f.machines) if (m.kind === 'turret') m.inv.rounds = TURRET_HOPPER;
  st.buffer = Math.min(st.config.bufferCap, START_CHEST.magazines * SHOT.count);
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
  if (!f || !inGround(ground(st), tx, ty)) return undefined;
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
/** The block a machine belongs to: the face (or lattice lot) it stands on, or whose street it stands on. */
function blockOf(st: SimState, m: Machine): Block {
  const i = blockOfTile(st, m.x, m.y);
  return st.blocks[i < 0 ? hqIndex(st) : i];
}
function blockIdxOf(st: SimState, m: Machine): number { return blockOfTile(st, m.x, m.y); }
/** §14: each substation powers its whole cell. It is powered when its block is claimed (Held or Contested), the
 *  block sim has not switched it off (unfed, shade), and the grid has any supply at all. GAME-ASSUMPTION: a
 *  grid with no Generator burning is dead, not browned out — everything on it stops at once. */
export function subPowered(st: SimState, b: Block): boolean {
  if (b.state !== HELD && b.state !== CONTESTED) return false;
  if (!b.subOn || st.t < b.shadeOff) return false;
  if (st.config.power && effectiveSupply(st) <= 0) return false;
  return true;
}
/** A machine with a draw runs while its cell is powered. D-B3-4: power never switches a machine off; short of supply
 *  every machine runs at `flow.power.throttle` (see `stepFlow`). */
export function powered(st: SimState, m: Machine): boolean {
  if (MACHINE_KW[m.kind] === 0) return true;
  return subPowered(st, blockOf(st, m));
}
/** D-B3-4: the speed every drawing machine runs at this second (supply ÷ demand, 1 when the grid is covered). */
export function throttle(st: SimState): number {
  return st.config.power && st.flow ? st.flow.power.throttle : 1;
}
function running(st: SimState, m: Machine): boolean {
  return blockIdxOf(st, m) >= 0 && blockOf(st, m).state === HELD && powered(st, m);
}
function stopReason(st: SimState, m: Machine): string {
  if (blockIdxOf(st, m) < 0 || blockOf(st, m).state !== HELD) return ' · stopped (block not Held)';
  if (MACHINE_KW[m.kind] === 0) return '';
  if (!subPowered(st, blockOf(st, m))) return ' · no power';
  if (throttle(st) < 1 - 1e-9) return ` · at ${Math.round(throttle(st) * 100)} % (brownout)`;
  return '';
}

// ------------------------------------------------------------------ rubble under a tile

export interface TileRubble { type: Item; units: number; bi: number; tile: number; patch: number }

/** What a tile holds for digging: an HQ patch tile (steel, copper, coal) or a standing district rubble tile, with
 *  the units left in it. Null for ground, street, a dug tile, or a tile the pool says is already gone. */
export function rubbleAt(st: SimState, tx: number, ty: number): TileRubble | null {
  const f = st.flow;
  if (!f) return null;
  const G = ground(st);
  if (!inGround(G, tx, ty)) return null;
  const t = ty * G.tw + tx, bi = G.owner[t];
  if (bi < 0) return null;
  const b = st.blocks[bi];
  if (b.state !== HELD) return null;
  const dug = f.dug[bi];
  if (dug && dug.includes(t)) return null;
  const key = `${bi}:${t}`;
  const p = G.patch[t];
  if (p !== 0) {
    const spec = HQ_PATCHES.find(q => q.type === p)!;
    const units = f.units[key] ?? (p === P_STEEL ? Math.min(spec.units, st.patch.steel) : spec.units);
    if (units <= 0) return null;
    return { type: p === P_STEEL ? 'steel' : p === P_COPPER ? 'copper' : 'coal', units, bi, tile: t, patch: p };
  }
  const bg = G.blocks[bi];
  if (!bg.rubble) return null;
  const r = G.rank[t];
  if (r < 0 || r < goneOf(st, bi)) return null;
  const cap = poolCap(st, b);
  const units = f.units[key] ?? (cap > 0 ? cap / bg.count : 1);
  if (units <= 0) return null;
  return { type: bg.rubble, units, bi, tile: t, patch: 0 };
}

/** Take one unit out of a tile: the block's pool (or the HQ steel patch) drops with it; an emptied tile is dug. */
function mineUnit(st: SimState, r: TileRubble): Item {
  const f = st.flow!;
  const key = `${r.bi}:${r.tile}`;
  const left = r.units - 1;
  const b = st.blocks[r.bi];
  if (r.patch === P_STEEL) st.patch.steel = Math.max(0, st.patch.steel - 1);
  else if (r.patch === 0) b.pool = Math.max(0, b.pool - 1);
  if (left <= 1e-9) {
    delete f.units[key];
    (f.dug[r.bi] ??= []).push(r.tile);
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
  if (!h.away && !nearDepot(st)) h.away = true;   // M4 telemetry: the next chest transaction is a trip
  if (h.mine) {
    const r = rubbleAt(st, h.mine[0], h.mine[1]);
    if (!r || !inReach(st, h.mine[0], h.mine[1])) { h.mine = null; h.prog = 0; }   // dug out, or walked away (D5: reach 8)
    else {
      h.prog += dt * HAND_MINE_PER_S;
      if (h.prog >= 1 - EPS) {
        // Prompt B M1: hand-mined units go to the pockets (engineer.ts stacks), never straight to the Depot; full
        // pockets stop the hands with the tile untouched.
        if (pocketTake(st.engineer, r.type, 1) === 0) { h.mine = null; h.prog = 0; h.full = true; }
        else { h.prog -= 1; mineUnit(st, r); f.stats.handMined++; }
      }
    }
  }
  // Prompt B M2: hand-crafting at the workbench, from the pockets, into the pockets (§14, §13's Workbench row).
  // GAME-ASSUMPTION: the craft pauses with its progress kept while the engineer is out of reach of the Depot (the
  // workbench is on its lot) or while the pockets cannot take the magazine; a queue the pockets cannot pay for is
  // dropped (`queueCraft` refuses it up front) rather than left waiting.
  if (h.crafts > 0) {
    if (!nearDepot(st)) return;
    const e = st.engineer;
    if (!h.crafting) {
      if ((e.inv.steel ?? 0) >= SHOT.inputs.steel && (e.inv.copper ?? 0) >= SHOT.inputs.copper) {
        pocketDrop(e, 'steel', SHOT.inputs.steel); pocketDrop(e, 'copper', SHOT.inputs.copper); h.crafting = true; h.craftProg = 0;
      } else { h.crafts = 0; return; }
    }
    h.craftProg += dt;
    if (h.craftProg >= SHOT.seconds - EPS && pocketTake(e, 'magazine', 1) === 1) {
      h.crafting = false; h.crafts--; h.craftProg = 0; f.stats.handCrafted++; st.stats.magsMade++;
    } else if (h.craftProg >= SHOT.seconds - EPS) h.full = true;
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
  tickEngineerTiles(st, dt);   // D5: the engineer moves (and lays kits) ahead of the machines and the block tick
  threatHooks.current?.tick(st, dt);   // M4: crawlers walk, turrets and the bots' rifle shoot, arrivals count
  for (const m of beltOrder(st, f)) if (running(st, m)) tickBelt(st, m, dt);
  let gens = 0;
  for (const m of f.machines) if (m.kind === 'generator' && (m.inv.coal ?? 0) > 0 && running(st, m)) gens++;
  const share = gens ? f.power.load / gens : 0;
  // D-B3-4: short of supply every drawing machine runs at supply ÷ demand — its clock runs that much slower.
  // GAME-ASSUMPTION: a Lamp or Floodlight cannot run slower; it stays lit at any throttle above zero and goes dark
  // only on a dead grid (no Generator burning). Belts and turrets draw nothing and are never slowed.
  const thr = st.config.power ? f.power.throttle : 1, mdt = dt * thr;
  for (const m of f.machines) {
    if (m.kind === 'turret') { m.timer = Math.max(0, m.timer - dt); continue; }
    if (m.kind === 'generator') { if (running(st, m)) tickGenerator(st, m, dt, share); continue; }
    if (m.kind === 'belt' || m.kind === 'depot' || m.kind === 'lamp' || m.kind === 'pole' || m.kind === 'floodlight' || m.kind === 'bigpole' || m.kind === 'substation' || !running(st, m)) continue;
    if (m.kind === 'inserter') tickInserter(st, m, mdt);
    else if (m.kind === 'excavator') tickExcavator(st, m, mdt);
    else tickAssembler(st, m, mdt);
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

export interface PlaceCheck {
  ok: boolean; reason: string;
  /** What the placement takes from the pockets: the rubble price, or nothing when a carried machine goes down. */
  cost: { steel: number; copper: number };
  /** M2: a carried machine (picked up earlier) is placed as it is; `cost` is then zero. */
  carried: boolean;
}
export const costStr = (c: { steel: number; copper: number }): string => c.steel || c.copper ? `${c.steel} steel${c.copper ? ` + ${c.copper} Cu` : ''}` : 'nothing';

/** GAME-ASSUMPTION: a machine goes on any tile of a Held block's cell (lot or its street margin: §14 lets belts run
 *  on streets; an inserter is belt furniture and may too; excavators, assemblers and Generators stay on the lot;
 *  (M3) turrets and Lamps may stand on the street too — §14 keeps room for "a lamp line" there; a pole goes on any
 *  street tile of the city and on any lot that is not Inert — Dark included, since a pole run is how the crew
 *  strings a claim ahead of itself),
 *  never on a tile another machine or the lot's substation holds, and only the Excavator may stand on rubble (it digs
 *  what it stands on) — except posts: a pole or a Lamp is thin enough to stand among rubble. */
export function placeable(st: SimState, kind: Kind, tx: number, ty: number): string {
  tx = Math.floor(tx); ty = Math.floor(ty);
  const f = ensureFlow(st), G = ground(st), size = MACHINE_SIZE[kind];
  const lock = lockReason(st, kind);
  if (lock) return lock;
  // posts (poles, Lamps, Big poles) stand in rubble; pole-likes may also stand on the street and on claimed or Dark blocks
  const post = kind === 'pole' || kind === 'lamp' || kind === 'bigpole', polelike = kind === 'pole' || kind === 'bigpole';
  for (let y = ty; y < ty + size; y++) for (let x = tx; x < tx + size; x++) {
    if (!inGround(G, x, y)) return 'outside the city';
    const t = y * G.tw + x, o = G.owner[t];
    if (o === -2) return 'in the river';
    const margin = o === -1, bi = margin ? G.near[t] : o;
    if (bi < 0) return 'outside the city';
    const b = st.blocks[bi];
    if (b.state !== HELD && !(polelike && (margin || b.state === CONTESTED || b.state === DARK))) return 'the block is not Held';
    if (f.occ[t] !== undefined) return 'another machine is there';
    if (margin && kind !== 'belt' && kind !== 'inserter' && kind !== 'turret' && kind !== 'floodlight' && !post) return 'not on the street';
    // prompt B M3: a craftable Substation goes on a face that has none (§7: the outskirts), on the lot, one a face
    if (kind === 'substation' && faceSub(st, bi)) return 'the face has a substation';
    if (!margin && substationOwner(st, x, y) >= 0) return 'the substation is there';
    if (kind !== 'excavator' && kind !== 'depot' && !post && rubbleAt(st, x, y)) return 'rubble in the way';
  }
  return '';
}
/** Prompt B M2 (run name B-M2-pockets): a machine is placed from the engineer's pockets (§14). A carried machine (one
 *  stack each, engineer.ts `stackSize`) goes down as it is; otherwise its rubble price (`MACHINE_COST`) is paid from
 *  the steel and copper the engineer carries — never from the Depot: the chest is reached through the pockets
 *  (`chestTake`). GAME-ASSUMPTION: paying rubble at placement stands in for crafting the machine (§13 prices none;
 *  Phase 5 recipes decide whether a machine is a workbench craft with its own time — D-B2-1). Reach is the caller's:
 *  the scene's cursor and the command hook refuse outside 8 tiles; `place` itself lands anywhere, as the tests and
 *  the dev hooks do. */
export function canPlace(st: SimState, kind: Kind, tx: number, ty: number): PlaceCheck {
  const cost = MACHINE_COST[kind];
  const reason = placeable(st, kind, tx, ty);
  if (reason) return { ok: false, reason, cost, carried: false };
  if (kind === 'depot') return { ok: true, reason: '', cost, carried: false };
  const inv = st.engineer.inv;
  if ((inv[kind] ?? 0) >= 1) return { ok: true, reason: '', cost: { steel: 0, copper: 0 }, carried: true };
  if ((inv.steel ?? 0) < cost.steel || (inv.copper ?? 0) < cost.copper) return { ok: false, reason: `not enough in the pockets (${costStr(cost)})`, cost, carried: false };
  return { ok: true, reason: '', cost, carried: false };
}

function addMachine(st: SimState, kind: Kind, tx: number, ty: number, dir: Dir): Machine {
  const f = st.flow!;
  const m: Machine = { id: f.next++, kind, x: tx, y: ty, dir, size: MACHINE_SIZE[kind], items: [], hold: null, timer: 0, phase: 0, inv: {}, out: 0, busy: false };
  f.machines.push(m);
  for (let y = ty; y < ty + m.size; y++) for (let x = tx; x < tx + m.size; x++) f.occ[y * f.tw + x] = m.id;
  // GAME-ASSUMPTION (M4): a new machine on an eaten lamp's tile is a fresh lamp (the eaten one was picked up)
  if (f.threat?.broken.length) f.threat.broken = f.threat.broken.filter(t => { const x = t % f.tw, y = Math.floor(t / f.tw); return x < tx || x >= tx + m.size || y < ty || y >= ty + m.size; });
  f.rev++;
  return m;
}

/** Place a machine from the pockets (a carried one, else its price in carried rubble). Returns the machine, or null
 *  with the reason in `canPlace`. */
export function place(st: SimState, kind: Kind, tx: number, ty: number, dir: Dir = 0): Machine | null {
  tx = Math.floor(tx); ty = Math.floor(ty);
  const chk = canPlace(st, kind, tx, ty);
  if (!chk.ok) return null;
  if (chk.carried) pocketDrop(st.engineer, kind, 1);
  else { pocketDrop(st.engineer, 'steel', chk.cost.steel); pocketDrop(st.engineer, 'copper', chk.cost.copper); }
  const m = addMachine(st, kind, tx, ty, dir);
  if (kind === 'pole' || kind === 'bigpole') poleClaims(st, m);
  return m;
}

/** What a pick-up puts in the pockets: the machine as one stack (two turrets and their magazines are four stacks),
 *  plus what it held. GAME-ASSUMPTION (M2): a turret's rounds come back as
 *  whole magazines, the loose remainder (< 10 rounds) to the line buffer; a Generator's coal and an assembler's
 *  inputs and finished magazines come back whole; items on a belt or in an inserter's hand come back too. */
export function pickUpItems(m: Machine): Record<string, number> {
  const out: Record<string, number> = { [m.kind]: 1 };
  const add = (k: string, n: number) => { if (n > 0) out[k] = (out[k] ?? 0) + n; };
  for (const it of m.items) add(it.k, 1);
  if (m.hold) add(m.hold, 1);
  if (m.kind === 'turret') add('magazine', Math.floor((m.inv.rounds ?? 0) / SHOT.count));
  else if (m.kind === 'generator') add('coal', Math.floor(m.inv.coal ?? 0));
  else { for (const k in m.inv) add(k, Math.floor(m.inv[k])); add('magazine', m.out); }
  return out;
}
export interface PickUpCheck { ok: boolean; reason: string; m: Machine | null; stacks: number; items: Record<string, number> }
/** Can the machine on this tile be picked up into the pockets? All of it or none: a full pocket refuses. */
export function canPickUp(st: SimState, tx: number, ty: number): PickUpCheck {
  const m = st.flow ? machineAt(st, tx, ty) : null;
  if (!m) return { ok: false, reason: 'nothing there', m: null, stacks: 0, items: {} };
  if (m.kind === 'depot') return { ok: false, reason: 'the Depot stays', m, stacks: 0, items: {} };
  const items = pickUpItems(m), e = st.engineer;
  const trial: Engineer = { ...e, inv: { ...e.inv } };
  const before = invStacks(trial.inv);
  let need = 0;
  for (const k in items) {
    const one: Record<string, number> = { [k]: items[k] };
    need += invStacks(one);
    if (pocketTake(trial, k, items[k]) < items[k]) {
      return { ok: false, reason: `the pockets are full (${need} stack${need === 1 ? '' : 's'} to carry, ${Math.max(0, invCap(e) - before)} free)`, m, stacks: 0, items };
    }
  }
  return { ok: true, reason: '', m, stacks: invStacks(trial.inv) - before, items };
}

/** Pick up the machine on a tile into the pockets (never the Depot; a full pocket refuses — `canPickUp` says why).
 *  Returns the machine, or null. The remove of the lattice slice: the refund is the machine itself now (M2). */
export function remove(st: SimState, tx: number, ty: number): Machine | null {
  const f = st.flow, chk = canPickUp(st, tx, ty), m = chk.m;
  if (!f || !m || !chk.ok) return null;
  for (const k in chk.items) pocketTake(st.engineer, k, chk.items[k]);
  if (m.kind === 'turret') st.buffer = Math.min(st.config.bufferCap, st.buffer + (m.inv.rounds ?? 0) % SHOT.count);   // the loose rounds back to the line buffer
  for (let y = m.y; y < m.y + m.size; y++) for (let x = m.x; x < m.x + m.size; x++) delete f.occ[y * f.tw + x];
  f.machines.splice(f.machines.indexOf(m), 1);
  f.rev++;
  return m;
}

/** Turn a placed machine a quarter clockwise. */
export function rotate(st: SimState, tx: number, ty: number): Machine | null {
  const m = machineAt(st, tx, ty);
  if (!m || !st.flow || m.kind === 'depot' || m.kind === 'pole' || m.kind === 'lamp' || m.kind === 'bigpole' || m.kind === 'substation') return null;
  m.dir = ((m.dir + 1) % 4) as Dir;
  st.flow.rev++;
  return m;
}

export function setHandMine(st: SimState, at: [number, number] | null): void {
  const f = ensureFlow(st);
  if (at && rubbleAt(st, at[0], at[1]) && inReach(st, at[0], at[1])) f.hand.mine = at; else f.hand.mine = null;
  f.hand.prog = 0;
}
/** Queue `n` hand crafts at the workbench. Returns '' or the refusal: out of reach of the Depot, or the pockets
 *  cannot pay for the first one (the queue is capped at what they can pay for). */
export function queueCraft(st: SimState, n = 1): string {
  const f = ensureFlow(st), e = st.engineer;
  if (n > 0) {
    if (!nearDepot(st)) return 'walk closer to the workbench';
    const afford = Math.min(Math.floor((e.inv.steel ?? 0) / SHOT.inputs.steel), Math.floor((e.inv.copper ?? 0) / SHOT.inputs.copper));
    const room = afford - (f.hand.crafts - (f.hand.crafting ? 1 : 0));
    if (room <= 0) return `not enough in the pockets (${SHOT.inputs.steel} steel + ${SHOT.inputs.copper} Cu a magazine)`;
    n = Math.min(n, room);
  }
  f.hand.crafts = Math.max(0, f.hand.crafts + n);
  return '';
}

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
  supplyKw: number; demandKw: number; loadKw: number; brownoutS: number; fired: number; coalBurned: number; handFed: number; throttle: number;   // throttle: D-B3-4, 1 = every machine at full speed
  /** M4: crawlers and shades on the tiles now, and the hour's tallies. */
  crawlers: number; shades: number; onPlayer: number; lampsEaten: number; arrivals: number; turretKills: number; rifleKills: number;
}
export function flowSummary(st: SimState): FlowSummary {
  const f = st.flow;
  const s: FlowSummary = { excavators: 0, belts: 0, inserters: 0, assemblers: 0, beltItems: 0, assemblersBusy: 0, productionMagPerMin: 0,
                           magsMade: 0, magsDelivered: 0, mined: 0, coal: 0, craftsQueued: 0,
                           turrets: 0, lamps: 0, lampsLit: 0, poles: 0, polesConnected: 0, generators: 0, generatorsBurning: 0, turretRounds: 0, turretCap: 0, genCoal: 0, beltAmmo: 0,
                           supplyKw: 0, demandKw: 0, loadKw: 0, brownoutS: 0, fired: 0, coalBurned: 0, handFed: 0, throttle: 1,
                           crawlers: 0, shades: 0, onPlayer: 0, lampsEaten: 0, arrivals: 0, turretKills: 0, rifleKills: 0 };
  if (!f) return s;
  if (f.threat) {
    for (const c of f.threat.crawlers) { if (c.kind === 'shade') s.shades++; else s.crawlers++; if (c.onPlayer) s.onPlayer++; }
    s.lampsEaten = f.threat.stats.lampsEaten; s.arrivals = f.threat.stats.arrivals; s.turretKills = f.threat.stats.turretKills; s.rifleKills = f.threat.stats.rifleKills;
  }
  for (const m of f.machines) {
    if (m.kind === 'excavator') s.excavators++;
    else if (m.kind === 'belt') { s.belts++; s.beltItems += m.items.length; for (const it of m.items) if (it.k === 'magazine') s.beltAmmo++; }
    else if (m.kind === 'inserter') s.inserters++;
    else if (m.kind === 'assembler') { s.assemblers++; if (m.busy) s.assemblersBusy++; }
    else if (m.kind === 'turret') { s.turrets++; s.turretRounds += m.inv.rounds ?? 0; s.turretCap += TURRET_HOPPER; }
    else if (m.kind === 'lamp' || m.kind === 'floodlight') { s.lamps++; if (running(st, m)) s.lampsLit++; }   // a Floodlight counts as a lamp here
    else if (m.kind === 'pole' || m.kind === 'bigpole') s.poles++;
    else if (m.kind === 'generator') { s.generators++; s.genCoal += m.inv.coal ?? 0; if (m.busy) s.generatorsBurning++; }
  }
  s.productionMagPerMin = s.assemblers * 60 / SHOT.seconds;
  s.magsMade = f.stats.magsMade; s.magsDelivered = f.stats.magsDelivered; s.mined = f.stats.mined; s.coal = f.store.coal + s.genCoal; s.craftsQueued = f.hand.crafts;
  s.supplyKw = f.power.supply; s.demandKw = f.power.demand; s.loadKw = f.power.load; s.brownoutS = f.power.overS; s.throttle = st.config.power ? f.power.throttle : 1;
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
    case 'floodlight': return `Floodlight → ${DIR_NAMES[m.dir]} · ${FLOODLIGHT_KW} kW · ${FLOODLIGHT_RANGE}-tile cone · ${running(st, m) ? 'lit' : 'dark'}${on}`;
    case 'bigpole': return `Big pole · reach ${BIG_POLE_REACH} · ${poleGrid(st).connected.has(m.id) ? 'on the grid' : 'not connected'}`;
    case 'substation': return `Substation (built) · powers the face · ${subPowered(st, blockOf(st, m)) ? 'on' : 'off'}`;
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
  if (!st.lattice) return faceTurretEdge(st, m);
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

/** On a face: the segment the turret serves (`faceSegOf` — front ring first, then the nearest ridge tile within
 *  range 9 of the turret's centre, the same one-street-a-turret rule as the lattice). */
function faceTurretEdge(st: SimState, m: Machine): number {
  const bi = blockIdxOf(st, m);
  if (bi < 0) return -1;
  const j = faceSegOf(st, bi, m.x, m.y, m.size);
  return j < 0 ? -1 : edgeId(st, bi, j);
}

/** D-B1-4: the turrets that serve an edge — those whose `turretEdge` it is; or, when it has none of its own, every
 *  running turret on the block with a ridge tile of that street in range. GAME-ASSUMPTION: a corner sliver (a
 *  segment whose front ring cannot hold a 2×2 — seed 3 has a 7-tile one, seed 5 a 12-tile one) is covered by the
 *  neighbouring segment's turrets that reach it, which then serve two streets at once. */
export function edgeTurrets(st: SimState, id: number, busyOnly = false): Machine[] {
  const f = st.flow!;
  const own = f.machines.filter(m => m.kind === 'turret' && running(st, m) && (!busyOnly || m.busy) && turretEdge(st, m) === id);
  if (own.length || st.lattice) return own;
  const bi = edgeFrom(st, id), j = edgeTo(st, id), cg = cityGeomOf(st), tw = cg.tw, sg = segBetween(cg, bi, j);
  if (!sg) return own;
  return f.machines.filter(m => {
    if (m.kind !== 'turret' || !running(st, m) || (busyOnly && !m.busy) || blockIdxOf(st, m) !== bi) return false;
    const cx = m.x + m.size / 2, cy = m.y + m.size / 2;
    for (let k = 0; k < sg.ridge.length; k++) { const t = sg.ridge[k], tx = t % tw, ty = (t - tx) / tw; if (Math.hypot(tx + 0.5 - cx, ty + 0.5 - cy) < TURRET_RANGE) return true; }
    return false;
  });
}

/** GAME-ASSUMPTION: an edge with physical turrets fires only through them; an edge with none keeps the block-level
 *  stand-in hopper until M6 decides (D-P4-5). D-B1-4: the HQ's start turrets are derived from its segments
 *  (`startTurrets`), so every live HQ segment is a turret edge from tick one — the first city soak's bare fourth
 *  segment (unfed, HQ lost at minute 20) is gone by rule, with no HQ branch here. */
function hookSyncEdges(st: SimState): void {
  const f = st.flow!, ring = st.ring;
  for (const e of ring) if (e.turrets !== undefined) { e.turrets = 0; e.fire = 0; e.hopper = 0; }
  for (const m of f.machines) if (m.kind === 'turret') { m.busy = false; m.out = 0; }
  for (const e of ring) {
    const ts = edgeTurrets(st, e.id);
    if (!ts.length) continue;
    if (e.turrets === undefined) { e.turrets = 0; e.fire = 0; e.hopper = 0; }
    for (const m of ts) {
      const rounds = m.inv.rounds ?? 0;
      e.turrets!++; e.fire! += Math.min(rounds, TURRET_ROUNDS_PER_S); e.hopper += rounds;
      m.busy = true;
    }
    if (e.kit === false) e.kit = true;   // D-B1-4: physical turrets are the kit
  }
  for (const e of ring) if (e.turrets === 0) { delete e.turrets; delete e.fire; }   // no turret left: back to the stand-in, ring-fed
}
function hookDrainEdges(st: SimState, fired: Float64Array): void {
  const f = st.flow!;
  for (let r = 0; r < st.ring.length; r++) {
    let left = fired[r];
    const e = st.ring[r];
    if (left <= 1e-9 || !e.turrets) continue;
    const ts = edgeTurrets(st, e.id, true).sort((a, b) => (a.inv.rounds ?? 0) - (b.inv.rounds ?? 0));
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
function hookDemandKw(st: SimState, all: boolean): number {
  const f = st.flow!;
  let kw = 0;
  for (const m of f.machines) {
    const w = MACHINE_KW[m.kind];
    if (!w) continue;
    if (blockIdxOf(st, m) < 0) continue;
    const b = blockOf(st, m);
    if (b.state !== HELD) continue;
    if (all || (b.subOn && st.t >= b.shadeOff)) kw += w;
  }
  return kw;
}
function hookSetLoad(st: SimState, supply: number, demand: number, load: number, throttle: number): void {
  const f = st.flow!;
  f.power.supply = supply; f.power.demand = demand; f.power.load = load; f.power.throttle = throttle;
  if (throttle < 1 - 1e-9) f.power.overS++;
}
/** D-B1-4: an edge is covered when a running turret serves it (`edgeTurrets`). */
function hookCovered(st: SimState, id: number): boolean {
  return edgeTurrets(st, id).length > 0;
}

tileHooks.current = { syncEdges: hookSyncEdges, covered: hookCovered, drainEdges: hookDrainEdges, supplyKw: hookSupplyKw, demandKw: hookDemandKw, setLoad: hookSetLoad };

// ------------------------------------------------------------------ M3: substations, streetlights, light

export interface SubstationView { tx: number; ty: number; size: number; on: boolean; kw: number }
/** A face's substation: the pre-existing one ground.ts places, else (prompt B M3) a craftable Substation the
 *  Electricians unlocked, standing on the face. Null on an outskirts face with neither (§7). */
export function faceSub(st: SimState, bi: number): { x: number; y: number; size: number } | null {
  const g = ground(st).blocks[bi].sub;
  if (g) return g;
  const f = st.flow;
  if (f) for (const m of f.machines) if (m.kind === 'substation' && blockIdxOf(st, m) === bi) return { x: m.x, y: m.y, size: m.size };
  return null;
}
/** The block's substation (pre-existing or built), with whether it is powered and what it draws now. */
export function substationAt(st: SimState, bx: number, by: number): SubstationView | null {
  const bi = idxOf(st, bx, by);
  if (bi < 0) return null;
  const b = st.blocks[bi], sub = faceSub(st, bi);
  if (!sub || (b.state !== HELD && b.state !== CONTESTED && b.state !== DARK)) return null;
  const half = st.config.draw === 'half';
  const kw = b.state === DARK ? 0 : b.state === CONTESTED ? (half ? 100 : 200) : st.config.draw === 'flat' ? 120 : b.exposed ? (half ? 100 : 200) : (half ? 20 : 40);
  return { tx: sub.x, ty: sub.y, size: sub.size, on: subPowered(st, b), kw };
}
export function isSubstationTile(st: SimState, tx: number, ty: number): boolean {
  return substationOwner(st, tx, ty) >= 0 || machineAt(st, tx, ty)?.kind === 'substation';
}

export interface Light { tx: number; ty: number; r: number; lit: boolean; broken: boolean; kind: 'streetlight' | 'lamp' | 'floodlight'; dir?: Dir;
  /** M5: 'broken' is §13's 3-in-8 (E repairs it), 'eaten' a crawler's (M4; E repairs it), '' otherwise. */
  why?: 'broken' | 'eaten' | '' }
/** M5 (§6): a claimed block's streetlights come on in sequence from the substation outward, this many a second
 *  (GAME-ASSUMPTION: the order is straight-line distance from the substation, not the walk along the kerb). */
export const LIGHT_SEQ_PER_S = 3;
/** M5: a repair costs this much copper from the pockets (GAME-ASSUMPTION: one Cu — wire — per light; §13 prices no repair). */
export const REPAIR_COPPER = 1;
const rankCache = new WeakMap<object, Int32Array>();
/** The order a face's streetlights come on in (0 first): by distance from the substation's centre, ties by index. */
function lightRanks(st: SimState, bi: number): Int32Array {
  const bg = ground(st).blocks[bi];
  const hit = rankCache.get(bg);
  if (hit) return hit;
  const c = bg.sub ? [bg.sub.x + bg.sub.size / 2, bg.sub.y + bg.sub.size / 2] : [bg.pole[0] + 0.5, bg.pole[1] + 0.5];
  const idx = bg.lights.map((l, k) => k).sort((a, b) => Math.hypot(bg.lights[a].tx + 0.5 - c[0], bg.lights[a].ty + 0.5 - c[1]) - Math.hypot(bg.lights[b].tx + 0.5 - c[0], bg.lights[b].ty + 0.5 - c[1]) || a - b);
  const ranks = new Int32Array(bg.lights.length);
  idx.forEach((k, r) => { ranks[k] = r; });
  rankCache.set(bg, ranks);
  return ranks;
}
/** M5: how far a block's burn-off has run — 0 at the claim, 1 when it turns Held (and for every Held block); -1 for a
 *  block that is neither Contested nor Held. The claim time is read back from `contestUntil` (§5 step 4's 20 + 60·d). */
export function contestProgress(st: SimState, bi: number): number {
  if (bi < 0 || bi >= st.blocks.length) return -1;
  const b = st.blocks[bi];
  if (b.state === HELD) return 1;
  if (b.state !== CONTESTED) return -1;
  const len = burnOffS(b.d);
  return Math.max(0, Math.min(1, (st.t - (b.contestUntil - len)) / len));
}
/** Everything that can light a block: its streetlights (lit when the substation powers and they are not broken) and
 *  the Lamps on it (lit while powered). A streetlight reaches the street midline (STREETLIGHT_RADIUS 7, D-B5-4), a
 *  Lamp §13's 4 tiles; M5 draws them as the light texture and, on a
 *  Contested block, switches the streetlights on in sequence from the substation outward at LIGHT_SEQ_PER_S. */
export function blockLights(st: SimState, bi: number): Light[] {
  if (bi < 0 || bi >= st.blocks.length) return [];
  const b = st.blocks[bi];
  if (b.state !== HELD && b.state !== CONTESTED && b.state !== DARK) return [];
  const on = subPowered(st, b);
  const f = st.flow, tw = ground(st).tw, eaten = f ? brokenSet(f) : null, fixed = f ? repairedSet(f) : null;   // M4/M5: eaten lights stay dark until E repairs them
  const seqT = b.state === CONTESTED ? b.contestUntil - burnOffS(b.d) : -Infinity, ranks = b.state === CONTESTED ? lightRanks(st, bi) : null;
  const out: Light[] = ground(st).blocks[bi].lights.map((l, k) => {
    const t = l.ty * tw + l.tx;
    const why: Light['why'] = eaten?.has(t) ? 'eaten' : l.broken && !fixed?.has(t) ? 'broken' : '';
    const inSeq = !ranks || st.t >= seqT + ranks[k] / LIGHT_SEQ_PER_S;   // M5 (§6): three a second down the street
    return { tx: l.tx, ty: l.ty, r: STREETLIGHT_RADIUS, lit: on && !why && inSeq, broken: !!why, kind: 'streetlight' as const, why };
  });
  if (f) for (const m of f.machines) {
    if (blockIdxOf(st, m) !== bi) continue;
    if (m.kind === 'lamp') { const broken = eaten!.has(m.y * tw + m.x); out.push({ tx: m.x, ty: m.y, r: LAMP_RADIUS, lit: running(st, m) && !broken, broken, kind: 'lamp', why: broken ? 'eaten' : '' }); }
    // prompt B M3: a Floodlight throws a 12-tile cone from its centre along its facing (60° wide, GAME-ASSUMPTION)
    else if (m.kind === 'floodlight') out.push({ tx: m.x + 0.5, ty: m.y + 0.5, r: FLOODLIGHT_RANGE, lit: running(st, m), broken: false, kind: 'floodlight', dir: m.dir, why: '' });
  }
  return out;
}
export function cellLights(st: SimState, bx: number, by: number): Light[] { return blockLights(st, idxOf(st, bx, by)); }
const brokenSets = new WeakMap<FlowState, { n: number; set: Set<number> }>();
function brokenSet(f: FlowState): Set<number> {
  const arr = f.threat?.broken ?? [];
  const c = brokenSets.get(f);
  if (c && c.n === arr.length) return c.set;
  const set = new Set(arr);
  brokenSets.set(f, { n: arr.length, set });
  return set;
}
const repairedSets = new WeakMap<FlowState, { n: number; set: Set<number> }>();
function repairedSet(f: FlowState): Set<number> {
  const arr = f.repaired ?? [];
  const c = repairedSets.get(f);
  if (c && c.n === arr.length) return c.set;
  const set = new Set(arr);
  repairedSets.set(f, { n: arr.length, set });
  return set;
}
/** Does a lit light reach tile (tx, ty)? Discs for streetlights and Lamps; the Floodlight's tiles under the fixture
 *  count, beyond them only the cone. One rule for the shade test (`litAt`) and the light texture (`lightMask`). */
export function lightCovers(l: Light, tx: number, ty: number): boolean {
  const ex = tx - l.tx, ey = ty - l.ty, d2 = ex * ex + ey * ey;
  if (d2 > l.r * l.r + 1e-9) return false;
  if (l.kind === 'floodlight' && d2 > 2) {
    const cos = (ex * DX[l.dir!] + ey * DY[l.dir!]) / Math.sqrt(d2);
    if (Math.acos(Math.max(-1, Math.min(1, cos))) > FLOODLIGHT_HALF_ANGLE + 1e-9) return false;
  }
  return true;
}
/** Is a tile lit by any light within its radius (its block's or a neighbour's)? */
export function litAt(st: SimState, tx: number, ty: number): boolean {
  for (const bi of blocksNear(st, tx, ty)) {
    for (const l of blockLights(st, bi)) if (l.lit && lightCovers(l, tx, ty)) return true;
  }
  return false;
}
/** M5: the light standing on a tile (a streetlight on the kerb, a Lamp), with its block; null when there is none. */
export function lightAt(st: SimState, tx: number, ty: number): { l: Light; bi: number } | null {
  for (const bi of blocksNear(st, tx, ty)) {
    for (const l of blockLights(st, bi)) if (l.kind !== 'floodlight' && l.tx === tx && l.ty === ty) return { l, bi };
  }
  return null;
}
export interface RepairCheck { ok: boolean; reason: string; l: Light | null }
/** M5: can E repair the light on this tile? Reach is the caller's (the scene's cursor). */
export function canRepair(st: SimState, tx: number, ty: number): RepairCheck {
  const hit = lightAt(st, tx, ty);
  if (!hit) return { ok: false, reason: 'no light here', l: null };
  if (!hit.l.why) return { ok: false, reason: 'this light is not broken', l: hit.l };
  if (!st.flow) return { ok: false, reason: 'no engineer on the tiles', l: hit.l };
  if ((st.engineer.inv.copper ?? 0) < REPAIR_COPPER) return { ok: false, reason: `no copper in the pockets (a repair is ${REPAIR_COPPER} Cu — take it from the Depot chest with I, or dig copper rubble)`, l: hit.l };
  return { ok: true, reason: '', l: hit.l };
}
/** M5: repair the light on a tile from the pockets. A §13-broken streetlight joins `flow.repaired`; an eaten light
 *  (M4) leaves `threat.broken`. It lights on the next read when its block powers it. Returns the check. */
export function repairLight(st: SimState, tx: number, ty: number): RepairCheck {
  const c = canRepair(st, tx, ty);
  if (!c.ok) return c;
  const f = st.flow!, t = ty * f.tw + tx;
  pocketDrop(st.engineer, 'copper', REPAIR_COPPER);
  if (c.l!.why === 'eaten' && f.threat) f.threat.broken = f.threat.broken.filter(v => v !== t);
  else (f.repaired ??= []).push(t);
  f.repairs = (f.repairs ?? 0) + 1;
  return c;
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
/** A pole's reach: 8, a Big pole's 12 (§13); a wire spans the longer of its two ends' reaches (GAME-ASSUMPTION). */
export const reachOf = (m: Machine): number => (m.kind === 'bigpole' ? BIG_POLE_REACH : POLE_REACH);
const isPole = (m: Machine): boolean => m.kind === 'pole' || m.kind === 'bigpole';
const pcx = (m: Machine): number => m.x + m.size / 2, pcy = (m: Machine): number => m.y + m.size / 2;
/** GAME-ASSUMPTION: a pole hangs from any pole or claimed substation within reach 8 (centre to centre, or to the
 *  substation's nearest edge). The block map stays the judge of power: the poles are how a claim is strung and shown,
 *  and a Dark substation a connected pole reaches raises the claim; a claimed block keeps its power whether or not
 *  its poles still stand. A built Substation (prompt B M3) anchors like a pre-existing one. */
export function poleGrid(st: SimState): PoleGrid {
  const f = ensureFlow(st);
  const c = gridCache.get(f);
  if (c && c.rev === f.rev && c.t === st.t) return c.grid;
  const grid: PoleGrid = { connected: new Set(), links: [], reached: [] };
  const poles = f.machines.filter(isPole);
  const subs: { bi: number; x: number; y: number; size: number; claimed: boolean }[] = [];
  const G = ground(st);
  for (const bg of G.blocks) {
    const b = st.blocks[bg.i];
    if (b.state !== HELD && b.state !== CONTESTED && b.state !== DARK) continue;
    const sub = faceSub(st, bg.i);
    if (!sub) continue;
    subs.push({ bi: bg.i, x: sub.x, y: sub.y, size: sub.size, claimed: b.state !== DARK });
  }
  const queue: Machine[] = [];
  for (const m of poles) {
    for (const s of subs) {
      if (!s.claimed || distToRect(pcx(m), pcy(m), s.x, s.y, s.size) > reachOf(m)) continue;
      grid.connected.add(m.id); queue.push(m);
      grid.links.push({ x0: pcx(m), y0: pcy(m), x1: s.x + s.size / 2, y1: s.y + s.size / 2 });
      break;
    }
  }
  for (let q = 0; q < queue.length; q++) {
    const a = queue[q];
    for (const m of poles) {
      if (grid.connected.has(m.id) || Math.hypot(pcx(m) - pcx(a), pcy(m) - pcy(a)) > Math.max(reachOf(m), reachOf(a))) continue;
      grid.connected.add(m.id); queue.push(m);
      grid.links.push({ x0: pcx(m), y0: pcy(m), x1: pcx(a), y1: pcy(a) });
    }
  }
  for (const s of subs) {
    if (s.claimed) continue;
    for (const m of queue) if (distToRect(pcx(m), pcy(m), s.x, s.y, s.size) <= reachOf(m)) { grid.reached.push(s.bi); break; }
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
    const sub = faceSub(st, bi);
    if (!sub || distToRect(pcx(m), pcy(m), sub.x, sub.y, sub.size) > reachOf(m)) continue;
    f.pending.push({ type: 'claim', x: b.x, y: b.y });
  }
}
/** A claim made from the map view strings its poles: from the nearest connected point (a pole, or a claimed
 *  neighbour's substation) straight towards the new block's substation, one pole every 7 tiles, each shifted to the
 *  nearest free tile. GAME-ASSUMPTION: the claim's 10 wire + 5 frames already paid for them; a run that finds no room
 *  simply stops short (the map still powers the block). */
export function layPoles(st: SimState, x: number, y: number): number {
  const f = ensureFlow(st), bi = idxOf(st, x, y);
  if (bi < 0) return 0;
  const b = st.blocks[bi], sub = faceSub(st, bi);
  if (!sub || (b.state !== HELD && b.state !== CONTESTED)) return 0;   // an outskirts face has no substation to string to (§7)
  const tx = sub.x, ty = sub.y, tcx = tx + sub.size / 2, tcy = ty + sub.size / 2;
  const g = poleGrid(st);
  let from: [number, number] | null = null, best = Infinity;
  for (const m of f.machines) {
    if (!isPole(m) || !g.connected.has(m.id)) continue;
    const d = distToRect(pcx(m), pcy(m), tx, ty, sub.size);
    if (d <= reachOf(m)) return 0;   // already strung (a world-view claim)
    if (d < best) { best = d; from = [pcx(m), pcy(m)]; }
  }
  for (const ni of st.nb[bi]) {
    const nb = st.blocks[ni], ns = faceSub(st, ni);
    if (!ns || (nb.state !== HELD && nb.state !== CONTESTED)) continue;
    const cx = ns.x + ns.size / 2, cy = ns.y + ns.size / 2;
    const d = Math.hypot(cx - tcx, cy - tcy);
    if (d < best) { best = d; from = [cx, cy]; }
  }
  if (!from) return 0;
  let [cx, cy] = from, laid = 0;
  for (let n = 0; n < 16; n++) {
    if (distToRect(cx, cy, tx, ty, sub.size) <= POLE_REACH) break;
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

export interface HandFed { kind: 'turret' | 'generator'; moved: number; reason: string }
/** Prompt B M3 (run name B-M3-hands): a click with the hand moves what the pockets carry — whole magazines into a
 *  turret's hopper, coal into a Generator — filling it. The pockets are filled from the Depot chest (I, within reach)
 *  or the workbench (E); the Depot itself is never drawn on here, as for placement (M2). GAME-ASSUMPTION: instant —
 *  §11's "hand-fed the west and east turrets twice each" is two clicks. Reach is the caller's (the scene's cursor). */
export function handFeed(st: SimState, tx: number, ty: number): HandFed | null {
  const f = st.flow, m = machineAt(st, tx, ty);
  if (!f || !m) return null;
  const inv = st.engineer.inv;
  if (m.kind === 'turret') {
    const room = Math.floor((TURRET_HOPPER - (m.inv.rounds ?? 0)) / SHOT.count), have = Math.floor(inv.magazine ?? 0);
    const mags = Math.max(0, Math.min(room, have));
    if (mags > 0) { m.inv.rounds = (m.inv.rounds ?? 0) + mags * SHOT.count; pocketDrop(st.engineer, 'magazine', mags); f.stats.handFed += mags; }
    return { kind: 'turret', moved: mags, reason: mags ? '' : room <= 0 ? 'the hopper is full' : 'no magazines in the pockets (take them from the Depot chest with I, or craft at the workbench with E)' };
  }
  if (m.kind === 'generator') {
    const n = Math.max(0, Math.min(GENERATOR_COAL_CAP - (m.inv.coal ?? 0), Math.floor(inv.coal ?? 0)));
    if (n > 0) { m.inv.coal = (m.inv.coal ?? 0) + n; pocketDrop(st.engineer, 'coal', n); f.stats.handFed += n; }
    return { kind: 'generator', moved: n, reason: n ? '' : (m.inv.coal ?? 0) >= GENERATOR_COAL_CAP ? 'the Generator is full' : 'no coal in the pockets (take it from the Depot chest with I, or dig the coal patch)' };
  }
  return null;
}
/** The bot's feed: from the Depot's magazines (the line buffer) and the Depot's coal, filling the machine. */
function depotFeed(st: SimState, m: Machine): number {
  const f = st.flow!;
  if (m.kind === 'turret') {
    const mags = Math.max(0, Math.min(Math.floor((TURRET_HOPPER - (m.inv.rounds ?? 0)) / SHOT.count), Math.floor(st.buffer / SHOT.count)));
    if (mags > 0) { m.inv.rounds = (m.inv.rounds ?? 0) + mags * SHOT.count; st.buffer -= mags * SHOT.count; f.stats.handFed += mags; }
    return mags;
  }
  const n = Math.max(0, Math.min(GENERATOR_COAL_CAP - (m.inv.coal ?? 0), Math.floor(f.store.coal)));
  if (n > 0) { m.inv.coal = (m.inv.coal ?? 0) + n; f.store.coal -= n; f.stats.handFed += n; }
  return n;
}

/** The bot's hands (autoplay only, a dev aid like the bot itself): each frame, hand-feed every turret and Generator at
 *  or under half from the Depot, the way §11's player does in the first minutes. GAME-ASSUMPTION: the bot has no
 *  pockets to fill, so its hands draw on the Depot directly (the player's draw on the pockets, `handFeed`); the bot
 *  never builds a line, so with the flow layer on the HQ's 200 rounds are its whole game unless the harness places
 *  one; a soak that measures the hour places §11's line and lets these hands carry magazines to six turrets. */
export function botHands(st: SimState): number {
  const f = st.flow;
  if (!f) return 0;
  let moved = 0;
  for (const m of f.machines) {
    if (m.kind === 'turret' && (m.inv.rounds ?? 0) <= TURRET_HOPPER / 2) moved += depotFeed(st, m);
    else if (m.kind === 'generator' && (m.inv.coal ?? 0) <= GENERATOR_COAL_CAP / 2) moved += depotFeed(st, m);
  }
  return moved;
}

/** Lot tiles on the start lot with a patch: for tests and the renderer. */
export function isDepotTile(lx: number, ly: number): boolean {
  return lx >= DEPOT_LOT && lx < DEPOT_LOT + DEPOT_TILES && ly >= DEPOT_LOT && ly < DEPOT_LOT + DEPOT_TILES;
}

// ------------------------------------------------------------------ Prompt B M1: pockets and the chest

export type ChestItem = 'steel' | 'copper' | 'stone' | 'coal' | 'magazine' | 'kit';
export const CHEST_ITEMS: readonly ChestItem[] = ['steel', 'copper', 'stone', 'coal', 'magazine', 'kit'];
export function isChestItem(s: string): s is ChestItem { return (CHEST_ITEMS as readonly string[]).includes(s); }

/** The Depot's footprint: the chest and workbench on the HQ lot. */
export function depotRect(st: SimState): { x: number; y: number; size: number } {
  const [x, y] = hqLot(st, DEPOT_LOT, DEPOT_LOT);
  return { x, y, size: DEPOT_TILES };
}
/** D5: the chest and the workbench answer only to an engineer within reach of the Depot. */
export function nearDepot(st: SimState): boolean {
  const d = depotRect(st);
  return inReach(st, d.x, d.y, d.size);
}
/** What the chest holds: the block sim's stock (§14), the line buffer as magazines, the flow store's coal; kits are
 *  free to draw (engineer.ts `restock`'s GAME-ASSUMPTION: the claim paid for them). */
export function chestCount(st: SimState, item: ChestItem): number {
  if (item === 'magazine') return Math.floor(st.buffer / SHOT.count);
  if (item === 'coal') return Math.floor(st.flow?.store.coal ?? 0);
  if (item === 'kit') return Infinity;
  return Math.floor(st.stock[item]);
}
/** GAME-ASSUMPTION (M4 telemetry): a trip to the chest is the first transaction after being out of the Depot's reach. */
function chestTrip(f: FlowState): void { if (f.hand.away) { f.stats.chestTrips++; f.hand.away = false; } }
/** Move up to `n` of an item from the chest to the pockets (as many as fit). */
export function chestTake(st: SimState, item: ChestItem, n: number): { moved: number; reason: string } {
  const f = ensureFlow(st);
  if (!nearDepot(st)) return { moved: 0, reason: 'walk closer to the Depot' };
  const have = chestCount(st, item), want = Math.max(0, Math.min(n, have));
  if (want <= 0) return { moved: 0, reason: `no ${item === 'magazine' ? 'magazines' : item} in the Depot` };
  const got = pocketTake(st.engineer, item, want);
  if (got <= 0) return { moved: 0, reason: 'the pockets are full' };
  chestTrip(f);
  if (item === 'magazine') st.buffer -= got * SHOT.count;
  else if (item === 'coal') f.store.coal -= got;
  else if (item !== 'kit') st.stock[item] -= got;
  return { moved: got, reason: '' };
}
/** Move up to `n` of an item from the pockets to the chest. A full line buffer refuses magazines. */
export function chestPut(st: SimState, item: ChestItem, n: number): { moved: number; reason: string } {
  const f = ensureFlow(st);
  if (!nearDepot(st)) return { moved: 0, reason: 'walk closer to the Depot' };
  const have = st.engineer.inv[item] ?? 0;
  let want = Math.max(0, Math.min(n, have));
  if (item === 'magazine') want = Math.min(want, Math.floor((st.config.bufferCap - st.buffer) / SHOT.count));
  if (want <= 0) return { moved: 0, reason: have <= 0 ? `no ${item} in the pockets` : 'the line buffer is full' };
  const put = pocketDrop(st.engineer, item, want);
  chestTrip(f);
  if (item === 'magazine') { st.buffer += put * SHOT.count; f.stats.magsDelivered += put; }
  else if (item === 'coal') f.store.coal += put;
  else if (item !== 'kit') st.stock[item] += put;
  if (item !== 'kit') f.stats.delivered[item]++;
  return { moved: put, reason: '' };
}

// the engineer's hand commands (types.ts) land here when the flow layer is loaded
handHook.current = (st, c) => {
  switch (c.type) {
    case 'mineAt': setHandMine(st, [c.x, c.y]); break;
    case 'craft': queueCraft(st, c.count ?? 1); break;
    case 'chestTake': if (isChestItem(c.item)) chestTake(st, c.item, c.n); break;
    case 'chestPut': if (isChestItem(c.item)) chestPut(st, c.item, c.n); break;
    // M2: the command form of the scene's placement — within reach (8 tiles) or nothing, never moved
    case 'place': if (isKind(c.item) && inReach(st, c.x, c.y, MACHINE_SIZE[c.item])) place(st, c.item, c.x, c.y, ((c.dir ?? 0) % 4) as Dir); break;
    case 'pickUp': { const m = machineAt(st, c.x, c.y); if (m && inReach(st, m.x, m.y, m.size)) remove(st, c.x, c.y); break; }
    // M6: the scene's E / R as commands (the scene checks reach before it calls; the command form checks it here)
    case 'feed': { const m = machineAt(st, c.x, c.y); if (m && inReach(st, m.x, m.y, m.size)) handFeed(st, c.x, c.y); break; }
    case 'repair': if (inReach(st, c.x, c.y, 1)) repairLight(st, c.x, c.y); break;   // a street light is a tile, not a machine
    case 'rotate': { const m = machineAt(st, c.x, c.y); if (m && inReach(st, m.x, m.y, m.size)) rotate(st, c.x, c.y); break; }
    default: break;
  }
};

// ------------------------------------------------------------------ world-view drawing (§18, docsync)

/** §18 legend for `renderLot`. */
export const LOT_LEGEND =
  '`:` street, `.` ground, `r` rubble, `#` steel patch, `&` copper patch, `*` coal patch, `~` inert/river; ' +
  '`L` streetlight lit, `l` streetlight dark, `b` streetlight broken; `T` Gun turret (hopper > 0), `t` turret with an empty hopper; ' +
  '`G` Generator burning, `g` Generator dry; `S` substation powered, `s` unpowered; `D` Depot; `X` Excavator running, `x` stopped; ' +
  '`A` Shot assembler crafting, `a` idle; `^ > v <` belt by direction, `I` inserter; `@` Lamp lit, `o` Lamp dark; `P` pole. ' +
  'A brownout (D-B3-4) slows every machine instead of stopping any, so it does not show in the drawing.';

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
      case 'excavator': ch = running(st, m) ? 'X' : 'x'; break;
      case 'assembler': ch = m.busy && running(st, m) ? 'A' : 'a'; break;
      case 'belt': ch = '^>v<'[m.dir]; break;
      case 'inserter': ch = 'I'; break;
      case 'lamp': ch = running(st, m) ? '@' : 'o'; break;
      case 'pole': ch = 'P'; break;
      case 'floodlight': ch = running(st, m) ? 'F' : 'f'; break;
      case 'bigpole': ch = 'B'; break;
      case 'substation': ch = 'S'; break;
    }
    for (let dy = 0; dy < m.size; dy++) for (let dx = 0; dx < m.size; dx++) put(m.x + dx - ox, m.y + dy - oy, ch);
  }
  const head = ' col: ' + Array.from({ length: CELL_TILES }, (_, i) => String(i % 10)).join(' ');
  return [head, ...rows.map((r, ty) => `${String(ty).padStart(4)}  ${r.join(' ')}`)].join('\n');
}
