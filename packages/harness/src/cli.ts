/** `npm run experiments [-- --only E2,E4 --seeds 3,4,5 --hours 5 --map river|lattice --big --out docs]`: runs the suite, writes docs/EXPERIMENTS.md and
 *  docs/experiments/E<n>.json, exits 1 if any check fails (red experiment = red build). `--nightly` runs the long suite. */
import { writeFileSync, mkdirSync, readFileSync } from 'node:fs';
import { join, dirname } from 'node:path';
import { fileURLToPath } from 'node:url';
import { EXPERIMENTS } from './experiments/index';
import { toMarkdown } from './report';
import { ExperimentResult } from './util';
import { nightly } from './nightly';
import { setDefaultMap, DEFAULT_MAP } from './run';
import { CityPreset } from '@relight/sim';

const here = dirname(fileURLToPath(import.meta.url));
const root = join(here, '..', '..', '..');
const args = process.argv.slice(2);
const opt = (name: string, def: string): string => { const i = args.indexOf(`--${name}`); return i >= 0 && args[i + 1] ? args[i + 1] : def; };
const seeds = opt('seeds', '3,4,5').split(',').map(Number);
const hours = Number(opt('hours', '5'));
const only = opt('only', '').split(',').filter(Boolean);
const outDir = join(root, opt('out', 'docs'));
const map = opt('map', DEFAULT_MAP) as 'lattice' | CityPreset;   // D6: the street-first city by default; --map lattice for the archived grid
const big = process.argv.includes('--big');                     // full-size E-variance (10,000 seeds) instead of the quick 200
setDefaultMap(map);

if (args.includes('--nightly')) {
  nightly({ seedsN: Number(opt('seeds-n', '10000')), outDir: join(outDir, 'experiments'), log: s => console.log(s) });
} else {
  const t0 = Date.now();
  const results: ExperimentResult[] = [];
  for (const e of EXPERIMENTS) {
    if (only.length && !only.includes(e.id)) continue;
    const t1 = Date.now();
    const r = e.run({ seeds, hours, log: s => console.log(`  ${s}`), nightly: false, map, big });
    results.push(r);
    console.log(`${r.id} ${r.title}: ${r.checks.filter(c => c.pass).length}/${r.checks.length} checks, ${((Date.now() - t1) / 1000).toFixed(1)} s`);
    for (const c of r.checks) if (!c.pass) console.log(`  ❌ ${c.name} — ${c.detail}`);
  }
  const seconds = (Date.now() - t0) / 1000;
  const simVersion = (JSON.parse(readFileSync(join(root, 'packages', 'sim', 'package.json'), 'utf8')) as { version: string }).version;
  mkdirSync(join(outDir, 'experiments'), { recursive: true });
  for (const r of results) writeFileSync(join(outDir, 'experiments', `${r.id}.json`), JSON.stringify({ ...r, meta: { seeds, hours, map } }, null, 1));
  if (!only.length) writeFileSync(join(outDir, 'EXPERIMENTS.md'), toMarkdown(results, { seeds, hours, map, date: new Date().toISOString().slice(0, 10), seconds, simVersion }));
  const failing = results.flatMap(r => r.checks.filter(c => !c.pass).map(c => `${r.id}: ${c.name}`));
  console.log(`${results.length} experiments, ${seconds.toFixed(0)} s, ${failing.length} failing check(s)`);
  if (failing.length) { for (const f of failing) console.log(`  ${f}`); process.exit(1); }
}
