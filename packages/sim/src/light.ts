/** Prompt B M5 Light (run name B-M5-light): the light map as data. §4: one texel per tile, multiplied over the world
 *  layer by the game; lit tiles are full colour, unlit ones darkened. A tile is lit when a lit light covers it —
 *  the same rule the shade test uses (`litAt`, `lightCovers`), so the picture and the threat never disagree.
 *  Streets are lit by their kerb streetlights (one every 4 tiles, radius 4, so a powered face's street is lit end to
 *  end) and lots only within a Lamp's or Floodlight's reach: "streets and lamp radii lit, lots unlit". The engineer
 *  carries no light (D-B5-1; the game can preview a 2-tile hand lamp, which is drawing only). */
import { SimState } from './types';
import { ground } from './ground';
import { blockLights, lightCovers, Light } from './flow';

/** Stamp one lit light into a tw × th mask. */
export function stampLight(mask: Uint8Array, tw: number, th: number, l: Light): void {
  const x0 = Math.max(0, Math.floor(l.tx - l.r)), x1 = Math.min(tw - 1, Math.ceil(l.tx + l.r));
  const y0 = Math.max(0, Math.floor(l.ty - l.r)), y1 = Math.min(th - 1, Math.ceil(l.ty + l.r));
  for (let ty = y0; ty <= y1; ty++) for (let tx = x0; tx <= x1; tx++) if (lightCovers(l, tx, ty)) mask[ty * tw + tx] = 1;
}

/** The city's lit tiles now: 1 lit, 0 unlit, one byte per tile (ty * tw + tx). Pass `into` to reuse a buffer. */
export function lightMask(st: SimState, into?: Uint8Array): Uint8Array {
  const G = ground(st), n = G.tw * G.th;
  const mask = into && into.length === n ? into : new Uint8Array(n);
  mask.fill(0);
  for (let bi = 0; bi < st.blocks.length; bi++) for (const l of blockLights(st, bi)) if (l.lit) stampLight(mask, G.tw, G.th, l);
  return mask;
}

export interface LitCount { street: number; lot: number; streetOf: number; lotOf: number }
/** How much of a block's street kerb and lot the mask lights: its lot tiles, and the street tiles nearest it. */
export function litCount(st: SimState, bi: number, mask = lightMask(st)): LitCount {
  const G = ground(st), bg = G.blocks[bi], out: LitCount = { street: 0, lot: 0, streetOf: 0, lotOf: bg.tiles.length };
  for (let k = 0; k < bg.tiles.length; k++) if (mask[bg.tiles[k]]) out.lot++;
  for (let t = 0; t < G.near.length; t++) if (G.near[t] === bi && G.owner[t] === -1) { out.streetOf++; if (mask[t]) out.street++; }
  return out;
}
