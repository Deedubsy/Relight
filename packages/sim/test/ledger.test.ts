/** RI-01: the resource-conservation check (ledger.ts) — every item's sources and sinks reconcile with what is held,
 *  on a bot-played opening and on a hand-played sequence that touches each transfer and each sink; and the check is
 *  not vacuous: an item that appears from nowhere is reported. */
import { test } from 'node:test';
import assert from 'node:assert/strict';
import {
  DEFAULT_CONFIG, protoCalibrated, createState, citySpec, ensureFlow, SimState,
  createHourBot, runHour, HOUR_CLAIM_AT, conservation, openLedger, heldItems,
  CELL_TILES, canPlace, place, remove, setRecipe, handFeed, chestTake, chestPut, queueCraft, setHandMine, rubbleAt,
  stepFlow, TILE_TPS, TILE_DT, SHOT, ROUNDS_PER_MAG,
} from '../src/index';

function city(seed = 3): SimState {
  const cfg = protoCalibrated({ ...DEFAULT_CONFIG, walk: true });
  Object.assign(cfg, { power: true, supply: 'generators', draw: 'half' });
  const st = createState(citySpec(seed, 'river', cfg), cfg, seed);
  ensureFlow(st);
  return st;
}
function run(st: SimState, seconds: number): void { for (let k = 0; k < seconds * TILE_TPS; k++) stepFlow(st, TILE_DT); }
/** The nearest tile around the engineer (who starts at the HQ workbench) where `kind` can go. */
function spot(st: SimState, kind: Parameters<typeof place>[1]): [number, number] {
  const ex = Math.floor(st.engineer.x), ey = Math.floor(st.engineer.y);
  for (let r = 1; r < CELL_TILES; r++) for (let dy = -r; dy <= r; dy++) for (let dx = -r; dx <= r; dx++)
    if (Math.max(Math.abs(dx), Math.abs(dy)) === r && canPlace(st, kind, ex + dx, ey + dy).ok) return [ex + dx, ey + dy];
  throw new Error(`no room for a ${kind} near the engineer`);
}
const off = (st: SimState) => Object.entries(conservation(st).unexplained).filter(([, v]) => Math.abs(v) > 0.01).map(([k, v]) => `${k} ${v.toFixed(2)}`).join(', ');

test('conservation: the hour bot\'s first twenty minutes reconcile on seed 3 — and an injected item is reported', () => {
  const st = city(3);
  runHour(st, createHourBot(false, 'chest', HOUR_CLAIM_AT.north), 20 * 60);
  const c = conservation(st);
  assert.ok(c.ok, `unexplained: ${c.problems.join(' | ')}`);
  assert.equal(c.openedAt, 0, 'the opening stock is recorded when the flow layer is created');
  assert.ok(c.sources.steel > 0 && c.sources.magazine > 0, 'rubble was mined and magazines made');
  assert.ok(c.sinks.steel > 0 && c.sinks.magazine > 0, 'machines were paid for and rounds fired');
  assert.ok(c.where.machines.magazine > 0 && c.where.chest.coal >= 0, 'the turrets hold rounds');
  st.stock.steel += 5;
  const t = conservation(st);
  assert.ok(!t.ok && t.problems.length === 1 && t.problems[0].startsWith('steel: +5.00'), `five steel from nowhere: ${t.problems.join(' | ')}`);
});

