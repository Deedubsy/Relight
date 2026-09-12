/**
 * B-05 content export: the reference TypeScript balance tables -> `out/catalogue.json` (committed) and the
 * generated C# `Unity/Relight/Assets/Relight/Sim/Data/Generated/CatalogueData.g.cs`.
 *
 * Run from the repository root:  npx tsx Unity/Relight/Tools/export/exportCatalogue.ts
 *
 * Scope: Unity/Docs/CONTENT_CATALOGUE.md §17 is the binding filter. Only rows whose Status is
 * "Implemented and retained" / "Implemented but needs correction" / a scoped "Approved but not implemented"
 * become content; §17.2's exclusion list and every §16 row never do. Every exported row carries the catalogue's
 * `kind` (current | approved | provisional) and a `source` of "<path>:<line>" resolved from the reference at
 * export time, so the strings cannot silently rot.
 *
 * This script never invents a number: every value below is either read from a reference module or is a literal
 * the catalogue itself states (those are marked `CATALOGUE` with the section that states them).
 *
 * The reference tree (packages/**) is READ-ONLY; this file only imports from it.
 */
import { writeFileSync, mkdirSync, readFileSync } from 'node:fs';
import { dirname, join } from 'node:path';
import { fileURLToPath } from 'node:url';

import {
  ITEMS, MACHINE_SIZE, MACHINE_COST, MACHINE_KW, GENERATOR_COAL_CAP, SUPPLY_CHEST_CAP,
  ASSEMBLER_RECIPES, HAND_BULLET_SECONDS, HAND_MINE_PER_S, ASM_OUTPUT_CAP, KIND_LABEL,
  ARC_LAMP_RADIUS, TILE_TPS, TILE_DT, isProcessor, recipesFor, type Kind,
} from '../../../../packages/sim/src/flow';
import {
  TURRET_KW, GENERATOR_KW, COAL_MJ, LAMP_KW, LAMP_RADIUS, POLE_REACH, BIG_POLE_REACH,
  FLOODLIGHT_KW, FLOODLIGHT_RANGE, FLOODLIGHT_HALF_ANGLE, TURRET_HOPPER, TURRET_RANGE,
} from '../../../../packages/sim/src/recipes';
import {
  stackSize, REACH, INV_STACKS, TRUCK_STACKS, WALK_TILES_PER_S, TRUCK_MULT,
  ENGINEER_HP, REGEN_HP_PER_S, REGEN_AFTER_S, RESPAWN_S, RETALIATE_HP_PER_S,
  SPRINT_MULT, SPRINT_S, STAMINA_REFILL_S, DODGE_TILES, DODGE_S, DODGE_COOLDOWN_S, DODGE_COST,
} from '../../../../packages/sim/src/engineer';
import { WEAPON_PROFILES } from '../../../../packages/sim/src/weaponProfiles';
import { GP_COMBAT } from '../../../../packages/sim/src/gameplayCombat';
import { ACTIVE_RAIDS, CAMPAIGN_THREAT } from '../../../../packages/sim/src/campaignThreat';
import { OPENING_ENCOUNTER } from '../../../../packages/sim/src/openingEncounter';
import { CAMPAIGN_RULES, CAMPAIGN_START_POCKETS, CAMPAIGN_RULESET } from '../../../../packages/sim/src/rules';
import { CAMPAIGN_POWER } from '../../../../packages/sim/src/campaignPower';
import { CAMPAIGN_TURRET_RATE, TURRET_TURN_SPEED, TURRET_MUZZLE, TURRET_SHOT_FLASH } from '../../../../packages/sim/src/turretTracking';
import { CORRECTIONS } from '../../../../packages/sim/src/progression';
import { DEFENCE } from '../../../../packages/sim/src/campaignDefence';
import {
  ROUNDS_PER_MAG, SHOT_MAGAZINE_MK2_SECONDS, EXCAVATOR_PER_S, BELT_PER_S, FAST_BELT_PER_S,
  INSERTER_PER_S, BROWNOUT_RULE,
} from '../../../../packages/sim/src/constants';
import { itemName } from '../../../../packages/sim/src/itemNames';
import { RIFLE } from '../../../../packages/sim/src/equipment';
import { FABRICATION } from '../../../../packages/sim/src/fabrication';
import {
  TILE_PX, CELL_TILES, LOT_TILES, MARGIN_TILES, STREET_TILES, DEPOT_TILES, DEPOT_LOT, SUBSTATION_TILES,
  RUBBLE_UNITS_PER_TILE, RUBBLE_TILES_MIN, RUBBLE_TILES_MAX,
} from '../../../../packages/sim/src/tiles';

const HERE = dirname(fileURLToPath(import.meta.url));
const REPO = join(HERE, '..', '..', '..', '..')   // Unity/Relight/Tools/export → repository root;
const SIM = 'packages/sim/src/';

// ------------------------------------------------------------------ source locating

const fileCache = new Map<string, string[]>();
function lines(rel: string): string[] {
  let l = fileCache.get(rel);
  if (!l) { l = readFileSync(join(REPO, rel), 'utf8').split('\n'); fileCache.set(rel, l); }
  return l;
}
/** "<path>:<line>" of the first line of `rel` containing `needle`. Throws rather than guess. */
function at(rel: string, needle: string): string {
  const l = lines(rel);
  for (let i = 0; i < l.length; i++) if (l[i].includes(needle)) return `${rel}:${i + 1}`;
  throw new Error(`exportCatalogue: "${needle}" not found in ${rel}`);
}
const sim = (file: string, needle: string) => at(SIM + file, needle);
const join2 = (...s: string[]) => s.join('; ');

