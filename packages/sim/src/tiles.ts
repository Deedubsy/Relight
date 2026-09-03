/** §4 ground at tile level, derived from block state (constitution Phase 4 M1).
 *
 *  Nothing here is stored in SimState. The map view's block (`st.blocks[]`) is authoritative: a cell's tiles are a
 *  pure function of the seed, the cell's district and depth, its block state and how much of its rubble pool is
 *  left. The world view draws what these functions return; a tile never changes a block.
 *
 *  Geometry (§4): a block cell is 32×32 tiles, a 24×24 buildable lot with a 4-tile street margin on every side, so
 *  two adjacent cells share an 8-tile street. The 24×24-cell city is 768×768 tiles. The river row (the bottom row
 *  of cells) is inert tiles with a 4-tile embankment street along its top. Scattered inert cells are inert lots
 *  ringed by street. */
import { SimState, Block, District, INERT, VOID } from './types';
import { hash01 } from './prng';
import { districtOf } from './districts';
import { poolMax } from './queries';

export const TILE_PX = 32;          // §4: 32 px tiles
export const CELL_TILES = 32;       // §4: one block cell is 32×32 tiles
export const LOT_TILES = 24;        // §4: the buildable lot
export const MARGIN_TILES = 4;      // §4: street margin on every side of the lot
export const STREET_TILES = 8;      // §4: two margins make the shared street

export const T_STREET = 0, T_GROUND = 1, T_RUBBLE = 2, T_INERT = 3, T_RIVER = 4, T_DEPOSIT = 5;
export type TileKind = 0 | 1 | 2 | 3 | 4 | 5;
export const TILE_NAMES: readonly string[] = ['street', 'ground', 'rubble', 'inert', 'river', 'deposit'];
export type RubbleType = 'stone' | 'copper' | 'steel';
export type DepositType = 'iron' | 'coal';

/** §12: a block holds 250–350 rubble tiles. GAME-ASSUMPTION: the count is uniform in that range per cell and does
 *  not vary with district or depth; only the density variant does. */
export const RUBBLE_TILES_MIN = 250, RUBBLE_TILES_MAX = 350;
/** §4: five density variants per rubble tileset. */
export const RUBBLE_VARIANTS = 5;
/** GAME-ASSUMPTION: outskirt lots carry no rubble (§7 "none (deposits)"); one outskirt cell in four holds a deposit
 *  patch of DEPOSIT_TILES tiles, iron three times in five and coal otherwise (§12 names iron mines and coal seams;
 *  the oil field is late game and not placed). Deposits are not consumed in M1: the block sim has no outskirt pool. */
export const DEPOSIT_CELL_FRACTION = 0.25, DEPOSIT_TILES = 160, DEPOSIT_IRON_FRACTION = 0.6;
/** GAME-ASSUMPTION: rubble sits in clusters (three blob centres per lot, σ = 6 tiles) so a lot reads as heaps with
 *  clear ground between them, as in the §18 sketch, rather than as a uniform speckle. */
const CLUSTERS = 3, SIGMA = 6;

export function rubbleOf(name: District): RubbleType | null {
  return name === 'civ' ? 'stone' : name === 'res' ? 'copper' : name === 'ind' ? 'steel' : null;
}

/** A lot's fixed layout: where its rubble (or deposit) tiles are, the order they are dug in, and each tile's
 *  density variant. Depends only on the seed and the cell, never on time. */
export interface LotLayout {
  rubble: RubbleType | null;
  deposit: DepositType | null;
  /** Number of rubble (or deposit) tiles laid down. */
  tiles: number;
  /** Lot tile indices (ly * LOT_TILES + lx) in digging order: the first entries go to plain ground first. */
  order: Uint16Array;
  /** Per lot tile, density variant 1..RUBBLE_VARIANTS; 0 where there is no rubble. */
  variant: Uint8Array;
}

const layoutCache = new Map<string, LotLayout>();

/** GAME-ASSUMPTION: the density gradient. A tile's variant blends its rank inside its heap (core tiles densest)
 *  with the cell's depth in the §7 rot gradient (`dmax` over the district base, 0 at the start, 1 at +50 %), so
 *  deeper blocks and heap cores show the heavy tilesets and the edges of a near-HQ lot show the light ones. */
export function depthFrac(b: Pick<Block, 'name' | 'dmax'>): number {
  const base = districtOf(b.name).base;
  return Math.max(0, Math.min(1, (b.dmax / base - 1) / 0.5));
}

