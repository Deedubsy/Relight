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
import { SimState, Block, HELD, CONTESTED, DARK, INERT, Command, Engineer, ProjectStage } from './types';
import { openLedger } from './ledger';
import { syncProjects, projectActivated, commission, projectOf, SUPPLY_DEPOT_PROJECT } from './project';   // RI-05: only functions cross, at call time (project.ts imports back)
import { idxOf, step, applyCommands, tileHooks, effectiveSupply, syncEdges as rebuildRing, burnOffS, isCandidate, startContested } from './sim';
import { edgeId, edgeFrom, edgeTo } from './graph';
import { Recipe, RECIPES, START_COAL, COAL_MJ, GENERATOR_KW, TURRET_HOPPER, TURRET_RANGE, TURRET_ROUNDS_PER_S, LAMP_KW, LAMP_RADIUS, POLE_REACH, FLOODLIGHT_KW, FLOODLIGHT_RANGE, FLOODLIGHT_HALF_ANGLE, BIG_POLE_REACH } from './recipes';
import { BELT_PER_S, EXCAVATOR_PER_S, INSERTER_PER_S, START_CHEST, START_TURRETS, STREETLIGHT_RADIUS } from './constants';
import {
  CELL_TILES, P_STEEL, P_COPPER, P_COAL, HQ_PATCHES, DEPOT_LOT, DEPOT_TILES, SUBSTATION_TILES,
  T_STREET, T_RUBBLE, T_INERT, T_RIVER, T_DEPOSIT, T_PATCH,
  cellTiles,
} from './tiles';
import { ground, inGround, hqLot, blockOfTile, goneOf, poolCap, substationOwner, blocksNear, inReach, cityGeomOf, segAxis, segLength, tileUnits } from './ground';
import { tickEngineerTiles, workbenchTile } from './walk';
import { blockName } from './names';   // RI-05: the supply depot's name
import { take as pocketTake, drop as pocketDrop, invStacks, invCap, handHook, hqIdx as hqIndex, upgradeEngineer, threatHooks } from './engineer';
import type { ThreatState } from './threat';
import type { HeartState } from './heart';
// RI-06: the Junction Heart (function-only, like project.ts: the modules import each other)
import { heartAt, heartCheck, heartStarted, heartTick, cabinetAt, deliverToCabinet, repairCabinet, abortHeart } from './heart';
import { segBetween, frontTiles, CitySeg } from './city';
import { type StationRules, type FreightReservation, setStationRules, transferFreight } from './freight';
import { isCampaign } from './rules';

export const TILE_TPS = 20;                    // constitution: fixed 20 ticks/s at tile level
export const TILE_DT = 1 / TILE_TPS;
/** float slack on second-counting timers (60 × 0.05 does not sum to 3 exactly) */
const EPS = 1e-9;
export type Dir = 0 | 1 | 2 | 3;               // N E S W
export const DX = [0, 1, 0, -1], DY = [-1, 0, 1, 0];
export const DIR_NAMES = ['north', 'east', 'south', 'west'];
export type Kind = 'excavator' | 'belt' | 'inserter' | 'assembler' | 'depot' | 'turret' | 'lamp' | 'pole' | 'generator' | 'floodlight' | 'bigpole' | 'substation'
  | 'chest' | 'track' | 'tramstop' | 'tram';   // RI-05: the supply chest (plan §4.2/§5.2) and the minimal transport kit (§13, the minimal part of T16)
export const KINDS: readonly Kind[] = ['excavator', 'belt', 'inserter', 'assembler', 'depot', 'turret', 'lamp', 'pole', 'generator', 'floodlight', 'bigpole', 'substation', 'chest', 'track', 'tramstop', 'tram'];
/** Prompt B M3 (run name B-M3-unlocks): the Electricians' unlocks (§8, §13). GAME-ASSUMPTION: a group's unlocks land
 *  on the toolbar when its block turns Held and stay if the block later falls (§11: "the Electricians walk into the
 *  Depot"); the other groups' unlocks are not in the slice. */
export const SURVIVOR_UNLOCKS: Record<string, readonly Kind[]> = { Electricians: ['floodlight', 'bigpole', 'substation'] };
/** RI-05 (plan §5.2, §9.1): the rail-yard restoration project's reward is the transport kit — Track, Tram stop and
 *  Tram — an operational capability, granted once when the project is restored and kept if the block is later lost
 *  (plan §5.1: "follow existing unlock persistence"). Implementation default: in this slice the project grants what
 *  §13's Tram depot / Rail crew rows give in the design; those rows stay the design. */
export const RAIL_ROUTE_KINDS: readonly Kind[] = ['track', 'tramstop', 'tram'];
export const PROJECT_UNLOCKS: Record<string, readonly Kind[]> = { 'rail-yard': RAIL_ROUTE_KINDS };
export const KIND_LABEL: Record<Kind, string> = {
  excavator: 'Excavator', belt: 'belt', inserter: 'inserter', assembler: 'Assembler', depot: 'Depot', turret: 'Gun turret', lamp: 'Lamp',
  pole: 'pole', generator: 'Generator', floodlight: 'Floodlight', bigpole: 'Big pole', substation: 'Substation',
  chest: 'Supply chest', track: 'Track', tramstop: 'Tram stop', tram: 'Tram',
};
/** RI-03 (plan §4.2): the field kit — what may stand on a Dark or Contested block directly adjacent to Held ground
 *  before it is claimed: poles, the unlocked lights, turrets, belts, inserters, (RI-05) the supply chest and the
 *  outskirts Substation. Extraction, assembly and the rail kit stay on Held ground (D-CU-1 (a), implementation
 *  default: no interior-only restriction). */
export const FIELD_KIT: readonly Kind[] = ['pole', 'bigpole', 'lamp', 'floodlight', 'turret', 'belt', 'inserter', 'chest', 'substation'];
export const isFieldKind = (k: Kind): boolean => FIELD_KIT.includes(k);
/** A block the field kit may stand on: Dark or Contested with a Held neighbour — the claim front. */
export function fieldBlock(st: SimState, bi: number): boolean {
  const b = st.blocks[bi];
  if (b.state !== DARK && b.state !== CONTESTED) return false;
  for (const n of st.nb[bi]) if (st.blocks[n].state === HELD) return true;
  return false;
}
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
/** The project whose reward unlocks a kind (RI-05), or null. */
export function unlockedByProject(kind: Kind): string | null {
  for (const [id, ks] of Object.entries(PROJECT_UNLOCKS)) if (ks.includes(kind)) return id;
  return null;
}
/** Why a kind cannot be placed yet ('' when it can): the group that unlocks it has not joined, or (RI-05) the
 *  project that grants it is not restored — owning the reward, not the facility being operational now. */
export function lockReason(st: SimState, kind: Kind): string {
  const who = unlockedBy(kind);
  if (who && !survivorJoined(st, who)) return `the ${who} unlock it — hold their block`;
  const pid = unlockedByProject(kind);
  if (pid && projectOf(st, pid)?.stage !== 'restored') return `the ${pid === 'rail-yard' ? 'rail yard restoration' : pid} unlocks it`;
  return '';
}
export function unlockedKinds(st: SimState): Kind[] { return KINDS.filter(k => !lockReason(st, k)); }
export function isKind(s: string): s is Kind { return (KINDS as readonly string[]).includes(s); }
/** Flow directions (N E S W) to the block sim's edge directions (+x −x +y −y) and back. */
export const SIM_DIR: readonly number[] = [3, 0, 2, 1], FLOW_DIR: readonly Dir[] = [1, 3, 2, 0];
export const SIM_DIR_NAMES = ['east', 'west', 'south', 'north'];
/** What a belt, an inserter, a pocket stack or a machine's hand can hold: the four rubbles, the magazine, and (RI-01,
 *  D-B2-1 (b)) the three §12 intermediates the placed Assembler makes. */
export type Item = 'steel' | 'copper' | 'stone' | 'coal' | 'magazine' | 'wire' | 'frame' | 'board';
export const ITEMS: readonly Item[] = ['steel', 'copper', 'stone', 'coal', 'magazine', 'wire', 'frame', 'board'];
export function isItem(s: string): s is Item { return (ITEMS as readonly string[]).includes(s); }
/** A zero count of every item (the stats ledgers). */
export function zeroItems(): Record<Item, number> { const o = {} as Record<Item, number>; for (const k of ITEMS) o[k] = 0; return o; }
/** Items the Depot keeps outside the block sim's stock (§14's stock is steel / copper / stone): coal and the intermediates. */
export type StoreItem = 'coal' | 'wire' | 'frame' | 'board';
export const STORE_ITEMS: readonly StoreItem[] = ['coal', 'wire', 'frame', 'board'];
export function isStoreItem(s: string): s is StoreItem { return (STORE_ITEMS as readonly string[]).includes(s); }

/** Constitution M2: belts carry 7.5 items/s (§13/§14 said 8; D-P4-6). GAME-ASSUMPTION: four items a tile, so the
 *  belt moves 1.875 tiles/s; belts are one lane, a side feed joins at the tile's start like a corner. */
export { BELT_PER_S, EXCAVATOR_PER_S, INSERTER_PER_S, TURRET_PER_TILES, START_CHEST, START_TURRETS } from './constants';
export const BELT_SPACING = 0.25, BELT_SPEED = BELT_PER_S * BELT_SPACING;
/** §13: Excavator 3×3, mines the 5×5 under and around it at 0.5/s. */
/** §13: inserter 1 item/s. GAME-ASSUMPTION: half a second each way; it waits with the item if the target is full. */
export const INSERTER_SWING = 0.5 / INSERTER_PER_S;
export const SHOT = RECIPES.find(r => r.name === 'Shot magazine')!;
/** RI-01 (D-B2-1 (b), 2026-09-05): the recipes a placed Assembler can be set to — the §12 rows whose machine is the
 *  start-unlocked Assembler: Shot magazine (the default, §11's line), Wire, Frame and Board. The other RECIPES rows
 *  (Shell: the Arsenal recipe; Concrete: the Mixer; Fuel and Polymer: the Refinery) stay data until their machine or
 *  unlock exists (RI-10 adds the Cannon / Shell chain). `MACHINE_RECIPES` is the doc's table (docsync:machines). */