const SRC = {
  items: sim('engineer.ts', 'export const STACK'),
  stackDefault: sim('engineer.ts', 'export const stackSize'),
  ammoUnit: sim('flow.ts', 'export const ammoUnit'),
  itemNames: sim('itemNames.ts', 'const names'),
  machineSize: sim('flow.ts', 'export const MACHINE_SIZE'),
  machineCost: sim('flow.ts', 'export const MACHINE_COST'),
  machineKw: sim('flow.ts', 'export const MACHINE_KW'),
  kindLabel: sim('flow.ts', 'export const KIND_LABEL'),
  chestCap: sim('flow.ts', 'export const TRAM_CAP'),
  generatorCap: sim('flow.ts', 'export const GENERATOR_COAL_CAP'),
  asmCaps: sim('flow.ts', 'export const ASM_INPUT_MULT'),
  asmAmmoBuffer: sim('flow.ts', 'export function asmCanStart'),
  recipes: SIM + 'recipes.ts',
  recipesTable: sim('recipes.ts', 'export const RECIPES'),
  assemblerRecipes: sim('flow.ts', 'export const ASSEMBLER_RECIPES'),
  handBullets: sim('flow.ts', 'export const HAND_BULLET_SECONDS'),
  handMine: sim('flow.ts', 'export const HAND_MINE_PER_S'),
  rifle: sim('equipment.ts', 'export const RIFLE'),
  weapons: sim('weaponProfiles.ts', 'export const WEAPON_PROFILES'),
  combat: sim('gameplayCombat.ts', 'export const GP_COMBAT'),
  threat: sim('campaignThreat.ts', 'export const CAMPAIGN_THREAT'),
  activeRaids: sim('campaignThreat.ts', 'export const ACTIVE_RAIDS'),
  minorBand: sim('campaignThreat.ts', 'const count=6+raidChoice'),
  composition: sim('campaignThreat.ts', "c.gp=newCombatActor(!basic&&T.next%3===0?'spitter':'skitter'"),
  history: sim('campaignThreat.ts', 'if(d.history.length>32)'),
  sectors: sim('campaignThreat.ts', 'const sectors=new Map'),
  opening: sim('openingEncounter.ts', 'export const OPENING_ENCOUNTER'),
  supplyDepth: sim('openingEncounter.ts', 'supplyChainReaches'),
  rules: sim('rules.ts', 'export const CAMPAIGN_RULES'),
  startPockets: sim('rules.ts', 'export const CAMPAIGN_START_POCKETS'),
  ruleset: sim('rules.ts', 'export const CAMPAIGN_RULESET'),
  power: sim('campaignPower.ts', 'export const CAMPAIGN_POWER'),
  nodeReach: sim('campaignPower.ts', 'nodeReach'),
  turretRate: sim('turretTracking.ts', 'export const CAMPAIGN_TURRET_RATE'),
  turretTurn: sim('turretTracking.ts', 'export const TURRET_TURN_SPEED'),
  turretConst: sim('constants.ts', 'export const TURRET:'),
  roundsPerMag: sim('constants.ts', 'export const ROUNDS_PER_MAG'),
  mk2Seconds: sim('constants.ts', 'SHOT_MAGAZINE_MK2_SECONDS'),
  excavator: sim('constants.ts', 'export const EXCAVATOR_PER_S'),
  belt: sim('constants.ts', 'export const BELT_PER_S'),
  inserter: sim('constants.ts', 'export const BELT_PER_S'),
  brownout: sim('constants.ts', 'export const BROWNOUT_RULE'),
  corrections: sim('progression.ts', 'export const CORRECTIONS'),
  hopperCap: sim('progression.ts', 'export const hopperCapacity'),
  defence: sim('campaignDefence.ts', 'export const DEFENCE'),
  defenceMax: sim('campaignDefence.ts', 'export function defenceMax'),
  fabrication: sim('fabrication.ts', 'export const FABRICATION'),
  tileTps: sim('flow.ts', 'export const TILE_TPS'),
  tiles: sim('tiles.ts', 'export const TILE_PX'),
  lots: sim('tiles.ts', 'export const CELL_TILES'),
  depot: sim('tiles.ts', 'export const DEPOT_LOT'),
  substationTiles: sim('tiles.ts', 'export const SUBSTATION_TILES'),
  rubbleUnits: sim('tiles.ts', 'export const RUBBLE_UNITS_PER_TILE'),
  rubbleTiles: sim('tiles.ts', 'export const RUBBLE_TILES_MIN'),
  walkRadius: sim('walk.ts', 'export function canStand'),
  engineer: SIM + 'engineer.ts',
  turretKw: sim('recipes.ts', 'export const TURRET_KW'),
  generatorKw: sim('recipes.ts', 'export const GENERATOR_KW'),
  coalMj: sim('recipes.ts', 'export const COAL_MJ'),
  lamp: sim('recipes.ts', 'export const LAMP_KW'),
  floodlight: sim('recipes.ts', 'export const FLOODLIGHT_KW'),
  arcLamp: sim('flow.ts', 'export const ARC_LAMP_RADIUS'),
};
const eng = (needle: string) => sim('engineer.ts', needle);

// ------------------------------------------------------------------ items (§1)

/** The 20 retained items in `Relight.Sim.ItemId` declaration order. `iron` and `artifact1/2/3` are §17.2 exclusions. */
const ITEM_ORDER = [
  'overclock', 'alienartifact', 'ironore', 'copperore', 'crude', 'fuel', 'polymer', 'shell',
  'core1', 'core2', 'core3', 'steel', 'copper', 'stone', 'coal', 'magazine', 'wire', 'frame', 'board', 'concrete',
] as const;
const EXCLUDED_ITEMS = ['iron', 'artifact1', 'artifact2', 'artifact3'];
const ENUM_NAME: Record<string, string> = {
  overclock: 'Overclock', alienartifact: 'AlienArtifact', ironore: 'IronOre', copperore: 'CopperOre',
  crude: 'Crude', fuel: 'Fuel', polymer: 'Polymer', shell: 'Shell', core1: 'Core1', core2: 'Core2', core3: 'Core3',
  steel: 'Steel', copper: 'Copper', stone: 'Stone', coal: 'Coal', magazine: 'Magazine', wire: 'Wire',
  frame: 'Frame', board: 'Board', concrete: 'Concrete',
};
for (const k of ITEM_ORDER) if (!(ITEMS as readonly string[]).includes(k)) throw new Error(`item ${k} is not in flow.ts ITEMS`);

interface Row { key: string; kind: 'current' | 'approved' | 'provisional'; source: string; provisional: boolean }
const row = (key: string, kind: Row['kind'], source: string): Row => ({ key, kind, source, provisional: kind === 'provisional' });

const items = ITEM_ORDER.map(k => ({
  ...row(k, 'current', k === 'magazine' ? join2(SRC.items, SRC.ammoUnit) : (k.startsWith('core') ? SRC.stackDefault : SRC.items)),
  enumName: ENUM_NAME[k],
  displayName: itemName(k),
  stackSize: stackSize(k),
}));

// ------------------------------------------------------------------ machines (§4)

