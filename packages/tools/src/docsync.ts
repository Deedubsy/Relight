/**
 * docsync — the §7 district and enemy tables and the §12 recipe table in docs/RELIGHT-design.md are
 * generated from packages/sim (districts.ts, enemies.ts, recipes.ts) plus the E3-block steady state in
 * docs/experiments/E3.json. `npm run docsync` rewrites them; `npm run docsync:check` (CI) fails if the
 * doc differs from what the code says. Edit the .ts files, never the tables.
 */
import { readFileSync, writeFileSync, existsSync } from 'node:fs';
import { join, dirname } from 'node:path';
import { fileURLToPath } from 'node:url';
import { DISTRICTS, RAIL_YARD, ENEMIES, RECIPES, WELL_RANGE, WELL_CAP_BONUS, WELL_G_MULT } from '@relight/sim';

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

const GENERATORS: Record<string, () => string> = { districts: districtsTable, enemies: enemiesTable, recipes: recipesTable };
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
