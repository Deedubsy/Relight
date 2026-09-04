/** §12 recipes and §11/§12 energy constants. The doc's recipe table is generated from RECIPES (packages/tools/docsync).
 *  The Shot magazine's numbers and the turret's come from constants.ts (the source of truth, guardrails Step 5).
 *  D-P4-4 / D-B1-1: the start Assembler is the Mk1 — a magazine in 6 s, 10 mag/min, the map-view calibration's
 *  `startAsmRate`; the 3 s / 20 mag/min rate is the Mk2 (the calibration's `asmRate`, the purchase), a §12 row the
 *  slice does not place. */
import { SHOT_MAGAZINE, SHOT_MAGAZINE_MK2_SECONDS, TURRET, START_CHEST } from './constants';
export interface Recipe { name: string; inputs: Record<string, number>; output: string; count: number; seconds: number; at: string }

export const RECIPES: readonly Recipe[] = [
  { name: 'Wire',          inputs: { copper: 1 },            output: 'wire',     count: 2,  seconds: 1, at: 'Assembler' },
  { name: 'Frame',         inputs: { steel: 2 },             output: 'frame',    count: 1,  seconds: 2, at: 'Assembler' },
  { name: 'Concrete',      inputs: { stone: 2 },             output: 'concrete', count: 1,  seconds: 2, at: 'Mixer' },
  { name: 'Board',         inputs: { wire: 3, steel: 1 },    output: 'board',    count: 1,  seconds: 4, at: 'Assembler' },
  { name: 'Shot magazine', inputs: { steel: SHOT_MAGAZINE.steel, copper: SHOT_MAGAZINE.copper }, output: 'rounds', count: SHOT_MAGAZINE.rounds, seconds: SHOT_MAGAZINE.seconds, at: 'Assembler (Mk1)' },
  { name: 'Shot magazine (Mk2)', inputs: { steel: SHOT_MAGAZINE.steel, copper: SHOT_MAGAZINE.copper }, output: 'rounds', count: SHOT_MAGAZINE.rounds, seconds: SHOT_MAGAZINE_MK2_SECONDS, at: 'Assembler Mk2 (the purchase; unlock open, D-B1-1)' },
  { name: 'Shell',         inputs: { steel: 2, coal: 1 },    output: 'shell',    count: 1,  seconds: 3, at: 'Assembler (Arsenal recipe)' },
  { name: 'Fuel',          inputs: { crude: 1 },             output: 'fuel',     count: 4,  seconds: 3, at: 'Refinery' },
  { name: 'Polymer',       inputs: { crude: 2 },             output: 'polymer',  count: 1,  seconds: 3, at: 'Refinery' },
];

/** §11: one coal = 4 MJ; a Generator makes 300 kW; the start patch is ~700 coal (D1). */
export const COAL_MJ = 4;
export const GENERATOR_KW = 300;
export const START_COAL = 40;
/** D-P4-7 (b): 40 more coal in the Depot chest at the start, in addition to Generator 1's hopper, so the second
 *  Generator is fed the minute it stands (the coal Excavator's first units reach the chest after it) — constants.ts. */
export const START_CHEST_COAL: number = START_CHEST.coal;
export const START_COAL_PATCH = 700;
/** Rounds per magazine (§12 Shot magazine; constants.ts). */
export { ROUNDS_PER_MAG } from './constants';
/** §13 defence (M3): Gun turret 2×2, range 9, 5 rounds/s, 50-round hopper (5 magazines); Lamp 1×1 5 kW radius 4;
 *  pole reach 8. */
export const TURRET_HOPPER = TURRET.hopper, TURRET_RANGE = TURRET.range, TURRET_ROUNDS_PER_S = TURRET.roundsPerS;
export const LAMP_KW = 5, LAMP_RADIUS = 4, POLE_REACH = 8;
/** D-B5-4: a kerb streetlight reaches the street's midline (radius 7) — constants.ts; the Lamp keeps §13's radius 4. */
export { STREETLIGHT_RADIUS } from './constants';
/** §13 Electricians' unlocks (prompt B M3): Floodlight 2×2 40 kW with a 12-tile cone; Big pole 2×2 reach 12.
 *  GAME-ASSUMPTION: the cone is 60° wide (±30° about the facing — §13 gives only its length). */
export const FLOODLIGHT_KW = 40, FLOODLIGHT_RANGE = 12, FLOODLIGHT_HALF_ANGLE = Math.PI / 6, BIG_POLE_REACH = 12;
