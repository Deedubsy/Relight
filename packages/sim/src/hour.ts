/** Prompt B M6 "The hour" (run name B-M6-hour): a bot that plays §11's minute list on the tile layer with real
 *  walking and real pockets — to the steel patch and back to the workbench, the turrets hand-fed on the first red pip
 *  (D-P4-8), the line placed from the pockets in trips to the chest, a second steel Excavator into the chest at 15:00
 *  (D-P4-7), east / west / north claimed at constants.HOUR's minutes (D-HOUR-1, D-P4-10: 15 / 25 / 65, the same
 *  minutes firsthour.ts reads) with the kit walked over — the scored hour is HOUR_MINUTES (75, D-HOUR-3), so north's
 *  claim is inside it and the hour is three claims — the rail-yard coal line placed on west's lot once it holds
 *  (RI-01: the real coal §11 puts on west, an Excavator on the rail-yard heap belted into the Depot), the hands
 *  running magazines and coal every five minutes, and writes down when each thing happened. Through
 *  Gate B a claim's edges keep the block sim's ring-fed hopper and no turret is carried to them (D-P4-9).
 *  `hourReport` turns the log into findings against §11's prose and the calibration timeline. Every command goes
 *  through `Command` (types.ts), so a session under the bot replays like a played one (`replay`, Gate B's "did the
 *  rifle matter" row). The bot is a dev aid: not a player control, never on by default (`?autoplay=hour`). */
import { SimState, Command, HELD, DARK } from './types';
import { hqIdx, RIFLE_RANGE, INV_STACKS, KIT_STACKS, invStacks, invCap } from './engineer';
import { ground, hqLot, inReach, Ground, blockOfTile } from './ground';
import { LOT_TILES, MARGIN_TILES } from './tiles';
import { findPath, passable } from './walk';
import { applyCommands, burnOffS, isCandidate } from './sim';
import { pipOf, edgeCap } from './queries';
import { TURRET_HOPPER } from './recipes';
import { HQ_PATCHES, P_STEEL, P_COPPER, DEPOT_LOT, DEPOT_TILES } from './tiles';
import {
  Kind, Dir, Item, Machine, depotRect, chestCount, turretEdge, edgeTurrets, placeable, canPlace, canPickUp, rubbleAt, outputTile, machineAt, DX, DY, DIR_NAMES,
  MACHINE_SIZE, MACHINE_COST, GENERATOR_COAL_CAP, survivorJoined, advanceFlow, TILE_DT, TILE_TPS, ensureFlow, SHOT, START_TURRETS,
  polePlan, polePlanTo, claimNeed, deliveredTo, activationCheck, faceSub, lockReason, tramAt, poolStr,
} from './flow';
import { projectOf, commissionCheck, describeProject, RAIL_YARD_PROJECT, SUPPLY_DEPOT_PROJECT, SUPPLY_DEPOT_NEED } from './project';
import { heartOf, heartAt, describeHeart, cabinetRepairCheck } from './heart';   // RI-06
import { threatActive, threatOf } from './threat';
import { HOUR_CLAIM_MIN, HOUR_GENERATOR_MIN, HOUR_SHOT_LINE_MIN, HOUR_STEEL2_MIN, HOUR_COPPER2_MIN, HOUR_ASM3_MIN, HOUR_MINUTES } from './constants';

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
  /** RI-03 (D-RI-2): how the bot claims — `physical` (string poles to the block's substation, deliver the claim's steel
   *  and copper there, Activate within reach; the game's path) or `map` (the legacy map click, kept as E-hour's
   *  comparison run and the benchmark's original configuration). */
  claimPath: 'physical' | 'map';
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
  /** RI-05: how the rail yard's coal reaches the Depot — `belt` (E-hour's line, the benchmark) or `tram` (E-project:
   *  the restoration's reward laid as one track, two stops and one tram, and the local depot supplied by it). */
  route: 'belt' | 'tram';
  /** RI-06 (E-heart): the west claim is the Junction Heart's encounter when the state carries the layer (`enableHeart`): the
   *  installation and both feeder cabinets supplied and strung, a turret by each cabinet, the Start, then the patrol that
   *  answers a knocked-out feeder (repair), an interruption (retry at the substation) and a low turret (feed). */
  heart: boolean;
  /** RI-05: the tram route as laid (`tramLine`), for the depot-supply step and the report. */
  tram: TramPlan | null;
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
/** §11's end state the report checks at HOUR_MINUTES (75, D-HOUR-3): four Generators (E4-doc's 0/6/15/45, all on the
 *  HQ lot — the rail yard's coal is belted into the Depot, not burnt beside the heap), seven Excavators (steel ×2,
 *  copper ×2, the HQ coal patch's, east's own rubble, the rail yard's coal — every one a real machine, D-P4-5; six
 *  where east is an outskirts face with nothing to dig, `HourReport.endWant`), three
 *  Assemblers (all real: Shot, Wire, and a third — D-P4-5), the six start turrets (none carried to a claim — D-P4-9
 *  keeps the block-level hopper through Gate B), four blocks Held: the HQ and the three claims constants.HOUR puts
 *  inside the 75-minute hour (east 15, west 25, north 65 — D-HOUR-1, D-P4-10, D-HOUR-3). */
export const HOUR_END = { generators: 4, excavators: 7, assemblers: 3, turrets: START_TURRETS, held: 4 } as const;
/** The scored hour in seconds (HOUR_MINUTES, D-HOUR-3). */
export const HOUR_S = HOUR_MINUTES * 60;
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

