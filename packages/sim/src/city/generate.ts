/** D6 — the street-first city generator. Streets are painted onto a tile canvas in the order a city grows: the
 *  boundary, the river with its embankment, curved arterials, one diagonal avenue, then a recursive subdivision
 *  of every face that is still too big, with jittered cuts and dead-ends. What is left is the blocks: irregular
 *  polygons, rasterised, 3–7 neighbours, ~20–50 tiles across. Adjacency is a watershed over the street tiles, so a
 *  "shared street" is a ridge of street tiles between two lots and its length is the ridge's tile count.
 *  Then the graph is decorated (districts by band from the HQ, wells, facilities, survivors) and validated (§17);
 *  a failing seed rerolls up to CITY_ATTEMPTS times with a different jitter stream. Everything is seeded. */
import { RngHolder, seedRng, rngNext, rngInt, rngUniform } from '../prng';
import { bfsHops, bfsHopsMulti } from '../graph';
import { SURVIVOR_BANDS } from '../map';
import { WELL_RANGE } from '../districts';
import { CityBlock, CityGeom, CitySeg, LAND, STREET, WATER, segKey } from './geom';

export type CityPreset = 'river' | 'ring' | 'industrial' | 'noout' | 'canals';
export const CITY_PRESETS: readonly CityPreset[] = ['river', 'ring', 'industrial', 'noout', 'canals'];
export const PRESET_LABELS: Record<CityPreset, string> = {
  river: 'River city', ring: 'Ring road', industrial: 'One giant industrial district', noout: 'No outskirts', canals: 'Canals',
};

/** GAME-ASSUMPTION: canvas and street widths. 800×800 tiles is a city of ~400 blocks, the lattice's 552 cells minus
 *  the streets' share; §4 wants 6–12-tile streets, so boundary 8, arterials 10–12, avenue 10, secondary 6–8, and a
 *  30-tile river with a 10-tile embankment. Question for the human: is 800 tiles (25 minutes' walk edge to edge at
 *  6 tiles/s) the right city, or should the canvas follow the block count the slice wants? */
export const CITY_TW = 800, CITY_TH = 800;
export const BOUNDARY_W = 8, ARTERIAL_W = [10, 12] as const, AVENUE_W = 10, SECONDARY_W = [6, 8] as const;
export const RIVER_W = 30, EMBANKMENT_W = 10, CANAL_W = 12, CANAL_BANK_W = 6;
/** GAME-ASSUMPTION: subdivision stops when a face fits in 50×50 (§17's "20–50 tiles across"); cuts land at 35–65 % of
 *  the longer side with up to ±10° of slope, and one cut in five is a dead-end reaching 55–75 % of the way across. */
export const FACE_MAX = 50, FACE_MAX_AREA = 2400, CUT_LO = 0.35, CUT_HI = 0.65, DEAD_END_P = 0.2;
export const FACE_MIN_AREA = 200, FACE_MIN_ACROSS = 12;
/** GAME-ASSUMPTION: 8–10 % of faces are plazas and parks (inert). */
export const INERT_LO = 0.08, INERT_HI = 0.10;
/** GAME-ASSUMPTION: the HQ lot needs a 24-tile square (the lattice lot the HQ layout was drawn on); a facility 16. */
export const HQ_SQ = 24, FAC_SQ = 16;
/** GAME-ASSUMPTION: the rejection-sampling budget. E-variance measured the validator rejecting 26 % of jitter
 *  streams (mean attempt index 0.35 over 10,000 seeds), so a budget of 8 leaves 0.26^8 ≈ 1 seed in 48,000
 *  with no valid city — and the 10,000-seed run found one (seed 3767, "no well site: west riverside", which
 *  validates at attempt index 8). 16 puts that at 1 in 2.3 billion and costs nothing for the 99.998 % of
 *  seeds that validate inside 8, because the loop breaks at the first valid attempt (D-R4). */
export const CITY_ATTEMPTS = 16;
/** GAME-ASSUMPTION: the Tram depot (where the truck is found, D5) is 3–5 hops out: the compact bot holds ~28 blocks by hour 3 (radius ~3), so the truck is an hour-3 to hour-5 find; §8's 5–10 blocks would put it past hour 5. */
export const TRAM_HOPS: [number, number] = [3, 4];
export const OUTSKIRT_DEPTH = 2;
export const DISTRICT_NAMES = ['civ', 'res', 'ind', 'out'] as const;

export interface CityOpts { tw?: number; th?: number; attempts?: number }

/* ------------------------------------------------------------------ canvas ------------------------------------- */

function catmull(pts: [number, number][], step = 0.5): number[] {
  const out: number[] = [];
  const P = [pts[0], ...pts, pts[pts.length - 1]];
  for (let i = 1; i < P.length - 2; i++) {
    const [p0, p1, p2, p3] = [P[i - 1], P[i], P[i + 1], P[i + 2]];
    const segLen = Math.hypot(p2[0] - p1[0], p2[1] - p1[1]);
    const n = Math.max(2, Math.ceil(segLen / step));
    for (let k = 0; k < n; k++) {
      const t = k / n, t2 = t * t, t3 = t2 * t;
      const x = 0.5 * (2 * p1[0] + (-p0[0] + p2[0]) * t + (2 * p0[0] - 5 * p1[0] + 4 * p2[0] - p3[0]) * t2 + (-p0[0] + 3 * p1[0] - 3 * p2[0] + p3[0]) * t3);
      const y = 0.5 * (2 * p1[1] + (-p0[1] + p2[1]) * t + (2 * p0[1] - 5 * p1[1] + 4 * p2[1] - p3[1]) * t2 + (-p0[1] + 3 * p1[1] - 3 * p2[1] + p3[1]) * t3);
      out.push(x, y);
    }
  }
  out.push(pts[pts.length - 1][0], pts[pts.length - 1][1]);
  return out;
}

