/** Prompt B M1 — the ground on faces (D6). One derivation per state, cached on `st.blocks`: every tile's base kind
 *  and owner, each block's buildable tiles, substation, streetlights and rubble layout in dig order (global tile
 *  indices), the HQ lot's origin, and the 32×32 chunk → block overlap lists the renderer keys its cache on. The
 *  lattice (Phase 4's 32×32 cells, kept until the Phase 5 gate deletes it) is wrapped into the same shape from
 *  tiles.ts so the flow layer and the world view have one code path. Dig state (rubble left, dug tiles) is read
 *  from the block sim and the flow layer at query time; the derivation itself depends only on the seed and the
 *  geometry, never on time. */
import { SimState, Block, INERT, VOID } from './types';
import { hash01 } from './prng';
import { LATTICE_AREA } from './graph';
import { generateCity, CityGeom, CityPreset, CitySeg, STREET, WATER, segBetween, frontTiles } from './city';
import { poolMax } from './queries';
import { blockAtHook, REACH } from './engineer';
import {
  CELL_TILES, LOT_TILES, MARGIN_TILES, T_STREET, T_GROUND, T_RUBBLE, T_INERT, T_RIVER, T_DEPOSIT, T_PATCH,
  HQ_PATCHES, hqReserved, HQ_RUBBLE_TILES, HQ_CLEAR_ROWS, SUBSTATION_TILES, HQ_SUBSTATION, substationLot,
  STREETLIGHT_BROKEN, Streetlight, streetlights, RUBBLE_TILES_MIN, RUBBLE_TILES_MAX, RUBBLE_VARIANTS,
  DEPOSIT_CELL_FRACTION, DEPOSIT_TILES, DEPOSIT_IRON_FRACTION, CLUSTERS, SIGMA, RubbleType, DepositType, rubbleOf,
  depthFrac, lotLayout, isMargin, TILE_NAMES, PATCH_NAMES,
} from './tiles';

export const CHUNK = 32;    // renderer chunk side in tiles (a lattice cell exactly)

export interface Substation { x: number; y: number; size: number }

export interface BlockGround {
  i: number;
  /** The block's buildable tiles (global index ty * tw + tx); empty for the lattice's river row. */
  tiles: Int32Array;
  x0: number; y0: number; x1: number; y1: number;
  /** Substation footprint (3×3; a face too thin for one gets a 1×1 stand-in on its pole tile: GAME-ASSUMPTION). */
  sub: Substation | null;
  lights: Streetlight[];
  rubble: RubbleType | null; deposit: DepositType | null;
  /** Rubble (or deposit) tiles laid down. */
  count: number;
  /** Global tile indices in digging order: the thinnest heap edge goes first. */
  order: Int32Array;
  hq: boolean;
  /** Where a walk to the block ends: the pole of inaccessibility on a city, the cell centre on the lattice. */
  pole: [number, number];
}

export interface Ground {
  tw: number; th: number;
  lattice: boolean;
  /** T_STREET | T_GROUND | T_RIVER per tile, state-free. */
  base: Uint8Array;
  /** Block index per tile, -1 street, -2 water. */
  owner: Int32Array;
  /** The block a tile belongs to for the flow layer: its owner, or on a street the block whose street it is
   *  (the lattice cell; the city's watershed label); -1 on water. */
  near: Int32Array;
  /** Dig rank inside the owner's order, -1 where no rubble was laid (reserved tiles included). */
  rank: Int32Array;
  variant: Uint8Array;
  /** P_* on the HQ lot's patches. */
  patch: Uint8Array;
  blocks: BlockGround[];
  /** Tile of HQ lot (0, 0): the 24×24 frame the §11 patches, the Depot, the start turrets and the substation sit in. */
  hqOrigin: [number, number];
  cw: number; ch: number;
  /** Per chunk (cy * cw + cx): the blocks with tiles inside it. */
  chunks: Int32Array[];
}

const NONE: readonly number[] = [];
const cache = new WeakMap<Block[], Ground>();

