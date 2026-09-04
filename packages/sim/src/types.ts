/** Plain-data types. The whole sim state is JSON-serialisable: save/load is JSON.stringify/parse. */
import type { FlowState } from './flow';
import { SHOT_MAGAZINE } from './constants';

export const DARK = 0, CONTESTED = 1, HELD = 2, INERT = 3, VOID = 4;
export type CellState = 0 | 1 | 2 | 3 | 4;
export const STATE_NAMES = ['dark', 'contested', 'held', 'inert', 'void'] as const;

export type District = 'civ' | 'res' | 'ind' | 'out';

export interface Block {
  x: number; y: number;
  state: CellState;
  d: number;            // rot density; for asleep Dark blocks this is stale (lazy growth, see catchUp)
  dmax: number; g: number; name: District; well: boolean;
  timer: number;        // next bloom tick, -1 = none
  contestUntil: number;
  upto: number;         // tick the lazy growth has been applied up to
  awake: boolean;
  subOn: boolean;       // substation running
  creep: number;        // tiles the dark has crept toward the substation while it is off
  unfed: number;        // crawler-arrivals that found an empty hopper (unfed=substation rule)
  fedTimer: number;     // consecutive seconds with every hopper non-empty
  shadeOff: number;     // tick until which an unfed shade keeps the substation dark
  unfedSince: number;   // -1 = none (diagnostic)
  starveSince: number;  // -1 = none (diagnostic)
  machines: number;     // §5: assemblers standing here (the HQ starts with the Mk1); the block has `slots` of them
  slots: number;        // D6: machine slots, one per ~600 buildable tiles, at least one (GAME-ASSUMPTION)
  area: number;         // D6: buildable tiles (the lattice lot is 576)
  base: number; gBase: number;   // the district's dmax / growth before any well influence (well death falls back to these)
  pool: number;         // §12: rubble left in the block (finite); drawn down by the yield, never refilled
  exposed: boolean;     // power: any hostile neighbour (frontage draw) — recomputed when the map is dirty
  exposedAt: number;    // tick the block last became exposed (interiorGrace)
}

/** One frontage edge: held block `a` facing hostile neighbour `b`. id = a*deg + slot of b in nb[a] (graph.ts), stable across removal and re-creation.
 *  `kit` (D5): false while the edge's turrets and lamps have not been carried out to it; an unkitted edge fires nothing. */
/** `turrets`/`fire` (M3): set by the tile layer for an edge covered by physical Gun turrets — their count and the rounds
 *  they can fire this second (Σ min(rounds, 5)); the edge's hopper is then the sum of their hoppers, fed by inserters
 *  and hands, not by the ring. Absent on a state without a flow layer. */
/** `cut`: the ring skips this edge until that tick (scenario hook: "the belt is 90 s away"; the slice's belts make it
 *  physical). */
export interface Edge { id: number; a: number; b: number; hopper: number; empty: number; turrets?: number; fire?: number; kit?: boolean; cut?: number; born?: number }   // born: the second the edge was created (a kitted edge is born fed; telemetry never reads its pip that second)

/** Crawlers from one bloom arriving at one edge over 15 s. */
export interface Engagement { id: number; cr: number; sh: number; rcr: number; rsh: number }

export interface Relight { hour: number; minutes: number; mult: number }
/** §15 brownout experiment: supply × (1 − pct/100) from hour·3600 for `minutes`. */
export interface Shortfall { pct: number; hour: number; minutes: number }

