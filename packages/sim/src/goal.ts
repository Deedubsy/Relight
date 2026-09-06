/** RI-02 — the current-goal line (D-GB-2 (a), constitution rule 8's form; plan §11.2 "prominent current goal with
 *  the reason it matters"). One line, read off the sim's own state every time it is asked: the next §11 constraint
 *  the player has not met, with the numbers that make it matter. No list, no screens, no quest state — the goal
 *  is a pure function of the state, so a save reloads to the same line and a replay never disagrees with it.
 *
 *  Implementation default (RI-02, reversible; not a historical approval): the goals follow §11's rows
 *  (constants.ts HOUR) in schedule order, each skipped once the state meets it, and each direction's claim has a
 *  "hold" form while the block is Contested and a "retake" form after it fell. A second, amber line — the support —
 *  names an immediate shortage (down, no coal, an empty street, a brownout) without replacing the goal (§11.2:
 *  "immediate shortages support that goal rather than constantly replacing it"). `machineStatus` is the readable
 *  running / starved / blocked / idle / off word of the same section. E-hour samples `currentGoal` at 1 Hz through
 *  the logged hour and checks every id against the state it claims (packages/harness goalcheck.ts). */
import { SimState, HELD, DARK, CONTESTED, Edge } from './types';
import { describeSite } from './expansion';
import { isCampaign } from './rules';
import {
  Machine, MACHINE_COST, MACHINE_KW, SHOT, ASM_OUTPUT_CAP, throttle, rubbleAt, machineAt, accepts,
  recipeOf, recipeOutput, subPowered, poleGrid, inputTile, outputTile, findRubble, asmCanStart, costStr, Item,
  isFieldKind, fieldBlock, powered, invTotal, tramAt, tramRoute,
} from './flow';
import { ground, hqLot, blockOfTile } from './ground';
import { HQ_PATCHES, P_COAL, P_COPPER, P_STEEL, RAIL_YARD_COAL } from './tiles';
import { hqIdx } from './engineer';
import { burnOffS, heldCount, rotOf } from './sim';
import { claimInfo, ammoStatus, edgeCap } from './queries';
import { COAL_MJ, GENERATOR_KW, TURRET_HOPPER } from './recipes';
import { EXCAVATOR_PER_S } from './constants';
import { HOUR_MINE_STEEL, HOUR_CRAFT_MAGS, HOUR_END } from './hour';
import { blockName, hqNeighbourToward, streetName } from './names';
import { heartAt, describeHeart } from './heart';   // RI-06

export type GoalDir = 'east' | 'west' | 'north';
export type GoalId =
  | 'home-factory' | 'home-explore'
  | 'hq-fell' | 'front' | 'mine' | 'craft' | 'coal-line' | 'gen-2' | 'steel-line' | 'copper-line' | 'shot-line' | 'steel-2' | 'gen-3'
  | 'claim-east' | 'contest-east' | 'retake-east' | 'claim-west' | 'contest-west' | 'retake-west' | 'rail-coal'
  | 'gen-4' | 'copper-2' | 'assembler-3' | 'claim-north' | 'contest-north' | 'retake-north' | 'hold';
export type SupportId = 'down' | 'gen-dry' | 'kit' | 'feed' | 'brownout';
export type LegacyGoalId = Exclude<GoalId, 'home-factory' | 'home-explore'>;
/** §11's row order: the goal ids a state can show, first to last. */
export const GOAL_ORDER: readonly LegacyGoalId[] = [
  'hq-fell', 'front', 'mine', 'craft', 'coal-line', 'gen-2', 'steel-line', 'copper-line', 'shot-line', 'steel-2', 'gen-3',
  'claim-east', 'contest-east', 'retake-east', 'claim-west', 'contest-west', 'retake-west', 'rail-coal',
  'gen-4', 'copper-2', 'assembler-3', 'claim-north', 'contest-north', 'retake-north', 'hold',
];

