/** Explicit new-session factory. Never converts a running legacy state. */
import { DEFAULT_CONFIG, createState } from './sim';
import { protoCalibrated, type SimConfig, type SimState } from './types';
import { citySpec } from './city';
import { ensureFlow } from './flow';
import { initExpansion } from './expansion';
import { CAMPAIGN_RULESET } from './rules';
export function campaignConfig(): SimConfig {
  return protoCalibrated({ ...DEFAULT_CONFIG, scatter: true, economy: true, walk: true, power: true, supply: 'generators', draw: 'half' });
}
export function createCampaign(seed = 3): SimState {
  const config = campaignConfig();
  const st = createState(citySpec(seed, 'river', config), config, seed, CAMPAIGN_RULESET);
  ensureFlow(st);
  initExpansion(st);
  return st;
}
