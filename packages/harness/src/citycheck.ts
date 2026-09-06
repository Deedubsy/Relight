/** RI-02A focused compatibility evidence. Stock grants are confined to the labelled capacity QA. */
import assert from 'node:assert/strict';
import { writeFileSync, mkdirSync } from 'node:fs';
import {
  DEFAULT_CONFIG, protoCalibrated, citySpec, createState, ground, cityGeomOf, ensureFlow, hqIdx,
  findPath, passable, advanceFlow, workbenchTile, canPlace, place, Kind, createHourBot, runHour,
  stateHash, makeSave, loadState, enableHeart, HELD, frontage, interior, isInterior,
  HOUR_CLAIM_AT, configHash,
} from '@relight/sim';
import { stamp, type ConfigRef } from './provenance';

const out = 'docs/evidence/city-rebuild';
mkdirSync(out, { recursive: true });
const configRef: ConfigRef = { kind: 'calibration', map: 'river', walk: true, economy: true,
  overrides: { power: true, supply: 'generators', draw: 'half' } };
export function cityState(seed: number, profile = true) {
  const cfg = protoCalibrated({ ...DEFAULT_CONFIG, scatter: true, economy: true, walk: true });
  Object.assign(cfg, { power: true, supply: 'generators', draw: 'half' });
  const spec = citySpec(seed, 'river', cfg);
  if (profile) spec.city!.profile = 'riverside-v1';
  const st = createState(spec, cfg, seed);
  ensureFlow(st);
  return st;
}

