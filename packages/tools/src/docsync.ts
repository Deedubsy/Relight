/**
 * docsync — the §7 district and enemy tables and the §12 recipe table in docs/RELIGHT-design.md are generated from
 * packages/sim (districts.ts, enemies.ts, recipes.ts) plus the E3-block steady state in docs/experiments/E3.json.
 * `npm run docsync` rewrites them; `npm run docsync:check` (CI) fails if the doc differs from what the code says.
 * Edit the .ts files, never the tables.
 *
 * The §18 map-view and lot drawings (section18, section18lot) were retired in the D5/D6 rework: §18 is redrawn as
 * rendered map-view images at rework Step 5 (docs/), not as generated text.
 */
import { readFileSync, writeFileSync, existsSync } from 'node:fs';
import { join, dirname } from 'node:path';
import { fileURLToPath } from 'node:url';
import {
  DISTRICTS, RAIL_YARD, ENEMIES, RECIPES, WELL_RANGE, WELL_CAP_BONUS, WELL_G_MULT,
  // the source of truth (packages/sim/src/constants.ts, guardrails Step 5) and its readers
  SHOT_MAGAZINE, ASSEMBLER_TIERS, ASSEMBLER_MAG_PER_MIN, EXCAVATOR_PER_S, BELT_PER_S, FAST_BELT_PER_S, INSERTER_PER_S, TURRET,
  TURRET_PER_TILES, LAMP_STEP_TILES, SUBSTATION_KW, BROWNOUT_RULE, START_CHEST, HOUR_GENERATOR_MIN, HOUR_CLAIM_MIN, HOUR_SHOT_LINE_MIN, HOUR_MINUTES,
  DEFAULT_CONFIG, protoCalibrated, ROUNDS_PER_MAG, TURRET_HOPPER, TURRET_RANGE, TURRET_ROUNDS_PER_S, ROUND_DMG,
  FACE_LIGHT_STEP, STREETLIGHT_STEP, FIRST_HOUR_DEFAULTS, FIRST_HOUR_D1, FIRST_HOUR_CLAIMS, HOUR_GEN_AT, HOUR_CLAIM_AT, SHOT,
} from '@relight/sim';

const root = join(dirname(fileURLToPath(import.meta.url)), '..', '..', '..');
const docPath = join(root, 'docs', 'RELIGHT-design.md');
const e3Path = join(root, 'docs', 'experiments', 'E3.json');

interface BlockRow { d: number; magPerMin: number; shellsPerMin: number }
function e3Block(): Record<string, BlockRow> {
  if (!existsSync(e3Path)) throw new Error(`docsync: ${e3Path} missing — run npm run experiments first`);
  return JSON.parse(readFileSync(e3Path, 'utf8')).data.block as Record<string, BlockRow>;
}

const f1 = (x: number) => x.toFixed(1);
const f2 = (x: number) => x.toFixed(2);
const range = (xs: number[], f: (x: number) => string) => `${f(Math.min(...xs))}–${f(Math.max(...xs))}`;
const ammo = (r: BlockRow) => `${f1(r.magPerMin)} mag/min` + (r.shellsPerMin > 0 ? ` + ${f1(r.shellsPerMin)} shells/min` : '');

function districtsTable(): string {
  const b = e3Block();
  const lines = [
    '| District | Rubble type | dmax | g (per s) | Steady-state awake *d* **[sim: E3-block]** | Ammo per awake block **[sim: E3-block]** |',
    '|---|---|---|---|---|---|',
  ];
  for (const r of DISTRICTS) {
    const s = b[r.name];
    lines.push(`| ${r.label} | ${r.rubble} | ${f2(r.base)} | ${r.g} | ${f2(s.d)} | ${ammo(s)} |`);
    if (r.name === RAIL_YARD.sameAs) lines.push(`| ${RAIL_YARD.label} | ${RAIL_YARD.rubble} | ${f2(r.base)} | ${r.g} | ${f2(s.d)} | ${ammo(s)} |`);
  }
  const wells = Object.keys(b).filter(k => k.startsWith('well on ')).map(k => b[k]);
  lines.push(
    `| Within ${WELL_RANGE} blocks of a well | as district | +${f2(WELL_CAP_BONUS)} | ×${1 + WELL_G_MULT} | ` +
    `${range(wells.map(w => w.d), f2)} | ${range(wells.map(w => w.magPerMin), f1)} mag/min + ${range(wells.map(w => w.shellsPerMin), f1)} shells/min |`,
  );
  return lines.join('\n');
}

