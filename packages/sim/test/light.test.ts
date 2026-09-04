/** Prompt B M5 Light (run name B-M5-light): the light map is the shade rule's lit set — streets lit by their kerb
 *  streetlights, lots unlit beyond a Lamp's reach; a claimed block's streetlights come on in sequence from the
 *  substation out at three a second and its burn-off runs 20 + 60·d s; E repairs a broken or eaten light for copper. */
import { test } from 'node:test';
import assert from 'node:assert/strict';
import {
  DEFAULT_CONFIG, createState, citySpec, ensureFlow, advanceFlow, SimState, HELD, CONTESTED, DARK, claim, layPoles,
  lightMask, litCount, litAt, lightAt, blockLights, canRepair, repairLight, contestProgress, burnOffS, LIGHT_SEQ_PER_S, REPAIR_COPPER,
  ground, place, hqLot, LAMP_RADIUS, FLOODLIGHT_RANGE, subPowered, threatOf,
} from '../src/index';

function city(seed = 3): SimState {
  const cfg = { ...DEFAULT_CONFIG, eco: { ...DEFAULT_CONFIG.eco } };
  const st = createState(citySpec(seed, 'river', cfg), cfg, seed);
  ensureFlow(st);
  st.engineer.inv.steel = 500; st.engineer.inv.copper = 200;
  return st;
}
const hqIndex = (st: SimState) => st.blocks.findIndex(b => b.x === st.start[0] && b.y === st.start[1]);
function runS(st: SimState, seconds: number): void { for (let s = 0; s < seconds; s++) advanceFlow(st, 1, [], 4); }

test('light map: the HQ face\'s street is lit by its kerb streetlights, its lot is unlit beyond them, and the mask agrees with litAt everywhere', () => {
  const st = city();
  runS(st, 1);
  const hq = hqIndex(st), G = ground(st), mask = lightMask(st);
  assert.ok(subPowered(st, st.blocks[hq]), 'the HQ powers its streetlights');
  const c = litCount(st, hq, mask);
  // the kerb row (the strip the streetlights stand on); the half-street to the midline is 6–7 tiles deep and a radius-4
  // light cannot reach its far side, so it lights ≈ 40 % — reported in SLICE_REPORT "M5 disagrees" (verification pass)
  assert.ok(c.kerbOf > 0 && c.kerb / c.kerbOf > 0.5, `most of the HQ's kerb row is lit (${c.kerb}/${c.kerbOf}; 3 in 8 lamps are broken)`);
  assert.ok(c.streetOf > 0 && c.street / c.streetOf > 0.25, `the half-street is lit in part (${c.street}/${c.streetOf})`);
  assert.ok(c.lot < c.lotOf, `the lot is not lit end to end (${c.lot}/${c.lotOf})`);
  const bg = G.blocks[hq];
  assert.equal(mask[bg.pole[1] * G.tw + bg.pole[0]], 0, 'the lot\'s pole of inaccessibility (farthest from any street) is unlit');
  for (let k = 0; k < 400; k++) {
    const t = bg.tiles[(k * 7919) % bg.tiles.length], tx = t % G.tw, ty = Math.floor(t / G.tw);
    assert.equal(!!mask[t], litAt(st, tx, ty), `mask and litAt agree at (${tx},${ty})`);
  }
  // a Dark neighbour's tiles: unlit but where the HQ's streetlights reach across the street
  for (const j of st.blocks.map((b, i) => i).filter(i => st.blocks[i].state === DARK)) {
    const cj = litCount(st, j, mask);
    assert.ok(cj.lot <= cj.lotOf * 0.25, `a Dark lot is mostly unlit (${cj.lot}/${cj.lotOf})`);
  }
});

test('a Lamp lights its radius on the lot; a Floodlight its cone', () => {
  const st = city();
  const [lx, ly] = hqLot(st, 6, 6);
  const lamp = place(st, 'lamp', lx, ly, 0);
  assert.ok(lamp, 'the lamp places');
  runS(st, 1);
  const G = ground(st), mask = lightMask(st);
  assert.equal(mask[ly * G.tw + lx], 1);
  assert.equal(mask[ly * G.tw + lx + LAMP_RADIUS], 1, 'the radius edge is lit');
  assert.equal(mask[(ly + LAMP_RADIUS) * G.tw + lx + LAMP_RADIUS], litAt(st, lx + LAMP_RADIUS, ly + LAMP_RADIUS) ? 1 : 0, 'the corner follows the disc rule');
  assert.ok(FLOODLIGHT_RANGE > LAMP_RADIUS);
});

