/** RI-04 (plan §6; GDD §28.5): emergence points — where a neighbourhood's rot comes out onto the street.
 *
 *  One point per (block, shared street): the block's frontage on that street — its kerb tiles there (`kerb`: the
 *  street tiles the block's own watershed labels, `Ground.near`, 4-adjacent to one of its lot tiles that faces the
 *  street, `CitySeg.frontA` / `frontB`), anchored at the kerb tile nearest the street's midpoint (`tx`, `ty`: the
 *  tile the map marks and the hover names). Ids are stable: numbered in (block, neighbour) order from the city's
 *  geometry alone, so a rebuilt or reloaded state has the same ids on the same tiles, and nothing about them is
 *  saved. The tile layer births a body on the *Dark* block's frontage for the engagement's edge (threat.ts `spawn`,
 *  a seeded hash along `kerb`), so a birth tile is always one the Dark block's street owns — never a tile the Held
 *  block owns or faces (§6: "no spawn inside a secured interior"); a Held block's points are quiet. A point is
 *  *active*, and drawn, while its block is Dark and the neighbour across the street is Held or Contested
 *  ("discovered sites show local activity and likely approaches").
 *  Implementation default (RI-04): the frontage's kerb, where M4 used the same seeded hash along the ridge (the
 *  street's centre line) — a body now walks the street's width before it reaches the Held frontage. A first cut
 *  birthed every body of an edge on the anchor tile alone; that stacked an edge's whole stream on the turret pair
 *  and on a rescuing engineer standing at the street midpoint (E-rifle's tile rescue knocked the engineer down on
 *  seed 5, the steady hour on seed 4 gained a down) — a balance shift RI-04 does not own, so the spread stays. */
import { SimState, DARK, HELD, CONTESTED } from './types';
import { Ground, ground, inGround, cityGeomOf } from './ground';
import { passable } from './walk';
import { NX, NY } from './move';

export interface EmergencePoint { id: number; block: number; other: number; tx: number; ty: number; /** the frontage's kerb tiles (tile indices, ascending) — where its bodies are born */ kerb: number[] }
export interface Emergence { points: EmergencePoint[]; index: Map<number, number> }

const cache = new WeakMap<Ground, Emergence>();

/** Every emergence point of the city, derived once per ground (empty on the lattice). */
export function emergence(st: SimState): Emergence {
  const G = ground(st);
  let e = cache.get(G);
  if (!e) { e = build(st, G); cache.set(G, e); }
  return e;
}

function build(st: SimState, G: Ground): Emergence {
  const points: EmergencePoint[] = [], index = new Map<number, number>();
  if (st.lattice) return { points, index };
  const cg = cityGeomOf(st), n = cg.blocks.length, tw = G.tw;
  const pairs: [number, number, number][] = [];
  cg.segs.forEach((sg, k) => { pairs.push([sg.a, sg.b, k]); pairs.push([sg.b, sg.a, k]); });
  pairs.sort((p, q) => p[0] - q[0] || p[1] - q[1]);
  for (const [block, other, k] of pairs) {
    const sg = cg.segs[k], front = sg.a === block ? sg.frontA : sg.frontB;
    let bt = -1, bd = Infinity;
    const kerbSet = new Set<number>();
    const consider = (t: number): void => {
      kerbSet.add(t);
      const x = t % tw, y = Math.floor(t / tw), d = Math.hypot(x + 0.5 - sg.mx, y + 0.5 - sg.my);
      if (d < bd - 1e-9) { bd = d; bt = t; }
    };
    for (const lt of front) {
      const x = lt % tw, y = Math.floor(lt / tw);
      for (let q = 0; q < 4; q++) {
        const xx = x + NX[q], yy = y + NY[q];
        if (!inGround(G, xx, yy)) continue;
        const t = yy * tw + xx;
        if (G.owner[t] === -1 && G.near[t] === block) consider(t);
      }
    }
    if (bt < 0) for (const t of sg.ridge) if (G.near[t] === block) consider(t);   // a frontage without a kerb (odd geometry): the ridge on this block's side
    if (bt < 0) for (const t of sg.ridge) consider(t);
    if (bt < 0) continue;
    index.set(block * n + other, points.length);
    points.push({ id: points.length + 1, block, other, tx: bt % tw, ty: Math.floor(bt / tw), kerb: [...kerbSet].sort((a, b) => a - b) });
  }
  return { points, index };
}

/** Block `block`'s point on the street it shares with `other`. */
export function emergencePoint(st: SimState, block: number, other: number): EmergencePoint | undefined {
  const e = emergence(st), k = e.index.get(block * st.blocks.length + other);
  return k === undefined ? undefined : e.points[k];
}
export function emergencePointsOf(st: SimState, block: number): EmergencePoint[] {
  return emergence(st).points.filter(p => p.block === block);
}
/** The points a player can read now: a Dark block's points facing a Held or Contested block. */
export function activeEmergencePoints(st: SimState): EmergencePoint[] {
  const B = st.blocks;
  return emergence(st).points.filter(p => B[p.block].state === DARK && (B[p.other].state === HELD || B[p.other].state === CONTESTED));
}
/** Where a body of this point is born: the kerb tile `u` (in [0, 1), the caller's seeded hash) picks along the
 *  frontage, or the next passable kerb tile after it in the list (wrapping), else the anchor tile itself. */
export function birthTile(st: SimState, p: EmergencePoint, u = 0): [number, number] {
  const G = ground(st), tw = G.tw, n = p.kerb.length;
  const k0 = Math.min(n - 1, Math.max(0, Math.floor(u * n)));
  for (let k = 0; k < n; k++) {
    const t = p.kerb[(k0 + k) % n], x = t % tw, y = Math.floor(t / tw);
    if (passable(st, x, y)) return [x, y];
  }
  return [p.tx, p.ty];
}
