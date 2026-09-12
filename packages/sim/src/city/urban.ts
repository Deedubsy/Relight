/** RI-02A: deterministic urban infill over the existing ownership graph.
 * Dimensions are tiles. These are reversible geometry defaults, authorised by the city brief;
 * no district/resource/economy or ownership rule is redefined. Missing profile means legacy.
 */
import type { Ground } from '../ground';
import type { SimState } from '../types';
import type { CityGeom } from './geom';
import { hash01 } from '../prng';

export const CITY_PROFILE = 'riverside-v1' as const;
export const URBAN = {
  pavement: 2, access: 2, factorySquare: 16, hqSquare: 24,
  residential: [6, 4], industrial: [12, 6], civic: [10, 6], rail: [14, 5],
  maxSolidFraction: 0.12,
} as const;
export type UrbanRole = 'hq' | 'residential' | 'industrial' | 'civic' | 'rail' | 'outskirts';
export interface CityStructure { block: number; role: UrbanRole; x: number; y: number; w: number; h: number; door: [number, number] }
export interface CityPlace { block: number; role: UrbanRole; name: string; pad: { x: number; y: number; w: number; h: number } }
export interface UrbanCity {
  profile: typeof CITY_PROFILE;
  solid: Uint8Array;
  /** 0 asphalt/lot, 1 pavement, 2 quay. All remain traversable. */
  surface: Uint8Array;
  structures: CityStructure[];
  places: CityPlace[];
  /** Geometric reservation, not an encounter or a transport unlock. */
  railReserve: { installation: [number, number]; feeders: [number, number][]; loading: { x: number; y: number; w: number; h: number } };
}