/** The ground for a state (derived once per state; ~50 ms for an 800×800 city, see tiles.test.ts). */
/** The city geometry a city state was generated from (cached by generateCity). */
export function cityGeomOf(st: SimState): CityGeom {
  const c = st.city!;
  return generateCity(c.seed, c.preset as CityPreset, { tw: c.tw, th: c.th });
}

export function ground(st: SimState): Ground {
  let g = cache.get(st.blocks);
  if (!g) { g = st.lattice ? latticeGround(st) : cityGround(st); cache.set(st.blocks, g); }
  return g;
}

function alloc(tw: number, th: number, lattice: boolean): Ground {
  const n = tw * th;
  return {
    tw, th, lattice, base: new Uint8Array(n), owner: new Int32Array(n).fill(-2), near: new Int32Array(n).fill(-1),
    rank: new Int32Array(n).fill(-1), variant: new Uint8Array(n), patch: new Uint8Array(n), blocks: [], hqOrigin: [0, 0],
    cw: Math.ceil(tw / CHUNK), ch: Math.ceil(th / CHUNK), chunks: [],
  };
}

const RESERVED = -2;   // rank marker while laying out: the substation, the HQ's Depot, patches and clear rows

// ------------------------------------------------------------------ lattice

function latticeGround(st: SimState): Ground {
  const tw = st.w * CELL_TILES, th = st.h * CELL_TILES, G = alloc(tw, th, true);
  G.hqOrigin = [st.start[0] * CELL_TILES + MARGIN_TILES, st.start[1] * CELL_TILES + MARGIN_TILES];
  for (let x = 0; x < st.w; x++) for (let y = 0; y < st.h; y++) {
    const i = x * st.h + y, b = st.blocks[i], river = y === st.h - 1, ox = x * CELL_TILES, oy = y * CELL_TILES;
    const hq = x === st.start[0] && y === st.start[1];
    const tiles = river ? new Int32Array(0) : new Int32Array(LOT_TILES * LOT_TILES);
    let n = 0;
    for (let ly = 0; ly < CELL_TILES; ly++) for (let lx = 0; lx < CELL_TILES; lx++) {
      const t = (oy + ly) * tw + ox + lx;
      if (river) { G.base[t] = ly < MARGIN_TILES ? T_STREET : T_RIVER; G.owner[t] = ly < MARGIN_TILES ? -1 : -2; G.near[t] = ly < MARGIN_TILES ? i : -1; }
      else if (isMargin(lx, ly)) { G.base[t] = T_STREET; G.owner[t] = -1; G.near[t] = i; }
      else { G.base[t] = T_GROUND; G.owner[t] = i; G.near[t] = i; tiles[n++] = t; }
    }
    const sl = river ? null : substationLot(st.seed, b, hq);
    const bg: BlockGround = {
      i, tiles, x0: ox + MARGIN_TILES, y0: oy + MARGIN_TILES, x1: ox + MARGIN_TILES + LOT_TILES - 1, y1: oy + MARGIN_TILES + LOT_TILES - 1,
      sub: sl ? { x: ox + MARGIN_TILES + sl[0], y: oy + MARGIN_TILES + sl[1], size: SUBSTATION_TILES } : null,
      lights: river ? [] : streetlights(st.seed, b), rubble: null, deposit: null, count: 0, order: new Int32Array(0), hq,
      pole: [ox + CELL_TILES / 2, oy + CELL_TILES / 2],
    };
    if (!river && b.state !== INERT && b.state !== VOID) {
      const lay = lotLayout(st.seed, b, hq);
      bg.rubble = lay.rubble; bg.deposit = lay.deposit; bg.count = lay.tiles;
      bg.order = new Int32Array(lay.tiles);
      for (let r = 0; r < lay.tiles; r++) {
        const li = lay.order[r], lx = li % LOT_TILES, ly = (li - lx) / LOT_TILES, t = (oy + MARGIN_TILES + ly) * tw + ox + MARGIN_TILES + lx;
        bg.order[r] = t; G.rank[t] = r; G.variant[t] = lay.variant[li];
      }
    }
    if (hq && !river) markPatches(G, bg);
    G.blocks.push(bg);
  }
  buildChunks(G);
  return G;
}

