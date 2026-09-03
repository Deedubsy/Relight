/** E7 — shape ratios on the canonical map: spike/compact and cheapest/compact at the §18 cadence and at the legacy
 *  8-minute cadence the §27 numbers were drawn at. */
import { runSim } from '../run';
import { mr, mean, within, Experiment, ExperimentResult, Section, Check } from '../util';
import { Policy } from '@relight/sim';

export const E7: Experiment = {
  id: 'E7', title: 'Shape ratios',
  run(ctx): ExperimentResult {
    const { seeds, hours } = ctx;
    const sections: Section[] = [], checks: Check[] = [], data: Record<string, unknown> = {};
    const rows: (string | number)[][] = [];
    for (const [cad, gapAfter] of [['§18 cadence (15 min in h1, then 5 min)', 300], ['legacy 8-min cadence', 480]] as [string, number][]) {
      const tot: Record<string, number> = {};
      for (const policy of ['compact', 'balanced', 'spike', 'cheapest', 'turtle'] as Policy[]) {
        const rs = seeds.map(seed => runSim({ seed, policy, hours, gapAfter }));
        tot[policy] = mean(rs.map(r => r.totalMags));
        rows.push([cad, policy, mr(rs.map(r => r.totalMags), 0), mr(rs.map(r => r.totalShells), 0), rs.map(r => r.firstInterior < 0 ? 'never' : Math.round(r.firstInterior / 60)).join('/'), mr(rs.map(r => r.held), 0), mr(rs.map(r => r.front), 0), mr(rs.map(r => r.lost), 1)]);
      }
      const k = gapAfter === 300 ? 'cadence18' : 'cadence8';
      data[k] = { totals: tot, spikeCompact: tot.spike / tot.compact, cheapestCompact: tot.cheapest / tot.compact, balancedCompact: tot.balanced / tot.compact, turtle: tot.turtle };
      rows.push([cad, 'spike/compact', (tot.spike / tot.compact).toFixed(2) + '×', '', '', '', '', '']);
      rows.push([cad, 'cheapest/compact', (tot.cheapest / tot.compact).toFixed(2) + '×', '', '', '', '', '']);
    }
    // §5's interleave rule (adjacent blooms ≥ 10 s apart), off in the canonical config
    const il: Record<string, number> = {};
    for (const policy of ['compact', 'spike'] as Policy[]) {
      const rs = seeds.map(seed => runSim({ seed, policy, hours, gapAfter: 300, cfg: { interleave: true } }));
      il[policy] = mean(rs.map(r => r.totalMags)) / (data.cadence18 as { totals: Record<string, number> }).totals[policy];
      rows.push(['§18 cadence + interleave 10 s', policy, mr(rs.map(r => r.totalMags), 0), mr(rs.map(r => r.totalShells), 0), rs.map(r => r.firstInterior < 0 ? 'never' : Math.round(r.firstInterior / 60)).join('/'), mr(rs.map(r => r.held), 0), mr(rs.map(r => r.front), 0), mr(rs.map(r => r.lost), 1)]);
    }
    data.interleave = il;
    sections.push({ title: 'E7: five policies on the canonical map, 5 h, production on (turtle never claims: the HQ alone)', header: ['cadence', 'policy', 'total magazines', 'total shells', 'first interior (min) per seed', 'held', 'front', 'lost'], rows });
    const c = data.cadence18 as { spikeCompact: number; cheapestCompact: number };
    checks.push(within('§7/§9 spike costs more ammo than compact (§18 cadence)', c.spikeCompact, 2.0, 3.5, '×'));
    checks.push(within('§7/§17 cheapest costs more than compact on the scattered map (§18 cadence)', c.cheapestCompact, 1.0, 1.5, '×'));
    checks.push(within('§5 interleaving adjacent blooms by 10 s changes compact ammo by < 10 %', il.compact, 0.9, 1.1, '×'));
    return { id: 'E7', title: E7.title, pyNames: ['E7 regression', 'E12'], docRefs: ['§7', '§9', '§27'],
      setup: 'compact, balanced, spike, cheapest; canonical map; production on', sections, checks, data };
  },
};
