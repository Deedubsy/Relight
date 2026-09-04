/** D6 — the street-first city's tile geometry. A city is a tile canvas where streets are painted first and the
 *  blocks are whatever land is left between them (the faces of the street graph). Everything the block sim needs
 *  (neighbours, segment lengths, buildable areas) and everything the tile layer needs (each lot's buildable tiles,
 *  each street segment's street-facing tiles, the largest square a facility can sit on) is derived here once per
 *  seed and cached. Tile index is `y * tw + x`. */

export const LAND = 0, STREET = 1, WATER = 2;

export interface CityBlock {
  id: number;
  tiles: Int32Array;      // buildable tiles of the lot (the polygon, rasterised)
  area: number;           // = tiles.length
  cx: number; cy: number; // pole of inaccessibility: the lot tile farthest from any street (the block's (x, y) in the spec)
  x0: number; y0: number; x1: number; y1: number;   // bounding box, inclusive
  sq: number; sqx: number; sqy: number;             // largest inscribed square (side, top-left)
  nb: number[];           // neighbours across a street segment, ascending
  river: boolean;         // touches the river embankment
  edge: boolean;          // touches the canvas boundary street
  inert: boolean;         // plaza / park: never a lot
}

export interface CitySeg {
  a: number; b: number;   // a < b
  len: number;            // ridge tiles: the shared street's length
  ridge: Int32Array;      // street tiles on the watershed between a and b
  frontA: Int32Array;     // lot tiles of a that face this street (a's turret ring for this edge)
  frontB: Int32Array;
  mx: number; my: number; // midpoint of the ridge
}

export interface CityGeom {
  seed: number; preset: string; attempt: number;
  tw: number; th: number;
  kind: Uint8Array;       // LAND / STREET / WATER per tile
  owner: Int32Array;      // block id per land tile, -1 street, -2 water
  near: Int32Array;       // per street tile: the nearest block (watershed label); -1 off-street
  blocks: CityBlock[];
  segs: CitySeg[];
  segAt: Map<number, number>;   // key a * n + b (a < b) → seg index
  hops: Int32Array;       // graph hops from the HQ
  hq: number; foundry: number;
  wells: number[];
  facilities: { name: string; block: number }[];
  survivors: { name: string; tag: string; block: number }[];
  district: Uint8Array;   // 0 civ, 1 res, 2 ind, 3 out per block
  valid: boolean; reasons: string[];
}

export function segKey(n: number, a: number, b: number): number { return a < b ? a * n + b : b * n + a; }

export function segBetween(g: CityGeom, a: number, b: number): CitySeg | undefined {
  const k = g.segAt.get(segKey(g.blocks.length, a, b));
  return k === undefined ? undefined : g.segs[k];
}

/** The lot tiles of block `a` that face the street it shares with `b`: the turret ring for edge a→b. */
export function frontTiles(g: CityGeom, a: number, b: number): Int32Array {
  const s = segBetween(g, a, b);
  if (!s) return new Int32Array(0);
  return s.a === a ? s.frontA : s.frontB;
}