export interface EconomyConfig {
  yieldPerMin: number;                       // GAME-ASSUMPTION: rubble per Held block per minute, flat
  claimCost: { copper: number; steel: number };      // GAME-ASSUMPTION: 10 wire (= 5 Cu) + 5 frames (= 10 steel); the doc names wire and frames, no recipe
  assemblerCost: { copper: number; steel: number };  // GAME-ASSUMPTION
  startStock: { stone: number; copper: number; steel: number };  // GAME-ASSUMPTION
  // GAME-ASSUMPTION: §19's "start block's steel runs out in ~2 hours". `copper`/`copperPerMin` (rework D6): the HQ lot's
  // own copper. On the lattice the start cell happened to be residential (zoneOf: x % 3 === 0) and yielded copper at the
  // flat rubble rate from minute 0; the city's HQ is civic (stone, no sink) and without this the Mk1 eats the 40 start
  // copper in 5 min, no claim can be paid, and the HQ falls at minute 27 on every bot. The tile layer's HQ lot has a
  // copper patch (tiles.ts HQ_PATCHES, 1,200 units); the block-only economy gives the lattice's supply so the
  // calibration deltas measure the graph, not the accident. Question for the human: 1,200 (the lot) or 3,840 (the lattice)?
  startPatch: { steel: number; perMin: number; copper: number; copperPerMin: number };
  magazineCost: { steel: number; copper: number }; // doc-derived (§12): 2 steel + 1 Cu per magazine of 10 rounds; paid only for magazines actually made
  pool: { civ: number; res: number; ind: number };   // §12 rubble is finite; GAME-ASSUMPTION for the size: rubble units per block by district
}

export interface SimConfig {
  production: boolean;
  asmSchedule: [number, number][];   // [tick, assemblers running from then on]; the doc's schedule
  startAssemblers: number;           // assemblers built by hand at t = 0 (proto)
  asmRate: number;                   // magazines per minute per assembler
  asmEarlyRate: number | null;
  startAsmRate: number | null;       // proto: the first `startAssemblers` run at this rate (the "Mk1"); null = asmRate. Fixtures: null.
  hopper: number; bufferCap: number; startRounds: number;
  unfed: 'none' | 'substation' | 'creep';
  unfedN: number;
  starveQuiet: boolean;
  scatter: boolean; scatterFrac: number;
  validator: 'none' | 'no2x2' | 'maxrun2' | 'inert-dark';
  shadeThr: number; hulkThr: number; wakeCap: boolean; bloomBase: number; jitter: number; fallTiles: number;
  hulkRoll: 'hash' | 'random';
  bloomT: number; bloomDrop: number;
  relight: Relight | null;
  // ---- power model (frontsim.py `power=True`); off in the regression fixtures, on in the power fixtures ----
  power: boolean;
  supply: 'track' | 'schedule' | 'generators';   // track: supply follows unshed demand + headroom every 10 min; schedule: the §15 table; generators (M3): the tile layer's Generators
  headroom: number;                  // kW
  shortfall: Shortfall | null;
  draw: 'doc' | 'half' | 'flat';     // doc 200/40 kW, half 100/20 kW (D1), flat 120 kW
  interiorGrace: number;             // seconds a newly exposed block keeps the interior draw (0 = off)
  asmTrack: number;                  // >0: assemblers are added when demand/production exceeds this (E5 track variant)
  // ---- §5 rules the Python sim never had; off by default so the fixtures stay exact ----
  wellDeath: boolean;                // a well dies after 5 min with all four neighbours Held
  interleave: boolean;               // no two adjacent blocks bloom within 10 s of each other
  economy: boolean;
  walk: boolean;        // D5: the engineer carries every claim's kit; an edge fires nothing until kitted (off in the frozen lattice fixtures)
  eco: EconomyConfig;
}

export interface CellSpec { x: number; y: number; name: District; well: boolean; dmax: number; g: number; d0: number; base?: number; gBase?: number }
/** D6: the street graph of a map. Indexed like `MapSpec.cells`. Absent on a lattice spec (derived from w×h). */
export interface GraphSpec {
  nb: number[][];      // per block: neighbour indices, ascending
  len: number[][];     // per block, per neighbour slot: the shared street segment's length in tiles
  area: number[];      // per block: buildable tiles
  inert: number[];     // blocks that are never lots (river, plazas, parks): INERT whatever the config
  pitch: number;       // tiles per unit of the block (x,y) coordinates: 32 on the lattice, 1 for a city (x,y = centroid tile)
}
/** D6: how to rebuild the city's tile geometry (packages/sim/src/city) from the spec's seed. */
export interface CityKey { seed: number; preset: string; tw: number; th: number }
export interface Facility { name: string; x: number; y: number }
/** A survivor group (§8) on a block; `tag` is the §18 map letter (E, N, G, K, M). */
export interface Survivor { name: string; tag: string; x: number; y: number }
export interface MapSpec {
  w: number; h: number;
  start: [number, number]; target: [number, number]; wells: [number, number][];
  cells: CellSpec[];
  scatteredInert: [number, number][];
  facilities: Facility[];
  survivors: Survivor[];
  graph?: GraphSpec;
  city?: CityKey;
}

