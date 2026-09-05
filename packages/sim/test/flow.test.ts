/** Phase 4 M2 Flow (run name M2-rates): the tile machines' measured rates against the doc's, the 20 ticks/s tile
 *  tick with its derived block tick, placement rules and costs, hand-mining and hand-crafting, and that the block
 *  map stays the judge (a machine on a block that is not Held stops; digging draws the block's pool). */
import { test } from 'node:test';
import assert from 'node:assert/strict';
import {
  DEFAULT_CONFIG, protoCalibrated, generateMap, createState, idxOf, HELD, DARK, step, advance,
  CELL_TILES, MARGIN_TILES, T_GROUND, T_PATCH, P_STEEL, cellTiles, cellKey, lotLayout, HQ_PATCHES, HQ_RUBBLE_TILES,
  ensureFlow, advanceFlow, stepFlow, place, remove, rotate, canPlace, machineAt, rubbleAt, giveItem, setHandMine, queueCraft,
  flowSummary, describeMachine, outputTile, entryDir,
  TILE_TPS, TILE_DT, BELT_PER_S, BELT_SPACING, EXCAVATOR_PER_S, INSERTER_PER_S, SHOT, MACHINE_COST, ASM_INPUT_MULT,
  Machine, SimState,
  inReach, chestPut, chestTake, canPickUp, invStacks, INV_STACKS,
} from '../src/index';

function fresh(seed = 3): SimState {
  const cfg = protoCalibrated({ ...DEFAULT_CONFIG, scatter: true, economy: true });
  const spec = generateMap(seed, cfg);
  return createState(spec, cfg, seed);
}
/** City tile of a lot tile on the start lot. */
function hq(st: SimState, lx: number, ly: number): [number, number] {
  return [st.start[0] * CELL_TILES + MARGIN_TILES + lx, st.start[1] * CELL_TILES + MARGIN_TILES + ly];
}
function run(st: SimState, seconds: number): void { for (let k = 0; k < seconds * TILE_TPS; k++) stepFlow(st, TILE_DT); }
/** M2: machines are paid from the pockets — 20 stacks of steel and 10 of copper, ten stacks free for pick-ups. */
function rich(st: SimState): SimState { ensureFlow(st); st.engineer.inv.steel = 1000; st.engineer.inv.copper = 500; return st; }
function mustPlace(st: SimState, kind: Parameters<typeof place>[1], lx: number, ly: number, dir: Parameters<typeof place>[4] = 0): Machine {
  const [tx, ty] = hq(st, lx, ly);
  const m = place(st, kind, tx, ty, dir);
  assert.ok(m, `${kind} at lot (${lx},${ly}): ${canPlace(st, kind, tx, ty).reason}`);
  return m!;
}

test('the HQ lot: §11 patches, a clear Depot footprint, the Depot placed by ensureFlow, the Mk1 stand-in retired', () => {
  const st = fresh();
  const [sx, sy] = st.start;
  const c = cellTiles(st, sx, sy);
  let patchTiles = 0;
  for (let i = 0; i < c.kind.length; i++) if (c.kind[i] === T_PATCH) patchTiles++;
  assert.equal(patchTiles, HQ_PATCHES.reduce((a, p) => a + p.w * p.h, 0));
  for (let ly = 9; ly < 15; ly++) for (let lx = 9; lx < 15; lx++) assert.equal(c.kind[(ly + MARGIN_TILES) * CELL_TILES + lx + MARGIN_TILES], T_GROUND, 'the Depot footprint is clear ground');
  const lay = lotLayout(st.seed, st.blocks[idxOf(st, sx, sy)], true);
  assert.equal(lay.tiles, HQ_RUBBLE_TILES, 'the start lot is cleared down to its south strip');
  const hqBlock = st.blocks[idxOf(st, sx, sy)];
  assert.equal(hqBlock.machines, 1, 'the block sim starts with the Mk1');
  const prod0 = st.config.startAsmRate;
  const f = ensureFlow(st);
  assert.equal(f.machines.length, 8, 'Depot, six turrets, one Generator (M3)'); assert.equal(f.machines[0].kind, 'depot');
  assert.equal(hqBlock.machines, 0, 'the Mk1 stand-in retires when the real machines arrive');
  assert.equal(st.asmManual, 0);
  assert.ok(prod0 !== null);
  const [dx, dy] = hq(st, 9, 9);
  assert.equal(machineAt(st, dx, dy)?.kind, 'depot'); assert.equal(machineAt(st, dx + 5, dy + 5)?.kind, 'depot'); assert.equal(machineAt(st, dx + 6, dy), undefined);
  // the HQ steel patch is now dug, not drained: 60 block ticks leave it where it was
  const patch = st.patch.steel;
  for (let k = 0; k < 60; k++) step(st);
  assert.equal(st.patch.steel, patch);
  assert.ok(describeMachine(st, f.machines[0]).startsWith('Depot'));
});

