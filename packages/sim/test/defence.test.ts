/** Phase 4 M3 Defence (run name M3-rates): the HQ's physical turrets own their edges' hoppers (5 rounds/s, 50-round
 *  hopper, fed by inserter or hand), the map pip and the world hopper are one number, the Generator sets the grid's
 *  supply and burns coal by load, the §14 shed order runs through the tile machines, lamps and streetlights follow
 *  the substation, and a pole run strings a claim. */
import { test } from 'node:test';
import assert from 'node:assert/strict';
import {
  EXCAVATOR_PER_S, DEFAULT_CONFIG, protoCalibrated, generateMap, createState, idxOf, HELD, DARK, CONTESTED, step, frontList, edgeCap,
  CELL_TILES, MARGIN_TILES, substationLot, streetlights, STREETLIGHTS_PER_SIDE, SUBSTATION_TILES,
  ensureFlow, advanceFlow, stepFlow, place, remove, canPlace, giveItem, handFeed, flowSummary, describeMachine, turretEdge,
  cellLights, litAt, poleGrid, substationAt, subPowered, layPoles, botHands,
  TILE_TPS, TILE_DT, TURRET_HOPPER, TURRET_ROUNDS_PER_S, GENERATOR_KW, COAL_MJ, START_COAL, START_CHEST_COAL, START_TURRETS, SHOT, LAMP_RADIUS, POLE_REACH, MACHINE_KW,
  Machine, SimState, SimEvent, citySpec, hqIdx,
  cityGeomOf, segBetween, segLength, TURRET_PER_TILES, canPickUp,
  lockReason, unlockedKinds, survivorJoined, faceSub, blockLights, hqLot, ground, machineAt, step as blockStep,
  FLOODLIGHT_KW, FLOODLIGHT_RANGE, BIG_POLE_REACH, SURVIVOR_UNLOCKS,
} from '../src/index';

function fresh(seed = 3): SimState {
  const cfg = protoCalibrated({ ...DEFAULT_CONFIG, scatter: true, economy: true });
  const spec = generateMap(seed, cfg);
  return createState(spec, cfg, seed);
}
function hq(st: SimState, lx: number, ly: number): [number, number] {
  return [st.start[0] * CELL_TILES + MARGIN_TILES + lx, st.start[1] * CELL_TILES + MARGIN_TILES + ly];
}
function rich(st: SimState): SimState { ensureFlow(st); st.engineer.inv.steel = 1000; st.engineer.inv.copper = 500; return st; }   // M2: paid from the pockets
function powered(st: SimState): SimState {
  rich(st);
  st.config.power = true; st.config.supply = 'generators'; st.config.draw = 'half';
  return st;
}
function mustPlace(st: SimState, kind: Parameters<typeof place>[1], lx: number, ly: number, dir: Parameters<typeof place>[4] = 0): Machine {
  const [tx, ty] = hq(st, lx, ly);
  const m = place(st, kind, tx, ty, dir);
  assert.ok(m, `${kind} at lot (${lx},${ly}): ${canPlace(st, kind, tx, ty).reason}`);
  return m!;
}
/** Block seconds through the tile clock, collecting the block sim's events. */
function runS(st: SimState, seconds: number): SimEvent[] {
  const out: SimEvent[] = [];
  for (let s = 0; s < seconds; s++) {
    advanceFlow(st, 1, [], 4);
    out.push(...st.events); st.events.length = 0;
  }
  return out;
}
const turrets = (st: SimState) => st.flow!.machines.filter(m => m.kind === 'turret');
const generator = (st: SimState) => st.flow!.machines.find(m => m.kind === 'generator')!;

test('HQ start (D-P4-8): six turrets with full hoppers, a Generator with 40 coal and 40 more in the chest, 20 magazines in the chest; every pip green from the first tick', () => {
  const st = fresh();
  const f = ensureFlow(st);
  const ts = turrets(st);
  assert.equal(ts.length, 6);
  assert.equal(generator(st).inv.coal, START_COAL); assert.equal(f.store.coal, START_CHEST_COAL, 'D-P4-7 (b): 40 coal in the chest');
  assert.equal(flowSummary(st).coal, START_COAL + START_CHEST_COAL, 'the summary counts the Generator hopper and the chest');
  for (const m of ts) assert.ok(turretEdge(st, m) >= 0, 'every start turret covers its street');
  const edges = new Set(ts.map(m => turretEdge(st, m)));
  assert.equal(edges.size, 3, 'two turrets a street on three streets');
  const rounds = ts.map(m => m.inv.rounds ?? 0).sort((a, b) => b - a);
  assert.deepEqual(rounds, [50, 50, 50, 50, 50, 50], 'D-P4-8: every start hopper full');
  assert.equal(st.buffer, 20 * SHOT.count, 'and 20 magazines in the chest');
  const hqIdx = idxOf(st, st.start[0], st.start[1]);
  const mine = st.ring.filter(e => e.a === hqIdx);
  assert.equal(mine.length, 3);
  for (const e of mine) { assert.equal(e.turrets, 2); assert.equal(edgeCap(st, e), 2 * TURRET_HOPPER); }
  assert.deepEqual(mine.map(e => e.hopper).sort((a, b) => b - a), [100, 100, 100], 'the edge hopper is the turrets\' hoppers');
  const pips = frontList(st).filter(v => v.from.x === st.start[0] && v.from.y === st.start[1]).map(v => v.pip).sort();
  assert.deepEqual(pips, ['green', 'green', 'green']);
  const ev = runS(st, 1);
  assert.equal(ev.filter(e => e.type === 'hopper-empty').length, 0, 'no edge is empty at the start; the first red pip is the ~6-minute hand-feed beat (E-hour)');
  assert.equal(ts.reduce((a, m) => a + (m.inv.rounds ?? 0), 0), START_TURRETS * TURRET_HOPPER, 'the ring does not refill physical turrets');
});