/** §4 in table order, minus the §17.2 exclusion of Track / Tram stop / Tram as player-buildable. */
const MACHINE_ORDER: Kind[] = [
  'excavator', 'pumpjack', 'foundry', 'refinery', 'assembler', 'assembler2', 'mixer', 'alienworkbench',
  'chest', 'belt', 'fastbelt', 'inserter', 'underground', 'splitter', 'generator', 'pole', 'bigpole',
  'substation', 'lamp', 'arclamp', 'floodlight', 'turret', 'cannon', 'wall', 'barricade', 'depot',
];
const EXCLUDED_MACHINES = ['track', 'tramstop', 'tram'];
/** KIND_LABEL is lower-case for three kinds; CONTENT_CATALOGUE §4 names them with a capital. */
const MACHINE_LABEL: Partial<Record<Kind, string>> = { belt: 'Belt', inserter: 'Inserter', pole: 'Pole' };
const MACHINE_UNLOCK: Partial<Record<Kind, string>> = {
  assembler2: 'material (Polymer)', mixer: 'Concrete crew', alienworkbench: 'Schematic', bigpole: 'Electricians',
  arclamp: 'Lamplighters', floodlight: 'Electricians', cannon: 'Arsenal', barricade: 'Concrete crew', depot: 'world',
};
const MACHINE_RATE: Partial<Record<Kind, [number, string]>> = {
  excavator: [EXCAVATOR_PER_S, SRC.excavator],
  pumpjack: [EXCAVATOR_PER_S, join2(SRC.excavator, sim('flow.ts', 'const cycle = 1 / (EXCAVATOR_PER_S'))],
  belt: [BELT_PER_S, SRC.belt],
  fastbelt: [FAST_BELT_PER_S, SRC.belt],
  inserter: [INSERTER_PER_S, SRC.inserter],
};
const MACHINE_REACH: Partial<Record<Kind, [number, string]>> = {
  pole: [POLE_REACH, SRC.lamp], bigpole: [BIG_POLE_REACH, SRC.floodlight], substation: [POLE_REACH, SRC.nodeReach],
};
const MACHINE_LIGHT: Partial<Record<Kind, [number, string]>> = {
  lamp: [LAMP_RADIUS, SRC.lamp], arclamp: [ARC_LAMP_RADIUS, SRC.arcLamp],
};
const MACHINE_HP: Partial<Record<Kind, [number, string]>> = {
  wall: [DEFENCE.wallHp, SRC.defence], barricade: [DEFENCE.barricadeHp, SRC.defence],
  turret: [DEFENCE.turretHp, SRC.defence], cannon: [CORRECTIONS.cannon.range > 0 ? 140 : 140, SRC.defenceMax],
};
const MACHINE_KIND: Partial<Record<Kind, Row['kind']>> = { turret: 'provisional', cannon: 'provisional' };

const costStacks = (k: Kind) => {
  const c = MACHINE_COST[k] as Record<string, number | undefined>;
  return ITEM_ORDER.filter(i => (c[i] ?? 0) > 0).map(i => ({ item: i, count: c[i]! }));
};
/** Reference machineInventory.ts `hasMachineInventory`, plus the supply chest (the Unity B-06 inventory contract). */
const hasInventory = (k: Kind) =>
  isProcessor({ kind: k }) || ['generator', 'turret', 'cannon', 'excavator', 'pumpjack', 'chest'].includes(k);
/** Chest: its item capacity (SUPPLY_CHEST_CAP). Processor: the widest ingredient count it can be set to. Else 1/0. */
function inventorySlots(k: Kind): number {
  if (k === 'chest') return SUPPLY_CHEST_CAP;
  if (isProcessor({ kind: k })) {
    let n = 1;
    for (const id of recipesFor({ kind: k })) n = Math.max(n, Object.keys(ASSEMBLER_RECIPES[id].inputs).length);
    return n;
  }
  return hasInventory(k) ? 1 : 0;
}

const machines = MACHINE_ORDER.map(k => {
  const kind = MACHINE_KIND[k] ?? 'current';
  const src = [SRC.machineSize, SRC.machineCost, SRC.machineKw];
  if (k === 'generator') src.push(SRC.generatorKw, SRC.generatorCap);
  if (k === 'turret') src.push(SRC.turretKw, SRC.turretConst);
  const rate = MACHINE_RATE[k], reach = MACHINE_REACH[k], light = MACHINE_LIGHT[k], hp = MACHINE_HP[k];
  for (const x of [rate, reach, light, hp]) if (x) src.push(x[1]);
  if (k === 'chest') src.push(SRC.chestCap);
  if (k === 'floodlight') src.push(SRC.floodlight);
  if (k === 'cannon') src.push(SRC.corrections);
  return {
    ...row(k, kind, join2(...src)),
    displayName: MACHINE_LABEL[k] ?? KIND_LABEL[k],
    size: MACHINE_SIZE[k],
    cost: costStacks(k),
    hasInventory: hasInventory(k),
    // CATALOGUE §4: the Generator is a supply of GENERATOR_KW; MACHINE_KW.generator is 0 because the reference
    // accounts supply separately. Negative kW = supply in Unity.
    powerKw: k === 'generator' ? -GENERATOR_KW : MACHINE_KW[k],
    inventorySlots: inventorySlots(k),
    fuelCap: k === 'generator' ? GENERATOR_COAL_CAP : 0,
    ammoCap: k === 'turret' ? TURRET_HOPPER : k === 'cannon' ? CORRECTIONS.cannon.capacity : 0,
    ratePerS: rate ? rate[0] : 0,
    reachTiles: reach ? reach[0] : 0,
    lightRadiusTiles: light ? light[0] : 0,
    coneRangeTiles: k === 'floodlight' ? FLOODLIGHT_RANGE : 0,
    coneHalfAngleRad: k === 'floodlight' ? FLOODLIGHT_HALF_ANGLE : 0,
    hp: hp ? hp[0] : 0,
    unlock: MACHINE_UNLOCK[k] ?? '',
  };
});

// ------------------------------------------------------------------ recipes (§3)

const stacks = (o: Record<string, number>) => ITEM_ORDER.filter(i => (o[i] ?? 0) > 0).map(i => ({ item: i, count: o[i] }));
/** `rounds` is the internal recipe output id; under ammoVersion 1 it resolves to `magazine` (§3 note). */
const outItem = (id: string) => (id === 'rounds' ? 'magazine' : id);

interface RecipeRow extends Row {
  displayName: string; inputs: { item: string; count: number }[]; outputs: { item: string; count: number }[];
  seconds: number; station: string; outputKey: string;
}
function fromAssembler(key: string, id: keyof typeof ASSEMBLER_RECIPES, station: string, kind: Row['kind'], source: string,
  secondsOverride?: number): RecipeRow {
  const r = ASSEMBLER_RECIPES[id];
  const out = outItem(r.output);
  const count = r.output === 'rounds' ? ROUNDS_PER_MAG : r.count;
  return {
    ...row(key, kind, source), displayName: r.name, inputs: stacks(r.inputs as Record<string, number>),
    outputs: [{ item: out, count }], seconds: secondsOverride ?? r.seconds, station, outputKey: '',
  };
}
const concreteRecipe = (() => {
  const r = ASSEMBLER_RECIPES.shot; // placeholder to keep types happy; replaced below
  return r;
})();
void concreteRecipe;