export interface GoalLine {
  id: GoalId | SupportId;
  /** What to do, one line. */
  text: string;
  /** Why it matters now, with the state's numbers. */
  why: string;
  /** The block the line is about (a claim, a street), for the map's marker; absent when it is about the HQ lot. */
  block?: number;
}
export interface Goal { goal: GoalLine; support: GoalLine | null }

// ------------------------------------------------------------------ counting what stands

const machines = (st: SimState): readonly Machine[] => st.flow?.machines ?? [];
export const generators = (st: SimState): Machine[] => machines(st).filter(m => m.kind === 'generator');
export const assemblers = (st: SimState): Machine[] => machines(st).filter(m => m.kind === 'assembler');
export const shotAssemblers = (st: SimState): Machine[] => assemblers(st).filter(m => (m.recipe ?? 'shot') === 'shot');
const PATCH_OF = { steel: P_STEEL, copper: P_COPPER, coal: P_COAL } as const;
export type PatchType = keyof typeof PATCH_OF;
/** Whether an Excavator's reach (flow.ts findRubble: one tile around its footprint) covers a tile of the HQ lot's
 *  `type` patch, dug out or not. */
function reachOnPatch(st: SimState, m: Machine, type: PatchType): boolean {
  const p = PATCH_OF[type];
  for (const q of HQ_PATCHES) {
    if (q.type !== p) continue;
    for (let ly = q.ly; ly < q.ly + q.h; ly++) for (let lx = q.lx; lx < q.lx + q.w; lx++) {
      const [tx, ty] = hqLot(st, lx, ly);
      if (tx >= m.x - 1 && tx <= m.x + m.size && ty >= m.y - 1 && ty <= m.y + m.size) return true;
    }
  }
  return false;
}
/** The Excavators placed on the HQ lot's `type` patch (their reach covers one of its tiles, dug out or not): what
 *  §11's patch rows count. A line whose first corner is dug out still stands; a district's Excavator digging the
 *  same kind of rubble elsewhere does not count here. */
export function patchExcavators(st: SimState, type: PatchType): Machine[] {
  return st.flow ? machines(st).filter(m => m.kind === 'excavator' && reachOnPatch(st, m, type)) : [];
}
/** Excavators digging `type` right now, anywhere: rubble of that type in reach, or a unit of it waiting at the output. */
export function excavatorsOn(st: SimState, type: Item): Machine[] {
  return machines(st).filter(m => m.kind === 'excavator' && (m.hold === type || findRubble(st, m)?.type === type));
}
/** Units left in the HQ lot's patch of `type` (steel, copper or coal), as `rubbleAt` sees them; 0 without a flow layer. */
export function patchLeft(st: SimState, type: 'steel' | 'copper' | 'coal'): number {
  if (!st.flow) return 0;
  const p = type === 'steel' ? P_STEEL : type === 'copper' ? P_COPPER : P_COAL;
  let left = 0;
  for (const q of HQ_PATCHES) {
    if (q.type !== p) continue;
    for (let ly = q.ly; ly < q.ly + q.h; ly++) for (let lx = q.lx; lx < q.lx + q.w; lx++) {
      const [tx, ty] = hqLot(st, lx, ly), r = rubbleAt(st, tx, ty);
      if (r) left += r.units;
    }
  }
  return left;
}
/** Coal the rail yard's heap still holds (RAIL_YARD_COAL minus what was mined there, D-P4-12); 0 without a rail yard. */
export function railCoalLeft(st: SimState): number {
  return ground(st).railYard < 0 || !st.flow ? 0 : Math.max(0, RAIL_YARD_COAL - st.flow.stats.railCoal);
}
/** Coal on hand: the Depot's store, every Generator's hopper and the pockets. */
export function coalOnHand(st: SimState): number {
  const f = st.flow;
  if (!f) return 0;
  let c = f.store.coal + (st.engineer.inv.coal ?? 0);
  for (const m of generators(st)) c += m.inv.coal ?? 0;
  return c;
}
/** Coal the grid burns a minute at the last block tick's load (4 MJ a coal, §11). */
export const coalBurnPerMin = (st: SimState): number => st.flow ? st.flow.power.load / (COAL_MJ * 1000) * 60 : 0;
const chestMags = (st: SimState): number => Math.floor(st.buffer / SHOT.count);
const n = (v: number): string => String(Math.floor(v + 1e-9));
const mins = (s: number): string => `${Math.floor(s / 60)} min`;
const cu = (c: { steel: number; copper: number }): string => costStr(c);