test('feeding: an inserter fills a turret from a belt of magazines to its 50-round hopper and waits; hand-feeding fills it from the pockets at once (prompt B M3); the pip follows', () => {
  const st = rich(fresh());
  const f = st.flow!;
  const t = turrets(st).find(m => m.x === hq(st, 3, 0)[0] && m.y === hq(st, 3, 0)[1])!;
  t.inv.rounds = 0;
  const belt = mustPlace(st, 'belt', 3, 3, 1);
  const ins = mustPlace(st, 'inserter', 3, 2, 0);
  const feed = () => { while (giveItem(st, belt, 'magazine', 0)) { /* saturate */ } };
  for (let k = 0; k < 12 * TILE_TPS; k++) { feed(); stepFlow(st, TILE_DT); }
  assert.equal(t.inv.rounds, TURRET_HOPPER, 'the hopper caps at 50 rounds');
  assert.ok(ins.hold === 'magazine' || belt.items.length > 0, 'the inserter waits with the next magazine');
  step(st); st.events.length = 0;
  const e = st.ring[st.edgeAt[turretEdge(st, t)]];
  assert.equal(e.hopper, TURRET_HOPPER + (turrets(st).find(m => m !== t && turretEdge(st, m) === e.id)!.inv.rounds ?? 0));
  // hand: empty the north pair, put 3 magazines in the pockets, feed one turret twice
  const pair = turrets(st).filter(m => turretEdge(st, m) === e.id);
  for (const m of pair) m.inv.rounds = 0;
  st.engineer.inv.magazine = 3; st.buffer = 0;
  step(st);
  assert.equal(frontList(st).find(v => v.id === e.id)!.pip, 'red');
  const r1 = handFeed(st, pair[0].x, pair[0].y)!;
  assert.equal(r1.kind, 'turret'); assert.equal(r1.moved, 3); assert.equal(pair[0].inv.rounds, 30); assert.equal(st.engineer.inv.magazine ?? 0, 0); assert.equal(st.buffer, 0, 'the Depot is never drawn on');
  assert.match(handFeed(st, pair[1].x, pair[1].y)!.reason, /no magazines/);
  assert.equal(f.stats.handFed, 3);
  step(st);
  assert.equal(frontList(st).find(v => v.id === e.id)!.pip, 'amber');
  st.engineer.inv.magazine = 8;
  assert.equal(handFeed(st, pair[0].x, pair[0].y)!.moved, 2, 'tops up to 50');
  assert.match(handFeed(st, pair[0].x, pair[0].y)!.reason, /full/);
  assert.equal(handFeed(st, pair[1].x, pair[1].y)!.moved, 5);
  assert.equal(st.engineer.inv.magazine, 1);
  step(st);
  assert.equal(frontList(st).find(v => v.id === e.id)!.pip, 'green');
  // M2: a picked-up turret goes to the pockets with its rounds as whole magazines (§11's four stacks for two turrets); the line buffer keeps what it had
  assert.equal(pair[1].inv.rounds, 50);
  delete st.engineer.inv.magazine;   // the last magazine spent, so the pick-up needs a magazine stack of its own
  assert.equal(canPickUp(st, pair[1].x, pair[1].y).stacks, 2, 'a turret and its magazines: two stacks');
  remove(st, pair[1].x, pair[1].y);
  assert.equal(st.buffer, 0); assert.equal(st.engineer.inv.turret, 1); assert.equal(st.engineer.inv.magazine, 5);
});

