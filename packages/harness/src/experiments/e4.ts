/** E4 — the first hour under the power model (§11): Generator count and coal to minute 60 without a brownout,
 *  §11 literally (200/40 kW, 40 coal) and under D1 (100/20 kW, HQ 100 kW, ~700-unit coal patch). */
import { firstHour, FirstHourOptions, FIRST_HOUR_D1 } from '@relight/sim';
import { min, isTrue, within, Experiment, ExperimentResult, Section, Check } from '../util';

export const E4: Experiment = {
  id: 'E4', title: 'First hour under the power model',
  run(): ExperimentResult {
    const sections: Section[] = [], checks: Check[] = [], data: Record<string, unknown> = {};
    const row = (label: string, o: Partial<FirstHourOptions>): (string | number)[] => {
      const r = firstHour(o); data[label] = r;
      const full = { ...o } as FirstHourOptions;
      return [label, min(r.firstBrownout), min(r.coalOut), (r.brownoutS / 60).toFixed(1), r.throttleMin.toFixed(2), r.peakKw.toFixed(0), r.gensNeeded.join(' '), r.coalBurned.toFixed(0),
              o.patch != null ? `${min(r.patchOut)} (${r.patchLeft.toFixed(0)} left)` : '-', (full.gens ?? [0, 1800]).map(g => g / 60).join(',')];
    };
    const header = ['run', 'first brownout (min)', 'coal out (min)', 'brownout minutes', 'worst throttle', 'peak kW', 'Generators needed per 10 min', 'coal burned in h1', 'patch mined out', 'Generators at (min)'];
    const lit: (string | number)[][] = [
      row('E4-literal', {}), row('E4-hq-draw-40', { hqDraw: 40 }), row('E4-flat-120', { drawFlat: true }),
      row('E4-start-coal-rubble', { coalRubble: true }), row('E4-coal-120', { startCoal: 120 }),
      row('E4-rubble+gen@6,15,25,40', { coalRubble: true, gens: [0, 360, 900, 1500, 2400] }),
      row('E4-rubble+gen@6,15,25,40,45', { coalRubble: true, gens: [0, 360, 900, 1500, 2400, 2700] }),
      row('E4-rubble+hq40+gen@15,25,40', { coalRubble: true, hqDraw: 40, gens: [0, 900, 1500, 2400] }),
      row('E4-coal200+gen@6,15,25,40,45', { startCoal: 200, gens: [0, 360, 900, 1500, 2400, 2700] }),
    ];
    let minCoal = -1;
    for (let c = 40; c < 400; c += 10) {
      const r = firstHour({ startCoal: c });
      if (r.coalOut < 0 || r.coalOut >= r.coalArrives + 120) { minCoal = c; break; }
    }
    data.minCoal = minCoal;
    sections.push({ title: 'E4-literal: §11 literally, 300 kW Generator, 40 coal, HQ 200 kW, substations 200/40 kW, machines at §11\'s minutes, coal rubble 60 s after the west block is Held',
      note: `E4-min-coal: ${minCoal} starting coal lasts until the west coal excavator + 2 min (coal arrives at ${(firstHour({}).coalArrives / 60).toFixed(1)} min)`, header, rows: lit });
    const d1: (string | number)[][] = [
      row('E4h-literal-1gen', { ...FIRST_HOUR_D1 }),
      row('E4h-gen@0,6', { ...FIRST_HOUR_D1, gens: [0, 360] }), row('E4h-gen@0,6,15', { ...FIRST_HOUR_D1, gens: [0, 360, 900] }),
      row('E4h-gen@0,6,15,25', { ...FIRST_HOUR_D1, gens: [0, 360, 900, 1500] }), row('E4h-gen@0,6,15,45', { ...FIRST_HOUR_D1, gens: [0, 360, 900, 2700] }),
    ];
    for (const patch of [450, 700, 1000, 3000]) d1.push(row(`E4h-patch${patch}`, { ...FIRST_HOUR_D1, patch, gens: [0, 360, 900, 1500] }));
    sections.push({ title: 'E4h: under D1, substations 100/20 kW, HQ 100 kW, coal patch on the HQ lot mined by one Excavator (0.5/s) from minute 6', header, rows: d1 });
    const chosen = firstHour({ ...FIRST_HOUR_D1, gens: [0, 360, 900, 1500] });
    checks.push(isTrue('§11 D1: four Generators (0, 6, 15, 25 min) reach minute 60 without a brownout', chosen.firstBrownout < 0, `first brownout ${min(chosen.firstBrownout)} min`));
    checks.push(isTrue('§11 D1: coal never runs out (patch + west rubble)', chosen.coalOut < 0, `coal out ${min(chosen.coalOut)} min`));
    checks.push(within('§11 D1: the ~700-unit patch mines out near minute 30 (west coal wanted by then)', chosen.patchOut / 60, 25, 35, ' min'));
    checks.push(within('§11 D1: coal burned in hour one', chosen.coalBurned, 400, 800, ' coal'));
    return { id: 'E4', title: E4.title, pyNames: ['E4-*', 'E4h-*'], docRefs: ['§11', '§12', '§13', '§15'],
      setup: 'closed §11 timeline, no map (firstHour())', sections, checks, data };
  },
};