function enemiesTable(): string {
  const cols = ENEMIES.map(e => e.name);
  const hp = (e: (typeof ENEMIES)[number]) => `${e.hp} (${e.note})`;
  const row = (label: string, f: (e: (typeof ENEMIES)[number]) => string) => `| ${label} | ${ENEMIES.map(f).join(' | ')} |`;
  return [
    `| | ${cols.join(' | ')} |`,
    `|---|${cols.map(() => '---').join('|')}|`,
    row('Footprint / speed', e => e.footprint),
    row('HP', hp),
    row('Appears', e => e.appears),
    row('Targets', e => e.targets),
    row('Special', e => e.special),
    row('Punishes', e => e.punishes),
    row('The player sees', e => e.sees),
  ].join('\n');
}

const ITEM: Record<string, string> = { copper: 'Cu', steel: 'steel', stone: 'stone', wire: 'wire', coal: 'coal', crude: 'crude' };
function recipesTable(): string {
  const lines = ['| Recipe | Inputs | Output | Time | Made in |', '|---|---|---|---|---|'];
  for (const r of RECIPES) {
    const inputs = Object.entries(r.inputs).map(([k, n]) => `${n} ${ITEM[k] ?? k}`).join(' + ');
    const out = r.output === 'rounds' ? `1 magazine (${r.count} rounds)` : `${r.count} ${r.output}`;
    lines.push(`| ${r.name} | ${inputs} | ${out} | ${r.seconds} s | ${r.at} |`);
  }
  return lines.join('\n');
}

/** Guardrails Step 5: the doc's table of the constants the sim is built from, generated from constants.ts. */
function constantsTable(): string {
  const lines = ['| Constant | Value | Where the doc says it | Decision |', '|---|---|---|---|'];
  for (const t of ASSEMBLER_TIERS) lines.push(`| Assembler rate (${t.name}) | ${t.magPerMin} mag/min | §12 ammo chain, §13 Assembler row | C9, D-P3-7 |`);
  lines.push(`| Shot magazine | ${SHOT_MAGAZINE.steel} steel + ${SHOT_MAGAZINE.copper} Cu → ${SHOT_MAGAZINE.rounds} rounds in ${SHOT_MAGAZINE.seconds} s | §12 recipe table | — |`);
  lines.push(`| Excavator rate | ${EXCAVATOR_PER_S}/s | §13 Excavator row | [sim: M2-rates] |`);
  lines.push(`| Belt / fast belt | ${BELT_PER_S}/s · ${FAST_BELT_PER_S}/s | §13 belt row, §14 | D-P4-6 |`);
  lines.push(`| Inserter | ${INSERTER_PER_S} item/s | §13 Inserter row, §14 | [sim: M2-rates] |`);
  lines.push(`| Gun turret | range ${TURRET.range}, ${TURRET.roundsPerS} rounds/s, ${TURRET.hopper}-round hopper, ${TURRET.roundDmg} HP a round | §13 Gun turret row, §7 | [sim: M3-rates, B-M4-threat] |`);
  lines.push(`| Turret kits per segment | one per ${TURRET_PER_TILES} tiles of segment length, at least one | §13 pre-existing fixtures, §14 | D-B1-4 |`);
  lines.push(`| Streetlight spacing | one every ${LAMP_STEP_TILES} tiles along the kerb | §13 pre-existing fixtures | D-B1-4 |`);
  lines.push(`| Substation draw | ${SUBSTATION_KW.front} kW front · ${SUBSTATION_KW.interior} kW interior | §5 table | D1 |`);
  lines.push(`| Brownout rule | ${BROWNOUT_RULE}: every machine runs at supply ÷ demand, nothing sheds | §14 Power | D-B3-4 |`);
  lines.push(`| Start chest | ${START_CHEST.steel} steel, ${START_CHEST.copper} copper, ${START_CHEST.stone} stone, ${START_CHEST.magazines} magazines | §11 0–10 min | C10, D-P2-1, D-B1-1 |`);
  lines.push(`| §11 minute list (the hour bot) | ${HOUR_MINUTES.map(m => `${m.min}: ${m.what}`).join(' · ')} | §11's three windows | D-P3-6, D-P4-7, D-B6-2 |`);
  return lines.join('\n');
}

