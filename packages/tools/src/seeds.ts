/**
 * seeds — the D6 seed browser. Renders street-first cities (packages/sim/src/city) to PNG and prints each one's
 * summary and validator verdict, so a human can look at a seed before it goes in a fixture or a doc.
 *
 *   npm run seeds                                  seeds 3, 4, 5 × every preset → docs/seeds/<preset>-<seed>.png
 *   npm run seeds -- --seeds 7,8 --presets river   a subset
 *   npm run seeds -- --scale 1 --out /tmp/x        one pixel per tile (default 2 tiles per pixel), another folder
 *   npm run seeds -- --labels                      also write <name>.txt with the block ids of HQ, facilities, wells
 *
 * Colours: water blue, street grey, plaza/park pale green, civic gold, residential green, industrial rust, outskirts
 * olive; HQ white square, Foundry/Arsenal/Turbine hall/Refinery/Power station/Tram depot magenta squares, wells black
 * rings. No text is drawn: the console lists which square is which by block id and lot centre.
 */
import { mkdirSync, writeFileSync } from 'node:fs';
import { join, resolve } from 'node:path';
import { deflateSync } from 'node:zlib';
import { generateCity, CITY_PRESETS, CityPreset, CityGeom, LAND, STREET, WATER, DISTRICT_NAMES } from '@relight/sim';

const argv = process.argv.slice(2);
const flag = (k: string, d: string): string => { const i = argv.indexOf(k); return i >= 0 && i + 1 < argv.length ? argv[i + 1] : d; };
const SEEDS = flag('--seeds', '3,4,5').split(',').map(Number);
const PRESETS = flag('--presets', CITY_PRESETS.join(',')).split(',') as CityPreset[];
const SCALE = Math.max(1, Number(flag('--scale', '2')));
const OUT = resolve(process.env.INIT_CWD ?? process.cwd(), flag('--out', 'docs/seeds'));
const LABELS = argv.includes('--labels');

// --- a minimal PNG writer (RGB, 8-bit, filter 0) -------------------------------------------------------------
const CRC = new Uint32Array(256).map((_, i) => { let c = i; for (let k = 0; k < 8; k++) c = c & 1 ? 0xedb88320 ^ (c >>> 1) : c >>> 1; return c >>> 0; });
function crc32(buf: Uint8Array): number { let c = 0xffffffff; for (const b of buf) c = CRC[(c ^ b) & 0xff] ^ (c >>> 8); return (c ^ 0xffffffff) >>> 0; }
function chunk(type: string, data: Uint8Array): Uint8Array {
  const out = new Uint8Array(12 + data.length), dv = new DataView(out.buffer);
  dv.setUint32(0, data.length); out.set([...type].map(ch => ch.charCodeAt(0)), 4); out.set(data, 8);
  dv.setUint32(8 + data.length, crc32(out.subarray(4, 8 + data.length)));
  return out;
}
export function png(w: number, h: number, rgb: Uint8Array): Uint8Array {
  const raw = new Uint8Array((w * 3 + 1) * h);
  for (let y = 0; y < h; y++) { raw[y * (w * 3 + 1)] = 0; raw.set(rgb.subarray(y * w * 3, (y + 1) * w * 3), y * (w * 3 + 1) + 1); }
  const ihdr = new Uint8Array(13), dv = new DataView(ihdr.buffer);
  dv.setUint32(0, w); dv.setUint32(4, h); ihdr[8] = 8; ihdr[9] = 2; ihdr[10] = 0; ihdr[11] = 0; ihdr[12] = 0;
  const parts = [new Uint8Array([137, 80, 78, 71, 13, 10, 26, 10]), chunk('IHDR', ihdr), chunk('IDAT', new Uint8Array(deflateSync(raw))), chunk('IEND', new Uint8Array(0))];
  const out = new Uint8Array(parts.reduce((a, p) => a + p.length, 0));
  let o = 0; for (const p of parts) { out.set(p, o); o += p.length; }
  return out;
}

// --- rendering ----------------------------------------------------------------------------------------------
type RGB = [number, number, number];
export const PALETTE = {
  water: [70, 110, 170] as RGB, street: [150, 150, 150] as RGB, inert: [190, 215, 170] as RGB,
  district: [[214, 178, 90], [120, 170, 110], [170, 110, 80], [140, 140, 90]] as RGB[],   // civ, res, ind, out
  hq: [255, 255, 255] as RGB, facility: [220, 60, 200] as RGB, well: [20, 20, 20] as RGB,
};