export type RecipeId = 'shot' | 'wire' | 'frame' | 'board';
export const RECIPE_IDS: readonly RecipeId[] = ['shot', 'wire', 'frame', 'board'];
export function isRecipeId(s: string): s is RecipeId { return (RECIPE_IDS as readonly string[]).includes(s); }
export const ASSEMBLER_RECIPES: Readonly<Record<RecipeId, Recipe>> = {
  shot: SHOT, wire: RECIPES.find(r => r.name === 'Wire')!, frame: RECIPES.find(r => r.name === 'Frame')!, board: RECIPES.find(r => r.name === 'Board')!,
};
/** The doc's machine → recipe table: what each §13 machine makes in the build as of RI-01 (docsync:machines). */
export const MACHINE_RECIPES: readonly { machine: string; recipes: string; state: string }[] = [
  { machine: 'Assembler (placed, 3×3, 100 kW)', recipes: 'Shot magazine (default), Wire, Frame, Board — chosen per machine (T on it in the world view; the `setRecipe` command)', state: 'built (RI-01, D-B2-1 (b))' },
  { machine: 'Assembler Mk2', recipes: 'Shot magazine at 3 s', state: 'data only (D-P4-4: a §12 row, not placed)' },
  { machine: 'Assembler (Arsenal recipe)', recipes: 'Shell', state: 'data only until RI-10' },
  { machine: 'Mixer', recipes: 'Concrete', state: 'data only (no machine)' },
  { machine: 'Refinery', recipes: 'Fuel, Polymer', state: 'data only (no machine)' },
  { machine: 'Workbench (the Depot)', recipes: 'Shot magazine by hand (E, from the pockets)', state: 'built (prompt B M2)' },
];
/** The recipe a placed Assembler runs (`recipe` absent on a pre-RI-01 save = the Shot magazine). */
export function recipeOf(m: Machine): Recipe { return ASSEMBLER_RECIPES[m.recipe ?? 'shot']; }
/** The item a recipe's craft puts in the Assembler's output slot: the Shot magazine's ten rounds are one magazine. */
export function recipeOutput(r: Recipe): Item { return r.output === 'rounds' ? 'magazine' : r.output as Item; }
/** Output items per craft as the Assembler counts them (one magazine, two wire, one frame, one board). */
export function recipeYield(r: Recipe): number { return r.output === 'rounds' ? 1 : r.count; }
function recipeNeed(r: Recipe, k: string): number | undefined { return r.inputs[k]; }
/** GAME-ASSUMPTION: an assembler holds four crafts' worth of each input and five finished magazines, then stops. */
export const ASM_INPUT_MULT = 4, ASM_OUTPUT_CAP = 5;
/** GAME-ASSUMPTION: hand-mining takes one unit a second straight into the Depot; hand-crafting a magazine takes the
 *  recipe's 3 s and its 2 steel + 1 Cu from the stock (§11 hand-feeds the first turrets). */
export const HAND_MINE_PER_S = 1;
export const MACHINE_SIZE: Record<Kind, number> = { excavator: 3, belt: 1, inserter: 1, assembler: 3, depot: DEPOT_TILES, turret: 2, lamp: 1, pole: 1, generator: 2, floodlight: 2, bigpole: 2, substation: SUBSTATION_TILES, chest: 2, track: 1, tramstop: 2, tram: 1 };
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
  // RI-05 GAME-ASSUMPTIONS (§13 prices none): a supply chest 10 steel; a Track tile 1 steel like a belt tile; a Tram
  // stop 10 steel; a Tram 20 steel + 5 Cu — copper, not steel, is the hour's short item when the reward is laid (the
  // Depot holds ~18 Cu at 26:00 and none comes until the second copper Excavator at 46:00; Generator 4 at 45:00 needs 10)
  chest: { steel: 10, copper: 0 }, track: { steel: 1, copper: 0 }, tramstop: { steel: 10, copper: 0 }, tram: { steel: 20, copper: 5 },
};
/** §13 power draw (kW). A machine with a draw runs only on a powered cell (M3); belts, turrets, poles and Generators
 *  draw nothing. GAME-ASSUMPTION: a placed machine draws its rated kW whether busy or idle (§13 has no idle draw). */
export const MACHINE_KW: Record<Kind, number> = { excavator: 60, belt: 0, inserter: 10, assembler: 100, depot: 0, turret: 0, lamp: LAMP_KW, pole: 0, generator: 0, floodlight: FLOODLIGHT_KW, bigpole: 0, substation: 0, chest: 0, track: 0, tramstop: 20, tram: 0 };
/** GAME-ASSUMPTION: a Generator holds 50 coal (the §11 start's 40 fit); a turret's muzzle flash lasts half a second. */
export const GENERATOR_COAL_CAP = 50, TURRET_FLASH_S = 0.5;
/** RI-05, the minimal transport route (§13's rows): a Tram carries 200 items at 8 tiles/s between the stops on its
 *  track; a Tram stop draws 20 kW. GAME-ASSUMPTIONS: a stop's platform and its arrivals hold 200 items each (one
 *  tram load); the tram's transfer at a stop is instant and it dwells 4 s (§13's "up to 6 inserters" is the full
 *  design's loading rate; here the stop's pools are the buffer); a supply chest holds 200 items in all; the stop's
 *  footprint is 2×2 (the machine record has one square size; §13 draws 2×3) and the tram one tile (§13's 1×3 is
 *  drawn longer, not occupied). */
export const TRAM_CAP = 200, TRAM_TPS = 8, TRAM_DWELL_S = 4, STOP_CAP = 200, SUPPLY_CHEST_CAP = 200;

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
  /** Assembler (RI-01): which of ASSEMBLER_RECIPES it runs; absent = the Shot magazine. */
  recipe?: RecipeId;
  /** Turret (RI-02): seconds until it may fire again (threat.ts); in the state so a load keeps it. Absent = ready. */
  cool?: number;
  /** RI-05. Tram: its load. Tram stop: the arrivals (what a tram unloaded; inserters and hands take from here),
   *  while `inv` is the platform (what boards the next tram). Supply chest: `inv` is the contents. */
  cargo?: Record<string, number>;
  /** RI-05, Tram: heading along its route (`tramRoute` order) and the stop it last served (-1 none). */
  run?: { fwd: boolean; stop: number };
  /** EX-06A: optional selective station contracts and cargo ownership metadata. */
  freight?: StationRules;
  manifest?: FreightReservation[];
}
/** RI-05 (plan §5.1): a neighbourhood project record — see project.ts. */
export interface ProjectRecord {
  projectId: string; siteId: number; neighbourhoodBlockIds: number[];
  requirements: Partial<Record<Item, number>>;
  /** What has reached the site: for the rail yard a view of the claim's committed store while it exists and the
   *  consumed amounts after activation; for the supply depot what its chest holds, capped at the requirement. */
  deliveredItems: Partial<Record<Item, number>>;
  stage: ProjectStage;
  /** The commissioning id (`FlowState.commissionSeq`) of the Activate that started its burn-off; -1 none. */
  activationAttemptId: number;
  rewardId: string | null;
  /** Sim seconds (-1 not yet): recorded once. */
  restoredAt: number; rewardAt: number;
  /** The supply chest's machine id on the site (-1 none). */
  installId: number;
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
  /** Items with no home in the block sim's stock. §11: the start's 40 coal (the Generator is M3); RI-01: the three
   *  intermediates the placed Assembler makes (wire, frame, board) until something consumes them. */
  store: Record<StoreItem, number>;
  /** M1 (prompt B): `full` is raised when the pockets refused a mined unit (the game toasts it and clears it). */
  hand: { mine: [number, number] | null; prog: number; crafts: number; crafting: boolean; craftProg: number; full: boolean;
          /** M4 telemetry: the engineer has been out of the Depot's reach since the last chest transaction (a "trip"). */
          away: boolean };
  stats: { magsMade: number; magsDelivered: number; mined: number; handMined: number; handCrafted: number;
           /** Items that reached the Depot by machine (belts, inserters, an Excavator facing it); `putBack` is what the
            *  pockets put in the chest by hand (RI-01: the two were one count before, and a put of n counted 1). */
           delivered: Record<Item, number>; putBack: Record<Item, number>;
           /** M3: rounds the turrets fired, coal the Generators burned, magazines and coal fed by hand (`handFed` is
            *  the two together, as M3 counted it; RI-01 splits it: `handFedMags` magazines into turret hoppers and
            *  `handFedCoal` coal into Generators, both from the pockets or, for the bot's hands, from the Depot). */
           fired: number; coalBurned: number; handFed: number; handFedMags: number; handFedCoal: number;
           /** M4 telemetry (§19): trips to the chest, placements the reach refused. */
           chestTrips: number; reachRefused: number;
           /** RI-01's resource accounting (ledger.ts): units mined by type (machines and hands together) and by hand
            *  alone; the rail yard's coal units among them (D-P4-12); items a recipe made and consumed (Assemblers
            *  and the workbench); magazines inserters put in turret hoppers and coal machines put in Generators;
            *  steel and copper machine placements took from the pockets. */
           minedOf: Record<Item, number>; handMinedOf: Record<Item, number>; railCoal: number;
           made: Record<Item, number>; consumed: Record<Item, number>; turretFed: number; genFed: number;
           placed: { steel: number; copper: number };
           /** RI-05: items a tram unloaded at a stop (the route's deliveries) and items a belt committed to a claim. */
           tramMoved?: number; beltDelivered?: number };
  /** M3: commands the tile layer raises for the block map. RI-03 (plan §4.1): a pole run no longer raises a claim —
   *  power connection alone never activates a block; the list stays for saves and dev hooks. */
  pending: Command[];
  /** RI-03 (plan §4.3): claim materials delivered to a Dark block's installation (`deliverTo`), by block index —
   *  committed to that restoration stage, neither in the pockets nor spent, until `activate` consumes them once
   *  (the ledger counts them as `committed`). */
  delivered: Record<number, { steel: number; copper: number }>;
  /** RI-03: the last commissioning id — one per activation attempt, accepted or refused (`claim` and `bloom` events
   *  carry it; a refusal is an `activate-rejected` event with it). */
  commissionSeq: number;
  /** RI-05 (plan §5): the neighbourhood project records by id (project.ts). */
  projects: Record<string, ProjectRecord>;
  /** M3: the grid as of the last block tick — kW supplied, demanded, delivered; seconds demand exceeded supply. */
  /** §14 as one number: kW the Generators give, kW asked, kW carried, seconds short so far, and the D-B3-4 throttle
   *  (supply ÷ demand, 1 when covered) every drawing machine runs at this second. */
  power: { supply: number; demand: number; load: number; overS: number; throttle: number };
  /** M4: the tile threat (threat.ts): crawlers, eaten lights, hand-fired engagements. Created on first use. */
  threat?: ThreatState;
  /** RI-06: the Junction Heart candidate layer — absent unless `enableHeart` touched the state (D-RI-5). */
  heart?: HeartState;
  /** M5: streetlights the engineer repaired (global tile index) — the §13 3-in-8 broken ones, once E has been on them
   *  with copper in the pockets. Eaten lights (`threat.broken`) are repaired by leaving that list. Created on first use. */
  repaired?: number[];
  /** M5 telemetry: lights repaired by hand (both kinds). */
  repairs?: number;
  /** RI-01: the opening stock the conservation check (ledger.ts) reads from — recorded when the flow layer is created,
   *  or when a save from before RI-01 is upgraded (its `tick` says which). */
  ledger?: { tick: number; base: Record<Item, number> };
}

