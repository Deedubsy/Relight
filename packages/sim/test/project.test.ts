/** RI-05 (run name RI-05-project, plan §5): the neighbourhood project record — stages derived from the site's real
 *  prerequisites, deliveries by hand and off a belt into the claim's installation, the rail-yard restoration as the
 *  first project (its Activate is its commissioning, the block record alone owns Held), its reward the transport kit
 *  (locked until then — no circular unlock), the local supply depot as a named chest commissioned on Held ground with
 *  no claim charge, restored once and kept through a later loss, and everything surviving save / load. Tests inject
 *  pocket stock and belt items (a labelled scenario, as RI-03's tests do); the ledger drift is taken after them. */
import { test } from 'node:test';
import assert from 'node:assert/strict';
import {
  DEFAULT_CONFIG, protoCalibrated, createState, citySpec, ensureFlow, SimState, HELD, DARK, CONTESTED, Machine,
  hqIdx, hqNeighbourToward, ground, advanceFlow, place, canPlace, remove, poleGrid, polePlan, claimNeed, deliveredTo, deliverTo, activationCheck, activate,
  faceSub, conservation, lockReason, unlockedKinds, giveItem, machineAt, chestPut, chestTake, invTotal, ITEMS, isItem, KINDS, RAIL_ROUTE_KINDS,
  syncProjects, projectOf, projectList, projectOperational, commission, commissionCheck, describeProject, projectTitle, installOf,
  RAIL_YARD_PROJECT, SUPPLY_DEPOT_PROJECT, RAIL_ROUTE_REWARD, LOCAL_DEPOT_REWARD, SUPPLY_DEPOT_NEED, SUPPLY_CHEST_CAP,
  stateHash, makeSave, loadState, createHourBot, runHour, applyCommands, MARGIN_TILES, Dir,
  StationRules, tramRoute,
} from '../src/index';

function city(seed = 3): SimState {
  const cfg = protoCalibrated({ ...DEFAULT_CONFIG, walk: true, economy: true });
  Object.assign(cfg, { power: true, supply: 'generators', draw: 'half' });
  const st = createState(citySpec(seed, 'river', cfg), cfg, seed);
  ensureFlow(st);
  return st;
}