/** The city as an RGB buffer, `scale` tiles per pixel (the majority kind of the pixel's tiles wins, streets first). */
export function renderCity(g: CityGeom, scale = 2): { w: number; h: number; rgb: Uint8Array } {
  const w = Math.ceil(g.tw / scale), h = Math.ceil(g.th / scale);
  const rgb = new Uint8Array(w * h * 3);
  const put = (px: number, py: number, c: RGB) => { if (px < 0 || py < 0 || px >= w || py >= h) return; const o = (py * w + px) * 3; rgb[o] = c[0]; rgb[o + 1] = c[1]; rgb[o + 2] = c[2]; };
  for (let py = 0; py < h; py++) for (let px = 0; px < w; px++) {
    let street = 0, water = 0, land = 0, owner = -1;
    for (let dy = 0; dy < scale; dy++) for (let dx = 0; dx < scale; dx++) {
      const x = px * scale + dx, y = py * scale + dy; if (x >= g.tw || y >= g.th) continue;
      const k = g.kind[y * g.tw + x];
      if (k === STREET) street++; else if (k === WATER) water++; else if (k === LAND) { land++; owner = g.owner[y * g.tw + x]; }
    }
    let c: RGB;
    if (water > land && water >= street) c = PALETTE.water;
    else if (street >= land) c = PALETTE.street;
    else if (owner < 0) c = PALETTE.street;
    else c = g.blocks[owner].inert ? PALETTE.inert : PALETTE.district[g.district[owner]];
    put(px, py, c);
  }
  const square = (bx: number, by: number, side: number, c: RGB) => {
    const px = Math.round(bx / scale), py = Math.round(by / scale), r = Math.max(1, Math.round(side / scale / 2));
    for (let dy = -r; dy <= r; dy++) for (let dx = -r; dx <= r; dx++) put(px + dx, py + dy, c);
  };
  const ring = (bx: number, by: number, rad: number, c: RGB) => {
    const px = Math.round(bx / scale), py = Math.round(by / scale), r = Math.max(2, Math.round(rad / scale));
    for (let a = 0; a < 64; a++) put(Math.round(px + r * Math.cos(a / 64 * Math.PI * 2)), Math.round(py + r * Math.sin(a / 64 * Math.PI * 2)), c);
  };
  for (const f of g.facilities) { const b = g.blocks[f.block]; square(b.cx, b.cy, 8, PALETTE.facility); }
  { const b = g.blocks[g.hq]; square(b.cx, b.cy, 10, PALETTE.hq); }
  for (const wi of g.wells) { const b = g.blocks[wi]; ring(b.cx, b.cy, 7, PALETTE.well); }
  return { w, h, rgb };
}

export function summarise(g: CityGeom): string {
  const live = g.blocks.filter(b => !b.inert);
  const deg: Record<number, number> = {}; for (const b of live) deg[b.nb.length] = (deg[b.nb.length] ?? 0) + 1;
  const dist: Record<string, number> = {}; for (const b of live) dist[DISTRICT_NAMES[g.district[b.id]]] = (dist[DISTRICT_NAMES[g.district[b.id]]] ?? 0) + 1;
  const areas = live.map(b => b.area).sort((a, b) => a - b);
  const q = (p: number) => areas[Math.min(areas.length - 1, Math.floor(p * areas.length))];
  let maxHops = 0; for (let i = 0; i < g.blocks.length; i++) if (g.hops[i] > maxHops) maxHops = g.hops[i];
  const lines = [
    `${g.preset} seed ${g.seed}: ${g.valid ? 'VALID' : 'INVALID'} (attempt ${g.attempt})${g.reasons.length ? ' — ' + g.reasons.join('; ') : ''}`,
    `  blocks ${g.blocks.length} (${g.blocks.length - live.length} inert), segments ${g.segs.length}, max hops ${maxHops}`,
    `  degree ${Object.entries(deg).map(([k, v]) => `${k}:${v}`).join(' ')}; area p10/50/90 ${q(0.1)}/${q(0.5)}/${q(0.9)} tiles`,
    `  districts ${Object.entries(dist).map(([k, v]) => `${k} ${v}`).join(', ')}`,
    `  HQ block ${g.hq} at (${g.blocks[g.hq].cx},${g.blocks[g.hq].cy}); ` +
      g.facilities.map(f => `${f.name} #${f.block} hops ${g.hops[f.block]} at (${g.blocks[f.block].cx},${g.blocks[f.block].cy})`).join('; '),
    `  wells ${g.wells.map(i => `#${i} hops ${g.hops[i]}`).join(', ')}`,
  ];
  return lines.join('\n');
}

const isMain = process.argv[1] && /seeds\.(ts|js)$/.test(process.argv[1]);
if (isMain) {
  mkdirSync(OUT, { recursive: true });
  for (const preset of PRESETS) for (const seed of SEEDS) {
    const t0 = Date.now();
    const g = generateCity(seed, preset);
    const { w, h, rgb } = renderCity(g, SCALE);
    const name = `${preset}-${seed}`;
    writeFileSync(join(OUT, `${name}.png`), png(w, h, rgb));
    const text = summarise(g);
    if (LABELS) writeFileSync(join(OUT, `${name}.txt`), text + '\n');
    console.log(`${text}\n  → ${join(OUT, `${name}.png`)} (${w}×${h}, ${Date.now() - t0} ms)`);
  }
}
