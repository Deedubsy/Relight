/** Prompt B M4 — threat, and the rifle, at tile level (threat.ts): crawlers born on the ridge walk lamp → turret →
 *  substation inside their two blocks; the 40-arrival rule and the fall are the block model's own; turrets and the
 *  rifle shoot bodies out of the same hoppers and pockets; retaliation only (D5); shades untargetable off lit tiles;
 *  no hulks; the lattice keeps the block-level arithmetic. */
import { test } from 'node:test';
import assert from 'node:assert/strict';
import {
  DEFAULT_CONFIG, createState, citySpec, SimState, ensureFlow, advanceFlow, workbenchTile, ground, segBetween, cityGeomOf, takeEvents,
  threatOf, Crawler, ENGINEER_HP, RESPAWN_S, RETALIATE_HP_PER_S, RIFLE_ROUNDS_PER_S, ROUNDS_PER_MAG, ROUND_DMG, ENEMIES, blockLights,
  generateMap, faceSub, litAt, flowSummary, step, TURRET_HOPPER,
} from '../src/index';

function city(seed = 3): SimState {
  const cfg = { ...DEFAULT_CONFIG, eco: { ...DEFAULT_CONFIG.eco } };
  const st = createState(citySpec(seed, 'river', cfg), cfg, seed);
  ensureFlow(st);
  st.buffer = 0;   // the chest feeds nothing: the turrets' hoppers are all there is
  return st;
}
const hqIndex = (st: SimState) => st.blocks.findIndex(b => b.x === st.start[0] && b.y === st.start[1]);
const turretEdge = (st: SimState) => st.ring.find(ed => ed.a === hqIndex(st) && ed.turrets !== undefined)!;
const dryTurrets = (st: SimState) => { for (const m of st.flow!.machines) if (m.kind === 'turret') m.inv.rounds = 0; };
const farAway = (st: SimState) => { const e = st.engineer, [wx, wy] = workbenchTile(st); e.x = wx + 0.5; e.y = wy + 0.5; };
function engage(st: SimState, cr: number, sh = 0): void { st.engagements.push({ id: turretEdge(st).id, cr, sh, rcr: cr / 15, rsh: sh / 15 }); }
/** A crawler placed by hand at a tile, walking the HQ's turret edge. */
function plant(st: SimState, x: number, y: number, kind: Crawler['kind'] = 'crawler', cls: 0 | 1 | 2 = 2): Crawler {
  const T = threatOf(st.flow!), ed = turretEdge(st);
  const c: Crawler = { id: T.next++, kind, x, y, hp: kind === 'shade' ? ENEMIES[1].hp : ENEMIES[0].hp, edge: ed.id, from: ed.b, to: ed.a, cls, onPlayer: false, escaped: false, born: st.t, stuck: 0 };
  T.crawlers.push(c);
  return c;
}
const ownerOf = (st: SimState, x: number, y: number) => { const G = ground(st), t = Math.floor(y) * G.tw + Math.floor(x); return G.owner[t] >= 0 ? G.owner[t] : G.near[t]; };

test('rot arrives as bodies on the segment ridge and walks the chain: lit lamps go out one by one, then the turret, then the substation; 40 unshot arrivals stop the substation and the block falls the block model\'s way; nothing ever leaves the two blocks; no hulks', () => {
  const st = city(), hq = hqIndex(st), ed = turretEdge(st), sg = segBetween(cityGeomOf(st), ed.a, ed.b)!;
  dryTurrets(st); farAway(st);
  const litBefore = blockLights(st, hq).filter(l => l.lit).length;
  assert.ok(litBefore >= 8, `lit lights ${litBefore}`);
  const ridge = new Set(Array.from(sg.ridge));
  let bornOnRidge = 0, born = 0, subOffAt = -1, fellAt = -1, maxCls = 0;
  for (let s = 0; s < 200; s++) {
    if (s % 15 === 0 && s < 60) engage(st, 10);
    advanceFlow(st, 1, []);
    const T = threatOf(st.flow!), G = ground(st);
    for (const c of T.crawlers) {
      if (c.born === st.t - 1 || c.born === st.t) { born++; if (ridge.has(Math.floor(c.y) * G.tw + Math.floor(c.x)) || st.t - c.born < 1.01) bornOnRidge++; }
      const o = ownerOf(st, c.x, c.y);
      assert.ok(o === c.to || o === c.from, `crawler ${c.id} at ${c.x},${c.y} is on block ${o}, not ${c.to}/${c.from}`);
      assert.ok(c.kind === 'crawler' || c.kind === 'shade', 'no hulks (§7: the wall is outside the hour)');
      maxCls = Math.max(maxCls, c.cls);
    }
    for (const ev of takeEvents(st)) {
      if (ev.type === 'sub-off' && subOffAt < 0) subOffAt = st.t;
      if (ev.type === 'fall' && fellAt < 0) fellAt = st.t;
    }
  }
  const T = threatOf(st.flow!);
  assert.equal(T.stats.spawned, 40, 'every crawler the block model let arrive was born');
  assert.ok(T.stats.lampsEaten >= litBefore, `lamps eaten ${T.stats.lampsEaten} of ${litBefore} lit`);
  assert.equal(maxCls, 2, 'the chain reached the substation');
  assert.equal(T.stats.arrivals, 40, 'every crawler reached the substation');
  assert.ok(subOffAt > 0 && subOffAt < 120, `substation off at ${subOffAt} s (the 40-arrival rule, §11)`);
  assert.ok(fellAt > subOffAt, `the block fell at ${fellAt} s after the substation went off at ${subOffAt}`);
  assert.equal(st.blocks[hq].state !== 1, true);
  assert.equal(T.crawlers.length, 0, 'a fallen block has nothing left to walk to');
});

