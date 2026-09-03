/** Power-model parity with frontsim.py (export_power_fixtures.py): seeds 3, 4, 5, scattered map, compact policy,
 *  five variants (draw × shed × supply). Per-minute demand and supply, shed log, lost log, hourly rows: exact. */
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
      assert.deepEqual(st.power.demandKw, py.demand_kw, 'demand per minute');
      assert.deepEqual(st.power.supplyKw, py.supply_kw, 'supply per minute');
      assert.deepEqual(st.stats.shedLog, py.shed_log, 'shed log');
      assert.equal(st.stats.shedEvents, py.shed_events, 'shed events');
      assert.equal(st.stats.firstBrownout, py.first_brownout === null ? -1 : py.first_brownout, 'first brownout');
      assert.deepEqual(st.stats.lostLog, py.lost_log, 'lost log');
      assert.equal(st.stats.lost, py.lost, 'lost');
      assert.equal(st.stats.lostInWindow, py.lost_in_window, 'lost in window');
      assert.equal(st.stats.lostAfterWindow, py.lost_after_window, 'lost after window');
      for (let h = 0; h < py.hourly.length; h++) {
        const a = st.hourly[h], b = py.hourly[h];
        assert.equal(a.held, b.held, `h${b.h} held`); assert.equal(a.front, b.front, `h${b.h} front`);
        assert.equal(a.interior, b.interior, `h${b.h} interior`); assert.equal(a.mags, b.mags, `h${b.h} mags`);
        assert.equal(a.shells, b.shells, `h${b.h} shells`); assert.equal(a.lost, b.lost, `h${b.h} lost`);
      }
      assert.equal(st.totalRounds / 10, py.total_mags, 'total magazines');
    });
  }
}
