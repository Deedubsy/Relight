/** Current campaign catalogue only: no dormant legacy recipes or imaginary consumers. */
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
  const all=[...RECIPE_IDS.map(id=>({recipe:ASSEMBLER_RECIPES[id],machine:'assembler' as Kind})),{recipe:recipeOf({kind:'mixer'}),machine:'mixer' as Kind}];
  const recipes=all.filter(r=>recipeOutput(r.recipe)===item).map(({recipe:r,machine})=>({name:r.name,inputs:Object.entries(r.inputs).map(([item,count])=>({item,count})),count:recipeYield(r),seconds:r.seconds,machine:KIND_LABEL[machine],kw:MACHINE_KW[machine],available:!lockReason(st,machine),provenance:machine==='mixer'?`Concrete crew · ${campaignRecruited(st,'concrete')?'recruited; plans retained':'recruit to unlock the Mixer'}`:'Starting Assembler plans'}));
  const sources:string[]=[];
  if(['steel','copper','coal'].includes(item))sources.push('Starting house supplies and finite home patches; mine by hand or use a powered Excavator. Regional extraction sites use powered Excavators.');
  if(item==='stone')sources.push('Starting house supplies and finite rubble in restored civic districts, including Home Court. Mine by hand or use a powered Excavator.');
  if(item==='magazine')sources.push('Starting house supplies; handcraft magazines near the house using carried steel and copper, or automate them in an Assembler.');
  const uses:string[]=[];
  for(const {recipe:r} of all)if(r.inputs[item])uses.push(`${r.name}: ${r.inputs[item]} ${item} per craft.`);
  const construction=CAMPAIGN_KINDS.filter(k=>k!=='depot'&&((MACHINE_COST[k] as Partial<Record<Item,number>>)[item]??0)>0);
  for(const k of construction)uses.push(`${KIND_LABEL[k]} construction: ${(MACHINE_COST[k] as Partial<Record<Item,number>>)[item]} ${item}${lockReason(st,k)?` · ${lockReason(st,k)}`:''}.`);
  for(const id of SITE_IDS)if(knownSite(st,id)){const n=(EXPANSION[id] as Partial<Record<Item,number>>)[item];if(n)uses.push(`${SITE_LABELS[id]} restoration: ${n} ${item} delivered locally.`);}
  const t=st.campaign?.turbine;if(t&&(t.seenAt>=0||t.restoredAt>=0)){const n=(TURBINE.cost as Partial<Record<Item,number>>)[item];if(n)uses.push(`Turbine hall restoration: ${n} ${item} delivered locally.`);}
  if(item==='steel'||item==='copper')uses.push(`Paid manual and workshop repairs, disabled-core recovery and radio precision upgrades consume ${item}.`);
  if(item==='copper')uses.push('Repairing an eaten light uses 1 copper.');
  if(item==='coal')uses.push(`Generator fuel: ${COAL_MJ} MJ per coal; ${GENERATOR_KW} kW capacity. Fuel burns with actual load.`);
  if(item==='magazine')uses.push(`${ROUNDS_PER_MAG} rounds per magazine. Feed Gun turrets directly by belt, powered inserter or E; the engineer reloads the rifle from carried magazines.`);
  if(!uses.length)uses.push('No implemented consumer yet. Producing this item is optional; keep or transport it.');
  return {item,title:item==='magazine'?'Shot magazine':item[0].toUpperCase()+item.slice(1),sources,recipes,uses,transport:'Carry or store in the house, Supply chests, station inventories and truck cargo. Belts and inserters move items; operating trams carry configured freight. Transfers do not create items.'};
}
