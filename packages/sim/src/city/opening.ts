/** First campaign home: the existing 24-tile starter factory frame inside a court
 * with one four-tile entrance. These are provisional geometry defaults, not combat balance. */
import type { Ground } from '../ground';
import { T_RIVER } from '../tiles';
export const OPENING_LAYOUT = { frame: 24, gateWidth: 4 } as const;
export interface OpeningGeometry { walls: number[]; gate: number[]; bounds: { x: number; y: number; size: number }; direction: number }
export function buildOpening(G: Ground): OpeningGeometry {
  const [ox, oy] = G.hqOrigin, x = ox - 1, y = oy - 1, size = OPENING_LAYOUT.frame + 2;
  const inset = (OPENING_LAYOUT.frame - OPENING_LAYOUT.gateWidth) / 2;
  const wall = new Set<number>();
  for (let k = 0; k < size; k++) { wall.add(y * G.tw + x + k); wall.add((y + size - 1) * G.tw + x + k); wall.add((y + k) * G.tw + x); wall.add((y + k) * G.tw + x + size - 1); }
  const gates = [
    [0, Array.from({ length: OPENING_LAYOUT.gateWidth }, (_, k) => y * G.tw + ox + inset + k)],
    [1, Array.from({ length: OPENING_LAYOUT.gateWidth }, (_, k) => (oy + inset + k) * G.tw + x + size - 1)],
    [2, Array.from({ length: OPENING_LAYOUT.gateWidth }, (_, k) => (y + size - 1) * G.tw + ox + inset + k)],
    [3, Array.from({ length: OPENING_LAYOUT.gateWidth }, (_, k) => (oy + inset + k) * G.tw + x)],
  ] as [number, number[]][];
  // Prefer east, then south, west, north; the entire mouth and exterior approach must be open.
  const selected = [1, 2, 3, 0].map(d => gates[d]).find(([d, tiles]) => tiles.every(t => {
    const offset = [-G.tw, 1, G.tw, -1][d];
    return [t, t + offset, t + offset * 2].every(q => q >= 0 && q < G.base.length && G.base[q] !== T_RIVER && !G.urban!.solid[q]);
  }));
  if (!selected) throw new Error('campaign home has no clear entrance');
  const [direction, gate] = selected;
  for (const t of gate) wall.delete(t);
  for (const t of wall) G.urban!.solid[t] = 1;
  const opening = { walls: [...wall], gate, bounds: { x, y, size }, direction };
  G.urban!.places.find(p => G.blocks[p.block].hq)!.name = 'Home Court';
  return opening;
}
