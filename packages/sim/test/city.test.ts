/** D6 regression: the street-first city generator and the sim on it, against fixtures/city{3,4,5}.json
 *  (written by _exportCity.ts). Generator summary exact; each 5 h run exact (magazines, hourly rows, lost log). */
import { test } from 'node:test';
import assert from 'node:assert/strict';
import { readFileSync } from 'node:fs';
import { fileURLToPath } from 'node:url';
import { dirname, join } from 'node:path';
import { Policy, generateCity, CITY_PRESETS, frontTiles, LAND, STREET } from '../src/index';
import { cityStats, runCity, CityRun, CityStats } from './_city';

const here = dirname(fileURLToPath(import.meta.url));
interface Fixture { seed: number; preset: 'river'; hours: number; stats: CityStats; runs: Record<string, CityRun> }
const load = (seed: number): Fixture => JSON.parse(readFileSync(join(here, '..', 'fixtures', `city${seed}.json`), 'utf8'));

test('every preset validates on seeds 3–5 within the attempt budget', () => {
  for (const preset of CITY_PRESETS) for (const seed of [3, 4, 5]) {
    const g = generateCity(seed, preset);
    assert.ok(g.valid, `${preset} seed ${seed}: ${g.reasons.join('; ')}`);
  }
});

test('geometry invariants: lots are land, fronts face a street, neighbours are symmetric, segments match adjacency', () => {
  const g = generateCity(3, 'river');
  const n = g.blocks.length;
  for (const b of g.blocks) {
    for (const t of b.tiles) assert.equal(g.kind[t], LAND, `block ${b.id} tile ${t} is land`);
    for (const j of b.nb) {
      assert.ok(g.blocks[j].nb.includes(b.id), `neighbour ${b.id}–${j} symmetric`);
      const ft = frontTiles(g, b.id, j);
      assert.ok(ft.length > 0, `edge ${b.id}→${j} has front tiles`);
      for (const t of ft) {
        assert.equal(g.owner[t], b.id, `front tile ${t} belongs to ${b.id}`);
        const x = t % g.tw, y = (t - x) / g.tw;
        const touches = [[1, 0], [-1, 0], [0, 1], [0, -1]].some(([dx, dy]) => g.kind[(y + dy) * g.tw + x + dx] === STREET);
        assert.ok(touches, `front tile ${t} touches a street`);
      }
    }
  }
  assert.equal(g.segs.length, g.blocks.reduce((a, b) => a + b.nb.length, 0) / 2, 'one segment per adjacency');
  assert.ok(g.segAt.size === g.segs.length && n > 0, 'segment index complete');
});

for (const seed of [3, 4, 5]) {
  const fx = load(seed);
  test(`city seed ${seed}: generator summary matches the fixture`, () => {
    assert.deepEqual(cityStats(seed, 'river'), fx.stats);
  });
  for (const key of Object.keys(fx.runs)) {
    test(`city seed ${seed} ${key}: 5-hour run matches the fixture`, () => {
      const policy = key.replace('-walk', '') as Policy;
      const r = runCity(seed, 'river', policy, fx.hours, key.endsWith('-walk'));
      assert.deepEqual(r, fx.runs[key]);
    });
  }
}