/** The requested HQ patch tile with units left nearest the Depot. */
function patchTile(st: SimState, type = P_STEEL): [number, number] | null {
  const p = HQ_PATCHES.find(q => q.type === type)!;
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
    // The candidate adds cabinets and turrets before the hour's second copper line.
    // Collect a shortfall with the same walking/mining/deposit commands a player uses.
    const short = (wants.copper ?? 0) - chestCount(st, 'copper');
    if (bot.heart && short > 0) {
      const p = patchTile(st, P_COPPER);
      if (p) {
        const target = (st.engineer.inv.copper ?? 0) + Math.ceil(short);
        note(bot, st, `hand-mine ${Math.ceil(short)} copper for the candidate's preparation`);
        const mine = (s: SimState, commands: Command[]) => {
          const q = patchTile(s, P_COPPER);
          if (q) commands.push({ type: 'mineAt', x: q[0], y: q[1] });
        };
        return [goto('the copper patch', p[0], p[1], 1), act('mine copper', mine),
          until('the preparation copper mined', s => (s.engineer.inv.copper ?? 0) >= target, short * 3 + 30,
            (s, commands) => { if (!s.flow!.hand.mine && s.flow!.tick % TILE_TPS === 0) mine(s, commands); }),
          act('stop mining copper', (_s, commands) => { commands.push({ type: 'mineAt', x: -1, y: -1 }); }),
          chest('return the preparation copper', st),
          act('deposit the copper', (s, commands) => { commands.push({ type: 'chestPut', item: 'copper', n: s.engineer.inv.copper ?? 0 }); }),
          // Do not retry mining indefinitely if the patch or pockets ran out.
          act('take the prepared materials', (s, commands) => {
            for (const [item, n] of Object.entries(wants) as [keyof typeof wants, number][]) {
              if (n <= 0) continue;
              const have = chestCount(s, item);
              if (have < n) refused(bot, s, `take ${n} ${item}`, `the chest has ${have}`);
              commands.push({ type: 'chestTake', item, n: Math.min(n, have) });
            }
          })];
      }
    }
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
      for (const item of ['steel', 'copper', 'stone', 'coal', 'magazine', 'wire', 'frame', 'board'] as const) {
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

/** RI-01: `put` at an absolute tile — a claim's lot or the street between it and the HQ. */
/** A hand action that must run within reach of a tile (the sim drops a hand command out of reach without a word — a
 *  walk still under way from an earlier goto can carry the engineer past the tile between the goto's check and the
 *  action's tick): out of reach, it stops the walk, walks back and tries again, up to three times (RI-05). */
function within(bot: HourBot, label: string, tx: number, ty: number, size: number, fn: (st: SimState, out: Command[]) => void, tries = 0): Task[] {
  return [goto(label, tx, ty, size), act(label, (st2, out) => {
    if (inReach(st2, tx, ty, size)) { fn(st2, out); return; }
    if (tries >= 3) { refused(bot, st2, label, 'out of reach after three walks'); return; }
    out.push({ type: 'move', x: st2.engineer.x, y: st2.engineer.y });
    return within(bot, label, tx, ty, size, fn, tries + 1);
  })];
}
function putAt(bot: HourBot, kind: Kind, tx: number, ty: number, dir: Dir, label = `${kind} at (${tx},${ty})`): Task[] {
  return within(bot, `place ${label}`, tx, ty, MACHINE_SIZE[kind], (st2, out) => {
    const chk = canPlace(st2, kind, tx, ty);
    if (!chk.ok) { refused(bot, st2, label, chk.reason); return; }
    out.push({ type: 'place', item: kind, x: tx, y: ty, dir });
  });
}
function dirBetween(x: number, y: number, nx: number, ny: number): Dir {
  for (let d = 0; d < 4; d++) if (x + DX[d] === nx && y + DY[d] === ny) return d as Dir;
  return 0;
}
export interface BeltStep { x: number; y: number; dir: Dir }
/** RI-01: the belt routes into the Depot from everywhere a belt may stand now (`placeable`: a Held lot or its street,
 *  no machine, no rubble; §14 lets belts run on streets), within a box round block `bi` and the HQ. One reverse
 *  breadth-first search from the goal tiles: a tile next to the Depot, or next to a belt that already runs into it
 *  (M4's HQ layout and the patches' undug rubble wall the Depot in — a claim's run joins the coal or steel line's last
 *  belts, which take items from any side). `dist` is belts to the Depot (0 = unreachable), `next` the tile after,
 *  `entry` the last belt's direction. `avoid` tiles (the lot the hour's later lines take) count as blocked; `free`
 *  tiles count as open whatever stands there (a turret the bot is about to move). */
export interface BeltRoutes { x0: number; y0: number; w: number; h: number; dist: Int32Array; next: Int32Array; entry: Int8Array }
function feedsDepot(st: SimState, m: Machine, d: { x: number; y: number; size: number }): boolean {
  let cur: Machine | undefined = m;
  for (let n = 0; cur && n < 400; n++) {
    const [ox, oy] = outputTile(cur);
    if (ox >= d.x && ox < d.x + d.size && oy >= d.y && oy < d.y + d.size) return true;
    cur = machineAt(st, ox, oy);
    if (!cur || cur.kind !== 'belt') return false;
  }
  return false;
}
export function beltRoutes(st: SimState, bi: number, avoid: ReadonlySet<number> = new Set(), free: ReadonlySet<number> = new Set()): BeltRoutes {
  const G = ground(st), d = depotRect(st), bg = G.blocks[bi], hb = G.blocks[hqIdx(st)], pad = 12;
  const x0 = Math.max(0, Math.min(bg.x0, hb.x0) - pad), y0 = Math.max(0, Math.min(bg.y0, hb.y0) - pad);
  const x1 = Math.min(G.tw - 1, Math.max(bg.x1, hb.x1) + pad), y1 = Math.min(G.th - 1, Math.max(bg.y1, hb.y1) + pad);
  const w = x1 - x0 + 1, h = y1 - y0 + 1, n = w * h;
  const dist = new Int32Array(n), next = new Int32Array(n).fill(-1), entry = new Int8Array(n).fill(-1);
  const ok = new Uint8Array(n);
  for (let y = y0; y <= y1; y++) for (let x = x0; x <= x1; x++) {
    const t = y * G.tw + x;
    ok[(y - y0) * w + (x - x0)] = !avoid.has(t) && (free.has(t) || placeable(st, 'belt', x, y) === '') ? 1 : 0;
  }
  const inDepot = (x: number, y: number) => x >= d.x && x < d.x + d.size && y >= d.y && y < d.y + d.size;
  const feeder = new Set<number>();
  for (const m of st.flow!.machines) if (m.kind === 'belt' && feedsDepot(st, m, d)) feeder.add(m.y * G.tw + m.x);
  const q: number[] = [];
  for (let y = y0; y <= y1; y++) for (let x = x0; x <= x1; x++) {
    const k = (y - y0) * w + (x - x0);
    if (!ok[k]) continue;
    for (let dir = 0; dir < 4; dir++) {
      const nx = x + DX[dir], ny = y + DY[dir];
      if (inDepot(nx, ny) || feeder.has(ny * G.tw + nx)) { dist[k] = 1; entry[k] = dir; q.push(k); break; }
    }
  }
  for (let hd = 0; hd < q.length; hd++) {
    const k = q[hd], lx = k % w, ly = (k - lx) / w;
    for (let dir = 0; dir < 4; dir++) {
      const nx = lx + DX[dir], ny = ly + DY[dir];
      if (nx < 0 || ny < 0 || nx >= w || ny >= h) continue;
      const nk = ny * w + nx;
      if (!ok[nk] || dist[nk] > 0) continue;
      dist[nk] = dist[k] + 1; next[nk] = k; q.push(nk);
    }
  }
  return { x0, y0, w, h, dist, next, entry };
}
/** The belt run from (sx,sy) along `routes`, in placement order, each belt's direction toward the next tile; null when
 *  (sx,sy) has no route (a rubble or machine tile, or walled off). */
export function beltPath(routes: BeltRoutes, sx: number, sy: number): BeltStep[] | null {
  const { x0, y0, w, h, dist, next, entry } = routes;
  if (sx < x0 || sy < y0 || sx >= x0 + w || sy >= y0 + h) return null;
  let k = (sy - y0) * w + (sx - x0);
  if (dist[k] <= 0) return null;
  const out: BeltStep[] = [];
  for (let n = 0; n < dist.length; n++) {
    const lx = k % w, ly = (k - lx) / w, nk = next[k];
    if (nk < 0) { out.push({ x: lx + x0, y: ly + y0, dir: entry[k] as Dir }); return out; }
    const nx = nk % w, ny = (nk - nx) / w;
    out.push({ x: lx + x0, y: ly + y0, dir: dirBetween(lx, ly, nx, ny) });
    k = nk;
  }
  return null;
}
/** The 3×3 lot spots on block `i` an Excavator may stand on with rubble of `type` in reach (its footprint plus one
 *  tile each way, flow.ts `findRubble`), richest first, ties nearest the Depot. */
function rubbleSpots(st: SimState, i: number, type: Item): { x: number; y: number; n: number }[] {
  const G = ground(st), bg = G.blocks[i], tw = G.tw, d = depotRect(st), cx = d.x + d.size / 2, cy = d.y + d.size / 2;
  const out: { x: number; y: number; n: number; dist: number }[] = [];
  for (const t of bg.tiles) {
    const tx = t % tw, ty = (t - tx) / tw;
    if (placeable(st, 'excavator', tx, ty)) continue;
    let n = 0;
    for (let y = ty - 1; y <= ty + 3; y++) for (let x = tx - 1; x <= tx + 3; x++) { const r = rubbleAt(st, x, y); if (r && r.type === type && r.bi === i) n++; }
    if (n > 0) out.push({ x: tx, y: ty, n, dist: Math.hypot(tx + 1.5 - cx, ty + 1.5 - cy) });
  }
  return out.sort((a, b) => b.n - a.n || a.dist - b.dist);
}
/** D-P4-12: the rail yard's coal heap (ground.ts `railYardLayout`, RAIL_YARD_HEAP square) as the Excavator's spot —
 *  exactly on it, so every heap tile is in reach. Null when block `i` is not the rail yard or the spot is taken. */
function heapSpot(st: SimState, i: number): [number, number] | null {
  const G = ground(st), bg = G.blocks[i];
  if (i !== G.railYard || bg.count <= 0) return null;
  let x0 = Infinity, y0 = Infinity;
  for (let k = 0; k < bg.count; k++) { const t = bg.order[k], tx = t % G.tw; x0 = Math.min(x0, tx); y0 = Math.min(y0, (t - tx) / G.tw); }
  return placeable(st, 'excavator', x0, y0) ? null : [x0, y0];
}
/** The HQ lot the hour's later fixed steps take (copperToChest's Excavator and belts, the third Assembler), as
 *  [lx, ly, w, h]: a claim's belt run is routed round them so those steps still find their tiles. */
const HOUR_LATER_LOT: readonly [number, number, number, number][] = [[4, 14, 3, 3], [5, 13, 4, 1], [15, 0, 3, 3]];
/** The HQ lot's west corridor, [lx, ly, w, h]: the two rows between the steel patch (lot y 7–11) and the copper patch
 *  (y 14–16), from the lot's west edge to the Depot's west face. M4's layout walls the Depot in on every other side
 *  (the steel line along row 6, the copper line along row 17, the Shot line down the east), so a claim's belt run
 *  comes in here; a start turret standing on it (`startTurrets` puts the west face's turret on the nearest lot tile —
 *  seed 5) is picked up and put down beside the run. */
const HOUR_CORRIDOR: readonly [number, number, number, number] = [0, 12, 9, 2];
const lotRectTiles = (st: SimState, rects: readonly (readonly [number, number, number, number])[]): Set<number> => {
  const G = ground(st), out = new Set<number>();
  for (const [lx, ly, w, h] of rects) for (let y = 0; y < h; y++) for (let x = 0; x < w; x++) { const [ax, ay] = hqLot(st, lx + x, ly + y); out.add(ay * G.tw + ax); }
  return out;
};
const footprint = (G: Ground, m: Machine): number[] => {
  const out: number[] = [];
  for (let y = m.y; y < m.y + m.size; y++) for (let x = m.x; x < m.x + m.size; x++) out.push(y * G.tw + x);
  return out;
};
/** The shortest run into the Depot from one of a spot's four output tiles, over every spot in order (a spot deep in
 *  the rubble has no free output tile and drops out at once), so the first spot with a route wins. */
function bestLine(routes: BeltRoutes, spots: readonly { x: number; y: number }[]): { x: number; y: number; d: Dir; path: BeltStep[] } | null {
  for (const sp of spots) {
    let best: { x: number; y: number; d: Dir; path: BeltStep[] } | null = null;
    for (let d = 0; d < 4; d++) {
      const [ox, oy] = outputTile({ x: sp.x, y: sp.y, dir: d as Dir, size: 3 } as Machine);
      if (ox >= sp.x && ox < sp.x + 3 && oy >= sp.y && oy < sp.y + 3) continue;
      const path = beltPath(routes, ox, oy);
      if (path && (!best || path.length < best.path.length)) best = { x: sp.x, y: sp.y, d: d as Dir, path };
    }
    if (best) return best;
  }
  return null;
}
/** RI-01: the Excavator on a claimed block and its belt run into the Depot (§11's "west's coal", east's own rubble —
 *  D-P4-5: a real machine on the claim's rubble, replacing the E4 stand-in on the HQ lot). The spot is the coal
 *  heap on the rail yard, else the richest 3×3 that has a route from one of its four output tiles; the facing is
 *  the side with the shortest run. When nothing routes and a start turret stands on the corridor, the run is planned
 *  with that turret's tiles open and the turret is moved first (picked up, put down on the nearest free 2×2 off the
 *  corridor and the run, its magazines fed back). Steel for it all comes from the chest in one trip; a refusal
 *  anywhere is logged, never forced. */
function blockLine(bot: HourBot, dir: HourDir, want: 'coal' | 'own', st: SimState): Task[] {
  const i = bot.claimed[dir];
  if (i === undefined || st.blocks[i].state !== HELD) { refused(bot, st, `${dir}'s line`, i === undefined ? `${dir} was never claimed` : `${dir} is not Held`); return []; }
  const G = ground(st), own = G.blocks[i].rubble;
  // D-P4-2: a block's rubble is its district's, so east's line digs whatever east has (stone, copper …); an outskirts
  // face with no rubble (its deposit is D-P4-3's placeholder) gets no line, and the report's end state expects one fewer
  if (want === 'own' && !own) { note(bot, st, `${dir} (block ${i}) has no rubble to dig (an outskirts face, D-P4-3): no ${dir} line`); return []; }
  const type: Item = want === 'coal' ? 'coal' : own!, what = `${dir}'s ${type} line`;
  const heap = type === 'coal' ? heapSpot(st, i) : null;
  if (type === 'coal' && !heap) note(bot, st, i === G.railYard ? 'the rail yard\'s heap spot is taken' : `${dir} (block ${i}) is not the rail yard (block ${G.railYard}) — the richest coal spot instead`);
  const spots = heap ? [{ x: heap[0], y: heap[1], n: 0 }] : rubbleSpots(st, i, type);
  if (spots.length === 0) { refused(bot, st, what, `no lot spot on block ${i} with ${type} rubble in an Excavator's reach`); return []; }
  const avoid = lotRectTiles(st, HOUR_LATER_LOT);
  let best = bestLine(beltRoutes(st, i, avoid), spots);
  let move: Machine | null = null;
  if (!best) {
    const corridor = lotRectTiles(st, [HOUR_CORRIDOR]), hq = hqIdx(st);
    for (const m of st.flow!.machines) {
      if (m.kind !== 'turret' || G.owner[m.y * G.tw + m.x] !== hq || !footprint(G, m).some(t => corridor.has(t))) continue;
      best = bestLine(beltRoutes(st, i, avoid, new Set(footprint(G, m))), spots);
      if (best) { move = m; break; }
    }
  }
  if (!best) { refused(bot, st, what, `no belt route to the Depot from any of ${spots.length} spots on block ${i}`); return []; }
  const { x: ex, y: ey } = best;
  note(bot, st, `${what}: Excavator at (${ex},${ey}) facing ${DIR_NAMES[best.d]}, ${best.path.length} belts to the Depot`);
  const steel = MACHINE_COST.excavator.steel + best.path.length * MACHINE_COST.belt.steel;
  const t: Task[] = [chest('to the chest', st), act('the pockets\' rubble and magazines into the chest', (st2, out) => {
    for (const item of ['steel', 'copper', 'stone', 'coal', 'magazine', 'wire', 'frame', 'board'] as const) {
      const n = st2.engineer.inv[item] ?? 0;
      if (n > 0) out.push({ type: 'chestPut', item, n });
    }
  }), takeTask(bot, { steel })];
  if (move) {
    const m = move, taken = new Set<number>([...corridorTiles(st), ...best.path.map(b => b.y * G.tw + b.x), ...avoid]);
    let spot: [number, number] | null = null;
    for (let d = 1; d <= 6 && !spot; d++) for (let dy = -d; dy <= d && !spot; dy++) for (let dx = -d; dx <= d && !spot; dx++) {
      if (Math.max(Math.abs(dx), Math.abs(dy)) !== d) continue;
      const tx = m.x + dx, ty = m.y + dy;
      if (placeable(st, 'turret', tx, ty)) continue;
      let clear = true;
      for (let y = ty; y < ty + m.size && clear; y++) for (let x = tx; x < tx + m.size; x++) if (taken.has(y * G.tw + x)) { clear = false; break; }
      if (clear) spot = [tx, ty];
    }
    if (!spot) { refused(bot, st, what, `the start turret at (${m.x},${m.y}) stands on the corridor and has no free 2×2 within 6 tiles to move to`); return []; }
    const [nx, ny] = spot;
    note(bot, st, `the start turret at (${m.x},${m.y}) stands on the HQ lot's west corridor: moved to (${nx},${ny}) for ${what}`);
    t.push(goto('the turret on the corridor', m.x, m.y, m.size), act('pick up the turret', (st2, out) => {
      const chk = canPickUp(st2, m.x, m.y);
      if (!chk.ok) { refused(bot, st2, `pick up the turret at (${m.x},${m.y})`, chk.reason); return; }
      out.push({ type: 'pickUp', x: m.x, y: m.y });
    }), ...putAt(bot, 'turret', nx, ny, m.dir, 'the moved turret'), feedTask('moved turret', nx, ny));
  }
  t.push(...putAt(bot, 'excavator', ex, ey, best.d, `${dir}'s ${type} Excavator`));
  for (const b of best.path) t.push(...putAt(bot, 'belt', b.x, b.y, b.dir));
  t.push(act(`${what} placed`, st2 => { mark(bot, st2, `${dir}-line`); }));
  return t;
}
const corridorTiles = (st: SimState): Set<number> => lotRectTiles(st, [HOUR_CORRIDOR]);

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
        act(`${dir} kitted`, st2 => kittedNote(bot, st2, dir, i)),
        chest(`back from ${dir}`, st),
      ];
    }),
  ];
}