export interface HourRow {
  h: number; held: number; front: number; interior: number;
  mags: number; shells: number; meanAwakeRot: number; lost: number;
  production: number;   // mag/min at the hour mark
}

export interface LostEntry { t: number; reason: 'unfed' | 'shade' | 'starved'; x: number; y: number }

export interface SimStats {
  lost: number; retakes: number; claims: number;
  firstInterior: number; firstFall: number; firstUnfed: number;   // ticks, -1 = never
  unfedTotal: number;
  lostLog: LostEntry[];
  ringReorders: number; assemblersAdded: number;
  claimsRejected: number;
  magsMade: number;        // magazines paid for under the §12 recipe (economy on only)
  machinesLost: number;    // assemblers lost with their block (§5: a block that falls loses its machine)
  ranDry: number;          // blocks whose rubble pool reached zero
  dryLog: { t: number; x: number; y: number; district: District }[];
  // power
  firstBrownout: number;   // tick demand first exceeded supply, -1 = never
  brownoutS: number;     // D-B3-4: seconds with demand > supply (every machine ran at supply ÷ demand)
  throttleMin: number;   // the lowest supply ÷ demand seen (1 = never short)
  lostInWindow: number; lostAfterWindow: number;   // blocks lost during / after the shortfall window
  wellsDead: number;
}

/** Power-model state. D-B3-4 (§14): one pool, no shedding; while demand exceeds supply every machine runs at
 *  `throttle` = supply ÷ demand. Substations are never touched by power (they stop only under the unfed rule, §5). */
export interface PowerState {
  supply: number;        // kW the grid can give this second (track / schedule / the Generators)
  throttle: number;      // supply ÷ demand, 1 when supply covers demand
  short: boolean;        // demand > supply this second
  shortAt: number;       // tick the current shortfall began (-1 when not short)
  okAt: number;          // tick the last shortfall ended
  asmActive: number;     // block assemblers running this tick (= the count; kept for the harness)
  demandKw: number[];    // as-built demand, one sample per minute
  supplyKw: number[];    // effective supply, one sample per minute
}

export type Command =
  | { type: 'claim'; x: number; y: number }
  | { type: 'ringOrder'; ids: number[] }
  | { type: 'addAssembler' }
  | { type: 'setSpeed'; mult: number }
  // D5: the engineer (engineer.ts). Tile coordinates; `walkTo` is the block-level form the bots use.
  | { type: 'move'; x: number; y: number }            // walk here (the map view's click on a Held or street tile; the A* in walk.ts)
  | { type: 'walk'; dx: number; dy: number }          // held keys: a direction, (0,0) stops; any direction cancels a walk-here
  | { type: 'walkTo'; block: number }                 // bots and dev hooks only: walk along the streets to a block's centre
  | { type: 'sprint'; on: boolean }                   // D-B1-5: Shift
  | { type: 'dodge' }                                 // D-B1-5: Space — a short dash in the movement direction
  | { type: 'aim'; at: [number, number] | null }      // D-B1-5: the rifle in hand, mouse held toward a tile; null releases
  | { type: 'mineAt'; x: number; y: number }
  | { type: 'craft'; item: string; count?: number }
  | { type: 'place'; item: string; x: number; y: number; dir?: number }
  | { type: 'pickUp'; x: number; y: number }
  | { type: 'fire'; edge: number }                    // -1 = cease fire
  | { type: 'enterTruck' }
  | { type: 'chestTake'; item: string; n: number }
  | { type: 'chestPut'; item: string; n: number }
  // M6: the scene's remaining direct calls as commands, so a played session's log replays whole (hour.ts `replay`)
  | { type: 'feed'; x: number; y: number }          // E on a turret / Generator: magazines / coal from the pockets
  | { type: 'repair'; x: number; y: number }        // E on an eaten lamp: 1 Cu from the pockets
  | { type: 'rotate'; x: number; y: number };       // R on a machine

