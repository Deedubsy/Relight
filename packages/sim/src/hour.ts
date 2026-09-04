/** Prompt B M6 "The hour" (run name B-M6-hour): a bot that plays §11's minute list on the tile layer with real
 *  walking and real pockets — to the steel patch and back to the workbench, the magazines walked to the turrets,
 *  the line placed from the pockets in trips to the chest, east / west / north claimed on the calibration's clock
 *  (firsthour.ts FIRST_HOUR_CLAIMS: 15 / 25 / 40 min) with the kit walked over, the two idle turrets carried to
 *  north, the hands running magazines and coal every five minutes — and writes down when each thing happened.
 *  `hourReport` turns the log into findings against §11's prose and the calibration timeline. Every command goes
 *  through `Command` (types.ts), so a session under the bot replays like a played one (`replay`, Gate B's "did the
 *  rifle matter" row). The bot is a dev aid: not a player control, never on by default (`?autoplay=hour`). */
import { SimState, Command, HELD, DARK, CONTESTED, INERT } from './types';
import { edgeTo } from './graph';
import { hqIdx, RIFLE_RANGE } from './engineer';
import { ground, hqLot, inReach, cityGeomOf } from './ground';
import { segBetween, frontTiles } from './city/geom';
import { findPath, passable } from './walk';
import { burnOffS, isCandidate } from './sim';
import { pipOf, edgeCap } from './queries';
import { TURRET_HOPPER, TURRET_RANGE } from './recipes';
import { HQ_PATCHES, P_STEEL, DEPOT_LOT, DEPOT_TILES } from './tiles';
import {
  Kind, Dir, Machine, depotRect, chestCount, machineAt, turretEdge, placeable, canPlace, faceSegOf, rubbleAt,
  MACHINE_SIZE, MACHINE_COST, GENERATOR_COAL_CAP, survivorJoined, advanceFlow, TILE_DT, TILE_TPS, ensureFlow,
} from './flow';
import { threatActive, threatOf } from './threat';
import { HOUR_CLAIM_MIN, HOUR_GENERATOR_MIN, HOUR_SHOT_LINE_MIN } from './constants';

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
  /** Turrets the bot carried to north (M6's "two turrets carried"). */
  carried: number;
  /** For the report: the state's ticks the bot has seen, and the hand-fed count when §11's two feeds were done. */
  ticks: number;
  fedAtFeedDone: number;
}

/** GAME-ASSUMPTION (M6): the bot's numbers where §11 gives none — 20 steel hand-mined (ten magazines' worth), ten
 *  magazines crafted, six kits a claim (one an edge, the spare comes back), a rounds run every five minutes from
 *  minute 10 (20 magazines and 50 coal a trip, turrets and Generators at or under half topped up). */
export const HOUR_MINE_STEEL = 20, HOUR_CRAFT_MAGS = 10, HOUR_KITS = 6, HOUR_RUN_FROM = 10 * 60, HOUR_RUN_GAP = 5 * 60;
export const HOUR_RUN_MAGS = 20, HOUR_RUN_COAL = 50;
/** The calibration's claim minutes and E4-doc's Generator minutes (0 / 6 / 15 / 45), from constants.ts (§11's minute list). */
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

const hqTurrets = (st: SimState): Machine[] => {
  const hq = hqIdx(st), G = ground(st);
  return st.flow!.machines.filter(m => m.kind === 'turret' && G.owner[m.y * G.tw + m.x] === hq);
};

/** An HQ turret whose edge faces a Held (or inert) block, or none at all, fires at nothing: idle, worth carrying. */
function idleTurret(st: SimState, m: Machine): boolean {
  const id = turretEdge(st, m);
  if (id < 0) return true;
  const b = st.blocks[edgeTo(st, id)];
  return b.state === HELD || b.state === INERT;
}

// ------------------------------------------------------------------ task builders

const chest = (label: string, st: SimState): Task => { const d = depotRect(st); return goto(label, d.x, d.y, d.size, label); };