/** The kit wait's verdict: `kitted-<dir>`, or the edges born unkitted written down (a kit is spent at the instant an edge is born). */
function kittedNote(bot: HourBot, st: SimState, dir: HourDir, i: number): void {
  if (kitted(st, i)) { mark(bot, st, `kitted-${dir}`); return; }
  const mine = st.ring.filter(e => e.a === i), un = mine.filter(e => e.kit === false);
  if (un.length) refused(bot, st, `kit ${dir}'s ring`, `${un.length} of ${mine.length} edges were born unkitted (${st.engineer.inv.kit ?? 0} kits left) — a kit is spent at the instant an edge is born`);
}

/** RI-03 (plan §4.1, D-RI-2): the physical claim. Take the claim's steel and copper plus the poles' from the chest,
 *  string a pole run to the direction's substation, deliver the materials there, go back for the kits, Activate within
 *  reach of the substation, then the same walk-over and kit wait as the map path. Two chest trips: HOUR_KITS kits fill
 *  the pockets (KIT_STACKS × HOUR_KITS = INV_STACKS), so the materials go first. Every command is one a player sends
 *  (`chestTake`, `place`, `deliver`, `activate`); the map click charges nothing. */
function physicalClaimStep(bot: HourBot, dir: HourDir): Task[] {
  return [
    act(`claim ${dir} (physical)`, st => {
      const i = neighbourToward(st, dir);
      if (i < 0) { refused(bot, st, `claim ${dir}`, 'no Dark candidate that way'); return; }
      const b = st.blocks[i], sub = faceSub(st, i);
      if (!sub) { refused(bot, st, `claim ${dir}`, 'no substation to deliver to (the outskirts need a Substation first)'); return; }
      bot.claimed[dir] = i;
      if (bot.heart && heartAt(st, i)) return heartClaimTasks(bot, dir, i, st);   // RI-06: the Heart's claim
      const need = claimNeed(st), poles = polePlan(st, i).length, spare = poles + 2;
      note(bot, st, `${dir} = block ${i} (${b.name}, d ${b.d.toFixed(2)}, burn-off ${burnOffS(b.d).toFixed(0)} s): ${poles} pole(s) to string, ${need.steel} steel + ${need.copper} Cu to deliver`);
      const deliver = (st2: SimState, out: Command[], item: 'steel' | 'copper'): void => {
        const got = deliveredTo(st2, i), n = item === 'steel' ? need.steel - got.steel : need.copper - got.copper;
        if (n > 0) out.push({ type: 'deliver', bx: b.x, by: b.y, item, n });
      };
      return [
        takeTask(bot, { steel: need.steel + spare * MACHINE_COST.pole.steel, copper: need.copper + spare * MACHINE_COST.pole.copper }),
        stringTask(bot, i, dir, 0),
        goto(`${dir}'s substation`, sub.x, sub.y, sub.size, `deliver ${dir}`),
        act(`deliver to ${dir}`, (st2, out) => { deliver(st2, out, 'steel'); deliver(st2, out, 'copper'); }),
        act(`${dir} delivered`, st2 => {
          const got = deliveredTo(st2, i);
          note(bot, st2, `${got.steel} steel + ${got.copper} Cu delivered to ${dir}'s substation (needs ${need.steel} + ${need.copper})`);
          if (got.steel < need.steel || got.copper < need.copper) refused(bot, st2, `deliver ${dir}'s materials`, `${got.steel} of ${need.steel} steel, ${got.copper} of ${need.copper} Cu delivered`);
          else mark(bot, st2, `deliver-${dir}`);
        }),
        chest(`back for ${dir}'s kits`, st),
        ...kitsTask(bot),
        goto(`${dir}'s substation`, sub.x, sub.y, sub.size, `activate ${dir}`),
        // a transient (a brownout second with no Generator burning) is waited out, up to a minute
        until(`${dir} ready to activate`, st2 => activationCheck(st2, b.x, b.y).ok, 60),
        act(`activate ${dir}`, (st2, out) => {
          const chk = activationCheck(st2, b.x, b.y);
          if (!chk.ok) { refused(bot, st2, `activate ${dir}`, chk.reason); return [chest(`back from ${dir}`, st2)]; }
          out.push({ type: 'activate', bx: b.x, by: b.y });
          mark(bot, st2, `claim-${dir}`);
          const [sx, sy] = blockStand(st2, i), t0 = st2.t;
          return [
            goto(`${dir}'s lot`, sx, sy, 1, `walk-over ${dir}`),
            act(`arrived on ${dir}`, st3 => {
              mark(bot, st3, `arrive-${dir}`);
              // the walk-over is timed like the map path's: an engineer who activated from the kerb and was already in reach of
              // the lot (the runner logs no walk it never had to send) walked the seconds between the Activate and this arrival
              if (!bot.log.walks.some(w => w.name === `walk-over ${dir}`)) bot.log.walks.push({ name: `walk-over ${dir}`, t0, t1: st3.t, tiles: 0 });
            }),
            until(`${dir} Held and kitted`, st3 => kitted(st3, i), burnOffS(b.d) + 180),
            act(`${dir} kitted`, st3 => kittedNote(bot, st3, dir, i)),
            chest(`back from ${dir}`, st2),
          ];
        }),
      ];
    }),
  ];
}