export function lotLayout(seed: number, b: Pick<Block, 'x' | 'y' | 'name' | 'dmax'>): LotLayout {
  const key = `${seed}:${b.x}:${b.y}`;
  const hit = layoutCache.get(key);
  if (hit) return hit;
  if (layoutCache.size > 8192) layoutCache.clear();
  const N = LOT_TILES * LOT_TILES;
  const variant = new Uint8Array(N);
  const rubble = rubbleOf(b.name);
  let deposit: DepositType | null = null;
  let count = 0, centres = CLUSTERS;
  if (rubble) count = RUBBLE_TILES_MIN + Math.floor(hash01(seed, 1, b.x, b.y) * (RUBBLE_TILES_MAX - RUBBLE_TILES_MIN + 1));
  else if (hash01(seed, 2, b.x, b.y) < DEPOSIT_CELL_FRACTION) {
    deposit = hash01(seed, 3, b.x, b.y) < DEPOSIT_IRON_FRACTION ? 'iron' : 'coal';
    count = DEPOSIT_TILES; centres = 1;
  }
  if (count === 0) {
    const lay = { rubble, deposit, tiles: 0, order: new Uint16Array(0), variant };
    layoutCache.set(key, lay);
    return lay;
  }
  const cx: number[] = [], cy: number[] = [];
  for (let k = 0; k < centres; k++) {
    cx.push(3 + hash01(seed, 10 + k, b.x, b.y) * (LOT_TILES - 6));
    cy.push(3 + hash01(seed, 20 + k, b.x, b.y) * (LOT_TILES - 6));
  }
  const score = new Float64Array(N);
  const idx: number[] = new Array(N);
  for (let ly = 0; ly < LOT_TILES; ly++) for (let lx = 0; lx < LOT_TILES; lx++) {
    let s = 0;
    for (let k = 0; k < centres; k++) {
      const dx = lx + 0.5 - cx[k], dy = ly + 0.5 - cy[k];
      s = Math.max(s, Math.exp(-(dx * dx + dy * dy) / (2 * SIGMA * SIGMA)));
    }
    const i = ly * LOT_TILES + lx;
    score[i] = 0.65 * s + 0.35 * hash01(seed, 100 + b.x * 64 + b.y, lx, ly);
    idx[i] = i;
  }
  idx.sort((a, c) => score[c] - score[a] || a - c);   // densest first
  const chosen = idx.slice(0, count);
  const depth = depthFrac(b);
  for (let r = 0; r < count; r++) {
    const rank = 1 - r / Math.max(1, count - 1);      // 1 at the heap core, 0 at the thinnest chosen tile
    const v = 0.55 * rank + 0.45 * depth;
    variant[chosen[r]] = 1 + Math.min(RUBBLE_VARIANTS - 1, Math.round(v * (RUBBLE_VARIANTS - 1)));
  }
  // GAME-ASSUMPTION: M1's digging order is thinnest heap edge first; M2's excavators replace it with their 5×5 footprints.
  const order = new Uint16Array(count);
  for (let r = 0; r < count; r++) order[r] = chosen[count - 1 - r];
  const lay = { rubble, deposit, tiles: count, order, variant };
  layoutCache.set(key, lay);
  return lay;
}

/** How many of a lot's rubble tiles still stand: the block's pool fraction, rounded. A district with no pool
 *  (outskirts, or the economy off) keeps every tile. */
export function rubbleLeft(st: SimState, b: Block): number {
  const lay = lotLayout(st.seed, b);
  if (!lay.rubble) return lay.tiles;
  const pm = poolMax(st, b.name);
  if (pm <= 0) return lay.tiles;
  return Math.round(lay.tiles * Math.max(0, Math.min(1, b.pool / pm)));
}

export interface CellTiles {
  x: number; y: number;
  /** CELL_TILES × CELL_TILES, row-major (ty * CELL_TILES + tx): a T_* kind per tile. */
  kind: Uint8Array;
  /** Density variant 1..RUBBLE_VARIANTS on rubble and deposit tiles, 0 elsewhere. */
  variant: Uint8Array;
  rubble: RubbleType | null;
  deposit: DepositType | null;
  /** Tiles laid down and tiles still standing. */
  rubbleTiles: number;
  rubbleLeft: number;
}

/** A cheap fingerprint of everything that changes a cell's tiles; the renderer repaints a cell when it changes. */
export function cellKey(st: SimState, x: number, y: number): string {
  const b = st.blocks[x * st.h + y];
  return `${b.state}:${rubbleLeft(st, b)}`;
}