/** The flow layer, created on first use: the Depot goes on the start lot, the block-level Mk1 stand-in retires
 *  (§12 C9: Phase 4 places the real machine), and from here the HQ patch is dug by machines and hands, not drained. */
export function ensureFlow(st: SimState): FlowState {
  upgradeEngineer(st.engineer);   // D-B1-5: a snapshot from before the body fields
  if (st.flow) { const f = upgrade(st.flow); f.ledger ??= openLedger(st); return f; }
  const f: FlowState = {
    version: 1, tick: 0, next: 1, rev: 0, tw: ground(st).tw, machines: [], occ: {}, dug: {}, units: {},
    store: { coal: 0, wire: 0, frame: 0, board: 0 },
    hand: { mine: null, prog: 0, crafts: 0, crafting: false, craftProg: 0, full: false, away: false },
    stats: { magsMade: 0, magsDelivered: 0, mined: 0, handMined: 0, handCrafted: 0, delivered: zeroItems(), putBack: zeroItems(),
             fired: 0, coalBurned: 0, handFed: 0, handFedMags: 0, handFedCoal: 0, chestTrips: 0, reachRefused: 0,
             minedOf: zeroItems(), handMinedOf: zeroItems(), railCoal: 0, made: zeroItems(), consumed: zeroItems(), turretFed: 0, genFed: 0,
             placed: { steel: 0, copper: 0 } },
    pending: [], delivered: {}, commissionSeq: 0, projects: {},
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
  else if (!isCampaign(st)) startTurrets(st);
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
  f.ledger = openLedger(st);   // RI-01: the opening stock the conservation check counts from
  syncProjects(st);            // RI-05: the rail-yard project is discovered from the start
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
  // RI-01: the intermediates' store and the accounting counters (a pre-RI-01 save starts them at zero — its history
  // is not reconstructed, so `conservation` on such a save reports the gap as unexplained, by design)
  for (const k of STORE_ITEMS) f.store[k] ??= 0;
  for (const k of ITEMS) f.stats.delivered[k] ??= 0;
  f.stats.putBack ??= zeroItems(); f.stats.handFedMags ??= 0; f.stats.handFedCoal ??= 0;
  f.stats.minedOf ??= zeroItems(); f.stats.handMinedOf ??= zeroItems(); f.stats.railCoal ??= 0;
  f.stats.made ??= zeroItems(); f.stats.consumed ??= zeroItems(); f.stats.turretFed ??= 0; f.stats.genFed ??= 0;
  f.stats.placed ??= { steel: 0, copper: 0 };
  f.delivered ??= {}; f.commissionSeq ??= 0;   // RI-03: a pre-RI-03 save has no deliveries and no attempts
  f.projects ??= {}; f.stats.tramMoved ??= 0; f.stats.beltDelivered ??= 0;   // RI-05: a pre-RI-05 save discovers its projects on the next sync
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
 *  every machine runs at `flow.power.throttle` (see `stepFlow`). RI-03: on the claim front (`fieldBlock`) a device is
 *  powered by a connected pole in reach (`fieldPowered`), not by the block's substation, which is not yet the grid's. */
export function powered(st: SimState, m: Machine): boolean {
  if (MACHINE_KW[m.kind] === 0) return true;
  const bi = blockIdxOf(st, m);
  if (bi >= 0 && fieldBlock(st, bi)) return fieldPowered(st, m);
  return subPowered(st, blockOf(st, m));
}
/** RI-03 (plan §4.2): a field device draws real power from the physically connected upstream grid — a pole hung
 *  (through poles) from a claimed substation that is switched on stands within its own reach of the device, and the
 *  grid has supply. Never the block sim's abstract supply and never a flag: a device out of every pole's reach is off,
 *  whatever its block's neighbours have. A field device with no draw (a belt, a turret, a pole) runs where it stands. */
export function fieldPowered(st: SimState, m: Machine): boolean {
  if (st.config.power && effectiveSupply(st) <= 0) return false;
  return poleReaches(st, poleGrid(st).on, m.x, m.y, m.size);
}
/** Whether a pole in `set` (ids) stands within its reach of the rect. */
export function poleReaches(st: SimState, set: Set<number>, x: number, y: number, size: number): boolean {
  const f = st.flow!;
  for (const p of f.machines) if (isPole(p) && set.has(p.id) && distToRect(pcx(p), pcy(p), x, y, size) <= reachOf(p)) return true;
  return false;
}
/** D-B3-4: the speed every drawing machine runs at this second (supply ÷ demand, 1 when the grid is covered). */
export function throttle(st: SimState): number {
  return st.config.power && st.flow ? st.flow.power.throttle : 1;
}
/** A machine runs on a Held block while powered; RI-03: a field device (`FIELD_KIT`) also runs on the claim front
 *  (`fieldBlock`), a belt or turret where it stands, a drawing device through a connected pole in reach. Nothing here
 *  changes the block's state: a running field device never marks its block Held (plan §4.2). */
function running(st: SimState, m: Machine): boolean {
  const bi = blockIdxOf(st, m);
  if (bi < 0) return false;
  if (st.blocks[bi].state === HELD) return powered(st, m);
  return isFieldKind(m.kind) && fieldBlock(st, bi) && powered(st, m);
}
/** Exported for threat.ts (the turrets) and goal.ts (the status words). */
export const machineRunning = running;
function stopReason(st: SimState, m: Machine): string {
  const bi = blockIdxOf(st, m), field = bi >= 0 && isFieldKind(m.kind) && fieldBlock(st, bi);
  if (bi < 0 || (blockOf(st, m).state !== HELD && !field)) return ' · stopped (block not Held)';
  if (MACHINE_KW[m.kind] === 0) return '';
  if (!powered(st, m)) return field ? ' · no connected pole in reach' : ' · no power';
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
  const units = f.units[key] ?? tileUnits(G, bi);   // D-P4-2: 300 a tile; the rail yard's 700 over its heap (D-P4-12)
  if (units <= 0) return null;
  return { type: bg.rubble, units, bi, tile: t, patch: 0 };
}

/** Take one unit out of a tile (the HQ steel patch drops with it); an emptied tile is dug, and a dug district tile
 *  takes one tile's share out of the block sim's pool so `standing` and the map's pool strip agree with the tiles
 *  (D-P4-2; before RI-01 the pool dropped a unit at a time and a half-dug tile made the thinnest edge tile flicker). */
function mineUnit(st: SimState, r: TileRubble): Item {
  const f = st.flow!;
  const key = `${r.bi}:${r.tile}`;
  const left = r.units - 1;
  const b = st.blocks[r.bi];
  if (r.patch === P_STEEL) st.patch.steel = Math.max(0, st.patch.steel - 1);
  if (left <= 1e-9) {
    delete f.units[key];
    (f.dug[r.bi] ??= []).push(r.tile);
    if (r.patch === 0) { const cap = poolCap(st, b), n = ground(st).blocks[r.bi].count; b.pool = Math.max(0, b.pool - (n > 0 ? cap / n : 0)); }
  } else f.units[key] = left;
  f.stats.mined++;
  f.stats.minedOf[r.type] = (f.stats.minedOf[r.type] ?? 0) + 1;
  if (r.bi === ground(st).railYard) f.stats.railCoal++;
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
  } else if (isStoreItem(k)) f.store[k] += 1;
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
    case 'assembler': { const need = recipeNeed(recipeOf(m), k); return need !== undefined && (m.inv[k] ?? 0) < need * ASM_INPUT_MULT; }
    case 'turret': return k === 'magazine' && (m.inv.rounds ?? 0) + SHOT.count <= TURRET_HOPPER + 1e-9;
    case 'generator': return k === 'coal' && (m.inv.coal ?? 0) < GENERATOR_COAL_CAP;
    case 'chest': return invTotal(m.inv) < SUPPLY_CHEST_CAP;        // RI-05: any item, up to the cap
    case 'tramstop': return invTotal(m.inv) < STOP_CAP;             // RI-05: onto the platform, up to the cap
    default: return false;
  }
}
/** Items in a pool, in all. */
export function invTotal(inv: Record<string, number> | undefined): number { let n = 0; for (const k in inv) n += inv[k]; return n; }
/** Could this machine ever take this item (type only)? An inserter picks up by this and waits by `accepts`. */
export function wants(m: Machine, k: Item): boolean {
  switch (m.kind) {
    case 'belt': case 'depot': case 'chest': case 'tramstop': return true;
    case 'assembler': return recipeNeed(recipeOf(m), k) !== undefined;
    case 'turret': return k === 'magazine';
    case 'generator': return k === 'coal';
    default: return false;
  }
}
export function giveItem(st: SimState, m: Machine, k: Item, p = 0): boolean {
  if (!accepts(st, m, k, p)) return false;
  if (m.kind === 'belt') beltInsert(m, k, p);
  else if (m.kind === 'depot') return deliver(st, k);
  else if (m.kind === 'turret') { m.inv.rounds = (m.inv.rounds ?? 0) + SHOT.count; st.flow!.stats.turretFed++; }   // a magazine is ten rounds in the hopper
  else { m.inv[k] = (m.inv[k] ?? 0) + 1; if (m.kind === 'generator') st.flow!.stats.genFed++; }
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
        if (!n && beltDeliver(st, m, it.k)) { items.pop(); continue; }   // RI-05: into a claim's installation
        np = 1 - BELT_SPACING / 2;
      }
    } else np = Math.min(np, items[i + 1].p - BELT_SPACING);
    it.p = Math.max(it.p, np);
  }
}

/** RI-05 (plan §5.1: "the same physical delivery systems as other facilities"): a belt whose next tile is the
 *  substation of a Dark block on the claim front commits the steel or copper it carries to that claim — the same
 *  committed store `deliverTo` fills by hand (ledger `committed`), up to what the claim still needs. */
