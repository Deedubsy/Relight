/** Campaign identity is independent of save schema and city presentation. Missing = legacy. */
import type { SimState } from './types';
import { CONTESTED } from './types';
export const LEGACY_RULESET = 'legacy-v1' as const;
export const CAMPAIGN_RULESET = 'exploration-v2' as const;
export type Ruleset = typeof LEGACY_RULESET | typeof CAMPAIGN_RULESET;
export const CAMPAIGN_RULES = { daySeconds: 1200, daylightSeconds: 900, firstAssaultNight: 3, opening: 'culdesac-v1' } as const;
export interface CampaignState { version: 1; opening: 'culdesac-v1'; homeBlock: number }
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
  if (!c || c.version !== 1 || c.opening !== CAMPAIGN_RULES.opening || !Number.isInteger(c.homeBlock)
    || !st.blocks[c.homeBlock] || st.blocks[c.homeBlock].x !== st.start[0] || st.blocks[c.homeBlock].y !== st.start[1]) return 'invalid campaign opening';
  if (!st.city || st.city.profile !== 'riverside-v1' || st.lattice) return 'campaign requires its city geometry';
  if (st.ring.length || st.engagements.length || st.blocks.some(b => b.state === CONTESTED)) return 'legacy frontage or commissioning in campaign state';
  if (st.flow?.heart || st.flow?.threat?.stalk || Object.keys(st.flow?.projects ?? {}).length) return 'legacy candidate or project in campaign state';
  return '';
}
