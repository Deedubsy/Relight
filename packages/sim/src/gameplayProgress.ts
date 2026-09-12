/** GP progression facts are independent of physical inventory and operating power. */
import type { SimState } from './types';

export const STRONGHOLDS = ['freight', 'quarry', 'wharf'] as const;
export type StrongholdId = typeof STRONGHOLDS[number];
export const SCHEMATICS = ['overclock', 'arc', 'plasma'] as const;
export type SchematicId = typeof SCHEMATICS[number];
export interface StrongholdProgress { keys: string[]; opened: boolean; recovered: boolean; inherited: boolean }
export interface GameplayProgress {
  version: 1;
  legacyEquipment?: string[];
  region?: import('./firstRegion').FirstRegion;
  upgradesMigrated?: true;
  legacyUpgrades?: string[];
  claimed: string[];
  strongholds: Record<StrongholdId, StrongholdProgress>;
  recoveredSchematics: SchematicId[];
  learnedSchematics: SchematicId[];
  commissioned: string[];
}

export function initGameplayProgress(st: SimState): void {
  const p = st.campaign?.progression;
  // Procedural/legacy worlds retain their original reward and access rules.
  if (!p || !st.city?.mapId || p.gameplay) return;
  const strongholds = {} as GameplayProgress['strongholds'];
  STRONGHOLDS.forEach((id, i) => {
    const recovered = !!p.sites.find(s => s.item === `core${i + 1}`)?.recovered;
    strongholds[id] = { keys: [], opened: recovered, recovered, inherited: recovered };
  });
  p.gameplay = { version: 1, claimed: [], strongholds, recoveredSchematics: [], learnedSchematics: [],
    commissioned: p.sites.filter(s => s.kind === 'plant' && s.installed).map(s => s.id) };
}

/** Read-only selector: never creates rewards or reveals unknown source positions. */
export function strongholdProgress(st: SimState, id: StrongholdId) {
  const p = st.campaign?.progression?.gameplay?.strongholds[id];
  return { collected: p?.keys.length ?? 0, required: 3, opened: p?.opened ?? false, recovered: p?.recovered ?? false };
}

export function gameplayProgressProblem(st: SimState): string {
  const g = st.campaign?.progression?.gameplay;
  if (g === undefined) return ''; // pre-GP saves are validated before copy migration
  const unique = (a: unknown): a is string[] => Array.isArray(a) && a.every(x => typeof x === 'string' && x.length > 0 && x.length <= 120) && new Set(a).size === a.length;
  if (!g || g.version !== 1 || !unique(g.claimed) || g.claimed.length > 256 || !g.strongholds ||
    Object.keys(g.strongholds).length !== STRONGHOLDS.length || !unique(g.recoveredSchematics) || !unique(g.learnedSchematics) ||
    [...g.recoveredSchematics, ...g.learnedSchematics].some(id => !(SCHEMATICS as readonly string[]).includes(id)) ||
    g.learnedSchematics.some(id => !g.recoveredSchematics.includes(id)) || !unique(g.commissioned)) return 'Invalid gameplay progression';
  if (g.upgradesMigrated !== undefined && (g.upgradesMigrated !== true || !unique(g.legacyUpgrades) || g.legacyUpgrades.some(id => !/^artifact[123]$/.test(id) || !st.campaign!.progression!.sites.some(s => s.item === id && s.recovered)))) return 'Invalid upgrade migration receipts';
  for (const id of STRONGHOLDS) {
    const p = g.strongholds[id];
    if (!p || !unique(p.keys) || p.keys.length > 4 || p.keys.some(key => !new RegExp(`^${id}:camp:[1-4]$`).test(key) || !g.claimed.includes(key)) ||
      [p.opened, p.recovered, p.inherited].some(v => typeof v !== 'boolean') || p.recovered && !p.opened ||
      p.opened && p.keys.length < 3 && !p.inherited) return 'Invalid stronghold progress';
  }
  for (const [i, id] of STRONGHOLDS.entries()) {
    const recovered = !!st.campaign!.progression!.sites.find(s => s.item === `core${i + 1}`)?.recovered;
    if (g.strongholds[id].recovered !== recovered || g.strongholds[id].inherited && !recovered && !(id==='freight'&&g.claimed.includes('freight:legacy-access'))) return 'Invalid core recovery history';
  }
  const plants = st.campaign!.progression!.sites.filter(s => s.kind === 'plant' && s.installed).map(s => s.id);
  if (g.commissioned.some(id => !plants.includes(id)) || plants.some(id => !g.commissioned.includes(id))) return 'Invalid commissioned plant history';
  return '';
}

/** Until an access gate is authored, preserve the existing legitimate guard-only recovery route. */
export function recordCoreRecovery(st: SimState, item: string): void {
  const g = st.campaign?.progression?.gameplay;
  const id = STRONGHOLDS[['core1', 'core2', 'core3'].indexOf(item)];
  if (!g || !id) return;
  const p = g.strongholds[id];
  if (!p.opened) { p.opened = true; p.inherited = true; }
  p.recovered = true;
}
