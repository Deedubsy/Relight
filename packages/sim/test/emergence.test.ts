/** RI-04 — emergence points (plan §6): every street segment gives its Dark side a stable, readable point on its own
 *  kerb; rot is born there and nowhere else — never on a tile the Held block owns or faces; a Crawler carries its
 *  origin, a heading and an inspectable target; a Shade leaves a trace on the tiles it crossed. */
import { test } from 'node:test';
import assert from 'node:assert/strict';
import {
  DEFAULT_CONFIG, createState, citySpec, SimState, ensureFlow, advanceFlow, workbenchTile, ground, cityGeomOf, takeEvents, threatOf,
  emergence, emergencePoint, emergencePointsOf, activeEmergencePoints, birthTile, walkable, Crawler, ENEMIES, HELD, DARK, CONTESTED,
  crawlerTarget, describeCrawler, headingWord, shadeTraces, shadeNear, TRACE_S, TRACE_N, litAt,
} from '../src/index';

function city(seed = 3): SimState {
  const cfg = { ...DEFAULT_CONFIG, eco: { ...DEFAULT_CONFIG.eco } };
  const st = createState(citySpec(seed, 'river', cfg), cfg, seed);
  ensureFlow(st);
  st.buffer = 0;
  return st;
}
const hqIndex = (st: SimState) => st.blocks.findIndex(b => b.x === st.start[0] && b.y === st.start[1]);
const turretEdge = (st: SimState) => st.ring.find(ed => ed.a === hqIndex(st) && ed.turrets !== undefined)!;
const dryTurrets = (st: SimState) => { for (const m of st.flow!.machines) if (m.kind === 'turret') m.inv.rounds = 0; };
const farAway = (st: SimState) => { const e = st.engineer, [wx, wy] = workbenchTile(st); e.x = wx + 0.5; e.y = wy + 0.5; };
function engage(st: SimState, cr: number, sh = 0): void { st.engagements.push({ id: turretEdge(st).id, cr, sh, rcr: cr / 15, rsh: sh / 15 }); }
function plant(st: SimState, x: number, y: number, kind: Crawler['kind'] = 'crawler', cls: 0 | 1 | 2 = 2): Crawler {
  const T = threatOf(st.flow!), ed = turretEdge(st);
  const c: Crawler = { id: T.next++, kind, x, y, hp: kind === 'shade' ? ENEMIES[1].hp : ENEMIES[0].hp, edge: ed.id, from: ed.b, to: ed.a, cls, onPlayer: false, escaped: false, born: st.t, stuck: 0, dir: [0, 0] };   // a born body carries a heading
  T.crawlers.push(c);
  return c;
}

test('every street segment has an emergence point per direction: a walkable kerb tile of the emerging block (a street tile facing it), ids 1..N in (block, other) order, identical on a rebuilt state, one for every ring edge', () => {
  const st = city(), G = ground(st), E = emergence(st), geom = cityGeomOf(st);
  assert.equal(E.points.length, geom.segs.length * 2, 'two per segment');
  const seen = new Set<string>();
  E.points.forEach((p, k) => {
    assert.equal(p.id, k + 1, 'sequential ids');
    const t = p.ty * G.tw + p.tx;
    assert.ok(walkable(G, p.tx, p.ty), `point ${p.id} walkable`);
    assert.equal(G.owner[t], -1, `point ${p.id} is a street tile`);
    assert.equal(G.near[t], p.block, `point ${p.id} faces its block`);
    assert.ok(geom.segs.some(sg => (sg.a === p.block && sg.b === p.other) || (sg.b === p.block && sg.a === p.other)), 'a real segment');
    seen.add(`${p.block}:${p.other}`);
    if (k) assert.ok(E.points[k - 1].block < p.block || (E.points[k - 1].block === p.block && E.points[k - 1].other < p.other), 'sorted by (block, other)');
  });
  assert.equal(seen.size, E.points.length, 'no pair twice');
  for (const ed of st.ring) { assert.ok(emergencePoint(st, ed.a, ed.b), `ring edge ${ed.id} a→b`); assert.ok(emergencePoint(st, ed.b, ed.a), `ring edge ${ed.id} b→a`); }
  assert.equal(JSON.stringify(emergence(city()).points), JSON.stringify(E.points), 'stable across a rebuilt state');
  assert.equal(JSON.stringify(emergence(city(4)).points) === JSON.stringify(E.points), false, 'another seed, another city');
  const hq = hqIndex(st);
  assert.ok(emergencePointsOf(st, hq).length >= 3, 'the HQ has points of its own (quiet while it is Held)');
  for (const p of E.points) {
    assert.ok(p.kerb.length >= 1 && p.kerb.includes(p.ty * G.tw + p.tx), `point ${p.id}'s anchor is one of its ${p.kerb.length} kerb tiles`);
    for (const t of p.kerb) assert.ok(G.owner[t] === -1 && G.near[t] === p.block, `kerb tile of ${p.id} on its block's street`);
    for (const u of [0, 0.5, 0.999]) { const [bx, by] = birthTile(st, p, u); assert.ok(walkable(G, bx, by) && G.near[by * G.tw + bx] === p.block, `birth tile of ${p.id} at u=${u} on its block's kerb`); }
  }
});