test('Excavator: 0.5 units/s onto the belt it faces, one tile at a time; the steel patch and the tile go with it', () => {
  const st = rich(fresh());
  const f = st.flow!;
  // on the steel patch (lot 1..5 × 7..11), facing north: output at lot (2,6), a belt run east then south into the Depot
  const ex = mustPlace(st, 'excavator', 1, 7, 0);
  assert.deepEqual(outputTile(ex), hq(st, 2, 6));
  for (let lx = 2; lx < 9; lx++) mustPlace(st, 'belt', lx, 6, 1);
  for (let ly = 6; ly < 9; ly++) mustPlace(st, 'belt', 9, ly, 2);
  const steel0 = st.stock.steel, patch0 = st.patch.steel;
  run(st, 600);
  assert.equal(f.stats.mined, 600 * EXCAVATOR_PER_S, 'units mined in 600 s');
  assert.equal(st.patch.steel, patch0 - 300, 'the HQ steel patch drops a unit per unit mined');
  const inTransit = f.machines.filter(m => m.kind === 'belt').reduce((a, m) => a + m.items.length, 0) + (ex.hold ? 1 : 0);
  assert.equal(f.stats.delivered.steel + inTransit, 300, 'every unit is on the belt or in the Depot');
  assert.equal(st.stock.steel - steel0, f.stats.delivered.steel);
  assert.ok(f.stats.delivered.steel >= 290, `delivered ${f.stats.delivered.steel}`);
  assert.equal(f.dug[idxOf(st, st.start[0], st.start[1])]?.length ?? 0, 0, '300 units do not empty a ~307-unit tile');
  run(st, 640);   // 620 units: two tiles gone
  const dug = f.dug[idxOf(st, st.start[0], st.start[1])];
  assert.equal(dug.length, 2, 'two patch tiles dug out');
  const c = cellTiles(st, st.start[0], st.start[1]);
  const tw = st.w * CELL_TILES;
  for (const t of dug) { const gx = t % tw, gy = (t - gx) / tw, lx = gx - st.start[0] * CELL_TILES, ly = gy - st.start[1] * CELL_TILES; assert.equal(c.kind[ly * CELL_TILES + lx], T_GROUND, 'a dug patch tile is ground'); }
  assert.notEqual(cellKey(st, st.start[0], st.start[1]).split(':')[2], '0', 'the cell key counts dug tiles');
  // a blocked output stops the drill with one unit waiting
  const st2 = rich(fresh());
  const ex2 = mustPlace(st2, 'excavator', 1, 7, 0);
  run(st2, 30);
  assert.equal(ex2.hold, 'steel'); assert.equal(st2.flow!.stats.mined, 1);
});