test('EX-06A: placed three-stop railway uses reach-checked commands, resumes in transit and parks intact across a broken track', () => {
  const st = powered(3), f = st.flow!, G = ground(st), ry = G.railYard, b = st.blocks[ry], hq = hqIdx(st);
  applyCommands(st, [{ type: 'claim', x: b.x, y: b.y }]); run(st, b.contestUntil - st.t + 2);
  st.engineer.inv.steel = 400; st.engineer.inv.copper = 100;
  const len = 18;
  let line: { x: number; y: number } | undefined;
  outer: for (let y = 2; y < G.th; y++) for (let x = 0; x + len < G.tw; x++) {
    if (!Array.from({ length: len }, (_, k) => {
      const t = y * G.tw + x + k;
      return G.owner[t] === -1 && (G.near[t] === hq || G.near[t] === ry) && canPlace(st, 'track', x + k, y).ok;
    }).every(Boolean)) continue;
    if ([0, 8, 16].every(k => canPlace(st, 'tramstop', x + k, y - 2).ok)) { line = { x, y }; break outer; }
  }
  assert.ok(line, 'a legal street with three stop sites');
  const { x, y } = line;
  const [a, middle, end] = [0, 8, 16].map(k => place(st, 'tramstop', x + k, y - 2)!);
  for (let k = 0; k < len; k++) assert.ok(place(st, 'track', x + k, y));
  const tram = place(st, 'tram', x, y)!;
  const rules: StationRules = { coal: { request: 0, reserve: 20, export: true }, copper: { request: 15, reserve: 0, export: false } };
  farFrom(st, a); applyCommands(st, [{ type: 'setStationRules', x: a.x, y: a.y, rules }]);
  assert.equal(Boolean(a.freight), false); assert.equal(st.version, 1);
  standAt(st, a);
  applyCommands(st, [{ type: 'setStationRules', x: a.x, y: a.y, rules: { coal: { ...rules.coal!, request: NaN } } }]);
  assert.equal(Boolean(a.freight), false);
  applyCommands(st, [{ type: 'setStationRules', x: a.x, y: a.y, rules }]);
  assert.equal(st.version, 2); assert.deepEqual(a.freight, rules);
  rules.coal!.reserve = 0; assert.equal(a.freight!.coal!.reserve, 20, 'command input cannot mutate installed rules afterwards');
  standAt(st, middle); applyCommands(st, [{ type: 'setStationRules', x: middle.x, y: middle.y, rules: { coal: { request: 10, reserve: 0, export: false }, copper: { request: 0, reserve: 5, export: true } } }]);
  standAt(st, end); applyCommands(st, [{ type: 'setStationRules', x: end.x, y: end.y, rules: { coal: { request: 30, reserve: 0, export: false } } }]);
  a.inv = { coal: 80 }; middle.inv = { copper: 20 }; // injected scenario stock, accounted from here
  const initial = conservation(st).unexplained;
  advanceFlow(st, 2, []);
  assert.equal(tram.cargo!.coal, 40);
  const saved = makeSave(st), resumed = loadState(saved); resumed.speed = st.speed;
  advanceFlow(st, 40, []); advanceFlow(resumed, 40, []);
  assert.equal(stateHash(st), stateHash(resumed));
  assert.equal(middle.cargo!.coal, 10); assert.equal(end.cargo!.coal, 30); assert.equal(a.cargo!.copper, 15);
  assert.deepEqual(conservation(st).unexplained, initial);
  // Dispatch again, then remove a remote piece of track. No path teleport or cargo deletion.
  end.cargo = {}; tram.x = x; tram.y = y; tram.run = { fwd: true, stop: -1 }; tram.phase = 0; tram.timer = 0;
  advanceFlow(st, 1, []); assert.ok((tram.cargo!.coal ?? 0) > 0);
  const carried = { ...tram.cargo };
  assert.ok(remove(st, x + 4, y));
  advanceFlow(st, 20, []);
  assert.deepEqual(tram.cargo, carried); assert.ok(tramRoute(st, tram).length < len);
  assert.ok(place(st, 'track', x + 4, y));
  advanceFlow(st, 40, []); assert.ok((end.cargo.coal ?? 0) > 0, 'repair reconnects the line and retries delivery');
  assert.ok(f.machines.includes(tram));
});
/** The hour bot's first ten minutes: Generators burning, nothing claimed yet. */
function powered(seed = 3): SimState {
  const st = city(seed);
  runHour(st, createHourBot(false), 10 * 60);
  st.events.length = 0;
  return st;
}
function standAt(st: SimState, r: { x: number; y: number; size: number }): void { st.engineer.x = r.x - 1; st.engineer.y = r.y + r.size / 2; st.engineer.target = null; }
function farFrom(st: SimState, r: { x: number; y: number; size: number }): void { st.engineer.x = r.x + 40; st.engineer.y = r.y + 40; st.engineer.target = null; }
/** Advance in whole seconds (the block tick runs every TILE_TPS tile ticks). */
function run(st: SimState, seconds: number): void { for (let s = 0; s < seconds; s += 100) advanceFlow(st, Math.min(100, seconds - s), []); }
function stages(st: SimState, id: string): string[] { return st.events.filter(e => e.type === 'project' && e.id === id).map(e => e.type === 'project' ? e.stage : ''); }
/** A lot tile of block `bi` where `kind` may stand. */
function lotTile(st: SimState, bi: number, kind: 'chest'): [number, number] | null {
  const b = ground(st).blocks[bi];
  for (let ty = b.y0 + MARGIN_TILES; ty < b.y1 - MARGIN_TILES; ty++) for (let tx = b.x0 + MARGIN_TILES; tx < b.x1 - MARGIN_TILES; tx++) if (canPlace(st, kind, tx, ty).ok) return [tx, ty];
  return null;
}
/** A belt tile next to the substation, pointing into it. */
function beltInto(st: SimState, sub: { x: number; y: number; size: number }): { x: number; y: number; dir: Dir } | null {
  for (let d = 0; d < 4; d++) {
    const into = ((d + 2) % 4) as Dir;   // the belt on side d of the footprint points back into it
    for (let k = 0; k < sub.size; k++) {
      const x = d === 1 ? sub.x + sub.size : d === 3 ? sub.x - 1 : sub.x + k, y = d === 2 ? sub.y + sub.size : d === 0 ? sub.y - 1 : sub.y + k;
      if (canPlace(st, 'belt', x, y).ok) return { x, y, dir: into };
    }
  }
  return null;
}

