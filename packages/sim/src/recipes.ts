/** §12 recipes and §11/§12 energy constants. The doc's recipe table is generated from RECIPES (packages/tools/docsync). */
export interface Recipe { name: string; inputs: Record<string, number>; output: string; count: number; seconds: number; at: string }

export const RECIPES: readonly Recipe[] = [
  { name: 'Wire',          inputs: { copper: 1 },            output: 'wire',     count: 2,  seconds: 1, at: 'Assembler' },
  { name: 'Frame',         inputs: { steel: 2 },             output: 'frame',    count: 1,  seconds: 2, at: 'Assembler' },
  { name: 'Concrete',      inputs: { stone: 2 },             output: 'concrete', count: 1,  seconds: 2, at: 'Mixer' },
  { name: 'Board',         inputs: { wire: 3, steel: 1 },    output: 'board',    count: 1,  seconds: 4, at: 'Assembler' },
  { name: 'Shot magazine', inputs: { steel: 2, copper: 1 },  output: 'rounds',   count: 10, seconds: 3, at: 'Assembler' },
  { name: 'Shell',         inputs: { steel: 2, coal: 1 },    output: 'shell',    count: 1,  seconds: 3, at: 'Assembler (Arsenal recipe)' },
  { name: 'Fuel',          inputs: { crude: 1 },             output: 'fuel',     count: 4,  seconds: 3, at: 'Refinery' },
  { name: 'Polymer',       inputs: { crude: 2 },             output: 'polymer',  count: 1,  seconds: 3, at: 'Refinery' },
];

/** §11: one coal = 4 MJ; a Generator makes 300 kW; the start patch is ~700 coal (D1). */
export const COAL_MJ = 4;
export const GENERATOR_KW = 300;
export const START_COAL = 40;
export const START_COAL_PATCH = 700;
/** Rounds per magazine (§12 Shot magazine). */
export const ROUNDS_PER_MAG = 10;
/** §13 defence (M3): Gun turret 2×2, range 9, 5 rounds/s, 50-round hopper (5 magazines); Lamp 1×1 5 kW radius 4;
 *  pole reach 8. */
export const TURRET_HOPPER = 50, TURRET_RANGE = 9, TURRET_ROUNDS_PER_S = 5;
export const LAMP_KW = 5, LAMP_RADIUS = 4, POLE_REACH = 8;