interface Canvas { tw: number; th: number; kind: Uint8Array }

function stamp(c: Canvas, xs: number[], halfW: number, value: number, only?: Int32Array, label = -1): void {
  const { tw, th, kind } = c;
  for (let i = 0; i < xs.length; i += 2) {
    const x0 = Math.max(0, Math.round(xs[i] - halfW)), x1 = Math.min(tw - 1, Math.round(xs[i] + halfW) - 1);
    const y0 = Math.max(0, Math.round(xs[i + 1] - halfW)), y1 = Math.min(th - 1, Math.round(xs[i + 1] + halfW) - 1);
    for (let y = y0; y <= y1; y++) for (let x = x0; x <= x1; x++) {
      const t = y * tw + x;
      if (only) { if (only[t] === label) kind[t] = value; } else kind[t] = value;
    }
  }
}

function line(x0: number, y0: number, x1: number, y1: number, step = 0.5): number[] {
  const n = Math.max(1, Math.ceil(Math.hypot(x1 - x0, y1 - y0) / step)), out: number[] = [];
  for (let k = 0; k <= n; k++) out.push(x0 + (x1 - x0) * k / n, y0 + (y1 - y0) * k / n);
  return out;
}

/* ------------------------------------------------------------------ faces -------------------------------------- */

interface Faces { labels: Int32Array; count: number; area: Int32Array; x0: Int32Array; y0: Int32Array; x1: Int32Array; y1: Int32Array }

function components(c: Canvas): Faces {
  const { tw, th, kind } = c, n = tw * th;
  const labels = new Int32Array(n).fill(-1);
  const stack = new Int32Array(n);
  const area: number[] = [], x0: number[] = [], y0: number[] = [], x1: number[] = [], y1: number[] = [];
  let count = 0;
  for (let s = 0; s < n; s++) {
    if (kind[s] !== LAND || labels[s] !== -1) continue;
    const id = count++;
    let sp = 0, a = 0, bx0 = tw, by0 = th, bx1 = -1, by1 = -1;
    stack[sp++] = s; labels[s] = id;
    while (sp > 0) {
      const t = stack[--sp], x = t % tw, y = (t - x) / tw;
      a++;
      if (x < bx0) bx0 = x; if (x > bx1) bx1 = x; if (y < by0) by0 = y; if (y > by1) by1 = y;
      if (x > 0 && kind[t - 1] === LAND && labels[t - 1] === -1) { labels[t - 1] = id; stack[sp++] = t - 1; }
      if (x < tw - 1 && kind[t + 1] === LAND && labels[t + 1] === -1) { labels[t + 1] = id; stack[sp++] = t + 1; }
      if (y > 0 && kind[t - tw] === LAND && labels[t - tw] === -1) { labels[t - tw] = id; stack[sp++] = t - tw; }
      if (y < th - 1 && kind[t + tw] === LAND && labels[t + tw] === -1) { labels[t + tw] = id; stack[sp++] = t + tw; }
    }
    area.push(a); x0.push(bx0); y0.push(by0); x1.push(bx1); y1.push(by1);
  }
  return { labels, count, area: Int32Array.from(area), x0: Int32Array.from(x0), y0: Int32Array.from(y0), x1: Int32Array.from(x1), y1: Int32Array.from(y1) };
}

/** Recursive subdivision: cut every face that is still too big, re-extract, repeat. Dead-end cuts leave the face
 *  whole, so it is cut again next round — which is how the dead-ends survive. */
function subdivide(c: Canvas, rng: RngHolder): void {
  for (let round = 0; round < 40; round++) {
    const f = components(c);
    let cut = 0;
    for (let id = 0; id < f.count; id++) {
      const bw = f.x1[id] - f.x0[id] + 1, bh = f.y1[id] - f.y0[id] + 1;
      if (bw <= FACE_MAX && bh <= FACE_MAX && f.area[id] <= FACE_MAX_AREA) continue;
      cut++;
      const vertical = bw > bh || (bw === bh && rngNext(rng) < 0.5);   // a vertical cut splits the width
      const span = vertical ? bw : bh, lo = vertical ? f.x0[id] : f.y0[id];
      const p = lo + span * rngUniform(rng, CUT_LO, CUT_HI);
      const slope = Math.tan(rngUniform(rng, -Math.PI / 18, Math.PI / 18));
      const w = SECONDARY_W[0] + rngInt(rng, SECONDARY_W[1] - SECONDARY_W[0] + 1);
      const u0 = (vertical ? f.y0[id] : f.x0[id]) - 2, u1 = (vertical ? f.y1[id] : f.x1[id]) + 2;
      let ua = u0, ub = u1;
      if (rngNext(rng) < DEAD_END_P) {
        const reach = rngUniform(rng, 0.55, 0.75) * (u1 - u0);
        if (rngNext(rng) < 0.5) ub = u0 + reach; else ua = u1 - reach;
      }
      const um = (u0 + u1) / 2;
      const pts = vertical ? line(p + slope * (ua - um), ua, p + slope * (ub - um), ub) : line(ua, p + slope * (ua - um), ub, p + slope * (ub - um));
      stamp(c, pts, w / 2, STREET, f.labels, id);
    }
    if (cut === 0) return;
  }
}

