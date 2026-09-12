/** RI-06 (run name RI-06-cand-heart, plan §9, GDD §28.8): the Junction Heart — the rail yard's claim as the first boss.
 *  The candidate layer (`enableHeart`, candidates.ts, outside SimConfig — D-RI-5) puts a rooted body on the yard's
 *  switching installation and two feeder cabinets on the yard's lot toward two Dark approaches. Tests: the layer's
 *  geometry and its idempotence; the cabinets as inventory targets (delivery by `deliver` with `cabinet`, the tile
 *  refused to placements); the encounter as the hour bot plays it rifle off (the marks and the once-only facts); a
 *  scripted stall — a feeder knocked out (a labelled scenario: `knockOutCabinet` stands in for the crawler's arrival),
 *  the 60 s to interruption, the deliveries and the charge preserved, the repair, the retry with no second charge and
 *  distinct packet keys; save / load mid-attempt; the explicit abort. Nothing here is the benchmark: every state
 *  carries the candidate layer, and E-hour keeps its configuration. */
import { test } from 'node:test';
import assert from 'node:assert/strict';
import {
  DEFAULT_CONFIG, protoCalibrated, createState, citySpec, ensureFlow, SimState, HELD, DARK, CONTESTED,
  ground, advanceFlow, placeable, activationCheck, activate, deliverTo, faceSub, remove, machineAt,
  enableHeart, heartOf, heartAt, cabinetAt, cabinetSupplied, cabinetConnected, heartCheck, describeHeart, describeCabinet, knockOutCabinet,
  repairCabinet, cabinetRepairCheck, abortHeart, heartBodies, REPAIR_COPPER,
  projectOf, RAIL_YARD_PROJECT, lockReason, conservation, stateHash, makeSave, loadState, createHourBot, runHour, HOUR_CLAIM_AT, HourBot, threatOf,
} from '../src/index';

function city(seed = 3): SimState {
  const cfg = protoCalibrated({ ...DEFAULT_CONFIG, walk: true, economy: true });
  Object.assign(cfg, { power: true, supply: 'generators', draw: 'half' });
  const st = createState(citySpec(seed, 'river', cfg), cfg, seed);
  ensureFlow(st);
  return st;
}
/** The Heart's city and its bot (the hour bot's physical claim path with the Heart step, rifle off). */
function heartRun(seed = 3): { st: SimState; bot: HourBot } {
  const st = city(seed);
  assert.ok(enableHeart(st), 'the yard has two Dark approaches');
  return { st, bot: createHourBot(false, 'chest', HOUR_CLAIM_AT.north, 'physical', 'belt', true) };
}
/** Drive the bot a second at a time until `pred` or `limitS`. */
function runUntil(st: SimState, bot: HourBot, pred: () => boolean, limitS: number): void {
  while (st.t < limitS && !pred()) runHour(st, bot, 1);
}
const yard = (st: SimState) => ground(st).railYard;

test('RI-06: enableHeart puts the Heart on the rail yard with two feeder cabinets on the lot toward two Dark approaches, once', () => {
  for (const seed of [3, 4, 5]) {
    const st = city(seed), H = enableHeart(st);
    assert.ok(H, `seed ${seed}: the layer`);
    assert.equal(H.site, yard(st));
    assert.equal(H.cabinets.length, 2);
    const lot = ground(st).blocks[H.site];
    for (const [k, c] of H.cabinets.entries()) {
      assert.ok(st.nb[H.site].includes(c.approach) && st.blocks[c.approach].state === DARK, `seed ${seed}: cabinet ${k + 1} faces a Dark neighbour`);
      assert.ok(c.x >= lot.x0 && c.x <= lot.x1 && c.y >= lot.y0 && c.y <= lot.y1, `seed ${seed}: cabinet ${k + 1} on the yard's lot`);
      assert.equal(cabinetAt(st, c.x, c.y), k);
      assert.equal(placeable(st, 'pole', c.x, c.y), 'the feeder cabinet is there');
      assert.equal(cabinetSupplied(H, k), false);
      assert.equal(cabinetConnected(st, k), false);
    }
    assert.notEqual(H.cabinets[0].approach, H.cabinets[1].approach);
    assert.equal(enableHeart(st), H, 'idempotent');
    assert.equal(heartAt(st, H.site), H);
    assert.equal(heartAt(st, st.nb[H.site][0]), undefined);
    assert.equal(heartCheck(st).ok, false);
    assert.match(heartCheck(st).reason, /feeder cabinet 1 needs/);
    assert.ok(describeHeart(st).length > 0 && describeCabinet(st, 0).length > 0);
    assert.equal(abortHeart(st).ok, false, 'nothing to abort before the Start');
  }
});

