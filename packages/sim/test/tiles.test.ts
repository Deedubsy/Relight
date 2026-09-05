/** §4 tile ground on faces (prompt B M1): geometry, the river and inert faces, rubble counts and types scaled by
 *  area, the depth gradient, the block map's authority over the tiles, determinism and the cost of a whole city.
 *  Every check derives tiles from a SimState and never writes one. The lattice ground (the M1–M3 fixtures, docsync)
 *  keeps its own checks in flow.test.ts and defence.test.ts. */
import { test } from 'node:test';
import assert from 'node:assert/strict';
import {
  DEFAULT_CONFIG, createState, citySpec, generateCity, claim, step, INERT, VOID, HELD, SimState,
  STREET, WATER, T_STREET, T_GROUND, T_RUBBLE, T_INERT, T_RIVER, T_DEPOSIT, T_PATCH,
  RUBBLE_TILES_MIN, RUBBLE_TILES_MAX, RUBBLE_VARIANTS, HQ_RUBBLE_TILES, HQ_PATCHES, LATTICE_AREA, DEPOSIT_TILES,
  rubbleOf, ground, groundTiles, tileAt, chunkTiles, chunkKey, blockKey, standing, poolCap, describeGround, hqLot, CHUNK,
  RAIL_YARD_COAL_TILES, RAIL_YARD_COAL, RUBBLE_UNITS_PER_TILE, tileUnits,
} from '../src/index';

function fresh(seed = 3, economy = false): SimState {
  const cfg = { ...DEFAULT_CONFIG, eco: { ...DEFAULT_CONFIG.eco }, economy };
  return createState(citySpec(seed, 'river', cfg), cfg, seed);
}

test('geometry: a lot is a rasterised face, streets are the ridges between faces, the river is water; 800×800', () => {
  const st = fresh();
  const cg = generateCity(3, 'river'), G = ground(st);
  assert.equal(G.lattice, false);
  assert.equal(G.tw, cg.tw); assert.equal(G.th, cg.th); assert.equal(G.tw, 800);
  assert.equal(G.blocks.length, st.blocks.length); assert.equal(st.blocks.length, cg.blocks.length);
  let street = 0, water = 0, lot = 0;
  for (let t = 0; t < G.tw * G.th; t++) {
    const k = cg.kind[t];
    if (k === STREET) { street++; assert.equal(G.base[t], T_STREET); assert.equal(G.owner[t], -1); assert.equal(G.near[t], cg.near[t]); }
    else if (k === WATER) { water++; assert.equal(G.base[t], T_RIVER); assert.equal(G.owner[t], -2); }
    else { lot++; assert.equal(G.base[t], T_GROUND); assert.equal(G.owner[t], cg.owner[t]); assert.equal(G.owner[t] >= 0, true); }
  }
  assert.ok(street > 0 && water > 0 && lot > street, `${lot} lot, ${street} street, ${water} water tiles`);
  // every block's tiles are its face's tiles, and its pole is inside it
  for (const bg of G.blocks) {
    const cb = cg.blocks[bg.i];
    assert.equal(bg.tiles, cb.tiles);
    assert.deepEqual(bg.pole, [cb.cx, cb.cy]);
    assert.equal(G.owner[cb.cy * G.tw + cb.cx], bg.i, 'the pole is on the face');
  }
  // the HQ lot's 24×24 frame holds the §11 patches, on HQ tiles
  const hq = G.blocks[st.blocks.findIndex(b => b.x === st.start[0] && b.y === st.start[1])];
  assert.ok(hq.hq);
  for (const p of HQ_PATCHES) {
    const [tx, ty] = hqLot(st, p.lx, p.ly);
    assert.equal(tileAt(st, tx, ty).kind, T_PATCH); assert.equal(tileAt(st, tx, ty).patch, p.type); assert.equal(G.owner[ty * G.tw + tx], hq.i);
  }
  assert.ok(hq.sub && hq.sub.size === 3, 'the HQ has its 3×3 substation');
});

