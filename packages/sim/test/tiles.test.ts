/** §4 tile ground (Phase 4 M1): geometry, streets, river, inert cells, rubble counts and types, the pool-driven
 *  dig-out, determinism, and the cost of a whole city. The block map stays authoritative: every check derives
 *  tiles from a SimState and never writes one. */
import { test } from 'node:test';
import assert from 'node:assert/strict';
import {
  DEFAULT_CONFIG, protoCalibrated, generateMap, createState, claim, step, idxOf, INERT, HELD,
  CELL_TILES, LOT_TILES, MARGIN_TILES, STREET_TILES, T_STREET, T_GROUND, T_RUBBLE, T_INERT, T_RIVER, T_DEPOSIT,
  RUBBLE_TILES_MIN, RUBBLE_TILES_MAX, RUBBLE_VARIANTS, cellTiles, cityTiles, lotLayout, rubbleLeft, cellKey, tileCell, describeTile,
  rubbleOf, poolMax, HQ_RUBBLE_TILES,
} from '../src/index';

function fresh(seed = 3) {
  const cfg = protoCalibrated({ ...DEFAULT_CONFIG, scatter: true, economy: true });
  const spec = generateMap(seed, cfg);
  return createState(spec, cfg, seed);
}

test('geometry: 32-tile cells, 24-tile lots, 4-tile margins, 8-wide shared streets, 768×768 city', () => {
  assert.equal(CELL_TILES, LOT_TILES + 2 * MARGIN_TILES);
  assert.equal(STREET_TILES, 2 * MARGIN_TILES);
  const st = fresh();
  const city = cityTiles(st);
  assert.equal(city.w, 768); assert.equal(city.h, 768);
  assert.equal(city.kind.length, 768 * 768);
  // the street between cells (5, 5) and (6, 5): tile columns 6·32−4 .. 6·32+3 are street on every lot row
  for (let ty = 5 * 32 + 4; ty < 5 * 32 + 28; ty++) {
    for (let tx = 6 * 32 - 4; tx < 6 * 32 + 4; tx++) assert.equal(city.kind[ty * 768 + tx], T_STREET, `street at ${tx},${ty}`);
    assert.notEqual(city.kind[ty * 768 + 6 * 32 - 5], T_STREET, 'lot tile left of the street');
    assert.notEqual(city.kind[ty * 768 + 6 * 32 + 4], T_STREET, 'lot tile right of the street');
  }
  // a cell's lot is exactly the 24×24 interior
  const c = cellTiles(st, 5, 5);
  let lot = 0, street = 0;
  for (let i = 0; i < c.kind.length; i++) (c.kind[i] === T_STREET ? street++ : lot++);
  assert.equal(street, 32 * 32 - 24 * 24); assert.equal(lot, 24 * 24);
  assert.deepEqual(tileCell(6 * 32 + 5, 5 * 32 + 9), { x: 6, y: 5, lx: 5, ly: 9 });
});

test('the river row is river tiles under a 4-tile embankment street; inert cells are inert lots ringed by street', () => {
  const st = fresh();
  const r = cellTiles(st, 12, st.h - 1);
  for (let ty = 0; ty < 32; ty++) for (let tx = 0; tx < 32; tx++) assert.equal(r.kind[ty * 32 + tx], ty < MARGIN_TILES ? T_STREET : T_RIVER);
  assert.equal(r.rubbleTiles, 0);
  const inertCells = st.blocks.filter(b => b.state === INERT && b.y !== st.h - 1);
  assert.ok(inertCells.length > 0, 'the scattered map has inert cells');
  const c = cellTiles(st, inertCells[0].x, inertCells[0].y);
  for (let ty = 0; ty < 32; ty++) for (let tx = 0; tx < 32; tx++) {
    const onLot = tx >= 4 && tx < 28 && ty >= 4 && ty < 28;
    assert.equal(c.kind[ty * 32 + tx], onLot ? T_INERT : T_STREET);
  }
  assert.equal(c.rubbleTiles, 0); assert.equal(c.rubble, null);
});

