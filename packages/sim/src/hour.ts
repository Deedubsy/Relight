/** Prompt B M6 "The hour" (run name B-M6-hour): a bot that plays §11's minute list on the tile layer with real
 *  walking and real pockets — to the steel patch and back to the workbench, the turrets hand-fed on the first red pip
 *  (D-P4-8), the line placed from the pockets in trips to the chest, a second steel Excavator into the chest at 15:00
 *  (D-P4-7), east / west / north claimed at constants.HOUR's minutes (D-HOUR-1, D-P4-10: 15 / 25 / 65, the same
 *  minutes firsthour.ts reads) with the kit walked over — north's is past the hour, so the scored hour is two claims
 *  — the hands running magazines and coal every five minutes, and writes down when each thing happened. Through
 *  Gate B a claim's edges keep the block sim's ring-fed hopper and no turret is carried to them (D-P4-9).
 *  `hourReport` turns the log into findings against §11's prose and the calibration timeline. Every command goes
 *  through `Command` (types.ts), so a session under the bot replays like a played one (`replay`, Gate B's "did the
 *  rifle matter" row). The bot is a dev aid: not a player control, never on by default (`?autoplay=hour`). */
import { SimState, Command, HELD, DARK } from './types';
import { hqIdx, RIFLE_RANGE, INV_STACKS, KIT_STACKS, invStacks, invCap } from './engineer';
import { ground, hqLot, inReach } from './ground';
import { LOT_TILES } from './tiles';
import { findPath, passable } from './walk';
import { burnOffS, isCandidate } from './sim';
import { pipOf, edgeCap } from './queries';
import { TURRET_HOPPER } from './recipes';
import { HQ_PATCHES, P_STEEL, DEPOT_LOT, DEPOT_TILES } from './tiles';
import {
  Kind, Dir, Machine, depotRect, chestCount, turretEdge, placeable, canPlace, rubbleAt,
  MACHINE_SIZE, MACHINE_COST, GENERATOR_COAL_CAP, survivorJoined, advanceFlow, TILE_DT, TILE_TPS, ensureFlow, SHOT, START_TURRETS,
} from './flow';
import { threatActive, threatOf } from './threat';
import { HOUR_CLAIM_MIN, HOUR_GENERATOR_MIN, HOUR_SHOT_LINE_MIN, HOUR_STEEL2_MIN, HOUR_COPPER2_MIN, HOUR_ASM3_MIN } from './constants';

// ------------------------------------------------------------------ the log

export interface HourEntry { t: number; what: string }
export interface HourWalk { name: string; t0: number; t1: number; tiles: number }
export interface HourRefusal { t: number; what: string; reason: string }
export interface HourLog {
  entries: HourEntry[];
  /** §11's moments by name → the sim second they happened (absent = not yet). */
  marks: Record<string, number>;
  /** Walks the bot timed (the claim walk-overs, the trips across the lot). */
  walks: HourWalk[];
  /** What the sim refused the bot: a placement out of reach or unaffordable, a walk that found no path, a wait that timed out. */
  refused: HourRefusal[];
}

export type HourDir = 'east' | 'west' | 'north';
const DIRS: readonly HourDir[] = ['east', 'west', 'north'];

// ------------------------------------------------------------------ tasks

type Task =
  | { kind: 'goto'; label: string; x: number; y: number; size: number; walk?: string; sent: boolean; t0: number; tiles: number; tries: number }
  | { kind: 'do'; label: string; fn: (st: SimState, out: Command[]) => void | Task[] }
  | { kind: 'until'; label: string; pred: (st: SimState) => boolean; each?: (st: SimState, out: Command[]) => void; secs: number; at: number };

const goto = (label: string, x: number, y: number, size: number, walk?: string): Task => ({ kind: 'goto', label, x, y, size, walk, sent: false, t0: -1, tiles: -1, tries: 0 });
const act = (label: string, fn: (st: SimState, out: Command[]) => void | Task[]): Task => ({ kind: 'do', label, fn });
const until = (label: string, pred: (st: SimState) => boolean, secs: number, each?: (st: SimState, out: Command[]) => void): Task =>
  ({ kind: 'until', label, pred, secs, each, at: -1 });

export interface HourStep { at: number; name: string; tasks: (st: SimState, bot: HourBot) => Task[] }

export interface HourBot {
  rifle: boolean;
  log: HourLog;
  steps: HourStep[];
  next: number;
  queue: Task[];
  /** Block index claimed per direction. */
  claimed: Partial<Record<HourDir, number>>;
  lastRun: number;
  aiming: boolean;
  /** Why a claimed block fell (the sim's fall event: unfed / shade / starved). */
  fellWhy: Partial<Record<HourDir, string>>;
  /** E-rifle's rescue: the script and the hands (feed beat, rounds run) dropped; only the rifle's reflex is left. */
  handsOff: boolean;
  /** For the report: the state's ticks the bot has seen, and the hand-fed count after the first hand-feed beat. */
  ticks: number;
  fedAtFeedDone: number;
  /** D-P4-7: where Generator 2's coal comes from — (b) the chest's 40 start coal, or (a) the coal Excavator's first
   *  units, waited for after its belt is laid (the harness runs both; the game ships (b)). */
  coalPlan: 'chest' | 'wait';
  /** The hand-feed beat's last run (D-P4-8: a red pip on an HQ edge sends the bot to its turrets with the pockets). */
  lastFeed: number;
  /** The chest by the minute, for the steel curve (D-P4-4: report its minimum). */
  stock: { t: number; steel: number; copper: number; coal: number; magazines: number }[];
  steelMin: number; steelMinAt: number; copperMin: number; coalMin: number;
  /** North's claim minute in seconds: constants.HOUR's (40:00, D-HOUR-1) unless the harness's two-claim variant
   *  (E-hour-north) hands it a later one. */
  northAt: number;
  /** M6 check: the threat's counters at the first red HQ pip (spawned so far, arrivals at a Held edge so far). */
  atFirstRed: { t: number; spawned: number; arrivals: number } | null;
  /** M6 check: when the chest's coal first read zero (-1: never). */
  coalZeroAt: number;
}

/** GAME-ASSUMPTION (M6): the bot's numbers where §11 gives none — 20 steel hand-mined (ten magazines' worth), ten
 *  magazines crafted, a full load of kits a claim, a rounds run every five minutes from
 *  minute 10 (20 magazines and 50 coal a trip, turrets and Generators at or under half topped up); the hand-feed
 *  beat (D-P4-8) fires on a red HQ pip at most once a minute and tops up the turrets at or under half. */
/** A kit costs KIT_STACKS of the engineer's INV_STACKS pockets (D5), so four is every kit one trip can hold and the
 *  claim step empties the pockets into the chest first to get all four. A claimed block whose ring is born with more
 *  than HOUR_KITS edges cannot be fully kitted in one trip: kits are spent at the instant an edge is born (D-B1-4),
 *  so a second trip cannot fix it. `kitsTask` logs that as a refusal rather than letting it pass in silence. */
export const HOUR_KITS = Math.floor(INV_STACKS / KIT_STACKS);
export const HOUR_MINE_STEEL = 20, HOUR_CRAFT_MAGS = 10, HOUR_RUN_FROM = 10 * 60, HOUR_RUN_GAP = 5 * 60;
export const HOUR_RUN_MAGS = 20, HOUR_RUN_COAL = 50, HOUR_FEED_GAP = 60;
/** D-P4-7 / D-HOUR-1: the second steel Excavator, belted into the Depot chest, at constants.HOUR's minute (15), placed
 *  before Generator 3 at the same minute so the chest never reaches zero. (GA-EF-1's 12:00 was withdrawn by the
 *  economy-fix task, Step 2: the minute is the decided row's.) GAME-ASSUMPTION: a second copper Excavator into the
 *  chest at 46:00 — the chest's 100 copper is 8 short of the hour's machines and claims, and a copper line into the
 *  chest from minute 12 would mine the 1,200-unit patch out under the ammo line by minute 40. */