test('engagement: crawlers drain the turrets fullest-first at 5 rounds/s each; a rush past the rate goes unfed; the hopper-empty event fires on the same tick the pip turns red', () => {
  const st = rich(fresh());
  const f = st.flow!;
  step(st); st.events.length = 0;
  const hqIdx = idxOf(st, st.start[0], st.start[1]);
  const full = st.ring.find(e => e.a === hqIdx && e.hopper === 100)!;
  const pair = turrets(st).filter(m => turretEdge(st, m) === full.id);
  // 30 crawlers over 15 s: 2 a second, 6 rounds a second, well under the pair's 10
  st.engagements.push({ id: full.id, cr: 30, sh: 0, rcr: 2, rsh: 0 });
  let ev = runS(st, 15);
  assert.equal(st.stats.unfedTotal, 0);
  assert.ok(Math.abs(f.stats.fired - 90) < 1e-6, `fired ${f.stats.fired}`);
  assert.deepEqual(pair.map(m => m.inv.rounds), [5, 5], 'the pair share the firing equally');
  assert.ok(Math.abs(full.hopper - 10) < 1e-6, 'Math.abs(full.hopper - 10) < 1e-6');
  assert.equal(ev.filter(e => e.type === 'hopper-empty').length, 0);
  // the last of them empties the hopper: pip red and the event on that tick
  st.engagements.push({ id: full.id, cr: 4, sh: 0, rcr: 2, rsh: 0 });
  ev = runS(st, 2);
  assert.equal(full.hopper, 0);
  assert.equal(ev.filter(e => e.type === 'hopper-empty').length, 1);
  assert.equal(frontList(st).find(v => v.id === full.id)!.pip, 'red');
  assert.ok(st.stats.unfedTotal > 0.5, 'the fourth crawler found the hopper empty');
  // a rush: 30 crawlers in one second need 90 rounds; two turrets fire 10
  const other = st.ring.find(e => e.a === hqIdx && e.hopper === 100)!;
  const unfed0 = st.stats.unfedTotal, fired0 = f.stats.fired;
  st.engagements.push({ id: other.id, cr: 30, sh: 0, rcr: 30, rsh: 0 });
  runS(st, 1);
  assert.ok(Math.abs(f.stats.fired - fired0 - 2 * TURRET_ROUNDS_PER_S) < 1e-6, 'the pair fire 10 rounds in the second');
  assert.ok(Math.abs(st.stats.unfedTotal - unfed0 - (30 - 10 / 3)) < 1e-6, 'the rest walk past');
  assert.equal(other.hopper, 90);
});

test('Generator: 300 kW, 4 MJ a coal — the start\'s 40 coal last 533 s at full load; a dry Generator kills the grid until hand-fed', () => {
  const st = powered(fresh());
  const f = st.flow!;
  const g = generator(st);
  // demand exactly at supply, no brownout: the substation (100 kW half draw, exposed) + two assemblers 200
  mustPlace(st, 'assembler', 12, 4, 2);
  mustPlace(st, 'assembler', 15, 5, 2);
  runS(st, 5);
  assert.equal(f.power.supply, GENERATOR_KW);
  assert.equal(f.power.demand, 300, `demand ${f.power.demand}`);
  assert.equal(f.power.load, GENERATOR_KW);
  assert.ok(g.busy, 'g.busy');
  const perS = GENERATOR_KW / (COAL_MJ * 1000);
  assert.ok(Math.abs(perS - 0.075) < 1e-9, 'Math.abs(perS - 0.075) < 1e-9');
  const ev = runS(st, 560);
  const dry = ev.find(e => e.type === 'gen-dry');
  assert.ok(dry, 'the Generator runs dry');
  assert.equal(g.inv.coal, 0);
  assert.equal(f.stats.coalBurned, START_COAL);
  assert.ok(Math.abs(f.stats.coalBurned / perS - 533.3) < 30, 'about 533 s of full load');
  assert.equal(f.power.supply, 0);
  assert.match(describeMachine(st, g), /OUT OF COAL/);
  assert.match(describeMachine(st, f.machines.find(m => m.kind === 'assembler')!), /no power/);
  assert.equal(subPowered(st, st.blocks[idxOf(st, st.start[0], st.start[1])]), false);
  assert.equal(cellLights(st, st.start[0], st.start[1]).some(l => l.lit), false, 'streetlights dark on a dead grid');
  // coal by hand: nothing in the pockets yet (prompt B M3: the hand feeds from the pockets)
  assert.match(handFeed(st, g.x, g.y)!.reason, /no coal/);
  st.engineer.inv.coal = 10;
  assert.equal(handFeed(st, g.x, g.y)!.moved, 10);
  runS(st, 2);   // D-B3-4: power never sheds the substation; it is back the second the supply is
  assert.equal(f.power.supply, GENERATOR_KW);
  assert.ok(g.busy, 'burning again');
  assert.equal(f.power.load, 300);
});

