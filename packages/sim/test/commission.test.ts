/** RI-03 (run name RI-03-commission, plan §4): one claim path. The map previews and charges nothing; a pole run
 *  claims nothing; the claim is the materials delivered to the block's substation and an explicit Activate within
 *  reach, paid once, one commissioning event per attempt; the field kit stands on Dark ground next to Held, draws real
 *  power from the connected grid and never marks the block Held; the outskirts Substation stands before activation;
 *  a logged commissioning replays to the same hash. */
import { test } from 'node:test';
import assert from 'node:assert/strict';
import {
  DEFAULT_CONFIG, protoCalibrated, createState, citySpec, ensureFlow, SimState, LoggedCommand, HELD, DARK, CONTESTED,
  hqIdx, hqNeighbourToward, ground, idxOf, isCandidate, advanceFlow, place, canPlace, remove, poleGrid, polePlan, layPoles, claimNeed, deliveredTo,
  deliverTo, activationCheck, activate, claimInfo, faceSub, machineStatus, conservation, tileHooks, survivorJoined,
  LAMP_KW, POLE_REACH, MACHINE_COST, CELL_TILES, MARGIN_TILES, stateHash, makeSave, loadState, replay, createHourBot, runHour, Machine, Kind,
} from '../src/index';

function city(seed = 3): SimState {
  const cfg = protoCalibrated({ ...DEFAULT_CONFIG, walk: true, economy: true });   // the game's configuration: the claim costs
  Object.assign(cfg, { power: true, supply: 'generators', draw: 'half' });
  const st = createState(citySpec(seed, 'river', cfg), cfg, seed);
  ensureFlow(st);
  return st;
}
/** The hour bot's first ten minutes: Generators burning, the lines laid, nothing claimed yet (east is 15:00). */
function powered(seed = 3): SimState {
  const st = city(seed);
  runHour(st, createHourBot(false), 10 * 60);
  st.events.length = 0;
  return st;
}
/** A test's teleport within reach of a substation (the hour bot walks there through `move`). */
function standAt(st: SimState, sub: { x: number; y: number; size: number }): void {
  st.engineer.x = sub.x - 1; st.engineer.y = sub.y + sub.size / 2; st.engineer.target = null;
}
function farFrom(st: SimState, sub: { x: number; y: number; size: number }): void {
  st.engineer.x = sub.x + 40; st.engineer.y = sub.y + 40; st.engineer.target = null;
}
/** A lot tile of block `bi` where `kind` may stand (the tile rules), or the last reason seen. */
function lotTile(st: SimState, bi: number, kind: Kind, near?: [number, number], within = POLE_REACH - 1): { tile: [number, number] | null; reason: string } {
  const b = ground(st).blocks[bi];
  let reason = '';
  for (let ty = b.y0 + MARGIN_TILES; ty < b.y1 - MARGIN_TILES; ty++) for (let tx = b.x0 + MARGIN_TILES; tx < b.x1 - MARGIN_TILES; tx++) {
    if (near && Math.hypot(tx + 0.5 - (near[0] + 0.5), ty + 0.5 - (near[1] + 0.5)) > within) continue;
    const c = canPlace(st, kind, tx, ty);
    if (c.ok) return { tile: [tx, ty], reason: '' };
    reason = c.reason;
  }
  return { tile: null, reason };
}
function stringTo(st: SimState, bi: number): [number, number][] {
  const plan = polePlan(st, bi);
  for (const [px, py] of plan) assert.ok(place(st, 'pole', px, py, 0), `pole at (${px},${py}): ${canPlace(st, 'pole', px, py).reason}`);
  return plan;
}