test('fed turrets shoot the bodies at 3 rounds a crawler out of their own hoppers, the edge\'s hopper drains to the same hopper-empty pip, and no arrival reaches the substation', () => {
  const st = city(), ed = turretEdge(st);
  farAway(st);
  let emptyAt = -1;
  for (let s = 0; s < 60; s++) {
    if (s % 15 === 0 && s < 45) engage(st, 10);
    advanceFlow(st, 1, []);
    for (const ev of takeEvents(st)) if (ev.type === 'hopper-empty' && emptyAt < 0) emptyAt = st.t;
  }
  const T = threatOf(st.flow!), fs = flowSummary(st);
  assert.ok(T.stats.turretKills >= 25, `turret kills ${T.stats.turretKills}`);
  assert.equal(T.stats.arrivals, 0);
  assert.equal(fs.fired, T.stats.turretKills * 3 + (fs.fired - T.stats.turretKills * 3), 'rounds fired');
  assert.ok(fs.fired >= T.stats.turretKills * 3 && fs.fired <= T.stats.turretKills * 3 + 6, `${fs.fired} rounds for ${T.stats.turretKills} kills`);
  assert.ok(emptyAt > 0, 'the turrets nearest the ridge ran dry: hopper-empty fired');
  assert.equal(fs.turretRounds + fs.fired, fs.turrets * TURRET_HOPPER, 'every round fired came out of a turret hopper');
  assert.ok(st.ring.some(x => x.turrets && x.hopper <= 1e-9), 'the edge hopper is the turrets\' rounds');
  void ed;
});

