import {parseBindings,type BindingOverrides} from './controls';
import {CAMPAIGN_KINDS,type Kind} from '@relight/sim';
export type QuickTool=Exclude<Kind,'depot'>|'rifle';
export const DEFAULT_QUICKBAR:readonly (QuickTool|null)[]=['belt','inserter','excavator',null,null,null,null,null,'rifle',null];
export const QUICKBAR_KEYS=['1','2','3','4','5','6','7','8','9','0'] as const;
export interface UiPreferences {version:1;quickbar:(QuickTool|null)[];scale:100|125|150;motion:'system'|'reduce'|'full';bindings:BindingOverrides;dismissedHints:string[]}
export function parseUiPreferences(raw:string|null):UiPreferences {
 const base:UiPreferences={version:1,quickbar:[...DEFAULT_QUICKBAR],scale:100,motion:'system',bindings:{},dismissedHints:[]};
 try{const v=JSON.parse(raw??'null');if(v?.version!==1)return base;
  if(Array.isArray(v.quickbar)&&v.quickbar.length===10){const seen=new Set<QuickTool>();base.quickbar=v.quickbar.map((k:unknown)=>{if(!(k==='rifle'||(CAMPAIGN_KINDS.includes(k as Kind)&&k!=='depot'))||seen.has(k as QuickTool))return null;seen.add(k as QuickTool);return k as QuickTool;});}   // GP-HOME-REPAIR: an earlier build let one tool pin to several slots; the first occurrence stays
  if([100,125,150].includes(v.scale))base.scale=v.scale;
  if(['system','reduce','full'].includes(v.motion))base.motion=v.motion;
  base.bindings=parseBindings(v.bindings);base.dismissedHints=Array.isArray(v.dismissedHints)?v.dismissedHints.filter((x:unknown)=>x==='opening'):[];
 }catch{/* Missing and corrupt preferences use safe defaults. */}return base;
}
export function readUiPreferences():UiPreferences {try{return parseUiPreferences(localStorage.getItem('relight.ui.v1'));}catch{return parseUiPreferences(null);}}
export const preferenceView:{value:UiPreferences}={value:readUiPreferences()};
/** Update memory even when storage is denied; callers explain the limited persistence. */
export function saveUiPreferences(change:Partial<Omit<UiPreferences,'version'>>):boolean {
 preferenceView.value=parseUiPreferences(JSON.stringify({...preferenceView.value,...change,version:1}));
 try{localStorage.setItem('relight.ui.v1',JSON.stringify(preferenceView.value));return true;}catch{return false;}
}
export function saveQuickbar(slots:readonly (QuickTool|null)[]):boolean{return saveUiPreferences({quickbar:[...slots]});}
/** Assignment swaps an already-used tool, preserving ten stable positions and unrelated choices. */
export function assignQuickbar(slots:readonly (QuickTool|null)[],index:number,tool:QuickTool|null):(QuickTool|null)[]{
  const next=[...slots];if(!Number.isInteger(index)||index<0||index>=10)return next;
  const prior=tool===null?-1:next.indexOf(tool);if(prior>=0&&prior!==index)next[prior]=next[index];next[index]=tool;return next;
}
export const quickbarView:{slots:(QuickTool|null)[]}={slots:[...DEFAULT_QUICKBAR]};
