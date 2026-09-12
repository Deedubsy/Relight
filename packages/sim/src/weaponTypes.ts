import type {WeaponKind} from './weaponProfiles';
export type WeaponItem = `${WeaponKind}:${number}`;
export const isWeaponItem = (id: string): id is WeaponItem => /^(rifle|double|arc|plasma):[1-9]\d{0,8}$/.test(id);
