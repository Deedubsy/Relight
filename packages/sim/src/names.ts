/** RI-02 (§11.2, plan line "stable named destinations and directions"): every block has a stable, state-free name
 *  the HUD, the tooltips and the goal line use instead of block coordinates. Geometry only — a name never changes
 *  while a block changes hands, so a toast about "east residential" still means the same lot an hour later.
 *
 *  Implementation default (RI-02, reversible; not a historical approval): the HQ is "HQ"; the rail yard (ground.ts
 *  `railYard`, D-P4-12) is "<bearing> rail yard"; every other block is its 8-point compass bearing from the HQ's lot
 *  centre plus its district word ("east residential", "north-west civic"), with the HQ hop count appended from two
 *  hops out ("north-east civic 2"). Blocks that would share a name get " A", " B", … in block-index order. Debug
 *  coordinates stay available behind `blockCoords` (the game shows them only with the debug toggle). */
import { SimState, District } from './types';
import { ground } from './ground';
import { hqIdx } from './engineer';
import { RAIL_YARD } from './districts';
import { edgeFrom, edgeTo } from './graph';

export type Bearing = 'north' | 'north-east' | 'east' | 'south-east' | 'south' | 'south-west' | 'west' | 'north-west';
const BEARINGS: readonly Bearing[] = ['east', 'south-east', 'south', 'south-west', 'west', 'north-west', 'north', 'north-east'];
export const DISTRICT_WORD: Record<District, string> = { civ: 'civic', res: 'residential', ind: 'industrial', out: 'outskirts' };

function centre(st: SimState, i: number): [number, number] {
  const g = ground(st).blocks[i];
  if (g.x1 >= g.x0 && g.y1 >= g.y0) return [(g.x0 + g.x1) / 2, (g.y0 + g.y1) / 2];
  const b = st.blocks[i];   // a block with no lot (the lattice's river row): its block coordinates
  return [b.x * st.tileScale, b.y * st.tileScale];
}

/** The 8-point compass bearing of block `to` from block `from` (lot centres; screen y grows southward). */
export function bearingOf(st: SimState, from: number, to: number): Bearing {
  const [ax, ay] = centre(st, from), [bx, by] = centre(st, to);
  const a = Math.atan2(by - ay, bx - ax);   // 0 = east, +π/2 = south
  const k = ((Math.round(a / (Math.PI / 4)) % 8) + 8) % 8;
  return BEARINGS[k];
}

const nameCache = new WeakMap<object, string[]>();
function names(st: SimState): string[] {
  const key = st.blocks;
  let out = nameCache.get(key);
  if (out) return out;
  const hq = hqIdx(st), ry = ground(st).railYard;
  const base = st.blocks.map((b, i) => {
    if (i === hq) return 'HQ';
    const bearing = bearingOf(st, hq, i);
    if (i === ry) return `${bearing} ${RAIL_YARD.label.toLowerCase()}`;
    const hops = st.hops[i];
    const far = hops < 0 ? ' (cut off)' : hops >= 2 ? ` ${hops}` : '';
    return `${bearing} ${DISTRICT_WORD[b.name]}${far}`;
  });
  const count = new Map<string, number>();
  for (const n of base) count.set(n, (count.get(n) ?? 0) + 1);
  const seen = new Map<string, number>();
  out = base.map(n => {
    if ((count.get(n) ?? 0) < 2) return n;
    const k = seen.get(n) ?? 0; seen.set(n, k + 1);
    return `${n} ${String.fromCharCode(65 + (k % 26))}${k >= 26 ? Math.floor(k / 26) : ''}`;
  });
  nameCache.set(key, out);
  return out;
}

/** The block's stable name (see the header). */
export function blockName(st: SimState, i: number): string {
  const place = ground(st).urban?.places.find(p => p.block === i);
  if (place) return place.name;
  return i >= 0 && i < st.blocks.length ? names(st)[i] : 'nowhere';
}
/** By block coordinates (the map's (x, y)). */
export function blockNameAt(st: SimState, x: number, y: number): string {
  const i = st.lattice ? (x >= 0 && x < st.w && y >= 0 && y < st.h ? x * st.h + y : -1) : st.blocks.findIndex(b => b.x === x && b.y === y);
  return blockName(st, i);
}
/** The debug form: block coordinates, shown only behind the game's debug toggle. */
export function blockCoords(st: SimState, i: number): string {
  const b = st.blocks[i];
  return b ? `(${b.x},${b.y})` : '(?)';
}
/** The name of a block, with its coordinates appended when `debug` is on. */
export function blockLabel(st: SimState, i: number, debug = false): string {
  return debug ? `${blockName(st, i)} ${blockCoords(st, i)}` : blockName(st, i);
}

/** The HQ's neighbour in a §11 direction by lot geometry over every neighbour that is not inert or void — the same
 *  cosine rule hour.ts `neighbourToward` applies to Dark candidates, but state-free so the goal line can name the
 *  block after it is claimed. West is the rail yard when there is one (ground.ts chose it by the same rule), and the
 *  three §11 directions are kept distinct: west is assigned first, then east, then north (then south). */
export function hqNeighbourToward(st: SimState, dir: 'east' | 'west' | 'north' | 'south'): number {
  const G = ground(st), hq = hqIdx(st);
  const [ax, ay] = centre(st, hq);
  const taken = new Set<number>();
  const pick = (d: 'east' | 'west' | 'north' | 'south'): number => {
    let best = -1, bs = -Infinity;
    for (const j of st.nb[hq] ?? []) {
      if (taken.has(j)) continue;
      const b = st.blocks[j];
      if (b.state === 3 || b.state === 4) continue;   // INERT, VOID
      const [bx, by] = centre(st, j), dx = bx - ax, dy = by - ay, L = Math.hypot(dx, dy) || 1;
      const s = d === 'east' ? dx / L : d === 'west' ? -dx / L : d === 'north' ? -dy / L : dy / L;
      if (s > bs) { bs = s; best = j; }
    }
    return best;
  };
  const west = G.railYard >= 0 ? G.railYard : pick('west');
  if (west >= 0) taken.add(west);
  if (dir === 'west') return west;
  const east = pick('east');
  if (east >= 0) taken.add(east);
  if (dir === 'east') return east;
  const north = pick('north');
  if (north >= 0) taken.add(north);
  if (dir === 'north') return north;
  return pick('south');
}

/** A street segment between two neighbouring blocks, named from `a`'s side: "HQ's east street",
 *  "east residential's north street". */
export function streetName(st: SimState, a: number, b: number): string {
  return `${blockName(st, a)}'s ${bearingOf(st, a, b)} street`;
}
/** A frontage edge by its graph id (graph.ts). */
export function edgeName(st: SimState, id: number): string {
  return id < 0 ? 'no street' : streetName(st, edgeFrom(st, id), edgeTo(st, id));
}
