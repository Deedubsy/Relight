import { test } from 'node:test';
import assert from 'node:assert/strict';
import { createCampaign, ensureFlow, applyCommands, actionResult, canPlace, inReach, machineAt, stateHash,
  makeSave, loadState, conservation, extendBuildPath, pathEdits, advanceFlow, HISTORY_LIMIT,
  type SimState, type Command, type Kind, type TilePoint } from '../src/index';
import { createSession, parseUrl, dispatch, queue, frame, replaySession, makeSessionSave } from '../../game/src/session';
import { buildCatalogue, toolForKey } from '../../game/src/buildCatalogue';
import { BINDINGS, bound } from '../../game/src/controls';

function send(st: SimState, c: Command, ok = true): void {
  applyCommands(st, [c]); assert.equal(actionResult(st).ok, ok, actionResult(st).reason);
}
function fresh(): SimState {
  const st = createCampaign(3); ensureFlow(st);
  send(st, { type: 'factory', action: { type: 'chestTake', item: 'steel', n: 180 } });
  send(st, { type: 'factory', action: { type: 'chestTake', item: 'copper', n: 90 } });
  return st;
}
function spot(st: SimState, kind: Kind = 'belt', width = 1, height = 1): TilePoint {
  const e = st.engineer;
  for (let y = Math.floor(e.y) - 7; y <= e.y + 7; y++) for (let x = Math.floor(e.x) - 7; x <= e.x + 7; x++) {
    let valid = true;
    for (let dy = 0; dy < height; dy++) for (let dx = 0; dx < width; dx++) if (!canPlace(st, kind, x + dx, y + dy).ok || !inReach(st, x + dx, y + dy, kind === 'belt' ? 1 : 3)) valid = false;
    if (valid) return { x, y };
  }
  throw new Error(`No reachable ${kind} footprint`);
}
const build = (item: Kind, p: TilePoint): Command => ({ type: 'construct', edits: [{ action: 'place', item, ...p, dir: 1 }] });
const conserved = (st: SimState) => assert.ok(conservation(st).ok, conservation(st).problems.join(', '));

test('construction: continuous corner path is one paid group; undo and redo conserve and keep simulation time', () => {
  const st = fresh(), p = spot(st, 'belt', 3, 3), steel = st.engineer.inv.steel;
  let path = extendBuildPath([], p); path = extendBuildPath(path, { x: p.x + 2, y: p.y + 2 });
  assert.equal(path.length, 5);
  assert.deepEqual(pathEdits(path, 0).map(e => e.action === 'place' ? e.dir : -1), [1, 1, 2, 2, 2]);
  send(st, { type: 'buildPath', path, dir: 0 });
  assert.equal(st.engineer.inv.steel, steel - 5); assert.equal(st.construction!.undo.length, 1);
  st.speed = 1; advanceFlow(st, 1, []); const time = st.flow!.tick;
  send(st, { type: 'undoBuild' }); assert.equal(st.flow!.tick, time); assert.equal(st.engineer.inv.belt, 5);
  send(st, { type: 'redoBuild' }); assert.equal(st.engineer.inv.belt ?? 0, 0); assert.equal(st.engineer.inv.steel, steel - 5);
  assert.ok(path.every(p => machineAt(st, p.x, p.y)?.kind === 'belt')); conserved(st);
});

test('construction: invalid, partially obstructed, unaffordable and out-of-reach groups have no partial effects', () => {
  const st = fresh(), p = spot(st, 'belt', 3, 2);
  send(st, build('belt', { x: p.x + 2, y: p.y }));
  const before = stateHash(st);
  send(st, { type: 'buildPath', path: [{ ...p }, { x: p.x + 1, y: p.y }, { x: p.x + 2, y: p.y }], dir: 1 }, false);
  assert.equal(stateHash(st), before);
  for (const path of [[p, p], [p, { x: p.x + 2, y: p.y }], [{ x: NaN, y: p.y }]]) {
    send(st, { type: 'buildPath', path, dir: 1 }, false); assert.equal(stateHash(st), before);
  }
  send(st, build('belt', { x: p.x + 100, y: p.y }), false); assert.equal(stateHash(st), before);
  const poor = createCampaign(3); ensureFlow(poor); const poorHash = stateHash(poor);
  send(poor, build('belt', p), false); assert.equal(stateHash(poor), poorHash);
  assert.deepEqual(extendBuildPath([p, { x: p.x + 1, y: p.y }], p), [p]);
});

