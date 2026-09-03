/** D6: the city is a street graph. Blocks are nodes; an edge is a shared street segment. This module holds the
 *  graph helpers the block sim, bots and queries share; nothing here mutates game state.
 *
 *  Edge ids: `a * deg + k` where k is the slot of the neighbour in `nb[a]` (ascending neighbour index). On the
 *  lattice (deg 4, neighbours W, N, S, E in ascending index) the slot order is the Python ring order, so the
 *  frozen fixtures still replay bit for bit; nothing in the sim reads a compass direction from an id any more. */
import type { SimState, GraphSpec } from './types';

/** Lattice adjacency for a w×h grid indexed x*h+y, neighbours in ascending index order (W, N, S, E). */
export function latticeGraph(w: number, h: number): GraphSpec {
  const nb: number[][] = [], len: number[][] = [], area: number[] = [];
  for (let x = 0; x < w; x++) for (let y = 0; y < h; y++) {
    const ns: number[] = [], ls: number[] = [];
    if (x > 0) { ns.push((x - 1) * h + y); ls.push(LATTICE_SEG); }
    if (y > 0) { ns.push(x * h + y - 1); ls.push(LATTICE_SEG); }
    if (y < h - 1) { ns.push(x * h + y + 1); ls.push(LATTICE_SEG); }
    if (x < w - 1) { ns.push((x + 1) * h + y); ls.push(LATTICE_SEG); }
    nb.push(ns); len.push(ls); area.push(LATTICE_AREA);
  }
  return { nb, len, area, inert: [], pitch: LATTICE_PITCH };
}
/** The lattice lot is 24×24 buildable tiles behind a 4-tile margin in a 32-tile cell (tiles.ts). */
export const LATTICE_AREA = 24 * 24, LATTICE_SEG = 24, LATTICE_PITCH = 32;

export const slotOf = (st: SimState, a: number, b: number): number => st.nb[a].indexOf(b);
export const edgeId = (st: SimState, a: number, b: number): number => { const k = slotOf(st, a, b); return k < 0 ? -1 : a * st.deg + k; };
export const edgeFrom = (st: SimState, id: number): number => Math.floor(id / st.deg);
export const edgeTo = (st: SimState, id: number): number => st.nb[Math.floor(id / st.deg)][id % st.deg];

/** BFS hops over every block (state ignored). On a full lattice this is the Manhattan distance. */
export function bfsHops(nb: number[][], src: number): Int32Array {
  const d = new Int32Array(nb.length).fill(-1);
  const q = [src]; d[src] = 0;
  for (let qi = 0; qi < q.length; qi++) {
    const i = q[qi], ns = nb[i];
    for (let k = 0; k < ns.length; k++) { const j = ns[k]; if (d[j] < 0) { d[j] = d[i] + 1; q.push(j); } }
  }
  return d;
}

/** BFS hops from any of `srcs` (multi-source). */
export function bfsHopsMulti(nb: number[][], srcs: number[]): Int32Array {
  const d = new Int32Array(nb.length).fill(-1);
  const q = srcs.slice(); for (const s of srcs) d[s] = 0;
  for (let qi = 0; qi < q.length; qi++) {
    const i = q[qi], ns = nb[i];
    for (let k = 0; k < ns.length; k++) { const j = ns[k]; if (d[j] < 0) { d[j] = d[i] + 1; q.push(j); } }
  }
  return d;
}

const hopCache = new WeakMap<number[][], Map<number, Int32Array>>();
/** Cached BFS hops from block `src`. Keyed on the adjacency array, so a cloned state gets its own cache. */
export function hopsFrom(st: SimState, src: number): Int32Array {
  let m = hopCache.get(st.nb);
  if (!m) { m = new Map(); hopCache.set(st.nb, m); }
  let d = m.get(src);
  if (!d) { d = bfsHops(st.nb, src); m.set(src, d); }
  return d;
}

/** Street distance in tiles between block centroids: Dijkstra over centroid-to-centroid lengths.
 *  GAME-ASSUMPTION: an engineer walks block centre to block centre along the streets; the true street path is a
 *  little longer (corners) and a little shorter (cutting across a held lot). */
const lenCache = new WeakMap<number[][], Map<number, Float64Array>>();
export function walkFrom(st: SimState, src: number): Float64Array {
  let m = lenCache.get(st.nb);
  if (!m) { m = new Map(); lenCache.set(st.nb, m); }
  let d = m.get(src);
  if (d) return d;
  const n = st.nb.length, B = st.blocks;
  d = new Float64Array(n).fill(Infinity); d[src] = 0;
  const done = new Uint8Array(n);
  // simple O(n²) selection is fine at n ≈ 500 and the result is cached per source
  for (;;) {
    let u = -1, best = Infinity;
    for (let i = 0; i < n; i++) if (!done[i] && d[i] < best) { best = d[i]; u = i; }
    if (u < 0) break;
    done[u] = 1;
    const ns = st.nb[u];
    for (let k = 0; k < ns.length; k++) {
      const v = ns[k];
      const w = Math.hypot(B[v].x - B[u].x, B[v].y - B[u].y) * st.tileScale;
      if (d[u] + w < d[v]) d[v] = d[u] + w;
    }
  }
  m.set(src, d);
  return d;
}
export const walkLen = (st: SimState, a: number, b: number): number => walkFrom(st, a)[b];

/** Tile position of a block's centre: lattice cells are 32 tiles with the block at (x,y); a city block's (x,y)
 *  is already its centroid tile. */
export function blockCentre(st: SimState, i: number): [number, number] {
  const b = st.blocks[i];
  return st.lattice ? [b.x * LATTICE_PITCH + LATTICE_PITCH / 2, b.y * LATTICE_PITCH + LATTICE_PITCH / 2] : [b.x, b.y];
}