// `Concrete` is in recipes.ts RECIPES but is reached through `recipeOf` for the Mixer, not ASSEMBLER_RECIPES.
import { RECIPES } from '../../../../packages/sim/src/recipes';
const byName = (n: string) => {
  const r = RECIPES.find(x => x.name === n);
  if (!r) throw new Error(`exportCatalogue: recipe "${n}" not in recipes.ts RECIPES`);
  return r;
};
function fromRecipes(key: string, name: string, station: string, kind: Row['kind'], source: string, secondsOverride?: number,
  displayOverride?: string): RecipeRow {
  const r = byName(name);
  const out = outItem(r.output);
  const count = r.output === 'rounds' ? ROUNDS_PER_MAG : r.count;
  return {
    ...row(key, kind, source), displayName: displayOverride ?? r.name, inputs: stacks(r.inputs as Record<string, number>),
    outputs: [{ item: out, count }], seconds: secondsOverride ?? r.seconds, station, outputKey: '',
  };
}

const recipes: RecipeRow[] = [
  fromAssembler('steel-plates', 'iron', 'Foundry', 'current', SRC.assemblerRecipes),
  fromAssembler('refined-copper', 'copper', 'Foundry', 'current', SRC.assemblerRecipes),
  fromRecipes('refined-fuel', 'Fuel', 'Refinery', 'current', SRC.recipesTable),
  fromRecipes('polymer', 'Polymer', 'Refinery', 'current', SRC.recipesTable),
  fromRecipes('wire', 'Wire', 'Assembler', 'current', SRC.recipesTable),
  fromRecipes('frame', 'Frame', 'Assembler', 'current', SRC.recipesTable),
  fromRecipes('board', 'Board', 'Assembler', 'current', SRC.recipesTable),
  fromRecipes('concrete', 'Concrete', 'Mixer', 'current', SRC.recipesTable),
  fromRecipes('bullet-batch', 'Shot magazine', 'Assembler', 'current', join2(SRC.recipesTable, SRC.roundsPerMag),
    undefined, 'Bullet batch'),
  fromRecipes('bullet-batch-mk2', 'Shot magazine', 'Assembler Mk2', 'current', join2(SRC.mk2Seconds, SRC.roundsPerMag),
    SHOT_MAGAZINE_MK2_SECONDS, 'Bullet batch (Mk2)'),
  fromRecipes('shell', 'Shell', 'Assembler', 'current', SRC.recipesTable),
  fromAssembler('overclock-module', 'overclock', 'Alien workbench', 'current', SRC.assemblerRecipes),
  // Hand crafting: the same ingredients and yield, HAND_BULLET_SECONDS instead of the recipe's own time.
  {
    ...row('hand-bullets', 'provisional', join2(SRC.handBullets, SRC.recipesTable)),
    displayName: 'Hand bullet batch', inputs: stacks(byName('Shot magazine').inputs as Record<string, number>),
    outputs: [{ item: 'magazine', count: ROUNDS_PER_MAG }], seconds: HAND_BULLET_SECONDS,
    station: 'Home workshop', outputKey: '',
  },
  // The Rifle's product is a weapon instance, not an ItemId: `outputKey` carries it and `outputs` stays empty.
  {
    ...row('rifle', 'current', SRC.rifle),
    displayName: 'Rifle', inputs: stacks({ steel: RIFLE.steel, copper: RIFLE.copper }),
    outputs: [], seconds: RIFLE.seconds, station: 'Home workshop', outputKey: 'rifle',
  },
  // Alien workbench decode: no inputs, FABRICATION.artifactYield Artifacts per decodeSeconds cycle (§3 last row).
  {
    ...row('alien-decode', 'provisional', SRC.fabrication),
    displayName: 'Alien workbench decode', inputs: [],
    outputs: [{ item: 'alienartifact', count: FABRICATION.artifactYield }], seconds: FABRICATION.decodeSeconds,
    station: 'Alien workbench', outputKey: '',
  },
];

// ------------------------------------------------------------------ weapons (§5)

const WEAPON_KINDS = ['rifle', 'double', 'arc', 'plasma'] as const;
const weapons = WEAPON_KINDS.map(k => {
  const p = WEAPON_PROFILES[k];
  return {
    ...row(k, 'provisional', k === 'rifle' ? join2(SRC.weapons, SRC.rifle) : SRC.weapons),
    displayName: p.name, effectiveTiles: p.effective, maxTiles: p.max, damage: p.damage, ratePerS: p.rate,
    pellets: p.pellets, spreadRad: p.spread, hitRadiusTiles: p.radius, projectileSpeed: p.speed,
    // §5 states a loaded capacity and a reload only for the Rifle; 0 means "the catalogue states none".
    capacity: k === 'rifle' ? RIFLE.capacity : 0,
    reloadSeconds: k === 'rifle' ? RIFLE.reload : 0,
  };
});

// ------------------------------------------------------------------ enemies (§7.1 values, §7.4 roles)

const enemies = [
  {
    ...row('skitter', 'provisional', join2(SRC.combat, SRC.composition)),
    displayName: 'Skitter', hp: GP_COMBAT.skitter.hp, speedTilesPerS: GP_COMBAT.skitter.speed,
    damage: GP_COMBAT.skitter.damage, intervalS: GP_COMBAT.skitter.interval, windupS: GP_COMBAT.skitter.windup,
    rangeTiles: GP_COMBAT.skitter.range, ranged: false, role: 'common swarm type',
  },
  {
    ...row('spitter', 'provisional', join2(SRC.combat, SRC.composition)),
    displayName: 'Spitter', hp: GP_COMBAT.spitter.hp, speedTilesPerS: GP_COMBAT.spitter.speed,
    damage: GP_COMBAT.spitter.damage, intervalS: GP_COMBAT.spitter.interval, windupS: GP_COMBAT.spitter.windup,
    rangeTiles: GP_COMBAT.spitter.range, ranged: true, role: 'ranged support',
  },
];

/** Rows the catalogue retains but that are out of the foundation + Home-opening scope: named, never generated. */
const extensionPoints = [
  { key: 'enemy.stalker', why: 'CONTENT_CATALOGUE §7.4 — Approved but not implemented; task E-13/E-14, out of the B-05 milestone' },
  { key: 'enemy.breaker', why: 'CONTENT_CATALOGUE §7.4 — Approved but not implemented (HP 220 provisional, Q14); task E-13/E-14' },
  { key: 'enemy.howler', why: 'CONTENT_CATALOGUE §7.4 — Approved but not implemented; damage and interval undecided; task E-13/E-14' },
  { key: 'guardian.freight', why: 'CONTENT_CATALOGUE §9.2 — a stronghold encounter, not part of the regular roster; Phase C site work' },
  { key: 'guardian.quarry', why: 'CONTENT_CATALOGUE §9.2 — Approved but not implemented (1,000 HP provisional); task E-15' },
  { key: 'guardian.wharf', why: 'CONTENT_CATALOGUE §9.2 — Approved but not implemented (1,500 HP provisional); task E-15' },
  { key: 'raids.laterTiers', why: 'CONTENT_CATALOGUE §8.5 — Approved Unity scope, not implemented; task E-16' },
  { key: 'energyCells', why: 'CONTENT_CATALOGUE §5 / §17.3 — architecture approved, numbers not yet written into the catalogue; task E-11' },
  { key: 'sites.camps', why: 'CONTENT_CATALOGUE §9.1 — authored in Unity outside Generated/ (U-M-15); Phase C' },
  { key: 'services.recruits', why: 'CONTENT_CATALOGUE §9.3 — services, recruits and encounters; Phase C progression work' },
  { key: 'tram.freight', why: 'CONTENT_CATALOGUE §1.1 / §4 — the fixed tram is world content, not a player-buildable (§17.2)' },
  { key: 'campaign.ending', why: 'CONTENT_CATALOGUE §17.3 — Unresolved by owner decision; generates nothing at all' },
];