/** Distance of every land tile from the nearest non-land tile (or the canvas edge). */
function landDistance(c: Canvas): Int32Array {
  const { tw, th, kind } = c, n = tw * th;
  const dist = new Int32Array(n).fill(-1), q = new Int32Array(n);
  let head = 0, tail = 0;
  for (let t = 0; t < n; t++) {
    if (kind[t] !== LAND) { dist[t] = 0; q[tail++] = t; continue; }
    const x = t % tw, y = (t - x) / tw;
    if (x === 0 || y === 0 || x === tw - 1 || y === th - 1) { dist[t] = 1; q[tail++] = t; }
  }
  while (head < tail) {
    const t = q[head++], x = t % tw, d = dist[t] + 1;
    if (x > 0 && dist[t - 1] === -1) { dist[t - 1] = d; q[tail++] = t - 1; }
    if (x < tw - 1 && dist[t + 1] === -1) { dist[t + 1] = d; q[tail++] = t + 1; }
    if (t >= tw && dist[t - tw] === -1) { dist[t - tw] = d; q[tail++] = t - tw; }
    if (t + tw < n && dist[t + tw] === -1) { dist[t + tw] = d; q[tail++] = t + tw; }
  }
  return dist;
}

/* ------------------------------------------------------------------ build -------------------------------------- */

function paintStreets(c: Canvas, preset: CityPreset, rng: RngHolder): { riverY: Float64Array } {
  const { tw, th } = c;
  // canals first: the arterials painted over them are the bridges
  if (preset === 'canals') {
    for (const fx of [0.3, 0.7]) {
      const x0 = fx * tw + rngUniform(rng, -40, 40);
      const pts: [number, number][] = [[x0 + rngUniform(rng, -30, 30), th * 0.18], [x0 + rngUniform(rng, -40, 40), th * 0.5], [x0, th]];
      const s = catmull(pts);
      stamp(c, s, CANAL_W / 2 + CANAL_BANK_W, STREET);
      stamp(c, s, CANAL_W / 2, WATER);
    }
  }
  // boundary street
  stamp(c, line(0, BOUNDARY_W / 2, tw, BOUNDARY_W / 2), BOUNDARY_W / 2, STREET);
  stamp(c, line(BOUNDARY_W / 2, 0, BOUNDARY_W / 2, th), BOUNDARY_W / 2, STREET);
  stamp(c, line(tw - BOUNDARY_W / 2, 0, tw - BOUNDARY_W / 2, th), BOUNDARY_W / 2, STREET);
  // east–west arterials
  for (const fy of [0.30, 0.62]) {
    const pts: [number, number][] = [];
    for (const fx of [-0.03, 0.25, 0.5, 0.75, 1.03]) pts.push([fx * tw, fy * th + rngUniform(rng, -45, 45)]);
    stamp(c, catmull(pts), ARTERIAL_W[rngInt(rng, 2)] / 2, STREET);
  }
  // north–south arterials
  const ns = 2 + rngInt(rng, 2);
  for (let k = 0; k < ns; k++) {
    const fx = (k + 1) / (ns + 1), pts: [number, number][] = [];
    for (const fy of [-0.03, 0.25, 0.5, 0.75, 1.03]) pts.push([fx * tw + rngUniform(rng, -45, 45), fy * th]);
    stamp(c, catmull(pts), ARTERIAL_W[rngInt(rng, 2)] / 2, STREET);
  }
  // one diagonal avenue
  {
    const flip = rngNext(rng) < 0.5;
    const xa = rngUniform(rng, 0.05, 0.25) * tw, xb = rngUniform(rng, 0.75, 0.95) * tw;
    const pts: [number, number][] = [[flip ? xb : xa, th * 0.95], [tw / 2 + rngUniform(rng, -60, 60), th * 0.5 + rngUniform(rng, -60, 60)], [flip ? xa : xb, -10]];
    stamp(c, catmull(pts), AVENUE_W / 2, STREET);
  }
  if (preset === 'ring') {
    const cx = tw / 2 + rngUniform(rng, -30, 30), cy = th * 0.45 + rngUniform(rng, -30, 30), rx = tw * 0.36, ry = th * 0.34;
    const pts: number[] = [];
    for (let a = 0; a <= 360; a += 0.5) pts.push(cx + rx * Math.cos(a * Math.PI / 180), cy + ry * Math.sin(a * Math.PI / 180));
    stamp(c, pts, ARTERIAL_W[1] / 2, STREET);
  }
  // the river, last: embankment street then water, filled to the bottom edge
  const rpts: [number, number][] = [];
  for (const fx of [-0.03, 0.25, 0.5, 0.75, 1.03]) rpts.push([fx * tw, th - 90 + rngUniform(rng, -40, 40)]);
  const rs = catmull(rpts, 0.25);
  const riverY = new Float64Array(tw).fill(th - 90);
  for (let i = 0; i < rs.length; i += 2) { const x = Math.round(rs[i]); if (x >= 0 && x < tw) riverY[x] = rs[i + 1]; }
  for (let x = 0; x < tw; x++) {
    const top = Math.round(riverY[x] - RIVER_W / 2);
    for (let y = Math.max(0, top - EMBANKMENT_W); y < th; y++) c.kind[y * tw + x] = y < top ? STREET : WATER;
  }
  return { riverY };
}

