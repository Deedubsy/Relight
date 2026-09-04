/** E1 — unfed consequence. What an empty hopper costs: the HQ under §11's starting ammo, the ring under each unfed
 *  rule, and the minutes from the first unfed arrival to the block's fall in residential/civic. */
import { runSim } from '../run';
import { mr, mean, clean, minOrNever, within, Experiment, ExperimentResult, Section, Check } from '../util';

export const E1: Experiment = {
  id: 'E1', title: 'Unfed consequence',
  run(ctx): ExperimentResult {
    const { seeds, hours } = ctx;
    const sections: Section[] = [], checks: Check[] = [], data: Record<string, unknown> = {};

    // E1-start: §11 literally — 20 magazines = 200 rounds against the HQ's three 100-round hoppers, 1 h
    const startRows: (string | number)[][] = [];
    for (const unfed of ['substation', 'creep'] as const) {
      const ff = seeds.map(seed => runSim({ seed, policy: 'compact', hours: 1, cfg: { startRounds: 200, unfed } }).firstFall);
      startRows.push([unfed, minOrNever(ff)]);
      data[`start-${unfed}`] = ff;
    }
    sections.push({ title: 'E1-start: HQ under §11\'s starting ammo (200 rounds), compact, 1 h', header: ['unfed rule', 'HQ falls at (min)'], rows: startRows });

    // E1-start-min: starting rounds needed so nothing falls under unfed=substation N=40 (5 h)
    const minRows: (string | number)[][] = [];
    let startMin = -1;
    for (let sr = 200; sr < 320; sr += 20) {
      const falls = seeds.map(seed => runSim({ seed, policy: 'compact', hours, cfg: { startRounds: sr, unfed: 'substation', unfedN: 40 } }).firstFall);
      minRows.push([sr, falls.map(f => f < 0 ? 'never' : (f / 60).toFixed(1)).join(' / ')]);
      if (falls.every(f => f < 0)) { startMin = sr; break; }
    }
    data.startMin = startMin;
    sections.push({ title: 'E1-start-min: starting rounds until no early fall (substation N 40, 5 h)', header: ['start rounds', 'first fall per seed (min)'], rows: minRows });

    // E1-ring-spike
    const spikeRows: (string | number)[][] = [];
    for (const [unfed, n] of [['none', 20], ['substation', 20], ['substation', 40], ['creep', 20]] as const) {
      const rs = seeds.map(seed => runSim({ seed, policy: 'spike', hours, cfg: { unfed, unfedN: n } }));
      const r3 = rs.map(r => r.ratios[3] ? r.ratios[3][0] / r.ratios[3][1] : NaN);
      const starved = [...new Set(rs.flatMap(r => r.lostLog.map(l => l.starved)))].sort();
      spikeRows.push([unfed, n, mr(rs.map(r => r.lost), 1), mr(rs.map(r => r.unfedTotal), 0), starved.join(',') || '-', mr(r3, 2), mr(rs.map(r => r.minBufferAfter2h), 0)]);
    }
    sections.push({ title: 'E1-ring-spike: the fed ring on the spike policy (well edges: a well bloom is up to 170 rounds against a 100-round hopper)',
      header: ['unfed', 'N', 'blocks lost in 5 h', 'unfed crawlers', 'starved-edge districts of falls', 'demand/production @3 h', 'min buffer after 2 h'], rows: spikeRows });

    // E1-ring / E1-starve matrix (compact)
    const matRows: (string | number)[][] = [];
    const delays: Record<string, number[]> = {};
    for (const starve of [false, true]) {
      for (const unfed of ['none', 'substation', 'creep'] as const) {
        for (const n of unfed === 'substation' ? [10, 20, 40] : [20]) {
          const rs = seeds.map(seed => runSim({ seed, policy: 'compact', hours, cfg: { unfed, unfedN: n, starveQuiet: starve } }));
          const tag = `E1-${starve ? 'starve' : 'ring'}-${unfed}${unfed === 'substation' ? `-N${n}` : ''}`;
          const ratio5 = rs.map(r => r.ratios[hours] ? r.ratios[hours][0] / r.ratios[hours][1] : NaN);
          const fdr = clean(rs.flatMap(r => r.lostLog.filter(l => (l.key === 'res' || l.key === 'civ') && l.delay >= 0).map(l => l.delay / 60)));
          const fdw = clean(rs.flatMap(r => r.lostLog.filter(l => l.key === 'well' || l.key === 'out' || l.key === 'ind').filter(l => l.delay >= 0).map(l => l.delay / 60)));
          delays[tag] = fdr;
          matRows.push([tag, mr(rs.map(r => r.lost / hours), 2), rs[0].lostByHour.map(x => x ?? 0).join(' '), mr(rs.map(r => r.unfedTotal), 0), mr(ratio5, 2),
                        mr(rs.map(r => r.hopperMean), 0), `${fdr.length}: ${mr(fdr, 1)}`, `${fdw.length}: ${mr(fdw, 1)}`]);
        }
      }
    }
    sections.push({ title: 'E1-ring / E1-starve: compact, 5 h, 200 starting rounds (C10); "starve" withholds from every edge whose dark block has rot < 0.3 (HQ edges exempt)',
      note: 'lost/h = blocks lost per hour; delay = minutes from the first unfed arrival at the block to its fall',
      header: ['run', 'lost/h', 'by hour (first seed)', 'unfed crawlers', 'demand/prod @5 h', 'mean hopper (rounds)', 'res/civ falls: first-unfed→fall (min)', 'well/out/ind falls: first-unfed→fall (min)'],
      rows: matRows });
    data.delays = delays;

    // Checks: what §5 says. The unfed consequence: a residential/civic block falls 5–15 min after its hopper first runs dry (canonical rule = substation N 40).
    const canon = delays['E1-starve-substation-N40'];
    checks.push(within('§5 unfed→fall delay, residential/civic, substation N 40 (starve): mean minutes', mean(canon), 5, 15, ' min'));
    checks.push(within('E1-start-min: starting rounds that survive the first five hours', startMin, 200, 300, ' rounds'));
    return { id: 'E1', title: E1.title, pyNames: ['E1-start', 'E1-start-min', 'E1-ring-spike', 'E1-ring-*', 'E1-starve-*'], docRefs: ['§5', '§11', '§15'],
      setup: 'compact (spike for the ring row), canonical map, production on, 200 starting rounds (C10) unless stated', sections, checks, data };
  },
};