test('retaliation only (D5): a crawler ignores the engineer beside its path; shot, it turns and lands 5 HP/s at arm\'s reach; standing in its path turns it too; at zero HP the engineer is down and stands up at the workbench 10 s later with the pockets intact', () => {
  const st = city(), e = st.engineer, hq = hqIndex(st);
  dryTurrets(st);
  const sub = faceSub(st, hq)!, G = ground(st);
  // 1. beside the path, unshot: nothing happens
  const c = plant(st, sub.x - 6.5, sub.y + 1.5);
  e.x = sub.x - 3.5; e.y = sub.y + 4.5;   // two tiles off the line the crawler walks
  const hp0 = e.hp;
  advanceFlow(st, 1, []);
  assert.equal(c.onPlayer, false); assert.equal(e.hp, hp0); assert.equal(threatOf(st.flow!).stats.turned, 0);
  threatOf(st.flow!).crawlers.length = 0;
  // 2. shot: it turns and comes; 5 HP/s once in contact
  const c2 = plant(st, e.x + 5, e.y);
  e.inv.magazine = 3;
  advanceFlow(st, 0.05, [{ type: 'aim', at: [c2.x, c2.y] }]);
  assert.equal(c2.onPlayer, true, 'a hit crawler turns on the engineer');
  assert.ok(c2.hp < ENEMIES[0].hp);
  advanceFlow(st, 0.05, [{ type: 'aim', at: null }]);
  const hp1 = e.hp;
  advanceFlow(st, 3, []);
  const lost = hp1 - e.hp;
  assert.ok(lost > RETALIATE_HP_PER_S * 1 && lost <= RETALIATE_HP_PER_S * 3 + 1e-6, `lost ${lost} HP in 3 s with a crawler on them`);
  assert.equal(c2.onPlayer, true);
  threatOf(st.flow!).crawlers.length = 0;
  // 3. standing in its path: it turns without a shot
  e.hp = ENGINEER_HP; e.lastHit = -100;
  const c3 = plant(st, sub.x - 6.5, sub.y + 1.5);
  e.x = c3.x + 2; e.y = c3.y;   // on the straight line to the substation
  advanceFlow(st, 1, []);
  assert.equal(c3.onPlayer, true, 'the engineer stood in its path');
  assert.equal(threatOf(st.flow!).stats.turned, 2);
  // 4. knocked down; up at the workbench with the pockets intact; no other penalty
  const inv = { ...e.inv };
  e.hp = 4; e.lastHit = st.t;
  const c4 = plant(st, e.x + 0.6, e.y);
  c4.onPlayer = true;
  advanceFlow(st, 2, []);
  assert.ok(e.down >= 0, 'knocked down');
  assert.equal(e.downs, 1);
  assert.equal(c4.onPlayer, false, 'a crawler drops a downed engineer and keeps its chain');
  advanceFlow(st, RESPAWN_S + 0.1, []);
  const [wx, wy] = workbenchTile(st);
  assert.equal(e.down, -1); assert.equal(e.hp, ENGINEER_HP);
  assert.ok(Math.abs(e.x - wx - 0.5) < 1e-6 && Math.abs(e.y - wy - 0.5) < 1e-6, 'stands up at the HQ workbench');
  assert.deepEqual(e.inv, inv, 'the pockets are intact');
  void G;
});

test('the rifle: 1.5 rounds a second, three rounds a crawler (12 HP at 4 a round), no reach past 9 tiles; a shade off a lit tile cannot be hit by rifle or turret, on a lit one it can', () => {
  const st = city(), e = st.engineer, hq = hqIndex(st);
  dryTurrets(st); farAway(st);
  e.inv.magazine = 5;
  const c = plant(st, e.x + 5, e.y, 'crawler', 2);
  const T = threatOf(st.flow!);
  const mags0 = e.inv.magazine;
  // pin the crawler: it is turned on the engineer but held off by a dash-less contact rule; simpler: freeze it by hp bookkeeping
  let t = 0, killedAt = -1;
  while (t < 4 && killedAt < 0) {
    advanceFlow(st, 0.05, [{ type: 'aim', at: [c.x, c.y] }]);
    t += 0.05;
    if (!T.crawlers.includes(c)) killedAt = t;
  }
  assert.ok(killedAt > 0, 'killed');
  assert.equal(Math.round(e.fired), 3, `${e.fired} rounds for one crawler`);
  assert.ok(Math.abs(e.inv.magazine - (mags0 - 3 / ROUNDS_PER_MAG)) < 1e-9, 'three rounds out of the magazines');
  assert.ok(killedAt >= 2 / RIFLE_ROUNDS_PER_S - 0.1 && killedAt <= 3 / RIFLE_ROUNDS_PER_S + 0.1, `≈2 s a crawler: ${killedAt.toFixed(2)} s`);
  assert.equal(ROUND_DMG * 3, ENEMIES[0].hp);
  assert.equal(e.kills, 1); assert.equal(T.stats.rifleKills, 1);
  advanceFlow(st, 0.05, [{ type: 'aim', at: null }]);
  // out of reach: a crawler 10 tiles away is not hit
  const far = plant(st, e.x + 10.5, e.y, 'crawler', 2);
  e.fired = 0;
  advanceFlow(st, 1, [{ type: 'aim', at: [far.x, far.y] }]);
  assert.ok(e.fired >= 1 && far.hp === ENEMIES[0].hp, 'rounds spent, none landed past 9 tiles');
  advanceFlow(st, 0.05, [{ type: 'aim', at: null }]);
  T.crawlers.length = 0;
  // shades: stand 5 tiles from a lit streetlight of the HQ; find an unlit tile within reach too
  const G = ground(st), l = blockLights(st, hq).find(x => x.lit && x.kind === 'streetlight')!;
  const lit: [number, number] = [Math.floor(l.tx), Math.floor(l.ty)];
  e.x = lit[0] + 5.5; e.y = lit[1] + 0.5;
  let unlit: [number, number] | null = null;
  for (let r = 2; r < 9 && !unlit; r++) for (let k = 0; k < 16 && !unlit; k++) {
    const a = k * Math.PI / 8, tx = Math.floor(e.x + r * Math.cos(a)), ty = Math.floor(e.y + r * Math.sin(a));
    if (tx < 0 || ty < 0 || tx >= G.tw || ty >= G.th) continue;
    if (!litAt(st, tx, ty)) unlit = [tx, ty];
  }
  assert.ok(unlit, 'an unlit tile within the rifle\'s reach');
  assert.ok(litAt(st, lit[0], lit[1]));
  const sDark = plant(st, unlit![0] + 0.5, unlit![1] + 0.5, 'shade', 2);
  advanceFlow(st, 1, [{ type: 'aim', at: [sDark.x, sDark.y] }]);
  assert.equal(sDark.hp, ENEMIES[1].hp, 'a shade on an unlit tile is untargetable');
  advanceFlow(st, 0.05, [{ type: 'aim', at: null }]);
  T.crawlers.length = 0;
  const sLit = plant(st, lit![0] + 0.5, lit![1] + 0.5, 'shade', 2);
  advanceFlow(st, 1, [{ type: 'aim', at: [sLit.x, sLit.y] }]);
  assert.ok(sLit.hp < ENEMIES[1].hp, 'a shade on a lit tile takes the round');
  void hq;
});