function largestSquare(owner: Int32Array, tw: number, id: number, x0: number, y0: number, x1: number, y1: number): [number, number, number] {
  const bw = x1 - x0 + 1, bh = y1 - y0 + 1, dp = new Int32Array(bw * bh);
  let best = 0, bx = x0, by = y0;
  for (let y = 0; y < bh; y++) for (let x = 0; x < bw; x++) {
    if (owner[(y0 + y) * tw + x0 + x] !== id) continue;
    const v = x === 0 || y === 0 ? 1 : Math.min(dp[(y - 1) * bw + x], dp[y * bw + x - 1], dp[(y - 1) * bw + x - 1]) + 1;
    dp[y * bw + x] = v;
    if (v > best) { best = v; bx = x0 + x - v + 1; by = y0 + y - v + 1; }
  }
  return [best, bx, by];
}

function build(seed: number, preset: CityPreset, attempt: number, tw: number, th: number): CityGeom {
  const rng: RngHolder = { rng: seedRng(Math.imul(seed, 2654435) + Math.imul(attempt + 1, 40503) + preset.length) };
  const n = tw * th, c: Canvas = { tw, th, kind: new Uint8Array(n) };
  paintStreets(c, preset, rng);
  subdivide(c, rng);
  // faces that are too small or too thin become street
  {
    const f = components(c), dist = landDistance(c), pole = new Int32Array(f.count);
    for (let t = 0; t < n; t++) if (f.labels[t] >= 0 && dist[t] > pole[f.labels[t]]) pole[f.labels[t]] = dist[t];
    const drop = new Uint8Array(f.count);
    for (let id = 0; id < f.count; id++) {
      const bw = f.x1[id] - f.x0[id] + 1, bh = f.y1[id] - f.y0[id] + 1;
      if (f.area[id] < FACE_MIN_AREA || Math.min(bw, bh) < FACE_MIN_ACROSS || pole[id] < FACE_MIN_ACROSS / 2) drop[id] = 1;
    }
    for (let t = 0; t < n; t++) if (f.labels[t] >= 0 && drop[f.labels[t]]) c.kind[t] = STREET;
  }
  const f = components(c), dist = landDistance(c), owner = new Int32Array(n);
  for (let t = 0; t < n; t++) owner[t] = f.labels[t] >= 0 ? f.labels[t] : c.kind[t] === WATER ? -2 : -1;
  const nb = f.count;
  // blocks: tiles, pole, square
  const tilesOf: number[][] = Array.from({ length: nb }, () => []);
  const poleD = new Int32Array(nb).fill(-1), poleT = new Int32Array(nb);
  for (let t = 0; t < n; t++) {
    const id = owner[t]; if (id < 0) continue;
    tilesOf[id].push(t);
    if (dist[t] > poleD[id]) { poleD[id] = dist[t]; poleT[id] = t; }
  }
  const blocks: CityBlock[] = [];
  for (let id = 0; id < nb; id++) {
    const [sq, sqx, sqy] = largestSquare(owner, tw, id, f.x0[id], f.y0[id], f.x1[id], f.y1[id]);
    blocks.push({ id, tiles: Int32Array.from(tilesOf[id]), area: f.area[id], cx: poleT[id] % tw, cy: Math.floor(poleT[id] / tw),
                  x0: f.x0[id], y0: f.y0[id], x1: f.x1[id], y1: f.y1[id], sq, sqx, sqy, nb: [], river: false, edge: false, inert: false });
  }
  // watershed over the street tiles: every street tile belongs to its nearest block
  const near = new Int32Array(n).fill(-1), q = new Int32Array(n);
  let head = 0, tail = 0;
  const waterD = new Int32Array(n).fill(-1);
  for (let t = 0; t < n; t++) {
    if (owner[t] < 0) continue;
    const x = t % tw;
    const adj = (x > 0 && c.kind[t - 1] === STREET) || (x < tw - 1 && c.kind[t + 1] === STREET) || (t >= tw && c.kind[t - tw] === STREET) || (t + tw < n && c.kind[t + tw] === STREET);
    if (adj) { near[t] = owner[t]; q[tail++] = t; }
  }
  while (head < tail) {
    const t = q[head++], x = t % tw, id = near[t];
    if (x > 0 && c.kind[t - 1] === STREET && near[t - 1] === -1) { near[t - 1] = id; q[tail++] = t - 1; }
    if (x < tw - 1 && c.kind[t + 1] === STREET && near[t + 1] === -1) { near[t + 1] = id; q[tail++] = t + 1; }
    if (t >= tw && c.kind[t - tw] === STREET && near[t - tw] === -1) { near[t - tw] = id; q[tail++] = t - tw; }
    if (t + tw < n && c.kind[t + tw] === STREET && near[t + tw] === -1) { near[t + tw] = id; q[tail++] = t + tw; }
  }
  // river / edge flags: street tiles within reach of water or the canvas edge
  head = 0; tail = 0;
  const edgeD = new Int32Array(n).fill(-1);
  const bfsStreet = (d: Int32Array, seed: (t: number, x: number, y: number) => boolean, limit: number) => {
    head = 0; tail = 0;
    for (let t = 0; t < n; t++) { const x = t % tw, y = (t - x) / tw; if (seed(t, x, y)) { d[t] = 0; q[tail++] = t; } }
    while (head < tail) {
      const t = q[head++], x = t % tw, dd = d[t] + 1;
      if (dd > limit) continue;
      if (x > 0 && c.kind[t - 1] === STREET && d[t - 1] === -1) { d[t - 1] = dd; q[tail++] = t - 1; }
      if (x < tw - 1 && c.kind[t + 1] === STREET && d[t + 1] === -1) { d[t + 1] = dd; q[tail++] = t + 1; }
      if (t >= tw && c.kind[t - tw] === STREET && d[t - tw] === -1) { d[t - tw] = dd; q[tail++] = t - tw; }
      if (t + tw < n && c.kind[t + tw] === STREET && d[t + tw] === -1) { d[t + tw] = dd; q[tail++] = t + tw; }
    }
  };
  bfsStreet(waterD, t => c.kind[t] === WATER, EMBANKMENT_W + 6);
  bfsStreet(edgeD, (t, x, y) => c.kind[t] !== WATER && (x === 0 || y === 0 || x === tw - 1), BOUNDARY_W + 4);
  for (let t = 0; t < n; t++) {
    const id = near[t]; if (id < 0 || owner[t] >= 0) continue;
    if (waterD[t] >= 0) blocks[id].river = true;
    if (edgeD[t] >= 0) blocks[id].edge = true;
  }
  // ridges: street tiles whose 4-neighbour belongs to another block
  const ridgeOf = new Map<number, number[]>(), lastPush = new Map<number, number>();
  const across = new Int32Array(n).fill(-1);
  for (let t = 0; t < n; t++) {
    if (c.kind[t] !== STREET) continue;
    const a = near[t], x = t % tw;
    const nbs = [x > 0 ? t - 1 : -1, x < tw - 1 ? t + 1 : -1, t >= tw ? t - tw : -1, t + tw < n ? t + tw : -1];
    for (const u of nbs) {
      if (u < 0 || c.kind[u] !== STREET) continue;
      const b = near[u]; if (b === a || b < 0 || a < 0) continue;
      if (across[t] === -1) across[t] = b;
      const k = segKey(nb, a, b);
      if (lastPush.get(k) === t) continue;
      lastPush.set(k, t);
      let l = ridgeOf.get(k); if (!l) { l = []; ridgeOf.set(k, l); }
      l.push(t);
    }
  }
  // the side across the street, carried from the ridge back to the kerb within each watershed
  head = 0; tail = 0;
  for (let t = 0; t < n; t++) if (across[t] >= 0) q[tail++] = t;
  while (head < tail) {
    const t = q[head++], x = t % tw, id = near[t], b = across[t];
    for (const u of [x > 0 ? t - 1 : -1, x < tw - 1 ? t + 1 : -1, t >= tw ? t - tw : -1, t + tw < n ? t + tw : -1]) {
      if (u < 0 || c.kind[u] !== STREET || near[u] !== id || across[u] !== -1) continue;
      across[u] = b; q[tail++] = u;
    }
  }
  const segs: CitySeg[] = [], segAt = new Map<number, number>();
  const keys = [...ridgeOf.keys()].sort((p, r) => p - r);
  /** GAME-ASSUMPTION: two lots share a street only if their ridge is at least 5 tiles long; shorter touches (the
   *  diagonal corners of a crossing) are not fronts. */
  for (const k of keys) {
    const ridge = ridgeOf.get(k)!;
    if (ridge.length < 5) continue;
    const a = Math.floor(k / nb), b = k % nb;
    let mx = 0, my = 0;
    for (const t of ridge) { mx += t % tw; my += Math.floor(t / tw); }
    segAt.set(k, segs.length);
    segs.push({ a, b, len: ridge.length, ridge: Int32Array.from(ridge), frontA: new Int32Array(0), frontB: new Int32Array(0),
                mx: Math.round(mx / ridge.length), my: Math.round(my / ridge.length) });
    blocks[a].nb.push(b); blocks[b].nb.push(a);
  }
  for (const b of blocks) b.nb.sort((p, r) => p - r);
  // fronts: lot tiles on the kerb, assigned to the segment their street tile looks across to
  const frontLists = segs.map(() => [[] as number[], [] as number[]]);
  const stampSeen = new Int32Array(n).fill(-1);
  for (let t = 0; t < n; t++) {
    const a = owner[t]; if (a < 0) continue;
    const x = t % tw;
    for (const u of [x > 0 ? t - 1 : -1, x < tw - 1 ? t + 1 : -1, t >= tw ? t - tw : -1, t + tw < n ? t + tw : -1]) {
      if (u < 0 || c.kind[u] !== STREET) continue;
      const b = across[u]; if (b < 0 || b === a) continue;
      const si = segAt.get(segKey(nb, a, b)); if (si === undefined) continue;
      const side = segs[si].a === a ? 0 : 1, mark = si * 2 + side;
      if (stampSeen[t] === mark) continue;
      stampSeen[t] = mark; frontLists[si][side].push(t);
    }
  }
  // a side whose kerb looks across to a third block everywhere (a short ridge at a corner) takes the lot tiles
  // nearest its ridge instead: every edge has a front, however small (a turret ring of one tile is still a ring)
  const seen = new Int32Array(n).fill(-1);
  segs.forEach((s, i) => {
    for (const side of [0, 1] as const) {
      if (frontLists[i][side].length) continue;
      const id = side === 0 ? s.a : s.b, mark = i * 2 + side;
      head = 0; tail = 0;
      for (const t of s.ridge) { if (seen[t] !== mark) { seen[t] = mark; q[tail++] = t; } }
      let found = false, depthEnd = tail;
      while (head < tail && !found) {
        while (head < depthEnd) {
          const t = q[head++], x = t % tw;
          for (const u of [x > 0 ? t - 1 : -1, x < tw - 1 ? t + 1 : -1, t >= tw ? t - tw : -1, t + tw < n ? t + tw : -1]) {
            if (u < 0 || seen[u] === mark) continue;
            if (owner[u] === id) { seen[u] = mark; frontLists[i][side].push(u); found = true; continue; }
            if (c.kind[u] !== STREET) continue;
            seen[u] = mark; q[tail++] = u;
          }
        }
        depthEnd = tail;
      }
    }
  });
  segs.forEach((s, i) => { s.frontA = Int32Array.from(frontLists[i][0]); s.frontB = Int32Array.from(frontLists[i][1]); });
  const g: CityGeom = {
    seed, preset, attempt, tw, th, kind: c.kind, owner, near, blocks, segs, segAt,
    hops: new Int32Array(nb).fill(-1), hq: -1, foundry: -1, wells: [], facilities: [], survivors: [],
    district: new Uint8Array(nb), valid: false, reasons: [],
  };
  decorate(g, preset, rng);
  return g;
}