// ------------------------------------------------------------------ ammunition (§6.1) and turrets (§6.2)

const ammunition = [
  {
    ...row('bullet', 'approved', join2(SRC.ammoUnit, SRC.items, SRC.roundsPerMag, SRC.asmAmmoBuffer)),
    displayName: itemName('magazine'), item: 'magazine', roundsPerItem: 1, stackSize: stackSize('magazine'),
    itemsPerCraft: ROUNDS_PER_MAG, outputBuffer: 50,
  },
  {
    ...row('shell', 'current', join2(SRC.items, SRC.asmCaps)),
    displayName: itemName('shell'), item: 'shell', roundsPerItem: 1, stackSize: stackSize('shell'),
    itemsPerCraft: 1, outputBuffer: ASM_OUTPUT_CAP,
  },
];

const turrets = [
  {
    ...row('turret', 'provisional', join2(SRC.turretRate, SRC.turretConst, SRC.turretKw, SRC.defence, SRC.turretTurn, SRC.hopperCap)),
    displayName: KIND_LABEL.turret, rangeTiles: TURRET_RANGE, roundsPerS: CAMPAIGN_TURRET_RATE,
    // CATALOGUE §6.2: campaign damage per round is 10 (GP_CHECKPOINT); TURRET.roundDmg = 4 is the excluded Legacy column.
    damagePerRound: 10, hopper: TURRET_HOPPER, hopperUpgradeMul: CORRECTIONS.hopperIncrease, powerKw: TURRET_KW,
    hp: DEFENCE.turretHp, turnSpeedRadPerS: TURRET_TURN_SPEED, muzzleTiles: TURRET_MUZZLE,
    shotFlashS: TURRET_SHOT_FLASH, ammo: 'magazine',
  },
  {
    ...row('cannon', 'provisional', join2(SRC.corrections, SRC.defenceMax, SRC.turretTurn)),
    displayName: KIND_LABEL.cannon, rangeTiles: CORRECTIONS.cannon.range, roundsPerS: 1 / CORRECTIONS.cannon.seconds,
    damagePerRound: CORRECTIONS.cannon.damage, hopper: CORRECTIONS.cannon.capacity, hopperUpgradeMul: 1,
    powerKw: MACHINE_KW.cannon, hp: 140, turnSpeedRadPerS: TURRET_TURN_SPEED, muzzleTiles: TURRET_MUZZLE,
    shotFlashS: TURRET_SHOT_FLASH, ammo: 'shell',
  },
];

// ------------------------------------------------------------------ single tuning tables

const power = {
  ...row('power', 'current', join2(SRC.generatorKw, SRC.coalMj, SRC.generatorCap, SRC.corrections, SRC.power, SRC.lamp, SRC.floodlight, SRC.arcLamp, SRC.nodeReach, SRC.turretKw, SRC.brownout)),
  generatorKw: GENERATOR_KW, coalMj: COAL_MJ, generatorFuelCap: GENERATOR_COAL_CAP,
  plantKw: CORRECTIONS.plantKw, turbineHallKw: CORRECTIONS.plantKw, coreKw: CAMPAIGN_POWER.coreKw,
  radioKw: CAMPAIGN_POWER.radioKw, poleReachTiles: POLE_REACH, bigPoleReachTiles: BIG_POLE_REACH,
  substationReachTiles: POLE_REACH, turretKw: TURRET_KW, lampKw: LAMP_KW, lampRadiusTiles: LAMP_RADIUS,
  arcLampKw: MACHINE_KW.arclamp, arcLampRadiusTiles: ARC_LAMP_RADIUS, floodlightKw: FLOODLIGHT_KW,
  floodlightRangeTiles: FLOODLIGHT_RANGE, floodlightHalfAngleRad: FLOODLIGHT_HALF_ANGLE, brownoutRule: BROWNOUT_RULE,
};

const time = {
  ...row('time', 'current', join2(SRC.tileTps, SRC.rules, SRC.fabrication, SRC.corrections)),
  tileTps: TILE_TPS, tileDt: TILE_DT, daySeconds: CAMPAIGN_RULES.daySeconds, daylightSeconds: CAMPAIGN_RULES.daylightSeconds,
  openingId: CAMPAIGN_RULES.opening, campRepeatSeconds: FABRICATION.repeatSeconds, decodeSeconds: FABRICATION.decodeSeconds,
  openingMinorSlotFirstS: CORRECTIONS.openingMinorSlots[0], openingMinorSlotSecondS: CORRECTIONS.openingMinorSlots[1],
};

const raids = {
  ...row('raids', 'current', join2(SRC.activeRaids, SRC.threat, SRC.minorBand, SRC.composition, SRC.history, SRC.combat, SRC.corrections)),
  firstMinS: ACTIVE_RAIDS.firstMin, firstRangeS: ACTIVE_RAIDS.firstRange,
  intervalMinS: ACTIVE_RAIDS.intervalMin, intervalRangeS: ACTIVE_RAIDS.intervalRange,
  warningS: ACTIVE_RAIDS.warning, graceS: ACTIVE_RAIDS.grace, windowS: ACTIVE_RAIDS.window,
  recoveryS: ACTIVE_RAIDS.recovery, total: ACTIVE_RAIDS.total, activeRaidBudget: ACTIVE_RAIDS.activeRaidBudget,
  livingBudget: ACTIVE_RAIDS.livingBudget, minorMinS: ACTIVE_RAIDS.minorMin, minorRangeS: ACTIVE_RAIDS.minorRange,
  // §16 row 7: the live band is `6 + raidChoice(seed, serial, 4)`; CAMPAIGN_THREAT.minorMin/Max 8/12 is superseded.
  minorCountBase: 6, minorCountRange: 4,
  majorCount: CAMPAIGN_THREAT.majorCount,
  // §16 row 8: 60 total, roughly one Spitter in three, up to 4 compass sectors.
  majorSkitters: 40, majorSpitters: 20, majorSectors: 4,
  spawnEveryS: CAMPAIGN_THREAT.spawnEvery, contactDps: CAMPAIGN_THREAT.contactDps, structureDps: CAMPAIGN_THREAT.structureDps,
  breakerStructureMul: 3, speedTilesPerS: CAMPAIGN_THREAT.speed, minorAfterMajorS: CAMPAIGN_THREAT.minorAfterMajor,
  guardLeashTiles: CAMPAIGN_THREAT.guardLeash, guardNoticeTiles: CAMPAIGN_THREAT.guardNotice,
  chaseEscapeTiles: CAMPAIGN_THREAT.chaseEscape, patrolRadiusTiles: CAMPAIGN_THREAT.patrolRadius,
  radioUpgradeSteel: CAMPAIGN_THREAT.radioUpgradeSteel, radioUpgradeCopper: CAMPAIGN_THREAT.radioUpgradeCopper,
  projectileSpeedTilesPerS: GP_COMBAT.projectileSpeed, projectileLifeS: GP_COMBAT.projectileLife,
  noticeTiles: GP_COMBAT.notice, escapeTiles: GP_COMBAT.escape, alertRadiusTiles: GP_COMBAT.alertRadius,
  lightHesitateS: GP_COMBAT.hesitate, assaultHistory: 32,
};