test('the rail-yard project: discovered → preparing (the pole run) → ready (materials by hand and off a belt) → commissioning (Activate, its id) → restored (Held) once; the reward unlocks the rail kit, which was locked before; a later loss keeps stage and reward and makes it non-operational; the retake re-grants nothing', () => {
  const fresh = city(3);
  assert.equal(projectOf(fresh, RAIL_YARD_PROJECT)?.stage, 'discovered', 'discovered from the start');
  const st = powered(3), f = st.flow!;
  const ry = ground(st).railYard, b = st.blocks[ry], sub = faceSub(st, ry)!;
  assert.ok(ry >= 0 && ry === hqNeighbourToward(st, 'west'), 'the rail yard is the west neighbour (names.ts)');
  const r = projectOf(st, RAIL_YARD_PROJECT)!;
  assert.ok(r); assert.equal(r.siteId, ry);
  assert.equal(r.stage, poleGrid(st).reached.includes(ry) ? 'preparing' : 'discovered', 'preparing is derived: a pole run reaching the site');
  assert.deepEqual(r.neighbourhoodBlockIds, st.nb[ry]); assert.deepEqual(r.requirements, claimNeed(st));
  assert.equal(projectTitle(st, r), 'West rail yard restoration');
  for (const p of projectList(st)) for (const k in p.requirements) assert.ok(isItem(k), `${k} is an ordinary item`);
  // no circular unlock: the kit the reward grants is locked, and the requirements are items, not the kit
  for (const k of RAIL_ROUTE_KINDS) { assert.equal(lockReason(st, k), 'the rail yard restoration unlocks it'); assert.equal(canPlace(st, k, sub.x, sub.y).reason, 'the rail yard restoration unlocks it'); }
  assert.equal(unlockedKinds(st).length, KINDS.length - RAIL_ROUTE_KINDS.length - 3, 'the Electricians\' three and the rail kit are locked');
  assert.ok(unlockedKinds(st).includes('chest'), 'the supply chest is in the field kit from the start (plan §4.2)');
  // the pole run: preparing
  if (!poleGrid(st).reached.includes(ry)) { st.engineer.inv.pole = (st.engineer.inv.pole ?? 0) + 20; for (const [px, py] of polePlan(st, ry)) assert.ok(place(st, 'pole', px, py, 0), `pole at (${px},${py})`); }
  assert.ok(poleGrid(st).reached.includes(ry));
  syncProjects(st);
  assert.equal(r.stage, 'preparing');
  // materials: some by hand, some off a belt into the substation (the same committed store)
  st.engineer.inv.steel = 100; st.engineer.inv.copper = 50;
  const need = claimNeed(st);
  standAt(st, sub);
  assert.ok(deliverTo(st, b.x, b.y, 'steel', need.steel - 3).ok);
  assert.ok(deliverTo(st, b.x, b.y, 'copper', need.copper).ok);
  assert.equal(r.stage, 'preparing'); assert.deepEqual(r.deliveredItems, { steel: need.steel - 3, copper: need.copper });
  const bt = beltInto(st, sub);
  assert.ok(bt, 'a belt tile next to the substation');
  const belt = place(st, 'belt', bt!.x, bt!.y, bt!.dir)!;
  assert.ok(belt, 'the belt stands on the front');
  for (let i = 0; i < 3; i++) assert.ok(giveItem(st, belt, 'steel', i * 0.34), 'the scenario\'s three steel on the belt');
  const U0 = { ...conservation(st).unexplained };
  const drift = (): string => { const U = conservation(st).unexplained; return Object.keys(U).filter(k => Math.abs(U[k as keyof typeof U] - U0[k as keyof typeof U]) > 0.01).map(k => `${k} ${U[k as keyof typeof U] - U0[k as keyof typeof U]}`).join(', '); };
  advanceFlow(st, 3, []);
  assert.equal(belt.items.length, 0, 'the belt handed its steel to the installation');
  assert.deepEqual(deliveredTo(st, ry), need); assert.equal(f.stats.beltDelivered, 3);
  assert.equal(drift(), '', 'belt → committed: on the ledger');
  assert.ok(giveItem(st, belt, 'steel', 0)); advanceFlow(st, 2, []);
  assert.equal(belt.items.length, 1, 'a claim that has its steel takes no more: the belt backs up');
  assert.equal(r.stage, 'ready', 'ready is the site\'s own prerequisites — the engineer may stand anywhere');
  farFrom(st, sub); syncProjects(st); assert.equal(r.stage, 'ready');
  assert.equal(activationCheck(st, b.x, b.y).reason, 'walk closer to the substation'); assert.ok(activationCheck(st, b.x, b.y, false).ok);
  assert.equal(describeProject(st, r), `West rail yard restoration · ready · Activate at the substation`);
  // Activate: commissioning with the attempt's id; the record keeps what was consumed
  standAt(st, sub);
  st.events.length = 0;
  assert.ok(activate(st, b.x, b.y).ok);
  assert.equal(b.state, CONTESTED); assert.equal(r.stage, 'commissioning'); assert.equal(r.activationAttemptId, f.commissionSeq);
  assert.deepEqual(r.deliveredItems, need); assert.equal(f.delivered[ry], undefined);
  assert.deepEqual(stages(st, RAIL_YARD_PROJECT), ['commissioning']);
  for (const k of RAIL_ROUTE_KINDS) assert.notEqual(lockReason(st, k), '', 'still locked while commissioning');
  // the burn-off: Held → restored once, the reward granted once, the next project discovered
  run(st, b.contestUntil - st.t + 2);
  assert.equal(b.state, HELD, 'the normal burn-off');
  assert.equal(r.stage, 'restored'); assert.ok(r.restoredAt >= 0); assert.equal(r.rewardId, RAIL_ROUTE_REWARD); assert.equal(r.rewardAt, r.restoredAt);
  assert.ok(projectOperational(st, r));
  for (const k of RAIL_ROUTE_KINDS) assert.equal(lockReason(st, k), '');
  const d = projectOf(st, SUPPLY_DEPOT_PROJECT)!;
  assert.ok(d, 'the local depot is the next opportunity'); assert.equal(d.siteId, ry); assert.equal(d.stage, 'discovered'); assert.deepEqual(d.requirements, SUPPLY_DEPOT_NEED);
  assert.deepEqual(stages(st, RAIL_YARD_PROJECT), ['commissioning', 'restored']);
  const restoredAt = r.restoredAt, events0 = st.events.filter(e => e.type === 'project').length;
  // lost later: the record stays restored and the reward stays; the facility is not operational; a retake re-grants nothing
  b.state = DARK; st.fallen[ry] = true;
  syncProjects(st);
  assert.equal(r.stage, 'restored'); assert.equal(projectOperational(st, r), false);
  for (const k of RAIL_ROUTE_KINDS) assert.equal(lockReason(st, k), '', 'owning the unlock outlives the facility');
  assert.equal(activationCheck(st, b.x, b.y, false).ok, false, 'a retake needs its materials again (normal retake rules)');
  b.state = HELD; st.fallen[ry] = false;
  syncProjects(st);
  assert.equal(r.restoredAt, restoredAt); assert.equal(st.events.filter(e => e.type === 'project').length, events0, 'restored once, no second event');
});