test('active points are the Dark blocks\' points facing a Held or contested block: at the start the HQ\'s dark neighbours, never a Held block\'s own; a block turning Held goes quiet', () => {
  const st = city(), hq = hqIndex(st), geom = cityGeomOf(st);
  const act = activeEmergencePoints(st);
  const expected = geom.segs.filter(sg => (sg.a === hq && st.blocks[sg.b].state === DARK) || (sg.b === hq && st.blocks[sg.a].state === DARK)).length;
  assert.equal(act.length, expected, `${act.length} active points for the HQ's ${expected} dark neighbours`);
  for (const p of act) {
    assert.equal(st.blocks[p.block].state, DARK, 'emerges from a Dark block');
    assert.ok(st.blocks[p.other].state === HELD || st.blocks[p.other].state === CONTESTED, 'toward a Held or contested one');
    assert.notEqual(p.block, hq, 'never the Held block\'s own');
  }
  const nb = act[0].block;
  st.blocks[nb].state = HELD;
  const after = activeEmergencePoints(st);
  assert.ok(after.every(p => p.block !== nb), 'a Held block emits nothing');
  assert.ok(after.some(p => p.other === nb), 'its dark neighbours\' points toward it wake');
  st.blocks[nb].state = DARK;
});

test('births: every crawler is born at its emergence point\'s birth tile on the Dark block\'s kerb — never on a tile the Held block owns or faces — with its origin id; once moving it has a heading and an inspectable target on the block it walks into; a shade leaves a trace', () => {
  const st = city(), G = ground(st), hq = hqIndex(st), ed = turretEdge(st);
  dryTurrets(st); farAway(st);
  const T = threatOf(st.flow!), seen = new Set<number>();
  let born = 0, described = 0, targeted = 0;
  const birthTiles: Record<number, Set<number>> = {};
  for (let k = 0; k < 20 * 60; k++) {
    if (k % 300 === 0 && k < 1200) engage(st, 10, k === 0 ? 2 : 0);
    advanceFlow(st, 0.05, []);
    for (const c of T.crawlers) {
      if (!seen.has(c.id)) {
        seen.add(c.id); born++;
        assert.ok(c.origin !== undefined, `crawler ${c.id} has an origin`);
        const pt = emergencePoint(st, c.from, c.to)!;
        assert.ok(pt, 'the point of its (from, to) pair'); assert.equal(pt.id, c.origin, 'its origin is that point');
        const tx = Math.floor(c.x), ty = Math.floor(c.y), t = ty * G.tw + tx;
        assert.ok(pt.kerb.includes(t), `crawler ${c.id} born at (${tx},${ty}), on its point's frontage kerb`);
        assert.ok(walkable(G, tx, ty), 'born on a walkable tile');
        (birthTiles[pt.id] ??= new Set()).add(t);
        assert.equal(G.owner[t], -1, 'born on the street'); assert.equal(G.near[t], c.from, 'facing the Dark block');
        assert.notEqual(st.blocks[c.from].state, HELD, 'from a block that is not Held');
        assert.notEqual(G.owner[t], c.to, 'never inside the Held block'); assert.notEqual(G.near[t], c.to, 'never on the Held block\'s kerb');
      } else if (st.t - c.born >= 1 && c.kind === 'crawler') {
        const h = headingWord(c.dir!);
        if (h !== '·') { described++; assert.ok(['E', 'SE', 'S', 'SW', 'W', 'NW', 'N', 'NE'].includes(h)); }
        const tg = crawlerTarget(st, c);
        if (tg) {
          targeted++;
          const tt = tg.ty * G.tw + tg.tx;
          if (tg.what === 'you') assert.deepEqual([tg.tx, tg.ty], [Math.floor(st.engineer.x), Math.floor(st.engineer.y)]);
          else assert.ok(G.owner[tt] === c.to || G.near[tt] === c.to, `target ${tg.what} at (${tg.tx},${tg.ty}) is on block ${c.to}`);
          const d = describeCrawler(st, c);
          assert.ok(d.includes(`from emergence point ${c.origin}`) && (h === '·' || d.includes(`heading ${h}`)) && (tg.what === 'you' || d.includes(`at (${tg.tx},${tg.ty})`)), d);
        }
      }
    }
    takeEvents(st);
  }
  assert.ok(born >= 20, `${born} born`); assert.equal(born, T.stats.spawned);
  assert.ok(described >= 20 && targeted >= 20, `${described} with a heading, ${targeted} with a target`);
  const pt = emergencePoint(st, ed.b, ed.a)!;
  assert.equal(Object.values(T.born).reduce((a, b) => a + b, 0), T.stats.spawned, 'every birth counted on its point');
  assert.ok((T.born[pt.id] ?? 0) >= 20, `the engaged edge's point bore ${T.born[pt.id]}`);
  assert.ok((birthTiles[pt.id]?.size ?? 0) >= Math.min(3, pt.kerb.length), `births spread along the frontage: ${birthTiles[pt.id]?.size} tiles of ${pt.kerb.length}`);
  const wide = Object.entries(birthTiles).filter(([id]) => emergence(st).points[Number(id) - 1].kerb.length >= 8);
  assert.ok(wide.every(([id, tiles]) => tiles.size >= 3), `every wide frontage that bore bodies spread them: ${wide.map(([id, t]) => `${id}:${t.size}`).join(' ')}`);
  assert.equal(hq, ed.a);
});