test('belt: 7.5 items/s through a 12-tile run with a corner; items keep their spacing; a head-on belt does not feed', () => {
  const st = rich(fresh());
  const f = st.flow!;
  // lot row 6 (row 4 has the M3 west turret), lx 0..8 east, then lx 9 rows 6..8 south into the Depot at (9,9)
  const belts: Machine[] = [];
  for (let lx = 0; lx < 9; lx++) belts.push(mustPlace(st, 'belt', lx, 6, 1));
  for (let ly = 6; ly < 9; ly++) belts.push(mustPlace(st, 'belt', 9, ly, 2));
  assert.equal(entryDir(st, belts[9]), 1, 'the corner belt takes its items from the east-running belt');
  assert.equal(entryDir(st, belts[10]), 2);
  const feed = () => { while (giveItem(st, belts[0], 'stone', 0)) { /* saturate the head */ } };
  for (let k = 0; k < 20 * TILE_TPS; k++) { feed(); stepFlow(st, TILE_DT); }
  const d0 = f.stats.delivered.stone;
  for (let k = 0; k < 60 * TILE_TPS; k++) {
    feed(); stepFlow(st, TILE_DT);
    for (const b of belts) for (let i = 1; i < b.items.length; i++) assert.ok(b.items[i].p - b.items[i - 1].p >= BELT_SPACING - 1e-9, 'items keep their spacing');
  }
  const perS = (f.stats.delivered.stone - d0) / 60;
  assert.ok(Math.abs(perS - BELT_PER_S) <= 0.05, `belt delivers ${perS}/s, doc ${BELT_PER_S}/s`);
  // a belt pointing back at the run's end does not take its items
  const st2 = rich(fresh());
  const a = mustPlace(st2, 'belt', 0, 6, 1), b = mustPlace(st2, 'belt', 1, 6, 3);
  giveItem(st2, a, 'stone', 0);
  run(st2, 5);
  assert.equal(a.items.length, 1); assert.equal(b.items.length, 0);
  assert.ok(a.items[0].p < 1 && a.items[0].p > 0.8, 'the item waits at the end');
});

test('inserter: 1 item/s from a belt into the Depot; it waits with the item when the target is full', () => {
  const st = rich(fresh());
  const f = st.flow!;
  const belt = mustPlace(st, 'belt', 7, 10, 3);   // dead-ends west: items queue at its end
  mustPlace(st, 'inserter', 8, 10, 1);             // takes from (7,10), drops into the Depot at (9,10)
  const feed = () => { while (giveItem(st, belt, 'copper', 0)) { /* saturate */ } };
  for (let k = 0; k < 5 * TILE_TPS; k++) { feed(); stepFlow(st, TILE_DT); }
  const d0 = f.stats.delivered.copper;
  for (let k = 0; k < 60 * TILE_TPS; k++) { feed(); stepFlow(st, TILE_DT); }
  const perS = (f.stats.delivered.copper - d0) / 60;
  assert.ok(Math.abs(perS - INSERTER_PER_S) <= 0.02, `inserter moves ${perS}/s, doc ${INSERTER_PER_S}/s`);
  // magazines into a full line buffer: the inserter holds its magazine until the buffer has room
  const st2 = rich(fresh());
  st2.buffer = st2.config.bufferCap;
  const asm = mustPlace(st2, 'assembler', 5, 12, 1);   // 5..7 × 12..14, output tile (8,13)
  const ins = mustPlace(st2, 'inserter', 8, 13, 1);
  asm.out = 3;
  run(st2, 5);
  assert.equal(ins.hold, 'magazine'); assert.equal(ins.phase, 1);
  st2.buffer = 0;
  run(st2, 5);
  assert.equal(st2.flow!.stats.magsDelivered, 3);
  assert.equal(st2.buffer, 3 * SHOT.count, 'ten rounds a magazine into the buffer the ring fills from');
});