test('one path: the map previews and charges nothing; a pole run claims nothing; the materials are delivered once and consumed once at Activate; one claim event and one wake bloom carry the attempt\'s id; a second Activate is refused; the ledger balances', () => {
  const st = powered(3), f = st.flow!;
  const east = hqNeighbourToward(st, 'east'), b = st.blocks[east], sub = faceSub(st, east)!;
  assert.equal(b.state, DARK); assert.ok(sub, 'east has a substation');
  st.engineer.inv.steel = 200; st.engineer.inv.copper = 100;   // the test's stock: an injection the ledger reports (as designed) — the claim must add nothing to it
  const U0 = { ...conservation(st).unexplained };
  const drift = (): string => { const U = conservation(st).unexplained; return Object.keys(U).filter(k => Math.abs(U[k as keyof typeof U] - U0[k as keyof typeof U]) > 0.01).map(k => `${k} ${U[k as keyof typeof U] - U0[k as keyof typeof U]}`).join(', '); };
  const stock0 = { ...st.stock }, spentSteel0 = st.stats.spentSteel ?? 0, spentCu0 = st.stats.spentCopper ?? 0, machines0 = f.machines.length;
  // the map: a preview, nothing else
  const info = claimInfo(st, b.x, b.y);
  assert.equal(typeof info.ok, 'boolean');
  assert.equal(b.state, DARK); assert.deepEqual(st.stock, stock0); assert.equal(f.machines.length, machines0);
  assert.equal(st.events.length, 0, 'the map click raised no event');
  // the pole run: connected, reaching, claiming nothing
  assert.equal(activationCheck(st, b.x, b.y).reason, 'no pole run reaches its substation');
  const plan = stringTo(st, east);
  assert.ok(plan.length >= 1, `${plan.length} poles planned`);
  assert.ok(poleGrid(st).reached.includes(east), 'the run reaches east\'s substation');
  assert.equal(polePlan(st, east).length, 0, 'strung'); assert.equal(layPoles(st, b.x, b.y), 0, 'nothing left to lay');
  advanceFlow(st, 2, []);
  assert.equal(b.state, DARK, 'power connection alone activates nothing');
  assert.equal(f.pending.length, 0, 'no claim was raised');
  // the materials: delivered to the substation within reach, up to the need, committed there
  const need = claimNeed(st);
  assert.deepEqual(need, st.config.eco.claimCost);
  assert.equal(activationCheck(st, b.x, b.y).reason, `needs ${need.steel} more steel, ${need.copper} more Cu delivered`);
  farFrom(st, sub);
  assert.equal(deliverTo(st, b.x, b.y, 'steel', need.steel).reason, 'walk closer to the substation');
  standAt(st, sub);
  assert.equal(deliverTo(st, b.x, b.y, 'coal', 5).reason, 'a claim takes steel and copper');
  const steelBefore = st.engineer.inv.steel ?? 0;
  const d1 = deliverTo(st, b.x, b.y, 'steel', need.steel + 7);
  assert.ok(d1.ok); assert.equal(d1.moved, need.steel, 'up to the need, never more');
  assert.equal(st.engineer.inv.steel, steelBefore - need.steel);
  assert.equal(deliverTo(st, b.x, b.y, 'steel', 1).reason, `it has its ${need.steel} steel`);
  assert.equal(activationCheck(st, b.x, b.y).reason, `needs ${need.copper} more Cu delivered`);
  assert.ok(deliverTo(st, b.x, b.y, 'copper', need.copper).ok);
  assert.deepEqual(deliveredTo(st, east), need);
  assert.equal(drift(), '', 'committed materials are on the ledger (neither lost nor doubled)');
  assert.equal(conservation(st).held.steel >= need.steel, true);
  assert.deepEqual(st.stock, stock0, 'the Depot chest is untouched by the delivery');
  // the Activate: once
  const chk = activationCheck(st, b.x, b.y);
  assert.ok(chk.ok, chk.reason); assert.deepEqual(chk.have, need);
  st.events.length = 0;
  const a1 = activate(st, b.x, b.y);
  assert.ok(a1.ok);
  assert.equal(b.state, CONTESTED, 'Activate starts Contested');
  assert.deepEqual(st.stock, stock0, 'the Depot chest is not charged (never both the Depot and the installation)');
  assert.equal(st.stats.spentSteel, spentSteel0 + need.steel); assert.equal(st.stats.spentCopper, spentCu0 + need.copper);
  assert.deepEqual(deliveredTo(st, east), { steel: 0, copper: 0 }); assert.equal(f.delivered[east], undefined, 'consumed, no longer committed');
  const claims = st.events.filter(e => e.type === 'claim'), blooms = st.events.filter(e => e.type === 'bloom');
  assert.equal(claims.length, 1); assert.equal(claims[0].type === 'claim' && claims[0].via, 'activate'); assert.equal(claims[0].type === 'claim' && claims[0].id, 1);
  assert.equal(blooms.length, 1, 'one commissioning response — the wake bloom, once');
  assert.equal(blooms[0].type === 'bloom' && blooms[0].wake, true); assert.equal(blooms[0].type === 'bloom' && blooms[0].id, 1);
  assert.equal(f.commissionSeq, 1);
  assert.equal(drift(), '', 'spent at Activate: committed → sink, nothing unexplained');
  // again: refused, nothing fires twice, nothing charged twice
  const a2 = activate(st, b.x, b.y);
  assert.equal(a2.ok, false); assert.equal(a2.reason, 'already commissioning');
  const rej = st.events.filter(e => e.type === 'activate-rejected');
  assert.equal(rej.length, 1); assert.equal(rej[0].type === 'activate-rejected' && rej[0].id, 2); assert.equal(f.commissionSeq, 2);
  assert.equal(st.events.filter(e => e.type === 'claim').length, 1); assert.equal(st.events.filter(e => e.type === 'bloom').length, 1);
  assert.equal(st.stats.spentSteel, spentSteel0 + need.steel);
  assert.equal(deliverTo(st, b.x, b.y, 'steel', 1).reason, 'already commissioning');
  // the same commands through the sim's dispatch (a replayed log) are as idempotent
  st.events.length = 0;
  advanceFlow(st, 1, [{ type: 'deliver', bx: b.x, by: b.y, item: 'steel', n: 5 }, { type: 'activate', bx: b.x, by: b.y }]);
  assert.equal(st.events.filter(e => e.type === 'claim').length, 0);
  assert.equal(st.events.filter(e => e.type === 'activate-rejected').length, 1);
  assert.equal(f.commissionSeq, 3);
  assert.equal(st.stats.spentSteel, spentSteel0 + need.steel, 'still paid once');
});

