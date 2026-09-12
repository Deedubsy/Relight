import {WEAPON_PROFILES,weaponRangeText,type WeaponKind} from './weaponProfiles';
import {ammoUnit} from './flow';
import type { SimState } from './types';
import { nearDepot } from './flow';
import { take, drop, markShot } from './engineer';
import { heldItems } from './ledger';

import { isWeaponItem, type WeaponItem } from './weaponTypes';
export { isWeaponItem, type WeaponItem } from './weaponTypes';
export interface Weapon { kind: WeaponKind; loaded: number; cooldown: number; reload: number }
export interface Equipment {
  version: 1; next: number; weapons: Partial<Record<WeaponItem, Weapon>>;
  slots: [WeaponItem | null, WeaponItem | null]; active: 0 | 1;
  craft?: { seconds: number };
}
/** D-GP-START provisional first-cycle tuning; later paired-shot redesign stays GP-16. */
export const RIFLE = { steel: 10, copper: 4, seconds: 6, capacity: 10, reload: 1.5, rate: 2.5, range: WEAPON_PROFILES.rifle.effective, damage: 10 } as const;
export type EquipmentAction = { type: 'craftRifle' } | { type: 'cancelRifle' } | { type: 'reload' } | { type: 'swap' } | { type: 'equip'; item: WeaponItem; slot: 0 | 1 } | { type: 'unequip'; slot: 0 | 1 };
export function activeWeapon(st: SimState): Weapon | undefined {
  const q = st.engineer.equipment, id = q?.slots[q.active]; return id ? q!.weapons[id] : undefined;
}
function createWeapon(st: SimState, kind: Weapon['kind']): WeaponItem {
  const q = st.engineer.equipment!, id: WeaponItem = `${kind}:${q.next++}`;
  q.weapons[id] = { kind, loaded: 0, cooldown: 0, reload: 0 }; return id;
}
export function initEquipment(st: SimState, fresh = false): void {
  const e = st.engineer;
  if (!st.campaign?.progression?.gameplay || e.equipment) return;
  e.equipment = { version: 1, next: 1, weapons: {}, slots: [null, null], active: 0 };
  if (!fresh) {
    const id = createWeapon(st, e.barrels === 2 || st.campaign.progression.arsenal ? 'double' : 'rifle');
    e.equipment.slots[0] = id;
    const w = e.equipment.weapons[id]!;
    // Legacy fractional magazine remainder is loaded once; full magazines stay in the Backpack.
    const reserve = Math.round((e.inv.magazine ?? 0) * ammoUnit(st)), partial = reserve % 10;
    if (partial) { drop(e, 'magazine', partial / ammoUnit(st)); w.loaded = partial; }
    w.cooldown = Math.max(0, e.cooldown);
    st.flow!.stats.minedOf[id] = 1;
    st.campaign.progression.notice = 'Existing rifle capability preserved as equipped gear; ammunition conserved.';
  }
}
export function equipmentCheck(st: SimState, a: EquipmentAction): string {
  const e = st.engineer, q = e.equipment;
  if (!q) return 'Equipment belongs to the authored campaign';
  if (e.down >= 0 || e.truckSeat || st.campaign?.progression?.passenger) return 'Use equipment on foot';
  if (a.type === 'cancelRifle') return q.craft ? '' : 'No Rifle is being crafted';
  if (a.type === 'craftRifle') return !nearDepot(st) ? 'Walk to Home workshop' : q.craft ? 'Rifle crafting already in progress' :
    (e.inv.steel ?? 0) < RIFLE.steel || (e.inv.copper ?? 0) < RIFLE.copper ? 'Carry 10 Steel plates + 4 Copper' : '';
  if (a.type === 'equip' || a.type === 'unequip') {
    if (a.slot !== 0 && a.slot !== 1) return 'Choose equipment slot 1 or 2';
    if (a.type === 'equip' && (!isWeaponItem(a.item) || !q.weapons[a.item] || e.inv[a.item] !== 1)) return 'Select a carried weapon';
    const old = q.slots[a.slot];
    if (old) {
      const trial = { ...e, inv: { ...e.inv }, pack: e.pack?.map(s => s && { ...s }) };
      if (a.type === 'equip') drop(trial, a.item, 1);
      if (take(trial, old, 1) !== 1) return 'Make room in the Backpack for the equipped weapon';
    }
    return '';
  }
  if (a.type === 'swap') return q.slots[q.active === 0 ? 1 : 0] ? '' : 'Other equipment slot is empty';
  const w = activeWeapon(st);
  return !w ? 'Equip a Rifle' : w.reload > 0 ? 'Reloading' : w.loaded >= RIFLE.capacity ? 'Magazine is full' :
    Math.round((e.inv.magazine ?? 0) * ammoUnit(st)) <= 0 ? 'Carry bullets in your Backpack' : '';
}
export function equipmentCommand(st: SimState, a: EquipmentAction): { ok: boolean; reason: string } {
  const reason = equipmentCheck(st, a); if (reason) return { ok: false, reason };
  const e = st.engineer, q = e.equipment!;
  if (a.type === 'craftRifle') {
    for (const [item, n] of [['steel', RIFLE.steel], ['copper', RIFLE.copper]] as const) { drop(e, item, n); st.flow!.stats.consumed[item] += n; }
    q.craft = { seconds: 0 }; return { ok: true, reason: 'Crafting Rifle at Home · 6 seconds; output waits safely if Backpack is full' };
  }
  if (a.type === 'cancelRifle') { cancelRifleCraft(st); return { ok: true, reason: `Rifle crafting cancelled. ${RIFLE.steel} Steel plates + ${RIFLE.copper} Copper returned.` }; }
  if (a.type === 'reload') { activeWeapon(st)!.reload = RIFLE.reload; return { ok: true, reason: st.speed === 0 ? 'Reload queued · resume the game to load carried bullets' : 'Reloading · 1.5 seconds; uses carried bullets' }; }
  // Reload progress belongs to the weapon and pauses while it is stored.
  if (a.type === 'swap') q.active = q.active === 0 ? 1 : 0;
  else {
    const old = q.slots[a.slot];
    if (a.type === 'equip') drop(e, a.item, 1);
    if (old) take(e, old, 1);
    q.slots[a.slot] = a.type === 'equip' ? a.item : null;
    if (a.type === 'equip') q.active = a.slot;
  }
  e.cooldown = activeWeapon(st)?.cooldown ?? 0;
  return { ok: true, reason: 'Equipment updated; loaded ammunition and cooldown retained' };
}
/** GP-PLAYTEST-FIX 4 (2026-09-11): an unfinished Rifle returns its reserved plates to the Backpack (spilling to Home stock);
 *  the finished output is never refunded, so repeated cancellation cannot create resources. */
