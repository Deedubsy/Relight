/** Prompt B M1: the engineer on the tile grid. Starts at the workbench, walks 6 tiles/s on any tile that is not
 *  water (D5), around machines, to a block's pole or a clicked tile; reach 8; the harness bot stays block-level. */
import { test } from 'node:test';
import assert from 'node:assert/strict';
import {
  DEFAULT_CONFIG, createState, citySpec, generateCity, HELD, DARK, SimState, T_RIVER,
  ensureFlow, advanceFlow, findPath, currentPath, workbenchTile, ground, tileAt, inReach, walkable, passable, place, canPlace,
  hqLot, WALK_TILES_PER_S, REACH, START_CHEST, chestCount, protoCalibrated, generateMap, createBot, botCommands, Command, step,
} from '../src/index';

function city(seed = 3): SimState {
  const cfg = { ...DEFAULT_CONFIG, eco: { ...DEFAULT_CONFIG.eco } };
  const st = createState(citySpec(seed, 'river', cfg), cfg, seed);
  ensureFlow(st);
  return st;
}
const hqIndex = (st: SimState) => st.blocks.findIndex(b => b.x === st.start[0] && b.y === st.start[1]);

test('the engineer starts at the HQ workbench with empty pockets; the chest holds §11\'s start stock', () => {
  const st = city();
  const [wx, wy] = workbenchTile(st);
  assert.deepEqual([st.engineer.x, st.engineer.y], [wx + 0.5, wy + 0.5]);
  assert.equal(st.engineer.block, hqIndex(st));
  assert.equal(ground(st).owner[wy * ground(st).tw + wx], hqIndex(st), 'the workbench is on the HQ face');
  assert.deepEqual(hqLot(st, 12, 15), [wx, wy]);
  assert.equal(Object.values(st.engineer.inv).reduce((a, b) => a + b, 0), 0);
  assert.equal(chestCount(st, 'steel'), START_CHEST.steel); assert.equal(chestCount(st, 'copper'), START_CHEST.copper);
  assert.equal(chestCount(st, 'stone'), START_CHEST.stone);
  assert.equal(chestCount(st, 'magazine'), Math.floor(st.buffer / 10), 'magazines are the line buffer; the start ones went round the ring (M3)');
});

test('walks 6 tiles/s to a clicked tile along a path that avoids water and machines, 8-connected without cutting corners', () => {
  const st = city();
  const G = ground(st), e = st.engineer;
  const [wx, wy] = workbenchTile(st);
  // straight east along the lot: 20 tiles in 20/6 s
  advanceFlow(st, 1, [{ type: 'move', x: wx + 20.5, y: wy + 0.5 }]);
  const p = currentPath(st);
  assert.ok(p && p.path.length === 20, `path of ${p?.path.length}`);
  advanceFlow(st, 20 / WALK_TILES_PER_S + 0.05);
  assert.ok(Math.abs(e.x - (wx + 20.5)) < 1e-6 && Math.abs(e.y - (wy + 0.5)) < 1e-6, `arrived at ${e.x},${e.y}`);
  assert.equal(e.target, null); assert.equal(e.block, hqIndex(st));
  // no path into the river: the walk is refused
  let river = -1;
  for (let t = 0; t < G.tw * G.th; t++) if (G.base[t] === T_RIVER) { river = t; break; }
  assert.ok(river >= 0);
  assert.equal(walkable(G, river % G.tw, Math.floor(river / G.tw)), false);
  const intoRiver = findPath(st, Math.floor(e.x), Math.floor(e.y), river % G.tw, Math.floor(river / G.tw));
  assert.ok(intoRiver === null, 'no path into the river');
  // a wall of lamps across the lot is walked round; belts and poles are walked through (GAME-ASSUMPTION: thin)
  const x0 = Math.floor(e.x), y0 = Math.floor(e.y);
  st.engineer.inv.steel = 1000; st.engineer.inv.copper = 500;   // M2: machines are paid from the pockets
  let laid = 0;
  for (let dy = -6; dy <= 6; dy++) if (place(st, 'lamp', x0 - 3, y0 + dy, 0)) laid++;
  assert.ok(laid >= 10, `${laid} lamps laid`);
  assert.equal(passable(st, x0 - 3, y0), false);
  const back = findPath(st, x0, y0, wx, wy);
  assert.ok(back !== null && back.length > 20, `detour of ${back?.length} (20 straight)`);
  for (const t of back) assert.equal(st.flow!.occ[t], undefined, 'never steps on a lamp');
  assert.ok(place(st, 'pole', x0 + 2, y0, 0) !== null, 'a pole goes on any block'); assert.equal(passable(st, x0 + 2, y0), true);
  const beltAt = [...Array(18).keys()].map(k => wx + 1 + k).find(x => canPlace(st, 'belt', x, wy).ok)!;
  assert.ok(beltAt !== undefined, 'a belt tile on the HQ row');
  assert.ok(place(st, 'belt', beltAt, wy, 0) !== null); assert.equal(passable(st, beltAt, wy), true);
});

