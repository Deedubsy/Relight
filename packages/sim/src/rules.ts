/** Campaign identity is independent of save schema and city presentation. Missing = legacy. */
import type { SimState } from './types';
import { CONTESTED, HELD } from './types';
export const LEGACY_RULESET = 'legacy-v1' as const;
export const CAMPAIGN_RULESET = 'exploration-v2' as const;
export type Ruleset = typeof LEGACY_RULESET | typeof CAMPAIGN_RULESET;
export const CAMPAIGN_RULES = { daySeconds: 1200, daylightSeconds: 900, firstAssaultNight: 3, opening: 'culdesac-v1' } as const;
export interface CampaignSite { block: number; x: number; y: number; size: number; delivered: { steel: number; copper: number }; restoredAt: number }
export interface ExpansionState { station: CampaignSite; radio: CampaignSite; route: number[]; stops: [number, number][]; reward: { track: number; tramstop: number; tram: number }; grantedAt: number }
export interface CampaignState { version: 1 | 2 | 3 | 4 | 5; truck?: import('./truck').TruckState; discovery?: import('./campaignDiscovery').DiscoveryState; districts?: import('./campaignDistricts').DistrictState; defence?: import('./campaignDefence').DefenceState; opening: 'culdesac-v1'; homeBlock: number; expansion?: ExpansionState }
export function isRuleset(value: unknown): value is Ruleset { return value === LEGACY_RULESET || value === CAMPAIGN_RULESET; }
export function rulesetOf(st: Pick<SimState, 'ruleset'>): Ruleset { return st.ruleset ?? LEGACY_RULESET; }
export function isCampaign(st: Pick<SimState, 'ruleset'>): boolean { return rulesetOf(st) === CAMPAIGN_RULESET; }
export function campaignClock(st: Pick<SimState, 't'>): { day: number; night: boolean; elapsed: number } {
  const elapsed = st.t % CAMPAIGN_RULES.daySeconds;
  return { day: Math.floor(st.t / CAMPAIGN_RULES.daySeconds) + 1, night: elapsed >= CAMPAIGN_RULES.daylightSeconds, elapsed };
}
export function rulesetProblem(st: SimState): string {
  if (st.ruleset !== undefined && !isRuleset(st.ruleset)) return 'unknown campaign ruleset';
  if (!isCampaign(st)) return st.campaign !== undefined || st.version === 3 ? 'campaign metadata on a legacy state' : '';
  if (st.version !== 3) return 'exploration campaign requires save schema 3';
  const c = st.campaign;
  if (!c || ![1,2,3,4,5].includes(c.version) || c.opening !== CAMPAIGN_RULES.opening || !Number.isInteger(c.homeBlock)
    || !st.blocks[c.homeBlock] || st.blocks[c.homeBlock].x !== st.start[0] || st.blocks[c.homeBlock].y !== st.start[1]) return 'invalid campaign opening';
  if (c.version >= 2) {
    const e = c.expansion;
    if (!st.flow) return 'expanded campaign requires the tile simulation';
    if (!e || !Array.isArray(e.route) || e.route.length < 2 || !e.route.every(Number.isInteger) || e.stops?.length !== 2) return 'invalid campaign route';
    const tw = st.flow.tw, th = st.city?.th ?? 0;
    if (e.route.some((t,i) => t < 0 || t >= tw * th || (i > 0 && Math.abs(t % tw - e.route[i-1] % tw) + Math.abs(Math.floor(t/tw) - Math.floor(e.route[i-1]/tw)) !== 1))
      || new Set(e.route).size !== e.route.length || e.stops.some(p => !Array.isArray(p) || p.length !== 2 || !p.every(Number.isInteger) || p[0]<0 || p[0]+2>tw || p[1]<0 || p[1]+2>th)) return 'invalid campaign route geometry';
    for (const site of [e.station, e.radio]) if (!site || !Number.isInteger(site.block) || !st.blocks[site.block]
      || !Number.isInteger(site.x) || !Number.isInteger(site.y) || site.x < 0 || site.y < 0 || !Number.isInteger(site.size) || site.size < 1 || site.x + site.size > tw || site.y + site.size > th || !Number.isFinite(site.restoredAt)
      || !site.delivered || ![site.delivered.steel, site.delivered.copper].every(v => Number.isInteger(v) && v >= 0)) return 'invalid campaign site';
    if (e.station.block !== e.radio.block || e.station.block === c.homeBlock || (e.station.restoredAt >= 0 && st.blocks[e.station.block].state !== HELD)) return 'invalid station base';
    if (!e.reward || ![e.reward.track, e.reward.tramstop, e.reward.tram].every(v => Number.isInteger(v) && v >= 0)
      || e.reward.track > e.route.length || e.reward.tramstop > 2 || e.reward.tram > 1 || !Number.isFinite(e.grantedAt)) return 'invalid tram reward';
    if (e.grantedAt < 0 && (e.station.restoredAt >= 0 || Object.values(e.reward).some(v => v > 0))) return 'unrecorded tram reward';
    if (e.radio.restoredAt >= 0 && e.station.restoredAt < 0) return 'radio restored before station';
  } else if (c.expansion) return 'expansion requires campaign metadata version 2';
  if (!st.city || st.city.profile !== 'riverside-v1' || st.lattice) return 'campaign requires its city geometry';
  if (st.ring.length || st.engagements.length || st.blocks.some(b => b.state === CONTESTED)) return 'legacy frontage or commissioning in campaign state';
  if (st.flow?.heart || st.flow?.threat?.stalk || Object.keys(st.flow?.projects ?? {}).length) return 'legacy candidate or project in campaign state';
  return '';
}