test('the river is river tiles and no block; plazas and parks are inert faces with no rubble, no substation, no lights; outskirts faces (§7) have neither a substation nor streetlights', () => {
  const st = fresh();
  const G = ground(st), cg = generateCity(3, 'river');
  let river = 0;
  for (let t = 0; t < G.tw * G.th; t++) if (G.base[t] === T_RIVER) { river++; const v = tileAt(st, t % G.tw, Math.floor(t / G.tw)); assert.equal(v.kind, T_RIVER); assert.equal(v.block, -1); }
  assert.ok(river > 5000, `${river} river tiles`);
  const inert = G.blocks.filter(bg => cg.blocks[bg.i].inert);
  assert.ok(inert.length > 0, 'the city has plazas or parks');
  for (const bg of inert) {
    assert.ok(st.blocks[bg.i].state === INERT || st.blocks[bg.i].state === VOID);
    assert.equal(bg.count, 0); assert.equal(bg.rubble, null); assert.equal(bg.sub, null); assert.equal(bg.lights.length, 0);
    for (const t of bg.tiles) assert.equal(tileAt(st, t % G.tw, Math.floor(t / G.tw)).kind, T_INERT);
  }
  // §7 (prompt B M3): an outskirts face has no substation and no streetlights — the Electricians' Substation gives it one
  const out = G.blocks.filter(bg => !cg.blocks[bg.i].inert && st.blocks[bg.i].state !== INERT && cg.district[bg.i] === 3);
  assert.ok(out.length > 0, 'the city has outskirts faces');
  for (const bg of out) { assert.equal(bg.sub, null); assert.equal(bg.lights.length, 0); }
  // every other live face has a substation on its own tiles and lights on its streets
  const live = G.blocks.filter(bg => !cg.blocks[bg.i].inert && st.blocks[bg.i].state !== INERT && cg.district[bg.i] !== 3);
  let threeByThree = 0, lit = 0;
  for (const bg of live) {
    assert.ok(bg.sub);
    if (bg.sub!.size === 3) threeByThree++;
    for (let dy = 0; dy < bg.sub!.size; dy++) for (let dx = 0; dx < bg.sub!.size; dx++) assert.equal(G.owner[(bg.sub!.y + dy) * G.tw + bg.sub!.x + dx], bg.i);
    for (const l of bg.lights) { assert.equal(G.base[l.ty * G.tw + l.tx], T_STREET); assert.equal(G.near[l.ty * G.tw + l.tx], bg.i); }
    if (bg.lights.length > 0) lit++;
  }
  assert.ok(threeByThree >= live.length * 0.9, `${threeByThree}/${live.length} faces fit a 3×3 substation`);
  assert.ok(lit === live.length, `${lit}/${live.length} faces have streetlights`);
});

