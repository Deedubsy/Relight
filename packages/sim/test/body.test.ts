/** D-B1-5 direct control: the engineer's body — sprint and the stamina bar, the dodge, the rifle aimed at the cursor.
 *  Stamina is movement only; sprint never drains below one dodge; a shot at empty street costs a round; a shot at an
 *  engaged edge kills what the turrets missed and draws retaliation; §19's danger counter ticks. */
import { test } from 'node:test';
import assert from 'node:assert/strict';
import {
  DEFAULT_CONFIG, createState, citySpec, SimState, ensureFlow, advanceFlow, workbenchTile, ground, hqLot, step, segBetween, cityGeomOf,
  WALK_TILES_PER_S, SPRINT_MULT, SPRINT_S, STAMINA_REFILL_S, DODGE_TILES, DODGE_S, DODGE_COST, DODGE_COOLDOWN_S, RIFLE_RANGE, RIFLE_ROUNDS_PER_S, ROUNDS_PER_MAG,
} from '../src/index';

function city(seed = 3): SimState {
  const cfg = { ...DEFAULT_CONFIG, eco: { ...DEFAULT_CONFIG.eco } };
  const st = createState(citySpec(seed, 'river', cfg), cfg, seed);
  ensureFlow(st);
  return st;
}
const hqIndex = (st: SimState) => st.blocks.findIndex(b => b.x === st.start[0] && b.y === st.start[1]);

test('sprint runs at 1.6× walk, drains the bar to one dodge\'s worth and never below; releasing Shift refills it', () => {
  const st = city(), e = st.engineer;
  const [wx, wy] = workbenchTile(st);
  assert.equal(e.stamina, 1);
  // walk east one second, then sprint east one second: 1.6× the distance
  advanceFlow(st, 1, [{ type: 'walk', dx: 1, dy: 0 }]);
  const walked = e.x - (wx + 0.5);
  assert.ok(Math.abs(walked - WALK_TILES_PER_S) < 0.2, `walked ${walked}`);
  const x1 = e.x;
  advanceFlow(st, 1, [{ type: 'sprint', on: true }]);
  const sprinted = e.x - x1;
  assert.ok(Math.abs(sprinted - WALK_TILES_PER_S * SPRINT_MULT) < 0.3, `sprinted ${sprinted}`);
  assert.ok(Math.abs(e.stamina - (1 - 1 / SPRINT_S)) < 0.05, `stamina ${e.stamina} after 1 s`);
  // keep sprinting (back and forth so the lot does not run out): the bar stops at the dodge floor
  for (let s = 0; s < SPRINT_S * 2; s++) advanceFlow(st, 1, [{ type: 'walk', dx: s % 2 ? 1 : -1, dy: 0 }]);
  assert.ok(Math.abs(e.stamina - DODGE_COST) < 1e-9, `floor ${e.stamina}`);
  // at the floor, Shift held is a walk (no sprint speed, no refill)
  const x2 = e.x; advanceFlow(st, 1, [{ type: 'walk', dx: 1, dy: 0 }]);
  assert.ok(Math.abs(e.x - x2 - WALK_TILES_PER_S) < 0.2, 'walk speed at the floor');
  assert.ok(Math.abs(e.stamina - DODGE_COST) < 1e-9, 'no refill with Shift held');
  // one dodge is still available after a full sprint
  advanceFlow(st, 0.05, [{ type: 'sprint', on: false }, { type: 'walk', dx: 0, dy: 0 }, { type: 'dodge' }]);
  assert.ok(e.dash > 0 || e.dashCooldown > 0, 'the dodge fired');
  assert.ok(e.stamina < DODGE_COST, `dodge paid ${e.stamina}`);
  // released: the bar refills in ~6 s from empty
  advanceFlow(st, STAMINA_REFILL_S + 0.1);
  assert.ok(e.stamina > 0.95, `refilled to ${e.stamina}`);
  advanceFlow(st, 1);
  assert.equal(e.stamina, 1);
});

test('the dodge is 3 tiles in 0.25 s in the movement direction (or the facing), on a 1 s cooldown, costing a quarter of the bar', () => {
  const st = city(), e = st.engineer;
  const [wx, wy] = workbenchTile(st);
  const y0 = e.y;
  // facing east from a walk, then keys released: the dodge goes the way the engineer faces
  advanceFlow(st, 0.5, [{ type: 'walk', dx: 1, dy: 0 }]);
  advanceFlow(st, 0.05, [{ type: 'walk', dx: 0, dy: 0 }]);
  const x0 = e.x;
  advanceFlow(st, 0.05, [{ type: 'dodge' }]);
  assert.ok(e.dash > 0, 'dashing');
  advanceFlow(st, DODGE_S);
  assert.equal(e.dash, 0);
  assert.ok(Math.abs(e.x - x0 - DODGE_TILES) < 0.3, `dashed ${e.x - x0} tiles east`);
  assert.equal(e.y, y0);
  assert.ok(Math.abs(e.stamina - (1 - DODGE_COST)) < 0.1, `stamina ${e.stamina}`);
  // a second Space inside the cooldown does nothing
  const x1 = e.x;
  advanceFlow(st, 0.05, [{ type: 'dodge' }]);
  assert.equal(e.dash, 0, 'cooldown');
  advanceFlow(st, DODGE_COOLDOWN_S);
  advanceFlow(st, 0.05, [{ type: 'dodge' }]);
  assert.ok(e.dash > 0, 'after the cooldown a dodge fires');
  advanceFlow(st, DODGE_S);
  assert.ok(e.x - x1 > DODGE_TILES - 0.5, `second dodge moved ${e.x - x1}`);
  assert.ok(wx >= 0 && wy >= 0);
});