test('conservation: a hand-played sequence — chest, craft, place, feed, pick-up, recipe change, hand mining — reconciles', () => {
  const st = city(3), e = st.engineer;
  // the chest: a transfer, not a source
  assert.equal(chestTake(st, 'steel', 60).moved, 60); assert.equal(chestTake(st, 'copper', 30).moved, 30); assert.equal(chestTake(st, 'magazine', 3).moved, 3);
  assert.equal(off(st), '', 'after the chest');
  // a hand craft: steel + copper → a magazine
  assert.equal(queueCraft(st, 2), ''); run(st, 2 * SHOT.seconds + 1);
  assert.ok((e.inv.magazine ?? 0) >= 5, `crafted: ${e.inv.magazine}`); assert.equal(off(st), '', 'after crafting');
  // a turret, priced from the pockets, fed by hand, then picked up with rounds in it
  const [tx, ty] = spot(st, 'turret'); const tur = place(st, 'turret', tx, ty); assert.ok(tur, 'a turret placed');
  assert.equal(off(st), '', 'after placing');
  const fed = handFeed(st, tx, ty); assert.ok(fed && fed.moved > 0, `fed from the pockets: ${fed?.reason}`); assert.equal(off(st), '', 'after feeding');
  assert.ok(remove(st, tx, ty), 'picked up'); assert.equal(off(st), '', 'after the pick-up (its magazines to the pockets)');
  assert.ok(place(st, 'turret', tx, ty), 'placed again, free (carried)'); assert.equal(off(st), '', 'after re-placing');
  // an assembler and a recipe change
  const [ax, ay] = spot(st, 'assembler'); assert.ok(place(st, 'assembler', ax, ay), 'an assembler placed');
  assert.equal(setRecipe(st, ax, ay, 'wire'), ''); assert.equal(off(st), '', 'after the recipe change');
  // hand mining a rubble tile on the HQ lot: the tile's units become pocket items (a source)
  const ex = Math.floor(e.x), ey = Math.floor(e.y);
  let mine: [number, number] | null = null;
  for (let r = 1; r < CELL_TILES && !mine; r++) for (let dy = -r; dy <= r && !mine; dy++) for (let dx = -r; dx <= r && !mine; dx++)
    if (Math.max(Math.abs(dx), Math.abs(dy)) === r && rubbleAt(st, ex + dx, ey + dy)) mine = [ex + dx, ey + dy];
  assert.ok(mine, 'a rubble tile on the HQ lot');
  e.x = mine![0] + 1.5; e.y = mine![1] + 0.5;   // stand beside it
  const before = heldItems(st).total;
  setHandMine(st, mine); assert.ok(st.flow!.hand.mine, 'mining'); run(st, 30); setHandMine(st, null);
  const after = heldItems(st).total;
  assert.ok(Object.keys(after).some(k => after[k as keyof typeof after] > before[k as keyof typeof before]), 'something was mined');
  assert.equal(off(st), '', 'after hand mining');
  // putting the crafts back
  assert.ok(chestPut(st, 'magazine', 1).moved === 1); assert.equal(off(st), '', 'after the chest put');
  // re-opening the ledger on a state with history: zero from here
  st.flow!.ledger = openLedger(st); st.flow!.ledger.tick = st.flow!.tick;
  run(st, 10); assert.equal(off(st), '', 'after a re-opened ledger');
});

test('conservation: rounds a full line buffer cannot take back are counted lost, not dropped (stats.roundsLost)', () => {
  const st = city(3), e = st.engineer;
  e.inv.steel = 100; e.inv.copper = 50;
  const [tx, ty] = spot(st, 'turret'); const tur = place(st, 'turret', tx, ty); assert.ok(tur, 'a turret placed');
  tur!.inv.rounds = 2 * ROUNDS_PER_MAG + 3; st.buffer = st.config.bufferCap;   // scenario: three loose rounds, the buffer full
  st.flow!.ledger = openLedger(st);   // the scenario set up by hand: the check counts from here
  assert.equal(off(st), '', 'baseline');
  assert.ok(remove(st, tx, ty), 'picked up');
  assert.equal(st.stats.roundsLost, 3, 'the three loose rounds had nowhere to go');
  assert.equal(st.buffer, st.config.bufferCap);
  assert.equal(off(st), '', 'after the pick-up: the loss is a sink');
  const c = conservation(st); assert.ok(c.ok && c.sinks.magazine >= 0.3 - 1e-9, `the lost rounds in the sinks: ${c.sinks.magazine}`);
});