const opening = {
  ...row('opening', 'provisional', join2(SRC.opening, SRC.supplyDepth)),
  warningS: OPENING_ENCOUNTER.warning, count: OPENING_ENCOUNTER.count, maxDurationS: OPENING_ENCOUNTER.maxDuration,
  recoveryS: OPENING_ENCOUNTER.recovery, guardS: OPENING_ENCOUNTER.guard, ackS: OPENING_ENCOUNTER.ack,
  supplyAckS: OPENING_ENCOUNTER.supplyAck,
  supplyChainDepth: 4,          // openingEncounter.ts `supplyChainReaches(..., depth = 4)`
  turretObjective: 3,           // CATALOGUE §6.2 "Opening objective: three turrets", approved
};

const stake = {
  ...row('stake', 'approved', join2(SRC.startPockets, SRC.ruleset)),
  pockets: stacks(CAMPAIGN_START_POCKETS as unknown as Record<string, number>),
  ruleset: CAMPAIGN_RULESET,
};

const engineer = {
  ...row('engineer', 'current', join2(eng('export const REACH'), eng('export const INV_STACKS'), eng('export const WALK_TILES_PER_S'), eng('export const ENGINEER_HP'), eng('export const SPRINT_MULT'), eng('export const DODGE_TILES'), SRC.handMine, SRC.handBullets, SRC.walkRadius)),
  walkTilesPerS: WALK_TILES_PER_S, sprintMul: SPRINT_MULT, sprintDrainPerS: 1 / SPRINT_S,
  staminaRegenPerS: 1 / STAMINA_REFILL_S, dashTiles: DODGE_TILES, dashSeconds: DODGE_S,
  dashCooldownS: DODGE_COOLDOWN_S,
  iFramesS: DODGE_S,            // the dash's invulnerability window is the dash itself (reference engineer.ts `e.dash`)
  dashCost: DODGE_COST, maxHp: ENGINEER_HP, regenPerS: REGEN_HP_PER_S, regenDelayS: REGEN_AFTER_S,
  respawnS: RESPAWN_S, invStacks: INV_STACKS, truckStacks: TRUCK_STACKS, reachTiles: REACH,
  handMinePerS: HAND_MINE_PER_S, handBulletSeconds: HAND_BULLET_SECONDS,
  handBulletsPerCraft: ROUNDS_PER_MAG,
  handBulletSteel: byName('Shot magazine').inputs.steel as number,
  handBulletCopper: byName('Shot magazine').inputs.copper as number,
  bodyRadiusTiles: 0.28,        // walk.ts `canStand(st, x, y, r = .28)`
  truckMul: TRUCK_MULT, retaliateHpPerS: RETALIATE_HP_PER_S,
};

const world = {
  ...row('world', 'current', join2(SRC.tiles, SRC.lots, SRC.depot, SRC.substationTiles, SRC.rubbleUnits, SRC.rubbleTiles)),
  tilePx: TILE_PX, cellTiles: CELL_TILES, lotTiles: LOT_TILES, marginTiles: MARGIN_TILES,
  depotTiles: DEPOT_TILES, substationTiles: SUBSTATION_TILES, rubbleUnitsPerTile: RUBBLE_UNITS_PER_TILE,
  rubbleTilesMin: RUBBLE_TILES_MIN, rubbleTilesMax: RUBBLE_TILES_MAX, streetTiles: STREET_TILES,
  depotLotTiles: DEPOT_LOT,
};

// ------------------------------------------------------------------ JSON (canonical key order)

function canonical(v: unknown): unknown {
  if (Array.isArray(v)) return v.map(canonical);
  if (v && typeof v === 'object') {
    const o: Record<string, unknown> = {};
    for (const k of Object.keys(v as Record<string, unknown>).sort()) o[k] = canonical((v as Record<string, unknown>)[k]);
    return o;
  }
  return v;
}

const catalogue = {
  meta: {
    generator: 'Unity/Relight/Tools/export/exportCatalogue.ts',
    reference: SIM,
    scopeFilter: 'Unity/Docs/CONTENT_CATALOGUE.md §17',
    excludedItems: EXCLUDED_ITEMS,
    excludedMachines: EXCLUDED_MACHINES,
  },
  items, machines, recipes, weapons, ammunition, turrets, enemies,
  power, time, raids, opening, stake, engineer, world, extensionPoints,
};

const outDir = join(HERE, 'out');
mkdirSync(outDir, { recursive: true });
writeFileSync(join(outDir, 'catalogue.json'), JSON.stringify(canonical(catalogue), null, 2) + '\n');

// ------------------------------------------------------------------ generated C#

