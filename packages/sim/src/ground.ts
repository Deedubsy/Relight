import {doorRect} from './city/parcelGeometry';
/** Prompt B M1 — the ground on faces (D6). One derivation per state, cached on `st.blocks`: every tile's base kind
 *  and owner, each block's buildable tiles, substation, streetlights and rubble layout in dig order (global tile
 *  indices), the HQ lot's origin, and the 32×32 chunk → block overlap lists the renderer keys its cache on. The
 *  lattice (Phase 4's 32×32 cells, kept until the Phase 5 gate deletes it) is wrapped into the same shape from
 *  tiles.ts so the flow layer and the world view have one code path. Dig state (rubble left, dug tiles) is read
 *  from the block sim and the flow layer at query time; the derivation itself depends only on the seed and the
 *  geometry, never on time. */
import { SimState, Block, INERT, VOID } from './types';
import {RIVERFRONT_ID,RIVERFRONT,RIVERFRONT_BUILDINGS,RIVERFRONT_PROPS,riverfrontGeom} from './city/riverfront';
import { buildUrban, UrbanCity } from './city/urban';
import { isCampaign } from './rules';
import { buildOpening, type OpeningGeometry } from './city/opening';
import { LAMP_STEP_TILES } from './constants';
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
  depthFrac, lotLayout, isMargin, TILE_NAMES, PATCH_NAMES, RAIL_YARD_HEAP, RAIL_YARD_COAL, RUBBLE_UNITS_PER_TILE,
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
  urban?: UrbanCity;
  opening?: OpeningGeometry;
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
  /** D-P4-12: the rail yard — the block whose rubble is coal (`railYardOf`); -1 on the lattice and when the HQ has
   *  no claimable neighbour. */
  railYard: number;
}

const NONE: readonly number[] = [];
const cache = new WeakMap<Block[], Ground>();