function beltDeliver(st: SimState, m: Machine, k: Item): boolean {
  if (isCampaign(st)) return false;
  if (k !== 'steel' && k !== 'copper') return false;
  const f = st.flow!, nx = m.x + DX[m.dir], ny = m.y + DY[m.dir];
  const sm = machineAt(st, nx, ny);
  const bi = sm ? (sm.kind === 'substation' ? blockIdxOf(st, sm) : -1) : substationOwner(st, nx, ny);
  if (bi < 0 || st.blocks[bi].state !== DARK || !fieldBlock(st, bi)) return false;
  const need = claimNeed(st), got = f.delivered[bi] ?? { steel: 0, copper: 0 };
  if (got[k] >= need[k]) return false;
  got[k]++; f.delivered[bi] = got; f.stats.beltDelivered = (f.stats.beltDelivered ?? 0) + 1;
  return true;
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
    } else if (src.kind === 'assembler' && src.out > 0) { const o = recipeOutput(recipeOf(src)); if (wants(dst, o)) { src.out--; k = o; } }
    else if (src.kind === 'chest' || src.kind === 'tramstop') {   // RI-05: from a chest's contents or a stop's arrivals, the first item the target could use
      const pool = src.kind === 'chest' ? src.inv : (src.cargo ??= {});
      for (const kk in pool) if (pool[kk] >= 1 && isItem(kk) && wants(dst, kk)) { pool[kk]--; if (pool[kk] <= 0) delete pool[kk]; k = kk; break; }
    }
    if (!k) return;
    m.hold = k; m.phase = 1; m.timer = INSERTER_SWING + Math.min(0, m.timer);
  }
}

/** The rubble tile an Excavator would dig next: the first in its reach (one tile around its footprint). RI-02 exports it for `machineStatus`. */
export function findRubble(st: SimState, m: Machine): TileRubble | null {
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

export function asmCanStart(m: Machine): boolean {
  const r = recipeOf(m);
  if (m.out >= ASM_OUTPUT_CAP) return false;
  for (const k in r.inputs) if ((m.inv[k] ?? 0) < r.inputs[k]) return false;
  return true;
}
function asmStart(st: SimState, m: Machine): void {
  const r = recipeOf(m), c = st.flow!.stats.consumed;
  for (const k in r.inputs) { m.inv[k] -= r.inputs[k]; if (isItem(k)) c[k] += r.inputs[k]; }
  m.busy = true;
}
function tickAssembler(st: SimState, m: Machine, dt: number): void {
  const r = recipeOf(m);
  if (!m.busy) {
    if (!asmCanStart(m)) return;
    asmStart(st, m); m.timer = 0;
  }
  m.timer += dt;
  if (m.timer < r.seconds - EPS) return;
  const n = recipeYield(r), o = recipeOutput(r);
  m.out += n; m.busy = false;
  st.flow!.stats.made[o] += n;
  if (o === 'magazine') { st.flow!.stats.magsMade += n; st.stats.magsMade += n; }
  const rem = m.timer - r.seconds;
  if (asmCanStart(m)) { asmStart(st, m); m.timer = rem; } else m.timer = 0;
}

/** RI-01: set a placed Assembler's recipe (T on it in the world view; the `setRecipe` command). Returns '' or the
 *  refusal. A craft in progress gives its inputs back; finished items of the old output go to the pockets first, and
 *  full pockets refuse the change (nothing is ever dropped). Inputs the new recipe does not take stay in the machine
 *  and come back on pick-up. */
export function setRecipe(st: SimState, tx: number, ty: number, id: RecipeId): string {
  const m = machineAt(st, tx, ty);
  if (!st.flow || !m || m.kind !== 'assembler') return 'no Assembler there';
  if ((m.recipe ?? 'shot') === id) return '';
  const old = recipeOf(m), o = recipeOutput(old);
  if (m.out > 0) {
    if (pocketTake(st.engineer, o, m.out) < m.out) return `the pockets are full (${m.out} ${o} to take out first)`;
    m.out = 0;
  }
  if (m.busy) { const c = st.flow.stats.consumed; for (const k in old.inputs) { m.inv[k] = (m.inv[k] ?? 0) + old.inputs[k]; if (isItem(k)) c[k] -= old.inputs[k]; } m.busy = false; }
  m.timer = 0; m.recipe = id;
  return '';
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
        else { h.prog -= 1; const k = mineUnit(st, r); f.stats.handMined++; f.stats.handMinedOf[k]++; }
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
        f.stats.consumed.steel += SHOT.inputs.steel; f.stats.consumed.copper += SHOT.inputs.copper;
      } else { h.crafts = 0; return; }
    }
    h.craftProg += dt;
    if (h.craftProg >= SHOT.seconds - EPS && pocketTake(e, 'magazine', 1) === 1) {
      h.crafting = false; h.crafts--; h.craftProg = 0; f.stats.handCrafted++; f.stats.made.magazine++; st.stats.magsMade++;
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
  heartTick(st, dt);   // RI-06: the Junction Heart's commissioning — a no-op on every state without the layer
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
    if (m.kind === 'tram') { tickTram(st, m, dt); continue; }   // RI-05: draws nothing and rides its track whatever the grid does; the stops need power to transfer
    if (m.kind === 'belt' || m.kind === 'depot' || m.kind === 'lamp' || m.kind === 'pole' || m.kind === 'floodlight' || m.kind === 'bigpole' || m.kind === 'substation'
      || m.kind === 'chest' || m.kind === 'track' || m.kind === 'tramstop' || !running(st, m)) continue;
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
    if (f.tick % TILE_TPS === 0) { ev0 = st.events.length; step(st); stringClaims(st, ev0); syncProjects(st); }   // RI-05: stages follow the block tick
  }
  return n;
}

// ------------------------------------------------------------------ RI-05: the tram route

const routeCache = new WeakMap<FlowState, { rev: number; n: number; routes: Map<number, number[]> }>();
/** The tram standing on a tile (a tram rides on its track and is not in `occ`). */
export function tramAt(st: SimState, tx: number, ty: number): Machine | undefined {
  const f = st.flow;
  if (!f) return undefined;
  for (const m of f.machines) if (m.kind === 'tram' && m.x === tx && m.y === ty) return m;
  return undefined;
}
function trackAt(st: SimState, tx: number, ty: number): boolean { return machineAt(st, tx, ty)?.kind === 'track'; }
/** A tram's route: the run of track its tile belongs to, as tile indices from one end to the other (the lower-index
 *  end first). Empty when it stands off the track or the track branches (a junction: the tram parks — the minimal
 *  route is one line). A closed loop runs as an out-and-back from the tram's tile. Cached per placement revision. */
export function tramRoute(st: SimState, m: Machine): number[] {
  const f = st.flow!;
  let c = routeCache.get(f);
  if (!c || c.rev !== f.rev || c.n !== f.machines.length) { c = { rev: f.rev, n: f.machines.length, routes: new Map() }; routeCache.set(f, c); }
  const t0 = m.y * f.tw + m.x, hit = c.routes.get(t0);
  if (hit) return hit;
  const tw = f.tw;
  const nbs = (t: number): number[] => {
    const x = t % tw, y = (t - x) / tw, out: number[] = [];
    for (let d = 0; d < 4; d++) if (trackAt(st, x + DX[d], y + DY[d])) out.push((y + DY[d]) * tw + x + DX[d]);
    return out;
  };
  let path: number[] = [];
  if (trackAt(st, m.x, m.y)) {
    // walk both ways from the tram's tile; a branch anywhere on the run voids the route
    const walk = (from: number, prev: number): number[] | null => {
      const out: number[] = []; let cur = from, back = prev;
      for (let guard = 0; guard < 100000; guard++) {
        const n = nbs(cur).filter(x => x !== back);
        if (n.length > 1) return null;
        if (n.length === 0 || n[0] === t0) return out;
        out.push(n[0]); back = cur; cur = n[0];
      }
      return out;
    };
    const first = nbs(t0);
    if (first.length <= 2) {
      const a = first.length > 0 ? walk(t0, first.length > 1 ? first[1] : -1) : [];
      const b = first.length > 1 ? walk(t0, first[0]) : [];
      if (a && b) { path = [...b.reverse(), t0, ...a]; if (path[0] > path[path.length - 1]) path.reverse(); }
    }
  }
  for (const t of path) c.routes.set(t, path);
  if (!path.length) c.routes.set(t0, path);
  return path;
}
/** The stop a track tile serves: a Tram stop whose footprint touches it. */
export function stopAt(st: SimState, tx: number, ty: number): Machine | undefined {
  for (let d = 0; d < 4; d++) { const n = machineAt(st, tx + DX[d], ty + DY[d]); if (n?.kind === 'tramstop') return n; }
  return undefined;
}
/** RI-05: the stop's cranes — the tram's arrival is unloaded into the stop's arrivals and its platform boards, each
 *  up to its cap, at once; nothing moves at an unpowered stop. */
function tramTransfer(st: SimState, m: Machine, stop: Machine): void {
  const stops = routeStops(st, tramRoute(st, m));
  if (stops.some(s => s.freight !== undefined) || m.manifest !== undefined) {
    transferFreight(st, m, stop, stops, running(st, stop));
    return;
  }
  if (!running(st, stop)) return;
  const f = st.flow!, cargo = (m.cargo ??= {}), arrivals = (stop.cargo ??= {});
  for (const k of Object.keys(cargo)) {
    const n = Math.min(cargo[k], STOP_CAP - invTotal(arrivals));
    if (n <= 0) break;
    arrivals[k] = (arrivals[k] ?? 0) + n; cargo[k] -= n; if (cargo[k] <= 0) delete cargo[k];
    f.stats.tramMoved = (f.stats.tramMoved ?? 0) + n;
  }
  for (const k of Object.keys(stop.inv)) {
    const n = Math.min(stop.inv[k], TRAM_CAP - invTotal(cargo));
    if (n <= 0) break;
    cargo[k] = (cargo[k] ?? 0) + n; stop.inv[k] -= n; if (stop.inv[k] <= 0) delete stop.inv[k];
  }
}
function routeStops(st: SimState, path: number[]): Machine[] {
  return [...new Set(path.map(t => stopAt(st, t % st.flow!.tw, Math.floor(t / st.flow!.tw))).filter((s): s is Machine => !!s))];
}
/** The tram shuttles end to end along its route at TRAM_TPS, dwelling TRAM_DWELL_S at each stop it passes (once a
 *  visit — a 2×2 stop touches two track tiles). `timer` is its progress to the next tile; `phase` 1 while dwelling,
 *  2 parked with no route. It moves without power (the stops need it to transfer). */