// ------------------------------------------------------------------ city

function cityGround(st: SimState): Ground {
  const c = st.city!, cg = generateCity(c.seed, c.preset as CityPreset, { tw: c.tw, th: c.th });
  const tw = cg.tw, th = cg.th, G = alloc(tw, th, false);
  for (let t = 0; t < tw * th; t++) {
    const k = cg.kind[t];
    if (k === STREET) { G.base[t] = T_STREET; G.owner[t] = -1; G.near[t] = cg.near[t]; }
    else if (k === WATER) { G.base[t] = T_RIVER; G.owner[t] = -2; G.near[t] = -1; }
    else { G.base[t] = T_GROUND; G.owner[t] = cg.owner[t]; G.near[t] = cg.owner[t]; }
  }
  const hqb = cg.blocks[cg.hq];
  G.hqOrigin = [hqb.sqx, hqb.sqy];
  for (let i = 0; i < cg.blocks.length; i++) {
    const cb = cg.blocks[i], b = st.blocks[i], hq = i === cg.hq;
    const inert = cb.inert || b.state === INERT || b.state === VOID;
    const bg: BlockGround = {
      i, tiles: cb.tiles, x0: cb.x0, y0: cb.y0, x1: cb.x1, y1: cb.y1, sub: null, lights: [], rubble: null, deposit: null,
      count: 0, order: new Int32Array(0), hq, pole: [cb.cx, cb.cy],
    };
    bg.sub = hq ? { x: hqb.sqx + HQ_SUBSTATION[0], y: hqb.sqy + HQ_SUBSTATION[1], size: SUBSTATION_TILES } : inert ? null : faceSubstation(G, cg, i);
    if (!inert) bg.lights = faceLights(G, cg, st.seed, i);
    if (bg.sub) for (let dy = 0; dy < bg.sub.size; dy++) for (let dx = 0; dx < bg.sub.size; dx++) G.rank[(bg.sub.y + dy) * tw + bg.sub.x + dx] = RESERVED;
    if (hq) {
      for (let ly = 0; ly < LOT_TILES; ly++) for (let lx = 0; lx < LOT_TILES; lx++) if (hqReserved(lx, ly)) G.rank[(hqb.sqy + ly) * tw + hqb.sqx + lx] = RESERVED;
      markPatches(G, bg);
    }
    if (!inert) faceLayout(G, st, i, b, bg);
    G.blocks.push(bg);
  }
  for (let t = 0; t < tw * th; t++) if (G.rank[t] === RESERVED) G.rank[t] = -1;
  buildChunks(G);
  return G;
}

/** D-B1-4: the face's substation is the 3×3 fully inside the face nearest the lot's centroid (the mean of its tiles);
 *  a face with no room for one gets a 1×1 on its pole tile (GA-B1-1). */
function faceSubstation(G: Ground, cg: CityGeom, i: number): Substation {
  const cb = cg.blocks[i], tw = G.tw;
  let mx = 0, my = 0;
  for (let k = 0; k < cb.tiles.length; k++) { const t = cb.tiles[k], tx = t % tw; mx += tx + 0.5; my += (t - tx) / tw + 0.5; }
  mx /= cb.tiles.length; my /= cb.tiles.length;
  let bestT = -1, bestD = Infinity;
  for (let k = 0; k < cb.tiles.length; k++) {
    const t = cb.tiles[k], tx = t % tw, ty = (t - tx) / tw;
    if (tx + SUBSTATION_TILES > tw || ty + SUBSTATION_TILES > G.th) continue;
    let ok = true;
    for (let dy = 0; dy < SUBSTATION_TILES && ok; dy++) for (let dx = 0; dx < SUBSTATION_TILES; dx++) if (G.owner[(ty + dy) * tw + tx + dx] !== i) { ok = false; break; }
    if (!ok) continue;
    const ddx = tx + 1.5 - mx, ddy = ty + 1.5 - my, d = ddx * ddx + ddy * ddy;
    if (d < bestD) { bestD = d; bestT = t; }
  }
  if (bestT < 0) return { x: cb.cx, y: cb.cy, size: 1 };
  return { x: bestT % tw, y: (bestT - bestT % tw) / tw, size: SUBSTATION_TILES };
}

