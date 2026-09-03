/** Regression against the Python sim (export_fixtures.py). For seeds 3, 4, 5, both maps, four policies:
 *  magazines exact, hourly held/front/interior exact, first-interior tick exact, blocks lost exact (with the
 *  lost log), shells exact (the hulk roll is the shared hash; the brief's bar is within 10 %). */
import { test } from 'node:test';
import assert from 'node:assert/strict';
import { readFileSync } from 'node:fs';
import { fileURLToPath } from 'node:url';
import { dirname, join } from 'node:path';
import { createState, configFromPython, step, districtBase, wellInfluence, hash01, pyRound, MapSpec, createBot, botCommands, Command, Policy } from '../src/index';

const here = dirname(fileURLToPath(import.meta.url));
const SEEDS = [3, 4, 5];

interface Fixture {
  seed: number; w: number; h: number; start: [number, number]; target: [number, number]; wells: [number, number][];
  config: Record<string, unknown>; hours: number;
  cells: { x: number; y: number; name: 'civ' | 'res' | 'ind' | 'out'; well: boolean; dmax: number; g: number; d0: number }[];
  scattered_inert: [number, number][];
  runs: Record<string, Record<string, {
    hourly: { h: number; held: number; front: number; interior: number; mags: number; shells: number; mean_awake_rot: number; lost: number }[];
    total_mags: number; total_shells: number; first_interior: number | null; lost: number; retakes: number; claims: number;
    held: number; front: number; interior: number; n_scatter: number;
    lost_log: { t: number; reason: string; x: number; y: number }[];
  }>>;
}

function loadFixture(seed: number): Fixture {
  return JSON.parse(readFileSync(join(here, '..', 'fixtures', `seed${seed}.json`), 'utf8'));
}

function specOf(fx: Fixture): MapSpec {
  return { w: fx.w, h: fx.h, start: fx.start, target: fx.target, wells: fx.wells, cells: fx.cells,
           scatteredInert: fx.scattered_inert, facilities: [] };
}

test('hash01 matches frontsim.hash01', () => {
  const ref: [number, number, number, number, number][] = [
    [3, 0, 0, 0, 0.703531444305554], [3, 900, 12, 19, 0.9080279334448278], [4, 7979, 5, 7, 0.8271070823539048],
    [5, 17999, 23, 20, 0.9644838699605316], [3, 123456, 0, 21, 0.38160651992075145]];
  for (const [s, t, x, y, v] of ref) assert.equal(hash01(s, t, x, y), v);
});

test('pyRound is half-to-even', () => {
  assert.equal(pyRound(2.5), 2); assert.equal(pyRound(3.5), 4); assert.equal(pyRound(4.4999), 4); assert.equal(pyRound(4.5001), 5);
});

test('districtBase() and wellInfluence() match the fixture cells bit for bit (24×22 fixture geometry)', () => {
  const fx = loadFixture(3);
  for (const c of fx.cells) {
    const base = districtBase(c.x, c.y, fx.start, fx.h);
    const infl = wellInfluence(c.x, c.y, fx.wells);
    assert.equal(Math.min(1.0, base.dmax + 0.3 * infl), c.dmax, `dmax at ${c.x},${c.y}`);
    assert.equal(base.g * (1 + 3 * infl), c.g, `g at ${c.x},${c.y}`);
    assert.equal(base.name, c.name, `district at ${c.x},${c.y}`);
    assert.equal(fx.wells.some(([wx, wy]) => Math.abs(c.x - wx) + Math.abs(c.y - wy) <= 3), c.well, `well at ${c.x},${c.y}`);
  }
});

for (const seed of SEEDS) {
  const fx = loadFixture(seed);
  for (const mapName of ['open', 'scattered'] as const) {
    for (const policy of Object.keys(fx.runs[mapName]) as Policy[]) {
      test(`seed ${seed} ${mapName} ${policy}: 5-hour run matches the Python fixture`, () => {
        const py = fx.runs[mapName][policy];
        const cfg = configFromPython(fx.config, { scatter: mapName === 'scattered', economy: false, startAssemblers: 0 });
        const st = createState(specOf(fx), cfg, seed);
        const bot = createBot(policy);
        const cmds: Command[] = [];
        const ticks = fx.hours * 3600;
        for (let k = 0; k < ticks; k++) {
          cmds.length = 0;
          botCommands(st, bot, cmds);
          step(st, cmds);
          st.events.length = 0;
        }
        assert.equal(st.t, ticks);
        assert.equal(st.hourly.length, py.hourly.length, 'hourly rows');
        for (let h = 0; h < py.hourly.length; h++) {
          const a = st.hourly[h], b = py.hourly[h];
          assert.equal(a.h, b.h);
          assert.equal(a.held, b.held, `h${b.h} held`);
          assert.equal(a.front, b.front, `h${b.h} front`);
          assert.equal(a.interior, b.interior, `h${b.h} interior`);
          assert.equal(a.mags, b.mags, `h${b.h} mags`);
          assert.equal(a.lost, b.lost, `h${b.h} lost`);
          assert.equal(a.shells, b.shells, `h${b.h} shells`);
          assert.ok(Math.abs(a.meanAwakeRot - b.mean_awake_rot) < 1e-9, `h${b.h} mean awake rot ${a.meanAwakeRot} vs ${b.mean_awake_rot}`);
        }
        assert.equal(st.totalRounds / 10, py.total_mags, 'total magazines');
        assert.equal(st.stats.firstInterior, py.first_interior === null ? -1 : py.first_interior, 'first interior');
        assert.equal(st.stats.lost, py.lost, 'blocks lost');
        assert.deepEqual(st.stats.lostLog, py.lost_log, 'lost log');
        assert.equal(st.stats.retakes, py.retakes, 'retakes');
        assert.equal(st.stats.claims, py.claims, 'claims');
        assert.equal(st.stats.lost, py.lost, 'lost');
        // shells: the brief asks for within 10 %; with the shared hash roll they are exact
        assert.ok(Math.abs(st.totalShells - py.total_shells) <= 0.1 * Math.max(1, py.total_shells), `shells ${st.totalShells} vs ${py.total_shells}`);
        assert.equal(st.totalShells, py.total_shells, 'total shells (exact with the hash roll)');
      });
    }
  }
}

test('state round-trips through JSON and continues identically', () => {
  const fx = loadFixture(3);
  const cfg = configFromPython(fx.config, { scatter: true });
  const a = createState(specOf(fx), cfg, 3);
  const bot = createBot('compact');
  const cmds: Command[] = [];
  for (let k = 0; k < 4000; k++) { cmds.length = 0; botCommands(a, bot, cmds); step(a, cmds); a.events.length = 0; }
  const b = JSON.parse(JSON.stringify(a));
  const botB = { ...bot };
  for (let k = 0; k < 4000; k++) {
    cmds.length = 0; botCommands(a, bot, cmds); step(a, cmds); a.events.length = 0;
    cmds.length = 0; botCommands(b, botB, cmds); step(b, cmds); b.events.length = 0;
  }
  assert.equal(JSON.stringify(a), JSON.stringify(b));
});