test('RI-06: the cabinets take deliveries from the pockets and the installation refuses the Activate until both are supplied and powered', () => {
  const st = city(3), H = enableHeart(st)!, c = H.cabinets[0], b = st.blocks[H.site];
  // a labelled scenario: stock in the pockets and the engineer beside cabinet 1 (the bot's walk is the next test's)
  st.engineer.inv.steel = (st.engineer.inv.steel ?? 0) + 20; st.engineer.inv.copper = (st.engineer.inv.copper ?? 0) + 20;
  const far = deliverTo(st, b.x, b.y, 'steel', 1, 0);
  assert.equal(far.ok, false, 'out of reach');
  st.engineer.x = c.x + 1.5; st.engineer.y = c.y + 0.5;
  const r1 = deliverTo(st, b.x, b.y, 'steel', H.cand.cabinet.steel + 3, 0);
  assert.ok(r1.ok); assert.equal(r1.moved, H.cand.cabinet.steel, 'no more than the cabinet needs');
  const r2 = deliverTo(st, b.x, b.y, 'copper', H.cand.cabinet.copper, 0);
  assert.ok(r2.ok); assert.equal(cabinetSupplied(H, 0), true);
  assert.equal(deliverTo(st, b.x, b.y, 'steel', 1, 0).ok, false, 'full');
  assert.equal(cabinetConnected(st, 0), false, 'supplied is not powered');
  const chk = activationCheck(st, b.x, b.y);
  assert.equal(chk.ok, false);
  assert.equal(projectOf(st, RAIL_YARD_PROJECT)?.stage, 'preparing');
});

test('RI-06 (plan §9.4): the hour bot, rifle off, prepares the yard, starts the Heart and destroys it once; the reward is granted once', () => {
  const { st, bot } = heartRun(3);
  runUntil(st, bot, () => (bot.log.marks['held-west'] !== undefined && bot.log.marks['rail-yard-restored'] !== undefined) || bot.log.refused.length > 0, HOUR_CLAIM_AT.west + 20 * 60);
  const H = heartOf(st)!, m = bot.log.marks, T = threatOf(st.flow!);
  assert.deepEqual(bot.log.refused.map(x => `${x.what}: ${x.reason}`), [], 'no refusal on the Heart step');
  for (const name of ['claim-west', 'cabinet-1-supplied', 'cabinet-2-supplied', 'turret-1', 'turret-2', 'heart-start', 'heart-destroyed', 'held-west', 'rail-yard-restored'])
    assert.ok(m[name] !== undefined, `mark ${name}`);
  assert.ok(m['heart-start'] < m['heart-destroyed'] && m['heart-destroyed'] <= m['held-west'], 'the order');
  assert.ok(H.destroyed && st.blocks[H.site].state === HELD);
  assert.equal(H.attempt, -1);
  assert.ok(H.stats.attempts >= 1);
  assert.ok(H.stats.productiveS >= H.cand.productiveS - 1, `productive ${H.stats.productiveS}`);
  assert.equal(bot.rifle, false); assert.equal(T.stats.rifleKills, 0); assert.equal(st.engineer.firstShot, -1, 'no shot fired');
  // the packets: at most one per threshold per attempt, each on a threshold, the marks on the first attempt's crossings
  const keys = Object.keys(H.requested);
  assert.ok(keys.length >= H.cand.thresholds.length, `packets ${keys.join(' ')}`);
  assert.equal(new Set(keys).size, keys.length);
  for (const k of keys) assert.ok(H.cand.thresholds.includes(Number(k.split(':')[1])), k);
  const perAttempt: Record<string, number> = {};
  for (const k of keys) perAttempt[k.split(':')[0]] = (perAttempt[k.split(':')[0]] ?? 0) + 1;
  for (const a in perAttempt) assert.ok(perAttempt[a] <= H.cand.thresholds.length, `attempt ${a}: ${perAttempt[a]} packets`);
  assert.ok(H.stats.born > 0 && (m['heart-first-body'] ?? Infinity) >= (m['packet-25'] ?? 0) + H.cand.approachS, 'the approach shown before the first body');
  assert.equal(H.pending.length, 0, 'nothing pending after the destruction');
  assert.equal(heartBodies(st), 0, 'no body left on the site');
  // charged once, the reward once, the kit unlocked at the restoration
  assert.ok(H.charged && H.charge.steel > 0);
  for (const c of H.cabinets) assert.deepEqual(c.delivered, { steel: 0, copper: 0 }, 'the cabinets\' materials spent at the destruction');
  const ry = projectOf(st, RAIL_YARD_PROJECT)!;
  assert.equal(ry.stage, 'restored'); assert.equal(ry.rewardAt, ry.restoredAt); assert.equal(lockReason(st, 'track'), '');
  assert.match(describeHeart(st), /destroyed.*operational/);
  const L = conservation(st);
  assert.ok(L.ok, L.problems.join('; '));
});

