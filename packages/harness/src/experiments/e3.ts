/** E3 — enemy thresholds per district: shade and hulk fractions of steady-state vs wake blooms, ammo per edge, and the
 *  isolated-block steady state that §7's cost table is drawn from. */
import { runSim, districtReport, steppedBlock, DISTRICT_KEYS, DistrictRow } from '../run';
import { mean, clean, fmt, within, Experiment, ExperimentResult, Section, Check } from '../util';

export const E3: Experiment = {
  id: 'E3', title: 'Enemy thresholds per district',
  run(ctx): ExperimentResult {
    const { seeds, hours } = ctx;
    const sections: Section[] = [], checks: Check[] = [], data: Record<string, unknown> = {};
    // E3-block: isolated awake block, the sim's bloom model, last two hours of six
    const blockRows: (string | number)[][] = [];
    const cells: [string, number, number][] = [['civ', 0.30, 0.0004], ['res', 0.45, 0.0005], ['res far (×1.3 depth)', 0.585, 0.0005], ['ind', 0.60, 0.0006], ['ind far (×1.3)', 0.78, 0.0006],
      ['out', 1.0, 0.0008], ['well on res (cap +0.3, g ×4)', 0.75, 0.002], ['well on ind', 0.9, 0.0024], ['well on out', 1.0, 0.0032], ['res 1 block from well', 0.675, 0.001625]];
    const block: Record<string, unknown> = {};
    for (const [name, dmax, g] of cells) {
      const a = steppedBlock(dmax, g), b = steppedBlock(dmax, g, 0.25, 0.45);
      block[name] = { dmax, g, d: a.d, magPerMin: a.magPerMin, shellsPerMin: a.shellsPerMin, lowThr: b };
      blockRows.push([name, dmax.toFixed(3), g.toFixed(4), fmt(a.d, 2), fmt(a.magPerMin, 1), fmt(a.shellsPerMin, 1), fmt(b.magPerMin, 1), fmt(b.shellsPerMin, 1)]);
    }
    sections.push({ title: 'E3-block: one awake block in isolation, steady state over hours 4–6 (thresholds 0.3/0.5; last two columns 0.25/0.45)',
      header: ['block', 'cap', 'growth/s', 'steady rot', 'mag/min', 'shells/min', 'mag/min @.25/.45', 'shells/min @.25/.45'], rows: blockRows });
    data.block = block;
    // E3 runs
    const agg: Record<string, Record<string, number[]>> = {};
    const firstRows: (string | number)[][] = [];
    const firsts: Record<string, { shade: number[]; hulk: number[] }> = {};
    for (const policy of ['compact', 'spike'] as const) {
      for (const [shadeThr, hulkThr] of [[0.3, 0.5], [0.25, 0.45]] as const) {
        const tag = `E3-${policy}-shade${shadeThr}-hulk${hulkThr}`;
        const per: Record<string, DistrictRow[]> = {};
        const runs = seeds.map(seed => runSim({ seed, policy, hours, cfg: { shadeThr, hulkThr } }));
        for (const run of runs) for (const r of districtReport(run)) (per[r.d] ??= []).push(r);
        firsts[tag] = { shade: runs.map(r => r.firstShade / 60), hulk: runs.map(r => r.firstHulk / 60) };
        firstRows.push([tag, runs.map(r => r.firstShade < 0 ? 'never' : (r.firstShade / 60).toFixed(0)).join('/'), runs.map(r => r.firstHulk < 0 ? 'never' : (r.firstHulk / 60).toFixed(0)).join('/')]);
        const rows: (string | number)[][] = [];
        agg[tag] = {};
        for (const key of DISTRICT_KEYS) {
          const rs = per[key] ?? [];
          const m = (f: keyof DistrictRow) => mean(clean(rs.map(r => r[f] as number)));
          agg[tag][key] = [m('shSs'), m('huSs'), m('shWk'), m('huWk'), m('magEdge'), m('magBlock'), m('shellEdge'), m('ssRounds'), m('wakeRounds')];
          rows.push([key, rs.reduce((a, r) => a + r.nSs, 0), rs.reduce((a, r) => a + r.nWake, 0), fmt(m('shSs')), fmt(m('huSs')), fmt(m('shWk')), fmt(m('huWk')), fmt(m('magEdge')), fmt(m('magBlock')), fmt(m('shellEdge'), 3), fmt(m('ssRounds'), 0), fmt(m('wakeRounds'), 0)]);
        }
        sections.push({ title: tag, note: 'ss = steady-state blooms after hour one, wk = wake blooms; fractions of blooms carrying a shade / a hulk; ammo per frontage-edge-minute',
          header: ['district', 'n ss', 'n wake', 'shade ss', 'hulk ss', 'shade wk', 'hulk wk', 'mag/edge/min', 'mag/block/min', 'shell/edge/min', 'rounds per ss bloom', 'rounds per wake bloom'], rows });
      }
    }
    sections.push({ title: 'E3-first: minute of the first shade and the first hulk (per seed)', header: ['run', 'first shade (min)', 'first hulk (min)'], rows: firstRows });
    data.firsts = firsts;
    data.runs = agg;
    const c = agg['E3-compact-shade0.3-hulk0.5'];
    checks.push(within('§7 deep industrial blocks shade on every steady bloom (compact, 0.3/0.5)', c.ind[0], 0.9, 1.0));
    checks.push(within('§7 civic steady blooms rarely carry a shade (deep civic only)', c.civ[0], 0, 0.15));
    checks.push(within('§7 isolated outskirts block: shells/min at steady state (hulks from d ≥ 0.5)', (block['out'] as { shellsPerMin: number }).shellsPerMin, 1.5, 2.5, ' shells/min'));
    checks.push(within('§7 a wake bloom is about twice a steady bloom (residential, compact)', c.res[8] / c.res[7], 1.5, 2.5, '×'));
    checks.push(within('§7 steady mag/min of an isolated residential block', (block['res'] as { magPerMin: number }).magPerMin, 1.0, 1.6, ' mag/min'));
    return { id: 'E3', title: E3.title, pyNames: ['E3-block', 'E3-compact-*', 'E3-spike-*', 'sanity'], docRefs: ['§7'],
      setup: 'compact and spike, canonical map, production on', sections, checks, data };
  },
};
