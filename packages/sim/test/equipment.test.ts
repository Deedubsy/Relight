import { test } from 'node:test';
import assert from 'node:assert/strict';
import { createCampaign, equipmentCommand, tickEquipment, activeWeapon, spendRound, heldItems, stateProblem, loadState, stateHash, depotRect, take, chestPut, chestTake } from '../src/index';

function home() { const s = createCampaign(), h = depotRect(s); s.engineer.x = h.x - .5; s.engineer.y = h.y + .5; return s; }
test('paid Rifle output, equip, reload, shot, full/partial storage and save conserve ownership', () => {
  const s = home(), e = s.engineer;
  assert.equal(spendRound(s, e), false);
  assert.equal(equipmentCommand(s, { type: 'craftRifle' }).ok, false);
  e.inv = { steel: 20, copper: 8, magazine: 30 };
  assert.equal(equipmentCommand(s, { type: 'craftRifle' }).ok, true);
  tickEquipment(s, 6);
  assert.equal(e.inv.steel, 10); assert.equal(e.inv['rifle:1'], 1);
  assert.equal(equipmentCommand(s, { type: 'equip', slot: 0, item: 'rifle:1' }).ok, true);
  const before = heldItems(s).total.magazine;
  assert.equal(equipmentCommand(s, { type: 'reload' }).ok, true); tickEquipment(s, 1.5);
  assert.equal(activeWeapon(s)!.loaded, 10); assert.equal(heldItems(s).total.magazine, before);
  assert.equal(spendRound(s, e), true); assert.equal(spendRound(s, e), false);
  assert.equal(activeWeapon(s)!.loaded, 9);
  assert.equal(equipmentCommand(s, { type: 'unequip', slot: 0 }).ok, true);
  assert.equal(chestPut(s, 'rifle:1', 1).moved, 1); assert.equal(chestTake(s, 'rifle:1', 1).moved, 1);
  assert.equal(equipmentCommand(s, { type: 'equip', slot: 1, item: 'rifle:1' }).ok, true);
  assert.equal(spendRound(s, e), false); // transfer/equip cannot reset cooldown
  assert.equal(activeWeapon(s)!.loaded, 9); assert.equal(stateProblem(s), '');
  assert.equal(stateHash(loadState(s)), stateHash(s));
});
test('reload swap cancellation and unavailable output never spend or duplicate ammunition', () => {
  const s = home(), e = s.engineer; e.inv = { steel: 20, copper: 8, magazine: 20 };
  for (const slot of [0, 1] as const) { equipmentCommand(s, { type: 'craftRifle' }); tickEquipment(s, 6); equipmentCommand(s, { type: 'equip', slot, item: `rifle:${slot + 1}` }); }
  equipmentCommand(s, { type: 'reload' }); tickEquipment(s, .5); equipmentCommand(s, { type: 'swap' }); tickEquipment(s, 2);
  assert.equal(e.inv.magazine, 20); assert.equal(e.equipment!.weapons['rifle:2']!.loaded, 0);
  assert.equal(take(e, 'rifle:1', .5), 0);
  assert.equal(stateProblem(s), '');
});
test('legacy intrinsic Arsenal and partial magazines migrate once without changing original', () => {
  const s = home(); delete s.flow!.ammoVersion;for(const m of s.flow!.machines)delete m.ammoVersion; delete s.engineer.equipment; s.engineer.barrels = 2; s.engineer.inv.magazine = 2.7;
  const before = JSON.stringify(s), next = loadState(s);
  assert.equal(JSON.stringify(s), before); assert.equal(activeWeapon(next)!.kind, 'double');
  assert.equal(activeWeapon(next)!.loaded, 7); assert.equal(next.engineer.inv.magazine, 20);
  assert.equal(heldItems(next).total.magazine, 27);
  assert.equal(stateHash(loadState(next)), stateHash(next));
});