/** GAME-ASSUMPTION (D-B1-4): a face's streetlights stand along each street segment's kerb (the street tiles on the
 *  face's side of the watershed that border its lot), one every FACE_LIGHT_STEP tiles along the segment, so a
 *  segment's count comes from its length (a 32-tile segment gets 8, the lattice's count); 3 in 8 broken as on the
 *  lattice (§13). This replaces the lattice's "8 per side". */
export const FACE_LIGHT_STEP = 4;

/** The ridge's unit direction (from its midpoint to its farthest tile), for ordering tiles along a segment. */
export function segAxis(sg: CitySeg, tw: number): [number, number] {
  let ax = 1, ay = 0, bd = -1;
  for (let k = 0; k < sg.ridge.length; k++) {
    const t = sg.ridge[k], tx = t % tw, ty = (t - tx) / tw, d = Math.hypot(tx - sg.mx, ty - sg.my);
    if (d > bd) { bd = d; ax = tx - sg.mx; ay = ty - sg.my; }
  }
  const L = Math.hypot(ax, ay) || 1;
  return [ax / L, ay / L];
}

/** D-B1-4: a segment's length in tiles along its axis (the extent of its ridge's projection on `segAxis`). `CitySeg.len`
 *  counts ridge tiles, which is about twice this on a two-wide ridge; the per-length rules (§5, §13) use this. */
export function segLength(sg: CitySeg, tw: number): number {
  const [ux, uy] = segAxis(sg, tw);
  let lo = Infinity, hi = -Infinity;
  for (let k = 0; k < sg.ridge.length; k++) {
    const t = sg.ridge[k], tx = t % tw, ty = (t - tx) / tw, s = (tx - sg.mx) * ux + (ty - sg.my) * uy;
    if (s < lo) lo = s; if (s > hi) hi = s;
  }
  return sg.ridge.length ? hi - lo + 1 : 0;
}

/** D-B1-4: a face's streetlights stand on the kerb — the street tiles that border its front ring on its side of the
 *  watershed — one every FACE_LIGHT_STEP tiles along each segment (GAME-ASSUMPTION: 4), so the count per segment is
 *  its length over the step; 3 in 8 broken as on the lattice (§13). Corners: two segments never share a kerb tile. */
function faceLights(G: Ground, cg: CityGeom, seed: number, i: number): Streetlight[] {
  const cb = cg.blocks[i], tw = G.tw, th = G.th;
  const out: Streetlight[] = [];
  for (const j of cb.nb) {
    const sg = segBetween(cg, i, j);
    if (!sg) continue;
    const front = frontTiles(cg, i, j);
    const cand = new Map<number, number>();   // kerb street tile → the side it faces from the lot
    for (let k = 0; k < front.length; k++) {
      const t = front[k], tx = t % tw, ty = (t - tx) / tw;
      if (ty > 0 && cg.kind[t - tw] === STREET && cg.near[t - tw] === i) cand.set(t - tw, 0);
      if (tx < tw - 1 && cg.kind[t + 1] === STREET && cg.near[t + 1] === i) cand.set(t + 1, 1);
      if (ty < th - 1 && cg.kind[t + tw] === STREET && cg.near[t + tw] === i) cand.set(t + tw, 2);
      if (tx > 0 && cg.kind[t - 1] === STREET && cg.near[t - 1] === i) cand.set(t - 1, 3);
    }
    const [ux, uy] = segAxis(sg, tw);
    const along = [...cand.keys()].map(t => { const tx = t % tw, ty = (t - tx) / tw; return { t, tx, ty, s: (tx - sg.mx) * ux + (ty - sg.my) * uy }; }).sort((a, b) => a.s - b.s || a.t - b.t);
    let last = -Infinity;
    for (const c of along) {
      if (c.s < last + FACE_LIGHT_STEP) continue;
      let ok = true;
      for (const l of out) if (Math.max(Math.abs(l.tx - c.tx), Math.abs(l.ty - c.ty)) < 2) { ok = false; break; }   // corners: two segments share a kerb tile
      if (!ok) continue;
      last = c.s;
      out.push({ tx: c.tx, ty: c.ty, side: cand.get(c.t)!, broken: hash01(seed, 40, c.tx, c.ty) < STREETLIGHT_BROKEN });
    }
  }
  return out;
}

