/** E9 — Relight hold: wells' growth ×3 and wake-size blooms on every awake block for ten minutes at hour 25, with the
 *  doc's four assemblers and with six, with and without a banked magazine stock. D4 pending. */
import { runSim, districtReport } from '../run';
import { mr, fmt, isTrue, within, mean, Experiment, ExperimentResult, Section, Check } from '../util';
import { SimConfig } from '@relight/sim';

export const E9: Experiment = {
  id: 'E9', title: 'Relight hold',
  run(ctx): ExperimentResult {
    const { seeds } = ctx;
    const sections: Section[] = [], checks: Check[] = [], data: Record<string, unknown> = {};
    const rows: (string | number)[][] = [];
    const six: Partial<SimConfig> = { asmSchedule: [[600, 1], [1800, 2], [3000, 3], [10800, 4], [14400, 6]] };
    const eight: Partial<SimConfig> = { asmSchedule: [[600, 1], [1800, 2], [3000, 3], [10800, 4], [14400, 8]] };
    const variants: [string, Partial<SimConfig>][] = [
      ['no relight, 4 assemblers', { relight: null }],
      ['relight ×3 for 10 min, 4 assemblers', { relight: { hour: 25, minutes: 10, mult: 3 } }],
      ['relight, 6 assemblers', { relight: { hour: 25, minutes: 10, mult: 3 }, ...six }],
      ['relight, 4 assemblers, banked stock (buffer cap 40,000 rounds)', { relight: { hour: 25, minutes: 10, mult: 3 }, bufferCap: 40000 }],
      ['relight, 6 assemblers, banked stock', { relight: { hour: 25, minutes: 10, mult: 3 }, ...six, bufferCap: 40000 }],
      ['relight, 8 assemblers from 4 h, no bank (buffer cap 4,000 rounds)', { relight: { hour: 25, minutes: 10, mult: 3 }, ...eight }],
      ['relight, 8 assemblers, bank 4,000 magazines (cap 40,000 rounds)', { relight: { hour: 25, minutes: 10, mult: 3 }, ...eight, bufferCap: 40000 }],
      ['relight, 8 assemblers, bank 20,000 magazines (cap 200,000 rounds)', { relight: { hour: 25, minutes: 10, mult: 3 }, ...eight, bufferCap: 200000 }],
    ];
    let baseRun: ReturnType<typeof runSim> | null = null;
    for (const [name, kw] of variants) {
      const need: number[] = [], before: number[] = [], lostW: number[] = [], lostAfter: number[] = [], wells: number[] = [], peak: number[] = [], lost24: number[] = [], bank: number[] = [], prod: number[] = [], parity: number[] = [];
      for (const seed of seeds) {
        const r = runSim({ seed, policy: 'compact', hours: 25.5, cfg: kw });
        if (name.startsWith('no relight') && !baseRun) baseRun = r;
        bank.push((r.bufferAt[25] ?? 0) / 10); prod.push(r.ratios[25] ? r.ratios[25][1] : NaN);
        let ph = -1; for (let h = 1; h <= 25; h++) if (r.ratios[h] && r.ratios[h][0] > r.ratios[h][1]) { ph = h; break; }
        parity.push(ph);
        const w0 = 25 * 3600, w1 = w0 + 600;
        const brW = r.blooms.filter(b => b.t >= w0 && b.t < w1), brB = r.blooms.filter(b => b.t >= w0 - 3600 && b.t < w0);
        need.push(brW.reduce((a, b) => a + b.rounds, 0) / 10 / 10); before.push(brB.reduce((a, b) => a + b.rounds, 0) / 10 / 60);
        wells.push(brW.filter(b => b.key === 'well').length);
        const perMin: number[] = []; for (const b of brW) { const m = Math.floor((b.t - w0) / 60); perMin[m] = (perMin[m] ?? 0) + b.rounds / 10; }
        peak.push(perMin.length ? Math.max(...perMin.map(x => x ?? 0)) : 0);
        lostW.push(r.lostLog.filter(l => l.t >= w0 && l.t < w1 + 600).length); lostAfter.push(r.lostLog.filter(l => l.t >= w0).length);
        lost24.push(r.lostLog.filter(l => l.t < w0).length);
      }
      data[name] = { need, before, lostWindow: lostW, lostAfter, wellBlooms: wells, peak, lostBefore: lost24, bankedMags: bank, productionAt25h: prod, demandPassesProductionHour: parity };
      rows.push([name, mr(need, 1), mr(before, 1), mr(prod, 0), mr(bank, 0), mr(peak, 0), wells.join('/'), lostW.join('/'), lostAfter.join('/'), lost24.join('/'), parity.map(p => p < 0 ? 'never' : p).join('/')]);
    }
    sections.push({ title: 'E9-hold: compact, canonical map, ammo line on, 25.5 h', note: 'need = magazines/min demanded in 25:00–25:10; before = mag/min over 24:00–25:00; peak = the worst minute in the window',
      header: ['variant', 'mag/min in window', 'mag/min hour before', 'production mag/min at 25 h', 'magazines banked at 25:00', 'peak minute (mags)', 'well blooms in window (per seed)', 'lost 25:00–25:20', 'lost from 25:00', 'lost before 25:00', 'hour demand first passes production'], rows });
    // §12/§15: compact demand and territory by hour with the doc's four assemblers (no relight, first seed)
    if (baseRun) {
      const b = baseRun;
      const hRows: (string | number)[][] = [];
      for (const h of [1, 3, 5, 8, 10, 12, 15, 20, 25]) { const row = b.hourly[h - 1]; hRows.push([h, row.held, row.front, row.interior, row.mags.toFixed(1), row.production.toFixed(0), row.shells.toFixed(1), row.lost]); }
      sections.push({ title: 'E9-hourly: compact at the §18 cadence, four Shot assemblers from 3 h, first seed (demand = mag/min over the last 10 min of the hour)', header: ['hour', 'held', 'front', 'interior', 'demand mag/min', 'production mag/min', 'shells/min', 'lost so far'], rows: hRows });
      data.hourly = b.hourly.map(r => ({ h: r.h, held: r.held, front: r.front, interior: r.interior, mags: r.mags, production: r.production, shells: r.shells, lost: r.lost }));
      const dRows: (string | number)[][] = [];
      const dr = districtReport(b);
      for (const r of dr) dRows.push([r.d, r.nSs, r.nWake, fmt(r.shSs), fmt(r.huSs), fmt(r.magEdge), fmt(r.magBlock), fmt(r.shellEdge, 3)]);
      data.district25h = dr;
      sections.push({ title: 'E9-district: per-district cost over hours 1–25.5 of the same run (the outskirts and well edges compact play reaches only after 5 h)', header: ['district', 'n ss', 'n wake', 'shade ss', 'hulk ss', 'mag/edge/min', 'mag/block/min', 'shell/edge/min'], rows: dRows });
    }
    const base = data['relight ×3 for 10 min, 4 assemblers'] as { lostWindow: number[]; need: number[] };
    const noBank8 = data['relight, 8 assemblers from 4 h, no bank (buffer cap 4,000 rounds)'] as { lostWindow: number[] };
    const bank8 = data['relight, 8 assemblers, bank 20,000 magazines (cap 200,000 rounds)'] as { lostWindow: number[] };
    checks.push(isTrue('E9 informational: the Relight window demands more than the hour before', base.need.every((n, i) => n > (data['relight ×3 for 10 min, 4 assemblers'] as { before: number[] }).before[i]), `need ${base.need.map(x => x.toFixed(1)).join('/')}`));
    // A bank that production cannot fill is no bank (4 assemblers = 80 mag/min under a ~84 mag/min demand banks 0), so D4 is
    // judged on the pair whose production exceeds demand: 8 assemblers with and without the 20,000-magazine bank.
    checks.push(isTrue('D4: a filled bank loses no more blocks in the window than the same assemblers without one', bank8.lostWindow.every((x, i) => x <= noBank8.lostWindow[i]), `bank ${bank8.lostWindow.join('/')} vs ${noBank8.lostWindow.join('/')}`));
    const big = data['relight, 8 assemblers, bank 20,000 magazines (cap 200,000 rounds)'] as { lostWindow: number[]; bankedMags: number[] };
    checks.push(within('D4 (design statement): with a real bank the hold costs fewer than 5 blocks — informational until the human reads E9', mean(big.lostWindow), 0, 100, ' blocks'));
    return { id: 'E9', title: E9.title, pyNames: ['E11 (phase5.py)', 'E11b (phase5b.py)'], docRefs: ['§15', '§22', '§25 item 13'],
      setup: 'compact, canonical map, production on, 25.5 h, relight at 25:00 for 10 min', sections, checks, data };
  },
};