/** RI-03: string the pole run one pole at a time — the plan is read off the state each time (the pole just placed is
 *  the next start), the bot walks within reach and places it; a refusal (reach, cost, the tile) ends the run. A run
 *  of twenty is a refusal (the plan never needs more than sixteen). */
function stringTask(bot: HourBot, i: number, dir: HourDir, n: number): Task {
  return act(n === 0 ? `string ${dir}` : `next pole to ${dir}`, st => {
    const plan = polePlan(st, i);
    if (!plan.length) {
      if (n > 0) mark(bot, st, `strung-${dir}`);
      note(bot, st, n > 0 ? `${n} pole(s) strung to ${dir}'s substation` : `${dir}'s substation is already on the grid`);
      return;
    }
    if (n >= 20) { refused(bot, st, `string ${dir}`, `${n} poles placed and the run still does not reach`); return; }
    const [px, py] = plan[0];
    return [
      goto('pole', px, py, 1, n === 0 ? `string ${dir}` : undefined),
      act('place pole', (st2, out) => {
        const chk = canPlace(st2, 'pole', px, py);
        if (!chk.ok) { refused(bot, st2, `pole at (${px},${py}) for ${dir}`, chk.reason); return; }
        out.push({ type: 'place', item: 'pole', x: px, y: py, dir: 0 });
        return [stringTask(bot, i, dir, n + 1)];
      }),
    ];
  });
}

// ------------------------------------------------------------------ RI-06: the Junction Heart's claim (E-heart)

/** RI-06: string a pole run to any tile on block `i` (a feeder cabinet), one pole at a time, as `stringTask` does. */
function stringToTask(bot: HourBot, i: number, label: string, tx: number, ty: number, n: number): Task {
  return act(n === 0 ? `string ${label}` : `next pole to ${label}`, st => {
    const plan = polePlanTo(st, i, tx, ty, 1);
    if (!plan.length) { note(bot, st, n > 0 ? `${n} pole(s) strung to ${label}` : `${label} is already on the grid`); return; }
    if (n >= 20) { refused(bot, st, `string ${label}`, `${n} poles placed and the run still does not reach`); return; }
    const [px, py] = plan[0];
    return [
      goto('pole', px, py, 1, n === 0 ? `string ${label}` : undefined),
      act('place pole', (st2, out) => {
        const chk = canPlace(st2, 'pole', px, py);
        if (!chk.ok) { refused(bot, st2, `pole at (${px},${py}) for ${label}`, chk.reason); return; }
        out.push({ type: 'place', item: 'pole', x: px, y: py, dir: 0 });
        return [stringToTask(bot, i, label, tx, ty, n + 1)];
      }),
    ];
  });
}
/** The nearest tile within ring 3 of (tx, ty) a turret may stand on now, or null. */
function turretSpot(st: SimState, tx: number, ty: number): [number, number] | null {
  for (let ring = 1; ring <= 3; ring++) for (let oy = -ring; oy <= ring; oy++) for (let ox = -ring; ox <= ring; ox++) {
    if (Math.max(Math.abs(ox), Math.abs(oy)) !== ring) continue;
    if (placeable(st, 'turret', tx + ox, ty + oy) === '') return [tx + ox, ty + oy];
  }
  return null;
}
/** RI-06 (plan §9.3, §9.4): the Heart's claim as the bot plays it — a prepared factory, rifle off. One chest trip carries
 *  the installation's materials, both cabinets' materials, the poles, two turrets and their magazines: the installation
 *  is strung and supplied as any claim's, then each feeder cabinet is strung (`polePlanTo`), supplied (`deliver` with
 *  `cabinet`) and guarded by one turret placed within reach of it and fed by hand; the kits; the Start at the
 *  substation; then the patrol (`heartPatrol`) until the yard is Held, and the ordinary walk-over and kit wait. */
