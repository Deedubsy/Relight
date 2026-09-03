/** E-variance — D6's irregular graph across many seeds. Neighbour-count and slot distributions of the generator,
 *  the ammo bill per Held block and the shape ratios (spike/compact, cheapest/compact) on the city against the
 *  archived lattice, on the same seeds. `--big` runs the brief's 10,000 seeds through the generator and 1,000 of
 *  them through the sim (the sim rows are the slow part); without it 200 and 30. */
import { runSim } from '../run';
import { mr, mean, within, isTrue, Experiment, ExperimentResult, Section, Check } from '../util';
import { generateCity, slotsOf, DISTRICT_NAMES } from '@relight/sim';

export const EVARIANCE: Experiment = {
  id: 'E-variance', title: 'Seed variance on the street-first city',
  run(ctx): ExperimentResult {
    const { hours, log } = ctx;
    const genN = ctx.big ? 10000 : 200, simN = ctx.big ? 1000 : 30;
    const sections: Section[] = [], checks: Check[] = [], data: Record<string, unknown> = {};
    // generator distribution
    const deg: Record<number, number> = {}, slots: Record<number, number> = {}, blocksN: number[] = [], inertF: number[] = [], hopsMax: number[] = [], attempts: number[] = [];
    const distr: Record<string, number[]> = { civ: [], res: [], ind: [], out: [] };
    let invalid = 0, degSum = 0, degN = 0;
    const invalidSeeds: number[] = [];   // named in the check, so a red run says which seed to look at
    const t0 = Date.now();
    for (let seed = 1; seed <= genN; seed++) {
      const g = generateCity(seed, 'river');
      if (!g.valid) { invalid++; invalidSeeds.push(seed); }
      attempts.push(g.attempt);
      const live = g.blocks.filter(b => !b.inert);
      blocksN.push(live.length); inertF.push((g.blocks.length - live.length) / g.blocks.length);
      let mh = 0; for (let i = 0; i < g.blocks.length; i++) if (g.hops[i] > mh) mh = g.hops[i];
      hopsMax.push(mh);
      const dc: Record<string, number> = { civ: 0, res: 0, ind: 0, out: 0 };
      for (const b of live) {
        const d = b.nb.filter(j => !g.blocks[j].inert).length;
        deg[d] = (deg[d] ?? 0) + 1; degSum += d; degN++;
        const s = slotsOf(b.area); slots[s] = (slots[s] ?? 0) + 1;
        dc[DISTRICT_NAMES[g.district[b.id]]]++;
      }
      for (const k in dc) distr[k].push(dc[k] / live.length);
      if (seed % 1000 === 0) log(`E-variance: ${seed}/${genN} seeds generated, ${((Date.now() - t0) / 1000).toFixed(0)} s`);
    }
    const degRows = Object.keys(deg).map(Number).sort((a, b) => a - b).map(d => [d, deg[d], (deg[d] / degN * 100).toFixed(1) + ' %']);
    sections.push({ title: `E-variance-neighbours: land-neighbour count per block over ${genN} seeds (River city)`, note: `mean ${(degSum / degN).toFixed(2)} neighbours; the lattice had 4 everywhere`,
      header: ['neighbours', 'blocks', 'share'], rows: degRows });
    const slotRows = Object.keys(slots).map(Number).sort((a, b) => a - b).map(s => [s, slots[s], (slots[s] / degN * 100).toFixed(1) + ' %']);
    sections.push({ title: 'E-variance-slots: machine slots per block (one per ~600 buildable tiles, at least one; the lattice lot had one)', header: ['slots', 'blocks', 'share'], rows: slotRows });
    sections.push({ title: 'E-variance-city: per-seed city size', header: ['metric', 'mean [min–max]'], rows: [
      ['live blocks', mr(blocksN, 0)], ['inert share', mr(inertF.map(x => x * 100), 1) + ' %'], ['max hops from the HQ', mr(hopsMax, 0)],
      ['civic share', mr(distr.civ.map(x => x * 100), 0) + ' %'], ['residential share', mr(distr.res.map(x => x * 100), 0) + ' %'], ['industrial share', mr(distr.ind.map(x => x * 100), 0) + ' %'], ['outskirts share', mr(distr.out.map(x => x * 100), 0) + ' %'],
      ['invalid after all attempts', `${invalid} / ${genN}`], ['attempts used', mr(attempts, 2)]] });
    data.generator = { genN, deg, slots, degMean: degSum / degN, invalid, invalidSeeds, blocks: mr(blocksN, 0), inertShare: mean(inertF) };
    // sim: ammo per held block and shape ratios, city vs lattice, same seeds
    const per: Record<string, number[]> = { city: [], lattice: [] }, spike: Record<string, number[]> = { city: [], lattice: [] }, cheap: Record<string, number[]> = { city: [], lattice: [] };
    const held: Record<string, number[]> = { city: [], lattice: [] }, lost: Record<string, number[]> = { city: [], lattice: [] };
    const t1 = Date.now();
    for (let seed = 1; seed <= simN; seed++) {
      for (const [k, map] of [['city', 'river'], ['lattice', 'lattice']] as const) {
        const c = runSim({ seed, policy: 'compact', hours, map }), s = runSim({ seed, policy: 'spike', hours, map }), ch = runSim({ seed, policy: 'cheapest', hours, map });
        per[k].push(c.totalMags / Math.max(1, c.held)); spike[k].push(s.totalMags / c.totalMags); cheap[k].push(ch.totalMags / c.totalMags);
        held[k].push(c.held); lost[k].push(c.lost);
      }
      if (seed % 100 === 0) log(`E-variance: ${seed}/${simN} seeds simulated, ${((Date.now() - t1) / 1000).toFixed(0)} s`);
    }
    sections.push({ title: `E-variance-ammo: compact 5 h on ${simN} seeds, city vs lattice`, header: ['map', 'magazines per Held block', 'held', 'lost', 'spike/compact', 'cheapest/compact'], rows: [
      ['city (River)', mr(per.city, 1), mr(held.city, 1), mr(lost.city, 2), mr(spike.city, 2) + '×', mr(cheap.city, 2) + '×'],
      ['lattice', mr(per.lattice, 1), mr(held.lattice, 1), mr(lost.lattice, 2), mr(spike.lattice, 2) + '×', mr(cheap.lattice, 2) + '×']] });
    data.sim = { simN, perHeld: { city: mean(per.city), lattice: mean(per.lattice) }, spikeCompact: { city: mean(spike.city), lattice: mean(spike.lattice) }, cheapestCompact: { city: mean(cheap.city), lattice: mean(cheap.lattice) },
                 lost: { city: mean(lost.city), lattice: mean(lost.lattice) }, held: { city: mean(held.city), lattice: mean(held.lattice) } };
    checks.push(isTrue('D6: every seed validates within its attempt budget', invalid === 0, `${invalid} invalid of ${genN}${invalidSeeds.length ? ` (seed${invalidSeeds.length > 1 ? 's' : ''} ${invalidSeeds.slice(0, 8).join(', ')})` : ''}`));
    checks.push(within('D6: 3–7 neighbours is the bulk of the city (share of blocks)', [3, 4, 5, 6, 7].reduce((a, d) => a + (deg[d] ?? 0), 0) / degN * 100, 85, 100, ' %'));
    checks.push(within('§7/§9 spike still costs 2–3.5× compact on the graph', mean(spike.city), 2.0, 3.5, '×'));
    checks.push(within('city ammo per Held block within ±35 % of the lattice', mean(per.city) / mean(per.lattice), 0.65, 1.35, '×'));
    return { id: 'E-variance', title: EVARIANCE.title, pyNames: ['nightly'], docRefs: ['§7', '§9', '§17', '§18', 'D6'],
      setup: `${genN} seeds through the generator, ${simN} through compact/spike/cheapest 5 h on both maps`, sections, checks, data };
  },
};