export function buildUrban(st: SimState, G: Ground, cg: CityGeom): UrbanCity {
  const n = G.tw * G.th, solid = new Uint8Array(n), surface = new Uint8Array(n);
  const structures: CityStructure[] = [], places: CityPlace[] = [];
  // Pavements occupy existing street tiles: widths and graph intersections stay identical.
  for (let y = 0; y < G.th; y++) for (let x = 0; x < G.tw; x++) {
    const t = y * G.tw + x;
    if (G.owner[t] !== -1) continue;
    for (let d = 1; d <= URBAN.pavement; d++) {
      for (const [xx, yy] of [[x - d, y], [x + d, y], [x, y - d], [x, y + d]]) {
        if (xx < 0 || yy < 0 || xx >= G.tw || yy >= G.th) continue;
        const o = G.owner[yy * G.tw + xx];
        if (o >= 0) surface[t] = Math.max(surface[t], 1);
        if (o === -2) surface[t] = 2;
      }
    }
  }
  const hq = cg.hq, local = new Set(cg.blocks[hq].nb);
  const residential = cg.blocks.filter(b => local.has(b.id) && b.id !== G.railYard && !b.inert)
    .sort((a, b) => b.cx - a.cx)[0]?.id;
  const industrial = cg.blocks.filter(b => !b.inert && b.id !== residential && b.id !== G.railYard && b.id !== hq && cg.hops[b.id] <= 2)
    .sort((a, b) => b.sq - a.sq || b.area - a.area)[0]?.id;
  const civic = cg.blocks.filter(b => !b.inert && b.id !== residential && b.id !== industrial && b.id !== G.railYard && b.id !== hq && cg.hops[b.id] <= 2 && !local.has(b.id))
    .sort((a, b) => b.sq - a.sq || b.area - a.area)[0]?.id;
  const title = { hq: 'Riverside HQ', rail: 'Switchyard', residential: 'Founders Court', industrial: 'Ironworks Yard', civic: 'Exchange Square', outskirts: 'Boundary Works' };
  for (const b of cg.blocks) {
    if (b.inert) continue;
    const bg = G.blocks[b.id];
    // Architecture is separate from district type: no resource identities are changed.
    const role: UrbanRole = b.id === hq ? 'hq' : b.id === G.railYard ? 'rail' : b.id === residential ? 'residential'
      : b.id === civic ? 'civic' : b.id === industrial ? 'industrial' : (['civic', 'residential', 'industrial', 'outskirts'] as const)[cg.district[b.id]];
    const size = Math.min(b.sq, role === 'hq' ? URBAN.hqSquare : role === 'residential' ? 12 : URBAN.factorySquare);
    const pad = { x: b.sqx, y: b.sqy, w: size, h: size };
    places.push({ block: b.id, role, name: [hq, G.railYard, residential, civic, industrial].includes(b.id) ? title[role]
      : `${['Ash', 'Mercer', 'Canal', 'Brass', 'Slate', 'North'][b.id % 6]} ${role === 'residential' ? 'Court' : role === 'industrial' ? 'Works' : role === 'civic' ? 'Exchange' : 'Reach'} ${b.id + 1}`, pad });
    if (role === 'hq') continue; // HQ uses the real Depot.
    const [w, h] = URBAN[role === 'outskirts' ? 'industrial' : role];
    const limit = Math.min(role === 'residential' ? 3 : 2, Math.floor(b.area * URBAN.maxSolidFraction / (w * h)));
    // Pick peripheral rectangles, leaving the inscribed factory square, resources, fixtures,
    // two-tile perimeter circulation and two-tile entrances untouched. Separate hash stream.
    const candidates: { x: number; y: number; w: number; h: number; score: number }[] = [];
    for (const [cw, ch] of [[w, h], [h, w]]) {
    for (let y = b.y0 + URBAN.access; y <= b.y1 + 1 - ch - URBAN.access; y++) for (let x = b.x0 + URBAN.access; x <= b.x1 + 1 - cw - URBAN.access; x++) {
      if (x < pad.x + pad.w && x + cw > pad.x && y < pad.y + pad.h && y + ch > pad.y) continue;
      let ok = true;
      for (let yy = y - 2; yy < y + ch + 2 && ok; yy++) for (let xx = x - 2; xx < x + cw + 2; xx++) {
        const t = yy * G.tw + xx;
        if (G.owner[t] !== b.id || G.patch[t] || (role === 'rail' && G.rank[t] >= 0) || (bg.sub && xx >= bg.sub.x - 3 && xx < bg.sub.x + bg.sub.size + 3 && yy >= bg.sub.y - 3 && yy < bg.sub.y + bg.sub.size + 3)) { ok = false; break; }
      }
      if (ok) candidates.push({ x, y, w: cw, h: ch, score: hash01(st.seed, 92021, x, y) });
    }
    }
    candidates.sort((a, b) => a.score - b.score);
    let placed = 0;
    for (const c of candidates) {
      if (placed >= limit) break;
      const { w, h } = c;
      let overlaps = false;
      for (let y = c.y - 2; y < c.y + h + 2; y++) for (let x = c.x - 2; x < c.x + w + 2; x++) if (solid[y * G.tw + x]) overlaps = true;
      if (overlaps) continue;
      structures.push({ block: b.id, role, x: c.x, y: c.y, w, h, door: [c.x + Math.floor(w / 2), c.y + h] });
      for (let y = c.y; y < c.y + h; y++) for (let x = c.x; x < c.x + w; x++) solid[y * G.tw + x] = 1;
      placed++;
    }
  }
  // Name a built landmark, not an empty reservation when an irregular lot cannot
  // accommodate its silhouette. Outskirts sheds share the industrial footprint.
  for (const role of ['industrial', 'civic'] as const) {
    const anchor = places.find(p => p.name === title[role]);
    if (anchor && structures.some(s => s.block === anchor.block)) continue;
    const replacement = places.filter(p => (p.role === role || (role === 'industrial'
      && (p.role === 'outskirts' || (p.role === 'civic' && p.name !== title.civic))))
      && structures.some(s => s.block === p.block))
      .sort((a, b) => cg.hops[a.block] - cg.hops[b.block] || cg.blocks[b.block].area - cg.blocks[a.block].area)[0];
    if (!replacement) continue;
    if (anchor) anchor.name = `Vacant ${role === 'industrial' ? 'Yard' : 'Square'} ${anchor.block + 1}`;
    replacement.name = title[role]; replacement.role = role;
    for (const s of structures) if (s.block === replacement.block) s.role = role;
  }
  const ry = G.blocks[G.railYard], rb = cg.blocks[G.railYard], sub = ry.sub!;
  const free = Array.from(ry.tiles).filter(t => !solid[t] && G.rank[t] === -1 && !(t % G.tw >= sub.x - 2 && t % G.tw < sub.x + sub.size + 2 && Math.floor(t / G.tw) >= sub.y - 2 && Math.floor(t / G.tw) < sub.y + sub.size + 2));
  const feeders = [free[0], free[free.length - 1]].filter(t => t !== undefined).map(t => [t % G.tw, Math.floor(t / G.tw)] as [number, number]);
  return { profile: CITY_PROFILE, solid, surface, structures, places,
    railReserve: { installation: [sub.x, sub.y], feeders, loading: { x: rb.sqx, y: rb.sqy, w: Math.min(rb.sq, 16), h: Math.min(rb.sq, 16) } } };
}
