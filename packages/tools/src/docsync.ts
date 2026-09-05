/**
 * docsync — the §7 district and enemy tables, the §12 recipe and Assembler tables, the §11 minute table and the §13
 * machines and constants tables in docs/RELIGHT-design.md are generated from packages/sim (districts.ts, enemies.ts,
 * recipes.ts, flow.ts, constants.ts) plus the E3-block steady state in docs/experiments/E3.json. `npm run docsync` rewrites them;
 * `npm run docsync:check` (CI) fails if the doc differs from what the code says, or if the calibration config, the
 * block sim, firsthour.ts, hour.ts or the doc's prose carry a number that differs from constants.ts (the
 * `disagreements` list). Edit the .ts files, never the tables. PROTO_CALIBRATED (types.ts) is prototype-era, frozen
 * at Gate A, and is not compared (D-B1-1). RI-01 adds the `[play: <gate>]` check: every play tag in the doc names a
 * gate whose record exists (PLAY_GATES); a placeholder or an unknown gate is red (constitution rule 11).
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
  SHOT_MAGAZINE, SHOT_MAGAZINE_MK2_SECONDS, ASSEMBLER_TIERS, ASSEMBLER_MAG_PER_MIN, ASSEMBLER_MK1_MAG_PER_MIN, START_TURRETS, STREETLIGHT_RADIUS, EXCAVATOR_PER_S, BELT_PER_S, FAST_BELT_PER_S, INSERTER_PER_S, TURRET,
  TURRET_PER_TILES, LAMP_STEP_TILES, SUBSTATION_KW, BROWNOUT_RULE, START_CHEST, HOUR, HOUR_GENERATOR_MIN, HOUR_CLAIM_MIN, EDGE_TURRETS,
  DEFAULT_CONFIG, protoCalibrated, ROUNDS_PER_MAG, TURRET_HOPPER, TURRET_RANGE, TURRET_ROUNDS_PER_S, ROUND_DMG,
  FACE_LIGHT_STEP, STREETLIGHT_STEP, FIRST_HOUR_DEFAULTS, FIRST_HOUR_CLAIMS, HOUR_GEN_AT, HOUR_CLAIM_AT, SHOT, MACHINE_RECIPES,
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

/** RI-01 (D-B2-1 (b)): the §13 machine → recipe table, from flow.ts MACHINE_RECIPES — which machine makes what in
 *  the build, and which recipes are still data. */
function machinesTable(): string {
  const lines = ['| Machine | Makes | In the build |', '|---|---|---|'];
  for (const m of MACHINE_RECIPES) lines.push(`| ${m.machine} | ${m.recipes} | ${m.state} |`);
  return lines.join('\n');
}

/** RI-01: the gates a `[play: <gate>]` tag may name, each with the record that holds its observations (constitution
 *  rule 11: Gate A's tags are approval locks, Gate B's support the sentence they sit on). A new played session gets a
 *  row here when its record is written; until then its tag is red. */
const PLAY_GATES: Record<string, string> = { 'Gate A': 'docs/TEST_RESULTS.md', 'Gate B': 'docs/GATE_B.md' };
function playTags(doc: string): string[] {
  const out: string[] = [];
  const re = /(`?)\[play: ([^\]]*)\]/g;
  let m: RegExpExecArray | null;
  while ((m = re.exec(doc))) {
    if (m[1] === '`') continue; // a quoted mention of the tag form, not a tag
    const gate = m[2].trim(), line = doc.slice(0, m.index).split('\n').length;
    const rec = PLAY_GATES[gate];
    if (!gate || gate === '…' || gate === '...' || /^<.*>$/.test(gate)) out.push(`line ${line}: [play: ${m[2]}] is a placeholder — name the gate whose record holds the observation`);
    else if (!rec) out.push(`line ${line}: [play: ${gate}] names no gate in PLAY_GATES (${Object.keys(PLAY_GATES).join(', ')})`);
    else if (!existsSync(join(root, rec))) out.push(`line ${line}: [play: ${gate}]'s record ${rec} is missing`);
  }
  return out;
}