test('rubble: 250–350 tiles scaled by area, typed by district, in clusters, five variants; outskirts carry deposits or nothing', () => {
  const st = fresh();
  const G = ground(st), cg = generateCity(3, 'river'), tiles = groundTiles(st);
  const seen = { stone: 0, copper: 0, steel: 0, coal: 0 };
  let deposits = 0, outskirts = 0, big = 0, small = 0, bigCount = 0, smallCount = 0;
  const variants = new Set<number>();
  for (const bg of G.blocks) {
    const b = st.blocks[bg.i];
    if (cg.blocks[bg.i].inert || b.state === INERT) continue;
    const scale = bg.tiles.length / LATTICE_AREA;
    if (b.name === 'out') {
      outskirts++;
      assert.equal(bg.rubble, null);
      if (bg.deposit) { deposits++; assert.equal(bg.count, Math.min(Math.round(scale * DEPOSIT_TILES), bg.count)); assert.ok(bg.count > 0); }
      else assert.equal(bg.count, 0);
      continue;
    }
    // RI-01 (D-P4-12, rules change reported in docs/RI_PASS_1_REPORT.md): the rail yard is the one district face whose
    // rubble is not its district's — one 3×3 coal heap of RAIL_YARD_COAL units, nothing else standing, the densest variant
    if (bg.i === G.railYard) {
      assert.equal(bg.rubble, 'coal'); assert.equal(bg.count, RAIL_YARD_COAL_TILES);
      assert.ok(Math.abs(tileUnits(G, bg.i) * bg.count - RAIL_YARD_COAL) < 1e-6, 'the heap holds RAIL_YARD_COAL units in all');
      for (const t of bg.tiles) if (tiles.kind[t] === T_RUBBLE) assert.equal(tiles.variant[t], RUBBLE_VARIANTS);
      assert.equal(standing(st, bg.i), bg.count);
      seen.coal++;
      continue;
    }
    assert.equal(tileUnits(G, bg.i), RUBBLE_UNITS_PER_TILE, 'a district tile holds §12\'s 300 units (D-P4-2)');
    assert.equal(bg.rubble, rubbleOf(b.name));
    seen[bg.rubble!]++;
    if (bg.hq) assert.equal(bg.count, HQ_RUBBLE_TILES, 'the HQ face is cleared (M2)');
    else {
      assert.ok(bg.count >= Math.round(scale * RUBBLE_TILES_MIN) - 1 && bg.count <= Math.round(scale * RUBBLE_TILES_MAX) + 1, `${bg.count} rubble tiles on face ${bg.i} of ${bg.tiles.length} tiles`);
      if (bg.tiles.length >= 2 * LATTICE_AREA) { big++; bigCount += bg.count; }
      if (bg.tiles.length <= LATTICE_AREA / 2 && bg.tiles.length > 0) { small++; smallCount += bg.count; }
    }
    let n = 0;
    for (const t of bg.tiles) {
      const k = tiles.kind[t];
      if (k === T_RUBBLE) { n++; assert.ok(tiles.variant[t] >= 1 && tiles.variant[t] <= RUBBLE_VARIANTS); variants.add(tiles.variant[t]); }
      else assert.equal(tiles.variant[t], 0);
      assert.notEqual(k, T_STREET); assert.notEqual(k, T_RIVER);
    }
    assert.equal(n, bg.count, 'a fresh block has all its rubble standing');
    assert.equal(standing(st, bg.i), bg.count);
    // clusters: the densest tile's 13×13 neighbourhood holds far more rubble than the face's average density
    if (!bg.hq && bg.count > 50 && bg.count / bg.tiles.length < 0.45) {
      const t0 = bg.order[bg.count - 1], x0 = t0 % G.tw, y0 = Math.floor(t0 / G.tw);
      let near = 0, nearAll = 0;
      for (const t of bg.tiles) { const x = t % G.tw, y = Math.floor(t / G.tw); if (Math.abs(x - x0) <= 6 && Math.abs(y - y0) <= 6) { nearAll++; if (tiles.kind[t] === T_RUBBLE) near++; } }
      const dens = bg.count / bg.tiles.length;
      assert.ok(near / nearAll > Math.min(1.5 * dens, dens + 0.2), `face ${bg.i}: ${(near / nearAll).toFixed(2)} near the densest heap vs ${(bg.count / bg.tiles.length).toFixed(2)} overall`);
    }
  }
  assert.ok(seen.stone > 0 && seen.copper > 0 && seen.steel > 0 && seen.coal === 1, JSON.stringify(seen));
  assert.ok(outskirts > 0 && deposits > 0 && deposits < outskirts, `${deposits} deposits on ${outskirts} outskirts faces`);
  assert.ok(big > 0 && small > 0 && bigCount / big > 2 * smallCount / small, `a big face carries more: ${(bigCount / big).toFixed(0)} vs ${(smallCount / small).toFixed(0)} (${big} big, ${small} small)`);
  assert.equal(variants.size, RUBBLE_VARIANTS, 'all five variants appear');
});

test('the density gradient: deeper faces (hops from the HQ) show heavier rubble on average', () => {
  const st = fresh();
  const G = ground(st), cg = generateCity(3, 'river'), tiles = groundTiles(st);
  const mean = (bg: (typeof G.blocks)[number]) => { let s = 0, n = 0; for (const t of bg.tiles) if (tiles.kind[t] === T_RUBBLE) { s += tiles.variant[t]; n++; } return n ? s / n : 0; };
  // the rail yard (RI-01, D-P4-12) is a designated coal heap, not a district face on the gradient
  const res = G.blocks.filter(bg => st.blocks[bg.i].name === 'res' && !bg.hq && bg.count > 0 && bg.i !== G.railYard);
  const shallow = res.filter(bg => cg.hops[bg.i] <= 2), deep = res.filter(bg => cg.hops[bg.i] >= 4);
  assert.ok(shallow.length > 0 && deep.length > 0, `${shallow.length} shallow, ${deep.length} deep residential faces`);
  const avg = (xs: number[]) => xs.reduce((a, b) => a + b, 0) / xs.length;
  const ms = avg(shallow.map(mean)), md = avg(deep.map(mean));
  assert.ok(md > ms, `deep ${md.toFixed(2)} > shallow ${ms.toFixed(2)}`);
});