function coalWhy(st: SimState): string {
  const burn = coalBurnPerMin(st), have = coalOnHand(st);
  if (burn <= 1e-9) return `${n(have)} coal on hand; the grid is idle`;
  return `the grid burns ${burn.toFixed(1)} coal/min; ${n(have)} coal on hand is ${mins(have / burn * 60)}`;
}
function powerWhy(st: SimState): string {
  const p = st.flow!.power, g = generators(st).length;
  return `${g} Generator${g === 1 ? '' : 's'} give${g === 1 ? 's' : ''} ${n(p.supply)} kW; demand ${n(p.demand)} kW${p.throttle < 1 - 1e-9 ? ` — machines at ${Math.round(p.throttle * 100)} %` : ''}`;
}
function frontWhy(st: SimState): string {
  const a = ammoStatus(st);
  return `${chestMags(st)} magazines in the chest; the front drew ${a.demandMagPerMin1.toFixed(1)} a minute last minute${a.emptyHoppers ? `; ${a.emptyHoppers} empty hopper${a.emptyHoppers === 1 ? '' : 's'}` : ''}`;
}

// ------------------------------------------------------------------ the claims

function claimLine(st: SimState, dir: GoalDir, i: number): GoalLine | null {
  const b = st.blocks[i], name = blockName(st, i);
  if (b.state === CONTESTED) {
    // RI-06: the Heart's commissioning is the encounter's line (objective, active failure condition, next action), not a burn-off clock
    const H = heartAt(st, i);
    if (H && H.attempt >= 0) return { id: `contest-${dir}`, block: i, text: `Commission ${name}: ${describeHeart(st)}`,
      why: 'the Junction Heart: both feeder cabinets powered for the whole productive time; a knocked-out or unpowered feeder pauses it and interrupts it after the stall time — everything on the block is kept for the retry' };
    const left = Math.max(0, b.contestUntil - st.t);
    return { id: `contest-${dir}`, block: i,
             text: `Hold ${name}: Held in ${n(left)} s`,
             why: `the burn-off (${n(burnOffS(rotOf(st, i)))} s at its rot) ends the claim; carry its street kit out — an unkitted street fires nothing` };
  }
  if (b.state !== DARK) return null;
  const ci = claimInfo(st, b.x, b.y), cost = st.config.eco.claimCost;
  const rubble = ground(st).blocks[i].rubble;
  const what = i === ground(st).railYard ? `${n(railCoalLeft(st))} coal in its heap` : rubble ? `${rubble} rubble for the line` : 'no rubble';
  const facts = `rot ${Math.round(ci.rot * 100)} % · front ${ci.frontDelta >= 0 ? '+' : ''}${ci.frontDelta} · closes ${ci.closes} · wake bloom ≈ ${ci.wakeBloomCrawlers}`;
  if (st.fallen[i]) return { id: `retake-${dir}`, block: i, text: `Retake ${name} (${cu(cost)} of wire and frames from the chest)`, why: `it fell; its street is open and its machines stand still until it is Held again · ${facts}` };
  const afford = ci.affordable ? '' : ` — ${n(st.stock.steel)} steel / ${n(st.stock.copper)} Cu in the chest, short`;
  return { id: `claim-${dir}`, block: i, text: `Claim ${name} (${cu(cost)} of wire and frames from the chest)${afford}`, why: `${what} · ${facts}` };
}