test('Shot assembler: 3 s a magazine (20/min) from 2 steel + 1 Cu, out through an inserter into the Depot', () => {
  const st = rich(fresh());
  const f = st.flow!;
  const asm = mustPlace(st, 'assembler', 5, 12, 1);
  mustPlace(st, 'inserter', 8, 13, 1);
  let steelIn = 0, cuIn = 0;
  const feed = () => { while (giveItem(st, asm, 'steel')) steelIn++; while (giveItem(st, asm, 'copper')) cuIn++; };
  feed();
  assert.equal(asm.inv.steel, SHOT.inputs.steel * ASM_INPUT_MULT);
  const stock0 = { ...st.stock };
  for (let k = 0; k < 300 * TILE_TPS; k++) { feed(); stepFlow(st, TILE_DT); }
  assert.equal(f.stats.magsMade, 300 / SHOT.seconds, 'magazines in 300 s');
  assert.equal(st.stats.magsMade, 300 / SHOT.seconds, 'the block sim counts them too');
  assert.ok(f.stats.magsDelivered >= f.stats.magsMade - 2, `delivered ${f.stats.magsDelivered} of ${f.stats.magsMade}`);
  const consumedSteel = steelIn - (asm.inv.steel ?? 0), consumedCu = cuIn - (asm.inv.copper ?? 0);
  const started = f.stats.magsMade + (asm.busy ? 1 : 0);
  assert.equal(consumedSteel, started * SHOT.inputs.steel); assert.equal(consumedCu, started * SHOT.inputs.copper);
  assert.deepEqual(st.stock, stock0, 'a tile assembler never touches the Depot stock directly');
  assert.equal(flowSummary(st).productionMagPerMin, 60 / SHOT.seconds);
  assert.ok(describeMachine(st, asm).startsWith('Shot assembler'));
});

test('a whole line: two Excavators → belt → assembler ← copper; magazines to the Depot at the doc rate', () => {
  const st = rich(fresh());
  const f = st.flow!;
  // steel: two drills on the patch facing north onto a belt along row 6 running east
  mustPlace(st, 'excavator', 1, 7, 0); mustPlace(st, 'excavator', 4, 7, 0);   // outputs (2,6) and (5,6)
  for (let lx = 2; lx < 8; lx++) mustPlace(st, 'belt', lx, 6, 1);
  mustPlace(st, 'inserter', 8, 6, 1);                 // belt (7,6) → assembler tile (9,6)
  const asm = mustPlace(st, 'assembler', 9, 5, 2);   // 9..11 × 5..7, output (10,8)
  mustPlace(st, 'inserter', 10, 8, 2);                // → Depot (10,9)
  // copper: a drill on the copper patch (1..4 × 14..16) facing north, belt north along lx 5 then east along row 5? keep it short: hand-feed copper
  const feedCu = () => { while (giveItem(st, asm, 'copper')) { /* stand-in for the copper line */ } };
  // the ring's consumption stands in as a drain: without it the 4,000-round buffer (400 magazines) fills at minute 19
  for (let k = 0; k < 1200 * TILE_TPS; k++) { feedCu(); if (k % (60 * TILE_TPS) === 0) st.buffer = 0; stepFlow(st, TILE_DT); }
  // two drills give 1 steel/s = 30 magazines a minute of input; the Mk1 assembler is the 10/min limit (D-P4-4: 6 s a magazine)
  const perMin = 60 / SHOT.seconds;
  assert.ok(f.stats.magsMade >= perMin * 20 - 3 && f.stats.magsMade <= perMin * 20, `${f.stats.magsMade} magazines in 20 min`);
  assert.ok(f.stats.magsDelivered >= f.stats.magsMade - 2);
});

