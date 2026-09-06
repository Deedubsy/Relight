/** EX-06A labelled logistics scenarios: injected stocks, no balance/play claims. */
import { test } from 'node:test';
import assert from 'node:assert/strict';
import {
  createState, citySpec, protoCalibrated, DEFAULT_CONFIG, ensureFlow, Machine, StationRule,
  transferFreight, freightInbound, makeSave, loadState, stateHash, conservation, pickUpItems,
} from '../src/index';

function rule(request = 0, reserve = 0, exporting = false): StationRule { return { request, reserve, export: exporting }; }
function scenario() {
  const cfg = protoCalibrated({ ...DEFAULT_CONFIG, walk: true });
  const st = createState(citySpec(3, 'river', cfg), cfg, 3);
  const f = ensureFlow(st); st.version = 2;
  const machine = (kind: 'tramstop' | 'tram'): Machine => ({ id: f.next++, kind, x: 0, y: 0, dir: 1, size: kind === 'tram' ? 1 : 2, items: [], hold: null, timer: 0, phase: 0, inv: {}, cargo: {}, out: 0, busy: false });
  const a = machine('tramstop'), b = machine('tramstop'), c = machine('tramstop'), tram = machine('tram'), other = machine('tram');
  f.machines.push(a, b, c, tram, other);
  a.freight = { coal: rule(0, 20, true), copper: rule(15) };
  b.freight = { coal: rule(10), copper: rule(0, 5, true) };
  c.freight = { coal: rule(30) };
  a.inv = { coal: 80 }; b.inv = { copper: 20 };
  const stops = [a, b, c];
  const visit = (s: Machine, powered = true, t = tram) => transferFreight(st, t, s, stops, powered);
  return { st, a, b, c, tram, other, stops, visit };
}

test('three stations reserve only their own deliveries; stock and in-flight reservations prevent double ordering across trams; exports return upstream', () => {
  const { st, a, b, c, tram, other, visit } = scenario();
  const ledger = conservation(st).unexplained;
  visit(a);
  assert.equal(a.inv.coal, 40); assert.equal(tram.cargo!.coal, 40);
  assert.equal(freightInbound(st, c.id, 'coal'), 30);
  visit(a, true, other); assert.deepEqual(other.cargo, {}, 'second tram cannot order stock already in flight');
  visit(b);
  assert.equal(b.cargo!.coal, 10); assert.equal(tram.cargo!.coal, 30, 'middle station cannot consume the last station’s coal');
  assert.equal(b.inv.copper, 5, 'export reserve stays on the platform');
  visit(c); assert.equal(c.cargo!.coal, 30); assert.equal(tram.cargo!.copper, 15);
  visit(b); visit(a);
  assert.equal(a.cargo!.copper, 15); assert.deepEqual(tram.cargo, {});
  assert.deepEqual(conservation(st).unexplained, ledger, 'metadata never owns a second copy of cargo');
  a.freight!.coal!.reserve = 39; c.freight!.coal!.request = 100;
  visit(a); assert.equal(a.inv.coal, 39); assert.equal(tram.cargo!.coal, 1, 'only export stock above the reserve');
});

test('capacity is reserved across all items and trams; partial refusal returns the remainder without losing it', () => {
  const { st, a, b, c, tram, other, visit } = scenario();
  b.freight = {}; c.freight = { coal: rule(200), copper: rule(200) };
  a.freight!.copper = rule(0, 0, true); a.inv = { coal: 120, copper: 80 }; c.cargo = { stone: 50 };
  visit(a); assert.equal(freightInbound(st, c.id), 150);
  assert.equal(tram.cargo!.coal, 70); assert.equal(tram.cargo!.copper, 80);
  visit(a, true, other); assert.deepEqual(other.cargo, {});
  c.cargo.stone = 195; // a local delivery competes for the arrivals buffer after dispatch
  visit(c); assert.equal(c.cargo.copper, 5); // stable catalogue order: copper before coal
  assert.equal(freightInbound(st, a.id), 145);
  visit(b); assert.deepEqual(b.cargo, {}, 'a return shipment cannot unload at an intermediate stop');
  visit(a); assert.equal(a.cargo!.coal, 70); assert.equal(a.cargo!.copper, 75);
});

test('unpowered stops retain cargo with bounded service; a refused destination returns home, and removal of either endpoint remains recoverable', () => {
  const { st, a, b, c, tram, stops, visit } = scenario();
  visit(a, false); assert.equal(a.inv.coal, 80); assert.deepEqual(tram.cargo, {});
  visit(a); visit(b, false); visit(c, false);
  assert.equal(tram.cargo!.coal, 40); assert.equal(freightInbound(st, a.id), 40);
  visit(a, false); assert.equal(tram.cargo!.coal, 40);
  visit(a); assert.equal(a.cargo!.coal, 40);
  a.cargo = {}; visit(a);
  stops.splice(stops.indexOf(c), 1); // destination no longer lies on the connected route
  visit(b); assert.ok(tram.manifest!.some(r => r.destination === c.id && r.returning));
  stops.splice(stops.indexOf(a), 1);
  visit(b); assert.ok((pickUpItems(tram).coal ?? 0) > 0, 'orphaned freight is included in normal pickup recovery');
});

test('a mid-shipment save resumes to the same hash; old saves stay version one and invalid manifests are rejected', () => {
  const { st, a, b, c, tram, visit } = scenario(); visit(a);
  const saved = makeSave(st), back = loadState(saved);
  assert.equal(saved.version, 2); assert.equal(stateHash(st), stateHash(back));
  for (const stop of [b, c, a]) {
    visit(stop);
    const machines = back.flow!.machines;
    transferFreight(back, machines.find(m => m.id === tram.id)!, machines.find(m => m.id === stop.id)!, machines.filter(m => m.kind === 'tramstop'), true);
  }
  assert.equal(stateHash(st), stateHash(back));
  const broken = structuredClone(saved);
  broken.state.flow!.machines.find(m => m.id === tram.id)!.manifest![0].n = 199;
  assert.throws(() => loadState(broken), /reservation exceeds/);
  const downgraded = structuredClone(saved); downgraded.state.version = 1; downgraded.version = 1;
  assert.throws(() => loadState(downgraded), /requires save version 2/);
  assert.throws(() => loadState({ ...saved, version: 1 }), /versions differ/);
  assert.throws(() => loadState({ ...saved, version: 99 }), /version 99 is unsupported/);
  const invalidRules = structuredClone(saved); invalidRules.state.flow!.machines.find(m => m.id === a.id)!.freight!.coal!.request = -1;
  assert.throws(() => loadState(invalidRules), /invalid station freight rules/);
  const cfg = protoCalibrated(DEFAULT_CONFIG), old = createState(citySpec(3, 'river', cfg), cfg, 3);
  assert.equal(makeSave(old).version, 1); assert.equal(stateHash(loadState(makeSave(old))), stateHash(old));
});
