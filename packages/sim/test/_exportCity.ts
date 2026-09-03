/** Writes packages/sim/fixtures/city{3,4,5}.json: the street-first city's generator summary and 5 h runs on four
 *  policies (walk off) plus compact with the engineer walking (D5). `npx tsx packages/sim/test/_exportCity.ts`.
 *  These are the D6 regression fixtures; a change that moves them is a rules change (constitution rule 1). */
import { writeFileSync } from 'node:fs';
import { fileURLToPath } from 'node:url';
import { dirname, join } from 'node:path';
import { Policy } from '../src/index';
import { cityStats, runCity } from './_city';

const here = dirname(fileURLToPath(import.meta.url));
const HOURS = 5;
for (const seed of [3, 4, 5]) {
  const t0 = Date.now();
  const stats = cityStats(seed, 'river');
  const runs: Record<string, unknown> = {};
  for (const policy of ['compact', 'balanced', 'spike', 'cheapest'] as Policy[]) runs[policy] = runCity(seed, 'river', policy, HOURS);
  runs['compact-walk'] = runCity(seed, 'river', 'compact', HOURS, true);
  const out = { seed, preset: 'river', hours: HOURS, stats, runs };
  const path = join(here, '..', 'fixtures', `city${seed}.json`);
  writeFileSync(path, JSON.stringify(out, null, 1) + '\n');
  console.log(`${path}: ${stats.blocks} blocks, ${stats.segs} segs, ${((Date.now() - t0) / 1000).toFixed(1)} s`);
}