/* ------------------------------------------------------------------ decorate ----------------------------------- */

function octant(g: CityGeom, from: number, to: number): number {
  const a = g.blocks[from], b = g.blocks[to];
  const ang = Math.atan2(b.cy - a.cy, b.cx - a.cx);
  return Math.floor((ang + Math.PI) / (Math.PI / 4)) % 8;
}

function pick(rng: RngHolder, cands: number[]): number { return cands.length ? cands[rngInt(rng, cands.length)] : -1; }

function decorate(g: CityGeom, preset: CityPreset, rng: RngHolder): void {
  const { blocks } = g, nb = blocks.length, nbList = blocks.map(b => b.nb);
  const reasons = g.reasons;
  // plazas and parks
  const inertN = Math.round(nb * rngUniform(rng, INERT_LO, INERT_HI));
  const order = blocks.map(b => b.id);
  for (let i = order.length - 1; i > 0; i--) { const j = rngInt(rng, i + 1); [order[i], order[j]] = [order[j], order[i]]; }
  for (let i = 0, k = 0; i < order.length && k < inertN; i++) {
    const b = blocks[order[i]];
    if (b.nb.length < 2) continue;   // keep the dead-end pockets as lots: they are the §8 pockets
    // a plaza must not strand a neighbour below two land neighbours (§17) unless that neighbour is river-adjacent
    if (b.nb.some(j => blocks[j].nb.filter(m => m !== b.id && !blocks[m].inert).length < (blocks[j].river ? 1 : 2))) continue;   // nobody stranded: river lots keep one street, the rest two
    b.inert = true; k++;
  }
  const landNb = (i: number) => blocks[i].nb.filter(j => !blocks[j].inert).length;
  // the HQ: river-adjacent, central, a 24-square, 3–4 land neighbours
  const hqC = blocks.filter(b => b.river && !b.inert && b.sq >= HQ_SQ && landNb(b.id) >= 3 && landNb(b.id) <= 4).map(b => b.id);
  hqC.sort((p, r) => Math.abs(blocks[p].cx - g.tw / 2) - Math.abs(blocks[r].cx - g.tw / 2));
  if (!hqC.length) { reasons.push('no HQ candidate (river-adjacent, 24-square, 3–4 land neighbours)'); return; }
  g.hq = hqC[rngInt(rng, Math.min(3, hqC.length))];
  blocks[g.hq].inert = false;
  const hops = bfsHops(nbList, g.hq);
  g.hops = hops;
  let maxHops = 0;
  for (let i = 0; i < nb; i++) if (hops[i] > maxHops) maxHops = hops[i];
  if (hops.some(h => h < 0)) reasons.push(`${hops.filter(h => h < 0).length} blocks unreachable from the HQ`);
  const hq = blocks[g.hq];
  const taken = new Set<number>([g.hq]);
  const free = (i: number) => !blocks[i].inert && !taken.has(i) && hops[i] >= 0;
  // the Foundry: 6–8 hops, north, a facility square
  const fC = blocks.filter(b => free(b.id) && hops[b.id] >= 6 && hops[b.id] <= 8 && b.cy < hq.cy - 120 && b.sq >= FAC_SQ).map(b => b.id);
  g.foundry = pick(rng, fC);
  if (g.foundry < 0) { reasons.push('no Foundry candidate (hops 6–8, north of the HQ, 16-square)'); return; }
  taken.add(g.foundry);
  const hopsT = bfsHops(nbList, g.foundry);
  // districts: bands of graph distance from the HQ (§17); the Foundry's surroundings are industrial
  /** GAME-ASSUMPTION: the outskirts are the city's fringe — every block within two hops of the boundary street — so the
   *  §17 rule "reachable without crossing more than two outskirt edges" holds by construction, as it did on the lattice
   *  (a 3–4 block band). §17's "beyond 60 %" would make an 8-hop-deep outskirts on a 20-hop city and break that rule;
   *  the bands civic ≤ 30 %, residential ≤ 45 %, industrial beyond apply inside the fringe. Question for the human:
   *  fringe (thin, as the lattice had it) or band (deep, as §17's percentages read)? */
  const edgeHops = bfsHopsMulti(nbList, blocks.filter(b => b.edge).map(b => b.id));
  const isInd = preset === 'industrial';
  for (let i = 0; i < nb; i++) {
    const f = hops[i] < 0 ? 1 : hops[i] / maxHops;
    let d: number;
    if (f <= 0.30) d = rngNext(rng) < 1 / 3 ? 1 : 0;
    else if (f <= 0.45) d = 1;
    else d = 2;
    if (edgeHops[i] >= 0 && edgeHops[i] <= OUTSKIRT_DEPTH && hops[i] > 2) d = 3;
    if (hopsT[i] >= 0 && hopsT[i] <= 2 && d !== 3) d = 2;
    if (preset === 'noout' && d === 3) d = 2;
    if (isInd && d !== 3) d = 2;
    g.district[i] = d;
  }
  // a fringe pocket whose way inward runs along the fringe (more than two outskirt edges) is a deep lot, not outskirts
  for (let pass = 0; pass < 4; pass++) {
    const cost = outskirtCost(g);
    let changed = 0;
    for (let i = 0; i < nb; i++) if (g.district[i] === 3 && cost[i] > OUTSKIRT_DEPTH) { g.district[i] = 2; changed++; }
    if (!changed) break;
  }
  g.district[g.hq] = 0;
  // wells: one two hops past the Foundry, two in the far northern corners, two riverside flanks
  const wells: number[] = [];
  const addWell = (cands: number[], what: string) => {
    const w = pick(rng, cands.filter(i => free(i) && !wells.includes(i) && wells.every(o => Math.abs(blocks[o].cx - blocks[i].cx) + Math.abs(blocks[o].cy - blocks[i].cy) > 120)));
    if (w < 0) reasons.push(`no well site: ${what}`); else { wells.push(w); taken.add(w); }
  };
  addWell(blocks.filter(b => hopsT[b.id] === 2 && hops[b.id] > hops[g.foundry]).map(b => b.id), 'past the Foundry');
  const corner = (score: (b: CityBlock) => number) => blocks.filter(b => free(b.id) && b.edge).sort((p, r) => score(p) - score(r)).slice(0, 4).map(b => b.id);
  addWell(corner(b => b.cx + b.cy), 'north-west corner');
  addWell(corner(b => (g.tw - b.cx) + b.cy), 'north-east corner');
  /** GAME-ASSUMPTION: no well within WELL_RANGE + 1 hops of the HQ, so its influence (WELL_RANGE hops) never reaches the
   *  HQ ring: a well at 3 hops (seed 4, first draft) put shades on the HQ's edges at minute 2.5 and the HQ fell at 7.7
   *  minutes on every policy. The lattice kept its wells ≥ 6 blocks from the start for the same reason. */
  const wellMin = WELL_RANGE + 2;
  addWell(blocks.filter(b => b.river && hops[b.id] >= wellMin && hops[b.id] <= 12 && b.cx < hq.cx - 60).map(b => b.id), 'west riverside');
  addWell(blocks.filter(b => b.river && hops[b.id] >= wellMin && hops[b.id] <= 12 && b.cx > hq.cx + 60).map(b => b.id), 'east riverside');
  g.wells = wells;
  const wellHops = wells.map(w => bfsHops(nbList, w));
  const nearWell = (i: number, r: number) => wellHops.some(wh => wh[i] >= 0 && wh[i] <= r);
  /** GAME-ASSUMPTION: "no facility behind a well" = no facility within one hop of a well and farther from the HQ than
   *  that well; the shortest way in would pass the well's ground. */
  const behindWell = (i: number) => wellHops.some((wh, k) => wh[i] >= 0 && wh[i] <= 1 && hops[i] > hops[wells[k]]);
  const facilities: { name: string; block: number }[] = [{ name: 'Foundry', block: g.foundry }];
  const place = (name: string, ok: (b: CityBlock) => boolean, relaxed?: (b: CityBlock) => boolean) => {
    let cands = blocks.filter(b => free(b.id) && b.sq >= FAC_SQ && !nearWell(b.id, 1) && !behindWell(b.id) && ok(b)).map(b => b.id);
    if (!cands.length && relaxed) cands = blocks.filter(b => free(b.id) && b.sq >= FAC_SQ && !nearWell(b.id, 1) && !behindWell(b.id) && relaxed(b)).map(b => b.id);
    const i = pick(rng, cands);
    if (i < 0) { reasons.push(`no site for ${name}`); return; }
    taken.add(i); facilities.push({ name, block: i });
  };
  const fo = octant(g, g.hq, g.foundry);
  place('Arsenal', b => hops[b.id] >= 5 && hops[b.id] <= 9 && octant(g, g.hq, b.id) !== fo);
  place('Turbine hall', b => hops[b.id] >= 5 && hops[b.id] <= 9 && b.cx > hq.cx + 60 && octant(g, g.hq, b.id) !== fo);
  place('Refinery', b => hops[b.id] >= 8 && hops[b.id] <= 12 && Math.abs(b.cx - hq.cx) >= 100);
  /** GAME-ASSUMPTION: "≥ 15 blocks" was a lattice number (a city hop is ~1.35 lattice blocks, 43 vs 32 tiles of pitch);
   *  on the graph the Power station sits at ≥ 60 % of the city's depth, on the far side from the Foundry. */
  const psMin = Math.max(10, Math.round(0.6 * maxHops));
  place('Power station', b => hops[b.id] >= psMin && Math.abs(octant(g, g.hq, b.id) - fo) % 8 >= 2 && Math.abs(octant(g, g.hq, b.id) - fo) % 8 <= 6,
        b => hops[b.id] >= psMin - 2 && octant(g, g.hq, b.id) !== fo);
  place('Tram depot', b => hops[b.id] >= TRAM_HOPS[0] && hops[b.id] <= TRAM_HOPS[1]);
  g.facilities = facilities;
  // survivors: §8 bands by hops
  const survivors: { name: string; tag: string; block: number }[] = [];
  for (const band of SURVIVOR_BANDS) {
    const i = pick(rng, blocks.filter(b => free(b.id) && hops[b.id] >= band.min && hops[b.id] <= band.max).map(b => b.id));
    if (i < 0) { reasons.push(`no block for ${band.name}`); continue; }
    taken.add(i); survivors.push({ name: band.name, tag: band.tag, block: i });
  }
  g.survivors = survivors;
  validate(g, preset, maxHops);
}

