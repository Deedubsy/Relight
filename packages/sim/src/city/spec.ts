/** D6 — a city as a MapSpec: one cell per block at its pole tile, the street graph, and the §7 district numbers
 *  re-run per graph hop instead of per lattice block. */
import { CellSpec, District, GraphSpec, MapSpec, SimConfig } from '../types';
import { rngUniform, seedRng } from '../prng';
import { DEPTH_PER_BLOCK, WELL_CAP_BONUS, WELL_G_MULT, WELL_RANGE, districtOf } from '../districts';
import { bfsHops } from '../graph';
import { CityGeom } from './geom';
import { CityPreset, CityOpts, DISTRICT_NAMES, generateCity } from './generate';

/** §7 depth and well rules on the graph: cap × (1 + DEPTH_PER_BLOCK × hops from the HQ), well influence
 *  1 − hops/4 within WELL_RANGE hops (GAME-ASSUMPTION: a hop is a block; the lattice's Manhattan block was one hop). */
export function cityCells(g: CityGeom, cfg: Pick<SimConfig, 'jitter'>, seed: number): CellSpec[] {
  const nbList = g.blocks.map(b => b.nb);
  const wellHops = g.wells.map(w => bfsHops(nbList, w));
  const jr = { rng: seedRng(Math.imul(seed, 104729) + 7) };
  const cells: CellSpec[] = [];
  for (const b of g.blocks) {
    const name = DISTRICT_NAMES[g.district[b.id]] as District, r = districtOf(name);
    const hops = Math.max(0, g.hops[b.id]);
    const base = Math.min(1, r.base * (1 + DEPTH_PER_BLOCK * hops));
    let infl = 0, well = false;
    for (const wh of wellHops) {
      const wd = wh[b.id];
      if (wd >= 0 && wd <= WELL_RANGE) { infl = Math.max(infl, 1 - wd / 4); well = true; }
    }
    const dmax = Math.min(1, base + WELL_CAP_BONUS * infl), gg = r.g * (1 + WELL_G_MULT * infl);
    const d0 = cfg.jitter ? 0.5 * dmax * (1 + rngUniform(jr, -cfg.jitter, cfg.jitter)) : 0.5 * dmax;
    cells.push({ x: b.cx, y: b.cy, name, well, dmax, g: gg, d0, base, gBase: r.g });
  }
  return cells;
}

export function cityGraph(g: CityGeom, plazas: 'inert' | 'buildable' = 'inert'): GraphSpec {
  const n = g.blocks.length;
  const nb = g.blocks.map(b => b.nb.slice());
  const len = g.blocks.map(b => b.nb.map(j => g.segs[g.segAt.get(j > b.id ? b.id * n + j : j * n + b.id)!].len));
  return { nb, len, area: g.blocks.map(b => b.area), inert: plazas === 'inert' ? g.blocks.filter(b => b.inert).map(b => b.id) : [], pitch: 1 };
}

/** The MapSpec of a city seed. `cfg.scatter` is ignored: a city's inert faces are its plazas (D6). `plazas: 'buildable'`
 *  is E6's counterfactual — the same streets with every face a lot, so "do inert walls pay" can be asked on the graph. */
export function citySpec(seed: number, preset: CityPreset, cfg: Pick<SimConfig, 'jitter'>, opts: CityOpts = {}, plazas: 'inert' | 'buildable' = 'inert'): MapSpec {
  const g = generateCity(seed, preset, opts);
  const at = (i: number): [number, number] => [g.blocks[i].cx, g.blocks[i].cy];
  return {
    w: g.tw, h: g.th, start: at(g.hq), target: at(g.foundry), wells: g.wells.map(at),
    cells: cityCells(g, cfg, seed), scatteredInert: [],
    facilities: g.facilities.map(f => ({ name: f.name, x: g.blocks[f.block].cx, y: g.blocks[f.block].cy })),
    survivors: g.survivors.map(s => ({ name: s.name, tag: s.tag, x: g.blocks[s.block].cx, y: g.blocks[s.block].cy })),
    graph: cityGraph(g, plazas), city: { seed, preset, tw: g.tw, th: g.th },
  };
}