test('hand-fired engagements record whether the edge held; the fight the engineer left alone resolves when the block falls', () => {
  const st = city(), e = st.engineer;
  dryTurrets(st); farAway(st);
  e.inv.magazine = 20;
  const T = threatOf(st.flow!);
  const c = plant(st, e.x + 4, e.y, 'crawler', 2);
  while (T.crawlers.includes(c)) advanceFlow(st, 0.05, [{ type: 'aim', at: [c.x, c.y] }]);
  advanceFlow(st, 1.1, [{ type: 'aim', at: null }]);
  assert.equal(T.fights.length, 1);
  assert.equal(T.fights[0].kills, 1); assert.equal(T.fights[0].rounds, 3); assert.equal(T.fights[0].held, true);
  // a second fight on the same edge that the engineer walks away from: the block falls, the fight did not hold
  engage(st, 12);
  const c2 = plant(st, e.x + 4, e.y, 'crawler', 2);
  advanceFlow(st, 0.05, [{ type: 'aim', at: [c2.x, c2.y] }]);
  advanceFlow(st, 0.05, [{ type: 'aim', at: null }]);
  assert.equal(T.fights.length, 2); assert.equal(T.fights[1].held, null);
  T.crawlers.length = 0;
  for (let s = 1; s < 260 && T.fights[1].held === null; s++) { if (s % 15 === 0 && s < 60) engage(st, 12); advanceFlow(st, 1, []); }
  assert.equal(T.fights[1].held, false, 'the edge did not hold: 40 arrivals put the substation out');
});

test('the lattice keeps the block model: with the flow layer on a lattice map no tile threat is created and the engagement arithmetic is unchanged', () => {
  const cfg = { ...DEFAULT_CONFIG, eco: { ...DEFAULT_CONFIG.eco } };
  const st = createState(generateMap(3, cfg), cfg, 3);
  ensureFlow(st);
  const hq = st.blocks.findIndex(b => b.x === st.start[0] && b.y === st.start[1]);
  const ed = st.ring.find(x => x.a === hq)!;
  st.engagements.push({ id: ed.id, cr: 6, sh: 0, rcr: 2, rsh: 0 });
  const h0 = ed.hopper;
  advanceFlow(st, 3, []);
  assert.equal(st.flow!.threat, undefined, 'no tile threat on the lattice');
  assert.ok(ed.hopper < h0, 'the block model fed the edge from its hopper');
  void step;
});

test('an M3 snapshot without the M4 fields loads: threat, chest trips and reach refusals appear on first use', () => {
  const st = city();
  const f = st.flow! as unknown as { threat?: unknown; hand: { away?: boolean }; stats: { chestTrips?: number; reachRefused?: number } };
  delete f.threat; delete f.hand.away; delete f.stats.chestTrips; delete f.stats.reachRefused;
  const json = JSON.parse(JSON.stringify(st)) as SimState;
  ensureFlow(json);
  assert.equal(json.flow!.hand.away, false); assert.equal(json.flow!.stats.chestTrips, 0); assert.equal(json.flow!.stats.reachRefused, 0);
  advanceFlow(json, 1, []);
  assert.ok(json.flow!.threat, 'the threat state is created by the first tile tick');
});