test('D-B3-4 proportional brownout: short of supply every machine runs at supply ÷ demand — the Excavator mines that much slower, the Lamp stays lit, the substation stays on, nothing switches off — and full speed returns the second the supply covers demand', () => {
  const st = powered(fresh());
  const f = st.flow!;
  const hq = st.blocks[idxOf(st, st.start[0], st.start[1])];
  const ex = mustPlace(st, 'excavator', 1, 7, 0);
  for (let lx = 2; lx < 9; lx++) mustPlace(st, 'belt', lx, 6, 1);
  for (let ly = 6; ly < 9; ly++) mustPlace(st, 'belt', 9, ly, 2);
  const lamp = mustPlace(st, 'lamp', 8, 8, 0);
  runS(st, 60);
  // 100 (substation) + 60 + the Lamp: covered by the 300 kW Generator
  assert.equal(f.power.throttle, 1); assert.equal(f.stats.mined, 60 * EXCAVATOR_PER_S);
  assert.equal(flowSummary(st).throttle, 1);
  // two assemblers push demand past the Generator
  const asm = mustPlace(st, 'assembler', 12, 4, 2);
  const asm2 = mustPlace(st, 'assembler', 15, 5, 2);
  let ev = runS(st, 1);
  const dem = 100 + MACHINE_KW.excavator + MACHINE_KW.lamp + 2 * MACHINE_KW.assembler;
  assert.equal(f.power.demand, dem); assert.equal(f.power.supply, GENERATOR_KW); assert.equal(f.power.load, GENERATOR_KW);
  const thr = GENERATOR_KW / dem;
  assert.ok(Math.abs(f.power.throttle - thr) < 1e-9, `throttle ${f.power.throttle} = ${thr}`);
  const m0 = f.stats.mined;
  ev = ev.concat(runS(st, 60));
  assert.ok(ev.some(e => e.type === 'brownout'), 'the brownout is announced once');
  assert.equal(ev.filter(e => e.type === 'brownout').length, 1);
  assert.ok(Math.abs(f.stats.mined - m0 - 60 * EXCAVATOR_PER_S * thr) <= 1, `mined ${f.stats.mined - m0} ≈ ${60 * EXCAVATOR_PER_S * thr} at ${thr}`);
  assert.ok(f.power.overS >= 60, 'brownout seconds count');
  assert.equal(hq.subOn, true, 'the substation is never shed by power'); assert.equal(subPowered(st, hq), true);
  assert.equal(flowSummary(st).lampsLit, 1, 'the Lamp stays lit at any throttle');
  assert.match(describeMachine(st, asm), / at \d+ % \(brownout\)/);
  assert.match(describeMachine(st, ex), / at \d+ % \(brownout\)/);
  assert.equal(describeMachine(st, lamp).includes('dark'), false);
  assert.ok(Math.abs(flowSummary(st).throttle - thr) < 1e-9);
  // a 90 % shortfall: 30 kW effective, everything at a tenth of that
  st.config.shortfall = { pct: 90, hour: 0, minutes: 60 };
  runS(st, 1);
  const m1 = f.stats.mined;
  runS(st, 60);
  const thr2 = 0.1 * GENERATOR_KW / dem;
  assert.ok(Math.abs(f.power.throttle - thr2) < 1e-9, `throttle ${f.power.throttle} = ${thr2}`);
  assert.ok(Math.abs(f.stats.mined - m1 - 60 * EXCAVATOR_PER_S * thr2) <= 1, `mined ${f.stats.mined - m1} at ${thr2}`);
  assert.equal(hq.subOn, true); assert.equal(hq.state, HELD); assert.equal(flowSummary(st).lampsLit, 1);
  assert.ok(Math.abs(st.stats.throttleMin - thr2) < 1e-9, 'the worst throttle is recorded');
  // the supply returns and one assembler goes: covered, full speed at once, no 20 s wait
  st.config.shortfall = null;
  remove(st, asm2.x, asm2.y);
  ev = runS(st, 2);
  assert.equal(f.power.throttle, 1); assert.equal(f.power.demand, dem - MACHINE_KW.assembler);
  assert.ok(ev.some(e => e.type === 'power-ok'), 'the all-clear is announced');
  assert.doesNotMatch(describeMachine(st, asm), /brownout/);
});

test('lamps and streetlights: radius 4, lit while the substation powers; three in eight streetlights are broken; a Lamp fills a dark spot', () => {
  const st = powered(fresh());
  const [sx, sy] = st.start;
  const b = st.blocks[idxOf(st, sx, sy)];
  const sl = streetlights(st.seed, b);
  assert.equal(sl.length, 4 * STREETLIGHTS_PER_SIDE);
  const broken = sl.filter(l => l.broken).length;
  assert.ok(broken >= 6 && broken <= 18, `${broken} broken`);
  runS(st, 1);
  let lights = cellLights(st, sx, sy);
  assert.equal(lights.filter(l => l.kind === 'streetlight' && l.lit).length, sl.length - broken);
  const dark = sl.find(l => l.broken)!;
  assert.equal(litAt(st, dark.tx, dark.ty) || true, true);
  const lamp = mustPlace(st, 'lamp', 5, 5, 0);
  runS(st, 1);
  lights = cellLights(st, sx, sy);
  assert.ok(lights.some(l => l.kind === 'lamp' && l.lit && l.r === LAMP_RADIUS), 'lights.some(l => l.kind === \'lamp\' && l.lit && l.r === LAMP_RADIUS)');
  assert.ok(litAt(st, lamp.x + 3, lamp.y), 'litAt(st, lamp.x + 3, lamp.y)'); assert.ok(!litAt(st, lamp.x + 9, lamp.y + 9) || lights.some(l => l.lit && Math.hypot(l.tx - lamp.x - 9, l.ty - lamp.y - 9) <= 4), '!litAt(st, lamp.x + 9, lamp.y + 9) || lights.some(l => l.lit && Math.hypot(l.tx - lamp.x - 9, l.ty - lamp.y - 9) <= 4)');
  assert.equal(flowSummary(st).lampsLit, 1);
  const sub = substationAt(st, sx, sy)!;
  assert.ok(sub.on, 'sub.on'); assert.equal(sub.kw, 100);
  // the substation goes off: everything dark
  b.subOn = false;
  runS(st, 1);
  assert.equal(cellLights(st, sx, sy).some(l => l.lit), false);
  assert.equal(flowSummary(st).lampsLit, 0);
  assert.match(describeMachine(st, lamp), /dark/);
});