function heartClaimTasks(bot: HourBot, dir: HourDir, i: number, st: SimState): Task[] {
  const H = heartAt(st, i)!, b = st.blocks[i], sub = faceSub(st, i)!, need = claimNeed(st), cab = H.cand.cabinet, nC = H.cabinets.length;
  const poles = polePlan(st, i).length + H.cabinets.reduce((n, c) => n + polePlanTo(st, i, c.x, c.y, 1).length, 0);
  const spare = poles + 2, C = MACHINE_COST;
  note(bot, st, `${dir} = block ${i} (${b.name}, d ${b.d.toFixed(2)}): the Junction Heart — ${nC} feeder cabinets at ${H.cabinets.map(c => `(${c.x},${c.y})`).join(' and ')}, ${cab.steel} steel + ${cab.copper} Cu each; the installation ${need.steel} steel + ${need.copper} Cu; ${H.cand.productiveS} s productive, ${H.cand.stallS} s stall, packets at ${H.cand.thresholds.join('/')} %`);
  const deliverSub = (st2: SimState, out: Command[], item: 'steel' | 'copper'): void => {
    const got = deliveredTo(st2, i), n = item === 'steel' ? need.steel - got.steel : need.copper - got.copper;
    if (n > 0) out.push({ type: 'deliver', bx: b.x, by: b.y, item, n });
  };
  const tasks: Task[] = [
    takeTask(bot, {
      steel: need.steel + nC * cab.steel + spare * C.pole.steel + nC * C.turret.steel,
      copper: need.copper + nC * cab.copper + spare * C.pole.copper + nC * C.turret.copper + 2 * nC,   // + REPAIR_COPPER a knock-out, two a cabinet
      magazine: Math.ceil(nC * TURRET_HOPPER / SHOT.count),
    }),
    stringTask(bot, i, dir, 0),
    goto(`${dir}'s substation`, sub.x, sub.y, sub.size, `deliver ${dir}`),
    act(`deliver to ${dir}`, (st2, out) => { deliverSub(st2, out, 'steel'); deliverSub(st2, out, 'copper'); }),
    act(`${dir} delivered`, st2 => {
      const got = deliveredTo(st2, i);
      note(bot, st2, `${got.steel} steel + ${got.copper} Cu delivered to the installation (needs ${need.steel} + ${need.copper})`);
      if (got.steel < need.steel || got.copper < need.copper) refused(bot, st2, `deliver ${dir}'s materials`, `${got.steel} of ${need.steel} steel, ${got.copper} of ${need.copper} Cu delivered`);
      else mark(bot, st2, `deliver-${dir}`);
    }),
  ];
  H.cabinets.forEach((c, k) => {
    const label = `cabinet ${k + 1}`;
    tasks.push(
      stringToTask(bot, i, label, c.x, c.y, 0),
      ...within(bot, `deliver to ${label}`, c.x, c.y, 1, (st2, out) => {
        const cc = heartOf(st2)!.cabinets[k];
        for (const item of ['steel', 'copper'] as const) { const n = cab[item] - cc.delivered[item]; if (n > 0) out.push({ type: 'deliver', bx: b.x, by: b.y, item, n, cabinet: k }); }
      }),
      act(`${label} delivered`, st2 => {
        const cc = heartOf(st2)!.cabinets[k];
        if (cc.delivered.steel < cab.steel || cc.delivered.copper < cab.copper) refused(bot, st2, `deliver ${label}'s materials`, `${cc.delivered.steel} of ${cab.steel} steel, ${cc.delivered.copper} of ${cab.copper} Cu delivered`);
        else mark(bot, st2, `cabinet-${k + 1}-supplied`);
      }),
      act(`a turret by ${label}`, st2 => {
        const spot = turretSpot(st2, c.x, c.y);
        if (!spot) { refused(bot, st2, `turret by ${label}`, 'no tile within three of the cabinet takes a turret'); return; }
        return [...putAt(bot, 'turret', spot[0], spot[1], 0, `turret by ${label}`), feedTask('turret', spot[0], spot[1]),
          act(`${label} guarded`, st3 => { if (machineAt(st3, spot[0], spot[1])?.kind === 'turret') mark(bot, st3, `turret-${k + 1}`); })];
      }),
    );
  });
  tasks.push(
    chest(`back for ${dir}'s kits`, st),
    ...kitsTask(bot),
    act('carry the Heart patrol supplies', (s, out) => {
      const kits = s.nb[i].filter((j, k) => s.blocks[j].state !== HELD && edgeTurrets(s, i * s.deg + k).length === 0).length;
      const surplus = (s.engineer.inv.kit ?? 0) - kits;
      if (surplus > 0) out.push({ type: 'chestPut', item: 'kit', n: surplus });
      out.push({ type: 'chestTake', item: 'copper', n: 2 * nC });
      out.push({ type: 'chestTake', item: 'magazine', n: Math.ceil(nC * TURRET_HOPPER / SHOT.count) });
    }),
    goto(`${dir}'s substation`, sub.x, sub.y, sub.size, `activate ${dir}`),
    until(`${dir} ready to start`, st2 => activationCheck(st2, b.x, b.y).ok, 60),
    act(`start commissioning ${dir}`, (st2, out) => {
      const chk = activationCheck(st2, b.x, b.y);
      if (!chk.ok) { refused(bot, st2, `start commissioning ${dir}`, chk.reason); return [chest(`back from ${dir}`, st2)]; }
      out.push({ type: 'activate', bx: b.x, by: b.y });
      mark(bot, st2, `claim-${dir}`); mark(bot, st2, 'heart-start');
      const Hn = heartOf(st2)!, deadline = st2.t + Hn.cand.productiveS + 3 * Hn.cand.stallS + 300, t0 = st2.t;
      const [sx, sy] = blockStand(st2, i);
      return [
        heartPatrol(bot, dir, i, deadline),
        goto(`${dir}'s lot`, sx, sy, 1, `walk-over ${dir}`),
        act(`arrived on ${dir}`, st3 => {
          mark(bot, st3, `arrive-${dir}`);
          if (!bot.log.walks.some(w => w.name === `walk-over ${dir}`)) bot.log.walks.push({ name: `walk-over ${dir}`, t0, t1: st3.t, tiles: 0 });
        }),
        until(`${dir} Held and kitted`, st3 => kitted(st3, i), 180),
        act(`${dir} kitted`, st3 => kittedNote(bot, st3, dir, i)),
        chest(`back from ${dir}`, st2),
      ];
    }),
  );
  return tasks;
}
/** The patrol: every two seconds read the encounter and answer its active problem — a knocked-out feeder (walk to it,
 *  repair), an interrupted attempt (the substation, Start again), a low turret (feed) — until the yard is Held, the
 *  Heart is destroyed, or the deadline passes (a refusal naming the encounter's line). Rifle off throughout. */