export const HOUR_STEEL2_AT = HOUR_STEEL2_MIN * 60, HOUR_COPPER2_AT = HOUR_COPPER2_MIN * 60, HOUR_ASM3_AT = HOUR_ASM3_MIN * 60;
/** §11's end state the report checks: four Generators, seven Excavators (steel ×2, copper ×2, coal, two E4 stand-ins),
 *  three Assemblers, the six start turrets (none carried to a claim — D-P4-9 keeps the block-level hopper through
 *  Gate B), three blocks Held: the HQ and the two claims constants.HOUR puts inside the hour (east 15, west 25 —
 *  D-HOUR-1; north's 65 is past it, D-P4-10). */
export const HOUR_END = { generators: 4, excavators: 7, assemblers: 3, turrets: START_TURRETS, held: 3 } as const;
/** The claim minutes and the Generator minutes, from constants.HOUR (D-HOUR-1, D-P4-7). */
export const HOUR_CLAIM_AT: Record<HourDir, number> = { east: HOUR_CLAIM_MIN.east * 60, west: HOUR_CLAIM_MIN.west * 60, north: HOUR_CLAIM_MIN.north * 60 };
export const HOUR_GEN_AT = HOUR_GENERATOR_MIN.map(m => m * 60);

// ------------------------------------------------------------------ helpers

function note(bot: HourBot, st: SimState, what: string): void { bot.log.entries.push({ t: st.t, what }); }
function mark(bot: HourBot, st: SimState, name: string, t = st.t): void {
  if (bot.log.marks[name] !== undefined) return;
  bot.log.marks[name] = t; note(bot, st, name);
}
function refused(bot: HourBot, st: SimState, what: string, reason: string): void { bot.log.refused.push({ t: st.t, what, reason }); note(bot, st, `refused: ${what} — ${reason}`); }

/** A tile the engineer can stand on next to a rect, nearest to where it is (reach 8 covers it from any side). */
function standTile(st: SimState, x: number, y: number, size: number): [number, number] {
  const e = st.engineer;
  let best: [number, number] | null = null, bd = Infinity;
  for (let r = 1; r <= 3 && !best; r++) {
    for (let ty = y - r; ty < y + size + r; ty++) for (let tx = x - r; tx < x + size + r; tx++) {
      if (tx > x - r && tx < x + size + r - 1 && ty > y - r && ty < y + size + r - 1) continue;   // the ring only
      if (!passable(st, tx, ty)) continue;
      const d = Math.hypot(tx + 0.5 - e.x, ty + 0.5 - e.y);
      if (d < bd) { bd = d; best = [tx, ty]; }
    }
  }
  return best ?? [x, y];
}

/** The steel patch tile with units left nearest the Depot. */
function steelTile(st: SimState): [number, number] | null {
  const p = HQ_PATCHES.find(q => q.type === P_STEEL)!;
  const [dx, dy] = hqLot(st, DEPOT_LOT + DEPOT_TILES / 2, DEPOT_LOT + DEPOT_TILES / 2);
  let best: [number, number] | null = null, bd = Infinity;
  for (let ly = p.ly; ly < p.ly + p.h; ly++) for (let lx = p.lx; lx < p.lx + p.w; lx++) {
    const [tx, ty] = hqLot(st, lx, ly);
    if (!rubbleAt(st, tx, ty)) continue;
    const d = Math.hypot(tx - dx, ty - dy);
    if (d < bd) { bd = d; best = [tx, ty]; }
  }
  return best;
}

/** The compass direction of block `to` from block `from` by their lots' centres. */
export function dirOf(st: SimState, from: number, to: number): 'north' | 'east' | 'south' | 'west' {
  const G = ground(st), a = G.blocks[from], b = G.blocks[to];
  const dx = (b.x0 + b.x1) / 2 - (a.x0 + a.x1) / 2, dy = (b.y0 + b.y1) / 2 - (a.y0 + a.y1) / 2;
  return Math.abs(dx) >= Math.abs(dy) ? (dx > 0 ? 'east' : 'west') : (dy > 0 ? 'south' : 'north');
}

/** The HQ's Dark candidate neighbour most in that direction (§11's east / west / north), -1 if none. */
export function neighbourToward(st: SimState, dir: HourDir): number {
  const hq = hqIdx(st), G = ground(st), a = G.blocks[hq];
  const ax = (a.x0 + a.x1) / 2, ay = (a.y0 + a.y1) / 2;
  let best = -1, bs = -Infinity;
  for (const j of st.nb[hq]) {
    const b = st.blocks[j];
    if (b.state !== DARK || !isCandidate(st, j)) continue;
    const g = G.blocks[j], dx = (g.x0 + g.x1) / 2 - ax, dy = (g.y0 + g.y1) / 2 - ay, L = Math.hypot(dx, dy) || 1;
    const s = dir === 'east' ? dx / L : dir === 'west' ? -dx / L : -dy / L;
    if (s > bs) { bs = s; best = j; }
  }
  return best;
}

/** Where a walk to a block ends for the bot: the passable lot tile nearest its pole of inaccessibility (rubble is walkable). */
function blockStand(st: SimState, i: number): [number, number] {
  const G = ground(st), bg = G.blocks[i], [px, py] = bg.pole;
  let best: [number, number] = [px, py], bd = Infinity;
  for (let k = 0; k < bg.tiles.length; k++) {
    const t = bg.tiles[k], tx = t % G.tw, ty = (t - tx) / G.tw;
    if (!passable(st, tx, ty)) continue;
    const d = Math.hypot(tx - px, ty - py);
    if (d < bd) { bd = d; best = [tx, ty]; }
  }
  return best;
}

function kitted(st: SimState, i: number): boolean {
  if (st.blocks[i].state !== HELD) return false;
  for (const ed of st.ring) if (ed.a === i && ed.kit === false) return false;
  return true;
}

/** Every turret standing on a Held block. Through Gate B that is the HQ's six alone (D-P4-9), but the beat watches
 *  every Held block on purpose: a turret standing on a claim takes its edge off the ring feed (`hookSyncEdges`: an
 *  edge with physical turrets fires only through them), so the hands become the only thing that fills it — a beat
 *  that watched the HQ alone left north's carried turrets empty for minutes when §11 still carried them. */
const heldTurrets = (st: SimState): Machine[] => {
  const G = ground(st);
  return st.flow!.machines.filter(m => m.kind === 'turret' && st.blocks[G.owner[m.y * G.tw + m.x]]?.state === HELD);
};

// ------------------------------------------------------------------ task builders

const chest = (label: string, st: SimState): Task => { const d = depotRect(st); return goto(label, d.x, d.y, d.size, label); };

/** Take from the chest; what the chest cannot give is a refusal in the log (D-P4-4's start stock). */
function takeTask(bot: HourBot, wants: Partial<Record<'steel' | 'copper' | 'coal' | 'magazine' | 'kit', number>>): Task {
  return act('take from the chest', (st, out) => {
    for (const [item, n] of Object.entries(wants) as ['steel' | 'copper' | 'coal' | 'magazine' | 'kit', number][]) {
      if (n <= 0) continue;
      const have = chestCount(st, item);
      if (have <= 0) refused(bot, st, `take ${n} ${item}`, 'the chest has none');
      else if (have < n) note(bot, st, `took ${have} of ${n} ${item} (the chest had no more)`);
      out.push({ type: 'chestTake', item, n: Math.min(n, have) });
    }
  });
}