/** D-P4-4 (economy-fix task Step 2, item 1): the §12 Assembler ladder, one row a tier, from constants.ts. */
function assemblersTable(): string {
  const lines = ['| Assembler | Magazines a minute | Seconds a magazine | Decision |', '|---|---|---|---|'];
  for (const t of ASSEMBLER_TIERS) {
    const seconds = t.name === 'Mk1' ? SHOT_MAGAZINE.seconds : SHOT_MAGAZINE_MK2_SECONDS;
    lines.push(`| ${t.name} | ${t.magPerMin} | ${seconds} s | D-P4-4 |`);
  }
  return lines.join('\n');
}

/** D-HOUR-1 (economy-fix task Step 2, item 8): the §11 minute table, from constants.ts HOUR. The table is the rule;
 *  the prose around it stays prose. */
function hourTable(): string {
  const lines = ['| Minute | What | Decided by |', '|---|---|---|'];
  for (const h of HOUR) lines.push(`| ${h.min} | ${h.what} | ${h.doc} |`);
  return lines.join('\n');
}

/** Guardrails Step 5: the doc's table of the constants the sim is built from, generated from constants.ts. */
function constantsTable(): string {
  const lines = ['| Constant | Value | Where the doc says it | Decision |', '|---|---|---|---|'];
  for (const t of ASSEMBLER_TIERS) lines.push(`| Assembler rate (${t.name}) | ${t.magPerMin} mag/min | §12 ammo chain, §13 Assembler row | D-P4-4, D-B1-1 (C9, D-P3-7 superseded) |`);
  lines.push(`| Shot magazine | ${SHOT_MAGAZINE.steel} steel + ${SHOT_MAGAZINE.copper} Cu → ${SHOT_MAGAZINE.rounds} rounds in ${SHOT_MAGAZINE.seconds} s (Mk1) · ${SHOT_MAGAZINE_MK2_SECONDS} s (Mk2) | §12 recipe table | D-P4-4 |`);
  lines.push(`| Excavator rate | ${EXCAVATOR_PER_S}/s | §13 Excavator row | [sim: M2-rates] |`);
  lines.push(`| Belt / fast belt | ${BELT_PER_S}/s · ${FAST_BELT_PER_S}/s | §13 belt row, §14 | D-P4-6 |`);
  lines.push(`| Inserter | ${INSERTER_PER_S} item/s | §13 Inserter row, §14 | [sim: M2-rates] |`);
  lines.push(`| Gun turret | range ${TURRET.range}, ${TURRET.roundsPerS} rounds/s, ${TURRET.hopper}-round hopper, ${TURRET.roundDmg} HP a round | §13 Gun turret row, §7 | [sim: M3-rates, B-M4-threat] |`);
  lines.push(`| Turret kits per segment | one per ${TURRET_PER_TILES} tiles of segment length, at least one | §13 pre-existing fixtures, §14 | D-B1-4 |`);
  lines.push(`| Start turrets | ${START_TURRETS}, hoppers full, spread over the HQ's segments by length | §11 0–10 min, §13 pre-existing fixtures | D-P4-8 |`);
  lines.push(`| Streetlight radius | ${STREETLIGHT_RADIUS} tiles, to the street midline (the Lamp keeps 4) | §13 pre-existing fixtures | D-B5-4 |`);
  lines.push(`| Streetlight spacing | one every ${LAMP_STEP_TILES} tiles along the kerb | §13 pre-existing fixtures | D-B1-4 |`);
  lines.push(`| Substation draw | ${SUBSTATION_KW.front} kW front · ${SUBSTATION_KW.interior} kW interior | §5 table | D1 |`);
  lines.push(`| Brownout rule | ${BROWNOUT_RULE}: every machine runs at supply ÷ demand, nothing sheds | §14 Power | D-B3-4 |`);
  lines.push(`| Start chest | ${START_CHEST.steel} steel, ${START_CHEST.copper} copper, ${START_CHEST.stone} stone, ${START_CHEST.coal} coal, ${START_CHEST.magazines} magazines | §11 0–10 min | C10, D-P2-1, D-B1-1, D-P4-4, D-P4-7, D-P4-8 |`);
  lines.push(`| Block-sim edge hopper | ${EDGE_TURRETS} turrets on the segment × ${TURRET.hopper} = ${EDGE_TURRETS * TURRET.hopper} rounds (computed, not a constant) | §13 Gun turret row | D-B1-4-rider, D-P1-3 rider |`);
  lines.push(`| §11 minute list | the generated §11 table (constants.ts HOUR, ${HOUR.length} rows) | §11 | D-HOUR-1 |`);
  return lines.join('\n');
}