test('RI-06 (§9.2 defaults 9–11): a knocked-out feeder stalls the commissioning, 60 s stalls it to interrupted; deliveries and the charge stay, the retry charges nothing twice and keys its packets anew', () => {
  const { st, bot } = heartRun(3);
  runUntil(st, bot, () => bot.log.marks['heart-start'] !== undefined || bot.log.refused.length > 0, HOUR_CLAIM_AT.west + 20 * 60);
  assert.deepEqual(bot.log.refused, []);
  const H = heartOf(st)!, b = st.blocks[H.site], spentSteel = st.stats.spentSteel, spentCopper = st.stats.spentCopper;
  assert.equal(b.state, CONTESTED); assert.equal(H.attempt >= 0, true); assert.ok(H.charged);
  assert.equal(b.contestUntil, Number.MAX_SAFE_INTEGER, 'no burn-off timer on the Heart\'s block');
  assert.equal(projectOf(st, RAIL_YARD_PROJECT)?.stage, 'commissioning');
  // ten productive seconds without the bot (its patrol would repair the feeder at once — the scenario wants the stall)
  advanceFlow(st, 10);
  assert.ok(H.progress >= 9.5, `progress ${H.progress}`);
  const attempt1 = H.attempt, delivered = H.cabinets.map(c => ({ ...c.delivered }));
  knockOutCabinet(st, 0);   // the scenario's stand-in for a crawler's arrival at the cabinet
  assert.equal(H.cabinets[0].down, true); assert.equal(cabinetConnected(st, 0), false);
  const p0 = H.progress;
  advanceFlow(st, 20);
  assert.equal(H.progress, p0, 'productive progress pauses while a feeder is down');
  assert.ok(H.stall >= 19.5 && b.state === CONTESTED);
  assert.match(describeHeart(st), /PAUSED/);
  advanceFlow(st, H.cand.stallS - 20 + 2);
  assert.equal(b.state, DARK, 'interrupted');
  assert.equal(H.attempt, -1); assert.equal(H.stats.interrupted, 1); assert.equal(H.progress, 0);
  assert.equal(projectOf(st, RAIL_YARD_PROJECT)?.stage, 'interrupted');
  assert.deepEqual(H.cabinets.map(c => c.delivered), delivered, 'the cabinets\' deliveries kept');
  assert.ok(H.charged, 'the installation keeps its materials');
  assert.equal(st.stats.spentSteel, spentSteel); assert.equal(st.stats.spentCopper, spentCopper);
  assert.equal(activationCheck(st, b.x, b.y).ok, false, 'the knocked-out feeder blocks the retry');
  assert.match(activationCheck(st, b.x, b.y).reason, /knocked out/);
  assert.equal(deliverTo(st, b.x, b.y, 'steel', 1).ok, false, 'no second delivery to a charged installation');
  // the repair (REPAIR_COPPER from the pockets, within reach — the engineer stands where the bot left it, so walk it there: a labelled scenario)
  const c0 = H.cabinets[0];
  st.engineer.x = c0.x + 1.5; st.engineer.y = c0.y + 0.5;
  const cu = st.engineer.inv.copper ?? 0;
  assert.ok(cu >= REPAIR_COPPER, 'the bot kept repair copper when collecting the kits');
  assert.ok(cabinetRepairCheck(st, 0).ok, cabinetRepairCheck(st, 0).reason);
  assert.ok(repairCabinet(st, 0).ok);
  assert.equal(st.engineer.inv.copper ?? 0, cu - REPAIR_COPPER);
  assert.equal(H.cabinets[0].down, false); assert.equal(H.stats.repairs, 1);
  // the retry at the substation: no second charge, a new attempt id, its packets keyed anew
  const sub = faceSub(st, H.site)!;
  st.engineer.x = sub.x + sub.size + 0.5; st.engineer.y = sub.y + 0.5;
  const chk = activationCheck(st, b.x, b.y);
  assert.ok(chk.ok, chk.reason);
  assert.ok(activate(st, b.x, b.y).ok);
  assert.equal(b.state, CONTESTED); assert.equal(H.stats.attempts, 2); assert.notEqual(H.attempt, attempt1);
  assert.equal(st.stats.spentSteel, spentSteel, 'charged once'); assert.equal(st.stats.spentCopper, spentCopper);
  assert.equal(H.progress, 0, 'productive commissioning resets for the retry');
  advanceFlow(st, H.cand.productiveS * H.cand.thresholds[0] / 100 + 2);
  const keys = Object.keys(H.requested);
  assert.ok(keys.includes(`${H.attempt}:${H.cand.thresholds[0]}`), `the retry's first packet: ${keys.join(' ')}`);
  assert.ok(keys.every(k => k.split(':')[0] === `${attempt1}` || k.split(':')[0] === `${H.attempt}`));
  assert.equal(new Set(keys).size, keys.length);
});

