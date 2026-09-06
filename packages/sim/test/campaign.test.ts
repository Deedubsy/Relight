import { test } from 'node:test';
import assert from 'node:assert/strict';
import {
  createCampaign, createState, citySpec, DEFAULT_CONFIG, protoCalibrated, ensureFlow, CAMPAIGN_RULESET, LEGACY_RULESET,
  rulesetOf, campaignClock, ground, passable, walkable, findPath, makeSave, loadState, stateHash, advanceFlow, step,
  applyCommands, claim, deliverTo, activationCheck, enableHeart, enableStalkers, canPlace, inReach, chestTake, currentGoal,
  conservation, P_STEEL, P_COPPER, CONTESTED, type SimState,
} from '../src/index';
import { evidenceHash, evidenceProfileProblem, configOf, type ConfigRef } from '../../harness/src/provenance';
import { parseUrl, createSession, replaySession, saveSlot, hasSlot, loadSnapshot, slotUrl, queue, frame } from '../../game/src/session';

test('legacy defaults retain their identity; campaign schema rejects mixed rules, metadata, candidates and evidence', () => {
  const cfg = protoCalibrated(DEFAULT_CONFIG), legacy = createState(citySpec(3, 'river', cfg), cfg, 3);
  assert.equal(legacy.ruleset, undefined); assert.equal(rulesetOf(legacy), LEGACY_RULESET); assert.equal(makeSave(legacy).version, 1);
  const st = createCampaign(), save = makeSave(st);
  assert.equal(save.version, 3); assert.equal(rulesetOf(loadState(save)), CAMPAIGN_RULESET);
  for (const mutate of [
    (s: SimState) => { s.version = 2; },
    (s: SimState) => { delete s.ruleset; },
    (s: SimState) => { s.campaign!.opening = 'unknown' as 'culdesac-v1'; },
    (s: SimState) => { s.blocks[s.campaign!.homeBlock].state = CONTESTED; },
    (s: SimState) => { s.city!.profile = 'unknown' as 'riverside-v1'; },
  ]) { const bad = structuredClone(st); mutate(bad); assert.throws(() => loadState(bad)); }
  assert.equal(enableHeart(st), null); assert.equal(enableStalkers(st), null);
  const ref: ConfigRef = { kind: 'campaign', ruleset: CAMPAIGN_RULESET, opening: 'culdesac-v1' };
  assert.notEqual(evidenceHash(ref), evidenceHash({ kind: 'snapshot' }));
  assert.equal(evidenceProfileProblem(ref, 'docs/experiments/campaign/opening.json', save), '');
  assert.match(evidenceProfileProblem(ref, 'docs/experiments/opening.json', save), /path/);
  assert.match(evidenceProfileProblem(ref, 'docs/experiments/campaign/opening.json', makeSave(legacy)), /saved state/);
  assert.throws(() => configOf({ kind: 'snapshot', ruleset: CAMPAIGN_RULESET } as unknown as ConfigRef), /legacy evidence/);
});

test('campaign has no legacy ammo ring, wake blooms, automatic attacks, claims or passive territory loss; clock and load continuation agree', () => {
  const st = createCampaign(), f = ensureFlow(st), home = st.blocks[st.campaign!.homeBlock];
  const neighbour = st.blocks[st.nb[st.campaign!.homeBlock].find(i => st.blocks[i].state === 0)!];
  const stock = { ...st.stock }, pockets = { ...st.engineer.inv };
  assert.equal(claim(st, neighbour.x, neighbour.y), false);
  assert.equal(deliverTo(st, neighbour.x, neighbour.y, 'steel', 20).moved, 0);
  assert.equal(activationCheck(st, neighbour.x, neighbour.y).ok, false);
  assert.deepEqual(st.stock, stock); assert.deepEqual(st.engineer.inv, pockets);
  const remaining = conservation(st).unexplained;
  for (let i = 0; i < 1800; i++) step(st);
  assert.deepEqual(st.ring, []); assert.deepEqual(st.engagements, []);
  assert.equal(st.events.filter(e => e.type === 'bloom' || e.type === 'fall').length, 0);
  assert.equal(home.state, 2); assert.equal(st.stats.ringDraw, 0); assert.deepEqual(f.projects, {});
  assert.deepEqual(conservation(st).unexplained, remaining);
  assert.deepEqual(campaignClock({ t: 899 }), { day: 1, night: false, elapsed: 899 });
  assert.equal(campaignClock({ t: 900 }).night, true); assert.equal(campaignClock({ t: 2400 }).day, 3);
  const live = createCampaign(), resumed = loadState(makeSave(live)); resumed.speed = live.speed;
  advanceFlow(live, 60, []); advanceFlow(resumed, 60, []);
  assert.equal(stateHash(live), stateHash(resumed));
});

