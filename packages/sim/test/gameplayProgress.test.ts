import { test } from 'node:test';
import assert from 'node:assert/strict';
import { createCampaign, loadState, makeSave, stateProblem, stateHash, strongholdProgress, addMachine, heldItems, progressionCommand } from '../src/index';

test('quest selectors and new-game save preserve distinct empty permanent knowledge', () => {
  const s = createCampaign(), before = stateHash(s);
  assert.deepEqual(strongholdProgress(s, 'freight'), { collected: 0, required: 3, opened: false, recovered: false });
  assert.equal(stateHash(s), before);
  assert.deepEqual(loadState(makeSave(s)).campaign!.progression!.gameplay, s.campaign!.progression!.gameplay);
});

test('pre-GP earned core migrates on a copy without relocking or replaying rewards', () => {
  const old = createCampaign(), p = old.campaign!.progression!;
  delete p.gameplay;
  old.flow!.threat!.crawlers = []; // historical saves have no GP actors
  for (const s of p.sites) delete s.guards;
  const core = p.sites.find(s => s.item === 'core1')!;
  core.recovered = true; core.enabled = false; old.engineer.inv.core1 = 1;
  const before = JSON.stringify(old), migrated = loadState(old);
  assert.equal(JSON.stringify(old), before);
  assert.equal(migrated.engineer.inv.core1, 1);
  assert.deepEqual(strongholdProgress(migrated, 'freight'), { collected: 0, required: 3, opened: true, recovered: true });
  assert.equal(stateProblem(migrated), '');
  assert.deepEqual(loadState(migrated).campaign!.progression!.gameplay, migrated.campaign!.progression!.gameplay);
});

test('corrupt keys, unknown knowledge, forged inheritance and future versions reject', () => {
  const base = createCampaign();
  for (const corrupt of [
    (s: typeof base) => { s.campaign!.progression!.gameplay!.claimed = ['freight:camp:1', 'freight:camp:1']; },
    (s: typeof base) => { s.campaign!.progression!.gameplay!.strongholds.freight.keys = ['quarry:camp:1']; },
    (s: typeof base) => { s.campaign!.progression!.gameplay!.learnedSchematics = ['overclock']; },
    (s: typeof base) => { s.campaign!.progression!.gameplay!.strongholds.freight.inherited = true; },
    (s: typeof base) => { Object.assign(s.campaign!.progression!.gameplay!, { version: 99 }); },
  ]) {
    const s = structuredClone(base); corrupt(s);
    const before = JSON.stringify(s);
    assert.throws(() => loadState(s), /Invalid/);
    assert.equal(JSON.stringify(s), before);
  }
});

test('old upgrades preserve installed and full-backpack ownership and migrate exactly once', () => {
  const old = createCampaign(), p = old.campaign!.progression!;
  delete p.gameplay;
  old.flow!.threat!.crawlers = []; // historical saves have no GP actors
  for (const s of p.sites) delete s.guards;
  for (const item of ['artifact1', 'artifact2']) {
    const source = p.sites.find(s => s.item === item)!;
    source.recovered = true; source.enabled = false;
  }
  const m = addMachine(old, 'assembler', 10, 10, 0); m.recipe = 'shot'; m.artifact = 'artifact1';
  old.engineer.inv = { artifact2: 1, steel: 1950 };
  old.engineer.pack = [{ item: 'artifact2', count: 1 }, ...Array.from({ length: 39 }, () => ({ item: 'steel', count: 50 }))];
  const before = JSON.stringify(old), next = loadState(old), g = next.campaign!.progression!.gameplay!;
  assert.equal(JSON.stringify(old), before);
  assert.equal(next.flow!.machines.find(x => x.id === m.id)!.artifact, 'overclock');
  assert.equal(next.engineer.inv.overclock, 1); assert.equal(next.engineer.pack![0]!.item, 'overclock');
  assert.equal(heldItems(next).total.overclock, 2);
  assert.deepEqual(g.legacyUpgrades, ['artifact1', 'artifact2']);
  assert.deepEqual(g.learnedSchematics, ['overclock']);
  assert.equal(stateProblem(next), '');
  assert.equal(stateHash(loadState(next)), stateHash(next));
  const owned = next.flow!.machines.find(x => x.id === m.id)!;
  next.engineer.x = owned.x - .5; next.engineer.y = owned.y + .5;
  assert.equal(progressionCommand(next, { type: 'detach', machine: owned.id }), 'Backpack full');
  assert.equal(owned.artifact, 'overclock'); assert.equal(heldItems(next).total.overclock, 2);
  old.engineer.inv.artifact1 = 1; // the same unique earned object now appears twice
  assert.throws(() => loadState(old), /unique reward ownership/);
});