const PACK = 4194304;   // 2^22: room for a face's local tile index under the quantised score

/** Rubble on a face: the lattice recipe (tiles.ts `lotLayout`) over the face's own tiles. Three clusters (one on
 *  the HQ lot, below its clear rows), 250–350 tiles scaled by area over the lattice lot's 576, typed by district;
 *  outskirts faces carry a deposit (a quarter of them) or nothing. Reserved tiles never carry rubble. */
function faceLayout(G: Ground, st: SimState, i: number, b: Block, bg: BlockGround): void {
  const seed = st.seed, tw = G.tw, tiles = bg.tiles, n = tiles.length, scale = n / LATTICE_AREA;
  const rubble = rubbleOf(b.name);
  let deposit: DepositType | null = null, count = 0, centres = CLUSTERS;
  if (rubble && bg.hq) { count = HQ_RUBBLE_TILES; centres = 1; }
  else if (rubble) count = Math.round(scale * (RUBBLE_TILES_MIN + Math.floor(hash01(seed, 1, b.x, b.y) * (RUBBLE_TILES_MAX - RUBBLE_TILES_MIN + 1))));
  else if (hash01(seed, 2, b.x, b.y) < DEPOSIT_CELL_FRACTION) {
    deposit = hash01(seed, 3, b.x, b.y) < DEPOSIT_IRON_FRACTION ? 'iron' : 'coal';
    count = Math.round(scale * DEPOSIT_TILES); centres = 1;
  }
  bg.rubble = rubble; bg.deposit = deposit;
  let avail = 0;
  for (let k = 0; k < n; k++) if (G.rank[tiles[k]] !== RESERVED) avail++;
  count = Math.min(count, avail);
  if (count === 0) return;
  const cx: number[] = [], cy: number[] = [];
  for (let k = 0; k < centres; k++) {
    if (bg.hq) {
      cx.push(G.hqOrigin[0] + 3 + hash01(seed, 10 + k, b.x, b.y) * (LOT_TILES - 6));
      cy.push(G.hqOrigin[1] + HQ_CLEAR_ROWS + 1 + hash01(seed, 20 + k, b.x, b.y) * (LOT_TILES - HQ_CLEAR_ROWS - 2));
    } else {
      const t = tiles[Math.floor(hash01(seed, 10 + k, b.x, b.y) * n)], tx = t % tw;
      cx.push(tx + 0.5); cy.push((t - tx) / tw + 0.5);
    }
  }
  const keys = new Float64Array(n), inv = 1 / (2 * SIGMA * SIGMA);
  for (let k = 0; k < n; k++) {
    const t = tiles[k];
    if (G.rank[t] === RESERVED) { keys[k] = 2e6 * PACK + k; continue; }
    const tx = t % tw, ty = (t - tx) / tw;
    let s = 0;
    for (let c = 0; c < centres; c++) {
      const dx = tx + 0.5 - cx[c], dy = ty + 0.5 - cy[c];
      const e = Math.exp(-(dx * dx + dy * dy) * inv);
      if (e > s) s = e;
    }
    const score = 0.65 * s + 0.35 * hash01(seed, 100 + i, tx, ty);
    keys[k] = Math.round((1 - score) * 1e6) * PACK + k;   // ascending sort = densest first, ties by tile order
  }
  keys.sort();
  const depth = depthFrac(b), order = new Int32Array(count);
  for (let r = 0; r < count; r++) {
    const t = tiles[keys[r] % PACK];
    const rank = 1 - r / Math.max(1, count - 1);
    const v = 0.55 * rank + 0.45 * depth;
    G.variant[t] = 1 + Math.min(RUBBLE_VARIANTS - 1, Math.round(v * (RUBBLE_VARIANTS - 1)));
    order[count - 1 - r] = t;
  }
  for (let r = 0; r < count; r++) G.rank[order[r]] = r;
  bg.count = count; bg.order = order;
}