/** Take from the chest; what the chest cannot give is a refusal in the log (D-P4-4's start stock). */
function takeTask(bot: HourBot, wants: Partial<Record<'steel' | 'copper' | 'coal' | 'magazine' | 'kit', number>>): Task {
  return act('take from the chest', (st, out) => {
    for (const [item, n] of Object.entries(wants) as ['steel' | 'copper' | 'coal' | 'magazine' | 'kit', number][]) {
      if (n <= 0) continue;
      const have = chestCount(st, item);
      if (have < n) refused(bot, st, `take ${n} ${item}`, `the chest has ${have}`);
      out.push({ type: 'chestTake', item, n: Math.min(n, have) });
    }
  });
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

/** Hand-feed a machine from the pockets (the `feed` command: E on the machine). */
const feedTask = (kind: string, tx: number, ty: number): Task => act(`feed the ${kind}`, (_st, out) => { out.push({ type: 'feed', x: tx, y: ty }); });

function putGenerator(bot: HourBot, n: number, lx: number, ly: number): Task[] {
  return [...put(bot, 'generator', lx, ly, 0), act(`coal into Generator ${n}`, st => {
    const [tx, ty] = hqLot(st, lx, ly);
    if (machineAt(st, tx, ty)?.kind !== 'generator') return;
    mark(bot, st, `generator-${n}`);
    return [feedTask('Generator', tx, ty)];
  })];
}

/** M4's proven HQ-lot line (the E4-doc layout `$S/m4measure.ts` placed): three Excavators with belts into the Depot. */
function excavatorLine(bot: HourBot): Task[] {
  const t: Task[] = [];
  t.push(...put(bot, 'excavator', 18, 14, 3), ...put(bot, 'belt', 17, 15, 3), ...put(bot, 'belt', 16, 15, 3), ...put(bot, 'belt', 15, 15, 0), ...put(bot, 'belt', 15, 14, 3));   // coal → Depot
  t.push(...putGenerator(bot, 2, 16, 8));
  t.push(...put(bot, 'excavator', 1, 7, 0)); for (let lx = 2; lx <= 21; lx++) t.push(...put(bot, 'belt', lx, 6, 1));
  t.push(...put(bot, 'belt', 22, 6, 2), ...put(bot, 'belt', 22, 7, 2), ...put(bot, 'belt', 22, 8, 2));
  t.push(...put(bot, 'excavator', 1, 14, 2)); for (let lx = 2; lx <= 20; lx++) t.push(...put(bot, 'belt', lx, 17, 1));
  t.push(...put(bot, 'belt', 21, 17, 0), ...put(bot, 'belt', 21, 16, 0), ...put(bot, 'belt', 21, 15, 0), ...put(bot, 'belt', 21, 14, 0));
  t.push(act('the line placed', st => { mark(bot, st, 'line-excavators'); }));
  return t;
}
function assemblerLine(bot: HourBot): Task[] {
  const t: Task[] = [];
  t.push(...put(bot, 'inserter', 22, 9, 2), ...put(bot, 'assembler', 21, 10, 0), ...put(bot, 'inserter', 21, 13, 0), ...put(bot, 'inserter', 20, 11, 3));
  for (let lx = 19; lx >= 15; lx--) t.push(...put(bot, 'belt', lx, 11, 3));
  t.push(act('the Shot line placed', st => { mark(bot, st, 'line-assembler'); }));
  return t;
}

/** The turrets' feed run: every HQ turret in the given order, E on each from the pockets. */
function feedTurrets(bot: HourBot, order: (st: SimState) => Machine[], walk: string): Task {
  return act('to the turrets', st => {
    const t: Task[] = [];
    for (const m of order(st)) t.push(goto('turret', m.x, m.y, m.size, walk), feedTask('turret', m.x, m.y));
    return t;
  });
}
const westThenEast = (st: SimState): Machine[] => {
  const hq = hqIdx(st), rank = (m: Machine) => { const id = turretEdge(st, m); if (id < 0) return 9; const d = dirOf(st, hq, edgeTo(st, id)); return d === 'west' ? 0 : d === 'east' ? 1 : d === 'north' ? 2 : 3; };
  return hqTurrets(st).sort((a, b) => rank(a) - rank(b));
};

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
        act(`${dir} kitted`, st2 => { if (kitted(st2, i)) mark(bot, st2, `kitted-${dir}`); }),
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
  const t: Task[] = [chest('to the chest', st), takeTask(bot, { magazine: HOUR_RUN_MAGS, coal: HOUR_RUN_COAL })];
  for (const m of low) t.push(goto(m.kind, m.x, m.y, m.size, 'rounds run'), feedTask(m.kind, m.x, m.y));
  t.push(act('rounds run done', st2 => { mark(bot, st2, 'first-rounds-run'); }));
  return t;
}