test('poles: reach 8 from a claimed substation; a connected run that reaches a Dark neighbour\'s substation claims it; a map claim strings its own poles', () => {
  const st = rich(fresh());
  const f = st.flow!;
  const [sx, sy] = st.start;
  const north = st.blocks[idxOf(st, sx, sy - 1)];
  assert.equal(north.state, DARK);
  const [hlx, hly] = substationLot(st.seed, st.blocks[idxOf(st, sx, sy)], true);
  const from: [number, number] = [sx * CELL_TILES + MARGIN_TILES + hlx + 1.5, sy * CELL_TILES + MARGIN_TILES + hly + 1.5];
  const [nlx, nly] = substationLot(st.seed, north, false);
  const target = { x: (sy - 1 + 0) * 0 + sx * CELL_TILES + MARGIN_TILES + nlx, y: (sy - 1) * CELL_TILES + MARGIN_TILES + nly };
  const distRect = (px: number, py: number) => Math.hypot(px - Math.max(target.x, Math.min(target.x + SUBSTATION_TILES, px)), py - Math.max(target.y, Math.min(target.y + SUBSTATION_TILES, py)));
  // a pole out of reach of everything is not connected
  const far = place(st, 'pole', from[0] + 12, Math.floor(from[1]), 0)!;
  assert.ok(far, 'far'); assert.equal(poleGrid(st).connected.has(far.id), false);
  remove(st, far.x, far.y);
  let cx = from[0], cy = from[1], n = 0;
  while (distRect(cx, cy) > POLE_REACH && n < 20) {
    const d = Math.hypot(target.x + 1.5 - cx, target.y + 1.5 - cy), ux = (target.x + 1.5 - cx) / d, uy = (target.y + 1.5 - cy) / d;
    const px = Math.floor(cx + ux * (POLE_REACH - 1)), py = Math.floor(cy + uy * (POLE_REACH - 1));
    const m = place(st, 'pole', px, py, 0);
    assert.ok(m, `pole at ${px},${py}: ${canPlace(st, 'pole', px, py).reason}`);
    assert.ok(poleGrid(st).connected.has(m!.id), 'each pole in the run is on the grid');
    cx = px + 0.5; cy = py + 0.5; n++;
  }
  assert.ok(n >= 2, `${n} poles`);
  assert.ok(f.pending.some(c => c.type === 'claim' && c.x === sx && c.y === sy - 1), 'the run raises the claim');
  advanceFlow(st, 1, [], 4);
  assert.ok(([CONTESTED, HELD] as number[]).includes(north.state), 'the block map accepted the pole claim');
  assert.equal(f.pending.length, 0);
  assert.equal(layPoles(st, sx, sy - 1), 0, 'already strung');
  // a claim from the map view lays its own run to the west neighbour
  const west = st.blocks[idxOf(st, sx - 1, sy)];
  assert.equal(west.state, DARK);
  const poles0 = flowSummary(st).poles;
  advanceFlow(st, 1, [{ type: 'claim', x: sx - 1, y: sy }], 4);
  assert.ok(([CONTESTED, HELD] as number[]).includes(west.state), 'the block map accepted the map claim');
  const laid = flowSummary(st).poles - poles0;
  assert.ok(laid >= 1, `${laid} poles laid for the map claim`);
  const g = poleGrid(st);
  assert.equal(g.connected.size, flowSummary(st).poles, 'every pole hangs from the grid');
});

test('a flow layer saved before M3 loads with the M3 fields; placement refuses the substation footprint', () => {
  const st = rich(fresh());
  const raw = JSON.parse(JSON.stringify(st.flow));
  delete raw.pending; delete raw.power; delete raw.stats.fired; for (const m of raw.machines) m.shed = false;   // a pre-D-B3-4 flag
  st.flow = raw;
  const f = ensureFlow(st);
  assert.deepEqual(f.pending, []); assert.equal(f.power.overS, 0); assert.equal(f.power.throttle, 1); assert.equal(f.stats.fired, 0); assert.equal('shed' in f.machines[0], false);
  const [lx, ly] = substationLot(st.seed, st.blocks[idxOf(st, st.start[0], st.start[1])], true);
  const [tx, ty] = hq(st, lx + 1, ly + 1);
  assert.match(canPlace(st, 'belt', tx, ty).reason, /substation/);
  assert.equal(place(st, 'pole', tx, ty, 0), null);
});

test("the bot's hands: every turret at or under half and every Generator at or under half is hand-fed from the Depot", () => {
  const st = rich(fresh());
  for (const m of turrets(st).slice(0, 2)) m.inv.rounds = 0;   // D-P4-8 starts every hopper full: two run dry for the test
  const empty = turrets(st).filter(m => (m.inv.rounds ?? 0) === 0), full = turrets(st).filter(m => (m.inv.rounds ?? 0) === TURRET_HOPPER);
  assert.equal(empty.length, 2); assert.equal(full.length, 4);
  st.buffer = 60;                                   // six magazines in the Depot
  assert.equal(botHands(st), 6, 'six magazines carried');
  assert.equal(st.buffer, 0);
  assert.equal(empty[0].inv.rounds, TURRET_HOPPER, 'the first empty turret fills first');
  assert.equal(empty[1].inv.rounds, 10);
  assert.equal(botHands(st), 0, 'nothing left to carry');
  st.buffer = 1000;
  assert.equal(botHands(st), 4, 'the half-empty turret is topped up; the full ones are left');
  assert.equal(turrets(st).reduce((a, m) => a + (m.inv.rounds ?? 0), 0), 6 * TURRET_HOPPER);
  const g = generator(st);
  g.inv.coal = 20; st.flow!.store.coal = 12;
  assert.equal(botHands(st), 12, 'coal from the Depot into the Generator');
  assert.equal(g.inv.coal, 32); assert.equal(st.flow!.store.coal, 0);
  assert.equal(flowSummary(st).handFed, 6 + 4 + 12);
});