export type SimEvent =
  | { type: 'claim'; t: number; x: number; y: number; district: District; well: boolean; d: number;
      fBefore: number; fAfter: number; iBefore: number; iAfter: number; retake: boolean;
      cr: number; sh: number; hu: number }
  | { type: 'claim-rejected'; t: number; x: number; y: number; reason: string }
  | { type: 'held'; t: number; x: number; y: number; facility: string | null; survivor: string | null; unlocks: string[] }   // unlocks (prompt B M3): what the survivor group puts on the toolbar
  | { type: 'bloom'; t: number; x: number; y: number; cr: number; sh: number; hu: number; wake: boolean }
  | { type: 'fall'; t: number; x: number; y: number; reason: string; delay: number; starved: string }   // delay: s from first unfed arrival (-1 none); starved: district of the empty edge's dark block ('-' none)
  | { type: 'sub-off'; t: number; x: number; y: number }
  | { type: 'sub-on'; t: number; x: number; y: number }
  | { type: 'reorder'; t: number; ids: number[] }
  | { type: 'assembler'; t: number; count: number; x: number; y: number }
  | { type: 'assembler-rejected'; t: number; reason: string }
  | { type: 'machine-lost'; t: number; x: number; y: number; count: number }
  | { type: 'run-dry'; t: number; x: number; y: number; district: District }
  | { type: 'brownout'; t: number; demandKw: number; supplyKw: number }
  | { type: 'power-ok'; t: number }                                          // D-B3-4: the shortfall ended
  | { type: 'hopper-empty'; t: number; x: number; y: number; nx: number; ny: number }   // M3: an edge's hopper just ran dry (the map pip turns red on this); (nx,ny) = the dark block it faces
  | { type: 'engineer-down'; t: number; x: number; y: number }               // D5: knocked down; respawns at the HQ
  | { type: 'engineer-up'; t: number }
  | { type: 'kitted'; t: number; x: number; y: number; edges: number }       // D5: the engineer laid kits on a block's new edges
  | { type: 'truck'; t: number }                                             // D5: the truck found at the Tram depot
  | { type: 'rifle'; t: number; x: number; y: number }                       // D5: first rifle shot (Gate B: at what minute)
  | { type: 'gen-dry'; t: number; x: number; y: number }                     // M3: a Generator burned its last coal
  | { type: 'retaliate'; t: number; tx: number; ty: number; cause: 'shot' | 'path' }   // M4: a crawler turned on the engineer (D5: shot by them, or they stood in its path)
  | { type: 'lamp-eaten'; t: number; x: number; y: number; tx: number; ty: number }    // M4: a crawler put a lit lamp out on block (x,y)
  | { type: 'arrival'; t: number; x: number; y: number; n: number; of: number; shade: boolean }   // M4: the 1st and every 10th unshot arrival at a block's substation (n of the 40)
  | { type: 'well-dead'; t: number; x: number; y: number }
  | { type: 'hour'; t: number; row: HourRow };

/** D5: the engineer. One body on the tile grid; the harness moves it block to block along the streets. */
export interface Engineer {
  x: number; y: number;        // tile position
  block: number;               // the block it stands in or last stood in (-1 between blocks while walking)
  hp: number; lastHit: number; // HP and the tick it was last hurt (regen after 5 s out of contact)
  down: number;                // -1, or the tick it gets back up at the HQ
  inv: Record<string, number>; // item counts (stacks = Σ ceil(count / stack size), capped at INV_STACKS)
  reach: number;               // tiles
  truck: boolean; truckFound: boolean;
  dest: number;                // block it is walking to (-1 none)
  remaining: number;           // tiles left on that walk
  vel: [number, number];       // held-key direction (world view)
  target: [number, number] | null;   // walk-here tile (the map view's click; cancelled by any WASD input)
  firing: number;              // edge id being fired at, -1 none (the bots' block-level rifle)
  // D-B1-5 direct control: the engineer's body — stamina is a movement resource only, never hunger, cold or dark
  stamina: number;             // 0..1; sprint drains it, rest refills it, a dodge costs a fixed chunk
  sprint: boolean;             // Shift held
  dash: number;                // seconds left in the current dodge (0 = none); invulnerable to crawlers meanwhile
  dashDir: [number, number]; dashCooldown: number;
  face: [number, number];      // last movement direction (a dodge with no keys held goes this way)
  aim: [number, number] | null;   // the cursor tile while the rifle is in hand and the mouse is held; null = not firing
  shots: Record<number, number>;  // rounds that hit an engaged edge this second, resolved to kills by the block step
  iframes: number;             // seconds of this block second spent dodging (scales that second's retaliation)
  shootS: number; shootHour: number[]; shotAt: number;   // seconds in which the rifle fired (§19: ≤ 10 % of an hour)
  danger: number; dangerHour: number[]; dangerShot: number;   // seconds with a crawler on the player (§19: ≤ 5 %); dangerShot: of those, seconds the rifle fired
  barrels: 1 | 2;              // the Arsenal upgrade
  cooldown: number;            // seconds until the next round
  walked: number;              // seconds spent walking (E-walk)
  walkedHour: number[];        // per hour
  fired: number;               // rounds fired
  firstShot: number;           // tick of the first shot, -1 none (Gate B)
  hurt: number;                // HP lost in total
  downs: number;
  kills: number;               // crawlers killed by the rifle
}

