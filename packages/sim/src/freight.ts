/** EX-06A: optional station contracts on the existing unbranched tram route.
 * Cargo stays in Machine.cargo (the ledger's single owner); manifests only reserve it.
 * Unconfigured routes retain RI-05 transfers. Configuring a stop opts its route into
 * selective loading. This is a transport increment, not the Version 2 campaign profile. */
import type { SimState } from './types';
import { type Machine, type Item, ITEMS, isItem, invTotal, STOP_CAP, TRAM_CAP, machineAt } from './flow';
import { inReach } from './ground';
import { recordDistrictDelivery } from './campaignDistricts';

export interface StationRule { request: number; reserve: number; export: boolean }
export type StationRules = Partial<Record<Item, StationRule>>;
export interface FreightReservation { item: Item; n: number; origin: number; destination: number; returning: boolean }

const count = (n: unknown, max = STOP_CAP): n is number => Number.isSafeInteger(n) && (n as number) >= 0 && (n as number) <= max;
export function validStationRules(v: unknown): v is StationRules {
  if (!v || typeof v !== 'object' || Array.isArray(v)) return false;
  return Object.entries(v).every(([item, rule]) => isItem(item) && rule && typeof rule === 'object'
    && count(rule.request) && count(rule.reserve) && typeof rule.export === 'boolean'
    && Object.keys(rule).every(k => ['request', 'reserve', 'export'].includes(k)));
}

/** Ordinary command path; invalid/out-of-reach edits do not upgrade the save. */
export function setStationRules(st: SimState, x: number, y: number, rules: StationRules): boolean {
  const stop = machineAt(st, x, y);
  if (stop?.kind !== 'tramstop' || !inReach(st, stop.x, stop.y, stop.size) || !validStationRules(rules)) return false;
  stop.freight = Object.fromEntries(Object.entries(rules).map(([k, r]) => [k, { ...r }]));
  if (st.version === 1) st.version = 2;
  return true;
}

/** Existing cargo may be unreserved when an old route is first configured. */
function reserved(tram: Machine, item: Item): number {
  return (tram.manifest ?? []).reduce((n, r) => n + (r.item === item ? r.n : 0), 0);
}
export function freightInbound(st: SimState, stop: number, item?: Item): number {
  let n = 0;
  for (const m of st.flow?.machines ?? []) for (const r of m.manifest ?? []) {
    if ((r.returning ? r.origin : r.destination) === stop && (!item || r.item === item)) n += r.n;
  }
  return n;
}

/** Serve once per visit. A refused delivery returns to its origin on the shuttle;
 * if that buffer is full/unpowered too, retain and retry. Missing origins retain
 * their cargo for recovery by picking up the tram. Dwell time never grows. */
export function transferFreight(st: SimState, tram: Machine, stop: Machine, routeStops: Machine[], powered: boolean): void {
  const cargo = tram.cargo ??= {}, manifest = tram.manifest ??= [];
  for (const r of manifest) {
    const target = routeStops.find(s => s.id === r.destination);
    if (!target?.freight) r.returning = true;
    const recipient = r.returning ? r.origin : r.destination;
    if (recipient !== stop.id) continue;
    if (powered) {
      const arrivals = stop.cargo ??= {};
      const n = Math.min(r.n, Math.max(0, STOP_CAP - invTotal(arrivals)));
      if (n > 0) {
        const origin=routeStops.find(s=>s.id===r.origin);
        if(!r.returning&&origin)recordDistrictDelivery(st,origin,stop,r.item,n);
        arrivals[r.item] = (arrivals[r.item] ?? 0) + n;
        cargo[r.item] -= n; if (!cargo[r.item]) delete cargo[r.item];
        r.n -= n;
        st.flow!.stats.tramMoved = (st.flow!.stats.tramMoved ?? 0) + n;
      }
    }
    if (r.n > 0 && !r.returning) r.returning = true;
  }
  tram.manifest = manifest.filter(r => r.n > 0);
  if (!powered) return;
  // Unreserved legacy cargo may unload, but never the portion owned by a later stop.
  const arrivals = stop.cargo ??= {};
  for (const item of ITEMS) {
    const n = Math.min((cargo[item] ?? 0) - reserved(tram, item), Math.max(0, STOP_CAP - invTotal(arrivals)));
    if (n <= 0) continue;
    arrivals[item] = (arrivals[item] ?? 0) + n;
    cargo[item] -= n; if (!cargo[item]) delete cargo[item];
    st.flow!.stats.tramMoved = (st.flow!.stats.tramMoved ?? 0) + n;
  }
  // Only explicitly exported platform stock boards; arrivals feed local belts/hands.
  for (const destination of routeStops) {
    if (destination.id === stop.id || !destination.freight) continue;
    for (const item of ITEMS) {
      const sourceRule = stop.freight?.[item], targetRule = destination.freight[item];
      if (!sourceRule?.export || !targetRule) continue;
      const demand = targetRule.request - (destination.inv[item] ?? 0) - (destination.cargo?.[item] ?? 0) - freightInbound(st, destination.id, item);
      const room = STOP_CAP - invTotal(destination.cargo) - freightInbound(st, destination.id);
      const n = Math.min(demand, room, TRAM_CAP - invTotal(cargo), (stop.inv[item] ?? 0) - sourceRule.reserve);
      if (n <= 0) continue;
      stop.inv[item] -= n; if (!stop.inv[item]) delete stop.inv[item];
      cargo[item] = (cargo[item] ?? 0) + n;
      tram.manifest.push({ item, n, origin: stop.id, destination: destination.id, returning: false });
    }
  }
}

/** Reject malformed new metadata rather than permitting inventory duplication on load. */
export function freightProblem(st: SimState): string {
  for (const m of st.flow?.machines ?? []) {
    if (m.freight === undefined && m.manifest === undefined) continue;
    if (st.version !== 2 && st.version !== 3) return 'station freight requires save version 2 or 3';
    if (m.freight !== undefined && (m.kind !== 'tramstop' || !validStationRules(m.freight))) return 'invalid station freight rules';
    if (m.manifest !== undefined) {
      if (m.kind !== 'tram' || !Array.isArray(m.manifest)) return 'invalid tram manifest';
      for (const r of m.manifest) {
        if (!r || !isItem(r.item) || !count(r.n, TRAM_CAP) || !r.n || !count(r.origin, Number.MAX_SAFE_INTEGER)
          || !count(r.destination, Number.MAX_SAFE_INTEGER) || r.origin === r.destination || typeof r.returning !== 'boolean') return 'invalid freight reservation';
      }
      if (invTotal(m.cargo) > TRAM_CAP) return 'tram cargo exceeds capacity';
      for (const item of ITEMS) if (!count(m.cargo?.[item] ?? 0, TRAM_CAP) || reserved(m, item) > (m.cargo?.[item] ?? 0)) return 'reservation exceeds tram cargo';
    }
  }
  return '';
}
