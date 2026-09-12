import type { SimState } from './types';

/** Run only on a validated save copy or fresh state. Source records retain their original IDs. */
export function migrateOverclocks(st: SimState): void {
  const p = st.campaign?.progression, g = p?.gameplay;
  if (!p || !g || g.upgradesMigrated) return;
  const oldIds = ['artifact1', 'artifact2', 'artifact3'];
  const receipts = p.sites.filter(s => oldIds.includes(s.item ?? '') && s.recovered).map(s => s.item!);
  const skip = new Set<unknown>([g, ...p.sites]);
  function convert(value: unknown): unknown {
    if (typeof value === 'string') return oldIds.includes(value) ? 'overclock' : value;
    if (!value || typeof value !== 'object' || skip.has(value)) return value;
    if (Array.isArray(value)) { for (let i = 0; i < value.length; i++) value[i] = convert(value[i]); return value; }
    const record = value as Record<string, unknown>;
    for (const key of Object.keys(record)) {
      const mapped = oldIds.includes(key) ? 'overclock' : key, v = convert(record[key]);
      if (mapped === key) { record[key] = v; continue; }
      if (record[mapped] === undefined) record[mapped] = v;
      else if (typeof v === 'number' && typeof record[mapped] === 'number') record[mapped] = (record[mapped] as number) + v;
      else if (typeof v === 'object' && v && 'request' in v) {
        // Multiple former unique-item freight policies now address the same reusable module.
        const a = record[mapped] as { request: number; reserve: number; export: boolean }, b = v as typeof a;
        a.request = Math.min(200, a.request + b.request); a.reserve = Math.min(200, a.reserve + b.reserve); a.export ||= b.export;
      } else throw new Error('Cannot merge legacy upgrade metadata; original save preserved');
      delete record[key];
    }
    return value;
  }
  convert(st);
  g.legacyUpgrades = receipts; g.upgradesMigrated = true;
  if (receipts.length) {
    if (!g.recoveredSchematics.includes('overclock')) g.recoveredSchematics.push('overclock');
    if (!g.learnedSchematics.includes('overclock')) g.learnedSchematics.push('overclock');
    p.notice = 'Earned Speed upgrades preserved as removable Overclocks; Overclock knowledge retained.';
  }
}