/** Pick up two idle HQ turrets and stand them on north's lot facing a Dark neighbour's street (§11's "two turrets carried"). */
function carryTurrets(bot: HourBot): Task[] {
  return [act('carry two turrets to north', st => {
    const north = bot.claimed.north;
    if (north === undefined || st.blocks[north].state !== HELD) { refused(bot, st, 'carry turrets', 'north is not Held'); return; }
    const idle = hqTurrets(st).filter(m => idleTurret(st, m)).slice(0, 2);
    if (idle.length < 2) refused(bot, st, 'carry two turrets', `${idle.length} idle HQ turret(s) (the others still face a Dark block)`);
    if (!idle.length) return;
    const t: Task[] = [];
    for (const m of idle) t.push(goto('turret', m.x, m.y, m.size, 'to the idle turret'), act('pick the turret up', (_s, out) => { out.push({ type: 'pickUp', x: m.x, y: m.y }); }));
    t.push(act('to north with the turrets', st2 => {
      const carried = st2.engineer.inv.turret ?? 0;
      if (carried < 1) { refused(bot, st2, 'carry turrets', 'no turret in the pockets'); return; }
      bot.carried = carried;
      mark(bot, st2, 'turrets-picked-up');
      const spots = turretSpots(st2, north, carried);
      if (spots.length < carried) refused(bot, st2, 'stand the turrets on north', `${spots.length} spot(s) on north's front within range of a Dark street`);
      const tt: Task[] = [];
      for (const s of spots) tt.push(goto('north\'s front', s.x, s.y, 2, 'carrying to north'),
        act('place the turret', (_s, out) => { out.push({ type: 'place', item: 'turret', x: s.x, y: s.y, dir: s.dir }); }), feedTask('turret', s.x, s.y));
      tt.push(act('turrets carried', st3 => { mark(bot, st3, 'turrets-carried'); note(bot, st3, `${st3.flow!.machines.filter(m => m.kind === 'turret').length} turrets standing`); }));
      return tt;
    }));
    return t;
  })];
}

/** Placeable 2×2 spots on block `i`'s front toward its Dark neighbours, within turret range of the shared street's ridge, facing it. */
function turretSpots(st: SimState, i: number, n: number): { x: number; y: number; dir: Dir }[] {
  const cg = cityGeomOf(st), tw = cg.tw, out: { x: number; y: number; dir: Dir }[] = [];
  for (const j of st.nb[i]) {
    if (out.length >= n) break;
    const b = st.blocks[j];
    if (b.state !== DARK && b.state !== CONTESTED) continue;
    const sg = segBetween(cg, i, j);
    if (!sg) continue;
    const front = frontTiles(cg, i, j);
    let best: { x: number; y: number; dir: Dir; d: number } | null = null;
    for (let k = 0; k < front.length; k++) {
      const t = front[k], tx = t % tw, ty = (t - tx) / tw;
      if (placeable(st, 'turret', tx, ty) || faceSegOf(st, i, tx, ty, 2) !== j) continue;
      const cx = tx + 1, cy = ty + 1;
      let nd = Infinity, nx = sg.mx, ny = sg.my;
      for (let q = 0; q < sg.ridge.length; q++) { const r = sg.ridge[q], rx = r % tw, ry = (r - rx) / tw, d = Math.hypot(rx + 0.5 - cx, ry + 0.5 - cy); if (d < nd) { nd = d; nx = rx + 0.5; ny = ry + 0.5; } }
      if (nd >= TURRET_RANGE) continue;
      const dx = nx - cx, dy = ny - cy, dm = Math.hypot(cx - (sg.mx + 0.5), cy - (sg.my + 0.5));
      const dir: Dir = Math.abs(dx) >= Math.abs(dy) ? (dx > 0 ? 1 : 3) : (dy > 0 ? 2 : 0);
      if (!best || dm < best.d) best = { x: tx, y: ty, dir, d: dm };
    }
    if (best) out.push({ x: best.x, y: best.y, dir: best.dir });
  }
  return out;
}

// ------------------------------------------------------------------ §11's minute list