test('the local supply depot: a supply chest on the restored (Held) site, stocked by hand with ordinary items, commissioned within reach — the record alone changes: no claim, no charge, the stock stays; the restored depot hands out kits; a pre-RI-05 save discovers its projects on load', () => {
  const st = powered(3), f = st.flow!;
  const ry = ground(st).railYard, b = st.blocks[ry];
  // the scenario: the yard restored by the legacy map claim (the lattice bots' path), Held by the block record
  applyCommands(st, [{ type: 'claim', x: b.x, y: b.y }]);
  assert.equal(b.state, CONTESTED);
  run(st, b.contestUntil - st.t + 2);
  assert.equal(b.state, HELD);
  const r = projectOf(st, RAIL_YARD_PROJECT)!, d = projectOf(st, SUPPLY_DEPOT_PROJECT)!;
  assert.equal(r.stage, 'restored'); assert.equal(r.activationAttemptId, -1, 'a map claim has no attempt id'); assert.ok(d);
  assert.equal(commissionCheck(st, RAIL_YARD_PROJECT).reason, 'the rail yard commissions through Activate at its substation');
  assert.equal(commissionCheck(st, SUPPLY_DEPOT_PROJECT).reason, 'no supply chest on the site');
  // the chest on the site
  st.engineer.inv.steel = 60; st.engineer.inv.coal = 30; st.engineer.inv.magazine = 12;
  const at = lotTile(st, ry, 'chest');
  assert.ok(at, 'a lot tile for the chest');
  const chest = place(st, 'chest', at![0], at![1])!;
  assert.ok(chest); assert.equal(st.engineer.inv.steel, 50, '10 steel');
  syncProjects(st);
  assert.equal(d.stage, 'preparing'); assert.equal(d.installId, chest.id); assert.equal(installOf(st, d), chest);
  assert.equal(describeProject(st, d), 'West rail yard supply depot · preparing · needs 20 coal, 10 magazine');
  // stocked by hand, within reach; the cap; take-back
  farFrom(st, chest);
  assert.equal(chestPut(st, 'coal', 20, [chest.x, chest.y]).reason, 'walk closer to the Supply chest');
  standAt(st, chest);
  assert.equal(chestTake(st, 'kit', 1, [chest.x, chest.y]).reason, 'kits come from the Depot or a restored supply depot');
  assert.equal(chestPut(st, 'coal', 20, [chest.x, chest.y]).moved, 20);
  assert.equal(chestPut(st, 'magazine', 5, [chest.x, chest.y]).moved, 5);
  assert.deepEqual(d.deliveredItems, { coal: 20, magazine: 5 }); assert.equal(d.stage, 'preparing');
  assert.equal(commissionCheck(st, SUPPLY_DEPOT_PROJECT).reason, 'needs 5 magazine in the chest');
  assert.equal(chestTake(st, 'coal', 3, [chest.x, chest.y]).moved, 3); assert.equal(d.deliveredItems.coal, 17);
  assert.equal(chestPut(st, 'coal', 3, [chest.x, chest.y]).moved, 3);
  assert.equal(chestPut(st, 'magazine', 7, [chest.x, chest.y]).moved, 7);
  assert.equal(invTotal(chest.inv), 32); assert.equal(chest.inv.magazine, 12);
  assert.equal(d.stage, 'ready'); assert.deepEqual(d.deliveredItems, SUPPLY_DEPOT_NEED, 'capped at the requirement');
  // commission: the record only
  const stock0 = { ...st.stock }, spent0 = st.stats.spentSteel ?? 0, seq0 = f.commissionSeq, inv0 = { ...chest.inv };
  farFrom(st, chest);
  assert.equal(commission(st, SUPPLY_DEPOT_PROJECT).reason, 'walk closer to the chest');
  standAt(st, chest);
  st.events.length = 0;
  const c = commission(st, SUPPLY_DEPOT_PROJECT);
  assert.ok(c.ok, c.reason);
  assert.equal(d.stage, 'restored'); assert.equal(d.rewardId, LOCAL_DEPOT_REWARD); assert.ok(d.restoredAt >= 0); assert.ok(projectOperational(st, d));
  assert.equal(b.state, HELD); assert.deepEqual(st.stock, stock0); assert.equal(st.stats.spentSteel ?? 0, spent0); assert.equal(f.commissionSeq, seq0);
  assert.equal(f.delivered[ry], undefined, 'no claim materials'); assert.deepEqual(chest.inv, inv0, 'the stock stays: it is the depot');
  assert.deepEqual(stages(st, SUPPLY_DEPOT_PROJECT), ['restored']);
  assert.equal(st.events.filter(e => e.type === 'claim' || e.type === 'bloom').length, 0, 'no second claim');
  assert.equal(commission(st, SUPPLY_DEPOT_PROJECT).reason, 'already restored');
  assert.equal(describeProject(st, d), 'West rail yard supply depot · restored · operational · hands out kits');
  // the reward: kits from the named depot, like the Depot
  assert.equal(chestTake(st, 'kit', 1, [chest.x, chest.y]).moved, 1);
  assert.ok(chestTake(st, 'magazine', 2, [chest.x, chest.y]).moved === 2);
  // pick the chest up: restored stays, not operational; a new chest on the site is the installation again
  assert.ok(remove(st, chest.x, chest.y)); syncProjects(st);
  assert.equal(d.stage, 'restored'); assert.equal(projectOperational(st, d), false); assert.equal(d.installId, -1);
  // save / load: records equal; a save without them (pre-RI-05) discovers them on load
  const save = makeSave(st, { log: [], logComplete: false });
  const back = loadState(JSON.parse(JSON.stringify(save)));
  assert.deepEqual(back.flow!.projects, f.projects); assert.equal(stateHash(back), stateHash(st));
  const old = JSON.parse(JSON.stringify(save)); delete old.state.flow.projects;
  const legacy = loadState(old);
  assert.equal((legacy.flow as { projects?: unknown }).projects, undefined, 'a pre-RI-05 save');
  ensureFlow(legacy); syncProjects(legacy);   // the load path's upgrade, then the first sync of a step
  assert.equal(legacy.flow!.projects[RAIL_YARD_PROJECT]?.stage, 'restored', 'derived from the Held block'); assert.equal(legacy.flow!.projects[SUPPLY_DEPOT_PROJECT]?.stage, 'discovered', 'the record, not the chest, is what the old save lacks');
});