function tickTram(st: SimState, m: Machine, dt: number): void {
  const f = st.flow!, run = (m.run ??= { fwd: true, stop: -1 });
  if (m.phase === 1) { m.timer -= dt; if (m.timer > EPS) return; m.phase = 0; m.timer = 0; }
  const path = tramRoute(st, m);
  if (path.length < 2) { m.phase = 2; m.timer = 0; return; }
  // A severed line must not turn a loaded delivery into a shorter shuttle route.
  // Park until reconnected. A removed destination, however, can return to origin.
  if (m.manifest?.length) {
    const connected = new Set(routeStops(st, path).map(s => s.id));
    if (m.manifest.some(r => {
      const target = r.returning ? r.origin : r.destination;
      return !connected.has(target) && f.machines.some(s => s.id === target && s.kind === 'tramstop');
    })) { m.phase = 2; m.timer = 0; return; }
  }
  m.phase = 0;
  m.timer += dt * TRAM_TPS;
  while (m.timer >= 1 - EPS) {
    m.timer -= 1;
    const i = path.indexOf(m.y * f.tw + m.x);
    if (i < 0) { m.phase = 2; m.timer = 0; return; }
    if (run.fwd && i === path.length - 1) run.fwd = false; else if (!run.fwd && i === 0) run.fwd = true;
    const t = path[run.fwd ? i + 1 : i - 1], nx = t % f.tw, ny = (t - nx) / f.tw;
    for (let d = 0; d < 4; d++) if (DX[d] === nx - m.x && DY[d] === ny - m.y) m.dir = d as Dir;
    m.x = nx; m.y = ny;
    const stop = stopAt(st, nx, ny);
    if (!stop) { run.stop = -1; continue; }
    if (stop.id === run.stop) continue;
    run.stop = stop.id;
    tramTransfer(st, m, stop);
    m.phase = 1; m.timer = TRAM_DWELL_S;
    return;
  }
}
/** Every legacy map claim accepted since event `from` gets its pole run (an activation's run already stands: `layPoles` lays nothing). */
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
 *  (M3) turrets and Lamps may stand on the street too — §14 keeps room for "a lamp line" there),
 *  never on a tile another machine or the lot's substation holds, and only the Excavator may stand on rubble (it digs
 *  what it stands on) — except posts: a pole or a Lamp is thin enough to stand among rubble.
 *  RI-03 (plan §4.2): off Held ground only the field kit stands, and only on the claim front — a Dark or Contested
 *  block with a Held neighbour (`fieldBlock`), its lot or its street margin. The outskirts Substation (D-CU-3 (b)) is
 *  a field kind: it goes on an unheld outskirts lot next to Held ground before activation, and it is the installation
 *  the claim's materials are delivered to. (M3's "a pole on any Dark lot" is narrowed to the front: a run past the
 *  front claimed nothing and now powers nothing.) */