/** D-B1-4: a kit is spent at the instant an edge is born, so a claim has one trip to carry one per edge. A kit costs
 *  KIT_STACKS of INV_STACKS, so the surplus (rubble and magazines) goes back into the chest first to make room for
 *  all HOUR_KITS of them; what the pockets end up holding is written down, because fewer kits than edges is a block
 *  that can never be fully kitted. */
function kitsTask(bot: HourBot): Task[] {
  return [
    act('the pockets into the chest, then the kits', (st, out) => {
      for (const item of ['steel', 'copper', 'stone', 'coal', 'magazine'] as const) {
        const n = st.engineer.inv[item] ?? 0;
        if (n > 0) out.push({ type: 'chestPut', item, n });
      }
      out.push({ type: 'chestTake', item: 'kit', n: HOUR_KITS });
    }),
    act('kits carried', st => {
      const k = st.engineer.inv.kit ?? 0;
      note(bot, st, `${k} kit(s) in the pockets (${invStacks(st.engineer.inv)} of ${invCap(st.engineer)} stacks used)`);
      if (k < HOUR_KITS) refused(bot, st, `take ${HOUR_KITS} kits`, `only ${k} fit — ${invStacks(st.engineer.inv)} of ${invCap(st.engineer)} stacks are full`);
    }),
  ];
}

/** Walk within reach of a lot position and place there; a refusal (reach, cost, the tile) is logged, never forced. */
function put(bot: HourBot, kind: Kind, lx: number, ly: number, dir: Dir): Task[] {
  return [act(`${kind} at lot (${lx},${ly})`, st => {
    const [tx, ty] = hqLot(st, lx, ly);
    return [goto(kind, tx, ty, MACHINE_SIZE[kind]), act(`place ${kind}`, (st2, out) => {
      const chk = canPlace(st2, kind, tx, ty);
      if (!chk.ok) { refused(bot, st2, `${kind} at lot (${lx},${ly})`, chk.reason); return; }
      out.push({ type: 'place', item: kind, x: tx, y: ty, dir });
    })];
  })];
}

/** Like `put`, but the spot is the nearest placeable one to lot (lx,ly) within `r` (Chebyshev rings, west-to-east then
 *  north-to-south inside a ring), so the E4 stand-ins and the 45:00 machines find room on every seed: a start turret,
 *  the substation or the rubble under the home spot moves them a tile or two, never off the lot. The reason logged
 *  when nothing within `r` fits is the home spot's. */
function putNear(bot: HourBot, kind: Kind, lx: number, ly: number, dir: Dir, r = 6): Task[] {
  return [act(`${kind} near lot (${lx},${ly})`, st => {
    const size = MACHINE_SIZE[kind];
    let spot: [number, number] | null = null;
    for (let d = 0; d <= r && !spot; d++) for (let dy = -d; dy <= d && !spot; dy++) for (let dx = -d; dx <= d && !spot; dx++) {
      if (Math.max(Math.abs(dx), Math.abs(dy)) !== d) continue;
      const ax = lx + dx, ay = ly + dy;
      if (ax < 0 || ay < 0 || ax + size > LOT_TILES || ay + size > LOT_TILES) continue;
      const [tx, ty] = hqLot(st, ax, ay);
      if (!placeable(st, kind, tx, ty)) spot = [tx, ty];
    }
    if (!spot) { const [hx, hy] = hqLot(st, lx, ly); refused(bot, st, `${kind} near lot (${lx},${ly})`, placeable(st, kind, hx, hy) || 'no room within reach of the spot'); return; }
    const [tx, ty] = spot, [hx, hy] = hqLot(st, lx, ly);
    if (tx !== hx || ty !== hy) note(bot, st, `${kind} moved from lot (${lx},${ly}) to (${lx + tx - hx},${ly + ty - hy}): ${placeable(st, kind, hx, hy)}`);
    return [goto(kind, tx, ty, size), act(`place ${kind}`, (st2, out) => {
      const chk = canPlace(st2, kind, tx, ty);
      if (!chk.ok) { refused(bot, st2, `${kind} at lot (${lx},${ly})`, chk.reason); return; }
      out.push({ type: 'place', item: kind, x: tx, y: ty, dir });
    })];
  })];
}

/** Hand-feed a machine from the pockets (the `feed` command: E on the machine). */
const feedTask = (kind: string, tx: number, ty: number): Task => act(`feed the ${kind}`, (_st, out) => { out.push({ type: 'feed', x: tx, y: ty }); });

function putGenerator(bot: HourBot, n: number, lx: number, ly: number): Task[] {
  const before = (st: SimState) => st.flow!.machines.filter(m => m.kind === 'generator').length;
  let n0 = 0;
  return [act('count Generators', st => { n0 = before(st); }), ...putNear(bot, 'generator', lx, ly, 0), act(`coal into Generator ${n}`, st => {
    const gens = st.flow!.machines.filter(m => m.kind === 'generator');
    if (gens.length <= n0) return;
    const g = gens[gens.length - 1];
    mark(bot, st, `generator-${n}`);
    return [feedTask('Generator', g.x, g.y)];
  })];
}

/** M4's proven HQ-lot line (the E4-doc layout `$S/m4measure.ts` placed): the coal Excavator with belts into the
 *  Depot, Generator 2, then the steel and copper Excavators with belts to the Shot assembler. D-P4-7: under plan (a)
 *  Generator 2 waits for the coal Excavator's first 40 units to reach the chest; under (b) it takes the chest's 40. */
function excavatorLine(bot: HourBot): Task[] {
  const t: Task[] = [];
  t.push(...put(bot, 'excavator', 18, 14, 3), ...put(bot, 'belt', 17, 15, 3), ...put(bot, 'belt', 16, 15, 3), ...put(bot, 'belt', 15, 15, 0), ...put(bot, 'belt', 15, 14, 3));   // coal → Depot
  if (bot.coalPlan === 'wait') {
    t.push(until('40 coal in the chest from the Excavator', st => chestCount(st, 'coal') >= GENERATOR_COAL_CAP - 10, 240),
      act('coal for Generator 2', st => [chest('to the chest for coal', st), takeTask(bot, { coal: Math.min(GENERATOR_COAL_CAP, chestCount(st, 'coal')) })]));
  }
  t.push(...putGenerator(bot, 2, 16, 8));
  t.push(...put(bot, 'excavator', 1, 7, 0)); for (let lx = 2; lx <= 21; lx++) t.push(...put(bot, 'belt', lx, 6, 1));
  t.push(...put(bot, 'belt', 22, 6, 2), ...put(bot, 'belt', 22, 7, 2), ...put(bot, 'belt', 22, 8, 2));
  t.push(...put(bot, 'excavator', 1, 14, 2)); for (let lx = 2; lx <= 20; lx++) t.push(...put(bot, 'belt', lx, 17, 1));
  t.push(...put(bot, 'belt', 21, 17, 0), ...put(bot, 'belt', 21, 16, 0), ...put(bot, 'belt', 21, 15, 0), ...put(bot, 'belt', 21, 14, 0));
  t.push(act('the line placed', st => { mark(bot, st, 'line-excavators'); }));
  return t;
}
/** D-P4-4: the second steel Excavator on the patch's south-east corner, two belts east into the Depot's west face. */
function steelToChest(bot: HourBot): Task[] {
  return [...put(bot, 'excavator', 4, 9, 1), ...put(bot, 'belt', 7, 10, 1), ...put(bot, 'belt', 8, 10, 1),
    act('the second steel Excavator feeds the chest', st => { mark(bot, st, 'steel-to-chest'); })];
}
/** The second copper Excavator beside the first, four belts into the Depot's west face (see HOUR_COPPER2_AT). */
function copperToChest(bot: HourBot): Task[] {
  return [...put(bot, 'excavator', 4, 14, 0), ...put(bot, 'belt', 5, 13, 1), ...put(bot, 'belt', 6, 13, 1), ...put(bot, 'belt', 7, 13, 1), ...put(bot, 'belt', 8, 13, 1),
    act('the second copper Excavator feeds the chest', st => { mark(bot, st, 'copper-2'); })];
}
function assemblerLine(bot: HourBot): Task[] {
  const t: Task[] = [];
  t.push(...put(bot, 'inserter', 22, 9, 2), ...put(bot, 'assembler', 21, 10, 0), ...put(bot, 'inserter', 21, 13, 0), ...put(bot, 'inserter', 20, 11, 3));
  for (let lx = 19; lx >= 15; lx--) t.push(...put(bot, 'belt', lx, 11, 3));
  t.push(act('the Shot line placed', st => { mark(bot, st, 'line-assembler'); }));
  return t;
}

