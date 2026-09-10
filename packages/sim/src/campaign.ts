import {initProgression} from './progression';
import {createRiverfrontCampaign} from './riverfrontCampaign';
import { initFixedTram } from './fixedTram';
import { SurveyPlacementError } from './campaignSurvey';
/** Explicit new-session factory. Never converts a running legacy state. */
import {initNavigation} from './navigation';
import { DEFAULT_CONFIG, createState } from './sim';
import { protoCalibrated, type SimConfig, type SimState } from './types';
import { citySpec } from './city';
import { ensureFlow } from './flow';
import { initDefence } from './campaignDefence';
import { initExpansion } from './expansion';
import { initKnowledge } from './campaignGuide';
import { initTurbine } from './campaignTurbine';
import { initForeman, initRecruits } from './campaignRecruits';
import { initDiscovery } from './campaignDiscovery';
import { initDistricts } from './campaignDistricts';
import { CAMPAIGN_RULESET } from './rules';
export function campaignConfig(): SimConfig {
  return protoCalibrated({ ...DEFAULT_CONFIG, scatter: true, economy: true, walk: true, power: true, supply: 'generators', draw: 'half' });
}
export function createCampaign(seed = 3): SimState {
  void seed; return createRiverfrontCampaign();
}
/** Preserved procedural constructor for historical fixtures, never normal New Game. */
export function createProceduralCampaign(seed = 3): SimState {
  const config = campaignConfig();
  const st = createState(citySpec(seed, 'river', config), config, seed, CAMPAIGN_RULESET);
  ensureFlow(st);
  initExpansion(st, expansion => {
    initDefence(expansion);
    try {
      initDistricts(expansion, district => {
        try { initDiscovery(district); initRecruits(district); initTurbine(district); initForeman(district); return true; }
        catch(error) { if(error instanceof SurveyPlacementError)return false; throw error; }
      });
      return true;
    } catch(error) { if(error instanceof SurveyPlacementError)return false; throw error; }
  });
  initFixedTram(st); initProgression(st); initKnowledge(st,true); initNavigation(st);
  return st;
}