test('prerequisites: the installation names the missing one, in order — adjacency, the substation, the pole run, a substation that is off, the grid\'s supply, the materials, reach, the engineer down', () => {
  // a fresh city has no Generator burning: the run reaches, the grid has nothing
  const cold = city(3);
  cold.engineer.inv.steel = 200; cold.engineer.inv.copper = 100;
  const eastC = hqNeighbourToward(cold, 'east');
  stringTo(cold, eastC);
  for (const g of cold.flow!.machines.filter(m => m.kind === 'generator')) assert.ok(remove(cold, g.x + 1, g.y + 1), 'the HQ\'s start Generator (D-P4-8) picked up');
  advanceFlow(cold, 1, []);
  assert.equal(activationCheck(cold, cold.blocks[eastC].x, cold.blocks[eastC].y).reason, 'the grid has no supply — no Generator burning');
  const st = powered(3), f = st.flow!;
  st.engineer.inv.steel = 200; st.engineer.inv.copper = 100;
  const hq = hqIdx(st), east = hqNeighbourToward(st, 'east'), b = st.blocks[east], sub = faceSub(st, east)!;
  const far = st.blocks.findIndex((x, i) => x.state === DARK && !isCandidate(st, i) && faceSub(st, i) !== null);
  assert.ok(far >= 0);
  assert.equal(activationCheck(st, st.blocks[far].x, st.blocks[far].y).reason, 'no Held block adjacent');
  assert.equal(activationCheck(st, st.blocks[hq].x, st.blocks[hq].y).reason, 'already Held');
  assert.equal(activationCheck(st, -99, -99).reason, 'out of bounds');
  assert.equal(activationCheck(st, b.x, b.y).reason, 'no pole run reaches its substation');
  stringTo(st, east);
  // the grid is cached per (flow rev, second): a direct flip of the block's switch here (in play it moves inside a step) bumps it
  st.blocks[hq].subOn = false; f.rev++;
  assert.equal(activationCheck(st, b.x, b.y).reason, 'its pole run hangs from a substation that is off');
  st.blocks[hq].subOn = true; f.rev++;
  const need = claimNeed(st);
  assert.equal(activationCheck(st, b.x, b.y).reason, `needs ${need.steel} more steel, ${need.copper} more Cu delivered`);
  standAt(st, sub);
  assert.ok(deliverTo(st, b.x, b.y, 'steel', need.steel).ok); assert.ok(deliverTo(st, b.x, b.y, 'copper', need.copper).ok);
  farFrom(st, sub);
  assert.equal(activationCheck(st, b.x, b.y).reason, 'walk closer to the substation');
  standAt(st, sub);
  st.engineer.down = 0;
  assert.equal(activationCheck(st, b.x, b.y).reason, 'the engineer is down');
  st.engineer.down = -1;
  assert.ok(activationCheck(st, b.x, b.y).ok);
  // empty pockets: a delivery moves nothing
  st.engineer.inv.copper = 0;
  const other = st.blocks.findIndex((x, i) => i !== east && x.state === DARK && isCandidate(st, i) && faceSub(st, i) !== null);
  assert.ok(other >= 0);
  standAt(st, faceSub(st, other)!);
  assert.equal(deliverTo(st, st.blocks[other].x, st.blocks[other].y, 'copper', 5).reason, 'no copper in the pockets');
});