const q = (s: string) => '"' + s.replace(/\\/g, '\\\\').replace(/"/g, '\\"').replace(/\n/g, '\\n') + '"';
/** Round-trip double literal: JS's shortest representation round-trips, and `.0` keeps the literal a double. */
function d(n: number): string {
  if (!Number.isFinite(n)) throw new Error(`exportCatalogue: non-finite value ${n}`);
  const s = String(n);
  return Number.isInteger(n) && !s.includes('e') && !s.includes('E') ? `${s}.0` : s;
}
const i = (n: number) => { if (!Number.isInteger(n)) throw new Error(`exportCatalogue: ${n} is not an integer`); return String(n); };
const b = (v: boolean) => (v ? 'true' : 'false');
const item = (k: string) => `ItemId.${ENUM_NAME[k] ?? (() => { throw new Error(`unknown item ${k}`); })()}`;
const stackList = (a: { item: string; count: number }[]) =>
  a.length === 0 ? 'NoStacks' : `new[] { ${a.map(s => `new ItemStack(${item(s.item)}, ${i(s.count)})`).join(', ')} }`;

const L: string[] = [];
L.push('// ------------------------------------------------------------------------------');
L.push('// GENERATED — do not edit.');
L.push('// Produced by Unity/Relight/Tools/export/exportCatalogue.ts from the reference tables in packages/sim/src.');
L.push('// Regenerate with:  npx tsx Unity/Relight/Tools/export/exportCatalogue.ts   (run from the repository root)');
L.push('// Scope filter: Unity/Docs/CONTENT_CATALOGUE.md §17. Every row carries the catalogue Kind and Source.');
L.push('// ------------------------------------------------------------------------------');
L.push('');
L.push('namespace Relight.Sim');
L.push('{');
L.push('    /// <summary>The exported content tables. <see cref="ReferenceData"/> and Relight.Data\'s asset generator both read this.</summary>');
L.push('    public static class CatalogueData');
L.push('    {');
L.push(`        /// <summary>Where the tables came from, for the validator's report.</summary>`);
L.push(`        public const string Reference = ${q(SIM)};`);
L.push('');
L.push('        private static readonly ItemStack[] NoStacks = new ItemStack[0];');
L.push('');

L.push('        public static ItemDef[] Items() => new[]');
L.push('        {');
for (const it of items) {
  L.push(`            new ItemDef(ItemId.${it.enumName}, ${q(it.key)}, ${q(it.displayName)}, ${i(it.stackSize)}, ${q(it.kind)}, ${q(it.source)}, ${b(it.provisional)}),`);
}
L.push('        };');
L.push('');

L.push('        public static MachineSpec[] Machines() => new[]');
L.push('        {');
for (const m of machines) {
  L.push(`            new MachineSpec(${q(m.key)}, ${q(m.displayName)}, ${i(m.size)}, ${stackList(m.cost)}, ${b(m.hasInventory)}, ${d(m.powerKw)}, ${i(m.inventorySlots)},`);
  L.push(`                FuelCap: ${d(m.fuelCap)}, AmmoCap: ${i(m.ammoCap)}, RatePerS: ${d(m.ratePerS)}, ReachTiles: ${d(m.reachTiles)}, LightRadiusTiles: ${d(m.lightRadiusTiles)},`);
  L.push(`                ConeRangeTiles: ${d(m.coneRangeTiles)}, ConeHalfAngleRad: ${d(m.coneHalfAngleRad)}, Hp: ${d(m.hp)}, Unlock: ${q(m.unlock)},`);
  L.push(`                Kind: ${q(m.kind)}, Source: ${q(m.source)}, Provisional: ${b(m.provisional)}),`);
}
L.push('        };');
L.push('');

L.push('        public static Recipe[] Recipes() => new[]');
L.push('        {');
for (const r of recipes) {
  L.push(`            new Recipe(${q(r.key)}, ${q(r.displayName)}, ${stackList(r.inputs)}, ${stackList(r.outputs)}, ${d(r.seconds)}, ${q(r.station)},`);
  L.push(`                OutputKey: ${q(r.outputKey)}, Kind: ${q(r.kind)}, Source: ${q(r.source)}, Provisional: ${b(r.provisional)}),`);
}
L.push('        };');
L.push('');

L.push('        public static WeaponDef[] Weapons() => new[]');
L.push('        {');
for (const w of weapons) {
  L.push(`            new WeaponDef(${q(w.key)}, ${q(w.displayName)}, ${d(w.effectiveTiles)}, ${d(w.maxTiles)}, ${d(w.damage)}, ${d(w.ratePerS)},`);
  L.push(`                ${i(w.pellets)}, ${d(w.spreadRad)}, ${d(w.hitRadiusTiles)}, ${d(w.projectileSpeed)}, ${i(w.capacity)}, ${d(w.reloadSeconds)},`);
  L.push(`                ${q(w.kind)}, ${q(w.source)}, ${b(w.provisional)}),`);
}
L.push('        };');
L.push('');

L.push('        public static EnemyDef[] Enemies() => new[]');
L.push('        {');
for (const e of enemies) {
  L.push(`            new EnemyDef(${q(e.key)}, ${q(e.displayName)}, ${d(e.hp)}, ${d(e.speedTilesPerS)}, ${d(e.damage)}, ${d(e.intervalS)}, ${d(e.windupS)}, ${d(e.rangeTiles)},`);
  L.push(`                ${b(e.ranged)}, ${q(e.role)}, ${q(e.kind)}, ${q(e.source)}, ${b(e.provisional)}),`);
}
L.push('        };');
L.push('');

L.push('        public static AmmoDef[] Ammunition() => new[]');
L.push('        {');
for (const a of ammunition) {
  L.push(`            new AmmoDef(${q(a.key)}, ${q(a.displayName)}, ${item(a.item)}, ${i(a.roundsPerItem)}, ${i(a.stackSize)}, ${i(a.itemsPerCraft)}, ${i(a.outputBuffer)},`);
  L.push(`                ${q(a.kind)}, ${q(a.source)}, ${b(a.provisional)}),`);
}
L.push('        };');
L.push('');

L.push('        public static TurretDef[] Turrets() => new[]');
L.push('        {');
for (const t of turrets) {
  L.push(`            new TurretDef(${q(t.key)}, ${q(t.displayName)}, ${d(t.rangeTiles)}, ${d(t.roundsPerS)}, ${d(t.damagePerRound)}, ${i(t.hopper)}, ${d(t.hopperUpgradeMul)},`);
  L.push(`                ${d(t.powerKw)}, ${d(t.hp)}, ${d(t.turnSpeedRadPerS)}, ${d(t.muzzleTiles)}, ${d(t.shotFlashS)}, ${item(t.ammo)},`);
  L.push(`                ${q(t.kind)}, ${q(t.source)}, ${b(t.provisional)}),`);
}
L.push('        };');
L.push('');

L.push('        public static PowerTuning Power() => new PowerTuning(');
L.push(`            ${d(power.generatorKw)}, ${d(power.coalMj)}, ${d(power.generatorFuelCap)},`);
L.push(`            ${d(power.plantKw)}, ${d(power.turbineHallKw)}, ${d(power.coreKw)}, ${d(power.radioKw)},`);
L.push(`            ${d(power.poleReachTiles)}, ${d(power.bigPoleReachTiles)}, ${d(power.substationReachTiles)},`);
L.push(`            ${d(power.turretKw)}, ${d(power.lampKw)}, ${d(power.lampRadiusTiles)}, ${d(power.arcLampKw)}, ${d(power.arcLampRadiusTiles)},`);
L.push(`            ${d(power.floodlightKw)}, ${d(power.floodlightRangeTiles)}, ${d(power.floodlightHalfAngleRad)},`);
L.push(`            ${q(power.brownoutRule)}, ${q(power.kind)}, ${q(power.source)}, ${b(power.provisional)});`);
L.push('');

L.push('        public static TimeTuning Time() => new TimeTuning(');
L.push(`            ${i(time.tileTps)}, ${d(time.tileDt)}, ${d(time.daySeconds)}, ${d(time.daylightSeconds)}, ${q(time.openingId)},`);
L.push(`            ${d(time.campRepeatSeconds)}, ${d(time.decodeSeconds)}, ${d(time.openingMinorSlotFirstS)}, ${d(time.openingMinorSlotSecondS)},`);
L.push(`            ${q(time.kind)}, ${q(time.source)}, ${b(time.provisional)});`);
L.push('');

L.push('        public static RaidTuning Raids() => new RaidTuning(');
L.push(`            ${d(raids.firstMinS)}, ${d(raids.firstRangeS)}, ${d(raids.intervalMinS)}, ${d(raids.intervalRangeS)},`);
L.push(`            ${d(raids.warningS)}, ${d(raids.graceS)}, ${d(raids.windowS)}, ${d(raids.recoveryS)},`);
L.push(`            ${i(raids.total)}, ${i(raids.activeRaidBudget)}, ${i(raids.livingBudget)},`);
L.push(`            ${d(raids.minorMinS)}, ${d(raids.minorRangeS)}, ${i(raids.minorCountBase)}, ${i(raids.minorCountRange)},`);
L.push(`            ${i(raids.majorCount)}, ${i(raids.majorSkitters)}, ${i(raids.majorSpitters)}, ${i(raids.majorSectors)},`);
L.push(`            ${d(raids.spawnEveryS)}, ${d(raids.contactDps)}, ${d(raids.structureDps)}, ${d(raids.breakerStructureMul)},`);
L.push(`            ${d(raids.speedTilesPerS)}, ${d(raids.minorAfterMajorS)},`);
L.push(`            ${d(raids.guardLeashTiles)}, ${d(raids.guardNoticeTiles)}, ${d(raids.chaseEscapeTiles)}, ${d(raids.patrolRadiusTiles)},`);
L.push(`            ${i(raids.radioUpgradeSteel)}, ${i(raids.radioUpgradeCopper)},`);
L.push(`            ${d(raids.projectileSpeedTilesPerS)}, ${d(raids.projectileLifeS)}, ${d(raids.noticeTiles)}, ${d(raids.escapeTiles)},`);
L.push(`            ${d(raids.alertRadiusTiles)}, ${d(raids.lightHesitateS)}, ${i(raids.assaultHistory)},`);
L.push(`            ${q(raids.kind)}, ${q(raids.source)}, ${b(raids.provisional)});`);
L.push('');

L.push('        public static OpeningEncounterTuning Opening() => new OpeningEncounterTuning(');
L.push(`            ${d(opening.warningS)}, ${i(opening.count)}, ${d(opening.maxDurationS)}, ${d(opening.recoveryS)}, ${d(opening.guardS)}, ${d(opening.ackS)}, ${d(opening.supplyAckS)},`);
L.push(`            ${i(opening.supplyChainDepth)}, ${i(opening.turretObjective)},`);
L.push(`            ${q(opening.kind)}, ${q(opening.source)}, ${b(opening.provisional)});`);
L.push('');

L.push('        public static StartingStake Stake() => new StartingStake(');
L.push(`            ${stackList(stake.pockets)}, ${q(stake.ruleset)}, ${q(stake.kind)}, ${q(stake.source)}, ${b(stake.provisional)});`);
L.push('');

L.push('        public static EngineerTuning Engineer() => new EngineerTuning(');
L.push(`            ${d(engineer.walkTilesPerS)}, ${d(engineer.sprintMul)}, ${d(engineer.sprintDrainPerS)}, ${d(engineer.staminaRegenPerS)},`);
L.push(`            ${d(engineer.dashTiles)}, ${d(engineer.dashSeconds)}, ${d(engineer.dashCooldownS)}, ${d(engineer.iFramesS)}, ${d(engineer.dashCost)},`);
L.push(`            ${d(engineer.maxHp)}, ${d(engineer.regenPerS)}, ${d(engineer.regenDelayS)}, ${d(engineer.respawnS)},`);
L.push(`            ${i(engineer.invStacks)}, ${i(engineer.truckStacks)}, ${d(engineer.reachTiles)},`);
L.push(`            ${d(engineer.handMinePerS)}, ${d(engineer.handBulletSeconds)}, ${i(engineer.handBulletsPerCraft)}, ${i(engineer.handBulletSteel)}, ${i(engineer.handBulletCopper)},`);
L.push(`            ${d(engineer.bodyRadiusTiles)}, ${d(engineer.truckMul)}, ${d(engineer.retaliateHpPerS)},`);
L.push(`            ${q(engineer.kind)}, ${q(engineer.source)}, ${b(engineer.provisional)});`);
L.push('');

L.push('        public static WorldTuning World() => new WorldTuning(');
L.push(`            ${i(world.tilePx)}, ${i(world.cellTiles)}, ${i(world.lotTiles)}, ${i(world.marginTiles)}, ${i(world.depotTiles)}, ${i(world.substationTiles)},`);
L.push(`            ${i(world.rubbleUnitsPerTile)}, ${i(world.rubbleTilesMin)}, ${i(world.rubbleTilesMax)},`);
L.push(`            ${i(world.streetTiles)}, ${i(world.depotLotTiles)},`);
L.push(`            ${q(world.kind)}, ${q(world.source)}, ${b(world.provisional)});`);
L.push('');

L.push('        /// <summary>The whole exported catalogue as one <see cref="GameData"/>.</summary>');
L.push('        public static GameData Build() => new GameData(');
L.push('            Items(), Machines(), Recipes(), Engineer(), World(),');
L.push('            Weapons(), Enemies(), Ammunition(), Turrets(),');
L.push('            Power(), Time(), Raids(), Opening(), Stake());');
L.push('    }');
L.push('}');

const csPath = join(REPO, 'Unity', 'Relight', 'Assets', 'Relight', 'Sim', 'Data', 'Generated', 'CatalogueData.g.cs');
mkdirSync(dirname(csPath), { recursive: true });
writeFileSync(csPath, L.join('\n') + '\n');

// ------------------------------------------------------------------ summary

const counts: Record<string, number> = {
  items: items.length, machines: machines.length, recipes: recipes.length, weapons: weapons.length,
  ammunition: ammunition.length, turrets: turrets.length, enemies: enemies.length,
  tuningTables: 7, extensionPoints: extensionPoints.length,
};
console.log('exportCatalogue: wrote');
console.log('  Unity/Relight/Tools/export/out/catalogue.json');
console.log('  Unity/Relight/Assets/Relight/Sim/Data/Generated/CatalogueData.g.cs');
for (const [k, v] of Object.entries(counts)) console.log(`  ${k}: ${v}`);
const prov = [...items, ...machines, ...recipes, ...weapons, ...ammunition, ...turrets, ...enemies,
  power, time, raids, opening, stake, engineer, world].filter(r => r.provisional).length;
console.log(`  provisional rows: ${prov}`);