// ------------------------------------------------------------------ the goal

function goalOf(st: SimState): GoalLine {
  const hq = hqIdx(st);
  if (st.blocks[hq].state !== HELD) return { id: 'hq-fell', block: hq, text: 'The HQ has fallen', why: 'every machine on its lot has stopped and the front has no home' };
  const f = st.flow;
  if (!f) {
    let empty = 0;
    for (const e of st.ring) if (e.hopper <= 1e-9) empty++;
    return { id: 'front', text: `Keep the front fed: ${empty} of ${st.ring.length} hoppers empty`, why: `an empty hopper lets crawlers reach the substation; ${st.config.unfedN} unfed arrivals turn it off` };
  }
  const e = st.engineer, shot = shotAssemblers(st).length, gens = generators(st).length;
  // §11 0:00 — hands: 20 steel, 10 magazines (HOUR_MINE_STEEL, HOUR_CRAFT_MAGS)
  if (shot === 0 && f.stats.handCrafted < HOUR_CRAFT_MAGS) {
    const left = HOUR_CRAFT_MAGS - f.stats.handCrafted, needSteel = SHOT.inputs.steel * left, needCu = SHOT.inputs.copper * left;
    const steel = e.inv.steel ?? 0, copper = e.inv.copper ?? 0;
    if (steel < needSteel && f.stats.handMined < HOUR_MINE_STEEL)
      return { id: 'mine', text: `Mine steel from the HQ patch by hand: ${n(f.stats.handMined)} / ${HOUR_MINE_STEEL} (pockets ${n(steel)} steel)`,
               why: `${left} hand-crafted magazines take ${needSteel} steel + ${needCu} Cu; ${frontWhy(st)}` };
    const cuNote = copper < needCu ? ` (take ${needCu - Math.floor(copper)} Cu from the chest)` : '';
    return { id: 'craft', text: `Craft Shot magazines at the workbench: ${n(f.stats.handCrafted)} / ${HOUR_CRAFT_MAGS}${cuNote}`, why: `${SHOT.seconds} s each from the pockets; ${frontWhy(st)}` };
  }
  // §11 6:00 — the coal Excavator and Generator 2, then the steel and copper Excavators
  if (patchExcavators(st, 'coal').length === 0 && patchLeft(st, 'coal') > 0)
    return { id: 'coal-line', text: `Dig the HQ coal patch: an Excavator (${cu(MACHINE_COST.excavator)}) and a belt to the Depot (${n(patchLeft(st, 'coal'))} coal left)`, why: coalWhy(st) };
  if (gens < 2) return { id: 'gen-2', text: `Build a second Generator (${cu(MACHINE_COST.generator)}) and feed it coal`, why: `${powerWhy(st)}; one ${GENERATOR_KW} kW Generator is the whole grid` };
  if (patchExcavators(st, 'steel').length === 0 && patchLeft(st, 'steel') > 0)
    return { id: 'steel-line', text: `Dig the HQ steel patch: an Excavator (${cu(MACHINE_COST.excavator)}) and a belt to the Depot`, why: `${n(st.stock.steel)} steel in the chest; a magazine takes ${SHOT.inputs.steel}, a claim ${st.config.eco.claimCost.steel}, a Generator ${MACHINE_COST.generator.steel}` };
  if (patchExcavators(st, 'copper').length === 0 && patchLeft(st, 'copper') > 0)
    return { id: 'copper-line', text: `Dig the HQ copper patch: an Excavator (${cu(MACHINE_COST.excavator)}) and a belt to the Depot`, why: `${n(st.stock.copper)} Cu in the chest; a magazine takes ${SHOT.inputs.copper}, a claim ${st.config.eco.claimCost.copper}` };
  // §11 8:00 — the Shot line
  if (shot === 0) return { id: 'shot-line', text: `Place a Shot Assembler (${cu(MACHINE_COST.assembler)}) with inserters from the steel and copper belts and one to the Depot`, why: `it makes a magazine every ${SHOT.seconds} s; ${frontWhy(st)}` };
  // §11 12:00 — the second steel Excavator; 15:00 — Generator 3, then the east claim
  if (patchExcavators(st, 'steel').length < 2 && patchLeft(st, 'steel') > 0)
    return { id: 'steel-2', text: `Add a second steel Excavator on the HQ patch (${cu(MACHINE_COST.excavator)})`, why: `one digs ${EXCAVATOR_PER_S} steel/s; ${n(st.stock.steel)} steel in the chest and the east claim takes ${st.config.eco.claimCost.steel}` };
  if (gens < 3) return { id: 'gen-3', text: `Build a third Generator (${cu(MACHINE_COST.generator)})`, why: powerWhy(st) };
  const east = hqNeighbourToward(st, 'east'), west = hqNeighbourToward(st, 'west'), north = hqNeighbourToward(st, 'north');
  if (east >= 0) { const g = claimLine(st, 'east', east); if (g) return g; }
  // §11 25:00 — the west claim (the rail yard) and its coal
  if (west >= 0) { const g = claimLine(st, 'west', west); if (g) return g; }
  if (west >= 0 && st.blocks[west].state === HELD && railCoalLeft(st) > 0 && !excavatorsOn(st, 'coal').some(m => blockOfTile(st, m.x, m.y) === west))
    return { id: 'rail-coal', block: west, text: `Dig the ${blockName(st, west)} heap: an Excavator (${cu(MACHINE_COST.excavator)}) and a belt to the Depot (${n(railCoalLeft(st))} coal left)`, why: coalWhy(st) };
  // §11 45:00 – 50:00 — Generator 4, the second copper Excavator, the third Assembler
  if (gens < HOUR_END.generators) return { id: 'gen-4', text: `Build a fourth Generator (${cu(MACHINE_COST.generator)})`, why: powerWhy(st) };
  if (patchExcavators(st, 'copper').length < 2 && patchLeft(st, 'copper') > 0)
    return { id: 'copper-2', text: `Add a second copper Excavator on the HQ patch (${cu(MACHINE_COST.excavator)})`, why: `${n(st.stock.copper)} Cu in the chest; ${frontWhy(st)}` };
  if (assemblers(st).length < HOUR_END.assemblers)
    return { id: 'assembler-3', text: `Place a third Assembler (${cu(MACHINE_COST.assembler)})`, why: `${assemblers(st).length} Assemblers stand; ${frontWhy(st)}` };
  // §11 65:00 — the north claim
  if (north >= 0) { const g = claimLine(st, 'north', north); if (g) return g; }
  return { id: 'hold', text: `Hold the ${heldCount(st)} blocks: kit and feed every street`, why: frontWhy(st) };
}