export function cancelRifleCraft(st: SimState): boolean {
  const e = st.engineer, q = e.equipment; if (!q?.craft) return false;
  for (const [item, n] of [['steel', RIFLE.steel], ['copper', RIFLE.copper]] as const) { const back = take(e, item, n); if (back < n) st.stock[item] += n - back; st.flow!.stats.consumed[item] -= n; }
  delete q.craft; return true;
}
export const rifleCrafting = (st: SimState): boolean => !!st.engineer.equipment?.craft;
export function tickEquipment(st: SimState, dt: number): void {
  const e = st.engineer, q = e.equipment; if (!q) return;
  if (q.craft && (e.down >= 0 || !nearDepot(st))) cancelRifleCraft(st);   // no remote or downed crafting: cancel and refund
  for (const w of Object.values(q.weapons)) if (w) w.cooldown = Math.max(0, w.cooldown - dt);
  if (q.craft && nearDepot(st) && e.down < 0) {
    q.craft.seconds = Math.min(RIFLE.seconds, q.craft.seconds + dt);
    if (q.craft.seconds >= RIFLE.seconds) {
      const id: WeaponItem = `rifle:${q.next}`;
      if (take(e, id, 1) === 1) { createWeapon(st, 'rifle'); st.flow!.stats.made[id] = 1; delete q.craft; }
    }
  }
  const w = activeWeapon(st);
  if (w && w.reload > 0 && e.down < 0 && !e.truckSeat && !st.campaign?.progression?.passenger) {
    w.reload = Math.max(0, w.reload - dt);
    if (w.reload === 0) { const n = Math.min(RIFLE.capacity - w.loaded, Math.round((e.inv.magazine ?? 0) * ammoUnit(st))); drop(e, 'magazine', n / ammoUnit(st)); w.loaded += n; }
  }
  e.cooldown = w?.cooldown ?? 0;
}
export function fireEquipment(st: SimState): boolean {
  const w = activeWeapon(st); if (!w || w.reload > 0 || w.cooldown > 1e-8 || w.loaded < 1) return false;
  w.loaded--; w.cooldown = 1 / WEAPON_PROFILES[w.kind].rate;
  st.engineer.fired++; if (st.engineer.firstShot < 0) st.engineer.firstShot = st.t;
  markShot(st); return true;
}
export function equipmentLabel(st: SimState): string {
  const q = st.engineer.equipment, w = activeWeapon(st);
  return !q ? 'Rifle' : !w ? Object.keys(q.weapons).length?'No weapon equipped · select your carried Rifle below, then Equip':'No weapon equipped · craft a Rifle at Home' :
    `${weaponRangeText(w.kind)} · ${w.loaded}/${RIFLE.capacity} loaded · ${Math.round((st.engineer.inv.magazine ?? 0) * ammoUnit(st))} reserve rounds${w.reload > 0 ? ` · Reload ${w.reload.toFixed(1)} s${st.speed === 0 ? ' · paused; resume to finish' : ''}` : ''}`;
}
export function equipmentProblem(st: SimState): string {
  const q = st.engineer.equipment; if (q === undefined) return '';
  if (!q || q.version !== 1 || !Number.isSafeInteger(q.next) || q.next < 1 || q.next > 999999999 ||
    !q.weapons || typeof q.weapons !== 'object' || Array.isArray(q.weapons) || !Array.isArray(q.slots) || q.slots.length !== 2 ||
    ![0, 1].includes(q.active) || q.slots.some(id => id !== null && (!isWeaponItem(id) || !q.weapons[id])) ||
    q.slots[0] !== null && q.slots[0] === q.slots[1] ||
    q.craft && (!Number.isFinite(q.craft.seconds) || q.craft.seconds < 0 || q.craft.seconds > RIFLE.seconds)) return 'Invalid equipment state';
  const total = heldItems(st).total;
  for (const [id, w] of Object.entries(q.weapons)) {
    if (!isWeaponItem(id) || Number(id.split(':')[1]) >= q.next || !w || w.kind !== id.split(':')[0] ||
      !Number.isSafeInteger(w.loaded) || w.loaded < 0 || w.loaded > RIFLE.capacity ||
      !Number.isFinite(w.cooldown) || w.cooldown < 0 || w.cooldown > 1 || !Number.isFinite(w.reload) || w.reload < 0 || w.reload > RIFLE.reload || total[id] !== 1) return 'Invalid weapon ownership or ammunition';
  }
  if (Object.entries(total).some(([id, n]) => isWeaponItem(id) && n > 0 && !q.weapons[id])) return 'Unknown weapon instance';
  return '';
}