function markPatches(G: Ground, bg: BlockGround): void {
  const [ox, oy] = G.hqOrigin;
  for (const p of HQ_PATCHES) for (let ly = p.ly; ly < p.ly + p.h; ly++) for (let lx = p.lx; lx < p.lx + p.w; lx++) {
    const t = (oy + ly) * G.tw + ox + lx;
    if (G.owner[t] === bg.i) G.patch[t] = p.type;
  }
}

function buildChunks(G: Ground): void {
  const sets: Set<number>[] = [];
  for (let k = 0; k < G.cw * G.ch; k++) sets.push(new Set());
  for (const bg of G.blocks) {
    if (bg.tiles.length === 0) continue;
    for (let cy = bg.y0 >> 5; cy <= bg.y1 >> 5; cy++) for (let cx = bg.x0 >> 5; cx <= bg.x1 >> 5; cx++) sets[cy * G.cw + cx].add(bg.i);
  }
  // a block's bbox can overlap chunks it has no tile in; trim by the owner map so chunk keys stay small
  const seen = new Uint8Array(G.blocks.length);
  for (let k = 0; k < sets.length; k++) {
    const cx = k % G.cw, cy = (k - cx) / G.cw, x0 = cx * CHUNK, y0 = cy * CHUNK, x1 = Math.min(G.tw, x0 + CHUNK), y1 = Math.min(G.th, y0 + CHUNK);
    const present: number[] = [];
    for (let y = y0; y < y1; y++) for (let x = x0; x < x1; x++) {
      const o = G.owner[y * G.tw + x];
      if (o >= 0 && !seen[o]) { seen[o] = 1; present.push(o); }
    }
    for (const o of present) seen[o] = 0;
    // substations and lights sit on a block's own tiles or on its streets: include the street tiles' blocks too
    for (let y = y0; y < y1; y++) for (let x = x0; x < x1; x++) {
      const o = G.near[y * G.tw + x];
      if (o >= 0 && !seen[o]) { seen[o] = 1; present.push(o); }
    }
    for (const o of present) seen[o] = 0;
    present.sort((a, b) => a - b);
    G.chunks[k] = Int32Array.from(present);
  }
}

// ------------------------------------------------------------------ queries

export const inGround = (G: Ground, tx: number, ty: number) => tx >= 0 && ty >= 0 && tx < G.tw && ty < G.th;

/** D5: walkable is any tile that is not water — streets, lots, dark blocks, rot, plazas. Machines are the flow
 *  layer's business (walk.ts). */
export function walkable(G: Ground, tx: number, ty: number): boolean {
  return inGround(G, tx, ty) && G.base[ty * G.tw + tx] !== T_RIVER;
}

/** The block a tile belongs to for the flow layer (see `Ground.near`), -1 on water or off the map. */
export function blockOfTile(st: SimState, tx: number, ty: number): number {
  const G = ground(st);
  return inGround(G, tx, ty) ? G.near[ty * G.tw + tx] : -1;
}

/** HQ lot coordinates (the 24×24 frame) → a tile. */
export function hqLot(st: SimState, lx: number, ly: number): [number, number] {
  const [ox, oy] = ground(st).hqOrigin;
  return [ox + lx, oy + ly];
}

/** The block's pool at full: `poolMax` for the lattice lot, scaled by the face's area on a city (D6). */
export function poolCap(st: SimState, b: Block): number {
  const pm = poolMax(st, b.name);
  return st.lattice ? pm : pm * b.area / LATTICE_AREA;
}

/** Rubble tiles still standing by the pool: the block's pool fraction, rounded. Deposits and blocks with no pool
 *  keep every tile. */
export function standing(st: SimState, i: number): number {
  const bg = ground(st).blocks[i];
  if (!bg.rubble || bg.count === 0) return bg.count;
  const cap = poolCap(st, st.blocks[i]);
  if (cap <= 0) return bg.count;
  return Math.round(bg.count * Math.max(0, Math.min(1, st.blocks[i].pool / cap)));
}