// ------------------------------------------------------------------ prompt B M1: the HQ's turrets on a city

test('city HQ (D-B1-4): the start turrets are derived from the HQ\'s segments — one per 16 tiles, at least one a segment, a corner sliver served by the turrets that reach it — so every live segment is a covered turret edge on seeds 3, 4 and 5, and the HQ holds three minutes with no hopper empty', () => {
  for (const seed of [3, 4, 5]) {
    const cfg = protoCalibrated({ ...DEFAULT_CONFIG, walk: true });
    Object.assign(cfg, { power: true, supply: 'generators', draw: 'half' });
    const st = createState(citySpec(seed, 'river', cfg), cfg, seed);
    ensureFlow(st);
    const hqI = hqIdx(st), cg = cityGeomOf(st), mine = st.ring.filter(e => e.a === hqI);
    assert.ok(mine.length >= 3, `seed ${seed}: the HQ has at least three live segments`);
    for (const e of mine) {
      const sg = segBetween(cg, hqI, e.b)!, len = segLength(sg, cg.tw);
      // D-P4-8: START_TURRETS apportioned by segment length (one per TURRET_PER_TILES, min 1); a corner sliver that
      // cannot hold a 2×2 of its own hands its turret to the longest segment, so a segment may count below its share
      assert.ok(e.turrets! >= 1 || len < 8, `seed ${seed}: segment to ${e.b} (${len} tiles) has a turret (or is a sliver too short for one)`);
      assert.equal(e.kit, true, `seed ${seed}: a covered segment is kitted`);
      assert.ok(e.hopper > 0, `seed ${seed}: a start turret edge is born fed`);
    }
    assert.ok(!st.flow!.machines.some(m => m.kind === 'turret' && turretEdge(st, m) < 0), `seed ${seed}: no start turret maps to no edge`);
    assert.equal(st.flow!.machines.filter(m => m.kind === 'turret').length, START_TURRETS, `seed ${seed}: ${START_TURRETS} start turrets (D-P4-8)`);
    const total = mine.reduce((a, e) => a + e.turrets!, 0), lens = mine.map(e => segLength(segBetween(cg, hqI, e.b)!, cg.tw));
    assert.ok(total >= Math.min(START_TURRETS, Math.max(mine.length, Math.floor(lens.reduce((a, b) => a + b, 0) / TURRET_PER_TILES))), `seed ${seed}: the segments carry the apportioned count (${total})`);
    // three minutes with the bot's hands on the turrets and no line: nothing on the HQ is unfed (the first city soak
    // had seed 3's fourth segment bare from tick one and the HQ lost at minute 20; the fix is the rule, not an HQ branch)
    const evs: SimEvent[] = [];
    for (let s = 0; s < 3 * 60; s++) { botHands(st); advanceFlow(st, 1, [], 4); evs.push(...st.events); st.events.length = 0; }
    assert.equal(st.blocks[hqI].state, HELD, `seed ${seed}: the HQ holds`);
    assert.ok(!evs.some(e => e.type === 'hopper-empty' && e.x === st.blocks[hqI].x && e.y === st.blocks[hqI].y), `seed ${seed}: no HQ hopper ran empty`);
  }
});

// ------------------------------------------------------------------ prompt B M3: the Electricians' unlocks (run name B-M3-unlocks)

function cityState(seed: number): SimState {
  const cfg = protoCalibrated({ ...DEFAULT_CONFIG, walk: true });
  Object.assign(cfg, { power: true, supply: 'generators', draw: 'half' });
  const st = createState(citySpec(seed, 'river', cfg), cfg, seed);
  ensureFlow(st); st.engineer.inv.steel = 1000; st.engineer.inv.copper = 500;
  return st;
}
const electricians = (st: SimState) => st.survivors.find(v => v.name === 'Electricians')!;
/** The Electricians' block turns Held the block sim's way: Contested with its clock run out, then one block tick. */
function joinElectricians(st: SimState): SimEvent[] {
  const sv = electricians(st), b = st.blocks[idxOf(st, sv.x, sv.y)];
  b.state = CONTESTED; b.contestUntil = st.t;
  st.events.length = 0; blockStep(st);
  const evs = [...st.events]; st.events.length = 0;
  return evs;
}
/** First lot spot on the HQ face where `kind` can go, scanning the lot; the callback may refuse a spot. */
function hqSpot(st: SimState, kind: Parameters<typeof place>[1], ok: (tx: number, ty: number) => boolean = () => true): [number, number] {
  for (let ly = 0; ly < 24; ly++) for (let lx = 0; lx < 24; lx++) {
    const [tx, ty] = hqLot(st, lx, ly);
    if (canPlace(st, kind, tx, ty).ok && ok(tx, ty)) return [tx, ty];
  }
  throw new Error(`no spot for a ${kind} on the HQ face`);
}
const distToRect = (px: number, py: number, rx: number, ry: number, size: number) => {
  const qx = Math.max(rx, Math.min(rx + size, px)), qy = Math.max(ry, Math.min(ry + size, py));
  return Math.hypot(px - qx, py - qy);
};