function heartPatrol(bot: HourBot, dir: HourDir, i: number, deadline: number): Task {
  return act(`Heart patrol ${dir}`, st => {
    const H = heartOf(st), b = st.blocks[i], sub = faceSub(st, i)!;
    if (!H) return;
    if (H.destroyed || b.state === HELD) { mark(bot, st, 'heart-destroyed'); return; }
    if (st.t >= deadline) { refused(bot, st, `the Heart at ${dir}`, `not destroyed by the patrol's deadline — ${describeHeart(st)}`); return; }
    const t0 = st.t, again = (): Task[] => [until('watching the Heart', s => s.t >= t0 + 2 || s.blocks[i].state === HELD, 10), heartPatrol(bot, dir, i, deadline)];
    const k = H.cabinets.findIndex(c => c.down);
    if (k >= 0) {
      const c = H.cabinets[k];
      mark(bot, st, 'cabinet-down');
      return [...within(bot, `repair cabinet ${k + 1}`, c.x, c.y, 1, (s2, out) => {
        const chk = cabinetRepairCheck(s2, k);
        if (!chk.ok) refused(bot, s2, `repair cabinet ${k + 1}`, chk.reason);
        else { out.push({ type: 'repairCabinet', cabinet: k }); mark(bot, s2, 'cabinet-repaired'); }
      }), ...again()];
    }
    if (H.attempt < 0 && b.state === DARK) {
      mark(bot, st, 'heart-interrupted');
      return [goto(`${dir}'s substation`, sub.x, sub.y, sub.size, `retry ${dir}`), until(`${dir} ready to retry`, s2 => activationCheck(s2, b.x, b.y).ok, 60),
        act(`retry ${dir}`, (s2, out) => {
          const chk = activationCheck(s2, b.x, b.y);
          if (!chk.ok) { refused(bot, s2, `retry ${dir}`, chk.reason); return; }
          out.push({ type: 'activate', bx: b.x, by: b.y }); mark(bot, s2, 'heart-retry');
        }), ...again()];
    }
    const e = st.engineer, low = st.flow!.machines.filter(m => m.kind === 'turret' && blockOfTile(st, m.x, m.y) === i && (m.inv.rounds ?? 0) <= TURRET_HOPPER / 2)
      .sort((p, q) => Math.hypot(p.x - e.x, p.y - e.y) - Math.hypot(q.x - e.x, q.y - e.y));
    if (low.length && (e.inv.magazine ?? 0) > 0) return [goto('turret', low[0].x, low[0].y, low[0].size, 'Heart feed'), feedTask('turret', low[0].x, low[0].y), ...again()];
    return again();
  });
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
  const wireAssembler = (at: number, lx: number, ly: number): HourStep => ({ at, name: 'the Wire Assembler (RI-01: a real machine on the Wire recipe, D-B2-1 (b))', tasks: st => {
    let n0 = 0;
    const count = (s: SimState) => s.flow!.machines.filter(m => m.kind === 'assembler').length;
    return [chest('to the chest', st), takeTask(bot, { steel: MACHINE_COST.assembler.steel, copper: MACHINE_COST.assembler.copper }),
      act('count Assemblers', s => { n0 = count(s); }), ...putNear(bot, 'assembler', lx, ly, 0),
      act('the Wire recipe', (s, out) => {
        const asms = s.flow!.machines.filter(m => m.kind === 'assembler');
        if (asms.length <= n0) return;
        const m = asms[asms.length - 1];
        out.push({ type: 'setRecipe', x: m.x, y: m.y, recipe: 'wire' });
        mark(bot, s, 'wire-assembler');
      })];
  } });
  const claimMin = (dir: HourDir): number => dir === 'north' ? bot.northAt : HOUR_CLAIM_AT[dir];
  // RI-03: the physical path (the game's) or the legacy map click (E-hour's comparison run)
  const claimAt = (dir: HourDir): HourStep => ({ at: claimMin(dir), name: `§11 ${claimMin(dir) / 60}:00 — claim ${dir}`, tasks: st =>
    bot.claimPath === 'physical' ? [chest('to the chest for the claim', st), ...physicalClaimStep(bot, dir)]
                                 : [chest('to the chest for kits', st), ...kitsTask(bot), ...claimStep(bot, dir)] });
  const burnE = burnOffS(0.22);
  return [
    { at: 0, name: '§11 0:00 — to the steel patch, hand-mine 20 steel', tasks: st => {
      const p = patchTile(st);
      if (!p) { refused(bot, st, 'mine steel', 'no steel patch tile with units'); return []; }
      const mine = (st2: SimState, out: Command[]) => { const q = patchTile(st2); if (q) out.push({ type: 'mineAt', x: q[0], y: q[1] }); };
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
    // RI-01 (D-P4-5): east's rubble and west's coal are real Excavators on the claims, belted into the Depot — the
    // E4 stand-ins on the HQ lot at (7,1) and (3,1) are gone; the Wire Assembler at (11,1) runs the Wire recipe
    // (unfed: nothing belts copper to it in the hour, so it is the line's load and no more — the report says so)
    { at: HOUR_CLAIM_AT.east + burnE + 60, name: 'east\'s Excavator on its own rubble, belted into the Depot (RI-01, D-P4-5)', tasks: st => blockLine(bot, 'east', 'own', st) },
    wireAssembler(HOUR_CLAIM_AT.east + burnE + 120, 11, 1),
    claimAt('west'),
    bot.route === 'tram'
      ? { at: HOUR_CLAIM_AT.west + burnE + 60, name: 'the rail yard\'s coal Excavator, its coal to the Depot by tram: the restoration\'s reward laid (RI-05, plan §5)', tasks: st => tramLine(bot, st) }
      : { at: HOUR_CLAIM_AT.west + burnE + 60, name: 'the rail yard\'s coal Excavator, belted into the Depot (RI-01, D-P4-12)', tasks: st => blockLine(bot, 'west', 'coal', st) },
    // RI-05: the local supply depot's materials ride the tram the other way (plan §5.2, §9.1: the reward used on the next
    // delivery); the step is absent on the belt route, so E-hour's timeline is unchanged
    ...(bot.route === 'tram' ? [{ at: HOUR_CLAIM_AT.west + burnE + 6 * 60, name: 'the local supply depot, supplied by tram and commissioned (RI-05, plan §5.2)', tasks: (st: SimState) => depotSupply(bot, st) }] : []),
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

// ------------------------------------------------------------------ RI-05: the tram route (plan §5, the minimal part of T16)

/** RI-05: the rail yard's reward laid and used on the very next delivery (plan §9.1). One track column on the street
 *  between the yard and the HQ, a stop at each end, one tram. The yard's coal Excavator belts into stop A's platform,
 *  the tram carries the coal to stop B, whose inserter puts it on a belt into the Depot (E-hour's coal line, by tram
 *  instead of by belt); on the way back the tram carries what the bot loads on B's platform to A, whose inserter
 *  fills the supply chest beside it — the local depot project's materials (plan §5.2), delivered by the reward.
 *  The plan is read off the state: the street column nearest the HQ lot that takes it all, stop B's row nearest the
 *  Depot's, stop A's nearest the heap's, the belts by breadth-first search over tiles a belt may stand on now. */
export interface TramPlan {
  /** The track column and its rows (inclusive). */
  xT: number; yLo: number; yHi: number;
  exc: { x: number; y: number; d: Dir }; excBelts: BeltStep[];
  stopA: [number, number]; insA: [number, number]; chest: [number, number];
  stopB: [number, number]; insB: [number, number]; depotBelts: BeltStep[];
}
/** The shortest belt run from `from` to any tile of `goals` (its value the direction into the machine beyond it), over
 *  tiles a belt may stand on now (`placeable`), never `avoid`, inside `box` (inclusive). */
function beltRun(st: SimState, from: [number, number], goals: Map<number, Dir>, avoid: ReadonlySet<number>, box: { x0: number; y0: number; x1: number; y1: number }): BeltStep[] | null {
  const tw = ground(st).tw, key = (x: number, y: number) => y * tw + x;
  const start = key(from[0], from[1]);
  if (from[0] < box.x0 || from[0] > box.x1 || from[1] < box.y0 || from[1] > box.y1 || avoid.has(start) || placeable(st, 'belt', from[0], from[1]) !== '') return null;
  const prev = new Map<number, number>([[start, -1]]), queue = [start];
  for (let qi = 0; qi < queue.length; qi++) {
    const t = queue[qi];
    if (goals.has(t)) {
      const tiles: number[] = [];
      for (let c = t; c !== -1; c = prev.get(c)!) tiles.push(c);
      tiles.reverse();
      return tiles.map((c, k) => {
        const x = c % tw, y = (c - x) / tw;
        if (k === tiles.length - 1) return { x, y, dir: goals.get(t)! };
        const nx = tiles[k + 1] % tw, ny = (tiles[k + 1] - nx) / tw;
        return { x, y, dir: dirBetween(x, y, nx, ny) };
      });
    }
    const x = t % tw, y = (t - x) / tw;
    for (let d = 0; d < 4; d++) {
      const nx = x + DX[d], ny = y + DY[d];
      if (nx < box.x0 || nx > box.x1 || ny < box.y0 || ny > box.y1) continue;
      const nt = key(nx, ny);
      if (prev.has(nt) || avoid.has(nt) || placeable(st, 'belt', nx, ny) !== '') continue;
      prev.set(nt, t); queue.push(nt);
    }
  }
  return null;
}
/** Rows from `c` outward within [lo, hi]: the nearest first. */
function rowsFrom(c: number, lo: number, hi: number): number[] {
  const out: number[] = [];
  for (let r = 0; r <= hi - lo; r++) for (const y of [c - r, c + r]) if (y >= lo && y <= hi && !out.includes(y)) out.push(y);
  return out;
}
export function tramPlan(st: SimState, yard: number, hq: number): TramPlan | string {
  const G = ground(st), Y = G.blocks[yard], H = G.blocks[hq], tw = G.tw;
  if (Y.x1 >= H.x0) return 'the rail yard is not west of the HQ';
  const heap = heapSpot(st, yard);
  if (!heap) return 'the rail yard\'s heap spot is taken';
  const [hx, hy] = heap, ecx = hx + 1, ecy = hy + 1;
  const d = depotRect(st), key = (x: number, y: number) => y * tw + x;
  const track = (x: number, y: number) => placeable(st, 'track', x, y) === '';
  const free = (kind: Kind, x: number, y: number) => placeable(st, kind, x, y) === '';
  const later = lotRectTiles(st, HOUR_LATER_LOT);
  for (let xT = H.x0 + MARGIN_TILES - 1; xT > Y.x1 - MARGIN_TILES; xT--) {
    // stop B east of the track, its inserter, the belt into the Depot (the row nearest the Depot's first)
    let B: { yB: number; belts: BeltStep[] } | null = null;
    for (const yB of rowsFrom(Math.floor(d.y + d.size / 2), H.y0 + 1, H.y1 - 2)) {
      if (!track(xT, yB) || !track(xT, yB + 1) || !free('tramstop', xT + 1, yB) || !free('inserter', xT + 3, yB)) continue;
      const avoid = new Set<number>(later);
      for (let y = H.y0; y <= H.y1; y++) avoid.add(key(xT, y));
      for (const [x, y] of [[xT + 1, yB], [xT + 2, yB], [xT + 1, yB + 1], [xT + 2, yB + 1], [xT + 3, yB]]) avoid.add(key(x, y));
      const belts = beltPath(beltRoutes(st, hq, avoid), xT + 4, yB);
      if (belts) { B = { yB, belts }; break; }
    }
    if (!B) continue;
    // stop A west of the track, its inserter, the supply chest on the yard's side, the Excavator's belts into the platform
    for (const yA of rowsFrom(ecy, Y.y0 + 1, Y.y1 - 2)) {
      if (Math.abs(yA - B.yB) < 2) continue;   // no track tile beside both stops
      if (!free('tramstop', xT - 2, yA) || !free('inserter', xT - 3, yA) || !free('chest', xT - 5, yA) || blockOfTile(st, xT - 5, yA) !== yard) continue;
      const yLo = Math.min(yA, B.yB), yHi = Math.max(yA, B.yB) + 1;
      let ok = true;
      for (let y = yLo; y <= yHi && ok; y++) ok = track(xT, y);
      if (!ok) continue;
      const avoid = new Set<number>();
      for (let y = Y.y0; y <= Y.y1; y++) avoid.add(key(xT, y));
      for (const [x, y] of [[xT - 3, yA], [xT - 5, yA], [xT - 4, yA], [xT - 5, yA + 1], [xT - 4, yA + 1], [xT - 2, yA], [xT - 1, yA], [xT - 2, yA + 1], [xT - 1, yA + 1]]) avoid.add(key(x, y));
      // the tiles a belt may enter the stop from, and its direction into the stop (the inserter's tile is not one)
      const goals = new Map<number, Dir>([[key(xT - 3, yA + 1), 1], [key(xT - 2, yA - 1), 2], [key(xT - 1, yA - 1), 2], [key(xT - 2, yA + 2), 0], [key(xT - 1, yA + 2), 0]]);
      let best: { d: Dir; belts: BeltStep[] } | null = null;
      for (let dd = 0; dd < 4; dd++) {
        const ed = dd as Dir;
        const belts = beltRun(st, [ecx + DX[ed] * 2, ecy + DY[ed] * 2], goals, avoid, { x0: Y.x0, y0: Y.y0, x1: xT - 1, y1: Y.y1 });
        if (belts && (!best || belts.length < best.belts.length)) best = { d: ed, belts };
      }
      if (!best) continue;
      return { xT, yLo, yHi, exc: { x: hx, y: hy, d: best.d }, excBelts: best.belts, stopA: [xT - 2, yA], insA: [xT - 3, yA], chest: [xT - 5, yA], stopB: [xT + 1, B.yB], insB: [xT + 3, B.yB], depotBelts: B.belts };
    }
  }
  return 'no street column between the yard and the HQ takes the track, a stop at each end with its inserter, the chest, and belts to the platform and the Depot';
}
/** The pockets' rubble and magazines into the chest (room for a take). */
const pocketsToChest = (label: string): Task => act(label, (st, out) => {
  for (const item of ['steel', 'copper', 'stone', 'coal', 'magazine', 'wire', 'frame', 'board'] as const) {
    const n = st.engineer.inv[item] ?? 0;
    if (n > 0) out.push({ type: 'chestPut', item, n });
  }
});
function tramLine(bot: HourBot, st: SimState): Task[] {
  const i = bot.claimed.west, G = ground(st), hq = hqIdx(st);
  if (i === undefined || st.blocks[i].state !== HELD) { refused(bot, st, 'the tram line', i === undefined ? 'west was never claimed' : 'west is not Held'); return []; }
  if (i !== G.railYard) { refused(bot, st, 'the tram line', `west (block ${i}) is not the rail yard (block ${G.railYard})`); return []; }
  const lock = lockReason(st, 'track');
  if (lock) { refused(bot, st, 'the tram line', lock); return []; }
  const plan = tramPlan(st, i, hq);
  if (typeof plan === 'string') { refused(bot, st, 'the tram line', plan); return []; }
  bot.tram = plan;
  const trackN = plan.yHi - plan.yLo + 1;
  note(bot, st, `the tram line: Excavator at (${plan.exc.x},${plan.exc.y}) facing ${DIR_NAMES[plan.exc.d]}, ${plan.excBelts.length} belts to stop A at (${plan.stopA[0]},${plan.stopA[1]}), the track on column ${plan.xT} rows ${plan.yLo}–${plan.yHi} (${trackN} tiles), stop B at (${plan.stopB[0]},${plan.stopB[1]}), ${plan.depotBelts.length} belts to the Depot, the supply chest at (${plan.chest[0]},${plan.chest[1]})`);
  const C = MACHINE_COST;
  const yardSteel = C.excavator.steel + plan.excBelts.length * C.belt.steel + C.tramstop.steel + C.inserter.steel + C.chest.steel, yardCu = C.tramstop.copper + C.inserter.copper + C.chest.copper;
  const hqSteel = trackN * C.track.steel + C.tramstop.steel + C.inserter.steel + plan.depotBelts.length * C.belt.steel, hqCu = C.tramstop.copper + C.inserter.copper;
  const t: Task[] = [chest('to the chest', st), pocketsToChest('the pockets\' rubble and magazines into the chest'), takeTask(bot, { steel: yardSteel, copper: yardCu })];
  // the yard's side: the Excavator on the heap, its belts, stop A, the inserter into the supply chest, the chest
  t.push(...putAt(bot, 'excavator', plan.exc.x, plan.exc.y, plan.exc.d, 'the rail yard\'s coal Excavator'));
  for (const b of plan.excBelts) t.push(...putAt(bot, 'belt', b.x, b.y, b.dir));
  t.push(...putAt(bot, 'tramstop', plan.stopA[0], plan.stopA[1], 0, 'stop A (the yard)'));
  t.push(...putAt(bot, 'inserter', plan.insA[0], plan.insA[1], 3, 'stop A\'s inserter into the supply chest'));
  t.push(...putAt(bot, 'chest', plan.chest[0], plan.chest[1], 0, 'the supply chest'));
  // the HQ's side: the track, stop B, the inserter onto the belt into the Depot, the belt
  t.push(chest('to the chest', st), takeTask(bot, { steel: hqSteel, copper: hqCu }));
  for (let y = plan.yLo; y <= plan.yHi; y++) t.push(...putAt(bot, 'track', plan.xT, y, 2, `track (${plan.xT},${y})`));
  t.push(...putAt(bot, 'tramstop', plan.stopB[0], plan.stopB[1], 0, 'stop B (the HQ)'));
  t.push(...putAt(bot, 'inserter', plan.insB[0], plan.insB[1], 1, 'stop B\'s inserter onto the Depot belt'));
  for (const b of plan.depotBelts) t.push(...putAt(bot, 'belt', b.x, b.y, b.dir));
  // the tram, on the track beside stop A
  t.push(chest('to the chest', st), takeTask(bot, { steel: C.tram.steel, copper: C.tram.copper }));
  t.push(...putAt(bot, 'tram', plan.xT, plan.stopA[1], 2, 'the tram'));
  t.push(act('the tram line laid', st2 => {
    if (tramAt(st2, plan.xT, plan.stopA[1])) mark(bot, st2, 'tram-route');
    else refused(bot, st2, 'the tram line', 'no tram on the track at the end of the step');
    mark(bot, st2, 'west-line');
  }));
  return t;
}
/** RI-05: the local depot's materials (SUPPLY_DEPOT_NEED) from the Depot to stop B's platform by hand, to stop A by tram,
 *  into the chest by A's inserter; then the commissioning within reach of the chest. */
function depotSupply(bot: HourBot, st: SimState): Task[] {
  const p = bot.tram;
  if (!p) { refused(bot, st, 'the depot\'s supply by tram', 'no tram route was laid'); return []; }
  const [bx, by] = p.stopB, [cx, cy] = p.chest, need = SUPPLY_DEPOT_NEED;
  return [
    chest('to the chest for the depot\'s supply', st), pocketsToChest('the pockets\' rubble and magazines into the chest'),
    takeTask(bot, { coal: need.coal, magazine: need.magazine }),
    ...within(bot, 'load stop B\'s platform', bx, by, MACHINE_SIZE.tramstop, (st2, out) => {
      for (const [item, n] of Object.entries(need) as ['coal' | 'magazine', number][]) {
        const have = st2.engineer.inv[item] ?? 0;
        if (have > 0) out.push({ type: 'chestPut', item, n: Math.min(n, have), x: bx, y: by });
        else refused(bot, st2, `load ${n} ${item} on the platform`, 'none in the pockets');
      }
    }),
    act('the platform loaded', st2 => { const m = machineAt(st2, bx, by); note(bot, st2, `stop B's platform: ${m && m.kind === 'tramstop' ? poolStr(m.inv) || 'empty' : 'no stop'}`); }),
    until('the depot\'s materials in its chest', st2 => { const s = projectOf(st2, SUPPLY_DEPOT_PROJECT)?.stage; return s === 'ready' || s === 'restored'; }, 300),
    ...within(bot, 'commission the depot at its chest', cx, cy, MACHINE_SIZE.chest, (st2, out) => {
      const chk = commissionCheck(st2, SUPPLY_DEPOT_PROJECT);
      if (!chk.ok) { refused(bot, st2, 'commission the supply depot', chk.reason); return; }
      out.push({ type: 'commission', id: SUPPLY_DEPOT_PROJECT });
    }),
    act('the depot', st2 => { const r = projectOf(st2, SUPPLY_DEPOT_PROJECT); note(bot, st2, r ? describeProject(st2, r) : 'no depot project'); }),
    chest('back from the depot', st),
  ];
}

// ------------------------------------------------------------------ the bot

export function createHourBot(rifle = false, coalPlan: 'chest' | 'wait' = 'chest', northAt = HOUR_CLAIM_AT.north, claimPath: 'physical' | 'map' = 'physical', route: 'belt' | 'tram' = 'belt', heart = false): HourBot {
  const bot: HourBot = { rifle, log: { entries: [], marks: {}, walks: [], refused: [] }, steps: [], next: 0, queue: [], claimed: {}, lastRun: HOUR_RUN_FROM - HOUR_RUN_GAP, aiming: false, fellWhy: {}, handsOff: false, ticks: 0, fedAtFeedDone: -1,
    coalPlan, claimPath, lastFeed: -Infinity, stock: [], steelMin: Infinity, steelMinAt: -1, copperMin: Infinity, coalMin: Infinity, northAt, atFirstRed: null, coalZeroAt: -1, route, tram: null, heart };
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
  if (f.stats.made.wire > 0) mark(bot, st, 'first-wire');
  // RI-01 (D-P4-12): the rail yard's first coal unit dug, and the first to reach the Depot by belt — read as the
  // Depot having taken more coal by machine than the HQ patch's Excavator ever dug (hand-mined coal goes to the
  // pockets, never by belt), so the mark lags the true arrival by the patch coal still on its belt at the time.
  if (f.stats.railCoal > 0) mark(bot, st, 'rail-coal-mined');
  // RI-05: the project's moments — the yard restored and its kit unlocked (the same second: no unlock before the
  // restoration), the tram's first unload, the depot's materials in its chest, the depot commissioned
  if (f.projects[RAIL_YARD_PROJECT]?.stage === 'restored') mark(bot, st, 'rail-yard-restored');
  if (lockReason(st, 'track') === '') mark(bot, st, 'rail-kit-unlocked');
  if ((f.stats.tramMoved ?? 0) > 0) mark(bot, st, 'tram-first-delivery');
  const dp = f.projects[SUPPLY_DEPOT_PROJECT];
  if (dp?.stage === 'ready' || dp?.stage === 'restored') mark(bot, st, 'depot-supplied');
  if (dp?.stage === 'restored') mark(bot, st, 'depot-restored');
  if (f.stats.delivered.coal > f.stats.minedOf.coal - f.stats.handMinedOf.coal - f.stats.railCoal) mark(bot, st, 'rail-coal-arrived');
  // RI-06: the Heart's moments — the first body born, each threshold's packet (once per attempt: the mark keeps the first), a feeder down / repaired, an interruption, the destruction
  const Hs = f.heart;
  if (Hs) {
    if (Hs.stats.born > 0) mark(bot, st, 'heart-first-body');
    for (const key in Hs.requested) mark(bot, st, `packet-${key.split(':')[1]}`, Hs.requested[key]);
    if (Hs.stats.knockouts > 0) mark(bot, st, 'cabinet-down');
    if (Hs.stats.repairs > 0) mark(bot, st, 'cabinet-repaired');
    if (Hs.stats.interrupted > 0) mark(bot, st, 'heart-interrupted');
    if (Hs.destroyed) mark(bot, st, 'heart-destroyed', Hs.destroyedAt);
  }
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
  /** RI-01 (D-P4-12, M6's check (a)): the rail yard's coal units dug; when the first reached the Depot (`rail-coal-arrived`,
   *  -1 never); when the chest's coal ran out for good (-1: it had coal at the last sample); the margin between them in
   *  seconds (the run's end stands in for a chest that never ran out; -Infinity when the coal never arrived). */
  railCoal: number; railArrival: number; coalOut: number; railMargin: number;
  /** RI-01: what reached the Depot by machine and what the recipes made, by item; the hand-feeds split (D-P4-11). */
  delivered: Record<Item, number>; made: Record<Item, number>; handFedMags: number; handFedCoal: number;
  /** HOUR_END for this city: one Excavator fewer where east has no rubble (D-P4-2 / D-P4-3). */
  endWant: { generators: number; excavators: number; assemblers: number; turrets: number; held: number };
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
  // RI-01: the claims' real lines (D-P4-5) and the rail yard's coal against the chest's (D-P4-12, M6 (a))
  const burnE = burnOffS(0.22);
  const eastRubble = bot.claimed.east === undefined ? null : ground(st).blocks[bot.claimed.east].rubble;
  const endWant = { ...HOUR_END, excavators: HOUR_END.excavators - (bot.claimed.east !== undefined && !eastRubble ? 1 : 0) };
  if (eastRubble) expect('east-line', HOUR_CLAIM_AT.east + burnE, HOUR_CLAIM_AT.east + burnE + 6 * 60, `east's ${eastRubble} Excavator and belt to the Depot`);
  expect('wire-assembler', HOUR_CLAIM_AT.east + burnE, HOUR_CLAIM_AT.east + burnE + 8 * 60, 'the Wire Assembler on its recipe');
  expect('west-line', HOUR_CLAIM_AT.west + burnE, HOUR_CLAIM_AT.west + burnE + 6 * 60, 'the rail yard\'s coal Excavator and belt to the Depot');
  expect('rail-coal-mined', HOUR_CLAIM_AT.west + burnE, HOUR_CLAIM_AT.west + burnE + 8 * 60, 'the rail yard\'s first coal unit dug');
  let lastWith = -1; bot.stock.forEach((x, k) => { if (x.coal > 0) lastWith = k; });
  const coalOut = lastWith + 1 < bot.stock.length ? bot.stock[lastWith + 1].t : -1;
  const railArrival = m['rail-coal-arrived'] ?? -1;
  const railMargin = railArrival < 0 ? -Infinity : (coalOut < 0 ? Math.max(st.t, HOUR_S) : coalOut) - railArrival;
  if (st.t >= HOUR_S - 1) {
    if (railArrival < 0) findings.push('the rail yard\'s coal never reached the Depot (RI-01: at the Generators ≥ 10 min before the Depot\'s coal is gone)');
    else if (railMargin < 600) findings.push(`the rail yard's coal reached the Depot at ${mm(railArrival)}, ${(railMargin / 60).toFixed(1)} min before the chest's coal ran out for good at ${mm(coalOut)} (RI-01: ≥ 10 min)`);
  }
  // §11 30–75 min. North's claim, the HQ's white border (its third neighbour Held) and the Electricians walking out
  // of civic north all wait on north (D-P4-10 (a): minute 65, inside the 75-minute hour — D-HOUR-3). They are scored
  // once the run has passed north's minute; a shorter run (the 45:00 unit test) reaches none of them.
  if (st.t >= bot.northAt + 60) {
    expect('held-north', bot.northAt, bot.northAt + 8 * 60, 'north Held (D-HOUR-3: inside the hour)');
    expect('claim-north', bot.northAt, bot.northAt + 5 * 60, `north claimed (${bot.northAt === HOUR_CLAIM_AT.north ? `constants.HOUR: minute ${HOUR_CLAIM_AT.north / 60}, D-P4-10` : `the variant's ${bot.northAt / 60}:00`})`);
    expect('enclosure', bot.northAt, bot.northAt + 10 * 60, 'the HQ interior (§11: the white border when north holds)');
    expect('electricians', 0, bot.northAt + 10 * 60, 'the Electricians in the Depot');
  }
  expect('first-shade', 32 * 60, 47 * 60, 'the first shade (§11/E3: minute 32–47)');
  expect('generator-4', 42 * 60, 50 * 60, 'the fourth Generator (E4-doc: minute 45)');
  expect('copper-2', HOUR_COPPER2_AT, HOUR_COPPER2_AT + 3 * 60, 'the second copper Excavator (after Generator 4, so no brownout)');
  const gens = count('generator'), exc = count('excavator'), asm = count('assembler'), tur = count('turret');
  if (st.t >= HOUR_S - 1) {
    if (gens !== HOUR_END.generators) findings.push(`${gens} Generators at the hour (§11: ${HOUR_END.generators})`);
    if (exc !== endWant.excavators) findings.push(`${exc} Excavators at the hour (§11: ${endWant.excavators}${endWant.excavators !== HOUR_END.excavators ? ', east has nothing to dig' : ''})`);
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
  if (st.t >= HOUR_S - 1 && held !== HOUR_END.held) findings.push(`${held} blocks Held at the hour (§11: ${HOUR_END.held})`);
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
    railCoal: f.stats.railCoal, railArrival, coalOut, railMargin, delivered: { ...f.stats.delivered }, made: { ...f.stats.made },
    handFedMags: f.stats.handFedMags, handFedCoal: f.stats.handFedCoal, endWant,
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
    opts.every?.(st);
    st.events.length = 0;
  }
  // UI commands can be committed while paused, including at the final tick. Apply that
  // boundary's logged inputs without advancing production, movement or combat another tick.
  cmds.length = 0;
  while (k < log.length && log[k].tick <= f.tick) {
    const c = log[k++].c;
    if (c.type !== 'setSpeed' && !(opts.dropAim && c.type === 'aim')) cmds.push(c);
  }
  applyCommands(st, cmds);
  st.events.length = 0;
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