export function placeable(st: SimState, kind: Kind, tx: number, ty: number): string {
  tx = Math.floor(tx); ty = Math.floor(ty);
  const f = ensureFlow(st), G = ground(st), size = MACHINE_SIZE[kind];
  const lock = lockReason(st, kind);
  if (lock) return lock;
  // RI-05: a tram rides a track tile (it is not in `occ`); one a tile
  if (kind === 'tram') return !trackAt(st, tx, ty) ? 'a tram goes on a track' : tramAt(st, tx, ty) ? 'a tram is there' : '';
  // posts (poles, Lamps, Big poles) stand in rubble
  const post = kind === 'pole' || kind === 'lamp' || kind === 'bigpole';
  for (let y = ty; y < ty + size; y++) for (let x = tx; x < tx + size; x++) {
    if (!inGround(G, x, y)) return 'outside the city';
    const t = y * G.tw + x, o = G.owner[t];
    if (o === -2) return 'in the river';
    if (G.urban?.solid[t]) return 'a city structure is there';
    const margin = o === -1, bi = margin ? G.near[t] : o;
    if (bi < 0) return 'outside the city';
    const b = st.blocks[bi];
    if (b.state !== HELD) {
      if (!isFieldKind(kind)) return 'the block is not Held';
      if (!fieldBlock(st, bi)) return b.state === DARK || b.state === CONTESTED ? 'not next to Held ground' : 'the block is not Held';
    }
    if (f.occ[t] !== undefined) return 'another machine is there';
    if (cabinetAt(st, x, y) >= 0) return 'the feeder cabinet is there';   // RI-06
    if (margin && kind !== 'belt' && kind !== 'inserter' && kind !== 'turret' && kind !== 'floodlight' && kind !== 'chest' && kind !== 'track' && kind !== 'tramstop' && !post) return 'not on the street';
    if (!margin && kind === 'track') return 'track runs on streets';   // RI-05, §13: Track — streets only
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
  if (kind === 'tram') { m.cargo = {}; m.run = { fwd: true, stop: -1 }; }   // RI-05: rides the track's tile, never occupies it
  else for (let y = ty; y < ty + m.size; y++) for (let x = tx; x < tx + m.size; x++) f.occ[y * f.tw + x] = m.id;
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
  else { pocketDrop(st.engineer, 'steel', chk.cost.steel); pocketDrop(st.engineer, 'copper', chk.cost.copper); st.flow!.stats.placed.steel += chk.cost.steel; st.flow!.stats.placed.copper += chk.cost.copper; }
  return addMachine(st, kind, tx, ty, dir);   // RI-03: a pole that reaches a Dark substation claims nothing (plan §4.1)
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
  else { for (const k in m.inv) add(k, Math.floor(m.inv[k])); add(m.kind === 'assembler' ? recipeOutput(recipeOf(m)) : 'magazine', m.out); }
  if (m.cargo) for (const k in m.cargo) add(k, Math.floor(m.cargo[k]));   // RI-05: a tram's load, a stop's arrivals
  return out;
}
export interface PickUpCheck { ok: boolean; reason: string; m: Machine | null; stacks: number; items: Record<string, number> }
/** Can the machine on this tile be picked up into the pockets? All of it or none: a full pocket refuses. */
export function canPickUp(st: SimState, tx: number, ty: number): PickUpCheck {
  const m = st.flow ? (tramAt(st, tx, ty) ?? machineAt(st, tx, ty)) : null;   // RI-05: the tram before the track under it
  if (!m) return { ok: false, reason: 'nothing there', m: null, stacks: 0, items: {} };
  if (m.kind === 'depot') return { ok: false, reason: 'the Depot stays', m, stacks: 0, items: {} };
  if (m.kind === 'track' && tramAt(st, tx, ty)) return { ok: false, reason: 'a tram stands on it', m, stacks: 0, items: {} };
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
  if (m.kind === 'turret') {   // the loose rounds back to the line buffer; what a full buffer cannot take is counted as lost (RI-01 ledger)
    const loose = (m.inv.rounds ?? 0) % SHOT.count, give = Math.min(loose, Math.max(0, st.config.bufferCap - st.buffer));
    st.buffer += give; st.stats.roundsLost = (st.stats.roundsLost ?? 0) + loose - give;
  }
  if (m.kind !== 'tram') for (let y = m.y; y < m.y + m.size; y++) for (let x = m.x; x < m.x + m.size; x++) delete f.occ[y * f.tw + x];
  f.machines.splice(f.machines.indexOf(m), 1);
  f.rev++;
  return m;
}

/** Turn a placed machine a quarter clockwise. */
export function rotate(st: SimState, tx: number, ty: number): Machine | null {
  const m = machineAt(st, tx, ty);
  if (!m || !st.flow || m.kind === 'depot' || m.kind === 'pole' || m.kind === 'lamp' || m.kind === 'bigpole' || m.kind === 'substation'
    || m.kind === 'chest' || m.kind === 'track' || m.kind === 'tramstop' || m.kind === 'tram') return null;
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
  /** RI-01: of the Assemblers, those on the Shot recipe (the line's magazine capacity counts only these). */
  shotAssemblers: number;
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
  /** RI-04: the Stalker candidate layer, all zero on a state without it. */
  stalkers: number; stalkerHits: number; stalkerKills: number;
}
export function flowSummary(st: SimState): FlowSummary {
  const f = st.flow;
  const s: FlowSummary = { excavators: 0, belts: 0, inserters: 0, assemblers: 0, shotAssemblers: 0, beltItems: 0, assemblersBusy: 0, productionMagPerMin: 0,
                           magsMade: 0, magsDelivered: 0, mined: 0, coal: 0, craftsQueued: 0,
                           turrets: 0, lamps: 0, lampsLit: 0, poles: 0, polesConnected: 0, generators: 0, generatorsBurning: 0, turretRounds: 0, turretCap: 0, genCoal: 0, beltAmmo: 0,
                           supplyKw: 0, demandKw: 0, loadKw: 0, brownoutS: 0, fired: 0, coalBurned: 0, handFed: 0, throttle: 1,
                           crawlers: 0, shades: 0, onPlayer: 0, lampsEaten: 0, arrivals: 0, turretKills: 0, rifleKills: 0, stalkers: 0, stalkerHits: 0, stalkerKills: 0 };
  if (!f) return s;
  if (f.threat) {
    for (const c of f.threat.crawlers) { if (c.kind === 'shade') s.shades++; else s.crawlers++; if (c.onPlayer) s.onPlayer++; }
    s.lampsEaten = f.threat.stats.lampsEaten; s.arrivals = f.threat.stats.arrivals; s.turretKills = f.threat.stats.turretKills; s.rifleKills = f.threat.stats.rifleKills;
    if (f.threat.stalk) { s.stalkers = f.threat.stalk.stalkers.length; s.stalkerHits = f.threat.stalk.stats.hits; s.stalkerKills = f.threat.stalk.stats.kills; }
  }
  for (const m of f.machines) {
    if (m.kind === 'excavator') s.excavators++;
    else if (m.kind === 'belt') { s.belts++; s.beltItems += m.items.length; for (const it of m.items) if (it.k === 'magazine') s.beltAmmo++; }
    else if (m.kind === 'inserter') s.inserters++;
    else if (m.kind === 'assembler') { s.assemblers++; if (m.busy) s.assemblersBusy++; if ((m.recipe ?? 'shot') === 'shot') s.shotAssemblers++; }
    else if (m.kind === 'turret') { s.turrets++; s.turretRounds += m.inv.rounds ?? 0; s.turretCap += TURRET_HOPPER; }
    else if (m.kind === 'lamp' || m.kind === 'floodlight') { s.lamps++; if (running(st, m)) s.lampsLit++; }   // a Floodlight counts as a lamp here
    else if (m.kind === 'pole' || m.kind === 'bigpole') s.poles++;
    else if (m.kind === 'generator') { s.generators++; s.genCoal += m.inv.coal ?? 0; if (m.busy) s.generatorsBurning++; }
  }
  s.productionMagPerMin = s.shotAssemblers * 60 / SHOT.seconds;   // RI-01: only the Assemblers on the Shot recipe make magazines
  s.magsMade = f.stats.magsMade; s.magsDelivered = f.stats.magsDelivered; s.mined = f.stats.mined; s.coal = f.store.coal + s.genCoal; s.craftsQueued = f.hand.crafts;
  s.supplyKw = f.power.supply; s.demandKw = f.power.demand; s.loadKw = f.power.load; s.brownoutS = f.power.overS; s.throttle = st.config.power ? f.power.throttle : 1;
  s.fired = f.stats.fired; s.coalBurned = f.stats.coalBurned; s.handFed = f.stats.handFed;
  if (s.poles) s.polesConnected = poleGrid(st).connected.size;
  return s;
}

function plural(item: Item, n: number): string { return n === 1 || item === 'wire' ? item : `${item}s`; }
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
    case 'assembler': {
      const r = recipeOf(m), o = recipeOutput(r);
      const ins = Object.keys(r.inputs).map(k => `${k === 'copper' ? 'Cu' : k} ${m.inv[k] ?? 0}`).join(' · ');
      return `${r.name === 'Shot magazine' ? 'Shot' : r.name} assembler → ${DIR_NAMES[m.dir]} · ${ins} · ${m.out} ${plural(o, m.out)} out${m.busy ? ` · ${Math.round(m.timer / r.seconds * 100)} %` : ''}${on}`;
    }
    case 'depot': { const s = st.flow!.store, mid = (['wire', 'frame', 'board'] as const).filter(k => s[k] > 0).map(k => ` · ${k} ${Math.floor(s[k])}`).join(''); return `Depot · steel ${Math.floor(st.stock.steel)} · Cu ${Math.floor(st.stock.copper)} · stone ${Math.floor(st.stock.stone)} · coal ${Math.floor(s.coal)}${mid} · ${Math.floor(st.buffer / SHOT.count)} magazines in the line buffer`; }
    // RI-05
    case 'chest': { const d = depotChestOf(st, m); return `${d ? `${blockName(st, d.siteId)} supply depot (restored · hands out kits)` : 'Supply chest'} · ${poolStr(m.inv) || 'empty'} · ${invTotal(m.inv)} / ${SUPPLY_CHEST_CAP}${on}`; }
    case 'track': { const tr = tramAt(st, m.x, m.y); return `Track · streets only${tr ? ' · a tram on it' : ''}${stopAt(st, m.x, m.y) ? ' · serves a stop' : ''}`; }
    case 'tramstop': return `Tram stop · ${MACHINE_KW.tramstop} kW · platform ${poolStr(m.inv) || 'empty'} (${invTotal(m.inv)} / ${STOP_CAP}) · arrivals ${poolStr(m.cargo) || 'none'} (${invTotal(m.cargo)} / ${STOP_CAP})${on}`;
    case 'tram': { const path = tramRoute(st, m); return `Tram → ${DIR_NAMES[m.dir]} · ${poolStr(m.cargo) || 'empty'} (${invTotal(m.cargo)} / ${TRAM_CAP}) · ${path.length < 2 ? 'no route (one line of track, no junction)' : m.phase === 1 ? 'at a stop' : `route ${path.length} tiles, ${TRAM_TPS} t/s`}${on}`; }
  }
}
/** "coal 20 · magazine 10" for a pool. */
export function poolStr(inv: Record<string, number> | undefined): string {
  return Object.keys(inv ?? {}).filter(k => (inv![k] ?? 0) > 0).map(k => `${k === 'copper' ? 'Cu' : k} ${Math.floor(inv![k])}`).join(' · ');
}
/** RI-05: the restored supply-depot project this chest is the installation of, if any. */
export function depotChestOf(st: SimState, m: Machine): ProjectRecord | undefined {
  const r = projectOf(st, SUPPLY_DEPOT_PROJECT);
  return r && r.stage === 'restored' && r.installId === m.id ? r : undefined;
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
    if (e.turrets === undefined) {
      // RI-01 ledger: a stand-in edge's ring-drawn rounds go back to the line buffer when its first turret arrives (the
      // rule sim.ts syncEdges applies when a stand-in edge leaves the ring); before, they were dropped here
      const give = Math.min(e.hopper, Math.max(0, st.config.bufferCap - st.buffer));
      st.buffer += give; st.stats.roundsLost = (st.stats.roundsLost ?? 0) + e.hopper - give;
      e.turrets = 0; e.fire = 0; e.hopper = 0;
    }
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
    const bi = blockIdxOf(st, m);
    if (bi < 0) continue;
    const b = st.blocks[bi];
    if (b.state === HELD) { if (all || (b.subOn && st.t >= b.shadeOff)) kw += w; continue; }
    // RI-03 (plan §4.2): a field device on the claim front is real demand on the grid its pole hangs from
    if (isFieldKind(m.kind) && fieldBlock(st, bi) && poleReaches(st, poleGrid(st).on, m.x, m.y, m.size)) kw += w;
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
  const H = heartAt(st, bi);
  if (H && H.attempt >= 0) return Math.max(0, Math.min(1, H.progress / H.cand.productiveS));   // RI-06: productive progress, not a burn-off
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
  const H = heartAt(st, bi);   // RI-06: the Heart's block lights up along its productive progress, not a burn-off clock
  const seqT = b.state === CONTESTED ? (H && H.attempt >= 0 ? st.t - H.progress : b.contestUntil - burnOffS(b.d)) : -Infinity, ranks = b.state === CONTESTED ? lightRanks(st, bi) : null;
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
  /** RI-03: the subset hung from a substation that is switched on (its block's `subOn`, past the shade) — the live
   *  grid a field device draws from (`fieldPowered`) and an activation needs (`activationCheck`). */
  on: Set<number>;
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
 *  substation's nearest edge). The block map stays the judge of a claimed block's power: its poles are how a claim is
 *  strung and shown, and a claimed block keeps its power whether or not its poles still stand. A built Substation
 *  (prompt B M3) anchors like a pre-existing one. RI-03 (plan §4.1): a Dark substation a connected pole reaches is
 *  `reached` — the power-connection prerequisite of `activationCheck` — and claims nothing by itself; `on` is the
 *  live part of the grid, the one field devices draw from. */
export function poleGrid(st: SimState): PoleGrid {
  const f = ensureFlow(st);
  const c = gridCache.get(f);
  if (c && c.rev === f.rev && c.t === st.t) return c.grid;
  const grid: PoleGrid = { connected: new Set(), on: new Set(), links: [], reached: [] };
  const poles = f.machines.filter(isPole);
  const subs: { bi: number; x: number; y: number; size: number; claimed: boolean; on: boolean }[] = [];
  const G = ground(st);
  for (const bg of G.blocks) {
    const b = st.blocks[bg.i];
    if (b.state !== HELD && b.state !== CONTESTED && b.state !== DARK) continue;
    const sub = faceSub(st, bg.i);
    if (!sub) continue;
    subs.push({ bi: bg.i, x: sub.x, y: sub.y, size: sub.size, claimed: b.state !== DARK, on: b.state !== DARK && b.subOn && st.t >= b.shadeOff });
  }
  // two floods over the same wires: from every claimed substation (connected), and from the switched-on ones (on)
  const flood = (anchor: (s: typeof subs[number]) => boolean, into: Set<number>, draw: boolean) => {
    const queue: Machine[] = [];
    for (const m of poles) {
      for (const s of subs) {
        if (!anchor(s) || distToRect(pcx(m), pcy(m), s.x, s.y, s.size) > reachOf(m)) continue;
        into.add(m.id); queue.push(m);
        if (draw) grid.links.push({ x0: pcx(m), y0: pcy(m), x1: s.x + s.size / 2, y1: s.y + s.size / 2 });
        break;
      }
    }
    for (let q = 0; q < queue.length; q++) {
      const a = queue[q];
      for (const m of poles) {
        if (into.has(m.id) || Math.hypot(pcx(m) - pcx(a), pcy(m) - pcy(a)) > Math.max(reachOf(m), reachOf(a))) continue;
        into.add(m.id); queue.push(m);
        if (draw) grid.links.push({ x0: pcx(m), y0: pcy(m), x1: pcx(a), y1: pcy(a) });
      }
    }
    return queue;
  };
  const queue = flood(s => s.claimed, grid.connected, true);
  flood(s => s.on, grid.on, false);
  for (const s of subs) {
    if (s.claimed) continue;
    for (const m of queue) if (distToRect(pcx(m), pcy(m), s.x, s.y, s.size) <= reachOf(m)) { grid.reached.push(s.bi); break; }
  }
  gridCache.set(f, { rev: f.rev, t: st.t, grid });
  return grid;
}
/** RI-03: the pole run that would connect block `bi`'s substation to the grid — from the nearest connected point (a
 *  pole, or a claimed neighbour's substation) straight towards it, one pole every 7 tiles, each shifted to the nearest
 *  free tile — as tiles, placing nothing. Empty when the substation is already within a connected pole's reach, when
 *  the block has no substation, when nothing connected is near, or when the run finds no room (it stops short). The
 *  hour bot places the run pole by pole from this (each pole priced and within reach like any placement); `layPoles`
 *  lays it whole for a legacy map claim. */
export function polePlan(st: SimState, bi: number): [number, number][] {
  if (bi < 0) return [];
  const sub = faceSub(st, bi);
  if (!sub) return [];
  return polePlanTo(st, bi, sub.x, sub.y, sub.size);
}
/** RI-06: the same run toward any rectangle on block `bi` (a feeder cabinet's tile) — from the nearest connected pole,
 *  else a claimed neighbour's substation. */
export function polePlanTo(st: SimState, bi: number, tx: number, ty: number, size: number): [number, number][] {
  const f = ensureFlow(st);
  const tcx = tx + size / 2, tcy = ty + size / 2;
  const g = poleGrid(st);
  let from: [number, number] | null = null, best = Infinity;
  for (const m of f.machines) {
    if (!isPole(m) || !g.connected.has(m.id)) continue;
    const d = distToRect(pcx(m), pcy(m), tx, ty, size);
    if (d <= reachOf(m)) return [];   // already strung
    if (d < best) { best = d; from = [pcx(m), pcy(m)]; }
  }
  for (const ni of st.nb[bi]) {
    const nb = st.blocks[ni], ns = faceSub(st, ni);
    if (!ns || (nb.state !== HELD && nb.state !== CONTESTED)) continue;
    const cx = ns.x + ns.size / 2, cy = ns.y + ns.size / 2;
    const d = Math.hypot(cx - tcx, cy - tcy);
    if (d < best) { best = d; from = [cx, cy]; }
  }
  if (!from) return [];
  const plan: [number, number][] = [], taken = new Set<number>();
  let [cx, cy] = from;
  for (let n = 0; n < 16; n++) {
    if (distToRect(cx, cy, tx, ty, size) <= POLE_REACH) break;
    const d = Math.hypot(tcx - cx, tcy - cy), ux = (tcx - cx) / d, uy = (tcy - cy) / d;
    const stepLen = Math.min(POLE_REACH - 1, d);
    const gx = Math.floor(cx + ux * stepLen), gy = Math.floor(cy + uy * stepLen);
    let put: [number, number] | null = null;
    for (let ring = 0; ring <= 3 && !put; ring++) {
      for (let oy = -ring; oy <= ring && !put; oy++) for (let ox = -ring; ox <= ring; ox++) {
        if (Math.max(Math.abs(ox), Math.abs(oy)) !== ring) continue;
        const px = gx + ox, py = gy + oy;
        if (Math.hypot(px + 0.5 - cx, py + 0.5 - cy) > POLE_REACH) continue;
        if (!taken.has(py * f.tw + px) && placeable(st, 'pole', px, py) === '') { put = [px, py]; break; }
      }
    }
    if (!put) break;
    plan.push(put); taken.add(put[1] * f.tw + put[0]);
    cx = put[0] + 0.5; cy = put[1] + 0.5;
  }
  return plan;
}
/** A legacy map claim strings its poles whole (`polePlan`, placed free). GAME-ASSUMPTION: the claim's 10 wire + 5
 *  frames already paid for them; a run that finds no room simply stops short (the map still powers the block). An
 *  activation lays nothing here: its run already stands, pole by pole from the pockets. */
export function layPoles(st: SimState, x: number, y: number): number {
  const bi = idxOf(st, x, y);
  if (bi < 0) return 0;
  const b = st.blocks[bi];
  if (b.state !== HELD && b.state !== CONTESTED) return 0;
  const plan = polePlan(st, bi);
  for (const [px, py] of plan) addMachine(st, 'pole', px, py, 0);
  return plan.length;
}

// ------------------------------------------------------------------ RI-03: physical commissioning (plan §4.1)

/** What a claim takes (§5's 10 wire + 5 frames as rubble: the block sim's `eco.claimCost`); nothing without the economy. */
export function claimNeed(st: SimState): { steel: number; copper: number } {
  return st.config.economy ? { steel: st.config.eco.claimCost.steel, copper: st.config.eco.claimCost.copper } : { steel: 0, copper: 0 };
}
/** What has been delivered to a block's installation so far (committed there until it activates). */
export function deliveredTo(st: SimState, bi: number): { steel: number; copper: number } {
  const d = st.flow?.delivered[bi];
  return d ? { ...d } : { steel: 0, copper: 0 };
}
export interface DeliverCheck { ok: boolean; reason: string; moved: number }
/** Claim materials go from the pockets into a Dark block's installation — its substation, within reach — up to what
 *  the claim still needs. They sit committed there (ledger `committed`), neither in the pockets nor spent, until
 *  `activate` consumes them; a legitimate inventory interaction, never a charge from the map or the Depot. */
export function deliverTo(st: SimState, bx: number, by: number, item: string, n: number, cabinet = -1): DeliverCheck {
  if (isCampaign(st)) return { ok: false, moved: 0, reason: 'station restoration is not available in this opening preview' };
  if (cabinet >= 0) return deliverToCabinet(st, cabinet, item, n);   // RI-06: a feeder cabinet's materials (heart.ts)
  const f = ensureFlow(st), bi = idxOf(st, bx, by);
  if (bi < 0) return { ok: false, reason: 'out of bounds', moved: 0 };
  if (item !== 'steel' && item !== 'copper') return { ok: false, reason: 'a claim takes steel and copper', moved: 0 };
  const b = st.blocks[bi];
  if (b.state !== DARK) return { ok: false, reason: b.state === CONTESTED ? 'already commissioning' : b.state === HELD ? 'already Held' : 'not a Dark block', moved: 0 };
  if (heartAt(st, bi)?.charged) return { ok: false, reason: 'the installation keeps its materials from the last attempt', moved: 0 };   // RI-06 (plan §9.2 default 10): charged once
  const sub = faceSub(st, bi);
  if (!sub) return { ok: false, reason: 'no substation — the outskirts need a Substation first', moved: 0 };
  if (!inReach(st, sub.x, sub.y, sub.size)) return { ok: false, reason: 'walk closer to the substation', moved: 0 };
  const need = claimNeed(st), got = f.delivered[bi] ?? { steel: 0, copper: 0 };
  const room = need[item] - got[item];
  if (room <= 0) return { ok: false, reason: `it has its ${need[item]} ${item === 'copper' ? 'Cu' : 'steel'}`, moved: 0 };
  const moved = pocketDrop(st.engineer, item, Math.min(Math.floor(n), room));
  if (moved <= 0) return { ok: false, reason: `no ${item} in the pockets`, moved: 0 };
  got[item] += moved; f.delivered[bi] = got;
  syncProjects(st);
  return { ok: true, reason: '', moved };
}
export interface ActivateCheck { ok: boolean; reason: string; need: { steel: number; copper: number }; have: { steel: number; copper: number } }
/** Why Activate is unavailable on block (bx, by), in the order the installation UI names them: the block's state, an
 *  adjacent Held block (the implementation default — a special case must be explicit here), the installation, the
 *  pole run, the grid, the materials, reach, the engineer. `ok` when it may activate. Power connection alone is never
 *  enough: nothing here fires — the block stays Dark until the explicit Activate. */
export function activationCheck(st: SimState, bx: number, by: number, hands = true): ActivateCheck {
  const need = claimNeed(st);
  const no = (reason: string, have = { steel: 0, copper: 0 }): ActivateCheck => ({ ok: false, reason, need, have });
  if (isCampaign(st)) return no('station restoration is not available in this opening preview');
  const bi = idxOf(st, bx, by);
  if (bi < 0) return no('out of bounds');
  const b = st.blocks[bi];
  if (b.state === CONTESTED) return no('already commissioning');
  if (b.state === HELD) return no('already Held');
  if (b.state !== DARK) return no('not a Dark block');
  if (!isCandidate(st, bi)) return no('no Held block adjacent');
  const sub = faceSub(st, bi);
  if (!sub) return no('no substation — the outskirts need a Substation first');
  const have = deliveredTo(st, bi), g = poleGrid(st);
  if (!g.reached.includes(bi)) return no('no pole run reaches its substation', have);
  if (!poleReaches(st, g.on, sub.x, sub.y, sub.size)) return no('its pole run hangs from a substation that is off', have);
  if (st.config.power && effectiveSupply(st) <= 0) return no('the grid has no supply — no Generator burning', have);
  // RI-06: the Heart's installation is charged on the first Start only; its feeder cabinets are prerequisites too (heart.ts)
  const H = heartAt(st, bi), got = H?.charged ? { ...H.charge } : have;
  if (!H?.charged && (have.steel < need.steel || have.copper < need.copper)) {
    const s = need.steel - have.steel, c = need.copper - have.copper;
    return no(`needs ${[s > 0 ? `${s} more steel` : '', c > 0 ? `${c} more Cu` : ''].filter(Boolean).join(', ')} delivered`, have);
  }
  if (H) { const hc = heartCheck(st); if (!hc.ok) return no(hc.reason, got); }
  if (hands && !inReach(st, sub.x, sub.y, sub.size)) return no('walk closer to the substation', got);   // RI-05: `hands` false asks about the site alone (a project's "ready")
  if (hands && st.engineer.down >= 0) return no('the engineer is down', got);
  return { ok: true, reason: '', need, have: got };
}
/** The explicit Activate (plan §4.1): one commissioning id per attempt. A refusal is an `activate-rejected` event
 *  naming the missing prerequisite and charges nothing. Success consumes the delivered materials once — into the block
 *  sim's spent counters, never out of the chest (plan §4.3: never both the Depot and the installation) — and starts
 *  Contested through the one shared path, whose wake bloom is this attempt's configured response and carries the same
 *  id. Idempotent: a repeated or replayed Activate on a block already commissioning is refused. */
export function activate(st: SimState, bx: number, by: number): ActivateCheck {
  const f = ensureFlow(st), id = ++f.commissionSeq;
  const chk = activationCheck(st, bx, by);
  if (!chk.ok) { st.events.push({ type: 'activate-rejected', t: st.t, x: bx, y: by, id, reason: chk.reason }); return chk; }
  const bi = idxOf(st, bx, by), got = f.delivered[bi] ?? { steel: 0, copper: 0 }, H = heartAt(st, bi);
  // RI-06 (plan §9.2 default 10): the Heart's installation is charged on the first Start only — a retry consumes nothing
  const paid = H?.charged ? { ...H.charge } : got;
  if (!H?.charged) { st.stats.spentSteel = (st.stats.spentSteel ?? 0) + got.steel; st.stats.spentCopper = (st.stats.spentCopper ?? 0) + got.copper; }
  if (H && !H.charged) { H.charged = true; H.charge = { steel: got.steel, copper: got.copper }; }
  projectActivated(st, bi, id, paid);   // RI-05: the site's project records the attempt and what it consumed
  delete f.delivered[bi];
  // RI-06: the Heart's commissioning wakes no bloom (its packets are its reinforcements) and has no burn-off — heart.ts ends it
  startContested(st, bi, 'activate', id, H ? { bloom: false, until: Number.MAX_SAFE_INTEGER } : undefined);
  if (H) heartStarted(st, id);
  syncProjects(st);
  return chk;
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
    if (mags > 0) { m.inv.rounds = (m.inv.rounds ?? 0) + mags * SHOT.count; pocketDrop(st.engineer, 'magazine', mags); f.stats.handFed += mags; f.stats.handFedMags += mags; }
    return { kind: 'turret', moved: mags, reason: mags ? '' : room <= 0 ? 'the hopper is full' : 'no magazines in the pockets (take them from the Depot chest with I, or craft at the workbench with E)' };
  }
  if (m.kind === 'generator') {
    const n = Math.max(0, Math.min(GENERATOR_COAL_CAP - (m.inv.coal ?? 0), Math.floor(inv.coal ?? 0)));
    if (n > 0) { m.inv.coal = (m.inv.coal ?? 0) + n; pocketDrop(st.engineer, 'coal', n); f.stats.handFed += n; f.stats.handFedCoal += n; }
    return { kind: 'generator', moved: n, reason: n ? '' : (m.inv.coal ?? 0) >= GENERATOR_COAL_CAP ? 'the Generator is full' : 'no coal in the pockets (take it from the Depot chest with I, or dig the coal patch)' };
  }
  return null;
}
/** The bot's feed: from the Depot's magazines (the line buffer) and the Depot's coal, filling the machine. */
function depotFeed(st: SimState, m: Machine): number {
  const f = st.flow!;
  if (m.kind === 'turret') {
    const mags = Math.max(0, Math.min(Math.floor((TURRET_HOPPER - (m.inv.rounds ?? 0)) / SHOT.count), Math.floor(st.buffer / SHOT.count)));
    if (mags > 0) { m.inv.rounds = (m.inv.rounds ?? 0) + mags * SHOT.count; st.buffer -= mags * SHOT.count; f.stats.handFed += mags; f.stats.handFedMags += mags; }
    return mags;
  }
  const n = Math.max(0, Math.min(GENERATOR_COAL_CAP - (m.inv.coal ?? 0), Math.floor(f.store.coal)));
  if (n > 0) { m.inv.coal = (m.inv.coal ?? 0) + n; f.store.coal -= n; f.stats.handFed += n; f.stats.handFedCoal += n; }
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

export type ChestItem = Item | 'kit';
export const CHEST_ITEMS: readonly ChestItem[] = ['steel', 'copper', 'stone', 'coal', 'magazine', 'wire', 'frame', 'board', 'kit'];
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
  if (isStoreItem(item)) return Math.floor(st.flow?.store[item] ?? 0);
  if (item === 'kit') return Infinity;
  return Math.floor(st.stock[item]);
}
/** GAME-ASSUMPTION (M4 telemetry): a trip to the chest is the first transaction after being out of the Depot's reach. */
function chestTrip(f: FlowState): void { if (f.hand.away) { f.stats.chestTrips++; f.hand.away = false; } }
/** RI-05: the supply chest or tram stop at a tile, for the hand: the machine and its pool, or the refusal. A stop's
 *  arrivals are taken from first, then its platform; the hand puts onto its platform. */