test('a walk-here target survives a key release and is cancelled by any WASD input; a dodge cancels it too', () => {
  const st = city(), e = st.engineer;
  const [wx, wy] = workbenchTile(st);
  advanceFlow(st, 0.05, [{ type: 'move', x: wx + 15.5, y: wy + 0.5 }]);
  assert.ok(e.target, 'target set');
  advanceFlow(st, 0.05, [{ type: 'walk', dx: 0, dy: 0 }]);
  assert.ok(e.target, 'a (0,0) walk (keys released) keeps the target');
  advanceFlow(st, 0.05, [{ type: 'walk', dx: 0, dy: 1 }]);
  assert.equal(e.target, null, 'a held key cancels it');
  advanceFlow(st, 0.05, [{ type: 'walk', dx: 0, dy: 0 }, { type: 'move', x: wx + 15.5, y: wy + 0.5 }]);
  advanceFlow(st, 0.05, [{ type: 'dodge' }]);
  assert.equal(e.target, null, 'a dodge cancels it');
});

test('the rifle aimed at empty street spends a round a shot and hits nothing; aimed at an engaged edge it kills what the turrets missed, draws retaliation and counts as danger', () => {
  const st = city(), e = st.engineer;
  e.inv.magazine = 2;
  const [wx, wy] = workbenchTile(st);
  // empty street: aim east at a point with no engagement anywhere
  advanceFlow(st, 2, [{ type: 'aim', at: [wx + 8, wy + 0.5] }]);
  const rounds = Math.round(e.fired);
  assert.ok(rounds >= Math.floor(2 * RIFLE_ROUNDS_PER_S) && rounds <= Math.ceil(2 * RIFLE_ROUNDS_PER_S) + 1, `fired ${rounds} rounds in 2 s`);
  assert.ok(Math.abs(e.inv.magazine - (2 - rounds / ROUNDS_PER_MAG)) < 1e-9, 'each round came out of the pockets');
  assert.equal(e.kills, 0); assert.equal(e.hurt, 0); assert.equal(e.danger, 0);
  advanceFlow(st, 0.05, [{ type: 'aim', at: null }]);
  // an engaged HQ edge whose turrets are dry (a stand-in edge would be ring-fed and leave the rifle nothing to hit):
  // stand within range of its street and hold the mouse on it
  const hq = hqIndex(st), cg = cityGeomOf(st), G = ground(st);
  st.buffer = 0;
  const edge = st.ring.find(ed => ed.a === hq && ed.turrets !== undefined)!;
  const sg = segBetween(cg, edge.a, edge.b)!;
  for (const m of st.flow!.machines) if (m.kind === 'turret') m.inv.rounds = 0;
  // stand on the HQ lot tile nearest the ridge midpoint
  let best = -1, bd = Infinity;
  for (const t of cg.blocks[hq].tiles) { const tx = t % G.tw, ty = Math.floor(t / G.tw), d = Math.hypot(tx - sg.mx, ty - sg.my); if (d < bd && !st.flow!.occ[t]) { bd = d; best = t; } }
  e.x = best % G.tw + 0.5; e.y = Math.floor(best / G.tw) + 0.5; e.block = hq;
  assert.ok(bd <= RIFLE_RANGE, `ridge ${bd} tiles away`);
  e.inv.magazine = 5; e.fired = 0;
  const before = e.hp;
  for (let s = 0; s < 4; s++) {
    st.engagements.push({ id: edge.id, cr: 3, sh: 0, rcr: 1, rsh: 0 });
    advanceFlow(st, 1, [{ type: 'aim', at: [sg.mx + 0.5, sg.my + 0.5] }]);
  }
  assert.ok(e.kills > 0.5, `kills ${e.kills}`);
  assert.ok(e.hp < before, 'retaliation landed');
  assert.ok(e.danger >= 1 && e.dangerShot >= 1, `danger ${e.danger} / ${e.dangerShot}`);
  assert.ok(e.shootS >= 2, `shooting seconds ${e.shootS}`);
  assert.ok(hqLot(st, 0, 0)[0] >= 0);
  void step;
});