test('time: 20 tile ticks a block tick; 1 h at 4× is 72,000 tile ticks; determinism across frame sizes; JSON round trip', () => {
  const build = (st: SimState) => {
    rich(st);
    mustPlace(st, 'excavator', 1, 7, 0);
    for (let lx = 2; lx < 9; lx++) mustPlace(st, 'belt', lx, 6, 1);
    for (let ly = 6; ly < 9; ly++) mustPlace(st, 'belt', 9, ly, 2);
    st.speed = 4;
  };
  const a = fresh(), b = fresh();
  build(a); build(b);
  let ticks = 0;
  for (let k = 0; k < 9000; k++) ticks += advanceFlow(a, 0.1);          // 900 s real at 4×
  for (let k = 0; k < 20000; k++) advanceFlow(b, 0.045);                 // same sim time, other frame sizes
  assert.equal(ticks, 3600 * TILE_TPS); assert.equal(a.flow!.tick, 3600 * TILE_TPS); assert.equal(a.t, 3600);
  assert.equal(b.t, 3600); assert.equal(b.flow!.tick, a.flow!.tick);
  const strip = (st: SimState) => { const j = JSON.parse(JSON.stringify(st)); j.acc = 0; j.events = []; return j; };
  assert.deepEqual(strip(a), strip(b), 'the same tile ticks in other frame sizes give the same state');
  const c = JSON.parse(JSON.stringify(a)) as SimState;
  advanceFlow(a, 1); advanceFlow(c, 1);
  assert.deepEqual(strip(a), strip(c), 'a state survives JSON and continues identically');
  // the block tick is the same `step`: an economy-off block state without a flow layer is untouched by M2
  const p = fresh(), q = fresh();
  p.speed = 1; q.speed = 1;
  advance(p, 100); for (let k = 0; k < 100; k++) step(q);
  assert.deepEqual(strip(p), strip(q));
});

test('placement: Held cells only, streets for belts and inserters, no rubble under anything but an Excavator, costs and refunds', () => {
  const st = rich(fresh());
  const [sx, sy] = st.start;
  const steel0 = st.engineer.inv.steel, cu0 = st.engineer.inv.copper, stock0 = { ...st.stock };
  // a Dark block away from Held ground takes nothing; RI-03 (plan §4.2): a Dark block next to Held ground takes the
  // field kit (a belt, a pole, a turret) and never production (an Assembler)
  const dark = st.blocks.find(b => b.state === DARK && b.y < st.h - 1)!;
  assert.equal(canPlace(st, 'belt', dark.x * CELL_TILES + 10, dark.y * CELL_TILES + 10).reason, 'not next to Held ground');
  assert.equal(canPlace(st, 'assembler', dark.x * CELL_TILES + 10, dark.y * CELL_TILES + 10).reason, 'the block is not Held');
  const front = st.blocks[idxOf(st, sx, sy - 1)];
  assert.equal(front.state, DARK);
  const frontTile = (kind: 'belt' | 'assembler'): string => {
    for (let ly = 0; ly < 12; ly++) for (let lx = 0; lx < 12; lx++) {
      const r = canPlace(st, kind, front.x * CELL_TILES + MARGIN_TILES + lx, front.y * CELL_TILES + MARGIN_TILES + ly).reason;
      if (r !== 'rubble in the way' && r !== 'another machine is there' && r !== 'the substation is there') return r;
    }
    return 'no free lot tile';
  };
  assert.equal(frontTile('belt'), '', 'a belt goes on the front block (field kit)');
  assert.equal(frontTile('assembler'), 'the block is not Held', 'an Assembler does not');
  // the street margin of the start cell
  const street: [number, number] = [sx * CELL_TILES + 1, sy * CELL_TILES + 10];
  assert.equal(canPlace(st, 'belt', ...street).ok, true);
  assert.equal(canPlace(st, 'assembler', ...street).reason, 'not on the street');
  // rubble
  const [px, py] = hq(st, 1, 7);
  assert.ok(rubbleAt(st, px, py));
  assert.equal(canPlace(st, 'belt', px, py).reason, 'rubble in the way');
  assert.equal(canPlace(st, 'excavator', px, py).ok, true);
  // overlap with the Depot
  assert.equal(canPlace(st, 'belt', ...hq(st, 10, 10)).reason, 'another machine is there');
  // M2 (B-M2-pockets): the price comes out of the pockets, never the Depot; a pick-up puts the machine itself back as a stack
  const ex = mustPlace(st, 'excavator', 1, 7, 0);
  assert.equal(st.engineer.inv.steel, steel0 - MACHINE_COST.excavator.steel);
  const ins = mustPlace(st, 'inserter', 8, 10, 1);
  assert.equal(st.engineer.inv.copper, cu0 - MACHINE_COST.inserter.copper);
  assert.deepEqual(st.stock, stock0, 'the Depot stock is untouched by placement');
  assert.ok(rotate(st, ins.x, ins.y)); assert.equal(ins.dir, 2);
  const stacks0 = invStacks(st.engineer.inv);
  assert.equal(canPickUp(st, ex.x + 1, ex.y + 1).stacks, 1, 'an empty Excavator is one stack');
  assert.equal(remove(st, ex.x + 1, ex.y + 1)?.id, ex.id);
  assert.equal(remove(st, ins.x, ins.y)?.id, ins.id);
  assert.equal(st.engineer.inv.excavator, 1); assert.equal(st.engineer.inv.inserter, 1);
  assert.equal(invStacks(st.engineer.inv), stacks0 + 2, 'two machines, two stacks');
  assert.equal(st.engineer.inv.steel, steel0 - MACHINE_COST.excavator.steel - MACHINE_COST.inserter.steel, 'no rubble refund: the machine is the refund');
  assert.equal(remove(st, ...hq(st, 10, 10)), null, 'the Depot cannot be removed');
  // a carried machine goes down free
  const chk = canPlace(st, 'excavator', px, py);
  assert.equal(chk.carried, true); assert.deepEqual(chk.cost, { steel: 0, copper: 0 });
  const steel1 = st.engineer.inv.steel;
  const ex2 = mustPlace(st, 'excavator', 1, 7, 0);
  assert.equal(st.engineer.inv.excavator ?? 0, 0); assert.equal(st.engineer.inv.steel, steel1);
  assert.equal(remove(st, ex2.x, ex2.y)?.id, ex2.id);
  // too poor: the Depot's stock does not count
  st.engineer.inv.steel = 0; delete st.engineer.inv.excavator; st.stock.steel = 10000;
  assert.equal(canPlace(st, 'excavator', px, py).reason, 'not enough in the pockets (10 steel)');
  // a full pocket refuses the pick-up, all or nothing
  st.engineer.inv.steel = 1000;
  const belt = mustPlace(st, 'belt', 8, 12, 1);
  giveItem(st, belt, 'stone', 0);
  st.engineer.inv.stone = 50 * (INV_STACKS - invStacks(st.engineer.inv)) + 1;   // every free stack filled, one unit over
  const full = canPickUp(st, belt.x, belt.y);
  assert.equal(full.ok, false); assert.match(full.reason, /pockets are full/);
  assert.equal(remove(st, belt.x, belt.y), null, 'refused: the belt stays');
  assert.ok(machineAt(st, belt.x, belt.y));
  delete st.engineer.inv.stone;
  assert.deepEqual(canPickUp(st, belt.x, belt.y).items, { belt: 1, stone: 1 });
  assert.ok(remove(st, belt.x, belt.y)); assert.equal(st.engineer.inv.stone, 1);
});