// ------------------------------------------------------------------ the support line

function supportOf(st: SimState): GoalLine | null {
  const e = st.engineer;
  if (e.down >= 0) return { id: 'down', text: `You are down — back on your feet at the HQ in ${n(Math.max(0, e.down - st.t))} s`, why: 'nothing is carried, mined or fired meanwhile' };
  const f = st.flow;
  if (!f) return null;
  const gens = generators(st);
  if (gens.length > 0 && gens.every(m => (m.inv.coal ?? 0) <= 0)) {
    const chest = f.store.coal, pockets = e.inv.coal ?? 0;
    return { id: 'gen-dry', text: chest + pockets > 0 ? `Every Generator is out of coal — feed one (E) from ${pockets > 0 ? `the pockets (${n(pockets)})` : `the chest (${n(chest)})`}` : 'Every Generator is out of coal and there is none on hand — dig the coal patch',
             why: 'a dead grid stops every Excavator, inserter and Assembler at once' };
  }
  let kit: Edge | null = null, empty: Edge | null = null;
  for (const ed of st.ring) {
    if (st.blocks[ed.a].state !== HELD) continue;
    if (ed.kit === false) { kit ??= ed; continue; }
    if (ed.hopper <= 1e-9 && edgeCap(st, ed) > 0) empty ??= ed;
  }
  if (kit) return { id: 'kit', block: kit.a, text: `Carry a kit to ${streetName(st, kit.a, kit.b)} (${n(e.inv.kit ?? 0)} in the pockets; the chest has more)`, why: 'an unkitted street has no turrets: its crawlers walk straight to the substation' };
  if (empty) return { id: 'feed', block: empty.a, text: `Feed ${streetName(st, empty.a, empty.b)}: its hopper is empty (pockets ${n(e.inv.magazine ?? 0)} magazines, chest ${chestMags(st)})`, why: `${empty.turrets ?? 0} turret${empty.turrets === 1 ? '' : 's'} on it hold ${TURRET_HOPPER} rounds each; an empty street lets crawlers through` };
  if (throttle(st) < 1 - 1e-9) return { id: 'brownout', text: `Brownout: machines at ${Math.round(throttle(st) * 100)} %`, why: `${powerWhy(st)}; another Generator or coal lifts it` };
  return null;
}