/** Tiles of a block that machines or hands have dug out (global tile indices), from the flow layer. */
export function dugOf(st: SimState, i: number): readonly number[] {
  return st.flow?.dug[i] ?? NONE;
}

/** How many of a block's rubble tiles are gone: the explicitly dug ones plus, thinnest heap edge first, as many
 *  more as the pool says (the block-level yield keeps drawing the pool down; the dug tiles count against it). */
export function goneOf(st: SimState, i: number): number {
  const bg = ground(st).blocks[i];
  const left = bg.rubble ? standing(st, i) : bg.count;
  return Math.max(0, bg.count - left - dugOf(st, i).length);
}

/** A cheap fingerprint of everything that changes a block's tiles. */
export function blockKey(st: SimState, i: number): string {
  return `${st.blocks[i].state}:${standing(st, i)}:${dugOf(st, i).length}`;
}

export interface TileView { kind: number; variant: number; patch: number; block: number }

/** What stands on a tile now. */
export function tileAt(st: SimState, tx: number, ty: number): TileView {
  const G = ground(st);
  if (!inGround(G, tx, ty)) return { kind: T_STREET, variant: 0, patch: 0, block: -1 };
  const t = ty * G.tw + tx, o = G.owner[t];
  if (o === -2) return { kind: T_RIVER, variant: 0, patch: 0, block: -1 };
  if (o === -1) return { kind: T_STREET, variant: 0, patch: 0, block: G.near[t] };
  const b = st.blocks[o];
  if (b.state === INERT || b.state === VOID) return { kind: T_INERT, variant: 0, patch: 0, block: o };
  const dug = dugOf(st, o);
  const isDug = dug.length > 0 && dug.includes(t);
  if (G.patch[t] && !isDug) return { kind: T_PATCH, variant: 0, patch: G.patch[t], block: o };
  const r = G.rank[t];
  if (r >= 0 && !isDug && r >= goneOf(st, o)) return { kind: G.blocks[o].deposit ? T_DEPOSIT : T_RUBBLE, variant: G.variant[t], patch: 0, block: o };
  return { kind: T_GROUND, variant: 0, patch: 0, block: o };
}

export interface ChunkTiles { cx: number; cy: number; kind: Uint8Array; variant: Uint8Array; patch: Uint8Array }

/** The renderer's cache key for a 32×32 chunk: the keys of every block with tiles in it. */
export function chunkKey(st: SimState, cx: number, cy: number): string {
  const G = ground(st), list = G.chunks[cy * G.cw + cx];
  if (!list || list.length === 0) return '';
  let s = '';
  for (let k = 0; k < list.length; k++) s += (k ? '|' : '') + blockKey(st, list[k]);
  return s;
}

function paint(st: SimState, G: Ground, x0: number, y0: number, x1: number, y1: number, kind: Uint8Array, variant: Uint8Array, patch: Uint8Array, stride: number): void {
  const gone = new Map<number, number>(), dugs = new Map<number, Set<number> | null>();
  for (let y = y0; y < y1; y++) for (let x = x0; x < x1; x++) {
    const t = y * G.tw + x, o = G.owner[t], j = (y - y0) * stride + (x - x0);
    if (o === -2) { kind[j] = T_RIVER; continue; }
    if (o === -1) { kind[j] = T_STREET; continue; }
    const b = st.blocks[o];
    if (b.state === INERT || b.state === VOID) { kind[j] = T_INERT; continue; }
    let d = dugs.get(o);
    if (d === undefined) { const arr = dugOf(st, o); d = arr.length ? new Set(arr) : null; dugs.set(o, d); }
    const isDug = d !== null && d.has(t);
    if (G.patch[t] && !isDug) { kind[j] = T_PATCH; patch[j] = G.patch[t]; continue; }
    const r = G.rank[t];
    if (r >= 0 && !isDug) {
      let g = gone.get(o);
      if (g === undefined) { g = goneOf(st, o); gone.set(o, g); }
      if (r >= g) { kind[j] = G.blocks[o].deposit ? T_DEPOSIT : T_RUBBLE; variant[j] = G.variant[t]; continue; }
    }
    kind[j] = T_GROUND;
  }
}

