/** E2 — brownout under D-B3-4 (proportional): a supply shortfall at hour four slows every machine to supply ÷ demand;
 *  nothing is shed, so the only way a block falls is through the ring — assemblers slow, hoppers drain, the red pip
 *  shows, the unfed rule stops the substation. Does the red pip lead the first fall (the behaviour D-B3-4 wants), and
 *  how long a shortfall of what depth does the ring survive? Plus the demand curves §12/§15 quote and the §15 schedule. */
import { runSim } from '../run';
import { mr, min, isTrue, within, Experiment, ExperimentResult, Section, Check } from '../util';
import { SimConfig } from '@relight/sim';

export const E2: Experiment = {
  id: 'E2', title: 'Brownout: proportional throttle, the ring and the red pip',
  run(ctx): ExperimentResult {
    const { seeds, hours } = ctx;
    const sections: Section[] = [], checks: Check[] = [], data: Record<string, unknown> = {};
    // the slice's power model: D1 draw, supply tracking demand + headroom, the ring fed by the assemblers, the unfed rule on
    const PW: Partial<SimConfig> = { power: true, supply: 'track', draw: 'half', production: true, unfed: 'substation', unfedN: 40 };
    const T0 = 4 * 3600;
    const rows: (string | number)[][] = [];
    const table: Record<string, unknown> = {};
    // [name, minutes, run hours, [headroom, pcts][]]; the 6 h window is the one long enough for the ring to starve (10 h run)
    const windows: [string, number, number, [number, number[]][]][] = [
      ['10 min', 10, hours, [[500, [5, 15, 25, 50]], [1000, [25, 50]]]],
      ['sustained (to 5 h)', 60, hours, [[500, [5, 15, 25, 50]], [1000, [25, 50]]]],
      ['sustained 6 h (10 h run)', 360, 10, [[500, [50, 75, 90]]]]];
    for (const [wname, minutes, runHours, cases] of windows) {
      for (const [headroom, pcts] of cases) {
        for (const pct of pcts) {
          const rs = seeds.map(seed => runSim({ seed, policy: 'compact', hours: runHours, cfg: { ...PW, headroom, shortfall: { pct, hour: 4, minutes } } }));
          const key = `E2-${minutes === 10 ? 'window' : minutes === 60 ? 'sustained' : 'sustained6h'}-h${headroom}-${pct}pct`;
          const lost = rs.map(r => r.lost), inw = rs.map(r => r.power.lostInWindow), aft = rs.map(r => r.power.lostAfterWindow);
          const bmin = rs.map(r => r.power.brownoutS / 60), thr = rs.map(r => r.power.throttleMin);
          const pipBefore = rs.map(r => r.hopperEmpty.filter(t => t < T0).length);
          const pip = rs.map(r => { const t = r.hopperEmpty.find(x => x >= T0); return t === undefined ? -1 : (t - T0) / 60; });
          const fall = rs.map(r => { const l = r.lostLog.find(x => x.t >= T0); return l ? (l.t - T0) / 60 : -1; });
          const lead = rs.map((_, i) => fall[i] < 0 ? NaN : pip[i] < 0 ? -Infinity : fall[i] - pip[i]);
          table[key] = { lost, inWindow: inw, afterWindow: aft, brownoutMin: bmin, throttleMin: thr, pipBefore, pipAfterStart: pip, fallAfterStart: fall,
                         falls: rs[0].lostLog.map(l => [+(l.t / 60).toFixed(1), l.reason]) };
          rows.push([wname, headroom, pct, mr(lost, 1), mr(inw, 1), mr(aft, 1), mr(bmin, 0), mr(thr, 2), pipBefore.join('/'),
                     pip.map(x => min(x < 0 ? -1 : x * 60)).join(' / '), fall.map(x => min(x < 0 ? -1 : x * 60)).join(' / '),
                     lead.map(x => Number.isNaN(x) ? '-' : x === -Infinity ? 'no pip' : x.toFixed(1)).join(' / '), rs.map(r => r.held).join('/')]);
          const lossy = rs.map((r, i) => [r, i] as const).filter(([r]) => r.lostLog.some(x => x.t >= T0));
          if (lossy.length) checks.push(isTrue(`${key}: the red pip leads every first fall`, lossy.every(([, i]) => lead[i] > 0),
            `fall − first pip (min) per losing seed ${lossy.map(([, i]) => lead[i] === -Infinity ? 'no pip' : lead[i].toFixed(1)).join('/')}`));
          if (minutes === 10 && pct <= 25) checks.push(isTrue(`${key}: a 10-minute shortfall of ${pct} % loses nothing`, lost.every(x => x === 0), `lost per seed ${lost.join('/')}`));
        }
      }
    }
    sections.push({ title: 'E2-matrix: compact, the slice power model (D1 draw, production on, unfed rule N40), shortfall from hour 4 for 10 minutes, to 5 h, or 6 h',
      note: 'headroom = spare supply above full demand, recomputed every 10 min outside the shortfall; every machine runs at supply ÷ demand while short (D-B3-4); pips before = hopper-empty events in the first 4 h; pip / fall = minutes after the shortfall starts, per seed, - = never; lead = fall − first pip',
      header: ['window', 'headroom kW', 'shortfall %', 'lost', 'in window', 'after window', 'brownout min', 'worst throttle', 'pips before', 'first red pip (min after start)', 'first fall (min after start)', 'lead (min)', 'held at end'], rows });
    data.matrix = table;

    // demand curves (§12/§15): D1 draw and doc draw, compact, 5 h then 25 h, first seed
    const DEM: Partial<SimConfig> = { production: false, power: true, supply: 'track' };
    const dRows: (string | number)[][] = [];
    const demand: Record<string, number[]> = {};
    for (const draw of ['half', 'doc'] as const) {
      const r5 = runSim({ seed: seeds[0], policy: 'compact', hours, cfg: { ...DEM, draw } });
      const r25 = runSim({ seed: seeds[0], policy: 'compact', hours: 25, cfg: { ...DEM, draw } });
      const at = (r: typeof r5, h: number) => (r.power.demandKw[h * 60 - 1] ?? NaN) / 1000;
      demand[draw] = [1, 2, 3, 4, 5].map(h => at(r5, h)); demand[`${draw}-25h`] = [5, 10, 15, 20, 25].map(h => at(r25, h));
      dRows.push([draw === 'half' ? 'D1 100/20 kW' : 'doc 200/40 kW', ...[1, 2, 3, 4, 5].map(h => at(r5, h).toFixed(2)), ...[10, 15, 20, 25].map(h => at(r25, h).toFixed(1)), `${r25.held}/${r25.front}/${r25.interior}`]);
    }
    sections.push({ title: 'E2-demand: modelled demand with every substation on (compact, first seed, 4 Shot assemblers from 3 h)', note: 'MW at the hour mark; 25 h row: held/front/interior at 25 h',
      header: ['draw', '1 h', '2 h', '3 h', '4 h', '5 h', '10 h', '15 h', '20 h', '25 h', 'held/front/interior @25 h'], rows: dRows });
    data.demand = demand;
    checks.push(within('§15 demand at 4 h under D1 (compact, first seed)', demand.half[3], 1.0, 3.0, ' MW'));

    // supply on the §15 schedule, the ring fed by throttled assemblers
    const sRows: (string | number)[][] = [];
    for (const draw of ['half', 'doc'] as const) {
      const rs = seeds.map(seed => runSim({ seed, policy: 'compact', hours, cfg: { ...PW, draw, supply: 'schedule', shortfall: null } }));
      sRows.push([draw === 'half' ? 'D1 100/20 kW' : 'doc 200/40 kW', mr(rs.map(r => r.lost), 1), rs.map(r => min(r.firstFall)).join(' / '), mr(rs.map(r => r.power.brownoutS / 60), 0), mr(rs.map(r => r.power.throttleMin), 2)]);
      data[`schedule-${draw}`] = rs.map(r => ({ lost: r.lost, firstFall: r.firstFall, brownoutMin: r.power.brownoutS / 60, throttleMin: r.power.throttleMin }));
    }
    sections.push({ title: 'E2-schedule: blocks lost if supply follows the §15 generator schedule (0.3 MW → 0.6 @30 min → 0.9 @1 h → 1.2 @2 h → 1.5 @3 h → 1.8 @4 h → 2.1 @5 h), production and the unfed rule on',
      header: ['draw', 'lost in 5 h', 'first fall (min) per seed', 'brownout min', 'worst throttle'], rows: sRows });
    return { id: 'E2', title: E2.title, pyNames: ['E2-h500-*', 'E2-h1000-*', 'E2-demand', 'E2-demand-25h', 'E2-demand-half', 'schedule supply'], docRefs: ['§5', '§12', '§14', '§15'],
      setup: 'compact, canonical map, power model on with production and the unfed rule (demand curves: production off)', sections, checks, data };
  },
};