/** The pip of the edge a turret serves ('green' for a turret with no edge). */
function turretPip(st: SimState, m: Machine): 'green' | 'amber' | 'red' {
  const id = turretEdge(st, m);
  if (id < 0) return 'green';
  const ed = st.ring[st.edgeAt[id]];
  return ed ? pipOf(ed.hopper / edgeCap(st, ed)) : 'green';
}
/** D-P4-8's hand-feed beat: a red pip on an HQ edge sends the bot to every HQ turret at or under half, worst pip first,
 *  E on each from the pockets (a chest stop first when the pockets are empty). */
function feedBeat(bot: HourBot, st: SimState): Task[] {
  const rank = { red: 0, amber: 1, green: 2 }, e = st.engineer;
  const low = heldTurrets(st).filter(m => (m.inv.rounds ?? 0) <= TURRET_HOPPER / 2)
    .sort((a, b) => rank[turretPip(st, a)] - rank[turretPip(st, b)] || (Math.hypot(a.x - e.x, a.y - e.y) - Math.hypot(b.x - e.x, b.y - e.y)));
  if (!low.length) return [];
  note(bot, st, `hand-feed beat: ${low.length} turret(s) at or under half, ${low.filter(m => turretPip(st, m) === 'red').length} on a red edge`);
  const t: Task[] = [];
  if ((st.engineer.inv.magazine ?? 0) < low.length * TURRET_HOPPER / SHOT.count) t.push(chest('to the chest for magazines', st), takeTask(bot, { magazine: Math.max(HOUR_RUN_MAGS, Math.ceil(low.length * TURRET_HOPPER / SHOT.count)) }));
  for (const m of low) t.push(goto('turret', m.x, m.y, m.size, 'hand-feed beat'), feedTask('turret', m.x, m.y));
  t.push(act('fed', st2 => { mark(bot, st2, 'first-hand-feed'); if (bot.fedAtFeedDone < 0) bot.fedAtFeedDone = st2.flow!.stats.handFed; note(bot, st2, `${st2.flow!.stats.handFed} magazines fed by hand so far`); }));
  return t;
}
/** A red pip on an HQ edge, the pockets or the chest holding magazines, and a minute since the last beat. */
function feedDue(bot: HourBot, st: SimState): boolean {
  if (st.t - bot.lastFeed < HOUR_FEED_GAP) return false;
  for (const ed of st.ring) {
    if (st.blocks[ed.a].state !== HELD || ed.kit === false || !ed.turrets) continue;   // ring-fed edges need no hands
    if (pipOf(ed.hopper / edgeCap(st, ed)) === 'red') return (st.engineer.inv.magazine ?? 0) > 0 || chestCount(st, 'magazine') > 0;
  }
  return false;
}

/** Claim a direction from the map (a click), take the kits, walk over, stand there until the block is Held and its edges kitted, walk back. */
function claimStep(bot: HourBot, dir: HourDir): Task[] {
  return [
    act(`claim ${dir}`, (st, out) => {
      const i = neighbourToward(st, dir);
      if (i < 0) { refused(bot, st, `claim ${dir}`, 'no Dark candidate that way'); return; }
      const b = st.blocks[i];
      const c = st.config.eco.claimCost;
      if (st.config.economy && (st.stock.copper < c.copper || st.stock.steel < c.steel)) refused(bot, st, `claim ${dir}`, `stock ${st.stock.steel | 0} steel, ${st.stock.copper | 0} Cu`);
      bot.claimed[dir] = i;
      out.push({ type: 'claim', x: b.x, y: b.y });
      mark(bot, st, `claim-${dir}`);
      note(bot, st, `${dir} = block ${i} (${b.name}, d ${b.d.toFixed(2)}, burn-off ${burnOffS(b.d).toFixed(0)} s)`);
      const [sx, sy] = blockStand(st, i);
      return [
        goto(`${dir}'s lot`, sx, sy, 1, `walk-over ${dir}`),
        act(`arrived on ${dir}`, st2 => { mark(bot, st2, `arrive-${dir}`); }),
        until(`${dir} Held and kitted`, st2 => kitted(st2, i), burnOffS(b.d) + 180),
        act(`${dir} kitted`, st2 => {
          if (kitted(st2, i)) { mark(bot, st2, `kitted-${dir}`); return; }
          const mine = st2.ring.filter(e => e.a === i), un = mine.filter(e => e.kit === false);
          if (un.length) refused(bot, st2, `kit ${dir}'s ring`, `${un.length} of ${mine.length} edges were born unkitted (${st2.engineer.inv.kit ?? 0} kits left) — a kit is spent at the instant an edge is born`);
        }),
        chest(`back from ${dir}`, st),
      ];
    }),
  ];
}

/** The five-minute rounds run: magazines and coal from the chest, every turret and Generator at or under half topped up. */
function roundsRun(bot: HourBot, st: SimState): Task[] {
  const low = st.flow!.machines.filter(m => (m.kind === 'turret' && (m.inv.rounds ?? 0) <= TURRET_HOPPER / 2) || (m.kind === 'generator' && (m.inv.coal ?? 0) <= GENERATOR_COAL_CAP / 2));
  if (!low.length) return [];
  note(bot, st, `rounds run: ${low.filter(m => m.kind === 'turret').length} turrets, ${low.filter(m => m.kind === 'generator').length} Generators at or under half`);
  const nT = low.filter(m => m.kind === 'turret').length, nG = low.length - nT;
  const t: Task[] = [chest('to the chest', st), takeTask(bot, { magazine: Math.max(HOUR_RUN_MAGS, nT * TURRET_HOPPER / SHOT.count), coal: Math.max(HOUR_RUN_COAL, nG * GENERATOR_COAL_CAP) })];
  for (const m of low) t.push(goto(m.kind, m.x, m.y, m.size, 'rounds run'), feedTask(m.kind, m.x, m.y));
  t.push(act('rounds run done', st2 => { mark(bot, st2, 'first-rounds-run'); }));
  return t;
}

// ------------------------------------------------------------------ §11's minute list

