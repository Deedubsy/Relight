/** §18's three worked examples as rendered map-view images (rework Step 5): the compact bot on the street-first
 *  city, seed 3, the §18 cadence, at 10 minutes, 5 hours and 25 hours. Same config as the harness's E8 (DEFAULT_CONFIG,
 *  no walking, no building), so the captions' 52/27/38 at 5 h and 292/33/270 at 25 h are what these pictures show for seed 3 (E8's three-seed mean is 52/27/39 and 292/23/276).
 *
 *    npm run section18            → docs/section18-10min.png, -5h.png, -25h.png (+ the numbers on stdout)
 *
 *  Colours follow the game's city map (packages/game/src/cityMapScene.ts): Dark by rot tier, Held amber, interior
 *  brighter amber with a white rim, plazas grey, streets near-black, water slate, front edges red along the street. */
import { writeFileSync } from 'node:fs';
import { join, resolve } from 'node:path';
import {
  SimConfig, createState, step, createBot, botCommands, Command, citySpec, generateCity, CityGeom,
  DARK, CONTESTED, HELD, INERT, idxOf, rotOf, rotTier, isInterior, heldCount, frontage, interior, frontList, segKey, STREET, WATER,
  SimState, ROUNDS_PER_MAG,
} from '@relight/sim';
import { png } from './seeds';
import { configOf, stamp } from '@relight/harness/src/provenance';

const ROOT = resolve(process.env.INIT_CWD ?? process.cwd());
const OUT = join(ROOT, 'docs');
const SEED = 3, PRESET = 'river' as const, SCALE = 1;   // one pixel per tile: 800 × 800
const STOPS: [string, number][] = [['10min', 600], ['5h', 5 * 3600], ['25h', 25 * 3600]];

type RGB = [number, number, number];
const hex = (c: number): RGB => [(c >> 16) & 255, (c >> 8) & 255, c & 255];
const DARK_TIER = [0x16204a, 0x1c2a5e, 0x243774, 0x30478f].map(hex);
const DARK_WELL = [0x161a48, 0x1c2260, 0x262d78, 0x333b94].map(hex);
const COL = { street: hex(0x0e1326), water: hex(0x2f3a4f), held: hex(0xe8a93a), interior: hex(0xf5c24f), white: hex(0xffffff),
              contested: hex(0xd99a2b), inert: hex(0x3f4553), edge: hex(0xe0442a), hq: hex(0xffffff), wellCell: hex(0x1e1250) };

function render(st: SimState, g: CityGeom): Uint8Array {
  const w = Math.ceil(g.tw / SCALE), h = Math.ceil(g.th / SCALE);
  const rgb = new Uint8Array(w * h * 3);
  const n = st.blocks.length;
  const col: RGB[] = new Array(n), inner = new Uint8Array(n);
  const wellSet = new Set(st.wells.map(([x, y]) => idxOf(st, x, y)));
  for (let i = 0; i < n; i++) {
    const b = st.blocks[i];
    switch (b.state) {
      case DARK: col[i] = wellSet.has(i) ? COL.wellCell : (b.well ? DARK_WELL : DARK_TIER)[rotTier(rotOf(st, i))]; break;
      case CONTESTED: col[i] = COL.contested; break;
      case HELD: { const inn = isInterior(st, i); inner[i] = inn ? 1 : 0; col[i] = inn ? COL.interior : COL.held; break; }
      case INERT: col[i] = COL.inert; break;
      default: col[i] = hex(0x1a1e2a);
    }
  }
  const own = g.owner, kind = g.kind, tw = g.tw;
  const put = (p: number, c: RGB) => { rgb[p * 3] = c[0]; rgb[p * 3 + 1] = c[1]; rgb[p * 3 + 2] = c[2]; };
  for (let y = 0; y < h; y++) for (let x = 0; x < w; x++) {
    const tx = x * SCALE, ty = y * SCALE, t = ty * tw + tx, k = kind[t], o = own[t];
    let c: RGB = k === STREET ? COL.street : k === WATER ? COL.water : o >= 0 ? col[o] : COL.street;
    if (o >= 0 && inner[o] && ((tx > 0 && own[t - 1] !== o) || (ty > 0 && own[t - tw] !== o) || (tx < tw - 1 && own[t + 1] !== o) || (ty < g.th - 1 && own[t + tw] !== o))) c = COL.white;
    put(y * w + x, c);
  }
  for (const e of frontList(st)) {
    const a = idxOf(st, e.from.x, e.from.y), b = idxOf(st, e.to.x, e.to.y);
    const k = g.segAt.get(segKey(n, a, b)); if (k === undefined) continue;
    for (const t of g.segs[k].ridge) { const x = Math.floor((t % tw) / SCALE), y = Math.floor(Math.floor(t / tw) / SCALE); put(y * w + x, COL.edge); }
  }
  // HQ: a small white square at the start block's pole
  const hq = idxOf(st, st.start[0], st.start[1]), bb = g.blocks[hq];
  for (let dy = -3; dy <= 3; dy++) for (let dx = -3; dx <= 3; dx++) { const x = Math.floor(bb.cx / SCALE) + dx, y = Math.floor(bb.cy / SCALE) + dy; if (x >= 0 && y >= 0 && x < w && y < h) put(y * w + x, COL.hq); }
  return rgb;
}

const cfg: SimConfig = configOf({ kind: 'section18' });   // DEFAULT_CONFIG with production off (provenance.ts); E8's config: production off, so the caption is the front's cost, not the line's
const st = createState(citySpec(SEED, PRESET, cfg), cfg, SEED);
const g = generateCity(SEED, PRESET);
const bot = createBot('compact', null, false);
const cmds: Command[] = [];
const PROV = stamp({ kind: 'section18' });
for (const [name, until] of STOPS) {
  while (st.t < until) { cmds.length = 0; botCommands(st, bot, cmds); step(st, cmds); st.events.length = 0; }
  const rgb = render(st, g);
  const file = join(OUT, `section18-${name}.png`);
  writeFileSync(file, png(Math.ceil(g.tw / SCALE), Math.ceil(g.th / SCALE), rgb));
  writeFileSync(file.replace(/\.png$/, '.json'), JSON.stringify(PROV, null, 1) + '\n');   // rule 11 sidecar: commit + config hash
  console.log(`${name.padEnd(6)} held ${heldCount(st)}  front ${frontage(st)}  interior ${interior(st)}  lost ${st.stats.lost}  mags ${Math.round(st.totalRounds / ROUNDS_PER_MAG)}  → ${file}`);
}