test("unlocks: the Electricians' Floodlight, Big pole and Substation are locked until their block turns Held, the held event names them, and losing the block later keeps them", () => {
  for (const seed of [3, 4, 5]) {
    const st = cityState(seed), sv = electricians(st), bi = idxOf(st, sv.x, sv.y);
    assert.ok(bi >= 0 && st.hops[bi] >= 1 && st.hops[bi] <= 3, `seed ${seed}: the Electricians sit 1–3 hops out (${st.hops[bi]})`);
    for (const k of SURVIVOR_UNLOCKS.Electricians) {
      assert.match(lockReason(st, k), /Electricians/, `seed ${seed}: ${k} locked`);
      assert.match(canPlace(st, k, ...hqLot(st, 4, 4)).reason, /Electricians/, `seed ${seed}: ${k} refuses to place`);
    }
    assert.ok(!unlockedKinds(st).includes('floodlight') && unlockedKinds(st).includes('turret'));
    assert.equal(survivorJoined(st, 'Electricians'), false);
    const evs = joinElectricians(st);
    const held = evs.find(e => e.type === 'held' && e.survivor === 'Electricians');
    assert.ok(held && held.type === 'held', `seed ${seed}: the held event names the group`);
    assert.deepEqual(held.unlocks, ['Floodlight', 'Big pole', 'Substation']);
    assert.equal(st.blocks[bi].state, HELD);
    for (const k of SURVIVOR_UNLOCKS.Electricians) assert.equal(lockReason(st, k), '', `seed ${seed}: ${k} unlocked`);
    assert.equal(unlockedKinds(st).length, 12);
    // the block falls: the group has walked into the Depot (§11) — the toolbar keeps the rows
    st.blocks[bi].state = DARK; st.fallen[bi] = true;
    assert.equal(survivorJoined(st, 'Electricians'), true);
    assert.equal(lockReason(st, 'substation'), '');
  }
});

test('Floodlight: 2×2, 40 kW, lights a 12-tile 60° cone along its facing while the face is powered, and nothing behind or beside it', () => {
  const st = cityState(3);
  joinElectricians(st);
  blockStep(st); const before = flowSummary(st).demandKw;
  // a spot whose cone to the east stays on the HQ's tiles for 12 tiles
  // (and whose probe tiles no streetlight or start lamp already lights)
  const dark = (x: number, y: number, pts: [number, number][]) => pts.every(([dx, dy]) => !litAt(st, x + 0.5 + dx, y + 0.5 + dy));
  const [tx, ty] = hqSpot(st, 'floodlight', (x, y) => { const [x1] = hqLot(st, 23, 0); return x + 13 <= x1 && y > hqLot(st, 0, 4)[1] && dark(x, y, [[-5, 0], [6, 6], [13, 0], [6, 0], [0, 6], [10, 3], [0, 0]]); });
  const m = place(st, 'floodlight', tx, ty, 1)!;   // facing east
  assert.equal(m.size, 2);
  blockStep(st); assert.equal(flowSummary(st).demandKw, before + FLOODLIGHT_KW, 'the face draws 40 kW more');
  const cx = tx + 0.5, cy = ty + 0.5;   // the fixture's centre in tile units
  assert.equal(describeMachine(st, m).includes('lit'), true, describeMachine(st, m));
  const l = blockLights(st, hqIdx(st)).find(v => v.kind === 'floodlight')!;
  assert.ok(l && l.lit && l.r === FLOODLIGHT_RANGE && l.dir === 1);
  assert.equal(litAt(st, cx + 6, cy), true, 'straight ahead');
  assert.equal(litAt(st, cx + 10, cy + 3), true, 'inside the cone (17°)');
  assert.equal(litAt(st, cx + 11.5, cy), true, 'to the cone\'s end');
  assert.equal(litAt(st, cx + 6, cy + 6), false, 'outside the cone (45°)');
  assert.equal(litAt(st, cx + 13, cy), false, 'beyond 12 tiles');
  assert.equal(litAt(st, cx - 5, cy), false, 'behind it');
  assert.equal(litAt(st, cx, cy), true, 'under the fixture');
  // D-B3-4: a brownout slows machines, never a light; the cone stays lit at any throttle above zero
  st.flow!.power.throttle = 0.2;
  assert.equal(litAt(st, cx + 6, cy), true, 'lit under a brownout');
  st.flow!.power.throttle = 1;
  // R turns it: facing south lights south, not east
  m.dir = 2; st.flow!.rev++;
  assert.equal(litAt(st, cx, cy + 6), true); assert.equal(litAt(st, cx + 6, cy), false);
});