export interface SimState {
  version: 1;
  seed: number;
  t: number;
  rng: number;
  speed: number;        // sim seconds per real second (0 = paused); used by advance()
  acc: number;          // fractional tick accumulator for advance()
  w: number; h: number;
  start: [number, number]; target: [number, number]; wells: [number, number][];
  facilities: Facility[];
  survivors: Survivor[];
  config: SimConfig;
  blocks: Block[];      // lattice: index = x*h + y (the Python grid order; ties in the bots break in this order); city: spec order
  /** D6 graph: neighbours in ascending index order; `deg` = max degree (edge id = a*deg + slot); `len` = street segment tiles per slot. */
  nb: number[][]; deg: number; len: number[][];
  lattice: boolean;     // true = a w×h grid with 32-tile cells; false = a generated city (blocks (x,y) are centroid tiles)
  tileScale: number;    // tiles per unit of block coordinates (32 on the lattice, 1 in a city)
  hops: number[]; hopsT: number[];   // BFS hops from the HQ / the target over every block (lattice: Manhattan)
  wellHops: number[][]; // per well: hops to every block
  city?: CityKey;
  engineer: Engineer;   // D5
  ring: Edge[];         // frontage edges in ring order
  edgeAt: number[];     // edgeAt[id] = index into ring, -1 = no such edge
  engagements: Engagement[];
  buffer: number;       // rounds in the line's buffer (magazines = rounds / 10)
  asmManual: number;    // assemblers added by command, on top of the schedule
  dirty: boolean;
  fallen: boolean[];    // blocks lost and not yet retaken; the bots retake these first
  recentRounds: number[]; recentShells: number[];   // 600-slot rings, indexed by tick % 600
  recentRoundsSum: number; recentShellsSum: number;
  totalRounds: number; totalShells: number;
  hourly: HourRow[];
  stats: SimStats;
  stock: { stone: number; copper: number; steel: number };
  patch: { steel: number; copper: number };   // steel and copper left in the start block's patch
  power: PowerState;
  asmTrack: { n: number; at: number };   // asmTrack mode: assemblers running, tick last evaluated
  wellDead: boolean[];        // per well (index into `wells`)
  wellEnclosedSince: number[]; // per well: tick all its neighbours became Held, -1 = not enclosed
  events: SimEvent[];   // appended by step(); the consumer drains them
  /** Phase 4 M2: the tile-level machines, present only once `ensureFlow` ran (the game); absent in the harness. */
  flow?: FlowState;
}

// ------------------------------------------------------------------ proto section
/** The prototype's config numbers. Every number that moved in the calibration is tagged PROTO-CALIBRATED with the
 *  target it was set to hit (CALIBRATION_REPORT.md); the rest are the GAME-ASSUMPTION values of the build report.
 *  The regression fixtures never read this block: they run with the economy off and `startAsmRate` null.
 *  D-B1-1 (2026-09-04): the start stock and rounds here stay as the calibration froze them (80 steel / 40 copper /
 *  0 stone / 200 rounds); the tile chest is constants.ts START_CHEST and docsync does not compare the two. */