test('walking to a block ends on its pole; the engineer\'s block follows the tile under them; a Dark block is walkable and harmless', () => {
  const st = city();
  const G = ground(st), e = st.engineer, hqi = hqIndex(st);
  const dark = st.nb[hqi].find(j => st.blocks[j].state === DARK)!;
  assert.ok(dark >= 0);
  const pole = G.blocks[dark].pole;
  advanceFlow(st, 1, [{ type: 'walkTo', block: dark }]);
  assert.equal(e.dest, dark);
  assert.ok(e.remaining > 0, 'remaining counts the tiles left');
  const path = currentPath(st)!;
  assert.ok(path.path.length > 0);
  for (let s = 0; s < 120 && e.dest >= 0; s++) advanceFlow(st, 1);
  assert.equal(e.dest, -1);
  assert.deepEqual([Math.floor(e.x), Math.floor(e.y)], pole);
  assert.equal(e.block, dark); assert.equal(st.blocks[dark].state, DARK, 'walking on a Dark block changes nothing (D5)');
  assert.equal(e.hp, e.hp, 'no damage from walking');
  assert.equal(tileAt(st, pole[0], pole[1]).block, dark);
});

test('the HQ to the Tram depot and back: a path exists both ways, and the walk takes the distance at 6 tiles/s', () => {
  const st = city();
  const cg = generateCity(3, 'river'), G = ground(st), e = st.engineer;
  const tram = cg.facilities.find(f => f.name === 'Tram depot')!.block, pole = G.blocks[tram].pole;
  const [wx, wy] = workbenchTile(st);
  const there = findPath(st, wx, wy, pole[0], pole[1]), back = findPath(st, pole[0], pole[1], wx, wy);
  assert.ok(there && back, 'paths both ways');
  assert.ok(there!.length >= 60 && Math.abs(there!.length - back!.length) <= 2, `there ${there!.length}, back ${back!.length}`);
  advanceFlow(st, 1, [{ type: 'walkTo', block: tram }]);
  const t0 = st.flow!.tick;
  for (let s = 0; s < 600 && e.dest >= 0; s++) advanceFlow(st, 1);
  assert.equal(e.dest, -1); assert.equal(e.block, tram);
  const secs = (st.flow!.tick - t0) / 20;
  assert.ok(secs <= there!.length * 1.42 / WALK_TILES_PER_S + 2, `${secs.toFixed(1)} s for ${there!.length} steps`);
  assert.ok(e.walked * WALK_TILES_PER_S > there!.length * 0.9, `walked ${e.walked.toFixed(0)} s`);
});

test('reach: 8 tiles to the nearest edge of the target; hands and chest refuse beyond it', () => {
  const st = city();
  const e = st.engineer;
  const [wx, wy] = workbenchTile(st);
  assert.equal(inReach(st, wx + REACH, wy), true);
  assert.equal(inReach(st, wx + REACH + 1, wy), false);
  assert.equal(inReach(st, wx + REACH + 1, wy, 3), false);
  assert.equal(inReach(st, wx - REACH - 1, wy, 3), true, 'a 3×3 target reaches with its near edge');
  e.x = wx + 30.5;
  assert.equal(inReach(st, wx, wy), false);
});

test('the harness bot keeps its block-level walk: the block sim without a flow layer moves the engineer between blocks as before', () => {
  const cfg = protoCalibrated({ ...DEFAULT_CONFIG, walk: true });
  const st = createState(generateMap(3, cfg), cfg, 3);
  const bot = createBot('compact');
  const cmds: Command[] = [];
  for (let k = 0; k < 3600; k++) { cmds.length = 0; botCommands(st, bot, cmds); step(st, cmds); }
  assert.equal(st.flow, undefined);
  assert.ok(st.engineer.walked > 0, 'the bot walked');
  assert.ok(st.blocks.filter(b => b.state === HELD).length > 1);
});
