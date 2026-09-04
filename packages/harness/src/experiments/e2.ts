/** E2 — brownout: a 10-minute supply shortfall of 5/15/25 % at hour four, under each draw model and shed order.
 *  Do losses stop at restoration, and which shed order makes "nothing cascades" true? Plus the demand curves §12/§15 quote. */
import { runSim } from '../run';
import { mr, min, isTrue, within, Experiment, ExperimentResult, Section, Check } from '../util';
import { SimConfig } from '@relight/sim';

export const E2: Experiment = {
  id: 'E2', title: 'Brownout cascade',
  run(ctx): ExperimentResult {
    const { seeds, hours } = ctx;
    const sections: Section[] = [], checks: Check[] = [], data: Record<string, unknown> = {};
    const PW: Partial<SimConfig> = { production: false, power: true, supply: 'track' };
    const variants: [string, Partial<SimConfig>][] = [
      ['D1 (100/20 kW, shed substations)', { draw: 'half' }],
      ['D1 + shed machines-first', { draw: 'half', shed: 'machines-first' }],
      ['D1 + interior grace 300 s', { draw: 'half', interiorGrace: 300 }],
      ['doc draw 200/40 kW', { draw: 'doc' }],
      ['flat 120 kW', { draw: 'flat' }],
    ];
    const rows: (string | number)[][] = [];
    const table: Record<string, unknown> = {};
    for (const headroom of [500, 1000]) {
      for (const pct of headroom === 500 ? [5, 15, 25] : [15]) {
        for (const [vname, kw] of variants) {
          const rs = seeds.map(seed => runSim({ seed, policy: 'compact', hours, cfg: { ...PW, headroom, shortfall: { pct, hour: 4, minutes: 10 }, ...kw } }));
          const key = `E2-h${headroom}-${pct}pct-${vname}`;
          const lost = rs.map(r => r.lost), inw = rs.map(r => r.power.lostInWindow), aft = rs.map(r => r.power.lostAfterWindow);
          const dem = rs.map(r => (r.power.demandKw[239] ?? NaN) / 1000);
          table[key] = { lost, inWindow: inw, afterWindow: aft, shed: rs.map(r => r.power.shedEvents), demand4h: dem,
                         timeline: { sheds: rs[0].power.shedLog, falls: rs[0].lostLog.map(l => [+(l.t / 60).toFixed(1), l.reason]) } };
          rows.push([headroom, pct, vname, mr(lost, 1), mr(inw, 1), mr(aft, 1), mr(rs.map(r => r.power.shedEvents), 1), mr(dem, 2)]);
          if (headroom === 500) {
            checks.push(isTrue(`${key}: losses stop at restoration (none after the window)`, aft.every(x => x === 0), `after-window losses per seed ${aft.join('/')}`));
            if (kw.shed === 'machines-first') checks.push(isTrue(`${key}: nothing cascades (no block lost)`, lost.every(x => x === 0), `lost per seed ${lost.join('/')}`));
          }
        }
      }
    }
    sections.push({ title: 'E2-matrix: compact, power on, production off, 10-minute shortfall at hour 4', note: 'headroom = spare supply above full demand, recomputed every 10 min; demand @4 h with every substation on',
      header: ['headroom kW', 'shortfall %', 'variant', 'lost', 'in window', 'after window', 'shed events', 'demand @4 h MW'], rows });
    data.matrix = table;

    // coupling: machines-first with production + unfed on
    const cRows: (string | number)[][] = [];
    for (const headroom of [500, 1000]) {
      const rs = seeds.map(seed => runSim({ seed, policy: 'compact', hours, cfg: { power: true, headroom, shortfall: { pct: 15, hour: 4, minutes: 10 }, draw: 'half', shed: 'machines-first', production: true, unfed: 'substation', unfedN: 40 } }));
      cRows.push([headroom, mr(rs.map(r => r.lost), 1), mr(rs.map(r => r.power.lostInWindow), 1), mr(rs.map(r => r.power.lostAfterWindow), 1)]);
    }
    sections.push({ title: 'E2-coupling: machines-first with production and the unfed rule on (15 %, D1 draw)', header: ['headroom kW', 'lost', 'in window', 'after window'], rows: cRows });

    // demand curves (§12/§15): D1 draw and doc draw, compact, 5 h then 25 h, first seed
    const dRows: (string | number)[][] = [];
    const demand: Record<string, number[]> = {};
    for (const draw of ['half', 'doc'] as const) {
      const r5 = runSim({ seed: seeds[0], policy: 'compact', hours, cfg: { ...PW, draw } });
      const r25 = runSim({ seed: seeds[0], policy: 'compact', hours: 25, cfg: { ...PW, draw } });
      const at = (r: typeof r5, h: number) => (r.power.demandKw[h * 60 - 1] ?? NaN) / 1000;
      demand[draw] = [1, 2, 3, 4, 5].map(h => at(r5, h)); demand[`${draw}-25h`] = [5, 10, 15, 20, 25].map(h => at(r25, h));
      dRows.push([draw === 'half' ? 'D1 100/20 kW' : 'doc 200/40 kW', ...[1, 2, 3, 4, 5].map(h => at(r5, h).toFixed(2)), ...[10, 15, 20, 25].map(h => at(r25, h).toFixed(1)), `${r25.held}/${r25.front}/${r25.interior}`]);
    }
    sections.push({ title: 'E2-demand: modelled demand with every substation on (compact, first seed, 4 Shot assemblers from 3 h)', note: 'MW at the hour mark; 25 h row: held/front/interior at 25 h',
      header: ['draw', '1 h', '2 h', '3 h', '4 h', '5 h', '10 h', '15 h', '20 h', '25 h', 'held/front/interior @25 h'], rows: dRows });
    data.demand = demand;
    checks.push(within('§15 demand at 4 h under D1 (compact, first seed)', demand.half[3], 1.0, 3.0, ' MW'));

    // supply on the §15 schedule
    const sRows: (string | number)[][] = [];
    for (const draw of ['half', 'doc'] as const) {
      const rs = seeds.map(seed => runSim({ seed, policy: 'compact', hours, cfg: { ...PW, draw, supply: 'schedule', shortfall: null } }));
      sRows.push([draw === 'half' ? 'D1 100/20 kW' : 'doc 200/40 kW', mr(rs.map(r => r.lost), 1), rs.map(r => min(r.firstFall)).join(' / '), mr(rs.map(r => r.power.shedEvents), 0)]);
      data[`schedule-${draw}`] = rs.map(r => ({ lost: r.lost, firstFall: r.firstFall, shed: r.power.shedEvents }));
    }
    sections.push({ title: 'E2-schedule: blocks lost if supply follows the §15 generator schedule (0.3 MW → 0.6 @30 min → 0.9 @1 h → 1.2 @2 h → 1.5 @3 h → 1.8 @4 h → 2.1 @5 h)',
      header: ['draw', 'lost in 5 h', 'first fall (min) per seed', 'shed events'], rows: sRows });
    return { id: 'E2', title: E2.title, pyNames: ['E2-h500-*', 'E2-h1000-*', 'E2-demand', 'E2-demand-25h', 'E2-demand-half', 'schedule supply'], docRefs: ['§5', '§12', '§14', '§15'],
      setup: 'compact, canonical map, power model on, production off unless stated', sections, checks, data };
  },
};