/** Guardrails Step 5: where the calibration config, the block sim, firsthour.ts, hour.ts or the doc's prose carry a
 *  number that differs from constants.ts. Each line is "Constant X: doc says A, calibration says B, code says C".
 *  Nothing here is fixed by docsync: the list is the report's §5 and a decision row's evidence. PROTO_CALIBRATED's
 *  start stock and rounds are not compared (D-B1-1: prototype-era, frozen at Gate A). */
function disagreements(doc: string): string[] {
  const out: string[] = [];
  const cal = protoCalibrated(DEFAULT_CONFIG);
  const has = (re: RegExp) => re.test(doc);
  // assembler rate by tier
  // the Mk1 / Mk2 ladder (D-P4-4, D-B1-1): the calibration's start "Mk1" is the doc's Mk1, its asmRate the Mk2
  if (cal.startAsmRate !== null && cal.startAsmRate !== ASSEMBLER_MK1_MAG_PER_MIN)
    out.push(`Assembler rate (Mk1): doc says ${ASSEMBLER_MK1_MAG_PER_MIN} mag/min, calibration says the start "Mk1" makes ${cal.startAsmRate} mag/min (PROTO_CALIBRATED.startAsmRate), code says ${60 / SHOT.seconds} mag/min (the tile assembler's ${SHOT.seconds} s recipe)`);
  if (60 / SHOT.seconds !== ASSEMBLER_MK1_MAG_PER_MIN) out.push(`Assembler rate (Mk1): doc says ${ASSEMBLER_MK1_MAG_PER_MIN} mag/min, code (the tile assembler's Shot recipe) says ${60 / SHOT.seconds}`);
  if (cal.asmRate !== ASSEMBLER_MAG_PER_MIN) out.push(`Assembler rate (Mk2): doc says ${ASSEMBLER_MAG_PER_MIN}, calibration asmRate (the block sim's one rate) says ${cal.asmRate}`);
  // magazine recipe
  const mc = cal.eco.magazineCost;
  if (mc.steel !== SHOT_MAGAZINE.steel || mc.copper !== SHOT_MAGAZINE.copper) out.push(`Magazine recipe: doc says ${SHOT_MAGAZINE.steel} steel + ${SHOT_MAGAZINE.copper} Cu, calibration says ${mc.steel} + ${mc.copper}`);
  if (ROUNDS_PER_MAG !== SHOT_MAGAZINE.rounds || SHOT.seconds !== SHOT_MAGAZINE.seconds) out.push(`Magazine recipe: doc says ${SHOT_MAGAZINE.rounds} rounds in ${SHOT_MAGAZINE.seconds} s, code says ${ROUNDS_PER_MAG} in ${SHOT.seconds}`);
  // turret
  if (TURRET_HOPPER !== TURRET.hopper || TURRET_RANGE !== TURRET.range || TURRET_ROUNDS_PER_S !== TURRET.roundsPerS || ROUND_DMG !== TURRET.roundDmg)
    out.push(`Turret: doc says range ${TURRET.range} / ${TURRET.roundsPerS} rounds/s / hopper ${TURRET.hopper} / ${TURRET.roundDmg} HP, code says ${TURRET_RANGE} / ${TURRET_ROUNDS_PER_S} / ${TURRET_HOPPER} / ${ROUND_DMG}`);
  // D-B1-4-rider: the block sim's edge hopper is the turrets on the segment × the turret hopper, not a constant
  const edgeHopper = EDGE_TURRETS * TURRET.hopper;
  if (cal.hopper !== edgeHopper)
    out.push(`Edge hopper: doc says ${EDGE_TURRETS} turrets × ${TURRET.hopper} = ${edgeHopper} rounds an edge (D-B1-4-rider), calibration (the block sim's hopper) says ${cal.hopper}`);
  // start chest: constants.ts START_CHEST is the tile chest (D-B1-1); PROTO_CALIBRATED's start stock is frozen and not compared
  // substation draw
  const calDraw = cal.draw === 'half' ? `${SUBSTATION_KW.front}/${SUBSTATION_KW.interior}` : cal.draw === 'doc' ? '200/40 (draw "doc", the pre-D1 numbers; power is off in the calibration so nothing draws it)' : '120 flat';
  if (cal.draw !== 'half') out.push(`Substation draw: doc says ${SUBSTATION_KW.front}/${SUBSTATION_KW.interior} kW (D1), calibration says ${calDraw}, code (the tile sessions, ehour, replay) says draw "half" = ${SUBSTATION_KW.front}/${SUBSTATION_KW.interior}`);
  if (FIRST_HOUR_DEFAULTS.subKw !== SUBSTATION_KW.front || FIRST_HOUR_DEFAULTS.interiorKw !== SUBSTATION_KW.interior)
    out.push(`Substation draw: doc says ${SUBSTATION_KW.front}/${SUBSTATION_KW.interior} kW (D1), firsthour.ts FIRST_HOUR_DEFAULTS says ${FIRST_HOUR_DEFAULTS.subKw}/${FIRST_HOUR_DEFAULTS.interiorKw} (the pre-D1 200/40 belongs in FIRST_HOUR_PRE_D1 only)`);
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
    [new RegExp(`One Mk1 Assembler = ${ASSEMBLER_MK1_MAG_PER_MIN} magazines/min \\(${SHOT_MAGAZINE.seconds} s a magazine\\)`), `§12 "One Mk1 Assembler = ${ASSEMBLER_MK1_MAG_PER_MIN} magazines/min (${SHOT_MAGAZINE.seconds} s a magazine)"`],
    [new RegExp(`the Mk2 makes it in ${SHOT_MAGAZINE_MK2_SECONDS} s, ${ASSEMBLER_MAG_PER_MIN} mag/min`), `§13 Assembler row "the Mk2 makes it in ${SHOT_MAGAZINE_MK2_SECONDS} s, ${ASSEMBLER_MAG_PER_MIN} mag/min"`],
    [new RegExp(`${['zero','one','two','three','four','five','six','seven','eight'][START_TURRETS] ?? START_TURRETS} Gun turrets`), `§11 "six Gun turrets" (D-P4-8; the word, START_TURRETS = ${START_TURRETS})`],
    [new RegExp(`${STREETLIGHT_RADIUS}-tile radius`), `§13 streetlight "${STREETLIGHT_RADIUS}-tile radius" (D-B5-4)`],
    [new RegExp(`${START_CHEST.coal} more in the chest`), `§11 chest coal (D-P4-7)`],
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
    [new RegExp(`and ${START_CHEST.magazines} magazines in the chest`), `§11 magazines`],
    [new RegExp(`at minute ${HOUR_GENERATOR_MIN[1]} a second Generator`), `§11 second Generator`],
  ];
  for (const [re, what] of prose) if (!has(re)) out.push(`Doc prose: ${what} no longer says what constants.ts says (${re.source})`);
  // §11's prose names the east and west claim minutes ("at about minute 15 / 25"); the minute table (generated from
  // constants.HOUR, D-HOUR-1) is the rule for every other minute, so no other prose minute is checked here
  if (!has(new RegExp(`claim east[^\\n]*?about minute ${HOUR_CLAIM_MIN.east}\\b`))) out.push(`Doc prose: §11 east claim "about minute ${HOUR_CLAIM_MIN.east}" no longer says what constants.ts says`);
  if (!has(new RegExp(`claim west[^\\n]*?about minute ${HOUR_CLAIM_MIN.west}\\b`))) out.push(`Doc prose: §11 west claim "about minute ${HOUR_CLAIM_MIN.west}" no longer says what constants.ts says`);
  return out;
}

const GENERATORS: Record<string, () => string> = { districts: districtsTable, enemies: enemiesTable, recipes: recipesTable, assemblers: assemblersTable, machines: machinesTable, hour: hourTable, constants: constantsTable };
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
const tags = playTags(doc);
if (tags.length) {
  console.error(`docsync: ${tags.length} [play: <gate>] tag${tags.length === 1 ? '' : 's'} name no recorded gate (RI-01 check; constitution rule 11):`);
  for (const t of tags) console.error(`  - ${t}`);
  bad.push(...tags);
} else console.log(`docsync: every [play: <gate>] tag names a recorded gate (${Object.keys(PLAY_GATES).join(', ')})`);
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
