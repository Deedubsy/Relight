/** Current campaign catalogue only: no dormant legacy recipes or imaginary consumers. */
import {itemName} from './itemNames';
import type { SimState } from './types';
import { ITEMS, RECIPE_IDS, ASSEMBLER_RECIPES, recipeOf, recipeOutput, recipeYield, MACHINE_KW, MACHINE_COST, CAMPAIGN_KINDS, KIND_LABEL, lockReason, type Item, type Kind } from './flow';
import { campaignRecruited } from './campaignRecruits';
import { isCampaign } from './rules';
import { SITE_IDS, EXPANSION, SITE_LABELS } from './expansion';
import { knownSite } from './campaignGuide';
import { TURBINE } from './campaignTurbine';
import { COAL_MJ, GENERATOR_KW, ROUNDS_PER_MAG } from './recipes';
export interface ItemGuide { item:Item; title:string; sources:string[]; recipes:{name:string;inputs:{item:string;count:number}[];count:number;seconds:number;machine:string;kw:number;provenance:string;available:boolean}[]; uses:string[]; transport:string }
export function itemGuide(st:SimState,item:Item):ItemGuide|null {
  if(!isCampaign(st)||!ITEMS.includes(item))return null;
  const all=[...RECIPE_IDS.map(id=>({recipe:ASSEMBLER_RECIPES[id],machine:(id==='iron'||id==='copper'?'foundry':id==='fuel'||id==='polymer'?'refinery':'assembler') as Kind})),{recipe:recipeOf({kind:'mixer'}),machine:'mixer' as Kind}];
  const recipes=all.filter(r=>recipeOutput(r.recipe)===item).map(({recipe:r,machine})=>({name:r.name,inputs:Object.entries(r.inputs).map(([item,count])=>({item,count})),count:st.flow?.ammoVersion===1&&r.output==='rounds'?r.count:recipeYield(r),seconds:r.seconds,machine:KIND_LABEL[machine],kw:MACHINE_KW[machine],available:!lockReason(st,machine)&&(r.output!=='shell'||!!st.campaign?.progression?.arsenal),provenance:machine==='mixer'?`Concrete crew · ${campaignRecruited(st,'concrete')?'recruited; plans retained':'recruit to unlock the Mixer'}`:machine==='foundry'?'Foundry plans':machine==='refinery'?'Refinery plans':r.output==='shell'?'Junction Heart / Arsenal reward':'Assembler plans'}));
  const sources:string[]=[];
  if(['steel','copper','coal'].includes(item))sources.push('Finite Home salvage patches; mine by hand or use a powered Excavator. Regional extraction sites use powered Excavators.');
  if(item==='stone')sources.push('Finite rubble at accessible district sites, including Home Court. Mine by hand or use a powered Excavator.');
  if(item==='magazine')sources.push('Craft 10 bullets per 20 seconds at Home Workshop near the house using carried steel and copper, or automate them in an Assembler.');
  if(item==='ironore'||item==='copperore')sources.push('Marked finite regional ore deposits; hand extraction or a powered Excavator. Process ore in a Foundry.');
  if(item==='crude')sources.push('Marked finite oil deposits. A powered Pumpjack extracts crude as physical items; hand mining cannot extract it.');
  if(item.startsWith('core'))sources.push('Find the matching alien relay within its broad search area, defeat its defenders, then recover locally into a free backpack slot.');
  if(item.startsWith('artifact'))sources.push('Recover the matching optional machine artifact cache while exploring.');
  const uses:string[]=[];
  if(item.startsWith('core'))uses.push('Install one carried core in a chosen prepared regional plant. Routine repairs retain it.');
  if(item.startsWith('artifact'))uses.push('One removable slot per production machine: +10% processing capacity; actual output still depends on inputs, power and output space. Inspect the machine to attach/remove; packing preserves the artifact.');
  if(item==='shell')uses.push('Cannon ammunition: one shell fires one 50-damage shot. Unlock via the Junction Heart / Arsenal.');
  for(const {recipe:r} of all)if(r.inputs[item])uses.push(`${r.name}: ${r.inputs[item]} ${itemName(item)} per craft.`);
  const construction=CAMPAIGN_KINDS.filter(k=>!['depot','track','tramstop','tram'].includes(k)&&((MACHINE_COST[k] as Partial<Record<Item,number>>)[item]??0)>0);
  for(const k of construction)uses.push(`${KIND_LABEL[k]} construction: ${(MACHINE_COST[k] as Partial<Record<Item,number>>)[item]} ${itemName(item)}${lockReason(st,k)?` · ${lockReason(st,k)}`:''}.`);
  for(const id of SITE_IDS)if(knownSite(st,id)){const n=(EXPANSION[id] as Partial<Record<Item,number>>)[item];if(n)uses.push(`${SITE_LABELS[id]} restoration: ${n} ${itemName(item)} delivered locally.`);}
  const t=st.campaign?.turbine;if(t&&(t.seenAt>=0||t.restoredAt>=0)){const n=(TURBINE.cost as Partial<Record<Item,number>>)[item];if(n)uses.push(`Turbine hall restoration: ${n} ${itemName(item)} delivered locally.`);}
  if(item==='steel'||item==='copper')uses.push(`Paid manual and workshop repairs, disabled-core recovery and radio precision upgrades consume ${itemName(item)}.`);
  if(item==='copper')uses.push('Repairing an eaten light uses 1 copper.');
  if(item==='coal'||item==='fuel')uses.push(`Generator fuel: ${COAL_MJ} MJ per fuel item; ${GENERATOR_KW} kW capacity. Fuel burns with actual load.`);
  if(item==='magazine')uses.push(`${st.flow?.ammoVersion===1?1:ROUNDS_PER_MAG} round(s) per item. Feed Gun turrets directly by belt, powered inserter or E; the engineer reloads the rifle from carried bullets.`);
  if(!uses.length)uses.push('No implemented consumer yet. Producing this item is optional; keep or transport it.');
  return {item,title:itemName(item),sources,recipes,uses,transport:'Carry or store in Home storage, Supply chests, station inventories and truck cargo. Belts and inserters move items; operating trams carry configured freight. Transfers do not create items.'};
}