export function hourSteps(bot: HourBot): HourStep[] {
  const gen = (n: number, lx: number, ly: number): HourStep => ({ at: HOUR_GEN_AT[n - 1], name: `§11 ${HOUR_GEN_AT[n - 1] / 60}:00 — Generator ${n}`, tasks: st => [
    chest('to the chest', st), takeTask(bot, { steel: MACHINE_COST.generator.steel, copper: MACHINE_COST.generator.copper, coal: GENERATOR_COAL_CAP }), ...putGenerator(bot, n, lx, ly)] });
  const standIn = (at: number, name: string, kinds: [Kind, number, number][]): HourStep => ({ at, name, tasks: st => {
    const steel = kinds.reduce((s, k) => s + MACHINE_COST[k[0]].steel, 0), copper = kinds.reduce((s, k) => s + MACHINE_COST[k[0]].copper, 0);
    const t: Task[] = [chest('to the chest', st), takeTask(bot, { steel, copper })];
    for (const [k, lx, ly] of kinds) t.push(...put(bot, k, lx, ly, 0));
    return t;
  } });
  const claimAt = (dir: HourDir): HourStep => ({ at: HOUR_CLAIM_AT[dir], name: `§11 ${HOUR_CLAIM_AT[dir] / 60}:00 — claim ${dir}`, tasks: st => [
    chest('to the chest for kits', st), takeTask(bot, { kit: HOUR_KITS }), ...claimStep(bot, dir)] });
  const burnE = burnOffS(0.22);
  return [
    { at: 0, name: '§11 0:00 — to the steel patch, hand-mine 20 steel', tasks: st => {
      const p = steelTile(st);
      if (!p) { refused(bot, st, 'mine steel', 'no steel patch tile with units'); return []; }
      const mine = (st2: SimState, out: Command[]) => { const q = steelTile(st2); if (q) out.push({ type: 'mineAt', x: q[0], y: q[1] }); };
      return [goto('the steel patch', p[0], p[1], 1, 'to the steel patch'), act('mine', mine),
        until(`${HOUR_MINE_STEEL} steel in the pockets`, st2 => (st2.engineer.inv.steel ?? 0) >= HOUR_MINE_STEEL, HOUR_MINE_STEEL * 3,
          (st2, out) => { if (!st2.flow!.hand.mine && st2.flow!.tick % TILE_TPS === 0) mine(st2, out); }),
        act('mined', st2 => { mark(bot, st2, 'mine-done'); note(bot, st2, `${st2.engineer.inv.steel ?? 0} steel in the pockets`); })];
    } },
    { at: 0, name: '§11 — back to the workbench: craft ten magazines', tasks: st => {
      let before = 0;
      return [
      chest('to the workbench', st),
      takeTask(bot, { copper: HOUR_CRAFT_MAGS }),
      act('craft', (st2, out) => { before = st2.flow!.stats.handCrafted; out.push({ type: 'craft', item: 'magazine', count: HOUR_CRAFT_MAGS }); }),
      until('the magazines crafted', st2 => st2.flow!.stats.handCrafted >= before + HOUR_CRAFT_MAGS, HOUR_CRAFT_MAGS * 3 + 30),
      act('crafted', st2 => { mark(bot, st2, 'craft-done'); note(bot, st2, `${st2.flow!.stats.handCrafted - before} crafted, ${st2.engineer.inv.magazine ?? 0} magazines in the pockets`); }),
      takeTask(bot, { magazine: HOUR_RUN_MAGS }),
    ]; } },
    { at: 0, name: '§11 — magazines walked to the west and east turrets, twice each', tasks: st => [
      feedTurrets(bot, westThenEast, 'to the turrets'),
      chest('back to the chest', st), takeTask(bot, { magazine: HOUR_RUN_MAGS }),
      feedTurrets(bot, westThenEast, 'to the turrets again'),
      act('fed', st2 => { mark(bot, st2, 'feed-done'); bot.fedAtFeedDone = st2.flow!.stats.handFed; note(bot, st2, `${st2.flow!.stats.handFed} magazines fed by hand`); }),
    ] },
    { at: HOUR_GEN_AT[1], name: '§11 6:00 — Generator 2, the coal Excavator, the steel and copper Excavators and their belts', tasks: st => {
      const steel = 3 * MACHINE_COST.excavator.steel + 50 * MACHINE_COST.belt.steel + MACHINE_COST.generator.steel;
      return [chest('to the chest', st), takeTask(bot, { steel, copper: MACHINE_COST.generator.copper, coal: GENERATOR_COAL_CAP }), ...excavatorLine(bot)];
    } },
    { at: HOUR_SHOT_LINE_MIN * 60, name: '§11 8:00 — the Shot assembler, three inserters, the ammo belt', tasks: st => {
      const steel = MACHINE_COST.assembler.steel + 3 * MACHINE_COST.inserter.steel + 5 * MACHINE_COST.belt.steel, copper = MACHINE_COST.assembler.copper + 3 * MACHINE_COST.inserter.copper;
      return [chest('to the chest', st), takeTask(bot, { steel, copper }), ...assemblerLine(bot)];
    } },
    gen(3, 22, 3),
    claimAt('east'),
    standIn(HOUR_CLAIM_AT.east + burnE + 60, 'stand-in Excavator for east\'s copper (E4, D-P4-5)', [['excavator', 7, 1]]),
    standIn(HOUR_CLAIM_AT.east + burnE + 120, 'stand-in Assembler (E4\'s wire recipe, D-P4-5)', [['assembler', 11, 1]]),
    claimAt('west'),
    standIn(HOUR_CLAIM_AT.west + burnE + 60, 'stand-in Excavator for west\'s coal (E4, D-P4-5)', [['excavator', 3, 1]]),
    claimAt('north'),
    { at: HOUR_CLAIM_AT.north, name: '§11 — the two idle turrets carried to north', tasks: () => carryTurrets(bot) },
    { at: HOUR_GEN_AT[3], name: '§11 45:00 — Generator 4, the fifth Excavator and third Assembler (E4 stand-ins)', tasks: st => [
      chest('to the chest', st),
      takeTask(bot, { steel: MACHINE_COST.generator.steel + MACHINE_COST.excavator.steel + MACHINE_COST.assembler.steel,
                      copper: MACHINE_COST.generator.copper + MACHINE_COST.excavator.copper + MACHINE_COST.assembler.copper, coal: GENERATOR_COAL_CAP }),
      ...putGenerator(bot, 4, 24, 8), ...put(bot, 'excavator', 14, 1, 0), ...put(bot, 'assembler', 6, 12, 0),
    ] },
  ];
}