export function hourSteps(bot: HourBot): HourStep[] {
  const gen = (n: number, lx: number, ly: number): HourStep => ({ at: HOUR_GEN_AT[n - 1], name: `§11 ${HOUR_GEN_AT[n - 1] / 60}:00 — Generator ${n}`, tasks: st => [
    chest('to the chest', st), takeTask(bot, { steel: MACHINE_COST.generator.steel, copper: MACHINE_COST.generator.copper, coal: GENERATOR_COAL_CAP }), ...putGenerator(bot, n, lx, ly)] });
  const standIn = (at: number, name: string, kinds: [Kind, number, number][]): HourStep => ({ at, name, tasks: st => {
    const steel = kinds.reduce((s, k) => s + MACHINE_COST[k[0]].steel, 0), copper = kinds.reduce((s, k) => s + MACHINE_COST[k[0]].copper, 0);
    const t: Task[] = [chest('to the chest', st), takeTask(bot, { steel, copper })];
    for (const [k, lx, ly] of kinds) t.push(...putNear(bot, k, lx, ly, 0));
    return t;
  } });
  const claimMin = (dir: HourDir): number => dir === 'north' ? bot.northAt : HOUR_CLAIM_AT[dir];
  const claimAt = (dir: HourDir): HourStep => ({ at: claimMin(dir), name: `§11 ${claimMin(dir) / 60}:00 — claim ${dir}`, tasks: st => [
    chest('to the chest for kits', st), ...kitsTask(bot), ...claimStep(bot, dir)] });
  const burnE = burnOffS(0.22);
  return [
    { at: 0, name: '§11 0:00 — to the steel patch, hand-mine 20 steel', tasks: st => {
      const p = steelTile(st);
      if (!p) { refused(bot, st, 'mine steel', 'no steel patch tile with units'); return []; }
      const mine = (st2: SimState, out: Command[]) => { const q = steelTile(st2); if (q) out.push({ type: 'mineAt', x: q[0], y: q[1] }); };
      return [goto('the steel patch', p[0], p[1], 1, 'to the steel patch'), act('mine', mine),
        until(`${HOUR_MINE_STEEL} steel in the pockets`, st2 => (st2.engineer.inv.steel ?? 0) >= HOUR_MINE_STEEL, HOUR_MINE_STEEL * 3,
          (st2, out) => { if (!st2.flow!.hand.mine && st2.flow!.tick % TILE_TPS === 0) mine(st2, out); }),
        // The hands keep digging the tile until they are told to stop (worldScene sends (-1,-1) on mouse-up), so
        // §11's "hand-mine 20 steel" ends with the hands off: without it the engineer mines the whole tile out and
        // carries ~290 steel round the hour, and the pockets it fills have no room left for a claim's kits.
        act('mined', (st2, out) => { out.push({ type: 'mineAt', x: -1, y: -1 }); mark(bot, st2, 'mine-done'); note(bot, st2, `${st2.engineer.inv.steel ?? 0} steel in the pockets`); })];
    } },
    { at: 0, name: '§11 — back to the workbench: craft ten magazines, the chest\'s 20 into the pockets', tasks: st => {
      let before = 0;
      return [
      chest('to the workbench', st),
      takeTask(bot, { copper: HOUR_CRAFT_MAGS }),
      act('craft', (st2, out) => { before = st2.flow!.stats.handCrafted; out.push({ type: 'craft', item: 'magazine', count: HOUR_CRAFT_MAGS }); }),
      until('the magazines crafted', st2 => st2.flow!.stats.handCrafted >= before + HOUR_CRAFT_MAGS, HOUR_CRAFT_MAGS * SHOT.seconds + 30),
      act('crafted', st2 => { mark(bot, st2, 'craft-done'); note(bot, st2, `${st2.flow!.stats.handCrafted - before} crafted, ${st2.engineer.inv.magazine ?? 0} magazines in the pockets`); }),
      takeTask(bot, { magazine: HOUR_RUN_MAGS }),
    ]; } },
    // D-P4-8: no feed at minute one — the hoppers are full; the hand-feed beat (feedBeat) answers the first red pip
    { at: HOUR_GEN_AT[1], name: '§11 6:00 — the coal Excavator and its belt, Generator 2, the steel and copper Excavators and their belts', tasks: st => {
      const steel = 3 * MACHINE_COST.excavator.steel + 50 * MACHINE_COST.belt.steel + MACHINE_COST.generator.steel;
      const coal = bot.coalPlan === 'chest' ? Math.min(GENERATOR_COAL_CAP, chestCount(st, 'coal')) : 0;
      return [chest('to the chest', st), takeTask(bot, { steel, copper: MACHINE_COST.generator.copper, coal }), ...excavatorLine(bot)];
    } },
    { at: HOUR_SHOT_LINE_MIN * 60, name: '§11 8:00 — the Shot assembler, three inserters, the ammo belt', tasks: st => {
      const steel = MACHINE_COST.assembler.steel + 3 * MACHINE_COST.inserter.steel + 5 * MACHINE_COST.belt.steel, copper = MACHINE_COST.assembler.copper + 3 * MACHINE_COST.inserter.copper;
      return [chest('to the chest', st), takeTask(bot, { steel, copper }), ...assemblerLine(bot)];
    } },
    { at: HOUR_STEEL2_AT, name: `§11 ${HOUR_STEEL2_AT / 60}:00 — the second steel Excavator, belted into the chest (D-P4-7)`, tasks: st => [
      chest('to the chest', st), takeTask(bot, { steel: MACHINE_COST.excavator.steel + 2 * MACHINE_COST.belt.steel }), ...steelToChest(bot)] },
    gen(3, 22, 3),
    claimAt('east'),
    standIn(HOUR_CLAIM_AT.east + burnE + 60, 'stand-in Excavator for east\'s copper (E4, D-P4-5)', [['excavator', 7, 1]]),
    standIn(HOUR_CLAIM_AT.east + burnE + 120, 'stand-in Assembler (E4\'s wire recipe, D-P4-5)', [['assembler', 11, 1]]),
    claimAt('west'),
    standIn(HOUR_CLAIM_AT.west + burnE + 60, 'stand-in Excavator for west\'s coal (E4, D-P4-5)', [['excavator', 3, 1]]),
    gen(4, 15, 3),
    // the second copper Excavator waits for Generator 4: three Generators carry 900 kW and the line plus the stand-ins
    // already draw 890, so a seventh Excavator before the fourth Generator is the 38:00 brownout E-hour found
    { at: HOUR_COPPER2_AT, name: `§11 ${HOUR_COPPER2_AT / 60}:00 — the second copper Excavator, belted into the chest`, tasks: st => [
      chest('to the chest', st), takeTask(bot, { steel: MACHINE_COST.excavator.steel + 4 * MACHINE_COST.belt.steel }), ...copperToChest(bot)] },
    // the third Assembler (E4's stand-in) waits for the second copper Excavator's first units: the chest's copper sits
    // at 27 from the east claim to 46:00 (the line's copper Excavator feeds the Shot assembler, not the chest)
    standIn(HOUR_ASM3_AT, `§11 ${HOUR_ASM3_AT / 60}:00 — the third Assembler (E4 stand-in)`, [['assembler', 15, 0]]),
    // north's minute is constants.HOUR's (65, D-P4-10 (a)): past the hour, so a 3,600 s run never reaches it and
    // the hour is two claims. No turret is carried over — through Gate B a claim's edges keep the block sim's
    // ring-fed hopper and nothing physical stands on them (D-P4-9); the carry comes back with "every front edge
    // physical" after Gate B.
    claimAt('north'),
  ];
}

// ------------------------------------------------------------------ the bot