test('burn-off: the claimed block\'s streetlights come on from the substation out at three a second; progress runs 0 → 1 over 20 + 60·d s and then the block is Held', () => {
  const st = city();
  runS(st, 1);
  const hq = hqIndex(st);
  const nb = st.blocks.findIndex((b, i) => b.state === DARK && ground(st).blocks[i].lights.length >= 6 && ground(st).blocks[i].sub && st.blocks[hq] && st.ring.some(e => (e.a === hq && e.b === i) || (e.a === i && e.b === hq)));
  assert.ok(nb >= 0, 'a Dark neighbour with streetlights');
  assert.ok(claim(st, st.blocks[nb].x, st.blocks[nb].y), 'the claim goes through');
  layPoles(st, st.blocks[nb].x, st.blocks[nb].y);
  const b = st.blocks[nb];
  assert.equal(b.state, CONTESTED);
  const len = burnOffS(b.d);
  assert.ok(Math.abs(b.contestUntil - st.t - len) < 1e-6, `contestUntil is 20 + 60·d from the claim (${len} s at d ${b.d.toFixed(2)})`);
  assert.ok(Math.abs(contestProgress(st, nb)) < 1e-6, 'progress 0 at the claim');
  const lights = () => blockLights(st, nb).filter(l => l.kind === 'streetlight');
  const sound = lights().filter(l => !l.broken).length;
  assert.ok(sound >= 2, `${sound} unbroken streetlights`);
  const litNow = lights().filter(l => l.lit).length;
  assert.ok(litNow <= 1, `at the claim at most the first light is on (${litNow})`);
  st.t += 1;   // one second on: three more lights in sequence
  const lit1 = lights().filter(l => l.lit).length;
  assert.ok(lit1 >= Math.min(sound, 1 + LIGHT_SEQ_PER_S) - 1 && lit1 <= Math.min(sound, 1 + LIGHT_SEQ_PER_S) + 1, `about ${1 + LIGHT_SEQ_PER_S} lights a second in (${lit1})`);
  // the ones that are on are the ones nearest the substation
  const sub = ground(st).blocks[nb].sub!, cx = sub.x + sub.size / 2, cy = sub.y + sub.size / 2;
  const on = lights().filter(l => l.lit).map(l => Math.hypot(l.tx + 0.5 - cx, l.ty + 0.5 - cy)), off = lights().filter(l => !l.lit && !l.broken).map(l => Math.hypot(l.tx + 0.5 - cx, l.ty + 0.5 - cy));
  if (on.length && off.length) assert.ok(Math.max(...on) <= Math.min(...off) + 1e-9, 'the sequence runs outward from the substation');
  st.t += lights().length / LIGHT_SEQ_PER_S + 1;   // a broken light keeps its slot in the sequence: the last one is on by N / 3 s
  assert.equal(lights().filter(l => l.lit).length, sound, 'all sound streetlights on within N / 3 s');
  const p = contestProgress(st, nb);
  assert.ok(p > 0 && p < 1, `progress mid-way (${p.toFixed(2)})`);
  st.t = b.contestUntil - 0.5;
  assert.ok(contestProgress(st, nb) < 1);
  runS(st, 2);
  assert.equal(b.state, HELD, 'Held when the burn-off ends');
  assert.equal(contestProgress(st, nb), 1);
});

test('repair: E on a broken streetlight costs one copper and lights it; an eaten Lamp leaves the broken list; no copper refuses', () => {
  const st = city();
  runS(st, 1);
  const hq = hqIndex(st), G = ground(st);
  const broken = blockLights(st, hq).find(l => l.kind === 'streetlight' && l.why === 'broken');
  assert.ok(broken, 'the HQ has a §13-broken streetlight');
  const hit = lightAt(st, broken!.tx, broken!.ty);
  assert.ok(hit && hit.bi === hq && hit.l.why === 'broken');
  const cu = st.engineer.inv.copper!;
  assert.equal(canRepair(st, broken!.tx, broken!.ty).ok, true);
  assert.equal(repairLight(st, broken!.tx, broken!.ty).ok, true);
  assert.equal(st.engineer.inv.copper, cu - REPAIR_COPPER);
  assert.equal(st.flow!.repairs, 1);
  const after = lightAt(st, broken!.tx, broken!.ty)!.l;
  assert.equal(after.why, ''); assert.equal(after.lit, true, 'lit as soon as it is repaired (the HQ powers it)');
  assert.equal(canRepair(st, broken!.tx, broken!.ty).ok, false, 'a repaired light is not broken');
  assert.equal(lightMask(st)[broken!.ty * G.tw + broken!.tx], 1);
  // a lamp a crawler ate
  const [lx, ly] = hqLot(st, 6, 6);
  const lamp = place(st, 'lamp', lx - 2, ly - 2, 0)!;   // (lx + 3, ly + 3) is the Depot's tile
  runS(st, 1);
  const T = threatOf(st.flow!);
  T.broken.push(lamp.y * G.tw + lamp.x);
  assert.equal(lightAt(st, lamp.x, lamp.y)!.l.why, 'eaten');
  assert.equal(lightAt(st, lamp.x, lamp.y)!.l.lit, false);
  assert.equal(repairLight(st, lamp.x, lamp.y).ok, true);
  assert.equal(T.broken.length, 0);
  assert.equal(lightAt(st, lamp.x, lamp.y)!.l.lit, true);
  // no copper
  const other = blockLights(st, hq).find(l => l.kind === 'streetlight' && l.why === 'broken');
  if (other) {
    st.engineer.inv.copper = 0;
    const c = canRepair(st, other.tx, other.ty);
    assert.equal(c.ok, false); assert.match(c.reason, /copper/);
  }
  assert.equal(canRepair(st, lx + 1, ly + 1).ok, false, 'no light on a bare lot tile');
});
