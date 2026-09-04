/** E6 — inert walls: the scatter validators (lattice) or the generator's plazas (city), open vs inert shape ratios
 *  (which map §7's headline refers to), and the river-hugging policy against compact under each validator.
 *  On the street-first city (D6) the "open" map is the same streets with every plaza made a buildable lot. */
import { runSim, RunOpts } from '../run';
import { mr, mean, within, Experiment, ExperimentResult, Section, Check } from '../util';
import { makeScatter, generateCity, H, SimConfig, Policy, CityPreset } from '@relight/sim';

export const E6: Experiment = {
  id: 'E6', title: 'Inert walls',
  run(ctx): ExperimentResult {
    const { seeds, hours } = ctx;
    const city = ctx.map !== 'lattice';
    const sections: Section[] = [], checks: Check[] = [], data: Record<string, unknown> = {};
    const vRows: (string | number)[][] = [];
    if (!city) {
      for (const seed of seeds) {
        for (const val of ['none', 'no2x2', 'maxrun2'] as const) {
          const sc = makeScatter(seed, 0.09, val);
          const set = new Set(sc.map(([x, y]) => x * H + y));
          const all = (x: number, y: number) => y === H - 1 || set.has(x * H + y);
          let sq = 0, runs3 = 0;
          for (const [x, y] of sc) {
            if (all(x + 1, y) && all(x, y + 1) && all(x + 1, y + 1)) sq++;
            if ((set.has((x + 1) * H + y) && set.has((x + 2) * H + y)) || (set.has(x * H + y + 1) && set.has(x * H + y + 2))) runs3++;
          }
          vRows.push([seed, val, sc.length, sq, runs3]);
        }
      }
      sections.push({ title: 'E6-validators: scattered inert cells at 9 % of the grid', header: ['seed', 'validator', 'cells', '2×2 inert squares (incl. river)', 'straight runs of 3+'], rows: vRows });
    } else {
      for (const seed of seeds) {
        const g = generateCity(seed, ctx.map as CityPreset);
        const plazas = g.blocks.filter(b => b.inert);
        const touching = plazas.filter(b => b.nb.some(j => g.blocks[j].inert)).length;
        const wallLen = plazas.reduce((a, b) => a + b.nb.filter(j => !g.blocks[j].inert).length, 0);
        vRows.push([seed, g.blocks.length, plazas.length, (100 * plazas.length / g.blocks.length).toFixed(1) + ' %', touching, mr(plazas.map(b => b.area), 0), wallLen]);
      }
      sections.push({ title: 'E6-plazas: the generator\'s inert faces (D6: plazas and parks, 8–10 % of faces, none stranding a neighbour)',
        header: ['seed', 'faces', 'plazas', 'share', 'plazas touching another plaza', 'plaza area (tiles)', 'lot edges that face a plaza'], rows: vRows });
    }
    // open vs inert shape ratios
    const openName = city ? 'plazas buildable' : 'open', canonName = city ? 'plazas inert' : 'scattered';
    const variant = (inert: boolean): Partial<RunOpts> => city ? { plazas: inert ? 'inert' : 'buildable' } : { cfg: { scatter: inert } };
    const sRows: (string | number)[][] = [];
    const ratios: Record<string, { spikeCompact: number; compactCheapest: number; compactTotal: number }> = {};
    for (const inert of [false, true]) {
      const name = inert ? canonName : openName;
      const tot: Record<string, number> = {};
      for (const policy of ['compact', 'spike', 'cheapest'] as Policy[]) {
        const mags = seeds.map(seed => runSim({ seed, policy, hours, ...variant(inert) }).totalMags);
        tot[policy] = mean(mags);
        sRows.push([name, policy, mr(mags, 0)]);
      }
      ratios[inert ? 'scattered' : 'open'] = { spikeCompact: tot.spike / tot.compact, compactCheapest: tot.compact / tot.cheapest, compactTotal: tot.compact };
      sRows.push([name, 'spike/compact', (tot.spike / tot.compact).toFixed(2) + '×']);
      sRows.push([name, 'compact/cheapest', (tot.compact / tot.cheapest).toFixed(2) + '×']);
    }
    data.ratios = ratios;
    sections.push({ title: `E6-shape: spread on the ${openName} and the ${canonName} map (5 h total magazines)`, header: ['map', 'policy', 'total magazines'], rows: sRows });
    // validators × compact/river
    const rRows: (string | number)[][] = [];
    const gaps: Record<string, number> = {};
    const cases: [boolean, SimConfig['validator']][] = city
      ? [[false, 'none'], [true, 'none'], [true, 'inert-dark']]
      : [[false, 'none'], [true, 'none'], [true, 'no2x2'], [true, 'maxrun2'], [true, 'inert-dark']];
    for (const [inert, val] of cases) {
      const name = inert ? canonName : openName;
      const res: Record<string, number> = {};
      for (const policy of ['compact', 'river'] as Policy[]) {
        const v = variant(inert);
        const rs = seeds.map(seed => runSim({ seed, policy, hours, ...v, cfg: { ...(v.cfg ?? {}), validator: val } }));
        res[policy] = mean(rs.map(r => r.totalMags));
        rRows.push([name, val, policy, mr(rs.map(r => r.held), 0), mr(rs.map(r => r.front / r.held), 2), mr(rs.map(r => r.interior), 0), mr(rs.map(r => r.totalMags), 0), mr(rs.map(r => r.nScatter), 0)]);
      }
      const gap = res.river / res.compact - 1;
      gaps[`${inert ? 'scattered' : 'open'}-${val}`] = gap;
      rRows.push([name, val, 'river vs compact ammo', '', '', '', `${(100 * gap).toFixed(0)} %`, '']);
    }
    data.riverGap = gaps;
    sections.push({ title: 'E6-river: river-hugging vs compact under each validator', header: ['map', 'validator', 'policy', 'held', 'front/held', 'interior', 'total magazines', 'inert cells'], rows: rRows });
    checks.push(within(`§7 headline spike/compact on the ${canonName} (canonical) map`, ratios.scattered.spikeCompact, 1.2, 4.0, '×'));
    const wall = ratios.scattered.compactTotal / ratios.open.compactTotal;
    data.wallRatio = wall;
    if (city) checks.push(within('§9/§17 inert walls on the graph: compact with the plazas inert vs every plaza buildable (ratio; the lattice\'s 9 % scatter gave 0.3–0.8, the city\'s 8–10 % plazas are measured here)', wall, 0.5, 1.1, '×'));
    else checks.push(within('§9/§17 inert walls pay: compact on the scattered map uses less ammo than on open ground (ratio)', wall, 0.3, 0.8, '×'));
    checks.push(within(`§25 item 2: inert-hugging (the sim's river policy) is no cheaper than compact on the ${canonName} map`, gaps['scattered-none'], 0, 1.0, ' (fraction more)'));
    return { id: 'E6', title: E6.title, pyNames: ['E6-scatter-*', 'E6-noscatter-*', 'E13'], docRefs: ['§6', '§7', '§9'],
      setup: `compact, spike, cheapest, river; ${openName} and ${canonName} maps; production on`, sections, checks, data };
  },
};
