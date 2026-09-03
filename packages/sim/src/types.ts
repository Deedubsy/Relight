import type { FlowState } from './flow';
/** Plain-data types. The whole sim state is JSON-serialisable: save/load is JSON.stringify/parse. */

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
  machine: boolean;     // §5: one machine slot per Held block; true = an assembler stands here (the HQ starts with the Mk1)
  pool: number;         // §12: rubble left in the block (finite); drawn down by the yield, never refilled
  exposed: boolean;     // power: any hostile neighbour (frontage draw) — recomputed when the map is dirty
  exposedAt: number;    // tick the block last became exposed (interiorGrace)
  shed: boolean;        // power: substation turned off by the brownout shedder
}

/** One frontage edge: held block `a` facing hostile neighbour `b`. id = a*4 + dir, stable across removal and re-creation. */
/** `turrets`/`fire` (M3): set by the tile layer for an edge covered by physical Gun turrets — their count and the rounds
 *  they can fire this second (Σ min(rounds, 5)); the edge's hopper is then the sum of their hoppers, fed by inserters
 *  and hands, not by the ring. Absent on a state without a flow layer. */
export interface Edge { id: number; a: number; b: number; hopper: number; empty: number; turrets?: number; fire?: number }

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
  startPatch: { steel: number; perMin: number };  // GAME-ASSUMPTION: §19's "start block's steel runs out in ~2 hours"
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
  shed: 'substations' | 'machines-first';
  interiorGrace: number;             // seconds a newly exposed block keeps the interior draw (0 = off)
  asmTrack: number;                  // >0: assemblers are added when demand/production exceeds this (E5 track variant)
  // ---- §5 rules the Python sim never had; off by default so the fixtures stay exact ----
  wellDeath: boolean;                // a well dies after 5 min with all four neighbours Held
  interleave: boolean;               // no two adjacent blocks bloom within 10 s of each other
  economy: boolean;
  eco: EconomyConfig;
}

export interface CellSpec { x: number; y: number; name: District; well: boolean; dmax: number; g: number; d0: number }
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
}

export interface HourRow {
  h: number; held: number; front: number; interior: number;
  mags: number; shells: number; meanAwakeRot: number; lost: number;
  production: number;   // mag/min at the hour mark
}

export interface LostEntry { t: number; reason: 'unfed' | 'shade' | 'starved' | 'brownout'; x: number; y: number }

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
  shedEvents: number;
  shedLog: number[];
  lostInWindow: number; lostAfterWindow: number;   // blocks lost during / after the shortfall window
  wellsDead: number;
}

/** Power-model state (frontsim.py). `shedStack` holds block indices; -1 stands for one shed assembler. */
export interface PowerState {
  supply: number;        // kW available (track mode)
  overTimer: number;     // consecutive seconds of demand > supply
  shedStack: number[];
  lastShed: number;
  asmShed: number;       // assemblers turned off by the shedder
  asmActive: number;     // assemblers running this tick
  demandKw: number[];    // unshed demand, one sample per minute
  supplyKw: number[];    // effective supply, one sample per minute
}

export type Command =
  | { type: 'claim'; x: number; y: number }
  | { type: 'ringOrder'; ids: number[] }
  | { type: 'addAssembler' }
  | { type: 'setSpeed'; mult: number };

export type SimEvent =
  | { type: 'claim'; t: number; x: number; y: number; district: District; well: boolean; d: number;
      fBefore: number; fAfter: number; iBefore: number; iAfter: number; retake: boolean;
      cr: number; sh: number; hu: number }
  | { type: 'claim-rejected'; t: number; x: number; y: number; reason: string }
  | { type: 'held'; t: number; x: number; y: number; facility: string | null; survivor: string | null }
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
  | { type: 'shed'; t: number; x: number; y: number; machine?: string }      // x = y = -1: an assembler line; machine (M3): a tile machine's kind
  | { type: 'restore'; t: number; x: number; y: number; machine?: string }
  | { type: 'hopper-empty'; t: number; x: number; y: number; dir: number }   // M3: an edge's hopper just ran dry (the map pip turns red on this)
  | { type: 'gen-dry'; t: number; x: number; y: number }                     // M3: a Generator burned its last coal
  | { type: 'well-dead'; t: number; x: number; y: number }
  | { type: 'hour'; t: number; row: HourRow };

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
  blocks: Block[];      // index = x*h + y (the Python grid order; ties in the bots break in this order)
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
  patch: { steel: number };   // steel left in the start block's patch
  power: PowerState;
  asmTrack: { n: number; at: number };   // asmTrack mode: assemblers running, tick last evaluated
  wellDead: boolean[];        // per well (index into `wells`)
  wellEnclosedSince: number[]; // per well: tick all four neighbours became Held, -1 = not enclosed
  events: SimEvent[];   // appended by step(); the consumer drains them
  /** Phase 4 M2: the tile-level machines, present only once `ensureFlow` ran (the game); absent in the harness. */
  flow?: FlowState;
}

// ------------------------------------------------------------------ proto section
/** The prototype's config numbers. Every number that moved in the calibration is tagged PROTO-CALIBRATED with the
 *  target it was set to hit (CALIBRATION_REPORT.md); the rest are the GAME-ASSUMPTION values of the build report.
 *  The regression fixtures never read this block: they run with the economy off and `startAsmRate` null. */
export const PROTO_CALIBRATED = {
  startAssemblers: 1,          // one assembler at the start (build report, assumption 3)
  // PROTO-CALIBRATED (T2): the start assembler is a 10 mag/min "Mk1"; the 20 mag/min assembler is the purchase.
  // Lever 1. Compact's second-assembler decision moves from 86–168 min (at 20) to 54–66 min; no other measured effect.
  startAsmRate: 10 as number | null,
  // PROTO-CALIBRATED: kept at 300. Lever 2 (100) was tried and rejected: 100 rounds fill one of the start block's
  // hoppers, so two pips are red at minute 0 for two minutes, and nothing else in the session changed.
  startRounds: 200,
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
    startPatch: { steel: 7680, perMin: 64 },
    magazineCost: { steel: 2, copper: 1 },   // §12 recipe, doc-derived, not a lever
    // §12: rubble is finite (doc rule). GAME-ASSUMPTION for the size: 120 min × yieldPerMin, so a block lasts about as
    // long as the doc's "start block's steel runs out in ~2 hours" at the current draw; the three districts are equal.
    pool: { civ: 3840, res: 3840, ind: 3840 },
  },
};

/** Apply the proto numbers to a base config (the proto's session and the calibration harness both go through here). */
export function protoCalibrated(base: SimConfig): SimConfig {
  const p = PROTO_CALIBRATED;
  return { ...base, asmSchedule: [], startAssemblers: p.startAssemblers, startAsmRate: p.startAsmRate, startRounds: p.startRounds,
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