test('block state is authoritative: the pool drains and rubble becomes ground in the layout order; a claim digs; the keys track it', () => {
  const st = fresh(3, true);   // the economy on: a Held block draws its pool down
  const G = ground(st);
  const hqi = st.blocks.findIndex(b => b.x === st.start[0] && b.y === st.start[1]), hq = st.blocks[hqi], bg = G.blocks[hqi];
  assert.equal(hq.state, HELD);
  const key0 = blockKey(st, hqi), ck0 = chunkKey(st, bg.pole[0] >> 5, bg.pole[1] >> 5);
  assert.equal(standing(st, hqi), bg.count);
  assert.ok(Math.abs(poolCap(st, hq) - hq.pool) < 1e-6, 'the HQ starts with a full pool scaled to its face');
  hq.pool = poolCap(st, hq) / 2;
  assert.equal(standing(st, hqi), Math.round(bg.count / 2));
  assert.notEqual(blockKey(st, hqi), key0); assert.notEqual(chunkKey(st, bg.pole[0] >> 5, bg.pole[1] >> 5), ck0);
  const first = bg.order[0], last = bg.order[bg.count - 1];
  assert.equal(tileAt(st, first % G.tw, Math.floor(first / G.tw)).kind, T_GROUND, 'the first tile in the order is dug');
  assert.equal(tileAt(st, last % G.tw, Math.floor(last / G.tw)).kind, T_RUBBLE, 'the last tile in the order still stands');
  hq.pool = 0;
  for (const t of bg.tiles) assert.notEqual(tileAt(st, t % G.tw, Math.floor(t / G.tw)).kind, T_RUBBLE);
  // claiming a neighbour: a Held face yields, so its heaps only shrink, in the layout's order
  const ni = st.nb[hqi].find(j => st.blocks[j].state !== INERT && G.blocks[j].count > 0)!;
  const nb = st.blocks[ni], nbg = G.blocks[ni];
  const before = chunkTiles(st, nbg.pole[0] >> 5, nbg.pole[1] >> 5), s0 = standing(st, ni);
  assert.ok(claim(st, nb.x, nb.y));
  for (let k = 0; k < 300; k++) step(st);   // Held by then; the default config loses it to a bloom around t = 450
  assert.equal(nb.state, HELD);
  const after = chunkTiles(st, nbg.pole[0] >> 5, nbg.pole[1] >> 5);
  assert.ok(standing(st, ni) < s0, `a Held face digs its rubble (${s0} → ${standing(st, ni)})`);
  for (let i = 0; i < after.kind.length; i++) {
    if (after.kind[i] === T_RUBBLE) assert.equal(before.kind[i], T_RUBBLE);
    else if (before.kind[i] !== T_RUBBLE) assert.equal(after.kind[i], before.kind[i]);
    else assert.equal(after.kind[i], T_GROUND);
  }
  assert.equal(after.kind.length, CHUNK * CHUNK);
});

test('deterministic per seed; a different seed lays the rubble differently; a whole city derives under 100 ms cold', () => {
  const a = groundTiles(fresh(3)), b = groundTiles(fresh(3)), c = groundTiles(fresh(4));
  assert.deepEqual(a.kind, b.kind); assert.deepEqual(a.variant, b.variant);
  assert.notDeepEqual(a.kind, c.kind);
  const st = fresh(5);
  const t0 = performance.now();
  const G = ground(st);
  const tiles = groundTiles(st);
  const ms = performance.now() - t0;
  assert.ok(ms < 100, `ground + groundTiles took ${ms.toFixed(0)} ms cold`);
  let rubble = 0, deposit = 0;
  for (let i = 0; i < tiles.kind.length; i++) { if (tiles.kind[i] === T_RUBBLE) rubble++; else if (tiles.kind[i] === T_DEPOSIT) deposit++; }
  assert.ok(rubble > 20_000, `${rubble} rubble tiles in the city`); assert.ok(deposit > 0);
  const hqPole = G.blocks[st.blocks.findIndex(x => x.x === st.start[0] && x.y === st.start[1])].pole;
  assert.match(describeGround(st, hqPole[0], hqPole[1]), /^tile \(\d+,\d+\) · /);
});