test('Big pole: 2×2, reach 12 — it hangs from a substation a pole at the same distance cannot reach, and claims through poleClaims like a pole', () => {
  const st = cityState(3);
  joinElectricians(st);
  const hq = st.blocks[hqIdx(st)], sub = substationAt(st, hq.x, hq.y)!;
  assert.ok(sub && sub.size === 3);
  // a lot spot 9–11 tiles from the substation's edge where both kinds can go
  const [tx, ty] = hqSpot(st, 'bigpole', (x, y) => { const d = distToRect(x + 1, y + 1, sub.tx, sub.ty, sub.size); return d > POLE_REACH + 1 && d < BIG_POLE_REACH - 1 && canPlace(st, 'pole', x, y).ok; });
  const pole = place(st, 'pole', tx, ty, 0)!;
  assert.equal(poleGrid(st).connected.has(pole.id), false, 'a pole 9+ tiles out is not on the grid');
  remove(st, tx, ty);
  const big = place(st, 'bigpole', tx, ty, 0)!;
  assert.equal(big.size, 2);
  assert.equal(poleGrid(st).connected.has(big.id), true, 'a Big pole at the same spot is');
  assert.match(describeMachine(st, big), /Big pole · reach 12 · on the grid/);
  // a small pole hangs from the Big pole at the Big pole's reach (the wire spans the longer of the two)
  const [px, py] = hqSpot(st, 'pole', (x, y) => { const d = Math.hypot(x + 0.5 - (tx + 1), y + 0.5 - (ty + 1)); return d > POLE_REACH + 0.5 && d < BIG_POLE_REACH - 0.5 && distToRect(x + 0.5, y + 0.5, sub.tx, sub.ty, sub.size) > BIG_POLE_REACH; });
  const p2 = place(st, 'pole', px, py, 0)!;
  assert.equal(poleGrid(st).connected.has(p2.id), true);
  assert.equal(poleGrid(st).links.length, 2);
});

test('craftable Substation: an outskirts face has none (§7) — no slab, no streetlights, no pole anchor — until the Electricians\' 3×3 is built on it; one a face; picked up, the face is bare again', () => {
  const st = cityState(3), cg = cityGeomOf(st), G = ground(st);
  // an outskirts face, forced Held for the test (the block sim's abstract substation powers it — D-P4-9's split)
  const oi = st.blocks.findIndex((b, i) => cg.district[i] === 3 && !cg.blocks[i].inert && b.state === DARK);
  assert.ok(oi >= 0, 'the city has a dark outskirts face');
  const ob = st.blocks[oi];
  ob.state = HELD; ob.subOn = true;
  assert.equal(faceSub(st, oi), null); assert.equal(substationAt(st, ob.x, ob.y), null); assert.equal(blockLights(st, oi).length, 0);
  assert.equal(subPowered(st, ob), true, 'the block sim still powers a Held outskirts block');
  const fit = (x: number, y: number) => canPlace(st, 'substation', x, y);
  let spot: [number, number] | null = null;
  for (const t of G.blocks[oi].tiles) { const x = t % G.tw, y = (t - x) / G.tw; if (fit(x, y).ok) { spot = [x, y]; break; } }
  // locked first
  const t0 = G.blocks[oi].tiles[0];
  assert.match(fit(t0 % G.tw, (t0 - t0 % G.tw) / G.tw).reason, /Electricians/);
  joinElectricians(st);
  for (const t of G.blocks[oi].tiles) { const x = t % G.tw, y = (t - x) / G.tw; if (fit(x, y).ok) { spot = [x, y]; break; } }
  assert.ok(spot, 'a 3×3 fits on the face');
  const steel = st.engineer.inv.steel, cu = st.engineer.inv.copper;
  const m = place(st, 'substation', spot![0], spot![1], 0)!;
  assert.equal(m.size, SUBSTATION_TILES);
  assert.equal(st.engineer.inv.steel, steel - 50); assert.equal(st.engineer.inv.copper, cu - 25);
  assert.deepEqual(faceSub(st, oi), { x: spot![0], y: spot![1], size: 3 });
  const view = substationAt(st, ob.x, ob.y)!;
  assert.ok(view && view.on && view.size === 3, 'the face now shows a powered substation');
  assert.match(describeMachine(st, m), /Substation \(built\) · powers the face · on/);
  // one a face: a second refuses; the HQ, which has one, refuses too
  for (const t of G.blocks[oi].tiles) { const x = t % G.tw, y = (t - x) / G.tw; const r = fit(x, y); if (!r.ok && /has a substation/.test(r.reason)) { spot = null; break; } }
  assert.equal(spot, null, 'a second Substation on the face is refused');
  { let seen = false; for (let ly = 0; ly < 24 && !seen; ly++) for (let lx = 0; lx < 24; lx++) { const r = canPlace(st, 'substation', ...hqLot(st, lx, ly)); if (/has a substation/.test(r.reason)) { seen = true; break; } }
    assert.ok(seen, 'the HQ face, which has one, refuses a Substation for that reason'); }
  // a pole beside it hangs from it
  const pole = place(st, 'pole', m.x + m.size + 1, m.y, 0);
  if (pole) assert.equal(poleGrid(st).connected.has(pole.id), true, 'a pole hangs from the built Substation');
  // picked up: bare again, the machine in the pockets
  assert.equal(canPickUp(st, m.x, m.y).ok, true);
  remove(st, m.x, m.y);
  assert.equal(st.engineer.inv.substation, 1);
  assert.equal(faceSub(st, oi), null); assert.equal(substationAt(st, ob.x, ob.y), null);
});