export function createHourBot(rifle = false, coalPlan: 'chest' | 'wait' = 'chest', northAt = HOUR_CLAIM_AT.north): HourBot {
  const bot: HourBot = { rifle, log: { entries: [], marks: {}, walks: [], refused: [] }, steps: [], next: 0, queue: [], claimed: {}, lastRun: HOUR_RUN_FROM - HOUR_RUN_GAP, aiming: false, fellWhy: {}, handsOff: false, ticks: 0, fedAtFeedDone: -1,
    coalPlan, lastFeed: -Infinity, stock: [], steelMin: Infinity, steelMinAt: -1, copperMin: Infinity, coalMin: Infinity, northAt, atFirstRed: null, coalZeroAt: -1 };
  bot.steps = hourSteps(bot).sort((a, b) => a.at - b.at);
  return bot;
}

/** What the bot watches every call: §11's moments the state shows on its own. */
function watch(st: SimState, bot: HourBot): void {
  const f = st.flow!, e = st.engineer, hq = hqIdx(st);
  for (const dir of DIRS) {
    const i = bot.claimed[dir];
    if (i === undefined) continue;
    if (st.blocks[i].state === HELD) mark(bot, st, `held-${dir}`);
    if (st.fallen[i]) mark(bot, st, `fell-${dir}`);
  }
  if (survivorJoined(st, 'Electricians')) mark(bot, st, 'electricians');
  if (e.firstShot >= 0) mark(bot, st, 'first-shot', e.firstShot);
  if (threatActive(st)) {
    const T = threatOf(f);
    if (T.stats.spawned > 0) mark(bot, st, 'first-crawler');
    if (T.stats.shades > 0) mark(bot, st, 'first-shade');
    if (T.stats.turned > 0) mark(bot, st, 'first-retaliation');
  }
  if (f.stats.fired > 0) mark(bot, st, 'first-turret-fire');
  if (st.blocks[hq].state !== HELD) mark(bot, st, 'hq-fell');
  if (st.stats.firstInterior >= 0) mark(bot, st, 'enclosure', st.stats.firstInterior);
  if (f.power.overS > 0) mark(bot, st, 'first-brownout');
  const gens = f.machines.filter(m => m.kind === 'generator');
  if (gens.length && gens.every(m => (m.inv.coal ?? 0) <= 0)) mark(bot, st, 'generators-dry');
  if (f.stats.magsMade > 0) mark(bot, st, 'first-line-magazine');
  if (f.tick % TILE_TPS === 0) {
    for (const ed of st.ring) {
      if (ed.born === st.t || ed.kit === false) continue;
      const p = pipOf(ed.hopper / edgeCap(st, ed));
      if (p !== 'green') mark(bot, st, 'first-amber');
      if (p === 'red') {
        if (bot.atFirstRed === null) { const T = threatOf(st.flow!); bot.atFirstRed = { t: st.t, spawned: T.stats.spawned, arrivals: T.stats.arrivals }; }
        mark(bot, st, 'first-red');
      }
    }
    const steel = chestCount(st, 'steel'), copper = chestCount(st, 'copper'), coal = chestCount(st, 'coal');
    if (steel < bot.steelMin) { bot.steelMin = steel; bot.steelMinAt = st.t; }
    if (copper < bot.copperMin) bot.copperMin = copper;
    if (coal < bot.coalMin) bot.coalMin = coal;
    if (coal <= 0 && bot.coalZeroAt < 0) bot.coalZeroAt = st.t;
    if (steel <= 0) mark(bot, st, 'steel-zero');
    if (copper <= 0) mark(bot, st, 'copper-zero');
    if (f.tick % (60 * TILE_TPS) === 0) bot.stock.push({ t: st.t, steel, copper, coal, magazines: chestCount(st, 'magazine') });
  }
}

/** The rifle in hand: with magazines in the pockets, aim at the nearest crawler within range; release when none. */
function reflex(st: SimState, bot: HourBot, out: Command[]): void {
  const e = st.engineer;
  let at: [number, number] | null = null;
  if ((e.inv.magazine ?? 0) > 0 && threatActive(st)) {
    let bd = RIFLE_RANGE;
    for (const c of threatOf(st.flow!).crawlers) { const d = Math.hypot(c.x - e.x, c.y - e.y); if (d < bd) { bd = d; at = [c.x, c.y]; } }
  }
  if (at) { out.push({ type: 'aim', at }); bot.aiming = true; }
  else if (bot.aiming) { out.push({ type: 'aim', at: null }); bot.aiming = false; }
}

/** E-rifle at tile scale (the rescue): the bot drops its script and its hands and walks to stand within reach of a
 *  tile (a dry edge's street midpoint); with `rifle` the reflex fires at the nearest crawler in range from there,
 *  without it the engineer just stands. Nothing else of the bot is left: no feed beat, no rounds run, no steps. */
export function rescueStance(st: SimState, bot: HourBot, tx: number, ty: number, rifle: boolean): void {
  bot.steps = []; bot.queue = []; bot.next = 0; bot.handsOff = true; bot.rifle = rifle;
  bot.queue.push(goto('the dry edge', tx, ty, 1, 'to the rescue'));
}

/** One call a frame (or a tick): the bot's commands for this frame. Needs the flow layer (a city with tiles). */
export function hourCommands(st: SimState, bot: HourBot, out: Command[]): void {
  if (!st.flow || st.lattice) return;
  bot.ticks++;
  watch(st, bot);
  if (bot.rifle) reflex(st, bot, out);
  const e = st.engineer;
  if (e.down >= 0) return;
  // D-P4-8: the hand-feed beat pre-empts between walks (never mid-wait: an `until` head means the bot is standing on a claim)
  if (!bot.handsOff && (!bot.queue.length || bot.queue[0].kind === 'goto') && feedDue(bot, st)) {
    bot.lastFeed = st.t;
    const t = feedBeat(bot, st);
    if (t.length) { if (bot.queue[0]?.kind === 'goto') bot.queue[0].sent = false; bot.queue.unshift(...t); }
  }
  if (!bot.queue.length) {
    const s = bot.steps[bot.next];
    if (s && st.t >= s.at) {
      bot.next++;
      const late = st.t - s.at;
      note(bot, st, `${s.name}${late > 5 ? ` (${late.toFixed(0)} s late)` : ''}`);
      bot.queue.push(...s.tasks(st, bot));
    } else if (!bot.handsOff && st.t >= HOUR_RUN_FROM && st.t - bot.lastRun >= HOUR_RUN_GAP) {
      bot.lastRun = st.t;
      bot.queue.push(...roundsRun(bot, st));
    }
  }
  const task = bot.queue[0];
  if (!task) return;
  if (task.kind === 'goto') {
    if (inReach(st, task.x, task.y, task.size)) {
      if (task.walk && task.sent) bot.log.walks.push({ name: task.walk, t0: task.t0, t1: st.t, tiles: task.tiles });
      bot.queue.shift(); return;
    }
    if (!task.sent) {
      const [gx, gy] = standTile(st, task.x, task.y, task.size);
      const p = findPath(st, Math.floor(e.x), Math.floor(e.y), gx, gy);
      if (task.tries === 0) { task.tiles = p ? p.length : -1; task.t0 = st.t; }
      task.sent = true; task.tries++;
      out.push({ type: 'move', x: gx + 0.5, y: gy + 0.5 });
      return;
    }
    // the walk ended (arrival, a dropped click, a knock-down) short of reach: one more try, then a refusal
    if (e.target === null && st.t > task.t0) {
      if (task.tries < 2) { task.sent = false; return; }
      if (task.walk) bot.log.walks.push({ name: task.walk, t0: task.t0, t1: st.t, tiles: task.tiles });
      refused(bot, st, `walk to ${task.label}`, task.tiles < 0 ? 'no path' : 'the walk ended out of reach'); bot.queue.shift();
    } else if (st.t - task.t0 > 300) { refused(bot, st, `walk to ${task.label}`, 'timed out'); out.push({ type: 'move', x: e.x, y: e.y }); bot.queue.shift(); }
    return;
  }
  if (task.kind === 'do') {
    bot.queue.shift();
    const more = task.fn(st, out);
    if (more && more.length) bot.queue.unshift(...more);
    return;
  }
  if (task.at < 0) task.at = st.t;
  task.each?.(st, out);
  if (task.pred(st)) { bot.queue.shift(); return; }
  if (st.t - task.at >= task.secs) { refused(bot, st, `wait for ${task.label}`, `timed out after ${task.secs.toFixed(0)} s`); bot.queue.shift(); }
}

