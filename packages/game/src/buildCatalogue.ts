import {DEFAULT_QUICKBAR,QUICKBAR_KEYS,type QuickTool} from './uiPreferences';
import { CAMPAIGN_KINDS, KINDS, KIND_LABEL, knownEquipment, type SimState, type Kind } from '@relight/sim';
import { shortcut, bound, BINDINGS, type Binding } from './controls';
import { FACTORY_TEXT } from './factoryStrings';

export const buildCatalogue = (campaign: boolean) => (campaign ? CAMPAIGN_KINDS : KINDS)
  .filter((kind): kind is Exclude<Kind, 'depot'> => kind !== 'depot')
  .map(kind => ({ kind, label: KIND_LABEL[kind], key: kind in BINDINGS?shortcut(kind as Binding):'', what: FACTORY_TEXT.purpose[kind] }));
export function toolForKey(key: string, slots:readonly (QuickTool|null)[]=DEFAULT_QUICKBAR): Exclude<Kind, 'depot'> | 'rifle' | null {
  const index=QUICKBAR_KEYS.indexOf(key as typeof QUICKBAR_KEYS[number]);if(index>=0)return slots[index]??null;
  if (bound('rifle', key)) return 'rifle';
  return buildCatalogue(true).find(b => b.kind in BINDINGS&&bound(b.kind as Binding, key))?.kind ?? null;
}
export const toolKeyLine = buildCatalogue(true).filter(b => b.key).map(b => `${b.key} ${b.label}`).join(' · ');

export const visibleBuildCatalogue=(st:SimState)=>buildCatalogue(!!st.campaign).filter(b=>knownEquipment(st,b.kind));
export type BuildCategory='Production'|'Logistics'|'Power'|'Defence';
export function buildCategory(kind:Kind):BuildCategory {
  if(['excavator','assembler','assembler2','mixer','foundry','refinery','pumpjack'].includes(kind))return 'Production';
  if(['turret','cannon','wall','barricade'].includes(kind))return 'Defence';
  if(['generator','pole','bigpole','substation','lamp','arclamp','floodlight'].includes(kind))return 'Power';
  return 'Logistics';
}