test('the outskirts: the Substation is field kit — refused until a neighbour is Held, then stands on the unheld outskirts lot (paid from the pockets, D-B3-1), the block stays Dark, and it is the installation the claim delivers to', () => {
  const st = powered(3), G = ground(st);
  st.engineer.inv.steel = 500; st.engineer.inv.copper = 250;
  const sv = st.survivors.find(v => v.name === 'Electricians');
  assert.ok(sv, 'the Electricians are on the map');
  const out = st.blocks.findIndex((b, i) => b.state === DARK && G.blocks[i].sub === null && st.nb[i].some(n => st.blocks[n].state === DARK) && !st.nb[i].includes(idxOf(st, sv!.x, sv!.y)));
  assert.ok(out >= 0, 'an outskirts block with no substation');
  const ob = st.blocks[out];
  assert.equal(faceSub(st, out), null);
  assert.equal(activationCheck(st, ob.x, ob.y).reason, 'no Held block adjacent');
  // the Electricians unlock it (their block Held); the outskirts block is still not next to Held ground
  st.blocks[idxOf(st, sv!.x, sv!.y)].state = HELD;
  assert.ok(survivorJoined(st, 'Electricians'));
  assert.equal(lotTile(st, out, 'substation').reason, 'not next to Held ground');
  const nbr = st.nb[out].find(n => st.blocks[n].state === DARK)!;
  st.blocks[nbr].state = HELD; st.blocks[nbr].subOn = true;
  assert.equal(activationCheck(st, ob.x, ob.y).reason, 'no substation — the outskirts need a Substation first');
  const spot = lotTile(st, out, 'substation');
  assert.ok(spot.tile, `a 3×3 lot on the outskirts block: ${spot.reason}`);
  const steel0 = st.engineer.inv.steel ?? 0, cu0 = st.engineer.inv.copper ?? 0;
  const sub = place(st, 'substation', spot.tile![0], spot.tile![1], 0);
  assert.ok(sub, 'the Substation stands before activation');
  assert.equal(st.engineer.inv.steel, steel0 - MACHINE_COST.substation.steel); assert.equal(st.engineer.inv.copper, cu0 - MACHINE_COST.substation.copper);
  assert.equal(ob.state, DARK, 'standing it is not the claim by itself — the block stays Dark');
  assert.deepEqual(faceSub(st, out), { x: sub!.x, y: sub!.y, size: sub!.size }, 'it is the installation');
  assert.equal(activationCheck(st, ob.x, ob.y).reason, 'no pole run reaches its substation');
  advanceFlow(st, 2, []);
  assert.equal(ob.state, DARK);
  assert.equal(lotTile(st, out, 'substation').reason, 'the face has a substation', 'one a face');
});