test('construction: removal history restores settings but leaves recovered contents in pockets; earlier undo still works', () => {
  const st = fresh(), p = spot(st, 'assembler'); send(st, build('assembler', p));
  send(st, { type: 'factory', action: { type: 'setRecipe', ...p, recipe: 'wire' } });
  const original = machineAt(st, p.x, p.y)!;
  send(st, { type: 'construct', edits: [{ action: 'pickUp', ...p }] });
  send(st, { type: 'undoBuild' });
  assert.equal(machineAt(st, p.x, p.y)!.recipe, 'wire'); assert.equal(machineAt(st, p.x, p.y)!.id, original.id);
  send(st, { type: 'undoBuild' }, false); // the earlier placement had Shot: intervening recipe edit is protected
  conserved(st);
  const clean = fresh(), q = spot(clean); send(clean, build('belt', q));
  send(clean, { type: 'construct', edits: [{ action: 'pickUp', ...q }] });
  send(clean, { type: 'undoBuild' }); send(clean, { type: 'undoBuild' });
  assert.equal(machineAt(clean, q.x, q.y), undefined); conserved(clean);
  send(clean, { type: 'redoBuild' }); send(clean, { type: 'redoBuild' });
  assert.equal(machineAt(clean, q.x, q.y), undefined);
  send(clean, { type: 'undoBuild' });
  send(clean, { type: 'construct', edits: [{ action: 'rotate', ...q }] });
  const direction = machineAt(clean, q.x, q.y)!.dir;
  send(clean, { type: 'undoBuild' }); send(clean, { type: 'undoBuild' });
  send(clean, { type: 'redoBuild' }); send(clean, { type: 'redoBuild' });
  assert.equal(machineAt(clean, q.x, q.y)!.dir, direction); conserved(clean);
});

test('construction: current contents, damage, full pockets, occupied undo sites and down state are respected', () => {
  const st = fresh(), p = spot(st, 'chest'); send(st, build('chest', p));
  send(st, { type: 'factory', action: { type: 'chestPut', ...p, item: 'steel', n: 7 } });
  send(st, { type: 'undoBuild' }); assert.equal(st.engineer.inv.steel, 170);
  send(st, { type: 'redoBuild' }); assert.deepEqual(machineAt(st, p.x, p.y)!.inv, {}); conserved(st);
  const q = spot(st); send(st, build('wall', q)); machineAt(st, q.x, q.y)!.hp = 1; // labelled damage scenario
  const damaged = stateHash(st); send(st, { type: 'undoBuild' }, false); assert.equal(stateHash(st), damaged);
  machineAt(st, q.x, q.y)!.hp = 120;
  st.engineer.inv = { belt: 40000 }; // labelled full-pocket refusal, no conservation claim for injected stock
  const full = stateHash(st); send(st, { type: 'undoBuild' }, false); assert.equal(stateHash(st), full);
  st.engineer.down = 1; send(st, { type: 'undoBuild' }, false);
  const occupied = fresh(), at = spot(occupied); send(occupied, build('belt', at));
  send(occupied, { type: 'construct', edits: [{ action: 'pickUp', ...at }] });
  applyCommands(occupied, [{ type: 'place', item: 'belt', ...at, dir: 0 }]); // ordinary untracked intervening command
  const hash = stateHash(occupied); send(occupied, { type: 'undoBuild' }, false); assert.equal(stateHash(occupied), hash);
});

