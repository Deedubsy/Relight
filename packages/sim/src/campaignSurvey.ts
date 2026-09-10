/** Fresh survey candidates share immutable terrain, but own all initializer mutations. */
import type { SimState } from './types';
export class SurveyPlacementError extends Error {}
export type SurveyAcceptance = (candidate: SimState) => boolean;
export function surveyCandidate(st: SimState): SimState {
  return {...st, campaign: structuredClone(st.campaign), flow: {...st.flow!}, events: [...st.events]};
}
export function acceptSurvey(st: SimState, candidate: SimState, accept?: SurveyAcceptance): boolean {
  if (accept && !accept(candidate)) return false;
  st.campaign = candidate.campaign;
  st.flow!.rev = candidate.flow!.rev;
  st.events = candidate.events;
  return true;
}