test('field kit: a Lamp on the Dark front is off until a connected pole reaches it, then lit on real demand; a turret there is not off; an Assembler is refused; a belt on a far Dark block is refused; nothing marks the block Held', () => {
  const st = powered(3), f = st.flow!;
  st.engineer.inv.steel = 500; st.engineer.inv.copper = 250;
  const east = hqNeighbourToward(st, 'east'), b = st.blocks[east];
  const demand = () => tileHooks.current!.demandKw(st, true);
  const lampSpot = lotTile(st, east, 'lamp');
  assert.ok(lampSpot.tile, lampSpot.reason);
  const lamp = place(st, 'lamp', lampSpot.tile![0], lampSpot.tile![1], 0)!;
  assert.ok(lamp);
  assert.deepEqual(machineStatus(st, lamp), { state: 'off', reason: 'no connected pole in reach' });
  assert.equal(demand(), demand(), 'demand is a pure read');
  // the run from the HQ: a lamp within reach of a connected pole is on the grid and draws; the first lamp stays off
  // unless the run happened to pass it
  const plan = stringTo(st, east);
  assert.ok(plan.length >= 1);
  const anchor = plan[plan.length - 1], reachPole = f.machines.find(m => m.kind === 'pole' && m.x === anchor[0] && m.y === anchor[1])!;
  assert.ok(reachPole && poleGrid(st).on.has(reachPole.id), 'the run\'s last pole hangs from a substation that is on');
  const litSpot = lotTile(st, east, 'lamp', anchor, POLE_REACH - 1);
  assert.ok(litSpot.tile, `a lot tile within reach of the run: ${litSpot.reason}`);
  const d1 = demand();
  const lit = place(st, 'lamp', litSpot.tile![0], litSpot.tile![1], 0)!;
  assert.ok(lit);
  assert.equal(machineStatus(st, lit).state === 'off', false, `the lamp is on the grid (${machineStatus(st, lit).reason})`);
  assert.equal(demand(), d1 + LAMP_KW, 'real demand on the grid');
  const near = f.machines.some(m => m.kind === 'pole' && poleGrid(st).on.has(m.id) && Math.hypot(m.x - lamp.x, m.y - lamp.y) <= POLE_REACH - 1);
  if (!near) assert.deepEqual(machineStatus(st, lamp), { state: 'off', reason: 'no connected pole in reach' }, 'the first lamp, out of the run\'s reach, is still off');
  assert.equal(b.state, DARK, 'a field device never marks the block Held');
  // a turret on the front runs (idle or starved, never off for its block); production is refused; a far Dark block takes nothing
  const tSpot = lotTile(st, east, 'turret', anchor, 6).tile ? lotTile(st, east, 'turret', anchor, 6) : lotTile(st, east, 'turret');
  assert.ok(tSpot.tile, tSpot.reason);
  const turret = place(st, 'turret', tSpot.tile![0], tSpot.tile![1], 0)!;
  assert.ok(turret);
  assert.notEqual(machineStatus(st, turret).state, 'off', machineStatus(st, turret).reason);
  assert.equal(lotTile(st, east, 'assembler').reason, 'the block is not Held');
  const farDark = st.blocks.findIndex((x, i) => x.state === DARK && !isCandidate(st, i) && ground(st).blocks[i].tiles.length > 0);
  assert.ok(farDark >= 0);
  assert.equal(lotTile(st, farDark, 'belt').reason, 'not next to Held ground');
  assert.equal(lotTile(st, farDark, 'pole').reason, 'not next to Held ground');
  advanceFlow(st, 5, []);
  assert.equal(b.state, DARK, 'five seconds of field power: still Dark');
  assert.equal(st.blocks[farDark].state, DARK);
});

test('save / replay: the hour bot\'s physical claim (poles, deliver, Activate) logged at 14:00 replays from a save to the unbroken run\'s hash at 16:00, with east commissioned by the physical path', () => {
  const a = city(3), bot = createHourBot(false), log: LoggedCommand[] = [];
  assert.equal(bot.claimPath, 'physical');
  runHour(a, bot, 14 * 60, log);
  const save = makeSave(a, { log, logComplete: true });
  const b = loadState(JSON.parse(JSON.stringify(save)));
  assert.equal(stateHash(b), save.hash);
  const from = log.length, vias: string[] = [];
  const east = hqNeighbourToward(a, 'east');
  runHour(a, bot, 2 * 60, log);
  const tail = JSON.stringify(log.slice(from));
  assert.ok(tail.includes('"activate"') && tail.includes('"deliver"') && tail.includes('"pole"'), 'the log carries the pole run, the delivery and the Activate');
  assert.ok(!tail.includes('"claim"'), 'no map claim in the log');
  assert.notEqual(a.blocks[east].state, DARK, 'east commissioned by 16:00');
  replay(b, log.slice(from), a.flow!.tick);
  assert.equal(b.flow!.tick, a.flow!.tick);
  assert.equal(stateHash(b), stateHash(a), 'the loaded copy replays to the unbroken run');
  assert.equal(b.blocks[east].state, a.blocks[east].state);
  assert.equal(b.flow!.commissionSeq, a.flow!.commissionSeq);
  void vias;
});