/** Guardrails Step 5: where the calibration config, the block sim, firsthour.ts, hour.ts or the doc's prose carry a
 *  number that differs from constants.ts. Each line is "Constant X: doc says A, calibration says B, code says C".
 *  Nothing here is fixed by docsync: the list is the report's §5 and a decision row's evidence. */
function disagreements(doc: string): string[] {
  const out: string[] = [];
  const cal = protoCalibrated(DEFAULT_CONFIG);
  const has = (re: RegExp) => re.test(doc);
  // assembler rate by tier
  if (cal.startAsmRate !== null && cal.startAsmRate !== ASSEMBLER_MAG_PER_MIN)
    out.push(`Assembler rate: doc says ${ASSEMBLER_MAG_PER_MIN} mag/min (one tier), calibration says the start "Mk1" makes ${cal.startAsmRate} mag/min (PROTO_CALIBRATED.startAsmRate), code says ${60 / SHOT.seconds} mag/min (the tile assembler's ${SHOT.seconds} s recipe)`);
  if (cal.asmRate !== ASSEMBLER_MAG_PER_MIN) out.push(`Assembler rate: doc says ${ASSEMBLER_MAG_PER_MIN}, calibration asmRate says ${cal.asmRate}`);
  // magazine recipe
  const mc = cal.eco.magazineCost;
  if (mc.steel !== SHOT_MAGAZINE.steel || mc.copper !== SHOT_MAGAZINE.copper) out.push(`Magazine recipe: doc says ${SHOT_MAGAZINE.steel} steel + ${SHOT_MAGAZINE.copper} Cu, calibration says ${mc.steel} + ${mc.copper}`);
  if (ROUNDS_PER_MAG !== SHOT_MAGAZINE.rounds || SHOT.seconds !== SHOT_MAGAZINE.seconds) out.push(`Magazine recipe: doc says ${SHOT_MAGAZINE.rounds} rounds in ${SHOT_MAGAZINE.seconds} s, code says ${ROUNDS_PER_MAG} in ${SHOT.seconds}`);
  // turret
  if (TURRET_HOPPER !== TURRET.hopper || TURRET_RANGE !== TURRET.range || TURRET_ROUNDS_PER_S !== TURRET.roundsPerS || ROUND_DMG !== TURRET.roundDmg)
    out.push(`Turret: doc says range ${TURRET.range} / ${TURRET.roundsPerS} rounds/s / hopper ${TURRET.hopper} / ${TURRET.roundDmg} HP, code says ${TURRET_RANGE} / ${TURRET_ROUNDS_PER_S} / ${TURRET_HOPPER} / ${ROUND_DMG}`);
  if (cal.hopper !== TURRET.hopper)
    out.push(`Turret hopper: doc says ${TURRET.hopper} rounds a turret, calibration says ${cal.hopper} rounds an edge (the block sim's edge hopper, "two turrets per edge", D-P1-3 rider), code says ${TURRET_HOPPER} a turret`);
  // start chest
  const ss = cal.eco.startStock, calMags = cal.startRounds / ROUNDS_PER_MAG;
  if (ss.steel !== START_CHEST.steel || ss.copper !== START_CHEST.copper || ss.stone !== START_CHEST.stone || calMags !== START_CHEST.magazines)
    out.push(`Start chest: doc says ${START_CHEST.steel} steel / ${START_CHEST.copper} copper / ${START_CHEST.stone} stone / ${START_CHEST.magazines} magazines, calibration says ${ss.steel} / ${ss.copper} / ${ss.stone} / ${calMags} (PROTO_CALIBRATED.eco.startStock, startRounds), code (the game's chest, flow.ts START_CHEST) says the doc's numbers (D-B1-1, D-P4-4)`);
  // substation draw
  const calDraw = cal.draw === 'half' ? `${SUBSTATION_KW.front}/${SUBSTATION_KW.interior}` : cal.draw === 'doc' ? '200/40 (draw "doc", the pre-D1 numbers; power is off in the calibration so nothing draws it)' : '120 flat';
  if (cal.draw !== 'half') out.push(`Substation draw: doc says ${SUBSTATION_KW.front}/${SUBSTATION_KW.interior} kW (D1), calibration says ${calDraw}, code (the tile sessions, ehour, replay) says draw "half" = ${SUBSTATION_KW.front}/${SUBSTATION_KW.interior}`);
  if (FIRST_HOUR_DEFAULTS.subKw !== SUBSTATION_KW.front || FIRST_HOUR_DEFAULTS.interiorKw !== SUBSTATION_KW.interior)
    out.push(`Substation draw: doc says ${SUBSTATION_KW.front}/${SUBSTATION_KW.interior} kW (D1), firsthour.ts FIRST_HOUR_DEFAULTS says ${FIRST_HOUR_DEFAULTS.subKw}/${FIRST_HOUR_DEFAULTS.interiorKw} (the doc-literal E4 variant; FIRST_HOUR_D1 says ${FIRST_HOUR_D1.subKw}/${FIRST_HOUR_D1.interiorKw})`);
  // lamp spacing and turret kits
  if (FACE_LIGHT_STEP !== LAMP_STEP_TILES) out.push(`Streetlight spacing: doc says every ${LAMP_STEP_TILES} tiles, code (ground.ts) says ${FACE_LIGHT_STEP}`);
  if (STREETLIGHT_STEP !== LAMP_STEP_TILES) out.push(`Streetlight spacing: doc says every ${LAMP_STEP_TILES} tiles (D-B1-4), code says ${STREETLIGHT_STEP} in tiles.ts STREETLIGHT_STEP (the lattice's eight a side, superseded by ground.ts FACE_LIGHT_STEP = ${FACE_LIGHT_STEP})`);
  // the hour's minute list
  const gens = HOUR_GENERATOR_MIN.map(m => m * 60);
  if (HOUR_GEN_AT.join() !== gens.join()) out.push(`Hour Generators: doc says ${HOUR_GENERATOR_MIN.join('/')}, hour.ts says ${HOUR_GEN_AT.map(t => t / 60).join('/')}`);
  const fhGens = (FIRST_HOUR_DEFAULTS.gens ?? []).map(t => t / 60);
  if (fhGens.join() !== HOUR_GENERATOR_MIN.join()) out.push(`Hour Generators: doc (§11 / E4-doc) says minutes ${HOUR_GENERATOR_MIN.join('/')}, calibration (firsthour.ts FIRST_HOUR_DEFAULTS.gens, the doc-literal E4 variant) says ${fhGens.join('/')}, code (hour.ts HOUR_GEN_AT) says ${HOUR_GEN_AT.map(t => t / 60).join('/')}`);
  for (const d of ['east', 'west', 'north'] as const) {
    if (HOUR_CLAIM_AT[d] !== HOUR_CLAIM_MIN[d] * 60) out.push(`Hour claim ${d}: doc says minute ${HOUR_CLAIM_MIN[d]}, hour.ts says ${HOUR_CLAIM_AT[d] / 60}`);
    if (FIRST_HOUR_CLAIMS[d] !== HOUR_CLAIM_MIN[d] * 60) out.push(`Hour claim ${d}: doc says minute ${HOUR_CLAIM_MIN[d]}, firsthour.ts says ${FIRST_HOUR_CLAIMS[d] / 60}`);
  }
  // the doc's prose against the source (the sentences the constants table repeats)
  const prose: [RegExp, string][] = [
    [new RegExp(`Assembler makes ${ASSEMBLER_MAG_PER_MIN} mag/min from the ${SHOT_MAGAZINE.seconds} s recipe`), `§12 "the Assembler makes ${ASSEMBLER_MAG_PER_MIN} mag/min from the ${SHOT_MAGAZINE.seconds} s recipe"`],
    [new RegExp(`at ${EXCAVATOR_PER_S}/s`), `§13 Excavator "${EXCAVATOR_PER_S}/s"`],
    [new RegExp(`[|] Belt / Fast belt [|] 1×1 [|] — [|] ${BELT_PER_S}/s`), `§13 belt row "${BELT_PER_S}/s"`],
    [new RegExp(`[|] Inserter [|] 1×1 [|] 10 kW [|] ${INSERTER_PER_S} item/s`), `§13 Inserter row "${INSERTER_PER_S} item/s"`],
    [new RegExp(`range ${TURRET.range}, ${TURRET.roundsPerS} rounds/s, ${TURRET.hopper}-round hopper`), `§13 Gun turret row`],
    [new RegExp(`at ${TURRET.roundDmg} HP a round`), `§7 / changelog "${TURRET.roundDmg} HP a round"`],
    [new RegExp(`one per ${TURRET_PER_TILES} tiles of each of its street segments`), `§13 Gun turret row (D-B1-4)`],
    [new RegExp(`one every ${LAMP_STEP_TILES} tiles along each street segment`), `§13 pre-existing fixtures (D-B1-4)`],
    [new RegExp(`with any dark neighbour [|] ${SUBSTATION_KW.front} kW`), `§5 table front draw`],
    [new RegExp(`interior block [|] ${SUBSTATION_KW.interior} kW`), `§5 table interior draw`],
    [/a brownout is proportional \(D-B3-4/, `§14 Power "a brownout is proportional"`],
    [new RegExp(`${START_CHEST.steel} steel, ${START_CHEST.copper} copper, ${START_CHEST.stone} stone in the chest`), `§11 chest`],
    [new RegExp(`with ${START_CHEST.magazines} magazines in the chest`), `§11 magazines`],
    [new RegExp(`at minute ${HOUR_GENERATOR_MIN[1]} a second Generator`), `§11 second Generator`],
  ];
  for (const [re, what] of prose) if (!has(re)) out.push(`Doc prose: ${what} no longer says what constants.ts says (${re.source})`);
  // §11 gives windows, not minutes, for the claims and the third Generator; the minute list is the bot's (D-B6-2)
  if (has(/You claim east \(residential/) && !has(new RegExp(`claim east[^.]*minute ${HOUR_CLAIM_MIN.east}\b`)))
    out.push(`Hour minute list: doc says east in the 10–30 window, west in the same window and north 30–60 ("about minute 40"), with no minute for the third Generator and the Shot line "by minute 10"; calibration (firsthour.ts) says east ${FIRST_HOUR_CLAIMS.east / 60} / west ${FIRST_HOUR_CLAIMS.west / 60} / north ${FIRST_HOUR_CLAIMS.north / 60}; code (hour.ts) says the same claims, Generators ${HOUR_GEN_AT.map(t => t / 60).join('/')}, the Shot line at ${HOUR_SHOT_LINE_MIN}`);
  return out;
}

const GENERATORS: Record<string, () => string> = { districts: districtsTable, enemies: enemiesTable, recipes: recipesTable, constants: constantsTable };
const RE = /(<!-- docsync:(\w+)[^\n]*-->\n)([\s\S]*?)(<!-- \/docsync:\2 -->)/g;

function render(doc: string): { out: string; changed: string[] } {
  const changed: string[] = [];
  const seen = new Set<string>();
  const out = doc.replace(RE, (_m, open: string, name: string, body: string, close: string) => {
    const gen = GENERATORS[name];
    if (!gen) throw new Error(`docsync: no generator for block "${name}"`);
    seen.add(name);
    const next = gen() + '\n';
    if (next !== body) changed.push(name);
    return open + next + close;
  });
  for (const name of Object.keys(GENERATORS)) if (!seen.has(name)) throw new Error(`docsync: marker <!-- docsync:${name} --> missing from the doc`);
  return { out, changed };
}

const check = process.argv.includes('--check');
const doc = readFileSync(docPath, 'utf8');
const { out, changed } = render(doc);
const bad = disagreements(doc);
if (bad.length) {
  console.error(`docsync: ${bad.length} constant${bad.length === 1 ? '' : 's'} disagree with packages/sim/src/constants.ts (recorded in GUARDRAILS_REPORT.md §5; a DECISIONS.md row settles each):`);
  for (const b of bad) console.error(`  - ${b}`);
}
if (changed.length === 0 && bad.length) {
  console.log('docsync: doc tables match packages/sim');
  if (check) process.exit(1);
} else if (changed.length === 0) {
  console.log('docsync: doc tables match packages/sim');
} else if (check) {
  console.error(`docsync: doc tables differ from packages/sim: ${changed.join(', ')} — run npm run docsync and commit`);
  for (const name of changed) {
    const cur = doc.match(new RegExp(`<!-- docsync:${name}[^\\n]*-->\\n([\\s\\S]*?)<!-- /docsync:${name} -->`))?.[1] ?? '';
    console.error(`--- doc ${name}\n${cur}+++ sim ${name}\n${GENERATORS[name]()}\n`);
  }
  process.exit(1);
} else {
  writeFileSync(docPath, out);
  console.log(`docsync: rewrote ${changed.join(', ')}`);
}