/** Outskirt edges (edges between two outskirt blocks) on the cheapest way in from the nearest non-outskirt block:
 *  a 0-1 BFS from the non-outskirt set. */
function outskirtCost(g: CityGeom): Int32Array {
  const { blocks } = g, nb = blocks.length;
  const cost = new Int32Array(nb).fill(1 << 30);
  const dq: number[] = [];
  for (const b of blocks) if (g.district[b.id] !== 3) { cost[b.id] = 0; dq.push(b.id); }
  while (dq.length) {
    const i = dq.shift()!;
    for (const j of blocks[i].nb) {
      const c = cost[i] + (g.district[i] === 3 && g.district[j] === 3 ? 1 : 0);
      if (c < cost[j]) { cost[j] = c; if (c === cost[i]) dq.unshift(j); else dq.push(j); }
    }
  }
  return cost;
}

/** §17 validator on the graph. */
function validate(g: CityGeom, preset: CityPreset, maxHops: number): void {
  const { blocks, reasons, hops } = g;
  const land = blocks.filter(b => !b.inert);
  for (const b of land) if (b.nb.filter(j => !blocks[j].inert).length < 2 && !b.river && b.id !== g.hq) { reasons.push(`block ${b.id} has fewer than 2 land neighbours and is not river-adjacent`); break; }
  for (const b of land) if (b.nb.filter(j => !blocks[j].inert).length < 1) { reasons.push(`block ${b.id} has no land neighbour (unreachable in play)`); break; }
  const cost = outskirtCost(g);
  const deep = land.filter(b => g.district[b.id] === 3 && cost[b.id] > 2).length;
  if (deep > 0) reasons.push(`${deep} outskirt blocks need more than two outskirt edges to reach`);
  if (g.facilities.length < 6) reasons.push('a facility is missing');
  const need = preset === 'industrial' ? [2, 3] : preset === 'noout' ? [0, 1, 2] : [0, 1, 2, 3];
  for (const d of need) if (land.filter(b => g.district[b.id] === d).length < 3) reasons.push(`district ${DISTRICT_NAMES[d]} has fewer than 3 blocks`);
  if (g.wells.length > 6) reasons.push('more than 6 wells');
  if (g.wells.length < 4) reasons.push('fewer than 4 wells');
  for (const w of g.wells) if (hops[w] >= 0 && hops[w] <= WELL_RANGE + 1) { reasons.push(`well #${w} at ${hops[w]} hops reaches the HQ ring`); break; }
  if (maxHops < 12) reasons.push(`the city is only ${maxHops} hops deep`);
  if (hops[g.foundry] < 5 || hops[g.foundry] > 9) reasons.push('Foundry is not 5–9 hops out');
  g.valid = reasons.length === 0;
}

/* ------------------------------------------------------------------ api ---------------------------------------- */

const cache = new Map<string, CityGeom>();

/** The city for a seed and preset: the first of CITY_ATTEMPTS jitter streams that validates, else the last one
 *  (with `valid` false and its reasons — the seed browser shows them). Cached per process. */
export function generateCity(seed: number, preset: CityPreset = 'river', opts: CityOpts = {}): CityGeom {
  const tw = opts.tw ?? CITY_TW, th = opts.th ?? CITY_TH, attempts = opts.attempts ?? CITY_ATTEMPTS;
  if (!Number.isInteger(attempts) || attempts < 1 || attempts > CITY_ATTEMPTS) throw new Error(`City attempts must be 1..${CITY_ATTEMPTS}`);
  const key = `${seed}:${preset}:${tw}x${th}:${attempts}`;
  const hit = cache.get(key);
  if (hit) return hit;
  let g: CityGeom | undefined;
  for (let a = 0; a < attempts; a++) {
    g = build(seed, preset, a, tw, th);
    if (g.valid) break;
  }
  if (cache.size > 64) cache.clear();
  cache.set(key, g!);
  return g!;
}