const rows = [];
for (const seed of [3, 4, 5, 6]) {
  const t0 = performance.now(), old = cityState(seed, false), st = cityState(seed), G = ground(st), O = ground(old), city = G.urban!, hq = hqIdx(st), cg = cityGeomOf(st);
  const generationMs = performance.now() - t0;
  assert.ok(cg.valid);
  assert.deepEqual(st.nb, old.nb); assert.deepEqual(st.ring, old.ring);
  assert.deepEqual(G.base, O.base); assert.deepEqual(G.patch, O.patch);
  assert.deepEqual(G.blocks.map(b => b.count), O.blocks.map(b => b.count));
  assert.deepEqual(G.blocks[hq].order, O.blocks[hq].order);
  assert.deepEqual(G.blocks[G.railYard].order, O.blocks[O.railYard].order);
  const reload = loadState(makeSave(st));
  assert.equal(stateHash(reload), stateHash(st));
  assert.deepEqual(ground(reload).urban, city);
  let occupied = 0;
  for (const s of city.structures) {
    for (let y = s.y; y < s.y + s.h; y++) for (let x = s.x; x < s.x + s.w; x++) {
      assert.equal(passable(st, x, y), false);
      assert.equal(G.rank[y * G.tw + x], -1, 'no resource under a roof');
      assert.equal(G.owner[y * G.tw + x], s.block); occupied++;
    }
    assert.ok(passable(st, ...s.door), 'every door has an open approach');
  }
  const routes = [];
  for (const name of ['Ironworks Yard', 'Exchange Square']) {
    const landmark = city.places.find(p => p.name === name);
    assert.ok(landmark && city.structures.some(s => s.block === landmark.block), `${name} has a real building`);
  }
  const [wx, wy] = workbenchTile(st);
  for (const [x, y] of city.railReserve.feeders) assert.ok(findPath(st, wx, wy, x, y), 'reserved cabinet approach reachable');
  for (const p of city.places.filter(p => st.nb[hq].includes(p.block) || ['Ironworks Yard', 'Exchange Square'].includes(p.name))) {
    const sub = G.blocks[p.block].sub;
    const tx = sub ? sub.x - 1 : p.pad.x, ty = sub ? sub.y + 1 : p.pad.y;
    const path = findPath(st, wx, wy, tx, ty), legacy = findPath(old, wx, wy, tx, ty);
    assert.ok(path && legacy, `${seed}: ${p.name} accessible`);
    for (const t of path) assert.ok(passable(st, t % G.tw, Math.floor(t / G.tw)));
    // Exercise movement, never teleport. The same command as the map's walk target.
    advanceFlow(st, 0.05, [{ type: 'move', x: tx + 0.5, y: ty + 0.5 }]);
    advanceFlow(st, path.length / 4 + 10);
    assert.ok(Math.hypot(st.engineer.x - tx - 0.5, st.engineer.y - ty - 0.5) < 0.1, `walk arrived at ${p.name}`);
    const back = findPath(st, tx, ty, wx, wy)!;
    advanceFlow(st, 0.05, [{ type: 'move', x: wx + 0.5, y: wy + 0.5 }]);
    advanceFlow(st, back.length / 4 + 10);
    assert.ok(Math.hypot(st.engineer.x - wx - 0.5, st.engineer.y - wy - 0.5) < 0.1);
    routes.push({ place: p.name, oldTiles: legacy.length, newTiles: path.length, walkedBothWays: true });
  }
  // The existing candidate's cabinet selection must still find two accessible sites.
  const H = enableHeart(st);
  assert.ok(H && H.cabinets.length === 2);
  for (const c of H.cabinets) assert.ok(findPath(st, wx, wy, c.x, c.y));

  const factory = cityState(seed), bot = createHourBot(false);
  runHour(factory, bot, 20 * 60);
  assert.ok(bot.log.refused.every(r => r.reason.startsWith('not enough in the pockets')), JSON.stringify(bot.log.refused));
  assert.ok(factory.flow!.machines.filter(m => m.kind === 'excavator').length >= 3);
  assert.ok(factory.flow!.machines.some(m => m.kind === 'assembler'));
  assert.ok(factory.flow!.stats.made.magazine > 0, 'real production');
  // Capacity-only scenario: carried machines do not imply minute-zero affordability.
  const capacity = loadState(makeSave(factory)), extras = [];
  for (const kind of ['assembler', 'generator', 'assembler'] as Kind[]) {
    capacity.engineer.inv[kind] = 1;
    let at: [number, number] | null = null;
    for (const t of ground(capacity).blocks[hq].tiles) {
      const x = t % G.tw, y = Math.floor(t / G.tw);
      if (canPlace(capacity, kind, x, y).ok) { at = [x, y]; break; }
    }
    assert.ok(at, `additional ${kind} footprint fits`);
    assert.ok(place(capacity, kind, ...at, 1)); extras.push({ kind, at });
  }
  const interiors = (profile: boolean) => {
    const c = cityState(seed, profile);
    // Labelled ownership-geometry QA; no economy pacing claim. Secure the HQ's live
    // neighbours and measure the resulting front using the real adjacency queries.
    for (const i of c.nb[hq]) if (c.blocks[i].state !== 3) c.blocks[i].state = HELD;
    assert.ok(isInterior(c, hq), 'small local ring encloses the HQ');
    return { front: frontage(c), interior: interior(c), held: c.blocks.filter(b => b.state === HELD).length };
  };
  assert.deepEqual(interiors(true), interiors(false));
  assert.equal(stamp(configRef).config_hash, configHash(st.config));
  const row = { seed, profile: city.profile, configHash: configHash(st.config), generationMs: +generationMs.toFixed(1), blocks: st.blocks.length, streets: cg.segs.length,
    startFront: old.ring.length, inert: cg.blocks.filter(b => b.inert).length, structures: city.structures.length, occupied,
    hqLotTiles: G.blocks[hq].tiles.length, hqOccupied: city.structures.filter(s => s.block === hq).length,
    resourcesUnchanged: true, routes, extraCapacityQA: extras, factory: { at: factory.t, machines: factory.flow!.machines.length, made: factory.flow!.stats.made, refused: bot.log.refused },
    saveHash: stateHash(reload), railCabinetsReachable: H!.cabinets.length, enclosureGeometryQA: interiors(true) };
  rows.push(row);
  if (seed === 3) {
    writeFileSync('packages/game/public/snapshots/city-factory-qa.json', JSON.stringify({ ...stamp(configRef), ...makeSave(factory) }));
    writeFileSync('packages/game/public/snapshots/city-opening-qa.json', JSON.stringify({ ...stamp(configRef), ...makeSave(cityState(3)) }));
    writeFileSync(`${out}/places.json`, JSON.stringify({ ...stamp(configRef), places: city.places.filter(p => cg.hops[p.block] <= 2 || ['Ironworks Yard', 'Exchange Square'].includes(p.name)), structures: city.structures, rail: city.railReserve, hq: G.hqOrigin }, null, 2));
  }
  console.log(JSON.stringify(row));
}
const transport = [];
for (const seed of [3, 4, 5]) {
  const st = cityState(seed), bot = createHourBot(false, 'chest', HOUR_CLAIM_AT.north, 'physical', 'tram');
  runHour(st, bot, 35 * 60);
  const row = { seed, moved: st.flow!.stats.tramMoved, refusals: bot.log.refused, held: st.blocks.filter(b => b.state === HELD).length };
  transport.push(row); console.log('transport', JSON.stringify(row));
  assert.ok((row.moved ?? 0) > 0, 'tram made an actual delivery');
  assert.equal(row.refusals.length, 0);
}
writeFileSync(`${out}/compatibility.json`, JSON.stringify({ ...stamp(configRef), node: process.version, rows, transport }, null, 2));