test('rubble: 250–350 tiles per lot, typed by district, five density variants; outskirts carry deposits or nothing', () => {
  const st = fresh();
  const seen = { stone: 0, copper: 0, steel: 0 };
  let deposits = 0, outskirts = 0, minCount = 1e9, maxCount = 0;
  const variants = new Set<number>();
  for (const b of st.blocks) {
    if (b.y === st.h - 1 || b.state === INERT) continue;
    const c = cellTiles(st, b.x, b.y);
    const kinds = new Set<number>();
    for (let i = 0; i < c.kind.length; i++) if (c.kind[i] !== T_STREET) kinds.add(c.kind[i]);
    if (b.name === 'out') {
      outskirts++;
      assert.equal(c.rubble, null);
      assert.ok(!kinds.has(T_RUBBLE));
      if (c.deposit) { deposits++; assert.ok(kinds.has(T_DEPOSIT)); assert.ok(c.rubbleTiles > 0); }
      else assert.deepEqual([...kinds], [T_GROUND]);
      continue;
    }
    assert.equal(c.rubble, rubbleOf(b.name));
    assert.equal(c.rubble, b.name === 'civ' ? 'stone' : b.name === 'res' ? 'copper' : 'steel');
    seen[c.rubble!]++;
    if (b.x === st.start[0] && b.y === st.start[1]) assert.equal(c.rubbleTiles, HQ_RUBBLE_TILES, 'the start lot is cleared for the HQ (M2)');
    else assert.ok(c.rubbleTiles >= RUBBLE_TILES_MIN && c.rubbleTiles <= RUBBLE_TILES_MAX, `${c.rubbleTiles} rubble tiles on (${b.x},${b.y})`);
    minCount = Math.min(minCount, c.rubbleTiles); maxCount = Math.max(maxCount, c.rubbleTiles);
    let n = 0;
    for (let i = 0; i < c.kind.length; i++) if (c.kind[i] === T_RUBBLE) { n++; assert.ok(c.variant[i] >= 1 && c.variant[i] <= RUBBLE_VARIANTS); variants.add(c.variant[i]); }
    else assert.equal(c.variant[i] === 0 || c.kind[i] === T_DEPOSIT, true);
    assert.equal(n, c.rubbleLeft, 'standing rubble equals the tile count');
    assert.equal(n, c.rubbleTiles, 'a fresh block has all its rubble');
  }
  assert.ok(seen.stone > 0 && seen.copper > 0 && seen.steel > 0);
  assert.ok(outskirts > 0 && deposits > 0 && deposits < outskirts);
  assert.ok(maxCount - minCount > 30, `counts vary across the range (${minCount}–${maxCount})`);
  assert.equal(variants.size, RUBBLE_VARIANTS, 'all five variants appear');
});

test('the density gradient: deeper blocks show heavier rubble on average', () => {
  const st = fresh();
  const mean = (x: number, y: number) => { const c = cellTiles(st, x, y); let s = 0, n = 0; for (let i = 0; i < c.kind.length; i++) if (c.kind[i] === T_RUBBLE) { s += c.variant[i]; n++; } return s / n; };
  // a residential lot beside the HQ against one at the far west of the same band
  const near = st.blocks.find(b => b.name === 'res' && b.state !== INERT && Math.abs(b.x - st.start[0]) + Math.abs(b.y - st.start[1]) === 1)!;
  const far = st.blocks.filter(b => b.name === 'res' && b.state !== INERT && b.y !== st.h - 1).sort((a, b) => (Math.abs(b.x - st.start[0]) + Math.abs(b.y - st.start[1])) - (Math.abs(a.x - st.start[0]) + Math.abs(a.y - st.start[1])))[0];
  assert.ok(mean(far.x, far.y) > mean(near.x, near.y), `far ${mean(far.x, far.y).toFixed(2)} > near ${mean(near.x, near.y).toFixed(2)}`);
});

