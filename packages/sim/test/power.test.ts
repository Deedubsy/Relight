/** Power-model parity with frontsim.py (export_power_fixtures.py): seeds 3, 4, 5, scattered map, compact policy,
 *  five variants (draw × shed order × supply). D-B3-4 (2026-09-04) replaced the shed order with the proportional
 *  brownout: nothing is switched off by power, so the Python runs, which shed a substation 20 s into every
 *  shortfall, diverge from the sim at their first shed. What still pins bit for bit: the per-minute demand and supply
 *  up to the minute of the fixture's first shed (the draw model, the track supply, the shortfall window) and the tick
 *  of the first brownout. After it the sim must lose no block to power at all (the only fall reasons left are
 *  unfed, shade and starved, and these runs have the unfed rule off), and never more than the Python run did. */
import { test } from 'node:test';
import assert from 'node:assert/strict';
import { readFileSync } from 'node:fs';
import { fileURLToPath } from 'node:url';
import { dirname, join } from 'node:path';
import { createState, configFromPython, step, MapSpec, createBot, botCommands, Command } from '../src/index';

const here = dirname(fileURLToPath(import.meta.url));
const SEEDS = [3, 4, 5];

interface PowerRun {
  config: Record<string, unknown>;
  demand_kw: number[]; supply_kw: number[]; shed_log: number[]; shed_events: number; first_brownout: number | null;
  lost: number; lost_in_window: number; lost_after_window: number;
  lost_log: { t: number; reason: string; x: number; y: number }[];
  hourly: { h: number; held: number; front: number; interior: number; mags: number; shells: number; lost: number }[];
  total_mags: number; held: number; front: number; interior: number;
}
interface PowerFixture { seed: number; hours: number; base: Record<string, unknown>; runs: Record<string, PowerRun> }
interface SeedFixture {
  w: number; h: number; start: [number, number]; target: [number, number]; wells: [number, number][];
  cells: MapSpec['cells']; scattered_inert: [number, number][];
}

const load = <T>(name: string): T => JSON.parse(readFileSync(join(here, '..', 'fixtures', 'lattice', name), 'utf8'));

for (const seed of SEEDS) {
  const fx = load<PowerFixture>(`power${seed}.json`);
  const sf = load<SeedFixture>(`seed${seed}.json`);
  const spec: MapSpec = { w: sf.w, h: sf.h, start: sf.start, target: sf.target, wells: sf.wells, cells: sf.cells,
                          scatteredInert: sf.scattered_inert, facilities: [], survivors: [] };
  for (const name of Object.keys(fx.runs)) {
    test(`seed ${seed} power ${name}: 5-hour compact run matches the Python fixture`, () => {
      const py = fx.runs[name];
      // Python DEFAULTS the fixture config leaves implicit: unfed 'none', start_rounds 200, no economy
      const cfg = configFromPython(py.config, { unfed: 'none', startRounds: 200, economy: false, startAssemblers: 0 });
      assert.equal(cfg.power, true); assert.equal(cfg.production, false);
      const st = createState(spec, cfg, seed);
      const bot = createBot('compact');
      const cmds: Command[] = [];
      const ticks = fx.hours * 3600;
      for (let k = 0; k < ticks; k++) { cmds.length = 0; botCommands(st, bot, cmds); step(st, cmds); st.events.length = 0; }
      // parity holds until the Python run sheds its first substation (D-B3-4 sheds nothing)
      assert.ok(py.shed_log.length > 0, 'every fixture run sheds under the Python rule');
      const upto = Math.floor(py.shed_log[0] / 60);
      assert.ok(upto >= 10, `at least ten minutes of parity (${upto})`);
      assert.deepEqual(st.power.demandKw.slice(0, upto), py.demand_kw.slice(0, upto), 'demand per minute to the first Python shed');
      assert.deepEqual(st.power.supplyKw.slice(0, upto), py.supply_kw.slice(0, upto), 'supply per minute to the first Python shed');
      assert.equal(st.stats.firstBrownout, py.first_brownout === null ? -1 : py.first_brownout, 'first brownout');
      assert.equal(st.power.demandKw.length, py.demand_kw.length, 'minutes sampled');
      // proportional: the brownout is counted and throttled, no block falls to power, never worse than the Python run
      assert.ok(st.stats.brownoutS >= 20, 'brownout seconds counted');
      assert.ok(st.stats.throttleMin > 0 && st.stats.throttleMin < 1, `throttle recorded (${st.stats.throttleMin})`);
      if (py.config.supply !== 'schedule') assert.equal(st.power.throttle, 1, 'full speed again by 5 h');   // the §15 schedule stays short at 5 h
      assert.ok(st.stats.lostLog.every(l => l.reason !== 'unfed'), 'unfed rule off: no unfed falls');
      assert.ok(st.stats.lost <= py.lost, `lost ${st.stats.lost} ≤ Python ${py.lost}`);
      assert.equal(st.stats.lostInWindow, 0, 'nothing falls in the window');
    });
  }
}