// prototype-era, frozen at Gate A; excluded from docsync
export const PROTO_CALIBRATED = {
  startAssemblers: 1,          // one assembler at the start (build report, assumption 3)
  // PROTO-CALIBRATED (T2): the start assembler is a 10 mag/min "Mk1"; the 20 mag/min assembler is the purchase.
  // Lever 1. Compact's second-assembler decision moves from 86–168 min (at 20) to 54–66 min; no other measured effect.
  startAsmRate: 10 as number | null,
  // PROTO-CALIBRATED: kept at 300. Lever 2 (100) was tried and rejected: 100 rounds fill one of the start block's
  // hoppers, so two pips are red at minute 0 for two minutes, and nothing else in the session changed.
  startRounds: 200,
  // D1 (economy-fix task Step 2, item 4): the substation draw the block sim states is the doc's 100 / 20 kW, not the
  // pre-D1 "doc" 200 / 40. Power is off in the calibration, so no calibrated number moves; only the config hash does.
  draw: 'half' as SimConfig['draw'],
  eco: {
    // PROTO-CALIBRATED (T3): rubble per Held block per minute, civic : residential : industrial fixed 1 : 1 : 1.
    // Lever 3, swept 4/8/12/16/24/26/28/30/32: 32 is the lowest level at which compact and cheapest reach 180 min
    // with 0 stalls and 0 blocks lost on seeds 3/4/5 (30 leaves one stall at 175 min on cheapest seed 5).
    yieldPerMin: 32,
    claimCost: { copper: 5, steel: 10 },        // lever 4 not triggered: T3 held after lever 3
    assemblerCost: { copper: 20, steel: 40 },   // lever 5 not triggered: compact affords its second assembler at 54–66 min
    startStock: { stone: 0, copper: 40, steel: 80 },
    // PROTO-CALIBRATED (T3): perMin = 2 × yieldPerMin (the start patch scales with the residential rate);
    // steel = 120 × perMin so the patch still runs out at ~2 h (§19). Was 240 at 2/min.
    startPatch: { steel: 7680, perMin: 64, copper: 3840, copperPerMin: 32 },   // copper = the lattice HQ's residential yield, 120 min × 32
    magazineCost: { steel: SHOT_MAGAZINE.steel, copper: SHOT_MAGAZINE.copper },   // §12 recipe (constants.ts), not a lever
    // §12: rubble is finite (doc rule). GAME-ASSUMPTION for the size: 120 min × yieldPerMin, so a block lasts about as
    // long as the doc's "start block's steel runs out in ~2 hours" at the current draw; the three districts are equal.
    pool: { civ: 3840, res: 3840, ind: 3840 },
  },
};

/** Apply the proto numbers to a base config (the proto's session and the calibration harness both go through here). */
export function protoCalibrated(base: SimConfig): SimConfig {
  const p = PROTO_CALIBRATED;
  return { ...base, asmSchedule: [], startAssemblers: p.startAssemblers, startAsmRate: p.startAsmRate, startRounds: p.startRounds, draw: p.draw,
           eco: { ...base.eco, ...p.eco, claimCost: { ...p.eco.claimCost }, assemblerCost: { ...p.eco.assemblerCost },
                  startStock: { ...p.eco.startStock }, startPatch: { ...p.eco.startPatch }, magazineCost: { ...p.eco.magazineCost }, pool: { ...p.eco.pool } } };
}

/** FNV-1a (32-bit) over the config as key-sorted JSON: the "config hash" the test plan names. */
export function configHash(cfg: SimConfig): string {
  const sorted = (v: unknown): unknown => Array.isArray(v) ? v.map(sorted)
    : v && typeof v === 'object' ? Object.fromEntries(Object.keys(v as object).sort().map(k => [k, sorted((v as Record<string, unknown>)[k])])) : v;
  const s = JSON.stringify(sorted(cfg));
  let h = 0x811c9dc5;
  for (let i = 0; i < s.length; i++) { h ^= s.charCodeAt(i); h = Math.imul(h, 0x01000193) >>> 0; }
  return h.toString(16).padStart(8, '0');
}