// ------------------------------------------------------------------ the bot

export function createHourBot(rifle = false): HourBot {
  const bot: HourBot = { rifle, log: { entries: [], marks: {}, walks: [], refused: [] }, steps: [], next: 0, queue: [], claimed: {}, lastRun: HOUR_RUN_FROM - HOUR_RUN_GAP, aiming: false, carried: 0, ticks: 0, fedAtFeedDone: -1 };
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
  if (f.stats.magsMade > 0) mark(bot, st, 'first-line-magazine');
  if (f.tick % TILE_TPS === 0) {
    for (const ed of st.ring) {
      if (ed.born === st.t || ed.kit === false) continue;
      const p = pipOf(ed.hopper / edgeCap(st, ed));
      if (p !== 'green') mark(bot, st, 'first-amber');
      if (p === 'red') mark(bot, st, 'first-red');
    }
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

/** One call a frame (or a tick): the bot's commands for this frame. Needs the flow layer (a city with tiles). */
export function hourCommands(st: SimState, bot: HourBot, out: Command[]): void {
  if (!st.flow || st.lattice) return;
  bot.ticks++;
  watch(st, bot);
  if (bot.rifle) reflex(st, bot, out);
  const e = st.engineer;
  if (e.down >= 0) return;
  if (!bot.queue.length) {
    const s = bot.steps[bot.next];
    if (s && st.t >= s.at) {
      bot.next++;
      const late = st.t - s.at;
      note(bot, st, `${s.name}${late > 5 ? ` (${late.toFixed(0)} s late)` : ''}`);
      bot.queue.push(...s.tasks(st, bot));
    } else if (st.t >= HOUR_RUN_FROM && st.t - bot.lastRun >= HOUR_RUN_GAP) {
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
  /** The claim walk-overs' seconds (§11: about 40 s each; the rework: under a minute of hour one for all of them). */
  claimWalkS: number;
  held: number; hqHeld: boolean; turrets: number; generators: number; excavators: number; assemblers: number;
  brownoutS: number; crawlers: number; shades: number; turretKills: number; rifleKills: number; fired: number; magsMade: number;
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
  expect('feed-done', 0, 10 * 60, 'magazines walked to the west and east turrets twice');
  if (m['feed-done'] !== undefined && bot.fedAtFeedDone === 0) findings.push('the turrets took no magazines by hand in the first ten minutes (their hoppers were full: §11\'s two feeds each moved nothing)');
  expect('first-crawler', 60, 6 * 60, 'the first bloom (§11: north at about minute 3)');
  expect('line-excavators', 0, 8 * 60, 'Excavators and belts placed');
  expect('line-assembler', 0, 10 * 60, 'the Shot line placed (§11: ammo automated by minute 10)');
  expect('generator-2', 5 * 60, 8 * 60, 'the second Generator (§11: minute 6)');
  expect('first-line-magazine', 0, 12 * 60, 'the first magazine off the line');
  // §11 10–30 min
  expect('claim-east', 10 * 60, 30 * 60, 'east claimed');
  expect('held-east', 10 * 60, 32 * 60, 'east Held');
  const wo = bot.log.walks.find(w => w.name === 'walk-over east');
  if (wo) { const s = wo.t1 - wo.t0; if (s < 20 || s > 60) findings.push(`the walk over to east took ${s.toFixed(0)} s (§11: about 40 s)`); }
  expect('generator-3', 12 * 60, 20 * 60, 'the third Generator (E4-doc: minute 15)');
  expect('claim-west', 15 * 60, 30 * 60, 'west claimed');
  // §11 30–60 min
  expect('claim-north', 30 * 60, 45 * 60, 'north claimed (the cap)');
  expect('enclosure', 35 * 60, 60 * 60, 'the HQ interior (§11: the white border at about minute 40; calibration: enclosure 45–60)');
  expect('turrets-carried', 38 * 60, 55 * 60, 'the two turrets carried to north');
  if (bot.carried < 2 && m['claim-north'] !== undefined) findings.push(`${bot.carried} turret(s) carried, §11 says two`);
  expect('electricians', 0, 60 * 60, 'the Electricians in the Depot');
  expect('first-shade', 32 * 60, 47 * 60, 'the first shade (§11/E3: minute 32–47)');
  expect('generator-4', 42 * 60, 50 * 60, 'the fourth Generator (E4-doc: minute 45)');
  const gens = count('generator'), exc = count('excavator'), asm = count('assembler'), tur = count('turret');
  if (st.t >= 3600 - 1) {
    if (gens !== 4) findings.push(`${gens} Generators at the hour (§11: 4)`);
    if (exc !== 5) findings.push(`${exc} Excavators at the hour (§11: 5)`);
    if (asm !== 3) findings.push(`${asm} Assemblers at the hour (§11: 3)`);
    if (tur !== 8) findings.push(`${tur} turrets at the hour (§11: 8)`);
    if (f.power.overS > 0) findings.push(`${f.power.overS.toFixed(0)} s of brownout (§11: none)`);
  }
  // the rework's findings re-checked
  if (claimWalkS > 60) findings.push(`the claim walk-overs took ${claimWalkS.toFixed(0)} s in all (rework: under a minute of hour one)`);
  if (walkedPct > 15) findings.push(`walked ${walkedPct.toFixed(1)} % of the hour (§19's budget: 15 %)`);
  if (e.truckFound) findings.push('the truck was found inside the hour (rework: outside it)');
  // the calibration timeline (docs/experiments/lattice/calibration.md)
  expect('first-amber', 16 * 60, 31 * 60, 'the first amber pip');
  expect('first-red', 16 * 60, 31 * 60, 'the first red pip');
  const held = st.blocks.filter(b => b.state === HELD).length;
  if (st.t >= 3600 - 1 && held !== 4) findings.push(`${held} blocks Held at the hour (calibration: 4)`);
  if (m['hq-fell'] !== undefined) findings.push(`the HQ fell at ${mm(m['hq-fell'])}`);
  for (const dir of DIRS) if (m[`fell-${dir}`] !== undefined) findings.push(`${dir} fell at ${mm(m[`fell-${dir}`])}`);
  for (const r of bot.log.refused) findings.push(`refused at ${mm(r.t)}: ${r.what} — ${r.reason}`);
  return {
    marks: { ...m }, walks: bot.log.walks.slice(), refused: bot.log.refused.slice(), entries: bot.log.entries.slice(),
    walkedS, walkedPct, chestTrips: f.stats.chestTrips, handFed: f.stats.handFed, reachRefused: f.stats.reachRefused, claimWalkS,
    held, hqHeld: st.blocks[hq].state === HELD, turrets: tur, generators: gens, excavators: exc, assemblers: asm,
    brownoutS: f.power.overS, crawlers: T?.stats.spawned ?? 0, shades: T?.stats.shades ?? 0, turretKills: T?.stats.turretKills ?? 0,
    rifleKills: T?.stats.rifleKills ?? 0, fired: e.fired, magsMade: f.stats.magsMade, findings,
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
    st.events.length = 0;
  }
}