test('block state is authoritative: the pool drains and rubble tiles become ground; the key tracks both', () => {
  const st = fresh();
  const [sx, sy] = st.start;
  const hq = st.blocks[idxOf(st, sx, sy)];
  assert.equal(hq.state, HELD);
  const lay = lotLayout(st.seed, hq, true);
  const key0 = cellKey(st, sx, sy);
  assert.equal(rubbleLeft(st, hq), lay.tiles);
  // half the pool → half the tiles, dug in the layout's order
  hq.pool = poolMax(st, hq.name) / 2;
  const half = cellTiles(st, sx, sy);
  assert.equal(half.rubbleLeft, Math.round(lay.tiles / 2));
  assert.notEqual(cellKey(st, sx, sy), key0);
  const dugFirst = lay.order[0], lx = dugFirst % LOT_TILES, ly = (dugFirst - lx) / LOT_TILES;
  assert.equal(half.kind[(ly + MARGIN_TILES) * CELL_TILES + lx + MARGIN_TILES], T_GROUND, 'the first tile in the order is dug');
  const dugLast = lay.order[lay.tiles - 1], lx2 = dugLast % LOT_TILES, ly2 = (dugLast - lx2) / LOT_TILES;
  assert.equal(half.kind[(ly2 + MARGIN_TILES) * CELL_TILES + lx2 + MARGIN_TILES], T_RUBBLE, 'the last tile in the order still stands');
  hq.pool = 0;
  const dry = cellTiles(st, sx, sy);
  assert.equal(dry.rubbleLeft, 0);
  for (let i = 0; i < dry.kind.length; i++) assert.notEqual(dry.kind[i], T_RUBBLE);
  // claiming a block does not move its rubble: a Held block yields, so its heaps only shrink, in the layout's order
  const nb = st.blocks.find(b => Math.abs(b.x - sx) + Math.abs(b.y - sy) === 1 && b.state !== INERT)!;
  const before = cellTiles(st, nb.x, nb.y);
  assert.ok(claim(st, nb.x, nb.y));
  for (let k = 0; k < 600; k++) step(st);
  assert.equal(nb.state, HELD);
  const after = cellTiles(st, nb.x, nb.y);
  assert.ok(after.rubbleLeft < before.rubbleLeft, `a Held block digs its rubble (${before.rubbleLeft} → ${after.rubbleLeft})`);
  for (let i = 0; i < after.kind.length; i++) {
    if (after.kind[i] === T_RUBBLE) assert.equal(before.kind[i], T_RUBBLE);
    else if (before.kind[i] !== T_RUBBLE) assert.equal(after.kind[i], before.kind[i]);
    else assert.equal(after.kind[i], T_GROUND);
  }
});

test('deterministic per seed; a different seed lays the rubble differently; a whole city is cheap', () => {
  const a = cellTiles(fresh(3), 10, 10), b = cellTiles(fresh(3), 10, 10), c = cellTiles(fresh(4), 10, 10);
  assert.deepEqual(a.kind, b.kind); assert.deepEqual(a.variant, b.variant);
  assert.notDeepEqual(a.kind, c.kind);
  const st = fresh(5);
  const t0 = performance.now();
  const city = cityTiles(st);
  const ms = performance.now() - t0;
  assert.ok(ms < 1500, `cityTiles took ${ms.toFixed(0)} ms`);
  let rubble = 0;
  for (let i = 0; i < city.kind.length; i++) if (city.kind[i] === T_RUBBLE) rubble++;
  assert.ok(rubble > 80_000, `${rubble} rubble tiles in the city`);
  assert.match(describeTile(st, 12 * 32 + 10, 22 * 32 + 10), /^tile \(394,714\) · /);
});