function handPool(st: SimState, at: [number, number]): { m: Machine; take: Record<string, number>[]; put: Record<string, number>; cap: number } | string {
  const m = machineAt(st, at[0], at[1]);
  if (!m || (m.kind !== 'chest' && m.kind !== 'tramstop')) return 'no chest or stop there';
  if (!inReach(st, m.x, m.y, m.size)) return `walk closer to the ${KIND_LABEL[m.kind]}`;
  return m.kind === 'chest' ? { m, take: [m.inv], put: m.inv, cap: SUPPLY_CHEST_CAP } : { m, take: [(m.cargo ??= {}), m.inv], put: m.inv, cap: STOP_CAP };
}
/** Move up to `n` of an item from the chest to the pockets (as many as fit). RI-05: `at` names a supply chest or a
 *  tram stop instead of the Depot; kits come from the Depot and from a restored supply depot (its reward). */
export function chestTake(st: SimState, item: ChestItem, n: number, at?: [number, number]): { moved: number; reason: string } {
  const f = ensureFlow(st);
  if (at) {
    const p = handPool(st, at);
    if (typeof p === 'string') return { moved: 0, reason: p };
    if (item === 'kit') {
      if (!depotChestOf(st, p.m)) return { moved: 0, reason: 'kits come from the Depot or a restored supply depot' };
      const got = pocketTake(st.engineer, 'kit', Math.max(0, n));
      return got > 0 ? { moved: got, reason: '' } : { moved: 0, reason: 'the pockets are full' };
    }
    let moved = 0;
    for (const pool of p.take) {
      const have = Math.floor(pool[item] ?? 0), want = Math.max(0, Math.min(n - moved, have));
      if (want <= 0) continue;
      const got = pocketTake(st.engineer, item, want);
      if (got <= 0) break;
      pool[item] -= got; if (pool[item] <= 0) delete pool[item]; moved += got;
    }
    if (moved <= 0) return { moved: 0, reason: p.take.some(pool => (pool[item] ?? 0) >= 1) ? 'the pockets are full' : `no ${item === 'magazine' ? 'magazines' : item} there` };
    syncProjects(st);
    return { moved, reason: '' };
  }
  if (!nearDepot(st)) return { moved: 0, reason: 'walk closer to the Depot' };
  const have = chestCount(st, item), want = Math.max(0, Math.min(n, have));
  if (want <= 0) return { moved: 0, reason: `no ${item === 'magazine' ? 'magazines' : item} in the Depot` };
  const got = pocketTake(st.engineer, item, want);
  if (got <= 0) return { moved: 0, reason: 'the pockets are full' };
  chestTrip(f);
  if (item === 'magazine') st.buffer -= got * SHOT.count;
  else if (isStoreItem(item)) f.store[item] -= got;
  else if (item !== 'kit') st.stock[item] -= got;
  return { moved: got, reason: '' };
}
/** Move up to `n` of an item from the pockets to the chest. A full line buffer refuses magazines. RI-05: `at` names
 *  a supply chest (its contents) or a tram stop (its platform) instead; the cap refuses the rest. */
