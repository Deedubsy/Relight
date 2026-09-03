/** E5 — early bite: demand ÷ production at 1, 3, 5 h per policy, base and the levers the Python runs tried. */
import { runSim } from '../run';
import { mr, mean, clean, within, Experiment, ExperimentResult, Section, Check } from '../util';
import { SimConfig, Policy } from '@relight/sim';

export const E5: Experiment = {
  id: 'E5', title: 'Early bite: demand ÷ production',
  run(ctx): ExperimentResult {
    const { seeds, hours } = ctx;
    const sections: Section[] = [], checks: Check[] = [], data: Record<string, unknown> = {};
    const variants: [string, Partial<SimConfig>][] = [
      ['base', {}], ['early-rate-10', { asmEarlyRate: 10 }], ['bloom-base-8', { bloomBase: 8 }], ['both', { asmEarlyRate: 10, bloomBase: 8 }],
      ['1-shot-asm-h1', { asmSchedule: [[600, 1], [5400, 2], [9000, 3], [12600, 4]] }], ['track-70pct', { asmTrack: 0.7 }],
    ];
    const rows: (string | number)[][] = [];
    for (const policy of ['compact', 'spike', 'cheapest', 'balanced'] as Policy[]) {
      for (const [vname, kw] of variants) {
        const rs = seeds.map(seed => runSim({ seed, policy, hours, cfg: kw }));
        const at = (h: number) => rs.map(r => r.ratios[h] ? r.ratios[h][0] / r.ratios[h][1] : NaN);
        const dem = (h: number) => rs.map(r => r.ratios[h] ? r.ratios[h][0] : NaN);
        const na = (h: number) => rs.map(r => r.asmAt[h] ?? NaN);
        const tag = `E5-${policy}-${vname}`;
        data[tag] = { ratio: { 1: at(1), 3: at(3), 5: at(5) }, demand: { 1: dem(1), 3: dem(3), 5: dem(5) }, assemblers: { 1: na(1), 3: na(3), 5: na(5) }, hqFell: rs.map(r => r.hqFell), lost: rs.map(r => r.lost) };
        rows.push([tag, mr(at(1), 2), mr(at(3), 2), mr(at(5), 2), mr(dem(1), 1), mr(dem(3), 1), mr(dem(5), 1), `${mr(na(1), 0)}/${mr(na(3), 0)}/${mr(na(5), 0)}`, rs.map(r => r.lost).join('/'), rs.map(r => r.hqFell < 0 ? '-' : (r.hqFell / 60).toFixed(0)).join('/')]);
      }
    }
    sections.push({ title: 'E5: demand ÷ production at the hour marks (demand = magazines demanded per minute over the last 10 min; production = assemblers × 20 mag/min)',
      note: 'a run whose HQ fell reads 0 from then on', header: ['run', 'ratio 1 h', 'ratio 3 h', 'ratio 5 h', 'demand 1 h', 'demand 3 h', 'demand 5 h', 'Shot assemblers 1/3/5 h', 'lost per seed', 'HQ fell at (min) per seed'], rows });
    const base = data['E5-compact-base'] as { ratio: Record<number, number[]> };
    const oneAsm = data['E5-compact-1-shot-asm-h1'] as { ratio: Record<number, number[]> };
    const spike = data['E5-spike-base'] as { ratio: Record<number, number[]> };
    checks.push(within('§11 one Shot assembler covers hour one of compact play (demand ÷ 20 mag/min at 1 h)', mean(clean(oneAsm.ratio[1])), 0.25, 0.6));
    checks.push(within('§12 compact play at 5 h runs its four assemblers at about half load', mean(clean(base.ratio[5])), 0.35, 0.75));
    checks.push(within('§24 a straight push reaches parity with four assemblers by 5 h', mean(clean(spike.ratio[5])), 0.85, 1.15));
    return { id: 'E5', title: E5.title, pyNames: ['E5-compact-*', 'E5-spike-*'], docRefs: ['§12', '§15', '§9'],
      setup: 'four policies, canonical map, production on, doc assembler schedule 1@10 min 2@30 3@50 4@3 h', sections, checks, data };
  },
};