test('the block map stays the judge: a machine on a block that stops being Held stands still', () => {
  const st = rich(fresh());
  const [sx, sy] = st.start;
  const nb = st.blocks.find(b => Math.abs(b.x - sx) + Math.abs(b.y - sy) === 1 && b.state === DARK)!;
  nb.state = HELD; nb.pool = 1000;
  const belt = place(st, 'belt', nb.x * CELL_TILES + 1, nb.y * CELL_TILES + 10, 1)!;   // on its street margin
  assert.ok(belt);
  giveItem(st, belt, 'stone', 0);
  run(st, 1);
  const p1 = belt.items[0].p;
  assert.ok(p1 > 0);
  nb.state = DARK;
  run(st, 1);
  assert.equal(belt.items[0].p, p1, 'no movement on a Dark block');
});

test('hands: mining a unit a second into the pockets within reach, and only then; the chest takes them within reach of the Depot; crafting a magazine in the recipe\'s time (Mk1, 6 s) from the pockets into the pockets', () => {
  const st = fresh();
  ensureFlow(st);
  const [px, py] = hq(st, 1, 7);
  const steel0 = st.stock.steel, patch0 = st.patch.steel;
  // the engineer starts at the workbench, 11+ tiles from the patch: out of reach (D5), the hands refuse
  assert.deepEqual([st.engineer.x, st.engineer.y], [hq(st, 12, 15)[0] + 0.5, hq(st, 12, 15)[1] + 0.5], 'starts at the workbench');
  assert.equal(inReach(st, px, py), false);
  setHandMine(st, [px, py]);
  assert.equal(st.flow!.hand.mine, null, 'out of reach: no mining');
  assert.equal(chestPut(st, 'steel', 1).moved, 0, 'nothing in the pockets');
  st.engineer.x = px - 1.5; st.engineer.y = py + 0.5;   // walk over (on the street west of the patch, 9+ tiles from the Depot) (M1's walk is exercised in walk.test.ts)
  setHandMine(st, [px, py]);
  run(st, 10);
  assert.equal(st.engineer.inv.steel, 10, 'ten units in the pockets'); assert.equal(st.stock.steel, steel0, 'none in the Depot');
  assert.equal(st.patch.steel, patch0 - 10);
  assert.equal(chestPut(st, 'steel', 10).moved, 0, 'the chest needs the engineer within reach of the Depot');
  setHandMine(st, null);
  run(st, 5);
  assert.equal(st.engineer.inv.steel, 10);
  const [dx, dy] = hq(st, 9, 9);
  st.engineer.x = dx - 1.5; st.engineer.y = dy + 0.5;
  assert.equal(chestPut(st, 'steel', 10).moved, 10);
  assert.equal(st.stock.steel, steel0 + 10); assert.equal(st.engineer.inv.steel ?? 0, 0);
  assert.equal(chestTake(st, 'steel', 3).moved, 3); assert.equal(st.stock.steel, steel0 + 7); assert.equal(st.engineer.inv.steel, 3);
  assert.equal(chestPut(st, 'steel', 3).moved, 3);
  // M2 (B-M2-pockets): the workbench crafts from what the engineer carries and hands the magazine back
  const buf0 = st.buffer, made0 = st.stats.magsMade, stock1 = { ...st.stock };
  assert.match(queueCraft(st, 1), /not enough in the pockets/, 'empty pockets: the craft is refused up front');
  assert.equal(chestTake(st, 'steel', 4).moved, 4); assert.equal(chestTake(st, 'copper', 2).moved, 2);
  assert.equal(queueCraft(st, 3), '', 'queue three, the pockets pay for two: capped'); assert.equal(st.flow!.hand.crafts, 2);
  run(st, SHOT.seconds);
  assert.equal(st.flow!.stats.handCrafted, 1, `one magazine in the recipe's ${SHOT.seconds} s`);
  run(st, 1);   // a second into the next craft
  st.engineer.x = px - 1.5; st.engineer.y = py + 0.5;   // walk away mid-craft: it pauses, progress kept
  run(st, 3);
  assert.equal(st.flow!.stats.handCrafted, 1); assert.equal(st.flow!.hand.crafting, true); assert.ok(Math.abs(st.flow!.hand.craftProg - 1) < 1e-6);
  st.engineer.x = dx - 1.5; st.engineer.y = dy + 0.5;
  run(st, SHOT.seconds - 1 + 0.1);
  assert.equal(st.flow!.stats.handCrafted, 2);
  assert.equal(st.engineer.inv.magazine, 2, 'the magazines are in the pockets'); assert.equal(st.engineer.inv.steel ?? 0, 0); assert.equal(st.engineer.inv.copper ?? 0, 0);
  assert.deepEqual(st.stock, { ...stock1, steel: stock1.steel - 4, copper: stock1.copper - 2 }, 'the Depot only gave what the chest handed over');
  assert.equal(st.buffer, buf0, 'nothing went to the line buffer'); assert.equal(st.stats.magsMade, made0 + 2);
  assert.equal(chestPut(st, 'magazine', 2).moved, 2); assert.equal(st.buffer, buf0 + 2 * SHOT.count);
  assert.equal(P_STEEL, 1);
});