/** One 32×32 chunk of tiles, row-major; tiles off the map read as street. */
export function chunkTiles(st: SimState, cx: number, cy: number): ChunkTiles {
  const G = ground(st), n = CHUNK * CHUNK;
  const kind = new Uint8Array(n), variant = new Uint8Array(n), patch = new Uint8Array(n);
  const x0 = cx * CHUNK, y0 = cy * CHUNK;
  paint(st, G, x0, y0, Math.min(G.tw, x0 + CHUNK), Math.min(G.th, y0 + CHUNK), kind, variant, patch, CHUNK);
  return { cx, cy, kind, variant, patch };
}

export interface GroundTiles { w: number; h: number; kind: Uint8Array; variant: Uint8Array; patch: Uint8Array }

/** Every tile of the city, row-major (the full-city derivation the tests time). */
export function groundTiles(st: SimState): GroundTiles {
  const G = ground(st), n = G.tw * G.th;
  const kind = new Uint8Array(n), variant = new Uint8Array(n), patch = new Uint8Array(n);
  paint(st, G, 0, 0, G.tw, G.th, kind, variant, patch, G.tw);
  return { w: G.tw, h: G.th, kind, variant, patch };
}

/** The block whose substation covers a tile, or -1. */
export function substationOwner(st: SimState, tx: number, ty: number): number {
  const G = ground(st);
  if (!inGround(G, tx, ty)) return -1;
  const list = G.chunks[(ty >> 5) * G.cw + (tx >> 5)];
  for (let k = 0; k < list.length; k++) {
    const s = G.blocks[list[k]].sub;
    if (s && tx >= s.x && tx < s.x + s.size && ty >= s.y && ty < s.y + s.size) return list[k];
  }
  return -1;
}

/** Blocks with tiles (or streets) in the chunk containing a tile and the eight chunks around it. */
export function blocksNear(st: SimState, tx: number, ty: number): number[] {
  const G = ground(st), out = new Set<number>(), cx = tx >> 5, cy = ty >> 5;
  for (let y = Math.max(0, cy - 1); y <= Math.min(G.ch - 1, cy + 1); y++) for (let x = Math.max(0, cx - 1); x <= Math.min(G.cw - 1, cx + 1); x++) {
    for (const i of G.chunks[y * G.cw + x]) out.add(i);
  }
  return [...out];
}

/** Distance from a point to an axis-aligned tile rectangle (0 inside). */
export function distToRect(px: number, py: number, x: number, y: number, w: number, h: number): number {
  const dx = Math.max(x - px, 0, px - (x + w)), dy = Math.max(y - py, 0, py - (y + h));
  return Math.hypot(dx, dy);
}

/** D5: the engineer's reach, 8 tiles from where they stand to the nearest edge of the target. */
export function inReach(st: SimState, tx: number, ty: number, size = 1): boolean {
  const e = st.engineer;
  return distToRect(e.x, e.y, tx, ty, size, size) <= REACH;
}

/** One line for a tooltip. */
export function describeGround(st: SimState, tx: number, ty: number): string {
  const G = ground(st);
  if (!inGround(G, tx, ty)) return '';
  const v = tileAt(st, tx, ty), bg = v.block >= 0 ? G.blocks[v.block] : null;
  const what = v.kind === T_RUBBLE ? `${bg?.rubble} rubble, density ${v.variant}/${RUBBLE_VARIANTS}` : v.kind === T_DEPOSIT ? `${bg?.deposit} deposit`
    : v.kind === T_PATCH ? `HQ ${PATCH_NAMES[v.patch]} patch` : TILE_NAMES[v.kind];
  return `tile (${tx},${ty}) · ${what}`;
}

// The block-level engineer (engineer.ts) asks where a tile is when it moves in a straight line on a city.
blockAtHook.current = (st, x, y) => {
  const G = ground(st), tx = Math.floor(x), ty = Math.floor(y);
  if (!inGround(G, tx, ty)) return -1;
  const o = G.owner[ty * G.tw + tx];
  return o >= 0 ? o : -1;
};
