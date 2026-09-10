/** Prompt B M5 Light (run name B-M5-light): the light map as data. §4: one texel per tile, multiplied over the world
 *  layer by the game; lit tiles are full colour, unlit ones darkened. A tile is lit when a lit light covers it —
 *  the same rule the shade test uses (`litAt`, `lightCovers`), so the picture and the threat never disagree.
 *  Streets are lit by their kerb streetlights (one every 4 tiles, radius 7 to the street midline — D-B5-4 — so a
 *  powered face lights its half of the shared street end to end) and lots only within a Lamp's or Floodlight's
 *  reach: "streets and lamp radii lit, lots unlit". The engineer
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
  // Home has built-in area lighting across its entire starting lot, including the base footprint.
  if(st.campaign&&!st.city?.mapId)for(const tile of G.blocks[st.campaign.homeBlock].tiles)mask[tile]=1;
  return mask;
}

export interface LitCount { street: number; lot: number; streetOf: number; lotOf: number;
  /** The kerb row: street tiles 4-adjacent to the lot (the strip §13's streetlights stand on and light). */
  kerb: number; kerbOf: number }
/** How much of a block's street and lot the mask lights: its lot tiles, the street tiles nearest it (the half-street
 *  to the midline, 6–7 tiles deep on the river city — a radius-7 kerb light reaches it, D-B5-4), and the kerb row. */
export function litCount(st: SimState, bi: number, mask = lightMask(st)): LitCount {
  const G = ground(st), bg = G.blocks[bi], out: LitCount = { street: 0, lot: 0, streetOf: 0, lotOf: bg.tiles.length, kerb: 0, kerbOf: 0 };
  for (let k = 0; k < bg.tiles.length; k++) if (mask[bg.tiles[k]]) out.lot++;
  const tw = G.tw, kerb = new Set<number>();
  for (let k = 0; k < bg.tiles.length; k++) {
    const t = bg.tiles[k], x = t % tw;
    for (const u of [x > 0 ? t - 1 : -1, x < tw - 1 ? t + 1 : -1, t - tw, t + tw]) if (u >= 0 && u < G.owner.length && G.owner[u] === -1) kerb.add(u);
  }
  for (const t of kerb) { out.kerbOf++; if (mask[t]) out.kerb++; }
  for (let t = 0; t < G.near.length; t++) if (G.near[t] === bi && G.owner[t] === -1) { out.streetOf++; if (mask[t]) out.street++; }
  return out;
}