export function chestPut(st: SimState, item: ChestItem, n: number, at?: [number, number]): { moved: number; reason: string } {
  const f = ensureFlow(st);
  if (at) {
    const p = handPool(st, at);
    if (typeof p === 'string') return { moved: 0, reason: p };
    if (item === 'kit') return { moved: 0, reason: 'kits stay in the pockets' };
    const have = st.engineer.inv[item] ?? 0, want = Math.max(0, Math.min(n, have, p.cap - invTotal(p.put)));
    if (want <= 0) return { moved: 0, reason: have <= 0 ? `no ${item} in the pockets` : `the ${KIND_LABEL[p.m.kind]} is full` };
    const put = pocketDrop(st.engineer, item, want);
    p.put[item] = (p.put[item] ?? 0) + put;
    syncProjects(st);
    return { moved: put, reason: '' };
  }
  if (!nearDepot(st)) return { moved: 0, reason: 'walk closer to the Depot' };
  const have = st.engineer.inv[item] ?? 0;
  let want = Math.max(0, Math.min(n, have));
  if (item === 'magazine') want = Math.min(want, Math.floor((st.config.bufferCap - st.buffer) / SHOT.count));
  if (want <= 0) return { moved: 0, reason: have <= 0 ? `no ${item} in the pockets` : 'the line buffer is full' };
  const put = pocketDrop(st.engineer, item, want);
  chestTrip(f);
  if (item === 'magazine') { st.buffer += put * SHOT.count; f.stats.magsDelivered += put; }
  else if (isStoreItem(item)) f.store[item] += put;
  else if (item !== 'kit') st.stock[item] += put;
  if (item !== 'kit') f.stats.putBack[item] += put;   // RI-01: by hand, apart from `delivered` (machines), and all of it
  return { moved: put, reason: '' };
}

// the engineer's hand commands (types.ts) land here when the flow layer is loaded
handHook.current = (st, c) => {
  switch (c.type) {
    case 'mineAt': setHandMine(st, [c.x, c.y]); break;
    case 'craft': queueCraft(st, c.count ?? 1); break;
    case 'chestTake': if (isChestItem(c.item)) chestTake(st, c.item, c.n, c.x !== undefined && c.y !== undefined ? [c.x, c.y] : undefined); break;
    case 'chestPut': if (isChestItem(c.item)) chestPut(st, c.item, c.n, c.x !== undefined && c.y !== undefined ? [c.x, c.y] : undefined); break;
    // M2: the command form of the scene's placement — within reach (8 tiles) or nothing, never moved
    case 'place': if (isKind(c.item) && inReach(st, c.x, c.y, MACHINE_SIZE[c.item])) place(st, c.item, c.x, c.y, ((c.dir ?? 0) % 4) as Dir); break;
    case 'pickUp': { const m = machineAt(st, c.x, c.y); if (m && inReach(st, m.x, m.y, m.size)) remove(st, c.x, c.y); break; }
    // M6: the scene's E / R as commands (the scene checks reach before it calls; the command form checks it here)
    case 'feed': { const m = machineAt(st, c.x, c.y); if (m && inReach(st, m.x, m.y, m.size)) handFeed(st, c.x, c.y); break; }
    case 'repair': if (inReach(st, c.x, c.y, 1)) repairLight(st, c.x, c.y); break;   // a street light is a tile, not a machine
    case 'rotate': { const m = machineAt(st, c.x, c.y); if (m && inReach(st, m.x, m.y, m.size)) rotate(st, c.x, c.y); break; }
    // RI-01: the scene's T on an Assembler as a command (within reach, a known recipe)
    case 'setRecipe': { const m = machineAt(st, c.x, c.y); if (m && isRecipeId(c.recipe) && inReach(st, m.x, m.y, m.size)) setRecipe(st, c.x, c.y, c.recipe); break; }
    case 'setStationRules': setStationRules(st, c.x, c.y, c.rules); break;
    // RI-03: physical commissioning — materials into the installation and the explicit Activate (both check reach of it)
    case 'deliver': deliverTo(st, c.bx, c.by, c.item, c.n, c.cabinet ?? -1); break;
    case 'activate': activate(st, c.bx, c.by); break;
    case 'commission': commission(st, c.id); break;   // RI-05: the supply depot's commissioning (checks reach of its chest)
    case 'repairCabinet': repairCabinet(st, c.cabinet); break;   // RI-06: a knocked-out feeder cabinet (reach, REPAIR_COPPER)
    case 'abort': abortHeart(st); break;   // RI-06: the explicit abort of the Heart's commissioning (plan §9.2 default 9)
    default: break;
  }
  syncProjects(st);   // RI-05: a placement, pick-up or transfer may move a project's stage
};

// ------------------------------------------------------------------ world-view drawing (§18, docsync)

/** §18 legend for `renderLot`. */
export const LOT_LEGEND =
  '`:` street, `.` ground, `r` rubble, `#` steel patch, `&` copper patch, `*` coal patch, `~` inert/river; ' +
  '`L` streetlight lit, `l` streetlight dark, `b` streetlight broken; `T` Gun turret (hopper > 0), `t` turret with an empty hopper; ' +
  '`G` Generator burning, `g` Generator dry; `S` substation powered, `s` unpowered; `D` Depot; `X` Excavator running, `x` stopped; ' +
  '`A` Shot assembler crafting, `a` idle; `^ > v <` belt by direction, `I` inserter; `@` Lamp lit, `o` Lamp dark; `P` pole; ' +
  '`c` supply chest, `=` track, `H` tram stop, `M` tram (RI-05). ' +
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
      case 'chest': ch = 'c'; break;
      case 'track': ch = '='; break;
      case 'tramstop': ch = 'H'; break;
      case 'tram': ch = 'M'; break;
    }
    for (let dy = 0; dy < m.size; dy++) for (let dx = 0; dx < m.size; dx++) put(m.x + dx - ox, m.y + dy - oy, ch);
  }
  const head = ' col: ' + Array.from({ length: CELL_TILES }, (_, i) => String(i % 10)).join(' ');
  return [head, ...rows.map((r, ty) => `${String(ty).padStart(4)}  ${r.join(' ')}`)].join('\n');
}