// ------------------------------------------------------------------ the report

export interface HourReport {
  marks: Record<string, number>;
  walks: HourWalk[];
  refused: HourRefusal[];
  entries: HourEntry[];
  /** Seconds walked in the hour, the share of the hour, chest trips, magazines/coal fed by hand, reach refusals. */
  walkedS: number; walkedPct: number; chestTrips: number; handFed: number; reachRefused: number;
  /** The claim walk-overs' seconds (§11: 2–8 s to east's kerb; the rework: under a minute of hour one for all of them). */
  claimWalkS: number;
  held: number; hqHeld: boolean; turrets: number; generators: number; excavators: number; assemblers: number;
  brownoutS: number; crawlers: number; shades: number; turretKills: number; rifleKills: number; fired: number; magsMade: number;
  /** D-P4-4: the chest's steel curve by the minute and its minimum; copper and coal minima; whether any block fell. */
  stock: HourBot['stock']; steelMin: number; steelMinAt: number; copperMin: number; coalMin: number; fell: number; fellWhy: Partial<Record<HourDir, string>>;
  atFirstRed: HourBot['atFirstRed']; coalZeroAt: number;
  /** Findings: every divergence from §11's prose and from the calibration timeline, one line each. */
  findings: string[];
}

/** mm:ss, or `never` for an absent mark. */
export const mmss = (s: number | undefined): string => s === undefined || s < 0 ? 'never' : `${Math.floor(s / 60)}:${String(Math.floor(s % 60)).padStart(2, '0')}`;
const mm = mmss;