export function cellTiles(st: SimState, x: number, y: number): CellTiles {
  const b = st.blocks[x * st.h + y];
  const n = CELL_TILES * CELL_TILES;
  const kind = new Uint8Array(n), variant = new Uint8Array(n);
  const lay = lotLayout(st.seed, b);
  const river = y === st.h - 1;
  const inert = !river && (b.state === INERT || b.state === VOID);
  let left = 0;
  if (river) {
    for (let ty = 0; ty < CELL_TILES; ty++) for (let tx = 0; tx < CELL_TILES; tx++) kind[ty * CELL_TILES + tx] = ty < MARGIN_TILES ? T_STREET : T_RIVER;
    return { x, y, kind, variant, rubble: null, deposit: null, rubbleTiles: 0, rubbleLeft: 0 };
  }
  // margins are street; the lot is ground, inert, or ground with rubble / a deposit on it
  for (let ty = 0; ty < CELL_TILES; ty++) for (let tx = 0; tx < CELL_TILES; tx++) {
    const onLot = tx >= MARGIN_TILES && tx < MARGIN_TILES + LOT_TILES && ty >= MARGIN_TILES && ty < MARGIN_TILES + LOT_TILES;
    kind[ty * CELL_TILES + tx] = !onLot ? T_STREET : inert ? T_INERT : T_GROUND;
  }
  if (!inert && lay.tiles > 0) {
    left = rubbleLeft(st, b);
    const k = lay.rubble ? T_RUBBLE : T_DEPOSIT;
    for (let r = lay.tiles - left; r < lay.tiles; r++) {
      const li = lay.order[r], lx = li % LOT_TILES, ly = (li - lx) / LOT_TILES;
      const ti = (ly + MARGIN_TILES) * CELL_TILES + lx + MARGIN_TILES;
      kind[ti] = k; variant[ti] = lay.variant[li];
    }
  }
  return { x, y, kind, variant, rubble: inert ? null : lay.rubble, deposit: inert ? null : lay.deposit, rubbleTiles: inert ? 0 : lay.tiles, rubbleLeft: left };
}

export interface CityTiles { w: number; h: number; kind: Uint8Array; variant: Uint8Array }

/** Every tile in the city (w·32 × h·32), row-major. ~600k tiles; a few tens of milliseconds. */
export function cityTiles(st: SimState): CityTiles {
  const w = st.w * CELL_TILES, h = st.h * CELL_TILES;
  const kind = new Uint8Array(w * h), variant = new Uint8Array(w * h);
  for (let x = 0; x < st.w; x++) for (let y = 0; y < st.h; y++) {
    const c = cellTiles(st, x, y);
    for (let ty = 0; ty < CELL_TILES; ty++) {
      const row = (y * CELL_TILES + ty) * w + x * CELL_TILES, src = ty * CELL_TILES;
      kind.set(c.kind.subarray(src, src + CELL_TILES), row);
      variant.set(c.variant.subarray(src, src + CELL_TILES), row);
    }
  }
  return { w, h, kind, variant };
}

/** Which cell a city tile belongs to and where in that cell it sits. */
export function tileCell(tx: number, ty: number): { x: number; y: number; lx: number; ly: number } {
  const x = Math.floor(tx / CELL_TILES), y = Math.floor(ty / CELL_TILES);
  return { x, y, lx: tx - x * CELL_TILES, ly: ty - y * CELL_TILES };
}

/** True for the 4-tile margin of a cell: the street side of the lot line. */
export function isMargin(lx: number, ly: number): boolean {
  return lx < MARGIN_TILES || ly < MARGIN_TILES || lx >= MARGIN_TILES + LOT_TILES || ly >= MARGIN_TILES + LOT_TILES;
}

/** One line for a tooltip. */
export function describeTile(st: SimState, tx: number, ty: number): string {
  const { x, y, lx, ly } = tileCell(tx, ty);
  if (x < 0 || y < 0 || x >= st.w || y >= st.h) return '';
  const c = cellTiles(st, x, y);
  const k = c.kind[ly * CELL_TILES + lx], v = c.variant[ly * CELL_TILES + lx];
  const what = k === T_RUBBLE ? `${c.rubble} rubble, density ${v}/${RUBBLE_VARIANTS}` : k === T_DEPOSIT ? `${c.deposit} deposit` : TILE_NAMES[k];
  return `tile (${tx},${ty}) · ${what}`;
}