/** The current goal and its support line. Pure; cheap enough for a HUD to call once a second. */
export function currentGoal(st: SimState): Goal {
  if (isCampaign(st)) {
    const factory = st.flow?.machines.some(m => m.kind === 'excavator' || m.kind === 'assembler');
    const ex = st.campaign?.expansion;
    if (ex && ex.radio.restoredAt >= 0) {
      const linked = ex.route.every(t => machineAt(st, t % st.flow!.tw, Math.floor(t / st.flow!.tw))?.kind === 'track')
        && ex.stops.every(([x,y]) => { const m = machineAt(st,x,y); return m?.kind === 'tramstop' && powered(st,m); })
        && !!st.flow?.machines.some(m => m.kind === 'tram' && tramRoute(st,m).includes(ex.route[0]));
      if (!linked) return { goal: { id: 'home-explore', text: 'Lay the supplied tram route and power both stops', why: 'Blue survey squares mark track and stop positions. Collect any remaining kit at the station; place the tram on the completed line.' }, support: null };
    }
    if (ex && (factory || ex.station.restoredAt >= 0)) return { goal: { id: 'home-explore', block: ex.station.block, text: ex.station.restoredAt < 0 ? `Explore to ${blockName(st, ex.station.block)} and restore its tram station` : ex.radio.restoredAt < 0 ? 'Connect the tram line and restore the nearby radio tower' : 'Build factories around your connected station', why: describeSite(st, ex.station.restoredAt < 0 ? 'station' : 'radio') }, support: null };
    return { goal: { id: factory ? 'home-explore' : 'home-factory', text: factory ? 'Explore beyond Home Court through its single entrance' : 'Build your first production line in Home Court',
      why: factory ? 'Your house and factory remain here when you return.' : 'E at the house opens your supplies; B opens building. Steel and copper patches are inside the court.' }, support: null };
  }
  return { goal: goalOf(st), support: supportOf(st) };
}

// ------------------------------------------------------------------ machine state words