/** §11's expectations and the calibration's bands, checked against the log. `hours` scales nothing: the hour is the hour. */
export function hourReport(st: SimState, bot: HourBot): HourReport {
  const f = ensureFlow(st), m = bot.log.marks, e = st.engineer, hq = hqIdx(st);
  const T = threatActive(st) ? threatOf(f) : null;
  const elapsed = Math.max(1, st.t);
  const walkedS = e.walked, walkedPct = 100 * walkedS / elapsed;
  const claimWalkS = bot.log.walks.filter(w => w.name.startsWith('walk-over')).reduce((s, w) => s + (w.t1 - w.t0), 0);
  const count = (k: Kind) => f.machines.filter(x => x.kind === k).length;
  const findings: string[] = [];
  const expect = (name: string, lo: number, hi: number, what: string) => {
    const t = m[name];
    if (t === undefined) findings.push(`${what}: never happened (§11/calibration: ${mm(lo)}–${mm(hi)})`);
    else if (t < lo || t > hi) findings.push(`${what}: at ${mm(t)} (§11/calibration: ${mm(lo)}–${mm(hi)})`);
  };
  // §11 0–10 min
  expect('mine-done', 0, 3 * 60, 'steel hand-mined');
  expect('craft-done', 0, 5 * 60, 'magazines crafted at the workbench');
  expect('first-hand-feed', 4 * 60, 10 * 60, 'the hand-feed beat (D-P4-8: the ~6-minute red pip)');
  if (m['first-hand-feed'] !== undefined && bot.fedAtFeedDone === 0) findings.push('the hand-feed beat moved no magazines (the turrets it walked to were full)');
  expect('first-crawler', 60, 6 * 60, 'the first bloom (§11: north at about minute 3)');
  expect('line-excavators', 0, 8 * 60, 'Excavators and belts placed');
  expect('line-assembler', 0, 10 * 60, 'the Shot line placed (§11: ammo automated by minute 10)');
  expect('generator-2', 5 * 60, 8 * 60, 'the second Generator (§11: minute 6)');
  expect('first-line-magazine', 0, 12 * 60, 'the first magazine off the line');
  // §11 10–30 min
  expect('claim-east', 10 * 60, 30 * 60, 'east claimed');
  expect('held-east', 10 * 60, 32 * 60, 'east Held');
  const wo = bot.log.walks.find(w => w.name === 'walk-over east');
  if (wo) { const s = wo.t1 - wo.t0; if (s < 2 || s > 8) findings.push(`the walk over to east took ${s.toFixed(0)} s (§11: 2–8 s)`); }
  expect('generator-3', 12 * 60, 20 * 60, 'the third Generator (E4-doc: minute 15)');
  expect('claim-west', 15 * 60, 30 * 60, 'west claimed');
  // §11 30–60 min. North's claim, the HQ's white border (its third neighbour Held) and the Electricians walking out
  // of civic north all wait on north (D-P4-10 (a): minute 65), so a 3,600 s run reaches none of them — that is the
  // shape of the hour, two claims, not a divergence. None is expected until north has been claimed.
  if (st.t >= bot.northAt + 60) {
    expect('claim-north', bot.northAt, bot.northAt + 5 * 60, `north claimed (${bot.northAt === HOUR_CLAIM_AT.north ? `constants.HOUR: minute ${HOUR_CLAIM_AT.north / 60}, D-P4-10` : `the variant's ${bot.northAt / 60}:00`})`);
    expect('enclosure', bot.northAt, bot.northAt + 10 * 60, 'the HQ interior (§11: the white border when north holds)');
    expect('electricians', 0, bot.northAt + 10 * 60, 'the Electricians in the Depot');
  }
  expect('first-shade', 32 * 60, 47 * 60, 'the first shade (§11/E3: minute 32–47)');
  expect('generator-4', 42 * 60, 50 * 60, 'the fourth Generator (E4-doc: minute 45)');
  expect('copper-2', HOUR_COPPER2_AT, HOUR_COPPER2_AT + 3 * 60, 'the second copper Excavator (after Generator 4, so no brownout)');
  const gens = count('generator'), exc = count('excavator'), asm = count('assembler'), tur = count('turret');
  if (st.t >= 3600 - 1) {
    if (gens !== HOUR_END.generators) findings.push(`${gens} Generators at the hour (§11: ${HOUR_END.generators})`);
    if (exc !== HOUR_END.excavators) findings.push(`${exc} Excavators at the hour (§11: ${HOUR_END.excavators})`);
    if (asm !== HOUR_END.assemblers) findings.push(`${asm} Assemblers at the hour (§11: ${HOUR_END.assemblers})`);
    if (tur !== HOUR_END.turrets) findings.push(`${tur} turrets at the hour (§11: ${HOUR_END.turrets})`);
    if (f.power.overS > 0) findings.push(`${f.power.overS.toFixed(0)} s of brownout (§11: none)`);
  }
  if (m['steel-zero'] !== undefined) findings.push(`the chest's steel touched zero at ${mm(m['steel-zero'])} (D-P4-4: never)`);
  if (m['copper-zero'] !== undefined) findings.push(`the chest's copper touched zero at ${mm(m['copper-zero'])}`);
  // the rework's findings re-checked
  if (claimWalkS > 60) findings.push(`the claim walk-overs took ${claimWalkS.toFixed(0)} s in all (rework: under a minute of hour one)`);
  if (walkedPct > 15) findings.push(`walked ${walkedPct.toFixed(1)} % of the hour (§19's budget: 15 %)`);
  if (e.truckFound) findings.push('the truck was found inside the hour (rework: outside it)');
  // the calibration timeline (docs/experiments/lattice/calibration.md)
  expect('first-amber', 2 * 60, 6 * 60, 'the first amber pip (D-P4-8: the north bloom at about minute 3; the calibration\'s 16–31 was the lattice)');
  expect('first-red', 4 * 60, 10 * 60, 'the first red pip (D-P4-8: the ~6-minute hand-feed beat)');
  const held = st.blocks.filter(b => b.state === HELD).length;
  if (st.t >= 3600 - 1 && held !== HOUR_END.held) findings.push(`${held} blocks Held at the hour (§11: ${HOUR_END.held})`);
  if (m['hq-fell'] !== undefined) findings.push(`the HQ fell at ${mm(m['hq-fell'])}`);
  let fell = m['hq-fell'] !== undefined ? 1 : 0;
  for (const dir of DIRS) if (m[`fell-${dir}`] !== undefined) { fell++; findings.push(`${dir} fell at ${mm(m[`fell-${dir}`])} (${bot.fellWhy[dir] ?? '?'})`); }
  for (const r of bot.log.refused) findings.push(`refused at ${mm(r.t)}: ${r.what} — ${r.reason}`);
  return {
    marks: { ...m }, walks: bot.log.walks.slice(), refused: bot.log.refused.slice(), entries: bot.log.entries.slice(),
    walkedS, walkedPct, chestTrips: f.stats.chestTrips, handFed: f.stats.handFed, reachRefused: f.stats.reachRefused, claimWalkS,
    held, hqHeld: st.blocks[hq].state === HELD, turrets: tur, generators: gens, excavators: exc, assemblers: asm,
    brownoutS: f.power.overS, crawlers: T?.stats.spawned ?? 0, shades: T?.stats.shades ?? 0, turretKills: T?.stats.turretKills ?? 0,
    rifleKills: T?.stats.rifleKills ?? 0, fired: e.fired, magsMade: f.stats.magsMade, findings,
    stock: bot.stock.slice(), steelMin: bot.steelMin, steelMinAt: bot.steelMinAt, copperMin: bot.copperMin, coalMin: bot.coalMin, fell, fellWhy: { ...bot.fellWhy },
    atFirstRed: bot.atFirstRed, coalZeroAt: bot.coalZeroAt,
  };
}

// ------------------------------------------------------------------ the replay (Gate B: "did it matter?")

/** A command with the tile tick it was applied before (the session logs every player command this way). */
export interface LoggedCommand { tick: number; c: Command }

/** Re-run a logged session from a state built the same way (same seed, config, map): every command is applied
 *  before the tick it was logged at; `dropAim` leaves the rifle out. GAME-ASSUMPTION (M6): the tile sim is
 *  frame-independent — a frame's commands all land before its first tick and nothing else touches the state
 *  between ticks — so a replay at one tick a step reproduces the played run exactly; the scene's direct calls
 *  (E on a machine, the chest panel, R, the workbench) are logged as the commands they stand for. */
export function replay(st: SimState, log: readonly LoggedCommand[], untilTick: number, opts: { dropAim?: boolean; every?: (st: SimState) => void } = {}): SimState {
  const f = ensureFlow(st);
  st.speed = 1;
  let k = 0;
  const cmds: Command[] = [];
  while (f.tick < untilTick) {
    cmds.length = 0;
    while (k < log.length && log[k].tick <= f.tick) { const c = log[k++].c; if (c.type === 'setSpeed' || (opts.dropAim && c.type === 'aim')) continue; cmds.push(c); }
    st.acc = 0;
    advanceFlow(st, TILE_DT, cmds, 1);
    st.events.length = 0;
    opts.every?.(st);
  }
  return st;
}

export type Verdict = 'held anyway' | 'saved it' | 'fell anyway' | 'open';
export interface FightVerdict { t: number; edge: number; bx: number; by: number; rounds: number;
  /** The fight's own outcome in play (null while it was still running at the end). */ fightHeld: boolean | null;
  /** Whether the fight's block stood at the end of the played run and of the replay: the verdict compares these. */ played: boolean; replayed: boolean; verdict: Verdict }
export interface ReplayVerdict { firstShot: number; fights: FightVerdict[]; hq: Verdict; heldPlayed: number; heldReplayed: number; verdict: Verdict }

/** Gate B's row: for every hand-fired engagement of the played run, did its block stand in the replay without the
 *  rifle? `held anyway` (stood both times), `saved it` (stood only with the rifle), `fell anyway` (fell both times);
 *  the whole-run verdict is the worst of the fights (or the HQ's own fate when no shot was fired). */
export function replayVerdict(played: SimState, replayed: SimState): ReplayVerdict {
  const fights: FightVerdict[] = [];
  const stood = (st: SimState, bi: number) => st.blocks[bi].state === HELD && !st.fallen[bi];
  const T = played.flow?.threat;
  const idx = (st: SimState, x: number, y: number) => st.blocks.findIndex(b => b.x === x && b.y === y);
  if (T) for (const fg of T.fights) {
    if (fg.rounds <= 0) continue;
    const bi = idx(played, fg.bx, fg.by);
    // the block's end state in both runs (verification pass: comparing the fight's own outcome in play with the block's
    // end state in the replay read "saved it" on a block that fell in both runs an hour later)
    const p = bi >= 0 && stood(played, bi), r = bi >= 0 && stood(replayed, bi);
    const verdict: Verdict = fg.held === null && p === r ? 'open' : p && r ? 'held anyway' : p && !r ? 'saved it' : 'fell anyway';
    fights.push({ t: fg.t, edge: fg.edge, bx: fg.bx, by: fg.by, rounds: fg.rounds, fightHeld: fg.held, played: p, replayed: r, verdict });
  }
  const hqP = stood(played, hqIdx(played)), hqR = stood(replayed, hqIdx(replayed));
  const hq: Verdict = hqP && hqR ? 'held anyway' : hqP && !hqR ? 'saved it' : 'fell anyway';
  const rank: Record<Verdict, number> = { 'held anyway': 0, open: 1, 'saved it': 2, 'fell anyway': 3 };
  let verdict: Verdict = played.engineer.firstShot < 0 ? hq : 'held anyway';
  for (const fg of fights) if (rank[fg.verdict] > rank[verdict]) verdict = fg.verdict;
  if (rank[hq] > rank[verdict]) verdict = hq;
  return { firstShot: played.engineer.firstShot, fights, hq, heldPlayed: played.blocks.filter(b => b.state === HELD).length, heldReplayed: replayed.blocks.filter(b => b.state === HELD).length, verdict };
}

/** For the harness and the dev hook: run the bot for `seconds` of sim time at one tick a step, logging its commands. */
export function runHour(st: SimState, bot: HourBot, seconds: number, log?: LoggedCommand[]): void {
  const f = ensureFlow(st);
  st.speed = 1;
  const cmds: Command[] = [];
  const end = f.tick + Math.round(seconds * TILE_TPS);
  while (f.tick < end) {
    cmds.length = 0;
    hourCommands(st, bot, cmds);
    if (log) for (const c of cmds) log.push({ tick: f.tick, c });
    st.acc = 0;
    advanceFlow(st, TILE_DT, cmds, 1);
    for (const ev of st.events) if (ev.type === 'fall') for (const dir of DIRS) { const i = bot.claimed[dir]; if (i !== undefined && st.blocks[i].x === ev.x && st.blocks[i].y === ev.y) bot.fellWhy[dir] = ev.reason; }
    st.events.length = 0;
  }
}