test('construction: rotation, bounded history, new-branch redo clearing, save validation and continuation', () => {
  const st = fresh(), p = spot(st); send(st, build('belt', p));
  for (let n = 0; n < HISTORY_LIMIT + 2; n++) send(st, { type: 'construct', edits: [{ action: 'rotate', ...p }] });
  assert.equal(st.construction!.undo.length, HISTORY_LIMIT);
  const dir = machineAt(st, p.x, p.y)!.dir;
  send(st, { type: 'undoBuild' }); assert.notEqual(machineAt(st, p.x, p.y)!.dir, dir);
  const loaded = loadState(makeSave(st)); assert.equal(stateHash(st), stateHash(loaded));
  send(st, { type: 'redoBuild' }); send(loaded, { type: 'redoBuild' }); assert.equal(stateHash(st), stateHash(loaded));
  send(st, { type: 'undoBuild' }); send(st, build('belt', spot(st))); assert.equal(st.construction!.redo.length, 0);
  const bad = structuredClone(st); bad.construction!.undo[0][0].dir = 9 as 0; assert.throws(() => loadState(bad), /construction/);
  assert.equal(createCampaign(3).construction, undefined); conserved(st);
});

test('construction: paused UI dispatch flushes pending inputs once and a saved command stream replays exactly', () => {
  Object.assign(globalThis, { location: { href: 'http://localhost/?rules=exploration-v2' } });
  const session = createSession(parseUrl('?rules=exploration-v2&seed=3&view=world'));
  queue(session, { type: 'walk', dx: 0, dy: 0 });
  const r = dispatch(session, { type: 'factory', action: { type: 'chestTake', item: 'steel', n: 20 } }); assert.ok(r.ok);
  assert.equal(session.pending.length, 0); assert.equal(session.log.length, 2);
  const p = spot(session.state); assert.ok(dispatch(session, build('belt', p)).ok);
  assert.ok(dispatch(session, { type: 'undoBuild' }).ok); assert.ok(dispatch(session, { type: 'redoBuild' }).ok);
  const paused = replaySession(session, { rifleOff: false }); assert.ok('state' in paused);
  assert.equal(stateHash(paused.state), stateHash(session.state), 'final-tick commands replay without advancing a tick');
  queue(session, { type: 'setSpeed', mult: 1 }); frame(session, 0.2);
  const replayed = replaySession(session, { rifleOff: false }); assert.ok('state' in replayed);
  assert.equal(stateHash(replayed.state), stateHash(session.state));
  assert.equal(stateHash(loadState(makeSessionSave(session))), stateHash(session.state)); conserved(session.state);
  dispatch(session, { type: 'undoBuild' });
  const boundary = replaySession(session, { rifleOff: false }); assert.ok('state' in boundary);
  assert.equal(stateHash(boundary.state), stateHash(session.state), 'commands at a nonzero final tick are included too');
});

test('construction: packing refuses an unfinished recipe, fractional stock or loose-round overflow atomically', () => {
  const st = fresh(), p = spot(st, 'assembler'); send(st, build('assembler', p));
  const m = machineAt(st, p.x, p.y)!;
  m.busy = true; // labelled in-progress recipe; no stock injection counted as production evidence
  let before = stateHash(st); send(st, { type: 'undoBuild' }, false); assert.equal(stateHash(st), before);
  m.busy = false; m.inv.copper = 0.5; // older fractional-content compatibility fixture
  before = stateHash(st); send(st, { type: 'undoBuild' }, false); assert.equal(stateHash(st), before);
  const gun = fresh(), q = spot(gun, 'turret'); send(gun, build('turret', q));
  machineAt(gun, q.x, q.y)!.inv.rounds = 3; gun.buffer = gun.config.bufferCap; // full-buffer refusal fixture
  before = stateHash(gun); send(gun, { type: 'undoBuild' }, false); assert.equal(stateHash(gun), before);
});

test('construction controls: build catalogue and hotkeys share references; modified history keys do not collide with tools', () => {
  for (const entry of buildCatalogue(true)) { assert.ok(entry.what); if (entry.key) assert.equal(toolForKey(entry.key), entry.kind); }
  assert.equal(toolForKey('9'), 'rifle'); assert.ok(bound('undo', 'Z')); assert.ok(bound('redo', 'y'));
  assert.equal(new Set(buildCatalogue(true).filter(b => b.key).map(b => b.key)).size, buildCatalogue(true).filter(b => b.key).length);
  assert.equal(BINDINGS.pockets[1], 'Tab');
});
