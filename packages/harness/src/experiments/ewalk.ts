/** E-walk — D5's walking cost. The compact bot with the engineer on foot: minutes walked per hour at hours 1, 3
 *  and 5, before and after the truck is found at the Tram depot, and what walking costs against the teleporting
 *  bot of the lattice era (held, lost, magazines). The brief's bar: more than 20 % of hour 3's moves are within
 *  reach or made in the truck. */
import { runSim, DEFAULT_MAP } from '../run';
import { mr, mean, min, within, Experiment, ExperimentResult, Section, Check } from '../util';
import { REACH } from '@relight/sim';

export const EWALK: Experiment = {
  id: 'E-walk', title: 'Walking minutes and the truck',
  run(ctx): ExperimentResult {
    const { seeds, hours } = ctx;
    const sections: Section[] = [], checks: Check[] = [], data: Record<string, unknown> = {};
    const rows: (string | number)[][] = [], cost: (string | number)[][] = [];
    const h3Frac: number[] = [], truckAt: number[] = [], perHour: number[][] = [];
    for (const seed of seeds) {
      const w = runSim({ seed, policy: 'compact', hours, walk: true });
      const tp = runSim({ seed, policy: 'compact', hours, walk: false });
      const wh = w.engineer.walkedHour.map(x => (x ?? 0) / 60);
      perHour.push(wh);
      const ta = w.engineer.truckAt; truckAt.push(ta);
      // minutes walked per hour of play before / after the truck
      let before = 0, after = 0;
      wh.forEach((m, h) => { const t0 = h * 3600, t1 = t0 + 3600; if (ta < 0 || t1 <= ta) before += m; else if (t0 >= ta) after += m; else { before += m * (ta - t0) / 3600; after += m * (t1 - ta) / 3600; } });
      const beforeRate = ta < 0 ? before / hours : before / (ta / 3600), afterRate = ta < 0 ? NaN : after / ((hours * 3600 - ta) / 3600);
      const h3 = w.walks.filter(x => x.t >= 2 * 3600 && x.t < 3 * 3600);
      const near = h3.filter(x => x.tiles <= REACH).length, byTruck = h3.filter(x => ta >= 0 && x.t >= ta).length;
      const frac = h3.length ? (near + byTruck) / h3.length : NaN;
      h3Frac.push(frac);
      rows.push([seed, wh[0]?.toFixed(1) ?? '-', wh[2]?.toFixed(1) ?? '-', wh[4]?.toFixed(1) ?? '-', min(ta, 0), beforeRate.toFixed(1), Number.isNaN(afterRate) ? '-' : afterRate.toFixed(1),
                 h3.length, near, byTruck, Number.isNaN(frac) ? '-' : (frac * 100).toFixed(0) + ' %', w.walks.length]);
      cost.push([seed, tp.held, w.held, tp.lost, w.lost, tp.totalMags.toFixed(0), w.totalMags.toFixed(0), ((w.totalMags - tp.totalMags) / tp.totalMags * 100).toFixed(1) + ' %', mr(w.walks.map(x => x.tiles), 0)]);
    }
    sections.push({ title: 'E-walk-hours: compact, engineer on foot (6 tiles/s; truck ×3 from the Tram depot at 3–4 hops), 5 h',
      note: 'moves = walks the bot started in hour 3; "within reach" = a walk of ≤ 8 tiles; "in the truck" = started after the truck was found',
      header: ['seed', 'walked h1 (min)', 'walked h3 (min)', 'walked h5 (min)', 'truck found (min)', 'min walked/h before truck', 'after truck', 'hour-3 moves', 'within reach', 'in the truck', 'reach-or-truck', 'walks in 5 h'], rows });
    sections.push({ title: 'E-walk-cost: the same bot teleporting (lattice era) vs walking',
      header: ['seed', 'held (teleport)', 'held (walk)', 'lost (teleport)', 'lost (walk)', 'magazines (teleport)', 'magazines (walk)', 'ammo diff', 'walk length (tiles)'], rows: cost });
    data.perHour = perHour; data.truckAt = truckAt; data.h3Frac = h3Frac;
    checks.push(within('rework: more than 20 % of hour-3 moves are within reach or in the truck (mean over seeds)', mean(h3Frac.filter(x => !Number.isNaN(x))) * 100, 20, 100, ' %'));
    checks.push(within('rework: the truck is found before hour 5 on every seed', Math.max(...truckAt.map(t => t < 0 ? 9e9 : t)) / 60, 0, 300, ' min'));
    checks.push(within('D5 tedium: walking under 10 min per hour of play in hour 3 (mean)', mean(perHour.map(p => p[2] ?? 0)), 0, 10, ' min'));
    return { id: 'E-walk', title: EWALK.title, pyNames: [], docRefs: ['§4', '§8', 'D5'],
      setup: `compact; ${DEFAULT_MAP} map; engineer walking, no rifle`, sections, checks, data };
  },
};
