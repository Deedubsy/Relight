/** E8 — cadence: territory by hour per claim cadence, and the check that the locked cadence (C2, D-P3-6: one claim per
 *  5 min after hour one) still reproduces §18's counts. Until Phase 3 the target was the hand-drawn 24×22 sketches
 *  (52/24/36 and 313/67/270); since Phase 3 §18 is generated from the sim at the locked cadence (seed 3, `npm run
 *  docsync`) and the target is the three-seed mean the doc's §18 caption states, so this check pins the doc, not a sketch. */
import { runSim } from '../run';
import { mr, mean, within, Experiment, ExperimentResult, Section, Check } from '../util';

export const E8: Experiment = {
  id: 'E8', title: 'Claim cadence behind §18',
  run(ctx): ExperimentResult {
    const { seeds } = ctx;
    const sections: Section[] = [], checks: Check[] = [], data: Record<string, unknown> = {};
    const target = { h5: [52, 19, 36], h25: [292, 43, 253] };   // §18 caption, three-seed means at gap 5 min (C2 locked, D-P3-6)
    const rows: (string | number)[][] = [];
    const errs: Record<number, number> = {}, errsHI: Record<number, number> = {};
    const hourly: Record<number, Record<number, number[]>> = {};
    for (const gapAfter of [240, 300, 360, 480]) {
      const rs = seeds.map(seed => runSim({ seed, policy: 'compact', hours: 25, gapAfter, cfg: { production: false } }));
      const at = (h: number, f: 'held' | 'front' | 'interior') => rs.map(r => r.hourly[h - 1][f]);
      hourly[gapAfter] = {}; for (const h of [1, 2, 3, 5, 8, 10, 12, 15, 20, 25]) hourly[gapAfter][h] = [mean(at(h, 'held')), mean(at(h, 'front')), mean(at(h, 'interior'))];
      const m5 = [mean(at(5, 'held')), mean(at(5, 'front')), mean(at(5, 'interior'))], m25 = [mean(at(25, 'held')), mean(at(25, 'front')), mean(at(25, 'interior'))];
      const err = Math.max(...m5.map((v, i) => Math.abs(v / target.h5[i] - 1)), ...m25.map((v, i) => Math.abs(v / target.h25[i] - 1)));
      errs[gapAfter] = err;
      errsHI[gapAfter] = Math.max(Math.abs(m5[0] / target.h5[0] - 1), Math.abs(m5[2] / target.h5[2] - 1), Math.abs(m25[0] / target.h25[0] - 1), Math.abs(m25[2] / target.h25[2] - 1));
      data[`gap${gapAfter}`] = { h5: m5, h25: m25, maxErr: err, claims: rs.map(r => r.claims) };
      rows.push([gapAfter / 60, mr(at(5, 'held'), 0), mr(at(5, 'front'), 0), mr(at(5, 'interior'), 0), mr(at(25, 'held'), 0), mr(at(25, 'front'), 0), mr(at(25, 'interior'), 0), (100 * err).toFixed(0) + ' %']);
    }
    rows.push(['§18 caption (gap 5, locked)', ...target.h5, ...target.h25, '']);
    data.hourly = hourly;
    const hRows: (string | number)[][] = [];
    for (const h of [1, 2, 3, 5, 8, 10, 12, 15, 20, 25]) hRows.push([h, ...[240, 300, 360, 480].map(g => hourly[g][h].map(v => v.toFixed(0)).join(' / '))]);
    sections.push({ title: 'E8-hourly: territory by hour (held / front / interior, mean of seeds) per cadence', header: ['hour', 'gap 4 min', 'gap 5 min', 'gap 6 min', 'gap 8 min'], rows: hRows });
    // §5 well death at the §18 cadence
    const wRows: (string | number)[][] = [];
    const wd = seeds.map(seed => runSim({ seed, policy: 'compact', hours: 25, gapAfter: 300, cfg: { production: false, wellDeath: true } }));
    const hrs = (a: number[]) => a.map(t => (t / 3600).toFixed(1)).join(', ') || '-';
    const firstWell = wd.map(r => Math.min(...r.wellDeadAt, ...r.wellHeldAt) / 3600);
    for (let i = 0; i < seeds.length; i++) wRows.push([seeds[i], wd[i].wellsDead, hrs(wd[i].wellDeadAt), wd[i].wellHeldAt.length, hrs(wd[i].wellHeldAt), wd[i].held, wd[i].totalMags.toFixed(0)]);
    data.wellDeath = { dead: wd.map(r => r.wellsDead), at: wd.map(r => r.wellDeadAt), claimed: wd.map(r => r.wellHeldAt), firstWell, mags: wd.map(r => r.totalMags) };
    sections.push({ title: 'E8-wells: gap 5 min with the §5 well-death rule on (a well dies 5 min after its last neighbour is Held). The compact bot claims the well block itself before it is ever enclosed, so the rule never fires; a well is neutralised when claimed or dead', header: ['seed', 'wells dead by 25 h', 'died at (h)', 'wells claimed by 25 h', 'claimed at (h)', 'held 25 h', 'total magazines'], rows: wRows });
    sections.push({ title: 'E8-cadence: compact, canonical map, 25 h, no ammo line; one claim per 15 min in hour one, then one per gap',
      header: ['gap after h1 (min)', 'held 5 h', 'front 5 h', 'interior 5 h', 'held 25 h', 'front 25 h', 'interior 25 h', 'worst deviation from §18'], rows });
    const best = Object.entries(errs).sort((a, b) => a[1] - b[1])[0];
    data.best = { gapAfter: +best[0], maxErr: best[1], heldInteriorErr: errsHI[+best[0]] };
    checks.push(within(`§18 held and interior counts reproduced at the ${+best[0] / 60}-min cadence within 10 %`, errsHI[+best[0]], 0, 0.10));
    checks.push(within('the locked cadence (5 min, C2) is the one that best matches §18', +best[0], 300, 300, ' s'));
    checks.push(within('first well neutralised (claimed or dead) at the §18 cadence, mean of seeds; §15 said hours 6–9, the sim says 13–24 h', mean(firstWell.filter(x => isFinite(x))), 10, 25, ' h'));
    return { id: 'E8', title: E8.title, pyNames: ['E9 claim cadence (phase5.py, retired Phase 3)'], docRefs: ['§18'],
      setup: 'compact, canonical map, production off, 25 h', sections, checks, data };
  },
};
