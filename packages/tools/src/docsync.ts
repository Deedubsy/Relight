/**
 * docsync — the §7 district and enemy tables, the §12 recipe table and the §18 map-view drawings in
 * docs/RELIGHT-design.md are generated from packages/sim (districts.ts, enemies.ts, recipes.ts, queries.ts renderMap)
 * plus the E3-block steady state in docs/experiments/E3.json. `npm run docsync` rewrites them; `npm run docsync:check`
 * (CI) fails if the doc differs from what the code says. Edit the .ts files, never the tables or the drawings.
 *
 * §18 is drawn from the compact bot on seed 3 at the locked cadence (C2: one claim per 15 min in hour one, then one per
 * 5 min; D-P3-6), production off — the E8-cadence run — at 0:10, 5:00 and 25:00. The counts in each drawing's header
 * are the sim's own; the three-seed means are E8's.
 */
import { readFileSync, writeFileSync, existsSync } from 'node:fs';
import { join, dirname } from 'node:path';
import { fileURLToPath } from 'node:url';
import {
  DISTRICTS, RAIL_YARD, ENEMIES, RECIPES, WELL_RANGE, WELL_CAP_BONUS, WELL_G_MULT,
  DEFAULT_CONFIG, SimConfig, generateMap, createState, step, createBot, botCommands, Command, renderMap, heldCount, frontage, interior, HELD,
  protoCalibrated, ensureFlow, place, canPlace, advanceFlow, renderLot, flowSummary, LOT_LEGEND, CELL_TILES, MARGIN_TILES, Kind, Dir,
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

/** §18 drawings: compact bot, seed 3, §18 cadence (gapAfter 300 s), production off (E8-cadence), 25 h. */
export const SECTION18_SEED = 3, SECTION18_GAP = 300;
const SECTION18_AT: [number, string][] = [[600, '10 minutes'], [5 * 3600, '5 hours'], [25 * 3600, '25 hours']];
function section18(): string {
  const cfg: SimConfig = { ...DEFAULT_CONFIG, production: false, eco: { ...DEFAULT_CONFIG.eco } };
  const st = createState(generateMap(SECTION18_SEED, cfg), cfg, SECTION18_SEED);
  const bot = createBot('compact', null, false, SECTION18_GAP);
  const cmds: Command[] = [];
  const out: string[] = [];
  const last = SECTION18_AT[SECTION18_AT.length - 1][0];
  for (let k = 0; k < last; k++) {
    cmds.length = 0; botCommands(st, bot, cmds); step(st, cmds);
    const at = SECTION18_AT.find(a => a[0] === st.t);
    if (!at) continue;
    const wells = st.wells.filter(([x, y], i) => st.wellDead[i] || st.blocks[x * st.h + y].state === HELD).length;
    out.push(`**${at[1]} (map view, seed ${SECTION18_SEED}, compact bot at the §18 cadence, no ammo line; held ${heldCount(st)}, front ${frontage(st)}, interior ${interior(st)}, lost ${st.stats.lost}, wells neutralised ${wells} of ${st.wells.length}) [sim: E8-cadence].**`, '', '```', renderMap(st), '```', '');
  }
  return out.join('\n').trimEnd();
}

/** §18's 10-minute world-view drawing of the HQ lot (M3): the game's session config (protoCalibrated, the §14 power
 *  model on Generators at the half draw, machines-first shed order), seed 3, no bot, and §11's first ten minutes placed
 *  by hand at the doc's stock — two Excavators on steel → belt → Shot assembler ← copper Excavator by belt, its
 *  magazines by belt and inserter into the north-west turret's hopper, and at minute 6 a second Generator with an
 *  Excavator on the coal patch feeding both by belt. §11's stock (200 steel / 100 copper) is used because the sim's
 *  80 / 40 (D-P4-4) does not buy the line the doc describes; §11 is rewritten at M6. Run name M3-rates. */
const SECTION18_LOT_STOCK = { steel: 200, copper: 100 };
function section18lot(): string {
  const cfg: SimConfig = { ...protoCalibrated({ ...DEFAULT_CONFIG, scatter: true, economy: true }), power: true, supply: 'generators', draw: 'half', shed: 'machines-first' };
  const st = createState(generateMap(SECTION18_SEED, cfg), cfg, SECTION18_SEED);
  ensureFlow(st);
  st.stock.steel = SECTION18_LOT_STOCK.steel; st.stock.copper = SECTION18_LOT_STOCK.copper;
  const lot = (lx: number, ly: number): [number, number] => [st.start[0] * CELL_TILES + MARGIN_TILES + lx, st.start[1] * CELL_TILES + MARGIN_TILES + ly];
  const put = (kind: Kind, lx: number, ly: number, dir: Dir = 0) => {
    if (!place(st, kind, ...lot(lx, ly), dir)) throw new Error(`docsync §18 lot: ${kind} at (${lx},${ly}): ${canPlace(st, kind, ...lot(lx, ly)).reason}`);
  };
  const belts = (kind: Kind, from: [number, number], to: [number, number], dir: Dir) => {
    for (let lx = Math.min(from[0], to[0]); lx <= Math.max(from[0], to[0]); lx++) for (let ly = Math.min(from[1], to[1]); ly <= Math.max(from[1], to[1]); ly++) put(kind, lx, ly, dir);
  };
  // 0:00 — steel: two Excavators on the patch (lot 1..5 × 7..11) facing north onto a belt east along row 6 into the assembler at 9..11 × 5..7
  put('excavator', 1, 7, 0); put('excavator', 4, 7, 0);
  belts('belt', [2, 6], [7, 6], 1); put('inserter', 8, 6, 1);
  put('assembler', 9, 5, 0);
  // magazines: out of the assembler's north face, west along row 3 and up into the north-west turret's hopper (lot 3..4 × 0..1)
  put('inserter', 10, 4, 0); belts('belt', [4, 3], [10, 3], 3); put('inserter', 4, 2, 0);
  // copper: an Excavator on the copper patch (1..4 × 14..16, at lot 2 to clear the west turret) facing east, belt east along row 15, north up column 15, west along row 6 to the assembler's east face
  put('excavator', 2, 14, 1); belts('belt', [5, 15], [14, 15], 1); belts('belt', [15, 7], [15, 15], 0); belts('belt', [13, 6], [15, 6], 3); put('inserter', 12, 6, 3);
  const run = (seconds: number) => { for (let k = 0; k < seconds * 10; k++) advanceFlow(st, 0.1); };
  run(6 * 60);
  // 6:00 — coal: an Excavator on the coal patch (18..20 × 14..16) facing north, belt up column 19 and west along row 11; inserters into both Generators (the start's at 19..20 × 8..9, the second at 17..18 × 8..9)
  put('generator', 17, 8, 0);
  put('excavator', 18, 14, 0); belts('belt', [19, 12], [19, 13], 0); belts('belt', [18, 11], [19, 11], 3); put('inserter', 19, 10, 0); put('inserter', 18, 10, 0);
  run(4 * 60);
  const s = flowSummary(st);
  const head = `**10 minutes, world view of the HQ lot (seed ${SECTION18_SEED}, the game's M3 session config, §11's line placed by hand at the doc's ${SECTION18_LOT_STOCK.steel} steel / ${SECTION18_LOT_STOCK.copper} copper; ` +
    `turret hoppers ${Math.round(s.turretRounds)}/${s.turretCap} rounds, ${Math.round(s.fired)} fired, ${s.beltAmmo} magazine${s.beltAmmo === 1 ? '' : 's'} on the belt; magazines made ${s.magsMade}; ` +
    `power ${Math.round(s.loadKw)} / ${Math.round(s.supplyKw)} kW (demand ${Math.round(s.demandKw)}), brownout ${Math.round(s.brownoutS)} s, ${s.shedMachines} shed; ` +
    `Generators ${s.generatorsBurning}/${s.generators} burning on ${Math.round(s.genCoal)} coal, ${Math.round(s.coalBurned)} burned; stock ${Math.round(st.stock.steel)} steel / ${Math.round(st.stock.copper)} copper) [sim: M3-rates].**`;
  return [head, '', `Legend: ${LOT_LEGEND}`, '', '```', renderLot(st), '```'].join('\n');
}

const GENERATORS: Record<string, () => string> = { districts: districtsTable, enemies: enemiesTable, recipes: recipesTable, section18, section18lot };
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
if (changed.length === 0) {
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