/** The ground for a state (derived once per state; ~50 ms for an 800×800 city, see tiles.test.ts). */
/** The city geometry a city state was generated from (cached by generateCity). */
export function cityGeomOf(st: SimState): CityGeom {
  const c = st.city!;
  if(c.mapId===RIVERFRONT_ID)return riverfrontGeom();
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
    cw: Math.ceil(tw / CHUNK), ch: Math.ceil(th / CHUNK), chunks: [], railYard: -1,
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
  if(st.city?.mapId===RIVERFRONT_ID)return riverfrontGround(st);
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
  G.railYard = railYardOf(cg, st);
  for (let i = 0; i < cg.blocks.length; i++) {
    const cb = cg.blocks[i], b = st.blocks[i], hq = i === cg.hq;
    const inert = cb.inert || b.state === INERT || b.state === VOID;
    const bg: BlockGround = {
      i, tiles: cb.tiles, x0: cb.x0, y0: cb.y0, x1: cb.x1, y1: cb.y1, sub: null, lights: [], rubble: null, deposit: null,
      count: 0, order: new Int32Array(0), hq, pole: [cb.cx, cb.cy],
    };
    // §7 (prompt B M3, run name B-M3-unlocks): an outskirts face has no substation and no streetlights; the
    // Electricians' craftable Substation (flow.ts `faceSub`) is how one gets a substation. RI-03 (D-CU-3 (b)): the
    // Substation stands on the unheld outskirts lot first, as field kit next to Held ground, and is the installation
    // the claim is delivered to and activated at — the block sim's abstract supply no longer holds an outskirts block
    // without one (the map claim that did is the labelled legacy path). GAME-ASSUMPTION (still): a Held outskirts
    // block's kerb has no streetlights to light.
    const outskirts = cg.district[i] === 3;
    bg.sub = hq ? { x: hqb.sqx + HQ_SUBSTATION[0], y: hqb.sqy + HQ_SUBSTATION[1], size: SUBSTATION_TILES } : inert || outskirts ? null : faceSubstation(G, cg, i);
    if (!inert && !outskirts) bg.lights = faceLights(G, cg, st.seed, i);
    if (bg.sub) for (let dy = 0; dy < bg.sub.size; dy++) for (let dx = 0; dx < bg.sub.size; dx++) G.rank[(bg.sub.y + dy) * tw + bg.sub.x + dx] = RESERVED;
    if (hq) {
      for (let ly = 0; ly < LOT_TILES; ly++) for (let lx = 0; lx < LOT_TILES; lx++) if (hqReserved(lx, ly)) G.rank[(hqb.sqy + ly) * tw + hqb.sqx + lx] = RESERVED;
      markPatches(G, bg);
    }
    if (!inert) faceLayout(G, st, i, b, bg);
    G.blocks.push(bg);
  }
  for (let t = 0; t < tw * th; t++) if (G.rank[t] === RESERVED) G.rank[t] = -1;
  if (st.city?.profile === 'riverside-v1') {
    G.urban = buildUrban(st, G, cg);
    if (isCampaign(st)) G.opening = buildOpening(G);
    // Structures precede the final resource placement. Relocate only covered rubble tiles,
    // preserving count, depletion rank and type; HQ patches and rail coal are untouched.
    for (const bg of G.blocks) {
      if (bg.hq || bg.i === G.railYard) continue;
      const free = Array.from(bg.tiles).filter(t => !G.urban!.solid[t] && G.rank[t] === -1 &&
        !(bg.sub && t % tw >= bg.sub.x && t % tw < bg.sub.x + bg.sub.size && Math.floor(t / tw) >= bg.sub.y && Math.floor(t / tw) < bg.sub.y + bg.sub.size));
      let next = 0;
      for (let r = 0; r < bg.order.length; r++) {
        const t = bg.order[r];
        if (!G.urban.solid[t]) continue;
        const to = free[next++];
        if (to === undefined) throw new Error(`City ${st.seed}: no resource space on block ${bg.i}`);
        G.rank[to] = r; G.variant[to] = G.variant[t]; G.rank[t] = -1; G.variant[t] = 0; bg.order[r] = to;
      }
    }
  }
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
export const FACE_LIGHT_STEP = LAMP_STEP_TILES;

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
/** D-P4-12: the rail yard is the HQ's most westward claimable neighbour by bounding-box centre — the same scoring
 *  the hour bot's `neighbourToward('west')` uses, so it is the block constants.HOUR's "claim west (rail yard)" lands
 *  on (D-HOUR-1). State-free: inert and void blocks are never claimable; every other neighbour is Dark at the start.
 *  GAME-ASSUMPTION (RI-01): the city generator has no rail-yard district, so geometry designates it. */
function railYardOf(cg: CityGeom, st: SimState): number {
  const hq = cg.hq, a = cg.blocks[hq];
  const ax = (a.x0 + a.x1) / 2, ay = (a.y0 + a.y1) / 2;
  let best = -1, bs = -Infinity;
  for (const j of st.nb[hq] ?? []) {
    const cb = cg.blocks[j], b = st.blocks[j];
    if (cb.inert || b.state === INERT || b.state === VOID) continue;
    const dx = (cb.x0 + cb.x1) / 2 - ax, dy = (cb.y0 + cb.y1) / 2 - ay, L = Math.hypot(dx, dy) || 1;
    const s = -dx / L;
    if (s > bs) { bs = s; best = j; }
  }
  return best;
}

/** D-P4-12: the rail yard's rubble is one RAIL_YARD_HEAP-square coal heap, RAIL_YARD_COAL units in all, on the lot
 *  spot nearest the face's pole whose surroundings (the heap plus two clear tiles each way, for the Excavator that
 *  digs it and the belt it feeds) are all lot tiles and none reserved; failing that, the pole itself. */
function railYardLayout(G: Ground, i: number, bg: BlockGround): void {
  const tw = G.tw, h = RAIL_YARD_HEAP, half = (h - 1) / 2, ring = half + 2;
  const [px, py] = bg.pole;
  let cx = px, cy = py, bd = Infinity;
  for (let k = 0; k < bg.tiles.length; k++) {
    const t = bg.tiles[k], tx = t % tw, ty = (t - tx) / tw;
    const d = Math.hypot(tx - px, ty - py);
    if (d >= bd) continue;
    let ok = true;
    for (let y = ty - ring; y <= ty + ring && ok; y++) for (let x = tx - ring; x <= tx + ring && ok; x++) {
      if (!inGround(G, x, y)) { ok = false; break; }
      const u = y * tw + x;
      if (G.owner[u] !== i || G.rank[u] === RESERVED) ok = false;
    }
    if (ok) { bd = d; cx = tx; cy = ty; }
  }
  const order: number[] = [];
  for (let y = cy - half; y <= cy + half; y++) for (let x = cx - half; x <= cx + half; x++) {
    if (!inGround(G, x, y)) continue;
    const t = y * tw + x;
    if (G.owner[t] !== i || G.rank[t] === RESERVED) continue;
    order.push(t);
  }
  bg.rubble = 'coal'; bg.deposit = null; bg.count = order.length; bg.order = Int32Array.from(order);
  for (let r = 0; r < order.length; r++) { G.rank[order[r]] = r; G.variant[order[r]] = RUBBLE_VARIANTS; }
}

function faceLayout(G: Ground, st: SimState, i: number, b: Block, bg: BlockGround): void {
  if (i === G.railYard) { railYardLayout(G, i, bg); return; }
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
  return inGround(G, tx, ty) && G.base[ty * G.tw + tx] !== T_RIVER && !G.urban?.solid[ty * G.tw + tx];
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

/** Units a fresh district rubble tile holds (D-P4-2: RUBBLE_UNITS_PER_TILE; the rail yard's heap shares
 *  RAIL_YARD_COAL over its tiles, D-P4-12). The HQ patches carry their own sizes (HQ_PATCHES). */
export function tileUnits(G: Ground, bi: number): number {
  const bg = G.blocks[bi];
  return bi === G.railYard ? RAIL_YARD_COAL / Math.max(1, bg.count) : RUBBLE_UNITS_PER_TILE;
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
export function inReach(st: SimState, tx: number, ty: number, size = 1, height = size): boolean {
  const e = st.engineer;
  return distToRect(e.x, e.y, tx, ty, size, height) <= REACH && citySight(st,e.x,e.y,Math.max(tx-.01,Math.min(tx+size+.01,e.x)),Math.max(ty-.01,Math.min(ty+height+.01,e.y)));
}

/** One line for a tooltip. */
export function describeGround(st: SimState, tx: number, ty: number): string {
  const G = ground(st);
  if (!inGround(G, tx, ty)) return '';
  const shell = G.urban?.structures.find(s => tx >= s.x && tx < s.x + s.w && ty >= s.y && ty < s.y + s.h);
  if(st.city?.mapId&&shell){const b=RIVERFRONT_BUILDINGS.find(b=>b.id===(shell as {id?:string}).id);return b?`${b.name} · ${b.enterable?'open doorway / physical interior':'boarded building'}${b.note?' · notes inside':''}`:'City building';}
  if (shell) return `tile (${tx},${ty}) · sealed ${shell.role} shell; open door approach on its south side`;
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

/** Authored geography adapter; physical walls are separate from non-solid roofs. */
function riverfrontGround(st:SimState):Ground {
 const cg=riverfrontGeom(),G=alloc(cg.tw,cg.th,false);G.hqOrigin=[...RIVERFRONT.homeOrigin];G.railYard=1;
 for(let t=0;t<G.base.length;t++){G.base[t]=cg.kind[t]===2?T_RIVER:cg.kind[t]===1?T_STREET:T_GROUND;G.owner[t]=cg.owner[t];G.near[t]=cg.near[t];}
 const solid=new Uint8Array(G.base.length),surface=new Uint8Array(G.base.length);
 for(const b of cg.blocks){const sub={x:RIVERFRONT.substations[b.id][0],y:RIVERFRONT.substations[b.id][1],size:3};G.blocks.push({i:b.id,tiles:b.tiles,x0:b.x0,y0:b.y0,x1:b.x1,y1:b.y1,sub,lights:[],rubble:null,deposit:null,count:0,order:new Int32Array(),hq:b.id===0,pole:[b.cx,b.cy]});}
 for(const b of RIVERFRONT_BUILDINGS)for(let y=b.y;y<b.y+b.h;y++)for(let x=b.x;x<b.x+b.w;x++){const wall=x===b.x||y===b.y||x===b.x+b.w-1||y===b.y+b.h-1,d=doorRect(b),door=b.enterable&&b.door&&x>=d.x&&x<d.x+d.w&&y>=d.y&&y<d.y+d.h||b.enterable&&b.doors?.some(([dx,dy,w,h])=>x>=dx&&x<dx+w&&y>=dy&&y<dy+h);solid[y*G.tw+x]=!b.enterable||wall&&!door?1:0;surface[y*G.tw+x]=1;}
 for(const p of RIVERFRONT_PROPS)if(!st.campaign?.authored?.cleared.includes(p.id)&&!st.campaign?.authored?.opened.includes(p.id))for(let y=p.y;y<p.y+p.h;y++)for(let x=p.x;x<p.x+p.w;x++)solid[y*G.tw+x]=1;
 for(const [x,y] of RIVERFRONT.substations)for(let dy=0;dy<3;dy++)for(let dx=0;dx<3;dx++)solid[(y+dy)*G.tw+x+dx]=1;
 for(const p of RIVERFRONT.yards)for(let y=p.y;y<p.y+p.h;y++)for(let x=p.x;x<p.x+p.w;x++)surface[y*G.tw+x]=1;
 G.urban={profile:'riverside-v1',solid,surface,structures:RIVERFRONT_BUILDINGS.map(b=>({...b,block:G.near[b.y*G.tw+b.x],role:b.kind==='house'?'residential':b.kind==='civic'?'civic':'industrial',door:b.door??[b.x+2,b.y+b.h-1]})),places:RIVERFRONT.regions.map(([name,x,y],block)=>({block,name,role:block===0?'hq':block===3?'civic':block===4||block===5?'residential':'industrial',pad:RIVERFRONT.yards[block]??{x:x-5,y:y-5,w:10,h:10}})),railReserve:{installation:RIVERFRONT.projects.station,feeders:[],loading:{x:RIVERFRONT.stops[1].x,y:RIVERFRONT.stops[1].y-3,w:7,h:3}}};
 for(const [tx,ty]of RIVERFRONT.lights){const bi=G.near[ty*G.tw+tx];if(bi>=0)G.blocks[bi].lights.push({tx,ty,side:0,broken:false});}
 G.opening={walls:[],gate:[...Array(6)].map((_,i)=>(RIVERFRONT.court.y+17)*G.tw+RIVERFRONT.court.x-3+i),bounds:{x:RIVERFRONT.homeOrigin[0]-7,y:RIVERFRONT.homeOrigin[1]-1,size:40},direction:2};markPatches(G,G.blocks[0]);buildChunks(G);return G;
}
/** Dynamic authored obstacle changes invalidate cached ground and movement collision once. */
export function invalidateGround(st:SimState):void {cache.delete(st.blocks);if(st.flow)st.flow.rev++;}

const sightWalls=new WeakMap<SimState,{rev:number;tiles:Set<number>}>();
/** Authored wall sight: roofs never change this result. Endpoints may be equipment. */
export function citySight(st:SimState,ax:number,ay:number,bx:number,by:number):boolean {
 if(!st.city?.mapId)return true;const G=ground(st),n=Math.ceil(Math.hypot(bx-ax,by-ay)*4);
 let walls=sightWalls.get(st);if(!walls||walls.rev!==st.flow?.rev){const tiles=new Set<number>();for(const m of st.flow?.machines??[])if((m.kind==='wall'||m.kind==='barricade')&&m.hp!==0)for(let y=m.y;y<m.y+m.size;y++)for(let x=m.x;x<m.x+m.size;x++)tiles.add(y*G.tw+x);walls={rev:st.flow?.rev??0,tiles};sightWalls.set(st,walls);}const end=Math.floor(by)*G.tw+Math.floor(bx),start=Math.floor(ay)*G.tw+Math.floor(ax);
 for(let i=1;i<n;i++){const x=Math.floor(ax+(bx-ax)*i/n),y=Math.floor(ay+(by-ay)*i/n);if(!inGround(G,x,y)||G.urban?.solid[y*G.tw+x]||y*G.tw+x!==end&&y*G.tw+x!==start&&walls.tiles.has(y*G.tw+x))return false;}
 return true;
}
