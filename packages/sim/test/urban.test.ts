import { test } from 'node:test';
import assert from 'node:assert/strict';
import { citySpec, DEFAULT_CONFIG, createState, ground, canPlace, ensureFlow, passable, HELD, makeSave, loadState, generateCity } from '../src/index';

test('RI-02A: saved urban geometry preserves resources, routes and solid placement restrictions', () => {
  for (const seed of [3, 4, 5, 6]) {
    const spec = citySpec(seed, 'river', DEFAULT_CONFIG);
    const legacy = createState(spec, DEFAULT_CONFIG, seed), old = ground(legacy);
    const st = createState({ ...spec, city: { ...spec.city!, profile: 'riverside-v1' } }, DEFAULT_CONFIG, seed);
    ensureFlow(st);
    const G = ground(st), U = G.urban!;
    assert.deepEqual(G.blocks.map(b => b.count), old.blocks.map(b => b.count));
    assert.deepEqual(G.patch, old.patch);
    assert.deepEqual(st.nb, legacy.nb);
    assert.deepEqual(ground(loadState(makeSave(st))).urban, U);
    assert.ok(U.structures.length > 0);
    for (const s of U.structures) {
      st.blocks[s.block].state = HELD;
      assert.equal(canPlace(st, 'belt', s.x, s.y).reason, 'a city structure is there');
      assert.equal(passable(st, s.x, s.y), false);
      assert.equal(passable(st, ...s.door), true);
      for (let y = s.y; y < s.y + s.h; y++) for (let x = s.x; x < s.x + s.w; x++) assert.equal(G.rank[y * G.tw + x], -1);
    }
    assert.equal(old.urban, undefined, 'legacy saves retain their original ground');
  }
});

test('RI-02A: retry cache respects budgets and playable specs reject invalid generation', () => {
  const full = generateCity(5, 'river'), one = generateCity(5, 'river', { attempts: 1 });
  assert.ok(full.valid);
  assert.equal(one.attempt, 0);
  if (!one.valid) assert.throws(() => citySpec(5, 'river', DEFAULT_CONFIG, { attempts: 1 }), /failed validation/);
  assert.throws(() => generateCity(3, 'river', { attempts: 0 }), /attempts/);
  const state = createState(citySpec(3, 'river', DEFAULT_CONFIG), DEFAULT_CONFIG, 3);
  const raw = JSON.parse(JSON.stringify(state)); raw.city.profile = 'future-v99';
  assert.throws(() => loadState(raw), /unsupported city profile/);
});