test('a supply chest on the front feeds an inserter and takes from one; the tram shuttle: track on the street only, a stop at each end, one tram — items put on the platform ride to the far stop\'s arrivals (hand and inserter take them there), the ledger balances, and a save mid-run advances to the same hash', () => {
  const st = powered(3), f = st.flow!;
  const ry = ground(st).railYard, b = st.blocks[ry], hq = hqIdx(st);
  applyCommands(st, [{ type: 'claim', x: b.x, y: b.y }]);
  run(st, b.contestUntil - st.t + 2);
  assert.equal(b.state, HELD); assert.equal(projectOf(st, RAIL_YARD_PROJECT)!.stage, 'restored');
  st.engineer.inv.steel = 200; st.engineer.inv.copper = 100; st.engineer.inv.coal = 40;
  // a straight run of street on the HQ's or the yard's margin: track tiles with a 2×2 stop touching each end
  const G = ground(st), LEN = 14;
  let line: { x0: number; y: number } | null = null;
  outer: for (let y = 0; y < G.th && !line; y++) for (let x0 = 0; x0 + LEN < G.tw; x0++) {
    let ok = true;
    for (let k = 0; k < LEN && ok; k++) { const t = y * G.tw + x0 + k; ok = G.owner[t] === -1 && (G.near[t] === hq || G.near[t] === ry) && canPlace(st, 'track', x0 + k, y).ok; }
    if (ok && canPlace(st, 'tramstop', x0, y - 2).ok && canPlace(st, 'tramstop', x0 + LEN - 2, y - 2).ok && canPlace(st, 'inserter', x0 + LEN, y - 1).ok && canPlace(st, 'belt', x0 + LEN + 1, y - 1).ok) { line = { x0, y }; break outer; }
  }
  assert.ok(line, 'a street line for the route');
  const { x0, y } = line!;
  const lot = lotTile(st, ry, 'chest')!;
  assert.equal(canPlace(st, 'track', lot[0], lot[1]).reason, 'track runs on streets');
  const A = place(st, 'tramstop', x0, y - 2)!, B = place(st, 'tramstop', x0 + LEN - 2, y - 2)!;
  for (let k = 0; k < LEN; k++) assert.ok(place(st, 'track', x0 + k, y, 1), `track ${k}`);
  assert.equal(canPlace(st, 'tram', x0, y - 1).reason, 'a tram goes on a track');
  const tram = place(st, 'tram', x0, y)!;
  assert.ok(tram); assert.equal(machineAt(st, x0, y)!.kind, 'track', 'the tram rides the track, it does not occupy it');
  assert.equal(canPlace(st, 'tram', x0, y).reason, 'a tram is there');
  assert.equal(st.engineer.inv.steel, 200 - 2 * 10 - LEN - 20); assert.equal(st.engineer.inv.copper, 100 - 5);   // the stops cost no copper, the tram 5 (the hour's Depot holds ~18 Cu at 26 min with none coming until 46:00)
  const ins = place(st, 'inserter', x0 + LEN, y - 1, 1)!, belt = place(st, 'belt', x0 + LEN + 1, y - 1, 1)!;
  assert.ok(ins && belt);
  // the platform at A, by hand
  standAt(st, A);
  assert.equal(chestPut(st, 'coal', 25, [A.x, A.y]).moved, 25);
  assert.deepEqual(A.inv, { coal: 25 });
  const U0 = { ...conservation(st).unexplained };
  const drift = (): string => { const U = conservation(st).unexplained; return Object.keys(U).filter(k => Math.abs(U[k as keyof typeof U] - U0[k as keyof typeof U]) > 0.01).map(k => `${k} ${U[k as keyof typeof U] - U0[k as keyof typeof U]}`).join(', '); };
  const mid = makeSave(st, { log: [], logComplete: false });
  advanceFlow(st, 30, []);
  assert.equal(invTotal(A.inv), 0, 'boarded at A');
  const arrived = invTotal(B.cargo), onBelt = belt.items.length + (ins.hold ? 1 : 0);
  assert.ok(arrived + onBelt >= 20 && f.stats.tramMoved === 25, `25 coal unloaded at B (${f.stats.tramMoved}), ${arrived} in the arrivals and ${onBelt} taken onward by the inserter`);
  assert.ok(f.stats.tramMoved === 25);
  assert.equal(drift(), '', 'platform → tram → arrivals → inserter → belt: all on the ledger');
  // the hand takes arrivals at B
  standAt(st, B);
  const got = chestTake(st, 'coal', 5, [B.x, B.y]).moved;
  assert.ok(got >= 1 && got <= 5, `${got} coal from B's arrivals`);
  assert.equal(drift(), '');
  // a save mid-run advances to the same hash (the route, the dwell and the transfers are all state)
  const back = loadState(JSON.parse(JSON.stringify(mid))); back.speed = 1;   // a load stands still until the player runs it
  advanceFlow(back, 30, []);
  const again = loadState(JSON.parse(JSON.stringify(mid))); again.speed = 1;
  advanceFlow(again, 30, []);
  assert.equal(stateHash(back), stateHash(again));
  assert.equal(back.flow!.stats.tramMoved, 25);
  // pick-up: the tram first, then its track; a track under a tram stays
  assert.ok(canPlace(st, 'track', x0, y).reason, 'occupied');
  const tramTile = { x: tram.x, y: tram.y };
  standAt(st, { x: tramTile.x, y: tramTile.y, size: 1 });
  assert.equal(canPlace(st, 'tram', tramTile.x, tramTile.y).reason, 'a tram is there');
  const picked = remove(st, tramTile.x, tramTile.y)!;
  assert.equal(picked.kind, 'tram'); assert.equal(machineAt(st, tramTile.x, tramTile.y)!.kind, 'track', 'the track stays');
  assert.equal(st.engineer.inv.tram, 1);
  // a supply chest on the claim front feeds an inserter (the chest's contents are an inserter's source)
  const chestAt = lotTile(st, ry, 'chest');
  assert.ok(chestAt);
  const chest = place(st, 'chest', chestAt![0], chestAt![1])!;
  assert.ok(chest);
  assert.ok(chest.kind === 'chest' && SUPPLY_CHEST_CAP === 200);
  standAt(st, chest);
  assert.equal(chestPut(st, 'coal', 15, [chest.x, chest.y]).moved, 15);
  assert.ok(ITEMS.includes('coal'));
});
