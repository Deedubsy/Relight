import { CAMPAIGN_KINDS, KINDS, KIND_LABEL, type Kind } from '@relight/sim';
import { shortcut, bound, type Binding } from './controls';
import { FACTORY_TEXT } from './factoryStrings';

export const buildCatalogue = (campaign: boolean) => (campaign ? CAMPAIGN_KINDS : KINDS)
  .filter((kind): kind is Exclude<Kind, 'depot'> => kind !== 'depot')
  .map(kind => ({ kind, label: KIND_LABEL[kind], key: shortcut(kind), what: FACTORY_TEXT.purpose[kind] }));
export function toolForKey(key: string): Exclude<Kind, 'depot'> | 'rifle' | null {
  if (bound('rifle', key)) return 'rifle';
  return buildCatalogue(true).find(b => bound(b.kind as Binding, key))?.kind ?? null;
}
export const toolKeyLine = buildCatalogue(true).filter(b => b.key).map(b => `${b.key} ${b.label}`).join(' · ');