test('RI-06: a save mid-attempt reloads to the same hash and runs on to the same hash; the explicit abort works only while commissioning', () => {
  const { st, bot } = heartRun(4);
  runUntil(st, bot, () => bot.log.marks['heart-start'] !== undefined || bot.log.refused.length > 0, HOUR_CLAIM_AT.west + 20 * 60);
  assert.deepEqual(bot.log.refused, []);
  advanceFlow(st, 30);
  const H = heartOf(st)!;
  assert.ok(H.attempt >= 0 && H.progress > 0);
  const copy = loadState(JSON.parse(JSON.stringify(makeSave(st))));
  assert.equal(stateHash(copy), stateHash(st));
  assert.deepEqual(heartOf(copy), H);
  assert.equal(copy.speed, 0, 'loading intentionally pauses the game');
  copy.speed = st.speed;
  advanceFlow(st, 45); advanceFlow(copy, 45);
  assert.equal(stateHash(copy), stateHash(st), 'the same hash 45 s on');
  assert.deepEqual(heartOf(copy)!.requested, H.requested);
  // the abort: once, and only while an attempt runs
  if (H.attempt >= 0) {
    const spent = st.stats.spentSteel;
    assert.ok(abortHeart(st).ok);
    assert.equal(st.blocks[H.site].state, DARK); assert.equal(H.stats.aborted, 1); assert.equal(H.pending.length, 0);
    assert.ok(H.charged); assert.equal(st.stats.spentSteel, spent);
    assert.equal(abortHeart(st).ok, false, 'nothing to abort twice');
  }
});

test('RI-06: a pole run reaching a cabinet is what "connected" means — removing it pauses the commissioning (no knock-out)', () => {
  const { st, bot } = heartRun(5);
  runUntil(st, bot, () => bot.log.marks['heart-start'] !== undefined || bot.log.refused.length > 0, HOUR_CLAIM_AT.west + 20 * 60);
  assert.deepEqual(bot.log.refused, []);
  const H = heartOf(st)!;
  advanceFlow(st, 5);
  assert.ok(cabinetConnected(st, 0) && cabinetConnected(st, 1));
  // the nearest pole to cabinet 2 (a labelled scenario: the engineer's removal of its own pole, in reach or not)
  const c = H.cabinets[1], poles = st.flow!.machines.filter(m => m.kind === 'pole').sort((p, q) => Math.hypot(p.x - c.x, p.y - c.y) - Math.hypot(q.x - c.x, q.y - c.y));
  assert.ok(poles.length > 0);
  st.engineer.x = poles[0].x + 1.5; st.engineer.y = poles[0].y + 0.5;
  assert.ok(remove(st, poles[0].x, poles[0].y), 'the pole picked up');
  assert.equal(machineAt(st, poles[0].x, poles[0].y), undefined);
  const p0 = H.progress, connected = cabinetConnected(st, 0) && cabinetConnected(st, 1);
  advanceFlow(st, 5);
  if (!connected) { assert.equal(H.progress, p0, 'paused'); assert.equal(H.cabinets[1].down, false, 'an unpowered feeder is not a knocked-out one'); }
  else assert.ok(H.progress > p0, 'another pole still reaches it');
});