test('a shade\'s trace: the tiles it crossed within TRACE_S, aged, at most TRACE_N; shadeNear finds it for the lamp flicker; the light layer is untouched by a trace', () => {
  const st = city(), G = ground(st), e = st.engineer;
  dryTurrets(st); farAway(st);
  const c = plant(st, e.x + 5, e.y, 'shade', 2);
  const litBefore = new Map<number, boolean>();
  for (let k = 0; k < 40; k++) {
    advanceFlow(st, 0.05, []);
    for (const tr of shadeTraces(st)) { const t = tr.ty * G.tw + tr.tx; if (!litBefore.has(t)) litBefore.set(t, litAt(st, tr.tx, tr.ty)); }
  }
  const traces = shadeTraces(st);
  assert.ok(traces.length >= 2 && traces.length <= TRACE_N, `${traces.length} trace tiles`);
  for (const tr of traces) {
    assert.ok(tr.age >= 0 && tr.age <= TRACE_S, `age ${tr.age}`);
    assert.ok(Math.hypot(tr.tx + 0.5 - c.x, tr.ty + 0.5 - c.y) <= ENEMIES[1].tilesPerSec * TRACE_S + 1, 'behind the shade');
    assert.equal(litAt(st, tr.tx, tr.ty), litBefore.get(tr.ty * G.tw + tr.tx), 'a trace never lights or unlights a tile');
  }
  assert.ok(shadeNear(st, Math.floor(c.x), Math.floor(c.y)), 'near its own tile');
  assert.ok(!shadeNear(st, Math.floor(c.x) + 12, Math.floor(c.y)), 'not twelve tiles off');
  c.trailT![0] = st.t - TRACE_S - 1;
  assert.equal(shadeTraces(st).length, traces.length - 1, 'an old trace fades');
  assert.match(describeCrawler(st, c), /^Shade · \d+\/\d+ HP · heading [NESW]{1,2} · toward the substation at \(\d+,\d+\) on \(\d+,\d+\)$/);
});