export type MachineState = 'running' | 'starved' | 'blocked' | 'idle' | 'off';
export interface MachineStatus { state: MachineState; reason: string }
/** §11.2: a machine's working / starved / blocked state in one word, with the reason (never colour alone). */
export function machineStatus(st: SimState, m: Machine): MachineStatus {
  const bi = blockOfTile(st, m.x, m.y), b = bi >= 0 ? st.blocks[bi] : null;
  const field = bi >= 0 && isFieldKind(m.kind) && fieldBlock(st, bi);   // RI-03: the field kit runs on the claim front
  if (!b || (!isCampaign(st) && b.state !== HELD && !field)) return { state: 'off', reason: 'block not Held' };
  if (MACHINE_KW[m.kind] > 0 && !powered(st, m)) return { state: 'off', reason: field ? 'no connected pole in reach' : 'no power' };
  switch (m.kind) {
    case 'turret': {
      if ((m.inv.rounds ?? 0) < 1) return { state: 'starved', reason: 'empty hopper' };
      return m.out > 0 || m.timer > 0 ? { state: 'running', reason: 'firing' } : { state: 'idle', reason: 'nothing in range' };
    }
    case 'generator': {
      if ((m.inv.coal ?? 0) <= 0) return { state: 'starved', reason: 'out of coal' };
      return m.busy ? { state: 'running', reason: 'burning' } : { state: 'idle', reason: 'no load' };
    }
    case 'excavator': {
      if (m.hold) return { state: 'blocked', reason: `output blocked (${m.hold})` };
      const r = findRubble(st, m);
      return r ? { state: 'running', reason: `digging ${r.type}` } : { state: 'starved', reason: 'nothing in reach' };
    }
    case 'assembler': {
      if (m.busy) return { state: 'running', reason: `making ${recipeOutput(recipeOf(m))}` };
      if (m.out >= ASM_OUTPUT_CAP) return { state: 'blocked', reason: 'output full' };
      if (!asmCanStart(m)) {
        const r = recipeOf(m), short = Object.keys(r.inputs).filter(k => (m.inv[k] ?? 0) < r.inputs[k]);
        return { state: 'starved', reason: `needs ${short.join(', ')}` };
      }
      return { state: 'idle', reason: 'ready' };
    }
    case 'inserter': {
      const [sx, sy] = inputTile(m), [dx, dy] = outputTile(m);
      const src = machineAt(st, sx, sy), dst = machineAt(st, dx, dy);
      if (!src || !dst) return { state: 'starved', reason: !src ? 'nothing behind it' : 'nothing in front' };
      if (m.phase === 1 && m.hold && m.timer <= 1e-9 && !accepts(st, dst, m.hold, 0.5)) return { state: 'blocked', reason: `${dst.kind} full` };
      if (m.phase !== 0) return { state: 'running', reason: m.hold ? `carrying ${m.hold}` : 'swinging back' };
      return { state: 'starved', reason: 'nothing to pick up' };
    }
    case 'belt': return m.items.length ? { state: 'running', reason: `${m.items.length} item${m.items.length === 1 ? '' : 's'}` } : { state: 'idle', reason: 'empty' };
    case 'lamp': case 'floodlight': return { state: 'running', reason: 'lit' };
    case 'pole': case 'bigpole': return poleGrid(st).connected.has(m.id) ? { state: 'running', reason: 'on the grid' } : { state: 'idle', reason: 'not connected' };
    case 'substation': return subPowered(st, b) ? { state: 'running', reason: 'on' } : { state: 'off', reason: 'off' };
    case 'depot': return { state: 'idle', reason: `${chestMags(st)} magazines in the line buffer` };
    // RI-05
    case 'chest': { const n = invTotal(m.inv); return n > 0 ? { state: 'idle', reason: `${n} item${n === 1 ? '' : 's'}` } : { state: 'idle', reason: 'empty' }; }
    case 'track': return { state: 'idle', reason: tramAt(st, m.x, m.y) ? 'a tram on it' : 'rail' };
    case 'tramstop': { const a = invTotal(m.cargo), q = invTotal(m.inv); return a + q > 0 ? { state: 'idle', reason: `${q} waiting, ${a} arrived` } : { state: 'idle', reason: 'empty' }; }
    case 'tram': { const path = tramRoute(st, m); return path.length < 2 ? { state: 'starved', reason: 'no route' } : m.phase === 1 ? { state: 'idle', reason: 'at a stop' } : { state: 'running', reason: `${invTotal(m.cargo)} aboard` }; }
  }
}