test('cul-de-sac across seeds has exactly one four-tile entrance, shared collision, reachable supplies/resources and buildable factory space', () => {
  for (const seed of [3, 4, 5, 8, 13]) {
    const st = createCampaign(seed), G = ground(st), opening = G.opening!, { x, y, size } = opening.bounds;
    assert.equal(opening.gate.length, 4);
    for (const t of opening.walls) {
      assert.equal(walkable(G, t % G.tw, Math.floor(t / G.tw)), false);
      assert.match(canPlace(st, 'belt', t % G.tw, Math.floor(t / G.tw)).reason, /structure/);
    }
    const sx = Math.floor(st.engineer.x), sy = Math.floor(st.engineer.y), gateSet = new Set(opening.gate);
    let exits = 0;
    for (let yy = y; yy < y + size; yy++) for (let xx = x; xx < x + size; xx++) {
      if (xx !== x && xx !== x + size - 1 && yy !== y && yy !== y + size - 1) continue;
      if (passable(st, xx, yy)) { assert.ok(gateSet.has(yy * G.tw + xx)); exits++; }
    }
    assert.equal(exits, 4, `single mouth on seed ${seed}`);
    const exit = opening.gate[1] + [-G.tw, 1, G.tw, -1][opening.direction] * 2;
    const path = findPath(st, sx, sy, exit % G.tw, Math.floor(exit / G.tw));
    assert.ok(path?.length, `walk out on seed ${seed}`); assert.ok([...path!].some(t => gateSet.has(t)));
    assert.ok(findPath(st, exit % G.tw, Math.floor(exit / G.tw), sx, sy)?.length, 'return home');
    for (const resource of [P_STEEL, P_COPPER]) {
      const tile = G.patch.findIndex(v => v === resource);
      assert.ok(tile >= 0 && findPath(st, sx, sy, tile % G.tw, Math.floor(tile / G.tw))?.length, 'starter patch is reachable');
    }
    assert.equal(chestTake(st, 'steel', 50).moved, 50); assert.equal(chestTake(st, 'copper', 50).moved, 50);
    let place: [number, number] | undefined;
    for (let yy = y + 1; yy < y + size - 3 && !place; yy++) for (let xx = x + 1; xx < x + size - 3; xx++) {
      if (inReach(st, xx, yy, 3) && canPlace(st, 'assembler', xx, yy).ok) { place = [xx, yy]; break; }
    }
    assert.ok(place, 'clear factory pad near the house');
    const before = conservation(st).unexplained;
    applyCommands(st, [{ type: 'place', item: 'assembler', x: place[0], y: place[1] }]);
    assert.ok(st.flow!.machines.some(m => m.kind === 'assembler')); assert.ok(st.engineer.inv.steel < 50);
    assert.deepEqual(conservation(st).unexplained, before);
    assert.equal(currentGoal(st).goal.id, 'home-explore');
    assert.deepEqual(ground(loadState(makeSave(st))).opening, opening, 'load regenerates the identical boundary');
  }
});

test('campaign session, share/load identity, replay and browser save slots remain isolated from legacy sessions', async () => {
  const store = new Map<string, string>();
  const oldLocation = Object.getOwnPropertyDescriptor(globalThis, 'location'), oldStorage = Object.getOwnPropertyDescriptor(globalThis, 'localStorage');
  Object.defineProperty(globalThis, 'location', { configurable: true, value: { href: 'http://localhost/' } });
  Object.defineProperty(globalThis, 'localStorage', { configurable: true, value: { getItem: (k: string) => store.get(k) ?? null, setItem: (k: string, v: string) => store.set(k, v) } });
  try {
    assert.throws(() => parseUrl('?rules=unknown'), /Unknown/);
    const session = createSession(parseUrl('?rules=exploration-v2'));
    const legacy = createSession(parseUrl('?city=legacy'));
    saveSlot(legacy); const original = store.get('relight.save.1'); saveSlot(session);
    assert.equal(store.get('relight.save.1'), original); assert.equal(hasSlot('1', CAMPAIGN_RULESET), true);
    assert.match(slotUrl(session), /exploration-v2/);
    const loaded = await loadSnapshot('local:exploration-v2:1');
    assert.throws(() => createSession(parseUrl('?rules=legacy-v1'), loaded), /do not match/);
    const restored = createSession(parseUrl(''), loaded); assert.equal(restored.params.ruleset, CAMPAIGN_RULESET);
    assert.throws(() => createSession(parseUrl('?rules=exploration-v2&autoplay=hour')), /Legacy bots/);
    queue(session, { type: 'chestTake', item: 'steel', n: 20 });
    queue(session, { type: 'chestTake', item: 'copper', n: 20 });
    frame(session, 15);
    assert.equal(session.state.engineer.inv.steel, 20); assert.equal(session.log.length, 2);
    const result = replaySession(session, { rifleOff: false });
    assert.ok('state' in result); assert.equal(stateHash(result.state), stateHash(session.state));
  } finally {
    if (oldLocation) Object.defineProperty(globalThis, 'location', oldLocation); else Reflect.deleteProperty(globalThis, 'location');
    if (oldStorage) Object.defineProperty(globalThis, 'localStorage', oldStorage); else Reflect.deleteProperty(globalThis, 'localStorage');
  }
});
